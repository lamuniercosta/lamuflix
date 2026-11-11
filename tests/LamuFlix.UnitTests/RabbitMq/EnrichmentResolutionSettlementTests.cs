using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Enrichment;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class EnrichmentResolutionSettlementTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task HandleAsync_ScannerIOException_SkipsThumbnailPublishAndAcknowledges()
    {
        // arrange
        var movieId = new MovieId(7);
        var movie = Movie.Create(movieId, "A", new LibraryPath("library/movies/a"), new MediaFormat("mkv"));
        var repository = Substitute.For<IMovieRepository>();
        repository.GetAsync(movieId, Arg.Any<CancellationToken>()).Returns(movie);
        var scanner = Substitute.For<IMediaLibraryScanner>();
        scanner.ResolvePrimaryVideo(Arg.Any<LibraryPath>()).Returns(_ => throw new IOException("scanner unavailable"));
        var consumer = NewConsumer(repository, scanner);
        var channel = Substitute.For<IChannel>();
        var delivery = NewDelivery(new EnrichmentRequested(movieId, 1));

        // act
        await InvokeHandleAsync(consumer, channel, delivery);

        // assert
        await channel.Received(1).BasicAckAsync(delivery.DeliveryTag, multiple: false, Arg.Any<CancellationToken>());
        await channel.DidNotReceive().BasicNackAsync(
            Arg.Any<ulong>(),
            Arg.Any<bool>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
        scanner.Received(1).ResolvePrimaryVideo(Arg.Any<LibraryPath>());
    }

    [Fact]
    public async Task HandleAsync_RepositoryIOException_DeadLettersWithoutAcknowledging()
    {
        // arrange
        var movieId = new MovieId(7);
        var repository = Substitute.For<IMovieRepository>();
        repository.GetAsync(movieId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Movie?>(new IOException("repository unavailable")));
        var scanner = Substitute.For<IMediaLibraryScanner>();
        var consumer = NewConsumer(repository, scanner);
        var channel = Substitute.For<IChannel>();
        var delivery = NewDelivery(new EnrichmentRequested(movieId, 1));

        // act
        await InvokeHandleAsync(consumer, channel, delivery);

        // assert
        await channel.Received(1).BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, Arg.Any<CancellationToken>());
        await channel.DidNotReceive().BasicAckAsync(
            Arg.Any<ulong>(),
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    private static EnrichmentConsumer NewConsumer(IMovieRepository repository, IMediaLibraryScanner scanner)
    {
        var rabbit = Microsoft.Extensions.Options.Options.Create(new RabbitMqOptions { HostName = "localhost", Prefetch = 1 });
        var enrichment = Microsoft.Extensions.Options.Options.Create(new EnrichmentOptions { MaxAttempts = 3, ClaimLease = TimeSpan.FromMinutes(1) });
        var owner = new RabbitMqConnectionOwner(rabbit);
        var topology = new RabbitMqTopology(owner, rabbit, enrichment);
        var handler = Substitute.For<ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>>();
        handler.HandleAsync(Arg.Any<ProcessEnrichmentCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessEnrichmentOutcome.Completed(Claimed: true));
        var services = new ServiceCollection();
        services.AddSingleton(handler);
        services.AddSingleton(repository);
        services.AddSingleton(scanner);
        var provider = services.BuildServiceProvider();
        return new EnrichmentConsumer(
            owner,
            topology,
            new RabbitMqEnrichmentQueuePublisher(topology, owner),
            provider.GetRequiredService<IServiceScopeFactory>(),
            rabbit,
            Microsoft.Extensions.Options.Options.Create(new ThumbnailOptions { Enabled = true }),
            new RabbitMqThumbnailQueuePublisher(topology, owner),
            NullLogger<EnrichmentConsumer>.Instance,
            NoOpLiveness.Instance,
            TimeProvider.System);
    }

    private static BasicDeliverEventArgs NewDelivery(EnrichmentRequested request)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(request, SerializerOptions);
        return new BasicDeliverEventArgs(
            "test",
            1,
            false,
            RabbitMqTopology.ExchangeName,
            RabbitMqTopology.RequestedQueue,
            new BasicProperties(),
            body,
            CancellationToken.None);
    }

    private static async Task InvokeHandleAsync(EnrichmentConsumer consumer, IChannel channel, BasicDeliverEventArgs delivery)
    {
        var method = typeof(EnrichmentConsumer).GetMethod("HandleAsync", BindingFlags.Instance | BindingFlags.NonPublic);
        method.ShouldNotBeNull();
        var invocation = method.Invoke(consumer, [channel, delivery, CancellationToken.None]);
        invocation.ShouldNotBeNull();
        await (Task)invocation;
    }

    private sealed class NoOpLiveness : IWorkerLiveness
    {
        public static readonly NoOpLiveness Instance = new();

        public bool IsListening => false;

        public void MarkListening()
        {
            // tests that do not observe liveness still construct a consumer
        }

        public void MarkStopped()
        {
            // tests that do not observe liveness still construct a consumer
        }
    }
}
