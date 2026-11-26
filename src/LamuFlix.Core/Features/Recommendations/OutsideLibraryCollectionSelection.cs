using System;
using System.Collections.Generic;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class OutsideLibraryCollectionSelection
{
    private const int MaxAdmitted = 20;

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
            if (IsRepresented(resolved, represented))
            {
                continue;
            }

            if (OutsideLibraryEligibility.Evaluate(resolved) != OutsideLibraryVerdict.Admit)
            {
                continue;
            }

            admitted.Add(candidate.Card);
            if (resolved?.CollectionId is { } collectionId && collectionId > 0)
            {
                represented.Add(collectionId);
            }
        }

        return admitted;
    }

    private static bool IsRepresented(
        CandidateImdbEvidence? evidence,
        HashSet<int> represented) =>
        evidence?.CollectionId is { } collectionId
        && collectionId > 0
        && represented.Contains(collectionId);
}
