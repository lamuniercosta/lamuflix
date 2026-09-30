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
  - The schema and index metadata queried from PostgreSQL match Q2/Q5: seven application tables, snake_case columns, and the Q5 nullable set.
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
- **AC6 (gates):** Roslyn, cyclomatic complexity (15, and the refactor pass at 6), InspectCode, `dotnet format --verify-no-changes`, the full `dotnet test`, and mutation at `harness.yml` threshold 80 on Infrastructure.
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
- If the mutation gate does not already exclude that folder, Phase B raises `needs recon` for its mutate filter before implementation.

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

1. Package and tool pins, and the Infrastructure csproj (Q6).
2. Converters and their unit tests (Q4, AC5).
3. Records, configurations, DbContext and design-time factory (Q1, Q2, Q5, Q8).
4. Legacy retirement: delete Data, apply the solution exclusions, remove the Data references, retarget Tests.Common (Q7 A, only once the owner checkbox is answered).
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
