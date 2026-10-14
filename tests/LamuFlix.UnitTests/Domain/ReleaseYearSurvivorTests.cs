using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class ReleaseYearSurvivorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(1888)]
    [InlineData(2031)]
    public void Constructor_BoundaryYears_KeepsValue(int value)
    {
        // act
        var releaseYear = new ReleaseYear(value, Now);

        // assert
        releaseYear.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData(1887)]
    [InlineData(2032)]
    public void Constructor_YearOutsideRange_ThrowsOutOfRangeWithValueAndMessage(int value)
    {
        // act
        var thrown = Should.Throw<ArgumentOutOfRangeException>(() => new ReleaseYear(value, Now));

        // assert
        thrown.ParamName.ShouldBe("value");
        thrown.ActualValue.ShouldBe(value);
        thrown.Message.ShouldContain("1888");
    }

    [Theory]
    [InlineData(1888)]
    [InlineData(2031)]
    public void TryCreate_BoundaryYears_ReturnsTrueWithResult(int value)
    {
        // act
        var created = ReleaseYear.TryCreate(value, Now, out var result);

        // assert
        created.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData(1887)]
    [InlineData(2032)]
    public void TryCreate_YearOutsideRange_ReturnsFalseWithNullResult(int value)
    {
        // act
        var created = ReleaseYear.TryCreate(value, Now, out var result);

        // assert
        created.ShouldBeFalse();
        result.ShouldBeNull();
    }
}
