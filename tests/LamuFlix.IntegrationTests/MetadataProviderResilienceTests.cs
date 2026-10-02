using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace LamuFlix.IntegrationTests;

[Collection(MetadataProviderCollection.Name)]
public sealed class MetadataProviderResilienceTests(MetadataProviderProbe probe) : IClassFixture<MetadataProviderProbe>, IAsyncLifetime
{
    private const string FoundBody = """
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
    public async Task FindAsync_AlwaysRateLimited_ReturnsFailedRateLimitedAfterEveryAttempt()
    {
        // arrange
        StubStatus(429);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.RateLimited);
        probe.RequestCount.ShouldBe(1 + MaxRetryAttempts);
    }

    [Fact]
    public async Task FindAsync_AlwaysServerError_ReturnsFailedProviderUnavailableAfterEveryAttempt()
    {
        // arrange
        StubStatus(500);
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        probe.RequestCount.ShouldBe(1 + MaxRetryAttempts);
    }

    [Fact]
    public async Task FindAsync_TransientServerErrorThenSuccess_ReturnsFoundAfterExactlyTwoRequests()
    {
        // arrange
        StubTransientThenSuccess();
        var services = probe.BuildServices();

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        probe.RequestCount.ShouldBe(2);
    }

    [Fact]
    public async Task FindAsync_RetryAfterHeader_WaitsAtLeastTheHeaderDelay()
    {
        // arrange
        StubRetryAfterThenSuccess();
        var services = probe.BuildServices();
        var clock = Stopwatch.StartNew();

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        clock.Stop();
        result.ShouldBeOfType<MetadataLookupResult.Found>();
        probe.RequestCount.ShouldBe(2);
        clock.Elapsed.ShouldBeGreaterThanOrEqualTo(RetryAfterDelay - Tolerance);
    }

    [Fact]
    public async Task FindAsync_RetryAfterBeyondTheTotalBudget_ReturnsFailedProviderUnavailableWithoutASecondRequest()
    {
        // arrange
        StubStatus(429, RetryAfterDelay * 30);
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
        using var slow = WireMockServer.Start();
        slow.Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(2)).WithBody(FoundBody));
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = slow.Urls[0],
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
        var dead = WireMockServer.Start();
        var deadUrl = dead.Urls[0];
        dead.Stop();
        dead.Dispose();
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = deadUrl,
        });

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
    }

    [Fact]
    public async Task FindAsync_WhenTheBreakerOpens_ReturnsFailedProviderUnavailableWithoutANewRequest()
    {
        // arrange
        StubStatus(500);
        var services = probe.BuildServices(new Dictionary<string, string?>
        {
            [MetadataProviderProbe.Resilience(nameof(MetadataProviderResilienceOptions.MinimumThroughput))] = "2",
        });

        // act
        var opening = await FindAsync(services, TestContext.Current.CancellationToken);
        var requestsBeforeOpenCircuit = probe.RequestCount;
        var open = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(opening).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        Category(open).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        requestsBeforeOpenCircuit.ShouldBeGreaterThanOrEqualTo(2);
        probe.RequestCount.ShouldBe(requestsBeforeOpenCircuit);
    }

    [Fact]
    public async Task FindAsync_CancelledDuringTheBackoffWait_ThrowsOperationCanceled()
    {
        // arrange
        StubStatus(500, RetryAfterDelay);
        var services = probe.BuildServices();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var token = cancellation.Token;

        // act
        var act = () => FindAsync(services, token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
    }

    private static async Task<MetadataLookupResult> FindAsync(IServiceProvider services, CancellationToken ct) =>
        await services
            .GetRequiredService<IMetadataProvider>()
            .FindAsync(new MetadataLookup("Solaris", null), ct);

    private static EnrichmentFailureCategory Category(MetadataLookupResult result) =>
        result.ShouldBeOfType<MetadataLookupResult.Failed>().Category;

    private void StubStatus(int statusCode, TimeSpan? retryAfter = null)
    {
        var response = Response.Create().WithStatusCode(statusCode);
        if (retryAfter is not null)
        {
            response = response.WithHeader(
                "Retry-After",
                ((int)retryAfter.Value.TotalSeconds).ToString(CultureInfo.InvariantCulture));
        }

        probe.Server.Given(Request.Create().UsingGet()).RespondWith(response);
    }

    private void StubTransientThenSuccess()
    {
        probe.Server
            .Given(Request.Create().UsingGet())
            .InScenario("transient")
            .WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(503));
        probe.Server
            .Given(Request.Create().UsingGet())
            .InScenario("transient")
            .WhenStateIs("recovered")
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(FoundBody));
    }

    private void StubRetryAfterThenSuccess()
    {
        probe.Server
            .Given(Request.Create().UsingGet())
            .InScenario("throttled")
            .WillSetStateTo("allowed")
            .RespondWith(Response.Create().WithStatusCode(429).WithHeader("Retry-After", "1"));
        probe.Server
            .Given(Request.Create().UsingGet())
            .InScenario("throttled")
            .WhenStateIs("allowed")
.RespondWith(Response.Create().WithStatusCode(200).WithBody(FoundBody));
    }
}
