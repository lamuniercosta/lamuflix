# DEV-346 Grill conclusions

## Q1 — Migration history and unchanged-suite interpretation

Keel asked: Ticket says first migration runs CREATE EXTENSION vector, but recon 72-91 shows two existing migrations and exact migration-count tests. Recommend preserve published Initial/AddMovieWriters history and add a new extension-only migration, with the minimum migration-contract assertion update and new real-Postgres extension proof. Does this interpret first migration as first vector-enablement migration and unchanged suites as preserved existing behavior, or require a structural owner checkbox?

Patron answered:
[from Patron] Approved: preserve published `Initial` and `AddMovieWriters` migrations and add a new extension-only migration (`CREATE EXTENSION IF NOT EXISTS vector` / `HasPostgresExtension("vector")`), updating `MigrationTests.cs` migration-list assertions and adding real-Postgres extension proof; no structural owner checkbox is required.

- **First vector migration (`DEV-346:15`, `recon-DEV-346:73-76`)**: In `first migration runs CREATE EXTENSION vector; ... movie_features split to DEV-413`, "first migration" denotes the first pgvector migration ahead of `DEV-413`'s `movie_features` table migration; sound EF Core practice forbids mutating already-published migrations (`20260930153520_Initial`, `20261016195556_AddMovieWriters`).
- **Unchanged suites & forced test edit (`DEV-346:15`, `recon-DEV-346:84-91`, Charter §2.3 item 6)**: `unit and integration suites pass unchanged` requires existing suite behavior to pass without regression on the new pgvector image and `UseVector` data source; updating the hardcoded 2-migration count in `tests/LamuFlix.IntegrationTests/MigrationTests.cs:65-67` and adding real-Postgres extension verification are forced edits required by the AC (`fresh DB migration creates vector extension`) and test/mutation gates.
- **No owner escalation (`specs/PRODUCT.md:36-40,45,48`, Charter §2.3)**: Because this delivers the exact scope and acceptance criteria of `DEV-346:15` without departing from `.specify/memory/constitution.md:343`, it is a Patron structural ruling for `specs/DEV-346/CONCLUSIONS.md`, not `blocked: structural`.

Implication: additive extension migration only; published history preserved; forced migration test edits authorized; no owner checkbox.

---

## Q2 — Epic sequencing and infrastructure boundary

Keel asked: Recon 123-126 says PRODUCT defers Epic 9 until SPA completion, while accepted ADR-0012 sequences DEV-345/346 now. Recommend treating DEV-346 as ticket-authorized infrastructure groundwork only, delivering no recommendation behavior, embeddings storage or movie_features, and no PRODUCT rewrite. Confirm cited precedence and whether any constitution departure or owner checkbox exists.

Patron answered:
[from Patron] Approved: treat `DEV-346` as ticket-authorized pgvector infrastructure groundwork only (no recommendation behavior, embeddings storage, `movie_features` table, or `specs/PRODUCT.md` rewrite); no constitution departure or structural owner checkbox exists.

- **Ticket & ADR precedence (`specs/PRODUCT.md:23-24`, `DEV-346:15`, `docs/adr/ADR-0012.md` via `recon-DEV-346:125-126`)**: `specs/PRODUCT.md:23-24` establishes that a ticket's *Scope & Technical Design* is an owner decision already made, and accepted `ADR-0012` explicitly sequences `DEV-345` and `DEV-346` to implement the foundational decision while `DEV-346:15` splits `movie_features` to `DEV-413`.
- **Bounded groundwork scope (`DEV-346:15`, `specs/PRODUCT.md:17,48`)**: Delivering only package wiring, test fixture pgvector image alignment, `UseVector` data-source configuration, and the `CREATE EXTENSION vector` migration honors Epic 9 feature deferral (`specs/PRODUCT.md:17`) without rewriting `specs/PRODUCT.md` (an unnamed tracked file under `specs/PRODUCT.md:48`).
- **No constitution departure or owner checkbox (`.specify/memory/constitution.md:343` via `recon-DEV-346:11,114`, `specs/PRODUCT.md:36-40`)**: The constitution technology table already specifies `pgvector via Pgvector.EntityFrameworkCore`, so neither §2.3 escalation trigger (ticket contradiction or constitution departure) applies.

