using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using LamuFlix.Infrastructure.RabbitMq;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace LamuFlix.Infrastructure.RabbitMq;

public sealed class RabbitMqHealthCheck(RabbitMqConnectionOwner owner) : IHealthCheck
{
    public const string UnhealthyDescription = "The RabbitMQ broker is not reachable.";
    public const string HealthyDescription = "The RabbitMQ broker is reachable.";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var connection = await owner.GetAsync(cancellationToken);
            return connection.IsOpen
                ? HealthCheckResult.Healthy(HealthyDescription)
                : HealthCheckResult.Unhealthy(UnhealthyDescription);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(UnhealthyDescription);
        }
    }
}