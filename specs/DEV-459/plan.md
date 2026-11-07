# Implementation Plan: Migrate stored movie folders to library-relative paths

**Branch**: `feature/459-spec` | **Date**: 2026-11-07 | **Spec**: `specs/DEV-459/spec.md`

**Input**: Grill-closed `brief.md` (A1–A10); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; proposed `docs/adr/0035-library-relative-movie-paths.md`; Maestri notes `DEV-459`, `recon-DEV-459`, `recon-DEV-459-2`, `recon-DEV-459-3`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (gates cite only; Quill does not edit constitution or Accept ADR 0035).

**Status**: Draft pending ordinary owner Gate 1. No implementation clearance. Mechanism, placement, and EF tooling composition are closed by Patron A8–A10; no open Quill needs-decision or needs-recon. Quill does not invent freeze.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — behaviour is ruled in the brief including A8 Option C, A9 placement/sequencing, and A10 two-scaffold tooling; public API shape is unchanged (value semantics only). Quill does not invent alternatives and does not equate `lower()`/`citext`/generic collation to OrdinalIgnoreCase.

## Summary

Migrate stored movie folder identities from deployment-specific absolute paths to canonical library-relative LibraryPath values, preserve movie IDs and portable uniqueness via Option C (`movies.library_path_key` + `IX_movies_library_path_key`, retaining `IX_movies_library_path`), resolve all runtime reads under configured `Library:RootPath`, and keep import, rescan, and queued thumbnail jobs correct across the migration via a mandatory transactional startup gate in the single Api composition root (`Program.cs` after Build before Run), shared legacy-root aliases, and unchanged public API/queue shapes and duplicate outcomes.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM).

**Primary Dependencies**: Existing pins only (EF Core, ASP.NET Core Minimal APIs, RabbitMQ consumers, System.IO path APIs already in use in Infrastructure). **No new packages** (brief A7; PRODUCT §5 item 1).

**Storage**: Existing Postgres `movies` table; rewrite `library_path` values; add `library_path_key` (whole-string `ToUpperInvariant`, non-null after activation); retain `IX_movies_library_path`; add binary deterministic unique `IX_movies_library_path_key`. **Mechanism = Patron A8 Option C** (trusted-writer derivation only; DB enforces uniqueness/concurrency, does not validate derivation). **A9/A10 snapshot/DDL strategy**: sole shipped tool-generated EF migration is additive only (`AddLibraryPathKey`: nullable `library_path_key`, no unique index, no NOT NULL, no collation); final EF model and in-repo model snapshot declare required key, property-level `UseCollation("C")` on `LibraryPathKey` (no index-level collation API in EF 10.0.14/Npgsql 10.0.3), and unique `IX_movies_library_path_key`; activation DDL is owned and executed by `MovieLibraryPathMigrationGate` inside its transaction and MUST explicitly set column collation `C` together with backfill, SET NOT NULL, and non-concurrent CREATE UNIQUE INDEX; post-gate schema assertion proves column+index match the final model including effective column collation `C`; a Migrate-only database is schema-not-ready (nullable column, no key index, no column collation), never a supported steady state. Ordering: additive `Migrate` → gate → `Run`. When `DatabaseOptions.ApplyMigrationsOnStartup` is false, the gate performs verify-only and fails closed with schema-not-ready if column/NOT NULL/unique index/column collation `C` is absent. Never equate `lower()`/`citext`/generic collation to OrdinalIgnoreCase.

### EF tooling composition (Patron A10; recon-DEV-459-3 §10 F5–F10)

Exact no-hand-edit sequence from worktree root (do **not** put real connection values in docs; do **not** run tooling during drafting):

```powershell
# Phase A — source model interim (string? LibraryPathKey; text column; IsRequired(false); no index)
$env:LAMUFLIX_DESIGN_TIME_CONNECTION_STRING = "<non-blank design-time placeholder; never commit>"
dotnet tool restore
dotnet ef migrations add AddLibraryPathKey --project src/LamuFlix.Infrastructure --startup-project src/LamuFlix.Infrastructure --output-dir Persistence/Migrations
# Phase B — source model final (required + UseCollation("C") + unique IX_movies_library_path_key)
dotnet ef migrations add LibraryPathKeyActivation --project src/LamuFlix.Infrastructure --startup-project src/LamuFlix.Infrastructure --output-dir <ABSOLUTE DIRECTORY OUTSIDE THE WORKTREE UNDER TEMP>
dotnet ef migrations has-pending-model-changes --project src/LamuFlix.Infrastructure --startup-project src/LamuFlix.Infrastructure   # must exit 0
```

