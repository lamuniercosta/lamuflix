# Tasks: DEV-329 Stage EnrichmentRequested in transactional outbox with movie write

**Input**: Design documents from `specs/DEV-329/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why). No `design.md` (no ui tag).

**Tests**: Included. Brief Q8 requires unit orchestration proofs, real Postgres Testcontainers adapter/atomicity proofs, MigrationTests, PersistenceCompositionTests, and ArchitectureTests. Author tests in the same phase as the surfaces they lock (TDD). Compile-red is permitted while types are introduced inside a phase; the phase is not done until its named targeted proofs are green. Do not start Phase 2 production until Phase 1 pickup clears landed DEV-419 and drift, the spec PR has merged, and Gate 1 is established.

**Organization**: Phases follow brief Q9 dependency ordering and `spec.md` user stories US1–US3. Anvil implements one phase per ask. Polish is preliminary verification (T017), Cog `/refactor` (T018), then T019 final post-refactor build/format/full tests before Gauge gates.

**Domain vocabulary** (feature-local; do not edit CONTEXT.md): **staged**, **durable**, **unprocessed** — definitions in `spec.md` Domain vocabulary.

**Exact file boundary**: Only the Q7 paths listed in `plan.md` / brief Q7. Outside delivery: HandlerRegistration.cs, EfMovieRepository.cs, broker/consumer, other handlers, API shape, web/, packages.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 stage-before-save + retained enqueue; US2 atomicity/failure boundaries; US3 schema/wire/DI/architecture.
- Exact file paths are in each description.

## Phase 1: Setup — Phase B pickup STOP (landed DEV-419 + drift)

**Purpose**: Confirm landed outbox ADR authority and Q7 file-set drift before production work.

- [ ] T001 Wisp Phase B pickup/drift recon against picked HEAD: verify landed DEV-419 / ADR-0027 transactional-outbox authority is present on main (do not treat cited branch object `cb6c6a780d77f41b32d69689f237bdce43d2562d` alone as landed — brief Q2, Q9); reconcile same-Postgres/same-transaction, ProcessedAt, and delegated-schema outcomes against brief Q2–Q5; confirm Q7 existing-file paths and absence/presence of net-new outbox files; note any main drift that would change handler/save/DI surfaces. Gauge baseline refresh for the 14 existing Q7 files is already recorded (task note DEV-329:125-171; 7 net-new absent). Keel supplies pickup/drift clearance. **STOP** for a cited decision on missing prerequisite landing or any contract/file-boundary mismatch rather than inventing a substitute. No production edits in this task.

**Checkpoint**: Prerequisites match the brief; Phase B build intake is allowed.

---

## Phase 2: Foundational — port, schema, adapter, DI, early proofs (Q9 step 2)

**Purpose**: Typed port, outbox record/configuration/DbContext, EF-generated migration, scoped adapter + DI, with schema/wire/scoping/no-pre-save proofs. No handler orchestration yet beyond what adapter tests require.

**⚠️ CRITICAL**: Handler wiring (Phase 3) depends on this foundation compiling and proving Stage has no pre-save persistence.

### Tests

- [ ] T002 [P] [US3] Add real-Postgres proofs in `tests/LamuFlix.IntegrationTests/EnrichmentOutboxPersistenceTests.cs` for: Web-default byte parity vs publisher defaults; consumer-default round-trip; Attempt preservation; injected TimeProvider CreatedAt; null ProcessedAt; distinct rows on repeated Stage; no pre-save DB persistence of staged rows; scoped shared-context save of outbox with movie write as needed for adapter-level proof. AAA headers only. Do not invent relay tests or path B/C support tests.
- [ ] T003 [P] [US3] Extend `tests/LamuFlix.IntegrationTests/MigrationTests.cs` for empty/upgrade/down covering only four column types/nullability, pending partial index `(created_at, id) WHERE processed_at IS NULL`, and no other-table delta.
- [ ] T004 [P] [US3] Extend `tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs` proving scoped `IEnrichmentOutbox` shares the repository DbContext within a scope and differs across scopes.
- [ ] T005 [P] [US3] Extend `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` PortWhitelistPattern to allow only the new `IEnrichmentOutbox` port (whitelist extension, not a threshold change).

### Implementation

- [ ] T006 [P] [US3] Add `src/LamuFlix.Core/Ports/IEnrichmentOutbox.cs` with `void Stage(EnrichmentRequested request)` (no CancellationToken; EF-free).
- [ ] T007 [P] [US3] Add `src/LamuFlix.Infrastructure/Persistence/Records/OutboxMessageRecord.cs` and `src/LamuFlix.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs` for four-column `outbox_messages` + pending index (brief Q5).
- [ ] T008 [US3] Update `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs` to include the outbox DbSet/configuration surface required by the record.
- [ ] T009 [US3] Generate exactly one EF-tooling migration `AddEnrichmentOutbox` (timestamp from tooling) producing `src/LamuFlix.Infrastructure/Persistence/Migrations/<EF timestamp>_AddEnrichmentOutbox.cs`, matching Designer, and updating `LamuFlixDbContextModelSnapshot.cs`. Never hand-edit generated files.
- [ ] T010 [US3] Implement `src/LamuFlix.Infrastructure/Persistence/EfEnrichmentOutbox.cs`: serialize once with adapter-local `JsonSerializerDefaults.Web` into lossless UTF-8 Payload; assign CreatedAt via injected TimeProvider; leave ProcessedAt null; track Added row on shared context only (no save/commit/broker/retry). Register scoped in `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`.

**Checkpoint**: Named targeted proofs — EnrichmentOutboxPersistenceTests (wire/no-pre-save), MigrationTests, PersistenceCompositionTests, ArchitectureTests green for foundation surfaces.

---

## Phase 3: User Story 1 — coherent handler wiring + unit orchestration (Q9 step 3)

**Goal**: Both named handlers Stage before SaveChangesAsync and retain direct enqueue after success; all approved constructor/endpoint/repository fixture adaptations remain one buildable unit.

**Independent Test**: Existing unit suites prove Stage → Save → enqueue once with identical request and negative guards.

### Tests

- [ ] T011 [P] [US1] [US2] Extend `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs` for Stage(request) → SaveChangesAsync → EnqueueAsync exactly once on success with identical MovieId/Attempt; no staging on scan/add failures; Stage throw ⇒ no save/enqueue; save throw/cancel ⇒ no enqueue; post-save queue throw propagates; existing metrics/status retained. AAA headers only.
- [ ] T012 [P] [US1] [US2] Extend `tests/LamuFlix.UnitTests/Features/Enrichment/RequestEnrichmentCommandHandlerTests.cs` with the same orchestration/failure matrix and existing status/metric semantics; explicitly assert no `Stage`, `SaveChangesAsync`, or `EnqueueAsync` on both missing-movie and guard/state-rejected RequestEnrichment paths. AAA headers only.

### Implementation

- [ ] T013 [US1] Wire `IEnrichmentOutbox` into `src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs`: Stage once before SaveChangesAsync; retain direct enqueue after successful save; no FeaturesRequested; no retry/coordinator.
- [ ] T014 [US1] Wire `IEnrichmentOutbox` into `src/LamuFlix.Core/Features/Enrichment/RequestEnrichmentCommandHandler.cs` with the same Stage → Save → enqueue contract.
- [ ] T015 [US1] Adapt constructors/fixtures within Q7 only: `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs`, `ImportMovieFolderCommandHandlerPersistenceTests.cs`, `EnrichmentEndpointTests.cs`, `ImportEndpointTests.cs` so the solution builds with the new port. Do not edit HandlerRegistration.cs. Do not edit EfMovieRepository.cs. Do not change API shape.

**Checkpoint**: Solution builds; unit orchestration proofs green; fixture adaptations compile without out-of-boundary edits.

---

## Phase 4: User Story 2 + US3 completion — real-DB atomicity and remaining proofs (Q9 step 4)

**Goal**: Cross-context Testcontainers atomicity/rollback/broker-failure proofs complete the Q8 matrix before full verification.

**Independent Test**: EnrichmentOutboxPersistenceTests cover both handler atomic commits, controlled insert rollback, ambient path-A rollback (RequestEnrichment preserves pre-existing movie), and post-commit broker failure retention.

### Tests

- [ ] T016 [US1] [US2] Extend `tests/LamuFlix.IntegrationTests/EnrichmentOutboxPersistenceTests.cs` for both handler atomic path-A commits; controlled outbox-insert failure rolling back the movie write; explicit ambient rollback; RequestEnrichment rollback preserving pre-existing movie state (not asserting zero movies); successful commit followed by broker failure retaining movie + unprocessed outbox. Assert zero durable changes only for deterministic pre-commit failures and explicit rollbacks (brief Q6a). No provider-specific failure taxonomy. AAA headers only.

**Checkpoint**: Full Q8 real-DB atomicity/rollback/broker-failure matrix green; migration/composition/architecture proofs from Phase 2 still green.

---

## Phase 5: Polish — preliminary verification, Cog refactor, then final verification before Gauge gates

**Purpose**: Configured backend verification bar per brief Q10. T017 is preliminary only and cannot satisfy final post-refactor evidence; after T018, T019 first re-runs build/format and full backend tests on the final changed HEAD, then the listed gates and review checkpoint.

- [ ] T017 Run configured backend build/format and full `dotnet test` (unit/integration/architecture). Fix only in-scope failures; do not invent gate flags, thresholds, or exemptions. Preliminary verification only — does not satisfy final post-T018 evidence.
- [ ] T018 Cog `/refactor` on the delivery diff only (helpers/early returns/guards; no scope expansion; no threshold changes; no new packages; no EfMovieRepository rewrite).
- [ ] T019 After T018 Cog `/refactor`, on the final changed HEAD, first run configured backend build/format and full `dotnet test` (unit/integration/architecture); then Gauge gates on exact changed C# paths including new files: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`; `./scripts/run-property-tests.ps1`; `./scripts/run-vulnerable-packages.ps1`; pre-PR `./scripts/run-mutation.ps1` with worktree-valid invocation **without** `--since`. Report generated migration/snapshot exclusions explicitly. Record command, head, and actual exit/verdict. Report SKIPPED (scope-empty) / SKIP / N/A / Could not run verbatim — never as PASS. Exit 1 or Could not run blocks. Do not lower `harness.yml` thresholds. Do not edit CONTEXT.md or author a new ADR. Do not claim Phase A gate results. No web/OpenAPI gates for this backend-only scope. Review checkpoint follows only after this post-refactor verification and the listed gates.

