using System;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Recommendations;

// ReSharper disable NotAccessedPositionalProperty.Global // Public result contract consumed by callers and tests
public sealed record RecommendationSeed(int MovieId, string Title, SeedSignal Signal);

public sealed record RecommendationEvidence(string Criterion, string Text, ImmutableList<string> MatchedNames)
{
    public bool Equals(RecommendationEvidence? other) =>
        other is not null
        && string.Equals(Criterion, other.Criterion, StringComparison.Ordinal)
        && string.Equals(Text, other.Text, StringComparison.Ordinal)
        && MatchedNames.SequenceEqual(other.MatchedNames, StringComparer.Ordinal);

    public override int GetHashCode() => HashCode.Combine(Criterion, Text, MatchedNames.Count);
}

public sealed record RecommendedMovie(
    MovieSummary Summary,
    double Score,
    ImmutableList<RecommendationEvidence> Evidence,
    RecommendationSeed? Seed = null,
    int? StartedCollectionId = null);
// ReSharper restore NotAccessedPositionalProperty.Global
