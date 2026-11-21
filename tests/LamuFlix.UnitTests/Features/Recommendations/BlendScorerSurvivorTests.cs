using System;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class BlendScorerSurvivorTests
{
    [Fact]
    public void Score_NullOptions_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => BlendScorer.Score(0.5, 0.0, null!));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Score_ValidTerms_ReturnsWeightedSimilarity()
    {
        // arrange
        var options = ValidOptions();

        // act
        var score = BlendScorer.Score(0.8, 0.0, options);

        // assert
        score.ShouldBe(0.8, 1e-12);
    }

    [Fact]
    public void Score_WatchedCandidate_SubtractsAlreadyWatchedPenalty()
    {
        // arrange
        var options = ValidOptions();

        // act
        var watched = BlendScorer.Score(0.5, 1.0, options);
        var unwatched = BlendScorer.Score(0.5, 0.0, options);

        // assert
        unwatched.ShouldBe(0.5, 1e-12);
        watched.ShouldBe(0.25, 1e-12);
    }

    [Fact]
    public void Score_NegativeCoefficient_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var options = ValidOptions() with { BlendSelectedWeight = -0.1 };

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() => BlendScorer.Score(0.5, 0.0, options));
    }

    [Fact]
    public void Score_NegativeAlreadyWatchedPenalty_ThrowsArgumentOutOfRangeExceptionWithMessage()
    {
        // arrange
        var options = ValidOptions() with { AlreadyWatchedPenalty = -1.0 };

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => BlendScorer.Score(0.5, 0.0, options));

        // assert
        failure.Message.ShouldContain("Blend coefficients must be non-negative and finite.");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Score_InvalidSelectedSimilarity_ThrowsArgumentOutOfRangeException(double selectedSimilarity)
    {
        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() => BlendScorer.Score(selectedSimilarity, 0.0, ValidOptions()));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Score_InvalidAlreadyWatchedInput_ThrowsArgumentOutOfRangeException(double penalty)
    {
        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() => BlendScorer.Score(0.5, penalty, ValidOptions()));
    }

    [Fact]
    public void Score_InvalidTerm_ThrowsArgumentOutOfRangeExceptionWithMessage()
    {
        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => BlendScorer.Score(1.1, 0.0, ValidOptions()));

        // assert
        failure.Message.ShouldContain("Blend terms must be finite values between 0.0 and 1.0.");
    }

    [Fact]
    public void NonNegativeOffset_NullScores_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => BlendScorer.NonNegativeOffset(null!));

        // assert
        failure.ParamName.ShouldBe("scores");
    }

    [Fact]
    public void NonNegativeOffset_AllNonNegative_ReturnsZero()
    {
        // act
        var offset = BlendScorer.NonNegativeOffset([0.2, 0.5]);

        // assert
        offset.ShouldBe(0.0);
    }

    [Fact]
    public void NonNegativeOffset_NegativeScores_ReturnsSmallestMagnitude()
    {
        // act
        var offset = BlendScorer.NonNegativeOffset([-0.4, 0.2, -0.15]);

        // assert
        offset.ShouldBe(0.4, 1e-12);
    }

    private static RecommendationOptions ValidOptions() =>
        new()
        {
            BlendSelectedWeight = 1.0,
            AlreadyWatchedPenalty = 0.25,
        };
}
