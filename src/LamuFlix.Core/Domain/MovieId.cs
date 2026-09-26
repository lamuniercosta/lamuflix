using System;
using System.Diagnostics.CodeAnalysis;

namespace LamuFlix.Core.Domain;

public sealed record MovieId
{
    public MovieId(int value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Movie id must be greater than zero.");
        }

        Value = value;
    }

    public int Value { get; }

    public static bool TryCreate(int value, [NotNullWhen(true)] out MovieId? result)
    {
        if (!IsValid(value))
        {
            result = null;
            return false;
        }

        result = new MovieId(value);
        return true;
    }

    private static bool IsValid(int value) => value > 0;
}