Implication: infrastructure groundwork only; no recommendation behavior, movie_features or PRODUCT rewrite; no owner checkbox or constitution departure.

---

## Q3 — Published package pins and reference placement

Keel asked: Recommend CPM pins Pgvector 0.3.2 and Pgvector.EntityFrameworkCore 0.3.0, keeping EF 10.0.13 and direct Npgsql EF 10.0.3 refs in every project adding the EF plugin. Recon-2 A verifies clean restore/build and EF10 runtime behavior; 10.0.0 is unpublished. Package refs only in projects needing APIs: Infrastructure and Tests.Common, plus UnitTests/IntegrationTests only for retained direct UseVector calls. Approve pins and placement rule, accepting the published net8 build with upstream EF10 support and requiring build/runtime regressions rather than an unpublished package?

Patron answered:
[from Patron] Approved: pin `Pgvector 0.3.2` and `Pgvector.EntityFrameworkCore 0.3.0` in `Directory.Packages.props` alongside existing EF Core `10.0.13` and `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3`, require a direct `Npgsql.EntityFrameworkCore.PostgreSQL` reference in every project referencing `Pgvector.EntityFrameworkCore`, and add package references only where their APIs are directly consumed (`LamuFlix.Infrastructure`, `LamuFlix.Tests.Common`, and `LamuFlix.UnitTests`/`LamuFlix.IntegrationTests` only if direct `UseVector` calls are retained).

- **Published CPM pins & EF 10 compatibility (`DEV-346:15`, `.specify/memory/constitution.md:343`, `recon-DEV-346-2:10-28,64-75,86`)**: `Pgvector 0.3.2` and `Pgvector.EntityFrameworkCore 0.3.0` are the latest published NuGet releases (`10.0.0` is an unmerged PR), upstream documents EF Core 9 and 10 support for `0.3.0`, and empirical probes confirm clean `net10.0` restore/build (`0` warnings/errors), MSBuild `build/net8.0` design-time target import, and runtime vector persistence against EF Core `10.0.13` + Npgsql EF `10.0.3`.
- **Direct Npgsql EF reference invariant (`Directory.Packages.props:12`, `specs/PRODUCT.md:12`, `recon-DEV-346-2:28-30`)**: Every `.csproj` that references `Pgvector.EntityFrameworkCore` must also carry a direct `PackageReference` to `Npgsql.EntityFrameworkCore.PostgreSQL` (`10.0.3`) so NuGet's lowest-applicable-version rule does not resolve `9.0.1` and fail `TreatWarningsAsErrors=true` with `NU1608`.
- **Minimal project placement (`recon-DEV-346:24-29`, `recon-DEV-346-2:74,78-82`, Charter §2.3 item 1)**: Reference `Pgvector` and `Pgvector.EntityFrameworkCore` in `LamuFlix.Infrastructure` (migrations + runtime/design-time `UseVector`) and `LamuFlix.Tests.Common` (`LamuFlixDbContextFactory`), adding `Pgvector.EntityFrameworkCore` (with direct `Npgsql.EntityFrameworkCore.PostgreSQL`) to `LamuFlix.UnitTests` or `LamuFlix.IntegrationTests` only if those projects retain direct `UseVector` calls.

Implication: no unpublished package or EF/provider version swap; direct provider reference required wherever EF plugin is referenced.

---

## Q4 — Exact image pin and compose dependency integration

Keel asked: Recommend pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d in both fixtures and docker-compose.yml postgres image only. Recon-2 B verifies PG17/vector 0.8.7, digest parsing and runtime; no alpine exists. Updating compose prevents runtime CREATE EXTENSION failure, but supersedes DEV-325 C13 postgres:17-alpine. Is this forced compose dependency integration authorized by DEV-346:15 rather than ticket expansion, retaining credentials, volumes, ports, healthcheck and RabbitMQ?

Patron answered:
[from Patron] Approved: pin `pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d` in `ContainerFixture.cs`, `PostgresFixture.cs`, and `docker-compose.yml:3` (`postgres` service `image:` only, retaining all existing compose env vars, volumes, ports, healthcheck, and `rabbitmq:4-alpine`); no structural owner checkbox is required.

