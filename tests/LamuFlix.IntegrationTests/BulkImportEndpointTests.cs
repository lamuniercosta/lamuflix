using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class BulkImportEndpointTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const string CandidatesRoute = "/api/movies/import/candidates";
    private const string LibraryRoute = "/api/movies/import/library";
    private const string ProblemDetailsJson = "application/problem+json";

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly ILibraryFolderDiscovery discovery = Substitute.For<ILibraryFolderDiscovery>();
    private readonly IMovieFolderImporter importer = Substitute.For<IMovieFolderImporter>();
    private readonly IEnrichmentOutbox outbox = Substitute.For<IEnrichmentOutbox>();
    private readonly CaptureLog log = new();

    [Fact]
    public async Task ListCandidates_SeededMovies_ReturnsOnlyNewFolders()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LibraryPath> { new("Heat") });
        discovery.DiscoverAsync(Arg.Any<CancellationToken>())
            .Returns(Folders(new LibraryPath("Heat"), new LibraryPath("Alpha")));
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(CandidatesRoute, ct);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.GetProperty("newFolders").EnumerateArray()
            .Select(folder => folder.GetString())
            .ShouldBe(["Alpha"]);
    }

    [Fact]
    public async Task ImportLibrary_NewFolders_ReturnsImportedCounts()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LibraryPath>());
        importer.ImportAsync(Arg.Any<LibraryPath>(), Arg.Any<CancellationToken>())
            .Returns(new MovieId(7));
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsJsonAsync(
            LibraryRoute, new { folders = new[] { "Alpha", "Bravo" }, retryFailed = false }, ct);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.GetProperty("imported").GetInt32().ShouldBe(2);
        payload.GetProperty("skipped").GetInt32().ShouldBe(0);
        payload.GetProperty("retried").GetInt32().ShouldBe(0);
        payload.GetProperty("failures").EnumerateArray().ShouldBeEmpty();
        await importer.Received(2).ImportAsync(Arg.Any<LibraryPath>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportLibrary_Rerun_ReportsAllSkipped()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LibraryPath> { new("Alpha"), new("Bravo") });
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsJsonAsync(
            LibraryRoute, new { folders = new[] { "Alpha", "Bravo" }, retryFailed = false }, ct);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.GetProperty("imported").GetInt32().ShouldBe(0);
        payload.GetProperty("skipped").GetInt32().ShouldBe(2);
        await importer.DidNotReceive().ImportAsync(Arg.Any<LibraryPath>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportLibrary_BadFolder_ReturnsUnprocessableEntityProblem()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsJsonAsync(
            LibraryRoute, new { folders = new[] { "../secret" }, retryFailed = false }, ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("errors").EnumerateObject().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ImportLibrary_EmptyWithoutRetry_ReturnsUnprocessableEntityProblem()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsJsonAsync(
            LibraryRoute, new { folders = Array.Empty<string>(), retryFailed = false }, ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("errors").EnumerateObject().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ImportLibrary_RetryOnlyWithoutFolders_ReturnsOk()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LibraryPath>());
        movies.FindFailedMoviesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Movie>());
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsJsonAsync(
            LibraryRoute, new { retryFailed = true }, ct);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.GetProperty("retried").GetInt32().ShouldBe(0);
        payload.GetProperty("failures").EnumerateArray().ShouldBeEmpty();
    }

    [Fact]
    public async Task ListCandidates_MissingRoot_ReturnsServerErrorWithoutLeakingDetail()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMoviePathsAsync(Arg.Any<CancellationToken>())
            .Returns(new List<LibraryPath>());
        discovery.DiscoverAsync(Arg.Any<CancellationToken>())
            .Throws(new DirectoryNotFoundException("C:/library is missing"));
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(CandidatesRoute, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        payload.ShouldNotContain("C:/library is missing");
    }

    [Fact]
    public async Task ImportLibrary_ValidationFailure_ProducesNoErrorLogEntries()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsJsonAsync(
            LibraryRoute, new { folders = new[] { "../secret" }, retryFailed = false }, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        log.Entries.Where(entry => entry.Level == LogLevel.Error).ShouldBeEmpty();
    }

    private WebApplicationFactory<Program> CreateHost() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(movies);
            services.AddSingleton(discovery);
            services.AddSingleton(importer);
            services.AddSingleton(outbox);
            services.AddSingleton<ILoggerProvider>(new CaptureLoggerProvider(log));
        }));

    private static async IAsyncEnumerable<LibraryPath> Folders(params LibraryPath[] folders)
    {
        foreach (var folder in folders)
        {
            yield return folder;
        }

        await Task.CompletedTask;
    }

    private sealed class CaptureLog
    {
        private readonly ConcurrentQueue<LogRecord> entries = new();

        public IReadOnlyList<LogRecord> Entries => [.. entries];

        public void Add(LogRecord record) => entries.Enqueue(record);
    }

    private sealed record LogRecord(LogLevel Level, string Category, string Message);

    private sealed class CaptureLoggerProvider(CaptureLog log) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(log, categoryName);

        public void Dispose()
        {
        }
    }

    private sealed class CaptureLogger(CaptureLog log, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!category.EndsWith("ExceptionHandlerMiddleware", StringComparison.Ordinal))
            {
                return;
            }

            log.Add(new LogRecord(logLevel, category, formatter(state, exception)));
        }
    }
}
