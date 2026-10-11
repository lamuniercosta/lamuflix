using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.UnitTests.Pipeline;

[Collection(nameof(MetricsCollection))]
public sealed class EnrichmentMetricsTests
{
    private const string MeterName = "LamuFlix";
    private const string DurationInstrumentName = "lamuflix.enrichment.duration";
    private const string OutcomeInstrumentName = "lamuflix.enrichment.outcome";
    private const string ImportInstrumentName = "lamuflix.import.count";
    private const string ProviderTagKey = "provider";
    private const string OutcomeTagKey = "outcome";
    private const string FailureCategoryTagKey = "failure_category";
    private const string OmdbProviderValue = "omdb";
    private const string EnrichedOutcomeValue = "enriched";
    private const string NotFoundOutcomeValue = "not_found";
    private const string FailedOutcomeValue = "failed";
    private const string ProviderUnavailableCode = "provider_unavailable";
    private const string ForeignMeterName = "LamuFlix.Foreign.Meter";
    private const string ForeignInstrumentName = "lamuflix.foreign.instrument";
    private const string IgnoredInstrumentName = "lamuflix.probe.ignored";

    [Fact]
    public void TelemetryConstants_MetricTagAndOutcomeLiterals_MatchTheTicketText()
    {
        // assert
        TelemetryConstants.ActivitySourceName.ShouldBe(MeterName);
        TelemetryConstants.EnrichmentDurationMetricName.ShouldBe(DurationInstrumentName);
        TelemetryConstants.EnrichmentOutcomeMetricName.ShouldBe(OutcomeInstrumentName);
        TelemetryConstants.ImportCountMetricName.ShouldBe(ImportInstrumentName);
        TelemetryConstants.ProviderTagName.ShouldBe(ProviderTagKey);
        TelemetryConstants.OutcomeTagName.ShouldBe(OutcomeTagKey);
        TelemetryConstants.FailureCategoryTagName.ShouldBe(FailureCategoryTagKey);
        TelemetryConstants.EnrichedOutcome.ShouldBe(EnrichedOutcomeValue);
        TelemetryConstants.NotFoundOutcome.ShouldBe(NotFoundOutcomeValue);
        TelemetryConstants.FailedOutcome.ShouldBe(FailedOutcomeValue);
        TelemetryConstants.OmdbProviderName.ShouldBe(OmdbProviderValue);
    }

    [Fact]
    public void ApplicationMeter_PublishesExactlyTheThreeRuledInstruments()
    {
        // arrange
        _ = EnrichmentMetrics.EnrichmentDuration;
        using var capture = new ApplicationMeterCapture();

        // assert
        capture.PublishedInstruments.Count.ShouldBe(3);
        capture.PublishedInstruments.Keys.ShouldContain(DurationInstrumentName);
        capture.PublishedInstruments.Keys.ShouldContain(OutcomeInstrumentName);
        capture.PublishedInstruments.Keys.ShouldContain(ImportInstrumentName);
        capture.PublishedInstruments[DurationInstrumentName].Meter.Name.ShouldBe(MeterName);
        capture.PublishedInstruments[OutcomeInstrumentName].Meter.Name.ShouldBe(MeterName);
        capture.PublishedInstruments[ImportInstrumentName].Meter.Name.ShouldBe(MeterName);
    }

    [Fact]
    public void PublishedInstruments_CarryTheRuledTypesAndUnits()
    {
        // arrange
        _ = EnrichmentMetrics.EnrichmentDuration;
        using var capture = new ApplicationMeterCapture();

        // assert
        capture.PublishedInstruments[DurationInstrumentName]
            .ShouldBeOfType<Histogram<double>>()
            .Unit.ShouldBe("s");
        capture.PublishedInstruments[OutcomeInstrumentName]
            .ShouldBeOfType<Counter<long>>()
            .Unit.ShouldBeNull();
        capture.PublishedInstruments[ImportInstrumentName]
            .ShouldBeOfType<Counter<long>>()
            .Unit.ShouldBeNull();
    }

    [Fact]
    public void Capture_ObservesInstrumentsPublishedAfterStart_WithExactMeterAndInstrumentFilters()
    {
        // arrange
        using var capture = new ApplicationMeterCapture();
        using var applicationMeter = new Meter(MeterName);
        using var foreignMeter = new Meter(ForeignMeterName);
        var matching = applicationMeter.CreateCounter<long>(ImportInstrumentName);
        var ignored = applicationMeter.CreateCounter<long>(IgnoredInstrumentName);
        var wrongMeter = foreignMeter.CreateCounter<long>(ImportInstrumentName);
        var foreign = foreignMeter.CreateCounter<long>(ForeignInstrumentName);

        // act
        matching.Add(1);
        ignored.Add(1);
        wrongMeter.Add(1);
        foreign.Add(1);

        // assert
        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(ImportInstrumentName);
        measured.Value.ShouldBe(1L);
        capture.PublishedInstruments.Keys.ShouldContain(ImportInstrumentName);
        capture.PublishedInstruments.Keys.ShouldContain(IgnoredInstrumentName);
        capture.PublishedInstruments.Keys.ShouldNotContain(ForeignInstrumentName);
        capture.PublishedInstruments[ImportInstrumentName].Meter.Name.ShouldBe(MeterName);
    }

