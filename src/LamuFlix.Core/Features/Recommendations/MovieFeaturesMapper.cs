using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

public static class MovieFeaturesMapper
{
    public static MovieFeatures From(Movie movie) => From(movie, includeSignals: false);

    public static MovieFeatures From(Movie movie, bool includeSignals)
    {
        ArgumentNullException.ThrowIfNull(movie);

        var metadata = movie.Metadata;
        if (metadata is null)
        {
            return new MovieFeatures();
        }

        var signals = includeSignals ? metadata.Signals : null;

        return new MovieFeatures(
            metadata.Genres,
            metadata.Directors,
            metadata.Writers,
            MergeCast(metadata.Actors, signals),
            metadata.ReleaseYear,
            metadata.Runtime,
            metadata.ImdbRating,
            metadata.MetaScore,
            metadata.Synopsis,
            KeywordIds(signals),
            includeSignals ? metadata.CollectionId : null);
    }

    private static IReadOnlyList<string> MergeCast(
        IReadOnlyList<string> actors,
        MovieSignalSnapshot? signals) =>
        signals is null ? actors : [.. actors, .. signals.FullCast];

    private static IEnumerable<int>? KeywordIds(MovieSignalSnapshot? signals) =>
        signals?.Keywords.Select(keyword => keyword.Id);
}
