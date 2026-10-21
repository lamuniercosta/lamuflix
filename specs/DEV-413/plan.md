# Implementation Plan: DEV-413 movie_features storage and HNSW index

**Branch**: `feature/413-spec` | **Date**: 2026-10-21 | **Spec**: `specs/DEV-413/spec.md`

**Input**: Feature specification from `specs/DEV-413/spec.md`, settled grill brief `specs/DEV-413/brief.md`, facts from `recon-DEV-413` and `recon-DEV-413-2`, Patron rulings in `specs/DEV-413/CONCLUSIONS.md` (Q1–Q7).

**Note**: Planning artifact only (Phase 2 step 2). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1, and until Phase B entry evidence is complete. No `design.md` (no ui tag). Quill does not create `docs/adr/` or invent ADR body text; Keel drafts ADR 0024 before plan challenge, and Phase B publishes it Accepted after collision recon.

## Phase B entry preflight (executable before Code Phase 1)

Schedule these Build-workspace checks before T001. Do not run gates, tests, or analyzers from Spec. Missing evidence blocks code.

| Check | Owner / evidence | Blocks code if missing |
|---|---|---|
| Recorded Epic 7 (SPA) completion receipt (`specs/PRODUCT.md`: Epic 9 / recommendations-pgvector deferred until Epic 7 completes) | Named handoff / receipt recorded in `DEV-413`. Requires the receipt record only; does not assert current Epic 7 status. | Yes |
| Spec PR merged and Gate 1 established | Rigger / YouTrack `Ready to build` handoff in `DEV-413` | Yes |
| Phase B drift recon vs `origin/main` for the authorized file set, including ADR `0024` collision check on main | Wisp → `recon-DEV-413` (refresh); stop if `0024` is claimed | Yes |
| Constitution baseline still `1.3.0` (or recorded reconciliation target) | Wisp/Keel on pickup; stop rather than overwrite a changed baseline | Yes |
| Keel Accepted ADR draft ready for publication at settled path (or Phase A Proposed draft already assigned) | Keel handoff in `DEV-413` | Yes |
| Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef`) | Gauge → recorded in `DEV-413` | Yes |
| Keel drift check or amend against brief/spec before code | Keel handoff in `DEV-413` | Yes |

## Summary

Add additive `movie_features` persistence with `vector(384)`, raw-SQL HNSW cosine index, Core port `IMovieFeaturesRepository` + `MovieFeaturesSnapshot`, and Infrastructure EF adapter with atomic last-write-wins upsert. Publish Accepted ADR `0024-movie-features-storage-port.md`, amend constitution Principle I (`1.3.0` → `1.4.0`), and extend the architecture port whitelist—with no harness/gate/threshold edits—before the port implementation may merge. Prove schema, index, and repository behavior on the pinned pgvector Testcontainers image.

## Technical Context

**Language/Version**: C# / .NET 10 backend. No web change.

**Primary Dependencies**: Existing centrally pinned `Pgvector` 0.3.2, `Pgvector.EntityFrameworkCore` 0.3.0, `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, EF Core 10.0.13, `Testcontainers.PostgreSql` 4.15.0. No new NuGet/npm dependency.

**Storage**: PostgreSQL with preexisting `vector` extension. New table `movie_features` only. HNSW cosine index via raw SQL in additive migration `AddMovieFeatures`.

**Testing**: xUnit; existing assertion libraries; pinned `pgvector/pgvector:0.8.7-pg17` via `PostgresFixture`; FsCheck for finite-vector properties where scheduled. No mocking of EF/Npgsql query translation or serialization. No new test dependency. All gate/test execution belongs to Phase B.

**Target Platform**: Existing `LamuFlix.Api` persistence stack. No public HTTP surface.

**Project Type**: Existing ports-and-adapters solution. New Core port requires ADR (Q7). No new project or layer.

**Performance Goals**: None. Default HNSW parameters; no tuning, EXPLAIN, or latency SLA.

**Constraints**: TreatWarningsAsErrors. Ticket scope authoritative. Port implementation cannot merge ahead of Accepted ADR and constitution MINOR amendment. No LocalPlay/secrets/`Process.Start`. No public HTTP/OpenAPI/web. Existing recommendation `MovieFeatures` unchanged. Phase B verifies EF provider representation for avoiding duplicate index creation via authoritative docs (context7 / microsoft-learn), not guesswork.

