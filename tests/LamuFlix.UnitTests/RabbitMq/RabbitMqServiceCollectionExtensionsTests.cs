using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Core.Pipeline;
using LamuFlix.Infrastructure.Pipeline;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.UnitTests.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class RabbitMqServiceCollectionExtensionsTests
{
    private static readonly TimeSpan Lease = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void AddLamuFlixRabbitMq_RegistersTheAlwaysPathAndGuardsTheConsumer(bool providerPort, bool repositoryPort)
    {
        // arrange
        var services = NewServices(providerPort, repositoryPort);

        // act
        services.AddLamuFlixRabbitMq();

        // assert
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(RabbitMqConnectionOwner));
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(RabbitMqTopology));
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IEnrichmentQueue));
        AssertConsumerPath(services, providerPort && repositoryPort);
    }

    [Fact]
    public async Task AddLamuFlixRabbitMq_WithBothPorts_ComposesTracingOutermost()
    {
        // arrange
        var services = NewServices(providerPort: true, repositoryPort: true);
        services.AddLamuFlixRabbitMq();

        // act
        await using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>>();

        // assert
        handler.ShouldBeOfType<TracingDecorator<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>>();
    }

    [Fact]
    public async Task AddLamuFlixRabbitMq_WithoutEitherPort_LogsExactlyOneInactiveLine()
    {
        // arrange
        var services = NewServices(providerPort: false, repositoryPort: false);
        services.AddLamuFlixRabbitMq();
        var logger = new RecordingLogger<EnrichmentConsumer>();
        services.AddSingleton<ILoggerFactory>(new SingleLoggerFactory(logger));

        // act
        await using var provider = services.BuildServiceProvider();
        foreach (var service in provider.GetServices<IHostedService>())
        {
            await service.StartAsync(CancellationToken.None);
        }

        // assert
        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].Level.ShouldBe(LogLevel.Information);
        logger.Entries[0].State.Single(pair => pair.Key == "Message").Value
            .ShouldBe(RabbitMqServiceCollectionExtensions.InactiveLogMessage);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task AddLamuFlixRabbitMq_EitherPath_RegistersTheReadyTaggedHealthCheck(
        bool providerPort,
        bool repositoryPort)
    {
        // arrange
        var services = NewServices(providerPort, repositoryPort);

        // act
        services.AddLamuFlixRabbitMq();

        // assert
        await using var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        var check = registrations.Single(registration => registration.Name == "rabbitmq");
        check.Tags.ShouldContain("ready");
    }

    [Fact]
    public void AddLamuFlixRabbitMq_InstallsTheW3CPropagator()
    {
        // arrange
        var services = NewServices(providerPort: false, repositoryPort: false);

        // act
        services.AddLamuFlixRabbitMq();

        // assert
        OpenTelemetry.Context.Propagation.Propagators.DefaultTextMapPropagator
            .ShouldBeOfType<OpenTelemetry.Context.Propagation.TraceContextPropagator>();
    }

    private static void AssertConsumerPath(ServiceCollection services, bool active)
    {
        if (active)
        {
            services.ShouldContain(descriptor => descriptor.ServiceType == typeof(ProcessEnrichmentCommandHandler));
            services.ShouldContain(
                descriptor => descriptor.ServiceType == typeof(ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>));
            services.ShouldContain(descriptor => descriptor.ServiceType == typeof(IValidateOptions<RabbitMqOptions>));
            return;
        }

        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(ProcessEnrichmentCommandHandler));
        services.ShouldNotContain(
            descriptor => descriptor.ServiceType == typeof(ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>));
        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(IValidateOptions<RabbitMqOptions>));
    }

    private static ServiceCollection NewServices(bool providerPort, bool repositoryPort)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton(ValidRabbitMqOptions());
        services.AddSingleton(ValidEnrichmentOptions());
        services.AddSingleton<IOptions<RabbitMqOptions>>(Microsoft.Extensions.Options.Options.Create(ValidRabbitMqOptions()));
        services.AddSingleton<IOptions<EnrichmentOptions>>(Microsoft.Extensions.Options.Options.Create(ValidEnrichmentOptions()));
        if (providerPort)
        {
            services.AddSingleton<IMetadataProvider, NoMetadataProvider>();
        }

        if (repositoryPort)
        {
            services.AddSingleton<IMovieRepository, NoMovieRepository>();
        }

        return services;
    }

    private static RabbitMqOptions ValidRabbitMqOptions() =>
        new() { HostName = "broker", RetryDelay = TimeSpan.FromSeconds(30) };

    private static EnrichmentOptions ValidEnrichmentOptions() =>
        new() { MaxAttempts = 3, ClaimLease = Lease };

    private sealed class NoMetadataProvider : IMetadataProvider
    {
        public Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct) =>
            Task.FromResult<MetadataLookupResult>(new MetadataLookupResult.NotFound());
    }

    private sealed class NoMovieRepository : IMovieRepository
    {
        public Task<Movie?> GetAsync(MovieId id, CancellationToken ct) => Task.FromResult<Movie?>(null);

        public Task AddAsync(Movie movie, CancellationToken ct) => Task.CompletedTask;

        public Task<MovieId> NextIdentityAsync(CancellationToken ct) => Task.FromResult(new MovieId(1));

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct) => Task.FromResult(false);
    }

    private sealed class SingleLoggerFactory(RecordingLogger<EnrichmentConsumer> logger) : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName) => logger;

        public void Dispose()
        {
        }
    }
}


