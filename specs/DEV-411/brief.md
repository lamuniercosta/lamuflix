# DEV-411 — Phase 2 grill brief

Status: grill closed; seven questions of the twelve-question cap. Patron accepted Q1–Q7. Quill may draft spec.md, plan.md and tasks.md. This brief is the decision source, with full exchanges and cited Patron rulings in CONCLUSIONS.md; taste conventions are tagged [assumed] in ASSUMPTIONS.md. No owner checkbox, ticket contradiction, or constitution departure was identified. No production code has been changed by this grill.

## Identity and evidence

- Ticket: DEV-411, Ingest OMDb genres, cast, director, writers, Metascore and full plot into metadata; size:M; parent DEV-288.
- Worktree: F:/Dev/LamuFlix.worktrees/feature-411-spec.
- Branch: feature/411-spec.
- Recon/base: aff9856b62f7ae217ba0c06587b11e968a3d80a3, matching origin/main at intake.
- Evidence: DEV-411 note:19-21; recon-DEV-411:1-77 in LamuFlix notes. All current implementation facts come from that recon; cited product/source references in CONCLUSIONS.md are Patron-provided bases, not new independent recon.
- Missing implementation facts must go to Conductor as `needs recon: <question>`; no speculative inventory. Quill returns `needs decision:` for a decision gap.

## Frozen scope and approach — Q1

Extend the existing typed OMDb provider, Core MovieMetadata and EF persistence to ingest full plot, genres, directors, writers, actors and Metascore on existing enrichment triggers. Add writers and movie_writers with an additive migration. Prove mapping with WireMock and persistence with real Postgres. Use existing projects, layers, libraries and fixtures. No UI, provider replacement, backfill/refresh job, playback, secret-handling, new dependency or new abstraction layer. Existing library entries gain these values only through existing enrichment behavior.

Basis: DEV-411:21; recon-DEV-411:6,10-17,21-37,41,65-70; Patron Q1.

## Public contract and hydration — Q2

Keep public routes, status behavior and MovieMetadataResponse field set unchanged. Existing Synopsis, MetaScore, Directors and Actors reflect persisted enrichment. Writers and per-movie Genres remain internal: no new HTTP DTO fields, browse widening, OpenAPI, generated TypeScript or frontend edits. Existing genre facets consume the newly persisted genres.

Extend Core metadata with Genres and Writers; hydrate all four collections (Genres, Directors, Writers, Actors), MetaScore and full Synopsis in EfMovieRepository rehydration and EfMovieCatalog internal metadata projections. Do not stop at provider mapping or hand-seeded join rows.

Basis: recon-DEV-411:21-25,36-37,47,68; Patron Q2 explicitly rules on charter §2.3 item 4.

## OMDb request and mapping — Q3

- Always send one `plot=full`; preserve `apikey`, `t`, `type=movie` and optional `y` behavior.
- Deserialize Genre, Director, Writer, Actors and Metascore alongside existing response fields.
- Comma-split each collection; trim tokens; remove empty, blank and N/A values; case-insensitively deduplicate, preserving first spelling and first-seen order [assumed]. Missing collections become empty.
- Preserve parenthetical writer credits literally [assumed]. A Writer is a role-specific literal credit, not an inferred person identity or structured credit parser.
- Metascore: invariant whole integer from 0 through 100, represented by the existing nullable Core score type; missing, blank, N/A, malformed or out-of-range becomes null. Invalid optional data must not fail otherwise valid enrichment.
- Feed full Plot through existing synopsis handling (MapSynopsis); preserve existing required-field and other provider behavior.

Basis: recon-DEV-411:12-14,21,43-46,54; Patron Q3. Assumed conventions are recorded in ASSUMPTIONS.md.

## Domain and persistence semantics — Q4

Add optional Genres/Writers constructor parameters without breaking existing callers. Use defensive immutable collection copies and deterministic order, with value equality/hash treatment consistent with Directors/Actors. Full-snapshot semantics apply to all four collections and MetaScore.

