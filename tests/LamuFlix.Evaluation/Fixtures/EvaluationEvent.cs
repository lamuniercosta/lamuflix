using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.Evaluation.Fixtures;

public sealed record EvaluationEvent(string EventId, string UserId, MovieId MovieId, DateTimeOffset Timestamp);
