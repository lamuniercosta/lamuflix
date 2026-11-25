namespace LamuFlix.Core.Features.Recommendations;

public sealed record OutsideLibraryRecommendation(
    int TmdbId,
    string Title,
    int? ReleaseYear,
    string? PosterPath,
    string Reason);
