using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(OllamaEmbeddingCollection.Name)]
public sealed class OllamaEmbeddingTelemetryTests(OllamaEmbeddingProbe probe)
    : IClassFixture<OllamaEmbeddingProbe>, IAsyncLifetime
{
    private const string PlotText = "A private plot line that must not appear in telemetry.";

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GenerateAsync_Success_RecordsOneLogicalSpanDurationAndOutcome()
    {
        // arrange
        probe.StubVectors(OllamaEmbeddingProbe.Vector());
        using var provider = probe.BuildServices();
        using var activities = new ActivityCapture();
        using var metrics = new ApplicationMetricCapture();

        // act
        await GenerateAsync(provider, TestContext.Current.CancellationToken);

        // assert
        var span = SingleSpan(activities);
        span.GetTagItem(TelemetryConstants.OutcomeTagName).ShouldBe(TelemetryConstants.SucceededOutcome);
        metrics.MeasurementsOf(TelemetryConstants.EmbeddingDurationMetricName).ShouldHaveSingleItem();
        var outcome = metrics.MeasurementsOf(TelemetryConstants.EmbeddingOutcomeMetricName).ShouldHaveSingleItem();
        outcome.Tags.ShouldBe([new KeyValuePair<string, object?>(TelemetryConstants.OutcomeTagName, TelemetryConstants.SucceededOutcome)]);
        TagCorpus(span).ShouldNotContain(PlotText);
        TagCorpus(span).ShouldNotContain(OllamaEmbeddingProbe.ModelName);
    }

    [Fact]
    public async Task GenerateAsync_AcrossRetries_RecordsOneLogicalCall()
    {
        // arrange
        probe.Server
            .Given(Request.Create().UsingPost())
            .InScenario("retry")
            .WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(503));
        probe.Server
            .Given(Request.Create().UsingPost())
            .InScenario("retry")
            .WhenStateIs("recovered")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(OllamaEmbeddingProbe.EmbedBody(OllamaEmbeddingProbe.Vector())));
        using var provider = probe.BuildServices();
        using var activities = new ActivityCapture();
        using var metrics = new ApplicationMetricCapture();

        // act
        await GenerateAsync(provider, TestContext.Current.CancellationToken);

        // assert
        probe.RequestCount.ShouldBe(2);
        activities.Activities.Count(IsEmbeddingSpan).ShouldBe(1);
        metrics.MeasurementsOf(TelemetryConstants.EmbeddingDurationMetricName).ShouldHaveSingleItem();
        metrics.MeasurementsOf(TelemetryConstants.EmbeddingOutcomeMetricName).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GenerateAsync_LocalRejects_RecordNoLogicalTelemetry()
    {
        // arrange
        using var provider = probe.BuildServices();
        using var activities = new ActivityCapture();
        using var metrics = new ApplicationMetricCapture();
        var generator = provider.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

        // act
        await generator.GenerateAsync([], cancellationToken: TestContext.Current.CancellationToken);
        await Should.ThrowAsync<ArgumentException>(
            () => generator.GenerateAsync([PlotText], new EmbeddingGenerationOptions { Dimensions = 768 }, TestContext.Current.CancellationToken));

        // assert
        activities.Activities.ShouldBeEmpty();
        metrics.Measurements.ShouldBeEmpty();
        probe.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task GenerateAsync_TypedFailure_RecordsOneFailureOutcomeWithOnlyLowCardinalityTags()
    {
        // arrange
        probe.StubBody(500);
        using var provider = probe.BuildServices();
        using var activities = new ActivityCapture();
        using var metrics = new ApplicationMetricCapture();

        // act
        await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));

        // assert
        var span = SingleSpan(activities);
        span.GetTagItem(TelemetryConstants.OutcomeTagName).ShouldBe(TelemetryConstants.FailedOutcome);
        span.GetTagItem(TelemetryConstants.ErrorType).ShouldBe("provider_unavailable");
        span.TagObjects.Select(tag => tag.Key)
            .ShouldBe([TelemetryConstants.OutcomeTagName, TelemetryConstants.ErrorType], ignoreOrder: true);

        metrics.MeasurementsOf(TelemetryConstants.EmbeddingDurationMetricName).ShouldHaveSingleItem();
        var outcome = metrics.MeasurementsOf(TelemetryConstants.EmbeddingOutcomeMetricName).ShouldHaveSingleItem();
        outcome.Tags.Select(tag => tag.Key)
            .ShouldBe([TelemetryConstants.OutcomeTagName, TelemetryConstants.FailureCategoryTagName], ignoreOrder: true);
        outcome.Tags.ShouldContain(new KeyValuePair<string, object?>(TelemetryConstants.FailureCategoryTagName, "provider_unavailable"));
        TagCorpus(span).ShouldNotContain(PlotText);
        TagCorpus(span).ShouldNotContain(OllamaEmbeddingProbe.ModelName);
    }

    [Fact]
    public async Task GenerateAsync_CallerCancellation_RecordsACancelledOutcomeWithoutAProviderFailureTag()
    {
        // arrange
        probe.Server
            .Given(Request.Create().UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(2))
                .WithBody(OllamaEmbeddingProbe.EmbedBody(OllamaEmbeddingProbe.Vector())));
        using var provider = probe.BuildServices();
        using var activities = new ActivityCapture();
        using var metrics = new ApplicationMetricCapture();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => GenerateAsync(provider, cancellation.Token));

        // assert
        var span = SingleSpan(activities);
        span.GetTagItem(TelemetryConstants.OutcomeTagName).ShouldBe(TelemetryConstants.CancelledOutcome);
        span.GetTagItem(TelemetryConstants.ErrorType).ShouldBeNull();
        var outcome = metrics.MeasurementsOf(TelemetryConstants.EmbeddingOutcomeMetricName).ShouldHaveSingleItem();
        outcome.Tags.ShouldBe([new KeyValuePair<string, object?>(TelemetryConstants.OutcomeTagName, TelemetryConstants.CancelledOutcome)]);
    }

    [Fact]
    public async Task StartupProbe_RecordsTheSameSingleLogicalCall()
    {
        // arrange
        probe.StubVectors(OllamaEmbeddingProbe.Vector());
        using var provider = probe.BuildServices();
        using var activities = new ActivityCapture();
        using var metrics = new ApplicationMetricCapture();

        // act
        await provider.GetServices<IHostedService>()
            .OfType<OllamaEmbeddingStartupValidator>()
            .Single()
            .StartAsync(TestContext.Current.CancellationToken);

        // assert
        activities.Activities.Count(IsEmbeddingSpan).ShouldBe(1);
        metrics.MeasurementsOf(TelemetryConstants.EmbeddingDurationMetricName).ShouldHaveSingleItem();
        metrics.MeasurementsOf(TelemetryConstants.EmbeddingOutcomeMetricName).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task HealthCheck_TransitionsWithoutIssuingHttp()
    {
        // arrange
        probe.StubVectors(OllamaEmbeddingProbe.Vector());
        using var provider = probe.BuildServices();
        var requestsBefore = probe.RequestCount;

        // act
        var before = await CheckAsync(provider);
        var requestsAfterBefore = probe.RequestCount;

        // assert
        before.Status.ShouldBe(HealthStatus.Unhealthy);
        requestsAfterBefore.ShouldBe(requestsBefore);

        // act
        await provider.GetServices<IHostedService>()
            .OfType<OllamaEmbeddingStartupValidator>()
            .Single()
            .StartAsync(TestContext.Current.CancellationToken);
        var requestsAfterStartup = probe.RequestCount;
        var healthy = await CheckAsync(provider);

        // assert
        healthy.Status.ShouldBe(HealthStatus.Healthy);
        probe.RequestCount.ShouldBe(requestsAfterStartup);

        // act
        probe.StubBody(500);
        await Should.ThrowAsync<EmbeddingGenerationException>(() => GenerateAsync(provider, TestContext.Current.CancellationToken));
        var requestsAfterFailure = probe.RequestCount;
        var degraded = await CheckAsync(provider);

        // assert
        degraded.Status.ShouldBe(HealthStatus.Degraded);
        probe.RequestCount.ShouldBe(requestsAfterFailure);
    }

    private static async Task GenerateAsync(
        IServiceProvider provider,
        CancellationToken cancellationToken = default) =>
        await provider
            .GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>()
            .GenerateAsync([PlotText], cancellationToken: cancellationToken);

    private static bool IsEmbeddingSpan(Activity activity) =>
        string.Equals(activity.DisplayName, TelemetryConstants.EmbeddingGenerate, StringComparison.Ordinal);

    private static Activity SingleSpan(ActivityCapture activities) =>
        activities.Activities.Where(IsEmbeddingSpan).ShouldHaveSingleItem();

    private static string TagCorpus(Activity activity) =>
        string.Join("\n", activity.TagObjects.Select(tag => $"{tag.Key}={tag.Value}"));

    private static async Task<HealthReportEntry> CheckAsync(IServiceProvider provider) =>
        (await provider
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(
                registration => registration.Name == OllamaEmbeddingProbe.CheckName,
                TestContext.Current.CancellationToken))
        .Entries[OllamaEmbeddingProbe.CheckName];

    private sealed class ApplicationMetricCapture : IDisposable
    {
        private readonly ConcurrentQueue<CapturedMetric> measurements = [];
        private readonly MeterListener listener = new();

        public ApplicationMetricCapture()
        {
            listener.SetMeasurementEventCallback<double>(
                (instrument, value, tags, _) => measurements.Enqueue(new(instrument, value, tags.ToArray())));
            listener.SetMeasurementEventCallback<long>(
                (instrument, value, tags, _) => measurements.Enqueue(new(instrument, value, tags.ToArray())));
            listener.InstrumentPublished = (instrument, publishedListener) =>
            {
                if (string.Equals(instrument.Meter.Name, TelemetryConstants.ActivitySourceName, StringComparison.Ordinal)
                    && (string.Equals(instrument.Name, TelemetryConstants.EmbeddingDurationMetricName, StringComparison.Ordinal)
                        || string.Equals(instrument.Name, TelemetryConstants.EmbeddingOutcomeMetricName, StringComparison.Ordinal)))
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

        public ActivityCapture()
        {
            listener = new ActivityListener
            {
                ShouldListenTo = source =>
                    string.Equals(source.Name, TelemetryConstants.ActivitySourceName, StringComparison.Ordinal),
                Sample = AlwaysSample,
                ActivityStopped = activity => activities.Add(activity),
            };
            ActivitySource.AddActivityListener(listener);
        }

        public IReadOnlyList<Activity> Activities => [.. activities];

        public void Dispose() => listener.Dispose();

        private static ActivitySamplingResult AlwaysSample(ref ActivityCreationOptions<ActivityContext> _) =>
            ActivitySamplingResult.AllData;
    }
}



