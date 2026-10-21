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
        var counts = new GenreCounts(10, new Dictionary<string, long> { ["Drama"] = 4 });

        // act
        var weights = GenreWeights.Calculate(counts);

        // assert
        weights.GetWeight("Drama").ShouldBe(Math.Log(11.0 / 5.0) + 1.0, 1e-12);
    }

    [Fact]
    public void Calculate_GenreNameCaseVariant_ReturnsSameCaseInsensitiveWeight()
    {
        // arrange
        var counts = new GenreCounts(10, new Dictionary<string, long> { ["Drama"] = 4 });

        // act
        var weights = GenreWeights.Calculate(counts);

        // assert
        weights.GetWeight("drama").ShouldBe(Math.Log(11.0 / 5.0) + 1.0, 1e-12);
        weights.GetWeight("DRAMA").ShouldBe(weights.GetWeight("Drama"));
    }

    [Fact]
    public void Calculate_ZeroCountGenreInNonEmptyLibrary_UsesZeroFrequencyFormula()
    {
        // arrange
        var counts = new GenreCounts(7, new Dictionary<string, long> { ["Comedy"] = 0 });

        // act
        var weights = GenreWeights.Calculate(counts);

        // assert
        weights.GetWeight("Comedy").ShouldBe(Math.Log(8.0) + 1.0, 1e-12);
    }

    [Fact]
    public void Similarity_RareGenreSharedUnderCommonUnion_ScoresHigherThanCommonSharedGenre()
    {
        // arrange
        var counts = new GenreCounts(
            100,
            new Dictionary<string, long>
            {
                ["Rare"] = 1,
                ["Common"] = 90,
                ["Shared"] = 50,
            });
        var weights = GenreWeights.Calculate(counts);

        // act
        var rareShared = weights.Similarity(["Rare"], ["Rare", "Shared"]);
        var commonShared = weights.Similarity(["Common"], ["Common", "Shared"]);

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
        var counts = new GenreCounts(-1, new Dictionary<string, long>());

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
        var counts = new GenreCounts(5, new Dictionary<string, long> { ["Drama"] = -1 });

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
        var counts = new GenreCounts(2, new Dictionary<string, long> { ["Drama"] = 3 });

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
        var counts = new GenreCounts(0, new Dictionary<string, long> { ["Drama"] = 0 });

        // act
        var weights = GenreWeights.Calculate(counts);

        // assert
        weights.GetWeight("Drama").ShouldBe(1.0);
    }

    [Fact]
    public void GetWeight_UnknownGenre_ReturnsOne()
    {
        // arrange
        var counts = new GenreCounts(10, new Dictionary<string, long> { ["Drama"] = 2 });

        // act
        var weights = GenreWeights.Calculate(counts);

        // assert
        weights.GetWeight("Unknown").ShouldBe(1.0);
    }

    [Fact]
    public void GetWeight_NullOrBlankGenre_ReturnsDefaultWithoutThrowing()
    {
        // arrange
        var counts = new GenreCounts(10, new Dictionary<string, long> { ["Drama"] = 2 });
        var weights = GenreWeights.Calculate(counts);

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the safe handling
        var nullWeight = weights.GetWeight(null!);
        var blankWeight = weights.GetWeight("   ");

        // assert
        nullWeight.ShouldBe(1.0);
        blankWeight.ShouldBe(1.0);
    }

    [Fact]
    public void Similarity_NullFirst_ThrowsArgumentNullException()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the guard
        var failure = Should.Throw<ArgumentNullException>(() => weights.Similarity(null!, ["G1"]));

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
        var failure = Should.Throw<ArgumentNullException>(() => weights.Similarity(["G1"], null!));

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
        var score = weights.Similarity(["G1", "G3", "G5"], ["G5", "G3", "G1"]);

        // assert
        score.ShouldBe(1.0);
    }

    [Fact]
    public void Similarity_CaseVariantIdenticalSets_ReturnsOne()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        var score = weights.Similarity(["G1", "G3"], ["g1", "g3"]);

        // assert
        score.ShouldBe(1.0);
    }

    [Fact]
    public void Similarity_DisjointSets_ReturnsZero()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        var score = weights.Similarity(["G1", "G2"], ["G3", "G4"]);

        // assert
        score.ShouldBe(0.0);
    }

    [Fact]
    public void Similarity_DuplicateGenres_MatchesDeduplicatedScore()
    {
        // arrange
        var weights = CreateControlledWeights();
        var expected = weights.Similarity(["G1", "G2"], ["G2", "G3"]);

        // act
        var duplicated = weights.Similarity(["G1", "G1", "G2", "G2", "G2"], ["G3", "G2", "g2", "G3"]);

        // assert
        duplicated.ShouldBe(expected);
    }

    [Fact]
    public void Similarity_PermutedGenreOrder_ReturnsSameScore()
    {
        // arrange
        var weights = CreateControlledWeights();
        var expected = weights.Similarity(["G1", "G2", "G3"], ["G2", "G3", "G4"]);

        // act
        var permuted = weights.Similarity(["G3", "G1", "G2"], ["G4", "G2", "G3"]);

        // assert
        permuted.ShouldBe(expected);
    }

    [Fact]
    public void Similarity_ArgumentsSwapped_ReturnsSameScore()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        var forward = weights.Similarity(["G1", "G2", "G4"], ["G2", "G5"]);
        var reverse = weights.Similarity(["G2", "G5"], ["G1", "G2", "G4"]);

        // assert
        reverse.ShouldBe(forward);
    }

    [Fact]
    public void Similarity_OverlappingSets_StaysWithinBounds()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        var score = weights.Similarity(["G1", "G2", "G3", "G4", "G5"], ["G2", "G4", "G6"]);

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
        var score = weights.Similarity(["G1", "G2", "G3"], ["G3", "G4", "G5"]);

        // assert
        double.IsFinite(score).ShouldBeTrue();
    }

    [Fact]
    public void Similarity_NullAndBlankMembers_AreIgnoredWithoutThrowing()
    {
        // arrange
        var weights = CreateControlledWeights();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed // deliberate null exercises the safe handling
        var score = weights.Similarity([null!, "G1", "   "], ["g1", ""]);

        // assert
        score.ShouldBe(1.0);
    }

    [Fact]
    public void Similarity_ControlledWeights_DoesNotApplyOverallGenreCoefficient()
    {
        // arrange
        var counts = new GenreCounts(
            3,
            new Dictionary<string, long>
            {
                ["Alpha"] = 1,
                ["Beta"] = 2,
            });
        var weights = GenreWeights.Calculate(counts);
        var genreOneWeight = Math.Log(4.0 / 2.0) + 1.0;
        var genreTwoWeight = Math.Log(4.0 / 3.0) + 1.0;
        var expected = genreOneWeight / (genreOneWeight + genreTwoWeight);

        // act
        var score = weights.Similarity(["Alpha", "Beta"], ["Alpha"]);

        // assert
        score.ShouldBe(expected, 1e-12);
    }

    [Fact]
    public void Calculate_InputMapMutatedAfterCalculation_LeavesPublishedWeightsUnchanged()
    {
        // arrange
        var countsByGenre = new Dictionary<string, long> { ["Alpha"] = 2, ["Beta"] = 5 };
        var counts = new GenreCounts(10, countsByGenre);
        var weights = GenreWeights.Calculate(counts);
        var originalWeight = weights.GetWeight("Alpha");

        // act
        countsByGenre["Alpha"] = 9;
        countsByGenre["Gamma"] = 4;

        // assert
        weights.GetWeight("Alpha").ShouldBe(originalWeight);
        weights.GetWeight("Gamma").ShouldBe(1.0);
    }

    [Fact]
    public void Similarity_ManyGenres_AccumulatesUnionWeightInAscendingGenreOrder()
    {
        // arrange
        var counts = new GenreCounts(1000, Enumerable.Range(1, 60).ToDictionary(i => $"{i:D2}", i => (long)i));
        var weights = GenreWeights.Calculate(counts);

        // act
        var score = weights.Similarity(
            Enumerable.Range(1, 60).Select(i => $"{i:D2}"),
            Enumerable.Range(1, 30).Select(i => $"{i:D2}"));

        // assert
        score.ShouldBe(0.5648622875443278);
    }

    private static GenreWeights CreateControlledWeights() =>
        GenreWeights.Calculate(
            new GenreCounts(
                50,
                new Dictionary<string, long>
                {
                    ["G1"] = 1,
                    ["G2"] = 5,
                    ["G3"] = 20,
                    ["G4"] = 45,
                    ["G5"] = 50,
                }));
}

