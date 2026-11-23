using System;
using System.Collections.Generic;

namespace LamuFlix.Core.Features.Recommendations;

/// <summary>
/// One library feature load: the scored library entries plus the immutable Crowd link lookup
/// resolved for that same load, so scoring stays pure and performs no further reads.
/// </summary>
public sealed record RecommendationLibrarySnapshot
{
    public RecommendationLibrarySnapshot(
        IReadOnlyList<RecommendationLibraryEntry> entries,
        CrowdLinks crowdLinks)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(crowdLinks);

        Entries = entries;
        CrowdLinks = crowdLinks;
    }

    public IReadOnlyList<RecommendationLibraryEntry> Entries { get; init; }

    public CrowdLinks CrowdLinks { get; init; }

    public int Count => Entries.Count;
}
