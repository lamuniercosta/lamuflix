# Tasks: DEV-318 End-to-End Import-to-Enrichment Integration Proof

**Input**: Design documents from `/specs/DEV-318/` (spec.md, plan.md, research.md,
data-model.md, quickstart.md)

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md,
data-model.md. No contracts/ (no public API change, FR-005).

**Tests**: Included as the deliverable itself: this feature IS one integration-test proof.
No synthetic unit-test duplication (brief.md Q6).

**Organization**: Single-proof tasks grouped by ordering step (brief.md Q7 steps 1-8); the
proof is one scope, not independently deliverable stories. US1/US2 tags trace each task to
its spec story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2)

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm prerequisites before code tasks (brief.md Q7 step 1; owned by Wisp/Gauge,
not the builder)

- [ ] T001 [US1] Confirm Wisp pickup drift/recon is resolved and Gauge baseline gate
  measurements for the bounded file set exist before code tasks begin.

**Checkpoint**: Evidence complete; code tasks may begin.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Test-local gate and capture support in the bounded file set (brief.md Q7 step 2)

- [ ] T002 [US1] Add cancellation-aware asynchronous gate wrapping the captured scoped
  production factory in `tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs` (default
  nested helpers), preserving the decorator chain, per-test state, async continuations, and
  ValidateScopes/ValidateOnBuild.
- [ ] T003 [US2] Add disposable ActivityListener capture support (AllData sampling,
  thread-safe stopped-span collection, TelemetryConstants names) beside the gate, after T002.
- [ ] T004 [US1] Capture the original scoped descriptor: assert exactly one descriptor
  matching `ICommandHandler<ProcessEnrichmentCommand, ProcessEnrichmentOutcome>`, assert its
  scoped lifetime and non-null ImplementationFactory, capture that exact factory, then replace
  only that descriptor. Extract `tests/LamuFlix.IntegrationTests/ApiImportEnrichmentGate.cs`
  and/or `tests/LamuFlix.IntegrationTests/ApiImportTraceCapture.cs` ONLY if clarity or gate
  results require it; otherwise keep nested helpers (no new layer, no new file).

**Checkpoint**: Gate and capture support ready; scenario extension can begin.

---

## Phase 3: User Story 1 - Pending-to-Enriched HTTP import proof (Priority: P1) 🎯 MVP

**Goal**: Deterministic 202-to-Pending-to-Enriched run over the real stack.

**Independent Test**: The extended scenario (T005-T007 with T009 cleanup) passes only on
202/Location id, Pending-before-release, Enriched-after, GET 200 with full metadata, one
measured OMDb request.

- [ ] T005 [US1] Extend `ImportMovieFolder_RealScannerThroughOmdb_PersistsEnrichedMovieWithMeasuredProviderEvidence`
  in `tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs`: install capture before
  host/import, send unique trace headers without ambient Activity, assert Activity.Current
  is null after request preparation immediately before sending the POST with no propagation
  suppression, use the owned real-scanner folder and production API-host consumer
  (brief.md Q7 step 3).
- [ ] T006 [US1] Await POST 202 and real consumer arrival at the gate with bounded waits;
  parse Location id, match delivery MovieId, assert Pending from a fresh Postgres context
  before releasing enrichment (brief.md Q7 step 4).
- [ ] T007 [US1] Release the gate, await persisted Enriched via the existing status seam, and
  assert GET 200 with unchanged response geometry, all metadata fields, and exactly one
  measured OMDb GET filtered by the scenario sentinel apikey and selected t/type values
  after the existing fixture warm-up then WireMock Reset, with method/path/query evidence
  plus preserved parameter and secret-scrub assertions; retries are outside this proof and a
  duplicate matching request fails the count (brief.md Q7 step 5). No added Reset, fixture
  change, or resilience change.
- [ ] T009 [US1] Bind cleanup to the scenario-level try/finally as part of this scenario
  implementation: finally releases the gate before host disposal and guarantees
  capture/listener disposal, with release-before-host-disposal and capture/listener disposal
  verified on success and failure exits; retain owned-folder cleanup; diagnose stuck gate or
  missing span explicitly. No new IAsyncDisposable abstraction and no separate business
  failure-path scenario.

**Checkpoint**: US1 proof functional end to end (T005-T007 with T009 finally guarantees);
code tasks may proceed to exercise only after this checkpoint.

---

## Phase 4: User Story 2 - Request-correlated trace ancestry proof (Priority: P2)

**Goal**: Full Q4 ancestry/propagation assertions on the US1 run.

**Independent Test**: Not standalone; verified inside the same scenario run, failing with the
missing span named.

- [ ] T008 [US2] Await stopped spans boundedly after Enriched and assert shared request
  TraceId across API server span, import ancestry, Enrichment.Enqueue, publisher publish span,
  Enrichment.Process consumer, processing handler, and Metadata.Lookup; assert import
  ancestry and Enrichment.Enqueue descend from the API server span, publish.ParentSpanId
  equals enqueue.SpanId, consumer.ParentSpanId equals publish.SpanId, the processing handler
  descends from the consumer span, Metadata.Lookup descends from the processing handler span,
  tracestate, Consumer kind, and zero links (brief.md Q7 step 6). The T005 Activity.Current
  null precondition immediately before the POST stands; no propagation suppression.

**Checkpoint**: US1 and US2 proven in the one run.

---

## Phase 5: Release and Cleanup Guarantees

**Purpose**: No leaked listener or blocking gate (brief.md Q7 step 7); implemented as T009 in
the Phase 3 scenario, checkpointed there before exercise.

No separate tasks in this phase; T009 in Phase 3 is the implementation.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Exercise the proof and run the pipeline gate set through designated owners
(brief.md Q7 step 8)

- [ ] T010 [US1] Exercise the enhanced scenario against the real stack (Gauge targeted run),
  then the complete pipeline gate set (Gauge full run per quickstart.md); claim nothing
  without receipts.
- [ ] T011 [P] [US1] `dotnet format --verify-no-changes` clean on touched files.
- [ ] T012 Confirm no new domain terms (no CONTEXT.md edit) as an implementation check;
  the L-path ADR and plan challenge follow at Phase A after clean analyze and before
  Gate 1/build, not in these tasks.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS the scenario extension
- **User Story 1 (Phase 3)**: Depends on Foundational completion
- **User Story 2 (Phase 4)**: Depends on the US1 run in Phase 3 (same scenario, not parallel)
- **Release/Cleanup (Phase 5)**: Bound to the scenario implementation in Phase 3 (T009 finally
  guarantees, checkpointed with T005-T007); no separate Phase 5 execution
- **Polish (Phase 6)**: Depends on the complete proof

### Within This Proof

- Gate and capture support before scenario extension
- POST/arrival/Pending before release; release before Enriched/GET/OMDb assertions
- Enriched before stopped-span ancestry assertions
- Release in finally before host disposal in all paths

### Parallel Opportunities

- T002 then T003 sequentially (same file by default; coordinate edits in order)
- T011 verification can run alongside T010 reporting
- US2 has no parallel path independent of the US1 run

---

## Implementation Strategy

Single proof, one phase of delivery: Setup, Foundational, US1 extension, US2 assertions on
that run, finally guarantees, then Gauge-owned exercise and gates. Keep all code tasks within
this one proof; no broader implementation phase.
