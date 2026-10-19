using System;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.FileSystem;

namespace LamuFlix.UnitTests.FileSystem;

public sealed class FolderNameCleanerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [MemberData(nameof(LibraryFolderNames))]
    public void Clean_LibraryFolderName_ReturnsTitleAndYear(string folderName, string expectedTitle, int? expectedYear)
    {
        // act
        var result = FolderNameCleaner.Clean(folderName, Now);

        // assert
        result.ShouldBe(new CleanedFolderName(
            expectedTitle,
            expectedYear is null ? null : new ReleaseYear(expectedYear.Value, Now)));
    }

    [Theory]
    [InlineData("www.Torrents.example - Heat (1995)", "Heat", 1995)]
    [InlineData("www.site.org -Heat (1995)", "www.site.org -Heat", 1995)]
    [InlineData("The.Matrix.1999.1080p.BluRay.x264-GROUP", "The Matrix", 1999)]
    [InlineData("The_Matrix_1999", "The Matrix", 1999)]
    [InlineData("Blade.Runner.2049", "Blade Runner 2049", null)]
    [InlineData("1917 (2019)", "1917", 2019)]
    [InlineData("2001 A Space Odyssey (1968)", "2001 A Space Odyssey", 1968)]
    [InlineData("Alien (1979) [Director's Cut]", "Alien", 1979)]
    [InlineData("Movie (2010) Part 2", "Movie (2010) Part 2", null)]
    [InlineData("Movie (2010) WEB-DL", "Movie", 2010)]
    public void Clean_ScenePrefixesTagsAndCompetingYears_ChoosesReleaseYear(
        string folderName,
        string expectedTitle,
        int? expectedYear)
    {
        // act
        var result = FolderNameCleaner.Clean(folderName, Now);

        // assert
        result.ShouldBe(new CleanedFolderName(
            expectedTitle,
            expectedYear is null ? null : new ReleaseYear(expectedYear.Value, Now)));
    }

    [Theory]
    [InlineData("Barbie.2023.720p.HDCAM-C1NEM4[TGx]", "Barbie", 2023)]
    [InlineData("Barbie 2023 720p HDCAM-C1NEM4[TGx]", "Barbie", 2023)]
    [InlineData("Conclave.2024.1080p.AMZN.WEBRip.DDP5.1.x265.10bit-GalaxyRG265[TGx]", "Conclave", 2024)]
    [InlineData("Parasite.2019.1080p.BluRay.KORSUB.x264-GROUP", "Parasite", 2019)]
    [InlineData("Movie 2010 Part 2", "Movie 2010 Part 2", null)]
    public void Clean_YearFollowedByReleaseTag_TakesYearDespiteUnknownTokens(
        string folderName,
        string expectedTitle,
        int? expectedYear)
    {
        // act
        var result = FolderNameCleaner.Clean(folderName, Now);

        // assert
        result.ShouldBe(new CleanedFolderName(
            expectedTitle,
            expectedYear is null ? null : new ReleaseYear(expectedYear.Value, Now)));
    }

    [Theory]
    [InlineData("The Lord of the Rings The Fellowship of the Ring  EXTENDED[2001]", "The Lord of the Rings The Fellowship of the Ring", 2001)]
    [InlineData("Aliens Director's Cut (1986)", "Aliens", 1986)]
    [InlineData("Blade Runner The Final Cut Remastered (1982)", "Blade Runner The Final Cut", 1982)]
    [InlineData("The Lord of the Rings The Two Towers EXTENDED", "The Lord of the Rings The Two Towers", null)]
    [InlineData("Extended", "Extended", null)]
    public void Clean_TrailingEditionLabel_IsDropped(string folderName, string expectedTitle, int? expectedYear)
    {
        // act
        var result = FolderNameCleaner.Clean(folderName, Now);

        // assert
        result.ShouldBe(new CleanedFolderName(
            expectedTitle,
            expectedYear is null ? null : new ReleaseYear(expectedYear.Value, Now)));
    }

    [Fact]
    public void Clean_FutureBeyondAllowedWindow_ReturnsWholeNameWithoutYear()
    {
        // act
        var result = FolderNameCleaner.Clean("Future (2032)", Now);

        // assert
        result.ShouldBe(new CleanedFolderName("Future (2032)", null));
    }

    public static TheoryData<string, string, int?> LibraryFolderNames() => new()
    {
        { "2 Days in New York[2012]", "2 Days in New York", 2012 },
        { "The Invite (2026) [1080p] [WEBRip] [5.1] [YTS.GG - YTS.BZ]", "The Invite", 2026 },
        { "The.Matrix.1999", "The Matrix", 1999 },
        { "Inception (2010) Remastered", "Inception", 2010 },
        { "Inception (2010]", "Inception (2010]", null },
        { "(2010)", "(2010)", 2010 },
        { "Do The Right Thing [20th Anniversary SE].1989.1080p.BluRay", "Do The Right Thing [20th Anniversary SE]", 1989 },
    };
}
