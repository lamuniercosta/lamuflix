using System;
using System.Collections.Generic;
using System.Globalization;
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
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(nameof(SubtitleEndpointCollection))]
public sealed class SubtitleSearchDownloadEndpointTests(ApiHostFactory factory)
{
    private const int KnownId = 11;
    private const string MovieFolder = "Heat (1995)";
    private const string PrimaryFileName = "Heat.1995.1080p.mkv";
    private const string ProblemDetailsJson = "application/problem+json";
    private const string CacheControlHeader = "Cache-Control";
    private const string PrivateDirective = "private";
    private const string NoStoreDirective = "no-store";
    private const string ReasonExtension = "reason";
    private const string SentinelKey = "sentinel-subtitle-key";
    private const string SearchPath = "/api/v1/subtitles";

    private readonly IMovieCatalog catalog = Substitute.For<IMovieCatalog>();
    private readonly ISubtitleProvider provider = Substitute.For<ISubtitleProvider>();

    private static string SearchRoute(int id, string language) =>
        string.Create(CultureInfo.InvariantCulture, $"/api/movies/{id}/subtitles/search?language={language}");

    [Fact]
    public async Task GetLanguages_ConfiguredPreferences_ReturnTheOrderedCatalogWithPrivateNoStore()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        await using var host = CreateHost(
            library.Root,
            PreferenceSettings("pt-BR", "en"),
            providerStub: false);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync("/api/subtitles/languages", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertFrozenNoStore(response);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        root.GetProperty("defaultLanguage").GetString().ShouldBe("pt-BR");
        var languages = root.GetProperty("languages").EnumerateArray().ToArray();
        languages[0].GetProperty("code").GetString().ShouldBe("pt-BR");
        languages[0].GetProperty("englishName").GetString().ShouldBe("Portuguese (BR)");
        languages[0].GetProperty("nativeName").GetString().ShouldBe("Português (BR)");
        languages[1].GetProperty("code").GetString().ShouldBe("en");
        languages.Select(language => language.GetProperty("code").GetString()).Distinct().Count()
            .ShouldBe(languages.Length);
        var remaining = languages
            .Skip(2)
            .Select(language => language.GetProperty("englishName").GetString().ShouldNotBeNull())
            .ToArray();
        remaining.ShouldBe([.. remaining.OrderBy(name => name, StringComparer.Ordinal)]);
        await provider.DidNotReceiveWithAnyArgs().SearchAsync(
            Arg.Any<SubtitleSearch>(), TestContext.Current.CancellationToken);
        body.ShouldNotContain(SentinelKey);
    }

    [Fact]
    public async Task GetLanguages_EmptyPreferences_DefaultIsNullAndCatalogStaysComplete()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        await using var host = CreateHost(library.Root, ProviderSettings(), providerStub: false);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync("/api/subtitles/languages", ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertFrozenNoStore(response);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        document.RootElement.GetProperty("defaultLanguage").ValueKind.ShouldBe(JsonValueKind.Null);
        document.RootElement.GetProperty("languages").GetArrayLength().ShouldBeGreaterThan(20);
    }

