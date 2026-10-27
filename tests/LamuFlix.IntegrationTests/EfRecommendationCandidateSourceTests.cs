using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Pgvector;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class EfRecommendationCandidateSourceTests(PostgresFixture fixture) : IAsyncLifetime
{
    private const double SimilarityTolerance = 1e-5;

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
        await SeedVectorsAsync(
            ct,
            (1, CosineVector(0.9)),
            (2, UnitVector()),
            (3, CosineVector(0.8)),
            (4, CosineVector(0.7)),
            (5, CosineVector(0.6)));
        var selected = new MovieId(2);
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(selected, ct);

        // assert
        candidates.Select(candidate => candidate.Movie.Title).ShouldBe(["Enriched"]);
        candidates.ShouldAllBe(candidate => candidate.Movie.Status == EnrichmentStatus.Enriched);
        candidates.ShouldNotContain(candidate => candidate.Movie.Id == selected);
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
        var selectedRecord = Record(4, "Selected", EnrichmentStatus.Enriched);
        await SeedRecordsAsync(ct, fullRecord, bareRecord, noneRecord, selectedRecord);
        await SeedVectorsAsync(
            ct,
            (1, CosineVector(0.9)),
            (2, CosineVector(0.7)),
            (3, CosineVector(0.5)),
            (4, UnitVector()));
        var fullId = fullRecord.Id.ShouldNotBeNull();
        var bareId = bareRecord.Id.ShouldNotBeNull();
        var noneId = noneRecord.Id.ShouldNotBeNull();
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(4), ct);

        // assert
        candidates.Count.ShouldBe(3);

        var full = candidates.Single(candidate => candidate.Movie.Id == fullId).Movie;
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

        var bare = candidates.Single(candidate => candidate.Movie.Id == bareId).Movie;
        bare.Metadata.ShouldNotBeNull();
        bare.Metadata.Title.ShouldBe("Bare metadata");
        bare.Metadata.Directors.ShouldBeEmpty();
        bare.Metadata.Actors.ShouldBeEmpty();
        bare.Metadata.Genres.ShouldBeEmpty();
        bare.Metadata.Writers.ShouldBeEmpty();

        var none = candidates.Single(candidate => candidate.Movie.Id == noneId).Movie;
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
            status: EnrichmentStatus.Enriched);
        await SeedVectorsAsync(
            ct,
            (fullId.Value, CosineVector(0.9)),
            (bareId.Value, CosineVector(0.7)),
            (selectedId.Value, UnitVector()));
        await using var candidateDb = fixture.CreateMigratedContext();
        var source = Source(candidateDb);
        await using var repositoryDb = fixture.CreateMigratedContext();
        var repository = Repository(repositoryDb);

        // act
        var candidates = await source.GetCandidatesAsync(selectedId, ct);
        var expectedFull = await repository.GetAsync(fullId, ct);
        var expectedBare = await repository.GetAsync(bareId, ct);

        // assert
        candidates.Count.ShouldBe(2);
        AssertSameState(candidates.Single(candidate => candidate.Movie.Id == fullId).Movie, expectedFull.ShouldNotBeNull());
        AssertSameState(candidates.Single(candidate => candidate.Movie.Id == bareId).Movie, expectedBare.ShouldNotBeNull());
    }

    [Fact]
    public async Task GetCandidatesAsync_DefaultOptions_CapsPoolAtFiftyNearestFirst()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedDefaultCappedPortfolioAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), ct);

        // assert
        candidates.Count.ShouldBe(50);
        candidates.Select(candidate => candidate.Movie.Id.Value)
            .ShouldBe(Enumerable.Range(110, 50).Reverse());
    }

    [Fact]
    public async Task GetCandidatesAsync_CustomCandidatePoolSize_CapsOnTheServerBeforeHydration()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedDefaultCappedPortfolioAsync(ct);
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var source = Source(db, new RecommendationOptions { Take = 1, CandidatePoolSize = 7 });

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), ct);

        // assert
        candidates.Count.ShouldBe(7);
        candidates.Select(candidate => candidate.Movie.Id.Value)
            .ShouldBe(Enumerable.Range(153, 7).Reverse());
        interceptor.ReaderCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetCandidatesAsync_EqualDistances_OrdersByAscendingMovieId()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Selected", EnrichmentStatus.Enriched),
            Record(10, "Later", EnrichmentStatus.Enriched),
            Record(5, "Earlier", EnrichmentStatus.Enriched));
        await SeedVectorsAsync(
            ct,
            (1, UnitVector()),
            (10, CosineVector(0.25)),
            (5, CosineVector(0.25)));
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), ct);

        // assert
        candidates.Select(candidate => candidate.Movie.Id.Value).ShouldBe([5, 10]);
    }

    [Fact]
    public async Task GetCandidatesAsync_NamedSyntheticCosines_ConvertsDistanceToSimilarity()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        double[] cosines = [0.8, 0.6, 0.4, 0.0, -1.0];
        await SeedRecordsAsync(
            ct,
            Record(1, "Selected", EnrichmentStatus.Enriched),
            Record(10, "Similar 0.8", EnrichmentStatus.Enriched),
            Record(11, "Similar 0.6", EnrichmentStatus.Enriched),
            Record(12, "At threshold 0.4", EnrichmentStatus.Enriched),
            Record(13, "Orthogonal 0.0", EnrichmentStatus.Enriched),
            Record(14, "Opposite -1.0", EnrichmentStatus.Enriched));
        await SeedVectorsAsync(
            ct,
            (1, UnitVector()),
            (10, CosineVector(cosines[0])),
            (11, CosineVector(cosines[1])),
            (12, CosineVector(cosines[2])),
            (13, CosineVector(cosines[3])),
            (14, CosineVector(cosines[4])));
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), ct);

        // assert
        candidates.Select(candidate => candidate.Movie.Id.Value).ShouldBe([10, 11, 12, 13, 14]);
        for (var index = 0; index < cosines.Length; index++)
        {
            candidates[index].CosineSimilarity.ShouldBe(cosines[index], SimilarityTolerance);
            candidates[index].CosineSimilarity.ShouldBeInRange(-1.0, 1.0);
        }
    }

    [Fact]
    public async Task GetCandidatesAsync_UnusableCandidateVectors_ExcludedBeforeCap()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Selected", EnrichmentStatus.Enriched),
            Record(10, "No features", EnrichmentStatus.Enriched),
            Record(11, "Zero vector", EnrichmentStatus.Enriched),
            Record(12, "Usable nearest", EnrichmentStatus.Enriched),
            Record(13, "Usable next", EnrichmentStatus.Enriched),
            Record(14, "Usable third", EnrichmentStatus.Enriched));
        await SeedVectorsAsync(
            ct,
            (1, UnitVector()),
            (11, new float[EmbeddingDimensions.Value]),
            (12, CosineVector(0.8)),
            (13, CosineVector(0.7)),
            (14, CosineVector(0.6)));
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db, new RecommendationOptions { Take = 1, CandidatePoolSize = 2 });

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), ct);

        // assert
        candidates.Select(candidate => candidate.Movie.Id.Value).ShouldBe([12, 13]);
    }

    [Fact]
    public async Task GetCandidatesAsync_MissingSelectedFeatures_ReturnsEmptyAfterOneReader()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Alpha", EnrichmentStatus.Enriched),
            Record(2, "Bravo", EnrichmentStatus.Enriched));
        await SeedVectorsAsync(ct, (1, CosineVector(0.8)), (2, CosineVector(0.7)));
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(9999), ct);

        // assert
        candidates.ShouldBeEmpty();
        interceptor.ReaderCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetCandidatesAsync_ZeroSelectedVector_ReturnsEmptyAfterOneReader()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Selected", EnrichmentStatus.Enriched),
            Record(2, "Bravo", EnrichmentStatus.Enriched));
        await SeedVectorsAsync(
            ct,
            (1, new float[EmbeddingDimensions.Value]),
            (2, CosineVector(0.7)));
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), ct);

        // assert
        candidates.ShouldBeEmpty();
        interceptor.ReaderCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetCandidatesAsync_PopulatedPool_UsesAtMostTwoReadersWithNoPerCandidateReads()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            [Record(1, "Selected", EnrichmentStatus.Enriched), .. Enumerable.Range(2, 24)
                .Select(id => Record(id, $"Movie {id}", EnrichmentStatus.Enriched))]);
        await SeedVectorsAsync(
            ct,
            [(1, UnitVector()), .. Enumerable.Range(2, 24)
                .Select(id => (id, CosineVector(0.01 * id)))]);
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), ct);

        // assert
        candidates.Count.ShouldBe(24);
        interceptor.ReaderCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetCandidatesAsync_QueryShape_UsesCosineDistanceOrderAndLimit()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedDefaultCappedPortfolioAsync(ct);
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var source = Source(db);

        // act
        await source.GetCandidatesAsync(new MovieId(1), ct);

        // assert
        var candidateCommand = interceptor.ReaderCommands
            .Single(command => command.Contains("<=>", StringComparison.Ordinal));
        candidateCommand.ShouldContain("movie_features");
        Regex.IsMatch(candidateCommand, @"ORDER\s+BY", RegexOptions.IgnoreCase).ShouldBeTrue();
        Regex.IsMatch(candidateCommand, @"LIMIT", RegexOptions.IgnoreCase).ShouldBeTrue();
    }

    [Fact]
    public async Task GetCandidatesAsync_AfterLoad_LeavesQueryContextUntracked()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Selected", EnrichmentStatus.Enriched),
            Record(2, "Alpha", EnrichmentStatus.Enriched),
            Record(3, "Bravo", EnrichmentStatus.Enriched));
        await SeedVectorsAsync(
            ct,
            (1, UnitVector()),
            (2, CosineVector(0.8)),
            (3, CosineVector(0.7)));
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);

        // act
        await source.GetCandidatesAsync(new MovieId(1), ct);

        // assert
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCandidatesAsync_CancelledToken_PropagatesCancellation()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(ct, Record(1, "Alpha", EnrichmentStatus.Enriched));
        await SeedVectorsAsync(ct, (1, CosineVector(0.8)));
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var token = cancelled.Token;

        // act
        var act = () => source.GetCandidatesAsync(new MovieId(1), token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task GetCandidatesAsync_CallerToken_ThreadsTokenIntoBothReaders()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Selected", EnrichmentStatus.Enriched),
            Record(2, "Alpha", EnrichmentStatus.Enriched));
        await SeedVectorsAsync(ct, (1, UnitVector()), (2, CosineVector(0.8)));
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var source = Source(db);
        using var caller = new CancellationTokenSource();

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), caller.Token);

        // assert
        candidates.Count.ShouldBe(1);
        interceptor.ReaderCount.ShouldBe(2);
        interceptor.ReaderTokens.ShouldAllBe(token => token == caller.Token);
    }

    [Fact]
    public async Task GetCandidatesAsync_CandidateReaderFails_PropagatesError()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Selected", EnrichmentStatus.Enriched),
            Record(2, "Alpha", EnrichmentStatus.Enriched));
        await SeedVectorsAsync(ct, (1, UnitVector()), (2, CosineVector(0.8)));
        var interceptor = new RecordingCommandInterceptor(failOnReader: 2);
        await using var db = OpenInstrumentedContext(interceptor);
        var source = Source(db);

        // act
        var act = () => source.GetCandidatesAsync(new MovieId(1), ct);

        // assert
        var failure = await Should.ThrowAsync<InvalidOperationException>(act);
        failure.Message.ShouldBe("reader failed");
    }

    private async Task SeedDefaultCappedPortfolioAsync(CancellationToken ct)
    {
        await SeedRecordsAsync(
            ct,
            [Record(1, "Selected", EnrichmentStatus.Enriched), .. Enumerable.Range(0, 60)
                .Select(index => Record(100 + index, $"Movie {100 + index}", EnrichmentStatus.Enriched))]);
        await SeedVectorsAsync(
            ct,
            [(1, UnitVector()), .. Enumerable.Range(0, 60)
                .Select(index => (100 + index, CosineVector(-0.9 + (index * 0.03))))]);
    }

    private static EfRecommendationCandidateSource Source(
        LamuFlixDbContext db,
        RecommendationOptions? options = null) =>
        new(db, Microsoft.Extensions.Options.Options.Create(options ?? new RecommendationOptions()));

    private static float[] UnitVector()
    {
        var vector = new float[EmbeddingDimensions.Value];
        vector[0] = 1f;
        return vector;
    }

    private static float[] CosineVector(double cosine)
    {
        var vector = new float[EmbeddingDimensions.Value];
        vector[0] = (float)cosine;
        vector[1] = (float)Math.Sqrt(1.0 - (cosine * cosine));
        return vector;
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

    private async Task SeedVectorsAsync(
        CancellationToken ct,
        params (int MovieId, float[] Vector)[] vectors)
    {
        await using var db = fixture.CreateMigratedContext();
        foreach (var (movieId, vector) in vectors)
        {
            db.MovieFeatures.Add(new MovieFeaturesRecord
            {
                MovieId = new MovieId(movieId),
                Vector = new Vector(vector),
                ComputedAt = Now
            });
        }

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
        new(
            db,
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions
            {
                ClaimLease = TimeSpan.FromMinutes(5)
            }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);

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

    private sealed class RecordingCommandInterceptor(int failOnReader = 0) : DbCommandInterceptor
    {
        private readonly List<CancellationToken> readerTokens = [];
        private readonly List<string> readerCommands = [];

        public IReadOnlyList<CancellationToken> ReaderTokens => readerTokens;

        public IReadOnlyList<string> ReaderCommands => readerCommands;

        public int ReaderCount => readerTokens.Count;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            readerTokens.Add(cancellationToken);
            readerCommands.Add(command.CommandText);
            if (readerTokens.Count == failOnReader)
            {
                throw new InvalidOperationException("reader failed");
            }

            return ValueTask.FromResult(result);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            readerTokens.Add(CancellationToken.None);
            readerCommands.Add(command.CommandText);
            return result;
        }
    }
}
