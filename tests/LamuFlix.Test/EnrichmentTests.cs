using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Data;
using LamuFlix.Data.Models;
using LamuFlix.Tests.Common;
using LamuFlix.Web.Services;
using LamuFlix.Worker.Models;
using LamuFlix.Worker.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RabbitMQ.Client;
using Shouldly;
using Xunit;

[assembly: AssemblyFixture(typeof(LamuFlix.Test.ContainerFixtureForTests))]

namespace LamuFlix.Test;

public sealed class ContainerFixtureForTests : IAsyncLifetime
{
    public ValueTask InitializeAsync() => new(ContainerFixture.StartAsync());

    public ValueTask DisposeAsync() => ContainerFixture.DisposeAsync();
}

public sealed class EnrichmentTests
{
    private class TestHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(response);
        }
    }

    private static IConfiguration CreateOmdbConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Movies:OMDB_API_KEY"] = "test-key" })
            .Build();

    [Fact]
    public async Task OmdbMetadataProvider_Success_ReturnsMetadata()
    {
        var json = "{\"Response\":\"True\",\"Title\":\"Test Movie\",\"Plot\":\"Great plot\",\"Year\":\"2020\",\"Runtime\":\"120 min\",\"Poster\":\"http://poster\",\"imdbRating\":\"8.0\",\"Metascore\":\"85\",\"imdbID\":\"tt123\",\"Genre\":\"Action\",\"Director\":\"Director X\",\"Actors\":\"Actor Y\",\"Ratings\":[{\"Source\":\"Rotten Tomatoes\",\"Value\":\"90%\"}]}";
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var httpClient = new HttpClient(handler);
        var config = CreateOmdbConfig();
        var provider = new OmdbMetadataProvider(httpClient, config);

        var meta = await provider.FetchMetadataAsync("Test Movie", cancellationToken: TestContext.Current.CancellationToken);

        meta.ShouldNotBeNull();
        meta.Title.ShouldBe("Test Movie");
        meta.Year.ShouldBe(2020);
        meta.DurationMinutes.ShouldBe(120);
        meta.ImdbRating.ShouldBe(0.8m);
        meta.RottenTomatoesScore.ShouldBe(90);
        meta.MetaScore.ShouldBe(85);
        meta.ImdbId.ShouldBe("tt123");
    }

    [Fact]
    public async Task OmdbMetadataProvider_NotFound_ReturnsNull()
    {
        var json = "{\"Response\":\"False\",\"Error\":\"Movie not found!\"}";
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var httpClient = new HttpClient(handler);
        var config = CreateOmdbConfig();
        var provider = new OmdbMetadataProvider(httpClient, config);

        var meta = await provider.FetchMetadataAsync("Unknown", cancellationToken: TestContext.Current.CancellationToken);

        meta.ShouldBeNull();
    }

    [Fact]
    public async Task OmdbMetadataProvider_ServerError_ThrowsException()
    {
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler);
        var config = CreateOmdbConfig();
        var provider = new OmdbMetadataProvider(httpClient, config);

        await Should.ThrowAsync<HttpRequestException>(() => provider.FetchMetadataAsync("Error Movie", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public void OmdbMetadataProvider_MissingApiKey_ThrowsInvalidOperationException()
    {
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var config = new ConfigurationBuilder().Build();

        var ex = Should.Throw<InvalidOperationException>(() => new OmdbMetadataProvider(httpClient, config));
        ex.Message.Contains("Movies:OMDB_API_KEY").ShouldBeTrue();
    }

    private static IServiceProvider BuildProvider(LamuFlixContext context)
    {
        var services = new ServiceCollection();
        services.AddSingleton(context);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task EnrichmentJobProcessor_TransitionsToEnriched()
    {
        var db = LamuFlixContextFactory.CreateContext();
        var provider = BuildProvider(db);
        var movie = new Movie
        {
            Title = "Matrix",
            Year = 1999,
            Location = "F:\\Matrix.mkv",
            Format = ".mkv",
            Status = MovieEnrichmentStatus.Pending
        };
        db.Movies.Add(movie);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var metadataProvider = Substitute.For<IMetadataProvider>();
        metadataProvider
            .FetchMetadataAsync("Matrix", 1999, null, Arg.Any<CancellationToken>())
            .Returns(new MovieMetadata("Matrix", "Sci-Fi action", 1999, 136, "http://poster", 8.7m, 88, 73, "tt0133093", new[] { "Action" }, new[] { "Keanu Reeves" }, new[] { "Wachowskis" }));

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);
        scopeFactory.CreateScope().Returns(scope);

        var config = new ConfigurationBuilder().Build();
        var processor = new EnrichmentJobProcessor(scopeFactory, metadataProvider, config, NullLogger<EnrichmentJobProcessor>.Instance);

        var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Matrix", Year = 1999 };

        await RunAndAssertAckedAsync(processor, message);

        var updatedMovie = await db.Movies.SingleAsync(x => x.Id == movie.Id, TestContext.Current.CancellationToken);
        updatedMovie.Status.ShouldBe(MovieEnrichmentStatus.Enriched);
        updatedMovie.Synopsis.ShouldBe("Sci-Fi action");
    }

    [Fact]
    public async Task EnrichmentJobProcessor_TransitionsToNotFound_RowRemainsVisible()
    {
        var db = LamuFlixContextFactory.CreateContext();
        var provider = BuildProvider(db);
        var movie = new Movie
        {
            Title = "Missing Movie",
            Year = 2021,
            Location = "F:\\Missing.mp4",
            Format = ".mp4",
            Status = MovieEnrichmentStatus.Pending
        };
        db.Movies.Add(movie);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var metadataProvider = Substitute.For<IMetadataProvider>();
        metadataProvider
            .FetchMetadataAsync("Missing Movie", 2021, null, Arg.Any<CancellationToken>())
            .Returns((MovieMetadata?)null);

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);
        scopeFactory.CreateScope().Returns(scope);

        var config = new ConfigurationBuilder().Build();
        var processor = new EnrichmentJobProcessor(scopeFactory, metadataProvider, config, NullLogger<EnrichmentJobProcessor>.Instance);

        var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Missing Movie", Year = 2021 };

        await RunAndAssertAckedAsync(processor, message);

        var updatedMovie = await db.Movies.SingleAsync(x => x.Id == movie.Id, TestContext.Current.CancellationToken);
        updatedMovie.Status.ShouldBe(MovieEnrichmentStatus.NotFound);
        updatedMovie.ShouldNotBeNull();
    }

    [Fact]
    public async Task EnrichmentJobProcessor_Idempotency_SkipsAlreadyEnriched()
    {
        var db = LamuFlixContextFactory.CreateContext();
        var provider = BuildProvider(db);
        var movie = new Movie
        {
            Title = "Already Enriched",
            Year = 2020,
            Location = "F:\\Test.mkv",
            Format = ".mkv",
            Status = MovieEnrichmentStatus.Enriched
        };
        db.Movies.Add(movie);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var metadataProvider = Substitute.For<IMetadataProvider>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);
        scopeFactory.CreateScope().Returns(scope);

        var config = new ConfigurationBuilder().Build();
        var processor = new EnrichmentJobProcessor(scopeFactory, metadataProvider, config, NullLogger<EnrichmentJobProcessor>.Instance);

        var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Already Enriched", Year = 2020 };

        await RunAndAssertAckedAsync(processor, message);

        metadataProvider.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task EnrichmentJobProcessor_RetryOnFailure_RepublishesWhenRetryCountBelowThree()
    {
        var db = LamuFlixContextFactory.CreateContext();
        var provider = BuildProvider(db);
        var movie = new Movie
        {
            Title = "Failing Movie",
            Year = 2022,
            Location = "F:\\Fail.mp4",
            Format = ".mp4",
            Status = MovieEnrichmentStatus.Pending
        };
        db.Movies.Add(movie);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var metadataProvider = Substitute.For<IMetadataProvider>();
        metadataProvider
            .FetchMetadataAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);
        scopeFactory.CreateScope().Returns(scope);

        var config = new ConfigurationBuilder().Build();
        var processor = new EnrichmentJobProcessor(scopeFactory, metadataProvider, config, NullLogger<EnrichmentJobProcessor>.Instance);

        var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Failing Movie", RetryCount = 0 };

        await RunAndAssertRepublishedAsync(processor, message, "task_queue", 1);

        message.RetryCount.ShouldBe(1);
    }

    [Fact]
    public async Task EnrichmentJobProcessor_ExceedsMaxRetries_MarksFailedAndForwardsToDlq()
    {
        var db = LamuFlixContextFactory.CreateContext();
        var provider = BuildProvider(db);
        var movie = new Movie
        {
            Title = "Dead Letter Movie",
            Year = 2022,
            Location = "F:\\DLQ.mp4",
            Format = ".mp4",
            Status = MovieEnrichmentStatus.Pending
        };
        db.Movies.Add(movie);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var metadataProvider = Substitute.For<IMetadataProvider>();
        metadataProvider
            .FetchMetadataAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Persistent error"));

        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(provider);
        scopeFactory.CreateScope().Returns(scope);

        var config = new ConfigurationBuilder().Build();
        var processor = new EnrichmentJobProcessor(scopeFactory, metadataProvider, config, NullLogger<EnrichmentJobProcessor>.Instance);

        var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Dead Letter Movie", RetryCount = 3 };

        await RunAndAssertRepublishedAsync(processor, message, "task_queue_dlq", 3);

        var updatedMovie = await db.Movies.SingleAsync(x => x.Id == movie.Id, TestContext.Current.CancellationToken);
        updatedMovie.Status.ShouldBe(MovieEnrichmentStatus.Failed);
    }

    [Fact]
    public void FilmesService_CriarFilme_InsertsAsPendingAndPublishesMessage()
    {
        var db = LamuFlixContextFactory.CreateContext();
        var queuePublisher = Substitute.For<IEnrichmentQueuePublisher>();
        _ = new MovieService(db, null, queuePublisher);

        var movie = new Movie
        {
            Title = "Imported Movie",
            Year = 2023,
            Location = "F:\\Test.mkv",
            Format = ".mkv",
            Status = MovieEnrichmentStatus.Pending
        };
        db.Movies.Add(movie);
        db.SaveChanges();

        movie.Status.ShouldBe(MovieEnrichmentStatus.Pending);
    }

    private static IConnection OpenRabbitConnection()
    {
        return new ConnectionFactory
        {
            Uri = new Uri(ContainerFixture.RabbitMq.GetConnectionString())
        }.CreateConnection();
    }

    private static void DeclareProcessorQueues(IModel channel)
    {
        channel.QueueDeclare("task_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueueDeclare("task_queue_dlq", durable: true, exclusive: false, autoDelete: false, arguments: null);
        channel.QueuePurge("task_queue");
        channel.QueuePurge("task_queue_dlq");
    }

    private static string DeclareSourceQueue(IModel channel)
    {
        var sourceQueue = $"source_{Guid.NewGuid():N}";
        channel.QueueDeclare(sourceQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        return sourceQueue;
    }

    private static ulong PublishAndTake(IModel channel, string sourceQueue, MovieEnrichmentMessage message)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        channel.BasicPublish(string.Empty, sourceQueue, basicProperties: null, body: body);
        var delivery = channel.BasicGet(sourceQueue, autoAck: false);
        if (delivery is null)
        {
            throw new InvalidOperationException("Message not found");
        }

        return delivery.DeliveryTag;
    }

    private static void AssertSourceQueueAcked(string sourceQueue)
    {
        using var connection = OpenRabbitConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclarePassive(sourceQueue).MessageCount.ShouldBe(0u);
    }

    private static void AssertRepublished(IModel channel, string queue, int expectedRetryCount)
    {
        var delivery = channel.BasicGet(queue, autoAck: true);
        delivery.ShouldNotBeNull();
        var republished = JsonSerializer.Deserialize<MovieEnrichmentMessage>(delivery.Body.Span);
        republished.ShouldNotBeNull();
        republished.RetryCount.ShouldBe(expectedRetryCount);
    }

    private static void DeleteSourceQueue(string sourceQueue)
    {
        using var connection = OpenRabbitConnection();
        using var channel = connection.CreateModel();
        channel.QueueDelete(sourceQueue);
    }

    private static async Task RunAndAssertAckedAsync(EnrichmentJobProcessor processor, MovieEnrichmentMessage message)
    {
        using var connection = OpenRabbitConnection();
        using var channel = connection.CreateModel();
        DeclareProcessorQueues(channel);
        var sourceQueue = DeclareSourceQueue(channel);
        try
        {
            var deliveryTag = PublishAndTake(channel, sourceQueue, message);
            await processor.ProcessMessageAsync(message, channel, deliveryTag);
            channel.Close();
            AssertSourceQueueAcked(sourceQueue);
        }
        finally
        {
            DeleteSourceQueue(sourceQueue);
        }
    }

    private static async Task RunAndAssertRepublishedAsync(
        EnrichmentJobProcessor processor,
        MovieEnrichmentMessage message,
        string targetQueue,
        int expectedRetryCount)
    {
        using var connection = OpenRabbitConnection();
        using var channel = connection.CreateModel();
        DeclareProcessorQueues(channel);
        var sourceQueue = DeclareSourceQueue(channel);
        try
        {
            var deliveryTag = PublishAndTake(channel, sourceQueue, message);
            channel.ConfirmSelect();
            await processor.ProcessMessageAsync(message, channel, deliveryTag);
            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            AssertRepublished(channel, targetQueue, expectedRetryCount);
        }
        finally
        {
            DeleteSourceQueue(sourceQueue);
        }
    }
}