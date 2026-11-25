using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class RecommendedTmdbMovieTests
{
    [Fact]
    public void MovieMetadata_RecommendedMovies_AreCarriedWithTheirMetadata()
    {
        // act
        var metadata = new MovieMetadata(
            "Seed",
            tmdbId: 603,
            recommendedTmdbMovies:
            [
                new RecommendedTmdbMovie(604, "Candidate", 2003, "/poster.jpg"),
                new RecommendedTmdbMovie(605, "No year", null, null),
            ]);

        // assert
        metadata.RecommendedTmdbIds.ShouldBe([604, 605]);
        metadata.RecommendedTmdbMovies.ShouldBe(
        [
            new RecommendedTmdbMovie(604, "Candidate", 2003, "/poster.jpg"),
            new RecommendedTmdbMovie(605, "No year", null, null),
        ]);
    }

    [Fact]
    public void MovieMetadata_RecommendedMovies_DeduplicatesByIdKeepingTheFirstTuple()
    {
        // act
        var metadata = new MovieMetadata(
            "Seed",
            recommendedTmdbMovies:
            [
                new RecommendedTmdbMovie(604, "First", 2003, "/first.jpg"),
                new RecommendedTmdbMovie(604, "Second", 2004, "/second.jpg"),
                new RecommendedTmdbMovie(605, " Other ", null, null),
            ]);

        // assert
        metadata.RecommendedTmdbMovies.ShouldBe(
        [
            new RecommendedTmdbMovie(604, "First", 2003, "/first.jpg"),
            new RecommendedTmdbMovie(605, " Other ", null, null),
        ]);
    }

    [Fact]
    public void MovieMetadata_ImmutableUpdates_PreserveRecommendedMovies()
    {
        // arrange
        var metadata = new MovieMetadata(
            "Seed",
            tmdbId: 603,
            recommendedTmdbMovies: [new RecommendedTmdbMovie(604, "Candidate", 2003, "/poster.jpg")]);

        // assert
        metadata.WithSignals(new MovieSignalSnapshot()).RecommendedTmdbMovies.ShouldBe(metadata.RecommendedTmdbMovies);
        metadata.WithTrailerKey("dQw4w9WgXcQ").RecommendedTmdbMovies.ShouldBe(metadata.RecommendedTmdbMovies);
        metadata.WithCollection(42, "Collection").RecommendedTmdbMovies.ShouldBe(metadata.RecommendedTmdbMovies);
    }

    [Fact]
    public void MovieMetadata_EqualTuples_AreEqualWithMatchingHashCodes()
    {
        // arrange
        var candidate = new RecommendedTmdbMovie(604, "Candidate", 2003, "/poster.jpg");
        var left = new MovieMetadata("Seed", recommendedTmdbMovies: [candidate]);
        var right = new MovieMetadata("Seed", recommendedTmdbMovies: [candidate]);

        // assert
        left.ShouldBe(right);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void Movie_RehydratedWithUntitledRows_IsEligibleForHydration()
    {
        // arrange
        var id = new MovieId(1);

        // act
        var movie = Movie.Rehydrate(
            id,
            "Seed",
            new LibraryPath("C:/library/seed.mkv"),
            new MediaFormat("mkv"),
            false,
            null,
            EnrichmentStatus.Enriched,
            null,
            0,
            null,
            null,
            tmdbId: 603,
            recommendedTmdbIds: [604]);

        // assert
        movie.RecommendedTmdbIds.ShouldBe([604]);
        movie.RecommendedTmdbMovies.ShouldBe([RecommendedTmdbMovie.FromId(604)]);
        movie.HasUntitledRecommendations.ShouldBeTrue();
    }

    [Fact]
    public void Movie_RecordBackfilledIdentity_ReplacesTheWholeTitledSet()
    {
        // arrange
        var movie = Movie.Create(
            new MovieId(1),
            "Seed",
            new LibraryPath("C:/library/seed.mkv"),
            new MediaFormat("mkv"));

        // act
        movie.RecordBackfilledTmdbIdentity(
            603,
            [
                new RecommendedTmdbMovie(604, "First", 2003, "/first.jpg"),
                new RecommendedTmdbMovie(605, null, null, null),
            ]);

        // assert
        movie.TmdbId.ShouldBe(603);
        movie.RecommendedTmdbIds.ShouldBe([604, 605]);
        movie.HasUntitledRecommendations.ShouldBeTrue();
    }
}
