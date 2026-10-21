using System;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class SimilarityScorer
{
    private const double YearDecayRate = 0.1;
    private const double RuntimeDecayRate = 0.01;
    private const double RatingScale = 10.0;

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

        var terms = CollectTerms(a, b, weights, genreWeights);
        var score = ComputeScore(a, b, terms);

        return new SimilarityResult(score, ImmutableList<Reason>.Empty);
    }

    private static ImmutableArray<Term> CollectTerms(
        MovieFeatures a,
        MovieFeatures b,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        var builder = ImmutableArray.CreateBuilder<Term>();

        AddSetTerm(builder, a.Directors, b.Directors, weights.Director);
        AddSetTerm(builder, a.Writers, b.Writers, weights.Writers);
        AddSetTerm(builder, a.Cast, b.Cast, weights.Cast);
        AddGenreTerm(builder, a.Genres, b.Genres, weights.Genres, genreWeights);
        AddYearTerm(builder, a.ReleaseYear, b.ReleaseYear, weights.YearProximity);
        AddRuntimeTerm(builder, a.Runtime, b.Runtime, weights.RuntimeProximity);
        AddRatingTerm(builder, a.ImdbRating, b.ImdbRating, weights.RatingBoost);

        return builder.ToImmutable();
    }

    private static void AddSetTerm(
        ImmutableArray<Term>.Builder builder,
        ImmutableHashSet<string> first,
        ImmutableHashSet<string> second,
        double weight)
    {
        if (first.Count > 0 && second.Count > 0)
        {
            builder.Add(new Term(weight, Jaccard(first, second)));
        }
    }

    private static void AddGenreTerm(
        ImmutableArray<Term>.Builder builder,
        ImmutableHashSet<string> first,
        ImmutableHashSet<string> second,
        double weight,
        GenreWeights genreWeights)
    {
        if (first.Count > 0 && second.Count > 0)
        {
            builder.Add(new Term(weight, genreWeights.Similarity(first, second)));
        }
    }

    private static void AddYearTerm(
        ImmutableArray<Term>.Builder builder,
        ReleaseYear? first,
        ReleaseYear? second,
        double weight)
    {
        if (first is not null && second is not null)
        {
            builder.Add(new Term(weight, Proximity(YearDecayRate, first.Value, second.Value)));
        }
    }

    private static void AddRuntimeTerm(
        ImmutableArray<Term>.Builder builder,
        Runtime? first,
        Runtime? second,
        double weight)
    {
        if (first is not null && second is not null)
        {
            builder.Add(new Term(weight, Proximity(RuntimeDecayRate, first.Minutes, second.Minutes)));
        }
    }

    private static void AddRatingTerm(
        ImmutableArray<Term>.Builder builder,
        ImdbRating? first,
        ImdbRating? second,
        double weight)
    {
        if (first is not null && second is not null)
        {
            builder.Add(new Term(weight, RatingProximity(first.Value, second.Value)));
        }
    }

    private static double ComputeScore(
        MovieFeatures a,
        MovieFeatures b,
        ImmutableArray<Term> terms)
    {
        if (terms.IsDefaultOrEmpty)
        {
            return 0.0;
        }

        var maxWeight = MaxWeight(terms);
        if (maxWeight <= 0.0)
        {
            return ZeroDenominatorScore(a, b);
        }

        var numerator = 0.0;
        var denominator = 0.0;
        var allOne = true;

        foreach (var term in terms)
        {
            var scaledWeight = term.Weight / maxWeight;
            numerator += scaledWeight * term.Similarity;
            denominator += scaledWeight;

            if (term.Weight > 0.0 && term.Similarity != 1.0)
            {
                allOne = false;
            }
        }

        return allOne
            ? 1.0
            : Math.Clamp(numerator / denominator, 0.0, 1.0);
    }

    private static double MaxWeight(ImmutableArray<Term> terms)
    {
        var max = 0.0;
        foreach (var term in terms)
        {
            if (term.Weight > max)
            {
                max = term.Weight;
            }
        }

        return max;
    }

    private static double ZeroDenominatorScore(MovieFeatures a, MovieFeatures b) =>
        IsIdenticalProfile(a, b) && HasScoringFeature(a) ? 1.0 : 0.0;

    private static bool IsIdenticalProfile(MovieFeatures a, MovieFeatures b) =>
        a.Directors.SetEquals(b.Directors)
        && a.Writers.SetEquals(b.Writers)
        && a.Cast.SetEquals(b.Cast)
        && a.Genres.SetEquals(b.Genres)
        && a.ReleaseYear == b.ReleaseYear
        && a.Runtime == b.Runtime
        && a.ImdbRating == b.ImdbRating;

    private static bool HasScoringFeature(MovieFeatures movie) =>
        movie.Directors.Count > 0
        || movie.Writers.Count > 0
        || movie.Cast.Count > 0
        || movie.Genres.Count > 0
        || movie.ReleaseYear is not null
        || movie.Runtime is not null
        || movie.ImdbRating is not null;

    private static double Proximity(double decayRate, double first, double second) =>
        Math.Exp(-decayRate * Math.Abs(first - second));

    private static double RatingProximity(decimal first, decimal second) =>
        1.0 - Math.Abs((double)first - (double)second) / RatingScale;

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

    private readonly record struct Term(double Weight, double Similarity);
}
