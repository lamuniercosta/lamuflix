using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.UnitTests.Domain;

public sealed class MovieReviewTests
{
    [Fact]
    public void Review_SmartEnum_CarriesLiteralLowercaseNamesAndStableValues()
    {
        Review.None.Name.ShouldBe("none");
        Review.Liked.Name.ShouldBe("liked");
        Review.Disliked.Name.ShouldBe("disliked");
        (Review.None.Value, Review.Liked.Value, Review.Disliked.Value).ShouldBe((0, 1, 2));
    }

    [Theory]
    [InlineData("none")]
    [InlineData("liked")]
    [InlineData("disliked")]
    public void TryFromName_ExactName_ReturnsMatchingReview(string name)
    {
        // act
        var found = Review.TryFromName(name, ignoreCase: false, out var review);

        // assert
        found.ShouldBeTrue();
        review.Name.ShouldBe(name);
    }

    [Theory]
    [InlineData("Liked")]
    [InlineData("LIKED")]
    [InlineData("unliked")]
    [InlineData("")]
    [InlineData(" ")]
    public void TryFromName_InexactOrUnknownName_ReturnsFalse(string name)
    {
        // act
        var found = Review.TryFromName(name, ignoreCase: false, out _);

        // assert
        found.ShouldBeFalse();
    }

