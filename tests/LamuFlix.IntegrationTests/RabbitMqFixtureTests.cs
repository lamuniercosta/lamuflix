using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(RabbitMqCollection))]
public sealed class RabbitMqFixtureTests(RabbitMqFixture fixture)
{
    private readonly RabbitMqProbe probe = new(fixture);

    [Fact]
    public async Task Startup_DeclaresTheExchangeAndTheThreeQueues()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        await probe.DeclarePassiveExchangeAsync(RabbitMqTopology.ExchangeName, cancellationToken);
        var declared = await DeclareNamesAsync(cancellationToken);
        var version = await ServerVersionAsync(cancellationToken);
        TestContext.Current.TestOutputHelper?.WriteLine($"[rabbitmq-server-version] {version}");
        TestContext.Current.TestOutputHelper?.WriteLine($"[rabbitmq-container-id] {fixture.Container.Id}");

        // assert
        declared.ShouldBe(Queues());
    }

    [Fact]
    public async Task ResetTopologyAsync_MessageOnTheRetryQueue_IsInNoQueueAfterwards()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetTopologyAsync(cancellationToken);
        await probe.PublishAsync(RabbitMqTopology.RetryRoutingKey, "reset-body"u8.ToArray(), cancellationToken);

        // act
        await fixture.ResetTopologyAsync(cancellationToken);
        var counts = await CountAllAsync(cancellationToken);

        // assert
        counts.ShouldAllBe(count => count == 0U);
    }

    [Fact]
    public async Task ResetTopologyAsync_Afterwards_PublishAndGetStillWork()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetTopologyAsync(cancellationToken);

        // act
        await probe.PublishAsync(RabbitMqTopology.RequestedRoutingKey, "after-reset"u8.ToArray(), cancellationToken);
        var message = await probe.PollGetMatchingAsync(RabbitMqTopology.RequestedQueue, _ => true, cancellationToken);

        // assert
        message.ShouldNotBeNull();
    }

    [Fact]
    public async Task DisposeAsync_WithoutInitialization_Completes()
    {
        // arrange
        var neverInitialized = new RabbitMqFixture();

        // act
        var act = () => neverInitialized.DisposeAsync().AsTask();

        // assert
        await Should.NotThrowAsync(act);
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_Completes()
    {
        // arrange
        var neverInitialized = new RabbitMqFixture();
        await neverInitialized.DisposeAsync();

        // act
        var act = () => neverInitialized.DisposeAsync().AsTask();

        // assert
        await Should.NotThrowAsync(act);
    }

    private async Task<List<string>> DeclareNamesAsync(CancellationToken cancellationToken)
    {
        var names = new List<string>();
        foreach (var queue in Queues())
        {
            names.Add((await probe.DeclarePassiveAsync(queue, cancellationToken)).QueueName);
        }

        return names;
    }

    private async Task<List<uint>> CountAllAsync(CancellationToken cancellationToken)
    {
        var counts = new List<uint>();
        foreach (var queue in Queues())
        {
            counts.Add((await probe.DeclarePassiveAsync(queue, cancellationToken)).MessageCount);
        }

        return counts;
    }

    private async Task<string> ServerVersionAsync(CancellationToken cancellationToken)
    {
        await using var connection = await fixture.CreateConnectionAsync(cancellationToken);
        var reported = connection.ServerProperties?["version"];
        if (reported is null)
        {
            throw new InvalidOperationException("The broker reported no server version.");
        }

        return reported is byte[] encoded ? Encoding.UTF8.GetString(encoded) : reported.ToString() ?? string.Empty;
    }

    private static IEnumerable<string> Queues() =>
    [
        RabbitMqTopology.RequestedQueue,
        RabbitMqTopology.RetryQueue,
        RabbitMqTopology.DeadLetterQueue,
    ];
}