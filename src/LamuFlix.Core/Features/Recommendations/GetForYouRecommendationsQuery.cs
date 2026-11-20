using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

public sealed record GetForYouRecommendationsQuery
{
    public GetForYouRecommendationsQuery(
        int take = 10,
        int offset = 0,
        ImmutableHashSet<MovieId>? excludedMovieIds = null)
    {
        Take = take;
        Offset = offset;
        ExcludedMovieIds = excludedMovieIds ?? ImmutableHashSet<MovieId>.Empty;
    }

    public int Take { get; init; }

    public int Offset { get; init; }

    public ImmutableHashSet<MovieId> ExcludedMovieIds { get; init; }
}
