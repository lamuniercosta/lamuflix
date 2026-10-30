using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Core.Ports;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using RecordingRequestGate = LamuFlix.IntegrationTests.MetadataProviderProbe.RecordingRequestGate;

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
    private const string NotFoundBody = """
        {
          "Response": "False",
          "Error": "Movie not found!"
        }
        """;
    private const string SearchBody = """
        {
          "Response": "True",
          "Search": [
            { "Title": "Solaris", "Year": "1972", "imdbID": "tt0069293" }
          ],
          "totalResults": "1"
        }
        """;
    private static readonly TimeSpan Tolerance = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan RetryAfterDelay = TimeSpan.FromSeconds(1);

    public static TheoryData<string, int> PhysicalAttemptMatrices => new()
    {
        { "direct-detail", 1 },
        { "search-and-detail", 3 },
        { "transient-then-success", 2 },
        { "always-rate-limited", 1 + MaxRetryAttempts },
        { "always-server-error", 1 + MaxRetryAttempts },
    };

    public static TheoryData<string> InterruptionSignals => new() { "exhausted", "persistence-failure" };

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
        using var slow = LoopbackWireMock.Start();
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
        var dead = LoopbackWireMock.Start();
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

    [Theory]
    [MemberData(nameof(PhysicalAttemptMatrices))]
    public async Task FindAsync_ShippedComposition_CommitsOneReservationPerPhysicalRequest(string scenario, int expected)
    {
        // arrange
        ArrangeScenario(scenario);
        var gate = RecordingRequestGate.Admitting();
        var services = probe.BuildServices(gate: gate);

        // act
        await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        gate.Invocations.ShouldBe(expected);
        gate.Commitments.ShouldBe(expected);
        probe.RequestCount.ShouldBe(expected);
    }

    [Fact]
    public async Task FindAsync_ExhaustedMidRetry_StopsTheNextTransportRequestAndReturnsRateLimited()
    {
        // arrange
        StubStatus(500);
        var gate = RecordingRequestGate.AdmittingOnly(1);
        var services = probe.BuildServices(gate: gate);

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.RateLimited);
        probe.RequestCount.ShouldBe(1);
        gate.Invocations.ShouldBe(2);
        gate.Commitments.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_ExhaustedDuringFallback_StopsOmdbVariantsWithoutFurtherTransport()
    {
        // arrange
        probe.Server.Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(NotFoundBody));
        var gate = RecordingRequestGate.AdmittingOnly(1);
        var services = probe.BuildServices(gate: gate);

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.RateLimited);
        probe.RequestCount.ShouldBe(1);
        probe.HasQuery("s").ShouldBeFalse();
        gate.Invocations.ShouldBe(2);
        gate.Commitments.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_GatePersistenceFailure_ReturnsProviderUnavailableWithZeroDispatch()
    {
        // arrange
        var gate = RecordingRequestGate.Faulting(new InvalidOperationException("database unavailable"));
        var services = probe.BuildServices(gate: gate);

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        probe.RequestCount.ShouldBe(0);
        gate.Invocations.ShouldBe(1);
        gate.Commitments.ShouldBe(0);
    }

    [Fact]
    public async Task FindAsync_OpenBreaker_SpendsNoFurtherGateInvocationOrReservation()
    {
        // arrange
        StubStatus(500);
        var gate = RecordingRequestGate.Admitting();
        var services = probe.BuildServices(
            new Dictionary<string, string?>
            {
                [MetadataProviderProbe.Resilience(nameof(MetadataProviderResilienceOptions.MinimumThroughput))] = "2",
            },
            gate);

        // act
        var opening = await FindAsync(services, TestContext.Current.CancellationToken);
        var requestsBeforeOpenCircuit = probe.RequestCount;
        var invocationsBeforeOpenCircuit = gate.Invocations;
        var commitmentsBeforeOpenCircuit = gate.Commitments;
        var open = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(opening).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        Category(open).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        requestsBeforeOpenCircuit.ShouldBeGreaterThanOrEqualTo(2);
        probe.RequestCount.ShouldBe(requestsBeforeOpenCircuit);
        gate.Invocations.ShouldBe(invocationsBeforeOpenCircuit);
        gate.Commitments.ShouldBe(commitmentsBeforeOpenCircuit);
    }

    [Fact]
    public async Task FindAsync_CancelledDuringTheBackoffWait_SpendsNoExtraReservation()
    {
        // arrange
        StubStatus(500, RetryAfterDelay);
        var gate = RecordingRequestGate.Admitting();
        var services = probe.BuildServices(gate: gate);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var token = cancellation.Token;

        // act
        var act = () => FindAsync(services, token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
        probe.RequestCount.ShouldBe(1);
        gate.Invocations.ShouldBe(1);
        gate.Commitments.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_AcquiredAttemptsThatTimeout_NeverRefundReservations()
    {
        // arrange
        using var slow = LoopbackWireMock.Start();
        slow.Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(2)).WithBody(FoundBody));
        var gate = RecordingRequestGate.Admitting();
        var services = probe.BuildServices(
            new Dictionary<string, string?>
            {
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = slow.Urls[0],
            },
            gate);

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        gate.Commitments.ShouldBeGreaterThan(1);
        gate.Commitments.ShouldBe(gate.Invocations);
    }

    [Fact]
    public async Task FindAsync_CancelledAfterAdmission_PropagatesCancellationWithoutRefund()
    {
        // arrange
        using var slow = LoopbackWireMock.Start();
        slow.Given(Request.Create().UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithDelay(TimeSpan.FromSeconds(2)).WithBody(FoundBody));
        var gate = RecordingRequestGate.Admitting();
        var services = probe.BuildServices(
            new Dictionary<string, string?>
            {
                [$"{OmdbOptions.SectionName}:{nameof(OmdbOptions.BaseUrl)}"] = slow.Urls[0],
            },
            gate);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var token = cancellation.Token;

        // act
        var act = () => FindAsync(services, token);

        // assert
        await Should.ThrowAsync<OperationCanceledException>(act);
        gate.Invocations.ShouldBe(1);
        gate.Commitments.ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(InterruptionSignals))]
    public async Task FindAsync_SignalInterruption_PreservesChainFallbackToTheSecondaryProvider(string signal)
    {
        // arrange
        using var secondary = LoopbackWireMock.Start();
        secondary.Given(Request.Create().WithPath("/3/search/movie").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody(SecondaryMetadataProviderProbe.Fixture("Search", "found.json")));
        secondary.Given(Request.Create().WithPath("/3/movie/603").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody(SecondaryMetadataProviderProbe.Fixture("Details", "found.json")));
        var gate = signal == "exhausted"
            ? RecordingRequestGate.AdmittingOnly(0)
            : RecordingRequestGate.Faulting(new InvalidOperationException("database unavailable"));
        var services = probe.BuildServices(SecondaryOverrides(secondary.Urls[0]), gate);

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        result.ShouldBeOfType<MetadataLookupResult.Found>().Metadata.Title.ShouldBe("The Matrix");
        probe.RequestCount.ShouldBe(0);
        secondary.LogEntries.Count().ShouldBe(2);
        gate.Invocations.ShouldBe(1);
    }

    [Fact]
    public async Task FindAsync_SlowGate_BoundsAdmissionsAndInvocationsWithoutReachingTransport()
    {
        // arrange
        StubStatus(200);
        var gate = RecordingRequestGate.Slow(TimeSpan.FromMilliseconds(600));
        var services = probe.BuildServices(gate: gate);

        // act
        var result = await FindAsync(services, TestContext.Current.CancellationToken);

        // assert
        Category(result).ShouldBe(EnrichmentFailureCategory.ProviderUnavailable);
        probe.RequestCount.ShouldBe(0);
        gate.Invocations.ShouldBeGreaterThan(0);
        gate.Invocations.ShouldBeLessThanOrEqualTo(1 + MaxRetryAttempts);
        gate.Commitments.ShouldBeLessThanOrEqualTo(1 + MaxRetryAttempts);
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

    private void ArrangeScenario(string scenario)
    {
        switch (scenario)
        {
            case "direct-detail":
                StubStatus(200);
                break;
            case "search-and-detail":
                StubSearchThenDetail();
                break;
            case "transient-then-success":
                StubTransientThenSuccess();
                break;
            case "always-rate-limited":
                StubStatus(429);
                break;
            default:
                StubStatus(500);
                break;
        }
    }

    private void StubSearchThenDetail()
    {
        probe.Server
            .Given(Request.Create().UsingGet())
            .InScenario("search-fallback")
            .WillSetStateTo("searched")
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(NotFoundBody));
        probe.Server
            .Given(Request.Create().UsingGet())
            .InScenario("search-fallback")
            .WhenStateIs("searched")
            .WillSetStateTo("detail")
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(SearchBody));
        probe.Server
            .Given(Request.Create().UsingGet())
            .InScenario("search-fallback")
            .WhenStateIs("detail")
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(FoundBody));
    }

    private static Dictionary<string, string?> SecondaryOverrides(string baseUrl) =>
        new()
        {
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.Enabled)}"] = "true",
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.BaseUrl)}"] = $"{baseUrl}/3",
            [$"{TmdbOptions.SectionName}:{nameof(TmdbOptions.ApiKey)}"] = "secondary-sentinel-key",
        };

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
