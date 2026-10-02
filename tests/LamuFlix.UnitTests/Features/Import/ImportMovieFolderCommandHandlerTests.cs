using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Ports;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Import;

public sealed class ImportMovieFolderCommandHandlerTests
{
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IEnrichmentQueue queue = Substitute.For<IEnrichmentQueue>();
    private readonly ImportMovieFolderCommandHandler handler;
    private readonly Fixture fixture = new();

    public ImportMovieFolderCommandHandlerTests()
    {
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

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        await movies.Received(1).SaveChangesAsync(ct);
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

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await queue.DidNotReceive().EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
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

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new ImportMovieFolderCommand(folder), ct));

        // assert
        await movies.Received(1).AddAsync(Arg.Any<Movie>(), ct);
        await queue.DidNotReceive().EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
    }

    private MovieId NewId() => new(Math.Abs(fixture.Create<int>()) + 1);
}
