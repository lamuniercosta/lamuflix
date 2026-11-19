using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Options;

public sealed class SimilarityWeightsTests
{
    public static TheoryData<string, double, bool> WeightEdges()
    {
        var members = new[]
        {
            nameof(SimilarityWeights.Director),
            nameof(SimilarityWeights.Themes),
            nameof(SimilarityWeights.Plot),
            nameof(SimilarityWeights.Cast),
            nameof(SimilarityWeights.Genres),
            nameof(SimilarityWeights.Collection),
            nameof(SimilarityWeights.ReviewPlotWeight),
        };
        var data = new TheoryData<string, double, bool>();
        foreach (var member in members)
        {
            data.Add(member, 0.0, true);
            data.Add(member, -0.0001, false);
            data.Add(member, double.MaxValue, true);
            data.Add(member, double.NaN, false);
            data.Add(member, double.PositiveInfinity, false);
            data.Add(member, double.NegativeInfinity, false);
        }

        return data;
    }

    [Fact]
    public void Defaults_Weights_MatchTheAgreedValues()
    {
        // arrange
        var options = new SimilarityWeights();

        // act
        var defaults = (options.Collection, options.Director, options.Themes, options.Plot, options.Cast, options.Genres);

        // assert
        defaults.Collection.ShouldBe(3.0);
        defaults.Director.ShouldBe(3.0);
        defaults.Themes.ShouldBe(3.0);
        defaults.Plot.ShouldBe(2.0);
        defaults.Cast.ShouldBe(2.0);
        defaults.Genres.ShouldBe(2.0);
        options.ReviewPlotWeight.ShouldBe(1.0);
    }

    [Fact]
    public void TryValidateObject_Defaults_AreValid()
    {
        // arrange
        var options = new SimilarityWeights();

        // act
        var results = Validate(options);

        // assert
        results.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(WeightEdges))]
    public void TryValidateObject_Weights_AtEachEdge_MatchTheNonNegativeRange(
        string memberName,
        double value,
        bool expectedValid)
    {
        // arrange
        var options = WithWeight(new SimilarityWeights(), memberName, value);

        // act
        var results = Validate(options);

        // assert
        if (expectedValid)
        {
            results.ShouldBeEmpty();
            return;
        }

        results.SelectMany(result => result.MemberNames).ShouldContain(memberName);
    }

    private static SimilarityWeights WithWeight(SimilarityWeights options, string memberName, double value) =>
        memberName switch
        {
            nameof(SimilarityWeights.Director) => options with { Director = value },
            nameof(SimilarityWeights.Themes) => options with { Themes = value },
            nameof(SimilarityWeights.Plot) => options with { Plot = value },
            nameof(SimilarityWeights.Cast) => options with { Cast = value },
            nameof(SimilarityWeights.Genres) => options with { Genres = value },
            nameof(SimilarityWeights.Collection) => options with { Collection = value },
            nameof(SimilarityWeights.ReviewPlotWeight) => options with { ReviewPlotWeight = value },
            _ => throw new ArgumentOutOfRangeException(nameof(memberName), memberName, null),
        };

    private static IReadOnlyList<ValidationResult> Validate(SimilarityWeights options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true);
        return results;
    }
}