- **Verified immutable PG17 + pgvector pin (`DEV-346:15`, `recon-DEV-346-2:36-47`)**: `pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d` is verified on PostgreSQL 17.11 with `vector 0.8.7` (no alpine variant exists in `pgvector/pgvector`), satisfies `AC: both fixtures same pinned image`, and parses cleanly in both Testcontainers 4.15.0 (`DockerImage.FullName`) and `docker compose config`.
- **Authorized compose image update (`DEV-346:15`, `recon-DEV-346:53-58,93`, `recon-DEV-346-2:42-43,48`, Charter §2.3 item 6)**: `DEV-346:15` explicitly cites `depends on DEV-325 for compose image` alongside `first migration runs CREATE EXTENSION vector`; updating `docker-compose.yml:3` from stock `postgres:17-alpine` (which lacks `vector.control`) to the pinned PG17 pgvector image is both ticket-named and forced so runtime database migration does not fail on `CREATE EXTENSION`, superseding `DEV-325` C13 (`postgres:17-alpine`) for the `postgres` image line only.
- **Strict compose boundary (`recon-DEV-346:53-58`, `specs/PRODUCT.md:36-40,48`)**: Leave `docker-compose.yml` credentials (`POSTGRES_USER`/`POSTGRES_PASSWORD`/`POSTGRES_DB`), volumes, ports, `pg_isready` healthcheck, `rabbitmq:4-alpine` (`docker-compose.yml:16`), and historical `specs/DEV-325/*` artifacts untouched; no ticket contradiction or constitution departure exists.

Implication: one identical digest pin at three sites; existing DEV-325 artifacts preserved; no owner checkbox.

---

## Q5 — Provider-owned vector wiring and forced option-build edits

Keel asked: Recommend UseNpgsql(connectionString, o => o.UseVector()) at runtime PersistenceServiceCollectionExtensions, design-time factory, both Tests.Common factory builds and retained direct option builds listed recon-2:78-82 (including model-only builds). Provider-owned data source plugin configures ADO UseVector; avoid manual data source, new abstraction and disposal ownership. Model HasPostgresExtension(vector) plus additive generated migration carries extension SQL. Is this sufficient for ticket UseVector on Npgsql data source without manual builder? Authorize forced narrow edits in listed test files/project refs under Q3, model-only options remaining connectionless; no vector entity/column or persistent test table.

Patron answered:
[from Patron] Approved: configure `UseNpgsql(..., o => o.UseVector())` via the provider-owned `INpgsqlDataSourceConfigurationPlugin` (no manual `NpgsqlDataSourceBuilder` or new abstraction), declare `modelBuilder.HasPostgresExtension("vector")` in `LamuFlixDbContext` with the additive generated migration, and apply forced narrow `o => o.UseVector()` edits across the runtime, design-time, `Tests.Common`, and retained test `UseNpgsql` sites (keeping model-only calls connectionless, with no production vector entity/column or persistent test table).

- **Provider-managed data-source `UseVector` (`DEV-346:15`, `specs/PRODUCT.md:18`, `recon-DEV-346-2:53-68`)**: In Npgsql EF 10, `UseNpgsql(connectionString, o => o.UseVector())` registers `VectorDataSourceConfigurationPlugin` (`INpgsqlDataSourceConfigurationPlugin`), which invokes `dataSourceBuilder.UseVector()` on the provider-cached `NpgsqlDataSource` and manages its disposal lifecycle—satisfying `enable UseVector on Npgsql data source` without a manual `NpgsqlDataSourceBuilder` or speculative wrapper.
- **Model & migration alignment without `movie_features` (`DEV-346:15`, `recon-DEV-346:82,89`, `recon-DEV-346-2:65,72-75`)**: Declaring `modelBuilder.HasPostgresExtension("vector")` in `LamuFlixDbContext.OnModelCreating` and scaffolding the additive extension-only migration (`AlterDatabase().Annotation("Npgsql:PostgresExtension:vector", ",,")` -> `CREATE EXTENSION IF NOT EXISTS vector;`) satisfies `first migration runs CREATE EXTENSION vector` and `HasPendingModelChanges().ShouldBeFalse()` while keeping `movie_features` deferred to `DEV-413` and adding no persistent test table.
- **Forced narrow edits at `UseNpgsql` sites (`recon-DEV-346-2:60,72-73,78-82`, Charter §2.3 items 1 & 6)**: Updating `PersistenceServiceCollectionExtensions.cs:31`, `LamuFlixDesignTimeDbContextFactory.cs:22`, `LamuFlixDbContextFactory.cs:30,85`, and retained direct `UseNpgsql` calls in `IntegrationTests`/`UnitTests` (`recon-DEV-346-2:82`, keeping model-only `UseNpgsql(o => o.UseVector())` connectionless per `NpgsqlDataSourceManager` arm 4) plus their Q3 package references is authorized as a forced consistency edit across runtime, design-time, and test `LamuFlixDbContext` options.

