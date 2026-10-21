using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(RabbitMqCollection))]
public sealed class RabbitMqTopologyTests(RabbitMqFixture fixture) : IAsyncLifetime
{
    private const int MaxAttempts = 3;
    private const int MaxDeclareAttempts = 4;

    private static readonly TimeSpan[] DeclareRetryDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
    ];

    private readonly RabbitMqProbe probe = new(fixture);

    public async ValueTask InitializeAsync() => await fixture.DeleteTopologyAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() => await fixture.ResetTopologyAsync(CancellationToken.None);

    [Fact]
    public async Task EnsureDeclaredAsync_DeclaresADurableDirectExchange()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);

        await Should.NotThrowAsync(() => probe.DeclarePassiveExchangeAsync(RabbitMqTopology.ExchangeName, ct));
    }

    [Fact]
    public async Task EnsureDeclaredAsync_ExchangeIsDirectAndDurable()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);

        await Should.NotThrowAsync(() => probe.DeclareExchangeAsync(
            RabbitMqTopology.ExchangeName, ExchangeType.Direct, durable: true, ct));
        await Should.ThrowAsync<OperationInterruptedException>(() => probe.DeclareExchangeAsync(
            RabbitMqTopology.ExchangeName, ExchangeType.Fanout, durable: true, ct));
        await Should.ThrowAsync<OperationInterruptedException>(() => probe.DeclareExchangeAsync(
            RabbitMqTopology.ExchangeName, ExchangeType.Direct, durable: false, ct));
    }

    [Fact]
    public async Task EnsureDeclaredAsync_DeclaresTheThreeQueues()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);

        foreach (var queue in Queues())
        {
            var declared = await probe.DeclarePassiveAsync(queue, ct);
            declared.QueueName.ShouldBe(queue);
        }
    }

    [Fact]
    public async Task EnsureDeclaredAsync_DeclaresDurableQueues()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);

        foreach (var (queue, arguments) in ArgumentsByQueue())
        {
            await Should.NotThrowAsync(() => probe.DeclareAsync(queue, durable: true, arguments, ct));
        }
    }

    [Fact]
    public async Task EnsureDeclaredAsync_EveryRequestedArgumentIsInEffect()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);

        await AssertEveryArgumentIsEnforcedAsync(
            RabbitMqTopology.RequestedQueue,
            RabbitMqTopology.RequestedArguments(MaxAttempts),
            ct);
    }

    [Fact]
    public async Task EnsureDeclaredAsync_EveryRetryArgumentIsInEffect()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);

        await AssertEveryArgumentIsEnforcedAsync(
            RabbitMqTopology.RetryQueue,
            RabbitMqTopology.RetryArguments(RabbitMqTopology.RetryDelayMilliseconds(fixture.Options)),
            ct);
    }

    [Fact]
    public async Task EnsureDeclaredAsync_EveryDeadLetterArgumentIsInEffect()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);

        await AssertEveryArgumentIsEnforcedAsync(
            RabbitMqTopology.DeadLetterQueue,
            RabbitMqTopology.DeadLetterArguments(),
            ct);
    }

    [Fact]
    public async Task EnsureDeclaredAsync_DeclaresTheThreeFeaturesQueues()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);

        foreach (var queue in FeaturesQueues())
        {
            var declared = await probe.DeclarePassiveAsync(queue, ct);
            declared.QueueName.ShouldBe(queue);
        }
    }

    [Fact]
    public async Task EnsureDeclaredAsync_EveryFeaturesRequestedArgumentIsInEffect()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);

        await AssertEveryArgumentIsEnforcedAsync(
            RabbitMqTopology.FeaturesRequestedQueue,
            RabbitMqTopology.FeaturesRequestedArguments(MaxAttempts),
            ct);
    }

    [Fact]
    public async Task EnsureDeclaredAsync_EveryFeaturesRetryArgumentIsInEffect()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);

        await AssertEveryArgumentIsEnforcedAsync(
            RabbitMqTopology.FeaturesRetryQueue,
            RabbitMqTopology.FeaturesRetryArguments(RabbitMqTopology.RetryDelayMilliseconds(fixture.Options)),
            ct);
    }

    [Fact]
    public async Task EnsureDeclaredAsync_EveryFeaturesDeadLetterArgumentIsInEffect()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);

        await AssertEveryArgumentIsEnforcedAsync(
            RabbitMqTopology.FeaturesDeadLetterQueue,
            RabbitMqTopology.DeadLetterArguments(),
            ct);
    }

    [Fact]
    public async Task EnsureDeclaredAsync_BindsEveryFeaturesRoutingKeyToItsOwnQueue()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.FeaturesRequestedQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.FeaturesDeadLetterQueue, ct);

        await probe.PublishAsync(EnrichmentRouting.FeaturesRequestedRoutingKey, "a"u8.ToArray(), ct);
        await probe.PublishAsync(EnrichmentRouting.FeaturesDeadLetterRoutingKey, "c"u8.ToArray(), ct);

        (await probe.PollMessageCountAsync(RabbitMqTopology.FeaturesRequestedQueue, ct)).ShouldBe(1U);
        (await probe.PollMessageCountAsync(RabbitMqTopology.FeaturesDeadLetterQueue, ct)).ShouldBe(1U);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.RequestedQueue, ct)).MessageCount.ShouldBe(0U);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.DeadLetterQueue, ct)).MessageCount.ShouldBe(0U);
    }

    [Fact]
    public async Task EnsureDeclaredAsync_AFeaturesRetryMessageReturnsToTheFeaturesRequestedQueue()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.FeaturesRequestedQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.FeaturesRetryQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.RequestedQueue, ct);

        await probe.PublishAsync(EnrichmentRouting.FeaturesRetryRoutingKey, "b"u8.ToArray(), ct);

        (await probe.PollMessageCountAsync(RabbitMqTopology.FeaturesRequestedQueue, ct)).ShouldBe(1U);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.RequestedQueue, ct)).MessageCount.ShouldBe(0U);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.FeaturesDeadLetterQueue, ct)).MessageCount.ShouldBe(0U);
    }

    [Fact]
    public async Task EnsureDeclaredAsync_CalledTwiceOnOneOwner_KeepsTheSameDeclaration()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var owner = NewOwner();
        var topology = NewTopology(owner);
        await DeclareWithRetryAsync(() => topology.EnsureDeclaredAsync(ct), ct);

        await Should.NotThrowAsync(() => topology.EnsureDeclaredAsync(ct));
    }

    [Fact]
    public async Task EnsureDeclaredAsync_CalledOnTwoOwners_IsIdempotentAgainstTheBroker()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var firstOwner = NewOwner();
        await DeclareWithRetryAsync(() => NewTopology(firstOwner).EnsureDeclaredAsync(ct), ct);

        await using var secondOwner = NewOwner();
        var secondTopology = NewTopology(secondOwner);
        await Should.NotThrowAsync(() => DeclareWithRetryAsync(
            () => secondTopology.EnsureDeclaredAsync(ct), ct));
    }

    [Fact]
    public async Task EnsureDeclaredAsync_BindsEveryRoutingKeyToItsOwnQueue()
    {
        var ct = TestContext.Current.CancellationToken;
        await DeclareAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.RequestedQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.DeadLetterQueue, ct);

        await probe.PublishAsync(RabbitMqTopology.RequestedRoutingKey, "a"u8.ToArray(), ct);
        await probe.PublishAsync(RabbitMqTopology.DeadLetterRoutingKey, "c"u8.ToArray(), ct);

        (await probe.PollMessageCountAsync(RabbitMqTopology.RequestedQueue, ct)).ShouldBe(1U);
        (await probe.PollMessageCountAsync(RabbitMqTopology.DeadLetterQueue, ct)).ShouldBe(1U);
    }

    private static IEnumerable<string> Queues() =>
    [
        RabbitMqTopology.RequestedQueue,
        RabbitMqTopology.RetryQueue,
        RabbitMqTopology.DeadLetterQueue,
    ];

    private static IEnumerable<string> FeaturesQueues() =>
    [
        RabbitMqTopology.FeaturesRequestedQueue,
        RabbitMqTopology.FeaturesRetryQueue,
        RabbitMqTopology.FeaturesDeadLetterQueue,
    ];

    private IEnumerable<(string Queue, IDictionary<string, object?> Arguments)> ArgumentsByQueue()
    {
        yield return (RabbitMqTopology.RequestedQueue, RabbitMqTopology.RequestedArguments(MaxAttempts));
        yield return (
            RabbitMqTopology.RetryQueue,
            RabbitMqTopology.RetryArguments(RabbitMqTopology.RetryDelayMilliseconds(fixture.Options)));
        yield return (RabbitMqTopology.DeadLetterQueue, RabbitMqTopology.DeadLetterArguments());
        yield return (
            RabbitMqTopology.FeaturesRequestedQueue,
            RabbitMqTopology.FeaturesRequestedArguments(MaxAttempts));
        yield return (
            RabbitMqTopology.FeaturesRetryQueue,
            RabbitMqTopology.FeaturesRetryArguments(RabbitMqTopology.RetryDelayMilliseconds(fixture.Options)));
        yield return (RabbitMqTopology.FeaturesDeadLetterQueue, RabbitMqTopology.DeadLetterArguments());
    }

    private async Task AssertEveryArgumentIsEnforcedAsync(
        string queue,
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        await Should.NotThrowAsync(() => probe.DeclareAsync(queue, durable: true, arguments, cancellationToken));

        foreach (var key in arguments.Keys)
        {
            var perturbed = Perturbed(arguments, key);
            var rejection = await Should.ThrowAsync<OperationInterruptedException>(
                () => probe.DeclareAsync(queue, durable: true, perturbed, cancellationToken));
            rejection.Message.ShouldContain("406");
        }
    }

    private static IDictionary<string, object?> Perturbed(IDictionary<string, object?> arguments, string key)
    {
        var copy = new Dictionary<string, object?>(arguments, StringComparer.Ordinal);
        copy[key] = copy[key] switch
        {
            string text => text + "-perturbed",
            int number => number + 1,
            _ => throw new InvalidOperationException($"Unexpected argument '{key}'."),
        };
        return copy;
    }

    private async Task DeclareAsync(CancellationToken cancellationToken)
    {
        await DeclareWithRetryAsync(
            async () =>
            {
                await using var owner = NewOwner();
                await NewTopology(owner).EnsureDeclaredAsync(cancellationToken);
            },
            cancellationToken);
    }

    private static async Task DeclareWithRetryAsync(Func<Task> declare, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await declare();
                return;
            }
            catch (Exception failure)
                when (attempt < MaxDeclareAttempts
                    && IsClusterNotFormed(failure)
                    && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(DeclareRetryDelays[attempt - 1], cancellationToken);
            }
        }
    }

    private static bool IsClusterNotFormed(Exception failure)
    {
        for (var current = failure; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("541", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("cluster_not_formed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private RabbitMqConnectionOwner NewOwner() => new(Options.Create(fixture.Options));

    private RabbitMqTopology NewTopology(RabbitMqConnectionOwner owner) =>
        new(
            owner,
            Options.Create(fixture.Options),
            Options.Create(new EnrichmentOptions { MaxAttempts = MaxAttempts }));
}



