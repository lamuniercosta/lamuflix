using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class OutsideLibraryDiscoveryTests
{
    [Fact]
    public void RankAll_CandidateTmdbIdAnywhereInLibrary_ExcludesCandidate()
    {
        // arrange
        var snapshot = Snapshot(
            [Seed(1, 100), Seed(2, 100)],
            [Identity(100, "Unrelated", 1999)]);

        // act
        var result = OutsideLibraryRanking.RankAll(snapshot, Options());

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void RankAll_MatchingTitleAndYearOnMovieWithTmdbId_DoesNotExcludeDifferentCandidate()
    {
        // arrange
        var snapshot = Snapshot(
            [Seed(1, 100, "Shared", 2001), Seed(2, 100, "Shared", 2001)],
            [Identity(999, "Shared", 2001)]);

        // act
        var result = OutsideLibraryRanking.RankAll(snapshot, Options());

        // assert
        result.Select(candidate => candidate.TmdbId).ShouldBe([100]);
    }

    [Fact]
    public void RankAll_MatchingTitleAndYearOnMovieWithoutTmdbId_ExcludesCandidate()
    {
        // arrange
        var snapshot = Snapshot(
            [Seed(1, 100, "Shared", 2001), Seed(2, 100, "Shared", 2001)],
            [Identity(null, "Shared", 2001)]);

        // act
        var result = OutsideLibraryRanking.RankAll(snapshot, Options());

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void RankAll_SameTitleDifferentYear_DoesNotExcludeCandidate()
    {
        // arrange
        var snapshot = Snapshot(
            [Seed(1, 100, "Shared", 2001), Seed(2, 100, "Shared", 2001)],
            [Identity(null, "Shared", 2002)]);

        // act
        var result = OutsideLibraryRanking.RankAll(snapshot, Options());

        // assert
        result.Select(candidate => candidate.TmdbId).ShouldBe([100]);
    }

    [Fact]
    public void RankAll_Normalization_ComposesAccentsAndCollapsesWhitespaceButKeepsPunctuation()
    {
        // arrange
        var decomposed = Snapshot(
            [Seed(1, 100, "Cafe\u0301  Noir", 2001), Seed(2, 100, "Cafe\u0301  Noir", 2001)],
            [Identity(null, "Café Noir", 2001)]);
        var punctuation = Snapshot(
            [Seed(1, 101, "Café Noir", 2001), Seed(2, 101, "Café Noir", 2001)],
            [Identity(null, "Cafe Noir", 2001)]);

        // act
        var decomposedResult = OutsideLibraryRanking.RankAll(decomposed, Options());
        var punctuationResult = OutsideLibraryRanking.RankAll(punctuation, Options());

        // assert
        decomposedResult.ShouldBeEmpty();
        punctuationResult.Select(candidate => candidate.TmdbId).ShouldBe([101]);
    }

    [Fact]
    public void RankAll_UnknownYearOnEitherSide_DoesNotEstablishFallbackMatch()
    {
        // arrange
        var candidateWithoutYear = Snapshot(
            [Seed(1, 100, "Shared", null), Seed(2, 100, "Shared", null)],
            [Identity(null, "Shared", 2001)]);
        var libraryWithoutYear = Snapshot(
            [Seed(1, 101, "Shared", 2001), Seed(2, 101, "Shared", 2001)],
            [Identity(null, "Shared", null)]);

        // act
        var candidateResult = OutsideLibraryRanking.RankAll(candidateWithoutYear, Options());
        var libraryResult = OutsideLibraryRanking.RankAll(libraryWithoutYear, Options());

        // assert
        candidateResult.Select(candidate => candidate.TmdbId).ShouldBe([100]);
        libraryResult.Select(candidate => candidate.TmdbId).ShouldBe([101]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RankAll_BlankCandidateTitle_DoesNotContribute(string? title)
    {
        // arrange
        var snapshot = Snapshot(
            [Seed(1, 100, "Visible"), Seed(2, 100, "Visible"), Seed(99, 200, title)]);

        // act
        var result = OutsideLibraryRanking.RankAll(snapshot, Options());

        // assert
        result.ShouldHaveSingleItem().Score.ShouldBe(4);
    }

    [Fact]
    public void RankAll_DislikedAndWatchlistOnlySources_DoNotContribute()
    {
        // arrange
        var snapshot = Snapshot(
            [
                Seed(1, 100, review: Review.Disliked, watched: true),
                Seed(2, 100, review: Review.None, watchlist: true),
                Seed(3, 100, review: Review.Liked),
            ]);

        // act
        var result = OutsideLibraryRanking.RankAll(snapshot, Options());

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void RankAll_OneDistinctEligibleSeed_IsOmitted_TwoDistinctSeeds_AreIncluded()
    {
        // arrange
        var oneSeed = Snapshot([Seed(1, 100)]);
        var twoSeeds = Snapshot([Seed(1, 100), Seed(2, 100)]);

        // act
        var oneSeedResult = OutsideLibraryRanking.RankAll(oneSeed, Options());
        var twoSeedsResult = OutsideLibraryRanking.RankAll(twoSeeds, Options());

        // assert
        oneSeedResult.ShouldBeEmpty();
        twoSeedsResult.ShouldHaveSingleItem().SeedCount.ShouldBe(2);
    }

    [Fact]
    public void RankAll_LikedTakesPrecedenceAndOverridesChangeTheScore()
    {
        // arrange
        var snapshot = Snapshot(
            [
                Seed(1, 100, seedTitle: "Watched", review: Review.None, watched: true),
                Seed(2, 100, seedTitle: "Liked", review: Review.Liked, watched: true),
                Seed(3, 100, seedTitle: "Watched Two", review: Review.None, watched: true),
            ]);

        // act
        var defaults = OutsideLibraryRanking.RankAll(snapshot, Options()).ShouldHaveSingleItem();
        var overrides = OutsideLibraryRanking.RankAll(snapshot, Options(3, 2)).ShouldHaveSingleItem();

        // assert
        defaults.Score.ShouldBe(4);
        overrides.Score.ShouldBe(7);
        defaults.Card.Reason.ShouldBe("Recommended for Liked and Watched");
        overrides.Card.Reason.ShouldBe("Recommended for Liked and Watched");
    }

    [Fact]
    public void RankAll_CanonicalTupleIsSelectedBeforeFallbackAndDisplay()
    {
        // arrange
        var contributions = new[]
        {
            Seed(1, 100, "Winner", 2001, "winner.jpg"),
            Seed(2, 100, "Loser", 1999, "loser.jpg", review: Review.None, watched: true),
            Seed(3, 100, "Ineligible", 1998, "ineligible.jpg", review: Review.Disliked, watched: true),
        };
        var losingIdentity = Snapshot(contributions, [Identity(null, "Loser", 1999)]);
        var winningIdentity = Snapshot(contributions, [Identity(null, "Winner", 2001)]);

        // act
        var card = OutsideLibraryRanking.RankAll(losingIdentity, Options()).ShouldHaveSingleItem().Card;
        var excluded = OutsideLibraryRanking.RankAll(winningIdentity, Options());

        // assert
        (card.Title, card.ReleaseYear, card.PosterPath).ShouldBe(("Winner", 2001, "winner.jpg"));
        excluded.ShouldBeEmpty();
    }

    [Fact]
    public void RankAll_CandidateOrderIsSeparateFromSeedOrder()
    {
        // arrange
        var snapshot = Snapshot(
            [
                Seed(1, 300), Seed(2, 300),
                Seed(1, 200), Seed(2, 200), Seed(3, 200, review: Review.None, watched: true),
                Seed(1, 100), Seed(2, 100),
                Seed(4, 180, seedTitle: "High"),
                Seed(5, 180, seedTitle: "Low One", review: Review.None, watched: true),
                Seed(6, 180, seedTitle: "Low Two", review: Review.None, watched: true),
            ]);

        // act
        var result = OutsideLibraryRanking.Rank(snapshot, Options());

        // assert
        result.Select(card => card.TmdbId).ShouldBe([200, 100, 180, 300]);
        result[2].Reason.ShouldBe("Recommended for High and Low One");
    }

    [Fact]
    public void Rank_ReturnsAtMostTwentyCardsSortedByScoreThenTmdbId()
    {
        // arrange
        var contributions = Enumerable.Range(0, 25)
            .SelectMany(index => new[]
            {
                Seed(1, 100 + index),
                Seed(2, 100 + index),
            })
            .ToArray();

        // act
        var result = OutsideLibraryRanking.Rank(Snapshot(contributions), Options());

        // assert
        result.Count.ShouldBe(20);
        result.Select(card => card.TmdbId).ShouldBe([.. Enumerable.Range(100, 20)]);
    }

    [Fact]
    public void Rank_CoincidentSeedTitles_AreBothNamedAndSanitizedAsPlainText()
    {
        // arrange
        var snapshot = Snapshot(
            [
                Seed(1, 100, "Wanted", seedTitle: "Same\u0001 Title"),
                Seed(2, 100, "Wanted", seedTitle: "  Same Title  "),
            ]);

        // act
        var card = OutsideLibraryRanking.Rank(snapshot, Options()).ShouldHaveSingleItem();

        // assert
        card.Reason.ShouldBe("Recommended for Same  Title and Same Title");
    }

    private static RecommendationOptions Options(int liked = 2, int watched = 1) =>
        new() { DiscoveryWeightLiked = liked, DiscoveryWeightWatched = watched };

    private static OutsideLibraryRecommendationSnapshot Snapshot(
        IEnumerable<OutsideLibraryRecommendationContribution> contributions,
        IEnumerable<OutsideLibraryMovieIdentity>? library = null) =>
        new([.. contributions], [.. library ?? []]);

    private static OutsideLibraryRecommendationContribution Seed(
        int sourceId,
        int tmdbId,
        string? title = "Candidate",
        int? year = 1888,
        string? poster = null,
        string seedTitle = "Seed",
        Review? review = null,
        bool watched = false,
        bool watchlist = false) =>
        new(
            new MovieId(sourceId),
            seedTitle,
            review ?? Review.Liked,
            watched,
            watchlist,
            tmdbId,
            title,
            year,
            poster);

    private static OutsideLibraryMovieIdentity Identity(int? tmdbId, string title, int? year = 1888) =>
        new(tmdbId, title, year);
}
