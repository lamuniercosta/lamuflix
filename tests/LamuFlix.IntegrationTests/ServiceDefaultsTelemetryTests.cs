using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Features.Import;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Tests.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(TelemetryCompositionCollection))]
public sealed class ServiceDefaultsTelemetryTests(PostgresFixture postgres, RabbitMqFixture rabbitMq)
    : IAsyncLifetime
{
    private const string AspNetCoreSourceName = "Microsoft.AspNetCore";
    private const string HttpClientSourceName = "System.Net.Http";
    private const string NpgsqlSourceName = "Npgsql";
    private const string EndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string ProtocolVariable = "OTEL_EXPORTER_OTLP_PROTOCOL";
    private const string HttpProtobuf = "http/protobuf";
    private const string TracesPath = "/v1/traces";
    private const string MetricsPath = "/v1/metrics";
    private const string LogsPath = "/v1/logs";
    private const int FlushTimeoutMilliseconds = 5_000;
    private const int MetricsFlushAttempts = 3;
    private const string MeterScopeName = "LamuFlix";
    private const string DurationMetricName = "lamuflix.enrichment.duration";
    private const string OutcomeMetricName = "lamuflix.enrichment.outcome";
    private const string ImportMetricName = "lamuflix.import.count";
    private const string ImportFolderName = "Blade Runner (1982)";
    private const string ImportVideoFileName = "blade runner.mkv";
    private static readonly byte[] ImportVideoBytes = [0x1A, 0x45, 0xDF, 0xA3];
    private static readonly HttpClient ProbeClient = new();
    private const string FoundBody = """
        {
          "Response": "True",
          "Title": "Blade Runner",
          "Year": "1982",
          "Runtime": "117 min",
          "Plot": "A blade runner must pursue and terminate four replicants.",
          "imdbRating": "8.1",
          "imdbID": "tt0083658"
        }
        """;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await postgres.ResetAsync(cancellationToken);
        await rabbitMq.ResetTopologyAsync(cancellationToken);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task ProductionHost_ForceFlushAfterOneSpanMetricAndLog_ExporterIsComposed()
    {
        // arrange
        WireMockServer? stub = null;
        WebApplicationFactory<Program>? host = null;
        EnvironmentOverride? env = null;
        try
        {
            stub = StartStub();
            env = new EnvironmentOverride(OtlpEndpoint(stub));
            host = CreateProductionHost(OtlpEndpoint(stub), stub.Urls[0]);
            var tracer = host.Services.GetRequiredService<TracerProvider>();
            var meterProvider = host.Services.GetRequiredService<MeterProvider>();
            var loggerProvider = host.Services.GetRequiredService<LoggerProvider>();

            // act
            EmitSignals(host);
            var tracesFlushed = tracer.ForceFlush(FlushTimeoutMilliseconds);
            var metricsFlushed = FlushMetricsWithRetry(meterProvider);
            var logsFlushed = loggerProvider.ForceFlush(FlushTimeoutMilliseconds);

            // assert
            tracesFlushed.ShouldBeTrue();
            metricsFlushed.ShouldBeTrue();
            logsFlushed.ShouldBeTrue();
            Received(stub, TracesPath).ShouldBeTrue();
            Received(stub, MetricsPath).ShouldBeTrue();
            Received(stub, LogsPath).ShouldBeTrue();
        }
        finally
        {
            if (host is not null)
            {
                await DisposeHostAsync(host);
            }

            env?.Dispose();
            stub?.Dispose();
            ClearAmbientActivity();
        }
    }

    [Fact]
    public async Task ProductionHost_FlushAfterRealImportAndEnrichment_ReceiveTheThreeRuledInstrumentIdentitiesTypesUnitsAndDimensions()
    {
        // arrange
        WireMockServer? stub = null;
        WebApplicationFactory<Program>? host = null;
        EnvironmentOverride? env = null;
        string? importRoot = null;
        try
        {
            stub = StartStub();
            env = new EnvironmentOverride(OtlpEndpoint(stub));
            host = CreateCompositionHost(stub);
            importRoot = CreateImportRoot();
            var cancellationToken = TestContext.Current.CancellationToken;

            // act
            ProcessEnrichmentOutcome outcome;
            using (var scope = host.Services.CreateScope())
            {
                var import = scope.ServiceProvider
                    .GetRequiredService<ICommandHandler<ImportMovieFolderCommand, MovieId>>();
                var movieId = await import.HandleAsync(
                    new ImportMovieFolderCommand(new LibraryPath(Path.Combine(importRoot, ImportFolderName))),
                    cancellationToken);
                var enrichment = scope.ServiceProvider
                    .GetRequiredService<ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>>();
                outcome = await enrichment.HandleAsync(new ProcessEnrichmentCommand(movieId, 1), cancellationToken);
            }

            var meterProvider = host.Services.GetRequiredService<MeterProvider>();
            var flushed = meterProvider.ForceFlush(FlushTimeoutMilliseconds);

            // assert
            flushed.ShouldBeTrue();
            outcome.ShouldBe(new ProcessEnrichmentOutcome.Completed(Claimed: true));
            var payloads = MetricsPayloads(stub);
            payloads.ShouldNotBeEmpty("the composed host exported no OTLP metrics payload");
            var metrics = payloads.SelectMany(payload => OtlpProtobuf.Read(payload)).ToList();
            AssertInstrument(metrics, DurationMetricName, MetricKind.Histogram, "s", "provider", "omdb");
            AssertInstrument(metrics, OutcomeMetricName, MetricKind.Sum, string.Empty, "outcome", "enriched");
            AssertInstrument(metrics, ImportMetricName, MetricKind.Sum, string.Empty, null, null);
        }
        finally
        {
            if (host is not null)
            {
                await DisposeHostAsync(host);
            }

            if (importRoot is not null && Directory.Exists(importRoot))
            {
                Directory.Delete(importRoot, recursive: true);
            }

            env?.Dispose();
            stub?.Dispose();
            ClearAmbientActivity();
        }
    }

    [Fact]
    public Task Observer_InProcessRequest_RecordsMicrosoftAspNetCore() =>
        AssertSourceAsync(
            AspNetCoreSourceName,
            static (_, _) => Task.CompletedTask,
            static (host, _, cancellationToken) => RequestInProcessAsync(host, cancellationToken),
            nameof(Observer_InProcessRequest_RecordsMicrosoftAspNetCore));

    [Fact]
    public Task Observer_OutboundRequest_RecordsSystemNetHttp() =>
        AssertSourceAsync(
            HttpClientSourceName,
            static (stub, cancellationToken) => GetAsync(OtlpEndpoint(stub), cancellationToken),
            static (host, stub, cancellationToken) => GetFromHostAsync(host, stub, cancellationToken));

    [Fact]
    public Task Observer_PostgresQuery_RecordsNpgsql() =>
        AssertSourceAsync(
            NpgsqlSourceName,
            (_, cancellationToken) => QueryPostgresAsync(cancellationToken),
            (_, _, cancellationToken) => QueryPostgresAsync(cancellationToken));

    [Fact]
    public Task Observer_OmdbLookup_RecordsLamuFlix() =>
        AssertSourceAsync(
            TelemetryConstants.ActivitySourceName,
            static (stub, cancellationToken) => LookupWithoutHostAsync(stub.Urls[0], cancellationToken),
            static (host, _, cancellationToken) => LookupWithHostAsync(host, cancellationToken));

    [Fact]
    public async Task Observer_BasicPublishAsync_RecordsRabbitMqClientPublisher()
    {
        var probe = new RabbitMqProbe(rabbitMq);
        await probe.InitializeAsync();
        try
        {
            await AssertSourceAsync(
                TelemetryConstants.RabbitMqPublisherActivitySourceName,
                (_, cancellationToken) => PublishAsync(probe, cancellationToken),
                (_, _, cancellationToken) => PublishAsync(probe, cancellationToken));
        }
        finally
        {
            await probe.DisposeAsync();
        }
    }

    [Fact]
    public async Task Observer_BasicGetAsync_RecordsRabbitMqClientSubscriber()
    {
        var probe = new RabbitMqProbe(rabbitMq);
        await probe.InitializeAsync();
        try
        {
            await AssertSourceAsync(
                TelemetryConstants.RabbitMqSubscriberActivitySourceName,
                (_, cancellationToken) => PublishAndGetAsync(probe, cancellationToken),
                (_, _, cancellationToken) => PublishAndGetAsync(probe, cancellationToken));
        }
        finally
        {
            await probe.DisposeAsync();
        }
    }

    private async Task AssertSourceAsync(
        string sourceName,
        Func<WireMockServer, CancellationToken, Task> negative,
        Func<WebApplicationFactory<Program>, WireMockServer, CancellationToken, Task> positive,
        string? observerCompletionIdentity = null)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        WireMockServer? stub = null;
        WebApplicationFactory<Program>? host = null;
        EnvironmentOverride? env = null;
        PassiveObserver? observer = null;
        try
        {
            ClearAmbientActivity();
            stub = StartStub();
            env = new EnvironmentOverride(OtlpEndpoint(stub));
            observer = new PassiveObserver(sourceName);

            await negative(stub, cancellationToken);
            observer.Recorded.ShouldBeEmpty(observer.Describe());

            host = CreateProductionHost(OtlpEndpoint(stub), stub.Urls[0]);
            _ = host.Services.GetRequiredService<TracerProvider>();

            await positive(host, stub, cancellationToken);
            if (observerCompletionIdentity is not null)
            {
                await observer.AwaitStoppedAsync(
                    sourceName,
                    observerCompletionIdentity,
                    FlushTimeoutMilliseconds,
                    cancellationToken);
            }

            observer.Recorded.ShouldNotBeEmpty(observer.Describe());
        }
        finally
        {
            observer?.Dispose();
            if (host is not null)
            {
                await DisposeHostAsync(host);
            }

            env?.Dispose();
            stub?.Dispose();
            ClearAmbientActivity();
        }
    }

    private static async Task DisposeHostAsync(WebApplicationFactory<Program> host)
    {
        // WebApplicationFactory.DisposeAsync returns before the minimal-hosting RunAsync continuation
        // disposes the inner host's container, so the providers' ActivityListener/MeterListener would
        // outlive this call by the OTLP export timeout. Dispose them here, on the awaited path; the
        // container's later Dispose is a no-op.
        host.Services.GetService<TracerProvider>()?.Dispose();
        host.Services.GetService<MeterProvider>()?.Dispose();
        await host.DisposeAsync();
    }

    private static void ClearAmbientActivity()
    {
        while (Activity.Current is { } ambient)
        {
            ambient.Stop();
        }
    }

    private WebApplicationFactory<Program> CreateProductionHost(string otlpEndpoint, string omdbBaseUrl) =>
        new ApiHostFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:DefaultConnection", postgres.ConnectionString);
            builder.UseSetting($"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}", omdbBaseUrl);
            builder.UseSetting($"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}", "telemetry-key");
            builder.UseSetting($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}", rabbitMq.Options.HostName);
            builder.UseSetting(
                $"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.Port)}",
                rabbitMq.Options.Port.ToString(CultureInfo.InvariantCulture));
            builder.UseSetting($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.UserName)}", rabbitMq.Options.UserName);
            builder.UseSetting($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.Password)}", rabbitMq.Options.Password);
            builder.UseSetting(EndpointVariable, otlpEndpoint);
            builder.UseSetting(ProtocolVariable, HttpProtobuf);
        });

    private WebApplicationFactory<Program> CreateCompositionHost(WireMockServer stub) =>
        new ApiHostFactory(
            retainSweeper: false,
            additionalSettings:
            [
                new("ConnectionStrings:DefaultConnection", postgres.ConnectionString),
                new($"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}", stub.Urls[0]),
                new($"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}", "telemetry-key"),
                new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}", rabbitMq.Options.HostName),
                new(
                    $"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.Port)}",
                    rabbitMq.Options.Port.ToString(CultureInfo.InvariantCulture)),
                new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.UserName)}", rabbitMq.Options.UserName),
                new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.Password)}", rabbitMq.Options.Password),
                new(
                    $"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.RetryDelay)}",
                    rabbitMq.Options.RetryDelay.ToString("c", CultureInfo.InvariantCulture)),
                new($"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}", "00:00:01"),
            ],
            disableEnrichmentConsumer: true);

    private static bool FlushMetricsWithRetry(MeterProvider meterProvider)
    {
        for (int attempt = 0; attempt < MetricsFlushAttempts; attempt++)
        {
            if (meterProvider.ForceFlush(FlushTimeoutMilliseconds))
            {
                return true;
            }
        }

        return false;
    }

    private static void EmitSignals(WebApplicationFactory<Program> host)
    {
        using var source = new ActivitySource(TelemetryConstants.ActivitySourceName);
        // ReSharper disable once ExplicitCallerInfoArgument - the span name is a stable telemetry contract, not the caller method name.
        using var activity = source.StartActivity("telemetry.composition.probe");
        var meterFactory = host.Services.GetRequiredService<IMeterFactory>();
        using var meter = meterFactory.Create(TelemetryConstants.ActivitySourceName);
        meter.CreateCounter<long>("telemetry.composition.probe").Add(1);
        host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("telemetry.composition.probe")
            .LogInformation("exporter is composed");
        activity?.Stop();
    }

    private static async Task RequestInProcessAsync(
        WebApplicationFactory<Program> host,
        CancellationToken cancellationToken)
    {
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), cancellationToken);
    }

    private static async Task GetAsync(string url, CancellationToken cancellationToken)
    {
        using var response = await ProbeClient.GetAsync(new Uri(url), cancellationToken);
    }

    private static async Task GetFromHostAsync(
        WebApplicationFactory<Program> host,
        WireMockServer stub,
        CancellationToken cancellationToken)
    {
        using var response = await host.Services.GetRequiredService<IHttpClientFactory>()
            .CreateClient()
            .GetAsync(new Uri(OtlpEndpoint(stub)), cancellationToken);
    }

    private async Task QueryPostgresAsync(CancellationToken cancellationToken)
    {
        await using var context = postgres.CreateMigratedContext();
        await context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
    }

    private static async Task LookupWithoutHostAsync(string baseUrl, CancellationToken cancellationToken)
    {
        // ReSharper disable once ShortLivedHttpClient - OmdbMetadataProvider owns this client for one lookup and must not share BaseAddress.
        using (var http = new HttpClient())
        {
            http.BaseAddress = new Uri(baseUrl);
            var provider = new OmdbMetadataProvider(
                http,
                Options.Create(new OmdbOptions { ApiKey = "telemetry-key", BaseUrl = baseUrl }),
                TimeProvider.System,
                NullLogger<OmdbMetadataProvider>.Instance,
                new MetadataProviderHealthState());
            await provider.FindAsync(new MetadataLookup("Blade Runner", null), cancellationToken);
        }
    }

    private static async Task LookupWithHostAsync(
        WebApplicationFactory<Program> host,
        CancellationToken cancellationToken)
    {
        using var scope = host.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMetadataProvider>()
            .FindAsync(new MetadataLookup("Blade Runner", null), cancellationToken);
    }

    private static async Task PublishAsync(RabbitMqProbe probe, CancellationToken cancellationToken) =>
        await probe.PublishAsync(RabbitMqTopology.RequestedRoutingKey, "telemetry"u8.ToArray(), cancellationToken);

    private static async Task PublishAndGetAsync(RabbitMqProbe probe, CancellationToken cancellationToken)
    {
        await PublishAsync(probe, cancellationToken);
        (await probe.PollGetAsync(RabbitMqTopology.RequestedQueue, cancellationToken)).ShouldNotBeNull();
    }

    private static WireMockServer StartStub()
    {
        var server = WireMockServer.Start();
        server.Given(Request.Create().WithPath(TracesPath)).RespondWith(Response.Create().WithStatusCode(200));
        server.Given(Request.Create().WithPath(MetricsPath)).RespondWith(Response.Create().WithStatusCode(200));
        server.Given(Request.Create().WithPath(LogsPath)).RespondWith(Response.Create().WithStatusCode(200));
        server.Given(Request.Create().UsingGet()).RespondWith(Response.Create().WithStatusCode(200).WithBody(FoundBody));
        return server;
    }

    private static string OtlpEndpoint(WireMockServer stub) => stub.Urls[0].TrimEnd('/');

    private static bool Received(WireMockServer stub, string path) =>
        stub.LogEntries.Any(entry =>
        {
            var request = entry.RequestMessage;
            if (request is null)
            {
                return false;
            }

            return request.Path.Contains(path, StringComparison.Ordinal)
                || request.AbsoluteUrl.Contains(path, StringComparison.Ordinal);
        });

    private static string CreateImportRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"lamuflix-dev319-composition-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, ImportFolderName));
        File.WriteAllBytes(Path.Combine(root, ImportFolderName, ImportVideoFileName), ImportVideoBytes);
        return root;
    }

    private static List<byte[]> MetricsPayloads(WireMockServer stub)
    {
        var payloads = new List<byte[]>();
        foreach (var entry in stub.LogEntries)
        {
            var request = entry.RequestMessage;
            if (request is not null
                && request.BodyAsBytes is { Length: > 0 } body
                && (request.Path.Contains(MetricsPath, StringComparison.Ordinal)
                    || request.AbsoluteUrl.Contains(MetricsPath, StringComparison.Ordinal)))
            {
                payloads.Add(body);
            }
        }

        return payloads;
    }

    private static List<ExportedMetric> InstrumentsNamed(IEnumerable<ExportedMetric> metrics, string name) =>
        [.. metrics.Where(metric => metric.Name == name)];

    private static void AssertInstrument(
        IReadOnlyList<ExportedMetric> metrics,
        string name,
        MetricKind kind,
        string unit,
        string? tagKey,
        string? tagValue)
    {
        var exported = InstrumentsNamed(metrics, name);
        exported.ShouldNotBeEmpty($"no metric named {name} reached the composed export");
        foreach (var metric in exported)
        {
            metric.Scope.ShouldBe(MeterScopeName, $"metric {name} was exported from an unexpected meter scope");
            metric.Kind.ShouldBe(kind, $"metric {name} was exported with an unexpected instrument type");
            metric.Unit.ShouldBe(unit, $"metric {name} was exported with an unexpected unit");
            if (kind == MetricKind.Sum)
            {
                metric.IsMonotonic.ShouldBeTrue($"metric {name} was exported as a non-monotonic sum");
            }
        }

        var points = exported.SelectMany(metric => metric.Points).ToList();
        points.ShouldNotBeEmpty($"metric {name} was exported without any data point");
        foreach (var point in points)
        {
            if (tagKey is null)
            {
                point.Tags.ShouldBeEmpty($"metric {name} was exported with tags but carries none");
            }
            else
            {
                point.Tags.Count.ShouldBe(1, $"metric {name} was not exported with exactly one tag");
                point.Tags.TryGetValue(tagKey, out var actual)
                    .ShouldBeTrue($"metric {name} was exported without the {tagKey} tag");
                actual.ShouldBe(tagValue, $"metric {name} was exported with an unexpected {tagKey} value");
            }
        }
    }

    private sealed class EnvironmentOverride : IDisposable
    {
        private readonly string? previousEndpoint;
        private readonly string? previousProtocol;

        public EnvironmentOverride(string endpoint)
        {
            previousEndpoint = Environment.GetEnvironmentVariable(EndpointVariable);
            previousProtocol = Environment.GetEnvironmentVariable(ProtocolVariable);
            Environment.SetEnvironmentVariable(EndpointVariable, endpoint);
            Environment.SetEnvironmentVariable(ProtocolVariable, HttpProtobuf);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(EndpointVariable, previousEndpoint);
            Environment.SetEnvironmentVariable(ProtocolVariable, previousProtocol);
        }
    }

    private sealed class PassiveObserver : IDisposable
    {
        private readonly ConcurrentBag<Activity> recorded = [];
        private readonly ConcurrentBag<string> stopped = [];
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ActivityListener listener;

        public PassiveObserver(string sourceName)
        {
            listener = new ActivityListener
            {
                ShouldListenTo = source => string.Equals(source.Name, sourceName, StringComparison.Ordinal),
                // ReSharper disable RedundantLambdaParameterType - the ref generic types select the Sample overloads.
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.None,
                SampleUsingParentId = static (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.None,
                // ReSharper restore RedundantLambdaParameterType
                ActivityStopped = activity =>
                {
                    stopped.Add(
                        $"{activity.Source.Name}:{activity.DisplayName}:Recorded={activity.Recorded}:AllData={activity.IsAllDataRequested}");
                    if (activity is { IsAllDataRequested: true, Recorded: true })
                    {
                        recorded.Add(activity);
                    }

                    completion.TrySetResult();
                },
            };
            ActivitySource.AddActivityListener(listener);
        }

        public IReadOnlyCollection<Activity> Recorded => recorded;

        public string Describe() => stopped.Count == 0
            ? "no stopped activities"
            : string.Join("; ", stopped);

        public async Task AwaitStoppedAsync(
            string sourceName,
            string testIdentity,
            int timeoutMilliseconds,
            CancellationToken cancellationToken)
        {
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            budget.CancelAfter(timeoutMilliseconds);
            var signaled = false;
            try
            {
                await completion.Task.WaitAsync(budget.Token);
                signaled = true;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Intentionally empty: only the finite wait budget elapsed; the assertion below fails with source, test identity and Describe output.
            }

            signaled.ShouldBeTrue(
                $"timed out after {timeoutMilliseconds} ms waiting for a stopped activity from source {sourceName} in {testIdentity}; {Describe()}");
        }

        public void Dispose() => listener.Dispose();
    }

    private enum MetricKind
    {
        Other,
        Sum,
        Histogram,
    }

    private sealed record ExportedPoint(IReadOnlyDictionary<string, string> Tags);

    private sealed record ExportedMetric(
        string Scope,
        string Name,
        string Unit,
        MetricKind Kind,
        bool IsMonotonic,
        IReadOnlyList<ExportedPoint> Points);

    private readonly record struct ProtoField(int Number, ReadOnlyMemory<byte> Payload);

    private static class OtlpProtobuf
    {
        private const int NumberDataPointAttributesField = 7;
        private const int HistogramDataPointAttributesField = 9;

        public static List<ExportedMetric> Read(ReadOnlyMemory<byte> payload)
        {
            var metrics = new List<ExportedMetric>();
            foreach (var field in ReadFields(payload))
            {
                if (field.Number == 1)
                {
                    ReadResourceMetrics(field.Payload, metrics);
                }
            }

            return metrics;
        }

        private static void ReadResourceMetrics(ReadOnlyMemory<byte> payload, List<ExportedMetric> metrics)
        {
            foreach (var field in ReadFields(payload))
            {
                if (field.Number == 2)
                {
                    ReadScopeMetrics(field.Payload, metrics);
                }
            }
        }

        private static void ReadScopeMetrics(ReadOnlyMemory<byte> payload, List<ExportedMetric> metrics)
        {
            var fields = ReadFields(payload);
            var scope = ReadScopeName(fields
                .Where(field => field.Number == 1)
                .Select(field => field.Payload)
                .FirstOrDefault());
            foreach (var field in fields)
            {
                if (field.Number == 2)
                {
                    metrics.Add(ReadMetric(scope, field.Payload));
                }
            }
        }

        private static string ReadScopeName(ReadOnlyMemory<byte> scopePayload) =>
            ReadString(ReadFields(scopePayload)
                .Where(field => field.Number == 1)
                .Select(field => field.Payload)
                .FirstOrDefault());

        private static ExportedMetric ReadMetric(string scope, ReadOnlyMemory<byte> payload)
        {
            var name = string.Empty;
            var unit = string.Empty;
            var kind = MetricKind.Other;
            var isMonotonic = false;
            var points = new List<ExportedPoint>();
            foreach (var field in ReadFields(payload))
            {
                switch (field.Number)
                {
                    case 1:
                        name = ReadString(field.Payload);
                        break;
                    case 3:
                        unit = ReadString(field.Payload);
                        break;
                    case 7:
                        kind = MetricKind.Sum;
                        isMonotonic = ReadSum(field.Payload, points);
                        break;
                    case 9:
                        kind = MetricKind.Histogram;
                        ReadHistogram(field.Payload, points);
                        break;
                }
            }

            return new ExportedMetric(scope, name, unit, kind, isMonotonic, points);
        }

        private static bool ReadSum(ReadOnlyMemory<byte> payload, List<ExportedPoint> points)
        {
            var isMonotonic = false;
            foreach (var field in ReadFields(payload))
            {
                if (field.Number == 1)
                {
                    points.Add(ReadDataPoint(field.Payload, NumberDataPointAttributesField));
                }
                else if (field.Number == 3)
                {
                    isMonotonic = ReadBool(field.Payload);
                }
            }

            return isMonotonic;
        }

        private static void ReadHistogram(ReadOnlyMemory<byte> payload, List<ExportedPoint> points)
        {
            foreach (var field in ReadFields(payload))
            {
                if (field.Number == 1)
                {
                    points.Add(ReadDataPoint(field.Payload, HistogramDataPointAttributesField));
                }
            }
        }

        private static ExportedPoint ReadDataPoint(ReadOnlyMemory<byte> payload, int attributesField)
        {
            var tags = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var field in ReadFields(payload))
            {
                if (field.Number == attributesField)
                {
                    ReadKeyValue(field.Payload, tags);
                }
            }

            return new ExportedPoint(tags);
        }

        private static void ReadKeyValue(ReadOnlyMemory<byte> payload, Dictionary<string, string> tags)
        {
            var key = string.Empty;
            string? value = null;
            foreach (var field in ReadFields(payload))
            {
                if (field.Number == 1)
                {
                    key = ReadString(field.Payload);
                }
                else if (field.Number == 2)
                {
                    value = ReadStringOrNull(field.Payload);
                }
            }

            if (value is not null)
            {
                tags[key] = value;
            }
        }

        private static string? ReadStringOrNull(ReadOnlyMemory<byte> payload)
        {
            foreach (var field in ReadFields(payload))
            {
                if (field.Number == 1)
                {
                    return ReadString(field.Payload);
                }
            }

            return null;
        }

        private static bool ReadBool(ReadOnlyMemory<byte> payload)
        {
            var offset = 0;
            return ReadVarint(payload, ref offset) != 0;
        }

        private static string ReadString(ReadOnlyMemory<byte> payload) =>
            Encoding.UTF8.GetString(payload.Span);

        private static List<ProtoField> ReadFields(ReadOnlyMemory<byte> payload)
        {
            var fields = new List<ProtoField>();
            var offset = 0;
            while (offset < payload.Length)
            {
                var tag = ReadVarint(payload, ref offset);
                var number = (int)(tag >> 3);
                switch ((int)(tag & 0x07))
                {
                    case 0:
                        {
                            var start = offset;
                            _ = ReadVarint(payload, ref offset);
                            fields.Add(new ProtoField(number, payload.Slice(start, offset - start)));
                            break;
                        }

                    case 1:
                        fields.Add(new ProtoField(number, payload.Slice(offset, 8)));
                        offset += 8;
                        break;

                    case 2:
                        {
                            var length = checked((int)ReadVarint(payload, ref offset));
                            fields.Add(new ProtoField(number, payload.Slice(offset, length)));
                            offset += length;
                            break;
                        }

                    case 5:
                        fields.Add(new ProtoField(number, payload.Slice(offset, 4)));
                        offset += 4;
                        break;

                    default:
                        throw new InvalidOperationException("Unsupported protobuf wire type.");
                }
            }

            return fields;
        }

        private static ulong ReadVarint(ReadOnlyMemory<byte> payload, ref int offset)
        {
            ulong value = 0;
            var shift = 0;
            while (offset < payload.Length)
            {
                var current = payload.Span[offset];
                offset++;
                value |= (ulong)(current & 0x7F) << shift;
                if ((current & 0x80) == 0)
                {
                    return value;
                }

                shift += 7;
            }

            throw new InvalidOperationException("Truncated protobuf varint.");
        }
    }
}
