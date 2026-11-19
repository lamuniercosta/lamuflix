using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

// ReSharper disable NotAccessedPositionalProperty.Global // Public selector contract consumed by callers and tests
public sealed record MmrCandidate(
    MovieId MovieId,
    double Relevance,
    MovieFeatures Features,
    MovieId? BestSeedId = null);
// ReSharper restore NotAccessedPositionalProperty.Global
