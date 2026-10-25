# Feature Specification: DEV-420 Relay outbox rows to RabbitMQ with publisher confirms and remove the direct enqueue

**Feature Branch**: `feature/420-spec`

**Created**: 2026-10-25

**Status**: Draft

**Input**: Ticket DEV-420 (size L, parent DEV-100, backend-only, no ui tag); closed grill brief `specs/DEV-420/brief.md` (Q1–Q12 settled; grill closed; plan-challenge Q7 logging amendment); facts from notes `DEV-420` and `recon-DEV-420`; Patron rulings in `specs/DEV-420/CONCLUSIONS.md`; product authority in `specs/PRODUCT.md`; design authority `docs/adr/0027-transactional-outbox-enrichment.md` and Draft `docs/adr/0028-confirmed-enrichment-outbox-relay.md` in this worktree. No decisions beyond the brief and those rulings. No `design.md` (no ui tag). Quill does not author or accept ADRs (historical Phase 2 step 3 drafts predated ADR-0028); Draft ADR-0028 remains Draft — acceptance, plan freeze, Gate 1, and implementation permission stay separate later steps and are not claimed here.

## Clarifications

### Session 2026-10-25

L-ticket `/speckit-clarify` scan against brief Q1–Q12, CONCLUSIONS, recon-DEV-420, PRODUCT, and ADR-0027. No critical ambiguities remain that require a new Patron ruling. Material answers already encoded below from the grill (settled answers; Q1–Q12 not reopened):

- Q: Prerequisite DEV-329 contracts → A: Adopt four-column `outbox_messages`, pending index, `IEnrichmentOutbox.Stage(EnrichmentRequested)`, scoped `EfEnrichmentOutbox`, Web JSON UTF-8 `bytea` payload, and Stage → shared save atomicity unchanged; never invent substitutes; require landed DEV-329 and DEV-419 pickup drift verification before Phase B build (brief Q1–Q2).
- Q: Relay persistence and publish lifecycle → A: One non-overlapping Infrastructure `OutboxRelayHostedService` registered from Api; fresh DI scope per pass; bounded `(created_at, id)` ordered unprocessed reads; sequential confirmed mandatory `IEnrichmentQueue` publish; per-row `ProcessedAt` only after settled confirm; no Core relay port, schema, or transaction across broker waits (brief Q3).
- Q: Failure, poison, and shutdown → A: Abort on first deserialize/publish/marker/DB failure; preserve committed prefix; never mark uncertain rows; capped exponential backoff via TimeProvider; invalid oldest payload blocks later progress and surfaces in health/backlog; clean shutdown without synthetic failure or retry delay (brief Q4).
- Q: Options, retention, health/metrics → A: Validated `OutboxRelay` options with approved defaults/bounds; independent bounded processed-only retention; Ready-tagged `outbox-relay` on existing readiness surface; six low-cardinality instruments with names defined as `const` members in existing `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` and consumed by Infrastructure `OutboxRelayMetrics` (no local telemetry-name literals); withhold invalid/failed backlog gauges; successful empty sample yields count/age zero; a later successful pass or cleanup clears only its respective fault; structured failure logs allow only outbox row id when row-scoped, exception type name, and stable internal error category — never exception objects, raw Message/ToString()/stack/inner-exception text, payload-derived values, or sensitive configuration (brief Q5–Q7, Q12; post-grill C1; plan-challenge Q7 / CONCLUSIONS.md:233-236).
- Q: Acceptance proofs, file boundary, ordering, loop discipline → A: Real Postgres/RabbitMQ plus controlled publisher faults and deterministic TimeProvider tests (including no-overlapping-passes and cutover both-or-neither rollback proofs); exact 24-file Q9 boundary including `TelemetryConstants.cs` as file 24 solely for the six Q7 constants (post-grill C1 / CONCLUSIONS.md:218-221); seven-step Q10 task order with constants before `OutboxRelayMetrics` consumer; Critical/High/Medium closing bar; two-round plan and code/ship review caps with full three-axis evidence (brief Q8–Q11). Draft ADR-0028 records settled relay decisions and remains Draft until separately accepted.

