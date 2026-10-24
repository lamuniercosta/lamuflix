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
        GenreWeights genreWeights)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(genreWeights);

        ValidateWeights(weights);
        ValidateLambda(lambda);
        ValidateTake(take);
        ValidateCandidates(candidates);

        if (candidates.Count == 0 || take == 0)
        {
            return ImmutableList<MovieId>.Empty;
        }

        return SelectOrdered(candidates, Math.Min(take, candidates.Count), lambda, weights, genreWeights);
    }

    private static ImmutableList<MovieId> SelectOrdered(
        IReadOnlyList<MmrCandidate> candidates,
        int count,
        double lambda,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        var selected = new bool[candidates.Count];
        var maximumRedundancy = new double[candidates.Count];
        var selectedFeatures = new List<MovieFeatures>(count);
        var builder = ImmutableList.CreateBuilder<MovieId>();

        for (var step = 0; step < count; step++)
        {
            var chosen = ChooseNext(candidates, selected, maximumRedundancy, lambda);
            selected[chosen] = true;
            builder.Add(candidates[chosen].MovieId);
            selectedFeatures.Add(candidates[chosen].Features);

            if (step == count - 1)
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
        double lambda)
    {
        var bestIndex = -1;
        var bestObjective = 0.0;

        for (var index = 0; index < candidates.Count; index++)
        {
            if (selected[index])
            {
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
            MmrCandidate? candidate = candidates[index];

            if (candidate is null)
            {
                throw new ArgumentException("Candidates cannot contain null entries.", nameof(candidates));
            }

            if (candidate.MovieId is null)
            {
                throw new ArgumentException("Candidate movie ids cannot be null.", nameof(candidates));
            }

            if (candidate.Features is null)
            {
                throw new ArgumentException("Candidate features cannot be null.", nameof(candidates));
            }

            if (!double.IsFinite(candidate.Relevance) || candidate.Relevance < 0.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(candidates),
                    candidate.Relevance,
                    "Candidate relevance must be non-negative and finite.");
            }

            if (!seen.Add(candidate.MovieId))
            {
                throw new ArgumentException("Candidate movie ids must be unique.", nameof(candidates));
            }
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
