using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(SecondaryMetadataProviderCollection.Name)]
public sealed class SecondaryMetadataProviderTelemetryTests(SecondaryMetadataProviderProbe probe)
    : IClassFixture<SecondaryMetadataProviderProbe>, IAsyncLifetime
{
    private const string DurationInstrumentName = "lamuflix.enrichment.duration";
    private const string ProviderTagKey = "provider";
    private const string TmdbProviderValue = "tmdb";

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindAsync_AFindingLookup_OpensOneSpanOnTheTelemetrySourceAndRecordsOneProviderDuration()
    {
        // arrange
        probe.StubFoundLookup();
        using var lookup = new ControlledLookup(probe, TimeSpan.FromSeconds(1.25), cancelAtBoundary: false);
        using var activities = new SecondaryActivityCapture(TelemetryConstants.ActivitySourceName);
        using var metrics = new SecondaryMetricCapture(DurationInstrumentName);
        using var ambient = new Activity("test.ambient").Start();

        // act
        var result = await lookup.FindAsync(TestContext.Current.CancellationToken);

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
        var spans = LookupSpans(activities, ambient.TraceId);
        spans.Count.ShouldBe(1);
        spans[0].Source.Name.ShouldBe(TelemetryConstants.ActivitySourceName);
        spans[0].ParentId.ShouldBe(ambient.Id);
        spans[0].GetTagItem(TelemetryConstants.ErrorType).ShouldBeNull();
        spans[0].Status.ShouldBe(ActivityStatusCode.Unset);
        AssertExactlyOneProviderDuration(metrics.Measurements, 1.25);
    }

    [Fact]
    public async Task FindAsync_AFailingLookup_TagsTheSpanWithTheCategoryCodeAndRecordsOneProviderDuration()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 400);
        using var lookup = new ControlledLookup(probe, TimeSpan.FromSeconds(2.5), cancelAtBoundary: false);
        using var activities = new SecondaryActivityCapture(TelemetryConstants.ActivitySourceName);
        using var metrics = new SecondaryMetricCapture(DurationInstrumentName);
        using var ambient = new Activity("test.ambient").Start();

        // act
        var result = await lookup.FindAsync(TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        var spans = LookupSpans(activities, ambient.TraceId);
        spans.Count.ShouldBe(1);
        spans[0].GetTagItem(TelemetryConstants.ErrorType)
            .ShouldBe(EnrichmentFailureCategory.InvalidResponse.Code);
        spans[0].Status.ShouldBe(ActivityStatusCode.Error);
        AssertExactlyOneProviderDuration(metrics.Measurements, 2.5);
    }

    [Fact]
    public async Task FindAsync_AcrossOutcomes_NeverTagsTheActivityWithSecretTitleOrImdbId()
    {
        // arrange
        var services = probe.BuildServices();
        using var activities = new SecondaryActivityCapture(
            TelemetryConstants.ActivitySourceName,
            "System.Net.Http");
        var ct = TestContext.Current.CancellationToken;

        // act
        probe.StubFoundLookup();
        _ = await FindAsync(services, ct);

        probe.Reset();
        probe.StubSearch(string.Empty, statusCode: 401);
        _ = await FindAsync(services, ct);

        probe.Reset();
        probe.StubSearch(string.Empty, statusCode: 503);
        _ = await FindAsync(services, ct);

        // assert
        var corpus = string.Join("\n", activities.Activities.SelectMany(Fragments));
        corpus.ShouldNotContain(SecondaryMetadataProviderProbe.SentinelApiKey);
        corpus.ShouldNotContain(Uri.EscapeDataString(SecondaryMetadataProviderProbe.SentinelApiKey));
        corpus.ShouldNotContain("The Matrix");
        corpus.ShouldNotContain("tt0133093");
    }

    private static async Task<MetadataLookupResult> FindAsync(IServiceProvider services, CancellationToken ct) =>
        await services
            .GetRequiredService<TmdbMetadataProvider>()
            .FindAsync(new MetadataLookup("The Matrix", null), ct);

    private static IReadOnlyList<Activity> LookupSpans(SecondaryActivityCapture capture, ActivityTraceId traceId) =>
        [.. capture.Activities.Where(activity =>
            string.Equals(activity.DisplayName, TelemetryConstants.MetadataLookup, StringComparison.Ordinal)
            && activity.TraceId == traceId)];

    private static void AssertExactlyOneProviderDuration(IReadOnlyList<CapturedMeasurement> measurements, double expectedSeconds)
    {
        var measured = measurements.ShouldHaveSingleItem();
        measured.Instrument.Name.ShouldBe(DurationInstrumentName);
        measured.Value.ShouldBe(expectedSeconds);
        var tag = measured.Tags.ShouldHaveSingleItem();
        tag.Key.ShouldBe(ProviderTagKey);
        tag.Value.ShouldBe(TmdbProviderValue);
    }

    private static IEnumerable<string> Fragments(Activity activity)
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

    private static MovieMetadata Metadata(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;

    private static EnrichmentFailureCategory Category(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category;

    private sealed class ControlledLookup : IDisposable
    {
        private readonly TimeSpan elapsedAtBoundary;
        private readonly bool cancelAtBoundary;
        private readonly ControlledTimeProvider clock = new(SecondaryMetadataProviderProbe.FixedNow);
        private readonly CancellationTokenSource boundaryCancellation = new();
        private readonly HttpClient httpClient;
        private int boundaryReached;

        public ControlledLookup(SecondaryMetadataProviderProbe probe, TimeSpan elapsedAtBoundary, bool cancelAtBoundary)
        {
            this.elapsedAtBoundary = elapsedAtBoundary;
            this.cancelAtBoundary = cancelAtBoundary;
            httpClient = new HttpClient(new TransportBoundary(AdvanceAtBoundary)
            {
                InnerHandler = new SocketsHttpHandler(),
            });
            Provider = new TmdbMetadataProvider(
                httpClient,
                Options.Create(new TmdbOptions
                {
                    Enabled = true,
                    ApiKey = SecondaryMetadataProviderProbe.SentinelApiKey,
                    BaseUrl = probe.ApiBaseUrl,
                }),
                Options.Create(new OmdbOptions()),
                clock,
                NullLogger<TmdbMetadataProvider>.Instance,
                new SecondaryMetadataProviderHealthState());
        }

        private TmdbMetadataProvider Provider { get; }

        public Task<MetadataLookupResult> FindAsync(CancellationToken ct) =>
            Provider.FindAsync(
                new MetadataLookup("The Matrix", null),
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
}
