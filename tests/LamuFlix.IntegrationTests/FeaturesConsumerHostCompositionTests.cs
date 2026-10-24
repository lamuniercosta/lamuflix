using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.RabbitMq;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

/// <summary>
/// DEV-414 T022: a minimal host that misses a feature prerequisite starts the inactive features consumer,
/// logs why, never resolves the missing dependency, and leaves metadata consumer activation unchanged.
/// </summary>
public sealed class FeaturesConsumerHostCompositionTests
{
    private static readonly TimeSpan Lease = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task MinimalHost_MissingEmbeddingGenerator_StartsInactiveFeatureConsumerWithoutResolvingIt()
    {
        // arrange
        var recorder = new MetadataProviderProbe.RecordingLoggerFactory();
        var builder = new HostBuilder();
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<ILoggerFactory>(recorder);
            services.AddLogging();
            services.AddSingleton(TimeProvider.System);
            var enrichment = new EnrichmentOptions { MaxAttempts = 3, ClaimLease = Lease };
            services.AddSingleton(enrichment);
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(enrichment));
            services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new RabbitMqOptions
            {
                HostName = "localhost",
                RetryDelay = RetryDelay,
            }));
            services.AddSingleton(Substitute.For<IMovieRepository>());
            services.AddSingleton(Substitute.For<IMovieFeaturesRepository>());
            services.AddSingleton(Substitute.For<IMetadataProvider>());
            services.AddLamuFlixRabbitMq();
        });
        using var host = builder.Build();
        var hostedServices = host.Services.GetServices<IHostedService>().ToArray();

        // act
        await host.StartAsync(TestContext.Current.CancellationToken);
        await host.StopAsync(TestContext.Current.CancellationToken);

        // assert
        hostedServices.ShouldContain(service => service is EnrichmentConsumer);
        hostedServices.ShouldNotContain(service => service is FeaturesConsumer);
        host.Services.GetService<ProcessFeaturesCommandHandler>().ShouldBeNull();
        host.Services.GetService<SweepMissingMovieFeaturesCommandHandler>().ShouldBeNull();
        host.Services.GetService<FeaturesConsumerLiveness>().ShouldBeNull();
        host.Services.GetService<IEmbeddingGenerator<string, Embedding<float>>>().ShouldBeNull();
        recorder.Entries.ShouldContain(entry =>
            entry.Message == RabbitMqServiceCollectionExtensions.InactiveFeaturesLogMessage);
        recorder.Entries.ShouldNotContain(entry =>
            entry.Message == RabbitMqServiceCollectionExtensions.InactiveLogMessage);
    }
}
