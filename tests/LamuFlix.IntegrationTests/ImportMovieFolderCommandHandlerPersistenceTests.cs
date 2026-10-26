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
    private const string DuplicateIndexName = "IX_movies_library_path";

    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();

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
            var firstHandler = new ImportMovieFolderCommandHandler(
                scanner,
                NewRepository(firstContext),
                new EfEnrichmentOutbox(firstContext, TimeProvider.System));
            firstId = await firstHandler.HandleAsync(new ImportMovieFolderCommand(folder), ct);
        }

        var baseline = capture.Measurements.Count;

        DbUpdateException rejection;
        await using (var secondContext = postgres.CreateMigratedContext())
        {
            var secondHandler = new ImportMovieFolderCommandHandler(
                scanner,
                NewRepository(secondContext),
                new EfEnrichmentOutbox(secondContext, TimeProvider.System));
            rejection = await Should.ThrowAsync<DbUpdateException>(
                () => secondHandler.HandleAsync(new ImportMovieFolderCommand(folder), ct));
        }

        // assert
        var uniqueViolation = rejection.InnerException.ShouldBeOfType<PostgresException>();
        uniqueViolation.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        uniqueViolation.ConstraintName.ShouldBe(DuplicateIndexName);
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
    public async Task HandleAsync_ExplicitRollback_LeavesNeitherMovieNorOutboxDurable()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await postgres.ResetAsync(ct);
        var folder = new LibraryPath("C:/library/rolled-back");
        var scanned = new ScannedMovie(new LibraryPath("C:/library/rolled-back/file"), "RolledBack", new MediaFormat("mkv"), null, 1024L);
        scanner.Scan(folder).Returns(scanned);
        await using var context = postgres.CreateMigratedContext();
        var handler = new ImportMovieFolderCommandHandler(
            scanner,
            NewRepository(context),
            new EfEnrichmentOutbox(context, TimeProvider.System));

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
