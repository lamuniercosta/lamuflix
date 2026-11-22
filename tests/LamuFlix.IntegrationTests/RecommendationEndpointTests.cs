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
using LamuFlix.Infrastructure.Pipeline;
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
    private const string ForYouRoute = "/api/recommendations/for-you";

    private static readonly string[] RelatedDtoFieldNames =
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
        "review",
        "score",
        "evidence",
    ];

    private static readonly string[] ForYouDtoFieldNames =
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
        "review",
        "score",
        "evidence",
        "seed",
    ];

    private static readonly string[] ForYouDtoFieldNamesWithStartedCollectionId =
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
        "review",
        "score",
        "evidence",
        "seed",
        "startedCollectionId",
    ];

    private readonly Fixture fixture = new();
    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IRecommendationCandidateSource candidateSource = Substitute.For<IRecommendationCandidateSource>();
    private readonly IRecommendationProfileSource profileSource = Substitute.For<IRecommendationProfileSource>();
    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>> handler =
        Substitute.For<IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>>();
    private readonly IQueryHandler<GetForYouRecommendationsQuery, ForYouRecommendationsPage> forYouHandler =
        Substitute.For<IQueryHandler<GetForYouRecommendationsQuery, ForYouRecommendationsPage>>();

    private GetRecommendationsQuery? DispatchedQuery { get; set; }

    private GetForYouRecommendationsQuery? DispatchedForYouQuery { get; set; }

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

    [Fact]
    public async Task GetRecommendations_UnreachableBarWithEmptyStrongReasonCriteria_ReturnsOkEmptyArray()
    {
        // arrange
        var selected = MovieIn(EnrichmentStatus.Enriched);
        movies.GetAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(selected);
        candidateSource.GetLibraryFeaturesAsync(Arg.Any<CancellationToken>()).Returns(
            new RecommendationLibrarySnapshot(
            [
                LibraryEntry(selected, PlotVector(primary: 1f)),
                LibraryEntry(OtherMovie(id: KnownId + 1, genres: ["Drama"]), plotEmbedding: null),
            ],
            CrowdLinks.Empty));
        profileSource.GetTasteProfileAsync(Arg.Any<CancellationToken>()).Returns(EmptyProfile());
        profileSource.GetProfileInputsAsync(
                Arg.Any<IReadOnlyCollection<MovieId>>(),
                Arg.Any<CancellationToken>())
            .Returns(new RecommendationProfileInputs(
                [],
                ImmutableDictionary<MovieId, ImmutableArray<float>>.Empty));
        catalog.GetGenreCountsAsync(Arg.Any<CancellationToken>())
            .Returns(new GenreCounts(2, new Dictionary<string, long> { ["Drama"] = 2 }));
        catalog.GetKeywordCountsAsync(Arg.Any<CancellationToken>())
            .Returns(new KeywordCounts(2, new Dictionary<string, long>()));
        var options = new RecommendationOptions { MinimumSimilarity = 1.0, StrongReasonCriteria = [] };
        await using var host = CreateHost(relatedHandler: RelatedHandler(options));
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
    }

    [Theory]
    [InlineData("Enriched")]
    [InlineData("Failed")]
    [InlineData("NotFound")]
    public async Task GetRecommendations_NonPendingMovie_DispatchesTheHandler(string statusName)
    {
        // arrange
        var status = EnrichmentStatus.FromName(statusName);
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
            .HandleAsync(new GetRecommendationsQuery(new MovieId(KnownId)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRecommendations_RankedResult_MapsExactlyTheThirteenCamelCaseFields()
    {
        // arrange
        var summary = Summary(EnrichmentStatus.Enriched, id: 11);
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns(Recommendation(
            summary,
            9.75,
            Evidence("Director", "Same director: Ada", "Ada"),
            Evidence("Genre", "Same genres: Drama", "Drama")));
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
        PropertyNames(item).ShouldBe(RelatedDtoFieldNames);
        ShouldNotCarryAnyCasingOf(item, "reasons", "seed", "startedCollectionId");
        item.GetProperty("id").GetInt32().ShouldBe(summary.Id.Value);
        item.GetProperty("title").GetString().ShouldBe(summary.Title);
        item.GetProperty("posterUrl").GetString().ShouldBe(summary.PosterUrl);
        item.GetProperty("releaseYear").GetInt32().ShouldBe(summary.ReleaseYear.ShouldNotBeNull());
        item.GetProperty("runtime").GetInt32().ShouldBe(summary.Runtime.ShouldNotBeNull());
        item.GetProperty("imdbRating").GetDecimal().ShouldBe(summary.ImdbRating.ShouldNotBeNull());
        item.GetProperty("enrichmentStatus").GetString().ShouldBe(summary.EnrichmentStatus.Name);
        item.GetProperty("isInWatchlist").GetBoolean().ShouldBe(summary.IsInWatchlist);
        item.GetProperty("isWatched").GetBoolean().ShouldBe(summary.IsWatched);
        item.GetProperty("isNotInterested").GetBoolean().ShouldBe(summary.IsNotInterested);
        item.GetProperty("score").GetDouble().ShouldBe(9.75);
        EvidenceCriteria(item).ShouldBe(["Director", "Genre"]);
        EvidenceTexts(item).ShouldBe(["Same director: Ada", "Same genres: Drama"]);
        MatchedNames(item).ShouldBe(["Ada", "Drama"]);
    }

    [Fact]
    public async Task GetRecommendations_NullPresentationValues_SerialiseAsJsonNull()
    {
        // arrange
        var summary = Summary(EnrichmentStatus.Enriched, id: 12, sparse: true);
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns(Recommendation(summary, 0, Evidence("Genre", "Same genres: Drama", "Drama")));
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
        ShouldNotCarryAnyCasingOf(item, "seed");
    }

    [Fact]
    public async Task GetRecommendations_HandlerOrderDiffersFromScoreOrder_PreservesHandlerOrder()
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns(
            Recommendation(Summary(EnrichmentStatus.Enriched, id: 21), 1.25, Evidence("Director", "First.", "First")),
            Recommendation(Summary(EnrichmentStatus.Enriched, id: 22), 9.75, Evidence("Director", "Second.", "Second")),
            Recommendation(Summary(EnrichmentStatus.Enriched, id: 23), 5.0, Evidence("Director", "Third.", "Third")));
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
    public async Task GetRecommendations_CriterionAndScoreCases_MapTextAndNamesVerbatim()
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns(
            Recommendation(
                Summary(EnrichmentStatus.Enriched, id: 31),
                8.5,
                Evidence("Director", "Same director: Ada", "Ada"),
                Evidence("Genre", "Same genres: Drama", "Drama")),
            Recommendation(Summary(EnrichmentStatus.Enriched, id: 32), 0, Evidence("Cast", "Shared cast: Actor", "Actor")),
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
        EvidenceCriteria(body[0]).ShouldBe(["Director", "Genre"]);
        EvidenceTexts(body[0]).ShouldBe(["Same director: Ada", "Same genres: Drama"]);
        MatchedNames(body[0]).ShouldBe(["Ada", "Drama"]);
        body[1].GetProperty("score").GetDouble().ShouldBe(0);
        EvidenceTexts(body[1]).ShouldBe(["Shared cast: Actor"]);
        body[2].GetProperty("score").GetDouble().ShouldBe(0);
        body[2].GetProperty("evidence").ValueKind.ShouldBe(JsonValueKind.Array);
        EvidenceTexts(body[2]).ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRecommendations_Evidence_SerialisesCriterionTextAndMatchedNameTriples()
    {
        // arrange
        GivenSelectedMovie(EnrichmentStatus.Enriched);
        HandlerReturns(Recommendation(
            Summary(EnrichmentStatus.Enriched, id: 41),
            2.5,
            Evidence("Cast", "Shared cast: Name One, Name Two, Name Three +2", "Name One", "Name Two", "Name Three", "Name Four", "Name Five")));
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route(KnownId), cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        ShouldNotCarryAnyCasingOf(body[0], "reasons", "seed");
        var evidence = body[0].GetProperty("evidence").EnumerateArray().ToList();
        evidence.ShouldHaveSingleItem();
        evidence[0].EnumerateObject().Select(property => property.Name)
            .ShouldBe(["criterion", "text", "matchedNames"], ignoreOrder: true);
        evidence[0].GetProperty("criterion").GetString().ShouldBe("Cast");
        evidence[0].GetProperty("text").GetString().ShouldBe("Shared cast: Name One, Name Two, Name Three +2");
        MatchedNames(body[0]).ShouldBe(["Name One", "Name Two", "Name Three", "Name Four", "Name Five"]);
        body[0].GetRawText().ShouldNotContain("Matches your viewing history");
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

    [Fact]
    public async Task GetForYouRecommendations_RankedResult_MapsSeedThenEvidenceInHandlerOrder()
    {
        // arrange
        var first = ForYouRecommendation(
            Summary(EnrichmentStatus.Enriched, id: 51),
            4.5,
            new RecommendationSeed(90, "Alien", SeedSignal.WatchedWithoutReview),
            Evidence("Director", "Same director: Ridley Scott", "Ridley Scott"));
        var second = ForYouRecommendation(
            Summary(EnrichmentStatus.Enriched, id: 52),
            -1.25,
            new RecommendationSeed(91, "Heat", SeedSignal.Watchlist));
        ForYouHandlerReturns(true, first, second);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(ForYouRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        body.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        body.GetProperty("hasSeeds").GetBoolean().ShouldBeTrue();
        var items = body.GetProperty("items");
        items.GetArrayLength().ShouldBe(2);
        items.EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ShouldBe([51, 52]);
        items[0].GetProperty("score").GetDouble().ShouldBe(4.5);
        PropertyNames(items[0]).ShouldBe(ForYouDtoFieldNames);
        ShouldNotCarryAnyCasingOf(items[0], "reasons");
        var firstSeed = items[0].GetProperty("seed");
        firstSeed.GetProperty("movieId").GetInt32().ShouldBe(90);
        firstSeed.GetProperty("title").GetString().ShouldBe("Alien");
        firstSeed.GetProperty("signal").GetString().ShouldBe("watched");
        EvidenceCriteria(items[0]).ShouldBe(["Director"]);
        EvidenceTexts(items[0]).ShouldBe(["Same director: Ridley Scott"]);
        MatchedNames(items[0]).ShouldBe(["Ridley Scott"]);
        items[1].GetProperty("score").GetDouble().ShouldBe(-1.25);
        items[1].GetProperty("seed").GetProperty("signal").GetString().ShouldBe("watchlist");
        items[1].GetProperty("evidence").ValueKind.ShouldBe(JsonValueKind.Array);
        EvidenceTexts(items[1]).ShouldBeEmpty();
        body.GetRawText().ShouldNotContain("Matches your viewing history");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetForYouRecommendations_NoEligibleRecommendations_ReturnsOkEmptyArray(int itemCount)
    {
        // arrange
        ForYouHandlerReturns([.. Enumerable.Range(0, itemCount)
            .Select(index => Recommendation(Summary(EnrichmentStatus.Enriched, id: 60 + index), index))]);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(ForYouRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ValueKind.ShouldBe(JsonValueKind.Object);
        body.GetProperty("items").GetArrayLength().ShouldBe(itemCount);
        body.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        body.GetProperty("hasSeeds").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task GetForYouRecommendations_NoHistoryOrNotInterestedOnly_ReturnsOkEmptyArray()
    {
        // arrange
        ForYouHandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(ForYouRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        body.ValueKind.ShouldBe(JsonValueKind.Object);
        body.GetProperty("items").GetArrayLength().ShouldBe(0);
        body.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        DispatchedForYouQuery.ShouldNotBeNull().Take.ShouldBe(10);
    }

    [Fact]
    public async Task GetForYouRecommendations_OmittedTake_DefaultsToTenIndependentlyOfConfiguredTake()
    {
        // arrange
        ForYouHandlerReturns();
        await using var host = CreateHost(configuredTake: 7, candidatePoolSize: DefaultCandidatePoolSize);
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(ForYouRoute, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DispatchedForYouQuery.ShouldNotBeNull().Take.ShouldBe(10);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(5, 1)]
    [InlineData(50, 50)]
    public async Task GetForYouRecommendations_TakeInsideInclusiveConfiguredBound_DispatchesThatTake(
        int candidatePoolSize,
        int take)
    {
        // arrange
        ForYouHandlerReturns();
        await using var host = CreateHost(configuredTake: 1, candidatePoolSize: candidatePoolSize);
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(
            $"{ForYouRoute}?take={take.ToString(CultureInfo.InvariantCulture)}",
            cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DispatchedForYouQuery.ShouldNotBeNull().Take.ShouldBe(take);
    }

    [Fact]
    public async Task GetForYouRecommendations_OffsetAndDuplicateExcludes_DispatchNormalizedQuery()
    {
        ForYouHandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();

        var response = await client.GetAsync($"{ForYouRoute}?offset=4&exclude=7&exclude=7&exclude=8",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DispatchedForYouQuery.ShouldNotBeNull().Offset.ShouldBe(4);
        DispatchedForYouQuery.ShouldNotBeNull().ExcludedMovieIds.ShouldBe(ImmutableHashSet.Create(new MovieId(7), new MovieId(8)));
    }

    [Fact]
    public async Task GetForYouRecommendations_HugeOffset_ReturnsEmptyEnvelope()
    {
        ForYouHandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();

        var response = await client.GetAsync($"{ForYouRoute}?offset={int.MaxValue.ToString(CultureInfo.InvariantCulture)}",
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        body.GetProperty("items").GetArrayLength().ShouldBe(0);
        body.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        body.GetProperty("hasSeeds").GetBoolean().ShouldBeFalse();
        DispatchedForYouQuery.ShouldNotBeNull().Offset.ShouldBe(int.MaxValue);
    }

    [Fact]
    public async Task GetForYouRecommendations_StartedCollection_PopulatesStartedCollectionIdOnTheWire()
    {
        // arrange
        ForYouHandlerReturns(
            true,
            ForYouCollectionRecommendation(
                Summary(EnrichmentStatus.Enriched, id: 51),
                4.5,
                new RecommendationSeed(90, "Alien", SeedSignal.Liked),
                7));
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(ForYouRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("hasSeeds").GetBoolean().ShouldBeTrue();
        var item = body.GetProperty("items")[0];
        PropertyNames(item).ShouldBe(ForYouDtoFieldNamesWithStartedCollectionId);
        item.GetProperty("startedCollectionId").GetInt32().ShouldBe(7);
    }

    [Fact]
    public async Task GetForYouRecommendations_NullStartedCollectionId_IsOmittedOnTheWire()
    {
        // arrange
        ForYouHandlerReturns(
            ForYouRecommendation(
                Summary(EnrichmentStatus.Enriched, id: 51),
                4.5,
                new RecommendationSeed(90, "Alien", SeedSignal.Liked)));
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(ForYouRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var item = body.GetProperty("items")[0];
        PropertyNames(item).ShouldBe(ForYouDtoFieldNames);
        ShouldNotCarryAnyCasingOf(item, "startedCollectionId");
        item.TryGetProperty("startedCollectionId", out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetForYouRecommendations_EmptyPage_MapsCoreHasSeedsOntoTheEnvelope(bool hasSeeds)
    {
        // arrange
        ForYouHandlerReturns(hasSeeds);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(ForYouRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("items").GetArrayLength().ShouldBe(0);
        body.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        body.GetProperty("hasSeeds").GetBoolean().ShouldBe(hasSeeds);
    }

    [Fact]
    public async Task GetForYouRecommendations_ExcludeRawLimitCountsRepeatedValuesBeforeDeduplication()
    {
        ForYouHandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var query = string.Join("&", Enumerable.Repeat("exclude=7", 51));

        var response = await client.GetAsync($"{ForYouRoute}?{query}", TestContext.Current.CancellationToken);

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest, TestContext.Current.CancellationToken);
        problem.GetRawText().ShouldNotContain("exclude=7");
        await forYouHandler.DidNotReceiveWithAnyArgs()
            .HandleAsync(Arg.Any<GetForYouRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("offset=")]
    [InlineData("offset=abc")]
    [InlineData("offset=-1")]
    [InlineData("offset=2147483648")]
    [InlineData("offset=1&offset=2")]
    [InlineData("exclude=")]
    [InlineData("exclude=0")]
    [InlineData("exclude=-1")]
    [InlineData("exclude=2147483648")]
    public async Task GetForYouRecommendations_InvalidOffsetOrExclude_ReturnsBadRequestWithoutDispatching(string query)
    {
        ForYouHandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();

        var response = await client.GetAsync($"{ForYouRoute}?{query}", TestContext.Current.CancellationToken);

        await ReadProblemAsync(response, HttpStatusCode.BadRequest, TestContext.Current.CancellationToken);
        await forYouHandler.DidNotReceiveWithAnyArgs()
            .HandleAsync(Arg.Any<GetForYouRecommendationsQuery>(), TestContext.Current.CancellationToken);
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
    public async Task GetForYouRecommendations_InvalidTake_ReturnsBadRequestProblemWithoutDispatching(string query)
    {
        // arrange
        ForYouHandlerReturns();
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{ForYouRoute}?{query}", cancellationToken);

        // assert
        await ReadProblemAsync(response, HttpStatusCode.BadRequest, cancellationToken);
        await forYouHandler.DidNotReceiveWithAnyArgs()
            .HandleAsync(Arg.Any<GetForYouRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetForYouRecommendations_OmittedTakeAboveSmallerValidPool_ReturnsBadRequestProblem()
    {
        // arrange
        ForYouHandlerReturns();
        await using var host = CreateHost(configuredTake: 5, candidatePoolSize: 5);
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(ForYouRoute, cancellationToken);

        // assert
        await ReadProblemAsync(response, HttpStatusCode.BadRequest, cancellationToken);
        await forYouHandler.DidNotReceiveWithAnyArgs()
            .HandleAsync(Arg.Any<GetForYouRecommendationsQuery>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetForYouRecommendations_DirectQueryOutsideBounds_FailsThroughTheComposedValidationDecorator()
    {
        // arrange
        await using var host = CreateHost(composeForYouHandler: true);
        using var scope = host.Services.CreateScope();
        var composed = scope.ServiceProvider
            .GetRequiredService<IQueryHandler<GetForYouRecommendationsQuery, ForYouRecommendationsPage>>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var failure = await Should.ThrowAsync<ValidationException>(() => composed.HandleAsync(
            new GetForYouRecommendationsQuery(DefaultCandidatePoolSize + 1),
            cancellationToken));

        // assert
        composed.ShouldBeOfType<TracingDecorator<GetForYouRecommendationsQuery, ForYouRecommendationsPage>>();
        failure.Errors.Keys.ShouldContain("Take");
    }

    private static string Route(int id) => $"/api/movies/{id.ToString(CultureInfo.InvariantCulture)}/recommendations";

    private static IEnumerable<string> PropertyNames(JsonElement item) =>
        item.EnumerateObject().Select(property => property.Name);

    private static void ShouldNotCarryAnyCasingOf(JsonElement item, params string[] names) =>
        item.EnumerateObject()
            .ShouldNotContain(property => names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)));

    private static IEnumerable<string?> EvidenceCriteria(JsonElement item) =>
        item.GetProperty("evidence").EnumerateArray().Select(evidence => evidence.GetProperty("criterion").GetString());

    private static IEnumerable<string?> EvidenceTexts(JsonElement item) =>
        item.GetProperty("evidence").EnumerateArray().Select(evidence => evidence.GetProperty("text").GetString());

    private static IEnumerable<string> MatchedNames(JsonElement item) =>
        item.GetProperty("evidence").EnumerateArray()
            .SelectMany(evidence => evidence.GetProperty("matchedNames").EnumerateArray())
            .Select(name => name.GetString() ?? string.Empty);

    private static async Task<JsonElement> ReadProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expected,
        CancellationToken cancellationToken)
    {
        response.StatusCode.ShouldBe(expected);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        problem.GetProperty("status").GetInt32().ShouldBe((int)expected);
        problem.TryGetProperty("traceId", out _).ShouldBeTrue(problem.GetRawText());
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

    private static Movie OtherMovie(int id, string[] genres)
    {
        var movie = Movie.Create(
            new MovieId(id),
            Faker.Lorem.GetFirstWord(),
            new LibraryPath($"C:/library/candidate-{id}.mkv"),
            new MediaFormat("mkv"));
        movie.MarkEnriched(new MovieMetadata(movie.Title, genres: genres), DateTimeOffset.UnixEpoch);
        return movie;
    }

    private static RecommendationLibraryEntry LibraryEntry(Movie movie, float[]? plotEmbedding) =>
        new(MovieFeaturesMapper.From(movie, includeSignals: true, plotEmbedding), MovieSummaryMapper.From(movie));

    private static float[] PlotVector(float primary)
    {
        var vector = new float[EmbeddingDimensions.Value];
        vector[0] = primary;
        return vector;
    }

    private static TasteProfile EmptyProfile() =>
        new(ImmutableDictionary<string, double>.Empty);

    private GetRecommendationsQueryHandler RelatedHandler(RecommendationOptions options, TmdbOptions? tmdbOptions = null) =>
        new(
            movies,
            candidateSource,
            profileSource,
            new GenreWeightsCalculator(catalog, new GenreWeightsCache()),
            new KeywordWeightsCalculator(catalog, new KeywordWeightsCache()),
            Microsoft.Extensions.Options.Options.Create(options),
            Microsoft.Extensions.Options.Options.Create(tmdbOptions ?? new TmdbOptions()));

    private static RecommendationEvidence Evidence(string criterion, string text, params string[] matchedNames) =>
        new(criterion, text, [.. matchedNames]);

    private static RecommendedMovie Recommendation(MovieSummary summary, double score, params RecommendationEvidence[] evidence) =>
        new(summary, score, [.. evidence]);

    private static RecommendedMovie ForYouRecommendation(
        MovieSummary summary,
        double score,
        RecommendationSeed seed,
        params RecommendationEvidence[] evidence) =>
        new(summary, score, [.. evidence], seed);

    private static RecommendedMovie ForYouCollectionRecommendation(
        MovieSummary summary,
        double score,
        RecommendationSeed seed,
        int startedCollectionId,
        params RecommendationEvidence[] evidence) =>
        new(summary, score, [.. evidence], seed, startedCollectionId);

    private MovieSummary Summary(EnrichmentStatus status, int id, bool sparse = false)
    {
        fixture.Customize<MovieId>(composer => composer.FromFactory(() => new MovieId(id)));
        fixture.Customize<EnrichmentStatus>(composer => composer.FromFactory(() => status));
        fixture.Customize<Review>(composer => composer.FromFactory(() => Review.None));
        fixture.Inject<EnrichmentFailureCategory?>(null);

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

    private void ForYouHandlerReturns(params RecommendedMovie[] items) => ForYouHandlerReturns(false, items);

    private void ForYouHandlerReturns(bool hasSeeds, params RecommendedMovie[] items)
    {
        var results = new ForYouRecommendationsPage([.. items], false, hasSeeds);
        forYouHandler.HandleAsync(
                Arg.Do<GetForYouRecommendationsQuery>(query => DispatchedForYouQuery = query),
                Arg.Any<CancellationToken>())
            .Returns(results);
    }

    private WebApplicationFactory<Program> CreateHost(
        int configuredTake = 10,
        int candidatePoolSize = DefaultCandidatePoolSize,
        bool composeForYouHandler = false,
        IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>? relatedHandler = null) =>
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
                services.Replace(ServiceDescriptor.Singleton(
                    relatedHandler ?? handler));

                if (!composeForYouHandler)
                {
                    services.Replace(ServiceDescriptor.Singleton(forYouHandler));
                }
            });
        });
}
