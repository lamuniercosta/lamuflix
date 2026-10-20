# Tasks: DEV-337 GenreWeights IDF calculator

**Input**: Design documents from `specs/DEV-337/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why).

**Tests**: Included. Brief Q6 requires them. Author tests in the same phase as the types they lock. Once a type compiles, the phase is not done until the listed tests pass. Do not start a later phase before the earlier phase is green and committed. Do not start Phase 1 until the spec PR has merged, Gate 1 is established, and Phase B prerequisites (including Epic 7) allow build intake.

**Organization**: Phases follow brief Q6 task ordering and `spec.md` user stories US1–US4. Anvil implements one phase per ask. Glossary publication is Phase 1 setup. Polish is Cog then Gauge.

**Domain vocabulary** (must remain aligned with root `CONTEXT.md`): **Genre weight**, **GenreWeights**, **Genre similarity** — definitions in `spec.md` Domain vocabulary.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 counts, US2 math, US3 cache/DI, US4 invalidation.
- Exact file paths are in each description.

## Phase 1: Setup — glossary publication

**Purpose**: Publish the three approved domain terms before production work.

- [ ] T001 Confirm root `CONTEXT.md` carries **Genre weight**, **GenreWeights**, and **Genre similarity** with the definitions and `_Avoid_:` lines fixed in `specs/DEV-337/spec.md` Domain vocabulary (and already drafted in this worktree). Do not invent alternate wording. Keep existing Movie, Library, Recommendation, and Taste Profile terms unchanged.

**Checkpoint**: Glossary text matches the published Spec Kit vocabulary.

---

## Phase 2: User Story 1 - Library genre counts (Priority: P1) 🎯 MVP foundation

**Goal**: `GenreCounts` and `GetGenreCountsAsync` return one coherent snapshot via a single grouped PostgreSQL command.

**Independent Test**: Real Postgres proofs for empty library, zero-count genres, movies without genres, shared memberships, total not join-multiplied, one SQL command; existing facets unchanged.

### Tests for User Story 1

- [ ] T002 [P] [US1] Extend `tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs` for `GetGenreCountsAsync`: no movies / no genres; zero-count persisted genres remain in the map with total 0; movies without genres included in `TotalMovies`; shared and multiple memberships counted distinctly; total not derived from summed genre counts; one database command / coherent snapshot via command instrumentation on the existing Postgres fixture. Name methods `Method_Condition_Expected`. AAA headers only. Shouldly. No EF InMemory. Do not add a second test file.

### Implementation for User Story 1

- [ ] T004 [P] [US1] Add `src/LamuFlix.Core/Ports/GenreCounts.cs` as `sealed record GenreCounts(long TotalMovies, IReadOnlyDictionary<int, long> MoviesWithGenre)`.

- [ ] T005 [US1] Extend `src/LamuFlix.Core/Ports/IMovieCatalog.cs` with `Task<GenreCounts> GetGenreCountsAsync(CancellationToken ct)`.

- [ ] T003 [US1] After T005, adapt `RecordingMovieCatalog` in `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` only as compiler-forced for the new `IMovieCatalog` member. Do not change facet HTTP assertions beyond what compilation requires. Completes in the same Phase 2 commit as T005 (not marked [P]; depends on the unfinished port member).

- [ ] T006 [US1] Implement `GetGenreCountsAsync` in `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` as one grouped SQL query / one database command over existing tables. Semantics per brief Q2 / spec FR-002–FR-003. Do not alter `GetGenresAsync` facet behavior. No migration.

**Checkpoint**: US1 Postgres tests pass. Facet HTTP behavior unchanged. After T005, RecordingMovieCatalog adaptation (T003) compiles in the same phase commit.

---

## Phase 3: User Story 2 - GenreWeights math and genre similarity (Priority: P2)

**Goal**: Immutable IDF **GenreWeights** and weighted Jaccard **genre similarity**.

**Independent Test**: Unit and property tests with no catalog or repository.

### Tests for User Story 2

- [ ] T007 [US2] Write `tests/LamuFlix.UnitTests/Domain/GenreWeightsTests.cs` covering formula tolerance with double arithmetic, rare/common controlled comparison under a common union, rejection of negative N / negative df / df > N, empty-library and unknown-genre default weight 1.0, empty/identical/disjoint similarity, duplicate and permutation invariance, symmetry, bounds in [0,1], finite scores, no overall genre coefficient, and immutability when the input counts map is mutated after `Calculate`. Method names `Method_Condition_Expected`. xUnit v3 + Shouldly. AAA only.

- [ ] T008 [US2] After T007, add FsCheck properties in the same `tests/LamuFlix.UnitTests/Domain/GenreWeightsTests.cs` (brief names this single file for numerical and property proof) for set permutation/duplicate invariance, symmetry, and bounds. Each property: FsCheck.Xunit `[Property(MaxTest = 100)]`, `[Trait("Category", "Property")]`, explicit deterministic Replay seed, no `System.Random`, no algorithm restatement. Do not add a second property-test file.

### Implementation for User Story 2

- [ ] T009 [US2] Add `src/LamuFlix.Core/Domain/GenreWeights.cs`: sealed immutable table; `Calculate(GenreCounts)`; `GetWeight(int)`; `Similarity(IEnumerable<int>, IEnumerable<int>)` per brief Q3 / spec FR-004–FR-005. No comments.

**Checkpoint**: US2 unit and property tests pass.

---

## Phase 4: User Story 3 - Cache, calculator, DI (Priority: P3)

**Goal**: Generation-safe singleton cache, scoped calculator, shared DI registration.

**Independent Test**: TaskCompletionSource-controlled concurrency tests; each registration extension alone and both orders; calculator lifetime and registration; no scoped capture.

### Tests for User Story 3

- [ ] T010 [P] [US3] Write `tests/LamuFlix.UnitTests/Domain/GenreWeightsCacheTests.cs`: single-load reuse; invalidation during load discarding stale result and retrying; `Invalidate` completes before a held load is released and never waits on the async load gate; atomic generation/publication; cancelled gate holder with an independent surviving waiter that succeeds; retry cancellation releases the gate; success/error/cancellation all release synchronization without publishing incomplete state; already-returned snapshots remain after rebuild. Use TaskCompletionSource, not sleeps. Method names `Method_Condition_Expected`.

- [ ] T011 [P] [US3] Write `tests/LamuFlix.UnitTests/Features/Recommendations/GenreWeightsCalculatorTests.cs`: catalog orchestration through caller-owned loader/token into `GetOrLoadAsync`, cache hit reuse across scopes (NSubstitute catalog), per-caller cancellation, no scoped capture into the singleton, loader and scoped catalog/DbContext not retained after the call. Method names `Method_Condition_Expected`.

- [ ] T012 [P] [US3] Extend `tests/LamuFlix.IntegrationTests/MovieCatalogRegistrationTests.cs`: `TryAddSingleton<GenreWeightsCache>` via `AddLamuFlixPersistence` alone (valid existing persistence extension configuration; no secrets), `AddMovieCatalog` alone, and both orders; singleton lifetime; scoped calculator registration under `AddMovieCatalog`. Do not assert repository-and-calculator cache sharing here (scheduled after T017).

### Implementation for User Story 3

- [ ] T013 [US3] Add `src/LamuFlix.Core/Domain/GenreWeightsCache.cs` per brief Q4 and Phase 2 step 4 clarifications: BCL synchronization, immutable weights, generation counter only; `GetOrLoadAsync(Func<CancellationToken, Task<GenreWeights>> load, CancellationToken ct)` and `Invalidate()`; invalidate completion independent of in-flight loads; caller-token ownership for wait/load/retry; no new cache interface, shared background task, package, or layer.

- [ ] T014 [US3] Add `src/LamuFlix.Core/Features/Recommendations/GenreWeightsCalculator.cs` scoped `Task<GenreWeights> GetAsync(CancellationToken)` depending on `IMovieCatalog` and `GenreWeightsCache`, passing a caller-owned loader and token into `GetOrLoadAsync` without retaining scoped catalog/DbContext after the call.

- [ ] T015 [US3] Register `TryAddSingleton<GenreWeightsCache>()` in `AddLamuFlixPersistence` (`src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`) and `AddMovieCatalog` (`src/LamuFlix.Infrastructure/Persistence/MovieCatalogServiceCollectionExtensions.cs`); register scoped calculator in `AddMovieCatalog` per brief Q5. Do not rename the existing persistence extension.

**Checkpoint**: US3 unit and registration tests pass. Extension-alone and both-order registration resolve one shared cache; calculator registration and lifetime checks pass. Repository-and-calculator sharing is deferred to Phase 5.

---

## Phase 5: User Story 4 - Post-persistence invalidation (Priority: P4)

**Goal**: Successful catalog writes invalidate the cache so subsequent reads rebuild.

**Independent Test**: Real Postgres integration: warm cache, import/save, genre membership change, delete; failure/cancel/false-delete retain prior cache. After repository injection, prove repository and calculator share one cache under DI.

### Tests for User Story 4

- [ ] T016 [US4] Write `tests/LamuFlix.IntegrationTests/GenreWeightsIntegrationTests.cs` on the existing Postgres collection: warm cache; successful import/save rebuilds; successful genre-membership replacement rebuilds cached IDF; successful delete (`rows == 1`) rebuilds; false delete retains cache; failed/cancelled save retains cache; warm the cache, perform a successful scalar-only repository save with no genre change, and prove the next read reloads even if computed weights are equal; ordinary successful path covered as well as races/failures. Method names `Method_Condition_Expected`. AAA only. No handler edits or extra files.

### Implementation for User Story 4

- [ ] T017 [US4] Wire `GenreWeightsCache` into `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs`: invalidate at successful completion of the public `SaveChangesAsync` (covering every internal path) and after `TryDeleteAsync` when `rows == 1`, before return. Failed/cancelled saves exit before invalidation. Optional `GenreWeightsCache? cache = null` with `cache ?? new GenreWeightsCache()` for direct construction; DI supplies the shared singleton. No mutable static. No command-handler edits. Conservative invalidation on scalar saves.

- [ ] T018 [US4] After T017, extend `tests/LamuFlix.IntegrationTests/MovieCatalogRegistrationTests.cs` to prove the repository and calculator share one `GenreWeightsCache` singleton under DI (both extensions registered).

**Checkpoint**: US4 integration tests pass. Repository and calculator share one cache under DI. Prior phases remain green.

---

## Phase 6: Polish & gates

**Purpose**: Cog refactors the committed build diff. Gauge runs gates. Not an Anvil phase.

- [ ] T019 Refactor only files in the approved set in `specs/DEV-337/plan.md` until `./scripts/run-cyclomatic-complexity.ps1` meets the refactor limit in `harness.yml`. Extract helpers, early returns, guard clauses. Do not suppress. Do not lower thresholds. Do not add files or dependencies.

- [ ] T020 After T019, run configured gates and record each script verdict: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1`, `./scripts/run-jetbrains-inspectcode.ps1` (no `-All`), `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, `./scripts/run-mutation.ps1` without `--since`. Thresholds from `harness.yml`. Unchanged `/web` is `SKIPPED (scope-empty)`, never PASS. Exit 1 or Could not run blocks. Property-test exit 2 blocks unless the task note records a documented opt-out (this ticket has no opt-out). Mutation all-excluded is N/A, not PASS.

---

## Excluded (never scheduled)

Recommendation routes, ranking, MMR, candidate-source port, MovieFeatures integration or wait, option tuning, UI, OpenAPI, generated clients, new port/package/project/layer/schema, direct-SQL or distributed invalidation, TTL/background refresh, handler/playback edits, unrelated cleanup, secrets, `Features:LocalPlay`, `Process.Start`, Gherkin/Reqnroll, new ADR, second assertion/mocking library, EF InMemory, driver mocking. A discovered need outside this list stops for a decision.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: Glossary confirmation. Can start first.
- **Phase 2 (US1)**: Depends on Phase 1 for vocabulary alignment; blocks US2–US4 production that needs counts.
- **Phase 3 (US2)**: Depends on GenreCounts type from Phase 2 (Calculate input). Pure math does not need the adapter, but the record must exist.
- **Phase 4 (US3)**: Depends on GenreWeights from Phase 3 and catalog member from Phase 2. Registration proof covers extension-alone, both orders, lifetime, and calculator registration only.
- **Phase 5 (US4)**: Depends on cache and DI from Phase 4. T018 (repository-and-calculator cache sharing) depends on T017 repository injection.
- **Phase 6**: Depends on Phases 2–5 committed. Cog owns T019; Gauge owns T020.

### User Story Dependencies

- **US1 (P1)**: Counts contract — foundation.
- **US2 (P2)**: Uses GenreCounts; independently testable as pure math.
- **US3 (P3)**: Uses GenreWeights + IMovieCatalog; independently testable with substitutes.
- **US4 (P4)**: Uses shared cache + repository write paths; repository-and-calculator DI sharing proof after injection.

### Within Each User Story

- Author tests before or with production files in that phase; phase commit waits until tests pass.
- No sleeps in concurrency tests — TaskCompletionSource only.
- Do not widen the approved file set.

### Parallel Opportunities

- T002 may proceed once the counts contract shape is known. T004 can accompany T005. T003 is not [P]: it follows T005 and completes in the same Phase 2 commit.
- T007 then T008 on the same `GenreWeightsTests.cs` file (not parallel).
- T010, T011, and T012 in parallel before or with T013–T015 once GenreWeights exists.
- T016 before T017 within Phase 5 is preferred (tests first). T018 runs after T017 (same registration test file as T012; sequential, not parallel with T017).
- Do not parallelize two writers on the same file.

## Parallel Example: User Story 3

```text
Task: "T010 Write GenreWeightsCacheTests in tests/LamuFlix.UnitTests/Domain/GenreWeightsCacheTests.cs"
Task: "T011 Write GenreWeightsCalculatorTests in tests/LamuFlix.UnitTests/Features/Recommendations/GenreWeightsCalculatorTests.cs"
Task: "T012 Extend MovieCatalogRegistrationTests in tests/LamuFlix.IntegrationTests/MovieCatalogRegistrationTests.cs"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Publish glossary (Phase 1) and deliver counts (Phase 2).
2. Validate Postgres snapshot proofs.
3. Continue US2–US4 in order; frozen scope requires all four stories before review.

### Incremental Delivery

US2–US4 add calculator, cache, and invalidation required by ticket AC. Phase 6 does not add behavior.

### Parallel Team Strategy

One implementer in phase order is the expected path. Safe splits are listed under Parallel Opportunities.

## Notes

- [P] tasks use different files and do not depend on an unfinished task.
- Story labels map to `spec.md`.
- Commit after each implementation phase using `DEV-337 - {phase subject}`.
- Gate 1 comes from the merged spec PR. This task list does not authorize implementation before that, and Phase B remains gated on Epic 7.
- Quill invented no decisions; content is from `brief.md` Q1–Q7 and recon facts.
