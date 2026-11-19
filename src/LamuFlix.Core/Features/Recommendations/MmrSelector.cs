using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class MmrSelector
{
    public static ImmutableList<MovieId> Select(
        IReadOnlyList<MmrCandidate> candidates,
        int take,
        double lambda,
        SimilarityWeights weights,
        GenreWeights genreWeights,
        int maxPerBestSeed = int.MaxValue,
        int maxPerCollection = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(genreWeights);

        ValidateWeights(weights);
        ValidateLambda(lambda);
        ValidateTake(take);
        ValidateCap(maxPerBestSeed, nameof(maxPerBestSeed));
        ValidateCap(maxPerCollection, nameof(maxPerCollection));
        ValidateCandidates(candidates);

        if (candidates.Count == 0 || take == 0)
        {
            return ImmutableList<MovieId>.Empty;
        }

        return SelectOrdered(candidates, Math.Min(take, candidates.Count), lambda, weights, genreWeights, maxPerBestSeed, maxPerCollection);
    }

    private static ImmutableList<MovieId> SelectOrdered(
        IReadOnlyList<MmrCandidate> candidates,
        int count,
        double lambda,
        SimilarityWeights weights,
        GenreWeights genreWeights,
        int maxPerBestSeed,
        int maxPerCollection)
    {
        var selected = new bool[candidates.Count];
        var maximumRedundancy = new double[candidates.Count];
        var selectedFeatures = new List<MovieFeatures>(count);
        var seedCounts = new Dictionary<MovieId, int>();
        var collectionCounts = new Dictionary<int, int>();
        var builder = ImmutableList.CreateBuilder<MovieId>();

        while (builder.Count < count)
        {
            var chosen = ChooseNext(candidates, selected, maximumRedundancy, lambda, seedCounts, collectionCounts, maxPerBestSeed, maxPerCollection);
            if (chosen < 0)
            {
                break;
            }

            selected[chosen] = true;
            builder.Add(candidates[chosen].MovieId);
            selectedFeatures.Add(candidates[chosen].Features);
            var candidate = candidates[chosen];
            if (candidate.BestSeedId is { } seedId)
            {
                seedCounts[seedId] = seedCounts.GetValueOrDefault(seedId) + 1;
            }

            if (candidate.Features.CollectionId is { } collectionId)
            {
                collectionCounts[collectionId] = collectionCounts.GetValueOrDefault(collectionId) + 1;
            }

            if (builder.Count == count)
            {
                break;
            }

            UpdateMaximumRedundancy(candidates, selected, maximumRedundancy, selectedFeatures, weights, genreWeights);
        }

        return builder.ToImmutable();
    }

    private static int ChooseNext(
        IReadOnlyList<MmrCandidate> candidates,
        bool[] selected,
        double[] maximumRedundancy,
        double lambda,
        Dictionary<MovieId, int> seedCounts,
        Dictionary<int, int> collectionCounts,
        int maxPerBestSeed,
        int maxPerCollection)
    {
        var bestIndex = -1;
        var bestObjective = 0.0;

        for (var index = 0; index < candidates.Count; index++)
        {
            if (selected[index])
            {
                continue;
            }

            var candidate = candidates[index];
            if ((candidate.BestSeedId is { } seedId && seedCounts.GetValueOrDefault(seedId) >= maxPerBestSeed)
                || (candidate.Features.CollectionId is { } collectionId
                    && collectionCounts.GetValueOrDefault(collectionId) >= maxPerCollection))
            {
                selected[index] = true;
                continue;
            }

            var objective = (lambda * candidates[index].Relevance)
                - ((1.0 - lambda) * maximumRedundancy[index]);

            if (bestIndex < 0 || IsBetter(candidates[index], objective, candidates[bestIndex], bestObjective))
            {
                bestIndex = index;
                bestObjective = objective;
            }
        }

        return bestIndex;
    }

    private static bool IsBetter(
        MmrCandidate candidate,
        double objective,
        MmrCandidate bestCandidate,
        double bestObjective) =>
        objective > bestObjective
        // ReSharper disable once CompareOfFloatsByEqualityOperator // exact objective equality drives the spec-mandated ID tie-break; no epsilon
        || (objective == bestObjective && candidate.MovieId.Value < bestCandidate.MovieId.Value);

    private static void UpdateMaximumRedundancy(
        IReadOnlyList<MmrCandidate> candidates,
        bool[] selected,
        double[] maximumRedundancy,
        List<MovieFeatures> selectedFeatures,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        var newest = selectedFeatures[^1];

        for (var index = 0; index < candidates.Count; index++)
        {
            if (selected[index])
            {
                continue;
            }

            var similarity = SimilarityScorer.Score(candidates[index].Features, newest, weights, genreWeights).Score;
            if (similarity > maximumRedundancy[index])
            {
                maximumRedundancy[index] = similarity;
            }
        }
    }

    private static void ValidateCandidates(IReadOnlyList<MmrCandidate> candidates)
    {
        var seen = new HashSet<MovieId>();

        for (var index = 0; index < candidates.Count; index++)
        {
            ValidateCandidate(candidates[index], seen, nameof(candidates));
        }
    }

    private static void ValidateCandidate(MmrCandidate? candidate, HashSet<MovieId> seen, string paramName)
    {
        if (candidate is null)
        {
            throw new ArgumentException("Candidates cannot contain null entries.", paramName);
        }

        if (candidate.MovieId is null)
        {
            throw new ArgumentException("Candidate movie ids cannot be null.", paramName);
        }

        if (candidate.Features is null)
        {
            throw new ArgumentException("Candidate features cannot be null.", paramName);
        }

        ValidateRelevance(candidate.Relevance, paramName);

        if (!seen.Add(candidate.MovieId))
        {
            throw new ArgumentException("Candidate movie ids must be unique.", paramName);
        }
    }

    private static void ValidateRelevance(double relevance, string paramName)
    {
        if (!double.IsFinite(relevance) || relevance < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                relevance,
                "Candidate relevance must be non-negative and finite.");
        }
    }

    private static void ValidateLambda(double lambda)
    {
        if (!double.IsFinite(lambda) || lambda < 0.0 || lambda > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lambda),
                lambda,
                "Lambda must be finite and within the inclusive range [0, 1].");
        }
    }

    private static void ValidateTake(int take)
    {
        if (take < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take),
                take,
                "Take cannot be negative.");
        }
    }

    private static void ValidateCap(int cap, string paramName)
    {
        if (cap < 1)
        {
            throw new ArgumentOutOfRangeException(paramName, cap, "Caps must be greater than zero.");
        }
    }

    private static void ValidateWeights(SimilarityWeights weights)
    {
        ValidateWeight(weights.Director, nameof(weights));
        ValidateWeight(weights.Writers, nameof(weights));
        ValidateWeight(weights.Cast, nameof(weights));
        ValidateWeight(weights.Genres, nameof(weights));
        ValidateWeight(weights.YearProximity, nameof(weights));
        ValidateWeight(weights.RuntimeProximity, nameof(weights));
        ValidateWeight(weights.RatingBoost, nameof(weights));
    }

    private static void ValidateWeight(double weight, string paramName)
    {
        if (weight < 0.0 || !double.IsFinite(weight))
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                weight,
                "Similarity weights must be non-negative and finite.");
        }
    }
}
