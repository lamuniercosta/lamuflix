using System;
using System.Collections.Immutable;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Evaluation.Metrics;

namespace LamuFlix.UnitTests.Features.Recommendations;

/// <summary>
/// DEV-356 T017 — FsCheck invariants for the pure HR@10/MRR metrics (FR-005,
/// Q5, Q8): finite <c>[0,1]</c> bounds, count ordering, observation-order
/// independence, appended-observation monotonicity, and cutoff-prefix
/// insensitivity.
/// </summary>
public sealed class EvaluationMetricPropertyTests
{
    private const double Tolerance = 1e-12;

    [Property(MaxTest = 150, Replay = "356001,100003")]
    [Trait("Category", "Property")]
    public bool Compute_GeneratedObservations_KeepsBothMetricsWithinUnitInterval(
        int[] targetSeeds,
        int[] rankingSeeds)
    {
        // arrange
        var observations = EvaluationPropertyFixtures.Observations(targetSeeds, rankingSeeds);

        // act
        var metrics = MetricCalculator.Compute(observations);

        // assert
        return double.IsFinite(metrics.HitRateAt10)
            && double.IsFinite(metrics.Mrr)
            && metrics.HitRateAt10 >= 0.0
            && metrics.HitRateAt10 <= 1.0
            && metrics.Mrr >= 0.0
            && metrics.Mrr <= 1.0;
    }

    [Property(MaxTest = 150, Replay = "356002,200009")]
    [Trait("Category", "Property")]
    public bool Compute_GeneratedObservations_KeepsCountsOrderedAndConsistent(
        int[] targetSeeds,
        int[] rankingSeeds)
    {
        // arrange
        var observations = EvaluationPropertyFixtures.Observations(targetSeeds, rankingSeeds);

        // act
        var metrics = MetricCalculator.Compute(observations);

        // assert
        return HasExpectedCounts(metrics, observations)
            && HasBoundedRankSum(metrics)
            && HasConsistentAverages(metrics);
    }

    private static bool HasExpectedCounts(EvaluationMetrics metrics, ImmutableArray<EvaluationObservation> observations) =>
        metrics.ObservationCount == observations.Length
        && metrics.HitCount >= 0
        && metrics.HitCount <= metrics.ObservationCount;

    private static bool HasBoundedRankSum(EvaluationMetrics metrics) =>
        double.IsFinite(metrics.ReciprocalRankSum)
        && metrics.ReciprocalRankSum >= 0.0
        && metrics.ReciprocalRankSum <= metrics.HitCount + Tolerance;

    private static bool HasConsistentAverages(EvaluationMetrics metrics) =>
        metrics.Mrr <= metrics.HitRateAt10 + Tolerance
        && Within(metrics.HitRateAt10, Average(metrics.HitCount, metrics.ObservationCount))
        && Within(metrics.Mrr, Average(metrics.ReciprocalRankSum, metrics.ObservationCount));

    [Property(MaxTest = 150, Replay = "356003,300011")]
    [Trait("Category", "Property")]
    public bool Compute_ReversedObservationOrder_LeavesMetricsUnchanged(
        int[] targetSeeds,
        int[] rankingSeeds)
    {
        // arrange
        var observations = EvaluationPropertyFixtures.Observations(targetSeeds, rankingSeeds);

        // act
        var forward = MetricCalculator.Compute(observations);
        var reversed = MetricCalculator.Compute([.. observations.Reverse()]);

        // assert
        return forward.ObservationCount == reversed.ObservationCount
            && forward.HitCount == reversed.HitCount
            && Within(forward.HitRateAt10, reversed.HitRateAt10)
            && Within(forward.ReciprocalRankSum, reversed.ReciprocalRankSum)
            && Within(forward.Mrr, reversed.Mrr);
    }

    [Property(MaxTest = 150, Replay = "356004,400001")]
    [Trait("Category", "Property")]
    public bool Compute_AppendedObservation_NeverDecreasesHitCountOrRankSum(
        int[] targetSeeds,
        int[] rankingSeeds,
        int extraTargetSeed,
        int extraRankingSeed)
    {
        // arrange
        var observations = EvaluationPropertyFixtures.Observations(targetSeeds, rankingSeeds);
        var extended = EvaluationPropertyFixtures.Observations(
            [.. targetSeeds, extraTargetSeed],
            [.. rankingSeeds, extraRankingSeed]);

        // act
        var prefix = MetricCalculator.Compute(observations);
        var full = MetricCalculator.Compute(extended);

        // assert
        return full.ObservationCount >= prefix.ObservationCount
            && full.HitCount >= prefix.HitCount
            && full.ReciprocalRankSum + Tolerance >= prefix.ReciprocalRankSum;
    }

    [Property(MaxTest = 150, Replay = "356005,500007")]
    [Trait("Category", "Property")]
    public bool Compute_RankingTailBeyondCutoff_DoesNotChangeMetrics(
        int[] targetSeeds,
        int[] rankingSeeds)
    {
        // arrange
        var observations = EvaluationPropertyFixtures.Observations(targetSeeds, rankingSeeds);
        var truncated = observations
            .Select(observation => new EvaluationObservation(
                observation.Target,
                [.. observation.Ranking.Take(MetricCalculator.Cutoff)]))
            .ToImmutableArray();

        // act
        var full = MetricCalculator.Compute(observations);
        var prefix = MetricCalculator.Compute(truncated);

        // assert
        return full.ObservationCount == prefix.ObservationCount
            && full.HitCount == prefix.HitCount
            && full.ReciprocalRankSum.Equals(prefix.ReciprocalRankSum)
            && full.HitRateAt10.Equals(prefix.HitRateAt10)
            && full.Mrr.Equals(prefix.Mrr);
    }

    private static double Average(double total, int count) =>
        count == 0 ? 0.0 : total / count;

    private static bool Within(double actual, double expected) =>
        Math.Abs(actual - expected) <= Tolerance;
}
