# DEV-302 — Patron grill rulings

Round 1 of 1, Q1–Q12. Worktree: `F:\Dev\LamuFlix.worktrees\feature-302-spec`; branch: `feature/302-spec`. Ticket read live with `scripts/get-task.ps1 -TaskId DEV-302` on 2026-09-30; no local receipt artifact exists. Recon facts are accepted from Keel and `recon-DEV-302`. No owner checkbox is introduced; Gate 1 remains closed pending the normal spec process.

The current assigned-role §2.3 and `specs/PRODUCT.md` §5 supersede the older escalation wording in `lamuflix-team-charter:9-17/28`.

## Q1

**Keel:** Scope of MovieSummary: keep it (Id, Title) as-is, or does the ticket require widening it (year/rating/runtime/poster)? Widening changes a Core port DTO.

**Patron:** Keep MovieSummary(MovieId Id, string Title) unchanged; project the stored file Title. No year/rating/runtime/poster DTO expansion.

- Basis: DEV-302 ticket L16-17 (existing MovieSummary projection); src/LamuFlix.Core/Ports/MovieSummary.cs:7; specs/PRODUCT.md §3/§5 (ticket authority and DTO care item).

---

## Q2

**Keel:** GetDetailsAsync: implement it in DEV-302 (the class must satisfy IMovieCatalog) — full MovieDetails with Metadata, or is a minimal implementation acceptable? Tested?

**Patron:** Implement the complete existing MovieDetails contract, returning null only for a missing Id. Map Id/Title/Path/Format and all existing MovieMetadata members from persisted scalar counterparts; absent representable metadata yields Metadata=null, otherwise MetadataTitle falls back to Title when absent. No stub, fabricated missing fields, or DTO expansion. Integration-test found-with-metadata, found-without-metadata, partial metadata, and missing Id.

- Basis: src/LamuFlix.Core/Ports/IMovieCatalog.cs:8-12; MovieDetails.cs:7-12; src/LamuFlix.Core/Domain/MovieMetadata.cs:7-38; DEV-302 ticket L2 (functional adapter); constitution I/IX. Completing the existing port is adapter implementation, not an added use case.

---

## Q3

**Keel:** DI registration of EfMovieCatalog as IMovieCatalog: in scope? Where (existing Infrastructure registration extension, scoped lifetime)?

**Patron:** Approve scoped IMovieCatalog -> EfMovieCatalog registration through a small AddMovieCatalog extension in the existing Infrastructure/Persistence folder. Recon found only the Pipeline AddHandler extension, not an existing persistence composition extension; do not mix persistence registration into AddHandler. No new layer/project/dependency, host rewiring, connection configuration, or DbContext provisioning.

- Basis: DEV-302 ticket L2; constitution I (Infrastructure adapter ownership); specs/PRODUCT.md §2/§5 (no speculative layers; deliberate file/architecture ruling). Scoped ownership matches the DbContext dependency; runtime host composition remains separate as in specs/DEV-19/CONCLUSIONS.md Q3.

---

## Q4

**Keel:** Text filter: which columns (Title only / Title+MetadataTitle / actors), match semantics (case-insensitive contains via ILIKE vs EF.Functions), and must LIKE wildcards (%,_,\) in user text be escaped?

**Patron:** [assumed] Search Title only, with case-insensitive literal substring matching through EF.Functions.ILike and an explicit backslash escape character. Escape backslash first, then percent and underscore, before adding surrounding percent wildcards. Null/blank text is no filter; do not silently trim nonblank text or search MetadataTitle/actor names.

- Basis: DEV-302 ticket L7 (case-insensitive title search using ILike); specs/PRODUCT.md §4/§5; Npgsql ILike escape overload documentation linked below. Literal substring handling prevents user text being interpreted as pattern syntax.

---

## Q5

**Keel:** GenreIds / ActorIds: match ANY of the given ids (OR) or ALL (AND)? Combined across filters as AND?

