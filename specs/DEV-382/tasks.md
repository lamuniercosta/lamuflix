# Tasks: Mutation gate linkage fix and receipts

**Input**: `spec.md`, `plan.md`, `brief.md`. There is no test phase because the repo has no script-test harness (brief AC6). Verification tasks use the real reports and scratch fixtures.

## Phase 1: Setup

- [x] T001 [P] Pin `dotnet-stryker` 5.0.0 in `.config/dotnet-tools.json` (FR-001)
- [x] T002 [P] Add `test-runner: mtp` and `coverage-analysis: off` to `stryker-config.json` (FR-002)
- [x] T003 [P] Point `.specify/feature.json` at `specs/DEV-382`
- [x] T004 Run `dotnet tool restore` and confirm Stryker 5.0.0 is resolved (depends on T001)

## Phase 2: US1 + US2 — gate (P1)

All tasks in this phase edit `scripts/run-mutation.ps1`, so they run in order.

- [x] T005 [US1] Add the `-Project` and `-OutputRoot` params, update the help text and header comment, and add the tracked-config wiring check (FR-003, FR-009, FR-010)
- [x] T006 [US1] Add `Get-EligibleTestProjects`, built on `Get-TestProjects`, and fail closed when no project is eligible (FR-004, FR-005, FR-006)
- [x] T007 [US1] Carry `test-runner`, `coverage-analysis` and `test-projects` into generated configs, and apply the `-Project` filter (FR-003, FR-010)
- [x] T008 [US1] Run Stryker from the project dir with `-O` under the output root, copy the effective config, print native exits, and drop the StrykerOutput search (FR-009, FR-011)
- [x] T009 [US2] Add the tripwire and unattributed-kill checks to `Evaluate-MutationReport`, and reword the zero-kill reason (FR-007, FR-008)
- [x] T010 [US2] Verify `-EvaluateReport` against E1, E3, E4 and E5 plus synthetic fixtures (plan "Verification plan")
- [x] T011 [US1] Verify `-DryRun` on this branch, and in a detached DEV-296 checkout with the tooling overlaid, including `-Project Nope` and an in-repo `-OutputRoot`

## Phase 3: US3 — receipts (P2)

- [x] T012 [US3] Write `scripts/new-mutation-receipt.ps1` per plan D-3 (FR-012 to FR-015)
- [x] T013 [US3] Run the receipt script on the pushed `feature/DEV-296` head for Infrastructure and Api, writing to `specs/DEV-382/receipts/` (SC-003, SC-004)
- [x] T014 [US3] Check the receipts: killedBy names are present, there are no ArchitectureTests killers, the assembly verdict is recorded, no machine paths appear, and no leftover worktree remains

## Phase 4: Polish

- [x] T015 [P] Update the stale docs and comments found by the scout (FR-016)
- [x] T016 Confirm the thresholds are unchanged (SC-005), that no `StrykerOutput` exists in the repo, and that `git status` shows only frozen-scope files

## Dependencies

- Run T001–T003 in parallel, then T004, then T005–T009 in order.
- T010 and T011 need T009. T012 can start after T008.
- T013 needs T004, T011 and T012. T015 is independent.
