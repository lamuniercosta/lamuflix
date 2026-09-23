# Implementation Plan: Restructure solution layout into src/ and tests/, delete legacy assets

**Branch:** `feature/290-spec` | **Date:** 2026-09-23 | **Spec:** [spec.md](spec.md)

## Summary

This ticket changes paths only. Every project folder is moved under `src/` (Web, Data, Worker) or `tests/` (Test) using `git mv`, which records each file as a pure rename at 100% similarity. The stale `LamuFlix.Work/` folder name is corrected to `LamuFlix.Worker/` in the same pass. Three legacy assets — `SetupWorker/`, `LamuFlix.WorkerSetup/`, and `LamuFlix.Data/Models/Temp.cs` — are deleted before or alongside the moves. The `.sln` and LamuFlix.Test.csproj files get path-string-only updates. An ADR documents the decision (0013). No `.cs` content is changed, no tests are added or modified, and no dependency is introduced. This Phase A artifact authorizes no production edit.

## Technical Context

**Language/Version:** C# / .NET 10; PowerShell harness configuration.
**Primary Dependencies:** None added. The four projects and their existing dependencies are unchanged (Q2).
**Storage:** No schema or migration change.
**Testing:** No new tests; no test content edits (Q2). Verification is by `dotnet build`, `dotnet test`, `git diff --find-renames=100%`, `Test-Path` checks, and the three pipeline gates.
**Project Type:** Existing flat solution being reorganized into `src/` and `tests/`.
**Constraints:** No credential literal, new API, schema change, `.cs` content edit, namespace edit, or project rename beyond the folder-name correction of `LamuFlix.Work/` → `src/LamuFlix.Worker/` (Q2, D4). `LamuFlix.Web.old/` is absent; no delete commit (D1). Bower libraries are a no-op (Q1). Acceptance tests are opted out (Q4).

## Constitution Check

| Principle | Application | Status |
|---|---|---|
| I–IX — General principles | No new code path, port, filter, state machine, domain type, API, configuration binding, or telemetry. Every file moved is a pure rename; the only content change is path strings in S5/S6. No new test. | N/A — pure structural move |
| Known Technical Debt — transitional layout (constitution:426-429) | The flat-project layout is the known technical debt this ticket begins to resolve. The moves and deletions are Epic 1 item 2 in the architecture plan. | Pass — the ticket is the debt-retirement step |
| Static-Analysis Gates | Gates run on the moved file set, matched by rule and code line, ignoring the path prefix. Pre-existing findings are carried and listed as PR follow-ups (DEV-281, DEV-366; D2 and precedent DEV-360 `plan.md:29`). A new finding that only a `.cs` edit could clear is `blocked: structural` to Patron. CA1502 is never suppressed. | Pass in plan |
| Charter §2.3 | Q1–Q4 and D1–D5 closed every open item. No §2.3 item remains. No new dependency, project, layer, schema, or public API. No unnamed file rewrite. Any out-of-scope discovery goes to Patron. | Closed |

## Phase 0: Plan-time Recon Addendum (re-confirm at pickup)

The following lines were recorded by Wisp (recon), filed by the Conductor in recon-DEV-290 Addendum and Literal lines. At pickup the implementer re-reads each line on the pickup base; **if any differs, stop and report to Keel before editing any file.**

**Exact sln project-path lines** (`LamuFlix.sln`, at 450e70f):
- sln:6 — Test project entry (`LamuFlix.Test\LamuFlix.Test.csproj`)
- sln:8 — Worker project entry (`LamuFlix.Work\LamuFlix.Worker.csproj`)
- sln:10 — Data project entry (`LamuFlix.Data\LamuFlix.Data.csproj`)
- sln:12 — Web project entry (`LamuFlix.Web\LamuFlix.Web.csproj`)

