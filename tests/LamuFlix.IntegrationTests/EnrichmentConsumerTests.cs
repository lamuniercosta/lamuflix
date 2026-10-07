using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Pipeline;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(RabbitMqCollection))]
public sealed class EnrichmentConsumerTests(RabbitMqFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan ClaimLease = TimeSpan.FromMilliseconds(500);
    private const int DefaultMaxAttempts = 3;

    private readonly RabbitMqProbe probe = new(fixture);

    public async ValueTask InitializeAsync() => await fixture.ResetTopologyAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Consumer_ARetryableOutcome_ReclaimsAndCallsTheProviderAgain()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(_ => new MetadataLookupResult.Failed(EnrichmentFailureCategory.ProviderUnavailable));
        var movie = host.AddPendingMovie(101);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await host.WaitForProviderCallsAsync(ct, 2);
        await host.StopAsync(CancellationToken.None);

        host.Repository.Find(movie).ShouldNotBeNull().EnrichmentAttempts.ShouldBe(2);
        (await probe.PollGetAsync(RabbitMqTopology.DeadLetterQueue, ct)).ShouldBeNull();
    }

    [Fact]
    public async Task Consumer_ADelayedRetryOutcome_ReclaimsAndCallsTheProviderAgain()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(_ => new MetadataLookupResult.Failed(EnrichmentFailureCategory.RateLimited));
        var movie = host.AddPendingMovie(102);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await host.WaitForProviderCallsAsync(ct, 2);
        await host.StopAsync(CancellationToken.None);

        host.Repository.Find(movie).ShouldNotBeNull().EnrichmentAttempts.ShouldBe(2);
    }

    [Fact]
    public async Task Consumer_ASuccessfulOutcome_IsAckedAndTheMovieIsEnriched()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(_ => new MetadataLookupResult.Found(new MovieMetadata("Enriched")));
        var movie = host.AddPendingMovie(103);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await host.WaitForStatusAsync(ct, movie, EnrichmentStatus.Enriched);
        await host.StopAsync(CancellationToken.None);

        host.Repository.Find(movie).ShouldNotBeNull().Status.ShouldBe(EnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task Consumer_ATerminalOutcome_ReachesTheDeadLetterQueueAndMarksTheMovieFailed()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(_ => new MetadataLookupResult.Failed(EnrichmentFailureCategory.InvalidResponse));
        var movie = host.AddPendingMovie(104);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, DefaultMaxAttempts), ct);
        var dead = await probe.PollGetMatchingAsync(
            RabbitMqTopology.DeadLetterQueue,
            candidate => Read(candidate).MovieId.Value == 104,
            ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        host.Repository.Find(movie).ShouldNotBeNull().Status.ShouldBe(EnrichmentStatus.Failed);
    }

    [Fact]
    public async Task Consumer_AMalformedBody_ReachesTheDeadLetterQueue()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(_ => new MetadataLookupResult.NotFound());
        await host.Topology.EnsureDeclaredAsync(ct);
        await probe.PublishAsync(RabbitMqTopology.RequestedRoutingKey, "not json"u8.ToArray(), ct);

        await host.StartAsync(ct);
        var dead = await probe.PollGetAsync(RabbitMqTopology.DeadLetterQueue, ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
    }

    [Fact]
    public async Task Consumer_ARefusedClaim_IsAckedAndNeverEnriched()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(_ => new MetadataLookupResult.Found(new MovieMetadata("Enriched")));
        var movie = new MovieId(105);
        await host.Repository.AddAsync(
            Movie.Rehydrate(
                movie,
                "Held",
                new LibraryPath("C:/library/a"),
                new MediaFormat("mkv"),
                false,
                null,
                EnrichmentStatus.Pending,
                null,
                0,
                null,
                host.Time.GetUtcNow()),
            ct);

        await host.StartAsync(ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await host.WaitForProviderCallsAsync(ct, 0);
        await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
        await host.StopAsync(CancellationToken.None);

        host.Provider.Calls.ShouldBe(0);
        host.Repository.Find(movie).ShouldNotBeNull().Status.ShouldBe(EnrichmentStatus.Pending);
        host.Repository.Find(movie).ShouldNotBeNull().EnrichmentAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task Consumer_APrefetchOfOne_LeavesTheSecondMessageReady()
    {
        var ct = TestContext.Current.CancellationToken;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var host = NewHost(
            _ =>
            {
                entered.TrySetResult();
                release.Task.Wait(ct);
                return new MetadataLookupResult.NotFound();
            },
            prefetch: 1);
        var movie = host.AddPendingMovie(106);
        await host.Topology.EnsureDeclaredAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.RequestedQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.RetryQueue, ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);

        await host.StartAsync(ct);
        await entered.Task.WaitAsync(ct);
        try
        {
            (await probe.PollMessageCountAsync(RabbitMqTopology.RequestedQueue, ct)).ShouldBe(1U);
        }
        finally
        {
            release.TrySetResult();
        }

        await host.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Consumer_AFailedRepublish_NeverSuccessAcksAndLeavesTheMoviePending()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(_ => new MetadataLookupResult.Failed(EnrichmentFailureCategory.Unknown));
        var movie = host.AddPendingMovie(107);
        await host.Topology.EnsureDeclaredAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.RequestedQueue, ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        await host.CloseRetryQueueAsync(ct);

        await host.StartAsync(ct);
        var dead = await probe.PollGetMatchingAsync(
            RabbitMqTopology.DeadLetterQueue,
            candidate => Read(candidate).MovieId.Value == 107,
            ct);
        await host.StopAsync(CancellationToken.None);

        dead.ShouldNotBeNull();
        host.Repository.Find(movie).ShouldNotBeNull().Status.ShouldBe(EnrichmentStatus.Pending);
    }

    [Fact]
    public async Task Consumer_CancellationRequeues_AndDoesNotMarkTheMovieFailed()
    {
        var ct = TestContext.Current.CancellationToken;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var host = NewHost(_ =>
        {
            gate.TrySetResult();
            return new MetadataLookupResult.Failed(EnrichmentFailureCategory.Unknown);
        });
        var movie = host.AddPendingMovie(108);
        await host.Topology.EnsureDeclaredAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.RequestedQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.RetryQueue, ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);

        await host.StartAsync(ct);
        await gate.Task.WaitAsync(ct);
        await host.StopAsync(CancellationToken.None);
        (await probe.PollGetMatchingAsync(
            RabbitMqTopology.RequestedQueue,
            candidate => Read(candidate).MovieId.Value == 108,
            ct)).ShouldNotBeNull();
        host.Repository.Find(movie).ShouldNotBeNull().Status.ShouldBe(EnrichmentStatus.Pending);
    }

    [Fact]
    public async Task Consumer_ADroppedConnection_ResumesConsumptionAfterReconnect()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(_ => new MetadataLookupResult.Found(new MovieMetadata("Enriched")));
        var first = host.AddPendingMovie(201);
        await host.Topology.EnsureDeclaredAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.RequestedQueue, ct);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(first, 1), ct);

        await host.StartAsync(ct);
        await host.WaitForStatusAsync(ct, first, EnrichmentStatus.Enriched);

        var connection = await host.Owner.GetAsync(ct);
        await connection.DisposeAsync();

        var second = host.AddPendingMovie(202);
        await host.Publisher.EnqueueAsync(new EnrichmentRequested(second, 1), ct);
        await host.WaitForStatusAsync(ct, second, EnrichmentStatus.Enriched);
        await host.StopAsync(CancellationToken.None);

        host.Repository.Find(second).ShouldNotBeNull().Status.ShouldBe(EnrichmentStatus.Enriched);
    }

    [Fact]
    public async Task Consumer_ABrokerUnreachableAtBoot_StartsAndStopsWithoutThrowing()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = NewHost(
            _ => new MetadataLookupResult.Found(new MovieMetadata("Enriched")),
            rabbitOptions: fixture.Options with { Port = ClosedPort() });

        await host.StartAsync(ct);
        await host.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Consumer_TheConsumerActivity_SharesTheProducerTraceId()
    {
        var ct = TestContext.Current.CancellationToken;
        var observed = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener();
        listener.ShouldListenTo = source => source.Name == TelemetryConstants.ActivitySourceName;
        SampleActivity<ActivityContext> sample = (ref _) => ActivitySamplingResult.AllData;
        listener.Sample = sample;
        listener.ActivityStopped = activity => observed.Enqueue(activity);
        ActivitySource.AddActivityListener(listener);

        await using var host = NewHost(_ => new MetadataLookupResult.NotFound());
        var movie = host.AddPendingMovie(109);
        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        const string traceState = "lamu=initial";
        using (var ambient = new Activity("publish")
                   .SetIdFormat(ActivityIdFormat.W3C)
                   .SetParentId($"00-{traceId}-{spanId}-01"))
        {
            ambient.TraceStateString = traceState;
            ambient.Start();
            await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        }

        var producer = observed.Single(activity => IsProducerActivity(activity, traceId));

        await host.StartAsync(ct);
        await host.WaitForStatusAsync(ct, movie, EnrichmentStatus.NotFound);
        var consumer = (await WaitForActivitiesAsync(
            observed,
            activity => IsConsumerActivity(activity, traceId),
            1,
            ct))[0];
        await host.StopAsync(CancellationToken.None);

        producer.TraceStateString.ShouldBe(traceState);
        consumer.Kind.ShouldBe(ActivityKind.Consumer);
        consumer.TraceId.ShouldBe(traceId);
        consumer.ParentSpanId.ShouldBe(producer.SpanId);
        consumer.TraceStateString.ShouldBe(traceState);
        consumer.Links.ShouldBeEmpty();
        consumer.GetTagItem(TelemetryConstants.MessagingDeliveryCount).ShouldBe(0);
    }

    [Fact]
    public async Task Consumer_ABrokerRedelivery_KeepsTheProducerParentAndCarriesOneOriginalContextLink()
    {
        var ct = TestContext.Current.CancellationToken;
        Convert.ToInt32(RabbitMqTopology.RequestedArguments(DefaultMaxAttempts)["x-delivery-limit"])
            .ShouldBeGreaterThanOrEqualTo(2);

        var observed = new ConcurrentQueue<Activity>();
        using var listener = new ActivityListener();
        listener.ShouldListenTo = source => source.Name == TelemetryConstants.ActivitySourceName;
        SampleActivity<ActivityContext> sample = (ref _) => ActivitySamplingResult.AllData;
        listener.Sample = sample;
        listener.ActivityStopped = activity => observed.Enqueue(activity);
        ActivitySource.AddActivityListener(listener);

        var calls = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var host = NewHost(_ =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.TrySetResult();
                release.Task.Wait(ct);
            }

            return new MetadataLookupResult.Found(new MovieMetadata("Enriched"));
        });
        await using var restarted = NewHost(_ => new MetadataLookupResult.Found(new MovieMetadata("Enriched")));
        var movie = host.AddPendingMovie(110);
        restarted.AddPendingMovie(110);
        await host.Topology.EnsureDeclaredAsync(ct);
        await probe.DrainAsync(RabbitMqTopology.RequestedQueue, ct);
        await probe.DrainAsync(RabbitMqTopology.RetryQueue, ct);

        var traceId = ActivityTraceId.CreateRandom();
        var spanId = ActivitySpanId.CreateRandom();
        const string traceState = "lamu=redelivery";
        using (var ambient = new Activity("publish")
                   .SetIdFormat(ActivityIdFormat.W3C)
                   .SetParentId($"00-{traceId}-{spanId}-01"))
        {
            ambient.TraceStateString = traceState;
            ambient.Start();
            await host.Publisher.EnqueueAsync(new EnrichmentRequested(movie, 1), ct);
        }

        var producer = observed.Single(activity => IsProducerActivity(activity, traceId));

        await host.StartAsync(ct);
        await entered.Task.WaitAsync(ct);
        var stopped = host.StopAsync(CancellationToken.None);
        try
        {
            (await probe.PollMessageCountAsync(RabbitMqTopology.RequestedQueue, ct)).ShouldBe(1U);

            await restarted.StartAsync(ct);
            var deliveries = await WaitForActivitiesAsync(
                observed,
                activity => IsConsumerActivity(activity, traceId),
                1,
                ct);
            var redelivered = deliveries.MaxBy(activity => activity.StartTimeUtc).ShouldNotBeNull();

            redelivered.Kind.ShouldBe(ActivityKind.Consumer);
            redelivered.TraceId.ShouldBe(traceId);
            redelivered.ParentSpanId.ShouldBe(producer.SpanId);
            redelivered.TraceStateString.ShouldBe(traceState);
            var link = redelivered.Links.ShouldHaveSingleItem();
            link.Context.TraceId.ShouldBe(traceId);
            link.Context.SpanId.ShouldBe(producer.SpanId);
            link.Context.TraceState.ShouldBe(traceState);
            redelivered.GetTagItem(TelemetryConstants.MessagingDeliveryCount).ShouldBe(1);

            producer.TraceStateString.ShouldBe(traceState);
            restarted.Repository.Find(movie).ShouldNotBeNull().Status.ShouldBe(EnrichmentStatus.Enriched);
        }
        finally
        {
            release.TrySetResult();
        }

        await stopped;
        await restarted.StopAsync(CancellationToken.None);
        (await probe.DeclarePassiveAsync(RabbitMqTopology.RequestedQueue, ct)).MessageCount.ShouldBe(0U);
    }

    private static int ClosedPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static EnrichmentRequested Read(BasicGetResult message) =>
        JsonSerializer.Deserialize<EnrichmentRequested>(
            Encoding.UTF8.GetString(message.Body.Span),
            new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldNotBeNull();

    private static bool IsProducerActivity(Activity activity, ActivityTraceId traceId) =>
        activity.Source.Name == TelemetryConstants.ActivitySourceName
        && activity.OperationName == TelemetryConstants.EnrichmentEnqueue
        && activity.TraceId == traceId;

    private static bool IsConsumerActivity(Activity activity, ActivityTraceId traceId) =>
        activity.Source.Name == TelemetryConstants.ActivitySourceName
        && activity.OperationName == TelemetryConstants.EnrichmentProcess
        && activity.TraceId == traceId;

    private static async Task<IReadOnlyList<Activity>> WaitForActivitiesAsync(
        ConcurrentQueue<Activity> observed,
        Func<Activity, bool> match,
        int count,
        CancellationToken cancellationToken)
    {
        var deadline = TimeProvider.System.GetUtcNow() + TimeSpan.FromSeconds(30);
        IReadOnlyList<Activity> found = [];
        while (TimeProvider.System.GetUtcNow() < deadline)
        {
            found = [.. observed.Where(match)];
            if (found.Count >= count)
            {
                return found;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }

        found.Count.ShouldBeGreaterThanOrEqualTo(count);
        return found;
    }

    private ConsumerHost NewHost(
        Func<MetadataLookup, MetadataLookupResult> script,
        int maxAttempts = DefaultMaxAttempts,
        ushort prefetch = 1,
        RabbitMqOptions? rabbitOptions = null)
    {
        var clock = new AdjustableTimeProvider(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));
        var provider = new ScriptedMetadataProvider(script);
        var repository = new LeaseAwareMovieRepository(clock, ClaimLease);
        var options = (rabbitOptions ?? fixture.Options) with { Prefetch = prefetch };
        var owner = new RabbitMqConnectionOwner(Options.Create(options));
        var enrichment = Options.Create(new EnrichmentOptions { MaxAttempts = maxAttempts, ClaimLease = ClaimLease });
        var topology = new RabbitMqTopology(owner, Options.Create(options), enrichment);
        var publisher = new RabbitMqEnrichmentQueuePublisher(topology, owner);
        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton(enrichment.Value);
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(enrichment);
        services.AddSingleton<IMetadataProvider>(provider);
        services.AddSingleton<IMovieRepository>(repository);
        services.AddLogging();
        services.AddSingleton<RabbitMqConnectionOwner>(_ => owner);
        services.AddSingleton(topology);
        services.AddSingleton(publisher);
        services.AddScoped<ProcessEnrichmentCommandHandler>();
        services.AddScoped<ProcessEnrichmentCommandValidator>();
        services.AddScoped<IValidator<ProcessEnrichmentCommand>>(p =>
            p.GetRequiredService<ProcessEnrichmentCommandValidator>());
        services.AddHandler<ProcessEnrichmentCommandHandler, ProcessEnrichmentCommand, ProcessEnrichmentOutcome>();
        var built = services.BuildServiceProvider();
        var consumer = new EnrichmentConsumer(
            owner,
            topology,
            publisher,
            built.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            NullLogger<EnrichmentConsumer>.Instance);
        return new ConsumerHost(owner, topology, publisher, repository, provider, clock, consumer, built);
    }

    private sealed class ConsumerHost(
        RabbitMqConnectionOwner owner,
        RabbitMqTopology topology,
        RabbitMqEnrichmentQueuePublisher publisher,
        LeaseAwareMovieRepository repository,
        ScriptedMetadataProvider provider,
        AdjustableTimeProvider time,
        EnrichmentConsumer consumer,
        ServiceProvider services) : IAsyncDisposable
    {
        public RabbitMqConnectionOwner Owner => owner;

        public RabbitMqTopology Topology { get; } = topology;

        public RabbitMqEnrichmentQueuePublisher Publisher { get; } = publisher;

        public LeaseAwareMovieRepository Repository { get; } = repository;

        public ScriptedMetadataProvider Provider { get; } = provider;

        public AdjustableTimeProvider Time { get; } = time;

        public Task StartAsync(CancellationToken cancellationToken) => consumer.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) => consumer.StopAsync(cancellationToken);

        public MovieId AddPendingMovie(int id)
        {
            var movieId = new MovieId(id);
            Repository.Add(
                Movie.Create(movieId, $"Movie {id}", new LibraryPath("C:/library/a"), new MediaFormat("mkv")));
            return movieId;
        }

        public async Task WaitForProviderCallsAsync(CancellationToken cancellationToken, int expected)
        {
            var clock = TimeProvider.System;
            var deadline = clock.GetUtcNow() + TimeSpan.FromSeconds(30);
            while (Provider.Calls < expected && clock.GetUtcNow() < deadline)
            {
                Time.Advance(ClaimLease + TimeSpan.FromMilliseconds(250));
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }

            Provider.Calls.ShouldBeGreaterThanOrEqualTo(expected);
        }

        public async Task WaitForStatusAsync(CancellationToken cancellationToken, MovieId id, EnrichmentStatus status)
        {
            var clock = TimeProvider.System;
            var deadline = clock.GetUtcNow() + TimeSpan.FromSeconds(30);
            while (clock.GetUtcNow() < deadline)
            {
                if (Repository.Find(id)?.Status == status)
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }

            Repository.Find(id).ShouldNotBeNull().Status.ShouldBe(status);
        }

        public async Task CloseRetryQueueAsync(CancellationToken cancellationToken)
        {
            var connection = await owner.GetAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.QueueDeleteAsync(RabbitMqTopology.RetryQueue, cancellationToken: cancellationToken);
        }

        public ValueTask DisposeAsync()
        {
            services.Dispose();
            return owner.DisposeAsync();
        }
    }

    private sealed class AdjustableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private readonly Lock sync = new();
        private DateTimeOffset utcNow = now;

        public override DateTimeOffset GetUtcNow()
        {
            lock (sync)
            {
                return utcNow;
            }
        }

        public void Advance(TimeSpan amount)
        {
            lock (sync)
            {
                utcNow += amount;
            }
        }
    }
}





