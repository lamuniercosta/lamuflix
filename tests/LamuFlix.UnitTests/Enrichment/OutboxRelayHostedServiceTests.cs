using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Enrichment;
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

    internal sealed class OutboxRelayHarness : IAsyncDisposable
    {
        public static readonly DateTimeOffset StartedAt = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

        private readonly ServiceProvider provider;

        private OutboxRelayHarness(
            ServiceProvider provider,
            ManualTimeProvider time,
            IEnrichmentQueue queue,
            RecordingLogSink logs,
            OutboxRelayOptions options)
        {
            this.provider = provider;
            Time = time;
            Queue = queue;
            Logs = logs;
            Options = options;
        }

        public ManualTimeProvider Time { get; }

        public IEnrichmentQueue Queue { get; }

        public RecordingLogSink Logs { get; }

        public OutboxRelayOptions Options { get; }

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
                provider.GetRequiredService<IOptions<OutboxRelayOptions>>().Value);
        }

        public static Type? RelayWorkerType() =>
            typeof(OutboxRelayState).Assembly.GetType(WorkerTypeName, throwOnError: false);

        public BackgroundService CreateWorker()
        {
            var workerType = RelayWorkerType()
                ?? throw new InvalidOperationException($"{WorkerTypeName} is not implemented.");
            return (BackgroundService)ActivatorUtilities.CreateInstance(provider, workerType);
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
