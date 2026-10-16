# Implementation Plan: DEV-411 Ingest OMDb genres, cast, director, writers, Metascore and full plot

**Branch**: `feature/411-spec` | **Date**: 2026-10-16 | **Spec**: `specs/DEV-411/spec.md`

**Input**: Feature specification from `specs/DEV-411/spec.md`, settled grill brief `specs/DEV-411/brief.md`, facts from `recon-DEV-411`, Patron rulings in `specs/DEV-411/CONCLUSIONS.md`.

**Note**: Planning artifact only (Phase 2 step 3). No design challenge, gates, or implementation are started by this ask. Build waits until the separate spec PR merges and establishes Gate 1 (brief Q6).

## Summary

Extend the existing typed OMDb provider, Core `MovieMetadata`, and EF persistence so existing enrichment stores full plot, genres, directors, writers, actors, and Metascore. Add `writers` and `movie_writers` with one generated second migration. Hydrate the snapshot in the repository and the internal catalog projection. Keep the public details field set unchanged, and let the existing genre facet read the newly persisted genres. Prove mapping with WireMock and persistence with real Postgres.

## Technical Context

**Language/Version**: C# / .NET 10 backend. No web change.

**Primary Dependencies**: None added. Existing stack only: EF Core (Npgsql/PostgreSQL), xUnit, Testcontainers PostgreSQL, WireMock.Net.

**Storage**: PostgreSQL. Existing `plot` and `meta_score` movie columns stay. New tables are `writers` (`id`, `name`) and `movie_writers` (`writer_id`, `movie_id`). Genre, director, and actor tables already exist and are reused. No unique name index is added: `ActorConfiguration`, `DirectorConfiguration`, and `GenreConfiguration` map `name` as required `text` and do not declare one.

**Testing**: xUnit; Shouldly; existing `PostgresFixture` / `PostgresCollection`; existing `MetadataProviderProbe`. Property tests for the new collection equality behavior, because no property-test opt-out is recorded. Real Postgres for persistence. WireMock for the provider. No EF InMemory and no mocked data-access driver.

**Target Platform**: API host that already runs enrichment. No new host.

**Project Type**: Existing ports-and-adapters solution. No new project or layer.

**Performance Goals**: No target is set in the brief. No new browse query shape. The existing genre facet reads genres that enrichment now persists.

**Constraints**: TreatWarningsAsErrors. No new NuGet or npm dependency. No new project, port, handler, or route. No change to `Features:LocalPlay`, secrets, or `Process.Start`. No hand-edited migration designer or snapshot. No backfill. Public `MovieMetadataResponse` field set unchanged. Existing-table schema changes are out of bounds and stop for a ruling (brief Q5).

