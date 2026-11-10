using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LamuFlix.Core.Features.Subtitles;

/// <summary>One eligible sidecar found beside the resolved primary video. No token yet.</summary>
public sealed record SubtitleSidecarCandidate(string FileName, string Label, string Language, bool IsDefault);

/// <summary>
/// Recognizes eligible BS Player style sidecar subtitles for one resolved primary video: exact,
/// case-insensitive stem matching against <c>&lt;stem&gt;.srt</c>, <c>&lt;stem&gt;.&lt;suffix&gt;.srt</c> and
/// <c>&lt;stem&gt;-&lt;suffix&gt;.srt</c> in the primary video's own directory only. No fuzzy matching, no
/// recursion, no compound suffix grammar.
/// </summary>
public static class SubtitleSidecarDiscovery
{
    public const string SubtitleExtension = ".srt";
    public const string UndeterminedLanguage = "und";
    public const string BrazilianPortugueseLanguage = "pt-BR";

    private const string DefaultMarker = "default";
    private const string EnglishSuffix = "en";
    private const string BrazilianPortugueseSuffix = "pob";

    private const string BareLabel = "Subtitles";
    private const string DefaultLabel = "Default";
    private const string EnglishLabel = "English";
    private const string BrazilianPortugueseLabel = "Portuguese (Brazil)";

    /// <summary>
    /// Returns the eligible sidecars for the primary video file <paramref name="primaryFileName"/> from the
    /// candidate file names of its directory, sorted default-marker first, then language and label ordinal
    /// case-insensitively, then file name ordinal, with duplicate labels disambiguated as <c>(2)</c>,
    /// <c>(3)</c> after sorting.
    /// </summary>
    public static IReadOnlyList<SubtitleSidecarCandidate> Discover(
        string primaryFileName,
        IEnumerable<string> candidateFileNames)
    {
        ArgumentNullException.ThrowIfNull(primaryFileName);
        ArgumentNullException.ThrowIfNull(candidateFileNames);

        var stem = Path.GetFileNameWithoutExtension(primaryFileName);
        if (string.IsNullOrEmpty(stem))
        {
            return [];
        }

        var matches = candidateFileNames
            .Select(fileName => Match(stem, fileName))
            .OfType<SubtitleSidecarCandidate>()
            .OrderBy(candidate => candidate.IsDefault ? 0 : 1)
            .ThenBy(candidate => candidate.Language, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.FileName, StringComparer.Ordinal)
            .ToList();

        return NumberDuplicateLabels(matches);
    }

    private static SubtitleSidecarCandidate? Match(string stem, string fileName)
    {
        if (fileName.Length <= stem.Length
            || !fileName.EndsWith(SubtitleExtension, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var withoutExtension = fileName[..^SubtitleExtension.Length];
        if (!withoutExtension.StartsWith(stem, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (withoutExtension.Length == stem.Length)
        {
            return new SubtitleSidecarCandidate(fileName, BareLabel, UndeterminedLanguage, IsDefault: false);
        }

        var separator = withoutExtension[stem.Length];
        if (separator is not ('.' or '-'))
        {
            return null;
        }

        var suffix = withoutExtension[(stem.Length + 1)..];
        if (suffix.Length == 0 || suffix.Contains('.', StringComparison.Ordinal)
            || suffix.Contains('-', StringComparison.Ordinal))
        {
            // No compound language/flag grammar: a suffix is exactly one token.
            return null;
        }

        return ClassifySuffix(fileName, withoutExtension, suffix);
    }

    private static SubtitleSidecarCandidate ClassifySuffix(string fileName, string withoutExtension, string suffix)
    {
        if (string.Equals(suffix, DefaultMarker, StringComparison.OrdinalIgnoreCase))
        {
            return new SubtitleSidecarCandidate(fileName, DefaultLabel, UndeterminedLanguage, IsDefault: true);
        }

        if (string.Equals(suffix, EnglishSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return new SubtitleSidecarCandidate(fileName, EnglishLabel, EnglishSuffix, IsDefault: false);
        }

        if (string.Equals(suffix, BrazilianPortugueseSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return new SubtitleSidecarCandidate(
                fileName, BrazilianPortugueseLabel, BrazilianPortugueseLanguage, IsDefault: false);
        }

        return new SubtitleSidecarCandidate(fileName, withoutExtension, UndeterminedLanguage, IsDefault: false);
    }

    private static IReadOnlyList<SubtitleSidecarCandidate> NumberDuplicateLabels(
        List<SubtitleSidecarCandidate> ordered)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<SubtitleSidecarCandidate>(ordered.Count);
        foreach (var candidate in ordered)
        {
            counts.TryGetValue(candidate.Label, out var seen);
            counts[candidate.Label] = seen + 1;
            var label = seen == 0 ? candidate.Label : $"{candidate.Label} ({seen + 1})";
            candidates.Add(candidate with { Label = label });
        }

        return candidates;
    }
}
