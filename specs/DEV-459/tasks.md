# Tasks: Migrate stored movie folders to library-relative paths

**Input**: `specs/DEV-459/` spec.md, plan.md, grill-closed brief.md (A1–A10), CONCLUSIONS.md, ASSUMPTIONS.md, recon-DEV-459, recon-DEV-459-2, recon-DEV-459-3, note DEV-459, proposed ADR 0035, specs/PRODUCT.md

**Status**: Draft pending ordinary owner Gate 1 merge receipt (T000) before implementation. Mechanism/placement/tooling closed by Patron A8–A10. No open Quill needs-decision or needs-recon. Quill does not invent freeze.

**Gate 1**: No owner structural checkbox (brief A7/A9/A10; PRODUCT.md §5). Handoff: **Patron provisional Gate 1 → Rigger opens the spec PR → user merges Gate 1 → Keel T000** records the verified merge receipt in `specs/DEV-459/CONCLUSIONS.md`.

**Tests**: Required (ticket Scope; brief Acceptance matrix; FR-019). Prefer new-behaviour regressions red before production edits where feasible. Never mock DB driver translation, uniqueness, or transaction/locking. Existing xUnit v3 / Shouldly / NSubstitute / AutoFixture / Faker.Net / FsCheck / Testcontainers / MockFileSystem only.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared build outputs (gates) are never `[P]`.

**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no explanatory code comments; never hand-edit generated EF snapshots; never suppress pending-model-changes; never manually delete in-repo generated artifacts; never lower harness thresholds; TimeProvider when time is needed; literals used in static contexts are `const`. Drafting does not run `dotnet ef` or embed real connection values.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement Phase N` dispatch. They are **not** canonical `task-pipeline` phases.

**Mechanism (A8 + A9 + A10)**: Option C — `movies.library_path_key` = whole-string `string.ToUpperInvariant()` (never per-char), non-null after activation, binary unique `IX_movies_library_path_key`, retain `IX_movies_library_path`. Trusted-writer derivation only via Infrastructure `LamuFlixDbContext` shared SaveChanges choke point (sync+async); DB enforces uniqueness/concurrency. A10 two-scaffold composition: ship sole additive `AddLibraryPathKey`; Phase B external `LibraryPathKeyActivation` scratch rewrites in-repo snapshot to final (required + property `UseCollation("C")` + unique index); gate-owned activation DDL sets column collation `C` + SET NOT NULL + unique index; Migrate → gate → Run; flag-off verify-only fail-closed; F8 successor-remove recovery documented. Never invent `lower()`/`citext`/generic-collation equivalence.

---

## Local phase dependency / execution order

```text
Phase 0   T000 (Keel Gate 1 record) — blocks T001+
Phase 1   T001 (Wisp drift recon; Keel reanalysis; Gauge baseline) — blocks T002+
Phase 2   T002 → T005   (Anvil; US1 canonical path + options + Unicode equivalence property)
          STOP before T006+ on property mismatch (Wisp recon → Patron → artifact revision; never waive)
