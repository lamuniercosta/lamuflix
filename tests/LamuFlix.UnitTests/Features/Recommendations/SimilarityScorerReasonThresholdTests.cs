using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SimilarityScorerReasonThresholdTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Score_LargeYearGap_OmitsYearReason()
    {
        // arrange
        var first = Features(releaseYear: 1973);
        var second = Features(releaseYear: 2002);

        // act
        var result = SimilarityScorer.Score(first, second, Only(year: 1.0), GenreTable());

        // assert
        Text(result).ShouldNotContain("Similar release years");
    }

    [Fact]
    public void Score_SmallYearGap_KeepsYearReason()
    {
        // arrange
        var first = Features(releaseYear: 2002);
        var second = Features(releaseYear: 2004);

        // act
        var result = SimilarityScorer.Score(first, second, Only(year: 1.0), GenreTable());

        // assert
        Text(result).ShouldContain("Similar release years");
    }

    [Fact]
    public void Score_LargeRuntimeGap_OmitsRuntimeReason()
    {
        // arrange
        var first = Features(runtime: 90);
        var second = Features(runtime: 200);

        // act
        var result = SimilarityScorer.Score(first, second, Only(runtime: 1.0), GenreTable());

        // assert
        Text(result).ShouldNotContain("Similar runtime");
    }

    [Fact]
    public void Score_SmallRuntimeGap_KeepsRuntimeReason()
    {
        // arrange
        var first = Features(runtime: 100);
        var second = Features(runtime: 110);

        // act
        var result = SimilarityScorer.Score(first, second, Only(runtime: 1.0), GenreTable());

        // assert
        Text(result).ShouldContain("Similar runtime");
    }

    [Fact]
    public void Score_LargeRatingGap_OmitsRatingReason()
    {
        // arrange
        var first = Features(imdbRating: 4.0m);
        var second = Features(imdbRating: 9.0m);

        // act
        var result = SimilarityScorer.Score(first, second, Only(rating: 1.0), GenreTable());

        // assert
        Text(result).ShouldNotContain("Similar rating");
    }

    [Fact]
    public void Score_SmallRatingGap_KeepsRatingReason()
    {
        // arrange
        var first = Features(imdbRating: 8.0m);
        var second = Features(imdbRating: 7.5m);

        // act
        var result = SimilarityScorer.Score(first, second, Only(rating: 1.0), GenreTable());

        // assert
        Text(result).ShouldContain("Similar rating");
    }

    [Fact]
    public void Score_YearGapAtThreshold_KeepsYearReason()
    {
        // arrange
        var first = Features(releaseYear: 2000);
        var second = Features(releaseYear: 2005);

        // act
        var result = SimilarityScorer.Score(first, second, Only(year: 1.0), GenreTable());

        // assert
        Text(result).ShouldContain("Similar release years");
    }

    [Fact]
    public void Score_YearGapBeyondThreshold_OmitsYearReason()
    {
        // arrange
        var first = Features(releaseYear: 2000);
        var second = Features(releaseYear: 2006);

        // act
        var result = SimilarityScorer.Score(first, second, Only(year: 1.0), GenreTable());

        // assert
        Text(result).ShouldNotContain("Similar release years");
    }

    [Fact]
    public void Score_RuntimeGapAtThreshold_KeepsRuntimeReason()
    {
        // arrange
        var first = Features(runtime: 100);
        var second = Features(runtime: 115);

        // act
        var result = SimilarityScorer.Score(first, second, Only(runtime: 1.0), GenreTable());

        // assert
        Text(result).ShouldContain("Similar runtime");
    }

    [Fact]
    public void Score_RuntimeGapBeyondThreshold_OmitsRuntimeReason()
    {
        // arrange
        var first = Features(runtime: 100);
        var second = Features(runtime: 116);

        // act
        var result = SimilarityScorer.Score(first, second, Only(runtime: 1.0), GenreTable());

        // assert
        Text(result).ShouldNotContain("Similar runtime");
    }

    [Fact]
    public void Score_RatingGapAtThreshold_KeepsRatingReason()
    {
        // arrange
        var first = Features(imdbRating: 8.0m);
        var second = Features(imdbRating: 7.0m);

        // act
        var result = SimilarityScorer.Score(first, second, Only(rating: 1.0), GenreTable());

        // assert
        Text(result).ShouldContain("Similar rating");
    }

    [Fact]
    public void Score_RatingGapBeyondThreshold_OmitsRatingReason()
    {
        // arrange
        var first = Features(imdbRating: 8.0m);
        var second = Features(imdbRating: 6.9m);

        // act
        var result = SimilarityScorer.Score(first, second, Only(rating: 1.0), GenreTable());

        // assert
        Text(result).ShouldNotContain("Similar rating");
    }

    private static string[] Text(SimilarityResult result) =>
        [.. result.Reasons.Select(reason => reason.Text)];

    private static SimilarityWeights Only(
        double year = 0.0,
        double runtime = 0.0,
        double rating = 0.0) =>
        new()
        {
            YearProximity = year,
            RuntimeProximity = runtime,
            RatingBoost = rating,
        };

    private static GenreWeights GenreTable() =>
        GenreWeights.Calculate(new GenreCounts(0, new Dictionary<string, long>()));

    private static MovieFeatures Features(
        int? releaseYear = null,
        int? runtime = null,
        decimal? imdbRating = null) =>
        new(
            releaseYear: releaseYear.HasValue ? new ReleaseYear(releaseYear.Value, Now) : null,
            runtime: runtime.HasValue ? new Runtime(runtime.Value) : null,
            imdbRating: imdbRating.HasValue ? new ImdbRating(imdbRating.Value) : null);
}
