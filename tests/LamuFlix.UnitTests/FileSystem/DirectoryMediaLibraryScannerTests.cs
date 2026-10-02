using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using LamuFlix.UnitTests.Features;

namespace LamuFlix.UnitTests.FileSystem;

public sealed class DirectoryMediaLibraryScannerTests
{
    private static readonly TimeProvider Now =
        new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Scan_ParenthesisedYear_ReturnsTitleAndYear()
    {
        // arrange
        var fileSystem = FileSystemWith(("C:/library/Inception (2010)/Inception (2010).mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var result = scanner.Scan(new LibraryPath("C:/library/Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("C:/library/Inception (2010)"),
            "Inception",
            new MediaFormat("mkv"),
            new ReleaseYear(2010, Now.GetUtcNow()),
            1024L));
    }

    [Fact]
    public void Scan_NoYearMarker_ReturnsWholeNameAndNoYear()
    {
        // arrange
        var fileSystem = FileSystemWith(("C:/library/Inception/Inception.mkv", 2048));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var result = scanner.Scan(new LibraryPath("C:/library/Inception"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("C:/library/Inception"),
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
        var fileSystem = FileSystemWith(($"C:/library/{folderName}/{folderName}.mkv", 4096));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var result = scanner.Scan(new LibraryPath($"C:/library/{folderName}"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath($"C:/library/{folderName}"),
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
        var fileSystem = FileSystemWith(($"C:/library/{folderName}/{folderName}.mkv", 4096));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var result = scanner.Scan(new LibraryPath($"C:/library/{folderName}"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath($"C:/library/{folderName}"),
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
            ("C:/library/Inception (2010)/Inception.srt", 9000),
            ("C:/library/Inception (2010)/Inception.nfo", 8000),
            ("C:/library/Inception (2010)/Inception.txt", 7000),
            ("C:/library/Inception (2010)/Inception.wmv", 6000));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var exception = Should.Throw<InvalidOperationException>(
            () => scanner.Scan(new LibraryPath("C:/library/Inception (2010)")));

        // assert
        exception.Message.ShouldContain("C:/library/Inception (2010)");
    }

    [Fact]
    public void Scan_MixedSupportedAndUnsupported_SelectsSupportedEvenWhenSmaller()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ("C:/library/Inception (2010)/Inception.mkv", 1500),
            ("C:/library/Inception (2010)/Inception.srt", 9999));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var result = scanner.Scan(new LibraryPath("C:/library/Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("C:/library/Inception (2010)"),
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
            ("C:/library/Inception (2010)/small.mkv", 10),
            ("C:/library/Inception (2010)/large.mp4", 5000),
            ("C:/library/Inception (2010)/medium.avi", 3000));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var result = scanner.Scan(new LibraryPath("C:/library/Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("C:/library/Inception (2010)"),
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
            ("C:/library/Inception (2010)/a.mkv", 4096),
            ("C:/library/Inception (2010)/B.mp4", 4096));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var result = scanner.Scan(new LibraryPath("C:/library/Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("C:/library/Inception (2010)"),
            "Inception",
            new MediaFormat("mp4"),
            new ReleaseYear(2010, Now.GetUtcNow()),
            4096L));
    }

    [Fact]
    public void Scan_MissingFolder_ThrowsDirectoryNotFound()
    {
        // arrange
        var fileSystem = FileSystemWith(("C:/library/Inception (2010)/Inception.mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var exception = Should.Throw<DirectoryNotFoundException>(
            () => scanner.Scan(new LibraryPath("C:/library/Absent (1999)")));

        // assert
        exception.Message.ShouldContain("C:/library/Absent (1999)");
    }

    [Fact]
    public void Scan_EmptyFolder_ThrowsInvalidOperation()
    {
        // arrange
        var fileSystem = FileSystemWith();
        fileSystem.AddDirectory("C:/library");
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var exception = Should.Throw<InvalidOperationException>(
            () => scanner.Scan(new LibraryPath("C:/library")));

        // assert
        exception.Message.ShouldContain("C:/library");
    }

    [Fact]
    public void Scan_VideoOnlyInSubfolder_ThrowsInvalidOperation()
    {
        // arrange
        var fileSystem = FileSystemWith(("C:/library/Inception (2010)/Featurettes/extra.mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var exception = Should.Throw<InvalidOperationException>(
            () => scanner.Scan(new LibraryPath("C:/library/Inception (2010)")));

        // assert
        exception.Message.ShouldContain("C:/library/Inception (2010)");
    }

    [Fact]
    public void Scan_TopLevelVideoBesideLargerNestedOne_SelectsTopLevel()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ("C:/library/Inception (2010)/Inception.mkv", 1500),
            ("C:/library/Inception (2010)/Featurettes/deleted-scene.mp4", 99999));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var result = scanner.Scan(new LibraryPath("C:/library/Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("C:/library/Inception (2010)"),
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
    };

    [Theory]
    [MemberData(nameof(SupportedExtension))]
    public void Scan_SupportedExtension_SelectsAndNormalisesFormat(string extension, string expectedFormat)
    {
        // arrange
        var fileSystem = FileSystemWith(($"C:/library/Clip/Clip{extension}", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var result = scanner.Scan(new LibraryPath("C:/library/Clip"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("C:/library/Clip"),
            "Clip",
            new MediaFormat(expectedFormat),
            null,
            512L));
    }

    [Fact]
    public void Scan_UpperCaseExtension_SelectsAndNormalisesFormat()
    {
        // arrange
        var fileSystem = FileSystemWith(("C:/library/Clip/MOVIE.MKV", 777));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var result = scanner.Scan(new LibraryPath("C:/library/Clip"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("C:/library/Clip"),
            "Clip",
            new MediaFormat("mkv"),
            null,
            777L));
    }

    [Fact]
    public void Scan_MixedCaseExtension_SelectsAndNormalisesFormat()
    {
        // arrange
        var fileSystem = FileSystemWith(("C:/library/Clip/Movie.Mp4", 888));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now);

        // act
        var result = scanner.Scan(new LibraryPath("C:/library/Clip"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("C:/library/Clip"),
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
    };

    public static TheoryData<string, string> FolderWithoutUsableYear() => new()
    {
        { "Inception (2010) Remastered", "Inception (2010) Remastered" },
        { "Inception (2010]", "Inception (2010]" },
        { "Inception (201)", "Inception (201)" },
        { "Inception (20100)", "Inception (20100)" },
        { "Old (1887)", "Old (1887)" },
        { "Future (2032)", "Future (2032)" },
        { "The.Matrix.1999", "The.Matrix.1999" },
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
