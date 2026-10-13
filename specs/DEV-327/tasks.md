# Tasks: DEV-327 — CI container build validation, Stryker Core gate, property-test gate

**Input**: `specs/DEV-327/spec.md`, `specs/DEV-327/plan.md`, `specs/DEV-327/brief.md` (ordering constraints 1–5), recon `recon-DEV-327`

**Prerequisites**: plan.md, spec.md (this file assumes both; no research.md / data-model.md / contracts/ — no data model or API surface in this ticket).

**Tests**: `scripts/Test-RunMutation.ps1` coverage is REQUIRED by Q4 (T003). Q5 test-raising, if triggered, is proven by the whole-Core score >= 80. No YAML unit tests; workflow verified by the CI run on the DEV-327 PR plus local script runs (brief Test strategy).

## Phase 1: Whole-project Stryker mode + coverage (blocks the Stryker CI job)

**Purpose**: Land the script mode the Stryker job invokes, with contract coverage, before the workflow references it (ordering constraint 2).

- [ ] T001 [US2] Add explicit whole-project mode for `LamuFlix.Core` in `scripts/run-mutation.ps1` (mutates whole `src/LamuFlix.Core/LamuFlix.Core.csproj`, `since` never enabled, threshold/test-project/FAIL-evaluation guardrails reused; diff-scoped behaviour and 0/1/2 contract unchanged). The mode MUST branch above the base-ref/merge-base resolution and diff discovery (run-mutation.ps1:499–535: no `origin/<base>` resolution, no scope-empty exit 2 on a zero-diff run) and select `LamuFlix.Core` directly, and MUST pass the enumerated files under the `LamuFlix.Core` mutate scope as the changed-file set to the FAIL evaluation so the outside-changed-files and changed-files-none-tested guards (run-mutation.ps1:314–345) stay meaningful. The mode MUST write to a deterministic whole-project output location passed explicitly as the output root (NOT the default temporary timestamped root `%TEMP%/LamuFlix-stryker/<UTC yyyyMMdd-HHmmss>-<6 hex>`, recon section 7): `<output-root>/LamuFlix.Core/`, with the Stryker report at `<output-root>/LamuFlix.Core/reports/mutation-report.json` and the HTML report at `<output-root>/LamuFlix.Core/reports/mutation-report.html` (reporters progress, html, json per tracked `stryker-config.json`, recon section 9)
- [ ] T002 [P] [US2] Local dry-run of the new mode classification (`-DryRun`) in `scripts/run-mutation.ps1` — record output in the task note
- [ ] T003 [US2] Extend `scripts/Test-RunMutation.ps1` to cover the new mode — classification via the whole-project dry-run, invocation via a stub `dotnet stryker` placed first on PATH in the fixture that writes a minimal `mutation-report.json` under the `-O` directory and exits 0 (no real Stryker run in the fixture), the zero-diff case, and both changed-file guards (outside-changed-files, changed-files-none-tested) under the whole-project changed-file set — and assert existing diff-scoped behaviour and 0/1/2 contract unchanged; `scripts/Test-RunMutation.ps1` green (Q4)

**Checkpoint**: Mode exists with coverage; nothing in ci.yml references it yet.

---

## Phase 2: Q5 baseline + conditional test-raising (before tasks are finalised)

**Purpose**: Measure first; cut test-raising work only from the measurement (ordering constraint 3).

- [ ] T004 [US2] Measure the whole-Core Stryker baseline for `LamuFlix.Core` with the T001 mode; record the raw score in `specs/DEV-327/plan.md` (replacing PENDING MEASUREMENT)
- [ ] T005 [US2] Conditional: if baseline < 80, size the gap-closing test work inside existing `LamuFlix.Test` / `LamuFlix.UnitTests` projects from named sizing inputs (tested mutant count, surviving mutant count, and the files carrying surviving mutants); if it exceeds size M, STOP and return to Patron before cutting further tasks (Q5). If baseline >= 80, mark this task N/A with the score cited — no test-raising.

**Checkpoint**: Baseline recorded; test-raising scope (or its absence) decided. A below-80 oversize gap parks here, not in a silent oversized list.

---

## Phase 3: ci.yml — three jobs in one edit (US1 + US2 + US3) 🎯 MVP

**Purpose**: One workflow edit adding all three jobs plus the Stryker artifact step (ordering constraint 4); `build-test-format` and `web` untouched. Phase 3 is BLOCKED until T004 (baseline recorded) and T005 (gap decision) are complete: a below-80 baseline that fits size M inserts T005a (test-raising) and T005b (re-measure) in Phase 2 and Phase 3 waits for T005b green; an oversize gap STOPS before Phase 3 and returns to Patron (Q5, brief lines 51, 74, 98).

