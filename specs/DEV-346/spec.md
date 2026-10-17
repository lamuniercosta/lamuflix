# Feature Specification: Enable the pgvector Extension in Existing PostgreSQL Persistence

**Feature Branch**: `feature/346-spec`

**Created**: 2026-10-17

**Status**: gate1: provisional

**Input**: DEV-346 (size M, parent DEV-288). Decision sources: `brief.md`, `CONCLUSIONS.md` Q1-Q7, `ASSUMPTIONS.md`. Facts: notes `recon-DEV-346` and `recon-DEV-346-2` at base `7bc803be582161094e3ec2dc00aab24e53647ebc`, and `specs/PRODUCT.md`. Ticket text in note DEV-346 is authoritative where the brief cites it. This draft opens no owner checkbox. Gate 1 is a later step.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The solution builds with the two approved vector packages (Priority: P1)

As the maintainer, I need the published vector packages pinned beside the existing Entity Framework and PostgreSQL provider so the extension can be configured, and so restore stays warning-clean.

**Why this priority**: Every later edit calls those packages. The pins and the direct-provider rule are Q3.

**Independent Test**: Restore and build the projects that reference the packages. The two approved versions are present, the existing Entity Framework and provider versions are unchanged, and each project that references the Entity Framework plugin also references the provider directly.

**Acceptance Scenarios**:

1. **Given** central package management, **When** the pins are added, **Then** `Pgvector` is `0.3.2` and `Pgvector.EntityFrameworkCore` is `0.3.0`, and Entity Framework Core stays `10.0.13` and `Npgsql.EntityFrameworkCore.PostgreSQL` stays `10.0.3`.
2. **Given** those pins, **When** Infrastructure and Tests.Common are restored, **Then** each references both approved packages and still references the provider directly.
3. **Given** a project that references `Pgvector.EntityFrameworkCore`, **When** it is restored, **Then** it also has a direct `Npgsql.EntityFrameworkCore.PostgreSQL` reference, and the unpublished `10.0.0` package is absent.
4. **Given** UnitTests only gains direct `UseVector` calls, **When** its references are updated, **Then** it references `Pgvector.EntityFrameworkCore` and keeps its existing direct provider reference. **Given** IntegrationTests also runs the vector-value proof, **Then** it references both approved packages and gains the direct provider reference it does not have today (`recon-DEV-346:28-30`).

---

### User Story 2 - Tests and Compose use one immutable pgvector image (Priority: P1)

As the operator, I need the legacy fixture, the integration fixture, and the Compose PostgreSQL service on the same digest-pinned image that already contains the vector extension.

**Why this priority**: A stock PostgreSQL image has no `vector.control`, so the extension migration fails there (`recon-DEV-346:95`, `recon-DEV-346-2:42-43`). Q4 supersedes the DEV-325 Compose PostgreSQL image line only.

**Independent Test**: Read the three image sites. They are the same pin. Compose credentials, volumes, ports, healthcheck, and RabbitMQ are unchanged. The legacy fixture's RabbitMQ behavior is unchanged.

**Acceptance Scenarios**:

1. **Given** the three sites, **When** the image is set, **Then** each is `pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d`.
2. **Given** `docker-compose.yml`, **When** the postgres image line changes, **Then** the existing environment-variable secret contract, database name, volume, ports, `pg_isready` healthcheck, and `rabbitmq:4-alpine` service stay as they are.
3. **Given** `ContainerFixture`, **When** its PostgreSQL image changes, **Then** its RabbitMQ image and start/dispose behavior stay as they are.
4. **Given** the historical DEV-325 spec documents, **When** this work lands, **Then** those documents are unchanged.

---

### User Story 3 - Runtime, design time, and tests share provider-owned vector options (Priority: P1)

As the maintainer, I need every retained database-options build to enable the vector plugin the same way, with the provider owning the driver registration and the data-source lifetime, and the model declaring the extension.

**Why this priority**: Q5. A model-only build must stay connectionless. A new wrapper or a hand-built data source is outside the ruling.

