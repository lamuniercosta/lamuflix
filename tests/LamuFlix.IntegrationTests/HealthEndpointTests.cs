using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.Enrichment;
using LamuFlix.Infrastructure.RabbitMq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class HealthEndpointTests
{
    private const string ReadyRoute = "/health/ready";
    private const string PostgresCheck = "postgres";
    private const string RabbitMqCheck = "rabbitmq";
    private const string MetadataCheck = "metadata-provider";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string SentinelException = "sentinel-exception-text";
    private const string SentinelDescription = "sentinel-check-description";
    private const string SentinelHost = "sentinel-db-host";
    private const string SentinelPort = "Port=65432";
    private const string SentinelData = "sentinel-check-data";

    public static TheoryData<string?, HttpStatusCode> ReadyStates =>
        new()
        {
            { null, HttpStatusCode.OK },
            { PostgresCheck, HttpStatusCode.ServiceUnavailable },
            { RabbitMqCheck, HttpStatusCode.ServiceUnavailable },
            { MetadataCheck, HttpStatusCode.OK },
        };

    [Theory]
    [MemberData(nameof(ReadyStates))]
    public async Task Ready_FourReadinessStates_ReturnsExpectedStatus(
        string? failingCheck,
        HttpStatusCode expected)
    {
        // arrange
        await using var host = CreateHost(failingCheck);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(ReadyRoute, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        response.StatusCode.ShouldBe(expected);
        body.ShouldNotContain(MetadataCheck);
    }

    [Fact]
    public void ReadyHost_RemovesOnlyTheSweeper()
    {
        // arrange
        using var host = CreateHost(null);

        // act
        var hostedServices = host.Services.GetServices<IHostedService>().ToArray();

        // assert
        hostedServices.ShouldNotContain(service => service is StrandedMovieSweeper);
        hostedServices.ShouldContain(service => service is EnrichmentConsumer);
    }

    [Fact]
    public void ReadyHost_RegistersTheSecondaryProviderCheckUntaggedOutsideReadiness()
    {
        // arrange
        using var host = new ApiHostFactory(retainSweeper: false, retainHealthChecks: true);

        // act
        var registrations = host.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        // assert
        var secondary = registrations.Single(registration => registration.Name == "metadata-provider-tmdb");
        secondary.Tags.ShouldBeEmpty();
        var ready = registrations
            .Where(registration => registration.Tags.Contains(HealthCheckTags.Ready))
            .Select(registration => registration.Name)
            .ToArray();
        ready.ShouldContain("postgres");
        ready.ShouldContain("rabbitmq");
        ready.ShouldNotContain("metadata-provider");
        ready.ShouldNotContain("metadata-provider-tmdb");
    }

    [Fact]
    public async Task Ready_Unhealthy_ReturnsProblemDetailsWithTraceIdAndNoExceptionText()
    {
        // arrange
        var leak = HealthCheckResult.Unhealthy(
            $"{SentinelDescription} Host={SentinelHost};{SentinelPort}",
            new InvalidOperationException($"{SentinelException} Host={SentinelHost};{SentinelPort}"),
            new Dictionary<string, object> { ["detail"] = SentinelData });
        await using var host = new ReadyApiHostFactory(leak, HealthCheckResult.Healthy(), HealthCheckResult.Healthy());
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(ReadyRoute, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("title").GetString().ShouldBe("Service Unavailable");
        problem.GetProperty("status").GetInt32().ShouldBe(503);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        var checks = problem.GetProperty("checks");
        checks.GetArrayLength().ShouldBe(2);
        foreach (var check in checks.EnumerateArray())
        {
            PropertyNames(check).ShouldBe(["name", "status", "durationMs"], ignoreOrder: true);
        }

        CheckNamed(checks, PostgresCheck).GetProperty("status").GetString().ShouldBe(nameof(HealthStatus.Unhealthy));
        CheckNamed(checks, RabbitMqCheck).GetProperty("status").GetString().ShouldBe(nameof(HealthStatus.Healthy));
        body.ShouldNotContain(SentinelException);
        body.ShouldNotContain(SentinelDescription);
        body.ShouldNotContain(SentinelHost);
        body.ShouldNotContain(SentinelPort);
        body.ShouldNotContain(SentinelData);
        body.ShouldNotContain(MetadataCheck);
    }

    [Fact]
    public async Task Ready_WriterMappingNotProductionBehavior_DegradedCheck_Returns200()
    {
        // arrange
        await using var host = new ReadyApiHostFactory(
            HealthCheckResult.Degraded(),
            HealthCheckResult.Healthy(),
            HealthCheckResult.Healthy());
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(ReadyRoute, TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.GetProperty("status").GetString().ShouldBe(nameof(HealthStatus.Degraded));
        payload.GetProperty("checks").EnumerateArray().Select(check => check.GetProperty("name").GetString())
            .ShouldBe([PostgresCheck, RabbitMqCheck], ignoreOrder: true);
    }

    [Fact]
    public async Task Ready_RealRelayCheckDegraded_Returns200AndReportsDegraded()
    {
        // arrange
        await using var host = new ReadyApiHostFactory(
            HealthCheckResult.Healthy(),
            HealthCheckResult.Healthy(),
            HealthCheckResult.Healthy(),
            useRealRelayCheck: true);
        var sampledAt = new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
        var relayState = host.Services.GetRequiredService<OutboxRelayState>();
        relayState.MarkRunning();
        relayState.RecordPassSucceeded(1, sampledAt.AddMinutes(-10), sampledAt);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(ReadyRoute, TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.GetProperty("status").GetString().ShouldBe(nameof(HealthStatus.Degraded));
        CheckNamed(payload.GetProperty("checks"), OutboxRelayHealthCheck.HealthCheckName)
            .GetProperty("status").GetString().ShouldBe(nameof(HealthStatus.Degraded));
    }

    private static ReadyApiHostFactory CreateHost(string? failingCheck) =>
        new(
            Result(failingCheck, PostgresCheck),
            Result(failingCheck, RabbitMqCheck),
            Result(failingCheck, MetadataCheck));

    private static HealthCheckResult Result(string? failingCheck, string name) =>
        failingCheck == name ? HealthCheckResult.Unhealthy() : HealthCheckResult.Healthy();

    private static JsonElement CheckNamed(JsonElement checks, string name) =>
        checks.EnumerateArray().Single(check => check.GetProperty("name").GetString() == name);

    private static IEnumerable<string> PropertyNames(JsonElement element) =>
        element.EnumerateObject().Select(property => property.Name);

    private sealed class ReadyApiHostFactory(
        HealthCheckResult postgres,
        HealthCheckResult rabbitMq,
        HealthCheckResult metadataProvider,
        bool useRealRelayCheck = false) : WebApplicationFactory<Program>
    {
        private static readonly KeyValuePair<string, string?>[] HostSettings =
        [
            new($"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}", "C:/library"),
            new($"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}", "not-a-real-key"),
            new($"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}", "https://example.invalid/"),
            new($"{RabbitMqOptions.SectionName}:{nameof(RabbitMqOptions.HostName)}", "localhost"),
            new("ConnectionStrings:DefaultConnection", "Host=localhost;Port=5432;Database=lamuflix"),
            new($"{DatabaseOptions.SectionName}:{nameof(DatabaseOptions.ApplyMigrationsOnStartup)}", "false"),
            new($"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.ClaimLease)}", "00:00:10"),
            new($"{EnrichmentOptions.SectionName}:{nameof(EnrichmentOptions.SweepInterval)}", "00:01:00"),
            new($"{FeatureOptions.SectionName}:{nameof(FeatureOptions.LocalPlay)}", "false"),
            new("OTEL_EXPORTER_OTLP_TIMEOUT", "1000"),
        ];

        protected override IHost CreateHost(IHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.ConfigureHostConfiguration(static configuration => configuration.AddInMemoryCollection(HostSettings));
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            base.ConfigureWebHost(builder);
            builder.UseDefaultServiceProvider(static options =>
            {
                options.ValidateOnBuild = true;
                options.ValidateScopes = true;
            });
            builder.ConfigureTestServices(ReplaceChecksWithStubs);
        }

        private void ReplaceChecksWithStubs(IServiceCollection services)
        {
            services.Configure<HealthCheckServiceOptions>(static options => options.Registrations.Clear());
            services.AddHealthChecks()
                .AddCheck(PostgresCheck, new StubHealthCheck(postgres), tags: [HealthCheckTags.Ready])
                .AddCheck(RabbitMqCheck, new StubHealthCheck(rabbitMq), tags: [HealthCheckTags.Ready])
                .AddCheck(MetadataCheck, new StubHealthCheck(metadataProvider));
            if (useRealRelayCheck)
            {
                services.AddHealthChecks().AddCheck<OutboxRelayHealthCheck>(
                    OutboxRelayHealthCheck.HealthCheckName,
                    tags: [HealthCheckTags.Ready]);
            }

            RemoveSweeper(services);
            RemoveOutboxRelay(services);
            RemoveEmbeddingStartupProbe(services);
        }

        private static void RemoveOutboxRelay(IServiceCollection services)
        {
            var registration = services.FirstOrDefault(descriptor =>
                descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(OutboxRelayHostedService));
            if (registration is not null)
            {
                services.Remove(registration);
            }
        }

        private static void RemoveEmbeddingStartupProbe(IServiceCollection services)
        {
            var registration = services.FirstOrDefault(descriptor =>
                descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(OllamaEmbeddingStartupValidator));
            if (registration is not null)
            {
                services.Remove(registration);
            }
        }

        private static void RemoveSweeper(IServiceCollection services)
        {
            var registration = services.FirstOrDefault(descriptor =>
                descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(StrandedMovieSweeper));
            if (registration is not null)
            {
                services.Remove(registration);
            }
        }
    }
}
