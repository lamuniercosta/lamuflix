using System;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityScorerInvariantPropertyTests
{
    private const double SymmetryTolerance = 1e-12;

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
        var result = SimilarityScorer.Score(movie, movie, weights, genreWeights);

        // assert
        result.Score.ShouldBe(1.0);
    }

    [Property(MaxTest = 1000, Replay = "342002,200009", Arbitrary = new[] { typeof(MovieFeaturesArbitraries) })]
    [Trait("Category", "Property")]
    public void Score_SwappedArguments_AreSymmetricWithinTolerance(
        MovieFeaturesPair pair,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        // arrange
        var first = pair.First;
        var second = pair.Second;

        // act
        var forward = SimilarityScorer.Score(first, second, weights, genreWeights).Score;
        var reverse = SimilarityScorer.Score(second, first, weights, genreWeights).Score;

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
        var first = pair.First;
        var second = pair.Second;

        // act
        var result = SimilarityScorer.Score(first, second, weights, genreWeights);

        // assert
        double.IsFinite(result.Score).ShouldBeTrue();
        result.Score.ShouldBeGreaterThanOrEqualTo(0.0);
        result.Score.ShouldBeLessThanOrEqualTo(1.0);
    }

    [Property(MaxTest = 1000, Replay = "342004,400001", Arbitrary = new[] { typeof(MovieFeaturesArbitraries) })]
    [Trait("Category", "Property")]
    public void Score_PositiveScore_EmitsNonEmptyReasons(
        MovieFeaturesPair pair,
        SimilarityWeights weights,
        GenreWeights genreWeights)
    {
        // arrange
        var first = pair.First;
        var second = pair.Second;

        // act
        var result = SimilarityScorer.Score(first, second, weights, genreWeights);

        // assert
        if (result.Score > 0.0)
        {
            result.Reasons.ShouldNotBeEmpty();
        }
    }
}
