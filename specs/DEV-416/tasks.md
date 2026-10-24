# Tasks: DEV-416 Apply Maximal Marginal Relevance diversification to blended recommendations

**Input**: Design documents from `specs/DEV-416/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (plan records why). Code Phases 1–4 also require the Phase B entry checklist below. Missing Phase B evidence blocks T001.

**Tests**: Included. Ticket tests, brief Q5–Q6/Q9–Q11, and FR-009/FR-010 require them. Brief Q9 allows tests alongside implementation; no separate mandatory red-first policy. Prefer authoring the behavior test before or with the production edit it locks. When types under test do not yet exist, add only the minimum compilable contracts needed so those tests compile; do not treat missing-type compile failures as behavioral red tests. Drive each phase's tests green before that phase's commit. Use existing xUnit v3, Shouldly, NSubstitute, and FsCheck conventions. Real DEV-338 scorer — no scorer mock. No new test dependency. No database fixture for selector proofs. Single Anvil ownership for Phases 1–4.

**Organization**: Phases follow brief task order items 2–6. Anvil implements Phases 1–4, one phase per ask, and stops after Phase 4. Phase 5 is Cog, then Gauge. Do not start Phase 1 until the Phase B entry checklist is satisfied. Selector FsCheck/reference and sequel fixture (Phase 3) MUST pass before handler wiring (Phase 4).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 selector MMR, US2 sequel-chain fixture, US3 handler integration.
- Exact file paths are in each description.

## Phase B entry (before Code Phase 1)

**Purpose**: Schedule Build workspace entry checks from the brief task order and PRODUCT §§1–3,5. Do not run gates, tests, or analyzers from this Spec workspace. Missing evidence blocks T001.

**Independent Test**: Every checklist item below has recorded evidence in the task note (or named owner handoff). Any missing item stops code.

- [x] TB001 Confirm the spec PR has merged and Gate 1 is established (Rigger / YouTrack `Ready to build` handoff). Checklist mark must reference the recorded receipt.
- [x] TB002 Obtain Phase B drift recon against `origin/main` for the authorized file set (Wisp → `recon-DEV-416` refresh). Reconcile handler ranking insertion point, SimilarityScorer signature, MovieFeaturesMapper, RecommendationOptions.MmrLambda, existing handler tests, and DEV-338 weight-validation policy/helper accessibility against recon-DEV-416, recon-DEV-416-2, and frozen Q1–Q12 plus A1. Off-envelope changes stop to Patron.
- [x] TB003 Obtain Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef` mode) recorded in `DEV-416`.
- [x] TB004 Keel checks or amends drift against the brief/spec before any Code Phase 1 edit. Missing Keel drift check blocks T001.

**Keel Phase B pickup receipt — 2026-10-24**: Checked at `F:/Dev/LamuFlix.worktrees/DEV-416`, branch `feature/DEV-416`, HEAD `22d726948770b601f01bfe715fda2990b57f257b` before code edits.

- TB001: `recon-DEV-416` R2 records spec PR #245 merged at this HEAD and the Rigger `Ready to build` handoff in note `DEV-416:4-10`; this satisfies the named Gate 1 receipt.
- TB002: `recon-DEV-416` R1/R3 and sections 2–7 establish zero authorized-file drift against local `origin/main` at the same HEAD and reconcile the six required seams against frozen Q1–Q12/A1. The private seven-weight validation policy is source-verified in R3.6; historical note anchors are superseded for pickup evidence by these fresh facts, without changing the decisions. No off-envelope change requires a ruling.
- TB003: `DEV-416:32-38,49-53`, `recon-DEV-416` R5 and its continuation `recon-DEV-416-2` R5.1–R5.4 record Roslyn, complexity and InspectCode baseline exit 0 / zero findings, each with explicit 37-file scope at this HEAD. Complexity effective threshold 15; InspectCode WARNING+, no `-All`; 16 source and 21 test paths agree across the scope receipts. These are baseline results; implementation gates remain T019 obligations.
- TB004: Keel compared the refreshed recon with the frozen brief/spec and plan/tasks: handler insertion, scorer signature, feature retention, existing lambda, handler tests, and A1 validation/accessibility remain consistent. No contract amendment is needed. Upstream primary-key evidence remains unverified as already accepted in brief A1; duplicate-ID rejection remains mandatory. Drift is against the recorded local `origin/main`; remote freshness was not independently established.

