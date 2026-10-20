using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
        string? fullPlot = null)
    {
        Genres = Normalize(genres);
        Directors = Normalize(directors);
        Writers = Normalize(writers);
        Cast = Normalize(cast);
        ReleaseYear = releaseYear;
        Runtime = runtime;
        ImdbRating = imdbRating;
        MetaScore = metaScore;
        FullPlot = fullPlot;
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

    public bool Equals(MovieFeatures? other) =>
        other is not null && SetsEqual(other) && ScalarsEqual(other);

    private bool SetsEqual(MovieFeatures other) =>
        Genres.SetEquals(other.Genres)
        && Directors.SetEquals(other.Directors)
        && Writers.SetEquals(other.Writers)
        && Cast.SetEquals(other.Cast);

    private bool ScalarsEqual(MovieFeatures other) =>
        ReleaseYear == other.ReleaseYear
        && Runtime == other.Runtime
        && ImdbRating == other.ImdbRating
        && MetaScore == other.MetaScore
        && string.Equals(FullPlot, other.FullPlot, StringComparison.Ordinal);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SetHash(Genres));
        hash.Add(SetHash(Directors));
        hash.Add(SetHash(Writers));
        hash.Add(SetHash(Cast));
        hash.Add(ReleaseYear);
        hash.Add(Runtime);
        hash.Add(ImdbRating);
        hash.Add(MetaScore);
        hash.Add(FullPlot is null ? 0 : StringComparer.Ordinal.GetHashCode(FullPlot));
        return hash.ToHashCode();
    }

    private static ImmutableHashSet<string> Normalize(IEnumerable<string>? values) =>
        values is null
            ? ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase)
            : values.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

    private static int SetHash(ImmutableHashSet<string> values)
    {
        var hash = 0;
        foreach (string? value in values)
        {
            hash = unchecked(hash + (value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(value)));
        }

        return hash;
    }
}
