using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

internal sealed record OutsideLibraryEligibleContribution(
    int SourceMovieId,
    string SeedTitle,
    long Weight,
    int TmdbId,
    string CandidateTitle,
    int? CandidateReleaseYear,
    string? CandidatePosterPath);

internal sealed record OutsideLibraryRankedCandidate(
    int TmdbId,
    long Score,
    int SeedCount,
    OutsideLibraryRecommendation Card);

public static class OutsideLibraryRanking
{
    private const int MaxCards = 20;

    internal static IReadOnlyList<OutsideLibraryRecommendation> Rank(
        OutsideLibraryRecommendationSnapshot snapshot,
        RecommendationOptions options) =>
        [.. Ranked(snapshot, options)
            .Take(MaxCards)
            .Select(candidate => candidate.Card)];

    public static IReadOnlyList<OutsideLibraryRecommendation> RankAdmitted(
        OutsideLibraryRecommendationSnapshot snapshot,
        RecommendationOptions options,
        IReadOnlyDictionary<int, CandidateImdbEvidence> evidenceByTmdbId,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(evidenceByTmdbId);

        return OutsideLibraryCollectionSelection.Admit(
            snapshot,
            options,
            new OutsideLibraryEvidenceLookup(
                evidenceByTmdbId,
                (timeProvider ?? TimeProvider.System).GetUtcNow()));
    }

    public static async Task<IReadOnlyList<OutsideLibraryRecommendation>> RankAdmittedAsync(
        OutsideLibraryRecommendationSnapshot snapshot,
        RecommendationOptions options,
        IReadOnlyDictionary<int, CandidateImdbEvidence> evidenceByTmdbId,
        Func<int, CancellationToken, ValueTask<CandidateImdbEvidence?>>? resolveEvidenceAsync,
        CancellationToken ct,
        TimeProvider? timeProvider = null,
        IReadOnlyDictionary<int, CandidateImdbEvidence>? fresherEvidence = null)
    {
        ArgumentNullException.ThrowIfNull(evidenceByTmdbId);

        return await OutsideLibraryCollectionSelection.AdmitAsync(
            snapshot,
            options,
            new OutsideLibraryEvidenceLookup(
                evidenceByTmdbId,
                (timeProvider ?? TimeProvider.System).GetUtcNow(),
                fresherEvidence: fresherEvidence,
                resolveEvidenceAsync: resolveEvidenceAsync),
            ct);
    }

    internal static IOrderedEnumerable<OutsideLibraryRankedCandidate> Ranked(
        OutsideLibraryRecommendationSnapshot snapshot,
        RecommendationOptions options) =>
        Ordered(RankAll(snapshot, options));

    internal static IReadOnlyList<OutsideLibraryRankedCandidate> RankAll(
        OutsideLibraryRecommendationSnapshot snapshot,
        RecommendationOptions options)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(options);

        var eligible = Eligible(snapshot.Contributions, options);
        var results = new List<OutsideLibraryRankedCandidate>();
        foreach (var group in eligible.GroupBy(contribution => contribution.TmdbId))
        {
            OutsideLibraryEligibleContribution[] contributors = [.. group];
            var canonical = Canonical(contributors);
            if (IsInLibrary(snapshot.Library, canonical))
            {
                continue;
            }

            var seeds = ByPreference(contributors)
                .DistinctBy(contribution => contribution.SourceMovieId)
                .ToArray();
            if (seeds.Length < 2)
            {
                continue;
            }

            var reason = OutsideLibraryReasonFormatter.Format(
                [.. seeds.Take(2).Select(seed => seed.SeedTitle)]);
            var card = new OutsideLibraryRecommendation(
                canonical.TmdbId,
                SimilarityScorer.SanitizeDisplayText(canonical.CandidateTitle),
                canonical.CandidateReleaseYear,
                canonical.CandidatePosterPath,
                reason);
            results.Add(new OutsideLibraryRankedCandidate(
                canonical.TmdbId,
                seeds.Sum(seed => seed.Weight),
                seeds.Length,
                card));
        }

        return results;
    }

    private static IOrderedEnumerable<OutsideLibraryRankedCandidate> Ordered(
        IEnumerable<OutsideLibraryRankedCandidate> candidates) =>
        candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.TmdbId);

    private static List<OutsideLibraryEligibleContribution> Eligible(
        IReadOnlyList<OutsideLibraryRecommendationContribution> contributions,
        RecommendationOptions options)
    {
        var eligible = new List<OutsideLibraryEligibleContribution>(contributions.Count);
        var seen = new HashSet<(int SourceId, int TmdbId)>();

        foreach (var contribution in contributions)
        {
            if (IsSuppressed(contribution))
            {
                continue;
            }

            var weight = WeightFor(contribution, options);
            if (weight <= 0 || string.IsNullOrWhiteSpace(contribution.CandidateTitle))
            {
                continue;
            }

            var sourceId = contribution.SourceMovieId.Value;
            if (!seen.Add((sourceId, contribution.TmdbId)))
            {
                continue;
            }

            eligible.Add(new OutsideLibraryEligibleContribution(
                sourceId,
                contribution.SeedTitle,
                weight,
                contribution.TmdbId,
                contribution.CandidateTitle,
                contribution.CandidateReleaseYear,
                contribution.CandidatePosterPath));
        }

        return eligible;
    }

    private static bool IsSuppressed(OutsideLibraryRecommendationContribution contribution) =>
        contribution.Review.IsDisliked
            || (contribution.IsInWatchlist
                && !contribution.IsWatched
                && !contribution.Review.IsLiked);

    private static long WeightFor(
        OutsideLibraryRecommendationContribution contribution,
        RecommendationOptions options) =>
        contribution.Review.IsLiked
            ? options.DiscoveryWeightLiked
            : contribution.IsWatched ? options.DiscoveryWeightWatched : 0;

    private static IOrderedEnumerable<OutsideLibraryEligibleContribution> ByPreference(
        IEnumerable<OutsideLibraryEligibleContribution> contributors) =>
        contributors
            .OrderByDescending(contribution => contribution.Weight)
            .ThenBy(contribution => contribution.SourceMovieId);

    private static OutsideLibraryEligibleContribution Canonical(
        IReadOnlyList<OutsideLibraryEligibleContribution> contributors) =>
        ByPreference(contributors).First();

    private static bool IsInLibrary(
        IReadOnlyList<OutsideLibraryMovieIdentity> library,
        OutsideLibraryEligibleContribution candidate)
    {
        var title = NormalizeTitle(candidate.CandidateTitle);
        foreach (var movie in library)
        {
            if (movie.TmdbId == candidate.TmdbId)
            {
                return true;
            }

            if (MatchesTitleYear(movie, candidate, title))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesTitleYear(
        OutsideLibraryMovieIdentity movie,
        OutsideLibraryEligibleContribution candidate,
        string title)
    {
        if (movie.TmdbId is not null
            || movie.ReleaseYear is not { } movieYear
            || candidate.CandidateReleaseYear is not { } candidateYear)
        {
            return false;
        }

        return movieYear == candidateYear
            && string.Equals(NormalizeTitle(movie.Title), title, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTitle(string? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var composed = value.Normalize(NormalizationForm.FormC).Trim();
        var builder = new StringBuilder(composed.Length);
        var pendingSpace = false;
        foreach (var character in composed)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
