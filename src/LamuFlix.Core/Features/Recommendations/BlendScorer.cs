using System;
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

        ValidateCoefficient(options.BlendSelectedWeight, nameof(options));
        ValidateCoefficient(options.BlendProfileWeight, nameof(options));
        ValidateCoefficient(options.AlreadyWatchedPenalty, nameof(options));
        ValidateCoefficient(options.NotInterestedPenalty, nameof(options));

        ValidateTerm(selectedSimilarity, nameof(selectedSimilarity));
        ValidateTerm(profileSimilarity, nameof(profileSimilarity));
        ValidateTerm(alreadyWatched, nameof(alreadyWatched));
        ValidateTerm(notInterested, nameof(notInterested));

        var score = (options.BlendSelectedWeight * selectedSimilarity)
            + (options.BlendProfileWeight * profileSimilarity)
            - (options.AlreadyWatchedPenalty * alreadyWatched)
            - (options.NotInterestedPenalty * notInterested);

        return double.IsFinite(score)
            ? score
            : throw new InvalidOperationException("The calculated blend score must be finite.");
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
