using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.RabbitMq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class FeaturesConsumerHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenListening_ReturnsHealthy()
    {
        // arrange
        var liveness = new FeaturesConsumerLiveness();
        liveness.MarkListening();
        var healthCheck = new FeaturesConsumerHealthCheck(liveness);

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe(FeaturesConsumerHealthCheck.HealthyDescription);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNotListening_ReturnsUnhealthy()
    {
        // arrange
        var healthCheck = new FeaturesConsumerHealthCheck(new FeaturesConsumerLiveness());

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe(FeaturesConsumerHealthCheck.UnhealthyDescription);
    }

    [Fact]
    public async Task CheckHealthAsync_FeatureDisconnect_IsNotMaskedByMetadataWorkerLiveness()
    {
        // arrange
        var enrichmentLiveness = new FixedLiveness(isListening: true);
        var featuresLiveness = new FeaturesConsumerLiveness();
        var enrichment = new EnrichmentConsumerHealthCheck(enrichmentLiveness);
        var features = new FeaturesConsumerHealthCheck(featuresLiveness);

        // act
        var enrichmentResult = await enrichment.CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken);
        var featuresResult = await features.CheckHealthAsync(
            new HealthCheckContext(),
            TestContext.Current.CancellationToken);

        // assert
        enrichmentResult.Status.ShouldBe(HealthStatus.Healthy);
        featuresResult.Status.ShouldBe(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_ReadsListeningStateOnEachCall()
    {
        // arrange
        var liveness = new FeaturesConsumerLiveness();
        var healthCheck = new FeaturesConsumerHealthCheck(liveness);

        // act
        var before = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);
        liveness.MarkListening();
        var after = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        before.Status.ShouldBe(HealthStatus.Unhealthy);
        after.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void CheckHealthAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // arrange
        var healthCheck = new FeaturesConsumerHealthCheck(new FeaturesConsumerLiveness());
        var checkHealthAsync = typeof(IHealthCheck).GetMethod(nameof(IHealthCheck.CheckHealthAsync))
            ?? throw new InvalidOperationException("IHealthCheck CheckHealthAsync method was not found.");

        // act
        var exception = Should.Throw<TargetInvocationException>(
            () => checkHealthAsync.Invoke(healthCheck, [null, CancellationToken.None]));

        // assert
        exception.InnerException.ShouldBeOfType<ArgumentNullException>();
    }

    private sealed class FixedLiveness(bool isListening) : IWorkerLiveness
    {
        public bool IsListening { get; set; } = isListening;

        public void MarkListening() => IsListening = true;

        public void MarkStopped() => IsListening = false;
    }
}