**Pickup disposition**: TB001–TB004 complete. Cleared for Anvil Code Phase 1, T001–T005 only, under the existing single-owner phase order and Q7 file inventory. All implementation tasks remain unchecked; later phases retain their own checkpoints.

**Checkpoint**: Phase B entry evidence is complete. Only then may Anvil start T001.

---

## Phase 1: Selector contract and validation (US1 foundational)

**Purpose**: Establish `MmrCandidate` and `MmrSelector` surface with Q3 validation and valid empty/zero/oversized-Take behavior in Q7 files. Blocks algorithm and handler work.

**Independent Test**: Every Q3 invalid category throws before empty shortcuts; valid empty pool and Take = 0 return empty; oversized Take returns the entire pool; relevance above 1 accepted when finite and nonnegative.

**Checkpoint**: Contract and validation compile and prove Q3 boundaries without requiring the full MMR loop body beyond empty/valid shortcuts.

### Tests for Phase 1

- [x] T001 [US1] Author unit tests in `tests/LamuFlix.UnitTests/Features/Recommendations/MmrSelectorTests.cs` covering every Q3 invalid category (null list/items/features/weights, duplicate MovieIds, negative Take, non-finite or out-of-[0,1] lambda, negative or non-finite relevance), combined invalid-weight cases with a valid empty pool and with Take = 0 (validation before shortcuts), valid empty pool, Take = 0, oversized Take, and relevance above 1. Minimum compilable contracts when types are absent; behavioral failure before T003 when possible.
- [x] T002 [US1] After or combined with T001 under single ownership of the same file, author focused call-isolation stubs/expectations in `tests/LamuFlix.UnitTests/Features/Recommendations/MmrSelectorTests.cs` (or clearly separated test region) for unchanged input order/content and feature/weight values after Select; full interleaved-call assertions may complete in Phase 2 once selection produces ordered IDs.

### Implementation for Phase 1

- [x] T003 [P] [US1] Add sealed record `MmrCandidate(MovieId MovieId, double Relevance, MovieFeatures Features)` in `src/LamuFlix.Core/Features/Recommendations/MmrCandidate.cs`.
- [x] T004 [US1] After T001, add pure static `MmrSelector` in `src/LamuFlix.Core/Features/Recommendations/MmrSelector.cs` with `Select` accepting complete candidate list, Take, lambda, SimilarityWeights, and GenreWeights; implement full-input validation before empty shortcuts; implement identical private seven-property SimilarityWeights rejection (Director, Writers, Cast, Genres, YearProximity, RuntimeProximity, RatingBoost) when < 0.0 or !double.IsFinite via `ArgumentOutOfRangeException` before shortcuts, without editing SimilarityScorer, reflection, or a dummy Score call; implement valid empty/zero/oversized-Take return behavior. Algorithm body for nonempty positive Take may remain incomplete only until Phase 2 if required for compilation of validation paths.
- [x] T005 [US1] Drive T001–T002 green for validation and empty/zero/oversized paths; leave algorithm-dependent isolation cases for Phase 2 if still pending.

**Checkpoint**: Phase 1 tests green for validation/empty paths. Commit `DEV-416 - MmrSelector contract and validation`.

---

## Phase 2: Deterministic MMR algorithm and incremental maxima (US1)

**Purpose**: Implement Q2 MMR with Q4 incremental running maxima, exact-ID ties, lambda endpoints, missing-feature/zero-weight reuse, earlier-selected decisive maximum, call isolation, and cross-Take prefix behavior.

**Independent Test**: Exact ties across permutations; lambda = 1 and lambda = 0 endpoints; real DEV-338 missing-feature/zero-weight; three-selection earlier-max fixture; Q10 isolation and interleaved calls; Q11 prefix consistency on focused cases.

