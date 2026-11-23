using LamuFlix.Core.Domain;

namespace LamuFlix.Infrastructure.Persistence.Records;

public sealed class MovieRecommendationRecord
{
    public MovieId? SourceMovieId { get; set; }

    public int RecommendedTmdbId { get; set; }

    public MovieRecord? SourceMovie { get; set; }
}
