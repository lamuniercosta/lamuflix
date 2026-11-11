using System;
using System.Text;
using LamuFlix.Infrastructure.Adapters;

namespace LamuFlix.UnitTests.Adapters;

public sealed class SubtitleRequestUriPolicyTests
{
    private static readonly string[] AllowedHosts = ["subtitles.example.test", "Download.Example.test"];

    [Theory]
    [InlineData("https://subtitles.example.test/api/v1/subtitles")]
    [InlineData("https://download.example.test/srt/1.srt")]
    public void IsPermitted_HttpsAllowlistedHost_ReturnsTrue(string url)
    {
        // act
        var permitted = SubtitleRequestUriPolicy.IsPermitted(new Uri(url), AllowedHosts);

        // assert
        permitted.ShouldBeTrue();
    }

    [Theory]
    [InlineData("https://not-allowlisted.example.test/api/v1/subtitles")]
    [InlineData("http://subtitles.example.test/api/v1/subtitles")]
    [InlineData("file:///C:/library/Heat.srt")]
    [InlineData("ftp://subtitles.example.test/subtitles")]
    [InlineData("https://subtitles.example.test.evil.test/api")]
    public void IsPermitted_UnsafeSchemeOrUnlistedHost_ReturnsFalse(string url)
    {
        // act
        var permitted = SubtitleRequestUriPolicy.IsPermitted(new Uri(url), AllowedHosts);

        // assert
        permitted.ShouldBeFalse();
    }

    [Fact]
    public void IsPermitted_LoopbackHttp_IsPermittedForTheLocalStubStack()
    {
        // act
        var permitted = SubtitleRequestUriPolicy.IsPermitted(new Uri("http://127.0.0.1:8123/api/v1/subtitles"), ["127.0.0.1"]);

        // assert
        permitted.ShouldBeTrue();
    }

    [Fact]
    public void IsPermitted_EmptyAllowlist_ReturnsFalse()
    {
        // act
        var permitted = SubtitleRequestUriPolicy.IsPermitted(new Uri("https://subtitles.example.test/api"), []);

        // assert
        permitted.ShouldBeFalse();
    }

    [Fact]
    public void IsPermitted_RelativeOrNullUri_ReturnsFalse()
    {
        // act
        var relative = SubtitleRequestUriPolicy.IsPermitted(new Uri("/api", UriKind.Relative), AllowedHosts);
        var missing = SubtitleRequestUriPolicy.IsPermitted(null, AllowedHosts);

        // assert
        relative.ShouldBeFalse();
        missing.ShouldBeFalse();
    }

    [Fact]
    public void TryBuildEndpoint_BaseUrlWithoutTrailingSlash_KeepsTheRelativePathUnderTheBase()
    {
        // act
        var endpoint = SubtitleRequestUriPolicy.TryBuildEndpoint("https://subtitles.example.test", "api/v1/subtitles");

        // assert
        endpoint.ShouldNotBeNull();
        endpoint.AbsoluteUri.ShouldBe("https://subtitles.example.test/api/v1/subtitles");
    }

    [Fact]
    public void TryBuildEndpoint_BaseUrlWithSubPath_AppendsToThatSubPath()
    {
        // act
        var endpoint = SubtitleRequestUriPolicy.TryBuildEndpoint("https://subtitles.example.test/base/", "api/v1/download");

        // assert
        endpoint.ShouldNotBeNull();
        endpoint.AbsoluteUri.ShouldBe("https://subtitles.example.test/base/api/v1/download");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("../api")]
    public void TryBuildEndpoint_UnusableBaseUrl_ReturnsNull(string baseUrl)
    {
        // act
        var endpoint = SubtitleRequestUriPolicy.TryBuildEndpoint(baseUrl, "api/v1/subtitles");

        // assert
        endpoint.ShouldBeNull();
    }

