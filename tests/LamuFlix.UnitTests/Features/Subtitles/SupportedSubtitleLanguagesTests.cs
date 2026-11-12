using System.Linq;
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
        languages.Select(language => language.Code).Distinct(System.StringComparer.OrdinalIgnoreCase).Count()
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
            .OrderBy(language => language.EnglishName, System.StringComparer.Ordinal)
            .ThenBy(language => language.Code, System.StringComparer.Ordinal)
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

    private static GetSubtitleLanguagesQueryHandler Handler(string[] preferences) =>
        new(Microsoft.Extensions.Options.Options.Create(new SubtitlesOptions { PreferredLanguages = preferences }));
}
