using System;
using System.Diagnostics.CodeAnalysis;

namespace LamuFlix.Core.Domain;

public sealed record ReleaseYear : IComparable<ReleaseYear>
{
    public ReleaseYear(int value, DateTimeOffset now)
    {
        if (!IsValid(value, now))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Release year must be from 1888 through five years after the supplied year.");
        }

        Value = value;
    }

    public int Value { get; }

    public int CompareTo(ReleaseYear? other) => other is null ? 1 : Value.CompareTo(other.Value);

    public static bool operator <(ReleaseYear? left, ReleaseYear? right) => Compare(left, right) < 0;

    public static bool operator <=(ReleaseYear? left, ReleaseYear? right) => Compare(left, right) <= 0;

    public static bool operator >(ReleaseYear? left, ReleaseYear? right) => Compare(left, right) > 0;

    public static bool operator >=(ReleaseYear? left, ReleaseYear? right) => Compare(left, right) >= 0;

    public static bool TryCreate(int value, DateTimeOffset now, [NotNullWhen(true)] out ReleaseYear? result)
    {
        if (!IsValid(value, now))
        {
            result = null;
            return false;
        }

        result = new ReleaseYear(value, now);
        return true;
    }

    private static bool IsValid(int value, DateTimeOffset now) =>
        value >= 1888 && value <= now.Year + 5;

    private static int Compare(ReleaseYear? left, ReleaseYear? right) =>
        left is null ? (right is null ? 0 : -1) : left.CompareTo(right);
}
