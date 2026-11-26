using System;
using System.Collections.Generic;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class OutsideLibraryEvidenceLookup(
    IReadOnlyDictionary<int, CandidateImdbEvidence> cachedEvidence,
    DateTimeOffset now,
    Func<int, CandidateImdbEvidence?>? resolveEvidence = null,
    IReadOnlyDictionary<int, CandidateImdbEvidence>? fresherEvidence = null)
{
    private readonly IReadOnlyDictionary<int, CandidateImdbEvidence> cached =
        cachedEvidence ?? throw new ArgumentNullException(nameof(cachedEvidence));

    public CandidateImdbEvidence? Peek(int tmdbId)
    {
        var evidence = PreferFresher(tmdbId, cached.GetValueOrDefault(tmdbId));
        return IsFresh(evidence) ? evidence : null;
    }

    public CandidateImdbEvidence? Resolve(int tmdbId) =>
        Peek(tmdbId) ?? resolveEvidence?.Invoke(tmdbId);

    private CandidateImdbEvidence? PreferFresher(
        int tmdbId,
        CandidateImdbEvidence? baseline)
    {
        if (fresherEvidence is null
            || !fresherEvidence.TryGetValue(tmdbId, out var fresher))
        {
            return baseline;
        }

        return fresher.ExpiresAtUtc > baseline?.ExpiresAtUtc ? fresher : baseline;
    }

    private bool IsFresh(CandidateImdbEvidence? evidence) =>
        evidence is not null
        && (evidence.ExpiresAtUtc is null || evidence.ExpiresAtUtc > now);
}
