using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MetadataProviderCollection
{
    public const string Name = "metadata-provider";
}

public sealed class MetadataProviderProbe : IAsyncLifetime
{
    public const string SentinelApiKey = "sentinel-key";
    public const string CheckName = "metadata-provider";

    private static readonly DateTimeOffset Now = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    private static readonly HttpClient WarmUpClient = new();
    private readonly List<IDisposable> disposables = [];

    public static DateTimeOffset FixedNow => Now;

    public RecordingLoggerFactory Logs { get; } = new();

    // ReSharper disable once NullableWarningSuppressionIsUsed - xUnit constructs the fixture; the server starts in InitializeAsync before any test reads it.
    public WireMockServer Server { get; private set; } = null!;

    private string BaseUrl => Server.Urls[0];

    public int RequestCount => Server.LogEntries.Count();

    public async ValueTask InitializeAsync()
    {
        Server = LoopbackWireMock.Start();

        // The first request in a process pays ~1s of one-time JIT across WireMock and the HTTP stack, which
        // overruns the 500ms loopback AttemptTimeout, so the first test would see a retried, double-counted request.
        using var warmUp = await WarmUpClient.GetAsync(new Uri(BaseUrl), TestContext.Current.CancellationToken);
        Server.Reset();
    }

    public ValueTask DisposeAsync()
    {
        Server.Stop();
        Server.Dispose();
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    public void Reset() => Server.Reset();

    public bool HasQuery(string name) =>
        Server.LogEntries.Any(entry => entry.RequestMessage?.Query?.Any(pair =>
            string.Equals(pair.Key, name, StringComparison.Ordinal)) == true);

    public IReadOnlyList<string> QueryValues(string name) =>
        [.. Server.LogEntries
            .Select(entry => entry.RequestMessage?.Query)
            .Where(query => query is not null)
            // ReSharper disable once NullableWarningSuppressionIsUsed - Where(is not null) above guarantees non-null; the compiler cannot narrow through Enumerable.Where.
            .SelectMany(query => query!)
            .Where(pair => string.Equals(pair.Key, name, StringComparison.Ordinal))
            .SelectMany(pair => pair.Value)
            .Distinct(StringComparer.Ordinal)];

    public ServiceProvider BuildServices(
        IDictionary<string, string?>? overrides = null,
        IOmdbRequestGate? gate = null)
    {
        var services = NewServices(overrides, new FixedTimeProvider(Now));
        services.AddSingleton(gate ?? RecordingRequestGate.Admitting());
        services.AddMetadataProvider();

        var provider = services.BuildServiceProvider();
        disposables.Add(provider);
        return provider;
    }

    public ServiceCollection NewServices(
        IDictionary<string, string?>? overrides = null,
        TimeProvider? clock = null)
    {
        var settings = LoopbackSettings();
        if (overrides is not null)
        {
            foreach (var pair in overrides)
            {
                settings[pair.Key] = pair.Value;
            }
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(Logs);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        if (clock is not null)
        {
            services.AddSingleton(clock);
        }

        services.AddLamuFlixOptions();
        return services;
    }

    private static Dictionary<string, string?> SharedSettings() =>
        new(StringComparer.Ordinal)
        {
            [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = @"C:\library",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = SentinelApiKey,
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://www.omdbapi.com/",
            [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
            [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}"] = "00:05:00",
            [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.SweepInterval)}"] = "00:01:00",
            [$"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}"] = "true",
        };

    private Dictionary<string, string?> LoopbackSettings() =>
        new(SharedSettings(), StringComparer.Ordinal)
        {
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = BaseUrl,
            [Resilience(nameof(MetadataProviderResilienceOptions.BaseDelay))] = "00:00:00.010",
            [Resilience(nameof(MetadataProviderResilienceOptions.AttemptTimeout))] = "00:00:00.500",
            [Resilience(nameof(MetadataProviderResilienceOptions.TotalTimeout))] = "00:00:03",
            [Resilience(nameof(MetadataProviderResilienceOptions.SamplingDuration))] = "00:00:01",
            [Resilience(nameof(MetadataProviderResilienceOptions.BreakDuration))] = "00:00:01",
        };

    public static string Resilience(string member) =>
        $"{MetadataProviderResilienceOptions.SectionName}:{member}";

    public sealed class RecordingRequestGate(Func<CancellationToken, Task<bool>> admission) : IOmdbRequestGate
    {
        private int invocations;
        private int commitments;

        public int Invocations => Volatile.Read(ref invocations);

        public int Commitments => Volatile.Read(ref commitments);

        public static RecordingRequestGate Admitting() => new(_ => Task.FromResult(true));

        public static RecordingRequestGate AdmittingOnly(int admissions)
        {
            var granted = 0;
            return new(_ => Task.FromResult(Interlocked.Increment(ref granted) <= admissions));
        }

        public static RecordingRequestGate Faulting(Exception failure) => new(_ => Task.FromException<bool>(failure));

        public static RecordingRequestGate Slow(TimeSpan delay) => new(async _ =>
        {
            await Task.Delay(delay, CancellationToken.None);
            return true;
        });

        public async Task<bool> TryAcquireAsync(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref invocations);
            var admitted = await admission(cancellationToken);
            if (admitted)
            {
                Interlocked.Increment(ref commitments);
            }

            return admitted;
        }
    }

    public sealed record CapturedLog(
        string CategoryName,
        string Message,
        IReadOnlyList<KeyValuePair<string, object?>> State,
        IReadOnlyList<object> Scopes,
        string ExceptionText);

    public sealed class RecordingLoggerFactory : ILoggerFactory
    {
        private readonly List<CapturedLog> entries = [];
        private readonly AsyncLocal<Scope?> ambientScope = new();

        public IReadOnlyList<CapturedLog> Entries
        {
            get
            {
                lock (entries)
                {
                    return [.. entries];
                }
            }
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => new RecordingLogger(this, categoryName);

        public void Dispose()
        {
        }

        private void Add(CapturedLog entry)
        {
            lock (entries)
            {
                entries.Add(entry);
            }
        }

        private Scope Push(object state)
        {
            var scope = new Scope(this, state, ambientScope.Value);
            ambientScope.Value = scope;
            return scope;
        }

        private void Pop(Scope scope)
        {
            if (!ReferenceEquals(ambientScope.Value, scope))
            {
                return;
            }

            ambientScope.Value = scope.Parent;
        }

        private IReadOnlyList<object> ActiveScopes()
        {
            var payloads = new List<object>();
            for (var scope = ambientScope.Value; scope is not null; scope = scope.Parent)
            {
                payloads.Add(scope.State);
            }

            return payloads;
        }

        private sealed class Scope(RecordingLoggerFactory owner, object state, Scope? parent) : IDisposable
        {
            private bool disposed;

            public object State { get; } = state;

            public Scope? Parent { get; } = parent;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                owner.Pop(this);
            }
        }

        private sealed class RecordingLogger(RecordingLoggerFactory owner, string categoryName) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull => owner.Push(state);

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                {
                    return;
                }

                owner.Add(new CapturedLog(
                    categoryName,
                    formatter(state, exception),
                    state is IEnumerable<KeyValuePair<string, object?>> pairs ? [.. pairs] : [],
                    owner.ActiveScopes(),
                    exception?.ToString() ?? string.Empty));
            }
        }
    }
}