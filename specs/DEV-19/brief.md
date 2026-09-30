# DEV-19 — Alignment Brief

Grill outcome for DEV-19 (size L, no parent): move persistence to PostgreSQL through EF Core in `LamuFlix.Infrastructure`, with per-entity configurations, one fresh `Initial` migration, and ADR-0003.
Rulings and evidence are in `specs/DEV-19/CONCLUSIONS.md` (Q1-Q10); the assumed identifier names are in `specs/DEV-19/ASSUMPTIONS.md`.
Grill: **10 of 12 budgeted questions asked, all 10 ruled by Patron.** Q7 is `blocked: structural` and becomes an owner checkbox on the spec PR.
Facts come from `recon-DEV-19` and the Conductor's follow-ups (LamuFlix notes), all verified at base `05484f6`.

## Owner prerequisite (Q7) — Gate 1 stays closed until answered

Checkbox wording for the spec PR:

> - [ ] DEV-19 deletes LamuFlix.Data and removes LamuFlix.Web, LamuFlix.Worker, and the dependent legacy LamuFlix.Test project from the solution build. Legacy app/test files remain for replacement work (Web: DEV-388; Worker: DEV-314/315/316), and Tests.Common moves to Infrastructure. The legacy Web UI and legacy enrichment worker are non-functional from this merge until their replacements land; the excluded legacy test suite no longer runs in the solution.

The spec, plan and tasks describe this proposed scope (Q7 option A) and nothing else (Q10b). If the owner declines:

- Gate 1 stays closed.
- Bernstein and Rigger work out the sequencing (option D).
- Keel reopens this brief, and the single spec/plan/tasks set is revised.

There is no second tasks path, and implementation never switches branches silently.

## Closing bar

- **AC1 (migration):** a single `Initial` migration applies cleanly against an empty `postgres:16.4` Testcontainers database.
  - Applied migration IDs are exactly that Initial ID.
  - `HasPendingModelChanges()` is false.
  - The schema and index metadata queried from PostgreSQL match Q2/Q5/P2: seven application tables, snake_case columns, and the P2 nullable set (Q5 plus `enriched_at`, `enrichment_failure_category`, `last_attempt_at`). Join tables have a composite primary key on their FK columns.
  - Indexes: `title`, `release_year`, `status`, unique `library_path`, and unique `imdb_id WHERE imdb_id IS NOT NULL`.
  - EF's history table is counted separately and is not an application table (Q9).
- **AC2 (no MySQL):**
  - No Pomelo package, `UseMySql` call, MySQL connection string, or MySQL migration remains in any project the solution builds.
  - The hard-coded `password=root` connection is gone (Q3).
- **AC3 (ADR):** `docs/adr/ADR-0003.md` is Proposed in the spec PR. The owner's merge makes it Accepted; no agent flips the status label (Q10a).
- **AC4 (mapping proof):**
  - A `MovieRecord` with Actor, Director and Genre links round-trips through real Postgres and is read back with a new context.
  - The all-null metadata case round-trips as null.
- **AC5 (converters):** unit tests cover every Q4 converter:
  - valid values;
  - invalid non-null values, which throw;
  - unknown failure-category codes, which throw;
  - status ints 0-3 accepted and any other status rejected;
  - null preserved.
- **AC6 (gates):** Roslyn, cyclomatic complexity (15, and the refactor pass at 6), InspectCode, `dotnet format --verify-no-changes`, the full `dotnet test`, mutation at `harness.yml` threshold 80 on Infrastructure, the vulnerable-package scan, and the property-test gate (P6).
  - Every gate passes on the changed `.cs` files.
  - A skipped gate is not a pass.

## Frozen scope

### Grill answers (short form; CONCLUSIONS.md is authoritative)

