using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Recommendations;

public static class SimilarityScorer
{
    private const int DisplayNameLimit = 3;

    public static SimilarityResult Score(
        MovieFeatures a,
        MovieFeatures b,
        RecommendationOptions options,
        GenreWeights genreWeights,
        KeywordCounts keywordCounts)
    {
        ArgumentNullException.ThrowIfNull(keywordCounts);
        return Score(a, b, options, genreWeights, PrepareKeywordWeights(options, keywordCounts));
    }

    public static SimilarityResult Score(
        MovieFeatures a,
        MovieFeatures b,
        RecommendationOptions options,
        GenreWeights genreWeights,
        IReadOnlyDictionary<string, double> keywordWeights)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(genreWeights);
        ArgumentNullException.ThrowIfNull(keywordWeights);
        ThrowIfInvalid(options);

        return ScoreWithoutValidation(a, b, options, genreWeights, keywordWeights);
    }

    internal static SimilarityResult ScoreWithoutValidation(
        MovieFeatures a,
        MovieFeatures b,
        RecommendationOptions options,
        GenreWeights genreWeights,
        IReadOnlyDictionary<string, double> keywordWeights)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(genreWeights);
        ArgumentNullException.ThrowIfNull(keywordWeights);

        var weights = options.Weights;
        var denominator = weights.Collection + weights.Director + weights.Themes + weights.Plot + weights.Cast + weights.Genres;
        var terms = new[]
        {
            Create("Collection", weights.Collection, CollectionValue(a, b), CollectionEvidence(a, b)),
            Create("Director", weights.Director, SetValue(a.Directors, b.Directors), SharedNames(a.Directors, b.Directors)),
            Create("Themes", weights.Themes, ThemesValue(a, b, options, keywordWeights), ThemeEvidence(a, b, keywordWeights)),
            Create("Plot", weights.Plot, PlotValue(a, b, options), Array.Empty<string>()),
            Create("Cast", weights.Cast, Math.Min(1.0, SharedNames(a.Cast, b.Cast).Count / 2.0), SharedNames(a.Cast, b.Cast)),
            Create("Genre", weights.Genres, genreWeights.Similarity(a.Genres, b.Genres), SharedNames(a.Genres, b.Genres)),
        };

        return BuildResult(terms, denominator, options);
    }

    internal static SimilarityResult ScorePreparedFull(PreparedMovie a, PreparedMovie b, ScoringContext context)
    {
        var options = context.Options;
        var weights = options.Weights;
        var fast = ScorePreparedValues(a, b, context);
        var terms = new[]
        {
            Create("Collection", weights.Collection, fast.Collection, CollectionEvidence(a.CollectionId, a.CollectionName, b.CollectionId, b.CollectionName)),
            Create("Director", weights.Director, fast.Director, SharedEvidence(a.Directors, b.Directors)),
            Create("Themes", weights.Themes, fast.Themes, ThemesEvidence(a, b, context)),
            Create("Plot", weights.Plot, fast.Plot, Array.Empty<string>()),
            Create("Cast", weights.Cast, fast.Cast, SharedEvidence(a.Cast, b.Cast)),
            Create("Genre", weights.Genres, fast.Genre, SharedEvidence(a.CleanedGenres, b.CleanedGenres)),
        };

        return BuildResult(terms, context.Denominator, options);
    }

    private static SimilarityResult BuildResult(ScoringTerm[] terms, double denominator, RecommendationOptions options)
    {
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

    private static IReadOnlyList<string> CollectionEvidence(int? leftId, string? leftName, int? rightId, string? rightName)
    {
        if (CollectionValue(leftId, rightId) == 0.0)
        {
            return Array.Empty<string>();
        }

        var name = string.IsNullOrWhiteSpace(leftName) ? rightName : leftName;
        return string.IsNullOrWhiteSpace(name) ? Array.Empty<string>() : [name.Trim()];
    }

    private static string[] SharedEvidence(HashSet<string> left, HashSet<string> right) =>
        [.. left.Where(right.Contains)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(name => name, StringComparer.Ordinal)];

    private static string[] ThemesEvidence(PreparedMovie a, PreparedMovie b, ScoringContext context)
    {
        var shared = new List<string>();
        foreach (var name in a.CleanedKeywords)
        {
            if (b.CleanedKeywords.Contains(name)
                && context.KeywordWeights.ContainsKey(name))
            {
                shared.Add(context.CanonicalKeywords[name]);
            }
        }

        return [.. shared.OrderByDescending(name => context.KeywordWeights[name])
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(name => name, StringComparer.Ordinal)
            .Take(3)];
    }

    private static ScoringTerm Create(string name, double weight, double value, IReadOnlyList<string> evidence) =>
        new(name, weight, Math.Clamp(value, 0.0, 1.0), evidence);

    private static double CollectionValue(MovieFeatures a, MovieFeatures b) =>
        CollectionValue(a.CollectionId, b.CollectionId);

    private static double CollectionValue(int? left, int? right) =>
        left is not null && right is not null && left == right ? 1.0 : 0.0;

    internal static HashSet<string> CleanNamesForRequest(IEnumerable<string> names) => CleanNames(names);

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

    public static IReadOnlyDictionary<string, double> PrepareKeywordWeights(
        RecommendationOptions options,
        KeywordCounts counts)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(counts);
        ThrowIfInvalid(options);
        ValidateKeywordCounts(counts);

        return KeywordWeights(options, counts);
    }

    internal static void ThrowIfInvalid(RecommendationOptions options)
    {
        var failures = options.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(options)).ToArray();
        if (failures.Length > 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Similarity scoring options are invalid.");
        }
    }

    private static double ThemesValue(MovieFeatures a, MovieFeatures b, RecommendationOptions options, IReadOnlyDictionary<string, double> weights)
    {
        if (weights.Count == 0)
        {
            return 0.0;
        }

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

    private static IReadOnlyList<string> ThemeEvidence(MovieFeatures a, MovieFeatures b, IReadOnlyDictionary<string, double> weights)
    {
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

    public static ImmutableList<RecommendationEvidence> BuildEvidence(
        SimilarityResult similarity,
        RecommendationOptions options)
    {
        ArgumentNullException.ThrowIfNull(similarity);
        ArgumentNullException.ThrowIfNull(options);

        return
        [
            .. similarity.Breakdown
                .Select((contribution, index) => (Contribution: contribution, Index: index))
                .Where(entry => IsDisplayable(entry.Contribution, options))
                .OrderByDescending(entry => entry.Contribution.Contribution)
                .ThenBy(entry => entry.Index)
                .Select(entry => new RecommendationEvidence(
                    entry.Contribution.Name,
                    EvidenceText(entry.Contribution.Name, entry.Contribution.Evidence),
                    [.. entry.Contribution.Evidence.Select(SanitizeDisplayText)])),
        ];
    }

    internal static string SanitizeDisplayText(string? value)
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

    private static bool IsDisplayable(CriterionContribution contribution, RecommendationOptions options)
    {
        if (contribution.Value <= 0.0 || contribution.Value < DisplayThreshold(contribution.Name, options))
        {
            return false;
        }

        return contribution.Evidence.Count > 0 || contribution.Name == "Plot";
    }

    private static string EvidenceText(string name, ImmutableList<string> evidence)
    {
        var label = EvidenceLabel(name);
        if (evidence.Count == 0)
        {
            return label;
        }

        var names = evidence.Select(SanitizeDisplayText).ToList();
        var displayed = names.Count > DisplayNameLimit
            ? string.Format(
                CultureInfo.InvariantCulture,
                "{0} +{1}",
                string.Join(", ", names.Take(DisplayNameLimit)),
                names.Count - DisplayNameLimit)
            : string.Join(", ", names);

        return string.Format(CultureInfo.InvariantCulture, "{0}: {1}", label, displayed);
    }

    private static string EvidenceLabel(string name) =>
        name switch
        {
            "Collection" => "Same collection",
            "Director" => "Same director",
            "Themes" => "Shared themes",
            "Cast" => "Shared cast",
            "Genre" => "Same genres",
            "Plot" => "Similar story",
            _ => name,
        };

    private static double DisplayThreshold(string name, RecommendationOptions options) =>
        name switch
        {
            "Themes" => options.ThemesDisplayThreshold,
            "Plot" => options.PlotDisplayThreshold,
            "Genre" => options.GenreDisplayThreshold,
            _ => 0.0,
        };

    private readonly record struct ScoringTerm(string Name, double Weight, double Value, IReadOnlyList<string> Evidence);

    internal static FastSimilarity ScorePreparedValues(PreparedMovie a, PreparedMovie b, ScoringContext context)
    {
        var weights = context.Options.Weights;
        var collection = CollectionValue(a.CollectionId, b.CollectionId);
        var director = SharedCount(a.Directors, b.Directors) > 0 ? 1.0 : 0.0;
        var themes = ThemesPreparedValue(a, b, context);
        var plot = PlotPreparedValue(a, b, context);
        var cast = Math.Min(1.0, SharedCount(a.Cast, b.Cast) / 2.0);
        var genre = GenreWeights.SimilaritySorted(
            a.GenresSorted,
            a.GenreWeightValues,
            b.GenresSorted,
            b.GenreWeightValues);

        var denominator = context.Denominator;
        var score = weights.Collection * collection / denominator
            + weights.Director * director / denominator
            + weights.Themes * themes / denominator
            + weights.Plot * plot / denominator
            + weights.Cast * cast / denominator
            + weights.Genres * genre / denominator;

        return new FastSimilarity(collection, director, themes, plot, cast, genre, score);
    }

    internal static bool IsStrongPrepared(FastSimilarity similarity, ScoringContext context, IReadOnlySet<string> criteria)
    {
        if (similarity.Score >= context.Options.MinimumSimilarity)
        {
            return true;
        }

        return HasStrongTerm(similarity, context.Options.Weights, criteria);
    }

    private static bool HasStrongTerm(FastSimilarity similarity, SimilarityWeights weights, IReadOnlySet<string> criteria) =>
        IsStrongTerm("Collection", similarity.Collection, weights.Collection, criteria)
            || IsStrongTerm("Director", similarity.Director, weights.Director, criteria)
            || IsStrongTerm("Themes", similarity.Themes, weights.Themes, criteria)
            || IsStrongTerm("Plot", similarity.Plot, weights.Plot, criteria)
            || IsStrongTerm("Cast", similarity.Cast, weights.Cast, criteria)
            || IsStrongTerm("Genre", similarity.Genre, weights.Genres, criteria);

    private static bool IsStrongTerm(string name, double value, double weight, IReadOnlySet<string> criteria) =>
        weight > 0.0 && value >= 1.0 && criteria.Contains(name);

    private static int SharedCount(HashSet<string> left, HashSet<string> right)
    {
        var (smaller, larger) = left.Count <= right.Count ? (left, right) : (right, left);
        var count = 0;
        foreach (var name in smaller)
        {
            if (larger.Contains(name))
            {
                count++;
            }
        }

        return count;
    }

    private static double ThemesPreparedValue(PreparedMovie a, PreparedMovie b, ScoringContext context)
    {
        if (context.KeywordWeights.Count == 0)
        {
            return 0.0;
        }

        if (a.KeywordNormSquares <= 0.0 || b.KeywordNormSquares <= 0.0)
        {
            return 0.0;
        }

        var numerator = 0.0;
        for (var index = 0; index < a.KeywordNames.Length; index++)
        {
            if (b.Keywords.Contains(a.KeywordNames[index]))
            {
                numerator += a.KeywordSquares[index];
            }
        }

        return Math.Min(1.0, numerator / (Math.Sqrt(a.KeywordNormSquares) * Math.Sqrt(b.KeywordNormSquares)) / context.Options.ThemesCeiling);
    }

    private static double PlotPreparedValue(PreparedMovie a, PreparedMovie b, ScoringContext context)
    {
        if (HasUnusablePlotEmbedding(a, b))
        {
            return 0.0;
        }

        if (HasZeroPlotNorm(a, b))
        {
            return 0.0;
        }

        var cosine = PlotCosine(a, b);
        return Math.Clamp((cosine - context.Options.PlotFloor) / (context.Options.PlotCeiling - context.Options.PlotFloor), 0.0, 1.0);
    }

    private static bool HasUnusablePlotEmbedding(PreparedMovie a, PreparedMovie b) =>
        a.PlotEmbedding.IsDefaultOrEmpty
            || b.PlotEmbedding.IsDefaultOrEmpty
            || a.PlotEmbedding.Length != b.PlotEmbedding.Length
            || a.PlotHasNonFinite
            || b.PlotHasNonFinite;

    private static bool HasZeroPlotNorm(PreparedMovie a, PreparedMovie b) =>
        a.PlotNormSquares == 0.0 || b.PlotNormSquares == 0.0;

    private static double PlotCosine(PreparedMovie a, PreparedMovie b)
    {
        var dot = 0.0;
        for (var index = 0; index < a.PlotEmbedding.Length; index++)
        {
            dot += a.PlotEmbedding[index] * b.PlotEmbedding[index];
        }

        return Math.Clamp(dot / (Math.Sqrt(a.PlotNormSquares) * Math.Sqrt(b.PlotNormSquares)), -1.0, 1.0);
    }
}

internal sealed class ScoringContext
{
    public ScoringContext(
        RecommendationOptions options,
        GenreWeights genreWeights,
        IReadOnlyDictionary<string, double> keywordWeights)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(genreWeights);
        ArgumentNullException.ThrowIfNull(keywordWeights);

        Options = options;
        GenreWeights = genreWeights;
        KeywordWeights = keywordWeights;
        var weights = options.Weights;
        Denominator = weights.Collection + weights.Director + weights.Themes + weights.Plot + weights.Cast + weights.Genres;

        var canonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keywordWeights.Keys)
        {
            if (!canonical.ContainsKey(key))
            {
                canonical[key] = key;
            }
        }

        CanonicalKeywords = canonical;
    }

    public RecommendationOptions Options { get; }

    public GenreWeights GenreWeights { get; }

    public IReadOnlyDictionary<string, double> KeywordWeights { get; }

    public double Denominator { get; }

    public IReadOnlyDictionary<string, string> CanonicalKeywords { get; }
}

