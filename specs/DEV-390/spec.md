# Feature Specification: Reconcile Enrichment Attempt Counting

**Feature Branch**: `feature/390-spec`

**Created**: 2026-10-06

**Status**: Draft — gate1: provisional

**Input**: DEV-390 ticket (Scope & Technical Design DEV-390:38-46); `brief.md`, `CONCLUSIONS.md` (Q1-Q6), `ASSUMPTIONS.md`, recon-DEV-390, `specs/PRODUCT.md`

## User Scenarios & Testing

### User Story 1 - One claim plus one outcome records one processing attempt (Priority: P1)

As the enrichment pipeline, when a real atomic claim is followed by a success, not-found, or terminal-failure domain outcome, exactly one processing attempt must be recorded in the persisted counter, so a single attempt is never counted twice.

**Why this priority**: This is the ticket's core defect (DEV-390:39) and acceptance criterion 2 (DEV-390:45).

**Independent Test**: Against real PostgreSQL via Testcontainers, seed a Pending movie with count N > 0, perform a successful claim, apply each of MarkEnriched / MarkNotFound / MarkFailed, save, and reload: persisted `enrichment_attempts` equals N+1 in every case. Cover both a fresh load and an aggregate preloaded before the claim.

**Acceptance Scenarios**:

1. **Given** a Pending movie with persisted count N > 0, **When** a claim succeeds and MarkEnriched is applied and saved, **Then** the reloaded count is N+1.
2. **Given** a Pending movie with persisted count N > 0, **When** a claim succeeds and MarkNotFound is applied and saved, **Then** the reloaded count is N+1.
3. **Given** a Pending movie with persisted count N > 0, **When** a claim succeeds and MarkFailed (terminal) is applied and saved, **Then** the reloaded count is N+1.
4. **Given** an aggregate preloaded before the claim, **When** the claim succeeds and any Mark* outcome is saved, **Then** the reloaded count is still N+1 (claim-sync plus zero domain delta).
5. **Given** a claimed movie, **When** a watchlist-only save occurs, **Then** the claimed count is preserved.

---

### User Story 2 - Retries and manual retries count claims, not messages (Priority: P1)

As the pipeline, retry, lease re-claim, and manual-retry flows must each increment the persisted counter exactly once per successful claim and never via the transport message, so operators can distinguish cumulative claims from the wire sequence ordinal.

**Why this priority**: Ticket acceptance criteria 1 and 3 (DEV-390:44,46): the contract must distinguish transport retry Attempt, and retry/manual-retry tests must prove counter semantics across the real claim/domain seam.

**Independent Test**: Against real PostgreSQL, drive Retry and RetryDelayed decisions, refused/double/concurrent claims, manual retry, and enqueue-only paths, asserting the exact persisted counts below.

**Acceptance Scenarios**:

1. **Given** count N, **When** a Retry or RetryDelayed decision follows the first successful claim without Mark*, **Then** the persisted count stays N+1.
2. **Given** count N+1 after a retry decision, **When** the next successful claim occurs, **Then** the count is N+2, and the final outcome after the second claim stays N+2.
3. **Given** a refused claim, a second refused claim, or a losing concurrent claim, **When** they complete, **Then** no increment occurs beyond the successful winning claims.
4. **Given** a movie in NotFound or Failed with count N, **When** manual retry (RequestEnrichment) saves then enqueues wire Attempt 1, **Then** the count stays N; the next successful manual-retry claim makes it N+1 and its final outcome stays N+1.
5. **Given** an enqueue or stranded requeue alone, **When** no claim occurs, **Then** the count is unchanged.
6. **Given** a movie in any other status, **When** manual retry is requested, **Then** the existing invalid-state rejection is kept.

---

### User Story 3 - Consumers read the documented counter meaning (Priority: P2)

As a consumer of `EnrichmentAttempts`, I read a glossary that states the cumulative-successful-claims meaning, the separate wire Attempt meaning, and the historical-overcount caveat, so I never misinterpret stored values.

