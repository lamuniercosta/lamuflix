using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

// ReSharper disable NotAccessedPositionalProperty.Global // Public evidence contract consumed by reason emission and tests
public readonly record struct ReviewAffinityEvidence(
    double LikedAffinity,
    double DislikedAffinity,
    bool HasReviewChannel,
    LikedSourceEvidence? LikedSource);

public readonly record struct LikedSourceEvidence(
    MovieId MovieId,
    string Title,
    double Affinity);
// ReSharper restore NotAccessedPositionalProperty.Global