Canonical feature-local vocabulary and at-least-once/per-batch ordering semantics are further encoded in Domain vocabulary, Requirements, and Edge Cases from brief Q12.

## Domain vocabulary *(mandatory — feature-local operational terms)*

Canonical state terminology for this ticket (brief Q12). These disambiguate ADR informal wording without editing root `CONTEXT.md`:

- **staged**: Tracked in memory by `IEnrichmentOutbox.Stage` before the shared save.
- **durable**: Committed in PostgreSQL with the movie write.
- **unprocessed**: `ProcessedAt IS NULL`.
- **confirmed**: Broker acceptance settled through the existing confirmed mandatory publish path.
- **processed**: Post-confirm marker durably committed in PostgreSQL.
- **expired processed**: `ProcessedAt` strictly older than the retention cutoff.

Consumed vocabulary (already in use; not redefined): **EnrichmentRequested** (`MovieId`, `Attempt`), **path A** (EfMovieRepository no-metadata save path), **direct enqueue** (existing `IEnrichmentQueue` publish after successful save — removed only from the two cutover handlers).

Delivery semantics (brief Q12): at-least-once is deliberate; ordering is per pending batch query only — never exactly-once or global FIFO.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Confirmed relay of durable unprocessed outbox rows (Priority: P1)

A single Api-hosted Infrastructure relay batch-polls durable unprocessed `outbox_messages`, deserializes each stored EnrichmentRequested payload with the approved Web JSON contract, publishes sequentially through the existing confirmed mandatory queue path, and commits ProcessedAt for that row only after the confirm settles — before proceeding to the next row. Already processed rows are not republished on later passes or restart. Query ordering is `(CreatedAt, Id)` within each batch, capped by BatchSize.

**Why this priority**: Ticket scope DEV-420:27-28 and brief Q3 require confirmed relay publication with per-row markers as the core delivery.

**Independent Test**: Real PostgreSQL + RabbitMQ integration proves end-to-end publish with wire-parity MovieId/Attempt, ProcessedAt only after confirm, batch order/limit, and restart idempotence for marked rows (brief Q8).

**Acceptance Scenarios**:

1. **Given** durable unprocessed outbox rows, **When** a relay pass runs, **Then** at most BatchSize rows are materialized ordered by `(CreatedAt, Id)`, each is published through the confirmed mandatory path, and ProcessedAt is set from injected TimeProvider and saved individually only after that row's confirm settles.
2. **Given** a row whose ProcessedAt is already committed, **When** a later pass or host restart runs, **Then** that row is not republished under normal polling.
3. **Given** stored payload bytes from DEV-329 Web JSON serialization, **When** the relay deserializes and publishes, **Then** MovieId and Attempt wire parity is preserved (semantic EnrichmentRequested identity).
4. **Given** a confirmed publish whose marker commit then fails or is uncertain, **When** a later fresh-scope pass runs, **Then** the row may be published again (at-least-once); enrichment claims absorb duplicates.
5. **Given** concurrent relays on multiple Api instances, **When** this ticket is evaluated, **Then** multi-instance coordination remains out of scope (ticket exclusion; one Api instance owns the relay operationally).
6. **Given** a relay pass already in flight when the next due time elapses, **When** the scheduler would otherwise start another pass, **Then** a second overlapping pass does not start (brief Q8 item 5; FR-001).

---

### User Story 2 - Failure abort, backoff, poison head-of-line, and clean shutdown (Priority: P2)

The first deserialize, publish/confirm, marker-save, or database failure aborts the current pass, disposes the scope, preserves already committed markers, and leaves uncertain rows unprocessed. Retries use a fresh ordered query after capped exponential delay. An invalid oldest payload blocks later progress and surfaces via health/backlog telemetry — no skip, drop, quarantine, dead lettering, or retry columns. Host shutdown cancellation exits without a synthetic failed pass or retry delay.

