using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Evaluation.Fixtures;

namespace LamuFlix.Evaluation.Replay;

/// <summary>
/// Builds the history-dependent ranking inputs strictly from rows whose
/// timestamp is before the target timestamp, across all users (Q4/TB002).
/// </summary>
internal static class AsOfHistory
{
    public static ImmutableDictionary<MovieId, double> RelevanceScores(
        ImmutableArray<EvaluationCatalogMovie> catalog,
        ImmutableArray<EvaluationEvent> orderedEvents,
        DateTimeOffset targetTimestamp,
        TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var reference = clock.GetUtcNow();
        var movieWeights = new Dictionary<MovieId, double>();
        var totalWeight = 0.0;

        foreach (var asOfEvent in orderedEvents)
        {
            if (asOfEvent.Timestamp >= targetTimestamp)
            {
                continue;
            }

            var weight = RecencyWeight(reference - asOfEvent.Timestamp);
            totalWeight += weight;
            movieWeights[asOfEvent.MovieId] = movieWeights.GetValueOrDefault(asOfEvent.MovieId) + weight;
        }

        var builder = ImmutableDictionary.CreateBuilder<MovieId, double>();
        foreach (var movie in catalog)
        {
            builder[movie.MovieId] = totalWeight > 0.0
                ? movieWeights.GetValueOrDefault(movie.MovieId) / totalWeight
                : 0.0;
        }

        return builder.ToImmutable();
    }

    public static GenreWeights CatalogGenreWeights(ImmutableArray<EvaluationCatalogMovie> catalog)
    {
        var counts = new Dictionary<string, long>(StringComparer.Ordinal);

        foreach (var movie in catalog)
        {
            foreach (var genre in movie.Features.Genres)
            {
                if (string.IsNullOrWhiteSpace(genre))
                {
                    continue;
                }

                counts[genre] = counts.GetValueOrDefault(genre) + 1;
            }
        }

        return GenreWeights.Calculate(new GenreCounts(catalog.Length, counts));
    }

    private static double RecencyWeight(TimeSpan age)
    {
        var days = Math.Max(0.0, age.TotalDays);
        return 1.0 / (1.0 + days);
    }
}