| Q | Ruling |
|---|---|
| Q1 | EF maps persistence records `MovieRecord`, `ActorRecord`, `DirectorRecord` and `GenreRecord` in Infrastructure. `MovieConfiguration` configures `MovieRecord` with VO converters. Skip navigations produce implicit join tables. The future repository maps records to and from Core `Movie`. This is not a new layer. |
| Q2 | Explicit lowercase snake_case through `ToTable` / `HasColumnName` / `UsingEntity`. Tables: `movies`, `actors`, `directors`, `genres`, `movie_actors`, `movie_directors`, `movie_genres`. No join CLR classes and no naming-convention package. The implied tables count as ticket tables. |
| Q3 | Delivery stops at: provider cleanup, the options-constructed DbContext, the design-time factory, records, configurations, the migration, ADR-0003, and the Testcontainers migration test. The factory reads only `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING`. If it is missing or blank the factory fails clearly, never echoes the value, and has no literal fallback. The repository and catalog (DEV-301/302), the atomic claim (DEV-301), and Api/Worker runtime DI (DEV-307/308) are out of scope. |
| Q4 | Metadata columns are flattened onto `movies` as nullable. `EnrichmentStatus` is stored as an int through an explicit `Value` converter (Pending=0, Enriched=1, NotFound=2, Failed=3, never renumbered). `EnrichmentFailureCategory` is stored as its `Code` in nullable `varchar(32)`: a read resolves the known singleton and rejects an unknown code. Converters read through the validating domain factory and throw on invalid non-null values. No `MapEnum` and no Core change. |
| Q5 | Nullable metadata columns: `metadata_title` text, `runtime_minutes` int, `release_year` int, `imdb_rating` numeric(3,1), `imdb_id` text, `rotten_tomatoes_rating` smallint, `meta_score` smallint, `plot` text (persists Synopsis), `poster_url` text. The two ratings and `poster_url` are persistence-only scalars because Core has no counterpart; a follow-up is routed for dedup. |
| Q6 | `Microsoft.EntityFrameworkCore` and `.Design` pinned to 10.0.12, `Npgsql.EntityFrameworkCore.PostgreSQL` to 10.0.3, local `dotnet-ef` to 10.0.12. Every Microsoft EF package stays aligned at 10.0.12. |
| Q7 | `blocked: structural` owner checkbox (above). |
| Q8 | Context, factory, configurations and migrations all live in `src/LamuFlix.Infrastructure/Persistence/`. Infrastructure is both target and startup project. The migration is tool-generated only: no hand edits, no `HasData`, no raw SQL. Any correction means regenerating the whole set. |
| Q9 | New `tests/LamuFlix.IntegrationTests` (xUnit v3 + Shouldly, following the existing central pins). Tests.Common is retargeted to Infrastructure. The fixture runs `postgres:16.4` only, with no RabbitMQ and no `EnsureCreated`, and state is isolated per test. Converter tests go in UnitTests. No new architecture rule. |
| Q10 | Keel drafts ADR-0003 as Proposed and cross-references CONCLUSIONS rather than amending the constitution. There is a single plan for Q7(A). |

### Files touched (Q7 A)

| File | Change |
|---|---|
| `Directory.Packages.props` | Remove Pomelo. EF Core and Design go to 10.0.12, Npgsql to 10.0.3, plus any other Microsoft EF pin present, aligned to 10.0.12. |
| `.config/dotnet-tools.json` | `dotnet-ef` goes to 10.0.12. No other tool entry changes. |
| `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` | Add `Npgsql.EntityFrameworkCore.PostgreSQL`, and `Microsoft.EntityFrameworkCore.Design` with `PrivateAssets=all`. |
| `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs` | New. Constructor takes `DbContextOptions<LamuFlixDbContext>`. `OnModelCreating` calls `ApplyConfigurationsFromAssembly`. No `OnConfiguring`. |
| `src/LamuFlix.Infrastructure/Persistence/LamuFlixDesignTimeDbContextFactory.cs` | New (Q3/Q8). |
| `src/LamuFlix.Infrastructure/Persistence/Records/` | New: `MovieRecord`, `ActorRecord`, `DirectorRecord`, `GenreRecord`. |
| `src/LamuFlix.Infrastructure/Persistence/Converters/` | New: one converter per VO and for status and category, plus a shared "throw when TryCreate fails" helper. |
| `src/LamuFlix.Infrastructure/Persistence/Configurations/` | New: `MovieConfiguration`, `ActorConfiguration`, `DirectorConfiguration`, `GenreConfiguration`. The join-table naming lives in `MovieConfiguration`. |
| `src/LamuFlix.Infrastructure/Persistence/Migrations/` | Generated: `<ts>_Initial.cs`, `<ts>_Initial.Designer.cs`, `LamuFlixDbContextModelSnapshot.cs`. |
| `src/LamuFlix.Data/**` | Delete the whole project: `LamuFlixContext`, models, MySQL migrations and csproj. |
| Solution file | Remove Data, Web, Worker and `tests/LamuFlix.Test`. Add `tests/LamuFlix.IntegrationTests`. |
| `src/LamuFlix.Web/LamuFlix.Web.csproj`, `src/LamuFlix.Worker/LamuFlix.Worker.csproj`, `tests/LamuFlix.Test/LamuFlix.Test.csproj` | Remove only the obsolete Data `ProjectReference`. No other edit; the files stay on disk for DEV-388 and DEV-314/315/316. |
| `tests/LamuFlix.Tests.Common/` | The csproj reference moves from Data to Infrastructure. `LamuFlixContextFactory.cs` is replaced by a factory for Infrastructure's `LamuFlixDbContext` built from explicit container-derived options, with no `EnsureCreated`. Add a Postgres-only fixture. `ContainerFixture.cs` is left as is, because the excluded legacy suite uses it. |
| `tests/LamuFlix.IntegrationTests/` | New project: `MigrationTests` (AC1) and `PersistenceRoundTripTests` (AC4). |
| `tests/LamuFlix.UnitTests/Persistence/` | New converter tests (AC5). |
| `docs/adr/ADR-0003.md` | New, drafted by Keel in the spec PR. |

