using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class TasteProfileCalculator
{
    private readonly TimeProvider _timeProvider;
    private readonly double _recencyDecayLambda;

    public TasteProfileCalculator(TimeProvider timeProvider, RecommendationOptions options)
    {
        _timeProvider = timeProvider;
        _recencyDecayLambda = options.RecencyDecayLambda;
    }

    public TasteProfile Calculate(ImmutableArray<TasteProfileMovie> movies, GenreWeights genreWeights)
    {
        var now = _timeProvider.GetUtcNow();
        var signals = BuildSignals(movies, now);

        return new TasteProfile(
            ComputeGenrePreferences(signals, genreWeights),
            ComputePlotCentroid(signals));
    }

    private static ImmutableArray<Signal> BuildSignals(
        ImmutableArray<TasteProfileMovie> movies,
        DateTimeOffset now)
    {
        if (movies.IsDefaultOrEmpty)
        {
            return ImmutableArray<Signal>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<Signal>();

        foreach (var movie in movies)
        {
            var genres = NormalizeGenres(movie.Genres);
            var embedding = movie.PlotEmbedding.IsDefaultOrEmpty
                ? ImmutableArray<float>.Empty
                : movie.PlotEmbedding;

            if (movie.PlayedAt.IsDefaultOrEmpty)
            {
                if (movie.IsWatched)
                {
                    builder.Add(new Signal(genres, embedding, 0.0));
                }

                continue;
            }

            foreach (var playedAt in movie.PlayedAt)
            {
                builder.Add(new Signal(genres, embedding, ComputeAge(now, playedAt)));
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<string> NormalizeGenres(ImmutableArray<string> genres)
    {
        if (genres.IsDefaultOrEmpty)
        {
            return ImmutableArray<string>.Empty;
        }

        var canonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var genre in genres)
        {
            if (string.IsNullOrWhiteSpace(genre))
            {
                continue;
            }

            var trimmed = genre.Trim();
            if (!canonical.TryGetValue(trimmed, out var current) || string.CompareOrdinal(trimmed, current) < 0)
            {
                canonical[trimmed] = trimmed;
            }
        }

        return [.. canonical.Values];
    }

    private static double ComputeAge(DateTimeOffset now, DateTimeOffset playedAt) =>
        Math.Max(0.0, (now - playedAt).TotalDays);

    private ImmutableDictionary<string, double> ComputeGenrePreferences(
        ImmutableArray<Signal> signals,
        GenreWeights genreWeights)
    {
        if (!TryFindMinimumAge(signals, static signal => !signal.Genres.IsEmpty, out var minimumAge))
        {
            return ImmutableDictionary.Create<string, double>(StringComparer.OrdinalIgnoreCase);
        }

        var numerators = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var canonicalNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var denominator = 0.0;

        foreach (var signal in signals)
        {
            if (signal.Genres.IsEmpty)
            {
                continue;
            }

            var weight = ScaledWeight(signal.Age, minimumAge);
            denominator += weight;

            foreach (var genre in signal.Genres)
            {
                TrackCanonicalName(canonicalNames, genre);
                numerators[genre] = numerators.GetValueOrDefault(genre) + (weight * genreWeights.GetWeight(genre));
            }
        }

        return BuildPreferences(numerators, canonicalNames, denominator);
    }

    private static ImmutableDictionary<string, double> BuildPreferences(
        Dictionary<string, double> numerators,
        Dictionary<string, string> canonicalNames,
        double denominator)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var genre in numerators.Keys.OrderBy(name => canonicalNames[name], StringComparer.Ordinal))
        {
            var preference = numerators[genre] / denominator;
            if (preference <= 0.0)
            {
                continue;
            }

            builder[canonicalNames[genre]] = preference;
        }

        return builder.ToImmutable();
    }

    private static void TrackCanonicalName(Dictionary<string, string> canonicalNames, string genre)
    {
        if (!canonicalNames.TryGetValue(genre, out var current) || string.CompareOrdinal(genre, current) < 0)
        {
            canonicalNames[genre] = genre;
        }
    }

    private ImmutableArray<float> ComputePlotCentroid(ImmutableArray<Signal> signals)
    {
        if (!TryFindMinimumAge(signals, static signal => !signal.PlotEmbedding.IsEmpty, out var minimumAge))
        {
            return ImmutableArray<float>.Empty;
        }

        var sums = new double[EmbeddingDimensions.Value];
        var denominator = 0.0;

        foreach (var signal in signals)
        {
            if (signal.PlotEmbedding.IsEmpty)
            {
                continue;
            }

            var weight = ScaledWeight(signal.Age, minimumAge);
            denominator += weight;

            for (var index = 0; index < sums.Length; index++)
            {
                sums[index] += weight * signal.PlotEmbedding[index];
            }
        }

        return BuildCentroid(sums, denominator);
    }

    private static ImmutableArray<float> BuildCentroid(double[] sums, double denominator)
    {
        var builder = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);
        var allZero = true;

        foreach (var sum in sums)
        {
            var coordinate = (float)(sum / denominator);
            if (coordinate != 0.0f)
            {
                allZero = false;
            }

            builder.Add(coordinate);
        }

        return allZero ? ImmutableArray<float>.Empty : builder.MoveToImmutable();
    }

    private double ScaledWeight(double age, double minimumAge) =>
        Math.Exp(-_recencyDecayLambda * (age - minimumAge));

    private static bool TryFindMinimumAge(
        ImmutableArray<Signal> signals,
        Func<Signal, bool> contributes,
        out double minimumAge)
    {
        minimumAge = double.MaxValue;
        var found = false;

        foreach (var signal in signals)
        {
            if (!contributes(signal))
            {
                continue;
            }

            found = true;
            if (signal.Age < minimumAge)
            {
                minimumAge = signal.Age;
            }
        }

        return found;
    }

    private readonly record struct Signal(
        ImmutableArray<string> Genres,
        ImmutableArray<float> PlotEmbedding,
        double Age);
}
