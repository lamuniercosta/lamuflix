using System;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class TasteProfileReviewAggregateTests
{
    private const double Tolerance = 1e-12;

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Calculate_LikedAndDislikedReviews_ProduceSeparateAggregates()
    {
        // arrange
        var movies = ImmutableArray.Create(
            ReviewMovie(1, Review.Liked, ["Drama"], Cast: ["Actor A"], Directors: ["Director A"]),
            ReviewMovie(2, Review.Disliked, ["Horror"], Cast: ["Actor B"], Directors: ["Director B"]));

        // act
        var profile = Calculator().Calculate(movies, Weights());

        // assert
        profile.Liked.Genres.Keys.ShouldBe(["Drama"]);
        profile.Disliked.Genres.Keys.ShouldBe(["Horror"]);
        profile.Liked.Cast.Keys.ShouldBe(["Actor A"]);
        profile.Disliked.Cast.Keys.ShouldBe(["Actor B"]);
        profile.Liked.Directors.Keys.ShouldBe(["Director A"]);
        profile.Disliked.Directors.Keys.ShouldBe(["Director B"]);
    }

    [Fact]
    public void Calculate_ReviewedButUnplayedMovie_ContributesEqualReviewSignal()
    {
        // arrange
        var movies = ImmutableArray.Create(ReviewMovie(1, Review.Liked, ["Drama"]));

        // act
        var profile = Calculator().Calculate(movies, Weights());

        // assert
        profile.GenrePreferences.ShouldBeEmpty();
        profile.PlotEmbedding.IsEmpty.ShouldBeTrue();
        profile.Liked.Genres["Drama"].ShouldBe(Weights().GetWeight("Drama"), Tolerance);
    }

    [Fact]
    public void Calculate_DislikedMovie_IsOmittedFromPositiveBaselineInputs()
    {
        // arrange
        var movies = ImmutableArray.Create(
            ReviewMovie(1, Review.None, ["Drama"], isWatched: true),
            ReviewMovie(2, Review.Disliked, ["Horror"], isWatched: true));

        // act
        var profile = Calculator().Calculate(movies, Weights());

        // assert
        profile.GenrePreferences.Keys.ShouldBe(["Drama"]);
        profile.Disliked.Genres.Keys.ShouldBe(["Horror"]);
    }

    [Fact]
    public void Calculate_TwoLikedMovies_UseMeanPerMovieFeatureIncidence()
    {
        // arrange
        var movies = ImmutableArray.Create(
            ReviewMovie(1, Review.Liked, ["Drama"], Cast: ["Actor A", "Actor B"]),
            ReviewMovie(2, Review.Liked, ["Drama", "Comedy"], Cast: ["Actor A"]));

        // act
        var profile = Calculator().Calculate(movies, Weights());

        // assert
        profile.Liked.Cast["Actor A"].ShouldBe(1.0, Tolerance);
        profile.Liked.Cast["Actor B"].ShouldBe(0.5, Tolerance);
        profile.Liked.Genres["Drama"].ShouldBe(Weights().GetWeight("Drama"), Tolerance);
    }

    [Fact]
    public void Calculate_TwoLikedMovies_UseEqualWeightPlotCentroid()
    {
        // arrange
        var first = Vector(1.0f);
        var second = Vector(3.0f);
        var movies = ImmutableArray.Create(
            ReviewMovie(1, Review.Liked, ["Drama"], embedding: first),
            ReviewMovie(2, Review.Liked, ["Drama"], embedding: second));

        // act
        var profile = Calculator().Calculate(movies, Weights());

        // assert
        profile.Liked.PlotEmbedding.Length.ShouldBe(EmbeddingDimensions.Value);
        for (var index = 0; index < EmbeddingDimensions.Value; index++)
        {
            profile.Liked.PlotEmbedding[index].ShouldBe(2.0f, 1e-5f);
        }
    }

    [Fact]
    public void Calculate_LikedSources_CarryProvenanceOrderedByMovieId()
    {
        // arrange
        var movies = ImmutableArray.Create(
            ReviewMovie(3, Review.Liked, ["Drama"], title: "Third"),
            ReviewMovie(1, Review.Liked, ["Drama"], title: "First"),
            ReviewMovie(2, Review.Disliked, ["Horror"], title: "Second"));

        // act
        var profile = Calculator().Calculate(movies, Weights());

        // assert
        profile.LikedSources.Select(source => source.MovieId.Value).ShouldBe([3, 1]);
        profile.LikedSources.Select(source => source.Title).ShouldBe(["Third", "First"]);
    }

    [Fact]
    public void Calculate_NoReviews_LeavesBothReviewAggregatesEmpty()
    {
        // arrange
        var movies = ImmutableArray.Create(ReviewMovie(1, Review.None, ["Drama"], isWatched: true));

        // act
        var profile = Calculator().Calculate(movies, Weights());

        // assert
        profile.Liked.IsEmpty.ShouldBeTrue();
        profile.Disliked.IsEmpty.ShouldBeTrue();
        profile.LikedSources.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void Calculate_ReviewWithoutMetadata_CreatesNoInventedSignal()
    {
        // arrange
        var movies = ImmutableArray.Create(ReviewMovie(1, Review.Liked, []));

        // act
        var profile = Calculator().Calculate(movies, Weights());

        // assert
        profile.Liked.Genres.ShouldBeEmpty();
        profile.Liked.Cast.ShouldBeEmpty();
        profile.Liked.Directors.ShouldBeEmpty();
        profile.Liked.PlotEmbedding.IsEmpty.ShouldBeTrue();
        profile.Liked.IsEmpty.ShouldBeTrue();
    }

    private static TasteProfileMovie ReviewMovie(
        int id,
        Review review,
        string[] genres,
        string[]? Cast = null,
        string[]? Directors = null,
        string? title = null,
        bool isWatched = false,
        ImmutableArray<float>? embedding = null) =>
        new(
            new MovieId(id),
            [.. genres],
            embedding ?? ImmutableArray<float>.Empty,
            ImmutableArray<DateTimeOffset>.Empty,
            isWatched)
        {
            Review = review,
            Title = title ?? $"Movie {id}",
            Cast = Cast is null ? ImmutableArray<string>.Empty : [.. Cast],
            Directors = Directors is null ? ImmutableArray<string>.Empty : [.. Directors],
        };

    private static ImmutableArray<float> Vector(float value)
    {
        var builder = ImmutableArray.CreateBuilder<float>(EmbeddingDimensions.Value);

        for (var index = 0; index < EmbeddingDimensions.Value; index++)
        {
            builder.Add(value);
        }

        return builder.MoveToImmutable();
    }

    private static GenreWeights Weights() => GenreWeights.Calculate(
        new GenreCounts(
            100,
            new System.Collections.Generic.Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
            {
                ["Drama"] = 10,
                ["Comedy"] = 20,
                ["Horror"] = 40,
            }));

    private static TasteProfileCalculator Calculator() =>
        new(new FixedTimeProvider(Now), new RecommendationOptions());
}
