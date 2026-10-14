using System;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class EnrichmentOptionsValidatorSurvivorTests
{
    [Fact]
    public void Validate_NullOptions_ThrowsArgumentNullExceptionNamingOptions()
    {
        // arrange
        var validator = new EnrichmentOptionsValidator();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var thrown = Should.Throw<ArgumentNullException>(() => validator.Validate(null, null!));

        // assert
        thrown.ParamName.ShouldBe("options");
    }
}
