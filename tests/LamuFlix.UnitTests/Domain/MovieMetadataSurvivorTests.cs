using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieMetadataSurvivorTests
{
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
}
