using System;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class BlendScorerTests
{
    private const double Tolerance = 1e-12;

    [Fact]
    public void Score_DefaultFormula_AppliesEachCoefficientAndPenalty()
    {
        // arrange
        var options = Options(a: 1.0, b: 1.0, c: 0.25, d: 1.0);

        // act
        var score = BlendScorer.Score(0.8, 0.5, 1.0, 0.0, options);

        // assert
        score.ShouldBe(1.05, Tolerance);
    }

    [Fact]
    public void Score_WatchedPenalty_IsBinary()
    {
        // arrange
        var options = Options(a: 1.0, b: 1.0, c: 0.25, d: 0.0);

        // act
        var watched = BlendScorer.Score(0.5, 0.5, 1.0, 0.0, options);
        var unwatched = BlendScorer.Score(0.5, 0.5, 0.0, 0.0, options);

        // assert
        watched.ShouldBe(0.75, Tolerance);
        unwatched.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void Score_NotInterestedPenalty_IsExercisedInPureFormula()
    {
        // arrange
        var options = Options(a: 1.0, b: 1.0, c: 0.0, d: 1.0);

        // act
        var score = BlendScorer.Score(0.5, 0.5, 0.0, 1.0, options);

        // assert
        score.ShouldBe(0.0, Tolerance);
    }

    [Fact]
    public void Score_NegativeRawResult_IsReturnedWithoutClamping()
    {
        // arrange
        var options = Options(a: 0.5, b: 0.0, c: 1.0, d: 1.0);

        // act
        var score = BlendScorer.Score(0.2, 0.0, 0.0, 1.0, options);

        // assert
        score.ShouldBe(-0.9, Tolerance);
    }

    [Fact]
    public void Score_AboveOneRawResult_IsReturnedWithoutRenormalization()
    {
        // arrange
        var options = Options(a: 1.0, b: 1.0, c: 0.0, d: 0.0);

        // act
        var score = BlendScorer.Score(1.0, 1.0, 0.0, 0.0, options);

        // assert
        score.ShouldBe(2.0, Tolerance);
    }

    [Fact]
    public void Score_ZeroProfileWeight_IgnoresProfileSimilarity()
    {
        // arrange
        var options = Options(a: 1.0, b: 0.0, c: 0.0, d: 0.0);

        // act
        var low = BlendScorer.Score(0.4, 0.0, 0.0, 0.0, options);
        var high = BlendScorer.Score(0.4, 1.0, 0.0, 0.0, options);

        // assert
        low.ShouldBe(0.4, Tolerance);
        high.ShouldBe(0.4, Tolerance);
    }

    [Fact]
    public void Score_NullOptions_ThrowsArgumentNullException()
    {
        // arrange / act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => BlendScorer.Score(0.5, 0.5, 0.0, 0.0, null!));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Score_NegativeSelectedCoefficient_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var options = Options(a: -0.1, b: 1.0, c: 0.25, d: 1.0);

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() => BlendScorer.Score(0.5, 0.5, 0.0, 0.0, options));
    }

    [Fact]
    public void Score_NonFiniteProfileCoefficient_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var options = Options(a: 1.0, b: double.NaN, c: 0.25, d: 1.0);

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() => BlendScorer.Score(0.5, 0.5, 0.0, 0.0, options));
    }

    [Fact]
    public void Score_NonFiniteWatchedPenalty_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var options = Options(a: 1.0, b: 1.0, c: double.PositiveInfinity, d: 1.0);

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() => BlendScorer.Score(0.5, 0.5, 0.0, 0.0, options));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Score_InvalidSelectedSimilarity_ThrowsArgumentOutOfRangeException(double selectedSimilarity)
    {
        // arrange
        var options = Options(a: 1.0, b: 1.0, c: 0.25, d: 1.0);

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            BlendScorer.Score(selectedSimilarity, 0.5, 0.0, 0.0, options));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Score_InvalidProfileSimilarity_ThrowsArgumentOutOfRangeException(double profileSimilarity)
    {
        // arrange
        var options = Options(a: 1.0, b: 1.0, c: 0.25, d: 1.0);

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() =>
            BlendScorer.Score(0.5, profileSimilarity, 0.0, 0.0, options));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void Score_InvalidPenaltyInput_ThrowsArgumentOutOfRangeException(double penalty)
    {
        // arrange
        var options = Options(a: 1.0, b: 1.0, c: 0.25, d: 1.0);

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() => BlendScorer.Score(0.5, 0.5, penalty, 0.0, options));
    }

    [Fact]
    public void Score_NonFiniteCalculatedResult_ThrowsInvalidOperationExceptionWithoutClamping()
    {
        // arrange
        var options = Options(a: double.MaxValue, b: double.MaxValue, c: 0.0, d: 0.0);

        // act / assert
        Should.Throw<InvalidOperationException>(() => BlendScorer.Score(1.0, 1.0, 0.0, 0.0, options));
    }

    private static RecommendationOptions Options(double a, double b, double c, double d) =>
        new()
        {
            BlendSelectedWeight = a,
            BlendProfileWeight = b,
            AlreadyWatchedPenalty = c,
            NotInterestedPenalty = d,
        };
}