Implication: EF plugin owns driver configuration and source lifetime; model-only callers remain connectionless; no new architectural layer.

---

## Q6 — Migration reversibility and behavioral proof

Keel asked: Recommend EnableVector extension-only migration scaffolded by EF tools (designer/snapshot generated; published migrations untouched), default non-cascading Down and no custom CASCADE. Real-Postgres tests prove fresh migration and AddMovieWriters-to-latest upgrade create vector extension, preserve public-table catalog/data and no pending model drift; bounded Down-to-AddMovieWriters and reapply prove rollback before vector consumers exist. Parameterized Pgvector.Vector SQL roundtrip without persistent vector table detects missing ADO registration through runtime/shared factory paths; design-time scaffold/SQL generation proves parity. Existing suites run with only forced contract/wiring edits; use existing MigrationTests/PersistenceCompositionTests/PostgresFixtureTests homes.

Patron answered:
[from Patron] Approved: scaffold the additive `EnableVector` extension-only migration via EF Core tools with its default non-cascading `Down`, verify fresh migration, `AddMovieWriters`->`EnableVector` upgrade, bounded `Down`/re-apply rollback, `HasPendingModelChanges`, design-time parity, and a tableless parameterized `Pgvector.Vector` SQL round-trip against real Testcontainers Postgres inside existing `MigrationTests`, `PersistenceCompositionTests`, and `PostgresFixtureTests` homes (logging `EnableVector` migration naming `[assumed]` in `ASSUMPTIONS.md` as applicable).

- **EF-scaffolded `EnableVector` migration & non-cascading `Down` (`DEV-346:15`, `recon-DEV-346:72-76`, `recon-DEV-346-2:71-75`, `AGENTS.md` Non-negotiables)**: Scaffolding `EnableVector` from `modelBuilder.HasPostgresExtension("vector")` generates the migration, `.Designer.cs`, and `LamuFlixDbContextModelSnapshot.cs` without hand-editing generated files, preserves published `Initial`/`AddMovieWriters` history, and keeps the standard non-cascading `Down` (no custom `CASCADE`, safe before `DEV-413` adds `movie_features`).
- **Real-Postgres extension, rollback & parameterized `Vector` round-trip proof (`DEV-346:15`, `recon-DEV-346:84-93`, `recon-DEV-346-2:64-74`, `AGENTS.md` Non-negotiables)**: Real Testcontainers assertions for fresh migration and `AddMovieWriters`->`EnableVector` upgrade (`pg_extension` `vector` row present, unchanged public-table catalog/data, `HasPendingModelChanges().ShouldBeFalse()`, bounded `Down`-to-`AddMovieWriters` and re-apply) plus a tableless parameterized `Pgvector.Vector` SQL round-trip across runtime/shared-factory contexts and design-time SQL/options parity directly verify the AC and catch missing ADO-level `UseVector()` registration (`recon-DEV-346-2:68` negative control E).
- **Existing test homes & unchanged suites (`DEV-346:15`, `recon-DEV-346:98,105`, `specs/PRODUCT.md:18,28-29,44,48`)**: Extending `MigrationTests.cs`, `PersistenceCompositionTests.cs`, and `PostgresFixtureTests.cs` avoids new test projects/fixtures while running existing unit, integration, and `LamuFlix.Test` (`ContainerFixture`) suites with only the Q1/Q5 forced contract/wiring edits.

