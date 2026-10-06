# Tasks: Reconcile Enrichment Attempt Counting

**Input**: `spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md` (Q1-Q6) from `/specs/DEV-390/`

**Prerequisites**: plan.md (required), spec.md (required for user stories)

**Tests**: Tests are REQUIRED here by the spec (FR-008, FR-010): tests-first order is binding for this ticket. Changed-contract cases must fail before the fix; regression guards may pass before the fix. T006 must show the claim-plus-outcome double-count failures before T007.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths in every description

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the frozen starting point; no implementation.

- [ ] T001 [US1] Record the Phase A receipt: spec worktree `F:/Dev/LamuFlix.worktrees/feature-390-spec` on branch `feature/390-spec` holds only the uncommitted `.specify/feature.json` pin plus `specs/DEV-390/{brief,CONCLUSIONS,ASSUMPTIONS,spec,plan,tasks}.md`; no implementation. T002 onward is blocked until Gate 1 user merge of the spec PR plus Phase B drift recon and Keel analysis; implementation then runs in the Conductor-designated delivery worktree after verification (no path invented here).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Contract tests that prove the defect (changed-contract cases fail before the fix; regression guards may pass); nothing here changes production code.

**CRITICAL**: No user story implementation (Phase 3+) can begin until this phase is complete.

- [X] T002 [US1] Add exact-count seam cases in `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs` (existing fixtures, no new file; tests-first, before any production edit): explicitly update the existing `TryClaim_ClaimStateSurvivesWatchlistAndMarkSaves` expectation at `EfMovieRepositoryTests.cs:209` from 2 to 1 while retaining its watchlist-save preservation checks; claim from N > 0 gives N+1; claim plus success / not-found / terminal failure stays N+1 via two sequences — (a) fresh load: successful claim with no aggregate preloaded, then load aggregate and baseline at N+1, apply each outcome, save and reload N+1; (b) preloaded before claim: aggregate loaded at N before claim, D8 sync, outcome delta zero, persisted reload N+1 per outcome (FR-004 mapped to both); watchlist save preserves the claimed N+1 (exact persisted reload). Changed-contract claim-plus-outcome cases must fail before the fix; regression guards may pass.
- [X] T003 [US2] Add exact-count seam cases in `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs` (existing fixtures, no new file; after T002 — same file, not parallel): refused/double/concurrent claims add zero beyond winning claims; Retry/RetryDelayed without `Mark*` leaves N+1, next claim N+2, final outcome N+2; manual retry saves then enqueues wire 1 at N, next claim N+1, final outcome N+1; enqueue/requeue alone unchanged; `TimeProvider` lease advancement, no sleeps. Changed-contract cases must fail before the fix; regression guards (successful claim alone, refusal, no-outcome retry, manual request, enqueue-only) may pass.
- [X] T004 [P] [US1] Update domain counter expectations to failing in `tests/LamuFlix.Test/Domain/MovieTests.cs`: every legal `Mark*` keeps count N; `RequestEnrichment` and rejected transitions keep history.
- [X] T005 [P] [US1] Update the FsCheck transition-table counter invariant to failing in `tests/LamuFlix.Test/Domain/PropertyTests.cs`: count unchanged for domain actions, other expectations kept.
- [X] T006 Run the Phase 2 tests and show the claim-plus-outcome double-count failures (claim + `Mark*` persisting above N+1) before T007; record the failure output for the PR. Regression guards may pass; the changed-contract outcome-after-claim cases must fail here.

**Checkpoint**: Contract tests written (changed-contract failures shown at T006); defect demonstrated; no production edit yet.

---

## Phase 3: User Story 1 - One claim plus one outcome records one processing attempt (Priority: P1)

**Goal**: Persisted counter equals cumulative successful claims after the outcome path.

**Independent Test**: T002 cases pass: every claim-plus-outcome path reloads N+1 for fresh and preloaded aggregates.

- [X] T007 [US1] Remove only `EnrichmentAttempts++` from `BeginPendingAttempt` in `src/LamuFlix.Core/Domain/Movie.cs`; keep `RequirePending`, `LastAttemptAt = now`, and all `Mark*` effects.
- [X] T008 [P] [US1] Update counter assertions to the accepted contract in `tests/LamuFlix.UnitTests/Features/Enrichment/ApplyEnrichmentResultCommandHandlerTests.cs`.
- [X] T009 [P] [US1] Update counter assertions to the accepted contract in `tests/LamuFlix.UnitTests/Features/Enrichment/ProcessEnrichmentCommandHandlerTests.cs`.
- [X] T010 [P] [US1] Update counter assertions to the accepted contract in `tests/LamuFlix.UnitTests/Features/Enrichment/RecordEnrichmentFailureCommandHandlerTests.cs`.
- [X] T011 [US1] Keep refused/double/concurrent-claim proof green in `tests/LamuFlix.IntegrationTests/EfMovieRepositoryClaimConcurrencyTests.cs`; retain (do not weaken) the concurrency coverage.
- [X] T012 [US1] Tighten deterministic consumer assertions to exact counts in `tests/LamuFlix.IntegrationTests/EnrichmentConsumerTests.cs` (secondary to the real seam).