**Why this priority**: Brief Q4 and ADR-0027 require never marking on uncertain outcomes and prefer head-of-line blocking over silent data loss.

**Independent Test**: Controlled publisher faults against real DB plus deterministic TimeProvider unit tests for abort/backoff/saturation/reset, timeout vs shutdown, and poison-row progress failure (brief Q8).

**Acceptance Scenarios**:

1. **Given** nack, mandatory return, publish timeout, connection failure, or cancellation before confirmation, **When** the pass ends, **Then** the affected row remains unprocessed (`ProcessedAt IS NULL`) and no later row in that pass is advanced.
2. **Given** a mid-batch failure after some markers committed, **When** the pass aborts, **Then** the committed prefix is preserved and a later pass resumes from a fresh ordered query.
3. **Given** consecutive failures, **When** delays are applied, **Then** delay is `FailureBackoffMin * 2^(failures - 1)` saturated at `FailureBackoffMax` via TimeProvider with no jitter; the sequence resets only after a successful pass including a successful empty poll.
4. **Given** an unprocessable oldest durable payload, **When** relay attempts continue, **Then** later rows are not silently advanced and the failure is observable in readiness/backlog telemetry.
5. **Given** host shutdown cancellation, **When** the worker exits, **Then** it does so without a synthetic failed-pass outcome or retry delay; shutdown is distinguished from PublishTimeout.

---

### User Story 3 - Bounded retention of expired processed rows (Priority: P3)

Within the same non-overlapping worker, an independently scheduled cleanup deletes at most CleanupBatchSize processed rows ordered by `(ProcessedAt, Id)` where ProcessedAt is strictly older than now minus ProcessedRetention. Unprocessed rows and cutoff-equal ProcessedAt values are never deleted. Cleanup failure is isolated from publication.

**Why this priority**: Ticket retention requirement and brief Q6 assign processed-row retention to this ticket without schema changes.

**Independent Test**: Real PostgreSQL retention tests prove survivors (unprocessed, recent/future, cutoff-equal), bounded ordered deletion, CreatedAt-irrelevance, and no schema delta (brief Q8).

**Acceptance Scenarios**:

1. **Given** processed rows older than the retention cutoff, **When** a due cleanup runs, **Then** at most CleanupBatchSize rows ordered by `(ProcessedAt, Id)` are deleted and the delete predicate rechecks ProcessedAt against the cutoff.
2. **Given** unprocessed rows or ProcessedAt equal to the cutoff, **When** cleanup runs, **Then** those rows remain.
3. **Given** a cleanup failure, **When** publication is next due, **Then** markers and later publication are not undone or blocked; cleanup retries at the next CleanupInterval (not a tight loop).
4. **Given** a large eligible backlog, **When** cleanup runs across intervals, **Then** it drains across bounded scheduled runs (retention age is eligibility, not a hard maximum storage age).

---

### User Story 4 - Relay readiness, backlog metrics, and host registration (Priority: P4)

`OutboxRelayHealthCheck` registers as `outbox-relay` with the existing Ready tag on the existing readiness endpoint. Thread-safe `OutboxRelayState` drives health precedence and six low-cardinality instruments without DB I/O in health/metric callbacks. Api `Program.cs` registers `AddLamuFlixOutboxRelay`. The sweeper remains registered.

**Why this priority**: Ticket health/backlog requirement and brief Q7 fix observability without new routes or DTOs.

**Independent Test**: Unit health/metric proofs plus ApiHostCompositionTests and HealthEndpointTests for registration, mapping, and sweeper retention (brief Q8).

**Acceptance Scenarios**:

