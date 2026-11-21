using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.Persistence;
using LamuFlix.UnitTests.Features;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace LamuFlix.UnitTests.Persistence;

public sealed class TasteProfileCacheObservabilityTests
{
    [Fact]
    public void BelowWarningFraction_EmitsOnlyTheStructuredObservation()
    {
        // arrange
        var logger = new RecordingLogger<HybridCachedRecommendationProfileSource>();
        var adapter = Adapter(logger, warningFraction: 0.95);
        var envelope = TasteProfileCacheBudgetFixture.Envelope(13500);

        // act
        adapter.ObservePayload(envelope);

        // assert
        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Information);
    }

    [Fact]
    public void EqualToWarningFraction_EmitsTheWarning()
    {
        // arrange
        var envelope = TasteProfileCacheBudgetFixture.Envelope(13500);
        var logger = new RecordingLogger<HybridCachedRecommendationProfileSource>();
        var adapter = Adapter(logger, PayloadBytes(envelope) / (double)Ceiling());

        // act
        adapter.ObservePayload(envelope);

        // assert
        logger.Entries.Count.ShouldBe(2);
        logger.Entries[^1].Level.ShouldBe(LogLevel.Warning);
    }

    [Fact]
    public void AboveWarningFraction_EmitsTheWarning()
    {
        // arrange
        var logger = new RecordingLogger<HybridCachedRecommendationProfileSource>();
        var adapter = Adapter(logger, warningFraction: 0.1);

        // act
        adapter.ObservePayload(TasteProfileCacheBudgetFixture.Envelope(13500));

        // assert
        logger.Entries.Select(entry => entry.Level).ShouldBe([LogLevel.Information, LogLevel.Warning]);
    }

    [Fact]
    public void Observation_ReportsNumericPayloadBytesBudgetBytesAndFractionOnly()
    {
        // arrange
        var envelope = TasteProfileCacheBudgetFixture.Envelope(13500);
        var payloadBytes = PayloadBytes(envelope);
        var ceiling = Ceiling();
        var logger = new RecordingLogger<HybridCachedRecommendationProfileSource>();
        var adapter = Adapter(logger, warningFraction: 0.1);

        // act
        adapter.ObservePayload(envelope);

        // assert
        foreach (var entry in logger.Entries)
        {
            entry.State.Select(pair => pair.Key).ShouldBeSubsetOf(
                ["PayloadBytes", "BudgetBytes", "Fraction", "{OriginalFormat}"]);

            foreach (var pair in entry.State.Where(pair => pair.Key != "{OriginalFormat}"))
            {
                (pair.Value is int or long or double).ShouldBeTrue();
            }

            entry.Exception.ShouldBeNull();
        }

        Value(logger.Entries[0], "PayloadBytes").ShouldBe(payloadBytes);
        Value(logger.Entries[0], "BudgetBytes").ShouldBe(ceiling);
        Value(logger.Entries[0], "Fraction").ShouldBe(payloadBytes / (double)ceiling);
    }

    [Fact]
    public void Observation_DoesNotRenderLikedSourceTitlesOrCastIdentities()
    {
        // arrange
        var logger = new RecordingLogger<HybridCachedRecommendationProfileSource>();
        var adapter = Adapter(logger, warningFraction: 0.1);

        // act
        adapter.ObservePayload(TasteProfileCacheBudgetFixture.Envelope(4));

        // assert
        var rendered = logger.Entries
            .SelectMany(entry => entry.State.Select(pair =>
                Convert.ToString(pair.Value, CultureInfo.InvariantCulture) ?? string.Empty))
            .ToArray();

        rendered.ShouldAllBe(value => !value.Contains("Liked source", StringComparison.Ordinal));
        rendered.ShouldAllBe(value => !value.Contains("Perf ", StringComparison.Ordinal));
    }

    [Fact]
    public void ConstructionObservation_UsesTheSharedSingletonSerializer()
    {
        // arrange
        var adapter = Adapter(new RecordingLogger<HybridCachedRecommendationProfileSource>(), warningFraction: 0.1);
        var envelope = TasteProfileCacheBudgetFixture.Envelope(145);
        var expected = PayloadBytes(envelope);

        // act
        adapter.ObservePayload(envelope);

        // assert
        ((long)adapter.Serializer.LastSerializedBytes).ShouldBe(expected);
    }

    [Fact]
    public async Task WarningBandScale_AdmitsAndReusesWithoutAnyPayloadCeilingError()
    {
        // arrange
        await using var harness = CacheHarness.Create();
        var envelope = TasteProfileCacheBudgetFixture.Envelope(13500);
        var adapter = Adapter(harness.ProfileLogger, warningFraction: 0.8, harness.Cache);
        var ct = TestContext.Current.CancellationToken;

        // act
        var first = await harness.Cache.GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.WarningBandProbe",
            _ => Observe(adapter, envelope),
            cancellationToken: ct);
        var serializeCountAfterFirst = harness.Serializer.SerializeCount;
        var second = await harness.Cache.GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.WarningBandProbe",
            _ => Observe(adapter, envelope),
            cancellationToken: ct);

        // assert
        PayloadBytes(envelope).ShouldBeGreaterThan((long)(Ceiling() * 0.8));
        PayloadBytes(envelope).ShouldBeLessThan(Ceiling());
        harness.ProfileLogger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        harness.CacheLogs.Entries.ShouldNotContain(level => level == LogLevel.Error);
        harness.Serializer.SerializeCount.ShouldBe(serializeCountAfterFirst);
        serializeCountAfterFirst.ShouldBe(1);
        first.Generation.ShouldBe(second.Generation);
    }

    [Fact]
    public async Task OversizeLocalCacheOnly_WarnsBeforeAdmissionAndRetainsLocalReuse()
    {
        // arrange
        await using var harness = CacheHarness.Create();
        var envelope = TasteProfileCacheBudgetFixture.Envelope(18000);
        var adapter = Adapter(harness.ProfileLogger, warningFraction: 0.8, harness.Cache);
        var factoryInvocations = 0;
        var ct = TestContext.Current.CancellationToken;
        ValueTask<CachedProfile> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref factoryInvocations);
            return Observe(adapter, envelope);
        }

        // act
        var first = await harness.Cache.GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.OversizeProbe",
            Factory,
            cancellationToken: ct);
        var serializeCountAfterFirst = harness.Serializer.SerializeCount;
        var second = await harness.Cache.GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.OversizeProbe",
            Factory,
            cancellationToken: ct);

        // assert
        PayloadBytes(envelope).ShouldBeGreaterThan(Ceiling());
        harness.ProfileLogger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        harness.CacheLogs.Entries.ShouldContain(level => level == LogLevel.Error);
        factoryInvocations.ShouldBe(1);
        harness.Serializer.SerializeCount.ShouldBe(serializeCountAfterFirst);
        serializeCountAfterFirst.ShouldBe(1);
        first.Generation.ShouldBe(second.Generation);
    }

    [Fact]
    public async Task OversizeDistributedCacheWithLocalDisabled_ProvesActualNonAdmission()
    {
        // arrange
        await using var harness = CacheHarness.Create(disableLocalCache: true);
        var envelope = TasteProfileCacheBudgetFixture.Envelope(18000);
        var adapter = Adapter(harness.ProfileLogger, warningFraction: 0.8, harness.Cache);
        var factoryInvocations = 0;
        var ct = TestContext.Current.CancellationToken;
        ValueTask<CachedProfile> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref factoryInvocations);
            return Observe(adapter, envelope);
        }

        // act
        await harness.Cache.GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.L2OversizeProbe",
            Factory,
            cancellationToken: ct);
        await harness.Cache.GetOrCreateAsync(
            "LamuFlix.Recommendations.TasteProfile.L2OversizeProbe",
            Factory,
            cancellationToken: ct);

        // assert
        PayloadBytes(envelope).ShouldBeGreaterThan(Ceiling());
        harness.ProfileLogger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning);
        harness.CacheLogs.Entries.ShouldContain(level => level == LogLevel.Error);
        factoryInvocations.ShouldBe(2);
    }

    private static ValueTask<CachedProfile> Observe(
        HybridCachedRecommendationProfileSource adapter,
        CachedProfile envelope)
    {
        adapter.ObservePayload(envelope);
        return ValueTask.FromResult(envelope);
    }

    private static long PayloadBytes(CachedProfile envelope)
    {
        var writer = new ArrayBufferWriter<byte>();
        new CachedProfileSerializer().Serialize(envelope, writer);
        return writer.WrittenCount;
    }

    private static long Ceiling() => new HybridCacheOptions().MaximumPayloadBytes;

    private static double Value(RecordingLogger<HybridCachedRecommendationProfileSource>.LogEntry entry, string key) =>
        Convert.ToDouble(
            entry.State.Single(pair => pair.Key == key).Value,
            CultureInfo.InvariantCulture);

    private static HybridCachedRecommendationProfileSource Adapter(
        RecordingLogger<HybridCachedRecommendationProfileSource> logger,
        double warningFraction,
        HybridCache? cache = null) =>
        new(
            cache ?? new UnusedCache(),
            new UnusedScopeFactory(),
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(new TasteProfileCacheOptions { WarningFraction = warningFraction }),
            new CachedProfileSerializer(),
            logger,
            Microsoft.Extensions.Options.Options.Create(new HybridCacheOptions()));

    private sealed class CacheHarness : IAsyncDisposable
    {
        private readonly ServiceProvider provider;

        private CacheHarness(ServiceProvider provider, CapturingLoggerProvider cacheLogs, CachedProfileSerializer serializer)
        {
            this.provider = provider;
            CacheLogs = cacheLogs;
            Cache = provider.GetRequiredService<HybridCache>();
            Serializer = serializer;
        }

        public HybridCache Cache { get; }

        public CachedProfileSerializer Serializer { get; }

        public RecordingLogger<HybridCachedRecommendationProfileSource> ProfileLogger { get; } = new();

        public CapturingLoggerProvider CacheLogs { get; }

        internal static CacheHarness Create(bool disableLocalCache = false)
        {
            var cacheLogs = new CapturingLoggerProvider();
            var serializer = new CachedProfileSerializer();
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddProvider(cacheLogs));
            services.AddSingleton(serializer);
            services.AddDistributedMemoryCache();
            services.AddHybridCache(options =>
                {
                    if (disableLocalCache)
                    {
                        options.DefaultEntryOptions = new HybridCacheEntryOptions
                        {
                            Flags = HybridCacheEntryFlags.DisableLocalCache,
                        };
                    }
                })
                .AddSerializer(serializer);

            return new CacheHarness(services.BuildServiceProvider(), cacheLogs, serializer);
        }

        public ValueTask DisposeAsync() => provider.DisposeAsync();
    }

    private sealed class UnusedCache : HybridCache
    {
        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) =>
            factory(state, cancellationToken);

        public override ValueTask SetAsync<T>(
            string key,
            T value,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }

    private sealed class UnusedScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => throw new NotSupportedException();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<LogLevel> entries = [];

        public IReadOnlyList<LogLevel> Entries => entries;

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(CapturingLoggerProvider owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                owner.entries.Add(logLevel);
        }
    }
}
