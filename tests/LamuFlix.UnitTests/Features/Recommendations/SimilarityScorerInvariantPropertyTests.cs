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

    [Property(MaxTest = 1000, Replay = "342005,500003", Arbitrary = new[] { typeof(MovieFeaturesArbitraries) })]
    [Trait("Category", "Property")]
    public bool Score_KeywordAndCollectionTerms_PreserveSymmetryWhenOnlySignalsDiffer(
        int[] firstIds,
        int[] secondIds,
        int firstCollection,
        int secondCollection,
        double keywordWeight,
        double collectionWeight)
    {
        // arrange
        var weights = SignalWeights(keywordWeight, collectionWeight);
        var first = new MovieFeatures(keywords: firstIds.Where(id => id > 0), collectionId: Positive(firstCollection));
        var second = new MovieFeatures(keywords: secondIds.Where(id => id > 0), collectionId: Positive(secondCollection));

        // act
        var forward = SimilarityScorer.Score(first, second, weights, EmptyTable()).Score;
        var reverse = SimilarityScorer.Score(second, first, weights, EmptyTable()).Score;

        // assert
        return double.IsFinite(forward)
            && forward is >= 0.0 and <= 1.0
            && Math.Abs(forward - reverse) <= SymmetryTolerance;
    }

    private static int Positive(int value) => (int)(Math.Abs((long)value) % 5000) + 1;

    private static SimilarityWeights SignalWeights(double keywordWeight, double collectionWeight) =>
        new()
        {
            Director = 0.0,
            Writers = 0.0,
            Cast = 0.0,
            Genres = 0.0,
            YearProximity = 0.0,
            RuntimeProximity = 0.0,
            RatingBoost = 0.0,
            Keyword = Normalize(keywordWeight),
            Collection = Normalize(collectionWeight),
        };

    private static double Normalize(double value) =>
        double.IsFinite(value) ? Math.Abs(value) : 1.0;

    private static GenreWeights EmptyTable() =>
        GenreWeights.Calculate(new GenreCounts(0, new Dictionary<string, long>()));

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
