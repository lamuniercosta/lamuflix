using Xunit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.Options;
using Shouldly;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(RabbitMqCollection))]
public sealed class QuotaDeferredTelemetryTests(RabbitMqFixture fixture) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqProbe probe = new(fixture);

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await fixture.ResetTopologyAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.QuotaDeferredQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.RequestedQueue, ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DelayedPublish_InjectsTraceparentIntoHeaders()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var options = fixture.Options with { Prefetch = 1 };
        await using var owner = new RabbitMqConnectionOwner(Options.Create(options));
        var topology = new RabbitMqTopology(
            owner,
            Options.Create(options),
            Options.Create(new EnrichmentOptions { MaxAttempts = 3 }));
        var publisher = new RabbitMqEnrichmentQueuePublisher(topology, owner);
        var movie = new MovieId(901);
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();

        // act
        using (var ambient = new Activity("quota-deferred-publish")
                   .SetIdFormat(ActivityIdFormat.W3C)
                   .SetParentId($"00-{traceId}-{spanId}-01"))
        {
            ambient.Start();
            await publisher.PublishAsync(
                new EnrichmentRequested(movie, 3),
                RabbitMqTopology.QuotaDeferredRoutingKey,
                new Dictionary<string, object?>(StringComparer.Ordinal),
                ct,
                expirationMilliseconds: 1000);
        }

        // assert
        var delivered = await probe.PollGetMatchingAsync(
            RabbitMqTopology.RequestedQueue,
            candidate => JsonSerializer.Deserialize<EnrichmentRequested>(candidate.Body.Span, SerializerOptions)
                is { MovieId.Value: 901 },
            ct);
        delivered.ShouldNotBeNull();
        var traceparent = Header(delivered, TraceContextCarrier.TraceParentHeader);
        traceparent.ShouldNotBeNull();
        traceparent.ShouldStartWith("00-");
        traceparent.ShouldContain(traceId.ToHexString());
        Header(delivered, TraceContextCarrier.TraceStateHeader).ShouldBeNull();
    }

    private static string? Header(RabbitMQ.Client.BasicGetResult result, string name)
    {
        if (result.BasicProperties.Headers is not { } headers || !headers.TryGetValue(name, out var raw))
        {
            return null;
        }

        return raw is byte[] bytes ? System.Text.Encoding.UTF8.GetString(bytes) : raw?.ToString();
    }
}
