using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GetForYouRecommendationsQueryHandlerTests
{
    private const int DefaultTake = 10;
    private const double Tolerance = 1e-9;

    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
            ["Horror"] = 5,
        });

    private static readonly SimilarityWeights GenreOnlyWeights = new()
    {
        Director = 0.0,
        Themes = 0.0,
        Plot = 0.0,
        Cast = 0.0,
        Genres = 3.0,
        Collection = 0.0,
    };

    private static readonly SimilarityWeights PlotOnlyWeights = new()
    {
        Director = 0.0,
        Themes = 0.0,
        Plot = 1.0,
        Cast = 0.0,
        Genres = 0.0,
        Collection = 0.0,
    };

    private static readonly SimilarityWeights DirectorAndGenreWeights = new()
    {
        Director = 3.0,
        Themes = 0.0,
        Plot = 0.0,
        Cast = 0.0,
        Genres = 12.0,
        Collection = 0.0,
    };

    private static readonly SimilarityWeights DirectorGenreAndPlotWeights = new()
    {
        Director = 3.0,
        Themes = 0.0,
        Plot = 10.0,
        Cast = 0.0,
        Genres = 2.0,
        Collection = 0.0,
    };

    private static readonly KeywordCounts EmptyKeywordCounts = new(0, new Dictionary<string, long>());

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IRecommendationCandidateSource candidateSource = Substitute.For<IRecommendationCandidateSource>();
    private readonly IRecommendationProfileSource profileSource = Substitute.For<IRecommendationProfileSource>();
    private Movie[] libraryMovies = [];
    private ImmutableDictionary<MovieId, ImmutableArray<float>> plotVectors =
        ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty;
    private ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>> playback =
        ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>.Empty;

    public GetForYouRecommendationsQueryHandlerTests()
    {
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Counts));
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(EmptyKeywordCounts));
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<RecommendationLibraryEntry>>(LibraryEntries()));
        profileSource.GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(Profile()));
    }

    [Fact]
    public async Task HandleAsync_LikedSeed_EmitsSeedLineThenPairEvidence()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, title: "Alien", genres: ["Drama"], isLiked: true),
            NewMovie(2, title: "Drama Kid", genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var item = result.Items.ShouldHaveSingleItem();
        item.Score.ShouldBe(1.0, Tolerance);
        item.Seed.ShouldBe(new RecommendationSeed(90, "Alien", SeedSignal.Liked));
        item.Evidence.ShouldBe(
        [
            new RecommendationEvidence("Genre", "Same genres: Drama", ["Drama"]),
        ]);
    }

    [Fact]
    public async Task HandleAsync_WatchedWithoutReviewSeed_EmitsWatchedTemplate()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, title: "Alien", genres: ["Drama"], isWatched: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var item = result.Items.ShouldHaveSingleItem();
        item.Score.ShouldBe(0.6, Tolerance);
        item.Seed.ShouldBe(new RecommendationSeed(90, "Alien", SeedSignal.WatchedWithoutReview));
    }

    [Fact]
    public async Task HandleAsync_WatchlistOnlySeed_EmitsWatchlistTemplate()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, title: "Alien", genres: ["Drama"], isInWatchlist: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var item = result.Items.ShouldHaveSingleItem();
        item.Score.ShouldBe(0.4, Tolerance);
        item.Seed.ShouldBe(new RecommendationSeed(90, "Alien", SeedSignal.Watchlist));
    }

    [Fact]
    public async Task HandleAsync_WatchedAndLikedSeed_IsCountedOnceAsLiked()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, title: "Alien", genres: ["Drama"], isWatched: true, isLiked: true, isInWatchlist: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var item = result.Items.ShouldHaveSingleItem();
        item.Score.ShouldBe(1.0, Tolerance);
        item.Seed.ShouldBe(new RecommendationSeed(90, "Alien", SeedSignal.Liked));
    }

    [Fact]
    public async Task HandleAsync_WatchlistedCandidate_DoesNotMatchItselfAndReturnsEmpty()
    {
        // arrange
        var handler = NewHandler();
        Arrange(NewMovie(2, genres: ["Drama"], isInWatchlist: true));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        result.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_NoPositiveSeeds_ReturnsEmptyWithoutPadding()
    {
        // arrange
        var handler = NewHandler();
        Arrange(NewMovie(2, genres: ["Drama"]), NewMovie(3, genres: ["Comedy"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        await candidateSource.Received(1).GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WeakAggregateOfSubBarPairs_ReturnsEmptyWithoutPadding()
    {
        // arrange
        var handler = NewHandler(weights: PlotOnlyWeights);
        var weak = VectorAtSimilarity(0.2);
        Arrange(
            NewMovie(90, genres: ["Drama"], isWatched: true),
            NewMovie(91, genres: ["Drama"], isWatched: true),
            NewMovie(92, genres: ["Drama"], isWatched: true),
            NewMovie(2, genres: ["Drama"]));
        ArrangePlot(
            (new MovieId(90), weak),
            (new MovieId(91), weak),
            (new MovieId(92), weak),
            (new MovieId(2), PlotVector(1.0f)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_PairAtConfiguredBar_Qualifies()
    {
        // arrange
        var handler = NewHandler(minimumSimilarity: 1.0, weights: PlotOnlyWeights);
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));
        ArrangePlot(
            (new MovieId(90), PlotVector(1.0f)),
            (new MovieId(2), PlotVector(1.0f)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldHaveSingleItem().Score.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public async Task HandleAsync_PairBelowConfiguredBar_IsRejected()
    {
        // arrange
        var handler = NewHandler(minimumSimilarity: 1.0, weights: PlotOnlyWeights);
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));
        ArrangePlot(
            (new MovieId(90), VectorAtSimilarity(0.9)),
            (new MovieId(2), PlotVector(1.0f)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_DirectorOnlyStrongMatch_QualifiesAndNamesTheSeed()
    {
        // arrange
        var handler = NewHandler(weights: DirectorAndGenreWeights);
        Arrange(
            NewMovie(90, title: "Alien", genres: ["Comedy"], directors: ["Ridley Scott"], isLiked: true),
            NewMovie(2, genres: ["Drama"], directors: ["Ridley Scott"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var item = result.Items.ShouldHaveSingleItem();
        item.Score.ShouldBe(0.2, Tolerance);
        item.Seed.ShouldBe(new RecommendationSeed(90, "Alien", SeedSignal.Liked));
        item.Evidence.ShouldContain(new RecommendationEvidence("Director", "Same director: Ridley Scott", ["Ridley Scott"]));
    }

    [Fact]
    public async Task HandleAsync_CloserToDislikedSeed_RanksLower()
    {
        // arrange
        var handler = NewHandler(weights: PlotOnlyWeights);
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(91, genres: ["Drama"], isLiked: true),
            NewMovie(95, genres: ["Drama"], isNotInterested: true),
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama"]));
        ArrangePlot(
            (new MovieId(90), PlotVector(1.0f)),
            (new MovieId(91), PlotVector(0.0f, 1.0f)),
            (new MovieId(95), PlotVector(1.0f)),
            (new MovieId(2), PlotVector(1.0f)),
            (new MovieId(3), PlotVector(0.0f, 1.0f)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Where(item => item.Summary.Id.Value is 2 or 3)
            .Select(item => item.Summary.Id.Value)
            .ShouldBe([3, 2]);
        result.Items.Single(item => item.Summary.Id.Value == 2).Score.ShouldBe(0.0, Tolerance);
        result.Items.Single(item => item.Summary.Id.Value == 3).Score.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public async Task HandleAsync_WatchlistedAndDislikedMovie_ContributesOnlyThePenalty()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, title: "Alien", genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"], isInWatchlist: true, isNotInterested: true),
            NewMovie(3, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var item = result.Items.ShouldHaveSingleItem();
        item.Summary.Id.ShouldBe(new MovieId(3));
        item.Score.ShouldBe(0.0, Tolerance);
        item.Seed.ShouldBe(new RecommendationSeed(90, "Alien", SeedSignal.Liked));
    }

    [Fact]
    public async Task HandleAsync_TopThreeAggregation_AppliesRankMultipliers()
    {
        // arrange
        var handler = NewHandler(weights: PlotOnlyWeights);
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(91, genres: ["Drama"], isLiked: true),
            NewMovie(92, genres: ["Drama"], isLiked: true),
            NewMovie(93, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));
        ArrangePlot(
            (new MovieId(90), PlotVector(1.0f)),
            (new MovieId(91), PlotVector(1.0f)),
            (new MovieId(92), PlotVector(1.0f)),
            (new MovieId(93), PlotVector(1.0f)),
            (new MovieId(2), PlotVector(1.0f)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(5);
        result.Items.Single(item => item.Summary.Id.Value == 2).Score.ShouldBe(1.75, Tolerance);
    }

    [Fact]
    public async Task HandleAsync_RecencyFloor_LowersOldSeedWithoutScalingWatchlistSeed()
    {
        // arrange
        var handler = NewHandler(weights: PlotOnlyWeights);
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(91, genres: ["Drama"], isLiked: true),
            NewMovie(92, genres: ["Drama"], isInWatchlist: true),
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama"]),
            NewMovie(4, genres: ["Drama"]));
        ArrangePlot(
            (new MovieId(90), PlotVector(1.0f)),
            (new MovieId(91), PlotVector(0.0f, 1.0f)),
            (new MovieId(92), PlotVector(0.0f, 0.0f, 1.0f)),
            (new MovieId(2), PlotVector(1.0f)),
            (new MovieId(3), PlotVector(0.0f, 1.0f)),
            (new MovieId(4), PlotVector(0.0f, 0.0f, 1.0f)));
        ArrangePlayback(
            (new MovieId(90), Now.AddDays(-400)),
            (new MovieId(91), Now.AddDays(-1)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Single(item => item.Summary.Id.Value == 2).Score.ShouldBe(0.25, Tolerance);
        result.Items.Single(item => item.Summary.Id.Value == 3).Score.ShouldBe(1.0, Tolerance);
        result.Items.Single(item => item.Summary.Id.Value == 4).Score.ShouldBe(0.4, Tolerance);
        result.Items.Where(item => item.Summary.Id.Value is 2 or 3 or 4)
            .Select(item => item.Summary.Id.Value)
            .First()
            .ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_WatchlistingAnotherMovie_RaisesSimilarCandidateScoreWithUnchangedFeatures()
    {
        // arrange
        var handler = NewHandler();
        var candidate = NewMovie(2, genres: ["Drama"]);
        Arrange(NewMovie(90, genres: ["Drama"], isLiked: true), candidate);
        var baseline = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // act
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(91, genres: ["Drama"], isInWatchlist: true),
            candidate);
        var watchlisted = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var after = watchlisted.Items.Single(item => item.Summary.Id.Value == 2);
        after.Summary.ShouldBe(baseline.Items.Single(item => item.Summary.Id.Value == 2).Summary);
        after.Score.ShouldBe(1.2, Tolerance);
    }

    [Fact]
    public async Task HandleAsync_BestStrongSeedOutsideTopThreeProducts_IsStillNamed()
    {
        // arrange
        var handler = NewHandler(weights: DirectorGenreAndPlotWeights);
        var weakVector = PlotVector(0.0f, 1.0f);
        Arrange(
            NewMovie(10, genres: ["Drama"], isLiked: true),
            NewMovie(11, genres: ["Drama"], isLiked: true),
            NewMovie(12, genres: ["Drama"], isLiked: true),
            NewMovie(13, title: "Director Seed", genres: ["Comedy"], directors: ["Ridley Scott"], isLiked: true),
            NewMovie(2, genres: ["Drama"], directors: ["Ridley Scott"]));
        ArrangePlot(
            (new MovieId(10), weakVector),
            (new MovieId(11), weakVector),
            (new MovieId(12), weakVector),
            (new MovieId(13), weakVector),
            (new MovieId(2), PlotVector(1.0f)));
        ArrangePlayback(
            (new MovieId(10), Now.AddDays(-1)),
            (new MovieId(13), Now.AddDays(-400)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var item = result.Items.Single(movie => movie.Summary.Id.Value == 2);
        item.Seed.ShouldBe(new RecommendationSeed(13, "Director Seed", SeedSignal.Liked));
        item.Score.ShouldBe(0.23333333333333334, Tolerance);
    }

    [Fact]
    public async Task HandleAsync_DeterministicTies_OrderByAscendingMovieId()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(5, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Select(item => item.Summary.Id.Value).ShouldBe([3, 5]);
    }

    [Fact]
    public async Task HandleAsync_SeedTitleWithBracesAndMarkup_IsPreservedLiterally()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, title: "Alien {Director's} <b>Cut</b>", genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldHaveSingleItem().Seed.ShouldBe(
            new RecommendationSeed(90, "Alien {Director's} <b>Cut</b>", SeedSignal.Liked));
    }

    [Fact]
    public async Task HandleAsync_SupersededGenericTemplates_AreAbsent()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var texts = result.Items.SelectMany(item => item.Evidence).Select(evidence => evidence.Text).ToArray();
        texts.ShouldNotContain("Matches your viewing history");
        texts.ShouldNotContain("Matches your Drama interest");
        texts.ShouldNotContain("No shared features identified");
    }

    [Fact]
    public async Task HandleAsync_EveryItem_EmitsPopulatedSeedWithNonblankTitle()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, title: "Alien", genres: ["Drama"], isLiked: true),
            NewMovie(91, title: "Arrival", genres: ["Drama"], isInWatchlist: true),
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama"]),
            NewMovie(4, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(5), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBeGreaterThan(1);
        foreach (var item in result.Items)
        {
            item.Seed.ShouldNotBeNull();
            item.Seed.Title.Any(character => !char.IsWhiteSpace(character) && !char.IsControl(character)).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task HandleAsync_MmrLambda_DoesNotChangePublishedOrder()
    {
        // arrange
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"], collectionId: 7),
            NewMovie(3, genres: ["Drama"], collectionId: 7),
            NewMovie(4, genres: ["Drama"]),
            NewMovie(5, genres: ["Drama"]));

        // act
        var relevance = await NewHandler(mmrLambda: 1.0)
            .HandleAsync(new GetForYouRecommendationsQuery(4), CancellationToken.None);
        var diversified = await NewHandler(mmrLambda: 0.0)
            .HandleAsync(new GetForYouRecommendationsQuery(4), CancellationToken.None);

        // assert
        diversified.Items.Select(item => item.Summary.Id.Value)
            .ShouldBe(relevance.Items.Select(item => item.Summary.Id.Value));
    }

    [Fact]
    public async Task HandleAsync_PagingPublishesSignedOrderSlicesWithHasMore()
    {
        // arrange
        var handler = NewHandler(weights: PlotOnlyWeights, take: 2, candidatePoolSize: 10);
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama"]),
            NewMovie(4, genres: ["Drama"]),
            NewMovie(5, genres: ["Drama"]));
        ArrangePlot(
            (new MovieId(90), PlotVector(1.0f)),
            (new MovieId(2), PlotVector(1.0f)),
            (new MovieId(3), VectorAtSimilarity(0.9)),
            (new MovieId(4), VectorAtSimilarity(0.8)),
            (new MovieId(5), VectorAtSimilarity(0.7)));

        // act
        var first = await handler.HandleAsync(new GetForYouRecommendationsQuery(2), CancellationToken.None);
        var second = await handler.HandleAsync(new GetForYouRecommendationsQuery(2, 2), CancellationToken.None);

        // assert
        first.Items.Select(item => item.Summary.Id.Value).ShouldBe([2, 3]);
        first.HasMore.ShouldBeTrue();
        second.Items.Select(item => item.Summary.Id.Value).ShouldBe([4, 5]);
        second.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_HugeOffsetReturnsEmpty()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(
            new GetForYouRecommendationsQuery(10, int.MaxValue),
            CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        result.HasMore.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_ExcludedMovieId_IsNotReturned()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(
            new GetForYouRecommendationsQuery(excludedMovieIds: ImmutableHashSet.Create(new MovieId(2))),
            CancellationToken.None);

        // assert
        result.Items.Select(item => item.Summary.Id.Value).ShouldBe([3]);
    }

    [Fact]
    public async Task HandleAsync_WatchedCandidate_IsExcluded()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isWatched: true, isLiked: true),
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama"], isWatched: true));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Select(item => item.Summary.Id.Value).ShouldBe([2]);
    }

    [Fact]
    public async Task HandleAsync_DislikedCandidate_IsExcluded()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"], isNotInterested: true),
            NewMovie(3, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Select(item => item.Summary.Id.Value).ShouldBe([3]);
    }

    [Fact]
    public async Task HandleAsync_CandidatePoolSize_CapsTake()
    {
        // arrange
        var handler = NewHandler(take: 50, candidatePoolSize: 50);
        var library = new List<Movie> { NewMovie(90, genres: ["Drama"], isLiked: true) };
        library.AddRange(Enumerable.Range(2, 60).Select(id => NewMovie(id, genres: ["Drama"])));
        Arrange([.. library]);

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(60), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(50);
    }

    [Fact]
    public async Task HandleAsync_CollectionCutKeepsEarliestKnownYearThenLowestMovieId()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(3, genres: ["Drama"], collectionId: 7),
            NewMovie(4, genres: ["Drama"], collectionId: 7, releaseYear: 2000),
            NewMovie(2, genres: ["Drama"], collectionId: 7, releaseYear: 2000),
            NewMovie(5, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var ids = result.Items.Select(item => item.Summary.Id.Value).ToArray();
        ids.ShouldContain(2);
        ids.ShouldContain(5);
        ids.ShouldNotContain(3);
        ids.ShouldNotContain(4);
    }

    [Fact]
    public async Task HandleAsync_OmittedTake_ReturnsTen()
    {
        // arrange
        var handler = NewHandler();
        var library = new List<Movie> { NewMovie(90, genres: ["Drama"], isLiked: true) };
        library.AddRange(Enumerable.Range(2, 12).Select(id => NewMovie(id, genres: ["Drama"])));
        Arrange([.. library]);

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(DefaultTake);
    }

    [Fact]
    public async Task HandleAsync_FewerQualifiedThanTake_ReturnsEveryQualifiedMovie()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(5), CancellationToken.None);

        // assert
        result.Items.Count.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_ResultSummary_CarriesEveryField()
    {
        // arrange
        var handler = NewHandler();
        var candidate = NewMovie(
            7,
            title: "Seven",
            posterUrl: "https://posters/seven.jpg",
            releaseYear: 1999,
            runtimeMinutes: 101,
            imdbRating: 7.5m,
            genres: ["Drama"],
            isInWatchlist: true);
        Arrange(NewMovie(90, genres: ["Drama"], isLiked: true), candidate);

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(1), CancellationToken.None);

        // assert
        var expected = new MovieSummary(
            new MovieId(7),
            "Seven",
            "https://posters/seven.jpg",
            1999,
            101,
            7.5m,
            EnrichmentStatus.Enriched,
            true,
            false,
            Review.None);
        result.Items.ShouldHaveSingleItem().Summary.ShouldBe(expected);
    }

    [Fact]
    public async Task HandleAsync_OneLibraryLoad_AndNoProfileInputsRead()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        await candidateSource.Received(1).GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
        await profileSource.Received(1).GetTasteProfileAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoEligibleCandidates_ReturnsEmptyWithoutProfile()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(2, genres: ["Drama"], isWatched: true),
            NewMovie(3, genres: ["Drama"], isNotInterested: true));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_PreCancelledToken_PerformsNoDependencyCalls()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await candidateSource.DidNotReceive().GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_LibraryLoadCancelsToken_ThrowsBeforeProfileAcquisition()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult<IReadOnlyList<RecommendationLibraryEntry>>([]);
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProfileCancelsToken_ThrowsBeforeGenreWeights()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));
        profileSource.GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(Profile());
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await catalog.DidNotReceive().GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_GenreWeightsCancelToken_ThrowsAfterProfileAcquisition()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(Counts);
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await profileSource.Received(1).GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CandidateInCollectionWithWatchedMember_CarriesStartedCollectionId()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(3, genres: ["Drama"], collectionId: 7, isWatched: true),
            NewMovie(2, genres: ["Drama"], collectionId: 7));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Single(item => item.Summary.Id.Value == 2).StartedCollectionId.ShouldBe(7);
    }

    [Fact]
    public async Task HandleAsync_CandidateInCollectionWithoutWatchedMember_OmitsStartedCollectionId()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(3, genres: ["Drama"], collectionId: 7, isInWatchlist: true),
            NewMovie(2, genres: ["Drama"], collectionId: 7));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Single(item => item.Summary.Id.Value == 2).StartedCollectionId.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_CandidateWithoutCollection_OmitsStartedCollectionId()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(3, genres: ["Drama"], collectionId: 7, isWatched: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Single(item => item.Summary.Id.Value == 2).StartedCollectionId.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_ExcludedWatchedCollectionMember_StillQualifiesCollection()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(3, genres: ["Drama"], collectionId: 7, isWatched: true),
            NewMovie(2, genres: ["Drama"], collectionId: 7));

        // act
        var result = await handler.HandleAsync(
            new GetForYouRecommendationsQuery(excludedMovieIds: ImmutableHashSet.Create(new MovieId(3))),
            CancellationToken.None);

        // assert
        result.Items.Single(item => item.Summary.Id.Value == 2).StartedCollectionId.ShouldBe(7);
    }

    [Fact]
    public async Task HandleAsync_DislikedWatchedCollectionMember_StillQualifiesCollection()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(3, genres: ["Drama"], collectionId: 7, isWatched: true, isNotInterested: true),
            NewMovie(2, genres: ["Drama"], collectionId: 7));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.Single(item => item.Summary.Id.Value == 2).StartedCollectionId.ShouldBe(7);
    }

    [Fact]
    public async Task HandleAsync_ExclusionsEmptyEligibility_RetainsHasSeeds()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(
            new GetForYouRecommendationsQuery(excludedMovieIds: ImmutableHashSet.Create(new MovieId(2), new MovieId(90))),
            CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        result.HasSeeds.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_RankingSignalGuard_RetainsHasSeeds()
    {
        // arrange
        var handler = NewHandler();
        Arrange(NewMovie(2, genres: ["Drama"], isInWatchlist: true));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        result.HasSeeds.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_OffsetPastLastRetained_RetainsHasSeeds()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(
            new GetForYouRecommendationsQuery(10, int.MaxValue),
            CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        result.HasSeeds.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_WatchlistOnlyLibrary_ReportsHasSeeds()
    {
        // arrange
        var handler = NewHandler();
        Arrange(NewMovie(2, genres: ["Drama"], isInWatchlist: true));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        result.HasSeeds.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_PlaybackHistoryOnlyLibrary_ReportsNoSeeds()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama"]));
        ArrangePlayback(
            (new MovieId(2), Now.AddDays(-1)),
            (new MovieId(3), Now.AddDays(-2)));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        result.HasSeeds.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_WatchedOnlyLibrary_ReportsHasSeeds()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isWatched: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldHaveSingleItem();
        result.HasSeeds.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_NoSeedsAtAll_ReportsNoSeeds()
    {
        // arrange
        var handler = NewHandler();
        Arrange(NewMovie(2, genres: ["Drama"]), NewMovie(3, genres: ["Comedy"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        result.HasSeeds.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_OffsetEqualsRetainedCount_ReturnsEmptyWithoutMore()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(10, 2), CancellationToken.None);

        // assert
        result.Items.ShouldBeEmpty();
        result.HasMore.ShouldBeFalse();
        result.HasSeeds.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_KeywordWeightsCancelToken_ThrowsBeforeRanking()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"]));
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(EmptyKeywordCounts);
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetForYouRecommendationsQuery(), cts.Token));
        await profileSource.Received(1).GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CollectionCutPrefersEarliestKnownYear()
    {
        // arrange
        var handler = NewHandler();
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"], collectionId: 7, releaseYear: 2005),
            NewMovie(9, genres: ["Drama"], collectionId: 7, releaseYear: 1999),
            NewMovie(5, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var ids = result.Items.Select(item => item.Summary.Id.Value).ToArray();
        ids.ShouldContain(9);
        ids.ShouldContain(5);
        ids.ShouldNotContain(2);
    }

    [Fact]
    public async Task HandleAsync_CollectionCutWithHigherCapRetainsEarliestTwo()
    {
        // arrange
        var handler = NewHandler(maxPerCollection: 2);
        Arrange(
            NewMovie(90, genres: ["Drama"], isLiked: true),
            NewMovie(2, genres: ["Drama"], collectionId: 7, releaseYear: 2001),
            NewMovie(3, genres: ["Drama"], collectionId: 7),
            NewMovie(4, genres: ["Drama"], collectionId: 7, releaseYear: 2000),
            NewMovie(5, genres: ["Drama"]));

        // act
        var result = await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);

        // assert
        var ids = result.Items.Select(item => item.Summary.Id.Value).ToArray();
        ids.ShouldContain(4);
        ids.ShouldContain(2);
        ids.ShouldContain(5);
        ids.ShouldNotContain(3);
    }

    private GetForYouRecommendationsQueryHandler NewHandler(
        int take = DefaultTake,
        int candidatePoolSize = 50,
        int maxPerCollection = 1,
        IReadOnlyList<double>? rankMultipliers = null,
        double seedWeightLiked = RecommendationOptions.DefaultSeedWeightLiked,
        double seedWeightWatched = RecommendationOptions.DefaultSeedWeightWatched,
        double seedWeightWatchlist = RecommendationOptions.DefaultSeedWeightWatchlist,
        double dislikedSeedWeight = RecommendationOptions.DefaultDislikedSeedWeight,
        double recencyFloor = RecommendationOptions.DefaultRecencyFloor,
        double minimumSimilarity = 0.225,
        double mmrLambda = 0.7,
        SimilarityWeights? weights = null) =>
        new(
            candidateSource,
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            new KeywordWeightsCalculator(catalog, new KeywordWeightsCache()),
            new FixedTimeProvider(Now),
            Microsoft.Extensions.Options.Options.Create(new RecommendationOptions
            {
                Take = take,
                CandidatePoolSize = candidatePoolSize,
                MaxPerCollection = maxPerCollection,
                SeedRankMultipliers = rankMultipliers ?? [1.0, 0.5, 0.25],
                SeedWeightLiked = seedWeightLiked,
                SeedWeightWatched = seedWeightWatched,
                SeedWeightWatchlist = seedWeightWatchlist,
                DislikedSeedWeight = dislikedSeedWeight,
                RecencyFloor = recencyFloor,
                MinimumSimilarity = minimumSimilarity,
                MmrLambda = mmrLambda,
                Weights = weights ?? GenreOnlyWeights,
            }));

    private void Arrange(params Movie[] library)
    {
        libraryMovies = library;
        plotVectors = ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty;
        playback = ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>.Empty;
    }

    private void ArrangePlot(params (MovieId MovieId, ImmutableArray<float> Vector)[] vectors) =>
        plotVectors = vectors.ToImmutableDictionary(
            entry => entry.MovieId,
            entry => entry.Vector);

    private void ArrangePlayback(params (MovieId MovieId, DateTimeOffset PlayedAt)[] entries) =>
        playback = entries.ToImmutableDictionary(
            entry => entry.MovieId,
            entry => ImmutableArray.Create(entry.PlayedAt));

    private List<RecommendationLibraryEntry> LibraryEntries() =>
    [
        .. libraryMovies.Select(movie => new RecommendationLibraryEntry(
            MovieFeaturesMapper.From(
                movie,
                includeSignals: true,
                plotEmbedding: plotVectors.GetValueOrDefault(movie.Id, ImmutableArray<float>.Empty)),
            MovieSummaryMapper.From(movie))),
    ];

    private TasteProfile Profile() =>
        new(ImmutableDictionary<string, double>.Empty)
        {
            PlaybackByMovieId = playback,
        };

    private static ImmutableArray<float> PlotVector(float primary, float secondary = 0.0f, float tertiary = 0.0f)
    {
        var builder = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);
        builder.Add(primary);
        builder.Add(secondary);
        builder.Add(tertiary);

        for (var index = 3; index < EmbeddingDimensions.Value; index++)
        {
            builder.Add(0.0f);
        }

        return builder.MoveToImmutable();
    }

    private static ImmutableArray<float> VectorAtSimilarity(double similarity)
    {
        var cosine = 0.16 + (similarity * 0.29);
        return PlotVector((float)cosine, (float)Math.Sqrt(1.0 - (cosine * cosine)));
    }

    private static Movie NewMovie(
        int id,
        string? title = null,
        string? posterUrl = null,
        int? releaseYear = null,
        int? runtimeMinutes = null,
        decimal? imdbRating = null,
        string[]? genres = null,
        string[]? directors = null,
        string[]? actors = null,
        string[]? writers = null,
        bool withMetadata = true,
        bool isInWatchlist = false,
        bool isWatched = false,
        bool isLiked = false,
        bool isNotInterested = false,
        int? collectionId = null,
        string? collectionName = null,
        MovieSignalSnapshot? signals = null)
    {
        var movieTitle = title ?? $"Movie {id}";
        var metadata = withMetadata
            ? BuildMetadata(movieTitle, posterUrl, releaseYear, runtimeMinutes, imdbRating, genres, directors, actors, writers, collectionId, collectionName, signals)
            : null;
        var review = isNotInterested ? Review.Disliked : isLiked ? Review.Liked : Review.None;

        return Movie.Rehydrate(
            new MovieId(id),
            movieTitle,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            isInWatchlist,
            metadata,
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null,
            isWatched: isWatched,
            review: review);
    }

    private static MovieMetadata BuildMetadata(
        string movieTitle,
        string? posterUrl,
        int? releaseYear,
        int? runtimeMinutes,
        decimal? imdbRating,
        string[]? genres,
        string[]? directors,
        string[]? actors,
        string[]? writers,
        int? collectionId = null,
        string? collectionName = null,
        MovieSignalSnapshot? signals = null) =>
        new(
            movieTitle,
            posterUrl: posterUrl,
            releaseYear: releaseYear is null ? null : new ReleaseYear(releaseYear.Value, Now),
            runtime: runtimeMinutes is null ? null : new Runtime(runtimeMinutes.Value),
            imdbRating: imdbRating is null ? null : new ImdbRating(imdbRating.Value),
            directors: directors,
            actors: actors,
            genres: genres,
            writers: writers,
            collectionId: collectionId,
            collectionName: collectionName ?? (collectionId.HasValue ? $"Collection {collectionId}" : null),
            signals: signals);
}

public sealed class GetForYouRecommendationsQueryHandlerPropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
            ["Mystery"] = 5,
        });

    private static readonly SimilarityWeights GenreOnlyWeights = new()
    {
        Director = 0.0,
        Themes = 0.0,
        Plot = 0.0,
        Cast = 0.0,
        Genres = 3.0,
        Collection = 0.0,
    };

    [Property(MaxTest = 100, Replay = "357001,100003")]
    [Trait("Category", "Property")]
    public bool HandleAsync_GeneratedLibrary_ReturnsAtMostTakeUniqueEligibleIds(int[] ids, int takeSeed)
    {
        // arrange
        var library = BuildLibrary(ids);
        var take = Take(takeSeed);

        // act
        var result = Handle(library, take);

        // assert
        var eligible = library.Where(movie => !movie.IsNotInterested && !movie.IsWatched).Select(movie => movie.Id).ToHashSet();
        return result.Count <= take
            && result.Select(item => item.Summary.Id).Distinct().Count() == result.Count
            && result.All(item => eligible.Contains(item.Summary.Id));
    }

    [Property(MaxTest = 100, Replay = "357002,200009")]
    [Trait("Category", "Property")]
    public bool HandleAsync_GeneratedLibrary_ScoresAreFiniteAndDescending(int[] ids, int takeSeed)
    {
        // arrange
        var library = BuildLibrary(ids);
        var take = Take(takeSeed);

        // act
        var result = Handle(library, take);

        // assert
        var scores = result.Select(item => item.Score).ToArray();
        return scores.All(double.IsFinite) && scores.Zip(scores.Skip(1)).All(pair => pair.First >= pair.Second);
    }

    [Property(MaxTest = 100, Replay = "357003,300011")]
    [Trait("Category", "Property")]
    public bool HandleAsync_PermutedLibrary_ReturnsIdenticalOrdering(int[] ids, int takeSeed)
    {
        // arrange
        var library = BuildLibrary(ids);
        var take = Take(takeSeed);

        // act
        var forward = Handle(library, take);
        var reversed = Handle([.. library.Reverse()], take);

        // assert
        return SameSequence(forward, reversed);
    }

    private static ImmutableList<RecommendedMovie> Handle(IReadOnlyList<Movie> library, int take)
    {
        var catalog = Substitute.For<IMovieCatalog>();
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(Counts));
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new KeywordCounts(0, new Dictionary<string, long>())));

        var candidates = Substitute.For<IRecommendationCandidateSource>();
        candidates.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<RecommendationLibraryEntry>>(
            [
                .. library.Select(movie => new RecommendationLibraryEntry(
                    MovieFeaturesMapper.From(movie, includeSignals: true),
                    MovieSummaryMapper.From(movie))),
            ]));

        var profileSource = Substitute.For<IRecommendationProfileSource>();
        profileSource
            .GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TasteProfile(ImmutableDictionary<string, double>.Empty)));

        var handler = new GetForYouRecommendationsQueryHandler(
            candidates,
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            new KeywordWeightsCalculator(catalog, new KeywordWeightsCache()),
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(new RecommendationOptions
            {
                Weights = GenreOnlyWeights,
            }));

        return handler
            .HandleAsync(new GetForYouRecommendationsQuery(take), CancellationToken.None)
            .GetAwaiter()
            .GetResult()
            .Items;
    }

    private static bool SameSequence(ImmutableList<RecommendedMovie> first, ImmutableList<RecommendedMovie> second) =>
        first.Count == second.Count
        && first.Zip(second).All(pair =>
            pair.First.Summary.Id == pair.Second.Summary.Id
            && Math.Abs(pair.First.Score - pair.Second.Score) < 1e-12
            && pair.First.Seed == pair.Second.Seed
            && pair.First.Evidence.SequenceEqual(pair.Second.Evidence));

    private static int Take(int takeSeed) => (Math.Abs(takeSeed % 20)) + 1;

    private static Movie[] BuildLibrary(int[] ids)
    {
        var distinct = ids
            .Select(value => (Math.Abs(value % 900)) + 2)
            .Distinct()
            .Take(12)
            .ToArray();

        var genres = new[] { "Drama", "Comedy", "Mystery" };
        var library = new Movie[distinct.Length];

        for (var index = 0; index < distinct.Length; index++)
        {
            library[index] = NewMovie(
                distinct[index],
                genres: [genres[index % genres.Length]],
                isWatched: index % 3 == 0);
        }

        return library;
    }

    private static Movie NewMovie(int id, string[] genres, bool isWatched)
    {
        var title = $"Movie {id}";

        return Movie.Rehydrate(
            new MovieId(id),
            title,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            false,
            new MovieMetadata(title, genres: genres),
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null,
            isWatched: isWatched);
    }
}

