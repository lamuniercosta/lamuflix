using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class RuntimeSurvivorTests
{
    [Fact]
    public void Constructor_OneMinute_KeepsMinutes()
    {
        // act
        var runtime = new Runtime(1);

        // assert
        runtime.Minutes.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_NonPositiveMinutes_ThrowsOutOfRangeWithValueAndMessage(int minutes)
    {
        // act
        var thrown = Should.Throw<ArgumentOutOfRangeException>(() => new Runtime(minutes));

        // assert
        thrown.ParamName.ShouldBe("minutes");
        thrown.ActualValue.ShouldBe(minutes);
        thrown.Message.ShouldContain("greater than zero");
    }

    [Fact]
    public void TryCreate_OneMinute_ReturnsTrueWithResult()
    {
        // act
        var created = Runtime.TryCreate(1, out var result);

        // assert
        created.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Minutes.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void TryCreate_NonPositiveMinutes_ReturnsFalseWithNullResult(int minutes)
    {
        // act
        var created = Runtime.TryCreate(minutes, out var result);

        // assert
        created.ShouldBeFalse();
        result.ShouldBeNull();
    }
}