**Scale/Scope**: The file set in brief "Planned file boundaries". Migration filename and any new focused test filename are tooling or phase outputs, not names chosen here.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Ticket Scope is authoritative (`specs/PRODUCT.md` Authority Rules). The ingestion slice matches DEV-411 scope: `plot=full`, the five mapped fields, Core metadata, writers and `movie_writers`, WireMock, and real Postgres. Nothing is added or dropped. No owner checkbox (brief Q1, Q7).
- No new dependency, project, or layer (constitution I, lines 114–147; PRODUCT.md care list 1–2). Provider, repository, and catalog stay the existing adapter and persistence types. Race handling stays inside `EfMovieRepository` (brief Q5).
- No new handler, route, or status (constitution II, lines 149–166; V, lines 217–237). Enrichment still enters through the existing consumer and `MarkEnriched` (constitution IV, lines 190–215). Failure classification, retry, and claim behavior are untouched.
- Browse filters are untouched (constitution III, lines 168–188). The existing genre facet consumes persisted genres. `HasMetadata` is not redefined: recon records it as cast presence, and the brief does not change that rule.
- Public API shape stays inside the existing details field set (constitution API and Contract Rules, lines 483–495; brief Q2). No OpenAPI or generated TypeScript edit. Writers and per-movie genres stay internal.
- Database & migrations (constitution Technology Stack Constraints, line 342; specs/PRODUCT.md:23-24, 45, 48; CONCLUSIONS.md Q1, Q5, Phase 2 step 3.3): One IEntityTypeConfiguration<T> per entity (WriterConfiguration) and skip navigations (MovieRecord.Writers) are preserved. While constitution.md:342 notes the Epic 1 greenfield baseline as a "single fresh Initial migration" (20260930153520_Initial), ticket scope (DEV-411:21) explicitly mandates "EF persistence including new movie_writers join table and migration" and charter §2.3 item 6 (specs/PRODUCT.md:48) forbids rewriting tracked files not named by the ticket. By Patron Phase 2 step 3.3 ruling, appending one generated second migration for writers and movie_writers while leaving 20260930153520_Initial intact is an authorized incremental schema change under authoritative ticket scope (specs/PRODUCT.md:23-24, 45), not a constitution departure or owner checkbox.
- No secret, machine path, clock, or `Features:LocalPlay` change (constitution VII, lines 263–282). The OMDb key stays on the existing options path.
- Vocabulary classification (CONCLUSIONS.md Q7; brief Q7): Genre is the existing glossary term and is not expanded. Director, Writer, and Actor are role-specific literal credits and are not new glossary terms. No `CONTEXT.md` or ADR edit (constitution VIII, lines 284–302; Documentation Rules, lines 497–507). Vendor naming stays on the existing Infrastructure provider type.
- Tests use the existing pyramid: unit and property tests in `LamuFlix.UnitTests`, WireMock and Testcontainers in `LamuFlix.IntegrationTests` (constitution IX, lines 304–331). Gates are the configured scripts with no threshold change (Static-Analysis Gates, lines 400–420; PR Quality Gates, lines 368–398; brief Q6–Q7).

Post-design re-check: the design below adds no port, package, public field, or existing-table change. It adds no glossary term: Genre remains the existing glossary classification, and Director, Writer, and Actor remain role-specific literal credits (CONCLUSIONS.md Q7). The same gates hold.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-411/
├── spec.md
├── plan.md
├── tasks.md
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
└── checklists/requirements.md
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced. The brief already freezes approach, files, and order, and this ask writes the spec, plan, and tasks. There is no new public contract to put under `contracts/`.

### Source Code (repository root)

```text
src/LamuFlix.Core/Domain/MovieMetadata.cs
src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs
src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.Mapping.cs
src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs
src/LamuFlix.Infrastructure/Persistence/Records/MovieRecord.cs
src/LamuFlix.Infrastructure/Persistence/Records/WriterRecord.cs
src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs
src/LamuFlix.Infrastructure/Persistence/Configurations/WriterConfiguration.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/
    <generated second migration and designer>
    LamuFlixDbContextModelSnapshot.cs

tests/LamuFlix.UnitTests/Domain/
tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs
tests/LamuFlix.IntegrationTests/ApiEndToEndEnrichmentTests.cs
tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs
tests/LamuFlix.IntegrationTests/MigrationTests.cs
tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs
tests/LamuFlix.IntegrationTests/PersistenceRoundTripTests.cs
tests/LamuFlix.IntegrationTests/EfMovieRepositoryDeleteTests.cs
tests/LamuFlix.IntegrationTests/EfMovieCatalogDetailsTests.cs
tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs
tests/LamuFlix.IntegrationTests/PostgresFixtureTests.cs
tests/LamuFlix.Tests.Common/PostgresFixture.cs
```

**Structure Decision**: Existing projects only. Recon calls the context `LamuFlixContext.cs`; the worktree file is `LamuFlixDbContext.cs`, and that is the file this plan edits. `EfMovieCatalogFacetsTests.cs` is named because Q6 step 4 requires the existing genre facet proof and that file already owns it. `src/LamuFlix.Api/Endpoints/LibraryResponses.cs` stays as it is. No `/web` edit. `20260930153520_Initial.cs` is not edited.

