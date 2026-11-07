using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class LibraryPathCanonicalTests
{
    [Theory]
    [InlineData("Heat (1995)", "Heat (1995)")]
    [InlineData("Heat (1995)/", "Heat (1995)")]
    [InlineData("./Heat (1995)", "Heat (1995)")]
    [InlineData("Heat (1995)/./Sub", "Heat (1995)/Sub")]
    [InlineData("Heat (1995)//Sub", "Heat (1995)/Sub")]
    [InlineData(".", ".")]
    [InlineData("./", ".")]
    [InlineData("././", ".")]
    public void TryCanonicalizeRelative_SegmentShapes_ProducesCanonicalIdentity(string input, string expected)
    {
        // act
        var created = LibraryPath.TryCanonicalizeRelative(input, out var canonical);

        // assert
        created.ShouldBeTrue();
        canonical.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Collections/Alien (1979)", "Collections/Alien (1979)")]
    [InlineData("Collections\\Alien (1979)", "Collections/Alien (1979)")]
    [InlineData("Collections\\Alien (1979)\\Extended", "Collections/Alien (1979)/Extended")]
    [InlineData("Mixed/Separators\\Here", "Mixed/Separators/Here")]
    public void TryCanonicalizeRelative_MixedSeparators_UsesForwardSlashes(string input, string expected)
    {
        // act
        var created = LibraryPath.TryCanonicalizeRelative(input, out var canonical);

        // assert
        created.ShouldBeTrue();
        canonical.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Heat (1995)", "Heat (1995)")]
    [InlineData("heat (1995)", "heat (1995)")]
    [InlineData("HeAt (1995)/SubFolder", "HeAt (1995)/SubFolder")]
    [InlineData("Heat/Sub/MOVIE", "Heat/Sub/MOVIE")]
    public void TryCanonicalizeRelative_SegmentCasing_IsPreserved(string input, string expected)
    {
        // act
        LibraryPath.TryCanonicalizeRelative(input, out var canonical);

        // assert
        canonical.ShouldBe(expected);
    }

    [Theory]
    [InlineData("C:/library/movies", "C:/library/movies", ".")]
    [InlineData(@"C:\library\movies", "C:/library/movies", ".")]
    [InlineData("C:/library/movies/Heat (1995)", "C:/library/movies", "Heat (1995)")]
    [InlineData("C:/library/movies/", "C:/library/movies", ".")]
    [InlineData("C:/library/movies/./Heat", "C:/library/movies", "Heat")]
    [InlineData("c:/LIBRARY/Movies/Heat", "C:/library/movies", "Heat")]
    public void TryCanonicalizeUnderRoot_WindowsFixtures_RunsOnAnyHost(string value, string root, string expected)
    {
        // act
        var created = LibraryPath.TryCanonicalizeUnderRoot(value, root, out var canonical);

        // assert
        created.ShouldBeTrue();
        canonical.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/media/library/Heat (1995)", "/media/library", "Heat (1995)")]
    [InlineData("/media/library/Heat (1995)/Extras", "/media/library", "Heat (1995)/Extras")]
    [InlineData("/media/library/", "/media/library", ".")]
    [InlineData("/media/library", "/media/library", ".")]
    public void TryCanonicalizeUnderRoot_PosixFixtures_RunsOnAnyHost(string value, string root, string expected)
    {
        // act
        var created = LibraryPath.TryCanonicalizeUnderRoot(value, root, out var canonical);

        // assert
        created.ShouldBeTrue();
        canonical.ShouldBe(expected);
    }

    [Theory]
    [InlineData(@"\\nas\media\Heat (1995)", @"\\nas\media", "Heat (1995)")]
    [InlineData("//nas/media/Heat (1995)", "//nas/media", "Heat (1995)")]
    public void TryCanonicalizeUnderRoot_UncFixtures_RunsOnAnyHost(string value, string root, string expected)
    {
        // act
        var created = LibraryPath.TryCanonicalizeUnderRoot(value, root, out var canonical);

        // assert
        created.ShouldBeTrue();
        canonical.ShouldBe(expected);
    }

    [Theory]
    [InlineData("/media/library/Heat (1995)", "/media/library2")]
    [InlineData("/media/librarySibling/Heat", "/media/library")]
    [InlineData("/media/library/../etc/passwd", "/media/library")]
    [InlineData("/etc/passwd", "/media/library")]
    [InlineData("D:/library/Heat", "C:/library")]
    [InlineData("C:/library/Heat", "D:/library")]
    public void TryCanonicalizeUnderRoot_OutsideRootOrEscaping_IsRejected(string value, string root)
    {
        // act
        var created = LibraryPath.TryCanonicalizeUnderRoot(value, root, out var canonical);

        // assert
        created.ShouldBeFalse();
        canonical.ShouldBeNull();
    }

    [Theory]
    [InlineData("/media/library/Heat (1995)", "C:/library")]
    [InlineData("C:/library/Heat (1995)", "/media/library")]
    public void TryCanonicalizeUnderRoot_ConflictingPathSyntax_IsRejected(string value, string root)
    {
        // act
        var created = LibraryPath.TryCanonicalizeUnderRoot(value, root, out var canonical);

        // assert
        created.ShouldBeFalse();
        canonical.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData("../escape")]
    [InlineData("Heat/../..")]
    [InlineData("Heat/..")]
    [InlineData("C:library")]
    [InlineData("C:")]
    [InlineData("Heat\u0000Movie")]
    public void TryCanonicalizeRelative_InvalidShapes_AreRejected(string? value)
    {
        // act
        var created = LibraryPath.TryCanonicalizeRelative(value, out var canonical);

        // assert
        created.ShouldBeFalse();
        canonical.ShouldBeNull();
    }

    [Theory]
    [InlineData("/media/library")]
    [InlineData(@"C:\library")]
    [InlineData("//nas/media")]
    public void TryCanonicalizeRelative_AbsoluteInput_IsRejected(string value)
    {
        // act
        var created = LibraryPath.TryCanonicalizeRelative(value, out var canonical);

        // assert
        created.ShouldBeFalse();
        canonical.ShouldBeNull();
    }

    [Theory]
    [InlineData("Heat (1995)")]
    [InlineData("Heat (1995)/Extras")]
    [InlineData("Heat\\Extras")]
    [InlineData("./Heat")]
    [InlineData(".")]
    public void TryCanonicalizeRelative_IsIdempotent(string value)
    {
        // arrange
        LibraryPath.TryCanonicalizeRelative(value, out var once);

        // act
        var created = LibraryPath.TryCanonicalizeRelative(once, out var twice);

        // assert
        created.ShouldBeTrue();
        twice.ShouldBe(once);
    }

    [Fact]
    public void Constructor_RedundantSeparators_StoresCanonicalIdentity()
    {
        // act
        var path = new LibraryPath("Heat (1995)//./Extras/");

        // assert
        path.Value.ShouldBe("Heat (1995)/Extras");
    }

    [Fact]
    public void Constructor_RootIdentity_StoresDot()
    {
        // act
        var path = new LibraryPath("./");

        // assert
        path.Value.ShouldBe(".");
    }

    [Theory]
    [InlineData("C:library")]
    [InlineData("Heat\u0000Movie")]
    public void Constructor_DriveRelativeOrMalformed_ThrowsWithReason(string value)
    {
        // act
        var thrown = Should.Throw<ArgumentException>(() => new LibraryPath(value));

        // assert
        thrown.ParamName.ShouldBe("value");
    }

    [Theory]
    [InlineData("/media/library", "/media")]
    [InlineData("/media", "/media/library")]
    [InlineData("C:/library", "C:/LIBRARY")]
    [InlineData(@"C:\library", "C:/library")]
    [InlineData("//nas/media", "//nas/media/")]
    public void OverlapsAtSegmentBoundary_OverlappingPrefixes_ReturnsTrue(string first, string second)
    {
        // act
        var overlaps = LibraryPath.OverlapsAtSegmentBoundary(first, second);

        // assert
        overlaps.ShouldBeTrue();
    }

    [Theory]
    [InlineData("/media/library", "/media/library2")]
    [InlineData("C:/library", "D:/library")]
    [InlineData("C:/library", "/media/library")]
    [InlineData("/media/library", "")]
    public void OverlapsAtSegmentBoundary_DistinctPrefixes_ReturnsFalse(string first, string second)
    {
        // act
        var overlaps = LibraryPath.OverlapsAtSegmentBoundary(first, second);

        // assert
        overlaps.ShouldBeFalse();
    }
}
