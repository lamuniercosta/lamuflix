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

    // Caller-safe failure history: only a Failed movie exposes the persisted category (DEV-522 Q3).
    public EnrichmentFailureCategory? FailedCategory =>
        FailureCategory is not null && ReferenceEquals(EnrichmentStatus, EnrichmentStatus.Failed)
            ? FailureCategory
            : null;
}
// ReSharper restore NotAccessedPositionalProperty.Global
