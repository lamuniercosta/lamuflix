using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class SeedCollectionGrouperTests
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

    [Fact]
    public void Group_NullSeedCollection_ReturnsMmrOrderUnchanged()
    {
        // arrange
        var order = ImmutableList.Create(new MovieId(2), new MovieId(3));
        var candidates = new List<MmrCandidate>
        {
            Candidate(2, collectionId: FranchiseId),
            Candidate(3, collectionId: FranchiseId),
        };

        // act
        var result = SeedCollectionGrouper.Group(candidates, order, Seed(collectionId: null), 10, Weights(), Table);

        // assert
        result.ShouldBeSameAs(order);
    }

    [Fact]
    public void Group_NoEligibleMatches_ReturnsMmrOrderUnchanged()
    {
        // arrange
        var order = ImmutableList.Create(new MovieId(2), new MovieId(3));
        var candidates = new List<MmrCandidate>
        {
            Candidate(2, collectionId: 99),
            Candidate(3, collectionId: null),
        };

        // act
        var result = SeedCollectionGrouper.Group(candidates, order, Seed(FranchiseId), 10, Weights(), Table);

        // assert
        result.ShouldBeSameAs(order);
    }

    [Fact]
    public void Group_MatchesAtFront_RankedBySimilarityThenMovieId()
    {
        // arrange
        var order = ImmutableList.Create(new MovieId(2), new MovieId(3), new MovieId(4));
        var candidates = new List<MmrCandidate>
        {
            Candidate(2, collectionId: null),
            Candidate(3, collectionId: FranchiseId, directors: ["Director B"]),
            Candidate(4, collectionId: FranchiseId, directors: ["Director B"]),
            Candidate(5, collectionId: FranchiseId, directors: ["Director A"]),
        };

        // act
        var result = SeedCollectionGrouper.Group(candidates, order, Seed(FranchiseId), 5, Weights(), Table);

        // assert
        result.ShouldBe([new MovieId(5), new MovieId(3), new MovieId(4), new MovieId(2)]);
    }

    [Fact]
    public void Group_MoreMatchesThanCapacity_KeepsHighestScoringBlockAndFillsFromMmr()
    {
        // arrange
        var order = ImmutableList.Create(new MovieId(2), new MovieId(3));
        var candidates = new List<MmrCandidate>
        {
            Candidate(2, collectionId: null),
            Candidate(3, collectionId: null),
            Candidate(10, collectionId: FranchiseId, directors: ["Director B"]),
            Candidate(11, collectionId: FranchiseId, directors: ["Director A"]),
        };

        // act
        var result = SeedCollectionGrouper.Group(candidates, order, Seed(FranchiseId), 3, Weights(), Table);

        // assert
        result.ShouldBe([new MovieId(11), new MovieId(10), new MovieId(2)]);
    }

    [Fact]
    public void Group_MoreThanTenMatches_ChoosesHighestScoringTen()
    {
        // arrange
        var matches = Enumerable.Range(20, 12)
            .Select(id => Candidate(id, collectionId: FranchiseId))
            .ToList();
        var order = ImmutableList.Create(new MovieId(2));
        var candidates = new List<MmrCandidate> { Candidate(2, collectionId: null) };
        candidates.AddRange(matches);

        // act
        var result = SeedCollectionGrouper.Group(candidates, order, Seed(FranchiseId), 12, Weights(), Table);

        // assert
        result.Take(SeedCollectionGrouper.MaximumBlockSize)
            .ShouldBe(Enumerable.Range(20, 10).Select(id => new MovieId(id)));
    }

    [Fact]
    public void Group_ZeroCapacity_ReturnsEmpty()
    {
        // arrange
        var order = ImmutableList<MovieId>.Empty;
        var candidates = new List<MmrCandidate> { Candidate(3, collectionId: FranchiseId) };

        // act
        var result = SeedCollectionGrouper.Group(candidates, order, Seed(FranchiseId), 0, Weights(), Table);

        // assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Group_SelectedBlockMembers_AreNotDuplicatedInRemainder()
    {
        // arrange
        var order = ImmutableList.Create(new MovieId(3), new MovieId(2));
        var candidates = new List<MmrCandidate>
        {
            Candidate(2, collectionId: null),
            Candidate(3, collectionId: FranchiseId),
        };

        // act
        var result = SeedCollectionGrouper.Group(candidates, order, Seed(FranchiseId), 5, Weights(), Table);

        // assert
        result.ShouldBe([new MovieId(3), new MovieId(2)]);
        result.Count.ShouldBe(2);
    }

    [Fact]
    public void Group_NullCandidates_ThrowsArgumentNullException()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => SeedCollectionGrouper.Group(
            null!,
            ImmutableList<MovieId>.Empty,
            Seed(FranchiseId),
            10,
            Weights(),
            Table));

        // assert
        failure.ParamName.ShouldBe("candidates");
    }

    [Fact]
    public void Group_NegativeTake_ThrowsArgumentOutOfRange()
    {
        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => SeedCollectionGrouper.Group(
            [],
            ImmutableList<MovieId>.Empty,
            Seed(FranchiseId),
            -1,
            Weights(),
            Table));

        // assert
        failure.ParamName.ShouldBe("take");
    }

    private static MmrCandidate Candidate(
        int id,
        int? collectionId,
        string[]? directors = null) =>
        new(
            new MovieId(id),
            Relevance: 1.0,
            Features(directors, collectionId));

    private static MovieFeatures Seed(int? collectionId) => Features(["Director A"], collectionId);

    private static MovieFeatures Features(string[]? directors, int? collectionId) =>
        new(
            genres: ["Drama"],
            directors: directors,
            releaseYear: new ReleaseYear(2000, Now),
            collectionId: collectionId);

    private static SimilarityWeights Weights() => new();
}
