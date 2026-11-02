using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class SeedCollectionGrouper
{
    public const int MaximumBlockSize = 10;

    public static ImmutableList<MovieId> Group(
        IReadOnlyList<MmrCandidate> candidates,
        ImmutableList<MovieId> mmrOrder,
        MovieFeatures seedFeatures,
        int take,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(mmrOrder);
        ArgumentNullException.ThrowIfNull(seedFeatures);
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(genreWeights);

        if (take < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(take), take, "Take cannot be negative.");
        }

        if (seedFeatures.CollectionId is not { } collectionId)
        {
            return mmrOrder;
        }

        var matches = MatchingCandidates(candidates, collectionId);
        if (matches.Count == 0)
        {
            return mmrOrder;
        }

        var blockSize = Math.Min(Math.Min(MaximumBlockSize, take), matches.Count);
        var block = Rank(seedFeatures, matches, weights, genreWeights, blockSize);
        if (block.Count == 0)
        {
            return mmrOrder;
        }

        return Compose(block, mmrOrder, take);
    }

    private static List<MmrCandidate> MatchingCandidates(
        IReadOnlyList<MmrCandidate> candidates,
        int collectionId)
    {
        var matches = new List<MmrCandidate>();

        foreach (var candidate in candidates)
        {
            if (candidate.Features.CollectionId == collectionId)
            {
                matches.Add(candidate);
            }
        }

        return matches;
    }

    private static List<MovieId> Rank(
        MovieFeatures seedFeatures,
        List<MmrCandidate> matches,
        SimilarityWeights weights,
        GenreWeights genreWeights,
        int blockSize)
    {
        var ranked = new List<(MovieId MovieId, double Score)>(matches.Count);

        foreach (var candidate in matches)
        {
            var score = SimilarityScorer.Score(seedFeatures, candidate.Features, weights, genreWeights).Score;
            ranked.Add((candidate.MovieId, score));
        }

        ranked.Sort(CompareRanked);

        var block = new List<MovieId>(blockSize);
        for (var index = 0; index < blockSize; index++)
        {
            block.Add(ranked[index].MovieId);
        }

        return block;
    }

    private static int CompareRanked((MovieId MovieId, double Score) first, (MovieId MovieId, double Score) second)
    {
        var byScore = second.Score.CompareTo(first.Score);
        return byScore != 0 ? byScore : first.MovieId.Value.CompareTo(second.MovieId.Value);
    }

    private static ImmutableList<MovieId> Compose(
        List<MovieId> block,
        ImmutableList<MovieId> mmrOrder,
        int take)
    {
        var builder = ImmutableList.CreateBuilder<MovieId>();
        var seen = new HashSet<MovieId>();

        foreach (var movieId in block)
        {
            if (builder.Count < take && seen.Add(movieId))
            {
                builder.Add(movieId);
            }
        }

        foreach (var movieId in mmrOrder)
        {
            if (builder.Count < take && seen.Add(movieId))
            {
                builder.Add(movieId);
            }
        }

        return builder.ToImmutable();
    }
}
