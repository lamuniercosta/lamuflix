using System;
using System.Collections.Generic;
using System.Data;
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
using Npgsql;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfMovieRepository : IMovieRepository
{
    private static readonly TimeSpan[] MetadataRetryDelays =
    [
        TimeSpan.FromMilliseconds(25),
        TimeSpan.FromMilliseconds(50),
    ];

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

        var record = await LoadTrackedRecordAsync(id, ct);
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

    public async Task<bool> TryDeleteAsync(MovieId id, CancellationToken ct)
    {
        var rows = await db.Movies
            .Where(movie => movie.Id == id)
            .ExecuteDeleteAsync(ct);

        if (rows == 1 && identityMap.Remove(id, out var tracked))
        {
            db.Entry(tracked.Record).State = EntityState.Detached;
        }

        return rows == 1;
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
        record.PosterUrl = metadata.PosterUrl;
        record.MetaScore = metadata.MetaScore;
    }

    private static void ClearMetadata(MovieRecord record)
    {
        record.MetadataTitle = null;
        record.Plot = null;
        record.ReleaseYear = null;
        record.RuntimeMinutes = null;
        record.ImdbRating = null;
        record.ImdbId = null;
        record.PosterUrl = null;
        record.MetaScore = null;
        record.Actors.Clear();
        record.Directors.Clear();
        record.Genres.Clear();
        record.Writers.Clear();
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
        if (!HasMetadataWrites())
        {
            await ApplyAndPersistAsync(ct);
            return;
        }

        if (db.Database.CurrentTransaction is not null)
        {
            await RunAmbientMetadataWriteAsync(ct);
            return;
        }

        await SaveMetadataWithRetryAsync(ct);
    }

    private async Task ApplyAndPersistAsync(CancellationToken ct)
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

    private bool HasMetadataWrites() =>
        identityMap.Values.Any(tracked => tracked.Baseline.Metadata != tracked.Movie.Metadata);

    private async Task SaveMetadataWithRetryAsync(CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var transaction = await db.Database
                    .BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
                await RunMetadataWriteAsync(ct);
                await transaction.CommitAsync(ct);
                return;
            }
            catch (Exception exception) when (attempt <= MetadataRetryDelays.Length && IsRetryableConflict(exception))
            {
                db.ChangeTracker.Clear();
                await Task.Delay(MetadataRetryDelays[attempt - 1], ct);
            }
            catch
            {
                db.ChangeTracker.Clear();
                throw;
            }
        }
    }

    private async Task RunAmbientMetadataWriteAsync(CancellationToken ct)
    {
        try
        {
            await RunMetadataWriteAsync(ct);
        }
        catch
        {
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private async Task RunMetadataWriteAsync(CancellationToken ct)
    {
        await RebuildTrackingAsync(ct);

        var metadataTracked = identityMap.Values
            .Where(tracked => tracked.Baseline.Metadata != tracked.Movie.Metadata)
            .ToList();

        foreach (var tracked in identityMap.Values)
        {
            Apply(tracked.Movie, tracked.Record, tracked.Baseline);
        }

        if (metadataTracked.Count > 0)
        {
            await AcquireSnapshotLocksAsync(metadataTracked, ct);
            foreach (var tracked in metadataTracked)
            {
                await ReplaceAssociationsAsync(tracked.Record, tracked.Movie.Metadata, ct);
            }
        }

        await db.SaveChangesAsync(ct);

        foreach (var tracked in identityMap.Values)
        {
            tracked.Baseline = Baseline.From(tracked.Movie);
        }
    }

    private async Task RebuildTrackingAsync(CancellationToken ct)
    {
        db.ChangeTracker.Clear();
        foreach (var tracked in identityMap.Values)
        {
            var record = await LoadTrackedRecordAsync(tracked.Movie.Id, ct);
            if (record is null)
            {
                record = ToRecord(tracked.Movie);
                db.Movies.Add(record);
            }

            tracked.Record = record;
        }
    }

    private Task<MovieRecord?> LoadTrackedRecordAsync(MovieId id, CancellationToken ct) =>
        db.Movies
            .Include(movie => movie.Actors)
            .Include(movie => movie.Directors)
            .Include(movie => movie.Genres)
            .Include(movie => movie.Writers)
            .SingleOrDefaultAsync(movie => movie.Id == id, ct);

    private async Task AcquireSnapshotLocksAsync(IReadOnlyList<TrackedMovie> tracked, CancellationToken ct)
    {
        var keys = new List<(int RoleId, string Name)>();
        foreach (var item in tracked)
        {
            if (item.Movie.Metadata is { } metadata)
            {
                CollectKeys(keys, 1, metadata.Genres);
                CollectKeys(keys, 2, metadata.Directors);
                CollectKeys(keys, 3, metadata.Writers);
                CollectKeys(keys, 4, metadata.Actors);
            }
        }

        var ordered = keys
            .Distinct()
            .OrderBy(key => key.RoleId)
            .ThenBy(key => key.Name, StringComparer.Ordinal);

        foreach (var (roleId, name) in ordered)
        {
            await db.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock({0}, hashtext({1}))",
                [roleId, name],
                ct);
        }
    }

    private static void CollectKeys(List<(int RoleId, string Name)> keys, int roleId, IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            var normalized = Normalize(name);
            if (normalized.Length > 0)
            {
                keys.Add((roleId, normalized));
            }
        }
    }

    private async Task ReplaceAssociationsAsync(MovieRecord record, MovieMetadata? metadata, CancellationToken ct)
    {
        if (metadata is null)
        {
            record.Actors.Clear();
            record.Directors.Clear();
            record.Genres.Clear();
            record.Writers.Clear();
            return;
        }

        await ReplaceRoleAsync(db.Actors, record.Actors, metadata.Actors, actor => actor.Name,
            name => new ActorRecord { Name = name }, ct);
        await ReplaceRoleAsync(db.Directors, record.Directors, metadata.Directors, director => director.Name,
            name => new DirectorRecord { Name = name }, ct);
        await ReplaceRoleAsync(db.Genres, record.Genres, metadata.Genres, genre => genre.Name,
            name => new GenreRecord { Name = name }, ct);
        await ReplaceRoleAsync(db.Writers, record.Writers, metadata.Writers, writer => writer.Name,
            name => new WriterRecord { Name = name }, ct);
    }

    private static async Task ReplaceRoleAsync<TEntity>(
        DbSet<TEntity> set,
        ICollection<TEntity> collection,
        IEnumerable<string> names,
        Func<TEntity, string> nameOf,
        Func<string, TEntity> create,
        CancellationToken ct)
        where TEntity : class
    {
        var desired = NormalizeUnique(names);
        var existing = desired.Count == 0
            ? []
            : await LoadMatchingAsync(set, desired, nameOf, ct);
        var byKey = new Dictionary<string, TEntity>(StringComparer.Ordinal);
        foreach (var entity in existing)
        {
            byKey[Normalize(nameOf(entity))] = entity;
        }

        collection.Clear();
        foreach (var name in desired)
        {
            var key = Normalize(name);
            if (!byKey.TryGetValue(key, out var entity))
            {
                entity = create(name);
                set.Add(entity);
                byKey[key] = entity;
            }

            collection.Add(entity);
        }
    }

    private static async Task<List<TEntity>> LoadMatchingAsync<TEntity>(
        DbSet<TEntity> set,
        IReadOnlyList<string> candidateNames,
        Func<TEntity, string> nameOf,
        CancellationToken ct)
        where TEntity : class
    {
        var wanted = new HashSet<string>(candidateNames.Select(Normalize), StringComparer.Ordinal);
        var all = await set.ToListAsync(ct);
        var matches = new List<TEntity>();
        foreach (var entity in all)
        {
            if (wanted.Contains(Normalize(nameOf(entity))))
            {
                matches.Add(entity);
            }
        }

        return matches;
    }

    private static List<string> NormalizeUnique(IEnumerable<string> names)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in names)
        {
            var trimmed = raw.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (seen.Add(Normalize(trimmed)))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    private static string Normalize(string name) => name.Trim().ToLowerInvariant();

    private static bool IsRetryableConflict(Exception exception) =>
        UnwrapPostgres(exception) is { } postgres && IsRetryableSqlState(postgres);

    private static PostgresException? UnwrapPostgres(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }

    private static bool IsRetryableSqlState(PostgresException exception) =>
        exception.SqlState switch
        {
            "40001" or "40P01" => true,
            "23505" => IsMovieOrJoinPrimaryKey(exception.ConstraintName),
            _ => false,
        };

    private static bool IsMovieOrJoinPrimaryKey(string? constraintName) =>
        constraintName is not null
        && (string.Equals(constraintName, "PK_movies", StringComparison.Ordinal)
            || constraintName.StartsWith("PK_movie_", StringComparison.Ordinal));

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

    public async Task<IReadOnlyList<MovieId>> FindStrandedMovieIdsAsync(DateTimeOffset leaseCutoff, CancellationToken ct)
    {
        var ids = await db.Movies
            .AsNoTracking()
            .Where(movie => movie.Status == EnrichmentStatus.Pending
                && (movie.LastAttemptAt == null || movie.LastAttemptAt < leaseCutoff))
            .Select(movie => movie.Id)
            .ToArrayAsync(ct);
        return PresentIds(ids);
    }

    private static MovieId[] PresentIds(MovieId?[] ids)
    {
        var present = new List<MovieId>(ids.Length);
        foreach (var id in ids)
        {
            if (id is not null)
            {
                present.Add(id);
            }
        }

        return [.. present];
    }

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
        public MovieRecord Record { get; set; } = record;
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
