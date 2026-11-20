using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Library;
using LamuFlix.Core.Ports;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfMovieCatalog(LamuFlixDbContext dbContext) : IMovieCatalog
{
    public async Task<KeywordCounts> GetKeywordCountsAsync(CancellationToken ct)
    {
        var rows = await dbContext.Database.SqlQuery<KeywordCountRow>($"""
            WITH library AS (
                SELECT COUNT(*)::bigint AS total_movies FROM movies
            ), keyword_counts AS (
                SELECT keywords.name AS keyword_name, COUNT(DISTINCT movie_keywords.movie_id)::bigint AS movie_count
                FROM movie_keywords
                JOIN keywords ON keywords.id = movie_keywords.keyword_id
                JOIN movies ON movies.id = movie_keywords.movie_id
                GROUP BY keywords.name
            )
            SELECT library.total_movies AS "TotalMovies", keyword_counts.keyword_name AS "KeywordName",
                COALESCE(keyword_counts.movie_count, 0)::bigint AS "MovieCount"
            FROM library LEFT JOIN keyword_counts ON TRUE
            """).ToListAsync(ct);

        var counts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var keywordName = row.KeywordName;
            if (keywordName is not null)
            {
                counts[keywordName] = row.MovieCount;
            }
        }

        return new KeywordCounts(
            rows[0].TotalMovies,
            counts.ToImmutableDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<PagedResult<MovieSummary>> BrowseAsync(MovieQuery query, CancellationToken ct)
    {
        var filtered = dbContext.Movies.AsNoTracking()
            .WhereText(query.Text)
            .WhereGenres(query.GenreIds)
            .WhereActors(query.ActorIds)
            .WhereDirector(query.DirectorId)
            .WhereRuntime(query.Runtime)
            .WhereYear(query.Year)
            .WhereRating(query.Rating)
            .WhereStatuses(query.Statuses)
            .WhereInWatchlist(query.InWatchlist)
            .WhereCollection(query.CollectionId);
        var totalCount = await filtered.CountAsync(ct);
        // ReSharper disable NullableWarningSuppressionIsUsed
        var items = await filtered.OrderByMovieSort(query.Sort!, query.Direction!)
        // ReSharper restore NullableWarningSuppressionIsUsed
            .Skip((query.Page.Number - 1) * query.Page.Size)
            .Take(query.Page.Size)
            // ReSharper disable once NullableWarningSuppressionIsUsed
            // ReSharper disable once NullableWarningSuppressionIsUsed
            .Select(movie => new MovieSummary(
            // ReSharper disable once NullableWarningSuppressionIsUsed
                movie.Id!,
                movie.Title,
                movie.PosterUrl,
                // ReSharper disable once NullableWarningSuppressionIsUsed
                movie.ReleaseYear!.Value,
                // ReSharper disable once NullableWarningSuppressionIsUsed
                movie.RuntimeMinutes!.Minutes,
                // ReSharper disable once NullableWarningSuppressionIsUsed
                movie.ImdbRating!.Value,
                movie.Status,
                movie.IsInWatchlist,
                movie.IsWatched,
                movie.Review,
                movie.EnrichmentFailureCategory))
            .ToArrayAsync(ct);
        return new PagedResult<MovieSummary>([.. items], totalCount);
    }

    public async Task<MovieDetails?> GetDetailsAsync(MovieId id, CancellationToken ct)
    {
        var record = await dbContext.Movies.AsNoTracking()
            .Where(movie => movie.Id == id)
            .AsSplitQuery()
            .Select(movie => new MovieDetailsRecord(
                movie.Id,
                movie.Title,
                movie.LibraryPath,
                movie.Format,
                movie.MetadataTitle,
                movie.Plot,
                movie.ReleaseYear,
                movie.RuntimeMinutes,
                movie.ImdbRating,
                movie.ImdbId,
                movie.PosterUrl,
                movie.RottenTomatoesRating,
                movie.MetaScore,
                movie.TrailerKey,
                movie.IsInWatchlist,
                movie.IsWatched,
                movie.Review,
                movie.Directors.Select(director => director.Name).ToList(),
                movie.Actors.Select(actor => actor.Name).ToList(),
                movie.Genres.Select(genre => genre.Name).ToList(),
                movie.Writers.Select(writer => writer.Name).ToList(),
                movie.Keywords.Select(keyword => new KeywordRow(keyword.Id, keyword.Name)).ToList(),
                movie.FullCast.Select(actor => actor.Name).ToList(),
                movie.CollectionId,
                movie.Collection == null ? null : movie.Collection.Name,
                movie.Status,
                movie.EnrichmentFailureCategory))
            .FirstOrDefaultAsync(ct);
        if (record is null)
        {
            return null;
        }

        // ReSharper disable once NullableWarningSuppressionIsUsed
        var movieId = record.Id!;

        var metadata = HasMetadata(record)
            ? new MovieMetadata(
                record.MetadataTitle ?? record.Title,
                record.Plot,
                record.ReleaseYear,
                record.RuntimeMinutes,
                record.ImdbRating,
                record.ImdbId,
                record.PosterUrl,
                record.RottenTomatoesRating,
                record.MetaScore,
                record.Directors,
                record.Actors,
                record.Genres,
                record.Writers,
                record.CollectionName is null ? null : record.CollectionId,
                record.CollectionName,
                Signals(record),
                record.TrailerKey)
            : null;
        return new MovieDetails(movieId, record.Title, record.LibraryPath, record.Format, metadata, record.IsInWatchlist, record.IsWatched, record.Review, record.Status, record.FailureCategory);
    }

    public async Task<IReadOnlyList<GenreFacet>> GetGenresAsync(CancellationToken ct)
    {
        var facets = await dbContext.Genres.AsNoTracking()
            .OrderBy(genre => genre.Name)
            .ThenBy(genre => genre.Id)
            .Select(genre => new GenreFacet(genre.Id, genre.Name, genre.Movies.Count)).ToArrayAsync(ct);
        return facets;
    }

    public async Task<IReadOnlyList<CollectionFacet>> GetCollectionsAsync(CancellationToken ct)
    {
        var facets = await dbContext.Collections.AsNoTracking()
            .Where(collection => collection.Movies.Any())
            .OrderBy(collection => collection.Name)
            .ThenBy(collection => collection.Id)
            .Select(collection => new CollectionFacet(collection.Id, collection.Name, collection.Movies.Count))
            .ToArrayAsync(ct);
        return facets;
    }

    public async Task<GenreCounts> GetGenreCountsAsync(CancellationToken ct)
    {
        var rows = await dbContext.Database.SqlQueryRaw<GenreCountRow>(
            """
            SELECT CAST(NULL AS text) AS "Genre", (SELECT COUNT(*) FROM movies) AS "MovieCount"
            UNION ALL
            SELECT g.name AS "Genre", COUNT(DISTINCT mg.movie_id) AS "MovieCount"
            FROM genres AS g
            LEFT JOIN movie_genres AS mg ON mg.genre_id = g.id
            GROUP BY g.name
            """).ToArrayAsync(ct);

        var total = 0L;
        foreach (var row in rows)
        {
            if (row.Genre is null)
            {
                total = row.MovieCount;
            }
        }

        var moviesWithGenre = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            if (row.Genre is { } genre)
            {
                moviesWithGenre[genre] = row.MovieCount;
            }
        }

        return new GenreCounts(total, moviesWithGenre);
    }

    public async Task<IReadOnlyList<PersonFacet>> GetPeopleAsync(CancellationToken ct)
    {
        var facets = await dbContext.Actors.AsNoTracking()
            .OrderBy(actor => actor.Name)
            .ThenBy(actor => actor.Id)
            .Select(actor => new PersonFacet(actor.Id, actor.Name))
            .ToArrayAsync(ct);
        return facets;
    }

    public async Task<IReadOnlyList<DirectorFacet>> GetDirectorsAsync(CancellationToken ct)
    {
        var facets = await dbContext.Directors.AsNoTracking()
            .Where(director => director.Movies.Any())
            .OrderBy(director => director.Name)
            .ThenBy(director => director.Id)
            .Select(director => new DirectorFacet(
                director.Id,
                director.Name,
                director.Movies.Select(movie => movie.Id).Distinct().Count()))
            .ToArrayAsync(ct);
        return facets;
    }

    private static bool HasMetadata(MovieDetailsRecord record) =>
        HasPrimaryMetadata(record)
        || HasExtendedMetadata(record)
        || HasCastMetadata(record);

    private static bool HasPrimaryMetadata(MovieDetailsRecord record) =>
        record.MetadataTitle is not null
        || record.Plot is not null
        || record.ReleaseYear is not null
        || record.RuntimeMinutes is not null
        || record.ImdbRating is not null;

    private static bool HasExtendedMetadata(MovieDetailsRecord record) =>
        record.ImdbId is not null
        || record.PosterUrl is not null
        || record.RottenTomatoesRating is not null
        || record.MetaScore is not null
        || record.TrailerKey is not null;

    private static bool HasCastMetadata(MovieDetailsRecord record) =>
        record.Directors.Count > 0
        || record.Actors.Count > 0;

    private static MovieSignalSnapshot? Signals(MovieDetailsRecord record) =>
        MovieSignalSnapshot.FromPersisted(
            record.Keywords
                .OrderBy(keyword => keyword.Id)
                .Select(keyword => new MovieKeyword(keyword.Id, keyword.Name)),
            record.FullCast,
            record.CollectionName is not null);

    private sealed record MovieDetailsRecord(
        MovieId? Id,
        string Title,
        LibraryPath LibraryPath,
        MediaFormat Format,
        string? MetadataTitle,
        string? Plot,
        ReleaseYear? ReleaseYear,
        Runtime? RuntimeMinutes,
        ImdbRating? ImdbRating,
        ImdbId? ImdbId,
        string? PosterUrl,
        short? RottenTomatoesRating,
        short? MetaScore,
        string? TrailerKey,
        bool IsInWatchlist,
        bool IsWatched,
        Review Review,
        List<string> Directors,
        List<string> Actors,
        List<string> Genres,
        List<string> Writers,
        List<KeywordRow> Keywords,
        List<string> FullCast,
        int? CollectionId,
        string? CollectionName,
        EnrichmentStatus Status,
        EnrichmentFailureCategory? FailureCategory);

    private sealed record KeywordRow(int Id, string Name);
}

// ReSharper disable UnusedAutoPropertyAccessor.Global // EF SqlQueryRaw materialization sets these properties reflectively.
internal sealed class KeywordCountRow
{
    public long TotalMovies { get; set; }
    public string? KeywordName { get; set; }
    public long MovieCount { get; set; }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global

// ReSharper disable UnusedAutoPropertyAccessor.Global // EF SqlQueryRaw materialization sets these properties reflectively.
internal sealed class GenreCountRow
{
    public string? Genre { get; set; }

    public long MovieCount { get; set; }
}
// ReSharper restore UnusedAutoPropertyAccessor.Global
