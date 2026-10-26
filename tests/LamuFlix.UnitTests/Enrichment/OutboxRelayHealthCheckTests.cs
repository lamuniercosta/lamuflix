using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Enrichment;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.UnitTests.Enrichment;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OutboxMetricsCollection
{
    public const string Name = "outbox-metrics";
}

[Collection(OutboxMetricsCollection.Name)]
public sealed class OutboxRelayHealthCheckTests
{
    private static readonly DateTimeOffset SampledAt = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Health_BeforeTheFirstSample_IsUnhealthy()
    {
        // arrange
        var state = new OutboxRelayState();

        // act
        var status = await CheckAsync(state);

        // assert
        status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Health_RunningWithoutASuccessfulPass_IsUnhealthy()
    {
        // arrange
        var state = new OutboxRelayState();
        state.MarkRunning();

        // act
        var status = await CheckAsync(state);

        // assert
        status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Health_StoppedWorker_IsUnhealthy()
    {
        // arrange
        var state = new OutboxRelayState();
        state.MarkRunning();
        state.RecordPassSucceeded(0, null, SampledAt);
        state.MarkStopped();

        // act
        var status = await CheckAsync(state);

        // assert
        status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Health_FailedPass_IsUnhealthy()
    {
        // arrange
        var state = new OutboxRelayState();
        state.MarkRunning();
        state.RecordPassSucceeded(1, SampledAt.AddMinutes(-1), SampledAt);
        state.RecordPassFailed(SampledAt);

        // act
        var status = await CheckAsync(state);

        // assert
        status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Health_SuccessfulEmptySample_IsHealthy()
    {
        // arrange
        var state = new OutboxRelayState();
        state.MarkRunning();
        state.RecordPassSucceeded(0, null, SampledAt);

        // act
        var status = await CheckAsync(state);

        // assert
        status.ShouldBe(HealthStatus.Healthy);
    }

    [Theory]
    [InlineData(-6, HealthStatus.Degraded)]
    [InlineData(-5, HealthStatus.Degraded)]
    [InlineData(-4, HealthStatus.Healthy)]
    public async Task Health_OldestPendingAgeAgainstTheWarning_IsRanked(int minutesBeforeSample, HealthStatus expected)
    {
        // arrange
        var state = new OutboxRelayState();
        state.MarkRunning();
        state.RecordPassSucceeded(1, SampledAt.AddMinutes(minutesBeforeSample), SampledAt);

        // act
        var status = await CheckAsync(state);

        // assert
        status.ShouldBe(expected);
    }

    [Fact]
    public async Task Health_FailedCleanup_IsDegraded()
    {
        // arrange
        var state = new OutboxRelayState();
        state.MarkRunning();
        state.RecordPassSucceeded(0, null, SampledAt);
        state.RecordCleanupFailed(SampledAt);

        // act
        var status = await CheckAsync(state);

        // assert
        status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task Health_UnhealthyTakesPrecedenceOverDegraded()
    {
        // arrange
        var state = new OutboxRelayState();
        state.MarkRunning();
        state.RecordPassSucceeded(1, SampledAt.AddHours(-1), SampledAt);
        state.RecordCleanupFailed(SampledAt);
        state.RecordPassFailed(SampledAt);

        // act
        var status = await CheckAsync(state);

        // assert
        status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task Health_LaterSuccessfulSampleClearsTheFailedPassFault()
    {
        // arrange
        var state = new OutboxRelayState();
        state.MarkRunning();
        state.RecordPassSucceeded(0, null, SampledAt);
        state.RecordPassFailed(SampledAt);

        // act
        state.RecordPassSucceeded(0, null, SampledAt);
        var status = await CheckAsync(state);

        // assert
        status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Health_LaterSuccessfulPassDoesNotClearTheCleanupFault()
    {
        // arrange
        var state = new OutboxRelayState();
        state.MarkRunning();
        state.RecordPassSucceeded(0, null, SampledAt);
        state.RecordCleanupFailed(SampledAt);
        state.RecordPassFailed(SampledAt);

        // act
        state.RecordPassSucceeded(0, null, SampledAt);
        var afterPassRecovery = await CheckAsync(state);
        state.RecordCleanupSucceeded(0, SampledAt);
        var afterCleanupRecovery = await CheckAsync(state);

        // assert
        afterPassRecovery.ShouldBe(HealthStatus.Degraded);
        afterCleanupRecovery.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Health_LaterSuccessfulCleanupDoesNotClearThePassFault()
    {
        // arrange
        var state = new OutboxRelayState();
        state.MarkRunning();
        state.RecordPassFailed(SampledAt);

        // act
        state.RecordCleanupSucceeded(0, SampledAt);
        var status = await CheckAsync(state);

        // assert
        status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public void Metrics_SuccessfulEmptySampleReportsZeroCountAndAge()
    {
        // arrange
        var state = new OutboxRelayState();
        using var collector = new OutboxMetricCollector();
        using var metrics = new OutboxRelayMetrics(state);
        metrics.ShouldNotBeNull();
        state.MarkRunning();
        state.RecordPassSucceeded(0, null, SampledAt);

        // act
        collector.CollectGauges();

        // assert
        Single(collector, TelemetryConstants.OutboxPendingMetricName).ShouldBe(0);
        Single(collector, TelemetryConstants.OutboxOldestPendingAgeMetricName).ShouldBe(0);
    }

    [Fact]
    public void Metrics_BacklogGaugesFollowTheSampledCountAndAge()
    {
        // arrange
        var state = new OutboxRelayState();
        using var collector = new OutboxMetricCollector();
        using var metrics = new OutboxRelayMetrics(state);
        metrics.ShouldNotBeNull();
        state.MarkRunning();
        state.RecordPassSucceeded(7, SampledAt.AddSeconds(-90), SampledAt);

        // act
        collector.CollectGauges();

        // assert
        Single(collector, TelemetryConstants.OutboxPendingMetricName).ShouldBe(7);
        Single(collector, TelemetryConstants.OutboxOldestPendingAgeMetricName).ShouldBe(90);
    }

    [Fact]
    public void Metrics_FailedSampleWithholdsTheBacklogGaugesUntilTheNextValidSample()
    {
        // arrange
        var state = new OutboxRelayState();
        using var collector = new OutboxMetricCollector();
        using var metrics = new OutboxRelayMetrics(state);
        metrics.ShouldNotBeNull();
        state.MarkRunning();
        state.RecordPassSucceeded(3, SampledAt.AddMinutes(-1), SampledAt);

        // act
        state.RecordPassFailed(SampledAt);
        collector.CollectGauges();
        var withheld = collector.Measurements;

        state.RecordPassSucceeded(1, null, SampledAt);
        collector.Clear();
        collector.CollectGauges();

        // assert
        withheld.ShouldBeEmpty();
        Single(collector, TelemetryConstants.OutboxPendingMetricName).ShouldBe(1);
        Single(collector, TelemetryConstants.OutboxOldestPendingAgeMetricName).ShouldBe(0);
    }

    [Fact]
    public void Metrics_CountersAggregatePublishedFailuresAndRetentionOutcomes()
    {
        // arrange
        var state = new OutboxRelayState();
        using var collector = new OutboxMetricCollector();
        using var metrics = new OutboxRelayMetrics(state);

        // act
        metrics.RecordPublished(3);
        metrics.RecordPublished(2);
        metrics.RecordPassFailure();
        metrics.RecordPassFailure();
        metrics.RecordRetentionDeleted(4);
        metrics.RecordRetentionDeleted(5);
        metrics.RecordRetentionFailure();

        // assert
        Total(collector, TelemetryConstants.OutboxPublishedMetricName).ShouldBe(5);
        Total(collector, TelemetryConstants.OutboxFailuresMetricName).ShouldBe(2);
        Total(collector, TelemetryConstants.OutboxRetentionDeletedMetricName).ShouldBe(9);
        Total(collector, TelemetryConstants.OutboxRetentionFailuresMetricName).ShouldBe(1);
    }

    [Fact]
    public void Metrics_ExportExactlyTheSixCentralizedNamesWithoutLabels()
    {
        // arrange
        var state = new OutboxRelayState();
        using var collector = new OutboxMetricCollector();
        using var metrics = new OutboxRelayMetrics(state);
        state.MarkRunning();
        state.RecordPassSucceeded(2, SampledAt.AddMinutes(-1), SampledAt);

        // act
        metrics.RecordPublished(2);
        metrics.RecordPassFailure();
        metrics.RecordRetentionDeleted(1);
        metrics.RecordRetentionFailure();
        collector.CollectGauges();

        // assert
        collector.Measurements.Select(measurement => measurement.Name).Distinct().Order(StringComparer.Ordinal)
            .ShouldBe(
                [
                    TelemetryConstants.OutboxFailuresMetricName,
                    TelemetryConstants.OutboxOldestPendingAgeMetricName,
                    TelemetryConstants.OutboxPendingMetricName,
                    TelemetryConstants.OutboxPublishedMetricName,
                    TelemetryConstants.OutboxRetentionDeletedMetricName,
                    TelemetryConstants.OutboxRetentionFailuresMetricName,
                ]);
        foreach (var measurement in collector.Measurements)
        {
            measurement.TagNames.ShouldBeEmpty();
        }
    }

    private static Task<HealthStatus> CheckAsync(OutboxRelayState state, OutboxRelayOptions? options = null)
    {
        var healthCheck = new OutboxRelayHealthCheck(state, Microsoft.Extensions.Options.Options.Create(options ?? new OutboxRelayOptions()));
        return RunAsync(healthCheck);
    }

    private static async Task<HealthStatus> RunAsync(OutboxRelayHealthCheck healthCheck)
    {
        var result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken);
        return result.Status;
    }

    private static double Single(OutboxMetricCollector collector, string name) =>
        collector.Measurements.Single(measurement => measurement.Name == name).Value;

    private static double Total(OutboxMetricCollector collector, string name) =>
        collector.Measurements.Where(measurement => measurement.Name == name).Sum(measurement => measurement.Value);

    private sealed record RecordedMeasurement(string Name, double Value, IReadOnlyList<string> TagNames);

    private sealed class OutboxMetricCollector : IDisposable
    {
        private readonly MeterListener listener = new();
        private readonly Lock gate = new();
        private readonly List<RecordedMeasurement> measurements = [];

        public OutboxMetricCollector()
        {
            listener.InstrumentPublished = static (instrument, instance) =>
            {
                if (string.Equals(instrument.Meter.Name, TelemetryConstants.ActivitySourceName, StringComparison.Ordinal)
                    && instrument.Name.StartsWith("lamuflix.outbox.", StringComparison.Ordinal))
                {
                    instance.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
                Record(instrument.Name, value, tags));
            listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
                Record(instrument.Name, value, tags));
            listener.Start();
        }

        public IReadOnlyList<RecordedMeasurement> Measurements
        {
            get
            {
                lock (gate)
                {
                    return [.. measurements];
                }
            }
        }

        public void CollectGauges() => listener.RecordObservableInstruments();

        public void Clear()
        {
            lock (gate)
            {
                measurements.Clear();
            }
        }

        public void Dispose() => listener.Dispose();

        private void Record(string name, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var tagNames = new List<string>(tags.Length);
            foreach (var tag in tags)
            {
                tagNames.Add(tag.Key);
            }

            lock (gate)
            {
                measurements.Add(new RecordedMeasurement(name, value, tagNames));
            }
        }
    }
}
