using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Recommendations;

public static class ProfileSimilarityCalculator
{
    public static double Similarity(
        ImmutableArray<string> candidateGenres,
        ImmutableArray<float> candidateEmbedding,
        TasteProfile profile,
        GenreWeights genreWeights)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(genreWeights);
        ValidateEmbedding(candidateEmbedding, nameof(candidateEmbedding));
        ValidateEmbedding(profile.PlotEmbedding, nameof(profile));

        var channels = new List<double>(2);

        if (TryGenreSimilarity(candidateGenres, profile.GenrePreferences, genreWeights, out var genreSimilarity))
        {
            channels.Add(genreSimilarity);
        }

        if (TryPlotSimilarity(candidateEmbedding, profile.PlotEmbedding, out var plotSimilarity))
        {
            channels.Add(plotSimilarity);
        }

        return channels.Count switch
        {
            0 => 0.0,
            1 => channels[0],
            _ => (channels[0] + channels[1]) / 2.0,
        };
    }

    private static bool TryGenreSimilarity(
        ImmutableArray<string> candidateGenres,
        ImmutableDictionary<string, double> preferences,
        GenreWeights genreWeights,
        out double similarity)
    {
        similarity = 0.0;
        var candidateWeights = CandidateWeights(candidateGenres, genreWeights);

        if (candidateWeights.Count == 0 || !HasPositiveMass(preferences))
        {
            return false;
        }

        var union = new HashSet<string>(candidateWeights.Keys, StringComparer.OrdinalIgnoreCase);
        foreach (var genre in preferences.Keys)
        {
            if (preferences[genre] > 0.0)
            {
                union.Add(genre);
            }
        }

        var intersectionWeight = 0.0;
        var unionWeight = 0.0;

        foreach (var genre in union)
        {
            var candidateWeight = candidateWeights.GetValueOrDefault(genre);
            var preference = preferences.TryGetValue(genre, out var value) ? Math.Max(0.0, value) : 0.0;
            intersectionWeight += Math.Min(candidateWeight, preference);
            unionWeight += Math.Max(candidateWeight, preference);
        }

        if (unionWeight <= 0.0)
        {
            return false;
        }

        similarity = Math.Clamp(intersectionWeight / unionWeight, 0.0, 1.0);
        return true;
    }

    private static Dictionary<string, double> CandidateWeights(
        ImmutableArray<string> candidateGenres,
        GenreWeights genreWeights)
    {
        var weights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        if (candidateGenres.IsDefaultOrEmpty)
        {
            return weights;
        }

        foreach (var genre in candidateGenres)
        {
            if (string.IsNullOrWhiteSpace(genre))
            {
                continue;
            }

            var trimmed = genre.Trim();
            weights[trimmed] = genreWeights.GetWeight(trimmed);
        }

        return weights;
    }

    private static bool HasPositiveMass(ImmutableDictionary<string, double> preferences)
    {
        foreach (var value in preferences.Values)
        {
            if (value > 0.0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryPlotSimilarity(
        ImmutableArray<float> candidateEmbedding,
        ImmutableArray<float> profileEmbedding,
        out double similarity)
    {
        similarity = 0.0;

        if (candidateEmbedding.IsDefaultOrEmpty || profileEmbedding.IsDefaultOrEmpty)
        {
            return false;
        }

        if (!TryCosine(candidateEmbedding, profileEmbedding, out var cosine))
        {
            return false;
        }

        similarity = Math.Clamp(Math.Max(0.0, cosine), 0.0, 1.0);
        return true;
    }

    private static bool TryCosine(
        ImmutableArray<float> first,
        ImmutableArray<float> second,
        out double cosine)
    {
        cosine = 0.0;
        var firstScale = Scale(first);
        var secondScale = Scale(second);

        if (firstScale <= 0.0 || secondScale <= 0.0)
        {
            return false;
        }

        var dot = 0.0;
        var firstNorm = 0.0;
        var secondNorm = 0.0;

        for (var index = 0; index < first.Length; index++)
        {
            var left = first[index] / firstScale;
            var right = second[index] / secondScale;
            dot += left * right;
            firstNorm += left * left;
            secondNorm += right * right;
        }

        var denominator = Math.Sqrt(firstNorm) * Math.Sqrt(secondNorm);
        if (denominator <= 0.0 || !double.IsFinite(denominator))
        {
            return false;
        }

        cosine = dot / denominator;
        return double.IsFinite(cosine);
    }

    private static double Scale(ImmutableArray<float> vector)
    {
        var scale = 0.0;

        foreach (var coordinate in vector)
        {
            var magnitude = Math.Abs(coordinate);
            if (magnitude > scale)
            {
                scale = magnitude;
            }
        }

        return scale;
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
}
