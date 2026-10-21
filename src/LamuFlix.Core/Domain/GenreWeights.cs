using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Domain;

public sealed class GenreWeights
{
    private const double DefaultWeight = 1.0;

    private readonly ImmutableDictionary<string, double> _weights;

    private GenreWeights(ImmutableDictionary<string, double> weights) => _weights = weights;

    public static GenreWeights Calculate(GenreCounts counts)
    {
        ArgumentNullException.ThrowIfNull(counts);

        if (counts.TotalMovies < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(counts),
                counts.TotalMovies,
                "Total movie count cannot be negative.");
        }

        var totalMovies = counts.TotalMovies;
        var builder = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var (genre, frequency) in counts.MoviesWithGenre)
        {
            if (frequency < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(counts),
                    frequency,
                    "Genre movie count cannot be negative.");
            }

            if (frequency > totalMovies)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(counts),
                    frequency,
                    "Genre movie count cannot exceed the total movie count.");
            }

            if (string.IsNullOrWhiteSpace(genre))
            {
                continue;
            }

            builder[genre] = Weight(totalMovies, frequency);
        }

        return new GenreWeights(builder.ToImmutable());
    }

    public double GetWeight(string genre) =>
        string.IsNullOrWhiteSpace(genre) || !_weights.TryGetValue(genre, out var weight)
            ? DefaultWeight
            : weight;

    public double Similarity(IEnumerable<string> first, IEnumerable<string> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var left = Names(first);
        var right = Names(second);
        var union = new HashSet<string>(left, StringComparer.OrdinalIgnoreCase);
        union.UnionWith(right);

        if (union.Count == 0)
        {
            return 0.0;
        }

        var intersectionWeight = 0.0;
        var unionWeight = 0.0;

        foreach (var genre in union.Order(StringComparer.Ordinal))
        {
            var weight = GetWeight(genre);
            unionWeight += weight;

            if (left.Contains(genre) && right.Contains(genre))
            {
                intersectionWeight += weight;
            }
        }

        return Math.Clamp(intersectionWeight / unionWeight, 0.0, 1.0);
    }

    private static HashSet<string> Names(IEnumerable<string> values)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                set.Add(value);
            }
        }

        return set;
    }

    private static double Weight(long totalMovies, long frequency) =>
        Math.Log((totalMovies + 1.0) / (frequency + 1.0)) + 1.0;
}
