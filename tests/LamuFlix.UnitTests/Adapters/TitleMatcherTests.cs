using LamuFlix.Infrastructure.Adapters;

namespace LamuFlix.UnitTests.Adapters;

public sealed class TitleMatcherTests
{
    [Fact]
    public void Score_IdenticalTitleAndYear_IsOne()
    {
        // act
        var score = TitleMatcher.Score("Blade Runner", 1982, "Blade Runner", 1982);

        // assert
        score.ShouldNotBeNull().ShouldBe(1.0);
    }

    [Fact]
    public void Score_YearsDifferByMoreThanTolerance_IsNull()
    {
        // act
        var score = TitleMatcher.Score("The Invite", 2026, "The Invite", 2024);

        // assert
        score.ShouldBeNull();
    }

    [Fact]
    public void Score_YearsDifferByOne_AddsYearBonus()
    {
        // act
        var score = TitleMatcher.Score("Blade Runner 2049", 2017, "Blade Runner", 2016);

        // assert
        score.ShouldNotBeNull().ShouldBe(0.806, 0.001);
    }

    [Fact]
    public void Score_YearBonusOnExactTitle_IsCappedAtOne()
    {
        // act
        var score = TitleMatcher.Score("The Invite", 2026, "The Invite", 2025);

        // assert
        score.ShouldNotBeNull().ShouldBe(1.0);
    }

    [Fact]
    public void Score_OneYearUnknown_GivesNoBonus()
    {
        // act
        var score = TitleMatcher.Score("Blade Runner", null, "Blade Runner 2049", 2017);

        // assert
        score.ShouldNotBeNull().ShouldBe(0.706, 0.001);
    }

    [Theory]
    [InlineData("City of God", 2002, 2004, 0.95)]
    [InlineData("A Fistful of Dollars", 1964, 1967, 0.9)]
    public void Score_CandidateUpToThreeYearsLater_IsPenalisedPerExtraYear(string title, int queryYear, int candidateYear, double expected)
    {
        // act
        var score = TitleMatcher.Score(title, queryYear, title, candidateYear);

        // assert
        score.ShouldNotBeNull().ShouldBe(expected, 0.001);
    }

    [Fact]
    public void Score_CandidateMoreThanThreeYearsLater_IsNull()
    {
        // act
        var score = TitleMatcher.Score("Hamilton", 2016, "Hamilton", 2020);

        // assert
        score.ShouldBeNull();
    }

    [Fact]
    public void Score_CandidateMainTitleBeforeSubtitle_MatchesWithSubtitlePenalty()
    {
        // act
        var score = TitleMatcher.Score("Pokémon - The First Movie", null, "Pokémon: The First Movie - Mewtwo Strikes Back", null);

        // assert
        score.ShouldNotBeNull().ShouldBe(0.95, 0.001);
    }

    [Fact]
    public void Score_FullTitleMatch_RanksAboveMainTitleMatch()
    {
        // act
        var full = TitleMatcher.Score("Alien - Resurrection", null, "Alien - Resurrection", null);
        var mainOnly = TitleMatcher.Score("Alien", null, "Alien - Resurrection", null);

        // assert
        full.ShouldNotBeNull().ShouldBeGreaterThan(mainOnly.ShouldNotBeNull());
    }

    [Fact]
    public void Similarity_IgnoresCaseDiacriticsAndPunctuation()
    {
        // act
        var score = TitleMatcher.Similarity("AMÉLIE!", "amelie");

        // assert
        score.ShouldBe(1.0);
    }

    [Fact]
    public void Similarity_TreatsAmpersandAsAnd()
    {
        // act
        var score = TitleMatcher.Similarity("Tom & Jerry", "Tom and Jerry");

        // assert
        score.ShouldBe(1.0);
    }

    [Fact]
    public void Similarity_ComparesDigitsAsWords()
    {
        // act
        var score = TitleMatcher.Similarity("2 Days in New York", "Two Days in New York");

        // assert
        score.ShouldBe(1.0);
    }

    [Theory]
    [InlineData("Gone in Sixty Seconds", "Gone in 60 Seconds")]
    [InlineData("Twenty-Eight Days Later", "28 Days Later")]
    public void Similarity_ComparesTensWordsAsDigits(string left, string right)
    {
        // act
        var score = TitleMatcher.Similarity(left, right);

        // assert
        score.ShouldBe(1.0);
    }

    [Fact]
    public void Similarity_UnrelatedTitles_IsLow()
    {
        // act
        var score = TitleMatcher.Similarity("Dune", "Solaris");

        // assert
        score.ShouldBeLessThan(0.5);
    }

    [Fact]
    public void SearchVariants_BracketGroupsRemoved_ReturnsStrippedTitle()
    {
        // act
        var variants = TitleMatcher.SearchVariants("The Invite (2026) [1080p] [WEBRip] [5.1] [YTS.GG - YTS.BZ]");

        // assert
        variants.ShouldBe(["The Invite"]);
    }

    [Fact]
    public void SearchVariants_LeadingDigits_AddsSpelledVariant()
    {
        // act
        var variants = TitleMatcher.SearchVariants("2 Days in New York[2012]");

        // assert
        variants.ShouldBe(["2 Days in New York", "Two Days in New York"]);
    }

    [Fact]
    public void SearchVariants_DigitsAboveTwenty_AreNotSpelled()
    {
        // act
        var variants = TitleMatcher.SearchVariants("Catch 22");

        // assert
        variants.ShouldBe(["Catch 22"]);
    }

    [Fact]
    public void SearchVariants_DashSeparator_AddsColonVariant()
    {
        // act
        var variants = TitleMatcher.SearchVariants("Futurama - Into The Wild Green Yonder");

        // assert
        variants.ShouldBe(["Futurama - Into The Wild Green Yonder", "Futurama: Into The Wild Green Yonder"]);
    }

    [Theory]
    [InlineData("Gone in Sixty Seconds", "Gone in 60 Seconds")]
    [InlineData("Twenty-Eight Days Later", "28 Days Later")]
    [InlineData("Ninety Nine Homes", "99 Homes")]
    public void SearchVariants_TensWrittenAsWords_AddsDigitVariant(string title, string digitized)
    {
        // act
        var variants = TitleMatcher.SearchVariants(title);

        // assert
        variants.ShouldBe([title, digitized]);
    }

    [Fact]
    public void SearchVariants_TitleWithoutDigits_ReturnsSingleVariant()
    {
        // act
        var variants = TitleMatcher.SearchVariants("Blade Runner");

        // assert
        variants.ShouldBe(["Blade Runner"]);
    }
}
