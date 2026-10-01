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
| Q10 | Use the validated `MovieQuery.Page.Number/Size` with the existing numbering convention. No new defaults, no hardcoded size. Run `CountAsync` over the full filtered query first, then fetch the page. Both run one after the other on the same context and forward the `CancellationToken`. A page past the end returns empty `Items` with the correct `TotalCount`. Offset is `(Number - 1) * Size` (1-based, see §8). |
| Q11 | Compose translated predicates, then the whitelisted sort, then paging, with `AsNoTracking`, then `Select(r => new MovieSummary(r.Id, r.Title))`. No `Include` and no entity materialisation. Tests assert that the ChangeTracker stays empty AND that the browse SELECT reads only Id/Title, captured through EF command logging or interception. |
| Q12 | Tests go in `tests/LamuFlix.IntegrationTests` on the existing `PostgresFixture` (full matrix in §5). No mandatory unit tests that duplicate the translated predicates. Spec review is capped at 2 rounds. |

## 5. Plan decisions

### 5.1 Approach
- **Predicates:** one `internal static` class of `IQueryable<MovieRecord>` extension methods, one method per ticket-named predicate. Each method returns the source unchanged when its filter is inactive, so `BrowseAsync` is a straight chain with no branching.
- **Sorting (superseded in part by D5: dispatch over the SmartEnum member, no throw path):** a private `switch` over `MovieSort` (the whitelist) that returns an `IOrderedQueryable<MovieRecord>`. Nullable keys use `OrderBy(r => r.Key == null)`, then `ThenBy` or `ThenByDescending(r => r.Key)` per Direction, then `ThenBy(Title)` and `ThenBy(Id)`. An undefined `MovieSort` value throws `ArgumentOutOfRangeException` and never falls back silently.
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
- **Details:** found with full metadata, found without metadata (all six sources null, `Metadata == null`), title-only metadata (only `MetadataTitle` set, title-only `MovieMetadata`), partial metadata (MetadataTitle falls back to Title), missing Id returns null.
- **Registration:** `AddMovieCatalog` resolves `IMovieCatalog` as `EfMovieCatalog`, with a distinct instance per scope.
- Unit tests are not required. A test to prove an undefined `MovieSort` throws is allowed where cheap.

### 5.4 Gate expectations
- Roslyn analyzers, cyclomatic complexity ≤ 15, and JetBrains InspectCode on every changed `.cs` file must exit 0. Refactor gate: complexity ≤ 6. The sort dispatch (D5) is the likely hotspot; split it into one helper per key rather than suppressing.
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

