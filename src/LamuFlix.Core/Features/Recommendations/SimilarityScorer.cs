using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class SimilarityScorer
{
    private const double YearDecayRate = 0.1;
    private const double RuntimeDecayRate = 0.01;
    private const double RatingScale = 10.0;
    private const string IdentityReason = "Identical feature profile";

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
        var maxWeight = MaxWeight(terms);

        if (maxWeight <= 0.0)
        {
            return ZeroWeightResult(a, b);
        }

        return new SimilarityResult(ComputeScore(terms, maxWeight), CollectReasons(terms));
    }

    private static ImmutableArray<Term> CollectTerms(
        MovieFeatures a,
        MovieFeatures b,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        var builder = ImmutableArray.CreateBuilder<Term>();

        AddSetTerm(builder, a.Directors, b.Directors, weights.Director, SetLabel.Director);
        AddSetTerm(builder, a.Writers, b.Writers, weights.Writers, SetLabel.Writers);
        AddSetTerm(builder, a.Cast, b.Cast, weights.Cast, SetLabel.Cast);
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
        double weight,
        SetLabel label)
    {
        if (first.Count > 0 && second.Count > 0)
        {
            var similarity = Jaccard(first, second);
            var reason = IsEligible(weight, similarity) ? SetReason(label, first, second) : null;
            builder.Add(new Term(weight, similarity, reason));
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
            var similarity = genreWeights.Similarity(first, second);
            var reason = IsEligible(weight, similarity)
                ? SetReason(SetLabel.Genres, first, second)
                : null;
            builder.Add(new Term(weight, similarity, reason));
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
            var similarity = Proximity(YearDecayRate, first.Value, second.Value);
            builder.Add(new Term(weight, similarity, Label(weight, similarity, "Similar release years")));
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
            var similarity = Proximity(RuntimeDecayRate, first.Minutes, second.Minutes);
            builder.Add(new Term(weight, similarity, Label(weight, similarity, "Similar runtime")));
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
            var similarity = RatingProximity(first.Value, second.Value);
            builder.Add(new Term(weight, similarity, Label(weight, similarity, "Similar rating")));
        }
    }

    private static double ComputeScore(ImmutableArray<Term> terms, double maxWeight)
    {
        var numerator = 0.0;
        var denominator = 0.0;
        var allOne = true;

        foreach (var term in terms)
        {
            var scaledWeight = term.Weight / maxWeight;
            numerator += scaledWeight * term.Similarity;
            denominator += scaledWeight;

            if (term.Weight > 0.0 && term.Similarity < 1.0)
            {
                allOne = false;
            }
        }

        return allOne
            ? 1.0
            : Math.Clamp(numerator / denominator, 0.0, 1.0);
    }

    private static ImmutableList<Reason> CollectReasons(ImmutableArray<Term> terms)
    {
        var builder = ImmutableList.CreateBuilder<Reason>();

        foreach (var term in terms)
        {
            if (term.Reason is { } reason)
            {
                builder.Add(new Reason(reason));
            }
        }

        return builder.ToImmutable();
    }

    private static SimilarityResult ZeroWeightResult(MovieFeatures a, MovieFeatures b) =>
        IsIdenticalProfile(a, b) && HasScoringFeature(a)
            ? new SimilarityResult(1.0, ImmutableList.Create(new Reason(IdentityReason)))
            : new SimilarityResult(0.0, ImmutableList<Reason>.Empty);

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

    private static bool IsIdenticalProfile(MovieFeatures a, MovieFeatures b) =>
        AreSetsIdentical(a, b) && AreScalarsIdentical(a, b);

    private static bool AreSetsIdentical(MovieFeatures a, MovieFeatures b) =>
        a.Directors.SetEquals(b.Directors)
        && a.Writers.SetEquals(b.Writers)
        && a.Cast.SetEquals(b.Cast)
        && a.Genres.SetEquals(b.Genres);

    private static bool AreScalarsIdentical(MovieFeatures a, MovieFeatures b) =>
        a.ReleaseYear == b.ReleaseYear
        && a.Runtime == b.Runtime
        && a.ImdbRating == b.ImdbRating;

    private static bool HasScoringFeature(MovieFeatures movie) =>
        HasSetFeature(movie) || HasScalarFeature(movie);

    private static bool HasSetFeature(MovieFeatures movie) =>
        movie.Directors.Count > 0
        || movie.Writers.Count > 0
        || movie.Cast.Count > 0
        || movie.Genres.Count > 0;

    private static bool HasScalarFeature(MovieFeatures movie) =>
        movie.ReleaseYear is not null
        || movie.Runtime is not null
        || movie.ImdbRating is not null;

    private static double Proximity(double decayRate, double first, double second) =>
        Math.Exp(-decayRate * Math.Abs(first - second));

    private static double RatingProximity(decimal first, decimal second) =>
        1.0 - Math.Abs((double)first - (double)second) / RatingScale;

    private static double Jaccard(ImmutableHashSet<string> first, ImmutableHashSet<string> second)
    {
        var intersection = SharedTokens(first, second).Count;
        var union = first.Count + second.Count - intersection;
        return (double)intersection / union;
    }

    private static bool IsEligible(double weight, double similarity) =>
        weight > 0.0 && similarity > 0.0;

    private static string? Label(double weight, double similarity, string label) =>
        IsEligible(weight, similarity) ? label : null;

    private static string SetReason(
        SetLabel label,
        ImmutableHashSet<string> first,
        ImmutableHashSet<string> second)
    {
        var tokens = DisplayTokens(SharedTokens(first, second));
        return tokens.Count == 0
            ? Fallback(label)
            : Format(label, string.Join(", ", tokens));
    }

    private static List<string> SharedTokens(
        ImmutableHashSet<string> first,
        ImmutableHashSet<string> second)
    {
        var tokens = new List<string>();

        foreach (var value in first)
        {
            if (second.TryGetValue(value, out var other))
            {
                tokens.Add(string.CompareOrdinal(value, other) <= 0 ? value : other);
            }
        }

        return tokens;
    }

    private static List<string> DisplayTokens(List<string> tokens)
    {
        tokens.Sort(CompareTokens);
        var display = new List<string>(tokens.Count);

        foreach (var token in tokens)
        {
            var text = Sanitize(token);
            if (text.Length > 0)
            {
                display.Add(text);
            }
        }

        return display;
    }

    private static int CompareTokens(string first, string second)
    {
        var insensitive = StringComparer.OrdinalIgnoreCase.Compare(first, second);
        return insensitive != 0 ? insensitive : StringComparer.Ordinal.Compare(first, second);
    }

    private static string Sanitize(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(char.IsControl(character) ? ' ' : character);
        }

        return builder.ToString().Trim();
    }

    private static string Format(SetLabel label, string names) =>
        label switch
        {
            SetLabel.Director => $"Same director: {names}",
            SetLabel.Writers => $"Shared writers: {names}",
            SetLabel.Cast => $"Features {names}",
            _ => $"Shares {names}",
        };

    private static string Fallback(SetLabel label) =>
        label switch
        {
            SetLabel.Director => "Shared director",
            SetLabel.Writers => "Shared writers",
            SetLabel.Cast => "Shared cast",
            _ => "Shared genres",
        };

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

    private enum SetLabel
    {
        Director,
        Writers,
        Cast,
        Genres,
    }

    private readonly record struct Term(double Weight, double Similarity, string? Reason);
}
