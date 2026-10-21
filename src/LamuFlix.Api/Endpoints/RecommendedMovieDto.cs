using System.Collections.Immutable;
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
    double Score,
    ImmutableList<string> Reasons);
// ReSharper restore NotAccessedPositionalProperty.Global
