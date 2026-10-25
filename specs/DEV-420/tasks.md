# Tasks: DEV-420 Relay outbox rows to RabbitMQ with publisher confirms and remove the direct enqueue

**Input**: Design documents from `specs/DEV-420/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`) and Draft `docs/adr/0028-confirmed-enrichment-outbox-relay.md` (remains Draft; not accepted by this packet).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why). No `design.md` (no ui tag). Code phases also require the Phase B entry checklist below. Missing Phase B evidence blocks T001 production work.

**Tests**: Included. Brief Q8 requires real Postgres/RabbitMQ proofs, controlled publisher faults, deterministic TimeProvider unit tests, and composition/health proofs. Author tests in the same phase as the surfaces they lock (TDD). Compile-red is permitted while types are introduced inside a phase; the phase is not done until its named targeted proofs are green. Do not start Code Phase 1 until Phase B entry clears landed DEV-329/DEV-419 and drift, the spec PR has merged, and Gate 1 is established.

**Organization**: Phases follow brief Q10 dependency ordering and `spec.md` user stories US1–US5. Anvil implements one code phase per ask. Polish is preliminary verification, Cog `/refactor`, then final post-refactor build/format/full tests before Gauge gates.

**Domain vocabulary** (feature-local; do not edit CONTEXT.md): **staged**, **durable**, **unprocessed**, **confirmed**, **processed**, **expired processed** — definitions in `spec.md` Domain vocabulary.

**Exact file boundary**: Only the Q9 24-file paths listed in `plan.md` / brief Q9 (including `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` as file 24 solely for the six Q7 constants — post-grill C1). Outside delivery: publisher/queue port, Stage adapter, DbContext/entity/configuration, migrations, movie repository, other enqueue callers, API contracts, web/, packages, harness; no further `TelemetryConstants.cs` edits beyond those six `const` members.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 confirmed relay; US2 failure/backoff/shutdown; US3 retention; US4 health/metrics/host; US5 handler cutover.
- Exact file paths are in each description.

## Phase B entry (before Code Phase 1)

**Purpose**: Confirm landed prerequisite contracts and Q9 file-set drift before production work. Do not run gates, tests, or analyzers from Spec.

**Independent Test**: Every checklist item below has recorded evidence in the task note (or named owner handoff). Any missing item stops code.

