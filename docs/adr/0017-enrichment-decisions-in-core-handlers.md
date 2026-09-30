# 0017. Enrichment decisions in Core handlers; transport in the consumer

- Status: Proposed (plan frozen after plan challenge; owner answered D1-D3 on spec PR #54; becomes
  Accepted when the DEV-299 PR merges.)
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
`Core/Domain` next to `EnrichmentFailureCategory`. The handler reuses DEV-300's existing
`EnrichmentOptions` in `LamuFlix.Core.Options`: a plain sealed record with data annotations
(`MaxAttempts`, `SweepInterval`, `ClaimLease`) and no `IOptions`. DEV-299 adds no second options
type in `Core/Pipeline`. The host binds and validates it when it wires the pipeline. The
feature-handler architecture rule allows `LamuFlix.Core.Options` for this, next to
`LamuFlix.Core.Library` (Q13). This is `CONCLUSIONS.md` Q14, which supersedes Q2's `Core.Pipeline`
placement.

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

## Stranded-work requeue: the sweeper does not claim

The ticket says the sweeper "claims stranded Pending movies and re-enqueues them", and taken
literally that livelocks. The sweeper's claim succeeds, so the worker's mandatory claim returns
false and the message is acked without being processed (`CONCLUSIONS.md` Q1).

The owner checked D1 on spec PR #54: "Drop sweeper claim, enqueue directly with existing
EnrichmentRequested." So:

- `RequeueStrandedMoviesCommand(IReadOnlyList<MovieId> MovieIds)` takes the stranded ids from its
  caller. `RequeueStrandedMoviesCommandHandler` depends only on `IEnrichmentQueue`. For each id it
  enqueues `EnrichmentRequested(id, 1)`, and it returns the count.
- The handler does not claim, load or save. The worker's claim stays the only claim, so the
  livelock cannot happen. `EnrichmentRequested` keeps its shape, and no constitution amendment is
  needed.
- Finding the lease-aged `Pending` rows, the timer, and the `SweepInterval`/`ClaimLease` binding
  belong to the sweeper wiring follow-up (D3). The lease and sweep-interval members already exist on
  DEV-300's `EnrichmentOptions`.

The checked text is what governs. It differs from the brief's earlier option (A), "defer the
requeue handler": the handler ships, and only the claim is dropped.

Consequence: every requeue restarts at attempt 1, so a movie that keeps getting stranded can be
requeued without bound, and `MaxAttempts` never applies across sweeps. Bounding that belongs to the
sweeper follow-up, not to this handler.

D2 was answered the same way. `IMovieRepository` gains `NextIdentityAsync(CancellationToken)`, and
import allocates the id before `Movie.Create`.

## Consequences

- Every retry and dead-letter branch is unit-testable with NSubstitute port doubles and no broker,
  which is the constitution's reason for keeping decisions in Core.
- On the retry path the handler does no I/O. A crash between the decision and the republish leaves
  the row `Pending`, and the sweeper recovers it.
- The consumer must switch exhaustively on `EnrichmentFailureAction`. A new action is a breaking
  change for it.
- Registration, validators, `EnrichmentOptions` binding, the consumer and the LocalPlay-gated
  launcher belong to wiring work and must exist before these handlers are used. Deferring them is a
  constitution departure. The owner accepted it as checkbox D3 on spec PR #54: the handlers ship
  unregistered in Core, and the wiring comes in a follow-up. DEV-299 does not deliver them.
