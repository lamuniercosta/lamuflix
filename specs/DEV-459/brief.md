# DEV-459 — Library-relative movie folders: grill brief

Status: PLAN FROZEN by Keel on 2026-11-07 after all 17 three-axis plan-challenge items were reconciled and final read-only analyze was CLEAN. Decisions A1–A12 govern; earlier drafting and blocker statements below are historical and superseded where A8–A12 and this freeze refine them. Ordinary user Gate 1, implementation, runtime acceptance and merge clearance remain pending.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-459-spec`  
Branch: `feature/459-spec`  
Intake base: `b9f9b5f31f7d9303ebdfbb731e33edaa66eb71bf`  
Claimed ADR: `docs/adr/0035-library-relative-movie-paths.md`  
Dependency: DEV-438. Facts: connected `recon-DEV-459`, sections 1–7. Authority: ticket scope quoted there, `specs/PRODUCT.md`, Patron rulings in `CONCLUSIONS.md`. No ticket or constitution escalation remains per A7.

## Frozen delivery scope and loop discipline

Deliver the ticket scope: backfill existing Windows and Docker paths, preserve movie IDs and uniqueness, resolve paths under the configured root, and cover import, rescan and queued thumbnail jobs across migration. A1–A7 below define the accepted design. Scope freezes after Gate 1 approval; anything else is a follow-up issue, not a finding in this round.

Closing bar: all required applicable verification gates PASS and zero open in-scope Critical, High, Medium or Low findings. Report SKIPPED (scope-empty), SKIP (opt-out), mutation N/A and Could not run separately and never as PASS. Could not run is unresolved verification, not clearance. Apply the harness eligibility rules; excluded/nonapplicable work does not become fabricated gate coverage. Maximum three review/remediation rounds, per Patron's standing-chain ruling confirmed in A7. A cap does not waive unresolved findings: return the outstanding state to Bernstein rather than silently extending or clearing the round.

No new dependencies, projects, top-level architectural layers, tables, public routes, DTO field sets, HTTP statuses, queue topology or message schema. No frontend implementation, LocalPlay flag or launcher modification. Playback gets regression coverage through its existing scanner seam. Patron approved bounded modification of existing Core/Infrastructure/API/Worker, test, config, documentation and EF migration files in A7. Never hand-edit generated files; generate required EF artifacts through tooling.

## Settled vocabulary

- **Library root:** active `Library:RootPath`, the filesystem location for all runtime media reads.
- **Canonical LibraryPath:** portable non-rooted movie-folder identity relative to that root, `/` separators, original segment casing, redundant dot segments removed, root represented by `.`. Reject parent traversal segments, drive-relative, malformed and outside-root paths. Persist and return this same value, including `MovieDetailsResponse.Path`.
- **Legacy root alias:** a recognized historical absolute Windows drive/UNC or POSIX root prefix used only for backfill and legacy thumbnail messages, never authorization of new imports outside the active root.
- **Portable uniqueness:** no two movie rows have equal whole-string `ToUpperInvariant()` keys under ordinal comparison, on any host, including Long S versus s/S collisions and distinct Garay keys as ruled in A12. Preserve the exact unique index as well (A11–A12).

These definitions belong in the feature specification. No root glossary rewrite is authorized by this brief; use established LibraryPath vocabulary from DEV-438 without changing unrelated domain terms.

## Confirmed decisions and implications

### A1 — Canonical identity and API compatibility

Stored `movies.library_path`, `Movie.Path` and returned details Path use canonical LibraryPath. New imports accept relative values or absolute values beneath the active root, canonicalized before uniqueness and persistence. Preserve API shape/status contracts; the displayed/copied Path value becomes relative. Basis: ticket scope, recon sections 2–4, PRODUCT care list 4. Patron also supplies the standing closing bar, scope freeze and three-round cap.

### A2 — Deterministic cross-platform backfill

Recognize active root automatically, configured legacy prefixes, and standard Docker alias as clarified in A6. Parse path syntax independent of the running OS. Windows drive/UNC prefix matching uses `OrdinalIgnoreCase` and equivalent slash/backslash separators; POSIX prefix matching uses `Ordinal`. Match complete path segments and preserve suffix casing. Already-relative values normalize idempotently. Never infer a root from basename or suffix, and never require media existence for migration. Unmatched, malformed, escaping or conflicting overlapping mappings fail preflight with movie IDs and actionable reasons. If multiple roots yield different relative identities, fail rather than preferring the longest prefix. Basis: ticket Windows/Docker backfill and ID/uniqueness preservation; recon sections 2 and 6.

### A3 — Atomic startup gate and portable collision protection

Both API and Worker must finish a configuration-aware startup gate before HTTP or RabbitMQ processing. No separate maintenance CLI. Acquire an exclusive database lock and preflight the entire table; commit only path updates within one transaction. Preserve IDs, every other column and `IX_movies_library_path`. Exact and case-only target collisions block on all hosts. Never merge/delete/suffix rows. Preflight or write failure rolls back and fails startup closed. An already-canonical valid table is an idempotent fast no-op; concurrent hosts serialize safely. Basis: ticket and recon persistence/consumer facts. Prove interruption/restart safety rather than relying on startup self-report.

### A4 — Old and new queued thumbnail payloads

Keep `ThumbnailRequested(MovieId, string FilePath)` and existing queue policy. Publish canonical relative paths. Canonicalize old absolute payloads through the root map, compare against the loaded movie's canonical path using whole-string `ToUpperInvariant()` ordinal key equality (A11–A12), and never authorize on ID alone. Resolve authorized folders under the active root, never CWD, preserving containment, reparse-point and existing eligibility checks. Unknown roots, escaping payloads, missing movies and mismatches take existing terminal `IneligibleRequest` / `UnauthorizedSourcePath` paths without source access or retry loops. Basis: ticket queue clause; recon section 4.

### A5 — Preserve invariant after migration

Enforce canonical case-insensitive uniqueness for future single/bulk imports race-safely in the database on movies while retaining the exact unique index. Application-only duplicate prechecks are insufficient. Use existing duplicate-import outcomes and preserve the existing ID. Import legacy aliases are not allowed; they only serve backfill/messages. All filesystem resolution uses active root. Rescan preserves canonical stored identity; playback regression uses the existing scanner. Basis: ticket, DEV-438 FR-003, PRODUCT care list 1–5 and recon sections 3–4.

### A6 — Options, deployment and recovery

Add optional `Library:LegacyRootPaths` on LibraryOptions, shared between API and Worker, as string-array/read-only-list configuration. Validate entries as absolute well-formed Windows drive/UNC or POSIX prefixes before backfill/message normalization. Automatically recognize `/media/library` for legacy backfill/messages only when unambiguous; never authorize new outside-root imports. Conflicting maps fail closed.

Require matching root/aliases across hosts, stop old API/Worker before upgrade, and prohibit mixed-version writers. Retain aliases through outstanding deliveries, retries and DLQ replays. Back up before upgrade; downgrade after successful backfill requires restoring that backup with matching old binaries, not automatic reversal. Document options, diagnostics, backup/recovery and alias retirement without an admin UI/CLI. Naming and diagnostic-copy conventions are logged `[assumed]` in ASSUMPTIONS.md as requested by Patron. Basis: ticket, A3–A5 and PRODUCT sections 2, 4 and 5.

### A7 — Acceptance and bounded file scope

Patron confirmed the acceptance matrix and task order below, bounded existing-file changes, standing loop terms, and no remaining escalation. Full questions and cited rulings are appended in CONCLUSIONS.md; the verbatim A1–A6 rulings are also included below so each grill answer travels with this brief.

## Implementation approach and planned files

Quill must turn these decisions into concrete tasks with acceptance traceability. Do not create speculative repository/mediator/service layers or pick a new library. Exact database mechanism must implement the A12 whole-string ToUpperInvariant ordinal key semantics, including Long S collisions, distinct Garay keys, and concurrent writes; do not silently equate SQL lower/collation semantics with that contract. If platform mechanics cannot meet a ruling, obtain recon and return a needs-decision question before drafting an exception.

Recon-bounded production surfaces:

- Core: `Domain/LibraryPath.cs`, `Options/LibraryOptions.cs`, import feature path handling as needed; existing scanner/port semantics remain the integration seam.
- Infrastructure: filesystem scanner/discovery, persistence configuration/repository and new tooling-generated EF migration artifacts for movies-only uniqueness, configuration-aware transactional backfill/startup composition, enrichment producer and ThumbnailConsumer.
- API/Worker: existing startup/composition surfaces to require the barrier before listeners or consumers start. API import boundary retains its route/DTO/status shape.
- Tests: existing domain/converter/property, discovery/scanner, import persistence/API, rescan, migration, thumbnail/enrichment consumer and playback scanner regression suites, plus focused additions alongside these suites.
- Configuration/documentation: relevant existing examples and operating instructions for roots/aliases, coordinated rollout and rollback; feature specs and ADR 0035.

This is a surface list from recon, not permission to rewrite unrelated files. Quill must name the exact host-composition files from recon evidence; missing facts go back to Wisp through Bernstein. No production edits occur during this grill.

## Task ordering

1. Canonical path rules, shared options and their validation; focused unit/property coverage.
2. Movies-only database uniqueness, whole-table preflight/backfill, exclusive lock/transaction and both-host startup barriers; real database proof before integrations depend on them.
3. Import/discovery/scanner/rescan canonical flows, current-root absolute compatibility, race-safe duplicate outcomes and playback scanner regressions.
4. Relative thumbnail publishing, legacy-message canonicalization, current-row identity checks and existing authorization/terminal handling.
5. Full acceptance/regression matrix and deployment/configuration/backup/recovery documentation.
6. Required harness verification, all three review axes for this L ticket, adjudication and bounded remediation before PR handoff. User owns merge.

## Acceptance and test strategy

| Scenario family | Required evidence |
| --- | --- |
| Cross-platform identity | Windows drive, UNC and POSIX roots parsed on either host; separators, nested paths, casing and `.`; idempotent relative normalization and root relocation |
| Safe preflight | Offline media migrates; malformed/unmapped/traversal/ambiguous roots fail; exact/case-only collisions fail with actionable IDs; full-row snapshots unchanged |
| Atomicity and startup | Real PostgreSQL/Testcontainers transaction rollback, interruption/restart and concurrent API/Worker gate serialization; no processing before success; canonical no-op |
| Persistence invariants | IDs and every non-path column unchanged, exact index retained, portable uniqueness enforced by database; non-ASCII and concurrent duplicate imports prove ruled comparison semantics |
| Import and rescan | Single/bulk relative and current-root absolute imports, existing duplicate outcomes, rejected legacy/outside-root imports; rescan preserves identity and resolves active root |
| Queue transition | New relative and old Windows/Docker absolute deliveries/retries; configured aliases after backfill; missing/mismatched movie/path and root containment/reparse rejection cause no source access or retry loop |
| Shared runtime resolution | Runtime reads use active root despite different process CWD; existing playback scanner behavior and flag boundary remain covered |
| Operations | Shared host config, default Docker alias/conflicts, stop-then-upgrade, backup-based downgrade and alias retirement instructions |

Use existing assertion/mocking libraries. Do not mock database driver translation/serialization, uniqueness or transaction/locking semantics. Use deterministic unit/property tests and real database integration tests; queue consumers use the repository's existing test approach. No hardcoded thresholds, suppressed survivor requirements or new testing libraries.

Run applicable Roslyn, complexity, InspectCode, property, vulnerable-package, full test and pre-PR mutation gates per harness configuration and documented commands. No web implementation is planned; record web/contract gates according to actual applicability, without manufacturing a PASS. Review and gates must be tied to the actual diff/head. A green design or backfill unit test is not runtime deployment acceptance.

## Evidence and next owner

Quill drafts spec.md, plan.md and tasks.md from this brief and proposed ADR. Keel then performs read-only speckit-analyze and brief-to-plan/tasks comparison; plan challenge/freeze and user Gate 1 remain pending. New facts require recon; decision changes must be recorded here before changing artifacts. No commits, pushes or merges are implied by grill closure.

## Full Patron answers A1–A6

### Verbatim A1

Verdict: Approved — adopt canonical library-relative identity and value semantics as recommended, with standing chain rulings cited once below.
- Stored and returned `LibraryPath` (`movies.library_path`, `Movie.Path`, `MovieDetailsResponse.Path`) is a non-rooted path relative to `Library:RootPath`, normalized to forward slashes (`/`), preserving segment casing, representing the library root itself as `.`, and rejecting `..` traversal or unrooted-outside-root/rooted persisted values (Basis: DEV-459 Scope 'Migrate stored movie folders to library-relative paths... preserve IDs and uniqueness, and resolve paths under the configured root'; recon-DEV-459 §2–§4).
- Public API routes, DTO field sets, and HTTP status codes remain unchanged; `POST /api/movies/import` accepts library-relative paths and compatible absolute paths under the configured `Library:RootPath`, canonicalizing to the relative `LibraryPath` before uniqueness checks and persistence (Basis: specs/PRODUCT.md §5 Care list #4; recon-DEV-459 §3, §6.4).
- Standing chain rulings cited once for DEV-459: closing bar (all verification gates green, zero Critical/High/Medium/Low open in-scope findings), frozen scope after Gate 1 approval, and review/remediation round cap (at most 3 rounds) (Basis: charter §2.3 standing rulings).

### Verbatim A2

Verdict: Approved — adopt explicit configured root prefix mapping with OS-independent syntax parsing, case-appropriate prefix matching, and fail-closed ambiguity handling.
- Backfill maps stored absolute paths using `Library:RootPath` (automatic, no extra entry needed) plus optional configured legacy root prefixes (plus standard default `/media/library` if configured/present), matching on path-segment boundaries without filesystem existence checks and normalizing already-relative paths idempotently (Basis: DEV-459 Scope 'Backfill existing Windows and Docker paths, preserve IDs and uniqueness, and resolve paths under the configured root'; recon-DEV-459 §2, §6.3).
- Parse path syntax OS-independently: Windows drive/UNC roots match case-insensitively (`OrdinalIgnoreCase`, treating `\` and `/` as equivalent separators) while preserving the relative suffix's original segment casing; POSIX `/...` roots match case-sensitively (`Ordinal`) (Basis: DEV-459 Scope covering cross-OS Windows and Docker paths; sound engineering practice).
- Never infer roots from basenames or suffixes; if a stored row matches no configured root, escapes the root (`..`), or matches multiple configured roots that yield different relative paths (conflicting/overlapping root prefixes), fail preflight closed with the movie ID and actionable reason rather than guessing (Basis: DEV-459 Scope 'preserve IDs and uniqueness'; sound engineering practice).

### Verbatim A3

Verdict: Approved — use an automatic configuration-aware startup gate mandatory on both API and Worker, with all-or-nothing transactional backfill and host-independent case-insensitive collision blocking.
- Execute the backfill as a configuration-aware startup gate (no separate maintenance CLI command) inside a single database transaction under an exclusive DB lock, updating only `movies.library_path` while preserving movie IDs, all other columns, and the unique `IX_movies_library_path` index; if any row fails preflight or collides, roll back completely and fail startup closed with actionable diagnostics (Basis: DEV-459 Scope 'Backfill existing Windows and Docker paths, preserve IDs and uniqueness, and resolve paths under the configured root'; recon-DEV-459 §2, §6.3).
- The startup gate/barrier is mandatory for both `LamuFlix.Api` and `LamuFlix.Work` so neither HTTP endpoints (import/rescan/details) nor RabbitMQ consumers (enrichment/thumbnail) process work until all stored paths are canonical; an already-canonical table is a fast idempotent no-op (Basis: DEV-459 Scope 'Cover import, rescan, and queued thumbnail jobs across the migration'; recon-DEV-459 §4).
- Preflight collision checks must block both exact (`Ordinal`) and case-only (`OrdinalIgnoreCase`) duplicate target relative paths conservatively on all hosts (Windows and Linux/Docker alike), never deleting, merging, or suffixing rows, so the library remains portable across Windows and Docker mounts (Basis: DEV-459 Scope covering Windows and Docker paths and uniqueness preservation; sound engineering practice).

### Verbatim A4

Verdict: Approved — keep the existing `ThumbnailRequested` contract and queue policy, canonicalizing incoming `FilePath` values and resolving under `Library:RootPath`.
- Retain `ThumbnailRequested(MovieId, string FilePath)` without changing message schema or queue topology; new messages publish the canonical library-relative `movie.Path.Value`, while in-flight pre-migration absolute `FilePath` payloads are canonicalized through the same configured root map (`Library:RootPath` plus legacy root aliases) before eligibility comparison (Basis: DEV-459 Scope 'Cover import, rescan, and queued thumbnail jobs across the migration'; recon-DEV-459 §4).
- Compare the canonicalized request path against the loaded movie's canonical `movie.Path.Value` using `OrdinalIgnoreCase` (consistent with the portable uniqueness rule in A3); a `MovieId` match alone never authorizes work (Basis: recon-DEV-459 §4 `ThumbnailConsumer.cs:295-299`; A3 portable uniqueness ruling).
- Resolve the authorized path against `Library:RootPath` (never process CWD), preserve root-containment and reparse-point checks, and route unknown roots, traversal/escaping payloads, missing movies, or path mismatches through the existing terminal failure paths (`IneligibleRequest` / `UnauthorizedSourcePath`) without opening source files or looping retries (Basis: recon-DEV-459 §4 `ThumbnailConsumer.cs:301-355`; sound engineering practice).

### Verbatim A5

Verdict: Approved — enforce race-safe case-insensitive relative-path uniqueness on `movies` for all future imports and preserve the bounded file, schema, and API scope.
- Apply `OrdinalIgnoreCase` canonical `LibraryPath` uniqueness to all single and bulk imports with race-safe database enforcement on the existing `movies` table while retaining `IX_movies_library_path` (no new tables, no new dependencies, no API shape changes), mapping duplicates and case-only variants to existing duplicate-import outcomes and preserving existing movie IDs (Basis: DEV-459 Scope 'preserve IDs and uniqueness... Cover import, rescan'; specs/DEV-438 FR-003; specs/PRODUCT.md §5 Care list #1–#4).
- New imports accept only canonicalizable relative paths or absolute paths under the active `Library:RootPath` (legacy root aliases are restricted to backfill and queued thumbnail compatibility); canonicalize `/` separators and redundant `.` segments, represent the root folder as `.`, reject `..` traversal, drive-relative paths, and outside-root paths, and resolve all filesystem reads against `Library:RootPath` rather than process CWD (Basis: DEV-459 Scope 'resolve paths under the configured root'; recon-DEV-459 §3–§4).
- Rescan resolves stored relative `movie.Path` values under `Library:RootPath` without mutating stored identity, and playback is verified by regression tests through the existing scanner seam without modifying `Features:LocalPlay` or `ProcessMediaPlayerLauncher` (Basis: DEV-459 Scope; specs/PRODUCT.md §5 Care list #5; recon-DEV-459 §4, §6.5).

### Verbatim A6

Verdict: Approved — adopt `Library:LegacyRootPaths` on `LibraryOptions` and the proposed default Docker alias, rollout, and backup-restore downgrade rules.
- Add optional `Library:LegacyRootPaths` (`string[]` / `IReadOnlyList<string>`) to `LibraryOptions` shared by API and Worker, validating entries as well-formed absolute Windows drive/UNC or POSIX root prefixes before backfill or queue normalization, and recognizing `/media/library` automatically for backfill and legacy thumbnail payloads when no conflict exists while never allowing new imports outside `Library:RootPath` (Basis: DEV-459 Scope 'Backfill existing Windows and Docker paths... Cover import, rescan, and queued thumbnail jobs'; recon-DEV-459 §2 `LibraryOptions.cs:8-11`, `docker/api.settings.example.json:26`).
- Require coordinated stop-then-upgrade deployment with matching `Library:RootPath` and `Library:LegacyRootPaths` across API and Worker hosts (no mixed-version writers), retain configured aliases until legacy queue deliveries/retries/DLQ replays are drained, and treat rollback after backfill as a pre-upgrade DB backup restore rather than auto-reversing relative paths (Basis: A3–A5 rulings; sound engineering practice).
- Keep scope bounded to configuration and documentation of preflight diagnostics, backup/recovery, and alias retirement with no admin UI or maintenance CLI; log the `Library:LegacyRootPaths` option name and diagnostic copy conventions as `[assumed]` in `specs/DEV-459/ASSUMPTIONS.md` (Basis: specs/PRODUCT.md §2, §4, §5 Care list #2).

### Verbatim A7

DEV-459 A7/12 confirmed: acceptance coverage, bounded Core/Infrastructure/API/Worker/test/config/doc/EF-migration scope (Basis: DEV-459 Scope, AGENTS.md Non-negotiables, PRODUCT.md §5 Care list #6), and A1 standing loop terms are shared understanding with no ticket or constitution escalation; proceed to record A7 in specs/DEV-459/CONCLUSIONS.md, write specs/DEV-459/brief.md and ADR 0035, and close the grill.

## A8 — Post-drafting mechanism and composition decision (2026-11-07)

**Keel question:** Approve Option C with trusted derivation, Unicode equivalence acceptance gate, single API-host barrier, atomic activation and unchanged duplicate outcomes, rather than a SQL Unicode port?

**Full Patron answer:**

# DEV-459 A8 — Patron ruling

Verdict: Approved — adopt Option C (.NET-derived key) with the explicit trusted-writer-for-derivation-only boundary; A and B rejected.

- Mechanism and trust boundary: Add movies.library_path_key derived by whole-string string.ToUpperInvariant() (never per-char char.ToUpperInvariant folding, which recon-DEV-459-2 section 9.2 proves wrong on supplementary Deseret characters), non-null after activation, with binary deterministic unique index IX_movies_library_path_key, retaining IX_movies_library_path and the original library_path casing. The database index enforces uniqueness and concurrency (second case-only writer blocks then fails 23505, recon section 9.3/9.7); the database does not validate key derivation, and no claim may state otherwise. Derivation is trusted-writer only: every supported EF write and the configuration-aware backfill must derive and update library_path and library_path_key together, mixed-version and raw writers bypassing derivation remain prohibited, and startup must verify existing key consistency under the shipping runtime and fail closed on drift. This is within the already-approved movies-only schema scope of A5/A7 and the single-writer deployment of A6; A/B are rejected because they require a hand-ported SQL Unicode fold and its maintenance/proof burden with no ticket need (recon sections 9.4, 9.3).
- Equivalence gate: Implementation acceptance is contingent on shipping-runtime verification that a.Equals(b, OrdinalIgnoreCase) equals (a.ToUpperInvariant() == b.ToUpperInvariant()) across Unicode, including supplementary characters, Turkish dotted/dotless i, sharp-s, ligatures and combining forms (for example an FsCheck property over Unicode strings on the shipping runtime, recon section 9.2). Probe samples are evidence, not proof; any mismatch blocks implementation acceptance and returns to Patron.
- Worker, sequencing and contracts: Worker means the hosted consumers inside LamuFlix.Api, not a new or resurrected project (CONTEXT.md:86-88; recon section 9.8 — the LamuFlix.Work wording in specs/PRODUCT.md:10 is stale against that glossary, and A3/A6 both-hosts language is satisfied by this single composition root). The await gate sits in src/LamuFlix.Api/Program.cs after Build and before Run, covering all hosted consumers and listeners. Require preflight (with actionable duplicate/movie-ID diagnostics) before creating the case-insensitive unique index, and atomic locked backfill plus key population plus constraint activation, covering fresh and legacy schemas and rollback/restart; use tooling-generated EF migrations only and no concurrent index build. Duplicate outcomes and status contracts remain unchanged: handle the new IX_movies_library_path_key 23505 on the existing duplicate-import outcome path while preserving existing IDs, and do not introduce a 409 mapping from the advertised ProducesProblem metadata (recon sections 9.5-9.6).

**Effect on A1–A7:** This clarification supersedes literal two-project/two-host wording: Worker is hosted in Api. Existing business columns remain unchanged; the new derived key is intentionally populated/validated alongside path. A canonical path table is a no-write fast path only after keys and constraints are valid. Before activation, key population and constraint installation are required even when paths are already canonical. No owner checkbox is introduced. This records a decision before Quill repairs drafting artifacts; it does not freeze the plan or approve Gate 1.



---

## A9 — Plan-challenge decisions (Ledger H1 H2 M1 M2 M3 L1; Sentry F1–F3)

**Patron ruling:** Approved as specified below; no constitution departure and no ticket contradiction found, so no owner checkbox. Compass's Unicode failure prediction is rejected: recon-DEV-459-2 section 9.2 rows 23–38 show the cited pairs unequal under both OrdinalIgnoreCase and whole-string ToUpperInvariant; the A8 shipping-runtime property gate stands, any mismatch returns to Patron, and no expected-divergence claim may be written.

- **One EF choke point, Core key-free (H1):** Derivation lives only in Infrastructure at the single `LamuFlixDbContext` save choke point: override both sync and async SaveChanges paths through one shared private derivation pass over tracked `MovieRecord` entries in Added/Modified state, setting `LibraryPathKey` from the whole-string `LibraryPath.Value.ToUpperInvariant()` before save. No per-writer derivation in `EfMovieRepository` (write sites at EfMovieRepository.cs:92, :567; SaveChanges at :201/:411/:502), no derivation in `LibraryPathConverter`, and Core stays key-free (`LibraryPath` and Core learn nothing of the key; the key property exists only on the Infrastructure `MovieRecord`). Tests must mutate the path through every supported writer and assert the key follows (Basis: findings-DEV-459-Ledger H1; A8 trusted-writer boundary; constitution Principle I).
- **Gate type, DDL owner and snapshot strategy (M1, H2, Sentry F1/F3):** The gate is one concrete sealed class `MovieLibraryPathMigrationGate` in `src/LamuFlix.Infrastructure/Persistence/` (beside `LamuFlixDbContext`), with no interface and no Core port, invoked from `Program.cs` after Build before Run; add an ArchitectureTests assertion for the no-new-port rule. Activation DDL is owned by that gate, not by a second migration: the sole tool-generated EF migration is additive only (nullable `library_path_key`, no unique index, no NOT NULL). The EF model and model snapshot declare the final state from the outset (required key, `IX_movies_library_path_key` unique, explicit collation below), so there is no model-vs-snapshot pending-changes drift; the gate executes the activation DDL (unique index + SET NOT NULL) inside its transaction and a post-gate schema assertion proves the database matches the final model. A database that has only run Migrate is explicitly schema-not-ready, never a supported steady state. Ordering is fixed: additive Migrate first, then the gate (preflight, backfill, activation), then Run — for both fresh and legacy schemas. When `DatabaseOptions.ApplyMigrationsOnStartup` is false (Program.cs:58–61), the gate performs no migration and no activation: it verifies the final activated state and fails closed with a schema-not-ready diagnostic if the column, NOT NULL or unique index is absent (Basis: findings-DEV-459-Ledger H2/M1; findings-DEV-459-Sentry F1/F3; recon-DEV-459-2 section 9.6).
- **Lock, transaction, collation (M2):** The gate opens one database transaction on its DbContext, takes `LOCK TABLE movies IN ACCESS EXCLUSIVE MODE` (not an advisory lock) as the first statement, and runs preflight, path+key backfill, activation DDL and consistency verification inside that single transaction; CREATE INDEX is non-concurrent and inside the transaction, and any failure rolls the whole transaction back. The key index uses an explicit deterministic binary collation `C` declared in `MovieConfiguration` (via the key property/index configuration) and in the gate's activation DDL, never the cluster default; T006/T010 must assert the effective collation on real PostgreSQL (Basis: findings-DEV-459-Ledger M2; recon-DEV-459-2 sections 9.3, 9.6).
- **Options validation (L1):** `LegacyRootPaths` semantic validation (well-formed absolute Windows drive/UNC or POSIX prefixes, conflict detection input) uses a new `IValidateOptions<LibraryOptions>` implementation `LibraryOptionsValidator` in `src/LamuFlix.Core/Options/` beside `EnrichmentOptionsValidator`/`ThumbnailOptionsValidator`, registered as a singleton `IValidateOptions<LibraryOptions>` in `AddLamuFlixOptions` (ServiceDefaults Extensions.cs) alongside the existing validators, retaining the existing `ValidateDataAnnotations()` + `ValidateOnStart()` binding for `LibraryOptions`. Options validation must complete before any gate database write: the gate resolves validated `LibraryOptions` first, and a validation failure aborts startup before the gate transaction opens (Basis: findings-DEV-459-Ledger L1; constitution Principle VII as cited there).
- **Runtime/globalization compatibility and upgrade hard stop (M3, Sentry F2):** Persisted keys are runtime-bound: every Api instance opening the same database must run the same .NET runtime version, the same globalization mode (`InvariantGlobalization` on/off) and the same ICU/Unicode data as the instance that persisted the keys, alongside the already-required matching `Library:RootPath`/`Library:LegacyRootPaths`. T022 operations documentation must make this a hard pre-upgrade stop, not advisory copy: stop all instances, verify application version, runtime/globalization/ICU and root/alias configuration match, take the A6 backup, then upgrade; no new pre-activation technical interlock is required, because post-activation NOT NULL rejects old-writer inserts and the startup consistency check fails closed on drift, which are the named backstops. The startup consistency check is a backstop, never a license to mix runtimes (Basis: findings-DEV-459-Ledger M3; findings-DEV-459-Sentry F2; A6 stop-then-upgrade).

**Also affirmed without new ruling:** Ledger M4 (Core canonicalisation is pure string parsing, no `System.IO.Path`, resolution under the root stays in Infrastructure) is a plan constraint Quill applies from constitution Principle I; Ledger L2/L3 are task-mechanics repairs for Quill, not Patron decisions. Missing technical facts, if any emerge in repair, route via Bernstein/Wisp as needs-recon; Quill does not infer them.


**Keel recording / unresolved fact:** A9 is the approved design direction, recorded before drafting repairs. The existing recon does not identify the exact EF tooling sequence that produces the sole additive-only nullable/no-index migration together with the final required/unique model snapshot without hand-editing generated artifacts. Obtain that fact through Bernstein/Wisp before Quill finalizes T007 or any freeze. If unavailable, return the mechanism to Patron; do not silently change A9 or grant a generated-file exception. Plan remains not frozen; Gate 1 remains pending.



---

## A10 — EF tooling composition for A9 (recon-DEV-459-3 section 10)

**Patron ruling:** Sanctioned exactly as proven in recon-DEV-459-3 section 10; A9 stands with the two refinements below. This is task mechanics inside the already-approved movies-only migration scope, not a ticket or constitution departure, so no owner checkbox (Basis: A5/A7/A9; charter section 2.3 items 2–3 — no new project, layer or table; AGENTS.md non-negotiable: never edit a generated file).

- **Two-scaffold composition is the approved sequence (F5–F7):** Phase A scaffolds `AddLibraryPathKey` from the interim nullable, no-index model into `Persistence/Migrations`; Phase B changes the source model to final (required key, property-level `UseCollation(C)`, unique `IX_movies_library_path_key`) and scaffolds `LibraryPathKeyActivation` with `--output-dir` set to an absolute directory outside the worktree (under TEMP), letting tooling rewrite the in-repo model snapshot to final in its existing location. Only the additive migration ships; the external activation migration and designer are disposable scratch, never committed, archived or hand-edited, and no in-repo generated artifact is hand-edited, suppressed or deleted. Acceptance must show `has-pending-model-changes` exit 0, live PG17 Migrate applying only the additive migration into the schema-not-ready state, and an unrelated later scaffold emitting no activation operations (Basis: recon-DEV-459-3 F1–F7, F10; Microsoft Learn mechanics cited there: one scaffold derives both operations and snapshot, and hand-editing the snapshot is disallowed).
- **A9 collation refinement (F9):** There is no index-level collation API in EF 10.0.14/Npgsql 10.0.3; the final model declares collation by property-level `UseCollation(C)` on `LibraryPathKey`, and the gate activation DDL must explicitly set the `library_path_key` column collation to `C` together with backfill, SET NOT NULL and CREATE UNIQUE INDEX — merely relying on index comparison is insufficient. The post-gate schema assertion and T006/T010 real-PostgreSQL checks must verify the effective column collation `C` (Basis: recon-DEV-459-3 F6, F9).
- **Maintenance recovery obligation (F8):** The F8 fragility is accepted with a recorded recovery duty, not suppression: if `migrations remove` is ever run on the immediate successor of `AddLibraryPathKey`, tooling restores the interim predecessor designer snapshot, so the operator must redo the Phase B final-model external scaffold, then verify `has-pending-model-changes` exits 0 and inspect the next scaffold's operations to confirm no AlterColumn/CreateIndex activation is re-emitted, before applying anything. This obligation must be written into tasks and the operations/migration documentation; `ConfigureWarnings` suppression and manual deletion of in-repo generated artifacts remain prohibited (Basis: recon-DEV-459-3 F4, F8, F10).


**Effect on prior unresolved-fact recording:** A10 and recon-DEV-459-3 section 10 resolve the earlier A9 tooling blocker. The recorded block above is historical and superseded. Quill must carry the exact two-scaffold sequence, column-collation activation, proof obligations and F8 recovery into plan/tasks/spec/receipt and operating-doc tasks before final analysis/freeze. No mechanism choice remains delegated to Build. Gate 1 remains pending.


## Plan freeze receipt — 2026-11-07

PLAN FROZEN. Keel independently verified Quill consolidated repair round 2 against the brief, CONCLUSIONS A1–A10, recon-DEV-459 parts 1–3, and all three challenge reports. All 17 finding dispositions and final file/task references are recorded in adjudication-DEV-459-Keel in LamuFlix notes. No open Critical, High, Medium or Low plan-challenge finding remains; no owner structural checkbox is introduced.

Patron A9 (50d347ee) and A10 (3096f9a6) fix the Infrastructure derivation/gate/validation boundaries, startup/flag behavior, lock/transaction, runtime compatibility, exact two-scaffold composition, column collation C and F8 maintenance recovery. Quill applied final spec/plan/tasks/checklist/receipt repairs; Keel aligned proposed ADR 0035. The EF tooling recon blocker is resolved, not deferred to Build.

Final read-only speckit-analyze: prerequisite exit 0 with PYTHONUTF8=1; no analyze extension hooks; 22 FRs plus 7 buildable SCs all covered by 33 tasks T000–T032; zero unresolved findings, unmapped tasks or established constitution conflicts. Analysis and adjudication notes carry evidence; no production gate or runtime acceptance was executed by Keel. The Unicode property, PG17 migration/atomicity/uniqueness proofs and SMOKE-001 remain mandatory implementation/delivery work.

Freeze covers the current planning artifact content in this worktree, not a production-code review SHA. The ADR remains proposed and the spec/plan/tasks remain pending ordinary owner Gate 1. Next: Bernstein routes Patron provisional Gate 1 and Rigger spec-PR publication. User merges. T000 verified Gate 1 receipt must exist before T001+. No commit, push, merge or implementation authority is granted by this freeze.

## A11–A12 — Long S/Garay divergences and authoritative portable key equality (2026-11-07)

Basis: Patron rulings A11 and A12 in `CONCLUSIONS.md`; runtime and PostgreSQL facts in `recon-DEV-459-2` §§2–5 and runtime verification in `recon-DEV-459-3` §§2–5, §7, §9. A12 adopts Option A (retain the A8 Option C mechanism with ordinal key equality), superseding the A11 sole-Long-S exception claim. This refines the A3/A4/A5 comparison contract and A8/A9 equivalence gate; their original Q&A above is historical where superseded. Option C stands, with no new mechanism or schema scope.

Portable uniqueness is whole-string `ToUpperInvariant()` key equality under ordinal comparison, intentionally collating U+017F (`ſ`) with s/S. Startup preflight, in-memory import collision checks, and ThumbnailConsumer canonical-path comparison must use this same equality so they agree with `IX_movies_library_path_key` (column collation C), including Long S; Long S versus s/S collides, while the 22 corresponding Garay pairs U+10D50..U+10D65 versus U+10D70..U+10D85 remain distinct under the non-invariant shipping runtime. Windows/UNC root-prefix matching remains as ruled in A2; this refinement concerns canonical movie identity. Core remains key-free; the Infrastructure SaveChanges derivation and gate-backfill placement remain A9.

Shipping-runtime equivalence property: for strings outside the exact 24 verified pairing divergences on the non-invariant shipping runtime (2 Long S pairs: U+017F versus U+0073/U+0053; 22 Garay pairs: U+10D50..U+10D65 versus corresponding U+10D70..U+10D85), prove `a.Equals(b, StringComparison.OrdinalIgnoreCase) == string.Equals(a.ToUpperInvariant(), b.ToUpperInvariant(), StringComparison.Ordinal)` across Unicode, including supplementary/Deseret, Turkish dotted/dotless i, sharp-s, ligatures, Kelvin sign, Greek/Coptic, and combining forms. Pair it with deterministic `[Fact]` tests in `LibraryPathUpperInvariantEquivalenceTests` locking both recon-verified .NET 10.0.14 non-invariant behaviors: Long S maps to U+0053, so its whole-string uppercase ordinal key equals the s/S keys while `OrdinalIgnoreCase` is false for both pairs; for each of the 22 corresponding Garay pairs, `OrdinalIgnoreCase` is true while both code points remain unchanged by `ToUpperInvariant()` and their ordinal keys are unequal under Windows NLS and Linux ICU 74.2. Exclude only the ruled pairings from the equivalence assertion, never all strings containing Long S or Garay and never a hand-picked Unicode subset. Samples are evidence, not proof. T005 property and both deterministic-fact classes must be green before T006+; any additional mismatch stops acceptance and returns through Bernstein/Wisp recon to Patron for decision and artifact revision; never waive the gate (A12 Option A; recon-DEV-459-3 §§2–5, §9).

A12 Option A rejects a hand-picked Unicode subset (Option B), `OrdinalIgnoreCase` comparison of upper keys (Option C in the A12 alternatives, distinct from the retained A8 Option C storage mechanism), and switching to `InvariantGlobalization` (Option D). Upper-key `OrdinalIgnoreCase` would disagree with the PostgreSQL collation-C key index on Garay; Windows NLS and the production Linux ICU 74.2 image are both non-invariant and exhibit the same 24 divergences. The property and both deterministic-fact classes remain mandatory (CONCLUSIONS.md A12; recon-DEV-459-3 §§2–5, §7).

Task alignment: T005 revises the property and locks deterministic Long S and Garay facts; T006 proves Long S collision/duplicate outcomes and distinct Garay identities against real PostgreSQL; T008 uses key equality in preflight; T020 uses it for queue identity, with T018/T021 regression coverage. This artifact alignment records a decision, not a passing T005 run or permission to skip it.