- [ ] TB000 Confirm the spec PR has merged and Gate 1 is established (Rigger / YouTrack `Ready to build` handoff). Checklist mark must reference the recorded receipt.
- [ ] TB001 Wisp Phase B pickup/drift recon against picked HEAD: verify landed DEV-329 `outbox_messages` schema/Stage/adapter/payload/save contracts and landed DEV-419 ADR-0027 on main; reconcile against brief Q2–Q8 and the Q9 24-file boundary (including `TelemetryConstants.cs`); note any main drift that would change relay/cutover/host/constants surfaces. Keel supplies pickup/drift clearance. **STOP** for a cited decision on missing prerequisite landing or any contract/file-boundary mismatch rather than inventing a substitute. No production edits in this task.
- [ ] TB002 Obtain Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef`) when Build intake requires them; record in `DEV-420`.
- [ ] TB003 Keel checks or amends drift against the brief/spec before any Code Phase 1 edit. Missing Keel drift check blocks T001.

**Checkpoint**: Prerequisites match the brief; Phase B build intake is allowed.

---

## Phase 1: Setup — options, state, and proof harness (Q10 step 2)

**Purpose**: Validated options, thread-safe state scaffolding, deterministic unit harness, and real DB/broker relay-proof fixtures using existing support. No confirmed publish loop or cutover yet beyond what harness scaffolding requires.

**Independent Test**: Options validation tests and TimeProvider-backed harness compile; Testcontainers fixtures reuse existing packages.

### Tests

- [ ] T001 [P] [US1] Add `tests/LamuFlix.UnitTests/Enrichment/OutboxRelayOptionsTests.cs` proving defaults/bounds/startup validation for all `OutboxRelayOptions` fields (brief Q5). AAA headers only. Existing assertion libraries only.
- [ ] T002 [P] [US1] [US2] Scaffold deterministic TimeProvider-backed harness in `tests/LamuFlix.UnitTests/Enrichment/OutboxRelayHostedServiceTests.cs` for immediate polling, success delays, backoff saturation/reset, linked timeout vs shutdown, clean cancellation, and no-overlapping-passes mapped to FR-001 (in-flight pass prevents a second pass despite elapsed due time; fail red until worker implements). Reuse available TimeProvider test support; no new package; no wall-clock sleeps.
- [ ] T003 [P] [US1] Scaffold real Postgres + RabbitMQ integration harness in `tests/LamuFlix.IntegrationTests/OutboxRelayTests.cs` using existing shared fixtures/packages (fail red until relay implements). Never mock the data-access driver.

### Implementation

- [ ] T004 [P] [US1] Add `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayOptions.cs` with approved defaults/bounds and startup validation under section `OutboxRelay`.
- [ ] T005 [P] [US4] Add `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayState.cs` as a thread-safe worker snapshot (lifecycle, pass outcome, backlog sample, cleanup outcome).
- [ ] T006 [US1] Add `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayServiceCollectionExtensions.cs` registering options/state (worker/metrics/health wiring completes in later phases). Drive T001 green.

**Checkpoint**: Options validation green; harnesses compile and fail for unimplemented behavior where expected.

---

## Phase 2: Foundational — confirmed per-row publish, markers, and failure recovery (Q10 step 3)

**Purpose**: Bounded ordered relay pass with sequential confirmed mandatory publish, per-row post-confirm markers, abort-first failure, and fresh-scope recovery. Blocks retention/health/cutover phases that depend on a working publish loop.

**⚠️ CRITICAL**: No handler cutover until later Phase 5. Do not edit Stage adapter, DbContext, migrations, or publisher port.

### Tests

- [ ] T007 [P] [US1] [US2] Extend `tests/LamuFlix.IntegrationTests/OutboxRelayTests.cs` for: batch order by CreatedAt then Id; BatchSize bound; skip already marked rows; ProcessedAt only after settled confirm; MovieId/Attempt wire parity; restart does not republish marked rows; fresh-scope recovery after failed/uncertain marker; controlled publisher faults (pre-confirm marker absent; nack/return/timeout/connection leave unprocessed; confirmed-then-marker-failure permits retry; committed prefix survives mid-batch failure); focused real-broker mandatory-return proof. AAA headers only.
- [ ] T008 [P] [US2] [US4] Extend `tests/LamuFlix.UnitTests/Enrichment/OutboxRelayHostedServiceTests.cs` for abort-on-first-failure, poison oldest-row head-of-line blocking, backoff saturation/reset including successful empty poll, shutdown without synthetic failure/retry delay, and Q7 failure-log allowlist proofs: payload/secret sentinel values planted in exception and inner-exception content must not reach captured structured logs; no logger exception-object argument; rowless failures do not invent a row id; allowed fields remain only row id when row-scoped, exception type name, and stable internal error category (brief plan-challenge Q7; CONCLUSIONS.md:233-236).

### Implementation

- [ ] T009 [US1] [US2] Implement `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayHostedService.cs` publish loop: fresh DI scope per pass; materialize ≤ BatchSize unprocessed rows ordered `(CreatedAt, Id)`; Web JSON deserialize to EnrichmentRequested; sequential confirmed mandatory `IEnrichmentQueue` publish with linked shutdown+PublishTimeout; set/save ProcessedAt via TimeProvider per row after confirm; abort-first failure; capped exponential backoff; clean shutdown. No DB transaction across broker waits. No schema/Stage/publisher edits.
- [ ] T010 [US1] Drive T002/T007/T008 green for publish/failure surfaces (composition registration may still be incomplete until Phase 4).

**Checkpoint**: Named relay publish/failure proofs green against real DB/broker where required.

---

## Phase 3: User Story 3 — isolated bounded retention (Q10 step 4)

**Goal**: Independent due cleanup inside the same worker deletes only expired processed rows in bounded `(ProcessedAt, Id)` batches.

**Independent Test**: Real Postgres retention suite proves survivors and bounded deletion.

### Tests

- [ ] T011 [P] [US3] Add `tests/LamuFlix.IntegrationTests/OutboxRetentionTests.cs` proving: preserve all unprocessed, recent/future ProcessedAt, and cutoff-equal rows; delete only strictly older processed rows in bounded ordered batches; CreatedAt irrelevant; no schema delta; cleanup failure isolation and next-interval retry semantics observable without blocking publication. AAA headers only.
- [ ] T012 [P] [US3] Extend `tests/LamuFlix.UnitTests/Enrichment/OutboxRelayHostedServiceTests.cs` for due cleanup at startup/interval even after publish failure, separate-scope cleanup, and failure isolation.

### Implementation

- [ ] T013 [US3] Extend `OutboxRelayHostedService` retention path per brief Q6 (independent schedule; separate fresh scope; CleanupBatchSize; rechecked cutoff predicate; never delete unprocessed). Drive T011–T012 green.

**Checkpoint**: Retention proofs green; publish proofs from Phase 2 still green.

---

## Phase 4: User Story 4 — health, metrics, and Api registration (Q10 step 5)

**Goal**: Cached readiness/backlog telemetry, six instruments, Api host registration, sweeper retained.

**Independent Test**: Health/metric unit proofs plus composition and readiness integration proofs.

### Tests

- [ ] T014 [P] [US4] Add `tests/LamuFlix.UnitTests/Enrichment/OutboxRelayHealthCheckTests.cs` for Unhealthy/Degraded/Healthy precedence; failed-sample gauge withholding (preserve until a valid sample); successful empty sample count/age zero; independent pass/sample fault clearing by a later successful pass and independent cleanup-fault clearing by a later successful cleanup (pass success must not clear cleanup fault and cleanup success must not clear pass fault); counter semantics; and low-cardinality labels (no MovieId/payload/exception-message labels).
- [ ] T015 [P] [US4] Extend `tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs` and `tests/LamuFlix.IntegrationTests/HealthEndpointTests.cs` proving exactly one hosted relay/`outbox-relay` Ready check, existing readiness mapping including Degraded, and sweeper retention. Real relay delivery proofs must still exercise the worker (factory isolation for unrelated tests is separate).

### Implementation

- [ ] T015a [P] [US4] Add only the six approved Q7 metric-name `const` members to existing `src/LamuFlix.Core/Pipeline/TelemetryConstants.cs` (`lamuflix.outbox.pending`, `lamuflix.outbox.oldest_pending_age`, `lamuflix.outbox.published`, `lamuflix.outbox.failures`, `lamuflix.outbox.retention.deleted`, `lamuflix.outbox.retention.failures`). No other edits in that file (post-grill C1 / brief Q9 file 24). Must complete before T016.
- [ ] T016 [US4] Add `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayMetrics.cs` with the six approved instruments, consuming the `TelemetryConstants` names from T015a (no local telemetry-name literals). Depends on T015a.
- [ ] T017 [P] [US4] Add `src/LamuFlix.Infrastructure/Enrichment/OutboxRelayHealthCheck.cs` (Ready tag, name `outbox-relay`, zero DB I/O in callbacks).
- [ ] T018 [US4] Complete `OutboxRelayServiceCollectionExtensions` registration of worker/options/state/metrics/health; register `AddLamuFlixOutboxRelay` from `src/LamuFlix.Api/Program.cs`. Wire worker backlog sampling after successful passes and cleanup outcome into state/metrics. Drive T014–T015 green.
- [ ] T019 [US4] Narrowly adjust `tests/LamuFlix.IntegrationTests/ApiHostFactory.cs` and `tests/LamuFlix.IntegrationTests/ApiEndToEndFactory.cs` only to isolate unrelated tests from the new worker where required; do not treat worker removal as relay delivery proof.

**Checkpoint**: Health/metrics/composition proofs green; sweeper still registered.

---

## Phase 5: User Story 5 — two-handler direct-enqueue cutover (Q10 step 6)

**Goal**: Remove direct enqueue only from ImportMovieFolder and RequestEnrichment; prove broker-down request success and broker-recovery relay delivery.

**Independent Test**: Unit/endpoint/persistence cutover expectations plus end-to-end broker-down then relay recovery for both handlers.

### Tests

- [ ] T020 [P] [US5] Adjust `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs` and `tests/LamuFlix.UnitTests/Features/Enrichment/RequestEnrichmentCommandHandlerTests.cs` for removed queue dependency/call while preserving Stage → Save semantics and failure propagation.
- [ ] T021 [P] [US5] Adjust `tests/LamuFlix.IntegrationTests/ImportEndpointTests.cs`, `EnrichmentEndpointTests.cs`, and `ImportMovieFolderCommandHandlerPersistenceTests.cs` for durable-save-without-direct-enqueue expectations, and assign real-PostgreSQL known-precommit-failure/explicit-rollback both-or-neither proof (neither movie nor outbox durable) for the Import cutover path in `ImportMovieFolderCommandHandlerPersistenceTests.cs`; preserve Stage/save exception and cancellation propagation and the uncertain-save distinction (uncertain completion is not proof of rollback); no new transaction coordinator or DEV-329 persistence production edits.
- [ ] T022 [US5] Extend `tests/LamuFlix.IntegrationTests/OutboxRelayTests.cs` (or handler-focused cases therein) for each cutover handler: broker unavailable at request → movie+outbox commit without direct enqueue; broker returns → relay confirmed publish with wire parity and ProcessedAt after confirm; real-PostgreSQL known-precommit-failure/explicit-rollback both-or-neither proof for both ImportMovieFolder and RequestEnrichment cutover handlers as needed to cover both authorized cases; sweeper remains registered; preserve uncertain-save distinction and error propagation.

### Implementation

- [ ] T023 [US5] Remove `IEnrichmentQueue` dependency and direct `EnqueueAsync` only from `src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs` and `src/LamuFlix.Core/Features/Enrichment/RequestEnrichmentCommandHandler.cs`. Preserve Stage before shared path-A save, atomicity, and error propagation. Do not touch other enqueue callers.
- [ ] T024 [US5] Drive T020–T022 green. Solution builds within Q9 boundary only.

**Checkpoint**: Cutover proofs green; other callers/sweeper unchanged; public API shape unchanged.

---

## Phase 6: Polish — preliminary verification, Cog refactor, then final verification before Gauge gates (Q10 step 7)

**Purpose**: Configured backend verification bar per brief Q10. T025 is preliminary only and cannot satisfy final post-refactor evidence; after T026, T027 first re-runs build/format and full backend tests on the final changed HEAD, then the listed gates and review checkpoint.

- [ ] T025 Run configured backend build/format and relevant then full `dotnet test` (unit/integration). Fix only in-scope failures; do not invent gate flags, thresholds, or exemptions. Preliminary verification only — does not satisfy final post-T026 evidence.
- [ ] T026 Cog `/refactor` on the delivery diff only (helpers/early returns/guards; no scope expansion; no threshold changes; no new packages; no publisher/Stage/schema edits).
- [ ] T027 After T026 Cog `/refactor`, on the final changed HEAD, first run configured backend build/format and full `dotnet test`; then Gauge gates on exact changed C# paths including new files: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`; `./scripts/run-property-tests.ps1`; `./scripts/run-vulnerable-packages.ps1`; pre-PR `./scripts/run-mutation.ps1` with worktree-valid invocation **without** `--since`. Record command, head, and actual exit/verdict. Report SKIPPED (scope-empty) / SKIP / N/A / Could not run verbatim — never as PASS. Exit 1 or Could not run blocks. Do not lower `harness.yml` thresholds. Do not edit CONTEXT.md or author the dedicated L ADR here. Do not claim Phase A gate results. No web/OpenAPI gates for this backend-only scope. Review checkpoint follows only after this post-refactor verification and the listed gates.

