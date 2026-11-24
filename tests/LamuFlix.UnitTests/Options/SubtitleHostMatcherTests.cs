using System;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class SubtitleHostMatcherTests
{
    [Theory]
    [InlineData("www.opensubtitles.com")]
    [InlineData(" WWW.OpenSubtitles.com ")]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    public void IsBareHost_BareHostName_ReturnsTrue(string value)
    {
        // act
        var bare = SubtitleHostMatcher.IsBareHost(value);

        // assert
        bare.ShouldBeTrue();
    }

    [Theory]
    [InlineData("https://www.opensubtitles.com")]
    [InlineData("user@www.opensubtitles.com")]
    [InlineData("www.opensubtitles.com:443")]
    [InlineData("www.opensubtitles.com/path")]
    [InlineData("www.opensubtitles.com?x=1")]
    [InlineData("www.opensubtitles.com.")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void IsBareHost_NonBareValue_ReturnsFalse(string? value)
    {
        // act
        var bare = SubtitleHostMatcher.IsBareHost(value);

        // assert
        bare.ShouldBeFalse();
    }

    [Fact]
    public void IsListed_TrimmedAndCaseInsensitiveMatch_ReturnsTrue()
    {
        // act
        var listed = SubtitleHostMatcher.IsListed(" WWW.Example.com ", [" www.example.COM "]);

        // assert
        listed.ShouldBeTrue();
    }

    [Fact]
    public void IsListed_BlankEntries_AreSkipped()
    {
        // act
        var listed = SubtitleHostMatcher.IsListed("dl.example.invalid", ["", "  ", "dl.example.invalid"]);

        // assert
        listed.ShouldBeTrue();
    }

    [Fact]
    public void IsListed_NonMember_ReturnsFalse()
    {
        // act
        var listed = SubtitleHostMatcher.IsListed("other.example.invalid", ["dl.example.invalid"]);

        // assert
        listed.ShouldBeFalse();
    }

    [Fact]
    public void IsListed_BlankHost_ReturnsFalse()
    {
        // act
        var listed = SubtitleHostMatcher.IsListed("   ", ["dl.example.invalid"]);

        // assert
        listed.ShouldBeFalse();
    }

    [Fact]
    public void IsListed_NullHost_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => { SubtitleHostMatcher.IsListed(null!, ["dl.example.invalid"]); };

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void IsListed_NullAllowlist_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => { SubtitleHostMatcher.IsListed("dl.example.invalid", null!); };

        // assert
        Should.Throw<ArgumentNullException>(act);
    }
}