**Patron:** [assumed] ANY within GenreIds and ANY within ActorIds; AND between the two and every other active filter. Default/empty arrays impose no filter; duplicate ids cannot duplicate movie rows. Use navigation predicates over the mapped persistence records.

- Basis: DEV-302 ticket L5-13 (chainable predicates); constitution III; specs/PRODUCT.md §4. Multi-select alternatives within one facet are OR; independent facets narrow the result together.

---

## Q6

**Keel:** Runtime/Year ranges: inclusive bounds? Open-ended ranges (only min or only max)? Movies with NULL runtime/year excluded when that range filter is set?

**Patron:** Bounds are inclusive and independently optional. A null range means no filter. A present Runtime range includes null runtimes exactly when IncludeUnknown=true; known runtimes must satisfy every supplied bound. A present Year range excludes null years. Present ranges with both bounds null retain those same unknown-value rules; do not reject or collapse them.

- Basis: DEV-302 ticket L10-11; src/LamuFlix.Core/Library/RuntimeRange.cs:3 (IncludeUnknown); YearRange.cs:3; constitution III:160 (Min <= Max); specs/DEV-297/CONCLUSIONS.md D1 (present empty ranges remain distinct).

---

## Q7

**Keel:** Statuses empty/null = no filter; InWatchlist null = no filter, true = only watchlist, false = only NOT in watchlist?

**Patron:** Yes: default/empty Statuses means no filter; nonempty Statuses matches ANY selected status. Statuses is ImmutableArray, not a nullable array. InWatchlist=null is no filter, true selects watchlisted movies, false selects non-watchlisted movies.

- Basis: DEV-302 ticket L12-13; recon-DEV-302:18/20; constitution III (typed query members).

---

## Q8

**Keel:** Sort keys: Title sorts on Title (file title) or COALESCE(MetadataTitle, Title)? Rating = ImdbRating? Nulls last in BOTH ascending and descending?

**Patron:** [assumed] Title sorts on file Title; Year on ReleaseYear, Rating on ImdbRating, Runtime on RuntimeMinutes. Apply requested Direction to the value key, with null rank ascending so nullable keys stay last in BOTH directions. Title is required and has no null case.

- Basis: DEV-302 ticket L14-15/L23; constitution III:154-156; MovieRecord.cs:11/31-35; MovieSummary.cs:7; specs/PRODUCT.md §4 (choice of title ordering).

---

## Q9

**Keel:** Deterministic tie-breaker for stable pagination (e.g. then Title, then Id)? Required by acceptance?

**Patron:** Require unique ordering: after a nullable primary key, use Title ascending then Id ascending; for primary Title use Id ascending. [assumed] Secondary-key directions stay ascending regardless of primary Direction. The ticket does not name tie-breakers, but correct pagination requires them for an unchanged dataset; this does not promise stability during concurrent writes.

- Basis: DEV-302 ticket L22 (correct pagination); Microsoft Learn pagination warning (fully unique ordering), linked below; specs/PRODUCT.md §4/§5 (taste directions and engineering basis).

---

## Q10

**Keel:** Pagination: page size source (MovieQuery.Page fields), TotalCount via separate COUNT on the filtered query, page past end returns empty items with correct TotalCount?

**Patron:** Yes: use validated MovieQuery.Page.Number/Size and the existing numbering convention, with no replacement defaults or hardcoded size. Count the complete filtered query before paging; then fetch the sorted projected page. Execute COUNT and page sequentially on the same context, forwarding CancellationToken. A page past the end returns initialized empty Items and the filtered TotalCount.

- Basis: DEV-302 ticket L22; src/LamuFlix.Core/Library/Page.cs:3; recon-DEV-302:17-18; constitution III:160 and async conventions. Exact validator numbering was not verified within this ask's read budget; Keel must cite the existing validator when spelling the offset formula.

---

## Q11

**Keel:** Query mechanics: AsNoTracking + Select projection straight to MovieSummary (no Include, no entity materialisation) — required, and should it be asserted (e.g. ChangeTracker empty) in tests?

