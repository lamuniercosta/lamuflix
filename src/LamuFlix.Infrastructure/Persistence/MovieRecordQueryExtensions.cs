using System;
using System.Collections.Immutable;
using System.Linq;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.Infrastructure.Persistence;

public static class MovieRecordQueryExtensions
{
    public static IQueryable<MovieRecord> WhereText(this IQueryable<MovieRecord> query, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return query;
        }

        var escaped = text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        return query.Where(movie => EF.Functions.ILike(movie.Title, $"%{escaped}%", "\\"));
    }

    public static IQueryable<MovieRecord> WhereGenres(this IQueryable<MovieRecord> query, ImmutableArray<int> ids) =>
        ids.IsDefaultOrEmpty ? query : query.Where(movie => movie.Genres.Any(genre => ids.Contains(genre.Id)));

    public static IQueryable<MovieRecord> WhereActors(this IQueryable<MovieRecord> query, ImmutableArray<int> ids) =>
        ids.IsDefaultOrEmpty ? query : query.Where(movie => movie.Actors.Any(actor => ids.Contains(actor.Id)));

    public static IQueryable<MovieRecord> WhereRuntime(this IQueryable<MovieRecord> query, RuntimeRange? range)
    {
        if (range is null)
        {
            return query;
        }

        var bounded = ApplyRuntimeBounds(query.Where(movie => movie.RuntimeMinutes != null), range);
        return range.IncludeUnknown
            ? query.Where(movie => movie.RuntimeMinutes == null).Concat(bounded)
            : bounded;
    }

    public static IQueryable<MovieRecord> WhereYear(this IQueryable<MovieRecord> query, YearRange? range)
    {
        if (range is null)
        {
            return query;
        }

        return ApplyYearBounds(query.Where(movie => movie.ReleaseYear != null), range);
    }

    public static IQueryable<MovieRecord> WhereStatuses(this IQueryable<MovieRecord> query, ImmutableArray<EnrichmentStatus> statuses) =>
        statuses.IsDefaultOrEmpty ? query : query.Where(movie => statuses.Contains(movie.Status));

    public static IQueryable<MovieRecord> WhereInWatchlist(this IQueryable<MovieRecord> query, bool? inWatchlist) =>
        inWatchlist is null ? query : query.Where(movie => movie.IsInWatchlist == inWatchlist.Value);

    public static IOrderedQueryable<MovieRecord> OrderByMovieSort(
        this IQueryable<MovieRecord> query,
        MovieSort sort,
        SortDirection direction) =>
        sort == MovieSort.Title ? OrderByTitle(query, direction)
        : sort == MovieSort.Year ? OrderByYear(query, direction)
        : sort == MovieSort.Rating ? OrderByRating(query, direction)
        : OrderByRuntime(query, direction);

    private static IQueryable<MovieRecord> ApplyRuntimeBounds(IQueryable<MovieRecord> query, RuntimeRange range)
    {
        if (!TryResolveRuntimeLower(range.Min, out var lower))
        {
            return NoMovies(query);
        }

        if (!TryResolveRuntimeUpper(range.Max, out var upper))
        {
            return NoMovies(query);
        }

        return FilterByRuntime(query, lower, upper);
    }

    private static IQueryable<MovieRecord> ApplyYearBounds(IQueryable<MovieRecord> query, YearRange range)
    {
        var now = TimeProvider.System.GetUtcNow();
        if (!TryResolveYearLower(range.Min, now, out var lower))
        {
            return NoMovies(query);
        }

        if (!TryResolveYearUpper(range.Max, now, out var upper))
        {
            return NoMovies(query);
        }

        return FilterByYear(query, lower, upper);
    }

    private static IQueryable<MovieRecord> NoMovies(IQueryable<MovieRecord> query) =>
        query.Where(_ => false);

    private static bool TryResolveRuntimeLower(int? min, out Runtime? lower)
    {
        if (min is null)
        {
            lower = null;
            return true;
        }

        return Runtime.TryCreate(min.Value, out lower);
    }

    private static bool TryResolveRuntimeUpper(int? max, out Runtime? upper)
    {
        if (max is null)
        {
            upper = null;
            return true;
        }

        return Runtime.TryCreate(max.Value, out upper);
    }

    private static IQueryable<MovieRecord> FilterByRuntime(IQueryable<MovieRecord> query, Runtime? lower, Runtime? upper)
    {
        if (lower is not null && upper is not null)
        {
            return query.Where(movie => movie.RuntimeMinutes >= lower && movie.RuntimeMinutes <= upper);
        }

        if (lower is not null)
        {
            return query.Where(movie => movie.RuntimeMinutes >= lower);
        }

        return upper is not null
            ? query.Where(movie => movie.RuntimeMinutes <= upper)
            : query;
    }

    private static bool TryResolveYearLower(int? min, DateTimeOffset now, out ReleaseYear? lower)
    {
        if (min is null)
        {
            lower = null;
            return true;
        }

        return ReleaseYear.TryCreate(min.Value, now, out lower);
    }

    private static bool TryResolveYearUpper(int? max, DateTimeOffset now, out ReleaseYear? upper)
    {
        if (max is null)
        {
            upper = null;
            return true;
        }

        return ReleaseYear.TryCreate(max.Value, now, out upper);
    }

    private static IQueryable<MovieRecord> FilterByYear(IQueryable<MovieRecord> query, ReleaseYear? lower, ReleaseYear? upper)
    {
        if (lower is not null && upper is not null)
        {
            return query.Where(movie => movie.ReleaseYear >= lower && movie.ReleaseYear <= upper);
        }

        if (lower is not null)
        {
            return query.Where(movie => movie.ReleaseYear >= lower);
        }

        return upper is not null
            ? query.Where(movie => movie.ReleaseYear <= upper)
            : query;
    }

    private static IOrderedQueryable<MovieRecord> OrderByTitle(IQueryable<MovieRecord> query, SortDirection direction) =>
        direction == SortDirection.Ascending
            ? query.OrderBy(movie => movie.Title).ThenBy(movie => movie.Id)
            : query.OrderByDescending(movie => movie.Title).ThenBy(movie => movie.Id);

    private static IOrderedQueryable<MovieRecord> OrderByYear(IQueryable<MovieRecord> query, SortDirection direction) =>
        direction == SortDirection.Ascending
            ? query.OrderBy(movie => movie.ReleaseYear == null).ThenBy(movie => movie.ReleaseYear).ThenBy(movie => movie.Title).ThenBy(movie => movie.Id)
            : query.OrderBy(movie => movie.ReleaseYear == null).ThenByDescending(movie => movie.ReleaseYear).ThenBy(movie => movie.Title).ThenBy(movie => movie.Id);

    private static IOrderedQueryable<MovieRecord> OrderByRating(IQueryable<MovieRecord> query, SortDirection direction) =>
        direction == SortDirection.Ascending
            ? query.OrderBy(movie => movie.ImdbRating == null).ThenBy(movie => movie.ImdbRating).ThenBy(movie => movie.Title).ThenBy(movie => movie.Id)
            : query.OrderBy(movie => movie.ImdbRating == null).ThenByDescending(movie => movie.ImdbRating).ThenBy(movie => movie.Title).ThenBy(movie => movie.Id);

    private static IOrderedQueryable<MovieRecord> OrderByRuntime(IQueryable<MovieRecord> query, SortDirection direction) =>
        direction == SortDirection.Ascending
            ? query.OrderBy(movie => movie.RuntimeMinutes == null).ThenBy(movie => movie.RuntimeMinutes).ThenBy(movie => movie.Title).ThenBy(movie => movie.Id)
            : query.OrderBy(movie => movie.RuntimeMinutes == null).ThenByDescending(movie => movie.RuntimeMinutes).ThenBy(movie => movie.Title).ThenBy(movie => movie.Id);
}