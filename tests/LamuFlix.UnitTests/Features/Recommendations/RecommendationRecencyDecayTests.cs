using System;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class RecommendationRecencyDecayTests
{
    private const double Tolerance = 1e-12;

    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AgeInDays_PlayedInTheFuture_ClampsToZero()
    {
        // arrange
        var playedAt = Now.AddDays(3);

        // act
        var age = RecommendationRecencyDecay.AgeInDays(Now, playedAt);

        // assert
        age.ShouldBe(0.0);
    }

    [Fact]
    public void AgeInDays_ThirtyDaysOld_ReturnsThirtyDays()
    {
        // arrange
        var playedAt = Now.AddDays(-30);

        // act
        var age = RecommendationRecencyDecay.AgeInDays(Now, playedAt);

        // assert
        age.ShouldBe(30.0, Tolerance);
    }

    [Fact]
    public void RelativeWeight_AtTheReferenceAge_IsExactlyOne()
    {
        // arrange
        var decay = new RecommendationRecencyDecay(new FixedTimeProvider(Now), new RecommendationOptions());

        // act
        var weight = decay.RelativeWeight(12.0, 12.0);

        // assert
        weight.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void RelativeWeight_MatchesTheExistingExponentialLambda()
    {
        // arrange
        var options = new RecommendationOptions { RecencyDecayLambda = 0.02 };
        var decay = new RecommendationRecencyDecay(new FixedTimeProvider(Now), options);
        var expected = Math.Exp(-0.02 * 7.5);

        // act
        var weight = decay.RelativeWeight(10.0, 2.5);

        // assert
        weight.ShouldBe(expected, Tolerance);
    }

    [Fact]
    public void Weight_OlderThanTheReference_AppliesTheConfiguredFloor()
    {
        // arrange
        var decay = new RecommendationRecencyDecay(new FixedTimeProvider(Now), new RecommendationOptions());
        var reference = Now.AddDays(-1);
        var older = Now.AddDays(-400);

        // act
        var weight = decay.Weight(Now, older, reference);

        // assert
        weight.ShouldBe(0.25, Tolerance);
    }

    [Fact]
    public void Weight_NewerThanTheReference_IsNotCappedAtOne()
    {
        // arrange
        var decay = new RecommendationRecencyDecay(new FixedTimeProvider(Now), new RecommendationOptions());
        var reference = Now.AddDays(-10);
        var newer = Now.AddDays(-2);

        // act
        var weight = decay.Weight(Now, newer, reference);

        // assert
        weight.ShouldBeGreaterThan(1.0);
    }

    [Fact]
    public void Weight_PlaybackAboveTheFloor_ReturnsTheExponentialValue()
    {
        // arrange
        var options = new RecommendationOptions
        {
            RecencyDecayLambda = Math.Log(2.0) / 30.0,
            RecencyFloor = 0.25,
        };
        var decay = new RecommendationRecencyDecay(new FixedTimeProvider(Now), options);
        var reference = Now.AddDays(-30);
        var seed = Now.AddDays(-60);
        var expected = Math.Max(0.25, Math.Exp(-(Math.Log(2.0) / 30.0) * 30.0));

        // act
        var weight = decay.Weight(Now, seed, reference);

        // assert
        weight.ShouldBe(expected, Tolerance);
    }

    [Fact]
    public void Weight_EqualInstantLaunches_ReturnTheUnscaledWeight()
    {
        // arrange
        var decay = new RecommendationRecencyDecay(new FixedTimeProvider(Now), new RecommendationOptions());

        // act
        var weight = decay.Weight(Now, Now, Now);

        // assert
        weight.ShouldBe(RecommendationRecencyDecay.UnscaledWeight, Tolerance);
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed - the null clock is the case under test.
        Should.Throw<ArgumentNullException>(() => new RecommendationRecencyDecay(null!, new RecommendationOptions()));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed - the null options is the case under test.
        Should.Throw<ArgumentNullException>(() => new RecommendationRecencyDecay(new FixedTimeProvider(Now), null!));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    public void Constructor_NonFiniteOrNonPositiveLambda_ThrowsArgumentOutOfRangeException(double lambda)
    {
        // arrange
        var options = new RecommendationOptions { RecencyDecayLambda = lambda };

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => new RecommendationRecencyDecay(new FixedTimeProvider(Now), options));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(0.0)]
    [InlineData(-0.25)]
    [InlineData(1.5)]
    public void Constructor_RecencyFloorOutsideTheUnitInterval_ThrowsArgumentOutOfRangeException(double floor)
    {
        // arrange
        var options = new RecommendationOptions { RecencyFloor = floor };

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => new RecommendationRecencyDecay(new FixedTimeProvider(Now), options));
    }

    [Theory]
    [InlineData(double.NaN, 0.0)]
    [InlineData(-1.0, 0.0)]
    [InlineData(0.0, double.PositiveInfinity)]
    [InlineData(0.0, -1.0)]
    public void RelativeWeight_NonFiniteOrNegativeAge_ThrowsArgumentOutOfRangeException(
        double age,
        double referenceAge)
    {
        // arrange
        var decay = new RecommendationRecencyDecay(new FixedTimeProvider(Now), new RecommendationOptions());

        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(() => decay.RelativeWeight(age, referenceAge));
    }

    [Fact]
    public void UtcNow_ReadsTheInjectedClock()
    {
        // arrange
        var decay = new RecommendationRecencyDecay(new FixedTimeProvider(Now), new RecommendationOptions());

        // act
        var now = decay.UtcNow;

        // assert
        now.ShouldBe(Now);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
