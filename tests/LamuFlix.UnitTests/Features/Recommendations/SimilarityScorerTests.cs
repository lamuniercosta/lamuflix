using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityScorerTests
{
    private const double Tolerance = 1e-12;
    private const string SharedTheme = "shared";
    private const string LeftRareTheme = "rare-a";
    private const string RightRareTheme = "rare-b";

    private static readonly string[] DefaultStoplist =
        ["aftercreditsstinger", "duringcreditsstinger", "sequel", "3d animation"];

    [Fact]
    public void Score_NullFirstMovie_ThrowsArgumentNullException()
    {
        // arrange
        var movie = Features();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => SimilarityScorer.Score(null!, movie, Options(), GenreTable(), NoKeywords()));

        // assert
        failure.ParamName.ShouldBe("a");
    }

    [Fact]
    public void Score_NullSecondMovie_ThrowsArgumentNullException()
    {
        // arrange
        var movie = Features();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => SimilarityScorer.Score(movie, null!, Options(), GenreTable(), NoKeywords()));

        // assert
        failure.ParamName.ShouldBe("b");
    }

    [Fact]
    public void Score_NullOptions_ThrowsArgumentNullException()
    {
        // arrange
        var movie = Features();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => SimilarityScorer.Score(movie, movie, null!, GenreTable(), NoKeywords()));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Score_NullGenreWeights_ThrowsArgumentNullException()
    {
        // arrange
        var movie = Features();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => SimilarityScorer.Score(movie, movie, Options(), null!, NoKeywords()));

        // assert
        failure.ParamName.ShouldBe("genreWeights");
    }

    [Fact]
    public void Score_NullKeywordCounts_ThrowsArgumentNullException()
    {
        // arrange
        var movie = Features();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => SimilarityScorer.Score(movie, movie, Options(), GenreTable(), (KeywordCounts)null!));

        // assert
        failure.ParamName.ShouldBe("keywordCounts");
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Score_InvalidWeightValue_ThrowsArgumentOutOfRange(double invalid)
    {
        // arrange
        var weights = Only(director: invalid);
        var movie = Features();

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => SimilarityScorer.Score(movie, movie, Options(weights), GenreTable(), NoKeywords()));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Score_InvalidWeightOnAnyScoringField_ThrowsArgumentOutOfRange()
    {
        // arrange
        var movie = Features();
        SimilarityWeights[] candidates =
        [
            new() { Collection = -1.0 },
            new() { Director = double.NaN },
            new() { Themes = double.PositiveInfinity },
            new() { Plot = double.NegativeInfinity },
            new() { Cast = -0.25 },
            new() { Genres = double.NaN },
        ];

        // act / assert
        foreach (var weights in candidates)
        {
            var failure = Should.Throw<ArgumentOutOfRangeException>(() => SimilarityScorer.Score(movie, movie, Options(weights), GenreTable(), NoKeywords()));
            failure.ParamName.ShouldBe("options");
        }
    }

    [Fact]
    public void Score_AllZeroScoringWeights_IsRejected()
    {
        // arrange
        var movie = Features(directors: ["Alpha"]);
        var weights = new SimilarityWeights { Collection = 0.0, Director = 0.0, Themes = 0.0, Plot = 0.0, Cast = 0.0, Genres = 0.0 };

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => SimilarityScorer.Score(movie, movie, Options(weights), GenreTable(), NoKeywords()));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Theory]
    [InlineData(-1L, 1L)]
    [InlineData(10L, 0L)]
    [InlineData(10L, 11L)]
    public void Score_InvalidKeywordCountSnapshot_ThrowsArgumentOutOfRange(long total, long count)
    {
        // arrange
        var movie = Features(keywordNames: ["theme"]);
        var counts = new KeywordCounts(total, new Dictionary<string, long> { ["theme"] = count });

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => SimilarityScorer.Score(movie, movie, Options(), GenreTable(), counts));

        // assert
        failure.ParamName.ShouldBe("counts");
    }

    [Fact]
    public void Score_BlankKeywordNameInSnapshot_ThrowsArgumentOutOfRange()
    {
        // arrange
        var movie = Features(keywordNames: ["theme"]);
        var counts = new KeywordCounts(10, new Dictionary<string, long> { ["  "] = 2 });

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => SimilarityScorer.Score(movie, movie, Options(), GenreTable(), counts));

        // assert
        failure.ParamName.ShouldBe("counts");
    }

    [Fact]
    public void Score_SharedCollectionId_ValuesOneWithNameReason()
    {
        // arrange
        var first = Features(collectionId: 7, collectionName: "Saga");
        var second = Features(collectionId: 7, collectionName: "Saga");

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        var collection = result.Breakdown[0];
        collection.Name.ShouldBe("Collection");
        collection.Value.ShouldBe(1.0);
        collection.Contribution.ShouldBe(3.0 / 15.0, Tolerance);
        collection.Evidence.ShouldBe(["Saga"]);
        Text(result).ShouldBe(["Same collection: Saga"]);
    }

    [Fact]
    public void Score_SharedCollectionIdWithSingleName_FallsBackToTheAvailableName()
    {
        // arrange
        var first = Features(collectionId: 7);
        var second = Features(collectionId: 7, collectionName: "Saga");

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[0].Value.ShouldBe(1.0);
        result.Breakdown[0].Evidence.ShouldBe(["Saga"]);
        Text(result).ShouldBe(["Same collection: Saga"]);
    }

    [Fact]
    public void Score_SharedCollectionIdWithoutNames_KeepsValueWithoutReason()
    {
        // arrange
        var first = Features(collectionId: 7);
        var second = Features(collectionId: 7);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[0].Value.ShouldBe(1.0);
        result.Breakdown[0].Contribution.ShouldBe(3.0 / 15.0, Tolerance);
        result.Breakdown[0].Evidence.ShouldBeEmpty();
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_DifferentCollectionIds_ValuesZeroAndEmitsNoReason()
    {
        // arrange
        var first = Features(collectionId: 42, collectionName: "First");
        var second = Features(collectionId: 43, collectionName: "Second");

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[0].Value.ShouldBe(0.0);
        result.Breakdown[0].Contribution.ShouldBe(0.0);
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_MissingCollectionIdOnEitherSide_ValuesZero()
    {
        // arrange
        var known = Features(collectionId: 42);
        var unknown = Features();

        // act
        var result = SimilarityScorer.Score(known, unknown, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[0].Value.ShouldBe(0.0);
        result.Breakdown[0].Evidence.ShouldBeEmpty();
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_SharedDirector_ValuesOneWithSharedNames()
    {
        // arrange
        var first = Features(directors: ["Alpha", "Gamma"]);
        var second = Features(directors: ["alpha", "Beta"]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        var director = result.Breakdown[1];
        director.Value.ShouldBe(1.0);
        director.Contribution.ShouldBe(3.0 / 15.0, Tolerance);
        director.Evidence.ShouldBe(["Alpha"]);
        Text(result).ShouldBe(["Same director: Alpha"]);
    }

    [Fact]
    public void Score_DisjointDirectors_ValuesZeroAndEmitsNoReason()
    {
        // arrange
        var first = Features(directors: ["Alpha"]);
        var second = Features(directors: ["Beta"]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[1].Value.ShouldBe(0.0);
        result.Breakdown[1].Contribution.ShouldBe(0.0);
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_NullAndBlankDirectorEntries_AreDiscardedBeforeMatching()
    {
        // arrange
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises safe handling
        var first = Features(directors: [null!, "   ", " Alpha "]);
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises safe handling
        var second = Features(directors: [null!, "  ", "alpha"]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[1].Value.ShouldBe(1.0);
        result.Breakdown[1].Evidence.ShouldBe(["Alpha"]);
        Text(result).ShouldBe(["Same director: Alpha"]);
    }

    [Fact]
    public void Score_SingleSharedActor_ValuesHalf()
    {
        // arrange
        var first = Features(cast: ["Actor", "Other"]);
        var second = Features(cast: ["actor"]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        var cast = result.Breakdown[4];
        cast.Value.ShouldBe(0.5);
        cast.Contribution.ShouldBe(1.0 / 15.0, Tolerance);
        cast.Evidence.ShouldBe(["Actor"]);
        Text(result).ShouldBe(["Shares cast: Actor"]);
    }

    [Fact]
    public void Score_TwoOrMoreSharedActors_ValuesOne()
    {
        // arrange
        var first = Features(cast: ["Actor", "Second", "Third"]);
        var second = Features(cast: ["actor", "SECOND"]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[4].Value.ShouldBe(1.0);
        result.Breakdown[4].Evidence.ShouldBe(["Actor", "Second"]);
        Text(result).ShouldBe(["Shares cast: Actor, Second"]);
    }

    [Fact]
    public void Score_NoSharedActors_ValuesZeroAndEmitsNoReason()
    {
        // arrange
        var first = Features(cast: ["Actor"]);
        var second = Features(cast: ["Other"]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[4].Value.ShouldBe(0.0);
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_Genres_DelegatesDirectlyToGenreWeightsSimilarity()
    {
        // arrange
        var table = GenreTable();
        var first = Features(genres: ["Drama", "Comedy"]);
        var second = Features(genres: ["Drama", "Shared"]);
        var expected = table.Similarity(first.Genres, second.Genres);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), table, NoKeywords());

        // assert
        result.Breakdown[5].Value.ShouldBe(expected, Tolerance);
        result.Breakdown[5].Evidence.ShouldBe(["Drama"]);
        Text(result).ShouldBe(["Same genres: Drama"]);
    }

    [Fact]
    public void Score_GenreOnlySimilarity_IsBoundedByTwoFifteenths()
    {
        // arrange
        var first = Features(genres: ["Drama"]);
        var second = Features(genres: ["Drama"]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Score.ShouldBe(2.0 / 15.0, Tolerance);
        result.Score.ShouldBeLessThanOrEqualTo(2.0 / 15.0);
        result.Breakdown.ShouldAllBe(entry => entry.Name == "Genre" || entry.Contribution == 0.0);
    }

    [Fact]
    public void Score_MissingKeywords_ValuesThemesZeroAndKeepsTheDefaultDenominator()
    {
        // arrange
        var first = Features(directors: ["Alpha"]);
        var second = Features(directors: ["alpha"]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[2].Value.ShouldBe(0.0);
        result.Breakdown[2].Contribution.ShouldBe(0.0);
        result.Breakdown[1].Contribution.ShouldBe(3.0 / 15.0, Tolerance);
        result.Score.ShouldBe(3.0 / 15.0, Tolerance);
        Text(result).ShouldBe(["Same director: Alpha"]);
    }

    [Fact]
    public void Score_EmptyCatalog_ValuesThemesZeroEvenWithMatchingKeywordNames()
    {
        // arrange
        var first = Features(keywordNames: ["theme"]);
        var second = Features(keywordNames: ["theme"]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), new KeywordCounts(0, new Dictionary<string, long>()));

        // assert
        result.Breakdown[2].Value.ShouldBe(0.0);
        result.Breakdown[2].Evidence.ShouldBeEmpty();
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_NullBlankAndDuplicateKeywordNames_AreTrimmedAndDeduplicated()
    {
        // arrange
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises safe handling
        var first = Features(keywordNames: [null!, "  ", " Theme ", "theme", "THEME"]);
        var second = Features(keywordNames: ["THEME"]);
        var counts = Keywords(10, ("theme", 2));

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(1.0);
        result.Breakdown[2].Evidence.ShouldBe(["theme"]);
    }

    [Fact]
    public void Score_ExplicitNullKeywordNames_MeanEmptyThemes()
    {
        // arrange
        var first = Features();
        var second = Features(keywordNames: ["theme"]);
        var counts = Keywords(10, ("theme", 2));

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(0.0);
        result.Reasons.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1L, 0.0)]
    [InlineData(2L, 1.0)]
    public void Score_DefaultMinimumKeywordMovies_ExcludesSingletonsAndKeepsCountsAtTwo(long movieCount, double expected)
    {
        // arrange
        var first = Features(keywordNames: ["theme"]);
        var second = Features(keywordNames: ["theme"]);
        var counts = Keywords(10, ("theme", movieCount));

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(expected);
    }

    [Fact]
    public void Score_CustomMinimumKeywordMovies_ExcludesCountsBelowTheThreshold()
    {
        // arrange
        var options = Options(minimumKeywordMovies: 3);
        var first = Features(keywordNames: ["theme"]);
        var second = Features(keywordNames: ["theme"]);
        var counts = Keywords(10, ("theme", 2));

        // act
        var result = SimilarityScorer.Score(first, second, options, GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(0.0);
        result.Breakdown[2].Evidence.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("sequel")]
    [InlineData("Sequel")]
    [InlineData("3d animation")]
    [InlineData("  aftercreditsstinger  ")]
    public void Score_DefaultStoplist_ExcludesMatchingSharedKeywords(string stoplisted)
    {
        // arrange
        var first = Features(keywordNames: [stoplisted]);
        var second = Features(keywordNames: [stoplisted]);
        var counted = stoplisted.Trim();
        var counts = Keywords(10, (counted, 2));

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(0.0);
        result.Breakdown[2].Evidence.ShouldBeEmpty();
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_ExplicitNullStoplist_AllowsOtherwiseStoplistedKeywords()
    {
        // arrange
        var options = Options(stoplist: []);
        var first = Features(keywordNames: ["sequel"]);
        var second = Features(keywordNames: ["sequel"]);
        var counts = Keywords(10, ("sequel", 2));

        // act
        var result = SimilarityScorer.Score(first, second, options, GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(1.0);
        result.Breakdown[2].Evidence.ShouldBe(["sequel"]);
    }

    [Fact]
    public void Score_CultureIndependentMatching_TreatsDotlessITurkishPairAsOrdinalIgnoreCase()
    {
        // arrange
        var first = Features(keywordNames: ["I"]);
        var second = Features(keywordNames: ["i"]);
        var counts = Keywords(10, ("i", 2));

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(1.0);
        result.Breakdown[2].Evidence.ShouldBe(["i"]);
    }

    [Fact]
    public void Score_Themes_UsesLogRarityWeightedCosine()
    {
        // arrange
        var first = Features(keywordNames: ["common", "rare"]);
        var second = Features(keywordNames: ["common"]);
        var counts = Keywords(100, ("common", 50), ("rare", 5));
        var common = Math.Log(100.0 / 50.0);
        var rare = Math.Log(100.0 / 5.0);
        var expected = common * common / (Math.Sqrt((common * common) + (rare * rare)) * common) / 0.35;

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(expected, Tolerance);
        result.Breakdown[2].Evidence.ShouldBe(["common"]);
    }

    [Fact]
    public void Score_ThemesEvidence_TakesThreeSharedNamesInDescendingRarity()
    {
        // arrange
        var first = Features(keywordNames: ["alpha", "beta", "gamma", "delta"]);
        var second = Features(keywordNames: ["delta", "gamma", "beta", "alpha"]);
        var counts = Keywords(100, ("alpha", 2), ("beta", 5), ("gamma", 8), ("delta", 10));

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(1.0);
        result.Breakdown[2].Evidence.ShouldBe(["alpha", "beta", "gamma"]);
        Text(result).ShouldBe(["Shared themes: alpha, beta, gamma"]);
    }

    [Fact]
    public void Score_UbiquitousOnlySharedKeywords_HaveZeroWeightAndNoReason()
    {
        // arrange
        var movie = Features(keywordNames: ["ubiquitous"]);
        var counts = Keywords(10, ("ubiquitous", 10));

        // act
        var result = SimilarityScorer.Score(movie, movie, Options(), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(0.0);
        result.Breakdown[2].Contribution.ShouldBe(0.0);
        result.Breakdown[2].Evidence.ShouldBe(["ubiquitous"]);
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_ZeroNormOnOneSide_ValuesThemesZeroWithNoReason()
    {
        // arrange
        var first = Features(keywordNames: ["theme"]);
        var second = Features(keywordNames: ["other"]);
        var counts = Keywords(10, ("theme", 2), ("other", 3));

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(0.0);
        result.Reasons.ShouldBeEmpty();
        double.IsFinite(result.Score).ShouldBeTrue();
    }

    [Fact]
    public void Score_ThemesWithDefaultCeiling_UsesDeclaredCeiling()
    {
        // arrange
        var counts = RarityFixture();
        var first = Features(keywordNames: [SharedTheme, LeftRareTheme]);
        var second = Features(keywordNames: [SharedTheme, RightRareTheme]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe((1.0 / 5.0) / 0.35, Tolerance);
    }

    [Fact]
    public void Score_ThemesAtCeiling_ValuesOne()
    {
        // arrange
        var counts = RarityFixture();
        var first = Features(keywordNames: [SharedTheme, LeftRareTheme]);
        var second = Features(keywordNames: [SharedTheme, RightRareTheme]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(themesCeiling: 0.2), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(1.0, Tolerance);
        Text(result).ShouldBe(["Shared themes: shared"]);
    }

    [Fact]
    public void Score_ThemesAtHalfCeiling_ValuesHalf()
    {
        // arrange
        var counts = RarityFixture();
        var first = Features(keywordNames: [SharedTheme, LeftRareTheme]);
        var second = Features(keywordNames: [SharedTheme, RightRareTheme]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(themesCeiling: 0.4), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(0.5, Tolerance);
    }

    [Fact]
    public void Score_ThemesAboveCeiling_CapsAtOne()
    {
        // arrange
        var counts = RarityFixture();
        var first = Features(keywordNames: [SharedTheme, LeftRareTheme]);
        var second = Features(keywordNames: [SharedTheme, RightRareTheme]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(themesCeiling: 0.1), GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBe(1.0);
    }

    [Fact]
    public void Score_BelowThemesThreshold_RetainsEvidenceAndContributionWithoutReason()
    {
        // arrange
        var counts = RarityFixture();
        var first = Features(keywordNames: [SharedTheme, LeftRareTheme]);
        var second = Features(keywordNames: [SharedTheme, RightRareTheme]);
        var options = Options(themesThreshold: 0.9);

        // act
        var result = SimilarityScorer.Score(first, second, options, GenreTable(), counts);

        // assert
        var themes = result.Breakdown[2];
        themes.Value.ShouldBe((1.0 / 5.0) / 0.35, Tolerance);
        themes.Evidence.ShouldBe(["shared"]);
        themes.Contribution.ShouldBe(3.0 * themes.Value / 15.0, Tolerance);
        result.Score.ShouldBe(themes.Contribution, Tolerance);
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_BelowGenreThreshold_RetainsEvidenceAndContributionWithoutReason()
    {
        // arrange
        var table = GenreTable(100, ("Drama", 50), ("Comedy", 90));
        var first = Features(genres: ["Drama", "Comedy"]);
        var second = Features(genres: ["Drama", "Action"]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), table, NoKeywords());

        // assert
        var genre = result.Breakdown[5];
        genre.Value.ShouldBeGreaterThan(0.0);
        genre.Value.ShouldBeLessThan(0.5);
        genre.Evidence.ShouldBe(["Drama"]);
        genre.Contribution.ShouldBeGreaterThan(0.0);
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_PositiveValueAtZeroThreshold_EmitsReason()
    {
        // arrange
        var counts = RarityFixture();
        var first = Features(keywordNames: [SharedTheme, LeftRareTheme]);
        var second = Features(keywordNames: [SharedTheme, RightRareTheme]);
        var options = Options(themesThreshold: 0.0);

        // act
        var result = SimilarityScorer.Score(first, second, options, GenreTable(), counts);

        // assert
        result.Breakdown[2].Value.ShouldBeGreaterThan(0.0);
        Text(result).ShouldBe(["Shared themes: shared"]);
    }

    [Fact]
    public void Score_ZeroThresholdNonMatch_EmitsNoReason()
    {
        // arrange
        var first = Features(directors: ["Alpha"], cast: ["Actor"]);
        var second = Features(directors: ["Beta"], cast: ["Other"]);
        var options = Options(themesThreshold: 0.0, plotThreshold: 0.0, genreThreshold: 0.0);

        // act
        var result = SimilarityScorer.Score(first, second, options, GenreTable(), NoKeywords());

        // assert
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_ValueEqualToGenreThreshold_EmitsReason()
    {
        // arrange
        var table = GenreTable(100, ("Drama", 1), ("Comedy", 90));
        var first = Features(genres: ["Drama"]);
        var second = Features(genres: ["Drama"]);
        var options = Options(genreThreshold: 1.0);

        // act
        var result = SimilarityScorer.Score(first, second, options, table, NoKeywords());

        // assert
        result.Breakdown[5].Value.ShouldBe(1.0);
        Text(result).ShouldBe(["Same genres: Drama"]);
    }

    [Fact]
    public void Score_MissingPlotEmbeddingOnEitherSide_ValuesPlotZeroAndKeepsDenominator()
    {
        // arrange
        var withPlot = Features(directors: ["Alpha"], plotEmbedding: [1f, 0f]);
        var withoutPlot = Features(directors: ["alpha"]);

        // act
        var result = SimilarityScorer.Score(withPlot, withoutPlot, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[3].Value.ShouldBe(0.0);
        result.Breakdown[1].Contribution.ShouldBe(3.0 / 15.0, Tolerance);
        result.Score.ShouldBe(3.0 / 15.0, Tolerance);
        Text(result).ShouldBe(["Same director: Alpha"]);
    }

    [Fact]
    public void Score_MismatchedPlotEmbeddingLengths_ValuesPlotZero()
    {
        // arrange
        var first = Features(plotEmbedding: [1f, 0f, 0f]);
        var second = Features(plotEmbedding: [1f, 0f]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[3].Value.ShouldBe(0.0);
        result.Reasons.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void Score_NonFinitePlotEmbedding_ValuesPlotZero(float invalid)
    {
        // arrange
        var first = Features(plotEmbedding: [1f, 0f]);
        var second = Features(plotEmbedding: [invalid, 0f]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[3].Value.ShouldBe(0.0);
        result.Reasons.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.16, 0.0)]
    [InlineData(0.2, 0.13793103448275862)]
    [InlineData(0.305, 0.5)]
    [InlineData(0.45, 1.0)]
    [InlineData(0.5, 1.0)]
    [InlineData(1.0, 1.0)]
    public void Score_PlotCosine_IsRescaledBetweenFloorAndCeiling(double cosine, double expected)
    {
        // arrange
        var first = Features(plotEmbedding: [1f, 0f]);
        var second = Features(plotEmbedding: EmbeddingFor(cosine));

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[3].Value.ShouldBe(expected, 1e-6);
    }

    [Fact]
    public void Score_PlotAboveDisplayThreshold_EmitsStoryReasonInTableOrder()
    {
        // arrange
        var first = Features(plotEmbedding: [1f, 0f], directors: ["Alpha"]);
        var second = Features(plotEmbedding: EmbeddingFor(0.34), directors: ["Alpha"]);
        var expected = (0.34 - 0.16) / (0.45 - 0.16);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[3].Value.ShouldBe(expected, 1e-6);
        result.Breakdown[3].Evidence.ShouldBeEmpty();
        result.Breakdown[3].Contribution.ShouldBe(2.0 * expected / 15.0, 1e-6);
        Text(result).ShouldBe(["Same director: Alpha", "Similar story"]);
    }

    [Fact]
    public void Score_PlotBelowDisplayThreshold_RetainsContributionWithoutReason()
    {
        // arrange
        var first = Features(plotEmbedding: [1f, 0f]);
        var second = Features(plotEmbedding: EmbeddingFor(0.25));

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());

        // assert
        result.Breakdown[3].Value.ShouldBeGreaterThan(0.0);
        result.Breakdown[3].Value.ShouldBeLessThan(0.5);
        result.Breakdown[3].Contribution.ShouldBeGreaterThan(0.0);
        result.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Score_CustomPlotBounds_ChangeTheRescaleWindow()
    {
        // arrange
        var first = Features(plotEmbedding: [1f, 0f]);
        var second = Features(plotEmbedding: EmbeddingFor(0.5));
        var options = Options(plotFloor: 0.0, plotCeiling: 1.0);

        // act
        var result = SimilarityScorer.Score(first, second, options, GenreTable(), NoKeywords());

        // assert
        result.Breakdown[3].Value.ShouldBe(0.5, 1e-6);
    }

    [Fact]
    public void Score_AllSixCriteriaMatched_EmitsReasonsInTableOrder()
    {
        // arrange
        var counts = Keywords(10, ("theme", 2));
        var first = Features(
            genres: ["Drama"],
            directors: ["Director"],
            cast: ["Actor A", "Actor B"],
            keywordNames: ["theme"],
            collectionId: 7,
            collectionName: "Collection",
            plotEmbedding: [1f, 0f]);
        var second = Features(
            genres: ["Drama"],
            directors: ["Director"],
            cast: ["Actor A", "Actor B"],
            keywordNames: ["theme"],
            collectionId: 7,
            collectionName: "Collection",
            plotEmbedding: [1f, 0f]);

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);

        // assert
        Text(result).ShouldBe(
        [
            "Same collection: Collection",
            "Same director: Director",
            "Shared themes: theme",
            "Similar story",
            "Shares cast: Actor A, Actor B",
            "Same genres: Drama",
        ]);
        result.Reasons.ShouldAllBe(reason => reason.Kind == ReasonKind.Structural);
    }

    [Fact]
    public void Score_Reasons_OmitInternalIdentifiers()
    {
        // arrange
        var first = Features(directors: ["Alpha"], metaScore: 42, fullPlot: "plot-secret-0001");
        var second = Features(directors: ["alpha"], metaScore: 13, fullPlot: "plot-secret-0002");

        // act
        var result = SimilarityScorer.Score(first, second, Options(), GenreTable(), NoKeywords());
        var texts = Text(result);

        // assert
        texts.ShouldBe(["Same director: Alpha"]);
        texts.ShouldAllBe(text =>
            !text.Contains("42", StringComparison.Ordinal)
            && !text.Contains("plot-secret-0001", StringComparison.Ordinal)
            && !text.Contains("plot-secret-0002", StringComparison.Ordinal));
    }

    [Fact]
    public void Score_RepeatedCalls_AreDeterministicAndDoNotMutateInputs()
    {
        // arrange
        var first = Features(genres: ["Drama"], directors: ["Alpha"], keywordNames: ["theme"], collectionId: 7, collectionName: "Saga");
        var second = Features(genres: ["Drama"], directors: ["alpha"], keywordNames: ["theme"], collectionId: 7, collectionName: "Saga");
        var options = Options();
        var firstBefore = first;
        var optionsBefore = options;

        // act
        var forward = SimilarityScorer.Score(first, second, options, GenreTable(), Keywords(10, ("theme", 2)));
        var repeated = SimilarityScorer.Score(first, second, options, GenreTable(), Keywords(10, ("theme", 2)));

        // assert
        repeated.Score.ShouldBe(forward.Score);
        Text(repeated).ShouldBe(Text(forward));
        first.ShouldBe(firstBefore);
        options.ShouldBe(optionsBefore);
    }

    [Fact]
    public void Score_ScoreIsSymmetricForReversedPairs()
    {
        // arrange
        var first = Features(directors: ["Alpha"], cast: ["Actor"], genres: ["Drama"], keywordNames: ["theme"]);
        var second = Features(directors: ["Alpha"], cast: ["Other"], genres: ["Comedy"], keywordNames: ["other"]);
        var counts = Keywords(10, ("theme", 2), ("other", 3));

        // act
        var forward = SimilarityScorer.Score(first, second, Options(), GenreTable(), counts);
        var reverse = SimilarityScorer.Score(second, first, Options(), GenreTable(), counts);

        // assert
        forward.Score.ShouldBe(reverse.Score, Tolerance);
        forward.Breakdown.Select(entry => entry.Name).ShouldBe(reverse.Breakdown.Select(entry => entry.Name));
    }

    [Fact]
    public void BuildEvidence_FiveMatchedCastNames_FormatsFirstThreeWithOmittedCountAndKeepsEveryName()
    {
        // arrange
        var similarity = Breakdown(("Cast", ["Name One", "Name Two", "Name Three", "Name Four", "Name Five"]));

        // act
        var evidence = SimilarityScorer.BuildEvidence(similarity, Options());

        // assert
        var item = evidence.ShouldHaveSingleItem();
        item.Criterion.ShouldBe("Cast");
        item.Text.ShouldBe("Shared cast: Name One, Name Two, Name Three +2");
        item.MatchedNames.ShouldBe(["Name One", "Name Two", "Name Three", "Name Four", "Name Five"]);
    }

    [Fact]
    public void BuildEvidence_PlotWithoutNames_EmitsLabelOnlyEvidenceWithNoMatchedNames()
    {
        // arrange
        var similarity = Breakdown(("Plot", []));

        // act
        var item = SimilarityScorer.BuildEvidence(similarity, Options()).ShouldHaveSingleItem();

        // assert
        item.Criterion.ShouldBe("Plot");
        item.Text.ShouldBe("Similar story");
        item.MatchedNames.ShouldBeEmpty();
    }

    [Fact]
    public void BuildEvidence_CriterionWithNoNamesOtherThanPlot_IsOmitted()
    {
        // arrange
        var similarity = Breakdown(("Cast", []), ("Director", ["Ada"]), ("Themes", []));

        // act
        var evidence = SimilarityScorer.BuildEvidence(similarity, Options());

        // assert
        evidence.Select(item => item.Criterion).ShouldBe(["Director"]);
    }

    [Fact]
    public void BuildEvidence_ThreeNames_DisplaysEveryNameWithoutOmittedCountSuffix()
    {
        // arrange
        var similarity = Breakdown(("Cast", ["First", "Second", "Third"]));

        // act
        var item = SimilarityScorer.BuildEvidence(similarity, Options()).ShouldHaveSingleItem();

        // assert
        item.Text.ShouldBe("Shared cast: First, Second, Third");
        item.MatchedNames.ShouldBe(["First", "Second", "Third"]);
    }

    [Fact]
    public void BuildEvidence_TenNames_CapsDisplayAtThreeAndCountsOmittedNames()
    {
        // arrange
        string[] names = ["N1", "N2", "N3", "N4", "N5", "N6", "N7", "N8", "N9", "N10"];
        var similarity = Breakdown(("Cast", names));

        // act
        var item = SimilarityScorer.BuildEvidence(similarity, Options()).ShouldHaveSingleItem();

        // assert
        item.Text.ShouldBe("Shared cast: N1, N2, N3 +7");
        item.MatchedNames.ShouldBe(names);
    }

    [Fact]
    public void BuildEvidence_ControlCharacters_AreRemovedFromDisplayTextAtTheDisplayBoundary()
    {
        // arrange
        var similarity = Breakdown(("Director", ["Ada\u0000Lovelace\u001f", "  Grace\tHopper "]));

        // act
        var item = SimilarityScorer.BuildEvidence(similarity, Options()).ShouldHaveSingleItem();

        // assert
        item.Text.ShouldBe("Same director: Ada Lovelace, Grace Hopper");
        item.MatchedNames.ShouldBe(["Ada Lovelace", "Grace Hopper"]);
    }

    [Fact]
    public void BuildEvidence_MixedCriteria_OrdersStrongestFirstWithTableOrderTieBreak()
    {
        // arrange
        var similarity = new SimilarityResult(0.5, ImmutableList<Reason>.Empty)
        {
            Breakdown =
            [
                Contribution("Collection", ["Franchise"], 0.2),
                Contribution("Director", ["Ada"], 0.2),
                Contribution("Themes", ["space"], 0.05),
                Contribution("Plot", [], 0.5),
                Contribution("Cast", ["Ada"], 0.1),
                Contribution("Genre", ["Drama"], 0.2),
            ],
        };

        // act
        var evidence = SimilarityScorer.BuildEvidence(similarity, Options());

        // assert
        evidence.Select(item => item.Criterion)
            .ShouldBe(["Plot", "Collection", "Director", "Genre", "Cast", "Themes"]);
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool BuildEvidence_MatchedNames_PreserveEveryNameInOrder(int[] seeds)
    {
        // arrange
        var names = Names([.. seeds.Select(seed => (long)seed)]);
        var similarity = Breakdown(("Cast", names));

        // act
        var evidence = SimilarityScorer.BuildEvidence(similarity, Options());

        // assert
        return names.Length == 0
            ? evidence.Count == 0
            : evidence.Single().MatchedNames.SequenceEqual(names, StringComparer.Ordinal);
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool BuildEvidence_MoreThanThreeNames_CapsDisplayAtThreeAndCountsTheOmittedNames(int seed, int countSeed)
    {
        // arrange
        var omitted = Math.Abs((long)countSeed % 20) + 1;
        var names = Names(Range(seed, (int)omitted + 3));
        var similarity = Breakdown(("Cast", names));

        // act
        var item = SimilarityScorer.BuildEvidence(similarity, Options()).Single();

        // assert
        return item.Text == $"Shared cast: {names[0]}, {names[1]}, {names[2]} +{omitted}"
            && item.MatchedNames.Count == names.Length;
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool BuildEvidence_UpToThreeNames_DisplaysEveryNameWithoutOmittedCount(int seed, int countSeed)
    {
        // arrange
        var count = (int)Math.Abs((long)countSeed % 3) + 1;
        var names = Names(Range(seed, count));
        var similarity = Breakdown(("Cast", names));

        // act
        var item = SimilarityScorer.BuildEvidence(similarity, Options()).Single();

        // assert
        return item.Text == $"Shared cast: {string.Join(", ", names)}"
            && item.MatchedNames.SequenceEqual(names, StringComparer.Ordinal);
    }

    private static long[] Range(int seed, int count) =>
        [.. Enumerable.Range(0, count).Select(index => (long)seed + index)];

    private static string[] Names(IEnumerable<long> seeds) =>
        [.. seeds.Select(seed => $"Name {Math.Abs(seed)}")];

    private static SimilarityResult Breakdown(params (string Name, string[] Evidence)[] entries) =>
        new(0.5, ImmutableList<Reason>.Empty)
        {
            Breakdown = [.. entries.Select(entry => Contribution(entry.Name, entry.Evidence, 0.1))],
        };

    private static CriterionContribution Contribution(string name, string[] evidence, double contribution) =>
        new(name, 1.0, 1.0, contribution) { Evidence = [.. evidence] };

    private static string[] Text(SimilarityResult result) =>
        [.. result.Reasons.Select(reason => reason.Text)];

    private static float[] EmbeddingFor(double cosine)
    {
        var sine = Math.Sqrt(Math.Max(0.0, 1.0 - (cosine * cosine)));
        return [(float)cosine, (float)sine];
    }

    private static SimilarityWeights Only(
        double collection = 0.0,
        double director = 0.0,
        double themes = 0.0,
        double plot = 0.0,
        double cast = 0.0,
        double genres = 0.0) =>
        new()
        {
            Collection = collection,
            Director = director,
            Themes = themes,
            Plot = plot,
            Cast = cast,
            Genres = genres,
        };

    private static RecommendationOptions Options(
        SimilarityWeights? weights = null,
        double themesCeiling = 0.35,
        double plotFloor = 0.16,
        double plotCeiling = 0.45,
        double themesThreshold = 0.3,
        double plotThreshold = 0.5,
        double genreThreshold = 0.5,
        int minimumKeywordMovies = 2,
        IReadOnlyList<string>? stoplist = null) =>
        new()
        {
            Weights = weights ?? new SimilarityWeights(),
            ThemesCeiling = themesCeiling,
            PlotFloor = plotFloor,
            PlotCeiling = plotCeiling,
            ThemesDisplayThreshold = themesThreshold,
            PlotDisplayThreshold = plotThreshold,
            GenreDisplayThreshold = genreThreshold,
            MinimumKeywordMovies = minimumKeywordMovies,
            ThemeStoplist = stoplist ?? DefaultStoplist,
        };

    private static KeywordCounts NoKeywords() => new(0, new Dictionary<string, long>());

    private static KeywordCounts Keywords(long totalMovies, params (string Name, long Count)[] entries) =>
        new(totalMovies, entries.ToDictionary(entry => entry.Name, entry => entry.Count, StringComparer.Ordinal));

    private static KeywordCounts RarityFixture() =>
        Keywords(1024, (SharedTheme, 512), (LeftRareTheme, 256), (RightRareTheme, 256));

    private static GenreWeights GenreTable() =>
        GenreTable(100, ("Drama", 1), ("Comedy", 90), ("Shared", 50));

    private static GenreWeights GenreTable(long totalMovies, params (string Name, long Count)[] entries) =>
        GenreWeights.Calculate(
            new GenreCounts(totalMovies, entries.ToDictionary(entry => entry.Name, entry => entry.Count, StringComparer.Ordinal)));

    private static MovieFeatures Features(
        IEnumerable<string>? genres = null,
        IEnumerable<string>? directors = null,
        IEnumerable<string>? cast = null,
        IEnumerable<string>? keywordNames = null,
        int? collectionId = null,
        string? collectionName = null,
        IEnumerable<float>? plotEmbedding = null,
        int? metaScore = null,
        string? fullPlot = null) =>
        new(
            genres: genres,
            directors: directors,
            cast: cast,
            metaScore: metaScore,
            fullPlot: fullPlot,
            collectionId: collectionId,
            keywordNames: keywordNames,
            collectionName: collectionName,
            plotEmbedding: plotEmbedding);
}
