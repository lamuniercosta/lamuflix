using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Library;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class ReviewRankingHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
            ["Horror"] = 30,
        });

    private static readonly SimilarityWeights GenreOnlyWeights = new()
    {
        Director = 0.0,
        Writers = 0.0,
        Cast = 0.0,
        Genres = 3.0,
        YearProximity = 0.0,
        RuntimeProximity = 0.0,
        RatingBoost = 0.0,
        Keyword = 0.0,
        Collection = 0.0,
        ReviewPlotWeight = 0.0,
    };

    private static GenreWeights Weights => GenreWeights.Calculate(Counts);

    [Fact]
    public async Task ForYou_LikedProfile_RaisesSimilarCandidateAbovePreLikeBaseline()
    {
        // arrange
        var harness = new ForYouHarness();
        var similar = MakeMovie(2, "Similar drama", ["Drama"]);
        var other = MakeMovie(3, "Other comedy", ["Comedy"]);
        harness.Arrange(similar, other);

        var baseline = await harness.HandleAsync(Profile());
        var beforeOrder = baseline.Select(item => item.Summary.Id.Value).ToList();

        // act
        var liked = await harness.HandleAsync(Profile(likedGenres: ["Drama"]));

        // assert
        beforeOrder[0].ShouldBe(2);
        var afterOrder = liked.Select(item => item.Summary.Id.Value).ToList();
        afterOrder[0].ShouldBe(2);
        liked.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeGreaterThan(baseline.Single(item => item.Summary.Id.Value == 2).Score);
    }

    [Fact]
    public async Task ForYou_DislikedProfile_LowersSimilarCandidateScore()
    {
        // arrange
        var harness = new ForYouHarness();
        var similar = MakeMovie(2, "Similar drama", ["Drama"]);
        var other = MakeMovie(3, "Other comedy", ["Comedy"]);
        harness.Arrange(similar, other);

        var baseline = await harness.HandleAsync(Profile());

        // act
        var disliked = await harness.HandleAsync(Profile(dislikedGenres: ["Drama"]));

        // assert
        disliked.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeLessThan(baseline.Single(item => item.Summary.Id.Value == 2).Score);
    }

    [Fact]
    public async Task ForYou_DislikedCandidate_IsExcludedFromResults()
    {
        // arrange
        var harness = new ForYouHarness();
        var disliked = MakeMovie(2, "Disliked drama", ["Drama"], review: Review.Disliked);
        var eligible = MakeMovie(3, "Eligible comedy", ["Comedy"]);
        harness.Arrange(disliked, eligible);

        // act
        var result = await harness.HandleAsync(Profile());

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldBe([3]);
    }

    [Fact]
    public async Task ForYou_LikedCandidate_IsNotExcluded()
    {
        // arrange
        var harness = new ForYouHarness();
        var liked = MakeMovie(2, "Liked drama", ["Drama"], review: Review.Liked);
        var other = MakeMovie(3, "Other comedy", ["Comedy"]);
        harness.Arrange(liked, other);

        // act
        var result = await harness.HandleAsync(Profile());

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldContain(2);
    }

    [Fact]
    public async Task ForYou_ClearedReview_RemovesThePreviousSignal()
    {
        // arrange
        var harness = new ForYouHarness();
        var similar = MakeMovie(2, "Similar drama", ["Drama"]);
        var other = MakeMovie(3, "Other comedy", ["Comedy"]);
        harness.Arrange(similar, other);

        var liked = await harness.HandleAsync(Profile(likedGenres: ["Drama"]));
        var baseline = await harness.HandleAsync(Profile());

        // act
        var cleared = await harness.HandleAsync(Profile());

        // assert
        cleared.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBe(baseline.Single(item => item.Summary.Id.Value == 2).Score, 1e-12);
        cleared.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeLessThan(liked.Single(item => item.Summary.Id.Value == 2).Score);
    }

    [Fact]
    public async Task ForYou_OppositeSwitch_ReplacesLikeWithDislikeEffect()
    {
        // arrange
        var harness = new ForYouHarness();
        var similar = MakeMovie(2, "Similar drama", ["Drama"]);
        var other = MakeMovie(3, "Other comedy", ["Comedy"]);
        harness.Arrange(similar, other);

        var liked = await harness.HandleAsync(Profile(likedGenres: ["Drama"]));
        var baseline = await harness.HandleAsync(Profile());

        // act
        var switched = await harness.HandleAsync(Profile(dislikedGenres: ["Drama"]));

        // assert
        switched.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeLessThan(baseline.Single(item => item.Summary.Id.Value == 2).Score);
        switched.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeLessThan(liked.Single(item => item.Summary.Id.Value == 2).Score);
    }

    [Fact]
    public async Task ForYou_LikedProfile_EmitsTruthfulLikedChip()
    {
        // arrange
        var harness = new ForYouHarness();
        var similar = MakeMovie(2, "Similar drama", ["Drama"]);
        harness.Arrange(similar);

        // act
        var result = await harness.HandleAsync(Profile(likedGenres: ["Drama"], likedTitle: "Alien"));

        // assert
        result.Single().Reasons.ShouldContain(Reason.Personal("Because you liked Alien"));
    }

    [Fact]
    public async Task ForYou_NegativeOnlyProfile_EmitsNoLikedChip()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(MakeMovie(2, "Similar drama", ["Drama"]));

        // act
        var result = await harness.HandleAsync(Profile(dislikedGenres: ["Drama"]));

        // assert
        result.Single().Reasons.ShouldNotContain(reason => reason.Text.StartsWith("Because you liked", StringComparison.Ordinal));
    }

    [Fact(DisplayName = "SMOKE-003 For You after a like ranks similar candidates higher without fabricated reasons")]
    public async Task ForYou_LikedProfile_RanksSimilarHigherWithoutFabricatedReasons()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(MakeMovie(2, "Similar drama", ["Drama"]), MakeMovie(3, "Other comedy", ["Comedy"]));

        var baseline = await harness.HandleAsync(Profile());

        // act
        var liked = await harness.HandleAsync(Profile(likedGenres: ["Drama"], likedTitle: "Alien"));

        // assert
        liked.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeGreaterThan(baseline.Single(item => item.Summary.Id.Value == 2).Score);
        liked.Select(item => item.Summary.Id.Value).First().ShouldBe(2);
        liked.Single(item => item.Summary.Id.Value == 3).Reasons
            .ShouldNotContain(reason => reason.Text.StartsWith("Because you liked", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Related_LikedProfile_RaisesSimilarCandidateAbovePreLikeBaseline()
    {
        // arrange
        var harness = new RelatedHarness();
        var similar = MakeMovie(2, "Similar drama", ["Drama"], directors: ["Director A"]);
        var other = MakeMovie(3, "Other comedy", ["Comedy"], directors: ["Director B"]);
        harness.Arrange(similar, other);

        var baseline = await harness.HandleAsync(Profile());
        var liked = await harness.HandleAsync(Profile(likedGenres: ["Drama"]));

        // assert
        liked.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeGreaterThan(baseline.Single(item => item.Summary.Id.Value == 2).Score);
    }

    [Fact]
    public async Task Related_DislikedProfile_LowersSimilarCandidateScore()
    {
        // arrange
        var harness = new RelatedHarness();
        var similar = MakeMovie(2, "Similar drama", ["Drama"], directors: ["Director A"]);
        var other = MakeMovie(3, "Other comedy", ["Comedy"], directors: ["Director B"]);
        harness.Arrange(similar, other);

        var baseline = await harness.HandleAsync(Profile());
        var disliked = await harness.HandleAsync(Profile(dislikedGenres: ["Drama"]));

        // assert
        disliked.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeLessThan(baseline.Single(item => item.Summary.Id.Value == 2).Score);
    }

    [Fact]
    public async Task Related_DislikedCandidate_IsExcludedFromResults()
    {
        // arrange
        var harness = new RelatedHarness();
        var disliked = MakeMovie(2, "Disliked drama", ["Drama"], review: Review.Disliked);
        var eligible = MakeMovie(3, "Eligible comedy", ["Comedy"]);
        harness.Arrange(disliked, eligible);

        // act
        var result = await harness.HandleAsync(Profile());

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldBe([3]);
    }

    [Fact]
    public async Task Related_LikedCandidate_IsNotExcluded()
    {
        // arrange
        var harness = new RelatedHarness();
        var liked = MakeMovie(2, "Liked drama", ["Drama"], review: Review.Liked);
        var other = MakeMovie(3, "Other comedy", ["Comedy"]);
        harness.Arrange(liked, other);

        // act
        var result = await harness.HandleAsync(Profile());

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldContain(2);
    }

    [Fact]
    public async Task Related_LikedProfile_EmitsTruthfulLikedChip()
    {
        // arrange
        var harness = new RelatedHarness();
        harness.Arrange(MakeMovie(2, "Similar drama", ["Drama"]));

        // act
        var result = await harness.HandleAsync(Profile(likedGenres: ["Drama"], likedTitle: "Alien"));

        // assert
        result.Single().Reasons.ShouldContain(Reason.Personal("Because you liked Alien"));
    }

    [Fact]
    public async Task Related_NegativeOnlyProfile_EmitsNoLikedChip()
    {
        // arrange
        var harness = new RelatedHarness();
        harness.Arrange(MakeMovie(2, "Similar drama", ["Drama"]));

        // act
        var result = await harness.HandleAsync(Profile(dislikedGenres: ["Drama"]));

        // assert
        result.Single().Reasons.ShouldNotContain(reason => reason.Text.StartsWith("Because you liked", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ForYou_DisabledReviewChannels_EmitsNoLikedChip()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(MakeMovie(2, "Similar drama", ["Drama"]));

        // act
        var result = await harness.HandleAsync(
            Profile(likedGenres: ["Drama"], likedTitle: "Alien"),
            options => options with
            {
                Weights = GenreOnlyWeights with { Genres = 0.0 },
            });

        // assert
        result.Single().Reasons.ShouldNotContain(reason => reason.Text.StartsWith("Because you liked", StringComparison.Ordinal));
    }

    private static TasteProfile Profile(
        string[]? likedGenres = null,
        string[]? dislikedGenres = null,
        string? likedTitle = null)
    {
        var liked = Channels(likedGenres, [], []);
        var disliked = Channels(dislikedGenres, [], []);

        return new TasteProfile(
            ImmutableDictionary<string, double>.Empty,
            ImmutableArray<float>.Empty)
        {
            Liked = liked,
            Disliked = disliked,
            LikedSources = likedGenres is { Length: > 0 }
                ?
                [
                    new LikedReviewSource(
                        new MovieId(90),
                        likedTitle ?? "Liked source",
                        Channels(likedGenres, [], [])),
                ]
                : [],
        };
    }

    private static ReviewProfileChannels Channels(string[]? genres, string[] cast, string[] directors) =>
        new(
            ImmutableArray<float>.Empty,
            Preferences(genres ?? [], Weights.GetWeight),
            Preferences(cast, _ => 1.0),
            Preferences(directors, _ => 1.0));

    private static ImmutableDictionary<string, double> Preferences(string[] names, Func<string, double> scale) =>
        names.ToImmutableDictionary(name => name, scale, StringComparer.OrdinalIgnoreCase);

    private static Movie MakeMovie(
        int id,
        string title,
        string[] genres,
        string[]? directors = null,
        bool isWatched = false,
        Review? review = null)
    {
        var metadata = new MovieMetadata(
            title,
            directors: directors,
            genres: genres);

        return Movie.Rehydrate(
            new MovieId(id),
            title,
            new LibraryPath($"C:/library/{id}.mkv"),
            new MediaFormat("mkv"),
            false,
            metadata,
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            null,
            isWatched: isWatched,
            review: review ?? Review.None);
    }

    private sealed class ForYouHarness
    {
        private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
        private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
        private readonly IRecommendationProfileSource profileSource = Substitute.For<IRecommendationProfileSource>();

        public ForYouHarness()
        {
            catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Counts);
        }


        public void Arrange(params Movie[] library)
        {
            var summaries = library
                .Select(movie => new MovieSummary(
                    movie.Id,
                    movie.Title,
                    null,
                    null,
                    null,
                    null,
                    movie.Status,
                    movie.IsInWatchlist,
                    movie.IsWatched,
                    movie.Review))
                .ToImmutableArray();

            catalog.BrowseAsync(Arg.Any<MovieQuery>(), Arg.Any<CancellationToken>())
                .Returns(new PagedResult<MovieSummary>(summaries, summaries.Length));

            foreach (var movie in library)
            {
                movies.GetAsync(movie.Id, Arg.Any<CancellationToken>()).Returns(movie);
            }
        }

        public async Task<ImmutableList<RecommendedMovie>> HandleAsync(
            TasteProfile profile,
            Func<RecommendationOptions, RecommendationOptions>? configure = null)
        {
            profileSource.GetTasteProfileAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(profile));
            profileSource.GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new RecommendationProfileInputs(
                    [
                        new TasteProfileMovie(
                            new MovieId(500),
                            ["Drama"],
                            ImmutableArray<float>.Empty,
                            [Now],
                            IsWatched: true),
                    ],
                    ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty)));

            var options = configure is null
                ? new RecommendationOptions { Weights = GenreOnlyWeights, MmrLambda = 1.0 }
                : configure(new RecommendationOptions { Weights = GenreOnlyWeights, MmrLambda = 1.0 });

            var handler = new GetForYouRecommendationsQueryHandler(
                catalog,
                movies,
                profileSource,
                new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
                Microsoft.Extensions.Options.Options.Create(options));

            return await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None);
        }
    }

    private sealed class RelatedHarness
    {
        private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
        private readonly IRecommendationCandidateSource candidates = Substitute.For<IRecommendationCandidateSource>();
        private readonly IRecommendationProfileSource profileSource = Substitute.For<IRecommendationProfileSource>();
        private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
        private readonly Movie selected = MakeMovie(1, "Selected", ["Drama"], directors: ["Director A"]);

        public RelatedHarness()
        {
            catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Counts);
            movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Movie?>(selected));
        }

        public void Arrange(params Movie[] pool)
        {
            candidates.GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
                .Returns(
                [
                    .. pool.Select(movie => new RecommendationCandidate(movie, 0.0)),
                ]);
        }

        public async Task<ImmutableList<RecommendedMovie>> HandleAsync(TasteProfile profile)
        {
            profileSource.GetTasteProfileAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(profile));
            profileSource.GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new RecommendationProfileInputs(
                    [
                        new TasteProfileMovie(
                            new MovieId(500),
                            ["Drama"],
                            ImmutableArray<float>.Empty,
                            [Now],
                            IsWatched: true),
                    ],
                    ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty)));

            var handler = new GetRecommendationsQueryHandler(
                movies,
                candidates,
                profileSource,
                new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
                Microsoft.Extensions.Options.Options.Create(new RecommendationOptions
                {
                    Weights = GenreOnlyWeights,
                    MmrLambda = 1.0,
                    SimilarStoryThreshold = 1.0,
                }));

            return await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);
        }
    }
}