**Checkpoint**: Selector algorithm proves deterministic diversification mechanics without requiring FsCheck suite or handler wiring.

### Tests for Phase 2

- [x] T006 [US1] Extend `tests/LamuFlix.UnitTests/Features/Recommendations/MmrSelectorTests.cs` for exact-ID ties across input permutations, lambda = 1 (relevance then ID), lambda = 0 (smallest ID then minimize max redundancy), real DEV-338 missing-feature/zero-weight behavior, and a fixture with at least three selections where an earlier selected item supplies the decisive maximum redundancy.
- [x] T007 [US1] After or combined with T006 under single ownership of the same file, complete Q10/Q11 focused checks in `tests/LamuFlix.UnitTests/Features/Recommendations/MmrSelectorTests.cs`: unchanged inputs after Select; interleaved calls over stable inputs with retained prior output; cross-Take prefix for representative `0 <= a <= b` cases including zero/oversized Takes.

### Implementation for Phase 2

- [x] T008 [US1] After T006, implement deterministic MMR and Q4 incremental numeric redundancy maxima in `src/LamuFlix.Core/Features/Recommendations/MmrSelector.cs`: project-free Select over provided features; maximize `lambda * relevance - (1 - lambda) * maxSim`; zero redundancy before first pick; after each new pick, update each remaining candidate's running maximum as `max(previous maximum, new pair similarity)` over all prior selections, never latest-only; discard pairwise Reasons; exact equal objectives tie by ascending MovieId.Value with no epsilon or rounding; keep selection state and numeric maxima invocation-owned with no static mutable/cross-request state and no full pairwise matrix, preserving O(n) auxiliary state; treat inputs as read-only; return ordered MovieIds of size min(Take, count). Retain earlier-selected-maximum and independent full-rescan reference proofs.
- [x] T009 [US1] Drive T006–T007 green.

**Checkpoint**: Phase 2 tests green. Commit `DEV-416 - deterministic MMR selection loop`.

---

## Phase 3: FsCheck/reference equivalence and sequel-chain fixture (US1–US2)

**Purpose**: Complete bounded FsCheck properties with independent full-rescan reference and the Q5 sequel-chain fixture. Both MUST pass before handler wiring. Construction may proceed in parallel once the selector contract from Phases 1–2 is stable.

**Independent Test**: Property suite green; sequel fixture asserts baseline first-five sequels, expected MMR order, uniqueness, outsider in top-10, and first-five not all sequels.

**Checkpoint**: Selector proof obligations complete. Handler work may begin.

### Tests for Phase 3

- [ ] T010 [P] [US1] Author bounded FsCheck properties in `tests/LamuFlix.UnitTests/Features/Recommendations/MmrSelectorPropertyTests.cs` for valid inputs (unique IDs, finite nonnegative relevance, valid features/weights/lambda, nonnegative Take): count = min(Take, count); unique input-subset IDs; repeated-call determinism; permutation invariance including all-equal objectives; equivalence to an independent test-only full-rescan reference using the real scorer that recomputes max over all selected items and does not copy production incremental state; cross-Take prefix property including zero/oversized Takes.
- [ ] T011 [P] [US2] Author the Q5 sequel-chain fixture tests in `tests/LamuFlix.UnitTests/Features/Recommendations/MmrSelectorSequelChainTests.cs`: 11 unique candidates; Take = 10; lambda = 0.7; six sequels relevances 0.90…0.85 sharing nonempty genre set; five outsiders at 0.80 with mutually disjoint nonempty genre sets disjoint from the chain; genre-only SimilarityWeights; valid uniform GenreWeights; real DEV-338 scorer (no mock); assert relevance baseline starts with five sequels; assert MMR order 0.90 sequel, five outsiders ascending MovieId, then 0.89–0.86 sequels; assert ten unique results, at least one outsider in top ten, first five not all sequels; document step-two outsider 0.56 vs next sequel 0.323.

### Implementation for Phase 3

