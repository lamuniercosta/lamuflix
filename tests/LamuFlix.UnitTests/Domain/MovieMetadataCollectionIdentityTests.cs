using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieMetadataCollectionIdentityTests
{
    [Fact]
    public void Constructor_WithCollectionIdAndName_ExposesBoth()
    {
        // act
        var metadata = new MovieMetadata("The Matrix", collectionId: 2344, collectionName: "The Matrix Collection");

        // assert
        metadata.CollectionId.ShouldBe(2344);
        metadata.CollectionName.ShouldBe("The Matrix Collection");
    }

    [Fact]
    public void Constructor_CollectionOmitted_LeavesCollectionAbsent()
    {
        // act
        var metadata = new MovieMetadata("The Matrix");

        // assert
        metadata.CollectionId.ShouldBeNull();
        metadata.CollectionName.ShouldBeNull();
    }

    [Fact]
    public void Constructor_CollectionIdWithoutName_ThrowsArgumentException()
    {
        // act
        var thrown = Should.Throw<ArgumentException>(
            () => new MovieMetadata("The Matrix", collectionId: 2344));

        // assert
        thrown.Message.ShouldContain("required together");
    }

    [Fact]
    public void Constructor_CollectionNameWithoutId_ThrowsArgumentException()
    {
        // act
        var thrown = Should.Throw<ArgumentException>(
            () => new MovieMetadata("The Matrix", collectionName: "The Matrix Collection"));

        // assert
        thrown.Message.ShouldContain("required together");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Constructor_NonPositiveCollectionId_ThrowsArgumentOutOfRangeException(int collectionId)
    {
        // act
        var thrown = Should.Throw<ArgumentOutOfRangeException>(
            () => new MovieMetadata("The Matrix", collectionId: collectionId, collectionName: "The Matrix Collection"));

        // assert
        thrown.ParamName.ShouldBe("collectionId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_BlankCollectionName_ThrowsArgumentException(string collectionName)
    {
        // act
        var thrown = Should.Throw<ArgumentException>(
            () => new MovieMetadata("The Matrix", collectionId: 2344, collectionName: collectionName));

        // assert
        thrown.ParamName.ShouldBe("collectionName");
    }

    [Fact]
    public void EqualsAndHash_SameCollectionIdentity_AreEqualWithEqualHash()
    {
        // arrange
        var left = new MovieMetadata("The Matrix", collectionId: 2344, collectionName: "The Matrix Collection");
        var right = new MovieMetadata("The Matrix", collectionId: 2344, collectionName: "The Matrix Collection");

        // act & assert
        left.ShouldBe(right);
        right.ShouldBe(left);
        left.GetHashCode().ShouldBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingCollectionId_AreNotEqualAndHashDiffers()
    {
        // arrange
        var left = new MovieMetadata("The Matrix", collectionId: 2344, collectionName: "The Matrix Collection");
        var right = new MovieMetadata("The Matrix", collectionId: 2345, collectionName: "The Matrix Collection");

        // act & assert
        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingCollectionName_AreNotEqualAndHashDiffers()
    {
        // arrange
        var left = new MovieMetadata("The Matrix", collectionId: 2344, collectionName: "The Matrix Collection");
        var right = new MovieMetadata("The Matrix", collectionId: 2344, collectionName: "Other Collection");

        // act & assert
        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_CollectionPresentVersusAbsent_AreNotEqualAndHashDiffers()
    {
        // arrange
        var absent = new MovieMetadata("The Matrix");
        var present = new MovieMetadata("The Matrix", collectionId: 2344, collectionName: "The Matrix Collection");

        // act & assert
        absent.ShouldNotBe(present);
        present.ShouldNotBe(absent);
        absent.GetHashCode().ShouldNotBe(present.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_SameGenresAndWritersButDifferingCollection_AreNotEqual()
    {
        // arrange
        var left = new MovieMetadata(
            "The Matrix",
            genres: new[] { "Action" },
            writers: new[] { "Writer A" },
            collectionId: 2344,
            collectionName: "The Matrix Collection");
        var right = new MovieMetadata(
            "The Matrix",
            genres: new[] { "Action" },
            writers: new[] { "Writer A" },
            collectionId: 604,
            collectionName: "The Matrix Collection");

        // act & assert
        left.ShouldNotBe(right);
    }
}