**Independent Test**: Read each named options site and the model. Connection-bearing sites pass `UseVector` with their existing connection argument. The two model-only sites pass `UseVector` and still omit a connection string. The model declares the `vector` extension. No new type sits between the caller and the provider.

**Acceptance Scenarios**:

1. **Given** runtime composition, the design-time factory, and both Tests.Common factory builds, **When** options are built, **Then** each connection-bearing call uses the provider-owned `UseVector` plugin and the design-time factory still reads `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING`.
2. **Given** the retained direct `UseNpgsql` calls in the named integration and unit tests, **When** they are updated, **Then** each gains `UseVector` and keeps its existing connection argument, including the unused-host argument where that argument already exists.
3. **Given** the model-only builds in `MovieModelTests` and `ModelMetadataTests`, **When** they are updated, **Then** they use `UseVector` and remain connectionless.
4. **Given** `LamuFlixDbContext`, **When** the model is built, **Then** it declares `HasPostgresExtension("vector")` and gains no vector column, vector entity, or `movie_features` table.

---

### User Story 4 - An additive extension-only migration joins the published history (Priority: P1)

As the maintainer, I need one new migration that enables the extension, generated by the Entity Framework tools, leaving the two published migrations byte-for-byte intact.

**Why this priority**: Q1 and Q6. "First migration" means the first vector-enablement migration ahead of DEV-413. The name `EnableVector` is assumption A1.

**Independent Test**: Diff the migrations folder. Only the new `EnableVector` migration, its designer, and the regenerated model snapshot change. The generated up path is the extension statement. The generated down path is the tool default and contains no added cascade. The snapshot matches the extension declaration and has no vector column.

**Acceptance Scenarios**:

1. **Given** the model declares the extension, **When** the tools scaffold `EnableVector`, **Then** they write the migration, the designer, and the model snapshot, and no generated file is hand-edited.
2. **Given** the published files `20260930153520_Initial` and `20261016195556_AddMovieWriters`, **When** the scaffold finishes, **Then** those files and their designers are unchanged.
3. **Given** the generated migration, **When** its up path is read, **Then** it creates the extension with `CREATE EXTENSION IF NOT EXISTS vector` and adds no table, column, or index. **When** its down path is read, **Then** it is the tool default and has no custom `CASCADE`.
4. **Given** the scaffold emits a table, column, index, or `movie_features` operation, **When** that is noticed, **Then** the work stops for a new ruling. The generated file is left for that ruling.

---

### User Story 5 - Real PostgreSQL proves the extension, the upgrade, and a tableless vector round-trip (Priority: P1)

As the maintainer, I need proof on real PostgreSQL that a fresh database and an upgraded database gain the extension, that existing data and tables survive, and that a vector value survives a parameterized round-trip only when the driver plugin is registered.

**Why this priority**: Q6 is the acceptance proof. Existing suite behavior stays; the migration-count assertion and these proofs are the forced edits from Q1.

**Independent Test**: Against the pinned image, run the proofs in `MigrationTests`, `PersistenceCompositionTests`, and `PostgresFixtureTests`, then run the existing unit, integration, and legacy container suites.

**Acceptance Scenarios**:

1. **Given** an empty database, **When** it migrates to latest, **Then** `pg_extension` has a `vector` row, the ordered migration names end in `Initial`, `AddMovieWriters`, `EnableVector`, the public table catalog is the catalog already asserted by `MigrationTests`, and there are no pending model changes.
2. **Given** a database migrated through `AddMovieWriters` with rows already stored, **When** it upgrades to `EnableVector`, **Then** those rows and the public table catalog remain, and there are no pending model changes.
3. **Given** that upgraded database, **When** the default down path runs back to `AddMovieWriters` and `EnableVector` is applied again, **Then** both steps succeed. The proof does not add a cascade and does not claim an extension-drop result beyond what the generated down path does.
4. **Given** the runtime composition and the shared test factory, **When** each sends a parameterized `Pgvector.Vector` and reads it back, **Then** the value matches and the test leaves no vector table behind.
5. **Given** the design-time factory, **When** its options and model are compared with runtime and with the generated migration, **Then** design time uses `UseVector`, the model declares the same extension, and the migration SQL is that extension statement.
6. **Given** the existing unit, integration, and legacy `ContainerFixture` suites, **When** they run, **Then** they pass with only the Q1 and Q5 forced edits and the proofs in this story.

