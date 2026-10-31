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

public sealed class RabbitMqThumbnailQueuePublisher(RabbitMqTopology topology, RabbitMqConnectionOwner owner)
{
    private static readonly ActivitySource Source = new(TelemetryConstants.ActivitySourceName);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync(ThumbnailRequested message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        await topology.EnsureDeclaredAsync(cancellationToken);

        // ReSharper disable once ExplicitCallerInfoArgument - the span name is a telemetry contract, not the caller member name.
        using var activity = Source.StartActivity(
            TelemetryConstants.ThumbnailEnqueue,
            ActivityKind.Producer);
        activity?.SetTag(TelemetryConstants.MovieId, message.MovieId.Value);

        var headers = new Dictionary<string, object?>(StringComparer.Ordinal);
        TraceContextCarrier.Inject(headers, activity?.Context);

        var properties = new BasicProperties { Persistent = true, Headers = headers };
        var body = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);

        var connection = await owner.GetAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken);
        await channel.BasicPublishAsync(
            RabbitMqTopology.ExchangeName,
            RabbitMqTopology.ThumbnailRoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
