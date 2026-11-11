using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(SubtitleEndpointCollection))]
public sealed class SubtitlePrepareServeEndpointTests(ApiHostFactory factory)
{
    private const int KnownId = 11;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";
    private const string SidecarFileName = "Heat.1995.1080p.srt";
    private const string SearchPath = "/api/v1/subtitles";
    private const string DownloadPath = "/api/v1/download";
    private const string LinkPath = "/srt/101.srt";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string SidecarContentType = "application/x-subrip";
    private const string ContentTypeOptionsHeader = "X-Content-Type-Options";
    private const string ContentDispositionHeader = "Content-Disposition";
    private const string NoSniffValue = "nosniff";
    private const string SentinelKey = "sentinel-subtitle-key";
    private const string SidecarText =
        "1\n00:00:01,000 --> 00:00:03,500\nI have seen things.\n\n2\n00:00:04,000 --> 00:00:06,000\nYou people would not believe.\n";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();

    private static readonly string SearchBody = """
        {
          "data": [
            {
              "attributes": {
                "language": "en",
                "download_count": 42,
                "moviehash_match": false,
                "files": [ { "file_id": 101 } ]
              }
            }
          ]
        }
        """;

    [Fact]
    public async Task GetSidecar_ExistingExactBasenameSidecar_ReturnsExactBytesAndHardenedHeaders()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, SidecarFileName, Encoding.UTF8.GetBytes(SidecarText));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SidecarRoute(KnownId), ct);
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(SidecarContentType);
        response.Headers.GetValues(ContentTypeOptionsHeader).ShouldBe([NoSniffValue]);
        var disposition = response.Content.Headers.GetValues(ContentDispositionHeader).ShouldHaveSingleItem();
        disposition.ShouldStartWith("attachment");
        disposition.ShouldContain(SidecarFileName);
        disposition.ShouldNotContain(library.Root);
        disposition.ShouldNotContain("/");
        bytes.ShouldBe(Encoding.UTF8.GetBytes(SidecarText));
    }

    [Theory]
    [InlineData("SaveEnabled")]
    [InlineData("ApiKey")]
    [InlineData("ReadOnly")]
    [InlineData("Defaults")]
    public async Task GetSidecar_ServingIsIndependentOfAcquisitionSettings(string configuration)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, SidecarFileName, Encoding.UTF8.GetBytes(SidecarText));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, SettingsFor(configuration));
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SidecarRoute(KnownId), ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues(ContentTypeOptionsHeader).ShouldBe([NoSniffValue]);
        (await response.Content.ReadAsStringAsync(ct)).ShouldBe(SidecarText);
    }

    [Fact]
    public async Task GetSidecar_ExactBytes_AreServedWithoutTheDownloadedSrtValidityGate()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        var raw = new byte[] { 0x00, 0xFF, 0x10, 0x3C, 0x62, 0x3E };
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, SidecarFileName, raw);
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SidecarRoute(KnownId), ct);
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        bytes.ShouldBe(raw);
    }

    [Fact]
    public async Task GetSidecar_MissingSidecar_ReturnsSanitizedNotFoundWithTraceId()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SidecarRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        JsonSerializer.Deserialize<JsonElement>(body).GetProperty("traceId").GetString()
            .ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(library.Root);
        body.ShouldNotContain(SidecarFileName);
    }

    [Fact]
    public async Task GetSidecar_UnknownMovie_ReturnsSanitizedNotFound()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SidecarRoute(999), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        JsonSerializer.Deserialize<JsonElement>(body).GetProperty("traceId").GetString()
            .ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(library.Root);
    }

    [Fact]
    public async Task GetSidecar_LibraryFolderOutsideAuthorizedRoot_ReturnsForbidden()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var outside = new TemporaryLibrary();
        outside.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        outside.WriteMovieFile(MovieFolder, SidecarFileName, Encoding.UTF8.GetBytes(SidecarText));
        DetailsReturning(KnownDetails(outside.Root));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SidecarRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        body.ShouldNotContain(outside.Root);
        body.ShouldNotContain(SidecarText);
    }

    [Fact]
    public async Task GetSidecar_SidecarBeyondConfiguredServingBound_ReturnsUnprocessableEntity()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, SidecarFileName, Encoding.UTF8.GetBytes(SidecarText));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, new Dictionary<string, string?>
        {
            [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.MaxSubtitleInputBytes)}"] = "8",
        });
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SidecarRoute(KnownId), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        JsonSerializer.Deserialize<JsonElement>(body).GetProperty("traceId").GetString()
            .ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain("I have seen things");
    }

    [Fact]
    public async Task GetSidecar_MalformedRawIdSegment_Returns400ProblemDetails()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync("/api/movies/not-a-number/subtitles/sidecar", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        JsonSerializer.Deserialize<JsonElement>(body).GetProperty("traceId").GetString()
            .ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Prepare_MalformedRawIdSegment_Returns400ProblemDetails()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync("/api/movies/not-a-number/subtitles/prepare", null, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        JsonSerializer.Deserialize<JsonElement>(body).GetProperty("traceId").GetString()
            .ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Prepare_ExistingSidecar_ReturnsAlreadyPresentWithNullLanguageAndNoProviderTraffic()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var provider = LoopbackWireMock.StartHttps();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, SidecarFileName, Encoding.UTF8.GetBytes(SidecarText));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, ProviderSettings(provider, saveEnabled: false));
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        payload.GetProperty("outcome").GetString().ShouldBe("alreadyPresent");
        payload.GetProperty("selectedLanguage").ValueKind.ShouldBe(JsonValueKind.Null);
        provider.LogEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Prepare_AcquisitionDisabled_Returns409WithoutPathOrSecretDisclosure()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, SettingsFor("Defaults"));
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        JsonSerializer.Deserialize<JsonElement>(body).GetProperty("traceId").GetString()
            .ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(library.Root);
        body.ShouldNotContain(SentinelKey);
    }

    [Fact]
    public async Task Prepare_InvalidLocalBaseUrl_Returns409()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, new Dictionary<string, string?>
        {
            [$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.SaveEnabled)}"] = "true",
            [$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.PreferredLanguages)}:0"] = "en",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.ApiKey)}"] = SentinelKey,
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.UserAgent)}"] = "LamuFlix/test",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.BaseUrl)}"] = "not-a-url",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.DownloadAllowedHosts)}:0"] = "127.0.0.1",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.MaxDownloadBytes)}"] = "65536",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.RequestTimeout)}"] = "00:00:05",
        });
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync(ct)).ShouldNotContain(SentinelKey);
    }

    [Fact]
    public async Task Prepare_NoSidecarAndValidSettings_ReturnsSavedWithMatchedLanguageAndWritesSidecar()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var provider = LoopbackWireMock.StartHttps();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        StubProvider(provider);
        await using var host = CreateHost(library.Root, ProviderSettings(provider));
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        payload.GetProperty("outcome").GetString().ShouldBe("saved");
        payload.GetProperty("selectedLanguage").GetString().ShouldBe("en");
        var written = Path.Combine(library.Root, MovieFolder, SidecarFileName);
        File.Exists(written).ShouldBeTrue();
        (await File.ReadAllTextAsync(written, ct)).ShouldBe(SidecarText);
        provider.LogEntries.Count(entry => string.Equals(entry.RequestMessage?.Path, SearchPath, StringComparison.Ordinal)).ShouldBe(1);
        provider.LogEntries.Count(entry => string.Equals(entry.RequestMessage?.Path, DownloadPath, StringComparison.Ordinal)).ShouldBe(1);
        provider.LogEntries.Count(entry => string.Equals(entry.RequestMessage?.Path, LinkPath, StringComparison.Ordinal)).ShouldBe(1);
    }

    [Fact]
    public async Task Prepare_NoEligiblePreferredLanguage_ReturnsNoMatchWithNullLanguageAndWritesNothing()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var provider = LoopbackWireMock.StartHttps();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"data":[]}"""));
        await using var host = CreateHost(library.Root, ProviderSettings(provider));
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = JsonSerializer.Deserialize<JsonElement>(body);
        payload.GetProperty("outcome").GetString().ShouldBe("noMatch");
        payload.GetProperty("selectedLanguage").ValueKind.ShouldBe(JsonValueKind.Null);
        File.Exists(Path.Combine(library.Root, MovieFolder, SidecarFileName)).ShouldBeFalse();
        provider.LogEntries.Count(entry => string.Equals(entry.RequestMessage?.Path, DownloadPath, StringComparison.Ordinal)).ShouldBe(0);
    }

    [Theory]
    [InlineData("120")]
    [InlineData("Wed, 11 Nov 2026 12:00:00 GMT")]
    public async Task Prepare_ProviderQuotaExhausted_Returns429WithRetryAfter(string retryAfter)
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var provider = LoopbackWireMock.StartHttps();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(429)
                .WithHeader("Retry-After", retryAfter));
        await using var host = CreateHost(library.Root, ProviderSettings(provider));
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.ShouldNotBeNull();
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
    }

    [Fact]
    public async Task Prepare_ProviderQuotaWithoutRetryAfter_Returns429WithoutTheHeader()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var provider = LoopbackWireMock.StartHttps();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(429));
        await using var host = CreateHost(library.Root, ProviderSettings(provider));
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.ShouldBeNull();
    }

    [Fact]
    public async Task Prepare_ProviderRedirect_Returns502AndNeverContactsTheRedirectTarget()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var provider = LoopbackWireMock.StartHttps();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(302)
                .WithHeader("Location", LinkPath));
        await using var host = CreateHost(library.Root, ProviderSettings(provider));
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        provider.LogEntries.Count(entry => string.Equals(entry.RequestMessage?.Path, LinkPath, StringComparison.Ordinal)).ShouldBe(0);
        File.Exists(Path.Combine(library.Root, MovieFolder, SidecarFileName)).ShouldBeFalse();
    }

    [Fact]
    public async Task Prepare_DownloadLinkOutsideTheAllowlist_Returns502WithoutContactingIt()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var provider = LoopbackWireMock.StartHttps();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(SearchBody));
        provider.Given(Request.Create().WithPath(DownloadPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"link":"https://evil.invalid/srt/101.srt"}"""));
        await using var host = CreateHost(library.Root, ProviderSettings(provider));
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        provider.LogEntries.Count(entry => string.Equals(entry.RequestMessage?.Path, LinkPath, StringComparison.Ordinal)).ShouldBe(0);
    }

    [Fact]
    public async Task Prepare_ProviderTimeout_Returns504()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var provider = LoopbackWireMock.StartHttps();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(SearchBody)
                .WithDelay(TimeSpan.FromSeconds(20)));
        await using var host = CreateHost(library.Root, ProviderSettings(provider, requestTimeout: "00:00:01"));
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
    }

    [Fact]
    public async Task Prepare_CallerAbort_NeverMapsToAProviderGatewayTimeout()
    {
        // arrange
        using var library = new TemporaryLibrary();
        using var provider = LoopbackWireMock.StartHttps();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(SearchBody)
                .WithDelay(TimeSpan.FromSeconds(20)));
        await using var host = CreateHost(library.Root, ProviderSettings(provider));
        using var client = host.CreateClient();
        using var abort = new CancellationTokenSource();

        // act
        var request = client.PostAsync(PrepareRoute(KnownId), null, abort.Token);
        await abort.CancelAsync();
        var abortObserved = false;
        try
        {
            using var response = await request;
            response.StatusCode.ShouldNotBe(HttpStatusCode.GatewayTimeout);
        }
        catch (OperationCanceledException)
        {
            abortObserved = true;
        }

        // assert
        abortObserved.ShouldBeTrue();
        File.Exists(Path.Combine(library.Root, MovieFolder, SidecarFileName)).ShouldBeFalse();
    }

    [Fact]
    public async Task Prepare_InvalidDownloadedSrt_Returns502AndWritesNothing()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var provider = LoopbackWireMock.StartHttps();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(SearchBody));
        provider.Given(Request.Create().WithPath(DownloadPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""{"link":"{{LinkUrl(provider)}}"}"""));
        provider.Given(Request.Create().WithPath(LinkPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("<html>not a subtitle</html>"));
        await using var host = CreateHost(library.Root, ProviderSettings(provider));
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        File.Exists(Path.Combine(library.Root, MovieFolder, SidecarFileName)).ShouldBeFalse();
    }

    [Fact]
    public async Task Prepare_MissingMovie_Returns404()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Prepare_MovieFolderOutsideAuthorizedRoot_Returns403()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var outside = new TemporaryLibrary();
        outside.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(outside.Root));
        await using var host = CreateHost(library.Root);
        using var client = host.CreateClient();

        // act
        var response = await client.PostAsync(PrepareRoute(KnownId), null, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        body.ShouldNotContain(outside.Root);
    }

    [Fact]
    public async Task GetSidecar_ServesWithoutTouchingTheProvider_WhileOrdinaryStreamingDoesNotPrepare()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        using var provider = LoopbackWireMock.StartHttps();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        library.WriteMovieFile(MovieFolder, SidecarFileName, Encoding.UTF8.GetBytes(SidecarText));
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, ProviderSettings(provider));
        using var client = host.CreateClient();

        // act
        var sidecar = await client.GetAsync(SidecarRoute(KnownId), ct);
        var list = await client.GetAsync($"/api/movies/{KnownId}/subtitles", ct);

        // assert
        sidecar.StatusCode.ShouldBe(HttpStatusCode.OK);
        list.StatusCode.ShouldBe(HttpStatusCode.OK);
        provider.LogEntries.ShouldBeEmpty();
    }

    private static void StubProvider(WireMockServer provider)
    {
        provider.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(SearchBody));
        provider.Given(Request.Create().WithPath(DownloadPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($$"""{"link":"{{LinkUrl(provider)}}"}"""));
        provider.Given(Request.Create().WithPath(LinkPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithHeader("Content-Type", "application/x-subrip")
                .WithBody(SidecarText));
    }

    private static string LinkUrl(WireMockServer provider) =>
        string.Concat(provider.Urls[0], LinkPath);

    private static Dictionary<string, string?> SettingsFor(string configuration) => configuration switch
    {
        "SaveEnabled" => new Dictionary<string, string?>
        {
            [$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.SaveEnabled)}"] = "true",
            [$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.PreferredLanguages)}:0"] = "en",
        },
        "ApiKey" => new Dictionary<string, string?>
        {
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.ApiKey)}"] = SentinelKey,
        },
        "ReadOnly" => new Dictionary<string, string?>
        {
            [$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.SaveEnabled)}"] = "false",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.ApiKey)}"] = string.Empty,
        },
        _ => new Dictionary<string, string?>(),
    };

    private static Dictionary<string, string?> ProviderSettings(
        WireMockServer provider,
        bool saveEnabled = true,
        string requestTimeout = "00:00:05") =>
        new()
        {
            [$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.SaveEnabled)}"] = saveEnabled ? "true" : "false",
            [$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.PreferredLanguages)}:0"] = "en",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.ApiKey)}"] = SentinelKey,
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.UserAgent)}"] = "LamuFlix/test",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.BaseUrl)}"] = provider.Urls[0],
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.DownloadAllowedHosts)}:0"] = "127.0.0.1",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.MaxDownloadBytes)}"] = "65536",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.RequestTimeout)}"] = requestTimeout,
        };

    private void DetailsReturning(MovieDetails details) =>
        catalog.GetDetailsAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(details);

    private static OpenSubtitlesSubtitleProvider CreateTestSubtitleProvider(IServiceProvider services)
    {
        var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        })
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        return new OpenSubtitlesSubtitleProvider(
            client,
            services.GetRequiredService<IOptions<SubtitleProviderOptions>>(),
            services.GetRequiredService<TimeProvider>());
    }

    private WebApplicationFactory<Program> CreateHost(
        string libraryRoot,
        IReadOnlyDictionary<string, string?>? additionalSettings = null) =>
        factory.WithWebHostBuilder(builder =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Features:LocalPlay"] = "false",
                [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = libraryRoot,
            };
            foreach (var setting in additionalSettings ?? new Dictionary<string, string?>())
            {
                settings[setting.Key] = setting.Value;
            }

            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings));
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(catalog);
                services.AddSingleton<ISubtitleProvider>(CreateTestSubtitleProvider);
            });
        });

    private static string PrepareRoute(int id) =>
        string.Create(CultureInfo.InvariantCulture, $"/api/movies/{id}/subtitles/prepare");

    private static string SidecarRoute(int id) =>
        string.Create(CultureInfo.InvariantCulture, $"/api/movies/{id}/subtitles/sidecar");

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

    private sealed class TemporaryLibrary : IDisposable
    {
        public TemporaryLibrary()
        {
            Root = Path.Combine(Path.GetTempPath(), "lamuflix-subtitle-serve-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void WriteMoviePrimary(string movieFolder, string fileName) =>
            WriteMovieFile(movieFolder, fileName, [0x1A, 0x45, 0xDF, 0xA3]);

        public void WriteMovieFile(string movieFolder, string fileName, byte[] contents)
        {
            var directory = Path.Combine(Root, movieFolder);
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(Path.Combine(directory, fileName), contents);
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
