using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class MovieSummaryMapperSurvivorTests
{
    [Fact]
    public void From_NullMovie_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => MovieSummaryMapper.From(null!));

        // assert
        failure.ParamName.ShouldBe("movie");
    }

    [Fact]
    public void From_EnrichedMovie_MapsReview()
    {
        // arrange
        var movie = Movie.Rehydrate(
            new MovieId(1),
            "Arrival",
            new LibraryPath("C:/library/1.mkv"),
            new MediaFormat("mkv"),
            false,
            null,
            EnrichmentStatus.Enriched,
            new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero),
            1,
            null,
            null,
            isWatched: false,
            review: Review.Liked);

        // act
        var summary = MovieSummaryMapper.From(movie);

        // assert
        summary.Review.ShouldBe(Review.Liked);
    }
}
