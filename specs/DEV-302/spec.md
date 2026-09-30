# Feature Specification: EfMovieCatalog with Composable Predicates and Nulls-Last Sorting

**Feature Branch**: `feature/302-spec`
**Created**: 2026-09-30
**Status**: Draft
**Input**: Ticket DEV-302 (parent DEV-283, Size M, UI false); `brief.md`, `CONCLUSIONS.md` (Q1–Q12), `ASSUMPTIONS.md`, `recon-DEV-302`; plan-challenge rulings brief §7b (D6–D24)

## Scope

**In scope**: `EfMovieCatalog` (Browse + Details); seven `IQueryable<MovieRecord>` predicate extensions; the public `OrderByMovieSort` extension with nulls-last and unique ordering; `AddMovieCatalog`; integration tests on the existing `PostgresFixture`.

**Out of scope**: `MovieSummary` widening (Q1); `MovieDetails`/`MovieMetadata` expansion (Q2); schema, migration or `MovieConfiguration` changes; host wiring (`Program.cs`), connection config; legacy `IMovieService`/`MoviesController`/`MovieServiceExtensions`; API routes, OpenAPI, `/web`; `Features:LocalPlay`, secrets, `Process.Start`; new packages, projects, folders or layers; mocked `IQueryable`, EF InMemory, new test frameworks.

**Ticket interpretation (recorded, not escalated)**: ticket L6 says `IQueryable<Movie>`; per Q11 and `specs/DEV-19/CONCLUSIONS.md` Q1 the persisted type is `MovieRecord`, so the extensions extend `IQueryable<MovieRecord>`. Names and parameter types stay as ticket L7–L13.

## User Scenarios & Testing

### User Story 1 - Filtered Browse (Priority: P1)

As a library handler, I need `BrowseAsync` to filter the catalog by text, genres, actors, runtime, year, statuses and watchlist so that a `MovieQuery` returns only matching movies.

**Independent Test**: seed a known data set in Testcontainers Postgres, run `BrowseAsync` per filter and per combination, assert returned ids.

**Acceptance Scenarios** (ticket L22):

