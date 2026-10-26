using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Enrichment;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.Persistence.Records;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
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
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private RabbitMqProbe probe = null!;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await postgres.ResetAsync(cancellationToken);
        await rabbitMq.ResetTopologyAsync(cancellationToken);
        probe = new RabbitMqProbe(rabbitMq);
        await probe.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await probe.DisposeAsync();
    }

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

    [Fact]
    public async Task RelayPass_PublishesUnprocessedRowsInCreatedAtThenIdOrderBoundedByBatchSize()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var newest = await InsertRowAsync(StartedAt.AddMinutes(3), movieId: 30, cancellationToken: cancellationToken);
        var olderFirst = await InsertRowAsync(StartedAt.AddMinutes(1), movieId: 20, cancellationToken: cancellationToken);
        var olderSecond = await InsertRowAsync(StartedAt.AddMinutes(1), movieId: 21, cancellationToken: cancellationToken);
        await using var provider = CreateRelayProvider(new Dictionary<string, string?>
        {
            [$"{OutboxRelayOptions.SectionName}:BatchSize"] = "2",
        });
        var worker = CreateWorker(provider);

        // act
        var firstPass = await worker.RunPassAsync(cancellationToken);

        // assert
        firstPass.ShouldBe(OutboxRelayPassResult.Succeeded);
        var published = await DrainAsync(2, cancellationToken);
        published.Select(message => message.MovieId.Value).ShouldBe(new[] { 20, 21 });
        (await ReadProcessedAtAsync(olderFirst, cancellationToken)).ShouldBe(StartedAt);
        (await ReadProcessedAtAsync(olderSecond, cancellationToken)).ShouldBe(StartedAt);
        (await ReadProcessedAtAsync(newest, cancellationToken)).ShouldBeNull();

        var secondPass = await worker.RunPassAsync(cancellationToken);

        secondPass.ShouldBe(OutboxRelayPassResult.Succeeded);
        var remaining = await DrainAsync(1, cancellationToken);
        remaining.Select(message => message.MovieId.Value).ShouldBe(new[] { 30 });
        (await ReadProcessedAtAsync(newest, cancellationToken)).ShouldBe(StartedAt);
    }

    [Fact]
    public async Task RelayPass_SkipsMarkedRowsAndARestartDoesNotRepublishThem()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var marked = await InsertRowAsync(
            StartedAt.AddMinutes(1),
            movieId: 40,
            processedAt: StartedAt.AddMinutes(2),
            cancellationToken: cancellationToken);
        var pending = await InsertRowAsync(StartedAt.AddMinutes(3), movieId: 41, cancellationToken: cancellationToken);

        await using (var firstProvider = CreateRelayProvider())
        {
            var worker = CreateWorker(firstProvider);
            var pass = await worker.RunPassAsync(cancellationToken);
            pass.ShouldBe(OutboxRelayPassResult.Succeeded);
        }

        var firstDelivery = await DrainAsync(1, cancellationToken);

        // act
        await using var restartedProvider = CreateRelayProvider();
        var restartedWorker = CreateWorker(restartedProvider);
        var restartedPass = await restartedWorker.RunPassAsync(cancellationToken);

        // assert
        firstDelivery.Select(message => message.MovieId.Value).ShouldBe(new[] { 41 });
        restartedPass.ShouldBe(OutboxRelayPassResult.Succeeded);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.RequestedQueue, cancellationToken)).MessageCount.ShouldBe(0u);
        (await ReadProcessedAtAsync(marked, cancellationToken)).ShouldBe(StartedAt.AddMinutes(2));
        (await ReadProcessedAtAsync(pending, cancellationToken)).ShouldBe(StartedAt);
    }

    [Fact]
    public async Task RelayPass_CommitsTheMarkerOnlyAfterTheConfirmedPublishSettles()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var row = await InsertRowAsync(StartedAt.AddMinutes(1), movieId: 50, attempt: 3, cancellationToken: cancellationToken);
        var publishGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var publishStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var provider = CreateRelayProvider(
            queueDecorator: inner => new FaultedQueue(inner)
            {
                Publish = async (message, token) =>
                {
                    publishStarted.TrySetResult();
                    await publishGate.Task.WaitAsync(token);
                    await inner.EnqueueAsync(message, token);
                },
            });
        var worker = CreateWorker(provider);

        // act
        var pass = worker.RunPassAsync(cancellationToken);
        await publishStarted.Task.WaitAsync(cancellationToken);
        var markerBeforeConfirm = await ReadProcessedAtAsync(row, cancellationToken);
        markerBeforeConfirm.ShouldBeNull();
        publishGate.SetResult();
        var outcome = await pass;

        // assert
        markerBeforeConfirm.ShouldBeNull();
        outcome.ShouldBe(OutboxRelayPassResult.Succeeded);
        (await ReadProcessedAtAsync(row, cancellationToken)).ShouldBe(StartedAt);
        var delivered = await DrainAsync(1, cancellationToken);
        delivered.ShouldHaveSingleItem().ShouldBe(new EnrichmentRequested(new MovieId(50), 3));
    }

    [Fact]
    public async Task RelayPass_ControlledPublisherFaultsLeaveTheRowUnprocessed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var behaviour = new QueueBehaviour();
        await using var provider = CreateRelayProvider(
            settings: new Dictionary<string, string?>
            {
                [$"{OutboxRelayOptions.SectionName}:PublishTimeout"] = "00:00:00.200",
            },
            queueDecorator: inner => new FaultedQueue(inner)
            {
                Publish = (message, token) => behaviour.PublishAsync(message, token),
            });
        var worker = CreateWorker(provider);

        // act / assert
        foreach (var mode in new[]
                 {
                     QueueBehaviourMode.ConnectionFailure,
                     QueueBehaviourMode.Nack,
                     QueueBehaviourMode.Timeout,
                 })
        {
            await ClearOutboxAsync(cancellationToken);
            var row = await InsertRowAsync(StartedAt.AddMinutes(1), movieId: 60, cancellationToken: cancellationToken);
            behaviour.Mode = mode;

            var outcome = await worker.RunPassAsync(cancellationToken);

            outcome.ShouldBe(OutboxRelayPassResult.Failed);
            (await ReadProcessedAtAsync(row, cancellationToken)).ShouldBeNull();
        }

        (await probe.DeclarePassiveAsync(RabbitMqTopology.RequestedQueue, cancellationToken)).MessageCount.ShouldBe(0u);
    }

    [Fact]
    public async Task RelayPass_MandatoryReturnFromTheRealBrokerLeavesTheRowUnprocessed()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var row = await InsertRowAsync(StartedAt.AddMinutes(1), movieId: 70, cancellationToken: cancellationToken);
        await using var provider = CreateRelayProvider();
        await provider.GetRequiredService<RabbitMqTopology>().EnsureDeclaredAsync(cancellationToken);
        await UnbindRequestedQueueAsync(cancellationToken);
        var worker = CreateWorker(provider);

        // act
        var outcome = await worker.RunPassAsync(cancellationToken);

        // assert
        outcome.ShouldBe(OutboxRelayPassResult.Failed);
        (await ReadProcessedAtAsync(row, cancellationToken)).ShouldBeNull();
        (await probe.DeclarePassiveAsync(RabbitMqTopology.RequestedQueue, cancellationToken)).MessageCount.ShouldBe(0u);
    }

    [Fact]
    public async Task RelayPass_ConfirmedPublishWithAFailedMarkerPermitsAtLeastOnceRetry()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var row = await InsertRowAsync(StartedAt.AddMinutes(1), movieId: 80, cancellationToken: cancellationToken);
        await using var provider = CreateRelayProvider();
        var worker = CreateWorker(provider);
        worker.SaveFault = (_, call) => call == 1
            ? Task.FromException(new InvalidOperationException("marker commit failed"))
            : Task.CompletedTask;

        // act
        var failedPass = await worker.RunPassAsync(cancellationToken);
        var deliveredOnce = await DrainAsync(1, cancellationToken);
        var retriedPass = await worker.RunPassAsync(cancellationToken);
        var deliveredTwice = await DrainAsync(1, cancellationToken);

        // assert
        failedPass.ShouldBe(OutboxRelayPassResult.Failed);
        (await ReadProcessedAtAsync(row, cancellationToken)).ShouldBe(StartedAt);
        retriedPass.ShouldBe(OutboxRelayPassResult.Succeeded);
        deliveredOnce.ShouldHaveSingleItem().MovieId.Value.ShouldBe(80);
        deliveredTwice.ShouldHaveSingleItem().MovieId.Value.ShouldBe(80);
    }

    [Fact]
    public async Task RelayPass_CommittedPrefixSurvivesAMidBatchFailure()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var first = await InsertRowAsync(StartedAt.AddMinutes(1), movieId: 90, cancellationToken: cancellationToken);
        var second = await InsertRowAsync(StartedAt.AddMinutes(2), movieId: 91, cancellationToken: cancellationToken);
        await using var provider = CreateRelayProvider();
        var worker = CreateWorker(provider);
        worker.SaveFault = (_, call) => call == 2
            ? Task.FromException(new InvalidOperationException("marker commit failed"))
            : Task.CompletedTask;

        // act
        var outcome = await worker.RunPassAsync(cancellationToken);

        // assert
        outcome.ShouldBe(OutboxRelayPassResult.Failed);
        worker.SavedRowIds.ShouldBe(new[] { first });
        (await ReadProcessedAtAsync(first, cancellationToken)).ShouldBe(StartedAt);
        (await ReadProcessedAtAsync(second, cancellationToken)).ShouldBeNull();

        var recovered = await worker.RunPassAsync(cancellationToken);

        recovered.ShouldBe(OutboxRelayPassResult.Succeeded);
        (await ReadProcessedAtAsync(second, cancellationToken)).ShouldBe(StartedAt);
    }

    private static Type? RelayWorkerType() =>
        typeof(OutboxRelayState).Assembly.GetType(WorkerTypeName, throwOnError: false);

    private static ScriptedOutboxRelayWorker CreateWorker(ServiceProvider provider) =>
        ActivatorUtilities.CreateInstance<ScriptedOutboxRelayWorker>(provider);

    private ServiceProvider CreateRelayProvider(
        Dictionary<string, string?>? settings = null,
        Func<IEnrichmentQueue, IEnrichmentQueue>? queueDecorator = null)
    {
        var values = new Dictionary<string, string?>
        {
            [$"{OutboxRelayOptions.SectionName}:PollInterval"] = "00:00:00.050",
        };
        foreach (var (key, value) in settings ?? [])
        {
            values[key] = value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
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
        {
            var publisher = provider.GetRequiredService<RabbitMqEnrichmentQueuePublisher>();
            return queueDecorator is null ? publisher : queueDecorator(publisher);
        });
        services.AddLogging();
        services.AddLamuFlixOutboxRelay();
        return services.BuildServiceProvider();
    }

    private async Task<long> InsertRowAsync(
        DateTimeOffset createdAt,
        int movieId,
        int attempt = 1,
        DateTimeOffset? processedAt = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = postgres.CreateMigratedContext();
        var record = new OutboxMessageRecord
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(
                new EnrichmentRequested(new MovieId(movieId), attempt),
                SerializerOptions),
            CreatedAt = createdAt,
            ProcessedAt = processedAt,
        };
        context.OutboxMessages.Add(record);
        await context.SaveChangesAsync(cancellationToken);
        return record.Id;
    }

    private async Task<DateTimeOffset?> ReadProcessedAtAsync(long id, CancellationToken cancellationToken)
    {
        await using var context = postgres.CreateMigratedContext();
        return await context.OutboxMessages
            .Where(message => message.Id == id)
            .Select(message => message.ProcessedAt)
            .SingleAsync(cancellationToken);
    }

    private async Task ClearOutboxAsync(CancellationToken cancellationToken)
    {
        await using var context = postgres.CreateMigratedContext();
        await context.OutboxMessages.ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<List<EnrichmentRequested>> DrainAsync(int expected, CancellationToken cancellationToken)
    {
        var delivered = new List<EnrichmentRequested>(expected);
        while (delivered.Count < expected)
        {
            var result = await probe.PollGetAsync(RabbitMqTopology.RequestedQueue, cancellationToken);
            if (result is null)
            {
                break;
            }

            delivered.Add(JsonSerializer.Deserialize<EnrichmentRequested>(result.Body.Span, SerializerOptions)
                ?? throw new JsonException("The delivered payload was empty."));
        }

        return delivered;
    }

    private async Task UnbindRequestedQueueAsync(CancellationToken cancellationToken)
    {
        await using var connection = await rabbitMq.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueueUnbindAsync(
            RabbitMqTopology.RequestedQueue,
            RabbitMqTopology.ExchangeName,
            RabbitMqTopology.RequestedRoutingKey,
            cancellationToken: cancellationToken);
    }

    private enum QueueBehaviourMode
    {
        ConnectionFailure = 0,
        Nack = 1,
        Timeout = 2,
    }

    private sealed class QueueBehaviour
    {
        public QueueBehaviourMode Mode { get; set; }

        public Task PublishAsync(EnrichmentRequested message, CancellationToken cancellationToken) => Mode switch
        {
            QueueBehaviourMode.ConnectionFailure => Task.FromException(
                new IOException("broker connection lost")),
            QueueBehaviourMode.Nack => Task.FromException(
                new InvalidOperationException("broker nacknowledged the publish")),
            _ => Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken),
        };
    }

    private sealed class FaultedQueue(IEnrichmentQueue inner) : IEnrichmentQueue
    {
        public Func<EnrichmentRequested, CancellationToken, Task>? Publish { get; set; }

        public Task EnqueueAsync(EnrichmentRequested message, CancellationToken ct) =>
            Publish is null ? inner.EnqueueAsync(message, ct) : Publish(message, ct);

        public Task EnqueueAsync(FeaturesRequested message, CancellationToken ct) =>
            inner.EnqueueAsync(message, ct);
    }

    private sealed class ScriptedOutboxRelayWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxRelayOptions> options,
        TimeProvider timeProvider,
        IEnrichmentQueue queue,
        OutboxRelayState state,
        ILogger<OutboxRelayHostedService> logger)
        : OutboxRelayHostedService(scopeFactory, options, timeProvider, queue, state, logger)
    {
        private readonly ConcurrentQueue<long> savedRowIds = new();
        private int saveCalls;

        public Func<OutboxMessageRecord, int, Task>? SaveFault { get; set; }

        public IReadOnlyCollection<long> SavedRowIds => [.. savedRowIds];

        internal override async Task SaveMarkerAsync(
            IServiceProvider scopedProvider,
            OutboxMessageRecord row,
            DateTimeOffset processedAt,
            CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref saveCalls);
            if (SaveFault is not null)
            {
                await SaveFault(row, call);
            }

            await base.SaveMarkerAsync(scopedProvider, row, processedAt, cancellationToken);
            savedRowIds.Enqueue(row.Id);
        }
    }
}
