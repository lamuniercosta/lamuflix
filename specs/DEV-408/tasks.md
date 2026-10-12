# Tasks: DEV-408 Persist enrichment poster URL so library browse tiles show posters

**Input**: Design documents from `specs/DEV-408/` (spec.md, plan.md, brief.md, CONCLUSIONS.md Q1-Q7).

**Prerequisites**: plan.md (required), spec.md (required), brief.md (decisions; authoritative with CONCLUSIONS.md).

**Tests**: Included because the ticket AC and brief Q5 explicitly require them. Real-Postgres Testcontainers plus WireMock OMDB infrastructure only; no EF InMemory, no mocked data-access driver, no raw-DbContext-seeding as sole persistence proof, no reflection/private-method bypass as clearing proof.

**Organization**: Grouped by user story in brief dependency order (Core, then adapter/repository, then browse/API, then contracts, then end-to-end). Tasks that touch any shared file run in sequence; shared repository, E2E, and contract test files have one writer at a time.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies).
- **[Story]**: Which user story this task belongs to (US1 browse posters, US2 clearing, US3 Worker disposition).
- Exact file paths are in each description.

## Phase 1: Core metadata carrier, equality, and clearing operation (US1/US2 foundation)

**Purpose**: Optional Core poster carrier plus the narrow clearing operation, with focused unit/property tests. Everything later depends on this.

**Checkpoint**: Core builds; poster participates in equality; `ClearMetadata()` is idempotent and preserves unrelated state.

- [ ] T001 [US1] Add optional `string? PosterUrl` to Core `MovieMetadata` in `src/LamuFlix.Core/Domain/MovieMetadata.cs`, include it in equality `State` and rehydration, without breaking existing construction call sites unnecessarily.
- [ ] T002 [US2] Add narrow `Movie.ClearMetadata()` in `src/LamuFlix.Core/Domain/Movie.cs`: nulls `Metadata` alone, preserves enrichment status/timestamps/attempts/unrelated state, idempotent on repeat; leave `MarkNotFound`/`MarkFailed`/retry untouched. Safeguard: if evidence establishes Enriched implies non-null Metadata, stop and reopen Q2 with bounded recon and Patron ruling before changing status semantics.
- [ ] T003 [P] [US1] Domain unit/property tests for poster-sensitive equality (poster-only difference, null vs value, trimming-independent equality) in the existing UnitTests project.
- [ ] T004 [P] [US2] Domain unit/property tests for clearing idempotence and unrelated-state preservation in the existing UnitTests project.

## Phase 2: Shipped provider mapping plus repository write/read/clear plumbing (US1/US2)

**Purpose**: OMDB poster mapping and seven-field persistence/clearing with real-database proofs. Depends on Phase 1. Serialize tasks that touch any shared file.

**Checkpoint**: Provider maps all six poster cases; repository inserts/updates/rehydrates/clears all seven fields; each proof observed via a fresh sibling context.

- [ ] T005 [US1] Extend shipped OMDB response/mapping in `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.Mapping.cs`: deserialize `Poster`, trim present strings, map missing/null/blank-whitespace/`N/A` to null; lookup still succeeds without artwork; opaque string only.
- [ ] T006 [US1] Provider mapping/lookup tests in `tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs`: present, trimmed, missing, JSON null, blank/whitespace, `N/A`; absence never turns success into failure (sequential with T007-T009 on shared files as needed).
- [ ] T007 [US1/US2] Extend `ToRecord`/`CopyMetadata`/`ClearMetadata`/`ToDomain` in `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` to all seven fields; null metadata clears all seven; null poster writes null with no fallback.
- [ ] T008 [US1/US2] Real-Postgres repository proofs in `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs` (existing `PostgresFixture`/`PostgresCollection`, fresh sibling contexts): insert enriched metadata with poster; update to a poster; poster-only replacement; null replacement; preserve poster on unrelated update; assert all seven fields where relevant.
- [ ] T009 [US2] Real-Postgres clearing proof in `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs`: start from non-null poster persisted through the supported enrichment/repository path, then `Movie.ClearMetadata()` plus `SaveChangesAsync`; assert all seven metadata fields null and unrelated fields unchanged via a fresh sibling context.
- [ ] T010 [US1] Update the old record-only poster preservation expectation for the poster alone in the affected repository/round-trip tests; retain all unrelated-data preservation assertions.

