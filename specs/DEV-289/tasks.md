---
description: "Task list for DEV-289 — build-configuration hardening (CPM, strict compilation, banned ambient time)"
---

# Tasks: DEV-289 — Build-configuration hardening (CPM, strict compilation, banned ambient time)

**Input**: `specs/DEV-289/spec.md`, `specs/DEV-289/plan.md`

**Prerequisites**: `plan.md` (required), `spec.md` (required for user stories)

**Tests**: No tests added. Acceptance tests (Gherkin/Reqnroll) are **opted OUT** — this is a build-tooling
ticket; the verifiable outcomes are build results and analyzer diagnostics, not runtime behaviour.

**Phase A note**: This spec PR contains **no implementation**. The tasks below execute at the implement
stage, in a build worktree off this branch. **Nothing here is committed or pushed by the spec PR.**

**Gate note (B1 / B4)**: T002 is a **hard prerequisite**. Tasks marked **[BLOCKED — Scope item 6]** must
not be started, and the enforcement tasks (T011, T012, T015, T016, T021) must not be started, pushed, or
merged, until the owner's DEV-289 Scope item 6 amendment is live in YouTrack. Until then the 35 source
files (34 `.cs` + 1 Razor view, counted precisely in T018–T020) are **not authorized** and the branch must
not be merged into a red tree (`CONCLUSIONS.md` B1, B4).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependencies)
- **[Story]**: US1 (version management), US2 (banned time symbols), US3 (strict compilation)
- Exact file paths are given in each task.

## Phase 1: Setup

**Purpose**: Capture the baseline the acceptance criteria are measured against.

- [ ] T001 From the worktree root, capture the baseline as **two separate, non-mutating** measurements
  against the current tree:
  (a) **build counts** — `dotnet build LamuFlix.sln` and
  `dotnet build LamuFlix.sln -p:TreatWarningsAsErrors=true -p:Nullable=enable`; record the two
  error/warning counts (expected 168 errors in 33 files — B1); and
  (b) **banned-symbol scan** — a separate source scan (e.g.
  `rg -n "DateTime\.(Now|UtcNow|Today)|DateTimeOffset\.(Now|UtcNow)" -g "*.cs" -g "*.cshtml"`) that
  enumerates the three live hits `LamuFlix.Web/Controllers/FilmesController.cs:28` and
  `LamuFlix.Work/Worker.cs:37,57`.
  The scan is independent of the builds and its result is reported **separately**; the three hits cannot be
  attributed to those build commands because `BannedApiAnalyzers` is not wired until T012 (M2).

---

## Phase 2: Foundational (HARD prerequisite — blocks all enforcement and remediation)

**⚠️ CRITICAL**: T002 is a **hard prerequisite** for every task that turns enforcement on or depends on it
— T011, T012, T015, T016, T021, and the remediation tasks T013/T014/T018–T020 — and it blocks push/merge of
the branch. Until the owner's Scope item 6 amendment is live in YouTrack, the configuration MUST NOT be
merged into a red tree; enforcement cannot land ahead of the remediation that makes it green (Sentry High,
`CONCLUSIONS.md` B1/B4). US1 (version management, T003–T010) is independent of T002.

- [ ] T002 **[OWNER — YouTrack, not a repo edit] — HARD GATE** Paste the proposed Scope item 6 from
  `CONCLUSIONS.md` B1 (which names the 35 source files — 34 `.cs` + 1 Razor view, T018–T020 — and the edit
  envelope) into DEV-289 in YouTrack. This is the gate-1 checkbox; the 35-file tasks and every enforcement
  task below are blocked until it is live.

**Checkpoint**: Scope item 6 is live (T002) before any T011, T012, T013, T014, T015, T016, T018–T020, or
T021 task begins, and before the branch is pushed or merged.

---

## Phase 3: User Story 1 — Package and SDK versions come from one place (Priority: P1) 🎯 MVP

**Goal**: Land `global.json` + `Directory.Packages.props` and strip every `Version` attribute so CPM is on.

**Independent Test**: `dotnet build LamuFlix.sln` succeeds with CPM; `dotnet list package` resolves
versions from `Directory.Packages.props`; no `Version=` remains on any `<PackageReference>`.

- [ ] T003 [US1] Create `global.json` at the repository root with `sdk.version = "10.0.400"`,
  `rollForward: "latestPatch"`, `allowPrerelease: false` — the exact numeric .NET 10 SDK band installed on
  this machine (10.0.201/302/400; `CONCLUSIONS.md` D4(a)). A wildcard such as `10.0.x` is not a valid
  `sdk.version` and is forbidden here (H1). (FR-001).
