using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using LamuFlix.Core.Domain;

namespace LamuFlix.Core.Library;

public static class MovieQueryString
{
    private const string TextKey = "text";
    private const string GenreIdsKey = "genreIds";
    private const string ActorIdsKey = "actorIds";
    private const string RuntimeMinKey = "runtimeMin";
    private const string RuntimeMaxKey = "runtimeMax";
    private const string RuntimeIncludeUnknownKey = "runtimeIncludeUnknown";
    private const string YearMinKey = "yearMin";
    private const string YearMaxKey = "yearMax";
    private const string StatusesKey = "statuses";
    private const string InWatchlistKey = "inWatchlist";
    private const string SortKey = "sort";
    private const string DirectionKey = "direction";
    private const string PageKey = "page";
    private const string PageSizeKey = "pageSize";

    public static string Format(MovieQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        var pairs = new List<string>();
        AppendText(pairs, query.Text);
        AppendInts(pairs, GenreIdsKey, query.GenreIds);
        AppendInts(pairs, ActorIdsKey, query.ActorIds);
        AppendRuntime(pairs, query.Runtime);
        AppendYear(pairs, query.Year);
        AppendStatuses(pairs, query.Statuses);
        AppendOptionalBool(pairs, query.InWatchlist);
        pairs.Add(Pair(SortKey, RequiredName(query.Sort, SortKey)));
        pairs.Add(Pair(DirectionKey, RequiredName(query.Direction, DirectionKey)));
        pairs.Add(Pair(PageKey, query.Page.Number.ToString(CultureInfo.InvariantCulture)));
        pairs.Add(Pair(PageSizeKey, query.Page.Size.ToString(CultureInfo.InvariantCulture)));
        return string.Join('&', pairs);
    }

    public static bool TryParse(string? queryString, [NotNullWhen(true)] out MovieQuery? query)
    {
        query = null;
        if (string.IsNullOrEmpty(queryString))
        {
            return false;
        }

        var state = new ParseState();
        foreach (var segment in queryString.Split('&'))
        {
            if (!TrySplit(segment, out var key, out var raw) || !TryApplyKey(key, raw, state))
            {
                return false;
            }
        }

        if (!TryBuild(state, out query))
        {
            return false;
        }

        return true;
    }

    private static bool TryApplyKey(string key, string raw, ParseState state) => key switch
    {
        TextKey => TryApplyText(raw, state),
        GenreIdsKey => TryApplyGenreId(raw, state),
        ActorIdsKey => TryApplyActorId(raw, state),
        RuntimeMinKey => TryApplyRuntimeMin(raw, state),
        RuntimeMaxKey => TryApplyRuntimeMax(raw, state),
        RuntimeIncludeUnknownKey => TryApplyRuntimeIncludeUnknown(raw, state),
        YearMinKey => TryApplyYearMin(raw, state),
        YearMaxKey => TryApplyYearMax(raw, state),
        StatusesKey => TryApplyStatus(raw, state),
        InWatchlistKey => TryApplyInWatchlist(raw, state),
        SortKey => TryApplySort(raw, state),
        DirectionKey => TryApplyDirection(raw, state),
        PageKey => TryApplyPage(raw, state),
        PageSizeKey => TryApplyPageSize(raw, state),
        _ => true,
    };

    private static bool TryApplyText(string raw, ParseState state)
    {
        if (!state.Seen.Add(TextKey))
        {
            return false;
        }

        state.Text = raw;
        return true;
    }

    private static bool TryApplyGenreId(string raw, ParseState state) =>
        TryParseInt(raw, out var id) && Add(state.GenreIds, id);

    private static bool TryApplyActorId(string raw, ParseState state) =>
        TryParseInt(raw, out var id) && Add(state.ActorIds, id);

    private static bool TryApplyRuntimeMin(string raw, ParseState state) =>
        TryApplyOptionalInt(raw, state, RuntimeMinKey, value => state.RuntimeMin = value);

    private static bool TryApplyRuntimeMax(string raw, ParseState state) =>
        TryApplyOptionalInt(raw, state, RuntimeMaxKey, value => state.RuntimeMax = value);

    private static bool TryApplyRuntimeIncludeUnknown(string raw, ParseState state) =>
        TryApplyRequiredBool(raw, state, RuntimeIncludeUnknownKey, value => state.RuntimeIncludeUnknown = value);

    private static bool TryApplyYearMin(string raw, ParseState state) =>
        TryApplyOptionalInt(raw, state, YearMinKey, value => state.YearMin = value);

