using System;
using LamuFlix.Evaluation.Metrics;
using LamuFlix.Evaluation.Replay;

namespace LamuFlix.Evaluation.Output;

public sealed record MetricsSummary(
    int SchemaVersion,
    string FixtureId,
    int Seed,
    int Cutoff,
    int ObservationCount,
    int HitCount,
    double ReciprocalRankSum,
    double HitRateAt10,
    double Mrr)
{
    public const int CurrentSchemaVersion = 1;

    public static MetricsSummary FromReplay(string fixtureId, int seed, EvaluationReplayResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new MetricsSummary(
            CurrentSchemaVersion,
            fixtureId,
            seed,
            MetricCalculator.Cutoff,
            result.Metrics.ObservationCount,
            result.Metrics.HitCount,
            result.Metrics.ReciprocalRankSum,
            result.Metrics.HitRateAt10,
            result.Metrics.Mrr);
    }
}
