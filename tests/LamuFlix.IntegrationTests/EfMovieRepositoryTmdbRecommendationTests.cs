using System;
using System.Collections.Generic;
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
            { "id": 604, "title": "The Matrix Reloaded" },
            { "id": 605, "title": "The Matrix Revolutions" },
            { "id": 606, "title": "The Animatrix" }
          ]
        }
        """;

    private const string EmptyPageBody = """
        { "page": 1, "total_pages": 1, "total_results": 0, "results": [] }
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

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(
            db,
            new FixedTimeProvider(Now),
            Microsoft.Extensions.Options.Options.Create(
                new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);

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
