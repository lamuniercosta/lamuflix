using System;
using System.Collections.Generic;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class BlendScorer
{
    public static double Score(
        double selectedSimilarity,
        double alreadyWatched,
        RecommendationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ValidateOptions(options);

        ValidateTerm(selectedSimilarity, nameof(selectedSimilarity));
        ValidateTerm(alreadyWatched, nameof(alreadyWatched));

        return Combine(selectedSimilarity, alreadyWatched, options);
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
        ValidateCoefficient(options.AlreadyWatchedPenalty, nameof(options));
    }

    private static double Combine(
        double selectedSimilarity,
        double alreadyWatched,
        RecommendationOptions options)
    {
        return (options.BlendSelectedWeight * selectedSimilarity)
            - (options.AlreadyWatchedPenalty * alreadyWatched);
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
