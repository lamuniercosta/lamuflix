# Feature Specification: DEV-413 movie_features storage and HNSW index

**Feature Branch**: `feature/413-spec`

**Created**: 2026-10-21

**Status**: Draft

**Input**: Ticket DEV-413 (size M, parent DEV-288, no ui tag); settled grill brief `specs/DEV-413/brief.md` (grill closed Q1–Q7); facts from notes `recon-DEV-413` and `recon-DEV-413-2`; Patron rulings in `specs/DEV-413/CONCLUSIONS.md`; product authority in `specs/PRODUCT.md`. No decisions beyond the brief and those rulings. No `design.md` (no ui tag).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Additive movie_features schema with HNSW cosine index (Priority: P1)

An operator applies the additive migration on the pinned pgvector PostgreSQL image and gets a `movie_features` table with a required 384-dimension vector column, a required computation timestamp, a primary-key foreign key to movies with cascade delete, and an HNSW cosine index on the vector column. Rolling the migration back removes the table and its index while leaving the existing vector extension and prior movie data intact.

**Why this priority**: Ticket acceptance criteria require migration on the pinned image and `pg_indexes` proof of the HNSW index. Downstream embedding storage cannot land without this schema.

**Independent Test**: Against the existing pinned `pgvector/pgvector` Testcontainers fixture, apply migrations clean and upgrade from `AddManualImdbId`, assert exact column types/nullability/PK/FK/cascade, assert `pg_indexes` contains `IX_movie_features_vector` with `USING hnsw` and `vector_cosine_ops` on `vector`, assert `HasPendingModelChanges()` is false, and prove rollback/reapply retains the vector extension and existing movies rows.

**Acceptance Scenarios**:

1. **Given** an empty database on the pinned pgvector image, **When** all migrations apply, **Then** `movie_features` exists with required `movie_id` integer PK, required `vector` of type `vector(384)`, and required `computed_at` timestamptz; FK to `movies` cascades on delete; no other table is altered.
2. **Given** a database already at `AddManualImdbId`, **When** the additive `AddMovieFeatures` migration applies, **Then** the same schema and index appear, prior movies rows remain, and the applied migration set ends with `_AddMovieFeatures`.
3. **Given** a migrated database, **When** `pg_indexes` is queried for `movie_features`, **Then** index `IX_movie_features_vector` is present with definition containing `USING hnsw` and `vector_cosine_ops` on `vector`.
4. **Given** a migrated database, **When** model metadata is inspected, **Then** `HasPendingModelChanges()` is false and the EF snapshot does not create a duplicate scaffolded index alongside the raw-SQL HNSW index.
5. **Given** a migrated database with movies and optional movie_features rows, **When** the migration rolls back and reapplies, **Then** `movie_features` and its index are dropped on Down and recreated on Up, the preexisting vector extension remains, and prior movies rows remain.

---

### User Story 2 - Read-by-movie and atomic last-write-wins upsert (Priority: P1)

A future enrichment or recommendation caller stores one embedding snapshot per movie and reads it back by movie id. Writing the same movie twice keeps exactly one row and replaces both vector and computation time with the submitted pair. Concurrent writers keep one row and one coherent submitted pair; the last successful database write wins. Missing movies yield null on read; missing parents are rejected by the foreign key.

**Why this priority**: Ticket acceptance criteria require read-by-movie and upsert for DEV-414 and DEV-348, including the two-upsert single-row/ComputedAt update proof.

**Independent Test**: Against the pinned Testcontainers database with fresh read contexts, prove absent null, vector and UTC-representable timestamp round-trip, two sequential upserts with one updated row, two-context concurrent upserts with one coherent pair, missing-parent FK rejection, cascade delete with parent movie, and CancellationToken propagation. Do not mock query translation or serialization.

**Acceptance Scenarios**:

