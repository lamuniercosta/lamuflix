using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using LamuFlix.Infrastructure.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(MetadataProviderCollection.Name)]
public sealed class MetadataProviderCompositionTests(MetadataProviderProbe probe) : IClassFixture<MetadataProviderProbe>, IAsyncLifetime
{
    private const string FoundBody = """
        {
          "Response": "True",
          "Title": "Solaris",
          "Year": "1972",
          "Runtime": "167 min",
          "Plot": "A psychologist...",
          "imdbRating": "8.0",
          "imdbID": "tt0069293"
        }
        """;

    private const string KeyAdvice = "Check the OMDb API key configuration.";

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public void AddMetadataProvider_ResolvesTheCorePortAsTheTypedClient()
    {
        // arrange
        var services = probe.BuildServices();

        // act
        var provider = services.GetRequiredService<IMetadataProvider>();

        // assert
        provider.ShouldBeOfType<OmdbMetadataProvider>();
    }

    [Fact]
    public void AddMetadataProvider_WithoutPersistence_StillResolvesATimeProvider()
    {
        // arrange
        var services = probe.NewServices();
        services.AddMetadataProvider();

        // act
        using var provider = services.BuildServiceProvider();

        // assert
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddMetadataProvider_WithAPreRegisteredClock_KeepsThePreRegisteredOne()
    {
        // arrange
        var fake = new FixedTimeProvider(new DateTimeOffset(1999, 12, 31, 23, 59, 58, TimeSpan.Zero));
        var services = probe.NewServices(clock: fake);
        services.AddMetadataProvider();

        // act
        using var provider = services.BuildServiceProvider();

        // assert
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(fake);
    }

    [Fact]
    public void AddMetadataProvider_WithAMissingApiKey_FailsStartupValidation()
    {
        // arrange
        var overrides = new Dictionary<string, string?>
        {
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.ApiKey)}"] = string.Empty,
        };

        // act
        var failure = StartupFailure(overrides);