## Phase 3: Browse projection, API response, and exact-shape test updates (US1)

**Purpose**: Always-present nullable `posterUrl` on browse items plus AC-forced test updates. Depends on Phases 1-2.

**Checkpoint**: `GET /api/movies` items are exactly `id`, `title`, `posterUrl` with explicit null when absent; details/metadata shapes unchanged.

- [ ] T011 [US1] Extend Core `MovieSummary` in `src/LamuFlix.Core/Ports/MovieSummary.cs` with nullable poster and project `MovieRecord.PosterUrl` in `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`; compile-forced caller adjustments only, no handler behavior change.
- [ ] T012 [US1] Extend `MovieSummaryResponse` in `src/LamuFlix.Api/Endpoints/LibraryResponses.cs` with always-present nullable `posterUrl`; no route/status/filter/pagination/details change.
- [ ] T013 [US1] Real-Postgres browse/API proofs: stored non-null poster returned; absent/cleared poster returned as explicit null; id/title/ordering/filter/pagination preserved (in `ApiEndToEndBrowseTests.cs` and direct projection coverage).
- [ ] T014 [US1] AC-forced exact-shape updates to the two browse-item field lists at `tests/LamuFlix.IntegrationTests/ApiEndToEndBrowseTests.cs:72` and `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs:365` (add `posterUrl`); compile-forced call-site updates in `BrowseMoviesQueryHandlerTests`, `LibraryEndpointsTests` (including the `RecordingMovieCatalog` construction at `:688`, compile-forced only), `ApiHostCompositionTests` only as forced; `ApiEndToEndImportTests`/`ApiEndToEndLibraryTests` details/metadata assertions explicitly preserved, as are all other details/metadata wire assertions.

## Phase 4: Generated contracts (US1)

**Purpose**: C#-derived OpenAPI and snapshot regeneration; approved TypeScript generation only when available. Depends on final API shape (Phase 3).

**Checkpoint**: OpenAPI and Verify snapshot match the C# response shape with `posterUrl` required AND nullable; no hand-edited generated artifacts.

- [ ] T015 [US1] Regenerate via the existing opt-in `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` with `LAMUFLIX_REGENERATE_OPENAPI=true`, writing `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` together; verify `MovieSummaryResponse.posterUrl` is required AND nullable (brief:45/FR-006 always-present explicit-null contract); generate `web/src/api/types.ts` only through existing approved tooling when available (required string-or-null property when generated), never by hand-edit or new tooling/dependency.

## Phase 5: Shipped enrichment-to-browse end-to-end and regression sweep (US1/US2/US3)

**Purpose**: Full-path proof plus verification that the Worker disposition (spec US3/FR-008/SC-004, brief Q4) stands and legacy files are untouched. Depends on Phases 1-4. One writer at a time on shared E2E/contract files.

**Checkpoint**: WireMock OMDB poster flows through shipped enrichment into `GET /api/movies`; clearing regression green; Worker/Web/Test files unchanged in the implementation diff; US3/FR-008/SC-004 and the Q4 rationale verified as recorded.

- [ ] T016 [US1] WireMock-OMDB plus real-Postgres end-to-end in `tests/LamuFlix.IntegrationTests/ApiEndToEndEnrichmentTests.cs`: stub OMDB returns a poster, shipped enrichment stores it, `GET /api/movies` exposes it; poster expectations updated, unrelated assertions preserved.
- [ ] T017 [US2] Full persistence/clearing regression: non-null poster, poster-only replacement, null replacement, `ClearMetadata()` clearing all seven fields, browse explicit null, via the supported path with fresh-context reloads.
- [ ] T018 [US3] Verification-only: confirm spec US3/FR-008/SC-004 and the brief Q4 retention rationale (outside solution, no ProjectReference, absent `LamuFlix.Data` namespaces; snapshot semantics govern, Worker fallback not ported) are already recorded, and confirm the implementation diff leaves legacy Worker/Web/Test files unchanged; no legacy Worker/Web/Test file edited or deleted, and no spec-artifact write in this task.

## Excluded (never scheduled)

Dependency/package changes, new project/layer/port/handler/route, schema/migrations, legacy Worker/Web/Test edits or deletion, LocalPlay/feature flags, secrets, process launching, UI features, unrelated cleanup, ADRs, shared-glossary rewrites, edits to old feature documents. A discovered need outside the approved boundary is routed for decision, never silently absorbed.
