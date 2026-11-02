using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace LamuFlix.Infrastructure.FileSystem;

/// <summary>
/// The single home for the supported video extension set, shared by the single-folder scanner
/// and the recursive candidate discovery so membership can never drift between them.
/// </summary>
internal static class SupportedVideoExtensions
{
    private static readonly FrozenSet<string> Supported =
        FrozenSet.ToFrozenSet(
            [".mkv", ".mp4", ".avi", ".m4v", ".rmvb", ".rm", ".flv"],
            StringComparer.OrdinalIgnoreCase);

    /// <summary>The exact supported extension set, with the landed OrdinalIgnoreCase membership.</summary>
    public static IReadOnlyCollection<string> All => Supported;

    /// <summary>True when <paramref name="extension"/> is a supported video extension.</summary>
    public static bool Contains(string extension) => Supported.Contains(extension);
}
