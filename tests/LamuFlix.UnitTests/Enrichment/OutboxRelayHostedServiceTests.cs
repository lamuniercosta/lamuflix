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

    private static Task<IReadOnlyList<OutboxMessageRecord>> FailReadAsync() =>
        Task.FromException<IReadOnlyList<OutboxMessageRecord>>(new InvalidOperationException("read failed"));

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
        worker.Read = static _ => FailReadAsync();
        using var cancellation = new CancellationTokenSource();
        await worker.StartAsync(cancellation.Token);
        await harness.WaitForScheduledDelayAsync();

        static async Task expectDelayAsync(
            OutboxRelayHarness relayHarness,
            ScriptedOutboxRelayWorker scriptedWorker,
            TimeSpan expectedDelay,
            int readsAfter)
        {
            await relayHarness.AdvanceAndWaitAsync(expectedDelay - TimeSpan.FromMilliseconds(1), waitForNextDelay: false);
            scriptedWorker.ReadCalls.ShouldBe(readsAfter - 1);
            await relayHarness.AdvanceAndWaitAsync(TimeSpan.FromMilliseconds(1), waitForNextDelay: true);
            scriptedWorker.ReadCalls.ShouldBe(readsAfter);
        }

        // assert
        worker.ReadCalls.ShouldBe(1);

        // act
        await expectDelayAsync(harness, worker, TimeSpan.FromSeconds(1), 2);
        await expectDelayAsync(harness, worker, TimeSpan.FromSeconds(2), 3);
        await expectDelayAsync(harness, worker, TimeSpan.FromSeconds(4), 4);
        await expectDelayAsync(harness, worker, TimeSpan.FromSeconds(4), 5);

        worker.Read = static _ => Task.FromResult<IReadOnlyList<OutboxMessageRecord>>([]);
        await expectDelayAsync(harness, worker, TimeSpan.FromSeconds(4), 6);

        worker.Read = static _ => FailReadAsync();
        await expectDelayAsync(harness, worker, TimeSpan.FromSeconds(1), 7);
        await expectDelayAsync(harness, worker, TimeSpan.FromSeconds(1), 8);

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

    [Fact]
    public async Task Cleanup_IsDueAtStartupInASeparateScopeEvenAfterAFailedPass()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create();
        var worker = harness.CreateScriptedWorker();
        worker.Read = _ => Task.FromException<IReadOnlyList<OutboxMessageRecord>>(
            new InvalidOperationException("read failed"));
        using var cancellation = new CancellationTokenSource();

        // act
        await worker.StartAsync(cancellation.Token);
        await harness.WaitForScheduledDelayAsync();

        // assert
        worker.CleanupCalls.ShouldBe(1);
        var passScope = worker.ScopedProviders.ShouldHaveSingleItem();
        var cleanupScope = worker.CleanupScopes.ShouldHaveSingleItem();
        cleanupScope.ShouldNotBeSameAs(passScope);
        harness.Published.ShouldBeEmpty();
        await worker.StopAsync(cancellation.Token);
    }

    [Fact]
    public async Task Cleanup_RetriesAtTheNextIntervalAndNeverBlocksPublication()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create(new Dictionary<string, string?>
        {
            [$"{OutboxRelayOptions.SectionName}:PollInterval"] = "00:00:01",
            [$"{OutboxRelayOptions.SectionName}:CleanupInterval"] = "00:00:10",
        });
        var worker = harness.CreateScriptedWorker();
        worker.DeleteExpired = (_, _, _, _) =>
            Task.FromException<int>(new InvalidOperationException("cleanup failed"));
        using var cancellation = new CancellationTokenSource();
        await worker.StartAsync(cancellation.Token);
        await harness.WaitForScheduledDelayAsync();

        // assert
        worker.CleanupCalls.ShouldBe(1);
        worker.ReadCalls.ShouldBe(1);

        for (var second = 1; second < 10; second++)
        {
            await harness.AdvanceAndWaitAsync(TimeSpan.FromSeconds(1), waitForNextDelay: true);
            worker.ReadCalls.ShouldBe(second + 1);
            worker.CleanupCalls.ShouldBe(1);
        }

        // act
        await harness.AdvanceAndWaitAsync(TimeSpan.FromSeconds(1), waitForNextDelay: true);

        // assert
        worker.CleanupCalls.ShouldBe(2);
        worker.ReadCalls.ShouldBe(11);
        harness.Logs.Entries.Count.ShouldBe(2);
        foreach (var entry in harness.Logs.Entries)
        {
            entry.Exception.ShouldBeNull();
            entry.Message.ShouldContain("cleanup");
            entry.Message.ShouldContain(nameof(InvalidOperationException));
            entry.Message.ShouldNotContain("RowId");
        }

        await worker.StopAsync(cancellation.Token);
    }

    [Fact]
    public async Task CleanupFailure_DoesNotUndoMarkersOrBlockLaterPublication()
    {
        // arrange
        await using var harness = OutboxRelayHarness.Create(new Dictionary<string, string?>
        {
            [$"{OutboxRelayOptions.SectionName}:PollInterval"] = "00:00:01",
        });
        var worker = harness.CreateScriptedWorker();
        worker.Read = _ => Task.FromResult<IReadOnlyList<OutboxMessageRecord>>([Rows.Valid(51, movieId: 51)]);
        var cleanupAttempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        worker.DeleteExpired = (_, _, _, _) =>
        {
            cleanupAttempted.TrySetResult();
            return Task.FromException<int>(new InvalidOperationException("cleanup failed"));
        };
        var saves = 0;
        var secondSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        worker.Save = (_, _, _) =>
        {
            if (Interlocked.Increment(ref saves) == 2)
            {
                secondSave.TrySetResult();
            }

            return Task.CompletedTask;
        };
        using var cancellation = new CancellationTokenSource();

        // act
        await worker.StartAsync(cancellation.Token);
        await cleanupAttempted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert
        worker.SavedRows.ShouldBe([51L]);
        harness.Published.Select(message => message.MovieId.Value).ShouldBe([51]);
        worker.CleanupCalls.ShouldBe(1);

        await harness.WaitForScheduledDelayAsync();
        harness.Time.Advance(TimeSpan.FromSeconds(1));
        await secondSave.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        worker.ReadCalls.ShouldBe(2);
        worker.SavedRows.Count.ShouldBe(2);
        harness.Logs.Entries.ShouldContain(entry => entry.Message.Contains("cleanup", StringComparison.Ordinal));
        await worker.StopAsync(cancellation.Token);
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
        OutboxRelayMetrics metrics,
        ILogger<OutboxRelayHostedService> logger)
        : OutboxRelayHostedService(scopeFactory, options, timeProvider, queue, state, metrics, logger)
    {
        private readonly ConcurrentQueue<long> savedRows = new();
        private readonly ConcurrentQueue<IServiceProvider> scopedProviders = new();
        private readonly ConcurrentQueue<IServiceProvider> cleanupScopes = new();
        private readonly TaskCompletionSource firstRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int readCalls;
        private int cleanupCalls;

        public Func<CancellationToken, Task<IReadOnlyList<OutboxMessageRecord>>> Read { get; set; } =
            _ => Task.FromResult<IReadOnlyList<OutboxMessageRecord>>([]);

        internal override Task<OutboxRelayBacklogSample> SampleBacklogAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new OutboxRelayBacklogSample(0, null));
        public Func<OutboxMessageRecord, DateTimeOffset, CancellationToken, Task> Save { get; set; } =
            (_, _, _) => Task.CompletedTask;

        public Func<IServiceProvider, IReadOnlyList<long>, DateTimeOffset, CancellationToken, Task<int>> DeleteExpired
        { get; set; } = (_, _, _, _) => Task.FromResult(0);

        public IReadOnlyCollection<long> SavedRows => [.. savedRows];

        public IReadOnlyCollection<IServiceProvider> ScopedProviders => [.. scopedProviders];

        public IReadOnlyCollection<IServiceProvider> CleanupScopes => [.. cleanupScopes];

        public int ReadCalls => Volatile.Read(ref readCalls);

        public int CleanupCalls => Volatile.Read(ref cleanupCalls);

        public Task FirstRead => firstRead.Task;

        internal override Task<IReadOnlyList<long>> ReadExpiredProcessedRowIdsAsync(
            IServiceProvider scopedProvider,
            DateTimeOffset cutoff,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<long>>([]);

        internal override Task<int> DeleteExpiredProcessedRowsAsync(
            IServiceProvider scopedProvider,
            IReadOnlyList<long> rowIds,
            DateTimeOffset cutoff,
            CancellationToken cancellationToken)
        {
            cleanupScopes.Enqueue(scopedProvider);
            Interlocked.Increment(ref cleanupCalls);
            return DeleteExpired(scopedProvider, rowIds, cutoff, cancellationToken);
        }

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

    // ReSharper disable once NotAccessedPositionalProperty.Global // Entries read by failure assertions.
    internal sealed record RecordedLogEntry(
        string Category,
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception);
}
