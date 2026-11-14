using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class LibraryPathLibraryRootTests
{
    private const string Root = "C:/library";

    [Theory]
    [InlineData(".")]
    [InlineData("./")]
    [InlineData("././")]
    [InlineData("./.")]
    [InlineData("C:/library")]
    [InlineData("C:/library/")]
    [InlineData("C:\\library")]
    [InlineData("C:/library/.")]
    [InlineData("c:/LIBRARY")]
    public void ResolvesToLibraryRoot_PathNamingTheRoot_ReturnsTrue(string value)
    {
        // act
        var resolves = LibraryPath.ResolvesToLibraryRoot(value, Root);

        // assert
        resolves.ShouldBeTrue();
    }

    [Theory]
    [InlineData("Heat (1995)")]
    [InlineData("./Heat (1995)")]
    [InlineData("C:/library/Heat (1995)")]
    [InlineData("C:/library/Heat (1995)/")]
    [InlineData("Collections/Heat (1995)")]
    public void ResolvesToLibraryRoot_FolderInsideTheRoot_ReturnsFalse(string value)
    {
        // act
        var resolves = LibraryPath.ResolvesToLibraryRoot(value, Root);

        // assert
        resolves.ShouldBeFalse();
    }

    [Theory]
    [InlineData("D:/elsewhere")]
    [InlineData("/mnt/elsewhere")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:/library/../secret")]
    public void ResolvesToLibraryRoot_UnimportableOrForeignPath_ReturnsFalse(string value)
    {
        // act
        var resolves = LibraryPath.ResolvesToLibraryRoot(value, Root);

        // assert
        resolves.ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolvesToLibraryRoot_NoConfiguredRoot_ReturnsFalse(string? rootPath)
    {
        // act
        var resolves = LibraryPath.ResolvesToLibraryRoot(".", rootPath);

        // assert
        resolves.ShouldBeFalse();
    }

    [Fact]
    public void IsRootIdentity_CanonicalDot_ReturnsTrue()
    {
        // act
        var isRoot = LibraryPath.IsRootIdentity(".");

        // assert
        isRoot.ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("..")]
    [InlineData("Heat")]
    [InlineData("./Heat")]
    public void IsRootIdentity_AnythingElse_ReturnsFalse(string? canonical)
    {
        // act
        var isRoot = LibraryPath.IsRootIdentity(canonical);

        // assert
        isRoot.ShouldBeFalse();
    }
}
