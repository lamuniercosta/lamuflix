using System.Collections.Immutable;

namespace LamuFlix.Core.Features.Recommendations;

public sealed record ForYouRecommendationsPage(ImmutableList<RecommendedMovie> Items, bool HasMore);