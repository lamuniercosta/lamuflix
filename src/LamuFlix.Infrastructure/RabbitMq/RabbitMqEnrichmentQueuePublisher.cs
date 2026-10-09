using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using RabbitMQ.Client;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class RabbitMqEnrichmentQueuePublisher : IEnrichmentQueue
{
    private static readonly ActivitySource Source = new(TelemetryConstants.ActivitySourceName);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly IReadOnlyDictionary<string, object?> NoAdditions =
        new Dictionary<string, object?>(StringComparer.Ordinal);

    private readonly RabbitMqTopology topology;
    private readonly RabbitMqConnectionOwner owner;

    public RabbitMqEnrichmentQueuePublisher(RabbitMqTopology topology, RabbitMqConnectionOwner owner)
    {
        this.topology = topology;
        this.owner = owner;
    }

    public Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct) =>
        PublishAsync(message, RabbitMqTopology.RequestedRoutingKey, ct);

    public Task PublishAsync(EnrichmentRequested message, string routingKey, CancellationToken ct) =>
        PublishAsync(message, routingKey, NoAdditions, ct);

    public async Task PublishAsync(
        EnrichmentRequested message,
        string routingKey,
        IReadOnlyDictionary<string, object?> additionalHeaders,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrEmpty(routingKey);
        ArgumentNullException.ThrowIfNull(additionalHeaders);
        EnsureUsableHeaders(additionalHeaders);

        await topology.EnsureDeclaredAsync(ct);

        // ReSharper disable once ExplicitCallerInfoArgument - the span name is a telemetry contract, not the caller member name.
        using var activity = Source.StartActivity(
            TelemetryConstants.EnrichmentEnqueue,
            ActivityKind.Producer);
        activity?.SetTag(TelemetryConstants.MovieId, message.MovieId.Value);

        var headers = new Dictionary<string, object?>(StringComparer.Ordinal);
        TraceContextCarrier.Inject(headers, activity?.Context);
        foreach (var (key, value) in additionalHeaders)
        {
            headers[key] = value;
        }

        var properties = new BasicProperties { Persistent = true, Headers = headers };

        var connection = await owner.GetAsync(ct);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            ct);
        await channel.BasicPublishAsync(
            RabbitMqTopology.ExchangeName,
            routingKey,
            mandatory: true,
            basicProperties: properties,
            body: Serialize(message),
            cancellationToken: ct);
    }

    private static void EnsureUsableHeaders(IReadOnlyDictionary<string, object?> additionalHeaders)
    {
        foreach (var (key, value) in additionalHeaders)
        {
            if (value is null)
            {
                throw new ArgumentException(
                    $"Header '{key}' must not carry a null value.",
                    nameof(additionalHeaders));
            }

            if (IsReservedHeader(key))
            {
                throw new ArgumentException(
                    $"Header '{key}' is owned by the publisher or the broker and cannot be supplied.",
                    nameof(additionalHeaders));
            }
        }
    }

    private static bool IsReservedHeader(string key) =>
        key is TraceContextCarrier.TraceParentHeader or TraceContextCarrier.TraceStateHeader
        || key.Equals("x-death", StringComparison.Ordinal)
        || key.StartsWith("x-first-death-", StringComparison.Ordinal);

    private static ReadOnlyMemory<byte> Serialize(EnrichmentRequested message) =>
        JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
}