public sealed class RecommendedMovieEqualityTests
{
    private static RecommendationEvidence Evidence(
        string criterion = "Genre",
        string text = "Same genres: Drama",
        params string[] matchedNames) =>
        new(criterion, text, [.. matchedNames]);

    [Fact]
    public void Equals_SameValues_ReturnsTrueWithSameHashCode()
    {
        // arrange
        var first = Evidence(matchedNames: ["Drama"]);
        var second = Evidence(matchedNames: ["Drama"]);

        // act / assert
        first.Equals(second).ShouldBeTrue();
        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // arrange
        var evidence = Evidence(matchedNames: ["Drama"]);

        // act / assert
        evidence.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentCriterion_ReturnsFalse()
    {
        // arrange
        var first = Evidence(criterion: "Genre", matchedNames: ["Drama"]);
        var second = Evidence(criterion: "Director", matchedNames: ["Drama"]);

        // act / assert
        first.Equals(second).ShouldBeFalse();
        first.ShouldNotBe(second);
    }

    [Fact]
    public void Equals_DifferentText_ReturnsFalse()
    {
        // arrange
        var first = Evidence(text: "Same genres: Drama", matchedNames: ["Drama"]);
        var second = Evidence(text: "Same genres: Comedy", matchedNames: ["Drama"]);

        // act / assert
        first.Equals(second).ShouldBeFalse();
        first.ShouldNotBe(second);
    }

    [Fact]
    public void Equals_DifferentMatchedNames_ReturnsFalse()
    {
        // arrange
        var first = Evidence(matchedNames: ["Drama"]);
        var second = Evidence(matchedNames: ["Comedy"]);

        // act / assert
        first.Equals(second).ShouldBeFalse();
        first.ShouldNotBe(second);
    }

    [Fact]
    public void Equals_CriterionCaseDiffers_ReturnsFalse()
    {
        // arrange
        var first = Evidence(criterion: "Genre", matchedNames: ["Drama"]);
        var second = Evidence(criterion: "genre", matchedNames: ["Drama"]);

        // act / assert
        first.Equals(second).ShouldBeFalse();
        first.ShouldNotBe(second);
    }

    [Fact]
    public void Equals_MatchedNameCaseDiffers_ReturnsFalse()
    {
        // arrange
        var first = Evidence(matchedNames: ["Drama"]);
        var second = Evidence(matchedNames: ["drama"]);

        // act / assert
        first.Equals(second).ShouldBeFalse();
        first.ShouldNotBe(second);
    }
}
