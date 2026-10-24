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
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class GetRecommendationsQueryHandlerTests
{
    private const int DefaultTake = 10;
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly SimilarityWeights Weights = new();
    private static readonly SimilarityWeights GenreOnlyWeights = new()
    {
        Director = 0.0,
        Writers = 0.0,
        Cast = 0.0,
        Genres = 3.0,
        YearProximity = 0.0,
        RuntimeProximity = 0.0,
        RatingBoost = 0.0,
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
        await candidateSource.DidNotReceive().GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SelectedMovieNotEnriched_ReturnsRecommendations()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, withMetadata: false, status: EnrichmentStatus.Pending);
        var pool = Candidates(NewMovie(2, genres: ["Drama"]));
        Arrange(selected, pool);

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
        result.Select(item => item.Summary.Id.Value).ToArray().ShouldBe(new[] { 2, 4, 3 });
        result[0].Score.ShouldBe(1.0);
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
    public async Task HandleAsync_DiversifiedResult_PreservesOriginalMovieScoreAndReasons()
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
            item.Reasons.ShouldBe(expected.Reasons);
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
            false);
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
        var expected = new MovieSummary(new MovieId(8), "Eight", null, null, null, null, EnrichmentStatus.Enriched, false, false, false);
        result[0].Summary.ShouldBe(expected);
    }

    [Fact]
    public async Task HandleAsync_ScoredCandidate_PreservesFeatureReasonPrefixAndOmitsStoryBelowThreshold()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = NewMovie(2, genres: ["Drama", "Action"], directors: ["Director A"], actors: ["Actor A"], writers: ["Writer A"], releaseYear: 2000, runtimeMinutes: 120, imdbRating: 8.0m);
        Arrange(selected, [new RecommendationCandidate(candidate, 0.4)]);
        var expected = SimilarityScorer.Score(
            MovieFeaturesMapper.From(selected),
            MovieFeaturesMapper.From(candidate),
            Weights,
            GenreWeights.Calculate(Counts));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        expected.Reasons.ShouldNotBeEmpty();
        result[0].Score.ShouldBe(expected.Score);
        result[0].Reasons.Count.ShouldBe(expected.Reasons.Count);
        for (var index = 0; index < expected.Reasons.Count; index++)
        {
            result[0].Reasons[index].ShouldBe(expected.Reasons[index]);
        }

        HasStoryReason(result[0]).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0.8, true)]
    [InlineData(0.6, true)]
    [InlineData(0.4, false)]
    [InlineData(0.0, false)]
    [InlineData(-1.0, false)]
    public async Task HandleAsync_NamedSyntheticCosine_UsesStrictDefaultThreshold(double cosine, bool expectStory)
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = IdenticalFeatureMovie(2);
        Arrange(selected, [new RecommendationCandidate(candidate, cosine)]);
        var expected = SimilarityScorer.Score(
            MovieFeaturesMapper.From(selected),
            MovieFeaturesMapper.From(candidate),
            Weights,
            GenreWeights.Calculate(Counts));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result[0].Score.ShouldBe(expected.Score);
        AssertFeaturePrefix(result[0], expected);
        AssertStorySuffix(result[0], expectStory);
    }

    [Fact]
    public async Task HandleAsync_CosineEqualToDefaultThreshold_EmitsNoStoryReason()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        Arrange(selected, [new RecommendationCandidate(IdenticalFeatureMovie(2), 0.5)]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        AssertStorySuffix(result[0], expectStory: false);
    }

    [Theory]
    [InlineData(0.7, 0.8, true)]
    [InlineData(0.7, 0.6, false)]
    [InlineData(0.5, 0.6, true)]
    [InlineData(-0.5, 0.0, true)]
    [InlineData(-0.5, -1.0, false)]
    [InlineData(1.0, 1.0, false)]
    public async Task HandleAsync_CustomThreshold_OverridesDefault(double threshold, double cosine, bool expectStory)
    {
        // arrange
        var handler = NewHandler(threshold: threshold);
        var selected = Selected();
        Arrange(selected, [new RecommendationCandidate(IdenticalFeatureMovie(2), cosine)]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        AssertStorySuffix(result[0], expectStory);
    }

    [Theory]
    [InlineData(0.6, true)]
    [InlineData(0.5, false)]
    public async Task HandleAsync_StoryEmission_IsIndependentOfSimilarityWeights(double cosine, bool expectStory)
    {
        // arrange
        var zeroWeights = new SimilarityWeights
        {
            Director = 0.0,
            Writers = 0.0,
            Cast = 0.0,
            Genres = 0.0,
            YearProximity = 0.0,
            RuntimeProximity = 0.0,
            RatingBoost = 0.0,
        };
        var selected = Selected();
        var candidate = NewMovie(2, genres: ["Drama"]);
        Arrange(selected, [new RecommendationCandidate(candidate, cosine)]);

        // act
        var defaultWeighted = await NewHandler().HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);
        var zeroWeighted = await NewHandler(weights: zeroWeights).HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        AssertStorySuffix(defaultWeighted[0], expectStory);
        AssertStorySuffix(zeroWeighted[0], expectStory);
        zeroWeighted[0].Score.ShouldBe(0.0);
        defaultWeighted[0].Score.ShouldBeGreaterThan(0.0);
    }

    [Fact]
    public async Task HandleAsync_CandidatesAtOrBelowThreshold_AreRetainedAndScored()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var low = IdenticalFeatureMovie(2);
        var zero = IdenticalFeatureMovie(3);
        var negative = IdenticalFeatureMovie(4);
        Arrange(selected, [
            new RecommendationCandidate(low, 0.4),
            new RecommendationCandidate(zero, 0.0),
            new RecommendationCandidate(negative, -1.0),
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
            var expected = SimilarityScorer.Score(MovieFeaturesMapper.From(selected), MovieFeaturesMapper.From(movie), Weights, genreWeights);
            item.Score.ShouldBe(expected.Score);
        }
    }

    [Fact]
    public async Task HandleAsync_StorySuffix_DoesNotChangeScoreOrderOrTake()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var highCosineLowId = IdenticalFeatureMovie(4);
        var lowCosineHighId = IdenticalFeatureMovie(9);
        Arrange(selected, [
            new RecommendationCandidate(lowCosineHighId, 0.9),
            new RecommendationCandidate(highCosineLowId, 0.6),
        ]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 2), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ToArray().ShouldBe(new[] { 4, 9 });
        result[0].Score.ShouldBe(result[1].Score);
        result.ShouldAllBe(item => HasStoryReason(item));
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
            Arg.Is<IReadOnlyCollection<MovieId>>(ids => ids.Count == 1 && ids.Single() == new MovieId(3)),
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
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetTasteProfileAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, 0.0)]
    [InlineData(true, 0.25)]
    public async Task HandleAsync_WatchedSignal_MapsToBlendPenalty(bool isWatched, double expectedPenalty)
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, genres: ["Drama"]);
        var candidate = NewMovie(5, genres: ["Drama"], isWatched: isWatched);
        Arrange(selected, Candidates(candidate));
        ArrangeProfile(ProfileMovie(90, "Drama"));
        var selectedSimilarity = SimilarityScorer.Score(
            MovieFeaturesMapper.From(selected),
            MovieFeaturesMapper.From(candidate),
            Weights,
            GenreWeights.Calculate(Counts));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result[0].Score.ShouldBe(selectedSimilarity.Score + 1.0 - expectedPenalty, 1e-12);
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
            Arg.Is<IReadOnlyCollection<MovieId>>(ids => ids.Count == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DerivedProfile_IsObtainedThroughThePort()
    {
        // arrange
        var handler = NewHandler(blendSelectedWeight: 0.0, blendProfileWeight: 1.0);
        var selected = NewMovie(1, genres: ["Action"]);
        var comedy = NewMovie(2, genres: ["Comedy"]);
        var drama = NewMovie(3, genres: ["Drama"]);
        Arrange(selected, Candidates(comedy, drama));
        ArrangeProfile(ProfileMovie(90, "Drama"));
        ArrangeTasteProfile(new TasteProfile(
            System.Collections.Immutable.ImmutableDictionary<string, double>.Empty.Add("Comedy", 1.0),
            []));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        await profileSource.Received(1).GetTasteProfileAsync(Arg.Any<CancellationToken>());
        result.Select(item => item.Summary.Id.Value).ToArray().ShouldBe(new[] { 2, 3 });
        result[0].Score.ShouldBeGreaterThan(0.0);
        result[1].Score.ShouldBe(0.0);
    }

    [Fact]
    public async Task HandleAsync_CandidateAndGenreWork_StayOutsideTheTasteProfileEntry()
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
        await profileSource.Received(1).GetTasteProfileAsync(cts.Token);
        await profileSource.Received(1).GetProfileInputsAsync(
            Arg.Is<IReadOnlyCollection<MovieId>>(ids => ids.Count == 3),
            cts.Token);
        await catalog.Received(1).GetGenreCountsAsync(cts.Token);
    }

    [Fact]
    public async Task HandleAsync_DerivedProfileCancelsToken_ThrowsBeforeCandidateInputs()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        Arrange(Selected(), Pool(2));
        profileSource.GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(EmptyTasteProfile());
            });

        // act / assert
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), cts.Token));
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DerivedProfileFails_PropagatesErrorWithoutCandidateInputs()
    {
        // arrange
        var handler = NewHandler();
        var failure = new InvalidOperationException("derived profile failed");
        Arrange(Selected(), Pool(2));
        profileSource.GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<TasteProfile>(failure));

        // act / assert
        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new GetRecommendationsQuery(new MovieId(1)), CancellationToken.None));
        thrown.ShouldBe(failure);
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ProfileWeightOnly_OrdersByProfileSimilarityWithoutRefill()
    {
        // arrange
        var handler = NewHandler(blendSelectedWeight: 0.0, blendProfileWeight: 1.0);
        var selected = NewMovie(1, genres: ["Drama"]);
        var comedy = NewMovie(3, genres: ["Comedy"]);
        var drama = NewMovie(4, genres: ["Drama"]);
        Arrange(selected, Candidates(comedy, drama));
        ArrangeProfile(ProfileMovie(90, "Drama"));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result.Select(item => item.Summary.Id.Value).ToArray().ShouldBe(new[] { 4, 3 });
        result[0].Score.ShouldBe(1.0, 1e-12);
        result[1].Score.ShouldBe(0.0, 1e-12);
    }

    [Fact]
    public async Task HandleAsync_LargePenalties_ProduceRawFiniteScoreOutsideUnitInterval()
    {
        // arrange
        var handler = NewHandler(
            blendSelectedWeight: 2.0,
            blendProfileWeight: 0.0,
            alreadyWatchedPenalty: 5.0);
        var selected = NewMovie(1, genres: ["Drama"]);
        var candidate = NewMovie(5, genres: ["Drama"], isWatched: true);
        Arrange(selected, Candidates(candidate));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result[0].Score.ShouldBe((2.0 * 1.0) - (5.0 * 1.0), 1e-12);
    }

    [Fact]
    public async Task HandleAsync_NegativeAndPositiveScores_RebaseSelectorRelevanceButPreserveRawScores()
    {
        // arrange
        var handler = NewHandler(
            blendSelectedWeight: 2.0,
            blendProfileWeight: 0.0,
            alreadyWatchedPenalty: 5.0);
        var selected = Selected();
        var unwatched = IdenticalFeatureMovie(2);
        var watched = IdenticalFeatureMovie(3, isWatched: true);
        Arrange(selected, [new RecommendationCandidate(unwatched, 0.0), new RecommendationCandidate(watched, 0.0)]);

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id, 2), CancellationToken.None);

        // assert
        result.Count.ShouldBe(2);
        result.Select(item => item.Summary.Id.Value).ShouldBe(new[] { 2, 3 });
        result[0].Score.ShouldBe(2.0, 1e-12);
        result[1].Score.ShouldBe(-3.0, 1e-12);
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
        await candidateSource.DidNotReceive().GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
        await catalog.DidNotReceive().GetGenreCountsAsync(Arg.Any<CancellationToken>());
        await profileSource.DidNotReceive().GetProfileInputsAsync(
            Arg.Any<IReadOnlyCollection<MovieId>>(),
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
        await candidateSource.DidNotReceive().GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CandidateLoadCancelsToken_ThrowsBeforeGenreWeights()
    {
        // arrange
        var handler = NewHandler();
        using var cts = new CancellationTokenSource();
        IReadOnlyList<RecommendationCandidate> pool = Pool(2);
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(Selected());
        candidateSource.GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
                cts.Cancel();
                return Task.FromResult(pool);
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
        var pool = new CancellingCandidates(Pool(3), cts);
        Arrange(Selected(), pool);

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
        candidateSource.GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<RecommendationCandidate>>(failure));

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
    public async Task HandleAsync_NoStructuralOrPersonalExplanation_EmitsStructuralFallback()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var unrelated = NewMovie(12, genres: ["Mystery"]);
        Arrange(selected, Candidates(unrelated));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result[0].Reasons.ShouldBe([Reason.Structural("No shared features identified")]);
    }

    [Theory]
    [InlineData(1.0, 3)]
    [InlineData(0.0, 0)]
    public async Task HandleAsync_ProfileOverlap_EmitsPersonalReasonsOnlyWhenProfileWeightPositive(
        double profileWeight,
        int expectedPersonalCount)
    {
        // arrange
        var handler = NewHandler(blendProfileWeight: profileWeight);
        var selected = Selected();
        var candidate = NewMovie(14, genres: ["Drama", "Comedy", "Horror"]);
        Arrange(selected, Candidates(candidate));
        ArrangeProfile(ProfileMovie(90, "Drama", "Comedy", "Horror"));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result[0].Reasons.Count(reason => reason.Kind == ReasonKind.Personal).ShouldBe(expectedPersonalCount);
        if (expectedPersonalCount == 0)
        {
            result[0].Reasons.ShouldAllBe(reason => reason.Kind == ReasonKind.Structural);
        }
    }

    [Fact]
    public async Task HandleAsync_MultiGenreOverlap_EmitsOnePersonalReasonPerCanonicalGenreInCaseInsensitiveThenOrdinalOrder()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = NewMovie(15, genres: ["Horror", "Comedy", "Drama"]);
        Arrange(selected, Candidates(candidate));
        ArrangeProfile(ProfileMovie(90, "Drama", "Comedy", "Horror"));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        PersonalTexts(result[0]).ShouldBe(
        [
            "Matches your Comedy interest",
            "Matches your Drama interest",
            "Matches your Horror interest",
        ]);
    }

    [Fact]
    public async Task HandleAsync_StructuralAndPersonalReasons_KeepStructuralReasonsFirstInScorerOrder()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = NewMovie(16, genres: ["Drama"], directors: ["Director A"], actors: ["Actor A"], writers: ["Writer A"]);
        Arrange(selected, [new RecommendationCandidate(candidate, 0.9)]);
        ArrangeProfile(ProfileMovie(90, "Drama"));
        var expectedStructural = SimilarityScorer.Score(
            MovieFeaturesMapper.From(selected),
            MovieFeaturesMapper.From(candidate),
            Weights,
            GenreWeights.Calculate(Counts));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        var reasons = result[0].Reasons;
        reasons.Take(expectedStructural.Reasons.Count).ShouldBe(expectedStructural.Reasons);
        reasons[expectedStructural.Reasons.Count].ShouldBe(Reason.Structural("Similar story"));
        var firstPersonal = reasons.Select((reason, index) => (reason, index))
            .First(item => item.reason.Kind == ReasonKind.Personal).index;
        reasons.Take(firstPersonal).ShouldAllBe(reason => reason.Kind == ReasonKind.Structural);
        reasons.Skip(firstPersonal).ShouldAllBe(reason => reason.Kind == ReasonKind.Personal);
    }

    [Fact]
    public async Task HandleAsync_PersonalReasons_UseGenericTemplatesWithoutNamedTitle()
    {
        // arrange
        var handler = NewHandler();
        var selected = Selected();
        var candidate = NewMovie(17, title: "Untitled Personal Fixture", genres: ["Drama"]);
        Arrange(selected, Candidates(candidate));
        ArrangeProfile(ProfileMovie(90, "Drama"));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        PersonalTexts(result[0]).ShouldBe(["Matches your Drama interest"]);
        result[0].Reasons.ShouldAllBe(reason => !reason.Text.Contains("Untitled Personal Fixture", StringComparison.Ordinal));
        result[0].Reasons.ShouldAllBe(reason => !reason.Text.Contains("Because you watched", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleAsync_PositivePlotChannel_EmitsViewingHistoryPersonalReason()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, genres: ["Drama"]);
        var candidate = NewMovie(18, genres: ["Mystery"]);
        Arrange(selected, Candidates(candidate));
        var vector = PlotVector(primary: 1.0f);
        ArrangeProfileWith(
            [new TasteProfileMovie(new MovieId(90), [], vector, [Now], IsWatched: true)],
            System.Collections.Immutable.ImmutableDictionary<MovieId, System.Collections.Immutable.ImmutableArray<float>>
                .Empty.Add(candidate.Id, vector));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        PersonalTexts(result[0]).ShouldBe(["Matches your viewing history"]);
        result[0].Reasons.Count(reason => reason.Text == "Matches your viewing history").ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ZeroPlotSimilarity_EmitsNoViewingHistoryReason()
    {
        // arrange
        var handler = NewHandler();
        var selected = NewMovie(1, genres: ["Drama"]);
        var candidate = NewMovie(19, genres: ["Mystery"]);
        Arrange(selected, Candidates(candidate));
        ArrangeProfileWith(
            [new TasteProfileMovie(new MovieId(90), [], PlotVector(primary: 1.0f), [Now], IsWatched: true)],
            System.Collections.Immutable.ImmutableDictionary<MovieId, System.Collections.Immutable.ImmutableArray<float>>
                .Empty.Add(candidate.Id, PlotVector(primary: 0.0f, secondary: 1.0f)));

        // act
        var result = await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);

        // assert
        result[0].Reasons.ShouldNotContain(reason => reason.Text == "Matches your viewing history");
        result[0].Reasons.ShouldBe([Reason.Structural("No shared features identified")]);
    }

    private static string[] PersonalTexts(RecommendedMovie movie) =>
        [.. movie.Reasons.Where(reason => reason.Kind == ReasonKind.Personal).Select(reason => reason.Text)];

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
        System.Collections.Immutable.ImmutableDictionary<MovieId, System.Collections.Immutable.ImmutableArray<float>> embeddings)
    {
        profileSource
            .GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new RecommendationProfileInputs(profileMovies, embeddings)));

        profileSource
            .GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(ComputeProfile(profileMovies)));
    }

    private void ArrangeTasteProfile(TasteProfile profile) =>
        profileSource
            .GetTasteProfileAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(profile));

    private static TasteProfile ComputeProfile(
        System.Collections.Immutable.ImmutableArray<TasteProfileMovie> profileMovies) =>
        new TasteProfileCalculator(new FixedTimeProvider(Now), new RecommendationOptions()).Calculate(
            profileMovies,
            GenreWeights.Calculate(Counts));

    private static TasteProfile EmptyTasteProfile() =>
        new(
            System.Collections.Immutable.ImmutableDictionary.Create<string, double>(StringComparer.OrdinalIgnoreCase),
            []);

    private GetRecommendationsQueryHandler NewHandler(
        int optionsTake = DefaultTake,
        double threshold = 0.5,
        SimilarityWeights? weights = null,
        double blendSelectedWeight = 1.0,
        double blendProfileWeight = 1.0,
        double alreadyWatchedPenalty = 0.25,
        double notInterestedPenalty = 1.0,
        double mmrLambda = 0.7) =>
        new(
            movies,
            candidateSource,
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            Microsoft.Extensions.Options.Options.Create(new RecommendationOptions
            {
                Take = optionsTake,
                Weights = weights ?? Weights,
                SimilarStoryThreshold = threshold,
                BlendSelectedWeight = blendSelectedWeight,
                BlendProfileWeight = blendProfileWeight,
                AlreadyWatchedPenalty = alreadyWatchedPenalty,
                NotInterestedPenalty = notInterestedPenalty,
                MmrLambda = mmrLambda,
            }));

    private void Arrange(Movie selected, IReadOnlyList<RecommendationCandidate> pool)
    {
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(selected);
        candidateSource.GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(pool);
    }

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
        movie.Reasons.Any(reason => reason.Text == "Similar story");

    private static void AssertFeaturePrefix(RecommendedMovie actual, SimilarityResult expected)
    {
        actual.Reasons.Count.ShouldBeGreaterThanOrEqualTo(expected.Reasons.Count);
        for (var index = 0; index < expected.Reasons.Count; index++)
        {
            actual.Reasons[index].ShouldBe(expected.Reasons[index]);
        }
    }

    private static void AssertStorySuffix(RecommendedMovie actual, bool expectStory)
    {
        if (!expectStory)
        {
            HasStoryReason(actual).ShouldBeFalse();
            return;
        }

        actual.Reasons.ShouldNotBeEmpty();
        actual.Reasons[^1].ShouldBe(Reason.Structural("Similar story"));
        actual.Reasons.Count(reason => reason.Text == "Similar story").ShouldBe(1);
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

    private static RecommendationCandidate[] Pool(int count) =>
        [.. Enumerable.Range(2, count).Select(id => new RecommendationCandidate(
            NewMovie(id, genres: ["Drama"], directors: ["Director A"]),
            Cosine(id)))];

    private static RecommendationCandidate[] Candidates(params Movie[] movies) =>
        [.. movies.Select((movie, index) => new RecommendationCandidate(movie, Cosine(index + 2)))];

    private static RecommendationCandidate[] DiversificationPool() =>
    [
        new(NewMovie(2, genres: ["Drama"]), 0.0),
        new(NewMovie(3, genres: ["Drama"]), 0.0),
        new(NewMovie(4, genres: ["Drama"]), 0.0),
        new(NewMovie(5, genres: ["Drama"]), 0.0),
        new(NewMovie(6, genres: ["Mystery"]), 0.0),
        new(NewMovie(7, genres: ["Western"]), 0.0),
    ];

    private static double Cosine(int seed) => ((seed % 21) - 10) / 10.0;

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
        bool isNotInterested = false)
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
            withMetadata);

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
            isNotInterested: isNotInterested);
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
        bool withMetadata)
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
            writers: writers);
    }

    private sealed class CancellingCandidates(IReadOnlyList<RecommendationCandidate> items, CancellationTokenSource source)
        : IReadOnlyList<RecommendationCandidate>
    {
        public int Yielded { get; private set; }

        public int Count => items.Count;

        public RecommendationCandidate this[int index] => items[index];

        public IEnumerator<RecommendationCandidate> GetEnumerator()
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
