using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class RecommendationEndpointTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const int KnownId = 7;
    private const int UnknownId = 999;
    private const int DefaultCandidatePoolSize = 50;
    private const string ProblemDetailsJson = "application/problem+json";

    private static readonly string[] DtoFieldNames =
    [
        "id",
        "title",
        "posterUrl",
        "releaseYear",
        "runtime",
        "imdbRating",
        "enrichmentStatus",
        "isInWatchlist",
        "isWatched",
        "isNotInterested",
        "score",
        "reasons",
    ];

    private readonly Fixture fixture = new();
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>> handler =
        Substitute.For<IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>>();

    private GetRecommendationsQuery? DispatchedQuery { get; set; }

    [Fact]
    public async Task GetRecommendations_OmittedTake_DefaultsToTenIndependentlyOfConfiguredTake()
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns();
        await using var host = CreateHost(configuredTake: 7, candidatePoolSize: DefaultCandidatePoolSize);
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DispatchedQuery.ShouldNotBeNull().Take.ShouldBe(10);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task GetRecommendations_TakeInsideInclusiveConfiguredBound_DispatchesThatTake(int take)
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns();
        await using var host = CreateHost(configuredTake: 1, candidatePoolSize: 5);
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{Route(KnownId)}?take={take.ToString(CultureInfo.InvariantCulture)}", cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DispatchedQuery.ShouldNotBeNull().Take.ShouldBe(take);
    }

    [Theory]
    [InlineData(4, 4, HttpStatusCode.OK)]
    [InlineData(4, 5, HttpStatusCode.BadRequest)]
    [InlineData(8, 8, HttpStatusCode.OK)]
    [InlineData(8, 9, HttpStatusCode.BadRequest)]
    public async Task GetRecommendations_ConfiguredBoundChanges_ValidateAgainstTheActivePoolSize(
        int candidatePoolSize,
        int take,
        HttpStatusCode expected)
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns();
        await using var host = CreateHost(configuredTake: 1, candidatePoolSize: candidatePoolSize);
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{Route(KnownId)}?take={take.ToString(CultureInfo.InvariantCulture)}", cancellationToken);

        // assert
        response.StatusCode.ShouldBe(expected);
        if (expected == HttpStatusCode.OK)
        {
            DispatchedQuery.ShouldNotBeNull().Take.ShouldBe(take);
            return;
        }

        await ReadProblemAsync(response, HttpStatusCode.BadRequest, cancellationToken);
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<GetRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRecommendations_OmittedTakeAboveSmallerValidPool_ReturnsBadRequestProblem()
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns();
        await using var host = CreateHost(configuredTake: 5, candidatePoolSize: 5);
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);

        // assert
        await ReadProblemAsync(response, HttpStatusCode.BadRequest, cancellationToken);
        await movies.DidNotReceiveWithAnyArgs().GetAsync(Arg.Any<MovieId>(), TestContext.Current.CancellationToken);
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<GetRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("take=")]
    [InlineData("take=%20")]
    [InlineData("take=abc")]
    [InlineData("take=1.5")]
    [InlineData("take=2147483648")]
    [InlineData("take=0")]
    [InlineData("take=-1")]
    [InlineData("take=51")]
    [InlineData("take=3&take=4")]
    public async Task GetRecommendations_InvalidTake_ReturnsBadRequestProblemWithoutDispatching(string query)
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{Route(KnownId)}?{query}", cancellationToken);

        // assert
        await ReadProblemAsync(response, HttpStatusCode.BadRequest, cancellationToken);
        await movies.DidNotReceiveWithAnyArgs().GetAsync(Arg.Any<MovieId>(), TestContext.Current.CancellationToken);
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<GetRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("take=%2B3", 3)]
    [InlineData("take=%203%20", 3)]
    [InlineData("take=003", 3)]
    public async Task GetRecommendations_InvariantIntegerForms_AreAccepted(string query, int expectedTake)
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{Route(KnownId)}?{query}", cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DispatchedQuery.ShouldNotBeNull().Take.ShouldBe(expectedTake);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(UnknownId)]
    public async Task GetRecommendations_InvalidTake_TakesPrecedenceOverIdAndLookup(int id)
    {
        // arrange
        HandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{Route(id)}?take=0", cancellationToken);

        // assert
        await ReadProblemAsync(response, HttpStatusCode.BadRequest, cancellationToken);
        await movies.DidNotReceiveWithAnyArgs().GetAsync(Arg.Any<MovieId>(), TestContext.Current.CancellationToken);
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<GetRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRecommendations_InvalidTakeWithPendingMovie_ReturnsBadRequestWithoutDispatch()
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Pending);
        HandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{Route(KnownId)}?take=51", cancellationToken);

        // assert
        await ReadProblemAsync(response, HttpStatusCode.BadRequest, cancellationToken);
        await movies.DidNotReceiveWithAnyArgs().GetAsync(Arg.Any<MovieId>(), TestContext.Current.CancellationToken);
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<GetRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetRecommendations_NonpositiveId_ReturnsNotFoundWithoutDispatch(int id)
    {
        // arrange
        HandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(id), cancellationToken);

        // assert
        await ReadProblemAsync(response, HttpStatusCode.NotFound, cancellationToken);
        await movies.DidNotReceiveWithAnyArgs().GetAsync(Arg.Any<MovieId>(), TestContext.Current.CancellationToken);
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<GetRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRecommendations_UnknownMovie_ReturnsNotFoundWithoutHandlerDispatch()
    {
        // arrange
        HandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(UnknownId), cancellationToken);

        // assert
        await ReadProblemAsync(response, HttpStatusCode.NotFound, cancellationToken);
        await movies.Received(1).GetAsync(new MovieId(UnknownId), Arg.Any<CancellationToken>());
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<GetRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRecommendations_PendingMovie_ReturnsOkEmptyArrayWithoutHandlerDispatch()
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Pending);
        HandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.GetArrayLength().ShouldBe(0);
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<GetRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("Enriched")]
    [InlineData("Failed")]
    [InlineData("NotFound")]
    public async Task GetRecommendations_NonPendingMovie_DispatchesTheHandler(string statusName)
    {
        // arrange
        var status = EnrichmentStatus.FromName(statusName, false);
        GivenSelectedMovie(status);
        HandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DispatchedQuery.ShouldNotBeNull().MovieId.ShouldBe(new MovieId(KnownId));
        await handler.Received(1)
            .HandleAsync(new GetRecommendationsQuery(new MovieId(KnownId), 10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecommendations_RankedResult_MapsExactlyTheTwelveCamelCaseFields()
    {
        // arrange
        var summary = Summary(EnrichmentStatus.Enriched, id: 11);
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns(Recommendation(summary, 9.75, "Shares the director.", "Shares two genres."));
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetArrayLength().ShouldBe(1);
        var item = body[0];
        PropertyNames(item).ShouldBe(DtoFieldNames);
        item.GetProperty("id").GetInt32().ShouldBe(summary.Id.Value);
        item.GetProperty("title").GetString().ShouldBe(summary.Title);
        item.GetProperty("posterUrl").GetString().ShouldBe(summary.PosterUrl);
        item.GetProperty("releaseYear").GetInt32().ShouldBe(summary.ReleaseYear!.Value);
        item.GetProperty("runtime").GetInt32().ShouldBe(summary.Runtime!.Value);
        item.GetProperty("imdbRating").GetDecimal().ShouldBe(summary.ImdbRating!.Value);
        item.GetProperty("enrichmentStatus").GetString().ShouldBe(summary.EnrichmentStatus.Name);
        item.GetProperty("isInWatchlist").GetBoolean().ShouldBe(summary.IsInWatchlist);
        item.GetProperty("isWatched").GetBoolean().ShouldBe(summary.IsWatched);
        item.GetProperty("isNotInterested").GetBoolean().ShouldBe(summary.IsNotInterested);
        item.GetProperty("score").GetDouble().ShouldBe(9.75);
        ReasonTexts(item).ShouldBe(["Shares the director.", "Shares two genres."]);
    }

    [Fact]
    public async Task GetRecommendations_NullPresentationValues_SerialiseAsJsonNull()
    {
        // arrange
        var summary = Summary(EnrichmentStatus.Enriched, id: 12, sparse: true);
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns(Recommendation(summary, 0, "Shares a genre."));
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var item = body[0];
        item.GetProperty("posterUrl").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("releaseYear").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("runtime").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("imdbRating").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetRecommendations_HandlerOrderDiffersFromScoreOrder_PreservesHandlerOrder()
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns(
            Recommendation(Summary(EnrichmentStatus.Enriched, id: 21), 1.25, "First."),
            Recommendation(Summary(EnrichmentStatus.Enriched, id: 22), 9.75, "Second."),
            Recommendation(Summary(EnrichmentStatus.Enriched, id: 23), 5.0, "Third."));
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ShouldBe([21, 22, 23]);
        body.EnumerateArray().Select(item => item.GetProperty("score").GetDouble()).ShouldBe([1.25, 9.75, 5.0]);
    }

    [Fact]
    public async Task GetRecommendations_ReasonAndScoreCases_MapScoreAndReasonTextVerbatim()
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns(
            Recommendation(Summary(EnrichmentStatus.Enriched, id: 31), 8.5, "Shares the director.", "Shares a genre."),
            Recommendation(Summary(EnrichmentStatus.Enriched, id: 32), 0, "Zero contribution underflow."),
            Recommendation(Summary(EnrichmentStatus.Enriched, id: 33), 0));
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetArrayLength().ShouldBe(3);
        body[0].GetProperty("score").GetDouble().ShouldBe(8.5);
        ReasonTexts(body[0]).ShouldBe(["Shares the director.", "Shares a genre."]);
        body[1].GetProperty("score").GetDouble().ShouldBe(0);
        ReasonTexts(body[1]).ShouldBe(["Zero contribution underflow."]);
        body[2].GetProperty("score").GetDouble().ShouldBe(0);
        ReasonTexts(body[2]).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRecommendations_ForwardsTheRequestCancellationTokenToBothDependencies()
    {
        // arrange
        CancellationToken preflightToken = default;
        CancellationToken handlerToken = default;
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Do<CancellationToken>(token => preflightToken = token))
            .Returns(MovieIn(EnrichmentStatus.Enriched));
        handler.HandleAsync(Arg.Any<GetRecommendationsQuery>(), Arg.Do<CancellationToken>(token => handlerToken = token))
            .Returns(ImmutableList<RecommendedMovie>.Empty);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        preflightToken.CanBeCanceled.ShouldBeTrue();
        handlerToken.CanBeCanceled.ShouldBeTrue();
        handlerToken.ShouldBe(preflightToken);
    }

    [Fact]
    public async Task GetRecommendations_CancelledDuringPreflight_PropagatesWithoutHandlerDispatch()
    {
        // arrange
        using var requestCancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var token = callInfo.ArgAt<CancellationToken>(1);
                entered.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                }
                catch (OperationCanceledException)
                {
                    cancelled.TrySetResult();
                    throw;
                }

                return (Movie?)MovieIn(EnrichmentStatus.Enriched);
            });
        HandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var request = client.GetAsync(Route(KnownId), requestCancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await requestCancellation.CancelAsync();

        // assert
        await Should.ThrowAsync<OperationCanceledException>(() => request);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<GetRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRecommendations_CancelledDuringHandler_PropagatesWithoutSuccessEmptyResponse()
    {
        // arrange
        using var requestCancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        handler.HandleAsync(Arg.Any<GetRecommendationsQuery>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var token = callInfo.ArgAt<CancellationToken>(1);
                entered.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                }
                catch (OperationCanceledException)
                {
                    cancelled.TrySetResult();
                    throw;
                }

                return ImmutableList<RecommendedMovie>.Empty;
            });
        await using var host = CreateHost();
        using var client = host.CreateClient();

        // act
        var request = client.GetAsync(Route(KnownId), requestCancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await requestCancellation.CancelAsync();

        // assert
        await Should.ThrowAsync<OperationCanceledException>(() => request);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRecommendations_PreflightFailure_PropagatesWithoutHandlerDispatch()
    {
        // arrange
        movies.GetAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Movie?>(new InvalidOperationException("preflight failed")));
        HandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);

        // assert
        await ReadProblemAsync(response, HttpStatusCode.InternalServerError, cancellationToken);
        await handler.DidNotReceiveWithAnyArgs().HandleAsync(Arg.Any<GetRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetRecommendations_HandlerFailure_IsNotConvertedIntoASuccessEmptyResult()
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        handler.HandleAsync(Arg.Any<GetRecommendationsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ImmutableList<RecommendedMovie>>(new InvalidOperationException("handler failed")));
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);

        // assert
        await ReadProblemAsync(response, HttpStatusCode.InternalServerError, cancellationToken);
        await movies.Received(1).GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>());
    }

    private static string Route(int id) => $"/api/movies/{id.ToString(CultureInfo.InvariantCulture)}/recommendations";

    private static IEnumerable<string> PropertyNames(JsonElement item) =>
        item.EnumerateObject().Select(property => property.Name);

    private static IEnumerable<string?> ReasonTexts(JsonElement item) =>
        item.GetProperty("reasons").EnumerateArray().Select(reason => reason.GetString());

    private static async Task<JsonElement> ReadProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expected,
        CancellationToken cancellationToken)
    {
        response.StatusCode.ShouldBe(expected);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        problem.GetProperty("status").GetInt32().ShouldBe((int)expected);
        return problem;
    }

    private static Movie MovieIn(EnrichmentStatus status)
    {
        var movie = Movie.Create(
            new MovieId(KnownId),
            Faker.Lorem.GetFirstWord(),
            new LibraryPath("C:/library/selected.mkv"),
            new MediaFormat("mkv"));

        if (status == EnrichmentStatus.Enriched)
        {
            movie.MarkEnriched(new MovieMetadata(movie.Title), DateTimeOffset.UnixEpoch);
        }
        else if (status == EnrichmentStatus.Failed)
        {
            movie.MarkFailed(EnrichmentFailureCategory.ProviderUnavailable, DateTimeOffset.UnixEpoch);
        }
        else if (status == EnrichmentStatus.NotFound)
        {
            movie.MarkNotFound(DateTimeOffset.UnixEpoch);
        }

        return movie;
    }

    private static RecommendedMovie Recommendation(MovieSummary summary, double score, params string[] reasons) =>
        new(summary, score, [.. reasons.Select(text => new Reason(text))]);

    private MovieSummary Summary(EnrichmentStatus status, int id, bool sparse = false)
    {
        fixture.Customize<MovieId>(composer => composer.FromFactory(() => new MovieId(id)));
        fixture.Customize<EnrichmentStatus>(composer => composer.FromFactory(() => status));

        var builder = fixture.Build<MovieSummary>()
            .With(item => item.Title, Faker.Lorem.GetFirstWord());

        if (sparse)
        {
            builder = builder
                .With(item => item.PosterUrl, (string?)null)
                .With(item => item.ReleaseYear, (int?)null)
                .With(item => item.Runtime, (int?)null)
                .With(item => item.ImdbRating, (decimal?)null);
        }

        return builder.Create();
    }

    private void GivenSelectedMovie(EnrichmentStatus status) =>
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(MovieIn(status));

    private void HandlerReturns(params RecommendedMovie[] items)
    {
        ImmutableList<RecommendedMovie> results = [.. items];
        handler.HandleAsync(
                Arg.Do<GetRecommendationsQuery>(query => DispatchedQuery = query),
                Arg.Any<CancellationToken>())
            .Returns(results);
    }

    private WebApplicationFactory<Program> CreateHost(
        int configuredTake = 10,
        int candidatePoolSize = DefaultCandidatePoolSize) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.Take)}"] =
                        configuredTake.ToString(CultureInfo.InvariantCulture),
                    [$"{RecommendationOptions.SectionName}:{nameof(RecommendationOptions.CandidatePoolSize)}"] =
                        candidatePoolSize.ToString(CultureInfo.InvariantCulture),
                }));
            builder.ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton(movies));
                services.Replace(ServiceDescriptor.Singleton(handler));
            });
        });
}