**Scale/Scope**: Authorized new and existing files in brief "Files and boundaries" plus the Q7-authorized constitution edit. Unanticipated files return to Keel.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). Ticket DEV-413:21 and brief Q1–Q7 match. Owner structural checkboxes: none.
- Dependency care list (§2.3 #1): no new package (recon-DEV-413:86).
- No new project/folder/layer (§2.3 #2): files land in existing `Core/Ports` and `Infrastructure/Persistence/*` (recon-DEV-413:87). New port is a compliant constitution extension via ADR, not a §2.3b departure (Q7).
- Database schema (§2.3 #3): only ticket-named table `movie_features`; FK to `movies` without altering `movies` (Q3).
- No public API shape change (§2.3 #4): no routes/DTOs/status codes; no OpenAPI or generated TypeScript.
- No LocalPlay/secrets/`Process.Start` (§2.3 #5): excluded.
- File scope (§2.3 #6): additive new files plus Patron-approved existing-file edits listed in the brief, including ArchitectureTests whitelist and Phase B constitution amendment (Q4/Q7).
- Principle I ports: adding `IMovieFeaturesRepository` requires ADR (`constitution.md:139-140`, PR gate `:375`). Q7 settles filename `docs/adr/0024-movie-features-storage-port.md`, Phase B Accepted publication after collision recon, and MINOR amendment synchronizing `:136-140` (`1.3.0` → `1.4.0`) before merge.
- ArchitectureTests enforcement (`constitution.md:141`): whitelist edit only at `ArchitectureTests.cs:28-29`; enforcing test `:152-175`; no harness/threshold change (recon-DEV-413-2:30-41; Q7).
- Vector store row (`constitution.md:343`): pgvector + HNSW cosine; matches ticket.
- Tests and gates (constitution IX; Static-Analysis Gates): real Testcontainers proof; configured scripts; no threshold waiver; scope-empty `SKIPPED` never PASS (brief).

Post-design re-check: the design below adds the ruled port with ADR + constitution sync + whitelist; no public HTTP contract, glossary rewrite, new dependency, or harness edit. The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-413/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── DRAFTING_RECEIPT.md
├── adr-0024-movie-features-storage-port.md   # Phase A Proposed (Keel-authored); staged
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief already freezes approach, files, and order. There is no new public HTTP contract. No `design.md` (no ui tag). Quill does not invent ADR body text. Keel-authored Phase A Proposed ADR is staged at `specs/DEV-413/adr-0024-movie-features-storage-port.md` (Status Proposed). Canonical `docs/adr/0024-movie-features-storage-port.md` remains reserved for Phase B Accepted publication after TB002 collision recon (T001).

### Source Code (repository root)

```text
src/LamuFlix.Core/Ports/IMovieFeaturesRepository.cs
src/LamuFlix.Core/Ports/MovieFeaturesSnapshot.cs
src/LamuFlix.Infrastructure/Persistence/Records/MovieFeaturesRecord.cs
src/LamuFlix.Infrastructure/Persistence/Configurations/MovieFeaturesConfiguration.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieFeaturesRepository.cs
src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs
src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/<timestamp>_AddMovieFeatures.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/<timestamp>_AddMovieFeatures.Designer.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs

docs/adr/0024-movie-features-storage-port.md          # Phase B Accepted after collision recon; publish from staged Proposed (absent until T001)
.specify/memory/constitution.md                        # Phase B MINOR 1.3.0 → 1.4.0

tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs # PortWhitelistPattern only
tests/LamuFlix.IntegrationTests/MigrationTests.cs
tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs
tests/LamuFlix.IntegrationTests/PostgresFixtureTests.cs
tests/LamuFlix.IntegrationTests/EfMovieFeaturesRepositoryTests.cs
tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs
tests/LamuFlix.UnitTests/…                            # snapshot/upsert validation + FsCheck as scheduled
```

No `/web` edit. No `harness.yml` or gate-script edit.

**Structure Decision**: Existing Core Ports + Infrastructure Persistence only. Migration is EF-scaffolded additive `AddMovieFeatures` with raw-SQL HNSW in `Up`. DI registration is a one-line scoped add beside existing persistence ports.

## Design Overview

1. **ADR and constitution (Phase B, before port merge).** After pickup recon confirms `0024` free on `origin/main` and constitution baseline is still `1.3.0` (else stop), publish `docs/adr/0024-movie-features-storage-port.md` with Status Accepted (Keel-authored content). Amend Principle I port enumeration to insert `IMovieFeaturesRepository` immediately after `IPlaybackHistory`, retain BCL/M.E.AI abstractions and `Adding a port requires an ADR.`, bump Version `1.3.0` → `1.4.0`, update Sync Impact Report and Last Amended (actual date), evidence template review with no-change conclusion. Same build PR as the port.

2. **Core contract.** `MovieFeaturesSnapshot` enforces 384 finite elements via `EmbeddingDimensions.Value`. `IMovieFeaturesRepository` exposes nullable get-by-id and upsert with `CancellationToken`. Do not change recommendation `MovieFeatures`.

3. **Whitelist.** In the same coherent change as the new port, add `|IMovieFeaturesRepository` to `PortWhitelistPattern` (`ArchitectureTests.cs:28-29`). Do not touch anti-vacuity floor, harness, or thresholds.

4. **Entity and EF mapping.** `MovieFeaturesRecord` + `MovieFeaturesConfiguration`: table `movie_features`; `movie_id` PK with `MovieIdConverter` and `ValueGeneratedNever`; `vector(384)`; `computed_at` timestamptz; FK cascade to `MovieRecord`. Add `DbSet` on `LamuFlixDbContext`. Keep EF model free of a duplicate HNSW index definition that would scaffold beside the raw SQL (confirm provider support from docs in Phase B).

5. **Migration.** Scaffold `AddMovieFeatures` only (designer + snapshot). `Up`: create table, then `migrationBuilder.Sql` for `CREATE INDEX "IX_movie_features_vector" ON movie_features USING hnsw (vector vector_cosine_ops);`. `Down`: drop table (index goes with it); do not touch vector extension. Leave all seven prior migrations untouched.

6. **Repository.** `EfMovieFeaturesRepository`: parameterized `INSERT ... ON CONFLICT (movie_id) DO UPDATE SET vector = EXCLUDED.vector, computed_at = EXCLUDED.computed_at`; map `Pgvector.Vector` only inside Infrastructure; pre-SQL guards; propagate cancellation; no repository clock.

7. **DI.** `AddScoped<IMovieFeaturesRepository, EfMovieFeaturesRepository>()` in `PersistenceServiceCollectionExtensions`.

8. **Proof order (brief).** Phase B entry evidence → ADR Accepted + constitution amendment + Core port/snapshot + whitelist as one coherent foundation → entity/config/context/DI/migration with migration lifecycle/`pg_indexes` proof → repository + focused real-DB and validation tests + forced assertion edits → Cog refactor → Gauge gates. Author tests first; minimum compilable contracts when types are absent; behavioral failure before the behavior edit when possible; missing-type compile failures are not behavioral red. Fresh read contexts; microsecond-representable UTC timestamps.

## Loop discipline

- **Closing bar:** Verified Critical and High findings block. Medium blocks on a demonstrated ticket, constitution, or Q1–Q4 contract violation; record disposition otherwise. Low and optional improvements do not block. Unresolved mandatory gates and owner checkboxes prevent closure.
- **Round cap:** two per review loop, with at most two fix commits per remediation round. After two unresolved rounds, report the bounded blocker; do not add rounds silently or weaken the bar.
- **Frozen scope:** Q1–Q4 movie_features storage, its specific Core port/snapshot/adapter/DI, additive migration/raw HNSW cosine index, required ADR, focused real-database and validation tests, exact forced schema/architecture assertion maintenance, and the Q7 constitution MINOR amendment + whitelist-only edit. Anything else is a follow-up issue, not a finding in this round. Follow-ups via Rigger only for Critical/High, broken behavior, or user request; otherwise `noted, no ticket`.

## Complexity Tracking

> No constitution violations requiring justification. New port uses the constitution’s built-in extension mechanism (ADR + amendment). Package set unchanged. File placements and whitelist/constitution edits are Patron-ruled under ticket scope without owner checkboxes.
