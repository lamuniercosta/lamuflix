using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Api;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Pipeline;
using LamuFlix.ServiceDefaults;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(SecondaryMetadataProviderCollection.Name)]
public sealed class BackfillTmdbRecommendationsTests(
    PostgresFixture fixture,
    SecondaryMetadataProviderProbe probe)
    : IClassFixture<PostgresFixture>, IClassFixture<SecondaryMetadataProviderProbe>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    private const string MatchImdb = "tt0000101";
    private const string UnmatchedImdb = "tt0000102";
    private const string FailedImdb = "tt0000103";
    private const string StoredImdb = "tt0000104";
    private const int MatchTmdbId = 701;
    private const int FailedTmdbId = 703;
    private const int StoredTmdbId = 999;

    public async ValueTask InitializeAsync()
    {
        await probe.ResetAsync(TestContext.Current.CancellationToken);
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task RunAsync_AbsentMigration_RefusesBeforeAnyProviderWork()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var unmigrated = await fixture.CreateEmptyDatabaseContextAsync(ct);
        await using var services = BuildServices(probe, unmigrated);
        var output = new StringWriter();

        // act
        var exitCode = await BackfillOperatorMode.RunAsync(services, output, ct);

        // assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldContain("refused reason=migration-not-applied");
        probe.RequestCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false, "configured-key")]
    [InlineData(true, "")]
    [InlineData(true, "   ")]
    public async Task RunAsync_TmdbNotConfigured_RefusesBeforeAnyProviderWork(bool enabled, string apiKey)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var seed = fixture.CreateMigratedContext();
        var id = await SeedEligibleMovieAsync(seed, "Solaris", MatchImdb);
        await using var services = BuildServices(
            probe,
            seed,
            new Dictionary<string, string?>
            {
                [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] =
                    enabled.ToString(CultureInfo.InvariantCulture),
                [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.ApiKey)}"] = apiKey,
            });
        var output = new StringWriter();

        // act
        var exitCode = await BackfillOperatorMode.RunAsync(services, output, ct);

        // assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldContain("refused reason=tmdb-not-configured");
        probe.RequestCount.ShouldBe(0);
        (await RecordAsync(id, ct)).TmdbId.ShouldBeNull();
    }

    [Fact]
    public void MalformedInvocation_IsRejectedBeforeAnyServicesAreBuilt()
    {
        // arrange
        var args = new[] { BackfillOperatorMode.Flag, "--environment", "Development" };

        // act
        var requested = BackfillOperatorMode.IsRequested(args);
        var parsed = BackfillOperatorMode.TryParse(args, out var error);

        // assert
        requested.ShouldBeTrue();
        parsed.ShouldBeFalse();
        error.ShouldNotBeNull();
        probe.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task RunAsync_FullLibrary_ReportsTruthfulCountsAndCommitsOnlyCompleteMatches()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var seed = fixture.CreateMigratedContext();
        var skippedWithoutImdb = await SeedMovieAsync(seed, "No Identity", imdbId: null);
        var matched = await SeedEligibleMovieAsync(seed, "The Matrix", MatchImdb);
        var unmatched = await SeedEligibleMovieAsync(seed, "Missing On Tmdb", UnmatchedImdb);
        var failed = await SeedEligibleMovieAsync(seed, "Failed First Page", FailedImdb);
        var stored = await SeedEligibleMovieAsync(seed, "Already Stored", StoredImdb, StoredTmdbId);
        StubMatch(MatchImdb, MatchTmdbId, [802, 803]);
        StubEmptyFind(UnmatchedImdb);
        StubMatch(FailedImdb, FailedTmdbId, recommendations: null, statusCode: 400);
        var logStart = probe.Logs.Entries.Count;
        await using var services = BuildServices(probe, seed);
        var output = new StringWriter();

        // act
        var exitCode = await BackfillOperatorMode.RunAsync(services, output, ct);

        // assert
        exitCode.ShouldBe(0);
        output.ToString().Trim().ShouldBe(
            "tmdb-backfill: imdb-eligible=4 matched=1 unmatched=1 skipped=2 failures=1");
        var matchedRecord = await RecordAsync(matched, ct);
        matchedRecord.TmdbId.ShouldBe(MatchTmdbId);
        matchedRecord.Recommendations.ShouldBe([802, 803]);
        (await RecordAsync(unmatched, ct)).TmdbId.ShouldBeNull();
        (await RecordAsync(failed, ct)).TmdbId.ShouldBeNull();
        (await RecordAsync(skippedWithoutImdb, ct)).TmdbId.ShouldBeNull();
        (await RecordAsync(stored, ct)).TmdbId.ShouldBe(StoredTmdbId);
        (await RecordAsync(failed, ct)).Recommendations.ShouldBeEmpty();
        FindRequests().ShouldBe(3);
        RecommendationsRequests(MatchTmdbId).ShouldBe(1);
        RecommendationsRequests(FailedTmdbId).ShouldBe(1);
        probe.QueryValues("page").ShouldBe(["1"]);
        probe.RequestPaths.ShouldNotContain(path => path.Contains(StoredImdb, StringComparison.Ordinal));
        var corpus = LogCorpus(logStart);
        corpus.ShouldNotContain(SecondaryMetadataProviderProbe.SentinelApiKey);
        corpus.ShouldNotContain(Uri.EscapeDataString(SecondaryMetadataProviderProbe.SentinelApiKey));
        output.ToString().ShouldNotContain(SecondaryMetadataProviderProbe.SentinelApiKey);
    }

    [Fact]
    public async Task RunAsync_Rerun_SkipsCommittedRowsAndRetriesTheEligibleOnes()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var seed = fixture.CreateMigratedContext();
        var matched = await SeedEligibleMovieAsync(seed, "The Matrix", MatchImdb);
        var failed = await SeedEligibleMovieAsync(seed, "Failed First Page", FailedImdb);
        StubMatch(MatchImdb, MatchTmdbId, [802]);
        StubMatch(FailedImdb, FailedTmdbId, recommendations: null, statusCode: 400);
        await using var services = BuildServices(probe, seed);
        var firstRun = new StringWriter();
        (await BackfillOperatorMode.RunAsync(services, firstRun, ct)).ShouldBe(0);

        // act
        await probe.ResetAsync(ct);
        StubMatch(FailedImdb, FailedTmdbId, [902]);
        var secondRun = new StringWriter();
        var exitCode = await BackfillOperatorMode.RunAsync(services, secondRun, ct);

        // assert
        exitCode.ShouldBe(0);
        firstRun.ToString().Trim().ShouldBe("tmdb-backfill: imdb-eligible=2 matched=1 unmatched=0 skipped=0 failures=1");
        secondRun.ToString().Trim().ShouldBe("tmdb-backfill: imdb-eligible=2 matched=1 unmatched=0 skipped=1 failures=0");
        probe.RequestPaths.ShouldNotContain(path => path.Contains(MatchImdb, StringComparison.Ordinal));
        (await RecordAsync(matched, ct)).TmdbId.ShouldBe(MatchTmdbId);
        var retried = await RecordAsync(failed, ct);
        retried.TmdbId.ShouldBe(FailedTmdbId);
        retried.Recommendations.ShouldBe([902]);
    }

    [Fact]
    public async Task RunAsync_ThrottledFind_RetriesWithinTheSharedPolicyAndMatches()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var seed = fixture.CreateMigratedContext();
        var matched = await SeedEligibleMovieAsync(seed, "The Matrix", MatchImdb);
        var findPath = string.Concat("/3/find/", MatchImdb);
        probe.Server
            .Given(Request.Create().WithPath(findPath).UsingGet())
            .InScenario("throttled")
            .WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "0"));
        probe.Server
            .Given(Request.Create().WithPath(findPath).UsingGet())
            .InScenario("throttled")
            .WhenStateIs("recovered")
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(FoundBody(MatchTmdbId)));
        probe.StubDetails(MatchTmdbId, DetailsBody(MatchTmdbId, MatchImdb));
        probe.StubPath(RecommendationsPath(MatchTmdbId), 200, RecommendationsBody([802]));
        await using var services = BuildServices(probe, seed);
        var output = new StringWriter();

        // act
        var exitCode = await BackfillOperatorMode.RunAsync(services, output, ct);

        // assert
        exitCode.ShouldBe(0);
        output.ToString().Trim().ShouldBe("tmdb-backfill: imdb-eligible=1 matched=1 unmatched=0 skipped=0 failures=0");
        probe.RequestPaths.Count(path => string.Equals(path, findPath, StringComparison.Ordinal)).ShouldBe(2);
        (await RecordAsync(matched, ct)).TmdbId.ShouldBe(MatchTmdbId);
    }

    [Fact]
    public async Task RunAsync_BreakerRejection_StopsWithPartialCountsAndASafeRerun()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var seed = fixture.CreateMigratedContext();
        var matched = await SeedEligibleMovieAsync(seed, "The Matrix", MatchImdb);
        var stopped = await SeedEligibleMovieAsync(seed, "Unavailable", UnmatchedImdb);
        StubMatch(MatchImdb, MatchTmdbId, [802]);
        probe.StubPath(string.Concat("/3/find/", UnmatchedImdb), 500);
        var overrides = new Dictionary<string, string?>
        {
            [SecondaryMetadataProviderProbe.Resilience(nameof(MetadataProviderResilienceOptions.MinimumThroughput))] = "2",
        };
        await using var services = BuildServices(probe, seed, overrides);
        var output = new StringWriter();

        // act
        var exitCode = await BackfillOperatorMode.RunAsync(services, output, ct);

        // assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldContain("STOPPED reason=provider-unavailable");
        output.ToString().ShouldContain("matched=1");
        (await RecordAsync(matched, ct)).TmdbId.ShouldBe(MatchTmdbId);
        (await RecordAsync(stopped, ct)).TmdbId.ShouldBeNull();

        // act
        await probe.ResetAsync(ct);
        StubMatch(UnmatchedImdb, FailedTmdbId, [903]);
        await using var rerunServices = BuildServices(probe, seed);
        var rerunOutput = new StringWriter();
        var rerunExitCode = await BackfillOperatorMode.RunAsync(rerunServices, rerunOutput, ct);

        // assert
        rerunExitCode.ShouldBe(0);
        rerunOutput.ToString().Trim().ShouldBe("tmdb-backfill: imdb-eligible=2 matched=1 unmatched=0 skipped=1 failures=0");
        (await RecordAsync(stopped, ct)).TmdbId.ShouldBe(FailedTmdbId);
    }

    [Fact]
    public async Task RunAsync_CancelledDuringTheSecondMovie_StopsWithPartialCountsAndASafeRerun()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var seed = fixture.CreateMigratedContext();
        var matched = await SeedEligibleMovieAsync(seed, "The Matrix", MatchImdb);
        var pending = await SeedEligibleMovieAsync(seed, "Pending", UnmatchedImdb);
        StubMatch(MatchImdb, MatchTmdbId, [802]);
        StubMatch(UnmatchedImdb, FailedTmdbId, [903]);
        using var cancellation = new CancellationTokenSource();
        await using var services = BuildServices(
            probe,
            seed,
            cancelOnSecondLookup: true,
            cancellation: cancellation);
        var output = new StringWriter();

        // act
        var exitCode = await BackfillOperatorMode.RunAsync(services, output, cancellation.Token);

        // assert
        exitCode.ShouldBe(1);
        output.ToString().ShouldContain("STOPPED reason=cancelled");
        output.ToString().ShouldContain("matched=1");
        (await RecordAsync(matched, ct)).TmdbId.ShouldBe(MatchTmdbId);
        (await RecordAsync(pending, ct)).TmdbId.ShouldBeNull();

        // act
        await using var rerunServices = BuildServices(probe, seed);
        var rerunOutput = new StringWriter();
        var rerunExitCode = await BackfillOperatorMode.RunAsync(rerunServices, rerunOutput, ct);

        // assert
        rerunExitCode.ShouldBe(0);
        rerunOutput.ToString().Trim().ShouldBe("tmdb-backfill: imdb-eligible=2 matched=1 unmatched=0 skipped=1 failures=0");
        (await RecordAsync(pending, ct)).TmdbId.ShouldBe(FailedTmdbId);
    }

    [Fact]
    public async Task RunAsync_LegacyTitlelessRows_HydratesTheWholeSetAndSkipsOnRerun()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var seed = fixture.CreateMigratedContext();
        var legacy = await SeedLegacyRowsAsync(seed, "Legacy Matrix", MatchImdb, MatchTmdbId);
        StubMatch(MatchImdb, MatchTmdbId, [802, 803]);
        await using var services = BuildServices(probe, seed);

        // act
        var firstOutput = new StringWriter();
        (await BackfillOperatorMode.RunAsync(services, firstOutput, ct)).ShouldBe(0);
        var firstRunRequests = RecommendationsRequests(MatchTmdbId);
        await probe.ResetAsync(ct);
        StubMatch(MatchImdb, MatchTmdbId, [802, 803]);
        await using var rerunServices = BuildServices(probe, seed);
        var rerunOutput = new StringWriter();
        (await BackfillOperatorMode.RunAsync(rerunServices, rerunOutput, ct)).ShouldBe(0);

        // assert
        firstOutput.ToString().Trim().ShouldBe("tmdb-backfill: imdb-eligible=1 matched=1 unmatched=0 skipped=0 failures=0");
        rerunOutput.ToString().Trim().ShouldBe("tmdb-backfill: imdb-eligible=1 matched=0 unmatched=0 skipped=1 failures=0");
        await using var verify = fixture.CreateMigratedContext();
        var rows = await verify.Movies
            .AsNoTracking()
            .Include(movie => movie.Recommendations)
            .SingleAsync(movie => movie.Id == legacy, ct);
        rows.Recommendations.Select(row => row.RecommendedTmdbId).Order().ToArray().ShouldBe([802, 803]);
        rows.Recommendations.ShouldAllBe(row => row.Title == "Related");
        firstRunRequests.ShouldBe(1);
        RecommendationsRequests(MatchTmdbId).ShouldBe(0);
    }

    [Fact]
    public async Task RunAsync_LegacyTitlelessRows_FailedFetchRetainsThePriorSet()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var seed = fixture.CreateMigratedContext();
        var legacy = await SeedLegacyRowsAsync(seed, "Legacy Matrix", MatchImdb, MatchTmdbId);
        StubMatch(MatchImdb, MatchTmdbId, recommendations: null, statusCode: 400);
        await using var services = BuildServices(probe, seed);

        // act
        var output = new StringWriter();
        (await BackfillOperatorMode.RunAsync(services, output, ct)).ShouldBe(0);

        // assert
        output.ToString().Trim().ShouldBe("tmdb-backfill: imdb-eligible=1 matched=0 unmatched=0 skipped=0 failures=1");
        var state = await RecordAsync(legacy, ct);
        state.TmdbId.ShouldBe(MatchTmdbId);
        state.Recommendations.ShouldBe([9001]);
    }

    private static ServiceProvider BuildServices(
        SecondaryMetadataProviderProbe probe,
        LamuFlixDbContext db,
        IDictionary<string, string?>? overrides = null,
        bool cancelOnSecondLookup = false,
        CancellationTokenSource? cancellation = null)
    {
        var services = probe.NewServicesWithoutOptions();
        services.AddSingleton(probe.BuildConfiguration(overrides));
        services.AddLamuFlixOptions();
        services.AddMetadataProvider();
        services.AddSingleton(db);
        services.AddSingleton<IMovieRepository>(_ => Repository(db));
        services.AddHandler<
            BackfillTmdbRecommendationsCommandHandler,
            BackfillTmdbRecommendationsCommand,
            BackfillTmdbRecommendationsResult>();
        services.AddScoped<BackfillTmdbRecommendationsCommandHandler>(
            provider => new BackfillTmdbRecommendationsCommandHandler(
                provider.GetRequiredService<IMovieRepository>(),
                TmdbProvider(provider, cancelOnSecondLookup, cancellation),
                provider.GetRequiredService<ILogger<BackfillTmdbRecommendationsCommandHandler>>()));
        return services.BuildServiceProvider();
    }

    private static IMetadataProvider TmdbProvider(
        IServiceProvider provider,
        bool cancelOnSecondLookup,
        CancellationTokenSource? cancellation)
    {
        var tmdb = provider.GetRequiredKeyedService<IMetadataProvider>(TelemetryConstants.TmdbProviderName);
        return cancelOnSecondLookup && cancellation is not null
            ? new CancellingMetadataProvider(tmdb, cancellation)
            : tmdb;
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(
            db,
            new FixedTimeProvider(Now),
            Options.Create(new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);

    private static async Task<MovieId> SeedMovieAsync(
        LamuFlixDbContext db,
        string title,
        ImdbId? imdbId,
        int? tmdbId = null)
    {
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        await repository.AddAsync(
            Movie.Create(id, title, new LibraryPath($"C:/library/{Guid.NewGuid():N}/movie.mkv"), new MediaFormat("mkv")),
            TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        if (imdbId is null && tmdbId is null)
        {
            return id;
        }

        var movie = await repository.GetAsync(id, TestContext.Current.CancellationToken);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            new MovieMetadata(
                title,
                imdbId: imdbId,
                tmdbId: tmdbId,
                recommendedTmdbMovies: tmdbId is null
                    ? null
                    : [new RecommendedTmdbMovie(9001, "Stored related", null, null)]),
            Now);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private static Task<MovieId> SeedEligibleMovieAsync(
        LamuFlixDbContext db,
        string title,
        string imdbId,
        int? tmdbId = null) =>
        SeedMovieAsync(db, title, new ImdbId(imdbId), tmdbId);

    private static async Task<MovieId> SeedLegacyRowsAsync(
        LamuFlixDbContext db,
        string title,
        string imdbId,
        int tmdbId)
    {
        var repository = Repository(db);
        var id = await repository.NextIdentityAsync(TestContext.Current.CancellationToken);
        await repository.AddAsync(
            Movie.Create(id, title, new LibraryPath($"C:/library/{Guid.NewGuid():N}/movie.mkv"), new MediaFormat("mkv")),
            TestContext.Current.CancellationToken);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        var movie = await repository.GetAsync(id, TestContext.Current.CancellationToken);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(
            new MovieMetadata(
                title,
                imdbId: new ImdbId(imdbId),
                tmdbId: tmdbId,
                recommendedTmdbIds: [9001]),
            Now);
        await repository.SaveChangesAsync(TestContext.Current.CancellationToken);
        return id;
    }

    private async Task<MovieRecordState> RecordAsync(MovieId id, CancellationToken ct)
    {
        await using var verify = fixture.CreateMigratedContext();
        var record = await verify.Movies
            .AsNoTracking()
            .Include(movie => movie.Recommendations)
            .SingleAsync(movie => movie.Id == id, ct);
        return new MovieRecordState(
            record.TmdbId,
            [.. record.Recommendations.Select(row => row.RecommendedTmdbId).Order()]);
    }

    private void StubMatch(
        string imdbId,
        int tmdbId,
        IReadOnlyList<int>? recommendations,
        int statusCode = 200)
    {
        probe.StubFind(imdbId, FoundBody(tmdbId));
        probe.StubDetails(tmdbId, DetailsBody(tmdbId, imdbId));
        var body = recommendations is null ? string.Empty : RecommendationsBody(recommendations);
        probe.StubPath(RecommendationsPath(tmdbId), statusCode, body);
    }

    private void StubEmptyFind(string imdbId) =>
        probe.StubFind(imdbId, SecondaryMetadataProviderProbe.Fixture("Find", "empty.json"));

    private int FindRequests() => probe.RequestPaths.Count(path => path.StartsWith("/3/find/", StringComparison.Ordinal));

    private int RecommendationsRequests(int tmdbId) =>
        probe.RequestPaths.Count(path => string.Equals(path, RecommendationsPath(tmdbId), StringComparison.Ordinal));

    private static string RecommendationsPath(int tmdbId) =>
        string.Create(CultureInfo.InvariantCulture, $"/3/movie/{tmdbId}/recommendations");

    private static string FoundBody(int tmdbId) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $$"""
            {
              "movie_results": [
                { "adult": false, "id": {{tmdbId}}, "original_language": "en", "title": "Match", "video": false }
              ],
              "person_results": [],
              "tv_results": [],
              "tv_episode_results": [],
              "tv_season_results": []
            }
            """);

    private static string DetailsBody(int tmdbId, string imdbId) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $$"""
            {
              "id": {{tmdbId}},
              "title": "Match",
              "overview": "A match.",
              "release_date": "1999-03-30",
              "imdb_id": "{{imdbId}}",
              "original_language": "en"
            }
            """);

    private static string RecommendationsBody(IReadOnlyList<int> recommended) =>
        string.Concat(
            """{ "page": 1, "total_pages": 2, "total_results": 2, "results": [""",
            string.Join(", ", recommended.Select(id => string.Create(
                CultureInfo.InvariantCulture,
                $"{{ \"id\": {id}, \"title\": \"Related\" }}"))),
            "] }");

    private string LogCorpus(int start) =>
        string.Join("\n", probe.Logs.Entries.Skip(start).SelectMany(LogFragments));

    private static IEnumerable<string> LogFragments(MetadataProviderProbe.CapturedLog entry)
    {
        yield return entry.Message;
        yield return entry.ExceptionText;
        foreach (var pair in entry.State)
        {
            yield return pair.Value?.ToString() ?? string.Empty;
        }
    }

    private sealed record MovieRecordState(int? TmdbId, IReadOnlyList<int> Recommendations);

    private sealed class CancellingMetadataProvider(IMetadataProvider inner, CancellationTokenSource cancellation)
        : IMetadataProvider
    {
        private int calls;

        public Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)
        {
            if (Interlocked.Increment(ref calls) > 1)
            {
                cancellation.Cancel();
                ct.ThrowIfCancellationRequested();
            }

            return inner.FindAsync(lookup, ct);
        }
    }
}

