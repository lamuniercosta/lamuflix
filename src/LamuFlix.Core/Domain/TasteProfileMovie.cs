using System;
using System.Collections.Immutable;

namespace LamuFlix.Core.Domain;

public sealed record TasteProfileMovie(
    MovieId MovieId,
    ImmutableArray<string> Genres,
    ImmutableArray<float> PlotEmbedding,
    ImmutableArray<DateTimeOffset> PlayedAt,
    bool IsWatched)
{
    public Review Review { get; init; } = Review.None;

    public string Title { get; init; } = string.Empty;

    public ImmutableArray<string> Cast { get; init; } = [];

    public ImmutableArray<string> Directors { get; init; } = [];
}
