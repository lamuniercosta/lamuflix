# Tasks: DEV-348 Postgres pgvector recommendation candidate source

**Input**: Design documents from `specs/DEV-348/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: [plan.md](plan.md) (required), [spec.md](spec.md) (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why).

**Tests**: Included. Ticket AC and brief Test strategy require real PostgreSQL+pgvector adapter proofs, handler unit/property proofs, and options startup/default regressions. Author tests in the same phase as the surfaces they lock. Compile-red is permitted while types are introduced inside a phase; the phase is not done until its named targeted proofs are green. Do not start Phase 2 production until Phase 1 prerequisites, drift recon, Keel cross-artifact speckit-analyze (findings decided or tasks amended), calibration, and Patron numeric-default ratification pass, the spec PR has merged, and Gate 1 / Phase B intake allow build work.

**Organization**: Phases follow brief Q10 stages A–D (Q10 supersedes Q8 five-stage ordering) and `spec.md` user stories US1–US3. Anvil implements Phases 2–3 one phase per ask and commits each phase. Polish is Cog then Gauge. Concrete DEV-413 entity/column identifiers and DEV-342 fixture paths come only from Phase B recon — never invent them in Spec docs or code guesses.

**propertyTests**: This brief grants no opt-out. Gauge runs `./scripts/run-property-tests.ps1` and records the printed exit; domain invariants for this ticket make property coverage mandatory.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 nearest-neighbour pool with similarity; US2 empty on missing/unusable selected vector; US3 Similar story threshold reason.
- Exact file paths are in each description.

## Phase 1: Setup — prerequisites, drift, schema/golden recon, and measured default (Stage A)

**Purpose**: Verify Phase B gates and bind concrete contracts before any production edit. No production or forced-test edits in this phase.

**⚠️ CRITICAL**: No user-story code until this phase completes with Patron-ratified numeric default (or an explicit needs-recon / Patron stop).

- [ ] T001 Confirm Phase B entry prerequisites: Epic 7 complete, landed DEV-413 schema on `origin/main`, landed DEV-342 golden implementation on `origin/main`, merged DEV-348 spec Gate 1, and Build-workspace intake per task-pipeline. Record evidence in the task note. Missing evidence blocks T002+.
- [ ] T002 Wisp read-only Phase B recon in the Build worktree: drift against `origin/main` for the nine-file envelope in `specs/DEV-348/plan.md`; bind concrete DEV-413 `movie_features` entity/column identifiers and DEV-342 golden fixture paths; confirm pgvector/`UseVector()` still present; confirm current port still returns `IReadOnlyList<Movie>` until Stage B migrates it. Stop for cited adjudication on any mismatch. No production edits.
- [ ] T002a Keel read-only cross-artifact speckit-analyze of `specs/DEV-348/brief.md`, `plan.md`, and `tasks.md` against T002 Phase B drift/consistency evidence (brief.md Preconditions; plan.md Scope and prerequisite checks). Route any CRITICAL, HIGH, or MEDIUM finding for Patron decision or Quill task amendment before Phase 2 (Stage B) production work begins. No production edits. Depends on T002.
- [ ] T003 Wisp read-only calibration receipt against the landed DEV-342 golden set: fixture SHA, stored vector/model provenance, pair cosine similarities, selected separating threshold and rationale. If semantic vector pairs or required evidence are missing, report needs recon to Conductor and obtain Patron ruling — never invent labels, substitute weighted scores, or pick a silent fallback.
- [ ] T004 Patron cited ratification of the numeric `SimilarStoryThreshold` default from the T003 receipt. Encode that ratified literal only after this ruling. Phase A drafts contain no placeholder number.
- [ ] T005 Gauge baseline for in-scope files (`-Files` / `-BaseRef` mode) recorded in the task note.

**Checkpoint**: Prerequisites verified; concrete DEV-413/DEV-342 contracts evidenced; Keel cross-artifact speckit-analyze cleared or findings decided/amended; calibrated default ratified; baseline recorded. Otherwise dependent code stops.

---

## Phase 2: User Story 1 + User Story 2 — atomic contract, real adapter, forced migration (Stage B)

**Goal**: Introduce `RecommendationCandidate`, migrate the port and options (with ratified default), implement the real two-read pgvector adapter, unwrap candidates in the handler for weighted scoring (without story suffix yet), and migrate all forced substitutes/tests together. Honest similarities only — no sentinel/stub/`NotImplemented`.

**Independent Test**: `RecommendationOptionsTests` green for finite-range validation and calibrated default; `EfRecommendationCandidateSourceTests` green against real PostgreSQL+pgvector for US1 pool/order/similarity and US2 empty/missing/unusable paths; handler substitutes compile and existing weighted-score suites remain green without requiring the story suffix yet.

**Depends on**: green Phase 1 checkpoint.

**⚠️ CRITICAL**: Finish T006–T014 in one Anvil ask/commit. Do not split port migration from the real vector query across commits.

### Implementation

- [ ] T006 [P] [US1] Add `public sealed record RecommendationCandidate(Movie Movie, double CosineSimilarity)` in `src/LamuFlix.Core/Features/Recommendations/RecommendationCandidate.cs` (brief Q2).
- [ ] T007 [US1] Change `src/LamuFlix.Core/Ports/IRecommendationCandidateSource.cs` in place so `GetCandidatesAsync` returns `Task<IReadOnlyList<RecommendationCandidate>>`. No compatibility overload; no architecture-whitelist edit (`recon-DEV-348:14,29`; brief Q2). Depends on T006.
- [ ] T008 [P] [US3] Add `public double SimilarStoryThreshold { get; init; }` on `src/LamuFlix.Core/Options/RecommendationOptions.cs` with the T004-ratified default; reject NaN/infinities/values outside `[-1, 1]` via existing `IValidatableObject`; accept endpoints; preserve `Take`, `CandidatePoolSize`, `MmrLambda`, `Weights`, and `CandidatePoolSize >= Take` (brief Q4; `recon-DEV-348:89-94`).
- [ ] T009 [US1] [US2] Implement `src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs` per brief Q5–Q6 using Phase B recon DEV-413 identifiers: selected-vector read (missing/unusable → empty after one reader); one `AsNoTracking` server-bounded candidate query with four existing includes and complete `ToDomain`; enriched-only + selected-id exclusion; usable-vector eligibility; exclude missing/null/zero/non-finite before cap; order `<=>` ascending then movie id ascending; apply `options.Value.CandidatePoolSize` on the server; project `Movie` + `CosineSimilarity` (`1.0 - distance`, clamp finite to `[-1, 1]`); inject `IOptions<RecommendationOptions>` directly; preserve existing scoped DI registration; propagate `CancellationToken`; no request-time embedding, client scan, N+1, or below-threshold pool filtering. Depends on T007 and T008.
- [ ] T010 [US1] In `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs`, migrate to unwrap `candidate.Movie` for `SimilarityScorer.Score` and preserve weighted `Score`, feature reasons, descending score / ascending movie-id ties, `Take(query.Take)`, `NotFound`, and empty-pool short circuit. Do not append "Similar story" in this phase (Stage C). Depends on T007.

### Forced tests

- [ ] T011 [P] [US3] Extend `tests/LamuFlix.UnitTests/Options/RecommendationOptionsTests.cs` for NaN/infinity/out-of-range rejection, endpoint acceptance, `IStartupValidator` via `AddLamuFlixOptions`, and regression assertion of the T004-ratified default (brief Q7). Coordinated with T008.
- [ ] T012 [US1] [US2] Rewrite `tests/LamuFlix.IntegrationTests/EfRecommendationCandidateSourceTests.cs` for real PostgreSQL+pgvector: default-50/custom caps with excess eligible rows; distance order opposite id order; equal-distance id ties; cosine conversion accuracy; missing selected row/usable vector → empty in one read without throw; candidate missing/unusable excluded before cap (null/zero where schema permits); self/non-enriched exclusion; four-collection hydration; AsNoTracking; bounded readers (1 absent / ≤2 populated); non-brittle command-shape evidence for `<=>`, `ORDER BY`, `LIMIT`; cancellation/faults remain faults. Replace obsolete uncapped-pool and exact-one-reader expectations. Deterministic 384-d vectors (`EmbeddingDimensions.Value`); no live Ollama; no mock driver; no container waiver (`recon-DEV-348:30`; brief Q7). Depends on T009.
- [ ] T013 [US1] Migrate substitutes in `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs` to `RecommendationCandidate` and keep weighted score/order/Take/feature-reason pass-through green without requiring the story suffix yet (`recon-DEV-348:28,84`; brief Q10). Depends on T010.
- [ ] T014 [US1] Migrate substitutes in `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerPropertyTests.cs` for the new port shape; preserve existing weighted-score invariants; do not require story-suffix properties yet (brief Q10). Depends on T010.

**Checkpoint**: Compilable; options + real DB adapter + forced handler substitute suites green; honest similarities; committed. No half-migrated port.

---

## Phase 3: User Story 3 — Similar story reason (Stage C)

**Goal**: Handler-only strict-threshold story suffix and unit/property regressions. Weighted score/order/feature-prefix invariants preserved.

**Independent Test**: Handler unit and FsCheck suites prove strict `>`, equality emits none, overrides, signed cosine values, weight independence, retention below threshold, and feature-reason prefix plus optional single story suffix.

**Depends on**: green Phase 2 checkpoint.

### Tests

- [ ] T015 [US3] Extend `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs` for strict `CosineSimilarity > SimilarStoryThreshold` (equality emits none), custom threshold overrides, negative/zero/positive cosine similarities, independence from `SimilarityWeights`, retention at/below threshold, unchanged weighted `Score`/final order/`query.Take`, and feature-reason prefix plus optional single `Reason("Similar story")` suffix. Replace obsolete pure reason-pass-through expectations forced by new AC (`recon-DEV-348:84`; brief Q3/Q7). Coordinated red-green loop with T017.

- [ ] T016 [US3] Extend `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerPropertyTests.cs` for the same story-suffix invariants with FsCheck (brief Q7). Coordinated with T017.

### Implementation

- [ ] T017 [US3] In `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs`, append a single `Reason("Similar story")` after all existing feature reasons iff `candidate.CosineSimilarity > options.SimilarStoryThreshold`. Do not change `SimilarityScorer`, `SimilarityResult`, `Reason`, or `RecommendedMovie` (brief Q3; ASSUMPTIONS suffix placement). Coordinated loop with T015–T016.

**Checkpoint**: Unit and FsCheck suites green for US3; weighted score/order/feature-prefix preserved; committed.

---

## Phase 4: Polish — Cog refactor, Gauge gates, review, ship (Stage D)

**Purpose**: Diff refactor; full applicable Phase 3 step 5 gate bar; three-axis review and bounded remediation; rebase/PR to awaiting-merge in the Build workspace.

**Depends on**: Phases 1–3 complete.

- [ ] T018 Cog single `/refactor` on the delivery diff only (helpers/early returns/guards; no scope expansion; no threshold changes; no new packages).
- [ ] T019 Gauge gates: `./scripts/run-roslyn-analyzers.ps1`; `./scripts/run-cyclomatic-complexity.ps1` (implementation threshold from `harness.yml`, then the script’s supported refactor-threshold invocation using the `harness.yml` configured refactor threshold, as applicable); `./scripts/run-jetbrains-inspectcode.ps1` with no arguments (never `-All`); `pwsh -NoProfile -File ./scripts/run-mutation.ps1` when production C# under `src/` changed (since disabled); `./scripts/run-property-tests.ps1` (mandatory; no opt-out); `./scripts/run-vulnerable-packages.ps1`; `dotnet format --verify-no-changes`; `dotnet test`; `scripts/run-web-gates.ps1` (expect scope-empty if web unchanged). Record command, head, and actual exit/verdict. Report SKIPPED (scope-empty) / SKIP / NOT APPLICABLE verbatim — never as PASS. Exit 1, Could not run, or unaccepted property exit 2 blocks. Do not lower `harness.yml` thresholds. Do not edit CONTEXT.md or add an ADR. Do not claim Phase A gate results. Architecture tests must remain green with the existing ports whitelist.
- [ ] T020 Sentry Risk, Ledger Standards, and Compass Spec reports on the pinned head/range (no missing-axis skip). Keel adjudication; remediation ≤ 2 rounds and ≤ 2 fix commits per round under brief closing bar (Critical/High/Medium block). Residuals past cap route to user PR review without claiming clearance.
- [ ] T021 Build-workspace rebase and PR to awaiting-merge. User merges. No auto-merge.

**Checkpoint**: Exact-head evidence; no above-bar residual findings; applicable gates/hosted checks satisfied; awaiting-merge.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup / Stage A)**: Blocks all production — prerequisites, drift recon, Keel cross-artifact speckit-analyze (findings decided or tasks amended), calibration, and Patron ratification must pass or stop.
- **Phase 2 (US1/US2 atomic Stage B)**: Depends on Phase 1; finishes T006–T014 green in one ask/commit; blocks story-suffix work.
- **Phase 3 (US3 Stage C)**: Depends on green Phase 2; finishes T015–T017 green.
- **Phase 4 (Polish Stage D)**: Depends on Phases 1–3 complete.

### User Story Dependencies

- **US1 (P1)**: Completes in Phase 2 (adapter pool/order/similarity + forced handler unwrap).
- **US2 (P2)**: Completes in Phase 2 (empty/missing/unusable selected vector + candidate filter-before-cap).
- **US3 (P3)**: Options contract lands in Phase 2 (T008/T011); story-suffix behavior completes in Phase 3 (T015–T017).

### Task Dependencies (Phase 2)

- T006 and T008 may be authored in parallel after Phase 1.
- T007 after T006.
- T009 after T007 and T008.
- T010 after T007.
- T011 with T008.
- T012 after T009.
- T013 and T014 after T010.

### Parallel Opportunities

- T006 with T008 in Phase 2 after Phase 1.
- T011 may author alongside T008.
- T013 / T014 may author in parallel after T010 once file ownership is split.
- Broker/DB integration execution for T012 stays sequential; no fixture-concurrency assumption.
- T015 / T016 / T017 remain one coordinated red-green loop in Phase 3.

### Parallel Example: Phase 2 foundation

```text
Task: "Add RecommendationCandidate record (T006)"
Task: "Add SimilarStoryThreshold on RecommendationOptions (T008)"
```

---

## Implementation Strategy

### Stage order (brief Q10)

1. Complete Phase 1: prerequisites + drift recon + Keel cross-artifact analyze + measured default (Stage A)
2. Complete Phase 2: atomic contract + real adapter + forced migration (Stage B) — MVP for US1/US2
3. **STOP and VALIDATE**: real DB + options + substitute suites green
4. Complete Phase 3: story reason (Stage C) — US3
5. Complete Phase 4: refactor / gates / review / ship (Stage D)

### Delivery notes

- One Anvil phase per ask/commit for Phases 2–3.
- Never hand off a half-migrated port or fake `CosineSimilarity`.
- Drift-induced file additions need cited Patron adjudication before editing.
- Anything else is a follow-up issue, not a finding in this round.

## Notes

- [P] tasks = different files, no unfinished dependency
- [Story] label maps task to US1/US2/US3
- Nine-file envelope is frozen; DI registration and architecture whitelist stay unchanged
- No numeric threshold invented in Spec artifacts — only the T004-ratified value enters code
- No DEV-413 identifiers invented in Spec artifacts — bind from T002 recon
- Commit messages use `DEV-348 - {subject}`
