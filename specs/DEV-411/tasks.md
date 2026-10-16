# Tasks: DEV-411 Ingest OMDb genres, cast, director, writers, Metascore and full plot

**Input**: Design documents from `specs/DEV-411/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (plan records why).

**Tests**: Included. Brief Q6 requires them. Author a behavior test before the production edit it locks, and confirm that test fails, when the test can compile against types that already exist and the locked behavior is not already implemented. Run a test to green only after its named execution prerequisites. T010 asserts the generated second migration, so author and execute T010 after T009 rather than requiring it to fail before generation. T018 proves the existing genre facet as a regression after T011 and has no mandatory red state before T016. Real Postgres via the existing `PostgresFixture`. WireMock via the existing `MetadataProviderProbe`. No EF InMemory. No mocked data-access driver.

**Organization**: Phases follow brief Q6. Anvil implements Phases 1–4, one phase per ask, and stops after Phase 4. Phase 5 is Cog, then Gauge. Do not start Phase 1 until the spec PR has merged and Gate 1 is established (brief Q6).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 full snapshot, US2 tolerant optional data, US3 replace/clear/delete/migration.
- Exact file paths are in each description.

## Phase 1: Core metadata collections (US1)

**Purpose**: Optional genres and writers on Core metadata, with equality and copy behavior matching directors and actors. Blocks Phases 2–4.

**Independent Test**: Construct metadata with and without the new collections and assert input-copy isolation, mutation resistance, order, equality, and hash.

**Checkpoint**: Existing construction call sites still compile. Genres and Writers are ordered `System.Collections.Immutable` collections. Exposed Genres and Writers cannot be mutated, including through a cast, so that the metadata changes. They participate in ordered equality and hash.

- [X] T001 [US1] Write failing focused tests in `tests/LamuFlix.UnitTests/Domain/` for constructor compatibility when genres and writers are omitted, input-copy isolation (mutating the supplied sequences after construction does not change Genres or Writers), and mutation resistance (the exposed Genres and Writers collections cannot be mutated, including through a cast, so that the metadata changes). Also cover Genres/Writers ordered value equality and hash parity with Directors/Actors, empty and nonempty collections, and deterministic order. Use the existing unit-test project and its assertion and property-test libraries. Add a new file in that folder only if these cases need one; the filename is a phase output, not a name frozen here.
- [X] T002 [US1] Add optional genres and writers parameters, typed as optional `IEnumerable<string>?`, after the existing actors parameter on `MovieMetadata` in `src/LamuFlix.Core/Domain/MovieMetadata.cs`. Store Genres and Writers as ordered `System.Collections.Immutable` collections, copying the inputs into that storage. Include them in the existing ordered value equality and per-element hash, on the same terms as Directors and Actors. Omitting the new parameters leaves existing callers valid.

---

## Phase 2: OMDb request and tolerant mapping (US1, US2)

**Purpose**: `plot=full`, collection normalization, literal writer credits, and tolerant Metascore. Depends on Phase 1.

**Independent Test**: WireMock proves the full payload, the tolerant cases, and the exact query with and without a year.

**Checkpoint**: A full payload maps to the snapshot. Bad optional fields do not fail a found title. Both year and no-year lookups send `plot=full` plus the existing keys.

- [X] T003 [US1] Extend `tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs` for a full response (Genre, Director, Writer, Actors, Metascore, full Plot) and for `plot=full` both with and without optional `y`. Assert the exact query keys. Author this test and confirm it fails before T005 and T006.
- [X] T004 [US2] Extend `tests/LamuFlix.IntegrationTests/MetadataProviderLookupTests.cs` for missing, blank, and `N/A` fields; token whitespace; case-insensitive duplicates that keep the first spelling and first-seen order; literal parenthetical writer credits; Metascore 0 and 100; malformed and out-of-range Metascore as null without failing an otherwise valid response. Author this test and confirm it fails before T006. Keep this file sequential with T003.
- [X] T007 [US1] Before T005, update the exact query-key assertions in `tests/LamuFlix.IntegrationTests/ApiEndToEndEnrichmentTests.cs` and `tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs` so `plot=full` is required. Confirm these assertions fail before T005. Preserve unrelated assertions.
- [X] T005 [P] [US1] After T003, T004, and T007 are failing, send one `plot=full` from `BuildUri` in `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.cs`. Preserve `apikey`, `t`, `type=movie`, and optional `y`.
- [X] T006 [P] [US1/US2] After T003 and T004 are failing, deserialize Genre, Director, Writer, Actors, and Metascore in `src/LamuFlix.Infrastructure/Adapters/OmdbMetadataProvider.Mapping.cs`. Comma-split, trim, drop empty, blank, and `N/A` tokens using the existing ordinal missing check, and dedupe case-insensitively in first-seen order. Pass Plot through existing `MapSynopsis`. Map a whole integer Metascore from 0 through 100 onto the existing nullable score; otherwise null. Do not fail a response whose only defects are optional.

---

## Phase 3: Writers migration and repository snapshot (US1, US3)

**Purpose**: Additive `writers` / `movie_writers` model and a complete EF read/write path. Depends on Phases 1–2.

**Independent Test**: Real Postgres proves upgrade, rollback, save, fresh-context reload, replace, clear, idempotence, shared-row reuse, concurrent enrichment of two movies, delete cascade, and fixture reset.

**Checkpoint**: Initial is unchanged. Existing movies survive upgrade with no fabricated writers. Repository rehydration returns the full snapshot. Shared lookup rows survive replace, clear, and delete.

- [X] T008 [US3] Add `WriterRecord` in `src/LamuFlix.Infrastructure/Persistence/Records/WriterRecord.cs` and `WriterConfiguration` in `src/LamuFlix.Infrastructure/Persistence/Configurations/WriterConfiguration.cs`, following `ActorConfiguration` (`writers`, integer identity `id`, required text `name`, no unique name index). Add `Writers` on `src/LamuFlix.Infrastructure/Persistence/LamuFlixDbContext.cs` and the `Writers` navigation on `src/LamuFlix.Infrastructure/Persistence/Records/MovieRecord.cs`. Add `movie_writers` (`writer_id`, `movie_id`) in `src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs` with the existing skip-navigation shape. Do not change existing tables or `src/LamuFlix.Infrastructure/Persistence/Migrations/20260930153520_Initial.cs`.
- [X] T009 [US3] After T008, generate one second migration, its designer, and the updated `src/LamuFlix.Infrastructure/Persistence/Migrations/LamuFlixDbContextModelSnapshot.cs` with EF tooling. Do not hand-edit the generated designer or snapshot. Do not edit the Initial migration. The timestamped filename is the tooling output.
- [X] T010 [P] [US3] After T009, update `tests/LamuFlix.IntegrationTests/MigrationTests.cs`: include `writers` and `movie_writers` in the application table list; expect Initial plus the second migration; prove upgrade from Initial with existing movies adds no writer rows; prove Down drops `movie_writers` before `writers` and keeps preexisting tables and rows. Author and execute this test only after the generated migration exists. Do not require it to fail before T009.
- [X] T012 [US1/US3] Before T011, author failing tests that prove save, fresh-context reload, replace, clear, idempotent repeat, lookup reuse, and shared-row preservation in `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs` and `tests/LamuFlix.IntegrationTests/PersistenceRoundTripTests.cs`. Fresh-context reload assertions include full Synopsis. Confirm they fail before T011 starts. These tests compile against Phase 1 `MovieMetadata` and the existing repository.
- [X] T013 [US3] Before T011, and after T012 on the shared file, author failing tests in `tests/LamuFlix.IntegrationTests/EfMovieRepositoryTests.cs` that prove concurrent enrichment of two movies sharing new names across all four roles (genres, directors, writers, and actors). Use a separate database context and connection for each movie. Control and evidence overlapping contention on those shared new names, with synchronization bounded so it stays compatible with the ordered `pg_advisory_xact_lock` waits in T011 (do not hold a test lock across the repository call in a way that deadlocks that wait). From a fresh context, assert one lookup identity per shared name per role and the correct nonduplicate join rows for both movies. Also prove bounded exhaustion and rollback as ruled for T011: a retryable conflict (`40001`, `40P01`, or `23505` on a movie or join primary key) while the repository owns the transaction rolls the attempt back, calls `ChangeTracker.Clear()`, and replays the full unit of work within 3 total attempts; on attempt-3 exhaustion, a non-retryable error, or an ambient transaction, the original persistence exception is rethrown and a fresh context shows no partial lookup or join rows from the failed attempt. Keep existing enrichment failure classification, retry, and claim handling unchanged. Use real Postgres. Do not mock the data-access driver. Do not add a unique name index or use a writer-only constraint as the proof for all four roles. Confirm these tests fail before T011 starts.
- [X] T014 [P] [US3] After T009, extend `tests/LamuFlix.IntegrationTests/EfMovieRepositoryDeleteTests.cs` so movie deletion cascades that movie's join rows, including `movie_writers`, and preserves shared lookup records. This proof locks the model and migration, and it does not wait on T011.
- [X] T015 [P] [US3] After T009, extend reset assertions in `tests/LamuFlix.IntegrationTests/PostgresFixtureTests.cs` for writers and writer joins. Edit `tests/LamuFlix.Tests.Common/PostgresFixture.cs` only if the model-driven truncate walk omits `movie_writers`. This proof locks fixture reset, and it does not wait on T011.
- [X] T011 [US1] After T012 and T013 fail, extend save, clear, and rehydration in `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` so a snapshot replaces genres, directors, writers, actors, and Metascore; rehydration returns full Synopsis and all four collections; lookup rows are reused by trimmed case-insensitive name with stored casing kept; only new names are inserted; clearing removes that movie's associations. Keep shared lookup rows. For each metadata-bearing write (`AddAsync` / `UpdateAsync`), run an explicit `READ COMMITTED` database transaction (`BeginTransactionAsync`, or reuse an ambient transaction already open). Collect distinct normalized keys `(role_id, lower(trim(name)))` in role order `1 = genres`, `2 = directors`, `3 = writers`, `4 = actors`, then ascending `StringComparer.Ordinal` on `lower(trim(name))`, and acquire `SELECT pg_advisory_xact_lock(@roleId, hashtext(@normalizedName))` in that order before reading or inserting lookup rows. Under those locks, reuse or insert lookup rows and replace the four join collections, then save and commit. When this repository owns the transaction, retry `PostgresException` SQLSTATE `40001`, `40P01`, or `23505` on a movie or join primary key for at most 3 total attempts (1 initial + 2 retries) with a `25ms` then `50ms` delay. Each retry rolls back, calls `ChangeTracker.Clear()`, and replays the full unit of work from a fresh reload of `MovieRecord` and all four role collections. On attempt-3 exhaustion, a non-retryable error, or an ambient transaction, roll back, clear tracked state, and rethrow the original persistence exception so existing enrichment failure classification, retry, and claim handling stay unchanged. Do not add a process-wide lock, package, layer, unique name index, or any other existing-table schema change. Do not add a writer-only uniqueness constraint as the race proof for all four roles. T008 stays with no unique name index on `WriterConfiguration`. If this mechanism cannot prevent duplicate lookup or join rows without an existing-table schema change, stop for a Patron ruling before continuing.

---

## Phase 4: Catalog projection and end-to-end enrichment (US1)

**Purpose**: Internal reads and the existing public shape. Depends on Phases 1–3.

**Independent Test**: The internal catalog projection returns persisted Genres and Writers with the rest of the snapshot. The existing genre facet shows persisted genres. WireMock plus Postgres import/enrichment stores the full snapshot, and the HTTP field set stays unchanged.

**Checkpoint**: The internal catalog projection returns Genres, Directors, Writers, Actors, MetaScore, and full Synopsis. HTTP details still expose Synopsis, MetaScore, Directors, and Actors only. The genre facet lists persisted genres. `LibraryResponses.cs` is unchanged.

- [ ] T017 [P] [US1] Before T016, author failing tests in `tests/LamuFlix.IntegrationTests/EfMovieCatalogDetailsTests.cs` that prove the internal catalog projection returns the complete metadata snapshot: full synopsis, Metascore, directors, actors, and Genres and Writers surviving that projection. Confirm they fail before T016. This catalog assertion is separate from HTTP field exclusion. A response that omits Writers and per-movie Genres on the HTTP field set is not this proof.
- [ ] T018 [P] [US1] After T011, author a regression proof that the existing genre facet returns newly persisted genres in `tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs`. Phase 3 persistence already feeds that facet, so this proof has no mandatory red state before T016, and T016 is not the edit that makes it pass. Do not edit the facet merely to force a failure. Do not widen browse responses.
- [ ] T019 [US1] Author the WireMock plus Postgres proof in `tests/LamuFlix.IntegrationTests/ApiEndToEndEnrichmentTests.cs` and `tests/LamuFlix.IntegrationTests/ApiEndToEndImportTests.cs`: full plot, existing exposed credits and score, internal writers and genres, the unchanged HTTP response field set (Writers and per-movie Genres excluded), and the `plot=full` query. An assertion tied to missing catalog projection data may fail before T016. Do not require every already-implemented end-to-end assertion to fail before T016. HTTP field exclusion is this proof, separate from the T017 catalog projection. Do not edit `src/LamuFlix.Api/Endpoints/LibraryResponses.cs`.
- [ ] T016 [US1] After T017 fails, hydrate Genres, Directors, Writers, Actors, MetaScore, and full Synopsis in the internal metadata projection in `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`. This edit makes the T017 projection assertions pass. It does not require T018 to fail first, and it is not what makes T018 pass. A T019 assertion that is red only because this projection is missing may turn green here; already-implemented end-to-end assertions do not. Do not change the existing `HasMetadata` cast-presence rule. Do not add response fields.

---

## Phase 5: Refactor and verification (Cog, then Gauge)

**Purpose**: Not an Anvil phase. After Phases 1–4 are committed, Cog refactors the whole change. Gauge then runs the configured gates. A task that only runs gates stays with Gauge.

**Checkpoint**: Refactor complexity is at or under the refactor threshold. Every mandatory gate has its script verdict. Scope-empty skip is not recorded as pass.

- [ ] T020 Refactor the completed change across the file set in `specs/DEV-411/plan.md` until `./scripts/run-cyclomatic-complexity.ps1 -Threshold 6` passes. Extract helpers, early returns, and guard clauses. Do not suppress to pass. Do not lower a threshold.
- [ ] T021 Run the configured gates and record each script verdict: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1` with no arguments, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and `./scripts/run-mutation.ps1` without `--since`. Run `./scripts/run-web-gates.ps1`; an unchanged `/web` is scope-empty `SKIPPED`, not PASS. No threshold edits and no new opt-out. A mandatory failure, `Could not run`, or an unaccepted property-test exit 2 blocks.

