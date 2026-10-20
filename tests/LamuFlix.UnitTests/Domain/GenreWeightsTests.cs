using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.UnitTests.Domain;

public sealed class GenreWeightsTests
{
    [Fact]
    public void Calculate_GenreFrequencyGiven_UsesNaturalLogFormulaWithDoubleArithmetic()
    {
        // arrange
        var counts = new GenreCounts(10, new Dictionary<int, long> { [1] = 4 });

        // act
        var weights = GenreWeights.Calculate(counts);

        // assert
        weights.GetWeight(1).ShouldBe(Math.Log(11.0 / 5.0) + 1.0, 1e-12);
    }

    [Fact]
    public void Calculate_ZeroCountGenreInNonEmptyLibrary_UsesZeroFrequencyFormula()
    {
        // arrange
        var counts = new GenreCounts(7, new Dictionary<int, long> { [2] = 0 });

        // act
        var weights = GenreWeights.Calculate(counts);

        // assert
        weights.GetWeight(2).ShouldBe(Math.Log(8.0) + 1.0, 1e-12);
    }

    [Fact]
    public void Similarity_RareGenreSharedUnderCommonUnion_ScoresHigherThanCommonSharedGenre()
    {
        // arrange
        var counts = new GenreCounts(
            100,
            new Dictionary<int, long>
            {
                [1] = 1,
                [2] = 90,
                [3] = 50,
            });
        var weights = GenreWeights.Calculate(counts);

        // act
        var rareShared = weights.Similarity([1], [1, 3]);
        var commonShared = weights.Similarity([2], [2, 3]);

        // assert
        rareShared.ShouldBeGreaterThan(commonShared);
    }

    [Fact]
    public void Calculate_NullCounts_ThrowsArgumentNullException()
    {
        // act / assert
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        Should.Throw<ArgumentNullException>(() => GenreWeights.Calculate(null!));
    }

    [Fact]
    public void Calculate_TotalMoviesNegative_ThrowsArgumentOutOfRange()
    {
        // arrange
        var counts = new GenreCounts(-1, new Dictionary<int, long>());

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => GenreWeights.Calculate(counts));

