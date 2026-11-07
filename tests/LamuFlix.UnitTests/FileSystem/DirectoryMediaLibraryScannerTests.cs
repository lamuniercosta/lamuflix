using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.FileSystem;
using LamuFlix.UnitTests.Features;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LamuFlix.UnitTests.FileSystem;

public sealed class DirectoryMediaLibraryScannerTests
{
    private const string Root = "/library";

    private static readonly TimeProvider Now =
        new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

    private static readonly IOptions<LibraryOptions> Library =
        Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = Root });

    [Fact]
    public void Scan_RelativeFolderName_ResolvesUnderRootPathAndReturnsCanonicalIdentity()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/2 Days in New York[2012]/2 Days in New York[2012].mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("2 Days in New York[2012]"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("2 Days in New York[2012]"),
            "2 Days in New York",
            new MediaFormat("mkv"),
            new ReleaseYear(2012, Now.GetUtcNow()),
            1024L));
    }

    [Fact]
    public void Scan_NestedRelativeFolder_ResolvesUnderRootPath()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Collections/Alien (1979)/Alien.mkv", 2048));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Collections/Alien (1979)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("Collections/Alien (1979)"),
            "Alien",
            new MediaFormat("mkv"),
            new ReleaseYear(1979, Now.GetUtcNow()),
            2048L));
    }

    [Fact]
    public void Scan_RedundantDotSegmentsAndBackslashes_ReturnsCanonicalRelativeIdentity()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/Heat.mkv", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("./Heat (1995)/."));

        // assert
        result.Path.ShouldBe(new LibraryPath("Heat (1995)"));
        result.Title.ShouldBe("Heat");
    }

    [Fact]
    public void Scan_RelativeFolderWithStrayTrailingSeparator_TrimsSeparatorBeforeResolving()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/Heat.mkv", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Heat (1995)/"));

        // assert
        result.Path.ShouldBe(new LibraryPath("Heat (1995)"));
        result.Title.ShouldBe("Heat");
    }

    [Fact]
    public void Scan_AbsolutePathUnderRoot_ReturnsCanonicalRelativeIdentity()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Inception (2010)/Inception.mkv", 1500));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath($"{Root}/Inception (2010)"));

        // assert
        result.Path.ShouldBe(new LibraryPath("Inception (2010)"));
        result.Title.ShouldBe("Inception");
    }

    [Fact]
    public void Scan_AbsoluteRoot_ReturnsRootIdentityDot()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/root.mkv", 700));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath(Root));

        // assert
        result.Path.ShouldBe(new LibraryPath("."));
    }

    [Fact]
    public void Scan_RootIdentityDot_ResolvesToConfiguredRoot()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/root.mkv", 700));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("."));

        // assert
        result.Path.ShouldBe(new LibraryPath("."));
        result.SizeBytes.ShouldBe(700);
    }

    [Theory]
    [InlineData("/elsewhere/Inception (2010)")]
    [InlineData("D:/outside/Inception (2010)")]
    public void Scan_AbsolutePathOutsideRoot_ThrowsNotFound(string folder)
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Inception (2010)/Inception.mkv", 1500));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<NotFoundException>(() => scanner.Scan(new LibraryPath(folder)));

        // assert
        exception.Message.ShouldNotContain(folder);
    }

    [Fact]
    public void Scan_ProcessCurrentDirectoryDiffersFromRoot_StillResolvesAgainstRoot()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/Heat.mkv", 512));
        fileSystem.Directory.SetCurrentDirectory("/tmp/elsewhere");
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Heat (1995)"));

        // assert
        result.Path.ShouldBe(new LibraryPath("Heat (1995)"));
        result.Format.ShouldBe(new MediaFormat("mkv"));
    }

    [Fact]
    public void Scan_MissingRelativeFolder_ThrowsNotFound()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Inception (2010)/Inception.mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<NotFoundException>(
            () => scanner.Scan(new LibraryPath("Absent (1999)")));

        // assert
        exception.Message.ShouldNotContain(Root);
    }

    [Fact]
    public void Scan_RelativeFolderWithoutVideo_ThrowsInvalidTransitionNamingResolvedPath()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Inception (2010)/Inception.srt", 9000));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<InvalidTransitionException>(
            () => scanner.Scan(new LibraryPath("Inception (2010)")));

        // assert
        exception.Message.ShouldContain(Path.Combine(Root, "Inception (2010)"));
    }

    [Fact]
    public void Scan_ParenthesisedYear_ReturnsTitleAndYear()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Inception (2010)/Inception (2010).mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("Inception (2010)"),
            "Inception",
            new MediaFormat("mkv"),
            new ReleaseYear(2010, Now.GetUtcNow()),
            1024L));
    }

    [Fact]
    public void Scan_NoYearMarker_ReturnsWholeNameAndNoYear()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Inception/Inception.mkv", 2048));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Inception"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("Inception"),
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
        var fileSystem = FileSystemWith(($"{Root}/{folderName}/{folderName}.mkv", 4096));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath(folderName));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath(folderName),
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
        var fileSystem = FileSystemWith(($"{Root}/{folderName}/{folderName}.mkv", 4096));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath(folderName));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath(folderName),
            expectedTitle,
            new MediaFormat("mkv"),
            null,
            4096L));
    }

    [Fact]
    public void Scan_UnsupportedOnly_ThrowsInvalidTransition()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/Inception (2010)/Inception.srt", 9000),
            ($"{Root}/Inception (2010)/Inception.nfo", 8000),
            ($"{Root}/Inception (2010)/Inception.txt", 7000),
            ($"{Root}/Inception (2010)/Inception.wmv", 6000));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<InvalidTransitionException>(
            () => scanner.Scan(new LibraryPath("Inception (2010)")));

        // assert
        exception.Message.ShouldContain("Inception (2010)");
    }

    [Fact]
    public void Scan_MixedSupportedAndUnsupported_SelectsSupportedEvenWhenSmaller()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/Inception (2010)/Inception.mkv", 1500),
            ($"{Root}/Inception (2010)/Inception.srt", 9999));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("Inception (2010)"),
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
            ($"{Root}/Inception (2010)/small.mkv", 10),
            ($"{Root}/Inception (2010)/large.mp4", 5000),
            ($"{Root}/Inception (2010)/medium.avi", 3000));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("Inception (2010)"),
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
            ($"{Root}/Inception (2010)/a.mkv", 4096),
            ($"{Root}/Inception (2010)/B.mp4", 4096));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("Inception (2010)"),
            "Inception",
            new MediaFormat("mp4"),
            new ReleaseYear(2010, Now.GetUtcNow()),
            4096L));
    }

    [Fact]
    public void Scan_MissingFolder_ThrowsNotFound()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Inception (2010)/Inception.mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<NotFoundException>(
            () => scanner.Scan(new LibraryPath("Absent (1999)")));

        // assert
        exception.Message.ShouldNotContain("Absent (1999)");
    }

    [Fact]
    public void Scan_EmptyRootFolder_ThrowsInvalidTransition()
    {
        // arrange
        var fileSystem = FileSystemWith();
        fileSystem.AddDirectory(Root);
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<InvalidTransitionException>(
            () => scanner.Scan(new LibraryPath(".")));

        // assert
        exception.Message.ShouldContain(Root);
    }

    [Fact]
    public void Scan_VideoOnlyInSubfolder_ThrowsInvalidTransition()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Inception (2010)/Featurettes/extra.mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<InvalidTransitionException>(
            () => scanner.Scan(new LibraryPath("Inception (2010)")));

        // assert
        exception.Message.ShouldContain("Inception (2010)");
    }

    [Fact]
    public void Scan_TopLevelVideoBesideLargerNestedOne_SelectsTopLevel()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/Inception (2010)/Inception.mkv", 1500),
            ($"{Root}/Inception (2010)/Featurettes/deleted-scene.mp4", 99999));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Inception (2010)"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("Inception (2010)"),
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
    public void Scan_SupportedExtensions_UnchangedAfterSharedHolderExtraction(string extension, string expectedFormat)
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Clip/Clip{extension}", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Clip"));

        // assert
        SupportedVideoExtensions.Contains(extension).ShouldBeTrue();
        result.Format.ShouldBe(new MediaFormat(expectedFormat));
    }

    [Theory]
    [MemberData(nameof(SupportedExtension))]
    public void SupportedVideoExtensions_UpperCaseAndMixedCaseMembership_StaysOrdinalIgnoreCase(
        string extension,
        string _)
    {
        // arrange
        var upper = extension.ToUpperInvariant();
        var mixed = extension[..1].ToUpperInvariant() + extension[1..];

        // act
        var results = new[]
        {
            SupportedVideoExtensions.Contains(extension),
            SupportedVideoExtensions.Contains(upper),
            SupportedVideoExtensions.Contains(mixed),
        };

        // assert
        results.ShouldAllBe(isSupported => isSupported);
    }

    [Fact]
    public void SupportedVideoExtensions_Membership_IsExactlyThePreviouslyInlinedScannerSet()
    {
        // arrange
        string[] expected = [".mkv", ".mp4", ".avi", ".m4v", ".rmvb", ".rm", ".flv"];

        // act
        var membership = SupportedVideoExtensions.All;

        // assert
        membership.ShouldBe(expected, ignoreOrder: true);
        SupportedVideoExtensions.Contains(".wmv").ShouldBeFalse();
        SupportedVideoExtensions.Contains(string.Empty).ShouldBeFalse();
    }

    [Theory]
    [MemberData(nameof(SupportedExtension))]
    public void Scan_SupportedExtension_SelectsAndNormalisesFormat(string extension, string expectedFormat)
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Clip/Clip{extension}", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Clip"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("Clip"),
            "Clip",
            new MediaFormat(expectedFormat),
            null,
            512L));
    }

    [Fact]
    public void Scan_UpperCaseExtension_SelectsAndNormalisesFormat()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Clip/MOVIE.MKV", 777));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Clip"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("Clip"),
            "Clip",
            new MediaFormat("mkv"),
            null,
            777L));
    }

    [Fact]
    public void Scan_MixedCaseExtension_SelectsAndNormalisesFormat()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Clip/Movie.Mp4", 888));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Clip"));

        // assert
        result.ShouldBe(new ScannedMovie(
            new LibraryPath("Clip"),
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

    public static TheoryData<string, string> TokenBasename() => new()
    {
        { "movie-sample.mkv", "excluded" },
        { "Movie.Sample.mkv", "excluded" },
        { "Movie_Sample.mkv", "excluded" },
        { "movie trailer.mkv", "excluded" },
        { "movie-featurette.mkv", "excluded" },
        { "movie.extras.mkv", "excluded" },
        { "movie-SAMPLE.mkv", "excluded" },
        { "sampler.mkv", "kept" },
        { "extraspecial.mkv", "kept" },
        { "sampled.mkv", "kept" },
        { "trailers.mkv", "kept" },
        { "my-sample-collection.mkv", "excluded" },
        { "movie.mkv", "kept" },
    };

    [Theory]
    [MemberData(nameof(TokenBasename))]
    public void Scan_TokenBasenames_ExcludesOnlyWholeAlphanumericTokens(string fileName, string expectation)
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/{fileName}", 2048));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var act = () => scanner.Scan(new LibraryPath("Heat (1995)"));

        // assert
        if (expectation == "excluded")
        {
            Should.Throw<InvalidTransitionException>(act);
        }
        else
        {
            act().Format.ShouldBe(new MediaFormat("mkv"));
        }
    }

    [Fact]
    public void Scan_ExcludedDecoysBesideFeatureFile_SelectsFeatureFile()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/Heat (1995)/Heat.mkv", 900),
            ($"{Root}/Heat (1995)/heat-sample.mkv", 90000),
            ($"{Root}/Heat (1995)/Heat.trailer.mp4", 80000),
            ($"{Root}/Heat (1995)/featurette.mkv", 70000));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.Scan(new LibraryPath("Heat (1995)"));

        // assert
        result.Format.ShouldBe(new MediaFormat("mkv"));
        result.SizeBytes.ShouldBe(900);
    }

    [Fact]
    public void Scan_OnlyExcludedFiles_ThrowsInvalidTransition()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/Heat (1995)/Heat.sample.mkv", 9000),
            ($"{Root}/Heat (1995)/Heat-trailer.mp4", 8000),
            ($"{Root}/Heat (1995)/extras.mkv", 7000));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<InvalidTransitionException>(
            () => scanner.Scan(new LibraryPath("Heat (1995)")));

        // assert
        exception.Message.ShouldContain("Heat (1995)");
    }

    [Fact]
    public void ResolvePrimaryVideo_FeatureFolder_ReturnsFilePairedWithItsFormat()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/Heat (1995)/Heat.avi", 900),
            ($"{Root}/Heat (1995)/Heat-sample.mkv", 90000));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.ResolvePrimaryVideo(new LibraryPath("Heat (1995)"));

        // assert
        result.ShouldBe(new PrimaryVideo(
            new LibraryPath(Path.Combine(Root, "Heat (1995)", "Heat.avi")),
            new MediaFormat("avi")));
    }

    [Fact]
    public void ResolvePrimaryVideo_RelativeFolder_ResolvesUnderRootPath()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/Heat.mkv", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.ResolvePrimaryVideo(new LibraryPath("Heat (1995)"));

        // assert
        result.Path.ShouldBe(new LibraryPath(Path.Combine(Root, "Heat (1995)", "Heat.mkv")));
        result.Format.ShouldBe(new MediaFormat("mkv"));
    }

    [Fact]
    public void ResolvePrimaryVideo_RootIdentityDot_ResolvesUnderConfiguredRoot()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/root.mkv", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.ResolvePrimaryVideo(new LibraryPath("."));

        // assert
        result.Path.ShouldBe(new LibraryPath(Path.Combine(Root, "root.mkv")));
    }

    [Fact]
    public void ResolvePrimaryVideo_ProcessCurrentDirectoryDiffersFromRoot_StillResolvesAgainstRoot()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/Heat.mkv", 512));
        fileSystem.Directory.SetCurrentDirectory("/tmp/elsewhere");
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.ResolvePrimaryVideo(new LibraryPath("Heat (1995)"));

        // assert
        result.Path.ShouldBe(new LibraryPath(Path.Combine(Root, "Heat (1995)", "Heat.mkv")));
    }

    [Fact]
    public void ResolvePrimaryVideo_AbsolutePathUnderRoot_ReturnsAbsoluteFile()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/Heat.mkv", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.ResolvePrimaryVideo(new LibraryPath($"{Root}/Heat (1995)"));

        // assert
        result.Path.ShouldBe(new LibraryPath(Path.Combine(Root, "Heat (1995)", "Heat.mkv")));
    }

    [Fact]
    public void ResolvePrimaryVideo_AbsolutePathOutsideRoot_ThrowsNotFound()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/Heat.mkv", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        Should.Throw<NotFoundException>(
            () => scanner.ResolvePrimaryVideo(new LibraryPath("/elsewhere/Heat (1995)")));
    }

    [Fact]
    public void ResolvePrimaryVideo_UpperCaseExtension_NormalisesFormat()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/Heat.MKV", 512));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.ResolvePrimaryVideo(new LibraryPath("Heat (1995)"));

        // assert
        result.Format.ShouldBe(new MediaFormat("mkv"));
        result.Path.ShouldBe(new LibraryPath(Path.Combine(Root, "Heat (1995)", "Heat.MKV")));
    }

    [Fact]
    public void ResolvePrimaryVideo_SizeTie_SelectsOrdinalLowestFileName()
    {
        // arrange
        var fileSystem = FileSystemWith(
            ($"{Root}/Heat (1995)/b.mkv", 4096),
            ($"{Root}/Heat (1995)/A.mp4", 4096));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var result = scanner.ResolvePrimaryVideo(new LibraryPath("Heat (1995)"));

        // assert
        result.Path.ShouldBe(new LibraryPath(Path.Combine(Root, "Heat (1995)", "A.mp4")));
    }

    [Fact]
    public void ResolvePrimaryVideo_VideoOnlyInSubfolder_ThrowsInvalidTransition()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/Featurettes/extra.mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<InvalidTransitionException>(
            () => scanner.ResolvePrimaryVideo(new LibraryPath("Heat (1995)")));

        // assert
        exception.Message.ShouldContain("Heat (1995)");
    }

    [Fact]
    public void ResolvePrimaryVideo_MissingFolder_ThrowsNotFound()
    {
        // arrange
        var fileSystem = FileSystemWith(($"{Root}/Heat (1995)/Heat.mkv", 1024));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        Should.Throw<NotFoundException>(
            () => scanner.ResolvePrimaryVideo(new LibraryPath("Absent (1999)")));
    }

    [Fact]
    public void Scan_UnreadableCandidateLength_PropagatesInsteadOfSelectingSibling()
    {
        // arrange
        var fileSystem = ListingFileSystem(
            ($"{Root}/Heat (1995)", [
                ("Heat.mkv", 1024L),
                ("Heat.avi", FaultedLength),
            ]));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        var exception = Should.Throw<UnauthorizedAccessException>(
            () => scanner.Scan(new LibraryPath("Heat (1995)")));

        // assert
        exception.Message.ShouldBe(FaultMessage);
    }

    [Fact]
    public void ResolvePrimaryVideo_UnreadableCandidateLength_PropagatesInsteadOfSelectingSibling()
    {
        // arrange
        var fileSystem = ListingFileSystem(
            ($"{Root}/Heat (1995)", [
                ("Heat.mkv", FaultedLength),
                ("Heat.avi", 1024L),
            ]));
        var scanner = new DirectoryMediaLibraryScanner(fileSystem, Now, Library);

        // act
        Should.Throw<UnauthorizedAccessException>(
            () => scanner.ResolvePrimaryVideo(new LibraryPath("Heat (1995)")));
    }

    private const long FaultedLength = -1;
    private const string FaultMessage = "candidate is unreadable";

    private static IFileSystem ListingFileSystem(
        params (string Directory, (string Name, long Length)[] Files)[] directories)
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.Path.IsPathRooted(Arg.Any<string>()).Returns(call => Path.IsPathRooted(call.Arg<string>()));
        fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => Path.Combine(call.ArgAt<string>(0), call.ArgAt<string>(1)));
        foreach (var (path, files) in directories)
        {
            var directory = Substitute.For<IDirectoryInfo>();
            directory.Exists.Returns(true);
            directory.Name.Returns(path.Split('/')[^1]);
            IFileInfo[] children = [.. files.Select(file => FileInfo(file.Name, file.Length))];
            directory.EnumerateFiles().Returns(children);
            fileSystem.DirectoryInfo.New(Arg.Is<string>(candidate => candidate.EndsWith(directory.Name, StringComparison.Ordinal)))
                .Returns(directory);
        }

        return fileSystem;
    }

    private static IFileInfo FileInfo(string name, long length)
    {
        var info = Substitute.For<IFileInfo>();
        info.Name.Returns(name);
        info.Extension.Returns(Path.GetExtension(name));
        if (length == FaultedLength)
        {
            info.Length.Returns(_ => throw new UnauthorizedAccessException(FaultMessage));
        }
        else
        {
            info.Length.Returns(length);
        }

        return info;
    }

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
