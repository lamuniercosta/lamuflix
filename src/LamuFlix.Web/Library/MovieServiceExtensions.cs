using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LamuFlix.Data.Constants;
using LamuFlix.Data.Models;
using LamuFlix.Web.Models.Movies;

namespace LamuFlix.Web.Library;

public static class MovieServiceExtensions
{
    public static IQueryable<Movie> ApplyLegacyFilters(this IQueryable<Movie> query, MoviesFilterViewModel? filter)
    {
        if (filter is null)
        {
            return query;
        }

        return query
            .FilterByText(filter.SearchField)
            .FilterByYear(filter.Year)
            .FilterByCollection(filter.CollectionId)
            .FilterByDirector(filter.DirectorId)
            .FilterByGenres(filter.GenreIds)
            .FilterByActors(filter.ActorIds);
    }

    public static IQueryable<Movie> FilterByText(this IQueryable<Movie> query, string? search)
    {
        if (search is null)
        {
            return query;
        }

        return query.Where(movie => movie.Title!.ToLower().Contains(search.ToLower()));
    }

    public static IQueryable<Movie> FilterByYear(this IQueryable<Movie> query, int? year) =>
        year is null ? query : query.Where(movie => movie.Year == year);

    public static IQueryable<Movie> FilterByCollection(this IQueryable<Movie> query, int? collectionId) =>
        collectionId is null ? query : query.Where(movie => movie.CollectionId == collectionId);

    public static IQueryable<Movie> FilterByDirector(this IQueryable<Movie> query, int? directorId) =>
        directorId is null ? query : query.Where(movie => movie.Directors.Any(director => director.DirectorId == directorId));

    public static IQueryable<Movie> FilterByGenres(this IQueryable<Movie> query, IEnumerable<int>? genreIds) =>
        FilterByIds(query, genreIds, ids => movie => movie.Genres.Any(genre => ids.Contains(genre.GenreId)));

    public static IQueryable<Movie> FilterByActors(this IQueryable<Movie> query, IEnumerable<int>? actorIds) =>
        FilterByIds(query, actorIds, ids => movie => movie.Actors.Any(actor => ids.Contains(actor.ActorId)));

    public static IQueryable<Movie> ApplyLegacySort(this IQueryable<Movie> query, string? sortBy, string? sortOrder)
    {
        var descending = sortOrder == GeneralConstants.Descending;
        if (string.IsNullOrEmpty(sortBy) || !LegacyMovieSort.TryFromName(sortBy, false, out var sort))
        {
            return query.OrderByDescending(movie => movie.Id);
        }

        return Order(query, sort, descending);
    }

    private static IQueryable<Movie> Order(IQueryable<Movie> query, LegacyMovieSort sort, bool descending)
    {
        if (sort == LegacyMovieSort.Title)
        {
            return OrderByTitle(query, descending);
        }

        if (sort == LegacyMovieSort.Year)
        {
            return OrderByValue(query, movie => movie.Year, descending);
        }

        if (sort == LegacyMovieSort.Duration)
        {
            return OrderByValue(query, movie => movie.Duration, descending);
        }

        if (sort == LegacyMovieSort.ImdbRating)
        {
            return OrderByValue(query, movie => movie.ImdbRating, descending);
        }

        if (sort == LegacyMovieSort.MetaScore)
        {
            return OrderByValue(query, movie => movie.MetaScore, descending);
        }

        if (sort == LegacyMovieSort.RottenTomatoes)
        {
            return OrderByValue(query, movie => movie.RottenTomatoes, descending);
        }

        return OrderByValue(query, movie => movie.Id, descending);
    }

    private static IQueryable<Movie> OrderByTitle(IQueryable<Movie> query, bool descending)
    {
        var ranked = query.OrderBy(movie => movie.Title == null ? 1 : 0);
        return descending ? ranked.ThenByDescending(movie => movie.Title) : ranked.ThenBy(movie => movie.Title);
    }

    private static IQueryable<Movie> OrderByValue<T>(IQueryable<Movie> query, Expression<Func<Movie, T>> key, bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);

    private static IQueryable<Movie> FilterByIds(
        IQueryable<Movie> query,
        IEnumerable<int>? ids,
        Func<int[], Expression<Func<Movie, bool>>> predicate)
    {
        if (ids is null)
        {
            return query;
        }

        var values = ids.ToArray();
        return values.Length == 0 ? query : query.Where(predicate(values));
    }
}