---

### Edge Cases

- Phase B pickup finds a migration other than `Initial` and `AddMovieWriters`, or a `UseNpgsql` call outside the recon-2 list. Stop and return through the decision process. Do not edit the new caller in this round.
- The scaffold writes anything other than the extension operation. Stop. Do not hand-edit the generated migration to force it back into scope.
- A project references `Pgvector.EntityFrameworkCore` and omits a direct provider reference. Restore must fail closed on the resulting dependency conflict rather than be silenced.
- A model-only options build gains a connection string. That build no longer matches Q5.
- The down path is edited to add `CASCADE`. That edit is outside Q6.
- The three image sites disagree, or any Compose setting other than the postgres image line changes.
- A proof creates a durable vector table or a vector column on an existing table.
- A required gate exits 1 or cannot run. That blocks. A scope-empty skip, a harness opt-out, or a mutation not-applicable result stays recorded as that result.
- The property-test script's outcome is kept as printed. This ticket adds no property test and does not edit the harness to change that outcome.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `Directory.Packages.props` pins `Pgvector` `0.3.2` and `Pgvector.EntityFrameworkCore` `0.3.0` and leaves Entity Framework Core `10.0.13` and `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` as they are. The unpublished `10.0.0` plugin is not used.
- **FR-002**: `LamuFlix.Infrastructure` and `LamuFlix.Tests.Common` each reference both approved packages and keep a direct `Npgsql.EntityFrameworkCore.PostgreSQL` reference.
- **FR-003**: `LamuFlix.UnitTests` references `Pgvector.EntityFrameworkCore` for its retained direct `UseVector` calls and keeps its existing direct provider reference. It does not reference `Pgvector` for a vector-value proof, because no named UnitTests file owns that proof.
- **FR-004**: `LamuFlix.IntegrationTests` references `Pgvector` and `Pgvector.EntityFrameworkCore` for its retained `UseVector` calls and its vector-value proof, and it gains a direct `Npgsql.EntityFrameworkCore.PostgreSQL` reference.
- **FR-005**: Every project that references `Pgvector.EntityFrameworkCore` has a direct `Npgsql.EntityFrameworkCore.PostgreSQL` reference.
- **FR-006**: `ContainerFixture.cs`, `PostgresFixture.cs`, and the `postgres` service `image` line in `docker-compose.yml` use the digest pin in User Story 2. No other Compose line changes. `ContainerFixture` RabbitMQ behavior stays. DEV-325 spec documents stay.
- **FR-007**: Connection-bearing options at the runtime composition, the design-time factory, both Tests.Common factory builds, and the retained direct test builds call `UseVector` through the provider-owned plugin. The existing connection argument at each site stays. The design-time environment contract stays.
- **FR-008**: `MovieModelTests` and `ModelMetadataTests` call connectionless `UseVector`.
- **FR-009**: Call sites named by FR-007 and FR-008 are edited in place. No shared helper, manual `NpgsqlDataSourceBuilder`, or new project, folder, or layer is added.
- **FR-010**: `LamuFlixDbContext` declares `HasPostgresExtension("vector")`. The model gains no vector entity or column and no `movie_features` table.
- **FR-011**: Entity Framework tools scaffold the `EnableVector` migration, its designer, and the model snapshot. Generated files are not hand-edited. `20260930153520_Initial` and `20261016195556_AddMovieWriters`, including their designers, are not edited.
- **FR-012**: The generated up path is `CREATE EXTENSION IF NOT EXISTS vector` and nothing else. The generated down path stays the tool default, with no custom `CASCADE`. A scaffold that emits any other schema operation stops for a ruling.
- **FR-013**: On real PostgreSQL from the pinned image, a fresh migrate yields a `vector` extension row, the ordered names `Initial`, `AddMovieWriters`, `EnableVector`, the existing public table catalog, and no pending model changes.
- **FR-014**: A database at `AddMovieWriters` upgrades to `EnableVector` with existing rows and the public table catalog preserved and with no pending model changes. The default down path returns to `AddMovieWriters` and a reapply succeeds. The proof does not add an extension-drop guarantee beyond the generated down path.
- **FR-015**: Runtime composition and the shared test factory each round-trip one parameterized `Pgvector.Vector` with no permanent test table. Design-time options include `UseVector`, the design-time model declares the extension, and the generated migration SQL is that extension statement.
- **FR-016**: Proofs live in `MigrationTests`, `PersistenceCompositionTests`, and `PostgresFixtureTests`. Existing unit, integration, and legacy `ContainerFixture` suites pass with only the forced migration-list update, the forced options edits, and these proofs.
- **FR-017**: Restore and build of the touched projects stay warning-clean. Required gates run at the live harness thresholds. `run-property-tests.ps1` runs against the existing suites and its printed outcome is kept. No new FsCheck test is added. Mutation, when the live configuration applies it, and the web gate report their actual verdicts. Exit 1 and a gate that could not run block. Scope-empty skip, harness skip, and mutation not-applicable are recorded as those verdicts.
- **FR-018**: Out of scope for this ticket: recommendation behavior, stored embeddings, `movie_features` and DEV-413, any API, OpenAPI, generated TypeScript, or web change, `Features:LocalPlay`, secrets, `Process.Start`, any dependency beyond the two approved packages, any new project or layer, edits to published migrations, rewrites of historical specs including DEV-325, edits to `specs/PRODUCT.md`, a new ADR, a glossary change, optional Gherkin, and unrelated cleanup. Anything else is a follow-up, not a finding in this round.

