using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(ApiEndToEndCollection))]
public sealed class ApiEndToEndValidationTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture)
{
    private const string ImportRoute = "/api/movies/import";
    private const string BrowseRoute = "/api/movies";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string RequiredOrdering = "sort=Title&direction=Ascending&page=1&pageSize=20";

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("C:/library/../secret")]
    public async Task ImportMovie_InvalidFolderPath_ReturnsUnprocessableEntityWithFieldKeyedErrors(string folderPath)
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsJsonAsync(ImportRoute, new { folderPath }, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        ShouldBeFieldKeyedProblem(response, problem, "folderPath");
    }

    [Fact]
    public async Task BrowseMovies_MalformedPageValue_ReturnsUnprocessableEntityWithFieldKeyedErrors()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}?page=abc&sort=Title&direction=Ascending&pageSize=20",
            cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        ShouldBeFieldKeyedProblem(response, problem, "page");
    }

    [Fact]
    public async Task BrowseMovies_MissingRequiredOrderingAndPaging_ReturnsUnprocessableEntityWithFieldKeyedErrors()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(BrowseRoute, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        FieldNames(problem).ShouldBe(
            ["Query.Direction", "Query.Page.Number", "Query.Page.Size", "Query.Sort"],
            ignoreOrder: true);
    }

    [Fact]
    public async Task BrowseMovies_InvertedYearBounds_ReturnsUnprocessableEntityWithFieldKeyedErrors()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.GetAsync(
            $"{BrowseRoute}?yearMin=2000&yearMax=1990&{RequiredOrdering}",
            cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        ShouldBeFieldKeyedProblem(response, problem, "Query.Year");
    }

    private static string[] FieldNames(JsonElement problem) =>
        [.. problem.GetProperty("errors").EnumerateObject().Select(property => property.Name)];

    private static void ShouldBeFieldKeyedProblem(
        HttpResponseMessage response,
        JsonElement problem,
        string expectedField)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.UnprocessableEntity);
        problem.GetProperty("title").GetString().ShouldBe("Unprocessable Entity");
        var errors = problem.GetProperty("errors");
        errors.ValueKind.ShouldBe(JsonValueKind.Object);
        errors.TryGetProperty(expectedField, out var messages).ShouldBe(true);
        messages.GetArrayLength().ShouldBeGreaterThan(0);
        messages.EnumerateArray().First().GetString().ShouldNotBeNullOrWhiteSpace();
    }
}
