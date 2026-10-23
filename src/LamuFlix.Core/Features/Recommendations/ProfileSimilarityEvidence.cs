using System.Collections.Immutable;

namespace LamuFlix.Core.Features.Recommendations;

// ReSharper disable NotAccessedPositionalProperty.Global // Public evidence contract consumed by reason emission and tests
public readonly record struct ProfileSimilarityEvidence(
    double Similarity,
    ImmutableArray<string> OverlappingGenres,
    bool HasPlotChannel,
    double PlotSimilarity);
// ReSharper restore NotAccessedPositionalProperty.Global
