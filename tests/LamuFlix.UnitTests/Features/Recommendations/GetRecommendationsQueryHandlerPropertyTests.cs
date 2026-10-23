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

public sealed class GetRecommendationsQueryHandlerPropertyTests
{
    private const int SelectedMovieId = 1;
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly GenreCounts Counts = new(
        100,
        new Dictionary<string, long>
        {
            ["Drama"] = 10,
            ["Comedy"] = 20,
            ["Horror"] = 5,
        });

    [Property(MaxTest = 100, Replay = "340101,100003")]
    [Trait("Category", "Property")]
    public bool HandleAsync_GeneratedPool_OrdersByScoreDescendingThenIdAscending(int[] ids, int[] seeds, int takeSeed)
    {
        // arrange
        var pool = BuildPool(ids, seeds);
        var take = Take(takeSeed);

        // act
        var result = Handle(pool, take);

        // assert
        return IsOrdered(result);
    }

    [Property(MaxTest = 100, Replay = "340102,200009")]
    [Trait("Category", "Property")]
    public bool HandleAsync_GeneratedPool_CountIsMinOfTakeAndPoolSize(int[] ids, int[] seeds, int takeSeed)
    {
        // arrange
        var pool = BuildPool(ids, seeds);
        var take = Take(takeSeed);

        // act
        var result = Handle(pool, take);

        // assert
        return result.Count == Math.Min(take, pool.Length);
    }

    [Property(MaxTest = 100, Replay = "340103,300011")]
    [Trait("Category", "Property")]
    public bool HandleAsync_PermutedPool_ReturnsIdenticalOrdering(int[] ids, int[] seeds, int takeSeed)
    {
        // arrange
        var pool = BuildPool(ids, seeds);
        var take = Take(takeSeed);

        // act
        var forward = Handle(pool, take);
        var reversed = Handle([.. pool.Reverse()], take);
        var sorted = Handle([.. pool.OrderByDescending(movie => movie.Id.Value)], take);

        // assert
        return SameSequence(forward, reversed) && SameSequence(forward, sorted);
    }

    [Property(MaxTest = 100, Replay = "340104,400017")]
    [Trait("Category", "Property")]
    public bool HandleAsync_StoryReasonPresentIffCosineStrictlyAboveThreshold(
        int[] ids,
        int[] cosineSeeds,
        int thresholdSeed,
        int takeSeed)
    {
        // arrange
        var pool = BuildCandidates(ids, cosineSeeds);
        var threshold = SyntheticCosine(thresholdSeed);

        // act
        var result = Handle(pool, Take(takeSeed), threshold);

        // assert
        var cosines = pool.ToDictionary(candidate => candidate.Movie.Id.Value, candidate => candidate.CosineSimilarity);
        return result.All(item => HasStoryReason(item) == (cosines[item.Summary.Id.Value] > threshold));
    }

