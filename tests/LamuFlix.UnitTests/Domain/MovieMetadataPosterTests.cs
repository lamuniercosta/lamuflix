using System;
using FsCheck.Xunit;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieMetadataPosterTests
{
    private const string SharedTitle = "Shared title";

    [Fact]
    public void Equals_PosterOnlyDifference_IsNotEqual()
    {
        var withoutPoster = MovieMetadataFixture.Create(SharedTitle, posterUrl: null);
        var withPoster = MovieMetadataFixture.Create(SharedTitle, "https://example.test/poster.jpg");

        withPoster.Equals(withoutPoster).ShouldBeFalse();
        withoutPoster.Equals(withPoster).ShouldBeFalse();
        withPoster.GetHashCode().ShouldNotBe(withoutPoster.GetHashCode());
    }

    [Fact]
    public void Equals_SameFieldsAndPoster_IsEqual()
    {
        MovieMetadataFixture.Create(SharedTitle, "https://example.test/poster.jpg")
            .ShouldBe(MovieMetadataFixture.Create(SharedTitle, "https://example.test/poster.jpg"));
    }

    [Fact]
    public void Equals_DifferingWhitespaceOnly_IsNotEqual()
    {
        MovieMetadataFixture.Create(SharedTitle, "poster.jpg")
            .ShouldNotBe(MovieMetadataFixture.Create(SharedTitle, " poster.jpg "));
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Equals_ForAnyPosterPair_ReflectsPosterEquality(int left, int right)
    {
        var leftPoster = Poster(left);
        var rightPoster = Poster(right);

        return MovieMetadataFixture.Create(SharedTitle, leftPoster)
                .Equals(MovieMetadataFixture.Create(SharedTitle, rightPoster))
            == string.Equals(leftPoster, rightPoster, StringComparison.Ordinal);
    }

    private static string? Poster(int value) => value % 3 == 0 ? null : $"poster-{value}.jpg";
}