`--output-dir` for Phase B moves only the activation `.cs` + `.Designer.cs` outside the worktree; tooling rewrites the in-repo `ModelSnapshot` to final in its existing `Persistence/Migrations` location. Only `AddLibraryPathKey` ships. External activation migration/designer are disposable scratch — never committed, archived, hand-edited, or deleted inside the repo. No in-repo generated artifact is hand-edited, suppressed (`ConfigureWarnings`), or manually deleted. Acceptance: pending-check exit 0; live PG17 Migrate applies only the additive migration into schema-not-ready; an unrelated later scaffold emits no activation AlterColumn/CreateIndex.

**F8 recovery** (immediate successor of `AddLibraryPathKey`): if `migrations remove` is ever run on that successor, tooling restores the interim predecessor designer snapshot. Operator MUST redo Phase B final-model external scaffold, then verify `has-pending-model-changes` exit 0 and inspect the next scaffold so no activation ops re-emit, before applying anything. Owned by T007 acceptance and T022 ops docs.

**Testing**: xUnit v3, Shouldly, NSubstitute, AutoFixture, Faker.Net, FsCheck/property suites as already present, Testcontainers Postgres, and existing `MockFileSystem` conventions for scanner/discovery unit tests. Do not mock DB driver translation, uniqueness, or transaction/locking. Queue consumers follow existing repository test approach. No new testing libraries. No Gherkin. Shipping-runtime Unicode equivalence property required (supplementary/Deseret, Turkish i, sharp-s, ligatures, combining forms); samples are not proof. Compass expected sharp-s/ligature divergence claim is rejected — recon-DEV-459-2 §9.2 rows 23–38 contradict that premise; the property gate stands and is never waived.

**Target Platform**: .NET server — single production host `LamuFlix.Api` (Worker = hosted consumers inside Api; no `LamuFlix.Work` project at HEAD). Windows and Docker/Linux hosts; Docker Compose local stack. Concurrent host testing means multiple instances of that same host. Matching application version, runtime/globalization/ICU, and root/alias configuration across instances that open the same database (A9).

**Project Type**: Ports-and-adapters vertical slices in existing Core / Infrastructure / Api / ServiceDefaults projects; config and docs; no web feature implementation; no Worker project resurrection.

**Performance Goals**: Whole-table preflight/backfill/key activation at personal-library scale during coordinated startup downtime; post-activation idempotent no-write fast path when already canonical and consistent.

**Constraints**: No new ports/projects/layers/dependencies; no API shape/status/queue schema changes; no LocalPlay/launcher/Process.Start changes; no frontend implementation; never hand-edit generated EF snapshots/OpenAPI/TS; never lower harness thresholds; TimeProvider where time is needed; `const` / non-mutable static init hygiene for mutation; no explanatory comments outside allowed exemptions; matching Api instance configuration; hard stop-then-upgrade only; no concurrent index build; bypass writers prohibited; Core canonicalisation is pure string parsing with no `System.IO.Path`; filesystem resolution under the root stays in Infrastructure.

