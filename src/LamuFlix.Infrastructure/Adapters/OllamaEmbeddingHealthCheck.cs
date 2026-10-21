using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LamuFlix.Infrastructure.Adapters;

internal sealed class OllamaEmbeddingHealthCheck(OllamaEmbeddingModelState state) : IHealthCheck
{
    private const string HealthyDescription = "The last embedding generation succeeded.";
    private const string DegradedDescription = "The last embedding generation did not succeed.";
    private const string UnhealthyDescription = "Embedding startup verification has not completed.";

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(Rank(state.Current));
    }

    private static HealthCheckResult Rank(OllamaEmbeddingModelState.Snapshot snapshot)
    {
        if (!snapshot.IsVerified)
        {
            return HealthCheckResult.Unhealthy(UnhealthyDescription);
        }

        return snapshot.LastFailure is null
            ? HealthCheckResult.Healthy(HealthyDescription)
            : HealthCheckResult.Degraded(DegradedDescription);
    }
}
