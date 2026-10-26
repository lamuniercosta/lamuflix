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

public partial class OutboxRelayHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxRelayOptions> options,
    TimeProvider timeProvider,
    IEnrichmentQueue queue,
    OutboxRelayState state,
    ILogger<OutboxRelayHostedService> logger) : BackgroundService
{
    private const string ReadCategory = "read";
    private const string DeserializeCategory = "deserialize";
    private const string PublishCategory = "publish";
    private const string PublishTimeoutCategory = "publish-timeout";
    private const string MarkerSaveCategory = "marker-save";
    private const string PassCategory = "pass";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        state.MarkRunning();
        try
        {
            var consecutiveFailures = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                var outcome = await RunPassAsync(stoppingToken);
                if (outcome == OutboxRelayPassResult.Shutdown)
                {
                    break;
                }

                TimeSpan delay;
                if (outcome == OutboxRelayPassResult.Succeeded)
                {
                    consecutiveFailures = 0;
                    delay = options.Value.PollInterval;
                }
                else
                {
                    consecutiveFailures++;
                    delay = FailureDelay(consecutiveFailures);
                }

                try
                {
                    await Task.Delay(delay, timeProvider, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
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

    internal async Task<OutboxRelayPassResult> RunPassAsync(CancellationToken stoppingToken)
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
                return OutboxRelayPassResult.Shutdown;
            }
            catch (Exception exception)
            {
                Log.PassFailure(logger, ReadCategory, ExceptionTypeName(exception));
                return OutboxRelayPassResult.Failed;
            }

            foreach (var row in rows)
            {
                var outcome = await TryRelayRowAsync(scope.ServiceProvider, row, stoppingToken);
                if (outcome is { } failed)
                {
                    return failed;
                }
            }

            return OutboxRelayPassResult.Succeeded;
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return OutboxRelayPassResult.Shutdown;
        }
        catch (Exception exception)
        {
            Log.PassFailure(logger, PassCategory, ExceptionTypeName(exception));
            return OutboxRelayPassResult.Failed;
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
    }
}