**Scale/Scope**: Size L; personal library; coordinated stop-then-upgrade downtime accepted.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (worktree current). Brief A7/A9/A10: no ticket contradiction, no constitution departure, no owner structural checkbox. Proposed ADR 0035 already claimed by grill and aligned by Keel under A10; Quill does not Accept it or invent a second ADR. A8 single-Api-host composition aligns with Principle I and recon-DEV-459-2 §9.8 (Worker hosted in Api).

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; new port needs ADR | **PASS** — reuse existing scanner/discovery/import/thumbnail surfaces; sealed Infrastructure gate with no interface/Core port; ArchitectureTests assert no new port; Core stays key-free and free of `System.IO.Path` (brief A5–A9; recon §§2–4, §9.8). |
| II (Explicit Handlers / Decorators) | Existing handler registration | **PASS** — import/rescan/details stay on existing endpoints/handlers; startup gate is composition/barrier in `Program.cs`, not a new public command API (A3, A8). |
| III (Typed query model) | MovieQuery | **N/A** — no filter/sort surface added. |
| IV (Domain transitions) | Movie mutators | **PASS** — path identity rewrite is migration/catalog persistence of Canonical LibraryPath; derived key populated at Infrastructure SaveChanges choke point and by gate backfill; rescan still must not mutate stored path (A5, A8–A9). |
| V (Validated inputs / errors) | Stable public statuses | **PASS** — routes/DTO fields/statuses unchanged; both-index `23505` stay on existing duplicate-import path; no new 409 mapping from advertised `ProducesProblem` (A1, A5, A8; recon §9.5). |
| VI (Observability) | Safe diagnostics | **PASS** — actionable diagnostics with movie IDs; no new public error fields ([assumed] ASSUMPTIONS). |
| VII (Configuration / LocalPlay / Process.Start) | Options validation; LocalPlay gated | **PASS** — extend existing `LibraryOptions` with `LegacyRootPaths`; named `LibraryOptionsValidator` + data annotations + `ValidateOnStart`; no LocalPlay/launcher changes (A5–A6, A9). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — define Library root / Canonical LibraryPath / Legacy root alias / Portable uniqueness / Library path key in spec; append at delivery (builder/Quill owns glossary edit; Gauge does not) without rewriting unrelated DEV-438 terms (brief Settled vocabulary; A8). |
| IX (Test Pyramid) | Real Postgres where translation/locking matter | **PASS** — Testcontainers for backfill transaction, uniqueness, concurrency, activation/rollback, lock/collation; unit/property for canonicalization and Unicode equivalence; AutoFixture/Faker.Net/MockFileSystem retained; existing consumer approach for queues (A7–A9). |
| Tech stack / API docs | OpenAPI chain | **PASS** — no DTO field-set change expected; regenerate/verify only if value-semantics docs require it; never hand-edit; no web feature work (A1, A7). |
| Governance / Static-Analysis Gates | Honest exits; never lower thresholds | Phase B / Build: report PASS/failure/SKIPPED/SKIP/N/A/Could not run honestly; Size L three-axis review; max three remediation rounds (A1, A7). Gates run sequentially (shared build outputs). |

### Complexity Tracking

No unjustified constitution violation. Complexity Tracking empty. Mechanism selection closed by Patron A8 Option C; placement/sequencing closed by Patron A9; EF tooling composition and F8 recovery closed by Patron A10 (recon-DEV-459-3 §10).

## Approach

- **Phase A (this draft)**: Quill writes/repairs spec/plan/tasks/checklist/receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon facts including A8–A10. Keel runs read-only `/speckit-analyze` and plan challenge. Transient status narration lives in `DRAFTING_RECEIPT.md`, not here. No Build code/gates here; drafting does not run `dotnet ef` or embed real connection values.
- **Phase B pickup (after Gate 1)**: Wisp fresh recon against landed main for recon-bounded surfaces and genuine drift checks (including Option C persistence/write surfaces and `Program.cs` barrier wiring). Host-composition path is already named (`src/LamuFlix.Api/Program.cs`); Phase B confirms drift only — the earlier composition-file gap is closed by recon-DEV-459-2 §9.8 + A8. Keel reanalysis on drift. Gauge baseline. Stop on unresolved facts/decisions — no invention.
- **Dependency-ordered implementation (brief task order)**:
  1. Canonical path rules (pure string parsing), shared `Library:LegacyRootPaths` + `LibraryOptionsValidator`, focused unit/property coverage including Unicode equivalence property gate. **Mismatch stop**: if the shipping-runtime property fails, stop before T006+; obtain Wisp counterexample/runtime recon via Bernstein; return to Patron; revise brief/CONCLUSIONS/ADR/spec/plan/tasks before continuation; never waive the gate.
  2. Movies-only Option C uniqueness via A10 two-scaffold composition (`AddLibraryPathKey` ships; external `LibraryPathKeyActivation` scratch), whole-table preflight before CI index, atomic locked path+key backfill + column-collation-`C` activation via sealed `MovieLibraryPathMigrationGate`, single Api-host startup barrier in `Program.cs` with Migrate→gate→Run and flag-off verify-only; real-DB proofs including pending-check exit 0, live PG17 additive-only Migrate/schema-not-ready, unrelated later scaffold emits no activation, column+index schema match, F8 recovery docs.
  3. Import/discovery/scanner/rescan canonical flows with Infrastructure SaveChanges choke-point key derivation (not per-writer), current-root absolute compatibility, both-index `23505` duplicate outcomes with endpoint assertions, automated `MovieDetailsResponse.Path` relative-value assertion, playback scanner regressions.
  4. Relative thumbnail publishing, legacy-message canonicalization, identity checks, existing authorization/terminal handling.
  5. Full acceptance/regression matrix (all eight families including Operations/T022) + deployment/configuration/backup/recovery/hard-stop/F8 migration-recovery documentation + **actual** SMOKE-001 Compose execution with recorded status.
  6. Required harness verification (sequential gates), Size L three-axis review, adjudication and bounded remediation; user owns merge.
