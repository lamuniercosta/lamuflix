# Feature Specification: PostgreSQL persistence via EF Core

**Feature Branch**: `feature/19-spec`

**Created**: 2026-09-30

**Status**: Draft (Gate 1 closed until the Q7 owner checkbox is answered)

**Input**: DEV-19 — "Implement Postgres persistence via EF Core, entity configurations, and fresh Initial migration". Rulings: `CONCLUSIONS.md` Q1-Q10; assumed names: `ASSUMPTIONS.md`; alignment: `brief.md`.

## Owner prerequisite (Q7) — blocks Gate 1 and implementation

This spec describes the Q7 option A scope and nothing else. The spec PR carries this checkbox for the owner:

> - [ ] DEV-19 deletes LamuFlix.Data and removes LamuFlix.Web, LamuFlix.Worker, and the dependent legacy LamuFlix.Test project from the solution build. Legacy app/test files remain for replacement work (Web: DEV-388; Worker: DEV-314/315/316), and Tests.Common moves to Infrastructure. The legacy Web UI and legacy enrichment worker are non-functional from this merge until their replacements land; the excluded legacy test suite no longer runs in the solution.

If the owner declines, Gate 1 stays closed, Bernstein and Rigger reconcile sequencing (option D), and Keel reopens `brief.md` and revises this single spec/plan/tasks set. There is no second scope path.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fresh PostgreSQL schema from one migration (Priority: P1)

A developer or CI run points the solution at an empty PostgreSQL 16 database and applies the single `Initial` migration to get the complete application schema.

**Why this priority**: It is the ticket's headline acceptance criterion; every later persistence ticket (DEV-301/302/307/308) builds on this schema.

**Independent Test**: `MigrationTests` in `tests/LamuFlix.IntegrationTests` against a `postgres:16.4` Testcontainers database.

**Acceptance Scenarios**:

1. **Given** an empty `postgres:16.4` database, **When** `MigrateAsync` runs, **Then** the applied migration IDs are exactly the generated `Initial` ID and `HasPendingModelChanges()` is false.
2. **Given** the migrated database, **When** `information_schema` and `pg_indexes` are queried, **Then** seven application tables exist (`movies`, `actors`, `directors`, `genres`, `movie_actors`, `movie_directors`, `movie_genres`) with snake_case columns and the FR-005/FR-005a nullable set, each join table has a composite primary key on its two FK columns, and EF's history table is counted separately.
3. **Given** the migrated database, **When** indexes are queried, **Then** `title`, `release_year`, `status`, unique `library_path`, and unique `imdb_id WHERE imdb_id IS NOT NULL` exist on `movies`.

---

### User Story 2 - Movie records round-trip through real Postgres (Priority: P1)

A `MovieRecord` with Actor, Director and Genre links is saved and read back with a new context, including the all-null metadata case.

**Why this priority**: Proves the configurations, converters and skip navigations behave against real infrastructure, not just the model snapshot.

**Independent Test**: `PersistenceRoundTripTests` in `tests/LamuFlix.IntegrationTests`.

**Acceptance Scenarios**:

1. **Given** a `MovieRecord` with linked actors, directors and genres, **When** it is saved and read back with a new `LamuFlixDbContext`, **Then** all three relationship sets and the scalar fields match.
2. **Given** a `MovieRecord` with every metadata column null, **When** it is saved and read back, **Then** each metadata value is null.

---

### User Story 3 - Value-object and enum converters validate on read (Priority: P2)

Every persisted value object, `EnrichmentStatus` and `EnrichmentFailureCategory` converts to and from its column type through the domain's validating factories.

**Why this priority**: Prevents invalid database content from silently entering the domain.

**Independent Test**: Converter unit tests in `tests/LamuFlix.UnitTests/Persistence/` (Docker-free).

**Acceptance Scenarios**:

1. **Given** a valid column value, **When** a converter reads it, **Then** the corresponding domain value is produced.
2. **Given** an invalid non-null value or an unknown failure-category code, **When** a converter reads it, **Then** it throws (it never returns null or bypasses validation).
3. **Given** a status int of 0-3, **When** converted, **Then** it maps to Pending, Enriched, NotFound, Failed; any other int is rejected.
4. **Given** null, **When** converted, **Then** null is preserved.

---

### User Story 4 - No MySQL and no hard-coded credentials remain (Priority: P2)

The solution build contains no Pomelo package, `UseMySql` call, MySQL connection string or MySQL migration, and the design-time factory never falls back to a literal connection.

**Why this priority**: Ticket AC and the secrets rule.

**Independent Test**: Repository search over projects the solution builds; factory unit test.

**Acceptance Scenarios**:

