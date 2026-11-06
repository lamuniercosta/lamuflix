using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Recommendations;

public static class ProfileSimilarityCalculator
{
    public static double Similarity(
        ImmutableArray<string> candidateGenres,
        ImmutableArray<float> candidateEmbedding,
        TasteProfile profile,
        GenreWeights genreWeights) =>
        Evaluate(candidateGenres, candidateEmbedding, profile, genreWeights).Similarity;

    public static ProfileSimilarityEvidence Evaluate(
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

        if (TryGenreSimilarity(
                candidateGenres,
                profile.GenrePreferences,
                genreWeights,
                out var genreSimilarity,
                out var overlappingGenres))
        {
            channels.Add(genreSimilarity);
        }

        var hasPlotChannel = TryPlotSimilarity(candidateEmbedding, profile.PlotEmbedding, out var plotSimilarity);
        if (hasPlotChannel)
        {
            channels.Add(plotSimilarity);
        }

        var similarity = channels.Count switch
        {
            0 => 0.0,
            1 => channels[0],
            _ => (channels[0] + channels[1]) / 2.0,
        };

        return new ProfileSimilarityEvidence(
            similarity,
            overlappingGenres,
            hasPlotChannel,
            hasPlotChannel ? plotSimilarity : 0.0);
    }

    public static ReviewAffinityEvidence EvaluateReviewAffinity(
        ImmutableArray<string> candidateGenres,
        ImmutableArray<string> candidateCast,
        ImmutableArray<string> candidateDirectors,
        ImmutableArray<float> candidateEmbedding,
        TasteProfile profile,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(weights);
        ArgumentNullException.ThrowIfNull(genreWeights);
        ValidateEmbedding(candidateEmbedding, nameof(candidateEmbedding));

        var candidate = new CandidateChannels(
            candidateGenres,
            candidateCast,
            candidateDirectors,
            candidateEmbedding);

        var liked = Affinity(candidate, profile.Liked, weights, genreWeights);
        var disliked = Affinity(candidate, profile.Disliked, weights, genreWeights);

        return new ReviewAffinityEvidence(
            liked.Value,
            disliked.Value,
            liked.HasChannel || disliked.HasChannel,
            BestLikedSource(candidate, profile.LikedSources, weights, genreWeights));
    }

    private static LikedSourceEvidence? BestLikedSource(
        CandidateChannels candidate,
        ImmutableArray<LikedReviewSource> sources,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        if (sources.IsDefaultOrEmpty)
        {
            return null;
        }

        LikedSourceEvidence? best = null;

        foreach (var source in sources)
        {
            var affinity = Affinity(candidate, source.Channels, weights, genreWeights);

            if (!affinity.HasChannel || affinity.Value <= 0.0)
            {
                continue;
            }

            if (best is { } current && !IsImproved(affinity.Value, source.MovieId, current))
            {
                continue;
            }

            best = new LikedSourceEvidence(source.MovieId, source.Title, affinity.Value);
        }

        return best;
    }

    private static bool IsImproved(double affinity, MovieId movieId, LikedSourceEvidence current)
    {
        var comparison = affinity.CompareTo(current.Affinity);
        return comparison > 0 || (comparison == 0 && movieId.Value <= current.MovieId.Value);
    }

    private static AffinityResult Affinity(
        CandidateChannels candidate,
        ReviewProfileChannels channels,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        Span<double> similarities = stackalloc double[4];
        Span<double> channelWeights = stackalloc double[4];
        var count = 0;

        Accumulate(
            similarities,
            channelWeights,
            ref count,
            weights.ReviewPlotWeight,
            TryPlotSimilarity(candidate.Embedding, channels.PlotEmbedding, out var plot) ? plot : double.NaN);

        Accumulate(
            similarities,
            channelWeights,
            ref count,
            weights.Genres,
            TryWeightedJaccard(
                candidate.Genres,
                channels.Genres,
                genreWeights.GetWeight,
                out var genres)
                ? genres
                : double.NaN);

        Accumulate(
            similarities,
            channelWeights,
            ref count,
            weights.Cast,
            TryWeightedJaccard(candidate.Cast, channels.Cast, UnitWeight, out var cast) ? cast : double.NaN);

        Accumulate(
            similarities,
            channelWeights,
            ref count,
            weights.Director,
            TryWeightedJaccard(candidate.Directors, channels.Directors, UnitWeight, out var directors)
                ? directors
                : double.NaN);

        if (count == 0)
        {
            return new AffinityResult(0.0, false);
        }

        var totalWeight = 0.0;
        var numerator = 0.0;

        for (var index = 0; index < count; index++)
        {
            totalWeight += channelWeights[index];
            numerator += channelWeights[index] * similarities[index];
        }

        if (totalWeight <= 0.0)
        {
            return new AffinityResult(0.0, false);
        }

        return new AffinityResult(Math.Clamp(numerator / totalWeight, 0.0, 1.0), true);
    }