**Exact ProjectReference lines** (at 450e70f):
- `LamuFlix.Test/LamuFlix.Test.csproj`:19 — reference to `LamuFlix.Data`
- `LamuFlix.Test/LamuFlix.Test.csproj`:20 — reference to `LamuFlix.Web`
- `LamuFlix.Test/LamuFlix.Test.csproj`:21 — reference to `LamuFlix.Worker`
- `LamuFlix.Web/LamuFlix.Web.csproj`:19 — reference to `LamuFlix.Data`
- `LamuFlix.Work/LamuFlix.Worker.csproj`:18 — reference to `LamuFlix.Data`

**Gauge baselines** (recon note `recon-DEV-290`, section "Baseline gates (Gauge GREEN 2026-09-23 at 450e70f)"):
- `dotnet build LamuFlix.sln`: **0 warnings / 0 errors**
- `dotnet test`: **18 passed / 4 skipped / 0 failed**
- `run-roslyn-analyzers.ps1`: exit **0**
- `run-cyclomatic-complexity.ps1` (CC15): exit **0**
- `run-cyclomatic-complexity.ps1 -Threshold 6` (CC6): exit **0**
- `run-jetbrains-inspectcode.ps1`: exit **0**

## Phase 1: Deletions (S7, S8, S9)

Delete the three legacy assets before any move, so that `Temp.cs` is never moved and then deleted, and the deletions are isolated in their own commit.

1. `git rm -r SetupWorker/` (S7).
2. `git rm -r LamuFlix.WorkerSetup/` (S8).
3. `git rm LamuFlix.Data/Models/Temp.cs` (S9, pre-move, zero references confirmed by D3).
4. Commit: `DEV-290 - delete SetupWorker, LamuFlix.WorkerSetup, and Temp.cs`.

## Phase 2: Moves (S1–S4)

Move all four project folders into their new locations. The build will break here until Phase 3 fixes the path strings; that is expected and short-lived.

1. `git mv LamuFlix.Web src/LamuFlix.Web` (S1).
2. `git mv LamuFlix.Data src/LamuFlix.Data` (S2, after the S9 deletion above).
3. `git mv LamuFlix.Work src/LamuFlix.Worker` (S3, corrects the stale folder name; csproj and namespaces already `LamuFlix.Worker`).
4. `git mv LamuFlix.Test tests/LamuFlix.Test` (S4).
5. Commit: `DEV-290 - move projects into src/ and tests/`.

## Phase 3: Path-string fixes (S5, S6)

Update only the path strings in the `.sln` and LamuFlix.Test.csproj files. No other content is touched. The exact source lines are cited below (from Phase 0 / recon addendum at 450e70f); the implementer re-confirms these match on the pickup base (T003) before editing.

1. Edit `LamuFlix.sln`: update sln:6 (Test), sln:8 (Worker), sln:10 (Data), sln:12 (Web) to the new locations.
2. Edit `LamuFlix.Test.csproj`: update `Test.csproj:19` (Data), `:20` (Web), `:21` (Worker) path strings.
3. Commit: `DEV-290 - fix project path strings in sln and csproj files`.

At this point `dotnet build LamuFlix.sln` must exit 0 with **0 warnings** (matching the 450e70f baseline) and `dotnet test` must pass with the same test count as the base.

## Phase 4: Evidence checks

Run and record all six acceptance checks before writing the ADR:

1. `dotnet build LamuFlix.sln` — 0 errors, **0 warnings** (base is 0 warnings at 450e70f; SC-001).
2. `dotnet test` — same pass/skip/fail counts as base (SC-002).
3. `git diff -M100% --name-status 450e70f...HEAD` — every path not listed below shows R100. The only other entries are the 3 deletions (S7–S9), sln as M, LamuFlix.Test.csproj as D+A pair, and the additions `docs/adr/0013` and `specs/DEV-290/*`. Use `--stat` as a supplement (SC-003).
4. `git grep -n -w Temp -- '*.cs'` — 0 hits (exit 1); before deletion the only hit was `LamuFlix.Data/Models/Temp.cs:5`; after deletion the command returns 0 lines. The `-w` flag prevents false hits from `TempData`/`Template` in controllers and views (SC-004).
5. `Test-Path` False for all old locations: `LamuFlix.Web.old`, `SetupWorker`, `LamuFlix.WorkerSetup`, `LamuFlix.Work`, root `LamuFlix.Web`, root `LamuFlix.Data`, root `LamuFlix.Test` (SC-005, D1).
6. `dotnet format --verify-no-changes` exits 0 (SC-006).

