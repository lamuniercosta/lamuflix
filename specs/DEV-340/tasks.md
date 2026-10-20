# Tasks: DEV-340 GetRecommendationsQuery and Phase A candidate provider

**Input**: Design documents from `specs/DEV-340/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why).

**Tests**: Included. Brief Q5/PD1 requires them. Author tests in the same phase as the types they lock. Compile-red is permitted while types are introduced inside a phase; the phase is not done until listed tests are green and the phase is committed before the next phase starts. Do not start Phase 2 production until Phase 1 drift recon passes, the spec PR has merged, Gate 1 is established, and Phase B prerequisites (Epic 7 + landed DEV-336/337/338) allow build intake.

**Organization**: Phases follow brief Q5/PD1 Phase B task order and `spec.md` user stories US1–US3. Anvil implements one phase per ask. Polish is Cog then Gauge.

**Domain vocabulary** (must remain aligned with root `CONTEXT.md`): **Recommendation**, **Reason** — definitions in `spec.md` Domain vocabulary. No new glossary edit.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 candidate source, US2 handler/rank, US3 DI/composition.
- Exact file paths are in each description.

## Phase 1: Setup — prerequisite drift recon

**Purpose**: Confirm Epic 7 sequencing and landed sibling contracts before production work.

- [ ] T001 Wisp prerequisite/drift recon: confirm Epic 7 completion gate, DEV-336/337/338 implementations landed on main, and compatibility of this brief's consumed signatures (`MovieFeaturesMapper.From`, `GenreWeightsCalculator.GetAsync` / `GenreWeightsCache`, `SimilarityScorer.Score` / `SimilarityResult` / `Reason`). Re-read the shared `PersistenceServiceCollectionExtensions` body after siblings land (no stale line assumptions; do not create a second cache). Record drift findings before any production edit. Stop for a decision if signatures diverge from the brief.

**Checkpoint**: Sibling contracts match the brief; Phase B build intake is allowed.

---

## Phase 2: Foundational — query, result, and validator contracts

**Purpose**: Ticket-owned sealed contracts and Take validation before candidate/handler work.

**⚠️ CRITICAL**: US1–US3 production that depends on these types waits on this phase.

- [ ] T002 [P] Add `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQuery.cs` as `sealed record GetRecommendationsQuery(MovieId MovieId, int Take = 10)` in namespace `LamuFlix.Core.Features.Recommendations`.

- [ ] T003 [P] Add `src/LamuFlix.Core/Features/Recommendations/RecommendedMovie.cs` as `sealed record RecommendedMovie(MovieSummary Summary, double Score, ImmutableList<Reason> Reasons)`.

- [ ] T004 Add `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryValidator.cs` with FluentValidation `Take > 0` and no invented maximum. Default Take remains exactly 10 on the query record.

- [ ] T005 Write `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryValidatorTests.cs`: Take > 0 passes; zero/negative Take fail. Validator scope is `Take > 0` only — do not assert query default 10 or options.Take independence here (handler owns those proofs in T009). Method names `Method_Condition_Expected`. Established xUnit + Shouldly conventions. AAA headers only.

**Checkpoint**: Contracts compile; validator tests pass. Compile-red may occur while introducing types; green before Phase 3.

---

## Phase 3: User Story 1 - Phase A candidate source (Priority: P1) 🎯 MVP foundation

**Goal**: `IRecommendationCandidateSource` and `EfRecommendationCandidateSource` return the full enriched-except-selected pool in one untracked SQL command.

**Independent Test**: Real Postgres proofs (IntegrationTests + UnitTests survivor) for enrichment filtering, selected exclusion, rehydration/parity, no tracking, exactly one SQL command/no-N+1, no cap, cancellation.

### Tests for User Story 1

- [ ] T006 [US1] Write `tests/LamuFlix.IntegrationTests/EfRecommendationCandidateSourceTests.cs` on `PostgresCollection` / `PostgresFixture`: filter every enrichment state; exclude selected id; rehydrate Actors/Directors/Genres/Writers plus scalars/flags (empty and nonempty collections; null `MetadataTitle` ⇒ null `Metadata`); full-state parity vs repository reads on separate contexts; exactly one SQL command for the candidate load via scoped `DbCommandInterceptor` (exclude setup/parity repository commands); fresh query context with empty `ChangeTracker.Entries()` afterward; no pool cap; cancellation propagates; thread `ct` into EF execution. Method names `Method_Condition_Expected`. AAA only. Shouldly. No EF InMemory. Never mock the EF driver.

- [ ] T006a [US1] Write `tests/LamuFlix.UnitTests/Persistence/EfRecommendationCandidateSourceSurvivorTests.cs` on real PostgreSQL via `PostgresFixture` (brief PD1): mutation-killing assertions for all-state enriched filter, selected-id exclusion, four collections/scalars/flags, null metadata, repository mapping parity (separate contexts), and command count (scoped interceptor; exclude setup/parity). Retain IntegrationTests (T006). Infrastructure mutation selects UnitTests only (recon-DEV-340 section 11); thresholds/config unchanged. Method names `Method_Condition_Expected`. AAA only. Shouldly. Never mock the EF driver.

### Implementation for User Story 1

- [ ] T007 [US1] Add `src/LamuFlix.Core/Ports/IRecommendationCandidateSource.cs` with `Task<IReadOnlyList<Movie>> GetCandidatesAsync(MovieId selectedMovieId, CancellationToken ct)`. Port precedes compiling adapter and Postgres tests.

- [ ] T008 [US1] Add sealed `src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs`: `AsNoTracking`, `Status == EnrichmentStatus.Enriched`, `Id != selectedMovieId`, include four collections + scalars/flags, exactly one SQL command and one materialization, thread `ct` into EF execution, private `Movie.Rehydrate` mapping. Retain full Includes and private Rehydrate; no projection rewrite. Do not call private `EfMovieRepository.ToDomain` or rewrite `EfMovieRepository`. Keep private null-Id guard without a fictitious persisted-null-PK path. No pagination or pool cap. Duplication gate finding stops for decision; no mapper/repository rewrite authorized.

**Checkpoint**: US1 IntegrationTests and UnitTests survivor proofs pass. Compile-red permitted while introducing types; green/committed before Phase 4.

---

## Phase 4: User Story 2 - Rank top-N recommendations (Priority: P2)

**Goal**: Handler returns top-N `RecommendedMovie` values with attached reason chips.

**Independent Test**: Unit and property tests with real sibling collaborators; I/O ports substituted only. Selected-id exclusion proved only in adapter suites (T006/T006a), not as a handler property (brief PD2).

### Tests for User Story 2

- [ ] T009 [US2] Write `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs`: missing selected → `NotFoundException`; existing unenriched selected accepted; empty/fewer/exact/more-than-N candidates; query default Take exactly 10 recorded once; two behavioral cases with enough candidates — (a) `options.Take != 10` + omitted query.Take still returns 10; (b) explicit query.Take differs from options.Take and returns `min(query.Take, count)`; ties (Id ascending); zero-score retention; all ten summary fields (null-metadata summaries keep null metadata-derived scalars); Score/Reasons passthrough; empty pool skips genre calculator; cancellation and dependency errors propagate. Cancellation proofs: pre-cancelled token performs no dependency calls; deterministic mid-scoring cancellation after scoring has begun for at least one candidate and before later candidates complete — without collaborator mocks of frozen types or sleep/timing races. Checkpoints covered: entry, after awaited dependencies, per candidate. Constructor wiring uses real `Options.Create`, fresh per-test `GenreWeightsCache`, and catalog `GetGenreCountsAsync` for calculator counts. Use real `MovieFeaturesMapper`, `SimilarityScorer`, `GenreWeightsCalculator`, `GenreWeightsCache`; substitute only `IMovieRepository`, `IRecommendationCandidateSource`, `IMovieCatalog`. Method names `Method_Condition_Expected`. AAA only.

- [ ] T010 [US2] Write `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerPropertyTests.cs`: FsCheck properties for total ordering (Score descending then `Id.Value` ascending, including permutation invariance), count bounds, and permutation invariance with real collaborators and valid candidate-source pools that exclude selected by construction. Do not assert handler self-exclusion (adapter owns that proof in T006/T006a). No defensive handler filter. Do not recreate scorer internals. Each property: FsCheck.Xunit `[Property(MaxTest = 100)]`, `[Trait("Category", "Property")]`, explicit deterministic Replay seed, no `System.Random`.

### Implementation for User Story 2

- [ ] T011 [US2] Add sealed `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs` implementing `IQueryHandler<GetRecommendationsQuery, ImmutableList<RecommendedMovie>>` per brief Q4 / spec FR-005–FR-010. Constructor dependencies: `IMovieRepository`, `IRecommendationCandidateSource`, `GenreWeightsCalculator`, `IOptions<RecommendationOptions>`. Rank Score descending then `MovieId.Value` ascending. Pass Score/Reasons unchanged. Build `MovieSummary` from the brief field table only. Explicit cancellation at entry, after each awaited dependency, and per scored candidate. Preserve error propagation and empty-pool short-circuit. No defensive selected-id filter.

**Checkpoint**: US2 unit and property tests pass. Prior phases remain green.

---

## Phase 5: User Story 3 - DI and host composition (Priority: P3)

**Goal**: Handler, validator, and candidate adapter resolve through established DI; route manifest unchanged.

**Independent Test**: Composition via existing registrations and `ManifestContracts`; handler/validator/scoped adapter coverage; route list unchanged.

### Tests for User Story 3

- [ ] T012 [US3] Extend `tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs` via existing `ManifestContracts` for handler and validator contract coverage plus scoped adapter resolution (including scoped calculator and bound options dependencies). Leave the `/api` route manifest unchanged. Author the composition assertions with the types; run green after T013/T014 registrations land.

### Implementation for User Story 3

- [ ] T013 [US3] Register scoped `IRecommendationCandidateSource` → `EfRecommendationCandidateSource` in `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` inside `AddLamuFlixPersistence`. Consume DEV-337 calculator/cache registrations as landed; do not create a second cache.

- [ ] T014 [US3] Register handler and validator in `src/LamuFlix.Api/HandlerRegistration.cs` using existing `AddHandler` and validator patterns.

**Checkpoint**: Composition tests pass after T013/T014. Route manifest unchanged. Phases 2–4 remain green.

---

## Phase 6: Polish & gates

**Purpose**: Cog refactors the committed build diff. Gauge runs gates. Not an Anvil phase. Gate and loop obligations: brief Q5 Gauge expectations, brief Q6 closing bar/cap/scope, and `AGENTS.md` verification commands / harness thresholds.

- [ ] T015 Refactor only files in the approved set in `specs/DEV-340/plan.md` until `./scripts/run-cyclomatic-complexity.ps1` meets the refactor limit in `harness.yml`. Extract helpers, early returns, guard clauses. Do not suppress. Do not lower thresholds. Do not add files or dependencies.

- [ ] T016 After T015, run configured gates and record each script verdict per brief Q5 / `AGENTS.md`: Roslyn analyzers, cyclomatic complexity, InspectCode, property tests, vulnerable packages, `dotnet format --verify-no-changes`, `dotnet test`, and `./scripts/run-mutation.ps1` without `--since`. Thresholds from `harness.yml` only — never lowered. Infrastructure mutation selects UnitTests only (recon-DEV-340 section 11). Unchanged `/web` is `SKIPPED (scope-empty)`, never PASS. Exit 1 or Could not run blocks. Property-test exit 2 blocks unless the task note records a documented opt-out (this ticket has no opt-out). Mutation all-excluded is N/A, not PASS.

---

## Excluded (never scheduled)

HTTP endpoints, OpenAPI, generated clients, web UI, MMR, embeddings/vector caps, new eligibility filters/thresholds, sibling implementation changes or placeholder stubs, schema/migrations, dependency/project/layer additions, unrelated rewrites, secrets, `Features:LocalPlay`, `Process.Start`, Gherkin/Reqnroll, new ADR, architecture whitelist edit, second assertion/mocking library, EF InMemory, driver mocking for translation/serialization, glossary edit, mutation config/threshold changes, projection rewrite / mapper abstraction, defensive handler selected-id filter. A discovered need outside this list stops for a decision.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: Drift recon. Blocks all production.
- **Phase 2**: Contracts/validator. Blocks US1–US3 types that need query/result shapes; validator tests lock Take > 0 only.
- **Phase 3 (US1)**: Depends on Phase 1; T007 port before compiling adapter/tests (T006/T006a/T008); IntegrationTests and UnitTests survivor both required.
- **Phase 4 (US2)**: Depends on Phase 2 contracts and Phase 3 port (handler depends on `IRecommendationCandidateSource`).
- **Phase 5 (US3)**: Depends on Phases 2–4 types existing; T012 authored before registration, run green after T013/T014.
- **Phase 6**: Depends on Phases 2–5 committed. Cog owns T015; Gauge owns T016.

### User Story Dependencies

- **US1 (P1)**: Candidate port/adapter — foundation for scoring; owns selected-id exclusion proof.
- **US2 (P2)**: Handler rank/take — ticket AC; needs US1 port and Phase 2 contracts; properties use valid pools only.
- **US3 (P3)**: DI/composition — needs US1–US2 types registered.

### Within Each User Story

- Compile-red is permitted while introducing types inside a phase; green and committed before the next phase.
- Use real sibling collaborators in handler tests; substitute I/O ports only.
- Do not widen the approved file set (five new test files per brief PD1).

### Parallel Opportunities

- T002 and T003 in parallel; T004 then T005 (validator file then its tests).
- T007 precedes T006/T006a/T008; T006 and T006a may be authored in parallel once the adapter shape is fixed, both green with T008 before Phase 4.
- T009 then T010 then T011 within Phase 4 (tests before or with handler; property file separate from unit file — T009/T010 may be [P] relative to each other once handler shape is fixed, but both should fail before T011 completes).
- T012 authored with types; T013 then T014; composition run green after registrations.
- Do not parallelize two writers on the same file.

## Parallel Example: Foundational contracts

```text
Task: "T002 Add GetRecommendationsQuery in src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQuery.cs"
Task: "T003 Add RecommendedMovie in src/LamuFlix.Core/Features/Recommendations/RecommendedMovie.cs"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Complete drift recon (Phase 1) and contracts (Phase 2).
2. Deliver candidate source (Phase 3) and validate on Postgres (IntegrationTests + UnitTests survivor).
3. Continue US2–US3 in order; frozen scope requires all three stories before review.

### Incremental Delivery

US2 adds the ticket AC (top-N with reason chips). US3 wires DI. Phase 6 does not add behavior.

### Parallel Team Strategy

One implementer in phase order is the expected path. Safe splits are listed under Parallel Opportunities.

## Notes

- [P] tasks use different files and do not depend on an unfinished task.
- Story labels map to `spec.md`.
- Commit after each implementation phase using `DEV-340 - {phase subject}`.
- Gate 1 comes from the merged spec PR. This task list does not authorize implementation before that, and Phase B remains gated on Epic 7 plus landed DEV-336/337/338.
- Quill invented no decisions; content is from `brief.md` Q1–Q6/PD1/PD2, `ASSUMPTIONS.md`, `CONCLUSIONS.md`, and named recon facts.
