using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(MetadataProviderCollection.Name)]
public sealed class MetadataProviderTelemetryTests(MetadataProviderProbe probe)
    : IClassFixture<MetadataProviderProbe>, IAsyncLifetime
{
    private const string FoundBody = """
        {
          "Response": "True",
          "Title": "Blade Runner",
          "Year": "1982",
          "Runtime": "117 min",
          "Plot": "A blade runner must pursue and terminate four replicants.",
          "imdbRating": "8.1",
          "imdbID": "tt0083658"
        }
        """;

    private const string ApplicationMeterName = "LamuFlix";
    private const string DurationInstrumentName = "lamuflix.enrichment.duration";
    private const string OutcomeInstrumentName = "lamuflix.enrichment.outcome";
    private const string ProviderTagKey = "provider";
    private const string OmdbProviderValue = "omdb";

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindAsync_AgainstTheStub_EmitsRedactedHttpClientTelemetryWithoutTheSentinel()
    {
        // arrange
        StubFound();
        var services = probe.BuildServices();
        var stub = new Uri(probe.Server.Urls[0]);
        var sentinel = MetadataProviderProbe.SentinelApiKey;
        var escaped = Uri.EscapeDataString(sentinel);

        // act
        using var capture = new ActivityCapture(TelemetryConstants.ActivitySourceName, "System.Net.Http");
        var result = await services.GetRequiredService<IMetadataProvider>()
            .FindAsync(new MetadataLookup("Blade Runner", null), TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();

        var httpClientLogs = probe.Logs.Entries.Where(IsHttpClientHandlerLog).ToList();
        httpClientLogs.Count.ShouldBeGreaterThan(0);
        httpClientLogs.ShouldContain(entry => CarriesRedactedStubUri(entry, stub, sentinel));

        capture.Activities.ShouldContain(activity =>
            activity.Kind == ActivityKind.Client
            && string.Equals(activity.Source.Name, "System.Net.Http", StringComparison.Ordinal)
            && MentionsHost(activity, stub.Host));
        capture.Activities.ShouldContain(activity =>
            string.Equals(activity.Source.Name, TelemetryConstants.ActivitySourceName, StringComparison.Ordinal)
            && string.Equals(activity.DisplayName, TelemetryConstants.MetadataLookup, StringComparison.Ordinal));

        var activityCorpus = string.Join("\n", capture.Activities.SelectMany(ActivityFragments));
        activityCorpus.ShouldNotContain(sentinel);
        activityCorpus.ShouldNotContain(escaped);

        var logCorpus = string.Join("\n", probe.Logs.Entries.SelectMany(LogFragments));
        logCorpus.ShouldNotContain(sentinel);
        logCorpus.ShouldNotContain(escaped);
    }

    [Fact]
    public async Task FindAsync_AFindingLookup_RecordsADurationHistogramOnTheApplicationMeterWithUnitSeconds()
    {
        // arrange
        StubFound();
        _ = EnrichmentMetrics.EnrichmentDuration;
        using var lookup = new ControlledLookup(probe, TimeSpan.FromSeconds(1.25), cancelAtBoundary: false);
        using var capture = new ApplicationMetricCapture(DurationInstrumentName);

        // act
        var result = await lookup.FindAsync("Blade Runner", TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        var measured = TheOnlyProviderDuration(capture.Measurements, 1.25);
        measured.Instrument.Meter.Name.ShouldBe(ApplicationMeterName);
        measured.Instrument.ShouldBeOfType<Histogram<double>>().Unit.ShouldBe("s");
    }

    [Fact]
    public async Task FindAsync_AFindingLookup_RecordsTheMetadataLookupSpanAndExactlyOneDurationMeasurement()
    {
        // arrange
        StubFound();
        using var lookup = new ControlledLookup(probe, TimeSpan.FromSeconds(3.5), cancelAtBoundary: false);
        using var capture = new ActivityCapture(TelemetryConstants.ActivitySourceName);
        using var metrics = new ApplicationMetricCapture(DurationInstrumentName);

        // act
        var result = await lookup.FindAsync("Blade Runner", TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        capture.Activities.Count(activity =>
            string.Equals(activity.DisplayName, TelemetryConstants.MetadataLookup, StringComparison.Ordinal)).ShouldBe(1);
        var measured = TheOnlyProviderDuration(metrics.Measurements, 3.5);
        measured.Instrument.Meter.Name.ShouldBe(ApplicationMeterName);
    }

    [Fact]
    public async Task FindAsync_ACancellationDuringTheLookup_RecordsTheLookupSpanAndDurationWithoutAnyOutcomeMeasurement()
    {
        // arrange
        StubFound();
        _ = EnrichmentMetrics.EnrichmentDuration;
        _ = EnrichmentMetrics.EnrichmentOutcome;
        using var lookup = new ControlledLookup(probe, TimeSpan.FromSeconds(4.25), cancelAtBoundary: true);
        using var capture = new ActivityCapture(TelemetryConstants.ActivitySourceName);
        using var metrics = new ApplicationMetricCapture(DurationInstrumentName, OutcomeInstrumentName);

        // act
        var act = () => lookup.FindAsync("Blade Runner", TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
        capture.Activities.Count(activity =>
            string.Equals(activity.DisplayName, TelemetryConstants.MetadataLookup, StringComparison.Ordinal)).ShouldBe(1);
        var measured = TheOnlyProviderDuration(metrics.MeasurementsOf(DurationInstrumentName), 4.25);
        measured.Instrument.Meter.Name.ShouldBe(ApplicationMeterName);
        metrics.MeasurementsOf(OutcomeInstrumentName).ShouldBeEmpty();
    }

    private void StubFound() =>
        probe.Server
            .Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(FoundBody));

    private static bool IsHttpClientHandlerLog(MetadataProviderProbe.CapturedLog entry) =>
        entry.CategoryName.StartsWith("System.Net.Http.HttpClient.", StringComparison.Ordinal)
        && (entry.CategoryName.EndsWith(".LogicalHandler", StringComparison.Ordinal)
            || entry.CategoryName.EndsWith(".ClientHandler", StringComparison.Ordinal));

    private static bool CarriesRedactedStubUri(
        MetadataProviderProbe.CapturedLog entry,
        Uri stub,
        string sentinel)
    {
        foreach (var value in NamedValues(entry, "Uri"))
        {
            var text = value?.ToString() ?? string.Empty;
            if (ContainsStubLocation(text, stub) && IsRedacted(text, sentinel))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsStubLocation(string text, Uri stub) =>
        text.Contains(stub.Host, StringComparison.Ordinal)
        && text.Contains(stub.AbsolutePath, StringComparison.Ordinal);

    private static bool IsRedacted(string text, string sentinel) =>
        !text.Contains("apikey", StringComparison.Ordinal)
        && !text.Contains(sentinel, StringComparison.Ordinal);

    private static IEnumerable<object?> NamedValues(MetadataProviderProbe.CapturedLog entry, string name)
    {
        foreach (var pair in entry.State)
        {
            if (string.Equals(pair.Key, name, StringComparison.Ordinal))
            {
                yield return pair.Value;
            }
        }

        foreach (var scope in entry.Scopes)
        {
            if (scope is not IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                continue;
            }

            foreach (var pair in pairs)
            {
                if (string.Equals(pair.Key, name, StringComparison.Ordinal))
                {
                    yield return pair.Value;
                }
            }
        }
    }

    private static bool MentionsHost(Activity activity, string host) =>
        ActivityFragments(activity).Any(fragment => fragment.Contains(host, StringComparison.Ordinal));

    private static IEnumerable<string> ActivityFragments(Activity activity)
    {
        yield return activity.DisplayName;
        yield return activity.OperationName;
        yield return activity.StatusDescription ?? string.Empty;
        foreach (var tag in activity.TagObjects)
        {
            yield return tag.Key;
            yield return tag.Value?.ToString() ?? string.Empty;
        }

        foreach (var activityEvent in activity.Events)
        {
            yield return activityEvent.Name;
            foreach (var tag in activityEvent.Tags)
            {
                yield return tag.Key;
                yield return tag.Value?.ToString() ?? string.Empty;
            }
        }
    }

    private static IEnumerable<string> LogFragments(MetadataProviderProbe.CapturedLog entry)
    {
        yield return entry.CategoryName;
        yield return entry.Message;
        yield return entry.ExceptionText;
        foreach (var pair in entry.State)
        {
            yield return pair.Key;
            yield return pair.Value?.ToString() ?? string.Empty;
        }

        foreach (var scope in entry.Scopes)
        {
            yield return scope.ToString() ?? string.Empty;
            if (scope is not IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                continue;
            }

            foreach (var pair in pairs)
            {
                yield return pair.Key;
                yield return pair.Value?.ToString() ?? string.Empty;
            }
        }
    }

    private static CapturedMetric TheOnlyProviderDuration(IReadOnlyList<CapturedMetric> measurements, double expectedSeconds)
    {
        var measured = measurements.ShouldHaveSingleItem();
        measured.Instrument.Name.ShouldBe(DurationInstrumentName);
        measured.Value.ShouldBe(expectedSeconds);
        var tag = measured.Tags.ShouldHaveSingleItem();
        tag.Key.ShouldBe(ProviderTagKey);
        tag.Value.ShouldBe(OmdbProviderValue);
        return measured;
    }

    private sealed class ControlledLookup : IDisposable
    {
        private readonly TimeSpan elapsedAtBoundary;
        private readonly bool cancelAtBoundary;
        private readonly ControlledTimeProvider clock = new(MetadataProviderProbe.FixedNow);
        private readonly CancellationTokenSource boundaryCancellation = new();
        private readonly HttpClient httpClient;
        private int boundaryReached;

        public ControlledLookup(MetadataProviderProbe probe, TimeSpan elapsedAtBoundary, bool cancelAtBoundary)
        {
            this.elapsedAtBoundary = elapsedAtBoundary;
            this.cancelAtBoundary = cancelAtBoundary;
            httpClient = new HttpClient(new TransportBoundary(AdvanceAtBoundary)
            {
                InnerHandler = new SocketsHttpHandler(),
            });
            Provider = new OmdbMetadataProvider(
                httpClient,
                Options.Create(new OmdbOptions
                {
                    ApiKey = MetadataProviderProbe.SentinelApiKey,
                    BaseUrl = probe.Server.Urls[0],
                }),
                clock,
                NullLogger<OmdbMetadataProvider>.Instance,
                new MetadataProviderHealthState());
        }

        public OmdbMetadataProvider Provider { get; }

        public Task<MetadataLookupResult> FindAsync(string title, CancellationToken ct) =>
            Provider.FindAsync(
                new MetadataLookup(title, null),
                cancelAtBoundary ? boundaryCancellation.Token : ct);

        public void Dispose()
        {
            httpClient.Dispose();
            boundaryCancellation.Dispose();
        }

        private void AdvanceAtBoundary()
        {
            if (Interlocked.Exchange(ref boundaryReached, 1) != 0)
            {
                return;
            }

            clock.Advance(elapsedAtBoundary);
            if (cancelAtBoundary)
            {
                boundaryCancellation.Cancel();
            }
        }
    }

    private sealed class TransportBoundary(Action onBoundary) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            onBoundary();
            cancellationToken.ThrowIfCancellationRequested();
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class ControlledTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset utcNow;
        private long timestamp;

        public ControlledTimeProvider(DateTimeOffset utcNow) => this.utcNow = utcNow;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Interlocked.Read(ref timestamp);

        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan elapsed) => Interlocked.Add(ref timestamp, elapsed.Ticks);
    }

    private sealed class ApplicationMetricCapture : IDisposable
    {
        private readonly ConcurrentQueue<CapturedMetric> measurements = [];
        private readonly MeterListener listener = new();

        public ApplicationMetricCapture(params string[] instrumentNames)
        {
            listener.SetMeasurementEventCallback<double>(
                (instrument, value, tags, _) => measurements.Enqueue(new(instrument, value, tags.ToArray())));
            listener.SetMeasurementEventCallback<long>(
                (instrument, value, tags, _) => measurements.Enqueue(new(instrument, value, tags.ToArray())));
            listener.InstrumentPublished = (instrument, publishedListener) =>
            {
                if (string.Equals(instrument.Meter.Name, ApplicationMeterName, StringComparison.Ordinal)
                    && instrumentNames.Contains(instrument.Name, StringComparer.Ordinal))
                {
                    publishedListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.Start();
        }

        public IReadOnlyList<CapturedMetric> Measurements => [.. measurements];

        public IReadOnlyList<CapturedMetric> MeasurementsOf(string instrumentName) =>
            [.. measurements.Where(measurement =>
                string.Equals(measurement.Instrument.Name, instrumentName, StringComparison.Ordinal))];

        public void Dispose() => listener.Dispose();
    }

    private sealed record CapturedMetric(Instrument Instrument, object Value, KeyValuePair<string, object?>[] Tags);

    private sealed class ActivityCapture : IDisposable
    {
        private readonly ConcurrentBag<Activity> activities = [];
        private readonly ActivityListener listener;

        public ActivityCapture(params string[] sourceNames)
        {
            listener = new ActivityListener
            {
                ShouldListenTo = source => Array.IndexOf(sourceNames, source.Name) >= 0,
                Sample = AlwaysSample,
                ActivityStopped = activity => activities.Add(activity),
            };
            ActivitySource.AddActivityListener(listener);
        }

        private static ActivitySamplingResult AlwaysSample(ref ActivityCreationOptions<ActivityContext> _) =>
            ActivitySamplingResult.AllData;

        public IReadOnlyList<Activity> Activities => [.. activities];

        public void Dispose() => listener.Dispose();
    }
}
