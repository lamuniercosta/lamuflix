using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Ports;
using LamuFlix.UnitTests.Pipeline;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Import;

/// <summary>
/// The class carries the single <c>MetricsCollection</c> assignment
/// so every application-meter observer in this file runs with no parallel neighbour.
/// </summary>
[Collection(nameof(MetricsCollection))]
public sealed class ImportMovieFolderCommandHandlerTests
{
    private const string MeterName = "LamuFlix";
    private const string ImportInstrumentName = "lamuflix.import.count";

    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IEnrichmentOutbox outbox = Substitute.For<IEnrichmentOutbox>();
    private readonly ImportMovieFolderCommandHandler handler;
    private readonly Fixture fixture = new();

    public ImportMovieFolderCommandHandlerTests()
    {
        handler = new ImportMovieFolderCommandHandler(scanner, movies, outbox);
    }

    [Fact]
    public async Task HandleAsync_HappyPath_ScanCreateAddStageSave()
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
            outbox.Stage(Arg.Is<EnrichmentRequested>(message => message.MovieId == id && message.Attempt == 1));
            movies.SaveChangesAsync(ct);
        });
        outbox.Received(1).Stage(Arg.Is<EnrichmentRequested>(message => message.MovieId == id && message.Attempt == 1));
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
    public async Task HandleAsync_AddThrows_DoesNotSaveOrStage()
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
        outbox.DidNotReceive().Stage(Arg.Any<EnrichmentRequested>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_StageThrows_DoesNotSave()
    {
        // arrange
        var folder = new LibraryPath("C:/library/incoming");
        var id = NewId();
        var scanned = new ScannedMovie(new LibraryPath("C:/library/incoming/file"), "Imported", new MediaFormat("mkv"), null, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(folder).Returns(scanned);
        movies.NextIdentityAsync(ct).Returns(id);
        outbox.When(stage => stage.Stage(Arg.Any<EnrichmentRequested>()))
            .Throw(new InvalidOperationException("stage"));
        using var capture = new ImportCountCapture();

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        outbox.Received(1).Stage(Arg.Is<EnrichmentRequested>(message => message.MovieId == id && message.Attempt == 1));
        await movies.Received(1).AddAsync(Arg.Any<Movie>(), ct);
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_SaveThrows_PropagatesAndRecordsNoImport()
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
        outbox.Received(1).Stage(Arg.Any<EnrichmentRequested>());
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_SaveCancelled_Propagates()
    {
        // arrange
        var folder = new LibraryPath("C:/library/incoming");
        var id = NewId();
        var scanned = new ScannedMovie(new LibraryPath("C:/library/incoming/file"), "Imported", new MediaFormat("mkv"), null, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(folder).Returns(scanned);
        movies.NextIdentityAsync(ct).Returns(id);
        movies.SaveChangesAsync(ct).ThrowsAsync(new OperationCanceledException("cancelled"));
        using var capture = new ImportCountCapture();

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        outbox.Received(1).Stage(Arg.Any<EnrichmentRequested>());
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
        outbox.DidNotReceive().Stage(Arg.Any<EnrichmentRequested>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        capture.Measurements.ShouldBeEmpty();
    }

    private MovieId NewId() => new(Math.Abs(fixture.Create<int>()) + 1);

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
