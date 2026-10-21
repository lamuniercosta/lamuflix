using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfRecommendationCandidateSource(LamuFlixDbContext db) : IRecommendationCandidateSource
{
    private readonly LamuFlixDbContext db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<IReadOnlyList<Movie>> GetCandidatesAsync(MovieId selectedMovieId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var records = await db.Movies
            .AsNoTracking()
            .Where(movie => movie.Status == EnrichmentStatus.Enriched && movie.Id != selectedMovieId)
            .Include(movie => movie.Actors)
            .Include(movie => movie.Directors)
            .Include(movie => movie.Genres)
            .Include(movie => movie.Writers)
            .ToListAsync(ct);

        return [.. records.Select(ToDomain)];
    }

    private static Movie ToDomain(MovieRecord record)
    {
        if (record.Id is null)
        {
            throw new InvalidOperationException("The database returned a movie row without an id.");
        }

        MovieMetadata? metadata = record.MetadataTitle is null
            ? null
            : new MovieMetadata(
                record.MetadataTitle,
                record.Plot,
                record.ReleaseYear,
                record.RuntimeMinutes,
                record.ImdbRating,
                record.ImdbId,
                record.PosterUrl,
                record.RottenTomatoesRating,
                record.MetaScore,
                record.Directors.Select(director => director.Name),
                record.Actors.Select(actor => actor.Name),
                record.Genres.Select(genre => genre.Name),
                record.Writers.Select(writer => writer.Name));

        return Movie.Rehydrate(
            id: record.Id,
            title: record.Title,
            path: record.LibraryPath,
            format: record.Format,
            isInWatchlist: record.IsInWatchlist,
            metadata: metadata,
            status: record.Status,
            enrichedAt: record.EnrichedAt,
            enrichmentAttempts: record.EnrichmentAttempts,
            lastFailureCategory: record.EnrichmentFailureCategory,
            lastAttemptAt: record.LastAttemptAt,
            parsedReleaseYear: record.ParsedReleaseYear,
            manualImdbId: record.ManualImdbId,
            isWatched: record.IsWatched,
            isNotInterested: record.IsNotInterested);
    }
}
