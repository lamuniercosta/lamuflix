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
        "POST /api/movies/import",
        "POST /api/movies/{id}/enrichment",
        "POST /api/movies/{id}/watchlist",
        "DELETE /api/movies/{id}/watchlist",
        "POST /api/movies/{id}/play",
        "GET /api/genres",
        "GET /api/people",
    ];

    private static readonly string[] ExpectedSchemas =
    [
        "BrowseMoviesResponse",
        "MovieDetailsResponse",
        "GenreDto",
        "PersonDto",
    ];

    private static readonly string[] OperationMethods =
        ["get", "post", "put", "patch", "delete", "head", "options", "trace"];

    public static TheoryData<string> DocumentationRoutes => new() { DocumentRoute, ScalarRoute };

    [Fact]
    public async Task Document_DevelopmentHost_ServesOpenApi31WithTheNineBusinessOperations()
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

    private static IEnumerable<string> Schemas(JsonElement document) =>
        document.GetProperty("components").GetProperty("schemas").EnumerateObject().Select(schema => schema.Name);

    private static IEnumerable<string> PathNames(JsonElement document) =>
        document.GetProperty("paths").EnumerateObject().Select(path => path.Name);

    private static bool IsStatusCode(string value) => value.Length == 3 && value.All(char.IsAsciiDigit);
}