internal sealed class PreparedMovie
{
    public PreparedMovie(MovieFeatures features, ScoringContext context)
    {
        ArgumentNullException.ThrowIfNull(features);
        ArgumentNullException.ThrowIfNull(context);

        CollectionId = features.CollectionId;
        CollectionName = features.CollectionName;
        CleanedGenres = SimilarityScorer.CleanNamesForRequest(features.Genres);
        Directors = SimilarityScorer.CleanNamesForRequest(features.Directors);
        Cast = SimilarityScorer.CleanNamesForRequest(features.Cast);
        GenresSorted = GenreWeights.PrepareSorted(features.Genres);
        GenreWeightValues = WeighGenres(GenresSorted, context.GenreWeights);
        CleanedKeywords = SimilarityScorer.CleanNamesForRequest(features.KeywordNames);
        Keywords = FilterKeywords(CleanedKeywords, context.KeywordWeights);
        KeywordNames = new string[Keywords.Count];
        Keywords.CopyTo(KeywordNames);
        KeywordSquares = new double[KeywordNames.Length];
        for (var index = 0; index < KeywordNames.Length; index++)
        {
            KeywordSquares[index] = Math.Pow(context.KeywordWeights[KeywordNames[index]], 2);
        }

        PlotEmbedding = features.PlotEmbedding;
        PlotNormSquares = PlotNormSquaresOf(features.PlotEmbedding, out var nonFinite);
        PlotHasNonFinite = nonFinite;
        KeywordNormSquares = KeywordSquares.Sum();
    }