Saving changed metadata replaces that movie's associations. Resolve lookup identity independently per role using trimmed case-insensitive names; existing database-record casing wins [assumed]. Reuse existing rows and insert only genuinely new names. Repeated enrichment is idempotent. Concurrent enrichment of separate movies sharing names must not produce duplicate lookup or join rows.

Clearing metadata removes that movie's associations. Movie deletion cascades its join rows and preserves shared lookup records. No global orphan cleanup, role reclassification or deletion of shared credits. Repository rehydration and catalog projection return complete metadata snapshots.

Database-backed race handling stays within `EfMovieRepository` with no existing-table schema changes, process-only lock, new dependency, or new layer:
- **Transaction boundary & lock order**: Run each metadata-bearing repository write (`AddAsync` / `UpdateAsync`) in an explicit `READ COMMITTED` DB transaction (`BeginTransactionAsync`, or reuse an ambient transaction if already open). Collect distinct normalized keys `(role_id, lower(trim(name)))` across the snapshot in deterministic order — role order `1 = genres`, `2 = directors`, `3 = writers`, `4 = actors`, then ascending `StringComparer.Ordinal` on `lower(trim(name))` — and acquire transaction-scoped PostgreSQL advisory locks `SELECT pg_advisory_xact_lock(@roleId, hashtext(@normalizedName))` in that exact order before reading or inserting lookup rows.
- **Normalized identity & snapshot write under lock**: After acquiring the ordered transaction locks, query existing lookup rows per role by trimmed case-insensitive match (`lower(trim(name))`), reuse matching rows with stored DB casing winning [assumed], insert only missing names, replace the movie's four join collections with deduplicated links, save, and commit.
- **Retryable conflicts, finite bound, rollback/replay & exhaustion**: When `EfMovieRepository` owns the transaction, retry transient DB concurrency conflicts (`PostgresException` SQLSTATE `40001` `serialization_failure`, `40P01` `deadlock_detected`, or `23505` `unique_violation` on movie/join PK) up to **3 total attempts** (1 initial + 2 retries, deterministic `25ms` then `50ms` delay). Each retry rolls back the transaction, calls `ChangeTracker.Clear()`, and replays the full unit of work from a fresh reload/reconstruction of `MovieRecord` and all four role collections. On attempt-3 exhaustion, non-retryable errors, or ambient transactions, roll back, clear tracked state, and rethrow the original persistence exception so existing enrichment failure classification, retry, and claim handling operate unchanged.

Basis: recon-DEV-411:21-25,30-37,60; adjudication-DEV-411:18-33,55; Patron Q4–Q5 and Phase 2 step 6 ruling.

## Schema and migration — Q5

Add WriterRecord and WriterConfiguration, writers (id, name), Writers DbSet, MovieRecord.Writers navigation and movie_writers (writer_id, movie_id), following existing lookup/join conventions. Add one second EF migration; generate its designer and model snapshot with EF tooling. Do not hand-edit generated artifacts.

Keep the Initial migration, existing tables and movie columns intact: meta_score and plot already exist. Up preserves existing movies and does not fabricate writer associations. Down drops movie_writers before writers while retaining preexisting tables and data. Update table-list and single-migration expectations, fixture reset and cascade tests to match the additive model.

Basis: DEV-411:21; recon-DEV-411:30-35,59-60,67; Patron Q1/Q5 deliberately approves the writers parent table under §2.3 item 3.

## Planned file boundaries

Paths below are relative to this worktree. This is the intended file set, not a claim that build changes already exist.