## 7a. Decision changes after analyze round 1 (Keel, 2026-09-30)
- **D1 (replaces §5.1 "internal static"):** the predicate class is `public static MovieRecordQueryExtensions`. Tests in `LamuFlix.IntegrationTests` call the predicates directly before `BrowseAsync` exists (task ordering §5.5). Making the class public avoids an `InternalsVisibleTo` edit to a csproj the ticket does not name (§2.3 item 6). The ticket L6 says "chainable extensions" and names no visibility.
- **D2 (replaces §5.3 "per test class"):** constitution IX requires "fresh data per test". Each test seeds its own rows. A container or database per class is fine; data shared across tests is not.
- **D3 (revised after recon, replaces the §5.2 seed-builder bullet):** recon says AutoFixture 4.18.1 and Faker.Net 2.0.163 are pinned in `Directory.Packages.props:28-29`, but only `LamuFlix.UnitTests.csproj:22-23` references them. `LamuFlix.IntegrationTests.csproj` and `Tests.Common` do not. `AutoFixture.Xunit3` is not pinned at all. The existing integration tests use hand-written static factories (`PersistenceRoundTripTests.cs:186-224`: `BaseMovie`, `FullyPopulatedMovie`, `TitleOnlyMovie`). **Ruling:** the seed helper is a small hand-written deterministic factory in the style of `PersistenceRoundTripTests`. No package reference is added and no csproj is edited (§2.3 items 1 and 6 are not triggered).
  - Basis: constitution IX reserves hand-written builders for data the tools cannot satisfy. Every DEV-302 assertion depends on exact, controlled values: inclusive-bound edges (bound ± 1), exact ties on key and title for tie-break order, specific nulls, literal `%`/`_`/`\` titles, a specific `EnrichmentStatus`. Anonymous generated values cannot guarantee these, and random titles would make sort and tie assertions flaky.
  - The factory sets only the attributes a test needs and uses plain fixed defaults for the rest.
  - Use `Theory` + `MemberData` instead of repeated `Fact`s. Fresh data per test (D2) still applies.
- **D4 (replaces the §5.4 mutation bullet):** Stryker's scope is `LamuFlix.Core` via `LamuFlix.UnitTests` (constitution IX table). It does not mutate Infrastructure. Instead, the test matrix must hold an explicit case for every predicate's inactive branch and for both sides of every inclusive bound (value = bound passes, value = bound ± 1 fails). This is a spec requirement, not a mutation-score claim.
- **D5 (sorting dispatch):** constitution III and the Enumerations rule put member behaviour on the Enumeration member. But Core must not reference EF (Principle I), so `MovieRecord` sort expressions cannot live on `MovieSort`. A single Infrastructure dispatch over the closed `MovieSort` set is the whitelist Principle III permits. The plan must cite the actual declarations of `MovieSort` and `SortDirection` (SmartEnum or C# enum). If they are SmartEnums, drop the "undefined value throws" case, because an unknown member cannot exist, and use the member dispatch the type offers. If `MovieSort` is a C# enum, record it as a known constitution deviation for a follow-up, not DEV-302 scope, and keep the throw.

## 7b. Plan-challenge adjudication (Keel, 2026-09-30)
Inputs: `findings-DEV-302-risk` (Sentry S1–S12), `findings-DEV-302-standards` (Ledger L1–L8), `findings-DEV-302-spec` (Compass C1–C12). The full text is in the notes (and in the `%TEMP%` copies the challengers wrote). Every finding is ruled on below. None triggers §2.3 and none needs Patron or the user: no new package, file, schema, API shape, or LocalPlay change, and no ticket-text change.

| D | Findings | Ruling | Basis |
|---|---|---|---|
| D6 | C1 (HIGH) | **Accepted.** Sorting becomes a public extension `OrderByMovieSort(this IQueryable<MovieRecord>, MovieSort sort, SortDirection direction) -> IOrderedQueryable<MovieRecord>` in `MovieRecordQueryExtensions`, the same reasoning as D1. It replaces the "private dispatch in `EfMovieCatalog`". T009 tests it directly, before T010 exists. One private helper per key stays. | The task graph could not otherwise be satisfied. D1 precedent. No new file. |
| D7 | S1, L5 | **Accepted.** `WhereGenres`, `WhereActors` and `WhereStatuses` are inactive exactly when `ids.IsDefaultOrEmpty`. A default array means no filter. The matrix gets one `default(ImmutableArray<T>)` case per predicate. | The predicates are public (D1). `MovieQuery.cs:63-64` normalises only on the `MovieQuery` path. |
| D8 | S2 | **Accepted.** `WhereRuntime` and `WhereYear` are inactive exactly when `range is null`. A present range with both bounds null is active (Q6). The inactive-branch test and the both-null tests are separate cases. | Q6. D4. |
| D9 | S3 | **Accepted.** `GetDetailsAsync` forwards the token to its async terminator (`FirstOrDefaultAsync(ct)`). Details and Browse each get a pre-cancelled-token test that expects `OperationCanceledException`. | Constitution async rules (CancellationToken flows end to end). |
| D10 | S4, S8, S11, C2 | **Accepted as recorded posture, no code branch.** `BrowseAsync` reads `query.Sort!` and `query.Direction!`, relying on `MovieQueryValidator.cs:12-13` (NotNull). There is no fallback default, per Q10. US2.4 is reworded to "every SmartEnum member is dispatched; null Sort/Direction is excluded by the validator (trusted-query posture)". Spec Edge Cases add three items: (a) `TotalCount` reflects the filtered set when the count runs, so `Items` can shift under concurrent writes; (b) null Sort/Direction are covered by the trusted-query posture; (c) `Page.Size` is bounded only by the validator, so any `IMovieCatalog` consumer must run `MovieQueryValidator`. | Q9. Q10. Microsoft Learn pagination. |
| D11 | S5 | **Accepted.** Isolation rule: a test's assertions can only ever see rows that test seeded. Each test clears the catalog tables (`movies` and the join and lookup tables) in its own arrange step before seeding. Classes that share one `PostgresFixture` database must be in one xUnit collection, so they run serially. Different collections must not share a database. xUnit runs tests within one collection serially. | Constitution IX "fresh data per test". D2. |
| D12 | L1 | **Accepted.** `WhereRuntime` and `WhereYear` each split unknown-value handling from bound application into private helpers, so every method is ≤ 6 before T015. | Refactor gate ≤ 6. Constitution: fix complexity by extracting helpers. |
| D13 | L2 | **Accepted.** T012 loses `[P]` and is sequenced T010 → T012 → T013, because both edit `EfMovieCatalog.cs`. | Same rule as T002–T008. |
| D14 | C3 | **Accepted (operationalises Q2, does not change it); corrected at Gate 1 (CONCLUSIONS 786b469).** `Metadata` is null exactly when `MetadataTitle`, `Plot`, `ReleaseYear`, `RuntimeMinutes`, `ImdbRating` and `ImdbId` are all null. A row with only `MetadataTitle` set returns title-only `MovieMetadata`. Any other representable scalar set with `MetadataTitle` null returns `MovieMetadata` with `Title` falling back to the file `Title`. Unsupported persistence-only fields do not create or expand the DTO. T012's "no metadata" seed leaves all six null; its "title-only" seed sets only `MetadataTitle`; its "partial" seed sets at least one non-title scalar and leaves `MetadataTitle` null. | Q2. `MovieMetadata.cs:7-25` (title-only metadata is representable). |
| D15 | C4, C9, S9 | **Accepted: add these matrix cases.** No trim: `"Movie "` (trailing space) matches `"Movie Night"` but not `"Movie"`. The same id twice in `GenreIds` or `ActorIds` returns the movie once. A combined-metacharacter title (`100%\file_name_v2`, plus a `\%` adjacency case) matches only literally. | Q4. Q5. D4. |
| D16 | C5 | **Accepted.** Tasks add T019 `./scripts/run-vulnerable-packages.ps1` exit 0 and T020 `LamuFlix.ArchitectureTests` green (called out separately, even though T018 covers it). **Property-test opt-out:** DEV-302 adds no Core type or domain invariant, and the adapter behaviour is proven by the integration matrix. `MovieQuery`'s round-trip property test already exists (constitution III). The Conductor records this opt-out in the `DEV-302` task note (AGENTS.md rule). | harness.yml `propertyTests`, `vulnerablePackages`. Constitution PR gates. |
| D17 | C6 | **Rejected as a test, kept as a review check.** `sealed` and the primary constructor on `EfMovieCatalog` are verified at code review (Ledger/Compass axis). No reflection test. | Tests assert behaviour, not type shape. Reviewers enforce constitution Types. |
| D18 | C7, C10 | **Accepted (wording).** `MovieRecord.Id` is `MovieId?` (`MovieRecord.cs:9`), so the projection and tie-breaker use the persisted non-null id, with `!` in the expression. The spec states it without spelling the code literally. The SQL-shape assertion is scoped to the **SELECT list** of the page query: it holds only id and title and has no JOIN. `ORDER BY` and `WHERE` may reference other columns and EXISTS subqueries. | Q11. |
| D19 | C8 | **Accepted.** The Details matrix adds "after `GetDetailsAsync`, a fresh context's ChangeTracker is empty". | Q11. FR-008. |
| D20 | S7 | **Accepted.** The SQL-shape hook is a `DbCommandInterceptor` capturing `CommandText`. Log scraping is not used. | A stable public EF surface. No new package. |
| D21 | S6, S10 | **Accepted as recorded risk.** `ILIKE '%…%'` cannot use the btree title index, so text browse is a sequential scan. That is acceptable at personal-library scale. A pg_trgm index would be a schema change and gets a follow-up only if slowness is reported. ILIKE case-folding follows the DB collation, and tests use ASCII. Both go in plan Risks. | Recon §4.3. |
| D22 | L3 | **Accepted.** §5.2's `Persistence/` test paths are superseded: test files stay flat in `tests/LamuFlix.IntegrationTests` (plan layout choice). | Precedent of `MigrationTests.cs` and `PersistenceRoundTripTests.cs`. |
| D23 | L6, L7 | **Accepted.** Test conventions to state in plan and T001: `Method_Condition_Expected` naming; `TestContext.Current.CancellationToken` for every async call; only `// arrange`, `// act`, `// assert` comments; seed timestamps as fixed `DateTimeOffset` literals, never `DateTime.Now` or `UtcNow` (banned symbols, RS0030). | Constitution Comments and TimeProvider rules. `PersistenceRoundTripTests.cs` precedent. |
| D24 | L4, C11 | **Accepted.** Fix the T001 colon. | Editorial. |
| — | L8 | No-op. It was already settled in analyze round 1: Details uses the private flat shape and maps in memory. | Plan Approach. |
| — | C12, S12 | No action. They confirm the Q11 interpretation and the security posture. | — |

