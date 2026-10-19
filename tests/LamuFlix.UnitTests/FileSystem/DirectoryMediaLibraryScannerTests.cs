using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using LamuFlix.UnitTests.Features;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.FileSystem;

public sealed class DirectoryMediaLibraryScannerTests
{
    private static readonly TimeProvider Now =
        new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

    private static readonly IOptions<LibraryOptions> Library =
        Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = "/library" });

    [Fact]
    public void Scan_RelativeFolderName_ResolvesUnderRootPath()
    {
        // arrange
        var fileSystem = FileSystemWith(("/library/2 Days in New York[2012]/2 Days in New York[2012].mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("2 Days in New York[2012]"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath(Path.Combine("/library", "2 Days in New York[2012]")),
            "2 Days in New York",
            new MediaFormat("mkv"),
            new ReleaseYear(2012, Now.GetUtcNow()),
            1024L));
    }

    [Fact]
    public void Scan_NestedRelativeFolder_ResolvesUnderRootPath()
    {
        // arrange
        var fileSystem = FileSystemWith(("/library/Collections/Alien (1979)/Alien.mkv", 2048));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Collections/Alien (1979)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath(Path.Combine("/library", "Collections/Alien (1979)")),
            "Alien",
            new MediaFormat("mkv"),
            new ReleaseYear(1979, Now.GetUtcNow()),
            2048L));
    }

    [Fact]
    public void Scan_RelativeFolderWithStrayTrailingSeparator_TrimsSeparatorBeforeResolving()
    {
        // arrange
        var fileSystem = FileSystemWith(("/library/Heat (1995)/Heat.mkv", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Heat (1995)/"));

        // assert
        result.Path.ShouldBe(new LibraryPath(Path.Combine("/library", "Heat (1995)")));
        result.Title.ShouldBe("Heat");
    }

    [Fact]
    public void Scan_AbsolutePath_UsesPathUnchangedAndIgnoresRootPath()
    {
        // arrange
        var fileSystem = FileSystemWith(("/elsewhere/Inception (2010)/Inception.mkv", 1500));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("/elsewhere/Inception (2010)"));

        // assert
        result.Path.ShouldBe(new LibraryPath("/elsewhere/Inception (2010)"));
        result.Title.ShouldBe("Inception");
    }

    [Fact]
    public void Scan_MissingRelativeFolder_ThrowsDirectoryNotFoundNamingResolvedPath()
    {
        // arrange
        var fileSystem = FileSystemWith(("/library/Inception (2010)/Inception.mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<DirectoryNotFoundException>(
            () => scanner.Scan(new LibraryPath("Absent (1999)")));

        // assert
        exception.Message.ShouldContain(Path.Combine("/library", "Absent (1999)"));
    }

    [Fact]
    public void Scan_RelativeFolderWithoutVideo_ThrowsInvalidOperationNamingResolvedPath()
    {
        // arrange
        var fileSystem = FileSystemWith(("/library/Inception (2010)/Inception.srt", 9000));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<InvalidOperationException>(
            () => scanner.Scan(new LibraryPath("Inception (2010)")));

        // assert
        exception.Message.ShouldContain(Path.Combine("/library", "Inception (2010)"));
    }

    [Fact]
    public void Scan_ParenthesisedYear_ReturnsTitleAndYear()
    {
        // arrange
        var fileSystem = FileSystemWith(("/library/Inception (2010)/Inception (2010).mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("/library/Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("/library/Inception (2010)"),
            "Inception",
            new MediaFormat("mkv"),
            new ReleaseYear(2010, Now.GetUtcNow()),
            1024L));
    }

    [Fact]
    public void Scan_NoYearMarker_ReturnsWholeNameAndNoYear()
    {
        // arrange
        var fileSystem = FileSystemWith(("/library/Inception/Inception.mkv", 2048));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("/library/Inception"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("/library/Inception"),
            "Inception",
            new MediaFormat("mkv"),
            null,
            2048L));
    }

    [Theory]
    [MemberData(nameof(FolderWithValidYear))]
    public void Scan_ValidYearMarker_ReturnsTitleAndYear(string folderName, string expectedTitle, int expectedYear)
    {
        // arrange
        var fileSystem = FileSystemWith(($"/library/{folderName}/{folderName}.mkv", 4096));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath($"/library/{folderName}"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath($"/library/{folderName}"),
            expectedTitle,
            new MediaFormat("mkv"),
            new ReleaseYear(expectedYear, Now.GetUtcNow()),
            4096L));
    }

    [Theory]
    [MemberData(nameof(FolderWithoutUsableYear))]
    public void Scan_NoUsableYearMarker_ReturnsWholeNameAndNoYear(string folderName, string expectedTitle)
    {
        // arrange
        var fileSystem = FileSystemWith(($"/library/{folderName}/{folderName}.mkv", 4096));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath($"/library/{folderName}"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath($"/library/{folderName}"),
            expectedTitle,
            new MediaFormat("mkv"),
            null,
            4096L));
    }

    [Fact]
    public void Scan_UnsupportedOnly_ThrowsInvalidOperation()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ("/library/Inception (2010)/Inception.srt", 9000),
            ("/library/Inception (2010)/Inception.nfo", 8000),
            ("/library/Inception (2010)/Inception.txt", 7000),
            ("/library/Inception (2010)/Inception.wmv", 6000));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<InvalidOperationException>(
            () => scanner.Scan(new LibraryPath("/library/Inception (2010)")));

        // assert
        exception.Message.ShouldContain("/library/Inception (2010)");
    }

    [Fact]
    public void Scan_MixedSupportedAndUnsupported_SelectsSupportedEvenWhenSmaller()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ("/library/Inception (2010)/Inception.mkv", 1500),
            ("/library/Inception (2010)/Inception.srt", 9999));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("/library/Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("/library/Inception (2010)"),
            "Inception",
            new MediaFormat("mkv"),
            new ReleaseYear(2010, Now.GetUtcNow()),
            1500L));
    }

    [Fact]
    public void Scan_SeveralSupported_SelectsLargest()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ("/library/Inception (2010)/small.mkv", 10),
            ("/library/Inception (2010)/large.mp4", 5000),
            ("/library/Inception (2010)/medium.avi", 3000));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("/library/Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("/library/Inception (2010)"),
            "Inception",
            new MediaFormat("mp4"),
            new ReleaseYear(2010, Now.GetUtcNow()),
            5000L));
    }

    [Fact]
    public void Scan_SizeTieBetweenNames_SelectsOrdinalLowestName()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ("/library/Inception (2010)/a.mkv", 4096),
            ("/library/Inception (2010)/B.mp4", 4096));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("/library/Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("/library/Inception (2010)"),
            "Inception",
            new MediaFormat("mp4"),
            new ReleaseYear(2010, Now.GetUtcNow()),
            4096L));
    }

    [Fact]
    public void Scan_MissingFolder_ThrowsDirectoryNotFound()
    {
        // arrange
        var fileSystem = FileSystemWith(("/library/Inception (2010)/Inception.mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<DirectoryNotFoundException>(
            () => scanner.Scan(new LibraryPath("/library/Absent (1999)")));

        // assert
        exception.Message.ShouldContain("/library/Absent (1999)");
    }

    [Fact]
    public void Scan_EmptyFolder_ThrowsInvalidOperation()
    {
        // arrange
        var fileSystem = FileSystemWith();
        fileSystem.AddDirectory("/library");
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<InvalidOperationException>(
            () => scanner.Scan(new LibraryPath("/library")));

        // assert
        exception.Message.ShouldContain("/library");
    }

    [Fact]
    public void Scan_VideoOnlyInSubfolder_ThrowsInvalidOperation()
    {
        // arrange
        var fileSystem = FileSystemWith(("/library/Inception (2010)/Featurettes/extra.mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<InvalidOperationException>(
            () => scanner.Scan(new LibraryPath("/library/Inception (2010)")));

        // assert
        exception.Message.ShouldContain("/library/Inception (2010)");
    }

    [Fact]
    public void Scan_TopLevelVideoBesideLargerNestedOne_SelectsTopLevel()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ("/library/Inception (2010)/Inception.mkv", 1500),
            ("/library/Inception (2010)/Featurettes/deleted-scene.mp4", 99999));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("/library/Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("/library/Inception (2010)"),
            "Inception",
            new MediaFormat("mkv"),
            new ReleaseYear(2010, Now.GetUtcNow()),
            1500L));
    }

    public static TheoryData<string, string> SupportedExtension() => new()
    {
        { ".mkv", "mkv" },
        { ".mp4", "mp4" },
        { ".avi", "avi" },
        { ".m4v", "m4v" },
        { ".rmvb", "rmvb" },
        { ".rm", "rm" },
        { ".flv", "flv" },
    };

    [Theory]
    [MemberData(nameof(SupportedExtension))]
    public void Scan_SupportedExtension_SelectsAndNormalisesFormat(string extension, string expectedFormat)
    {
        // arrange
        var fileSystem = FileSystemWith(($"/library/Clip/Clip{extension}", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("/library/Clip"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("/library/Clip"),
            "Clip",
            new MediaFormat(expectedFormat),
            null,
            512L));
    }

    [Fact]
    public void Scan_UpperCaseExtension_SelectsAndNormalisesFormat()
    {
        // arrange
        var fileSystem = FileSystemWith(("/library/Clip/MOVIE.MKV", 777));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("/library/Clip"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("/library/Clip"),
            "Clip",
            new MediaFormat("mkv"),
            null,
            777L));
    }

    [Fact]
    public void Scan_MixedCaseExtension_SelectsAndNormalisesFormat()
    {
        // arrange
        var fileSystem = FileSystemWith(("/library/Clip/Movie.Mp4", 888));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("/library/Clip"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("/library/Clip"),
            "Clip",
            new MediaFormat("mp4"),
            null,
            888L));
    }

    public static TheoryData<string, string, int> FolderWithValidYear() => new()
    {
        { "The Matrix [1999]", "The Matrix", 1999 },
        { "The Lord of the Rings - The Fellowship (2001)", "The Lord of the Rings - The Fellowship", 2001 },
        { "(2010)", "(2010)", 2010 },
        { "The_Matrix (1999)", "The_Matrix", 1999 },
        { "Old (1888)", "Old", 1888 },
        { "Future (2031)", "Future", 2031 },
        { "  Inception (2010)", "Inception", 2010 },
        { "2 Days in New York[2012]", "2 Days in New York", 2012 },
        { "The Invite (2026) [1080p] [WEBRip] [5.1] [YTS.GG - YTS.BZ]", "The Invite", 2026 },
        { "The.Matrix.1999", "The Matrix", 1999 },
    };

    public static TheoryData<string, string> FolderWithoutUsableYear() => new()
    {
        { "Inception (2010) Revisited", "Inception (2010) Revisited" },
        { "Inception (2010]", "Inception (2010]" },
        { "Inception (201)", "Inception (201)" },
        { "Inception (20100)", "Inception (20100)" },
        { "Old (1887)", "Old (1887)" },
        { "Future (2032)", "Future (2032)" },
        { "Blade Runner 2049", "Blade Runner 2049" },
        { "(٢٠١٠)", "(٢٠١٠)" },
    };

    private static MockFileSystem FileSystemWith(params (string Path, int Size)[] files)
    {
        var entries = new Dictionary<string, MockFileData>(files.Length, StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            entries.Add(file.Path, new MockFileData(new byte[file.Size]));
        }

        return new MockFileSystem(entries, new MockFileSystemOptions());
    }
}
