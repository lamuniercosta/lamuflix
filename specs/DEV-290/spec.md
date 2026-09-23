# Feature Specification: Restructure solution layout into src/ and tests/, delete legacy assets

**Feature Branch**: `feature/290-spec`
**Created**: 2026-09-23
**Status**: Phase A — spec PR open, Gate 1 needs no owner checkbox
**Input**: DEV-290 Scope & Technical Design; grill rulings Q1–Q4 and drift re-confirms D1–D5 in [CONCLUSIONS.md](CONCLUSIONS.md); recon facts from `recon-DEV-290`; [ASSUMPTIONS.md](ASSUMPTIONS.md)

## User Scenarios & Testing

### User Story 1 — Project folders live under src/ and tests/ (Priority: P1)

As a developer, I check out the repository and find all production projects under `src/` and the test project under `tests/`, with the solution and every cross-project reference pointing at the new paths. **Independent proof:** `dotnet build LamuFlix.sln` exits 0 with no new warnings; `dotnet test` passes with the same test count as the base; `git diff --find-renames=100% 450e70f...HEAD --stat` lists every moved `.cs` file as a pure rename (no content diff outside S5 and S6); and `Test-Path` returns False for all four old project folder locations.

### User Story 2 — Stale folder name corrected (Priority: P1)

As a developer, the folder for the Worker project is `src/LamuFlix.Worker/`, matching the csproj and namespaces that are already `LamuFlix.Worker`. **Independent proof:** `Test-Path src/LamuFlix.Worker` is True; `Test-Path LamuFlix.Work` is False; the rename `git diff --find-renames=100%` lists files under `LamuFlix.Work/` as renamed to `src/LamuFlix.Worker/`.

### User Story 3 — Legacy assets removed (Priority: P2)

As a developer, `SetupWorker/`, `LamuFlix.WorkerSetup/`, and `LamuFlix.Data/Models/Temp.cs` are gone and the build remains green. **Independent proof:** `Test-Path` returns False for all three locations; a zero-reference check for `Temp` in `.cs` source outside `Temp.cs` itself returns no matches; `dotnet build LamuFlix.sln` exits 0.

### Edge Cases

- `LamuFlix.Web.old/` is absent at `450e70f` and from all reachable history (D1). The acceptance check verifies `Test-Path` is False. No delete commit is made.
- All 8 `LamuFlix.Web/wwwroot/lib` folders are used by live views. None are deleted (Q1 no-op).
- No `.cs` content changes, no namespace edits, no Web→Api or Data→Infrastructure rename (Q2). Any edit beyond a path string stops and goes to Patron.
- `Temp.cs` has zero production references: no other `.cs` file, no `DbSet<Temp>`, no EF snapshot entity (D3). The deletion is a `git rm` at its current path, before the S2 move, so the file is never moved and then deleted.
- The path-string edits in S5 and S6 touch only the four files named in the brief at `450e70f` and no other file (D4). If any other file is found to need a path edit, stop and report to Patron.
- Pre-existing analyzer findings are carried forward as follow-ups (DEV-281, DEV-366). They are not fixed or suppressed. A new finding that only a `.cs` content edit could clear is `blocked: structural` to Patron.

## Requirements

### Functional Requirements

- **FR-001**: Move `LamuFlix.Web/` to `src/LamuFlix.Web/` using `git mv`, keeping folder and project names (S1, Q2).
- **FR-002**: Move `LamuFlix.Data/` to `src/LamuFlix.Data/` using `git mv`, keeping folder and project names (S2, Q2).
- **FR-003**: Move `LamuFlix.Work/` to `src/LamuFlix.Worker/` using `git mv`, correcting the stale folder name. The csproj and all namespaces are already `LamuFlix.Worker`; no content change is made (S3, recon §Solution).
- **FR-004**: Move `LamuFlix.Test/` to `tests/LamuFlix.Test/` using `git mv`, keeping folder and project names (S4, Q2).
- **FR-005**: Update `LamuFlix.sln` to change the path strings of its four project entries to the new locations. Only path strings are changed; no other content is touched (S5, D4).
- **FR-006**: Update the three `ProjectReference` path strings in LamuFlix.Test.csproj to the new locations. Only path strings are changed; no other content is touched (S6, D4). The plan-time recon addendum lists the exact lines. Web.csproj:19 and Worker.csproj:18 still resolve and are not edited.
- **FR-007**: Delete `SetupWorker/` (`SetupWorker.vdproj`) with `git rm` (S7).
- **FR-008**: Delete `LamuFlix.WorkerSetup/` (`LamuFlix.WorkerSetup.vdproj`) with `git rm` (S8).
- **FR-009**: Delete `LamuFlix.Data/Models/Temp.cs` with `git rm` at its pre-move path. Nothing references the file (D3). This deletion precedes the S2 move (S9).
- **FR-010**: Write `docs/adr/0013-src-tests-solution-layout.md`, citing plan §3 *Target Solution Layout* and §11 Epic 1 item 2 (S12, Q3, D5).
- **FR-011**: Run pipeline gates (Roslyn analyzers, cyclomatic complexity at 15 and at `-Threshold 6`, JetBrains InspectCode) and report numeric exits. The pass bar is no finding missing from the `450e70f` `-Files` baseline (D2). A skipped or unrunnable gate is not a pass.

## Success Criteria

- **SC-001**: `dotnet build LamuFlix.sln` exits 0 with 0 warnings / 0 errors (the `450e70f` base per recon Baseline gates is 0 warnings).
- **SC-002**: `dotnet test` passes; the test count matches the `450e70f` base (18 passed / 4 skipped).
- **SC-003**: `git diff -M100% --name-status 450e70f...HEAD` shows every moved `.cs` file as R100 (pure rename). The only other entries are the 3 deletions (S7–S9), sln as M, LamuFlix.Test.csproj as D+A pair, and additions `docs/adr/0013` + `specs/DEV-290/`. (Use `--stat` as a supplement for byte count.)
- **SC-004**: `git grep -n -w Temp -- '*.cs'` returns no matches after deletion (baseline: `LamuFlix.Data/Models/Temp.cs:5` before deletion; exit 1 after).
- **SC-005**: `Test-Path` returns False for `LamuFlix.Web.old`, `SetupWorker`, `LamuFlix.WorkerSetup`, `LamuFlix.Work`, `LamuFlix.Web` (root), `LamuFlix.Data` (root), `LamuFlix.Test` (root).
- **SC-006**: `dotnet format --verify-no-changes` exits 0.
- **SC-007**: Pipeline gates report numeric exits; no new finding appears that was absent from the `450e70f` baseline on the same file set at its old paths (D2). Pre-existing findings are listed as follow-ups on the PR.
- **SC-008**: `docs/adr/0013-src-tests-solution-layout.md` exists and cites plan §3 and §11 Epic 1 item 2 (FR-010).

## Gate 1 and authorization envelope

- No owner checkbox is required. The src/tests moves and the deletions are named in the ticket. Q1 (no-op) and D1 (void by drift) remove work without adding or reordering any. No §2.3 item is open.
- Nothing else is authorized: no `.cs` content edit, namespace rename, Web→Api rename, Data→Infrastructure rename, new dependency, schema change, public API change, or out-of-ticket file rewrite. Any such need is a structural owner question.
- Acceptance tests (Gherkin/Reqnroll) are opted out (Q4).
