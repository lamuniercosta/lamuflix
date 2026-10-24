using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class MmrSelectorPropertyTests
{
    private static readonly string[] GenreTokens = ["Drama", "Comedy", "Noir", "Sci-Fi", "Doc"];

    private static readonly SimilarityWeights Weights = new()
    {
        Director = 1.0,
        Writers = 0.0,
        Cast = 0.75,
        Genres = 1.5,
        YearProximity = 0.0,
        RuntimeProximity = 0.0,
        RatingBoost = 0.0,
    };

    private static readonly GenreWeights GenreTable = GenreWeights.Calculate(
        new GenreCounts(
            120,
            new Dictionary<string, long>
            {
                ["Drama"] = 40,
                ["Comedy"] = 25,
                ["Noir"] = 5,
                ["Sci-Fi"] = 30,
                ["Doc"] = 0,
            }));

    [Property(MaxTest = 150)]
    [Trait("Category", "Property")]
    public bool Select_ValidPool_ReturnsMinOfTakeAndCandidateCount(
        int[] idSeeds,
        int[] relevanceSeeds,
        int takeSeed)
    {
        // arrange
        var pool = BuildPool(idSeeds, relevanceSeeds);
        var take = Take(takeSeed, pool.Count);

        // act
        var result = MmrSelector.Select(pool, take, 0.7, Weights, GenreTable);

        // assert
        return result.Count == Math.Min(take, pool.Count);
    }

    [Property(MaxTest = 150)]
    [Trait("Category", "Property")]
    public bool Select_ValidPool_ReturnsUniqueIdsFromTheInputSubset(
        int[] idSeeds,
        int[] relevanceSeeds,
        int takeSeed)
    {
        // arrange
        var pool = BuildPool(idSeeds, relevanceSeeds);
        var inputIds = pool.Select(candidate => candidate.MovieId).ToHashSet();
        var take = Take(takeSeed, pool.Count);

        // act
        var result = MmrSelector.Select(pool, take, 0.7, Weights, GenreTable);

        // assert
        return result.Distinct().Count() == result.Count
            && result.All(inputIds.Contains);
    }

    [Property(MaxTest = 150)]
    [Trait("Category", "Property")]
    public bool Select_RepeatedCallOnSameInputs_ReturnsIdenticalOrder(
        int[] idSeeds,
        int[] relevanceSeeds,
        int takeSeed,
        int lambdaSeed)
    {
        // arrange
        var pool = BuildPool(idSeeds, relevanceSeeds);
        var take = Take(takeSeed, pool.Count);
        var lambda = Lambda(lambdaSeed);

        // act
        var first = MmrSelector.Select(pool, take, lambda, Weights, GenreTable);
        var second = MmrSelector.Select(pool, take, lambda, Weights, GenreTable);

        // assert
        return first.SequenceEqual(second);
    }

    [Property(MaxTest = 150)]
    [Trait("Category", "Property")]
    public bool Select_PermutedPool_ReturnsIdenticalOrder(
        int[] idSeeds,
        int[] relevanceSeeds,
        int takeSeed,
        int lambdaSeed)
    {
        // arrange
        var pool = BuildPool(idSeeds, relevanceSeeds);
        var take = Take(takeSeed, pool.Count);
        var lambda = Lambda(lambdaSeed);
        var reversed = pool.ToArray();
        Array.Reverse(reversed);
        var rotated = new List<MmrCandidate>(pool.Count);
        for (var index = 1; index < pool.Count; index++)
        {
            rotated.Add(pool[index]);
        }

        if (pool.Count > 0)
        {
            rotated.Add(pool[0]);
        }

        // act
        var forward = MmrSelector.Select(pool, take, lambda, Weights, GenreTable);
        var reverseResult = MmrSelector.Select(reversed, take, lambda, Weights, GenreTable);
        var rotatedResult = MmrSelector.Select(rotated, take, lambda, Weights, GenreTable);

        // assert
        return forward.SequenceEqual(reverseResult) && forward.SequenceEqual(rotatedResult);
    }

    [Property(MaxTest = 150)]
    [Trait("Category", "Property")]
    public bool Select_AllEqualObjectives_TieBreakByAscendingMovieId(int[] idSeeds, int takeSeed)
    {
        // arrange
        var pool = BuildPool(idSeeds, new int[idSeeds.Length]);
        var equalPool = pool
            .Select(candidate => new MmrCandidate(candidate.MovieId, 0.5, new MovieFeatures()))
            .ToList();
        var take = Take(takeSeed, equalPool.Count);
        var expected = equalPool
            .Select(candidate => candidate.MovieId)
            .OrderBy(id => id.Value)
            .Take(Math.Min(take, equalPool.Count))
            .ToArray();

        // act
        var result = MmrSelector.Select(equalPool, take, 0.7, Weights, GenreTable);

        // assert
        return result.SequenceEqual(expected);
    }

    [Property(MaxTest = 150)]
    [Trait("Category", "Property")]
    public bool Select_MatchesIndependentFullRescanReference(
        int[] idSeeds,
        int[] relevanceSeeds,
        int takeSeed,
        int lambdaSeed)
    {
        // arrange
        var pool = BuildPool(idSeeds, relevanceSeeds);
        var take = Take(takeSeed, pool.Count);
        var lambda = Lambda(lambdaSeed);

        // act
        var actual = MmrSelector.Select(pool, take, lambda, Weights, GenreTable);
        var expected = FullRescanReference(pool, take, lambda, Weights, GenreTable);

        // assert
        return actual.SequenceEqual(expected);
    }

    [Property(MaxTest = 150)]
    [Trait("Category", "Property")]
    public bool Select_CrossTakeResultsArePrefixes(
        int[] idSeeds,
        int[] relevanceSeeds,
        int firstTakeSeed,
        int secondTakeSeed,
        int lambdaSeed)
    {
        // arrange
        var pool = BuildPool(idSeeds, relevanceSeeds);
        var lambda = Lambda(lambdaSeed);
        var shorterTake = Take(firstTakeSeed, pool.Count);
        var longerTake = shorterTake + Take(secondTakeSeed, pool.Count) + (secondTakeSeed & 1);
        var full = MmrSelector.Select(pool, longerTake, lambda, Weights, GenreTable);
        var expectedPrefix = full.Take(Math.Min(shorterTake, pool.Count)).ToArray();

        // act
        var shorter = MmrSelector.Select(pool, shorterTake, lambda, Weights, GenreTable);

        // assert
        return shorter.SequenceEqual(expectedPrefix);
    }

    private static List<MmrCandidate> BuildPool(int[] idSeeds, int[] relevanceSeeds)
    {
        var count = Math.Min(idSeeds.Length, relevanceSeeds.Length);
        var usedIds = new HashSet<long>();
        var pool = new List<MmrCandidate>(count);

        for (var index = 0; index < count; index++)
        {
            long rawId = ((uint)idSeeds[index] % 100_000) + 1;
            while (!usedIds.Add(rawId))
            {
                rawId++;
            }

            pool.Add(new MmrCandidate(
                new MovieId((int)rawId),
                Relevance(relevanceSeeds[index]),
                Features(idSeeds[index])));
        }

        return pool;
    }

    private static MovieFeatures Features(int seed)
    {
        var normalized = seed & int.MaxValue;
        var genreCount = (normalized % 3) + 1;
        var offset = (normalized / 3) % GenreTokens.Length;
        var genres = new List<string>(genreCount);

        for (var index = 0; index < genreCount; index++)
        {
            genres.Add(GenreTokens[(offset + index) % GenreTokens.Length]);
        }

        var directors = normalized % 2 == 0 ? new[] { "Director-A" } : new[] { "Director-B", "Director-C" };
        var cast = normalized % 3 == 0 ? new[] { "Actor-A", "Actor-B" } : new[] { "Actor-B" };

        return new MovieFeatures(genres: genres, directors: directors, cast: cast);
    }

    private static double Relevance(int seed) =>
        ((uint)seed % 2001) / 1000.0;

    private static double Lambda(int seed) =>
        ((uint)seed % 101) / 100.0;

    private static int Take(int seed, int poolCount) =>
        (int)((uint)seed % (uint)(poolCount + 3));

    private static ImmutableList<MovieId> FullRescanReference(
        IReadOnlyList<MmrCandidate> candidates,
        int take,
        double lambda,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        var count = Math.Min(take, candidates.Count);
        var selected = new List<int>(count);
        var builder = ImmutableList.CreateBuilder<MovieId>();

        for (var step = 0; step < count; step++)
        {
            var bestIndex = -1;
            var bestObjective = 0.0;

            for (var index = 0; index < candidates.Count; index++)
            {
                if (selected.Contains(index))
                {
                    continue;
                }

                var maximumSimilarity = 0.0;
                foreach (var selectedIndex in selected)
                {
                    var similarity = SimilarityScorer.Score(
                        candidates[index].Features,
                        candidates[selectedIndex].Features,
                        weights,
                        genreWeights).Score;

                    if (similarity > maximumSimilarity)
                    {
                        maximumSimilarity = similarity;
                    }
                }

                var objective = (lambda * candidates[index].Relevance)
                    - ((1.0 - lambda) * maximumSimilarity);

                if (bestIndex < 0 || IsBetter(candidates[index], objective, candidates[bestIndex], bestObjective))
                {
                    bestIndex = index;
                    bestObjective = objective;
                }
            }

            selected.Add(bestIndex);
            builder.Add(candidates[bestIndex].MovieId);
        }

        return builder.ToImmutable();
    }

    private static bool IsBetter(
        MmrCandidate candidate,
        double objective,
        MmrCandidate bestCandidate,
        double bestObjective) =>
        objective > bestObjective
        || (objective == bestObjective && candidate.MovieId.Value < bestCandidate.MovieId.Value);
}
