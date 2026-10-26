using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Enrichment;

internal enum OutboxRelayPassResult
{
    Succeeded = 0,
    Failed = 1,
    Shutdown = 2,
}

internal enum OutboxRelayCleanupResult
{
    Succeeded = 0,
    Failed = 1,
    Shutdown = 2,
}

internal readonly record struct OutboxRelayCleanupCompletion(OutboxRelayCleanupResult Result, int DeletedCount);

internal readonly record struct OutboxRelayBacklogSample(long PendingCount, DateTimeOffset? OldestPendingCreatedAt);

public partial class OutboxRelayHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxRelayOptions> options,
    TimeProvider timeProvider,
    IEnrichmentQueue queue,
    OutboxRelayState state,
    OutboxRelayMetrics metrics,
    ILogger<OutboxRelayHostedService> logger) : BackgroundService
{
    private const string ReadCategory = "read";
    private const string SampleCategory = "sample";
    private const string DeserializeCategory = "deserialize";
    private const string PublishCategory = "publish";
    private const string PublishTimeoutCategory = "publish-timeout";
    private const string MarkerSaveCategory = "marker-save";
    private const string PassCategory = "pass";
    private const string CleanupCategory = "cleanup";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        state.MarkRunning();
        try
        {
            var consecutiveFailures = 0;
            var nextCleanupDueAt = timeProvider.GetUtcNow();
            while (!stoppingToken.IsCancellationRequested)
            {
                var outcome = await RunPassAsync(stoppingToken);
                if (outcome == OutboxRelayPassResult.Shutdown)
                {
                    break;
                }

                var nextDue = await RunCleanupIfDueAsync(nextCleanupDueAt, stoppingToken);
                if (nextDue is null)
                {
                    break;
                }

                nextCleanupDueAt = nextDue.Value;
                var delay = NextDelay(outcome, ref consecutiveFailures);
                if (!await DelayAsync(delay, stoppingToken))
                {
                    break;
                }
            }
        }
        finally
        {
            state.MarkStopped();
        }
    }

    private async Task<DateTimeOffset?> RunCleanupIfDueAsync(
        DateTimeOffset nextCleanupDueAt,
        CancellationToken stoppingToken)
    {
        if (timeProvider.GetUtcNow() < nextCleanupDueAt)
        {
            return nextCleanupDueAt;
        }

        var cleanup = await RunCleanupAsync(stoppingToken);
        if (cleanup.Result == OutboxRelayCleanupResult.Shutdown)
        {
            return null;
        }

        if (cleanup.Result == OutboxRelayCleanupResult.Succeeded)
        {
            state.RecordCleanupSucceeded(cleanup.DeletedCount, timeProvider.GetUtcNow());
            metrics.RecordRetentionDeleted(cleanup.DeletedCount);
        }
        else
        {
            state.RecordCleanupFailed(timeProvider.GetUtcNow());
            metrics.RecordRetentionFailure();
        }

        return timeProvider.GetUtcNow() + options.Value.CleanupInterval;
    }

    private TimeSpan NextDelay(OutboxRelayPassResult outcome, ref int consecutiveFailures)
    {
        if (outcome == OutboxRelayPassResult.Succeeded)
        {
            consecutiveFailures = 0;
            return options.Value.PollInterval;
        }

        consecutiveFailures++;
        return FailureDelay(consecutiveFailures);
    }

    private async Task<bool> DelayAsync(TimeSpan delay, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(delay, timeProvider, stoppingToken);
            return true;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }

    internal async Task<OutboxRelayPassResult> RunPassAsync(CancellationToken stoppingToken)
    {
        var publishedCount = 0;
        var (outcome, backlog) = await ExecutePassAsync(stoppingToken, () => publishedCount++);

        if (publishedCount > 0)
        {
            metrics.RecordPublished(publishedCount);
        }

        if (outcome == OutboxRelayPassResult.Failed)
        {
            metrics.RecordPassFailure();
            state.RecordPassFailed(timeProvider.GetUtcNow());
        }
        else if (outcome == OutboxRelayPassResult.Succeeded && backlog is { } sample)
        {
            state.RecordPassSucceeded(sample.PendingCount, sample.OldestPendingCreatedAt, timeProvider.GetUtcNow());
        }

        return outcome;
    }

    internal virtual async Task<OutboxRelayBacklogSample> SampleBacklogAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();
        long pendingCount = await db.OutboxMessages
            .CountAsync(message => message.ProcessedAt == null, cancellationToken);
        DateTimeOffset? oldestCreatedAt = pendingCount == 0
            ? null
            : await db.OutboxMessages
                .Where(message => message.ProcessedAt == null)
                .MinAsync(message => (DateTimeOffset?)message.CreatedAt, cancellationToken);

        return new OutboxRelayBacklogSample(pendingCount, oldestCreatedAt);
    }

    private async Task<(OutboxRelayPassResult Outcome, OutboxRelayBacklogSample? Backlog)> ExecutePassAsync(
        CancellationToken stoppingToken,
        Action onRowPublished)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();

            IReadOnlyList<OutboxMessageRecord> rows;
            try
            {
                rows = await ReadPendingRowsAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return (OutboxRelayPassResult.Shutdown, null);
            }
            catch (Exception exception)
            {
                Log.PassFailure(logger, ReadCategory, ExceptionTypeName(exception));
                return (OutboxRelayPassResult.Failed, null);
            }

            foreach (var row in rows)
            {
                var outcome = await TryRelayRowAsync(scope.ServiceProvider, row, stoppingToken);
                if (outcome == OutboxRelayPassResult.Failed)
                {
                    return (OutboxRelayPassResult.Failed, null);
                }

                if (outcome == OutboxRelayPassResult.Shutdown)
                {
                    return (OutboxRelayPassResult.Shutdown, null);
                }

                onRowPublished();
            }

            try
            {
                return (OutboxRelayPassResult.Succeeded, await SampleBacklogAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return (OutboxRelayPassResult.Shutdown, null);
            }
            catch (Exception exception)
            {
                Log.PassFailure(logger, SampleCategory, ExceptionTypeName(exception));
                return (OutboxRelayPassResult.Failed, null);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return (OutboxRelayPassResult.Shutdown, null);
        }
        catch (Exception exception)
        {
            Log.PassFailure(logger, PassCategory, ExceptionTypeName(exception));
            return (OutboxRelayPassResult.Failed, null);
        }
    }

    internal virtual async Task<IReadOnlyList<OutboxMessageRecord>> ReadPendingRowsAsync(
        IServiceProvider scopedProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopedProvider);

        var db = scopedProvider.GetRequiredService<LamuFlixDbContext>();
        return await db.OutboxMessages
            .Where(message => message.ProcessedAt == null)
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);
    }

    internal virtual async Task SaveMarkerAsync(
        IServiceProvider scopedProvider,
        OutboxMessageRecord row,
        DateTimeOffset processedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopedProvider);
        ArgumentNullException.ThrowIfNull(row);

        var db = scopedProvider.GetRequiredService<LamuFlixDbContext>();
        row.ProcessedAt = processedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    internal async Task<OutboxRelayCleanupCompletion> RunCleanupAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var cutoff = timeProvider.GetUtcNow() - options.Value.ProcessedRetention;
            var rowIds = await ReadExpiredProcessedRowIdsAsync(scope.ServiceProvider, cutoff, stoppingToken);
            var deleted = await DeleteExpiredProcessedRowsAsync(
                scope.ServiceProvider,
                rowIds,
                cutoff,
                stoppingToken);

            return new OutboxRelayCleanupCompletion(OutboxRelayCleanupResult.Succeeded, deleted);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return new OutboxRelayCleanupCompletion(OutboxRelayCleanupResult.Shutdown, 0);
        }
        catch (Exception exception)
        {
            Log.CleanupFailure(logger, CleanupCategory, ExceptionTypeName(exception));
            return new OutboxRelayCleanupCompletion(OutboxRelayCleanupResult.Failed, 0);
        }
    }

    internal virtual async Task<IReadOnlyList<long>> ReadExpiredProcessedRowIdsAsync(
        IServiceProvider scopedProvider,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopedProvider);

        var db = scopedProvider.GetRequiredService<LamuFlixDbContext>();
        return await db.OutboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedAt != null && message.ProcessedAt < cutoff)
            .OrderBy(message => message.ProcessedAt)
            .ThenBy(message => message.Id)
            .Take(options.Value.CleanupBatchSize)
            .Select(message => message.Id)
            .ToListAsync(cancellationToken);
    }

    internal virtual async Task<int> DeleteExpiredProcessedRowsAsync(
        IServiceProvider scopedProvider,
        IReadOnlyList<long> rowIds,
        DateTimeOffset cutoff,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scopedProvider);
        ArgumentNullException.ThrowIfNull(rowIds);

        if (rowIds.Count == 0)
        {
            return 0;
        }

        var db = scopedProvider.GetRequiredService<LamuFlixDbContext>();
        return await db.OutboxMessages
            .Where(message => rowIds.Contains(message.Id)
                              && message.ProcessedAt != null
                              && message.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<OutboxRelayPassResult?> TryRelayRowAsync(
        IServiceProvider scopedProvider,
        OutboxMessageRecord row,
        CancellationToken stoppingToken)
    {
        EnrichmentRequested request;
        try
        {
            request = DeserializePayload(row.Payload);
        }
        catch (Exception exception)
        {
            Log.RowFailure(logger, row.Id, DeserializeCategory, ExceptionTypeName(exception));
            return OutboxRelayPassResult.Failed;
        }

        try
        {
            await PublishAsync(request, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return OutboxRelayPassResult.Shutdown;
        }
        catch (OperationCanceledException exception)
        {
            Log.RowFailure(logger, row.Id, PublishTimeoutCategory, ExceptionTypeName(exception));
            return OutboxRelayPassResult.Failed;
        }
        catch (Exception exception)
        {
            Log.RowFailure(logger, row.Id, PublishCategory, ExceptionTypeName(exception));
            return OutboxRelayPassResult.Failed;
        }

        try
        {
            await SaveMarkerAsync(scopedProvider, row, timeProvider.GetUtcNow(), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return OutboxRelayPassResult.Shutdown;
        }
        catch (Exception exception)
        {
            Log.RowFailure(logger, row.Id, MarkerSaveCategory, ExceptionTypeName(exception));
            return OutboxRelayPassResult.Failed;
        }

        return null;
    }

    private async Task PublishAsync(EnrichmentRequested request, CancellationToken stoppingToken)
    {
        using var timeout = new CancellationTokenSource(options.Value.PublishTimeout, timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, timeout.Token);
        await queue.EnqueueAsync(request, linked.Token);
    }

    private TimeSpan FailureDelay(int consecutiveFailures)
    {
        var minimum = options.Value.FailureBackoffMin;
        var maximum = options.Value.FailureBackoffMax;
        var exponent = Math.Min(Math.Max(consecutiveFailures - 1, 0), 62);
        var scaled = minimum.Ticks * Math.Pow(2, exponent);

        return scaled >= maximum.Ticks ? maximum : TimeSpan.FromTicks((long)scaled);
    }

    private static EnrichmentRequested DeserializePayload(byte[] payload)
    {
        var request = JsonSerializer.Deserialize<EnrichmentRequested>(payload, SerializerOptions);
        return request ?? throw new JsonException("The stored outbox payload was empty.");
    }

    private static string ExceptionTypeName(Exception exception) => exception.GetType().Name;

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Error,
            Message = "Outbox relay pass failed. Category {Category}. Exception type {ExceptionType}.")]
        public static partial void PassFailure(ILogger logger, string category, string exceptionType);

        [LoggerMessage(
            EventId = 2,
            Level = LogLevel.Error,
            Message = "Outbox relay row {RowId} failed. Category {Category}. Exception type {ExceptionType}.")]
        public static partial void RowFailure(ILogger logger, long rowId, string category, string exceptionType);

        [LoggerMessage(
            EventId = 3,
            Level = LogLevel.Error,
            Message = "Outbox relay cleanup failed. Category {Category}. Exception type {ExceptionType}.")]
        public static partial void CleanupFailure(ILogger logger, string category, string exceptionType);
    }
}
