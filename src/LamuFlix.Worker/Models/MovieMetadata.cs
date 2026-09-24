using System.Collections.Generic;

namespace LamuFlix.Worker.Models
{
    public record MovieMetadata(
        string? Title,
        string? Synopsis,
        int? Year,
        int? DurationMinutes,
        string? PosterUrl,
        decimal? ImdbRating,
        int? RottenTomatoesScore,
        int? MetaScore,
        string? ImdbId,
        IReadOnlyList<string>? Genres,
        IReadOnlyList<string>? Actors,
        IReadOnlyList<string>? Directors
    );
}