**Checkpoint**: Delivery ready for review loops under brief Q11–Q12 (Critical/High/Medium block; Low recorded; three axes required; plan-challenge and code/ship-review each at most two numbered full rounds; at cap report blocked with exact evidence).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase B entry**: Blocks all production — pickup must pass or STOP for decision.
- **Phase 1 (Setup)**: Depends on Phase B entry; establishes options/state/harness.
- **Phase 2 (US1/US2 relay+failure)**: Depends on Phase 1; blocks retention/health/cutover that need a working publish loop.
- **Phase 3 (US3 retention)**: Depends on Phase 2 worker existing.
- **Phase 4 (US4 health/host)**: Depends on Phase 2 (and ideally Phase 3 cleanup outcome wiring); provides Api registration before cutover E2E.
- **Phase 5 (US5 cutover)**: Depends on Phase 2–4 so broker-down requests rely on a registered working relay.
- **Phase 6 (Polish)**: Depends on Phases 1–5 complete.

### User Story Dependencies

- **US1 (P1)**: Confirmed relay — Phases 1–2.
- **US2 (P2)**: Failure/backoff/shutdown — Phase 2 (with Phase 1 harness).
- **US3 (P3)**: Retention — Phase 3.
- **US4 (P4)**: Health/metrics/host — Phase 4.
- **US5 (P5)**: Handler cutover — Phase 5 (after relay proofs).

