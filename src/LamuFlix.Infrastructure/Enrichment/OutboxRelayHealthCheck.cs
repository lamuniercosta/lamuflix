using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LamuFlix.Infrastructure.Enrichment;

public sealed class OutboxRelayHealthCheck(OutboxRelayState state, IOptions<OutboxRelayOptions> options) : IHealthCheck
{
    public const string HealthCheckName = "outbox-relay";

    public const string HealthyDescription = "The outbox relay is sampling a current backlog below the warning age.";

    public const string DegradedDescription = "The outbox relay backlog is older than the warning age or its last cleanup failed.";

    public const string UnhealthyDescription = "The outbox relay has no successful backlog sample or is not running.";

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return Task.FromResult(Rank(state.Current));
    }

    private HealthCheckResult Rank(OutboxRelaySnapshot snapshot)
    {
        if (snapshot.Lifecycle != OutboxRelayLifecycle.Running
            || snapshot.LastPass is not { Outcome: OutboxRelayPassOutcome.Succeeded }
            || snapshot.Backlog is not { } backlog)
        {
            return HealthCheckResult.Unhealthy(UnhealthyDescription);
        }

        if (OldestPendingAge(backlog) >= options.Value.BacklogAgeWarning
            || snapshot.LastCleanup is { Outcome: OutboxRelayCleanupOutcome.Failed })
        {
            return HealthCheckResult.Degraded(DegradedDescription);
        }

        return HealthCheckResult.Healthy(HealthyDescription);
    }

    private static TimeSpan OldestPendingAge(OutboxRelayBacklogSnapshot backlog) =>
        backlog.OldestPendingCreatedAt is { } oldest ? backlog.SampledAt - oldest : TimeSpan.Zero;
}
