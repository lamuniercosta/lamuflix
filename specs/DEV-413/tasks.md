# Tasks: DEV-413 movie_features storage and HNSW index

**Input**: Design documents from `specs/DEV-413/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (plan records why). Code Phases 1–3 also require the Phase B entry checklist below. Missing Phase B evidence blocks T001 and later code work.

**Tests**: Included. Brief Q4 and FR-008 require them. Author the behavior test before the production edit it locks. When the types under test do not yet exist, add only the minimum compilable contracts needed so those tests compile; do not treat missing-type compile failures as behavioral red tests. When types already exist and the locked behavior is not already implemented, demonstrate behavioral failure before the behavior edit when possible. Drive each phase's tests green before that phase's commit. Use existing xUnit, assertion libraries, pinned pgvector Testcontainers, and FsCheck. No mocks of EF/Npgsql query translation or serialization. No new test dependency. Single Anvil ownership for Phases 1–3.

**Organization**: Phases follow brief task order and Q7 Phase B ordering. Anvil implements Phases 1–3, one phase per ask, and stops after Phase 3. Phase 4 is Cog, then Gauge. Do not start Phase 1 until the Phase B entry checklist is satisfied. Port implementation cannot merge ahead of Accepted ADR and constitution MINOR amendment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 schema/HNSW migration, US2 read/upsert repository, US3 validation and forced assertion/ADR/constitution/whitelist sync.
- Exact file paths are in each description.

## Phase B entry (before Code Phase 1)

**Purpose**: Schedule Build workspace entry checks. Do not run gates, tests, or analyzers from this Spec workspace. Missing evidence blocks T001.

**Independent Test**: Every checklist item below has recorded evidence in the task note (or named owner handoff). Any missing item stops code.

- [ ] TB000 Confirm a recorded Epic 7 (SPA) completion receipt is present in the task note (`specs/PRODUCT.md`: Epic 9 / recommendations-pgvector work is deferred until Epic 7 completes). This item requires the receipt record only; it does not assert whether Epic 7 is currently complete. Absence of a recorded receipt blocks code.
- [ ] TB001 Confirm the spec PR has merged and Gate 1 is established (Rigger / YouTrack `Ready to build` handoff). Checklist mark must reference the recorded receipt.
- [ ] TB002 Obtain Phase B drift recon against `origin/main` for the authorized file set (Wisp → `recon-DEV-413` refresh). Explicitly confirm ADR number `0024` / path `docs/adr/0024-movie-features-storage-port.md` remains unclaimed on main; if claimed, stop for renumbering rather than colliding. Confirm constitution Version baseline is still `1.3.0` or record the reconciliation target; if baseline changed, stop rather than overwriting.
- [ ] TB003 Confirm the ready Phase A Proposed ADR handoff at `specs/DEV-413/adr-0024-movie-features-storage-port.md` (Keel-authored; Status Proposed). Quill does not invent ADR body text. Checklist mark must reference the Keel handoff. TB002 collision stop and T001 Accepted publication at `docs/adr/0024-movie-features-storage-port.md` remain unchanged.
- [ ] TB004 Obtain Gauge baseline gate counts for in-scope files (`-Files` / `-BaseRef` mode) recorded in `DEV-413`.
- [ ] TB005 Keel checks or amends drift against the brief/spec before any Code Phase 1 edit. Missing Keel drift check blocks T001.

**Checkpoint**: Phase B entry evidence is complete (including the recorded Epic 7 completion receipt). Only then may Anvil start T001.

---

## Phase 1: ADR, constitution, Core port, and whitelist (US3 foundational; blocks US1–US2 merge)

**Purpose**: Publish Accepted ADR, synchronize constitution Principle I (`1.3.0` → `1.4.0`), add Core snapshot/port, and extend `PortWhitelistPattern` as one coherent change before architecture verification. Blocks Phases 2–3 merge readiness. Does not scaffold the migration yet.

**Independent Test**: ArchitectureTests whitelist passes with the new port present; unit/property snapshot validation tests compile and pass without PostgreSQL; constitution Version/port list and ADR Status Accepted are present in the same change set.

**Checkpoint**: ADR Accepted at settled path. Constitution lists `IMovieFeaturesRepository` after `IPlaybackHistory` at `1.4.0` (or reconciled). Core port/snapshot exist. Whitelist updated. No harness/threshold edit.

- [ ] T001 [US3] After TB002 confirms `0024` free (and baseline reconciliation if needed), publish `docs/adr/0024-movie-features-storage-port.md` with Status Accepted using Keel-drafted content (from TB003). Do not invent ADR decisions. Do not create a competing ADR number.
- [ ] T002 [US3] Amend `.specify/memory/constitution.md` Principle I: add `IMovieFeaturesRepository` immediately after `IPlaybackHistory` in the closed named-port enumeration; retain BCL/M.E.AI abstractions and the sentence `Adding a port requires an ADR.`; MINOR bump Version `1.3.0` → `1.4.0` against the confirmed baseline (stop if baseline changed); update top Sync Impact Report, Version, and Last Amended (actual amendment date); evidence `.specify/templates/*.md` review with no-change conclusion unless review finds a required edit. Same build PR as the port.
- [ ] T003 [P] [US3] Author unit and FsCheck property tests for `MovieFeaturesSnapshot` construction invariants only: `Vector.Length != 384`, NaN/±Infinity rejection, and acceptance of finite 384-vectors including all-zero, in the existing unit-test project (new focused test file under `tests/LamuFlix.UnitTests/` beside related port/persistence tests). Do not schedule repository null/pre-SQL upsert guards here (those belong in Phase 3 with T014a/T015/T017). If types are absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T005 when possible. Do not treat missing-type compile errors as the red proof.
- [ ] T004 [P] [US3] Author/adjust the architecture whitelist expectation so `Core_ports_must_match_the_constitution_whitelist` will require `|IMovieFeaturesRepository` on `PortWhitelistPattern` in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs:28-29` without changing the anti-vacuity floor at `:152-175`. If the production constant is absent from the regex, demonstrate failure before T006 when possible.
- [ ] T005 [US3] After T003 is authored (and behavioral failure shown when possible), add `MovieFeaturesSnapshot` in `src/LamuFlix.Core/Ports/MovieFeaturesSnapshot.cs` and `IMovieFeaturesRepository` in `src/LamuFlix.Core/Ports/IMovieFeaturesRepository.cs` per Q1/Q2 (`GetByMovieIdAsync`, `UpsertAsync`, CancellationToken; enforce 384 finite elements via `EmbeddingDimensions.Value`). Do not change `src/LamuFlix.Core/Features/Recommendations/MovieFeatures.cs`.
- [ ] T006 [US3] After T004/T005, add `|IMovieFeaturesRepository` to `PortWhitelistPattern` in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs:28-29`, preserving every existing name and anchors. Do not edit `harness.yml`, gate scripts, or thresholds.

**Checkpoint**: Phase 1 tests green. Commit `DEV-413 - movie features port ADR constitution whitelist`.

---

## Phase 2: Entity, EF mapping, and additive HNSW migration (US1)

**Purpose**: Persistence record, configuration, DbContext DbSet, DI registration, and additive `AddMovieFeatures` migration with raw-SQL HNSW cosine index. Depends on Phase 1.

**Independent Test**: MigrationTests and model metadata prove table/columns/PK/FK/cascade, `pg_indexes` HNSW proof, upgrade/rollback/reapply, and `HasPendingModelChanges() == false` on the pinned pgvector fixture.

**Checkpoint**: Migration set length/order/latest updated; `movie_features` and `IX_movie_features_vector` exist; vector extension retained on Down; no duplicate EF index.

### Tests for Phase 2

- [ ] T007 [P] [US1] Author/extend migration lifecycle and schema assertions in `tests/LamuFlix.IntegrationTests/MigrationTests.cs`: add `movie_features` to `ApplicationTables`; expect migration count `8` and latest `_AddMovieFeatures`; upgrade from `_AddManualImdbId`; rollback/reapply retaining vector extension and prior movies data; exact column type/dimension/nullability/PK/FK/cascade; `ListTableIndexesAsync("movie_features")` proves `IX_movie_features_vector` with `USING hnsw` and `vector_cosine_ops` on `vector`; `HasPendingModelChanges().ShouldBeFalse()`. Preserve coverage intent of existing helpers. If types/migrations are absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T010–T012 when possible.
- [ ] T008 [P] [US1] Author/extend model metadata assertions for `MovieFeaturesRecord` in `tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs` (table name, column names/types/nullability, FK cascade to `MovieRecord`) following the existing per-entity template. Minimum compilable contracts when types are absent; behavioral failure before T010 when possible.

### Implementation for Phase 2

- [ ] T009 [US1] After T007/T008 are authored (and behavioral failure shown when possible), add `MovieFeaturesRecord` in `src/LamuFlix.Infrastructure/Persistence/Records/MovieFeaturesRecord.cs` and `MovieFeaturesConfiguration` in `src/LamuFlix.Infrastructure/Persistence/Configurations/MovieFeaturesConfiguration.cs` (`movie_id` PK with `MovieIdConverter` + `ValueGeneratedNever`, `vector vector(384)`, `computed_at` timestamptz, FK cascade). Keep EF model aligned so no duplicate scaffolded HNSW `CreateIndex` is emitted beside the raw-SQL index; confirm supported provider representation from authoritative docs during implementation.
- [ ] T010 [US1] After T009, add `DbSet<MovieFeaturesRecord> MovieFeatures` to `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs` and `AddScoped<IMovieFeaturesRepository, EfMovieFeaturesRepository>()` in `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` (adapter body may be a minimal stub until Phase 3 if required for compilation; prefer completing T013 in Phase 3 before claiming repository behavior green).
- [ ] T011 [US1] After T009/T010, scaffold additive EF migration `AddMovieFeatures` under `src/LamuFlix.Infrastructure/Persistence/Migrations/` (designer + `LamuFlixDbContextModelSnapshot.cs` generated). Do not edit the seven prior migrations. In `Up`, after table creation, execute raw SQL `CREATE INDEX "IX_movie_features_vector" ON movie_features USING hnsw (vector vector_cosine_ops);` transactionally with default HNSW parameters (no `CONCURRENTLY`). `Down` drops `movie_features` (index with table) and retains the existing vector extension.
- [ ] T012 [US1] Drive T007–T008 green against the pinned pgvector Testcontainers fixture for clean migrate, upgrade, rollback/reapply, schema metadata, `pg_indexes`, and model/snapshot alignment.

**Checkpoint**: Phase 2 migration proofs green. Commit `DEV-413 - movie_features schema and HNSW migration`.

---

## Phase 3: Repository, validation wiring, and forced assertion maintenance (US2–US3)

**Purpose**: Full `EfMovieFeaturesRepository` atomic upsert/read, complete validation wiring, and forced replacement of obsolete global no-vector assertions. Depends on Phase 2.

**Independent Test**: `EfMovieFeaturesRepositoryTests` on pinned pgvector prove absent null, round-trip (including all-zero 384-vector upsert + fresh-context read), sequential upsert, two-context concurrent upsert coherence, missing-parent FK, cascade delete, and cancellation. Unit tests prove repository null/pre-SQL upsert guards. Composition/fixture tests assert exact `movie_features.vector` rather than global absence.

**Checkpoint**: Ticket upsert AC satisfied, including zero-vector storage success on the real database. Forced assertion edits preserve coverage. No harness edit.

### Tests for Phase 3

- [ ] T013 [P] [US2] Author integration tests in `tests/LamuFlix.IntegrationTests/EfMovieFeaturesRepositoryTests.cs` using `[Collection]`/`PostgresFixture`, `ResetAsync`, `CreateMigratedContext`, and fresh read contexts: absent → null; 384-vector + UTC-representable `ComputedAt` round-trip; all-zero 384-vector upsert for an existing parent movie followed by a fresh-context read that proves the stored zero vector (real pgvector database proof for finite zero-vector storage success; no query or serialization mocks); two sequential upserts → one row with updated vector and `ComputedAt`; two-context concurrent upserts → one row with one coherent submitted pair; missing-parent FK rejection; parent delete cascades; CancellationToken propagation on get/upsert. Preserve the rest of this matrix. No query/serialization mocks. If adapter is absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T015 when possible.
- [ ] T014 [P] [US3] Author/adjust forced assertion replacements in `tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs` and `tests/LamuFlix.IntegrationTests/PostgresFixtureTests.cs`: replace obsolete global no-vector assertions with precise `movie_features.vector` checks; preserve fixed historical migration-range assertions (for example EnableVector script-range checks). Demonstrate failure of obsolete assertions before the replacement edit when possible.
- [ ] T014a [P] [US3] Author unit tests for repository null/pre-SQL upsert guards covering null snapshot and `Vector.IsDefault` (`ArgumentNullException` / `ArgumentException` before SQL), in the existing unit-test project under `tests/LamuFlix.UnitTests/` (focused file beside related port/persistence tests; may extend the Phase 1 snapshot test file only for repository-guard cases, not by moving snapshot invariants). Do not re-own Phase 1 snapshot construction invariants (`Vector.Length != 384`, NaN/±Infinity, finite/all-zero acceptance at snapshot construction). If the adapter is absent, add only the minimum compilable contracts so the tests compile; then demonstrate behavioral failure before T015 when possible.

### Implementation for Phase 3

- [ ] T015 [US2] After T013 and T014a are authored (and behavioral failure shown when possible), implement `EfMovieFeaturesRepository` in `src/LamuFlix.Infrastructure/Persistence/EfMovieFeaturesRepository.cs`: pre-SQL guards (null snapshot, `Vector.IsDefault`, and Q2 before-SQL rejection aligned with the repository entry path); parameterized `INSERT ... ON CONFLICT (movie_id) DO UPDATE` replacing `vector` and `computed_at` unconditionally; map `Pgvector.Vector` only in Infrastructure; no repository `TimeProvider`; check/propagate CancellationToken on both methods.
- [ ] T016 [US3] After T014, complete the PersistenceCompositionTests/PostgresFixtureTests assertion replacements and any remaining MigrationTests/ModelMetadataTests polish forced by the landed schema so obsolete global no-vector coverage is not deleted.
- [ ] T017 [US2/US3] Drive T013–T014, T014a green on the pinned fixture: repository matrix (including all-zero 384-vector upsert + fresh-context read), repository null/pre-SQL guard unit tests, and forced assertion maintenance. Phase 1 snapshot invariant tests (T003) remain Phase 1 ownership and are not re-driven here except as already-green prerequisites. No query/serialization mocks.

**Checkpoint**: Phase 3 tests green. Commit `DEV-413 - movie features repository and assertion sync`.

---

## Phase 4: Refactor and verification (Cog, then Gauge)

**Purpose**: Not an Anvil phase. After Phases 1–3 are committed, Cog refactors the whole change. Gauge then runs the configured gates. Verify constitution/ADR/whitelist consistency before the build PR may merge.

**Checkpoint**: Refactor complexity is at or under the refactor threshold. Every mandatory gate has its script verdict. Scope-empty skip is not recorded as pass. Port does not merge without Accepted ADR + constitution amendment + whitelist match.

- [ ] T018 Refactor the completed change across the file set in `specs/DEV-413/plan.md` until cyclomatic complexity passes at the configured refactor threshold. Read `gates.complexity.refactor` from `harness.yml` and pass that configured value to the supported `-Threshold` parameter of `./scripts/run-cyclomatic-complexity.ps1`. Do not encode a task-owned numeric literal as configuration. Extract helpers, early returns, and guard clauses. Do not suppress to pass. Do not edit harness/config/generated files or weaken thresholds.
- [ ] T019 Run the configured gates and record each script verdict: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` with no arguments (normal changed-diff gate; does not replace T018 stricter refactor evidence), `./scripts/run-jetbrains-inspectcode.ps1` with no arguments, `./scripts/run-property-tests.ps1` (require execution evidence covering the new snapshot/vector properties from T003; for this property-bearing change, disabled/all-skipped/no-matching-property exit 2 does not satisfy required evidence and must not be relabeled PASS), `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test` (including ArchitectureTests whitelist), and worktree-safe `./scripts/run-mutation.ps1` without `--since` for changed eligible production projects. Run web gates; unchanged `/web` is scope-empty `SKIPPED`, not PASS. No threshold edits and no new opt-out. Confirm ADR Accepted, constitution `1.4.0` (or reconciled) port list, and whitelist are consistent before merge. A mandatory failure, `Could not run`, or an unaccepted property-test exit 2 blocks.

---

## Excluded (never scheduled)

UI / `design.md`, embedding generation (DEV-412), FeaturesRequested consumer/worker orchestration (DEV-414), recommendation ranking/retrieval (DEV-348), HNSW parameter tuning / EXPLAIN / latency benchmarks, public HTTP/OpenAPI/web contracts, new NuGet/npm dependency, new project/layer beyond the ruled port, LocalPlay, secrets, `Process.Start`, glossary rewrite, harness/gate/threshold edits, inventing ADR body text or ADR numbers, editing prior published migrations, and unrelated refactors. A discovered need outside this list stops for a decision. It is not absorbed into a phase.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase B entry**: Depends on Spec workspace handoff. TB000–TB005 must have recorded evidence before T001. Missing recorded Epic 7 completion receipt, Gate 1, drift/ADR-collision/constitution-baseline recon, Keel ADR draft handoff, Gauge baseline, or Keel drift check blocks code.
- **Phase 1**: Depends on Phase B entry. Publishes ADR + constitution amendment + Core port/snapshot + whitelist. Blocks merge of later phases without this coherent foundation. Author T003/T004 first; T005/T006 after. Phase 1 snapshot-invariant tests green before the phase commit (repository null/pre-SQL guards are Phase 3). Single Anvil ownership.
- **Phase 2**: Depends on Phase 1. Author T007/T008 first; entity/config/context/DI/migration T009–T011; drive T012 green before phase commit.
- **Phase 3**: Depends on Phase 2. Author T013/T014/T014a first; implement T015–T016; drive T017 green before phase commit.
- **Phase 4**: Depends on Phases 1–3 being committed. Cog owns T018. Gauge owns T019.

### User Story Dependencies

- **US3 (P1)**: Starts in Phase 1 (ADR/constitution/port/whitelist/snapshot invariants); repository null/pre-SQL guards and forced assertion maintenance complete in Phase 3 (T014a/T015/T014/T016/T017).
- **US1 (P1)**: Schema/HNSW in Phase 2 (T007–T012).
- **US2 (P1)**: Repository in Phase 3 (T013/T015/T017, including all-zero upsert + fresh-context read); depends on US1 migration.

### Within Each User Story

- Author the behavior test before the production edit it locks. When types are absent, add minimum compilable contracts first; demonstrate behavioral failure before the behavior edit when possible. Missing-type compile failures are not behavioral red tests.
- ADR + constitution + whitelist with the port before architecture verification.
- Entity/config before migration scaffold.
- Migration before repository behavior proof.
- Phase tests green before that phase's commit.

### Parallel Opportunities

- TB000–TB005 are intake evidence gates; they complete before any Code Phase 1 task.
- T003 and T004 test authoring in parallel (different files).
- T007 and T008 test authoring in parallel (different files).
- T013, T014, and T014a test authoring in parallel (different files; T014a may share a unit-test file with T003 only for additive repository-guard cases after Phase 1 snapshot invariants already landed).
- Single Anvil ownership for Phases 1–3; parallel markers never split two writers onto the same migration or shared test file.

## Parallel Example: Phase 2 tests

```text
Task: "T007 Extend MigrationTests for movie_features schema and HNSW pg_indexes"
Task: "T008 Extend ModelMetadataTests for MovieFeaturesRecord"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Finish Phase 1 port/ADR/constitution/whitelist foundation.
2. Finish Phase 2 schema and HNSW migration proofs.
3. Stop there only to validate US1. Frozen scope still requires US2–US3 before review.

### Incremental Delivery

US2 lands with Phase 3 repository proofs (including all-zero upsert + fresh-context read). US3 snapshot invariants start in Phase 1; repository null/pre-SQL guards and assertion maintenance finish in Phase 3. Phase 4 does not add behavior.

### Parallel Team Strategy

One implementer is the expected path, in phase order. Do not split two writers onto the same migration, repository, or shared test file.

## Notes

- [P] tasks use different files and do not depend on an unfinished task or shared test/minimal-contract file still being written by another unfinished task.
- Story labels map to `spec.md`.
- Commit after each phase, not after every task, using `DEV-413 - {phase subject}`.
- Gate 1 comes from the merged spec PR. Phase B entry (TB000–TB005), including the recorded Epic 7 completion receipt, must also be evidenced before T001. This task list does not authorize implementation before that.
- Port implementation cannot merge ahead of Accepted ADR and constitution MINOR amendment (Q7).
- New unanticipated files or decisions return to Keel; do not invent requirements.
