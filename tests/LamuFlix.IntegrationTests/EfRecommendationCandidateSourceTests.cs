using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
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

public sealed class EfRecommendationCandidateSourceTests(PostgresFixture fixture) : IAsyncLifetime, IClassFixture<PostgresFixture>
{
    private const double SimilarityTolerance = 1e-5;

    private const int SplitQueryReaderCount = 9;

    private static readonly string[] AssociationTables =
    [
        "movie_actors",
        "movie_directors",
        "movie_genres",
        "movie_writers",
        "movie_keywords",
        "movie_tmdb_cast",
    ];

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
        var candidates = await source.GetCandidatesAsync(selected, null, ct);

        // assert
        candidates.Select(candidate => candidate.Library.Summary.Title).ShouldBe(["Enriched"]);
        candidates.ShouldAllBe(candidate => candidate.Library.Summary.EnrichmentStatus == EnrichmentStatus.Enriched);
        candidates.ShouldNotContain(candidate => candidate.Library.Id == selected);
    }
    [Fact]
    public async Task GetCandidatesAsync_FullAndEmptyMetadata_CarriesSummaryAndFeatureState()
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
            Review = Review.Disliked,
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
        var library = await source.GetLibraryFeaturesAsync(ct);

        // assert
        var full = library.Single(entry => entry.Id == fullId);
        full.Summary.Title.ShouldBe("Full");
        full.Summary.PosterUrl.ShouldBe("https://example.test/poster.jpg");
        full.Summary.ReleaseYear.ShouldBe(2024);
        full.Summary.Runtime.ShouldBe(121);
        full.Summary.ImdbRating.ShouldBe(8.1m);
        full.Summary.IsInWatchlist.ShouldBeTrue();
        full.Summary.IsWatched.ShouldBeTrue();
        full.Summary.Review.ShouldBe(Review.Disliked);
        full.Summary.EnrichmentStatus.ShouldBe(EnrichmentStatus.Enriched);
        full.Features.Genres.Order(StringComparer.Ordinal).ShouldBe(["Drama", "Sci-Fi"]);
        full.Features.Directors.ShouldBe(["Ridley Scott"]);
        full.Features.Writers.ShouldBe(["Wanda Writer"]);
        full.Features.Cast.ShouldBe(["Harrison Ford"]);
        full.Features.PlotEmbedding.Length.ShouldBe(EmbeddingDimensions.Value);

        var bare = library.Single(entry => entry.Id == bareId);
        bare.Summary.Title.ShouldBe("Bare");
        bare.Features.Directors.ShouldBeEmpty();
        bare.Features.Cast.ShouldBeEmpty();
        bare.Features.Genres.ShouldBeEmpty();
        bare.Features.Writers.ShouldBeEmpty();

        var none = library.Single(entry => entry.Id == noneId);
        none.Summary.Title.ShouldBe("None");
        none.Features.Genres.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetLibraryFeaturesAsync_UncappedEnrichedLoad_KeepsOptionalEmbeddings()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedDefaultCappedPortfolioAsync(ct);
        await SeedRecordsAsync(
            ct,
            Record(200, "No feature row", EnrichmentStatus.Enriched),
            Record(201, "Zero vector", EnrichmentStatus.Enriched),
            Record(202, "Pending", EnrichmentStatus.Pending));
        await SeedVectorsAsync(ct, (201, new float[EmbeddingDimensions.Value]));
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);
        int[] expectedIds = [1, .. Enumerable.Range(100, 60), 200, 201];

        // act
        var library = await source.GetLibraryFeaturesAsync(ct);

        // assert
        library.Count.ShouldBe(63);
        library.Select(entry => entry.Id.Value).Order().ShouldBe(expectedIds);
        library.ShouldNotContain(entry => entry.Id == new MovieId(202));

        library.Single(entry => entry.Id == new MovieId(1))
            .Features.PlotEmbedding.Length.ShouldBe(EmbeddingDimensions.Value);
        library.Single(entry => entry.Id == new MovieId(100))
            .Features.PlotEmbedding.Length.ShouldBe(EmbeddingDimensions.Value);
        library.Where(entry => entry.Id.Value is 200 or 201)
            .ShouldAllBe(entry => entry.Features.PlotEmbedding.IsDefaultOrEmpty);
        library.Single(entry => entry.Id == new MovieId(200))
            .Summary.EnrichmentStatus.ShouldBe(EnrichmentStatus.Enriched);
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
        var candidates = await source.GetCandidatesAsync(selectedId, null, ct);
        var expectedFull = await repository.GetAsync(fullId, ct);
        var expectedBare = await repository.GetAsync(bareId, ct);

        // assert
        candidates.Count.ShouldBe(2);
        AssertSameState(
            candidates.Single(candidate => candidate.Library.Id == fullId).Library,
            expectedFull.ShouldNotBeNull());
        AssertSameState(
            candidates.Single(candidate => candidate.Library.Id == bareId).Library,
            expectedBare.ShouldNotBeNull());
    }
    [Fact]
    public async Task GetCandidatesAsync_DefaultOptions_ReturnsEveryOtherEnrichedMovie()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedDefaultCappedPortfolioAsync(ct);
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), null, ct);

        // assert
        candidates.Count.ShouldBe(60);
        candidates.Select(candidate => candidate.Library.Id.Value).Order()
            .ShouldBe(Enumerable.Range(100, 60));
    }
    [Fact]
    public async Task GetCandidatesAsync_CustomCandidatePoolSize_DoesNotCapTheLoad()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedDefaultCappedPortfolioAsync(ct);
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), null, ct);

        // assert
        candidates.Count.ShouldBe(60);
        candidates.Select(candidate => candidate.Library.Id.Value).Order()
            .ShouldBe(Enumerable.Range(100, 60));
        interceptor.ReaderCount.ShouldBeGreaterThan(0);
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
        var candidates = await source.GetCandidatesAsync(new MovieId(1), null, ct);

        // assert
        candidates.Select(candidate => candidate.Library.Id.Value).ShouldBe([5, 10]);
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
        var candidates = await source.GetCandidatesAsync(new MovieId(1), null, ct);

        // assert
        candidates.Select(candidate => candidate.Library.Id.Value).ShouldBe([10, 11, 12, 13, 14]);
        for (var index = 0; index < cosines.Length; index++)
        {
            candidates[index].CosineSimilarity.ShouldBe(cosines[index], SimilarityTolerance);
            candidates[index].CosineSimilarity.ShouldBeInRange(-1.0, 1.0);
        }
    }

    [Fact]
    public async Task GetCandidatesAsync_UnusableCandidateVectors_AreKeptWithZeroCosine()
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
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), null, ct);

        // assert
        candidates.Select(candidate => candidate.Library.Id.Value).ShouldBe([12, 13, 14, 10, 11]);
        candidates.Where(candidate => candidate.Library.Id.Value is 10 or 11)
            .ShouldAllBe(candidate => candidate.CosineSimilarity == 0.0);
        candidates.Where(candidate => candidate.Library.Id.Value is 12 or 13 or 14)
            .ShouldAllBe(candidate => candidate.CosineSimilarity > 0.0);
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
        var candidates = await source.GetCandidatesAsync(new MovieId(9999), null, ct);

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
        var candidates = await source.GetCandidatesAsync(new MovieId(1), null, ct);

        // assert
        candidates.ShouldBeEmpty();
        interceptor.ReaderCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetCandidatesAsync_PopulatedPool_UsesBoundedReadersWithNoPerCandidateReads()
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
        var candidates = await source.GetCandidatesAsync(new MovieId(1), null, ct);

        // assert
        candidates.Count.ShouldBe(24);
        interceptor.ReaderCount.ShouldBe(SplitQueryReaderCount);
    }

    [Fact]
    public async Task GetCandidatesAsync_QueryShape_HasNoPlotCutoffOrLimit()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedDefaultCappedPortfolioAsync(ct);
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var source = Source(db);

        // act
        await source.GetCandidatesAsync(new MovieId(1), null, ct);

        // assert
        var libraryCommands = interceptor.ReaderCommands
            .Where(command => command.Contains("FROM movies", StringComparison.Ordinal))
            .ToList();
        libraryCommands.ShouldNotBeEmpty();
        libraryCommands.ShouldAllBe(command =>
            !Regex.IsMatch(command, @"LIMIT", RegexOptions.IgnoreCase));
        libraryCommands.ShouldAllBe(command =>
            !command.Contains("<=>", StringComparison.Ordinal));
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
        await source.GetCandidatesAsync(new MovieId(1), null, ct);

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
        var act = () => source.GetCandidatesAsync(new MovieId(1), null, token);

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
        var candidates = await source.GetCandidatesAsync(new MovieId(1), null, caller.Token);

        // assert
        candidates.Count.ShouldBe(1);
        interceptor.ReaderCount.ShouldBe(SplitQueryReaderCount);
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
        var act = () => source.GetCandidatesAsync(new MovieId(1), null, ct);

        // assert
        var failure = await Should.ThrowAsync<InvalidOperationException>(act);
        failure.Message.ShouldBe("reader failed");
    }

    [Fact]
    public async Task GetCandidatesAsync_DenseAssociationPool_LoadsEveryCollectionWithoutFanningThemIntoOneJoin()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var dense = SignalRecord(2);
        dense.Directors.Add(new DirectorRecord { Name = "Director One" });
        dense.Writers.Add(new WriterRecord { Name = "Writer One" });
        await SeedRecordsAsync(
            ct,
            Record(1, "Selected", EnrichmentStatus.Enriched),
            dense);
        await SeedVectorsAsync(ct, (1, UnitVector()), (2, CosineVector(0.8)));
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), 900, ct);

        // assert
        var features = candidates.Single(item => item.Library.Id == new MovieId(2)).Library.Features;
        features.Cast.Order(StringComparer.Ordinal).ShouldBe(["Base Actor", "Guest Actor"]);
        features.Genres.ShouldBe(["Drama"]);
        features.Directors.ShouldBe(["Director One"]);
        features.Writers.ShouldBe(["Writer One"]);
        features.Keywords.Select(keyword => keyword).Order().ShouldBe([101, 202]);
        features.KeywordNames.Order(StringComparer.Ordinal).ShouldBe(["Heist", "Noir"]);
        features.CollectionId.ShouldBe(900);
        interceptor.ReaderCommands.ShouldNotBeEmpty();
        interceptor.ReaderCommands.Max(AssociationJoins).ShouldBeLessThanOrEqualTo(1);
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

    [Fact]
    public async Task GetCandidatesAsync_CollectionIdArgument_DoesNotNarrowTheWholeLibraryLoad()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Selected", EnrichmentStatus.Enriched),
            Record(2, "Franchise vectorless", EnrichmentStatus.Enriched, collectionId: 900),
            Record(3, "Franchise second", EnrichmentStatus.Enriched, collectionId: 900),
            Record(4, "Other collection", EnrichmentStatus.Enriched, collectionId: 901),
            Record(5, "Franchise pending", EnrichmentStatus.Pending, collectionId: 900));
        await SeedVectorsAsync(
            ct,
            (1, UnitVector()),
            (4, CosineVector(0.6)));
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);

        // act
        var candidates = await source.GetCandidatesAsync(new MovieId(1), 900, ct);

        // assert
        candidates.Select(candidate => candidate.Library.Id.Value).Order().ShouldBe([2, 3, 4]);
        candidates.ShouldNotContain(candidate => candidate.Library.Id == new MovieId(1));
        candidates.ShouldNotContain(candidate => candidate.Library.Id == new MovieId(5));
        candidates.Where(candidate => candidate.Library.Id.Value is 2 or 3)
            .ShouldAllBe(candidate => candidate.CosineSimilarity == 0.0);
    }

    [Fact]
    public async Task GetLibraryFeaturesAsync_MissingFeatureRow_KeepsNonPlotInputs()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var signal = SignalRecord(2);
        signal.Directors.Add(new DirectorRecord { Name = "Director One" });
        await SeedRecordsAsync(
            ct,
            Record(1, "Selected", EnrichmentStatus.Enriched),
            signal);
        await SeedVectorsAsync(ct, (1, UnitVector()));
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);

        // act
        var library = await source.GetLibraryFeaturesAsync(ct);

        // assert
        var entry = library.Single(item => item.Id == new MovieId(2));
        entry.Summary.Title.ShouldBe("Signal member");
        entry.Summary.EnrichmentStatus.ShouldBe(EnrichmentStatus.Enriched);
        entry.Features.Directors.ShouldBe(["Director One"]);
        entry.Features.Cast.Order(StringComparer.Ordinal).ShouldBe(["Base Actor", "Guest Actor"]);
        entry.Features.Genres.ShouldBe(["Drama"]);
        entry.Features.CollectionId.ShouldBe(900);
        entry.Features.CollectionName.ShouldBe("Franchise 900");
        entry.Features.Keywords.Select(keyword => keyword).Order().ShouldBe([101, 202]);
        entry.Features.KeywordNames.Order(StringComparer.Ordinal).ShouldBe(["Heist", "Noir"]);
        entry.Features.PlotEmbedding.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCandidatesAsync_CollectionArgumentCancellation_Propagates()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            Record(1, "Selected", EnrichmentStatus.Enriched),
            Record(2, "Franchise", EnrichmentStatus.Enriched, collectionId: 900));
        await SeedVectorsAsync(ct, (1, UnitVector()));
        await using var db = fixture.CreateMigratedContext();
        var source = Source(db);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var token = cancelled.Token;

        // act
        var act = () => source.GetCandidatesAsync(new MovieId(1), 900, token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task LiveRelated_SharedDirectorCandidateWithoutFeatureRow_IsAdmittedBelowTheBarOnDirectorAlone()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            DirectedRecord(1, "Selected", director: "Shared Director", genre: "Drama"),
            DirectedRecord(2, "Director only", director: "Shared Director", genre: "Thriller"));
        await SeedVectorsAsync(ct, (1, UnitVector()));
        var options = new RecommendationOptions();
        await using var db = fixture.CreateMigratedContext();
        var catalog = new EfMovieCatalog(db);
        var library = await Source(db).GetLibraryFeaturesAsync(ct);
        var selected = library.Single(entry => entry.Id == new MovieId(1));
        var candidate = library.Single(entry => entry.Id == new MovieId(2));
        candidate.Features.PlotEmbedding.IsDefaultOrEmpty.ShouldBeTrue();

        // act
        var similarity = SimilarityScorer.Score(
            selected.Features,
            candidate.Features,
            options,
            await new GenreWeightsCalculator(catalog, new GenreWeightsCache()).GetAsync(ct),
            await new KeywordWeightsCalculator(catalog, new KeywordWeightsCache()).GetAsync(ct));
        var result = await NewRelatedHandler(db, options, RealProfileSource(db, options))
            .HandleAsync(new GetRecommendationsQuery(new MovieId(1)), ct);

        // assert
        double.IsFinite(similarity.Score).ShouldBeTrue();
        similarity.Score.ShouldBeLessThan(options.MinimumSimilarity);
        similarity.Breakdown.Single(term => term.Name == "Plot").Value.ShouldBe(0.0);
        similarity.Breakdown.Single(term => term.Name == "Director").Value.ShouldBe(1.0);
        result.Select(item => item.Summary.Id).ShouldBe([new MovieId(2)]);
        double.IsFinite(result[0].Score).ShouldBeTrue();
        result[0].Reasons.Select(reason => reason.Text).ShouldContain("Same director: Shared Director");
        result[0].Reasons.Select(reason => reason.Text).ShouldNotContain("Similar story");
    }

    [Fact]
    public async Task LiveRelated_SelectedMissingFeatureRow_ReturnsEmptyFromHandlerAndRetainedMethod()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(
            ct,
            DirectedRecord(1, "Selected", director: "Shared Director", genre: "Drama"),
            DirectedRecord(2, "Would match", director: "Shared Director", genre: "Thriller"));
        await using var db = fixture.CreateMigratedContext();
        var options = new RecommendationOptions();

        // act
        var retained = await Source(db).GetCandidatesAsync(new MovieId(1), null, ct);
        var live = await NewRelatedHandler(db, options, RealProfileSource(db, options))
            .HandleAsync(new GetRecommendationsQuery(new MovieId(1)), ct);

        // assert
        retained.ShouldBeEmpty();
        live.ShouldBeEmpty();
    }

    [Fact]
    public async Task LiveRelated_LibraryReadSuppliesCandidateEmbeddingsWithFlatReaderCount()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await SeedRecordsAsync(ct, DirectedRecord(1, "Selected", director: "Shared Director", genre: "Drama"));
        await SeedVectorsAsync(ct, (1, UnitVector()));
        await SeedDirectorCandidatesAsync(ct, startId: 100, count: 20, director: "Shared Director");
        await SeedVectorsAsync(ct, (100, UnitVector()));
        var small = await RunInstrumentedRelatedAsync(ct);

        await SeedDirectorCandidatesAsync(ct, startId: 200, count: 180, director: "Shared Director");

        // act
        var large = await RunInstrumentedRelatedAsync(ct);

        // assert
        small.Result.Count.ShouldBe(10);
        large.Result.Count.ShouldBe(10);
        small.Profile.CandidateIdCalls.ShouldNotBeEmpty();
        small.Profile.CandidateIdCalls.ShouldAllBe(ids => ids.Count == 0);
        large.Profile.CandidateIdCalls.ShouldAllBe(ids => ids.Count == 0);
        large.ReaderCount.ShouldBe(small.ReaderCount);
        small.Result.ShouldContain(item => item.Summary.Id == new MovieId(100)
            && item.Reasons.Any(reason => reason.Text == "Similar story"));
    }

    private async Task<(IReadOnlyList<RecommendedMovie> Result, int ReaderCount, RecordingProfileSource Profile)>
        RunInstrumentedRelatedAsync(CancellationToken ct)
    {
        var options = new RecommendationOptions();
        var interceptor = new RecordingCommandInterceptor();
        await using var db = OpenInstrumentedContext(interceptor);
        var profile = new RecordingProfileSource(RealProfileSource(db, options));
        var result = await NewRelatedHandler(db, options, profile)
            .HandleAsync(new GetRecommendationsQuery(new MovieId(1)), ct);
        return (result, interceptor.ReaderCount, profile);
    }

    private static GetRecommendationsQueryHandler NewRelatedHandler(
        LamuFlixDbContext db,
        RecommendationOptions options,
        IRecommendationProfileSource profileSource)
    {
        var catalog = new EfMovieCatalog(db);
        return new GetRecommendationsQueryHandler(
            Repository(db),
            Source(db),
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            new KeywordWeightsCalculator(catalog, new KeywordWeightsCache()),
            Microsoft.Extensions.Options.Options.Create(options));
    }

    private static IRecommendationProfileSource RealProfileSource(
        LamuFlixDbContext db,
        RecommendationOptions options)
    {
        var catalog = new EfMovieCatalog(db);
        return new EfRecommendationProfileSource(
            db,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(options));
    }

    private async Task SeedDirectorCandidatesAsync(
        CancellationToken ct,
        int startId,
        int count,
        string director) =>
        await SeedRecordsAsync(
            ct,
            [.. Enumerable.Range(startId, count)
                .Select(id => DirectedRecord(id, $"Director movie {id}", director, "Thriller"))]);

    private static MovieRecord DirectedRecord(int id, string title, string director, string genre)
    {
        var record = new MovieRecord
        {
            Id = new MovieId(id),
            Title = title,
            LibraryPath = new LibraryPath($"C:/library/directed-{id}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Enriched,
            MetadataTitle = title
        };
        record.Directors.Add(new DirectorRecord { Name = director });
        record.Genres.Add(new GenreRecord { Name = genre });
        return record;
    }

    private static int AssociationJoins(string command)
    {
        var counted = 0;
        foreach (var table in AssociationTables)
        {
            if (command.Contains(table, StringComparison.Ordinal))
            {
                counted++;
            }
        }

        return counted;
    }

    private static EfRecommendationCandidateSource Source(LamuFlixDbContext db) => new(db);

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

    private static void AssertSameState(RecommendationLibraryEntry actual, Movie expected)
    {
        actual.Id.ShouldBe(expected.Id);
        actual.Summary.Title.ShouldBe(expected.Title);
        actual.Summary.IsInWatchlist.ShouldBe(expected.IsInWatchlist);
        actual.Summary.IsWatched.ShouldBe(expected.IsWatched);
        actual.Summary.Review.ShouldBe(expected.Review);
        actual.Summary.EnrichmentStatus.ShouldBe(expected.Status);

        var expectedMetadata = expected.Metadata;
        if (expectedMetadata is null)
        {
            actual.Features.Genres.ShouldBeEmpty();
            actual.Features.Directors.ShouldBeEmpty();
            return;
        }

        AssertMetadataContents(actual.Features, expectedMetadata);
    }

    private static void AssertMetadataContents(MovieFeatures actual, MovieMetadata expected)
    {
        actual.ReleaseYear.ShouldBe(expected.ReleaseYear);
        actual.Runtime.ShouldBe(expected.Runtime);
        actual.ImdbRating.ShouldBe(expected.ImdbRating);
        actual.FullPlot.ShouldBe(expected.Synopsis);
        actual.Directors.OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(expected.Directors.OrderBy(name => name, StringComparer.Ordinal));
        actual.Cast.OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(expected.Actors.OrderBy(name => name, StringComparer.Ordinal));
        actual.Genres.OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(expected.Genres.OrderBy(name => name, StringComparer.Ordinal));
        actual.Writers.OrderBy(name => name, StringComparer.Ordinal)
            .ShouldBe(expected.Writers.OrderBy(name => name, StringComparer.Ordinal));
    }

    private async Task SeedRecordsAsync(CancellationToken ct, params MovieRecord[] records)
    {
        await using var db = fixture.CreateMigratedContext();
        var collections = records
            .Select(record => record.CollectionId)
            .OfType<int>()
            .Distinct()
            .Select(id => new CollectionRecord { Id = id, Name = $"Franchise {id}" })
            .ToList();
        db.Collections.AddRange(collections);
        await db.SaveChangesAsync(ct);
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
            review: isNotInterested ? Review.Disliked : Review.None);
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

    private static MovieRecord Record(int id, string title, EnrichmentStatus status, int? collectionId = null) =>
        new()
        {
            Id = new MovieId(id),
            Title = title,
            LibraryPath = new LibraryPath($"C:/library/candidate-{id}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = status,
            CollectionId = collectionId
        };

    private static MovieRecord SignalRecord(int id)
    {
        var record = new MovieRecord
        {
            Id = new MovieId(id),
            Title = "Signal member",
            LibraryPath = new LibraryPath($"C:/library/candidate-signal-{id}.mkv"),
            Format = new MediaFormat("mkv"),
            Status = EnrichmentStatus.Enriched,
            MetadataTitle = "Signal member",
            CollectionId = 900
        };
        record.Actors.Add(new ActorRecord { Name = "Base Actor" });
        record.Genres.Add(new GenreRecord { Name = "Drama" });
        record.Keywords.Add(new KeywordRecord { Id = 202, Name = "Noir" });
        record.Keywords.Add(new KeywordRecord { Id = 101, Name = "Heist" });
        record.FullCast.Add(new ActorRecord { Name = "Base Actor" });
        record.FullCast.Add(new ActorRecord { Name = "Guest Actor" });
        return record;
    }

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

    private sealed class RecordingProfileSource(IRecommendationProfileSource inner) : IRecommendationProfileSource
    {
        public List<IReadOnlyCollection<MovieId>> CandidateIdCalls { get; } = [];

        public Task<TasteProfile> GetTasteProfileAsync(CancellationToken ct) => inner.GetTasteProfileAsync(ct);

        public Task<RecommendationProfileInputs> GetProfileInputsAsync(
            IReadOnlyCollection<MovieId> candidateMovieIds,
            IReadOnlyCollection<MovieId> likedSourceIds,
            CancellationToken ct)
        {
            CandidateIdCalls.Add(candidateMovieIds);
            return inner.GetProfileInputsAsync(candidateMovieIds, likedSourceIds, ct);
        }
    }

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
