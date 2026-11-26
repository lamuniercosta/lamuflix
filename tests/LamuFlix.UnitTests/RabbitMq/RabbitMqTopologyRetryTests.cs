using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.RabbitMq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class RabbitMqTopologyRetryTests
{
    private static readonly TimeSpan[] TestDelays =
    [
        TimeSpan.FromMilliseconds(1),
        TimeSpan.FromMilliseconds(1),
    ];

    [Fact]
    public void IsClusterNotFormed_ReplyCode541_ReturnsTrue()
    {
        // arrange
        var failure = new InvalidOperationException("PRECONDITION_FAILED - reply code 541 (CLUSTER_NOT_FORMED)");

        // act
        var result = RabbitMqTopology.IsClusterNotFormed(failure);

        // assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsClusterNotFormed_SymbolicName_ReturnsTrue()
    {
        // arrange
        var failure = new InvalidOperationException("unexpected reply cluster_not_formed while declaring quorum queue");

        // act
        var result = RabbitMqTopology.IsClusterNotFormed(failure);

        // assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsClusterNotFormed_WrappedInInnerException_ReturnsTrue()
    {
        // arrange
        var failure = new InvalidOperationException(
            "channel error",
            new InvalidOperationException("541 cluster_not_formed"));

        // act
        var result = RabbitMqTopology.IsClusterNotFormed(failure);

        // assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsClusterNotFormed_UnrelatedFailure_ReturnsFalse()
    {
        // arrange
        var failure = new InvalidOperationException("PRECONDITION_FAILED - inequivalent arg 'x-queue-type'");

        // act
        var result = RabbitMqTopology.IsClusterNotFormed(failure);

        // assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteWithClusterRetryAsync_SucceedsFirstAttempt_RunsOnce()
    {
        // arrange
        var calls = 0;

        // act
        await RabbitMqTopology.ExecuteWithClusterRetryAsync(
            _ =>
            {
                calls++;
                return Task.CompletedTask;
            },
            CancellationToken.None,
            TestDelays);

        // assert
        calls.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteWithClusterRetryAsync_ClusterNotFormedThenReady_RetriesUntilSuccess()
    {
        // arrange
        var calls = 0;

        // act
        await RabbitMqTopology.ExecuteWithClusterRetryAsync(
            _ =>
            {
                calls++;
                return calls < 3
                    ? Task.FromException(new InvalidOperationException("541 cluster_not_formed"))
                    : Task.CompletedTask;
            },
            CancellationToken.None,
            TestDelays);

        // assert
        calls.ShouldBe(3);
    }

    [Fact]
    public async Task ExecuteWithClusterRetryAsync_UnrelatedFailure_ThrowsWithoutRetrying()
    {
        // arrange
        var calls = 0;

        // act
        var failure = await Should.ThrowAsync<InvalidOperationException>(() =>
            RabbitMqTopology.ExecuteWithClusterRetryAsync(
                _ =>
                {
                    calls++;
                    return Task.FromException(new InvalidOperationException("inequivalent arg 'x-queue-type'"));
                },
                CancellationToken.None,
                TestDelays));

        // assert
        failure.Message.ShouldContain("inequivalent arg");
        calls.ShouldBe(1);
    }

    [Fact]
    public void IsNotFound_QueueMissing404_ReturnsTrue()
    {
        // arrange
        var failure = NotFoundFailure();

        // act
        var result = RabbitMqTopology.IsNotFound(failure);

        // assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsNotFound_AlreadyClosedCarrying404_ReturnsTrue()
    {
        // arrange
        var failure = new AlreadyClosedException(
            new ShutdownEventArgs(ShutdownInitiator.Peer, 404, "NOT_FOUND - no queue 'enrichment.requested'"));

        // act
        var result = RabbitMqTopology.IsNotFound(failure);

        // assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsNotFound_WrappedInInnerException_ReturnsTrue()
    {
        // arrange
        var failure = new InvalidOperationException("channel error", NotFoundFailure());

        // act
        var result = RabbitMqTopology.IsNotFound(failure);

        // assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsNotFound_Non404ReplyCode_ReturnsFalse()
    {
        // arrange
        var failure = new OperationInterruptedException(
            new ShutdownEventArgs(ShutdownInitiator.Peer, 406, "PRECONDITION_FAILED - inequivalent arg 'x-queue-type'"));

        // act
        var result = RabbitMqTopology.IsNotFound(failure);

        // assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsNotFound_UnrelatedFailure_ReturnsFalse()
    {
        // arrange
        var failure = new InvalidOperationException("inequivalent arg 'x-queue-type'");

        // act
        var result = RabbitMqTopology.IsNotFound(failure);

        // assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteWithClusterRetryAsync_ClusterNeverForms_ThrowsAfterExhaustingDelays()
    {
        // arrange
        var calls = 0;

        // act
        await Should.ThrowAsync<InvalidOperationException>(() =>
            RabbitMqTopology.ExecuteWithClusterRetryAsync(
                _ =>
                {
                    calls++;
                    return Task.FromException(new InvalidOperationException("541 cluster_not_formed"));
                },
                CancellationToken.None,
                TestDelays));

        // assert
        calls.ShouldBe(TestDelays.Length + 1);
    }

    private static OperationInterruptedException NotFoundFailure() =>
        new(new ShutdownEventArgs(ShutdownInitiator.Peer, 404, "NOT_FOUND - no queue 'enrichment.requested'"));
}
