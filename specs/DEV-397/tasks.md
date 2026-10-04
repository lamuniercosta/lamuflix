# Tasks: DEV-397 Mutation Runner Api Diff Support

**Input**: Design documents from `specs/DEV-397/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [brief.md](brief.md), [CONCLUSIONS.md](CONCLUSIONS.md)

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches distinct files and has no dependency on another task.
- **[Story]**: User story covered by the task.

## Phase 1: Phase B Recon and Owner Gate

**Purpose**: Reconcile the frozen scope with the implementation base and obtain required owner decisions before Gate 1/implementation proceeds.

- [ ] T001 Verify drift from `origin/main` at `1e6830b` for F1-F5 files in `brief.md` §3 and record evidence in Phase B recon.
- [ ] T002 Establish and verify the DEV-309 pre-merge base SHA for its retrospective run; record evidence in Phase B recon.
- [ ] T003 Record owner answers for OD-1 and OD-2 in the spec PR; keep Gate 1 closed until both checkboxes are answered.

## Phase 2: Foundational Configuration and Fixture

**Purpose**: Establish exclusion policy parsing and the behavioral fixture before changing runner classification.

- [ ] T004 [US3] Add the `gates.mutation.exclusions` map value type, default, and nonblank reason validation in `scripts/_harness-config.ps1`; preserve unknown-key rejection outside the map.
- [ ] T005 [P] [US2] Add the five dependency-free fixture cases in `scripts/Test-RunMutation.ps1`, covering the exact exit and classification requirements in `spec.md`.
- [ ] T006 [US3] Add the `LamuFlix.Api` exclusion and configured reason to `harness.yml` only after confirming OD-1 chose option (a); if option (b) was chosen, follow the brief's authorized amendment before Phase B implementation.

## Phase 3: User Story 1 - Run Eligible Projects in a Mixed Diff (Priority: P1)

**Goal**: Run Stryker on eligible changed projects while explicitly reporting excluded and failing unlisted projects.

**Independent Test**: Fixture listed Api + Infrastructure classification; then the real non-DryRun run against `9f92ad1` with per-project output, Infrastructure score, Api N/A, and exit code captured.

- [ ] T007 [US1] Replace fail-fast classification in `scripts/run-mutation.ps1` with classify-all behavior and per-project lines for eligible, excluded, and unlisted ineligible changed projects.
- [ ] T008 [US1] Ensure all eligible projects run and scores are reported before an unlisted ineligible project makes the overall real-run result exit 1.
- [ ] T009 [US1] Implement the specified exit-code table, keeping threshold sourced from `harness.yml`; preserve receipt-consumed verdict/native-exit output seams in `scripts/run-mutation.ps1`.


## Phase 4: User Story 2 - Complete DryRun Classification (Priority: P1)

**Goal**: Print one classification per changed project in DryRun without running Stryker.

**Independent Test**: Run fixture cases and ensure DryRun output covers every changed project, including unlisted Api, and exit behavior matches the spec.

- [ ] T011 [US2] Ensure `-DryRun` prints all classifications and returns 1 for invalid configuration or any unlisted ineligible project without invoking Stryker.
- [ ] T012 [US2] Ensure a listed project with eligible tests remains excluded and emits a stale-exclusion WARNING; unmatched valid entries remain silent.

## Phase 5: User Story 3 - Document and Validate Gate Outcomes (Priority: P1)

**Goal**: Keep script guidance, repository guidance, and task-pipeline policy consistent.

**Independent Test**: Inspect script help and `AGENTS.md` for verbatim table consistency; run all five fixture cases.

- [ ] T013 [US3] Update header and failure message in `scripts/run-mutation.ps1` to name both `*.ArchitectureTests` and `*.IntegrationTests`; the failure message also names `gates.mutation.exclusions` and matches behavior.
- [ ] T014 [US3] Add the verbatim exit table from `spec.md` to script help and `AGENTS.md`; copy the `AGENTS.md` table from the script help text after F1 so the two cannot differ.
- [ ] T015 [US3] Bernstein applies the exact Phase 3 step 5 mutation-gate replacement from `brief.md` §6 to the `task-pipeline` note and reads it back into the PR body. Leave step 4's L-only `/architect` unchanged.
- [ ] T016 [US2] Run `pwsh -NoProfile -File ./scripts/Test-RunMutation.ps1`; record the command and exit code.

## Phase 6: Refactor and Final Runs

**Purpose**: Refactor the completed build before collecting final mutation and gate evidence.

- [ ] T025 Cog runs `/refactor` over the build.
- [ ] T010 [US1] Gauge runs `pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef 9f92ad1` without `-DryRun`; records exact verdict lines and exit code for the PR body.

## Phase 7: User Story 4 - Retrospective DEV-309 Validator Evidence (Priority: P2)

**Goal**: Measure the two DEV-309 Infrastructure validators and explicitly disposition survivors.

**Independent Test**: Retrospective run at verified pre-merge SHA records Infrastructure score and results for both validators; after permitted tests, rerun and record final evidence.

- [ ] T017 [US4] Gauge runs `scripts/run-mutation.ps1 -Project LamuFlix.Infrastructure -BaseRef <verified DEV-309 pre-merge SHA>` and records score plus `BrowseMoviesQueryValidator` and `GetMovieDetailsQueryValidator` mutant results.
- [ ] T018 [US4] Anvil adds `LamuFlix.UnitTests` survivor-killing tests only for surviving mutants in the two named validators; record each outcome.
- [ ] T019 [US4] Rigger handles every other survivor, or any survivor requiring out-of-box changes, as a duplicate-checked follow-up with explicit disposition; do not treat follow-up creation as a passing gate.
- [ ] T020 [US4] Gauge reruns the DEV-309 retrospective after survivor tests and records score, per-validator results, and survivor dispositions in the PR body.

## Phase 8: Polish, Scope, and Gates

**Purpose**: Confirm frozen scope, compatibility, and gate evidence without confusing scope-empty results with passes.

- [ ] T021 [P] Confirm `scripts/new-mutation-receipt.ps1` still consumes the preserved raw-exit, final-verdict, and Stryker-native-exit output seams; do not edit the helper.
- [ ] T022 Gauge records every applicable gate command and exit code per `brief.md` §7: run Roslyn and complexity with `-Files` on changed `.cs` files, otherwise `-All`; run InspectCode with no arguments, never `-All`, recording exit 2 scope-empty as such; record `Test-RunMutation.ps1` exit 0. Report disabled web gate exit 2 as opt-out SKIP, never PASS.
- [ ] T023 Gauge records own-diff mutation command, verdict, and exit verbatim; expected scripts-only exit 2 SKIPPED is scope-empty, never green. Keep AC7 unresolved pending OD-2.
- [ ] T024 Confirm the implementation diff stays within the frozen scope in `brief.md` §3, and capture final real-run and retrospective evidence in the PR body.
## Dependencies & Execution Order

- Phase 1 recon and owner checkboxes precede implementation and Gate 1.
- T004 precedes runner consumption of exclusion configuration; T005 is written before or alongside runner classification.
- T006 depends on OD-1 choosing option (a); if option (b), Keel amends the brief before implementation.
- T007-T009 and documentation tasks T013-T015 precede T025 (Cog `/refactor`); T010 (real Api-plus-Infrastructure run), T016 (final fixture run), T017 (retrospective), and T020 (retrospective rerun) follow T025.
- Runner behavior (T007-T012) and documentation tasks (T013-T015) precede T025; T025 precedes final fixture run (T016).
- T017 establishes survivor evidence; T018 depends on actual survivors; T020 follows T018. T019 applies to all out-of-scope survivors.
- Documentation tasks T013-T015 must describe implemented behavior and preserve the exact pipeline note wording.
- All final gates and evidence collation follow implementation and survivor remediation.

## Notes

- Do not implement while either owner checkbox keeps Gate 1 closed.
- Do not lower thresholds or substitute DryRun for real mutation evidence.
- Project references and test eligibility follow the existing direct-reference rule; `*.ArchitectureTests` and `*.IntegrationTests` remain excluded.
- No extra tests, files, policy entries, or scope beyond the brief are authorized.
