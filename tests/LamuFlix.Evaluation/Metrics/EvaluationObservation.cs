using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Evaluation.Metrics;

public sealed record EvaluationObservation(MovieId Target, ImmutableArray<MovieId> Ranking);
