using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Persistence;

namespace LamuFlix.UnitTests.Persistence;

public sealed class EfMovieRepositoryMetadataWriteTests
{
    private const string Title = "The Matrix";

    [Fact]
    public void MetadataWriteRequired_AbsentFetchAndEmptyPage_ReturnsFalse()
    {
        // act
        var absent = new MovieMetadata(Title, tmdbId: 603);
        var emptyPage = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: []);

        // assert
        EfMovieRepository.MetadataWriteRequired(absent, emptyPage).ShouldBeFalse();
        EfMovieRepository.MetadataWriteRequired(emptyPage, absent).ShouldBeFalse();
    }

    [Fact]
    public void MetadataWriteRequired_EmptyPageAndEmptyPage_ReturnsFalse()
    {
        // act
        var left = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: []);
        var right = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: []);

        // assert
        EfMovieRepository.MetadataWriteRequired(left, right).ShouldBeFalse();
    }

    [Fact]
    public void MetadataWriteRequired_AbsentFetchAndAbsentFetch_ReturnsFalse()
    {
        // act
        var left = new MovieMetadata(Title, tmdbId: 603);
        var right = new MovieMetadata(Title, tmdbId: 603);

        // assert
        EfMovieRepository.MetadataWriteRequired(left, right).ShouldBeFalse();
    }

    [Fact]
    public void MetadataWriteRequired_SameRecommendations_ReturnsFalse()
    {
        // act
        var left = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: [604, 605]);
        var right = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: [604, 605]);

        // assert
        EfMovieRepository.MetadataWriteRequired(left, right).ShouldBeFalse();
    }

    [Fact]
    public void MetadataWriteRequired_DifferentRecommendations_ReturnsTrue()
    {
        // act
        var left = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: [604]);
        var right = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: [604, 605]);

        // assert
        EfMovieRepository.MetadataWriteRequired(left, right).ShouldBeTrue();
    }

    [Fact]
    public void MetadataWriteRequired_AbsentFetchAndPopulatedPage_ReturnsTrue()
    {
        // act
        var absent = new MovieMetadata(Title, tmdbId: 603);
        var populated = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: [604]);

        // assert
        EfMovieRepository.MetadataWriteRequired(absent, populated).ShouldBeTrue();
        EfMovieRepository.MetadataWriteRequired(populated, absent).ShouldBeTrue();
    }

    [Fact]
    public void MetadataWriteRequired_EmptyPageAndPopulatedPage_ReturnsTrue()
    {
        // act
        var emptyPage = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: []);
        var populated = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: [604]);

        // assert
        EfMovieRepository.MetadataWriteRequired(emptyPage, populated).ShouldBeTrue();
        EfMovieRepository.MetadataWriteRequired(populated, emptyPage).ShouldBeTrue();
    }

    [Fact]
    public void MetadataWriteRequired_MissingMetadataAgainstPresent_ReturnsTrue()
    {
        // act
        var present = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: []);

        // assert
        EfMovieRepository.MetadataWriteRequired(null, present).ShouldBeTrue();
        EfMovieRepository.MetadataWriteRequired(present, null).ShouldBeTrue();
    }

    [Fact]
    public void MetadataWriteRequired_MissingMetadataAgainstMissing_ReturnsFalse()
    {
        // act
        var required = EfMovieRepository.MetadataWriteRequired(null, null);

        // assert
        required.ShouldBeFalse();
    }

    [Fact]
    public void MetadataWriteRequired_SameRecommendationsWithDifferentTmdbId_ReturnsTrue()
    {
        // act
        var left = new MovieMetadata(Title, tmdbId: 603, recommendedTmdbIds: []);
        var right = new MovieMetadata(Title, tmdbId: 604, recommendedTmdbIds: []);

        // assert
        EfMovieRepository.MetadataWriteRequired(left, right).ShouldBeTrue();
    }
}
