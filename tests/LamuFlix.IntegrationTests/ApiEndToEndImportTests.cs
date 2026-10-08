using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(ApiEndToEndCollection))]
public sealed class ApiEndToEndImportTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture)
{
    private const string ImportRoute = "/api/movies/import";
    private const string DetailsRoutePrefix = "/api/movies/";
    private const string ApplicationJson = "application/json";
    private const string FolderName = "Blade Runner (1982)";
    private const string FolderTitle = "Blade Runner";
    private const string VideoFileName = "blade runner.mkv";
    private const string ExpectedFormat = "mkv";
    private const string ExpectedSynopsis = "A blade runner must pursue and terminate four replicants.";
    private const string ExpectedImdbId = "tt0083658";
    private const int ExpectedRuntime = 117;
    private const int ExpectedYear = 1982;
    private const decimal ExpectedImdbRating = 8.1m;

    private static readonly byte[] VideoBytes = [0x1A, 0x45, 0xDF, 0xA3];

    private static readonly string OmdbBody = $$"""
        {
          "Response": "True",
          "Title": "{{FolderTitle}}",
          "Year": "{{ExpectedYear}}",
          "Runtime": "{{ExpectedRuntime}} min",
          "Plot": "{{ExpectedSynopsis}}",
          "imdbRating": "{{ExpectedImdbRating.ToString(CultureInfo.InvariantCulture)}}",
          "imdbID": "{{ExpectedImdbId}}"
        }
        """;

    private string? ownedTempRoot;

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        DeleteOwnedTempRoot();
    }

    [Fact]
    public async Task ImportMovieFolder_RealScannerThroughOmdb_PersistsEnrichedMovieWithMeasuredProviderEvidence()
    {
        // arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var folderPath = CreateOwnedImportFolder();
        var client = await StartHostAsync(InstallOmdbStub);

        // act
        var response = await client.PostAsJsonAsync(ImportRoute, new { folderPath }, cancellationToken);
        var importBody = await response.Content.ReadAsStringAsync(cancellationToken);

        // assert - 202 accepted with relative Location carrying the generated id and an empty body
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        importBody.ShouldBe(string.Empty);
        var location = response.Headers.Location.ShouldNotBeNull();
        location.IsAbsoluteUri.ShouldBeFalse();
        var id = ParseImportedId(location.OriginalString);

        // assert - the imported movie is persisted under the generated id
        var persisted = await ReadPersistedMovieAsync(id, cancellationToken);
        persisted.ShouldNotBeNull();
        persisted.Title.ShouldBe(FolderTitle);
        persisted.Path.ShouldBe(folderPath);
        persisted.Format.ShouldBe(ExpectedFormat);

        // act - the live consumer enriches the imported row through the real OMDb path
        await WaitForStatusAsync(new MovieId(id), EnrichmentStatus.Enriched);
        var details = await client.GetAsync($"{DetailsRoutePrefix}{id}", cancellationToken);
        var detailsBody = await details.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);

        // assert - GET details confirms the enrichment written back over HTTP
        details.StatusCode.ShouldBe(HttpStatusCode.OK);
        details.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ApplicationJson);
        ShouldCarryExactly(detailsBody, "id", "title", "path", "format", "metadata");
        detailsBody.GetProperty("id").GetInt32().ShouldBe(id);
        detailsBody.GetProperty("title").GetString().ShouldBe(FolderTitle);
        detailsBody.GetProperty("path").GetString().ShouldBe(folderPath);
        detailsBody.GetProperty("format").GetString().ShouldBe(ExpectedFormat);
        var metadata = detailsBody.GetProperty("metadata");
        ShouldCarryExactly(metadata, "title", "synopsis", "releaseYear", "runtime", "imdbRating", "imdbId");
        metadata.GetProperty("title").GetString().ShouldBe(FolderTitle);
        metadata.GetProperty("synopsis").GetString().ShouldBe(ExpectedSynopsis);
        metadata.GetProperty("releaseYear").GetInt32().ShouldBe(ExpectedYear);
        metadata.GetProperty("runtime").GetInt32().ShouldBe(ExpectedRuntime);
        metadata.GetProperty("imdbRating").GetDecimal().ShouldBe(ExpectedImdbRating);
        metadata.GetProperty("imdbId").GetString().ShouldBe(ExpectedImdbId);

        // assert - the measured WireMock log proves the exact OMDb lookup that produced the enrichment
        var measured = Fixture.Server.LogEntries.ShouldHaveSingleItem().RequestMessage.ShouldNotBeNull();
        measured.Method.ShouldBe("GET");
        measured.AbsolutePath.ShouldBe("/");
        var query = measured.Query.ShouldNotBeNull();
        query.Keys.ShouldBe(["apikey", "t", "type"], ignoreOrder: true);
        query["apikey"].ShouldContain(MetadataProviderProbe.SentinelApiKey);
        query["t"].ShouldContain(FolderTitle);
        query["type"].ShouldContain("movie");
    }

    private static void InstallOmdbStub(WireMockServer server) =>
        server
            .Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(OmdbBody));

    private static int ParseImportedId(string relativeLocation)
    {
        relativeLocation.ShouldStartWith(DetailsRoutePrefix);
        var idText = relativeLocation[DetailsRoutePrefix.Length..];
        int.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            .ShouldBe(true, $"import Location '{relativeLocation}' does not end in a generated movie id");
        return id;
    }

    private static void ShouldCarryExactly(JsonElement element, params string[] names) =>
        element.EnumerateObject().Select(property => property.Name).ShouldBe(names, ignoreOrder: true);

    private string CreateOwnedImportFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"lamuflix-dev313-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        ownedTempRoot = root;
        var folder = Path.Combine(root, FolderName);
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, VideoFileName), VideoBytes);
        return folder;
    }

    private void DeleteOwnedTempRoot()
    {
        if (ownedTempRoot is not { } root)
        {
            return;
        }

        ownedTempRoot = null;
        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
    }

    private async Task<PersistedImport?> ReadPersistedMovieAsync(int id, CancellationToken cancellationToken)
    {
        var movieId = new MovieId(id);
        await using var context = Fixture.Postgres.CreateMigratedContext();
        var record = await context.Movies
            .AsNoTracking()
            .Where(movie => movie.Id == movieId)
            .Select(movie => new
            {
                movie.Title,
                Path = movie.LibraryPath.Value,
                Format = movie.Format.Extension
            })
            .SingleOrDefaultAsync(cancellationToken);
        return record is null
            ? null
            : new PersistedImport(record.Title, record.Path, record.Format);
    }

    private sealed record PersistedImport(string Title, string Path, string Format);
}
