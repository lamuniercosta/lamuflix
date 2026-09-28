using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace LamuFlix.Core.Domain;

public sealed partial record ImdbId
{
    public ImdbId(string? value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException("IMDb id must match tt followed by 7 or 8 digits.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out ImdbId? result)
    {
        if (!IsValid(value))
        {
            result = null;
            return false;
        }

        result = new ImdbId(value);
        return true;
    }

    private static bool IsValid([NotNullWhen(true)] string? value) =>
        value is not null && IdPattern().IsMatch(value);

    [GeneratedRegex(@"^tt[0-9]{7,8}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();
}