    private static bool TryApplyYearMax(string raw, ParseState state) =>
        TryApplyOptionalInt(raw, state, YearMaxKey, value => state.YearMax = value);

    private static bool TryApplyStatus(string raw, ParseState state)
    {
        if (raw.Length == 0 || !EnrichmentStatus.TryFromName(raw, false, out var status))
        {
            return false;
        }

        state.Statuses.Add(status);
        return true;
    }

    private static bool TryApplyInWatchlist(string raw, ParseState state) =>
        TryApplyRequiredBool(raw, state, InWatchlistKey, value => state.InWatchlist = value);

    private static bool TryApplySort(string raw, ParseState state)
    {
        if (!state.Seen.Add(SortKey) || raw.Length == 0 || !MovieSort.TryFromName(raw, false, out var sort))
        {
            return false;
        }

        state.Sort = sort;
        return true;
    }

    private static bool TryApplyDirection(string raw, ParseState state)
    {
        if (!state.Seen.Add(DirectionKey) || raw.Length == 0 || !SortDirection.TryFromName(raw, false, out var direction))
        {
            return false;
        }

        state.Direction = direction;
        return true;
    }

    private static bool TryApplyPage(string raw, ParseState state) =>
        TryApplyRequiredInt(raw, state, PageKey, value => state.Page = value);

    private static bool TryApplyPageSize(string raw, ParseState state) =>
        TryApplyRequiredInt(raw, state, PageSizeKey, value => state.PageSize = value);

    private static bool TryApplyOptionalInt(string raw, ParseState state, string key, Action<int?> assign)
    {
        if (!state.Seen.Add(key))
        {
            return false;
        }

        if (raw.Length == 0)
        {
            assign(null);
            return true;
        }

        if (!TryParseInt(raw, out var parsed))
        {
            return false;
        }

        assign(parsed);
        return true;
    }

    private static bool TryApplyRequiredInt(string raw, ParseState state, string key, Action<int> assign)
    {
        if (!state.Seen.Add(key) || raw.Length == 0 || !TryParseInt(raw, out var parsed))
        {
            return false;
        }

        assign(parsed);
        return true;
    }

    private static bool TryApplyRequiredBool(string raw, ParseState state, string key, Action<bool> assign)
    {
        if (!state.Seen.Add(key) || raw.Length == 0 || !bool.TryParse(raw, out var parsed))
        {
            return false;
        }

        assign(parsed);
        return true;
    }

