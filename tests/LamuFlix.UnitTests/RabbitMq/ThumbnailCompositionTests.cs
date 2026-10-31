using System;
using System.Linq;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class ThumbnailCompositionTests
{
    [Fact]
    public void ThumbnailRequestedArguments_DeclareAQuorumQueueWithSingleActiveConsumerAndDeadLettering()
    {
        // arrange
        var arguments = RabbitMqTopology.ThumbnailRequestedArguments();

        // act
        var queueType = arguments["x-queue-type"];
        var singleActiveConsumer = arguments["x-single-active-consumer"];
        var deadLetterExchange = arguments["x-dead-letter-exchange"];
        var deadLetterRoutingKey = arguments["x-dead-letter-routing-key"];

        // assert
        queueType.ShouldBe("quorum");
        singleActiveConsumer.ShouldBe(true);
        deadLetterExchange.ShouldBe(RabbitMqTopology.ExchangeName);
        deadLetterRoutingKey.ShouldBe(RabbitMqTopology.ThumbnailDeadLetterQueue);
        arguments.Keys.ShouldNotContain("x-delivery-limit");
    }

    [Fact]
    public void ThumbnailRoutingKeys_MatchTheirQueueNames()
    {
        // arrange
        var requestedQueue = RabbitMqTopology.ThumbnailQueue;
        var deadLetterQueue = RabbitMqTopology.ThumbnailDeadLetterQueue;

        // act
        var requestedRoutingKey = RabbitMqTopology.ThumbnailRoutingKey;
        var deadLetterRoutingKey = RabbitMqTopology.ThumbnailDeadLetterRoutingKey;

        // assert
        requestedRoutingKey.ShouldBe(requestedQueue);
        deadLetterRoutingKey.ShouldBe(deadLetterQueue);
    }

    [Fact]
    public void AddLamuFlixRabbitMq_RegistersTheThumbnailCacheAndPublisherAsSingletons()
    {
        // arrange
        var services = NewServices(new ThumbnailOptions { Enabled = true });

        // act
        services.AddLamuFlixRabbitMq();

        // assert
        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(ThumbnailCache)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
        services.ShouldContain(descriptor =>
            descriptor.ServiceType == typeof(RabbitMqThumbnailQueuePublisher)
            && descriptor.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddLamuFlixRabbitMq_ResolvesOneThumbnailCacheInstanceAcrossScopes()
    {
        // arrange
        var services = NewServices(new ThumbnailOptions { Enabled = true });
        services.AddLamuFlixRabbitMq();

        // act
        using var provider = services.BuildServiceProvider();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();
        var fromFirst = first.ServiceProvider.GetRequiredService<ThumbnailCache>();
        var fromSecond = second.ServiceProvider.GetRequiredService<ThumbnailCache>();

        // assert
        fromFirst.ShouldBeSameAs(fromSecond);
        provider.GetRequiredService<IOptions<ThumbnailOptions>>().Value.Enabled.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false, typeof(InactiveThumbnailConsumer))]
    [InlineData(true, typeof(ThumbnailConsumer))]
    public void AddLamuFlixRabbitMq_UsesTheStartupThumbnailFlagToChooseTheConsumer(
        bool enabled,
        Type expected)
    {
        // arrange
        var services = NewServices(new ThumbnailOptions { Enabled = enabled });
        services.AddLamuFlixRabbitMq();

        // act
        using var provider = services.BuildServiceProvider();
        var selected = provider.GetServices<IHostedService>()
            .Where(service => service is ThumbnailConsumer or InactiveThumbnailConsumer)
            .ToArray();

        // assert
        selected.Length.ShouldBe(1);
        selected[0].GetType().ShouldBe(expected);
    }

    private static ServiceCollection NewServices(ThumbnailOptions thumbnailOptions)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IHostEnvironment>());
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(thumbnailOptions));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new RabbitMqOptions
        {
            HostName = "broker",
            RetryDelay = TimeSpan.FromSeconds(30),
        }));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions
        {
            MaxAttempts = 3,
            ClaimLease = TimeSpan.FromSeconds(5),
        }));
        return services;
    }
}
