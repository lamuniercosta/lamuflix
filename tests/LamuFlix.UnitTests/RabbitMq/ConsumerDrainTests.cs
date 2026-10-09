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
        _ = drain.Track(release.Task);

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
        _ = drain.Track(release.Task);

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
        _ = drain.Track(new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task);

        // act
        var pausing = drain.PauseAndDrainAsync(channel, "ctag", shutdown.Token);
        var finished = await Task.WhenAny(pausing, Task.Delay(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken));

        // assert
        finished.ShouldBe(pausing);
        await Should.ThrowAsync<OperationCanceledException>(async () => await pausing);
        await channel.Received(1).BasicCancelAsync("ctag", false, Arg.Any<CancellationToken>());
    }
}
