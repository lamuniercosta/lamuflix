using System;
using System.Linq;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieTmdbIdentityTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MarkEnriched_CarriesTmdbIdAndRecommendedIdsFromMetadata()
    {
        // arrange
        var movie = PendingMovie();

        // act
        movie.MarkEnriched(Metadata(603, 604, 605), Now);

        // assert
        movie.TmdbId.ShouldBe(603);
        movie.RecommendedTmdbIds.ShouldBe([604, 605]);
    }

    [Fact]
    public void MarkEnriched_MetadataWithoutTmdbIdentity_LeavesBothCarriersNull()
    {
        // arrange
        var movie = PendingMovie();

        // act
        movie.MarkEnriched(new MovieMetadata("The Matrix"), Now);

        // assert
        movie.TmdbId.ShouldBeNull();
        movie.RecommendedTmdbIds.ShouldBeNull();
    }

    [Fact]
    public void MarkEnriched_SuccessfulEmptyPage_CarriesAnEmptyListNotAnAbsentFetch()
    {
        // arrange
        var movie = PendingMovie();

        // act
        movie.MarkEnriched(Metadata(603), Now);

        // assert
        movie.RecommendedTmdbIds.ShouldNotBeNull();
        movie.RecommendedTmdbIds.ShouldBeEmpty();
    }

    [Fact]
    public void Rehydrate_CarriesTmdbIdAndRecommendedIds()
    {
        // act
        var movie = Movie.Rehydrate(
            new MovieId(1),
            "The Matrix",
            new LibraryPath("C:/library/the-matrix.mkv"),
            new MediaFormat("mkv"),
            isInWatchlist: false,
            metadata: null,
            status: EnrichmentStatus.Enriched,
            enrichedAt: Now,
            enrichmentAttempts: 1,
            lastFailureCategory: null,
            lastAttemptAt: Now,
            tmdbId: 603,
            recommendedTmdbIds: [604]);

        // assert
        movie.TmdbId.ShouldBe(603);
        movie.RecommendedTmdbIds.ShouldBe([604]);
    }

    [Fact]
    public void MarkEnriched_RejectsNullMetadata()
    {
        // arrange
        var movie = PendingMovie();

        // act
        var failure = Should.Throw<ArgumentNullException>(() => movie.MarkEnriched(null!, Now));

        // assert
        failure.ParamName.ShouldBe("metadata");
    }

    private static Movie PendingMovie() =>
        Movie.Create(
            new MovieId(1),
            "The Matrix",
            new LibraryPath("C:/library/the-matrix.mkv"),
            new MediaFormat("mkv"));

    private static MovieMetadata Metadata(int tmdbId, params int[] recommendedTmdbIds) =>
        new("The Matrix", tmdbId: tmdbId, recommendedTmdbIds: recommendedTmdbIds);
}
