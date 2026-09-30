using System;
using LamuFlix.Core.Domain;
using Shouldly;
using Xunit;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieRehydrateTests
{
    [Fact]
    public void Rehydrate_RestoresEveryPropertyWithoutReplayingTransitions()
    {
        var id = new MovieId(37);
        var path = new LibraryPath("C:/library/rehydrated.mkv");
        var format = new MediaFormat(".MKV");
        var enrichedAt = new DateTimeOffset(2025, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var lastAttemptAt = new DateTimeOffset(2025, 3, 4, 5, 6, 7, TimeSpan.Zero);
        var metadata = new MovieMetadata(
            "Rehydrated metadata",
            "Distinct synopsis",
            new ReleaseYear(2023, enrichedAt),
            new Runtime(117),
            new ImdbRating(8.2m),
            new ImdbId("tt1234567"));

        var movie = Movie.Rehydrate(
            id,
            "Rehydrated title",
            path,
            format,
            true,
            metadata,
            EnrichmentStatus.Failed,
            enrichedAt,
            4,
            EnrichmentFailureCategory.RateLimited,
            lastAttemptAt);

        movie.Id.ShouldBe(id);
        movie.Title.ShouldBe("Rehydrated title");
        movie.Path.ShouldBe(path);
        movie.Format.ShouldBe(format);
        movie.IsInWatchlist.ShouldBeTrue();
        movie.Metadata.ShouldBe(metadata);
        movie.Status.ShouldBe(EnrichmentStatus.Failed);
        movie.EnrichedAt.ShouldBe(enrichedAt);
        movie.EnrichmentAttempts.ShouldBe(4);
        movie.LastFailureCategory.ShouldBe(EnrichmentFailureCategory.RateLimited);
        movie.LastAttemptAt.ShouldBe(lastAttemptAt);

        Should.Throw<InvalidTransitionException>(() => movie.MarkEnriched(metadata, lastAttemptAt));
        movie.RequestEnrichment();
        Should.NotThrow(() => movie.MarkEnriched(metadata, lastAttemptAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Rehydrate_RejectsBlankTitle(string title)
    {
        Should.Throw<ArgumentException>(() => Movie.Rehydrate(
            new MovieId(1), title, new LibraryPath("C:/movie.mkv"), new MediaFormat("mkv"), false,
            null, EnrichmentStatus.Pending, null, 0, null, null));
    }
}