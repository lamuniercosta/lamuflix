# DEV-315 grill conclusions

Worktree: F:/Dev/LamuFlix.worktrees/feature-315-spec; branch: feature/315-spec.
Phase 2, size:M. Question cap: 12. Append-only log.

## Q1 — Outcome translation boundary

### Full question

[from Keel] /grill-with-docs DEV-315 Q1/12. Worktree F:/Dev/LamuFlix.worktrees/feature-315-spec; branch feature/315-spec; task note DEV-315; recon recon-DEV-315. Read those notes, task-pipeline-2 Phase 2, lamuflix-team-charter-2 and specs/PRODUCT.md in that worktree. Begin at Q1, prior attempt had zero questions/writes. Please rule on the outcome translation boundary: I recommend retain existing Completed(Claimed true or false) => BasicAck (cover Enriched, NotFound, refused claim), Failed Retry/RetryDelayed => publish existing enrichment.retry route with NextAttempt from core, then ack only after successful publish, Failed DeadLetter => publish enrichment.dead-letter then ack. Ticket wording BasicAck + republish specifies actions, not unsafe ack-first ordering; recon lines 23,30-32,53,56,59 show existing core/routing separation and publish-before-ack. Cost: real-broker proofs and targeted gaps rather than a redundant new translator or policy. Preserve malformed-message terminal nack and cancellation requeue as existing behavior outside the outcome table. ACCEPT/AMEND/REJECT with cited basis; explicitly flag any ticket contradiction/constitution departure as blocked: structural. One decision per turn, no independent searches, code, gates or writes; Keel records each full exchange in specs/DEV-315/CONCLUSIONS.md before advancing. Facts come from recon-DEV-315; if missing, needs recon and stop. Reply via this ask or ask Keel #2 back.

### Full Patron answer

[from Patron] DEV-315 Q1 ruling: AMEND. Accept the translation boundary as Keel recommends, with one addition: the dead-letter publish must carry failure headers.
- Basis (accept): ticket Description maps Enriched/NotFound/Claim Failed to BasicAck, retryable Attempt<Max to republish enrichment.retry Attempt+1, and fatal or Attempt>=Max to enrichment.dead-letter. It names the actions but not their order. EnrichmentConsumer.SettleAsync (src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs:172-193) already calls EnrichmentRouting.Decide, publishes, and only then acks, so publish-before-ack is the ticket's at-least-once reading and is not a contradiction. Do not add a new translator or policy type; that would be a new layer with no AC behind it (charter 2.3 item 2).
- Basis (amend): the ticket text says publish enrichment.dead-letter with failure headers. Current SettleAsync republishes only a bare EnrichmentRequested(MovieId, NextAttempt ?? Attempt) with no headers. The spec must name the header set (failure kind/reason, attempt) as a FR plus a real-broker assertion. The header names are a message-contract shape, so give the exact names in the spec as a Patron 2.3 item 4 ruling; that is in scope, not blocked.
- Basis (out of table): keep malformed body nack(requeue:false) (EnrichmentConsumer.cs:124-131), unexpected-exception nack(requeue:false) (148-156), and stopping-token nack(requeue:true) (140-146) as existing behaviour outside the outcome table. Prove them, do not change them. Proofs are Testcontainers RabbitMQ, one per routing path, per the ticket text. Not blocked: no ticket contradiction and no constitution departure.
- Unverified: recon-DEV-315 is not connected to Patron #2, so I checked the cited lines against the worktree code instead of the note.

### Recorded decision and implications

AMEND: Completed outcomes acknowledge; retry outcomes publish to the existing retry route with the core-provided next attempt before acknowledging; terminal outcomes publish to the existing dead-letter route with failure headers before acknowledging. No new translator or policy layer. Preserve malformed-body, unexpected-exception and stopping-token settlement behaviour and prove it with real-broker tests.

Basis: DEV-315 Description; recon-DEV-315:22-25,30-32,53-60; PRODUCT.md sections 2-3; charter section 2.3.

Exact failure headers remain undecided. Patron disclosed that recon was not connected to its seat and inspected code instead. Its claim that current republishing has no failure headers is not adopted as a verified recon fact. Request authoritative recon of publisher/header contract and fixture before Q2. No owner escalation identified in Q1. No taste ruling occurred.

---

## Patron charter ruling — dead-letter failure-header shape (before Q2)

### Question (Conductor)

Does choosing failure-header names and types for the RabbitMQ dead-letter publish trigger charter §2.3 item 4 (public API shape not already in spec.md), or is it an internal transport detail within the ticket? Recon-DEV-315:99: no terminal-failure headers exist today, no failure category or reason is carried through outcomes, and the existing publisher headers are telemetry only.

### Ruling

Within Patron authority. Not blocked. The ticket is unchanged.

- Not §2.3 item 4. Item 4 covers a public API (route, DTO field set, status codes). The contract chain is C# DTO → OpenAPI → TS. The enrichment.dead-letter queue is internal Worker topology (PRODUCT.md §Worker line 10). Nothing outside the system consumes it and it never reaches OpenAPI. It is a message-contract detail that the ticket itself asks for ("publish enrichment.dead-letter with failure headers", DEV-315 Description). Because spec.md does not yet name the headers, Patron records the shape here deliberately rather than leaving it to an assumption.
- Not a ticket change. Carrying the failure category from Core to the transport is forced by that ticket AC. Today the handler has `EnrichmentFailureCategory` in scope at dead-letter time (src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs:60-77) but drops it from `ProcessEnrichmentOutcome.Failed`/`EnrichmentFailureDecision`. Adding the category to the Failed outcome is an in-scope Core type change. It is not a schema change and adds no new layer, and Q1 forbids adding one.
- Not a constitution departure. The values are closed codes only, in line with the caller-safe category contract (ADR-0006, DEV-295; src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs). No owner escalation is needed.

### Allowed shape (dead-letter publishes from SettleAsync only)

| Header | AMQP type | Value |
|---|---|---|
| `x-lamuflix-failure-category` | string (UTF-8) | `EnrichmentFailureCategory.Code`: `provider_unavailable`, `rate_limited`, `invalid_response`, `unknown` |
| `x-lamuflix-failure-reason` | string (UTF-8) | `non_retryable` when `!IsRetryable`; `max_attempts_exhausted` when retryable but Attempt >= MaxAttempts (the two ticket triggers) |
| `x-lamuflix-failure-attempt` | int32 | the request Attempt that failed |

Constraints:
- No free text. That means no exception message, no `SafeDescription`, no provider payload, no PII or secrets.
- Do not write broker-owned `x-death`/`x-first-death-*`.
- Existing telemetry headers stay unchanged.
- Retry publishes carry no failure headers.
- Out-of-table paths are unchanged and get no failure headers: malformed-body nack, unexpected-exception nack and stopping-token requeue.
- Each header needs one real-broker assertion on the dead-letter path.

Basis: DEV-315 Description; recon-DEV-315:99; charter §2.3 items 2 and 4; PRODUCT.md line 10; ADR-0006.
