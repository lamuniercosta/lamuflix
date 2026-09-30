# Implementation Plan: EfMovieCatalog with Composable Predicates and Nulls-Last Sorting

**Branch**: `feature/302-spec` | **Date**: 2026-09-30 | **Spec**: `specs/DEV-302/spec.md`
**Input**: `brief.md` §5 (plan decisions), §7b (plan-challenge rulings D6–D24), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `recon-DEV-302`

## Summary

Add a persistence adapter implementing `IMovieCatalog` over `LamuFlixDbContext`: seven `IQueryable<MovieRecord>` predicate extensions, a public whitelisted nulls-last sort extension with unique tie-breakers, and an untracked direct `MovieSummary` projection. Register it scoped through `AddMovieCatalog`. Prove it with Testcontainers Postgres integration tests.

## Technical Context

**Language/Version**: C# / .NET 10
**Dependencies (all already present)**: EF Core 10.0.12, Npgsql EF provider 10.0.3, Testcontainers.PostgreSql 4.15.0. No new package.
**Storage**: existing PostgreSQL schema; existing columns and indexes suffice (recon §4.3). No migration.
**Testing**: xUnit v3, Shouldly, `PostgresFixture` (`postgres:16.4`), `LamuFlixDbContextFactory`. No required unit tests; no InMemory, no mocked `IQueryable`.
**Constraints**: paging is 1-based, offset `(Number - 1) * Size`, trust the validated query (brief §8).

## Constitution Check

