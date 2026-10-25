using System;
using System.Collections.Generic;
using System.Linq;

namespace LamuFlix.Evaluation.Replay;

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

        return HasSameIdentity(x, y) && HasSameRanking(x, y);
    }

    private static bool HasSameIdentity(EvaluationObservationResult x, EvaluationObservationResult y)
    {
        return string.Equals(x.EventId, y.EventId, StringComparison.Ordinal)
            && string.Equals(x.UserId, y.UserId, StringComparison.Ordinal)
            && x.Target == y.Target
            && x.Timestamp == y.Timestamp;
    }

    private static bool HasSameRanking(EvaluationObservationResult x, EvaluationObservationResult y)
    {
        return x.Ranking.SequenceEqual(y.Ranking) && x.Score.Equals(y.Score);
    }

    public int GetHashCode(EvaluationObservationResult obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return HashCode.Combine(obj.EventId, obj.UserId, obj.Target, obj.Timestamp, obj.Score);
    }
}
