using System;
using System.Collections.Generic;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

public sealed record OutsideLibraryRecommendationContribution(
    MovieId SourceMovieId,
    string SeedTitle,
    Review Review,
    bool IsWatched,
    bool IsInWatchlist,
    int TmdbId,
    string? CandidateTitle,
    int? CandidateReleaseYear,
    string? CandidatePosterPath);

public sealed record OutsideLibraryMovieIdentity(
    int? TmdbId,
    string Title,
    int? ReleaseYear);

public sealed record OutsideLibraryRecommendationSnapshot
{
    public OutsideLibraryRecommendationSnapshot(
        IReadOnlyList<OutsideLibraryRecommendationContribution> contributions,
        IReadOnlyList<OutsideLibraryMovieIdentity> library)
    {
        ArgumentNullException.ThrowIfNull(contributions);
        ArgumentNullException.ThrowIfNull(library);

        Contributions = contributions;
        Library = library;
    }

    public IReadOnlyList<OutsideLibraryRecommendationContribution> Contributions { get; init; }

    public IReadOnlyList<OutsideLibraryMovieIdentity> Library { get; init; }
}
