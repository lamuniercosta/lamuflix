using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Recommendations;

// ReSharper disable NotAccessedPositionalProperty.Global // Public port contract consumed by callers and tests
public sealed record RecommendationLibraryEntry(MovieFeatures Features, MovieSummary Summary)
{
    public MovieId Id => Summary.Id;

    public bool IsWatched => Summary.IsWatched;

    public bool IsInWatchlist => Summary.IsInWatchlist;

    public Review Review => Summary.Review;
}

public sealed record RecommendationCandidate(RecommendationLibraryEntry Library, double CosineSimilarity);
// ReSharper restore NotAccessedPositionalProperty.Global
