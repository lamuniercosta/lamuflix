---

description: "Task list for DEV-19 PostgreSQL persistence via EF Core"
---

# Tasks: PostgreSQL persistence via EF Core

**Input**: `specs/DEV-19/spec.md`, `plan.md`, `CONCLUSIONS.md`

**Prerequisites**: Owner answers the Q7 checkbox on the spec PR (Gate 1). All implementation follows Gate 1; do not start any task before it opens.

**Tests**: Required (AC1, AC4, AC5). Write each test before its production code where practical.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency)
- **[Story]**: US1 migration, US2 round-trip, US3 converters, US4 no MySQL, US5 ADR

After every `.cs` change run the three gates from `CLAUDE.md`: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1`.

## Phase 0: Mutation gate prerequisite (brief P1) — blocks Phase 2

- [ ] T000 `needs recon`: confirm how `scripts/run-mutation.ps1` (`:91-114`, `:456`, `:604`) and `stryker-config.json` can exclude `src/LamuFlix.Infrastructure/Persistence/Migrations/**` from the mutate set and keep `LamuFlix.IntegrationTests` out of the per-mutant test run. Patron then rules on the edit to a file this ticket does not name. Record the ruling in `CONCLUSIONS.md` and the approach in `brief.md`. The threshold stays 80.

## Phase 1: Legacy retirement, then packages and tools (Q7 A, Q6; A1)

Retirement comes first: the Pomelo references in Data and Worker would break the central-package-management restore once the pin is removed. Task IDs are not in execution order here; run in the order listed.

- [ ] T017 [US4] Delete `src/LamuFlix.Data/**` (whole project)
- [ ] T018 [US4] `LamuFlix.sln`: remove Data, Web, Worker, `tests/LamuFlix.Test`; leave their files on disk
- [ ] T019 [P] [US4] Remove only the Data `ProjectReference` from `LamuFlix.Web.csproj`, `LamuFlix.Worker.csproj`, `tests/LamuFlix.Test/LamuFlix.Test.csproj`. Worker's Pomelo `PackageReference` stays on disk, unpinned and unbuilt, for DEV-314/315/316 (FR-012); AC2 is scoped to built projects
- [ ] T032 [US4] `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj`: switch the reference from Data to Infrastructure; delete the legacy `LamuFlixContextFactory.cs` (its only Data-dependent file); `ContainerFixture.cs` is untouched
- [ ] T001 [US4] `Directory.Packages.props`: remove Pomelo; set `Microsoft.EntityFrameworkCore` and `.Design` to 10.0.12, `Npgsql.EntityFrameworkCore.PostgreSQL` to 10.0.3, align every other Microsoft EF pin to 10.0.12
- [ ] T002 [P] [US1] `.config/dotnet-tools.json`: `dotnet-ef` to 10.0.12, no other tool entry changed
- [ ] T003 [US1] `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj`: add Npgsql provider; add `Microsoft.EntityFrameworkCore.Design` with `PrivateAssets=all`
- [ ] T004 Run `dotnet tool restore`, `dotnet restore` and a solution build; record the result (version evidence is not a build PASS). A failure is a Phase 1 blocker reported to Keel, never worked around.

## Phase 2: Converters and unit tests (Q4, AC5)

- [ ] T005 [US3] Read Core factories (`MovieId`, `LibraryPath`, `MediaFormat`, `ImdbId`, `ImdbRating`, `Runtime`, `ReleaseYear`, `EnrichmentStatus`, `EnrichmentFailureCategory`) and note each `TryCreate` signature and key type
- [ ] T006 [US3] Tests first in `tests/LamuFlix.UnitTests/Persistence/`: per converter valid, invalid non-null throws, null preserved; status 0-3 accepted and others rejected; unknown category code throws; FsCheck property tests tagged `[Trait("Category", "Property")]` for `./scripts/run-property-tests.ps1`: valid values round-trip provider→model→provider, and the status mapping is a bijection on 0-3 (brief P6)
- [ ] T007 [US3] `src/LamuFlix.Infrastructure/Persistence/Converters/`: shared throw-on-failed-`TryCreate` helper
- [ ] T008 [P] [US3] Converters for `MovieId`, `LibraryPath`, `MediaFormat` (stores `Extension`), `ImdbId`, `ImdbRating`, `Runtime`, `ReleaseYear`
- [ ] T009 [P] [US3] `EnrichmentStatus` int converter via `Value`; `EnrichmentFailureCategory` `Code` converter (known singleton or throw)
- [ ] T010 [US3] Run `dotnet test --filter "FullyQualifiedName~Persistence" --nologo -v q`; run gates

## Phase 3: Records, configurations, context, factory (Q1, Q2, Q3, Q5, Q8)

- [ ] T011 [P] [US1] `Persistence/Records/`: `MovieRecord`, `ActorRecord`, `DirectorRecord`, `GenreRecord` (int identity + required `name` for the three ancillary records; nine nullable metadata scalars plus the FR-005a Core scalars on `MovieRecord`; `MovieRecord.Id` is `MovieId`, null before insert)
- [ ] T012 [US1] `Persistence/Configurations/MovieConfiguration.cs`: table `movies`, snake_case columns, exact types and nullability (FR-005, FR-005a), identity key with the `MovieId` converter and `ValueGeneratedOnAdd`, converters, indexes (`title`, `release_year`, `status`, unique `library_path`, unique filtered `imdb_id`), `UsingEntity` for `movie_actors`, `movie_directors`, `movie_genres` with snake_case FK columns
- [ ] T013 [P] [US1] `ActorConfiguration`, `DirectorConfiguration`, `GenreConfiguration` (`actors`, `directors`, `genres`)
- [ ] T014 [US1] `Persistence/LamuFlixDbContext.cs`: `DbContextOptions<LamuFlixDbContext>` constructor, `ApplyConfigurationsFromAssembly`, no `OnConfiguring`
- [ ] T015 [US4] Test first, then `Persistence/LamuFlixDesignTimeDbContextFactory.cs`: reads only `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING`; missing or blank fails clearly, message never contains the value, no fallback (unit tests set and restore the variable inside a dedicated collection with `DisableParallelization = true`, brief P5)
- [ ] T016 [US1] Docker-free model test in `tests/LamuFlix.UnitTests/Persistence/`: build the model with `UseNpgsql()` and no connection string; assert that the `movies` key has the `MovieId` converter and `ValueGenerated.OnAdd`, and that the FR-005/FR-005a column names, types and nullability match (brief P3). Build Infrastructure; run gates

## Phase 4: Tests.Common fixtures and negative sweep

- [ ] T020 [US1] `tests/LamuFlix.Tests.Common/`: add a new context factory building `LamuFlixDbContext` from explicit container options (no `EnsureCreated`); add `PostgresFixture` (`IAsyncLifetime`, `postgres:16.4` only), which alone starts and disposes one container per test assembly, plus a helper that creates a fresh uniquely named database per test (the `LamuFlixContextFactory.cs:13` pattern; cleaning a shared database is not isolation); leave `ContainerFixture.cs`, and never use it from IntegrationTests (brief P4)
- [ ] T021 [US4] Verify no Pomelo, `UseMySql`, MySQL connection string, MySQL migration or `password=root` in any project the solution builds, and no `EnsureCreated` call in any project the solution builds; `dotnet build` the solution. FR-015 diff check: `git diff --name-only main...HEAD` shows nothing under `src/LamuFlix.Api/`, and under `src/LamuFlix.Worker/` only `LamuFlix.Worker.csproj`, with only its Data `ProjectReference` removed

## Phase 5: Generate the migration (Q8)

- [ ] T022 [US1] Set `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` in the shell only (never committed) and run from repo root: `dotnet ef migrations add Initial --project src/LamuFlix.Infrastructure --startup-project src/LamuFlix.Infrastructure --output-dir Persistence/Migrations`
- [ ] T023 [US1] Confirm exactly three generated files, no hand edits, no `HasData`, no raw SQL; if wrong, fix configurations and regenerate all three
- [ ] T024 If analyzers flag Migrations, add `generated_code = true` for `src/LamuFlix.Infrastructure/Persistence/Migrations/**` in `.editorconfig`; the mutation exclusion is already settled by T000

## Phase 6: Integration tests (Q9, AC1, AC4)

- [ ] T025 [US1] Create `tests/LamuFlix.IntegrationTests` (xUnit v3 + Shouldly, central pins), reference Infrastructure and Tests.Common, register `PostgresFixture` as the assembly fixture, add to the solution
- [ ] T026 [US1] `MigrationTests`: fresh database, `MigrateAsync`, applied IDs equal the `Initial` ID, `HasPendingModelChanges()` false, `information_schema` and `pg_indexes` assertions (seven tables, snake_case columns, the FR-005/FR-005a nullable set with every other `movies` column not null, a composite primary key on each join table's FK columns, five indexes, history table counted separately)
- [ ] T027 [US2] `PersistenceRoundTripTests`: movie with actor/director/genre sets read back with a new context; store-assigned `Id.Value > 0` after `SaveChanges`; UTC `enriched_at`/`last_attempt_at` round-trip; all-null metadata case; title-only metadata; second null `imdb_id` allowed
- [ ] T028 Run `dotnet test --filter "FullyQualifiedName~IntegrationTests" --nologo -v q`

## Phase 7: Gates (AC6)

- [ ] T029 [US5] Confirm `docs/adr/ADR-0003.md` is unchanged and still Proposed
- [ ] T030 Run the three static-analysis scripts, `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` (refactor pass), `dotnet format --verify-no-changes`, full `dotnet test`, and mutation on Infrastructure at the `harness.yml` threshold (80) under the T000 ruling, `./scripts/run-vulnerable-packages.ps1` (`harness.yml` `vulnerablePackages`, transitive), and `./scripts/run-property-tests.ps1` (must find the Category=Property converter tests; its "no tests tagged" exit is a FAIL for this ticket). The web gate does not apply: DEV-19 touches no `web/` and `harness.yml` has `web.enabled: false`
- [ ] T031 Record each gate result; a skipped gate is not a pass. Full `dotnet test` needs Docker; an outage is "Could not run", never a verdict

## Dependencies

- All tasks need Gate 1 (the Q7 answer). T000 blocks Phase 2 onward (it can run alongside Phase 1). Phase 1 blocks all; within it, retirement (T017-T019, T032) precedes the pins and T004. Phases 2 and 3 can overlap after T003. Phase 4 needs Phase 3. Phase 5 needs Phases 3-4. Phase 6 needs Phase 5. Phase 7 last.
- Commits use `DEV-19 - {subject}`.
