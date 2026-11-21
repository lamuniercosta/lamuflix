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
    private readonly RecommendationRecencyDecay decay;

    public TasteProfileCalculator(TimeProvider timeProvider, RecommendationOptions options)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);

        decay = new RecommendationRecencyDecay(timeProvider, options);
    }

    public TasteProfile Calculate(ImmutableArray<TasteProfileMovie> movies, GenreWeights genreWeights)
    {
        ArgumentNullException.ThrowIfNull(genreWeights);
        ValidateMovies(movies);

        var now = decay.UtcNow;
        var signals = BuildSignals(movies, now);

        return new TasteProfile(ComputeGenrePreferences(signals, genreWeights))
        {
            PlaybackByMovieId = PlaybackByMovieId(movies),
        };
    }

    private static ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>> PlaybackByMovieId(
        ImmutableArray<TasteProfileMovie> movies)
    {
        if (movies.IsDefaultOrEmpty)
        {
            return ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>.Empty;
        }

        var builder = ImmutableDictionary.CreateBuilder<MovieId, ImmutableArray<DateTimeOffset>>();

        foreach (var movie in movies)
        {
            if (movie.PlayedAt.IsDefaultOrEmpty)
            {
                continue;
            }

            builder[movie.MovieId] = [.. movie.PlayedAt];
        }

        return builder.ToImmutable();
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
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract - collection expressions can carry default rows.
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

        foreach (var movie in movies.Where(movie => !movie.Review.IsDisliked))
        {
            AddMovieSignals(builder, movie, now);
        }

        return builder.ToImmutable();
    }

    private static void AddMovieSignals(ImmutableArray<Signal>.Builder builder, TasteProfileMovie movie, DateTimeOffset now)
    {
        var genres = NormalizeGenres(movie.Genres);

        if (movie.PlayedAt.IsDefaultOrEmpty)
        {
            if (movie.IsWatched)
            {
                builder.Add(new Signal(genres, 0.0));
            }

            return;
        }

        foreach (var playedAt in movie.PlayedAt)
        {
            builder.Add(new Signal(genres, ComputeAge(now, playedAt)));
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
        RecommendationRecencyDecay.AgeInDays(now, playedAt);

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

    private double ScaledWeight(double age, double minimumAge) =>
        decay.RelativeWeight(age, minimumAge);

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
        double Age);
}
