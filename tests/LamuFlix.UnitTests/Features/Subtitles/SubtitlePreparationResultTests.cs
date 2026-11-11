using System;
using LamuFlix.Core.Features.Subtitles;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitlePreparationResultTests
{
    [Fact]
    public void Saved_CarriesTheMatchedPreferredLanguage()
    {
        // act
        var result = SubtitlePreparationResult.Saved("pob");

        // assert
        result.Outcome.ShouldBe(SubtitlePrepareOutcome.Saved);
        result.Outcome.Code.ShouldBe("saved");
        result.SelectedLanguage.ShouldBe("pob");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Saved_WithoutAMatchedLanguage_Throws(string selectedLanguage)
    {
        // act
        var exception = Should.Throw<ArgumentException>(() => SubtitlePreparationResult.Saved(selectedLanguage));

        // assert
        exception.ParamName.ShouldBe("selectedLanguage");
    }

    [Fact]
    public void AlreadyPresent_HasNoSelectedLanguage()
    {
        // act
        var result = SubtitlePreparationResult.AlreadyPresent();

        // assert
        result.Outcome.ShouldBe(SubtitlePrepareOutcome.AlreadyPresent);
        result.Outcome.Code.ShouldBe("alreadyPresent");
        result.SelectedLanguage.ShouldBeNull();
    }

    [Fact]
    public void NoMatch_HasNoSelectedLanguage()
    {
        // act
        var result = SubtitlePreparationResult.NoMatch();

        // assert
        result.Outcome.ShouldBe(SubtitlePrepareOutcome.NoMatch);
        result.Outcome.Code.ShouldBe("noMatch");
        result.SelectedLanguage.ShouldBeNull();
    }
}
