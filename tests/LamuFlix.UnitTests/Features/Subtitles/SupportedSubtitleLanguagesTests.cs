using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Subtitles;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Subtitles;

public sealed class SupportedSubtitleLanguagesTests
{
    private const string PortugueseBrazil = "pt-BR";

    [Fact]
    public void TryResolve_ExactCode_ReturnsTheCanonicalEntry()
    {
        // act
        var resolved = SupportedSubtitleLanguages.TryResolve(PortugueseBrazil, out var language);

        // assert
        resolved.ShouldBeTrue();
        language.ShouldNotBeNull();
        language.Code.ShouldBe(PortugueseBrazil);
        language.EnglishName.ShouldBe("Portuguese (BR)");
        language.NativeName.ShouldBe("Português (BR)");
    }

    [Theory]
    [InlineData(" pt-br ")]
    [InlineData("PT-BR")]
    [InlineData("Pt-Br")]
    public void TryResolve_CaseOrPaddingDifferences_ResolveToTheCanonicalCode(string input)
    {
        // act
        var resolved = SupportedSubtitleLanguages.TryResolve(input, out var language);

        // assert
        resolved.ShouldBeTrue();
        language.ShouldNotBeNull().Code.ShouldBe(PortugueseBrazil);
    }

    [Theory]
    [InlineData("pob")]
    [InlineData("pb")]
    public void TryResolve_ProviderAlias_ResolvesToTheCanonicalCode(string alias)
    {
        // act
        var resolved = SupportedSubtitleLanguages.TryResolve(alias, out var language);

        // assert
        resolved.ShouldBeTrue();
        language.ShouldNotBeNull().Code.ShouldBe(PortugueseBrazil);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xx-YY")]
    public void TryResolve_UnsupportedOrBlankCode_ReturnsFalse(string? input)
    {
        // act
        var resolved = SupportedSubtitleLanguages.TryResolve(input, out var language);

        // assert
        resolved.ShouldBeFalse();
        language.ShouldBeNull();
    }

    [Fact]
    public void All_CarriesUniqueCanonicalCodesWithBilingualNames()
    {
        // act
        var languages = SupportedSubtitleLanguages.All;

        // assert
        languages.Count.ShouldBeGreaterThan(20);
        languages.Select(language => language.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count()
            .ShouldBe(languages.Count);
        languages.ShouldAllBe(language =>
            language.Code.Length > 0 && language.EnglishName.Length > 0 && language.NativeName.Length > 0);
    }

    [Fact]
    public void OrderedFor_ConfiguredPreferences_LeadInConfiguredDeduplicatedOrder()
    {
        // act
        var ordered = SupportedSubtitleLanguages.OrderedFor(["en", "pt-BR", "en"]);

        // assert
        ordered[0].Code.ShouldBe("en");
        ordered[1].Code.ShouldBe(PortugueseBrazil);
        ordered.Count(language => language.Code == "en").ShouldBe(1);
    }

    [Fact]
    public void OrderedFor_RemainingEntries_AreOrdinalEnglishNameThenCode()
    {
        // act
        var ordered = SupportedSubtitleLanguages.OrderedFor(["en"]);
        var remaining = ordered.Skip(1).ToArray();

        // assert
        var expected = remaining
            .OrderBy(language => language.EnglishName, StringComparer.Ordinal)
            .ThenBy(language => language.Code, StringComparer.Ordinal)
            .Select(language => language.Code);
        remaining.Select(language => language.Code).ShouldBe(expected);
    }

    [Fact]
    public void OrderedFor_EmptyOrNullPreferences_ReturnsTheFullCatalogOrdered()
    {
        // act
        var empty = SupportedSubtitleLanguages.OrderedFor([]);
        var absent = SupportedSubtitleLanguages.OrderedFor(null);

        // assert
        empty.Count.ShouldBe(SupportedSubtitleLanguages.All.Count);
        absent.Select(language => language.Code).ShouldBe(empty.Select(language => language.Code));
    }

    [Fact]
    public void OrderedFor_UnsupportedPreference_IsIgnoredRatherThanFailing()
    {
        // act
        var ordered = SupportedSubtitleLanguages.OrderedFor(["xx-YY", "en"]);

        // assert
        ordered[0].Code.ShouldBe("en");
        ordered.Count.ShouldBe(SupportedSubtitleLanguages.All.Count);
    }

    [Theory]
    [MemberData(nameof(CatalogEntries))]
    public void TryResolve_EveryCatalogEntry_ResolvesWithBilingualNames(string code, string englishName, string nativeName)
    {
        // act
        var resolved = SupportedSubtitleLanguages.TryResolve(code, out var language);

        // assert
        resolved.ShouldBeTrue();
        language.ShouldNotBeNull();
        language.Code.ShouldBe(code);
        language.EnglishName.ShouldBe(englishName);
        language.NativeName.ShouldBe(nativeName);
    }

    [Theory]
    [MemberData(nameof(ProviderAliases))]
    public void TryResolve_EveryProviderAlias_ResolvesToTheCanonicalEntry(string alias, string canonicalCode)
    {
        // act
        var resolved = SupportedSubtitleLanguages.TryResolve(alias, out var language);

        // assert
        resolved.ShouldBeTrue();
        language.ShouldNotBeNull();
        language.Code.ShouldBe(canonicalCode);
    }

    public static TheoryData<string, string, string> CatalogEntries => new()
    {
        { "pt-BR", "Portuguese (BR)", "Português (BR)" },
        { "pt", "Portuguese", "Português" },
        { "en", "English", "English" },
        { "es", "Spanish", "Español" },
        { "sq", "Albanian", "Shqip" },
        { "de", "German", "Deutsch" },
        { "ar", "Arabic", "العربية" },
        { "hy", "Armenian", "Հայերեն" },
        { "eu", "Basque", "Euskara" },
        { "be", "Belarusian", "Беларуская" },
        { "bn", "Bengali", "বাংলা" },
        { "bs", "Bosnian", "Bosanski" },
        { "bg", "Bulgarian", "Български" },
        { "ca", "Catalan", "Català" },
        { "zh", "Chinese", "中文" },
        { "ko", "Korean", "한국어" },
        { "hr", "Croatian", "Hrvatski" },
        { "cs", "Czech", "Čeština" },
        { "da", "Danish", "Dansk" },
        { "sk", "Slovak", "Slovenčina" },
        { "sl", "Slovenian", "Slovenščina" },
        { "sr", "Serbian", "Српски" },
        { "nl", "Dutch", "Nederlands" },
        { "et", "Estonian", "Eesti" },
        { "fi", "Finnish", "Suomi" },
        { "fr", "French", "Français" },
        { "gl", "Galician", "Galego" },
        { "ka", "Georgian", "ქართული" },
        { "el", "Greek", "Ελληνικά" },
        { "he", "Hebrew", "עברית" },
        { "hi", "Hindi", "हिन्दी" },
        { "hu", "Hungarian", "Magyar" },
        { "is", "Icelandic", "Íslenska" },
        { "id", "Indonesian", "Bahasa Indonesia" },
        { "it", "Italian", "Italiano" },
        { "ja", "Japanese", "日本語" },
        { "lv", "Latvian", "Latviešu" },
        { "lt", "Lithuanian", "Lietuvių" },
        { "mk", "Macedonian", "Македонски" },
        { "ms", "Malay", "Bahasa Melayu" },
        { "no", "Norwegian", "Norsk" },
        { "fa", "Persian", "فارسی" },
        { "pl", "Polish", "Polski" },
        { "ro", "Romanian", "Română" },
        { "ru", "Russian", "Русский" },
        { "sv", "Swedish", "Svenska" },
        { "th", "Thai", "ไทย" },
        { "tr", "Turkish", "Türkçe" },
        { "uk", "Ukrainian", "Українська" },
        { "ur", "Urdu", "اردو" },
        { "vi", "Vietnamese", "Tiếng Việt" },
        { "cy", "Welsh", "Cymraeg" },
    };

    public static TheoryData<string, string> ProviderAliases => new()
    {
        { "pob", "pt-BR" },
        { "pb", "pt-BR" },
        { "por", "pt" },
        { "eng", "en" },
        { "spa", "es" },
        { "alb", "sq" },
        { "sqi", "sq" },
        { "ara", "ar" },
        { "arm", "hy" },
        { "hye", "hy" },
        { "baq", "eu" },
        { "eus", "eu" },
        { "bel", "be" },
        { "ben", "bn" },
        { "bos", "bs" },
        { "bul", "bg" },
        { "cat", "ca" },
        { "chi", "zh" },
        { "zho", "zh" },
        { "kor", "ko" },
        { "hrv", "hr" },
        { "scr", "hr" },
        { "cze", "cs" },
        { "ces", "cs" },
        { "dan", "da" },
        { "slo", "sk" },
        { "slk", "sk" },
        { "slv", "sl" },
        { "srp", "sr" },
        { "scc", "sr" },
        { "dut", "nl" },
        { "nld", "nl" },
        { "est", "et" },
        { "fin", "fi" },
        { "fre", "fr" },
        { "fra", "fr" },
        { "glg", "gl" },
        { "geo", "ka" },
        { "kat", "ka" },
        { "ger", "de" },
        { "deu", "de" },
        { "gre", "el" },
        { "ell", "el" },
        { "heb", "he" },
        { "hin", "hi" },
        { "hun", "hu" },
        { "ice", "is" },
        { "isl", "is" },
        { "ind", "id" },
        { "ita", "it" },
        { "jpn", "ja" },
        { "lav", "lv" },
        { "lit", "lt" },
        { "mac", "mk" },
        { "mkd", "mk" },
        { "may", "ms" },
        { "msa", "ms" },
        { "nor", "no" },
        { "per", "fa" },
        { "fas", "fa" },
        { "pol", "pl" },
        { "rum", "ro" },
        { "ron", "ro" },
        { "rus", "ru" },
        { "swe", "sv" },
        { "tha", "th" },
        { "tur", "tr" },
        { "ukr", "uk" },
        { "urd", "ur" },
        { "vie", "vi" },
        { "wel", "cy" },
        { "cym", "cy" },
    };
}

public sealed class GetSubtitleLanguagesQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ConfiguredPreferences_DefaultIsTheFirstResolvablePreference()
    {
        // arrange
        var handler = Handler(["pt-BR", "en"]);

        // act
        var result = await handler.HandleAsync(
            new GetSubtitleLanguagesQuery(), TestContext.Current.CancellationToken);

        // assert
        result.DefaultLanguage.ShouldBe("pt-BR");
        result.Languages[0].Code.ShouldBe("pt-BR");
        result.Languages[1].Code.ShouldBe("en");
    }

    [Fact]
    public async Task HandleAsync_EmptyPreferences_DefaultIsNullAndCatalogIsComplete()
    {
        // arrange
        var handler = Handler([]);

        // act
        var result = await handler.HandleAsync(
            new GetSubtitleLanguagesQuery(), TestContext.Current.CancellationToken);

        // assert
        result.DefaultLanguage.ShouldBeNull();
        result.Languages.Length.ShouldBe(SupportedSubtitleLanguages.All.Count);
    }

    [Fact]
    public async Task HandleAsync_PreferenceAlias_EmitsTheCanonicalCodeAsDefault()
    {
        // arrange
        var handler = Handler(["pob"]);

        // act
        var result = await handler.HandleAsync(
            new GetSubtitleLanguagesQuery(), TestContext.Current.CancellationToken);

        // assert
        result.DefaultLanguage.ShouldBe("pt-BR");
    }

    [Fact]
    public async Task HandleAsync_UnsupportedLeadingPreference_DefaultSkipsToTheFirstSupportedEntry()
    {
        // arrange
        var handler = Handler(["xx-YY", "en"]);

        // act
        var result = await handler.HandleAsync(
            new GetSubtitleLanguagesQuery(), TestContext.Current.CancellationToken);

        // assert
        result.DefaultLanguage.ShouldBe("en");
    }

    [Fact]
    public async Task HandleAsync_AllPreferencesUnsupported_DefaultIsNullAndCatalogIsComplete()
    {
        // arrange
        var handler = Handler(["xx-YY"]);

        // act
        var result = await handler.HandleAsync(
            new GetSubtitleLanguagesQuery(), TestContext.Current.CancellationToken);

        // assert
        result.DefaultLanguage.ShouldBeNull();
        result.Languages.Length.ShouldBe(SupportedSubtitleLanguages.All.Count);
    }

    [Fact]
    public async Task HandleAsync_BlankLeadingPreference_DefaultSkipsToTheFirstSupportedEntry()
    {
        // arrange
        var handler = Handler([" ", "es"]);

        // act
        var result = await handler.HandleAsync(
            new GetSubtitleLanguagesQuery(), TestContext.Current.CancellationToken);

        // assert
        result.DefaultLanguage.ShouldBe("es");
        result.Languages[0].Code.ShouldBe("es");
    }

    [Fact]
    public async Task HandleAsync_NullQuery_ThrowsArgumentNullException()
    {
        // arrange
        var handler = Handler([]);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => handler.HandleAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task HandleAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        // arrange
        var handler = Handler(["en"]);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var token = cancellation.Token;

        // act
        var act = () => handler.HandleAsync(new GetSubtitleLanguagesQuery(), token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    private static GetSubtitleLanguagesQueryHandler Handler(string[] preferences) =>
        new(Microsoft.Extensions.Options.Options.Create(new SubtitlesOptions { PreferredLanguages = preferences }));
}