    public int? CollectionId { get; }

    public string? CollectionName { get; }

    public HashSet<string> CleanedGenres { get; }

    public HashSet<string> Directors { get; }

    public HashSet<string> Cast { get; }

    public string[] GenresSorted { get; }

    public double[] GenreWeightValues { get; }

    public HashSet<string> CleanedKeywords { get; }

    public HashSet<string> Keywords { get; }

    public string[] KeywordNames { get; }

    public double[] KeywordSquares { get; }

    public ImmutableArray<float> PlotEmbedding { get; }

    public double PlotNormSquares { get; }

    public bool PlotHasNonFinite { get; }

    public double KeywordNormSquares { get; }

    private static double[] WeighGenres(string[] sorted, GenreWeights genreWeights)
    {
        var weights = new double[sorted.Length];
        for (var index = 0; index < sorted.Length; index++)
        {
            weights[index] = genreWeights.GetWeight(sorted[index]);
        }

        return weights;
    }

    private static HashSet<string> FilterKeywords(
        HashSet<string> cleaned,
        IReadOnlyDictionary<string, double> keywordWeights)
    {
        var filtered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in cleaned)
        {
            if (keywordWeights.ContainsKey(name))
            {
                filtered.Add(name);
            }
        }

        return filtered;
    }

    private static double PlotNormSquaresOf(ImmutableArray<float> embedding, out bool nonFinite)
    {
        nonFinite = false;
        var norm = 0.0;
        for (var index = 0; index < embedding.Length; index++)
        {
            var value = embedding[index];
            if (!float.IsFinite(value))
            {
                nonFinite = true;
            }

            norm += value * value;
        }

        return norm;
    }
}

internal readonly record struct FastSimilarity(
    double Collection,
    double Director,
    double Themes,
    double Plot,
    double Cast,
    double Genre,
    double Score);
