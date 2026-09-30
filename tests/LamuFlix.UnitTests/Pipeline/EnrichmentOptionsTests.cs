using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Pipeline;

public sealed class EnrichmentOptionsTests
{
    [Fact]
    public void TryValidateObject_MaxAttemptsZero_IsInvalid()
    {
        // arrange
        var options = new EnrichmentOptions { MaxAttempts = 0 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // act
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // assert
        valid.ShouldBeFalse();
        results.Count.ShouldBe(1);
        results[0].MemberNames.ShouldContain(nameof(EnrichmentOptions.MaxAttempts));
    }

    [Fact]
    public void TryValidateObject_MaxAttemptsOne_IsValid()
    {
        // arrange
        var options = new EnrichmentOptions { MaxAttempts = 1 };
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // act
        var valid = Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        // assert
        valid.ShouldBeTrue();
        results.ShouldBeEmpty();
    }
}
