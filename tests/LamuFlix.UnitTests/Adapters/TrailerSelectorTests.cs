using System.Linq;
using TrailerCandidate = LamuFlix.Infrastructure.Adapters.TmdbMetadataProvider.TrailerCandidate;
using TrailerSelector = LamuFlix.Infrastructure.Adapters.TmdbMetadataProvider.TrailerSelector;

namespace LamuFlix.UnitTests.Adapters;

public sealed class TrailerSelectorTests
{
    private const string English = "en";
    private const string Portuguese = "pt";
    private const string Brazil = "BR";
    private const string UnitedStates = "US";
    private const string YouTube = "YouTube";
    private const string Trailer = "Trailer";
    private const string PtBrOfficial = "PtBrOfficl1";
    private const string PtBrUnofficial = "PtBrUnoffcl";
    private const string EnOfficial = "EnOfficial1";
    private const string EnUnofficial = "EnUnoffcl11";
    private const string OrdinalFirst = "AAAaaaaaa11";
    private const string OrdinalSecond = "BBBbbbbbb22";

    [Fact]
    public void Select_PtBrAndOriginalCandidates_ReturnsThePtBrKey()
    {
        // arrange
        TrailerCandidate[] candidates =
        [
            Candidate(EnOfficial, English, UnitedStates, official: true),
            Candidate(PtBrUnofficial, Portuguese, Brazil, official: false),
        ];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBe(PtBrUnofficial);
    }

    [Fact]
    public void Select_NonofficialPtBrAndOfficialOriginal_ReturnsTheNonofficialPtBrKey()
    {
        // arrange
        TrailerCandidate[] candidates =
        [
            Candidate(EnOfficial, English, UnitedStates, official: true),
            Candidate(PtBrUnofficial, Portuguese, Brazil, official: false),
        ];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBe(PtBrUnofficial);
    }

    [Fact]
    public void Select_OfficialAndNonofficialInTheSameTier_ReturnsTheOfficialKey()
    {
        // arrange
        TrailerCandidate[] candidates =
        [
            Candidate(EnUnofficial, English, UnitedStates, official: false),
            Candidate(EnOfficial, English, UnitedStates, official: true),
        ];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBe(EnOfficial);
    }

    [Fact]
    public void Select_EqualTierAndOfficialness_ReturnsTheOrdinalSmallestKey()
    {
        // arrange
        TrailerCandidate[] candidates =
        [
            Candidate(OrdinalSecond, English, UnitedStates, official: true),
            Candidate(OrdinalFirst, English, UnitedStates, official: true),
        ];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBe(OrdinalFirst);
    }

    [Fact]
    public void Select_ReversedInputOrder_ReturnsTheSameKey()
    {
        // arrange
        TrailerCandidate[] candidates =
        [
            Candidate(EnOfficial, English, UnitedStates, official: true),
            Candidate(PtBrUnofficial, Portuguese, Brazil, official: false),
            Candidate(OrdinalFirst, English, UnitedStates, official: false),
        ];

        // act
        var forward = TrailerSelector.Select(candidates, English);
        var backward = TrailerSelector.Select([.. candidates.Reverse()], English);

        // assert
        forward.ShouldBe(PtBrUnofficial);
        backward.ShouldBe(forward);
    }

    [Fact]
    public void Select_DuplicateKeys_KeepsTheBestRankForThatKey()
    {
        // arrange
        TrailerCandidate[] candidates =
        [
            new(OrdinalFirst, YouTube, Trailer, false, English, UnitedStates),
            new(OrdinalSecond, YouTube, Trailer, true, English, UnitedStates),
            new(OrdinalFirst, YouTube, Trailer, true, English, UnitedStates),
        ];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBe(OrdinalFirst);
    }

    [Fact]
    public void Select_KeyRepeatedAcrossLanguages_TreatsTheBetterRankingCopyAsTheOnlyCopy()
    {
        // arrange
        TrailerCandidate[] candidates =
        [
            new(OrdinalFirst, YouTube, Trailer, false, English, UnitedStates),
            new(OrdinalFirst, YouTube, Trailer, false, Portuguese, Brazil),
        ];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBe(OrdinalFirst);
    }

