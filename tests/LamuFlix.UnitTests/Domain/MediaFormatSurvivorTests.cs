using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MediaFormatSurvivorTests
{
    [Theory]
    [InlineData(".MKV", "mkv")]
    [InlineData("  .Mp4 ", "mp4")]
    [InlineData("avi", "avi")]
    public void Constructor_MixedCaseOrDottedExtension_NormalizesToLowercaseWithoutDot(string extension, string expected)
    {
        // act
        var format = new MediaFormat(extension);

        // assert
        format.Extension.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    [InlineData("..mkv")]
    public void Constructor_BlankOrDotOnlyExtension_ThrowsArgumentExceptionWithParamNameAndMessage(string? extension)
    {
        // act
        var thrown = Should.Throw<ArgumentException>(() => new MediaFormat(extension));

        // assert
        thrown.ParamName.ShouldBe("extension");
        thrown.Message.ShouldContain("non-blank extension");
    }

    [Fact]
    public void TryCreate_ValidExtension_ReturnsTrueWithResult()
    {
        // act
        var created = MediaFormat.TryCreate(".MKV", out var result);

        // assert
        created.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Extension.ShouldBe("mkv");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".")]
    public void TryCreate_BlankOrDotOnlyExtension_ReturnsFalseWithNullResult(string? extension)
    {
        // act
        var created = MediaFormat.TryCreate(extension, out var result);

        // assert
        created.ShouldBeFalse();
        result.ShouldBeNull();
    }
}