- [ ] T005a [US2] Conditional (only when T005 finds baseline < 80 AND the gap fits size M): add gap-closing tests inside the existing `LamuFlix.Test` / `LamuFlix.UnitTests` projects only, following existing conventions (xUnit, `Method_Condition_Expected` naming) — concrete test files enumerated in the task note before writing; break stays 80, no threshold change
- [ ] T005b [US2] Conditional (only when T005a ran): re-measure whole-Core with the T001 mode at the T001 deterministic output location; record the new score in `specs/DEV-327/plan.md`; Phase 3 proceeds only at score >= 80, otherwise STOP and return to Patron
- [ ] T006 [US1] [US2] [US3] Edit `.github/workflows/ci.yml`: add `container-build`, `stryker-core`, `property-tests` jobs on `ubuntu-latest` with existing triggers, no path filters, following the plan step shape (`actions/checkout@v4`, `actions/setup-dotnet@v4` with `global-json-file: global.json` for the .NET jobs). Pass bar (FR-005, FR-007): the Stryker step passes only on whole-project-mode exit 0 and the step MUST fail the job on exit 1 or exit 2 (no swallowed `$LASTEXITCODE`); each property-test invocation MUST exit 0 or the job fails. `container-build` uses one step per Dockerfile, each step named for its file. `stryker-core` carries `timeout-minutes: 120`, runs `dotnet tool restore` (tracked manifest; must exit 0) before invoking the T001 mode with an explicit deterministic output root of `${{ runner.temp }}/stryker-core` (local runs: `<system temp>/stryker-core` with the same `LamuFlix.Core/` layout, recorded in the task note; the default temporary timestamped root is NOT used), and uploads the exact artifact path derived from it — `${{ runner.temp }}/stryker-core/LamuFlix.Core/reports/mutation-report.html` (plus `mutation-report.json` from the same `reports/` directory) — as `stryker-report-core` via `actions/upload-artifact@v4` with `if: always()` and `if-no-files-found: error`; `property-tests` runs the three enumerated `run-property-tests.ps1 -Project` invocations; no login, no secret beyond `GITHUB_TOKEN`, no `docker/*` action
- [ ] T007 [US3] Local run of each enumerated property-test invocation (`pwsh scripts/run-property-tests.ps1 -Project <each of the 3 csproj>`) — exit 0 each; record output in the task note

**Checkpoint**: Workflow edit complete in one commit; Stryker job references a mode that exists on the same branch.

---

## Phase 4: Verification (Q7 green bar; DEV-21-gated container proof)

**Purpose**: Prove the Q7 bar; container proof waits for DEV-21 (ordering constraint 1).

- [ ] T008 [US2] [US3] (Owner: Gauge) Push the DEV-327 branch and record CI evidence in the task note: `build-test-format`, `web`, `stryker-core` (tool-restore exit 0, mode exit 0, artifact present), `property-tests` (exit 0 x 3) green
- [ ] T008a [US2] When a failing Phase B Stryker run occurs: for a post-T006 CI `stryker-core` failure, record in the task note the `stryker-report-core` artifact presence (present on green AND red runs via `if: always()`; absence fails the upload step via `if-no-files-found: error`) and the job failure (exit 1, score cited); for a pre-T006 below-80 T004/T005b baseline run, record instead the local report files present at the T001 deterministic location and the failure (exit 1, score cited) — no workflow artifact exists yet; break stays 80 — never lowered to make the run pass
- [ ] T009 [US1] Container-job proof (GATED: requires DEV-21's three Dockerfiles merged to main + branch rebased; Owner: Bernstein sequences this task against the DEV-21 merge per CONCLUSIONS.md Q2): CI `container-build` job green with all three `docker build -f <file> .` exit 0; if DEV-21 has not merged, this task WAITS while T001–T008 proceed; record the green run in the task note
- [ ] T010 (Owner: Gauge) Static-analysis + format on touched `.cs`/script files per repo gates (`run-roslyn-analyzers`, `run-cyclomatic-complexity`, `run-jetbrains-inspectcode` scope rules; `dotnet format --verify-no-changes`); never lower a threshold

**Checkpoint**: All five jobs green (T009 completes the set once DEV-21 lands); evidence recorded.

---

## Dependencies & Execution Order

- T001 → T002, T003 (mode before its coverage/dry-run); T001+T003 → T004 (baseline needs the covered mode); T004 → T005 (no test-raising before measurement); T004+T005 → Phase 3 (no T006 until baseline recorded and gap decided; below-80 in-size-M inserts T005a → T005b before T006, oversize STOPS for Patron before Phase 3); T001+T005b → T006 (workflow never references a missing mode and never precedes the Q5 decision); T006 → T008; T008a runs when a failing Stryker run occurs (CI failure or below-80 baseline); DEV-21 merge → T009 (T009 waits, others proceed); all → T010.
- Parallel: T002 + T003 (different evidence, same mode); T008 + T009 once DEV-21 lands.
- Forbidden in every task (brief constraint 5 — a violation is struck at /speckit-analyze): registry push/login, secret, `docker/*` action, Dockerfile creation, threshold change, edits to `harness.yml` / `stryker-config.json` / `scripts/run-property-tests.ps1` / `gitleaks.yml`.

## Implementation Strategy

- MVP = T001–T008 (Stryker + property-test signal without the DEV-21-gated container proof), plus conditional T005a–T005b and T008a only when their triggers occur; T009 completes the Q7 five-job green once DEV-21 merges.
- No new test project; no changes beyond `.github/workflows/ci.yml`, `scripts/run-mutation.ps1`, `scripts/Test-RunMutation.ps1`, and conditional Q5 test files in the two existing test projects.
