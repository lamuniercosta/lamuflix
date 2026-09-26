using System;
using System.Diagnostics.CodeAnalysis;

namespace LamuFlix.Core.Domain;

public sealed record ImdbRating
{
    public ImdbRating(decimal value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "IMDb rating must be from 0.0 to 10.0 with at most one decimal place.");
        }

        Value = value;
    }

    public decimal Value { get; }

    public static bool TryCreate(decimal value, [NotNullWhen(true)] out ImdbRating? result)
    {
        if (!IsValid(value))
        {
            result = null;
            return false;
        }

        result = new ImdbRating(value);
        return true;
    }

    private static bool IsValid(decimal value) =>
        value >= 0.0m && value <= 10.0m && value == decimal.Round(value, 1);
}
