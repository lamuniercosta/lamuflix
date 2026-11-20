using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;

namespace LamuFlix.Evaluation.RealLibrary;

public static class RealLibrarySnapshotLoader
{
    private const string ConnectionStringVariable = "ConnectionStrings__DefaultConnection";

    public static string CredentialVariableName => ConnectionStringVariable;

    public static bool IsCredentialPresent(Func<string, string?> readVariable)
    {
        ArgumentNullException.ThrowIfNull(readVariable);

        return !string.IsNullOrWhiteSpace(readVariable(ConnectionStringVariable));
    }

    public static LamuFlixDbContext OpenContext(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return new LamuFlixDbContext(
            new DbContextOptionsBuilder<LamuFlixDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseVector())
                .Options);
    }

    public static async Task<RealLibrarySnapshot> LoadAsync(
        LamuFlixDbContext db,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(db);

        ct.ThrowIfCancellationRequested();

        var rows = await ReadMovieRowsAsync(db, ct);
        ct.ThrowIfCancellationRequested();

        var embeddings = await ReadEmbeddingsAsync(db, ct);
        ct.ThrowIfCancellationRequested();

        var launches = await ReadLaunchesAsync(db, ct);
        ct.ThrowIfCancellationRequested();

        var genreCounts = await new EfMovieCatalog(db).GetGenreCountsAsync(ct);

        return new RealLibrarySnapshot(
            [.. rows.Select(SnapshotMapper.ToDomain)],
            SnapshotMapper.BuildProfileMovies(rows, launches, embeddings),
            embeddings,
            genreCounts);
    }

    private static async Task<List<MovieRecord>> ReadMovieRowsAsync(LamuFlixDbContext db, CancellationToken ct) =>
        await db.Movies
            .AsNoTracking()
            .Include(movie => movie.Genres)
            .Include(movie => movie.Actors)
            .Include(movie => movie.Directors)
            .Include(movie => movie.Writers)
            .Include(movie => movie.Keywords)
            .Include(movie => movie.FullCast)
            .Include(movie => movie.Collection)
            .AsSplitQuery()
            .OrderBy(movie => movie.Id)
            .ToListAsync(ct);

    private static async Task<ImmutableDictionary<MovieId, ImmutableArray<float>>> ReadEmbeddingsAsync(
        LamuFlixDbContext db,
        CancellationToken ct)
    {
        var rows = await db.MovieFeatures
            .AsNoTracking()
            .Select(feature => new { feature.MovieId, feature.Vector })
            .ToListAsync(ct);

        var builder = ImmutableDictionary.CreateBuilder<MovieId, ImmutableArray<float>>();

        foreach (var row in rows)
        {
            builder[row.MovieId] = [.. row.Vector.ToArray()];
        }

        return builder.ToImmutable();
    }

    private static async Task<ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>>> ReadLaunchesAsync(
        LamuFlixDbContext db,
        CancellationToken ct)
    {
        var events = await db.PlaybackEvents
            .AsNoTracking()
            .Select(playbackEvent => new { playbackEvent.MovieId, playbackEvent.PlayedAt })
            .ToListAsync(ct);

        var builder = ImmutableDictionary.CreateBuilder<MovieId, ImmutableArray<DateTimeOffset>>();

        foreach (var launchGroup in events.GroupBy(playbackEvent => playbackEvent.MovieId))
        {
            builder[launchGroup.Key] =
            [
                .. launchGroup
                    .Select(playbackEvent => playbackEvent.PlayedAt)
                    .OrderBy(playedAt => playedAt),
            ];
        }

        return builder.ToImmutable();
    }
}

public sealed class RealLibrarySnapshot
{
    private readonly GenreWeights genreWeights;
    private readonly KeywordCounts keywordCounts;

    public RealLibrarySnapshot(
        ImmutableArray<Movie> movies,
        ImmutableArray<TasteProfileMovie> profileMovies,
        ImmutableDictionary<MovieId, ImmutableArray<float>> plotEmbeddings,
        GenreCounts genreCounts)
    {
        if (movies.IsDefaultOrEmpty)
        {
            throw new ArgumentException("A snapshot must contain library movies.", nameof(movies));
        }

        if (profileMovies.IsDefault)
        {
            throw new ArgumentException("Snapshot profile movies must be initialized.", nameof(profileMovies));
        }

        Movies = OrderByTitle(movies);
        ProfileMovies = profileMovies;
        PlotEmbeddings = plotEmbeddings ?? throw new ArgumentNullException(nameof(plotEmbeddings));
        GenreCounts = genreCounts ?? throw new ArgumentNullException(nameof(genreCounts));
        genreWeights = GenreWeights.Calculate(GenreCounts);
        keywordCounts = BuildKeywordCounts(Movies);
    }

