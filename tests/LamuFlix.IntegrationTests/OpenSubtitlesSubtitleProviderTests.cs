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
    private const string SearchPath = OpenSubtitlesEmulator.SearchPath;
    private const string DownloadPath = OpenSubtitlesEmulator.DownloadPath;
    private const string ApiKeyHeader = "Api-Key";
    private const string UserAgentHeader = "User-Agent";
    private const string AcceptHeader = "Accept";
    private const string JsonMediaType = "application/json";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    private static readonly DateTimeOffset Now = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    private static readonly DateTimeOffset ResetAt = new(2026, 3, 4, 23, 59, 59, 999, TimeSpan.Zero);

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

    private readonly OpenSubtitlesEmulator emulator = new(ApiKey, Scenario());
    private readonly List<IDisposable> disposables = [];

    public void Dispose()
    {
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }

        emulator.Dispose();
    }

    private static OpenSubtitlesScenario Scenario() => new()
    {
        Subtitles =
        [
            new OpenSubtitlesSubtitle(101, "en", 42, "Dune.2021.1080p.BluRay.x264-GROUP", "Movie", "Dune", MovieHashMatch: true),
            new OpenSubtitlesSubtitle(201, "pt-BR", 7, "Dune.2021.WEB-DL.H264", "Movie", "Dune"),
            new OpenSubtitlesSubtitle(301, "en", 9, "Go.On.S01E01.720p", "Episode", "Go On"),
        ],
    };

    [Fact]
    public async Task SearchAsync_HashSearch_SendsCanonicalQueryWithApiKeyAndUserAgent()
    {
        // arrange
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
        var request = SearchRequest();
        HeaderValues(request.Headers, ApiKeyHeader).ShouldBe([ApiKey]);
        HeaderValues(request.Headers, UserAgentHeader).ShouldBe([UserAgent]);
        request.RawQuery.TrimStart('?').ShouldBe(OpenSubtitlesEmulator.CanonicalQuery(request.RawQuery.TrimStart('?')));
        QueryValue(request, "moviehash").ShouldBe(Hash);
        QueryValue(request, "languages").ShouldBe("en,pt-br");
        QueryValue(request, "type").ShouldBe("movie");
        QueryValue(request, "query").ShouldBeNull();
    }

    [Fact]
    public async Task SearchAsync_TitleSearch_SendsTheLowercaseCanonicalTitleAndTypeFilter()
    {
        // arrange
        var provider = Provider();

        // act
        await provider.SearchAsync(
            new SubtitleSearch(MovieHash: null, 900, "Blade Runner", ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        var request = SearchRequest();
        request.RawQuery.TrimStart('?').ShouldBe(OpenSubtitlesEmulator.CanonicalQuery(request.RawQuery.TrimStart('?')));
        QueryValue(request, "query").ShouldBe("blade runner");
        QueryValue(request, "type").ShouldBe("movie");
        QueryValue(request, "moviehash").ShouldBeNull();
    }

    [Fact]
    public async Task SearchAsync_TitleSearch_ExcludesEpisodeResults()
    {
        // arrange
        var provider = Provider();

        // act
        var result = await provider.SearchAsync(
            new SubtitleSearch(MovieHash: null, 900, "Go On", ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        result.Candidates.Select(candidate => candidate.FileId).ShouldNotContain(301);
    }

    [Fact]
    public async Task SearchAsync_RejectedApiKey_FailsWithoutExposingTheKey()
    {
        // arrange
        const string wrongKey = "wrong-key-value";
        var provider = Provider(configure: options => options with { ApiKey = wrongKey });

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        var failure = await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        failure.Message.ShouldNotContain(wrongKey);
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
        emulator.Api.LogEntries.ShouldBeEmpty();
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
        emulator.Api.LogEntries.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_TooManyRequestsWithDeltaSeconds_PropagatesTheRetryAfterDelay()
    {
        // arrange
        emulator.OverrideSearch(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "42"));
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
        emulator.OverrideSearch(Response.Create().WithStatusCode(429)
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

        emulator.OverrideSearch(response);
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
        emulator.OverrideSearch(Response.Create().WithStatusCode(status).WithHeader("Location", destination.Urls[0]));
        var provider = Provider();

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        destination.LogEntries.ShouldBeEmpty();
        emulator.Api.LogEntries.Count().ShouldBe(1);
    }


    [Fact]
    public async Task SearchAsync_ServerError_FailsAsAProviderFailureWithoutRetrying()
    {
        // arrange
        emulator.OverrideSearch(Response.Create().WithStatusCode(503));
        var provider = Provider();

        // act
        var act = () => provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        emulator.Api.LogEntries.Count().ShouldBe(1);
    }

    [Fact]
    public async Task SearchAsync_ResponseSlowerThanTheRequestTimeout_FailsAsAProviderTimeout()
    {
        // arrange
        emulator.OverrideSearch(Response.Create().WithStatusCode(200).WithBody("{\"data\":[]}").WithDelay(TimeSpan.FromSeconds(3)));
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
        emulator.OverrideSearch(Response.Create().WithStatusCode(200).WithBody("{\"data\":[]}").WithDelay(TimeSpan.FromSeconds(3)));
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
    public async Task DownloadAsync_PostsTheFileIdThenFetchesTheTemporaryLinkWithoutTheApiKey()
    {
        // arrange
        var provider = Provider();

        // act
        var content = await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        Encoding.UTF8.GetString(content.Content).ShouldBe(OpenSubtitlesScenario.DefaultSrt);
        DownloadRequestBody().ShouldContain("\"file_id\":101");
        HeaderValues(Only(emulator.Api, DownloadPath).Headers, AcceptHeader).ShouldBe([JsonMediaType]);
        var link = LinkGetRequest();
        link.Method.ShouldBe("GET");
        HeaderValues(link.Headers, ApiKeyHeader).ShouldBeEmpty();
        HeaderValues(link.Headers, AcceptHeader).ShouldBeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_CalledTwice_RequestsAFreshTemporaryLinkEachTime()
    {
        // arrange
        var provider = Provider();

        // act
        await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);
        await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        ApiCount(DownloadPath).ShouldBe(2);
        emulator.Links.LogEntries.Count().ShouldBe(2);
    }

    [Fact]
    public async Task DownloadAsync_LinkOutsideTheAllowlist_FailsWithoutContactingThatHost()
    {
        // arrange
        OverrideDownloadWithLink("https://not-allowlisted.example.test/srt/101.srt");
        var provider = Provider();

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        var thrown = await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        thrown.RefusedLinkHost.ShouldBe("not-allowlisted.example.test");
        thrown.Message.ShouldNotContain("/srt/101.srt");
        ApiCount(DownloadPath).ShouldBe(1);
        emulator.Links.LogEntries.Count().ShouldBe(0);
    }

    [Theory]
    [InlineData("http://example.test/srt/101.srt")]
    [InlineData("file:///C:/library/Heat.srt")]
    [InlineData("/relative/srt/101.srt")]
    public async Task DownloadAsync_UnsafeLinkScheme_FailsBeforeAnyLinkRequest(string link)
    {
        // arrange
        OverrideDownloadWithLink(link);
        var provider = Provider();

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        emulator.Api.LogEntries.Count().ShouldBe(1);
        emulator.Links.LogEntries.Count().ShouldBe(0);
    }

    [Fact]
    public async Task DownloadAsync_LinkResponseRedirect_FailsWithoutFollowingIt()
    {
        // arrange
        using var destination = LoopbackWireMock.StartHttps();
        emulator.OverrideLink(Response.Create().WithStatusCode(301).WithHeader("Location", destination.Urls[0]));
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
        emulator.OverrideLink(Response.Create().WithStatusCode(200).WithBody(payload));
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
        emulator.OverrideLink(Response.Create().WithStatusCode(200).WithBody(string.Concat("\uFEFF", OpenSubtitlesScenario.DefaultSrt)));
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
        emulator.OverrideLink(Response.Create().WithStatusCode(200).WithBody("1\n00:00:01.000 --> 00:00:03.500\ntext\n"));
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
        emulator.OverrideLink(Response.Create().WithStatusCode(200).WithBody(oversized));
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
        emulator.OverrideDownload(Response.Create().WithStatusCode(500));
        var provider = Provider();

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        await Should.ThrowAsync<SubtitleProviderFailureException>(act);
        ApiCount(DownloadPath).ShouldBe(1);
    }

    [Fact]
    public async Task DownloadAsync_TooManyRequests_ReportsTheQuotaWithoutARetry()
    {
        // arrange
        emulator.OverrideDownload(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "9"));
        var provider = Provider();

        // act
        var act = () => provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        var quota = await Should.ThrowAsync<SubtitleQuotaExceededException>(act);
        quota.RetryAfter.ShouldBe(TimeSpan.FromSeconds(9));
        emulator.Api.LogEntries.Count().ShouldBe(1);
    }

    [Fact]
    public async Task DownloadAsync_NoResponseWithinTheRequestTimeout_FailsAsAProviderTimeout()
    {
        // arrange
        emulator.OverrideLink(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(3)));
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
    public async Task SearchAsync_RateLimitHeaders_DoNotBecomeADailyQuota()
    {
        // arrange
        emulator.OverrideSearch(Response.Create()
            .WithStatusCode(200)
            .WithHeader("X-RateLimit-Remaining", "17")
            .WithBody("{\"data\":[]}"));
        var provider = Provider();

        // act
        var result = await provider.SearchAsync(
            new SubtitleSearch(Hash, 4_000_000, Title: null, ["en"]),
            TestContext.Current.CancellationToken);

        // assert
        result.Quota.ShouldBe(SubtitleQuota.Unknown);
    }

    [Fact]
    public async Task SearchAsync_WithoutQuotaMetadata_ReportsAnUnknownQuotaWithoutInventingValues()
    {
        // arrange
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
    public async Task DownloadAsync_BodyQuota_IsReportedWithThePayload()
    {
        // arrange
        emulator.OverrideDownload(Response.Create().WithStatusCode(200).WithBody(
            $"{{\"link\":\"{emulator.DownloadLinkFor(101)}\",\"remaining\":3,\"reset_time_utc\":\"2026-03-04T23:59:59.999Z\"}}"));
        var provider = Provider();

        // act
        var download = await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        download.Quota.RemainingDownloads.ShouldBe(3);
        download.Quota.ResetAtUtc.ShouldBe(ResetAt);
        download.Content.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task DownloadAsync_DefaultEmulatorBody_ReportsTheScenarioQuota()
    {
        // arrange
        var provider = Provider();

        // act
        var download = await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        download.Quota.RemainingDownloads.ShouldBe(99);
        download.Quota.ResetAtUtc.ShouldBe(ResetAt);
    }

    [Fact]
    public async Task DownloadAsync_ZeroRemainingInBody_ReportsAnExhaustedQuota()
    {
        // arrange
        emulator.OverrideDownload(Response.Create().WithStatusCode(200).WithBody(
            $"{{\"link\":\"{emulator.DownloadLinkFor(101)}\",\"remaining\":0}}"));
        var provider = Provider();

        // act
        var download = await provider.DownloadAsync(Candidate(), TestContext.Current.CancellationToken);

        // assert
        download.Quota.RemainingDownloads.ShouldBe(0);
        download.Quota.IsExhausted.ShouldBeTrue();
    }

    [Theory]
    [InlineData("{\"link\":\"LINK\"}")]
    [InlineData("{\"link\":\"LINK\",\"remaining\":\"soon\"}")]
    public async Task DownloadAsync_BodyWithoutAUsableQuota_ReportsAnUnknownQuota(string bodyTemplate)
    {
        // arrange
        var body = bodyTemplate.Replace("LINK", emulator.DownloadLinkFor(101), StringComparison.Ordinal);
        emulator.OverrideDownload(Response.Create().WithStatusCode(200).WithBody(body));
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
        emulator.OverrideLink(Response.Create().WithStatusCode(200).WithBody(oversized));
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
            BaseUrl = emulator.BaseUrl,
            UserAgent = UserAgent,
            DownloadAllowedHosts = ["127.0.0.1"],
            DownloadLinkHost = "127.0.0.1",
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
        emulator.OverrideSearch(Response.Create().WithStatusCode(200).WithBody(body));

    private void OverrideDownloadWithLink(string link) =>
        emulator.OverrideDownload(Response.Create().WithStatusCode(200).WithBody($"{{\"link\":\"{link}\"}}"));

    private sealed record Recorded(string Method, string RawQuery, string? Body, IDictionary<string, WireMock.Types.WireMockList<string>>? Headers);

    private static Recorded Only(WireMockServer server, string? path)
    {
        var entries = server.LogEntries.Where(entry => path is null || entry.RequestMessage?.Path == path).ToArray();
        entries.Length.ShouldBe(1);
        var message = entries[0].RequestMessage.ShouldNotBeNull();
        return new Recorded(message.Method, message.RawQuery, message.Body, message.Headers);
    }

    private Recorded SearchRequest() => Only(emulator.Api, SearchPath);

    private Recorded LinkGetRequest() => Only(emulator.Links, null);

    private string DownloadRequestBody() => Only(emulator.Api, DownloadPath).Body.ShouldNotBeNull();

    private static string? QueryValue(Recorded message, string name)
    {
        foreach (var pair in message.RawQuery.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(Uri.UnescapeDataString(parts[0]), name, StringComparison.Ordinal))
            {
                return Uri.UnescapeDataString(parts[1].Replace('+', ' '));
            }
        }

        return null;
    }

    private int ApiCount(string path) => emulator.Api.LogEntries.Count(entry => entry.RequestMessage?.Path == path);

    private static string[] HeaderValues(IDictionary<string, WireMock.Types.WireMockList<string>>? headers, string name) =>
        headers is not null && headers.TryGetValue(name, out var values) ? [.. values] : [];
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SubtitleProviderCollection
{
    public const string Name = "subtitle-provider";
}
