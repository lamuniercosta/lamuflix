using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

public sealed class EnrichmentConsumer(
    RabbitMqConnectionOwner owner,
    RabbitMqTopology topology,
    RabbitMqEnrichmentQueuePublisher publisher,
    IServiceScopeFactory scopes,
    IOptions<RabbitMqOptions> options,
    ILogger<EnrichmentConsumer> logger,
    IWorkerLiveness liveness) : BackgroundService
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
                        RabbitMqTopology.RequestedQueue,
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
                logger.LogWarning(exception, "The enrichment consumer lost its RabbitMQ connection and will try to reconnect");
            }

            try
            {
                await Task.Delay(ReconnectDelay, stoppingToken);
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
                "The enrichment consumer could not record worker liveness and will stop");
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
            logger.LogWarning(exception, "The enrichment consumer could not clear worker liveness");
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
            logger.LogWarning(exception, "The enrichment consumer could not be paused during shutdown");
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
                "Enrichment message could not be read as a request and is dead-lettered redelivered {Redelivered}",
                delivery.Redelivered);
            await DeadLetterAsync(consumerChannel, delivery);
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
                "Enrichment for {MovieId} attempt {Attempt} requeued because the host is stopping",
                request.MovieId.Value,
                request.Attempt);
            await consumerChannel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, CancellationToken.None);
        }
        catch (Exception exception)
        {
            MarkError(activity, exception);
            logger.LogError(
                exception,
                "Enrichment for {MovieId} attempt {Attempt} failed unexpectedly and is dead-lettered",
                request.MovieId.Value,
                request.Attempt);
            await DeadLetterAsync(consumerChannel, delivery);
        }
    }

    private async Task<ProcessEnrichmentOutcome> DispatchAsync(
        EnrichmentRequested request,
        CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<
            ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>>();
        return await handler.HandleAsync(
            new ProcessEnrichmentCommand(request.MovieId, request.Attempt),
            cancellationToken);
    }

    private async Task SettleAsync(
        Activity? activity,
        IChannel consumerChannel,
        BasicDeliverEventArgs delivery,
        EnrichmentRequested request,
        ProcessEnrichmentOutcome outcome)
    {
        var disposition = EnrichmentRouting.Decide(outcome);
        LogOutcome(activity, outcome, request);

        if (disposition.Action == EnrichmentRouting.Ack)
        {
            await AckAsync(consumerChannel, delivery);
            return;
        }

        var next = new EnrichmentRequested(
            request.MovieId,
            disposition.NextAttempt ?? request.Attempt);
        var headers = disposition.Action == RabbitMqTopology.DeadLetterRoutingKey
                      && outcome is ProcessEnrichmentOutcome.Failed failed
            ? FailureHeaders(failed, request)
            : NoHeaders;
        await publisher.PublishAsync(next, disposition.RoutingKey, headers, CancellationToken.None);
        await AckAsync(consumerChannel, delivery);
    }

    private static IReadOnlyDictionary<string, object?> FailureHeaders(
        ProcessEnrichmentOutcome.Failed failed,
        EnrichmentRequested request) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [FailureCategoryHeader] = failed.Category.Code,
            [FailureReasonHeader] = failed.Category.IsRetryable
                ? MaxAttemptsExhaustedReason
                : NonRetryableReason,
            [FailureAttemptHeader] = request.Attempt,
        };

    private static ValueTask AckAsync(IChannel consumerChannel, BasicDeliverEventArgs delivery) =>
            consumerChannel.BasicAckAsync(delivery.DeliveryTag, multiple: false, CancellationToken.None);

    private static ValueTask DeadLetterAsync(IChannel consumerChannel, BasicDeliverEventArgs delivery) =>
        consumerChannel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);

    private void LogOutcome(Activity? activity, ProcessEnrichmentOutcome outcome, EnrichmentRequested request)
    {
        if (outcome is not ProcessEnrichmentOutcome.Completed completed)
        {
            return;
        }

        if (completed.Claimed)
        {
            activity?.SetTag(TelemetryConstants.EnrichmentOutcome, "completed");
            logger.LogInformation(
                "Enrichment for {MovieId} attempt {Attempt} completed",
                request.MovieId.Value,
                request.Attempt);
            return;
        }

        activity?.SetTag(TelemetryConstants.EnrichmentOutcome, "skipped");
        logger.LogInformation(
            "Enrichment for {MovieId} attempt {Attempt} skipped because the claim was refused",
            request.MovieId.Value,
            request.Attempt);
    }

    private static Activity? StartActivity(BasicDeliverEventArgs delivery, EnrichmentRequested request)
    {
        var parent = TraceContextCarrier.ExtractContext(delivery.BasicProperties.Headers);
        var links = delivery.Redelivered
                    ? new List<ActivityLink> { new(parent) }
                    : [];
        var activity = Source.StartActivity(
                    TelemetryConstants.EnrichmentProcess,
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

    private static EnrichmentRequested? Read(ReadOnlyMemory<byte> body)
    {
        try
        {
            return JsonSerializer.Deserialize<EnrichmentRequested>(body.Span, SerializerOptions) is { MovieId: not null } read
                ? read
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

