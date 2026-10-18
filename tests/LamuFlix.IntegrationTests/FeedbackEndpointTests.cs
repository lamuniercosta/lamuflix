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

public sealed class FeedbackEndpointTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
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

    public static TheoryData<string, string> Operations => new()
    {
        { "POST", "watched" },
        { "DELETE", "watched" },
        { "POST", "not-interested" },
        { "DELETE", "not-interested" },
    };

    public static TheoryData<string, string, bool, bool, bool, bool> LegalTransitions => new()
    {
        { "POST", "watched", false, false, true, false },
        { "DELETE", "watched", true, false, false, false },
        { "POST", "not-interested", false, false, false, true },
        { "DELETE", "not-interested", false, true, false, false },
    };

    public static TheoryData<string, string, bool, bool> RepeatedTransitions => new()
    {
        { "POST", "watched", true, false },
        { "DELETE", "watched", false, false },
        { "POST", "not-interested", false, true },
        { "DELETE", "not-interested", false, false },
    };

    public static TheoryData<string, string, string> MalformedOrOverflowingIds => new()
    {
        { "POST", "watched", "abc" },
        { "POST", "watched", "99999999999999999999" },
        { "DELETE", "watched", "abc" },
        { "DELETE", "watched", "99999999999999999999" },
        { "POST", "not-interested", "abc" },
        { "POST", "not-interested", "99999999999999999999" },
        { "DELETE", "not-interested", "abc" },
        { "DELETE", "not-interested", "99999999999999999999" },
    };

    [Theory]
    [MemberData(nameof(LegalTransitions))]
    public async Task Operation_OnLegalTransition_ReturnsEmptyNoContent(
        string method,
        string segment,
        bool watchedBefore,
        bool notInterestedBefore,
        bool expectedWatched,
        bool expectedNotInterested)
    {
        // arrange
        Seed(watchedBefore, notInterestedBefore);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(new HttpMethod(method), Route(segment));

        // act
        var response = await client.SendAsync(request, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(string.Empty);
        movie.IsWatched.ShouldBe(expectedWatched);
        movie.IsNotInterested.ShouldBe(expectedNotInterested);
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(RepeatedTransitions))]
    public async Task Operation_OnRepeatedTransition_ReturnsConflictProblemWithoutSaving(
        string method,
        string segment,
        bool watchedBefore,
        bool notInterestedBefore)
    {
        // arrange
        Seed(watchedBefore, notInterestedBefore);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(new HttpMethod(method), Route(segment));

        // act
        var response = await client.SendAsync(request, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("title").GetString().ShouldBe("Conflict");
        problem.TryGetProperty("action", out _).ShouldBeFalse();
        problem.TryGetProperty("state", out _).ShouldBeFalse();
        problem.TryGetProperty("Action", out _).ShouldBeFalse();
        problem.TryGetProperty("State", out _).ShouldBeFalse();
        movie.IsWatched.ShouldBe(watchedBefore);
        movie.IsNotInterested.ShouldBe(notInterestedBefore);
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task Operation_UnknownPositiveId_ReturnsNotFoundProblemWithoutSaving(
        string method,
        string segment)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(new HttpMethod(method), Route(UnknownId, segment));

        // act
        var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain(nameof(NotFoundException));
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(Operations))]
    public async Task Operation_NonpositiveId_ReturnsUnprocessableEntityProblemWithoutSaving(
        string method,
        string segment)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(new HttpMethod(method), Route(0, segment));

        // act
        var response = await client.SendAsync(request, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name).ShouldContain("id");
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(MalformedOrOverflowingIds))]
    public async Task Operation_MalformedOrOverflowingId_ReturnsBadRequestProblemWithoutSaving(
        string method,
        string segment,
        string id)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(
            new HttpMethod(method),
            $"/api/movies/{id}/{segment}");

        // act
        var response = await client.SendAsync(request, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(400);
        problem.GetProperty("title").GetString().ShouldBe("Bad Request");
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private void Seed(bool watched, bool notInterested)
    {
        if (watched)
        {
            movie.MarkWatched();
        }

        if (notInterested)
        {
            movie.MarkNotInterested();
        }
    }

    private static string Route(string segment) => Route(KnownId, segment);

    private static string Route(int id, string segment) => $"/api/movies/{id}/{segment}";

    private WebApplicationFactory<Program> CreateHost()
    {
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(movie);
        return factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton(movies)));
    }
}
