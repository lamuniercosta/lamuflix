using System.Collections.Generic;

namespace LamuFlix.Core.Features.Recommendations;

/// <summary>
/// One library feature load: the scored library entries plus the immutable Crowd link lookup
/// resolved for that same load, so scoring stays pure and performs no further reads.
/// </summary>
// ReSharper disable NotAccessedPositionalProperty.Global // Public port contract consumed by callers and tests
public sealed record RecommendationLibrarySnapshot(
    IReadOnlyList<RecommendationLibraryEntry> Entries,
    CrowdLinks CrowdLinks)
{
    public int Count => Entries.Count;
}
// ReSharper restore NotAccessedPositionalProperty.Global
