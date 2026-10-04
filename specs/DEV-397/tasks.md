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

- [ ] T004 [US3] Add the `gates.mutation.exclusions` map value type in `scripts/_harness-config.ps1`: widen key grammar only for direct children of the exact anchored prefix; keep nested children, a scalar on `exclusions`, and near-miss neighbours as hard errors; add one prefix schema entry, an empty-map default in `Get-HarnessDefaults`, and one listing accessor while `Get-HarnessValue` stays scalar. Validate all entries before classification and before scope-empty; reasons must be nonblank, keys match ordinal-ignore-case, and `.csproj` suffix keys are configuration errors with exit 1. Preserve unknown-key rejection elsewhere.
- [ ] T005 [P] [US2] Add the dependency-free fixture in `scripts/Test-RunMutation.ps1`. Overlay gate files, `.config/dotnet-tools.json`, `stryker-config.json`, the solution with test projects, and merge-base-reaching git history through `HARNESS_REPO_ROOT`. Every case asserts exit code and verdict/classification text. Cases: (1) listed Api + eligible Infrastructure, exit 0; (2) listed Api only, exit 2 NOT APPLICABLE both with and without `-DryRun`; (3) unlisted Api + Infrastructure, exit 1 with both classifications; (4) blank reason, exit 1 configuration error; (5) listed-but-eligible warning while excluded.
- [ ] T006 [US3] Add the `LamuFlix.Api` exclusion and configured reason to `harness.yml` only after confirming OD-1 chose option (a). If option (b) was chosen, Keel amends the brief before Phase 2 implementation.

## Phase 3: User Story 1 - Run Eligible Projects in a Mixed Diff (Priority: P1)

**Goal**: Run Stryker on eligible changed projects while explicitly reporting excluded and failing unlisted projects.

**Independent Test**: Fixture listed Api + Infrastructure classification; then the real non-DryRun run against `9f92ad1` with per-project output, Infrastructure score, Api N/A, and exit code captured.

- [ ] T007 [US1] Replace fail-fast classification in `scripts/run-mutation.ps1` with classify-all behavior and per-project lines for eligible, excluded, and unlisted ineligible changed projects.
- [ ] T008 [US1] Ensure all eligible projects run and scores are reported before an unlisted ineligible project makes the overall real-run result exit 1.
- [ ] T009 [US1] Implement the specified exit-code table, keeping threshold sourced from `harness.yml`; preserve receipt-consumed verdict/native-exit output seams in `scripts/run-mutation.ps1`.

## Phase 4: User Story 2 - Complete DryRun Classification (Priority: P1)

**Goal**: Print one classification per changed project in DryRun without running Stryker.

**Independent Test**: Run fixture cases and ensure DryRun output covers every changed project, including unlisted Api, and exit behavior matches the spec.

- [ ] T010 [US2] Ensure `-DryRun` prints all classifications and implements all branches: exit 1 for invalid configuration or any unlisted ineligible project; exit 2 `SKIPPED` for scope-empty; exit 2 `NOT APPLICABLE` when every changed project is listed; otherwise exit 0 for classification only. Add classifications to the existing banner and generated-config output, preserving that output; do not invoke Stryker.
- [ ] T011 [US2] Ensure a listed project with eligible tests remains excluded and emits a stale-exclusion WARNING; unmatched valid entries remain silent.

## Phase 5: User Story 3 - Document and Validate Gate Outcomes (Priority: P1)

**Goal**: Keep script guidance, repository guidance, and task-pipeline policy consistent.

**Independent Test**: Inspect script help and `AGENTS.md` for verbatim table consistency; run all five fixture cases.

- [ ] T012 [US3] Update header and failure message in `scripts/run-mutation.ps1` to name both `*.ArchitectureTests` and `*.IntegrationTests`; the failure message also names `gates.mutation.exclusions` and matches behavior.
- [ ] T013 [US3] Document in script help that exclusion reasons must not contain ` #`; do not add validation or a sixth fixture case.
- [ ] T014 [US3] Add the verbatim exit table from `spec.md` to script help and `AGENTS.md`; identical header and row text after trimming leading whitespace on each line, with the table inside the `<# ... #>` comment-help block. Reconcile `AGENTS.md:124-125` so mutation exit 2 `NOT APPLICABLE` is mutation-only and non-blocking, while exit 2 `SKIPPED` stays scope-empty, blocking, and never green. Copy the `AGENTS.md` table from the help text after F1 so they match.
- [ ] T015 [US3] Bernstein applies the exact Phase 3 step 5 mutation-gate replacement from `brief.md` §6 to the `task-pipeline` note and reads it back into the PR body. Leave step 4's L-only `/architect` unchanged.

