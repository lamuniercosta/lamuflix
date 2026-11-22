using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
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
        Themes = 0.0,
        Plot = 0.0,
        Cast = 0.0,
        Genres = 3.0,
        Collection = 0.0,
    };

    private static readonly KeywordCounts EmptyKeywordCounts = new(0, new Dictionary<string, long>());

    private static GenreWeights Weights => GenreWeights.Calculate(Counts);

    [Fact]
    public async Task ForYou_LikedSeed_RanksSimilarCandidateAboveUnrelatedCandidate()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(
            MakeMovie(90, "Liked drama", ["Drama"], review: Review.Liked),
            MakeMovie(2, "Similar drama", ["Drama"]),
            MakeMovie(3, "Drama comedy", ["Drama", "Comedy"]));

        // act
        var result = await harness.HandleAsync(Profile());

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldBe([2, 3]);
        result.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeGreaterThan(result.Single(item => item.Summary.Id.Value == 3).Score);
    }

    [Fact]
    public async Task ForYou_DislikedSeed_LowersSimilarCandidateScore()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(
            MakeMovie(90, "Liked drama", ["Drama"], review: Review.Liked),
            MakeMovie(2, "Similar drama", ["Drama"]));
        var baseline = await harness.HandleAsync(Profile());

        // act
        harness.Arrange(
            MakeMovie(90, "Liked drama", ["Drama"], review: Review.Liked),
            MakeMovie(95, "Disliked drama", ["Drama"], review: Review.Disliked),
            MakeMovie(2, "Similar drama", ["Drama"]));
        var disliked = await harness.HandleAsync(Profile());

        // assert
        disliked.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeLessThan(baseline.Single(item => item.Summary.Id.Value == 2).Score);
    }

    [Fact]
    public async Task ForYou_DislikedCandidate_IsExcludedFromResults()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(
            MakeMovie(90, "Liked drama", ["Drama"], review: Review.Liked),
            MakeMovie(2, "Disliked drama", ["Drama"], review: Review.Disliked),
            MakeMovie(3, "Eligible drama", ["Drama"]));

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
        harness.Arrange(
            MakeMovie(90, "Liked drama", ["Drama"], review: Review.Liked),
            MakeMovie(2, "Liked drama too", ["Drama"], review: Review.Liked));

        // act
        var result = await harness.HandleAsync(Profile());

        // assert
        result.Select(item => item.Summary.Id.Value).ShouldContain(2);
    }

    [Fact]
    public async Task ForYou_ClearedReview_RemovesThePreviousSeed()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(
            MakeMovie(90, "Liked drama", ["Drama"], review: Review.Liked),
            MakeMovie(2, "Similar drama", ["Drama"]));
        var liked = await harness.HandleAsync(Profile());

        // act
        harness.Arrange(MakeMovie(2, "Similar drama", ["Drama"]));
        var cleared = await harness.HandleAsync(Profile());

        // assert
        liked.Select(item => item.Summary.Id.Value).ShouldBe([2]);
        cleared.ShouldBeEmpty();
    }

    [Fact]
    public async Task ForYou_OppositeSwitch_ReplacesTheLikedSeedWithADislikedPenalty()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(
            MakeMovie(90, "First drama", ["Drama"], review: Review.Liked),
            MakeMovie(91, "Second drama", ["Drama"], review: Review.Liked),
            MakeMovie(2, "Similar drama", ["Drama"]));
        var liked = await harness.HandleAsync(Profile());

        // act
        harness.Arrange(
            MakeMovie(90, "First drama", ["Drama"], review: Review.Disliked),
            MakeMovie(91, "Second drama", ["Drama"], review: Review.Liked),
            MakeMovie(2, "Similar drama", ["Drama"]));
        var switched = await harness.HandleAsync(Profile());

        // assert
        switched.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeLessThan(liked.Single(item => item.Summary.Id.Value == 2).Score);
    }

    [Fact]
    public async Task ForYou_LikedSeed_EmitsTruthfulLikedChip()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(
            MakeMovie(90, "Alien", ["Drama"], review: Review.Liked),
            MakeMovie(2, "Similar drama", ["Drama"]));

        // act
        var result = await harness.HandleAsync(Profile());

        // assert
        result.Single(item => item.Summary.Id.Value == 2).Seed
            .ShouldBe(new RecommendationSeed(90, "Alien", SeedSignal.Liked));
    }

    [Fact]
    public async Task ForYou_NegativeOnlyLibrary_EmitsNoLikedChip()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(
            MakeMovie(90, "Watched drama", ["Drama"], isWatched: true),
            MakeMovie(95, "Disliked comedy", ["Comedy"], review: Review.Disliked),
            MakeMovie(2, "Similar drama", ["Drama"]));

        // act
        var result = await harness.HandleAsync(Profile());

        // assert
        result.Single(item => item.Summary.Id.Value == 2).Seed
            .ShouldBe(new RecommendationSeed(90, "Watched drama", SeedSignal.WatchedWithoutReview));
    }

    [Fact(DisplayName = "SMOKE-003 For You after a like ranks similar candidates higher without fabricated reasons")]
    public async Task ForYou_LikedSeed_RanksSimilarHigherWithoutFabricatedReasons()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(
            MakeMovie(90, "Watched drama", ["Drama"], isWatched: true),
            MakeMovie(2, "Similar drama", ["Drama"]),
            MakeMovie(3, "Drama comedy", ["Drama", "Comedy"]));
        var baseline = await harness.HandleAsync(Profile());

        // act
        harness.Arrange(
            MakeMovie(90, "Watched drama", ["Drama"], isWatched: true),
            MakeMovie(91, "Alien", ["Drama"], review: Review.Liked),
            MakeMovie(2, "Similar drama", ["Drama"]),
            MakeMovie(3, "Drama comedy", ["Drama", "Comedy"]));
        var liked = await harness.HandleAsync(Profile());

        // assert
        liked.Single(item => item.Summary.Id.Value == 2).Score
            .ShouldBeGreaterThan(baseline.Single(item => item.Summary.Id.Value == 2).Score);
        liked.Select(item => item.Summary.Id.Value).First().ShouldBe(2);
        liked.Single(item => item.Summary.Id.Value == 3).Seed
            .ShouldBe(new RecommendationSeed(91, "Alien", SeedSignal.Liked));
    }

    [Fact]
    public async Task Related_LikedProfile_LeavesSimilarCandidateScoreUnchanged()
    {
        // arrange
        var harness = new RelatedHarness();
        var similar = MakeMovie(2, "Similar drama", ["Drama"], directors: ["Director A"]);
        var other = MakeMovie(3, "Other comedy", ["Comedy"], directors: ["Director B"]);
        harness.Arrange(similar, other);

        var baseline = await harness.HandleAsync(Profile());
        var liked = await harness.HandleAsync(Profile(genres: ["Drama"]));

        // assert
        liked.Select(item => item.Summary.Id).ShouldBe(baseline.Select(item => item.Summary.Id));
        liked.Select(item => item.Score).ShouldBe(baseline.Select(item => item.Score));
    }

    [Fact]
    public async Task Related_DislikedProfile_LeavesSimilarCandidateScoreUnchanged()
    {
        // arrange
        var harness = new RelatedHarness();
        var similar = MakeMovie(2, "Similar drama", ["Drama"], directors: ["Director A"]);
        var other = MakeMovie(3, "Other comedy", ["Comedy"], directors: ["Director B"]);
        harness.Arrange(similar, other);

        var baseline = await harness.HandleAsync(Profile());
        var disliked = await harness.HandleAsync(Profile(genres: ["Comedy"]));

        // assert
        disliked.Select(item => item.Summary.Id).ShouldBe(baseline.Select(item => item.Summary.Id));
        disliked.Select(item => item.Score).ShouldBe(baseline.Select(item => item.Score));
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
    public async Task Related_LikedProfile_EmitsNoLikedChip()
    {
        // arrange
        var harness = new RelatedHarness();
        harness.Arrange(MakeMovie(2, "Similar drama", ["Drama"]));

        // act
        var result = await harness.HandleAsync(Profile(genres: ["Drama"]));

        // assert
        result.Single().Seed.ShouldBeNull();
        result.Single().Evidence.ShouldAllBe(evidence => !evidence.Text.StartsWith("Matches your", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Related_NegativeOnlyProfile_EmitsNoLikedChip()
    {
        // arrange
        var harness = new RelatedHarness();
        harness.Arrange(MakeMovie(2, "Similar drama", ["Drama"]));

        // act
        var result = await harness.HandleAsync(Profile(genres: ["Comedy"]));

        // assert
        result.Single().Seed.ShouldBeNull();
    }

    [Fact]
    public async Task ForYou_BestSeedNotLiked_EmitsNoLikedChip()
    {
        // arrange
        var harness = new ForYouHarness();
        harness.Arrange(
            MakeMovie(90, "Watched drama", ["Drama"], isWatched: true),
            MakeMovie(2, "Similar drama", ["Drama"]));

        // act
        var result = await harness.HandleAsync(Profile());

        // assert
        result.Single(item => item.Summary.Id.Value == 2).Seed
            .ShouldBe(new RecommendationSeed(90, "Watched drama", SeedSignal.WatchedWithoutReview));
    }

    private static TasteProfile Profile(string[]? genres = null) =>
        new(Preferences(genres ?? [], Weights.GetWeight));

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
        private readonly IRecommendationCandidateSource candidates = Substitute.For<IRecommendationCandidateSource>();
        private readonly IRecommendationProfileSource profileSource = Substitute.For<IRecommendationProfileSource>();

        public ForYouHarness()
        {
            catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Counts);
            catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>()).Returns(EmptyKeywordCounts);
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

            IReadOnlyList<RecommendationLibraryEntry> entries =
            [
                .. library.Select((movie, index) => new RecommendationLibraryEntry(
                    MovieFeaturesMapper.From(movie, includeSignals: true),
                    summaries[index])),
            ];

            candidates.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new RecommendationLibrarySnapshot(entries, CrowdLinks.Empty)));
        }

        public async Task<ImmutableList<RecommendedMovie>> HandleAsync(
            TasteProfile profile,
            Func<RecommendationOptions, RecommendationOptions>? configure = null)
        {
            profileSource.GetTasteProfileAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(profile with
                {
                    PlaybackByMovieId = ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>
                        .Empty.Add(new MovieId(500), [Now]),
                }));
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
                candidates,
                profileSource,
                new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
                new KeywordWeightsCalculator(catalog, new KeywordWeightsCache()),
                new FixedTimeProvider(Now),
                Microsoft.Extensions.Options.Options.Create(options));

            return (await handler.HandleAsync(new GetForYouRecommendationsQuery(), CancellationToken.None)).Items;
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
            catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>()).Returns(EmptyKeywordCounts);
            movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Movie?>(selected));
        }

        public void Arrange(params Movie[] pool)
        {
            var library = new List<RecommendationLibraryEntry> { Entry(selected, UnitVector()) };
            library.AddRange(pool.Select(movie => Entry(movie)));
            candidates.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>())
                .Returns(new RecommendationLibrarySnapshot(library, CrowdLinks.Empty));
        }

        private static RecommendationLibraryEntry Entry(Movie movie, ImmutableArray<float>? plotEmbedding = null) =>
            new(
                MovieFeaturesMapper.From(
                    movie,
                    includeSignals: true,
                    plotEmbedding: plotEmbedding is { IsDefaultOrEmpty: false } embedding ? embedding : null),
                MovieSummaryMapper.From(movie));

        private static ImmutableArray<float> UnitVector()
        {
            var builder = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);
            for (var index = 0; index < EmbeddingDimensions.Value; index++)
            {
                builder.Add(index == 0 ? 1.0f : 0.0f);
            }

            return builder.MoveToImmutable();
        }

        public async Task<ImmutableList<RecommendedMovie>> HandleAsync(TasteProfile profile)
        {
            profileSource.GetTasteProfileAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(profile with
                {
                    PlaybackByMovieId = ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>
                        .Empty.Add(new MovieId(500), [Now]),
                }));
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
                new KeywordWeightsCalculator(catalog, new KeywordWeightsCache()),
                Microsoft.Extensions.Options.Options.Create(new RecommendationOptions
                {
                    // Legacy review-ranking fixtures are not about strong-match eligibility.
                    MinimumSimilarity = 0.0,
                    Weights = GenreOnlyWeights,
                    MmrLambda = 1.0,
                }));

            return await handler.HandleAsync(new GetRecommendationsQuery(selected.Id), CancellationToken.None);
        }
    }
}


