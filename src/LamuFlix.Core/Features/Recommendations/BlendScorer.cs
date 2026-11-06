using System;
using System.Collections.Generic;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class BlendScorer
{
    public static double Score(
        double selectedSimilarity,
        double profileSimilarity,
        double alreadyWatched,
        double notInterested,
        RecommendationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidateOptions(options);

        ValidateTerm(selectedSimilarity, nameof(selectedSimilarity));
        ValidateTerm(profileSimilarity, nameof(profileSimilarity));
        ValidateTerm(alreadyWatched, nameof(alreadyWatched));
        ValidateTerm(notInterested, nameof(notInterested));

        return Combine(selectedSimilarity, profileSimilarity, alreadyWatched, notInterested, options);
    }

    private static void ValidateCoefficient(double value, string paramName)
    {
        if (!double.IsFinite(value) || value < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                "Blend coefficients must be non-negative and finite.");
        }
    }

    public static double ScoreWithProfileContribution(
        double selectedSimilarity,
        double profileContribution,
        double alreadyWatched,
        double notInterested,
        RecommendationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidateOptions(options);

        ValidateTerm(selectedSimilarity, nameof(selectedSimilarity));
        ValidateTerm(alreadyWatched, nameof(alreadyWatched));
        ValidateTerm(notInterested, nameof(notInterested));

        if (!double.IsFinite(profileContribution))
        {
            throw new ArgumentOutOfRangeException(
                nameof(profileContribution),
                profileContribution,
                "Profile contribution must be finite.");
        }

        return Combine(selectedSimilarity, profileContribution, alreadyWatched, notInterested, options);
    }

    public static double NonNegativeOffset(IEnumerable<double> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);

        var minimum = double.PositiveInfinity;

        foreach (var score in scores)
        {
            if (score < minimum)
            {
                minimum = score;
            }
        }

        return minimum < 0.0 ? -minimum : 0.0;
    }

    private static void ValidateOptions(RecommendationOptions options)
    {
        ValidateCoefficient(options.BlendSelectedWeight, nameof(options));
        ValidateCoefficient(options.BlendProfileWeight, nameof(options));
        ValidateCoefficient(options.AlreadyWatchedPenalty, nameof(options));
        ValidateCoefficient(options.NotInterestedPenalty, nameof(options));
    }

    private static double Combine(
        double selectedSimilarity,
        double profileSimilarity,
        double alreadyWatched,
        double notInterested,
        RecommendationOptions options)
    {
        var score = (options.BlendSelectedWeight * selectedSimilarity)
            + (options.BlendProfileWeight * profileSimilarity)
            - (options.AlreadyWatchedPenalty * alreadyWatched)
            - (options.NotInterestedPenalty * notInterested);

        return double.IsFinite(score)
            ? score
            : throw new InvalidOperationException("The calculated blend score must be finite.");
    }

    private static void ValidateTerm(double value, string paramName)
    {
        if (!double.IsFinite(value) || value is < 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                "Blend terms must be finite values between 0.0 and 1.0.");
        }
    }
}
