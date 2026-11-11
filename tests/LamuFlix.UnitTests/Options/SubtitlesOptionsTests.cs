using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class SubtitlesOptionsTests
{
    [Fact]
    public void SectionName_IsSubtitles()
    {
        // act
        var sectionName = SubtitlesOptions.SectionName;

        // assert
        sectionName.ShouldBe("Subtitles");
    }

    [Fact]
    public void Defaults_KeepAcquisitionDisabledAndPreferencesEmpty()
    {
        // act
        var options = new SubtitlesOptions();

        // assert
        options.SaveEnabled.ShouldBeFalse();
        options.PreferredLanguages.ShouldBeEmpty();
    }

    [Fact]
    public void Configuration_BindsSaveEnabledAndOrderedPreferencesWithoutInventingDefaults()
    {
        // arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.SaveEnabled)}"] = "true",
                [$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.PreferredLanguages)}:0"] = "en",
                [$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.PreferredLanguages)}:1"] = "pob",
            })
            .Build();

        // act
        var options = configuration.GetSection(SubtitlesOptions.SectionName).Get<SubtitlesOptions>();

        // assert
        options.ShouldNotBeNull();
        options.SaveEnabled.ShouldBeTrue();
        options.PreferredLanguages.ShouldBe(["en", "pob"]);
    }

    [Fact]
    public void AbsentSection_BindsSafeDemoDefaultsThroughTheOptionsPipeline()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection([]).Build());
        services.AddOptions<SubtitlesOptions>().BindConfiguration(SubtitlesOptions.SectionName);
        using var provider = services.BuildServiceProvider();

        // act
        var options = provider.GetRequiredService<IOptions<SubtitlesOptions>>().Value;

        // assert
        options.SaveEnabled.ShouldBeFalse();
        options.PreferredLanguages.ShouldBeEmpty();
    }
}
