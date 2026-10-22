using System.Collections.Generic;
using System.Collections.Immutable;

namespace LamuFlix.Core.Domain;

public sealed record TasteProfile(
    ImmutableDictionary<string, double> GenrePreferences,
    ImmutableArray<float> PlotEmbedding)
{
    public bool IsEmpty => GenrePreferences.IsEmpty && PlotEmbedding.IsDefaultOrEmpty;
}
