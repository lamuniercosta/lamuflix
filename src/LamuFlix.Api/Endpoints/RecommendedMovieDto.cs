using System.Collections.Immutable;
using System.Text.Json.Serialization;
using LamuFlix.Core.Domain;

namespace LamuFlix.Api.Endpoints;

// ReSharper disable NotAccessedPositionalProperty.Global // Serialized by System.Text.Json as the frozen wire shape, never read in code
internal sealed record RecommendedMovieDto(
    int Id,
    string Title,
    string? PosterUrl,
    int? ReleaseYear,
    int? Runtime,
    decimal? ImdbRating,
    EnrichmentStatus EnrichmentStatus,
    bool IsInWatchlist,
    bool IsWatched,
    bool IsNotInterested,
    string Review,
    double Score,
    ImmutableList<RecommendationEvidenceDto> Evidence,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RecommendationSeedDto? Seed = null);

internal sealed record RecommendationSeedDto(
    int MovieId,
    string Title,
    RecommendationSeedSignal Signal);

internal sealed record RecommendationEvidenceDto(
    string Criterion,
    string Text,
    ImmutableList<string> MatchedNames);

internal enum RecommendationSeedSignal
{
    [JsonStringEnumMemberName("liked")] Liked,
    [JsonStringEnumMemberName("watched")] Watched,
    [JsonStringEnumMemberName("watchlist")] Watchlist,
}
// ReSharper restore NotAccessedPositionalProperty.Global
