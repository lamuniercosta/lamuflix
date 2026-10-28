using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Pipeline;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(SecondaryMetadataProviderCollection.Name)]
public sealed class SecondaryMetadataProviderLookupTests(SecondaryMetadataProviderProbe probe)
    : IClassFixture<SecondaryMetadataProviderProbe>, IAsyncLifetime
{
    private const string UnauthorizedAdvice = "TMDb API key";

    public static TheoryData<int> OtherClientErrors => new() { 400, 403, 410 };

    public static TheoryData<int> TransientServerErrors => new() { 500, 502, 503, 504, 408 };

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindAsync_RecordedSearchAndDetailsFixtures_ReturnsFound()
    {
        // arrange
        probe.StubFoundLookup();
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
    }

    [Fact]
    public async Task FindAsync_EmptySearchResults_ReturnsNotFound()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "empty.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
        probe.RequestPaths.ShouldBe(["/3/search/movie"]);
    }

    [Fact]
    public async Task FindAsync_NoQualifyingSearchHit_ReturnsNotFound()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "no-qualifying-hit.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
    }

    [Fact]
    public async Task FindAsync_ManualImdbId_PrefersFindAndSkipsTheTitleSearch()
    {
        // arrange
        probe.StubFind("tt0133093", SecondaryMetadataProviderProbe.Fixture("Find", "found.json"));
        probe.StubDetails(603, SecondaryMetadataProviderProbe.Fixture("Details", "found.json"));
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "no-qualifying-hit.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindByImdbAsync(services, "tt0133093", TestContext.Current.CancellationToken);

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
        probe.RequestPaths.ShouldBe(["/3/find/tt0133093", "/3/movie/603"]);
        probe.QueryValues("external_source").ShouldBe(["imdb_id"]);
    }

    [Fact]
    public async Task FindAsync_ManualImdbIdWithNoMovieResults_ReturnsNotFoundWithoutATitleFallback()
    {
        // arrange
        probe.StubFind("tt0133093", SecondaryMetadataProviderProbe.Fixture("Find", "empty.json"));
        probe.StubFoundLookup();
        var services = probe.BuildServices();

        // act
        var result = await FindByImdbAsync(services, "tt0133093", TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
        probe.RequestPaths.ShouldBe(["/3/find/tt0133093"]);
    }

    [Fact]
    public async Task FindAsync_WithReleaseYear_SendsThePrimaryReleaseYear()
    {
        // arrange
        probe.StubFoundLookup();
        var services = probe.BuildServices();

        // act
        await FindAsync(services, "The Matrix", 1999, TestContext.Current.CancellationToken);

        // assert
        probe.QueryValues("primary_release_year").ShouldBe(["1999"]);
    }

    [Fact]
    public async Task FindAsync_TiedScores_SelectsTheLowestMovieId()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "duplicates.json"));
        probe.StubDetails(603, SecondaryMetadataProviderProbe.Fixture("Details", "found.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
        probe.RequestPaths.ShouldBe(["/3/search/movie", "/3/movie/603"]);
    }

    [Fact]
    public async Task FindAsync_AnyLookup_SendsTheApiKeyAsAVersionThreeQueryParameter()
    {
        // arrange
        probe.StubFoundLookup();
        var services = probe.BuildServices();

        // act
        await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        probe.QueryValues("api_key").ShouldBe([SecondaryMetadataProviderProbe.SentinelApiKey]);
        probe.HasQuery("apikey").ShouldBeFalse();
        foreach (var entry in probe.Server.LogEntries)
        {
            var headers = entry.RequestMessage?.Headers;
            if (headers is not null)
            {
                headers.Keys.ShouldNotContain(key =>
                    string.Equals(key, "Authorization", StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Fact]
    public async Task FindAsync_SlashlessLoopbackBaseUrl_ResolvesEveryPathUnderTheApiVersion()
    {
        // arrange
        probe.StubFoundLookup();
        var services = probe.BuildServices();

        // act
        await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        probe.RequestPaths.ShouldBe(["/3/search/movie", "/3/movie/603"]);
    }

    [Fact]
    public async Task FindAsync_SearchHitWithInvalidId_ReturnsFailedInvalidResponseWithoutDetailsRequest()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "invalid-id.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        probe.RequestPaths.ShouldBe(["/3/search/movie"]);
    }

    [Fact]
    public async Task FindAsync_FindHitWithMissingId_ReturnsFailedInvalidResponseWithoutDetailsRequest()
    {
        // arrange
        probe.StubFind("tt0133093", SecondaryMetadataProviderProbe.Fixture("Find", "missing-id.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindByImdbAsync(services, "tt0133093", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        probe.RequestPaths.ShouldBe(["/3/find/tt0133093"]);
    }

    [Fact]
    public async Task FindAsync_Unauthorized_ReturnsFailedInvalidResponseAfterOneRequest()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 401);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        probe.RequestCount.ShouldBe(1);
        var entry = await CheckAsync(services, SecondaryMetadataProviderProbe.CheckName);
        entry.Status.ShouldBe(HealthStatus.Unhealthy);
        entry.Description.ShouldNotBeNull().ShouldContain(UnauthorizedAdvice);
    }

    [Fact]
    public async Task FindAsync_AlwaysRateLimited_ReturnsFailedRateLimited()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 429);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.RateLimited);
    }

    [Theory]
    [MemberData(nameof(TransientServerErrors))]
    public async Task FindAsync_TransientServerStatus_ReturnsFailedProviderUnavailable(int statusCode)
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: statusCode);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public async Task FindAsync_DetailsNotFound_ReturnsNotFound()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "found.json"));
        probe.StubDetails(603, SecondaryMetadataProviderProbe.Fixture("Errors", "not-found.json"), statusCode: 404);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
    }

    [Fact]
    public async Task FindAsync_FindNotFound_ReturnsNotFound()
    {
        // arrange
        probe.StubFind("tt0133093", string.Empty, statusCode: 404);
        var services = probe.BuildServices();

        // act
        var result = await FindByImdbAsync(services, "tt0133093", TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
    }

    [Fact]
    public async Task FindAsync_DetailsNoResultStatus_ReturnsNotFound()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "found.json"));
        probe.StubDetails(603, SecondaryMetadataProviderProbe.Fixture("Errors", "no-result.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
    }

    [Fact]
    public async Task FindAsync_FindNoResultStatus_ReturnsNotFound()
    {
        // arrange
        probe.StubFind("tt0133093", SecondaryMetadataProviderProbe.Fixture("Errors", "no-result.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindByImdbAsync(services, "tt0133093", TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.NotFound>();
    }

    [Fact]
    public async Task FindAsync_OkErrorBodyThatIsNotANoResult_ReturnsFailedInvalidResponse()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "found.json"));
        probe.StubDetails(603, SecondaryMetadataProviderProbe.Fixture("Errors", "invalid-request.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_DetailsSuccessFalseWithTitle_ReturnsFailedInvalidResponse()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "found.json"));
        probe.StubDetails(603, SecondaryMetadataProviderProbe.Fixture("Details", "success-false.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        probe.RequestPaths.ShouldBe(["/3/search/movie", "/3/movie/603"]);
    }

    [Fact]
    public async Task FindAsync_FindSuccessFalseWithEmptyResults_ReturnsFailedInvalidResponse()
    {
        // arrange
        probe.StubFind("tt0133093", SecondaryMetadataProviderProbe.Fixture("Find", "success-false-empty.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindByImdbAsync(services, "tt0133093", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        probe.RequestPaths.ShouldBe(["/3/find/tt0133093"]);
    }

    [Fact]
    public async Task FindAsync_SearchNotFound_ReturnsFailedInvalidResponse()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 404);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
        probe.RequestPaths.ShouldBe(["/3/search/movie"]);
    }

    [Fact]
    public async Task FindAsync_MalformedJson_ReturnsFailedInvalidResponse()
    {
        // arrange
        probe.StubSearch("{ \"page\": 1, ");
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Theory]
    [MemberData(nameof(OtherClientErrors))]
    public async Task FindAsync_OtherClientError_ReturnsFailedInvalidResponse(int statusCode)
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: statusCode);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.InvalidResponse);
    }

    [Fact]
    public async Task FindAsync_CallerCancellationDuringTheRequest_PropagatesWithoutRecordingAProviderFailure()
    {
        // arrange
        probe.StubSearch(SecondaryMetadataProviderProbe.Fixture("Search", "found.json"));
        probe.StubDetails(603, SecondaryMetadataProviderProbe.Fixture("Details", "found.json"), delay: TimeSpan.FromSeconds(2));
        var services = probe.BuildServices();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(350));
        var provider = services.GetRequiredService<TmdbMetadataProvider>();

        // act
        var pending = provider.FindAsync(new MetadataLookup("The Matrix", null), cancellation.Token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(() => pending);
        var entry = await CheckAsync(services, SecondaryMetadataProviderProbe.CheckName);
        entry.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task FindAsync_Disabled_MakesNoHttpRequestAndReturnsFailedProviderUnavailable()
    {
        // arrange
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = "false",
        });

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        probe.RequestCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FindAsync_BlankApiKey_MakesNoHttpRequestAndReturnsFailedProviderUnavailable(bool enabled)
    {
        // arrange
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = enabled ? "true" : "false",
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.ApiKey)}"] = string.Empty,
        });

        // act
        var result = await FindAsync(services, "The Matrix", TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        probe.RequestCount.ShouldBe(0);
    }

    [Fact]
    public async Task FindAsync_AcrossEveryOutcome_NeverLeaksTheApiKeyIntoLogsStateExceptionsTelemetryOrHealth()
    {
        // arrange
        var services = probe.BuildServices();
        var sentinel = SecondaryMetadataProviderProbe.SentinelApiKey;
        var escaped = Uri.EscapeDataString(sentinel);
        using var activities = new SecondaryActivityCapture(
            TelemetryConstants.ActivitySourceName,
            "System.Net.Http");
        var ct = TestContext.Current.CancellationToken;

        // act
        probe.StubFoundLookup();
        await FindAsync(services, "The Matrix", ct);

        probe.Reset();
        probe.StubSearch(string.Empty, statusCode: 401);
        await FindAsync(services, "The Matrix", ct);

        probe.Reset();
        probe.StubSearch(string.Empty, statusCode: 503);
        await FindAsync(services, "The Matrix", ct);

        probe.Reset();
        probe.StubSearch(string.Empty, statusCode: 429);
        await FindAsync(services, "The Matrix", ct);

        probe.Reset();
        probe.StubSearch("{ \"page\": 1, ");
        await FindAsync(services, "The Matrix", ct);

        var secondary = await CheckAsync(services, SecondaryMetadataProviderProbe.CheckName);
        var primary = await CheckAsync(services, SecondaryMetadataProviderProbe.PrimaryCheckName);

        // assert
        var logCorpus = string.Join("\n", probe.Logs.Entries.SelectMany(LogFragments));
        logCorpus.ShouldNotContain(sentinel);
        logCorpus.ShouldNotContain(escaped);
        probe.Logs.Entries.ShouldContain(entry =>
            entry.CategoryName.StartsWith("LamuFlix.Infrastructure.Adapters.TmdbMetadataProvider", StringComparison.Ordinal));
        probe.Logs.Entries.ShouldContain(entry =>
            entry.CategoryName.StartsWith("System.Net.Http.HttpClient.", StringComparison.Ordinal));

        var activityCorpus = string.Join("\n", activities.Activities.SelectMany(ActivityFragments));
        activityCorpus.ShouldNotContain(sentinel);
        activityCorpus.ShouldNotContain(escaped);

        (secondary.Description ?? string.Empty).ShouldNotContain(sentinel);
        (primary.Description ?? string.Empty).ShouldNotContain(sentinel);
    }

    private static async Task<MetadataLookupResult> FindAsync(
        IServiceProvider services,
        string title,
        CancellationToken ct) =>
        await FindAsync(services, title, year: null, ct);

    private static async Task<MetadataLookupResult> FindAsync(
        IServiceProvider services,
        string title,
        int? year,
        CancellationToken ct)
    {
        var releaseYear = year is null ? null : new ReleaseYear(year.Value, SecondaryMetadataProviderProbe.FixedNow);
        return await services
            .GetRequiredService<TmdbMetadataProvider>()
            .FindAsync(new MetadataLookup(title, releaseYear), ct);
    }

    private static async Task<MetadataLookupResult> FindByImdbAsync(
        IServiceProvider services,
        string imdbId,
        CancellationToken ct) =>
        await services
            .GetRequiredService<TmdbMetadataProvider>()
            .FindAsync(new MetadataLookup("ignored title", null, new ImdbId(imdbId)), ct);

    private static async Task<HealthReportEntry> CheckAsync(IServiceProvider services, string name) =>
        (await services
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registration => registration.Name == name, TestContext.Current.CancellationToken))
        .Entries[name];

    private static MovieMetadata Metadata(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;

    private static EnrichmentFailureCategory Category(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category;

    private static IEnumerable<string> LogFragments(MetadataProviderProbe.CapturedLog entry)
    {
        yield return entry.CategoryName;
        yield return entry.Message;
        yield return entry.ExceptionText;
        foreach (var pair in entry.State)
        {
            yield return pair.Key;
            yield return pair.Value?.ToString() ?? string.Empty;
        }

        foreach (var scope in entry.Scopes)
        {
            yield return scope.ToString() ?? string.Empty;
            if (scope is not IEnumerable<KeyValuePair<string, object?>> pairs)
            {
                continue;
            }

            foreach (var pair in pairs)
            {
                yield return pair.Key;
                yield return pair.Value?.ToString() ?? string.Empty;
            }
        }
    }

    private static IEnumerable<string> ActivityFragments(System.Diagnostics.Activity activity)
    {
        yield return activity.DisplayName;
        yield return activity.OperationName;
        yield return activity.StatusDescription ?? string.Empty;
        foreach (var tag in activity.TagObjects)
        {
            yield return tag.Key;
            yield return tag.Value?.ToString() ?? string.Empty;
        }

        foreach (var activityEvent in activity.Events)
        {
            yield return activityEvent.Name;
            foreach (var tag in activityEvent.Tags)
            {
                yield return tag.Key;
                yield return tag.Value?.ToString() ?? string.Empty;
            }
        }
    }
}
