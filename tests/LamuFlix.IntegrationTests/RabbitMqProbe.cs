using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using RabbitMQ.Client;
using Xunit;

namespace LamuFlix.IntegrationTests;

internal sealed class RabbitMqProbe(RabbitMqFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    private IConnection? connection;
    private readonly SemaphoreSlim gate = new(1, 1);

    public async ValueTask InitializeAsync() => await RequireConnectionAsync();

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }

        gate.Dispose();
    }

    private async Task<IChannel> OpenChannelAsync(CancellationToken cancellationToken) =>
        await (await RequireConnectionAsync()).CreateChannelAsync(cancellationToken: cancellationToken);

    public async Task DeclarePassiveExchangeAsync(string exchange, CancellationToken cancellationToken)
    {
        await using var channel = await OpenChannelAsync(cancellationToken);
        await channel.ExchangeDeclarePassiveAsync(exchange, cancellationToken: cancellationToken);
    }

    public async Task DeclareExchangeAsync(
        string exchange,
        string exchangeType,
        bool durable,
        CancellationToken cancellationToken)
    {
        await using var channel = await OpenChannelAsync(cancellationToken);
        await channel.ExchangeDeclareAsync(
            exchange,
            exchangeType,
            durable: durable,
            autoDelete: false,
            cancellationToken: cancellationToken);
    }

    public async Task DeclareAsync(
        string queue,
        bool durable,
        IDictionary<string, object?> arguments,
        CancellationToken cancellationToken)
    {
        await using var channel = await OpenChannelAsync(cancellationToken);
        await channel.QueueDeclareAsync(
            queue,
            durable: durable,
            exclusive: false,
            autoDelete: false,
            arguments: arguments,
            cancellationToken: cancellationToken);
    }

    public async Task<QueueDeclareOk> DeclarePassiveAsync(string queue, CancellationToken cancellationToken)
    {
        await using var channel = await OpenChannelAsync(cancellationToken);
        return await channel.QueueDeclarePassiveAsync(queue, cancellationToken: cancellationToken);
    }

    public async Task<uint> PollMessageCountAsync(string queue, CancellationToken cancellationToken)
    {
        var clock = TimeProvider.System;
        var deadline = clock.GetUtcNow() + Bound;
        uint count = 0;
        while (clock.GetUtcNow() < deadline)
        {
            count = (await DeclarePassiveAsync(queue, cancellationToken)).MessageCount;
            if (count > 0)
            {
                return count;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        return count;
    }

    public async Task PublishAsync(string routingKey, byte[] body, CancellationToken cancellationToken)
    {
        await using var channel = await OpenChannelAsync(cancellationToken);
        await channel.BasicPublishAsync(
            RabbitMqTopology.ExchangeName,
            routingKey,
            mandatory: true,
            basicProperties: new BasicProperties { Persistent = true },
            body: body,
            cancellationToken: cancellationToken);
    }

    public async Task PublishUnroutableAsync(byte[] body, CancellationToken cancellationToken)
    {
        await using var channel = await OpenChannelAsync(cancellationToken);
        await channel.BasicPublishAsync(
            RabbitMqTopology.ExchangeName,
            "no-such-routing-key",
            mandatory: true,
            basicProperties: new BasicProperties { Persistent = true },
            body: body,
            cancellationToken: cancellationToken);
    }

    public async Task<BasicGetResult?> PollGetAsync(string queue, CancellationToken cancellationToken) =>
        await PollGetMatchingAsync(queue, _ => true, cancellationToken);

    public async Task<BasicGetResult?> PollGetMatchingAsync(
        string queue,
        Func<BasicGetResult, bool> accept,
        CancellationToken cancellationToken)
    {
        var clock = TimeProvider.System;
        var deadline = clock.GetUtcNow() + Bound;
        while (clock.GetUtcNow() < deadline)
        {
            await using var channel = await OpenChannelAsync(cancellationToken);
            var result = await channel.BasicGetAsync(queue, autoAck: true, cancellationToken: cancellationToken);
            if (result is not null && accept(result))
            {
                return result;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        return null;
    }

    public async Task<Dictionary<string, BasicGetResult>> PollGetAllAsync(
        string queue,
        IReadOnlySet<string> keys,
        Func<BasicGetResult, string> keyOf,
        CancellationToken cancellationToken)
    {
        var found = new Dictionary<string, BasicGetResult>(StringComparer.Ordinal);
        var clock = TimeProvider.System;
        var deadline = clock.GetUtcNow() + Bound;
        while (found.Count < keys.Count && clock.GetUtcNow() < deadline)
        {
            await using var channel = await OpenChannelAsync(cancellationToken);
            var result = await channel.BasicGetAsync(queue, autoAck: true, cancellationToken: cancellationToken);
            if (result is not null)
            {
                var key = keyOf(result);
                if (keys.Contains(key))
                {
                    found[key] = result;
                }
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        return found;
    }

    public async Task DrainAsync(string queue, CancellationToken cancellationToken)
    {
        var clock = TimeProvider.System;
        var deadline = clock.GetUtcNow() + Bound;
        while (clock.GetUtcNow() < deadline)
        {
            await using var channel = await OpenChannelAsync(cancellationToken);
            var result = await channel.BasicGetAsync(queue, autoAck: true, cancellationToken: cancellationToken);
            if (result is null)
            {
                return;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }
    }

    private async Task<IConnection> RequireConnectionAsync()
    {
        if (connection is not null)
        {
            return connection;
        }

        await gate.WaitAsync();
        try
        {
            connection ??= await fixture.CreateConnectionAsync();
            return connection;
        }
        finally
        {
            gate.Release();
        }
    }
}