    private static void Accumulate(
        Span<double> similarities,
        Span<double> channelWeights,
        ref int count,
        double weight,
        double similarity)
    {
        if (!double.IsFinite(similarity) || weight <= 0.0 || count >= similarities.Length)
        {
            return;
        }

        similarities[count] = similarity;
        channelWeights[count] = weight;
        count++;
    }

    private static double UnitWeight(string _) => 1.0;

    private static bool TryGenreSimilarity(
        ImmutableArray<string> candidateGenres,
        ImmutableDictionary<string, double> preferences,
        GenreWeights genreWeights,
        out double similarity,
        out ImmutableArray<string> overlappingGenres)
    {
        overlappingGenres = ImmutableArray<string>.Empty;

        if (!TryWeightedJaccard(candidateGenres, preferences, genreWeights.GetWeight, out similarity))
        {
            return false;
        }

        overlappingGenres = OverlappingGenres(candidateGenres, preferences);
        return true;
    }

    private static bool TryWeightedJaccard(
        ImmutableArray<string> candidateValues,
        ImmutableDictionary<string, double> preferences,
        Func<string, double> rarity,
        out double similarity)
    {
        similarity = 0.0;

        var candidateWeights = Weights(candidateValues, rarity);
        var preferenceWeights = PositiveWeights(preferences);

        if (candidateWeights.Count == 0 || preferenceWeights.Count == 0)
        {
            return false;
        }

        var union = new HashSet<string>(candidateWeights.Keys, StringComparer.OrdinalIgnoreCase);
        union.UnionWith(preferenceWeights.Keys);

        var intersectionWeight = 0.0;
        var unionWeight = 0.0;

        foreach (var name in union)
        {
            var candidateWeight = candidateWeights.GetValueOrDefault(name);
            var preferenceWeight = preferenceWeights.GetValueOrDefault(name);
            intersectionWeight += Math.Min(candidateWeight, preferenceWeight);
            unionWeight += Math.Max(candidateWeight, preferenceWeight);
        }

        if (unionWeight <= 0.0)
        {
            return false;
        }

        similarity = Math.Clamp(intersectionWeight / unionWeight, 0.0, 1.0);
        return true;
    }

    private static Dictionary<string, double> Weights(ImmutableArray<string> values, Func<string, double> scale)
    {
        var weights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        if (values.IsDefaultOrEmpty)
        {
            return weights;
        }

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmed = value.Trim();
            weights[trimmed] = scale(trimmed);
        }

        return weights;
    }

    private static Dictionary<string, double> PositiveWeights(ImmutableDictionary<string, double> preferences)
    {
        var weights = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, value) in preferences)
        {
            if (value > 0.0 && !string.IsNullOrWhiteSpace(name))
            {
                weights[name.Trim()] = value;
            }
        }

        return weights;
    }

    private static ImmutableArray<string> OverlappingGenres(
        ImmutableArray<string> candidateGenres,
        ImmutableDictionary<string, double> preferences)
    {
        if (candidateGenres.IsDefaultOrEmpty || preferences.IsEmpty)
        {
            return ImmutableArray<string>.Empty;
        }

        var matched = new List<string>();

        foreach (var genre in candidateGenres)
        {
            if (string.IsNullOrWhiteSpace(genre))
            {
                continue;
            }

            if (!preferences.TryGetValue(genre.Trim(), out var preference) || preference <= 0.0)
            {
                continue;
            }

            matched.Add(genre.Trim());
        }

        return
        [
            .. matched
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(genre => genre, StringComparer.OrdinalIgnoreCase)
                .ThenBy(genre => genre, StringComparer.Ordinal),
        ];
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

    private readonly record struct CandidateChannels(
        ImmutableArray<string> Genres,
        ImmutableArray<string> Cast,
        ImmutableArray<string> Directors,
        ImmutableArray<float> Embedding);

    private readonly record struct AffinityResult(double Value, bool HasChannel);
}
