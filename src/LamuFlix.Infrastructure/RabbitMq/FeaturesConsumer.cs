using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class FeaturesConsumer(
    RabbitMqConnectionOwner owner,
    RabbitMqTopology topology,
    RabbitMqEnrichmentQueuePublisher publisher,
    IServiceScopeFactory scopes,
    IOptions<RabbitMqOptions> options,
    ILogger<FeaturesConsumer> logger,
    FeaturesConsumerLiveness liveness,
    TimeProvider timeProvider) : BackgroundService
{
    private const string FailureCategoryHeader = "x-lamuflix-failure-category";
    private const string FailureReasonHeader = "x-lamuflix-failure-reason";
    private const string FailureAttemptHeader = "x-lamuflix-failure-attempt";
    private const string NonRetryableReason = "non_retryable";
    private const string MaxAttemptsExhaustedReason = "max_attempts_exhausted";

    private static readonly ActivitySource Source = new(TelemetryConstants.ActivitySourceName);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly IReadOnlyDictionary<string, object?> NoHeaders =
        new Dictionary<string, object?>(StringComparer.Ordinal);

    private readonly ConsumerDrain drain = new();
    private IChannel? channel;
    private string? consumerTag;
    private CancellationToken hostShutdown;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await topology.EnsureDeclaredAsync(stoppingToken);

                var connection = await owner.GetAsync(stoppingToken);
                var active = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                channel = active;
                try
                {
                    await active.BasicQosAsync(0, options.Value.Prefetch, global: false, stoppingToken);

                    var consumer = new AsyncEventingBasicConsumer(active);
                    consumer.ReceivedAsync += (_, delivery) => drain.Track(HandleAsync(active, delivery, stoppingToken));
                    consumerTag = await active.BasicConsumeAsync(
                        RabbitMqTopology.FeaturesRequestedQueue,
                        autoAck: false,
                        consumer,
                        stoppingToken);
                    if (!TryMarkListening())
                    {
                        return;
                    }

                    await WaitForShutdownAsync(connection, active, stoppingToken);
                }
                finally
                {
                    ClearLiveness();
                    await ReleaseConsumerAsync(active);
                    channel = null;
                    await active.DisposeAsync();
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "The features consumer lost its RabbitMQ connection and will try to reconnect");
            }

            try
            {
                await Task.Delay(ReconnectDelay, timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private static async Task WaitForShutdownAsync(IConnection connection, IChannel active, CancellationToken stoppingToken)
    {
        var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task SignalShutdown(object? sender, ShutdownEventArgs args)
        {
            _ = sender;
            _ = args;
            shutdown.TrySetResult();
            return Task.CompletedTask;
        }

        active.ChannelShutdownAsync += SignalShutdown;
        connection.ConnectionShutdownAsync += SignalShutdown;
        try
        {
            await using (stoppingToken.Register(static state => { if (state is TaskCompletionSource tcs) tcs.TrySetResult(); }, shutdown))
            {
                await shutdown.Task;
            }
        }
        finally
        {
            active.ChannelShutdownAsync -= SignalShutdown;
            connection.ConnectionShutdownAsync -= SignalShutdown;
        }

        stoppingToken.ThrowIfCancellationRequested();
    }

    private bool TryMarkListening()
    {
        try
        {
            liveness.MarkListening();
            return true;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "The features consumer could not record worker liveness and will stop");
            return false;
        }
    }

    private void ClearLiveness()
    {
        try
        {
            liveness.MarkStopped();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The features consumer could not clear worker liveness");
        }
    }

    private async Task ReleaseConsumerAsync(IChannel active)
    {
        var tag = consumerTag;
        consumerTag = null;
        try
        {
            if (tag is null)
            {
                await drain.WhenIdleAsync(hostShutdown);
                return;
            }

            await drain.PauseAndDrainAsync(active, tag, hostShutdown);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The features consumer could not be paused during shutdown");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        hostShutdown = cancellationToken;
        await base.StopAsync(cancellationToken);
        if (ExecuteTask is { IsCompleted: true } && channel is { IsOpen: true })
        {
            await channel.CloseAsync(cancellationToken: CancellationToken.None);
        }
    }

    private async Task HandleAsync(
        IChannel consumerChannel,
        BasicDeliverEventArgs delivery,
        CancellationToken stoppingToken)
    {
        var request = Read(delivery.Body);
        if (request is null)
        {
            logger.LogWarning(
                "Feature message could not be read as a request and is dead-lettered redelivered {Redelivered}",
                delivery.Redelivered);
            await SettleMalformedAsync(consumerChannel, delivery);
            return;
        }

        using var activity = StartActivity(delivery, request);
        try
        {
            var outcome = await DispatchAsync(request, stoppingToken);
            await SettleAsync(activity, consumerChannel, delivery, request, outcome);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation(
                "Feature processing for {MovieId} attempt {Attempt} requeued because the host is stopping",
                request.MovieId.Value,
                request.Attempt);
            await consumerChannel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
        }
        catch (Exception exception)
        {
            MarkError(activity, exception);
            logger.LogError(
                "Feature processing for {MovieId} attempt {Attempt} failed unexpectedly with {ErrorType} and is dead-lettered",
                request.MovieId.Value,
                request.Attempt,
                exception.GetType().Name);
            await DeadLetterAsync(consumerChannel, delivery);
        }
    }

    private async Task<ProcessFeaturesOutcome> DispatchAsync(
        FeaturesRequested request,
        CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<
            ICommandHandler<ProcessFeaturesCommand, ProcessFeaturesOutcome>>();
        return await handler.HandleAsync(
            new ProcessFeaturesCommand(request.MovieId, request.Attempt),
            cancellationToken);
    }

    private async Task SettleAsync(
        Activity? activity,
        IChannel consumerChannel,
        BasicDeliverEventArgs delivery,
        FeaturesRequested request,
        ProcessFeaturesOutcome outcome)
    {
        var disposition = FeaturesRouting.Decide(outcome);
        LogOutcome(activity, outcome, request);

        if (disposition.Action == FeaturesRouting.Ack)
        {
            await AckAsync(consumerChannel, delivery);
            return;
        }

        var next = new FeaturesRequested(
            request.MovieId,
            disposition.NextAttempt ?? request.Attempt);
        var headers = disposition.RoutingKey == EnrichmentRouting.FeaturesDeadLetterRoutingKey
                      && outcome is ProcessFeaturesOutcome.Failed failed
            ? FailureHeaders(failed, request)
            : NoHeaders;
        try
        {
            await publisher.PublishAsync(next, disposition.RoutingKey, headers, CancellationToken.None);
            await AckAsync(consumerChannel, delivery);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Feature publication for {MovieId} attempt {Attempt} failed and the original is requeued",
                request.MovieId.Value,
                request.Attempt);
            await RequeueAsync(consumerChannel, delivery);
        }
    }

    private async Task SettleMalformedAsync(
        IChannel consumerChannel,
        BasicDeliverEventArgs delivery)
    {
        try
        {
            await publisher.PublishRawAsync(
                delivery.Body,
                EnrichmentRouting.FeaturesDeadLetterRoutingKey,
                MalformedHeaders(),
                CancellationToken.None);
            await AckAsync(consumerChannel, delivery);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Feature malformed payload publication failed and the original is requeued");
            await RequeueAsync(consumerChannel, delivery);
        }
    }

    private static IReadOnlyDictionary<string, object?> MalformedHeaders() =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [FailureCategoryHeader] = EnrichmentFailureCategory.InvalidResponse.Code,
            [FailureReasonHeader] = NonRetryableReason,
            [FailureAttemptHeader] = 0,
        };

    private static IReadOnlyDictionary<string, object?> FailureHeaders(
        ProcessFeaturesOutcome.Failed failed,
        FeaturesRequested request) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [FailureCategoryHeader] = SafeCategoryCode(failed.Category),
            [FailureReasonHeader] = failed.Category.IsRetryable
                ? MaxAttemptsExhaustedReason
                : NonRetryableReason,
            [FailureAttemptHeader] = request.Attempt,
        };

    private static string SafeCategoryCode(EnrichmentFailureCategory category) =>
        ReferenceEquals(category, EnrichmentFailureCategory.ProviderUnavailable)
        || ReferenceEquals(category, EnrichmentFailureCategory.RateLimited)
        || ReferenceEquals(category, EnrichmentFailureCategory.InvalidResponse)
        || ReferenceEquals(category, EnrichmentFailureCategory.Unknown)
            ? category.Code
            : EnrichmentFailureCategory.Unknown.Code;

    private static ValueTask AckAsync(IChannel consumerChannel, BasicDeliverEventArgs delivery) =>
        consumerChannel.BasicAckAsync(delivery.DeliveryTag, multiple: false, CancellationToken.None);

    private static ValueTask DeadLetterAsync(IChannel consumerChannel, BasicDeliverEventArgs delivery) =>
        consumerChannel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);

    private static ValueTask RequeueAsync(IChannel consumerChannel, BasicDeliverEventArgs delivery) =>
        consumerChannel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);

    private void LogOutcome(Activity? activity, ProcessFeaturesOutcome outcome, FeaturesRequested request)
    {
        if (outcome is not ProcessFeaturesOutcome.Completed completed)
        {
            return;
        }

        activity?.SetTag(TelemetryConstants.FeaturesOutcome, completed.Generated ? "generated" : "no_input");
        logger.LogInformation(
            "Feature processing for {MovieId} attempt {Attempt} completed {Outcome}",
            request.MovieId.Value,
            request.Attempt,
            completed.Generated ? "generated" : "with no input");
    }

    private static Activity? StartActivity(BasicDeliverEventArgs delivery, FeaturesRequested request)
    {
        var parent = TraceContextCarrier.ExtractContext(delivery.BasicProperties.Headers);
        var links = delivery.Redelivered
                    ? new List<ActivityLink> { new(parent) }
                    : [];
        var activity = Source.StartActivity(
                    TelemetryConstants.FeaturesProcess,
                    ActivityKind.Consumer,
                    parent,
                    tags: null,
                    links);
        activity?.SetTag(TelemetryConstants.MovieId, request.MovieId.Value);
        activity?.SetTag(TelemetryConstants.MessagingDeliveryCount, delivery.Redelivered ? 1 : 0);
        return activity;
    }

    private static void MarkError(Activity? activity, Exception exception)
    {
        activity?.SetStatus(ActivityStatusCode.Error);
        activity?.SetTag(TelemetryConstants.ErrorType, exception.GetType().Name);
    }

    private static FeaturesRequested? Read(ReadOnlyMemory<byte> body)
    {
        try
        {
            return JsonSerializer.Deserialize<FeaturesRequested>(body.Span, SerializerOptions) is { MovieId: not null } read
                ? read
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
