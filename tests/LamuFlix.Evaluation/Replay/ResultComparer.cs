using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Evaluation.Replay;

namespace LamuFlix.Evaluation.Tests.Replay;

internal sealed class ResultComparer : IEqualityComparer<EvaluationObservationResult>
{
    public static readonly ResultComparer Instance = new();

    public bool Equals(EvaluationObservationResult? x, EvaluationObservationResult? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return string.Equals(x.EventId, y.EventId, StringComparison.Ordinal)
            && string.Equals(x.UserId, y.UserId, StringComparison.Ordinal)
            && x.Target == y.Target
            && x.Timestamp == y.Timestamp
            && x.Ranking.SequenceEqual(y.Ranking)
            && x.Score.Equals(y.Score);
    }

    public int GetHashCode(EvaluationObservationResult obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return HashCode.Combine(obj.EventId, obj.UserId, obj.Target, obj.Timestamp, obj.Score);
    }
}