1. **Given** the built solution, **When** searched, **Then** no Pomelo reference, `UseMySql`, MySQL connection string or MySQL migration exists, and `password=root` is gone.
2. **Given** `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` is missing or blank, **When** the design-time factory runs, **Then** it fails with a clear message that never contains the variable's value.

---

### User Story 5 - ADR-0003 records the decision (Priority: P3)

`docs/adr/ADR-0003.md` is in the spec PR as Proposed; the owner's merge makes it Accepted. No agent changes the status label.

**Independent Test**: Review of the file and its status line.

**Acceptance Scenarios**:

1. **Given** the spec PR, **When** ADR-0003 is read, **Then** it states PostgreSQL 16+, the single fresh `Initial` migration and no MySQL data migration, with status Proposed.

### Edge Cases

- A movie with only some metadata columns set (for example a metadata title only) must round-trip without collapsing into "absent metadata"; all-null scalar metadata represents absence, and non-empty relationship sets are never discarded because scalar columns are null.
- An unknown `enrichment_failure_category` code in the database must fail materialisation, not become null.
- A blank or whitespace design-time connection variable is treated as missing.
- `enriched_at` and `last_attempt_at` carry UTC (offset zero); Npgsql rejects a non-zero offset for `timestamptz`. Tests use UTC values; normalisation on write is the DEV-301 repository's job.
- A second `imdb_id = NULL` row must not violate the unique `imdb_id` index (the index is partial).
- A model change after `Initial` is generated must be fixed by regenerating the whole migration set, never by hand edits.
- The owner declines Q7: nothing in this spec is implemented (see Owner prerequisite).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Infrastructure MUST map persistence records `MovieRecord`, `ActorRecord`, `DirectorRecord`, `GenreRecord` (Q1); the Core `Movie` aggregate MUST NOT be mapped by EF.
- **FR-002**: `LamuFlixDbContext` MUST take `DbContextOptions<LamuFlixDbContext>`, apply configurations via `ApplyConfigurationsFromAssembly`, and have no `OnConfiguring` (Q3, Q8).
- **FR-003**: `LamuFlixDesignTimeDbContextFactory` MUST read only `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING`, fail clearly when missing or blank, never echo the value, and have no literal fallback (Q3).
- **FR-004**: Tables and columns MUST be explicit lowercase snake_case: `movies`, `actors`, `directors`, `genres`, `movie_actors`, `movie_directors`, `movie_genres`; join tables come from skip navigations via `UsingEntity`, with no join CLR classes and no naming-convention package (Q2).
- **FR-005**: `movies` MUST hold flattened nullable metadata: `metadata_title` text, `runtime_minutes` int, `release_year` int, `imdb_rating` numeric(3,1), `imdb_id` text, `rotten_tomatoes_rating` smallint, `meta_score` smallint, `plot` text (Synopsis), `poster_url` text (Q4, Q5). `rotten_tomatoes_rating`, `meta_score` and `poster_url` are persistence-only scalars with no Core counterpart; no Core change.
- **FR-005a**: `movies` MUST also hold the Core `Movie` scalars (brief P2): `id` integer identity primary key; `title` text not null; `library_path` text not null (`LibraryPath` converter, unique); `format` text not null (`MediaFormat` converter storing `Extension`); `is_in_watchlist` boolean not null; `status` integer not null; `enriched_at` timestamptz null; `enrichment_attempts` integer not null; `enrichment_failure_category` varchar(32) null; `last_attempt_at` timestamptz null. The AC1 nullable set is the nine FR-005 columns plus `enriched_at`, `enrichment_failure_category` and `last_attempt_at`; every other `movies` column is not null.
- **FR-006**: `EnrichmentStatus` MUST be stored as int through an explicit `Value` converter (Pending=0, Enriched=1, NotFound=2, Failed=3, never renumbered); `EnrichmentFailureCategory` MUST be stored as its `Code` in nullable `varchar(32)`, resolving the known singleton and rejecting unknown codes. No `MapEnum`, no native enum, no Core change (Q4).
- **FR-007**: Converters MUST read through validating domain factories via a shared helper that throws when `TryCreate` fails; null stays null (Q4).
- **FR-008**: `movies` MUST have indexes on `title`, `release_year`, `status`; unique on `library_path`; unique on `imdb_id` filtered `IS NOT NULL`.
- **FR-009**: Actor, Director and Genre records use an `int` identity key and a required `name` text column, with no extra indexes (plan decision). `MovieRecord.Id` uses the `MovieId` converter with `ValueGeneratedOnAdd`: null before insert, store-assigned identity after `SaveChanges` (brief P2/P3).
- **FR-010**: Package pins: `Microsoft.EntityFrameworkCore` and `.Design` 10.0.12 (Design `PrivateAssets=all`), `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, local `dotnet-ef` 10.0.12; every Microsoft EF package aligned at 10.0.12; Pomelo removed (Q6).
- **FR-011**: Exactly one migration, `Initial`, in `src/LamuFlix.Infrastructure/Persistence/Migrations/` (three files), generated with `dotnet ef migrations add Initial --project src/LamuFlix.Infrastructure --startup-project src/LamuFlix.Infrastructure --output-dir Persistence/Migrations`; no hand edits, no `HasData`, no raw SQL (Q8).
- **FR-012**: `LamuFlix.Data` MUST be deleted; `LamuFlix.Web`, `LamuFlix.Worker` and `tests/LamuFlix.Test` MUST be removed from the solution build with only their Data `ProjectReference` edited and files kept on disk; `Tests.Common` moves to Infrastructure (Q7 A, pending owner). `LamuFlix.Worker.csproj` keeps its Pomelo `PackageReference` on disk, unpinned and unbuilt, for DEV-314/315/316; FR-012 allows only the Data `ProjectReference` edit, and AC2 (SC-002) is scoped to projects the solution builds.
- **FR-013**: `Tests.Common` MUST provide a Postgres-only `postgres:16.4` fixture (`PostgresFixture`, an xUnit v3 assembly fixture that alone starts and disposes one container per test assembly; no RabbitMQ, no `EnsureCreated`; each test gets a fresh, uniquely named database, per brief P4) and a context factory using explicit container-derived options; `ContainerFixture.cs` is left as is (Q9).
- **FR-014**: ADR-0003 MUST ship in the spec PR as Proposed (Q10). It already exists at `docs/adr/ADR-0003.md`.
- **FR-015**: Api and Worker MUST receive zero code, DI, config-key or user-secrets changes; no `IMovieRepository`/`IMovieCatalog` implementation and no atomic claim (DEV-301, DEV-302, DEV-307, DEV-308, DEV-314/315/316, DEV-388).

### Key Entities

- **MovieRecord**: persistence record for a movie; scalar columns plus skip navigations to Actor, Director, Genre records.
- **ActorRecord / DirectorRecord / GenreRecord**: `int` identity key, required `name`.
- **Join tables** (`movie_actors`, `movie_directors`, `movie_genres`): implicit, FK columns `movie_id` and `actor_id` / `director_id` / `genre_id`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001 (AC1)**: `MigrationTests` pass against `postgres:16.4`: single `Initial` ID applied, no pending model changes, seven tables, snake_case columns, FR-005/FR-005a nullable set, join-table composite keys, five named indexes.
- **SC-002 (AC2)**: zero Pomelo, `UseMySql`, MySQL connection string or MySQL migration in any built project; `password=root` absent.
- **SC-003 (AC3)**: ADR-0003 present and Proposed in the spec PR; Accepted only by owner merge.
- **SC-004 (AC4)**: `PersistenceRoundTripTests` pass, read back with a new context, including all-null metadata.
- **SC-005 (AC5)**: converter unit tests (including `LibraryPath` and `MediaFormat`) cover valid, invalid non-null (throws), unknown category code (throws), status 0-3 accepted and others rejected, null preserved.
- **SC-006 (AC6)**: Roslyn, complexity (15; refactor pass at 6), InspectCode, `dotnet format --verify-no-changes`, full `dotnet test`, and mutation at the `harness.yml` threshold (80) on Infrastructure (Migrations excluded from mutate and IntegrationTests kept out of the per-mutant run, per brief P1), the vulnerable-package scan, and the property-test gate all pass on the changed `.cs` files; a skipped gate is not a pass.

## Assumptions

- Identifier aliases follow `ASSUMPTIONS.md` (Q2, Q5).
- Any correction to the generated migration regenerates the full set.
- If Roslyn or InspectCode flag generated migration files, `.editorconfig` marks `src/LamuFlix.Infrastructure/Persistence/Migrations/**` as `generated_code = true`; no per-line suppression.
- The mutation gate's Migrations exclusion and IntegrationTests handling are settled by T000 (recon, then a Patron ruling) before implementation starts (brief P1).
- Local full `dotnet test` requires Docker; a Docker outage is "Could not run", not a verdict.

## Out of Scope

- `IMovieRepository`/`IMovieCatalog` implementations and `TryClaimForEnrichmentAsync` (DEV-301, DEV-302).
- Api/Worker `AddDbContext`, connection-string keys, user-secrets (DEV-307, DEV-308).
- Core changes, including MetaScore/RottenTomatoes VOs (follow-up routed).
- Rewriting or stubbing legacy Web and Worker code.
- MySQL data migration.
- A new architecture rule.
