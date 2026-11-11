using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.RabbitMq;
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class ThumbnailResolutionSettlementTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task HandleAsync_ScannerIOException_RequeuesWithoutGenerationPromotionOrTerminalReject()
    {
        // arrange
        var movieId = new MovieId(9);
        var movie = Movie.Create(movieId, "A", new LibraryPath("library/movies/a"), new MediaFormat("mkv"));
        var repository = Substitute.For<IMovieRepository>();
        repository.GetAsync(movieId, Arg.Any<CancellationToken>()).Returns(movie);
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(_ => throw new IOException("scanner unavailable"));
        var generator = Substitute.For<IThumbnailGenerator>();
        var cacheDirectory = Path.Combine(Path.GetTempPath(), $"lamuflix-thumbnails-{Guid.NewGuid():N}");
        var cache = new ThumbnailCache(
            Microsoft.Extensions.Options.Options.Create(new ThumbnailOptions { CacheDirectory = cacheDirectory }),
            Substitute.For<IHostEnvironment>());
        var consumer = NewConsumer(repository, scanner, generator, cache);
        var channel = Substitute.For<IChannel>();
        var delivery = NewDelivery(new ThumbnailRequested(movieId, "library/movies/a"));

        // act
        await InvokeHandleAsync(consumer, channel, delivery);

        // assert
        await channel.Received(1).BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, Arg.Any<CancellationToken>());
        await channel.DidNotReceive().BasicAckAsync(
            Arg.Any<ulong>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
        await channel.DidNotReceive().BasicRejectAsync(
            Arg.Any<ulong>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
        await generator.DidNotReceive().GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        File.Exists(Path.Combine(cacheDirectory, "9.jpg")).ShouldBeFalse();
    }

    private static ThumbnailConsumer NewConsumer(
        IMovieRepository repository,
        IMediaLibraryScanner scanner,
        IThumbnailGenerator generator,
        ThumbnailCache cache)
    {
        var rabbit = Microsoft.Extensions.Options.Options.Create(new RabbitMqOptions { HostName = "localhost", Prefetch = 1 });
        var enrichment = Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { MaxAttempts = 3, ClaimLease = TimeSpan.FromMinutes(1) });
        var owner = new RabbitMqConnectionOwner(rabbit);
        var topology = new RabbitMqTopology(owner, rabbit, enrichment);
        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton(scanner);
        var provider = services.BuildServiceProvider();
        return new ThumbnailConsumer(
            owner,
            topology,
            provider.GetRequiredService<IServiceScopeFactory>(),
            rabbit,
            Microsoft.Extensions.Options.Options.Create(new LibraryOptions { RootPath = "library" }),
            generator,
            cache,
            Substitute.For<IHostEnvironment>(),
            NullLogger<ThumbnailConsumer>.Instance);
    }

    private static BasicDeliverEventArgs NewDelivery(ThumbnailRequested request)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(request, SerializerOptions);
        return new BasicDeliverEventArgs(
            "test",
            1,
            false,
            RabbitMqTopology.ExchangeName,
            RabbitMqTopology.ThumbnailQueue,
            new BasicProperties(),
            body,
            CancellationToken.None);
    }

    private static async Task InvokeHandleAsync(ThumbnailConsumer consumer, IChannel channel, BasicDeliverEventArgs delivery)
    {
        var method = typeof(ThumbnailConsumer).GetMethod("HandleAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull();
        var invocation = method.Invoke(consumer, [channel, delivery, CancellationToken.None]);
        invocation.ShouldNotBeNull();
        await (Task)invocation;
    }
}
