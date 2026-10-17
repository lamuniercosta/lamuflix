# Implementation Plan: Enable the pgvector Extension in Existing PostgreSQL Persistence

**Branch**: `feature/346-spec` | **Date**: 2026-10-17 | **Spec**: [spec.md](spec.md)

**Input**: [spec.md](spec.md), [brief.md](brief.md), [CONCLUSIONS.md](CONCLUSIONS.md) Q1-Q7, [ASSUMPTIONS.md](ASSUMPTIONS.md), notes `recon-DEV-346` and `recon-DEV-346-2`, `specs/PRODUCT.md`.

## Summary

Enable the `vector` extension in the existing PostgreSQL database. Pin `Pgvector` `0.3.2` and `Pgvector.EntityFrameworkCore` `0.3.0` beside the current Entity Framework Core `10.0.13` and `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` pins. Point both test fixtures and the Compose postgres image at one digest-pinned pgvector image. Configure `UseVector` through the provider-owned plugin at every retained options site, declare `HasPostgresExtension("vector")` on the model, and scaffold an additive `EnableVector` migration. Prove fresh migrate, upgrade, down/reapply, catalog stability, design-time parity, and a tableless vector round-trip on real PostgreSQL. Existing library tables and published migrations stay.

**Note on the Spec Kit phases**: research, the data model, contracts, and the quickstart are folded into this plan. No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md` is produced. The package, image, and wiring choices are already ruled in Q3-Q6 from recon-2; this ticket adds no domain entity, no API route, and no vector column. `.specify/extensions.yml` is absent, so no extension hook ran. There is no agent-context script under `.specify/`; `AGENTS.md` is outside the Phase A artifact set in `brief.md` and is not edited.

## Technical Context

**Language/Version**: C# on .NET 10 (`recon-DEV-346-2:78`, every cited csproj is `net10.0`).

**Primary Dependencies**: existing central pins `Microsoft.EntityFrameworkCore` `10.0.13`, `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3`, `Testcontainers.PostgreSql` `4.15.0` (`recon-DEV-346:18-22`). Approved additions: `Pgvector` `0.3.2`, `Pgvector.EntityFrameworkCore` `0.3.0`. The plugin's published build is `net8.0` and recon-2 A verified it restores and runs on this `net10.0` stack. `Pgvector.EntityFrameworkCore` `10.0.0` is unpublished (recon-2 A.3).

**Storage**: existing PostgreSQL. The migration enables the `vector` extension. No new table or column.

**Testing**: xUnit v3, Shouldly, Testcontainers PostgreSQL in `LamuFlix.Tests.Common`. Proof homes: `MigrationTests`, `PersistenceCompositionTests`, `PostgresFixtureTests`. No new FsCheck test. No EF Core InMemory provider. No mocked driver translation.

**Target Platform**: the repo's existing test and Compose hosts. The image is Debian PostgreSQL 17.11 with `vector` `0.8.7` (`recon-DEV-346-2:41-42`). No Alpine pgvector tag exists.

**Project Type**: existing .NET solution. No new project.

**Performance Goals**: none in the brief. None added.

**Constraints**: warning-clean restore (`TreatWarningsAsErrors`); direct provider reference wherever the Entity Framework plugin is referenced (NU1608 otherwise, recon-2 A.4); generated migration files stay tool-owned; published migrations stay untouched; no new abstraction over the options edits; secrets stay in the existing Compose environment-variable contract and are not copied into artifacts.

**Scale/Scope**: the file table below. Recon base `7bc803be582161094e3ec2dc00aab24e53647ebc`. Line numbers in recon are citations at that base, re-pinned by Wisp before edits.

## Constitution Check

*Initial and post-design. No violation is proposed, so Complexity Tracking stays empty.*

- Principle I (boundaries): no new project, folder, port, or layer. Core does not gain an EF or Pgvector reference. The options edits stay in Infrastructure and the tests that already build `UseNpgsql`. Q5 forbids a manual data source and a new wrapper.
- Principle VII (configuration): the design-time factory keeps `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING`. Compose keeps its required environment-variable interpolation. No secret is written into the spec set.
- Principle VIII (language): `EnableVector` is the A1 name. No glossary edit (Q7).
- Principle IX (tests): extension and mapping proof uses Testcontainers PostgreSQL. Driver serialization is not mocked. No new FsCheck test; the existing property-test script still runs (Q7).
- Technology table, vector row (`constitution.md:343`): pgvector via `Pgvector.EntityFrameworkCore` is the mandated package. Q2 records no constitution departure. The row's HNSW note is Recommendations Phase B and stays out of this ticket with `movie_features` (Q2, FR-018).
- Technology table, database row: the repo already has `Initial` and `AddMovieWriters` (`recon-DEV-346:76-77`). Q1 preserves both and adds the extension migration, and records no owner checkbox.
- API and contract rules: no route, DTO, OpenAPI document, or generated TypeScript change (FR-018).
- Documentation rules: ADR-0012 already governs the store (`recon-DEV-346:127`). Q7 adds no ADR. `CONTEXT.md`, `README.md`, and `specs/PRODUCT.md` stay.
- Static-analysis gates: Roslyn, complexity (gate 15, refactor gate 6), and InspectCode stay mandatory. Thresholds are read from the live harness and are not edited.
- Charter §2.3: the two packages, the extension migration, and the narrow edits of files the ticket names are the Q1-Q6 rulings. No item is left as a silent assumption, and no owner checkbox is open.

Post-design: the same checks hold. The migration is extension-only, the image change is the one Compose line Q4 names, and the test edits are the forced list in the file table.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-346/
|- brief.md, CONCLUSIONS.md, ASSUMPTIONS.md   (Keel / Patron)
|- spec.md                                     (/speckit-specify)
|- plan.md                                     (/speckit-plan)
|- tasks.md                                    (/speckit-tasks)
|- checklists/requirements.md
`- DRAFTING_RECEIPT.md
```

### File boundary (from `brief.md`)

Package and project references:

- `Directory.Packages.props` — two version pins; existing EF and provider versions unchanged
- `src/LamuFlix.Infrastructure/LamuFlix.Infrastructure.csproj` — both packages; keep the direct provider reference
- `tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj` — both packages; keep the direct provider reference
- `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj` — `Pgvector.EntityFrameworkCore` for retained `UseVector` calls; keep the direct provider reference at `:13`; do not add `Pgvector`
- `tests/LamuFlix.IntegrationTests/LamuFlix.IntegrationTests.csproj` — both packages, plus a direct provider reference (absent at recon, `recon-DEV-346:28-30`)

Image sites, one literal at each site, no shared constant:

- `docker-compose.yml` — postgres `image` line only
- `tests/LamuFlix.Tests.Common/ContainerFixture.cs` — PostgreSQL image only
- `tests/LamuFlix.Tests.Common/PostgresFixture.cs` — PostgreSQL image only

Options and model:

- `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` — runtime `UseNpgsql(connectionString, o => o.UseVector())`; health-check registration stays
- `src/LamuFlix.Infrastructure/Persistence/LamuFlixDesignTimeDbContextFactory.cs` — the same options shape; environment contract stays
- `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs` — `HasPostgresExtension("vector")`
- `src/LamuFlix.Infrastructure/Persistence/Migrations/` — new tool-generated `EnableVector` migration and designer, plus the regenerated snapshot
- `tests/LamuFlix.Tests.Common/LamuFlixDbContextFactory.cs` — `OpenContext` and `CreateAdminContext`
- `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs`
- `tests/LamuFlix.IntegrationTests/EfMovieRepositoryClaimConcurrencyTests.cs`
- `tests/LamuFlix.IntegrationTests/EfMovieCatalogPagingTests.cs`
- `tests/LamuFlix.IntegrationTests/MovieCatalogRegistrationTests.cs` — keep the existing unused-host connection argument
- `tests/LamuFlix.UnitTests/Persistence/MovieModelTests.cs` — connectionless `UseNpgsql(o => o.UseVector())`
- `tests/LamuFlix.UnitTests/Persistence/ModelMetadataTests.cs` — connectionless
- `tests/LamuFlix.UnitTests/Persistence/EfMovieRepositorySurvivorTests.cs`
- `tests/LamuFlix.UnitTests/Persistence/EfMovieRepositoryConstructorTests.cs`

Proofs:

- `tests/LamuFlix.IntegrationTests/MigrationTests.cs`
- `tests/LamuFlix.IntegrationTests/PersistenceCompositionTests.cs`
- `tests/LamuFlix.IntegrationTests/PostgresFixtureTests.cs`

No deletion and no full-file rewrite. A caller or a migration file outside this list stops the edit and returns through recon and a Patron ruling.

**Structure Decision**: edits stay inside the existing Infrastructure, Tests.Common, UnitTests, IntegrationTests, and Compose layout. `tests/LamuFlix.Test` keeps its current behavior aside from the `ContainerFixture` image it already consumes.

## Design

### Packages

Use `dotnet add <project> package <PackageId> --version <Q3 pin>` to add the two central `PackageVersion` entries and project references; do not hand-edit the project files. Run T004-T008 sequentially because the CLI can write the shared `Directory.Packages.props`. Do not change the Entity Framework `10.0.13` lines or the provider `10.0.3` line. Do not add a floating version.

Reference placement follows Q3:

| Project | Pgvector | Pgvector.EntityFrameworkCore | Direct Npgsql EF provider |
|---|---|---|---|
| Infrastructure | add | add | already present; keep |
| Tests.Common | add | add | already present; keep |
| UnitTests | do not add | add | already present; keep |
| IntegrationTests | add | add | add; absent at recon |

### Image

Use this exact value at the three sites:

`pgvector/pgvector:0.8.7-pg17@sha256:ac08538c6f8b9904c33c8224c5e5706dbe760aca29db1d096972b4052c22a75d`

Leave `ContainerFixture`'s RabbitMQ builder and Compose's `rabbitmq:4-alpine` service on their current images. Those two RabbitMQ tags differ today and stay different.

### Options

Connection-bearing sites take `UseVector` on the existing `UseNpgsql` call. The provider plugin registers the ADO type mapping and owns the cached data source (`CONCLUSIONS.md` Q5). Model-only sites in `MovieModelTests` and `ModelMetadataTests` use `UseNpgsql(o => o.UseVector())` and stay connectionless (`recon-DEV-346-2:60`).

Retained direct sites, from recon-2 C.5, are the only test edits for options consistency:

- `EfMovieRepositoryTests.cs:893`
- `EfMovieRepositoryClaimConcurrencyTests.cs:34`
- `EfMovieCatalogPagingTests.cs:69`
- `MovieCatalogRegistrationTests.cs:17`
- `MovieModelTests.cs:154`
- `ModelMetadataTests.cs:77`
- `EfMovieRepositorySurvivorTests.cs:1441`
- `EfMovieRepositoryConstructorTests.cs:16`
- `LamuFlixDbContextFactory.cs:30` and `:85`
- `PersistenceServiceCollectionExtensions.cs:31`
- `LamuFlixDesignTimeDbContextFactory.cs:22`

### Migration

Scaffold `EnableVector` with the Entity Framework tools after the model declaration and the design-time `UseVector` call are in place. The tools write the migration, the designer, and `LamuFlixDbContextModelSnapshot.cs`. Expected up shape, from the provider generator cited in recon-2 C.4 and from Q1: `AlterDatabase().Annotation("Npgsql:PostgresExtension:vector", ",,")` generating `CREATE EXTENSION IF NOT EXISTS vector`. Down stays that scaffold's default. Do not type `CASCADE` into it.

If the scaffold also emits a `CreateTable`, a column, an index, or `movie_features`, stop. Do not delete those operations by hand.

Migration precondition (Q8): when `vector` is absent, the migration connection must have privilege to execute `CREATE EXTENSION IF NOT EXISTS vector`. Fixture bootstrap superusers and the current Compose `POSTGRES_USER` satisfy this deployment premise; a least-privilege application role requires out-of-band extension pre-provisioning. Provisioning roles, automating grants, managed-host support and an additional negative test are outside this delivery. A missing privilege is a migration failure, never a successful proof.

Before scaffold, supply `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` only for the tool session, securely and without committing or printing its value; preserve the factory environment contract (Q8).

### Proofs

`MigrationTests` updates the ordered-name assertion from length 2 (`recon-DEV-346:88-90`) to `Initial`, `AddMovieWriters`, `EnableVector`, and covers fresh migrate, upgrade from `AddMovieWriters` with existing rows, default down and reapply, the unchanged public-table catalog assertion, and `HasPendingModelChanges() == false`.

`PersistenceCompositionTests` covers runtime registration, the tableless parameterized round-trip through runtime composition, and design-time options/model/SQL parity.

The design-time parity proof remains in the existing `PostgresCollection`, restores the prior process-global environment value in `finally`, and must not overlap another collection reading or writing that variable or invoking the factory. Collection serialization alone does not exclude other collections: T001 has Wisp recheck the sole-reader/writer premise and stop for a ruling if it changed (Q8).

`PostgresFixtureTests` covers the same tableless round-trip through the shared factory (`LamuFlixDbContextFactory`).

The round-trip asserts value equality. It does not create a mapped vector column. It does not assert a dimension the brief does not name; any single `Pgvector.Vector` value that compares equal is enough.

### Gates

Read thresholds from the live `harness.yml`. Do not edit that file.

Required close-out, each recorded with its real exit:

- `./scripts/run-roslyn-analyzers.ps1`
- `./scripts/run-cyclomatic-complexity.ps1` at the live gate, then `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` after the refactor pass
- `./scripts/run-jetbrains-inspectcode.ps1`
- `dotnet format --verify-no-changes`
- `./scripts/run-vulnerable-packages.ps1`
- `./scripts/run-property-tests.ps1` on the existing suites; keep the printed outcome; add no FsCheck test
- `pwsh -NoProfile -File ./scripts/run-mutation.ps1` when the live configuration includes a changed project; record PASSED, FAILED, SKIPPED, or NOT APPLICABLE
- `dotnet test`
- the pipeline web gate, recorded as its actual scope verdict

Exit 1 and a gate that could not run block. SKIPPED (scope-empty), SKIP (harness opt-out), and mutation NOT APPLICABLE are not recorded as pass. Later-head gates after rebase stay required by the pipeline and are not waived here.

### Loop

Unresolved Critical, High, or Medium findings block closure. Low findings are recorded and do not block. Maximum two review rounds and two fix commits per round. Unresolved blockers stay blocked at the cap. Sentry, Ledger, and Compass all review. A round is finished when the findings and the summary are on the PR. This plan does not merge.

## Phase order

1. Wisp re-pins the base and the file boundary. Gauge records the baseline at that pin.
2. Package pins and project references, and the three image sites.
3. Runtime, design-time, and test `UseVector` edits, plus the model extension declaration.
4. Scaffold `EnableVector`. Inspect the generated operations before any suite is treated as the proof.
5. Forced migration assertions and the real-PostgreSQL proofs, then the existing suites.
6. Cog refactors the implementation diff. Gauge runs the gates. Sentry, Ledger, and Compass review.

## Complexity Tracking

No constitution violation is proposed. This section is empty.
