# Data Model: DEV-330 Enrichment status SignalR push

**Date**: 2026-10-26

No database schema, migration, or EF entity change. This ticket notifies about existing `Movie.EnrichmentStatus` transitions after commit.

## EnrichmentStatus (existing)

SmartEnum / domain enumeration: `Pending`, `Enriched`, `NotFound`, `Failed` (`src/LamuFlix.Core/Domain/EnrichmentStatus.cs`). Wire form is the name string only.

## Movie enrichment status transition (logical)

| Field | Meaning |
|-------|---------|
| Movie identity | Assigned `MovieId` after insert; Api unwraps to `int Id` |
| Persisted-before status | Status already committed before this save, or absent for a new insert |
| Committed-after status | Status after successful enclosing transaction commit |
| Transition? | Insert into Pending, or persisted-before ≠ committed-after |

## EnrichmentStatusChangedDto (Api wire payload)

| Field | Type | Wire name | Rules |
|-------|------|-----------|-------|
| Id | `int` | `id` | Required; unwrapped `MovieId.Value` |
| EnrichmentStatus | `EnrichmentStatus` | `enrichmentStatus` | Required; one of four string names |

No timestamps, metadata, or error details.

## Notification lifecycle

1. Repository observes candidate transitions during save preparation (method-local; never shared/static).
2. On failed save / aborted transaction: discard candidates; emit nothing.
3. On successful enclosing commit: sequentially await one `NotifyAsync(..., CancellationToken.None)` per changed movie; catch/log per-movie failures and continue remaining movies.
4. Adapter maps to DTO and awaits one broadcast; logs attempt start/failure with movie id and committed status; send failures isolated.
5. No durable notification store; process death between commit and send can lose the event; reconnect/refetch is DEV-422.
