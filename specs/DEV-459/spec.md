# Feature Specification: Migrate stored movie folders to library-relative paths

**Feature Branch**: `feature/459-spec`
**Created**: 2026-11-07
**Status**: gate1: provisional
**Input**: Ticket DEV-459 (parent DEV-100); grill-closed `brief.md` (A1–A10); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; proposed `docs/adr/0035-library-relative-movie-paths.md`; Maestri notes `DEV-459`, `recon-DEV-459`, `recon-DEV-459-2`, `recon-DEV-459-3`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only). Dependency: DEV-438.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` A7/A9/A10; `PRODUCT.md` §5). Ordinary owner Gate 1 on the spec PR remains pending. Patron approval is not owner approval.

**Mechanism decision (resolved):** Patron A8 adopts Option C — movies-only `library_path_key` derived by whole-string `string.ToUpperInvariant()` with binary unique index `IX_movies_library_path_key`, retaining `IX_movies_library_path`. Patron A9 places derivation at the Infrastructure `LamuFlixDbContext` SaveChanges choke point, names sealed `MovieLibraryPathMigrationGate` (no Core port), requires sole additive nullable/no-index migration with gate-owned activation DDL and final model/snapshot, Migrate→gate→Run with flag-off verify-only fail-closed, ACCESS EXCLUSIVE + single transaction, named `LibraryOptionsValidator`, and hard-stop runtime/globalization/ICU matching. Patron A10 sanctions the exact recon-DEV-459-3 §10 F5 two-scaffold composition (`AddLibraryPathKey` ships; external `LibraryPathKeyActivation` scratch rewrites in-repo snapshot), property-level `UseCollation("C")` with gate-set **column** collation `C` (no index-level collation API), pending-check/PG17/later-scaffold proofs, and F8 immediate-successor `migrations remove` recovery without suppression or manual generated-file deletion. Quill does not invent alternatives and does not equate `lower()`, `citext`, or generic collation to that contract. Expected sharp-s/ligature property failure is rejected (recon-DEV-459-2 §9.2); the shipping-runtime property gate stands and is never waived. Proposed ADR 0035 remains proposed until user Gate 1 receipt.

## Clarifications

### Session 2026-11-07

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity? → A: None after A1–A7 for identity, mapping, startup gate, queue compatibility, import/rescan, configuration/deployment, acceptance matrix, and bounded file scope. Post-drafting Patron A8 later resolved the OrdinalIgnoreCase uniqueness mechanism (Option C), single Api-host composition, atomic key activation, and unchanged duplicate outcomes. Patron A9 later resolved plan-challenge placement/sequencing (DbContext choke point, sealed gate, additive migration + gate DDL, Migrate→gate→Run, flag-off fail-closed, lock/collation, options validator, hard-stop runtime match). Patron A10 later resolved the exact EF two-scaffold tooling composition, column-collation activation refinement, proof obligations, and F8 recovery (recon-DEV-459-3 §10). No residual drafting needs-decision or needs-recon remains.

## Scope

**In scope** (`brief.md` Frozen delivery scope; A1–A10; ticket Scope):

1. **Canonical LibraryPath identity**: Persist and return portable non-rooted movie-folder identity relative to active `Library:RootPath`, `/` separators, original segment casing, redundant `.` segments removed, library root represented as `.`. Reject parent traversal, drive-relative, malformed, and outside-root paths. Same value on `movies.library_path`, `Movie.Path`, and `MovieDetailsResponse.Path`. Original `library_path` casing is retained. Core canonicalisation is pure string parsing (no `System.IO.Path`); both foreign-OS fixture sets run on one host; filesystem resolution under the root stays in Infrastructure.
2. **Cross-platform backfill**: Map existing Windows drive/UNC and POSIX absolute stored paths via active root (automatic) plus optional `Library:LegacyRootPaths` and unambiguous standard Docker alias `/media/library` (shared automatic root-map construction and ambiguity policy owned by the startup gate). OS-independent syntax parsing; Windows prefix match `OrdinalIgnoreCase` with equivalent separators; POSIX prefix match `Ordinal`; preserve suffix casing; segment-boundary matches; idempotent relative normalization; no media-existence requirement; fail closed on unmatched/malformed/escaping/conflicting overlapping maps.
3. **Atomic startup gate (single Api host)**: Mandatory configuration-aware sealed `MovieLibraryPathMigrationGate` (Infrastructure Persistence; no interface; no Core port) invoked from `src/LamuFlix.Api/Program.cs` after `Build` and before `Run`, covering HTTP listeners and all hosted RabbitMQ consumers (Worker denotes those hosted consumers; no separate Worker project). Options validation completes before any gate DB write. Ordering: additive `Migrate` → gate → `Run`. When startup migrations are disabled, the gate is verify-only and fails closed with schema-not-ready if the activated schema is absent. First SQL in the gate transaction is `LOCK TABLE movies IN ACCESS EXCLUSIVE MODE`; whole-table preflight with actionable duplicate/movie-ID diagnostics **before** case-insensitive unique-index creation; atomic locked path backfill + `library_path_key` population + activation that sets **column** collation `C` + SET NOT NULL + non-concurrent unique index in one transaction; preserve IDs and pre-existing business columns; intentionally populate the new derived key; retain `IX_movies_library_path`; exact and case-only target collisions block on all hosts without merge/delete/suffix; rollback and fail startup closed on any failure; post-activation already-canonical+consistent catalog is an idempotent no-write rerun; concurrent instances of the same host serialize safely. Sole shipped tool-generated migration is additive nullable/no-index (`AddLibraryPathKey` via A10 two-scaffold); final EF model/snapshot declare required + property `UseCollation("C")` + unique index; gate owns activation DDL; Migrate-only DB is schema-not-ready.
4. **Import / discovery / scanner / rescan**: New imports accept relative or current-root absolute values, canonicalize before uniqueness and persistence; reject legacy-alias and outside-root imports; race-safe portable uniqueness for future imports via Option C with key derivation at the Infrastructure `LamuFlixDbContext` SaveChanges choke point (every supported writer; no Core/converter/per-writer derivation); all filesystem resolution under active root (never process CWD); rescan preserves canonical stored identity; automated assertion that `MovieDetailsResponse.Path` returns the canonical relative value; playback regression through existing scanner seam without LocalPlay/launcher changes.
5. **Queued thumbnails across migration**: Keep `ThumbnailRequested(MovieId, string FilePath)` and queue topology; publish canonical relative paths; canonicalize old absolute payloads through the same root map; compare with `OrdinalIgnoreCase` to current movie path; never authorize on ID alone; resolve under active root with existing containment/reparse/eligibility; terminal `IneligibleRequest` / `UnauthorizedSourcePath` without source access or retry loops; retain aliases through deliveries/retries/DLQ.
6. **Configuration and operations docs**: Optional `Library:LegacyRootPaths` on shared `LibraryOptions` with named `LibraryOptionsValidator` plus retained data annotations/`ValidateOnStart`; hard stop of old instances; verify matching application version, runtime/globalization/ICU, and roots/aliases; backup; no mixed writers; backup-based downgrade; alias retirement documentation; F8 immediate-successor migrations-remove recovery; named existing backstops only (no new technical interlock); no admin UI/CLI.
7. **Bounded surfaces**: Existing Core/Infrastructure/Api/ServiceDefaults (Worker consumers hosted in Api), tests, config, documentation, and tooling-generated EF migration artifacts for movies-only uniqueness/key (`AddLibraryPathKey` only). No new dependencies, projects, layers, tables, routes, DTO field sets, HTTP statuses, queue schema, or frontend implementation.

**Out of scope**:

- Frontend feature work / design / new UI.
- LocalPlay flag or `ProcessMediaPlayerLauncher` / `Process.Start` changes.
- New maintenance CLI or admin UI for migration.
- Automatic reverse backfill / downgrade without backup restore.
- New NuGet/npm packages, ports, projects (including resurrecting a Worker project), or architectural layers.
- Schema beyond the movies-table path/key uniqueness migration already ruled (A5/A7/A8/A9/A10).
- Inferring historical roots from basenames or suffixes.
- Mixing old and new writers across hosts or mismatched runtime/globalization/ICU instances on the same database.
- SQL `lower()` / `citext` / generic collation / Options A–B Unicode SQL ports as uniqueness mechanisms.
- Waiving the shipping-runtime Unicode equivalence property gate, or writing expected sharp-s/ligature divergence contrary to recon-DEV-459-2 §9.2.
- Hand-editing generated EF artifacts; suppressing pending-model-changes; manually deleting in-repo generated artifacts; inventing a tooling sequence other than A10 / recon-DEV-459-3 §10 F5.

**Precedence**: Ticket Scope & Technical Design is already approved authority (`PRODUCT.md` §3). Brief A1–A10 and `CONCLUSIONS.md` govern boundaries. Ordinary owner Gate 1 remains pending. Proposed ADR 0035 awaits user Gate 1 merge receipt before status flip; Quill does not Accept it.

## Domain vocabulary *(mandatory — glossary terms for CONTEXT.md at delivery)*

Use established **LibraryPath** vocabulary from DEV-438 without rewriting unrelated domain terms. Feature definitions:

**Library root**
Active `Library:RootPath`, the filesystem location for all runtime media reads.
_Avoid_: media folder (ambiguous), CWD, process working directory

**Canonical LibraryPath**
Portable non-rooted movie-folder identity relative to the library root: `/` separators, original segment casing, redundant `.` segments removed, root represented by `.`. Reject parent traversal, drive-relative, malformed, and outside-root paths. Persist and return this same value.
_Avoid_: absolute folder, resolved path (when meaning stored identity), full path

**Legacy root alias**
A recognized historical absolute Windows drive/UNC or POSIX root prefix used only for backfill and legacy thumbnail messages; never authorization of new imports outside the active root.
_Avoid_: import root, secondary library root

**Portable uniqueness**
No two movie rows have canonical identities equal under `OrdinalIgnoreCase`, on any host, while retaining the exact unique index and enforcing the Option C derived-key unique index.
_Avoid_: case-sensitive uniqueness alone, application-only duplicate check, SQL `lower()`/collation equivalence

**Library path key (Option C)**
Derived equality key stored in `movies.library_path_key` as whole-string `string.ToUpperInvariant()` of the canonical `library_path` (never per-char `char.ToUpperInvariant`). Non-null after activation. Binary deterministic unique index `IX_movies_library_path_key`. Column collation `C` is declared in the final model via property-level `UseCollation("C")` and applied by gate activation DDL (no index-level collation API). Derived only at the Infrastructure `LamuFlixDbContext` SaveChanges choke point (and by the configuration-aware gate backfill); Core stays key-free. Database enforces uniqueness/concurrency only; it does not validate derivation.
_Avoid_: per-char fold, SQL fold port, generated-column Options A/B, per-writer/converter derivation, index-level collation API claim

## What the User Will See *(mandatory)*

- **Where**: Movie details path line (existing details view that shows/copies `path` from movie details).
- **What the user sees or does**: After upgrade and successful startup gate, the path value shown and copied is the canonical library-relative folder (for example a folder name or nested relative path, or `.` for the library root itself), not a host-specific absolute Windows or Docker path. No new controls or pages.
- **What it needs to show up**: Default Compose (or equivalent) stack with Api upgraded (hosted consumers included), matching `Library:RootPath` / `Library:LegacyRootPaths`, successful startup backfill/key activation, and at least one movie whose details are opened.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001** *(planned criteria)*: **Given** a library already containing an imported movie and a successful stop-then-upgrade with matching root configuration, **When** the user opens that movie's details, **Then** the path shown (and available to copy) is library-relative rather than an absolute host path.
- **SMOKE-001** *(observed runtime acceptance)*: Delivery must actually execute Compose movie-details display/copy verification and record runtime evidence with the exact resulting status. If Compose/runtime evidence is unavailable, report **Could not run** as unresolved acceptance — never as PASS. No frontend implementation required.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Canonical identity, options, and validation (Priority: P1)

Operators and the running system share one definition of canonical LibraryPath and optional legacy root aliases. Relative values normalize idempotently via pure string parsing (no `System.IO.Path` in Core; both foreign-OS fixtures on one host); absolute values under the active root canonicalize before use; invalid shapes are rejected. Configuration validates legacy prefixes via named `LibraryOptionsValidator` (plus retained data annotations/`ValidateOnStart`) before any gate write, backfill, or message normalization. The unambiguous Docker alias `/media/library` participates for legacy backfill/messages only when it does not conflict.

**Why this priority**: Foundation for every later story (brief task order 1; A1, A5, A6, A9).

**Independent Test**: Domain/unit/property coverage for canonicalization, separator/dot rules, `.` root representation, rejection cases, both-OS one-host fixtures, `LibraryOptionsValidator` coverage, and shipping-runtime Unicode equivalence — without requiring HTTP or queue consumers. Property mismatch stops before US2 uniqueness work.

**Acceptance Scenarios**:

1. **Given** a relative folder path with mixed separators and redundant `.` segments, **When** it is canonicalized, **Then** the result uses `/`, preserves segment casing, removes redundant dots, and is idempotent under re-normalization.
2. **Given** an absolute path under the active library root, **When** it is accepted for import or mapping, **Then** the stored identity is the canonical relative form.
3. **Given** a path with `..`, a drive-relative form, or a location outside the active root, **When** validation runs for persistence or new import, **Then** it is rejected.
4. **Given** the library root itself as the movie folder, **When** identity is canonicalized, **Then** it is represented as `.`.
5. **Given** malformed or non-absolute legacy alias entries, **When** options validate, **Then** startup/backfill/message normalization fails closed with actionable diagnostics ([assumed] copy conventions in `ASSUMPTIONS.md`).

---

### User Story 2 - Atomic startup backfill, Option C key, and portable uniqueness (Priority: P1)

On Api startup, before any HTTP endpoint or hosted RabbitMQ consumer processes work, validated options are resolved and a configuration-aware sealed `MovieLibraryPathMigrationGate` runs from `Program.cs` (after Build, before Run) with fixed order additive `Migrate` → gate → `Run`. When startup migrations are disabled, the gate verifies the final activated schema only and fails closed with schema-not-ready if incomplete. The gate opens one DbContext transaction, takes `LOCK TABLE movies IN ACCESS EXCLUSIVE MODE` first, preflights the entire movies table (with actionable duplicate/movie-ID diagnostics) before creating the case-insensitive unique index, and atomically performs path backfill, `library_path_key` population (whole-string `ToUpperInvariant`), activation that sets column collation `C` + SET NOT NULL + non-concurrent unique index, plus consistency verification in that transaction. Shared automatic `/media/library` root-map construction and ambiguity policy are owned here. Movie IDs and pre-existing business columns remain unchanged; the new derived key is intentionally populated. Exact and case-only collisions fail closed. Ordinary EF `Migrate` ships only additive `AddLibraryPathKey` (nullable key, no unique index, no NOT NULL, no collation) via A10 two-scaffold; the final EF model/snapshot declare required + property `UseCollation("C")` + unique index; activation DDL is gate-owned. Tooling-generated EF artifacts only; no concurrent index build; no hand-edits/suppression/manual generated-file deletion. An already-canonical path table still requires key population and constraint activation before the no-write fast path applies. Concurrent Api host instances serialize safely; interruption leaves no partial rewrite. Startup verifies existing key consistency under the shipping runtime and fails closed on drift.

**Why this priority**: Ticket backfill + ID/uniqueness preservation; blocks safe import/rescan/queue work (brief task order 2; A2–A3, A5, A8–A10).

**Independent Test**: Real PostgreSQL/Testcontainers PG17 proofs for `has-pending-model-changes` exit 0, additive-only Migrate/schema-not-ready, post-gate column+index schema match including column collation `C`, unrelated later scaffold emitting no activation, preflight failures, collision rejection, ACCESS EXCLUSIVE + single-transaction rollback after actual writes/activation failure (mid-preflight failure alone does not prove write rollback), interruption/restart, concurrent same-host-instance gate serialization, ID/pre-existing-column preservation with intentional key population, exact index retention, fresh/legacy/flag-off/ordering/atomicity proofs, default Docker alias legacy-row/ambiguity proofs, already-canonical paths with missing keys, consistency drift, post-activation no-write rerun, and concurrent duplicate-import uniqueness — without mocking driver translation/locking. Shipping-runtime Unicode equivalence property covering supplementary/Deseret, Turkish dotted/dotless i, sharp-s, ligatures, and combining forms must already be green (or Patron-revised after mismatch stop) before this story's uniqueness work; probe samples are evidence not proof; mismatch returns to Patron and never waives the gate.

**Acceptance Scenarios**:

1. **Given** movies stored under Windows drive, UNC, and POSIX absolute roots that map through active/legacy/default Docker aliases (including unambiguous `/media/library`), **When** the startup gate succeeds after additive Migrate, **Then** each row's path is canonical relative identity, `library_path_key` is populated, IDs and pre-existing business columns are unchanged, both unique indexes are active, `library_path_key` has effective column collation `C`, and the database matches the final EF model.
2. **Given** offline media (files absent), **When** backfill runs, **Then** rows still migrate without requiring filesystem existence.
3. **Given** an unmatched, malformed, escaping, or conflicting overlapping root mapping, **When** preflight runs, **Then** startup fails closed with movie ID and actionable reason and no path/key writes commit.
4. **Given** two rows that would collide exactly or only by case under `OrdinalIgnoreCase`, **When** preflight runs, **Then** startup fails closed without merging, deleting, or suffixing rows and before CI unique-index creation.
5. **Given** already-canonical paths with missing or inactive keys/constraints, **When** either host instance starts, **Then** key population and constraint activation still run before any processing; only after keys and constraints are valid is a later start an idempotent no-write rerun.
6. **Given** multiple Api host instances starting together, **When** they run the gate, **Then** they serialize safely and neither processes HTTP or hosted consumers before success.
7. **Given** interruption after actual writes or during activation, **When** the process restarts, **Then** rows are unchanged from before the failed attempt or fully canonical with valid keys after a later successful commit — never a partial mix — proven by real-DB evidence rather than startup self-report alone.
8. **Given** key/path derivation drift under the shipping runtime, **When** startup consistency validation runs, **Then** the host fails closed and does not process work.
9. **Given** `DatabaseOptions.ApplyMigrationsOnStartup` is false and the activated schema is incomplete, **When** the gate runs, **Then** it performs no migration/activation and fails closed with a schema-not-ready diagnostic before any processing.

---

### User Story 3 - Import, discovery, scanner, rescan, and playback regression (Priority: P1)

Single and bulk import accept relative paths and absolute paths under the active root only, canonicalize before uniqueness checks, rely on the Infrastructure `LamuFlixDbContext` SaveChanges choke point so `library_path_key` follows every supported EF writer, and map duplicates (including case-only variants) to existing duplicate-import outcomes when either `IX_movies_library_path` or `IX_movies_library_path_key` raises `23505`, while preserving existing movie IDs. Discovery and scanner resolve under the active root, never process CWD. Legacy aliases never authorize new imports. Rescan resolves stored relative identity under the active root without mutating that identity. Returned `MovieDetailsResponse.Path` is the canonical relative value (automated assertion, distinct from SMOKE-001). Playback continues through the existing scanner seam; LocalPlay and launcher code stay untouched, covered by regression tests. Advertised `ProducesProblem(409)` metadata MUST NOT introduce a new status mapping from these exceptions.

**Why this priority**: Ticket import/rescan coverage and post-migration invariant (brief task order 3; A1, A5, A8–A9).

**Independent Test**: Existing import persistence/API, discovery/scanner, rescan, details Path relative-value, and playback scanner regression suites plus focused additions; race-safe duplicate proofs on real DB for both constraint names; endpoint behaviour assertions for both constraints (not only database exception tests); sequential and concurrent single/bulk imports preserving winner ID; every-writer choke-point key proofs.

**Acceptance Scenarios**:

1. **Given** a relative folder under the library root, **When** single or bulk import succeeds, **Then** the stored and returned path is canonical relative identity and `library_path_key` follows via the DbContext SaveChanges choke point.
2. **Given** an absolute path under the active root, **When** import runs, **Then** it canonicalizes and persists the relative identity with matching key via that same choke point.
3. **Given** a legacy-alias absolute path or outside-root path on a new import, **When** import is attempted, **Then** it is rejected (aliases are backfill/message-only).
4. **Given** an exact or case-only duplicate of an existing canonical path, **When** import runs concurrently or sequentially (single or bulk), **Then** `23505` from either `IX_movies_library_path` or `IX_movies_library_path_key` maps through the unchanged existing duplicate-import path, the existing/winner ID is preserved, and endpoint behaviour matches today's contract without a new 409 mapping.
5. **Given** a stored canonical relative path, **When** rescan runs, **Then** resolution uses the active root and stored identity is unchanged.
6. **Given** playback of a movie with a relative stored path and a process CWD different from the library root, **When** play resolves through the scanner, **Then** media resolution still uses the active root; LocalPlay gate and launcher remain unmodified.

---

### User Story 4 - Queued thumbnail compatibility across migration (Priority: P1)

Enrichment continues to enqueue thumbnail work using the movie's canonical relative path. Consumers accept new relative payloads and old absolute Windows/Docker payloads by canonicalizing through the same root map, comparing to the loaded movie with `OrdinalIgnoreCase`, and resolving authorized folders under the active root. Message schema and queue topology stay unchanged. Unknown roots, escaping payloads, missing movies, and mismatches take existing terminal paths without opening sources or retrying forever. Configured aliases remain until deliveries, retries, and DLQ replays drain.

**Why this priority**: Ticket queued-thumbnail clause (brief task order 4; A4, A6).

**Independent Test**: Existing thumbnail/enrichment consumer suites plus focused relative/legacy absolute cases, mismatch/missing/terminal paths, and CWD-vs-root resolution.

**Acceptance Scenarios**:

1. **Given** a migrated movie, **When** a new `ThumbnailRequested` is published, **Then** `FilePath` is the canonical relative `movie.Path` value.
2. **Given** an in-flight absolute Windows or Docker `FilePath` for a migrated movie whose aliases still match, **When** the consumer processes it, **Then** the payload canonicalizes, matches the movie under `OrdinalIgnoreCase`, and resolves under the active root.
3. **Given** a payload whose MovieId matches but canonicalized path does not, **When** the consumer evaluates eligibility, **Then** it does not authorize on ID alone and takes the existing terminal ineligible/unauthorized path without source access.
4. **Given** an unknown root, escaping payload, or missing movie, **When** the consumer runs, **Then** existing terminal handling applies with no retry loop and no source open.
5. **Given** a relative payload and a process CWD outside the library root, **When** authorization resolves the folder, **Then** resolution uses `Library:RootPath`, not CWD.

---

### User Story 5 - Operations documentation and acceptance matrix (Priority: P2)

Operators receive documentation for shared host configuration, default Docker alias behavior and conflicts, hard stop of old instances, verification of matching application version / runtime / globalization / ICU / roots / aliases, pre-upgrade backup, no mixed writers, backup-restore downgrade, preflight diagnostics, alias retirement after queue drain, and the named existing backstops (post-activation NOT NULL; startup drift fail-closed) with no new technical interlock. The full acceptance/regression matrix from the brief — all eight families including Operations — is covered by automated evidence; SMOKE-001 is actually executed with runtime evidence (or Could not run); harness gates run sequentially and three-axis review follows L-ticket rules without fabricating PASS for inapplicable web gates.

**Why this priority**: Completes operable delivery after functional stories (brief task order 5–6; A6–A7, A9).

**Independent Test**: Doc review against A6/A9; actual Compose smoke execution with recorded status; gate/review evidence tied to actual diff/head; web/contract gates reported by applicability (no web implementation planned).

**Acceptance Scenarios**:

1. **Given** matching vs mismatched Api instance application/runtime/globalization/ICU/root/alias configuration, **When** operators follow the docs, **Then** they hard-stop old instances, verify match, take backup, upgrade only then, and understand fail-closed startup with named existing backstops and no new interlock.
2. **Given** a successful backfill, **When** rollback is required, **Then** docs prescribe restoring the pre-upgrade database backup with matching old binaries — not automatic path reversal.
3. **Given** drained legacy deliveries/retries/DLQ, **When** aliases are retired, **Then** docs state when removal is safe.
4. **Given** Compose with a migrated movie, **When** SMOKE-001 executes, **Then** runtime evidence records the observed relative path display/copy result, or Could not run if unavailable.
5. **Given** the acceptance matrix lock, **When** T023 completes, **Then** all eight brief families including Operations/T022 have named automated evidence.

### Edge Cases

- Conflicting overlapping legacy prefixes that yield different relative identities → fail closed (no longest-prefix preference).
- Case-only collisions on Linux/Docker hosts → still blocked for portability.
- Non-ASCII / Unicode under Option C: shipping-runtime proof that `a.Equals(b, OrdinalIgnoreCase)` equals `(a.ToUpperInvariant() == b.ToUpperInvariant())` across supplementary/Deseret, Turkish dotted/dotless i, sharp-s, ligatures, and combining forms; samples are not proof; mismatch blocks acceptance, stops before uniqueness implementation (T006+), returns to Patron after Wisp counterexample/runtime recon, and requires brief/CONCLUSIONS/ADR/spec/plan/tasks revision before continuation — never waive. Expected sharp-s/ligature divergence claims are rejected (recon-DEV-459-2 §9.2).
- Root folder movie represented as `.`.
- Already-canonical paths with missing keys still require key population/activation before no-write fast path.
- Mixed-version / raw writers that bypass key derivation remain prohibited.
- Standard `/media/library` alias omitted or conflicting → never authorize outside-root imports; conflicts fail closed.
- Mid-preflight failure alone does not prove write rollback; rollback coverage requires actual writes/activation failure paths.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST persist and return Canonical LibraryPath for movie folders as defined in Domain vocabulary (A1).
- **FR-002**: System MUST leave public API routes, DTO field sets, and HTTP status codes unchanged while Path value semantics become relative (A1).
- **FR-003**: `POST /api/movies/import` MUST accept library-relative paths and absolute paths under the active `Library:RootPath`, canonicalizing before uniqueness and persistence (A1, A5).
- **FR-004**: Backfill MUST map absolute stored paths using active root plus optional `Library:LegacyRootPaths` and unambiguous `/media/library`, with OS-independent pure-string parsing (no `System.IO.Path` in Core), shared automatic default Docker root-map construction owned by the gate, and fail-closed ambiguity rules (A2, A6, A9).
- **FR-005**: Backfill MUST NOT require media files to exist on disk (A2).
- **FR-006**: `LamuFlix.Api` MUST complete additive `Migrate` (when enabled) then the configuration-aware sealed `MovieLibraryPathMigrationGate` in `src/LamuFlix.Api/Program.cs` after `Build` and before `Run`, covering HTTP listeners and all hosted consumers (Worker = hosted consumers in Api; no separate Worker project) before any of them process work; when migrations are disabled the gate MUST verify-only and fail closed with schema-not-ready if the activated schema is absent (A3 as clarified by A8–A9; recon-DEV-459-2 §9.8).
- **FR-007**: Startup backfill MUST open one DbContext transaction, take `LOCK TABLE movies IN ACCESS EXCLUSIVE MODE` as the first statement, preflight the whole table (actionable duplicate/movie-ID diagnostics) **before** creating the case-insensitive unique index, and atomically commit path backfill + `library_path_key` population + activation DDL that explicitly sets column collation `C` together with SET NOT NULL and non-concurrent CREATE UNIQUE INDEX, plus consistency verification, in that transaction while preserving movie IDs and pre-existing business columns, retaining `IX_movies_library_path`, and intentionally writing the new derived key (A3 as clarified by A8–A10; recon-DEV-459-3 F9). Ordinary EF `Migrate` MUST ship only additive `AddLibraryPathKey` (nullable, no index, no NOT NULL, no collation); final EF model/snapshot declare required + property `UseCollation("C")` + unique index via A10 two-scaffold composition; activation DDL is gate-owned; Migrate-only DB is schema-not-ready. Tooling-generated EF artifacts only; never hand-edit, suppress pending-model-changes, or manually delete in-repo generated artifacts; no concurrent index build. Acceptance MUST include `has-pending-model-changes` exit 0, live PG17 additive-only Migrate/schema-not-ready, unrelated later scaffold emitting no activation, and final column+index schema proof. F8 immediate-successor `migrations remove` recovery MUST be owned here and in ops docs (A10; recon-DEV-459-3 F5–F8, F10).
- **FR-008**: Preflight MUST block exact and `OrdinalIgnoreCase` target collisions on all hosts without merge/delete/suffix; failures MUST roll back and fail startup closed with actionable diagnostics (A3; ASSUMPTIONS).
- **FR-009**: After keys and constraints are valid, an already-canonical consistent table MUST be an idempotent no-write startup rerun; concurrent Api instances MUST serialize safely; interruption/restart safety and rollback after actual writes/activation failure MUST be proven with real DB evidence (mid-preflight failure alone is insufficient) (A3, A8–A10).
- **FR-010**: Future single/bulk imports MUST enforce race-safe portable uniqueness via Option C: `movies.library_path_key` derived by whole-string `string.ToUpperInvariant()` only at the Infrastructure `LamuFlixDbContext` shared SaveChanges choke point (sync+async) and by gate backfill — not in Core, converter, or per-writer sites — non-null after activation, binary unique `IX_movies_library_path_key`, column collation `C` (property `UseCollation("C")` + gate DDL), retaining `IX_movies_library_path`; application-only prechecks and bypass writers are insufficient/prohibited; database enforces uniqueness/concurrency and does not validate derivation; tests MUST cover every supported writer; startup MUST verify key consistency under the shipping runtime and fail closed on drift (A5, A8–A10).
- **FR-011**: New imports MUST NOT treat legacy aliases as authorized import roots; aliases are restricted to backfill and queued thumbnail compatibility (A5, A6).
- **FR-012**: All runtime filesystem resolution for stored/imported relative paths MUST use active `Library:RootPath`, never process CWD; resolution belongs in Infrastructure only (A4, A5, A9).
- **FR-013**: Rescan MUST resolve relative stored identity under the active root without mutating that identity (A5).
- **FR-014**: Playback MUST remain gated/behaved through the existing scanner seam; this ticket MUST NOT modify `Features:LocalPlay` or the process launcher (A5).
- **FR-015**: System MUST retain `ThumbnailRequested(MovieId, string FilePath)` schema and queue topology; new publishes MUST use canonical relative paths (A4).
- **FR-016**: Consumers MUST canonicalize legacy absolute payloads through the same root map, compare with `OrdinalIgnoreCase` to the loaded movie path, and MUST NOT authorize on MovieId alone (A4).
- **FR-017**: Authorized thumbnail folders MUST resolve under the active root with existing containment, reparse-point, and eligibility checks; unknown/escaping/missing/mismatch cases MUST use existing terminal outcomes without source access or retry loops (A4).
- **FR-018**: System MUST expose optional `Library:LegacyRootPaths` on shared `LibraryOptions`, validate via named `LibraryOptionsValidator` (`IValidateOptions<LibraryOptions>` registered in `AddLamuFlixOptions`) while retaining data annotations and `ValidateOnStart`, complete options validation before any gate database write, and document hard-stop upgrade (stop old instances; verify application/runtime/globalization/ICU and roots/aliases; backup; no mixed writers), backup/restore downgrade, diagnostics, alias retirement, F8 immediate-successor migrations-remove recovery (redo Phase B external scaffold; pending-check exit 0; inspect next scaffold for no activation before apply; no suppression/manual deletion), and named existing backstops with no new technical interlock, without an admin UI/CLI; docs MUST NOT embed real connection values (A6, A9–A10).
- **FR-019**: Delivery MUST cover all eight brief acceptance matrix families (cross-platform identity, safe preflight, atomicity/startup, persistence invariants, import/rescan, queue transition, shared runtime resolution, operations) with the named evidence styles, including Operations evidence via T022 and actual SMOKE-001 Compose execution with recorded runtime status or Could not run (A7–A10).
- **FR-020**: No new dependencies, projects (including Worker resurrection), layers, Core ports, tables, public routes, DTO field sets, statuses, queue topology, message schema, or frontend implementation; sealed Infrastructure gate has no interface/port (A1, A5–A10).
- **FR-021**: Implementation acceptance MUST include shipping-runtime verification that `a.Equals(b, OrdinalIgnoreCase)` equals `(a.ToUpperInvariant() == b.ToUpperInvariant())` across Unicode including supplementary characters, Turkish dotted/dotless i, sharp-s, ligatures, and combining forms (FsCheck property over Unicode strings on the shipping runtime). Probe samples are evidence, not proof; any mismatch blocks acceptance, stops before T006+, returns to Patron after Wisp counterexample/runtime recon, and requires brief/CONCLUSIONS/ADR/spec/plan/tasks revision before continuation — never waive; do not claim expected sharp-s/ligature divergence contrary to recon-DEV-459-2 §9.2 (A8–A10; recon-DEV-459-2 §9.2).
- **FR-022**: Duplicate imports MUST route `23505` from both `IX_movies_library_path` and `IX_movies_library_path_key` through the existing duplicate-import outcome path, preserving the winner ID under sequential and concurrent single/bulk imports; endpoint behaviour for both constraints MUST be asserted; advertised `ProducesProblem(409)` MUST NOT become a new status mapping; automated tests MUST assert `MovieDetailsResponse.Path` relative-value semantics (A5, A8–A10; recon-DEV-459-2 §9.5).

### Key Entities

- **Movie**: Existing catalog row; identity and pre-existing business columns preserved; `Path` / `library_path` becomes Canonical LibraryPath; `library_path_key` holds Option C derived equality key after activation.
- **Library root**: Active configured root for all runtime media reads.
- **Legacy root alias**: Optional configured or default Docker historical absolute prefix for backfill/messages only.
- **ThumbnailRequested**: Existing queue message carrying MovieId + FilePath; FilePath values transition from absolute to relative across migration.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After a successful upgrade gate, 100% of retained movie rows either remain valid canonical relative paths with valid keys/constraints or were rewritten/activated to that state; zero partial-commit catalogs remain serving traffic.
- **SC-002**: 100% of movie IDs present before a successful backfill are present afterward with pre-existing business columns unchanged (derived key intentionally populated).
- **SC-003**: Any preflight collision or unmapped/malformed/escaping/ambiguous row prevents startup processing and reports affected movie IDs with actionable reasons before CI unique-index creation.
- **SC-004**: Duplicate and case-only variant imports after migration map to existing duplicate outcomes via either unique index without creating a second row for the same portable identity.
- **SC-005**: New thumbnail publishes use relative paths; legacy absolute publishes that map through configured aliases still process when the movie path matches; unauthorized/mismatched cases never open source media.
- **SC-006**: Movie details path display/copy shows a library-relative value for migrated titles (SMOKE-001). Planned criteria alone are insufficient — observed runtime Compose evidence with exact status is required; Could not run remains unresolved acceptance.
- **SC-007**: Applicable harness verification gates report honest PASS/failure/SKIPPED/SKIP/N/A/Could not run; inapplicable web gates are not labeled PASS; review closes under the brief three-round bar with zero open in-scope findings or returns outstanding state to Bernstein.

## Assumptions

- Taste/diagnostic naming assumptions already logged in `specs/DEV-459/ASSUMPTIONS.md` (`Library:LegacyRootPaths`; actionable diagnostic copy conventions).
- DEV-438 LibraryPath vocabulary and unique index retention remain authoritative companions for this migration.
- Personal-library scale; coordinated downtime for stop-then-upgrade is accepted (A6).
- No Gherkin/acceptance-test opt-in requested for this ticket.
- Worker vocabulary means hosted consumers inside `LamuFlix.Api` (A8; CONTEXT.md; recon-DEV-459-2 §9.8); `specs/PRODUCT.md` `LamuFlix.Work` wording is stale against that glossary.
- Existing test conventions remain: AutoFixture, Faker.Net, and MockFileSystem for scanner/discovery unit suites (constitution Principle IX); no new testing libraries.
- Exact no-hand-edit EF tooling sequence is Patron A10 / recon-DEV-459-3 §10 F5; no Quill invention and no drafting-time tooling execution.