Phase 3   T006 → T011   (Anvil; US2 Option C uniqueness + A10 two-scaffold + atomic activation + Program.cs gate) — blocks US3/US4
Phase 4   T012 → T017   (Anvil; US3 import/scanner/rescan/playback + both-index 23505 + Path assertion)
Phase 5   T018 → T021   (Anvil; US4 thumbnail queue transition)
Phase 6   T022 → T023   (Anvil; US5 ops docs + acceptance matrix incl. Operations + actual SMOKE-001)
Phase 7   T024          (Cog /refactor once)
Phase 8   T025 → T029   (Gauge gates — sequential)
Phase 9   T030          (canonical Phase 4 Size L review)
Phase 10  T031 → T032   (canonical Phase 5 delivery)
```

---

## Phase 0: Gate 1 record (blocks Phase 1)

**Purpose**: Confirm user Gate 1 merge before any Build work.
**Checkpoint**: Verified merge receipt in CONCLUSIONS.md; or stop.

- [ ] T000 **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-459 in `specs/DEV-459/CONCLUSIONS.md`, including that Patron A8 Option C, A9 placement/sequencing, and A10 two-scaffold tooling/F8 recovery (and related composition/activation/duplicate rulings) are the recorded mechanism — receipt only. T001–T032 MUST NOT proceed until this receipt exists (brief A7–A10; FR-019).

---

## Phase 1: Pickup — fresh recon, drift reanalysis, Gauge baseline (blocks implementation)

**Purpose**: Confirm Gate-1-merged brief/spec/plan/tasks still match landed main; confirm Option C persistence/write surfaces, A10 migration artifacts, and `Program.cs` barrier wiring for drift only (composition path already named by recon-DEV-459-2 §9.8 / A8; tooling composition closed by A10 / recon-DEV-459-3 §10).
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift/decision stops — no invention. Baseline captured.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main for recon-bounded surfaces (`LibraryPath`, `LibraryOptions`, import handlers/endpoints, discovery/scanner, `MovieRecord` / Movie persistence/configuration/converter/`LamuFlixDbContext`/`EfMovieRepository`/`EfMovieCatalog`, tooling migration folder including shipped `AddLibraryPathKey` + final snapshot, EnrichmentConsumer / ThumbnailConsumer, `src/LamuFlix.Api/Program.cs` barrier slot after Build before Run covering HTTP + hosted consumers, named test suites in recon-DEV-459 §5, config examples, operating docs, ADR 0035, CONTEXT.md). Confirm drift only on already-named composition (`Program.cs`) and Option C write surfaces — do not invent a Worker project or re-open the closed composition-file gap (brief A8–A10; plan Project Structure; recon-DEV-459-2 §9.8; recon-DEV-459-3 §10). **Keel** runs cross-artifact reanalysis of brief/spec/plan/tasks against landed main and confirms A8–A10 remain recorded. Amend tasks only where authorized by demonstrated drift. **STOP** on unresolved drift or missing contract facts. **Gauge** records pre-edit baseline. No production edits in this task (brief A7; plan Phase B).

---

## Phase 2: Canonical path rules and LegacyRootPaths (Anvil; Priority: P1 — User Story 1)

**Goal**: Shared canonical LibraryPath rules (pure string parsing), validated `Library:LegacyRootPaths` via named validator, and shipping-runtime Unicode equivalence property gate for Option C.
**Independent Test**: Domain/unit/property coverage for canonicalization, options validation, and OrdinalIgnoreCase↔ToUpperInvariant equivalence green without HTTP/queue.
**Order**: T002 → T005. **Hard stop**: T005 property mismatch blocks T006+ (never waive).

- [ ] T002 [P] [US1] Add failing unit/property coverage for Canonical LibraryPath rules (separators, casing preservation, redundant `.` removal, root as `.`, reject `..` / drive-relative / outside-root / malformed, idempotent relative normalization, current-root absolute → relative) including both Windows and POSIX foreign-OS fixture sets runnable on one host, in existing domain suites under `tests/LamuFlix.UnitTests/Domain/` (extend `LibraryPathSurvivorTests.cs` and related; exact additional files named by T001) (FR-001; A1, A5, A9/M4; SC-001)
- [ ] T003 [US1] Extend `src/LamuFlix.Core/Domain/LibraryPath.cs` (and import-facing helpers only as needed) so persisted/returned identity matches Canonical LibraryPath rules using **pure string parsing only** — no `System.IO.Path` in Core; keep DEV-438 vocabulary; no new port; Core remains key-free (FR-001, FR-003, FR-012; A1, A5, A9/M4)
- [ ] T004 [US1] Add optional `LegacyRootPaths` (`string[]` / `IReadOnlyList<string>`) to `src/LamuFlix.Core/Options/LibraryOptions.cs` retaining data annotations; add `src/LamuFlix.Core/Options/LibraryOptionsValidator.cs` as `IValidateOptions<LibraryOptions>` beside existing Enrichment/Thumbnail validators; register as singleton `IValidateOptions<LibraryOptions>` in `AddLamuFlixOptions` (`src/LamuFlix.ServiceDefaults/Extensions.cs`) while retaining `ValidateDataAnnotations()` + `ValidateOnStart()`; wire shared Api configuration examples including `docker/api.settings.example.json`; document automatic `/media/library` recognition for backfill/messages only when unambiguous (FR-004, FR-018; A2, A6, A9/L1; ASSUMPTIONS)
- [ ] T005 [US1] Make T002 green; add focused options-validation unit coverage for malformed/non-absolute alias entries and conflict detection via `LibraryOptionsValidator`; add shipping-runtime FsCheck (or equivalent existing property) coverage that `a.Equals(b, OrdinalIgnoreCase)` equals `(a.ToUpperInvariant() == b.ToUpperInvariant())` across Unicode including supplementary/Deseret, Turkish dotted/dotless i, sharp-s, ligatures, and combining forms — samples are evidence not proof; any mismatch **blocks acceptance, stops before T006+, and returns to Patron** after Bernstein routes Wisp counterexample/runtime recon and brief/CONCLUSIONS/ADR/spec/plan/tasks are revised — never waive the gate; do not write expected sharp-s/ligature divergence claims (recon-DEV-459-2 §9.2 contradicts that premise) (FR-004, FR-018, FR-021; A6, A8–A9)

---

## Phase 3: Option C uniqueness and atomic startup activation (Anvil; Priority: P1 — User Story 2)

**Goal**: Movies-only Option C race-safe uniqueness, sealed gate with ACCESS EXCLUSIVE + single-transaction activation, DbContext SaveChanges choke point, single Api-host barrier with Migrate→gate→Run.
**Independent Test**: Real PostgreSQL/Testcontainers proofs for preflight-before-CI-index, collisions, rollback after actual writes/activation failure, interruption/restart, concurrent same-host serialization, ID/pre-existing-column preservation with intentional key population, both indexes with collation `C`, fresh/legacy/flag-off/post-gate/default-alias/ordering/missing-key/drift/post-activation no-write.
**Order**: T006 → T011 sequential on shared persistence/startup surfaces. **Prerequisite**: T005 property green (or Patron-revised mechanism after mismatch stop).

- [ ] T006 [US2] Add failing real-Postgres Testcontainers coverage for portable uniqueness (exact + case-only + non-ASCII per A8 Option C) and concurrent duplicate import race under `tests/LamuFlix.IntegrationTests/` (extend MigrationTests / import persistence suites named by T001); cover `23505` from both `IX_movies_library_path` and `IX_movies_library_path_key`; assert effective **column** collation `C` on `library_path_key` on real PostgreSQL (not an index-level API claim); never mock EF driver (FR-008, FR-010, FR-022; A3, A5, A8–A10; SC-002–SC-004; recon-DEV-459-3 F9)
- [ ] T007 [US2] Apply Patron A10 / recon-DEV-459-3 §10 F5 two-scaffold composition exactly (do not invent alternatives; do not embed real connection values in docs; Build runs tooling — drafting does not): (1) interim source model — `MovieRecord` `string? LibraryPathKey`, `MovieConfiguration` text column `IsRequired(false)`, no index; from worktree root set non-blank design-time env placeholder (never commit), `dotnet tool restore`, then `dotnet ef migrations add AddLibraryPathKey --project src/LamuFlix.Infrastructure --startup-project src/LamuFlix.Infrastructure --output-dir Persistence/Migrations`; (2) change source to final — required key, property-level `UseCollation("C")` on `LibraryPathKey`, unique `IX_movies_library_path_key`; `dotnet ef migrations add LibraryPathKeyActivation --project src/LamuFlix.Infrastructure --startup-project src/LamuFlix.Infrastructure --output-dir <ABSOLUTE TEMP DIRECTORY OUTSIDE THE WORKTREE>`; tooling rewrites existing in-repo model snapshot to final; only additive `AddLibraryPathKey` ships; external activation migration+designer are disposable scratch (never commit/archive/hand-edit; no in-repo hand-edit/suppression/`ConfigureWarnings`/manual generated-file deletion). Acceptance: `dotnet ef migrations has-pending-model-changes ...` exit 0; live PG17 Migrate applies only additive migration into schema-not-ready; an unrelated later scaffold emits no activation AlterColumn/CreateIndex. Own F8 recovery: if `migrations remove` runs on the immediate successor of `AddLibraryPathKey`, redo Phase B final-model external scaffold, re-check pending exit 0, inspect next scaffold for no activation before apply. Never `lower()`/`citext`/generic collation; no concurrent index build (FR-007, FR-010, FR-020; A3, A5, A8–A10; recon-DEV-459-3 F5–F10)
- [ ] T008 [US2] Implement sealed `src/LamuFlix.Infrastructure/Persistence/MovieLibraryPathMigrationGate.cs` (no interface; no Core port) with: resolve validated `LibraryOptions` first (validation failure aborts before any DB write); open one DbContext transaction; first SQL `LOCK TABLE movies IN ACCESS EXCLUSIVE MODE`; configuration-aware whole-table preflight (actionable duplicate/movie-ID diagnostics) **before** CI unique-index creation; atomic path backfill + whole-string `ToUpperInvariant` key population + activation DDL that **explicitly sets `library_path_key` column collation to `C`** together with SET NOT NULL and non-concurrent CREATE UNIQUE INDEX (merely relying on index comparison is insufficient) + consistency verification inside that single transaction; preserve IDs and pre-existing business columns while intentionally writing `library_path_key`; fail closed for unmapped/malformed/escaping/ambiguous/colliding rows; **own shared automatic `/media/library` default Docker root-map construction and ambiguity/conflict policy** (active root + `LegacyRootPaths` + unambiguous default alias); add ArchitectureTests assertion that this gate introduces no new Core port (FR-004–FR-010; A2–A3, A8–A10; ASSUMPTIONS; recon-DEV-459-2 §9.4–9.6; recon-DEV-459-3 F9)
- [ ] T009 [US2] Wire startup in `src/LamuFlix.Api/Program.cs` after `Build` and before `Run` with fixed order: additive `Migrate` (when `DatabaseOptions.ApplyMigrationsOnStartup` is true) → await `MovieLibraryPathMigrationGate` → `Run`, so neither HTTP listeners nor hosted RabbitMQ consumers process work until the gate succeeds; when ApplyMigrationsOnStartup is false, gate is verify-only and fails closed with schema-not-ready if column/NOT NULL/unique index/column collation `C` is absent (no migration, no activation); cover concurrent instances of this same host; after keys/constraints are valid, already-canonical consistent catalog is an idempotent no-write rerun; before activation, already-canonical paths with missing keys still require key population/activation; startup key-consistency validation fails closed on drift; no Worker project; no new technical interlock beyond named backstops (FR-006, FR-009, FR-010; A3, A8–A10; recon §9.8)
- [ ] T010 [US2] Add real-Postgres PG17 proofs for: `has-pending-model-changes` exit 0 after A10 composition; live additive-only Migrate into schema-not-ready (nullable column, no key index, no column collation); post-gate final column+index schema match including effective column collation `C`; unrelated later scaffold emits no activation ops; offline-media migration; fresh schema; legacy schema; flag-off verify-only/schema-not-ready fail-closed; Migrate→gate→Run ordering on fresh schema; default Docker `/media/library` alias legacy-row success and conflict/ambiguity fail-closed; already-canonical paths with missing keys; rollback after actual writes/activation failure (mid-preflight failure alone does not prove write rollback); interruption/restart safety; concurrent Api-instance gate serialization; ACCESS EXCLUSIVE lock / single-transaction boundary; consistency-drift fail-closed; post-activation no-write rerun; actionable failure diagnostics containing movie IDs (FR-005–FR-009; A2–A3, A8–A10; SC-001–SC-003; recon-DEV-459-3 F5–F7, F9)
- [ ] T011 [US2] Make T006 green against Option C and backfill/activation; green-lock exact unique index retention plus `IX_movies_library_path_key` and **column** collation `C` assertions in `tests/LamuFlix.IntegrationTests/MigrationTests.cs`; implement ONE Infrastructure choke point in `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs` — override sync and async `SaveChanges` through one shared private derivation pass over tracked `MovieRecord` Added/Modified entries setting `LibraryPathKey` from whole-string `LibraryPath.Value.ToUpperInvariant()`; no derivation in `EfMovieRepository`, `LibraryPathConverter`, or Core; tests must mutate the path through **every** supported writer and assert the key follows; bypass writers prohibited (FR-007, FR-010, FR-022; A9/H1; A10/F9; recon §5, §9.5)

---

## Phase 4: Import, discovery, scanner, rescan, playback regression (Anvil; Priority: P1 — User Story 3)

**Goal**: Canonical import/discovery/scanner/rescan flows; key follows via DbContext choke point; both-index duplicate outcomes unchanged; automated relative Path assertion; reject legacy-alias imports; playback scanner regression without LocalPlay/launcher edits.
**Independent Test**: Import persistence/API (including endpoint behaviour for both constraints), discovery/scanner, rescan, details Path relative-value, and playback scanner suites green.
**Order**: T012 → T017.

- [ ] T012 [P] [US3] Update failing/extend unit coverage in `tests/LamuFlix.UnitTests/FileSystem/DirectoryMediaLibraryScannerTests.cs` and discovery suites so relative folders resolve under `Library:RootPath` and stored/returned scan identity expectations match Canonical LibraryPath (replace absolute-stored assumptions where this ticket flips them); retain existing `MockFileSystem` conventions (FR-001, FR-012; A5; recon §5)
- [ ] T013 [US3] Update `src/LamuFlix.Infrastructure/FileSystem/DirectoryMediaLibraryScanner.cs` and `src/LamuFlix.Infrastructure/FileSystem/FileSystemLibraryFolderDiscovery.cs` so runtime resolution uses active root (never CWD) and discovery/import candidates align with canonical relative identity, including root represented as `.`; filesystem resolution stays in Infrastructure only (FR-012; A5, A9/M4; recon §3)
- [ ] T014 [US3] Update import path handling in `src/LamuFlix.Core/Features/Import/ImportMovieFolderCommandHandler.cs` (and bulk/scoped importer path as needed) plus `src/LamuFlix.Api/Endpoints/ImportEndpoints.cs` so relative and current-root absolute inputs canonicalize before uniqueness/persistence; rely on `LamuFlixDbContext` SaveChanges choke point for `library_path_key` (no per-writer derivation in Core/handlers); reject legacy-alias and outside-root imports; keep route/DTO/status shape unchanged; do **not** introduce a 409 mapping from advertised `ProducesProblem` metadata (FR-002, FR-003, FR-011, FR-022; A1, A5, A8–A9; recon §9.5)
- [ ] T015 [US3] Extend import persistence/API tests under `tests/LamuFlix.UnitTests/Features/Import/` and `tests/LamuFlix.IntegrationTests/` (suites named in recon §5) for relative/current-root absolute success with key derivation via choke point, legacy/outside-root rejection, and race-safe duplicate/case-only outcomes from **both** `IX_movies_library_path` and `IX_movies_library_path_key` (`23505`) through the existing duplicate-import path, preserving winner ID under sequential and concurrent single/bulk imports; include endpoint behaviour assertions for both constraints, not only database exception tests; add automated assertion that returned `MovieDetailsResponse.Path` is the canonical relative value (distinct from SMOKE-001 runtime execution) (FR-002, FR-003, FR-010, FR-011, FR-022; SC-004; A8–A9)
- [ ] T016 [US3] Confirm `src/LamuFlix.Core/Features/Library/RescanMovieOperation.cs` resolves relative `movie.Path` under active root without mutating stored identity; extend rescan unit/integration suites named in recon §5; include/confirm automated `MovieDetailsResponse.Path` relative-value assertion coverage for details/rescan paths where those suites already exercise details responses (FR-002, FR-013; A5)
- [ ] T017 [US3] Add/extend playback scanner regression coverage in existing playback suites named in recon §5 proving relative stored paths resolve under active root with divergent process CWD; do **not** modify `Features:LocalPlay` or `src/LamuFlix.Infrastructure/Playback/ProcessMediaPlayerLauncher.cs` (FR-014; A5; SC-005 family shared runtime)

---

## Phase 5: Queued thumbnail transition (Anvil; Priority: P1 — User Story 4)

**Goal**: Relative publish; legacy absolute canonicalize; OrdinalIgnoreCase identity check; root-based authorization; existing terminal paths.
**Independent Test**: Enrichment/thumbnail consumer suites cover new relative + legacy absolute + mismatch/terminal cases.
**Order**: T018 → T021.

- [ ] T018 [P] [US4] Add failing coverage in `tests/LamuFlix.IntegrationTests/ThumbnailConsumerTests.cs` / `EnrichmentConsumerTests.cs` and unit suites named in recon §5 for relative publish, legacy Windows/Docker absolute canonicalize+match, MovieId-only mismatch refusal, unknown/escaping/missing terminal paths, and CWD≠root resolution (FR-015–FR-017; A4; SC-005)
- [ ] T019 [US4] Update `src/LamuFlix.Infrastructure/RabbitMq/EnrichmentConsumer.cs` so new `ThumbnailRequested` publishes canonical relative `movie.Path.Value` without changing message schema or queue topology (FR-015; A4)
- [ ] T020 [US4] Update `src/LamuFlix.Infrastructure/RabbitMq/ThumbnailConsumer.cs` to canonicalize absolute payloads through active root + `LegacyRootPaths` (+ unambiguous `/media/library`), compare with `OrdinalIgnoreCase` to loaded movie path, never authorize on ID alone, resolve under `Library:RootPath` (not CWD), preserve containment/reparse/eligibility, and keep existing terminal `IneligibleRequest` / `UnauthorizedSourcePath` behaviour (FR-016, FR-017; A4)
- [ ] T021 [US4] Make T018 green; retain alias configuration through outstanding deliveries/retries/DLQ in docs cross-check with T022 (FR-018; A6)

---

## Phase 6: Operations docs and acceptance matrix lock (Anvil; Priority: P2 — User Story 5)

**Goal**: Operator docs for config/hard-stop rollout/rollback/alias retirement; acceptance matrix evidence complete for all eight families; **actual** SMOKE-001 Compose execution with recorded status.
**Independent Test**: Doc review against A6/A9; SMOKE-001 executed (or Could not run); matrix rows mapped to green suites.

- [ ] T022 [US5] Update operating/configuration documentation (existing README/AppHost/docker docs surfaces named by T001 — do not invent a new admin UI) covering shared `Library:RootPath` / `Library:LegacyRootPaths`, default Docker alias conflicts, **hard stop of all old Api instances** before upgrade, verify matching application version + runtime/globalization/ICU + roots/aliases, pre-upgrade backup, no mixed-version writers, backup-restore downgrade, preflight diagnostics, alias retirement after queue drain, and **F8 immediate-successor `migrations remove` recovery** (redo Phase B final-model external scaffold under absolute TEMP outside worktree; `has-pending-model-changes` exit 0; inspect next scaffold so no activation AlterColumn/CreateIndex re-emits before applying; no `ConfigureWarnings` suppression; no manual deletion of in-repo generated artifacts); document named existing backstops (post-activation NOT NULL rejects old-writer inserts; startup consistency check fails closed on drift) and explicitly state **no new pre-activation technical interlock** is required; do not put real connection values in docs (FR-018; A6, A9/M3, A10/F8; SC-007; recon-DEV-459-3 F8)
- [ ] T023 [US5] Lock the brief acceptance matrix to green automated evidence for **all eight** families (cross-platform identity, safe preflight, atomicity/startup, persistence invariants, import/rescan, queue transition, shared runtime resolution, **Operations via T022**) **and actually execute** SMOKE-001 Compose movie-details display/copy verification with runtime evidence and the exact resulting status (PASS with observed relative path, or **Could not run** as unresolved acceptance if unavailable — never treat planned criteria alone as observed acceptance); no frontend code (FR-019; A7–A9; SC-006–SC-007)

---

## Phase 7: Refactor (Cog)

- [ ] T024 After T023: **Cog** runs `/refactor` once over completed C# implementation for this ticket; fix complexity/duplication/property gaps without expanding scope or lowering thresholds (brief task order 6; harness refactor gate).

---

## Phase 8: Verification gates (Gauge) — sequential

- [ ] T025 **Gauge** runs changed-C# Roslyn, cyclomatic (build then refactor threshold from `harness.yml`), and InspectCode scripts for DEV-459 files (FR-019; plan Gates). Sequential with T026–T028 — shared build outputs; not parallel.
- [ ] T026 **Gauge** runs affected suites then full `dotnet test` (FR-019). After T025; not parallel.
- [ ] T027 **Gauge** runs property / vulnerable-packages / `dotnet format --verify-no-changes` per harness; report SKIPPED/SKIP/N/A/Could not run honestly (FR-019; A7). After T026; not parallel.
- [ ] T028 **Gauge** runs `pwsh -NoProfile -File ./scripts/run-mutation.ps1` for eligible changed production C# (no `--since` from worktree) (FR-019; A7). After T027; sequential.
- [ ] T029 **Gauge** reports web/contract gate applicability only (no web implementation planned — do not manufacture PASS). Gauge does **not** edit CONTEXT.md. Glossary append of terms authorized by spec Domain vocabulary is owned by **builder/Quill** at delivery (paired with T031/T032), not Gauge (FR-019; plan Gates; A9/L3)

---

## Phase 9: Size L review (canonical Phase 4)

- [ ] T030 **Sentry / Ledger / Compass** three-axis review under brief closing bar (zero open in-scope Critical/High/Medium/Low; max three remediation rounds). Unresolved findings return to Bernstein — do not silently extend or clear (A1, A7).

---

## Phase 10: Delivery (canonical Phase 5)

- [ ] T031 Rebase onto latest default branch; re-run required gates on rebased HEAD; open/update PR with merge-bar proof; **builder/Quill** appends CONTEXT.md glossary terms authorized by spec Domain vocabulary if not already landed (user merges; never auto-merge) (A7; PRODUCT §2; A9/L3)
- [ ] T032 After T000 user Gate 1 merge receipt exists in `CONCLUSIONS.md`, **Keel** (or the seat the Gate 1 process names) flips proposed ADR 0035 from `proposed` to `accepted` as the delivery status update — without treating Patron provisional approval or draft plan text as that flip; Quill does not Accept in Phase A; no CHANGELOG (A7; Quill duties; A9/L3)

---

## Dependencies & Execution Order

- Phase 0 blocks all Build.
- Phase 1 blocks production edits; composition path and A10 tooling already named — drift checks only.
- Phase 2 (US1) before Phase 3; Unicode property mismatch stops before T006+.
- Phase 3 (US2) before Phases 4–5 integration that assumes canonical stored values, keys, and barriers. T006–T011 share persistence files — sequential. T007 executes A10 F5 sequence at Build.
- Phase 4 (US3) and Phase 5 (US4) proceed sequentially as ordered; do not start US4 publish/consume assumptions until US2 gate exists on the Api host.
- Phase 6 docs/matrix/smoke execution after functional suites exist; T023 includes Operations/T022 (including F8 recovery docs).
- Phases 7–10 follow task-pipeline Build → refactor → sequential gates → review → ship.

### Parallel opportunities

- T002 with early options test scaffolding after T001 (different files).
- T012 scanner test updates can draft in parallel with T014 import work after T011 (different files).
- T018 consumer tests can draft in parallel with T019/T020 after T011 (different files).
- **Not parallel**: T006 with T007/T010/T011 (shared integration persistence files). T025–T028 (shared build outputs). Gates remain sequential.

### MVP

US1 + US2 (canonical rules, options, Option C uniqueness, A10 two-scaffold, atomic activation, Program.cs gate) is the minimum operable migration foundation; US3–US4 are required for ticket Scope completeness before ship.

## Notes

- `[P]` tasks = different files, no incomplete deps, no shared build outputs.
- Host composition is `src/LamuFlix.Api/Program.cs` (A8; recon-DEV-459-2 §9.8). Unspecified helper filenames still come from T001 drift recon — do not invent.
- Portable uniqueness mechanism is Patron A8 Option C — Quill never substitutes `lower()`/`citext`/generic collation.
- Key derivation choke point is `LamuFlixDbContext` (A9) — never per-writer / converter / Core.
- EF tooling composition is Patron A10 / recon-DEV-459-3 §10 F5 — Quill does not invent alternatives; Build runs the commands; drafting does not.
- Local Phase N ≠ canonical task-pipeline phase numbers.
