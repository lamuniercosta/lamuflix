using System;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

public static class MovieFeaturesMapper
{
    public static MovieFeatures From(Movie movie)
    {
        ArgumentNullException.ThrowIfNull(movie);

        var metadata = movie.Metadata;
        if (metadata is null)
        {
            return new MovieFeatures();
        }

        return new MovieFeatures(
            metadata.Genres,
            metadata.Directors,
            metadata.Writers,
            metadata.Actors,
            metadata.ReleaseYear,
            metadata.Runtime,
            metadata.ImdbRating,
            metadata.MetaScore,
            metadata.Synopsis);
    }
}
