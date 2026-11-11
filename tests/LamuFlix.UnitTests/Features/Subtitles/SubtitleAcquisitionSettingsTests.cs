using System;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleAcquisitionSettingsTests
{
    [Fact]
    public void Require_SaveDisabled_ThrowsAcquisitionUnavailable()
    {
        // act
        var act = () => SubtitleAcquisitionSettings.Require(
            Microsoft.Extensions.Options.Options.Create(new SubtitlesOptions()),
            Microsoft.Extensions.Options.Options.Create(ConfiguredProvider()));

        // assert
        Should.Throw<SubtitleAcquisitionUnavailableException>(act);
    }

    [Fact]
    public void Require_EmptyPreferredLanguages_ThrowsAcquisitionUnavailable()
    {
        // act
        var act = () => SubtitleAcquisitionSettings.Require(
            Microsoft.Extensions.Options.Options.Create(new SubtitlesOptions { SaveEnabled = true }),
            Microsoft.Extensions.Options.Options.Create(ConfiguredProvider()));

        // assert
        Should.Throw<SubtitleAcquisitionUnavailableException>(act);
    }

    [Fact]
    public void Require_InvalidProviderSettings_ThrowsAcquisitionUnavailableWithoutLeakingTheKey()
    {
        // arrange
        const string sentinelKey = "sentinel-settings-key";
        var subtitles = Microsoft.Extensions.Options.Options.Create(
            new SubtitlesOptions { SaveEnabled = true, PreferredLanguages = ["en"] });
        var provider = Substitute.For<IOptions<SubtitleProviderOptions>>();
        provider.Value.Returns(_ => throw new OptionsValidationException(
            nameof(SubtitleProviderOptions),
            typeof(SubtitleProviderOptions),
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.ApiKey)} must be provided."]));

        // act
        var exception = Should.Throw<SubtitleAcquisitionUnavailableException>(
            () => SubtitleAcquisitionSettings.Require(subtitles, provider));

        // assert
        exception.Message.ShouldNotContain(sentinelKey);
        exception.InnerException.ShouldBeOfType<OptionsValidationException>();
    }

    [Fact]
    public void Require_ValidSettings_ReturnsTrimmedPreferencesInOrder()
    {
        // arrange
        var subtitles = Microsoft.Extensions.Options.Options.Create(new SubtitlesOptions
        {
            SaveEnabled = true,
            PreferredLanguages = [" en ", "pt-BR", "  "],
        });

        // act
        var settings = SubtitleAcquisitionSettings.Require(
            subtitles, Microsoft.Extensions.Options.Options.Create(ConfiguredProvider()));

        // assert
        settings.PreferredLanguages.ShouldBe(["en", "pt-BR"]);
    }

    [Fact]
    public void Require_NullOptions_ThrowsArgumentNullException()
    {
        // act
        var act = () => SubtitleAcquisitionSettings.Require(
            null!, Microsoft.Extensions.Options.Options.Create(ConfiguredProvider()));

        // assert
        Should.Throw<ArgumentNullException>(act);
    }

    private static SubtitleProviderOptions ConfiguredProvider() =>
        new()
        {
            ApiKey = "not-a-real-key",
            UserAgent = "LamuFlix/test",
            BaseUrl = "https://api.opensubtitles.invalid",
            DownloadAllowedHosts = ["dl.opensubtitles.invalid"],
            MaxDownloadBytes = 4096,
            RequestTimeout = TimeSpan.FromSeconds(5),
        };
}
