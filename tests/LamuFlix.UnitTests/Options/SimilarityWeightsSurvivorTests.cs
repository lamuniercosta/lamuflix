using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class SimilarityWeightsSurvivorTests
{
    [Fact]
    public void Defaults_CollectionWeight_IsThree()
    {
        // arrange
        var weights = new SimilarityWeights();

        // act
        var collection = weights.Collection;

        // assert
        collection.ShouldBe(3.0);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(2.5)]
    [InlineData(double.MaxValue)]
    public void Validate_NonNegativeWeight_IsValid(double value)
    {
        // arrange
        var weights = new SimilarityWeights { Collection = value };

        // act
        var results = Validate(weights);

        // assert
        results.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Validate_InvalidWeight_ReportsCollection(double value)
    {
        // arrange
        var weights = new SimilarityWeights { Collection = value };

        // act
        var results = Validate(weights);

        // assert
        results.Select(result => result.MemberNames).SelectMany(names => names)
            .ShouldContain(nameof(SimilarityWeights.Collection));
    }

    private static IReadOnlyList<ValidationResult> Validate(SimilarityWeights options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