        // assert
        failure.Message.ShouldContain(nameof(OmdbOptions.ApiKey));
    }

    [Fact]
    public void AddMetadataProvider_WithAnAttemptTimeoutAboveTheTotal_FailsStartupValidation()
    {
        // arrange
        var overrides = new Dictionary<string, string?>
        {
            [MetadataProviderProbe.Resilience(nameof(MetadataProviderResilienceOptions.AttemptTimeout))] = "00:00:05",
        };

        // act
        var failure = StartupFailure(overrides);

        // assert
        failure.Message.ShouldContain(nameof(MetadataProviderResilienceOptions.AttemptTimeout));
    }

    [Fact]
    public void AddMetadataProvider_WithSamplingBelowTwiceTheAttempt_FailsStartupValidation()
    {
        // arrange
        var overrides = new Dictionary<string, string?>
        {
            [MetadataProviderProbe.Resilience(nameof(MetadataProviderResilienceOptions.SamplingDuration))] = "00:00:00.900",
        };

        // act
        var failure = StartupFailure(overrides);

        // assert
        failure.Message.ShouldContain(nameof(MetadataProviderResilienceOptions.SamplingDuration));
    }

    [Fact]
    public void AddMetadataProvider_RegistersTheUntaggedHealthCheck()
    {
        // arrange
        var services = probe.BuildServices();

        // act
        var registrations = services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        // assert
        var check = registrations.Single(registration => registration.Name == MetadataProviderProbe.CheckName);
        check.Tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task MetadataProviderHealthCheck_BeforeAnyLookup_IsHealthy()
    {
        // arrange
        var services = probe.BuildServices();

        // act
        var entry = await CheckAsync(services);

        // assert
        entry.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task MetadataProviderHealthCheck_AfterFound_IsHealthy()
    {
        // arrange
        StubBody(200, FoundBody);
        var services = probe.BuildServices();

        // act
        await FindAsync(services);
        var entry = await CheckAsync(services);

        // assert
        entry.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task MetadataProviderHealthCheck_AfterAProviderUnavailable_IsDegraded()
    {
        // arrange
        StubBody(500);
        var services = probe.BuildServices();

        // act
        await FindAsync(services);
        var entry = await CheckAsync(services);

        // assert
        entry.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task MetadataProviderHealthCheck_AfterARateLimited_IsDegraded()
    {
        // arrange
        StubBody(429);
        var services = probe.BuildServices();

        // act
        await FindAsync(services);
        var entry = await CheckAsync(services);

        // assert
        entry.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task MetadataProviderHealthCheck_AfterAMalformedBody_IsDegradedRatherThanUnhealthy()
    {
        // arrange
        StubBody(200, "{ \"Response\": ");
        var services = probe.BuildServices();

        // act
        await FindAsync(services);
        var entry = await CheckAsync(services);

        // assert
        entry.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task MetadataProviderHealthCheck_AfterAnUnauthorized_IsUnhealthyWithTheKeyAdvice()
    {
        // arrange
        StubBody(401);
        var services = probe.BuildServices();

        // act
        await FindAsync(services);
        var entry = await CheckAsync(services);

        // assert
        entry.Status.ShouldBe(HealthStatus.Unhealthy);
        entry.Description.ShouldBe(KeyAdvice);
    }

    [Fact]
    public void ProviderRegisteredBeforeRabbitMq_ActivatesTheEnrichmentConsumer()
    {
        // arrange
        var services = probe.NewServices();
        services.AddSingleton<IMovieRepository, UnusedMovieRepository>();
        services.AddMetadataProvider();

        // act
        services.AddLamuFlixRabbitMq();

        // assert
        AssertConsumerPath(services, active: true);
    }

    [Fact]
    public void ProviderRegisteredAfterRabbitMq_LeavesTheEnrichmentConsumerInactive()
    {
        // arrange
        var services = probe.NewServices();
        services.AddSingleton<IMovieRepository, UnusedMovieRepository>();
        services.AddLamuFlixRabbitMq();

        // act
        services.AddMetadataProvider();

        // assert
        AssertConsumerPath(services, active: false);
    }

    private static void AssertConsumerPath(ServiceCollection services, bool active)
    {
        var hostsConsumer = services.Any(descriptor =>
            descriptor.ServiceType == typeof(IHostedService) && descriptor.ImplementationType == typeof(EnrichmentConsumer));
        var hasHandler = services.Any(descriptor =>
            descriptor.ServiceType == typeof(ProcessEnrichmentCommandHandler));

        hostsConsumer.ShouldBe(active);
        hasHandler.ShouldBe(active);
    }

    private OptionsValidationException StartupFailure(IDictionary<string, string?> overrides)
    {
        var services = probe.NewServices(overrides);
        using var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IStartupValidator>().ToArray();
        validators.ShouldNotBeEmpty();
        return Should.Throw<OptionsValidationException>(() =>
        {
            foreach (var validator in validators)
            {
                validator.Validate();
            }
        });
    }

    private static async Task<HealthReportEntry> CheckAsync(IServiceProvider services) =>
        (await services
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(
                registration => registration.Name == MetadataProviderProbe.CheckName,
                TestContext.Current.CancellationToken))
            .Entries[MetadataProviderProbe.CheckName];

    private static Task FindAsync(IServiceProvider services) =>
        services
            .GetRequiredService<IMetadataProvider>()
            .FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);

    private void StubBody(int statusCode, string body = "") =>
        probe.Server
            .Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(statusCode).WithBody(body));

    private sealed class UnusedMovieRepository : IMovieRepository
    {
        public Task<Movie?> GetAsync(MovieId id, CancellationToken ct) => Task.FromResult<Movie?>(null);

        public Task AddAsync(Movie movie, CancellationToken ct) => Task.CompletedTask;

        public Task<MovieId> NextIdentityAsync(CancellationToken ct) => Task.FromResult(new MovieId(1));

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct) => Task.FromResult(false);
    }
}
