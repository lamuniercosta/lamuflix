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
        var context = new ScoringContext(options, genreWeights, keywordWeights);
        var preparedByEntry = PrepareLibrary(library, context);
        var seedsPrepared = PrepareSeeds(seeds, preparedByEntry);
        var dislikedPrepared = PrepareDisliked(disliked, preparedByEntry);
        var ranked = new List<NearestSeedMatch>(candidates.Count);

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Evaluate(candidate, seedsPrepared, dislikedPrepared, preparedByEntry, context, multipliers, criteria) is { } match)
            {
                ranked.Add(match);
            }
        }

        ranked.Sort(CompareByScoreThenMovieId);
        return [.. ranked];
    }

    private static Dictionary<RecommendationLibraryEntry, PreparedMovie> PrepareLibrary(
        IReadOnlyList<RecommendationLibraryEntry> library,
        ScoringContext context)
    {
        var prepared = new Dictionary<RecommendationLibraryEntry, PreparedMovie>(ReferenceEqualityComparer.Instance);
        foreach (var entry in library)
        {
            prepared[entry] = new PreparedMovie(entry.Features, context);
        }

        return prepared;
    }

    private static List<(PositiveSeed Seed, PreparedMovie Prepared)> PrepareSeeds(
        ImmutableList<PositiveSeed> seeds,
        Dictionary<RecommendationLibraryEntry, PreparedMovie> preparedByEntry)
    {
        var prepared = new List<(PositiveSeed Seed, PreparedMovie Prepared)>(seeds.Count);
        foreach (var seed in seeds)
        {
            prepared.Add((seed, preparedByEntry[seed.Entry]));
        }

        return prepared;
    }

    private static List<(RecommendationLibraryEntry Entry, PreparedMovie Prepared)> PrepareDisliked(
        ImmutableList<RecommendationLibraryEntry> disliked,
        Dictionary<RecommendationLibraryEntry, PreparedMovie> preparedByEntry)
    {
        var prepared = new List<(RecommendationLibraryEntry Entry, PreparedMovie Prepared)>(disliked.Count);
        foreach (var entry in disliked)
        {
            prepared.Add((entry, preparedByEntry[entry]));
        }

        return prepared;
    }

    private static NearestSeedMatch? Evaluate(
        RecommendationLibraryEntry candidate,
        List<(PositiveSeed Seed, PreparedMovie Prepared)> seeds,
        List<(RecommendationLibraryEntry Entry, PreparedMovie Prepared)> disliked,
        Dictionary<RecommendationLibraryEntry, PreparedMovie> preparedByEntry,
        ScoringContext context,
        IReadOnlyList<double> multipliers,
        IReadOnlySet<string> criteria)
    {
        var pairs = PositivePairs(candidate, seeds, preparedByEntry[candidate], context);
        if (pairs.Count == 0 || StrongestPair(pairs, context, criteria) is not { } best)
        {
            return null;
        }

        var score = Aggregate(pairs, multipliers)
            - DislikedPenalty(candidate.Id, preparedByEntry[candidate], disliked, context);

        var similarity = SimilarityScorer.ScorePreparedFull(
            preparedByEntry[candidate],
            preparedByEntry[best.Seed.Entry],
            context);

        return new NearestSeedMatch(
            candidate,
            score,
            best.Seed.Kind,
            best.Seed.Entry.Id,
            best.Seed.Entry.Summary.Title,
            similarity);
    }

    private static List<SeedPair> PositivePairs(
        RecommendationLibraryEntry candidate,
        List<(PositiveSeed Seed, PreparedMovie Prepared)> seeds,
        PreparedMovie candidatePrepared,
        ScoringContext context)
    {
        var pairs = new List<SeedPair>(seeds.Count);

        foreach (var (seed, prepared) in seeds)
        {
            if (seed.Entry.Id == candidate.Id)
            {
                continue;
            }

            var similarity = SimilarityScorer.ScorePreparedValues(candidatePrepared, prepared, context);

            if (double.IsFinite(similarity.Score))
            {
                pairs.Add(new SeedPair(seed, similarity));
            }
        }

        return pairs;
    }

    private static SeedPair? StrongestPair(
        List<SeedPair> pairs,
        ScoringContext context,
        IReadOnlySet<string> criteria)
    {
        SeedPair? strongest = null;

        foreach (var pair in pairs)
        {
            if (!SimilarityScorer.IsStrongPrepared(pair.Similarity, context, criteria))
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

    private static double Aggregate(List<SeedPair> pairs, IReadOnlyList<double> multipliers)
    {
        if (multipliers.Count == 0 || pairs.Count == 0)
        {
            return 0.0;
        }

        Span<int> top = stackalloc int[Math.Min(multipliers.Count, pairs.Count)];
        var selected = SelectTopPairs(pairs, top);

        var sum = 0.0;
        for (var rank = 0; rank < selected; rank++)
        {
            sum += pairs[top[rank]].Seed.EffectiveWeight * pairs[top[rank]].Similarity.Score * multipliers[rank];
        }

        return sum;
    }

    private static int SelectTopPairs(List<SeedPair> pairs, Span<int> top)
    {
        var selected = 0;
        for (var index = 0; index < pairs.Count; index++)
        {
            var product = pairs[index].Seed.EffectiveWeight * pairs[index].Similarity.Score;
            if (double.IsFinite(product))
            {
                selected = InsertTopPair(pairs, top, selected, index, product);
            }
        }

        return selected;
    }

    private static int InsertTopPair(
        List<SeedPair> pairs,
        Span<int> top,
        int selected,
        int index,
        double product)
    {
        var rank = selected;
        while (rank > 0 && OutranksSeedPair(product, index, pairs, top[rank - 1]))
        {
            rank--;
        }

        if (rank >= top.Length)
        {
            return selected;
        }

        for (var move = Math.Min(selected, top.Length - 1); move > rank; move--)
        {
            top[move] = top[move - 1];
        }

        top[rank] = index;
        return Math.Min(selected + 1, top.Length);
    }

    private static bool OutranksSeedPair(
        double product,
        int index,
        List<SeedPair> pairs,
        int incumbentIndex)
    {
        var incumbentProduct = pairs[incumbentIndex].Seed.EffectiveWeight * pairs[incumbentIndex].Similarity.Score;
        return product > incumbentProduct
            || (product.CompareTo(incumbentProduct) == 0 && index < incumbentIndex);
    }

    private static double DislikedPenalty(
        MovieId candidateId,
        PreparedMovie candidate,
        List<(RecommendationLibraryEntry Entry, PreparedMovie Prepared)> disliked,
        ScoringContext context)
    {
        var maximum = 0.0;
        var found = false;

        foreach (var (entry, prepared) in disliked)
        {
            var score = TryDislikedScore(candidateId, candidate, entry, prepared, context);
            (maximum, found) = TrackMaximum(maximum, found, score);
        }

        return found ? maximum * context.Options.DislikedSeedWeight : 0.0;
    }

    private static double? TryDislikedScore(
        MovieId candidateId,
        PreparedMovie candidate,
        RecommendationLibraryEntry entry,
        PreparedMovie prepared,
        ScoringContext context)
    {
        if (entry.Id == candidateId)
        {
            return null;
        }

        var similarity = SimilarityScorer.ScorePreparedValues(candidate, prepared, context);
        return double.IsFinite(similarity.Score) ? similarity.Score : null;
    }

    private static (double Maximum, bool Found) TrackMaximum(double maximum, bool found, double? score)
    {
        if (score is null)
        {
            return (maximum, found);
        }

        return !found || score.Value > maximum ? (score.Value, true) : (maximum, found);
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

    private readonly record struct SeedPair(PositiveSeed Seed, FastSimilarity Similarity);
}