### Parallel Opportunities

- T001–T003 can proceed in parallel once Phase B entry clears.
- T004–T005 can proceed in parallel; T006 follows options/state.
- T007–T008 can proceed in parallel once T009 shape is known; T009–T010 sequential on the worker.
- T011–T012 parallel; T013 sequential.
- T014–T015 and T015a can proceed in parallel (different files); T016 depends on T015a (constants before metrics consumer) and may run in parallel with T017 after T015a; T018–T019 sequential on registration/factories.
- T020–T021 parallel; T023 then T022/T024.

### Parallel Example: Phase 1 harness

```text
Task: "OutboxRelayOptionsTests defaults/bounds"
Task: "OutboxRelayHostedServiceTests TimeProvider harness scaffold"
Task: "OutboxRelayTests Postgres/RabbitMQ harness scaffold"
```

---

## Implementation Strategy

### MVP First (relay before cutover)

1. Complete Phase B entry pickup (STOP on divergence).
2. Complete Phase 1 options/state/harness.
3. Complete Phase 2 confirmed publish + failure recovery.
4. **STOP and VALIDATE**: Real DB/broker publish/failure proofs green before retention/cutover.
5. Complete Phase 3 retention, Phase 4 health/host, Phase 5 cutover.
6. Phase 6 preliminary suite (T025) + Cog refactor (T026) + T027 post-refactor build/format/full tests then Gauge gates.

