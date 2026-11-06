using System;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public static class RecommendationScoring
{
    public static double ProfileContribution(
        double baselineSimilarity,
        ReviewAffinityEvidence reviewAffinity,
        RecommendationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return baselineSimilarity
            + (options.LikedReviewWeight * reviewAffinity.LikedAffinity)
            - (options.DislikedReviewWeight * reviewAffinity.DislikedAffinity);
    }

    public static ImmutableArray<string> Genres(MovieFeatures features)
    {
        ArgumentNullException.ThrowIfNull(features);
        return [.. features.Genres.Order(StringComparer.Ordinal)];
    }

    public static ImmutableArray<string> Cast(MovieFeatures features)
    {
        ArgumentNullException.ThrowIfNull(features);
        return [.. features.Cast.Order(StringComparer.Ordinal)];
    }

    public static ImmutableArray<string> Directors(MovieFeatures features)
    {
        ArgumentNullException.ThrowIfNull(features);
        return [.. features.Directors.Order(StringComparer.Ordinal)];
    }
}
