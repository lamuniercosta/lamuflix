using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
            var metricsFlushed = meterProvider.ForceFlush(FlushTimeoutMilliseconds);
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
                await host.DisposeAsync();
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
            static (host, _, cancellationToken) => RequestInProcessAsync(host, cancellationToken));

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
        Func<WebApplicationFactory<Program>, WireMockServer, CancellationToken, Task> positive)
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
            observer.Recorded.ShouldNotBeEmpty(observer.Describe());
        }
        finally
        {
            observer?.Dispose();
            if (host is not null)
            {
                await host.DisposeAsync();
            }

            env?.Dispose();
            stub?.Dispose();
            ClearAmbientActivity();
        }
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
                },
            };
            ActivitySource.AddActivityListener(listener);
        }

        public IReadOnlyCollection<Activity> Recorded => recorded;

        public string Describe() => stopped.Count == 0
            ? "no stopped activities"
            : string.Join("; ", stopped);

        public void Dispose() => listener.Dispose();
    }
}
