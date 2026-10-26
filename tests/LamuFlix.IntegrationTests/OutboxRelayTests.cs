using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Enrichment;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[CollectionDefinition(nameof(OutboxRelayCollection))]
public sealed class OutboxRelayCollection : ICollectionFixture<PostgresFixture>, ICollectionFixture<RabbitMqFixture>;

[Collection(nameof(OutboxRelayCollection))]
public sealed class OutboxRelayTests(PostgresFixture postgres, RabbitMqFixture rabbitMq) : IAsyncLifetime
{
    private const string WorkerTypeName = "LamuFlix.Infrastructure.Enrichment.OutboxRelayHostedService";
    private static readonly DateTimeOffset StartedAt = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await postgres.ResetAsync(cancellationToken);
        await rabbitMq.ResetTopologyAsync(cancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Harness_ReachesRealPostgresOutboxTableAndRealRabbitMqTopology()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var provider = CreateRelayProvider();

        // act
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LamuFlixDbContext>();
        var stored = await context.OutboxMessages.CountAsync(cancellationToken);
        await using var connection = await rabbitMq.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        var queue = await channel.QueueDeclarePassiveAsync(RabbitMqTopology.RequestedQueue, cancellationToken);

        // assert
        stored.ShouldBe(0);
        queue.QueueName.ShouldBe(RabbitMqTopology.RequestedQueue);
    }

    [Fact]
    public async Task RelayWorker_IsImplementedSoRealDatabaseAndBrokerProofsCanRun()
    {
        // arrange
        await using var provider = CreateRelayProvider();

        // act
        var workerType = RelayWorkerType();

        // assert
        workerType.ShouldNotBeNull(
            $"T009 must add {WorkerTypeName} before the real Postgres and RabbitMQ relay proofs can run (FR-002, FR-010).");
        ActivatorUtilities.CreateInstance(provider, workerType).ShouldBeAssignableTo<BackgroundService>();
    }

    private static Type? RelayWorkerType() =>
        typeof(OutboxRelayState).Assembly.GetType(WorkerTypeName, throwOnError: false);

    private ServiceProvider CreateRelayProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{OutboxRelayOptions.SectionName}:PollInterval"] = "00:00:00.050",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<LamuFlixDbContext>(options =>
            options.UseNpgsql(postgres.ConnectionString, builder => builder.UseVector()));
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(StartedAt));
        services.AddSingleton(Options.Create(rabbitMq.Options));
        services.AddSingleton(Options.Create(new EnrichmentOptions
        {
            ClaimLease = TimeSpan.FromSeconds(5),
            SweepInterval = TimeSpan.FromMinutes(1),
        }));
        services.AddSingleton<RabbitMqConnectionOwner>();
        services.AddSingleton<RabbitMqTopology>();
        services.AddSingleton<RabbitMqEnrichmentQueuePublisher>();
        services.AddSingleton<IEnrichmentQueue>(provider =>
            provider.GetRequiredService<RabbitMqEnrichmentQueuePublisher>());
        services.AddLamuFlixOutboxRelay();
        return services.BuildServiceProvider();
    }
}
