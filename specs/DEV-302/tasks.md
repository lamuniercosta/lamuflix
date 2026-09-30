# Tasks: EfMovieCatalog with Composable Predicates and Nulls-Last Sorting

**Input**: `specs/DEV-302/` spec.md, plan.md, brief.md §5.5 (task ordering)
**Tests**: Integration tests are required (Q12); each task pairs code with its test. Format: `[ID] [P?] Description (spec ref)`; `[P]` = parallelisable.

## Phase 1: Test data

- [ ] T001 Create `tests/LamuFlix.IntegrationTests/MovieCatalogSeed.cs`: seed helper over `LamuFlixDbContextFactory` a small hand-written deterministic factory in the style of `PersistenceRoundTripTests.cs:186-224`, building `MovieRecord`s with exact values (null runtime/year/rating, status, watchlist, tied keys and titles, bounds, literal `%`/`_`/`\`); a `MovieRecord` is an EF graph with genres/actors (brief D3). No package reference added, no csproj edited. Each test seeds its own data; tests use `Theory` + `MemberData` (FR-010)

## Phase 2: Predicates (each its own task with its own tests; order free after T001)

- [ ] T002 `WhereText` in `src/LamuFlix.Infrastructure/Persistence/MovieRecordQueryExtensions.cs` + cases in `EfMovieCatalogBrowseTests.cs` (FR-002, FR-003; Q4)
- [ ] T003 `WhereGenres` + tests (FR-004; Q5)
- [ ] T004 `WhereActors` + tests (FR-004; Q5)
- [ ] T005 `WhereRuntime` + tests incl. both-null bounds, `IncludeUnknown`, inactive branch, and both sides of each inclusive bound (bound passes, bound ± 1 fails) (FR-005; Q6)
- [ ] T006 `WhereYear` + tests incl. null years excluded, both-null bounds, inactive branch, and both sides of each inclusive bound (bound passes, bound ± 1 fails) (FR-005; Q6)
- [ ] T007 `WhereStatuses` + tests (FR-005; Q7)
- [ ] T008 `WhereInWatchlist` + tests (FR-005; Q7)

Note: T002–T008 share one source file and one test file; run them sequentially in one worktree, or merge carefully if split. Tests for these predicates run against the public predicate extensions directly over `LamuFlixDbContext.Movies`, since `BrowseAsync` arrives in T010; no `InternalsVisibleTo` is needed.

## Phase 3: Sorting (depends on T001 only)

- [ ] T009 Sort mapping (private whitelisted dispatch over the `MovieSort` member, one helper per key, nulls-last, Title/Id tie-breakers, no throw path: SmartEnum members are closed) + `EfMovieCatalogSortingTests.cs`: every `MovieSort` × {Asc, Desc}, nulls last both directions, ties (FR-006; Q8, Q9)

## Phase 4: Browse (depends on T002–T009)

- [ ] T010 `EfMovieCatalog.BrowseAsync`: composition, `CountAsync`, `Skip((Number-1)*Size)`/`Take`, direct `MovieSummary` projection, `AsNoTracking`, token forwarding + `EfMovieCatalogPagingTests.cs`: first/middle/last page, `TotalCount`, past-end, no overlap/gaps on ties, empty ChangeTracker, SQL shape (id/title only, no JOIN) (FR-001, FR-007; Q10, Q11)
- [ ] T011 Add ≥ 2 multi-filter AND combination tests through `BrowseAsync` to `EfMovieCatalogBrowseTests.cs` (spec US1.9)

## Phase 5: Details (independent of Phases 2–4; may run in parallel)

- [ ] T012 [P] `EfMovieCatalog.GetDetailsAsync` + `EfMovieCatalogDetailsTests.cs`: full metadata, none (`Metadata == null`), partial (Title fallback), missing Id → null; no entity materialisation (FR-008; Q2)

## Phase 6: Registration (depends on T010, T012)

- [ ] T013 `MovieCatalogServiceCollectionExtensions.AddMovieCatalog` (Scoped) + `MovieCatalogRegistrationTests.cs` registering `LamuFlixDbContext` itself (`AddMovieCatalog` provisions none), then resolving `EfMovieCatalog` with a distinct instance per scope (FR-009; Q3)

## Phase 7: Gates (last)

- [ ] T014 `./scripts/run-roslyn-analyzers.ps1` exit 0
- [ ] T015 `./scripts/run-cyclomatic-complexity.ps1` (≤ 15), then refactor gate `-Threshold 6`
- [ ] T016 `./scripts/run-jetbrains-inspectcode.ps1` exit 0
- [ ] T017 `dotnet format --verify-no-changes` clean
- [ ] T018 Full `dotnet test` green; `MigrationTests` and `PersistenceRoundTripTests` unchanged (SC-002)

## Dependencies

T001 → T002–T008, T009 → T010 → T011 → T013; T012 (after T001) → T013; T013 → T014–T018.
