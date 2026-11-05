using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LamuFlix.UnitTests.Features.Library;

public sealed class RescanLibraryCommandHandlerTests
{
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly RescanLibraryCommandHandler handler;

    public RescanLibraryCommandHandlerTests()
    {
        handler = new RescanLibraryCommandHandler(movies, scanner);
    }

    [Fact]
    public async Task HandleAsync_EmptySnapshot_ReportsZeroCountsAndNoSave()
    {
        // arrange
        var ct = CancellationToken.None;
        movies.SnapshotMovieIdsAsync(ct).Returns(Ids());

        // act
        var result = await handler.HandleAsync(new RescanLibraryCommand(), ct);

        // assert
        result.Changed.ShouldBe(0);
        result.Unchanged.ShouldBe(0);
        result.Failed.ShouldBe(0);
        result.FailedMovieIds.ShouldBeEmpty();
        scanner.DidNotReceive().ResolvePrimaryVideo(Arg.Any<LibraryPath>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MixedLibrary_CountsEverySnapshotIdExactlyOnceAndSavesOnlyChangedItems()
    {
        // arrange
        var ct = CancellationToken.None;
        var changed = MovieAt(1, "avi");
        var unchanged = MovieAt(2, "mkv");
        var missingFolder = MovieAt(3, "avi");
        var noEligibleVideo = MovieAt(4, "avi");
        movies.SnapshotMovieIdsAsync(ct).Returns(Ids(1, 2, 3, 4, 5));
        Track(movies, changed, unchanged, missingFolder, noEligibleVideo);
        scanner.ResolvePrimaryVideo(changed.Path).Returns(Primary("mkv"));
        scanner.ResolvePrimaryVideo(unchanged.Path).Returns(Primary("mkv"));
        scanner.ResolvePrimaryVideo(missingFolder.Path).Throws(new NotFoundException());
        scanner.ResolvePrimaryVideo(noEligibleVideo.Path)
            .Throws(new InvalidTransitionException("Library folder contains no eligible top-level video file."));

        // act
        var result = await handler.HandleAsync(new RescanLibraryCommand(), ct);

        // assert
        result.Changed.ShouldBe(1);
        result.Unchanged.ShouldBe(1);
        result.Failed.ShouldBe(3);
        result.FailedMovieIds.Select(id => id.Value).ShouldBe([3, 4, 5]);
        changed.Format.ShouldBe(new MediaFormat("mkv"));
        unchanged.Format.ShouldBe(new MediaFormat("mkv"));
        missingFolder.Format.ShouldBe(new MediaFormat("avi"));
        noEligibleVideo.Format.ShouldBe(new MediaFormat("avi"));
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnchangedSnapshot_ReportsUnchangedAndWritesNothing()
    {
        // arrange
        var ct = CancellationToken.None;
        var first = MovieAt(1, "mkv");
        var second = MovieAt(2, "mp4");
        movies.SnapshotMovieIdsAsync(ct).Returns(Ids(1, 2));
        Track(movies, first, second);
        scanner.ResolvePrimaryVideo(first.Path).Returns(Primary("mkv"));
        scanner.ResolvePrimaryVideo(second.Path).Returns(Primary("mp4"));

        // act
        var result = await handler.HandleAsync(new RescanLibraryCommand(), ct);

        // assert
        result.Changed.ShouldBe(0);
        result.Unchanged.ShouldBe(2);
        result.Failed.ShouldBe(0);
        result.FailedMovieIds.ShouldBeEmpty();
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SnapshottedRowDeletedBeforeLoad_CountsFailedAndContinuesWithLaterItems()
    {
        // arrange
        var ct = CancellationToken.None;
        var survivor = MovieAt(2, "avi");
        movies.SnapshotMovieIdsAsync(ct).Returns(Ids(1, 2, 3));
        Track(movies, survivor);
        scanner.ResolvePrimaryVideo(survivor.Path).Returns(Primary("mkv"));

        // act
        var result = await handler.HandleAsync(new RescanLibraryCommand(), ct);

        // assert
        result.Changed.ShouldBe(1);
        result.Unchanged.ShouldBe(0);
        result.Failed.ShouldBe(2);
        result.FailedMovieIds.Select(id => id.Value).ShouldBe([1, 3]);
        survivor.Format.ShouldBe(new MediaFormat("mkv"));
    }

    [Fact]
    public async Task HandleAsync_UnexpectedCandidateFailure_AbortsWithPriorCommitsKeptAndIsNotCountedFailed()
    {
        // arrange
        var ct = CancellationToken.None;
        var changed = MovieAt(1, "avi");
        var unreadable = MovieAt(2, "avi");
        var later = MovieAt(3, "avi");
        movies.SnapshotMovieIdsAsync(ct).Returns(Ids(1, 2, 3));
        Track(movies, changed, unreadable, later);
        scanner.ResolvePrimaryVideo(changed.Path).Returns(Primary("mkv"));
        scanner.ResolvePrimaryVideo(unreadable.Path)
            .Throws(new UnauthorizedAccessException("candidate is unreadable"));

        // act
        var thrown = await Should.ThrowAsync<UnauthorizedAccessException>(
            () => handler.HandleAsync(new RescanLibraryCommand(), ct));

        // assert
        thrown.Message.ShouldBe("candidate is unreadable");
        changed.Format.ShouldBe(new MediaFormat("mkv"));
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        scanner.DidNotReceive().ResolvePrimaryVideo(later.Path);
        later.Format.ShouldBe(new MediaFormat("avi"));
    }

    [Fact]
    public async Task HandleAsync_SaveFailureOnLaterItem_AbortsWithoutProcessingTheRemainder()
    {
        // arrange
        var ct = CancellationToken.None;
        var saved = MovieAt(1, "avi");
        var failing = MovieAt(2, "avi");
        var remainder = MovieAt(3, "avi");
        movies.SnapshotMovieIdsAsync(ct).Returns(Ids(1, 2, 3));
        Track(movies, saved, failing, remainder);
        scanner.ResolvePrimaryVideo(saved.Path).Returns(Primary("mkv"));
        scanner.ResolvePrimaryVideo(failing.Path).Returns(Primary("mkv"));
        movies.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.CompletedTask, _ => Task.FromException(new InvalidOperationException("database is gone")));

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new RescanLibraryCommand(), ct));

        // assert
        await movies.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
        scanner.DidNotReceive().ResolvePrimaryVideo(remainder.Path);
    }

    [Fact]
    public async Task HandleAsync_CancelledDuringRun_PropagatesWithoutSummaryOrFailedCount()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        var first = MovieAt(1, "avi");
        var second = MovieAt(2, "avi");
        movies.SnapshotMovieIdsAsync(ct).Returns(Ids(1, 2));
        Track(movies, first, second);
        scanner.ResolvePrimaryVideo(first.Path).Returns(_ =>
        {
            // ReSharper disable once AccessToDisposedClosure // lambda runs only within the awaited call inside the using scope
            cancellation.Cancel();
            return Primary("avi");
        });

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new RescanLibraryCommand(), ct));

        // assert
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        scanner.DidNotReceive().ResolvePrimaryVideo(second.Path);
    }

    [Fact]
    public async Task HandleAsync_CancelledBeforeSnapshot_ThrowsWithoutReadingOrSaving()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var ct = cancellation.Token;

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new RescanLibraryCommand(), ct));

        // assert
        await movies.DidNotReceive().SnapshotMovieIdsAsync(Arg.Any<CancellationToken>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AscendingSnapshot_LoadsEachIdOnceInAscendingOrder()
    {
        // arrange
        var ct = CancellationToken.None;
        var visited = new List<int>();
        movies.SnapshotMovieIdsAsync(ct).Returns(Ids(1, 2, 3));
        movies.GetAsync(Arg.Any<MovieId>(), ct).Returns(call =>
        {
            var id = call.Arg<MovieId>();
            visited.Add(id.Value);
            return MovieAt(id.Value, "mkv");
        });
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(Primary("mkv"));

        // act
        var result = await handler.HandleAsync(new RescanLibraryCommand(), ct);

        // assert
        visited.ShouldBe([1, 2, 3]);
        result.Unchanged.ShouldBe(3);
        await movies.Received(3).GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
    }

    private static void Track(IMovieRepository repository, params Movie[] tracked)
    {
        foreach (var movie in tracked)
        {
            repository.GetAsync(movie.Id, Arg.Any<CancellationToken>()).Returns(movie);
        }
    }

    private static IReadOnlyList<MovieId> Ids(params int[] values) => [.. values.Select(value => new MovieId(value))];

    private static PrimaryVideo Primary(string extension) =>
        new(new LibraryPath($"C:/library/Heat.{extension}"), new MediaFormat(extension));

    private static Movie MovieAt(int id, string extension) =>
        Movie.Create(new MovieId(id), "Heat", new LibraryPath($"C:/library/Heat{id}"), new MediaFormat(extension));
}
