using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class EfMovieRepositoryBulkImportTests(PostgresFixture fixture) : IAsyncLifetime, IClassFixture<PostgresFixture>
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

    public async ValueTask InitializeAsync() => await fixture.ResetAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task SnapshotMoviePathsAsync_PopulatedLibrary_ReturnsPathsInIdOrder()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var first = await SeedAsync(db, "Thief", EnrichmentStatus.Pending, ct);
        var second = await SeedAsync(db, "Heat", EnrichmentStatus.Enriched, ct);
        var third = await SeedAsync(db, "Ronin", EnrichmentStatus.NotFound, ct);

        // act
        var paths = await Repository(db).SnapshotMoviePathsAsync(ct);

        // assert
        paths.Count.ShouldBe(3);
        paths.Select(path => path.Value).ShouldBe([first.Path.Value, second.Path.Value, third.Path.Value]);
    }

    [Fact]
    public async Task SnapshotMoviePathsAsync_EmptyLibrary_ReturnsEmpty()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();

        // act
        var paths = await Repository(db).SnapshotMoviePathsAsync(ct);

        // assert
        paths.ShouldBeEmpty();
    }

    [Fact]
    public async Task FindFailedMoviesAsync_MixedStatuses_ReturnsOnlyFailedMoviesInIdOrder()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        await SeedAsync(db, "PendingFilm", EnrichmentStatus.Pending, ct);
        await SeedAsync(db, "EnrichedFilm", EnrichmentStatus.Enriched, ct);
        await SeedAsync(db, "NotFoundFilm", EnrichmentStatus.NotFound, ct);
        var first = await SeedAsync(db, "ZedFilm", EnrichmentStatus.Failed, ct);
        var second = await SeedAsync(db, "AshFilm", EnrichmentStatus.Failed, ct);

        // act
        var repository = Repository(db);
        var movies = await repository.FindFailedMoviesAsync(ct);

        // assert
        movies.Select(movie => movie.Id).ShouldBe([first.Id, second.Id]);

        // act
        foreach (var movie in movies)
        {
            movie.RequestEnrichment();
        }

        await repository.SaveChangesAsync(ct);

        // assert
        await using var fresh = fixture.CreateMigratedContext();
        var reread = await Repository(fresh).GetAsync(first.Id, ct);
        reread.ShouldNotBeNull().Status.ShouldBe(EnrichmentStatus.Pending);
    }

    private static async Task<(MovieId Id, LibraryPath Path)> SeedAsync(
        LamuFlixDbContext db,
        string pathSuffix,
        EnrichmentStatus status,
        CancellationToken ct)
    {
        var record = MovieCatalogSeed.Create(pathSuffix, pathSuffix);
        record.Status = status;
        record.ParsedReleaseYear = new ReleaseYear(1995, MovieCatalogSeed.FixedTime);
        record.MetadataTitle = pathSuffix;
        await MovieCatalogSeed.AddAsync(db, record, ct);
        record.Id.ShouldNotBeNull();
        return (record.Id, record.LibraryPath);
    }

    [Fact]
    public async Task DisposeAsync_DeferredNotificationFlushFails_LogsWarningWithoutErrorEntries()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var db = fixture.CreateMigratedContext();
        var log = new CaptureLog();
        var failing = Substitute.For<IEnrichmentStatusNotifier>();
        failing
            .NotifyAsync(Arg.Any<MovieId>(), Arg.Any<EnrichmentStatus>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("broadcast down"));
        var repository = new EfMovieRepository(
            db,
            TimeProvider.System,
            Options.Create(new EnrichmentOptions { ClaimLease = ClaimLease }),
            failing,
            new CaptureLogger(log));
        var movie = Movie.Create(new MovieId(1), "Flush Film", new LibraryPath("FlushFilm"), new MediaFormat("mp4"));
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            await repository.AddAsync(movie, ct);
            await repository.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        // act
        await repository.DisposeAsync();

        // assert
        log.Entries.Where(entry => entry.Level == LogLevel.Error).ShouldBeEmpty();
        var warning = log.Entries.Single(entry => entry.Level == LogLevel.Warning);
        warning.Message.ShouldContain("notification failed");
    }

    private static EfMovieRepository Repository(LamuFlixDbContext db) =>
        new(
            db,
            TimeProvider.System,
            Options.Create(new EnrichmentOptions { ClaimLease = ClaimLease }),
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);

    private sealed class CaptureLog
    {
        private readonly List<LogRecord> entries = [];

        public IReadOnlyList<LogRecord> Entries => entries;

        public void Add(LogRecord record) => entries.Add(record);
    }

    private sealed record LogRecord(LogLevel Level, string Message);

    private sealed class CaptureLogger(CaptureLog log) : ILogger<EfMovieRepository>
    {
        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            log.Add(new LogRecord(logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
