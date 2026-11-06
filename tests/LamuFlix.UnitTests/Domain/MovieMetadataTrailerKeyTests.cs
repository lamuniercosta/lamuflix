using System;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieMetadataTrailerKeyTests
{
    private const string SharedTitle = "Shared title";

    [Fact]
    public void Constructor_TrailerKeyOmitted_IsNull()
    {
        // act
        var metadata = new MovieMetadata(SharedTitle);

        // assert
        metadata.TrailerKey.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ValidTrailerKey_IsCarried()
    {
        // act
        var metadata = new MovieMetadata(SharedTitle, trailerKey: "dQw4w9WgXcQ");

        // assert
        metadata.TrailerKey.ShouldBe("dQw4w9WgXcQ");
    }

    [Fact]
    public void WithSignals_CarriesTrailerKey()
    {
        // arrange
        var metadata = MovieMetadataFixture.Create(SharedTitle, trailerKey: "dQw4w9WgXcQ");

        // act
        var replaced = metadata.WithSignals(new MovieSignalSnapshot(collectionAvailability: SignalAvailability.Available));

        // assert
        replaced.TrailerKey.ShouldBe("dQw4w9WgXcQ");
    }

    [Fact]
    public void WithCollection_CarriesTrailerKey()
    {
        // arrange
        var metadata = MovieMetadataFixture.Create(SharedTitle, trailerKey: "dQw4w9WgXcQ");

        // act
        var replaced = metadata.WithCollection(603, "The Matrix Collection");

        // assert
        replaced.TrailerKey.ShouldBe("dQw4w9WgXcQ");
        replaced.CollectionId.ShouldBe(603);
    }

    [Fact]
    public void EqualsAndHash_DifferingTrailerKey_AreNotEqualAndHashDiffers()
    {
        // arrange
        var left = MovieMetadataFixture.Create(SharedTitle, trailerKey: "dQw4w9WgXcQ");
        var right = MovieMetadataFixture.Create(SharedTitle, trailerKey: "oHg5SJYRHA0");

        // act & assert
        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_TrailerKeyAdded_IsNotEqualAndHashDiffers()
    {
        // arrange
        var withoutKey = MovieMetadataFixture.Create(SharedTitle, trailerKey: null);
        var withKey = MovieMetadataFixture.Create(SharedTitle, trailerKey: "dQw4w9WgXcQ");

        // act & assert
        withKey.ShouldNotBe(withoutKey);
        withoutKey.ShouldNotBe(withKey);
        withKey.GetHashCode().ShouldNotBe(withoutKey.GetHashCode());
    }

    [Fact]
    public void ClearMetadata_MetadataCarryingTrailerKey_RemovesTheKeyWithTheMetadata()
    {
        // arrange
        var movie = Movie.Rehydrate(
            new MovieId(21),
            SharedTitle,
            new LibraryPath("C:/library/trailer-clear.mkv"),
            new MediaFormat("mkv"),
            false,
            MovieMetadataFixture.Create(SharedTitle, "poster.jpg", "dQw4w9WgXcQ"),
            EnrichmentStatus.Enriched,
            new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero),
            1,
            null,
            new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero));

        // act
        movie.ClearMetadata();

        // assert
        movie.Metadata.ShouldBeNull();
    }

    [Fact]
    public void WithSignals_WithoutTrailerKey_DoesNotFabricateAKey()
    {
        // arrange
        var metadata = MovieMetadataFixture.Create(SharedTitle, "poster.jpg");

        // act
        var replaced = metadata.WithSignals(new MovieSignalSnapshot());

        // assert
        replaced.TrailerKey.ShouldBeNull();
    }

    [Theory]
    [InlineData("dQw4w9WgXcQ")]
    [InlineData("abcdefghijk")]
    [InlineData("ABCDEFGHIJK")]
    [InlineData("0123456789-")]
    [InlineData("___________")]
    [InlineData("-----------")]
    public void IsValidTrailerKey_ElevenAsciiKeyCharacters_IsTrue(string key)
    {
        // act & assert
        MovieMetadata.IsValidTrailerKey(key).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("dQw4w9WgXc")]
    [InlineData("dQw4w9WgXcQQ")]
    [InlineData("dQw4w9WgXc.")]
    [InlineData("dQw4w9WgXc/")]
    [InlineData("dQw4w9WgXc ")]
    [InlineData(" dQw4w9WgXc")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("dQw4w9WgXcQ&t=1")]
    [InlineData("dQw4w9WgXç")]
    public void IsValidTrailerKey_NotAnElevenAsciiKey_IsFalse(string? key)
    {
        // act & assert
        MovieMetadata.IsValidTrailerKey(key).ShouldBeFalse();
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool IsValidTrailerKey_ForAnyString_ReflectsExactShape(string? candidate)
    {
        var value = candidate ?? "";

        return MovieMetadata.IsValidTrailerKey(candidate) == IsElevenKeyCharacters(value);
    }

    private static bool IsElevenKeyCharacters(string value) =>
        value.Length == 11
        && value.All(character =>
            char.IsAsciiLetterOrDigit(character) || character == '_' || character == '-');
}
