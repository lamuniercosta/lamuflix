using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Evaluation.Metrics;

public static class MetricCalculator
{
    public const int Cutoff = 10;

    private const int NearCutoff = 3;
    private const int FirstRank = 1;

    public static EvaluationMetrics Compute(IReadOnlyList<EvaluationObservation> observations)
    {
        var accumulation = Accumulate(observations);
        var observationCount = accumulation.AttemptedCount;

        return new EvaluationMetrics(
            observationCount,
            accumulation.HitAtCutoff,
            accumulation.ReciprocalRankSum,
            observationCount == 0 ? 0.0 : (double)accumulation.HitAtCutoff / observationCount,
            observationCount == 0 ? 0.0 : accumulation.ReciprocalRankSum / observationCount);
    }

    public static RankAccumulation Accumulate(IReadOnlyList<EvaluationObservation> observations) =>
        Accumulate(observations, Cutoff);

    public static RankAccumulation Accumulate(IReadOnlyList<EvaluationObservation> observations, int reciprocalRankCutoff)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(reciprocalRankCutoff);

        var hitAtFirst = 0;
        var hitAtNear = 0;
        var hitAtCutoff = 0;
        var reciprocalRankSum = 0.0;

        foreach (var observation in observations)
        {
            var rank = RankOf(observation);
            if (rank == 0)
            {
                continue;
            }

            if (rank <= FirstRank)
            {
                hitAtFirst++;
            }

            if (rank <= NearCutoff)
            {
                hitAtNear++;
            }

            if (rank <= Cutoff)
            {
                hitAtCutoff++;
            }

            if (rank <= reciprocalRankCutoff)
            {
                reciprocalRankSum += 1.0 / rank;
            }
        }

        return new RankAccumulation(
            observations.Count,
            hitAtFirst,
            hitAtNear,
            hitAtCutoff,
            reciprocalRankSum);
    }

    private static int RankOf(EvaluationObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(observation.Target);

        if (observation.Ranking.IsDefault)
        {
            throw new ArgumentException(
                "Observation ranking must be initialized.",
                nameof(observation));
        }

        EnsureUnique(observation.Ranking);

        for (var index = 0; index < observation.Ranking.Length; index++)
        {
            if (observation.Ranking[index] == observation.Target)
            {
                return index + 1;
            }
        }

        return 0;
    }

    private static void EnsureUnique(ImmutableArray<MovieId> ranking)
    {
        var seen = new HashSet<MovieId>();

        foreach (var movieId in ranking)
        {
            if (!seen.Add(movieId))
            {
                throw new ArgumentException(
                    $"Observation ranking movie ids must be unique; duplicate id '{movieId.Value}'.",
                    nameof(ranking));
            }
        }
    }
}