1. **Given** worker lifecycle and backlog samples, **When** readiness is queried, **Then** status is Unhealthy before the first successful sample, after failed pass/sample, or on unexpected worker stop; otherwise Degraded if oldest pending age ≥ BacklogAgeWarning or latest cleanup failed; otherwise Healthy — using existing HTTP mapping for Degraded.
2. **Given** a failed or invalid backlog sample, **When** gauges are read, **Then** pending count and oldest-pending-age measurements are withheld (never stale-as-current or false zero); counters retain legitimate history.
3. **Given** a successful empty backlog sample, **When** gauges are read, **Then** pending count and oldest-pending-age are zero (brief Q7; ADR-0028).
4. **Given** a failed pass or sample followed later by a successful publication pass (including successful empty poll), **When** readiness is queried, **Then** that pass/sample fault is cleared; a later successful cleanup clears only the cleanup fault and does not clear a pass fault, nor does pass success clear a cleanup fault (brief Q7; ADR-0028).
5. **Given** Api host composition, **When** the host starts, **Then** exactly one hosted relay and `outbox-relay` Ready check are registered, and the sweeper remains present.
6. **Given** structured relay/cleanup failure logs, **When** a failure is recorded, **Then** allowed fields are only outbox row id when row-scoped, exception type name, and a stable internal error category; logger calls never receive an exception object; raw Message/ToString()/stack/inner-exception text, payload-derived values, and sensitive connection/configuration details are prohibited; rowless failures do not invent a row id; metric labels exclude MovieId, payload, and exception-message content (brief Q7 plan-challenge amendment; CONCLUSIONS.md:233-236).

---

### User Story 5 - Cut over ImportMovieFolder and RequestEnrichment off direct enqueue (Priority: P5)

Remove only the `IEnrichmentQueue` dependency and direct `EnqueueAsync` call from `ImportMovieFolderCommandHandler` and `RequestEnrichmentCommandHandler`. Preserve Stage before the shared path-A save, both-or-neither movie/outbox atomicity, and failure propagation. A successful durable save succeeds regardless of current broker availability; the relay delivers later. Other enqueue callers and sweeper behavior stay unchanged.

**Why this priority**: Ticket cutover requirement and brief Q2 confine direct-publish removal to the two named handlers after relay proofs exist (brief Q10 step 6).

**Independent Test**: For each cutover handler: broker unavailable at request time → movie+outbox commit without direct enqueue; broker returns → relay delivers with wire parity and ProcessedAt after confirm; authorized unit/endpoint/persistence/factory tests adjusted (brief Q8).

**Acceptance Scenarios**:

1. **Given** ImportMovieFolder or RequestEnrichment with broker unavailable, **When** the handler completes a successful path-A save, **Then** movie and outbox are durable, no direct enqueue occurred, and the request succeeds.
2. **Given** those durable unprocessed rows and a restored broker, **When** the relay runs, **Then** EnrichmentRequested is published with correct MovieId/Attempt and ProcessedAt is committed only after confirm.
3. **Given** a known pre-commit failure or explicit rollback on either cutover handler against real PostgreSQL, **When** the handler exits, **Then** movie write and outbox insert are both absent (both-or-neither); Stage/serialization/save exceptions and cancellation still propagate; an uncertain save completion is not treated as proof of rollback (brief Q2/Q8 item 2; recon-DEV-420 staging atomicity).
4. **Given** MatchMovieImdbId, ProcessEnrichment, RequeueStrandedMovies, SweepMissingMovieFeatures, and sweeper behavior, **When** this ticket ships, **Then** those paths remain outside the cutover.
5. **Given** public routes, DTO fields, and status-code definitions, **When** this ticket ships, **Then** they are unchanged.

### Edge Cases

