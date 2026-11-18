namespace LamuFlix.Evaluation.Metrics;

public sealed record RankAccumulation(
    int AttemptedCount,
    int HitAtFirst,
    int HitAtNear,
    int HitAtCutoff,
    double ReciprocalRankSum);