    [Fact]
    public void IsAllowedHost_BlankAllowlistEntries_AreIgnored()
    {
        // act
        var allowed = SubtitleRequestUriPolicy.IsAllowedHost("subtitles.example.test", ["", "  ", "SUBTITLES.example.test"]);

        // assert
        allowed.ShouldBeTrue();
    }

    [Fact]
    public void IsAllowedHost_NoEntries_ReturnsFalse()
    {
        // act
        var allowed = SubtitleRequestUriPolicy.IsAllowedHost("subtitles.example.test", []);

        // assert
        allowed.ShouldBeFalse();
    }
}

public sealed class SubtitlePayloadValidatorTests
{
    private const long MaxBytes = 1_024;
    private const string ValidSrt = "1\n00:00:01,000 --> 00:00:03,500\ntext\n";

    [Theory]
    [InlineData("1\n00:00:01,000 --> 00:00:03,500\ntext\n")]
    [InlineData("1\r\n00:00:01,000 --> 00:00:03,500\r\ntext\r\n")]
    [InlineData("1\n00:00:01.000 --> 00:00:03.500\ntext\n")]
    [InlineData("\uFEFF1\n00:00:01,000 --> 00:00:03,500\ntext\n")]
    public void IsAcceptable_SrtWithACueLine_ReturnsTrue(string payload)
    {
        // act
        var acceptable = SubtitlePayloadValidator.IsAcceptable(Encoding.UTF8.GetBytes(payload), MaxBytes);

        // assert
        acceptable.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html></html>")]
    [InlineData("{\"a\":1}")]
    [InlineData("[1]")]
    [InlineData("PK\u0003\u0004")]
    [InlineData("\u001F\u008B")]
    [InlineData("plain text without cues")]
    [InlineData("1\n00:00:01 --> 00:00:03\ntext\n")]
    public void IsAcceptable_BlockedPrefixOrMissingCue_ReturnsFalse(string payload)
    {
        // act
        var acceptable = SubtitlePayloadValidator.IsAcceptable(Encoding.UTF8.GetBytes(payload), MaxBytes);

        // assert
        acceptable.ShouldBeFalse();
    }

    [Fact]
    public void IsAcceptable_InvalidUtf8Bytes_ReturnsFalse()
    {
        // arrange
        var payload = new byte[] { 0xFF, 0xFE, 0x00, 0x00, (byte)'1' };

        // act
        var acceptable = SubtitlePayloadValidator.IsAcceptable(payload, MaxBytes);

        // assert
        acceptable.ShouldBeFalse();
    }

    [Fact]
    public void IsAcceptable_PayloadAboveTheBound_ReturnsFalse()
    {
        // arrange
        var payload = Encoding.UTF8.GetBytes(string.Concat(ValidSrt, new string('x', (int)MaxBytes)));

        // act
        var acceptable = SubtitlePayloadValidator.IsAcceptable(payload, MaxBytes);

        // assert
        acceptable.ShouldBeFalse();
    }

    [Fact]
    public void IsAcceptable_PayloadExactlyAtTheBound_ReturnsTrue()
    {
        // arrange
        var suffix = new string('x', (int)MaxBytes - Encoding.UTF8.GetByteCount(ValidSrt));
        var payload = Encoding.UTF8.GetBytes(string.Concat(ValidSrt, suffix));

        // act
        var acceptable = SubtitlePayloadValidator.IsAcceptable(payload, MaxBytes);

        // assert
        payload.Length.ShouldBe((int)MaxBytes);
        acceptable.ShouldBeTrue();
    }

    [Fact]
    public void IsAcceptable_CueLineAfterTheFirstLines_IsStillDetected()
    {
        // arrange
        var payload = Encoding.UTF8.GetBytes(string.Concat("WEBVTT-ish preamble\n\n", ValidSrt));

        // act
        var acceptable = SubtitlePayloadValidator.IsAcceptable(payload, MaxBytes);

        // assert
        acceptable.ShouldBeTrue();
    }
}
