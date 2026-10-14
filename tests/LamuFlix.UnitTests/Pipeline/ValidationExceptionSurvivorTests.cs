using System;
using System.Collections.Generic;
using LamuFlix.Core.Pipeline;

namespace LamuFlix.UnitTests.Pipeline;

public sealed class ValidationExceptionSurvivorTests
{
    [Fact]
    public void Constructor_Errors_ExposesErrorsAndFixedMessage()
    {
        // arrange
        var errors = new Dictionary<string, string[]>
        {
            ["Title"] = ["Title is required."],
        };

        // act
        var thrown = new ValidationException(errors);

        // assert
        thrown.Message.ShouldBe("Validation failed.");
        thrown.Errors.ShouldBeSameAs(errors);
    }

    [Fact]
    public void Constructor_NullErrors_ThrowsArgumentNullExceptionWithParamName()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var thrown = Should.Throw<ArgumentNullException>(() => new ValidationException(null!));

        // assert
        thrown.ParamName.ShouldBe("errors");
    }
}
