using System;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Library;

public sealed record MovieQuery
{
    private readonly ImmutableArray<int> genreIds = [];
    private readonly ImmutableArray<int> actorIds = [];
    private readonly ImmutableArray<EnrichmentStatus> statuses = [];

    public string? Text { get; init; }

    public ImmutableArray<int> GenreIds
    {
        get => genreIds;
        init => genreIds = Normalize(value);
    }

    public ImmutableArray<int> ActorIds
    {
        get => actorIds;
        init => actorIds = Normalize(value);
    }

    public RuntimeRange? Runtime { get; init; }

    public YearRange? Year { get; init; }

    public ImmutableArray<EnrichmentStatus> Statuses
    {
        get => statuses;
        init => statuses = Normalize(value);
    }

    public bool? InWatchlist { get; init; }

    public MovieSort? Sort { get; init; }

    public SortDirection? Direction { get; init; }

    public Page Page { get; init; } = new(0, 0);

    public bool Equals(MovieQuery? other)
    {
        if (other is null)
        {
            return false;
        }

        return HasSameCoreValues(other)
            && HasSameCollectionValues(other)
            && HasSameStatusAndPagingValues(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Text);
        AddSequence(ref hash, GenreIds);
        AddSequence(ref hash, ActorIds);
        hash.Add(Runtime);
        hash.Add(Year);
        AddSequence(ref hash, Statuses);
        hash.Add(InWatchlist);
        hash.Add(Sort);
        hash.Add(Direction);
        hash.Add(Page);
        return hash.ToHashCode();
    }

    private static ImmutableArray<T> Normalize<T>(ImmutableArray<T> values) =>
        values.IsDefault ? [] : values;

    private bool HasSameCoreValues(MovieQuery other) =>
        Text == other.Text
        && Runtime == other.Runtime
        && Year == other.Year;

    private bool HasSameCollectionValues(MovieQuery other) =>
        Enumerable.SequenceEqual(GenreIds, other.GenreIds)
        && Enumerable.SequenceEqual(ActorIds, other.ActorIds)
        && Enumerable.SequenceEqual(Statuses, other.Statuses);

    private bool HasSameStatusAndPagingValues(MovieQuery other) =>
        InWatchlist == other.InWatchlist
        && Equals(Sort, other.Sort)
        && Equals(Direction, other.Direction)
        && Page == other.Page;

    private static void AddSequence<T>(ref HashCode hash, ImmutableArray<T> values)
    {
        hash.Add(values.Length);
        foreach (var value in values)
        {
            hash.Add(value);
        }
    }
}
