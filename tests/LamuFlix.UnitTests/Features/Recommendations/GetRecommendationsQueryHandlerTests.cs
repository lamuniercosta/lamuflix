using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GetRecommendationsQueryHandlerTests
{
    private const int DefaultTake = 10;
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static SimilarityWeights Weights => new();
    private static KeywordCounts EmptyKeywordCounts => new(0, new Dictionary<string, long>());
    private static SimilarityWeights GenreOnlyWeights => new()
    {
        Director = 0.0,
        Themes = 0.0,
        Plot = 0.0,
        Cast = 0.0,
        Collection = 0.0,
        Genres = 3.0,
    };

    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
        });

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IRecommendationCandidateSource candidateSource = Substitute.For<IRecommendationCandidateSource>();
    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IRecommendationProfileSource profileSource = Substitute.For<IRecommendationProfileSource>();

    public GetRecommendationsQueryHandlerTests()
    {
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Counts);
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>()).Returns(EmptyKeywordCounts);
        ArrangeProfile();
    }

    [Fact]
    public async Task HandleAsync_MissingSelectedMovie_ThrowsNotFoundException()
    {
        // arrange
        var handler = NewHandler();
        var query = new GetRecommendationsQuery(new MovieId(1));
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((Movie?)null);

        // act / assert
        await Should.ThrowAsync<NotFoundException>(() => handler.HandleAsync(query, CancellationToken.None));
        await candidateSource.DidNotReceive().GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SelectedMovieNotEnriched_UsableFallbackVectorScores()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, withMetadata: false, status: EnrichmentStatus.Pending);
        var candidate = NewMovie(2, genres: ["Drama"]);
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(selected);
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>()).Returns(Snapshot([Entry(candidate)]));
        ArrangeProfileWith(
            [],
            System.Collections.Immutable.ImmutableDictionary<MovieId, System.Collections.Immutable.ImmutableArray<float>>
                .Empty.Add(selected.Id, PlotVector(primary: 1.0f)));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.Count.ShouldBe(1);
        result[0].Summary.Id.ShouldBe(new MovieId(2));
    }

    [Fact]
    public async Task HandleAsync_EmptyPool_ReturnsEmptyImmutableList()
    {
        // arrange
        var handler = NewHandler();
        Arrange(Selected(), []);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id), CancellationToken.None);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_EmptyPool_DoesNotLoadGenreWeights()
    {
        // arrange
        var handler = NewHandler();
        Arrange(Selected(), []);

        // act
        await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id), CancellationToken.None);

        // assert
        await catalog.Received(0).GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FewerCandidatesThanTake_ReturnsEveryCandidate()
    {
        // arrange
        var handler = NewHandler();
        var pool = Pool(3);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id), CancellationToken.None);

        // assert
        result.Count.ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_CandidatesEqualToTake_ReturnsTake()
    {
        // arrange
        var handler = NewHandler();
        var pool = Pool(DefaultTake);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id), CancellationToken.None);

        // assert
        result.Count.ShouldBe(DefaultTake);
    }

    [Fact]
    public async Task HandleAsync_MoreCandidatesThanTake_ReturnsTake()
    {
        // arrange
        var handler = NewHandler();
        var pool = Pool(25);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id), CancellationToken.None);

        // assert
        result.Count.ShouldBe(DefaultTake);
    }

    [Fact]
    public async Task HandleAsync_OmittedTake_DefaultsToExactlyTen()
    {
        // arrange
        var handler = NewHandler();
        var pool = Pool(12);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id), CancellationToken.None);

        // assert
        result.Count.ShouldBe(DefaultTake);
    }

    [Fact]
    public async Task HandleAsync_OptionsTakeDiffersFromTen_OmittedQueryTakeStillReturnsTen()
    {
        // arrange
        var handler = NewHandler(optionsTake: 3);
        var pool = Pool(12);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id), CancellationToken.None);

        // assert
        result.Count.ShouldBe(DefaultTake);
    }

    [Theory]
    [InlineData(3, 5, 7, 5)]
    [InlineData(9, 2, 7, 2)]
    public async Task HandleAsync_ExplicitTakeDiffersFromOptionsTake_ReturnsMinOfTakeAndCount(
        int optionsTake,
        int queryTake,
        int candidateCount,
        int expected)
    {
        // arrange
        var handler = NewHandler(optionsTake);
        var pool = Pool(candidateCount);
        Arrange(Selected(), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(Selected().Id, queryTake), CancellationToken.None);

        // assert
        result.Count.ShouldBe(expected);
    }

    [Fact]
    public async Task HandleAsync_EqualScores_OrdersByAscendingMovieId()
    {
        // arrange
        var handler = NewHandler();
        var pool = Candidates(NewMovie(9, genres: ["Drama"]), NewMovie(4, genres: ["Drama"]), NewMovie(11, genres: ["Drama"]), NewMovie(7, genres: ["Drama"]));
        Arrange(NewMovie(1, genres: ["Drama"]), pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ToArray().ShouldBe(new[] { 4, 7, 9, 11 });
    }

    [Fact]
    public async Task HandleAsync_DifferentScores_OrdersByDescendingScore()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var identical = NewMovie(2, genres: ["Drama", "Action"], directors: ["Director A"], actors: ["Actor A"], writers: ["Writer A"], releaseYear: 2000, runtimeMinutes: 120, imdbRating: 8.0m);
        var partial = NewMovie(4, genres: ["Drama"]);
        var zero = NewMovie(3, withMetadata: false);
        Arrange(selected, Candidates(zero, partial, identical));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        var expectedTop = SimilarityScorer.Score(
            MovieFeaturesMapper.From(selected),
            MovieFeaturesMapper.From(identical),
            new RecommendationOptions { Weights = Weights },
            GenreWeights.Calculate(Counts),
            EmptyKeywordCounts);

        result.Select(item => item.Summary.Id.Value).ToArray().ShouldBe(new[] { 2, 4, 3 });
        result[0].Score.ShouldBe(expectedTop.Score);
        result[2].Score.ShouldBe(0.0);
    }

    [Fact]
    public async Task HandleAsync_PoolLargerThanTake_DiversifyingItemBeyondRelevanceCutoff_IsSelectedBeforeTake()
    {
        // arrange
        var handler = NewHandler(weights: GenreOnlyWeights);
        var selected = NewMovie(1, genres: ["Drama", "Action", "Comedy", "Horror"]);
        Arrange(selected, DiversificationPool());

        // act
        var diversified = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 3), CancellationToken.None);
        var relevanceOnly = await NewHandler(weights: GenreOnlyWeights, mmrLambda: 1.0)
            .HandleAsync(new GetRecommendationsQuery(selected.Id, 3), CancellationToken.None);

        // assert
        relevanceOnly.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 3, 4 });
        diversified.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 6, 7 });
        diversified.ShouldContain(item => item.Summary.Id == new MovieId(6));
    }

    [Fact]
    public async Task HandleAsync_ConfiguredMmrLambda_ChangesSelectionOrder()
    {
        // arrange
        var selected = NewMovie(1, genres: ["Drama", "Action", "Comedy", "Horror"]);
        Arrange(selected, DiversificationPool());

        // act
        var diversified = await NewHandler(weights: GenreOnlyWeights, mmrLambda: 0.7)
            .HandleAsync(new GetRecommendationsQuery(selected.Id, 5), CancellationToken.None);
        var relevanceOnly = await NewHandler(weights: GenreOnlyWeights, mmrLambda: 1.0)
            .HandleAsync(new GetRecommendationsQuery(selected.Id, 5), CancellationToken.None);

        // assert
        relevanceOnly.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 3, 4, 5, 6 });
        diversified.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 6, 7, 3, 4 });
        diversified.Select(item => item.Summary.Id).ShouldNotBe(relevanceOnly.Select(item => item.Summary.Id));
    }

    [Fact]
    public async Task HandleAsync_SelectorOrder_IsRetainedWhenScoresAreNotMonotonic()
    {
        // arrange
        var handler = NewHandler(weights: GenreOnlyWeights);
        var selected = NewMovie(1, genres: ["Drama", "Action", "Comedy", "Horror"]);
        Arrange(selected, DiversificationPool());

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 5), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 6, 7, 3, 4 });
        result[0].Score.ShouldBeGreaterThan(result[1].Score);
        result[1].Score.ShouldBe(0.0, 1e-12);
        result[2].Score.ShouldBe(0.0, 1e-12);
        result[3].Score.ShouldBeGreaterThan(result[2].Score);
        result[3].Score.ShouldBe(result[0].Score, 1e-12);
    }

    [Fact]
    public async Task HandleAsync_DiversifiedResult_PreservesOriginalMovieScoreAndEvidence()
    {
        // arrange
        var selected = NewMovie(1, genres: ["Drama", "Action", "Comedy", "Horror"]);
        Arrange(selected, DiversificationPool());
        var relevanceOnly = await NewHandler(weights: GenreOnlyWeights, mmrLambda: 1.0)
            .HandleAsync(new GetRecommendationsQuery(selected.Id, 6), CancellationToken.None);
        var original = relevanceOnly.ToDictionary(item => item.Summary.Id);

        // act
        var diversified = await NewHandler(weights: GenreOnlyWeights, mmrLambda: 0.7)
            .HandleAsync(new GetRecommendationsQuery(selected.Id, 6), CancellationToken.None);

        // assert
        diversified.Count.ShouldBe(6);
        foreach (var item in diversified)
        {
            var expected = original[item.Summary.Id];
            item.Summary.ShouldBe(expected.Summary);
            item.Score.ShouldBe(expected.Score, 1e-12);
            item.Evidence.ShouldBe(expected.Evidence);
        }
    }

    [Fact]
    public async Task HandleAsync_ZeroScoreCandidate_IsRetainedWithinTake()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var pool = Candidates(NewMovie(2, genres: ["Drama", "Action"], directors: ["Director A"], actors: ["Actor A"], writers: ["Writer A"], releaseYear: 2000, runtimeMinutes: 120, imdbRating: 8.0m), NewMovie(3, withMetadata: false));
        Arrange(selected, pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 5), CancellationToken.None);

        // assert
        result.Count.ShouldBe(2);
        result[1].Summary.Id.ShouldBe(new MovieId(3));
        result[1].Score.ShouldBe(0.0);
    }

    [Fact]
    public async Task HandleAsync_Candidate_SummaryCarriesAllTenFields()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = NewMovie(
            7,
            title: "Seven",
            posterUrl: "https://posters/seven.jpg",
            releaseYear: 1999,
            runtimeMinutes: 101,
            imdbRating: 7.5m,
            genres: ["Drama"],
            isInWatchlist: true,
            isWatched: true,
            isNotInterested: false);
        Arrange(selected, Candidates(candidate));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

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
            true,
            Review.None);
        result[0].Summary.ShouldBe(expected);
    }

    [Fact]
    public async Task HandleAsync_CandidateWithoutMetadata_SummaryKeepsNullMetadataScalars()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = NewMovie(8, title: "Eight", withMetadata: false);
        Arrange(selected, Candidates(candidate));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        var expected = new MovieSummary(new MovieId(8), "Eight", null, null, null, null, EnrichmentStatus.Enriched, false, false, Review.None);
        result[0].Summary.ShouldBe(expected);
    }

    [Fact]
    public async Task HandleAsync_ScoredCandidate_PreservesFeatureReasonPrefixWithoutStorySuffix()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = NewMovie(2, genres: ["Drama", "Action"], directors: ["Director A"], actors: ["Actor A"], writers: ["Writer A"], releaseYear: 2000, runtimeMinutes: 120, imdbRating: 8.0m);
        Arrange(selected, [Entry(candidate)]);
        var expected = SimilarityScorer.Score(
            MovieFeaturesMapper.From(selected),
            MovieFeaturesMapper.From(candidate),
            new RecommendationOptions { Weights = Weights },
            GenreWeights.Calculate(Counts),
            EmptyKeywordCounts);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        expected.Reasons.ShouldNotBeEmpty();
        result[0].Score.ShouldBe(expected.Score);
        result[0].Evidence.ShouldNotBeEmpty();
        result[0].Evidence.ShouldBe(SimilarityScorer.BuildEvidence(expected, new RecommendationOptions { Weights = Weights }));

        HasStoryReason(result[0]).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_CandidateWithoutEmbedding_DoesNotEmitStoryReason()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = IdenticalFeatureMovie(2);
        Arrange(selected, [Entry(candidate)]);
        var expected = SimilarityScorer.Score(
            MovieFeaturesMapper.From(selected),
            MovieFeaturesMapper.From(candidate),
            new RecommendationOptions { Weights = Weights },
            GenreWeights.Calculate(Counts),
            EmptyKeywordCounts);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result[0].Score.ShouldBe(expected.Score);
        AssertFeaturePrefix(result[0], expected);
        HasStoryReason(result[0]).ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_LowCosineCandidates_AreRetainedAndScored()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var low = IdenticalFeatureMovie(2);
        var zero = IdenticalFeatureMovie(3);
        var negative = IdenticalFeatureMovie(4);
        Arrange(selected, [
            Entry(low),
            Entry(zero),
            Entry(negative),
        ]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.Count.ShouldBe(3);
        result.ShouldAllBe(item => !HasStoryReason(item));
        var genreWeights = GenreWeights.Calculate(Counts);
        foreach (var item in result)
        {
            var movie = item.Summary.Id.Value switch
            {
                2 => low,
                3 => zero,
                _ => negative,
            };
            var expected = SimilarityScorer.Score(
                MovieFeaturesMapper.From(selected),
                MovieFeaturesMapper.From(movie),
                new RecommendationOptions { Weights = Weights },
                genreWeights,
                EmptyKeywordCounts);
            item.Score.ShouldBe(expected.Score);
        }
    }

    [Fact]
    public async Task HandleAsync_EqualScores_TieBreakByIdAcrossTake()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var highCosineLowId = IdenticalFeatureMovie(4);
        var lowCosineHighId = IdenticalFeatureMovie(9);
        Arrange(selected, [
            Entry(lowCosineHighId),
            Entry(highCosineLowId),
        ]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 2), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ToArray().ShouldBe([4, 9]);
        result[0].Score.ShouldBe(result[1].Score);
    }

    [Fact]
    public async Task HandleAsync_MixedPool_ExcludesNotInterestedCandidatesBeforeScoring()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, genres: ["Drama"]);
        var excluded = NewMovie(2, genres: ["Drama"], isNotInterested: true);
        var eligible = NewMovie(3, genres: ["Drama"]);
        Arrange(selected, Candidates(excluded, eligible));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ToArray().ShouldBe(new[] { 3 });
        await profileSource.Received(1).GetProfileInputsAsync(
            Arg.Is<IReadOnlyCollection<MovieId>>(ids => ids.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_HighSimilarityNotInterestedCandidate_IsNeverReturned()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var excludedMovie = NewMovie(2, genres: ["Drama", "Action"], directors: ["Director A"], actors: ["Actor A"], writers: ["Writer A"], releaseYear: 2000, runtimeMinutes: 120, imdbRating: 8.0m, isNotInterested: true);
        Arrange(selected, Candidates(excludedMovie, NewMovie(3, withMetadata: false)));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ToArray().ShouldBe(new[] { 3 });
    }

    [Fact]
    public async Task HandleAsync_AllCandidatesNotInterested_ReturnsEmptyWithoutProfileAcquisition()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        Arrange(selected, Candidates(
            NewMovie(2, genres: ["Drama"], isNotInterested: true),
            NewMovie(3, genres: ["Drama"], isNotInterested: true)));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldBeEmpty();
        await catalog.DidNotReceive().GetGenreCountsAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, 0.0)]
    [InlineData(true, 0.25)]
    public async Task HandleAsync_WatchedCandidate_AppliesAlreadyWatchedPenalty(bool isWatched, double expectedPenalty)
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, genres: ["Drama"]);
        var candidate = NewMovie(5, genres: ["Drama"], isWatched: isWatched);
        Arrange(selected, Candidates(candidate));
        var selectedSimilarity = SimilarityScorer.Score(
            MovieFeaturesMapper.From(selected),
            MovieFeaturesMapper.From(candidate),
            new RecommendationOptions { Weights = Weights },
            GenreWeights.Calculate(Counts),
            EmptyKeywordCounts);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result[0].Score.ShouldBe(selectedSimilarity.Score - expectedPenalty, 1e-12);
    }

    [Fact]
    public async Task HandleAsync_ProfileInputs_AreAcquiredOncePerRequest()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        Arrange(selected, Pool(3));
        ArrangeProfile(ProfileMovie(90, "Drama"));

        // act
        await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        await profileSource.Received(1).GetProfileInputsAsync(
            Arg.Is<IReadOnlyCollection<MovieId>>(ids => ids.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnrelatedLikedAndDislikedReviews_LeaveExistingScoresUnchanged()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, genres: ["Drama"]);
        var first = NewMovie(2, genres: ["Drama"]);
        var second = NewMovie(3, genres: ["Drama", "Comedy"]);
        Arrange(selected, Candidates(first, second));
        var baseline = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // act
        ArrangeProfileWith(
            [
                new TasteProfileMovie(new MovieId(90), ["Drama"], [], [Now], IsWatched: true) { Review = Review.Liked },
                new TasteProfileMovie(new MovieId(91), ["Drama"], [], [Now], IsWatched: true) { Review = Review.Disliked },
            ],
            System.Collections.Immutable.ImmutableDictionary<MovieId, System.Collections.Immutable.ImmutableArray<float>>.Empty);
        var reviewed = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        reviewed.Select(item => item.Summary.Id).ShouldBe(baseline.Select(item => item.Summary.Id));
        reviewed.Select(item => item.Score).ShouldBe(baseline.Select(item => item.Score));
    }

    [Fact]
    public async Task HandleAsync_ReviewedMovieThatWasEligible_IsExcludedFromRelatedResults()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var reviewed = NewMovie(2, genres: ["Drama", "Action"], directors: ["Director A"], actors: ["Actor A"], writers: ["Writer A"], releaseYear: 2000, runtimeMinutes: 120, imdbRating: 8.0m, isNotInterested: true);
        var eligible = NewMovie(3, genres: ["Drama", "Action"], directors: ["Director A"], actors: ["Actor A"], writers: ["Writer A"], releaseYear: 2000, runtimeMinutes: 120, imdbRating: 8.0m);
        Arrange(selected, Candidates(reviewed, eligible));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldBe([3]);
    }

    [Fact]
    public async Task HandleAsync_RelatedOperation_DoesNotAcquireTasteProfile()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        Arrange(selected, Pool(3));
        ArrangeProfile(ProfileMovie(90, "Drama"));
        using var cts = new CancellationTokenSource();

        // act
        await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), cts.Token);

        // assert
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
        await profileSource.Received(1).GetProfileInputsAsync(
            Arg.Is<IReadOnlyCollection<MovieId>>(ids => ids.Count == 0),
            cts.Token);
        await catalog.Received(1).GetGenreCountsAsync(cts.Token);
    }

    [Fact]
    public async Task HandleAsync_LargePenalties_ProduceRawFiniteScoreOutsideUnitInterval()
    {
        // arrange
        var handler = NewHandler(
            blendSelectedWeight: 2.0,
            alreadyWatchedPenalty: 5.0);
        var selected = NewMovie(1, genres: ["Drama"]);
        var candidate = NewMovie(5, genres: ["Drama"], isWatched: true);
        Arrange(selected, Candidates(candidate));

        var similarity = SimilarityScorer.Score(
            MovieFeaturesMapper.From(selected),
            MovieFeaturesMapper.From(candidate),
            new RecommendationOptions { Weights = Weights },
            GenreWeights.Calculate(Counts),
            EmptyKeywordCounts).Score;

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result[0].Score.ShouldBe((2.0 * similarity) - (5.0 * 1.0), 1e-12);
    }

    [Fact]
    public async Task HandleAsync_NegativeAndPositiveScores_RebaseSelectorRelevanceButPreserveRawScores()
    {
        // arrange
        var handler = NewHandler(
            blendSelectedWeight: 2.0,
            alreadyWatchedPenalty: 5.0);
        var selected = Selected();
        var unwatched = IdenticalFeatureMovie(2);
        var watched = IdenticalFeatureMovie(3, isWatched: true);
        Arrange(selected, [Entry(unwatched), Entry(watched)]);

        var similarity = SimilarityScorer.Score(
            MovieFeaturesMapper.From(selected),
            MovieFeaturesMapper.From(unwatched),
            new RecommendationOptions { Weights = Weights },
            GenreWeights.Calculate(Counts),
            EmptyKeywordCounts).Score;

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 2), CancellationToken.None);

        // assert
        result.Count.ShouldBe(2);
        result.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 3 });
        result[0].Score.ShouldBe(2.0 * similarity, 1e-12);
        result[1].Score.ShouldBe((2.0 * similarity) - (5.0 * 1.0), 1e-12);
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
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), cts.Token));
        await movies.DidNotReceive().GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        await candidateSource.DidNotReceive().GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
        await catalog.DidNotReceive().GetGenreCountsAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SelectedLookupCancelsToken_ThrowsBeforeCandidateLoad()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult<Movie?>(Selected());
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), cts.Token));
        await candidateSource.DidNotReceive().GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CandidateLoadCancelsToken_ThrowsBeforeGenreWeights()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        IReadOnlyList<RecommendationLibraryEntry> pool = Pool(2);
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(Selected());
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(Snapshot(pool));
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), cts.Token));
        await catalog.DidNotReceive().GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_GenreWeightsCancelToken_ThrowsBeforeScoring()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        Arrange(Selected(), Pool(2));
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(Counts);
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), cts.Token));
        await catalog.Received(1).GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CancelledAfterFirstCandidateScored_StopsBeforeScoringRest()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        var selected = Selected();
        var library = new List<RecommendationLibraryEntry> { Entry(selected, PlotVector(primary: 1.0f)) };
        library.AddRange(Pool(3));
        var pool = new CancellingCandidates(library, cts);
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(selected);
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>()).Returns(Snapshot(pool));

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), cts.Token));
        pool.Yielded.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_SelectedLookupFails_PropagatesError()
    {
        // arrange
        var handler = NewHandler();
        var failure = new InvalidOperationException("selected lookup failed");
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Movie?>(failure));

        // act / assert
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), CancellationToken.None));
        thrown.ShouldBe(failure);
    }

    [Fact]
    public async Task HandleAsync_CandidateLoadFails_PropagatesError()
    {
        // arrange
        var handler = NewHandler();
        var failure = new InvalidOperationException("candidate load failed");
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(Selected());
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RecommendationLibrarySnapshot>(failure));

        // act / assert
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), CancellationToken.None));
        thrown.ShouldBe(failure);
    }

    [Fact]
    public async Task HandleAsync_GenreWeightsFail_PropagatesError()
    {
        // arrange
        var handler = NewHandler();
        var failure = new InvalidOperationException("genre counts failed");
        Arrange(Selected(), Pool(2));
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GenreCounts>(failure));

        // act / assert
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), CancellationToken.None));
        thrown.ShouldBe(failure);
    }

    [Fact]
    public async Task HandleAsync_NoStructuralOrPersonalExplanation_EmitsEmptyEvidence()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var unrelated = NewMovie(12, genres: ["Mystery"]);
        Arrange(selected, Candidates(unrelated));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result[0].Evidence.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ProfileOverlap_EmitsNoProfilePersonalReasons()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = NewMovie(14, genres: ["Drama", "Comedy", "Horror"]);
        Arrange(selected, Candidates(candidate));
        ArrangeProfile(ProfileMovie(90, "Drama", "Comedy", "Horror"));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        PersonalTexts(result[0]).ShouldBeEmpty();
        result[0].Evidence.ShouldAllBe(evidence => !evidence.Text.StartsWith("Matches your", StringComparison.Ordinal));
    }


    [Fact]
    public async Task HandleAsync_ZeroPlotSimilarity_EmitsNoViewingHistoryReason()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, genres: ["Drama"]);
        var candidate = NewMovie(19, genres: ["Mystery"]);
        Arrange(selected, [Entry(candidate, PlotVector(primary: 0.0f, secondary: 1.0f))]);
        ArrangeProfileWith(
            [new TasteProfileMovie(new MovieId(90), [], PlotVector(primary: 1.0f), [Now], IsWatched: true)],
            System.Collections.Immutable.ImmutableDictionary<MovieId, System.Collections.Immutable.ImmutableArray<float>>.Empty);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result[0].Evidence.ShouldNotContain(evidence => evidence.Text == "Matches your viewing history");
        result[0].Evidence.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_StoredSeedAndCandidateVectors_ContributePlotScoreAndReason()
    {
        // arrange
        var handler = NewHandler(weights: PlotOnlyWeights);
        var selected = NewMovie(1, genres: ["Drama"]);
        var candidate = NewMovie(2, genres: ["Mystery"]);
        var vector = PlotVector(primary: 1.0f);
        Arrange(selected, [Entry(candidate, vector)]);
        ArrangeProfileWith(
            [ProfileMovie(90, "Drama")],
            System.Collections.Immutable.ImmutableDictionary<MovieId, System.Collections.Immutable.ImmutableArray<float>>.Empty);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldHaveSingleItem();
        result[0].Score.ShouldBe(1.0, 1e-12);
        result[0].Evidence.ShouldContain(evidence => evidence.Text == "Similar story");
        await profileSource.Received(1).GetProfileInputsAsync(
            Arg.Is<IReadOnlyCollection<MovieId>>(ids => ids.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SelectedMovieWithoutUsableVector_ReturnsEmptyRail()
    {
        // arrange
        var handler = NewHandler(weights: PlotOnlyWeights);
        var selected = NewMovie(1, genres: ["Drama"]);
        var candidate = NewMovie(2, genres: ["Mystery"]);
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(selected);
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
            .Returns(Snapshot([Entry(candidate, PlotVector(primary: 1.0f))]));
        ArrangeProfileWith([ProfileMovie(90, "Drama")], System.Collections.Immutable.ImmutableDictionary<MovieId, System.Collections.Immutable.ImmutableArray<float>>.Empty);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_SelectedMovieAbsentFromLoad_UsableFallbackVectorStillScores()
    {
        // arrange
        var handler = NewHandler(weights: PlotOnlyWeights);
        var selected = NewMovie(1, genres: ["Drama"]);
        var candidate = NewMovie(2, genres: ["Mystery"]);
        var vector = PlotVector(primary: 1.0f);
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(selected);
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
            .Returns(Snapshot([Entry(candidate, vector)]));
        ArrangeProfileWith(
            [ProfileMovie(90, "Drama")],
            System.Collections.Immutable.ImmutableDictionary<MovieId, System.Collections.Immutable.ImmutableArray<float>>
                .Empty.Add(selected.Id, vector));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldHaveSingleItem();
        result[0].Score.ShouldBe(1.0, 1e-12);
        result[0].Evidence.ShouldContain(evidence => evidence.Text == "Similar story");
        await profileSource.Received(1).GetProfileInputsAsync(
            Arg.Is<IReadOnlyCollection<MovieId>>(ids => ids.Count == 1 && ids.Single() == selected.Id),
            Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task HandleAsync_DirectorMatchWithLowestPlotSimilarity_AppearsInRailAfterEveryAboveBarMatch()
    {
        // arrange
        var handler = NewHandler(minimumSimilarity: 0.225);
        var selected = NewMovie(1, genres: ["Drama"], directors: ["Director A"], actors: ["Actor A"]);
        var entries = new List<RecommendationLibraryEntry>();
        for (var id = 2; id <= 56; id++)
        {
            entries.Add(Entry(NewMovie(id, genres: ["Mystery"]), PlotVector(primary: 0.0f, secondary: 1.0f)));
        }

        for (var id = 57; id <= 59; id++)
        {
            entries.Add(Entry(
                NewMovie(id, genres: ["Drama"], actors: ["Actor A"]),
                PlotVector(primary: 1.0f)));
        }

        entries.Add(Entry(
            NewMovie(60, genres: ["Mystery"], directors: ["Director A"]),
            PlotVector(primary: 0.0f, secondary: 1.0f)));
        ArrangeLibrary(selected, entries);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.Count.ShouldBe(4);
        result.Select(item => item.Summary.Id.Value).ShouldBe([57, 58, 59, 60]);
    }

    [Fact]
    public async Task HandleAsync_DirectorOnlyCandidateWithoutEmbedding_IsAdmittedAsStrongReason()
    {
        // arrange
        var handler = NewHandler(minimumSimilarity: 0.225);
        var selected = NewMovie(1, genres: ["Drama"], directors: ["Director A"]);
        var candidate = NewMovie(2, genres: ["Mystery"], directors: ["Director A"]);
        ArrangeLibrary(selected, [Entry(candidate)]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldHaveSingleItem();
        result[0].Summary.Id.ShouldBe(new MovieId(2));
        result[0].Score.ShouldBe(3.0 / 15.0, 1e-12);
    }

    [Fact]
    public async Task HandleAsync_SimilarityExactlyAtTheBar_IsAboveBarInclusive()
    {
        // arrange
        var handler = NewHandler(minimumSimilarity: 3.0 / 15.0);
        var selected = NewMovie(1, genres: ["Drama"], directors: ["Director A"]);
        var candidate = NewMovie(2, genres: ["Mystery"], directors: ["Director A"]);
        ArrangeLibrary(selected, [Entry(candidate)]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task HandleAsync_EmptyStrongReasonCriteria_ExcludesBelowBarCandidate()
    {
        // arrange
        var handler = NewHandler(minimumSimilarity: 0.225, strongReasonCriteria: []);
        var selected = NewMovie(1, genres: ["Drama"], directors: ["Director A"]);
        var candidate = NewMovie(2, genres: ["Mystery"], directors: ["Director A"]);
        ArrangeLibrary(selected, [Entry(candidate)]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ChangingTheConfiguredBar_ChangesEligibilityWithoutCodeChange()
    {
        // arrange
        var selected = NewMovie(1, genres: ["Drama"], directors: ["Director A"]);
        var candidate = NewMovie(2, genres: ["Mystery"], directors: ["Director A"]);
        var strict = NewHandler(minimumSimilarity: 0.225, strongReasonCriteria: []);
        var relaxed = NewHandler(minimumSimilarity: 3.0 / 15.0, strongReasonCriteria: []);
        ArrangeLibrary(selected, [Entry(candidate)]);

        // act
        var excluded = await strict.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);
        var included = await relaxed.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        excluded.ShouldBeEmpty();
        included.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task HandleAsync_ConfiguredCaseVariantStrongReason_IsAccepted()
    {
        // arrange
        var handler = NewHandler(minimumSimilarity: 0.225, strongReasonCriteria: ["DIRECTOR"]);
        var selected = NewMovie(1, genres: ["Drama"], directors: ["Director A"]);
        var candidate = NewMovie(2, genres: ["Mystery"], directors: ["Director A"]);
        ArrangeLibrary(selected, [Entry(candidate)]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task HandleAsync_AbsentStrongReasonTerm_NeverQualifies()
    {
        // arrange
        var handler = NewHandler(minimumSimilarity: 0.225, strongReasonCriteria: ["Collection"]);
        var selected = NewMovie(1, genres: ["Drama"], directors: ["Director A"]);
        var candidate = NewMovie(2, genres: ["Mystery"], directors: ["Director A"]);
        ArrangeLibrary(selected, [Entry(candidate)]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ThemesOnlyAtConfiguredCeiling_IsAdmittedAsStrongReason()
    {
        // arrange
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>()).Returns(ThemeKeywordCounts());
        var handler = NewHandler(minimumSimilarity: 0.225);
        var selected = KeywordMovie(1, "Selected", "heist", "noir");
        var candidate = KeywordEntry(2, "Themed", "heist", "noir");
        ArrangeLibrary(selected, [candidate]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldHaveSingleItem();
        result[0].Summary.Id.ShouldBe(new MovieId(2));
    }

    [Fact]
    public async Task HandleAsync_ThemesJustBelowCeilingWithoutAnotherReason_IsExcluded()
    {
        // arrange
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>()).Returns(ThemeKeywordCounts());
        var handler = NewHandler(minimumSimilarity: 0.225);
        var selected = KeywordMovie(1, "Selected", "heist", "noir");
        var candidate = KeywordEntry(2, "Nearly themed", "heist", "noir2");
        ArrangeLibrary(selected, [candidate]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ThemesInsideFormerToleranceWindow_IsExcluded()
    {
        // arrange
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>()).Returns(ThemeKeywordCounts());
        var handler = NewHandler(minimumSimilarity: 0.5, themesCeiling: 0.7071067815401009);
        var selected = KeywordMovie(1, "Selected", "noir", "noir2");
        var candidate = KeywordEntry(2, "Nearly themed", "noir");
        ArrangeLibrary(selected, [candidate]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Collection")]
    [InlineData("Director")]
    [InlineData("Themes")]
    public void SimilarityScorer_EmitsEveryConfiguredStrongReasonNameAtValueOne(string criterion)
    {
        // arrange
        var selected = PairForCriterion(criterion);
        var candidate = PairForCriterion(criterion);
        var keywordCounts = criterion == "Themes"
            ? ThemeKeywordCounts()
            : EmptyKeywordCounts;

        // act
        var result = SimilarityScorer.Score(
            selected,
            candidate,
            new RecommendationOptions(),
            GenreWeights.Calculate(Counts),
            keywordCounts);

        // assert
        var term = result.Breakdown.Single(entry => entry.Name == criterion);
        term.Value.ShouldBe(1.0);
        term.Weight.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public async Task HandleAsync_TierBComesAfterEveryTierAEvenWhenBlendWouldPromoteIt()
    {
        // arrange
        var handler = NewHandler(minimumSimilarity: 0.225, alreadyWatchedPenalty: 5.0);
        var selected = NewMovie(1, genres: ["Drama"], directors: ["Director A"], actors: ["Actor A"]);
        var aboveBar = Entry(
            NewMovie(2, genres: ["Drama"], actors: ["Actor A"], isWatched: true),
            PlotVector(primary: 1.0f));
        var strongReason = Entry(NewMovie(3, genres: ["Mystery"], directors: ["Director A"]));
        ArrangeLibrary(selected, [strongReason, aboveBar]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 2), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldBe([2, 3]);
        result[0].Score.ShouldBeLessThan(result[1].Score);
    }

    [Fact]
    public async Task HandleAsync_StrongReasonBudget_UsesOnlyTakeMinusSelectedAboveBarSlots()
    {
        // arrange
        var handler = NewHandler(minimumSimilarity: 0.225);
        var selected = NewMovie(1, genres: ["Drama"], directors: ["Director A"], actors: ["Actor A"]);
        var aboveBar = Entry(
            NewMovie(2, genres: ["Drama"], actors: ["Actor A"]),
            PlotVector(primary: 1.0f));
        var strongReason = Entry(NewMovie(3, genres: ["Mystery"], directors: ["Director A"]));
        ArrangeLibrary(selected, [strongReason, aboveBar]);

        // act
        var oneSlot = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 1), CancellationToken.None);
        var twoSlots = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 2), CancellationToken.None);
        var padded = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 5), CancellationToken.None);

        // assert
        oneSlot.Select(item => item.Summary.Id.Value).ShouldBe([2]);
        twoSlots.Select(item => item.Summary.Id.Value).ShouldBe([2, 3]);
        padded.Select(item => item.Summary.Id.Value).ShouldBe([2, 3]);
    }

    [Fact]
    public async Task HandleAsync_CancellationDuringScoring_StopsThePerMovieLoop()
    {
        // arrange
        using var cts = new CancellationTokenSource();
        var handler = NewHandler(minimumSimilarity: 0.0);
        var selected = NewMovie(1, genres: ["Drama"], directors: ["Director A"]);
        var entries = new List<RecommendationLibraryEntry> { Entry(selected, PlotVector(primary: 1.0f)) };
        for (var id = 2; id <= 12; id++)
        {
            entries.Add(Entry(NewMovie(id, genres: ["Drama"])));
        }

        var library = new CancellingOnSecondPassLibrary(entries, cts);
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(selected);
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>()).Returns(Snapshot(library));

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(selected.Id), cts.Token));
        library.ScoredYielded.ShouldBeLessThan(entries.Count - 1);
    }

    [Fact]
    public void RecommendationOptions_MinimumSimilarityAndStrongReasonDefaults_MatchTheTicket()
    {
        // arrange
        var options = new RecommendationOptions();

        // act / assert
        options.MinimumSimilarity.ShouldBe(0.225);
        options.StrongReasonCriteria.ShouldBe(["Collection", "Director", "Themes"]);
    }

    [Fact]
    public void RecommendationOptions_EmptyStrongReasonCriteria_IsValid()
    {
        // arrange
        var options = new RecommendationOptions { StrongReasonCriteria = [] };

        // act
        var failures = ValidateRecommendationOptions(options);

        // assert
        failures.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Collection")]
    [InlineData("director")]
    [InlineData("THEMES")]
    public void RecommendationOptions_CanonicalStrongReasonTokens_AreValid(String criterion)
    {
        // arrange
        var options = new RecommendationOptions { StrongReasonCriteria = [criterion] };

        // act
        var failures = ValidateRecommendationOptions(options);

        // assert
        failures.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(" Director")]
    [InlineData("Director ")]
    [InlineData("Plot")]
    [InlineData("Cast")]
    [InlineData("Genre")]
    [InlineData("Keyword")]
    [InlineData("")]
    [InlineData("Drama")]
    public void RecommendationOptions_UnsupportedStrongReasonTokens_AreRejected(String criterion)
    {
        // arrange
        var options = new RecommendationOptions { StrongReasonCriteria = [criterion] };

        // act
        var failures = ValidateRecommendationOptions(options);

        // assert
        failures.ShouldNotBeEmpty();
    }

    [Fact]
    public void RecommendationOptions_NullStrongReasonCollection_IsRejected()
    {
        // arrange
        var options = new RecommendationOptions { StrongReasonCriteria = null };

        // act
        var failures = ValidateRecommendationOptions(options);

        // assert
        failures.ShouldNotBeEmpty();
    }

    [Fact]
    public void RecommendationOptions_NullStrongReasonEntry_IsRejected()
    {
        // arrange
        var withNullEntry = new string[1];
        var options = new RecommendationOptions { StrongReasonCriteria = withNullEntry };

        // act
        var failures = ValidateRecommendationOptions(options);

        // assert
        failures.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RecommendationOptions_MinimumSimilarityOutsideFiniteUnitInterval_IsRejected(double bar)
    {
        // arrange
        var options = new RecommendationOptions { MinimumSimilarity = bar };

        // act
        var failures = ValidateRecommendationOptions(options);

        // assert
        failures.ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.225)]
    [InlineData(1.0)]
    public void RecommendationOptions_MinimumSimilarityInsideFiniteUnitInterval_IsValid(double bar)
    {
        // arrange
        var options = new RecommendationOptions { MinimumSimilarity = bar };

        // act
        var failures = ValidateRecommendationOptions(options);

        // assert
        failures.ShouldBeEmpty();
    }

    [Fact]
    public void RecommendationOptions_InvalidBinding_FailsThroughTheRealOptionsPipeline()
    {
        // arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Recommendations:MinimumSimilarity"] = "1.5",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLamuFlixOptions();
        using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RecommendationOptions>>();

        // act / assert
        Should.Throw<Microsoft.Extensions.Options.OptionsValidationException>(() => _ = accessor.Value);
    }

    private static KeywordCounts ThemeKeywordCounts() =>
        new(100, new Dictionary<string, long>
        {
            ["heist"] = 6,
            ["noir"] = 2,
            ["noir2"] = 2,
        });

    private static string[] ValidateRecommendationOptions(RecommendationOptions options)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            options,
            new System.ComponentModel.DataAnnotations.ValidationContext(options),
            results,
            validateAllProperties: true);
        return [.. results.Select(result => result.ErrorMessage ?? string.Empty)];
    }

    private static MovieFeatures PairForCriterion(string criterion) =>
        criterion switch
        {
            "Collection" => new MovieFeatures(collectionId: 900, collectionName: "Franchise"),
            "Director" => new MovieFeatures(directors: ["Director A"]),
            _ => new MovieFeatures(
                keywords: [901, 902],
                keywordNames: ["heist", "noir"]),
        };

    private static Movie KeywordMovie(int id, string title, params string[] keywords) =>
        Movie.Rehydrate(
            new MovieId(id),
            title,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            false,
            new MovieMetadata(
                title,
                signals: MovieSignalSnapshot.FromPersisted(
                    keywords.Select((name, index) => new MovieKeyword(900 + index, name)),
                    [],
                    collectionAvailable: false)),
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null);

    private static RecommendationLibraryEntry KeywordEntry(int id, string title, params string[] keywords) =>
        new(
            new MovieFeatures(
                keywords: [.. keywords.Select((_, index) => 900 + index)],
                keywordNames: keywords),
            new MovieSummary(
                new MovieId(id),
                title,
                null,
                null,
                null,
                null,
                EnrichmentStatus.Enriched,
                false,
                false,
                Review.None));

    private void ArrangeLibrary(Movie selected, IReadOnlyList<RecommendationLibraryEntry> entries)
    {
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(selected);
        var library = new List<RecommendationLibraryEntry>(entries.Count + 1) { Entry(selected, PlotVector(primary: 1.0f)) };
        library.AddRange(entries);
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>()).Returns(Snapshot(library));
    }

    private static string[] PersonalTexts(RecommendedMovie movie) =>
        [.. movie.Evidence.Select(evidence => evidence.Text).Where(text => text.StartsWith("Matches your", StringComparison.Ordinal))];

    private static System.Collections.Immutable.ImmutableArray<float> PlotVector(float primary, float secondary = 0.0f)
    {
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);
        for (var index = 0; index < EmbeddingDimensions.Value; index++)
        {
            builder.Add(index switch
            {
                0 => primary,
                1 => secondary,
                _ => 0.0f,
            });
        }

        return builder.MoveToImmutable();
    }

    private void ArrangeProfileWith(
        System.Collections.Immutable.ImmutableArray<TasteProfileMovie> profileMovies,
        System.Collections.Immutable.ImmutableDictionary<MovieId, System.Collections.Immutable.ImmutableArray<float>> embeddings) =>
        profileSource
            .GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new RecommendationProfileInputs(profileMovies, embeddings)));

    private static SimilarityWeights PlotOnlyWeights => new()
    {
        Director = 0.0,
        Themes = 0.0,
        Plot = 1.0,
        Cast = 0.0,
        Genres = 0.0,
        Collection = 0.0,
    };

    private static readonly TmdbOptions ConfiguredTmdb = new() { Enabled = true, ApiKey = "test-key" };

    [Fact]
    public async Task HandleAsync_CrowdActiveAndLinked_EmitsRelatedCrowdCopyAndActiveDenominator()
    {
        // arrange
        var handler = NewHandler(
            weights: new SimilarityWeights(),
            crowdEnabled: true,
            tmdbOptions: ConfiguredTmdb,
            mmrLambda: 1.0);
        var selected = NewMovie(1, genres: ["Drama"]);
        var pool = Candidates(NewMovie(2, title: "Candidate", genres: ["Comedy"]));
        Arrange(selected, pool, CrowdLinks.Create([(selected.Id, new MovieId(2))]));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        var item = result.ShouldHaveSingleItem();
        item.Score.ShouldBe(2.0 / 17.0, 1e-12);
        item.Evidence.ShouldBe(
        [
            new RecommendationEvidence("Crowd", "Often liked together", []),
        ]);
    }

    [Fact]
    public async Task HandleAsync_CrowdActiveWithThreshold_AppliesTheCrowdActiveBarOnlyWhileActive()
    {
        // arrange
        var selected = NewMovie(1, genres: ["Drama"]);
        var pool = Candidates(NewMovie(2, title: "Candidate", genres: ["Comedy"]));
        var links = CrowdLinks.Create([(selected.Id, new MovieId(2))]);
        Arrange(selected, pool, links);
        var active = NewHandler(
            weights: new SimilarityWeights(),
            crowdEnabled: true,
            crowdActiveStrongMatchThreshold: 0.5,
            tmdbOptions: ConfiguredTmdb,
            mmrLambda: 1.0);
        var inactive = NewHandler(
            weights: new SimilarityWeights(),
            crowdEnabled: false,
            crowdActiveStrongMatchThreshold: 0.5,
            tmdbOptions: ConfiguredTmdb,
            mmrLambda: 1.0);

        // act
        var activeResult = await active.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);
        var inactiveResult = await inactive.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        activeResult.ShouldBeEmpty();
        inactiveResult.ShouldHaveSingleItem();
    }

    private GetRecommendationsQueryHandler NewHandler(
        int optionsTake = DefaultTake,
        SimilarityWeights? weights = null,
        double blendSelectedWeight = 1.0,
        double alreadyWatchedPenalty = 0.25,
        double mmrLambda = 0.7,
        KeywordWeightsCache? keywordCache = null,
        double minimumSimilarity = 0.0,
        IReadOnlyList<string>? strongReasonCriteria = null,
        double? themesCeiling = null,
        bool crowdEnabled = false,
        double? crowdActiveStrongMatchThreshold = null,
        TmdbOptions? tmdbOptions = null)
    {
        var options = new RecommendationOptions
        {
            Take = optionsTake,
            MinimumSimilarity = minimumSimilarity,
            Weights = weights ?? Weights,
            BlendSelectedWeight = blendSelectedWeight,
            AlreadyWatchedPenalty = alreadyWatchedPenalty,
            MmrLambda = mmrLambda,
            CrowdEnabled = crowdEnabled,
            CrowdActiveStrongMatchThreshold = crowdActiveStrongMatchThreshold,
        };

        if (strongReasonCriteria is not null)
        {
            options = options with { StrongReasonCriteria = strongReasonCriteria };
        }

        if (themesCeiling is not null)
        {
            options = options with { ThemesCeiling = themesCeiling.Value };
        }

        return new GetRecommendationsQueryHandler(
            movies,
            candidateSource,
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            new KeywordWeightsCalculator(catalog, keywordCache ?? new KeywordWeightsCache()),
            Microsoft.Extensions.Options.Options.Create(options),
            Microsoft.Extensions.Options.Options.Create(tmdbOptions ?? new TmdbOptions()));
    }

    [Fact]
    public async Task HandleAsync_RelatedOperation_ResolvesOneKeywordSnapshotForEveryPair()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, genres: ["Drama"], collectionId: 7, collectionName: "Franchise");
        var pool = Candidates(
            NewMovie(2, genres: ["Drama"], collectionId: 7, collectionName: "Franchise"),
            NewMovie(3, genres: ["Drama"]),
            NewMovie(4, genres: ["Comedy"]));
        Arrange(selected, pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 3), CancellationToken.None);

        // assert
        result.Count.ShouldBe(3);
        await catalog.Received(1).GetKeywordCountsAsync(Arg.Any<CancellationToken>());
        await catalog.Received(1).GetGenreCountsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_InvalidatedKeywordSnapshot_IsResolvedAgainByTheNextOperation()
    {
        // arrange
        var cache = new KeywordWeightsCache();
        var handler = NewHandler(keywordCache: cache);
        var selected = NewMovie(1, genres: ["Drama"], collectionId: 7, collectionName: "Franchise");
        var pool = Candidates(
            NewMovie(2, genres: ["Drama"], collectionId: 7, collectionName: "Franchise"),
            NewMovie(3, genres: ["Drama"]),
            NewMovie(4, genres: ["Comedy"]));
        Arrange(selected, pool);

        // act
        var first = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 3), CancellationToken.None);
        cache.Invalidate();
        var second = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 3), CancellationToken.None);

        // assert
        await catalog.Received(2).GetKeywordCountsAsync(Arg.Any<CancellationToken>());
        second.Select(item => item.Score).ShouldBe(first.Select(item => item.Score));
        second.Select(item => item.Summary.Id).ShouldBe(first.Select(item => item.Summary.Id));
    }

    [Fact]
    public async Task HandleAsync_SelectedCollection_LoadsTheWholeLibraryOnce()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, genres: ["Drama"], collectionId: 7, collectionName: "Franchise");
        Arrange(selected, Pool(2));

        // act
        await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        await candidateSource.Received(1).GetLibraryFeaturesAsync(Arg.Any<CancellationToken>());
        await candidateSource.DidNotReceive().GetCandidatesAsync(
            Arg.Any<MovieId>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SeedCollection_LeadsWithContiguousFranchiseBlock()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, genres: ["Drama"], collectionId: 7, collectionName: "Franchise");
        var pool = Candidates(
            NewMovie(2, genres: ["Drama"]),
            NewMovie(3, genres: ["Drama"], collectionId: 7, collectionName: "Franchise"),
            NewMovie(4, genres: ["Comedy"]),
            NewMovie(5, genres: ["Drama"], collectionId: 8, collectionName: "Other Franchise"));
        Arrange(selected, pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 4), CancellationToken.None);

        // assert
        result.Count.ShouldBe(4);
        result[0].Summary.Id.ShouldBe(new MovieId(3));
        result.Select(item => item.Summary.Id).Distinct().Count().ShouldBe(4);
    }

    [Fact]
    public async Task HandleAsync_NonSeedCollection_RetainsBothMoviesWhenSlotsAllow()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, genres: ["Drama"]);
        var pool = Candidates(
            NewMovie(2, genres: ["Drama"], collectionId: 8, collectionName: "Other Franchise"),
            NewMovie(3, genres: ["Drama"], collectionId: 8, collectionName: "Other Franchise"));
        Arrange(selected, pool);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 2), CancellationToken.None);

        // assert
        result.Count.ShouldBe(2);
        result.Select(item => item.Summary.Id.Value).ShouldBe([2, 3]);
    }

    private void Arrange(Movie selected, IReadOnlyList<RecommendationLibraryEntry> pool) =>
        Arrange(selected, pool, CrowdLinks.Empty);

    private void Arrange(Movie selected, IReadOnlyList<RecommendationLibraryEntry> pool, CrowdLinks crowdLinks)
    {
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(selected);
        var library = new List<RecommendationLibraryEntry>(pool.Count + 1)
        {
            Entry(selected, PlotVector(primary: 1.0f)),
        };
        library.AddRange(pool);
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>()).Returns(Snapshot(library, crowdLinks));
    }

    private static RecommendationLibraryEntry Entry(Movie movie, System.Collections.Immutable.ImmutableArray<float> plotEmbedding = default) =>
        new(
            MovieFeaturesMapper.From(
                movie,
                includeSignals: true,
                plotEmbedding: plotEmbedding.IsDefault ? null : plotEmbedding),
            MovieSummaryMapper.From(movie));

    private static RecommendationLibrarySnapshot Snapshot(
        IReadOnlyList<RecommendationLibraryEntry> entries,
        CrowdLinks? crowdLinks = null) =>
        new(entries, crowdLinks ?? CrowdLinks.Empty);

    private void ArrangeProfile(params TasteProfileMovie[] profileMovies) =>
        ArrangeProfileWith(
            [.. profileMovies],
            System.Collections.Immutable.ImmutableDictionary<MovieId, System.Collections.Immutable.ImmutableArray<float>>.Empty);

    private static TasteProfileMovie ProfileMovie(int id, params string[] genres) =>
        new(new MovieId(id), [.. genres], [], [Now], IsWatched: true);

    private static Movie IdenticalFeatureMovie(int id, bool isWatched = false) => NewMovie(
        id,
        genres: ["Drama", "Action"],
        directors: ["Director A"],
        actors: ["Actor A"],
        writers: ["Writer A"],
        releaseYear: 2000,
        runtimeMinutes: 120,
        imdbRating: 8.0m,
        isWatched: isWatched);

    private static bool HasStoryReason(RecommendedMovie movie) =>
        movie.Evidence.Any(evidence => evidence.Text == "Similar story");

    private static void AssertFeaturePrefix(RecommendedMovie actual, SimilarityResult expected)
    {
        var expectedEvidence = SimilarityScorer.BuildEvidence(expected, new RecommendationOptions { Weights = Weights });
        actual.Evidence.Count.ShouldBeGreaterThanOrEqualTo(expectedEvidence.Count);
        for (var index = 0; index < expectedEvidence.Count; index++)
        {
            actual.Evidence[index].ShouldBe(expectedEvidence[index]);
        }
    }

    private static Movie Selected() => NewMovie(
        1,
        genres: ["Drama", "Action"],
        directors: ["Director A"],
        actors: ["Actor A"],
        writers: ["Writer A"],
        releaseYear: 2000,
        runtimeMinutes: 120,
        imdbRating: 8.0m);

    private static RecommendationLibraryEntry[] Pool(int count) =>
        [.. Enumerable.Range(2, count).Select(id => Entry(
            NewMovie(id, genres: ["Drama"], directors: ["Director A"])))];

    private static RecommendationLibraryEntry[] Candidates(params Movie[] movies) =>
        [.. movies.Select(movie => Entry(movie))];

    private static RecommendationLibraryEntry[] DiversificationPool() =>
    [
        Entry(NewMovie(2, genres: ["Drama"])),
        Entry(NewMovie(3, genres: ["Drama"])),
        Entry(NewMovie(4, genres: ["Drama"])),
        Entry(NewMovie(5, genres: ["Drama"])),
        Entry(NewMovie(6, genres: ["Mystery"])),
        Entry(NewMovie(7, genres: ["Western"])),
    ];

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
        EnrichmentStatus? status = null,
        bool isInWatchlist = false,
        bool isWatched = false,
        bool isNotInterested = false,
        int? collectionId = null,
        string? collectionName = null)
    {
        var movieTitle = title ?? $"Movie {id}";
        var metadata = BuildMetadata(
            movieTitle,
            posterUrl,
            releaseYear,
            runtimeMinutes,
            imdbRating,
            genres,
            directors,
            actors,
            writers,
            withMetadata,
            collectionId,
            collectionName);

        return Movie.Rehydrate(
            new MovieId(id),
            movieTitle,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            isInWatchlist,
            metadata,
            status ?? EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null,
            isWatched: isWatched,
            review: isNotInterested ? Review.Disliked : Review.None);
    }

    private static MovieMetadata? BuildMetadata(
        string movieTitle,
        string? posterUrl,
        int? releaseYear,
        int? runtimeMinutes,
        decimal? imdbRating,
        string[]? genres,
        string[]? directors,
        string[]? actors,
        string[]? writers,
        bool withMetadata,
        int? collectionId = null,
        string? collectionName = null)
    {
        if (!withMetadata)
        {
            return null;
        }

        return new MovieMetadata(
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
            collectionName: collectionName);
    }

    private sealed class CancellingOnSecondPassLibrary(IReadOnlyList<RecommendationLibraryEntry> items, CancellationTokenSource source)
        : IReadOnlyList<RecommendationLibraryEntry>
    {
        private int passes;

        public int ScoredYielded { get; private set; }

        public int Count => items.Count;

        public RecommendationLibraryEntry this[int index] => items[index];

        public IEnumerator<RecommendationLibraryEntry> GetEnumerator()
        {
            passes++;
            for (var index = 0; index < items.Count; index++)
            {
                if (passes >= 2)
                {
                    if (index == 2)
                    {
                        source.Cancel();
                    }

                    ScoredYielded++;
                }

                yield return items[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class CancellingCandidates(IReadOnlyList<RecommendationLibraryEntry> items, CancellationTokenSource source)
        : IReadOnlyList<RecommendationLibraryEntry>
    {
        public int Yielded { get; private set; }

        public int Count => items.Count;

        public RecommendationLibraryEntry this[int index] => items[index];

        public IEnumerator<RecommendationLibraryEntry> GetEnumerator()
        {
            for (var index = 0; index < items.Count; index++)
            {
                if (index == 1)
                {
                    source.Cancel();
                }

                Yielded++;
                yield return items[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