    public ImmutableArray<Movie> Movies { get; }

    public ImmutableArray<TasteProfileMovie> ProfileMovies { get; }

    public ImmutableDictionary<MovieId, ImmutableArray<float>> PlotEmbeddings { get; }

    public GenreCounts GenreCounts { get; }

    public GenreWeights GenreWeights => genreWeights;

    public KeywordCounts KeywordCounts => keywordCounts;

    public bool Contains(MovieId movieId) => Movies.Any(movie => movie.Id == movieId);

    private static KeywordCounts BuildKeywordCounts(ImmutableArray<Movie> movies)
    {
        var counts = movies
            .SelectMany(movie => movie.Metadata?.Signals?.Keywords.Select(keyword => (movie.Id, keyword.Name)) ?? [])
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (long)group.Select(item => item.Id).Distinct().Count(),
                StringComparer.OrdinalIgnoreCase);
        return new KeywordCounts(movies.Length, counts);
    }

    private static ImmutableArray<Movie> OrderByTitle(ImmutableArray<Movie> movies) =>
    [
        .. movies
            .OrderBy(movie => movie.Title, StringComparer.Ordinal)
            .ThenBy(movie => movie.Id.Value),
    ];
}

internal static class SnapshotMapper
{
    private static MovieId RequireId(MovieRecord record) =>
        record.Id ?? throw new InvalidOperationException("The database returned a movie row without an id.");

    public static Movie ToDomain(MovieRecord record)
    {
        var collectionId = record.CollectionId is { } availableId && record.Collection is not null
            ? availableId
            : (int?)null;

        return Movie.Rehydrate(
            id: RequireId(record),
            title: record.Title,
            path: record.LibraryPath,
            format: record.Format,
            isInWatchlist: record.IsInWatchlist,
            metadata: BuildMetadata(record, collectionId),
            status: record.Status,
            enrichedAt: record.EnrichedAt,
            enrichmentAttempts: record.EnrichmentAttempts,
            lastFailureCategory: record.EnrichmentFailureCategory,
            lastAttemptAt: record.LastAttemptAt,
            parsedReleaseYear: record.ParsedReleaseYear,
            manualImdbId: record.ManualImdbId,
            isWatched: record.IsWatched,
            review: record.Review,
            enrichmentDeferredUntil: record.EnrichmentDeferredUntil);
    }

    public static ImmutableArray<TasteProfileMovie> BuildProfileMovies(
        List<MovieRecord> rows,
        ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>> launches,
        ImmutableDictionary<MovieId, ImmutableArray<float>> plotEmbeddings)
    {
        var builder = ImmutableArray.CreateBuilder<TasteProfileMovie>();

        foreach (var row in rows)
        {
            var movieId = RequireId(row);
            if (!ContributesToProfile(row, launches))
            {
                continue;
            }

            builder.Add(new TasteProfileMovie(
                movieId,
                [.. row.Genres.Select(genre => genre.Name).Order(StringComparer.Ordinal)],
                plotEmbeddings.GetValueOrDefault(movieId, ImmutableArray<float>.Empty),
                launches.GetValueOrDefault(movieId, ImmutableArray<DateTimeOffset>.Empty),
                row.IsWatched)
            {
                Review = row.Review,
                Title = row.Title,
                Cast = CastNames(row),
                Directors = [.. row.Directors.Select(director => director.Name).Order(StringComparer.Ordinal)],
            });
        }

        return builder.ToImmutable();
    }

    private static bool ContributesToProfile(
        MovieRecord row,
        ImmutableDictionary<MovieId, ImmutableArray<DateTimeOffset>> launches) =>
        row.IsWatched || row.Review != Review.None || launches.ContainsKey(RequireId(row));

    private static ImmutableArray<string> CastNames(MovieRecord row) =>
    [
        .. row.Actors
            .Select(actor => actor.Name)
            .Concat(row.FullCast.Select(actor => actor.Name))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal),
    ];

    private static MovieMetadata? BuildMetadata(MovieRecord record, int? collectionId)
    {
        if (record.MetadataTitle is null)
        {
            return null;
        }

        return new MovieMetadata(
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
            collectionId,
            collectionId is null ? null : record.Collection?.Name,
            Signals(record),
            record.TrailerKey);
    }

    private static MovieSignalSnapshot? Signals(MovieRecord record) =>
        MovieSignalSnapshot.FromPersisted(
            record.Keywords
                .OrderBy(keyword => keyword.Id)
                .Select(keyword => new MovieKeyword(keyword.Id, keyword.Name)),
            record.FullCast.Select(actor => actor.Name),
            record.CollectionId is not null);
}
