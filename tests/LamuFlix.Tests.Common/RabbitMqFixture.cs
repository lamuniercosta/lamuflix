using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.RabbitMq;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;
using Xunit;

namespace LamuFlix.Tests.Common;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    private const int MaxAttempts = 3;

    private RabbitMqContainer? container;
    private ConnectionFactory? factory;

    public RabbitMqContainer Container =>
        container ?? throw new InvalidOperationException(
            "RabbitMqFixture has not been initialized. InitializeAsync must run first.");

    public RabbitMqOptions Options => OptionsFor(new Uri(Container.GetConnectionString()));

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        try
        {
            container = new RabbitMqBuilder("rabbitmq:4-management-alpine").Build();
            await container.StartAsync(cancellationToken);
            factory = new ConnectionFactory { Uri = new Uri(container.GetConnectionString()) };
            await EnsureTopologyAsync(cancellationToken);
        }
        catch
        {
            await DiscardAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (container is not { } started)
        {
            return;
        }

        await started.DisposeAsync();
        container = null;
    }

    public Task<IConnection> CreateConnectionAsync() => RequireFactory().CreateConnectionAsync();

    public Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken) =>
        RequireFactory().CreateConnectionAsync(cancellationToken);

    public async Task ResetTopologyAsync(CancellationToken cancellationToken)
    {
        await using var connection = await CreateConnectionAsync(cancellationToken);
        await DeleteTopologyAsync(connection, cancellationToken);
        await EnsureTopologyAsync(cancellationToken);
    }

    public async Task DeleteTopologyAsync(CancellationToken cancellationToken)
    {
        await using var connection = await CreateConnectionAsync(cancellationToken);
        await DeleteTopologyAsync(connection, cancellationToken);
    }

    private static RabbitMqOptions OptionsFor(Uri uri)
    {
        var credentials = uri.UserInfo.Split(':', 2);
        var secret = credentials.Length > 1 ? credentials[1] : string.Empty;
        return new RabbitMqOptions
        {
            HostName = uri.Host,
            Port = uri.Port,
            UserName = credentials[0],
            Password = secret,
            RetryDelay = RetryDelay,
            Prefetch = 1,
        };
    }

    private async Task EnsureTopologyAsync(CancellationToken cancellationToken)
    {
        await using var owner = new RabbitMqConnectionOwner(Wrap(Options));
        await NewTopology(owner).EnsureDeclaredAsync(cancellationToken);
    }

    private static async Task DeleteTopologyAsync(IConnection connection, CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueueDeleteAsync(RabbitMqTopology.RequestedQueue, cancellationToken: cancellationToken);
        await channel.QueueDeleteAsync(RabbitMqTopology.RetryQueue, cancellationToken: cancellationToken);
        await channel.QueueDeleteAsync(RabbitMqTopology.DeadLetterQueue, cancellationToken: cancellationToken);
        await channel.ExchangeDeleteAsync(RabbitMqTopology.ExchangeName, cancellationToken: cancellationToken);
    }

    private RabbitMqTopology NewTopology(RabbitMqConnectionOwner owner) =>
        new(
            owner,
            Wrap(Options),
            Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { MaxAttempts = MaxAttempts }));

    private static IOptions<RabbitMqOptions> Wrap(RabbitMqOptions value) =>
        Microsoft.Extensions.Options.Options.Create(value);

    private ConnectionFactory RequireFactory() =>
        factory ?? throw new InvalidOperationException("RabbitMqFixture has not been initialized.");

    private async ValueTask DiscardAsync()
    {
        if (container is not { } started)
        {
            return;
        }

        container = null;
        try
        {
            await started.DisposeAsync();
        }
        catch (Exception)
        {
            // Deliberately empty: a cleanup failure must not replace the initialization failure.
        }
    }
}