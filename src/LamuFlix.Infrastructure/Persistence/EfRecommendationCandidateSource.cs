using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfRecommendationCandidateSource(
    LamuFlixDbContext db,
    IOptions<RecommendationOptions> options) : IRecommendationCandidateSource
{
    private const double CosineDistanceTolerance = 1e-5;

    private readonly LamuFlixDbContext db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly RecommendationOptions options =
        (options is null ? throw new ArgumentNullException(nameof(options)) : options.Value);

    public async Task<IReadOnlyList<RecommendationCandidate>> GetCandidatesAsync(
        MovieId selectedMovieId,
        int? collectionId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var selectedVector = await ReadSelectedVectorAsync(selectedMovieId, ct);
        if (selectedVector is null)
        {
            return [];
        }

        ct.ThrowIfCancellationRequested();

        var rows = await (
                from movie in Traversable(db.Movies
                    .AsNoTracking()
                    .Where(movie => movie.Status == EnrichmentStatus.Enriched && movie.Id != selectedMovieId))
                join feature in db.MovieFeatures.AsNoTracking() on movie.Id equals feature.MovieId
                let distance = feature.Vector.CosineDistance(selectedVector)
                where distance >= -CosineDistanceTolerance
                    && distance <= 2.0 + CosineDistanceTolerance
                orderby distance, movie.Id
                select new { Movie = movie, Distance = distance })
            .Take(options.CandidatePoolSize)
            .ToListAsync(ct);

        ct.ThrowIfCancellationRequested();

        var candidates = new List<RecommendationCandidate>(rows.Count + options.CandidatePoolSize);
        var seen = new HashSet<MovieId>();
        foreach (var row in rows)
        {
            AddCandidate(candidates, seen, ToDomain(row.Movie), ToCosineSimilarity(row.Distance));
        }

        if (collectionId is { } franchiseId)
        {
            await UnionSameCollectionAsync(candidates, seen, selectedMovieId, franchiseId, ct);
        }

        return candidates;
    }

    private async Task UnionSameCollectionAsync(
        List<RecommendationCandidate> candidates,
        HashSet<MovieId> seen,
        MovieId selectedMovieId,
        int collectionId,
        CancellationToken ct)
    {
        var members = await Traversable(db.Movies
                .AsNoTracking()
                .Where(movie => movie.CollectionId == collectionId
                    && movie.Status == EnrichmentStatus.Enriched
                    && movie.Id != selectedMovieId))
            .OrderBy(movie => movie.Id)
            .ToListAsync(ct);

        ct.ThrowIfCancellationRequested();

        foreach (var member in members)
        {
            AddCandidate(candidates, seen, ToDomain(member), 0.0);
        }
    }

    private static IQueryable<MovieRecord> Traversable(IQueryable<MovieRecord> query) =>
        query
            .Include(movie => movie.Actors)
            .Include(movie => movie.Directors)
            .Include(movie => movie.Genres)
            .Include(movie => movie.Writers)
            .Include(movie => movie.Keywords)
            .Include(movie => movie.FullCast)
            .Include(movie => movie.Collection);

    private static void AddCandidate(
        List<RecommendationCandidate> candidates,
        HashSet<MovieId> seen,
        Movie movie,
        double cosineSimilarity)
    {
        if (seen.Add(movie.Id))
        {
            candidates.Add(new RecommendationCandidate(movie, cosineSimilarity));
        }
    }

    private async Task<Vector?> ReadSelectedVectorAsync(MovieId movieId, CancellationToken ct)
    {
        var vector = await db.MovieFeatures
            .AsNoTracking()
            .Where(feature => feature.MovieId == movieId)
            .Select(feature => feature.Vector)
            .SingleOrDefaultAsync(ct);

        return vector is not null && IsUsable(vector) ? vector : null;
    }

    private static bool IsUsable(Vector vector)
    {
        var values = vector.ToArray();
        if (values.Length != EmbeddingDimensions.Value)
        {
            return false;
        }

        var normSquared = 0.0;
        foreach (var value in values)
        {
            if (!float.IsFinite(value))
            {
                return false;
            }

            normSquared += value * value;
        }

        return normSquared > 0.0;
    }

    private static double ToCosineSimilarity(double distance) =>
        Math.Clamp(1.0 - distance, -1.0, 1.0);

    private static Movie ToDomain(MovieRecord record)
    {
        if (record.Id is null)
        {
            throw new InvalidOperationException("The database returned a movie row without an id.");
        }

        var collection = CollectionIdentity(record);
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
                record.Writers.Select(writer => writer.Name),
                collection?.Id,
                collection?.Name,
                Signals(record));

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

    private static (int Id, string Name)? CollectionIdentity(MovieRecord record) =>
        record.CollectionId is { } id && record.Collection is { } collection
            ? (id, collection.Name)
            : null;

    private static MovieSignalSnapshot? Signals(MovieRecord record) =>
        MovieSignalSnapshot.FromPersisted(
            record.Keywords
                .OrderBy(keyword => keyword.Id)
                .Select(keyword => new MovieKeyword(keyword.Id, keyword.Name)),
            record.FullCast.Select(actor => actor.Name),
            record.CollectionId is not null);
}
