using System;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleSelectionTests
{
    private static readonly string[] EnglishThenPortuguese = ["en", "pt-BR"];

    [Fact]
    public void Select_FirstPreferenceHasAnEligibleCandidate_ReturnsItAndNeverConsidersLaterPreferences()
    {
        // arrange
        SubtitleCandidate[] candidates =
        [
            new(2, "pt-BR", DownloadCount: 900, MatchesMovieHash: true),
            new(1, "en", DownloadCount: 2, MatchesMovieHash: false),
        ];

        // act
        var selected = SubtitleSelection.Select(candidates, EnglishThenPortuguese);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(1);
        selected.Language.ShouldBe("en");
    }

    [Fact]
    public void Select_FirstPreferenceHasNoCandidate_FallsBackToTheSecondPreference()
    {
        // arrange
        SubtitleCandidate[] candidates =
        [
            new(7, "pt-BR", DownloadCount: 1, MatchesMovieHash: false),
            new(9, "de", DownloadCount: 500, MatchesMovieHash: true),
        ];

        // act
        var selected = SubtitleSelection.Select(candidates, EnglishThenPortuguese);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(7);
    }

    [Fact]
    public void Select_NoCandidateInAnyPreference_ReturnsNullInsteadOfAnUnpreferredLanguage()
    {
        // arrange
        SubtitleCandidate[] candidates =
        [
            new(3, "de", DownloadCount: 5000, MatchesMovieHash: true),
            new(4, "fr", DownloadCount: 4000, MatchesMovieHash: true),
        ];

        // act
        var selected = SubtitleSelection.Select(candidates, EnglishThenPortuguese);

        // assert
        selected.ShouldBeNull();
    }

    [Fact]
    public void Select_EmptyPreferences_ReturnsNull()
    {
        // arrange
        SubtitleCandidate[] candidates = [new(1, "en", DownloadCount: 1, MatchesMovieHash: false)];

        // act
        var selected = SubtitleSelection.Select(candidates, []);

        // assert
        selected.ShouldBeNull();
    }

    [Fact]
    public void Select_TieWithinLanguage_PrefersTheMovieHashMatchOverAHigherDownloadCount()
    {
        // arrange
        SubtitleCandidate[] candidates =
        [
            new(1, "en", DownloadCount: 9000, MatchesMovieHash: false),
            new(2, "en", DownloadCount: 1, MatchesMovieHash: true),
        ];

        // act
        var selected = SubtitleSelection.Select(candidates, EnglishThenPortuguese);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(2);
    }

    [Fact]
    public void Select_TieOnHashStatus_PrefersTheHighestDownloadCount()
    {
        // arrange
        SubtitleCandidate[] candidates =
        [
            new(1, "en", DownloadCount: 5, MatchesMovieHash: true),
            new(2, "en", DownloadCount: 80, MatchesMovieHash: true),
            new(3, "en", DownloadCount: 40, MatchesMovieHash: true),
        ];

        // act
        var selected = SubtitleSelection.Select(candidates, EnglishThenPortuguese);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(2);
    }

    [Fact]
    public void Select_TieOnHashStatusAndDownloadCount_PrefersTheLowestFileId()
    {
        // arrange
        SubtitleCandidate[] candidates =
        [
            new(30, "en", DownloadCount: 12, MatchesMovieHash: true),
            new(10, "en", DownloadCount: 12, MatchesMovieHash: true),
            new(20, "en", DownloadCount: 12, MatchesMovieHash: true),
        ];

        // act
        var selected = SubtitleSelection.Select(candidates, EnglishThenPortuguese);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(10);
    }

    [Fact]
    public void Select_CandidateOrderDoesNotChangeTheOutcome()
    {
        // arrange
        SubtitleCandidate[] forward =
        [
            new(1, "en", DownloadCount: 5, MatchesMovieHash: false),
            new(2, "en", DownloadCount: 5, MatchesMovieHash: true),
            new(3, "pt-BR", DownloadCount: 99, MatchesMovieHash: true),
        ];
        SubtitleCandidate[] reversed = [forward[2], forward[1], forward[0]];

        // act
        var first = SubtitleSelection.Select(forward, EnglishThenPortuguese);
        var second = SubtitleSelection.Select(reversed, EnglishThenPortuguese);

        // assert
        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        second.FileId.ShouldBe(first.FileId);
    }

    [Fact]
    public void Select_LanguageCodesDifferOnlyByCaseAndPadding_StillMatch()
    {
        // arrange
        SubtitleCandidate[] candidates = [new(4, " PT-br ", DownloadCount: 1, MatchesMovieHash: false)];

        // act
        var selected = SubtitleSelection.Select(candidates, EnglishThenPortuguese);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(4);
    }

    [Theory]
    [InlineData("EN", "en")]
    [InlineData("EN", " EN ")]
    [InlineData("pt-BR", "PT-br")]
    public void Select_MatchDiffersInCaseOrPadding_ReturnsTheConfiguredPreferenceCode(string preference, string candidateLanguage)
    {
        // arrange
        SubtitleCandidate[] candidates = [new(4, candidateLanguage, DownloadCount: 1, MatchesMovieHash: false)];

        // act
        var selected = SubtitleSelection.Select(candidates, [preference]);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(4);
        selected.Language.ShouldBe(preference);
    }

    [Fact]
    public void Select_BlankPreferenceInTheList_IsSkippedWithoutThrowing()
    {
        // arrange
        SubtitleCandidate[] candidates = [new(1, "en", DownloadCount: 1, MatchesMovieHash: false)];

        // act
        var selected = SubtitleSelection.Select(candidates, [" ", "en"]);

        // assert
        selected.ShouldNotBeNull();
        selected.FileId.ShouldBe(1);
    }

    [Fact]
    public void Select_NullCandidates_Throws()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => SubtitleSelection.Select(null!, EnglishThenPortuguese);

        // assert
        Should.Throw<ArgumentNullException>(act);
    }
}