- [ ] T004 [US1] Create `Directory.Packages.props` enabling
  `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` and centralising every version
  currently declared across the solution — including `Microsoft.CodeAnalysis.BannedApiAnalyzers` pinned to
  the ticket-approved `3.3.4` (Scope item 5; `CONCLUSIONS.md` D4(b)) and today's
  `Microsoft.CodeAnalysis.NetAnalyzers` `9.0.0` — with **no version upgrades** (FR-002, H2).
- [ ] T005 [US1] Remove the 2 `Version` attributes from `LamuFlix.Web/LamuFlix.Web.csproj`
  (Newtonsoft.Json 13.0.3, RabbitMQ.Client 6.8.1) (FR-003).
- [ ] T006 [P] [US1] Remove the 1 `Version` attribute from `LamuFlix.Data/LamuFlix.Data.csproj`
  (Pomelo.EntityFrameworkCore.MySql 9.0.0) (FR-003).
- [ ] T007 [P] [US1] Remove the 5 `Version` attributes from `LamuFlix.Work/LamuFlix.Worker.csproj`
  (FR-003).
- [ ] T008 [P] [US1] Remove the 6 `Version` attributes from `LamuFlix.Test/LamuFlix.Test.csproj`
  (FR-003).
- [ ] T009 [US1] Move the `Microsoft.CodeAnalysis.NetAnalyzers` `Version="9.0.0"` out of
  `Directory.Build.props` `PackageReference` into `Directory.Packages.props`, so no CPM `NU1008` error
  remains (FR-003).
- [ ] T010 [US1] Verify CPM: `dotnet build LamuFlix.sln` succeeds and `dotnet list package` reports the
  centrally-managed versions (SC-004).

**Checkpoint**: version management is central and the solution still builds (before strict settings land).

---

## Phase 4: User Story 2 — Ambient time APIs are a compile error (Priority: P1)

**Goal**: `BannedSymbols.txt` + BannedApiAnalyzers make the `DateTime.Now` family a build failure.

**Independent Test**: a scratch line using `DateTime.Now` fails the build with a diagnostic naming
`System.TimeProvider`; all five time symbols fail; both `Task` bans remain.

- [ ] T011 [US2] Rewrite `BannedSymbols.txt` to exactly seven rows — the five ticket time symbols plus the
  two retained `Task` rows, which MUST be preserved **byte-for-byte, verbatim**. The exact existing rows
  are:
  `M:System.Threading.Tasks.Task.Wait;Sync-over-async deadlocks under a synchronization context. Await the task.`
  and
  `M:System.Threading.Tasks.Task.WaitAll(System.Threading.Tasks.Task[]);Sync-over-async. Use await Task.WhenAll(...).`
  — full documentation-comment IDs; the `...` shown is the message's own text, not the ID, and the rows are
  never rewritten with a literal ellipsis in the ID (M1, C3). Correct every time-symbol
  message to point at `System.TimeProvider`, never at `DateTime.UtcNow` (FR-006, B2).
- [ ] T012 [US2] In `Directory.Build.props`, reference `Microsoft.CodeAnalysis.BannedApiAnalyzers` pinned to
  `3.3.4` (centralised in `Directory.Packages.props`; ticket Scope item 5, `CONCLUSIONS.md` D4(b)) and
  register `BannedSymbols.txt` as an `AdditionalFiles` input, mirroring the existing NetAnalyzers wiring
  (FR-007, H2).
- [ ] T013 [US2] **[BLOCKED — Scope item 6]** In `LamuFlix.Web/Controllers/FilmesController.cs`, inject an
  optional `TimeProvider` (defaulting to `TimeProvider.System`) and replace the call at line 28 with
  `GetUtcNow()`; no other change (FR-009, FR-010).
- [ ] T014 [US2] **[BLOCKED — Scope item 6]** In `LamuFlix.Work/Worker.cs`, inject an optional
  `TimeProvider` (defaulting to `TimeProvider.System`) and replace the calls at lines 37 and 57 with
  `GetUtcNow()`; no other change (FR-009, FR-010).
- [ ] T015 [US2] Verify: add a scratch `.cs` line using `DateTime.Now` (and the four companions), assert
  each fails the build with a BannedApiAnalyzers error naming `System.TimeProvider`, then remove the
  scratch file. Confirm the two `Task` rows are still present **verbatim** (byte-for-byte) (SC-002, SC-006).

**Checkpoint**: a new `DateTime.Now` is a compile error and the build is green.

---

## Phase 5: User Story 3 — Every project compiles strict (Priority: P2)

**Goal**: warnings-as-errors + nullable on across all four projects, with the B1 remediation applied.

**Independent Test**: `dotnet build LamuFlix.sln --no-incremental` exits 0 with 0 errors and 0 warnings;
nullable analysis on.

- [ ] T016 [US3] Edit `Directory.Build.props` to set `TreatWarningsAsErrors=true` and `Nullable=enable`
  (keeping `EnforceCodeStyleInBuild=true`) (FR-004).
