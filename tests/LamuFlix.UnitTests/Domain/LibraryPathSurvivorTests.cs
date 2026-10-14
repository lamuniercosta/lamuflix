using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class LibraryPathSurvivorTests
{
    [Fact]
    public void Constructor_ValidPath_KeepsValue()
    {
        // act
        var path = new LibraryPath("C:/library/movies");

        // assert
        path.Value.ShouldBe("C:/library/movies");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:/library/../secret")]
    public void Constructor_InvalidPath_ThrowsArgumentExceptionWithParamNameAndMessage(string? value)
    {
        // act
        var thrown = Should.Throw<ArgumentException>(() => new LibraryPath(value));

        // assert
        thrown.ParamName.ShouldBe("value");
        thrown.Message.ShouldContain("must not contain '..'");
    }

    [Fact]
    public void TryCreate_ValidPath_ReturnsTrueWithResult()
    {
        // act
        var created = LibraryPath.TryCreate("C:/library/movie.mkv", out var result);

        // assert
        created.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Value.ShouldBe("C:/library/movie.mkv");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("a/../b")]
    public void TryCreate_InvalidPath_ReturnsFalseWithNullResult(string? value)
    {
        // act
        var created = LibraryPath.TryCreate(value, out var result);

        // assert
        created.ShouldBeFalse();
        result.ShouldBeNull();
    }
}
