using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;

namespace LamuFlix.Evaluation.RealLibrary;

public static class EmbeddingInputComposer
{
    public static string Compose(
        Movie movie,
        PlotEmbeddingVariant variant,
        IReadOnlyDictionary<string, double>? keywordWeights)
    {
        ArgumentNullException.ThrowIfNull(movie);

        return variant switch
        {
            PlotEmbeddingVariant.BaselinePlot => PlotOf(movie),
            PlotEmbeddingVariant.AlternateModel => PlotOf(movie),
            PlotEmbeddingVariant.EnrichedText => EnrichedTextOf(movie, keywordWeights),
            _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, "Unknown plot embedding variant."),
        };
    }

    public static IReadOnlyList<string> SelectKeywords(
        IEnumerable<string> candidateNames,
        IReadOnlyDictionary<string, double> keywordWeights,
        int cap)
    {
        ArgumentNullException.ThrowIfNull(candidateNames);
        ArgumentNullException.ThrowIfNull(keywordWeights);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(cap, 0);

        return
        [
            .. candidateNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderBy(name => name, StringComparer.Ordinal).First())
                .Where(name => keywordWeights.ContainsKey(name))
                .Select(name => keywordWeights.Keys.First(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(name => keywordWeights[name])
                .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(name => name, StringComparer.Ordinal)
                .Take(cap),
        ];
    }

    private static string EnrichedTextOf(Movie movie, IReadOnlyDictionary<string, double>? keywordWeights)
    {
        ArgumentNullException.ThrowIfNull(keywordWeights);

        var metadata = movie.Metadata;
        var genres = metadata is null
            ? []
            : metadata.Genres.OrderBy(name => name, StringComparer.Ordinal).ToImmutableArray();
        var keywords = metadata?.Signals?.Keywords is null
            ? []
            : SelectKeywords(
                metadata.Signals.Keywords.Select(keyword => keyword.Name),
                keywordWeights,
                PlotEmbeddingExperiment.EnrichedKeywordCap);

        return string.Join(
            "\n",
            $"Title: {movie.Title}",
            $"Genres: {string.Join(", ", genres)}",
            $"Keywords: {string.Join(", ", keywords)}",
            $"Plot: {PlotOf(movie)}");
    }

    private static string PlotOf(Movie movie) => movie.Metadata?.Synopsis ?? string.Empty;
}
