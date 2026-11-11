using System;
using System.IO;

namespace LamuFlix.Core.Features.Subtitles;

public static class SubtitleSidecarName
{
    private const string SubtitleExtension = ".srt";

    public static string For(string primaryFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryFileName);

        if (!string.Equals(Path.GetFileName(primaryFileName), primaryFileName, StringComparison.Ordinal))
        {
            throw new ArgumentException("The primary file name must not carry a directory.", nameof(primaryFileName));
        }

        var stem = Path.GetFileNameWithoutExtension(primaryFileName);
        if (string.IsNullOrEmpty(stem) || stem is "." or "..")
        {
            throw new ArgumentException("The primary file name has no usable stem.", nameof(primaryFileName));
        }

        return string.Concat(stem, SubtitleExtension);
    }
}
