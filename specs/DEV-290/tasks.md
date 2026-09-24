# Tasks: Restructure solution layout into src/ and tests/, delete legacy assets

**Input:** [spec.md](spec.md), [plan.md](plan.md)
**Branch:** `feature/290-spec` (spec only); Phase B uses its own task worktree from merged `main`.

**Phase A note:** This spec PR contains no implementation. The tasks below execute at the implement stage, in a task worktree off the merged `main`. Nothing here is committed or pushed by the spec PR.

**Tests:** No tests added. No test content changed (Q2). Acceptance tests (Gherkin/Reqnroll) are opted out (Q4).

---

## Phase A: Analysis and Gate 1

- [x] T001 Run `/speckit-analyze` read-only on this ticket's Phase A artifacts. Persist the report byte-for-byte as `analyze.md` and record its SHA256. It must show Critical 0 and High 0. If a finding appears, Keel adjudicates; after every adjudication edit, rerun and re-persist. The plan is frozen only on a persisted report with Critical 0 and High 0 that is newer than every artifact edit. Cap is 2 rounds; an unresolved Round 2 stops to Patron.
- [x] T002 Post the analysis findings and any adjudication summary as comments on the spec PR. Reply to each fixed finding with its fix and resolve the thread. The review round is not finished until it is on the PR. Depends on T001.

---

## Phase B0: Plan-time Recon Addendum (pickup prerequisite)

- [x] T003 At pickup, **re-confirm** the following lines on the pickup base; if any line differs from what is recorded here, stop and report to Keel before making any edits. Record `pickupBase = $(git merge-base HEAD origin/main)` for use in T018, T024, and Phase 6. After the D13 rebase, `$pickupBase` must equal `8d1d183`; if it does not, stop and report to Keel. Baseline cites below stay at `1d9a616` (D13: no D7 file, sln or csproj changed `1d9a616..8d1d183`).
  (a) Exact sln project-path lines (at pickup base 1d9a616, recon W1; unchanged from 450e70f): sln:6 Test (`LamuFlix.Test\LamuFlix.Test.csproj`), sln:8 Worker (`LamuFlix.Work\LamuFlix.Worker.csproj`), sln:10 Data (`LamuFlix.Data\LamuFlix.Data.csproj`), sln:12 Web (`LamuFlix.Web\LamuFlix.Web.csproj`). Re-read these four lines on the pickup base; report any difference.
  (b) Exact ProjectReference lines (at pickup base 1d9a616, recon W1): `LamuFlix.Test\LamuFlix.Test.csproj`:19 Data, `:20` Web, `:21` Worker; `LamuFlix.Web\LamuFlix.Web.csproj`:18 Data (was :19 at 450e70f; DEV-362 removed one PackageReference line above it; content unchanged, and Web.csproj is not path-edited per A1); `LamuFlix.Work\LamuFlix.Worker.csproj`:18 Data. Re-read these five lines on the pickup base; report any difference.
  (c) Gauge baselines at pickup base 1d9a616 per recon-DEV-290 §"G1 baselines at 1d9a616" (D8, D9; explicit `-Files`, 52 files): `dotnet build LamuFlix.sln` — **0 warnings / 0 errors**; `dotnet test` — **18 passed / 4 skipped / 0 failed**; `run-roslyn-analyzers.ps1` exit **0** findings []; `run-cyclomatic-complexity.ps1` (CC15) exit **0** findings []; `run-cyclomatic-complexity.ps1 -Threshold 6` (CC6) exit **1**, 2 findings (HasQuery `ViewModelExtensions.cs:8` CC8; HasPropertyValue `:36` CC7); `run-jetbrains-inspectcode.ps1` exit **1**, 52 files analyzed, 16 warnings as listed in G1. A2 (450e70f) is superseded. Re-measure on a moved `main` as a drift check; report any difference. Never measure baselines in the main checkout — use a throwaway detached worktree.
  Record confirmations and pickupBase in the PR "Verification evidence" section. This task is read-only; it changes only the PR evidence record.

---

## Phase B1: Deletions (S7, S8, S9)