### Incremental Delivery

1. Pickup recon → foundation ready or blocked with cited decision.
2. Options/harness → validated configuration surface.
3. Relay publish/markers/failure → core ticket delivery.
4. Retention → processed-only cleanup.
5. Health/host → readiness/backlog observability.
6. Cutover → remove direct enqueue from two handlers.
7. Preliminary suite + refactor + post-refactor full tests then gates.

### Notes

- [P] tasks = different files, no unfinished dependency.
- Preserve Phase B STOP, exact Q9 24-file boundary, seven-step Q10 delivery order, and final post-refactor gates (T027).
- Do not invent multi-instance ownership, quarantine columns, or new packages.
- Do not commit secrets; do not touch LocalPlay / Process.Start.
- Do not author, accept, or rewrite Draft ADR-0028 or Gherkin artifacts on Build asks unless Conductor separately assigns ADR work to Keel; Draft status and separate acceptance/freeze/Gate 1 steps remain in force.
- Frozen scope wording (brief Q11–Q12) governs review findings once freeze is separately authorized.

## Phase A freeze receipt — 2026-10-25 (Keel)

**Task plan: FROZEN for Phase A planning.** Read-only speckit-analyze is CLEAN and the consolidated plan-challenge Round 1 is CLOSED/CLEAN (PLAN_CHALLENGE_ADJUDICATION.md:72-104). Five accepted Medium findings are closed; Ledger F2 remains rejected; six Low dispositions are preserved; no Round 2 or open decision is required. See brief.md, Phase A freeze receipt, for the analysis coverage, exact input SHA256 manifest, scope and limitations.

Target: F:/Dev/LamuFlix.worktrees/feature-420-spec, feature/420-spec, HEAD/base 8063eed0d5cc4b5bb3a8eb0c68b8990acf7a8cd3. The eight reviewed artifacts remain untracked working bytes, not HEAD-pinned content. All preceding text is preserved as historical stage content; this append supersedes only its pending-freeze status. No design, Q9 24-file boundary, Q7 policy, task completion mark or round-cap change is made.

No unmet Phase A freeze criterion. ADR-0028 remains Draft; Gate 1 and gate1: provisional remain unset. Phase B entry TB000-TB003 still requires its own evidence. No implementation, tests/gates, commit, PR or tracker action is performed. Phase 2 step 7 remains the later Conductor/Patron/Rigger action; user merge remains separate.
