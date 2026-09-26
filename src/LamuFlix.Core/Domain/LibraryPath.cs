using System;
using System.Diagnostics.CodeAnalysis;

namespace LamuFlix.Core.Domain;

public sealed record LibraryPath
{
    public LibraryPath(string? value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException("Library path must be non-blank and must not contain '..'.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out LibraryPath? result)
    {
        if (!IsValid(value))
        {
            result = null;
            return false;
        }

        result = new LibraryPath(value);
        return true;
    }

    private static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Contains("..", StringComparison.Ordinal);
}
