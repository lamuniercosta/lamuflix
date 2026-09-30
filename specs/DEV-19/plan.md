# Implementation Plan: PostgreSQL persistence via EF Core

**Branch**: `feature/19-spec` | **Date**: 2026-09-30 | **Spec**: `specs/DEV-19/spec.md`

**Input**: `specs/DEV-19/spec.md`, `brief.md`, `CONCLUSIONS.md` (Q1-Q10), `ASSUMPTIONS.md`, `docs/adr/ADR-0003.md`

## Summary

Replace MySQL/Pomelo with PostgreSQL through EF Core in `LamuFlix.Infrastructure`: persistence records, per-entity configurations with explicit snake_case names, validating converters, a `DbContextOptions`-constructed `LamuFlixDbContext`, an environment-driven design-time factory, and one tool-generated `Initial` migration. Prove it with Docker-free converter unit tests and Testcontainers `postgres:16.4` migration and round-trip tests. Scope is Q7 option A; **implementation is blocked until the owner answers the Q7 checkbox** (see spec).

## Technical Context

**Language/Version**: C# 14 on .NET 10
**Primary Dependencies**: `Microsoft.EntityFrameworkCore` 10.0.12, `.Design` 10.0.12 (`PrivateAssets=all`), `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3, local tool `dotnet-ef` 10.0.12 (`.config/dotnet-tools.json`)
**Storage**: PostgreSQL 16+ (tests: Testcontainers `postgres:16.4`)
**Testing**: xUnit v3 + Shouldly (central pins); UnitTests (Docker-free) and new `tests/LamuFlix.IntegrationTests` (Docker)
**Target Platform**: library (Infrastructure); CI `ubuntu-latest` (`ci.yml:15,31`, no filter)
**Constraints**: no hand-edited migration; no `EnsureCreated`; no secrets or literal connection strings; complexity <= 15 (refactor pass 6); mutation >= 80 on Infrastructure
**Scale/Scope**: 4 records, 4 configurations, ~9 converters/helper, 1 migration, 1 test project, 1 fixture

## Constitution Check

- I (Clean layering): Infrastructure depends on Core; records and mapping stay inside the adapter; no new project except the test project (care list 2 approved, Q9), no new layer (Q1).
- IV: `CONTEXT.md:38-40` distinguishes the Movie Aggregate from the EF entity; reconciled in Q1, cross-referenced not amended (Q10). Status ints fixed 0-3.
- IX (real infrastructure): migration and round-trip tests run against real Postgres.
- VII (FR-003): the design-time connection comes from the environment only, with no hard-coded fallback.
- Coding Conventions (FR-006/FR-007): Enumerations are persisted by `Value` and parsed with `TryFromValue`; value objects are read through validating factories.
- Departures: none beyond the Q7 owner checkbox.

## Project Structure

```text
Directory.Packages.props                         # remove Pomelo; EF 10.0.12, Npgsql 10.0.3
.config/dotnet-tools.json                        # dotnet-ef 10.0.12
src/LamuFlix.Infrastructure/
  LamuFlix.Infrastructure.csproj                 # + Npgsql provider, + EF Design (PrivateAssets=all)
  Persistence/
    LamuFlixDbContext.cs
    LamuFlixDesignTimeDbContextFactory.cs
    Records/    MovieRecord, ActorRecord, DirectorRecord, GenreRecord
    Converters/ one per VO + status + category + throw-on-failed-TryCreate helper
    Configurations/ MovieConfiguration (join tables), ActorConfiguration, DirectorConfiguration, GenreConfiguration
    Migrations/ <ts>_Initial.cs, <ts>_Initial.Designer.cs, LamuFlixDbContextModelSnapshot.cs   # generated
