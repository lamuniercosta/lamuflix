# Tasks: DEV-347 FeaturesRequested message, topology, and publish after enrichment succeeds

**Input**: Design documents from `specs/DEV-347/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: [plan.md](plan.md) (required), [spec.md](spec.md) (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why).

**Tests**: Included. Ticket AC and brief Test strategy require Core eligibility/failure-propagation unit proofs, real-broker topology/publisher/consumer proofs, and enrichment routing regression coverage. Author tests in the same phase as the surfaces they lock. Compile-red is permitted while types are introduced inside a phase; the phase is not done until its named targeted proofs are green. Do not start Phase 2 production until Phase 1 drift recon passes, the spec PR has merged, and Gate 1 / Phase B intake allow build work.

**Organization**: Phases follow brief Task ordering (`brief.md:190`) and `spec.md` user stories US1–US3. Anvil implements one phase per ask and commits each phase. Polish is Cog then Gauge.

**propertyTests**: This brief grants no opt-out. Gauge runs `./scripts/run-property-tests.ps1` and records the printed exit; a no-tests-tagged exit 2 without a later task-note opt-out is a failure per task-pipeline.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 non-empty-plot publish; US2 empty/ineligible skip; US3 features topology + unchanged enrichment routing.
- Exact file paths are in each description.

## Phase 1: Setup — prerequisite drift recon

**Purpose**: Re-pin facts against the Phase A base before production edits.

- [ ] T001 Wisp drift recon in the Phase B worktree against the Phase A file envelope in `specs/DEV-347/plan.md`: `FeaturesRequested` still absent in C#; `IEnrichmentQueue` still has only the `EnrichmentRequested` overload (`src/LamuFlix.Core/Ports/IEnrichmentQueue.cs`); `RabbitMqTopology` still declares only enrichment queues on `lamuflix.enrichment`; `ProcessEnrichmentCommandHandler.CompleteAsync` still saves without features enqueue; `EnrichmentRouting.Decide` enrichment dispositions unchanged; publisher confirm/mandatory/trace pattern still present; architecture port whitelist still eight names; corresponding unit/integration test homes still present. Gauge supplies applicable baseline. Stop for a cited decision on any mismatch rather than inventing a bridge. No production edits in this task.

**Checkpoint**: Facts match the plan envelope; Phase B build intake is allowed.

---

## Phase 2: Foundational — message, port, topology, routing, publisher, and focused proofs

**Purpose**: Introduce the message contract, queue overload, features topology/routing, and publisher support, and prove those foundation surfaces with focused green tests before Core post-save publish or consumer e2e (`brief.md:190`).

**⚠️ CRITICAL**: No Core eligibility publish or consumer e2e until this phase completes. All Phase 2 implementation and proof tasks finish in the same phase ask and commit.

### Implementation

- [X] T002 [P] Add `public sealed record FeaturesRequested(MovieId MovieId, int Attempt);` in `src/LamuFlix.Core/Ports/FeaturesRequested.cs` (`namespace LamuFlix.Core.Ports`), matching `EnrichmentRequested` style (`recon-DEV-347:33`; brief Q1).
- [X] T003 Extend `src/LamuFlix.Core/Ports/IEnrichmentQueue.cs` with `Task EnqueueAsync(FeaturesRequested message, CancellationToken ct);`. Do not add a new port interface or edit the architecture whitelist (`recon-DEV-347:34,87`; brief Q2). Depends on T002.
- [X] T004 In `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqTopology.cs`, keep exchange `lamuflix.enrichment` and enrichment queues/keys unchanged; declare quorum queues `features.requested`, `features.retry`, and `features.dead-letter` with Q3 arguments and bindings (brief Q3; `recon-DEV-347:39-42`). Depends on T005.
- [X] T005 [P] In `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentRouting.cs`, expose features routing constants/helpers needed to route `FeaturesRequested` while leaving `Decide(ProcessEnrichmentOutcome)` enrichment semantics unchanged (brief Q4; `recon-DEV-347:55`).
- [X] T006 In `src/LamuFlix.Infrastructure/RabbitMq/RabbitMqEnrichmentQueuePublisher.cs`, implement `EnqueueAsync(FeaturesRequested, CancellationToken)` publishing to the features requested route with existing publisher confirms, `mandatory: true`, web-default JSON, `TraceContextCarrier.Inject`, and `TelemetryConstants.EnrichmentEnqueue` tagged with `TelemetryConstants.MovieId` (brief Q8; `recon-DEV-347:47-49`). Depends on T003, T004, and T005.
- [X] T007 Update `tests/LamuFlix.Tests.Common/RabbitMqFixture.cs` only if reset/cleanup must enumerate the new features queues; otherwise leave unchanged and record that disposition before any new-queue integration proof (brief Files touched; `recon-DEV-347:86`). Depends on T004; completes before T008 and T012.

### Focused proofs

- [X] T008 [US3] Extend `tests/LamuFlix.IntegrationTests/RabbitMqTopologyTests.cs` to assert `features.requested` / `features.retry` / `features.dead-letter` are quorum queues on durable direct `lamuflix.enrichment` with routing keys `features.requested` / `features.retry` / `features.dead-letter`; requested DLX args with `x-dead-letter-routing-key = "features.dead-letter"`; retry TTL and `x-dead-letter-routing-key = "features.requested"`; dead-letter `DeadLetterArguments()`; idempotent declaration; and every existing enrichment queue/key/argument assertion still passes (`recon-DEV-347:83`; brief Test strategy). Depends on T004 and T007.
- [X] T009 [US3] Extend `tests/LamuFlix.UnitTests/RabbitMq/EnrichmentRoutingTests.cs` (and `RabbitMqServiceCollectionExtensionsTests.cs` only if registration behavior changes) so enrichment `Decide` dispositions remain Ack/retry/dead-letter as today and any new features routing helpers are covered without altering enrichment outcomes (`recon-DEV-347:75-76`). Depends on T005.
- [X] T012 [US1] Extend `tests/LamuFlix.IntegrationTests/RabbitMqPublisherTests.cs` to round-trip typed `FeaturesRequested` to `features.requested`, preserve JSON contract and trace headers, and exercise mandatory unroutable/unreachable failure using the existing integration-test pattern (`recon-DEV-347:84`; brief Test strategy). Depends on T006 and T007.

**Checkpoint**: Message/port/publisher compile; `RabbitMqTopologyTests`, `EnrichmentRoutingTests`, and `RabbitMqPublisherTests` green for the named feature requirements and existing enrichment regressions; registration tests green if registration behavior changes; fixture isolation/cleanup ready (or unchanged disposition recorded). No features consumer or Worker change. Compile-red is allowed only within the phase, never at this checkpoint.

---

## Phase 3: User Story 1 + User Story 2 - Core eligibility publish (Priority: P1/P2)

**Goal**: Core owns eligibility and enqueues exactly one `FeaturesRequested` after a successful save for Found+non-whitespace plot; all excluded cases enqueue nothing; publish/confirm exceptions propagate with the row left `Enriched`.

**Independent Test**: `ProcessEnrichmentCommandHandlerTests` prove call ordering, cardinality, excluded cases, and failure propagation without RabbitMQ.

**Depends on**: green Phase 2 checkpoint.

### Tests

- [X] T010 [US1] [US2] Extend `tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs`: Found with saved non-whitespace `Synopsis` enqueues `FeaturesRequested(movie.Id, 1)` exactly once after save; null/empty/whitespace `Synopsis`, NotFound, failed/duplicate lookup, and refused claim enqueue nothing; save failure never reaches publish; after save succeeds, publish/confirm exceptions propagate and the persisted row remains `Enriched` with no rollback or in-handler retry; refused-claim redelivery returns `Completed(Claimed: false)` with no metadata-provider call and no features republish. Use observable call ordering, not only enqueue counts. AAA headers only (`recon-DEV-347:77`; brief Test strategy / Q9). Coordinated red-green loop with T011.

### Implementation

- [X] T011 [US1] [US2] In `src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs` `CompleteAsync`, after successful `movies.SaveChangesAsync` when `result is MetadataLookupResult.Found` and `!string.IsNullOrWhiteSpace(movie.Metadata?.Synopsis)`, await `queue.EnqueueAsync(new FeaturesRequested(movie.Id, 1), cancellationToken)` without catching publish/confirm exceptions. Do not publish for excluded cases. Retain existing `ProcessEnrichmentOutcome` claim semantics; do not add consumer-side publish (brief Q5–Q6, Q9; `recon-DEV-347:57`). Coordinated red-green loop with T010.

**Checkpoint**: `ProcessEnrichmentCommandHandlerTests` green for T010/T011 ordering, cardinality, skips, and Q9 propagation (US1 exactly-one emission and US2 skips/failure propagation).

---

## Phase 4: User Story 1 + User Story 2 - Consumer e2e and Q9 settlement (Priority: P1/P2)

**Goal**: Consumer end-to-end for non-empty and empty plot; Q9 settlement on publish/confirm failure and cancellation; enrichment regressions retained. Do not move consumer e2e earlier than this phase.

**Independent Test**: `EnrichmentConsumerTests` green for features publish path, empty-plot skip, Q9 nack paths, and refused-claim skip without features republish.

**Depends on**: green Phase 3 checkpoint.

### Tests

- [X] T013 [US1] [US2] Extend `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs`: successful persisted non-empty plot yields one features message and normal enrichment settlement; successful empty plot yields none; post-save publish/confirm failure marks activity error, logs error, and nacks enrichment with `requeue: false` without success ack; stopping-token cancellation during publish/confirm nacks with `requeue: true`; redelivery after commit with refused claim is acked as skipped with no features republish; exercise ambiguous-confirm exception path without asserting whether the broker accepted the features message; retain enrichment retry, DLQ, false-claim, and failed-republish regressions (`recon-DEV-347:85`; brief Q9). Do not create a features consumer or eventual-recovery sweeper test. Coordinated loop with T014.

### Implementation

- [X] T014 [US1] In `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs`, preserve the existing single post-handler ack path and Q9 settlement contract. Do not add a second consumer-side `FeaturesRequested` publish. Adjust only if required to keep settlement behavior aligned with Core-thrown publish/confirm exceptions already covered by existing catch paths (`recon-DEV-347:54-56`; brief Q6, Q9). Coordinated loop with T013.

**Checkpoint**: `EnrichmentConsumerTests` green for T013/T014 e2e, settlement, cancellation, refused-claim, and enrichment regressions (AC 1–2 and Q9). No features consumer shipped.

---

## Phase 5: Polish — Cog refactor and Gauge gates

**Purpose**: Diff refactor; full applicable Phase 3 step 5 gate bar.

**Depends on**: all prior phases complete (Phases 1–4).

- [ ] T015 Cog `/refactor` on the delivery diff only (helpers/early returns/guards; no scope expansion; no threshold changes; no new packages).
- [ ] T016 Gauge gates: `./scripts/run-roslyn-analyzers.ps1`; `./scripts/run-cyclomatic-complexity.ps1` (implementation threshold from `harness.yml`, then the script’s supported refactor-threshold invocation using the `harness.yml` configured refactor threshold, as applicable); `./scripts/run-jetbrains-inspectcode.ps1` with no arguments (never `-All`); `pwsh -NoProfile -File ./scripts/run-mutation.ps1` when production C# under `src/` changed; `./scripts/run-property-tests.ps1` (no opt-out granted by this brief); `./scripts/run-vulnerable-packages.ps1`; `dotnet format --verify-no-changes`; `dotnet test`; `scripts/run-web-gates.ps1` (expect scope-empty if web unchanged). Record command, head, and actual exit/verdict. Report SKIPPED (scope-empty) / SKIP / NOT APPLICABLE verbatim — never as PASS. Exit 1, Could not run, or unaccepted property exit 2 blocks. Do not lower `harness.yml` thresholds. Do not edit CONTEXT.md or add an ADR. Do not claim Phase A gate results. Architecture tests must remain green with the existing ports whitelist.

**Checkpoint**: Delivery ready for review loops under brief closing bar (Critical/High block; Medium/Low disposition; plan challenge and remediation each at most two rounds).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Blocks all production — Wisp recon must pass or stop for decision.
- **Phase 2 (Foundational + focused proofs)**: Depends on Phase 1; finishes T002–T009 and T012 with green focused topology/routing/publisher proofs in the same ask/commit; blocks Core publish and consumer e2e.
- **Phase 3 (US1/US2 Core publish)**: Depends on green Phase 2; finishes `ProcessEnrichmentCommandHandlerTests` green for T010/T011.
- **Phase 4 (US1/US2 consumer e2e + Q9)**: Depends on green Phase 3; finishes `EnrichmentConsumerTests` green for T013/T014. Consumer e2e stays after Core wiring.
- **Phase 5 (Polish)**: Depends on Phases 1–4 complete.

### User Story Dependencies

- **US3 (P3)**: Topology/routing — completes in Phase 2 (implementation + T008/T009 proofs).
- **US1 (P1)**: Publisher proofs in Phase 2 (T012); Core proofs in Phase 3 (T010/T011); consumer proofs in Phase 4 (T013/T014).
- **US2 (P2)**: Core proofs in Phase 3 (T010/T011); consumer proof in Phase 4 (T013/T014).

### Task Dependencies (Phase 2)

- T002 and T005 may be authored in parallel.
- T003 after T002.
- T004 after T005.
- T007 after T004 and before any new-queue integration proof (T008, T012).
- T006 after T003, T004, and T005.
- T008 after T004 and T007.
- T009 after T005.
- T012 after T006 and T007.

### Parallel Opportunities

- T002 with T005 in Phase 2.
- T008 / T009 / T012 proof authoring may parallelize only after their listed dependencies; broker test execution stays sequential, with no fixture-concurrency assumption.
- T010 / T011 remain one coordinated red-green loop in Phase 3.
- T013 / T014 remain one coordinated loop in Phase 4.

### Parallel Example: Phase 2 foundation

```text
Task: "Add FeaturesRequested message record (T002)"
Task: "Add EnrichmentRouting features helpers (T005)"
```

### Parallel Example: Phase 2 focused proofs (after dependencies)

```text
Task: "Extend RabbitMqTopologyTests for features.* quorum queues and enrichment regressions (T008; after T004+T007)"
Task: "Extend EnrichmentRoutingTests for unchanged Decide dispositions (T009; after T005)"
Task: "Extend RabbitMqPublisherTests for FeaturesRequested round-trip (T012; after T006+T007)"
```

Broker test runs for T008 and T012 stay sequential; do not assume concurrent fixture use.

---

## Implementation Strategy

### Delivery sequence

1. Phase 1 recon (stop on divergence).
2. Phase 2 foundation and green focused topology/routing/publisher proofs (T002–T009, T012).
3. Phase 3 Core eligibility publish and green unit proofs (T010/T011).
4. **STOP and VALIDATE**: Core unit proofs before consumer e2e.
5. Phase 4 consumer e2e and green Q9/regression proofs (T013/T014).
6. Phase 5 Cog refactor + Gauge gates (T015/T016).

One phase per Anvil ask/commit. Full-gate ownership and results rules remain: Gauge owns the full applicable gate run on the implementation diff and again after rebase; Cog refactors the completed diff before Gauge. Do not claim Phase A gate results.

### Notes

- [P] tasks = different files, no unfinished dependency.
- One publish owner only: Core after save; never a second consumer-side features publish.
- Do not invent outbox, sweeper, features consumer, schema, or Worker edits.
- Do not commit secrets; do not touch LocalPlay / Process.Start.
- Delivery scope (brief) describes the delivery boundary and does not preclude findings about failures to meet in-scope requirements.
- Preserve ticket AC wording "exactly one" as execution emission cardinality (brief Q9).
