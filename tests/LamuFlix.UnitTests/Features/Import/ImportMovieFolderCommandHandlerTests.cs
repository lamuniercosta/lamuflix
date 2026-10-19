using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using LamuFlix.UnitTests.Pipeline;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Npgsql;

namespace LamuFlix.UnitTests.Features.Import;

/// <summary>
/// This file owns the <c>PostgresFixture</c> the real-persistence case consumes: xUnit creates one
/// instance for the class, runs <c>InitializeAsync</c> (container start plus migration) before the
/// first test and <c>DisposeAsync</c> after the last, and the duplicate-import case calls
/// <c>ResetAsync</c> before it acts. The class carries the single <c>MetricsCollection</c> assignment
/// so every application-meter observer in this file runs with no parallel neighbour.
/// </summary>
[Collection(nameof(MetricsCollection))]
public sealed class ImportMovieFolderCommandHandlerTests : IClassFixture<PostgresFixture>
{
    private const string MeterName = "LamuFlix";
    private const string ImportInstrumentName = "lamuflix.import.count";
    private const string DuplicateIndexName = "IX_movies_library_path";

    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IEnrichmentQueue queue = Substitute.For<IEnrichmentQueue>();
    private readonly ImportMovieFolderCommandHandler handler;
    private readonly PostgresFixture postgres;
    private readonly Fixture fixture = new();

    public ImportMovieFolderCommandHandlerTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
        handler = new ImportMovieFolderCommandHandler(scanner, movies, queue);
    }

    [Fact]
    public async Task HandleAsync_HappyPath_ScanCreateAddSaveEnqueue()
    {
        // arrange
        var folder = new LibraryPath("C:/library/incoming");
        var id = NewId();
        var scanned = new ScannedMovie(new LibraryPath("C:/library/incoming/file"), "Imported", new MediaFormat("mkv"), null, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(folder).Returns(scanned);
        movies.NextIdentityAsync(ct).Returns(id);
        using var capture = new ImportCountCapture();

        // act
        var result = await handler.HandleAsync(new ImportMovieFolderCommand(folder), ct);

        // assert
        result.ShouldBe(id);
        Received.InOrder(() =>
        {
            scanner.Scan(folder);
            movies.NextIdentityAsync(ct);
            movies.AddAsync(Arg.Is<Movie>(movie => movie.Id == id && movie.Title == scanned.Title), ct);
            movies.SaveChangesAsync(ct);
            queue.EnqueueAsync(Arg.Is<EnrichmentRequested>(message => message.MovieId == id && message.Attempt == 1), ct);
        });
        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(ImportInstrumentName);
        measured.Value.ShouldBe(1L);
        measured.Tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ScannedYear_IsPersistedAsParsedReleaseYear()
    {
        // arrange
        var folder = new LibraryPath("C:/library/incoming");
        var id = NewId();
        var year = new ReleaseYear(2012, new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero));
        var scanned = new ScannedMovie(new LibraryPath("C:/library/incoming/file"), "2 Days in New York", new MediaFormat("mkv"), year, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(folder).Returns(scanned);
        movies.NextIdentityAsync(ct).Returns(id);

        // act
        await handler.HandleAsync(new ImportMovieFolderCommand(folder), ct);

        // assert
        await movies.Received(1).AddAsync(
            Arg.Is<Movie>(movie => movie.ParsedReleaseYear == year && movie.Title == "2 Days in New York"),
            ct);
    }

    [Fact]
    public async Task HandleAsync_QueueFailure_PropagatesAfterSave()
    {
        // arrange
        var folder = new LibraryPath("C:/library/incoming");
        var id = NewId();
        var scanned = new ScannedMovie(new LibraryPath("C:/library/incoming/file"), "Imported", new MediaFormat("mkv"), null, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(folder).Returns(scanned);
        movies.NextIdentityAsync(ct).Returns(id);
        queue.EnqueueAsync(Arg.Any<EnrichmentRequested>(), ct)
            .ThrowsAsync(new InvalidOperationException("broker"));
        using var capture = new ImportCountCapture();

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        await movies.Received(1).SaveChangesAsync(ct);
        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(ImportInstrumentName);
        measured.Value.ShouldBe(1L);
        measured.Tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_AddThrows_DoesNotSaveOrEnqueue()
    {
        // arrange
        var folder = new LibraryPath("C:/library/incoming");
        var id = NewId();
        var scanned = new ScannedMovie(new LibraryPath("C:/library/incoming/file"), "Imported", new MediaFormat("mkv"), null, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(folder).Returns(scanned);
        movies.NextIdentityAsync(ct).Returns(id);
        movies.AddAsync(Arg.Any<Movie>(), ct).ThrowsAsync(new InvalidOperationException("add"));
        using var capture = new ImportCountCapture();

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await queue.DidNotReceive().EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_SaveThrows_DoesNotEnqueue()
    {
        // arrange
        var folder = new LibraryPath("C:/library/incoming");
        var id = NewId();
        var scanned = new ScannedMovie(new LibraryPath("C:/library/incoming/file"), "Imported", new MediaFormat("mkv"), null, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(folder).Returns(scanned);
        movies.NextIdentityAsync(ct).Returns(id);
        movies.SaveChangesAsync(ct).ThrowsAsync(new InvalidOperationException("save"));
        using var capture = new ImportCountCapture();

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        await movies.Received(1).AddAsync(Arg.Any<Movie>(), ct);
        await queue.DidNotReceive().EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ScanThrows_RecordsNoImportMeasurement()
    {
        // arrange
        var folder = new LibraryPath("C:/library/incoming");
        var ct = CancellationToken.None;
        scanner.Scan(folder).Throws(new InvalidOperationException("scan"));
        using var capture = new ImportCountCapture();

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        await movies.DidNotReceive().AddAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await queue.DidNotReceive().EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_DuplicateLibraryPath_RealDatabaseRejectsSecondImport_RecordsOneImport()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await postgres.ResetAsync(ct);
        var folder = new LibraryPath("C:/library/incoming");
        var scanned = new ScannedMovie(new LibraryPath("C:/library/incoming/file"), "Imported", new MediaFormat("mkv"), null, 1024L);
        scanner.Scan(folder).Returns(scanned);
        using var capture = new ImportCountCapture();

        // act
        MovieId firstId;
        await using (var firstContext = postgres.CreateMigratedContext())
        {
            var firstHandler = new ImportMovieFolderCommandHandler(scanner, NewRepository(firstContext), queue);
            firstId = await firstHandler.HandleAsync(new ImportMovieFolderCommand(folder), ct);
        }

        var baseline = capture.Measurements.Count;

        DbUpdateException rejection;
        await using (var secondContext = postgres.CreateMigratedContext())
        {
            var secondHandler = new ImportMovieFolderCommandHandler(scanner, NewRepository(secondContext), queue);
            rejection = await Should.ThrowAsync<DbUpdateException>(
                () => secondHandler.HandleAsync(new ImportMovieFolderCommand(folder), ct));
        }

        // assert
        var uniqueViolation = rejection.InnerException.ShouldBeOfType<PostgresException>();
        uniqueViolation.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        uniqueViolation.ConstraintName.ShouldBe(DuplicateIndexName);
        await queue.Received(1).EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
        await using var verificationContext = postgres.CreateMigratedContext();
        var persisted = await verificationContext.Movies.ToListAsync(ct);
        var stored = persisted.ShouldHaveSingleItem();
        stored.Id.ShouldBe(firstId);
        stored.LibraryPath.ShouldBe(scanned.Path);
        baseline.ShouldBe(1);
        capture.Measurements.Count.ShouldBe(baseline);
        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(ImportInstrumentName);
        measured.Value.ShouldBe(1L);
        measured.Tags.ShouldBeEmpty();
    }

    private MovieId NewId() => new(Math.Abs(fixture.Create<int>()) + 1);

    private static EfMovieRepository NewRepository(LamuFlixDbContext context)
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) });
        return new EfMovieRepository(context, TimeProvider.System, options);
    }

    private sealed class ImportCountCapture : IDisposable
    {
        private readonly ConcurrentQueue<CapturedImport> measurements = [];
        private readonly MeterListener listener = new();

        public ImportCountCapture()
        {
            listener.SetMeasurementEventCallback<long>(
                (instrument, value, tags, _) => measurements.Enqueue(new(instrument.Name, value, tags.ToArray())));
            listener.InstrumentPublished = (instrument, publishedListener) =>
            {
                if (string.Equals(instrument.Meter.Name, MeterName, StringComparison.Ordinal)
                    && string.Equals(instrument.Name, ImportInstrumentName, StringComparison.Ordinal))
                {
                    publishedListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.Start();
        }

        public IReadOnlyList<CapturedImport> Measurements => [.. measurements];

        public void Dispose() => listener.Dispose();
    }

    private sealed record CapturedImport(string InstrumentName, long Value, KeyValuePair<string, object?>[] Tags);
}
