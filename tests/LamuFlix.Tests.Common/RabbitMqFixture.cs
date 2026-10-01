using System;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace LamuFlix.Tests.Common;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private RabbitMqContainer? container;
    private ConnectionFactory? factory;

    public RabbitMqContainer Container =>
        container ?? throw new InvalidOperationException(
            "RabbitMqFixture has not been initialized. InitializeAsync must run first.");

    public RabbitMqOptions Options => OptionsFor(new Uri(Container.GetConnectionString()));

    public async ValueTask InitializeAsync()
    {
        container = new RabbitMqBuilder("rabbitmq:4.0.0").Build();
        await container.StartAsync();
        factory = new ConnectionFactory { Uri = new Uri(container.GetConnectionString()) };
    }

    public async ValueTask DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    public Task<IConnection> CreateConnectionAsync() =>
        (factory ?? throw new InvalidOperationException("RabbitMqFixture has not been initialized."))
        .CreateConnectionAsync();

    public static RabbitMqOptions OptionsFor(Uri uri)
    {
        var credentials = uri.UserInfo.Split(':', 2);
        var secret = credentials.Length > 1 ? credentials[1] : string.Empty;
        return new RabbitMqOptions
        {
            HostName = uri.Host,
            Port = uri.Port,
            UserName = credentials[0],
            Password = secret,
        };
    }
}