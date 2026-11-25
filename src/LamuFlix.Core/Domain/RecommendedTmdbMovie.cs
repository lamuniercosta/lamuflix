namespace LamuFlix.Core.Domain;

public sealed record RecommendedTmdbMovie(
    int TmdbId,
    string? Title,
    int? ReleaseYear,
    string? PosterPath)
{
    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

    public static RecommendedTmdbMovie FromId(int tmdbId) => new(tmdbId, null, null, null);
}
