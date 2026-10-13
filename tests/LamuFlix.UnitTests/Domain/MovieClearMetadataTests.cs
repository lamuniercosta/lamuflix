using System;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieClearMetadataTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly EnrichmentStatus[] Statuses =
    [
        EnrichmentStatus.Pending,
        EnrichmentStatus.Enriched,
        EnrichmentStatus.NotFound,
        EnrichmentStatus.Failed,
    ];

    [Fact]
    public void ClearMetadata_NullsMetadataAndPreservesEveryUnrelatedField()
    {
        var id = new MovieId(11);
        var path = new LibraryPath("C:/library/clear.mkv");
        var format = new MediaFormat("mkv");
        var enrichedAt = Now.AddHours(-2);
        var lastAttemptAt = Now.AddHours(-1);
        var movie = Movie.Rehydrate(
            id,
            "Clear me",
            path,
            format,
            true,
            Metadata("Clear me"),
            EnrichmentStatus.Enriched,
            enrichedAt,
            3,
            EnrichmentFailureCategory.Unknown,
            lastAttemptAt);

        movie.ClearMetadata();

        movie.Metadata.ShouldBeNull();
        State(movie).ShouldBe(new StateSnapshot(id, "Clear me", path, format, true, EnrichmentStatus.Enriched, enrichedAt, 3, EnrichmentFailureCategory.Unknown, lastAttemptAt));
    }

    [Fact]
    public void ClearMetadata_Repeated_IsIdempotent()
    {
        var movie = Movie.Rehydrate(
            new MovieId(12),
            "Idempotent",
            new LibraryPath("C:/library/idempotent.mkv"),
            new MediaFormat("mkv"),
            false,
            Metadata("Idempotent"),
            EnrichmentStatus.Enriched,
            Now,
            1,
            null,
            Now);
        var before = State(movie);

        movie.ClearMetadata();
        movie.ClearMetadata();

        movie.Metadata.ShouldBeNull();
        State(movie).ShouldBe(before);
    }

    [Fact]
    public void ClearMetadata_WithoutMetadata_DoesNotThrowAndKeepsState()
    {
        var movie = Movie.Create(new MovieId(13), "Fresh", new LibraryPath("C:/library/fresh.mkv"), new MediaFormat("mkv"));

        Should.NotThrow(movie.ClearMetadata);
        Should.NotThrow(movie.ClearMetadata);

        movie.Metadata.ShouldBeNull();
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        movie.EnrichmentAttempts.ShouldBe(0);
        movie.LastAttemptAt.ShouldBeNull();
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool ClearMetadata_AcrossAnyState_OnlyNullsMetadataAndStaysIdempotent(int value)
    {
        var magnitude = Math.Abs((long)value);
        var id = new MovieId((int)(magnitude % int.MaxValue) + 1);
        var path = new LibraryPath($"C:/library/{value}.mkv");
        var format = new MediaFormat("mkv");
        var enrichedAt = Now.AddSeconds(magnitude % 10000);
        var attempts = (int)(magnitude % 100);
        var failureCategory = magnitude % 2 == 0 ? null : EnrichmentFailureCategory.Unknown;
        var lastAttemptAt = Now.AddMinutes(-(magnitude % 1000));
        var status = Statuses[(int)(magnitude % Statuses.Length)];
        var movie = Movie.Rehydrate(
            id,
            $"Movie {value}",
            path,
            format,
            value % 2 == 0,
            Metadata($"Movie {value}"),
            status,
            enrichedAt,
            attempts,
            failureCategory,
            lastAttemptAt);
        var before = State(movie);

        movie.ClearMetadata();
        var afterFirst = movie.Metadata is null && State(movie) == before;
        movie.ClearMetadata();

        return afterFirst && movie.Metadata is null && State(movie) == before;
    }

    private static MovieMetadata Metadata(string title) =>
        new(
            title,
            "Synopsis",
            new ReleaseYear(2000, Now),
            new Runtime(100),
            new ImdbRating(7.5m),
            new ImdbId("tt0000001"),
            "https://example.test/poster.jpg");

    private static StateSnapshot State(Movie movie) =>
        new(
            movie.Id,
            movie.Title,
            movie.Path,
            movie.Format,
            movie.IsInWatchlist,
            movie.Status,
            movie.EnrichedAt,
            movie.EnrichmentAttempts,
            movie.LastFailureCategory,
            movie.LastAttemptAt);

    private sealed record StateSnapshot(
        MovieId Id,
        string Title,
        LibraryPath Path,
        MediaFormat Format,
        bool IsInWatchlist,
        EnrichmentStatus Status,
        DateTimeOffset? EnrichedAt,
        int EnrichmentAttempts,
        EnrichmentFailureCategory? LastFailureCategory,
        DateTimeOffset? LastAttemptAt);
}