Then run the three pipeline gates on the moved file set, matching against the Phase 0 baseline by rule and code line (D2). Record all numeric exits (SC-007).

Gate-finding ladder (D2):
- (a) A new finding introduced by the DEV-290 diff: because this ticket changes path strings only, a new finding implies a gate configuration issue; stop and report to Patron.
- (b) A pre-existing finding: carry it forward; record it on the PR as a follow-up (DEV-281, DEV-366; precedent DEV-360 `plan.md:29`). Never fix, suppress, or lower a threshold.
- (c) A finding that only a `.cs` edit could clear: `blocked: structural` to Patron.

## Phase 5: ADR (S12)

Commit the ADR **verbatim** from the text in `specs/DEV-290/brief.md` section "ADR 0013 draft (Keel, 2026-09-23)" as `docs/adr/0013-src-tests-solution-layout.md`. The `docs/adr/` directory is absent at 450e70f; this commit creates it. Only the date field changes from the draft text. Commit: `DEV-290 - add ADR 0013 src-tests solution layout`.

## Phase 6: Task list tick and handoff

Tick tasks.md items. Run final scope-guard diff: `git diff 450e70f...HEAD --name-only`. The changed set is:
- Renamed: every tracked file under the four project folders (S1–S4).
- Edited (path strings only): `LamuFlix.sln`, `LamuFlix.Test.csproj`, `LamuFlix.Web.csproj`, `LamuFlix.Worker.csproj` (S5, S6).
- Deleted: `SetupWorker/SetupWorker.vdproj`, `LamuFlix.WorkerSetup/LamuFlix.WorkerSetup.vdproj`, `LamuFlix.Data/Models/Temp.cs` (S7–S9).
- Added: `docs/adr/0013-src-tests-solution-layout.md`, `specs/DEV-290/*` (spec.md, plan.md, tasks.md, ASSUMPTIONS.md, CONCLUSIONS.md, brief.md, analyze.md).
- `.specify/feature.json` is untracked and not ignored; it is **never committed**.
- No other file. A file outside this set is a §2.3 question, not implied authorization.

## Task ordering

1. Phase 0 recon addendum (read-only, pickup).
2. `git rm` S7, S8, S9. Commit.
3. `git mv` S1–S4. Commit. (Build breaks here — expected, short-lived.)
4. Fix path strings S5, S6. Commit. (Build is green again.)
5. Evidence checks (items 1–6) and gate runs against baseline.
6. Write ADR 0013 (S12). Commit.
7. Tick `tasks.md`.

Every commit message follows `DEV-290 - {subject}`.

## File impact boundary

Renamed: all tracked files under `LamuFlix.Web/`, `LamuFlix.Data/`, `LamuFlix.Work/`, `LamuFlix.Test/`.
Content-edited: `LamuFlix.sln`, `LamuFlix.Test.csproj` (path strings only).
Deleted: `SetupWorker/SetupWorker.vdproj`, `LamuFlix.WorkerSetup/LamuFlix.WorkerSetup.vdproj`, `LamuFlix.Data/Models/Temp.cs`.
Added: `docs/adr/0013-src-tests-solution-layout.md`, `specs/DEV-290/*` (spec.md, plan.md, tasks.md, ASSUMPTIONS.md, CONCLUSIONS.md, brief.md, analyze.md). `.specify/feature.json` is untracked and not ignored; never committed.
No other file. Anything outside this boundary is a §2.3 question, not implied authorization.

## Complexity tracking

No new architecture, project, layer, type, or behavior. The entire change is mechanical path-string editing and filesystem renames recorded by git.
