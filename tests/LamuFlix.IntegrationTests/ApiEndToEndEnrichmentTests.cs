using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Pipeline;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(ApiEndToEndCollection))]
public sealed class ApiEndToEndEnrichmentTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture)
{
    private const string DetailsRoutePrefix = "/api/movies/";
    private const string RetryRouteSuffix = "/enrichment";
    private const string ApplicationJson = "application/json";
    private const string ProblemDetailsJson = "application/problem+json";
    private const int UnknownId = 999999;
    private const string SeedTitle = "Run Silent Run Deep";
    private const string SeedPathSuffix = "enrichment-retry";
    private const string ExpectedSynopsis = "A hellboat captain pursues the submarine that sank his crew.";
    private const string ExpectedImdbId = "tt0052142";
    private const int ExpectedRuntime = 93;
    private const int ExpectedYear = 1958;
    private const decimal ExpectedImdbRating = 7.8m;

    private static readonly string OmdbBody = $$"""
        {
          "Response": "True",
          "Title": "{{SeedTitle}}",
          "Year": "{{ExpectedYear}}",
          "Runtime": "{{ExpectedRuntime}} min",
          "Plot": "{{ExpectedSynopsis}}",
          "imdbRating": "{{ExpectedImdbRating.ToString(CultureInfo.InvariantCulture)}}",
          "imdbID": "{{ExpectedImdbId}}"
        }
        """;

    [Fact]
    public async Task RequestEnrichment_RetryEligibleFailedMovie_ReachesEnrichedWithMeasuredOmdbEvidence()
    {
        // arrange
        var client = await StartHostAsync(InstallOmdbStub);
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = MovieCatalogSeed.Create(SeedTitle, SeedPathSuffix);
        seed.Status = EnrichmentStatus.Failed;
        seed.EnrichmentAttempts = 1;
        seed.EnrichmentFailureCategory = EnrichmentFailureCategory.ProviderUnavailable;
        seed.LastAttemptAt = null;
        var movie = await SeedAsync(seed);
        var id = movie.Id.ShouldNotBeNull().Value;

        // act
        var response = await client.PostAsync(RetryRoute(id), null, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        responseBody.ShouldBe(string.Empty);
        var location = response.Headers.Location.ShouldNotBeNull();
        location.IsAbsoluteUri.ShouldBeFalse();
        location.ToString().ShouldBe(DetailsRoute(id));

        // act
        await WaitForStatusAsync(new MovieId(id), EnrichmentStatus.Enriched);
        var details = await client.GetAsync(DetailsRoute(id), cancellationToken);
        var detailsBody = await details.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert
        details.StatusCode.ShouldBe(HttpStatusCode.OK);
        details.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        ShouldCarryExactly(detailsBody, "id", "title", "path", "format", "metadata");
        detailsBody.GetProperty("id").GetInt32().ShouldBe(id);
        detailsBody.GetProperty("title").GetString().ShouldBe(SeedTitle);
        detailsBody.GetProperty("path").GetString().ShouldBe($"C:/library/{SeedPathSuffix}.mkv");
        detailsBody.GetProperty("format").GetString().ShouldBe("mkv");
        var metadata = detailsBody.GetProperty("metadata");
        ShouldCarryExactly(metadata, "title", "synopsis", "releaseYear", "runtime", "imdbRating", "imdbId");
        metadata.GetProperty("title").GetString().ShouldBe(SeedTitle);
        metadata.GetProperty("synopsis").GetString().ShouldBe(ExpectedSynopsis);
        metadata.GetProperty("releaseYear").GetInt32().ShouldBe(ExpectedYear);
        metadata.GetProperty("runtime").GetInt32().ShouldBe(ExpectedRuntime);
        metadata.GetProperty("imdbRating").GetDecimal().ShouldBe(ExpectedImdbRating);
        metadata.GetProperty("imdbId").GetString().ShouldBe(ExpectedImdbId);

        // assert
        var measured = Fixture.Server.LogEntries.ShouldHaveSingleItem().RequestMessage.ShouldNotBeNull();
        measured.Method.ShouldBe("GET");
        measured.AbsolutePath.ShouldBe("/");
        var query = measured.Query.ShouldNotBeNull();
        query.Keys.ShouldBe(["apikey", "t", "type"], ignoreOrder: true);
        query["apikey"].ShouldContain(MetadataProviderProbe.SentinelApiKey);
        query["t"].ShouldContain(SeedTitle);
        query["type"].ShouldContain("movie");
    }

    [Fact]
    public async Task RequestEnrichment_PendingMovie_ReturnsConflictProblemAndKeepsPersistedStatus()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = await SeedAsync(MovieCatalogSeed.Create("Pending Film", "enrichment-conflict"));
        var id = movie.Id.ShouldNotBeNull().Value;

        // act
        var response = await client.PostAsync(RetryRoute(id), null, cancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        var persistedStatus = await ReadPersistedStatusAsync(id, cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.Conflict);
        problem.GetProperty("title").GetString().ShouldBe("Conflict");
        problem.GetRawText().ShouldNotContain(nameof(InvalidTransitionException));
        persistedStatus.ShouldBe(EnrichmentStatus.Pending);
        Fixture.Server.LogEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task RequestEnrichment_UnknownId_ReturnsNotFoundProblem()
    {
        // arrange
        var client = await StartHostAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        // act
        var response = await client.PostAsync(RetryRoute(UnknownId), null, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        payload.ShouldNotContain(nameof(NotFoundException));
        Fixture.Server.LogEntries.ShouldBeEmpty();
    }

    private static string DetailsRoute(int id) => $"{DetailsRoutePrefix}{id}";

    private static string RetryRoute(int id) => $"{DetailsRoutePrefix}{id}{RetryRouteSuffix}";

    private static void InstallOmdbStub(WireMockServer server) =>
        server
            .Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(OmdbBody));

    private static void ShouldCarryExactly(JsonElement element, params string[] names) =>
        element.EnumerateObject().Select(property => property.Name).ShouldBe(names, ignoreOrder: true);

    private async Task<EnrichmentStatus?> ReadPersistedStatusAsync(int id, CancellationToken cancellationToken)
    {
        var movieId = new MovieId(id);
        await using var context = Fixture.Postgres.CreateMigratedContext();
        return await context.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == movieId)
            .Select(movie => (EnrichmentStatus?)movie.Status)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
