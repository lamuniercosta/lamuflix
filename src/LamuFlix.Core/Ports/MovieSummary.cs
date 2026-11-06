using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

// ReSharper disable NotAccessedPositionalProperty.Global
// Consumed by future movie catalog and database adapters
public sealed record MovieSummary(MovieId Id, string Title, string? PosterUrl, int? ReleaseYear, int? Runtime, decimal? ImdbRating, EnrichmentStatus EnrichmentStatus, bool IsInWatchlist, bool IsWatched, Review Review)
{
    public bool IsNotInterested => Review == Review.Disliked;
}
// ReSharper restore NotAccessedPositionalProperty.Global
