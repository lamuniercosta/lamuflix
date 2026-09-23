# Tasks: Restructure solution layout into src/ and tests/, delete legacy assets

**Input:** [spec.md](spec.md), [plan.md](plan.md)
**Branch:** `feature/290-spec` (spec only); Phase B uses its own task worktree from merged `main`.

**Phase A note:** This spec PR contains no implementation. The tasks below execute at the implement stage, in a task worktree off the merged `main`. Nothing here is committed or pushed by the spec PR.

**Tests:** No tests added. No test content changed (Q2). Acceptance tests (Gherkin/Reqnroll) are opted out (Q4).

---

## Phase A: Analysis and Gate 1

- [ ] T001 Run `/speckit-analyze` read-only on this ticket's Phase A artifacts. Persist the report byte-for-byte as `analyze.md` and record its SHA256. It must show Critical 0 and High 0. If a finding appears, Keel adjudicates; after every adjudication edit, rerun and re-persist. The plan is frozen only on a persisted report with Critical 0 and High 0 that is newer than every artifact edit. Cap is 2 rounds; an unresolved Round 2 stops to Patron.
- [ ] T002 Post the analysis findings and any adjudication summary as comments on the spec PR. Reply to each fixed finding with its fix and resolve the thread. The review round is not finished until it is on the PR. Depends on T001.

---

## Phase B0: Plan-time Recon Addendum (pickup prerequisite)

- [ ] T003 At pickup, **re-confirm** the following lines on the pickup base; if any line differs from what is recorded here, stop and report to Keel before making any edits.
  (a) Exact sln project-path lines (at 450e70f): sln:6 Test (`LamuFlix.Test\LamuFlix.Test.csproj`), sln:8 Worker (`LamuFlix.Work\LamuFlix.Worker.csproj`), sln:10 Data (`LamuFlix.Data\LamuFlix.Data.csproj`), sln:12 Web (`LamuFlix.Web\LamuFlix.Web.csproj`). Re-read these four lines on the pickup base; report any difference.
  (b) Exact ProjectReference lines (at 450e70f): `LamuFlix.Test\LamuFlix.Test.csproj`:19 Data, `:20` Web, `:21` Worker; `LamuFlix.Web\LamuFlix.Web.csproj`:19 Data; `LamuFlix.Work\LamuFlix.Worker.csproj`:18 Data. Re-read these five lines on the pickup base; report any difference.
  (c) Gauge baselines per recon A2 re-run (recon-DEV-290 section "Baseline gates (Gauge GREEN 2026-09-23 at 450e70f)"): `dotnet build LamuFlix.sln` — **0 warnings / 0 errors**; `dotnet test` — **18 passed / 4 skipped / 0 failed**; `run-roslyn-analyzers.ps1` exit **0**; `run-cyclomatic-complexity.ps1` (CC15) exit **0**; `run-cyclomatic-complexity.ps1 -Threshold 6` (CC6) exit **0**; `run-jetbrains-inspectcode.ps1` exit **0**. Re-measure on a moved `main` as a drift check; report any difference. Never measure baselines in the main checkout — use a throwaway detached worktree.
  Record confirmations in the PR "Verification evidence" section. This task is read-only; it changes only the PR evidence record.

---

## Phase B1: Deletions (S7, S8, S9)

- [ ] T004 `git rm -r SetupWorker/` (S7). Verify `Test-Path SetupWorker` returns False.
- [ ] T005 `git rm -r LamuFlix.WorkerSetup/` (S8). Verify `Test-Path LamuFlix.WorkerSetup` returns False.
- [ ] T006 Run `git grep -n -w Temp -- '*.cs'`; confirm the only hit is `LamuFlix.Data/Models/Temp.cs:5` (D3). The `-w` flag prevents false hits from `TempData`/`Template` in controllers and views. Then `git rm LamuFlix.Data/Models/Temp.cs` at its current pre-move path (S9). Verify `Test-Path LamuFlix.Data/Models/Temp.cs` returns False.
- [ ] T007 Commit: `DEV-290 - delete SetupWorker, LamuFlix.WorkerSetup, and Temp.cs`. Depends on T004–T006.

---

## Phase B2: Moves (S1–S4)

- [ ] T008 `git mv LamuFlix.Web src/LamuFlix.Web` (S1). Depends on T007.
- [ ] T009 `git mv LamuFlix.Data src/LamuFlix.Data` (S2). Depends on T007 (Temp.cs already deleted).
- [ ] T010 `git mv LamuFlix.Work src/LamuFlix.Worker` (S3, corrects stale folder name; no content change — csproj and namespaces already `LamuFlix.Worker`). Depends on T007.
- [ ] T011 `git mv LamuFlix.Test tests/LamuFlix.Test` (S4). Depends on T007.
- [ ] T012 Commit: `DEV-290 - move projects into src/ and tests/`. The build is intentionally broken here (path strings not yet updated). Depends on T008–T011.

---

## Phase B3: Path-string fixes (S5, S6)

