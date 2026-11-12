using System;
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
    public void Validate_EnabledWithEmptyPreferences_SucceedsBecauseEmptyPreferencesAreValid()
    {
        // act
        var result = Validate(new SubtitlesOptions { SaveEnabled = true });

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
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

    [Fact]
    public void Validate_UnsupportedPreferenceCode_FailsNamingTheOffendingEntry()
    {
        // arrange
        var options = new SubtitlesOptions
        {
            PreferredLanguages = ["en", "xx-YY"],
        };

        // act
        var result = Validate(options);

        // assert
        ShouldFailNaming(result, $"{nameof(SubtitlesOptions.PreferredLanguages)}[1]");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCandidateStoreMaxEntries_FailsNamingTheBound(int value)
    {
        // act
        var result = Validate(new SubtitlesOptions { CandidateStoreMaxEntries = value });

        // assert
        ShouldFailNaming(result, nameof(SubtitlesOptions.CandidateStoreMaxEntries));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveMaxSubtitleDownloadBytes_FailsNamingTheBound(int value)
    {
        // act
        var result = Validate(new SubtitlesOptions { MaxSubtitleDownloadBytes = value });

        // assert
        ShouldFailNaming(result, nameof(SubtitlesOptions.MaxSubtitleDownloadBytes));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_NonPositiveMaxSubtitlePublicationAttempts_FailsNamingTheBound(int value)
    {
        // act
        var result = Validate(new SubtitlesOptions { MaxSubtitlePublicationAttempts = value });

        // assert
        ShouldFailNaming(result, nameof(SubtitlesOptions.MaxSubtitlePublicationAttempts));
    }

    [Fact]
    public void Validate_ZeroCandidateTokenTtl_FailsNamingTheBound()
    {
        // act
        var result = Validate(new SubtitlesOptions { CandidateTokenTtl = TimeSpan.Zero });

        // assert
        ShouldFailNaming(result, nameof(SubtitlesOptions.CandidateTokenTtl));
    }

    [Fact]
    public void Validate_DefaultBounds_Succeed()
    {
        // act
        var result = Validate(new SubtitlesOptions());

        // assert
        result.ShouldBe(ValidateOptionsResult.Success);
    }

    private static ValidateOptionsResult Validate(SubtitlesOptions options) =>
        new SubtitlesOptionsValidator().Validate(global::Microsoft.Extensions.Options.Options.DefaultName, options);

    private static void ShouldFailNaming(ValidateOptionsResult result, string memberName)
    {
        result.Failed.ShouldBeTrue();
        string.Join("; ", result.Failures).ShouldContain(memberName);
    }
}
