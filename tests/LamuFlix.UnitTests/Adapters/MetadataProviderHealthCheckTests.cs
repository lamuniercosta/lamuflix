using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Domain;
using LamuFlix.Infrastructure.Adapters;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.UnitTests.Adapters;

public sealed class MetadataProviderHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var healthCheck = CreateHealthCheck();

        await Should.ThrowAsync<ArgumentNullException>(
            () => healthCheck.CheckHealthAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CheckHealthAsync_WithMissingCategory_ReturnsHealthy()
    {
        var healthCheck = CreateHealthCheck(category: null, wasUnauthorized: false, hasSnapshot: true);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNullSnapshot_ReturnsHealthy()
    {
        var healthCheck = CreateHealthCheck();

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithRecordedCategory_ReturnsDegraded()
    {
        var healthCheck = CreateHealthCheck(EnrichmentFailureCategory.ProviderUnavailable, wasUnauthorized: false);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task CheckHealthAsync_WithNullCategory_ReturnsHealthy()
    {
        var healthCheck = CreateHealthCheck(category: null, wasUnauthorized: false, hasSnapshot: true);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNullCategoryBlockIsRemoved_ReturnsHealthy()
    {
        var healthCheck = CreateHealthCheck(category: null, wasUnauthorized: false, hasSnapshot: true);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenUnauthorized_ReturnsUnhealthy()
    {
        var healthCheck = CreateHealthCheck(EnrichmentFailureCategory.ProviderUnavailable, wasUnauthorized: true);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNotUnauthorized_ReturnsDegraded()
    {
        var healthCheck = CreateHealthCheck(EnrichmentFailureCategory.ProviderUnavailable, wasUnauthorized: false);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    private static IHealthCheck CreateHealthCheck(
        EnrichmentFailureCategory? category = null,
        bool wasUnauthorized = false,
        bool hasSnapshot = false)
    {
        var assembly = typeof(MetadataProviderServiceCollectionExtensions).Assembly;
        var stateType = assembly.GetType("LamuFlix.Infrastructure.Adapters.MetadataProviderHealthState", throwOnError: true)!;
        var state = Activator.CreateInstance(stateType, nonPublic: true)!;
        if (hasSnapshot || category is not null || wasUnauthorized)
        {
            stateType.GetMethod("Record", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(state, [category, wasUnauthorized]);
        }

        var healthCheckType = assembly.GetType("LamuFlix.Infrastructure.Adapters.MetadataProviderHealthCheck", throwOnError: true)!;
        return (IHealthCheck)Activator.CreateInstance(
            healthCheckType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [state],
            null)!;
    }
}