Gates checked against `.specify/memory/constitution.md`; all pass, no deviation.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Core has no EF references; adapter lives in Infrastructure | `EfMovieCatalog`, predicates and registration are new files in `Infrastructure/Persistence`; `IMovieCatalog` and the Core types are unchanged. |
| III (Typed query model) | Typed `MovieQuery`; whitelisted sort; NULLS LAST; predicates as extensions; untracked projection | `MovieQuery` is the only input; a closed dispatch over `MovieSort` is the whitelist; nullable keys sort `key == null` first; seven `IQueryable<MovieRecord>` extensions; `AsNoTracking` plus direct `MovieSummary` projection. |
| IX (Test pyramid, real infrastructure) | xUnit v3, Shouldly, Testcontainers; no InMemory; no mocked infrastructure; fresh data per test; AutoFixture/Faker (exception below) | Integration tests on `PostgresFixture` (Postgres 16.4); no InMemory, no mocked `IQueryable`; each test seeds its own rows; test data from a small hand-written deterministic seed factory, because these tests need exact bound, tie, null and literal (`%`, `_`, `\`) values that generated data cannot guarantee (brief D3; AutoFixture/Faker.Net are referenced only by `LamuFlix.UnitTests`, so none is added here). |
| Tech Stack | Sealed classes, no comments, Enumerations, `CancellationToken` end to end | `EfMovieCatalog` is `sealed`; no code comments; `MovieSort`/`SortDirection` stay SmartEnums; the token reaches `CountAsync`, `ToArrayAsync` and the Details query; sort dispatch lives in Infrastructure rather than on the SmartEnum member because Core has no EF (Principle I; brief D5). |
| Gates | Roslyn analyzers, cyclomatic complexity, InspectCode | Phase 7 tasks T014-T016 (plus format and full suite). |

No new dependency, project, folder, layer, schema change, public API shape, `LocalPlay`/secret/`Process.Start` touch, or deletion of an unnamed file. No §2.3 item is triggered and no structural question is outstanding. Contract chain untouched.

## Approach (brief §5.1)

- **Predicates**: one `public static` class `MovieRecordQueryExtensions` (brief D1: tests call the predicates directly before `BrowseAsync` exists, and public visibility avoids an `InternalsVisibleTo` edit to a csproj the ticket does not name), one method per ticket-named predicate, each returning the source unchanged when inactive, so `BrowseAsync` is a straight chain with no branching. Inactive means: `string.IsNullOrWhiteSpace(text)`; `ids.IsDefaultOrEmpty` for the three array predicates, so a `default` array is no filter (D7); `range is null` for Runtime and Year, where a present range with both bounds null stays active (D8); `inWatchlist is null`. `WhereRuntime` and `WhereYear` each split unknown-value handling from bound application into private helpers so every method is ≤ 6 (D12).
- **Sorting**: `MovieSort` and `SortDirection` are SmartEnums (`src/LamuFlix.Core/Library/MovieSort.cs:5`, `src/LamuFlix.Core/Library/SortDirection.cs:5`) with members `Title`, `Year`, `Rating`, `Runtime` and `Ascending`, `Descending`. They carry no behaviour, and Core must not reference EF (Principle I), so one Infrastructure dispatch over the closed member set is the whitelist (brief D5). It is a public extension `OrderByMovieSort(this IQueryable<MovieRecord>, MovieSort, SortDirection)` in `MovieRecordQueryExtensions` returning `IOrderedQueryable<MovieRecord>`, public for the same reason as D1, so T009 can test it before `BrowseAsync` exists (D6). Nullable keys: `OrderBy(r => r.Key == null)` → `ThenBy`/`ThenByDescending(r => r.Key)` per Direction → `ThenBy(Title)` → `ThenBy(Id)`. Title: `OrderBy`/`OrderByDescending(Title)` → `ThenBy(Id)`. An unknown member cannot exist, so there is no throw path. One helper per key to stay under the complexity limits.
- **Browse**: `Movies.AsNoTracking()` → predicates → `CountAsync(ct)` → `OrderByMovieSort(query.Sort!, query.Direction!)` (trusted-query posture: `MovieQueryValidator.cs:12-13` guarantees NotNull; no fallback, D10) → `Skip((Number-1)*Size).Take(Size)` → `Select(r => new MovieSummary(<persisted id>, r.Title))`. `MovieRecord.Id` is `MovieId?` but is always set on persisted rows, so the projection and the Id tie-breaker use the non-null persisted id. The exact null-forgiving form is an implementation detail (D18) → `ToArrayAsync(ct)` → `PagedResult<MovieSummary>`.
- **Details**: `AsNoTracking` `Select` of the scalar columns into a private flat shape, then map it to `MovieDetails`/`MovieMetadata` in memory, with `Metadata` per Q2 as made concrete by D14: null exactly when `MetadataTitle`, `Plot`, `ReleaseYear`, `RuntimeMinutes`, `ImdbRating` and `ImdbId` are all null. Otherwise it is built with `Title = MetadataTitle ?? Title` (a `MetadataTitle`-only row yields title-only `MovieMetadata`); persistence-only fields do not create or expand the DTO. The token is forwarded to `FirstOrDefaultAsync(ct)` (D9).
- **Class shape**: `sealed`, primary constructor taking `LamuFlixDbContext`, checked at review rather than by a test (D17).
- **DI**: `MovieCatalogServiceCollectionExtensions.AddMovieCatalog(this IServiceCollection)` → `AddScoped<IMovieCatalog, EfMovieCatalog>()`; not in Pipeline `AddHandler`; no host change.

## Files Touched (all new; none deleted or rewritten)

```text
src/LamuFlix.Infrastructure/Persistence/
  EfMovieCatalog.cs
  MovieRecordQueryExtensions.cs
  MovieCatalogServiceCollectionExtensions.cs
tests/LamuFlix.IntegrationTests/
  MovieCatalogSeed.cs                 # seed helper; none exists in Tests.Common (recon §3)
  EfMovieCatalogBrowseTests.cs
  EfMovieCatalogSortingTests.cs
  EfMovieCatalogPagingTests.cs
  EfMovieCatalogDetailsTests.cs
  MovieCatalogRegistrationTests.cs
```

**Layout choice (brief §5.2, superseded per D22)**: `tests/LamuFlix.IntegrationTests` has no `Persistence/` subfolder (`MigrationTests.cs` and `PersistenceRoundTripTests.cs` are flat), so the test files stay flat in that project rather than creating `Persistence/`. The seed helper lives in the integration-test project and reuses `LamuFlixDbContextFactory`; `Tests.Common` is not touched. It is a small hand-written deterministic factory in the style of `PersistenceRoundTripTests.cs:186-224` (`BaseMovie`, `FullyPopulatedMovie`, `TitleOnlyMovie`), because a `MovieRecord` is an EF graph (genres, actors) and the tests need exact bound, tie, null and literal values (brief D3). No package reference is added and no csproj is edited. Each test seeds its own data. Tests use `Theory` + `MemberData` rather than repeated `Fact`s.

**Isolation (D11)**: a test only ever asserts on rows it seeded. Each test clears the catalog tables (`movies` and its join and lookup tables) in its own arrange step before seeding. Test classes that share one `PostgresFixture` database are in one xUnit collection, so they run serially. Different collections never share a database.

**Conventions (D23)**: `Method_Condition_Expected` names; `TestContext.Current.CancellationToken`; only arrange, act and assert comments; fixed `DateTimeOffset` literals in seeds (`DateTime.Now`/`UtcNow` are banned, RS0030).

## Test Matrix (brief §5.3)

All on real Postgres; each test seeds its own data (brief D2).

| Class | Cases |
|---|---|
| Browse | **Text**: case-insensitive; middle substring; literal `%`, `_`, `\`; combined `100%\file_name_v2` and `\%` adjacency (D15); no trim (`"Movie "` matches `Movie Night`, not `Movie`); null/empty/whitespace = no filter; `MetadataTitle` not searched. **Genres/Actors**: single; multiple = ANY; a movie matching two ids appears once; same id duplicated in input appears once; empty = no filter; `default` array = no filter (D7). **Inactive branch**: every predicate has an explicit case where it is inactive and returns the source unchanged. For Runtime and Year the inactive case (`null` range) is separate from the both-null-bounds case (D8). **Bounds (brief D4)**: for every inclusive bound, value = bound passes and value = bound ± 1 fails, both sides. **Runtime**: min only, max only, both inclusive edges, `IncludeUnknown` true/false, both bounds null × true/false. **Year**: min, max, both inclusive, null excluded, present with both bounds null. **Statuses**: single, several, empty, `default`. **Watchlist**: null/true/false. **Combinations**: ≥ 2 (text+genre+year; actor+runtime+watchlist+status). |
| Sorting (calls `OrderByMovieSort` directly) | every `MovieSort` × {Asc, Desc}; nulls last in both directions for Year, Rating, Runtime; ties by Title then Id (equal keys and equal titles seeded). |
| Paging | first/middle/last page; `TotalCount` = filtered count on every page; past-end empty with full count; no overlap/gaps on a tied set; empty `ChangeTracker` on a fresh context; the SELECT list of the captured page query holds only id and title and there is no JOIN. EXISTS subqueries are allowed, and ORDER BY and WHERE may reference other columns. It is captured by a `DbCommandInterceptor` (D18, D20). A pre-cancelled token throws `OperationCanceledException` (D9). |
| Details | full metadata; no metadata, meaning all six sources are null (`Metadata == null`); title-only, with only `MetadataTitle` set (title-only `MovieMetadata`); partial, with one non-title source set and `MetadataTitle` null (falls back to Title) (D14); missing Id → null; a fresh context's ChangeTracker is empty afterwards (D19); a pre-cancelled token throws `OperationCanceledException` (D9). |
| Registration | the test registers `LamuFlixDbContext` itself (`AddMovieCatalog` provisions no DbContext, Q3), then `AddMovieCatalog` resolves `EfMovieCatalog`; distinct instance per scope. |

"First page" is `Number = 1`; the past-end case uses `Number > ceil(TotalCount / Size)`.

## Gates (brief §5.4)

Roslyn analyzers, complexity ≤ 15, JetBrains InspectCode on every changed `.cs` (exit 0); refactor gate complexity ≤ 6 (split the dispatch over the `MovieSort` member per key rather than suppress); no suppression without a cited ruling; `dotnet format --verify-no-changes` clean; full `dotnet test` green with `MigrationTests` and `PersistenceRoundTripTests` unchanged; `./scripts/run-vulnerable-packages.ps1` exit 0; `LamuFlix.ArchitectureTests` green (D16). Property tests are opted out, and the opt-out is recorded in the task note: no new Core type or domain invariant (D16). Stryker does not mutate Infrastructure (constitution IX table), so predicate and bound coverage is held by the explicit Test Matrix cases (brief D4), not a mutation score. Baseline: analyzers SKIPPED at recon (no changed `.cs`); no pre-existing debt.

## Risks

- Sort dispatch complexity: one helper per key.
- Nulls-last relies on `key == null` ordering translating to `ORDER BY (col IS NULL)`; proven by the Sorting tests.
- Converter-typed columns (`MovieId`, `ReleaseYear`, `Runtime`, `ImdbRating`): compare using Core value types as `MovieRecord` exposes them; proven by integration tests.
- The SQL-shape assertion uses a `DbCommandInterceptor`, scoped to the SELECT list (D18, D20).
- `ILIKE '%…%'` cannot use the btree title index, so text browse is a sequential scan. That is accepted at personal-library scale. A pg_trgm index would be a schema change, followed up only if slowness is reported (D21).
- ILIKE case folding follows the database collation. Tests use ASCII titles (D21).
- COUNT and page are two statements, so `TotalCount` and `Items` are not snapshot-consistent under concurrent writes (Q9, D10).
- `Page.Size` is bounded only by `MovieQueryValidator`. Every `IMovieCatalog` consumer must validate first (trusted-query posture, D10).

## Ordering

See `tasks.md` (brief §5.5).
