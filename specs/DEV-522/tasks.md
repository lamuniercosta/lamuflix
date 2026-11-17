---

description: "Task list for DEV-522 stop duplicate movie imports"
---

# Tasks: Stop duplicate movie imports

**Input**: Design documents from `specs/DEV-522/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q7), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `contracts/library-failure-fields.md`; Maestri notes `DEV-522`, `recon-DEV-522` (lines 24, 28–32, 41–75); `specs/PRODUCT.md`.

**Status**: PLAN FROZEN — Keel adjudication in PLAN_ADJUDICATION.md. Gate 1, Q4 investigation, Build, and runtime acceptance remain open.

**Tests**: Required (`brief.md` Test strategy; FR-001–FR-016; SC-001–SC-007). TDD optional. Persistence/constraint/serialization behaviour uses Postgres Testcontainers — never driver mocks for those claims. Property tests required for identity/idempotency invariants (Q6). No new test framework/dependency.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/investigation tasks are never `[P]`.

**Conventions**: Edit only the Q5 authorized envelope in `plan.md` / brief; mark conditional tasks as conditional; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` / `needs decision` if a required path/fact/decision is missing; no historical merge/cleanup; no external metadata at import; no new dependency/project/port/schema/index relaxation/LocalPlay; no hand-edits to generated OpenAPI/TS; never invent cause; never lower harness thresholds. Otherwise-unassigned implementation tasks are owned by the generic Build lane (do not invent named seats).

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel records Gate 1 merge receipt. No Build tasks (T001+) before Gate 1. Q4 investigation is the first Build-lane obligation after pickup.

