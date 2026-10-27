using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.UnitTests.Adapters;

public sealed class SecondaryMetadataProviderHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WithInitialBlankKey_ReturnsUnhealthy()
    {
        // arrange
        var healthCheck = CreateHealthCheck(new TmdbOptions());

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        (result.Description ?? string.Empty).ShouldContain("TMDb API key");
    }

    [Fact]
    public async Task CheckHealthAsync_WithDisabledBlankKey_ReturnsUnhealthy()
    {
        // arrange
        var healthCheck = CreateHealthCheck(new TmdbOptions { Enabled = false });

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        (result.Description ?? string.Empty).ShouldContain("TMDb API key");
    }

    [Fact]
    public async Task CheckHealthAsync_WithWhitespaceKey_ReturnsUnhealthy()
    {
        // arrange
        var healthCheck = CreateHealthCheck(new TmdbOptions { Enabled = true, ApiKey = "   " });

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithKeyAndNoSnapshot_ReturnsHealthy()
    {
        // arrange
        var healthCheck = CreateHealthCheck(EnabledOptions());

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithKeyAndDisabledAndNoFailure_ReturnsHealthy()
    {
        // arrange
        var healthCheck = CreateHealthCheck(new TmdbOptions { Enabled = false, ApiKey = "sentinel-key" });

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithKeyAndSuccessfulOutcome_ReturnsHealthy()
    {
        // arrange
        var healthCheck = CreateHealthCheck(EnabledOptions(), category: null, wasUnauthorized: false, hasSnapshot: true);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithKeyAndRecordedFailure_ReturnsDegraded()
    {
        // arrange
        var healthCheck = CreateHealthCheck(EnabledOptions(), EnrichmentFailureCategory.ProviderUnavailable);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Degraded);
        (result.Description ?? string.Empty).ShouldContain("did not succeed");
    }

    [Fact]
    public async Task CheckHealthAsync_WithKeyAndRateLimitedFailure_ReturnsDegraded()
    {
        // arrange
        var healthCheck = CreateHealthCheck(EnabledOptions(), EnrichmentFailureCategory.RateLimited);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_WithKeyAndUnauthorizedFailure_ReturnsUnhealthy()
    {
        // arrange
        var healthCheck = CreateHealthCheck(EnabledOptions(), EnrichmentFailureCategory.InvalidResponse, wasUnauthorized: true);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        (result.Description ?? string.Empty).ShouldContain("TMDb API key");
    }

    [Fact]
    public async Task CheckHealthAsync_AfterFailureThenSuccess_ReturnsHealthy()
    {
        // arrange
        var state = new SecondaryMetadataProviderHealthState();
        Record(state, EnrichmentFailureCategory.ProviderUnavailable, wasUnauthorized: false);
        Record(state, category: null, wasUnauthorized: false);
        var healthCheck = CreateHealthCheck(EnabledOptions(), state);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_AfterUnauthorizedThenSuccess_ReturnsHealthy()
    {
        // arrange
        var state = new SecondaryMetadataProviderHealthState();
        Record(state, EnrichmentFailureCategory.InvalidResponse, wasUnauthorized: true);
        Record(state, category: null, wasUnauthorized: false);
        var healthCheck = CreateHealthCheck(EnabledOptions(), state);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void CheckHealthAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // arrange
        var healthCheck = CreateHealthCheck(EnabledOptions());
        var checkHealthAsync = typeof(IHealthCheck).GetMethod(nameof(IHealthCheck.CheckHealthAsync))
            ?? throw new InvalidOperationException("IHealthCheck CheckHealthAsync method was not found.");

        // act
        var exception = Should.Throw<TargetInvocationException>(
            () => checkHealthAsync.Invoke(healthCheck, [null, CancellationToken.None]));

        // assert
        exception.InnerException.ShouldBeOfType<ArgumentNullException>();
    }

    private static TmdbOptions EnabledOptions() => new() { Enabled = true, ApiKey = "sentinel-key" };

    private static IHealthCheck CreateHealthCheck(
        TmdbOptions options,
        EnrichmentFailureCategory? category = null,
        bool wasUnauthorized = false,
        bool hasSnapshot = false)
    {
        var state = new SecondaryMetadataProviderHealthState();
        if (hasSnapshot || category is not null || wasUnauthorized)
        {
            Record(state, category, wasUnauthorized);
        }

        return CreateHealthCheck(options, state);
    }

    private static IHealthCheck CreateHealthCheck(TmdbOptions options, SecondaryMetadataProviderHealthState state)
    {
        var healthCheckType = typeof(SecondaryMetadataProviderHealthState).Assembly
            .GetType("LamuFlix.Infrastructure.Adapters.SecondaryMetadataProviderHealthCheck", throwOnError: true)
            ?? throw new InvalidOperationException("Secondary metadata provider health check type was not found.");
        return (IHealthCheck)(Activator.CreateInstance(
            healthCheckType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [state, Microsoft.Extensions.Options.Options.Create(options)],
            null) ?? throw new InvalidOperationException("Secondary metadata provider health check could not be created."));
    }

    private static void Record(
        SecondaryMetadataProviderHealthState state,
        EnrichmentFailureCategory? category,
        bool wasUnauthorized) =>
        (typeof(SecondaryMetadataProviderHealthState)
            .GetMethod("Record", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Secondary metadata provider health state Record method was not found."))
        .Invoke(state, [category, wasUnauthorized]);
}
