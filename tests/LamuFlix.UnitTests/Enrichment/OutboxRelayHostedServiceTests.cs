using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Enrichment;
using LamuFlix.Infrastructure.Persistence.Records;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LamuFlix.UnitTests.Enrichment;

public sealed class OutboxRelayHostedServiceTests
{
    private const string WorkerTypeName = "LamuFlix.Infrastructure.Enrichment.OutboxRelayHostedService";

    [Fact]
    public async Task Harness_ProvidesDeterministicTimeOptionsStateQueueAndCapturedLogs()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create();

        // act
        var state = harness.Services.GetRequiredService<OutboxRelayState>();

        // assert
        harness.Time.GetUtcNow().ShouldBe(OutboxRelayHarness.StartedAt);
        harness.Options.ShouldBe(new OutboxRelayOptions());
        state.ShouldBeSameAs(harness.Services.GetRequiredService<OutboxRelayState>());
        harness.Queue.ShouldNotBeNull();
        harness.Logs.Entries.ShouldNotBeNull();
    }

    [Fact]
    public async Task RelayWorker_IsImplementedSoTheHarnessCanProvePollingBackoffAndNoOverlap()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create();

        // act
        var workerType = OutboxRelayHarness.RelayWorkerType();

        // assert
        workerType.ShouldNotBeNull(
            $"T009 must add {WorkerTypeName} before this harness can prove immediate polling, success delays, " +
            "backoff saturation and reset, linked publish timeout versus shutdown, clean cancellation, and the " +
            "in-flight pass preventing a second pass despite elapsed due time (FR-001).");
        harness.CreateWorker().ShouldBeAssignableTo<BackgroundService>();
    }

    [Fact]
    public async Task Harness_AppliesConfiguredOptionOverridesThroughTheProductionBinding()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create(new Dictionary<string, string?>
        {
            [$"{OutboxRelayOptions.SectionName}:BatchSize"] = "3",
            [$"{OutboxRelayOptions.SectionName}:PollInterval"] = "00:00:00.500",
            [$"{OutboxRelayOptions.SectionName}:FailureBackoffMax"] = "00:00:30",
        });

        // act
        var timeProvider = harness.Services.GetRequiredService<TimeProvider>();

        // assert
        harness.Options.BatchSize.ShouldBe(3);
        harness.Options.PollInterval.ShouldBe(TimeSpan.FromMilliseconds(500));
        harness.Options.FailureBackoffMax.ShouldBe(TimeSpan.FromSeconds(30));
        timeProvider.ShouldBeSameAs(harness.Time);
    }

    [Fact]
    public async Task RelayPass_AbortsOnFirstFailureAndKeepsTheCommittedPrefix()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create();
        var worker = harness.CreateScriptedWorker();
        var first = Rows.Valid(1, movieId: 11);
        var second = Rows.Valid(2, movieId: 12);
        worker.Read = _ => Task.FromResult<IReadOnlyList<OutboxMessageRecord>>([first, second]);
        var published = new List<MovieId>();
        var publishCalls = 0;
        harness.Queue.EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                publishCalls++;
                if (publishCalls > 1)
                {
                    return Task.FromException(new InvalidOperationException("publish failed"));
                }

                published.Add(callInfo.Arg<EnrichmentRequested>().MovieId);
                return Task.CompletedTask;
            });

        // act
        var outcome = await worker.RunPassAsync(TestContext.Current.CancellationToken);

        // assert
        outcome.ShouldBe(OutboxRelayPassResult.Failed);
        published.ShouldBe(new[] { new MovieId(11) });
        worker.SavedRows.ShouldBe(new[] { 1L });
        second.ProcessedAt.ShouldBeNull();
        harness.Logs.Entries.ShouldContain(entry =>
            entry.Message.Contains("row 2", StringComparison.Ordinal)
            && entry.Message.Contains("publish", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RelayPass_PoisonOldestRowBlocksLaterRowsAndRecoversThroughAFreshScope()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create();
        var worker = harness.CreateScriptedWorker();
        var poison = Rows.WithPayload(1, "{\"movieId\":\"poison-sentinel\"}"u8.ToArray());
        var later = Rows.Valid(2, movieId: 22);
        worker.Read = _ => Task.FromResult<IReadOnlyList<OutboxMessageRecord>>([poison, later]);

        // act
        var firstOutcome = await worker.RunPassAsync(TestContext.Current.CancellationToken);
        var secondOutcome = await worker.RunPassAsync(TestContext.Current.CancellationToken);

        // assert
        firstOutcome.ShouldBe(OutboxRelayPassResult.Failed);
        secondOutcome.ShouldBe(OutboxRelayPassResult.Failed);
        harness.Published.ShouldBeEmpty();
        worker.SavedRows.ShouldBeEmpty();
        worker.ReadCalls.ShouldBe(2);
        worker.ScopedProviders.Distinct().Count().ShouldBe(2);
        later.ProcessedAt.ShouldBeNull();
    }

    [Fact]
    public async Task RelayLoop_AppliesCappedExponentialBackoffAndResetsAfterASuccessfulEmptyPoll()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create(new Dictionary<string, string?>
        {
            [$"{OutboxRelayOptions.SectionName}:PollInterval"] = "00:00:01",
            [$"{OutboxRelayOptions.SectionName}:FailureBackoffMin"] = "00:00:01",
            [$"{OutboxRelayOptions.SectionName}:FailureBackoffMax"] = "00:00:04",
        });
        var worker = harness.CreateScriptedWorker();
        var failing = true;
        worker.Read = _ => failing
            ? Task.FromException<IReadOnlyList<OutboxMessageRecord>>(new InvalidOperationException("read failed"))
            : Task.FromResult<IReadOnlyList<OutboxMessageRecord>>([]);
        using var cancellation = new CancellationTokenSource();
        await worker.StartAsync(cancellation.Token);
        await harness.WaitForScheduledDelayAsync();

        async Task expectDelayAsync(TimeSpan expectedDelay, int readsAfter)
        {
            await harness.AdvanceAndWaitAsync(expectedDelay - TimeSpan.FromMilliseconds(1), waitForNextDelay: false);
            worker.ReadCalls.ShouldBe(readsAfter - 1);
            await harness.AdvanceAndWaitAsync(TimeSpan.FromMilliseconds(1), waitForNextDelay: true);
            worker.ReadCalls.ShouldBe(readsAfter);
        }

        // act / assert
        worker.ReadCalls.ShouldBe(1);
        await expectDelayAsync(TimeSpan.FromSeconds(1), 2);
        await expectDelayAsync(TimeSpan.FromSeconds(2), 3);
        await expectDelayAsync(TimeSpan.FromSeconds(4), 4);
        await expectDelayAsync(TimeSpan.FromSeconds(4), 5);

        failing = false;
        await expectDelayAsync(TimeSpan.FromSeconds(4), 6);

        failing = true;
        await expectDelayAsync(TimeSpan.FromSeconds(1), 7);
        await expectDelayAsync(TimeSpan.FromSeconds(1), 8);

        await worker.StopAsync(cancellation.Token);
    }

    [Fact]
    public async Task RelayLoop_DoesNotStartASecondPassWhileOneIsInFlight()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create();
        var worker = harness.CreateScriptedWorker();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        worker.Read = async cancellationToken =>
        {
            await gate.Task.WaitAsync(cancellationToken);
            return [];
        };
        using var cancellation = new CancellationTokenSource();
        await worker.StartAsync(cancellation.Token);
        await worker.FirstRead.WaitAsync(TestContext.Current.CancellationToken);

        // act
        harness.Time.Advance(TimeSpan.FromHours(1));

        // assert
        worker.ReadCalls.ShouldBe(1);
        gate.SetResult();
        await harness.WaitForScheduledDelayAsync();
        worker.ReadCalls.ShouldBe(1);
        await worker.StopAsync(cancellation.Token);
    }

    [Fact]
    public async Task RelayLoop_ExitsCleanlyOnShutdownWithoutSyntheticFailureOrRetryDelay()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create();
        var worker = harness.CreateScriptedWorker();
        worker.Read = async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return [];
        };
        using var cancellation = new CancellationTokenSource();
        await worker.StartAsync(cancellation.Token);
        await worker.FirstRead.WaitAsync(TestContext.Current.CancellationToken);

        // act
        await worker.StopAsync(cancellation.Token);

        // assert
        harness.Logs.Entries.ShouldBeEmpty();
        harness.Time.HasPendingTimer.ShouldBeFalse();
        harness.Services.GetRequiredService<OutboxRelayState>().Current.Lifecycle
            .ShouldBe(OutboxRelayLifecycle.Stopped);
    }

    [Fact]
    public async Task RelayPass_PublishTimeoutFailsTheRowButShutdownDoesNot()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create(new Dictionary<string, string?>
        {
            [$"{OutboxRelayOptions.SectionName}:PublishTimeout"] = "00:00:01",
        });
        var worker = harness.CreateScriptedWorker();
        var row = Rows.Valid(7, movieId: 70);
        worker.Read = _ => Task.FromResult<IReadOnlyList<OutboxMessageRecord>>([row]);
        var publishStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Queue.EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                publishStarted.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, callInfo.Arg<CancellationToken>());
            });

        // act
        var pass = worker.RunPassAsync(TestContext.Current.CancellationToken);
        await publishStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        harness.Time.Advance(TimeSpan.FromSeconds(1));
        var outcome = await pass;

        // assert
        outcome.ShouldBe(OutboxRelayPassResult.Failed);
        row.ProcessedAt.ShouldBeNull();
        worker.SavedRows.ShouldBeEmpty();
        harness.Logs.Entries.ShouldContain(entry =>
            entry.Message.Contains("publish-timeout", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RelayFailureLogs_ExposeOnlyRowIdCategoryAndExceptionTypeName()
    {
        // arrange
        const string payloadSentinel = "payload-secret-sentinel";
        const string secretSentinel = "connection-secret-sentinel";
        await using var harness = OutboxRelayHarness.Create();
        var worker = harness.CreateScriptedWorker();
        worker.Read = _ => Task.FromException<IReadOnlyList<OutboxMessageRecord>>(
            new InvalidOperationException(
                $"read failed with {payloadSentinel}",
                new InvalidOperationException($"inner {secretSentinel}")));

        // act
        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        // assert
        harness.Logs.Entries.ShouldNotBeEmpty();
        assertAllowlisted(harness.Logs.Entries, [payloadSentinel, secretSentinel]);
        var readEntry = harness.Logs.Entries.ShouldHaveSingleItem();
        readEntry.Message.ShouldContain("read");
        readEntry.Message.ShouldContain(nameof(InvalidOperationException));
        readEntry.Message.ShouldNotContain("RowId");
    }

    [Fact]
    public async Task RowScopedFailureLogs_IncludeTheRowIdAndNeverThePayloadOrSecret()
    {
        // arrange
        const string payloadSentinel = "payload-secret-sentinel";
        const string secretSentinel = "connection-secret-sentinel";
        await using var harness = OutboxRelayHarness.Create();
        var worker = harness.CreateScriptedWorker();
        worker.Read = _ => Task.FromResult<IReadOnlyList<OutboxMessageRecord>>(
            [Rows.Valid(4242, movieId: 42)]);
        harness.Queue.EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(
                new InvalidOperationException(
                    $"publish failed with {payloadSentinel}",
                    new InvalidOperationException($"inner {secretSentinel}"))));

        // act
        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        // assert
        assertAllowlisted(harness.Logs.Entries, [payloadSentinel, secretSentinel]);
        var publishEntry = harness.Logs.Entries.ShouldHaveSingleItem();
        publishEntry.Message.ShouldContain("4242");
        publishEntry.Message.ShouldContain("publish");
        publishEntry.Message.ShouldContain(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task DeserializeFailureLogs_NeverEchoTheStoredPayload()
    {
        // arrange
        const string payloadSentinel = "payload-secret-sentinel";
        await using var harness = OutboxRelayHarness.Create();
        var worker = harness.CreateScriptedWorker();
        worker.Read = _ => Task.FromResult<IReadOnlyList<OutboxMessageRecord>>(
            [Rows.WithPayload(99, JsonSerializer.SerializeToUtf8Bytes(new { movieId = payloadSentinel }))]);

        // act
        await worker.RunPassAsync(TestContext.Current.CancellationToken);

        // assert
        assertAllowlisted(harness.Logs.Entries, [payloadSentinel]);
        var deserializeEntry = harness.Logs.Entries.ShouldHaveSingleItem();
        deserializeEntry.Message.ShouldContain("99");
        deserializeEntry.Message.ShouldContain("deserialize");
        deserializeEntry.Message.ShouldContain(nameof(JsonException));
    }

    private static void assertAllowlisted(
        IEnumerable<RecordedLogEntry> entries,
        IReadOnlyCollection<string> forbiddenValues)
    {
        foreach (var entry in entries)
        {
            entry.Exception.ShouldBeNull();
            foreach (var forbidden in forbiddenValues)
            {
                entry.Message.ShouldNotContain(forbidden);
            }
        }
    }

    private static class Rows
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public static OutboxMessageRecord Valid(long id, int movieId, int attempt = 1) =>
            WithPayload(
                id,
                JsonSerializer.SerializeToUtf8Bytes(
                    new EnrichmentRequested(new MovieId(movieId), attempt),
                    SerializerOptions));

        public static OutboxMessageRecord WithPayload(long id, byte[] payload) =>
            new()
            {
                Id = id,
                Payload = payload,
                CreatedAt = OutboxRelayHarness.StartedAt,
            };
    }

    internal sealed class ScriptedOutboxRelayWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxRelayOptions> options,
        TimeProvider timeProvider,
        IEnrichmentQueue queue,
        OutboxRelayState state,
        ILogger<OutboxRelayHostedService> logger)
        : OutboxRelayHostedService(scopeFactory, options, timeProvider, queue, state, logger)
    {
        private readonly ConcurrentQueue<long> savedRows = new();
        private readonly ConcurrentQueue<IServiceProvider> scopedProviders = new();
        private readonly TaskCompletionSource firstRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int readCalls;

        public Func<CancellationToken, Task<IReadOnlyList<OutboxMessageRecord>>> Read { get; set; } =
            _ => Task.FromResult<IReadOnlyList<OutboxMessageRecord>>([]);

        public Func<OutboxMessageRecord, DateTimeOffset, CancellationToken, Task> Save { get; set; } =
            (_, _, _) => Task.CompletedTask;

        public IReadOnlyCollection<long> SavedRows => [.. savedRows];

        public IReadOnlyCollection<IServiceProvider> ScopedProviders => [.. scopedProviders];

        public int ReadCalls => Volatile.Read(ref readCalls);

        public Task FirstRead => firstRead.Task;

        internal override Task<IReadOnlyList<OutboxMessageRecord>> ReadPendingRowsAsync(
            IServiceProvider scopedProvider,
            CancellationToken cancellationToken)
        {
            scopedProviders.Enqueue(scopedProvider);
            Interlocked.Increment(ref readCalls);
            firstRead.TrySetResult();
            return Read(cancellationToken);
        }

        internal override async Task SaveMarkerAsync(
            IServiceProvider scopedProvider,
            OutboxMessageRecord row,
            DateTimeOffset processedAt,
            CancellationToken cancellationToken)
        {
            savedRows.Enqueue(row.Id);
            await Save(row, processedAt, cancellationToken);
        }
    }

    internal sealed class OutboxRelayHarness : IAsyncDisposable
    {
        public static readonly DateTimeOffset StartedAt = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

        private readonly ServiceProvider provider;

        private OutboxRelayHarness(
            ServiceProvider provider,
            ManualTimeProvider time,
            IEnrichmentQueue queue,
            RecordingLogSink logs,
            OutboxRelayOptions options,
            ConcurrentQueue<EnrichmentRequested> published)
        {
            this.provider = provider;
            Time = time;
            Queue = queue;
            Logs = logs;
            Options = options;
            Published = published;
        }

        public ManualTimeProvider Time { get; }

        public IEnrichmentQueue Queue { get; }

        public RecordingLogSink Logs { get; }

        public OutboxRelayOptions Options { get; }

        public ConcurrentQueue<EnrichmentRequested> Published { get; }

        public IServiceProvider Services => provider;

        public static OutboxRelayHarness Create(
            Dictionary<string, string?>? settings = null,
            DateTimeOffset? startedAt = null)
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings ?? [])
                .Build();
            var time = new ManualTimeProvider(startedAt ?? StartedAt);
            var queue = Substitute.For<IEnrichmentQueue>();
            var logs = new RecordingLogSink();
            var published = new ConcurrentQueue<EnrichmentRequested>();
            queue.EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    published.Enqueue(callInfo.Arg<EnrichmentRequested>());
                    return Task.CompletedTask;
                });
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<TimeProvider>(time);
            services.AddSingleton(queue);
            services.AddLogging(logging => logging.AddProvider(logs));
            services.AddLamuFlixOutboxRelay();
            var provider = services.BuildServiceProvider();

            return new OutboxRelayHarness(
                provider,
                time,
                queue,
                logs,
                provider.GetRequiredService<IOptions<OutboxRelayOptions>>().Value,
                published);
        }

        public static Type? RelayWorkerType() =>
            typeof(OutboxRelayState).Assembly.GetType(WorkerTypeName, throwOnError: false);

        public BackgroundService CreateWorker()
        {
            var workerType = RelayWorkerType()
                ?? throw new InvalidOperationException($"{WorkerTypeName} is not implemented.");
            return (BackgroundService)ActivatorUtilities.CreateInstance(provider, workerType);
        }

        public ScriptedOutboxRelayWorker CreateScriptedWorker() =>
            ActivatorUtilities.CreateInstance<ScriptedOutboxRelayWorker>(provider);

        public async Task AdvanceAndWaitAsync(TimeSpan amount, bool waitForNextDelay)
        {
            Time.Advance(amount);
            if (waitForNextDelay)
            {
                await WaitForScheduledDelayAsync();
            }
        }

        public Task WaitForScheduledDelayAsync() =>
            Time.WaitForPendingTimerAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        public ValueTask DisposeAsync() => provider.DisposeAsync();
    }

    internal sealed class RecordingLogSink : ILoggerProvider
    {
        public ConcurrentQueue<RecordedLogEntry> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, Entries);

        public void Dispose()
        {
        }

        private sealed class RecordingLogger(string categoryName, ConcurrentQueue<RecordedLogEntry> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                ArgumentNullException.ThrowIfNull(formatter);
                entries.Enqueue(new RecordedLogEntry(
                    categoryName,
                    logLevel,
                    eventId,
                    formatter(state, exception),
                    exception));
            }
        }
    }

    internal sealed record RecordedLogEntry(
        string Category,
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception);
}
