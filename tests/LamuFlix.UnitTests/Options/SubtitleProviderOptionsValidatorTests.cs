using System;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class SubtitleProviderOptionsValidatorTests
{
    [Fact]
    public void Validate_CompleteAcquisitionSettings_Succeeds()
    {
        // act
        var result = Validate(Complete());

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_LoopbackHttpBaseUrl_FailsNamingBaseUrl()
    {
        // arrange
        var options = Complete() with { BaseUrl = "http://localhost:8080/api/v1/" };

        // act
        var result = Validate(options);

        // assert
        ShouldFailNaming(result, nameof(SubtitleProviderOptions.BaseUrl));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_BlankApiKey_FailsNamingApiKey(string apiKey)
    {
        // act
        var result = Validate(Complete() with { ApiKey = apiKey });

        // assert
        ShouldFailNaming(result, nameof(SubtitleProviderOptions.ApiKey));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_BlankUserAgent_FailsNamingUserAgent(string userAgent)
    {
        // act
        var result = Validate(Complete() with { UserAgent = userAgent });

        // assert
        ShouldFailNaming(result, nameof(SubtitleProviderOptions.UserAgent));
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://api.example.invalid/v1/")]
    [InlineData("http://api.example.invalid/v1/")]
    public void Validate_InvalidLocalBaseUrl_FailsNamingBaseUrlWithoutEchoingTheValue(string baseUrl)
    {
        // act
        var result = Validate(Complete() with { BaseUrl = baseUrl });

        // assert
        ShouldFailNaming(result, nameof(SubtitleProviderOptions.BaseUrl));
        (result.FailureMessage ?? string.Empty).ShouldNotContain(baseUrl);
    }

    [Fact]
    public void Validate_NoDownloadAllowedHosts_FailsNamingDownloadAllowedHosts()
    {
        // act
        var result = Validate(Complete() with { DownloadAllowedHosts = [] });

        // assert
        ShouldFailNaming(result, nameof(SubtitleProviderOptions.DownloadAllowedHosts));
    }

    [Fact]
    public void Validate_BlankDownloadAllowedHost_FailsNamingTheOffendingEntry()
    {
        // act
        var result = Validate(Complete() with { DownloadAllowedHosts = ["dl.example.invalid", ""] });

        // assert
        ShouldFailNaming(result, $"{nameof(SubtitleProviderOptions.DownloadAllowedHosts)}[1]");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_BlankDownloadLinkHost_FailsNamingDownloadLinkHost(string linkHost)
    {
        // act
        var result = Validate(Complete() with { DownloadLinkHost = linkHost });

        // assert
        result.Failed.ShouldBeTrue();
        string.Join("; ", result.Failures).ShouldContain("Subtitles:OpenSubtitles:DownloadLinkHost must be provided.");
    }

    [Fact]
    public void Validate_DownloadLinkHostMissingFromDownloadAllowedHosts_FailsNamingBothSettings()
    {
        // act
        var result = Validate(Complete() with { DownloadLinkHost = "www.example.invalid" });

        // assert
        result.Failed.ShouldBeTrue();
        var message = string.Join("; ", result.Failures);
        message.ShouldContain("Subtitles:OpenSubtitles:DownloadLinkHost must be listed in");
        message.ShouldContain(nameof(SubtitleProviderOptions.DownloadAllowedHosts));
    }

    [Fact]
    public void Validate_DownloadLinkHostMatchesAllowedHostCaseInsensitivelyAndTrimmed_Succeeds()
    {
        // arrange
        var options = Complete() with
        {
            DownloadAllowedHosts = [" www.example.com "],
            DownloadLinkHost = "WWW.Example.com",
        };

        // act
        var result = Validate(options);

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_PaddedAndDifferentlyCasedDownloadLinkHost_Succeeds()
    {
        // arrange
        var options = Complete() with
        {
            DownloadAllowedHosts = ["dl.example.invalid"],
            DownloadLinkHost = "  DL.Example.INVALID  ",
        };

        // act
        var result = Validate(options);

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Theory]
    [InlineData("https://www.opensubtitles.com")]
    [InlineData("user@www.opensubtitles.com")]
    [InlineData("www.opensubtitles.com:443")]
    [InlineData("www.opensubtitles.com/path")]
    [InlineData("www.opensubtitles.com?x=1")]
    [InlineData("www.opensubtitles.com.")]
    public void Validate_NonBareDownloadLinkHost_FailsEvenWhenListed(string linkHost)
    {
        // arrange
        var options = Complete() with
        {
            DownloadAllowedHosts = [linkHost, "www.opensubtitles.com"],
            DownloadLinkHost = linkHost,
        };

        // act
        var result = Validate(options);

        // assert
        result.Failed.ShouldBeTrue();
        string.Join("; ", result.Failures).ShouldContain("must be a host name only");
        SubtitleProviderOptionsValidator.IsLinkHostAllowed(options).ShouldBeFalse();
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    public void Validate_LoopbackBareDownloadLinkHostInAllowlist_Succeeds(string linkHost)
    {
        // arrange
        var options = Complete() with
        {
            DownloadAllowedHosts = [linkHost],
            DownloadLinkHost = linkHost,
        };

        // act
        var result = Validate(options);

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
        SubtitleProviderOptionsValidator.IsLinkHostAllowed(options).ShouldBeTrue();
    }

    [Fact]
    public void IsLinkHostAllowed_BlankDownloadLinkHost_ReturnsFalse()
    {
        // arrange
        var options = Complete() with { DownloadLinkHost = " " };

        // act
        var allowed = SubtitleProviderOptionsValidator.IsLinkHostAllowed(options);

        // assert
        allowed.ShouldBeFalse();
    }

    [Fact]
    public void IsLinkHostAllowed_DownloadLinkHostInTheAllowlist_ReturnsTrue()
    {
        // act
        var allowed = SubtitleProviderOptionsValidator.IsLinkHostAllowed(Complete());

        // assert
        allowed.ShouldBeTrue();
    }

    [Fact]
    public void IsLinkHostAllowed_NullOptions_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => { SubtitleProviderOptionsValidator.IsLinkHostAllowed(null!); };

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveMaxDownloadBytes_FailsNamingMaxDownloadBytes(long maxDownloadBytes)
    {
        // act
        var result = Validate(Complete() with { MaxDownloadBytes = maxDownloadBytes });

        // assert
        ShouldFailNaming(result, nameof(SubtitleProviderOptions.MaxDownloadBytes));
    }

    [Fact]
    public void Validate_NonPositiveRequestTimeout_FailsNamingRequestTimeout()
    {
        // act
        var result = Validate(Complete() with { RequestTimeout = TimeSpan.Zero });

        // assert
        ShouldFailNaming(result, nameof(SubtitleProviderOptions.RequestTimeout));
    }

    private static SubtitleProviderOptions Complete() => new()
    {
        ApiKey = "not-a-real-key",
        BaseUrl = "https://api.example.invalid/api/v1/",
        UserAgent = "LamuFlix test",
        DownloadAllowedHosts = ["dl.example.invalid"],
        DownloadLinkHost = "dl.example.invalid",
        MaxDownloadBytes = 1048576,
        RequestTimeout = TimeSpan.FromSeconds(15),
    };

    private static ValidateOptionsResult Validate(SubtitleProviderOptions options) =>
        new SubtitleProviderOptionsValidator().Validate(global::Microsoft.Extensions.Options.Options.DefaultName, options);

    private static void ShouldFailNaming(ValidateOptionsResult result, string memberName)
    {
        result.Failed.ShouldBeTrue();
        string.Join("; ", result.Failures).ShouldContain(memberName);
    }
}