1. **Given** no row for a movie id, **When** `GetByMovieIdAsync` runs, **Then** the result is null.
2. **Given** a parent movie exists and a valid snapshot is upserted, **When** a fresh context reads by that movie id, **Then** the stored vector (384 finite elements) and `ComputedAt` match the submitted values at timestamptz precision.
3. **Given** an existing row for a movie id, **When** a second upsert submits a different vector and later `ComputedAt`, **Then** exactly one row remains and both columns reflect the second submission.
4. **Given** two concurrent upserts for the same movie id on separate contexts, **When** both complete, **Then** exactly one row remains and its `(Vector, ComputedAt)` pair equals one of the two complete submitted pairs (last successful database write wins; no freshness comparison).
5. **Given** a snapshot whose movie id has no parent movie, **When** upsert runs, **Then** the database foreign key rejects the write.
6. **Given** a parent movie with a features row, **When** the movie is deleted, **Then** the features row is removed by cascade.
7. **Given** a cancelled token, **When** get or upsert runs, **Then** cancellation is checked/propagated and no silent completion occurs.

---

### User Story 3 - Snapshot validation and forced assertion maintenance (Priority: P1)

Invalid snapshots never reach SQL. Callers that supply a null snapshot, a default vector, a wrong dimension, or non-finite elements are rejected before persistence. Existing tests that asserted “no vector column in the public schema” are updated to exact `movie_features.vector` checks so coverage is preserved. The architecture port whitelist includes the new port; the constitution closed port list is synchronized by the ruled MINOR amendment in Phase B before the port may merge.

**Why this priority**: Q2 and Q4 freeze pre-SQL guards and forced existing-file assertion edits; Q7 freezes ADR publication, constitution MINOR amendment, and whitelist-only architecture test edit with no harness/threshold change.

**Independent Test**: Unit/model/FsCheck tests reject invalid vectors and accept finite 384-vectors including zero. Integration/composition/fixture tests replace obsolete global no-vector assertions with precise `movie_features.vector` checks while preserving fixed historical migration-range assertions. ArchitectureTests whitelist matching and constitution/ADR consistency are verified in Phase B after collision recon.

**Acceptance Scenarios**:

1. **Given** a null snapshot, default vector, length other than 384, or any NaN/±Infinity element, **When** snapshot construction and/or upsert validation runs, **Then** an `ArgumentNullException` or `ArgumentException` is thrown before SQL.
2. **Given** a finite all-zero 384-element vector, **When** upsert runs for an existing parent movie, **Then** the row is stored successfully.
3. **Given** the pre-DEV-413 global “no vector column” assertions, **When** this feature lands, **Then** those assertions are replaced with exact `movie_features.vector` verification rather than deleted, and fixed historical migration-range assertions remain meaningful.
4. **Given** Phase B pickup recon confirming ADR `0024` remains available on `origin/main`, **When** the build PR is prepared, **Then** `docs/adr/0024-movie-features-storage-port.md` is published with Status Accepted, `.specify/memory/constitution.md` Principle I adds `IMovieFeaturesRepository` after `IPlaybackHistory` with MINOR bump `1.3.0` → `1.4.0` (or stops for version reconciliation if the baseline changed), Sync Impact Report / Version / Last Amended are updated, template review is evidenced, and `PortWhitelistPattern` gains `|IMovieFeaturesRepository` with no harness/gate/threshold edit.
5. **Given** the new Core port, **When** `Core_ports_must_match_the_constitution_whitelist` runs under `dotnet test`, **Then** the port matches the updated whitelist and the anti-vacuity floor is unchanged.

### Edge Cases

