using System;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Evaluation.Metrics;
using Shouldly;

namespace LamuFlix.Evaluation.Tests.Metrics;

public sealed class MetricCalculatorTests
{
    [Fact]
    public void Compute_TargetAtFirstRank_ScoresFullHitAndFullReciprocalRank()
    {
        // act
        var metrics = MetricCalculator.Compute([Observation(target: 1, 1, 2, 3)]);

        // assert
        metrics.ShouldSatisfyAllConditions(
            m => m.ObservationCount.ShouldBe(1),
            m => m.HitCount.ShouldBe(1),
            m => m.ReciprocalRankSum.ShouldBe(1.0),
            m => m.HitRateAt10.ShouldBe(1.0),
            m => m.Mrr.ShouldBe(1.0));
    }

    [Fact]
    public void Compute_TargetAtCutoffRank_IsAHitWithTenthReciprocalRank()
    {
        // act
        var metrics = MetricCalculator.Compute([Observation(target: 10, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]);

        // assert
        metrics.ShouldSatisfyAllConditions(
            m => m.HitCount.ShouldBe(1),
            m => m.HitRateAt10.ShouldBe(1.0),
            m => m.Mrr.ShouldBe(0.1));
    }

    [Fact]
    public void Compute_TargetJustBeyondCutoff_ContributesNothing()
    {
        // act
        var metrics = MetricCalculator.Compute([Observation(target: 11, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)]);

        // assert
        metrics.ShouldSatisfyAllConditions(
            m => m.ObservationCount.ShouldBe(1),
            m => m.HitCount.ShouldBe(0),
            m => m.ReciprocalRankSum.ShouldBe(0.0),
            m => m.HitRateAt10.ShouldBe(0.0),
            m => m.Mrr.ShouldBe(0.0));
    }

    [Fact]
    public void Compute_AbsentTarget_ContributesNothing()
    {
        // act
        var metrics = MetricCalculator.Compute([Observation(target: 7, 1, 2, 3)]);

        // assert
        metrics.ShouldSatisfyAllConditions(
            m => m.HitCount.ShouldBe(0),
            m => m.HitRateAt10.ShouldBe(0.0),
            m => m.Mrr.ShouldBe(0.0));
    }

    [Fact]
    public void Compute_RepeatedPlays_CountEachObservationOnce()
    {
        // act
        var metrics = MetricCalculator.Compute(
        [
            Observation(target: 1, 1, 2),
            Observation(target: 1, 2, 1),
        ]);

        // assert
        metrics.ShouldSatisfyAllConditions(
            m => m.ObservationCount.ShouldBe(2),
            m => m.HitCount.ShouldBe(2),
            m => m.ReciprocalRankSum.ShouldBe(1.5),
            m => m.HitRateAt10.ShouldBe(1.0),
            m => m.Mrr.ShouldBe(0.75));
    }

    [Fact]
    public void Compute_ShortRanking_UsesThePresentRank()
    {
        // act
        var metrics = MetricCalculator.Compute([Observation(target: 2, 1, 2, 3)]);

        // assert
        metrics.ShouldSatisfyAllConditions(
            m => m.HitCount.ShouldBe(1),
            m => m.ReciprocalRankSum.ShouldBe(0.5),
            m => m.Mrr.ShouldBe(0.5));
    }

    [Fact]
    public void Compute_EmptyRanking_CountsAsMissedObservation()
    {
        // act
        var metrics = MetricCalculator.Compute([Observation(target: 1)]);

        // assert
        metrics.ShouldSatisfyAllConditions(
            m => m.ObservationCount.ShouldBe(1),
            m => m.HitCount.ShouldBe(0),
            m => m.ReciprocalRankSum.ShouldBe(0.0),
            m => m.HitRateAt10.ShouldBe(0.0),
            m => m.Mrr.ShouldBe(0.0));
    }

    [Fact]
    public void Compute_ZeroObservations_YieldsZeroObservationCountAndZeroMetrics()
    {
        // act
        var metrics = MetricCalculator.Compute([]);

        // assert
        metrics.ShouldSatisfyAllConditions(
            m => m.ObservationCount.ShouldBe(0),
            m => m.HitCount.ShouldBe(0),
            m => m.ReciprocalRankSum.ShouldBe(0.0),
            m => m.HitRateAt10.ShouldBe(0.0),
            m => m.Mrr.ShouldBe(0.0));
    }

    [Fact]
    public void Compute_MixedRanks_AveragesAcrossEveryObservation()
    {
        // act
        var metrics = MetricCalculator.Compute(
        [
            Observation(target: 1, 1, 2),
            Observation(target: 9, 1, 2),
            Observation(target: 2, 1, 2),
        ]);

        // assert
        metrics.ShouldSatisfyAllConditions(
            m => m.ObservationCount.ShouldBe(3),
            m => m.HitCount.ShouldBe(2),
            m => m.ReciprocalRankSum.ShouldBe(1.5),
            m => m.HitRateAt10.ShouldBe(2.0 / 3.0),
            m => m.Mrr.ShouldBe(0.5));
    }

    [Fact]
    public void Compute_DuplicateMovieIdsInRanking_ThrowsInsteadOfRepairing()
    {
        // act
        var failure = Should.Throw<ArgumentException>(
            () => MetricCalculator.Compute([Observation(target: 1, 1, 2, 2)]));

        // assert
        failure.Message.ShouldContain("unique");
    }

    [Fact]
    public void Compute_DefaultRanking_ThrowsInsteadOfTreatingItAsEmpty()
    {
        // arrange
        var observation = new EvaluationObservation(new MovieId(1), default(ImmutableArray<MovieId>));

        // act
        var failure = Should.Throw<ArgumentException>(() => MetricCalculator.Compute([observation]));

        // assert
        failure.Message.ShouldContain("ranking");
    }

    private static EvaluationObservation Observation(int target, params int[] ranking) =>
        new(new MovieId(target), [.. ranking.Select(id => new MovieId(id))]);
}
