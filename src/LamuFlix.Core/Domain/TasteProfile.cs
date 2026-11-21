using System;
using System.Collections.Immutable;

namespace LamuFlix.Core.Domain;

public sealed record TasteProfile(ImmutableDictionary<string, double> GenrePreferences)
{
    public ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>> PlaybackByMovieId { get; init; } =
        ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>.Empty;

    public bool IsEmpty => GenrePreferences.IsEmpty && PlaybackByMovieId.IsEmpty;
}
