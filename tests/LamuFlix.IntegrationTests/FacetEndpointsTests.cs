using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class FacetEndpointsTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const string GenresRoute = "/api/genres";
    private const string PeopleRoute = "/api/people";
    private const string ProblemDetailsJson = "application/problem+json";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();

    [Fact]
    public async Task GetGenres_WithStoredGenres_ReturnsScalarArrayOfIdAndName()
    {
        // arrange
        IReadOnlyList<GenreFacet> genres = [new GenreFacet(1, "Drama"), new GenreFacet(2, "Thriller")];
        catalog.GetGenresAsync(Arg.Any<CancellationToken>()).Returns(genres);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(GenresRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        var items = body.EnumerateArray().ToList();
        items.Count.ShouldBe(2);
        items[0].GetProperty("id").GetInt32().ShouldBe(1);
        items[0].GetProperty("name").GetString().ShouldBe("Drama");
        items[1].GetProperty("id").GetInt32().ShouldBe(2);
        items[1].GetProperty("name").GetString().ShouldBe("Thriller");
        PropertyNames(items[0]).ShouldBe(["id", "name"], ignoreOrder: true);
        await catalog.Received(1).GetGenresAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetGenres_WithNoStoredGenres_ReturnsEmptyArray()
    {
        // arrange
        IReadOnlyList<GenreFacet> genres = [];
        catalog.GetGenresAsync(Arg.Any<CancellationToken>()).Returns(genres);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(GenresRoute, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        body.ValueKind.ShouldBe(JsonValueKind.Array);
        body.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task GetPeople_ActorRole_ReturnsScalarArrayPreservingSharedNames()
    {
        // arrange
        IReadOnlyList<PersonFacet> people = [new PersonFacet(1, "Chase"), new PersonFacet(2, "Chase")];
        catalog.GetPeopleAsync(Arg.Any<CancellationToken>()).Returns(people);
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync($"{PeopleRoute}?role=actor", cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
        var items = body.EnumerateArray().ToList();
        items.Count.ShouldBe(2);
        items.Select(item => item.GetProperty("id").GetInt32()).ShouldBe([1, 2], ignoreOrder: true);
        items.Select(item => item.GetProperty("name").GetString()).ShouldBe(
            ["Chase", "Chase"],
            ignoreOrder: true);
        PropertyNames(items[0]).ShouldBe(["id", "name"], ignoreOrder: true);
        await catalog.Received(1).GetPeopleAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("role=")]
    [InlineData("role=actor&role=actor")]
    [InlineData("role=director")]
    public async Task GetPeople_InvalidRole_ReturnsUnprocessableEntityProblem(string query)
    {
        // arrange
        await using var host = CreateHost();
        using var client = host.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        var url = query.Length == 0 ? PeopleRoute : $"{PeopleRoute}?{query}";

        // act
        var response = await client.GetAsync(url, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        Fields(problem).ShouldContain("role");
        await catalog.DidNotReceive().GetPeopleAsync(Arg.Any<CancellationToken>());
    }

    private WebApplicationFactory<Program> CreateHost() =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddSingleton(catalog)));

    private static IEnumerable<string> PropertyNames(JsonElement item) =>
        item.EnumerateObject().Select(property => property.Name);

    private static IEnumerable<string> Fields(JsonElement problem) =>
        problem.GetProperty("errors").EnumerateObject().Select(property => property.Name);
}
