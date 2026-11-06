using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
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

    public static TheoryData<string, int, bool, bool> ReviewAssignments => new()
    {
        { "none", 0, false, false },
        { "liked", 1, false, false },
        { "disliked", 2, true, false },
    };

    public static TheoryData<string, string> InvalidReviewPayloads => new()
    {
        { "{\"review\":null}", "null review" },
        { "{\"review\":\"\"}", "empty review" },
        { "{\"review\":\"   \"}", "whitespace review" },
        { "{\"review\":\"Liked\"}", "wrong case review" },
        { "{\"review\":\"LIKED\"}", "upper case review" },
        { "{\"review\":\"love\"}", "unknown review" },
        { "{\"review\":\"2\"}", "numeric string review" },
        { "{}", "missing review" },
    };

    [Theory]
    [MemberData(nameof(ReviewAssignments))]
    public async Task PutReview_ValidName_AssignsReviewAndReturnsEmptyNoContent(
        string review,
        int expectedValue,
        bool expectedNotInterested,
        bool expectedWatched)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var content = JsonContent.Create(new { review });

        // act
        using var response = await client.PutAsync(Route("review"), content, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync(cancellationToken)).ShouldBe(string.Empty);
        movie.Review.Value.ShouldBe(expectedValue);
        movie.IsNotInterested.ShouldBe(expectedNotInterested);
        movie.IsWatched.ShouldBe(expectedWatched);
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("liked", "disliked", 2)]
    [InlineData("disliked", "liked", 1)]
    [InlineData("liked", "none", 0)]
    public async Task PutReview_RepeatedOrOppositeAssignment_IsIdempotentWithoutConflict(
        string first,
        string second,
        int expectedValue)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        using var firstResponse = await client.PutAsync(
            Route("review"),
            JsonContent.Create(new { review = first }),
            cancellationToken);
        using var secondResponse = await client.PutAsync(
            Route("review"),
            JsonContent.Create(new { review = second }),
            cancellationToken);

        // assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        movie.Review.Value.ShouldBe(expectedValue);
        await movies.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(InvalidReviewPayloads))]
    public async Task PutReview_InvalidReviewName_ReturnsUnprocessableEntityWithReviewFieldWithoutSaving(
        string payload,
        string description)
    {
        // arrange
        description.ShouldNotBeNullOrWhiteSpace();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // act
        using var response = await client.PutAsync(Route("review"), content, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(422);
        problem.GetProperty("title").GetString().ShouldBe("Unprocessable Entity");
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name)
            .ShouldBe(["review"], ignoreOrder: true);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        problem.GetRawText().ShouldNotContain(nameof(ValidationException));
        movie.Review.ShouldBe(Review.None);
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("{\"review\":", "truncated json")]
    [InlineData("not json", "unbindable body")]
    [InlineData("[]", "array body")]
    [InlineData("{\"review\":{\"name\":\"liked\"}}", "object review")]
    [InlineData("{\"review\":2}", "numeric review")]
    public async Task PutReview_MalformedOrUnbindableBody_ReturnsBadRequestProblemWithoutSaving(
        string payload,
        string description)
    {
        // arrange
        description.ShouldNotBeNullOrWhiteSpace();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // act
        using var response = await client.PutAsync(Route("review"), content, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe(400);
        problem.GetProperty("title").GetString().ShouldBe("Bad Request");
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutReview_EmptyBody_ReturnsBadRequestProblemWithoutSaving()
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        // act
        using var response = await client.PutAsync(Route("review"), content, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.GetProperty("status").GetInt32().ShouldBe(400);
        movie.Review.ShouldBe(Review.None);
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PutReview_UnknownMovie_ReturnsNotFoundProblemWithoutSaving()
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        using var response = await client.PutAsync(
            Route(UnknownId, "review"),
            JsonContent.Create(new { review = "liked" }),
            cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain(nameof(NotFoundException));
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public async Task PutReview_NonpositiveId_ReturnsUnprocessableEntityWithIdFieldWithoutSaving(int id)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        using var response = await client.PutAsync(
            Route(id, "review"),
            JsonContent.Create(new { review = "liked" }),
            cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name).ShouldContain("id");
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("99999999999999999999")]
    [InlineData("1.5")]
    public async Task PutReview_MalformedOrOverflowingId_ReturnsBadRequestProblemWithoutSaving(string id)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        using var response = await client.PutAsync(
            $"/api/movies/{id}/review",
            JsonContent.Create(new { review = "liked" }),
            cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.GetProperty("status").GetInt32().ShouldBe(400);
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LegacyDeleteNotInterested_OnLikedMovie_KeepsConflictAndPreservesLiked()
    {
        // arrange
        movie.AssignReview(Review.Liked);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Delete, Route("not-interested"));

        // act
        using var response = await client.SendAsync(request, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        movie.Review.ShouldBe(Review.Liked);
        await movies.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LegacyPostNotInterested_OnLikedMovie_ProjectsDislikedWithoutConflict()
    {
        // arrange
        movie.AssignReview(Review.Liked);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        using var request = new HttpRequestMessage(HttpMethod.Post, Route("not-interested"));

        // act
        using var response = await client.SendAsync(request, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        movie.Review.ShouldBe(Review.Disliked);
        movie.IsNotInterested.ShouldBeTrue();
        await movies.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