public sealed class GenreWeightsPropertyTests
{
    [Property(MaxTest = 100, Replay = "337001,337003")]
    [Trait("Category", "Property")]
    public bool Similarity_ReorderedAndDuplicatedSets_ReturnsSameScore(string[]? first, string[]? second)
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
    public bool Similarity_ArgumentsSwapped_ReturnsSameScore(string[]? first, string[]? second)
    {
        var weights = CreateControlledWeights();
        var left = first ?? [];
        var right = second ?? [];

        return Math.Abs(weights.Similarity(left, right) - weights.Similarity(right, left)) < 1e-12;
    }

    [Property(MaxTest = 100, Replay = "337005,337007")]
    [Trait("Category", "Property")]
    public bool Similarity_AnyGenreSets_ReturnsFiniteScoreWithinBounds(string[]? first, string[]? second)
    {
        var weights = CreateControlledWeights();

        var score = weights.Similarity(first ?? [], second ?? []);

        return double.IsFinite(score) && score >= 0.0 && score <= 1.0;
    }

    private static GenreWeights CreateControlledWeights() =>
        GenreWeights.Calculate(
            new GenreCounts(
                50,
                new Dictionary<string, long>
                {
                    ["G1"] = 1,
                    ["G2"] = 5,
                    ["G3"] = 20,
                    ["G4"] = 45,
                    ["G5"] = 50,
                }));
}
