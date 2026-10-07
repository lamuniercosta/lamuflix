using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class ImportEndpointTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const string ImportRoute = "/api/movies/import";
    private const string ProblemDetailsJson = "application/problem+json";

    private readonly IMediaLibraryScanner scanner = Substitute.For<IMediaLibraryScanner>();
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IEnrichmentQueue queue = Substitute.For<IEnrichmentQueue>();

    [Fact]
    public async Task ImportMovie_ValidFolder_ReturnsAcceptedWithLocationAndEmptyBody()
    {
        // arrange
        scanner.Scan(Arg.Any<LibraryPath>())
            .Returns(callInfo => new ScannedMovie(
                callInfo.Arg<LibraryPath>(),
                "Heat",
                new MediaFormat(".mkv"),
                null,
                1024));
        movies.NextIdentityAsync(Arg.Any<CancellationToken>()).Returns(new MovieId(42));
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsJsonAsync(
            ImportRoute,
            new { folderPath = "C:/library/Heat" },
            cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldNotBeNull().ToString().ShouldBe("/api/movies/42");
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(string.Empty);
        scanner.Received(1).Scan(new LibraryPath("C:/library/Heat"));
        await queue.Received(1)
            .EnqueueAsync(new EnrichmentRequested(new MovieId(42), 1), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:/library/../secret")]
    public async Task ImportMovie_InvalidFolder_ReturnsUnprocessableEntityProblem(string folderPath)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsJsonAsync(
            ImportRoute,
            new { folderPath },
            cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        Fields(problem).ShouldContain("folderPath");
        scanner.DidNotReceive().Scan(Arg.Any<LibraryPath>());
        await queue.DidNotReceive()
            .EnqueueAsync(Arg.Any<EnrichmentRequested>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportMovie_MalformedBody_ReturnsFrameworkBadRequestProblem()
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var content = new StringContent("{ \"folderPath\": ", Encoding.UTF8, "application/json");

        // act
        var response = await client.PostAsync(ImportRoute, content, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(400);
        problem.GetProperty("title").GetString().ShouldBe("Bad Request");
        problem.TryGetProperty("detail", out _).ShouldBeFalse();
        problem.TryGetProperty("errors", out _).ShouldBeFalse();
        problem.GetRawText().ShouldNotContain("System.Text.Json");
        problem.GetRawText().ShouldNotContain(nameof(JsonException));
    }

    private WebApplicationFactory<Program> CreateHost() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.AddSingleton(scanner);
            services.AddSingleton(movies);
            services.AddSingleton(queue);
        }));

    private static IEnumerable<string> Fields(JsonElement problem) =>
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name);
}
