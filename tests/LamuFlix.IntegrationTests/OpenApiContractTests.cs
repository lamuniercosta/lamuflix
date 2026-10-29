using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using VerifyXunit;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class OpenApiContractTests(ApiHostFactory factory) : IClassFixture<ApiHostFactory>
{
    private const string DocumentRoute = "/openapi/v1.json";
    private const string ScalarRoute = "/scalar";
    private const string ScalarTitle = "Scalar API Reference";
    private const string HtmlMediaType = "text/html";
    private const string OpenApiVersionPrefix = "3.1";
    private const string HealthPathPrefix = "/health";
    private const string RootMarkerFileName = "Directory.Packages.props";
    private const int RootWalkLimit = 8;
    private const string CommittedBaselineRelativePath = "web/src/api/openapi.json";
    private const string DerivedSnapshotRelativePath =
        "tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json";
    private const string SnapshotDirectory = "Snapshots";
    private const string SnapshotFileName = "OpenApiContractTests.DriftMatchesCommittedBaseline";
    private const string RegenerationSwitchName = "LAMUFLIX_REGENERATE_OPENAPI";
    private const string RegenerationSwitchValue = "true";
    private const string RegenerationSkipReason =
        "Regeneration writes tracked baseline files; set LAMUFLIX_REGENERATE_OPENAPI=true locally to enable it. CI never sets it.";

    private static readonly string[] ExpectedOperations =
    [
        "GET /api/movies",
        "GET /api/movies/{id}",
        "GET /api/movies/{id}/recommendations",
        "GET /api/recommendations/for-you",
        "DELETE /api/movies/{id}",
        "POST /api/movies/import",
        "POST /api/movies/{id}/enrichment",
        "PUT /api/movies/{id}/imdb",
        "POST /api/movies/{id}/watchlist",
        "DELETE /api/movies/{id}/watchlist",
        "POST /api/movies/{id}/play",
        "GET /api/genres",
        "GET /api/collections",
        "GET /api/people",
        "GET /api/directors",
        "POST /api/movies/{id}/watched",
        "DELETE /api/movies/{id}/watched",
        "POST /api/movies/{id}/not-interested",
        "DELETE /api/movies/{id}/not-interested",
    ];

    private static readonly string[] ExpectedSchemas =
    [
        "BrowseMoviesResponse",
        "MovieDetailsResponse",
        "RecommendedMovieDto",
        "RecommendedMovieReasonDto",
        "GenreDto",
        "CollectionDto",
        "PersonDto",
        "DirectorDto",
    ];

    private static readonly string[] OperationMethods =
        ["get", "post", "put", "patch", "delete", "head", "options", "trace"];

    public static TheoryData<string> DocumentationRoutes => new() { DocumentRoute, ScalarRoute };

    [Fact]
    public async Task Document_DevelopmentHost_ServesOpenApi31WithTheExpectedBusinessOperations()
    {
        // arrange
        await using var development = CreateDevelopmentHost();
        using var client = development.CreateClient();

        // act
        var document = await GenerateDocumentAsync(client);
        using var parsed = JsonDocument.Parse(document);

        // assert
        var version = parsed.RootElement.GetProperty("openapi").GetString();
        version.ShouldNotBeNull();
        version.ShouldStartWith(OpenApiVersionPrefix);
        Operations(parsed.RootElement).ShouldBe(ExpectedOperations, ignoreOrder: true);
        OperationsWithResponses(parsed.RootElement).ShouldBe(ExpectedOperations, ignoreOrder: true);
        ResponseStatuses(parsed.RootElement).Where(status => !IsStatusCode(status)).ShouldBeEmpty();
        ExpectedSchemas.Except(Schemas(parsed.RootElement)).ShouldBeEmpty();
        PathNames(parsed.RootElement).ShouldNotContain(path => path.StartsWith(HealthPathPrefix, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Document_EnrichmentStatusSchema_IsStringEnumOfTheFourWireNames()
    {
        // arrange
        await using var development = CreateDevelopmentHost();
        using var client = development.CreateClient();

        // act
        var document = await GenerateDocumentAsync(client);
        using var parsed = JsonDocument.Parse(document);
        var schema = parsed.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("EnrichmentStatus");

        // assert
        schema.GetProperty("type").GetString().ShouldBe("string");
        schema.GetProperty("enum").EnumerateArray().Select(item => item.GetString()).ShouldBe(
            ["Pending", "Enriched", "NotFound", "Failed"]);
    }

    [Fact]
    public async Task Document_EnrichmentStatusChangedDto_IsAComponentSchemaWithoutARestOperation()
    {
        // arrange
        await using var development = CreateDevelopmentHost();
        using var client = development.CreateClient();

        // act
        var document = await GenerateDocumentAsync(client);
        using var parsed = JsonDocument.Parse(document);
        var schemas = parsed.RootElement.GetProperty("components").GetProperty("schemas");

        // assert
        Operations(parsed.RootElement).ShouldBe(ExpectedOperations, ignoreOrder: true);
        PathNames(parsed.RootElement).ShouldNotContain(
            path => path.Contains("/hubs", StringComparison.Ordinal));
        schemas.TryGetProperty("EnrichmentStatusChangedDto", out var dto).ShouldBeTrue(
            "components.schemas must expose the hub payload EnrichmentStatusChangedDto.");
        SchemaTypes(dto).ShouldContain("object");
        dto.GetProperty("required").EnumerateArray().Select(item => item.GetString())
            .ShouldBe(["id", "enrichmentStatus"], ignoreOrder: true);
        var properties = dto.GetProperty("properties");
        properties.EnumerateObject().Select(property => property.Name)
            .ShouldBe(["id", "enrichmentStatus"], ignoreOrder: true);
        SchemaTypes(properties.GetProperty("id")).ShouldContain("integer");

        var statusSchema = ResolveSchema(parsed.RootElement, properties.GetProperty("enrichmentStatus"));
        SchemaTypes(statusSchema).ShouldContain("string");
        statusSchema.GetProperty("enum").EnumerateArray().Select(item => item.GetString()).ShouldBe(
            ["Pending", "Enriched", "NotFound", "Failed"]);
    }

    [Fact]
    public async Task Document_MovieResponseSchemas_RequireTheIsInWatchlistBoolean()
    {
        // arrange
        await using var development = CreateDevelopmentHost();
        using var client = development.CreateClient();

        // act
        var document = await GenerateDocumentAsync(client);
        using var parsed = JsonDocument.Parse(document);
        var schemas = parsed.RootElement.GetProperty("components").GetProperty("schemas");

        // assert
        foreach (var name in new[] { "MovieSummaryResponse", "MovieDetailsResponse" })
        {
            var schema = schemas.GetProperty(name);
            schema.GetProperty("required").EnumerateArray().Select(item => item.GetString())
                .ShouldContain("isInWatchlist");
            schema.GetProperty("properties").GetProperty("isInWatchlist").GetProperty("type").GetString()
                .ShouldBe("boolean");
        }
    }

    [Fact]
    public async Task Document_BrowseMovies_RequiresCanonicalQueryKeysWithWireEnumsAndBounds()
    {
        // arrange
        await using var development = CreateDevelopmentHost();
        using var client = development.CreateClient();

        // act
        var document = await GenerateDocumentAsync(client);
        using var parsed = JsonDocument.Parse(document);
        var operation = parsed.RootElement.GetProperty("paths").GetProperty("/api/movies").GetProperty("get");
        var parameters = operation.GetProperty("parameters").EnumerateArray()
            .ToDictionary(
                parameter => parameter.GetProperty("name").GetString()
                    ?? throw new InvalidOperationException("OpenAPI parameter is missing a name."),
                StringComparer.Ordinal);

        // assert
        Operations(parsed.RootElement).ShouldBe(ExpectedOperations, ignoreOrder: true);
        parameters.Keys.ShouldBe(
            [
                "Text",
                "GenreIds",
                "ActorIds",
                "DirectorId",
                "RuntimeMin",
                "RuntimeMax",
                "RuntimeIncludeUnknown",
                "YearMin",
                "YearMax",
                "MinRating",
                "MaxRating",
                "Statuses",
                "InWatchlist",
                "CollectionId",
                "sort",
                "direction",
                "page",
                "pageSize",
            ],
            ignoreOrder: true);
        RequiredQuery(parameters["sort"]).ShouldBeTrue();
        RequiredQuery(parameters["direction"]).ShouldBeTrue();
        RequiredQuery(parameters["page"]).ShouldBeTrue();
        RequiredQuery(parameters["pageSize"]).ShouldBeTrue();
        parameters["sort"].GetProperty("schema").GetProperty("type").GetString().ShouldBe("string");
        parameters["sort"].GetProperty("schema").GetProperty("enum").EnumerateArray().Select(item => item.GetString())
            .ShouldBe(["Title", "Year", "Rating", "Runtime"]);
        parameters["direction"].GetProperty("schema").GetProperty("type").GetString().ShouldBe("string");
        parameters["direction"].GetProperty("schema").GetProperty("enum").EnumerateArray().Select(item => item.GetString())
            .ShouldBe(["Ascending", "Descending"]);
        parameters["page"].GetProperty("schema").GetProperty("type").GetString().ShouldBe("integer");
        parameters["page"].GetProperty("schema").GetProperty("minimum").GetInt32().ShouldBe(1);
        parameters["pageSize"].GetProperty("schema").GetProperty("type").GetString().ShouldBe("integer");
        parameters["pageSize"].GetProperty("schema").GetProperty("minimum").GetInt32().ShouldBe(1);
        parameters["pageSize"].GetProperty("schema").GetProperty("maximum").GetInt32().ShouldBe(100);
    }

    [Fact]
    public async Task Document_GetRecommendations_DocumentsOptionalTakeBoundsDtoArrayAndProblemResponses()
    {
        // arrange
        await using var development = CreateDevelopmentHost();
        using var client = development.CreateClient();

        // act
        var document = await GenerateDocumentAsync(client);
        using var parsed = JsonDocument.Parse(document);

        // assert
        Operations(parsed.RootElement).ShouldBe(ExpectedOperations, ignoreOrder: true);
        var operation = parsed.RootElement.GetProperty("paths")
            .GetProperty("/api/movies/{id}/recommendations")
            .GetProperty("get");
        operation.GetProperty("operationId").GetString().ShouldBe("GetRecommendations");
        operation.GetProperty("summary").GetString().ShouldBe("Get recommendations for a library movie");

        var parameters = operation.GetProperty("parameters").EnumerateArray()
            .ToDictionary(
                parameter => parameter.GetProperty("name").GetString()
                    ?? throw new InvalidOperationException("OpenAPI parameter is missing a name."),
                StringComparer.Ordinal);
        parameters.Keys.ShouldContain("id");
        var take = parameters["take"];
        OptionalQuery(take);
        take.GetProperty("description").GetString().ShouldBe(
            "The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.");
        var takeSchema = take.GetProperty("schema");
        takeSchema.GetProperty("type").GetString().ShouldBe("integer");
        takeSchema.GetProperty("default").GetInt32().ShouldBe(10);
        takeSchema.GetProperty("minimum").GetInt32().ShouldBe(1);
        takeSchema.TryGetProperty("maximum", out _).ShouldBeFalse();

        var responses = operation.GetProperty("responses");
        var successSchema = responses.GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");
        successSchema.GetProperty("type").GetString().ShouldBe("array");
        successSchema.GetProperty("items").GetProperty("$ref").GetString()
            .ShouldBe("#/components/schemas/RecommendedMovieDto");
        foreach (var status in new[] { "400", "404" })
        {
            var problemSchema = responses.GetProperty(status).GetProperty("content")
                .GetProperty("application/problem+json").GetProperty("schema");
            problemSchema.GetProperty("$ref").GetString().ShouldBe("#/components/schemas/ProblemDetails");
        }

        var dto = parsed.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("RecommendedMovieDto");
        var expectedPropertyNames = new[]
        {
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
        };
        dto.GetProperty("required").EnumerateArray().Select(item => item.GetString())
            .ShouldBe(expectedPropertyNames, ignoreOrder: true);
        var properties = dto.GetProperty("properties");
        properties.EnumerateObject().Select(property => property.Name)
            .ShouldBe(expectedPropertyNames, ignoreOrder: true);
        properties.GetProperty("reasons").GetProperty("type").GetString().ShouldBe("array");
        properties.GetProperty("reasons").GetProperty("items").GetProperty("$ref").GetString()
            .ShouldBe("#/components/schemas/RecommendedMovieReasonDto");
        var reasonDto = parsed.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("RecommendedMovieReasonDto");
        reasonDto.GetProperty("type").GetString().ShouldBe("object");
        reasonDto.GetProperty("required").EnumerateArray().Select(item => item.GetString())
            .ShouldBe(["kind", "text"], ignoreOrder: true);
        reasonDto.GetProperty("properties").EnumerateObject().Select(property => property.Name)
            .ShouldBe(["kind", "text"], ignoreOrder: true);
        reasonDto.GetProperty("properties").GetProperty("kind").GetProperty("type").GetString().ShouldBe("string");
        reasonDto.GetProperty("properties").GetProperty("text").GetProperty("type").GetString().ShouldBe("string");
        properties.GetProperty("enrichmentStatus").GetProperty("$ref").GetString()
            .ShouldBe("#/components/schemas/EnrichmentStatus");
        properties.GetProperty("score").GetProperty("type").EnumerateArray().Select(item => item.GetString())
            .ShouldContain("number");
    }

    [Fact]
    public async Task Document_GetForYouRecommendations_DocumentsDtoArrayAndProblemResponses()
    {
        // arrange
        await using var development = CreateDevelopmentHost();
        using var client = development.CreateClient();

        // act
        var document = await GenerateDocumentAsync(client);
        using var parsed = JsonDocument.Parse(document);

        // assert
        Operations(parsed.RootElement).ShouldBe(ExpectedOperations, ignoreOrder: true);
        var operation = parsed.RootElement.GetProperty("paths")
            .GetProperty("/api/recommendations/for-you")
            .GetProperty("get");
        operation.GetProperty("operationId").GetString().ShouldBe("GetForYouRecommendations");
        operation.GetProperty("summary").GetString().ShouldBe(
            "Get personalized recommendations from the taste profile");

        var parameters = operation.GetProperty("parameters").EnumerateArray()
            .ToDictionary(
                parameter => parameter.GetProperty("name").GetString()
                    ?? throw new InvalidOperationException("OpenAPI parameter is missing a name."),
                StringComparer.Ordinal);
        var take = parameters["take"];
        OptionalQuery(take);
        take.GetProperty("description").GetString().ShouldBe(
            "The inclusive upper bound is the configured RecommendationOptions.CandidatePoolSize.");
        var takeSchema = take.GetProperty("schema");
        takeSchema.GetProperty("type").GetString().ShouldBe("integer");
        takeSchema.GetProperty("default").GetInt32().ShouldBe(10);
        takeSchema.GetProperty("minimum").GetInt32().ShouldBe(1);
        takeSchema.TryGetProperty("maximum", out _).ShouldBeFalse();

        var responses = operation.GetProperty("responses");
        var successSchema = responses.GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("schema");
        successSchema.GetProperty("type").GetString().ShouldBe("array");
        successSchema.GetProperty("items").GetProperty("$ref").GetString()
            .ShouldBe("#/components/schemas/RecommendedMovieDto");
        foreach (var status in new[] { "400", "422" })
        {
            var problemSchema = responses.GetProperty(status).GetProperty("content")
                .GetProperty("application/problem+json").GetProperty("schema");
            problemSchema.GetProperty("$ref").GetString().ShouldBe("#/components/schemas/ProblemDetails");
        }
    }

    [Fact]
    public async Task Scalar_DevelopmentHost_ServesTheReferenceUiWiredToTheDocument()
    {
        // arrange
        await using var development = CreateDevelopmentHost();
        using var client = development.CreateClient();

        // act
        using var response = await client.GetAsync(ScalarRoute, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var contentType = response.Content.Headers.ContentType;
        contentType.ShouldNotBeNull();
        contentType.MediaType.ShouldBe(HtmlMediaType);
        body.ShouldContain(ScalarTitle);
        body.ShouldContain(DocumentRoute);
    }

    [Theory]
    [MemberData(nameof(DocumentationRoutes))]
    public async Task DocumentationRoute_ProductionHost_ReturnsNotFound(string route)
    {
        // arrange
        await using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Production));
        using var client = production.CreateClient();

        // act
        using var response = await client.GetAsync(route, TestContext.Current.CancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        production.Services.GetRequiredService<IOptions<FeatureOptions>>().Value.LocalPlay.ShouldBeFalse();
    }

    [Fact]
    public async Task Export_DevelopmentHost_GeneratesTheSharedDocumentAndResolvesTheCheckoutPaths()
    {
        // arrange
        await using var development = CreateDevelopmentHost();
        using var client = development.CreateClient();
        var root = RepositoryRoot();
        var baseline = CommittedBaselinePath;
        var snapshot = DerivedSnapshotPath;

        // act
        var document = await GenerateDocumentAsync(client);

        // assert
        client.BaseAddress.ShouldBe(new Uri("http://localhost/"));
        document.ShouldNotBeNullOrWhiteSpace();
        File.Exists(Path.Combine(root, RootMarkerFileName)).ShouldBeTrue();
        IsWithinRoot(baseline, root).ShouldBeTrue();
        IsWithinRoot(snapshot, root).ShouldBeTrue();
    }

    [Fact]
    public async Task DriftMatchesCommittedBaseline()
    {
        // arrange
        await using var development = CreateDevelopmentHost();
        using var client = development.CreateClient();

        // act
        var document = NormalizeLineEndings(await GenerateDocumentAsync(client));

        // assert
        File.Exists(CommittedBaselinePath).ShouldBeTrue($"Missing committed baseline '{CommittedBaselineRelativePath}'.");
        File.Exists(DerivedSnapshotPath).ShouldBeTrue($"Missing derived snapshot '{DerivedSnapshotRelativePath}'.");
        NormalizeLineEndings(await File.ReadAllTextAsync(CommittedBaselinePath, TestContext.Current.CancellationToken)).ShouldBe(document);
        NormalizeLineEndings(await File.ReadAllTextAsync(DerivedSnapshotPath, TestContext.Current.CancellationToken)).ShouldBe(document);

        await Verifier.Verify(target: document, extension: "json")
            .UseDirectory(SnapshotDirectory)
            .UseFileName(SnapshotFileName);
    }

    [Fact]
    public async Task Document_RepeatedGeneration_IsIdenticalAfterNormalization()
    {
        // arrange
        await using var firstHost = CreateDevelopmentHost();
        await using var secondHost = CreateDevelopmentHost();
        using var firstClient = firstHost.CreateClient();
        using var secondClient = secondHost.CreateClient();

        // act
        var firstDocument = NormalizeLineEndings(await GenerateDocumentAsync(firstClient));
        var secondDocument = NormalizeLineEndings(await GenerateDocumentAsync(secondClient));

        // assert
        firstDocument.ShouldNotBeNullOrWhiteSpace();
        secondDocument.ShouldBe(firstDocument);
    }

    [Fact]
    public async Task OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether()
    {
        // arrange
        Assert.SkipUnless(RegenerationEnabled, RegenerationSkipReason);
        await using var development = CreateDevelopmentHost();
        using var client = development.CreateClient();
        var root = RepositoryRoot();
        var baseline = CommittedBaselinePath;
        var snapshot = DerivedSnapshotPath;
        IsWithinRoot(baseline, root).ShouldBeTrue();
        IsWithinRoot(snapshot, root).ShouldBeTrue();
        var document = NormalizeLineEndings(await GenerateDocumentAsync(client));

        // act
        WriteRegeneratedOutput(baseline, document);
        WriteRegeneratedOutput(snapshot, document);

        // assert
        client.BaseAddress.ShouldBe(new Uri("http://localhost/"));
        File.Exists(baseline).ShouldBeTrue();
        File.Exists(snapshot).ShouldBeTrue();
        NormalizeLineEndings(await File.ReadAllTextAsync(baseline, TestContext.Current.CancellationToken)).ShouldBe(document);
        NormalizeLineEndings(await File.ReadAllTextAsync(snapshot, TestContext.Current.CancellationToken)).ShouldBe(document);
    }

    private static bool RegenerationEnabled =>
        string.Equals(
            Environment.GetEnvironmentVariable(RegenerationSwitchName),
            RegenerationSwitchValue,
            StringComparison.OrdinalIgnoreCase);

    private static void WriteRegeneratedOutput(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException($"Regeneration path '{path}' has no directory.");
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(path, contents);
    }

    private WebApplicationFactory<Program> CreateDevelopmentHost() =>
        factory.WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Development));

    private static async Task<string> GenerateDocumentAsync(HttpClient client)
    {
        using var response = await client.GetAsync(DocumentRoute, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string RepositoryRoot()
    {
        var matches = new List<string>();
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        for (var depth = 0; directory is not null && depth <= RootWalkLimit; depth++)
        {
            if (File.Exists(Path.Combine(directory.FullName, RootMarkerFileName)))
            {
                matches.Add(directory.FullName);
            }

            directory = directory.Parent;
        }

        if (matches.Count == 1)
        {
            return matches[0];
        }

        throw new InvalidOperationException(
            matches.Count == 0
                ? $"Checkout marker '{RootMarkerFileName}' was not found within {RootWalkLimit} ancestors of '{AppContext.BaseDirectory}'."
                : $"Checkout marker '{RootMarkerFileName}' is ambiguous under '{AppContext.BaseDirectory}': {string.Join(", ", matches)}.");
    }

    private static string CommittedBaselinePath =>
        Path.GetFullPath(Path.Combine(RepositoryRoot(), CommittedBaselineRelativePath));

    private static string DerivedSnapshotPath =>
        Path.GetFullPath(Path.Combine(RepositoryRoot(), DerivedSnapshotRelativePath));

    private static bool IsWithinRoot(string candidate, string root) =>
        candidate.Length > root.Length
        && candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal);

    private static IEnumerable<(JsonElement Operation, string Label)> OperationEntries(JsonElement document)
    {
        foreach (var path in document.GetProperty("paths").EnumerateObject())
        {
            foreach (var member in path.Value.EnumerateObject())
            {
                if (OperationMethods.Contains(member.Name, StringComparer.Ordinal))
                {
                    yield return (member.Value, $"{member.Name.ToUpperInvariant()} {path.Name}");
                }
            }
        }
    }

    private static IEnumerable<string> Operations(JsonElement document) =>
        OperationEntries(document).Select(entry => entry.Label);

    private static IEnumerable<string> OperationsWithResponses(JsonElement document) =>
        OperationEntries(document)
            .Where(entry => entry.Operation.TryGetProperty("responses", out var responses) && responses.EnumerateObject().Any())
            .Select(entry => entry.Label);

    private static IEnumerable<string> ResponseStatuses(JsonElement document) =>
        OperationEntries(document)
            .SelectMany(entry => entry.Operation.GetProperty("responses").EnumerateObject().Select(response => response.Name));

    private static IReadOnlyList<string> SchemaTypes(JsonElement schema)
    {
        var type = schema.GetProperty("type");
        return type.ValueKind == JsonValueKind.Array
            ? type.EnumerateArray().Select(item => item.GetString().ShouldNotBeNull()).ToList()
            : [type.GetString().ShouldNotBeNull()];
    }

    private static JsonElement ResolveSchema(JsonElement document, JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference))
        {
            return schema;
        }

        var referenceValue = reference.GetString().ShouldNotBeNull();
        const string ComponentPrefix = "#/components/schemas/";
        referenceValue.ShouldStartWith(ComponentPrefix);
        return document.GetProperty("components").GetProperty("schemas")
            .GetProperty(referenceValue[ComponentPrefix.Length..]);
    }

    private static IEnumerable<string> Schemas(JsonElement document) =>
        document.GetProperty("components").GetProperty("schemas").EnumerateObject().Select(schema => schema.Name);

    private static IEnumerable<string> PathNames(JsonElement document) =>
        document.GetProperty("paths").EnumerateObject().Select(path => path.Name);

    private static bool IsStatusCode(string value) => value.Length == 3 && value.All(char.IsAsciiDigit);

    private static bool RequiredQuery(JsonElement parameter)
    {
        parameter.GetProperty("in").GetString().ShouldBe("query");
        return parameter.TryGetProperty("required", out var required) && required.GetBoolean();
    }

    private static void OptionalQuery(JsonElement parameter)
    {
        parameter.GetProperty("in").GetString().ShouldBe("query");
        if (parameter.TryGetProperty("required", out var required))
        {
            required.GetBoolean().ShouldBeFalse();
        }
    }
}
