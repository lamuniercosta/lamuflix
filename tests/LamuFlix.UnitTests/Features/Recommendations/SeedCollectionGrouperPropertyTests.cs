using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SeedCollectionGrouperPropertyTests
{
    private const int FranchiseId = 7;
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private static readonly GenreWeights Table = GenreWeights.Calculate(
        new GenreCounts(
            10,
            new Dictionary<string, long>
            {
                ["Drama"] = 5,
                ["Comedy"] = 5,
            }));

    [Property(MaxTest = 100, Replay = "345001,100003")]
    [Trait("Category", "Property")]
    public bool Group_GeneratedPortfolio_ReturnsUniqueCandidateIdsWithinCapacity(
        int[] ids,
        bool[] franchiseFlags,
        int takeSeed)
    {
        // arrange
        var candidates = Candidates(ids, franchiseFlags);
        var take = PositiveMod(takeSeed, 15);
        var order = Order(candidates, take);

        // act
        var result = SeedCollectionGrouper.Group(candidates, order, Seed(), take, Weights(), Table);

        // assert
        return result.Count <= take
            && result.Distinct().Count() == result.Count
            && result.All(id => candidates.Any(candidate => candidate.MovieId == id));
    }

    [Property(MaxTest = 100, Replay = "345002,200009")]
    [Trait("Category", "Property")]
    public bool Group_GeneratedPortfolio_IsDeterministic(
        int[] ids,
        bool[] franchiseFlags,
        int takeSeed)
    {
        // arrange
        var candidates = Candidates(ids, franchiseFlags);
        var take = PositiveMod(takeSeed, 15);
        var order = Order(candidates, take);

        // act
        var first = SeedCollectionGrouper.Group(candidates, order, Seed(), take, Weights(), Table);
        var second = SeedCollectionGrouper.Group(candidates, order, Seed(), take, Weights(), Table);

        // assert
        return first.SequenceEqual(second);
    }

    [Property(MaxTest = 100, Replay = "345003,300011")]
    [Trait("Category", "Property")]
    public bool Group_NullSeedCollection_IsExactNoOp(int[] ids, bool[] franchiseFlags, int takeSeed)
    {
        // arrange
        var candidates = Candidates(ids, franchiseFlags);
        var take = PositiveMod(takeSeed, 15);
        var order = Order(candidates, take);

        // act
        var result = SeedCollectionGrouper.Group(candidates, order, Seed(collectionId: null), take, Weights(), Table);

        // assert
        return result.SequenceEqual(order);
    }

    [Property(MaxTest = 100, Replay = "345004,400001")]
    [Trait("Category", "Property")]
    public bool Group_MatchingPrefix_ContainsOnlySameCollectionMembers(
        int[] ids,
        bool[] franchiseFlags,
        int takeSeed)
    {
        // arrange
        var candidates = Candidates(ids, franchiseFlags);
        var take = PositiveMod(takeSeed, 15);
        var order = Order(candidates, take);
        var expectedBlockSize = Math.Min(
            Math.Min(SeedCollectionGrouper.MaximumBlockSize, take),
            candidates.Count(candidate => candidate.Features.CollectionId == FranchiseId));

        // act
        var result = SeedCollectionGrouper.Group(candidates, order, Seed(), take, Weights(), Table);

        // assert
        var byId = candidates.ToDictionary(candidate => candidate.MovieId);
        return result.Take(expectedBlockSize)
            .All(id => byId[id].Features.CollectionId == FranchiseId);
    }

    private static List<MmrCandidate> Candidates(int[] ids, bool[] franchiseFlags)
    {
        var distinct = ids
            .Select(value => Math.Abs(value % 500) + 2)
            .Distinct()
            .Take(16)
            .ToArray();
        var candidates = new List<MmrCandidate>(distinct.Length);

        for (var index = 0; index < distinct.Length; index++)
        {
            var inFranchise = franchiseFlags.Length > 0 && franchiseFlags[index % franchiseFlags.Length];
            candidates.Add(new MmrCandidate(
                new MovieId(distinct[index]),
                Relevance: 1.0,
                Features(inFranchise ? FranchiseId : null)));
        }

        return candidates;
    }

    private static ImmutableList<MovieId> Order(List<MmrCandidate> candidates, int take) =>
        [.. candidates.Select(candidate => candidate.MovieId).Reverse().Take(take)];

    private static MovieFeatures Seed(int? collectionId = FranchiseId) => Features(collectionId);

    private static MovieFeatures Features(int? collectionId) =>
        new(
            genres: ["Drama"],
            releaseYear: new ReleaseYear(2000, Now),
            collectionId: collectionId);

    private static SimilarityWeights Weights() => new();

    private static int PositiveMod(int value, int divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }
}
