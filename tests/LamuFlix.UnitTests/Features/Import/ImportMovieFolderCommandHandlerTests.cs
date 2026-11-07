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
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.UnitTests.Pipeline;
using Microsoft.Extensions.Options;
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
    private const string Root = "C:/library";

    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IEnrichmentOutbox outbox = Substitute.For<IEnrichmentOutbox>();
    private readonly ImportMovieFolderCommandHandler handler;
    private readonly Fixture fixture = new();

    public ImportMovieFolderCommandHandlerTests()
    {
        handler = new ImportMovieFolderCommandHandler(scanner, movies, outbox, Library());
    }

    [Fact]
    public async Task HandleAsync_HappyPath_ScanCreateAddStageSave()
    {
        // arrange
        var folder = new LibraryPath("incoming");
        var id = NewId();
        var scanned = new ScannedMovie(new LibraryPath("incoming"), "Imported", new MediaFormat("mkv"), null, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(new LibraryPath("incoming")).Returns(scanned);
        movies.NextIdentityAsync(ct).Returns(id);
        using var capture = new ImportCountCapture();

        // act
        var result = await handler.HandleAsync(new ImportMovieFolderCommand(folder), ct);

        // assert
        result.ShouldBe(id);
        Received.InOrder(() =>
        {
            scanner.Scan(new LibraryPath("incoming"));
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
        var folder = new LibraryPath("incoming");
        var id = NewId();
        var year = new ReleaseYear(2012, new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.Zero));
        var scanned = new ScannedMovie(new LibraryPath("incoming"), "2 Days in New York", new MediaFormat("mkv"), year, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(Arg.Any<LibraryPath>()).Returns(scanned);
        movies.NextIdentityAsync(ct).Returns(id);

        // act
        await handler.HandleAsync(new ImportMovieFolderCommand(folder), ct);

        // assert
        await movies.Received(1).AddAsync(
            Arg.Is<Movie>(movie => movie.ParsedReleaseYear == year && movie.Title == "2 Days in New York"),
            ct);
    }

    [Theory]
    [InlineData("Heat (1995)")]
    [InlineData("Collections/Heat (1995)")]
    [InlineData("./Heat (1995)/")]
    [InlineData("Heat (1995)\\")]
    public async Task HandleAsync_RelativeFolder_CanonicalizesBeforeScanningAndPersisting(string supplied)
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        var canonical = new LibraryPath(supplied);
        scanner.Scan(canonical).Returns(new ScannedMovie(canonical, "Heat", new MediaFormat("mkv"), null, 1L));
        movies.NextIdentityAsync(ct).Returns(id);

        // act
        await handler.HandleAsync(new ImportMovieFolderCommand(new LibraryPath(supplied)), ct);

        // assert
        scanner.Received(1).Scan(canonical);
        await movies.Received(1).AddAsync(
            Arg.Is<Movie>(movie => movie.Path == canonical && movie.Title == "Heat"),
            ct);
    }

    [Theory]
    [InlineData("C:/library/Heat (1995)")]
    [InlineData("C:\\library\\Heat (1995)")]
    [InlineData("C:/library/./Heat (1995)/")]
    public async Task HandleAsync_AbsolutePathUnderActiveRoot_CanonicalizesToRelativeIdentity(string supplied)
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        var canonical = new LibraryPath("Heat (1995)");
        scanner.Scan(canonical).Returns(new ScannedMovie(canonical, "Heat", new MediaFormat("mkv"), null, 1L));
        movies.NextIdentityAsync(ct).Returns(id);

        // act
        await handler.HandleAsync(new ImportMovieFolderCommand(new LibraryPath(supplied)), ct);

        // assert
        scanner.Received(1).Scan(canonical);
        await movies.Received(1).AddAsync(
            Arg.Is<Movie>(movie => movie.Path == canonical), ct);
    }

    [Fact]
    public async Task HandleAsync_AbsoluteActiveRootItself_CanonicalizesToRootIdentity()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        var canonical = new LibraryPath(".");
        scanner.Scan(canonical).Returns(new ScannedMovie(canonical, "Root", new MediaFormat("mkv"), null, 1L));
        movies.NextIdentityAsync(ct).Returns(id);

        // act
        await handler.HandleAsync(new ImportMovieFolderCommand(new LibraryPath("C:/library")), ct);

        // assert
        scanner.Received(1).Scan(canonical);
    }

    [Theory]
    [InlineData("D:/elsewhere/Heat (1995)")]
    [InlineData("/mnt/elsewhere/Heat")]
    public async Task HandleAsync_AbsolutePathOutsideActiveRoot_RejectsWithoutScanningOrPersisting(string supplied)
    {
        // arrange
        var ct = CancellationToken.None;
        using var capture = new ImportCountCapture();

        // act
        await Should.ThrowAsync<ValidationException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(new LibraryPath(supplied)), ct));

        // assert
        scanner.DidNotReceive().Scan(Arg.Any<LibraryPath>());
        await movies.DidNotReceive().AddAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        outbox.DidNotReceive().Stage(Arg.Any<EnrichmentRequested>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        capture.Measurements.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("D:/legacy/Heat (1995)", "D:/legacy")]
    [InlineData("D:\\legacy\\Heat (1995)", "D:\\legacy\\")]
    public async Task HandleAsync_LegacyAliasPath_RejectsBecauseAliasesNeverAuthorizeNewImports(string supplied, string alias)
    {
        // arrange
        using var capture = new ImportCountCapture();
        var aliased = new ImportMovieFolderCommandHandler(
            scanner,
            movies,
            outbox,
            Library(alias));

        // act
        await Should.ThrowAsync<ValidationException>(
            () => aliased.HandleAsync(new ImportMovieFolderCommand(new LibraryPath(supplied)), CancellationToken.None));

        // assert
        scanner.DidNotReceive().Scan(Arg.Any<LibraryPath>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_AddThrows_DoesNotSaveOrStage()
    {
        // arrange
        var folder = new LibraryPath("incoming");
        var id = NewId();
        var scanned = new ScannedMovie(new LibraryPath("incoming"), "Imported", new MediaFormat("mkv"), null, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(Arg.Any<LibraryPath>()).Returns(scanned);
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
        var folder = new LibraryPath("incoming");
        var id = NewId();
        var scanned = new ScannedMovie(new LibraryPath("incoming"), "Imported", new MediaFormat("mkv"), null, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(Arg.Any<LibraryPath>()).Returns(scanned);
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
        var folder = new LibraryPath("incoming");
        var id = NewId();
        var scanned = new ScannedMovie(new LibraryPath("incoming"), "Imported", new MediaFormat("mkv"), null, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(Arg.Any<LibraryPath>()).Returns(scanned);
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
        var folder = new LibraryPath("incoming");
        var id = NewId();
        var scanned = new ScannedMovie(new LibraryPath("incoming"), "Imported", new MediaFormat("mkv"), null, 1024L);
        var ct = CancellationToken.None;
        scanner.Scan(Arg.Any<LibraryPath>()).Returns(scanned);
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
    public async Task HandleAsync_ScanThrowsUnexpected_RecordsNoImportMeasurement()
    {
        // arrange
        var folder = new LibraryPath("incoming");
        var ct = CancellationToken.None;
        scanner.Scan(Arg.Any<LibraryPath>()).Throws(new UnauthorizedAccessException("candidate is unreadable"));
        using var capture = new ImportCountCapture();

        // act
        await Should.ThrowAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        await movies.DidNotReceive().AddAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        outbox.DidNotReceive().Stage(Arg.Any<EnrichmentRequested>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ScanThrowsNotFound_PropagatesWithoutImporting()
    {
        // arrange
        var folder = new LibraryPath("incoming");
        var ct = CancellationToken.None;
        scanner.Scan(Arg.Any<LibraryPath>()).Throws(new NotFoundException());
        using var capture = new ImportCountCapture();

        // act
        await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        await movies.DidNotReceive().AddAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        outbox.DidNotReceive().Stage(Arg.Any<EnrichmentRequested>());
        capture.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ScanThrowsInvalidTransition_PropagatesWithoutImporting()
    {
        // arrange
        var folder = new LibraryPath("incoming");
        var ct = CancellationToken.None;
        scanner.Scan(Arg.Any<LibraryPath>()).Throws(new InvalidTransitionException("no eligible video"));
        using var capture = new ImportCountCapture();

        // act
        await Should.ThrowAsync<InvalidTransitionException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        await movies.DidNotReceive().AddAsync(Arg.Any<Movie>(), Arg.Any<CancellationToken>());
        outbox.DidNotReceive().Stage(Arg.Any<EnrichmentRequested>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        capture.Measurements.ShouldBeEmpty();
    }

    private static IOptions<LibraryOptions> Library(params string[] legacyRoots) =>
        Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = Root, LegacyRootPaths = legacyRoots });

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
