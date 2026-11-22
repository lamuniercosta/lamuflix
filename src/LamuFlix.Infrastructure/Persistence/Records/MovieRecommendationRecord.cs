using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.Persistence.Records;

/// <summary>
/// One first-page TMDb recommendation stored for the owning library movie.
/// The target id may name a movie outside the library and is retained for a later library addition.
/// </summary>
public sealed class MovieRecommendationRecord
{
    public MovieId? SourceMovieId { get; set; }

    public int RecommendedTmdbId { get; set; }

    public MovieRecord? SourceMovie { get; set; }
}