- Caller supplies `ComputedAt`; the repository has no `TimeProvider` and does not invent time.
- Concurrent upserts: one row, one coherent submitted pair; last successful database write wins; no optimistic concurrency or timestamp freshness `WHERE`.
- Finite zero vector is permitted storage input; ranking semantics belong to DEV-348.
- Missing parent movie is a database FK failure, not a silent upsert.
- Down migration drops `movie_features` (and its index with the table) and must not drop the preexisting vector extension.
- Raw-SQL HNSW index uses default HNSW parameters, transactional create (no `CONCURRENTLY`), stable name `IX_movie_features_vector`.
- EF configuration must not emit a duplicate scaffolded `CreateIndex` for the same index.
- Existing recommendation `MovieFeatures` type remains unchanged and is not the storage snapshot.
- No HTTP/API/OpenAPI/web, embedding generation, worker orchestration, ranking/retrieval, LocalPlay, secrets, or new dependency/project/layer.
- Phase B must recheck ADR 0024 availability on main; other worktrees were outside Phase A recon scope.
- If pickup recon finds a constitution version baseline other than `1.3.0`, stop for version reconciliation rather than overwriting it.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Core MUST expose `IMovieFeaturesRepository` in `src/LamuFlix.Core/Ports/IMovieFeaturesRepository.cs` with `Task<MovieFeaturesSnapshot?> GetByMovieIdAsync(MovieId movieId, CancellationToken ct)` and `Task UpsertAsync(MovieFeaturesSnapshot features, CancellationToken ct)`.
- **FR-002**: Core MUST define sealed record `MovieFeaturesSnapshot` in `src/LamuFlix.Core/Ports/MovieFeaturesSnapshot.cs` with `MovieId MovieId`, `ImmutableArray<float> Vector`, and `DateTimeOffset ComputedAt`, enforcing `Vector.Length == EmbeddingDimensions.Value` (384) and finite elements. Existing `Features/Recommendations/MovieFeatures` MUST remain unchanged.
- **FR-003**: Infrastructure MUST implement `MovieFeaturesRecord` (`MovieId` PK/FK to movies, `Vector` `vector(384)`, `ComputedAt`), `MovieFeaturesConfiguration`, and `EfMovieFeaturesRepository`, and MUST register `AddScoped<IMovieFeaturesRepository, EfMovieFeaturesRepository>()` in the existing persistence registration. `Pgvector.Vector` and EF records MUST stay in Infrastructure.
- **FR-004**: `UpsertAsync` MUST use one parameterized PostgreSQL `INSERT ... ON CONFLICT (movie_id) DO UPDATE` that unconditionally replaces `vector` and `computed_at` together (last successful database write wins). The repository MUST NOT take a `TimeProvider`. `GetByMovieIdAsync` MUST return null when absent. Both methods MUST check/propagate `CancellationToken`.
- **FR-005**: Pre-SQL validation MUST reject null snapshot, default vector, wrong dimension, and non-finite elements (`ArgumentNullException` / `ArgumentException`). Finite zero vectors MUST be accepted. Missing parents MUST be rejected by the movies FK with cascade delete on parent removal.
- **FR-006**: Schema MUST be table `movie_features` with required `movie_id integer` PK (`MovieIdConverter`, `ValueGeneratedNever`), required `vector vector(384)`, required `computed_at timestamp with time zone`, and FK to `movies` with `DeleteBehavior.Cascade`. `LamuFlixDbContext` MUST add `DbSet<MovieFeaturesRecord> MovieFeatures`. No other table alteration.
- **FR-007**: One additive EF-scaffolded migration `AddMovieFeatures` (designer + snapshot generated; prior migrations untouched) MUST create the table and execute raw SQL `CREATE INDEX "IX_movie_features_vector" ON movie_features USING hnsw (vector vector_cosine_ops);` transactionally with default HNSW parameters. Down MUST drop `movie_features` (index with table) and MUST retain the existing vector extension. EF model/snapshot MUST agree (`HasPendingModelChanges() == false`) without duplicate index creation. Phase B verifies supported provider/index representation through authoritative docs rather than guessing.
- **FR-008**: Proof MUST use the existing pinned pgvector Testcontainers fixtures without mocking query translation or serialization. Cover clean migration, upgrade from `AddManualImdbId`, rollback/reapply retaining vector extension and prior movie data; exact column type/dimension/nullability/PK/FK/cascade; `pg_indexes` HNSW/cosine/vector and expected index name; model/snapshot alignment; repository absent/round-trip/sequential upsert/concurrent upsert/FK/cascade/cancellation; unit/model/FsCheck validation including finite zero vectors. UTC test instants MUST be representable at database precision; reads MUST use fresh contexts.
- **FR-009**: Forced existing-file assertion maintenance MUST update `MigrationTests` (schema table list, expected migration count/order/latest, lifecycle and `movie_features` schema/index assertions), replace obsolete global no-vector assertions in `PersistenceCompositionTests` and `PostgresFixtureTests` with precise `movie_features.vector` checks while preserving fixed historical migration-range assertions, and update `ModelMetadataTests` as required by the new entity.
- **FR-010**: Phase B MUST publish new-port ADR `docs/adr/0024-movie-features-storage-port.md` with Status Accepted after bounded pickup recon confirms `0024` remains available on `origin/main`. Quill does not invent ADR body text; Keel owns ADR drafting. If a separate Phase A Proposed draft is later assigned, its path is `specs/DEV-413/adr-0024-movie-features-storage-port.md`.
- **FR-011**: Phase B MUST amend `.specify/memory/constitution.md` Principle I by adding `IMovieFeaturesRepository` immediately after `IPlaybackHistory` in the closed named-port enumeration, retaining BCL/M.E.AI abstractions and the sentence `Adding a port requires an ADR.`, via standard MINOR amendment `1.3.0` → `1.4.0` against the current baseline (stop for reconciliation if baseline changed), updating Sync Impact Report, Version, and Last Amended (actual amendment date), and evidencing `.specify/templates/*.md` review (no template edit currently indicated). The port implementation MUST NOT merge ahead of the amendment and Accepted ADR.
- **FR-012**: Phase B MUST add `|IMovieFeaturesRepository` to `PortWhitelistPattern` in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs:28-29`, preserving every existing name, anchors, and the enforcing `Core_ports_must_match_the_constitution_whitelist` test (`:152-175`), including its anti-vacuity floor. MUST NOT change `harness.yml`, gate scripts, or thresholds.
- **FR-013**: The system MUST NOT add UI, public HTTP/OpenAPI/web contracts, embedding generation, worker/FeaturesRequested orchestration, recommendation ranking/retrieval, HNSW parameter tuning or query-plan benchmarks, new NuGet/npm dependency, new project/top-level folder/architectural layer, LocalPlay/secrets/`Process.Start`, glossary rewrite, or unrelated refactor. Owner structural checkboxes: none.

### Key Entities

- **Movie features snapshot**: Caller-facing immutable embedding plus computation instant (`MovieId`, 384-float vector, `ComputedAt`) used by the Core port.
- **Movie features record**: Infrastructure persistence row for one movie’s stored embedding and computation time.
- **HNSW cosine index**: Database index `IX_movie_features_vector` on `movie_features.vector` using HNSW with `vector_cosine_ops`.
- **New-port ADR 0024**: Accepted architectural record authorizing `IMovieFeaturesRepository` (published in Phase B after collision recon).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On the pinned pgvector image, the additive migration creates `movie_features` with `vector(384)` and `pg_indexes` proves `IX_movie_features_vector` uses HNSW cosine ops on `vector`.
- **SC-002**: Upserting the same `MovieId` twice leaves exactly one row whose `ComputedAt` (and vector) match the latest successful write; concurrent writers leave one coherent submitted pair.
- **SC-003**: Absent reads return null; valid round-trips preserve 384 finite floats and UTC timestamptz-representable `ComputedAt` via fresh contexts; invalid vectors fail before SQL; zero vectors succeed.
- **SC-004**: Forced schema/composition/architecture assertions stay green after precise updates; obsolete global no-vector assertions are replaced, not deleted.
- **SC-005**: Before the build PR merges, ADR 0024 is Accepted, constitution Principle I lists the new port at version `1.4.0` (or reconciled), and the architecture whitelist matches—without harness/threshold edits.
- **SC-006**: Configured backend verification gates run without a lowered threshold. Unchanged `/web` is scope-empty `SKIPPED`, never PASS.

## Assumptions

- No `[assumed]` taste items were recorded for this ticket; there is no `ASSUMPTIONS.md`.
- No owner structural checkbox is open (`blocked: structural = none`). Anything not decided in `brief.md` / `CONCLUSIONS.md` is a `needs decision:` item, not an assumption invented here.
- ADR body text is Keel’s to draft; this specification carries the settled filename, publication timing, collision-recon requirement, and constitution amendment text already ruled in Q7.
