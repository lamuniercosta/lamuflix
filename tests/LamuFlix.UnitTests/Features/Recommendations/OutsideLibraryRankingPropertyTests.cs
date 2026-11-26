using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using Contribution = LamuFlix.Core.Features.Recommendations.OutsideLibraryRecommendationContribution;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class OutsideLibraryRankingPropertyTests
{
    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool RankAll_DuplicatePairsAndShuffledInput_ProduceIdenticalCards(
        int[] sourceSeeds,
        int[] tmdbSeeds,
        int shuffleSeed)
    {
        // arrange
        var contributions = BuildContributions(sourceSeeds, tmdbSeeds)
            .SelectMany(contribution => new[] { contribution, contribution })
            .ToArray();
        var shuffled = contributions
            .OrderBy(contribution => (contribution.SourceMovieId.Value * 397) ^ contribution.TmdbId ^ shuffleSeed)
            .ThenBy(contribution => contribution.SeedTitle, StringComparer.Ordinal)
            .ToArray();

        // act
        var original = OutsideLibraryRanking.RankAll(Snapshot(contributions), Options())
            .OrderBy(candidate => candidate.TmdbId)
            .Select(Describe)
            .ToArray();
        var reordered = OutsideLibraryRanking.RankAll(Snapshot(shuffled), Options())
            .OrderBy(candidate => candidate.TmdbId)
            .Select(Describe)
            .ToArray();

        // assert
        return original.SequenceEqual(reordered);
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Rank_AlwaysSortedByScoreThenTmdbIdAndBoundedAtTwenty(
        int[] sourceSeeds,
        int[] tmdbSeeds)
    {
        // arrange
        var contributions = BuildContributions(sourceSeeds, tmdbSeeds).ToArray();
        var rankedCandidates = OutsideLibraryRanking.RankAll(Snapshot(contributions), Options());

        // act
        var cards = OutsideLibraryRanking.Rank(Snapshot(contributions), Options());

        // assert
        var expected = rankedCandidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.TmdbId)
            .Take(20)
            .Select(candidate => candidate.Card);
        return cards.Count <= 20 && cards.SequenceEqual(expected);
    }

    [Fact]
    public void Rank_LikedAndWatchedSeeds_UseTwoAndOneWeights()
    {
        // arrange
        Contribution[] contributions =
        [
            Contribution(1, 100, liked: true),
            Contribution(2, 100, liked: true),
            Contribution(3, 101, liked: false),
            Contribution(4, 101, liked: false),
        ];

        // act
        var ranked = OutsideLibraryRanking.RankAll(Snapshot(contributions), Options());

        // assert
        ranked.Select(candidate => candidate.TmdbId).ShouldBe([100, 101]);
        ranked.Select(candidate => candidate.Score).ShouldBe([4L, 2L]);
        ranked.Select(candidate => candidate.SeedCount).ShouldBe([2, 2]);
    }

    [Fact]
    public void Rank_EqualScores_OrdersByAscendingTmdbId()
    {
        // arrange
        Contribution[] contributions =
        [
            Contribution(1, 300, liked: true),
            Contribution(2, 300, liked: true),
            Contribution(3, 200, liked: true),
            Contribution(4, 200, liked: true),
        ];

        // act
        var cards = OutsideLibraryRanking.Rank(Snapshot(contributions), Options());

        // assert
        cards.Select(card => card.TmdbId).ShouldBe([200, 300]);
    }

    [Fact]
    public void RankAll_LibraryIdentityAndSingleSeedCandidates_AreExcluded()
    {
        // arrange
        Contribution[] contributions =
        [
            Contribution(1, 100, liked: true),
            Contribution(2, 100, liked: true),
            Contribution(3, 101, liked: true),
            Contribution(4, 101, liked: true),
            Contribution(5, 102, liked: true),
        ];
        var snapshot = new OutsideLibraryRecommendationSnapshot(
            contributions,
            [new OutsideLibraryMovieIdentity(101, "Other", null)]);

        // act
        var ranked = OutsideLibraryRanking.RankAll(snapshot, Options());

        // assert
        ranked.Select(candidate => candidate.TmdbId).ShouldBe([100]);
    }

    [Fact]
    public void Rank_MoreThanTwentyEligibleCandidates_ReturnsTopTwenty()
    {
        // arrange
        var contributions = Enumerable.Range(1, 50)
            .SelectMany(source => new[]
            {
                Contribution(source, 100 + source, liked: true),
                Contribution(100 + source, 100 + source, liked: true),
            })
            .ToArray();

        // act
        var cards = OutsideLibraryRanking.Rank(Snapshot(contributions), Options());

        // assert
        cards.Count.ShouldBe(20);
        cards.Select(card => card.TmdbId).ShouldBe(Enumerable.Range(101, 20));
    }

    private static OutsideLibraryRecommendationContribution Contribution(
        int sourceId,
        int tmdbId,
        bool liked) =>
        new(
            new MovieId(sourceId),
            $"Seed {sourceId}",
            liked ? Review.Liked : Review.None,
            IsWatched: !liked,
            IsInWatchlist: false,
            tmdbId,
            $"Candidate {tmdbId}",
            2001,
            null);

    private static IEnumerable<OutsideLibraryRecommendationContribution> BuildContributions(
        int[] sourceSeeds,
        int[] tmdbSeeds)
    {
        var sources = sourceSeeds
            .Select(value => Math.Abs(value) % 6 + 1)
            .Distinct()
            .Take(6)
            .ToArray();
        var tmdbIds = tmdbSeeds
            .Select(value => Math.Abs(value) % 8 + 100)
            .Distinct()
            .Take(8)
            .ToArray();

        return sources.SelectMany(source => tmdbIds.Select(tmdbId =>
            new OutsideLibraryRecommendationContribution(
                new MovieId(source),
                $"Seed {source}",
                source % 2 == 0 ? Review.Liked : Review.None,
                IsWatched: source % 2 != 0,
                IsInWatchlist: false,
                tmdbId,
                $"Candidate {tmdbId}",
                2001,
                null)));
    }

    private static OutsideLibraryRecommendationSnapshot Snapshot(
        IEnumerable<OutsideLibraryRecommendationContribution> contributions) =>
        new([.. contributions], []);

    private static RecommendationOptions Options() =>
        new() { DiscoveryWeightLiked = 2, DiscoveryWeightWatched = 1 };

    private static string Describe(OutsideLibraryRankedCandidate candidate) =>
        string.Concat(
            candidate.TmdbId,
            ":",
            candidate.Score,
            ":",
            candidate.SeedCount,
            ":",
            candidate.Card.Title,
            ":",
            candidate.Card.Reason);
}