    [Fact]
    public async Task GetSearch_RankedCandidates_ReturnTheFrozenEnvelopeWithPrivateNoStore()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult(
                [
                    new SubtitleCandidate(303, "en", 4, false, "Unrelated.Release-GROUP"),
                    new SubtitleCandidate(101, "en", 4, false, "Heat.1995.1080p.BluRay-GROUP"),
                    new SubtitleCandidate(202, "en", 41, true, "Heat.1995.1080p.WEB-GROUP"),
                ],
                new SubtitleQuota(17, new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero)))));
        await using var host = CreateHost(library.Root, WritableProviderSettings());
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SearchRoute(KnownId, "en"), ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertFrozenNoStore(response);
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        root.EnumerateObject().Select(property => property.Name)
            .ShouldBe(["candidates", "quota", "downloadAvailability"], ignoreOrder: true);
        var candidates = root.GetProperty("candidates").EnumerateArray().ToArray();
        candidates.Select(candidate => candidate.GetProperty("releaseName").GetString()).ShouldBe(
        [
            "Heat.1995.1080p.WEB-GROUP",
            "Heat.1995.1080p.BluRay-GROUP",
            "Unrelated.Release-GROUP",
        ]);
        candidates[0].EnumerateObject().Select(property => property.Name).ShouldBe(
            ["candidateId", "language", "releaseName", "matchPercent", "downloadCount"], ignoreOrder: true);
        candidates[0].GetProperty("matchPercent").GetInt32().ShouldBe(100);
        candidates[0].GetProperty("language").GetString().ShouldBe("en");
        candidates[0].GetProperty("downloadCount").GetInt32().ShouldBe(41);
        var candidateIds = candidates
            .Select(candidate => candidate.GetProperty("candidateId").GetString().ShouldNotBeNull())
            .ToArray();
        candidateIds.Distinct(StringComparer.Ordinal).Count().ShouldBe(3);
        candidateIds.ShouldAllBe(candidateId => candidateId.Length == 32);
        var quota = root.GetProperty("quota");
        quota.GetProperty("remainingDownloads").GetInt32().ShouldBe(17);
        quota.GetProperty("resetAtUtc").ValueKind.ShouldBe(JsonValueKind.String);
        var availability = root.GetProperty("downloadAvailability");
        availability.GetProperty("enabled").GetBoolean().ShouldBeTrue();
        availability.GetProperty("reason").GetString().ShouldBeEmpty();
        body.ShouldNotContain(SentinelKey);
        body.ShouldNotContain(library.Root);
        body.ShouldNotContain("file_id");
    }

    [Fact]
    public async Task GetSearch_UnknownQuota_KeepsTheQuotaObjectWithNullFields()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult([], SubtitleQuota.Unknown)));
        await using var host = CreateHost(library.Root, WritableProviderSettings());
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SearchRoute(KnownId, "en"), ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertFrozenNoStore(response);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        document.RootElement.GetProperty("candidates").GetArrayLength().ShouldBe(0);
        var quota = document.RootElement.GetProperty("quota");
        quota.GetProperty("remainingDownloads").ValueKind.ShouldBe(JsonValueKind.Null);
        quota.GetProperty("resetAtUtc").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetSearch_ReadOnlyLibrary_StillSearchesAndReportsDisabledDownloadAvailability()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult(
                [new SubtitleCandidate(101, "en", 3, false, "Heat.1995.1080p-GROUP")],
                SubtitleQuota.Unknown)));
        var settings = WritableProviderSettings();
        settings[$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.SaveEnabled)}"] = "false";
        await using var host = CreateHost(library.Root, settings);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SearchRoute(KnownId, "en"), ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AssertFrozenNoStore(response);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        document.RootElement.GetProperty("candidates").GetArrayLength().ShouldBe(1);
        var availability = document.RootElement.GetProperty("downloadAvailability");
        availability.GetProperty("enabled").GetBoolean().ShouldBeFalse();
        availability.GetProperty("reason").GetString().ShouldBe("read_only_library");
    }

    [Fact]
    public async Task GetSearch_EmptyPreferencesWithExplicitLanguage_ReturnsCandidatesWithoutAConflict()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new SubtitleSearchResult([], SubtitleQuota.Unknown)));
        await using var host = CreateHost(library.Root, WritableProviderSettings(preferences: []));
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SearchRoute(KnownId, "pt-BR"), ct);

        // assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await provider.ReceivedWithAnyArgs(1).SearchAsync(
            Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSearch_MissingLanguageParameter_Returns400FromQueryBinding()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, WritableProviderSettings());
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"/api/movies/{KnownId}/subtitles/search"),
            ct);

        // assert
        var body = await response.Content.ReadAsStringAsync(ct);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("status").GetInt32().ShouldBe((int)HttpStatusCode.BadRequest);
        document.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(library.Root);
        await provider.DidNotReceiveWithAnyArgs().SearchAsync(
            Arg.Any<SubtitleSearch>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetSearch_EmptyLanguage_Returns422FromTheValidationDecorator()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, WritableProviderSettings());
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"/api/movies/{KnownId}/subtitles/search?language="),
            ct);

        // assert
        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, "invalid_request", library.Root);
        await provider.DidNotReceiveWithAnyArgs().SearchAsync(
            Arg.Any<SubtitleSearch>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetSearch_UnsupportedLanguage_Returns400FromTheDomainPrecheck()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, WritableProviderSettings());
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SearchRoute(KnownId, "xx-YY"), ct);

        // assert
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "invalid_request", library.Root);
        await provider.DidNotReceiveWithAnyArgs().SearchAsync(
            Arg.Any<SubtitleSearch>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetSearch_MissingMovie_Returns404MovieNotFound()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        catalog.GetDetailsAsync(Arg.Any<MovieId>(), Arg.Any<CancellationToken>()).Returns((MovieDetails?)null);
        await using var host = CreateHost(library.Root, WritableProviderSettings());
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SearchRoute(KnownId, "en"), ct);

        // assert
        await AssertProblemAsync(response, HttpStatusCode.NotFound, "movie_not_found", library.Root);
    }

    [Fact]
    public async Task GetSearch_MissingProviderConfiguration_Returns409ProviderNotConfigured()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        await using var host = CreateHost(library.Root, providerSettings: null);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SearchRoute(KnownId, "en"), ct);

        // assert
        await AssertProblemAsync(response, HttpStatusCode.Conflict, "provider_not_configured", library.Root);
        await provider.DidNotReceiveWithAnyArgs().SearchAsync(
            Arg.Any<SubtitleSearch>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task GetSearch_ProviderRateLimit_Returns429SearchRateLimitedRatherThanQuotaCopy()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SubtitleSearchResult>(
                new SubtitleQuotaExceededException(TimeSpan.FromSeconds(30))));
        await using var host = CreateHost(library.Root, WritableProviderSettings());
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SearchRoute(KnownId, "en"), ct);

        // assert
        await AssertProblemAsync(response, HttpStatusCode.TooManyRequests, "search_rate_limited", library.Root);
        response.Headers.RetryAfter.ShouldNotBeNull().Delta.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task GetSearch_ProviderFailure_Returns502ProviderUnavailable()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SubtitleSearchResult>(
                new SubtitleProviderFailureException("The subtitle provider returned an unusable search response.")));
        await using var host = CreateHost(library.Root, WritableProviderSettings());
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SearchRoute(KnownId, "en"), ct);

        // assert
        await AssertProblemAsync(response, HttpStatusCode.BadGateway, "provider_unavailable", library.Root);
    }

    [Fact]
    public async Task GetSearch_ProviderTimeout_Returns504ProviderTimeout()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SubtitleSearchResult>(new SubtitleProviderTimeoutException()));
        await using var host = CreateHost(library.Root, WritableProviderSettings());
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SearchRoute(KnownId, "en"), ct);

        // assert
        await AssertProblemAsync(response, HttpStatusCode.GatewayTimeout, "provider_timeout", library.Root);
    }

    [Fact]
    public async Task GetSearch_CallerCancellation_IsNotReportedAsAProviderTimeout()
    {
        // arrange
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        provider.SearchAsync(Arg.Any<SubtitleSearch>(), Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                var cancellationToken = callInfo.Arg<CancellationToken>();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new SubtitleSearchResult([], SubtitleQuota.Unknown);
            });
        await using var host = CreateHost(library.Root, WritableProviderSettings());
        using var client = host.CreateClient();
        using var cancellation = new CancellationTokenSource();

        // act
        var request = client.GetAsync(SearchRoute(KnownId, "en"), cancellation.Token);
        await cancellation.CancelAsync();
        var act = async () => await request;

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task GetSearch_ProviderHttpRateLimit_Returns429SearchRateLimitedEndToEnd()
    {
        // arrange
        var ct = TestContext.Current.CancellationToken;
        using var library = new TemporaryLibrary();
        library.WriteMoviePrimary(MovieFolder, PrimaryFileName);
        DetailsReturning(KnownDetails(MovieFolder));
        using var stub = LoopbackWireMock.StartHttps();
        stub.Given(Request.Create().WithPath(SearchPath).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "12"));
        await using var host = CreateHost(
            library.Root,
            ProviderSettings(stub.Urls[0]),
            providerStub: false);
        using var client = host.CreateClient();

        // act
        var response = await client.GetAsync(SearchRoute(KnownId, "en"), ct);

        // assert
        await AssertProblemAsync(response, HttpStatusCode.TooManyRequests, "search_rate_limited", library.Root);
        stub.LogEntries.Count.ShouldBeGreaterThan(0);
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedReason,
        string forbiddenPath)
    {
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(expectedStatus);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe(ProblemDetailsJson);
        AssertFrozenNoStore(response);
        using var document = JsonDocument.Parse(body);
        var problem = document.RootElement;
        problem.GetProperty("status").GetInt32().ShouldBe((int)expectedStatus);
        problem.GetProperty(ReasonExtension).GetString().ShouldBe(expectedReason);
        problem.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.ShouldNotContain(forbiddenPath);
        body.ShouldNotContain(SentinelKey);
        body.ShouldNotContain("stackTrace");
    }

    private static void AssertFrozenNoStore(HttpResponseMessage response)
    {
        var values = response.Headers.GetValues(CacheControlHeader).ShouldHaveSingleItem();
        var directives = values.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        directives.ShouldContain(PrivateDirective);
        directives.ShouldContain(NoStoreDirective);
        directives.Length.ShouldBe(2);
    }

    private void DetailsReturning(MovieDetails details) =>
        catalog.GetDetailsAsync(new MovieId(KnownId), Arg.Any<CancellationToken>()).Returns(details);

    private static Dictionary<string, string?> PreferenceSettings(params string[] preferences) =>
        Preferences(ProviderSettings(), preferences);

    private static Dictionary<string, string?> WritableProviderSettings(
        string[]? preferences = null,
        string? baseUrl = null) =>
        Preferences(ProviderSettings(baseUrl), preferences);

    private static Dictionary<string, string?> Preferences(
        Dictionary<string, string?> settings,
        string[]? preferences)
    {
        foreach (var preference in preferences ?? ["en"])
        {
            settings[$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.PreferredLanguages)}:{settings.Count}"] =
                preference;
        }

        return settings;
    }

    private static Dictionary<string, string?> ProviderSettings(string? baseUrl = null) =>
        new(StringComparer.Ordinal)
        {
            [$"{SubtitlesOptions.SectionName}:{nameof(SubtitlesOptions.SaveEnabled)}"] = "true",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.ApiKey)}"] = SentinelKey,
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.UserAgent)}"] = "LamuFlix/test",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.BaseUrl)}"] =
                baseUrl ?? "https://api.opensubtitles.invalid",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.DownloadAllowedHosts)}:0"] =
                "127.0.0.1",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.MaxDownloadBytes)}"] = "65536",
            [$"{SubtitleProviderOptions.SectionName}:{nameof(SubtitleProviderOptions.RequestTimeout)}"] = "00:00:05",
        };

    private WebApplicationFactory<Program> CreateHost(
        string libraryRoot,
        IReadOnlyDictionary<string, string?>? providerSettings = null,
        bool providerStub = true) =>
        factory.WithWebHostBuilder(builder =>
        {
            var settings = new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["Features:LocalPlay"] = "false",
                [$"{LibraryOptions.SectionName}:{nameof(LibraryOptions.RootPath)}"] = libraryRoot,
            };
            foreach (var setting in providerSettings ?? new Dictionary<string, string?>())
            {
                settings[setting.Key] = setting.Value;
            }

            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings));
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(catalog);
                if (providerStub)
                {
                    services.AddSingleton(provider);
                }
                else
                {
                    services.AddSingleton<ISubtitleProvider>(CreateTestSubtitleProvider);
                }
            });
        });

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
            services.GetRequiredService<IOptions<SubtitlesOptions>>(),
            services.GetRequiredService<TimeProvider>());
    }

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
            Root = Path.Combine(Path.GetTempPath(), "lamuflix-subtitle-search-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void WriteMoviePrimary(string movieFolder, string fileName) =>
            WriteMovieFile(movieFolder, fileName, [0x1A, 0x45, 0xDF, 0xA3]);

        private void WriteMovieFile(string movieFolder, string fileName, byte[] contents)
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
