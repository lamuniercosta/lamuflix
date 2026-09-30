# DEV-302 — Brief (Keel)

Ticket: DEV-302 — Implement `EfMovieCatalog` with composable `IQueryable` predicates and NULLS LAST sorting. Size M, UI false, parent DEV-283.
Worktree: `F:\Dev\LamuFlix.worktrees\feature-302-spec` · branch `feature/302-spec` · base `5438176` (grill rulings `1e63df4`).
Sources: ticket text (quoted in `CONCLUSIONS.md` "Ticket description line basis"), `recon-DEV-302`, Patron rulings Q1–Q12 in `CONCLUSIONS.md`, taste rulings in `ASSUMPTIONS.md`.

If a decision is not in this file, it is not decided. Quill drafts `spec.md`, `plan.md`, `tasks.md` from this brief only.

---

## 1. Closing bar

DEV-302 is done when all of the following hold:

1. `EfMovieCatalog` in `src/LamuFlix.Infrastructure/Persistence/` implements both `IMovieCatalog` members (`BrowseAsync`, `GetDetailsAsync`) against `LamuFlixDbContext`.
2. The seven ticket-named predicates (`WhereText`, `WhereGenres`, `WhereActors`, `WhereRuntime`, `WhereYear`, `WhereStatuses`, `WhereInWatchlist`) exist as chainable `IQueryable` extensions, all translated to SQL.
3. Sorting on every `MovieSort` key and both directions puts null keys last, and ties are broken the same way every time.
4. Browse reads with `AsNoTracking` and selects straight into `MovieSummary`. It uses no `Include` and does not build `MovieRecord` entities.
5. Integration tests against Testcontainers Postgres (existing `PostgresFixture`) prove correct filtering, pagination and nulls-last (ticket acceptance L22–L23), plus the extra coverage in §5.
6. Scoped DI registration `IMovieCatalog -> EfMovieCatalog` via `AddMovieCatalog`.
7. All gates in §6 pass; full `dotnet test` green; `dotnet format --verify-no-changes` clean.

## 2. Frozen scope

**In scope**
- `EfMovieCatalog` (Browse + Details).
- Predicate extensions (ticket L5–L13) plus the sort mapping (ticket L14–L15).
- `AddMovieCatalog` service-collection extension (Q3).
- Integration tests (Q12).

**Out of scope (do not touch)**
- `MovieSummary` stays `(MovieId Id, string Title)`. No widening (Q1). `MovieDetails` / `MovieMetadata` are not expanded (Q2).
- Any schema, migration, or `MovieConfiguration` change. Recon §4.3: the existing columns and indexes are enough.
- Host wiring (`Program.cs` in Api/Worker), connection configuration, DbContext provisioning (Q3).
- Legacy `IMovieService` / `MoviesController` / `MovieServiceExtensions`. API routes, OpenAPI, `/web`.
- `Features:LocalPlay`, secrets, `Process.Start`. None are touched (recon §4.5).
- New NuGet/npm packages (recon §4.1: EF Core 10.0.12, Npgsql 10.0.3 and Testcontainers.PostgreSql 4.15.0 are already present).
- New projects, folders or layers. No repository, specification-pattern or "service" wrapper.
- Mocked `IQueryable`, EF InMemory, new test frameworks (Q12).

**Ticket-text interpretation (recorded, not escalated):** ticket L6 says `IQueryable<Movie>`. Per Q11 and `specs/DEV-19/CONCLUSIONS.md` Q1, the persisted Movie is `MovieRecord`, so the extensions extend `IQueryable<MovieRecord>`. The names and parameter types stay exactly as ticket L7–L13 gives them.

## 3. Round cap

- Grill: 1 round, 12/12 questions used. Closed.
- Spec review (Keel ↔ Quill): **2 rounds, hard cap** (Q12). If issues remain after round 2, they go to the Conductor as `blocked:`.
- Plan challenge: one adjudication pass over the Challenger findings.

## 4. Grill answers (Patron, Q1–Q12; full text and citations in `CONCLUSIONS.md`)

