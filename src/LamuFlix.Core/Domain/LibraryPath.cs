using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace LamuFlix.Core.Domain;

public sealed record LibraryPath
{
    private const string BlankOrTraversalMessage = "Library path must be non-blank and must not contain '..'.";
    private const string DriveRelativeMessage = "Library path must not be drive-relative.";
    private const string MalformedMessage = "Library path is malformed.";
    private const string TraversalSegment = "..";
    private const string CurrentSegment = ".";
    private const string RootIdentity = ".";

    public LibraryPath(string? value)
    {
        if (!TryCanonicalize(value, out var canonical))
        {
            throw new ArgumentException(DescribeFailure(value), nameof(value));
        }

        Value = canonical;
    }

    public string Value { get; }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out LibraryPath? result)
    {
        result = TryCanonicalize(value, out var canonical) ? new LibraryPath(canonical) : null;
        return result is not null;
    }

    /// <summary>
    /// Canonicalises a non-rooted library identity: forward slashes, redundant <c>.</c> segments
    /// removed, the library root represented as <c>.</c>, original segment casing preserved.
    /// </summary>
    public static bool TryCanonicalizeRelative(string? value, [NotNullWhen(true)] out string? canonical)
    {
        canonical = null;
        if (!IsWellFormed(value) || IsAbsolute(value))
        {
            return false;
        }

        canonical = ToCanonical(value);
        return true;
    }

    /// <summary>
    /// Canonicalises a relative identity or an absolute value beneath <paramref name="rootPath"/>
    /// using OS-independent string parsing. Values outside the root are rejected.
    /// </summary>
    public static bool TryCanonicalizeUnderRoot(string? value, string? rootPath, [NotNullWhen(true)] out string? canonical)
    {
        canonical = null;
        if (!IsWellFormed(value) || string.IsNullOrWhiteSpace(rootPath) || !IsWellFormed(rootPath))
        {
            return false;
        }

        if (!IsAbsolute(value))
        {
            return TryCanonicalizeRelative(value, out canonical);
        }

        if (!TryRelativeSuffix(rootPath, value, out var suffix))
        {
            return false;
        }

        canonical = ToCanonical(suffix);
        return true;
    }

    /// <summary>
    /// True when two absolute prefixes touch the same path segments, so mapping a stored path
    /// against both could yield two different relative identities.
    /// </summary>
    public static bool OverlapsAtSegmentBoundary(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second)
            || UsesWindowsSyntax(first) != UsesWindowsSyntax(second))
        {
            return false;
        }

        var comparison = UsesWindowsSyntax(first) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var left = TrimTrailingSeparators(Normalize(first));
        var right = TrimTrailingSeparators(Normalize(second));
        return IsBoundaryMatch(left, right, comparison) || IsBoundaryMatch(right, left, comparison);
    }

    private static bool TryCanonicalize(string? value, [NotNullWhen(true)] out string? canonical)
    {
        if (!IsWellFormed(value))
        {
            canonical = null;
            return false;
        }

        canonical = IsAbsolute(value) ? value : ToCanonical(value);
        return true;
    }

    private static bool IsWellFormed([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value) && !HasParentTraversal(value) && !IsDriveRelative(value)
        && !HasControlCharacter(value);

    /// <summary>
    /// True for an absolute Windows drive/UNC root or an absolute POSIX root, judged by syntax
    /// alone so either host can recognise a path written for the other.
    /// </summary>
    public static bool IsAbsolute([NotNullWhen(true)] string? value) =>
        !string.IsNullOrEmpty(value) && !IsDriveRelative(value) && !HasControlCharacter(value)
        && (IsSeparator(value[0]) || IsDriveAbsolute(value));

    private static bool HasParentTraversal(string value) => HasSegment(value, TraversalSegment);

    private static bool HasSegment(string value, string segment)
    {
        var start = 0;
        for (var index = 0; index <= value.Length; index++)
        {
            if (index < value.Length && !IsSeparator(value[index]))
            {
                continue;
            }

            if (value.AsSpan(start, index - start).SequenceEqual(segment))
            {
                return true;
            }

            start = index + 1;
        }

        return false;
    }

    private static string ToCanonical(string value) =>
        JoinSegments(value) is { Length: > 0 } joined ? joined : RootIdentity;

    private static string JoinSegments(string value)
    {
        var builder = new StringBuilder(value.Length);
        var start = 0;
        for (var index = 0; index <= value.Length; index++)
        {
            if (index < value.Length && !IsSeparator(value[index]))
            {
                continue;
            }

            AppendSegment(builder, value.AsSpan(start, index - start));
            start = index + 1;
        }

        return builder.ToString();
    }

    private static void AppendSegment(StringBuilder builder, ReadOnlySpan<char> segment)
    {
        if (segment.Length == 0 || segment.SequenceEqual(CurrentSegment))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append('/');
        }

        builder.Append(segment);
    }

    private static bool TryRelativeSuffix(string rootPath, string value, out string suffix)
    {
        suffix = string.Empty;
        if (UsesWindowsSyntax(rootPath) != UsesWindowsSyntax(value))
        {
            return false;
        }

        var comparison = UsesWindowsSyntax(rootPath) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var root = TrimTrailingSeparators(Normalize(rootPath));
        var candidate = Normalize(value);
        if (!IsBoundaryMatch(root, candidate, comparison))
        {
            return false;
        }

        suffix = candidate.Length == root.Length ? string.Empty : candidate[root.Length..].Trim('/');
        return true;
    }

    private static bool IsBoundaryMatch(string prefix, string candidate, StringComparison comparison)
    {
        if (string.Equals(prefix, candidate, comparison))
        {
            return true;
        }

        return candidate.Length > prefix.Length
            && candidate.StartsWith(prefix, comparison)
            && (prefix[^1] == '/' || candidate[prefix.Length] == '/');
    }

    private static bool UsesWindowsSyntax(string value) =>
        (value.Length >= 2 && value[1] == ':')
        || (value.Length >= 2 && IsSeparator(value[0]) && IsSeparator(value[1]));

    private static string Normalize(string value) => value.Replace('\\', '/');

    private static string TrimTrailingSeparators(string value)
    {
        var end = value.Length;
        while (end > 1 && value[end - 1] == '/')
        {
            end--;
        }

        return value[..end];
    }

    private static bool IsDriveAbsolute(string value) =>
        value.Length >= 3 && value[1] == ':' && IsSeparator(value[2]);

    private static bool IsDriveRelative(string value) =>
        value.Length >= 2 && value[1] == ':' && !IsDriveAbsolute(value);

    private static bool HasControlCharacter(string value)
    {
        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSeparator(char character) => character is '/' or '\\';

    private static string DescribeFailure(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value) && IsDriveRelative(value))
        {
            return DriveRelativeMessage;
        }

        if (!string.IsNullOrWhiteSpace(value) && !HasParentTraversal(value) && HasControlCharacter(value))
        {
            return MalformedMessage;
        }

        return BlankOrTraversalMessage;
    }
}
