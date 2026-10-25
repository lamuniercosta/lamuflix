namespace LamuFlix.Core.Features.Recommendations;

// ReSharper disable NotAccessedPositionalProperty.Global // Public query contract consumed by callers and tests
public sealed record GetForYouRecommendationsQuery(int Take = 10);
// ReSharper restore NotAccessedPositionalProperty.Global
