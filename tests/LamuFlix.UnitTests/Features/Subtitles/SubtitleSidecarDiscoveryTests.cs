using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Features.Subtitles;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SubtitleSidecarDiscoveryTests
{
    private const string Primary = "Heat.1995.1080p.mkv";
    private const string Stem = "Heat.1995.1080p";

    [Fact]
    public void Discover_BareFilename_ReturnsSubtitlesTrackWithUndeterminedLanguage()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [$"{Stem}.srt"]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.Label.ShouldBe("Subtitles");
        track.Language.ShouldBe("und");
        track.IsDefault.ShouldBeFalse();
    }

    [Theory]
    [InlineData($"{Stem}.en.srt")]
    [InlineData($"{Stem}-en.srt")]
    [InlineData($"{Stem}.EN.SRT")]
    [InlineData($"{Stem}-En.Srt")]
    public void Discover_EnglishSuffixVariants_ReturnEnglishTrack(string fileName)
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [fileName]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.Label.ShouldBe("English");
        track.Language.ShouldBe("en");
        track.IsDefault.ShouldBeFalse();
    }

    [Theory]
    [InlineData($"{Stem}.pob.srt")]
    [InlineData($"{Stem}-pob.srt")]
    [InlineData($"{Stem}.POB.srt")]
    public void Discover_BrazilianPortugueseSuffixVariants_ReturnPortugueseBrazilTrack(string fileName)
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [fileName]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.Label.ShouldBe("Portuguese (Brazil)");
        track.Language.ShouldBe("pt-BR");
        track.IsDefault.ShouldBeFalse();
    }

    [Theory]
    [InlineData($"{Stem}.default.srt")]
    [InlineData($"{Stem}-default.srt")]
    [InlineData($"{Stem}.DEFAULT.srt")]
    public void Discover_DefaultMarkerVariants_SetIsDefault(string fileName)
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [fileName]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.Label.ShouldBe("Default");
        track.Language.ShouldBe("und");
        track.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void Discover_MultipleDefaultTracks_KeepBothSelectableNoneAutoEnabled()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(
            Primary,
            [$"{Stem}.default.srt", $"{Stem}-default.srt"]);

        // assert
        tracks.Count.ShouldBe(2);
        tracks.ShouldAllBe(track => track.IsDefault);
        tracks.Select(track => track.Label).ShouldBe(["Default", "Default (2)"]);
    }

    [Fact]
    public void Discover_UnknownSuffix_LabelsWithBasenameAndUndeterminedLanguage()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [$"{Stem}.forced.srt"]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.Label.ShouldBe($"{Stem}.forced");
        track.Language.ShouldBe("und");
        track.IsDefault.ShouldBeFalse();
        track.Label.ShouldNotContain("/");
    }

    [Theory]
    [InlineData("unrelated.srt")]
    [InlineData("Heat.1995.srt")]
    [InlineData("Heat.1995.1080p.srt.bak")]
    [InlineData("Heat.1995.1080p.en.ass")]
    [InlineData("Heat.1995.1080pXen.srt")]
    [InlineData("Heat.1995.1080p.en.forced.srt")]
    [InlineData("Heat.1995.1080p.")]
    public void Discover_IneligibleFileNames_AreExcluded(string fileName)
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [fileName]);

        // assert
        tracks.ShouldBeEmpty();
    }

    [Fact]
    public void Discover_MixedSet_SortsDefaultFirstThenLanguageLabelThenFileName()
    {
        // arrange
        var candidates = new[]
        {
            $"{Stem}.zzz.srt",
            $"{Stem}-en.srt",
            $"{Stem}.en.srt",
            $"{Stem}.pob.srt",
            $"{Stem}.srt",
            $"{Stem}.default.srt",
        };

        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, candidates);

        // assert
        tracks.Select(track => track.Label).ShouldBe(
        [
            "Default",
            "English",
            "English (2)",
            "Portuguese (Brazil)",
            $"{Stem}.zzz",
            "Subtitles",
        ]);
    }

    [Fact]
    public void Discover_LabelsNeverExposeDirectoryPaths()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [$"{Stem}.forced.srt"]);

        // assert
        tracks.ShouldAllBe(track => !track.Label.Contains('/') && !track.Label.Contains('\\'));
    }

    [Theory]
    [InlineData($"Subs/{Stem}-pob.srt")]
    [InlineData($"Subs/{Stem}.pob.srt")]
    [InlineData($"Subs/{Stem}-pb.srt")]
    public void Discover_SubsBrazilianPortugueseSuffixVariants_ReturnPortugueseBrazilTrack(string fileName)
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [fileName]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.FileName.ShouldBe(fileName);
        track.Label.ShouldBe("Portuguese (Brazil)");
        track.Language.ShouldBe("pt-BR");
        track.IsDefault.ShouldBeFalse();
    }

    [Theory]
    [InlineData($"Subs/{Stem}-por.srt", "pt", "Portuguese")]
    [InlineData($"Subs/{Stem}-pt.srt", "pt", "Portuguese")]
    [InlineData($"Subs/{Stem}-eng.srt", "en", "English")]
    [InlineData($"Subs/{Stem}.eng.srt", "en", "English")]
    [InlineData($"Subs/{Stem}-spa.srt", "es", "Spanish")]
    [InlineData($"Subs/{Stem}-es.srt", "es", "Spanish")]
    [InlineData($"{Stem}-eng.srt", "en", "English")]
    [InlineData($"{Stem}-spa.srt", "es", "Spanish")]
    public void Discover_BsPlayerCodeVariants_MapLanguageAndLabel(
        string fileName, string language, string label)
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [fileName]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.FileName.ShouldBe(fileName);
        track.Label.ShouldBe(label);
        track.Language.ShouldBe(language);
        track.IsDefault.ShouldBeFalse();
    }

    [Theory]
    [InlineData($"Subs/{Stem}-fre.srt", "fr", "French")]
    [InlineData($"Subs/{Stem}-ger.srt", "de", "German")]
    [InlineData($"Subs/{Stem}-dut.srt", "nl", "Dutch")]
    [InlineData($"Subs/{Stem}-ita.srt", "it", "Italian")]
    public void Discover_SubsIso639DashTwoCode_MapsLanguageAndLabel(
        string fileName, string language, string label)
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [fileName]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.FileName.ShouldBe(fileName);
        track.Label.ShouldBe(label);
        track.Language.ShouldBe(language);
        track.IsDefault.ShouldBeFalse();
    }

    [Theory]
    [InlineData("subs")]
    [InlineData("SUBS")]
    [InlineData("Subs")]
    public void Discover_SubsFolderName_IsCaseInsensitive(string folder)
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [$"{folder}/{Stem}-eng.srt"]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.Label.ShouldBe("English");
        track.Language.ShouldBe("en");
    }

    [Fact]
    public void Discover_SubsBackslashSeparator_MapsLanguageAndLabel()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [$"Subs\\{Stem}-eng.srt"]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.Label.ShouldBe("English");
        track.Language.ShouldBe("en");
    }

    [Fact]
    public void Discover_SubsNumberedDuplicate_StripsCounterToReadableLabel()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [$"Subs/{Stem}-eng(2).srt"]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.FileName.ShouldBe($"Subs/{Stem}-eng(2).srt");
        track.Label.ShouldBe("English");
        track.Language.ShouldBe("en");
    }

    [Fact]
    public void Discover_SameFolderAndSubsEnglishDuplicates_NumberLabelsDeterministically()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(
            Primary,
            [$"Subs/{Stem}-eng(2).srt", $"{Stem}.en.srt"]);

        // assert
        tracks.Select(track => track.Label).ShouldBe(["English", "English (2)"]);
        tracks.Select(track => track.FileName).ShouldBe([$"{Stem}.en.srt", $"Subs/{Stem}-eng(2).srt"]);
    }

    [Theory]
    [InlineData("Subs/English.srt", "English", "en")]
    [InlineData("Subs/Forced.eng.srt", "English (Forced)", "en")]
    [InlineData("Subs/SDH.eng.HI.srt", "English (SDH)", "en")]
    public void Discover_SubsReleaseGroupNames_MapReadableLabels(
        string fileName, string label, string language)
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [fileName]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.FileName.ShouldBe(fileName);
        track.Label.ShouldBe(label);
        track.Language.ShouldBe(language);
        track.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void Discover_SubsUnknownLeaf_ListsWithLeafLabelAndUndeterminedLanguage()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, ["Subs/random.srt"]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.FileName.ShouldBe("Subs/random.srt");
        track.Label.ShouldBe("random");
        track.Language.ShouldBe("und");
    }

    [Fact]
    public void Discover_SubsBareStem_ReturnsSubtitlesTrackWithUndeterminedLanguage()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [$"Subs/{Stem}.srt"]);

        // assert
        var track = tracks.ShouldHaveSingleItem();
        track.Label.ShouldBe("Subtitles");
        track.Language.ShouldBe("und");
        track.IsDefault.ShouldBeFalse();
    }

    [Theory]
    [InlineData("other/Heat.1995.1080p-en.srt")]
    [InlineData("Subs/nested/Heat.1995.1080p-en.srt")]
    [InlineData("Subs/../Heat.1995.1080p-en.srt")]
    [InlineData("../Heat.1995.1080p-en.srt")]
    public void Discover_NonSubsOrDeeperOrTraversalCandidates_AreExcluded(string fileName)
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, [fileName]);

        // assert
        tracks.ShouldBeEmpty();
    }

    [Fact]
    public void Discover_SubsTracks_LabelsNeverExposeDirectoryPaths()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(
            Primary,
            [$"Subs/{Stem}-eng.srt", "Subs/English.srt", "Subs/Forced.eng.srt", "Subs/random.srt"]);

        // assert
        tracks.Count.ShouldBe(4);
        tracks.ShouldAllBe(track => !track.Label.Contains('/') && !track.Label.Contains('\\'));
    }

    [Fact]
    public void Discover_PrimaryWithoutStem_ReturnsEmpty()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(".mkv", [".srt"]);

        // assert
        tracks.ShouldBeEmpty();
    }

    [Fact]
    public void Discover_EmptyCandidateSet_ReturnsEmpty()
    {
        // act
        var tracks = SubtitleSidecarDiscovery.Discover(Primary, Array.Empty<string>());

        // assert
        tracks.ShouldBeEmpty();
    }

    [Fact]
    public void Discover_RepeatedCallWithSameSet_ReturnsSameLabelsAndLanguages()
    {
        // arrange
        IReadOnlyList<string> candidates = [$"{Stem}.pob.srt", $"{Stem}.en.srt"];

        // act
        var first = SubtitleSidecarDiscovery.Discover(Primary, candidates);
        var second = SubtitleSidecarDiscovery.Discover(Primary, candidates.Reverse());

        // assert
        second.Select(track => (track.FileName, track.Label, track.Language, track.IsDefault)).ShouldBe(
            first.Select(track => (track.FileName, track.Label, track.Language, track.IsDefault)));
    }
}