    [Property(MaxTest = 100, Replay = "340105,500021")]
    [Trait("Category", "Property")]
    public bool HandleAsync_ReasonsAreScorerPrefixThenAtMostOneStorySuffix(
        int[] ids,
        int[] cosineSeeds,
        int thresholdSeed,
        int takeSeed)
    {
        // arrange
        var pool = BuildCandidates(ids, cosineSeeds);
        var threshold = SyntheticCosine(thresholdSeed);
        var byId = pool.ToDictionary(candidate => candidate.Movie.Id.Value);
        var genreWeights = GenreWeights.Calculate(Counts);

        // act
        var result = Handle(pool, Take(takeSeed), threshold);

        // assert
        foreach (var item in result)
        {
            if (!ReasonsMatch(item, byId, genreWeights))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ReasonsMatch(
        RecommendedMovie item,
        IReadOnlyDictionary<int, RecommendationCandidate> byId,
        GenreWeights genreWeights)
    {
        var candidate = byId[item.Summary.Id.Value];
        var expected = SimilarityScorer.Score(
            MovieFeaturesMapper.From(SelectedMovie()),
            MovieFeaturesMapper.From(candidate.Movie),
            new RecommendationOptions().Weights,
            genreWeights);
        var story = HasStoryReason(item);
        return HasExpectedCount(item, expected, story)
            && PrefixMatches(item, expected)
            && SuffixMatches(item, story);
    }

    private static bool HasExpectedCount(RecommendedMovie item, SimilarityResult expected, bool story) =>
        item.Reasons.Count == expected.Reasons.Count + (story ? 1 : 0);

    private static bool PrefixMatches(RecommendedMovie item, SimilarityResult expected)
    {
        for (var index = 0; index < expected.Reasons.Count; index++)
        {
            if (item.Reasons[index].Text != expected.Reasons[index].Text)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SuffixMatches(RecommendedMovie item, bool story)
    {
        if (story)
        {
            return item.Reasons.Count != 0 && item.Reasons[^1].Text == StoryReasonText;
        }

        return !HasStoryReason(item);
    }

    [Property(MaxTest = 100, Replay = "340106,600029")]
    [Trait("Category", "Property")]
    public bool HandleAsync_StorySuffix_DoesNotChangeRetentionScoreOrOrder(
        int[] ids,
        int[] cosineSeeds,
        int thresholdSeed,
        int takeSeed)
    {
        // arrange
        var pool = BuildCandidates(ids, cosineSeeds);
        var take = Take(takeSeed);

        // act
        var withSuffix = Handle(pool, take, SyntheticCosine(thresholdSeed));
        var withoutSuffix = Handle(pool, take, 1.0);

        // assert
        return withSuffix.Count == Math.Min(take, pool.Length)
            && SameIgnoringStorySuffix(withSuffix, withoutSuffix);
    }

    [Property(MaxTest = 100, Replay = "340107,700031")]
    [Trait("Category", "Property")]
    public bool HandleAsync_CosineEqualToThreshold_EmitsNoStoryReason(int[] ids, int[] cosineSeeds, int takeSeed)
    {
        // arrange
        var pool = BuildCandidates(ids, cosineSeeds);
        if (pool.Length == 0)
        {
            return true;
        }

        var subject = pool[0];

        // act
        var result = Handle(pool, Take(takeSeed), subject.CosineSimilarity);

        // assert
        var match = result.FirstOrDefault(item => item.Summary.Id.Value == subject.Movie.Id.Value);
        return match is null || !HasStoryReason(match);
    }

    [Property(MaxTest = 50, Replay = "340108,800033")]
    [Trait("Category", "Property")]
    public bool HandleAsync_NamedSyntheticSet_DefaultThresholdEmitsOnlyAbove(int seed)
    {
        // arrange
        var cosines = NamedCosines();
        var pool = NamedCandidates(cosines, seed);

        // act
        var result = Handle(pool, pool.Length, new RecommendationOptions().SimilarStoryThreshold);

        // assert
        return result.Count == pool.Length
            && result.All(item => HasStoryReason(item) == (CosinesById(pool)[item.Summary.Id.Value] > 0.5));
    }

    [Property(MaxTest = 100, Replay = "340109,900037")]
    [Trait("Category", "Property")]
    public bool HandleAsync_NamedSyntheticSet_ThresholdOverrideEmitsStrictlyAbove(int thresholdSeed, int takeSeed)
    {
        // arrange
        var cosines = NamedCosines();
        var pool = NamedCandidates(cosines, thresholdSeed);
        var threshold = SyntheticCosine(thresholdSeed);

        // act
        var result = Handle(pool, Take(takeSeed), threshold);

        // assert
        var byId = CosinesById(pool);
        return result.All(item => HasStoryReason(item) == (byId[item.Summary.Id.Value] > threshold));
    }

    [Property(MaxTest = 100, Replay = "340110,1100041")]
    [Trait("Category", "Property")]
    public bool HandleAsync_NotInterestedCandidates_AreNeverScoredOrReturned(
        int[] ids,
        int[] cosineSeeds,
        bool[] notInterested,
        int takeSeed)
    {
        // arrange
        var pool = BuildCandidates(ids, cosineSeeds, notInterested);
        var take = Take(takeSeed);
        var expectedCount = Math.Min(take, pool.Count(candidate => !candidate.Movie.IsNotInterested));

        // act
        var result = Handle(pool, take);

        // assert
        return result.Count == expectedCount
            && result.All(item => !pool.Any(candidate =>
                candidate.Movie.IsNotInterested
                && candidate.Movie.Id.Value == item.Summary.Id.Value));
    }

    [Property(MaxTest = 100, Replay = "340111,1200043")]
    [Trait("Category", "Property")]
    public bool HandleAsync_PositiveNotInterestedPenalty_NeverSubstitutesForExclusion(
        int[] ids,
        int[] cosineSeeds,
        bool[] notInterested,
        int penaltySeed,
        int takeSeed)
    {
        // arrange
        var pool = BuildCandidates(ids, cosineSeeds, notInterested);
        var take = Take(takeSeed);
        var penalty = Math.Abs((long)penaltySeed % 1000) / 10.0;
        var options = new RecommendationOptions { NotInterestedPenalty = penalty };
        var expectedCount = Math.Min(take, pool.Count(candidate => !candidate.Movie.IsNotInterested));

        // act
        var result = Handle(pool, take, options);

        // assert
        return result.Count == expectedCount
            && result.All(item => !pool.Any(candidate =>
                candidate.Movie.IsNotInterested
                && candidate.Movie.Id.Value == item.Summary.Id.Value));
    }

    private static ImmutableList<RecommendedMovie> Handle(IReadOnlyList<Movie> pool, int take) =>
        Handle([.. pool.Select((movie, index) => new RecommendationCandidate(movie, Cosine(index)))], take);

    private static ImmutableList<RecommendedMovie> Handle(IReadOnlyList<RecommendationCandidate> pool, int take) =>
        Handle(pool, take, new RecommendationOptions());

    private static ImmutableList<RecommendedMovie> Handle(
        IReadOnlyList<RecommendationCandidate> pool,
        int take,
        double threshold) =>
        Handle(pool, take, new RecommendationOptions { SimilarStoryThreshold = threshold });

    private static ImmutableList<RecommendedMovie> Handle(
        IReadOnlyList<RecommendationCandidate> pool,
        int take,
        RecommendationOptions options)
    {
        var movies = Substitute.For<IMovieRepository>();
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(SelectedMovie());
        var source = Substitute.For<IRecommendationCandidateSource>();
        source.GetCandidatesAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns(pool);
        var catalog = Substitute.For<IMovieCatalog>();
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>()).Returns(Counts);
        var profileSource = Substitute.For<IRecommendationProfileSource>();
        profileSource
            .GetProfileInputsAsync(Arg.Any<IReadOnlyCollection<MovieId>>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new RecommendationProfileInputs(
                ImmutableArray<TasteProfileMovie>.Empty,
                ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty)));
        var handler = new GetRecommendationsQueryHandler(
            movies,
            source,
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(options));

        return handler
            .HandleAsync(new GetRecommendationsQuery(new MovieId(SelectedMovieId), take), CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    private static int Take(int takeSeed) => Math.Abs(takeSeed % 20) + 1;

    private const string StoryReasonText = "Similar story";

    private static double Cosine(int index) => (index % 21 - 10) / 10.0;

    private static double SyntheticCosine(int seed) => ((Math.Abs((long)seed) % 201) - 100) / 100.0;

    private static double[] NamedCosines() => [0.8, 0.6, 0.4, 0.0, -1.0];

    private static RecommendationCandidate[] NamedCandidates(double[] cosines, int seed)
    {
        var candidates = new RecommendationCandidate[cosines.Length];
        for (var index = 0; index < cosines.Length; index++)
        {
            candidates[index] = new RecommendationCandidate(BuildMovie(index + 2, seed + index), cosines[index]);
        }

        return candidates;
    }

    private static Dictionary<int, double> CosinesById(IReadOnlyList<RecommendationCandidate> pool) =>
        pool.ToDictionary(candidate => candidate.Movie.Id.Value, candidate => candidate.CosineSimilarity);

    private static RecommendationCandidate[] BuildCandidates(
        int[] ids,
        int[] cosineSeeds,
        bool[]? notInterested = null)
    {
        var distinct = ids
            .Select(value => Math.Abs(value % 900) + 2)
            .Distinct()
            .Take(12)
            .ToArray();

        var candidates = new RecommendationCandidate[distinct.Length];
        for (var index = 0; index < distinct.Length; index++)
        {
            var seed = cosineSeeds.Length == 0 ? distinct[index] : cosineSeeds[index % cosineSeeds.Length];
            var isNotInterested = notInterested is { Length: > 0 } && notInterested[index % notInterested.Length];
            candidates[index] = new RecommendationCandidate(
                BuildMovie(distinct[index], seed, isNotInterested),
                SyntheticCosine(seed));
        }

        return candidates;
    }

    private static bool HasStoryReason(RecommendedMovie movie) =>
        movie.Reasons.Any(reason => reason.Text == StoryReasonText);

    private static bool SameIgnoringStorySuffix(
        ImmutableList<RecommendedMovie> first,
        ImmutableList<RecommendedMovie> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (var index = 0; index < first.Count; index++)
        {
            if (first[index].Summary != second[index].Summary || !first[index].Score.Equals(second[index].Score))
            {
                return false;
            }

            var firstReasons = first[index].Reasons.Where(reason => reason.Text != StoryReasonText).Select(reason => reason.Text);
            var secondReasons = second[index].Reasons.Where(reason => reason.Text != StoryReasonText).Select(reason => reason.Text);
            if (!firstReasons.SequenceEqual(secondReasons, StringComparer.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static Movie[] BuildPool(int[] ids, int[] seeds)
    {
        var distinct = ids
            .Select(value => Math.Abs(value % 900) + 2)
            .Distinct()
            .Take(12)
            .ToArray();

        var pool = new Movie[distinct.Length];
        for (var index = 0; index < distinct.Length; index++)
        {
            var seed = seeds.Length == 0 ? distinct[index] : seeds[index % seeds.Length];
            pool[index] = BuildMovie(distinct[index], seed);
        }

        return pool;
    }

    private static Movie BuildMovie(int id, int seed, bool isNotInterested = false)
    {
        var magnitude = Math.Abs((long)seed);
        var title = $"Movie {id}";

        if (magnitude % 6 == 0)
        {
            return Movie.Rehydrate(
                new MovieId(id),
                title,
                new LibraryPath($"C:/library/{id}.mkv"),
                new MediaFormat("mkv"),
                false,
                null,
                EnrichmentStatus.Enriched,
                Now,
                1,
                null,
                null,
                isNotInterested: isNotInterested);
        }

        var metadata = new MovieMetadata(
            title,
            posterUrl: $"https://posters/{id}",
            releaseYear: new ReleaseYear(1990 + (int)(magnitude % 30), Now),
            runtime: new Runtime(80 + (int)(magnitude % 80)),
            imdbRating: new ImdbRating((magnitude % 101) / 10.0m),
            directors: [(magnitude & 1) == 0 ? "Director A" : "Director B"],
            actors: [(magnitude & 2) == 0 ? "Actor A" : "Actor B"],
            genres: Genres(magnitude),
            writers: [(magnitude & 4) == 0 ? "Writer A" : "Writer B"]);

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
            isNotInterested: isNotInterested);
    }

    private static string[] Genres(long magnitude)
    {
        var palette = new[] { "Drama", "Comedy", "Horror", "Action" };
        return [.. palette.Where((_, index) => ((magnitude >> index) & 1) == 1)];
    }

    private static Movie SelectedMovie() => Movie.Rehydrate(
        new MovieId(SelectedMovieId),
        "Selected",
        new LibraryPath("C:/library/selected.mkv"),
        new MediaFormat("mkv"),
        false,
        new MovieMetadata(
            "Selected",
            releaseYear: new ReleaseYear(2000, Now),
            runtime: new Runtime(120),
            imdbRating: new ImdbRating(8.0m),
            directors: ["Director A"],
            actors: ["Actor A"],
            genres: ["Drama", "Action"],
            writers: ["Writer A"]),
        EnrichmentStatus.Enriched,
        Now,
        1,
        null,
        null);

    private static bool IsOrdered(ImmutableList<RecommendedMovie> result)
    {
        for (var index = 1; index < result.Count; index++)
        {
            var previous = result[index - 1];
            var current = result[index];

            if (previous.Score < current.Score)
            {
                return false;
            }

            if (previous.Score.Equals(current.Score) && previous.Summary.Id.Value > current.Summary.Id.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameSequence(ImmutableList<RecommendedMovie> first, ImmutableList<RecommendedMovie> second)
    {
        if (first.Count != second.Count)
        {
            return false;
        }

        for (var index = 0; index < first.Count; index++)
        {
            if (!SameRecommendation(first[index], second[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SameRecommendation(RecommendedMovie first, RecommendedMovie second) =>
        first.Summary == second.Summary
        && first.Score.Equals(second.Score)
        && first.Reasons.Select(reason => reason.Text)
            .SequenceEqual(second.Reasons.Select(reason => reason.Text), StringComparer.Ordinal);
}
