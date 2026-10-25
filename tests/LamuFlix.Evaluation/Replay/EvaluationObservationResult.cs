using System;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Evaluation.Replay;

public sealed record EvaluationObservationResult(
    string EventId,
    string UserId,
    MovieId Target,
    DateTimeOffset Timestamp,
    ImmutableArray<MovieId> Ranking,
    double Score);
