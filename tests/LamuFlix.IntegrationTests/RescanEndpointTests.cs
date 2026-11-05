using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class RescanEndpointTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const int KnownId = 7;
    private const int UnknownId = 999;
    private const int NonpositiveId = 0;
    private const string ProblemDetailsJson = "application/problem+json";
    private const string Folder = "C:/library/Heat";
    private const string BulkRoute = "/api/movies/rescan";

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();

    [Fact]
    public async Task RescanMovie_ReplacedExtension_ReturnsChangedFormatsWithoutDots()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movie = MovieAt(KnownId, "avi");
        movies.GetAsync(movie.Id, Arg.Any<CancellationToken>()).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path)
            .Returns(Primary("mkv"));
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(Route(KnownId), null, ct);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.GetProperty("movieId").GetInt32().ShouldBe(KnownId);
        payload.GetProperty("changed").GetBoolean().ShouldBeTrue();
        payload.GetProperty("oldFormat").GetString().ShouldBe("avi");
        payload.GetProperty("newFormat").GetString().ShouldBe("mkv");
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanMovie_SameExtensionReplacement_ReturnsUnchangedWithoutSaving()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movie = MovieAt(KnownId, "mkv");
        movies.GetAsync(movie.Id, Arg.Any<CancellationToken>()).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path).Returns(Primary("mkv"));
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(Route(KnownId), null, ct);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.GetProperty("changed").GetBoolean().ShouldBeFalse();
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanMovie_UnknownId_ReturnsNotFoundProblemWithoutSaving()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(Route(UnknownId), null, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain(nameof(NotFoundException));
        scanner.DidNotReceive().ResolvePrimaryVideo(Arg.Any<LibraryPath>());
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanMovie_MissingFolder_ReturnsNotFoundProblemWithTraceIdAndNoFilesystemPath()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movie = MovieAt(KnownId, "avi");
        movies.GetAsync(movie.Id, Arg.Any<CancellationToken>()).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path).Throws(new NotFoundException());
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(Route(KnownId), null, ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("title").GetString().ShouldBe("Not Found");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        problem.GetRawText().ShouldNotContain(Folder);
        problem.GetRawText().ShouldNotContain(nameof(NotFoundException));
        movie.Format.ShouldBe(new MediaFormat("avi"));
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanMovie_NoEligibleVideo_ReturnsConflictProblemWithoutMutation()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movie = MovieAt(KnownId, "avi");
        movies.GetAsync(movie.Id, Arg.Any<CancellationToken>()).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path)
            .Throws(new InvalidTransitionException($"Library folder '{Folder}' contains no eligible top-level video file."));
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(Route(KnownId), null, ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(409);
        problem.GetProperty("title").GetString().ShouldBe("Conflict");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        problem.GetRawText().ShouldNotContain(Folder);
        movie.Format.ShouldBe(new MediaFormat("avi"));
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanMovie_NonpositiveId_ReturnsUnprocessableEntityProblem()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(Route(NonpositiveId), null, ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("errors").GetProperty("id").EnumerateArray().ShouldNotBeEmpty();
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("99999999999999999999")]
    public async Task RescanMovie_MalformedOrOverflowingId_ReturnsBadRequestProblem(string id)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync($"/api/movies/{id}/rescan", null, ct);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(400);
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanMovie_UnexpectedScannerFailure_ReturnsServerErrorWithoutLeakingDetail()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movie = MovieAt(KnownId, "avi");
        movies.GetAsync(movie.Id, Arg.Any<CancellationToken>()).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path).Throws(new UnauthorizedAccessException("candidate is unreadable"));
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(Route(KnownId), null, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        payload.ShouldNotContain("candidate is unreadable");
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanMovie_AbortedRequest_CancelsTheTokenFlowingIntoTheRepository()
    {
        // arrange
        using var clientCancellation = new CancellationTokenSource();
        var observed = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var ct = call.Arg<CancellationToken>();
                observed.TrySetResult(ct);
                entered.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return (Movie?)null;
            });
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var request = client.PostAsync(Route(KnownId), null, clientCancellation.Token);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        await clientCancellation.CancelAsync();
        await Should.ThrowAsync<TaskCanceledException>(() => request);

        // assert
        var repositoryToken = await observed.Task;
        repositoryToken.IsCancellationRequested.ShouldBeTrue();
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private WebApplicationFactory<Program> CreateHost() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(movies);
            services.AddSingleton(scanner);
        }));

    [Fact]
    public async Task RescanLibrary_MixedLibrary_ReturnsAscendingAggregateCounts()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var changed = MovieAt(KnownId, "avi");
        var unchanged = MovieAt(KnownId + 1, "mkv");
        var missingFolder = MovieAt(KnownId + 2, "avi");
        movies.SnapshotMovieIdsAsync(Arg.Any<CancellationToken>())
            .Returns([changed.Id, unchanged.Id, missingFolder.Id]);
        movies.GetAsync(changed.Id, Arg.Any<CancellationToken>()).Returns(changed);
        movies.GetAsync(unchanged.Id, Arg.Any<CancellationToken>()).Returns(unchanged);
        movies.GetAsync(missingFolder.Id, Arg.Any<CancellationToken>()).Returns(missingFolder);
        scanner.ResolvePrimaryVideo(changed.Path).Returns(Primary("mkv"));
        scanner.ResolvePrimaryVideo(unchanged.Path).Returns(Primary("mkv"));
        scanner.ResolvePrimaryVideo(missingFolder.Path).Throws(new NotFoundException());
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(BulkRoute, null, ct);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.GetProperty("changed").GetInt32().ShouldBe(1);
        payload.GetProperty("unchanged").GetInt32().ShouldBe(1);
        payload.GetProperty("failed").GetInt32().ShouldBe(1);
        payload.GetProperty("failedMovieIds").EnumerateArray()
            .Select(item => item.GetInt32())
            .ShouldBe([missingFolder.Id.Value]);
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanLibrary_EmptyLibrary_ReturnsZeroCountsAndEmptyIdArray()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        movies.SnapshotMovieIdsAsync(Arg.Any<CancellationToken>()).Returns([]);
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(BulkRoute, null, ct);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.GetProperty("changed").GetInt32().ShouldBe(0);
        payload.GetProperty("unchanged").GetInt32().ShouldBe(0);
        payload.GetProperty("failed").GetInt32().ShouldBe(0);
        payload.GetProperty("failedMovieIds").EnumerateArray().ShouldBeEmpty();
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanLibrary_UnexpectedScannerFailure_ReturnsServerErrorWithoutLeakingDetail()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        var movie = MovieAt(KnownId, "avi");
        movies.SnapshotMovieIdsAsync(Arg.Any<CancellationToken>()).Returns([movie.Id]);
        movies.GetAsync(movie.Id, Arg.Any<CancellationToken>()).Returns(movie);
        scanner.ResolvePrimaryVideo(movie.Path).Throws(new UnauthorizedAccessException("candidate is unreadable"));
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(BulkRoute, null, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        payload.ShouldNotContain("candidate is unreadable");
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescanLibrary_AbortedRequest_CancelsTheTokenFlowingIntoTheRepository()
    {
        // arrange
        using var clientCancellation = new CancellationTokenSource();
        var observed = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        movies.SnapshotMovieIdsAsync(Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var ct = call.Arg<CancellationToken>();
                observed.TrySetResult(ct);
                entered.TrySetResult();
                return Task.Delay(Timeout.InfiniteTimeSpan, ct).ContinueWith(
                    _ => (IReadOnlyList<MovieId>)[],
                    ct,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            });
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var request = client.PostAsync(BulkRoute, null, clientCancellation.Token);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        await clientCancellation.CancelAsync();
        await Should.ThrowAsync<TaskCanceledException>(() => request);

        // assert
        var repositoryToken = await observed.Task;
        repositoryToken.IsCancellationRequested.ShouldBeTrue();
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Movie MovieAt(int id, string extension) =>
        Movie.Create(new MovieId(id), "Heat", new LibraryPath($"{Folder}{id}"), new MediaFormat(extension));

    private static PrimaryVideo Primary(string extension) =>
        new(new LibraryPath($"{Folder}/Heat.{extension}"), new MediaFormat(extension));

    private static string Route(int id) => $"/api/movies/{id}/rescan";
}
