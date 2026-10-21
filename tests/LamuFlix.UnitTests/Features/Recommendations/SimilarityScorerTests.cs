using System;
using System.Collections.Generic;
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
            releaseYear is int year ? new ReleaseYear(year, Now) : null,
            runtime is int minutes ? new Runtime(minutes) : null,
            imdbRating is decimal rating ? new ImdbRating(rating) : null,
            metaScore,
            fullPlot);
}
