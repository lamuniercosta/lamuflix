using System;
using System.Diagnostics.CodeAnalysis;

namespace LamuFlix.Core.Domain;

public sealed record MediaFormat
{
    public MediaFormat(string? extension)
    {
        if (!TryNormalize(extension, out var normalized))
        {
            throw new ArgumentException("Media format must contain a non-blank extension.", nameof(extension));
        }

        Extension = normalized;
    }

    public string Extension { get; }

    public static bool TryCreate(string? extension, [NotNullWhen(true)] out MediaFormat? result)
    {
        if (extension is null || !TryNormalize(extension, out _))
        {
            result = null;
            return false;
        }

        result = new MediaFormat(extension);
        return true;
    }

    private static bool TryNormalize(string? extension, [NotNullWhen(true)] out string? normalized)
    {
        if (extension is null)
        {
            normalized = null;
            return false;
        }

        var trimmed = extension.Trim();
        if (trimmed.StartsWith('.'))
        {
            trimmed = trimmed[1..];
        }

        trimmed = trimmed.Trim();
        trimmed = trimmed.ToLowerInvariant();
        if (trimmed.Length == 0 || trimmed.StartsWith('.'))
        {
            normalized = null;
            return false;
        }

        normalized = trimmed;
        return true;
    }
}
