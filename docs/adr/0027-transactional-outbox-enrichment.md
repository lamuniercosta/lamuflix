# 0027. Transactional outbox for enrichment requests

- Status: Accepted
- Date: 2026-10-25
- Ticket: DEV-329

## Context

Enrichment requests are staged with an insert-then-enqueue dual write: the movie row is committed in Postgres and the enrichment message is published to RabbitMQ in two separate steps (ADR-0005). A broker outage, nack, or returned message between the two leaves a `Pending` movie with no message anywhere, and recovery waits on the sweeper (ADR-0004, ADR-0005 point 2), up to one claim lease plus one sweep interval. ADR-0005 point 4 named the transactional outbox as the stretch goal that removes the "database committed, publish failed" window entirely. This plan accepts that stretch goal; implementation belongs to DEV-329 with the follow-up in DEV-420.

Basis: ADR-0004 (claims, confirmed mandatory publish, sweeper design); ADR-0005 points 2 and 4; task note DEV-419.

## Decision

Stage every enrichment request in an outbox table that lives in the same Postgres database as the movies, written in the same transaction as the movie write. A relay hosted in the Api host reads unstaged rows and publishes them through the existing confirmed, mandatory publish path, and sets `ProcessedAt` only after the broker confirm settles. A confirm timeout, nack, returned message, or connection failure leaves the row unstaged for a later relay pass; it is never marked sent on an uncertain outcome.

The sweeper (ADR-0004, ADR-0005 point 2) stays as a safety net for paths the outbox cannot close, such as a crash inside a live claim lease. At-least-once delivery stands: duplicates produced by the relay or the sweeper are absorbed by the enrichment claim, which still refuses work that is not `Pending`.

## Assumptions

- One Api instance runs the relay; multi-instance relay ownership is out of scope.
- Processed rows are kept for a retention window rather than deleted on publish.
- The outbox table is a schema change and the relay a new hosted service; both ship in DEV-329/DEV-420, not here.
- The constitution text itself is unchanged by this ticket.

## Consequences

This ADR supersedes ADR-0005 point 2 on the publish side and realises ADR-0005 point 4; the sweeper remains the safety net, and no ADR-0005 history is rewritten.

The constitution still describes the pre-outbox position and needs a later amendment, which is out of scope here: lines 225-226 (rationale stating the pipeline is trustworthy without a transactional outbox), lines 488-489 (the insert-then-enqueue dual-write passage naming the sweeper mitigation with the outbox as the documented stretch), lines 532-533 (the dual-write-on-import debt entry naming the outbox as Epic 8), and lines 220-221 (the `StrandedMovieSweeper` re-enqueue rule). A follow-up ticket amends those lines to name the outbox as adopted.