    [Fact]
    public void Select_MissingOriginalLanguageWithoutAPtBrCandidate_ReturnsNull()
    {
        // arrange
        TrailerCandidate[] candidates =
        [
            Candidate(EnOfficial, English, UnitedStates, official: true),
            Candidate(OrdinalFirst, English, UnitedStates, official: false),
        ];

        // act
        var selected = TrailerSelector.Select(candidates, originalLanguage: null);

        // assert
        selected.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Select_BlankOriginalLanguageWithoutAPtBrCandidate_ReturnsNull(string originalLanguage)
    {
        // arrange
        TrailerCandidate[] candidates = [Candidate(EnOfficial, English, UnitedStates, official: true)];

        // act
        var selected = TrailerSelector.Select(candidates, originalLanguage);

        // assert
        selected.ShouldBeNull();
    }

    [Fact]
    public void Select_MissingOriginalLanguageWithAPtBrCandidate_ReturnsThePtBrKey()
    {
        // arrange
        TrailerCandidate[] candidates =
        [
            Candidate(EnOfficial, English, UnitedStates, official: true),
            Candidate(PtBrUnofficial, Portuguese, Brazil, official: false),
        ];

        // act
        var selected = TrailerSelector.Select(candidates, originalLanguage: null);

        // assert
        selected.ShouldBe(PtBrUnofficial);
    }

    [Fact]
    public void Select_EmptyOrAbsentCandidateSet_ReturnsNull()
    {
        // act
        var empty = TrailerSelector.Select([], English);
        var absent = TrailerSelector.Select(candidates: null, English);

        // assert
        empty.ShouldBeNull();
        absent.ShouldBeNull();
    }

    [Theory]
    [InlineData("Vimeo", Trailer)]
    [InlineData(YouTube, "Teaser")]
    [InlineData(YouTube, "Clip")]
    [InlineData(YouTube, "Featurette")]
    [InlineData(null, Trailer)]
    [InlineData(YouTube, null)]
    public void Select_WrongSiteOrType_ReturnsNull(string? site, string? type)
    {
        // arrange
        TrailerCandidate[] candidates = [new(EnOfficial, site, type, true, English, UnitedStates)];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short")]
    [InlineData("toolongkeyvalue")]
    [InlineData("dQw4w9WgXc!")]
    [InlineData("dQw4w9WgXc/")]
    [InlineData("dQw4w9WgXc ")]
    public void Select_BlankMalformedOrUrlShapedKey_ReturnsNull(string key)
    {
        // arrange
        TrailerCandidate[] candidates = [new(key, YouTube, Trailer, true, English, UnitedStates)];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBeNull();
    }

    [Fact]
    public void Select_NullKey_ReturnsNull()
    {
        // arrange
        TrailerCandidate[] candidates = [new(null, YouTube, Trailer, true, English, UnitedStates)];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBeNull();
    }

    [Theory]
    [InlineData("YOUTUBE", "TRAILER")]
    [InlineData("youtube", "trailer")]
    [InlineData("YouTube", "Trailer")]
    [InlineData("yOuTuBe", "tRaIlEr")]
    public void Select_CaseVariantSiteAndType_RemainsEligible(string site, string type)
    {
        // arrange
        TrailerCandidate[] candidates = [new(EnOfficial, site, type, true, English, UnitedStates)];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBe(EnOfficial);
    }

    [Theory]
    [InlineData("PT", "br")]
    [InlineData("pt", "BR")]
    [InlineData("Pt", "Br")]
    [InlineData("PT", "BR")]
    public void Select_CaseVariantPortugueseAndRegion_StillQualifiesAsPtBr(string language, string region)
    {
        // arrange
        TrailerCandidate[] candidates =
        [
            Candidate(EnOfficial, English, UnitedStates, official: true),
            new(PtBrUnofficial, YouTube, Trailer, false, language, region),
        ];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBe(PtBrUnofficial);
    }

    [Theory]
    [InlineData("EN")]
    [InlineData("en")]
    [InlineData("En")]
    public void Select_CaseVariantOriginalLanguage_StillQualifiesForTheOriginalTier(string originalLanguage)
    {
        // arrange
        TrailerCandidate[] candidates = [Candidate(PtBrUnofficial, "EN", "US", official: true)];

        // act
        var selected = TrailerSelector.Select(candidates, originalLanguage);

        // assert
        selected.ShouldBe(PtBrUnofficial);
    }

    [Fact]
    public void Select_PortugueseLanguageOutsideBrazil_DoesNotQualifyForThePtBrTier()
    {
        // arrange
        TrailerCandidate[] candidates = [new(PtBrUnofficial, YouTube, Trailer, true, Portuguese, "PT")];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBeNull();
    }

    [Fact]
    public void Select_UnrelatedLanguageCandidate_IsNotASubstituteForAMissingOriginalLanguage()
    {
        // arrange
        TrailerCandidate[] candidates = [Candidate(EnOfficial, "fr", "FR", official: true)];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBeNull();
    }

    [Fact]
    public void Select_PtBrCandidateWithoutARegion_DoesNotQualifyForThePtBrTier()
    {
        // arrange
        TrailerCandidate[] candidates = [new(PtBrUnofficial, YouTube, Trailer, true, Portuguese, null)];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBeNull();
    }

    [Fact]
    public void Select_CandidatesFromBothLanguages_RanksEveryTierBeforeOfficialness()
    {
        // arrange
        TrailerCandidate[] candidates =
        [
            Candidate(EnOfficial, English, UnitedStates, official: true),
            Candidate(EnUnofficial, English, UnitedStates, official: false),
            Candidate(PtBrUnofficial, Portuguese, Brazil, official: false),
            Candidate(PtBrOfficial, Portuguese, Brazil, official: true),
        ];

        // act
        var selected = TrailerSelector.Select(candidates, English);

        // assert
        selected.ShouldBe(PtBrOfficial);
    }

    private static TrailerCandidate Candidate(string key, string? language, string? region, bool official) =>
        new(key, YouTube, Trailer, official, language, region);
}
