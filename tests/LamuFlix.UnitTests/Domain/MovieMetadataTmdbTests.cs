using System;
using System.Linq;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieMetadataTmdbTests
{
    private const string SharedTitle = "Shared title";

    [Fact]
    public void Constructor_TmdbIdentityOmitted_IsNull()
    {
        // act
        var metadata = new MovieMetadata(SharedTitle);

        // assert
        metadata.TmdbId.ShouldBeNull();
        metadata.RecommendedTmdbIds.ShouldBeNull();
    }

    [Fact]
    public void Constructor_TmdbIdentityProvided_IsCarried()
    {
        // act
        var metadata = new MovieMetadata(SharedTitle, tmdbId: 603, recommendedTmdbIds: [604, 605]);

        // assert
        metadata.TmdbId.ShouldBe(603);
        metadata.RecommendedTmdbIds.ShouldBe([604, 605]);
    }

    [Fact]
    public void Constructor_RepeatedRecommendedIds_KeepsFirstOccurrenceOnly()
    {
        // act
        var metadata = new MovieMetadata(SharedTitle, recommendedTmdbIds: [604, 605, 604]);

        // assert
        metadata.RecommendedTmdbIds.ShouldBe([604, 605]);
    }

    [Fact]
    public void Constructor_EmptyRecommendedPage_IsAnEmptySuccessfulSet()
    {
        // act
        var metadata = new MovieMetadata(SharedTitle, recommendedTmdbIds: []);

        // assert
        metadata.RecommendedTmdbIds.ShouldNotBeNull();
        metadata.RecommendedTmdbIds.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveTmdbId_Throws(int tmdbId)
    {
        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(
            () => new MovieMetadata(SharedTitle, tmdbId: tmdbId));

        // assert
        failure.ParamName.ShouldBe("tmdbId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveRecommendedId_Throws(int recommendedTmdbId)
    {
        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(
            () => new MovieMetadata(SharedTitle, recommendedTmdbIds: [recommendedTmdbId]));

        // assert
        failure.ActualValue.ShouldBe(recommendedTmdbId);
    }

    [Fact]
    public void WithSignals_PreservesTmdbIdentity()
    {
        // arrange
        var metadata = new MovieMetadata(SharedTitle, tmdbId: 603, recommendedTmdbIds: [604]);

        // act
        var replaced = metadata.WithSignals(new MovieSignalSnapshot(collectionAvailability: SignalAvailability.Available));

        // assert
        replaced.TmdbId.ShouldBe(603);
        replaced.RecommendedTmdbIds.ShouldBe([604]);
    }

    [Fact]
    public void WithTrailerKey_PreservesTmdbIdentity()
    {
        // arrange
        var metadata = new MovieMetadata(SharedTitle, tmdbId: 603, recommendedTmdbIds: [604]);

        // act
        var replaced = metadata.WithTrailerKey("dQw4w9WgXcQ");

        // assert
        replaced.TmdbId.ShouldBe(603);
        replaced.RecommendedTmdbIds.ShouldBe([604]);
    }

    [Fact]
    public void WithCollection_PreservesTmdbIdentity()
    {
        // arrange
        var metadata = new MovieMetadata(SharedTitle, tmdbId: 603, recommendedTmdbIds: [604]);

        // act
        var replaced = metadata.WithCollection(42, "The Collection");

        // assert
        replaced.TmdbId.ShouldBe(603);
        replaced.RecommendedTmdbIds.ShouldBe([604]);
    }

    [Fact]
    public void Equals_DifferentTmdbId_IsNotEqual()
    {
        // act
        var left = new MovieMetadata(SharedTitle, tmdbId: 603);
        var right = new MovieMetadata(SharedTitle, tmdbId: 604);

        // assert
        left.Equals(right).ShouldBeFalse();
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentRecommendedSets_IsNotEqual()
    {
        // act
        var left = new MovieMetadata(SharedTitle, recommendedTmdbIds: [604]);
        var right = new MovieMetadata(SharedTitle, recommendedTmdbIds: [604, 605]);

        // assert
        left.Equals(right).ShouldBeFalse();
    }

    [Fact]
    public void Equals_AbsentFetchAndEmptyPage_AreNotEqual()
    {
        // act
        var absent = new MovieMetadata(SharedTitle, tmdbId: 603);
        var emptyPage = new MovieMetadata(SharedTitle, tmdbId: 603, recommendedTmdbIds: []);

        // assert
        absent.Equals(emptyPage).ShouldBeFalse();
        absent.ShouldNotBe(emptyPage);
    }

    [Fact]
    public void Equals_SameTmdbIdentity_IsEqual()
    {
        // act
        var left = new MovieMetadata(SharedTitle, tmdbId: 603, recommendedTmdbIds: [604, 605]);
        var right = new MovieMetadata(SharedTitle, tmdbId: 603, recommendedTmdbIds: [604, 605]);

        // assert
        left.Equals(right).ShouldBeTrue();
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }
}
