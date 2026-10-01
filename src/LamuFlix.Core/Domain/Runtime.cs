using System;
using System.Diagnostics.CodeAnalysis;

namespace LamuFlix.Core.Domain;

public sealed record Runtime : IComparable<Runtime>
{
    public Runtime(int minutes)
    {
        if (!IsValid(minutes))
        {
            throw new ArgumentOutOfRangeException(nameof(minutes), minutes, "Runtime must be greater than zero minutes.");
        }

        Minutes = minutes;
    }

    public int Minutes { get; }

    public int CompareTo(Runtime? other) => other is null ? 1 : Minutes.CompareTo(other.Minutes);

    public static bool operator <(Runtime? left, Runtime? right) => Compare(left, right) < 0;

    public static bool operator <=(Runtime? left, Runtime? right) => Compare(left, right) <= 0;

    public static bool operator >(Runtime? left, Runtime? right) => Compare(left, right) > 0;

    public static bool operator >=(Runtime? left, Runtime? right) => Compare(left, right) >= 0;

    public static bool TryCreate(int minutes, [NotNullWhen(true)] out Runtime? result)
    {
        if (!IsValid(minutes))
        {
            result = null;
            return false;
        }

        result = new Runtime(minutes);
        return true;
    }

    private static bool IsValid(int minutes) => minutes > 0;

    private static int Compare(Runtime? left, Runtime? right) =>
        left is null ? (right is null ? 0 : -1) : left.CompareTo(right);
}
