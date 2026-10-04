using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class RabbitMqConnectionOwner(IOptions<RabbitMqOptions> options) : IAsyncDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private IConnection? connection;

    public async Task<IConnection> GetAsync(CancellationToken cancellationToken)
    {
        if (connection is { IsOpen: true })
        {
            return connection;
        }

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (connection is { IsOpen: true })
            {
                return connection;
            }

            if (connection is not null)
            {
                await connection.DisposeAsync();
                connection = null;
            }

            var factory = CreateFactory(options.Value);
            var created = await factory.CreateConnectionAsync(cancellationToken);
            connection = created;
            return created;
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (connection is not null)
            {
                await connection.DisposeAsync();
                connection = null;
            }
        }
        finally
        {
            gate.Release();
            gate.Dispose();
        }
    }

    private static ConnectionFactory CreateFactory(RabbitMqOptions value)
    {
        var secret = value.Password;
        return new ConnectionFactory
        {
            HostName = value.HostName,
            Port = value.Port,
            UserName = value.UserName,
            Password = secret,
            AutomaticRecoveryEnabled = false,
        };
    }
}