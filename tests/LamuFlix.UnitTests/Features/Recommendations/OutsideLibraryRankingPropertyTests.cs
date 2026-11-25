using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

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
