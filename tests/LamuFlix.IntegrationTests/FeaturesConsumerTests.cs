using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Pipeline;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using LamuFlix.UnitTests;
using LamuFlix.UnitTests.Features.Enrichment;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

/// <summary>
/// DEV-414 T011: live-broker proof of the features consumer delivery, retry, dead-letter, shutdown
/// requeue, failed publication, malformed-payload, and sanitized-log behavior.
/// </summary>
[Collection(nameof(RabbitMqCollection))]
public sealed class FeaturesConsumerTests(RabbitMqFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset ComputedAt = new(2026, 10, 23, 9, 30, 0, TimeSpan.Zero);
    private static readonly TimeSpan RetryResidence = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollDeadline = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);
    private const int DefaultMaxAttempts = 3;
    private const int SmallMaxAttempts = 2;
    private const string FailureCategoryHeader = "x-lamuflix-failure-category";
    private const string FailureReasonHeader = "x-lamuflix-failure-reason";
    private const string FailureAttemptHeader = "x-lamuflix-failure-attempt";

    private readonly RabbitMqProbe probe = new(fixture);

    public async ValueTask InitializeAsync() => await fixture.ResetTopologyAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Consumer_AStoredPlot_GeneratesOneVectorAndPersistsOneSnapshot()
    {
        var ct = TestContext.Current.CancellationToken;
        const string plot = "A stored plot that must never appear in logs.";
        var generator = SuccessfulGenerator();
        await using var host = NewHost(generator);
        var movie = host.AddMovie(801, plot);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 1), ct);
        var snapshot = await WaitForSnapshotAsync(host, movie, ct);
        await host.StopAsync(CancellationToken.None);

        snapshot.ComputedAt.ShouldBe(ComputedAt);
        generator.CallCount.ShouldBe(1);
        var expected = (await new FakeEmbeddingGenerator().GenerateAsync([plot], cancellationToken: ct))[0]
            .Vector.ToArray();
        snapshot.Vector.ToArray().ShouldBe(expected);
        host.Features.UpsertCount.ShouldBe(1);
        (await ReadyMessageCountAsync(RabbitMqTopology.FeaturesRequestedQueue, ct)).ShouldBe(0U);
        (await ReadyMessageCountAsync(RabbitMqTopology.FeaturesRetryQueue, ct)).ShouldBe(0U);
        (await ReadyMessageCountAsync(RabbitMqTopology.FeaturesDeadLetterQueue, ct)).ShouldBe(0U);
    }

    [Fact]
    public async Task Consumer_AWhitespacePlot_AcksAsANoOpWithoutGeneratingOrUpserting()
    {
        var ct = TestContext.Current.CancellationToken;
        var generator = SuccessfulGenerator();
        var log = new CapturingLog();
        await using var host = NewHost(generator, log: log);
        var movie = host.AddMovie(802, " \t\n\u00A0 ");

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 1), ct);
        await WaitUntilAsync(
            _ => Task.FromResult(log.Messages.Any(entry => entry.Contains("no input", StringComparison.Ordinal))),
            ct);
        await host.StopAsync(CancellationToken.None);

        generator.CallCount.ShouldBe(0);
        host.Features.UpsertCount.ShouldBe(0);
        (await ReadyMessageCountAsync(RabbitMqTopology.FeaturesDeadLetterQueue, ct)).ShouldBe(0U);
    }

    [Fact]
    public async Task Consumer_ARetryableFailureBelowMax_PublishesAttemptPlusOneToTheFeaturesRetryQueue()
    {
        var ct = TestContext.Current.CancellationToken;
        await fixture.DeleteTopologyAsync(ct);
        var generator = FailingGenerator(
            new EmbeddingGenerationException(EnrichmentFailureCategory.RateLimited));
        await using var host = NewHost(generator, retryDelay: RetryResidence);
        var movie = host.AddMovie(803, "A stored plot.");

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 1), ct);
        var retried = await probe.PollGetMatchingAsync(
            RabbitMqTopology.FeaturesRetryQueue,
            candidate => Read(candidate).MovieId.Value == movie.Value,
            ct);
        await host.StopAsync(CancellationToken.None);

        retried.ShouldNotBeNull();
        Read(retried).Attempt.ShouldBe(2);
        Header(retried, FailureCategoryHeader).ShouldBeNull();
        Header(retried, FailureReasonHeader).ShouldBeNull();
        RawHeader(retried, FailureAttemptHeader).ShouldBeNull();
        Header(retried, TraceContextCarrier.TraceParentHeader).ShouldStartWith("00-");
        host.Features.UpsertCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(1, "invalid_response", "invalid_response", "non_retryable")]
    [InlineData(SmallMaxAttempts, "provider_unavailable", "provider_unavailable", "max_attempts_exhausted")]
    [InlineData(SmallMaxAttempts + 1, "provider_unavailable", "provider_unavailable", "max_attempts_exhausted")]
    public async Task Consumer_ATerminalFailure_PublishesToTheFeaturesDeadLetterQueueWithWhitelistedHeaders(
        int attempt,
        string categoryCode,
        string expectedCategory,
        string expectedReason)
    {
        var ct = TestContext.Current.CancellationToken;
        await fixture.DeleteTopologyAsync(ct);
        var category = ParseCategory(categoryCode);
        var generator = FailingGenerator(new EmbeddingGenerationException(category));
        var log = new CapturingLog();
        await using var host = NewHost(generator, maxAttempts: SmallMaxAttempts, log: log);
        var movie = host.AddMovie(804, "A stored plot.");

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new FeaturesRequested(movie, attempt), ct);
        var dead = await probe.PollGetMatchingAsync(
            RabbitMqTopology.FeaturesDeadLetterQueue,
            candidate => Read(candidate).MovieId.Value == movie.Value,
            ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        Read(dead).Attempt.ShouldBe(attempt);
        Header(dead, FailureCategoryHeader).ShouldBe(expectedCategory);
        Header(dead, FailureReasonHeader).ShouldBe(expectedReason);
        RawHeader(dead, FailureAttemptHeader).ShouldBeOfType<int>().ShouldBe(attempt);
        Header(dead, TraceContextCarrier.TraceParentHeader).ShouldStartWith("00-");
        var headers = dead.BasicProperties.Headers.ShouldNotBeNull();
        headers.Keys.Where(key => key.StartsWith("x-lamuflix-", StringComparison.Ordinal)).Order()
            .ShouldBe([FailureAttemptHeader, FailureCategoryHeader, FailureReasonHeader], ignoreOrder: true);
        Header(dead, TraceContextCarrier.TraceParentHeader).ShouldStartWith("00-");
        host.Features.UpsertCount.ShouldBe(0);
    }

    [Fact]
    public async Task Consumer_AnAttemptBelowOne_DeadLettersAsInvalidResponseWithoutCallingTheGenerator()
    {
        var ct = TestContext.Current.CancellationToken;
        var generator = SuccessfulGenerator();
        await using var host = NewHost(generator);
        var movie = host.AddMovie(805, "A stored plot.");

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 0), ct);
        var dead = await probe.PollGetMatchingAsync(
            RabbitMqTopology.FeaturesDeadLetterQueue,
            candidate => Read(candidate).MovieId.Value == movie.Value,
            ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        Header(dead, FailureCategoryHeader).ShouldBe("invalid_response");
        Header(dead, FailureReasonHeader).ShouldBe("non_retryable");
        RawHeader(dead, FailureAttemptHeader).ShouldBeOfType<int>().ShouldBe(0);
        generator.CallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Consumer_AMalformedBody_ReachesTheFeaturesDeadLetterQueue()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(SuccessfulGenerator());
        await host.Topology.EnsureDeclaredAsync(ct);

        await probe.PublishAsync(
            EnrichmentRouting.FeaturesRequestedRoutingKey,
            "not json"u8.ToArray(),
            ct);
        await host.StartAsync(ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.FeaturesDeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        Encoding.UTF8.GetString(dead.Body.Span).ShouldBe("not json");
        RawHeader(dead, FailureCategoryHeader).ShouldBeNull();
        host.Features.UpsertCount.ShouldBe(0);
    }

    [Fact]
    public async Task Consumer_ShutdownDuringGeneration_RequeuesTheUnchangedRequestWithoutIncrementingAttempt()
    {
        var ct = TestContext.Current.CancellationToken;
        var entered = new SemaphoreSlim(0);
        var generator = BlockingGenerator(entered);
        await using var host = NewHost(generator);
        var movie = host.AddMovie(806, "A stored plot.");
        await host.Topology.EnsureDeclaredAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.FeaturesRequestedQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.FeaturesRetryQueue, ct);
        await host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 1), ct);

        await host.StartAsync(ct);
        await entered.WaitAsync(PollDeadline, ct);
        await host.StopAsync(CancellationToken.None);

        var requeued = await probe.PollGetMatchingAsync(
            RabbitMqTopology.FeaturesRequestedQueue,
            candidate => Read(candidate).MovieId.Value == movie.Value,
            ct);
        requeued.ShouldNotBeNull();
        Read(requeued).Attempt.ShouldBe(1);
        RawHeader(requeued, FailureCategoryHeader).ShouldBeNull();
        RawHeader(requeued, FailureReasonHeader).ShouldBeNull();
        RawHeader(requeued, FailureAttemptHeader).ShouldBeNull();
        host.Features.UpsertCount.ShouldBe(0);
        (await ReadyMessageCountAsync(RabbitMqTopology.FeaturesDeadLetterQueue, ct)).ShouldBe(0U);
    }

    [Fact]
    public async Task Consumer_RepeatedShutdownRequeue_RecordsResidenceHeadersAndApplicationAttempt()
    {
        var ct = TestContext.Current.CancellationToken;
        await fixture.DeleteTopologyAsync(ct);
        var entered = new SemaphoreSlim(0);
        var generator = BlockingUntilCallAsync(entered, blockingCalls: 2);
        var movies = new PlotMovieRepository();
        var features = new RecordingFeaturesRepository();
        var movie = new MovieId(807);
        movies.Add(movie, "A stored plot.");

        for (var cycle = 1; cycle <= 2; cycle++)
        {
            await using var host = NewHost(generator, maxAttempts: 10, movies: movies, features: features);
            await host.Topology.EnsureDeclaredAsync(ct);
            if (cycle == 1)
            {
                await probe.DrainAsync(RabbitMqTopology.FeaturesRequestedQueue, ct);
                await probe.DrainAsync(RabbitMqTopology.FeaturesRetryQueue, ct);
                await host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 1), ct);
            }

            await host.StartAsync(ct);
            await entered.WaitAsync(PollDeadline, ct);
            await host.StopAsync(CancellationToken.None);

            (await ReadyMessageCountAsync(RabbitMqTopology.FeaturesRequestedQueue, ct))
                .ShouldBeGreaterThanOrEqualTo(1U, $"cycle {cycle} did not leave the delivery on features.requested");
            (await ReadyMessageCountAsync(RabbitMqTopology.FeaturesDeadLetterQueue, ct))
                .ShouldBe(0U, $"cycle {cycle} unexpectedly reached the features dead-letter queue");
        }

        await using (var recorder = NewHost(generator, maxAttempts: 10, movies: movies, features: features))
        {
            var observed = await PeekAndRequeueAsync(recorder, ct);
            observed.ShouldNotBeNull();
            Read(observed).Attempt.ShouldBe(1);
            RawHeader(observed, FailureCategoryHeader).ShouldBeNull();
            RawHeader(observed, FailureReasonHeader).ShouldBeNull();
            RawHeader(observed, FailureAttemptHeader).ShouldBeNull();
            Header(observed, TraceContextCarrier.TraceParentHeader).ShouldStartWith("00-");
        }

        await using (var finalHost = NewHost(generator, maxAttempts: 10, movies: movies, features: features))
        {
            await finalHost.StartAsync(ct);
            var snapshot = await WaitForSnapshotAsync(finalHost, movie, ct);
            await finalHost.StopAsync(CancellationToken.None);
            snapshot.MovieId.ShouldBe(movie);
        }

        features.UpsertCount.ShouldBe(1);
    }

    [Fact]
    public async Task Consumer_AFailedRetryPublication_DoesNotSuccessAckAndLeavesNoSnapshot()
    {
        var ct = TestContext.Current.CancellationToken;
        var generator = FailingGenerator(
            new EmbeddingGenerationException(EnrichmentFailureCategory.ProviderUnavailable));
        await using var host = NewHost(generator);
        var movie = host.AddMovie(808, "A stored plot.");
        await host.Topology.EnsureDeclaredAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.FeaturesRequestedQueue, ct);
        await host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 1), ct);
        await host.CloseFeaturesRetryQueueAsync(ct);

        await host.StartAsync(ct);
        var dead = await probe.PollGetMatchingAsync(
            RabbitMqTopology.FeaturesDeadLetterQueue,
            candidate => Read(candidate).MovieId.Value == movie.Value,
            ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        host.Features.UpsertCount.ShouldBe(0);
        (await ReadyMessageCountAsync(RabbitMqTopology.FeaturesRequestedQueue, ct)).ShouldBe(0U);
    }

    [Fact]
    public async Task Consumer_LogsRetainIdentifiersAndCategoryWithoutPlotTextVectorOrRawExceptionText()
    {
        var ct = TestContext.Current.CancellationToken;
        const string plotSentinel = "TOP-SECRET-PLOT-9f3c";
        const string exceptionSentinel = "RAW-EXCEPTION-TEXT-77b1";
        var generator = FailingGenerator(
            new EmbeddingGenerationException(
                EnrichmentFailureCategory.ProviderUnavailable,
                $"provider failed {exceptionSentinel}"));
        var log = new CapturingLog();
        await using var host = NewHost(generator, log: log);
        var movie = host.AddMovie(809, plotSentinel);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new FeaturesRequested(movie, 1), ct);
        var retried = await probe.PollGetMatchingAsync(
            RabbitMqTopology.FeaturesRetryQueue,
            candidate => Read(candidate).MovieId.Value == movie.Value,
            ct);
        await host.StopAsync(CancellationToken.None);

        retried.ShouldNotBeNull();
        var messages = log.Messages;
        messages.ShouldContain(entry => entry.Contains(movie.Value.ToString(), StringComparison.Ordinal));
        messages.ShouldContain(entry => entry.Contains("attempt 1", StringComparison.Ordinal));
        messages.ShouldContain(entry => entry.Contains("provider_unavailable", StringComparison.Ordinal));
        messages.ShouldAllBe(entry => !entry.Contains(plotSentinel, StringComparison.Ordinal));
        messages.ShouldAllBe(entry => !entry.Contains(exceptionSentinel, StringComparison.Ordinal));
        messages.ShouldAllBe(entry => !entry.Contains("System.Single[]", StringComparison.Ordinal));
    }

    private static EnrichmentFailureCategory ParseCategory(string code) =>
        code switch
        {
            "invalid_response" => EnrichmentFailureCategory.InvalidResponse,
            "provider_unavailable" => EnrichmentFailureCategory.ProviderUnavailable,
            _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unsupported failure category."),
        };

    private static RecordingEmbeddingGenerator SuccessfulGenerator() =>
        new((values, token) => new FakeEmbeddingGenerator().GenerateAsync(values, cancellationToken: token));

    private static RecordingEmbeddingGenerator FailingGenerator(Exception failure) =>
        new((_, _) => Task.FromException<GeneratedEmbeddings<Embedding<float>>>(failure));

    private static RecordingEmbeddingGenerator BlockingGenerator(SemaphoreSlim entered) =>
        new(async (values, token) =>
        {
            entered.Release();
            await Task.Delay(Timeout.Infinite, token);
            return await new FakeEmbeddingGenerator().GenerateAsync(values, cancellationToken: token);
        });

    private static RecordingEmbeddingGenerator BlockingUntilCallAsync(SemaphoreSlim entered, int blockingCalls)
    {
        var calls = 0;
        return new RecordingEmbeddingGenerator(async (values, token) =>
        {
            if (Interlocked.Increment(ref calls) <= blockingCalls)
            {
                entered.Release();
                await Task.Delay(Timeout.Infinite, token);
            }

            return await new FakeEmbeddingGenerator().GenerateAsync(values, cancellationToken: token);
        });
    }

    private static FeaturesRequested Read(BasicGetResult message) =>
        JsonSerializer.Deserialize<FeaturesRequested>(
            Encoding.UTF8.GetString(message.Body.Span),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldNotBeNull();

    private static string? Header(BasicGetResult message, string name) =>
        TraceContextCarrier.TryReadHeader(message.BasicProperties.Headers, name, out var value) ? value : null;

    private static object? RawHeader(BasicGetResult message, string name) =>
        message.BasicProperties.Headers is { } headers && headers.TryGetValue(name, out var value)
            ? value
            : null;

    private async Task<uint> ReadyMessageCountAsync(string queue, CancellationToken ct) =>
        (await probe.DeclarePassiveAsync(queue, ct)).MessageCount;

    private static async Task<MovieFeaturesSnapshot> WaitForSnapshotAsync(
        FeaturesHost host,
        MovieId movie,
        CancellationToken ct)
    {
        var deadline = TimeProvider.System.GetUtcNow() + PollDeadline;
        MovieFeaturesSnapshot? snapshot = null;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            snapshot = host.Features.Find(movie);
            if (snapshot is not null)
            {
                return snapshot;
            }

            await Task.Delay(PollInterval, ct);
        }

        return snapshot.ShouldNotBeNull("the features consumer did not persist a snapshot in time");
    }

    private static async Task WaitUntilAsync(Func<CancellationToken, Task<bool>> condition, CancellationToken ct)
    {
        var deadline = TimeProvider.System.GetUtcNow() + PollDeadline;
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            if (await condition(ct))
            {
                return;
            }

            await Task.Delay(PollInterval, ct);
        }

        (await condition(ct)).ShouldBeTrue();
    }

    private static async Task<BasicGetResult?> PeekAndRequeueAsync(FeaturesHost host, CancellationToken ct)
    {
        var connection = await host.Owner.GetAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        var result = await channel.BasicGetAsync(
            RabbitMqTopology.FeaturesRequestedQueue,
            autoAck: false,
            cancellationToken: ct);
        if (result is not null)
        {
            await channel.BasicNackAsync(result.DeliveryTag, multiple: false, requeue: true, ct);
        }

        return result;
    }

    private FeaturesHost NewHost(
        RecordingEmbeddingGenerator generator,
        int maxAttempts = DefaultMaxAttempts,
        TimeSpan? retryDelay = null,
        PlotMovieRepository? movies = null,
        RecordingFeaturesRepository? features = null,
        CapturingLog? log = null)
    {
        var options = fixture.Options with { RetryDelay = retryDelay ?? fixture.Options.RetryDelay };
        var owner = new RabbitMqConnectionOwner(Options.Create(options));
        var enrichment = Options.Create(new EnrichmentOptions
        {
            MaxAttempts = maxAttempts,
            ClaimLease = TimeSpan.FromSeconds(5),
        });
        var topology = new RabbitMqTopology(owner, Options.Create(options), enrichment);
        var publisher = new RabbitMqEnrichmentQueuePublisher(topology, owner);
        var movieRepository = movies ?? new PlotMovieRepository();
        var featureRepository = features ?? new RecordingFeaturesRepository();
        ILoggerFactory loggerFactory = log is null ? NullLoggerFactory.Instance : new CapturingLoggerFactory(log);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(loggerFactory);
        services.AddSingleton(enrichment);
        services.AddSingleton(enrichment.Value);
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(ComputedAt));
        services.AddSingleton<IMovieRepository>(movieRepository);
        services.AddSingleton<IMovieFeaturesRepository>(featureRepository);
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(generator);
        services.AddSingleton<RabbitMqConnectionOwner>(_ => owner);
        services.AddSingleton(topology);
        services.AddSingleton(publisher);
        services.AddHandler<ProcessFeaturesCommandHandler, ProcessFeaturesCommand, ProcessFeaturesOutcome>();

        var built = services.BuildServiceProvider();
        var consumer = new FeaturesConsumer(
            owner,
            topology,
            publisher,
            built.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            loggerFactory.CreateLogger<FeaturesConsumer>(),
            new FeaturesConsumerLiveness());
        return new FeaturesHost(owner, topology, publisher, movieRepository, featureRepository, consumer, built);
    }

    private sealed class FeaturesHost(
        RabbitMqConnectionOwner owner,
        RabbitMqTopology topology,
        RabbitMqEnrichmentQueuePublisher publisher,
        PlotMovieRepository movies,
        RecordingFeaturesRepository features,
        FeaturesConsumer consumer,
        ServiceProvider services) : IAsyncDisposable
    {
        public RabbitMqTopology Topology { get; } = topology;

        public RabbitMqConnectionOwner Owner { get; } = owner;

        public RabbitMqEnrichmentQueuePublisher Publisher { get; } = publisher;

        public RecordingFeaturesRepository Features { get; } = features;

        public Task StartAsync(CancellationToken cancellationToken) => consumer.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => consumer.StopAsync(cancellationToken);

        public MovieId AddMovie(int id, string? plot)
        {
            var movieId = new MovieId(id);
            movies.Add(movieId, plot);
            return movieId;
        }

        public async Task CloseFeaturesRetryQueueAsync(CancellationToken cancellationToken)
        {
            var connection = await Owner.GetAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.QueueDeleteAsync(RabbitMqTopology.FeaturesRetryQueue, cancellationToken: cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            services.Dispose();
            return Owner.DisposeAsync();
        }
    }

    private sealed class PlotMovieRepository : IMovieRepository
    {
        private readonly Dictionary<MovieId, string?> plots = [];

        public void Add(MovieId id, string? plot) => plots[id] = plot;

        public Task<Movie?> GetAsync(MovieId id, CancellationToken ct) => Task.FromResult<Movie?>(null);

        public Task AddAsync(Movie movie, CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TryDeleteAsync(MovieId id, CancellationToken ct) => Task.FromResult(false);

        public Task<MovieId> NextIdentityAsync(CancellationToken ct) => Task.FromResult(new MovieId(plots.Count + 1));

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct) => Task.FromResult(false);

        public Task<MovieId?> FindMovieIdByImdbIdAsync(ImdbId imdbId, CancellationToken ct) =>
            Task.FromResult<MovieId?>(null);

        public Task<IReadOnlyList<MovieId>> FindStrandedMovieIdsAsync(DateTimeOffset leaseCutoff, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MovieId>>([]);

        public Task<string?> GetPlotAsync(MovieId id, CancellationToken ct) =>
            Task.FromResult(plots.TryGetValue(id, out var plot) ? plot : null);
    }

    private sealed class RecordingFeaturesRepository : IMovieFeaturesRepository
    {
        private readonly ConcurrentDictionary<MovieId, MovieFeaturesSnapshot> snapshots = new();
        private int upsertCount;

        public int UpsertCount => Volatile.Read(ref upsertCount);

        public MovieFeaturesSnapshot? Find(MovieId movieId) =>
            snapshots.TryGetValue(movieId, out var snapshot) ? snapshot : null;

        public Task<MovieFeaturesSnapshot?> GetByMovieIdAsync(
            MovieId movieId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshots.TryGetValue(movieId, out var snapshot) ? snapshot : null);

        public Task UpsertAsync(MovieFeaturesSnapshot features, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            snapshots[features.MovieId] = features;
            Interlocked.Increment(ref upsertCount);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MovieId>> FindMovieIdsMissingFeaturesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MovieId>>([]);
    }

    private sealed class CapturingLog
    {
        private readonly ConcurrentQueue<string> messages = new();

        public IReadOnlyCollection<string> Messages => messages;

        public void Add(string message) => messages.Enqueue(message);
    }

    private sealed class CapturingLoggerFactory(CapturingLog log) : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(log, categoryName);

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger(CapturingLog log, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull =>
            null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _ = eventId;
            log.Add($"{category}: {formatter(state, exception)} {exception?.Message}");
        }
    }
}
