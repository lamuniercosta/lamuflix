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
    public async Task<PagedResult<MovieSummary>> BrowseAsync(MovieQuery query, CancellationToken ct)
    {
        var filtered = dbContext.Movies.AsNoTracking()
            .WhereText(query.Text)
            .WhereGenres(query.GenreIds)
            .WhereActors(query.ActorIds)
            .WhereRuntime(query.Runtime)
            .WhereYear(query.Year)
            .WhereStatuses(query.Statuses)
            .WhereInWatchlist(query.InWatchlist);
        var totalCount = await filtered.CountAsync(ct);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var items = await filtered.OrderByMovieSort(query.Sort!, query.Direction!)
            .Skip((query.Page.Number - 1) * query.Page.Size)
            .Take(query.Page.Size)
            // ReSharper disable once NullableWarningSuppressionIsUsed
            .Select(movie => new MovieSummary(movie.Id!, movie.Title))
            .ToArrayAsync(ct);
        return new PagedResult<MovieSummary>([.. items], totalCount);
    }

    public async Task<MovieDetails?> GetDetailsAsync(MovieId id, CancellationToken ct)
    {
        var record = await dbContext.Movies.AsNoTracking()
            .Where(movie => movie.Id == id)
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
                movie.ImdbId))
            .FirstOrDefaultAsync(ct);
        if (record is null)
        {
            return null;
        }

        var movieId = record.Id;
        if (movieId is null)
        {
            return null;
        }

        var metadata = HasMetadata(record)
            ? new MovieMetadata(
                record.MetadataTitle ?? record.Title,
                record.Plot,
                record.ReleaseYear,
                record.RuntimeMinutes,
                record.ImdbRating,
                record.ImdbId)
            : null;
        return new MovieDetails(movieId, record.Title, record.LibraryPath, record.Format, metadata);
    }

    private static bool HasMetadata(MovieDetailsRecord record) =>
        record.MetadataTitle is not null || record.Plot is not null || record.ReleaseYear is not null
        || record.RuntimeMinutes is not null || record.ImdbRating is not null || record.ImdbId is not null;

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
        ImdbId? ImdbId);
}