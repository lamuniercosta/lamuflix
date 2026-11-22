using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;

namespace LamuFlix.Core.Features.Recommendations;

/// <summary>
/// The Crowd criterion activation state for one scoring pass: the configured activation switch
/// combined with the TMDb configuration predicate, plus the immutable link lookup resolved for the
/// same library feature load. Scoring stays pure because every other input is already in memory.
/// </summary>
public sealed class CrowdScoring
{
    private CrowdScoring(bool active, CrowdLinks links)
    {
        Active = active;
        Links = links;
    }

    /// <summary>Gets the disabled state: no Crowd criterion, no Crowd weight in the denominator.</summary>
    public static CrowdScoring Disabled { get; } = new(false, CrowdLinks.Empty);

    /// <summary>
    /// Gets whether the Crowd criterion participates in scoring. It does only when
    /// <see cref="RecommendationOptions.CrowdEnabled"/> is on and TMDb is configured with a
    /// nonblank API key; enrichment can stay enabled while Crowd scoring is off.
    /// </summary>
    public bool Active { get; }

    /// <summary>Gets the symmetric binary link lookup for the current library feature load.</summary>
    public CrowdLinks Links { get; }

    /// <summary>Resolves the activation state for one scoring pass.</summary>
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

    /// <summary>
    /// Returns the binary Crowd value: 1 when either library movie's stored recommendations include
    /// the other, 0 otherwise. Distinct catalogue rows that merely share a TMDb id never link, and a
    /// movie never links to itself.
    /// </summary>
    public bool IsLinked(MovieId? first, MovieId? second) =>
        Active
        && first is not null
        && second is not null
        && Links.IsLinked(first, second);

    private static bool IsActive(RecommendationOptions recommendations, TmdbOptions tmdb) =>
        recommendations.CrowdEnabled && tmdb.Enabled && !string.IsNullOrWhiteSpace(tmdb.ApiKey);
}
