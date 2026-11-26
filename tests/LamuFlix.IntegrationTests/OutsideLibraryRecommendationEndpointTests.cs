using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Recommendations;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class OutsideLibraryRecommendationEndpointTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const string Route = "/api/recommendations/not-in-library";
    private const string ForYouRoute = "/api/recommendations/for-you";
    private const string RelatedRoute = "/api/movies/7/recommendations";

    private readonly IMovieRepository movies = Substitute.For<IMovieRepository>();
    private readonly IQueryHandler<GetOutsideLibraryRecommendationsQuery, IReadOnlyList<OutsideLibraryRecommendation>> handler =
        Substitute.For<IQueryHandler<GetOutsideLibraryRecommendationsQuery, IReadOnlyList<OutsideLibraryRecommendation>>>();
    private readonly IQueryHandler<GetForYouRecommendationsQuery, ForYouRecommendationsPage> forYouHandler =
        Substitute.For<IQueryHandler<GetForYouRecommendationsQuery, ForYouRecommendationsPage>>();
    private readonly IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>> relatedHandler =
        Substitute.For<IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>>();

    [Fact]
    public async Task GetNotInLibrary_Configured_ReturnsCardArrayWithOnlyTheContractFields()
    {
        // arrange
        OutsideHandlerReturns(
            new OutsideLibraryRecommendation(101, "<b>Array</b>", 1979, "/abc123.jpg", "Recommended for Alien and Heat"),
            new OutsideLibraryRecommendation(102, "Sequel", null, null, "Recommended for Alien and Heat"));
        await using var host = CreateHost(tmdbEnabled: true, apiKey: "configured-key");
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.GetArrayLength().ShouldBe(2);
        body[0].EnumerateObject().Select(property => property.Name).ShouldBe(
            ["tmdbId", "title", "releaseYear", "posterPath", "reason"], ignoreOrder: true);
        body[0].GetProperty("tmdbId").GetInt32().ShouldBe(101);
        body[0].GetProperty("title").GetString().ShouldBe("<b>Array</b>");
        body[0].GetProperty("releaseYear").GetInt32().ShouldBe(1979);
        body[0].GetProperty("posterPath").GetString().ShouldBe("/abc123.jpg");
        body[0].GetProperty("reason").GetString().ShouldBe("Recommended for Alien and Heat");
        body[1].GetProperty("releaseYear").ValueKind.ShouldBe(JsonValueKind.Null);
        body[1].GetProperty("posterPath").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetNotInLibrary_FilteredShortRow_PreservesStatusOrderAndCardFieldSet()
    {
        // arrange
        OutsideHandlerReturns(
            new OutsideLibraryRecommendation(945971, "Next Eligible Survivor", 1997, null, "Recommended for Alien and Heat"),
            new OutsideLibraryRecommendation(945972, "Collection Representative", 1998, null, "Recommended for Alien and Heat"));
        await using var host = CreateHost(tmdbEnabled: true, apiKey: "configured-key");
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.GetArrayLength().ShouldBe(2);
        body.EnumerateArray().Select(card => card.GetProperty("tmdbId").GetInt32()).ShouldBe([945971, 945972]);
        foreach (var card in body.EnumerateArray())
        {
            card.EnumerateObject().Select(property => property.Name).ShouldBe(
                ["tmdbId", "title", "releaseYear", "posterPath", "reason"],
                ignoreOrder: true);
            card.GetProperty("posterPath").ValueKind.ShouldBe(JsonValueKind.Null);
        }
    }

    [Fact]
    public async Task GetNotInLibrary_ChallengesUnauthenticatedBeforeAnyEmptyOrHandlerBranch()
    {
        // arrange
        OutsideHandlerReturns(new OutsideLibraryRecommendation(101, "Array", 1979, null, "Recommended for Alien and Heat"));
        await using var host = CreateHost(tmdbEnabled: true, apiKey: "configured-key", enforceAuthentication: true);
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await handler.DidNotReceiveWithAnyArgs()
            .HandleAsync(Arg.Any<GetOutsideLibraryRecommendationsQuery>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false, "configured-key")]
    [InlineData(true, "  ")]
    public async Task GetNotInLibrary_TmdbUnconfigured_ReturnsOkEmptyWithoutDispatching(bool enabled, string apiKey)
    {
        // arrange
        OutsideHandlerReturns(new OutsideLibraryRecommendation(101, "Array", 1979, null, "Recommended for Alien and Heat"));
        await using var host = CreateHost(tmdbEnabled: enabled, apiKey: apiKey);
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.GetArrayLength().ShouldBe(0);
        await handler.DidNotReceiveWithAnyArgs()
            .HandleAsync(Arg.Any<GetOutsideLibraryRecommendationsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetNotInLibrary_ConfiguredWithoutCandidates_ReturnsOkEmpty()
    {
        // arrange
        OutsideHandlerReturns();
        await using var host = CreateHost(tmdbEnabled: true, apiKey: "configured-key");
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.GetArrayLength().ShouldBe(0);
        await handler.Received(1)
            .HandleAsync(Arg.Any<GetOutsideLibraryRecommendationsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetNotInLibrary_MakesNoHttpRequests()
    {
        // arrange
        var requests = new RequestRecorder();
        OutsideHandlerReturns(new OutsideLibraryRecommendation(101, "Array", 1979, null, "Recommended for Alien and Heat"));
        await using var host = CreateHost(tmdbEnabled: true, apiKey: "configured-key", requests: requests);
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(Route, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        requests.Uris.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetNotInLibrary_LeavesForYouIdsAndOrderUnchanged()
    {
        // arrange
        OutsideHandlerReturns(new OutsideLibraryRecommendation(101, "Array", 1979, null, "Recommended for Alien and Heat"));
        var first = Recommended(
            new MovieSummary(
                new MovieId(51),
                "First",
                null,
                null,
                null,
                null,
                EnrichmentStatus.Enriched,
                false,
                false,
                Review.None),
            4.5);
        var second = Recommended(
            new MovieSummary(
                new MovieId(52),
                "Second",
                null,
                null,
                null,
                null,
                EnrichmentStatus.Enriched,
                false,
                false,
                Review.None),
            -1.25);
        forYouHandler.HandleAsync(Arg.Any<GetForYouRecommendationsQuery>(), Arg.Any<CancellationToken>())
            .Returns(new ForYouRecommendationsPage([first, second], false, true));
        await using var host = CreateHost(tmdbEnabled: true, apiKey: "configured-key");
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        await client.GetAsync(Route, cancellationToken);
        var response = await client.GetAsync(ForYouRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("id").GetInt32()).ShouldBe([51, 52]);
        body.GetRawText().ShouldNotContain("tmdbId");
        body.GetRawText().ShouldNotContain("not-in-library");
    }

    [Fact]
    public async Task GetNotInLibrary_LeavesRelatedLibraryOnlyAndCrowdConsumersIsolated()
    {
        // arrange
        OutsideHandlerReturns(new OutsideLibraryRecommendation(101, "Array", 1979, null, "Recommended for Alien and Heat"));
        var seed = Movie.Create(
            new MovieId(7),
            "Related seed",
            new LibraryPath("C:/library/related-seed.mkv"),
            new MediaFormat("mkv"));
        seed.MarkEnriched(new MovieMetadata(seed.Title), DateTimeOffset.UnixEpoch);
        movies.GetAsync(new MovieId(7), Arg.Any<CancellationToken>()).Returns(seed);
        relatedHandler.HandleAsync(Arg.Any<GetRecommendationsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ImmutableList<RecommendedMovie>.Empty);
        await using var host = CreateHost(tmdbEnabled: true, apiKey: "configured-key");
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(RelatedRoute, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await handler.DidNotReceiveWithAnyArgs()
            .HandleAsync(Arg.Any<GetOutsideLibraryRecommendationsQuery>(), Arg.Any<CancellationToken>());
        await forYouHandler.DidNotReceiveWithAnyArgs()
            .HandleAsync(Arg.Any<GetForYouRecommendationsQuery>(), Arg.Any<CancellationToken>());
        await relatedHandler.Received(1)
            .HandleAsync(Arg.Any<GetRecommendationsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Routes_BothRecommendationReads_ShareTheSameAuthorizationConvention()
    {
        // arrange
        using var host = new ApiHostFactory();
        var endpoints = host.Services.GetRequiredService<Microsoft.AspNetCore.Routing.EndpointDataSource>().Endpoints
            .OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>()
            .ToArray();

        // act
        var discovery = endpoints.First(endpoint => RouteOf(endpoint) == Route);
        var forYou = endpoints.First(endpoint => RouteOf(endpoint) == ForYouRoute);

        // assert
        AuthorizeMetadata(discovery).ShouldBe(AuthorizeMetadata(forYou));
        AllowAnonymousMetadata(discovery).ShouldBe(AllowAnonymousMetadata(forYou));
    }

    private void OutsideHandlerReturns(params OutsideLibraryRecommendation[] cards) =>
        handler.HandleAsync(Arg.Any<GetOutsideLibraryRecommendationsQuery>(), Arg.Any<CancellationToken>())
            .Returns(cards);

    private WebApplicationFactory<Program> CreateHost(
        bool tmdbEnabled,
        string apiKey,
        bool enforceAuthentication = false,
        RequestRecorder? requests = null) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = tmdbEnabled.ToString(),
                    [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.ApiKey)}"] = apiKey,
                    [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.BaseUrl)}"] = "https://example.invalid/",
                }));
            builder.ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton(movies));
                services.Replace(ServiceDescriptor.Singleton(handler));
                services.Replace(ServiceDescriptor.Singleton(forYouHandler));
                services.Replace(ServiceDescriptor.Singleton(relatedHandler));
                if (enforceAuthentication)
                {
                    services.AddAuthentication("Rejecting")
                        .AddScheme<AuthenticationSchemeOptions, RejectingAuthenticationHandler>("Rejecting", null);
                    services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder("Rejecting")
                        .RequireAuthenticatedUser()
                        .Build());
                }

                if (requests is not null)
                {
                    services.ConfigureHttpClientDefaults(client =>
                        client.AddHttpMessageHandler(() => new RecordingHandler(requests)));
                }
            });
        });

    private static RecommendedMovie Recommended(MovieSummary summary, double score) =>
        new(summary, score, [new RecommendationEvidence("Director", "Same director", ["Ridley Scott"])]);

    private static string RouteOf(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint) =>
        endpoint.RoutePattern.RawText ?? string.Empty;

    private static string[] AuthorizeMetadata(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint) =>
        [.. endpoint.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Select(data => $"{data.Policy}|{data.Roles}|{data.AuthenticationSchemes}")];

    private static bool AllowAnonymousMetadata(Microsoft.AspNetCore.Routing.RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;

    private sealed class RequestRecorder
    {
        internal List<Uri?> Uris { get; } = [];
    }

    private sealed class RecordingHandler(RequestRecorder recorder) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            recorder.Uris.Add(request.RequestUri);
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class RejectingAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
            Task.FromResult(AuthenticateResult.NoResult());
    }
}