- Core: src/LamuFlix.Core/Domain/MovieMetadata.cs; focused metadata value-object tests in the existing test project.
- Provider: src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs and OmdbMetadataProvider.Mapping.cs.
- Persistence: src/LamuFlix.Infrastructure/Persistence/LamuFlixContext.cs, Records/MovieRecord.cs, new Records/WriterRecord.cs, Configurations/MovieConfiguration.cs, new Configurations/WriterConfiguration.cs, EfMovieRepository.cs, EfMovieCatalog.cs and the new generated migration/designer plus LamuFlixDbContextModelSnapshot.cs.
- Existing integration tests under tests/LamuFlix.IntegrationTests/: MetadataProviderLookupTests.cs, ApiEndToEndEnrichmentTests.cs, ApiEndToEndImportTests.cs, MigrationTests.cs, EfMovieRepositoryTests.cs, PersistenceRoundTripTests.cs, EfMovieRepositoryDeleteTests.cs, EfMovieCatalogDetailsTests.cs and PostgresFixtureTests.cs. Add focused test files within existing projects only where the approved cases need them.
- Fixture: tests/LamuFlix.Tests.Common/PostgresFixture.cs; reuse MetadataProviderProbe and existing end-to-end fixtures.
- Retain public LibraryResponses.cs shape. No planned changes to /web, OpenAPI/types, playback, packages, harness thresholds, Initial migration or unrelated files. Gate- or acceptance-forced edits must cite their necessity; new scope needs a Patron ruling.

Basis: recon-DEV-411:10-61; Q1–Q6. Exact new migration filename and focused test filenames are build outputs, not invented recon facts.

## Test strategy and task order — Q6

1. Core: constructor compatibility, immutable defensive copies, Genres/Writers equality/hash parity, empty/nonempty collections and deterministic ordering.
2. Provider: WireMock full response and full plot; missing/blank/N/A fields, token whitespace/duplicates/annotated writers; Metascore endpoints 0 and 100, malformed and outside bounds. Assert exact query keys and plot=full both with and without optional year; update existing end-to-end exact-key assertions.
3. EF: writer model and additive migration, then repository full snapshot save/fresh-context reload, replace, clear, idempotence, lookup reuse/shared-row preservation and concurrent enrichment of separate movies. Real Postgres proof of upgrade from Initial with existing data, rollback preserving old rows, fixture reset and movie-delete join cascade.
4. Catalog and end-to-end: persisted metadata details and existing genre facets; WireMock plus Postgres import/enrichment proves full plot, existing exposed credits/score and internal writers/genres, without widening the response DTO.
5. Refactor the complete change, then Gauge executes configured verification. Do not start build before the separate merged spec PR establishes Gate 1.

Use existing assertion/mocking/test libraries and fixtures; never mock database query translation. No new dependency. Anvil follows tasks.md phases; read-only recon belongs to Wisp and gate execution to Gauge.

## Gate expectations — Q6–Q7

Gauge runs configured Roslyn, cyclomatic complexity, InspectCode, property tests, vulnerable packages, dotnet format --verify-no-changes, dotnet test and scripts/run-mutation.ps1. No threshold reductions or new opt-outs. Mutation uses the pipeline script without --since from this worktree, and preserves kill-share/file-span checks and timeout evidence.

Run web gate according to pipeline; an unchanged /web yields scope-empty SKIPPED. Script-authorized scope-empty SKIPPED, mutation NOT APPLICABLE and configured SKIP remain distinct nonblocking results, never PASS. Mandatory gate failure or Could not run blocks. An unaccepted property-test exit 2 blocks. Baseline receipts in DEV-411:38-46 do not prove future build readiness.

## Loop discipline and closure — Q7

- Closing bar: zero verified Critical, High or Medium findings within frozen scope before review clearance; Low recorded nonblocking. All required gate results must meet the pipeline bar.
- Round cap: two review rounds, at most two fix commits per round. At the cap, unresolved above-bar findings accompany user review on the PR; no third round and no claim of clearance.
- Frozen scope: Q1–Q6 provider query/mapping, Core metadata snapshots, writers/movie_writers additive migration, complete EF write/read paths, unchanged HTTP shape and the specified integration/regression proof; anything else is a follow-up issue, not a finding in this round.
- No new ADR or glossary expansion is required for this M ingestion slice. Genre and role-specific literal Director/Writer/Actor credits are the agreed vocabulary. No owner checkbox is required.
- Shared understanding confirmed by Patron Q7. Next: Quill drafts spec/plan/tasks from this brief, CONCLUSIONS.md and recon; Keel later analyzes those drafts read-only, then the separate plan challenge and Gate 1 proceed. Grill closure is not plan freeze, review clearance, Gate 1 or merge approval. Only the user merges.
