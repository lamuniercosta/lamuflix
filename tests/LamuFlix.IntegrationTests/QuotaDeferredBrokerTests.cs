using Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shouldly;

namespace LamuFlix.IntegrationTests;

public sealed class QuotaDeferredBrokerTests(RabbitMqFixture rabbit, PostgresFixture postgres)
    : IClassFixture<RabbitMqFixture>, IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 23, 59, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NextUtcMidnight = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(1);
    private const int DefaultMaxAttempts = 3;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RabbitMqProbe probe = new(rabbit);
    private readonly PostgresFixture fixture = postgres;

    public async ValueTask InitializeAsync()
    {
        var ct = TestContext.Current.CancellationToken;
        await rabbit.ResetTopologyAsync(ct);
        await fixture.ResetAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.RequestedQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.QuotaDeferredQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.RetryQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.DeadLetterQueue, ct);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Publish_NearMidnightClock_RealTtlExpiresIntoRequested()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(_ => new MetadataLookupResult.NotFound(), new AdjustableTimeProvider(NextUtcMidnight.AddSeconds(-2)));
        var movie = await host.AddMovieAsync(ct, EnrichmentStatus.Pending, deferredUntil: NextUtcMidnight);

        // act
        await host.Publisher.PublishAsync(
            new EnrichmentRequested(movie, 4),
            RabbitMqTopology.QuotaDeferredRoutingKey,
            new Dictionary<string, object?>(StringComparer.Ordinal),
            ct,
            expirationMilliseconds: 1500);

        // assert
        var delivered = await probe.PollGetMatchingAsync(
            RabbitMqTopology.RequestedQueue,
            candidate => Read(candidate).MovieId == movie,
            ct);
        delivered.ShouldNotBeNull();
        Read(delivered).Attempt.ShouldBe(4);
        (await probe.PollQuietMessageCountAsync(RabbitMqTopology.QuotaDeferredQueue, ct)).ShouldBe(0U);
    }

    [Fact]
    public async Task ConsumerSettlementPublish_FakeClockAtEqualOrAfterPersistedDeadline_RecordsExpirationZeroOnePublishOriginalAttemptPublishBeforeAck()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var clock = new EqualDeadlineTimeProvider(NextUtcMidnight);
        await using var host = NewHost(
            _ => new MetadataLookupResult.Failed(EnrichmentFailureCategory.QuotaExhausted),
            clock);
        var movie = await host.AddMovieAsync(ct, EnrichmentStatus.Pending, deferredUntil: NextUtcMidnight.AddMinutes(-1));
        clock.ResetReads(); // seeding reads the clock for AddedAt; the scripted sequence must start at the consumer
        await host.Publisher.PublishAsync(
            new EnrichmentRequested(movie, 6),
            RabbitMqTopology.QuotaDeferredRoutingKey,
            new Dictionary<string, object?>(StringComparer.Ordinal),
            ct);
        await using var connection = await rabbit.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        var fetched = await channel.BasicGetAsync(RabbitMqTopology.QuotaDeferredQueue, autoAck: false, ct);
        fetched.ShouldNotBeNull();
        var delivery = new BasicDeliverEventArgs(
            "test",
            fetched.DeliveryTag,
            fetched.Redelivered,
            fetched.Exchange,
            fetched.RoutingKey,
            fetched.BasicProperties,
            fetched.Body,
            ct);

        var (recording, ackChannel) = PublishBeforeAckChannel.Wrap(channel, rabbit);

        // act
        await InvokeHandleAsync(host.Consumer, ackChannel, delivery, ct);

        // assert
        var published = await probe.PollGetMatchingAsync(
            RabbitMqTopology.RequestedQueue,
            candidate => Read(candidate).MovieId == movie,
            ct);
        published.ShouldNotBeNull();
        Read(published).Attempt.ShouldBe(6);
        (await probe.PollQuietMessageCountAsync(RabbitMqTopology.QuotaDeferredQueue, ct)).ShouldBe(0U);
        DeathField(published, "original-expiration").ShouldBe("0");
        DeathField(published, "reason").ShouldBe("expired");
        DeathField(published, "queue").ShouldBe(RabbitMqTopology.QuotaDeferredQueue);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.RequestedQueue, ct)).MessageCount.ShouldBe(0U);
        var persisted = await ReadPersistedAsync(movie, ct);
        persisted.EnrichmentDeferredUntil.ShouldNotBeNull();
        host.Provider.Calls.ShouldBe(1);
        recording.PublishSeenBeforeAck.ShouldBeTrue();
    }

    [Fact]
    public async Task Crash_BeforePublish_RedeliveryNoopsUntilExpiryThenSweeperOrDelayedPathRecovers()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var clock = new AdjustableTimeProvider(Now);
        await using var host = NewHost(_ => new MetadataLookupResult.Found(new MovieMetadata("Recovered")), clock);
        var deadline = Now.AddMinutes(30);
        var movie = await host.AddMovieAsync(ct, EnrichmentStatus.Pending, deferredUntil: deadline);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await WaitForQueueEmptyAsync(RabbitMqTopology.RequestedQueue, ct);

        // assert
        host.Provider.Calls.ShouldBe(0);
        (await ReadPersistedAsync(movie, ct)).EnrichmentDeferredUntil.ShouldBe(deadline);
        (await probe.PollQuietMessageCountAsync(RabbitMqTopology.QuotaDeferredQueue, ct)).ShouldBe(0U);

        await host.StopAsync(CancellationToken.None);
        clock.Set(deadline.AddMinutes(1));

        // act
        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await WaitForStatusAsync(movie, EnrichmentStatus.Enriched, ct);

        // assert
        host.Provider.Calls.ShouldBe(1);
        (await ReadPersistedAsync(movie, ct)).EnrichmentDeferredUntil.ShouldBeNull();
        await host.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Crash_AfterPublishBeforeAck_DuplicateDeliverySettlesOnce()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(
            _ => new MetadataLookupResult.Failed(EnrichmentFailureCategory.QuotaExhausted),
            new AdjustableTimeProvider(Now));
        var movie = await host.AddMovieAsync(ct, EnrichmentStatus.Pending, deferredUntil: Now.AddMinutes(-1));

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);

        // act
        await WaitUntilAsync(
            async () => await probe.PollMessageCountAsync(RabbitMqTopology.QuotaDeferredQueue, ct) == 1U,
            ct);

        // assert
        await WaitForQueueEmptyAsync(RabbitMqTopology.RequestedQueue, ct);
        host.Provider.Calls.ShouldBe(1);
        (await probe.PollMessageCountAsync(RabbitMqTopology.QuotaDeferredQueue, ct)).ShouldBe(1U);
        (await ReadPersistedAsync(movie, ct)).EnrichmentDeferredUntil.ShouldBe(NextUtcMidnight);
        await host.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ProcessOrConsumerRestart_PersistedDeferralSurvivesWithSameDeadline()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var deadline = Now.AddHours(3);
        MovieId movie;
        await using (var first = NewHost(_ => new MetadataLookupResult.NotFound(), new AdjustableTimeProvider(Now)))
        {
            movie = await first.AddMovieAsync(ct, EnrichmentStatus.Pending, deferredUntil: deadline);
            await first.StartAsync(ct);
            await first.StopAsync(CancellationToken.None);
        }

        // act
        await using var restarted = NewHost(
            _ => new MetadataLookupResult.Found(new MovieMetadata("Restarted")),
            new AdjustableTimeProvider(Now.AddMinutes(5)));

        // assert
        (await ReadPersistedAsync(movie, ct)).EnrichmentDeferredUntil.ShouldBe(deadline);
        (await probe.PollQuietMessageCountAsync(RabbitMqTopology.QuotaDeferredQueue, ct)).ShouldBe(0U);
    }

    [Fact]
    public async Task DelayedMessage_RetainsOriginalAttemptAcrossExpiry()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(_ => new MetadataLookupResult.NotFound(), new AdjustableTimeProvider(Now));
        var movie = await host.AddMovieAsync(ct, EnrichmentStatus.Pending, deferredUntil: Now.AddMinutes(-1));

        // act
        await host.Publisher.PublishAsync(
            new EnrichmentRequested(movie, 7),
            RabbitMqTopology.QuotaDeferredRoutingKey,
            new Dictionary<string, object?>(StringComparer.Ordinal),
            ct,
            expirationMilliseconds: 1000);

        // assert
        var delivered = await probe.PollGetMatchingAsync(
            RabbitMqTopology.RequestedQueue,
            candidate => Read(candidate).MovieId == movie,
            ct);
        delivered.ShouldNotBeNull();
        Read(delivered).Attempt.ShouldBe(7);
    }

    [Fact]
    public async Task EarlyStrayDelivery_BeforeDeferral_NoOpAckZeroProviderCallsUnchangedDeadlineZeroExtraPublish()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var deadline = Now.AddHours(2);
        await using var host = NewHost(
            _ => new MetadataLookupResult.Found(new MovieMetadata("Stray")),
            new AdjustableTimeProvider(Now));
        var movie = await host.AddMovieAsync(ct, EnrichmentStatus.Pending, deferredUntil: deadline);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 9), ct);
        await WaitForQueueEmptyAsync(RabbitMqTopology.RequestedQueue, ct);

        // assert
        host.Provider.Calls.ShouldBe(0);
        (await ReadPersistedAsync(movie, ct)).EnrichmentDeferredUntil.ShouldBe(deadline);
        (await probe.PollQuietMessageCountAsync(RabbitMqTopology.QuotaDeferredQueue, ct)).ShouldBe(0U);
        await host.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SaveBeforePublishBeforeAck_OrderingHoldsAndCancellationDuringLookupPropagates()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var host = NewHost(
            async (lookup, token) =>
            {
                if (lookup.Title == "Block")
                {
                    entered.TrySetResult();
                    await Task.Delay(Timeout.Infinite, token);
                }

                return lookup.Title == "Deferred"
                    ? new MetadataLookupResult.Failed(EnrichmentFailureCategory.QuotaExhausted)
                    : new MetadataLookupResult.Found(new MovieMetadata("Found"));
            },
            new AdjustableTimeProvider(Now));
        var deferred = await host.AddMovieAsync(ct, EnrichmentStatus.Pending, deferredUntil: Now.AddMinutes(-1), title: "Deferred");
        var blocked = await host.AddMovieAsync(ct, EnrichmentStatus.Pending, deferredUntil: Now.AddMinutes(-1), title: "Block");

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(deferred, 1), ct);
        await WaitUntilAsync(
            async () => await probe.PollMessageCountAsync(RabbitMqTopology.QuotaDeferredQueue, ct) == 1U,
            ct);

        // assert
        var persisted = await ReadPersistedAsync(deferred, ct);
        persisted.EnrichmentDeferredUntil.ShouldBe(NextUtcMidnight);
        persisted.Status.ShouldBe(EnrichmentStatus.Pending);

        await host.Publisher.EnqueueAsync(new EnrichmentRequested(blocked, 1), ct);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
        await host.StopAsync(CancellationToken.None);

        var requeued = await probe.PollGetMatchingAsync(
            RabbitMqTopology.RequestedQueue,
            candidate => Read(candidate).MovieId == blocked,
            ct);
        requeued.ShouldNotBeNull();
        (await ReadPersistedAsync(blocked, ct)).Status.ShouldBe(EnrichmentStatus.Pending);
    }

    [Fact]
    public async Task PostExpiryLostMessage_SweeperRecoversWithAttempt1Only()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(
            _ => new MetadataLookupResult.Found(new MovieMetadata("Swept")),
            new AdjustableTimeProvider(Now));
        var movie = await host.AddMovieAsync(ct, EnrichmentStatus.Pending, deferredUntil: Now.AddMinutes(-1));

        // act
        var sweep = new SweepStrandedMoviesCommandHandler(
            host.Repository,
            Options.Create(new EnrichmentOptions { MaxAttempts = DefaultMaxAttempts, ClaimLease = ClaimLease }),
            host.Time,
            new RequeueStrandedMoviesCommandHandler(host.Publisher));
        var requeued = await sweep.HandleAsync(new SweepStrandedMoviesCommand(), ct);

        // assert
        requeued.ShouldBe(1);
        var message = await probe.PollGetMatchingAsync(
            RabbitMqTopology.RequestedQueue,
            candidate => Read(candidate).MovieId == movie,
            ct);
        message.ShouldNotBeNull();
        Read(message).Attempt.ShouldBe(1);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(Read(message), ct);
        await WaitForStatusAsync(movie, EnrichmentStatus.Enriched, ct);
        host.Provider.Calls.ShouldBe(1);
        await host.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task DuplicateDelivery_SingleClaimWins()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(
            async (_, token) =>
            {
                await Task.Delay(TimeSpan.FromMilliseconds(300), token);
                return new MetadataLookupResult.Found(new MovieMetadata("Single"));
            },
            new AdjustableTimeProvider(Now));
        var movie = await host.AddMovieAsync(ct, EnrichmentStatus.Pending, deferredUntil: Now.AddMinutes(-1));

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);

        // act
        await WaitForStatusAsync(movie, EnrichmentStatus.Enriched, ct);
        await WaitForQueueEmptyAsync(RabbitMqTopology.RequestedQueue, ct);

        // assert
        host.Provider.Calls.ShouldBe(1);
        (await ReadPersistedAsync(movie, ct)).EnrichmentAttempts.ShouldBe(1);
        await host.StopAsync(CancellationToken.None);
    }

    private QuotaHost NewHost(
        Func<MetadataLookup, MetadataLookupResult> script,
        TimeProvider clock,
        int maxAttempts = DefaultMaxAttempts) =>
        NewHost((lookup, _) => Task.FromResult(script(lookup)), clock, maxAttempts);

    private QuotaHost NewHost(
        Func<MetadataLookup, CancellationToken, Task<MetadataLookupResult>> script,
        TimeProvider clock,
        int maxAttempts = DefaultMaxAttempts)
    {
        var db = fixture.CreateMigratedContext();
        var options = rabbit.Options with { Prefetch = 1 };
        var owner = new RabbitMqConnectionOwner(Options.Create(options));
        var enrichment = Options.Create(new EnrichmentOptions
        {
            MaxAttempts = maxAttempts,
            ClaimLease = ClaimLease,
        });
        var topology = new RabbitMqTopology(owner, Options.Create(options), enrichment);
        var publisher = new RabbitMqEnrichmentQueuePublisher(topology, owner);
        var repository = new EfMovieRepository(
            db,
            clock,
            enrichment,
            new RecordingEnrichmentStatusNotifier(),
            NullLogger<EfMovieRepository>.Instance);
        var provider = new DelegateProvider(script);
        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton(enrichment.Value);
        services.AddSingleton(enrichment);
        services.AddSingleton(clock);
        services.AddSingleton<IMetadataProvider>(provider);
        services.AddSingleton<IMovieRepository>(repository);
        services.AddSingleton<IEnrichmentQueue>(publisher);
        services.AddLogging();
        services.AddSingleton(owner);
        services.AddSingleton(topology);
        services.AddSingleton(publisher);
        services.AddScoped<ProcessEnrichmentCommandHandler>();
        services.AddScoped<ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>>(
            scope => scope.GetRequiredService<ProcessEnrichmentCommandHandler>());
        var built = services.BuildServiceProvider();
        var consumer = new EnrichmentConsumer(
            owner,
            topology,
            publisher,
            built.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            Options.Create(new ThumbnailOptions()),
            new RabbitMqThumbnailQueuePublisher(topology, owner),
            NullLogger<EnrichmentConsumer>.Instance,
            NoOpLiveness.Instance,
            clock);
        return new QuotaHost(db, owner, publisher, repository, provider, clock, consumer, built);
    }

    private async Task WaitForStatusAsync(MovieId id, EnrichmentStatus status, CancellationToken ct)
    {
        var deadline = TimeProvider.System.GetUtcNow() + TimeSpan.FromSeconds(30);
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            if (await ReadStatusAsync(id, ct) == status)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
        }

        (await ReadStatusAsync(id, ct)).ShouldBe(status);
    }

    private async Task<EnrichmentStatus> ReadStatusAsync(MovieId id, CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        return await db.Movies.AsNoTracking()
            .Where(movie => movie.Id == id)
            .Select(movie => movie.Status)
            .SingleAsync(ct);
    }

    private async Task<MovieRecordSnapshot> ReadPersistedAsync(MovieId id, CancellationToken ct)
    {
        await using var db = fixture.CreateMigratedContext();
        var movie = await db.Movies.AsNoTracking().SingleAsync(record => record.Id == id, ct);
        return new MovieRecordSnapshot(
            movie.Status,
            movie.EnrichmentDeferredUntil,
            movie.EnrichmentAttempts);
    }

    private async Task WaitForQueueEmptyAsync(string queue, CancellationToken ct) =>
        await WaitUntilAsync(
            async () => await probe.PollQuietMessageCountAsync(queue, ct) == 0U,
            ct);

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, CancellationToken ct)
    {
        var deadline = TimeProvider.System.GetUtcNow() + TimeSpan.FromSeconds(30);
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
        }

        (await condition()).ShouldBeTrue();
    }

    private static async Task InvokeHandleAsync(
        EnrichmentConsumer consumer,
        IChannel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken ct)
    {
        var method = typeof(EnrichmentConsumer).GetMethod(
            "HandleAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull();
        var invocation = method.Invoke(consumer, [channel, delivery, ct])
            ?? throw new InvalidOperationException("The enrichment consumer handle did not run.");
        await (Task)invocation;
    }

    public sealed class PublishObservation
    {
        public bool PublishSeenBeforeAck { get; set; }
    }

    public class PublishBeforeAckChannel : DispatchProxy
    {
        private static readonly ConditionalWeakTable<object, Initialization> Initializations = new();

        private sealed record Initialization(IChannel Channel, RabbitMqFixture Fixture, PublishObservation Observation);

        public static (PublishObservation Recording, IChannel Channel) Wrap(
            IChannel channel,
            RabbitMqFixture fixture)
        {
            var observation = new PublishObservation();
            IChannel proxy = Create<IChannel, PublishBeforeAckChannel>();
            Initializations.Add(proxy, new Initialization(channel, fixture, observation));
            return (observation, proxy);
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            ArgumentNullException.ThrowIfNull(args);
            if (!Initializations.TryGetValue(this, out var state))
            {
                throw new InvalidOperationException("The recording channel was not initialized.");
            }

            if (string.Equals(targetMethod.Name, nameof(IChannel.BasicAckAsync), StringComparison.Ordinal))
            {
                var (deliveryTag, multiple, cancellationToken) = ParseAckArgs(args);
                return ObservePublishThenAckAsync(state, deliveryTag, multiple, cancellationToken);
            }

            return targetMethod.Invoke(state.Channel, args);
        }

        private static (ulong DeliveryTag, bool Multiple, CancellationToken CancellationToken) ParseAckArgs(object?[] args)
        {
            if (args is not [ulong deliveryTag, bool multiple, ..])
            {
                throw new InvalidOperationException("Unexpected BasicAckAsync arguments.");
            }

            var cancellationToken = args is [_, _, CancellationToken requested]
                ? requested
                : CancellationToken.None;
            return (deliveryTag, multiple, cancellationToken);
        }

        private static async ValueTask ObservePublishThenAckAsync(Initialization state, ulong deliveryTag, bool multiple, CancellationToken cancellationToken)
        {
            state.Observation.PublishSeenBeforeAck = await WaitForPublishedAsync(state.Fixture, cancellationToken);
            await state.Channel.BasicAckAsync(deliveryTag, multiple, cancellationToken);
        }

        private static async Task<bool> WaitForPublishedAsync(RabbitMqFixture fixture, CancellationToken cancellationToken)
        {
            await using var observeConnection = await fixture.CreateConnectionAsync(cancellationToken);
            await using var observe = await observeConnection.CreateChannelAsync(cancellationToken: cancellationToken);
            var deadline = TimeProvider.System.GetUtcNow() + TimeSpan.FromSeconds(5);
            while (true)
            {
                var deferred = await observe.QueueDeclarePassiveAsync(
                    RabbitMqTopology.QuotaDeferredQueue,
                    cancellationToken: cancellationToken);
                var requested = await observe.QueueDeclarePassiveAsync(
                    RabbitMqTopology.RequestedQueue,
                    cancellationToken: cancellationToken);
                if (deferred.MessageCount + requested.MessageCount > 0)
                {
                    return true;
                }

                if (TimeProvider.System.GetUtcNow() >= deadline)
                {
                    return false;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }
        }
    }

    private static string? DeathField(BasicGetResult message, string field)
    {
        if (message.BasicProperties.Headers is not { } headers
            || !headers.TryGetValue("x-death", out var deaths)
            || deaths is not IList list)
        {
            return null;
        }

        foreach (var entry in list)
        {
            if (entry is IDictionary<string, object?> death
                && death.TryGetValue(field, out var raw))
            {
                return raw switch
                {
                    byte[] bytes => Encoding.UTF8.GetString(bytes),
                    string text => text,
                    _ => raw?.ToString(),
                };
            }
        }

        return null;
    }

    private static EnrichmentRequested Read(BasicGetResult result) =>
        JsonSerializer.Deserialize<EnrichmentRequested>(result.Body.Span, SerializerOptions)
        ?? throw new InvalidOperationException("The message was not an enrichment request.");

    private sealed record MovieRecordSnapshot(
        EnrichmentStatus Status,
        DateTimeOffset? EnrichmentDeferredUntil,
        int EnrichmentAttempts);

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset current = now;

        public override DateTimeOffset GetUtcNow() => current;

        public void Set(DateTimeOffset value) => current = value;
    }

    private sealed class EqualDeadlineTimeProvider(DateTimeOffset persisted) : TimeProvider
    {
        private int reads;

        public void ResetReads() => Volatile.Write(ref reads, 0);

        public override DateTimeOffset GetUtcNow() => Interlocked.Increment(ref reads) switch
        {
            1 => persisted.AddMinutes(-1),
            2 => persisted.AddSeconds(-1),
            _ => persisted,
        };
    }

    private sealed class DelegateProvider(
        Func<MetadataLookup, CancellationToken, Task<MetadataLookupResult>> script) : IMetadataProvider
    {
        private int calls;

        public int Calls => Volatile.Read(ref calls);

        public Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct)
        {
            Interlocked.Increment(ref calls);
            return script(lookup, ct);
        }
    }

    private sealed class NoOpLiveness : IWorkerLiveness
    {
        public static readonly NoOpLiveness Instance = new();

        public bool IsListening => true;

        public void MarkListening()
        {
        }

        public void MarkStopped()
        {
        }
    }

    private sealed class QuotaHost(
        LamuFlixDbContext db,
        RabbitMqConnectionOwner owner,
        RabbitMqEnrichmentQueuePublisher publisher,
        EfMovieRepository repository,
        DelegateProvider provider,
        TimeProvider time,
        EnrichmentConsumer consumer,
        ServiceProvider services) : IAsyncDisposable
    {
        public RabbitMqEnrichmentQueuePublisher Publisher => publisher;

        public EfMovieRepository Repository => repository;

        public DelegateProvider Provider => provider;

        public EnrichmentConsumer Consumer => consumer;

        public TimeProvider Time => time;

        public Task StartAsync(CancellationToken ct) => consumer.StartAsync(ct);

        public Task StopAsync(CancellationToken ct) => consumer.StopAsync(ct);

        public async Task<MovieId> AddMovieAsync(
            CancellationToken ct,
            EnrichmentStatus status,
            DateTimeOffset? deferredUntil,
            string title = "Deferred")
        {
            var id = await repository.NextIdentityAsync(ct);
            var movie = Movie.Rehydrate(
                id,
                title,
                new LibraryPath($"C:/library/{id.Value}"),
                new MediaFormat("mkv"),
                false,
                null,
                status,
                status == EnrichmentStatus.Enriched ? time.GetUtcNow() : null,
                0,
                null,
                null,
                enrichmentDeferredUntil: deferredUntil);
            await repository.AddAsync(movie, ct);
            await repository.SaveChangesAsync(ct);
            return id;
        }

        public async ValueTask DisposeAsync()
        {
            await services.DisposeAsync();
            await db.DisposeAsync();
            await owner.DisposeAsync();
        }
    }

}