---

## Excluded (never scheduled)

UI, provider replacement, backfill or refresh job, playback, `Features:LocalPlay`, secrets, `Process.Start`, new dependencies, new projects or layers, OpenAPI, generated TypeScript, `/web`, edits to `LibraryResponses.cs`, edits to the Initial migration, hand-edits of the generated designer or snapshot, unique indexes or other schema changes on existing tables, global orphan cleanup, role reclassification, `CONTEXT.md` glossary edits, and ADRs. A discovered need outside this list stops for a decision. It is not absorbed into a phase.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No earlier phase. Blocks Phases 2–4. Author T001 and confirm it fails before T002.
- **Phase 2**: Depends on Phase 1. Author T003, T004, and T007 and confirm they fail before the provider edits. T006 starts once T003 and T004 are failing. T005 starts once T003, T004, and T007 are failing.
- **Phase 3**: Depends on Phases 1–2. T009 depends on T008. Author and execute T010 only after T009, because T010 asserts the generated migration and is not required to fail before that artifact exists. Author T012 and T013 and confirm they fail before T011. T011 is the production edit that makes T012 and T013 pass. A green run of T012 and T013 requires T011. T014 and T015 depend on T009 and do not depend on T011.
- **Phase 4**: Depends on Phase 3. Author T017 and confirm it fails before T016. T016 is the production edit that makes the T017 projection assertions pass. A green run of T017 requires T016. T018 is a regression after T011: no mandatory red state before T016, and T016 does not make T018 pass. A green run of T018 follows repository persistence and does not require T016. T019 keeps the end-to-end acceptance proof. An assertion in T019 that fails because catalog projection data is missing may be red before T016; do not require every already-implemented end-to-end assertion to fail, and do not treat T016 as the edit that makes those already-implemented assertions pass. T019 also requires the Phase 2 query-key updates. T017 proves Genres and Writers on the internal catalog projection. HTTP field exclusion is T019 and does not substitute for T017.
- **Phase 5**: Depends on Phases 1–4 being committed. Cog owns T020. Gauge owns T021.

