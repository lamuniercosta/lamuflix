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
using Microsoft.Extensions.Options;
using Npgsql;

namespace LamuFlix.Infrastructure.Persistence;

public sealed class EfMovieRepository : IMovieRepository
{
    private const string ImdbIdIndexName = "IX_movies_imdb_id";

    private readonly TimeSpan[] metadataRetryDelays =
    [
        TimeSpan.FromMilliseconds(25),
        TimeSpan.FromMilliseconds(50),
    ];

    private readonly LamuFlixDbContext db;
    private readonly TimeProvider timeProvider;
    private readonly TimeSpan claimLease;
    private readonly GenreWeightsCache cache;
    private readonly HybridCachedRecommendationProfileSource? tasteProfileCache;
    private readonly Dictionary<MovieId, TrackedMovie> identityMap = [];

    public EfMovieRepository(
        LamuFlixDbContext db,
        TimeProvider timeProvider,
        IOptions<EnrichmentOptions> options,
        GenreWeightsCache? cache = null,
        HybridCachedRecommendationProfileSource? tasteProfileCache = null)
    {
        this.db = db;
        this.timeProvider = timeProvider;
        claimLease = options.Value.ClaimLease;
        this.cache = cache ?? new GenreWeightsCache();
        this.tasteProfileCache = tasteProfileCache;
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

        if (rows == 1)
        {
            if (identityMap.Remove(id, out var tracked))
            {
                db.Entry(tracked.Record).State = EntityState.Detached;
            }

            cache.Invalidate();
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
            IsWatched = movie.IsWatched,
            IsNotInterested = movie.IsNotInterested,
            Status = movie.Status,
            EnrichedAt = movie.EnrichedAt,
            EnrichmentAttempts = movie.EnrichmentAttempts,
            EnrichmentFailureCategory = movie.LastFailureCategory,
            LastAttemptAt = movie.LastAttemptAt,
            ParsedReleaseYear = movie.ParsedReleaseYear,
            ManualImdbId = movie.ManualImdbId,
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
        }
        else if (db.Database.CurrentTransaction is not null)
        {
            await RunAmbientMetadataWriteAsync(ct);
        }
        else
        {
            await SaveMetadataWithRetryAsync(ct);
        }

        cache.Invalidate();

        if (tasteProfileCache is not null)
        {
            await tasteProfileCache.InvalidateAsync(ct);
        }
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
            catch (Exception exception) when (attempt <= metadataRetryDelays.Length && IsRetryableConflict(exception))
            {
                await Task.Delay(metadataRetryDelays[attempt - 1], timeProvider, ct);
            }
            catch (Exception exception) when (IsImdbIdConflict(exception))
            {
                ResetAfterDuplicateImdbId();
                throw new DuplicateImdbIdException(exception.Message, exception);
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
        catch (Exception exception) when (IsImdbIdConflict(exception))
        {
            ResetAfterDuplicateImdbId();
            throw new DuplicateImdbIdException(exception.Message, exception);
        }
        catch
        {
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private void ResetAfterDuplicateImdbId()
    {
        db.ChangeTracker.Clear();
        identityMap.Clear();
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

        await AcquireSnapshotLocksAsync(metadataTracked, ct);
        foreach (var tracked in metadataTracked)
        {
            await ReplaceAssociationsAsync(tracked.Record, tracked.Movie.Metadata, ct);
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
                CollectKeys(keys, 1, NormalizeUnique(metadata.Genres));
                CollectKeys(keys, 2, NormalizeUnique(metadata.Directors));
                CollectKeys(keys, 3, NormalizeUnique(metadata.Writers));
                CollectKeys(keys, 4, NormalizeUnique(metadata.Actors));
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

    private static void CollectKeys(List<(int RoleId, string Name)> keys, int roleId, List<string> names)
    {
        foreach (var name in names)
        {
            keys.Add((roleId, Normalize(name)));
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
        var existing = await LoadMatchingAsync(set, desired, nameOf, ct);
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
        var candidates = await set
            .Where(entity => wanted.Contains(EF.Property<string>(entity, "Name").Trim().ToLower()))
            .ToListAsync(ct);
        var matches = new List<TEntity>();
        foreach (var entity in candidates)
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

    private static bool IsImdbIdConflict(Exception exception) =>
        UnwrapPostgres(exception) is { SqlState: "23505", ConstraintName: ImdbIdIndexName };

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

    internal static bool IsRetryableSqlState(string? sqlState, string? constraintName) =>
        sqlState switch
        {
            "40001" or "40P01" => true,
            "23505" => IsMovieOrJoinPrimaryKey(constraintName),
            _ => false,
        };

    internal static bool IsMovieOrJoinPrimaryKey(string? constraintName) =>
        constraintName is not null
        && (string.Equals(constraintName, "PK_movies", StringComparison.Ordinal)
            || constraintName.StartsWith("PK_movie_", StringComparison.Ordinal));

    private static bool IsRetryableSqlState(PostgresException exception) =>
        IsRetryableSqlState(exception.SqlState, exception.ConstraintName);

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

    public async Task<string?> GetPlotAsync(MovieId id, CancellationToken ct) =>
        await db.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == id)
            .Select(movie => movie.Plot)
            .SingleOrDefaultAsync(ct);

    public async Task<MovieId?> FindMovieIdByImdbIdAsync(ImdbId imdbId, CancellationToken ct)
    {
        var ids = await db.Movies
            .AsNoTracking()
            .Where(movie => movie.ImdbId == imdbId)
            .Select(movie => movie.Id)
            .ToArrayAsync(ct);
        return PresentIds(ids).FirstOrDefault();
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
        ApplyState(movie, record, baseline);
        ApplyAttempts(movie, record, baseline);
        ApplyMetadata(movie, record, baseline);
    }

    private static void ApplyState(Movie movie, MovieRecord record, Baseline baseline)
    {
        ApplyWatchState(movie, record, baseline);
        ApplyEnrichmentState(movie, record, baseline);
    }

    private static void ApplyWatchState(Movie movie, MovieRecord record, Baseline baseline)
    {
        if (baseline.IsInWatchlist != movie.IsInWatchlist)
        {
            record.IsInWatchlist = movie.IsInWatchlist;
        }

        if (baseline.IsWatched != movie.IsWatched)
        {
            record.IsWatched = movie.IsWatched;
        }

        if (baseline.IsNotInterested != movie.IsNotInterested)
        {
            record.IsNotInterested = movie.IsNotInterested;
        }
    }

    private static void ApplyEnrichmentState(Movie movie, MovieRecord record, Baseline baseline)
    {
        if (baseline.Status != movie.Status)
        {
            record.Status = movie.Status;
        }

        if (baseline.EnrichedAt != movie.EnrichedAt)
        {
            record.EnrichedAt = movie.EnrichedAt;
        }

        if (baseline.ManualImdbId != movie.ManualImdbId)
        {
            record.ManualImdbId = movie.ManualImdbId;
        }
    }

    private static void ApplyAttempts(Movie movie, MovieRecord record, Baseline baseline)
    {
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
            lastAttemptAt: record.LastAttemptAt,
            parsedReleaseYear: record.ParsedReleaseYear,
            manualImdbId: record.ManualImdbId,
            isWatched: record.IsWatched,
            isNotInterested: record.IsNotInterested);
    }

    private sealed class TrackedMovie(Movie movie, MovieRecord record, Baseline baseline)
    {
        public Movie Movie { get; } = movie;
        public MovieRecord Record { get; set; } = record;
        public Baseline Baseline { get; set; } = baseline;
    }

    private sealed record Baseline(
        bool IsInWatchlist,
        bool IsWatched,
        bool IsNotInterested,
        EnrichmentStatus Status,
        DateTimeOffset? EnrichedAt,
        EnrichmentFailureCategory? LastFailureCategory,
        DateTimeOffset? LastAttemptAt,
        MovieMetadata? Metadata,
        ImdbId? ManualImdbId)
    {
        public static Baseline From(Movie movie) => new(
            movie.IsInWatchlist,
            movie.IsWatched,
            movie.IsNotInterested,
            movie.Status,
            movie.EnrichedAt,
            movie.LastFailureCategory,
            movie.LastAttemptAt,
            movie.Metadata,
            movie.ManualImdbId);
    }
}
