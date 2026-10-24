using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LamuFlix.UnitTests.RabbitMq;

/// <summary>
/// DEV-414 T013: the features consumer activates only when its own four prerequisites exist, it is
/// independent of IMetadataProvider, and an incomplete host logs inactive without resolving a
/// missing dependency.
/// </summary>
public sealed class FeaturesConsumerActivationTests
{
    private static readonly TimeSpan Lease = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task AddLamuFlixRabbitMq_WithAllFeaturePrerequisites_RegistersTheConsumerAndReadinessWithoutMetadata()
    {
        // arrange
        var services = NewServices(movieRepository: true, featuresRepository: true, embedding: true, metadata: false);

        // act
        services.AddLamuFlixRabbitMq();

        // assert
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(ProcessFeaturesCommandHandler));
        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(ICommandHandler<ProcessFeaturesCommand, ProcessFeaturesOutcome>));
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(SweepMissingMovieFeaturesCommandHandler));
        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(ICommandHandler<SweepMissingMovieFeaturesCommand, int>));
        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(FeaturesConsumerLiveness));
        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(IHostedService)
            && descriptor.ImplementationType == typeof(FeaturesConsumer));
        services.ShouldNotContain(descriptor => descriptor.ImplementationType == typeof(EnrichmentConsumer));

        await using var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        var features = registrations.SingleOrDefault(registration => registration.Name == "features-consumer");
        features.ShouldNotBeNull();
        features.Tags.ShouldContain(HealthCheckTags.Ready);
        registrations.Any(registration => registration.Name == "enrichment-consumer").ShouldBeFalse();

        using var scope = provider.CreateScope();
        scope.ServiceProvider
            .GetRequiredService<ICommandHandler<ProcessFeaturesCommand, ProcessFeaturesOutcome>>()
            .ShouldBeOfType<TracingDecorator<ProcessFeaturesCommand, ProcessFeaturesOutcome>>();
        scope.ServiceProvider
            .GetRequiredService<ICommandHandler<SweepMissingMovieFeaturesCommand, int>>()
            .ShouldBeOfType<TracingDecorator<SweepMissingMovieFeaturesCommand, int>>();
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public async Task AddLamuFlixRabbitMq_WithAMissingFeaturePrerequisite_DoesNotRegisterTheConsumer(
        bool movieRepository,
        bool featuresRepository,
        bool embedding)
    {
        // arrange
        var services = NewServices(movieRepository, featuresRepository, embedding, metadata: true);

        // act
        services.AddLamuFlixRabbitMq();

        // assert
        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(ProcessFeaturesCommandHandler));
        services.ShouldNotContain(descriptor =>
            descriptor.ServiceType == typeof(ICommandHandler<ProcessFeaturesCommand, ProcessFeaturesOutcome>));
        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(SweepMissingMovieFeaturesCommandHandler));
        services.ShouldNotContain(descriptor =>
            descriptor.ServiceType == typeof(ICommandHandler<SweepMissingMovieFeaturesCommand, int>));
        services.ShouldNotContain(descriptor => descriptor.ServiceType == typeof(FeaturesConsumerLiveness));
        services.ShouldNotContain(descriptor => descriptor.ImplementationType == typeof(FeaturesConsumer));

        await using var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;
        registrations.Any(registration => registration.Name == "features-consumer").ShouldBeFalse();
        provider.GetService<ProcessFeaturesCommandHandler>().ShouldBeNull();
        provider.GetService<SweepMissingMovieFeaturesCommandHandler>().ShouldBeNull();
    }

    [Fact]
    public async Task AddLamuFlixRabbitMq_IncompleteHost_LogsFeaturesInactiveWithoutResolvingMissingDependencies()
    {
        // arrange
        var services = NewServices(movieRepository: true, featuresRepository: true, embedding: false, metadata: true);
        services.AddLamuFlixRabbitMq();
        var logger = new RecordingLogger<FeaturesConsumer>();
        services.AddSingleton<ILoggerFactory>(new SingleLoggerFactory(logger));

        // act
        await using var provider = services.BuildServiceProvider();
        foreach (var service in provider.GetServices<IHostedService>())
        {
            await service.StartAsync(CancellationToken.None);
        }

        // assert
        logger.Entries.Count.ShouldBe(1);
        logger.Entries[0].State.Single(pair => pair.Key == "Message").Value
            .ShouldBe(RabbitMqServiceCollectionExtensions.InactiveFeaturesLogMessage);
        provider.GetService<ProcessFeaturesCommandHandler>().ShouldBeNull();
        provider.GetService<IEmbeddingGenerator<string, Embedding<float>>>().ShouldBeNull();
    }

    private static ServiceCollection NewServices(
        bool movieRepository,
        bool featuresRepository,
        bool embedding,
        bool metadata)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        var enrichment = new EnrichmentOptions { MaxAttempts = 3, ClaimLease = Lease };
        services.AddSingleton(enrichment);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(enrichment));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new RabbitMqOptions
        {
            HostName = "broker",
            RetryDelay = TimeSpan.FromSeconds(30),
        }));
        if (movieRepository)
        {
            services.AddSingleton<IMovieRepository, NoMovieRepository>();
        }

        if (featuresRepository)
        {
            services.AddSingleton<IMovieFeaturesRepository, NoMovieFeaturesRepository>();
        }

        if (embedding)
        {
            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(new FakeEmbeddingGenerator());
        }

        if (metadata)
        {
            services.AddSingleton<IMetadataProvider, NoMetadataProvider>();
        }

        return services;
    }

    private sealed class NoMetadataProvider : IMetadataProvider
    {
        public Task<MetadataLookupResult> FindAsync(MetadataLookup lookup, CancellationToken ct) =>
            Task.FromResult<MetadataLookupResult>(new MetadataLookupResult.NotFound());
    }

    private sealed class NoMovieRepository : IMovieRepository
    {
        public Task<Movie?> GetAsync(MovieId id, CancellationToken ct) => Task.FromResult<Movie?>(null);

        public Task AddAsync(Movie movie, CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TryDeleteAsync(MovieId id, CancellationToken ct) => Task.FromResult(false);

        public Task<MovieId> NextIdentityAsync(CancellationToken ct) => Task.FromResult(new MovieId(1));

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

        public Task<bool> TryClaimForEnrichmentAsync(MovieId id, CancellationToken ct) => Task.FromResult(false);

        public Task<MovieId?> FindMovieIdByImdbIdAsync(ImdbId imdbId, CancellationToken ct) =>
            Task.FromResult<MovieId?>(null);

        public Task<IReadOnlyList<MovieId>> FindStrandedMovieIdsAsync(DateTimeOffset leaseCutoff, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<MovieId>>([]);

        public Task<string?> GetPlotAsync(MovieId id, CancellationToken ct) => Task.FromResult<string?>(null);
    }

    private sealed class NoMovieFeaturesRepository : IMovieFeaturesRepository
    {
        public Task<MovieFeaturesSnapshot?> GetByMovieIdAsync(
            MovieId movieId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<MovieFeaturesSnapshot?>(null);

        public Task UpsertAsync(MovieFeaturesSnapshot features, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<MovieId>> FindMovieIdsMissingFeaturesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<MovieId>>([]);
    }

    private sealed class SingleLoggerFactory(RecordingLogger<FeaturesConsumer> logger) : ILoggerFactory
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