1. **Given** titles differing only in case **When** Text is searched **Then** all case-insensitive substring matches are returned (Q4).
2. **Given** text containing `%`, `_` or `\`, alone or combined (e.g. `100%\file_name_v2`, `\%`) **When** searched **Then** only titles containing those literal characters match (D15).
3. **Given** null, empty or whitespace-only Text **When** browsed **Then** no text filter applies; nonblank text is not trimmed (`"Movie "` matches `Movie Night` but not `Movie`); `MetadataTitle` and actor names are not searched.
4. **Given** several GenreIds (or ActorIds) **When** browsed **Then** a movie matching ANY id is returned, once, even when it matches more than one id or an id is duplicated; empty or `default` means no filter (Q5, D7).
5. **Given** a Runtime range **When** browsed **Then** bounds are inclusive and optional; null runtimes are included exactly when `IncludeUnknown = true`; a present range with both bounds null keeps the same rules (Q6).
6. **Given** a Year range **When** browsed **Then** bounds are inclusive and optional; null years are excluded (Q6).
7. **Given** Statuses **When** browsed **Then** ANY status matches; empty or `default` means no filter (Q7, D7).
8. **Given** InWatchlist null/true/false **When** browsed **Then** no filter / watchlisted only / not-watchlisted only (Q7).
9. **Given** several active filters **When** browsed **Then** they combine with AND (Q5).

### User Story 2 - Nulls-Last Deterministic Sorting (Priority: P1)

As a browsing user, I need every `MovieSort` key in both directions to place unknown values last and to order ties identically every time.

**Acceptance Scenarios** (ticket L23):

1. **Given** movies with null Year, Rating or Runtime **When** sorted by that key Ascending or Descending **Then** null-key movies come last in both directions (Q8).
2. **Given** Title, Year, Rating, Runtime **When** sorted **Then** keys map to `Title`, `ReleaseYear`, `ImdbRating`, `RuntimeMinutes`.
3. **Given** equal primary keys **When** sorted **Then** ties break by Title ascending then Id ascending; for Title primary, by Id ascending; tie-breakers stay ascending regardless of Direction (Q9).
4. **Given** `MovieSort` and `SortDirection` are closed SmartEnums **When** sorting **Then** dispatch covers every member, so there is no undefined-value path. `MovieQueryValidator` excludes a null `Sort` or `Direction` (trusted-query posture, D10). `EfMovieCatalog` has no fallback.

### User Story 3 - Correct Pagination (Priority: P1)

**Acceptance Scenarios**:

1. **Given** a validated `Page` **When** browsed **Then** offset is `(Number - 1) * Size`, 1-based, with no re-validation, clamping or new defaults (Q10, brief §8).
2. **Given** any page **When** browsed **Then** `TotalCount` equals the full filtered count.
3. **Given** a page past the end **When** browsed **Then** `Items` is empty and `TotalCount` is correct.
4. **Given** a tied data set **When** paged through **Then** pages neither overlap nor leave gaps.

### User Story 4 - Untracked Direct Projection (Priority: P1)

**Acceptance Scenarios** (ticket L17):

1. **Given** a Browse call **When** it completes **Then** a fresh context's `ChangeTracker.Entries()` is empty.
2. **Given** the page query captured by a `DbCommandInterceptor` **When** its SELECT list is inspected **Then** it projects only the id and title columns, and the query has no JOIN to navigation tables. EXISTS subqueries are allowed, and ORDER BY and WHERE may reference sort and filter columns. There is no `Include` and no `MovieRecord` materialisation (Q11, D18, D20).
3. **Given** a pre-cancelled token **When** browsed **Then** `OperationCanceledException` is thrown (D9).

### User Story 5 - Movie Details (Priority: P2)

**Acceptance Scenarios** (Q2):

1. **Given** an existing Id with full metadata **Then** `MovieDetails` maps Id/Title/Path/Format and every `MovieMetadata` member from its persisted column.
2. **Given** an existing Id where every persisted source of a non-title `MovieMetadata` member is null or empty **Then** `Metadata` is null (D14).
3. **Given** at least one non-title metadata source set and no `MetadataTitle` **Then** `Metadata` is built and `Metadata.Title` falls back to `Title` (D14).
4. **Given** a missing Id **Then** null is returned.
5. The Details query does not materialise the entity; a fresh context's ChangeTracker is empty afterwards (D19).
6. **Given** a pre-cancelled token **Then** `OperationCanceledException` is thrown; the token reaches `FirstOrDefaultAsync` (D9).

### User Story 6 - Scoped DI Registration (Priority: P2)

1. **Given** `AddMovieCatalog` **When** `IMovieCatalog` is resolved **Then** it is an `EfMovieCatalog`, with a distinct instance per scope (Q3). It is not mixed into Pipeline `AddHandler`; no host rewiring.

### Edge Cases

- A present Runtime or Year range with both bounds null is not collapsed to "no filter".
- `MovieQuery` is trusted as validated; `EfMovieCatalog` does not re-validate.
- Order is not promised stable under concurrent writes (Q9). COUNT and page are separate statements, so `TotalCount` can disagree with `Items` under concurrent writes (D10).
- A present range is active even with both bounds null. A range predicate is inactive only when its range is `null` (D8).
- A `default(ImmutableArray<T>)` filter is inactive, like an empty one (D7).
- Only `MovieQueryValidator` excludes a null `Sort` or `Direction` and an unbounded `Page.Size`. Every `IMovieCatalog` consumer must validate first (trusted-query posture, D10).
- ILIKE case folding follows the database collation. Non-ASCII folding is not asserted (D21).

## Requirements

### Functional Requirements

- **FR-001**: `EfMovieCatalog` (sealed, primary constructor over `LamuFlixDbContext`) implements `IMovieCatalog.BrowseAsync` and `GetDetailsAsync`.
- **FR-002**: Seven chainable `IQueryable<MovieRecord>` extensions — `WhereText`, `WhereGenres`, `WhereActors`, `WhereRuntime`, `WhereYear`, `WhereStatuses`, `WhereInWatchlist` — translate to SQL; each returns the source unchanged when inactive.
- **FR-003**: `WhereText` uses `EF.Functions.ILike(title, pattern, "\\")`; escape `\` first, then `%` and `_`, then wrap in `%…%` (Q4).
- **FR-004**: Genre/actor predicates use `Any(...)` navigation predicates, never joins (Q5).
- **FR-005**: Runtime, Year, Statuses and Watchlist rules exactly as Q6/Q7.
- **FR-006**: Sort via the public extension `OrderByMovieSort(this IQueryable<MovieRecord>, MovieSort, SortDirection)` in `MovieRecordQueryExtensions`, a whitelisted dispatch over the `MovieSort` member returning `IOrderedQueryable<MovieRecord>`: nullable keys `OrderBy(key == null)` then `ThenBy`/`ThenByDescending(key)`, then `ThenBy(Title)`, `ThenBy(Id)`; Title primary then `ThenBy(Id)` (Q8, Q9, D6).
- **FR-007**: Browse chain: `AsNoTracking` → predicates → `CountAsync` → sort → `Skip`/`Take` → `Select(r => new MovieSummary(r.Id, r.Title))` (the persisted `MovieRecord.Id` is typed `MovieId?` and is non-null for stored rows, D18) → `ToArrayAsync` → `PagedResult<MovieSummary>`; count then page run sequentially on the same context, forwarding the `CancellationToken` (Q10, Q11).
- **FR-008**: `GetDetailsAsync` implements Q2 as made concrete by D14, `AsNoTracking`, no entity materialisation, token forwarded to `FirstOrDefaultAsync` (D9).
- **FR-009**: `AddMovieCatalog` registers `IMovieCatalog -> EfMovieCatalog` as Scoped in `Infrastructure/Persistence` (Q3).
- **FR-010**: Integration tests in `tests/LamuFlix.IntegrationTests` on `PostgresFixture` (Q12), isolated per D11 and following the D23 conventions.
- **FR-011**: No schema/migration/DTO/OpenAPI/TS change; no new package.

### Key Entities

- **MovieRecord**: persisted movie; predicates and sort run over it. `MovieId` and `ReleaseYear` converters already apply; compare with Core value types.
- **MovieSummary(Id, Title)**, **MovieDetails**, **MovieMetadata**, **PagedResult<T>**, **MovieQuery/Page/RuntimeRange/YearRange**: existing Core types, unchanged.

## Success Criteria

- **SC-001**: Every test in the plan's Test Matrix passes against Postgres 16.4.
- **SC-002**: Roslyn analyzers, complexity ≤ 15 (refactor gate ≤ 6), InspectCode and `dotnet format --verify-no-changes` exit 0 on all changed `.cs`; full `dotnet test` green; `MigrationTests` and `PersistenceRoundTripTests` unchanged; `./scripts/run-vulnerable-packages.ps1` exit 0; `LamuFlix.ArchitectureTests` green (D16).
- **SC-003**: The test matrix holds an explicit case for every predicate's inactive branch and both sides of every inclusive bound (value = bound passes, value = bound ± 1 fails).

## Traceability

Ticket L22 → US1, US3; L23 → US2; L17 → US4. Q1–Q12 → FRs as cited above.
