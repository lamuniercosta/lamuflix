using System;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class SimilarityScorer
{
    public static SimilarityResult Score(
        MovieFeatures a,
        MovieFeatures b,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(genreWeights);

        ValidateWeights(weights);

        var numerator = 0.0;
        var denominator = 0.0;

        AccumulateSet(a.Directors, b.Directors, weights.Director, ref numerator, ref denominator);
        AccumulateSet(a.Writers, b.Writers, weights.Writers, ref numerator, ref denominator);
        AccumulateSet(a.Cast, b.Cast, weights.Cast, ref numerator, ref denominator);

        if (a.Genres.Count > 0 && b.Genres.Count > 0)
        {
            Accumulate(
                weights.Genres,
                genreWeights.Similarity(a.Genres, b.Genres),
                ref numerator,
                ref denominator);
        }

        var score = denominator > 0.0
            ? Math.Clamp(numerator / denominator, 0.0, 1.0)
            : 0.0;

        return new SimilarityResult(score, ImmutableList<Reason>.Empty);
    }

    private static void AccumulateSet(
        ImmutableHashSet<string> first,
        ImmutableHashSet<string> second,
        double weight,
        ref double numerator,
        ref double denominator)
    {
        if (first.Count > 0 && second.Count > 0)
        {
            Accumulate(weight, Jaccard(first, second), ref numerator, ref denominator);
        }
    }

    private static void Accumulate(
        double weight,
        double similarity,
        ref double numerator,
        ref double denominator)
    {
        numerator += weight * similarity;
        denominator += weight;
    }

    private static double Jaccard(ImmutableHashSet<string> first, ImmutableHashSet<string> second)
    {
        var intersection = 0;
        foreach (var value in first)
        {
            if (second.Contains(value))
            {
                intersection++;
            }
        }

        var union = first.Count + second.Count - intersection;
        return (double)intersection / union;
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
