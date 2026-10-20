# Feature Specification: DEV-337 GenreWeights IDF calculator

**Feature Branch**: `feature/337-spec`

**Created**: 2026-10-20

**Status**: Draft

**Input**: Ticket DEV-337 (size M, parent DEV-288, no ui); settled grill brief `specs/DEV-337/brief.md` (grill closed Q7/12 on 2026-10-20); facts from notes `recon-DEV-337` and `recon-DEV-337-2`; Patron rulings in `specs/DEV-337/CONCLUSIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## Domain vocabulary *(mandatory — published from root CONTEXT.md)*

These three terms are approved in brief Q3 and already present in root `CONTEXT.md`. Published Spec Kit artifacts must use them as written:

- **Genre weight**: The library rarity multiplier for a genre; rarer genres carry more weight in genre similarity. _Avoid_: genre preference, genre popularity score.
- **GenreWeights**: An immutable table of genre weights for one library catalog snapshot. _Avoid_: user taste weights, recommendation option weights.
- **Genre similarity**: The overlap of two movies genre sets, weighted by library rarity. _Avoid_: unweighted genre overlap, overall recommendation score.

Use existing Movie, Library, Recommendation, and Taste Profile vocabulary. These weights are neither user preference weights nor `RecommendationOptions.Weights.Genres`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Read library genre counts in one snapshot (Priority: P1)

A catalog caller asks for the library's movie total and the per-genre movie counts as one consistent snapshot. The result covers every persisted genre, including genres with zero movies, and counts distinct movie membership. The total includes movies that have no genres.

**Why this priority**: IDF and cache rebuild need real counts. Without the counts contract there is nothing to weigh.

**Independent Test**: Against real PostgreSQL, call the new catalog counts member for empty library, zero-count persisted genres, movies without genres, shared and multiple memberships, and prove one database command yields a coherent total and map. Existing `GET /genres` facet behavior is unchanged.

**Acceptance Scenarios**:

1. **Given** an empty library with no genre rows, **When** genre counts are read, **Then** `TotalMovies` is 0 and `MoviesWithGenre` is an empty map, and the empty-library path does not divide by zero.
2. **Given** persisted genre rows and zero movies, **When** genre counts are read, **Then** `TotalMovies` is 0 and every persisted genre ID appears in the map with count 0.
3. **Given** movies that have no genres and movies that share genres, **When** genre counts are read, **Then** `TotalMovies` counts every movie once (including movies without genres), each genre count is distinct membership, and the total is not derived by summing genre counts.
4. **Given** the existing genres facet HTTP path, **When** counts are added to the catalog port, **Then** `GET /genres` behavior and payloads stay unchanged.

---

### User Story 2 - Build GenreWeights and compute genre similarity (Priority: P2)

A caller builds an immutable **GenreWeights** table from a counts snapshot using natural-log IDF. For a genre with document frequency `df` in library size `N`, the **genre weight** is `ln((N + 1)/(df + 1)) + 1`. Empty-library and unknown-genre weights default to 1. Two movies' genre-ID sets yield a **genre similarity** score by weighted Jaccard: sum of weights in the intersection divided by sum of weights in the union. A shared rare genre contributes more than a shared common genre under a controlled common-union comparison.

**Why this priority**: This is the ticket's calculator and weighted overlap. It is useful once counts exist.

**Independent Test**: Unit and property tests over pure `GenreWeights` construction and `Similarity`, with no catalog or repository. Cover formula tolerance, rare/common controlled comparison, invalid counts, defaults, empty/identical/disjoint sets, duplicate/permutation invariance, symmetry, bounds, and immutability against later input-map mutation.

**Acceptance Scenarios**:

1. **Given** counts with library size N and a genre document frequency df, **When** GenreWeights is calculated, **Then** that genre's weight equals `ln((N + 1.0)/(df + 1.0)) + 1.0` using double arithmetic before division and addition.
2. **Given** an empty library (N = 0) or a genre ID absent from the table, **When** `GetWeight` is called, **Then** the weight is 1.0 and no division-by-zero occurs.
3. **Given** negative N, negative df, or df above N, **When** GenreWeights is calculated, **Then** the call is rejected.
4. **Given** two genre-ID sequences that share a rare genre versus two that share a common genre under the same union context, **When** genre similarity is computed, **Then** the rare shared genre contributes more than the common shared genre. The contract does not claim every arbitrary movie-pair score rises merely because one genre is rare.
5. **Given** empty union or disjoint nonempty sets, **When** similarity is computed, **Then** the score is 0. **Given** identical nonempty sets, **Then** the score is 1.
6. **Given** inputs with duplicates or different order, **When** similarity is computed, **Then** inputs are treated as sets, the score is finite, symmetric, order/duplicate invariant, and within [0, 1]. The configurable overall genre coefficient is not applied.
7. **Given** a published GenreWeights table, **When** the source counts map is mutated afterward, **Then** the table's weights do not change.

---

### User Story 3 - Cache GenreWeights across the process (Priority: P3)

A scoped calculator returns GenreWeights for the current catalog snapshot. A process-wide generation-aware cache holds the immutable table, rebuilds lazily on first read after invalidation, serializes concurrent loads, discards stale results when invalidation races a load, and never captures a scoped catalog or DbContext. `Invalidate` completes independently of any in-flight database load. Only the current gate holder invokes its own scoped loader; gate wait, load, and generation-mismatch retries use that caller token. DI registers one shared cache from either persistence or catalog extension alone, and from both registration orders.

**Why this priority**: The ticket requires in-memory caching of IDF values. Math alone does not satisfy the cache AC.

**Independent Test**: TaskCompletionSource-controlled unit tests for single-load reuse across scopes, invalidation during load with discarded stale result and retry, invalidation completion before a held load is released, a waiter that survives holder cancellation and succeeds, retry cancellation that releases the gate, atomic invalidation/publication, cancellation/error recovery, and synchronization release on success/error/cancellation. Registration tests for each extension alone and both orders; repository and calculator share one cache under DI.

**Acceptance Scenarios**:

1. **Given** a warm cache and no invalidation, **When** two scopes request GenreWeights, **Then** both receive the same immutable snapshot and the catalog counts query runs once.
2. **Given** invalidation during an in-flight counts query, **When** that query completes, **Then** its result is discarded, a retry loads fresh counts, and the discarded result never becomes the current cached table. **Given** a load is held, **When** `Invalidate` is called, **Then** invalidation completes before the held load is released and never waits on the async load gate.
3. **Given** cancellation or an error during load, **When** the attempt ends, **Then** incomplete state is not published and the synchronization gate is not stranded. **Given** the gate holder cancels, **When** an independent waiter continues, **Then** that waiter is not cancelled by the holder and can succeed. **Given** a generation-mismatch retry is cancelled, **Then** the gate is released.
4. **Given** `AddLamuFlixPersistence` alone, **When** the host builds the service provider, **Then** `GenreWeightsCache` and the repository resolve against that shared cache. **Given** `AddMovieCatalog` alone, **When** the host builds the service provider, **Then** `GenreWeightsCache`, the catalog, and the scoped calculator resolve against that cache. **Given** both extensions in either order, **When** the host builds the service provider, **Then** there is one shared `GenreWeightsCache` singleton.
5. **Given** a GenreWeights table already returned to a caller, **When** the cache later rebuilds, **Then** the already-returned table remains that earlier snapshot.

---

### User Story 4 - Refresh weights after catalog writes (Priority: P4)

After a successful movie save (including import and genre-membership replacement) or a successful movie delete (`rows == 1`), the repository invalidates the shared cache at successful completion of the public `SaveChangesAsync` (covering every internal branch) or after `TryDeleteAsync` when `rows == 1`, before returning. A reader that starts after that completed write rebuilds from fresh counts. Failed or cancelled saves exit before invalidation and leave the prior cache intact; false deletes leave the prior cache intact. Scalar saves also invalidate (conservative cost accepted).

**Why this priority**: Acceptance requires cached IDF values to change after genre membership changes and after movies are added or removed.

**Independent Test**: Integration tests on real PostgreSQL: warm the cache, import/save movies, change genre memberships, remove movies; subsequent reads rebuild with changed counts/weights. Warm the cache, perform a successful scalar-only repository save with no genre change, and prove the next read reloads even if computed weights are equal. Failed/cancelled saves and false deletes retain prior cache. Cover the ordinary successful path as well as races/failures.

**Acceptance Scenarios**:

1. **Given** a warm GenreWeights cache, **When** a movie is imported or saved successfully, **Then** the next GenreWeights read rebuilds using the updated counts.
2. **Given** a warm cache, **When** a movie's genre memberships are replaced successfully, **Then** the next read rebuilds and cached IDF values reflect the membership change.
3. **Given** a warm cache, **When** a movie is deleted successfully (`rows == 1`), **Then** the next read rebuilds. **When** delete reports no row, **Then** the prior cache remains.
4. **Given** a warm cache, **When** a save fails or is cancelled, **Then** invalidation is not reached, the prior cache remains, and incomplete state is not published.
5. **Given** a warm cache and a successful scalar repository save that does not change genres, **When** the next GenreWeights read runs, **Then** the cache reloads even if the computed weights are equal (extra reload is accepted).

### Edge Cases

- Empty library: counts total 0, empty or zero-count genre map as ruled; GenreWeights defaults unknown/empty weights to 1.0; no division by zero.
- Movies without genres contribute to `TotalMovies` and do not invent genre rows.
- Zero-count persisted genres remain in the map; DEV-411 is already merged, so real persisted counts are delivered (not placeholder zeros).
- Invalid counts (negative N, negative df, df > N) are rejected at GenreWeights calculation.
- Empty union and disjoint sets score 0; identical nonempty sets score 1.
- Duplicate genre IDs in similarity inputs collapse to set membership.
- Concurrent load plus invalidation never publishes a stale table as current.
- Cancellation/errors never strand the cache gate or publish partial state.
- Optional constructor fallback `GenreWeightsCache? cache = null` with `cache ?? new GenreWeightsCache()` is allowed for directly constructed repositories; DI always supplies the shared singleton. No mutable static fallback.
- Existing `GetGenresAsync` facet counts are not a substitute for the ticket-required grouped counts contract.
- No recommendation route, ranking, MMR, candidate-source port, MovieFeatures integration, options tuning, UI, OpenAPI, or generated client changes.
- No secrets, `Features:LocalPlay`, or `Process.Start` changes.
- Phase B build remains gated on Epic 7 completion under `specs/PRODUCT.md`; this Phase A specification does not claim Epic 7 is complete.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Extend `IMovieCatalog` with `Task<GenreCounts> GetGenreCountsAsync(CancellationToken ct)`. Add `sealed record GenreCounts(long TotalMovies, IReadOnlyDictionary<int, long> MoviesWithGenre)` in Core/Ports beside existing catalog contracts.
- **FR-002**: `TotalMovies` MUST include all library movies, including those with no genres. `MoviesWithGenre` MUST include all persisted genre IDs, including zero-count genres, and MUST count distinct movie membership. No genre rows means an empty map. Zero movies means total zero while any persisted zero-count genre IDs remain in the map.
- **FR-003**: `EfMovieCatalog` MUST execute one grouped SQL query / one database command over existing tables, returning a consistent total and genre-count snapshot. It MUST NOT use separate total and per-genre round trips, MUST NOT derive total from summed genre counts, MUST NOT alter existing facet HTTP behavior, and MUST NOT add a migration or schema change. Query translation and command count MUST be proven on real PostgreSQL.
- **FR-004**: `GenreWeights.Calculate(GenreCounts)` MUST build a sealed immutable weight table. For genre count `df` in library size `N`, compute `Math.Log((N + 1.0)/(df + 1.0)) + 1.0` with double arithmetic before division/addition. Reject negative N, negative df, and df above N. Empty-library and unknown-genre weights MUST default to `1.0` through `GetWeight(int)`.
- **FR-005**: `Similarity(IEnumerable<int>, IEnumerable<int>)` MUST treat inputs as sets (deduplicate genre IDs), return the sum of weights in the intersection divided by the sum in the union, return 0 for empty union and for disjoint sets, return 1 for identical nonempty sets, and produce scores that are finite, symmetric, invariant under input order/duplicates, and within [0, 1]. It MUST NOT multiply by the configurable overall genre coefficient. Published tables MUST remain immutable if the input map is later mutated.
- **FR-006**: Place `GenreWeights` and `GenreWeightsCache` in `Core/Domain/`. Place scoped `GenreWeightsCalculator` in `Core/Features/Recommendations/` with `Task<GenreWeights> GetAsync(CancellationToken)`, depending on scoped `IMovieCatalog` and the singleton cache. `GenreWeightsCache` MUST expose `Task<GenreWeights> GetOrLoadAsync(Func<CancellationToken, Task<GenreWeights>> load, CancellationToken ct)` and `void Invalidate()`. Rebuild lazily on first read after invalidation; serialize loads; recheck generation atomically with publication; discard and retry if invalidation occurs during a query. `Invalidate` MUST complete independently of any in-flight database load and MUST NOT wait on the async load gate; brief synchronous publication synchronization is permitted and no lock-free algorithm is mandated. Only the current gate holder MAY invoke its own scoped loader; gate wait, load, and generation-mismatch retries MUST use that caller token. Cancellation of the holder MUST release the gate without cancelling independent waiters. Never publish incomplete state; never retain the loader or scoped catalog/DbContext after the call completes; never capture scoped catalog or DbContext in the singleton. No new cache interface, shared background task, package, or layer.
- **FR-007**: No new cache port/interface, package, architectural layer, TTL, cache option, or background refresh. No new ADR: the existing catalog port is extended, not the closed port set.
- **FR-008**: Inject the shared cache into `EfMovieRepository`. Invalidate at successful completion of the public `SaveChangesAsync` (covering every internal path) and after `TryDeleteAsync` only when `rows == 1`, before returning. Do not invalidate on pre-save `AddAsync`, failed/cancelled saves (which MUST exit before invalidation), or false deletes. No command-handler edits. Conservatively invalidate on every successful repository save, including scalar saves.
- **FR-009**: Register with `TryAddSingleton<GenreWeightsCache>()` in both `PersistenceServiceCollectionExtensions.AddLamuFlixPersistence` and `MovieCatalogServiceCollectionExtensions.AddMovieCatalog`; register the scoped calculator with `AddMovieCatalog`. Both registration orders MUST yield one shared cache. An optional `GenreWeightsCache? cache = null` constructor parameter with instance fallback is allowed for directly constructed repositories; DI always supplies the shared singleton. Never introduce a mutable static fallback. No production rename of the existing persistence extension.
- **FR-010**: Adapt `RecordingMovieCatalog` in `LibraryEndpointsTests` only as compiler-forced for the new port member. Extend `EfMovieCatalogFacetsTests` and `MovieCatalogRegistrationTests` as named in the brief. Add the named unit and integration test files. Use xUnit v3, Shouldly, NSubstitute for unit collaborators, FsCheck for properties, and the existing real PostgreSQL Testcontainers fixture. No driver mocking, EF InMemory, second assertion/mocking library, new test dependency, or Gherkin/Reqnroll.
- **FR-011**: Publish the three approved glossary terms in root `CONTEXT.md` (already drafted in this worktree) with the definitions in the Domain vocabulary section above. No new NuGet/npm package, project, top-level folder, architectural layer, public API route/DTO/status code, schema/migration, secrets, `Features:LocalPlay`, or `Process.Start` change. Do not wait for or implement MovieFeatures. Do not add recommendation routes, ranking, MMR, candidate-source port, or option tuning.

### Key Entities

- **GenreCounts**: Snapshot of library size and per-genre distinct movie counts for one catalog read.
- **Genre weight**: Library rarity multiplier for one genre (see Domain vocabulary).
- **GenreWeights**: Immutable table of genre weights for one library catalog snapshot (see Domain vocabulary).
- **Genre similarity**: Weighted Jaccard overlap of two movies' genre-ID sets (see Domain vocabulary).
- **GenreWeightsCache**: Process-wide generation-aware holder of the current GenreWeights snapshot.
- **GenreWeightsCalculator**: Scoped orchestrator that loads counts, calculates GenreWeights, and publishes through the cache.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On real PostgreSQL, one catalog counts call returns a coherent `TotalMovies` and per-genre map for empty library, zero-count genres, movies without genres, and multi-membership cases, using a single database command.
- **SC-002**: Rare shared genres contribute a measurably higher similarity contribution than common shared genres under the controlled common-union comparison in the brief.
- **SC-003**: Empty-library and unknown-genre weights are 1.0 with no division by zero.
- **SC-004**: After successful import, genre-membership change, or successful delete, the next GenreWeights read reflects updated counts; failed/cancelled saves and false deletes leave the prior cache.
- **SC-005**: Concurrent load and invalidation never leave a stale table as current, and cancellation/errors never publish incomplete state or strand the gate.
- **SC-006**: Each registration extension alone and both orders resolve one shared cache; repository and calculator share that instance under DI; already-returned tables remain snapshots after rebuild.
- **SC-007**: Root `CONTEXT.md` carries the three approved terms with the published definitions. Backend gates run with thresholds from `harness.yml` only; scope-empty skips are not recorded as passes. Unchanged `/web` is scope-empty skip.

## Assumptions

- DEV-411 is already merged and persists genres; real counts are delivered, not placeholder zeros (brief Q1; recon section 7). Logged as settled precondition, not taste.
- Phase B build intake remains gated on Epic 7 completion (`specs/PRODUCT.md:17`). Specifying in Phase A does not authorize Phase B (brief identity).
- No taste decisions were made; no `ASSUMPTIONS.md` is required (brief Canonical vocabulary).
- No owner structural checkbox was identified (brief Q1, handoff).
- Exclusions in brief Q7 remain out of scope for this feature.