**Checkpoint**: Delivery ready for review loops under brief Q11–Q13 (Critical/High/Medium block; Low recorded; three axes required; plan-challenge and code-review/ship-review each at most two numbered full rounds [assumed]; at cap report blocked with exact evidence).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Blocks all production — pickup must pass or STOP for decision.
- **Phase 2 (Foundational)**: Depends on Phase 1; blocks handler wiring that needs the port/adapter/migration.
- **Phase 3 (US1 handlers + fixtures)**: Depends on Phase 2; must remain one coherent buildable unit (brief Q9 step 3).
- **Phase 4 (US2 real-DB atomicity)**: Depends on Phase 3 handlers existing against real DI/DB.
- **Phase 5 (Polish)**: Depends on Phases 2–4 complete.

### User Story Dependencies

- **US3 (P3)**: Schema/wire/DI/architecture — primarily Phase 2; atomicity cases shared with US2 in Phase 4.
- **US1 (P1)**: Stage-before-save + retained enqueue — Phase 3 (+ Phase 4 commit proofs).
- **US2 (P2)**: Failure/atomicity boundaries — unit negatives in Phase 3; real-DB proofs in Phase 4.

### Parallel Opportunities

- T002–T005 can proceed in parallel once T001 clears prerequisites and the schema/port shapes from brief Q4–Q5 are known.
- T006–T007 can proceed in parallel; T008–T010 are sequential on Persistence files.
- T011 and T012 can proceed in parallel; T013–T015 must keep the solution buildable as one unit (coordinate fixture adaptations with handler ctor changes).
- T016 depends on Phase 3 wiring.

