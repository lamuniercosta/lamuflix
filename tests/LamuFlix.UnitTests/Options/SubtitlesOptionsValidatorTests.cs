using LamuFlix.Core.Options;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class SubtitlesOptionsValidatorTests
{
    [Fact]
    public void Validate_DisabledWithNoPreferences_Succeeds()
    {
        // act
        var result = Validate(new SubtitlesOptions());

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_EnabledWithOrderedPreferences_Succeeds()
    {
        // act
        var result = Validate(new SubtitlesOptions
        {
            SaveEnabled = true,
            PreferredLanguages = ["en", "pob"],
        });

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    [Fact]
    public void Validate_EnabledWithoutPreferences_FailsNamingPreferredLanguages()
    {
        // act
        var result = Validate(new SubtitlesOptions { SaveEnabled = true });

        // assert
        ShouldFailNaming(result, nameof(SubtitlesOptions.PreferredLanguages));
    }

    [Fact]
    public void Validate_EnabledWithBlankPreference_failsNamingTheOffendingEntry()
    {
        // arrange
        var options = new SubtitlesOptions
        {
            SaveEnabled = true,
            PreferredLanguages = ["en", " "],
        };

        // act
        var result = Validate(options);

        // assert
        ShouldFailNaming(result, $"{nameof(SubtitlesOptions.PreferredLanguages)}[1]");
        result.Failed.ShouldBeTrue();
    }

    private static ValidateOptionsResult Validate(SubtitlesOptions options) =>
        new SubtitlesOptionsValidator().Validate(global::Microsoft.Extensions.Options.Options.DefaultName, options);

    private static void ShouldFailNaming(ValidateOptionsResult result, string memberName)
    {
        result.Failed.ShouldBeTrue();
        string.Join("; ", result.Failures).ShouldContain(memberName);
    }
}
