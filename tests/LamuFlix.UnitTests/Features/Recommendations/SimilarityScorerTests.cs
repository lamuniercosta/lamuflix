using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityScorerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Score_NullFirstMovie_ThrowsArgumentNullException()
    {
        // arrange
        var weights = Only(director: 1.0);
        var movie = Features();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => SimilarityScorer.Score(null!, movie, weights, GenreTable()));

        // assert
        failure.ParamName.ShouldBe("a");
    }

    [Fact]
    public void Score_NullSecondMovie_ThrowsArgumentNullException()
    {
        // arrange
        var weights = Only(director: 1.0);
        var movie = Features();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => SimilarityScorer.Score(movie, null!, weights, GenreTable()));

        // assert
        failure.ParamName.ShouldBe("b");
    }

    [Fact]
    public void Score_NullWeights_ThrowsArgumentNullException()
    {
        // arrange
        var movie = Features();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => SimilarityScorer.Score(movie, movie, null!, GenreTable()));

        // assert
        failure.ParamName.ShouldBe("weights");
    }

    [Fact]
    public void Score_NullGenreWeights_ThrowsArgumentNullException()
    {
        // arrange
        var movie = Features();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => SimilarityScorer.Score(movie, movie, Only(), null!));

        // assert
        failure.ParamName.ShouldBe("genreWeights");
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
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => SimilarityScorer.Score(movie, movie, weights, GenreTable()));

        // assert
        failure.ParamName.ShouldBe("weights");
    }

    [Fact]
    public void Score_InvalidWeightOnAnyOfSevenFields_ThrowsArgumentOutOfRange()
    {
        // arrange
        var movie = Features();
        SimilarityWeights[] candidates =
        [
            new() { Director = -1.0 },
            new() { Writers = double.NaN },
            new() { Cast = double.PositiveInfinity },
            new() { Genres = double.NegativeInfinity },
            new() { YearProximity = -0.25 },
            new() { RuntimeProximity = double.NaN },
            new() { RatingBoost = double.PositiveInfinity },
        ];

        // act / assert
        foreach (var weights in candidates)
        {
            var failure = Should.Throw<ArgumentOutOfRangeException>(() => SimilarityScorer.Score(movie, movie, weights, GenreTable()));
            failure.ParamName.ShouldBe("weights");
        }
    }

    [Fact]
    public void Score_IsolatedDirectorOverlap_UsesJaccardRatioUnderOrdinalIgnoreCase()
    {
        // arrange
        var first = Features(directors: ["Alpha", "Beta", "Gamma"]);
        var second = Features(directors: ["alpha", "Delta"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(0.25, 1e-12);
    }

    [Fact]
    public void Score_IsolatedWritersOverlap_UsesJaccardRatio()
    {
        // arrange
        var first = Features(writers: ["One", "Two"]);
        var second = Features(writers: ["two", "Three"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(writers: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(1.0 / 3.0, 1e-12);
    }

    [Fact]
    public void Score_IsolatedCastOverlap_UsesJaccardRatio()
    {
        // arrange
        var first = Features(cast: ["A", "B", "C", "D"]);
        var second = Features(cast: ["b", "c"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(cast: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(0.5, 1e-12);
    }

    [Fact]
    public void Score_MissingDirectorsOnOneSide_ExcludesDirectorWeightFromDenominator()
    {
        // arrange
        var first = Features(cast: ["Actor"]);
        var second = Features(directors: ["Someone"], cast: ["actor"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 3.0, cast: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(1.0, 1e-12);
    }

    [Fact]
    public void Score_DisjointDirectors_KeepWeightAndContributeZero()
    {
        // arrange
        var first = Features(directors: ["Alpha"], cast: ["Actor"]);
        var second = Features(directors: ["Beta"], cast: ["actor"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 3.0, cast: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(0.25, 1e-12);
    }

    [Fact]
    public void Score_ZeroWeightTermPositiveSimilarity_IsIgnored()
    {
        // arrange
        var first = Features(directors: ["Alpha"], cast: ["Actor"]);
        var second = Features(directors: ["alpha"], cast: ["Actor"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 0.0, cast: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(1.0, 1e-12);
    }

    [Fact]
    public void Score_AllWeightsZeroAndMismatchedProfiles_ReturnsZero()
    {
        // arrange
        var first = Features(directors: ["Alpha"]);
        var second = Features(directors: ["Beta"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(), GenreTable());

        // assert
        result.Score.ShouldBe(0.0);
    }

    [Fact]
    public void Score_MissingGenresOnOneSide_ExcludesGenreWeightFromDenominator()
    {
        // arrange
        var first = Features(cast: ["Actor"]);
        var second = Features(genres: ["Drama"], cast: ["actor"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(genres: 3.0, cast: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(1.0, 1e-12);
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
        var result = SimilarityScorer.Score(first, second, Only(genres: 1.0), table);

        // assert
        result.Score.ShouldBe(expected);
    }

    [Fact]
    public void Score_GenreCaseVariants_AreShared()
    {
        // arrange
        var first = Features(genres: ["Drama", "Comedy"]);
        var second = Features(genres: ["drama", "comedy"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(genres: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(1.0);
    }

    [Fact]
    public void Score_UnknownGenresOnBothSides_UseDefaultWeightAndScoreOne()
    {
        // arrange
        var first = Features(genres: ["Documentary"]);
        var second = Features(genres: ["documentary"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(genres: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(1.0);
    }

    [Fact]
    public void Score_NullAndBlankGenreMembers_AreSafeAndIgnored()
    {
        // arrange
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the safe handling
        var first = Features(genres: [null!, "Drama", "   "]);
        var second = Features(genres: ["drama", ""]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(genres: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(1.0);
    }

    [Fact]
    public void Score_BothGenreSetsEmpty_ExcludesGenreTerm()
    {
        // arrange
        var first = Features(genres: []);
        var second = Features(genres: []);

        // act
        var result = SimilarityScorer.Score(first, second, Only(genres: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(0.0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(25)]
    public void Score_YearProximity_AbsoluteYearDistanceUsesExpDecay(int distance)
    {
        // arrange
        var first = Features(releaseYear: 2000);
        var second = Features(releaseYear: 2000 + distance);
        var expected = Math.Exp(-0.1 * distance);

        // act
        var result = SimilarityScorer.Score(first, second, Only(year: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(expected, 1e-12);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    public void Score_RuntimeProximity_AbsoluteMinuteDistanceUsesExpDecay(int distance)
    {
        // arrange
        var first = Features(runtime: 90);
        var second = Features(runtime: 90 + distance);
        var expected = Math.Exp(-0.01 * distance);

        // act
        var result = SimilarityScorer.Score(first, second, Only(runtime: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(expected, 1e-12);
    }

    [Theory]
    [InlineData(8.0, 8.0, 1.0)]
    [InlineData(7.0, 9.5, 0.75)]
    [InlineData(0.0, 10.0, 0.0)]
    [InlineData(5.0, 0.0, 0.5)]
    public void Score_RatingProximity_TenPointScaleDifference(double firstRating, double secondRating, double expected)
    {
        // arrange
        var first = Features(imdbRating: (decimal)firstRating);
        var second = Features(imdbRating: (decimal)secondRating);

        // act
        var result = SimilarityScorer.Score(first, second, Only(rating: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(expected, 1e-12);
    }

    [Fact]
    public void Score_AllZeroWeightsIdenticalNonemptyProfiles_ReturnsExactOne()
    {
        // arrange
        var movie = Features(directors: ["Alpha"], releaseYear: 2000);

        // act
        var result = SimilarityScorer.Score(movie, movie, Only(), GenreTable());

        // assert
        result.Score.ShouldBe(1.0);
    }

    [Fact]
    public void Score_MixedZeroAndPositiveWeights_OnlyPositiveWeightTermCounts()
    {
        // arrange
        var first = Features(directors: ["Alpha", "Beta"], releaseYear: 2000);
        var second = Features(directors: ["alpha"], releaseYear: 2025);

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 2.0, year: 0.0), GenreTable());

        // assert
        result.Score.ShouldBe(0.5, 1e-12);
    }

    [Fact]
    public void Score_BothProfilesFullyEmpty_ReturnsZero()
    {
        // arrange
        var empty = new MovieFeatures();

        // act
        var result = SimilarityScorer.Score(empty, empty, Only(director: 1.0, year: 1.0), GenreTable());

        // assert
        result.Score.ShouldBe(0.0);
    }

    [Fact]
    public void Score_IdenticalSevenFieldProfiles_ReturnsExactOne()
    {
        // arrange
        var movie = Features(
            genres: ["Drama", "Comedy"],
            directors: ["Director"],
            writers: ["Writer"],
            cast: ["Actor"],
            releaseYear: 2000,
            runtime: 120,
            imdbRating: 8.5m);

        // act
        var result = SimilarityScorer.Score(movie, movie, new SimilarityWeights(), GenreTable());

        // assert
        result.Score.ShouldBe(1.0);
    }

    [Fact]
    public void Score_AllZeroWeightsProfilesDifferingOnlyInNonScoredFields_ReturnsExactOne()
    {
        // arrange
        var first = Features(directors: ["Alpha"], metaScore: 10, fullPlot: "First");
        var second = Features(directors: ["alpha"], metaScore: 90, fullPlot: "Second");

        // act
        var result = SimilarityScorer.Score(first, second, Only(), GenreTable());

        // assert
        result.Score.ShouldBe(1.0);
    }

    [Fact]
    public void Score_EveryEligiblePositiveWeightTermOne_ReturnsExactOneDespiteZeroWeightMismatch()
    {
        // arrange
        var first = Features(directors: ["Alpha"], runtime: 90);
        var second = Features(directors: ["alpha"], runtime: 200);

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 2.0, runtime: 0.0), GenreTable());

        // assert
        result.Score.ShouldBe(1.0);
    }

    [Fact]
    public void Score_MaxValueWeightsOnTwoTerms_StayFiniteAndUseScaledNormalization()
    {
        // arrange
        var first = Features(releaseYear: 2000, runtime: 100);
        var second = Features(releaseYear: 2010, runtime: 200);
        var weights = new SimilarityWeights
        {
            Director = 0.0,
            Writers = 0.0,
            Cast = 0.0,
            Genres = 0.0,
            YearProximity = double.MaxValue,
            RuntimeProximity = double.MaxValue,
            RatingBoost = 0.0,
        };
        var expected = Math.Exp(-1.0);

        // act
        var result = SimilarityScorer.Score(first, second, weights, GenreTable());

        // assert
        double.IsFinite(result.Score).ShouldBeTrue();
        result.Score.ShouldBe(expected, 1e-12);
    }

    [Fact]
    public void Score_TinyWeightAgainstMaxWeight_UnderflowsWithoutChangingScore()
    {
        // arrange
        var first = Features(releaseYear: 1900, runtime: 100);
        var second = Features(releaseYear: 2000, runtime: 100);
        var weights = new SimilarityWeights
        {
            Director = 0.0,
            Writers = 0.0,
            Cast = 0.0,
            Genres = 0.0,
            YearProximity = double.MaxValue,
            RuntimeProximity = double.Epsilon,
            RatingBoost = 0.0,
        };
        var expected = Math.Exp(-10.0);

        // act
        var result = SimilarityScorer.Score(first, second, weights, GenreTable());

        // assert
        double.IsFinite(result.Score).ShouldBeTrue();
        result.Score.ShouldBe(expected, 1e-12);
    }

    [Fact]
    public void Score_MultipleEligibleTerms_EmitsReasonsInFixedOrder()
    {
        // arrange
        var first = Features(
            genres: ["Drama", "Comedy"],
            directors: ["Alpha"],
            writers: ["One"],
            cast: ["Actor"],
            releaseYear: 2000,
            runtime: 100,
            imdbRating: 8.0m);
        var second = Features(
            genres: ["drama", "Shared"],
            directors: ["alpha"],
            writers: ["one"],
            cast: ["actor"],
            releaseYear: 2005,
            runtime: 110,
            imdbRating: 7.5m);

        // act
        var result = SimilarityScorer.Score(first, second, new SimilarityWeights(), GenreTable());

        // assert
        Text(result).ShouldBe(
        [
            "Same director: Alpha",
            "Shared writers: One",
            "Features Actor",
            "Shares Drama",
            "Similar release years",
            "Similar runtime",
            "Similar rating",
        ]);
    }

    [Fact]
    public void Score_ZeroWeightTermWithPositiveSimilarity_EmitsNoReasonForThatTerm()
    {
        // arrange
        var first = Features(directors: ["Alpha"], cast: ["Actor"]);
        var second = Features(directors: ["alpha"], cast: ["Actor"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 0.0, cast: 1.0), GenreTable());

        // assert
        Text(result).ShouldBe(["Features Actor"]);
    }

    [Fact]
    public void Score_DisjointSetTerm_EmitsNoReasonForThatTerm()
    {
        // arrange
        var first = Features(directors: ["Alpha"], cast: ["Actor"]);
        var second = Features(directors: ["Beta"], cast: ["actor"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 1.0, cast: 1.0), GenreTable());

        // assert
        Text(result).ShouldBe(["Features Actor"]);
    }

    [Fact]
    public void Score_CaseVariantSharedTokens_EmitIdenticalReasonsUnderSwap()
    {
        // arrange
        var first = Features(directors: ["alpha", "BETA"]);
        var second = Features(directors: ["Alpha", "beta"]);
        var weights = Only(director: 1.0);

        // act
        var forward = SimilarityScorer.Score(first, second, weights, GenreTable());
        var reverse = SimilarityScorer.Score(second, first, weights, GenreTable());

        // assert
        Text(forward).ShouldBe(["Same director: Alpha, BETA"]);
        Text(reverse).ShouldBe(Text(forward));
    }

    [Fact]
    public void Score_NullAndBlankSharedTokens_AreOmittedFromLabel()
    {
        // arrange
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises safe display handling
        var first = Features(directors: [null!, "  ", "Alpha"]);
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises safe display handling
        var second = Features(directors: [null!, "  ", "alpha"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 1.0), GenreTable());

        // assert
        Text(result).ShouldBe(["Same director: Alpha"]);
    }

    [Fact]
    public void Score_AllSharedSetTokensSanitizeEmpty_UsesGenericFallbacks()
    {
        // arrange
        const string control = "\u0001";
        var weights = Only(director: 1.0, writers: 1.0, cast: 1.0, genres: 1.0);

        // act
        var directors = SimilarityScorer.Score(
            Features(directors: [control]),
            Features(directors: [control]),
            weights,
            GenreTable());
        var writers = SimilarityScorer.Score(
            Features(writers: [control]),
            Features(writers: [control]),
            weights,
            GenreTable());
        var cast = SimilarityScorer.Score(
            Features(cast: [control]),
            Features(cast: [control]),
            weights,
            GenreTable());
        var genres = SimilarityScorer.Score(
            Features(genres: [control]),
            Features(genres: [control]),
            weights,
            GenreTable());

        // assert
        Text(directors).ShouldBe(["Shared director"]);
        Text(writers).ShouldBe(["Shared writers"]);
        Text(cast).ShouldBe(["Shared cast"]);
        Text(genres).ShouldBe(["Shared genres"]);
    }

    [Fact]
    public void Score_ControlCharactersInSharedTokens_AreReplacedWithSpacesAndTrimmed()
    {
        // arrange
        var first = Features(directors: ["\u0001Al\u0002pha\u0003"]);
        var second = Features(directors: ["\u0001Al\u0002pha\u0003"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 1.0), GenreTable());

        // assert
        Text(result).ShouldBe(["Same director: Al pha"]);
    }

    [Fact]
    public void Score_MarkupLikeTokens_AreEmittedAsPlainText()
    {
        // arrange
        var first = Features(directors: ["<b>Alpha</b>"], writers: ["*Beta*"]);
        var second = Features(directors: ["<b>alpha</b>"], writers: ["*beta*"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 1.0, writers: 1.0), GenreTable());

        // assert
        Text(result).ShouldBe(["Same director: <b>Alpha</b>", "Shared writers: *Beta*"]);
    }

    [Fact]
    public void Score_ReasonTexts_ContainNoInternalIdentifiers()
    {
        // arrange
        var first = Features(directors: ["Alpha"], metaScore: 42, fullPlot: "plot-secret-0001");
        var second = Features(directors: ["alpha"], metaScore: 13, fullPlot: "plot-secret-0002");

        // act
        var result = SimilarityScorer.Score(first, second, Only(director: 1.0), GenreTable());
        var texts = Text(result);

        // assert
        texts.ShouldBe(["Same director: Alpha"]);
        texts.ShouldAllBe(text =>
            !text.Contains("42", StringComparison.Ordinal)
            && !text.Contains("plot-secret-0001", StringComparison.Ordinal)
            && !text.Contains("plot-secret-0002", StringComparison.Ordinal));
    }

    [Fact]
    public void Score_ZeroWeightIdentity_EmitsSingleIdentityReason()
    {
        // arrange
        var movie = Features(directors: ["Alpha"], releaseYear: 2000);

        // act
        var result = SimilarityScorer.Score(movie, movie, Only(), GenreTable());

        // assert
        result.Score.ShouldBe(1.0);
        Text(result).ShouldBe(["Identical feature profile"]);
    }

    [Fact]
    public void Score_ZeroWeightMismatchedProfiles_EmitsNoReasons()
    {
        // arrange
        var first = Features(directors: ["Alpha"]);
        var second = Features(directors: ["Beta"]);

        // act
        var result = SimilarityScorer.Score(first, second, Only(), GenreTable());

        // assert
        result.Score.ShouldBe(0.0);
        Text(result).ShouldBeEmpty();
    }

    [Fact]
    public void Score_ZeroWeightFullyEmptyProfiles_EmitsNoReasons()
    {
        // arrange
        var empty = new MovieFeatures();

        // act
        var result = SimilarityScorer.Score(empty, empty, Only(), GenreTable());

        // assert
        result.Score.ShouldBe(0.0);
        Text(result).ShouldBeEmpty();
    }

    [Fact]
    public void Score_ScaledUnderflowZeroesScore_StillEmitsReasonForPositiveRawContribution()
    {
        // arrange
        var first = Features(releaseYear: 2000, runtime: 1);
        var second = Features(releaseYear: 2010, runtime: int.MaxValue);
        var weights = Only(year: double.Epsilon, runtime: double.MaxValue);

        // act
        var result = SimilarityScorer.Score(first, second, weights, GenreTable());

        // assert
        result.Score.ShouldBe(0.0);
        Text(result).ShouldBe(["Similar release years"]);
    }

    [Fact]
    public void Score_AllReasons_AreStructural()
    {
        // arrange
        var first = Features(directors: ["Alpha"], writers: ["Beta"]);
        var second = Features(directors: ["alpha"], writers: ["beta"]);

        // act
        var result = SimilarityScorer.Score(
            first,
            second,
            Only(director: 1.0, writers: 1.0),
            GenreTable());

        // assert
        result.Reasons.ShouldNotBeEmpty();
        result.Reasons.ShouldAllBe(reason => reason.Kind == ReasonKind.Structural);
    }

    private static string[] Text(SimilarityResult result) =>
        [.. result.Reasons.Select(reason => reason.Text)];

    private static SimilarityWeights Only(
        double director = 0.0,
        double writers = 0.0,
        double cast = 0.0,
        double genres = 0.0,
        double year = 0.0,
        double runtime = 0.0,
        double rating = 0.0) =>
        new()
        {
            Director = director,
            Writers = writers,
            Cast = cast,
            Genres = genres,
            YearProximity = year,
            RuntimeProximity = runtime,
            RatingBoost = rating,
        };

    private static GenreWeights GenreTable() =>
        GenreWeights.Calculate(
            new GenreCounts(
                100,
                new Dictionary<string, long>
                {
                    ["Drama"] = 1,
                    ["Comedy"] = 90,
                    ["Shared"] = 50,
                }));

    private static MovieFeatures Features(
        IEnumerable<string>? genres = null,
        IEnumerable<string>? directors = null,
        IEnumerable<string>? writers = null,
        IEnumerable<string>? cast = null,
        int? releaseYear = null,
        int? runtime = null,
        decimal? imdbRating = null,
        int? metaScore = null,
        string? fullPlot = null) =>
        new(
            genres,
            directors,
            writers,
            cast,
            releaseYear.HasValue ? new ReleaseYear(releaseYear.Value, Now) : null,
            runtime.HasValue ? new Runtime(runtime.Value) : null,
            imdbRating.HasValue ? new ImdbRating(imdbRating.Value) : null,
            metaScore,
            fullPlot);
}