**Patron:** Required: compose translated IQueryable<MovieRecord> predicates, explicit whitelisted sorting and paging, AsNoTracking, then direct Select to MovieSummary without Include or entity materialization. In a fresh read context, assert ChangeTracker stays empty; that alone does not prove direct projection, so also verify the browse data SELECT reads only Id/Title through existing EF command logging/interception.

- Basis: DEV-302 ticket L5-17; constitution I/III:157-159; specs/DEV-19/CONCLUSIONS.md Q1 (mapped MovieRecord represents the Movie in persistence, without mapping Core). SQL observation checks the named projection guarantee rather than relying only on tracking.

---

## Q12

**Keel:** Test strategy: integration tests in tests/LamuFlix.IntegrationTests on PostgresFixture covering each filter, pagination, nulls-last per sort key; any unit tests expected (e.g. for filter composition helpers)? Round cap for spec review: 2 rounds OK?

**Patron:** Approve tests/LamuFlix.IntegrationTests with existing PostgresFixture: every filter individually and representative combinations, literal text metacharacters, ANY semantics, inclusive/open/empty ranges and IncludeUnknown, all watchlist/status modes, each sort and direction with nullable keys last, tied pagination/count/past-end, details, scoped registration, and direct untracked projection. No mocked IQueryable, EF InMemory, new test framework, or mandatory duplicate unit tests for translated predicates. Spec-review cap is two rounds, hard; grill is complete after these 12 answers.

- Basis: DEV-302 ticket L18-23; constitution IX:282-307; recon-DEV-302:50-58/64-70; Keel's Q12 two-round proposal and assigned-role grill budget.

---

## Documentation consulted

- [Microsoft Learn: Pagination](https://learn.microsoft.com/ef/core/querying/pagination): ordering must be fully unique; offset pagination does not provide a concurrent-write snapshot guarantee.
- [Microsoft Learn: Tracking vs. No-Tracking Queries](https://learn.microsoft.com/ef/core/querying/tracking#no-tracking-queries): read-only no-tracking queries avoid tracking entity instances.
- [Npgsql ILike API and escape overload](https://github.com/npgsql/efcore.pg/blob/main/_autodocs/api-reference/db-functions-core.md), queried through Context7: the escape overload supports literal wildcard handling. The ruling uses only PostgreSQL percent/underscore pattern wildcards, not the broader wildcard list in the generated documentation.

## Ticket description line basis

```text
1: ### Overview
2: Implement `EfMovieCatalog` in `LamuFlix.Infrastructure/Persistence/` providing high-performance query projections with `NULLS LAST` sorting.
3:
4: ### Scope & Technical Design
5: 1. **Predicate Composition**:
6:    - Create clean, chainable `IQueryable<Movie>` extensions:
7:      - `WhereText(string? text)`: case-insensitive title search (using ILike in Postgres).
8:      - `WhereGenres(ImmutableArray<int> genreIds)`
9:      - `WhereActors(ImmutableArray<int> actorIds)`
10:      - `WhereRuntime(RuntimeRange? range)`
11:      - `WhereYear(YearRange? range)`
12:      - `WhereStatuses(ImmutableArray<EnrichmentStatus> statuses)`
13:      - `WhereInWatchlist(bool? inWatchlist)`
14: 2. **NULLS LAST Sorting**:
15:    - Implement explicit sort mapping using `OrderBy(m => m.Year == null).ThenBy(...)` or EF functions to ensure null years/ratings/runtimes sort to the end.
16: 3. **Direct Projection**:
17:    - Project directly to `MovieSummary` records with `.Select(...)` without entity tracking (`AsNoTracking`).
18: 4. **Integration Tests**:
19:    - Test each filter predicate and verify NULLS LAST sort order against Testcontainers Postgres.
20:
21: ### Acceptance Criteria
22: - Integration tests confirm correct filtering and pagination.
23: - Movies with null ratings/years appear at the end of sorted queries.
```
