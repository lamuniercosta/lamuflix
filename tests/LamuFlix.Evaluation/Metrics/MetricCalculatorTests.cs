using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Evaluation.RealLibrary;

namespace LamuFlix.Evaluation.Metrics;

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

    [Fact]
    public void Accumulate_CountsHitsAtOneThreeAndTenFromTheOneSharedPath()
    {
        // act
        var accumulation = MetricCalculator.Accumulate(
        [
            Observation(target: 1, 1, 2, 3, 4),
            Observation(target: 2, 1, 2, 3, 4),
            Observation(target: 3, 1, 2, 3, 4),
            Observation(target: 4, 1, 2, 3, 4),
            Observation(target: 11, 1, 2, 3, 4),
        ]);

        // assert
        accumulation.ShouldSatisfyAllConditions(
            value => value.AttemptedCount.ShouldBe(5),
            value => value.HitAtFirst.ShouldBe(1),
            value => value.HitAtNear.ShouldBe(3),
            value => value.HitAtCutoff.ShouldBe(4),
            value => value.ReciprocalRankSum.ShouldBe(1.0 + 0.5 + (1.0 / 3.0) + 0.25));
    }

    [Fact]
    public void Accumulate_KeepsHitsBeyondTheReciprocalRankCutoffInTheHitCounts()
    {
        // act
        var accumulation = MetricCalculator.Accumulate([Observation(target: 5, 1, 2, 3, 4, 5)], reciprocalRankCutoff: 3);

        // assert
        accumulation.ShouldSatisfyAllConditions(
            value => value.HitAtNear.ShouldBe(0),
            value => value.HitAtCutoff.ShouldBe(1),
            value => value.ReciprocalRankSum.ShouldBe(0.0));
    }

    [Fact]
    public void Accumulate_AbsentTargetCountsAsAttemptedWithNoHits()
    {
        // act
        var accumulation = MetricCalculator.Accumulate([Observation(target: 9, 1, 2)]);

        // assert
        accumulation.ShouldSatisfyAllConditions(
            value => value.AttemptedCount.ShouldBe(1),
            value => value.HitAtFirst.ShouldBe(0),
            value => value.HitAtNear.ShouldBe(0),
            value => value.HitAtCutoff.ShouldBe(0),
            value => value.ReciprocalRankSum.ShouldBe(0.0));
    }

    [Fact]
    public void Accumulate_RejectsANonPositiveReciprocalRankCutoff()
    {
        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => MetricCalculator.Accumulate([Observation(target: 1, 1)], reciprocalRankCutoff: 0));
    }

    [Fact]
    public void Compute_MatchesTheAccumulationItShares()
    {
        // arrange
        IReadOnlyList<EvaluationObservation> observations =
        [
            Observation(target: 1, 1, 2),
            Observation(target: 4, 1, 2, 3, 4),
        ];

        // act
        var metrics = MetricCalculator.Compute(observations);
        var accumulation = MetricCalculator.Accumulate(observations);

        // assert
        metrics.ShouldSatisfyAllConditions(
            value => value.ObservationCount.ShouldBe(accumulation.AttemptedCount),
            value => value.HitCount.ShouldBe(accumulation.HitAtCutoff),
            value => value.ReciprocalRankSum.ShouldBe(accumulation.ReciprocalRankSum));
    }

    [Fact]
    public void RealLibraryCompute_ZeroObservations_YieldsNullMetricsInsteadOfSyntheticZero()
    {
        // act
        var metrics = RealLibraryMetricCalculator.Compute([], reciprocalRankCutoff: MetricCalculator.Cutoff);

        // assert
        metrics.ShouldSatisfyAllConditions(
            value => value.AttemptedCount.ShouldBe(0),
            value => value.HitRateAt1.ShouldBeNull(),
            value => value.HitRateAt3.ShouldBeNull(),
            value => value.HitRateAt10.ShouldBeNull(),
            value => value.Mrr.ShouldBeNull());
    }

    [Fact]
    public void RealLibraryCompute_PopulatedCohort_DividesByAttemptedSeeds()
    {
        // act
        var metrics = RealLibraryMetricCalculator.Compute(
            [
                Observation(target: 1, 1, 2, 3),
                Observation(target: 2, 1, 2, 3),
                Observation(target: 9, 1, 2, 3),
            ],
            reciprocalRankCutoff: MetricCalculator.Cutoff);

        // assert
        metrics.ShouldSatisfyAllConditions(
            value => value.AttemptedCount.ShouldBe(3),
            value => value.HitRateAt1.ShouldBe(1.0 / 3.0),
            value => value.HitRateAt3.ShouldBe(2.0 / 3.0),
            value => value.HitRateAt10.ShouldBe(2.0 / 3.0),
            value => value.Mrr.ShouldBe(0.5));
    }

    [Fact]
    public void RealLibraryCompute_CohortOfOnlyAbsentTargets_ReportsZeroNotUnavailable()
    {
        // act
        var metrics = RealLibraryMetricCalculator.Compute(
            [Observation(target: 9, 1, 2), Observation(target: 8, 1, 2)],
            reciprocalRankCutoff: MetricCalculator.Cutoff);

        // assert
        metrics.ShouldSatisfyAllConditions(
            value => value.AttemptedCount.ShouldBe(2),
            value => value.HitRateAt1.ShouldBe(0.0),
            value => value.HitRateAt3.ShouldBe(0.0),
            value => value.Mrr.ShouldBe(0.0));
    }

    [Fact]
    public void RealLibraryCompute_RejectsANonPositiveReciprocalRankCutoff()
    {
        // act / assert
        Should.Throw<ArgumentOutOfRangeException>(
            () => RealLibraryMetricCalculator.Compute([Observation(target: 1, 1)], reciprocalRankCutoff: 0));
    }

    private static EvaluationObservation Observation(int target, params int[] ranking) =>
        new(new MovieId(target), [.. ranking.Select(id => new MovieId(id))]);
}
