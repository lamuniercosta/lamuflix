# Feature Specification: DEV-316 StrandedMovieSweeper

**Feature Branch**: `feature/316-spec`

**Created**: 2026-10-08

**Status**: gate1: provisional

**Input**: `specs/DEV-316/brief.md` (Patron Q10 closure; grill CLOSED at Q10/12, no owner structural checkbox)

## User Scenarios & Testing

### User Story 1 - Lost enqueue recovered by the sweep (Priority: P1)

A movie left `Pending` with no queued message (dual-write gap: row persisted, nothing published) is re-enqueued by the periodic sweep through the existing requeue path.

**Why this priority**: Directly satisfies the first acceptance clause (lost/failed enqueue).

**Independent Test**: Persist `Pending` with null `LastAttemptAt` and publish no message against real PostgreSQL + RabbitMQ (Testcontainers) with the actual registered sweeper; observe `EnrichmentRequested(id,1)` on the real queue and prove a subsequent real claim succeeds.

**Acceptance Scenarios**:

1. **Given** a `Pending` row with null `LastAttemptAt` and no published message, **When** the sweep passes, **Then** `EnrichmentRequested(id,1)` is published via `RequeueStrandedMoviesCommand/Handler` and a later real claim succeeds.
2. **Given** the sweep selection query, **When** rows are fresh-leased, exact-boundary, or non-`Pending`, **Then** they are excluded and no mutation occurs.

---

### User Story 2 - Stale claim recovered by the sweep (Priority: P2)

A movie with an expired enrichment claim lease is re-enqueued by the sweep.

**Why this priority**: Satisfies the second acceptance clause (stale claim recovered by the sweep).

**Independent Test**: Make a real claim, advance deterministic time beyond `ClaimLease`, run the actual registered sweeper against real PostgreSQL + RabbitMQ; observe the message on the real queue and prove a new real claim succeeds.

**Acceptance Scenarios**:

1. **Given** a real claim older than `ClaimLease`, **When** the sweep passes, **Then** `EnrichmentRequested(id,1)` is published and a subsequent real claim succeeds.
2. **Given** the sweep alone, **When** asserted before the intentional later claim, **Then** no status, `LastAttemptAt`, or `EnrichmentAttempts` mutation occurred.

---

### User Story 3 - Continuous periodic recovery with clean shutdown (Priority: P3)

The sweep runs periodically in the Api host with serial cadence, error logging, and clean cancellation.

**Why this priority**: Operational contract (Q3/Q4/Q8); not acceptance evidence on its own.

**Independent Test**: Hosted lifecycle tests with timer-capable test-owned `TimeProvider`: immediate first pass, interval delay after success/failure, serial execution, scope disposal, Error-with-exception logging, cancellation forwarding, clean shutdown.

**Acceptance Scenarios**:

1. **Given** Api startup, **When** the host starts, **Then** the first sweep pass runs immediately and each later pass follows one full `SweepInterval` after the previous pass completes.
2. **Given** a pass failure or host shutdown, **When** they occur, **Then** the failure is logged at Error with its exception and retried only at the next interval; shutdown exits cleanly.

---

### Edge Cases

- Equal-boundary lease (`LastAttemptAt == now - ClaimLease`) is excluded (strict `<`).
- Fresh (unexpired) leases and all non-`Pending` states are excluded.
- `Failed` rows are never selected (worker `MarkFailed` exits `Pending`).
- No `EnrichmentAttempts` filter, cap, increment, or mutation; requeue message carries Attempt=1.
- Persistent publish failure or claim-then-crash may recur at lease/interval cadence; no new terminal-state policy.
- Long passes lengthen effective cadence; a failed enqueue aborts the pass and defers remaining IDs.
- Host cancellation flows into query and enqueue; orderly shutdown is not a sweep failure.

## Requirements

### Functional Requirements

