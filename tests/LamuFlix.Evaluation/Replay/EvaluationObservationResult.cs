using System;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Evaluation.Replay;

/// <summary>
/// One scored as-of observation. <see cref="Ranking"/> is the real DEV-416 MMR
/// selection order and <see cref="Score"/> is the target's original as-of
/// relevance, preserved for leakage evidence (before any offset is applied).
/// </summary>
public sealed record EvaluationObservationResult(
    string EventId,
    string UserId,
    MovieId Target,
    DateTimeOffset Timestamp,
    ImmutableArray<MovieId> Ranking,
    double Score);