Plan decisions not ruled by Patron (Keel, for planning):

- Actor, Director and Genre records use an `int` identity key and a required `name` text column. No indexes beyond the ticket's (care list 3).
- `MovieRecord.Id` uses the `MovieId` converter and the key type `MovieId.cs` defines.
- If the Roslyn or InspectCode gates flag generated migration files, mark `src/LamuFlix.Infrastructure/Persistence/Migrations/**` as `generated_code = true` in `.editorconfig`. Never hand-edit the migration, and never suppress per line.
- If the mutation gate does not already exclude that folder, Phase B raises `needs recon` for its mutate filter before implementation. *(Superseded by P1 below: the check now happens before implementation starts, not in Phase 5.)*

### Plan-challenge decisions (round 1, Keel; supersede anything above that conflicts)

- **P1: the mutation gate is settled before implementation (Sentry F1/F2).** Sentry cites `scripts/run-mutation.ps1:456,604` and `:91-114`. The script globs changed `src/**/*.cs` including Migrations, overwrites Stryker's `mutate` key (discarding any config exclusion), and admits every directly-referencing test project, so `LamuFlix.IntegrationTests` would run Docker-backed tests once per mutant (`coverage-analysis: off`). New task **T000** blocks Phase 2. Recon confirms the mechanism, then Patron rules on it: a change to `run-mutation.ps1` or `stryker-config.json` edits a file this ticket does not name (§2.3 item 6). The ruling must keep threshold 80, exclude `Persistence/Migrations/**` from the mutate set, and keep `LamuFlix.IntegrationTests` out of the per-mutant test run. It must never lower a threshold. If the fix belongs to a harness ticket, T030's mutation gate waits on it and is reported as blocked, never as a pass.
- **P2: the full `movies` column set (Compass F1, F2).** The Core `Movie` scalars (`Movie.cs:19-39`) are columns on the ticket's own `movies` table, so they are in scope. Names and types:
  - `id` `integer` identity (generated by default), primary key. `MovieRecord.Id` is `MovieId` through the `MovieId` converter with `ValueGeneratedOnAdd`. It is null before insert, and the store assigns it.
  - `title` `text` not null.
  - `library_path` `text` not null, through a `LibraryPath` converter, unique.
  - `format` `text` not null, through a `MediaFormat` converter that stores `Extension`.
  - `is_in_watchlist` `boolean` not null.
  - `status` `integer` not null (FR-006).
  - `enriched_at` `timestamptz` null.
  - `enrichment_attempts` `integer` not null.
  - `enrichment_failure_category` `varchar(32)` null (FR-006; the name already used in the spec's edge case).
  - `last_attempt_at` `timestamptz` null.

  The AC1 nullable set is therefore the nine Q5 columns plus `enriched_at`, `enrichment_failure_category` and `last_attempt_at`; every other `movies` column is not null. Converters are added for `LibraryPath` and `MediaFormat`. `DateTimeOffset` columns carry UTC (offset zero): Npgsql rejects a non-zero offset for `timestamptz`, tests use UTC values, and normalisation on write belongs to the DEV-301 repository.
- **P3: the insert path is proven early (Sentry F6).** Phase 3 adds a Docker-free model test. It builds the model with `UseNpgsql()` and no connection string, then asserts that the `movies` key has the `MovieId` converter and `ValueGenerated.OnAdd`, and that the columns, types and nullability match P2. T027 also asserts that the store assigned `Id.Value > 0` after `SaveChanges`.
- **P4: the fixture and isolation (Sentry F3/F4).** `Tests.Common` adds `PostgresFixture`, an `IAsyncLifetime` registered as an xUnit v3 assembly fixture by `LamuFlix.IntegrationTests`. That fixture alone starts and disposes one `postgres:16.4` container per test assembly. Each test gets a **fresh, uniquely named database** on that container, following the unique-name pattern of `LamuFlixContextFactory.cs:13`. Cleaning up a shared database is not isolation. `LamuFlix.IntegrationTests` never references `ContainerFixture`.
- **P5: design-time factory tests (Sentry F5).** They run in a dedicated collection with `DisableParallelization = true`, because they set and restore a process-global environment variable.
- **P6: gate completeness (Ledger H1/H2/M1, Sentry F7/F10).**
  - T030 adds the vulnerable-package scan (`harness.yml` `vulnerablePackages`, fail and transitive) and the property-test gate (`propertyTests: enabled`). Converters carry domain invariants, so Phase 2 adds FsCheck property tests: valid values round-trip provider→model→provider, and the status mapping is a bijection on 0-3. There is no opt-out.
  - T030 names the scripts: `run-vulnerable-packages.ps1` and `run-property-tests.ps1` (tests tagged `Category=Property`; "no tests tagged" counts as a FAIL for this ticket).
  - The web gate does not apply to DEV-19: no `web/` change, and `harness.yml` has `web.enabled: false`. T030/T031 do not list it.
  - Local full `dotnet test` requires Docker. A Docker outage is reported as "Could not run", never as a verdict.
  - A restore or build failure in T004 is a Phase 1 blocker reported to Keel, never worked around.
- **P7: negative checks (Compass F3).** T021 also verifies that no `EnsureCreated` call exists in any project the solution builds. The Pomelo/`UseMySql`/MySQL/`password=root` sweep is already in T021.
- **Rejected: Ledger M2** (an ArchitectureTests rule for no EF in Api). Q9 ruled against it (`CONCLUSIONS.md:73`): an assembly assertion is a new cross-ticket architecture rule and would not prove there is no runtime wiring. Review enforces the FR-015 boundary.
- **Rejected: Compass F5's database default for `enrichment_attempts`.** The column is not null (P2). EF always writes the CLR value, and `HasDefaultValue(0)` on a non-nullable `int` whose CLR default is also 0 creates the EF sentinel ambiguity. There is no store default.
- **Rejected: Ledger L1-L5** (minor conventions, the duplicated `postgres:16.4` tag, CPM pin naming). The report cites no `file:line` or failure scenario, so they are below the closing bar. The image-tag duplication is deliberate while `ContainerFixture.cs` stays untouched (Q9) and the legacy suite is excluded.
- **No action: Compass F4, Compass F6, Ledger M3.** P2 already covers F4 (`enriched_at` and `last_attempt_at` nullable). F6 confirms CHK005. T000 already covers M3.
- **Rejected: Sentry F8** (join-table uniqueness). EF's implicit skip-navigation join entity already has a composite primary key on its two FK columns, so duplicate links cannot be stored. T026 asserts that key instead.
- **Follow-up: Sentry F9** (idempotent `MigrateAsync`). This is routed to DEV-307, which owns runtime migration wiring. It is not added here.

### Analyze round 1 decisions (Keel; supersede anything above that conflicts)

Receipt: `artifacts/DEV-19/analyze-round-1.md`. Recon R1 (Wisp, base `05484f6`): only `tests/LamuFlix.Tests.Common/LamuFlixContextFactory.cs:21` in Tests.Common uses a Data type (`ContainerFixture.cs` uses none); only the legacy `tests/LamuFlix.Test/LamuFlix.Test.csproj:23` references Tests.Common, and UnitTests does not; the solution file is `LamuFlix.sln`.

- **A1 (F1): legacy retirement moves ahead of the first restore.** `Data.csproj` and `Worker.csproj` reference Pomelo. If T001 removes the Pomelo pin while they are still in the solution, the central-package-management restore in T004 fails. Gate 1 already requires the owner's Q7 answer before any implementation, so the separate Phase 4 gate adds nothing. The new order:
  - Phase 1 opens with the retirement: delete `src/LamuFlix.Data/**`; remove Data, Web, Worker and `tests/LamuFlix.Test` from `LamuFlix.sln`; remove only the Data `ProjectReference` from the Web, Worker and legacy Test csproj files.
  - Also in Phase 1: `LamuFlix.Tests.Common.csproj` switches its reference from Data to Infrastructure, and the legacy `LamuFlixContextFactory.cs` is deleted. It is the only Data-dependent file, and nothing in the built solution consumes Tests.Common.
  - Then come the package pins, the tool pin, the Infrastructure csproj, and the T004 restore and solution build.
  - The new context factory and `PostgresFixture` still land after the Phase 3 `LamuFlixDbContext` exists, before the migration phase. `ContainerFixture.cs` stays as is.
  - The T021 negative sweep stays after Phase 3.
- **A2 (F2):** the plan's Constitution Check also cites Principle VII (FR-003: no hard-coded connection fallback) and Coding Conventions (FR-006/FR-007: Enumerations persisted by `Value` and parsed with `TryFromValue`; value objects read through validating factories).
- **A3 (F3):** T021 adds a diff check. `git diff --name-only main...HEAD` touches nothing under `src/LamuFlix.Api/`, and under `src/LamuFlix.Worker/` only `LamuFlix.Worker.csproj`, with only the Data `ProjectReference` line removed (FR-015). CHK015 stays checked only once this task exists.
- **A4 (F4):** the Pomelo `PackageReference` in `LamuFlix.Worker.csproj` stays on disk, unpinned and unbuilt, for DEV-314/315/316. FR-012 allows only the Data `ProjectReference` edit. AC2 is scoped to projects the solution builds, so it holds. FR-012 and T019 disclose this.
- **A5 (F5):** the solution file is `LamuFlix.sln` in the plan and tasks.
- **A6 (F6):** AC6 above now lists the vulnerable-package scan and the property-test gate.

**Anything else is a follow-up issue, not a finding in this round.**

Explicitly out of scope:

- Any `IMovieRepository` or `IMovieCatalog` implementation and `TryClaimForEnrichmentAsync` (DEV-301, DEV-302).
- Api/Worker `AddDbContext`, connection-string keys, or user-secrets (DEV-307, DEV-308).
- Core changes, including MetaScore or RottenTomatoes VOs (follow-up routed).
- Rewriting or stubbing legacy Web and Worker code.
- MySQL data migration.
- A new architecture rule.

## Test strategy

- **UnitTests (no Docker):** converter behaviour for every mapped VO, status and category (AC5). Honour `TimeProvider` where a validating factory needs time.
- **IntegrationTests (Docker, runs in CI on ubuntu-latest, `ci.yml:15,31`, no filter):**
  - `MigrationTests`: fresh database, `MigrateAsync`, applied IDs, `HasPendingModelChanges`, `information_schema` and `pg_indexes` assertions (AC1).
  - `PersistenceRoundTripTests`: skip-navigation sets and the null-metadata case, read back with a new context (AC4).
- The design-time factory's missing or blank variable path gets a unit test that asserts a clear failure, and that the message never contains the value.

## Task ordering

All implementation starts only after the owner has answered the Q7 checkbox (Gate 1). Order revised by A1:

1. Legacy retirement (Q7 A): delete Data; apply the solution exclusions; remove the Data references; retarget the Tests.Common csproj to Infrastructure and delete the legacy `LamuFlixContextFactory.cs`. Then set the package and tool pins and the Infrastructure csproj (Q6), and run restore and a solution build.
2. Converters and their unit tests (Q4, AC5).
3. Records, configurations, DbContext and design-time factory (Q1, Q2, Q5, Q8).
4. Tests.Common: the new context factory and `PostgresFixture`; then the negative sweep and solution build (T021).
5. Generate `Initial` with the Q8 command.
6. IntegrationTests project, Postgres fixture, migration test and round-trip test (Q9).
7. All gates (AC6); the refactor pass at complexity 6.

ADR-0003 ships in the spec PR, not in implementation.

## Round cap

- Analyze: 2 rounds.
- Plan challenge: 1 round.
- Review: 2 rounds, with at most 2 fix commits per round.
- The review closing bar is Critical or High, with a concrete failure scenario.
- On an L ticket, adjudication needs all three axis reports (Sentry, Ledger, Compass).

## Human gate 1

Closed until the owner answers the Q7 checkbox on the spec PR. A finished grill or a clean analysis does not open it. The owner merges; no agent merges.
