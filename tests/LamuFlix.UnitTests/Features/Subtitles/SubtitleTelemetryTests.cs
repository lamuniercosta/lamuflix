using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleTelemetryTests
{
    [Fact]
    public void StartPrepare_StartsAnActivityNamedForThePrepareOperation()
    {
        // arrange
        using var listener = ListeningListener();
        ActivitySource.AddActivityListener(listener);

        // act
        using var activity = SubtitleTelemetry.StartPrepare();

        // assert
        activity.ShouldNotBeNull();
        activity.OperationName.ShouldBe(TelemetryConstants.SubtitlePrepare);
    }

    [Fact]
    public void StartServeAndLookup_UseTheFunctionalityNamedOperations()
    {
        // arrange
        using var listener = ListeningListener();
        ActivitySource.AddActivityListener(listener);

        // act
        using var serve = SubtitleTelemetry.StartServe();
        using var lookup = SubtitleTelemetry.StartProviderLookup();

        // assert
        serve.ShouldNotBeNull().OperationName.ShouldBe(TelemetryConstants.SubtitleServe);
        lookup.ShouldNotBeNull().OperationName.ShouldBe(TelemetryConstants.SubtitleProviderLookup);
    }

    [Fact]
    public void Complete_TagsTheMovieIdAndOutcome()
    {
        // arrange
        using var listener = ListeningListener();
        ActivitySource.AddActivityListener(listener);
        using var activity = SubtitleTelemetry.StartPrepare();

        // act
        SubtitleTelemetry.Complete(activity, new MovieId(7), TelemetryConstants.SubtitleSavedOutcome, TimeSpan.FromMilliseconds(5));

        // assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(TelemetryConstants.MovieId).ShouldBe(7);
        activity.GetTagItem(TelemetryConstants.OutcomeTagName).ShouldBe(TelemetryConstants.SubtitleSavedOutcome);
    }

    [Fact]
    public void Fail_TagsTheFailureCategoryAndMarksTheActivityAsAnError()
    {
        // arrange
        using var listener = ListeningListener();
        ActivitySource.AddActivityListener(listener);
        using var activity = SubtitleTelemetry.StartPrepare();

        // act
        SubtitleTelemetry.Fail(activity, new MovieId(7), new NotFoundException());

        // assert
        activity.ShouldNotBeNull();
        activity.GetTagItem(TelemetryConstants.MovieId).ShouldBe(7);
        activity.GetTagItem(TelemetryConstants.OutcomeTagName).ShouldBe(TelemetryConstants.NotFoundOutcome);
        activity.GetTagItem(TelemetryConstants.FailureCategoryTagName).ShouldBe(TelemetryConstants.NotFoundOutcome);
        activity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Fact]
    public void Complete_RecordsOutcomeAndDurationMeasurements()
    {
        // arrange
        using var measurements = new MeasurementRecorder();

        // act
        SubtitleTelemetry.Complete(null, new MovieId(7), "sentinel-outcome", TimeSpan.FromMilliseconds(5));

        // assert
        measurements.Recorded.ShouldContain((TelemetryConstants.SubtitleOutcomeMetricName, "sentinel-outcome"));
        measurements.Recorded.ShouldContain((TelemetryConstants.SubtitleDurationMetricName, null));
    }

    [Fact]
    public void Fail_RecordsTheFailureOutcomeMeasurement()
    {
        // arrange
        using var measurements = new MeasurementRecorder();

        // act
        SubtitleTelemetry.Fail(null, new MovieId(7), new InvalidOperationException("sentinel failure"));

        // assert
        measurements.Recorded.ShouldContain((TelemetryConstants.SubtitleOutcomeMetricName, TelemetryConstants.FailedOutcome));
    }

    [Fact]
    public void Complete_WithNullActivity_StillRecordsTheOutcomeMetric()
    {
        // act
        var act = () => SubtitleTelemetry.Complete(null, new MovieId(7), TelemetryConstants.SubtitleNoMatchOutcome, TimeSpan.Zero);

        // assert
        Should.NotThrow(act);
    }

    [Fact]
    public void Complete_WithNullMovieId_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleTelemetry.Complete(null, null!, TelemetryConstants.SubtitleNoMatchOutcome, TimeSpan.Zero);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Fail_WithNullException_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleTelemetry.Fail(null, new MovieId(7), null!);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void Complete_WithBlankOutcome_ThrowsArgumentNullException()
    {
        // act
        var act = () => SubtitleTelemetry.Complete(null, new MovieId(7), "  ", TimeSpan.Zero);

        // assert
        Should.Throw<ArgumentException>(act);
    }

    private static ActivityListener ListeningListener() =>
        new()
        {
            ShouldListenTo = source => source.Name == TelemetryConstants.ActivitySourceName,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
        };

    private sealed class MeasurementRecorder : IDisposable
    {
        private readonly MeterListener listener = new();

        internal MeasurementRecorder()
        {
            listener.InstrumentPublished = (instrument, publisher) =>
            {
                if (instrument.Meter.Name == TelemetryConstants.ActivitySourceName)
                {
                    publisher.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>(RecordOutcome);
            listener.SetMeasurementEventCallback<double>(RecordDuration);
            listener.Start();
        }

        public List<(string Instrument, string? Outcome)> Recorded { get; } = [];

        public void Dispose() => listener.Dispose();

        private void RecordOutcome(Instrument instrument, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            if (instrument.Name == TelemetryConstants.SubtitleOutcomeMetricName)
            {
                Recorded.Add((instrument.Name, OutcomeOf(tags)));
            }
        }

        private void RecordDuration(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            if (instrument.Name == TelemetryConstants.SubtitleDurationMetricName)
            {
                Recorded.Add((instrument.Name, null));
            }
        }

        private static string? OutcomeOf(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            foreach (var tag in tags.ToArray())
            {
                if (tag.Key == TelemetryConstants.OutcomeTagName)
                {
                    return tag.Value?.ToString();
                }
            }

            return null;
        }
    }
}