src/LamuFlix.Data/**                             # DELETE
tests/LamuFlix.Tests.Common/                     # reference Data -> Infrastructure; context factory replaced; Postgres fixture added
tests/LamuFlix.IntegrationTests/                 # NEW: MigrationTests, PersistenceRoundTripTests
tests/LamuFlix.UnitTests/Persistence/            # NEW: converter + design-time factory tests
LamuFlix.sln                                     # remove Data, Web, Worker, tests/LamuFlix.Test; add IntegrationTests
```

Legacy `Web`, `Worker`, `tests/LamuFlix.Test` csproj files lose only their Data `ProjectReference`; sources stay on disk (DEV-388, DEV-314/315/316). `Tests.Common/ContainerFixture.cs` is untouched.

## Design Decisions

1. **Records, not Core (Q1)**: EF maps `*Record` types; `MovieConfiguration` configures `MovieRecord` with VO converters; a future repository maps records to Core `Movie`.
2. **Naming (Q2)**: explicit `ToTable`, `HasColumnName`, and `UsingEntity` with join-table and FK column names; no CLR join classes, no naming package.
3. **Metadata flattened (Q4/Q5)**: nine nullable scalar columns on `movies` with the exact types in FR-005; all-null represents absent metadata; relationships kept independently. The Core `Movie` scalars (`id`, `title`, `library_path`, `format`, `is_in_watchlist`, `status`, `enriched_at`, `enrichment_attempts`, `enrichment_failure_category`, `last_attempt_at`) follow FR-005a (brief P2); `DateTimeOffset` values are UTC.
4. **Converters (Q4)**: `EnrichmentStatus` <-> int via `Value`; `EnrichmentFailureCategory` <-> `varchar(32)` via `Code`; VO converters (`MovieId`, `LibraryPath`, `MediaFormat`, `ImdbId`, `ImdbRating`, `Runtime`, `ReleaseYear`) call validating factories through one helper that throws on failure. FsCheck property tests cover the round-trip and the status bijection (brief P6). Honour `TimeProvider` where a factory needs time.
5. **Context and factory (Q3/Q8)**: options-constructed context, `ApplyConfigurationsFromAssembly`, no `OnConfiguring`; factory reads `LAMUFLIX_DESIGN_TIME_CONNECTION_STRING` only.
6. **Migration (Q8)**: generated from repo root with the Q8 command, Infrastructure as target and startup project; a correction means changing configurations and regenerating all three files.
7. **Ancillary keys (plan decision)**: Actor/Director/Genre use `int` identity and required `name`; `MovieRecord.Id` is `MovieId` (int) through its converter with `ValueGeneratedOnAdd` on an identity column; a Docker-free model test proves the key metadata in Phase 3 (brief P3).
8. **Generated code**: if analyzers flag Migrations, mark the folder `generated_code = true` in `.editorconfig`; never per-line suppress or hand-edit.
9. **Test fixture (Q9, brief P4)**: `PostgresFixture` in Tests.Common, registered as an xUnit v3 assembly fixture, owns one `postgres:16.4` container per test assembly; each test creates a fresh uniquely named database; options built from the container; no `EnsureCreated`; `ContainerFixture` is never used by IntegrationTests. Design-time factory tests run in a non-parallel collection (brief P5).

## Ordering and Risks

Order follows `brief.md` task ordering. All implementation follows Gate 1 (the Q7 answer). Legacy retirement (delete Data, solution exclusions, Data reference removal, Tests.Common retarget) now opens Phase 1, before the package pins and the first restore, because the Pomelo references in Data and Worker would break the central-package-management restore once the Pomelo pin is removed.

- Mutation gate (brief P1, Sentry F1/F2): `run-mutation.ps1` includes Migrations and overwrites `mutate`, and would admit IntegrationTests into the per-mutant run. T000 (recon, then a Patron ruling on the file edit) settles this before Phase 2; the threshold stays 80.
- Local full `dotnet test` needs Docker; an outage is "Could not run". A restore or build failure in T004 blocks Phase 1 and goes to Keel.
- Removing Data breaks `Tests.Common` and legacy `tests/LamuFlix.Test` references: handled in Phase 1, ahead of the first restore (Tests.Common retargeted to Infrastructure, legacy `LamuFlixContextFactory.cs` deleted); the legacy suite is intentionally excluded and disclosed. Worker's Pomelo `PackageReference` stays on disk, unpinned and unbuilt, for DEV-314/315/316.
- Provider 10.0.3 requires EF Core `[10.0.4, 11.0.0)`; 10.0.12 satisfies it (Q6). Restore/build not yet run.
- The `MovieId` key is `int` (`MovieId.cs:8,31`, value > 0); each other VO's factory signature is read from Core in T005.

## Round Caps

Analyze 2, plan challenge 1, review 2 (max 2 fix commits per round; Critical/High with concrete failure scenario); L ticket adjudication needs Sentry, Ledger and Compass reports.
