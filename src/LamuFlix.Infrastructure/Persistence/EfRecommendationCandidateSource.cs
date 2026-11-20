using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfRecommendationCandidateSource(
    LamuFlixDbContext db) : IRecommendationCandidateSource
{
    private readonly LamuFlixDbContext db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<IReadOnlyList<RecommendationLibraryEntry>> GetLibraryFeaturesAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var rows = await Traversable(db.Movies
                .AsNoTracking()
                .Where(movie => movie.Status == EnrichmentStatus.Enriched))
            .OrderBy(movie => movie.Id)
            .ToListAsync(ct);

        ct.ThrowIfCancellationRequested();

        var vectors = await db.MovieFeatures
            .AsNoTracking()
            .Select(feature => new { feature.MovieId, feature.Vector })
            .ToDictionaryAsync(feature => feature.MovieId.Value, feature => feature.Vector, ct);

        ct.ThrowIfCancellationRequested();

        var entries = new List<RecommendationLibraryEntry>(rows.Count);
        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            var movieId = row.Id
                ?? throw new InvalidOperationException("The database returned a movie row without an id.");
            entries.Add(ToLibraryEntry(ToDomain(row), vectors.GetValueOrDefault(movieId.Value)));
        }

        return entries;
    }

    public async Task<IReadOnlyList<RecommendationCandidate>> GetCandidatesAsync(
        MovieId selectedMovieId,
        int? collectionId,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        _ = collectionId;

        var selectedVector = await ReadSelectedVectorAsync(selectedMovieId, ct);
        if (selectedVector is null)
        {
            return [];
        }

        ct.ThrowIfCancellationRequested();

        var library = await GetLibraryFeaturesAsync(ct);
        ct.ThrowIfCancellationRequested();

        var selected = selectedVector.ToArray();
        var candidates = new List<RecommendationCandidate>(library.Count);

        foreach (var entry in library)
        {
            ct.ThrowIfCancellationRequested();

            if (entry.Id == selectedMovieId)
            {
                continue;
            }

            candidates.Add(new RecommendationCandidate(entry, SelectedRelativeCosine(selected, entry.Features.PlotEmbedding)));
        }

        return [.. candidates
            .OrderByDescending(candidate => candidate.CosineSimilarity)
            .ThenBy(candidate => candidate.Library.Id.Value)];
    }

    private static IQueryable<MovieRecord> Traversable(IQueryable<MovieRecord> query) =>
        query
            .Include(movie => movie.Actors)
            .Include(movie => movie.Directors)
            .Include(movie => movie.Genres)
            .Include(movie => movie.Writers)
            .Include(movie => movie.Keywords)
            .Include(movie => movie.FullCast)
            .Include(movie => movie.Collection)
            .AsSplitQuery();

    private static RecommendationLibraryEntry ToLibraryEntry(Movie movie, Vector? vector)
    {
        var embedding = vector is not null && IsUsable(vector) ? vector.ToArray() : null;
        return new RecommendationLibraryEntry(
            MovieFeaturesMapper.From(movie, includeSignals: true, plotEmbedding: embedding),
            MovieSummaryMapper.From(movie));
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

    private static double SelectedRelativeCosine(float[] selected, ImmutableArray<float> candidate)
    {
        if (candidate.IsDefaultOrEmpty || candidate.Length != selected.Length)
        {
            return 0.0;
        }

        var dot = 0.0;
        var selectedNormSquared = 0.0;
        var candidateNormSquared = 0.0;

        for (var index = 0; index < selected.Length; index++)
        {
            var left = selected[index];
            var right = candidate[index];
            if (!float.IsFinite(left) || !float.IsFinite(right))
            {
                return 0.0;
            }

            dot += left * right;
            selectedNormSquared += left * left;
            candidateNormSquared += right * right;
        }

        if (selectedNormSquared <= 0.0 || candidateNormSquared <= 0.0)
        {
            return 0.0;
        }

        return Math.Clamp(dot / (Math.Sqrt(selectedNormSquared) * Math.Sqrt(candidateNormSquared)), -1.0, 1.0);
    }

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
                Signals(record),
                record.TrailerKey);

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
            review: record.Review);
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
