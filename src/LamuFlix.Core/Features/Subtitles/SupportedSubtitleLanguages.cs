using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LamuFlix.Core.Features.Subtitles;

public sealed record SubtitleLanguage(string Code, string EnglishName, string NativeName);

public static class SupportedSubtitleLanguages
{
    private static readonly SubtitleLanguage[] CatalogEntries =
    [
        new("pt-BR", "Portuguese (BR)", "Português (BR)"),
        new("pt", "Portuguese", "Português"),
        new("en", "English", "English"),
        new("es", "Spanish", "Español"),
        new("sq", "Albanian", "Shqip"),
        new("de", "German", "Deutsch"),
        new("ar", "Arabic", "العربية"),
        new("hy", "Armenian", "Հայերեն"),
        new("eu", "Basque", "Euskara"),
        new("be", "Belarusian", "Беларуская"),
        new("bn", "Bengali", "বাংলা"),
        new("bs", "Bosnian", "Bosanski"),
        new("bg", "Bulgarian", "Български"),
        new("ca", "Catalan", "Català"),
        new("zh", "Chinese", "中文"),
        new("ko", "Korean", "한국어"),
        new("hr", "Croatian", "Hrvatski"),
        new("cs", "Czech", "Čeština"),
        new("da", "Danish", "Dansk"),
        new("sk", "Slovak", "Slovenčina"),
        new("sl", "Slovenian", "Slovenščina"),
        new("sr", "Serbian", "Српски"),
        new("nl", "Dutch", "Nederlands"),
        new("et", "Estonian", "Eesti"),
        new("fi", "Finnish", "Suomi"),
        new("fr", "French", "Français"),
        new("gl", "Galician", "Galego"),
        new("ka", "Georgian", "ქართული"),
        new("el", "Greek", "Ελληνικά"),
        new("he", "Hebrew", "עברית"),
        new("hi", "Hindi", "हिन्दी"),
        new("hu", "Hungarian", "Magyar"),
        new("is", "Icelandic", "Íslenska"),
        new("id", "Indonesian", "Bahasa Indonesia"),
        new("it", "Italian", "Italiano"),
        new("ja", "Japanese", "日本語"),
        new("lv", "Latvian", "Latviešu"),
        new("lt", "Lithuanian", "Lietuvių"),
        new("mk", "Macedonian", "Македонски"),
        new("ms", "Malay", "Bahasa Melayu"),
        new("no", "Norwegian", "Norsk"),
        new("fa", "Persian", "فارسی"),
        new("pl", "Polish", "Polski"),
        new("ro", "Romanian", "Română"),
        new("ru", "Russian", "Русский"),
        new("sv", "Swedish", "Svenska"),
        new("th", "Thai", "ไทย"),
        new("tr", "Turkish", "Türkçe"),
        new("uk", "Ukrainian", "Українська"),
        new("ur", "Urdu", "اردو"),
        new("vi", "Vietnamese", "Tiếng Việt"),
        new("cy", "Welsh", "Cymraeg"),
    ];

    private static readonly (string Alias, string Canonical)[] Aliases =
    [
        ("pob", "pt-BR"),
        ("pb", "pt-BR"),
        ("por", "pt"),
        ("eng", "en"),
        ("spa", "es"),
        ("alb", "sq"),
        ("sqi", "sq"),
        ("ara", "ar"),
        ("arm", "hy"),
        ("hye", "hy"),
        ("baq", "eu"),
        ("eus", "eu"),
        ("bel", "be"),
        ("ben", "bn"),
        ("bos", "bs"),
        ("bul", "bg"),
        ("cat", "ca"),
        ("chi", "zh"),
        ("zho", "zh"),
        ("kor", "ko"),
        ("hrv", "hr"),
        ("scr", "hr"),
        ("cze", "cs"),
        ("ces", "cs"),
        ("dan", "da"),
        ("slo", "sk"),
        ("slk", "sk"),
        ("slv", "sl"),
        ("srp", "sr"),
        ("scc", "sr"),
        ("dut", "nl"),
        ("nld", "nl"),
        ("est", "et"),
        ("fin", "fi"),
        ("fre", "fr"),
        ("fra", "fr"),
        ("glg", "gl"),
        ("geo", "ka"),
        ("kat", "ka"),
        ("ger", "de"),
        ("deu", "de"),
        ("gre", "el"),
        ("ell", "el"),
        ("heb", "he"),
        ("hin", "hi"),
        ("hun", "hu"),
        ("ice", "is"),
        ("isl", "is"),
        ("ind", "id"),
        ("ita", "it"),
        ("jpn", "ja"),
        ("lav", "lv"),
        ("lit", "lt"),
        ("mac", "mk"),
        ("mkd", "mk"),
        ("may", "ms"),
        ("msa", "ms"),
        ("nor", "no"),
        ("per", "fa"),
        ("fas", "fa"),
        ("pol", "pl"),
        ("rum", "ro"),
        ("ron", "ro"),
        ("rus", "ru"),
        ("swe", "sv"),
        ("tha", "th"),
        ("tur", "tr"),
        ("ukr", "uk"),
        ("urd", "ur"),
        ("vie", "vi"),
        ("wel", "cy"),
        ("cym", "cy"),
    ];

    private static readonly ImmutableArray<SubtitleLanguage> Catalog = [.. CatalogEntries];

    private static readonly ImmutableDictionary<string, SubtitleLanguage> ByCode = BuildLookup();

    public static IReadOnlyList<SubtitleLanguage> All => Catalog;

    public static bool TryResolve(string? code, [NotNullWhen(true)] out SubtitleLanguage? language)
    {
        language = null;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return ByCode.TryGetValue(code.Trim(), out language);
    }

    public static IReadOnlyList<SubtitleLanguage> OrderedFor(IEnumerable<string>? preferredCodes)
    {
        var ordered = ImmutableArray.CreateBuilder<SubtitleLanguage>(Catalog.Length);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (preferredCodes is not null)
        {
            foreach (var candidate in preferredCodes)
            {
                if (TryResolve(candidate, out var language) && seen.Add(language.Code))
                {
                    ordered.Add(language);
                }
            }
        }

        ordered.AddRange(Catalog
            .Where(language => !seen.Contains(language.Code))
            .OrderBy(language => language.EnglishName, StringComparer.Ordinal)
            .ThenBy(language => language.Code, StringComparer.Ordinal));

        return ordered.ToImmutable();
    }

    private static ImmutableDictionary<string, SubtitleLanguage> BuildLookup()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, SubtitleLanguage>(StringComparer.OrdinalIgnoreCase);
        foreach (var language in Catalog)
        {
            builder[language.Code] = language;
        }

        foreach (var (alias, canonical) in Aliases)
        {
            builder[alias] = ByCanonical(canonical);
        }

        return builder.ToImmutable();
    }

    private static SubtitleLanguage ByCanonical(string code) =>
        Catalog.First(language => string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase));
}