- [ ] T012 [US1] Keep fixture builders and the full-rescan reference test-local inside the three approved new test files only (`MmrSelectorTests.cs`, `MmrSelectorPropertyTests.cs`, `MmrSelectorSequelChainTests.cs`). Do not add adjacent helper files outside the Q7 eight-file inventory. No production reference implementation. Fix only selector defects in `src/LamuFlix.Core/Features/Recommendations/MmrSelector.cs` revealed by T010–T011.
- [ ] T013 [US1/US2] Drive T010–T011 green. Do not start Phase 4 until both are green.

**Checkpoint**: Phase 3 tests green. Commit `DEV-416 - MMR property and sequel-chain proofs`.

---

## Phase 4: Handler integration before Take (US3)

**Purpose**: Integrate MMR over the full scored pool before Take with configured lambda and original-payload mapping. Add handler coverage and necessary ordering expectation updates. Selector-only evidence does not complete this phase.

**Independent Test**: Pool larger than Take with diversifying item beyond relevance-only cutoff; MmrLambda sensitivity; Movie/Score/Reasons preserved; non-monotonic-score order retained; existing handler property/unit expectations updated only where they conflict with ratified MMR.

**Checkpoint**: Ticket end-to-end handler path evidenced. Ready for Cog refactor.

### Tests for Phase 4

- [ ] T014 [P] [US3] Author/extend unit tests in `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs` for pre-Take diversification on a pool larger than Take with a diversifying item beyond the relevance-only cutoff; configured MmrLambda sensitivity; original Movie/Score/Reasons preservation; explicit non-monotonic-score order assertion. Update existing ordering expectations only where they conflict with ratified MMR.
- [ ] T015 [P] [US3] Author/extend property/unit coverage in `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerPropertyTests.cs` only as required for ratified coverage/order under MMR.

### Implementation for Phase 4

- [ ] T016 [US3] After T014, narrowly modify `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs` to replace only the ranking/truncation stage: project each scored candidate once via `MovieFeaturesMapper.From`, call `MmrSelector.Select` over the full scored pool with `recommendationOptions.MmrLambda` and `query.Take`, map selected IDs to original Movie/blended Score/Reasons in selector order. Do not change RecommendedMovie/OpenAPI shapes. Do not edit RecommendationOptions, SimilarityScorer, BlendScorer, or MovieFeaturesMapper.
- [ ] T017 [US3] Drive T014–T015 green and confirm Phase 3 selector proofs still pass.

**Checkpoint**: Phase 4 tests green. Commit `DEV-416 - handler MMR before Take`.

---

## Phase 5: Refactor and verification (Cog, then Gauge)

**Purpose**: Not an Anvil phase. After Phases 1–4 are committed, Cog refactors within Q7 files. Gauge then runs the Q8 gates at the exact implementation head and fixed diff base, and reruns on the rebased head. Review loops follow brief Q1 closing bar and two-round cap.

**Checkpoint**: Refactor complexity is at or under the refactor threshold. Every mandatory gate has its script verdict. Scope-empty skip is not recorded as pass. Mutation and property tests executed with exit 0.

- [ ] T018 Refactor the completed change across the file set in `specs/DEV-416/plan.md` until cyclomatic complexity passes at the configured refactor threshold. Read `gates.complexity.refactor` from `harness.yml` and pass that configured value to the supported `-Threshold` parameter of `./scripts/run-cyclomatic-complexity.ps1`. Do not encode a task-owned numeric literal as configuration. Extract helpers, early returns, and guard clauses. Do not suppress to pass. Do not edit harness/config/generated files or weaken thresholds. Stay inside Q7 files.
- [ ] T019 Run the configured gates and record each script verdict: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` with no arguments (normal changed-diff gate; does not replace T018 stricter refactor evidence), `./scripts/run-jetbrains-inspectcode.ps1` with no arguments (changed scope, not `-All`), `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, worktree-safe `./scripts/run-mutation.ps1` without `--since` for changed eligible production projects, and `./scripts/run-web-gates.ps1`. Schedule Gauge to run the applicable web gate and record its actual script verdict; scope-empty is the expected outcome because `/web` is untouched, not a pre-label before execution. Mutation and property tests must execute and exit 0 — no property opt-out or threshold lowering. Surviving mutants route to missing-test remediation. Preserve Q8 verdict handling: skipped/opt-out/N/A/could-not-run never PASS. Rerun required verification after fixes and on the rebased head.

