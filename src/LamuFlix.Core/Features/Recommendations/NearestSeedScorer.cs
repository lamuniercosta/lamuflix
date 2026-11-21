using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public enum SeedSignal
{
    Liked,
    WatchedWithoutReview,
    Watchlist,
}

// ReSharper disable NotAccessedPositionalProperty.Global // Public scoring contract consumed by the handler and tests
public sealed record NearestSeedMatch(
    RecommendationLibraryEntry Candidate,
    double Score,
    SeedSignal Signal,
    MovieId SeedMovieId,
    string SeedTitle,
    SimilarityResult Pair);
// ReSharper restore NotAccessedPositionalProperty.Global

public static class NearestSeedScorer
{
    public static ImmutableList<NearestSeedMatch> Rank(
        IReadOnlyList<RecommendationLibraryEntry> candidates,
        IReadOnlyList<RecommendationLibraryEntry> library,
        TasteProfile profile,
        RecommendationOptions options,
        RecommendationRecencyDecay decay,
        GenreWeights genreWeights,
        IReadOnlyDictionary<string, double> keywordWeights,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(decay);
        ArgumentNullException.ThrowIfNull(genreWeights);
        ArgumentNullException.ThrowIfNull(keywordWeights);
        SimilarityScorer.ThrowIfInvalid(options);

        var seeds = PositiveSeeds(library, profile, options, decay);
        if (seeds.Count == 0)
        {
            return ImmutableList<NearestSeedMatch>.Empty;
        }

        var disliked = DislikedSeeds(library);
        var multipliers = options.SeedRankMultipliers ?? [];
        var criteria = StrongReasonCriteria(options);
        var ranked = new List<NearestSeedMatch>(candidates.Count);

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Evaluate(candidate, seeds, disliked, options, multipliers, criteria, genreWeights, keywordWeights) is { } match)
            {
                ranked.Add(match);
            }
        }

        ranked.Sort(CompareByScoreThenMovieId);
        return [.. ranked];
    }

    private static NearestSeedMatch? Evaluate(
        RecommendationLibraryEntry candidate,
        ImmutableList<PositiveSeed> seeds,
        ImmutableList<RecommendationLibraryEntry> disliked,
        RecommendationOptions options,
        IReadOnlyList<double> multipliers,
        IReadOnlySet<string> criteria,
        GenreWeights genreWeights,
        IReadOnlyDictionary<string, double> keywordWeights)
    {
        var pairs = PositivePairs(candidate, seeds, options, genreWeights, keywordWeights);
        if (pairs.Count == 0 || StrongestPair(pairs, options, criteria) is not { } best)
        {
            return null;
        }

        var score = Aggregate(pairs, multipliers)
            - DislikedPenalty(candidate, disliked, options, genreWeights, keywordWeights);

        return new NearestSeedMatch(
            candidate,
            score,
            best.Seed.Kind,
            best.Seed.Entry.Id,
            best.Seed.Entry.Summary.Title,
            best.Similarity);
    }

    private static List<SeedPair> PositivePairs(
        RecommendationLibraryEntry candidate,
        ImmutableList<PositiveSeed> seeds,
        RecommendationOptions options,
        GenreWeights genreWeights,
        IReadOnlyDictionary<string, double> keywordWeights)
    {
        var pairs = new List<SeedPair>(seeds.Count);

        foreach (var seed in seeds)
        {
            if (seed.Entry.Id == candidate.Id)
            {
                continue;
            }

            var similarity = SimilarityScorer.ScoreWithoutValidation(
                candidate.Features,
                seed.Entry.Features,
                options,
                genreWeights,
                keywordWeights);

            if (double.IsFinite(similarity.Score))
            {
                pairs.Add(new SeedPair(seed, similarity));
            }
        }

        return pairs;
    }

    private static SeedPair? StrongestPair(
        List<SeedPair> pairs,
        RecommendationOptions options,
        IReadOnlySet<string> criteria)
    {
        SeedPair? strongest = null;

        foreach (var pair in pairs)
        {
            if (!IsStrong(pair.Similarity, options, criteria))
            {
                continue;
            }

            if (strongest is null || IsStronger(pair, strongest.Value))
            {
                strongest = pair;
            }
        }

        return strongest;
    }

    private static bool IsStronger(SeedPair candidate, SeedPair current)
    {
        var candidateWeighted = candidate.Seed.EffectiveWeight * candidate.Similarity.Score;
        var currentWeighted = current.Seed.EffectiveWeight * current.Similarity.Score;

        if (candidateWeighted > currentWeighted)
        {
            return true;
        }

        if (candidateWeighted < currentWeighted)
        {
            return false;
        }

        return candidate.Seed.Entry.Id.Value < current.Seed.Entry.Id.Value;
    }

    private static bool IsStrong(
        SimilarityResult similarity,
        RecommendationOptions options,
        IReadOnlySet<string> criteria)
    {
        if (similarity.Score >= options.MinimumSimilarity)
        {
            return true;
        }

        foreach (var contribution in similarity.Breakdown)
        {
            if (contribution.Weight > 0.0
                && contribution.Value >= 1.0
                && criteria.Contains(contribution.Name))
            {
                return true;
            }
        }

        return false;
    }

    private static double Aggregate(List<SeedPair> pairs, IReadOnlyList<double> multipliers)
    {
        var products = pairs
            .Select(pair => pair.Seed.EffectiveWeight * pair.Similarity.Score)
            .Where(double.IsFinite)
            .OrderByDescending(product => product)
            .ToArray();

        var sum = 0.0;
        var count = Math.Min(multipliers.Count, products.Length);

        for (var index = 0; index < count; index++)
        {
            sum += products[index] * multipliers[index];
        }

        return sum;
    }

    private static double DislikedPenalty(
        RecommendationLibraryEntry candidate,
        ImmutableList<RecommendationLibraryEntry> disliked,
        RecommendationOptions options,
        GenreWeights genreWeights,
        IReadOnlyDictionary<string, double> keywordWeights)
    {
        var maximum = 0.0;
        var found = false;

        foreach (var seed in disliked)
        {
            if (ScoreDislikedSeed(candidate, seed, options, genreWeights, keywordWeights) is not { } score)
            {
                continue;
            }

            if (!found || score > maximum)
            {
                maximum = score;
                found = true;
            }
        }

        return found ? maximum * options.DislikedSeedWeight : 0.0;
    }

    private static double? ScoreDislikedSeed(
        RecommendationLibraryEntry candidate,
        RecommendationLibraryEntry seed,
        RecommendationOptions options,
        GenreWeights genreWeights,
        IReadOnlyDictionary<string, double> keywordWeights)
    {
        if (seed.Id == candidate.Id)
        {
            return null;
        }

        var similarity = SimilarityScorer.ScoreWithoutValidation(
            candidate.Features,
            seed.Features,
            options,
            genreWeights,
            keywordWeights);

        return double.IsFinite(similarity.Score) ? similarity.Score : null;
    }

    private static ImmutableList<PositiveSeed> PositiveSeeds(
        IReadOnlyList<RecommendationLibraryEntry> library,
        TasteProfile profile,
        RecommendationOptions options,
        RecommendationRecencyDecay decay)
    {
        var signals = new List<SeedSignalEntry>(library.Count);

        foreach (var entry in library)
        {
            if (Signal(entry) is { } signal)
            {
                signals.Add(new SeedSignalEntry(entry, signal));
            }
        }

        if (signals.Count == 0)
        {
            return ImmutableList<PositiveSeed>.Empty;
        }

        var reference = ReferencePlayback(signals, profile.PlaybackByMovieId);
        var builder = ImmutableList.CreateBuilder<PositiveSeed>();

        foreach (var (entry, signal) in signals)
        {
            var weight = BaseWeight(options, signal)
                * RecencyMultiplier(entry.Id, profile.PlaybackByMovieId, reference, decay);
            builder.Add(new PositiveSeed(entry, signal, weight));
        }

        return builder.ToImmutable();
    }

    private static ImmutableList<RecommendationLibraryEntry> DislikedSeeds(
        IReadOnlyList<RecommendationLibraryEntry> library)
    {
        var builder = ImmutableList.CreateBuilder<RecommendationLibraryEntry>();

        foreach (var entry in library)
        {
            if (entry.Review.IsDisliked)
            {
                builder.Add(entry);
            }
        }

        return builder.ToImmutable();
    }

    private static SeedSignal? Signal(RecommendationLibraryEntry entry)
    {
        if (entry.Review.IsDisliked)
        {
            return null;
        }

        if (entry.Review.IsLiked)
        {
            return SeedSignal.Liked;
        }

        if (entry.IsWatched)
        {
            return SeedSignal.WatchedWithoutReview;
        }

        return entry.IsInWatchlist ? SeedSignal.Watchlist : null;
    }

    private static double BaseWeight(RecommendationOptions options, SeedSignal signal) =>
        signal switch
        {
            SeedSignal.Liked => options.SeedWeightLiked,
            SeedSignal.WatchedWithoutReview => options.SeedWeightWatched,
            _ => options.SeedWeightWatchlist,
        };

    private static DateTimeOffset? ReferencePlayback(
        IEnumerable<SeedSignalEntry> seeds,
        ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>> playback)
    {
        DateTimeOffset? reference = null;

        foreach (var seed in seeds)
        {
            if (!TryLatestPlay(playback, seed.Entry.Id, out var playedAt))
            {
                continue;
            }

            if (reference is null || playedAt > reference.Value)
            {
                reference = playedAt;
            }
        }

        return reference;
    }

    private static double RecencyMultiplier(
        MovieId movieId,
        ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>> playback,
        DateTimeOffset? reference,
        RecommendationRecencyDecay decay)
    {
        if (reference is not { } referenceAt
            || !TryLatestPlay(playback, movieId, out var playedAt))
        {
            return RecommendationRecencyDecay.UnscaledWeight;
        }

        return decay.Weight(playedAt, referenceAt);
    }

    private static bool TryLatestPlay(
        ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>> playback,
        MovieId movieId,
        out DateTimeOffset playedAt)
    {
        playedAt = default;

        if (playback.IsEmpty
            || !playback.TryGetValue(movieId, out var timestamps)
            || timestamps.IsDefaultOrEmpty)
        {
            return false;
        }

        playedAt = timestamps.Max();
        return true;
    }

    private static IReadOnlySet<string> StrongReasonCriteria(RecommendationOptions options) =>
        new HashSet<string>(options.StrongReasonCriteria ?? [], StringComparer.OrdinalIgnoreCase);

    private static int CompareByScoreThenMovieId(NearestSeedMatch left, NearestSeedMatch right)
    {
        var byScore = right.Score.CompareTo(left.Score);
        return byScore != 0 ? byScore : left.Candidate.Id.Value.CompareTo(right.Candidate.Id.Value);
    }

    private readonly record struct SeedSignalEntry(RecommendationLibraryEntry Entry, SeedSignal Kind);

    private readonly record struct PositiveSeed(
        RecommendationLibraryEntry Entry,
        SeedSignal Kind,
        double EffectiveWeight);

    private readonly record struct SeedPair(PositiveSeed Seed, SimilarityResult Similarity);
}
