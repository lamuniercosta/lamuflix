using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

// ReSharper disable NotAccessedPositionalProperty.Global
// Consumed by future movie catalog and database adapters
public sealed record MovieDetails(
    MovieId Id,
    string Title,
    LibraryPath Path,
    MediaFormat Format,
    MovieMetadata? Metadata,
    bool IsInWatchlist,
    bool IsWatched,
    Review Review,
    EnrichmentStatus EnrichmentStatus)
{
    public bool IsNotInterested => Review == Review.Disliked;
}
// ReSharper restore NotAccessedPositionalProperty.Global
