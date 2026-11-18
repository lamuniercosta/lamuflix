using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Ports;

// ReSharper disable NotAccessedPositionalProperty.Global
// Consumed by future movie catalog and database adapters
public sealed record MovieSummary(MovieId Id, string Title, string? PosterUrl, int? ReleaseYear, int? Runtime, decimal? ImdbRating, EnrichmentStatus EnrichmentStatus, bool IsInWatchlist, bool IsWatched, Review Review, EnrichmentFailureCategory? FailureCategory = null)
{
    public bool IsNotInterested => Review == Review.Disliked;

    public EnrichmentFailureCategory? FailedCategory =>
        FailureCategoryProjection.ForFailedOnly(EnrichmentStatus, FailureCategory);
}

internal static class FailureCategoryProjection
{
    public static EnrichmentFailureCategory? ForFailedOnly(EnrichmentStatus status, EnrichmentFailureCategory? category) =>
        category is not null && ReferenceEquals(status, EnrichmentStatus.Failed)
            ? category
            : null;
}
// ReSharper restore NotAccessedPositionalProperty.Global
