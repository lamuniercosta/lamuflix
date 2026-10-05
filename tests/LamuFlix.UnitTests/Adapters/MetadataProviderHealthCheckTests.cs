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
    public void CheckHealthAsync_WithNullContext_ThrowsArgumentNullException()
    {
        var healthCheck = CreateHealthCheck();
        var checkHealthAsync = typeof(IHealthCheck).GetMethod(nameof(IHealthCheck.CheckHealthAsync))
            ?? throw new InvalidOperationException("IHealthCheck CheckHealthAsync method was not found.");

        var exception = Should.Throw<TargetInvocationException>(
            () => checkHealthAsync.Invoke(healthCheck, [null, CancellationToken.None]));

        exception.InnerException.ShouldBeOfType<ArgumentNullException>();
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
        var stateType = assembly.GetType("LamuFlix.Infrastructure.Adapters.MetadataProviderHealthState", throwOnError: true)
            ?? throw new InvalidOperationException("Metadata provider health state type was not found.");
        var state = Activator.CreateInstance(stateType, nonPublic: true)
            ?? throw new InvalidOperationException("Metadata provider health state could not be created.");
        if (hasSnapshot || category is not null || wasUnauthorized)
        {
            (stateType.GetMethod("Record", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Metadata provider health state Record method was not found."))
                .Invoke(state, [category, wasUnauthorized]);
        }

        var healthCheckType = assembly.GetType("LamuFlix.Infrastructure.Adapters.MetadataProviderHealthCheck", throwOnError: true)
            ?? throw new InvalidOperationException("Metadata provider health check type was not found.");
        return (IHealthCheck)(Activator.CreateInstance(
            healthCheckType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [state],
            null) ?? throw new InvalidOperationException("Metadata provider health check could not be created."));
    }
}