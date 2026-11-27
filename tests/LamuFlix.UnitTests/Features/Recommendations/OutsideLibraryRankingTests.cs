using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class OutsideLibraryRankingTests
{
    [Fact]
    public void RankAll_NullSnapshot_ThrowsArgumentNullException()
    {
        // act
        var failure = Should.Throw<ArgumentNullException>(() =>
            OutsideLibraryRanking.RankAll(null!, Options()));

        // assert
        failure.ParamName.ShouldBe("snapshot");
    }

    [Fact]
    public void RankAll_NullOptions_ThrowsArgumentNullException()
    {
        // act
        var failure = Should.Throw<ArgumentNullException>(() =>
            OutsideLibraryRanking.RankAll(new OutsideLibraryRecommendationSnapshot([], []), null!));

        // assert
        failure.ParamName.ShouldBe("options");
    }

    [Fact]
    public void RankAll_ZeroWeightCandidate_IsExcluded()
    {
        // arrange
        var contributions = Pair(1, 201, Review.Liked, false, false, "Candidate 201")
            .Concat(Pair(3, 202, Review.None, false, false, "Candidate 202"))
            .ToArray();

        // act
        var ranked = OutsideLibraryRanking.RankAll(Snapshot(contributions), Options());

        // assert
        ranked.Select(candidate => candidate.TmdbId).ShouldBe([201]);
    }

    [Fact]
    public void RankAll_BlankTitleCandidate_IsExcluded()
    {
        // arrange
        var contributions = Pair(1, 201, Review.Liked, false, false, "Candidate 201")
            .Concat(Pair(3, 202, Review.Liked, false, false, "   "))
            .ToArray();

        // act
        var ranked = OutsideLibraryRanking.RankAll(Snapshot(contributions), Options());

        // assert
        ranked.Select(candidate => candidate.TmdbId).ShouldBe([201]);
    }

    [Fact]
    public void RankAll_DuplicateContribution_CountsEachSeedOnce()
    {
        // arrange
        OutsideLibraryRecommendationContribution[] contributions =
        [
            Contribution(1, 201, Review.Liked, false, false, "Candidate 201"),
            Contribution(1, 201, Review.Liked, false, false, "Candidate 201"),
            Contribution(2, 201, Review.Liked, false, false, "Candidate 201"),
        ];

        // act
        var ranked = OutsideLibraryRanking.RankAll(Snapshot(contributions), Options());

        // assert
        ranked.ShouldHaveSingleItem().Score.ShouldBe(4L);
    }

    [Fact]
    public void RankAll_DislikedOrUnactionableWatchlistCandidates_AreSuppressed()
    {
        // arrange
        var contributions = Pair(1, 201, Review.Liked, false, false, "Candidate 201")
            .Concat(Pair(3, 202, Review.Disliked, true, false, "Candidate 202"))
            .Concat(Pair(5, 203, Review.None, true, true, "Candidate 203"))
            .Concat(Pair(7, 204, Review.Liked, false, true, "Candidate 204"))
            .ToArray();

        // act
        var ranked = OutsideLibraryRanking.RankAll(Snapshot(contributions), Options());

        // assert
        ranked.Select(candidate => candidate.TmdbId).OrderBy(id => id).ShouldBe([201, 203, 204]);
        ranked.Single(candidate => candidate.TmdbId == 201).Score.ShouldBe(4L);
        ranked.Single(candidate => candidate.TmdbId == 204).Score.ShouldBe(4L);
        ranked.Single(candidate => candidate.TmdbId == 203).Score.ShouldBe(2L);
    }

    [Fact]
    public void RankAll_IrregularSpacing_MatchesLibraryTitle()
    {
        // arrange
        OutsideLibraryMovieIdentity[] library =
        [
            new(null, "Candidate 201", 2001),
            new(null, "Candidate 202", 2001),
        ];
        var contributions = Pair(1, 201, Review.Liked, false, false, " Candidate 201")
            .Concat(Pair(3, 202, Review.Liked, false, false, "Candidate  202"))
            .ToArray();

        // act
        var ranked = OutsideLibraryRanking.RankAll(new OutsideLibraryRecommendationSnapshot(contributions, library), Options());

        // assert
        ranked.ShouldBeEmpty();
    }

    private static IEnumerable<OutsideLibraryRecommendationContribution> Pair(
        int firstSourceId,
        int tmdbId,
        Review review,
        bool watched,
        bool watchlist,
        string title) =>
    [
        Contribution(firstSourceId, tmdbId, review, watched, watchlist, title),
        Contribution(firstSourceId + 1, tmdbId, review, watched, watchlist, title),
    ];

    private static OutsideLibraryRecommendationContribution Contribution(
        int sourceId,
        int tmdbId,
        Review review,
        bool watched,
        bool watchlist,
        string title) =>
        new(
            new MovieId(sourceId),
            $"Seed {sourceId}",
            review,
            watched,
            watchlist,
            tmdbId,
            title,
            2001,
            null);

    private static OutsideLibraryRecommendationSnapshot Snapshot(
        IEnumerable<OutsideLibraryRecommendationContribution> contributions) =>
        new([.. contributions], []);

    private static RecommendationOptions Options() =>
        new() { DiscoveryWeightLiked = 2, DiscoveryWeightWatched = 1 };
}
