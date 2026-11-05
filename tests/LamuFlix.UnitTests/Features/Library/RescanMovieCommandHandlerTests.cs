using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Library;

public sealed class RescanMovieCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly RescanMovieCommandHandler handler;
    private readonly Fixture fixture = new();

    public RescanMovieCommandHandlerTests()
    {
        handler = new RescanMovieCommandHandler(movies, scanner);
    }

    [Fact]
    public async Task HandleAsync_ReplacedExtension_ReportsChangedFormatsAndSavesOnce()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        var movie = MovieAt(id, "avi");
        movies.GetAsync(id, ct).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path).Returns(Primary(movie.Path, "mkv"));

        // act
        var result = await handler.HandleAsync(new RescanMovieCommand(id), ct);

        // assert
        result.MovieId.ShouldBe(id);
        result.Changed.ShouldBeTrue();
        result.OldFormat.ShouldBe(new MediaFormat("avi"));
        result.NewFormat.ShouldBe(new MediaFormat("mkv"));
        movie.Format.ShouldBe(new MediaFormat("mkv"));
        await movies.Received(1).SaveChangesAsync(ct);
    }

    [Fact]
    public async Task HandleAsync_ReplacedExtension_PreservesIdentityTitlePathYearAndMetadata()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        var metadata = new MovieMetadata(
            "Heat", "A crew of thieves.", new ReleaseYear(1995, Now), new Runtime(170), new ImdbRating(8.3m));
        var movie = Movie.Rehydrate(
            id,
            "Heat",
            new LibraryPath("C:/library/Heat"),
            new MediaFormat("avi"),
            isInWatchlist: true,
            metadata: metadata,
            status: EnrichmentStatus.Enriched,
            enrichedAt: Now,
            enrichmentAttempts: 2,
            lastFailureCategory: null,
            lastAttemptAt: Now,
            parsedReleaseYear: new ReleaseYear(1995, Now),
            isWatched: true,
            isNotInterested: true);
        movies.GetAsync(id, ct).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path).Returns(Primary(movie.Path, "mkv"));

        // act
        await handler.HandleAsync(new RescanMovieCommand(id), ct);

        // assert
        movie.Id.ShouldBe(id);
        movie.Title.ShouldBe("Heat");
        movie.Path.ShouldBe(new LibraryPath("C:/library/Heat"));
        movie.ParsedReleaseYear.ShouldBe(new ReleaseYear(1995, Now));
        movie.Metadata.ShouldBe(metadata);
        movie.Status.ShouldBe(EnrichmentStatus.Enriched);
        movie.EnrichedAt.ShouldBe(Now);
        movie.EnrichmentAttempts.ShouldBe(2);
        movie.IsInWatchlist.ShouldBeTrue();
        movie.IsWatched.ShouldBeTrue();
        movie.IsNotInterested.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_SameExtensionReplacement_ReportsUnchangedWithoutSave()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        var movie = MovieAt(id, "mkv");
        movies.GetAsync(id, ct).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path).Returns(Primary(movie.Path, "mkv"));

        // act
        var result = await handler.HandleAsync(new RescanMovieCommand(id), ct);

        // assert
        result.Changed.ShouldBeFalse();
        result.OldFormat.ShouldBe(new MediaFormat("mkv"));
        result.NewFormat.ShouldBe(new MediaFormat("mkv"));
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownMovie_ThrowsNotFoundWithoutResolveOrSave()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        movies.GetAsync(id, ct).Returns((Movie?)null);

        // act
        await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(new RescanMovieCommand(id), ct));

        // assert
        scanner.DidNotReceive().ResolvePrimaryVideo(Arg.Any<LibraryPath>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MissingFolder_ThrowsNotFoundWithoutSaveOrMutation()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        var movie = MovieAt(id, "avi");
        movies.GetAsync(id, ct).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path).Throws(new NotFoundException());

        // act
        await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(new RescanMovieCommand(id), ct));

        // assert
        movie.Format.ShouldBe(new MediaFormat("avi"));
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoEligibleVideo_ThrowsInvalidTransitionWithoutSaveOrMutation()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        var movie = MovieAt(id, "avi");
        movies.GetAsync(id, ct).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path)
            .Throws(new InvalidTransitionException("Library folder contains no eligible top-level video file."));

        // act
        await Should.ThrowAsync<InvalidTransitionException>(
            () => handler.HandleAsync(new RescanMovieCommand(id), ct));

        // assert
        movie.Format.ShouldBe(new MediaFormat("avi"));
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CancelledBeforeResolve_ThrowsWithoutResolveOrSave()
    {
        // arrange
        var id = NewId();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var ct = cancellation.Token;
        var movie = MovieAt(id, "avi");
        movies.GetAsync(id, ct).Returns(movie);

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new RescanMovieCommand(id), ct));

        // assert
        scanner.DidNotReceive().ResolvePrimaryVideo(Arg.Any<LibraryPath>());
        movie.Format.ShouldBe(new MediaFormat("avi"));
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CancelledBeforeSave_ThrowsWithoutSave()
    {
        // arrange
        var id = NewId();
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        var movie = MovieAt(id, "avi");
        movies.GetAsync(id, ct).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path).Returns(_ =>
        {
            // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
            cancellation.Cancel();
            return Primary(movie.Path, "mkv");
        });

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new RescanMovieCommand(id), ct));

        // assert
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SaveInFlightCancelled_PropagatesCancellation()
    {
        // arrange
        var id = NewId();
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        var movie = MovieAt(id, "avi");
        movies.GetAsync(id, ct).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path).Returns(Primary(movie.Path, "mkv"));
        movies.SaveChangesAsync(ct).Returns(Task.FromException(new OperationCanceledException(ct)));

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new RescanMovieCommand(id), ct));

        // assert
        await movies.Received(1).SaveChangesAsync(ct);
    }

    private static PrimaryVideo Primary(LibraryPath folder, string extension) =>
        new(new LibraryPath($"{folder.Value}/primary.{extension}"), new MediaFormat(extension));

    private static Movie MovieAt(MovieId id, string extension) =>
        Movie.Create(id, "Heat", new LibraryPath("C:/library/Heat"), new MediaFormat(extension));

    private MovieId NewId() => new(Math.Abs(fixture.Create<int>()) + 1);
}
