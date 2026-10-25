namespace LamuFlix.Evaluation.Metrics;

public sealed record EvaluationMetrics(
    int ObservationCount,
    int HitCount,
    double ReciprocalRankSum,
    double HitRateAt10,
    double Mrr);
