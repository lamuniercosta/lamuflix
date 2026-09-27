using System;

namespace LamuFlix.Core.Domain;

public sealed record MovieMetadata
{
    public MovieMetadata(
        string title,
        string? synopsis = null,
        ReleaseYear? releaseYear = null,
        Runtime? runtime = null,
        ImdbRating? imdbRating = null,
        ImdbId? imdbId = null)
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
    }

    public string Title { get; }

    public string? Synopsis { get; }

    public ReleaseYear? ReleaseYear { get; }

    public Runtime? Runtime { get; }

    public ImdbRating? ImdbRating { get; }

    public ImdbId? ImdbId { get; }

    public bool Equals(MovieMetadata? other) => other is not null && State() == other.State();

    public override int GetHashCode() => State().GetHashCode();

    private (string Title, string? Synopsis, ReleaseYear? ReleaseYear, Runtime? Runtime, ImdbRating? ImdbRating, ImdbId? ImdbId) State() =>
        (Title, Synopsis, ReleaseYear, Runtime, ImdbRating, ImdbId);
}
