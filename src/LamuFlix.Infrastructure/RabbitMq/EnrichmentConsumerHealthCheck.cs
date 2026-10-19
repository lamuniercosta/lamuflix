using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class EnrichmentConsumerHealthCheck(IWorkerLiveness liveness) : IHealthCheck
{
    public const string UnhealthyDescription = "The enrichment consumer is not listening.";
    public const string HealthyDescription = "The enrichment consumer is listening.";

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
