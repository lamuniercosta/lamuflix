using System;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieMetadataPosterTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Equals_PosterOnlyDifference_IsNotEqual()
    {
        var withoutPoster = Metadata(null);
        var withPoster = Metadata("https://example.test/poster.jpg");

        withPoster.Equals(withoutPoster).ShouldBeFalse();
        withoutPoster.Equals(withPoster).ShouldBeFalse();
        withPoster.GetHashCode().ShouldNotBe(withoutPoster.GetHashCode());
    }

    [Fact]
    public void Equals_SameFieldsAndPoster_IsEqual()
    {
        Metadata("https://example.test/poster.jpg").ShouldBe(Metadata("https://example.test/poster.jpg"));
    }

    [Fact]
    public void Equals_DifferingWhitespaceOnly_IsNotEqual()
    {
        Metadata("poster.jpg").ShouldNotBe(Metadata(" poster.jpg "));
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Equals_ForAnyPosterPair_ReflectsPosterEquality(int left, int right)
    {
        var leftPoster = Poster(left);
        var rightPoster = Poster(right);

        return Metadata(leftPoster).Equals(Metadata(rightPoster))
            == string.Equals(leftPoster, rightPoster, StringComparison.Ordinal);
    }

    private static string? Poster(int value) => value % 3 == 0 ? null : $"poster-{value}.jpg";

    private static MovieMetadata Metadata(string? posterUrl) =>
        new(
            "Shared title",
            "Shared synopsis",
            new ReleaseYear(2000, Now),
            new Runtime(100),
            new ImdbRating(7.5m),
            new ImdbId("tt0000001"),
            posterUrl);
}
