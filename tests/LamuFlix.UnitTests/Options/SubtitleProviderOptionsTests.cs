using System;
using System.Collections.Generic;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Configuration;

namespace LamuFlix.UnitTests.Options;

public sealed class SubtitleProviderOptionsTests
{
    [Fact]
    public void SectionName_IsTheVendorConfigurationPath()
    {
        // act
        var sectionName = SubtitleProviderOptions.SectionName;

        // assert
        sectionName.ShouldBe("Subtitles:OpenSubtitles");
    }

    [Fact]
    public void Defaults_InventNoGuessedValues()
    {
        // act
        var options = new SubtitleProviderOptions();

        // assert
        options.ApiKey.ShouldBeEmpty();
        options.BaseUrl.ShouldBeEmpty();
        options.UserAgent.ShouldBeEmpty();
        options.DownloadAllowedHosts.ShouldBeEmpty();
        options.MaxDownloadBytes.ShouldBe(0);
        options.RequestTimeout.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void Configuration_BindsEveryAcquisitionSettingWithoutDefaulting()
    {
        // arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.ApiKey)}"] = "not-a-real-key",
                [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.BaseUrl)}"] = "https://example.invalid/api/v1/",
                [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.UserAgent)}"] = "LamuFlix test",
                [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.DownloadAllowedHosts)}:0"] = "dl.example.invalid",
                [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.MaxDownloadBytes)}"] = "1048576",
                [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.RequestTimeout)}"] = "00:00:15",
            })
            .Build();

        // act
        var options = configuration.GetSection(SubtitleProviderOptions.SectionName).Get<SubtitleProviderOptions>();

        // assert
        options.ShouldNotBeNull();
        options.ApiKey.ShouldBe("not-a-real-key");
        options.BaseUrl.ShouldBe("https://example.invalid/api/v1/");
        options.UserAgent.ShouldBe("LamuFlix test");
        options.DownloadAllowedHosts.ShouldBe(["dl.example.invalid"]);
        options.MaxDownloadBytes.ShouldBe(1048576);
        options.RequestTimeout.ShouldBe(TimeSpan.FromSeconds(15));
    }
}