Implication: migration is tool-generated and non-cascading; behavioral proof uses real Postgres without adding domain tables; migration name is a taste assumption.

---

## Q7 — Loop terms, gate obligations and grill closure

Keel asked: Recommend Critical/High/Medium block closure, Low non-blocking; max two review rounds and two fix commits per round, unresolved blockers stay blocked at cap. Frozen scope Q1-Q6 only: packages/provider wiring, three image sites, extension-only migration and forced tests; exclude recommendations, embeddings storage, movie_features/DEV-413, API/web/LocalPlay, abstractions, historical migration/spec rewrites and unrelated cleanup; anything else is a follow-up issue, not a finding in this round. Phase B order: Wisp drift pin + Gauge baseline, package/image + wiring/model, generated migration, proofs and suites, Cog refactor, Gauge gates, all three review axes. All required gates/applicable mutation mandatory, thresholds unchanged. Proposed property-test opt-out because no generated domain invariant, preserving script outcome. No optional Gherkin, new glossary or ADR (size M; ADR-0012 decides store). Confirm terms and shared understanding to close.

Patron answered:
[from Patron] Approved and ratified to close the `DEV-346` grill: confirm the standing loop terms, frozen Q1–Q6 scope, Phase B execution order, no optional Gherkin/glossary/ADR, and no new FsCheck property tests for this infrastructure/migration scope while keeping `harness.yml` untouched and running `run-property-tests.ps1` with its script outcome retained.

- **Standing loop terms & frozen scope (`DEV-346:9,15`, `specs/PRODUCT.md:17-18,36-48`, Q1–Q6)**: Critical/High/Medium findings block closure and Low is non-blocking (max 2 review rounds, max 2 fix commits per round, unresolved blockers stay blocked at cap); scope is frozen to Q1–Q6 (`Pgvector` CPM/project wiring, 3 pinned image sites, `UseVector` + `HasPostgresExtension("vector")`, additive `EnableVector` migration, and forced/proof tests), excluding recommendations, `movie_features`/`DEV-413`, API/web/`LocalPlay`, abstractions, and historical migration/spec rewrites.
- **Verification gates, mutation & property-test scope (`AGENTS.md` Non-negotiables & Verification commands, `specs/PRODUCT.md:12`)**: All required gates and applicable Stryker mutation testing remain mandatory with unchanged `harness.yml` thresholds; no new FsCheck property tests are required because package/image/extension-migration wiring introduces no pure domain invariant, while `run-property-tests.ps1` still executes against existing suites with its script outcome reported as-is (`harness.yml` is not edited).
- **Phase B sequence & artifact boundaries (`DEV-346:9`, `docs/adr/ADR-0012.md` via `recon-DEV-346:125`)**: Proceed with the proposed Phase B order (Wisp drift pin + Gauge baseline -> package/image + wiring/model -> generated `EnableVector` migration -> real-Postgres proofs + existing suites -> Cog refactor -> Gauge gates -> 3-axis review), with no optional Gherkin, new domain glossary, or new ADR since `DEV-346` is `size:M` and `ADR-0012` already governs pgvector storage.

Implication: grill closed; no new FsCheck property tests required, but existing property-test script still runs and its result is retained. No harness edit, waiver or gate-PASS claim. Quill may draft; plan challenge, plan freeze and Gate 1 remain separate.

---

## Q8 — Phase 2 step 6 plan-challenge adjudication (Sentry, Ledger, Compass)

Keel asked: DEV-346 Phase 2 step 6 adjudication: all three reports present, 1 Medium and 6 Low. Request cited ruling on bounded corrections: accept Sentry M1 as a documented migration precondition that the migration connection can create vector when absent; no role provisioning, privilege automation, managed-host support or new negative test delivered. Accept Sentry L1: T026 requires session LAMUFLIX_DESIGN_TIME_CONNECTION_STRING supplied securely, never committed or printed, before scaffold. Consolidate Sentry L2/Ledger L2/Compass L2: T030 stays in existing PostgresCollection with finally restoration; process-global override must not overlap any factory/env reader or writer, and Phase B recon checks that assumption, stopping for a ruling on new consumers. Accept Ledger L1: T004-T008 use dotnet add package with Q3 exact pins and direct provider invariant, no manual project edits or constitution deviation. Accept Compass L1: T032/brief include authorized US2 image edits. No scope expansion, new tests or file boundary changes; append ruling in CONCLUSIONS and brief before plan/tasks edits. Confirm authority and cited bases for these decisions; plan challenge readiness remains separate from your Gate 1 ruling.

