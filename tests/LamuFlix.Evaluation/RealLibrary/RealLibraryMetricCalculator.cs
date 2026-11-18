using System;
using System.Collections.Generic;
using LamuFlix.Evaluation.Metrics;

namespace LamuFlix.Evaluation.RealLibrary;

public static class RealLibraryMetricCalculator
{
    public static RealLibraryMetrics Compute(
        IReadOnlyList<EvaluationObservation> observations,
        int reciprocalRankCutoff)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(reciprocalRankCutoff);

        var accumulation = MetricCalculator.Accumulate(observations, reciprocalRankCutoff);

        if (accumulation.AttemptedCount == 0)
        {
            return new RealLibraryMetrics(0, null, null, null, null);
        }

        var attempted = accumulation.AttemptedCount;

        return new RealLibraryMetrics(
            attempted,
            (double)accumulation.HitAtFirst / attempted,
            (double)accumulation.HitAtNear / attempted,
            (double)accumulation.HitAtCutoff / attempted,
            accumulation.ReciprocalRankSum / attempted);
    }
}

public sealed record RealLibraryMetrics(
    int AttemptedCount,
    double? HitRateAt1,
    double? HitRateAt3,
    double? HitRateAt10,
    double? Mrr);
