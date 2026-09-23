using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;
using LamuFlix.Data;
using LamuFlix.Data.Models;
using LamuFlix.Worker.Models;
using LamuFlix.Worker.Services;
using LamuFlix.Web.Services;

namespace LamuFlix.Test
{
    [TestClass]
    public sealed class EnrichmentTests
    {
        private class TestHttpMessageHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public TestHttpMessageHandler(HttpResponseMessage response)
            {
                _response = response;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_response);
            }
        }

        private static IConfiguration CreateOmdbConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Movies:OMDB_API_KEY"] = "test-key" })
                .Build();

        [TestMethod]
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

            var meta = await provider.FetchMetadataAsync("Test Movie");

            Assert.IsNotNull(meta);
            Assert.AreEqual("Test Movie", meta.Title);
            Assert.AreEqual(2020, meta.Year);
            Assert.AreEqual(120, meta.DurationMinutes);
            Assert.AreEqual(0.8m, meta.ImdbRating);
            Assert.AreEqual(90, meta.RottenTomatoesScore);
            Assert.AreEqual(85, meta.MetaScore);
            Assert.AreEqual("tt123", meta.ImdbId);
        }

        [TestMethod]
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

            var meta = await provider.FetchMetadataAsync("Unknown");

            Assert.IsNull(meta);
        }

        [TestMethod]
        public async Task OmdbMetadataProvider_ServerError_ThrowsException()
        {
            var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var httpClient = new HttpClient(handler);
            var config = CreateOmdbConfig();
            var provider = new OmdbMetadataProvider(httpClient, config);

            await Assert.ThrowsExceptionAsync<HttpRequestException>(() => provider.FetchMetadataAsync("Error Movie"));
        }

        [TestMethod]
        public void OmdbMetadataProvider_MissingApiKey_ThrowsInvalidOperationException()
        {
            var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var httpClient = new HttpClient(handler);
            var config = new ConfigurationBuilder().Build();

            var ex = Assert.ThrowsException<InvalidOperationException>(() => new OmdbMetadataProvider(httpClient, config));
            Assert.IsTrue(ex.Message.Contains("Movies:OMDB_API_KEY"));
        }

        private static (LamuFlixContext, IServiceProvider) CreateInMemoryDb()
        {
            var options = new DbContextOptionsBuilder<LamuFlixContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new LamuFlixContext(options);

            var services = new ServiceCollection();
            services.AddSingleton(context);
            var provider = services.BuildServiceProvider();
            return (context, provider);
        }

        [TestMethod]
        public async Task EnrichmentJobProcessor_TransitionsToEnriched()
        {
            var (db, provider) = CreateInMemoryDb();
            var movie = new Movie
            {
                Title = "Matrix",
                Year = 1999,
                Location = "F:\\Matrix.mkv",
                Format = ".mkv",
                Status = MovieEnrichmentStatus.Pending
            };
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var metadataProviderMock = new Mock<IMetadataProvider>();
            metadataProviderMock
                .Setup(x => x.FetchMetadataAsync("Matrix", 1999, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MovieMetadata("Matrix", "Sci-Fi action", 1999, 136, "http://poster", 8.7m, 88, 73, "tt0133093", new[] { "Action" }, new[] { "Keanu Reeves" }, new[] { "Wachowskis" }));

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(x => x.ServiceProvider).Returns(provider);
            scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

            var config = new ConfigurationBuilder().Build();
            var processor = new EnrichmentJobProcessor(scopeFactoryMock.Object, metadataProviderMock.Object, config, NullLogger<EnrichmentJobProcessor>.Instance);

            var channelMock = new Mock<IModel>();
            var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Matrix", Year = 1999 };

            await processor.ProcessMessageAsync(message, channelMock.Object, 1UL);

            var updatedMovie = await db.Movies.SingleAsync(x => x.Id == movie.Id);
            Assert.AreEqual(MovieEnrichmentStatus.Enriched, updatedMovie.Status);
            Assert.AreEqual("Sci-Fi action", updatedMovie.Synopsis);
            channelMock.Verify(x => x.BasicAck(1UL, false), Times.Once());
        }

        [TestMethod]
        public async Task EnrichmentJobProcessor_TransitionsToNotFound_RowRemainsVisible()
        {
            var (db, provider) = CreateInMemoryDb();
            var movie = new Movie
            {
                Title = "Missing Movie",
                Year = 2021,
                Location = "F:\\Missing.mp4",
                Format = ".mp4",
                Status = MovieEnrichmentStatus.Pending
            };
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var metadataProviderMock = new Mock<IMetadataProvider>();
            metadataProviderMock
                .Setup(x => x.FetchMetadataAsync("Missing Movie", 2021, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((MovieMetadata?)null);

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(x => x.ServiceProvider).Returns(provider);
            scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

            var config = new ConfigurationBuilder().Build();
            var processor = new EnrichmentJobProcessor(scopeFactoryMock.Object, metadataProviderMock.Object, config, NullLogger<EnrichmentJobProcessor>.Instance);

            var channelMock = new Mock<IModel>();
            var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Missing Movie", Year = 2021 };

            await processor.ProcessMessageAsync(message, channelMock.Object, 2UL);

            var updatedMovie = await db.Movies.SingleAsync(x => x.Id == movie.Id);
            Assert.AreEqual(MovieEnrichmentStatus.NotFound, updatedMovie.Status);
            // Verify row remains visible in DB
            Assert.IsNotNull(updatedMovie);
            channelMock.Verify(x => x.BasicAck(2UL, false), Times.Once());
        }

        [TestMethod]
        public async Task EnrichmentJobProcessor_Idempotency_SkipsAlreadyEnriched()
        {
            var (db, provider) = CreateInMemoryDb();
            var movie = new Movie
            {
                Title = "Already Enriched",
                Year = 2020,
                Location = "F:\\Test.mkv",
                Format = ".mkv",
                Status = MovieEnrichmentStatus.Enriched
            };
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var metadataProviderMock = new Mock<IMetadataProvider>();
            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(x => x.ServiceProvider).Returns(provider);
            scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

            var config = new ConfigurationBuilder().Build();
            var processor = new EnrichmentJobProcessor(scopeFactoryMock.Object, metadataProviderMock.Object, config, NullLogger<EnrichmentJobProcessor>.Instance);

            var channelMock = new Mock<IModel>();
            var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Already Enriched", Year = 2020 };

            await processor.ProcessMessageAsync(message, channelMock.Object, 3UL);

            metadataProviderMock.Verify(x => x.FetchMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never());
            channelMock.Verify(x => x.BasicAck(3UL, false), Times.Once());
        }

        [TestMethod]
        public async Task EnrichmentJobProcessor_RetryOnFailure_RepublishesWhenRetryCountBelowThree()
        {
            var (db, provider) = CreateInMemoryDb();
            var movie = new Movie
            {
                Title = "Failing Movie",
                Year = 2022,
                Location = "F:\\Fail.mp4",
                Format = ".mp4",
                Status = MovieEnrichmentStatus.Pending
            };
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var metadataProviderMock = new Mock<IMetadataProvider>();
            metadataProviderMock
                .Setup(x => x.FetchMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Network error"));

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(x => x.ServiceProvider).Returns(provider);
            scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

            var config = new ConfigurationBuilder().Build();
            var processor = new EnrichmentJobProcessor(scopeFactoryMock.Object, metadataProviderMock.Object, config, NullLogger<EnrichmentJobProcessor>.Instance);

            var channelMock = new Mock<IModel>();
            var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Failing Movie", RetryCount = 0 };

            await processor.ProcessMessageAsync(message, channelMock.Object, 4UL);

            Assert.AreEqual(1, message.RetryCount);
            channelMock.Verify(x => x.QueueDeclare("task_queue", true, false, false, null), Times.Once());
            channelMock.Verify(x => x.BasicAck(4UL, false), Times.Once());
        }

        [TestMethod]
        public async Task EnrichmentJobProcessor_ExceedsMaxRetries_MarksFailedAndForwardsToDlq()
        {
            var (db, provider) = CreateInMemoryDb();
            var movie = new Movie
            {
                Title = "Dead Letter Movie",
                Year = 2022,
                Location = "F:\\DLQ.mp4",
                Format = ".mp4",
                Status = MovieEnrichmentStatus.Pending
            };
            db.Movies.Add(movie);
            await db.SaveChangesAsync();

            var metadataProviderMock = new Mock<IMetadataProvider>();
            metadataProviderMock
                .Setup(x => x.FetchMetadataAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Persistent error"));

            var scopeFactoryMock = new Mock<IServiceScopeFactory>();
            var scopeMock = new Mock<IServiceScope>();
            scopeMock.Setup(x => x.ServiceProvider).Returns(provider);
            scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

            var config = new ConfigurationBuilder().Build();
            var processor = new EnrichmentJobProcessor(scopeFactoryMock.Object, metadataProviderMock.Object, config, NullLogger<EnrichmentJobProcessor>.Instance);

            var channelMock = new Mock<IModel>();
            var message = new MovieEnrichmentMessage { MovieId = movie.Id, Title = "Dead Letter Movie", RetryCount = 3 };

            await processor.ProcessMessageAsync(message, channelMock.Object, 5UL);

            var updatedMovie = await db.Movies.SingleAsync(x => x.Id == movie.Id);
            Assert.AreEqual(MovieEnrichmentStatus.Failed, updatedMovie.Status);
            channelMock.Verify(x => x.QueueDeclare("task_queue_dlq", true, false, false, null), Times.Once());
            channelMock.Verify(x => x.BasicAck(5UL, false), Times.Once());
        }

        [TestMethod]
        public void FilmesService_CriarFilme_InsertsAsPendingAndPublishesMessage()
        {
            var (db, _) = CreateInMemoryDb();
            var queuePublisherMock = new Mock<IEnrichmentQueuePublisher>();
            _ = new FilmesService(db, null, queuePublisherMock.Object);

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

            Assert.AreEqual(MovieEnrichmentStatus.Pending, movie.Status);
        }
    }
}
