using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Recommendations;

public static class SimilarityScorer
{
    public static SimilarityResult Score(
        MovieFeatures a,
        MovieFeatures b,
        RecommendationOptions options,
        GenreWeights genreWeights,
        KeywordCounts keywordCounts)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(genreWeights);
        ArgumentNullException.ThrowIfNull(keywordCounts);
        ValidateKeywordCounts(keywordCounts);

        var failures = options.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(options)).ToArray();
        if (failures.Length > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Similarity scoring options are invalid.");
        }

        var weights = options.Weights;
        var denominator = weights.Collection + weights.Director + weights.Themes + weights.Plot + weights.Cast + weights.Genres;
        var terms = new[]
        {
            Create("Collection", weights.Collection, CollectionValue(a, b), CollectionEvidence(a, b)),
            Create("Director", weights.Director, SetValue(a.Directors, b.Directors), SharedNames(a.Directors, b.Directors)),
            Create("Themes", weights.Themes, ThemesValue(a, b, options, keywordCounts), ThemeEvidence(a, b, options, keywordCounts)),
            Create("Plot", weights.Plot, PlotValue(a, b, options), Array.Empty<string>()),
            Create("Cast", weights.Cast, Math.Min(1.0, SharedNames(a.Cast, b.Cast).Count / 2.0), SharedNames(a.Cast, b.Cast)),
            Create("Genre", weights.Genres, genreWeights.Similarity(a.Genres, b.Genres), SharedNames(a.Genres, b.Genres)),
        };

        var breakdown = terms.Select(term => new CriterionContribution(
            term.Name,
            term.Value,
            term.Weight,
            term.Weight * term.Value / denominator)
        {
            Evidence = [.. term.Evidence],
        }).ToImmutableList();
        var reasons = BuildReasons(terms, options);

        return new SimilarityResult(breakdown.Sum(entry => entry.Contribution), reasons) { Breakdown = breakdown };
    }

    private static ScoringTerm Create(string name, double weight, double value, IReadOnlyList<string> evidence) =>
        new(name, weight, Math.Clamp(value, 0.0, 1.0), evidence);

    private static double CollectionValue(MovieFeatures a, MovieFeatures b) =>
        a.CollectionId is not null && b.CollectionId is not null && a.CollectionId == b.CollectionId ? 1.0 : 0.0;

    private static IReadOnlyList<string> CollectionEvidence(MovieFeatures a, MovieFeatures b)
    {
        if (CollectionValue(a, b) == 0.0)
        {
            return Array.Empty<string>();
        }

        var name = string.IsNullOrWhiteSpace(a.CollectionName) ? b.CollectionName : a.CollectionName;
        return string.IsNullOrWhiteSpace(name) ? Array.Empty<string>() : [name.Trim()];
    }

    private static double SetValue(ImmutableHashSet<string> a, ImmutableHashSet<string> b) =>
        SharedNames(a, b).Count > 0 ? 1.0 : 0.0;

    private static IReadOnlyList<string> SharedNames(IEnumerable<string> a, IEnumerable<string> b)
    {
        var left = CleanNames(a);
        var right = CleanNames(b);
        return [.. left.Where(name => right.Contains(name))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(name => name, StringComparer.Ordinal)];
    }

    private static HashSet<string> CleanNames(IEnumerable<string> names) =>
        names.Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderBy(name => name, StringComparer.Ordinal).First())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static double ThemesValue(MovieFeatures a, MovieFeatures b, RecommendationOptions options, KeywordCounts counts)
    {
        if (counts.TotalMovies <= 0)
        {
            return 0.0;
        }

        var weights = KeywordWeights(options, counts);
        var left = CleanNames(a.KeywordNames).Where(weights.ContainsKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var right = CleanNames(b.KeywordNames).Where(weights.ContainsKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var leftNorm = left.Sum(name => Math.Pow(weights[name], 2));
        var rightNorm = right.Sum(name => Math.Pow(weights[name], 2));
        if (leftNorm <= 0.0 || rightNorm <= 0.0)
        {
            return 0.0;
        }

        var numerator = left.Intersect(right, StringComparer.OrdinalIgnoreCase).Sum(name => Math.Pow(weights[name], 2));
        return Math.Min(1.0, numerator / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm)) / options.ThemesCeiling);
    }

    private static IReadOnlyList<string> ThemeEvidence(MovieFeatures a, MovieFeatures b, RecommendationOptions options, KeywordCounts counts)
    {
        var weights = KeywordWeights(options, counts);
        return [.. CleanNames(a.KeywordNames).Intersect(CleanNames(b.KeywordNames), StringComparer.OrdinalIgnoreCase)
            .Where(weights.ContainsKey)
            .Select(name => weights.Keys.First(keyword => string.Equals(keyword, name, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(name => weights[name])
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(name => name, StringComparer.Ordinal)
            .Take(3)];
    }

    private static Dictionary<string, double> KeywordWeights(RecommendationOptions options, KeywordCounts counts)
    {
        var stoplist = (options.ThemeStoplist ?? Array.Empty<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, count) in counts.MoviesWithKeyword)
        {
            if (IsExcludedKeyword(name, count, options, counts, stoplist))
            {
                continue;
            }

            result[name.Trim()] = Math.Log((double)counts.TotalMovies / count);
        }

        return result;
    }

    private static bool IsExcludedKeyword(string name, long count, RecommendationOptions options, KeywordCounts counts, HashSet<string> stoplist) =>
        string.IsNullOrWhiteSpace(name)
        || stoplist.Contains(name.Trim())
        || IsOutOfKeywordRange(count, options, counts);

    private static bool IsOutOfKeywordRange(long count, RecommendationOptions options, KeywordCounts counts) =>
        count < options.MinimumKeywordMovies
        || count <= 0
        || count > counts.TotalMovies;

    private static void ValidateKeywordCounts(KeywordCounts counts)
    {
        if (counts.TotalMovies < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(counts), "The keyword-count snapshot total cannot be negative.");
        }

        foreach (var (name, count) in counts.MoviesWithKeyword)
        {
            if (string.IsNullOrWhiteSpace(name) || count <= 0 || count > counts.TotalMovies)
            {
                throw new ArgumentOutOfRangeException(nameof(counts), "Keyword counts must be between 1 and the library movie total.");
            }
        }
    }

    private static double PlotValue(MovieFeatures a, MovieFeatures b, RecommendationOptions options)
    {
        if (HasMismatchedEmbeddings(a, b))
        {
            return 0.0;
        }

        var cosine = CosineSimilarity(a.PlotEmbedding, b.PlotEmbedding);
        if (cosine is null)
        {
            return 0.0;
        }

        return Math.Clamp((cosine.Value - options.PlotFloor) / (options.PlotCeiling - options.PlotFloor), 0.0, 1.0);
    }

    private static bool HasMismatchedEmbeddings(MovieFeatures a, MovieFeatures b) =>
        a.PlotEmbedding.IsDefaultOrEmpty
        || b.PlotEmbedding.IsDefaultOrEmpty
        || a.PlotEmbedding.Length != b.PlotEmbedding.Length;

    private static double? CosineSimilarity(ImmutableArray<float> left, ImmutableArray<float> right)
    {
        var dot = 0.0;
        var leftNorm = 0.0;
        var rightNorm = 0.0;
        for (var index = 0; index < left.Length; index++)
        {
            var x = left[index];
            var y = right[index];
            if (!float.IsFinite(x) || !float.IsFinite(y))
            {
                return null;
            }

            dot += x * y;
            leftNorm += x * x;
            rightNorm += y * y;
        }

        if (leftNorm == 0.0 || rightNorm == 0.0)
        {
            return null;
        }

        return Math.Clamp(dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm)), -1.0, 1.0);
    }

    private static ImmutableList<Reason> BuildReasons(ScoringTerm[] terms, RecommendationOptions options)
    {
        var reasons = ImmutableList.CreateBuilder<Reason>();
        foreach (var term in terms)
        {
            var text = ReasonText(term, options);
            if (text is not null)
            {
                reasons.Add(Reason.Structural(text));
            }
        }

        return reasons.ToImmutable();
    }

    private static string? ReasonText(ScoringTerm term, RecommendationOptions options)
    {
        if (term.Value <= 0.0 || term.Value < DisplayThreshold(term.Name, options))
        {
            return null;
        }

        if (term.Evidence.Count == 0)
        {
            return term.Name == "Plot" ? "Similar story" : null;
        }

        return term.Name switch
        {
            "Collection" => $"Same collection: {term.Evidence[0]}",
            "Director" => $"Same director: {string.Join(", ", term.Evidence)}",
            "Themes" => $"Shared themes: {string.Join(", ", term.Evidence)}",
            "Plot" => "Similar story",
            "Cast" => $"Shares cast: {string.Join(", ", term.Evidence)}",
            _ => $"Same genres: {string.Join(", ", term.Evidence)}",
        };
    }

    private static double DisplayThreshold(string name, RecommendationOptions options) =>
        name switch
        {
            "Themes" => options.ThemesDisplayThreshold,
            "Plot" => options.PlotDisplayThreshold,
            "Genre" => options.GenreDisplayThreshold,
            _ => 0.0,
        };

    private readonly record struct ScoringTerm(string Name, double Weight, double Value, IReadOnlyList<string> Evidence);

}