- [ ] T013 Edit `LamuFlix.sln`: update sln:6 (Test → `tests\LamuFlix.Test\LamuFlix.Test.csproj`), sln:8 (Worker → `src\LamuFlix.Worker\LamuFlix.Worker.csproj`), sln:10 (Data → `src\LamuFlix.Data\LamuFlix.Data.csproj`), sln:12 (Web → `src\LamuFlix.Web\LamuFlix.Web.csproj`). Only path strings are changed (D4). Depends on T012.
- [ ] T014 Edit `LamuFlix.Test.csproj`: update `Test.csproj:19` (Data → `..\..\src\LamuFlix.Data\LamuFlix.Data.csproj`), `:20` (Web → `..\..\src\LamuFlix.Web\LamuFlix.Web.csproj`), `:21` (Worker → `..\..\src\LamuFlix.Worker\LamuFlix.Worker.csproj`) path strings. Only path strings are changed (D4). Depends on T012.
- [ ] T015 Commit: `DEV-290 - fix project path strings in sln and csproj files`. Depends on T013–T014.

---

## Phase B4: Evidence checks

- [ ] T016 [SC-001] Run `dotnet build LamuFlix.sln`. Record exit code and warning count. Warning count must be **0** (base is 0 warnings at 450e70f). Depends on T015.
- [ ] T017 [SC-002] Run `dotnet test`. Record exit code, pass count, skip count, fail count. Counts must match T003(c) base (18 passed / 4 skipped / 0 failed). Depends on T016.
- [ ] T018 [SC-003] Run `git diff -M100% --name-status 450e70f...HEAD`. Every path not listed below must show R100. The only other entries must be: the 3 deletions; sln as M; LamuFlix.Test.csproj as a D+A pair; and the additions `docs/adr/0013-src-tests-solution-layout.md` and `specs/DEV-290/*`. Also record `git diff -M100% --stat 450e70f...HEAD` as a supplement. Depends on T015.
- [ ] T019 [SC-004] Run `git grep -n -w Temp -- '*.cs'`. Before deletion the only hit was `LamuFlix.Data/Models/Temp.cs:5`; after deletion the command must return 0 lines (exit 1). The `-w` flag is required to prevent false hits from `TempData`/`Template` in controllers and views. Record the command and output. Depends on T015.
- [ ] T020 [SC-005] Run `Test-Path` for each old location: `LamuFlix.Web.old`, `SetupWorker`, `LamuFlix.WorkerSetup`, `LamuFlix.Work`, root `LamuFlix.Web`, root `LamuFlix.Data`, root `LamuFlix.Test`. All must return False. Verify D1 (LamuFlix.Web.old is absent). Depends on T015.
- [ ] T021 [SC-006] Run `dotnet format --verify-no-changes`. Record exit. Depends on T015.
- [ ] T022 [SC-007] Run the three pipeline gates with explicit `-Files` at the new project paths (pwsh 7): `run-roslyn-analyzers.ps1 -Files "src/LamuFlix.Data/*.cs","src/LamuFlix.Web/*.cs","src/LamuFlix.Worker/*.cs","tests/LamuFlix.Test/*.cs"`, `run-cyclomatic-complexity.ps1 -Files "src/LamuFlix.Data/*.cs","src/LamuFlix.Web/*.cs","src/LamuFlix.Worker/*.cs","tests/LamuFlix.Test/*.cs"` (threshold 15), `run-cyclomatic-complexity.ps1 -Files "src/LamuFlix.Data/*.cs","src/LamuFlix.Web/*.cs","src/LamuFlix.Worker/*.cs","tests/LamuFlix.Test/*.cs" -Threshold 6`, and `run-jetbrains-inspectcode.ps1`. Record all numeric exits and findings lists. Match findings against the T003(c) baseline by rule and code line, ignoring path prefix (D2). Apply the gate-finding ladder from plan.md Phase 4; a new finding stops to Patron. Pre-existing findings are listed as PR follow-ups (DEV-281, DEV-366). A skipped or unrunnable gate is not a pass. Depends on T016–T021.

---

## Phase B5: ADR (S12)

- [ ] T023 [SC-008] Commit `docs/adr/0013-src-tests-solution-layout.md` **verbatim** from `specs/DEV-290/brief.md` section "ADR 0013 draft (Keel, 2026-09-23)". The `docs/adr/` directory is absent at 450e70f; this commit creates it. Only the date field changes from the draft text. Commit: `DEV-290 - add ADR 0013 src-tests solution layout`. Depends on T022.

---

## Phase B6: Scope guard and handoff

- [ ] T024 Run `git diff 450e70f...HEAD --name-only`. Confirm the changed set matches the file impact boundary in plan.md exactly: renames under the four project folders; the sln and LamuFlix.Test.csproj edits; the three deletions; `docs/adr/0013-src-tests-solution-layout.md`; and `specs/DEV-290/*` (spec.md, plan.md, tasks.md, ASSUMPTIONS.md, CONCLUSIONS.md, brief.md, analyze.md). `.specify/feature.json` must not appear. Any file outside the boundary is a §2.3 question, not implied authorization. Record the check in the PR evidence section. Depends on T023.

---

## Dependencies and execution order

T001–T002 complete in Phase A before Gate 1. After the spec PR is merged, T003 is the Phase B pickup step. T004–T006 precede T007 (deletion commit). T008–T011 depend on T007 and can run in any order (disjoint folders). T012 follows T008–T011. T013–T014 depend on T012 and can run in any order (disjoint files). T015 follows T013–T014. T016–T021 depend on T015 and may run in parallel (independent checks). T022 depends on T016–T021. T023 follows T022. T024 follows T023.

## Implementation strategy

Deletions first (isolated commit), then moves (build intentionally broken), then path-string fixes (build restored), then evidence and gates, then ADR. No production code is changed. Every commit carries the `DEV-290 - ` prefix. Any discovery outside S1–S12 goes to Patron before any edit.
