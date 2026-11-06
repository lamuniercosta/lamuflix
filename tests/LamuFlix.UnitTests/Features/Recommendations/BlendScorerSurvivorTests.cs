using System;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class BlendScorerSurvivorTests
{
    [Fact]
    public void ScoreWithProfileContribution_NullOptions_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(
            () => BlendScorer.ScoreWithProfileContribution(0.5, 0.5, 0.0, 0.0, null!));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public void ScoreWithProfileContribution_NegativeCoefficient_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var options = ValidOptions() with { BlendSelectedWeight = -0.1 };

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => BlendScorer.ScoreWithProfileContribution(0.5, 0.5, 0.0, 0.0, options));
    }

    [Fact]
    public void ScoreWithProfileContribution_NegativeNotInterestedPenalty_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var options = ValidOptions() with { NotInterestedPenalty = -1.0 };

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => BlendScorer.ScoreWithProfileContribution(0.5, 0.5, 0.0, 0.0, options));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void ScoreWithProfileContribution_InvalidSelectedSimilarity_ThrowsArgumentOutOfRangeException(
        double selectedSimilarity)
    {
        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => BlendScorer.ScoreWithProfileContribution(selectedSimilarity, 0.5, 0.0, 0.0, ValidOptions()));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void ScoreWithProfileContribution_InvalidPenaltyInput_ThrowsArgumentOutOfRangeException(double penalty)
    {
        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => BlendScorer.ScoreWithProfileContribution(0.5, 0.5, penalty, 0.0, ValidOptions()));
    }

    [Fact]
    public void ScoreWithProfileContribution_InvalidNotInterestedInput_ThrowsArgumentOutOfRangeException()
    {
        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => BlendScorer.ScoreWithProfileContribution(0.5, 0.5, 0.0, 1.1, ValidOptions()));
    }

    [Fact]
    public void Score_InvalidNotInterestedInput_ThrowsArgumentOutOfRangeException()
    {
        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => BlendScorer.Score(0.5, 0.5, 0.0, 1.1, ValidOptions()));
    }

    [Fact]
    public void Score_NegativeNotInterestedPenalty_ThrowsArgumentOutOfRangeExceptionWithMessage()
    {
        // arrange
        var options = ValidOptions() with { NotInterestedPenalty = -1.0 };

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(
            () => BlendScorer.Score(0.5, 0.5, 0.0, 0.0, options));

        // assert
        failure.Message.ShouldContain("Blend coefficients must be non-negative and finite.");
    }

    [Fact]
    public void Score_NonFiniteCalculatedResult_ThrowsInvalidOperationExceptionWithMessage()
    {
        // arrange
        var options = ValidOptions() with { BlendSelectedWeight = double.MaxValue, BlendProfileWeight = double.MaxValue };

        // act
        var failure = Should.Throw<InvalidOperationException>(
            () => BlendScorer.Score(1.0, 1.0, 0.0, 0.0, options));

        // assert
        failure.Message.ShouldBe("The calculated blend score must be finite.");
    }

    [Fact]
    public void Score_InvalidTerm_ThrowsArgumentOutOfRangeExceptionWithMessage()
    {
        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(
            () => BlendScorer.Score(1.1, 0.5, 0.0, 0.0, ValidOptions()));

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

    private static RecommendationOptions ValidOptions() =>
        new()
        {
            BlendSelectedWeight = 1.0,
            BlendProfileWeight = 1.0,
            AlreadyWatchedPenalty = 0.25,
            NotInterestedPenalty = 1.0,
        };
}
