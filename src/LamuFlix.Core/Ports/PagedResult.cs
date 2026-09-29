using System;
using System.Collections.Immutable;

namespace LamuFlix.Core.Ports;

public sealed record PagedResult<T>
{
    public PagedResult(ImmutableArray<T> items, int totalCount)
    {
        if (items.IsDefault)
        {
            throw new ArgumentException("Items must be initialized.", nameof(items));
        }

        if (totalCount < items.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount));
        }

        Items = items;
        TotalCount = totalCount;
    }

    public ImmutableArray<T> Items { get; }

    public int TotalCount { get; }
}