- **Tests**: Prefer failing coverage for new behaviour before production edits where feasible; never mock uniqueness/transaction/locking semantics.
- **Refactor / architect / ship**: Per task-pipeline after Build; mutation for changed production C#; no fabricated web PASS.

## Project Structure

Surfaces from `brief.md` Implementation approach, `recon-DEV-459` §§2–5, `recon-DEV-459-2` §9 (Option C + composition), `recon-DEV-459-3` §10 (A10 tooling), and Patron A9–A10.

```text
specs/DEV-459/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md

docs/adr/
  0035-library-relative-movie-paths.md          (proposed; grill-claimed; status flip after user Gate 1 receipt — see T032)

src/LamuFlix.Core/
  Domain/LibraryPath.cs                         (canonical rules; pure string parsing; no System.IO.Path; key-free)
  Options/LibraryOptions.cs                     (LegacyRootPaths + data annotations)
  Options/LibraryOptionsValidator.cs            (IValidateOptions<LibraryOptions>; A9)
  Features/Import/*                             (canonicalize before uniqueness/persist; no key derivation)
  Features/Library/RescanMovieOperation.cs      (resolve; do not rewrite path)
  Features/Playback/PlayMovieCommandHandler.cs  (regression only via scanner)

src/LamuFlix.ServiceDefaults/
  Extensions.cs                                 (AddLamuFlixOptions: register LibraryOptionsValidator singleton; retain ValidateDataAnnotations + ValidateOnStart)

src/LamuFlix.Infrastructure/
  FileSystem/FileSystemLibraryFolderDiscovery.cs
  FileSystem/DirectoryMediaLibraryScanner.cs
  Persistence/LamuFlixDbContext.cs              (ONE shared private derivation pass; sync+async SaveChanges overrides)
  Persistence/MovieLibraryPathMigrationGate.cs  (sealed; no interface; no Core port; A9)
  Persistence/Records/MovieRecord.cs            (library_path + library_path_key; key lives only here)
  Persistence/Configurations/MovieConfiguration.cs  (final required key + property UseCollation("C") + unique IX; no index-level collation API)
  Persistence/Converters/LibraryPathConverter.cs    (no key derivation)
  Persistence/EfMovieRepository.cs / EfMovieCatalog.cs  (writers only; no key derivation)
  Persistence/Migrations/*                      (ship AddLibraryPathKey only; snapshot rewritten by Phase B tooling; never hand-edit/suppress/manual-delete)
  RabbitMq/EnrichmentConsumer.cs
  RabbitMq/ThumbnailConsumer.cs

src/LamuFlix.Api/
  Program.cs                                    (Migrate then gate then Run; HTTP + all hosted consumers)
  Endpoints/ImportEndpoints.cs                  (shape unchanged; value canonicalization; no new 409 mapping)
  Endpoints/LibraryResponses.cs                 (Path value semantics; automated relative assertion via T015/T016)
  ExceptionHandling/GlobalExceptionHandler.cs   (unchanged duplicate outcome path; no DbUpdateException→409)

tests/                                          (existing suites named in recon §5 + focused additions; ArchitectureTests no-new-port assertion; MockFileSystem retained)
docker/api.settings.example.json                (and related shared config examples)
operating docs for roots/aliases/rollout/rollback/hard-stop
CONTEXT.md                                      (delivery glossary append owned by builder/Quill; not Gauge; not Phase A edit)
```

