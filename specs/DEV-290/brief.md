# DEV-290 — Restructure solution layout into src/ and tests/, delete legacy assets

**Ticket:** DEV-290 · parent DEV-281 · size:M · ui:none · base `origin/main` `450e70f` · branch `feature/290-spec` (worktree `F:\Dev\LamuFlix.worktrees\feature-290-spec`). Facts come from `recon-DEV-290`. The grill rulings are in [CONCLUSIONS.md](CONCLUSIONS.md): Q1–Q4 from the prior grill, re-held by Patron, and D1–D5 from the 2026-09-23 drift re-confirm. If a decision is not in this file, it has not been made.

## Frozen scope

This is the ticket's *Scope & Technical Design* with the Q1 and D1 rulings applied. Nothing else is in scope.

| # | Item | Action |
|---|---|---|
| S1 | `LamuFlix.Web/` → `src/LamuFlix.Web/` | `git mv`. The folder and project keep their names (Q2). |
| S2 | `LamuFlix.Data/` → `src/LamuFlix.Data/` | `git mv`. The folder and project keep their names (Q2). |
| S3 | `LamuFlix.Work/` → `src/LamuFlix.Worker/` | `git mv` corrects the stale folder name. The csproj and namespaces are already `LamuFlix.Worker` (recon §Solution), so no content changes. |
| S4 | `LamuFlix.Test/` → `tests/LamuFlix.Test/` | `git mv`. |
| S5 | `LamuFlix.sln` | Change only the path strings of its 4 project entries. |
| S6 | 3 `ProjectReference` lines in `LamuFlix.Test.csproj` (:19 Data, :20 Web, :21 Work) | Change only the path strings, keeping backslashes: `..\LamuFlix.X\` becomes `..\..\src\LamuFlix.X\` (Work becomes `src\LamuFlix.Worker\`) (D4, A1). `LamuFlix.Web.csproj:19` and `LamuFlix.Worker.csproj:18` are `..\LamuFlix.Data\…`. They still resolve after the moves because Web, Worker, and Data all land in `src/`, so they are **not edited** (recon-DEV-290 §Literal lines). |
| S7 | Delete `SetupWorker/` (`SetupWorker.vdproj`) | `git rm`. |
| S8 | Delete `LamuFlix.WorkerSetup/` (`LamuFlix.WorkerSetup.vdproj`) | `git rm`. |
| S9 | Delete `LamuFlix.Data/Models/Temp.cs` | `git rm`. Nothing references the file (D3). Delete it at its current path before the S2 move. |
| S10 | `LamuFlix.Web.old/` | **Void by drift (D1).** It is absent at `450e70f` and from all reachable history. There is no delete commit. An acceptance check verifies `Test-Path` is False. |
| S11 | Unused legacy bower libraries | **No-op (Q1).** All 8 `wwwroot/lib` folders are used by live views, so none are deleted. |
| S12 | `docs/adr/0013-src-tests-solution-layout.md` | The ADR is required (Q3). Its number is 0013 (D5). It must cite architecture plan §3 *Target Solution Layout* and §11 Epic 1 item 2, so it records a decision the plan already made. |

**Out of scope (Q2 plus the ticket):** any `.cs` content edit, including namespaces; renaming Web to Api or Data to Infrastructure; DEV-289 fixes; the legacy `packages/` folder; stale `.claude/worktrees/*`; `harness.yml` (`worktreeRoot` is machine config); `scripts/`; `.github/`; `stryker-config.json`; `Directory.*.props`. At `450e70f`, none of these files contains a project path (D4). If a file outside S5/S6 turns out to need a path edit, or any edit needs more than a path string, stop and send it to Patron before editing.

## Closing bar and round cap (Q4)

- **Bar:** only Critical or High findings with a concrete failure scenario block.
- **Frozen scope:** S1–S12 plus the ticket's acceptance criteria. Anything else becomes a follow-up issue, not a finding.
- **Cap:** 2 review rounds (`task-pipeline`).
- **Acceptance tests (Gherkin/Reqnroll):** opted out.

## Plan decisions

**Approach.** The restructure changes paths only. Every move uses `git mv`, so git records each file as a rename at 100% similarity. Delete S9 before moving Data (S2), so Temp.cs is never moved and then deleted. After the moves, fix the path strings in `.sln` and the csproj files in one commit. Then write the ADR.

**Files touched (complete list):**
- Renamed: every tracked file under the 4 project folders (S1–S4).
- Edited (path strings only): `LamuFlix.sln` (4 entries) and `LamuFlix.Test.csproj` (3 references). That is 2 files. `LamuFlix.Web.csproj` and `LamuFlix.Worker.csproj` are moved at R100 and not edited (A1, recon-DEV-290 §Literal lines). Quill cites the literal lines, with their backslashes, in `plan.md`.
- Deleted: `SetupWorker/SetupWorker.vdproj`, `LamuFlix.WorkerSetup/LamuFlix.WorkerSetup.vdproj`, `LamuFlix.Data/Models/Temp.cs`.
- Added: `docs/adr/0013-src-tests-solution-layout.md`, plus the `specs/DEV-290/*` artifacts.

**Test strategy.** No new tests; no test code changes (Q2). Evidence:
1. `dotnet build LamuFlix.sln` has 0 errors, and the warning count is no higher than the base count at `450e70f`, measured by Gauge.
2. `dotnet test` passes with the same number of tests as the base.
3. `git diff --find-renames=100% 450e70f...HEAD --stat` lists every moved `.cs` file as a pure rename. The only content diffs are in S5 and S6.
4. The zero-reference check for `Temp` returns no matches (D3).
5. `Test-Path` returns False for `LamuFlix.Web.old`, `SetupWorker`, `LamuFlix.WorkerSetup`, and the four old project folders.
6. `dotnet format --verify-no-changes` exits 0.

**Gate expectations (D2).** The three gates run on the moved set:
- `run-roslyn-analyzers.ps1`;
- `run-cyclomatic-complexity.ps1` at 15, and again with `-Threshold 6`;
- `run-jetbrains-inspectcode.ps1`.

The pass bar is **no finding missing from the `450e70f` baseline** of the same file set at its old paths. Findings are matched by rule and code line, ignoring the path prefix. Measure the baseline with explicit `-Files` on an unchanged `450e70f` checkout in a throwaway detached worktree, never in the main checkout. Record the numeric exit codes of all four runs for both base and head. Pre-existing hits are carried forward, never fixed or suppressed. List them in the PR as existing follow-ups (DEV-281, DEV-366), citing DEV-360 `plan.md:29` as precedent. If a **new** finding appears that only a `.cs` content edit could clear, do not edit and do not suppress. Report `blocked: structural` to Patron.

**Baseline evidence (A2, 2026-09-23).** The recorded `450e70f` gate baseline is exit codes only. Nothing proves `-Files` was used, and no findings lists were recorded. A no-arg run on a base equal to the default branch analyses zero files, so its exit 0 proves nothing. Before T022 can pass, a Gauge re-run must file the following in `recon-DEV-290`: the exact `-Files` set (every tracked `.cs` under the four project folders at their old paths, less `Temp.cs`), the command lines, the numeric exits, and the findings list of each of the four runs, where an empty list is recorded as `[]`. The head run uses the same set at the new paths, with explicit `-Files`.

**Task ordering.**
1. Plan-time recon addendum: the exact sln and csproj path lines, and the Gauge baselines (build warnings, test count, gate runs at `450e70f`).
2. `git rm` S7, S8, S9. Commit.
3. `git mv` S1–S4. Commit. The build breaks here, which is expected and short-lived.
4. Fix the path strings in S5 (`.sln`) and S6 (`LamuFlix.Test.csproj` only). Commit. The build is green again.
5. Run the evidence checks (items 1–6) and the gates against the baseline.
6. Write the ADR 0013 (S12). Commit.
7. Tick `tasks.md`.

Every commit message follows `DEV-290 - {subject}`.

**Phase A artifacts still to write.** Quill drafts `spec.md`, `plan.md`, and `tasks.md` from this brief with `/speckit-*`; the Spec Kit blocker is gone because `.specify/` is tracked on main (D5). Keel then runs `/speckit-analyze` read-only, and it must come back clean. Keel drafts the ADR 0013 text. The prior artifacts were never on disk (Patron fact, D5), so everything is rewritten from scratch.

## Gate 1 owner checkboxes

None. The src/tests moves and the deletions are named in the ticket. Q1 (no-op) and D1 (void by drift) remove work without adding or reordering any. No §2.3 item is open.

**Status: `gate1: provisional`** (Patron, 2026-09-23). The freeze `/speckit-analyze` is clean — Critical 0 / High 0 / Medium 2 / Low 3, coverage 19/19, 26 tasks, `analyze.md` SHA256 `2AB3B679…` — and the plan challenge is adjudicated (§"Plan challenge adjudication", D6/D7). The five findings are **not** Gate 1 checkboxes, and no owner checkbox is required: I1 (T022's `-Files` set vs D7) and I2 (the `$pickupBase` spec-entry wording vs D6) are wording that brings `tasks.md`/`spec.md`/`plan.md` in line with decisions already recorded, and they ride on the spec PR as owner review items; I3 (dependency wording T015 → T015a), I4 (shorthand "sln + Test.csproj"), and C1 (constitution §VIII note, no violation) are Low review items. All of it sits inside ticket-named scope, and Q1/D1 only remove work without adding or reordering any.

## Pending records (Rigger)

A recon comment for DEV-290 is queued: LamuFlix.Web.old is absent at `450e70f` and from reachable history, and `e65cd71` no longer resolves. Its body is in the canvas note `DEV-290-patron-youtrack-pending`. Rigger is not connected. Only Rigger writes to YouTrack.

## Taste

Logged `[assumed]` in [ASSUMPTIONS.md](ASSUMPTIONS.md).

## ADR 0013 draft (Keel, 2026-09-23)

At T023 the implementer commits this text verbatim as `docs/adr/0013-src-tests-solution-layout.md`. `docs/` does not exist at `450e70f`, so the commit creates it. The only change allowed is to the date.

```markdown
# 0013. Solution layout: src/ and tests/

- Status: Accepted
- Date: <commit date>
- Ticket: DEV-290 (parent DEV-281)

## Context

The solution kept all four projects in flat root folders: `LamuFlix.Web/`, `LamuFlix.Data/`,
`LamuFlix.Work/`, and `LamuFlix.Test/`. The folder `LamuFlix.Work/` did not match its project
and namespaces, which were already `LamuFlix.Worker`. The root also held two Visual Studio
installer projects (`SetupWorker/`, `LamuFlix.WorkerSetup/`) that the solution no longer builds.
The architecture plan, §3 *Target Solution Layout*, puts production projects under `src/` and
test projects under `tests/`. §11, Epic 1 item 2, schedules this move. The constitution names
the flat layout as Known Technical Debt ("Transitional layout").

## Decision

- Move the production projects to `src/LamuFlix.Web/`, `src/LamuFlix.Data/`, and
  `src/LamuFlix.Worker/`. The last move also corrects the stale folder name.
- Move the test project to `tests/LamuFlix.Test/`.
- Change only the path strings in `LamuFlix.sln` and in the `ProjectReference` items.
- Keep every project name and namespace. Web is not renamed to Api, and Data is not renamed to
  Infrastructure. Those renames belong to later Epic 1 work.
- Delete the installer projects and the unreferenced `LamuFlix.Data/Models/Temp.cs`.

## Consequences

- New projects go under `src/` or `tests/`.
- Each move is a pure `git mv`, so `git log --follow` and blame keep file history.
- The constitution's "Transitional layout" debt entry and its deferred TODO still describe the
  old folders. A follow-up ticket updates them.
- The later Epic 1 renames (Api, Infrastructure, Core) start from this layout.
```

## Plan challenge: Round 2 closure (Keel, 2026-09-23)

- **A1 (a decision change, recorded above in S6 and Files touched):** only `.sln` and `LamuFlix.Test.csproj` get path edits. Plan steps 3–4, T015, and T016 are dropped. This removes work and adds none, so no §2.3 item opens.
- **A2–A4:** Quill applies them as wording fixes to decisions already made. They add no new decision.
- **The cap is reached at 2 rounds (Q4), so the plan does not freeze by round.** The spec PR carries the following for owner review. They are review items, not Gate 1 checkboxes:
  1. The Round-3 wording edits (A2–A4) are applied after the cap. The persisted `analyze.md` either predates them or is re-run once, and it must show Critical 0 and High 0.
  2. The A2 Gauge re-run (the baseline with `-Files` and findings lists) gates T022. It is now filed in `recon-DEV-290` §"A2 re-run 2026-09-23": all four gates exit 0 with findings `[]`.
- **Gate 1 owner checkboxes:** still none.

## Plan challenge adjudication (Keel, 2026-09-23)

Axes challenged `69b4d40`, which is HEAD. Nothing in the findings was already fixed. Bar: Critical/High with a concrete failure blocks; Medium is fixed only when the fix is wording on a decision already made.

| Finding | Ruling | Reason |
|---|---|---|
| Compass F1 (High), `git mv` into missing `src/`, `tests/` | **Accept** | `git mv` does not create parent dirs (exit 128, reproduced). The S1–S4 destinations already imply the dirs; creating them is not new work. |
| Compass F2 (High), Sentry F4, Ledger 1, analyze I1: `-Files` globs | **Accept** | `_gate-common.ps1:438-440,467-469` matches literal paths only. The globs match nothing, roslyn/CC exit 0 vacuously, and SC-007 is unproven. |
| Compass F3 (Med), inspectcode run bare | **Accept** (merged into the row above) | `run-jetbrains-inspectcode.ps1:18,141,176` accepts `-Files`; analyze.md:12 is wrong. |
| Compass F4 (Low→Med), diff base `450e70f` | **Accept** | `origin/main` is already at `d1d9cfa` (DEV-362). `450e70f...HEAD` on a Phase B branch off main would list unrelated tickets' files and fail the boundary. |
| Sentry F1 (Med), stale bin/obj after moves | **Accept** (cheap) | Stale `obj/` from the old depth can make SC-001 evidence wrong. Purging untracked build output does not touch the diff. |
| analyze I2 (Low), wrong recon section cited | **Accept** (rides with T022) | T022 must match against the findings lists, and only §"A2 re-run 2026-09-23" has them. |
| Sentry F2 (Low), sln casing literals | **Reject**, already fixed | tasks.md:50-51 (T013/T014) pin the exact literals; the implementer runs tasks. |
| Sentry F3 (Low), rollback composition | **Reject**, below bar | Phase B is a task branch; to abandon it, discard the branch. No task reverts single commits. |

**Decision changes** (they replace Test strategy item 3 and the Gate expectations head-run wording above):
- **D6, diff base.** At T003 the implementer records `$pickupBase = git merge-base HEAD origin/main`. SC-003/T018 and T024 diff `$pickupBase...HEAD`, not `450e70f...HEAD`. The expected set is the file impact boundary, adjusted because `specs/DEV-290/*` is already on main: the only spec entry is `specs/DEV-290/tasks.md` as M.
- **D7, gate file set.** The head `-Files` set is built with `git ls-files`, never with a glob: `$files = @(git ls-files -- src/LamuFlix.Data src/LamuFlix.Web src/LamuFlix.Worker tests/LamuFlix.Test | Where-Object { $_ -like '*.cs' })`. The same `$files` goes to all four runs, inspectcode included. Guard: `$files.Count` must equal the base count, which is `git ls-tree -r --name-only 450e70f -- LamuFlix.Data LamuFlix.Web LamuFlix.Work LamuFlix.Test` filtered to `*.cs` less `Temp.cs`. Where a script prints an "analyzing N file(s)" line (inspectcode does), that line must show the same count. A count mismatch is a stop to Keel. It is not a pass.