Patron answered:
[from Patron] Approved: ratify the bounded Phase 2 step 6 plan-challenge corrections for Sentry M1/L1/L2, Ledger L1/L2, and Compass L1/L2 with no scope expansion, new tests, file-boundary changes, constitution deviation, or owner checkbox; append this ruling in `CONCLUSIONS.md` and `brief.md` before updating `plan.md` and `tasks.md`, with Gate 1 remaining separate.

- **Migration privilege precondition & design-time env handling (`findings-DEV-346-Sentry:8-64`, `findings-DEV-346-Ledger:31-44`, `findings-DEV-346-Compass:30-32`, `DEV-346:15`, `CONCLUSIONS.md` Q1/Q4/Q5/Q6/Q7)**: Document in `plan.md` that the migration connection must hold privilege to execute `CREATE EXTENSION IF NOT EXISTS vector` when absent (satisfied by fixture bootstrap superusers and `docker-compose.yml:5` `POSTGRES_USER`, while least-privilege roles require out-of-band pre-provisioning; no role provisioning, privilege automation, managed-host support, or negative test added); require a session-only, never-committed/never-printed `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` in `T026` before scaffolding (`LamuFlixDesignTimeDbContextFactory.cs:14-19`, Charter §2.3 secrets rule); and keep `T030` in `PostgresCollection` with `finally` restoration on the verified premise that no concurrent collection reads/writes that variable or factory, with Phase B recon (`T001`) checking and stopping for a ruling if any new consumer exists (`aaron-csharp-testing.mdc:22`).
- **Package CLI conformance & US2 test-source boundary alignment (`findings-DEV-346-Ledger:13-29`, `findings-DEV-346-Compass:26-28`, `CONCLUSIONS.md` Q3/Q4)**: Specify `dotnet add package` in `T004`–`T008` with the exact Q3 pins (`Pgvector 0.3.2`, `Pgvector.EntityFrameworkCore 0.3.0`) and direct `Npgsql.EntityFrameworkCore.PostgreSQL` (`10.0.3`) reference invariant rather than manual project edits (`.specify/memory/constitution.md` Coding Conventions, `aaron-dotnet-sdk-dependency-management.mdc:32-33`), and include the authorized US2 fixture image-pin edits (`ContainerFixture.cs`, `PostgresFixture.cs`, `T010`–`T011`) in `T032` (`tasks.md:116`) and `brief.md`.
- **Patron authority, artifact order & Gate 1 separation (`specs/PRODUCT.md:36-48`, Charter §2.3, `CONCLUSIONS.md` Q7)**: All five corrections are bounded plan/task clarifications within Patron's authority and the frozen Q1–Q7 scope/file boundary (neither a ticket contradiction nor a constitution departure); record this ruling in `specs/DEV-346/CONCLUSIONS.md` and `specs/DEV-346/brief.md` before `plan.md`/`tasks.md` edits, while Gate 1 (`gate1: provisional` in `spec.md`) remains a separate step after the edits and verification close.

Implication: bounded plan-challenge corrections authorized across brief.md, plan.md, and tasks.md; no scope, test, or file-boundary expansion; Gate 1 remains separate.

---

## Gate 1 — Provisional ruling

Conductor asked: Run Phase 2 step 7 Gate 1 provisional ruling for DEV-346 after Keel's clean `/speckit-analyze` recheck and frozen Phase 2 step 6 plan-challenge adjudication under Q8.

Patron answered:
[from Patron] Approved: mark `**Status**: gate1: provisional` in `specs/DEV-346/spec.md:7` with zero open owner checkboxes; `DEV-346` spec artifacts (`spec.md`, `plan.md`, `tasks.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`) are cleared for Rigger to commit and open the spec PR (`DEV-346:15`, `specs/DEV-346/CONCLUSIONS.md` Q1–Q8, `DEV-346` note Phase 2 steps 3.3 & 6).
