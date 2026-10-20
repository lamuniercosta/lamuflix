using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Domain;

public sealed class GenreWeights
{
    private const double DefaultWeight = 1.0;

    private readonly ImmutableDictionary<int, double> _weights;

    private GenreWeights(ImmutableDictionary<int, double> weights) => _weights = weights;

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
        var builder = ImmutableDictionary.CreateBuilder<int, double>();

        foreach (var (genreId, frequency) in counts.MoviesWithGenre)
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

            builder[genreId] = Weight(totalMovies, frequency);
        }

        return new GenreWeights(builder.ToImmutable());
    }

    public double GetWeight(int genreId) =>
        _weights.TryGetValue(genreId, out var weight) ? weight : DefaultWeight;

    public double Similarity(IEnumerable<int> first, IEnumerable<int> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var left = first.ToHashSet();
        var right = second.ToHashSet();
        var union = new HashSet<int>(left);
        union.UnionWith(right);

        if (union.Count == 0)
        {
            return 0.0;
        }

        var intersectionWeight = 0.0;
        var unionWeight = 0.0;

        foreach (var genreId in union.Order())
        {
            var weight = GetWeight(genreId);
            unionWeight += weight;

            if (left.Contains(genreId) && right.Contains(genreId))
            {
                intersectionWeight += weight;
            }
        }

        return Math.Clamp(intersectionWeight / unionWeight, 0.0, 1.0);
    }

    private static double Weight(long totalMovies, long frequency) =>
        Math.Log((totalMovies + 1.0) / (frequency + 1.0)) + 1.0;
}
