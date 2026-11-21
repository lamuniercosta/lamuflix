using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class NearestSeedScorerTests
{
    private const double Tolerance = 1e-6;

    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
            ["Horror"] = 5,
        });

    private static readonly SimilarityWeights PlotOnlyWeights = new()
    {
        Director = 0.0,
        Themes = 0.0,
        Plot = 1.0,
        Cast = 0.0,
        Genres = 0.0,
        Collection = 0.0,
    };

    private static readonly SimilarityWeights GenreOnlyWeights = new()
    {
        Director = 0.0,
        Themes = 0.0,
        Plot = 0.0,
        Cast = 0.0,
        Genres = 3.0,
        Collection = 0.0,
    };

    private static readonly IReadOnlyDictionary<string, double> NoKeywords =
        new Dictionary<string, double>();

    [Fact]
    public void Rank_LikedSeed_ProducesWeightedSimilarityAndLikedSignal()
    {
        // arrange
        var seed = Entry(90, Features("Drama"), liked: true, title: "Alien");
        var candidate = Entry(2, Features("Drama"));
        var options = PlotOptions(weights: GenreOnlyWeights);

        // act
        var result = Rank([seed, candidate], [candidate], Profile(), options);

        // assert
        var match = result.ShouldHaveSingleItem();
        match.Score.ShouldBe(1.0, Tolerance);
        match.Signal.ShouldBe(SeedSignal.Liked);
        match.SeedMovieId.ShouldBe(seed.Id);
        match.SeedTitle.ShouldBe("Alien");
    }

    [Fact]
    public void Rank_WatchedAndLikedMovie_CountsOnceAsLiked()
    {
        // arrange
        var seed = Entry(90, Features("Drama"), watched: true, liked: true, watchlist: true);
        var candidate = Entry(2, Features("Drama"));
        var options = PlotOptions(weights: GenreOnlyWeights);

        // act
        var result = Rank([seed, candidate], [candidate], Profile(), options);

        // assert
        var match = result.ShouldHaveSingleItem();
        match.Signal.ShouldBe(SeedSignal.Liked);
        match.Score.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void Rank_WatchedWithoutReviewSeed_UsesWatchedWeight()
    {
        // arrange
        var seed = Entry(90, Features("Drama"), watched: true);
        var candidate = Entry(2, Features("Drama"));
        var options = PlotOptions(weights: GenreOnlyWeights);

        // act
        var result = Rank([seed, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(0.6, Tolerance);
    }

    [Fact]
    public void Rank_WatchlistOnlySeed_UsesWatchlistWeight()
    {
        // arrange
        var seed = Entry(90, Features("Drama"), watchlist: true);
        var candidate = Entry(2, Features("Drama"));
        var options = PlotOptions(weights: GenreOnlyWeights);

        // act
        var result = Rank([seed, candidate], [candidate], Profile(), options);

        // assert
        var match = result.ShouldHaveSingleItem();
        match.Signal.ShouldBe(SeedSignal.Watchlist);
        match.Score.ShouldBe(0.4, Tolerance);
    }

    [Fact]
    public void Rank_WatchlistedCandidate_DoesNotMatchItself()
    {
        // arrange
        var candidate = Entry(2, Features("Drama"), watchlist: true);
        var options = PlotOptions();

        // act
        var result = Rank([candidate], [candidate], Profile(), options);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Rank_NoPositiveSeeds_ReturnsEmpty()
    {
        // arrange
        var candidate = Entry(2, Features("Drama"));
        var options = PlotOptions();

        // act
        var result = Rank([candidate], [candidate], Profile(), options);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Rank_DislikedSeed_ContributesOnlyThePenalty()
    {
        // arrange
        var liked = Entry(90, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var disliked = Entry(95, Features(["Drama"], vector: Vector(1.0)), disliked: true, watchlist: true);
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var options = PlotOptions();

        // act
        var result = Rank([liked, disliked, candidate], [candidate], Profile(), options);

        // assert
        var match = result.ShouldHaveSingleItem();
        match.SeedMovieId.ShouldBe(liked.Id);
        match.Score.ShouldBe(0.0, Tolerance);
    }

    [Fact]
    public void Rank_ConfiguredDislikedWeight_ScalesThePenalty()
    {
        // arrange
        var liked = Entry(90, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var disliked = Entry(95, Features(["Drama"], vector: Vector(1.0)), disliked: true);
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var options = PlotOptions(dislikedWeight: 2.0);

        // act
        var result = Rank([liked, disliked, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(-1.0, Tolerance);
    }

    [Fact]
    public void Rank_NoDislikedSeeds_AddsNoPenalty()
    {
        // arrange
        var liked = Entry(90, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var options = PlotOptions();

        // act
        var result = Rank([liked, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void Rank_TopThreeProducts_UseDescendingRankMultipliers()
    {
        // arrange
        var seeds = Enumerable.Range(90, 4)
            .Select(id => Entry(id, Features(["Drama"], vector: Vector(1.0)), liked: true))
            .ToArray();
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var options = PlotOptions();

        // act
        var result = Rank([.. seeds, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(1.75, Tolerance);
    }

    [Fact]
    public void Rank_FewerThanThreeProducts_UseLeadingMultipliers()
    {
        // arrange
        var seeds = Enumerable.Range(90, 2)
            .Select(id => Entry(id, Features(["Drama"], vector: Vector(1.0)), liked: true))
            .ToArray();
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var options = PlotOptions();

        // act
        var result = Rank([.. seeds, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(1.5, Tolerance);
    }

    [Fact]
    public void Rank_ConfiguredRankMultipliers_ReplaceTheDefaults()
    {
        // arrange
        var seeds = Enumerable.Range(90, 3)
            .Select(id => Entry(id, Features(["Drama"], vector: Vector(1.0)), liked: true))
            .ToArray();
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var options = PlotOptions(multipliers: [1.0, 1.0, 1.0]);

        // act
        var result = Rank([.. seeds, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(3.0, Tolerance);
    }

    [Fact]
    public void Rank_InvalidOptions_ThrowsBeforeScoring()
    {
        // arrange
        var seed = Entry(90, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var options = PlotOptions(likedWeight: -1.0);

        // act
        var act = () => Rank([seed, candidate], [candidate], Profile(), options);

        // assert
        Should.Throw<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Rank_ConfiguredSeedWeight_ChangesTheAggregate()
    {
        // arrange
        var seed = Entry(90, Features("Drama"), liked: true);
        var candidate = Entry(2, Features("Drama"));
        var options = PlotOptions(likedWeight: 2.0, weights: GenreOnlyWeights);

        // act
        var result = Rank([seed, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(2.0, Tolerance);
    }

    [Fact]
    public void Rank_DirectorOnlyStrongMatch_QualifiesBelowTheBar()
    {
        // arrange
        var weights = new SimilarityWeights
        {
            Director = 3.0,
            Themes = 0.0,
            Plot = 0.0,
            Cast = 0.0,
            Genres = 12.0,
            Collection = 0.0,
        };
        var seed = Entry(90, Features(["Comedy"], directors: ["Ridley Scott"]), liked: true);
        var candidate = Entry(2, Features(["Drama"], directors: ["Ridley Scott"]));
        var options = PlotOptions(weights: weights);

        // act
        var result = Rank([seed, candidate], [candidate], Profile(), options);

        // assert
        var match = result.ShouldHaveSingleItem();
        match.Score.ShouldBe(0.2, Tolerance);
        match.SeedMovieId.ShouldBe(seed.Id);
    }

    [Fact]
    public void Rank_WeakAggregateOfSubBarPairs_IsRejected()
    {
        // arrange
        var seeds = Enumerable.Range(90, 3)
            .Select(id => Entry(id, Features(["Drama"], vector: VectorAtSimilarity(0.2)), liked: true))
            .ToArray();
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var options = PlotOptions();

        // act
        var result = Rank([.. seeds, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Rank_PairAtTheConfiguredBar_Qualifies()
    {
        // arrange
        var seed = Entry(90, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var options = PlotOptions(minimumSimilarity: 1.0);

        // act
        var result = Rank([seed, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void Rank_PairBelowTheConfiguredBar_IsRejected()
    {
        // arrange
        var seed = Entry(90, Features(["Drama"], vector: VectorAtSimilarity(0.9)), liked: true);
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var options = PlotOptions(minimumSimilarity: 1.0);

        // act
        var result = Rank([seed, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Rank_OldSeedWithPlayback_AppliesTheRecencyFloor()
    {
        // arrange
        var fresh = Entry(90, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var old = Entry(91, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var playback = Playback(
            (fresh.Id, Now.AddDays(-1)),
            (old.Id, Now.AddDays(-400)));

        // act
        var result = Rank([fresh, old, candidate], [candidate], Profile(playback), PlotOptions());

        // assert
        var match = result.ShouldHaveSingleItem();
        match.Score.ShouldBe(1.125, Tolerance);
        match.SeedMovieId.ShouldBe(fresh.Id);
    }

    [Fact]
    public void Rank_SeedWithoutPlayback_StaysUnscaled()
    {
        // arrange
        var played = Entry(90, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var unplayed = Entry(91, Features(["Drama"], vector: Vector(1.0)), watchlist: true);
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var playback = Playback((played.Id, Now.AddDays(-1)));

        // act
        var result = Rank([played, unplayed, candidate], [candidate], Profile(playback), PlotOptions());

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(1.2, Tolerance);
    }

    [Fact]
    public void Rank_DislikedPenalty_IsNotRecencyScaled()
    {
        // arrange
        var liked = Entry(90, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var disliked = Entry(95, Features(["Drama"], vector: Vector(1.0)), disliked: true);
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));
        var playback = Playback(
            (liked.Id, Now.AddDays(-1)),
            (disliked.Id, Now.AddDays(-400)));

        // act
        var result = Rank([liked, disliked, candidate], [candidate], Profile(playback), PlotOptions());

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(0.0, Tolerance);
    }

    [Fact]
    public void Rank_EquallyWeightedStrongSeeds_BreakTiesByAscendingMovieId()
    {
        // arrange
        var higher = Entry(91, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var lower = Entry(90, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var candidate = Entry(2, Features(["Drama"], vector: Vector(1.0)));

        // act
        var result = Rank([higher, lower, candidate], [candidate], Profile(), PlotOptions());

        // assert
        result.ShouldHaveSingleItem().SeedMovieId.ShouldBe(lower.Id);
    }

    [Fact]
    public void Rank_BestStrongSeedOutsideTopThreeProducts_IsStillNamed()
    {
        // arrange
        var weakSeeds = Enumerable.Range(10, 3)
            .Select(id => Entry(id, Features(["Drama"], vector: Vector(0.0, 1.0)), liked: true))
            .ToArray();
        var strongSeed = Entry(
            13,
            Features(["Comedy"], directors: ["Ridley Scott"], vector: Vector(0.0, 1.0)),
            liked: true,
            title: "Old Director");
        var candidate = Entry(2, Features(["Drama"], directors: ["Ridley Scott"], vector: Vector(1.0)));
        var playback = Playback(
            (weakSeeds[0].Id, Now.AddDays(-1)),
            (strongSeed.Id, Now.AddDays(-400)));
        var options = PlotOptions(weights: DirectorGenreAndPlotWeights);

        // act
        var result = Rank(
            [.. weakSeeds, strongSeed, candidate],
            [candidate],
            Profile(playback),
            options);

        // assert
        var match = result.ShouldHaveSingleItem();
        match.SeedMovieId.ShouldBe(strongSeed.Id);
        match.SeedTitle.ShouldBe("Old Director");
        match.Score.ShouldBe(0.23333333333333334, Tolerance);
    }

    [Fact]
    public void Rank_NonFiniteEmbedding_IsTreatedAsMissingPlotFeature()
    {
        // arrange
        var seed = Entry(90, Features(["Comedy"], directors: ["Ridley Scott"], vector: Vector(double.NaN)), liked: true);
        var candidate = Entry(2, Features(["Drama"], directors: ["Ridley Scott"], vector: Vector(double.NaN)));
        var options = PlotOptions(weights: DirectorAndGenreWeights);

        // act
        var result = Rank([seed, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(0.2, Tolerance);
    }

    [Fact]
    public void Rank_MissingFeatures_ProducesNoAdmittedPairs()
    {
        // arrange
        var seed = Entry(90, new MovieFeatures(), liked: true);
        var candidate = Entry(2, new MovieFeatures());
        var options = PlotOptions();

        // act
        var result = Rank([seed, candidate], [candidate], Profile(), options);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Rank_TiedCandidateScores_OrderByAscendingMovieId()
    {
        // arrange
        var seed = Entry(90, Features(["Drama"], vector: Vector(1.0)), liked: true);
        var higher = Entry(5, Features(["Drama"], vector: Vector(1.0)));
        var lower = Entry(3, Features(["Drama"], vector: Vector(1.0)));

        // act
        var result = Rank([seed, higher, lower], [higher, lower], Profile(), PlotOptions());

        // assert
        result.Select(match => match.Candidate.Id.Value).ShouldBe([3, 5]);
    }

    private static SimilarityWeights DirectorGenreAndPlotWeights => new()
    {
        Director = 3.0,
        Themes = 0.0,
        Plot = 10.0,
        Cast = 0.0,
        Genres = 2.0,
        Collection = 0.0,
    };

    private static SimilarityWeights DirectorAndGenreWeights => new()
    {
        Director = 3.0,
        Themes = 0.0,
        Plot = 0.0,
        Cast = 0.0,
        Genres = 12.0,
        Collection = 0.0,
    };

    private static RecommendationOptions PlotOptions(
        IReadOnlyList<double>? multipliers = null,
        double likedWeight = 1.0,
        double watchedWeight = 0.6,
        double watchlistWeight = 0.4,
        double dislikedWeight = 1.0,
        double minimumSimilarity = 0.225,
        SimilarityWeights? weights = null) =>
        new()
        {
            Weights = weights ?? PlotOnlyWeights,
            SeedRankMultipliers = multipliers ?? [1.0, 0.5, 0.25],
            SeedWeightLiked = likedWeight,
            SeedWeightWatched = watchedWeight,
            SeedWeightWatchlist = watchlistWeight,
            DislikedSeedWeight = dislikedWeight,
            MinimumSimilarity = minimumSimilarity,
        };

    private static ImmutableList<NearestSeedMatch> Rank(
        IReadOnlyList<RecommendationLibraryEntry> library,
        IReadOnlyList<RecommendationLibraryEntry> candidates,
        TasteProfile profile,
        RecommendationOptions options)
    {
        var genreWeights = GenreWeights.Calculate(Counts);
        var decay = new RecommendationRecencyDecay(new FixedTimeProvider(Now), options);

        return NearestSeedScorer.Rank(
            candidates,
            library,
            profile,
            options,
            decay,
            genreWeights,
            NoKeywords,
            CancellationToken.None);
    }

    private static TasteProfile Profile(
        ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>? playback = null) =>
        new(ImmutableDictionary<string, double>.Empty)
        {
            PlaybackByMovieId = playback
                ?? ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>.Empty,
        };

    private static ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>> Playback(
        params (MovieId MovieId, DateTimeOffset PlayedAt)[] entries) =>
        entries.ToImmutableDictionary(entry => entry.MovieId, entry => ImmutableArray.Create(entry.PlayedAt));

    private static RecommendationLibraryEntry Entry(
        int id,
        MovieFeatures features,
        bool watched = false,
        bool liked = false,
        bool watchlist = false,
        bool disliked = false,
        string? title = null) =>
        new(
            features,
            new MovieSummary(
                new MovieId(id),
                title ?? $"Movie {id}",
                null,
                null,
                null,
                null,
                EnrichmentStatus.Enriched,
                watchlist,
                watched,
                disliked ? Review.Disliked : liked ? Review.Liked : Review.None));

    private static MovieFeatures Features(
        string[] genres,
        string[]? directors = null,
        ImmutableArray<float>? vector = null) =>
        new(
            genres: genres,
            directors: directors,
            plotEmbedding: vector is { IsDefaultOrEmpty: false } embedding ? embedding : null);

    private static MovieFeatures Features(string genre) => Features([genre]);

    private static ImmutableArray<float> Vector(double primary, double secondary = 0.0)
    {
        var builder = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);
        builder.Add((float)primary);
        builder.Add((float)secondary);

        for (var index = 2; index < EmbeddingDimensions.Value; index++)
        {
            builder.Add(0.0f);
        }

        return builder.MoveToImmutable();
    }

    private static ImmutableArray<float> VectorAtSimilarity(double similarity)
    {
        var cosine = 0.16 + (similarity * 0.29);
        return Vector(cosine, Math.Sqrt(1.0 - (cosine * cosine)));
    }
}
