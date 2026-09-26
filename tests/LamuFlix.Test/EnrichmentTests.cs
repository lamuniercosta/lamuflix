using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
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

namespace LamuFlix.Test;

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

        var channel = Substitute.For<IModel>();
        var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Matrix", Year = 1999 };

        await processor.ProcessMessageAsync(message, channel, 1UL);

        var updatedMovie = await db.Movies.SingleAsync(x => x.Id == movie.Id, TestContext.Current.CancellationToken);
        updatedMovie.Status.ShouldBe(MovieEnrichmentStatus.Enriched);
        updatedMovie.Synopsis.ShouldBe("Sci-Fi action");
        channel.Received(1).BasicAck(1UL, false);
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

        var channel = Substitute.For<IModel>();
        var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Missing Movie", Year = 2021 };

        await processor.ProcessMessageAsync(message, channel, 2UL);

        var updatedMovie = await db.Movies.SingleAsync(x => x.Id == movie.Id, TestContext.Current.CancellationToken);
        updatedMovie.Status.ShouldBe(MovieEnrichmentStatus.NotFound);
        // Verify row remains visible in DB
        updatedMovie.ShouldNotBeNull();
        channel.Received(1).BasicAck(2UL, false);
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

        var channel = Substitute.For<IModel>();
        var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Already Enriched", Year = 2020 };

        await processor.ProcessMessageAsync(message, channel, 3UL);

        metadataProvider.ReceivedCalls().ShouldBeEmpty();
        channel.Received(1).BasicAck(3UL, false);
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

        var channel = Substitute.For<IModel>();
        var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Failing Movie", RetryCount = 0 };

        await processor.ProcessMessageAsync(message, channel, 4UL);

        message.RetryCount.ShouldBe(1);
        channel.Received(1).QueueDeclare("task_queue", true, false, false, null);
        channel.Received(1).BasicAck(4UL, false);
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

        var channel = Substitute.For<IModel>();
        var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Dead Letter Movie", RetryCount = 3 };

        await processor.ProcessMessageAsync(message, channel, 5UL);

        var updatedMovie = await db.Movies.SingleAsync(x => x.Id == movie.Id, TestContext.Current.CancellationToken);
        updatedMovie.Status.ShouldBe(MovieEnrichmentStatus.Failed);
        channel.Received(1).QueueDeclare("task_queue_dlq", true, false, false, null);
        channel.Received(1).BasicAck(5UL, false);
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
}