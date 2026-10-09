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

[Collection(nameof(RabbitMqCollection))]
public sealed class RabbitMqPublisherTests(RabbitMqFixture fixture) : IAsyncLifetime
{

    private readonly RabbitMqProbe probe = new(fixture);

    public async ValueTask InitializeAsync() => await fixture.ResetTopologyAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task EnqueueAsync_ARoutablePublish_ReachesTheRequestedQueue()
    {
        TestContext.Current.TestOutputHelper?.WriteLine($"[rabbitmq-container-id] {fixture.Container.Id}");
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

    [Fact]
    public async Task PublishAsync_WithTerminalFailureHeaders_CarriesTheClosedCodeHeadersAndTelemetry()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();
        await host.Topology.EnsureDeclaredAsync(ct);

        await host.Publisher.PublishAsync(
            new EnrichmentRequested(new MovieId(41), 3),
            RabbitMqTopology.DeadLetterRoutingKey,
            new Dictionary<string, object?>
            {
                ["x-lamuflix-failure-category"] = "provider_unavailable",
                ["x-lamuflix-failure-reason"] = "max_attempts_exhausted",
                ["x-lamuflix-failure-attempt"] = 3,
            },
            ct);

        var message = await probe.PollGetMatchingAsync(
            RabbitMqTopology.DeadLetterQueue,
            candidate => Read(candidate).MovieId.Value == 41,
            ct);
        message.ShouldNotBeNull();
        Header(message, "x-lamuflix-failure-category").ShouldBe("provider_unavailable");
        Header(message, "x-lamuflix-failure-reason").ShouldBe("max_attempts_exhausted");
        RawHeader(message, "x-lamuflix-failure-attempt").ShouldBeOfType<int>().ShouldBe(3);
        Header(message, TraceContextCarrier.TraceParentHeader).ShouldStartWith("00-");
    }

    [Theory]
    [InlineData(RabbitMqTopology.RequestedRoutingKey, RabbitMqTopology.RequestedQueue)]
    [InlineData(RabbitMqTopology.RetryRoutingKey, RabbitMqTopology.RetryQueue)]
    public async Task PublishAsync_WithoutAdditions_LeavesTheClosedCodeHeadersOff(string routingKey, string queue)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();
        await host.Topology.EnsureDeclaredAsync(ct);

        await host.Publisher.PublishAsync(new EnrichmentRequested(new MovieId(42), 1), routingKey, ct);

        var message = await probe.PollGetMatchingAsync(
            queue,
            candidate => Read(candidate).MovieId.Value == 42,
            ct);
        message.ShouldNotBeNull();
        RawHeader(message, "x-lamuflix-failure-category").ShouldBeNull();
        RawHeader(message, "x-lamuflix-failure-reason").ShouldBeNull();
        RawHeader(message, "x-lamuflix-failure-attempt").ShouldBeNull();
    }

    [Theory]
    [InlineData("traceparent")]
    [InlineData("tracestate")]
    [InlineData("x-death")]
    [InlineData("x-first-death-reason")]
    public async Task PublishAsync_WithAReservedHeader_Throws(string key)
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();
        var additions = new Dictionary<string, object?> { [key] = "reserved" };

        await Should.ThrowAsync<ArgumentException>(
            () => host.Publisher.PublishAsync(
                new EnrichmentRequested(new MovieId(43), 1),
                RabbitMqTopology.RequestedRoutingKey,
                additions,
                ct));
    }

    [Fact]
    public async Task PublishAsync_WithANullHeaderValue_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();
        var additions = new Dictionary<string, object?> { ["x-lamuflix-failure-category"] = null };

        await Should.ThrowAsync<ArgumentException>(
            () => host.Publisher.PublishAsync(
                new EnrichmentRequested(new MovieId(44), 1),
                RabbitMqTopology.RequestedRoutingKey,
                additions,
                ct));
    }

    [Fact]
    public async Task PublishAsync_WithANullHeaderDictionary_Throws()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();

        await Should.ThrowAsync<ArgumentNullException>(
            () => host.Publisher.PublishAsync(
                new EnrichmentRequested(new MovieId(45), 1),
                RabbitMqTopology.RequestedRoutingKey,
                // ReSharper disable once NullableWarningSuppressionIsUsed - the null dictionary is the subject of this guard test.
                null!,
                ct));
    }

    [Fact]
    public async Task PublishAsync_WithCallerHeaders_LeavesTheCallerDictionaryUnchanged()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost();
        await host.Topology.EnsureDeclaredAsync(ct);
        var additions = new Dictionary<string, object?> { ["x-lamuflix-failure-category"] = "unknown" };

        await host.Publisher.PublishAsync(
            new EnrichmentRequested(new MovieId(46), 2),
            RabbitMqTopology.DeadLetterRoutingKey,
            additions,
            ct);

        additions.Count.ShouldBe(1);
        additions["x-lamuflix-failure-category"].ShouldBe("unknown");

        var message = await probe.PollGetMatchingAsync(
            RabbitMqTopology.DeadLetterQueue,
            candidate => Read(candidate).MovieId.Value == 46,
            ct);
        message.ShouldNotBeNull();
        Header(message, "x-lamuflix-failure-category").ShouldBe("unknown");
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

    private static object? RawHeader(BasicGetResult message, string name) =>
        message.BasicProperties.Headers is { } headers && headers.TryGetValue(name, out var value)
            ? value
            : null;

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

