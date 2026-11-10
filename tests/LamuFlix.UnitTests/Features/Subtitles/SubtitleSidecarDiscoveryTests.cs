using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Features.Subtitles;
using Shouldly;
using Xunit;

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
