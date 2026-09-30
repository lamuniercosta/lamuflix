using System;
using FsCheck;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using Xunit;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieRehydratePropertyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly EnrichmentStatus[] Statuses =
    [
        EnrichmentStatus.Pending,
        EnrichmentStatus.Enriched,
        EnrichmentStatus.NotFound,
        EnrichmentStatus.Failed,
    ];

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Rehydrate_RestoresAnyValidFieldSet(int value)
    {
        var magnitude = Math.Abs((long)value);
        var id = new MovieId((int)(magnitude % int.MaxValue) + 1);
        var title = $"Movie {value}";
        var path = new LibraryPath($"C:/library/{value}.mkv");
        var format = new MediaFormat("mkv");
        var metadata = magnitude % 2 == 0
            ? null
            : new MovieMetadata(
                $"Metadata {value}",
                $"Synopsis {value}",
                new ReleaseYear(1888 + (int)(magnitude % 100), Now),
                new Runtime((int)(magnitude % 300) + 1),
                new ImdbRating((magnitude % 101) / 10m),
                new ImdbId($"tt{magnitude % 10000000:0000000}"));
        var isInWatchlist = value % 2 == 0;
        var status = Statuses[(int)(magnitude % Statuses.Length)];
        var enrichedAt = Now.AddSeconds(magnitude % 10000);
        var attempts = (int)(magnitude % 100);
        var failureCategory = magnitude % 2 == 0 ? null : EnrichmentFailureCategory.Unknown;
        var lastAttemptAt = Now.AddMinutes(-(magnitude % 1000));

        var movie = Movie.Rehydrate(
            id, title, path, format, isInWatchlist, metadata, status, enrichedAt,
            attempts, failureCategory, lastAttemptAt);

        return movie.Id == id
            && movie.Title == title
            && movie.Path == path
            && movie.Format == format
            && movie.IsInWatchlist == isInWatchlist
            && Equals(movie.Metadata, metadata)
            && movie.Status == status
            && movie.EnrichedAt == enrichedAt
            && movie.EnrichmentAttempts == attempts
            && movie.LastFailureCategory == failureCategory
            && movie.LastAttemptAt == lastAttemptAt;
    }
}