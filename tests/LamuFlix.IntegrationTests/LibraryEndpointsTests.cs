using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
using Microsoft.AspNetCore.Mvc.Testing;
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
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/json");
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
        var property = Prop.ForAll(MovieQueryFixture.Queries(), (MovieQuery query) =>
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

    private WebApplicationFactory<Program> WithCatalog() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton<IMovieCatalog>(catalog)));

    private static async Task<JsonElement> ReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType!.MediaType.ShouldBe(ProblemDetailsJson);
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
    }

    private static void ShouldFailField(JsonElement problem, string field) =>
        Fields(problem).ShouldContain(field);

    private static IEnumerable<string> Fields(JsonElement problem) =>
        problem.GetProperty("errors")
            .EnumerateObject()
            .Select(property => property.Name);

    private static MovieQuery Accepted() => new()
    {
        Sort = MovieSort.Title,
        Direction = LamuFlix.Core.Library.SortDirection.Ascending,
        Page = new Page(1, 20),
    };

    private sealed class RecordingMovieCatalog : IMovieCatalog
    {
        public List<MovieQuery> Queries { get; } = [];

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
            return Task.FromResult<MovieDetails?>(null);
        }
    }
}
