using System;
using System.Collections.Immutable;
using System.Linq;

namespace LamuFlix.Core.Features.Recommendations;

public static class RecommendationScoring
{
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
