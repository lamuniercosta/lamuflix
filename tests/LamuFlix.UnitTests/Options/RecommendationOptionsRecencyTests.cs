using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LamuFlix.Core.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.UnitTests.Options;

public sealed class RecommendationOptionsRecencyTests
{
    [Fact]
    public void Default_RecencyDecayLambda_IsThirtyDayHalfLife()
    {
        // arrange
        var options = new RecommendationOptions();

        // act
        var lambda = options.RecencyDecayLambda;

        // assert
        lambda.ShouldBe(Math.Log(2.0) / 30.0);
        double.IsFinite(lambda).ShouldBeTrue();
        (lambda > 0.0).ShouldBeTrue();
    }

    [Fact]
    public void Default_MmrLambda_RemainsUnchanged()
    {
        // arrange
        var options = new RecommendationOptions();

        // act
        var mmrLambda = options.MmrLambda;

        // assert
        mmrLambda.ShouldBe(0.7);
    }

    [Theory]
    [InlineData(0.0001)]
    [InlineData(0.5)]
    [InlineData(10.0)]
    public void Validate_PositiveFiniteLambda_IsValid(double lambda)
    {
        // arrange
        var options = new RecommendationOptions { RecencyDecayLambda = lambda };

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0.0)]
    [InlineData(-0.0001)]
    public void Validate_NonFiniteOrNonPositiveLambda_ReportsRecencyDecayLambda(double lambda)
    {
        // arrange
        var options = new RecommendationOptions { RecencyDecayLambda = lambda };

        // act
        var results = Validate(options);

        // assert
        results.SelectMany(result => result.MemberNames)
            .ShouldContain(nameof(RecommendationOptions.RecencyDecayLambda));
    }

    private static IReadOnlyList<ValidationResult> Validate(RecommendationOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