### User Story Dependencies

- **US1 (P1)**: Starts in Phase 1. Its end-to-end proof is T019.
- **US2 (P2)**: Provider tests in T004. The mapping is T006, shared with US1. No persistence of its own.
- **US3 (P3)**: Starts at T008 after the Core collections exist. Repository replace/clear proofs share T012 with US1.

### Within Each User Story

- Author a behavior test before the production edit it locks, and confirm that test fails, when it can compile against types that already exist and the locked behavior is not already implemented.
- Run that test to green only after the production edit and its named execution prerequisites.
- T010 is the stated exception: author and execute it after migration generation (T009). T018 is the stated regression: author it after T011, with no mandatory failure before T016.
- Core collections before the provider mapping that fills them.
- Provider mapping before repository snapshot writes.
- Repository writes before a green catalog projection. The T018 genre-facet regression is proven after those writes and does not wait on T016. End-to-end proof follows the same persistence, and only its missing-projection assertions wait on T016.
- Model configuration (T008) before migration generation (T009). Migration generation before migration tests (T010).

### Parallel Opportunities

- T005 and T006 touch different provider files. T006 can proceed once T003 and T004 are failing. T005 can proceed once T003, T004, and T007 are failing.
- T010 can proceed after T009 beside the authoring of T012 and T013 (different files).
- T014 and T015 can proceed together after T009 (different files).
- T017 proceeds before T016. T018 can proceed after T011 beside T017 (different files) and does not block T016.
- T012 and T013 share `EfMovieRepositoryTests.cs` and stay sequential.
- T003 and T004 share `MetadataProviderLookupTests.cs` and stay sequential.

