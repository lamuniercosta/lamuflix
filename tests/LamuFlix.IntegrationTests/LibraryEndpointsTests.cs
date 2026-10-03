using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FsCheck;
using FsCheck.Fluent;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Features.Library;
using LamuFlix.Core.Library;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Library;
using LamuFlix.UnitTests.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class LibraryEndpointsTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const string BrowseRoute = "/api/movies";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string RequiredQuery = "sort=Title&direction=Ascending&page=1&pageSize=20";

    private readonly RecordingMovieCatalog catalog = new();

    [Fact]
    public void Validators_StartedComposition_ResolveForBothHandlerQueries()
    {
        // arrange
        using var scope = factory.Services.CreateScope();

        // act
        var browse = scope.ServiceProvider.GetServices<IValidator<BrowseMoviesQuery>>().ToList();
        var details = scope.ServiceProvider.GetServices<IValidator<GetMovieDetailsQuery>>().ToList();

        // assert
        browse.ShouldHaveSingleItem().ShouldBeOfType<BrowseMoviesQueryValidator>();
        details.ShouldHaveSingleItem().ShouldBeOfType<GetMovieDetailsQueryValidator>();
    }

    [Fact]
    public async Task BrowseMovies_CodecQuery_DispatchesTheDecodedQueryAndReturnsThePage()
    {
        // arrange
        var query = Accepted();
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{BrowseRoute}?{MovieQueryString.Format(query)}", cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        body.GetProperty("totalCount").GetInt32().ShouldBe(1);
        body.GetProperty("items")[0].GetProperty("title").GetString().ShouldBe("Heat");
        catalog.Queries.ShouldHaveSingleItem().ShouldBe(query);
    }

    [Fact]
    [Trait("Category", "Property")]
    public void BrowseMovies_GeneratedQuery_RoundTripsToAnEqualDispatchedQuery()
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var property = Prop.ForAll(MovieQueryFixture.Queries(), query =>
        {
            catalog.Queries.Clear();
            var response = client.GetAsync($"{BrowseRoute}?{MovieQueryString.Format(query)}", cancellationToken)
                .GetAwaiter()
                .GetResult();

            return response.IsSuccessStatusCode && catalog.Queries.Single().Equals(query);
        });

        // assert
        property.QuickCheckThrowOnFailure();
    }

    [Theory]
    [InlineData("text=&", "")]
    [InlineData("", null)]
    public async Task BrowseMovies_TextKey_PreservesEmptyVersusAbsent(string prefix, string? expected)
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}?{prefix}{RequiredQuery}",
            cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        catalog.Queries.ShouldHaveSingleItem().Text.ShouldBe(expected);
    }

    [Fact]
    public async Task BrowseMovies_RepeatedArrayKeys_PreserveRequestOrder()
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}?genreIds=3&genreIds=1&genreIds=2&statuses=Failed&statuses=Pending&{RequiredQuery}",
            cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dispatched = catalog.Queries.ShouldHaveSingleItem();
        dispatched.GenreIds.ShouldBe([3, 1, 2]);
        dispatched.Statuses.ShouldBe([EnrichmentStatus.Failed, EnrichmentStatus.Pending]);
    }

    [Fact]
    public async Task BrowseMovies_EmptyBounds_StayDistinctFromAbsentBounds()
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}?runtimeMin=&runtimeMax=&runtimeIncludeUnknown=false&yearMin=&yearMax=&{RequiredQuery}",
            cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dispatched = catalog.Queries.ShouldHaveSingleItem();
        dispatched.Runtime.ShouldBe(new RuntimeRange(null, null, false));
        dispatched.Year.ShouldBe(new YearRange(null, null));
    }

    [Fact]
    public async Task BrowseMovies_AbsentBounds_StayNull()
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{BrowseRoute}?{RequiredQuery}", cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dispatched = catalog.Queries.ShouldHaveSingleItem();
        dispatched.Runtime.ShouldBeNull();
        dispatched.Year.ShouldBeNull();
        dispatched.InWatchlist.ShouldBeNull();
    }

    [Theory]
    [InlineData("sort=Title&sort=Year&direction=Ascending&page=1&pageSize=20", "sort")]
    [InlineData("sort=title&direction=Ascending&page=1&pageSize=20", "sort")]
    [InlineData("sort=Title&direction=ascending&page=1&pageSize=20", "direction")]
    [InlineData("statuses=pending&sort=Title&direction=Ascending&page=1&pageSize=20", "statuses")]
    [InlineData("page=nope&sort=Title&direction=Ascending&pageSize=20", "page")]
    [InlineData("pageSize=1.5&sort=Title&direction=Ascending&page=1", "pageSize")]
    [InlineData("genreIds=x&sort=Title&direction=Ascending&page=1&pageSize=20", "genreIds")]
    [InlineData("inWatchlist=yes&sort=Title&direction=Ascending&page=1&pageSize=20", "inWatchlist")]
    [InlineData("runtimeIncludeUnknown=&sort=Title&direction=Ascending&page=1&pageSize=20", "runtimeIncludeUnknown")]
    public async Task BrowseMovies_MalformedValues_FailFieldKeyedWithoutReachingTheCatalog(
        string rawQuery,
        string field)
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{BrowseRoute}?{rawQuery}", cancellationToken);
        var problem = await ReadProblemAsync(response, cancellationToken);

        // assert
        ShouldFailField(problem, field);
        catalog.Queries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("runtimeMin=1&sort=Title&direction=Ascending&page=1&pageSize=20", "runtimeMax")]
    [InlineData("runtimeMin=1&sort=Title&direction=Ascending&page=1&pageSize=20", "runtimeIncludeUnknown")]
    [InlineData("runtimeMax=2&sort=Title&direction=Ascending&page=1&pageSize=20", "runtimeMin")]
    [InlineData("yearMin=1990&sort=Title&direction=Ascending&page=1&pageSize=20", "yearMax")]
    public async Task BrowseMovies_PartialRange_FailsFieldKeyedOnTheMissingMembers(
        string rawQuery,
        string field)
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{BrowseRoute}?{rawQuery}", cancellationToken);
        var problem = await ReadProblemAsync(response, cancellationToken);

        // assert
        ShouldFailField(problem, field);
        catalog.Queries.ShouldBeEmpty();
    }

    [Fact]
    public async Task BrowseMovies_MissingRequiredKeys_ReportsFourFieldFailuresWithoutReachingTheCatalog()
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(BrowseRoute, cancellationToken);
        var problem = await ReadProblemAsync(response, cancellationToken);

        // assert
        Fields(problem).ShouldBe(
            ["Query.Direction", "Query.Page.Number", "Query.Page.Size", "Query.Sort"],
            ignoreOrder: true);
        catalog.Queries.ShouldBeEmpty();
    }

    [Fact]
    public async Task BrowseMovies_PageSizeAboveTheLimit_IsRejectedByTheDecoratedValidator()
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}?sort=Title&direction=Ascending&page=1&pageSize=101",
            cancellationToken);
        var problem = await ReadProblemAsync(response, cancellationToken);

        // assert
        ShouldFailField(problem, "Query.Page.Size");
        catalog.Queries.ShouldBeEmpty();
    }

    [Fact]
    public async Task BrowseMovies_InvertedBounds_ReachTheDecoratedValidator()
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}?yearMin=2000&yearMax=1990&{RequiredQuery}",
            cancellationToken);
        var problem = await ReadProblemAsync(response, cancellationToken);

        // assert
        ShouldFailField(problem, "Query.Year");
        catalog.Queries.ShouldBeEmpty();
    }

    [Fact]
    public async Task BrowseMovies_ValidationFailure_EmitsProblemDetailsCarryingTheFieldErrors()
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(BrowseRoute, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.ShouldNotBeNull();
        problem.Status.ShouldBe((int)HttpStatusCode.UnprocessableEntity);
        problem.Title.ShouldNotBeNullOrWhiteSpace();
        problem.Extensions.ShouldContainKey("errors");
        ErrorFields(problem.Extensions["errors"]).ShouldBe(
            ["Query.Direction", "Query.Page.Number", "Query.Page.Size", "Query.Sort"],
            ignoreOrder: true);
        catalog.Queries.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMovieDetails_KnownId_DispatchesTheParsedMovieIdAndReturnsTheDetails()
    {
        // arrange
        catalog.Details = KnownDetails();
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(DetailsUrl("7"), cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        body.GetProperty("title").GetString().ShouldBe("Heat");
        catalog.DetailsIds.ShouldHaveSingleItem().ShouldBe(new MovieId(7));
    }

    [Fact]
    public async Task GetMovieDetails_UnknownPositiveId_BubblesTheHandlerNotFound()
    {
        // arrange
        catalog.Details = null;
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(DetailsUrl("999"), cancellationToken);

        // assert
        await ReadNotFoundAsync(response, cancellationToken);
        catalog.DetailsIds.ShouldHaveSingleItem().ShouldBe(new MovieId(999));
    }

    [Fact]
    public async Task GetMovieDetails_NonPositiveId_ReturnsNotFoundWithoutDispatching()
    {
        // arrange
        catalog.Details = KnownDetails();
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(DetailsUrl("0"), cancellationToken);

        // assert
        await ReadNotFoundAsync(response, cancellationToken);
        catalog.DetailsIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMovieDetails_NonIntegerId_MissesTheRouteAndReturnsAProblemBody()
    {
        // arrange
        catalog.Details = KnownDetails();
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(DetailsUrl("abc"), cancellationToken);

        // assert
        await ReadNotFoundAsync(response, cancellationToken);
        catalog.DetailsIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task UnknownRoute_ReceivesAProblemBodyFromStatusCodePages()
    {
        // arrange
        using var catalogFactory = WithCatalog();
        using var client = catalogFactory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync("/api/no-such-route", cancellationToken);

        // assert
        await ReadNotFoundAsync(response, cancellationToken);
    }

    [Fact]
    public void LibraryEndpoints_StartedComposition_DeclareNamesSummariesAndMatchingProblemMetadata()
    {
        // arrange
        var dataSource = factory.Services.GetRequiredService<EndpointDataSource>();

        // act
        var endpoints = dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => (Endpoint: endpoint, Name: endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName))
            .Where(pair => pair.Name is not null)
            .ToDictionary(pair => pair.Name.ShouldNotBeNull(), pair => pair.Endpoint, StringComparer.Ordinal);

        // assert
        endpoints.Keys.ShouldBe(["BrowseMovies", "GetMovieDetails"], ignoreOrder: true);
        ShouldDescribe(endpoints["BrowseMovies"], typeof(PagedResult<MovieSummary>), StatusCodes.Status422UnprocessableEntity);
        ShouldDescribe(endpoints["GetMovieDetails"], typeof(MovieDetails), StatusCodes.Status404NotFound);
    }

    private static void ShouldDescribe(RouteEndpoint endpoint, Type successType, int problemStatus)
    {
        // arrange
        var responses = endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().ToList();

        // act
        var summary = endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>();

        // assert
        summary.ShouldNotBeNull();
        summary.Summary.ShouldNotBeNullOrWhiteSpace();
        responses.ShouldContain(item => item.StatusCode == StatusCodes.Status200OK && item.Type == successType);
        responses.ShouldContain(item => item.StatusCode == problemStatus && item.Type == typeof(ProblemDetails));
    }

    private static string DetailsUrl(string id) => $"{BrowseRoute}/{id}";

    private WebApplicationFactory<Program> WithCatalog() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton<IMovieCatalog>(catalog)));

    private static async Task<JsonElement> ReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        TraceId(problem).ShouldNotBeNullOrWhiteSpace();
        return problem;
    }

    private static async Task ReadNotFoundAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        TraceId(problem).ShouldNotBeNullOrWhiteSpace();
    }

    private static string? TraceId(JsonElement problem) =>
        problem.TryGetProperty("traceId", out var traceId) ? traceId.GetString() : null;

    private static void ShouldFailField(JsonElement problem, string field) =>
        Fields(problem).ShouldContain(field);

    private static IEnumerable<string> Fields(JsonElement problem) =>
        problem.GetProperty("errors")
            .EnumerateObject()
            .Select(property => property.Name);

    private static IEnumerable<string> ErrorFields(object? errors) =>
        ((JsonElement)errors.ShouldNotBeNull()).EnumerateObject()
            .Select(property => property.Name);

    private static MovieDetails KnownDetails() =>
        new(
            new MovieId(7),
            "Heat",
            new LibraryPath("C:/library/Heat/Heat.mkv"),
            new MediaFormat(".MKV"),
            null);

    private static MovieQuery Accepted() => new()
    {
        Sort = MovieSort.Title,
        Direction = LamuFlix.Core.Library.SortDirection.Ascending,
        Page = new Page(1, 20),
    };

    private sealed class RecordingMovieCatalog : IMovieCatalog
    {
        public List<MovieQuery> Queries { get; } = [];

        public List<MovieId> DetailsIds { get; } = [];

        public MovieDetails? Details { get; set; }

        public Task<PagedResult<MovieSummary>> BrowseAsync(MovieQuery query, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            ct.ThrowIfCancellationRequested();
            Queries.Add(query);
            return Task.FromResult(
                new PagedResult<MovieSummary>([new MovieSummary(new MovieId(1), "Heat")], 1));
        }

        public Task<MovieDetails?> GetDetailsAsync(MovieId id, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(id);
            ct.ThrowIfCancellationRequested();
            DetailsIds.Add(id);
            return Task.FromResult(Details);
        }
    }
}
