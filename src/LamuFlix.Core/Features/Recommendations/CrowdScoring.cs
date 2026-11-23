using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class CrowdScoring
{
    private CrowdScoring(bool active, CrowdLinks links)
    {
        Active = active;
        Links = links;
    }

    public static CrowdScoring Disabled { get; } = new(false, CrowdLinks.Empty);

    public bool Active { get; }

    private CrowdLinks Links { get; }

    public static CrowdScoring Resolve(
        RecommendationOptions recommendations,
        TmdbOptions tmdb,
        CrowdLinks? links = null)
    {
        ArgumentNullException.ThrowIfNull(recommendations);
        ArgumentNullException.ThrowIfNull(tmdb);

        return IsActive(recommendations, tmdb)
            ? new CrowdScoring(true, links ?? CrowdLinks.Empty)
            : Disabled;
    }

    public bool IsLinked(MovieId? first, MovieId? second) =>
        Active
        && first is not null
        && second is not null
        && Links.IsLinked(first, second);

    private static bool IsActive(RecommendationOptions recommendations, TmdbOptions tmdb) =>
        recommendations.CrowdEnabled && tmdb.Enabled && !string.IsNullOrWhiteSpace(tmdb.ApiKey);
}

