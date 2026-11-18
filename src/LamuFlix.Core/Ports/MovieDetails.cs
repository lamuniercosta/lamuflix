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
    EnrichmentStatus EnrichmentStatus,
    EnrichmentFailureCategory? FailureCategory = null)
{
    public bool IsNotInterested => Review == Review.Disliked;

    public EnrichmentFailureCategory? FailedCategory =>
        FailureCategoryProjection.ForFailedOnly(EnrichmentStatus, FailureCategory);
}
// ReSharper restore NotAccessedPositionalProperty.Global
