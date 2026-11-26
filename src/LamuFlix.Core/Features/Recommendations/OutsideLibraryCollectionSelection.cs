using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class OutsideLibraryCollectionSelection
{
    public const int MaxAdmitted = 20;

    public static IReadOnlyList<OutsideLibraryRecommendation> Admit(
        OutsideLibraryRecommendationSnapshot snapshot,
        RecommendationOptions options,
        OutsideLibraryEvidenceLookup evidence)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(evidence);

        var admitted = new List<OutsideLibraryRecommendation>(MaxAdmitted);
        var represented = new HashSet<int>();

        foreach (var candidate in OutsideLibraryRanking.Ranked(snapshot, options))
        {
            if (admitted.Count == MaxAdmitted)
            {
                break;
            }

            if (IsRepresented(evidence.Peek(candidate.TmdbId), represented))
            {
                continue;
            }

            var resolved = evidence.Resolve(candidate.TmdbId);
            if (IsSkipped(resolved, represented))
            {
                continue;
            }

            TrackAdmission(admitted, represented, candidate, resolved);
        }

        return admitted;
    }

    public static async Task<IReadOnlyList<OutsideLibraryRecommendation>> AdmitAsync(
        OutsideLibraryRecommendationSnapshot snapshot,
        RecommendationOptions options,
        OutsideLibraryEvidenceLookup evidence,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(evidence);

        var admitted = new List<OutsideLibraryRecommendation>(MaxAdmitted);
        var represented = new HashSet<int>();

        foreach (var candidate in OutsideLibraryRanking.Ranked(snapshot, options))
        {
            if (admitted.Count == MaxAdmitted)
            {
                break;
            }

            if (IsRepresented(evidence.Peek(candidate.TmdbId), represented))
            {
                continue;
            }

            var resolved = await evidence.ResolveAsync(candidate.TmdbId, ct);
            if (IsSkipped(resolved, represented))
            {
                continue;
            }

            TrackAdmission(admitted, represented, candidate, resolved);
        }

        return admitted;
    }

    private static bool IsSkipped(
        CandidateImdbEvidence? resolved,
        HashSet<int> represented) =>
        IsRepresented(resolved, represented)
        || OutsideLibraryEligibility.Evaluate(resolved) != OutsideLibraryVerdict.Admit;

    private static void TrackAdmission(
        List<OutsideLibraryRecommendation> admitted,
        HashSet<int> represented,
        OutsideLibraryRankedCandidate candidate,
        CandidateImdbEvidence? resolved)
    {
        admitted.Add(candidate.Card);
        if (resolved?.CollectionId is { } collectionId && collectionId > 0)
        {
            represented.Add(collectionId);
        }
    }

    private static bool IsRepresented(
        CandidateImdbEvidence? evidence,
        HashSet<int> represented) =>
        evidence?.CollectionId is { } collectionId
        && collectionId > 0
        && represented.Contains(collectionId);
}
