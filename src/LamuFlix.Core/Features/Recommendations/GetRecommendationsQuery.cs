using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

// ReSharper disable NotAccessedPositionalProperty.Global // Public query contract consumed by callers and tests
public sealed record GetRecommendationsQuery(MovieId MovieId, int Take = 10);
// ReSharper restore NotAccessedPositionalProperty.Global
