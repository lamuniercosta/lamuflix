using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Library;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace LamuFlix.Api.Endpoints;

internal static class LibraryEndpoints
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
    private const bool EmptyMeansNull = true;
    private const bool EmptyMeansInvalid = false;

    private static readonly string[] RuntimeKeys = [RuntimeMinKey, RuntimeMaxKey, RuntimeIncludeUnknownKey];
    private static readonly string[] YearKeys = [YearMinKey, YearMaxKey];

    internal static RouteGroupBuilder MapLibraryEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/movies", BrowseMoviesAsync)
            .WithName("BrowseMovies")
            .WithSummary("Browse movies using the canonical library query string keys.")
            .Produces<PagedResult<MovieSummary>>()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        api.MapGet("/movies/{id:int}", GetMovieDetailsAsync)
            .WithName("GetMovieDetails")
            .WithSummary("Retrieve the details of a single movie by its identifier.")
            .Produces<MovieDetails>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Ok<MovieDetails>> GetMovieDetailsAsync(
        int id,
        IQueryHandler<GetMovieDetailsQuery, MovieDetails> handler,
        CancellationToken cancellationToken)
    {
        if (!MovieId.TryCreate(id, out var movieId))
        {
            throw new NotFoundException();
        }

        var details = await handler.HandleAsync(new GetMovieDetailsQuery(movieId), cancellationToken);
        return TypedResults.Ok(details);
    }

    private static async Task<Ok<PagedResult<MovieSummary>>> BrowseMoviesAsync(
        [AsParameters] BrowseMoviesRequest request,
        HttpRequest httpRequest,
        IQueryHandler<BrowseMoviesQuery, PagedResult<MovieSummary>> handler,
        CancellationToken cancellationToken)
    {
        var query = ToMovieQuery(request, httpRequest.Query);
        var page = await handler.HandleAsync(new BrowseMoviesQuery(query), cancellationToken);
        return TypedResults.Ok(page);
    }

    private static MovieQuery ToMovieQuery(BrowseMoviesRequest request, IQueryCollection rawQuery)
    {
        var state = new MappingState();
        ReadText(rawQuery, request.Text, state);
        ReadRepeated(request.GenreIds, GenreIdsKey, IntegerMessage(GenreIdsKey), TryParseId, state.GenreIds, state);
        ReadRepeated(request.ActorIds, ActorIdsKey, IntegerMessage(ActorIdsKey), TryParseId, state.ActorIds, state);
        ReadRepeated(
            request.Statuses,
            StatusesKey,
            NamesMessage(StatusesKey, EnrichmentStatus.List.Select(item => item.Name)),
            TryParseStatus,
            state.Statuses,
            state);
        ReadNumber(rawQuery, RuntimeMinKey, request.RuntimeMin, EmptyMeansNull, state);
        ReadNumber(rawQuery, RuntimeMaxKey, request.RuntimeMax, EmptyMeansNull, state);
        ReadNumber(rawQuery, YearMinKey, request.YearMin, EmptyMeansNull, state);
        ReadNumber(rawQuery, YearMaxKey, request.YearMax, EmptyMeansNull, state);
        ReadBoolean(rawQuery, RuntimeIncludeUnknownKey, request.RuntimeIncludeUnknown, state);
        ReadBoolean(rawQuery, InWatchlistKey, request.InWatchlist, state);
        ReadSort(rawQuery, request.Sort, state);
        ReadDirection(rawQuery, request.Direction, state);
        ReadNumber(rawQuery, PageKey, request.Page, EmptyMeansInvalid, state);
        ReadNumber(rawQuery, PageSizeKey, request.PageSize, EmptyMeansInvalid, state);
        state.ApplyRanges();
        state.ThrowIfInvalid();
        return state.ToQuery();
    }

    private static void ReadText(IQueryCollection rawQuery, string? raw, MappingState state)
    {
        if (raw is null)
        {
            return;
        }

        if (MarkSeenOnce(rawQuery, TextKey, state))
        {
            state.Text = raw;
        }
    }

    private static void ReadRepeated<T>(
        string[]? raw,
        string key,
        string message,
        TryParseValue<T> parse,
        List<T> target,
        MappingState state)
    {
        if (raw is null)
        {
            return;
        }

        foreach (var item in raw)
        {
            if (parse(item, out var parsed))
            {
                target.Add(parsed);
                continue;
            }

            state.Fail(key, message);
        }
    }

    private static void ReadNumber(
        IQueryCollection rawQuery,
        string key,
        string? raw,
        bool emptyMeansNull,
        MappingState state)
    {
        if (raw is null || !MarkSeenOnce(rawQuery, key, state))
        {
            return;
        }

        if (raw.Length == 0)
        {
            if (!emptyMeansNull)
            {
                state.Fail(key, EmptyMessage(key));
            }

            return;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            state.Ints[key] = parsed;
            return;
        }

        state.Fail(key, IntegerMessage(key));
    }

    private static void ReadBoolean(IQueryCollection rawQuery, string key, string? raw, MappingState state)
    {
        if (raw is null || !MarkSeenOnce(rawQuery, key, state))
        {
            return;
        }

        if (bool.TryParse(raw, out var parsed))
        {
            state.Bools[key] = parsed;
            return;
        }

        state.Fail(key, $"'{key}' must be true or false.");
    }

    private static void ReadSort(IQueryCollection rawQuery, string? raw, MappingState state)
    {
        if (raw is null || !MarkSeenOnce(rawQuery, SortKey, state))
        {
            return;
        }

        if (MovieSort.TryFromName(raw, false, out var sort))
        {
            state.Sort = sort;
            return;
        }

        state.Fail(SortKey, NamesMessage(SortKey, MovieSort.List.Select(item => item.Name)));
    }

    private static void ReadDirection(IQueryCollection rawQuery, string? raw, MappingState state)
    {
        if (raw is null || !MarkSeenOnce(rawQuery, DirectionKey, state))
        {
            return;
        }

        if (SortDirection.TryFromName(raw, false, out var direction))
        {
            state.Direction = direction;
            return;
        }

        state.Fail(DirectionKey, NamesMessage(DirectionKey, SortDirection.List.Select(item => item.Name)));
    }

    private static bool MarkSeenOnce(IQueryCollection rawQuery, string key, MappingState state)
    {
        state.Seen.Add(key);
        if (rawQuery.TryGetValue(key, out var values) && values.Count > 1)
        {
            state.Fail(key, $"'{key}' must be supplied at most once.");
            return false;
        }

        return true;
    }

    private static bool TryParseId(string raw, out int value) =>
        int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static bool TryParseStatus(string raw, out EnrichmentStatus value) =>
        EnrichmentStatus.TryFromName(raw, false, out value);

    private static string IntegerMessage(string key) => $"'{key}' must be an integer.";

    private static string EmptyMessage(string key) => $"'{key}' must not be empty.";

    private static string NamesMessage(string key, IEnumerable<string> names) =>
        $"'{key}' must be one of: {string.Join(", ", names)}.";

    private delegate bool TryParseValue<T>(string raw, out T value);

    private sealed class MappingState
    {
        private RuntimeRange? Runtime { get; set; }

        private YearRange? Year { get; set; }

        public HashSet<string> Seen { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, List<string>> Errors { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, int> Ints { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, bool> Bools { get; } = new(StringComparer.Ordinal);

        public List<int> GenreIds { get; } = [];

        public List<int> ActorIds { get; } = [];

        public List<EnrichmentStatus> Statuses { get; } = [];

        public string? Text { get; set; }

        public MovieSort? Sort { get; set; }

        public SortDirection? Direction { get; set; }

        public void Fail(string key, string message)
        {
            if (!Errors.TryGetValue(key, out var messages))
            {
                messages = [];
                Errors[key] = messages;
            }

            messages.Add(message);
        }

        public void ApplyRanges()
        {
            ApplyRuntime();
            ApplyYear();
        }

        public void ThrowIfInvalid()
        {
            if (Errors.Count == 0)
            {
                return;
            }

            throw new ValidationException(
                Errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal));
        }

        public MovieQuery ToQuery() => new()
        {
            Text = Text,
            GenreIds = [.. GenreIds],
            ActorIds = [.. ActorIds],
            Runtime = Runtime,
            Year = Year,
            Statuses = [.. Statuses],
            InWatchlist = LookupBoolean(InWatchlistKey),
            Sort = Sort,
            Direction = Direction,
            Page = new Page(Ints.GetValueOrDefault(PageKey), Ints.GetValueOrDefault(PageSizeKey)),
        };

        private void ApplyRuntime()
        {
            if (!SeenOverlaps(RuntimeKeys) || !RequireSeen(RuntimeKeys))
            {
                return;
            }

            Runtime = new RuntimeRange(
                LookupNumber(RuntimeMinKey),
                LookupNumber(RuntimeMaxKey),
                Bools.GetValueOrDefault(RuntimeIncludeUnknownKey));
        }

        private void ApplyYear()
        {
            if (!SeenOverlaps(YearKeys) || !RequireSeen(YearKeys))
            {
                return;
            }

            Year = new YearRange(LookupNumber(YearMinKey), LookupNumber(YearMaxKey));
        }

        private int? LookupNumber(string key) =>
            Ints.TryGetValue(key, out var value) ? value : null;

        private bool SeenOverlaps(string[] keys) => keys.Any(key => Seen.Contains(key));

        private bool RequireSeen(string[] keys)
        {
            var complete = true;
            foreach (var key in keys.Where(key => !Seen.Contains(key)))
            {
                Fail(key, $"'{key}' is required when its range is supplied.");
                complete = false;
            }

            return complete;
        }

        private bool? LookupBoolean(string key) =>
            Bools.TryGetValue(key, out var value) ? value : null;
    }
}
