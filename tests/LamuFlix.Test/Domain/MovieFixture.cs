using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.Test.Domain;

internal static class MovieFixture
{
    public static readonly DateTimeOffset SeedTimestamp = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly MovieMetadata SeedMetadata = new("Seed");

    public static Movie CreateInStatus(
        EnrichmentStatus status,
        int id = 1,
        string title = "Seven",
        DateTimeOffset? arrivedAt = null)
    {
        var timestamp = arrivedAt ?? SeedTimestamp;
        var movie = Movie.Create(new MovieId(id), title, new LibraryPath(@"C:\lib\a.mkv"), new MediaFormat("mkv"));
        switch (status.Name)
        {
            case "Pending":
                return movie;
            case "Enriched":
                movie.MarkEnriched(SeedMetadata, timestamp);
                return movie;
            case "NotFound":
                movie.MarkNotFound(timestamp);
                return movie;
            case "Failed":
                movie.MarkFailed(EnrichmentFailureCategory.RateLimited, timestamp);
                return movie;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status.Name, "Unknown enrichment status.");
        }
    }
}
