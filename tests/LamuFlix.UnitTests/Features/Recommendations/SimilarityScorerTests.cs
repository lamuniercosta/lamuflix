using System;
using System.Collections.Generic;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityScorerTests
{
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

    private static SimilarityWeights Only(
        double director = 0.0,
        double writers = 0.0,
        double cast = 0.0,
        double genres = 0.0) =>
        new()
        {
            Director = director,
            Writers = writers,
            Cast = cast,
            Genres = genres,
            YearProximity = 0.0,
            RuntimeProximity = 0.0,
            RatingBoost = 0.0,
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
        IEnumerable<string>? cast = null) =>
        new(genres: genres, directors: directors, writers: writers, cast: cast);
}