**Why this priority**: Ticket requirement DEV-390:42 (monotonic counter, documented meaning); lower priority because it documents rather than fixes behavior.

**Independent Test**: Inspect `CONTEXT.md`: the EnrichmentAttempts entry states cumulative successful claims and the pre-DEV-390 overcount caveat; a separate entry defines transport retry Attempt.

**Acceptance Scenarios**:

1. **Given** the glossary, **When** read, **Then** EnrichmentAttempts is defined as cumulative successful atomic claims, monotonic, never reset, preserved across manual retries.
2. **Given** the glossary, **When** read, **Then** transport retry Attempt is defined as the message sequence ordinal, restarted at 1 by manual retry/requeue and advanced by retry decisions, never copied into the persisted counter.
3. **Given** the glossary, **When** read, **Then** it states pre-DEV-390 values can overcount historical attempts and cannot be retroactively corrected.

---

### Edge Cases

- A claim whose provider work retries, crashes, times out, or never completes still counts as one processing attempt; a later successful lease re-claim adds another one.
- Refused claims (unexpired lease, terminal/absent status, strict lease-expiry boundary) add zero.
- `RequestEnrichment` keeps history and never changes the count; rejected transitions preserve it.
- Lease advancement in tests uses the injected `TimeProvider`; no sleeps.
- No migration, data correction, or backfill: stored historical overcounts remain to preserve monotonicity.

## Requirements

### Functional Requirements

- **FR-001** (Q1): `EnrichmentAttempts` / `enrichment_attempts` is the monotonic cumulative count of successful atomic claims (processing attempts started). New movie starts at 0, never resets. One successful `TryClaimForEnrichmentAsync` adds exactly one; refused claims add zero.
- **FR-002** (Q1, Q2): `MarkEnriched`, `MarkNotFound`, `MarkFailed`, and `RequestEnrichment` add zero to the count. No separate completed-outcome counter is introduced.
- **FR-003** (Q2): Production edit is confined to `src/LamuFlix.Core/Domain/Movie.cs`: remove only `EnrichmentAttempts++` from `BeginPendingAttempt`. Keep `RequirePending` validation, `LastAttemptAt = now`, and all `Mark*` status/metadata/`EnrichedAt`/failure-category behavior unchanged.
- **FR-004** (Q2): Repository claim SQL, D8 claim synchronization, and D2 baseline/delta `ApplyAttempts` behavior stay unchanged (DEV-301 FR-002/FR-006 intact). Loaded-before-claim: DB N to N+1 with synced tracked record; domain outcome leaves aggregate count unchanged, delta zero, persisted N+1 preserved. Loaded-after-claim: aggregate and baseline N+1, outcome changes nothing, N+1 preserved.
- **FR-005** (Q3): `EnrichmentRequested.Attempt` is a transport-only sequence ordinal. Manual retry and stranded requeue enqueue wire Attempt 1. `EnrichmentRetryPolicy` emits attempt+1 under existing `MaxAttempts` semantics. The wire value is never copied into or used to reset `EnrichmentAttempts`.
- **FR-006** (Q3): `RequestEnrichmentCommandHandler` keeps current rules: accepts only NotFound/Failed, preserves cumulative history, saves before enqueue, performs no claim. Each subsequent successful initial, Retry, RetryDelayed, or lease-reclaim claim increments the persisted count once, even without `Mark*`. Enqueue/requeue alone adds zero.
- **FR-007** (Q4): Frozen file set. Production: `Movie.cs` only. Docs: `CONTEXT.md` narrow glossary update only. Tests (counter assertions and accepted-contract cases only): `tests/LamuFlix.Test/Domain/MovieTests.cs`, `tests/LamuFlix.Test/Domain/PropertyTests.cs`, `tests/LamuFlix.UnitTests/Features/Enrichment/{ApplyEnrichmentResultCommandHandlerTests,RecordEnrichmentFailureCommandHandlerTests,ProcessEnrichmentCommandHandlerTests,RequestEnrichmentCommandHandlerTests,RequeueStrandedMoviesCommandHandlerTests}.cs`, `tests/LamuFlix.IntegrationTests/{EfMovieRepositoryTests,EfMovieRepositoryClaimConcurrencyTests,EnrichmentConsumerTests}.cs`. New real-seam cases go in `EfMovieRepositoryTests.cs` using existing fixtures; no new test file or project. Unchanged: repository, handlers, wire record, `LeaseAwareMovieRepository`, migrations/generated models, historical specs/ADRs, public contracts, dependencies, schema, LocalPlay/secrets/`Process.Start`.
- **FR-008** (Q5): Tests-first order after Gate 1: write/update failing exact-value contract tests, then apply the domain fix and glossary update, then full verification. Domain tests prove every legal `Mark*` keeps count N with existing timestamp/state effects; `RequestEnrichment` and rejected transitions keep history; FsCheck transition-table invariant becomes count-unchanged for domain actions. PostgreSQL/Testcontainers tests (never EF/query-translation mocks) prove the exact-count table in the Test Strategy with N > 0, fresh-load and preloaded-claim coverage, watchlist-save preservation, refused/double/concurrent-claim zeros, and `TimeProvider` lease advancement. Keep existing manual-retry rejection and save-before-enqueue ordering; keep arbitrary stored-value round trips.
- **FR-009** (Q5): No migration, data correction, or backfill. Historical overcounts stay stored; future successful claims add one. Consumer documentation states pre-DEV-390 values can overcount and cannot be interpreted as corrected counts.
- **FR-010** (Q6): Loop discipline: closing bar is no unresolved Critical/High or correctness/spec/contract/security/test-seam defect in the frozen diff; review cap two rounds, at most two fix commits per round; third-round unresolved findings go to user review on the PR, never silently waived. Scope is frozen to claim-counter reconciliation, wire/manual-retry meaning, narrow consumer glossary, and Q4-Q5 tests.