**Conditional edits**: Import endpoint files and enrichment settlement files change only if Q4 evidence proves need (`brief.md` Q5). Quill does not assert those patches are required.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (US1 Q4 real-DB investigation + Rigger YouTrack receipt) — blocks all production edits
Phase 2   T002 → T006   (US2 import idempotency / concurrency / preserved retry)
Phase 3   T007 → T008   (US3 late-IMDb Failed settlement — prove; change only if needed)
Phase 4   T009 → T012   (US4 additive failure projection + OpenAPI/TS + UI)
Phase 5   T013 → T014   (property tests + mandatory gates)
Phase 6   T015          (capped three-axis review / remediation per Q7)
Phase 7   T016          (delivery / merge-bar handoff — user merges)
```

Ordering per brief Task ordering / Q6: **Gate 1 → Q4 investigation/receipt → import idempotency → late-IMDb settlement → DTO→OpenAPI→TS→UI → gates/review**. No dependency cycle. Failed/unavailable investigation remains open and cannot unlock production fix tasks.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-522 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-522 in `specs/DEV-522/CONCLUSIONS.md`. Receipt only. T001–T016 MUST NOT proceed without verified merge (`brief.md` Status; Q7; no structural owner checkbox invented by Quill).

---

## Phase 1: User Story 1 - Defect-mechanism evidence (Priority: P1) (blocks production edits)

**Purpose**: Reproduce repeat-import and late-IMDb paths against a real DB; capture sanitized logs and row/constraint outcomes; distinguish demonstrated mechanism from unproven historical trigger; obtain Rigger verified YouTrack receipt under Patron ruling.
**Independent Test**: Receipt + sanitized evidence exist before any production fix commit.
**Order**: T001 after T000. **STOP** on failed evidence gate (`brief.md` Q4; FR-001–FR-002; SC-006).

- [X] T001 [US1] **Build lane** / investigation owners per pipeline. After T000: Against a real database in the Build worktree, reproduce repeat-import and late-IMDb paths; capture sanitized logs and row/constraint outcomes; explicitly qualify that this demonstrates the defect mechanism and does **not** by itself prove the historical scan/rebuild/reimport trigger (`recon-DEV-522` lines 74–75; `brief.md` Mandatory investigation gate). Patron rules on the evidence text; **Rigger alone** records cited causal evidence in YouTrack and supplies the verified receipt. **STOP** `needs recon:` if no failing mechanism can be reproduced or required evidence is unavailable — no invented cause, waiver, or unlock of T002+. If evidence shows existing ports cannot support race-safe reuse, **STOP** needs recon/decision rather than adding a port (`brief.md` Q5; FR-015). Record which conditional files (import endpoints; enrichment settlement) the evidence says must change, if any. Also investigate available historical logs/DB rows to classify the actual scan/rebuild/reimport trigger. Historical evidence unavailable -> STOP needs recon; mechanism-only evidence cannot close Done-when 3 (Patron ca667dc7 supersedes original Q4). Record enqueue call sites/order against durable save/conflict translation, bulk commit/visibility granularity, and concrete existing IMDb carriers including ManualImdbId participation/constraint handling. Document any absent carrier and obtain explicit evidence-backed applicability disposition; never invent input, silently mark N/A, or drop a ticket requirement. Retain reproduction code as durable T003/T007 Testcontainers regressions in the named integration suites; attach sanitized logs/row summaries to ticket/review receipts with credentials, connection strings and unsafe identifying paths redacted.

**Checkpoint**: T002+ unlocks only with Rigger verified receipt and Patron-ruled evidence text. Pending/Could not run investigation does not unlock implementation.

---

## Phase 2: User Story 2 - Idempotent import of known identity (Priority: P1) 🎯 MVP

**Goal**: Path/key and known-IMDb skip/reuse; one row; skipped/already-present; no second enrichment enqueue; concurrency on real DB; retry preserved.
**Independent Test**: Single/bulk/concurrency suites green for one-row/no-second-enqueue without requiring UI/contract work.
**Order**: T002 → T006 after T001. Conditional endpoint edits only if T001 evidence requires them.

### Tests for User Story 2

- [X] T002 [P] [US2] **Build lane**. After T001: Extend coverage in `tests/LamuFlix.UnitTests/Features/Import/ImportMovieFolderCommandHandlerTests.cs` and `tests/LamuFlix.UnitTests/Features/Import/ImportLibraryFoldersCommandHandlerTests.cs` for skip/reuse orchestration by library path/key and by IMDb identity when already available to the operation; assert no second enrichment enqueue and preserved retry separation (FR-003–FR-005, FR-007; SC-001; `brief.md` Q6; recon lines 69–70). Assert a distinct-path known-IMDb collision returns skipped/already-present for the existing movie identity with no second row, no enqueue and no Failed settlement for the second path. Cover both sequential and conflict translations according to T001 carrier evidence. **Disposition per T001 carrier evidence:** the literal distinct-path known-IMDb variant is not reachable at import — `IMediaLibraryScanner.Scan` and both import commands carry no IMDb id, and `ManualImdbId`/`Metadata.ImdbId` exist only on persisted rows (T001 evidence §2b; Patron ruling `CONCLUSIONS.md:140`). The covered evidence is: path/key skip before scan/`Stage`, no second enqueue, no import measurement, case-variant equality on existing key semantics, distinct path for a known-IMDb sibling still importing one row (collision settled by US3), bulk duplicate-path conflict counted as skipped, and non-retryable `duplicate_imdb_id` retry requeueing without adding a movie.
- [X] T003 [P] [US2] **Build lane**. After T001: Extend Postgres Testcontainers coverage in `tests/LamuFlix.IntegrationTests/ImportMovieFolderCommandHandlerPersistenceTests.cs`, `tests/LamuFlix.IntegrationTests/ImportLibraryCommandHandlerPersistenceTests.cs`, and `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs` proving repeated and concurrent path/key and known-IMDb imports leave one persisted movie via uniqueness/conflict translation with no second enrichment enqueue (FR-005–FR-006; SC-001–SC-002; recon lines 70–71). Orchestration unit tests alone do not satisfy concurrency. Count enrichment enqueue calls at the actual seam (at most one) for both winner and losing uniqueness-conflict translation paths, not merely lookup skip. Include two new paths sharing an already-known IMDb in one bulk request and assert one row, deterministic skip for the loser and continued processing of remaining items under existing semantics. Assert returned existing identity, no second Failed row, and no enqueue for distinct-path reuse. Use T001-established carrier and commit/visibility evidence. **Disposition:** enqueue counts are asserted at the durable seam (`OutboxMessages` rows) for winner and loser; the same-known-IMDb-in-one-request case asserts both rows persist and processing continues, because no import-time IMDb carrier exists (T001 evidence §2b/§2c; `CONCLUSIONS.md:140`); identity reuse is proven on the existing lookup seam (`FindMovieIdByImdbIdAsync` returns the `ManualImdbId` holder with no new row, no Failed row, no new enqueue) rather than on an invented import input.

### Implementation for User Story 2

- [X] T004 [US2] **Build lane**. After T002 and T003 are authored: Implement identity lookup/reuse and duplicate-conflict translation only as required by T001 evidence in `src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs`, `src/LamuFlix.Core/Features/Import/ImportLibraryFoldersCommandHandler.cs`, `src/LamuFlix.Infrastructure/Import/ScopedMovieFolderImporter.cs`, and `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs`. Skip/reuse by library path/key and by IMDb ID when already available; never silent merge/delete; never add external metadata lookup at import; preserve unique indexes in `src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs` (evidence only — not a planned relaxation); preserve existing skipped/already-present import semantics (FR-003–FR-007; `brief.md` Q2/Q5; recon 46–55). Follow the explicit phase matrix: import-phase known-IMDb/ DuplicateImdbIdException conflict -> deterministic skipped/already-present with existing identity and no Failed settlement; enrichment-phase late conflict -> T007/T008 Failed behavior. Do not implement a shared catch that erases that distinction. **Implemented:** path/key skip before scan and `outbox.Stage` in the single-folder handler (reusing `SnapshotMoviePathsAsync` + `ImportedPathSet`, existing key semantics), and explicit `DuplicateLibraryPathException` translation to skipped inside the sequential bulk loop. `ScopedMovieFolderImporter.cs`, `EfMovieRepository.cs` and `MovieConfiguration.cs` needed no change — the existing per-folder scope, uniqueness translation and `FindMovieIdByImdbIdAsync` seam already provide race-safe reuse (T001 evidence §2d; `CONCLUSIONS.md:140`). No shared catch: enrichment settlement untouched.
- [X] T005 [US2] **Build lane**. CONDITIONAL after T001/T004: Touch `src/LamuFlix.Api/Endpoints/ImportEndpoints.cs` and/or `src/LamuFlix.Api/Endpoints/BulkImportEndpoints.cs` **only if** T001 evidence proves need to carry existing import results. Preserve existing routes/status codes/contract shape. Any additional public import-contract change **STOP** for separate Patron ruling (FR-014; `brief.md` Q5). If not required, record “not needed per T001 evidence” and leave unchecked-not-applicable note in task note / CONCLUSIONS — do not invent edits. **Not needed per T001 evidence:** existing 202/400/404/409/422 and the bulk imported/skipped/failed/retried result already carry these outcomes, so no transport file was edited.
- [X] T006 [US2] **Build lane**. After T004 (and T005 if applicable): Make T002–T003 green without weakening assertions; confirm existing failed-import/enrichment retry path remains available independently of duplicate skipping (FR-007; SC-001–SC-002). Exercise retry of Failed + duplicate_imdb_id: no added movie, no indefinite enqueue loop, and no Pending regression. Preserve existing non-retryable no-op or resettle the same Failed category/canonical safe message; do not introduce new retry UX. **Evidence:** `HandleAsync_RetryFailed_RequeuesNonRetryableDuplicateFailedRowWithoutAddingMovies` shows the retry path requeues a non-retryable `duplicate_imdb_id` row (existing product behaviour: `RequestEnrichment` + one staged `EnrichmentRequested`) with no importer call and no added movie; enrichment retry policy is unchanged.


**Checkpoint**: US2 independently testable — one movie after repeat/concurrent known-identity import; no second enqueue; retry preserved.

---

## Phase 3: User Story 3 - Truthful late-IMDb Failed settlement (Priority: P1)

**Goal**: Distinct-path late IMDb collision settles Failed + `duplicate_imdb_id` + canonical message; change enrichment files only if T001 proves repair needed.
**Independent Test**: Settlement coverage shows truthful Failed reason without requiring library DTO/UI.
**Order**: T007 → T008 after T006.

### Tests for User Story 3

- [X] T007 [US3] **Build lane**. After T006: Extend/confirm coverage in `tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs` (and related integration coverage named in recon 71 if needed) for late IMDb collision on a distinct path: Failed status, category `duplicate_imdb_id`, existing canonical caller-safe message, no auto delete/merge (FR-008–FR-009; SC-003; recon 58–59, 71; `brief.md` Q2). Pair with T004 phase matrix: late enrichment collision -> Failed + duplicate_imdb_id + canonical SafeDescription; never translate it into import skip. Cover duplicate_imdb_id retry integrity per T006 and durable real-DB reproduction in the named EfMovieRepositoryTests integration suite when persistence behavior is involved. **Extended:** unit coverage `HandleAsync_LateImdbCollisionOnDistinctPath_SettlesFailedWithCanonicalDuplicateReason`, `HandleAsync_LateImdbCollisionAtFirstAttempt_DoesNotRetryOrRequeue`, `HandleAsync_LateImdbCollision_StaysFailedRatherThanReportingAnImportSkip`; real-DB `LateImdbCollision_RealDatabase_SettledRowRetriesOnceWithoutAddingOrMergingMovies` in `EfMovieRepositoryTests` proves durable Failed + `duplicate_imdb_id` + canonical `SafeDescription`, both rows surviving, no enqueue for the loser, and retry requeueing the same row once with no added movie.

### Implementation for User Story 3

- [X] T008 [US3] **Build lane**. CONDITIONAL after T007: Change `src/LamuFlix.Core/Features/Enrichment/ProcessEnrichmentCommandHandler.cs` and/or `src/LamuFlix.Core/Domain/EnrichmentFailureCategory.cs` **only if** T001 reproduction proves existing late-failure settlement needs repair. Otherwise record “existing settlement sufficient per T001” and keep T007 green without production enrichment edits (FR-009; `brief.md` Q5). Never persist raw exception text (FR-011). Preserve the T004/T007 phase matrix and the non-retryable retry integrity assertions; import-time skip and enrichment-time Failed are distinct outcomes. **Not triggered — recorded "existing settlement sufficient per T001"** (`artifacts/DEV-522/T001-evidence.md` §3: no change needed in either file). No production enrichment or category file was edited; T007 is green against the existing `FailAsync`/`SettleFailureAsync`/`MarkFailed` settlement and the existing `duplicate_imdb_id` code and `SafeDescription`. Raw exception text is never persisted: the category code is the only failure value written, and the unit regression asserts no exception or URL text reaches the log state.

**Checkpoint**: US3 independently testable — late collision is visible Failed with safe reason, not a silent gap.

---

## Phase 4: User Story 4 - Visible caller-safe failure reason (Priority: P2)

**Goal**: Additive `failureCategory` / `failureMessage` on library summary/detail; regenerate OpenAPI/TS; render in MovieCard and BulkImportDialog; Vitest + RTL component regressions (T010/T012) cover Failed safe reason and null fallback in both views; null fallback retains Failed label; no browser e2e.
**Independent Test**: Real API JSON (T009, separate) + Vitest + RTL component regressions (T010/T012) green against generated types — never browser e2e.
**Order**: T009 → T012 after T008. Strict dependency: C# DTO → OpenAPI → TypeScript → UI (`brief.md` Q6).

### Tests for User Story 4

- [ ] T009 [US4] **Build lane**. After T008: Extend `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` with real serialized summary and detail JSON assertions for Failed with available reason, nullable fallback when unavailable, both fields null for Pending/Enriched/NotFound, and no raw exception leakage (FR-010–FR-011; SC-004; `contracts/library-failure-fields.md`; recon 72). Keep this DB/API suite separate from T010/T012 component regressions.
- [ ] T010 [US4] **Build lane**. After T011 generated types exist: Vitest + React Testing Library **component regressions only** — extend `web/src/library/BulkImportDialog.test.tsx` and extend existing `web/src/library/MovieCard.test.tsx` so **both views** cover Failed safe reason (canonical caller-safe message), null fallback retaining existing Failed label, and no raw exception text (SMOKE-001/SMOKE-002; FR-013; SC-005; `ASSUMPTIONS.md`; recon 72; `brief.md` Post-grill drafting clarification). Consume generated types only. **Do not** add `web/e2e` files or treat these as browser e2e / executed browser smoke tests (`constitution.md:707-734,760`).

### Implementation for User Story 4

- [ ] T011 [US4] **Build lane**. After T009 authored: Project nullable `failureCategory` and `failureMessage` on existing library summary/detail DTOs in `src/LamuFlix.Api/Endpoints/LibraryResponses.cs` and mapping/projection in `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs` per Q3/`contracts/library-failure-fields.md`. Preserve status enum and routes. Regenerate `web/src/api/openapi.json` then `web/src/api/types.ts` from C# DTOs — no handwritten duplicates or manual generated-file edits (FR-010–FR-012; `brief.md` Approach 4–5). Make T009 green. Per Patron ca667dc7, extend only existing `src/LamuFlix.Core/Ports/MovieSummary.cs`, `src/LamuFlix.Core/Ports/MovieDetails.cs`, and `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` to carry persisted nullable LastFailureCategory through existing projections into LibraryResponses. Use category.Code/category.SafeDescription for Failed only, null otherwise (recon projection lines 5–16). No new port/schema/layer. Establish the repository generation procedure at pickup before generation if unavailable; do not invent commands or hand-edit generated files.
- [ ] T012 [US4] **Build lane**. After T011: Render canonical failure message alongside Failed in `web/src/library/MovieCard.tsx` and the failed-enrichment section of `web/src/library/BulkImportDialog.tsx` using generated types. When details are null, retain existing Failed label without inventing cause wording (FR-013; SC-005; Q7). Make T010 green. SMOKE-001/SMOKE-002 are satisfied by the T010/T012 Vitest + RTL **component regressions** against the brief-named files — never by browser e2e or executed browser smoke; do not add `web/e2e/*.e2e.ts` (`CONCLUSIONS.md` post-grill drafting ruling; `constitution.md:707-734,760`).

**Checkpoint**: US4 independently testable — API JSON + UI show caller-safe Failed reason.

---

## Phase 5: Property tests and mandatory gates

**Purpose**: Identity/idempotency properties and full configured gate suite at exact SHA.
**Checkpoint**: Gate table filled with distinct verdicts — never fold non-pass into PASS.

- [ ] T013 **Build lane**. After T012: Add/extend property tests for identity/idempotency invariants using repo conventions and existing semantics (FR-016; `brief.md` Q6 — no blanket property-test opt-out). Use `tests/LamuFlix.UnitTests/Features/Import/ImportIdentityPropertyTests.cs` (new test file if needed in the existing FsCheck unit project; recon web-standards lines 29–30). Properties run in memory; real query/constraint behavior belongs to T003, not FsCheck.
- [ ] T014 **Gauge** / Build-lane gate execution. After T013 and required story tests green: Run configured backend gates — `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (including applicable refactor-gate obligation), `./scripts/run-jetbrains-inspectcode.ps1`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and pre-PR `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` when production C# under `src/` is eligible. From `web/`, run `npm run lint`, `npx tsc --noEmit`, `npm run test:run`, `npm run build` (recon web-standards lines 23–24; `scripts/run-web-gates.ps1:102–128`). Retain format verification required by `.github/workflows/ci.yml:59–60`. No configured OpenAPI drift command was found in the authorized roots; record that limitation, verify contract regeneration output and generated diff, and do not report an invented drift gate as PASS. Check existing `web/e2e/import.e2e.ts` and `web/e2e/library-path.e2e.ts` Failed-text assertions for compatibility; request cited ruling before any out-of-envelope edit. Record exact commands, HEAD, exits, and distinct PASS / SKIPPED (scope-empty) / SKIP / N/A / Could not run / FAIL verdicts. Never fold non-pass into PASS; surviving mutants require tests; never lower thresholds (FR-016; SC-007; `brief.md` Gate expectations). Thresholds/applicability from `harness.yml` only.

**Checkpoint**: Evidence and gates cannot be claimed green from empty/old-head runs.

---

## Phase 6: Review / remediation (Q7 closing bar)

**Purpose**: Capped three-axis implementation review under the frozen closing bar.
**Checkpoint**: Zero undispositioned findings in frozen scope, or unresolved items explicitly left for user review on the PR at the 2×2 cap — never auto-cleared.

- [ ] T015 After T014: Conduct capped implementation review/remediation (at most two rounds and two fix commits per round) requiring Sentry/Risk, Ledger/Standards, and Compass/Spec reports pinned to the reviewed head (size M). Closing bar: every verified Critical/High/Medium/Low finding within frozen scope resolved or explicitly dispositioned with cited evidence; Critical/High cannot be waived away; accepted unresolved behavior blocks delivery. Unresolved above-bar findings after the cap go to user PR review — never silent clearance. **After any remediation commit**: rerun affected gates on final HEAD (`brief.md` Q7; SC-007).

---

## Phase 7: Delivery handoff

**Purpose**: Prepare merge-bar evidence packaging; only the user merges.
**Checkpoint**: No claim of merge clearance by agents. Final-head freshness required. Root-cause receipt remains part of Done-when evidence (SC-006).

- [ ] T016 After T015: Publish exact-head merge-bar evidence package for DEV-522 (gates, idempotency/concurrency, late-IMDb settlement, API/UI failure projection, Q4/Rigger receipt status) using only receipts that match the **final delivery/review HEAD**. Leave merge to the user. Do not merge or enable auto-merge. Do not claim PLAN FROZEN, Gate 1, or runtime acceptance from packaging alone. Keel has declared PLAN FROZEN in PLAN_ADJUDICATION.md; delivery packaging does not grant Gate 1 or runtime acceptance (SC-006–SC-007; `brief.md` Q7).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Blocks all Build work
- **Phase 1 (T001 / US1)**: Depends on T000 — blocks all production fix tasks
- **Phase 2 (US2)**: Depends on T001; T002/T003 may run in parallel after T001; T004 after both authored; T005 conditional; T006 after implementation
- **Phase 3 (US3)**: Depends on T006; T008 conditional on T001 evidence
- **Phase 4 (US4)**: Depends on T008; T011 before T012; T010 after generated types (T011)
- **Phase 5**: Depends on T012; T014 after T013
- **Phase 6–7**: Depend on T014 status

### User Story Dependencies

- **US1 (P1)**: Investigation gate; no code dependency on later stories
- **US2 (P1) MVP**: After US1 receipt
- **US3 (P1)**: After US2 (ordering per brief); may prove existing settlement sufficient
- **US4 (P2)**: After US3; contract generation before UI

### Parallel Opportunities

- T002 and T003 after T001 (different test projects/files)
- T007 after T006
- T009 after T008; then T011; then T010/T012 (no parallel execution claim)
- T013 after T012; T014 waits for T013 (no parallel execution claim)

---

## Parallel Example: User Story 2

```text
Task: "Extend unit import orchestration tests in ImportMovieFolderCommandHandlerTests.cs and ImportLibraryFoldersCommandHandlerTests.cs"
Task: "Extend Testcontainers persistence/concurrency coverage in ImportMovieFolderCommandHandlerPersistenceTests.cs, ImportLibraryCommandHandlerPersistenceTests.cs, and EfMovieRepositoryTests.cs"
```

---

## Implementation Strategy

### MVP First (US1 evidence + US2 idempotent import)

1. Complete Phase 0: Gate 1
2. Complete Phase 1: Q4 investigation + Rigger receipt
3. Complete Phase 2: User Story 2
4. **STOP and VALIDATE**: One movie after repeat import; no second enqueue

### Incremental Delivery

1. Add US3 late-IMDb truthful Failed settlement
2. Add US4 additive failure projection + generated contracts + UI
3. Properties + gates + three-axis review + user merge

### Notes

- `[P]` tasks = different files, no incomplete deps
- Conditional T005/T008 must not invent edits
- SMOKE-001/SMOKE-002 map exclusively to T010/T012 Vitest + RTL component regressions on brief-named files; never browser e2e / never `web/e2e` (`brief.md` Post-grill drafting clarification; `CONCLUSIONS.md` post-grill drafting ruling; resolved in `DRAFTING_RECEIPT.md`)
- Real-DB (Testcontainers) and serialized API (`LibraryEndpointsTests` / T009) remain separate from T010/T012
- Avoid: vague tasks, same-file false `[P]`, expanding past Q5 envelope, inventing cause or PLAN FROZEN, claiming component tests as browser smoke/e2e