---

## Excluded (never scheduled)

UI / `design.md`, clarify/checklist, ADR, CONTEXT.md or ASSUMPTIONS.md rewrite, new schema/migration, new NuGet/npm dependency, new project/layer/port/DI service, public HTTP/OpenAPI/web contracts, pairwise matrix or cross-request cache, embedding term in redundancy, hard franchise quota, forced outsider insertion, scorer mock, benchmark/instrumentation seam, exact scorer-call-count assertion, LocalPlay/secrets/`Process.Start`, playback execution changes, edits to RecommendationOptions/MovieFeaturesMapper/SimilarityScorer/BlendScorer, lowering gate thresholds, mutation `--since`, and unrelated refactors. A discovered need outside this list stops for a decision. It is not absorbed into a phase.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase B entry**: Depends on Spec workspace handoff. TB001–TB004 must have recorded evidence before T001.
- **Phase 1**: Depends on Phase B entry. Contract/validation. Author T001/T002 first; T003–T004; drive T005 green before phase commit.
- **Phase 2**: Depends on Phase 1. Algorithm/ties/isolation. Author T006/T007; T008; drive T009 green.
- **Phase 3**: Depends on Phase 2. FsCheck/reference and sequel fixture. Author T010/T011 in parallel; T012 fixes only as needed; drive T013 green. Blocks Phase 4.
- **Phase 4**: Depends on Phase 3 (both selector proof obligations green). Author T014/T015; T016; drive T017 green.
- **Phase 5**: Depends on Phases 1–4 being committed. Cog owns T018. Gauge owns T019.

### User Story Dependencies

- **US1 (P1)**: Contract in Phase 1; algorithm in Phase 2; FsCheck/reference in Phase 3.
- **US2 (P1)**: Sequel fixture in Phase 3 (needs Phase 2 algorithm).
- **US3 (P1)**: Handler integration in Phase 4 (needs Phase 3 proofs green).

### Within Each User Story

- Prefer behavior test before or with the production edit. When types are absent, add minimum compilable contracts first.
- Phase tests green before that phase's commit.
- Anvil one production phase per ask.

### Parallel Opportunities

- TB001–TB004 are intake evidence gates; they complete before any Code Phase 1 task.
- T001 and T002 are sequential or combined single-owner work on the same file (`MmrSelectorTests.cs`); not parallel.
- T006 and T007 are sequential or combined single-owner work on the same file (`MmrSelectorTests.cs`); not parallel.
- T010 and T011 test authoring in parallel (different files) once Phase 2 is green.
- T014 and T015 test authoring in parallel when files differ.
- Single Anvil ownership for Phases 1–4; parallel markers never split two writers onto the same selector or shared test file.

## Parallel Example: Phase 3 tests

```text
Task: "T010 Author MmrSelectorPropertyTests FsCheck and full-rescan reference"
Task: "T011 Author MmrSelectorSequelChainTests Q5 fixture"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Finish Phase 1 contract/validation.
2. Finish Phase 2 algorithm.
3. Finish Phase 3 FsCheck/reference.
4. Stop there only to validate US1. Frozen scope still requires US2–US3 before review.

### Incremental Delivery

US2 lands with Phase 3 sequel fixture. US3 lands with Phase 4 handler integration. Phase 5 does not add behavior.

### Parallel Team Strategy

One implementer is the expected path, in phase order. Do not split two writers onto the same selector, handler, or shared test file.

## Notes

- [P] tasks use different files and do not depend on an unfinished task or shared test/minimal-contract file still being written by another unfinished task.
- Story labels map to `spec.md`.
- Commit after each phase, not after every task, using `DEV-416 - {phase subject}`.
- Gate 1 comes from the merged spec PR. Phase B entry (TB001–TB004) must also be evidenced before T001. This task list does not authorize implementation before that.
- New unanticipated files or decisions return to Keel; do not invent requirements.
- Spec workspace does not run gates, tests, or analyzers for this ask.
