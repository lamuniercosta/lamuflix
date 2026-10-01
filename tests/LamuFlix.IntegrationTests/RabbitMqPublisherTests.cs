using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class RabbitMqPublisherTests(RabbitMqFixture fixture) : IClassFixture<RabbitMqFixture>
{

    private readonly RabbitMqProbe probe = new(fixture);

    [Fact]
    public async Task EnqueueAsync_ARoutablePublish_ReachesTheRequestedQueue()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();
        await host.Topology.EnsureDeclaredAsync(ct);

        await host.Publisher.EnqueueAsync(new EnrichmentRequested(new MovieId(11), 1), ct);

        var message = await Await(ct, 11);
        message.ShouldNotBeNull();
        Read(message).Attempt.ShouldBe(1);
    }

    [Fact]
    public async Task EnqueueAsync_WithoutAnyListener_WritesAValidTraceParentHeader()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();
        await host.Topology.EnsureDeclaredAsync(ct);

        await host.Publisher.EnqueueAsync(new EnrichmentRequested(new MovieId(12), 1), ct);

        var message = await Await(ct, 12);
        message.ShouldNotBeNull();
        var traceParent = Header(message, TraceContextCarrier.TraceParentHeader);
        traceParent.ShouldNotBeNullOrWhiteSpace();
        traceParent.ShouldStartWith("00-");
        message.BasicProperties.Persistent.ShouldBe(true);
    }

    [Fact]
    public async Task EnqueueAsync_WithAnAmbientContext_UsesThatTraceId()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();
        await host.Topology.EnsureDeclaredAsync(ct);
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        using var activity = new Activity("outer")
            .SetIdFormat(ActivityIdFormat.W3C)
            .SetParentId($"00-{traceId}-{spanId}-01");
        activity.Start();

        await host.Publisher.EnqueueAsync(new EnrichmentRequested(new MovieId(13), 1), ct);

        var message = await Await(ct, 13);
        message.ShouldNotBeNull();
        TraceContextCarrier.ExtractContext(message.BasicProperties.Headers).TraceId.ShouldBe(traceId);
    }

    [Fact]
    public async Task PublishAsync_AnUnroutablePublish_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();
        await host.Topology.EnsureDeclaredAsync(ct);

        await Should.ThrowAsync<PublishException>(
            () => host.Publisher.PublishAsync(new EnrichmentRequested(new MovieId(14), 1), "no-such-key", ct));
    }

    [Fact]
    public async Task PublishAsync_AnUnreachableBroker_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        var options = fixture.Options with { Port = 1 };
        await using var owner = new RabbitMqConnectionOwner(Options.Create(options));
        var publisher = new RabbitMqEnrichmentQueuePublisher(
            new RabbitMqTopology(
                owner,
                Options.Create(options),
                Options.Create(new EnrichmentOptions())),
            owner);

        await Should.ThrowAsync<Exception>(
            () => publisher.EnqueueAsync(new EnrichmentRequested(new MovieId(15), 1), ct));
    }

    [Fact]
    public async Task PublishAsync_TwoConcurrentPublishes_BothArrive()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();
        await host.Topology.EnsureDeclaredAsync(ct);

        await Task.WhenAll(
            host.Publisher.EnqueueAsync(new EnrichmentRequested(new MovieId(21), 1), ct),
            host.Publisher.EnqueueAsync(new EnrichmentRequested(new MovieId(22), 2), ct));

        var found = await probe.PollGetAllAsync(
            RabbitMqTopology.RequestedQueue,
            new HashSet<string>(StringComparer.Ordinal) { "21", "22" },
            message => Read(message).MovieId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ct);

        found.Count.ShouldBe(2);
        Read(found["21"]).Attempt.ShouldBe(1);
        Read(found["22"]).Attempt.ShouldBe(2);
    }

    [Fact]
    public async Task PublishAsync_ToTheRetryKey_ArrivesOnTheRetryQueue()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();
        await host.Topology.EnsureDeclaredAsync(ct);

        await host.Publisher.PublishAsync(new EnrichmentRequested(new MovieId(31), 4), RabbitMqTopology.RetryRoutingKey, ct);

        var message = await probe.PollGetMatchingAsync(
            RabbitMqTopology.RetryQueue,
            candidate => Read(candidate).MovieId.Value == 31,
            ct);
        message.ShouldNotBeNull();
        Read(message).Attempt.ShouldBe(4);
    }

    private Task<BasicGetResult?> Await(CancellationToken cancellationToken, int movieId) =>
        probe.PollGetMatchingAsync(
            RabbitMqTopology.RequestedQueue,
            message => Read(message).MovieId.Value == movieId,
            cancellationToken);

    private static EnrichmentRequested Read(BasicGetResult message) =>
        JsonSerializer.Deserialize<EnrichmentRequested>(
            Encoding.UTF8.GetString(message.Body.Span),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldNotBeNull();

    private static string? Header(BasicGetResult message, string name) =>
        TraceContextCarrier.TryReadHeader(message.BasicProperties.Headers, name, out var value) ? value : null;

    private PublisherHost NewHost()
    {
        var options = fixture.Options;
        var owner = new RabbitMqConnectionOwner(Options.Create(options));
        var topology = new RabbitMqTopology(
            owner,
            Options.Create(options),
            Options.Create(new EnrichmentOptions { MaxAttempts = 3 }));
        return new PublisherHost(owner, topology, new RabbitMqEnrichmentQueuePublisher(topology, owner));
    }

    private sealed class PublisherHost(
        RabbitMqConnectionOwner owner,
        RabbitMqTopology topology,
        RabbitMqEnrichmentQueuePublisher publisher) : IAsyncDisposable
    {
        public RabbitMqTopology Topology { get; } = topology;

        public RabbitMqEnrichmentQueuePublisher Publisher { get; } = publisher;

        public ValueTask DisposeAsync() => owner.DisposeAsync();
    }
}