    [Theory]
    [InlineData("none")]
    [InlineData("liked")]
    [InlineData("disliked")]
    public void TryParse_ExactName_YieldsMatchingReview(string name)
    {
        // act
        var parsed = Review.TryParse(name, out var review);

        // assert
        parsed.ShouldBeTrue();
        review.Name.ShouldBe(name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Liked")]
    [InlineData("LIKED")]
    [InlineData("like")]
    [InlineData("2")]
    public void TryParse_MissingOrInexactName_YieldsNoneAndFalse(string? name)
    {
        // act
        var parsed = Review.TryParse(name, out var review);

        // assert
        parsed.ShouldBeFalse();
        review.ShouldBe(Review.None);
    }

    [Fact]
    public void NewMovie_HasNoExplicitReview()
    {
        // act
        var movie = NewMovie();

        // assert
        movie.Review.ShouldBe(Review.None);
        movie.IsNotInterested.ShouldBeFalse();
    }

    [Fact]
    public void AssignReview_FromNoneToLiked_SetsLikedWithoutTouchingWatchedOrWatchlist()
    {
        // arrange
        var movie = NewMovie();

        // act
        movie.AssignReview(Review.Liked);

        // assert
        movie.Review.ShouldBe(Review.Liked);
        movie.IsNotInterested.ShouldBeFalse();
        movie.IsWatched.ShouldBeFalse();
        movie.IsInWatchlist.ShouldBeFalse();
    }

    [Fact]
    public void AssignReview_LikedToDisliked_SwitchesDirectlyWithoutIntermediateNone()
    {
        // arrange
        var movie = NewMovie();
        movie.AssignReview(Review.Liked);

        // act
        movie.AssignReview(Review.Disliked);

        // assert
        movie.Review.ShouldBe(Review.Disliked);
        movie.IsNotInterested.ShouldBeTrue();
    }

    [Fact]
    public void AssignReview_DislikedToLiked_SwitchesDirectly()
    {
        // arrange
        var movie = NewMovie();
        movie.AssignReview(Review.Disliked);

        // act
        movie.AssignReview(Review.Liked);

        // assert
        movie.Review.ShouldBe(Review.Liked);
        movie.IsNotInterested.ShouldBeFalse();
    }

    [Fact]
    public void AssignReview_ActiveReviewReassignedToItself_IsIdempotent()
    {
        // arrange
        var movie = NewMovie();
        movie.AssignReview(Review.Disliked);

        // act
        movie.AssignReview(Review.Disliked);

        // assert
        movie.Review.ShouldBe(Review.Disliked);
    }

    [Fact]
    public void AssignReview_NullReview_ThrowsArgumentNullException()
    {
        // arrange
        var movie = NewMovie();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var thrown = Should.Throw<ArgumentNullException>(() => movie.AssignReview(null!));

        // assert
        thrown.ParamName.ShouldBe("review");
        movie.Review.ShouldBe(Review.None);
    }

    [Fact]
    public void MarkWatched_ReviewedMovie_LeavesReviewUnchanged()
    {
        // arrange
        var movie = NewMovie();
        movie.AssignReview(Review.Liked);

        // act
        movie.MarkWatched();

        // assert
        movie.IsWatched.ShouldBeTrue();
        movie.Review.ShouldBe(Review.Liked);
    }

    [Fact]
    public void UnmarkWatched_ReviewedMovie_LeavesReviewUnchanged()
    {
        // arrange
        var movie = NewMovie();
        movie.AssignReview(Review.Disliked);
        movie.MarkWatched();

        // act
        movie.UnmarkWatched();

        // assert
        movie.IsWatched.ShouldBeFalse();
        movie.Review.ShouldBe(Review.Disliked);
    }

    [Fact]
    public void AddToWatchlist_ReviewedMovie_LeavesReviewUnchanged()
    {
        // arrange
        var movie = NewMovie();
        movie.AssignReview(Review.Liked);

        // act
        movie.AddToWatchlist();

        // assert
        movie.IsInWatchlist.ShouldBeTrue();
        movie.Review.ShouldBe(Review.Liked);
    }

    [Fact]
    public void MarkNotInterested_ReviewedMovie_SetsDislikedAndKeepsWatchedAndWatchlist()
    {
        // arrange
        var movie = NewMovie();
        movie.MarkWatched();
        movie.AddToWatchlist();

        // act
        movie.MarkNotInterested();

        // assert
        movie.Review.ShouldBe(Review.Disliked);
        movie.IsWatched.ShouldBeTrue();
        movie.IsInWatchlist.ShouldBeTrue();
    }

    [Fact]
    public void MarkNotInterested_LikedMovie_MovesToDisliked()
    {
        // arrange
        var movie = NewMovie();
        movie.AssignReview(Review.Liked);

        // act
        movie.MarkNotInterested();

        // assert
        movie.Review.ShouldBe(Review.Disliked);
    }

    [Fact]
    public void MarkNotInterested_AlreadyDisliked_ThrowsInvalidTransition()
    {
        // arrange
        var movie = NewMovie();
        movie.MarkNotInterested();

        // act
        var thrown = Should.Throw<InvalidTransitionException>(() => movie.MarkNotInterested());

        // assert
        thrown.Action.ShouldBe(nameof(Movie.MarkNotInterested));
        thrown.State.ShouldBe("NotInterested");
        movie.Review.ShouldBe(Review.Disliked);
    }

    [Fact]
    public void UnmarkNotInterested_DislikedMovie_ClearsToNone()
    {
        // arrange
        var movie = NewMovie();
        movie.MarkNotInterested();

        // act
        movie.UnmarkNotInterested();

        // assert
        movie.Review.ShouldBe(Review.None);
        movie.IsNotInterested.ShouldBeFalse();
    }

    [Fact]
    public void UnmarkNotInterested_LikedMovie_KeepsConflictAndPreservesLiked()
    {
        // arrange
        var movie = NewMovie();
        movie.AssignReview(Review.Liked);

        // act
        var thrown = Should.Throw<InvalidTransitionException>(() => movie.UnmarkNotInterested());

        // assert
        thrown.Action.ShouldBe(nameof(Movie.UnmarkNotInterested));
        thrown.State.ShouldBe("Interested");
        movie.Review.ShouldBe(Review.Liked);
        movie.IsNotInterested.ShouldBeFalse();
    }

    [Fact]
    public void UnmarkNotInterested_UnreviewedMovie_ThrowsInvalidTransition()
    {
        // arrange
        var movie = NewMovie();

        // act
        var thrown = Should.Throw<InvalidTransitionException>(() => movie.UnmarkNotInterested());

        // assert
        thrown.Action.ShouldBe(nameof(Movie.UnmarkNotInterested));
        thrown.State.ShouldBe("Interested");
        movie.Review.ShouldBe(Review.None);
    }

    [Fact]
    public void Rehydrate_ReviewNotSupplied_DefaultsToNone()
    {
        // act
        var movie = NewMovie();

        // assert
        movie.Review.ShouldBe(Review.None);
        movie.IsNotInterested.ShouldBeFalse();
    }

    [Fact]
    public void Rehydrate_DislikedReview_ProjectsIsNotInterested()
    {
        // act
        var movie = NewMovie(Review.Disliked);

        // assert
        movie.Review.ShouldBe(Review.Disliked);
        movie.IsNotInterested.ShouldBeTrue();
    }

    [Theory]
    [InlineData("none", false, true, false)]
    [InlineData("liked", true, false, false)]
    [InlineData("disliked", false, false, true)]
    public void Review_StatePredicates_MatchTheReviewName(
        string name,
        bool isLiked,
        bool isNone,
        bool isDisliked)
    {
        // arrange
        Review.TryFromName(name, ignoreCase: false, out var review);

        // act
        var actual = (review.IsNone, review.IsLiked, review.IsDisliked);

        // assert
        actual.ShouldBe((isNone, isLiked, isDisliked));
    }

    [Fact]
    public void Review_Is_ComparesByStateWithoutOperatorOverloads()
    {
        // arrange
        Review.TryFromName("liked", ignoreCase: false, out var liked);

        // act
        var matches = liked.Is(Review.Liked);
        var differs = liked.Is(Review.Disliked);
        var nullMatches = liked.Is(null);

        // assert
        matches.ShouldBeTrue();
        differs.ShouldBeFalse();
        nullMatches.ShouldBeFalse();
    }

    [Fact]
    public void Review_AreEqual_HandlesNullsAndMatchingStates()
    {
        // act
        var bothNull = Review.AreEqual(null, null);
        var oneNull = Review.AreEqual(Review.Liked, null);
        var same = Review.AreEqual(Review.Disliked, Review.Disliked);
        var different = Review.AreEqual(Review.Liked, Review.Disliked);

        // assert
        bothNull.ShouldBeTrue();
        oneNull.ShouldBeFalse();
        same.ShouldBeTrue();
        different.ShouldBeFalse();
    }

    private static Movie NewMovie(Review? review = null) =>
        Movie.Rehydrate(
            new MovieId(37),
            "Heat",
            new LibraryPath("C:/library/Heat (1995)"),
            new MediaFormat("mkv"),
            isInWatchlist: false,
            metadata: null,
            EnrichmentStatus.Enriched,
            enrichedAt: null,
            enrichmentAttempts: 0,
            lastFailureCategory: null,
            lastAttemptAt: null,
            review: review);
}
