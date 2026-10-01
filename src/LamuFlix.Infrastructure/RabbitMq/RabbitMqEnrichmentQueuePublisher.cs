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

    private readonly RabbitMqTopology topology;
    private readonly RabbitMqConnectionOwner owner;

    public RabbitMqEnrichmentQueuePublisher(RabbitMqTopology topology, RabbitMqConnectionOwner owner)
    {
        this.topology = topology;
        this.owner = owner;
    }

    public Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct) =>
        PublishAsync(message, RabbitMqTopology.RequestedRoutingKey, ct);

    public async Task PublishAsync(EnrichmentRequested message, string routingKey, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrEmpty(routingKey);

        await topology.EnsureDeclaredAsync(ct);

        using var activity = Source.StartActivity(
            TelemetryConstants.EnrichmentEnqueue,
            ActivityKind.Producer);
        activity?.SetTag(TelemetryConstants.MovieId, message.MovieId.Value);

        var headers = new Dictionary<string, object?>(StringComparer.Ordinal);
        TraceContextCarrier.Inject(headers, activity?.Context);
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

    private static ReadOnlyMemory<byte> Serialize(EnrichmentRequested message) =>
        JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
}
