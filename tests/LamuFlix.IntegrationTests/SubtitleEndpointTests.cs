using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(SubtitleEndpointCollection))]
public sealed class SubtitleEndpointTests(ApiHostFactory factory)
{
    private const int KnownId = 7;
    private const int UnknownId = 999;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string CacheControlHeader = "Cache-Control";
    private const string NoStoreDirective = "no-store";
    private const string PrivateDirective = "private";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();

    [Fact]
    public async Task Get_EligibleSidecarMatrix_ReturnsFrozenTracksInFrozenOrder()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.en.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p-EN.SRT");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.pob.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p-POB.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.default.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p-default.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.forced.srt");
        library.WriteMovieFile(MovieFolder, "unrelated.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.srt.bak");
        library.WriteMovieFile(Path.Combine(MovieFolder, "nested"), "Heat.1995.1080p.de.srt");
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var tracks = TrackElements(body);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertFrozenNoStore(response);
        tracks.Select(track => track.GetProperty("label").GetString()).ShouldBe(
        [
            "Default",
            "Default (2)",
            "English",
            "English (2)",
            "Portuguese (Brazil)",
            "Portuguese (Brazil) (2)",
            "Heat.1995.1080p.forced",
            "Subtitles",
        ]);
        tracks.Select(track => track.GetProperty("language").GetString()).ShouldBe(
        [
            "und",
            "und",
            "en",
            "en",
            "pt-BR",
            "pt-BR",
            "und",
            "und",
        ]);
        tracks.Select(track => track.GetProperty("isDefault").GetBoolean()).ShouldBe(
        [
            true,
            true,
            false,
            false,
            false,
            false,
            false,
            false,
        ]);
        body.ShouldNotContain("nested");
        body.ShouldNotContain(PrimaryFileName);
        body.ShouldNotContain(library.Root);
    }

    [Fact]
    public async Task Get_NoEligibleSidecars_ReturnsEmptyArray()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, "Another Movie.srt");
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertFrozenNoStore(response);
        body.ShouldBe("[]");
    }

    [Fact]
    public async Task Get_RepeatedListingsOfUnchangedSet_ReturnStableOpaqueDistinctNonOrdinalTokens()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.en.srt");
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.pob.srt");
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var first = TrackElements(await (await client.GetAsync(SubtitlesRoute(KnownId), ct))
            .Content.ReadAsStringAsync(ct));
        var second = TrackElements(await (await client.GetAsync(SubtitlesRoute(KnownId), ct))
            .Content.ReadAsStringAsync(ct));

        // assert
        var firstIds = first.Select(track => track.GetProperty("id").GetString()).ToArray();
        var secondIds = second.Select(track => track.GetProperty("id").GetString()).ToArray();
        secondIds.ShouldBe(firstIds);
        firstIds.ShouldAllBe(id => !string.IsNullOrWhiteSpace(id));
        firstIds.Distinct(StringComparer.Ordinal).Count().ShouldBe(firstIds.Length);
        firstIds.ShouldNotContain("0");
        firstIds.ShouldNotContain("1");
        firstIds.ShouldNotContain("2");
        foreach (var id in firstIds)
        {
            var token = id.ShouldNotBeNull();
            token.ShouldNotContain("/");
            token.ShouldNotContain("+");
            token.ShouldNotContain("=");
            token.ShouldNotContain(".srt");
        }
    }

    [Fact]
    public async Task Get_UnknownMovie_ReturnsSanitizedNotFoundProblemDetailsWithNoStoreAndTraceId()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(UnknownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.NotFound);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(library.Root);
    }

    [Fact]
    public async Task Get_MovieFolderWithoutPrimaryVideo_ReturnsNotFoundProblemDetails()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMovieFile(MovieFolder, "Heat.1995.1080p.srt");
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain("Heat.1995.1080p.srt");
    }

    [Fact]
    public async Task Get_LibraryFolderOutsideAuthorizedRoot_ReturnsForbiddenThroughSingleHandler()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var outside = new TemporaryLibrary();
        outside.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        outside.WriteMovieFile(MovieFolder, "Heat.1995.1080p.srt");
        DetailsReturning(KnownDetails(outside.Root));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.Forbidden);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(outside.Root);
        body.ShouldNotContain(PrimaryFileName);
    }

    [Fact]
    public async Task Get_UnexpectedFailure_ReturnsGenericSanitizedProblemDetailsWithNoStoreAndTraceId()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        const string sentinel = "sentinel-subtitle-list-failure";
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException(sentinel));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SubtitlesRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        AssertFrozenNoStore(response);
        var problem = JsonSerializer.Deserialize<JsonElement>(body);
        problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.InternalServerError);
        problem.GetProperty("title").GetString().ShouldBe("Internal Server Error");
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(sentinel);
        body.ShouldNotContain(nameof(InvalidOperationException));
        body.ShouldNotContain("stackTrace");
        body.ShouldNotContain(library.Root);
    }

    /// <summary>
    /// The frozen policy is <c>private, no-store</c>. Framework serialization may reorder directives,
    /// and directive order carries no HTTP meaning, so the directive set is asserted instead.
    /// </summary>
    private static void AssertFrozenNoStore(HttpResponseMessage response)
    {
        var values = response.Headers.GetValues(CacheControlHeader).ShouldHaveSingleItem();
        var directives = values.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        directives.ShouldContain(PrivateDirective);
        directives.ShouldContain(NoStoreDirective);
        directives.Length.ShouldBe(2);
    }

    private static JsonElement[] TrackElements(string body)
    {
        using var document = JsonDocument.Parse(body);
        return [.. document.RootElement.EnumerateArray().Select(element => element.Clone())];
    }

    private static string SubtitlesRoute(int id) => $"/api/movies/{id}/subtitles";

    private static MovieDetails KnownDetails(string storedPath) =>
        new(
            new MovieId(KnownId),
            "Heat",
            new LibraryPath(storedPath),
            new MediaFormat("mkv"),
            null,
            false,
            false,
            Review.None,
            EnrichmentStatus.Enriched);

    private void DetailsReturning(MovieDetails details) =>
        catalog.GetDetailsAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(details);

    private WebApplicationFactory<Program> CreateHost(string libraryRoot) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Features:LocalPlay"] = "false",
                    [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = libraryRoot,
                }));
            builder.ConfigureTestServices(services => services.AddSingleton(catalog));
        });

    private sealed class TemporaryLibrary : IDisposable
    {
        public TemporaryLibrary()
        {
            Root = Path.Combine(Path.GetTempPath(), "lamuflix-subtitles-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void WriteMoviePrimary(string movieFolder, string fileName) =>
            WriteMovieFile(movieFolder, fileName, [0x1A, 0x45, 0xDF, 0xA3]);

        public void WriteMovieFile(string movieFolder, string fileName, byte[]? contents = null)
        {
            var directory = Path.Combine(Root, movieFolder);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, fileName), contents ?? [0x31, 0x0A]);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Test cleanup is best effort.
            }
        }
    }
}
