# Tasks: Enable the pgvector Extension in Existing PostgreSQL Persistence

**Input**: Design documents from `specs/DEV-346/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [brief.md](brief.md), [CONCLUSIONS.md](CONCLUSIONS.md), [ASSUMPTIONS.md](ASSUMPTIONS.md)

**Tests**: Required for US5. Proofs are written after the scaffold, in the brief's order. This list does not add a red-first requirement the brief does not state.

**Organization**: Setup and foundational tasks carry no story label. US1 through US5 carry story labels. Polish carries no story label.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: User story from `spec.md`
- File paths are relative to the worktree `F:\Dev\LamuFlix.worktrees\feature-346-spec`

## Phase 1: Setup

**Purpose**: Re-pin facts and record a baseline before any production edit.

- [ ] T001 Wisp re-pins `feature/346-spec` against recon base `7bc803be582161094e3ec2dc00aab24e53647ebc`. Compare `src/LamuFlix.Infrastructure/Persistence/Migrations/` with `20260930153520_Initial` and `20261016195556_AddMovieWriters` only, and compare every `UseNpgsql` call under `src/` and `tests/` with the retained list in `specs/DEV-346/plan.md`. Also have Wisp recheck all readers/writers of `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` and design-time factory consumers: T030 assumes no concurrent collection uses either. A new environment/factory consumer, migration or options caller stops this list; report it for a Patron ruling and do not edit that caller.
- [ ] T002 Gauge records the baseline at the pin from T001: `dotnet test`, `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`, `dotnet format --verify-no-changes`, `./scripts/run-vulnerable-packages.ps1`, `./scripts/run-property-tests.ps1`, `pwsh -NoProfile -File ./scripts/run-mutation.ps1`, and the pipeline web gate. Record each exit as printed. Do not edit `harness.yml`.

**Checkpoint**: Facts are current and the baseline exits are recorded.

---

## Phase 2: Foundational

**Purpose**: Hold production edits until the pin is clean.

- [ ] T003 Start story edits only after T001 reports the migration set and the `UseNpgsql` caller list still match `specs/DEV-346/plan.md`, and after T002 has recorded the baseline. The allowed edit set is that plan's file boundary plus the tool-generated `EnableVector` migration, designer, and snapshot.

**Checkpoint**: Story work stays inside the file boundary.

---

## Phase 3: User Story 1 - Approved vector packages (Priority: P1)

**Goal**: Pin the two published packages and reference them only where a retained call consumes them.

**Independent Test**: Restore and build Infrastructure, Tests.Common, UnitTests, and IntegrationTests with zero warnings. Each `Pgvector.EntityFrameworkCore` reference has a direct `Npgsql.EntityFrameworkCore.PostgreSQL` reference.

- [X] T004 [US1] Establish the Q3 central pins via `dotnet add src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj package Pgvector --version 0.3.2` and `dotnet add src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj package Pgvector.EntityFrameworkCore --version 0.3.0`. Let the CLI update `Directory.Packages.props` and project references. Keep EF Core `10.0.13` and the provider `10.0.3` unchanged; never use unpublished `10.0.0`. No manual project edits.
- [X] T005 [US1] After T004, verify the CLI-created direct `Pgvector` and `Pgvector.EntityFrameworkCore` references in `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` and retain its direct `Npgsql.EntityFrameworkCore.PostgreSQL` reference. Any missing reference is added via `dotnet add <project> package <PackageId> --version <Q3 pin>`, never by hand.
- [X] T006 [US1] After T005, use `dotnet add tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj package <PackageId> --version <Q3 pin>` for `Pgvector` `0.3.2` and `Pgvector.EntityFrameworkCore` `0.3.0`. Keep the existing direct provider reference. No manual project edits.
- [X] T007 [US1] After T006, use `dotnet add tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj package Pgvector.EntityFrameworkCore --version 0.3.0`. Keep its existing direct provider reference and do not add `Pgvector`. No manual project edits.
- [X] T008 [US1] After T007, use `dotnet add tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj package <PackageId> --version <pin>` for `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` first, then `Pgvector` `0.3.2` and `Pgvector.EntityFrameworkCore` `0.3.0`. No manual project edits.
- [X] T009 [US1] Restore and build those four projects. Confirm zero warnings, zero errors, the two pins, unchanged EF and provider versions, and a direct provider reference on every project that references `Pgvector.EntityFrameworkCore`.

**Checkpoint**: US1 is met. US2 may already have landed beside it.

---

## Phase 4: User Story 2 - One image pin at three sites (Priority: P1)

**Goal**: Put the same digest-pinned image on both fixtures and the Compose postgres service.

**Independent Test**: The three image strings match, and the Compose diff is the postgres `image` line only.

- [X] T010 [P] [US2] In `tests/LamuFlix.Tests.Common/ContainerFixture.cs`, replace only the PostgreSQL image argument with `pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d`. Leave the RabbitMQ builder and start/dispose behavior unchanged.
- [X] T011 [P] [US2] In `tests/LamuFlix.Tests.Common/PostgresFixture.cs`, replace only the PostgreSQL image argument with the same digest pin.
- [X] T012 [P] [US2] In `docker-compose.yml`, replace only the postgres service `image` line with the same digest pin. Leave environment interpolation, database name, volume, ports, `pg_isready` healthcheck, and `rabbitmq:4-alpine` unchanged.
- [X] T013 [US2] Confirm the three sites contain that exact pin, `ContainerFixture` still builds `rabbitmq:4.0.0`, Compose still contains `rabbitmq:4-alpine`, and `specs/DEV-325/` is unmodified.

**Checkpoint**: US2 is met. Container tests that migrate the extension wait for US4 as well.

---

## Phase 5: User Story 3 - Provider-owned vector options and the model declaration (Priority: P1)

**Goal**: Add `UseVector` at every retained options site and declare the extension on the model.

**Independent Test**: Each named site calls `UseVector`. `MovieModelTests` and `ModelMetadataTests` still have no connection string. The model declares `vector` and has no vector column.

- [ ] T014 [P] [US3] In `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`, change the runtime registration to `UseNpgsql(connectionString, o => o.UseVector())`. Leave the PostgreSQL health-check registration unchanged.
- [ ] T015 [P] [US3] In `src/LamuFlix.Infrastructure/Persistence/LamuFlixDesignTimeDbContextFactory.cs`, add `UseVector` on the existing connection-bearing `UseNpgsql` call. Keep `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` as the connection source.
- [ ] T016 [P] [US3] In `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs`, declare `modelBuilder.HasPostgresExtension("vector")` from `OnModelCreating`. Add no vector property, column, or `movie_features` entity.
- [ ] T017 [P] [US3] In `tests/LamuFlix.Tests.Common/LamuFlixDbContextFactory.cs`, add `UseVector` on the `OpenContext` and `CreateAdminContext` `UseNpgsql` calls. Keep each existing connection argument.
- [ ] T018 [P] [US3] In `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs`, add `UseVector` on the retained direct `UseNpgsql` call. Keep its connection argument.
- [ ] T019 [P] [US3] In `tests/LamuFlix.IntegrationTests/EfMovieRepositoryClaimConcurrencyTests.cs`, add `UseVector` on the retained direct `UseNpgsql` call. Keep its connection argument.
- [ ] T020 [P] [US3] In `tests/LamuFlix.IntegrationTests/EfMovieCatalogPagingTests.cs`, add `UseVector` on the retained direct `UseNpgsql` call. Keep its connection argument.
- [ ] T021 [P] [US3] In `tests/LamuFlix.IntegrationTests/MovieCatalogRegistrationTests.cs`, add `UseVector` on the retained direct `UseNpgsql` call. Keep the existing unused-host connection argument.
- [ ] T022 [P] [US3] In `tests/LamuFlix.UnitTests/Persistence/MovieModelTests.cs`, change the model-only build to `UseNpgsql(o => o.UseVector())`. Do not add a connection string.
- [ ] T023 [P] [US3] In `tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs`, change the model-only build to `UseNpgsql(o => o.UseVector())`. Do not add a connection string.
- [ ] T024 [P] [US3] In `tests/LamuFlix.UnitTests/Persistence/EfMovieRepositorySurvivorTests.cs`, add `UseVector` on the retained direct `UseNpgsql` call. Keep its connection argument.
- [ ] T025 [P] [US3] In `tests/LamuFlix.UnitTests/Persistence/EfMovieRepositoryConstructorTests.cs`, add `UseVector` on the retained direct `UseNpgsql` call. Keep its connection argument.

**Checkpoint**: US3 is met. No shared options helper exists.

---

## Phase 6: User Story 4 - Tool-generated EnableVector migration (Priority: P1)

**Goal**: Scaffold the extension-only migration and leave published history intact.

**Independent Test**: The migrations folder gains only `EnableVector`, its designer, and a regenerated snapshot. Up SQL is the extension statement. Down SQL is the tool default.

- [ ] T026 [US4] After T014 through T025, securely supply a session-only `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` before invoking the tools; never commit or print the value. Then scaffold `EnableVector` with the repository Entity Framework tools, using `src/LamuFlix.Infrastructure/Persistence/LamuFlixDesignTimeDbContextFactory.cs` as the options source. The tool writes the migration, the designer, and `src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs`. If the tool cannot resolve the factory, stop and report the tool error. Do not hand-write the migration.
- [ ] T027 [US4] Read the generated up and down paths under `src/LamuFlix.Infrastructure/Persistence/Migrations/`. Up is `CREATE EXTENSION IF NOT EXISTS vector` and no table, column, or index. Down is the tool default and contains no `CASCADE`. `20260930153520_Initial` and `20261016195556_AddMovieWriters`, including designers, have an empty diff. If Up contains any other schema operation, stop for a ruling and do not edit the generated file.
- [ ] T028 [US4] Confirm `LamuFlixDbContextModelSnapshot.cs` records the `vector` extension annotation and no vector column.

**Checkpoint**: US4 is met. Published migrations are unchanged.

---

## Phase 7: User Story 5 - Real PostgreSQL proofs and existing suites (Priority: P1)

**Goal**: Prove the extension, the upgrade, and a tableless vector round-trip, then run the existing suites.

**Independent Test**: The three proof classes pass on the pinned image, and the existing unit, integration, and legacy container suites pass.

- [ ] T029 [US5] In `tests/LamuFlix.IntegrationTests/MigrationTests.cs`, update the ordered migration assertion from length 2 to `Initial`, `AddMovieWriters`, `EnableVector`. Use a migration connection privileged to create the extension when absent, as required by the plan Migration precondition (Q8); do not add role provisioning or a negative privilege test. On the pinned image, assert a fresh migrate has a `pg_extension` row named `vector`, the existing public-table catalog assertion is unchanged, and `HasPendingModelChanges()` is false. Assert an `AddMovieWriters` database with its existing rows upgrades to `EnableVector` with those rows and that catalog preserved and with no pending model changes. Assert the generated down path returns the applied list to `AddMovieWriters` and a reapply ends at `EnableVector`. Do not assert `DROP EXTENSION` or `CASCADE`.
- [ ] T030 [P] [US5] In `tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs`, round-trip one parameterized `Pgvector.Vector` through runtime composition against the fixture database and assert the read value equals the sent value. Leave no vector table behind. Keep this proof in the existing `PostgresCollection`; T001 must confirm no concurrent collection reads/writes the environment variable or invokes the design-time factory. If that premise changes, stop for a ruling before the override. For design-time parity, set `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` to the fixture connection for the test only and restore the previous value in `finally`. Assert the design-time options include `UseVector`, the model declares the `vector` extension, and the `EnableVector` up SQL is `CREATE EXTENSION IF NOT EXISTS vector`. Do not print the connection string.
- [ ] T031 [P] [US5] In `tests/LamuFlix.IntegrationTests/PostgresFixtureTests.cs`, round-trip one parameterized `Pgvector.Vector` through `tests/LamuFlix.Tests.Common/LamuFlixDbContextFactory.cs` against the fixture database. Assert value equality and leave no vector table behind.
- [ ] T032 [US5] Run the existing `LamuFlix.UnitTests`, `LamuFlix.IntegrationTests`, and legacy `tests/LamuFlix.Test` suites. The only expected test-source edits are the authorized US2 image-pin edits in `ContainerFixture.cs` and `PostgresFixture.cs` (T010-T011), the Q1 migration assertion, the Q5 options edits, and T029 through T031.

**Checkpoint**: US5 is met. The ticket acceptance criteria have a proof.

---

## Phase 8: Polish and close-out

**Purpose**: Refactor, gates, boundary check, and the three review axes.

- [ ] T033 Cog refactors the implementation diff inside the file boundary. Then run `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` on the changed C# files. Fix over-threshold methods by extraction, early return, or guard clauses. Do not add an options helper to satisfy the gate.
- [ ] T034 Confirm the diff against the T001 pin is limited to the `specs/DEV-346/plan.md` file boundary plus the generated `EnableVector` migration, designer, and snapshot. Confirm every retained `UseNpgsql` site passes `UseVector`, no `movie_features` or `CASCADE` text exists in the new migration, and `specs/PRODUCT.md`, `specs/DEV-325/`, `docs/adr/`, `CONTEXT.md`, `README.md`, `web/`, and `harness.yml` are unmodified.
- [ ] T035 Gauge runs `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`, and `dotnet format --verify-no-changes` on the changed files. Record exits. Do not edit `harness.yml`.
- [ ] T036 Gauge runs `./scripts/run-vulnerable-packages.ps1`, `./scripts/run-property-tests.ps1` (no new FsCheck test; keep the printed outcome), `pwsh -NoProfile -File ./scripts/run-mutation.ps1`, `dotnet test`, and the pipeline web gate. Record each verdict. Exit 1 and a gate that could not run block. Record scope-empty SKIPPED, harness SKIP, and mutation NOT APPLICABLE as those verdicts.
- [ ] T037 Sentry, Ledger, and Compass review the implementation diff against the T001 pin, limited to the file boundary in `specs/DEV-346/plan.md` plus the generated migration files. Post findings and the summary on the PR. Critical, High, and Medium findings block. Low findings are recorded and do not block. Maximum two review rounds and two fix commits per round. Unresolved blockers stay blocked at the cap. This task does not merge.

**Checkpoint**: Gates and the three axes are recorded. Later-head gates after rebase remain required and are not waived.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: starts immediately. T002 follows T001.
- **Foundational (Phase 2)**: follows T001 and T002. Blocks every story edit.
- **US1 (Phase 3)**: follows T003. T004-T008 run sequentially in ID order because their CLI commands can update the shared `Directory.Packages.props`. T009 follows T008.
- **US2 (Phase 4)**: follows T003. Independent of US1 and may run beside it.
- **US3 (Phase 5)**: follows T009. T014-T025 run together.
- **US4 (Phase 6)**: follows T014-T025. T027 and T028 follow T026.
- **US5 (Phase 7)**: follows T013, T025, and successful completion of both T027 and T028. T029-T031 run together only after both succeed. An unexpected schema operation stops at T027 for a ruling, and T029-T031 do not start. T032 follows them.
- **Polish (Phase 8)**: follows T032. T034 and T035 follow T033. T036 follows T035. T037 follows T036.

### User Story Dependencies

- **US1 (P1)**: package graph. Blocks US3.
- **US2 (P1)**: image pin. Blocks US5 container runs. Does not block US3 or US4.
- **US3 (P1)**: options and model. Blocks US4.
- **US4 (P1)**: generated migration. Blocks US5.
- **US5 (P1)**: proofs and existing suites. Depends on US2 and US4.

### Parallel Opportunities

- T010, T011, and T012 can run beside T004-T008.
- T004-T008 cannot run together: package CLI writes share `Directory.Packages.props`; execute in ID order.
- T014 through T025 can run together after T009.
- T029, T030, and T031 can run together after T013 and after both T027 and T028 succeed. An unexpected schema operation stops at T027 for a ruling, and T029-T031 do not start.

---

## Parallel Example: User Story 3

```text
T014 PersistenceServiceCollectionExtensions.cs
T015 LamuFlixDesignTimeDbContextFactory.cs
T016 LamuFlixDbContext.cs
T017 LamuFlixDbContextFactory.cs
T018 EfMovieRepositoryTests.cs
T019 EfMovieRepositoryClaimConcurrencyTests.cs
T020 EfMovieCatalogPagingTests.cs
T021 MovieCatalogRegistrationTests.cs
T022 MovieModelTests.cs
T023 ModelMetadataTests.cs
T024 EfMovieRepositorySurvivorTests.cs
T025 EfMovieRepositoryConstructorTests.cs
```

---

## Implementation Strategy

### Ticket delivery

The ticket acceptance criteria are met when US1 through US5 are all present. US2 can be written beside US1. US3 waits for the package references. US4 waits for the model and the design-time options. US5 waits for the image and the migration.

### Incremental checkpoints

1. Finish Phase 1 and Phase 2 before editing production files.
2. Finish US1 and US2, then US3, then US4, and inspect the scaffold before writing proofs.
3. Finish US5, then the refactor and the gates, then the three review axes.

### Close-out

Commit messages during implementation start `DEV-346 - `. This task list does not authorize a merge. Optional Gherkin is not generated. No ADR and no glossary edit.

---

## Notes

- [P] tasks touch different files.
- Story labels map to `spec.md`.
- Line numbers in the recon notes belong to `7bc803be582161094e3ec2dc00aab24e53647ebc` and are re-checked by T001.
- A scaffold that is not extension-only stops at T027.
- Task count: 37. Setup 2, foundational 1, US1 6, US2 4, US3 12, US4 3, US5 4, polish 5.