- [x] T004 `git rm -r SetupWorker/` (S7). Verify `Test-Path SetupWorker` returns False.
- [x] T005 `git rm -r LamuFlix.WorkerSetup/` (S8). Verify `Test-Path LamuFlix.WorkerSetup` returns False.
- [x] T006 Run `git grep -n -w Temp -- '*.cs'`; confirm the only hit is `LamuFlix.Data/Models/Temp.cs:5` (D3). The `-w` flag prevents false hits from `TempData`/`Template` in controllers and views. Then `git rm LamuFlix.Data/Models/Temp.cs` at its current pre-move path (S9). Verify `Test-Path LamuFlix.Data/Models/Temp.cs` returns False.
- [x] T007 Commit: `DEV-290 - delete SetupWorker, LamuFlix.WorkerSetup, and Temp.cs`. Depends on T004–T006.

---

## Phase B2: Moves (S1–S4)

- [x] T007a `New-Item -ItemType Directory -Force -Path src,tests`. Create the target directories before moving projects. Depends on T007.
- [x] T008 `git mv LamuFlix.Web src/LamuFlix.Web` (S1). Depends on T007a.
- [x] T009 `git mv LamuFlix.Data src/LamuFlix.Data` (S2). Depends on T007a (Temp.cs already deleted).
- [x] T010 `git mv LamuFlix.Work src/LamuFlix.Worker` (S3, corrects stale folder name; no content change — csproj and namespaces already `LamuFlix.Worker`). Depends on T007a.
- [x] T011 `git mv LamuFlix.Test tests/LamuFlix.Test` (S4). Depends on T007a.
- [x] T012 Commit: `DEV-290 - move projects into src/ and tests/`. The build is intentionally broken here (path strings not yet updated). Depends on T008–T011.

---

## Phase B3: Path-string fixes (S5, S6)

- [x] T013 Edit `LamuFlix.sln`: update sln:6 (Test → `tests\LamuFlix.Test\LamuFlix.Test.csproj`), sln:8 (Worker → `src\LamuFlix.Worker\LamuFlix.Worker.csproj`), sln:10 (Data → `src\LamuFlix.Data\LamuFlix.Data.csproj`), sln:12 (Web → `src\LamuFlix.Web\LamuFlix.Web.csproj`). Only path strings are changed (D4). Depends on T012.
- [x] T014 Edit `LamuFlix.Test.csproj`: update `Test.csproj:19` (Data → `..\..\src\LamuFlix.Data\LamuFlix.Data.csproj`), `:20` (Web → `..\..\src\LamuFlix.Web\LamuFlix.Web.csproj`), `:21` (Worker → `..\..\src\LamuFlix.Worker\LamuFlix.Worker.csproj`) path strings. Only path strings are changed (D4). Depends on T012.
- [x] T015 Commit: `DEV-290 - fix project path strings in sln and csproj files`. Depends on T013–T014.

---

## Phase B3a: Cleanup untracked build artifacts

- [x] T015a Purge untracked build output: `Get-ChildItem src,tests -Directory | ForEach-Object { Remove-Item -Recurse -Force (Join-Path $_.FullName bin),(Join-Path $_.FullName obj) -ErrorAction SilentlyContinue }`. This command changes no tracked file. Depends on T015.

---

## Phase B4: Evidence checks