- [ ] T017 [US3] Edit `.editorconfig` to standardise repo-wide formatting and naming, including
  file-scoped namespaces (FR-005).
- [ ] T018 [US3] **[BLOCKED — Scope item 6]** Apply the B1 nullable/analyzer remediation to the 16
  `LamuFlix.Data` `.cs` files (incl. `Repositories/GenericRepository.cs`, `Models/*`, migration
  `.Designer.cs`) listed in `brief.md` §Appendix — annotations only, no behaviour change (FR-009).
- [ ] T019 [US3] **[BLOCKED — Scope item 6]** Apply the B1 remediation to the 14 `LamuFlix.Web` **source**
  files — 13 `.cs` plus the Razor view `Views/Filmes/Index.cshtml` (incl. `Models/Filmes/FilmesViewModel.cs`,
  `Services/FilmesServices.cs`, `Extensions/*`, `TagHelpers/*`) (FR-009, C1).
- [ ] T020 [US3] **[BLOCKED — Scope item 6]** Apply the B1 remediation to the 3 `LamuFlix.Test` `.cs` files
  (`ApiDataModel.cs`, `UnitTest1.cs`, `EnrichmentTests.cs`) — annotations/analyzer fixes only (FR-009).
  (T018 16 + T019 14 + T020 3 = 33 nullable/analyzer files; plus T013/T014's 2 banned-symbol files = 35
  source files total: 34 `.cs` + 1 Razor view.)
- [ ] T021 [US3] Verify the strict build on a **non-incremental / fresh checkout**: run
  `dotnet build LamuFlix.sln --no-incremental` (or build from a clean clone) and confirm it exits 0 with 0
  errors and 0 warnings. A warm incremental build is not proof (Sentry High). (SC-001, SC-003).

**Checkpoint**: all projects build under warnings-as-errors and nullable, with zero warnings.

---

## Phase 6: Polish & Cross-Cutting

- [ ] T022 [P] Run `dotnet format LamuFlix.sln --verify-no-changes --include <authorized files>` scoped to
  the solution and the exact **authorized changed set** (the ticket-named files and, once T002 is live, the
  35 source files); do not run it un-scoped over the whole repository (M3). (SC-003 acceptance).
- [ ] T023 [P] Run the three static-analysis gates on the changed set
  (`./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`,
  `./scripts/run-jetbrains-inspectcode.ps1`) and report each exit code as it runs
  (`0` = pass, `1` = fail, `2` = SKIPPED/OPT-OUT). Never report SKIPPED as PASS.
- [ ] T024 Confirm the scope guard (B4): `git diff --name-only` touches only the ticket-named files and,
  once Scope item 6 is live, the 35 enumerated source files (34 `.cs` + 1 Razor view); `brief.md` and
  `CONCLUSIONS.md` are unchanged (SC-005).

> **T025 removed (H3).** The prior T025 amended `CONTEXT.md` and `docs/adr/`, neither of which the ticket
> names; it violated B4 frozen scope / `PRODUCT.md` §5.6 and is deferred to a separately authorized
> follow-up. The `harness.yml` vs MSBuild `TreatWarningsAsErrors` reconciliation is recorded in `spec.md`
> and `plan.md` (documentation within this spec PR), not as a repo edit.

---

## Dependencies & Execution Order

- **Setup (T001)** → **US1 (T003–T010)** is independent of everything else and is the MVP.
- **T002 (owner hard gate)** BLOCKS T011, T012, T013, T014, T015, T016, T018–T020, and T021, and blocks
  push/merge of the branch. It can run in parallel with US1 (T003–T010).
- **US2** config tasks (T011, T012) require T002; T013/T014 (call sites) require T002.
- **US3** config tasks (T016, T017) require T002; T018–T020 require T002 and T016.
- **T015** requires T011–T014 and T016 (the ban must be an error under warnings-as-errors).
- **T021** requires T016–T020. **Polish (T022–T024)** runs last.
- Only T006–T008 and T022/T023 are safely `[P]` (disjoint files).

## Implementation Strategy

1. Baseline (T001) — builds and the separate banned-symbol scan, reported independently.
2. MVP = US1: land `global.json` + `Directory.Packages.props` + `Version` removals; confirm CPM build.
3. Owner pastes Scope item 6 (T002) — the hard gate that unlocks enforcement (T011/T012/T016) and the
   35-file remediation. Enforcement MUST NOT be merged before this.
4. US2: banned symbols + the three call-site fixes; prove the compile error (T015).
5. US3: strict settings + the 33-file nullable remediation; green strict build, non-incremental (T021).
6. Polish: formatter (scoped), three gates, scope guard (T022–T024).
7. Commit with the `DEV-289 - ` prefix once per logical group, then run the review loop against the frozen
   scope and the Critical/High-with-failure bar (B3–B5).
