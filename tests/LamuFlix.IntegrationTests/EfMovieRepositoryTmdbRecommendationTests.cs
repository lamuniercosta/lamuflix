using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(SecondaryMetadataProviderCollection.Name)]
public sealed class EfMovieRepositoryTmdbRecommendationTests(PostgresFixture fixture, SecondaryMetadataProviderProbe probe)
    : IClassFixture<PostgresFixture>, IClassFixture<SecondaryMetadataProviderProbe>, IAsyncLifetime
{
    private const string RecommendationsPath = "/3/movie/603/recommendations";
    private const string RecommendationLogReason = "recommendations signal unavailable";

    private static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    private const string OmdbFoundBody = """
        {
          "Response": "True",
          "Type": "movie",
          "Title": "Solaris",
          "Year": "1972",
          "Plot": "A psychologist...",
          "imdbRating": "8.0",
          "imdbID": "tt0069293"
        }
        """;

    private const string FirstPageBody = """
        {
          "page": 1,
          "total_pages": 4,
          "total_results": 80,
          "results": [
            { "id": 604, "title": "The Matrix Reloaded", "release_date": "2003-05-15", "poster_path": "/reloaded.jpg" },
            { "id": 605, "title": "The Matrix Revolutions", "release_date": "2003-11-05" },
            { "id": 606, "title": "The Animatrix", "poster_path": "../secret.jpg" },
            { "id": 604, "title": "Duplicate", "release_date": "1999-01-01" }
          ]
        }
        """;

    private const string EmptyPageBody = """
        { "page": 1, "total_pages": 1, "total_results": 0, "results": [] }
        """;

    private const string ReplacementPageBody = """
        {
          "page": 1,
          "total_pages": 1,
          "results": [
            { "id": 701, "title": "Replacement one", "release_date": "2001-01-01" },
            { "id": 702, "title": "Replacement two", "poster_path": "/two.jpg" }
          ]
        }
        """;

    private const string MalformedPageBody = """
        { "page": 1, "total_pages": 2, "results": [ { "id": 0, "title": "Unusable" } ] }
        """;

    private const string Search604Body = """
        {
          "page": 1,
          "total_pages": 1,
          "total_results": 1,
          "results": [ { "id": 604, "title": "The Matrix Reloaded", "release_date": "2003-05-15" } ]
        }
        """;

    private const string Details604Body = """
        {
          "id": 604,
          "title": "The Matrix Reloaded",
          "overview": "The second chapter.",
          "release_date": "2003-05-15",
          "imdb_id": "tt0234215",
          "original_language": "en"
        }
        """;

    public async ValueTask InitializeAsync()
    {
        await probe.ResetAsync(TestContext.Current.CancellationToken);
        await fixture.ResetAsync(TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindAsync_FirstPageAdvertisingMorePages_RequestsItOnceAndPersistsTheIdentityAndRows()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        probe.StubFoundLookup();
        probe.StubPath(RecommendationsPath, 200, FirstPageBody);
        var services = probe.BuildServices();
        var logStart = probe.Logs.Entries.Count;

        // act
        var metadata = Metadata(await TmdbFindAsync(services, "The Matrix", ct));

        // assert
        metadata.TmdbId.ShouldBe(603);
        metadata.RecommendedTmdbIds.ShouldBe([604, 605, 606]);
        metadata.RecommendedTmdbMovies.ShouldBe(
        [
            new RecommendedTmdbMovie(604, "The Matrix Reloaded", 2003, "/reloaded.jpg"),
            new RecommendedTmdbMovie(605, "The Matrix Revolutions", 2003, null),
            new RecommendedTmdbMovie(606, "The Animatrix", null, null),
        ]);
        probe.RequestPaths.ShouldContain(RecommendationsPath);
        probe.RequestPaths.Count(path => path == RecommendationsPath).ShouldBe(1);
        probe.QueryValues("page").ShouldBe(["1"]);
        LogCorpus(logStart).ShouldNotContain(SecondaryMetadataProviderProbe.SentinelApiKey);
        LogCorpus(logStart).ShouldNotContain(Uri.EscapeDataString(SecondaryMetadataProviderProbe.SentinelApiKey));

        var id = await PersistEnrichmentAsync(metadata, ct);

        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadRecordAsync(verify, id, ct);
        record.TmdbId.ShouldBe(603);
        record.Recommendations.Select(recommendation => recommendation.RecommendedTmdbId).Order().ToArray()
            .ShouldBe([604, 605, 606]);
        var reloaded = record.Recommendations.Single(recommendation => recommendation.RecommendedTmdbId == 604);
        reloaded.Title.ShouldBe("The Matrix Reloaded");
        reloaded.ReleaseYear.ShouldBe(2003);
        reloaded.PosterPath.ShouldBe("/reloaded.jpg");
        var animatrix = record.Recommendations.Single(recommendation => recommendation.RecommendedTmdbId == 606);
        animatrix.Title.ShouldBe("The Animatrix");
        animatrix.ReleaseYear.ShouldBeNull();
        animatrix.PosterPath.ShouldBeNull();
    }

    [Fact]
    public async Task FindAsync_ChainWithPrimaryFound_CarriesTheTmdbIdentityAndRecommendations()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        probe.StubPath("/", 200, OmdbFoundBody);
        probe.StubFind("tt0069293", SecondaryMetadataProviderProbe.Fixture("Find", "found.json"));
        probe.StubDetails(603, SecondaryMetadataProviderProbe.Fixture("Details", "found.json"));
        probe.StubPath(RecommendationsPath, 200, FirstPageBody);
        var services = probe.BuildServices();

        // act
        var result = await services
            .GetRequiredService<IMetadataProvider>()
            .FindAsync(new MetadataLookup("Solaris", null), ct);

        // assert
        var metadata = Metadata(result);
        metadata.Title.ShouldBe("Solaris");
        metadata.TmdbId.ShouldBe(603);
        metadata.RecommendedTmdbIds.ShouldBe([604, 605, 606]);
    }

    [Fact]
    public async Task SaveChanges_SuccessfulEmptyFirstPage_ReplacesTheStoredRowsWithNone()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var metadata = await FirstPageMetadataAsync(ct);
        var id = await PersistEnrichmentAsync(metadata, ct);

        // act
        await probe.ResetAsync(ct);
        var reEnrichment = await EnrichAgainAsync(id, ct);
        var emptyMetadata = await FindWithRecommendationsAsync(EmptyPageBody, ct);
        emptyMetadata.RecommendedTmdbIds.ShouldBeEmpty();
        reEnrichment.Movie.MarkEnriched(emptyMetadata, Now.AddMinutes(1));
        await reEnrichment.Store.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadRecordAsync(verify, id, ct);
        record.TmdbId.ShouldBe(603);
        record.Recommendations.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveChanges_MalformedFirstPage_PreservesStoredRowsAndCarriesNoReplaceSet()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var metadata = await FirstPageMetadataAsync(ct);
        var id = await PersistEnrichmentAsync(metadata, ct);

        // act
        await probe.ResetAsync(ct);
        var reEnrichment = await EnrichAgainAsync(id, ct);
        var malformedMetadata = await FindWithRecommendationsAsync(MalformedPageBody, ct);
        malformedMetadata.TmdbId.ShouldBe(603);
        malformedMetadata.RecommendedTmdbIds.ShouldBeNull();
        reEnrichment.Movie.MarkEnriched(malformedMetadata, Now.AddMinutes(1));
        await reEnrichment.Store.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadRecordAsync(verify, id, ct);
        record.Recommendations.Select(recommendation => recommendation.RecommendedTmdbId).Order().ToArray()
            .ShouldBe([604, 605, 606]);
    }

    [Fact]
    public async Task SaveChanges_RecommendationsRequestFailure_PreservesStoredRowsAndLogsASafeOutcome()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var metadata = await FirstPageMetadataAsync(ct);
        var id = await PersistEnrichmentAsync(metadata, ct);

        // act
        await probe.ResetAsync(ct);
        probe.StubFoundLookup();
        probe.StubPath(RecommendationsPath, 400);
        var logStart = probe.Logs.Entries.Count;
        var reEnrichment = await EnrichAgainAsync(id, ct);
        var failedMetadata = Metadata(await TmdbFindAsync(reEnrichment.Services, "The Matrix", ct));
        failedMetadata.TmdbId.ShouldBe(603);
        failedMetadata.RecommendedTmdbIds.ShouldBeNull();
        reEnrichment.Movie.MarkEnriched(failedMetadata, Now.AddMinutes(1));
        await reEnrichment.Store.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadRecordAsync(verify, id, ct);
        record.Recommendations.Select(recommendation => recommendation.RecommendedTmdbId).Order().ToArray()
            .ShouldBe([604, 605, 606]);
        var corpus = LogCorpus(logStart);
        corpus.ShouldContain(RecommendationLogReason);
        corpus.ShouldNotContain(SecondaryMetadataProviderProbe.SentinelApiKey);
        corpus.ShouldNotContain(Uri.EscapeDataString(SecondaryMetadataProviderProbe.SentinelApiKey));
        corpus.ShouldNotContain("api_key=");
    }

    [Fact]
    public async Task SaveChanges_ReloadedEmptyPageAgainstEmptyRows_KeepsTheIdentityAndLeavesNoRows()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var emptyMetadata = await FindWithRecommendationsAsync(EmptyPageBody, ct);
        emptyMetadata.RecommendedTmdbIds.ShouldBeEmpty();
        var id = await PersistEnrichmentAsync(emptyMetadata, ct);

        // act
        await probe.ResetAsync(ct);
        var reloaded = await EnrichAgainAsync(id, ct);
        reloaded.Movie.RecommendedTmdbIds.ShouldBeNull();
        var repeatedMetadata = await FindWithRecommendationsAsync(EmptyPageBody, ct);
        repeatedMetadata.RecommendedTmdbIds.ShouldBeEmpty();
        reloaded.Movie.MarkEnriched(repeatedMetadata, Now.AddMinutes(1));
        await reloaded.Store.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadRecordAsync(verify, id, ct);
        record.TmdbId.ShouldBe(603);
        record.Recommendations.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveChanges_ChangedTmdbIdentityWithFailedFetch_ClearsTheStaleRows()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var metadata = await FirstPageMetadataAsync(ct);
        var id = await PersistEnrichmentAsync(metadata, ct);

        // act
        await probe.ResetAsync(ct);
        probe.StubSearch(Search604Body);
        probe.StubDetails(604, Details604Body);
        probe.StubPath("/3/movie/604/recommendations", 400);
        var reEnrichment = await EnrichAgainAsync(id, ct);
        var changedMetadata = Metadata(await TmdbFindAsync(reEnrichment.Services, "The Matrix Reloaded", ct));
        changedMetadata.TmdbId.ShouldBe(604);
        changedMetadata.RecommendedTmdbIds.ShouldBeNull();
        reEnrichment.Movie.MarkEnriched(changedMetadata, Now.AddMinutes(1));
        await reEnrichment.Store.SaveChangesAsync(ct);

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadRecordAsync(verify, id, ct);
        record.TmdbId.ShouldBe(604);
        record.Recommendations.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveChanges_ReplacementPausedBeforeCommit_SeparateReaderSeesTheOldSetThenTheNewSet()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var original = await FirstPageMetadataAsync(ct);
        var id = await PersistEnrichmentAsync(original, ct);
        await probe.ResetAsync(ct);
        var replacement = await FindWithRecommendationsAsync(ReplacementPageBody, ct);
        var gate = new RecommendationWriteGateInterceptor();

        // act
        await using var db = OpenInstrumentedContext(gate);
        var store = Repository(db);
        var movie = await store.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.RequestEnrichment();
        movie.MarkEnriched(replacement, Now.AddMinutes(1));
        var save = store.SaveChangesAsync(ct);
        await gate.Arrived.WaitAsync(TimeSpan.FromSeconds(30), ct);

        // assert
        await using (var reader = fixture.CreateMigratedContext())
        {
            var during = await LoadRecordAsync(reader, id, ct);
            during.Recommendations.Select(row => row.RecommendedTmdbId).Order().ToArray().ShouldBe([604, 605, 606]);
            during.Recommendations.ShouldAllBe(row => row.Title != null);
        }

        gate.Release();
        await save.WaitAsync(TimeSpan.FromSeconds(30), ct);
        await using (var after = fixture.CreateMigratedContext())
        {
            var committed = await LoadRecordAsync(after, id, ct);
            committed.Recommendations.Select(row => row.RecommendedTmdbId).Order().ToArray().ShouldBe([701, 702]);
            committed.Recommendations.ShouldAllBe(row => row.Title != null);
        }
    }

    [Fact]
    public async Task SaveChanges_ReplacementWriteFails_PriorLinksRemain()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var id = await PersistEnrichmentAsync(await FirstPageMetadataAsync(ct), ct);
        await probe.ResetAsync(ct);
        var replacement = await FindWithRecommendationsAsync(ReplacementPageBody, ct);

        // act
        await using var db = OpenInstrumentedContext(new FailingRecommendationWriteInterceptor());
        var store = Repository(db);
        var movie = await store.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.RequestEnrichment();
        movie.MarkEnriched(replacement, Now.AddMinutes(1));
        await Should.ThrowAsync<DbUpdateException>(() => store.SaveChangesAsync(ct));

        // assert
        await using var verify = fixture.CreateMigratedContext();
        var record = await LoadRecordAsync(verify, id, ct);
        record.Recommendations.Select(row => row.RecommendedTmdbId).Order().ToArray().ShouldBe([604, 605, 606]);
        record.Recommendations.ShouldAllBe(row => row.Title != null);
    }

    private async Task<MovieMetadata> FirstPageMetadataAsync(CancellationToken ct)
    {
        probe.StubFoundLookup();
        probe.StubPath(RecommendationsPath, 200, FirstPageBody);
        var services = probe.BuildServices();
        return Metadata(await TmdbFindAsync(services, "The Matrix", ct));
    }

    private async Task<MovieMetadata> FindWithRecommendationsAsync(string recommendationsBody, CancellationToken ct)
    {
        probe.StubFoundLookup();
        probe.StubPath(RecommendationsPath, 200, recommendationsBody);
        var services = probe.BuildServices();
        return Metadata(await TmdbFindAsync(services, "The Matrix", ct));
    }

    private async Task<MovieId> PersistEnrichmentAsync(MovieMetadata metadata, CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var id = await AddMovieAsync(repository, metadata.Title, $"recommendations-{Guid.NewGuid():N}", ct);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.MarkEnriched(metadata, Now);
        await repository.SaveChangesAsync(ct);
        return id;
    }

    private async Task<ReEnrichment> EnrichAgainAsync(MovieId id, CancellationToken ct)
    {
        var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var movie = await repository.GetAsync(id, ct);
        movie.ShouldNotBeNull();
        movie.RequestEnrichment();
        return new ReEnrichment(repository, movie, probe.BuildServices());
    }

    private static async Task<MetadataLookupResult> TmdbFindAsync(
        IServiceProvider services,
        string title,
        CancellationToken ct) =>
        await services
            .GetRequiredService<TmdbMetadataProvider>()
            .FindAsync(new MetadataLookup(title, null), ct);

    private static MovieMetadata Metadata(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;

    private static Task<MovieRecord> LoadRecordAsync(LamuFlixDbContext context, MovieId id, CancellationToken ct) =>
        context.Movies
            .AsNoTracking()
            .Include(movie => movie.Recommendations)
            .SingleAsync(movie => movie.Id == id, ct);

    private static async Task<MovieId> AddMovieAsync(
        EfMovieRepository repository,
        string title,
        string suffix,
        CancellationToken ct)
    {
        var id = await repository.NextIdentityAsync(ct);
        await repository.AddAsync(
            Movie.Create(id, title, new LibraryPath($"C:/library/{suffix}.mkv"), new MediaFormat("mkv")), ct);
        await repository.SaveChangesAsync(ct);
        return id;
    }

    private LamuFlixDbContext OpenInstrumentedContext(DbCommandInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(fixture.ConnectionString, npgsql => npgsql.UseVector())
            .AddInterceptors(interceptor)
            .Options;
        return new LamuFlixDbContext(options);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(
            db,
            new FixedTimeProvider(Now),
            Microsoft.Extensions.Options.Options.Create(
                new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);

    private sealed class RecommendationWriteGateInterceptor : DbCommandInterceptor
    {
        private readonly TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource released = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Arrived => arrived.Task;

        public void Release() => released.TrySetResult();

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await GateAsync(command, cancellationToken);
            return result;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            await GateAsync(command, cancellationToken);
            return result;
        }

        private async Task GateAsync(DbCommand command, CancellationToken cancellationToken)
        {
            if (command.CommandText.Contains("movie_recommendations", StringComparison.Ordinal)
                && command.CommandText.Contains("INSERT INTO", StringComparison.Ordinal))
            {
                arrived.TrySetResult();
                await released.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }

    private sealed class FailingRecommendationWriteInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            IsRecommendationInsert(command)
                ? ValueTask.FromException<InterceptionResult<int>>(new InvalidOperationException("write failed"))
                : ValueTask.FromResult(result);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default) =>
            IsRecommendationInsert(command)
                ? ValueTask.FromException<InterceptionResult<DbDataReader>>(new InvalidOperationException("write failed"))
                : ValueTask.FromResult(result);

        private static bool IsRecommendationInsert(DbCommand command) =>
            command.CommandText.Contains("movie_recommendations", StringComparison.Ordinal)
            && command.CommandText.Contains("INSERT INTO", StringComparison.Ordinal);
    }

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

    private sealed record ReEnrichment(EfMovieRepository Store, Movie Movie, IServiceProvider Services);
}
