using System;
using System.Collections.Immutable;

namespace LamuFlix.Core.Domain;

public sealed record TasteProfileMovie(
    MovieId MovieId,
    ImmutableArray<string> Genres,
    ImmutableArray<float> PlotEmbedding,
    ImmutableArray<DateTimeOffset> PlayedAt,
    bool IsWatched);
