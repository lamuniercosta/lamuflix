using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class ImdbIdSurvivorTests
{
    [Theory]
    [InlineData("tt1234567")]
    [InlineData("tt12345678")]
    public void Constructor_SevenOrEightDigits_KeepsValue(string value)
    {
        // act
        var id = new ImdbId(value);

        // assert
        id.Value.ShouldBe(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("tt123456")]
    [InlineData("tt123456789")]
    [InlineData("TT1234567")]
    public void Constructor_InvalidValue_ThrowsArgumentExceptionWithParamNameAndMessage(string? value)
    {
        // act
        var thrown = Should.Throw<ArgumentException>(() => new ImdbId(value));

        // assert
        thrown.ParamName.ShouldBe("value");
        thrown.Message.ShouldContain("tt followed by 7 or 8 digits");
    }

    [Fact]
    public void TryCreate_ValidValue_ReturnsTrueWithResult()
    {
        // act
        var created = ImdbId.TryCreate("tt7654321", out var result);

        // assert
        created.ShouldBeTrue();
        result.ShouldNotBeNull();
        result.Value.ShouldBe("tt7654321");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("tt123456")]
    public void TryCreate_InvalidValue_ReturnsFalseWithNullResult(string? value)
    {
        // act
        var created = ImdbId.TryCreate(value, out var result);

        // assert
        created.ShouldBeFalse();
        result.ShouldBeNull();
    }
}
