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
        && Title == other.Title
        && Synopsis == other.Synopsis
        && Equals(ReleaseYear, other.ReleaseYear)
        && Equals(Runtime, other.Runtime)
        && Equals(ImdbRating, other.ImdbRating)
        && Equals(ImdbId, other.ImdbId)
        && PosterUrl == other.PosterUrl
        && RottenTomatoesRating == other.RottenTomatoesRating
        && MetaScore == other.MetaScore
        && Directors.SequenceEqual(other.Directors)
        && Actors.SequenceEqual(other.Actors);

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
