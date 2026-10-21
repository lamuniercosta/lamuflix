using System.Collections.Immutable;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Recommendations;

// ReSharper disable NotAccessedPositionalProperty.Global // Public result contract consumed by callers and tests
public sealed record RecommendedMovie(MovieSummary Summary, double Score, ImmutableList<Reason> Reasons);
// ReSharper restore NotAccessedPositionalProperty.Global
