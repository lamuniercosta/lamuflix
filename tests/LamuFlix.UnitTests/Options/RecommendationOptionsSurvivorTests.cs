using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class RecommendationOptionsSurvivorTests
{
    [Fact]
    public void Validate_NonFiniteRecencyDecayLambda_ReportsTheExactMessage()
    {
        // arrange
        var options = new RecommendationOptions { RecencyDecayLambda = double.NaN };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage)
            .ShouldContain("RecencyDecayLambda must be finite and greater than 0.0; got NaN.");
    }

    [Fact]
    public void Validate_CandidatePoolSizeBelowTake_ReportsTheExactMessage()
    {
        // arrange
        var options = new RecommendationOptions { CandidatePoolSize = 5, Take = 10 };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage)
            .ShouldContain("CandidatePoolSize caps the requested Take and must be greater than or equal to it; got 5 and 10.");
    }

    [Fact]
    public void Validate_PlotCeilingBelowFloor_ReportsTheExactMessage()
    {
        // arrange
        var options = new RecommendationOptions { PlotFloor = 0.5, PlotCeiling = 0.25 };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage).ShouldContain(
            "PlotCeiling must be finite, between -1.0 and 1.0, and greater than PlotFloor.");
    }

    [Fact]
    public void Validate_NegativeBlendSelectedWeight_ReportsTheExactMessage()
    {
        // arrange
        var options = new RecommendationOptions { BlendSelectedWeight = -1.0 };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage)
            .ShouldContain("BlendSelectedWeight must be finite and greater than or equal to 0.0; got -1.");
    }

    [Fact]
    public void Validate_SelectedWeightZero_ReportsTheExactMessage()
    {
        // arrange
        var options = new RecommendationOptions { BlendSelectedWeight = 0.0 };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage)
            .ShouldContain("BlendSelectedWeight cannot be 0.0.");
    }

    private static IReadOnlyList<ValidationResult> Validate(RecommendationOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
