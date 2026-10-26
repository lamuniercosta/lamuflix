using System;
using System.Threading;

namespace LamuFlix.Infrastructure.Enrichment;

public enum OutboxRelayLifecycle
{
    NotStarted = 0,
    Running = 1,
    Stopped = 2,
}

public enum OutboxRelayPassOutcome
{
    Succeeded = 0,
    Failed = 1,
}

public enum OutboxRelayCleanupOutcome
{
    Succeeded = 0,
    Failed = 1,
}

// ReSharper disable NotAccessedPositionalProperty.Global
// The pass timing stays on the public snapshot for diagnostics; health and metrics decide on Outcome.
public sealed record OutboxRelayPassSnapshot(OutboxRelayPassOutcome Outcome, DateTimeOffset CompletedAt);
// ReSharper restore NotAccessedPositionalProperty.Global

public sealed record OutboxRelayBacklogSnapshot(
    long PendingCount,
    DateTimeOffset? OldestPendingCreatedAt,
    DateTimeOffset SampledAt);

// ReSharper disable NotAccessedPositionalProperty.Global
// The cleanup count and timing stay on the public snapshot for diagnostics; health decides on Outcome.
public sealed record OutboxRelayCleanupSnapshot(
    OutboxRelayCleanupOutcome Outcome,
    int DeletedCount,
    DateTimeOffset CompletedAt);
// ReSharper restore NotAccessedPositionalProperty.Global

public sealed record OutboxRelaySnapshot(
    OutboxRelayLifecycle Lifecycle,
    OutboxRelayPassSnapshot? LastPass,
    OutboxRelayBacklogSnapshot? Backlog,
    OutboxRelayCleanupSnapshot? LastCleanup)
{
    public static OutboxRelaySnapshot Initial { get; } = new(OutboxRelayLifecycle.NotStarted, null, null, null);
}

public sealed class OutboxRelayState
{
    private readonly Lock gate = new();
    private OutboxRelaySnapshot current = OutboxRelaySnapshot.Initial;

    public OutboxRelaySnapshot Current
    {
        get
        {
            lock (gate)
            {
                return current;
            }
        }
    }

    public void MarkRunning() => Update(snapshot => snapshot with { Lifecycle = OutboxRelayLifecycle.Running });

    public void MarkStopped() => Update(snapshot => snapshot with { Lifecycle = OutboxRelayLifecycle.Stopped });

    public void RecordPassSucceeded(
        long pendingCount,
        DateTimeOffset? oldestPendingCreatedAt,
        DateTimeOffset sampledAt) =>
        Update(snapshot => snapshot with
        {
            LastPass = new OutboxRelayPassSnapshot(OutboxRelayPassOutcome.Succeeded, sampledAt),
            Backlog = new OutboxRelayBacklogSnapshot(pendingCount, oldestPendingCreatedAt, sampledAt),
        });

    public void RecordPassFailed(DateTimeOffset failedAt) =>
        Update(snapshot => snapshot with
        {
            LastPass = new OutboxRelayPassSnapshot(OutboxRelayPassOutcome.Failed, failedAt),
        });

    public void RecordCleanupSucceeded(int deletedCount, DateTimeOffset completedAt) =>
        Update(snapshot => snapshot with
        {
            LastCleanup = new OutboxRelayCleanupSnapshot(
                OutboxRelayCleanupOutcome.Succeeded,
                deletedCount,
                completedAt),
        });

    public void RecordCleanupFailed(DateTimeOffset failedAt) =>
        Update(snapshot => snapshot with
        {
            LastCleanup = new OutboxRelayCleanupSnapshot(OutboxRelayCleanupOutcome.Failed, 0, failedAt),
        });

    private void Update(Func<OutboxRelaySnapshot, OutboxRelaySnapshot> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);

        lock (gate)
        {
            current = mutate(current);
        }
    }
}
