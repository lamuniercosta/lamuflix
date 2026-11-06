using System;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;

namespace LamuFlix.Core.Features.Recommendations;

public static class MovieSummaryMapper
{
    public static MovieSummary From(Movie movie)
    {
        ArgumentNullException.ThrowIfNull(movie);

        var metadata = movie.Metadata;

        return new MovieSummary(
            movie.Id,
            movie.Title,
            metadata?.PosterUrl,
            metadata?.ReleaseYear?.Value,
            metadata?.Runtime?.Minutes,
            metadata?.ImdbRating?.Value,
            movie.Status,
            movie.IsInWatchlist,
            movie.IsWatched,
            movie.Review);
    }
}