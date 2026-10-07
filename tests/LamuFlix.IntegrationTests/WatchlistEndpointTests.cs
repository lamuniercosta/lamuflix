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
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class WatchlistEndpointTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const int KnownId = 7;
    private const int UnknownId = 999;
    private const string ProblemDetailsJson = "application/problem+json";

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly Movie movie = Movie.Create(
        new MovieId(KnownId),
        "Heat",
        new LibraryPath("C:/library/Heat"),
        new MediaFormat(".mkv"));

    [Fact]
    public async Task AddToWatchlist_MovieNotInWatchlist_ReturnsEmptyNoContent()
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(WatchlistRoute(KnownId), null, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(string.Empty);
        movie.IsInWatchlist.ShouldBeTrue();
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddToWatchlist_MovieAlreadyInWatchlist_ReturnsConflictProblem()
    {
        // arrange
        movie.AddToWatchlist();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(WatchlistRoute(KnownId), null, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("title").GetString().ShouldBe("Conflict");
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveFromWatchlist_MovieInWatchlist_ReturnsEmptyNoContent()
    {
        // arrange
        movie.AddToWatchlist();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.DeleteAsync(WatchlistRoute(KnownId), cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(string.Empty);
        movie.IsInWatchlist.ShouldBeFalse();
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveFromWatchlist_MovieNotInWatchlist_ReturnsConflictProblem()
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.DeleteAsync(WatchlistRoute(KnownId), cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("title").GetString().ShouldBe("Conflict");
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task WatchlistRoute_UnknownId_ReturnsNotFoundProblem(string method)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            WatchlistRoute(UnknownId));

        // act
        var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain(nameof(NotFoundException));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task WatchlistRoute_NonpositiveId_ReturnsUnprocessableEntityProblem(string method)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            WatchlistRoute(0));

        // act
        var response = await client.SendAsync(request, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name).ShouldContain("id");
    }

    private static string WatchlistRoute(int id) => $"/api/movies/{id}/watchlist";

    private WebApplicationFactory<Program> CreateHost()
    {
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(movie);
        return factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton(movies)));
    }
}
