using System;
using System.Diagnostics.CodeAnalysis;

namespace LamuFlix.Core.Domain;

public sealed record ReleaseYear
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
}