**Layout decisions**:

- No research/data-model/contracts folders — decisions frozen in brief including A8 Option C, A9, and A10 tooling.
- Quill owns spec/plan/tasks/receipt/checklist. Quill does not edit brief/CONCLUSIONS/ASSUMPTIONS/ADR body/constitution/production code.
- Host-composition file is `src/LamuFlix.Api/Program.cs` (recon-DEV-459-2 §9.8; A8). No `src/LamuFlix.Work` barrier. Phase B recon checks drift only.
- Key derivation choke point is Infrastructure `LamuFlixDbContext` only (A9 H1): one shared private derivation pass reached by sync and async `SaveChanges`, setting `LibraryPathKey` from whole-string `LibraryPath.Value.ToUpperInvariant()` for tracked `MovieRecord` Added/Modified entries. No Core/converter/per-writer derivation. Gate backfill also writes path+key together. Tests mutate path through every supported writer and assert the key follows.
- Gate type is sealed `MovieLibraryPathMigrationGate` in `Persistence/` (A9 M1): no interface, no Core port; ArchitectureTests assertion for the no-new-port rule.
- Gate mechanics (A9 M2 + A10 F9): open one DbContext transaction; first SQL `LOCK TABLE movies IN ACCESS EXCLUSIVE MODE`; preflight through path+key backfill, activation DDL (explicit column COLLATE `"C"`, SET NOT NULL, non-concurrent CREATE UNIQUE INDEX), and consistency validation are atomic inside that transaction; final model declares property-level `UseCollation("C")` (not index-level API); T006/T010 assert effective **column** collation `C` on real PostgreSQL.
- EF tooling (A10 F5–F8): Phase A/B two-scaffold composition above; F8 immediate-successor `migrations remove` recovery owned by T007/T022.

## Test Strategy

| Family | Required evidence |
|---|---|
| Cross-platform identity | Windows drive, UNC, POSIX roots parsed on either host via pure string rules; both foreign-OS fixture sets run on one host; separators; nested paths; casing; `.`; idempotent relative normalization; root relocation; no `System.IO.Path` in Core |
| Safe preflight | Offline media migrates; malformed/unmapped/traversal/ambiguous roots fail; exact/case-only collisions fail with IDs before CI index creation; full-row snapshots of pre-existing columns unchanged; shared automatic `/media/library` map + ambiguity owned by T008 |
| Atomicity and startup | Real PostgreSQL/Testcontainers PG17: `has-pending-model-changes` exit 0; additive-only Migrate → schema-not-ready; post-gate column+index schema match including column collation `C`; unrelated later scaffold emits no activation; fresh/legacy/flag-off/ordering/atomicity proofs; ACCESS EXCLUSIVE + single-transaction path+key+column-collation activation; default Docker alias legacy-row proof; no processing before success |
| Persistence invariants | IDs and pre-existing columns unchanged; exact index retained; Option C key index active with **column** collation `C`; portable uniqueness DB-enforced; Unicode equivalence property on shipping runtime (never waived); concurrent duplicate imports; every-writer SaveChanges choke-point proofs |
| Import and rescan | Single/bulk relative and current-root absolute imports; key follows via DbContext choke point; `23505` from both indexes through existing duplicate path with endpoint assertions; rejected legacy/outside-root; rescan preserves identity; automated `MovieDetailsResponse.Path` relative-value assertion |
| Queue transition | New relative and old Windows/Docker absolute deliveries/retries; aliases after backfill; missing/mismatch/containment/reparse → no source access or retry loop |
| Shared runtime resolution | Reads use active root despite different CWD; playback scanner regression with MockFileSystem retained; LocalPlay boundary untouched |
| Operations | Shared host config; Docker alias/conflicts; hard stop of old instances; verify application/runtime/globalization/ICU and roots/aliases; backup; no mixed writers; backup-restore downgrade; alias retirement; F8 immediate-successor migrations-remove recovery; named existing backstops (post-activation NOT NULL + startup drift check); no new technical interlock |
| Visible outcome | SMOKE-001 actually executed on Compose movie details with recorded runtime status (or Could not run) |

