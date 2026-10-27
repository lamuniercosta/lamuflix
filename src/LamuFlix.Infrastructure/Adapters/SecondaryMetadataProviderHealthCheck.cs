using System;
using System.Threading;
using System.Threading.Tasks;
using LamuFlix.Core.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Adapters;

internal sealed class SecondaryMetadataProviderHealthCheck(
    SecondaryMetadataProviderHealthState state,
    IOptions<TmdbOptions> options) : IHealthCheck
{
    private const string HealthyDescription = "The last TMDb lookup succeeded.";
    private const string DegradedDescription = "The last TMDb lookup did not succeed.";
    private const string UnhealthyDescription = "Check the TMDb API key configuration.";

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(Rank(state.Current));
    }

    private HealthCheckResult Rank(SecondaryMetadataProviderHealthState.Snapshot? snapshot)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            return HealthCheckResult.Unhealthy(UnhealthyDescription);
        }

        if (snapshot is null || snapshot.Category is null)
        {
            return HealthCheckResult.Healthy(HealthyDescription);
        }

        return snapshot.WasUnauthorized
            ? HealthCheckResult.Unhealthy(UnhealthyDescription)
            : HealthCheckResult.Degraded(DegradedDescription);
    }
}
