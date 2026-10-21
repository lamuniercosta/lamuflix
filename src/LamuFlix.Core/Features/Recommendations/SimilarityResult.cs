using System.Collections.Immutable;

namespace LamuFlix.Core.Features.Recommendations;

// ReSharper disable NotAccessedPositionalProperty.Global // Public scorer contract consumed by callers and tests
public sealed record SimilarityResult(double Score, ImmutableList<Reason> Reasons);
// ReSharper restore NotAccessedPositionalProperty.Global