## Parallel Example: Phase 4

```text
Task: "T017 Prove Genres and Writers survive the internal catalog projection in tests/LamuFlix.IntegrationTests/EfMovieCatalogDetailsTests.cs"
Task: "T018 Prove the existing genre facet in tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Finish Phase 1 and confirm Core equality.
2. Finish Phase 2 and confirm the WireMock full payload.
3. Finish Phase 3 through the repository reload.
4. Finish Phase 4. US1 is then demonstrable end to end.
5. Stop there only to validate US1. The frozen ticket scope still requires US2 and US3 before review.

### Incremental Delivery

US2 is proven in Phase 2 with the same mapping change as US1. US3 is proven in Phase 3 and is part of the same frozen scope, not a follow-up. Phase 5 does not add behavior.

### Parallel Team Strategy

One implementer is the expected path, in phase order. The parallel tasks above are the only safe splits. Do not split two writers onto `MetadataProviderLookupTests.cs` or `EfMovieRepositoryTests.cs`.

## Notes

- [P] tasks use different files and do not depend on an unfinished task.
- Story labels map to `spec.md`.
- Author a behavior test and confirm it fails before the production edit it locks, when that behavior is not already implemented. T010 is the exception: author it after the generated migration exists. T018 is the regression exception: author it after T011, with no mandatory red state before T016, and do not edit the facet merely to force a failure.
- Commit after each phase, not after every task, using `DEV-411 - {phase subject}`.
- Gate 1 comes from the merged spec PR. This task list does not authorize implementation before that.