## Design Overview

1. **Core.** Add optional `genres` and `writers` parameters after the existing `actors` parameter on `MovieMetadata`, typed as optional `IEnumerable<string>?`, so current call sites still compile. Store Genres and Writers as ordered `System.Collections.Immutable` collections: copy the supplied sequences into that storage, and expose that storage so neither the caller nor a cast can mutate the snapshot. Include Genres and Writers in the existing ordered value equality and per-element hash, on the same terms as Directors and Actors. Order is the order supplied after provider normalization. Empty and omitted collections are empty.
2. **Provider request.** `BuildUri` gains one `plot=full`. `apikey`, `t`, `type=movie`, and optional `y` stay. Both the year and no-year lookups send `plot=full`.
3. **Provider mapping.** Deserialize `Genre`, `Director`, `Writer`, `Actors`, and `Metascore` beside the fields already read. Split each collection on commas, trim, drop empty and blank tokens, and drop `N/A` with the existing ordinal `IsMissing` check. Dedupe case-insensitively, keeping the first spelling and first-seen order. Pass `Plot` through existing `MapSynopsis` (no new trim). Map Metascore onto the existing `short?` when it is a whole integer from 0 through 100; otherwise null. Invalid optional data does not fail a response that still has a title. Writer parentheticals stay literal.
4. **Lookup rows.** `WriterRecord` and `WriterConfiguration` follow `ActorRecord` / `ActorConfiguration`: table `writers`, integer identity `id`, required `text` `name`, no unique index. `LamuFlixDbContext.Writers` is the new set. `MovieRecord.Writers` is the new skip navigation. `movie_writers` uses the same `UsingEntity` shape as `movie_actors`, with `writer_id` and `movie_id`. Genres, directors, and actors keep their current tables. This change starts writing all four collections from the metadata snapshot; today `CopyMetadata` writes scalars only and tests seed joins by hand (recon).
5. **Snapshot writes.** On non-null metadata, replace that movie's four associations and Metascore. Resolve each role's names independently with a trimmed case-insensitive match; keep the stored casing; insert only new names. On null metadata, clear that movie's associations and the scalar metadata already cleared today, including synopsis and Metascore. Rehydration returns the full snapshot. Do not delete shared lookup rows on replace, clear, or movie delete. Do not add orphan cleanup.
6. **Concurrency.** Inside `EfMovieRepository`, each metadata-bearing write (`AddAsync` / `UpdateAsync`) runs in an explicit `READ COMMITTED` database transaction (`BeginTransactionAsync`, or the ambient transaction when one is already open). Before reading or inserting lookup rows, collect the distinct normalized keys `(role_id, lower(trim(name)))` for the snapshot in deterministic order — role order `1 = genres`, `2 = directors`, `3 = writers`, `4 = actors`, then ascending `StringComparer.Ordinal` on `lower(trim(name))` — and acquire transaction-scoped PostgreSQL advisory locks `SELECT pg_advisory_xact_lock(@roleId, hashtext(@normalizedName))` in that exact order. Under those locks, query existing lookup rows per role by trimmed case-insensitive match, reuse matching rows with stored database casing kept, insert only missing names, replace that movie's four join collections with deduplicated links, save, and commit. When this repository owns the transaction, retry transient concurrency conflicts (`PostgresException` SQLSTATE `40001` `serialization_failure`, `40P01` `deadlock_detected`, or `23505` `unique_violation` on a movie or join primary key) for at most 3 total attempts (1 initial + 2 retries) with a deterministic `25ms` then `50ms` delay. Each retry rolls the transaction back, calls `ChangeTracker.Clear()`, and replays the full unit of work from a fresh reload of `MovieRecord` and all four role collections. On attempt-3 exhaustion, a non-retryable error, or an ambient transaction, roll back, clear tracked state, and rethrow the original persistence exception so existing enrichment failure classification, retry, and claim handling stay unchanged. Do not add a process-wide lock, package, layer, unique name index, or any other change to existing tables. Do not treat a writer-only constraint as the race proof for genres, directors, and actors. `WriterConfiguration` stays without a unique name index (T008). If this mechanism cannot prevent duplicate lookup or join rows without an existing-table schema change, stop for a Patron ruling (brief Q5). Proof (T013): two separate database contexts and connections; controlled, evidenced overlapping contention on shared new names across all four roles; synchronization bounded so it remains compatible with these advisory-lock waits; a fresh context asserting one lookup identity per shared name per role and correct nonduplicate joins for both movies; and a bounded exhaustion proof that a retryable conflict rolls back, calls `ChangeTracker.Clear()`, and replays within the 3-attempt bound, while attempt-3 exhaustion, a non-retryable error, or an ambient transaction rethrows the original persistence exception with no partial rows committed. Existing enrichment failure policy stays unchanged. Basis: brief.md:47-50; CONCLUSIONS.md Phase 2 step 6.
7. **Migration.** Generate one second migration, its designer, and the updated `LamuFlixDbContextModelSnapshot.cs` with EF tooling (`LamuFlixDesignTimeDbContextFactory` is already present). Do not hand-edit those generated files. Do not edit Initial. Up creates the new tables only. Down drops `movie_writers`, then `writers`. Update `MigrationTests` so the table list includes `writers` and `movie_writers`, the applied set is Initial plus this second migration, an upgrade from Initial with existing movies fabricates no writer rows, and a rollback preserves preexisting tables and rows.
8. **Reads.** `EfMovieCatalog` internal metadata projection hydrates Genres, Directors, Writers, Actors, MetaScore, and full Synopsis. Leave `HasMetadata` as it is. Details HTTP mapping stays on `LibraryResponses.cs` and therefore still omits writers and per-movie genres. The existing genre facet query returns genres now stored on `movie_genres`.
9. **Fixture reset.** `PostgresFixture.BuildTruncate` already walks model entity tables. Extend `PostgresFixtureTests` so reset asserts writers and writer joins are empty. Change `PostgresFixture.cs` only if that walk omits `movie_writers`.
10. **Proof order.** Core tests, then WireMock mapping and the existing exact-key assertions, then migration and repository tests, then catalog and end-to-end import/enrichment. Refactor and configured gates come after the implementation phases, owned by Cog and Gauge, not by the implementation phases.