- [x] T016 [SC-001] Run `dotnet build LamuFlix.sln`. Record exit code and warning count. Warning count must be **0** (base is 0 warnings at pickup base 1d9a616, G1). Depends on T015a.
- [x] T017 [SC-002] Run `dotnet test`. Record exit code, pass count, skip count, fail count. Counts must match T003(c) base (18 passed / 4 skipped / 0 failed). Depends on T016.
- [x] T018 [SC-003] Run `git diff -M100% --name-status $pickupBase...HEAD`. Every path not listed below must show R100. The only other entries must be: the 3 deletions; sln as M; LamuFlix.Test.csproj as a D+A pair; and the additions `docs/adr/0013-src-tests-solution-layout.md` and `specs/DEV-290/*`. Also record `git diff -M100% --stat $pickupBase...HEAD` as a supplement. Depends on T015a.
- [x] T019 [SC-004] Run `git grep -n -w Temp -- '*.cs'`. Before deletion the only hit was `LamuFlix.Data/Models/Temp.cs:5`; after deletion the command must return 0 lines (exit 1). The `-w` flag is required to prevent false hits from `TempData`/`Template` in controllers and views. Record the command and output. Depends on T015.
- [x] T020 [SC-005] Run `Test-Path` for each old location: `LamuFlix.Web.old`, `SetupWorker`, `LamuFlix.WorkerSetup`, `LamuFlix.Work`, root `LamuFlix.Web`, root `LamuFlix.Data`, root `LamuFlix.Test`. All must return False. Verify D1 (LamuFlix.Web.old is absent). Depends on T015.
- [x] T021 [SC-006] Run `dotnet format --verify-no-changes`. Record exit. Depends on T015.
- [x] T022 [SC-007] Run the four pipeline gates with files from `git ls-files` (D7). First, retrieve the tracked .cs files: `$files = @(git ls-files 'src/**/*.cs','tests/**/*.cs'); $fileCount = $files.Count; if ($fileCount -eq 0) { Write-Error "No .cs files found"; exit 1 }`. Pass the same `$files` array to all four gates (pwsh 7): `run-roslyn-analyzers.ps1 -Files $files`, `run-cyclomatic-complexity.ps1 -Files $files` (threshold 15), `run-cyclomatic-complexity.ps1 -Files $files -Threshold 6`, and `run-jetbrains-inspectcode.ps1 -Files $files`. Record all numeric exits, file count, and findings lists. Match findings against the T003(c) baseline by rule and code line, ignoring path prefix (D2, D9: baseline is the G1 findings lists at 1d9a616 — roslyn [], CC15 [], CC6 2, Inspect 16; a head exit 1 carrying only those findings is a pass, any finding not in G1 is new). Apply the gate-finding ladder from plan.md Phase 4; a new finding stops to Patron. Pre-existing findings are listed as PR follow-ups (DEV-281, DEV-366). A skipped or unrunnable gate is not a pass. Depends on T016–T021.

---

## Phase B5: ADR (S12)

- [x] T023 [SC-008] Commit `docs/adr/0013-src-tests-solution-layout.md` **verbatim** from `specs/DEV-290/brief.md` section "ADR 0013 draft (Keel, 2026-09-23)". The `docs/adr/` directory is absent at pickup base 1d9a616 (recon W2); this commit creates it. Only the date field changes from the draft text. Commit: `DEV-290 - add ADR 0013 src-tests solution layout`. Depends on T022.

---

## Phase B6: Scope guard and handoff

- [x] T024 Run `git diff $pickupBase...HEAD --name-only`. Confirm the changed set matches the file impact boundary in plan.md exactly: renames under the four project folders; the sln and LamuFlix.Test.csproj edits; the three deletions; `docs/adr/0013-src-tests-solution-layout.md`; and `specs/DEV-290/*` (spec.md, plan.md, tasks.md, ASSUMPTIONS.md, CONCLUSIONS.md, brief.md, analyze.md). `.specify/feature.json` must not appear. Any file outside the boundary is a §2.3 question, not implied authorization. Record the check in the PR evidence section. Depends on T023.

---

## Dependencies and execution order

T001–T002 complete in Phase A before Gate 1. After the spec PR is merged, T003 is the Phase B pickup step. T004–T006 precede T007 (deletion commit). T007a follows T007 (directory creation). T008–T011 depend on T007a and can run in any order (disjoint folders). T012 follows T008–T011. T013–T014 depend on T012 and can run in any order (disjoint files). T015 follows T013–T014. T015a follows T015 (cleanup). T016–T021 depend on T015a and may run in parallel (independent checks). T022 depends on T016–T021. T023 follows T022. T024 follows T023.

## Implementation strategy

Deletions first (isolated commit), then moves (build intentionally broken), then path-string fixes (build restored), then evidence and gates, then ADR. No production code is changed. Every commit carries the `DEV-290 - ` prefix. Any discovery outside S1–S12 goes to Patron before any edit.