| # | Ruling |
|---|---|
| Q1 | Keep `MovieSummary(Id, Title)`, projecting the stored file `Title`. No DTO widening. |
| Q2 | Implement the full existing `MovieDetails` contract. Return null only when the Id is missing. Map Id/Title/Path/Format and every existing `MovieMetadata` member from its persisted scalar column. When no metadata can be built, `Metadata = null`; otherwise a missing `MetadataTitle` falls back to `Title`. No stubs and no invented fields. |
| Q3 | Scoped `IMovieCatalog -> EfMovieCatalog` through a small `AddMovieCatalog` extension in `Infrastructure/Persistence`. Do not mix it into the Pipeline `AddHandler`. No host rewiring. |
| Q4 | [assumed] Search `Title` only, as a case-insensitive literal substring via `EF.Functions.ILike(title, pattern, "\\")`. Escape `\` first, then `%` and `_`, then wrap in `%…%`. Null or whitespace-only text means no filter. Other text is not trimmed. `MetadataTitle` and actor names are not searched. |
| Q5 | [assumed] Match ANY id within `GenreIds` and ANY within `ActorIds`, and AND across all active filters. Default or empty arrays mean no filter. Duplicate ids must not duplicate rows, so use `Any(...)` navigation predicates, not joins. |
| Q6 | Bounds are inclusive and each is optional. A null range means no filter. When a Runtime range is present, null runtimes are included exactly when `IncludeUnknown = true`, and known runtimes must satisfy every bound given. When a Year range is present, null years are excluded. A present range with both bounds null keeps these same rules and is not collapsed. |
| Q7 | Default or empty `Statuses` means no filter; otherwise match ANY. `InWatchlist`: null means no filter, true means watchlisted only, false means not watchlisted only. |
| Q8 | [assumed] Sort keys map Title→`Title`, Year→`ReleaseYear`, Rating→`ImdbRating`, Runtime→`RuntimeMinutes`. Nullable keys sort first on `key == null` ascending, then on the value in the requested Direction, so nulls are last in BOTH directions. |
| Q9 | Order must be unique: a nullable primary key, then `Title` ASC, then `Id` ASC; a Title primary key, then `Id` ASC. [assumed] Tie-breakers stay ascending whatever the primary Direction is. The order does not promise stability under concurrent writes. |
| Q10 | Use the validated `MovieQuery.Page.Number/Size` with the existing numbering convention. No new defaults, no hardcoded size. Run `CountAsync` over the full filtered query first, then fetch the page. Both run one after the other on the same context and forward the `CancellationToken`. A page past the end returns empty `Items` with the correct `TotalCount`. **Offset formula is pending recon (see §8).** |
| Q11 | Compose translated predicates, then the whitelisted sort, then paging, with `AsNoTracking`, then `Select(r => new MovieSummary(r.Id, r.Title))`. No `Include` and no entity materialisation. Tests assert that the ChangeTracker stays empty AND that the browse SELECT reads only Id/Title, captured through EF command logging or interception. |
| Q12 | Tests go in `tests/LamuFlix.IntegrationTests` on the existing `PostgresFixture` (full matrix in §5). No mandatory unit tests that duplicate the translated predicates. Spec review is capped at 2 rounds. |

## 5. Plan decisions

### 5.1 Approach
- **Predicates:** one `internal static` class of `IQueryable<MovieRecord>` extension methods, one method per ticket-named predicate. Each method returns the source unchanged when its filter is inactive, so `BrowseAsync` is a straight chain with no branching.
- **Sorting:** a private `switch` over `MovieSort` (the whitelist) that returns an `IOrderedQueryable<MovieRecord>`. Nullable keys use `OrderBy(r => r.Key == null)`, then `ThenBy` or `ThenByDescending(r => r.Key)` per Direction, then `ThenBy(Title)` and `ThenBy(Id)`. An undefined `MovieSort` value throws `ArgumentOutOfRangeException` and never falls back silently.
- **Browse:** `Movies.AsNoTracking()` → predicates → `CountAsync` → sort → `Skip`/`Take` → `Select` to `MovieSummary` → `ToArrayAsync` → `PagedResult<MovieSummary>`.
- **Details:** `AsNoTracking` `Select` of the scalar columns into a private flat shape (or straight into `MovieDetails`, if the `MovieMetadata` factory can be translated), then map `Metadata` per Q2 in memory. The Details query also must not materialise the entity.
- **Constructor:** primary constructor taking `LamuFlixDbContext`. The class is `sealed`.
- The `ReleaseYearConverter` and `MovieId` converters already apply, so compare using the Core value types, as `MovieRecord` exposes them.

### 5.2 Files touched (all new, none deleted or rewritten)
- `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`
- `src/LamuFlix.Infrastructure/Persistence/MovieRecordQueryExtensions.cs` (the seven `Where*` extensions)
- `src/LamuFlix.Infrastructure/Persistence/MovieCatalogServiceCollectionExtensions.cs` (`AddMovieCatalog`)
- `tests/LamuFlix.IntegrationTests/Persistence/EfMovieCatalogBrowseTests.cs` (filters, combinations)
- `tests/LamuFlix.IntegrationTests/Persistence/EfMovieCatalogSortingTests.cs` (sort / direction / nulls-last / ties)
- `tests/LamuFlix.IntegrationTests/Persistence/EfMovieCatalogPagingTests.cs` (count, pages, past-end, projection/tracking/SQL shape)
- `tests/LamuFlix.IntegrationTests/Persistence/EfMovieCatalogDetailsTests.cs` (Q2 cases)
- `tests/LamuFlix.IntegrationTests/Persistence/MovieCatalogRegistrationTests.cs` (scoped resolution)
- A seed-data builder under `tests/LamuFlix.IntegrationTests/Persistence/`, only if one is not already in `Tests.Common` (Quill confirms against recon §3; reuse `LamuFlixDbContextFactory`).

If the tests project has no `Persistence/` subfolder, keep the flat layout used by `MigrationTests.cs` and `PersistenceRoundTripTests.cs` rather than creating a new folder. Quill picks whichever matches the existing layout and states the choice in `plan.md`.

### 5.3 Test strategy (Q12 matrix)
All tests run against real Postgres (`PostgresFixture`, `postgres:16.4`) in a clean, seeded database for each test class. Each test class seeds its own data set.
- **Text:** case-insensitive match; substring in the middle; literal `%`, `_` and `\` in the query match only titles that contain them; null, empty and whitespace mean no filter; `MetadataTitle` is not searched.
- **Genres / Actors:** a single id; multiple ids use ANY; a movie matching two ids appears once; empty means no filter.
- **Runtime:** min only, max only, both (inclusive edges), `IncludeUnknown` true and false, both bounds null with `IncludeUnknown` true and false.
- **Year:** min only, max only, both inclusive, null years excluded, present with both bounds null.
- **Statuses:** a single status, several (ANY), empty means no filter.
- **Watchlist:** null, true, false.
- **Combinations:** at least two representative multi-filter ANDs (e.g. text + genre + year; actor + runtime + watchlist + status).
- **Sorting:** every `MovieSort` × {Asc, Desc}; for Year, Rating and Runtime, nulls last in both directions; ties resolved by Title then Id (seed equal keys and equal titles).
- **Pagination:** first, middle and last page; `TotalCount` equals the filtered count on every page; a page past the end returns empty items with the full count; pages have no overlap or gaps across a tied data set.
- **Projection:** after Browse, a fresh context's `ChangeTracker.Entries()` is empty; the captured SQL for the page query selects only the id and title columns and has no JOIN to the navigation tables (EXISTS subqueries for predicates are allowed).
- **Details:** found with full metadata, found without metadata (`Metadata == null`), partial metadata (MetadataTitle falls back to Title), missing Id returns null.
- **Registration:** `AddMovieCatalog` resolves `IMovieCatalog` as `EfMovieCatalog`, with a distinct instance per scope.
- Unit tests are not required. A test to prove an undefined `MovieSort` throws is allowed where cheap.

### 5.4 Gate expectations
- Roslyn analyzers, cyclomatic complexity ≤ 15, and JetBrains InspectCode on every changed `.cs` file must exit 0. Refactor gate: complexity ≤ 6. The sort `switch` is the likely hotspot; split it into one helper per key rather than suppressing.
- No suppressions without a cited ruling. `dotnet format --verify-no-changes` must be clean.
- Full `dotnet test` must be green, including the existing `MigrationTests` and `PersistenceRoundTripTests`, which must not change.
- Mutation (architect stage): predicates and sort mapping are the kill targets. Every predicate's inactive branch and every bound comparison (`<=` vs `<`) must be caught by a test.
- Baseline: recon §5 reports analyzers SKIPPED (no changed `.cs`). There is no pre-existing debt to carry.

### 5.5 Task ordering constraints
1. Seed builder / test-data helper (if one is needed) comes first.
2. Predicate extensions, each with its integration tests. Order is free, but each predicate is a separate task with its own test.
3. Sort mapping + tie-breakers + nulls-last tests. This depends on the seed data only.
4. `EfMovieCatalog.BrowseAsync` (composition, count, paging, projection) + paging and projection tests. Depends on 2 and 3.
5. `EfMovieCatalog.GetDetailsAsync` + details tests. Independent of 2–4 and can run in parallel with them.
6. `AddMovieCatalog` + registration test. Depends on 4 and 5 (the class must compile completely).
7. Gates (analyzers, complexity 15, then 6, InspectCode, format, full suite) are last.

## 6. Constraints carried from rules
- Commit messages and the PR title follow `DEV-302 - {subject}`.
- Contract chain untouched: no DTO, OpenAPI or TS changes.
- Work only in this worktree. The main checkout stays clean.

## 7. Traceability
Ticket acceptance L22 (filtering and pagination) → §5.3 filters/combinations/pagination. Ticket acceptance L23 (null ratings and years last) → §5.3 sorting. Ticket L17 (untracked direct projection) → §5.3 projection.

## 8. Open item (pending recon, blocks only the offset formula)
Q10: which page numbering does `Page` / `MovieQueryValidator` enforce (1-based or 0-based `Number`, and `Size` bounds)? Requested from the Conductor as `needs recon`. Until the answer is in, `plan.md` states the offset as "`Skip((Number − base) * Size)` with base per the validator citation". Keel fills in the exact formula here when recon returns.