## Phase 6: Refactor

**Purpose**: Refactor the completed implementation before final runs and evidence collection.

- [ ] T016 Cog runs `/refactor` over the build.

## Phase 7: Final Runs

**Purpose**: Collect post-refactor fixture, real mixed-diff, and retrospective evidence in the required order.

- [ ] T017 [US2] Run `pwsh -NoProfile -File ./scripts/Test-RunMutation.ps1`; record the command and exit code.
- [ ] T018 [US1] Gauge runs `pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef 9f92ad1` without `-DryRun`; records exact verdict lines and exit code for the PR body.
- [ ] T019 [US4] Gauge runs `scripts/run-mutation.ps1 -Project LamuFlix.Infrastructure -BaseRef <verified DEV-309 pre-merge SHA>` and records score plus `BrowseMoviesQueryValidator` and `GetMovieDetailsQueryValidator` mutant results.
- [ ] T020 [US4] Anvil adds `LamuFlix.UnitTests` survivor-killing tests only for surviving mutants in the two named validators; record each outcome.
- [ ] T021 [US4] Rigger handles every other survivor, or any survivor requiring out-of-box changes, as a duplicate-checked follow-up with explicit disposition; do not treat follow-up creation as a passing gate.
- [ ] T022 [US4] Gauge reruns the DEV-309 retrospective after survivor tests and records score, per-validator results, and survivor dispositions in the PR body.

## Phase 8: Polish, Scope, and Gates

**Purpose**: Confirm frozen scope, compatibility, and gate evidence without confusing scope-empty results with passes.

- [ ] T023 [P] Confirm `scripts/new-mutation-receipt.ps1` still consumes the preserved raw-exit, final-verdict, and Stryker-native-exit output seams; for `-Project` naming a listed project, record that the helper fails the receipt because there is no report, as intended. Do not edit the helper.
- [ ] T024 Gauge records the nine commands named in `brief.md` §7 with exits: `run-roslyn-analyzers.ps1` (`-Files` on changed `.cs`, otherwise `-All`); `run-cyclomatic-complexity.ps1` (same selection); `run-jetbrains-inspectcode.ps1` (no arguments, never `-All`, exit 2 scope-empty recorded as such); `run-property-tests.ps1`; `run-vulnerable-packages.ps1`; `dotnet format --verify-no-changes`; `dotnet test`; `run-web-gates.ps1` exit 2 disabled/opt-out recorded as SKIP; and `pwsh -NoProfile -File ./scripts/Test-RunMutation.ps1`. Record each command and exit, not just a summary.
- [ ] T025 Gauge records own-diff mutation command, verdict, and exit verbatim; expected scripts-only exit 2 SKIPPED is scope-empty, never green. Keep AC7 unresolved pending OD-2.
- [ ] T026 Confirm the implementation diff stays within the frozen scope in `brief.md` §3, and capture final real-run and retrospective evidence in the PR body.

## Dependencies & Execution Order

- Phase 1 recon and owner checkboxes precede implementation and Gate 1.
- T004 precedes runner consumption of exclusion configuration; T005 is written before or alongside runner classification.
- T006 depends on OD-1 choosing option (a); if option (b), Keel amends the brief before Phase 2 implementation.
- T007-T011 and documentation tasks T012-T015 precede T016 (refactor).
- T016 precedes the final runs in order: T017 fixture rerun, T018 real Api-plus-Infrastructure run tagged US1, then the US4 retrospective block T019-T022. T020 depends on actual in-scope survivors from T019; T022 follows T020. T021 dispositions all out-of-scope survivors.
- The Phase 8 polish and gates follow all final runs; T023 preserves the receipt helper without editing it, and T024-T026 collect and reconcile final evidence.
- Documentation tasks T012-T015 must describe implemented behavior and preserve the exact pipeline note wording.

## Notes

- Do not implement while either owner checkbox keeps Gate 1 closed.
- Do not lower thresholds or substitute DryRun for real mutation evidence.
- Project references and test eligibility follow the existing direct-reference rule; `*.ArchitectureTests` and `*.IntegrationTests` remain excluded.
- No extra tests, files, policy entries, or scope beyond the brief are authorized.