### Parallel Example: Phase 2 early proofs

```text
Task: "EnrichmentOutboxPersistenceTests wire/no-pre-save proofs"
Task: "MigrationTests empty/upgrade/down schema proofs"
Task: "PersistenceCompositionTests scoped shared context"
Task: "ArchitectureTests IEnrichmentOutbox whitelist"
```

---

## Implementation Strategy

### MVP First (foundation + both handlers)

1. Complete Phase 1 pickup (STOP on divergence).
2. Complete Phase 2 port/schema/adapter/DI + early proofs.
3. Complete Phase 3 handler wiring + unit orchestration + fixture adaptations as one buildable unit.
4. **STOP and VALIDATE**: Unit orchestration green before expanding real-DB failure matrix.
5. Complete Phase 4 atomicity/rollback/broker-failure proofs.
6. Phase 5 preliminary suite (T017) + Cog refactor (T018) + T019 post-refactor build/format/full tests then Gauge gates.

### Incremental Delivery

1. Pickup recon → foundation ready or blocked with cited decision.
2. Port/schema/adapter → wire/schema/DI proofs.
3. Handlers + fixtures → Stage → Save → enqueue delivery.
4. Real-DB atomicity → Q6a failure contract proven.
5. Preliminary suite + refactor + post-refactor full tests then gates.

### Notes

- [P] tasks = different files, no unfinished dependency.
- Preserve Q2 Phase B STOP and exact Q7 file boundary.
- Do not invent MovieId converter internals; prove via typed serialize/round-trip.
- Do not commit secrets; do not touch LocalPlay / Process.Start.
- Do not author a new ADR or Gherkin artifacts.
- Frozen scope wording (brief Q12) governs review findings once freeze is separately authorized.
- Q13 round cap is `[assumed]` per ASSUMPTIONS.md.
