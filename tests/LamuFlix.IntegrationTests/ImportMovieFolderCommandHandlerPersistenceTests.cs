using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Npgsql;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

// The import counter lives on a static meter, so ImportEndpointTests would leak into this
// listener if both ran at once; the non-parallel collection keeps the count exact.
[Collection(nameof(TelemetryCompositionCollection))]
public sealed class ImportMovieFolderCommandHandlerPersistenceTests(PostgresFixture postgres)
{
    private const string MeterName = "LamuFlix";
    private const string ImportInstrumentName = "lamuflix.import.count";
    private const string ExactIndexName = "IX_movies_library_path";
    private const string KeyIndexName = "IX_movies_library_path_key";
    private const string LibraryRoot = "C:/library";

    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();

    [Fact]
    public async Task HandleAsync_DuplicateLibraryPath_RealDatabaseRejectsSecondImport_RecordsOneImport()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await postgres.ResetAsync(ct);
        var folder = new LibraryPath("incoming");
        var scanned = new ScannedMovie(new LibraryPath("incoming/file"), "Imported", new MediaFormat("mkv"), null, 1024L);
        scanner.Scan(folder).Returns(scanned);
        using var capture = new ImportCountCapture();

        // act
        MovieId firstId;
        await using (var firstContext = postgres.CreateMigratedContext())
        {
            var firstHandler = NewHandler(firstContext);
            firstId = await firstHandler.HandleAsync(new ImportMovieFolderCommand(folder), ct);
        }

        var baseline = capture.Measurements.Count;

        DbUpdateException rejection;
        await using (var secondContext = postgres.CreateMigratedContext())
        {
            var secondHandler = NewHandler(secondContext);
            rejection = await Should.ThrowAsync<DbUpdateException>(
                () => secondHandler.HandleAsync(new ImportMovieFolderCommand(folder), ct));
        }

        // assert
        var uniqueViolation = rejection.InnerException.ShouldBeOfType<PostgresException>();
        uniqueViolation.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        uniqueViolation.ConstraintName.ShouldBe(ExactIndexName);
        await using var verificationContext = postgres.CreateMigratedContext();
        var persisted = await verificationContext.Movies.ToListAsync(ct);
        var stored = persisted.ShouldHaveSingleItem();
        stored.Id.ShouldBe(firstId);
        stored.LibraryPath.ShouldBe(scanned.Path);
        (await verificationContext.OutboxMessages.CountAsync(ct)).ShouldBe(1);
        baseline.ShouldBe(1);
        capture.Measurements.Count.ShouldBe(baseline);
        var measured = capture.Measurements.ShouldHaveSingleItem();
        measured.InstrumentName.ShouldBe(ImportInstrumentName);
        measured.Value.ShouldBe(1L);
        measured.Tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_CaseOnlyDuplicate_RejectsOnTheKeyIndexAndPreservesTheWinner()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await postgres.ResetAsync(ct);
        var firstFolder = new LibraryPath("Heat (1995)");
        var caseVariedFolder = new LibraryPath("heat (1995)");
        scanner.Scan(firstFolder).Returns(
            new ScannedMovie(firstFolder, "Heat", new MediaFormat("mkv"), null, 1024L));
        scanner.Scan(caseVariedFolder).Returns(
            new ScannedMovie(caseVariedFolder, "Heat Lower", new MediaFormat("mkv"), null, 1024L));

        // act
        MovieId winnerId;
        await using (var firstContext = postgres.CreateMigratedContext())
        {
            winnerId = await NewHandler(firstContext)
                .HandleAsync(new ImportMovieFolderCommand(firstFolder), ct);
        }

        DbUpdateException rejection;
        await using (var secondContext = postgres.CreateMigratedContext())
        {
            rejection = await Should.ThrowAsync<DbUpdateException>(
                () => NewHandler(secondContext)
                    .HandleAsync(new ImportMovieFolderCommand(caseVariedFolder), ct));
        }

        // assert
        ExpectConstraintViolation(rejection, KeyIndexName);
        await using var verification = postgres.CreateMigratedContext();
        var stored = await verification.Movies.ToListAsync(ct);
        var winner = stored.ShouldHaveSingleItem();
        winner.Id.ShouldBe(winnerId);
        winner.LibraryPath.ShouldBe(new LibraryPath("Heat (1995)"));
        winner.LibraryPathKey.ShouldBe("HEAT (1995)");
        (await verification.OutboxMessages.CountAsync(ct)).ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_AbsolutePathUnderRoot_PersistsRelativeIdentityAndDerivedKey()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await postgres.ResetAsync(ct);
        var canonical = new LibraryPath("Collections/Alien (1979)");
        scanner.Scan(canonical).Returns(
            new ScannedMovie(canonical, "Alien", new MediaFormat("mkv"), null, 2048L));

        // act
        await using var context = postgres.CreateMigratedContext();
        var handler = NewHandler(context);
        var id = await handler.HandleAsync(
            new ImportMovieFolderCommand(new LibraryPath($"{LibraryRoot}/Collections/Alien (1979)")),
            ct);

        // assert
        await using var verification = postgres.CreateMigratedContext();
        var stored = await verification.Movies.SingleAsync(movie => movie.Id == id, ct);
        stored.LibraryPath.ShouldBe(canonical);
        stored.LibraryPathKey.ShouldBe("COLLECTIONS/ALIEN (1979)");
    }

    [Fact]
    public async Task HandleAsync_ExplicitRollback_LeavesNeitherMovieNorOutboxDurable()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await postgres.ResetAsync(ct);
        var folder = new LibraryPath("rolled-back");
        var scanned = new ScannedMovie(new LibraryPath("rolled-back/file"), "RolledBack", new MediaFormat("mkv"), null, 1024L);
        scanner.Scan(folder).Returns(scanned);
        await using var context = postgres.CreateMigratedContext();
        var handler = NewHandler(context);

        // act
        await using (var transaction = await context.Database.BeginTransactionAsync(ct))
        {
            await handler.HandleAsync(new ImportMovieFolderCommand(folder), ct);
            await transaction.RollbackAsync(ct);
        }

        // assert
        await using var verificationContext = postgres.CreateMigratedContext();
        (await verificationContext.Movies.CountAsync(ct)).ShouldBe(0);
        (await verificationContext.OutboxMessages.CountAsync(ct)).ShouldBe(0);
    }

    private ImportMovieFolderCommandHandler NewHandler(LamuFlixDbContext context) =>
        new(
            scanner,
            NewRepository(context),
            new EfEnrichmentOutbox(context, TimeProvider.System),
            Options.Create(new LibraryOptions { RootPath = LibraryRoot }));

    private static void ExpectConstraintViolation(Exception? failure, string constraintName)
    {
        var postgres = failure.ShouldNotBeNull();
        var unwrapped = Find(postgres).ShouldNotBeNull();
        unwrapped.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        unwrapped.ConstraintName.ShouldBe(constraintName);
    }

    private static PostgresException? Find(Exception exception)
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

    private static EfMovieRepository NewRepository(LamuFlixDbContext context)
    {
        var options = Options.Create(
            new EnrichmentOptions { ClaimLease = TimeSpan.FromMinutes(5) });
        return new EfMovieRepository(
            context,
            TimeProvider.System,
            options,
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);
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