**Checkpoint**: User Story 1 fully functional and testable independently; claim-plus-any-outcome reloads N+1.

---

## Phase 4: User Story 2 - Retries and manual retries count claims, not messages (Priority: P1)

**Goal**: Retry, re-claim, and manual-retry flows count exactly one per successful claim with wire Attempt kept separate.

**Independent Test**: T003 cases pass with exact N/N+1/N+2 values on the real seam.

- [X] T013 [P] [US2] Update counter assertions to the accepted contract in `tests/LamuFlix.UnitTests/Features/Enrichment/RequestEnrichmentCommandHandlerTests.cs`; keep invalid-state rejection and save-before-enqueue ordering.
- [X] T014 [P] [US2] Update counter assertions to the accepted contract in `tests/LamuFlix.UnitTests/Features/Enrichment/RequeueStrandedMoviesCommandHandlerTests.cs`.
- [X] T015 [US2] Verify no wire-to-counter coupling: `EnrichmentRequested.Attempt` is never written to `EnrichmentAttempts` on any handler path (code inspection against the frozen file set; no new assertion file).

**Checkpoint**: User Stories 1 AND 2 both work independently with exact counter semantics.

---

## Phase 5: User Story 3 - Consumers read the documented counter meaning (Priority: P2)

**Goal**: Glossary states the claim-count meaning, the separate wire meaning, and the historical-overcount caveat.

**Independent Test**: `CONTEXT.md` entries read as specified in spec User Story 3.

- [X] T016 [US3] Narrow the `EnrichmentAttempts` glossary entry in `CONTEXT.md` to cumulative successful claims plus the pre-DEV-390 overcount caveat, and add the separate transport retry Attempt term; no other `CONTEXT.md` edits.

**Checkpoint**: All user stories independently functional; docs match behavior.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verification and PR readiness within the frozen scope.

- [X] T017 Run the static-analysis gates on changed `.cs` files: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (then `-Threshold 6` refactor gate), `./scripts/run-jetbrains-inspectcode.ps1`; then `dotnet format --verify-no-changes`.
- [X] T018 Run `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet test`, and `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (never `--since` from a worktree); record each verdict from the script itself. FAILED or Could not run blocks; scope-empty exit 2 is non-blocking SKIPPED (scope-empty), never PASS; gate-disabled opt-out is SKIP; mutation N/A only on the script's exit-2 verdict; unaccepted property-test exit 2 blocks.
- [X] T019 Confirm `git diff origin/main...HEAD --stat` shows only the frozen-scope files; confirm no migration, backfill, new dependency, project, schema, API, LocalPlay, secret, or `Process.Start` change.

---

## Dependencies & Execution Order

- **Setup (Phase 1)**: No dependencies - can start immediately.
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories. T002 onward additionally requires Gate 1 user merge plus Phase B drift recon and Keel analysis (see T001). Tests MUST be written before implementation (binding for DEV-390); changed-contract cases MUST FAIL before implementation, regression guards may pass.
- **User Stories (Phases 3-5)**: Depend on Foundational; proceed in priority order P1 to P2.
- **Polish (Phase 6)**: Depends on all user stories being complete.

### Within Each User Story

- Tests (Phase 2) written before the T007 fix; changed-contract cases FAIL before the T007 fix, regression guards may pass.
- Domain fix before handler/consumer assertion updates.
- Glossary update (T016) after the fix so docs match behavior.
- Story complete before moving to next priority.

### Parallel Opportunities

- T004-T005 can run in parallel (different files, no dependencies). T002 then T003 in order (same file, not parallel).
- T008-T010 can run in parallel (different handler test files).
- T013-T014 can run in parallel (different handler test files).

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational (CRITICAL) — contract tests written, T006 double-count failures shown.
3. Complete Phase 3: User Story 1 (T007 + T008-T012).
4. STOP and VALIDATE: every claim-plus-outcome path reloads N+1.

### Incremental Delivery

1. Setup + Foundational: contract tests ready (changed-contract failures shown at T006).
2. Add User Story 1: single-line fix, core defect closed.
3. Add User Story 2: retry/manual-retry exact counts green.
4. Add User Story 3: glossary matches behavior.

---

## Notes

- [P] tasks = different files, no dependencies.
- [Story] label maps each task to its user story for traceability.
- Frozen scope only: anything outside the FR-007 file set is a follow-up issue, not a finding in this round (adapter simplification and sweep budget limitation are noted, no ticket).
- Never lower a gate threshold; never edit a generated file; never mock the data-access driver for seam behavior.
