using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Evaluation.Metrics;

public static class MetricCalculator
{
    public const int Cutoff = 10;

    public static EvaluationMetrics Compute(IReadOnlyList<EvaluationObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);

        var hitCount = 0;
        var reciprocalRankSum = 0.0;

        foreach (var observation in observations)
        {
            var rank = RankOf(observation);
            if (rank == 0)
            {
                continue;
            }

            hitCount++;
            reciprocalRankSum += 1.0 / rank;
        }

        var observationCount = observations.Count;

        return new EvaluationMetrics(
            observationCount,
            hitCount,
            reciprocalRankSum,
            observationCount == 0 ? 0.0 : (double)hitCount / observationCount,
            observationCount == 0 ? 0.0 : reciprocalRankSum / observationCount);
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

        for (var index = 0; index < observation.Ranking.Length && index < Cutoff; index++)
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
