using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

// ReSharper disable once NotAccessedPositionalProperty.Global // Public evidence contract consumed by reason emission and tests
public readonly record struct ReviewAffinityEvidence(
    double LikedAffinity,
    double DislikedAffinity,
    bool HasReviewChannel,
    LikedSourceEvidence? LikedSource);

// ReSharper disable once NotAccessedPositionalProperty.Global // Public evidence contract consumed by reason emission and tests
public readonly record struct LikedSourceEvidence(
    MovieId MovieId,
    string Title,
    double Affinity);
