using System;
using System.Collections.Generic;
using System.Linq;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

public static class MovieFeaturesMapper
{
    public static MovieFeatures From(Movie movie) => From(movie, includeSignals: false);

    public static MovieFeatures From(Movie movie, bool includeSignals, IEnumerable<float>? plotEmbedding = null)
    {
        ArgumentNullException.ThrowIfNull(movie);

        var metadata = movie.Metadata;
        if (metadata is null)
        {
            // The optional plot embedding survives a missing metadata projection: it is the Plot criterion input.
            return new MovieFeatures(plotEmbedding: plotEmbedding);
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
            metadata.CollectionId,
            KeywordNames(signals),
            includeSignals ? metadata.CollectionName : null,
            plotEmbedding);
    }

    private static IReadOnlyList<string> MergeCast(
        IReadOnlyList<string> actors,
        MovieSignalSnapshot? signals) =>
        signals is null ? actors : [.. actors, .. signals.FullCast];

    private static IEnumerable<int>? KeywordIds(MovieSignalSnapshot? signals) =>
        signals?.Keywords.Select(keyword => keyword.Id);

    private static IEnumerable<string>? KeywordNames(MovieSignalSnapshot? signals) =>
        signals?.Keywords.Select(keyword => keyword.Name);
}
