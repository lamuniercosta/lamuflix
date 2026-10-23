using System;
using FsCheck.Xunit;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;

namespace LamuFlix.UnitTests.Features.Recommendations;

public sealed class BlendScorerPropertyTests
{
    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool Score_ValidInputs_EqualsTheLiteralFormula(
        int selectedSeed,
        int profileSeed,
        int watchedSeed,
        int notInterestedSeed,
        int selectedWeightSeed,
        int profileWeightSeed,
        int watchedPenaltySeed,
        int notInterestedPenaltySeed)
    {
        // arrange
        var selected = Term(selectedSeed);
        var profile = Term(profileSeed);
        var watched = Binary(watchedSeed);
        var notInterested = Binary(notInterestedSeed);
        var options = new RecommendationOptions
        {
            BlendSelectedWeight = Coefficient(selectedWeightSeed),
            BlendProfileWeight = Coefficient(profileWeightSeed),
            AlreadyWatchedPenalty = Coefficient(watchedPenaltySeed),
            NotInterestedPenalty = Coefficient(notInterestedPenaltySeed),
        };
        var expected = (options.BlendSelectedWeight * selected)
            + (options.BlendProfileWeight * profile)
            - (options.AlreadyWatchedPenalty * watched)
            - (options.NotInterestedPenalty * notInterested);

        // act
        var actual = BlendScorer.Score(selected, profile, watched, notInterested, options);

        // assert
        return Math.Abs(actual - expected) <= 1e-9 && double.IsFinite(actual);
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool Score_ValidInputs_IsFiniteEvenOutsideTheUnitInterval(
        int selectedSeed,
        int profileSeed,
        int selectedWeightSeed,
        int profileWeightSeed,
        int watchedPenaltySeed,
        int notInterestedPenaltySeed)
    {
        // arrange
        var options = new RecommendationOptions
        {
            BlendSelectedWeight = Coefficient(selectedWeightSeed),
            BlendProfileWeight = Coefficient(profileWeightSeed),
            AlreadyWatchedPenalty = Coefficient(watchedPenaltySeed),
            NotInterestedPenalty = Coefficient(notInterestedPenaltySeed),
        };

        // act
        var score = BlendScorer.Score(
            Term(selectedSeed),
            Term(profileSeed),
            1.0,
            0.0,
            options);

        // assert
        return double.IsFinite(score);
    }

    [Property(MaxTest = 200)]
    [Trait("Category", "Property")]
    public bool Score_IncreasingWatchedSignal_DoesNotIncreaseScore(
        int selectedSeed,
        int profileSeed,
        int selectedWeightSeed,
        int profileWeightSeed,
        int watchedPenaltySeed)
    {
        // arrange
        var selected = Term(selectedSeed);
        var profile = Term(profileSeed);
        var options = new RecommendationOptions
        {
            BlendSelectedWeight = Coefficient(selectedWeightSeed),
            BlendProfileWeight = Coefficient(profileWeightSeed),
            AlreadyWatchedPenalty = Coefficient(watchedPenaltySeed),
            NotInterestedPenalty = 0.0,
        };

        // act
        var unwatched = BlendScorer.Score(selected, profile, 0.0, 0.0, options);
        var watched = BlendScorer.Score(selected, profile, 1.0, 0.0, options);

        // assert
        return watched <= unwatched + 1e-12;
    }

    [Property(MaxTest = 100)]
    [Trait("Category", "Property")]
    public bool Score_NegativeCoefficient_AlwaysThrows(int coefficientSeed)
    {
        // arrange
        var options = new RecommendationOptions
        {
            BlendSelectedWeight = -Coefficient(coefficientSeed) - 1e-6,
            BlendProfileWeight = 1.0,
            AlreadyWatchedPenalty = 0.25,
            NotInterestedPenalty = 1.0,
        };

        // act / assert
        try
        {
            _ = BlendScorer.Score(0.5, 0.5, 0.0, 0.0, options);
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return true;
        }
    }

    private static double Term(int seed) => Math.Abs(seed % 1000.0) / 1000.0;

    private static double Coefficient(int seed) => Math.Abs(seed % 100.0) / 10.0;

    private static double Binary(int seed) => (seed & 1) == 0 ? 0.0 : 1.0;
}