    [Fact]
    public void EnrichmentDuration_RecordCarriesOnlyTheProviderTag()
    {
        // arrange
        using var capture = new ApplicationMeterCapture();

        // act
        EnrichmentMetrics.EnrichmentDuration.Record(
            1.5,
            new KeyValuePair<string, object?>(TelemetryConstants.ProviderTagName, TelemetryConstants.OmdbProviderName));

        // assert
        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(DurationInstrumentName);
        measured.Value.ShouldBe(1.5);
        measured.Tags.Length.ShouldBe(1);
        measured.Tags[0].Key.ShouldBe(ProviderTagKey);
        measured.Tags[0].Value.ShouldBe(OmdbProviderValue);
    }

    [Fact]
    public void EnrichmentOutcome_FailedMeasurementCarriesOutcomeAndFailureCategoryTags()
    {
        // arrange
        using var capture = new ApplicationMeterCapture();

        // act
        EnrichmentMetrics.EnrichmentOutcome.Add(
            1,
            new KeyValuePair<string, object?>(TelemetryConstants.OutcomeTagName, TelemetryConstants.FailedOutcome),
            new KeyValuePair<string, object?>(TelemetryConstants.FailureCategoryTagName, EnrichmentFailureCategory.ProviderUnavailable.Code));

        // assert
        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(OutcomeInstrumentName);
        measured.Value.ShouldBe(1L);
        measured.Tags.Length.ShouldBe(2);
        measured.Tags[0].Key.ShouldBe(OutcomeTagKey);
        measured.Tags[0].Value.ShouldBe(FailedOutcomeValue);
        measured.Tags[1].Key.ShouldBe(FailureCategoryTagKey);
        measured.Tags[1].Value.ShouldBe(ProviderUnavailableCode);
    }

    [Fact]
    public void EnrichmentOutcome_EnrichedMeasurementCarriesOnlyTheOutcomeTag()
    {
        // arrange
        using var capture = new ApplicationMeterCapture();

        // act
        EnrichmentMetrics.EnrichmentOutcome.Add(
            1,
            new KeyValuePair<string, object?>(TelemetryConstants.OutcomeTagName, TelemetryConstants.EnrichedOutcome));

        // assert
        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(OutcomeInstrumentName);
        measured.Tags.Length.ShouldBe(1);
        measured.Tags[0].Key.ShouldBe(OutcomeTagKey);
        measured.Tags[0].Value.ShouldBe(EnrichedOutcomeValue);
    }

    [Fact]
    public void EnrichmentOutcome_NotFoundMeasurementCarriesOnlyTheOutcomeTag()
    {
        // arrange
        using var capture = new ApplicationMeterCapture();

        // act
        EnrichmentMetrics.EnrichmentOutcome.Add(
            1,
            new KeyValuePair<string, object?>(TelemetryConstants.OutcomeTagName, TelemetryConstants.NotFoundOutcome));

        // assert
        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(OutcomeInstrumentName);
        measured.Tags.Length.ShouldBe(1);
        measured.Tags[0].Key.ShouldBe(OutcomeTagKey);
        measured.Tags[0].Value.ShouldBe(NotFoundOutcomeValue);
    }

    [Fact]
    public void ImportCount_AddCarriesNoTags()
    {
        // arrange
        using var capture = new ApplicationMeterCapture();

        // act
        EnrichmentMetrics.ImportCount.Add(1);

        // assert
        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(ImportInstrumentName);
        measured.Value.ShouldBe(1L);
        measured.Tags.ShouldBeEmpty();
    }

    private sealed class ApplicationMeterCapture : IDisposable
    {
        private static readonly HashSet<string> ApplicationInstrumentNames = new(StringComparer.Ordinal)
        {
            DurationInstrumentName,
            OutcomeInstrumentName,
            ImportInstrumentName,
        };

        private readonly ConcurrentDictionary<string, Instrument> publishedInstruments = new(StringComparer.Ordinal);
        private readonly ConcurrentQueue<CapturedMeasurement> measurements = [];
        private readonly MeterListener listener;

        public ApplicationMeterCapture()
        {
            listener = new MeterListener();
            listener.SetMeasurementEventCallback<long>(
                (instrument, value, tags, _) => measurements.Enqueue(new(instrument.Name, value, tags.ToArray())));
            listener.SetMeasurementEventCallback<double>(
                (instrument, value, tags, _) => measurements.Enqueue(new(instrument.Name, value, tags.ToArray())));
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (!string.Equals(instrument.Meter.Name, MeterName, StringComparison.Ordinal))
                {
                    return;
                }

                publishedInstruments[instrument.Name] = instrument;
                if (ApplicationInstrumentNames.Contains(instrument.Name))
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.Start();
        }

        public IReadOnlyDictionary<string, Instrument> PublishedInstruments => publishedInstruments;

        public IReadOnlyList<CapturedMeasurement> Measurements => [.. measurements];

        public void Dispose() => listener.Dispose();
    }

    private sealed record CapturedMeasurement(string InstrumentName, object Value, KeyValuePair<string, object?>[] Tags);
}
