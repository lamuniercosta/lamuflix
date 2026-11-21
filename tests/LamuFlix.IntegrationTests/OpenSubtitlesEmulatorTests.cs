using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

public sealed class OpenSubtitlesEmulatorTests
{
    private const string ApiKey = "emulator-test-key";
    private const string UserAgent = "LamuFlix/1.0 (emulator self-test)";
    private const string SearchPath = OpenSubtitlesEmulator.SearchPath;
    private const string DownloadPath = OpenSubtitlesEmulator.DownloadPath;
    private const string JsonAccept = "application/json";
    private static readonly HttpClient Client = new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    });

    public static TheoryData<string, string> CanonicalRows => new()
    {
        { "query=13+Going+On+30", "query=13+going+on+30" },
        { "query=AMELIE", "query=amelie" },
        { "query=13%20going", "query=13+going" },
        { "languages=PT-BR", "languages=pt-br" },
        { "Languages=pt-br", "languages=pt-br" },
        { "type=Movie", "type=movie" },
        { "moviehash=ABCDEF0123456789", "moviehash=abcdef0123456789" },
        { "moviehash=abcdef0123456789&languages=pt-br", "languages=pt-br&moviehash=abcdef0123456789" },
        { "query=amelie&languages=pt-br", "languages=pt-br&query=amelie" },
        { "query=am%C3%A9lie", "query=am%C3%A9lie" },
        { "query=am%c3%a9lie", "query=am%c3%a9lie" },
        { "query=Am%C3%A9lie", "query=Am%C3%A9lie" },
        { "query=AM%C3%89LIE", "query=AM%C3%89LIE" },
        { "query=star+wars%3A+episode", "query=star+wars%3A+episode" },
        { "query=star+wars%3a+episode", "query=star+wars%3a+episode" },
        { "query=a:b", "query=a:b" },
        { "query=x%2By", "query=x%2By" },
        { "query=tom+%26+jerry", "query=tom+%26+jerry" },
        { "languages=pt-br&query=amelie&type=movie", "languages=pt-br&query=amelie&type=movie" },
        { "languages=en,pt-br", "languages=en,pt-br" },
        { "languages=pt-br,en", "languages=pt-br,en" },
    };

    [Theory]
    [MemberData(nameof(CanonicalRows))]
    public void CanonicalQuery_MatchesTheContractRow(string raw, string canonical) =>
        OpenSubtitlesEmulator.CanonicalQuery(raw).ShouldBe(canonical);

    [Fact]
    public async Task Search_WithoutUserAgent_IsForbidden()
    {
        // arrange
        using var emulator = new OpenSubtitlesEmulator(ApiKey);

        // act
        var reply = await SendAsync(HttpMethod.Get, Url(emulator, SearchPath, "languages=en"), ApiKey, userAgent: null, ct: TestContext.Current.CancellationToken);

        // assert
        reply.Status.ShouldBe(HttpStatusCode.Forbidden);
        reply.Body.ShouldContain("User agent required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("wrong-key")]
    public async Task Search_WithoutTheRightKey_IsForbiddenWithTheProviderMessage(string? key)
    {
        // arrange
        using var emulator = new OpenSubtitlesEmulator(ApiKey);

        // act
        var reply = await SendAsync(HttpMethod.Get, Url(emulator, SearchPath, "languages=en"), key, UserAgent, TestContext.Current.CancellationToken);

        // assert
        reply.Status.ShouldBe(HttpStatusCode.Forbidden);
        reply.Body.ShouldContain("You cannot consume this service");
    }

    [Fact]
    public async Task Search_WithTheKeyAndAgent_IsOkWithPerSecondRateLimitHeaders()
    {
        // arrange
        using var emulator = new OpenSubtitlesEmulator(ApiKey);

        // act
        var reply = await SendAsync(HttpMethod.Get, Url(emulator, SearchPath, "languages=en"), ApiKey, UserAgent, TestContext.Current.CancellationToken);

        // assert
        reply.Status.ShouldBe(HttpStatusCode.OK);
        reply.Headers.ShouldContainKey("RateLimit-Limit");
        reply.Headers.ShouldContainKey("X-RateLimit-Remaining-Second");
        reply.Body.ShouldContain("\"total_count\":0");
    }

    [Fact]
    public async Task Search_NonCanonicalQuery_Is301WithoutRateLimitHeadersEvenWithoutAKey()
    {
        // arrange
        using var emulator = new OpenSubtitlesEmulator(ApiKey);

        // act
        var reply = await SendAsync(HttpMethod.Get, Url(emulator, SearchPath, "query=AMELIE"), key: null, UserAgent, TestContext.Current.CancellationToken);

        // assert
        reply.Status.ShouldBe(HttpStatusCode.MovedPermanently);
        reply.Headers["Location"].ShouldBe($"{SearchPath}?query=amelie");
        reply.Headers["X-OS-Rule"].ShouldBe("canonical");
        reply.Headers.Keys.ShouldNotContain("RateLimit-Limit");
    }

    [Fact]
    public async Task Search_TrailingSlash_RoutesToTheSearchEndpoint()
    {
        // arrange
        using var emulator = new OpenSubtitlesEmulator(ApiKey);

        // act
        var reply = await SendAsync(HttpMethod.Get, Url(emulator, SearchPath + "/", "languages=en"), ApiKey, UserAgent, TestContext.Current.CancellationToken);

        // assert
        reply.Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostSubtitles_WithTheKey_IsNotFoundWithRateLimitHeaders()
    {
        // arrange
        using var emulator = new OpenSubtitlesEmulator(ApiKey);

        // act
        var reply = await SendAsync(HttpMethod.Post, Url(emulator, SearchPath, null), ApiKey, UserAgent, TestContext.Current.CancellationToken, "{}");

        // assert
        reply.Status.ShouldBe(HttpStatusCode.NotFound);
        reply.Body.ShouldContain("Not Found");
        reply.Headers.ShouldContainKey("RateLimit-Limit");
    }

    [Fact]
    public async Task PostSubtitles_WithoutTheKey_IsForbidden()
    {
        // arrange
        using var emulator = new OpenSubtitlesEmulator(ApiKey);

        // act
        var reply = await SendAsync(HttpMethod.Post, Url(emulator, SearchPath, null), key: null, UserAgent, TestContext.Current.CancellationToken, "{}");

        // assert
        reply.Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Download_WithoutTheKey_IsForbidden()
    {
        // arrange
        using var emulator = new OpenSubtitlesEmulator(ApiKey);

        // act
        var reply = await SendAsync(HttpMethod.Post, Url(emulator, DownloadPath, null), key: null, UserAgent, TestContext.Current.CancellationToken, "{\"file_id\":101}");

        // assert
        reply.Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Download_WithTheKey_ReturnsTheLinkAndTheQuotaInTheBody()
    {
        // arrange
        using var emulator = new OpenSubtitlesEmulator(ApiKey);

        // act
        var reply = await SendAsync(HttpMethod.Post, Url(emulator, DownloadPath, null), ApiKey, UserAgent, TestContext.Current.CancellationToken, "{\"file_id\":101}", JsonAccept);

        // assert
        reply.Status.ShouldBe(HttpStatusCode.OK);
        reply.Body.ShouldContain(emulator.DownloadLinkFor(101));
        reply.Body.ShouldContain("\"remaining\":99");
        reply.Body.ShouldContain("\"reset_time_utc\":\"");
        reply.Headers.Keys.ShouldNotContain("X-RateLimit-Remaining");
    }

    [Fact]
    public async Task Download_WithoutAJsonAcceptHeader_ReturnsTheServiceUnavailableHtmlPage()
    {
        // arrange
        using var emulator = new OpenSubtitlesEmulator(ApiKey);

        // act
        var reply = await SendAsync(HttpMethod.Post, Url(emulator, DownloadPath, null), ApiKey, UserAgent, TestContext.Current.CancellationToken, "{\"file_id\":101}");

        // assert
        reply.Status.ShouldBe(HttpStatusCode.ServiceUnavailable);
        reply.Headers["Content-Type"].ShouldStartWith("text/html");
        reply.Body.ShouldContain("Error 503 - OpenSubtitles.com");
    }

    [Fact]
    public async Task LinkServer_ServesTheSrtWithoutAnApiKey()
    {
        // arrange
        using var emulator = new OpenSubtitlesEmulator(ApiKey);
        var link = emulator.DownloadLinkFor(101);

        // act
        var reply = await SendAsync(HttpMethod.Get, new Uri(link), key: null, UserAgent, TestContext.Current.CancellationToken);

        // assert
        reply.Status.ShouldBe(HttpStatusCode.OK);
        reply.Headers["Content-Type"].ShouldStartWith("text/plain");
        reply.Headers["Content-Type"].ShouldNotContain("charset");
        reply.Headers["Content-Disposition"].ShouldStartWith("attachment");
        reply.Body.ShouldBe(OpenSubtitlesScenario.DefaultSrt);
        reply.Headers.Keys.ShouldNotContain("Content-Length");
        reply.Headers["Transfer-Encoding"].ShouldBe("chunked");
        emulator.Links.LogEntries.Count().ShouldBe(1);
    }

    private static string Url(OpenSubtitlesEmulator emulator, string path, string? query) =>
        query is null ? emulator.BaseUrl + path : $"{emulator.BaseUrl}{path}?{query}";

    private static Task<Reply> SendAsync(
        HttpMethod method,
        string url,
        string? key,
        string? userAgent,
        CancellationToken ct,
        string? jsonBody = null,
        string? accept = null) => SendAsync(method, new Uri(url), key, userAgent, ct, jsonBody, accept);

    private static async Task<Reply> SendAsync(
        HttpMethod method,
        Uri uri,
        string? key,
        string? userAgent,
        CancellationToken ct,
        string? jsonBody = null,
        string? accept = null)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (accept is not null)
        {
            request.Headers.TryAddWithoutValidation("Accept", accept);
        }

        if (key is not null)
        {
            request.Headers.TryAddWithoutValidation("Api-Key", key);
        }

        if (userAgent is not null)
        {
            request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        }

        if (jsonBody is not null)
        {
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        }

        using var response = await Client.SendAsync(request, ct);
        var headers = response.Headers.Concat(response.Content.Headers)
            .ToDictionary(header => header.Key, header => string.Join(",", header.Value), StringComparer.OrdinalIgnoreCase);
        var body = await response.Content.ReadAsStringAsync(ct);
        return new Reply(response.StatusCode, headers, body);
    }

    private sealed record Reply(HttpStatusCode Status, IReadOnlyDictionary<string, string> Headers, string Body);
}