## Gates (after Gate 1 and implementation — Gauge owns configured Phase 3 gates)

Gates run **sequentially** (shared worktree `obj`/`bin`; no parallel gate scripts).

1. Changed-C# scope: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (+ refactor threshold from `harness.yml`), `./scripts/run-jetbrains-inspectcode.ps1`.
2. Affected suites then full `dotnet test`.
3. Property / vulnerable-packages / `dotnet format --verify-no-changes` per harness; honest SKIPPED/SKIP/N/A/Could not run.
4. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` for eligible changed production C# (no `--since` from worktrees).
5. Web/contract gates by actual applicability only — no web implementation planned; do not manufacture PASS. Gauge reports applicability; does not edit CONTEXT.md.
6. Size L three-axis review (Sentry/Ledger/Compass) under closing bar; max three remediation rounds; unresolved findings return to Bernstein.
7. Rebased gates before awaiting-merge; user merges.

Phase A runs none of these and claims no gate result.

## Risks

- **OrdinalIgnoreCase vs SQL**: Mitigated by Patron A8 Option C (.NET-derived key); prohibited `lower()`/generic collation/citext; shipping-runtime Unicode equivalence property retained. Compass expected sharp-s/ligature divergence is **rejected** because recon-DEV-459-2 §9.2 contradicts that premise. **Mismatch contingency (never waive)**: stop before T006+; Bernstein routes Wisp counterexample/runtime recon; Patron decision; revise brief/CONCLUSIONS/ADR/spec/plan/tasks before continuation (A8–A9; recon §9.2–9.4).
- **Trusted-writer derivation drift**: Mitigated by single Infrastructure `LamuFlixDbContext` SaveChanges choke point (sync+async shared pass), gate backfill writing path+key, bypass prohibition, startup consistency validation fail-closed; tests cover every supported writer (A8–A9).
- **Migrate-before-preflight / snapshot drift**: Mitigated by A9/A10 — additive `AddLibraryPathKey` Migrate, final model/snapshot via Phase B external scaffold, gate-owned activation DDL with column collation `C`, Migrate→gate→Run, flag-off verify-only fail-closed, pending-check/PG17/later-scaffold proofs (A8–A10; recon-DEV-459-3 §10).
- **F8 successor remove fragility**: Documented recovery — redo Phase B external scaffold, pending-check exit 0, inspect next scaffold for no activation before apply; no suppression/manual deletion (A10; recon F8).
- **Runtime/globalization/ICU drift**: Persisted keys are runtime-bound; T022 hard-stop requires matching application version, runtime/globalization/ICU, and roots/aliases; named backstops are post-activation NOT NULL and startup drift fail-closed; no new technical interlock (A6, A9).
- **Queued absolute vs relative window**: Mitigated by shared root map + alias retention through drain (A4, A6).
- **CWD resolution bugs**: Mitigated by FR-012/FR-017 and explicit tests with divergent CWD (recon §4 Risk A).
- **Host composition drift**: Barrier file named (`Program.cs`); Phase B confirms drift only (recon §9.8).
- **Coordinated downtime / backup discipline**: Documented hard stop-then-upgrade and backup-restore downgrade (A6, A9); fail-closed startup preferred over partial migration (ADR 0035 trade-off).

## Ordering

Matches brief Task ordering 1→6 and user stories US1→US5. US1 Unicode property must pass (or return to Patron) before US2 T006+. US2 (gate/uniqueness/key activation) blocks US3–US4 integration dependence on canonical stored values. US5 docs/gates/smoke execution last.

## ADR status (durable)

Proposed ADR 0035 remains `proposed` until the user Gate 1 spec-PR merge receipt is recorded in `CONCLUSIONS.md` (T000). After that receipt exists, Keel (or the seat the Gate 1 process names) flips ADR status from proposed to accepted as part of delivery process task T032 — without treating Patron provisional approval or this plan draft as that flip. Quill does not Accept the ADR in Phase A.
