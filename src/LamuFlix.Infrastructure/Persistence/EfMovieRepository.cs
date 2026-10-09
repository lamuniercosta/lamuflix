using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfMovieRepository : IMovieRepository
{
    private readonly LamuFlixDbContext db;
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan claimLease;
    private readonly Dictionary<MovieId, TrackedMovie> identityMap = [];

    public EfMovieRepository(
        LamuFlixDbContext db,
        TimeProvider timeProvider,
        IOptions<EnrichmentOptions> options)
    {
        this.db = db;
        this.timeProvider = timeProvider;
        claimLease = options.Value.ClaimLease;
        if (claimLease <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), claimLease, "Claim lease must be positive.");
        }
    }

    public async Task<Movie?> GetAsync(MovieId id, CancellationToken ct)
    {
        if (identityMap.TryGetValue(id, out var tracked))
        {
            return tracked.Movie;
        }

        var record = await db.Movies.SingleOrDefaultAsync(movie => movie.Id == id, ct);
        if (record is null)
        {
            return null;
        }

        var movie = ToDomain(record);
        identityMap.Add(id, new TrackedMovie(movie, record, Baseline.From(movie)));
        return movie;
    }

    public Task AddAsync(Movie movie, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (identityMap.ContainsKey(movie.Id))
        {
            throw new InvalidOperationException($"Movie {movie.Id.Value} is already tracked.");
        }

        var record = ToRecord(movie);
        db.Movies.Add(record);
        identityMap.Add(movie.Id, new TrackedMovie(movie, record, Baseline.From(movie)));
        return Task.CompletedTask;
    }

    private static MovieRecord ToRecord(Movie movie)
    {
        var record = new MovieRecord
        {
            Id = movie.Id,
            Title = movie.Title,
            LibraryPath = movie.Path,
            Format = movie.Format,
            IsInWatchlist = movie.IsInWatchlist,
            Status = movie.Status,
            EnrichedAt = movie.EnrichedAt,
            EnrichmentAttempts = movie.EnrichmentAttempts,
            EnrichmentFailureCategory = movie.LastFailureCategory,
            LastAttemptAt = movie.LastAttemptAt,
        };
        CopyMetadata(record, movie.Metadata);
        return record;
    }

    private static void CopyMetadata(MovieRecord record, MovieMetadata? metadata)
    {
        if (metadata is null)
        {
            ClearMetadata(record);
            return;
        }

        record.MetadataTitle = metadata.Title;
        record.Plot = metadata.Synopsis;
        record.ReleaseYear = metadata.ReleaseYear;
        record.RuntimeMinutes = metadata.Runtime;
        record.ImdbRating = metadata.ImdbRating;
        record.ImdbId = metadata.ImdbId;
    }

    private static void ClearMetadata(MovieRecord record)
    {
        record.MetadataTitle = null;
        record.Plot = null;
        record.ReleaseYear = null;
        record.RuntimeMinutes = null;
        record.ImdbRating = null;
        record.ImdbId = null;
    }

    public async Task<MovieId> NextIdentityAsync(CancellationToken ct)
    {
        var value = await db.Database
            .SqlQuery<long>($"SELECT nextval(pg_get_serial_sequence('movies', 'id')) AS \"Value\"")
            .SingleAsync(ct);
        if (value > int.MaxValue || !MovieId.TryCreate((int)value, out var id))
        {
            throw new InvalidOperationException($"The database generated an invalid movie id: {value}.");
        }

        return id;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        foreach (var tracked in identityMap.Values)
        {
            Apply(tracked.Movie, tracked.Record, tracked.Baseline);
        }

        await db.SaveChangesAsync(ct);

        foreach (var tracked in identityMap.Values)
        {
            tracked.Baseline = Baseline.From(tracked.Movie);
        }
    }

    public async Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var leaseExpiry = now - claimLease;
        var rows = await db.Movies
            .Where(movie => movie.Id == id
                && movie.Status == EnrichmentStatus.Pending
                && (movie.LastAttemptAt == null || movie.LastAttemptAt < leaseExpiry))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(movie => movie.LastAttemptAt, now)
                .SetProperty(movie => movie.EnrichmentAttempts, movie => movie.EnrichmentAttempts + 1), ct);

        if (rows != 1)
        {
            return false;
        }

        if (identityMap.TryGetValue(id, out var tracked))
        {
            var attempts = await db.Movies.AsNoTracking()
                .Where(movie => movie.Id == id)
                .Select(movie => movie.EnrichmentAttempts)
                .SingleAsync(ct);
            SetCurrentAndOriginal(db.Entry(tracked.Record).Property(movie => movie.LastAttemptAt), now);
            SetCurrentAndOriginal(db.Entry(tracked.Record).Property(movie => movie.EnrichmentAttempts), attempts);
        }

        return true;
    }

    public async Task<IReadOnlyList<MovieId>> FindStrandedMovieIdsAsync(DateTimeOffset leaseCutoff, CancellationToken ct) =>
        await db.Movies
            .AsNoTracking()
            .Where(movie => movie.Status == EnrichmentStatus.Pending
                && (movie.LastAttemptAt == null || movie.LastAttemptAt < leaseCutoff))
            .Select(movie => movie.Id!)
            .ToArrayAsync(ct);

    private static void Apply(Movie movie, MovieRecord record, Baseline baseline)
    {
        ApplyCore(movie, record);
        ApplyState(movie, record, baseline);
        ApplyAttempts(movie, record, baseline);
        ApplyMetadata(movie, record, baseline);
    }

    private static void ApplyCore(Movie movie, MovieRecord record)
    {
        record.Title = movie.Title;
        record.LibraryPath = movie.Path;
        record.Format = movie.Format;
    }

    private static void ApplyState(Movie movie, MovieRecord record, Baseline baseline)
    {
        if (baseline.IsInWatchlist != movie.IsInWatchlist)
        {
            record.IsInWatchlist = movie.IsInWatchlist;
        }

        if (baseline.Status != movie.Status)
        {
            record.Status = movie.Status;
        }

        if (baseline.EnrichedAt != movie.EnrichedAt)
        {
            record.EnrichedAt = movie.EnrichedAt;
        }
    }

    private static void ApplyAttempts(Movie movie, MovieRecord record, Baseline baseline)
    {
        if (baseline.EnrichmentAttempts != movie.EnrichmentAttempts)
        {
            record.EnrichmentAttempts += movie.EnrichmentAttempts - baseline.EnrichmentAttempts;
        }

        if (baseline.LastFailureCategory != movie.LastFailureCategory)
        {
            record.EnrichmentFailureCategory = movie.LastFailureCategory;
        }

        if (baseline.LastAttemptAt != movie.LastAttemptAt)
        {
            record.LastAttemptAt = movie.LastAttemptAt;
        }
    }

    private static void ApplyMetadata(Movie movie, MovieRecord record, Baseline baseline)
    {
        if (baseline.Metadata != movie.Metadata)
        {
            CopyMetadata(record, movie.Metadata);
        }
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
                record.ImdbId);

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
            lastAttemptAt: record.LastAttemptAt);
    }

    private static void SetCurrentAndOriginal<T>(PropertyEntry<MovieRecord, T> property, T value)
    {
        property.CurrentValue = value;
        property.OriginalValue = value;
    }

    private sealed class TrackedMovie(Movie movie, MovieRecord record, Baseline baseline)
    {
        public Movie Movie { get; } = movie;
        public MovieRecord Record { get; } = record;
        public Baseline Baseline { get; set; } = baseline;
    }

    private sealed record Baseline(
        bool IsInWatchlist,
        EnrichmentStatus Status,
        DateTimeOffset? EnrichedAt,
        int EnrichmentAttempts,
        EnrichmentFailureCategory? LastFailureCategory,
        DateTimeOffset? LastAttemptAt,
        MovieMetadata? Metadata)
    {
        public static Baseline From(Movie movie) => new(
            movie.IsInWatchlist,
            movie.Status,
            movie.EnrichedAt,
            movie.EnrichmentAttempts,
            movie.LastFailureCategory,
            movie.LastAttemptAt,
            movie.Metadata);
    }
}