# Tasks: EfMovieCatalog with Composable Predicates and Nulls-Last Sorting

**Input**: `specs/DEV-302/` spec.md, plan.md, brief.md §5.5 (task ordering), §7b (plan-challenge rulings D6–D24)
**Tests**: Integration tests are required (Q12). Each task pairs code with its test. Format: `[ID] [P?] Description (spec ref)`. `[P]` = parallelisable.
**Test conventions (brief D23)**: `Method_Condition_Expected` names; `TestContext.Current.CancellationToken` on every async call; only `// arrange`, `// act`, `// assert` comments; seed timestamps as fixed `DateTimeOffset` literals (no `DateTime.Now`/`UtcNow`); `Theory` + `MemberData` in place of repeated `Fact`s.
**Isolation (brief D11)**: each test clears the catalog tables in its own arrange step before seeding. Test classes that share a `PostgresFixture` database are in one xUnit collection. A test only ever asserts on rows it seeded.

## Phase 1: Test data

- [ ] T001 Create `tests/LamuFlix.IntegrationTests/MovieCatalogSeed.cs`: a seed helper over `LamuFlixDbContextFactory`. It is a small hand-written deterministic factory in the style of `PersistenceRoundTripTests.cs:186-224`. It builds `MovieRecord`s with exact values: null runtime, year and rating; status; watchlist; tied keys and titles; bounds; literal `%`, `_`, `\` and the combined `100%\file_name_v2`; and a trailing-space-sensitive title set (`Movie`, `Movie Night`). A `MovieRecord` is an EF graph with genres and actors (brief D3). The helper also provides the per-test table reset (brief D11). No package reference is added and no csproj is edited (FR-010)

## Phase 2: Predicates (each its own task with its own tests; order free after T001)

- [ ] T002 `WhereText` in `src/LamuFlix.Infrastructure/Persistence/MovieRecordQueryExtensions.cs`, with cases in `EfMovieCatalogBrowseTests.cs`. Include the no-trim case (`"Movie "` matches `Movie Night`, not `Movie`) and the combined-metacharacter literal cases (FR-002, FR-003; Q4; D15)
- [ ] T003 `WhereGenres` + tests: inactive when `IsDefaultOrEmpty`, with an explicit `default(ImmutableArray<int>)` case; duplicate id in input returns the movie once (FR-004; Q5; D7, D15)
- [ ] T004 `WhereActors` + tests, the same cases as T003 (FR-004; Q5; D7, D15)
- [ ] T005 `WhereRuntime` + tests. Inactive exactly when `range is null`, as a separate case from both-null bounds. Also `IncludeUnknown` and both sides of each inclusive bound (bound passes, bound ± 1 fails). Unknown-value handling and bound application go in separate private helpers (FR-005; Q6; D8, D12)
- [ ] T006 `WhereYear` + tests. Null years excluded; inactive exactly when `range is null`, separate from both-null bounds; both sides of each inclusive bound. Same helper split as T005 (FR-005; Q6; D8, D12)
- [ ] T007 `WhereStatuses` + tests, inactive when `IsDefaultOrEmpty`, with a `default` case (FR-005; Q7; D7)
- [ ] T008 `WhereInWatchlist` + tests (FR-005; Q7)

Note: T002–T008 share one source file and one test file. Run them sequentially in one worktree, or merge carefully if split. These tests call the public predicate extensions directly over `LamuFlixDbContext.Movies`, because `BrowseAsync` arrives in T010. No `InternalsVisibleTo` is needed.

## Phase 3: Sorting (depends on T001 only)

- [ ] T009 Add a public `OrderByMovieSort(this IQueryable<MovieRecord>, MovieSort, SortDirection)` to `MovieRecordQueryExtensions.cs`, returning `IOrderedQueryable<MovieRecord>`. It is a whitelisted dispatch over the `MovieSort` member, one private helper per key, with nulls last, Title then Id tie-breakers, and no throw path because SmartEnum members are closed. Add `EfMovieCatalogSortingTests.cs`, which calls the extension directly over `LamuFlixDbContext.Movies`: every `MovieSort` × {Asc, Desc}, nulls last in both directions, ties (FR-006; Q8, Q9; D6)

## Phase 4: Browse (depends on T002–T009)

- [ ] T010 `EfMovieCatalog.BrowseAsync`: composition, `OrderByMovieSort(query.Sort!, query.Direction!)`, `CountAsync`, `Skip((Number-1)*Size)`/`Take`, direct `MovieSummary` projection, `AsNoTracking`, token forwarding. Add `EfMovieCatalogPagingTests.cs`: first, middle and last page; `TotalCount`; past-end; no overlap or gaps on ties; empty ChangeTracker; SELECT-list shape (only id and title, no JOIN) captured by a `DbCommandInterceptor`; pre-cancelled token → `OperationCanceledException` (FR-001, FR-007; Q10, Q11; D9, D10, D18, D20)
- [ ] T011 Add ≥ 2 multi-filter AND combination tests through `BrowseAsync` to `EfMovieCatalogBrowseTests.cs` (spec US1.9)

## Phase 5: Details (after T010; same source file)

- [ ] T012 `EfMovieCatalog.GetDetailsAsync`, forwarding the token to `FirstOrDefaultAsync(ct)`. Add `EfMovieCatalogDetailsTests.cs` with these cases:
  - full metadata;
  - no metadata: every non-title metadata source is null or empty → `Metadata == null`;
  - partial metadata: at least one non-title source set, `MetadataTitle` null → `Title` fallback;
  - missing Id → null;
  - a fresh context's ChangeTracker is empty afterwards;
  - pre-cancelled token → `OperationCanceledException`.

  (FR-008; Q2; D9, D13, D14, D19)

## Phase 6: Registration (depends on T012)

- [ ] T013 `MovieCatalogServiceCollectionExtensions.AddMovieCatalog` (Scoped), plus `MovieCatalogRegistrationTests.cs`. The test registers `LamuFlixDbContext` itself, because `AddMovieCatalog` provisions none. It then resolves `EfMovieCatalog` and checks for a distinct instance per scope (FR-009; Q3)

## Phase 7: Gates (last)

- [ ] T014 `./scripts/run-roslyn-analyzers.ps1` exit 0
- [ ] T015 `./scripts/run-cyclomatic-complexity.ps1` (≤ 15), then refactor gate `-Threshold 6`
- [ ] T016 `./scripts/run-jetbrains-inspectcode.ps1` exit 0
- [ ] T017 `dotnet format --verify-no-changes` clean
- [ ] T018 Full `dotnet test` green; `MigrationTests` and `PersistenceRoundTripTests` unchanged (SC-002)
- [ ] T019 `./scripts/run-vulnerable-packages.ps1` exit 0 (SC-002; D16)
- [ ] T020 `LamuFlix.ArchitectureTests` green: Core still has no EF reference (SC-002; D16)

Property tests: opted out, and recorded in the task note (brief D16). No new Core type or domain invariant is added.

## Dependencies

T001 → T002–T008, T009 → T010 → T011; T010 → T012 → T013; T011, T013 → T014–T020.
