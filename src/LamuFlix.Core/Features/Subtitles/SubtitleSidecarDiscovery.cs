using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record SubtitleSidecarCandidate(string FileName, string Label, string Language, bool IsDefault);

public static class SubtitleSidecarDiscovery
{
    private const string SubtitleExtension = ".srt";
    private const string SubsFolderName = "subs";
    private const string UndeterminedLanguage = "und";

    private const string DefaultMarker = "default";

    private const string BareLabel = "Subtitles";
    private const string DefaultLabel = "Default";

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

        var languages = LanguageTable();
        var names = LanguageNames(languages);
        var matches = candidateFileNames
            .Select(fileName => Match(stem, fileName, languages, names))
            .OfType<SubtitleSidecarCandidate>()
            .OrderBy(candidate => candidate.IsDefault ? 0 : 1)
            .ThenBy(candidate => candidate.Language, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.FileName, StringComparer.Ordinal)
            .ToList();

        return NumberDuplicateLabels(matches);
    }

    private static SubtitleSidecarCandidate? Match(
        string stem,
        string fileName,
        Dictionary<string, (string Language, string Label)> languages,
        Dictionary<string, (string Language, string Label)> names)
    {
        if (!fileName.EndsWith(SubtitleExtension, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!SplitCandidate(fileName, out var folder, out var leaf))
        {
            return null;
        }

        var withoutExtension = leaf[..^SubtitleExtension.Length];
        if (withoutExtension.StartsWith(stem, StringComparison.OrdinalIgnoreCase))
        {
            return MatchStemmed(fileName, stem, withoutExtension, languages);
        }

        return folder is null ? null : MatchReleaseGroup(fileName, withoutExtension, languages, names);
    }

    private static bool SplitCandidate(string fileName, out string? folder, out string leaf)
    {
        folder = null;
        leaf = fileName;
        var parts = fileName.Split('/', '\\');
        if (parts.Length == 1)
        {
            return IsSafeLeaf(leaf);
        }

        if (parts.Length != 2 || !string.Equals(parts[0], SubsFolderName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        folder = parts[0];
        leaf = parts[1];
        return IsSafeLeaf(leaf);
    }

    private static bool IsSafeLeaf(string leaf)
    {
        if (string.IsNullOrEmpty(leaf) || leaf is "." or "..")
        {
            return false;
        }

        foreach (var character in leaf)
        {
            if (char.IsControl(character))
            {
                return false;
            }
        }

        return true;
    }

    private static SubtitleSidecarCandidate? MatchStemmed(
        string fileName,
        string stem,
        string withoutExtension,
        Dictionary<string, (string Language, string Label)> languages)
    {
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
        if (suffix.Length == 0)
        {
            return null;
        }

        suffix = StripDuplicateCounter(suffix);
        if (suffix is null || suffix.Length == 0 || suffix.Contains('.') || suffix.Contains('-'))
        {
            return null;
        }

        return ClassifySuffix(fileName, withoutExtension, suffix, languages);
    }

    private static string? StripDuplicateCounter(string suffix)
    {
        if (!suffix.EndsWith(')'))
        {
            return suffix;
        }

        var open = suffix.LastIndexOf('(');
        if (open <= 0 || open + 1 >= suffix.Length - 1)
        {
            return null;
        }

        for (var index = open + 1; index < suffix.Length - 1; index++)
        {
            if (!char.IsAsciiDigit(suffix[index]))
            {
                return null;
            }
        }

        return suffix[..open];
    }

    private static SubtitleSidecarCandidate ClassifySuffix(
        string fileName,
        string withoutExtension,
        string suffix,
        Dictionary<string, (string Language, string Label)> languages)
    {
        if (string.Equals(suffix, DefaultMarker, StringComparison.OrdinalIgnoreCase))
        {
            return new SubtitleSidecarCandidate(fileName, DefaultLabel, UndeterminedLanguage, IsDefault: true);
        }

        if (languages.TryGetValue(suffix, out var language))
        {
            return new SubtitleSidecarCandidate(fileName, language.Label, language.Language, IsDefault: false);
        }

        return new SubtitleSidecarCandidate(fileName, withoutExtension, UndeterminedLanguage, IsDefault: false);
    }

    private static SubtitleSidecarCandidate MatchReleaseGroup(
        string fileName,
        string withoutExtension,
        Dictionary<string, (string Language, string Label)> languages,
        Dictionary<string, (string Language, string Label)> names)
    {
        var parts = withoutExtension.Split('.');
        if (parts.Length == 1)
        {
            var bare = StripDuplicateCounter(parts[0]) ?? parts[0];
            if (languages.TryGetValue(bare, out var single) || names.TryGetValue(bare, out single))
            {
                return new SubtitleSidecarCandidate(fileName, single.Label, single.Language, IsDefault: false);
            }

            return new SubtitleSidecarCandidate(fileName, parts[0], UndeterminedLanguage, IsDefault: false);
        }

        var codeIndex = FindCodePart(parts, languages);
        if (codeIndex < 0)
        {
            return new SubtitleSidecarCandidate(fileName, withoutExtension, UndeterminedLanguage, IsDefault: false);
        }

        var entry = languages[parts[codeIndex]];
        var qualifier = FindQualifier(parts, codeIndex);
        var label = qualifier is null ? entry.Label : string.Concat(entry.Label, " (", qualifier, ")");
        return new SubtitleSidecarCandidate(fileName, label, entry.Language, IsDefault: false);
    }

    private static int FindCodePart(
        string[] parts,
        Dictionary<string, (string Language, string Label)> languages)
    {
        for (var index = 0; index < parts.Length; index++)
        {
            if (parts[index].Length != 0 && languages.ContainsKey(parts[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private static string? FindQualifier(string[] parts, int codeIndex)
    {
        for (var index = 0; index < parts.Length; index++)
        {
            if (index != codeIndex && parts[index].Length != 0)
            {
                return parts[index];
            }
        }

        return null;
    }

    private static Dictionary<string, (string Language, string Label)> LanguageTable()
    {
        var entries = new (string Code, string Language, string Label)[]
        {
            ("pb", "pt-BR", "Portuguese (Brazil)"),
            ("pob", "pt-BR", "Portuguese (Brazil)"),
            ("por", "pt", "Portuguese"),
            ("pt", "pt", "Portuguese"),
            ("en", "en", "English"),
            ("eng", "en", "English"),
            ("es", "es", "Spanish"),
            ("spa", "es", "Spanish"),
            ("alb", "sq", "Albanian"),
            ("sqi", "sq", "Albanian"),
            ("sq", "sq", "Albanian"),
            ("ara", "ar", "Arabic"),
            ("ar", "ar", "Arabic"),
            ("arm", "hy", "Armenian"),
            ("hye", "hy", "Armenian"),
            ("hy", "hy", "Armenian"),
            ("baq", "eu", "Basque"),
            ("eus", "eu", "Basque"),
            ("eu", "eu", "Basque"),
            ("bel", "be", "Belarusian"),
            ("be", "be", "Belarusian"),
            ("ben", "bn", "Bengali"),
            ("bn", "bn", "Bengali"),
            ("bos", "bs", "Bosnian"),
            ("bs", "bs", "Bosnian"),
            ("bul", "bg", "Bulgarian"),
            ("bg", "bg", "Bulgarian"),
            ("cat", "ca", "Catalan"),
            ("ca", "ca", "Catalan"),
            ("chi", "zh", "Chinese"),
            ("zho", "zh", "Chinese"),
            ("zh", "zh", "Chinese"),
            ("hrv", "hr", "Croatian"),
            ("scr", "hr", "Croatian"),
            ("hr", "hr", "Croatian"),
            ("cze", "cs", "Czech"),
            ("ces", "cs", "Czech"),
            ("cs", "cs", "Czech"),
            ("dan", "da", "Danish"),
            ("da", "da", "Danish"),
            ("dut", "nl", "Dutch"),
            ("nld", "nl", "Dutch"),
            ("nl", "nl", "Dutch"),
            ("est", "et", "Estonian"),
            ("et", "et", "Estonian"),
            ("fin", "fi", "Finnish"),
            ("fi", "fi", "Finnish"),
            ("fre", "fr", "French"),
            ("fra", "fr", "French"),
            ("fr", "fr", "French"),
            ("geo", "ka", "Georgian"),
            ("kat", "ka", "Georgian"),
            ("ka", "ka", "Georgian"),
            ("ger", "de", "German"),
            ("deu", "de", "German"),
            ("de", "de", "German"),
            ("glg", "gl", "Galician"),
            ("gl", "gl", "Galician"),
            ("gre", "el", "Greek"),
            ("ell", "el", "Greek"),
            ("el", "el", "Greek"),
            ("heb", "he", "Hebrew"),
            ("he", "he", "Hebrew"),
            ("hin", "hi", "Hindi"),
            ("hi", "hi", "Hindi"),
            ("hun", "hu", "Hungarian"),
            ("hu", "hu", "Hungarian"),
            ("ice", "is", "Icelandic"),
            ("isl", "is", "Icelandic"),
            ("is", "is", "Icelandic"),
            ("ind", "id", "Indonesian"),
            ("id", "id", "Indonesian"),
            ("ita", "it", "Italian"),
            ("it", "it", "Italian"),
            ("jpn", "ja", "Japanese"),
            ("ja", "ja", "Japanese"),
            ("kor", "ko", "Korean"),
            ("ko", "ko", "Korean"),
            ("lav", "lv", "Latvian"),
            ("lv", "lv", "Latvian"),
            ("lit", "lt", "Lithuanian"),
            ("lt", "lt", "Lithuanian"),
            ("mac", "mk", "Macedonian"),
            ("mkd", "mk", "Macedonian"),
            ("mk", "mk", "Macedonian"),
            ("may", "ms", "Malay"),
            ("msa", "ms", "Malay"),
            ("ms", "ms", "Malay"),
            ("nor", "no", "Norwegian"),
            ("no", "no", "Norwegian"),
            ("per", "fa", "Persian"),
            ("fas", "fa", "Persian"),
            ("fa", "fa", "Persian"),
            ("pol", "pl", "Polish"),
            ("pl", "pl", "Polish"),
            ("rum", "ro", "Romanian"),
            ("ron", "ro", "Romanian"),
            ("ro", "ro", "Romanian"),
            ("rus", "ru", "Russian"),
            ("ru", "ru", "Russian"),
            ("slo", "sk", "Slovak"),
            ("slk", "sk", "Slovak"),
            ("sk", "sk", "Slovak"),
            ("slv", "sl", "Slovenian"),
            ("sl", "sl", "Slovenian"),
            ("srp", "sr", "Serbian"),
            ("scc", "sr", "Serbian"),
            ("sr", "sr", "Serbian"),
            ("swe", "sv", "Swedish"),
            ("sv", "sv", "Swedish"),
            ("tha", "th", "Thai"),
            ("th", "th", "Thai"),
            ("tur", "tr", "Turkish"),
            ("tr", "tr", "Turkish"),
            ("ukr", "uk", "Ukrainian"),
            ("uk", "uk", "Ukrainian"),
            ("urd", "ur", "Urdu"),
            ("ur", "ur", "Urdu"),
            ("vie", "vi", "Vietnamese"),
            ("vi", "vi", "Vietnamese"),
            ("wel", "cy", "Welsh"),
            ("cym", "cy", "Welsh"),
            ("cy", "cy", "Welsh"),
        };
        var table = new Dictionary<string, (string Language, string Label)>(
            entries.Length, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            table[entry.Code] = (entry.Language, entry.Label);
        }

        return table;
    }

    private static Dictionary<string, (string Language, string Label)> LanguageNames(
        Dictionary<string, (string Language, string Label)> languages)
    {
        var names = new Dictionary<string, (string Language, string Label)>(
            languages.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in languages.Values)
        {
            names[entry.Label] = entry;
        }

        return names;
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
