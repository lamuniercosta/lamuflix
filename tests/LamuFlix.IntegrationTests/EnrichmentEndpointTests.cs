using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class EnrichmentEndpointTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const int KnownId = 7;
    private const int UnknownId = 999;
    private const string ProblemDetailsJson = "application/problem+json";

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IEnrichmentQueue queue = Substitute.For<IEnrichmentQueue>();
    private readonly Movie movie = Movie.Rehydrate(
        new MovieId(KnownId),
        "Heat",
        new LibraryPath("C:/library/Heat"),
        new MediaFormat(".mkv"),
        false,
        null,
        EnrichmentStatus.NotFound,
        null,
        0,
        null,
        null);

    [Fact]
    public async Task RequestEnrichment_MissingMetadataMovie_ReturnsAcceptedWithLocationAndEmptyBody()
    {
        // arrange
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(movie);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync($"/api/movies/{KnownId}/enrichment", null, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldNotBeNull().ToString().ShouldBe($"/api/movies/{KnownId}");
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(string.Empty);
        movie.Status.ShouldBe(EnrichmentStatus.Pending);
        await queue.Received(1)
            .EnqueueAsync(new EnrichmentRequested(new MovieId(KnownId), 1), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RequestEnrichment_NonpositiveId_ReturnsUnprocessableEntityProblem(int id)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync($"/api/movies/{id}/enrichment", null, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name).ShouldContain("id");
        await movies.DidNotReceive().GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestEnrichment_PendingMovie_ReturnsConflictProblemAndDoesNotEnqueue()
    {
        // arrange
        var pending = Movie.Rehydrate(
            new MovieId(KnownId),
            "Heat",
            new LibraryPath("C:/library/Heat"),
            new MediaFormat(".mkv"),
            false,
            null,
            EnrichmentStatus.Pending,
            null,
            0,
            null,
            null);
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(pending);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync($"/api/movies/{KnownId}/enrichment", null, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(StatusCodes.Status409Conflict);
        problem.GetProperty("title").GetString().ShouldBe("Conflict");
        problem.GetRawText().ShouldNotContain(nameof(InvalidTransitionException));
        pending.Status.ShouldBe(EnrichmentStatus.Pending);
        await queue.DidNotReceive()
            .EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
        ConflictMetadata(host).ShouldContain(item =>
            item.StatusCode == StatusCodes.Status409Conflict && item.Type == typeof(ProblemDetails));
    }

    [Fact]
    public async Task RequestEnrichment_UnknownId_ReturnsNotFoundProblem()
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync($"/api/movies/{UnknownId}/enrichment", null, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain(nameof(NotFoundException));
        await queue.DidNotReceive()
            .EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestEnrichment_NonIntegerId_ReturnsFrameworkBadRequestProblem()
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync("/api/movies/abc/enrichment", null, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(400);
        problem.GetProperty("title").GetString().ShouldBe("Bad Request");
        problem.TryGetProperty("detail", out _).ShouldBeFalse();
        problem.TryGetProperty("errors", out _).ShouldBeFalse();
        problem.GetRawText().ShouldNotContain("BadHttpRequestException");
    }

    private WebApplicationFactory<Program> CreateHost() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(movies);
            services.AddSingleton(queue);
        }));

    private static IEnumerable<IProducesResponseTypeMetadata> ConflictMetadata(WebApplicationFactory<Program> host) =>
        host.Services.GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == "RequestEnrichment")
            .Metadata
            .OfType<IProducesResponseTypeMetadata>();
}
