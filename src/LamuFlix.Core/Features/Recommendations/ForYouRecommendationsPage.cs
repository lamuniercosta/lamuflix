using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace LamuFlix.Core.Features.Recommendations;

public sealed record ForYouRecommendationsPage(ImmutableList<RecommendedMovie> Items, bool HasMore) : IReadOnlyList<RecommendedMovie>
{
    public int Count => Items.Count;

    public RecommendedMovie this[int index] => Items[index];

    public IEnumerator<RecommendedMovie> GetEnumerator() => Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}