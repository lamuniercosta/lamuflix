using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Evaluation.Replay;

namespace LamuFlix.Evaluation.Fixtures;

public sealed record EvaluationCatalogMovie
{
    public EvaluationCatalogMovie(MovieId movieId, MovieFeatures features)
        : this(movieId, features, null, null, null)
    {
    }

    public EvaluationCatalogMovie(
        MovieId movieId,
        MovieFeatures features,
        IEnumerable<int>? tmdbKeywords,
        IEnumerable<string>? tmdbFullCast,
        int? collectionId)
    {
        ArgumentNullException.ThrowIfNull(features);

        MovieId = movieId;
        Features = features;
        TmdbKeywords = tmdbKeywords is null
            ? ImmutableHashSet<int>.Empty
            : ImmutableHashSet.CreateRange(tmdbKeywords);
        TmdbFullCast = tmdbFullCast is null
            ? ImmutableHashSet<string>.Empty
            : ImmutableHashSet.CreateRange(StringComparer.OrdinalIgnoreCase, tmdbFullCast);
        CollectionId = collectionId;
    }

    public MovieId MovieId { get; }

    public MovieFeatures Features { get; }

    public ImmutableHashSet<int> TmdbKeywords { get; }

    public ImmutableHashSet<string> TmdbFullCast { get; }

    public int? CollectionId { get; }

    public MovieFeatures FeaturesFor(TmdbSignalsMode mode) =>
        mode == TmdbSignalsMode.Enabled ? EnabledFeatures() : Features;

    private MovieFeatures EnabledFeatures() =>
        new(
            Features.Genres,
            Features.Directors,
            Features.Writers,
            [.. Features.Cast, .. TmdbFullCast],
            Features.ReleaseYear,
            Features.Runtime,
            Features.ImdbRating,
            Features.MetaScore,
            Features.FullPlot,
            TmdbKeywords,
            CollectionId);
}
