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
using LamuFlix.Infrastructure.Thumbnails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(ApiEndToEndCollection))]
public sealed class ThumbnailEndpointTests(ApiEndToEndFixture fixture) : ApiEndToEndTestBase(fixture)
{
    private const string JpegMediaType = "image/jpeg";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string ContentTypeOptionsHeader = "X-Content-Type-Options";
    private const string NoSniffValue = "nosniff";
    private const string NotFoundTitle = "Not Found";
    private const string NotFoundType = "https://tools.ietf.org/html/rfc9110#section-15.5.5";
    private const int UnknownId = 999999;
    private const int NonPositiveId = -1;

    private static readonly byte[] CachedJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private static readonly byte[] ReplacementJpeg = [0xFF, 0xD8, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66];
    private static readonly byte[] CorruptJpeg = [0x00, 0x01, 0x02];

    [Fact]
    public async Task Get_CachedJpeg_ReturnsTheExactBytesAsJpegWithNosniffWithoutGenerating()
    {
        // arrange
        using var cache = new TemporaryCacheDirectory();
        var generator = new RecordingThumbnailGenerator();
        var client = await StartHostAsync(
            configureTestServices: services => ReplaceGenerator(services, generator),
            additionalSettings: Settings(cache.Root, enabled: true));
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = await SeedAsync(MovieCatalogSeed.Create("Heat", "thumbnail-hit"));
        var id = movie.Id.ShouldNotBeNull().Value;
        cache.WriteCanonical(id, CachedJpeg);

        // act
        var response = await client.GetAsync(Route(id), cancellationToken);
        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(JpegMediaType);
        response.Headers.GetValues(ContentTypeOptionsHeader).ShouldBe([NoSniffValue]);
        body.ShouldBe(CachedJpeg);
        generator.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Get_EachMissClass_ReturnsTheSameSanitizedProblemDetails()
    {
        // arrange
        using var cache = new TemporaryCacheDirectory();
        var generator = new RecordingThumbnailGenerator();
        var client = await StartHostAsync(
            configureTestServices: services => ReplaceGenerator(services, generator),
            additionalSettings: Settings(cache.Root, enabled: true));
        var cancellationToken = TestContext.Current.CancellationToken;
        var missing = await SeedAsync(MovieCatalogSeed.Create("Missing", "thumbnail-missing"));
        var unreadable = await SeedAsync(MovieCatalogSeed.Create("Unreadable", "thumbnail-unreadable"));
        var corrupt = await SeedAsync(MovieCatalogSeed.Create("Corrupt", "thumbnail-corrupt"));
        var missingId = missing.Id.ShouldNotBeNull().Value;
        var unreadableId = unreadable.Id.ShouldNotBeNull().Value;
        var corruptId = corrupt.Id.ShouldNotBeNull().Value;
        cache.WriteDirectory(unreadableId);
        cache.WriteCanonical(corruptId, CorruptJpeg);

        // act
        var responses = new[]
        {
            await ReadMissAsync(client, Route(NonPositiveId), cancellationToken),
            await ReadMissAsync(client, Route(UnknownId), cancellationToken),
            await ReadMissAsync(client, Route(missingId), cancellationToken),
            await ReadMissAsync(client, Route(unreadableId), cancellationToken),
            await ReadMissAsync(client, Route(corruptId), cancellationToken),
        };

        // assert
        foreach (var miss in responses)
        {
            miss.Status.ShouldBe(HttpStatusCode.NotFound);
            miss.MediaType.ShouldBe(ProblemDetailsJson);
            miss.Problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.NotFound);
            miss.Problem.GetProperty("title").GetString().ShouldBe(NotFoundTitle);
            miss.Problem.GetProperty("type").GetString().ShouldBe(NotFoundType);
            miss.Problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
            miss.Body.ShouldNotContain(cache.Root, Case.Insensitive);
            miss.Body.ShouldNotContain(".jpg", Case.Insensitive);
            miss.Body.ShouldNotContain("thumbnail-corrupt", Case.Insensitive);
        }

        responses.Select(miss => miss.Problem.GetProperty("title").GetString()).Distinct().Count().ShouldBe(1);
        responses.Select(miss => miss.Problem.GetProperty("type").GetString()).Distinct().Count().ShouldBe(1);
        generator.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Get_DisabledEvenWhenACachedJpegExists_ReturnsTheUniformSanitizedProblemDetails()
    {
        // arrange
        using var cache = new TemporaryCacheDirectory();
        var generator = new RecordingThumbnailGenerator();
        var client = await StartHostAsync(
            configureTestServices: services => ReplaceGenerator(services, generator),
            additionalSettings: Settings(cache.Root, enabled: false));
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = await SeedAsync(MovieCatalogSeed.Create("Disabled", "thumbnail-disabled"));
        var id = movie.Id.ShouldNotBeNull().Value;
        cache.WriteCanonical(id, CachedJpeg);

        // act
        var miss = await ReadMissAsync(client, Route(id), cancellationToken);

        // assert
        miss.Status.ShouldBe(HttpStatusCode.NotFound);
        miss.MediaType.ShouldBe(ProblemDetailsJson);
        miss.Problem.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.NotFound);
        miss.Problem.GetProperty("title").GetString().ShouldBe(NotFoundTitle);
        miss.Problem.GetProperty("type").GetString().ShouldBe(NotFoundType);
        miss.Problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        miss.Body.ShouldNotContain(cache.Root, Case.Insensitive);
        generator.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Get_ConcurrentReadsDuringPromotion_StreamCanonicalJpegsWithoutSharedLocking()
    {
        // arrange
        using var cache = new TemporaryCacheDirectory();
        var generator = new RecordingThumbnailGenerator();
        var client = await StartHostAsync(
            configureTestServices: services => ReplaceGenerator(services, generator),
            additionalSettings: Settings(cache.Root, enabled: true));
        var cancellationToken = TestContext.Current.CancellationToken;
        var movie = await SeedAsync(MovieCatalogSeed.Create("Concurrent", "thumbnail-concurrent"));
        var id = movie.Id.ShouldNotBeNull().Value;
        cache.WriteCanonical(id, CachedJpeg);

        // act
        var reads = Enumerable.Range(0, 8)
            .Select(_ => ReadJpegAsync(client, Route(id), cancellationToken))
            .ToArray();
        var promoter = cache.CreatePromoter();
        var promotions = Task.Run(
            () => cache.PromoteRepeatedly(promoter, id, ReplacementJpeg, iterations: 8, cancellationToken),
            cancellationToken);
        var observed = await Task.WhenAll(reads);
        await promotions;

        // assert
        foreach (var read in observed)
        {
            read.Status.ShouldBe(HttpStatusCode.OK);
            read.MediaType.ShouldBe(JpegMediaType);
            read.NoSniff.ShouldBe(NoSniffValue);
            (read.Body.SequenceEqual(CachedJpeg) || read.Body.SequenceEqual(ReplacementJpeg)).ShouldBeTrue();
        }

        generator.Calls.ShouldBe(0);
    }

    [Fact]
    public void Routes_ThumbnailEndpoint_MirrorsTheLibraryCallerAccessPolicy()
    {
        // arrange
        using var host = new ApiHostFactory();
        var endpoints = host.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .ToArray();

        // act
        var thumbnail = endpoints.First(endpoint => RouteOf(endpoint) == "/api/movies/{id:int}/thumbnail");
        var library = endpoints.First(endpoint =>
            RouteOf(endpoint) == "/api/movies/{id:int}" && IsGet(endpoint));

        // assert
        AuthorizeMetadata(thumbnail).ShouldBe(AuthorizeMetadata(library));
        AllowAnonymousMetadata(thumbnail).ShouldBe(AllowAnonymousMetadata(library));
    }

    private static string Route(int id) => $"/api/movies/{id}/thumbnail";

    private static string RouteOf(RouteEndpoint endpoint) => endpoint.RoutePattern.RawText ?? string.Empty;

    private static bool IsGet(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Contains("GET", StringComparer.Ordinal) == true;

    private static string[] AuthorizeMetadata(RouteEndpoint endpoint) =>
        [.. endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Select(data => $"{data.Policy}|{data.Roles}|{data.AuthenticationSchemes}")];

    private static bool AllowAnonymousMetadata(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;

    private static KeyValuePair<string, string?>[] Settings(string cacheDirectory, bool enabled) =>
    [
        new($"{ThumbnailOptions.SectionName}:{nameof(ThumbnailOptions.Enabled)}", enabled ? "true" : "false"),
        new($"{ThumbnailOptions.SectionName}:{nameof(ThumbnailOptions.CacheDirectory)}", cacheDirectory),
    ];

    private static void ReplaceGenerator(IServiceCollection services, IThumbnailGenerator generator)
    {
        services.RemoveAll<IThumbnailGenerator>();
        services.AddSingleton(generator);
    }

    private static async Task<JpegRead> ReadJpegAsync(HttpClient client, string route, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(route, cancellationToken);
        var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return new JpegRead(
            response.StatusCode,
            response.Content.Headers.ContentType?.MediaType,
            response.Headers.TryGetValues(ContentTypeOptionsHeader, out var values) ? values.Single() : null,
            body);
    }

    private static async Task<MissRead> ReadMissAsync(HttpClient client, string route, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(route, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(body);
        return new MissRead(
            response.StatusCode,
            response.Content.Headers.ContentType?.MediaType,
            document.RootElement.Clone(),
            body);
    }

    private sealed record JpegRead(HttpStatusCode Status, string? MediaType, string? NoSniff, byte[] Body);

    private sealed record MissRead(HttpStatusCode Status, string? MediaType, JsonElement Problem, string Body);

    private sealed class RecordingThumbnailGenerator : IThumbnailGenerator
    {
        private int calls;

        public int Calls => Volatile.Read(ref calls);

        public Task<ThumbnailResult> GenerateAsync(string sourcePath, CancellationToken cancellationToken = default)
        {
            _ = sourcePath;
            _ = cancellationToken;
            Interlocked.Increment(ref calls);
            return Task.FromResult(new ThumbnailResult(false, null, "Disabled"));
        }
    }

    private sealed class TemporaryCacheDirectory : IDisposable
    {
        public TemporaryCacheDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "lamuflix-dev428-endpoint-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void WriteCanonical(int id, byte[] bytes) =>
            File.WriteAllBytes(CanonicalPath(id), bytes);

        public void WriteDirectory(int id) =>
            Directory.CreateDirectory(CanonicalPath(id));

        public ThumbnailCache CreatePromoter()
        {
            var environment = Substitute.For<IHostEnvironment>();
            environment.ContentRootPath.Returns(Root);
            return new ThumbnailCache(
                Options.Create(new ThumbnailOptions { Enabled = true, CacheDirectory = Root }),
                environment);
        }

        public void PromoteRepeatedly(
            ThumbnailCache promoter,
            int id,
            byte[] bytes,
            int iterations,
            CancellationToken cancellationToken)
        {
            for (var index = 0; index < iterations; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var temporary = Path.Combine(Root, Guid.NewGuid().ToString("N") + ".jpg");
                File.WriteAllBytes(temporary, bytes);
                promoter.TryPromote(temporary, new MovieId(id)).ShouldBeTrue();
                Thread.Sleep(5);
            }
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                _ = exception;
            }
        }

        private string CanonicalPath(int id) => Path.Combine(Root, id + ".jpg");
    }
}
