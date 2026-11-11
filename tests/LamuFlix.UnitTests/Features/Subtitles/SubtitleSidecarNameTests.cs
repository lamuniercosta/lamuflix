using System;
using LamuFlix.Core.Features.Subtitles;
using Shouldly;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleSidecarNameTests
{
    [Theory]
    [InlineData("Film.mkv", "Film.srt")]
    [InlineData("Film.1995.1080p.mkv", "Film.1995.1080p.srt")]
    [InlineData("Film", "Film.srt")]
    [InlineData("Film.MKV", "Film.srt")]
    public void For_PrimaryVideoName_ReturnsTheExactBasenameSidecar(string primaryFileName, string expected)
    {
        // act
        var name = SubtitleSidecarName.For(primaryFileName);

        // assert
        name.ShouldBe(expected);
        name.ShouldNotContain("/");
        name.ShouldNotContain("\\");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("folder/Film.mkv")]
    [InlineData("folder\\Film.mkv")]
    [InlineData(".mkv")]
    public void For_UnusablePrimaryVideoName_ThrowsArgumentException(string? primaryFileName)
    {
        // act
        var act = () => SubtitleSidecarName.For(primaryFileName!);

        // assert
        Should.Throw<ArgumentException>(act);
    }
}
