using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

public sealed record MovieFeatures
{
    public MovieFeatures(
        IEnumerable<string>? genres = null,
        IEnumerable<string>? directors = null,
        IEnumerable<string>? writers = null,
        IEnumerable<string>? cast = null,
        ReleaseYear? releaseYear = null,
        Runtime? runtime = null,
        ImdbRating? imdbRating = null,
        int? metaScore = null,
        string? fullPlot = null,
        IEnumerable<int>? keywords = null,
        int? collectionId = null)
    {
        RequirePositiveCollectionId(collectionId);

        Genres = Normalize(genres);
        Directors = Normalize(directors);
        Writers = Normalize(writers);
        Cast = Normalize(cast);
        ReleaseYear = releaseYear;
        Runtime = runtime;
        ImdbRating = imdbRating;
        MetaScore = metaScore;
        FullPlot = fullPlot;
        Keywords = NormalizeKeywords(keywords);
        CollectionId = collectionId;
    }

    public ImmutableHashSet<string> Genres { get; }

    public ImmutableHashSet<string> Directors { get; }

    public ImmutableHashSet<string> Writers { get; }

    public ImmutableHashSet<string> Cast { get; }

    public ReleaseYear? ReleaseYear { get; }

    public Runtime? Runtime { get; }

    public ImdbRating? ImdbRating { get; }

    public int? MetaScore { get; }

    public string? FullPlot { get; }

    public ImmutableHashSet<int> Keywords { get; }

    public int? CollectionId { get; }

    public bool Equals(MovieFeatures? other) =>
        other is not null && SetsEqual(other) && ScalarsEqual(other);

    private bool SetsEqual(MovieFeatures other) =>
        Genres.SetEquals(other.Genres)
        && Directors.SetEquals(other.Directors)
        && Writers.SetEquals(other.Writers)
        && Cast.SetEquals(other.Cast)
        && Keywords.SetEquals(other.Keywords);

    private bool ScalarsEqual(MovieFeatures other) =>
        ReleaseYear == other.ReleaseYear
        && Runtime == other.Runtime
        && ImdbRating == other.ImdbRating
        && MetaScore == other.MetaScore
        && CollectionId == other.CollectionId
        && string.Equals(FullPlot, other.FullPlot, StringComparison.Ordinal);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SetHash(Genres));
        hash.Add(SetHash(Directors));
        hash.Add(SetHash(Writers));
        hash.Add(SetHash(Cast));
        hash.Add(KeywordSetHash(Keywords));
        hash.Add(ReleaseYear);
        hash.Add(Runtime);
        hash.Add(ImdbRating);
        hash.Add(MetaScore);
        hash.Add(CollectionId);
        hash.Add(FullPlot is null ? 0 : StringComparer.Ordinal.GetHashCode(FullPlot));
        return hash.ToHashCode();
    }

    private static void RequirePositiveCollectionId(int? collectionId)
    {
        if (collectionId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(collectionId), collectionId, "Collection id must be positive.");
        }
    }

    private static ImmutableHashSet<int> NormalizeKeywords(IEnumerable<int>? values) =>
        values is null
            ? ImmutableHashSet<int>.Empty
            : ImmutableHashSet.CreateRange(values);

    private static ImmutableHashSet<string> Normalize(IEnumerable<string>? values) =>
        values is null
            ? ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase)
            : ImmutableHashSet.CreateRange(StringComparer.OrdinalIgnoreCase, values);

    private static int SetHash(ImmutableHashSet<string> values)
    {
        var hash = 0;
        // ReSharper disable once VariableCanBeNotNullable // sets under test intentionally contain null members that violate the annotation.
        foreach (object? value in values)
        {
            hash = unchecked(hash + (value is string text ? StringComparer.OrdinalIgnoreCase.GetHashCode(text) : 0));
        }

        return hash;
    }

    private static int KeywordSetHash(ImmutableHashSet<int> values)
    {
        var hash = 0;
        foreach (var value in values)
        {
            hash = unchecked(hash + value.GetHashCode());
        }

        return hash;
    }
}
