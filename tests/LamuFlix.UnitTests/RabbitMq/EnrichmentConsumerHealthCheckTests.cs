using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Infrastructure.RabbitMq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.UnitTests.RabbitMq;

public sealed class EnrichmentConsumerHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WhenListening_ReturnsHealthy()
    {
        // arrange
        var healthCheck = new EnrichmentConsumerHealthCheck(new FixedLiveness(isListening: true));

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldBe(EnrichmentConsumerHealthCheck.HealthyDescription);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenNotListening_ReturnsUnhealthy()
    {
        // arrange
        var healthCheck = new EnrichmentConsumerHealthCheck(new FixedLiveness(isListening: false));

        // act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldBe(EnrichmentConsumerHealthCheck.UnhealthyDescription);
    }

    [Fact]
    public async Task CheckHealthAsync_ReadsListeningStateOnEachCall()
    {
        // arrange
        var liveness = new FixedLiveness(isListening: false);
        var healthCheck = new EnrichmentConsumerHealthCheck(liveness);

        // act
        var before = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);
        liveness.IsListening = true;
        var after = await healthCheck.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        // assert
        before.Status.ShouldBe(HealthStatus.Unhealthy);
        after.Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public void CheckHealthAsync_WithNullContext_ThrowsArgumentNullException()
    {
        // arrange
        var healthCheck = new EnrichmentConsumerHealthCheck(new FixedLiveness(isListening: true));
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
