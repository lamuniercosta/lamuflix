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

    private static readonly TimeSpan[] TopologyRetryDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
        TimeSpan.FromSeconds(8),
    ];

    // Purge order: the TTL queues that dead-letter into the work queues go first, so nothing
    // can expire into a queue that has already been purged.
    private static readonly string[] PurgeOrder =
    [
        RabbitMqTopology.RetryQueue,
        RabbitMqTopology.QuotaDeferredQueue,
        RabbitMqTopology.FeaturesRetryQueue,
        RabbitMqTopology.RequestedQueue,
        RabbitMqTopology.FeaturesRequestedQueue,
        RabbitMqTopology.DeadLetterQueue,
        RabbitMqTopology.FeaturesDeadLetterQueue,
    ];

    private const int MaxAttempts = 3;
    private const int MaxTopologyAttempts = 4;

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
        // Deleting and re-declaring the seven quorum queues costs about 3 s, paid before every
        // broker test. When the topology is intact, re-declaring it is a no-op and purging empties
        // it in milliseconds. Any sign a test changed it (a queue deleted or re-declared with other
        // arguments, a consumer still attached) falls back to the full rebuild.
        if (await TryPurgeTopologyAsync(cancellationToken))
        {
            return;
        }

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
        Exception? lastFailure = null;
        for (var attempt = 1; attempt <= MaxTopologyAttempts; attempt++)
        {
            try
            {
                await using var owner = new RabbitMqConnectionOwner(Wrap(Options));
                await NewTopology(owner).EnsureDeclaredAsync(cancellationToken);
                return;
            }
            catch (Exception failure)
                when (attempt < MaxTopologyAttempts && !cancellationToken.IsCancellationRequested)
            {
                lastFailure = failure;
                await Task.Delay(TopologyRetryDelays[attempt - 1], cancellationToken);
            }
        }

        throw lastFailure ?? new InvalidOperationException("RabbitMqFixture topology declaration failed.");
    }

    private async Task<bool> TryPurgeTopologyAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using (var owner = new RabbitMqConnectionOwner(Wrap(Options)))
            {
                await NewTopology(owner).EnsureDeclaredAsync(cancellationToken);
            }

            await using var connection = await CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            foreach (var queue in PurgeOrder)
            {
                var state = await channel.QueueDeclarePassiveAsync(queue, cancellationToken);
                if (state.ConsumerCount > 0)
                {
                    return false;
                }

                await channel.QueuePurgeAsync(queue, cancellationToken);
            }

            return true;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Deliberately swallowed: the full rebuild below is the original reset and either
            // restores the topology or throws its own failure.
            return false;
        }
    }

    private static async Task DeleteTopologyAsync(IConnection connection, CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueueDeleteAsync(RabbitMqTopology.RequestedQueue, cancellationToken: cancellationToken);
        await channel.QueueDeleteAsync(RabbitMqTopology.RetryQueue, cancellationToken: cancellationToken);
        await channel.QueueDeleteAsync(RabbitMqTopology.DeadLetterQueue, cancellationToken: cancellationToken);
        await channel.QueueDeleteAsync(RabbitMqTopology.QuotaDeferredQueue, cancellationToken: cancellationToken);
        await channel.QueueDeleteAsync(RabbitMqTopology.FeaturesRequestedQueue, cancellationToken: cancellationToken);
        await channel.QueueDeleteAsync(RabbitMqTopology.FeaturesRetryQueue, cancellationToken: cancellationToken);
        await channel.QueueDeleteAsync(RabbitMqTopology.FeaturesDeadLetterQueue, cancellationToken: cancellationToken);
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