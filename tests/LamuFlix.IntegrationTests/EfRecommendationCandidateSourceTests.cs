using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfRecommendationCandidateSourceTests(PostgresFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetCandidatesAsync_MixedEnrichmentStates_ReturnsOnlyOtherEnrichedMovies()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Enriched", EnrichmentStatus.Enriched),
            Record(2, "Selected", EnrichmentStatus.Enriched),
            Record(3, "Pending", EnrichmentStatus.Pending),
            Record(4, "NotFound", EnrichmentStatus.NotFound),
            Record(5, "Failed", EnrichmentStatus.Failed));
        var selected = new MovieId(2);
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationCandidateSource(db);

        // act
        var candidates = await source.GetCandidatesAsync(selected, ct);

        // assert
        candidates.Select(movie => movie.Title).ShouldBe(["Enriched"]);
        candidates.ShouldAllBe(movie => movie.Status == EnrichmentStatus.Enriched);
        candidates.ShouldNotContain(movie => movie.Id == selected);
    }

    [Fact]
    public async Task GetCandidatesAsync_FullAndEmptyMetadata_RehydratesEveryState()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var fullRecord = new MovieRecord
        {
            Id = new MovieId(1),
            Title = "Full",
            LibraryPath = new LibraryPath("C:/library/candidate-full.mkv"),
            Format = new MediaFormat("mkv"),
            IsInWatchlist = true,
            IsWatched = true,
            IsNotInterested = true,
            Status = EnrichmentStatus.Enriched,
            EnrichedAt = Now.AddDays(-1),
            EnrichmentAttempts = 2,
            LastAttemptAt = Now.AddHours(-1),
            MetadataTitle = "Metadata title",
            Plot = "Synopsis",
            ReleaseYear = new ReleaseYear(2024, Now),
            RuntimeMinutes = new Runtime(121),
            ImdbRating = new ImdbRating(8.1m),
            ImdbId = new ImdbId("tt1234567"),
            ManualImdbId = new ImdbId("tt7654321"),
            PosterUrl = "https://example.test/poster.jpg",
            RottenTomatoesRating = 91,
            MetaScore = 82,
            ParsedReleaseYear = new ReleaseYear(2023, Now)
        };
        fullRecord.Actors.Add(new ActorRecord { Name = "Harrison Ford" });
        fullRecord.Directors.Add(new DirectorRecord { Name = "Ridley Scott" });
        fullRecord.Genres.Add(new GenreRecord { Name = "Drama" });
        fullRecord.Genres.Add(new GenreRecord { Name = "Sci-Fi" });
        fullRecord.Writers.Add(new WriterRecord { Name = "Wanda Writer" });
        var bareRecord = new MovieRecord
        {
            Id = new MovieId(2),
            Title = "Bare",
            LibraryPath = new LibraryPath("C:/library/candidate-bare.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Enriched,
            MetadataTitle = "Bare metadata"
        };
        var noneRecord = new MovieRecord
        {
            Id = new MovieId(3),
            Title = "None",
            LibraryPath = new LibraryPath("C:/library/candidate-none.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Enriched
        };
        await SeedRecordsAsync(ct, fullRecord, bareRecord, noneRecord);
        var fullId = fullRecord.Id.ShouldNotBeNull();
        var bareId = bareRecord.Id.ShouldNotBeNull();
        var noneId = noneRecord.Id.ShouldNotBeNull();
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationCandidateSource(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(9999), ct);

        // assert
        candidates.Count.ShouldBe(3);

        var full = candidates.Single(movie => movie.Id == fullId);
        full.Title.ShouldBe("Full");
        full.Path.ShouldBe(new LibraryPath("C:/library/candidate-full.mkv"));
        full.Format.ShouldBe(new MediaFormat("mkv"));
        full.IsInWatchlist.ShouldBeTrue();
        full.IsWatched.ShouldBeTrue();
        full.IsNotInterested.ShouldBeTrue();
        full.Status.ShouldBe(EnrichmentStatus.Enriched);
        full.EnrichedAt.ShouldBe(Now.AddDays(-1));
        full.EnrichmentAttempts.ShouldBe(2);
        full.LastAttemptAt.ShouldBe(Now.AddHours(-1));
        full.ParsedReleaseYear.ShouldBe(new ReleaseYear(2023, Now));
        full.ManualImdbId.ShouldBe(new ImdbId("tt7654321"));
        AssertMetadataContents(full.Metadata.ShouldNotBeNull(), FullMetadata());

        var bare = candidates.Single(movie => movie.Id == bareId);
        bare.Metadata.ShouldNotBeNull();
        bare.Metadata.Title.ShouldBe("Bare metadata");
        bare.Metadata.Directors.ShouldBeEmpty();
        bare.Metadata.Actors.ShouldBeEmpty();
        bare.Metadata.Genres.ShouldBeEmpty();
        bare.Metadata.Writers.ShouldBeEmpty();

        var none = candidates.Single(movie => movie.Id == noneId);
        none.Metadata.ShouldBeNull();
    }

    [Fact]
    public async Task GetCandidatesAsync_SeededMovies_MatchesRepositoryReadState()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var fullId = await AddMovieAsync(
            ct,
            "Metadata title",
            "candidate-parity-full",
            FullMetadata(),
            isInWatchlist: true,
            isWatched: true,
            isNotInterested: true,
            enrichedAt: Now.AddDays(-1),
            enrichmentAttempts: 2,
            lastAttemptAt: Now.AddHours(-1),
            parsedReleaseYear: new ReleaseYear(2023, Now),
            manualImdbId: new ImdbId("tt7654321"));
        var bareId = await AddMovieAsync(ct, "Bare", "candidate-parity-bare", metadata: null);
        var selectedId = await AddMovieAsync(
            ct,
            "Selected",
            "candidate-parity-selected",
            metadata: null,
            status: EnrichmentStatus.Pending);
        await using var candidateDb = fixture.CreateMigratedContext();
        var source = new EfRecommendationCandidateSource(candidateDb);
        await using var repositoryDb = fixture.CreateMigratedContext();
        var repository = Repository(repositoryDb);

        // act
        var candidates = await source.GetCandidatesAsync(selectedId, ct);
        var expectedFull = await repository.GetAsync(fullId, ct);
        var expectedBare = await repository.GetAsync(bareId, ct);

        // assert
        candidates.Count.ShouldBe(2);
        AssertSameState(candidates.Single(movie => movie.Id == fullId), expectedFull.ShouldNotBeNull());
        AssertSameState(candidates.Single(movie => movie.Id == bareId), expectedBare.ShouldNotBeNull());
    }

    [Fact]
    public async Task GetCandidatesAsync_EnrichedPool_ExecutesExactlyOneReaderCommand()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Alpha", EnrichmentStatus.Enriched),
            Record(2, "Bravo", EnrichmentStatus.Enriched),
            Record(3, "Charlie", EnrichmentStatus.Enriched),
            Record(4, "Pending", EnrichmentStatus.Pending));
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var source = new EfRecommendationCandidateSource(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(9999), ct);

        // assert
        candidates.Count.ShouldBe(3);
        interceptor.ReaderTokens.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetCandidatesAsync_AfterLoad_LeavesQueryContextUntracked()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Alpha", EnrichmentStatus.Enriched),
            Record(2, "Bravo", EnrichmentStatus.Enriched));
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationCandidateSource(db);

        // act
        await source.GetCandidatesAsync(new MovieId(9999), ct);

        // assert
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCandidatesAsync_TwentyFiveEnrichedMovies_ReturnsUncappedPool()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var records = Enumerable.Range(1, 25)
            .Select(id => Record(id, $"Movie {id}", EnrichmentStatus.Enriched))
            .ToArray();
        await SeedRecordsAsync(ct, records);
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationCandidateSource(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(9999), ct);

        // assert
        candidates.Count.ShouldBe(25);
        candidates.Select(movie => movie.Id.Value).OrderBy(value => value).ShouldBe(Enumerable.Range(1, 25));
    }

    [Fact]
    public async Task GetCandidatesAsync_CancelledToken_PropagatesCancellation()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(ct, Record(1, "Alpha", EnrichmentStatus.Enriched));
        await using var db = fixture.CreateMigratedContext();
        var source = new EfRecommendationCandidateSource(db);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // act
        var token = cancelled.Token;
        var act = () => source.GetCandidatesAsync(new MovieId(9999), token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task GetCandidatesAsync_CallerToken_ThreadsTokenIntoReaderCommand()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(ct, Record(1, "Alpha", EnrichmentStatus.Enriched));
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var source = new EfRecommendationCandidateSource(db);
        using var caller = new CancellationTokenSource();

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(9999), caller.Token);

        // assert
        candidates.Count.ShouldBe(1);
        interceptor.ReaderTokens.Count.ShouldBe(1);
        interceptor.ReaderTokens[0].ShouldBe(caller.Token);
    }

    private static void AssertSameState(Movie actual, Movie expected)
    {
        actual.Id.ShouldBe(expected.Id);
        actual.Title.ShouldBe(expected.Title);
        actual.Path.ShouldBe(expected.Path);
        actual.Format.ShouldBe(expected.Format);
        actual.ParsedReleaseYear.ShouldBe(expected.ParsedReleaseYear);
        actual.IsInWatchlist.ShouldBe(expected.IsInWatchlist);
        actual.IsWatched.ShouldBe(expected.IsWatched);
        actual.IsNotInterested.ShouldBe(expected.IsNotInterested);
        actual.Status.ShouldBe(expected.Status);
        actual.EnrichedAt.ShouldBe(expected.EnrichedAt);
        actual.EnrichmentAttempts.ShouldBe(expected.EnrichmentAttempts);
        actual.LastFailureCategory.ShouldBe(expected.LastFailureCategory);
        actual.LastAttemptAt.ShouldBe(expected.LastAttemptAt);
        actual.ManualImdbId.ShouldBe(expected.ManualImdbId);

        var expectedMetadata = expected.Metadata;
        if (expectedMetadata is null)
        {
            actual.Metadata.ShouldBeNull();
            return;
        }

        AssertMetadataContents(actual.Metadata.ShouldNotBeNull(), expectedMetadata);
    }

    private static void AssertMetadataContents(MovieMetadata actual, MovieMetadata expected)
    {
        actual.Title.ShouldBe(expected.Title);
        actual.Synopsis.ShouldBe(expected.Synopsis);
        actual.ReleaseYear.ShouldBe(expected.ReleaseYear);
        actual.Runtime.ShouldBe(expected.Runtime);
        actual.ImdbRating.ShouldBe(expected.ImdbRating);
        actual.ImdbId.ShouldBe(expected.ImdbId);
        actual.PosterUrl.ShouldBe(expected.PosterUrl);
        actual.RottenTomatoesRating.ShouldBe(expected.RottenTomatoesRating);
        actual.MetaScore.ShouldBe(expected.MetaScore);
        actual.Directors.OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(expected.Directors.OrderBy(name => name, StringComparer.Ordinal));
        actual.Actors.OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(expected.Actors.OrderBy(name => name, StringComparer.Ordinal));
        actual.Genres.OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(expected.Genres.OrderBy(name => name, StringComparer.Ordinal));
        actual.Writers.OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(expected.Writers.OrderBy(name => name, StringComparer.Ordinal));
    }

    private async Task SeedRecordsAsync(CancellationToken ct, params MovieRecord[] records)
    {
        await using var db = fixture.CreateMigratedContext();
        db.Movies.AddRange(records);
        await db.SaveChangesAsync(ct);
    }

    private async Task<MovieId> AddMovieAsync(
        CancellationToken ct,
        string title,
        string suffix,
        MovieMetadata? metadata,
        bool isInWatchlist = false,
        bool isWatched = false,
        bool isNotInterested = false,
        EnrichmentStatus? status = null,
        DateTimeOffset? enrichedAt = null,
        int enrichmentAttempts = 0,
        DateTimeOffset? lastAttemptAt = null,
        ReleaseYear? parsedReleaseYear = null,
        ImdbId? manualImdbId = null)
    {
        await using var db = fixture.CreateMigratedContext();
        var repository = Repository(db);
        var movie = Movie.Rehydrate(
            await repository.NextIdentityAsync(ct),
            title,
            new LibraryPath($"C:/library/{suffix}.mkv"),
            new MediaFormat("mkv"),
            isInWatchlist,
            metadata,
            status ?? EnrichmentStatus.Enriched,
            enrichedAt,
            enrichmentAttempts,
            lastFailureCategory: null,
            lastAttemptAt: lastAttemptAt,
            parsedReleaseYear: parsedReleaseYear,
            manualImdbId: manualImdbId,
            isWatched: isWatched,
            isNotInterested: isNotInterested);
        await repository.AddAsync(movie, ct);
        await repository.SaveChangesAsync(ct);
        return movie.Id;
    }

    private LamuFlixDbContext OpenInstrumentedContext(DbCommandInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<LamuFlixDbContext>()
            .UseNpgsql(fixture.ConnectionString, o => o.UseVector())
            .AddInterceptors(interceptor)
            .Options;
        return new LamuFlixDbContext(options);
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(db, TimeProvider.System, Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions
        {
            ClaimLease = TimeSpan.FromMinutes(5)
        }));

    private static MovieRecord Record(int id, string title, EnrichmentStatus status) =>
        new()
        {
            Id = new MovieId(id),
            Title = title,
            LibraryPath = new LibraryPath($"C:/library/candidate-{id}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = status
        };

    private static MovieMetadata FullMetadata() =>
        new(
            "Metadata title",
            "Synopsis",
            new ReleaseYear(2024, Now),
            new Runtime(121),
            new ImdbRating(8.1m),
            new ImdbId("tt1234567"),
            "https://example.test/poster.jpg",
            91,
            82,
            ["Ridley Scott"],
            ["Harrison Ford"],
            ["Drama", "Sci-Fi"],
            ["Wanda Writer"]);

    private sealed class RecordingCommandInterceptor : DbCommandInterceptor
    {
        private readonly List<CancellationToken> readerTokens = [];

        public IReadOnlyList<CancellationToken> ReaderTokens => readerTokens;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            readerTokens.Add(cancellationToken);
            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            readerTokens.Add(CancellationToken.None);
            return result;
        }
    }
}

