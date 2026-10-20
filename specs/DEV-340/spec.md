# Feature Specification: DEV-340 GetRecommendationsQuery and Phase A candidate provider

**Feature Branch**: `feature/340-spec`

**Created**: 2026-10-20

**Status**: Draft

**Input**: Ticket DEV-340 (size M, parent DEV-288, ui:none); settled grill brief `specs/DEV-340/brief.md` (grill closed Q6/12 on 2026-10-20); facts from notes `recon-DEV-340`, `recon-DEV-340-2`, `recon-DEV-340-3`, `recon-DEV-340-3-2`; Patron rulings in `specs/DEV-340/CONCLUSIONS.md`; taste receipt `specs/DEV-340/ASSUMPTIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## Domain vocabulary *(mandatory — published from root CONTEXT.md)*

These terms are already present in root `CONTEXT.md`. No new glossary term is added by this ticket (brief Q6). Published Spec Kit artifacts must use them as written:

- **Recommendation**: A suggested library movie from the details page. _Avoid_: search result, related-title guess without score/reasons.
- **Reason**: A caller-safe explanation attached to a recommendation. _Avoid_: internal identifiers, scorer debug strings rewritten by this ticket.

Use existing Movie, MovieSummary, MovieFeatures, GenreWeights, SimilarityScorer, and EnrichmentStatus vocabulary. This ticket owns `RecommendedMovie` and `GetRecommendationsQuery`; it consumes sibling contracts from DEV-336/337/338 without duplicating them.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Load Phase A recommendation candidates (Priority: P1)

A recommendations caller asks for every enriched library movie except the selected movie. One persistence query returns the full candidate pool as domain `Movie` instances with actors, directors, genres, writers, scalar metadata, and watchlist/watched/not-interested flags rehydrated. Non-enriched movies and the selected id are excluded. The pool is not paginated or capped.

**Why this priority**: Scoring and ranking need a real candidate set. Without the Phase A port and adapter there is nothing to recommend.

**Independent Test**: Against real PostgreSQL via `PostgresFixture`/`PostgresCollection` in both `tests/LamuFlix.IntegrationTests/EfRecommendationCandidateSourceTests.cs` and `tests/LamuFlix.UnitTests/Persistence/EfRecommendationCandidateSourceSurvivorTests.cs` (brief PD1), prove filtering across every enrichment state, selected-id exclusion, all four collections plus scalars/flags rehydration (including empty collections and null `MetadataTitle` ⇒ null `Metadata`), repository parity from separate contexts, exactly one SQL command for the candidate load (scoped interceptor; setup/parity commands excluded), fresh query-context `ChangeTracker` empty afterward, no pool cap, and cancellation. Thread `ct` into EF execution. Never mock the EF driver. Infrastructure mutation selects UnitTests only (recon-DEV-340 section 11); thresholds/config unchanged.

**Acceptance Scenarios**:

1. **Given** enriched movies and a selected movie id present in the library, **When** candidates are loaded, **Then** every returned movie has `Status == Enriched` and none has the selected id.
2. **Given** movies in Pending, NotFound, and Failed states, **When** candidates are loaded, **Then** those movies are absent from the pool.
3. **Given** enriched movies with actors, directors, genres, and writers (including empty and nonempty collections) and rows with null `MetadataTitle`, **When** candidates are loaded, **Then** rehydrated domain movies match full domain state from repository reads on separate contexts — collections, scalars, flags/status/enrichment fields, and null `Metadata` iff `MetadataTitle` is null — without inventing a persisted null primary-key scenario; private null-Id guard retained.
4. **Given** a cancelled token, **When** candidate load is started, **Then** work stops and cancellation propagates; failures are not turned into an empty successful pool.
5. **Given** the Phase A adapter, **When** the pool is loaded, **Then** exactly one SQL command materializes the pool once (scoped `DbCommandInterceptor` count excludes setup/parity calls), the fresh query context remains untracked (`ChangeTracker.Entries()` empty), and there is no per-candidate lookup, pagination, or pool cap. Full Includes and private `Rehydrate` are retained; no projection rewrite is required.

---

### User Story 2 - Rank top-N recommendations with reason chips (Priority: P2)

A caller issues `GetRecommendationsQuery(MovieId, Take)` (default Take = 10). The handler loads the selected movie, loads the Phase A candidate pool, scores each candidate once with the frozen `SimilarityScorer`, ranks by score descending (MovieId ascending on ties), and returns at most `Take` `RecommendedMovie` values carrying `MovieSummary`, unchanged score, and unchanged `Reason` chips. An empty pool returns an empty list without calling the genre-weights calculator. Missing selected movie throws parameterless `NotFoundException`. Selected may be in any enrichment state.

**Why this priority**: This is the ticket acceptance criterion — top-N ranked recommendations with attached reason chips.

**Independent Test**: Unit tests with real `MovieFeaturesMapper`, `SimilarityScorer`, `GenreWeightsCalculator`, and `GenreWeightsCache`; substitute only `IMovieRepository`, `IRecommendationCandidateSource`, and `IMovieCatalog` (genre counts via catalog). Property tests cover total ordering (Score descending then `Id.Value` ascending), count bounds, and permutation invariance with real collaborators and valid candidate-source pools that exclude selected by construction (brief PD2); selected-id exclusion is proved only in the two real-Postgres adapter files, not as a handler property. No defensive handler filter. Cover Take validation, default 10 versus options.Take, missing/unenriched selected, empty/fewer/exact/more-than-N candidates, ties, zero-score retention, all ten summary fields (null-metadata summaries keep null metadata-derived scalars per Q4), Score/Reasons passthrough, cancellation, and errors.

**Acceptance Scenarios**:

1. **Given** a selected movie and N or more scored candidates, **When** the query runs with Take = N, **Then** the result has exactly N items ordered by score descending, and each item carries Summary, Score, and Reasons from the scorer unchanged.
2. **Given** equal scores between two candidates, **When** results are ranked, **Then** the lower `MovieId.Value` precedes the higher (deterministic tie-break; ASSUMPTIONS.md Q4).
3. **Given** an empty candidate pool, **When** the query runs, **Then** the result is an empty immutable list and `GenreWeightsCalculator` is not called.
4. **Given** fewer candidates than Take, **When** the query runs, **Then** the result length equals the candidate count; zero-score candidates are retained; there is no positive-score cutoff.
5. **Given** a missing selected movie id, **When** the query runs, **Then** parameterless `NotFoundException` is thrown.
6. **Given** an existing selected movie in a non-enriched state, **When** the query runs, **Then** the selected movie is accepted and mapping proceeds through the frozen mapper.
7. **Given** Take ≤ 0, **When** validation runs, **Then** the query is rejected. **Given** Take omitted, **Then** the default is exactly 10 and `RecommendationOptions.Take` does not override it.
8. **Given** a pre-cancelled token or a dependency failure, **When** the query runs, **Then** cancellation or the error propagates; failures are not converted into successful empty results.
9. **Given** scored candidates, **When** summaries are built, **Then** each `MovieSummary` uses exactly Id, Title, PosterUrl, ReleaseYear, Runtime, ImdbRating, EnrichmentStatus, IsInWatchlist, IsWatched, and IsNotInterested from the candidate as ruled in brief Q4 — no ParsedReleaseYear fallback.

---

### User Story 3 - Register handler, validator, and candidate source (Priority: P3)

The host resolves `GetRecommendationsQueryHandler`, its FluentValidation validator, and scoped `IRecommendationCandidateSource` → `EfRecommendationCandidateSource` through existing DI extension points. Composition tests cover the new handler/validator/adapter without changing the HTTP route manifest.

**Why this priority**: Ticket delivery is incomplete until the handler and port resolve in the established host pipeline. No HTTP route ships.

**Independent Test**: Extend `ApiHostCompositionTests` handler-manifest/composition coverage only; leave the route manifest unchanged. Prove scoped adapter registration via `AddLamuFlixPersistence` and handler/validator via `AddLamuFlixHandlers` / `AddHandler`.

**Acceptance Scenarios**:

1. **Given** the API host service provider, **When** composition runs, **Then** the recommendations query handler and validator resolve through the existing `AddHandler` pipeline.
2. **Given** persistence registration, **When** composition runs, **Then** `IRecommendationCandidateSource` resolves to scoped `EfRecommendationCandidateSource`.
3. **Given** the route manifest test, **When** this ticket's DI changes land, **Then** the `/api` route list stays unchanged (no recommendations endpoint).

### Edge Cases

- Empty candidate pool → empty recommendations; genre calculator not called.
- Take default 10; Take > 0 required; no invented maximum; options.Take does not override query.Take.
- Missing selected → `NotFoundException`; repository null result follows existing handler pattern.
- Selected in any enrichment state is accepted; frozen mapper handles absent metadata.
- Zero-score candidates retained; no positive-score cutoff; no watched/not-interested exclusion; no candidate cap.
- Tie-break: score descending, then `MovieId.Value` ascending.
- Cancellation checked during scoring; caller token passed to selected lookup, candidate source, and genre calculator.
- Dependency/cancellation errors propagate; never success-with-empty on failure.
- Do not retest or reimplement sibling scoring formula (DEV-338).
- Do not invent placeholder copies of DEV-336/337/338 types to compile early; Phase B waits for Epic 7 and those siblings landed on main, then drift recon.
- No HTTP route, OpenAPI, generated client, or web UI.
- No MMR, embeddings/vector caps, schema/migrations, new package/project/layer, secrets, `Features:LocalPlay`, or `Process.Start`.
- No new glossary term or ADR (port already in constitution whitelist).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Create `sealed record GetRecommendationsQuery(MovieId MovieId, int Take = 10)` in `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQuery.cs` under namespace `LamuFlix.Core.Features.Recommendations`.
- **FR-002**: Create `sealed record RecommendedMovie(MovieSummary Summary, double Score, ImmutableList<Reason> Reasons)` in `src/LamuFlix.Core/Features/Recommendations/RecommendedMovie.cs`. Do not invent a competing result contract.
- **FR-003**: Create `GetRecommendationsQueryValidator` with FluentValidation rule `Take > 0` and no invented maximum. Query default Take is exactly 10; `RecommendationOptions.Take` MUST NOT override `query.Take`.
- **FR-004**: Create `GetRecommendationsQueryHandler` as a sealed class implementing `IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>`.
- **FR-005**: Handler MUST load the selected movie via `IMovieRepository.GetAsync(query.MovieId, ct)`. Null MUST throw parameterless `NotFoundException`. Selected movie in any enrichment state is accepted.
- **FR-006**: Create `IRecommendationCandidateSource` in `src/LamuFlix.Core/Ports/IRecommendationCandidateSource.cs` with `Task<IReadOnlyList<Movie>> GetCandidatesAsync(MovieId selectedMovieId, CancellationToken ct)`. Use existing domain `Movie`; do not introduce a candidate DTO or another port. The constitution whitelist already permits this interface; no ADR and no architecture-test whitelist edit.
- **FR-007**: Create sealed scoped `EfRecommendationCandidateSource` in `src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs`. Query `db.Movies.AsNoTracking()`, filter `Status == EnrichmentStatus.Enriched` and `Id != selectedMovieId`, include Actors/Directors/Genres/Writers with scalar metadata and flags, exactly one SQL command materializing the pool once (thread `ct` into EF execution). Privately map to domain movies via existing `Movie.Rehydrate` semantics. Retain full Includes and private Rehydrate; no projection rewrite is mandated (brief PD2; accepted Phase A memory/cartesian trade-off). MUST NOT call/expose private `EfMovieRepository.ToDomain` or rewrite `EfMovieRepository`. No pagination, pool cap, N+1 repository calls, or per-candidate database lookup. Mapping-parity proofs compare full domain state from candidate/repository reads in separate contexts (null `MetadataTitle` ⇒ null `Metadata`, empty and nonempty collections, scalars/flags/status/enrichment); keep the private null-Id guard without a fictitious persisted-null-PK test. A duplication gate finding stops for decision; no mapper/repository rewrite is authorized.
- **FR-008**: Handler orchestration MUST: (1) load selected then full candidate pool; (2) empty pool → `ImmutableList<RecommendedMovie>.Empty` before calling `GenreWeightsCalculator`; (3) otherwise call `GenreWeightsCalculator.GetAsync` once and read `IOptions<RecommendationOptions>.Value.Weights` once; (4) map selected features once via `MovieFeaturesMapper.From(Movie)`, then map/score each candidate exactly once via frozen `SimilarityScorer.Score(...)`; (5) rank by Score descending then `candidate.Id.Value` ascending; (6) return `min(query.Take, candidate count)` keeping zero-score candidates; (7) pass each `SimilarityResult.Score` and `Reasons` unchanged into `RecommendedMovie`.
- **FR-009**: Construct each `MovieSummary` with exactly: Id ← candidate.Id; Title ← candidate.Title; PosterUrl ← candidate.Metadata?.PosterUrl; ReleaseYear ← candidate.Metadata?.ReleaseYear?.Value; Runtime ← candidate.Metadata?.Runtime?.Minutes; ImdbRating ← candidate.Metadata?.ImdbRating?.Value; EnrichmentStatus ← candidate.Status; IsInWatchlist ← candidate.IsInWatchlist; IsWatched ← candidate.IsWatched; IsNotInterested ← candidate.IsNotInterested. No ParsedReleaseYear fallback, reason rewriting, new thresholds, watched/not-interested exclusion, or candidate cap.
- **FR-010**: Pass the caller cancellation token to selected lookup, candidate source, and genre calculator; check cancellation at entry, after each awaited dependency, and per scored candidate. Propagate cancellation and dependency errors; do not turn failures into successful empty results. A pre-cancelled token stops work with no dependency I/O.
- **FR-011**: Consume frozen sibling contracts without duplicating, stubbing, or replacing them: DEV-336 `MovieFeatures` / `MovieFeaturesMapper.From(Movie)`; DEV-337 scoped `GenreWeightsCalculator.GetAsync` and shared `GenreWeightsCache`; DEV-338 `SimilarityScorer.Score(...)` → `SimilarityResult` and `Reason`. Phase A may specify against frozen contracts; Phase B MUST wait for Epic 7 completion and DEV-336/337/338 implementations on main, then drift recon. Do not manufacture placeholder copies to compile early. Do not create a second genre-weights cache or alter sibling implementations.
- **FR-012**: Register scoped `IRecommendationCandidateSource` → `EfRecommendationCandidateSource` in `PersistenceServiceCollectionExtensions.AddLamuFlixPersistence`. Register handler and validator in `HandlerRegistration.cs` using existing `AddHandler` and validator patterns. Extend `ApiHostCompositionTests` via existing `ManifestContracts` for handler and validator contract coverage plus scoped adapter resolution (including scoped calculator and bound options dependencies); leave route manifest unchanged.
- **FR-013**: Approved new production files are exactly the six named in brief Q5. Approved new test files are exactly the five named in brief Q5/PD1 (four original plus `tests/LamuFlix.UnitTests/Persistence/EfRecommendationCandidateSourceSurvivorTests.cs`). Approved edits are only the three named existing files in brief Q5. No new project, architectural layer, service wrapper, mapper abstraction, package, HTTP route, OpenAPI/web shape, schema/migration, secrets, `Features:LocalPlay`, or `Process.Start`. No unrelated deletion or rewrite. Never alter mutation configuration or thresholds; Infrastructure mutation selects UnitTests only (recon-DEV-340 section 11).
- **FR-014**: Tests MUST match existing xUnit, Shouldly, NSubstitute, AutoFixture, and FsCheck conventions. Use actual sibling collaborators; substitute only I/O ports (`IMovieRepository`, `IRecommendationCandidateSource`, `IMovieCatalog`). Real PostgreSQL Testcontainers for both candidate-adapter proof files (IntegrationTests and UnitTests survivor). Property tests planned (not opted out); handler properties use valid pools excluding selected and do not assert self-exclusion. No Gherkin/Reqnroll unless separately opted in (not for this ticket).

### Key Entities

- **GetRecommendationsQuery**: Sealed query carrying selected `MovieId` and `Take` (default 10).
- **RecommendedMovie**: Sealed result pairing a `MovieSummary` with score and caller-safe `Reason` chips.
- **Recommendation**: Suggested library movie from the details page (glossary; this ticket produces the ranked list that feeds that concept).
- **Reason**: Caller-safe explanation attached to a recommendation (owned by DEV-338; passed through unchanged).
- **IRecommendationCandidateSource**: Port returning the Phase A candidate `Movie` pool for a selected id.
- **MovieSummary**: Existing ten-field port DTO used inside `RecommendedMovie`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a selected movie with a non-empty Phase A pool, the handler returns at most `Take` recommendations ordered by score descending (deterministic MovieId ascending on ties), each with attached reason chips from the scorer.
- **SC-002**: Empty pool yields an empty recommendation list without invoking the genre-weights calculator.
- **SC-003**: Missing selected movie yields `NotFoundException`; invalid Take (≤ 0) is rejected; default Take is 10 independent of `RecommendationOptions.Take`.
- **SC-004**: On real PostgreSQL (IntegrationTests and UnitTests survivor), the candidate adapter returns only enriched movies excluding the selected id, rehydrates collections/scalars/flags with repository parity (including null metadata and empty collections), executes exactly one SQL command for the candidate load on a fresh untracked context with no pool cap, and honors cancellation.
- **SC-005**: Zero-score candidates remain eligible; Score and Reasons are unchanged from `SimilarityResult`; all ten `MovieSummary` fields match the brief mapping table.
- **SC-006**: Host composition resolves handler, validator, and candidate adapter; route manifest remains unchanged (no recommendations HTTP endpoint).
- **SC-007**: Backend gates run with thresholds from `harness.yml` only; `/web` is `SKIPPED (scope-empty)`, never PASS. Property tests are executed (no opt-out). Scope-empty SKIPPED, configured SKIP, and mutation N/A are non-blocking but never recorded as PASS.

## Assumptions

- Tie-break on equal scores uses `MovieId.Value` ascending — Patron taste ruling tagged `[assumed]` in `specs/DEV-340/ASSUMPTIONS.md` (brief Q4).
- Phase B build intake remains gated on Epic 7 completion (`specs/PRODUCT.md` Non-Goals & Sequencing) and landed DEV-336/337/338 on main with drift recon (brief Q2, Q5). Specifying in Phase A does not authorize Phase B.
- Sibling contracts are frozen in their specs; at pinned HEAD they have zero implementation matches in `src`/`tests` (recon facts). Phase A specifies against those contracts.
- No owner structural checkbox was identified (brief Q5 care-list rulings; Q6).
- Exclusions in brief Q6 frozen scope remain out of scope for this feature.