    private static bool TryParseInt(string raw, out int parsed) =>
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);

    private static bool Add(List<int> values, int id)
    {
        values.Add(id);
        return true;
    }

    private static bool TryBuild(ParseState state, [NotNullWhen(true)] out MovieQuery? query)
    {
        query = null;
        if (!HasRequiredValues(state))
        {
            return false;
        }

        if (!TryRuntime(state, out var runtime) || !TryYear(state, out var year))
        {
            return false;
        }

        query = CreateQuery(state, runtime, year);
        return true;
    }

    private static bool TryRuntime(ParseState state, out RuntimeRange? runtime)
    {
        if (!HasAnyRuntimeValue(state))
        {
            runtime = null;
            return true;
        }

        if (!HasCompleteRuntimeValues(state))
        {
            runtime = null;
            return false;
        }

        if (!TryGetRuntimeIncludeUnknown(state, out var includeUnknown))
        {
            runtime = null;
            return false;
        }

        if (!HasMatchingRuntimeBoundPresence(state))
        {
            runtime = null;
            return false;
        }

        runtime = new RuntimeRange(state.RuntimeMin, state.RuntimeMax, includeUnknown);
        return true;
    }

    private static bool TryYear(ParseState state, out YearRange? year)
    {
        if (!HasAnyYearValue(state))
        {
            year = null;
            return true;
        }

        if (state.Seen.Contains(YearMinKey) != state.Seen.Contains(YearMaxKey)
            || state.YearMin.HasValue != state.YearMax.HasValue)
        {
            year = null;
            return false;
        }

        year = new YearRange(state.YearMin, state.YearMax);
        return true;
    }

    private static bool HasRequiredValues(ParseState state) =>
        state.Sort is not null
        && state.Direction is not null
        && state.Page is not null
        && state.PageSize is not null;

    private static MovieQuery CreateQuery(ParseState state, RuntimeRange? runtime, YearRange? year) =>
        new()
        {
            Text = state.Text,
            GenreIds = [.. state.GenreIds],
            ActorIds = [.. state.ActorIds],
            Runtime = runtime,
            Year = year,
            Statuses = [.. state.Statuses],
            InWatchlist = state.InWatchlist,
            Sort = state.Sort,
            Direction = state.Direction,
            Page = new Page(state.Page.GetValueOrDefault(), state.PageSize.GetValueOrDefault()),
        };

    private static bool HasAnyRuntimeValue(ParseState state) =>
        state.Seen.Contains(RuntimeMinKey)
        || state.Seen.Contains(RuntimeMaxKey)
        || state.Seen.Contains(RuntimeIncludeUnknownKey);

    private static bool HasCompleteRuntimeValues(ParseState state) =>
        state.Seen.Contains(RuntimeMinKey)
        && state.Seen.Contains(RuntimeMaxKey)
        && state.Seen.Contains(RuntimeIncludeUnknownKey);

    private static bool TryGetRuntimeIncludeUnknown(ParseState state, out bool includeUnknown)
    {
        if (state.RuntimeIncludeUnknown is not bool value)
        {
            includeUnknown = default;
            return false;
        }

        includeUnknown = value;
        return true;
    }

    private static bool HasMatchingRuntimeBoundPresence(ParseState state) =>
        state.RuntimeMin.HasValue == state.RuntimeMax.HasValue;

    private static bool HasAnyYearValue(ParseState state) =>
        state.Seen.Contains(YearMinKey) || state.Seen.Contains(YearMaxKey);

    private static bool TrySplit(string segment, out string key, out string value)
    {
        var separator = segment.IndexOf('=');
        if (separator <= 0)
        {
            key = string.Empty;
            value = string.Empty;
            return false;
        }

        key = Uri.UnescapeDataString(segment[..separator]);
        value = Uri.UnescapeDataString(segment[(separator + 1)..]);
        return true;
    }

    private static void AppendText(List<string> pairs, string? text)
    {
        if (text is not null)
        {
            pairs.Add(Pair(TextKey, text));
        }
    }

    private static void AppendInts(List<string> pairs, string key, ImmutableArray<int> values)
    {
        foreach (var value in values)
        {
            pairs.Add(Pair(key, value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static void AppendRuntime(List<string> pairs, RuntimeRange? runtime)
    {
        if (runtime is null)
        {
            return;
        }

        pairs.Add(Pair(RuntimeMinKey, Bound(runtime.Min)));
        pairs.Add(Pair(RuntimeMaxKey, Bound(runtime.Max)));
        pairs.Add(Pair(RuntimeIncludeUnknownKey, runtime.IncludeUnknown ? "true" : "false"));
    }

    private static void AppendYear(List<string> pairs, YearRange? year)
    {
        if (year is null)
        {
            return;
        }

        pairs.Add(Pair(YearMinKey, Bound(year.Min)));
        pairs.Add(Pair(YearMaxKey, Bound(year.Max)));
    }

    private static void AppendStatuses(List<string> pairs, ImmutableArray<EnrichmentStatus> statuses)
    {
        foreach (var status in statuses)
        {
            pairs.Add(Pair(StatusesKey, status.Name));
        }
    }

    private static void AppendOptionalBool(List<string> pairs, bool? value)
    {
        if (value is not null)
        {
            pairs.Add(Pair(InWatchlistKey, value.Value ? "true" : "false"));
        }
    }

    private static string Bound(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string RequiredName<TEnum>(TEnum? value, string key)
        where TEnum : Ardalis.SmartEnum.SmartEnum<TEnum, int> =>
        value?.Name ?? throw new ArgumentException($"'{key}' is required to format a query.", nameof(value));

    private static string Pair(string key, string value) =>
        $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";

    private sealed class ParseState
    {
        public HashSet<string> Seen { get; } = [];

        public string? Text { get; set; }

        public List<int> GenreIds { get; } = [];

        public List<int> ActorIds { get; } = [];

        public int? RuntimeMin { get; set; }

        public int? RuntimeMax { get; set; }

        public bool? RuntimeIncludeUnknown { get; set; }

        public int? YearMin { get; set; }

        public int? YearMax { get; set; }

        public List<EnrichmentStatus> Statuses { get; } = [];

        public bool? InWatchlist { get; set; }

        public MovieSort? Sort { get; set; }

        public SortDirection? Direction { get; set; }

        public int? Page { get; set; }

        public int? PageSize { get; set; }
    }
}
