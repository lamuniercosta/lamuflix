using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.Infrastructure.Adapters;

internal sealed class MetadataProviderHealthCheck(MetadataProviderHealthState state) : IHealthCheck
{
    public const string HealthyDescription = "The last metadata provider lookup succeeded.";
    public const string DegradedDescription = "The last metadata provider lookup did not succeed.";
    public const string UnhealthyDescription = "Check the OMDb API key configuration.";

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(Rank(state.Current));
    }

    private static HealthCheckResult Rank(MetadataProviderHealthState.Snapshot? snapshot)
    {
        if (snapshot is null || snapshot.Category is null)
        {
            return HealthCheckResult.Healthy(HealthyDescription);
        }

        return snapshot.WasUnauthorized
            ? HealthCheckResult.Unhealthy(UnhealthyDescription)
            : HealthCheckResult.Degraded(DegradedDescription);
    }
}