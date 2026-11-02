using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace LamuFlix.Core.Domain;

public enum SignalAvailability
{
    Unavailable,
    KnownEmpty,
    Available,
}

public sealed record MovieSignalSnapshot
{
    public MovieSignalSnapshot(
        SignalAvailability keywordAvailability = SignalAvailability.Unavailable,
        IEnumerable<MovieKeyword>? keywords = null,
        SignalAvailability fullCastAvailability = SignalAvailability.Unavailable,
        IEnumerable<string>? fullCast = null,
        SignalAvailability collectionAvailability = SignalAvailability.Unavailable)
    {
        var keywordValues = MaterializeKeywords(keywords);
        RequireAvailability(keywordAvailability, keywordValues.Length, nameof(keywords));
        RequireDistinctKeywordIds(keywordValues);

        var castValues = MaterializeCast(fullCast);
        RequireAvailability(fullCastAvailability, castValues.Count, nameof(fullCast));

        KeywordAvailability = keywordAvailability;
        Keywords = keywordValues;
        FullCastAvailability = fullCastAvailability;
        FullCast = castValues;
        CollectionAvailability = collectionAvailability;
    }

    public SignalAvailability KeywordAvailability { get; }

    public ImmutableArray<MovieKeyword> Keywords { get; }

    public SignalAvailability FullCastAvailability { get; }

    public ImmutableHashSet<string> FullCast { get; }

    public SignalAvailability CollectionAvailability { get; }

    public bool Equals(MovieSignalSnapshot? other) =>
        other is not null
        && KeywordAvailability == other.KeywordAvailability
        && SameKeywords(Keywords, other.Keywords)
        && FullCastAvailability == other.FullCastAvailability
        && FullCast.SetEquals(other.FullCast)
        && CollectionAvailability == other.CollectionAvailability;

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(KeywordAvailability);
        hash.Add(KeywordHash(Keywords));
        hash.Add(FullCastAvailability);
        hash.Add(SetHash(FullCast));
        hash.Add(CollectionAvailability);
        return hash.ToHashCode();
    }

    private static ImmutableArray<MovieKeyword> MaterializeKeywords(IEnumerable<MovieKeyword>? source)
    {
        if (source is null)
        {
            return ImmutableArray<MovieKeyword>.Empty;
        }

        var values = source.ToImmutableArray();
        if (values.Any(keyword => keyword is null))
        {
            throw new ArgumentException("Keyword entries must not be null.", nameof(source));
        }

        return values;
    }

    private static ImmutableHashSet<string> MaterializeCast(IEnumerable<string>? source)
    {
        if (source is null)
        {
            return ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);
        }

        var values = source.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        if (values.Any(name => name is null))
        {
            throw new ArgumentException("Full cast names must not be null.", nameof(source));
        }

        return values;
    }

    private static void RequireAvailability(SignalAvailability availability, int count, string paramName)
    {
        if (availability == SignalAvailability.Available && count == 0)
        {
            throw new ArgumentException("An available signal requires at least one value.", paramName);
        }

        if (availability != SignalAvailability.Available && count > 0)
        {
            throw new ArgumentException("Only an available signal may carry values.", paramName);
        }
    }

    private static void RequireDistinctKeywordIds(ImmutableArray<MovieKeyword> keywords)
    {
        if (keywords.Select(keyword => keyword.Id).Distinct().Count() != keywords.Length)
        {
            throw new ArgumentException("Keyword ids must be distinct.", nameof(keywords));
        }
    }

    private static bool SameKeywords(ImmutableArray<MovieKeyword> left, ImmutableArray<MovieKeyword> right) =>
        left.Length == right.Length && left.All(keyword => right.Contains(keyword));

    private static int KeywordHash(ImmutableArray<MovieKeyword> keywords)
    {
        var hash = 0;
        foreach (var keyword in keywords)
        {
            hash = unchecked(hash + keyword.GetHashCode());
        }

        return hash;
    }

    private static int SetHash(ImmutableHashSet<string> values)
    {
        var hash = 0;
        foreach (var value in values)
        {
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            hash = unchecked(hash + (value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(value)));
        }

        return hash;
    }
}
