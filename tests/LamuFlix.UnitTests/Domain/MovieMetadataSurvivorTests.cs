using System;
using System.Collections.Generic;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieMetadataSurvivorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_BlankTitle_ThrowsArgumentExceptionWithParamNameAndMessage(string title)
    {
        // act
        var thrown = Should.Throw<ArgumentException>(() => new MovieMetadata(title));

        // assert
        thrown.ParamName.ShouldBe("title");
        thrown.Message.ShouldContain("Title is required");
    }

    [Fact]
    public void Constructor_TitleOnly_KeepsTitleAndLeavesOptionalMembersNull()
    {
        // act
        var metadata = new MovieMetadata("Arrival");

        // assert
        metadata.Title.ShouldBe("Arrival");
        metadata.Synopsis.ShouldBeNull();
        metadata.ReleaseYear.ShouldBeNull();
        metadata.Runtime.ShouldBeNull();
        metadata.ImdbRating.ShouldBeNull();
        metadata.ImdbId.ShouldBeNull();
        metadata.PosterUrl.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithDirectorsAndActors_KeepsThem()
    {
        // act
        var metadata = new MovieMetadata("Title", directors: new[] { "D1", "D2" }, actors: new[] { "A1" });

        // assert
        metadata.Directors.ShouldBe(new[] { "D1", "D2" });
        metadata.Actors.ShouldBe(new[] { "A1" });
    }

    [Fact]
    public void Constructor_NullDirectorsAndActors_YieldsEmpty()
    {
        // act
        var metadata = new MovieMetadata("Title");

        // assert
        metadata.Directors.ShouldBeEmpty();
        metadata.Actors.ShouldBeEmpty();
    }

    private static MovieMetadata Make(
        string title = "Title",
        string? synopsis = "Synopsis",
        ReleaseYear? releaseYear = null,
        Runtime? runtime = null,
        ImdbRating? imdbRating = null,
        ImdbId? imdbId = null,
        string? posterUrl = "poster.jpg",
        short? rottenTomatoesRating = 80,
        short? metaScore = 75,
        IEnumerable<string>? directors = null,
        IEnumerable<string>? actors = null) =>
        new(
            title,
            synopsis,
            releaseYear ?? new ReleaseYear(2000, Now),
            runtime ?? new Runtime(100),
            imdbRating ?? new ImdbRating(7.5m),
            imdbId ?? new ImdbId("tt0000001"),
            posterUrl,
            rottenTomatoesRating,
            metaScore,
            directors ?? new[] { "Director A" },
            actors ?? new[] { "Actor A" });

    [Fact]
    public void EqualsAndHash_DifferingTitle_AreNotEqualAndHashDiffers()
    {
        // arrange
        var left = Make();
        var right = Make(title: "Other");

        // act & assert
        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingSynopsis_AreNotEqualAndHashDiffers()
    {
        var left = Make();
        var right = Make(synopsis: "Other");

        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingReleaseYear_AreNotEqualAndHashDiffers()
    {
        var left = Make();
        var right = Make(releaseYear: new ReleaseYear(2001, Now));

        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingRuntime_AreNotEqualAndHashDiffers()
    {
        var left = Make();
        var right = Make(runtime: new Runtime(101));

        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingImdbRating_AreNotEqualAndHashDiffers()
    {
        var left = Make();
        var right = Make(imdbRating: new ImdbRating(7.6m));

        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingImdbId_AreNotEqualAndHashDiffers()
    {
        var left = Make();
        var right = Make(imdbId: new ImdbId("tt0000002"));

        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingPosterUrl_AreNotEqualAndHashDiffers()
    {
        var left = Make();
        var right = Make(posterUrl: "other.jpg");

        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingRottenTomatoesRating_AreNotEqualAndHashDiffers()
    {
        var left = Make();
        var right = Make(rottenTomatoesRating: 81);

        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingMetaScore_AreNotEqualAndHashDiffers()
    {
        var left = Make();
        var right = Make(metaScore: 76);

        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingDirectors_AreNotEqualAndHashDiffers()
    {
        var left = Make();
        var right = Make(directors: new[] { "Director B" });

        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_DifferingActors_AreNotEqualAndHashDiffers()
    {
        var left = Make();
        var right = Make(actors: new[] { "Actor B" });

        left.ShouldNotBe(right);
        right.ShouldNotBe(left);
        left.GetHashCode().ShouldNotBe(right.GetHashCode());
    }

    [Fact]
    public void EqualsAndHash_SameDirectorsButDifferingActors_AreNotEqual()
    {
        var left = Make();
        var right = Make(directors: new[] { "Director A" }, actors: new[] { "Actor B" });

        left.ShouldNotBe(right);
    }

    [Fact]
    public void EqualsAndHash_SameActorsButDifferingDirectors_AreNotEqual()
    {
        var left = Make();
        var right = Make(directors: new[] { "Director B" }, actors: new[] { "Actor A" });

        left.ShouldNotBe(right);
    }
}