- Concurrent relays on multiple Api instances remain excluded (ticket; brief Q3).
- No batch-wide marking; no DB transaction/connection held across broker waits (brief Q3).
- Marker save uses the host shutdown token, never an already-expired publish timeout token (brief Q5).
- Polling starts immediately; PollInterval applies after every successful pass including empty/full batches (brief Q5).
- Cleanup is first due at startup and then every CleanupInterval even when publishing failed; separate fresh DB scope (brief Q6).
- Real relay tests must exercise the worker; removing it from a test host cannot count as delivery proof (brief Q9).
- Reuse available deterministic TimeProvider test support; no new package (brief Q8–Q9, Q12).
- Phase B STOP: verify landed DEV-329 schema/Stage/payload/save contracts and DEV-419 ADR against current main; resolve pickup drift before Build (brief Q2, Q10).
- No publisher/queue-port, Stage adapter, DbContext/entity/configuration, migration, movie-repository, other-caller, project/package, harness, generated contract/frontend, secrets, or LocalPlay edits (brief Q9).
- No root CONTEXT.md or constitution edit; historical constitution wording stays with the separate follow-up named in ADR-0027 (brief Q12).
- No Gherkin/Reqnroll. Draft ADR-0028 exists and remains Draft; ADR acceptance, plan freeze, and Gate 1 remain separate later steps (brief Closure; historical Quill drafts did not author the ADR).
- Closing bar: verified in-scope Critical/High/Medium block; Low recorded without required fix commits; speculative abstraction follows charter follow-up policy (brief Q11).
- Round cap: two plan-challenge rounds and two code/ship-review rounds; grill already closed at 12/12 (brief Q11–Q12).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Deliver one non-overlapping `OutboxRelayHostedService` in existing `LamuFlix.Infrastructure` Enrichment folder, registered from Api via `AddLamuFlixOutboxRelay` in `Program.cs`, with options, state, metrics, and Ready-tagged `outbox-relay` health check. An in-flight pass prevents a second overlapping pass despite elapsed due time. One Api instance owns the relay; no multi-instance protocol (brief Q3, Q5, Q7, Q8 item 5, Q9; DEV-420:28).
- **FR-002**: Each pass creates a fresh DI scope/`LamuFlixDbContext`, materializes at most `BatchSize` durable unprocessed `OutboxMessageRecord` rows ordered by `(CreatedAt, Id)`, deserializes payloads with DEV-329 `JsonSerializerDefaults.Web` into `EnrichmentRequested`, publishes sequentially through existing confirmed mandatory `IEnrichmentQueue`, and sets/saves `ProcessedAt` from injected `TimeProvider` per row only after settled confirm — without holding a DB transaction across broker waits (brief Q3).
- **FR-003**: Abort the pass on the first deserialize, publish/confirm, marker-save, or database failure; dispose the scope; preserve committed markers; never mark uncertain outcomes; retry from a fresh ordered query after capped exponential backoff; exit cleanly on shutdown cancellation; never skip/drop/quarantine/add columns for poison rows (brief Q4).
- **FR-004**: Bind and startup-validate `OutboxRelayOptions` under section `OutboxRelay` with approved defaults/bounds: BatchSize 100 (1..1000), PollInterval 1s > 0, PublishTimeout 30s > 0, FailureBackoffMin 1s > 0, FailureBackoffMax 60s ≥ min, ProcessedRetention 7d > 0, CleanupInterval 1h > 0, CleanupBatchSize 1000 (1..10000), BacklogAgeWarning 5m > 0. Immediate start; PollInterval after successful passes; backoff `min * 2^(failures-1)` capped at max; no jitter; no enable/disable toggle; linked shutdown+PublishTimeout for publish; host token for marker save (brief Q5).
- **FR-005**: Retention cleanup runs inside the same worker on an independent TimeProvider schedule (first due at startup, then every CleanupInterval, even after publish failure) in a separate fresh scope; delete at most CleanupBatchSize rows ordered by `(ProcessedAt, Id)` where `ProcessedAt IS NOT NULL AND ProcessedAt < now - ProcessedRetention` (rechecked in delete); never delete unprocessed or filter by CreatedAt; isolate cleanup errors from publication (brief Q6).
- **FR-006**: Maintain thread-safe `OutboxRelayState`; sample backlog after successful publication passes in a fresh scope (sample failure fails the pass); health/metric callbacks do no DB I/O; health precedence Unhealthy → Degraded → Healthy as ruled; a successful empty sample yields count/age zero; a later successful pass or cleanup clears only its respective fault (pass success never clears cleanup fault and cleanup success never clears pass fault); define the six instrument names `lamuflix.outbox.pending`, `lamuflix.outbox.oldest_pending_age`, `lamuflix.outbox.published`, `lamuflix.outbox.failures`, `lamuflix.outbox.retention.deleted`, `lamuflix.outbox.retention.failures` as `const` members in existing `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` and export them from Infrastructure `OutboxRelayMetrics` by consuming those constants (no local telemetry-name literals); withhold invalid/failed backlog gauges; no MovieId/payload/exception-message labels; structured failure logs allow only outbox row id when row-scoped, exception type name, and stable internal error category — never logger exception-object arguments, raw Message/ToString()/stack/inner-exception text, payload-derived values, or sensitive connection/configuration details; rowless failures do not invent a row id (brief Q7, Q12; post-grill C1; plan-challenge Q7 / CONCLUSIONS.md:233-236; Draft ADR-0028).
- **FR-007**: Remove only `IEnrichmentQueue` dependency and direct `EnqueueAsync` from `ImportMovieFolderCommandHandler` and `RequestEnrichmentCommandHandler`; preserve Stage before shared path-A save, both-or-neither movie/outbox atomicity on known pre-commit failure/explicit rollback, Stage/serialization/save exception and cancellation propagation, and the uncertain-save distinction (uncertain completion is not proof of rollback); leave other enqueue callers and sweeper unchanged; no public API shape change; no new transaction coordinator (brief Q2, Q8 item 2, Q9).
- **FR-008**: Adopt DEV-329 schema/staging contracts unchanged (four columns + pending index; `IEnrichmentOutbox.Stage`; scoped `EfEnrichmentOutbox`; Web JSON UTF-8 payload). No schema/migration/entity/configuration edits in this ticket. Phase B verifies landed DEV-329 and DEV-419 against main before Build (brief Q1–Q2, Q10).
- **FR-009**: Exact Q9 24-file production/test delivery boundary only (plus Phase A `specs/DEV-420/` docs and Draft ADR-0028 as a separate Phase A document), including existing `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` as file 24 solely for the six Q7 metric-name constants (post-grill C1 / CONCLUSIONS.md:218-221). Additional files need a cited Patron scope ruling before edit (brief Q9).
- **FR-010**: Test strategy per brief Q8: real Postgres+RabbitMQ end-to-end cutover/relay proofs; real-DB batch order/limit/fresh-scope recovery and cutover both-or-neither rollback for both handlers; controlled publisher faults with real DB plus focused real-broker mandatory-return; real-DB retention; deterministic TimeProvider unit tests including no-overlapping-passes and failure-log allowlist sentinel/rowless proofs; state/metric/composition/health proofs including empty-sample zeros and independent fault clearing. Existing packages/patterns only; never mock the data-access driver for query translation/serialization.
- **FR-011**: Dependency ordering per brief Q10: (1) pre-build landed-contract/drift verification; (2) options + deterministic/state/time tests and real DB/broker harness; (3) confirmed per-row publish/markers + failure recovery; (4) isolated retention; (5) six centralized Q7 metric-name constants in `TelemetryConstants.cs` before Infrastructure `OutboxRelayMetrics` consumer, then state/metrics/readiness + host registration; (6) two-handler cutover + authorized test/factory adjustments; (7) Phase B gates/review. No Gherkin/Reqnroll in this ticket.
- **FR-012**: Gate expectations per brief Q10: Phase A drafts only; Phase B runs configured analyzers/complexity/InspectCode on exact changed files, relevant then full tests, property/security/format checks, and pre-PR mutation via `scripts/run-mutation.ps1` without `--since`. Live harness thresholds only; never lower thresholds or hand-edit generated gate config. Report PASS / SKIPPED (scope-empty) / SKIP / N/A / Could not run separately — never fold skips into PASS; build alone is no substitute.
- **FR-013**: Loop discipline per brief Q11–Q12: verified in-scope Critical/High/Medium findings block plan freeze and later code/ship-review clearance; Low recorded without required fix commits; out-of-scope/speculative items follow charter follow-up (`noted, no ticket` unless eligible). Two plan-challenge rounds and two code/ship-review rounds; each round needs complete Sentry/Ledger/Compass evidence on the exact target HEAD. Frozen scope: Q2–Q10 decisions and exact 24-file Q9 boundary (including post-grill C1 `TelemetryConstants.cs`); anything else is a follow-up issue, not a finding in this round.
- **FR-014**: Charter care-list: no new NuGet/npm dependency; no new project/top-level folder/architectural layer; no schema beyond adopted DEV-329 `outbox_messages`; no public API shape change; no Features:LocalPlay/secrets/Process.Start; Q9 forced host/test rewrites under §2.3 item 6. No owner structural checkbox (brief Q12).

