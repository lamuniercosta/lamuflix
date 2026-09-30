# 0017. Enrichment decisions in Core handlers; transport in the consumer

- Status: Proposed (plan frozen after plan challenge; becomes Accepted when the DEV-299 PR merges.
  The *Stranded-work requeue* section stays pending until the owner answers D1.)
- Date: 2026-09-30
- Ticket: DEV-299 (parent DEV-282)

## Context

ADR-0002 chose explicit handlers and decorators, and ADR-0015 put the handler contracts in Core
and the decorators in Infrastructure. DEV-299 adds the use-case handlers for Library, Import,
Enrichment, Watchlist and Playback. None of them may depend on a database or on RabbitMQ.

The constitution's Enrichment Reliability rules (`.specify/memory/constitution.md:445-459`) say
the consumer is thin and every decision lives in Core handlers, so it can be unit-tested without
RabbitMQ. Retry is driven by category: a retryable category below the attempt limit is republished
with `Attempt + 1`, with `RateLimited` going through the TTL retry queue. Anything else is
`MarkFailed` and dead-lettered. Those rules do not say how a Core handler can express "republish"
or "dead-letter" when Core has no queue types. `IEnrichmentQueue` only enqueues; it cannot
dead-letter. The grill rulings are in `specs/DEV-299/CONCLUSIONS.md` (Q2-Q5, Q7, Q12).

## Decision

**Failure handling returns a decision and never publishes.**
`RecordEnrichmentFailureCommand(MovieId, Attempt, EnrichmentFailureCategory)` returns
`EnrichmentFailureDecision(EnrichmentFailureAction Action, int? NextAttempt)`.

- The category's `IsRetryable` is true and `Attempt < EnrichmentOptions.MaxAttempts`: the handler
  returns `Retry`, or `RetryDelayed` for `RateLimited`, with `NextAttempt = Attempt + 1`. It does
  not load, mutate or save the movie, and the row stays `Pending`.
- In every other case it loads the movie, calls `MarkFailed(category, now)`, saves, and returns
  `DeadLetter` with `NextAttempt = null`.
- On every path it writes one structured log entry: movie id, attempt, category and action. It
  never logs exception text.

`EnrichmentFailureAction` is a SmartEnum (`Retry`, `RetryDelayed`, `DeadLetter`), as the constitution requires of a new closed set of values. It lives in
`Core/Domain` next to `EnrichmentFailureCategory`. `EnrichmentOptions` is a plain sealed record in
`Core/Pipeline` with data annotations and no `IOptions`. The host binds and validates it when it
wires the pipeline.

**The consumer owns transport.** Only `EnrichmentConsumer` turns a decision into a republish, a
delayed republish, a dead-letter, or an ack. It also calls the metadata provider and builds the
`MetadataLookupResult`. Core never publishes a retry.

**The other enrichment handlers are thin.**
- Claim returns the bool from `TryClaimForEnrichmentAsync` unchanged.
- Apply handles `Found` (`MarkEnriched`) and `NotFound` (`MarkNotFound`), saves, and returns the
  new status. It rejects `Failed` with `ArgumentException`, because failures go through the
  decision handler.
- Manual retry is allowed only from `NotFound` or `Failed`, and the handler rejects any other status
  before the domain call. It saves first, then enqueues `EnrichmentRequested(id, 1)`. A failed
  publish propagates, and the sweeper is the recovery (constitution 454-455).

**Every handler follows the same shape.**
- Each is a sealed handler over a sealed record in `Features/<Feature>/`. No handler calls another
  handler or references another feature, and none takes more than three ports.
- A missing movie throws a new Core `NotFoundException`, which the single Api `IExceptionHandler`
  maps to 404 `ProblemDetails`. `InvalidTransitionException` propagates from the domain.
- A no-value result is a shared `Unit` record in `Core/Pipeline`.
- Time comes only from an injected `TimeProvider`, and every async port call receives the
  `CancellationToken`.
- Playback calls `IMediaPlayerLauncher` without a `Features:LocalPlay` check. That gate stays in
  the adapter registration and in `DisabledMediaPlayerLauncher`.

## Stranded-work requeue: pending owner D1

This section is not decided. The ticket says the sweeper "claims stranded Pending movies and
re-enqueues them", and taken literally that livelocks. The sweeper's claim succeeds, so the
worker's mandatory claim returns false and the message is acked without being processed
(`CONCLUSIONS.md` Q1). The owner chooses one of three options:

- (A) defer the requeue handler;
- (B) the sweeper lists lease-aged `Pending` rows through a new repository member and enqueues them
  without claiming;
- (C) add a claim token to `EnrichmentRequested`, which needs a constitution amendment.

This ADR records the answer before it becomes Accepted. The lease and sweep-interval members of
`EnrichmentOptions` wait for that answer too.

## Consequences

- Every retry and dead-letter branch is unit-testable with NSubstitute port doubles and no broker,
  which is the constitution's reason for keeping decisions in Core.
- On the retry path the handler does no I/O. A crash between the decision and the republish leaves
  the row `Pending`, and the sweeper recovers it.
- The consumer must switch exhaustively on `EnrichmentFailureAction`. A new action is a breaking
  change for it.
- Registration, validators, `EnrichmentOptions` binding, the consumer and the LocalPlay-gated
  launcher belong to wiring work and must exist before these handlers are used. Deferring them is a constitution departure that the owner accepts or rejects as checkbox D3 on the spec PR. DEV-299 does not
  deliver them.
