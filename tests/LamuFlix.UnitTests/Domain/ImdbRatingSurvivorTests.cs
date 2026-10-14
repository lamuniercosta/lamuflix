using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class ImdbRatingSurvivorTests
{
    [Fact]
    public void Constructor_LowerBoundZero_KeepsValue()
    {
        // act
        var rating = new ImdbRating(0.0m);

        // assert
        rating.Value.ShouldBe(0.0m);
    }

    [Fact]
    public void Constructor_UpperBoundTenPointZero_KeepsValue()
    {
        // act
        var rating = new ImdbRating(10.0m);

        // assert
        rating.Value.ShouldBe(10.0m);
    }

    [Fact]
    public void Constructor_AboveTen_ThrowsOutOfRangeWithValueAndMessage()
    {
        // act
        var thrown = Should.Throw<ArgumentOutOfRangeException>(() => new ImdbRating(10.1m));

        // assert
        thrown.ParamName.ShouldBe("value");
        thrown.ActualValue.ShouldBe(10.1m);
        thrown.Message.ShouldContain("0.0 to 10.0");
    }

    [Fact]
    public void Constructor_TooManyDecimalPlaces_ThrowsOutOfRange()
    {
        // act
        var thrown = Should.Throw<ArgumentOutOfRangeException>(() => new ImdbRating(7.25m));

        // assert
        thrown.ParamName.ShouldBe("value");
    }

    [Fact]
    public void TryCreate_ValidRating_ReturnsTrueWithResult()
    {
        // act
        var created = ImdbRating.TryCreate(7.5m, out var result);

        // assert
        created.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Value.ShouldBe(7.5m);
    }

    [Fact]
    public void TryCreate_AboveTen_ReturnsFalseWithNullResult()
    {
        // act
        var created = ImdbRating.TryCreate(10.1m, out var result);

        // assert
        created.ShouldBeFalse();
        result.ShouldBeNull();
    }

    [Fact]
    public void TryCreate_BelowZero_ReturnsFalseWithNullResult()
    {
        // act
        var created = ImdbRating.TryCreate(-0.1m, out var result);

        // assert
        created.ShouldBeFalse();
        result.ShouldBeNull();
    }
}
