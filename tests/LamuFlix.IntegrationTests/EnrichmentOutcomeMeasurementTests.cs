using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Pipeline;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(TelemetryCompositionCollection))]
public sealed class EnrichmentOutcomeMeasurementTests(RabbitMqFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMilliseconds(500);
    private const int DefaultMaxAttempts = 3;
    private const int SmallMaxAttempts = 2;
    private const string OutcomeMeterName = "LamuFlix";
    private const string OutcomeInstrumentName = "lamuflix.enrichment.outcome";
    private const string OutcomeTagKey = "outcome";
    private const string FailureCategoryTagKey = "failure_category";
    private const string EnrichedOutcomeValue = "enriched";
    private const string NotFoundOutcomeValue = "not_found";
    private const string FailedOutcomeValue = "failed";
    private const string ProviderUnavailableCode = "provider_unavailable";

    private readonly RabbitMqProbe probe = new(fixture);

    public async ValueTask InitializeAsync() => await fixture.ResetTopologyAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Consumer_AFoundLookup_RecordsOneEnrichedOutcomeMeasurement()
    {
        var ct = TestContext.Current.CancellationToken;
        using var capture = new OutcomeCapture();
        await using var host = NewHost(_ => new MetadataLookupResult.Found(new MovieMetadata("Enriched")));
        var movie = host.AddPendingMovie(319);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await host.WaitForStatusAsync(ct, movie, EnrichmentStatus.Enriched);
        await WaitForMeasurementsAsync(capture, 1, ct);
        await host.StopAsync(CancellationToken.None);

        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(OutcomeInstrumentName);
        measured.Value.ShouldBe(1L);
        var tag = measured.Tags.ShouldHaveSingleItem();
        tag.Key.ShouldBe(OutcomeTagKey);
        tag.Value.ShouldBe(EnrichedOutcomeValue);
    }

    [Fact]
    public async Task Consumer_ANotFoundLookup_RecordsOneNotFoundOutcomeMeasurement()
    {
        var ct = TestContext.Current.CancellationToken;
        using var capture = new OutcomeCapture();
        await using var host = NewHost(_ => new MetadataLookupResult.NotFound());
        var movie = host.AddPendingMovie(320);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await host.WaitForStatusAsync(ct, movie, EnrichmentStatus.NotFound);
        await WaitForMeasurementsAsync(capture, 1, ct);
        await host.StopAsync(CancellationToken.None);

        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(OutcomeInstrumentName);
        measured.Value.ShouldBe(1L);
        var tag = measured.Tags.ShouldHaveSingleItem();
        tag.Key.ShouldBe(OutcomeTagKey);
        tag.Value.ShouldBe(NotFoundOutcomeValue);
    }

    [Fact]
    public async Task Consumer_ARetryableLookup_RecordsOneFailedMeasurementPerAttempt()
    {
        var ct = TestContext.Current.CancellationToken;
        using var capture = new OutcomeCapture();
        await fixture.DeleteTopologyAsync(ct);
        await using var host = NewHost(
            _ => new MetadataLookupResult.Failed(EnrichmentFailureCategory.ProviderUnavailable),
            maxAttempts: SmallMaxAttempts);
        var movie = host.AddPendingMovie(321);
        await host.Topology.EnsureDeclaredAsync(ct);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await host.WaitForProviderCallsAsync(ct, 2);
        await WaitForMeasurementsAsync(capture, 2, ct);
        await host.StopAsync(CancellationToken.None);

        host.Repository.Find(movie).ShouldNotBeNull().EnrichmentAttempts.ShouldBe(2);
        capture.Measurements.Count.ShouldBe(2);
        capture.Measurements.ShouldAllBe(
            measurement => measurement.InstrumentName == OutcomeInstrumentName && measurement.Value == 1L);
        capture.Measurements.ShouldAllBe(
            measurement => measurement.Tags.Length == 2
                           && Equals(measurement.Tags[0].Key, OutcomeTagKey)
                           && Equals(measurement.Tags[0].Value, FailedOutcomeValue)
                           && Equals(measurement.Tags[1].Key, FailureCategoryTagKey)
                           && Equals(measurement.Tags[1].Value, ProviderUnavailableCode));
    }

    [Fact]
    public async Task Consumer_ARefusedClaim_RecordsNoOutcomeMeasurement()
    {
        var ct = TestContext.Current.CancellationToken;
        var delivered = new ConcurrentQueue<Activity>();
        using var deliveryListener = ObserveConsumerDeliveries(delivered);
        using var capture = new OutcomeCapture();
        await using var host = NewHost(_ => new MetadataLookupResult.Found(new MovieMetadata("Enriched")));
        var movie = new MovieId(322);
        await host.Repository.AddAsync(
            Movie.Rehydrate(
                movie,
                "Held",
                new LibraryPath("C:/library/a"),
                new MediaFormat("mkv"),
                false,
                null,
                EnrichmentStatus.Pending,
                null,
                0,
                null,
                host.Time.GetUtcNow()),
            ct);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await WaitForDeliveryAsync(delivered, movie, ct);
        await host.StopAsync(CancellationToken.None);

        host.Provider.Calls.ShouldBe(0);
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task Consumer_ACancellationDuringLookup_RecordsNoOutcomeMeasurement()
    {
        var ct = TestContext.Current.CancellationToken;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var capture = new OutcomeCapture();
        await using var host = NewHost(
            _ => new MetadataLookupResult.Found(new MovieMetadata("Enriched")),
            beforeLookup: async (_, token) =>
            {
                entered.TrySetResult();
                await Task.Delay(Timeout.Infinite, token);
            });
        var movie = host.AddPendingMovie(323);
        await host.Topology.EnsureDeclaredAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.RequestedQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.RetryQueue, ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);

        await host.StartAsync(ct);
        await entered.Task.WaitAsync(ct);
        await host.StopAsync(CancellationToken.None);

        (await probe.PollGetMatchingAsync(
            RabbitMqTopology.RequestedQueue,
            candidate => Read(candidate).MovieId.Value == 323,
            ct)).ShouldNotBeNull();
        host.Repository.Find(movie).ShouldNotBeNull().Status.ShouldBe(EnrichmentStatus.Pending);
        capture.Measurements.ShouldBeEmpty();
    }

    private static async Task WaitForMeasurementsAsync(
        OutcomeCapture capture,
        int expected,
        CancellationToken ct)
    {
        var deadline = TimeProvider.System.GetUtcNow() + TimeSpan.FromSeconds(30);
        while (TimeProvider.System.GetUtcNow() < deadline && capture.Measurements.Count < expected)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
        }

        capture.Measurements.Count.ShouldBeGreaterThanOrEqualTo(expected);
    }

    private static ActivityListener ObserveConsumerDeliveries(ConcurrentQueue<Activity> observed)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == TelemetryConstants.ActivitySourceName,
            ActivityStopped = observed.Enqueue,
        };
        SampleActivity<ActivityContext> sample = (ref _) => ActivitySamplingResult.AllData;
        listener.Sample = sample;
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static Task<IReadOnlyList<Activity>> WaitForDeliveryAsync(
        ConcurrentQueue<Activity> observed,
        MovieId movie,
        CancellationToken ct) =>
        WaitForDeliveriesAsync(observed, movie, 1, ct);

    private static Task<IReadOnlyList<Activity>> WaitForDeliveriesAsync(
        ConcurrentQueue<Activity> observed,
        MovieId movie,
        int count,
        CancellationToken ct) =>
        WaitForActivitiesAsync(
            observed,
            activity => activity.OperationName == TelemetryConstants.EnrichmentProcess
                        && Equals(activity.GetTagItem(TelemetryConstants.MovieId), movie.Value),
            count,
            ct);

    private static async Task<IReadOnlyList<Activity>> WaitForActivitiesAsync(
        ConcurrentQueue<Activity> observed,
        Func<Activity, bool> match,
        int count,
        CancellationToken cancellationToken)
    {
        var deadline = TimeProvider.System.GetUtcNow() + TimeSpan.FromSeconds(30);
        IReadOnlyList<Activity> found = [];
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            found = [.. observed.Where(match)];
            if (found.Count >= count)
            {
                return found;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }

        found.Count.ShouldBeGreaterThanOrEqualTo(count);
        return found;
    }

    private static EnrichmentRequested Read(BasicGetResult message) =>
        JsonSerializer.Deserialize<EnrichmentRequested>(
            Encoding.UTF8.GetString(message.Body.Span),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldNotBeNull();

    private ConsumerHost NewHost(
        Func<MetadataLookup, MetadataLookupResult> script,
        int maxAttempts = DefaultMaxAttempts,
        Func<MetadataLookup, CancellationToken, Task>? beforeLookup = null)
    {
        var clock = new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));
        var provider = new ScriptedMetadataProvider(script);
        IMetadataProvider metadata = beforeLookup is null
            ? provider
            : new PausingMetadataProvider(provider, beforeLookup);
        var repository = new LeaseAwareMovieRepository(clock, ClaimLease);
        var options = fixture.Options with { Prefetch = 1 };
        var owner = new RabbitMqConnectionOwner(Options.Create(options));
        var enrichment = Options.Create(new EnrichmentOptions { MaxAttempts = maxAttempts, ClaimLease = ClaimLease });
        var topology = new RabbitMqTopology(owner, Options.Create(options), enrichment);
        var publisher = new RabbitMqEnrichmentQueuePublisher(topology, owner);
        var thumbnailPublisher = new RabbitMqThumbnailQueuePublisher(topology, owner);
        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton(enrichment.Value);
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(enrichment);
        services.AddSingleton(metadata);
        services.AddSingleton<IMovieRepository>(repository);
        services.AddLogging();
        services.AddSingleton<RabbitMqConnectionOwner>(_ => owner);
        services.AddSingleton(topology);
        services.AddSingleton(publisher);
        services.AddSingleton<IEnrichmentQueue>(publisher);
        services.AddScoped<ProcessEnrichmentCommandHandler>();
        services.AddScoped<ProcessEnrichmentCommandValidator>();
        services.AddScoped<IValidator<ProcessEnrichmentCommand>>(p =>
            p.GetRequiredService<ProcessEnrichmentCommandValidator>());
        services.AddHandler<ProcessEnrichmentCommandHandler, ProcessEnrichmentCommand, ProcessEnrichmentOutcome>();

        var built = services.BuildServiceProvider();
        var consumer = new EnrichmentConsumer(
            owner,
            topology,
            publisher,
            built.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            Options.Create(new ThumbnailOptions()),
            thumbnailPublisher,
            NullLogger<EnrichmentConsumer>.Instance,
            NoOpLiveness.Instance,
            clock);
        return new ConsumerHost(owner, topology, publisher, repository, provider, clock, consumer, built);
    }

    private sealed class ConsumerHost(
        RabbitMqConnectionOwner owner,
        RabbitMqTopology topology,
        RabbitMqEnrichmentQueuePublisher publisher,
        LeaseAwareMovieRepository repository,
        ScriptedMetadataProvider provider,
        AdjustableTimeProvider time,
        EnrichmentConsumer consumer,
        ServiceProvider services) : IAsyncDisposable
    {
        public RabbitMqTopology Topology { get; } = topology;

        public RabbitMqEnrichmentQueuePublisher Publisher { get; } = publisher;

        public LeaseAwareMovieRepository Repository { get; } = repository;

        public ScriptedMetadataProvider Provider { get; } = provider;

        public AdjustableTimeProvider Time { get; } = time;

        public Task StartAsync(CancellationToken cancellationToken) => consumer.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => consumer.StopAsync(cancellationToken);

        public MovieId AddPendingMovie(int id)
        {
            var movieId = new MovieId(id);
            Repository.Add(
                Movie.Create(movieId, $"Movie {id}", new LibraryPath("C:/library/a"), new MediaFormat("mkv")));
            return movieId;
        }

        public async Task WaitForProviderCallsAsync(CancellationToken cancellationToken, int expected)
        {
            var clock = TimeProvider.System;
            var deadline = clock.GetUtcNow() + TimeSpan.FromSeconds(30);
            while (Provider.Calls < expected && clock.GetUtcNow() < deadline)
            {
                Time.Advance(ClaimLease + TimeSpan.FromMilliseconds(250));
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }

            Provider.Calls.ShouldBeGreaterThanOrEqualTo(expected);
        }

        public async Task WaitForStatusAsync(CancellationToken cancellationToken, MovieId id, EnrichmentStatus status)
        {
            var clock = TimeProvider.System;
            var deadline = clock.GetUtcNow() + TimeSpan.FromSeconds(30);
            while (clock.GetUtcNow() < deadline)
            {
                if (Repository.Find(id)?.Status == status)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }

            Repository.Find(id).ShouldNotBeNull().Status.ShouldBe(status);
        }

        public ValueTask DisposeAsync()
        {
            services.Dispose();
            return owner.DisposeAsync();
        }
    }

    private sealed class NoOpLiveness : IWorkerLiveness
    {
        public static readonly NoOpLiveness Instance = new();

        public bool IsListening => false;

        public void MarkListening()
        {
        }

        public void MarkStopped()
        {
        }
    }

    private sealed class PausingMetadataProvider(
        ScriptedMetadataProvider inner,
        Func<MetadataLookup, CancellationToken, Task> pause) : IMetadataProvider
    {
        public async Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)
        {
            await pause(lookup, ct);
            return await inner.FindAsync(lookup, ct);
        }
    }

    private sealed class OutcomeCapture : IDisposable
    {
        private readonly ConcurrentQueue<CapturedOutcome> measurements = [];

        private readonly MeterListener listener = new();

        public OutcomeCapture()
        {
            listener.SetMeasurementEventCallback<long>(
                (instrument, value, tags, _) => measurements.Enqueue(new(instrument.Name, value, tags.ToArray())));
            listener.InstrumentPublished = (instrument, publishedListener) =>
            {
                if (string.Equals(instrument.Meter.Name, OutcomeMeterName, StringComparison.Ordinal)
                    && string.Equals(instrument.Name, OutcomeInstrumentName, StringComparison.Ordinal))
                {
                    publishedListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.Start();
        }

        public IReadOnlyList<CapturedOutcome> Measurements => [.. measurements];

        public void Dispose() => listener.Dispose();
    }

    private sealed record CapturedOutcome(string InstrumentName, long Value, KeyValuePair<string, object?>[] Tags);

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly Lock sync = new();

        private DateTimeOffset utcNow = now;

        public override DateTimeOffset GetUtcNow()
        {
            lock (sync)
            {
                return utcNow;
            }
        }

        public void Advance(TimeSpan amount)
        {
            lock (sync)
            {
                utcNow += amount;
            }
        }
    }
}