### Key Entities

- **Movie (aggregate)**: domain type; `EnrichmentAttempts` is the cumulative successful-claim count defined by FR-001.
- **enrichment_attempts (column)**: persisted form of the counter; not-null integer since DEV-19; no schema change.
- **EnrichmentRequested.Attempt (wire)**: transport-only retry sequence ordinal per FR-005, distinct from the persisted counter.
- **Processing attempt**: work admitted by a successful atomic claim; its counted start needs no completed outcome.
- **Completed outcome**: success, not-found, or terminal-failure domain result; adds no claim count.

## Supersedes

Future behavior under DEV-390 explicitly supersedes the older domain counting contract; historical specs and ADRs remain untouched as delivered history:

- `specs/DEV-294/CONCLUSIONS.md:45,53,57` (each `Mark*` increments `EnrichmentAttempts`).
- `specs/DEV-299/CONCLUSIONS.md:17` (completed-outcome-count wording).
- `specs/DEV-301/brief.md:39` and DEV-301 Q7 domain-count freeze of `BeginPendingAttempt` counting.
- `docs/adr/0017-enrichment-decisions-in-core-handlers.md` completed-outcome terminology (ADR-0017 itself stays immutable).

DEV-301 acceptance (FR-002 atomic claim increment, FR-006 claim sync) stands intact and is not rewritten.

## Success Criteria

- **SC-001**: A real claim followed by success, not-found, or terminal failure records exactly one processing attempt (persisted N to N+1).
- **SC-002**: The accepted contract names the persisted counter semantics and distinguishes transport retry Attempt.
- **SC-003**: Retry and manual-retry tests prove the accepted counter semantics across the real claim/domain seam with exact N/N+1/N+2 values.
- **SC-004**: The cumulative counter is monotonic and its meaning (including the historical-overcount caveat) is documented for consumers.

## Assumptions

- [assumed] Prose term **processing attempt** means work started by a successful atomic claim; **transport retry Attempt** means the message sequence ordinal. Existing property/column names are kept (Patron Q6; ASSUMPTIONS.md).
- No new ADR is required for this M non-ui ticket; supersession is stated here (brief.md).
- Test iteration counts and fixed instants follow existing fixture conventions in the authorized test files.
