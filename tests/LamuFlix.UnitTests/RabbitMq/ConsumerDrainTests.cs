using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.RabbitMq;
using NSubstitute;
using RabbitMQ.Client;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class ConsumerDrainTests
{
    [Fact]
    public async Task PauseAndDrainAsync_OpenChannel_CancelsBeforeInFlightFinishes()
    {
        // arrange
        var channel = Substitute.For<IChannel>();
        channel.IsOpen.Returns(true);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        channel.When(static call => call.BasicCancelAsync("ctag", false, Arg.Any<CancellationToken>()))
            .Do(_ => cancelled.TrySetResult());
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var drain = new ConsumerDrain();
        _ = drain.Track(() => release.Task);

        // act
        var pausing = drain.PauseAndDrainAsync(channel, "ctag", CancellationToken.None);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert
        pausing.IsCompleted.ShouldBeFalse();
        release.SetResult();
        await pausing.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await channel.Received(1).BasicCancelAsync("ctag", false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PauseAndDrainAsync_ClosedChannel_WaitsWithoutCancelling()
    {
        // arrange
        var channel = Substitute.For<IChannel>();
        channel.IsOpen.Returns(false);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var drain = new ConsumerDrain();
        _ = drain.Track(() => release.Task);

        // act
        var pausing = drain.PauseAndDrainAsync(channel, "ctag", CancellationToken.None);
        await Task.Delay(TimeSpan.FromMilliseconds(50), TestContext.Current.CancellationToken);

        // assert
        pausing.IsCompleted.ShouldBeFalse();
        release.SetResult();
        await pausing.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await channel.DidNotReceive().BasicCancelAsync(
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PauseAndDrainAsync_ShutdownAlreadyRequested_CancelsThenStopsWaiting()
    {
        // arrange
        var channel = Substitute.For<IChannel>();
        channel.IsOpen.Returns(true);
        using var shutdown = new CancellationTokenSource();
        await shutdown.CancelAsync();
        var drain = new ConsumerDrain();
        _ = drain.Track(() => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task);

        // act
        var pausing = drain.PauseAndDrainAsync(channel, "ctag", shutdown.Token);
        var finished = await Task.WhenAny(pausing, Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        // assert
        finished.ShouldBe(pausing);
        await Should.ThrowAsync<OperationCanceledException>(async () => await pausing);
        await channel.Received(1).BasicCancelAsync("ctag", false, shutdown.Token);
    }

    [Fact]
    public async Task Track_WorkStillRunningSynchronously_IsAlreadyInFlight()
    {
        // arrange
        var drain = new ConsumerDrain();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? idleWhileStarting = null;

        // act
        var tracked = drain.Track(() =>
        {
            idleWhileStarting = drain.WhenIdleAsync(CancellationToken.None);
            return release.Task;
        });

        // assert
        idleWhileStarting.ShouldNotBeNull().IsCompleted.ShouldBeFalse();
        release.SetResult();
        await tracked.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await idleWhileStarting.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Track_WorkThatThrowsWhileStarting_LeavesTheDrainIdle()
    {
        // arrange
        var drain = new ConsumerDrain();

        // act
        var tracked = drain.Track(static () => throw new InvalidOperationException("start failed"));

        // assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await tracked);
        drain.WhenIdleAsync(CancellationToken.None).IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public void WhenIdleAsync_NothingTracked_IsAlreadyComplete()
    {
        // arrange
        var drain = new ConsumerDrain();

        // act
        var idle = drain.WhenIdleAsync(CancellationToken.None);

        // assert
        idle.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public void Track_NoWork_Throws()
    {
        // arrange
        var drain = new ConsumerDrain();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null contract test.
        var tracking = () => drain.Track(null!);

        // assert
        Should.Throw<ArgumentNullException>(tracking);
    }

    [Fact]
    public async Task PauseAndDrainAsync_NoChannel_Throws()
    {
        // arrange
        var drain = new ConsumerDrain();

        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed - intentional null contract test.
        var pausing = () => drain.PauseAndDrainAsync(null!, "ctag", CancellationToken.None);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(pausing);
    }
}
