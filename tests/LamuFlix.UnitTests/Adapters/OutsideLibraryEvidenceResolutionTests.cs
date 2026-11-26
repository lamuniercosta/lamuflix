using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.UnitTests.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LamuFlix.UnitTests.Adapters;

public sealed class OutsideLibraryEvidenceResolutionTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 23, 59, 0, TimeSpan.Zero);

    private const string FoundBody = """
        { "Response": "True", "Title": "Candidate", "Type": "movie", "imdbID": "tt0111161", "imdbRating": "8.1", "imdbVotes": "25000" }
        """;

    [Fact]
    public async Task GetCandidateEvidenceAsync_FoundResponse_ParsesRatingAndVotesAfterOneCharge()
    {
        // arrange
        var handler = new OmdbHandler((HttpStatusCode.OK, FoundBody));
        var provider = CreateOmdbProvider(handler);

        // act
        var result = await provider.GetCandidateEvidenceAsync("tt0111161", TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(CandidateLookupStatus.Evidence);
        var evidence = result.Evidence.ShouldNotBeNull();
        evidence.Rating.ShouldBe("8.1");
        evidence.Votes.ShouldBe("25000");
        handler.GateAttempts.ShouldBe(1);
        handler.Sends.ShouldHaveSingleItem().Query.ShouldContain("i=tt0111161");
    }

    [Fact]
    public async Task GetCandidateEvidenceAsync_ExplicitNotFound_IsConfirmedAbsence()
    {
        // arrange
        var handler = new OmdbHandler(
            (HttpStatusCode.OK, """{ "Response": "False", "Error": "Movie not found!" }"""));

        // act
        var result = await CreateOmdbProvider(handler)
            .GetCandidateEvidenceAsync("tt0111161", TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(CandidateLookupStatus.ConfirmedAbsence);
        result.Evidence.ShouldNotBeNull().ProviderConfirmedAbsent.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCandidateEvidenceAsync_MalformedInputId_MakesNoSendOrCharge()
    {
        // arrange
        var handler = new OmdbHandler((HttpStatusCode.OK, FoundBody));

        // act
        var result = await CreateOmdbProvider(handler).GetCandidateEvidenceAsync("tt-not-an-id", TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(CandidateLookupStatus.Invalid);
        handler.GateAttempts.ShouldBe(0);
        handler.Sends.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCandidateEvidenceAsync_MalformedIdEchoedByProvider_IsUnresolvedButCharged()
    {
        // arrange
        var body = FoundBody.Replace("tt0111161", "not-an-id", StringComparison.Ordinal);
        var handler = new OmdbHandler((HttpStatusCode.OK, body));

        // act
        var result = await CreateOmdbProvider(handler).GetCandidateEvidenceAsync("tt0111161", TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(CandidateLookupStatus.Unresolved);
        handler.GateAttempts.ShouldBe(1);
        handler.Sends.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetCandidateEvidenceAsync_QuotaDenialAtSendBoundary_DispatchesNothing()
    {
        // arrange
        var handler = new OmdbHandler((HttpStatusCode.OK, FoundBody)) { Deny = true };

        // act
        var result = await CreateOmdbProvider(handler).GetCandidateEvidenceAsync("tt0111161", TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(CandidateLookupStatus.QuotaDenied);
        handler.GateAttempts.ShouldBe(1);
        handler.Sends.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResolveAsync_CachedImdbId_SkipsTmdbAndWritesDurableEvidence()
    {
        // arrange
        var harness = new Harness((HttpStatusCode.OK, FoundBody));
        var cached = new CandidateImdbEvidence("tt0111161", 77, 8.0m, 4000, CandidateEvidenceOutcome.Complete, Now.AddDays(30));

        // act
        var resolved = await harness.ResolveAsync(201, cached, ceiling: 2);

        // assert
        resolved.ShouldNotBeNull();
        harness.Tmdb.Requests.ShouldBeEmpty();
        harness.Omdb.Sends.Count.ShouldBe(1);
        harness.Writer.Records.ShouldHaveSingleItem().Payload.CollectionId.ShouldBe(77);
    }

    [Fact]
    public async Task ResolveAsync_MalformedCachedImdbId_DeferredWithZeroCallsAndCharges()
    {
        // arrange
        var harness = new Harness((HttpStatusCode.OK, FoundBody));
        var cached = new CandidateImdbEvidence("bad", null, 8.0m, 4000, CandidateEvidenceOutcome.Complete, Now.AddDays(30));

        // act
        var resolved = await harness.ResolveAsync(201, cached, ceiling: 2);

        // assert
        resolved.ShouldBeNull();
        harness.Tmdb.Requests.ShouldBeEmpty();
        harness.Omdb.GateAttempts.ShouldBe(0);
        harness.Writer.Records.ShouldBeEmpty();
    }

    [Fact]
    public async Task RankAdmittedAsync_FreshCacheHit_UsesNoProviderCall()
    {
        // arrange
        var harness = new Harness((HttpStatusCode.OK, FoundBody));
        Dictionary<int, CandidateImdbEvidence> evidence = new()
        {
            [201] = Complete(Now.AddDays(30)),
        };

        // act
        var cards = await OutsideLibraryRanking.RankAdmittedAsync(
            Snapshot(Contributions(201, 2)),
            Options(),
            evidence,
            (_, _) => ValueTask.FromResult<CandidateImdbEvidence?>(null),
            TestContext.Current.CancellationToken,
            new FixedTimeProvider(Now));

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([201]);
        harness.Tmdb.Requests.ShouldBeEmpty();
        harness.Omdb.GateAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task RankAdmittedAsync_ExpiredEvidence_ResolvesSeriallyInRankOrder()
    {
        // arrange
        var harness = new Harness((HttpStatusCode.OK, FoundBody));
        harness.Tmdb.Respond("/3/movie/201/external_ids", """{ "imdb_id": "tt0111161" }""");
        harness.Tmdb.Respond("/3/movie/202/external_ids", """{ "imdb_id": "tt0111162" }""");
        harness.Tmdb.Respond("/3/movie/203/external_ids", """{ "imdb_id": "tt0111163" }""");
        Dictionary<int, CandidateImdbEvidence> evidence = new()
        {
            [201] = Complete(Now.AddDays(-1)),
            [202] = Complete(Now.AddDays(-1)),
            [203] = Complete(Now.AddDays(-1)),
        };

        var budget = new OutsideLibraryEvidenceBudget(3, DateOnly.FromDateTime(Now.UtcDateTime));

        // act
        await OutsideLibraryRanking.RankAdmittedAsync(
            Snapshot(Contributions(201, 3)),
            Options(),
            evidence,
            (tmdbId, token) => harness.Resolver.ResolveAsync(tmdbId, null, budget, token),
            TestContext.Current.CancellationToken,
            new FixedTimeProvider(Now));

        // assert
        harness.Tmdb.Requests.Select(uri => uri.AbsolutePath).ShouldBe([
            "/3/movie/201/external_ids",
            "/3/movie/202/external_ids",
            "/3/movie/203/external_ids",
        ]);
    }

    [Fact]
    public async Task ResolveAsync_FailedAndEmptyExternalIds_CountAgainstTheSameCeiling()
    {
        // arrange
        var harness = new Harness();
        harness.Tmdb.Enqueue(HttpStatusCode.InternalServerError, string.Empty);
        harness.Tmdb.Enqueue(HttpStatusCode.OK, """{ "id": 202 }""");
        harness.Tmdb.Enqueue(HttpStatusCode.OK, """{ "id": 203 }""");
        var budget = new OutsideLibraryEvidenceBudget(2, DateOnly.FromDateTime(Now.UtcDateTime));

        // act
        var first = await harness.Resolver.ResolveAsync(201, null, budget, TestContext.Current.CancellationToken);
        var second = await harness.Resolver.ResolveAsync(202, null, budget, TestContext.Current.CancellationToken);
        var third = await harness.Resolver.ResolveAsync(203, null, budget, TestContext.Current.CancellationToken);

        // assert
        first.ShouldBeNull();
        second.ShouldBeNull();
        third.ShouldBeNull();
        harness.Tmdb.Requests.Count.ShouldBe(2);
        harness.Omdb.GateAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task RankAdmittedAsync_MidScanDenial_AdmitsLowerCachedSurvivorWithoutEscaping()
    {
        // arrange
        var harness = new Harness((HttpStatusCode.OK, FoundBody));
        harness.Omdb.Deny = true;
        Dictionary<int, CandidateImdbEvidence> evidence = new()
        {
            [201] = Complete(Now.AddDays(-1)),
            [202] = Complete(Now.AddDays(30)),
        };

        var budget = new OutsideLibraryEvidenceBudget(2, DateOnly.FromDateTime(Now.UtcDateTime));

        // act
        var cards = await OutsideLibraryRanking.RankAdmittedAsync(
            Snapshot(Contributions(201, 2)),
            Options(),
            evidence,
            (tmdbId, token) => harness.Resolver.ResolveAsync(
                tmdbId,
                evidence.GetValueOrDefault(tmdbId),
                budget,
                token),
            TestContext.Current.CancellationToken,
            new FixedTimeProvider(Now));

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([202]);
        harness.Tmdb.Requests.ShouldBeEmpty();
        harness.Omdb.GateAttempts.ShouldBe(1);
        harness.Omdb.Sends.ShouldBeEmpty();
        harness.Writer.Records.ShouldBeEmpty();
        harness.Logger.Messages.ShouldContain(message => message.Contains("TMDb id 201", StringComparison.Ordinal));
        harness.Logger.Messages.ShouldAllBe(message =>
            !message.Contains("apikey", StringComparison.OrdinalIgnoreCase)
            && !message.Contains("http", StringComparison.OrdinalIgnoreCase)
            && !message.Contains("tt0111161", StringComparison.Ordinal)
            && !message.Contains("SECRET", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResolveAsync_CrossingMidnight_DoesNotReplenishTheFixedLoadCeiling()
    {
        // arrange
        var clock = new ManualTimeProvider(Now);
        var harness = new Harness(clock, (HttpStatusCode.OK, FoundBody));
        var budget = new OutsideLibraryEvidenceBudget(1, DateOnly.FromDateTime(Now.UtcDateTime));

        // act
        var first = await harness.Resolver.ResolveAsync(201, null, budget, TestContext.Current.CancellationToken);
        clock.Advance(TimeSpan.FromMinutes(2));
        var second = await harness.Resolver.ResolveAsync(202, null, budget, TestContext.Current.CancellationToken);

        // assert
        first.ShouldNotBeNull();
        second.ShouldBeNull();
        clock.GetUtcNow().Date.ShouldBe(Now.Date.AddDays(1));
        harness.Tmdb.Requests.Count.ShouldBe(1);
        harness.Omdb.GateAttempts.ShouldBe(1);
        budget.CacheOnly.ShouldBeFalse();
        budget.UsageDate.ShouldBe(DateOnly.FromDateTime(Now.UtcDateTime));
    }

    [Fact]
    public async Task ResolveAsync_ConcurrentSameKeyMiss_UsesOneLeaderAndNoWaiterCalls()
    {
        // arrange
        var harness = new Harness((HttpStatusCode.OK, FoundBody));
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var leaderBlocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Omdb.BeforeSend = async (_, _) =>
        {
            leaderBlocked.TrySetResult();
            await release.Task;
        };
        var budget = new OutsideLibraryEvidenceBudget(2, DateOnly.FromDateTime(Now.UtcDateTime));

        // act
        var leader = harness.Resolver.ResolveAsync(201, null, budget, TestContext.Current.CancellationToken).AsTask();
        await leaderBlocked.Task;
        var waiter = harness.Resolver.ResolveAsync(201, null, budget, TestContext.Current.CancellationToken).AsTask();
        await Task.Delay(100, TestContext.Current.CancellationToken);
        release.SetResult();
        var outcomes = await Task.WhenAll(leader, waiter);

        // assert
        outcomes[0].ShouldNotBeNull();
        outcomes[1].ShouldBe(outcomes[0]);
        harness.Tmdb.Requests.Count.ShouldBe(1);
        harness.Omdb.GateAttempts.ShouldBe(1);
        budget.UsedSends.ShouldBe(1);
    }

    [Fact]
    public async Task ResolveAsync_LeaderCancellationAndWaiterCancellation_DeferAndAllowLaterRetry()
    {
        // arrange
        var harness = new Harness((HttpStatusCode.OK, FoundBody));
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var leaderBlocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Omdb.BeforeSend = async (_, token) =>
        {
            leaderBlocked.TrySetResult();
            await release.Task.WaitAsync(token);
        };
        using var leaderCancellation = new CancellationTokenSource();
        using var waiterCancellation = new CancellationTokenSource();
        var firstBudget = new OutsideLibraryEvidenceBudget(2, DateOnly.FromDateTime(Now.UtcDateTime));

        // act
        var leader = harness.Resolver.ResolveAsync(201, null, firstBudget, leaderCancellation.Token).AsTask();
        await leaderBlocked.Task;
        var waiter = harness.Resolver.ResolveAsync(201, null, firstBudget, waiterCancellation.Token).AsTask();
        await waiterCancellation.CancelAsync();
        var waiterOutcome = await waiter;
        await leaderCancellation.CancelAsync();
        release.SetResult();
        var leaderOutcome = await leader;

        var retry = await harness.Resolver.ResolveAsync(
            201,
            null,
            new OutsideLibraryEvidenceBudget(2, DateOnly.FromDateTime(Now.UtcDateTime)),
            TestContext.Current.CancellationToken);

        // assert
        waiterOutcome.ShouldBeNull();
        leaderOutcome.ShouldBeNull();
        retry.ShouldNotBeNull();
        harness.Omdb.GateAttempts.ShouldBe(2);
    }

    private static CandidateImdbEvidence Complete(DateTimeOffset expiresAtUtc) =>
        new("tt0111161", null, 8.0m, 5000, CandidateEvidenceOutcome.Complete, expiresAtUtc);

    private static IEnumerable<OutsideLibraryRecommendationContribution> Contributions(
        int firstTmdbId,
        int count) =>
        Enumerable.Range(0, count)
            .SelectMany(index => (OutsideLibraryRecommendationContribution[])
            [
                Contribution(index * 2 + 1, firstTmdbId + index),
                Contribution(index * 2 + 2, firstTmdbId + index),
            ]);

    private static OutsideLibraryRecommendationContribution Contribution(int sourceId, int tmdbId) =>
        new(
            new MovieId(sourceId),
            $"Seed {sourceId}",
            Review.Liked,
            IsWatched: false,
            IsInWatchlist: false,
            tmdbId,
            $"Candidate {tmdbId}",
            2001,
            null);

    private static OutsideLibraryRecommendationSnapshot Snapshot(
        IEnumerable<OutsideLibraryRecommendationContribution> contributions) =>
        new([.. contributions], []);

    private static RecommendationOptions Options() =>
        new() { DiscoveryWeightLiked = 2, DiscoveryWeightWatched = 1 };

    private static OmdbMetadataProvider CreateOmdbProvider(OmdbHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost:9999/") },
            Microsoft.Extensions.Options.Options.Create(
                new OmdbOptions { ApiKey = "key", BaseUrl = "http://localhost:9999/" }),
            new FixedTimeProvider(Now),
            NullLogger<OmdbMetadataProvider>.Instance,
            new MetadataProviderHealthState());

    private sealed class Harness
    {
        public Harness(params (HttpStatusCode Status, string Body)[] omdbResponses)
            : this(new FixedTimeProvider(Now), omdbResponses)
        {
        }

        public Harness(TimeProvider clock, params (HttpStatusCode Status, string Body)[] omdbResponses)
        {
            var omdbHandler = new OmdbHandler(omdbResponses);
            Omdb = omdbHandler;
            Tmdb = new SequenceHandler();
            Writer = new RecordingEvidenceWriter(clock);
            OmdbProvider = CreateOmdbProvider(omdbHandler);
            TmdbProvider = new TmdbMetadataProvider(
                new HttpClient(Tmdb) { BaseAddress = new Uri("http://localhost:9999/3/") },
                Microsoft.Extensions.Options.Options.Create(new TmdbOptions { Enabled = true, ApiKey = "key" }),
                Microsoft.Extensions.Options.Options.Create(new OmdbOptions()),
                clock,
                NullLogger<TmdbMetadataProvider>.Instance,
                new SecondaryMetadataProviderHealthState());
            Resolver = new OutsideLibraryEvidenceResolver(
                writer: Writer,
                flights: new CandidateEvidenceFlightRegistry(),
                logger: Logger,
                tmdb: TmdbProvider,
                omdb: OmdbProvider);
        }

        public OmdbHandler Omdb { get; }

        public SequenceHandler Tmdb { get; }

        public RecordingEvidenceWriter Writer { get; }

        private OmdbMetadataProvider OmdbProvider { get; }

        private TmdbMetadataProvider TmdbProvider { get; }

        public OutsideLibraryEvidenceResolver Resolver { get; }

        public CapturingLogger Logger { get; } = new();

        public Task<CandidateImdbEvidence?> ResolveAsync(
            int tmdbId,
            CandidateImdbEvidence? cached,
            int ceiling) =>
            Resolver.ResolveAsync(
                tmdbId,
                cached,
                new OutsideLibraryEvidenceBudget(ceiling, DateOnly.FromDateTime(Now.UtcDateTime)),
                TestContext.Current.CancellationToken).AsTask();
    }

    private sealed class OmdbHandler(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> queue = new(responses);

        public bool Deny { get; set; }

        public int GateAttempts { get; private set; }

        public List<Uri> Sends { get; } = [];

        public Func<HttpRequestMessage, CancellationToken, Task>? BeforeSend { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            GateAttempts++;
            if (Deny)
            {
                throw new MetadataQuotaExhaustedException();
            }

            Sends.Add(request.RequestUri ?? throw new InvalidOperationException("Missing request URI."));
            if (BeforeSend is not null)
            {
                await BeforeSend(request, ct);
            }

            var (status, body) = queue.Count > 0
                ? queue.Dequeue()
                : (HttpStatusCode.InternalServerError, string.Empty);
            return new HttpResponseMessage(status) { Content = new StringContent(body) };
        }
    }

    private sealed class SequenceHandler : HttpMessageHandler
    {
        private readonly Queue<(HttpStatusCode Status, string Body)> responses = new();

        private readonly Dictionary<string, string> overrides = new(StringComparer.Ordinal);

        public List<Uri> Requests { get; } = [];

        public void Respond(string path, string body) => overrides[path] = body;

        public void Enqueue(HttpStatusCode status, string body) => responses.Enqueue((status, body));

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var uri = request.RequestUri ?? throw new InvalidOperationException("Missing request URI.");
            Requests.Add(uri);
            var (status, body) = overrides.TryGetValue(uri.AbsolutePath, out var overrideBody)
                ? (HttpStatusCode.OK, overrideBody)
                : responses.Count > 0 ? responses.Dequeue() : (HttpStatusCode.OK, """{ "imdb_id": "tt0111161" }""");
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    private sealed class RecordingEvidenceWriter(TimeProvider clock) : ICandidateEvidenceWriter
    {
        public List<(int TmdbId, CandidateEvidencePayload Payload)> Records { get; } = [];

        public Task<CandidateImdbEvidence> RecordAsync(
            int tmdbId,
            CandidateEvidencePayload payload,
            CancellationToken cancellationToken)
        {
            Records.Add((tmdbId, payload));
            // ReSharper disable once MethodHasAsyncOverload
            return Task.FromResult(CandidateEvidenceParser.Parse(payload, clock.GetUtcNow()));
        }
    }

    private sealed class CapturingLogger : ILogger<OutsideLibraryEvidenceResolver>
    {
        private readonly ConcurrentBag<string> messages = [];

        public IReadOnlyCollection<string> Messages => messages;

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            messages.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
