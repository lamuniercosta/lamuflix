using System;
using System.Collections.Generic;
using System.Linq;

namespace LamuFlix.Core.Domain;

public sealed record MovieMetadata
{
    public MovieMetadata(
        string title,
        string? synopsis = null,
        ReleaseYear? releaseYear = null,
        Runtime? runtime = null,
        ImdbRating? imdbRating = null,
        ImdbId? imdbId = null,
        string? posterUrl = null,
        short? rottenTomatoesRating = null,
        short? metaScore = null,
        IEnumerable<string>? directors = null,
        IEnumerable<string>? actors = null)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        Title = title;
        Synopsis = synopsis;
        ReleaseYear = releaseYear;
        Runtime = runtime;
        ImdbRating = imdbRating;
        ImdbId = imdbId;
        PosterUrl = posterUrl;
        RottenTomatoesRating = rottenTomatoesRating;
        MetaScore = metaScore;
        Directors = directors?.ToArray() ?? [];
        Actors = actors?.ToArray() ?? [];
    }

    public string Title { get; }

    public string? Synopsis { get; }

    public ReleaseYear? ReleaseYear { get; }

    public Runtime? Runtime { get; }

    public ImdbRating? ImdbRating { get; }

    public ImdbId? ImdbId { get; }

    public string? PosterUrl { get; }

    public short? RottenTomatoesRating { get; }

    public short? MetaScore { get; }

    public IReadOnlyList<string> Directors { get; }

    public IReadOnlyList<string> Actors { get; }

    public bool Equals(MovieMetadata? other) =>
        other is not null
        && SameScalars(this, other)
        && SameOptionals(this, other)
        && SameCollections(this, other);

    private static bool SameScalars(MovieMetadata left, MovieMetadata right) =>
        left.Title == right.Title
        && left.Synopsis == right.Synopsis
        && left.PosterUrl == right.PosterUrl
        && left.RottenTomatoesRating == right.RottenTomatoesRating
        && left.MetaScore == right.MetaScore;

    private static bool SameOptionals(MovieMetadata left, MovieMetadata right) =>
        Equals(left.ReleaseYear, right.ReleaseYear)
        && Equals(left.Runtime, right.Runtime)
        && Equals(left.ImdbRating, right.ImdbRating)
        && Equals(left.ImdbId, right.ImdbId);

    private static bool SameCollections(MovieMetadata left, MovieMetadata right) =>
        left.Directors.SequenceEqual(right.Directors)
        && left.Actors.SequenceEqual(right.Actors);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Title);
        hash.Add(Synopsis);
        hash.Add(ReleaseYear);
        hash.Add(Runtime);
        hash.Add(ImdbRating);
        hash.Add(ImdbId);
        hash.Add(PosterUrl);
        hash.Add(RottenTomatoesRating);
        hash.Add(MetaScore);
        foreach (var director in Directors)
        {
            hash.Add(director);
        }
        foreach (var actor in Actors)
        {
            hash.Add(actor);
        }
        return hash.ToHashCode();
    }
}