- **FR-001**: Sweeper MUST select only rows with `Status == Pending AND (LastAttemptAt == null OR LastAttemptAt < now - ClaimLease)` with strict `<`, where `now` comes from injected `TimeProvider` captured once per Core pass.
- **FR-002**: Selection MUST be a read-only ID query (`FindStrandedMovieIdsAsync(leaseCutoff, ct)` on `IMovieRepository`); it MUST NOT claim, mutate status/attempts/timestamps, or refresh `LastAttemptAt`.
- **FR-003**: Dispatch MUST go through the existing `RequeueStrandedMoviesCommand/Handler` (sole dependency `IEnrichmentQueue`), publishing `EnrichmentRequested(id,1)`; no second enqueue path.
- **FR-004**: Core orchestration MUST live in new `SweepStrandedMoviesCommand/Handler` in `Core/Features/Enrichment`; the infrastructure host MUST be thin (timing, scope, logging only).
- **FR-005**: Hosted loop MUST run serially in Api as `StrandedMovieSweeper` (`Infrastructure/Enrichment/StrandedMovieSweeper.cs`): immediate first pass, fresh DI scope per pass, one `now` per pass, full `SweepInterval` delay via `TimeProvider` after success or failure, no within-instance overlap.
- **FR-006**: Pass exceptions MUST be logged at Error with the exception; retry happens only at the next interval; `stoppingToken` MUST flow into query and enqueue; shutdown exits cleanly.
- **FR-007**: `ClaimLease` and `SweepInterval` MUST be validated positive at startup by sealed `EnrichmentOptionsValidator : IValidateOptions<EnrichmentOptions>` registered alongside existing binding/`ValidateOnStart`; messages name the invalid key; defaults, `MaxAttempts` Range validation, and the EF lease guard are preserved; no upper limit.
- **FR-008**: `StrandedMovieSweeper` MUST be registered unconditionally via `AddHostedService` in Api `Program.cs`; sweep and requeue handlers MUST register via `AddHandler` in Api `HandlerRegistration.cs` preserving decorators; the sweep handler injects the decorated requeue command-handler abstraction; the host resolves the sweep handler interface per fresh scope.
- **FR-009**: `EfMovieRepository` MUST implement the selection as IDs-only, no-tracking query; every listed implementation/test double MUST reflect the new port member.
- **FR-010**: Unrelated Api test hosts MUST remove only the sweeper registration; recovery hosts MUST retain it and assert registration.
- **FR-011**: System MUST add only the bounded file set in `brief.md` (9 new, 10 narrowly scoped edits including the ADR-0004 status note); no Worker, gate, persistence-registration, schema/migration, package/project, endpoint/DTO, web-contract, LocalPlay, secret, or process-execution changes.

### Key Entities

- **Stranded row**: a `Pending` movie row with null `LastAttemptAt` or a strictly expired claim lease; attributes read, never written, by the sweep.
- **Claim lease**: `EnrichmentOptions.ClaimLease` duration; cutoff is `now - ClaimLease`.
- **Requeue command**: existing `RequeueStrandedMoviesCommand` carrying stranded IDs; handler publishes `EnrichmentRequested(id, 1)` per ID.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Lost-enqueue integration test passes on real PostgreSQL + RabbitMQ with the actual registered sweeper (message observed, later real claim succeeds).
- **SC-002**: Stale-claim integration test passes on real PostgreSQL + RabbitMQ with deterministic time advancement (message observed, new real claim succeeds, no sweep mutation before the intentional claim).
- **SC-003**: EF selection matrix passes on a real database (null attempt, strictly expired, fresh excluded, exact boundary excluded, non-`Pending` excluded, no mutation).
- **SC-004**: Unit/lifecycle evidence passes: empty selection, captured cutoff, requeue-path dispatch, cancellation forwarding, failure propagation, immediate pass, interval after success/failure, serial execution, scope disposal, Error logging, clean shutdown.
- **SC-005**: Composition/startup evidence passes: decorated handler registration, unconditional sweeper registration retained on recovery hosts and removed only on unrelated hosts, positive-duration validator (zero/negative per duration, valid defaults, key-specific messages, startup wiring).
- **SC-006**: Review closes with no unresolved Critical/High/Medium in-scope findings and all applicable gates carry actual successful receipts (or the applicable explicitly non-blocking verdict) at the exact reviewed head.

## Assumptions

- Grill Q1-Q10 rulings in `brief.md`/`CONCLUSIONS.md` are authoritative and exhaustive; no open decision remains.
- Existing `MovieId`, command-result, xUnit v3/NSubstitute/Shouldly/AutoFixture conventions are reused; no duplicate domain/transport DTOs.
- Existing database/broker fixtures are reused without rewriting them.
- Existing atomic worker claim absorbs cross-instance duplicate delivery; serial completion-based cadence is deliberate.
- Canonical vocabulary (`Pending`, claim lease, stranded row, requeue) stands; no new glossary term, root `CONTEXT.md` edit, or new ADR.
