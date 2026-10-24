using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class FeaturesConsumerHealthCheck(FeaturesConsumerLiveness liveness) : IHealthCheck
{
    public const string UnhealthyDescription = "The features consumer is not listening.";
    public const string HealthyDescription = "The features consumer is listening.";

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Task.FromResult(
            liveness.IsListening
                ? HealthCheckResult.Healthy(HealthyDescription)
                : HealthCheckResult.Unhealthy(UnhealthyDescription));
    }
}
