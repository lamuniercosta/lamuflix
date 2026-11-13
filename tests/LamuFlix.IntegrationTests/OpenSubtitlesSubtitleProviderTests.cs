using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.Options;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(SubtitleProviderCollection.Name)]
public sealed class OpenSubtitlesSubtitleProviderTests : IDisposable
{
    private const string ApiKey = "sentinel-api-key";
    private const string UserAgent = "LamuFlix/1.0 (personal library)";
    private const string Hash = "a1b2c3d4e5f60718";
    private const long MaxBytes = 4_096;
    private const string SearchPath = "/api/v1/subtitles";
    private const string DownloadPath = "/api/v1/download";
    private const string LinkPath = "/srt/101.srt";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    private static readonly DateTimeOffset Now = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);

    private const string ValidSrt = "1\n00:00:01,000 --> 00:00:03,500\nI have seen things.\n\n2\n00:00:04,000 --> 00:00:06,000\nYou people would not believe.\n";

    private const string SearchBody = """
        {
          "data": [
            {
              "attributes": {
                "language": "en",
                "download_count": 42,
                "moviehash_match": true,
                "release": "Dune.2021.1080p.BluRay.x264-GROUP",
                "files": [ { "file_id": 101 }, { "file_id": 102 } ]
              }
            },
            {
              "attributes": {
                "language": "pt-br",
                "download_count": 7,
                "moviehash_match": false,
                "files": [ { "file_id": 201, "file_name": "Dune.2021.WEB-DL.H264.srt" } ]
              }
            }
          ]
        }
        """;

    public static TheoryData<string> InvalidPayloads => new()
    {
        string.Empty,
        "\u0000",
        "{\"link\":\"x\"}",
        "[1,2,3]",
        "<html><body>not a subtitle</body></html>",
        "PK\u0003\u0004binary",
        "1\nno timestamps here\n",
        "1\n00:00:01 --> 00:00:03\ntext\n",
    };

    private readonly WireMockServer server = LoopbackWireMock.StartHttps();
    private readonly List<IDisposable> disposables = [];

    public void Dispose()
    {
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }

        server.Stop();
        server.Dispose();
    }

    private string BaseUrl => server.Urls[0];

    [Fact]
    public async Task SearchAsync_SendsApiKeyAndUserAgentHeadersWithTheHashAndLanguagePreferences()
    {
        // arrange
        StubSearch(SearchBody);
        var provider = Provider();

        // act
        var result = await provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, Title: null, ["en", "PT-BR"]),
            TestContext.Current.CancellationToken);

        // assert
        var candidates = result.Candidates;
        candidates.Count.ShouldBe(2);
        candidates[0].FileId.ShouldBe(101);
        candidates[0].Language.ShouldBe("en");
        candidates[0].DownloadCount.ShouldBe(42);
        candidates[0].MatchesMovieHash.ShouldBeTrue();
        candidates[0].ReleaseName.ShouldBe("Dune.2021.1080p.BluRay.x264-GROUP");
        candidates[1].FileId.ShouldBe(201);
        candidates[1].ReleaseName.ShouldBe("Dune.2021.WEB-DL.H264");
        var (body, apiKeys, userAgents) = SearchRequest();
        apiKeys.ShouldBe([ApiKey]);
        userAgents.ShouldBe([UserAgent]);
        body.ShouldContain("\"moviehash\":\"a1b2c3d4e5f60718\"");
        body.ShouldContain("\"languages\":\"en,pt-br\"");
        body.ShouldNotContain("\"query\"");
    }

    [Fact]
    public async Task SearchAsync_TitleSearch_SendsTheTitleAndNoHash()
    {
        // arrange
        StubSearch(SearchBody);
        var provider = Provider();

        // act
        await provider.SearchAsync(
            new SubtitleSearch(MovieHash: null, 900, "Blade Runner", ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        SearchRequest().Body.ShouldContain("\"query\":\"Blade Runner\"");
        SearchRequest().Body.ShouldNotContain("\"moviehash\"");
    }

    [Fact]
    public async Task SearchAsync_RejectedApiKey_FailsWithoutExposingTheKey()
    {
        // arrange
        server.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(401).WithBody("{\"message\":\"unauthorized\"}"));
        var provider = Provider();

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        var failure = await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        failure.Message.ShouldNotContain(ApiKey);
    }

    [Fact]
    public async Task SearchAsync_UnusableBody_FailsAsAProviderFailure()
    {
        // arrange
        StubSearch("{ not json");
        var provider = Provider();

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
    }

    [Fact]
    public async Task SearchAsync_BaseUrlHostOutsideTheAllowlist_FailsBeforeAnyRequest()
    {
        // arrange
        var provider = Provider(configure: options => options with { DownloadAllowedHosts = ["subtitles.example.test"] });

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        server.LogEntries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://127.0.0.1")]
    [InlineData("not a url")]
    [InlineData("https://127.0.0.1:65535/base")]
    public async Task SearchAsync_NonHttpsOrUnusableBaseUrl_FailsBeforeAnyRequest(string baseUrl)
    {
        // arrange
        var provider = Provider(configure: options => options with { BaseUrl = baseUrl });

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        server.LogEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_TooManyRequestsWithDeltaSeconds_PropagatesTheRetryAfterDelay()
    {
        // arrange
        server.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "42"));
        var provider = Provider();

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        var quota = await Should.ThrowAsync<SubtitleQuotaExceededException>(act);
        quota.RetryAfter.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public async Task SearchAsync_TooManyRequestsWithHttpDate_PropagatesTheRemainingDelay()
    {
        // arrange
        var retryAt = Now.AddMinutes(3);
        server.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(429)
                .WithHeader("Retry-After", retryAt.ToString("R", CultureInfo.InvariantCulture)));
        var provider = Provider();

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        var quota = await Should.ThrowAsync<SubtitleQuotaExceededException>(act);
        quota.RetryAfter.ShouldBe(TimeSpan.FromMinutes(3));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("soon")]
    public async Task SearchAsync_TooManyRequestsWithoutAUsableRetryAfter_OmitsTheDelay(string? retryAfter)
    {
        // arrange
        var response = Response.Create().WithStatusCode(429);
        if (retryAfter is not null)
        {
            response = response.WithHeader("Retry-After", retryAfter);
        }

        server.Given(Request.Create().WithPath(SearchPath).UsingPost()).RespondWith(response);
        var provider = Provider();

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        var quota = await Should.ThrowAsync<SubtitleQuotaExceededException>(act);
        quota.RetryAfter.ShouldBeNull();
    }

    [Theory]
    [InlineData(302)]
    [InlineData(307)]
    [InlineData(308)]
    public async Task SearchAsync_RedirectResponse_FailsAsAProviderFailureWithoutContactingTheDestination(int status)
    {
        // arrange
        using var destination = LoopbackWireMock.StartHttps();
        server.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(status).WithHeader("Location", destination.Urls[0]));
        var provider = Provider();

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        destination.LogEntries.ShouldBeEmpty();
        server.LogEntries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SearchAsync_ServerError_FailsAsAProviderFailureWithoutRetrying()
    {
        // arrange
        server.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(503));
        var provider = Provider();

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        server.LogEntries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SearchAsync_ResponseSlowerThanTheRequestTimeout_FailsAsAProviderTimeout()
    {
        // arrange
        server.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(SearchBody).WithDelay(TimeSpan.FromSeconds(3)));
        var provider = Provider(configure: options => options with { RequestTimeout = TimeSpan.FromMilliseconds(400) });

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderTimeoutException>(act);
    }

    [Fact]
    public async Task SearchAsync_CallerCancellation_PropagatesCancellationRatherThanAProviderTimeout()
    {
        // arrange
        server.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(SearchBody).WithDelay(TimeSpan.FromSeconds(3)));
        var provider = Provider(configure: options => options with { RequestTimeout = TimeSpan.FromSeconds(30) });
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var token = cancellation.Token;

        // act
        var act = () => provider.SearchAsync(new SubtitleSearch(Hash, 4_000_000, null, ["en"]), token);

        // assert
        var thrown = await Should.ThrowAsync<OperationCanceledException>(act);
        thrown.ShouldNotBeOfType<SubtitleProviderTimeoutException>();
    }

    [Fact]
    public async Task DownloadAsync_PostsTheFileIdThenFetchesTheTemporaryLink()
    {
        // arrange
        StubDownload(ValidSrt);
        var provider = Provider();

        // act
        var content = await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        Encoding.UTF8.GetString(content.Content).ShouldBe(ValidSrt);
        DownloadRequestBody().ShouldContain("\"file_id\":101");
        LinkGetRequest().Method.ShouldBe("GET");
    }

    [Fact]
    public async Task DownloadAsync_CalledTwice_RequestsAFreshTemporaryLinkEachTime()
    {
        // arrange
        StubDownload(ValidSrt);
        var provider = Provider();

        // act
        await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);
        await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        Count(DownloadPath).ShouldBe(2);
        Count(LinkPath).ShouldBe(2);
    }

    [Fact]
    public async Task DownloadAsync_LinkOutsideTheAllowlist_FailsWithoutContactingThatHost()
    {
        // arrange
        StubDownloadLinkResponse(
            Response.Create().WithStatusCode(200),
            link: "https://not-allowlisted.example.test/srt/101.srt");
        var provider = Provider();

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        Count(DownloadPath).ShouldBe(1);
        Count(LinkPath).ShouldBe(0);
    }

    [Theory]
    [InlineData("http://example.test/srt/101.srt")]
    [InlineData("file:///C:/library/Heat.srt")]
    [InlineData("/relative/srt/101.srt")]
    public async Task DownloadAsync_UnsafeLinkScheme_FailsBeforeAnyLinkRequest(string link)
    {
        // arrange
        server.Given(Request.Create().WithPath(DownloadPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody($"{{\"link\":\"{link}\"}}"));
        var provider = Provider();

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        server.LogEntries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DownloadAsync_LinkResponseRedirect_FailsWithoutFollowingIt()
    {
        // arrange
        using var destination = LoopbackWireMock.StartHttps();
        StubDownloadLinkResponse(
            Response.Create().WithStatusCode(301).WithHeader("Location", destination.Urls[0]));
        var provider = Provider();

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        destination.LogEntries.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(InvalidPayloads))]
    public async Task DownloadAsync_PayloadFailingTheMinimalSrtCriteria_FailsAsAProviderFailure(string payload)
    {
        // arrange
        StubDownload(payload);
        var provider = Provider();

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
    }

    [Fact]
    public async Task DownloadAsync_SrtWithUtf8Bom_IsAccepted()
    {
        // arrange
        StubDownload(string.Concat("\uFEFF", ValidSrt));
        var provider = Provider();

        // act
        var content = await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        content.Content.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_PayloadWithDotMillisecondSeparators_IsAccepted()
    {
        // arrange
        StubDownload("1\n00:00:01.000 --> 00:00:03.500\ntext\n");
        var provider = Provider();

        // act
        var content = await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        content.Content.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_PayloadLargerThanTheConfiguredBound_FailsAsAProviderFailure()
    {
        // arrange
        var oversized = string.Concat("1\n00:00:01,000 --> 00:00:03,500\n", new string('x', (int)MaxBytes + 1), "\n");
        StubDownload(oversized);
        var provider = Provider();

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
    }

    [Fact]
    public async Task DownloadAsync_RetryableProviderError_IsNotRetriedAutomatically()
    {
        // arrange
        server.Given(Request.Create().WithPath(DownloadPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500));
        var provider = Provider();

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        Count(DownloadPath).ShouldBe(1);
    }

    [Fact]
    public async Task DownloadAsync_TooManyRequests_ReportsTheQuotaWithoutARetry()
    {
        // arrange
        server.Given(Request.Create().WithPath(DownloadPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "9"));
        var provider = Provider();

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        var quota = await Should.ThrowAsync<SubtitleQuotaExceededException>(act);
        quota.RetryAfter.ShouldBe(TimeSpan.FromSeconds(9));
        server.LogEntries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task DownloadAsync_NoResponseWithinTheRequestTimeout_FailsAsAProviderTimeout()
    {
        // arrange
        StubDownloadLinkResponse(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(3)));
        var provider = Provider(configure: options => options with { RequestTimeout = TimeSpan.FromMilliseconds(400) });

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderTimeoutException>(act);
    }

    [Fact]
    public async Task DownloadAsync_NullCandidate_Throws()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => Provider().DownloadAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task SearchAsync_NullSearch_Throws()
    {
        // act
        // ReSharper disable once NullableWarningSuppressionIsUsed deliberate null exercises the guard
        var act = () => Provider().SearchAsync(null!, TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    [Fact]
    public async Task SearchAsync_QuotaHeaders_MapRemainingAndRetryAfterIntoResetAtUtc()
    {
        // arrange
        server.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("X-RateLimit-Remaining", "17")
                .WithHeader("Retry-After", "90")
                .WithBody(SearchBody));
        var provider = Provider();

        // act
        var result = await provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, Title: null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        result.Quota.RemainingDownloads.ShouldBe(17);
        result.Quota.ResetAtUtc.ShouldBe(Now.AddSeconds(90));
    }

    [Fact]
    public async Task SearchAsync_WithoutQuotaMetadata_ReportsAnUnknownQuotaWithoutInventingValues()
    {
        // arrange
        StubSearch(SearchBody);
        var provider = Provider();

        // act
        var result = await provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, Title: null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        result.Quota.ShouldBe(SubtitleQuota.Unknown);
        result.Quota.RemainingDownloads.ShouldBeNull();
        result.Quota.ResetAtUtc.ShouldBeNull();
        result.Quota.IsExhausted.ShouldBeFalse();
    }

    [Fact]
    public async Task SearchAsync_ZeroRemainingDownloads_ReportsAnExhaustedQuota()
    {
        // arrange
        server.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("X-RateLimit-Remaining", "0")
                .WithBody(SearchBody));
        var provider = Provider();

        // act
        var result = await provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, Title: null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        result.Quota.RemainingDownloads.ShouldBe(0);
        result.Quota.IsExhausted.ShouldBeTrue();
    }

    [Fact]
    public async Task SearchAsync_NonNumericRemainingHeader_IsTreatedAsUnknown()
    {
        // arrange
        server.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("X-RateLimit-Remaining", "soon")
                .WithBody(SearchBody));
        var provider = Provider();

        // act
        var result = await provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, Title: null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        result.Quota.RemainingDownloads.ShouldBeNull();
    }

    [Fact]
    public async Task DownloadAsync_QuotaHeaders_AreReportedWithThePayload()
    {
        // arrange
        server.Given(Request.Create().WithPath(DownloadPath).UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("X-RateLimit-Remaining", "3")
                .WithBody($"{{\"link\":\"{BaseUrl + LinkPath}\"}}"));
        server.Given(Request.Create().WithPath(LinkPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(ValidSrt));
        var provider = Provider();

        // act
        var download = await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        download.Quota.RemainingDownloads.ShouldBe(3);
        download.Content.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_SearchPathQuotaIsNotReused_EachResponseMapsItsOwnHeaders()
    {
        // arrange
        StubDownload(ValidSrt);
        var provider = Provider();

        // act
        var download = await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        download.Quota.ShouldBe(SubtitleQuota.Unknown);
    }

    [Fact]
    public async Task DownloadAsync_PayloadOverTheSubtitlesDownloadBound_FailsAsAProviderFailure()
    {
        // arrange
        var oversized = string.Concat("1\n00:00:01,000 --> 00:00:03,500\n", new string('x', 2048), "\n");
        server.Given(Request.Create().WithPath(DownloadPath).UsingPost())
            .RespondWith(Response.Create().WithBody($"{{\"link\":\"{BaseUrl + LinkPath}\"}}"));
        server.Given(Request.Create().WithPath(LinkPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(oversized));
        var provider = Provider(subtitles: new SubtitlesOptions { MaxSubtitleDownloadBytes = 1024 });

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
    }

    private static SubtitleCandidate Candidate() => new(101, "en", DownloadCount: 42, MatchesMovieHash: true);

    private OpenSubtitlesSubtitleProvider Provider(
        Func<SubtitleProviderOptions, SubtitleProviderOptions>? configure = null,
        SubtitlesOptions? subtitles = null)
    {
        var options = new SubtitleProviderOptions
        {
            ApiKey = ApiKey,
            BaseUrl = BaseUrl,
            UserAgent = UserAgent,
            DownloadAllowedHosts = ["127.0.0.1"],
            MaxDownloadBytes = MaxBytes,
            RequestTimeout = RequestTimeout,
        };
        if (configure is not null)
        {
            options = configure(options);
        }

        var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        })
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        disposables.Add(client);
        return new OpenSubtitlesSubtitleProvider(
            client,
            Options.Create(options),
            Options.Create(subtitles ?? new SubtitlesOptions()),
            new FixedTimeProvider(Now));
    }

    private void StubSearch(string body) =>
        server.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(body));

    private void StubDownload(string payload)
    {
        StubDownloadLinkResponse(Response.Create().WithStatusCode(200));
        server.Given(Request.Create().WithPath(LinkPath).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(payload));
    }

    private void StubDownloadLinkResponse(IResponseBuilder response, string? link = null) =>
        server.Given(Request.Create().WithPath(DownloadPath).UsingPost())
            .RespondWith(response.WithBody($"{{\"link\":\"{link ?? BaseUrl + LinkPath}\"}}"));

    private (string Body, string[] ApiKeys, string[] UserAgents) SearchRequest()
    {
        var entries = server.LogEntries.Where(entry => entry.RequestMessage is { Path: SearchPath }).ToArray();
        entries.Length.ShouldBe(1);
        var message = entries[0].RequestMessage.ShouldNotBeNull();
        return (message.Body.ShouldNotBeNull(), HeaderValues(message.Headers, "Api-Key"), HeaderValues(message.Headers, "User-Agent"));
    }

    private (string Body, string Method) LinkGetRequest()
    {
        var entries = server.LogEntries.Where(entry => entry.RequestMessage is { Path: LinkPath }).ToArray();
        entries.Length.ShouldBe(1);
        var message = entries[0].RequestMessage.ShouldNotBeNull();
        return (message.Body ?? string.Empty, message.Method.ShouldNotBeNull());
    }

    private string DownloadRequestBody()
    {
        var entries = server.LogEntries.Where(entry => entry.RequestMessage is { Path: DownloadPath }).ToArray();
        entries.Length.ShouldBe(1);
        return entries[0].RequestMessage.ShouldNotBeNull().Body.ShouldNotBeNull();
    }

    private int Count(string path) => server.LogEntries.Count(entry => entry.RequestMessage?.Path == path);

    private static string[] HeaderValues(IDictionary<string, WireMock.Types.WireMockList<string>>? headers, string name) =>
        headers is not null && headers.TryGetValue(name, out var values) ? [.. values] : [];
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SubtitleProviderCollection
{
    public const string Name = "subtitle-provider";
}
