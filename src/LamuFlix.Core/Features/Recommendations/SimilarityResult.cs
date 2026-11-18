using System.Collections.Immutable;

namespace LamuFlix.Core.Features.Recommendations;

// ReSharper disable NotAccessedPositionalProperty.Global // Public scorer contract consumed by callers and tests
public sealed record SimilarityResult(double Score, ImmutableList<Reason> Reasons)
{
    public ImmutableList<CriterionContribution> Breakdown { get; init; } = ImmutableList<CriterionContribution>.Empty;
}
// ReSharper restore NotAccessedPositionalProperty.Global

public sealed record CriterionContribution(string Name, double Value, double Weight, double Contribution);
