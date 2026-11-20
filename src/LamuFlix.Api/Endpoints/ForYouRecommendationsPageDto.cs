using System.Collections.Generic;

namespace LamuFlix.Api.Endpoints;

// ReSharper disable NotAccessedPositionalProperty.Global // Serialized by System.Text.Json as the frozen wire shape, never read in code
internal sealed record ForYouRecommendationsPageDto(IReadOnlyList<RecommendedMovieDto> Items, bool HasMore);
// ReSharper restore NotAccessedPositionalProperty.Global