## 8. Paging offset (Q10, resolved by recon-DEV-302 §7 lines 120-179)
- `Page` is `public sealed record Page(int Number, int Size)` (`src/LamuFlix.Core/Library/Page.cs:3`).
- `MovieQueryValidator` enforces `Page.Number >= 1` and `Page.Size` in [1, 100] inclusive (`src/LamuFlix.Infrastructure/Library/MovieQueryValidator.cs:10-11`). `MovieQueryValidatorTests.cs:11-29` confirms this.
- **Offset formula:** `Skip((query.Page.Number - 1) * query.Page.Size).Take(query.Page.Size)`, so page numbers are 1-based.
- `EfMovieCatalog` trusts the validated query. It does not re-validate, clamp or add a default for `Number` or `Size`, per Q10 ("no replacement defaults or hardcoded size").
- Tests use `Number` from 1. "First page" means `Number = 1`. The past-end case uses `Number` beyond `ceil(TotalCount / Size)`.

## 7c. Phase B decision (Keel, 2026-09-30)
- **D25 (Patron ruling, CONCLUSIONS c3aee14; §2.3 item 6):** `WhereRuntime` and `WhereYear` compare the converted columns directly against value-object bounds (`m.RuntimeMinutes >= lower`, `m.ReleaseYear <= upper`). EF Core 10 cannot translate `.Minutes` or `.Value` on value-converted columns, and `EF.Property<int?>` throws `InvalidCastException` (recon by Wisp, relayed by the Conductor). Relational operators translate through the existing converters.
  - Edit `src/LamuFlix.Core/Domain/Runtime.cs` and `ReleaseYear.cs` additively. Each type implements `IComparable<T>` and the `<`, `<=`, `>`, `>=` operators, ordered by `Minutes` or `Value`. A null sorts lowest, and two nulls are equal. Constructors, invariants, converters, schema and persistence configuration do not change.
  - Build the bounds from the already-validated request values with `TryCreate` (`ReleaseYear` takes the injected `TimeProvider`'s now). Do not relax any invariant.
  - Keep the explicit unknown-value handling (D8, D12). A null column must not satisfy a lower bound just because null sorts lowest in Core, so Q6 and NULLS LAST stay as they are.
  - **D16 narrowed:** the property-test opt-out no longer covers Core. New task T-new, before the `WhereRuntime`/`WhereYear` tasks: unit tests in `LamuFlix.UnitTests` cover `CompareTo`, every operator, and null on either or both sides. Add an FsCheck property for each type: the operator and `CompareTo` results agree with `int` ordering of `Minutes`/`Value`, and null sorts lowest. Core Stryker covers these edits. The existing PostgreSQL inclusive-bound and unknown-value cases remain the proof of translation.
  - Rejected: a shadow int property or a converter swap (persistence config change), raw SQL (breaks composable predicates, D1/D6), hand-built `Convert` expression trees (untranslatable).
