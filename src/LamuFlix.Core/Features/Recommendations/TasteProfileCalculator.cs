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
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);

        if (!double.IsFinite(options.RecencyDecayLambda) || options.RecencyDecayLambda <= 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.RecencyDecayLambda,
                "RecencyDecayLambda must be finite and greater than 0.0.");
        }

        _timeProvider = timeProvider;
        _recencyDecayLambda = options.RecencyDecayLambda;
    }

    public TasteProfile Calculate(ImmutableArray<TasteProfileMovie> movies, GenreWeights genreWeights)
    {
        ArgumentNullException.ThrowIfNull(genreWeights);
        ValidateMovies(movies);

        var now = _timeProvider.GetUtcNow();
        var signals = BuildSignals(movies, now);

        return new TasteProfile(
            ComputeGenrePreferences(signals, genreWeights),
            ComputePlotCentroid(signals));
    }

    private static void ValidateMovies(ImmutableArray<TasteProfileMovie> movies)
    {
        if (movies.IsDefaultOrEmpty)
        {
            return;
        }

        var movieIds = new HashSet<MovieId>();

        foreach (var movie in movies)
        {
            if (movie is null)
            {
                throw new ArgumentException("Movie rows cannot contain null entries.", nameof(movies));
            }

            if (!movieIds.Add(movie.MovieId))
            {
                throw new ArgumentException(
                    $"Movie rows cannot repeat movie id {movie.MovieId.Value}.",
                    nameof(movies));
            }

            ValidateEmbedding(movie.PlotEmbedding, nameof(movies));
        }
    }

    private static void ValidateEmbedding(ImmutableArray<float> embedding, string paramName)
    {
        if (embedding.IsDefaultOrEmpty)
        {
            return;
        }

        if (embedding.Length != EmbeddingDimensions.Value)
        {
            throw new ArgumentException(
                $"Plot embeddings must contain exactly {EmbeddingDimensions.Value} coordinates.",
                paramName);
        }

        foreach (var coordinate in embedding)
        {
            if (!float.IsFinite(coordinate))
            {
                throw new ArgumentException("Plot embeddings must contain only finite coordinates.", paramName);
            }
        }
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
            AddMovieSignals(builder, movie, now);
        }

        return builder.ToImmutable();
    }

    private static void AddMovieSignals(ImmutableArray<Signal>.Builder builder, TasteProfileMovie movie, DateTimeOffset now)
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

            return;
        }

        foreach (var playedAt in movie.PlayedAt)
        {
            builder.Add(new Signal(genres, embedding, ComputeAge(now, playedAt)));
        }
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
