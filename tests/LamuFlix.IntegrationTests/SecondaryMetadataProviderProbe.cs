using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.ServiceDefaults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SecondaryMetadataProviderCollection
{
    public const string Name = "secondary-metadata-provider";
}

public sealed class SecondaryMetadataProviderProbe : IAsyncLifetime
{
    public const string SentinelApiKey = "secondary-sentinel-key";
    public const string CheckName = "metadata-provider-tmdb";
    public const string PrimaryCheckName = "metadata-provider";

    private static readonly DateTimeOffset Now = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    private static readonly HttpClient WarmUpClient = new();
    private readonly List<IDisposable> disposables = [];

    public static DateTimeOffset FixedNow => Now;

    public MetadataProviderProbe.RecordingLoggerFactory Logs { get; } = new();

    // ReSharper disable once NullableWarningSuppressionIsUsed - xUnit constructs the fixture; the server starts in InitializeAsync before any test reads it.
    public WireMockServer Server { get; private set; } = null!;

    private string BaseUrl => Server.Urls[0];

    public string ApiBaseUrl => string.Concat(BaseUrl, "/3");

    public int RequestCount => Server.LogEntries.Count();

    public IReadOnlyList<string> RequestPaths =>
        [.. Server.LogEntries.Select(entry => entry.RequestMessage?.Path ?? string.Empty)];

    public async ValueTask InitializeAsync()
    {
        Server = LoopbackWireMock.Start();

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

    public ServiceProvider BuildServices(IDictionary<string, string?>? overrides = null, TimeProvider? clock = null)
    {
        var services = NewServices(overrides, clock);
        services.AddMetadataProvider();

        var provider = services.BuildServiceProvider();
        disposables.Add(provider);
        return provider;
    }

    private ServiceCollection NewServices(
        IDictionary<string, string?>? overrides = null,
        TimeProvider? clock = null)
    {
        var services = NewServicesWithoutOptions(clock);
        services.AddSingleton(Configuration(overrides));
        services.AddLamuFlixOptions();
        return services;
    }

    public ServiceCollection NewServicesWithoutOptions(TimeProvider? clock = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(Logs);
        if (clock is not null)
        {
            services.AddSingleton(clock);
        }

        return services;
    }

    public void StubSearch(string body, int statusCode = 200, TimeSpan? delay = null, TimeSpan? retryAfter = null) =>
        StubPath("/3/search/movie", statusCode, body, delay, retryAfter);

    public void StubDetails(int movieId, string body, int statusCode = 200, TimeSpan? delay = null) =>
        StubPath(string.Concat("/3/movie/", movieId.ToString(CultureInfo.InvariantCulture)), statusCode, body, delay);

    public void StubFind(string imdbId, string body, int statusCode = 200) =>
        StubPath(string.Concat("/3/find/", imdbId), statusCode, body);

    public void StubFoundLookup(string? searchBody = null, string? detailsBody = null)
    {
        StubSearch(searchBody ?? Fixture("Search", "found.json"));
        StubDetails(603, detailsBody ?? Fixture("Details", "found.json"));
    }

    public void StubPath(
        string path,
        int statusCode,
        string body = "",
        TimeSpan? delay = null,
        TimeSpan? retryAfter = null)
    {
        var response = Response.Create().WithStatusCode(statusCode).WithBody(body);
        if (delay is not null)
        {
            response = response.WithDelay(delay.Value);
        }

        if (retryAfter is not null)
        {
            response = response.WithHeader(
                "Retry-After",
                ((int)retryAfter.Value.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        }

        Server.Given(Request.Create().WithPath(path).UsingGet()).RespondWith(response);
    }

    public static string Fixture(params string[] segments) =>
        File.ReadAllText(Path.Combine([AppContext.BaseDirectory, "Fixtures", "SecondaryMetadata", .. segments]));

    public static string Resilience(string member) =>
        $"{MetadataProviderResilienceOptions.SectionName}:{member}";

    private IConfiguration Configuration(IDictionary<string, string?>? overrides)
    {
        var settings = LoopbackSettings();
        if (overrides is not null)
        {
            foreach (var pair in overrides)
            {
                settings[pair.Key] = pair.Value;
            }
        }

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static Dictionary<string, string?> SharedSettings() =>
        new(StringComparer.Ordinal)
        {
            [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = @"C:\library",
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = SentinelApiKey,
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = "https://www.omdbapi.com/",
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = "true",
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.ApiKey)}"] = SentinelApiKey,
            [$"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}"] = "localhost",
            [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}"] = "00:05:00",
            [$"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.SweepInterval)}"] = "00:01:00",
            [$"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}"] = "true",
        };

    private Dictionary<string, string?> LoopbackSettings() =>
        new(SharedSettings(), StringComparer.Ordinal)
        {
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = BaseUrl,
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.BaseUrl)}"] = ApiBaseUrl,
            [Resilience(nameof(MetadataProviderResilienceOptions.BaseDelay))] = "00:00:00.010",
            [Resilience(nameof(MetadataProviderResilienceOptions.AttemptTimeout))] = "00:00:00.500",
            [Resilience(nameof(MetadataProviderResilienceOptions.TotalTimeout))] = "00:00:03",
            [Resilience(nameof(MetadataProviderResilienceOptions.SamplingDuration))] = "00:00:01",
            [Resilience(nameof(MetadataProviderResilienceOptions.BreakDuration))] = "00:00:01",
        };
}

internal sealed class SecondaryActivityCapture : IDisposable
{
    private readonly ConcurrentBag<Activity> activities = [];
    private readonly ActivityListener listener;

    public SecondaryActivityCapture(params string[] sourceNames)
    {
        listener = new ActivityListener
        {
            ShouldListenTo = source => Array.IndexOf(sourceNames, source.Name) >= 0,
            Sample = static (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => activities.Add(activity),
        };
        ActivitySource.AddActivityListener(listener);
    }

    public IReadOnlyList<Activity> Activities => [.. activities];

    public IReadOnlyList<Activity> LookupSpans(string traceId) =>
        [.. activities.Where(activity =>
            string.Equals(activity.DisplayName, Core.Pipeline.TelemetryConstants.MetadataLookup, StringComparison.Ordinal)
            && string.Equals(activity.TraceId.ToHexString(), traceId, StringComparison.Ordinal))];

    public void Dispose() => listener.Dispose();
}

internal sealed record CapturedMeasurement(Instrument Instrument, object Value, KeyValuePair<string, object?>[] Tags);

internal sealed class SecondaryMetricCapture : IDisposable
{
    private const string ApplicationMeterName = "LamuFlix";
    private readonly ConcurrentQueue<CapturedMeasurement> measurements = [];
    private readonly MeterListener listener = new();

    public SecondaryMetricCapture(params string[] instrumentNames)
    {
        listener.SetMeasurementEventCallback<double>(
            (instrument, value, tags, _) => measurements.Enqueue(new(instrument, value, tags.ToArray())));
        listener.SetMeasurementEventCallback<long>(
            (instrument, value, tags, _) => measurements.Enqueue(new(instrument, value, tags.ToArray())));
        listener.InstrumentPublished = (instrument, publishedListener) =>
        {
            if (string.Equals(instrument.Meter.Name, ApplicationMeterName, StringComparison.Ordinal)
                && instrumentNames.Contains(instrument.Name, StringComparer.Ordinal))
            {
                publishedListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.Start();
    }

    public IReadOnlyList<CapturedMeasurement> Measurements => [.. measurements];

    public void Dispose() => listener.Dispose();
}
