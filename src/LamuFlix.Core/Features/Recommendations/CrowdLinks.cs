using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Features.Recommendations;

public sealed class CrowdLinks
{
    public static CrowdLinks Empty { get; } = new(ImmutableHashSet<(int First, int Second)>.Empty);

    private readonly ImmutableHashSet<(int First, int Second)> pairs;

    private CrowdLinks(ImmutableHashSet<(int First, int Second)> pairs) => this.pairs = pairs;

    public int Count => pairs.Count;

    public bool IsLinked(MovieId first, MovieId second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return first.Value != second.Value && pairs.Contains(Key(first, second));
    }

    public static CrowdLinks Create(IEnumerable<(MovieId First, MovieId Second)> links)
    {
        ArgumentNullException.ThrowIfNull(links);

        var builder = ImmutableHashSet.CreateBuilder<(int First, int Second)>();
        foreach (var (first, second) in links)
        {
            ArgumentNullException.ThrowIfNull(first);
            ArgumentNullException.ThrowIfNull(second);

            if (first.Value != second.Value)
            {
                builder.Add(Key(first, second));
            }
        }

        return builder.Count == 0 ? Empty : new CrowdLinks(builder.ToImmutable());
    }

    private static (int First, int Second) Key(MovieId first, MovieId second) =>
        first.Value < second.Value ? (first.Value, second.Value) : (second.Value, first.Value);
}