## Loop discipline

- **Closing bar:** zero verified Critical, High, or Medium findings inside the frozen scope before review clearance. Low is recorded and nonblocking. A mandatory gate failure or `Could not run` blocks. An unaccepted property-test exit 2 blocks. Scope-empty `SKIPPED`, mutation `NOT APPLICABLE`, and a configured `SKIP` stay distinct nonblocking results and are never recorded as PASS (brief Q7).
- **Round cap:** two review rounds, at most two fix commits per round. At the cap, unresolved above-bar findings go to user review on the PR. No third round and no claim of clearance.
- **Frozen scope:** Q1–Q6 provider query and mapping, Core metadata snapshots, the writers / `movie_writers` migration, complete EF write and read paths, the unchanged HTTP shape, and the integration and regression proof named in the brief. Anything else is a follow-up issue, not a finding in this round.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Additive second EF migration alongside Initial (constitution.md:342 note) | Mandated by authoritative ticket scope (DEV-411:21) and Patron §2.3 #3 ruling (CONCLUSIONS.md Q1, Q5, step 3.3) to add writers and movie_writers | Rewriting 20260930153520_Initial violates charter §2.3 item 6 (specs/PRODUCT.md:48) and breaks upgrade paths; ticket-mandated scope is an owner decision already made (specs/PRODUCT.md:23-24), not a constitution departure or owner checkbox |
