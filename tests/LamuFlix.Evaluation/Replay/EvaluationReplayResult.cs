using System.Collections.Immutable;
using LamuFlix.Evaluation.Metrics;

namespace LamuFlix.Evaluation.Replay;

public sealed record EvaluationReplayResult(
    int Seed,
    ImmutableArray<EvaluationObservationResult> Observations,
    EvaluationMetrics Metrics);
