using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class RabbitMqTopology(
    RabbitMqConnectionOwner owner,
    IOptions<RabbitMqOptions> rabbitMq,
    IOptions<EnrichmentOptions> enrichment)
{
    public const string ExchangeName = "lamuflix.enrichment";
    public const string RequestedQueue = "enrichment.requested";
    public const string RetryQueue = "enrichment.retry";
    public const string DeadLetterQueue = "enrichment.dead-letter";
    public const string RequestedRoutingKey = "requested";
    public const string RetryRoutingKey = "retry";
    public const string DeadLetterRoutingKey = "dead-letter";
    public const string FeaturesRequestedQueue = "features.requested";
    public const string FeaturesRetryQueue = "features.retry";
    public const string FeaturesDeadLetterQueue = "features.dead-letter";
    private const string QuorumQueueType = "quorum";
    private const string DeadLetterStrategyAtLeastOnce = "at-least-once";
    private const string OverflowRejectPublish = "reject-publish";

    private readonly SemaphoreSlim gate = new(1, 1);
    private volatile bool declared;

    public async Task EnsureDeclaredAsync(CancellationToken cancellationToken)
    {
        if (declared)
        {
            return;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (declared)
            {
                return;
            }

            await DeclareAsync(cancellationToken);
            declared = true;
        }
        finally
        {
            gate.Release();
        }
    }

    public static IDictionary<string, object?> RequestedArguments(int maxAttempts) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["x-queue-type"] = QuorumQueueType,
            ["x-dead-letter-exchange"] = ExchangeName,
            ["x-dead-letter-routing-key"] = DeadLetterRoutingKey,
            ["x-delivery-limit"] = maxAttempts,
            ["x-dead-letter-strategy"] = DeadLetterStrategyAtLeastOnce,
            ["x-overflow"] = OverflowRejectPublish,
        };

    public static IDictionary<string, object?> RetryArguments(int retryDelayMilliseconds) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["x-queue-type"] = QuorumQueueType,
            ["x-message-ttl"] = retryDelayMilliseconds,
            ["x-dead-letter-exchange"] = ExchangeName,
            ["x-dead-letter-routing-key"] = RequestedRoutingKey,
            ["x-dead-letter-strategy"] = DeadLetterStrategyAtLeastOnce,
            ["x-overflow"] = OverflowRejectPublish,
        };

    public static IDictionary<string, object?> FeaturesRequestedArguments(int maxAttempts) =>
        WithDeadLetterRoutingKey(RequestedArguments(maxAttempts), EnrichmentRouting.FeaturesDeadLetterRoutingKey);

    public static IDictionary<string, object?> FeaturesRetryArguments(int retryDelayMilliseconds) =>
        WithDeadLetterRoutingKey(RetryArguments(retryDelayMilliseconds), EnrichmentRouting.FeaturesRequestedRoutingKey);

    public static IDictionary<string, object?> DeadLetterArguments() =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["x-queue-type"] = QuorumQueueType,
        };

    public static int RetryDelayMilliseconds(RabbitMqOptions options) =>
        checked((int)Math.Round(options.RetryDelay.TotalMilliseconds, MidpointRounding.AwayFromZero));

    private static IDictionary<string, object?> WithDeadLetterRoutingKey(
        IDictionary<string, object?> arguments,
        string routingKey)
    {
        arguments["x-dead-letter-routing-key"] = routingKey;
        return arguments;
    }

    private async Task DeclareAsync(CancellationToken cancellationToken)
    {
        var connection = await owner.GetAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            ExchangeName,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await DeclareQueueAsync(channel, RequestedQueue, RequestedArguments(enrichment.Value.MaxAttempts), cancellationToken);
        await DeclareQueueAsync(channel, RetryQueue, RetryArguments(RetryDelayMilliseconds(rabbitMq.Value)), cancellationToken);
        await DeclareQueueAsync(channel, DeadLetterQueue, DeadLetterArguments(), cancellationToken);
        await DeclareQueueAsync(
            channel,
            FeaturesRequestedQueue,
            FeaturesRequestedArguments(enrichment.Value.MaxAttempts),
            cancellationToken);
        await DeclareQueueAsync(
            channel,
            FeaturesRetryQueue,
            FeaturesRetryArguments(RetryDelayMilliseconds(rabbitMq.Value)),
            cancellationToken);
        await DeclareQueueAsync(channel, FeaturesDeadLetterQueue, DeadLetterArguments(), cancellationToken);

        await channel.QueueBindAsync(RequestedQueue, ExchangeName, RequestedRoutingKey, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(RetryQueue, ExchangeName, RetryRoutingKey, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(DeadLetterQueue, ExchangeName, DeadLetterRoutingKey, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            FeaturesRequestedQueue,
            ExchangeName,
            EnrichmentRouting.FeaturesRequestedRoutingKey,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            FeaturesRetryQueue,
            ExchangeName,
            EnrichmentRouting.FeaturesRetryRoutingKey,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            FeaturesDeadLetterQueue,
            ExchangeName,
            EnrichmentRouting.FeaturesDeadLetterRoutingKey,
            cancellationToken: cancellationToken);
    }

    private static async Task DeclareQueueAsync(
        IChannel channel,
        string queue,
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: cancellationToken);
    }
}