        // assert
        failure.ParamName.ShouldBe("counts");
        failure.Message.ShouldContain("Total movie count cannot be negative.");
    }

    [Fact]
    public void Calculate_GenreFrequencyNegative_ThrowsArgumentOutOfRange()
    {
        // arrange
        var counts = new GenreCounts(5, new Dictionary<int, long> { [1] = -1 });

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => GenreWeights.Calculate(counts));

        // assert
        failure.ParamName.ShouldBe("counts");
        failure.Message.ShouldContain("Genre movie count cannot be negative.");
    }

    [Fact]
    public void Calculate_GenreFrequencyAboveTotal_ThrowsArgumentOutOfRange()
    {
        // arrange
        var counts = new GenreCounts(2, new Dictionary<int, long> { [1] = 3 });

        // act
        var failure = Should.Throw<ArgumentOutOfRangeException>(() => GenreWeights.Calculate(counts));

        // assert
        failure.ParamName.ShouldBe("counts");
        failure.Message.ShouldContain("Genre movie count cannot exceed the total movie count.");
    }

    [Fact]
    public void Calculate_EmptyLibrary_DefaultsPersistedZeroCountGenreWeightToOne()
    {
        // arrange
        var counts = new GenreCounts(0, new Dictionary<int, long> { [7] = 0 });

        // act
        var weights = GenreWeights.Calculate(counts);

        // assert
        weights.GetWeight(7).ShouldBe(1.0);
    }

    [Fact]
    public void GetWeight_UnknownGenre_ReturnsOne()
    {
        // arrange
        var counts = new GenreCounts(10, new Dictionary<int, long> { [1] = 2 });

        // act
        var weights = GenreWeights.Calculate(counts);

        // assert
        weights.GetWeight(999).ShouldBe(1.0);
    }

    [Fact]
    public void Similarity_NullFirst_ThrowsArgumentNullException()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => weights.Similarity(null!, [1]));

        // assert
        failure.ParamName.ShouldBe("first");
    }

    [Fact]
    public void Similarity_NullSecond_ThrowsArgumentNullException()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => weights.Similarity([1], null!));

        // assert
        failure.ParamName.ShouldBe("second");
    }

    [Fact]
    public void Similarity_BothSetsEmpty_ReturnsZero()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        var score = weights.Similarity([], []);

        // assert
        score.ShouldBe(0.0);
    }

    [Fact]
    public void Similarity_IdenticalNonEmptySets_ReturnsOne()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        var score = weights.Similarity([1, 3, 5], [5, 3, 1]);

        // assert
        score.ShouldBe(1.0);
    }

    [Fact]
    public void Similarity_DisjointSets_ReturnsZero()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        var score = weights.Similarity([1, 2], [3, 4]);

        // assert
        score.ShouldBe(0.0);
    }

    [Fact]
    public void Similarity_DuplicateGenres_MatchesDeduplicatedScore()
    {
        // arrange
        var weights = CreateControlledWeights();
        var expected = weights.Similarity([1, 2], [2, 3]);

        // act
        var duplicated = weights.Similarity([1, 1, 2, 2, 2], [3, 2, 2, 3]);

        // assert
        duplicated.ShouldBe(expected);
    }

    [Fact]
    public void Similarity_PermutedGenreOrder_ReturnsSameScore()
    {
        // arrange
        var weights = CreateControlledWeights();
        var expected = weights.Similarity([1, 2, 3], [2, 3, 4]);

        // act
        var permuted = weights.Similarity([3, 1, 2], [4, 2, 3]);

        // assert
        permuted.ShouldBe(expected);
    }

    [Fact]
    public void Similarity_ArgumentsSwapped_ReturnsSameScore()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        var forward = weights.Similarity([1, 2, 4], [2, 5]);
        var reverse = weights.Similarity([2, 5], [1, 2, 4]);

        // assert
        reverse.ShouldBe(forward);
    }

    [Fact]
    public void Similarity_OverlappingSets_StaysWithinBounds()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        var score = weights.Similarity([1, 2, 3, 4, 5], [2, 4, 6]);

        // assert
        score.ShouldBeGreaterThanOrEqualTo(0.0);
        score.ShouldBeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void Similarity_OverlappingSets_ReturnsFiniteScore()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        var score = weights.Similarity([1, 2, 3], [3, 4, 5]);

        // assert
        double.IsFinite(score).ShouldBeTrue();
    }

    [Fact]
    public void Similarity_ControlledWeights_DoesNotApplyOverallGenreCoefficient()
    {
        // arrange
        var counts = new GenreCounts(
            3,
            new Dictionary<int, long>
            {
                [1] = 1,
                [2] = 2,
            });
        var weights = GenreWeights.Calculate(counts);
        var genreOneWeight = Math.Log(4.0 / 2.0) + 1.0;
        var genreTwoWeight = Math.Log(4.0 / 3.0) + 1.0;
        var expected = genreOneWeight / (genreOneWeight + genreTwoWeight);

        // act
        var score = weights.Similarity([1, 2], [1]);

        // assert
        score.ShouldBe(expected, 1e-12);
    }

    [Fact]
    public void Calculate_InputMapMutatedAfterCalculation_LeavesPublishedWeightsUnchanged()
    {
        // arrange
        var countsByGenre = new Dictionary<int, long> { [1] = 2, [2] = 5 };
        var counts = new GenreCounts(10, countsByGenre);
        var weights = GenreWeights.Calculate(counts);
        var originalWeight = weights.GetWeight(1);

        // act
        countsByGenre[1] = 9;
        countsByGenre[3] = 4;

        // assert
        weights.GetWeight(1).ShouldBe(originalWeight);
        weights.GetWeight(3).ShouldBe(1.0);
    }

    [Fact]
    public void Similarity_ManyGenres_AccumulatesUnionWeightInAscendingGenreOrder()
    {
        // arrange
        var counts = new GenreCounts(1000, Enumerable.Range(1, 60).ToDictionary(i => i, i => (long)i));
        var weights = GenreWeights.Calculate(counts);

        // act
        var score = weights.Similarity(Enumerable.Range(1, 60), Enumerable.Range(1, 30));

        // assert
        score.ShouldBe(0.5648622875443278);
    }

    private static GenreWeights CreateControlledWeights() =>
        GenreWeights.Calculate(
            new GenreCounts(
                50,
                new Dictionary<int, long>
                {
                    [1] = 1,
                    [2] = 5,
                    [3] = 20,
                    [4] = 45,
                    [5] = 50,
                }));
}

public sealed class GenreWeightsPropertyTests
{
    [Property(MaxTest = 100, Replay = "337001,337003")]
    [Trait("Category", "Property")]
    public bool Similarity_ReorderedAndDuplicatedSets_ReturnsSameScore(int[]? first, int[]? second)
    {
        var weights = CreateControlledWeights();
        var left = first ?? [];
        var right = second ?? [];
        var permutedLeft = left.Reverse().Concat(left).ToArray();
        var permutedRight = right.Reverse().Concat(right).ToArray();

        return Math.Abs(weights.Similarity(left, right) - weights.Similarity(permutedLeft, permutedRight)) < 1e-12;
    }

    [Property(MaxTest = 100, Replay = "337003,337005")]
    [Trait("Category", "Property")]
    public bool Similarity_ArgumentsSwapped_ReturnsSameScore(int[]? first, int[]? second)
    {
        var weights = CreateControlledWeights();
        var left = first ?? [];
        var right = second ?? [];

        return Math.Abs(weights.Similarity(left, right) - weights.Similarity(right, left)) < 1e-12;
    }

    [Property(MaxTest = 100, Replay = "337005,337007")]
    [Trait("Category", "Property")]
    public bool Similarity_AnyGenreSets_ReturnsFiniteScoreWithinBounds(int[]? first, int[]? second)
    {
        var weights = CreateControlledWeights();

        var score = weights.Similarity(first ?? [], second ?? []);

        return double.IsFinite(score) && score >= 0.0 && score <= 1.0;
    }

    private static GenreWeights CreateControlledWeights() =>
        GenreWeights.Calculate(
            new GenreCounts(
                50,
                new Dictionary<int, long>
                {
                    [1] = 1,
                    [2] = 5,
                    [3] = 20,
                    [4] = 45,
                    [5] = 50,
                }));
}