### Key Entities

- **OutboxMessageRecord**: Existing Infrastructure record (`Id`, `Payload`, `CreatedAt`, `ProcessedAt`) consumed unchanged for relay reads/markers and retention deletes.
- **EnrichmentRequested**: Existing typed broker payload (`MovieId`, `Attempt`) deserialized from stored Web JSON UTF-8 bytes.
- **OutboxRelayOptions**: Validated operational settings under configuration section `OutboxRelay`.
- **OutboxRelayState**: Thread-safe worker-owned snapshot for lifecycle, pass outcome, backlog sample, and cleanup outcome.
- **OutboxRelayMetrics**: Six approved low-cardinality instruments whose names are consumed from `TelemetryConstants` (no local telemetry-name literals).
- **TelemetryConstants**: Existing Core central metric-name constants; this ticket adds only the six Q7 `lamuflix.outbox.*` `const` members (post-grill C1 / Q9 file 24).
- **OutboxRelayHealthCheck**: Ready-tagged `outbox-relay` check on the existing readiness surface.
- **OutboxRelayHostedService**: Single non-overlapping background worker owning publish passes and retention cleanup.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Durable unprocessed rows are published through the confirmed mandatory path with MovieId/Attempt wire parity, and ProcessedAt is committed only after that confirm settles.
- **SC-002**: Already marked rows are not republished after restart; a confirmed-then-failed marker may redeliver (at-least-once) without inventing exactly-once semantics.
- **SC-003**: First-failure abort preserves committed prefix; nack/return/timeout/connection/pre-confirm cancellation leave rows unprocessed; poison oldest row blocks silent advancement and is visible in readiness/backlog.
- **SC-004**: Retention deletes only strictly older processed rows in bounded `(ProcessedAt, Id)` batches; unprocessed and cutoff-equal rows survive; no schema delta.
- **SC-005**: Readiness follows Unhealthy/Degraded/Healthy precedence; failed samples withhold backlog gauges; a successful empty sample reports count/age zero; respective pass and cleanup faults clear independently; failure logs obey the Q7 allowlist; exactly one relay/check is registered and sweeper remains.
- **SC-006**: Each cutover handler commits movie+outbox without direct enqueue while the broker is down; known pre-commit failure/explicit rollback leaves neither durable change; after broker recovery the relay delivers and marks processed.
- **SC-007**: Delivery stays inside the exact 24-file Q9 boundary (including `TelemetryConstants.cs` solely for the six Q7 constants); Phase B gate verdicts are reported with correct PASS/SKIPPED/SKIP/N/A/Could not run labels without threshold changes.

## Assumptions

- Approved DEV-329 packet at `feature/329-spec` tip `d85978f9957a321137626ccb43743c384869bbb7` (PR #256 per Conductor) is the design contract source; Phase B pickup must still verify landing on main and drift before Build (brief Identity; Q2, Q10).
- ADR-0027 in this worktree is accepted design authority for confirmed marking, at-least-once, one Api relay, and retention window. Draft ADR-0028 elaborates the DEV-420 relay follow-up while preserving those guarantees and this vocabulary; it remains Draft until separately accepted (brief Closure).
- No taste rulings were made; no `ASSUMPTIONS.md` is required (brief Ticket delivery).
- Brief grill is closed at 12/12; plan-challenge correction, freeze, Gate 1, implementation, review, and merge remain separate later steps (brief Closure).
