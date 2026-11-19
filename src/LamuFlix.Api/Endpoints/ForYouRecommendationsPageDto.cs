using System.Collections.Generic;

namespace LamuFlix.Api.Endpoints;

internal sealed record ForYouRecommendationsPageDto(IReadOnlyList<RecommendedMovieDto> Items, bool HasMore);