### Requirement crosswalk

| Requirement | Story | Success criterion |
|---|---|---|
| FR-001, FR-002, FR-003, FR-004, FR-005 | US1 | SC-001 |
| FR-006 | US2 | SC-002 |
| FR-007, FR-008, FR-009, FR-010 | US3 | SC-003 |
| FR-011, FR-012 | US4 | SC-004 |
| FR-013, FR-014, FR-015, FR-016 | US5 | SC-005, SC-006 |
| FR-017 | US5 and close-out | SC-007 |
| FR-018 | all stories | SC-008 |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The four package projects restore and build with zero warnings and zero errors. The two pins are `0.3.2` and `0.3.0`. The Entity Framework and provider pins are unchanged. Each Entity Framework plugin reference has a matching direct provider reference.
- **SC-002**: The three image sites contain the same digest pin, and a diff of `docker-compose.yml` is confined to the postgres `image` line.
- **SC-003**: Every named options site calls `UseVector`, the two model-only sites have no connection string, and the model declares the `vector` extension with no vector column.
- **SC-004**: The migrations folder gains only `EnableVector`, its designer, and a regenerated snapshot. The two published migrations are unchanged. The up SQL is the extension statement and the down SQL is the untouched tool default.
- **SC-005**: Fresh migrate, upgrade from `AddMovieWriters`, down, and reapply behave as FR-013 and FR-014 on the pinned image.
- **SC-006**: The runtime path and the shared-factory path each return the same vector value they sent, and the database has no test-created vector table afterward.
- **SC-007**: The existing unit, integration, and legacy container suites pass, and each required gate's recorded exit matches FR-017.
- **SC-008**: The change set contains none of the FR-018 exclusions.

## Assumptions

- A1, from `ASSUMPTIONS.md`: the additive migration is named `EnableVector`. The timestamp, designer, and snapshot come from the tools. The name adds no schema.
- Q1-Q7 are Patron rulings recorded in `CONCLUSIONS.md`. This specification adds no further assumption.
- Fact citations are from `recon-DEV-346` and `recon-DEV-346-2` at `7bc803be582161094e3ec2dc00aab24e53647ebc`. Phase B re-pins that base. This draft is not a later-head receipt.
