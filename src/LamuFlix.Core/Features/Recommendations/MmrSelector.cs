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

        return SelectOrdered(candidates, Math.Min(take, candidates.Count));
    }

    // Phase 2 replaces this placeholder with the deterministic MMR selection loop.
    private static ImmutableList<MovieId> SelectOrdered(
        IReadOnlyList<MmrCandidate> candidates,
        int count)
    {
        var builder = ImmutableList.CreateBuilder<MovieId>();

        for (var index = 0; index < count; index++)
        {
            builder.Add(candidates[index].MovieId);
        }

        return builder.ToImmutable();
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
