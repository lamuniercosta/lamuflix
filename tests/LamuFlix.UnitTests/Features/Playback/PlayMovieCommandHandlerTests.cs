using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Playback;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using NSubstitute;

namespace LamuFlix.UnitTests.Features.Playback;

public sealed class PlayMovieCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 8, 15, 30, TimeSpan.Zero);

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IMediaPlayerLauncher launcher = Substitute.For<IMediaPlayerLauncher>();
    private readonly IPlaybackHistory history = Substitute.For<IPlaybackHistory>();
    private readonly TimeProvider clock = Substitute.For<TimeProvider>();
    private readonly PlayMovieCommandHandler handler;
    private readonly Fixture fixture = new();

    public PlayMovieCommandHandlerTests()
    {
        clock.GetUtcNow().Returns(Now);
        handler = new PlayMovieCommandHandler(catalog, launcher, history, clock);
    }

    [Fact]
    public async Task HandleAsync_DetailsFound_LaunchesOnceWithPathAndFormat()
    {
        // arrange
        var id = NewId();
        var path = new LibraryPath("C:/library/play");
        var format = new MediaFormat("mp4");
        var details = new MovieDetails(id, "Play Me", path, format, null, false, false, false);
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns(details);

        // act
        var result = await handler.HandleAsync(new PlayMovieCommand(id), ct);

        // assert
        result.ShouldBe(Unit.Value);
        launcher.Received(1).Launch(path, format);
    }

    [Fact]
    public async Task HandleAsync_DetailsFound_AppendsResolvedMovieAtClockInstantWithCancellationToken()
    {
        // arrange
        var id = NewId();
        var details = Details(id);
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        catalog.GetDetailsAsync(id, ct).Returns(details);

        // act
        await handler.HandleAsync(new PlayMovieCommand(id), ct);

        // assert
        await history.Received(1).RecordAsync(Arg.Any<PlaybackEvent>(), ct);
        var recorded = history.ReceivedCalls()
            .Select(call => call.GetArguments()[0])
            .OfType<PlaybackEvent>()
            .ShouldHaveSingleItem();
        recorded.MovieId.ShouldBe(id);
        recorded.PlayedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task HandleAsync_DetailsFound_ResolvesThenLaunchesThenSamplesClockThenAppends()
    {
        // arrange
        var id = NewId();
        var details = Details(id);
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns(details);

        // act
        await handler.HandleAsync(new PlayMovieCommand(id), ct);

        // assert
        Received.InOrder(() =>
        {
            catalog.GetDetailsAsync(id, ct);
            launcher.Launch(details.Path, details.Format);
            clock.GetUtcNow();
            history.RecordAsync(Arg.Any<PlaybackEvent>(), ct);
        });
    }

    [Fact]
    public async Task HandleAsync_Success_DoesNotCompleteUntilAppendCompletes()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns(Details(id));
        var append = new TaskCompletionSource();
        history.RecordAsync(Arg.Any<PlaybackEvent>(), ct).Returns(append.Task);

        // act
        var handling = handler.HandleAsync(new PlayMovieCommand(id), ct);

        // assert
        handling.IsCompleted.ShouldBeFalse();
        append.SetResult();
        (await handling).ShouldBe(Unit.Value);
    }

    [Fact]
    public async Task HandleAsync_MissingMovie_ThrowsNotFoundWithoutLaunchOrAppend()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns((MovieDetails?)null);

        // act
        await Should.ThrowAsync<NotFoundException>(
            () => handler.HandleAsync(new PlayMovieCommand(id), ct));

        // assert
        launcher.DidNotReceive().Launch(Arg.Any<LibraryPath>(), Arg.Any<MediaFormat>());
        await history.DidNotReceive().RecordAsync(Arg.Any<PlaybackEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DisabledLaunch_ThrowsFeatureDisabledWithoutAppend()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns(Details(id));
        launcher.When(instance => instance.Launch(Arg.Any<LibraryPath>(), Arg.Any<MediaFormat>()))
            .Throw(new FeatureDisabledException("Local playback is disabled."));

        // act
        await Should.ThrowAsync<FeatureDisabledException>(
            () => handler.HandleAsync(new PlayMovieCommand(id), ct));

        // assert
        await history.DidNotReceive().RecordAsync(Arg.Any<PlaybackEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FailedLaunch_ThrowsWithoutAppend()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns(Details(id));
        launcher.When(instance => instance.Launch(Arg.Any<LibraryPath>(), Arg.Any<MediaFormat>()))
            .Throw(new InvalidOperationException("No configured player supports format."));

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new PlayMovieCommand(id), ct));

        // assert
        await history.DidNotReceive().RecordAsync(Arg.Any<PlaybackEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AppendFailure_PropagatesWithoutRetryOrSecondLaunch()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns(Details(id));
        history.RecordAsync(Arg.Any<PlaybackEvent>(), ct)
            .Returns(Task.FromException(new InvalidOperationException("append failed")));

        // act
        await Should.ThrowAsync<InvalidOperationException>(
            () => handler.HandleAsync(new PlayMovieCommand(id), ct));

        // assert
        launcher.Received(1).Launch(Arg.Any<LibraryPath>(), Arg.Any<MediaFormat>());
        await history.Received(1).RecordAsync(Arg.Any<PlaybackEvent>(), ct);
    }

    [Fact]
    public async Task HandleAsync_AppendCancellation_PropagatesWithoutRetryOrSecondLaunch()
    {
        // arrange
        var id = NewId();
        using var cancellation = new CancellationTokenSource();
        var ct = cancellation.Token;
        catalog.GetDetailsAsync(id, ct).Returns(Details(id));
        history.RecordAsync(Arg.Any<PlaybackEvent>(), ct)
            .Returns(Task.FromException(new OperationCanceledException(ct)));

        // act
        await Should.ThrowAsync<OperationCanceledException>(
            () => handler.HandleAsync(new PlayMovieCommand(id), ct));

        // assert
        launcher.Received(1).Launch(Arg.Any<LibraryPath>(), Arg.Any<MediaFormat>());
        await history.Received(1).RecordAsync(Arg.Any<PlaybackEvent>(), ct);
    }

    [Fact]
    public async Task HandleAsync_TwoSuccesses_AppendsTwice()
    {
        // arrange
        var id = NewId();
        var ct = CancellationToken.None;
        catalog.GetDetailsAsync(id, ct).Returns(Details(id));

        // act
        await handler.HandleAsync(new PlayMovieCommand(id), ct);
        await handler.HandleAsync(new PlayMovieCommand(id), ct);

        // assert
        await history.Received(2).RecordAsync(Arg.Any<PlaybackEvent>(), ct);
        launcher.Received(2).Launch(Arg.Any<LibraryPath>(), Arg.Any<MediaFormat>());
    }

    private static MovieDetails Details(MovieId id) =>
        new(id, "Play Me", new LibraryPath("C:/library/play"), new MediaFormat("mp4"), null, false, false, false);

    private MovieId NewId() => new(Math.Abs(fixture.Create<int>()) + 1);
}
