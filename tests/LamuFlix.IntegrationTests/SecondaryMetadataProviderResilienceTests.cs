using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(SecondaryMetadataProviderCollection.Name)]
public sealed class SecondaryMetadataProviderResilienceTests(SecondaryMetadataProviderProbe probe)
    : IClassFixture<SecondaryMetadataProviderProbe>, IAsyncLifetime
{
    private const string OmdbFoundBody = """
        {
          "Response": "True",
          "Title": "Solaris",
          "Year": "1972",
          "Runtime": "167 min",
          "Plot": "A psychologist...",
          "imdbRating": "8.0",
          "imdbID": "tt0069293"
        }
        """;

    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan Tolerance = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan RetryAfterDelay = TimeSpan.FromSeconds(1);

    public ValueTask InitializeAsync()
    {
        probe.Reset();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FindAsync_TransientServerErrorThenSuccess_ReturnsFoundAfterRetries()
    {
        // arrange
        probe.Server
            .Given(Request.Create().WithPath("/3/search/movie").UsingGet())
            .InScenario("transient")
            .WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(503));
        probe.Server
            .Given(Request.Create().WithPath("/3/search/movie").UsingGet())
            .InScenario("transient")
            .WhenStateIs("recovered")
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody(SecondaryMetadataProviderProbe.Fixture("Search", "found.json")));
        probe.StubDetails(603, SecondaryMetadataProviderProbe.Fixture("Details", "found.json"));
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Metadata(result).Title.ShouldBe("The Matrix");
        probe.RequestPaths.ShouldBe(["/3/search/movie", "/3/search/movie", "/3/movie/603"]);
    }

    [Fact]
    public async Task FindAsync_AlwaysServerError_ReturnsFailedProviderUnavailableAfterEveryAttempt()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 500);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        probe.RequestCount.ShouldBe(1 + MaxRetryAttempts);
    }

    [Fact]
    public async Task FindAsync_AlwaysRateLimited_ReturnsFailedRateLimitedAfterEveryAttempt()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 429);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.RateLimited);
        probe.RequestCount.ShouldBe(1 + MaxRetryAttempts);
    }

    [Fact]
    public async Task FindAsync_RetryAfterHeader_WaitsAtLeastTheHeaderDelay()
    {
        // arrange
        probe.Server
            .Given(Request.Create().WithPath("/3/search/movie").UsingGet())
            .InScenario("throttled")
            .WillSetStateTo("allowed")
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "1"));
        probe.Server
            .Given(Request.Create().WithPath("/3/search/movie").UsingGet())
            .InScenario("throttled")
            .WhenStateIs("allowed")
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody(SecondaryMetadataProviderProbe.Fixture("Search", "found.json")));
        probe.StubDetails(603, SecondaryMetadataProviderProbe.Fixture("Details", "found.json"));
        var services = probe.BuildServices();
        var clock = Stopwatch.StartNew();

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        clock.Stop();
        Metadata(result).Title.ShouldBe("The Matrix");
        probe.RequestCount.ShouldBe(3);
        clock.Elapsed.ShouldBeGreaterThanOrEqualTo(RetryAfterDelay - Tolerance);
    }

    [Fact]
    public async Task FindAsync_RetryAfterBeyondTheTotalBudget_ReturnsFailedProviderUnavailableWithoutASecondRequest()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 429, retryAfter: RetryAfterDelay * 30);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        probe.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_ResponseSlowerThanTheAttemptTimeout_ReturnsFailedProviderUnavailable()
    {
        // arrange
        using var slow = LoopbackWireMock.Start();
        slow.Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(2))
                .WithBody(SecondaryMetadataProviderProbe.Fixture("Search", "found.json")));
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.BaseUrl)}"] = string.Concat(slow.Urls[0], "/3"),
        });

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public async Task FindAsync_UnreachableServer_ReturnsFailedProviderUnavailable()
    {
        // arrange
        var dead = LoopbackWireMock.Start();
        var deadUrl = dead.Urls[0];
        dead.Stop();
        dead.Dispose();
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.BaseUrl)}"] = string.Concat(deadUrl, "/3"),
        });

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public async Task FindAsync_WhenTheSecondaryBreakerOpens_LeavesOmdbCallableAndItsBreakerUntouched()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 500);
        probe.StubPath("/", statusCode: 200, body: OmdbFoundBody);
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [SecondaryMetadataProviderProbe.Resilience(nameof(MetadataProviderResilienceOptions.MinimumThroughput))] = "2",
        });

        // act
        var opening = await FindAsync(services, TestContext.Current.CancellationToken);
        var requestsAfterOpening = probe.RequestCount;
        var open = await FindAsync(services, TestContext.Current.CancellationToken);
        var requestsBeforeOmdb = probe.RequestCount;
        var omdb = await services
            .GetRequiredService<IMetadataProvider>()
            .FindAsync(new MetadataLookup("Solaris", null), TestContext.Current.CancellationToken);
        var omdbHealth = await CheckAsync(services, SecondaryMetadataProviderProbe.PrimaryCheckName);

        // assert
        Category(opening).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        Category(open).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        requestsAfterOpening.ShouldBeGreaterThanOrEqualTo(2);
        requestsBeforeOmdb.ShouldBe(requestsAfterOpening);
        probe.RequestCount.ShouldBe(requestsBeforeOmdb + 1);
        Metadata(omdb).Title.ShouldBe("Solaris");
        omdbHealth.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task FindAsync_CancelledDuringTheRequest_ThrowsOperationCanceled()
    {
        // arrange
        probe.StubSearch(
            SecondaryMetadataProviderProbe.Fixture("Search", "found.json"),
            delay: TimeSpan.FromSeconds(2));
        var services = probe.BuildServices();
        var provider = services.GetRequiredService<TmdbMetadataProvider>();

        // act
        var act = async () =>
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            await provider.FindAsync(new MetadataLookup("The Matrix", null), cancellation.Token);
        };

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    [Fact]
    public async Task FindAsync_CancelledDuringTheBackoffWait_ThrowsOperationCanceled()
    {
        // arrange
        probe.StubSearch(string.Empty, statusCode: 500, retryAfter: RetryAfterDelay);
        var services = probe.BuildServices();
        var provider = services.GetRequiredService<TmdbMetadataProvider>();

        // act
        var act = async () =>
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            await provider.FindAsync(new MetadataLookup("The Matrix", null), cancellation.Token);
        };

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    private static async Task<MetadataLookupResult> FindAsync(IServiceProvider services, CancellationToken ct) =>
        await services
            .GetRequiredService<TmdbMetadataProvider>()
            .FindAsync(new MetadataLookup("The Matrix", null), ct);

    private static async Task<HealthReportEntry> CheckAsync(IServiceProvider services, string name) =>
        (await services
            .GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registration => registration.Name == name, TestContext.Current.CancellationToken))
        .Entries[name];

    private static MovieMetadata Metadata(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata;

    private static EnrichmentFailureCategory Category(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category;
}
