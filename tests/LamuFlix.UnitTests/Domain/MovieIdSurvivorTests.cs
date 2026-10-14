using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieIdSurvivorTests
{
    [Fact]
    public void Constructor_SmallestPositiveId_KeepsValue()
    {
        // act
        var id = new MovieId(1);

        // assert
        id.Value.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveId_ThrowsOutOfRangeWithValueAndMessage(int value)
    {
        // act
        var thrown = Should.Throw<ArgumentOutOfRangeException>(() => new MovieId(value));

        // assert
        thrown.ParamName.ShouldBe("value");
        thrown.ActualValue.ShouldBe(value);
        thrown.Message.ShouldContain("greater than zero");
    }

    [Fact]
    public void TryCreate_PositiveId_ReturnsTrueWithResult()
    {
        // act
        var created = MovieId.TryCreate(42, out var result);

        // assert
        created.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Value.ShouldBe(42);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-7)]
    public void TryCreate_NonPositiveId_ReturnsFalseWithNullResult(int value)
    {
        // act
        var created = MovieId.TryCreate(value, out var result);

        // assert
        created.ShouldBeFalse();
        result.ShouldBeNull();
    }
}
