using System;
using System.Collections.Immutable;

namespace LamuFlix.Core.Domain;

public sealed record ReviewProfileChannels(
    ImmutableArray<float> PlotEmbedding,
    ImmutableDictionary<string, double> Genres,
    ImmutableDictionary<string, double> Cast,
    ImmutableDictionary<string, double> Directors)
{
    public static ReviewProfileChannels Empty { get; } = new(
        ImmutableArray<float>.Empty,
        ImmutableDictionary.Create<string, double>(StringComparer.OrdinalIgnoreCase),
        ImmutableDictionary.Create<string, double>(StringComparer.OrdinalIgnoreCase),
        ImmutableDictionary.Create<string, double>(StringComparer.OrdinalIgnoreCase));

    public bool IsEmpty =>
        PlotEmbedding.IsDefaultOrEmpty
        && Genres.IsEmpty
        && Cast.IsEmpty
        && Directors.IsEmpty;
}
