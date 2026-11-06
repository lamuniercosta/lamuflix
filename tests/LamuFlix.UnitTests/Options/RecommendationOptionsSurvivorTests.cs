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
            .ShouldContain("CandidatePoolSize must be greater than or equal to Take; got 5 and 10.");
    }

    [Fact]
    public void Validate_SimilarStoryThresholdAboveOne_ReportsTheExactMessage()
    {
        // arrange
        var options = new RecommendationOptions { SimilarStoryThreshold = 2.0 };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage)
            .ShouldContain("SimilarStoryThreshold must be a finite value between -1 and 1; got 2.");
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
    public void Validate_BothBlendWeightsZero_ReportsTheExactMessage()
    {
        // arrange
        var options = new RecommendationOptions { BlendSelectedWeight = 0.0, BlendProfileWeight = 0.0 };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage)
            .ShouldContain("BlendSelectedWeight and BlendProfileWeight cannot both be 0.0.");
    }

    [Fact]
    public void Validate_ZeroLikedReviewWeight_ReportsTheExactMessage()
    {
        // arrange
        var options = new RecommendationOptions { LikedReviewWeight = 0.0 };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage)
            .ShouldContain("LikedReviewWeight must be finite and greater than 0.0; got 0.");
    }

    [Fact]
    public void Validate_AllReviewChannelsDisabled_ReportsTheExactMessage()
    {
        // arrange
        var options = new RecommendationOptions
        {
            Weights = new SimilarityWeights
            {
                Genres = 0.0,
                Cast = 0.0,
                Director = 0.0,
                ReviewPlotWeight = 0.0,
            },
        };

        // act
        var results = Validate(options);

        // assert
        results.Select(result => result.ErrorMessage).ShouldContain(
            "At least one review channel must be enabled; Weights.ReviewPlotWeight must be greater than 0.0.");
    }

    private static IReadOnlyList<ValidationResult> Validate(RecommendationOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
