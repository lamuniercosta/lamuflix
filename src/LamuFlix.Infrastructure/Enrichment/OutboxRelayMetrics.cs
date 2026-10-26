using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.Infrastructure.Enrichment;

public sealed class OutboxRelayMetrics : IDisposable
{
    private static readonly Measurement<long>[] NoPendingMeasurements = [];
    private static readonly Measurement<double>[] NoAgeMeasurements = [];

    private readonly Meter meter;
    private readonly Counter<long> published;
    private readonly Counter<long> failures;
    private readonly Counter<long> retentionDeleted;
    private readonly Counter<long> retentionFailures;

    public OutboxRelayMetrics(OutboxRelayState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        meter = new Meter(TelemetryConstants.ActivitySourceName);
        meter.CreateObservableGauge(
            TelemetryConstants.OutboxPendingMetricName,
            () => ObservePending(state));
        meter.CreateObservableGauge(
            TelemetryConstants.OutboxOldestPendingAgeMetricName,
            () => ObserveOldestPendingAge(state),
            TelemetryConstants.EnrichmentDurationUnit);
        published = meter.CreateCounter<long>(TelemetryConstants.OutboxPublishedMetricName);
        failures = meter.CreateCounter<long>(TelemetryConstants.OutboxFailuresMetricName);
        retentionDeleted = meter.CreateCounter<long>(TelemetryConstants.OutboxRetentionDeletedMetricName);
        retentionFailures = meter.CreateCounter<long>(TelemetryConstants.OutboxRetentionFailuresMetricName);
    }

    public void RecordPublished(long confirmedAndMarkedCount) => published.Add(confirmedAndMarkedCount);

    public void RecordPassFailure() => failures.Add(1);

    public void RecordRetentionDeleted(int deletedCount) => retentionDeleted.Add(deletedCount);

    public void RecordRetentionFailure() => retentionFailures.Add(1);

    public void Dispose() => meter.Dispose();

    private static IEnumerable<Measurement<long>> ObservePending(OutboxRelayState state) =>
        CurrentBacklog(state) is { } backlog
            ? [new Measurement<long>(backlog.PendingCount)]
            : NoPendingMeasurements;

    private static IEnumerable<Measurement<double>> ObserveOldestPendingAge(OutboxRelayState state)
    {
        if (CurrentBacklog(state) is not { } backlog)
        {
            return NoAgeMeasurements;
        }

        var age = backlog.OldestPendingCreatedAt is { } oldest
            ? Math.Max(0, (backlog.SampledAt - oldest).TotalSeconds)
            : 0;

        return [new Measurement<double>(age)];
    }

    private static OutboxRelayBacklogSnapshot? CurrentBacklog(OutboxRelayState state)
    {
        var snapshot = state.Current;
        return snapshot.LastPass is { Outcome: OutboxRelayPassOutcome.Succeeded } ? snapshot.Backlog : null;
    }
}
