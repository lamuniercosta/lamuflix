using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityScorerInvariantPropertyTests
{
    private const double SymmetryTolerance = 1e-12;
    private const double ReconciliationTolerance = 1e-12;

    private static readonly string[] CriterionOrder =
        ["Collection", "Director", "Themes", "Plot", "Cast", "Genre"];

    private static KeywordCounts Counts => new(
        100,
        new Dictionary<string, long>
        {
            ["alpha"] = 10,
            ["beta"] = 20,
            ["gamma"] = 40,
            ["delta"] = 2,
            ["anchor-theme"] = 50,
        });

    [Property(MaxTest = 1000, Replay = "342005,500003", Arbitrary = new[] { typeof(MovieFeaturesArbitraries) })]
    [Trait("Category", "Property")]
    public bool Score_KeywordAndCollectionTerms_PreserveSymmetryWhenOnlySignalsDiffer(
        MovieFeaturesPair pair,
        GenreWeights genreWeights,
        double themesWeight,
        double collectionWeight)
    {
        // arrange
        var options = Options(SignalWeights(themesWeight, collectionWeight));

        // act
        var forward = SimilarityScorer.Score(pair.First, pair.Second, options, genreWeights, Counts).Score;
        var reverse = SimilarityScorer.Score(pair.Second, pair.First, options, genreWeights, Counts).Score;

        // assert
        return double.IsFinite(forward)
            && forward is >= 0.0 and <= 1.0
            && Math.Abs(forward - reverse) <= SymmetryTolerance;
    }

    [Property(MaxTest = 1000, Replay = "342001,100003", Arbitrary = new[] { typeof(MovieFeaturesArbitraries) })]
    [Trait("Category", "Property")]
    public void Score_ScoringProfileAgainstItself_IsExactlyOne(
        SelfSimilarityProfile profile,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        // arrange
        var movie = profile.Value;

        // act
        var result = SimilarityScorer.Score(movie, movie, Options(weights), genreWeights, Counts);

        // assert
        result.Score.ShouldBe(1.0, SymmetryTolerance);
    }

    [Property(MaxTest = 1000, Replay = "342002,200009", Arbitrary = new[] { typeof(MovieFeaturesArbitraries) })]
    [Trait("Category", "Property")]
    public void Score_SwappedArguments_AreSymmetricWithinTolerance(
        MovieFeaturesPair pair,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        // arrange
        var options = Options(weights);

        // act
        var forward = SimilarityScorer.Score(pair.First, pair.Second, options, genreWeights, Counts).Score;
        var reverse = SimilarityScorer.Score(pair.Second, pair.First, options, genreWeights, Counts).Score;

        // assert
        Math.Abs(forward - reverse).ShouldBeLessThanOrEqualTo(SymmetryTolerance);
    }

    [Property(MaxTest = 1000, Replay = "342003,300011", Arbitrary = new[] { typeof(MovieFeaturesArbitraries) })]
    [Trait("Category", "Property")]
    public void Score_GeneratedPair_IsFiniteAndWithinUnitInterval(
        MovieFeaturesPair pair,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        // arrange
        var options = Options(weights);

        // act
        var result = SimilarityScorer.Score(pair.First, pair.Second, options, genreWeights, Counts);

        // assert
        double.IsFinite(result.Score).ShouldBeTrue();
        result.Score.ShouldBeGreaterThanOrEqualTo(0.0);
        result.Score.ShouldBeLessThanOrEqualTo(1.0);
    }

    [Property(MaxTest = 1000, Replay = "342004,400001", Arbitrary = new[] { typeof(MovieFeaturesArbitraries) })]
    [Trait("Category", "Property")]
    public void Score_GeneratedPair_BreakdownReconcilesToScore(
        MovieFeaturesPair pair,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        // arrange
        var options = Options(weights);

        // act
        var result = SimilarityScorer.Score(pair.First, pair.Second, options, genreWeights, Counts);

        // assert
        result.Breakdown.Select(entry => entry.Name).ShouldBe(CriterionOrder);
        double.IsFinite(result.Score).ShouldBeTrue();
        Math.Abs(result.Score - result.Breakdown.Sum(entry => entry.Contribution))
            .ShouldBeLessThanOrEqualTo(ReconciliationTolerance);
    }

    [Property(MaxTest = 1000, Replay = "342006,600013", Arbitrary = new[] { typeof(MovieFeaturesArbitraries) })]
    [Trait("Category", "Property")]
    public void Score_GeneratedPair_ReasonsUseTheContractTexts(
        MovieFeaturesPair pair,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        // arrange
        var options = Options(weights);
        string[] contractPrefixes =
        [
            "Same collection: ",
            "Same director: ",
            "Shared themes: ",
            "Similar story",
            "Shares cast: ",
            "Same genres: ",
        ];

        // act
        var result = SimilarityScorer.Score(pair.First, pair.Second, options, genreWeights, Counts);

        // assert
        result.Reasons.Count.ShouldBeLessThanOrEqualTo(CriterionOrder.Length);
        result.Reasons.ShouldAllBe(reason => reason.Kind == ReasonKind.Structural);
        result.Reasons.Select(reason => reason.Text).ShouldAllBe(text =>
            contractPrefixes.Any(prefix => text.StartsWith(prefix, StringComparison.Ordinal)));
    }

    private static RecommendationOptions Options(SimilarityWeights weights) =>
        new() { Weights = weights };

    private static SimilarityWeights SignalWeights(double themesWeight, double collectionWeight)
    {
        var themes = Normalize(themesWeight);
        var collection = Normalize(collectionWeight);

        return new SimilarityWeights
        {
            Director = 0.0,
            Themes = themes,
            Plot = 0.0,
            Cast = 0.0,
            Genres = 0.0,
            Collection = themes + collection > 0.0 ? collection : 1.0,
        };
    }

    private static double Normalize(double value) =>
        double.IsFinite(value) ? Math.Min(Math.Abs(value), 1e6) : 1.0;
}
