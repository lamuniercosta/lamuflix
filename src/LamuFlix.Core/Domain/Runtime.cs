using System;
using System.Diagnostics.CodeAnalysis;

namespace LamuFlix.Core.Domain;

public sealed record Runtime
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
}
