# Implementation Plan: DEV-322 Filter Panel

**Branch**: `feature/322-spec` | **Date**: 2026-10-12 | **Spec**: `specs/DEV-322/spec.md`

**Input**: `specs/DEV-322/spec.md`; frozen brief `specs/DEV-322/brief.md` (Q1-Q12; Q12 at brief.md:170-181, F2 remedy at brief.md:204-210, F3 decision at brief.md:212-217); `recon-DEV-322:11-63,65-106,108-187,189-234`; `specs/PRODUCT.md`; rulings `specs/DEV-322/CONCLUSIONS.md` (Q12 at :54-58).

**Note**: Palette `design.md` is an input for this `ui:` ticket; plan challenge remains a separate next step. Design cannot alter frozen ticket behavior, API shape, dependency choices, or range serialization.

## Summary

Deliver the ticket-named rich Filter Panel (debounced text search, genre/cast multi-select, runtime/year ranges, eight sort choices, status chips) as one feature slice in `web/src/features/library/`, with the URL as the committed source of truth and exactly one movie refetch per committed query change. The only backend change is additive required integer `movieCount` on `GenreDto` for `GET /api/genres`, counted over the existing genre/movie relationship and regenerated through the contract chain (C# DTO, committed `openapi.json`, generated TypeScript via DEV-320 tooling).

## Technical Context

**Language/Version**: C# (.NET 10 backend) + TypeScript (React 19, Vite frontend)

**Primary Dependencies**: Existing: React 19.3.0, react-dom 19.3.0. Sole permitted addition, only if absent at Phase B pickup: TanStack Query (constitution-chosen, pinned per Q3). No slider, multi-select, debounce, router, or URL-state package.

**Storage**: Existing database via EF Core; no schema change, no table, no migration. Counts query existing `movie_genres` join (`MovieConfiguration.cs:154-159`) through the `GenreRecord.Movies` navigation.

**Testing**: Backend: xUnit (integration with real database via existing infrastructure; handler unit tests; count-only scope per Q10, existing MovieQueryString/validator witnesses unchanged). Frontend: Vitest + RTL + MSW with fake timers; web-to-existing-contract compatibility extends the already planned `filterQuery.test.ts`/`LibraryView.test.ts` suites with independently specified expected API queries (no new backend file or cross-runtime harness): empty URL yields all four Q12 defaults; explicit versus omitted defaults normalize equally; text/discrete change resets API page to 1 while preserving supplied pageSize/inWatchlist; arrays preserve order; generated sort/direction/status spellings and lowercase booleans; active runtime/year bounds with empty extents and runtimeIncludeUnknown; canonical-key equivalence and one request per effective query change. MSW never supplies missing defaults or accepts omitted required keys as success. Mutation (`scripts/run-mutation.ps1`) applies to changed `src` files.

**Target Platform**: Web API + Vite SPA (same as repo).

**Project Type**: Web application (backend `src/` + `tests/`; frontend `web/src/`).

**Performance Goals**: Text commits 300 ms after last keystroke; one fetch per committed effective API query change (canonical API query string is the movie query key; equivalent omitted/explicit Q12 defaults share one key); prior results retained during pending refetch (`placeholderData: keepPreviousData`); fixed control/badge dimensions, no control unmount on refetch.

**Constraints**: Zero compiler warnings; Roslyn/complexity/InspectCode gates on changed files; contract drift enforcement (generated doc must equal committed `openapi.json` and the Verify snapshot; regeneration only through the verified opt-in `LAMUFLIX_REGENERATE_OPENAPI=true` workflow, cleared before drift enforcement); no hand edits to generated files; backend `TimeProvider` for backend time (no `DateTime.Now`); frontend year extent via the `currentYearExtent(now)` mount-time seam (no clock package); no new assertion/mocking library.

**Scale/Scope**: One feature slice (4 new handwritten frontend files + tests); up to 7 existing production paths (including both conditional package files); up to 9 existing test/contract paths (including conditional/source-only-if-forced edits). Size M.

## Constitution Check

Constitution checks below apply to this analysis and the conditional Phase B pickup; Gate 1 remains a separate owner gate.

- Dependencies (care 1): no new npm package for slider/multi-select/debounce/router/URL state (Q3 native + local hook). TanStack Query only if absent at Phase B pickup, pinned as the constitution-chosen dependency (constitution:335 via CONCLUSIONS.md Q3). No NuGet dependency. PASS.
- Architecture (care 2): feature folder under existing `web/src/features/library/` is not a new top-level folder or layer (Q5); no repository/mediator/services abstraction, no state library, no router. PASS.
- Database schema (care 3): no schema or migration change; counts computed over existing tables (Q1). PASS.
- API shape (care 4): additive required `movieCount` on `GET /api/genres` is the ticket-delivered count badge (Q1); no new route, no status-code change. PASS.
- Security & local execution (care 5): no panel scope touches playback, `Features:LocalPlay`, secrets, or `Process.Start` (Q1-Q5). PASS.
- File scope (care 6): `App.tsx`/`App.test.tsx` edits forced by the URL/refetch AC (Q5); genre endpoint/catalog/contract test edits forced by Q1. Conditional library-page mount resolved by bounded pickup recon. PASS.
- Contract chain: C# DTO, committed `openapi.json`, generated `types.ts` via DEV-320 tooling; web consumes generated types only (Q4). PASS.

Normative checks use recon-DEV-322:154-161: principle III (compact URL source of truth plus Q12 canonical API query equality and required MovieQuery query-string property round-trip proof), principle V (existing validation failures return 422, consistent with corrected CONCLUSIONS.md:47-52; Q12 preserves 422 with web transport defaults `Title`/`Ascending`/`1`/`20` per recon-DEV-322:192-219), principle IX (xUnit v3/Shouldly/NSubstitute/Testcontainers, no EF InMemory), Technology Stack Constraints, API and Contract Rules (generated client/MSW chain), Pull Request Quality Gates and Static-Analysis Gates. Principle III coverage is supplied by recon-DEV-322:169-185: tests/LamuFlix.UnitTests/Library/MovieQueryStringTests.cs:151-160, Format_RoundTripsValidatorAcceptedQueries, equality at :158; tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs:81-110, BrowseMovies_GeneratedQuery_RoundTripsToAnEqualDispatchedQuery, equality at :106. Both use FsCheck Prop.ForAll and Category=Property. Existing proof coverage is accounted for; this analysis does not claim current test execution or a passing gate.

No constitution departure or ticket change is authorized. Gate 1 remains a separate owner gate.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-322/
├── spec.md               # Feature specification
├── plan.md               # This file
├── tasks.md              # Task list
└── design.md             # Palette visual/contract presentation (input, cannot alter frozen behavior)
```

No clarify or checklist artifacts for this M ticket per the ask. No `research.md`, `data-model.md`, `quickstart.md`, or `contracts/` directory: the frozen brief plus `recon-DEV-322` already fix the approach, data shape, and contract delta, so no Phase 0/1 artifacts are generated.

### Source Code (repository root)

```text
src/
├── LamuFlix.Core/Ports/IMovieCatalog.cs                  # GenreFacet gains count (signature retained)
├── LamuFlix.Core/Features/Library/                       # GetGenresQuery/Handler unchanged (passthrough)
├── LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs  # GetGenresAsync counts via Movies nav
└── LamuFlix.Api/Endpoints/
    ├── GenreDto.cs          # Required wire movieCount
    └── FacetEndpoints.cs    # Maps count into wire DTO

web/src/
├── features/library/
│   ├── FilterPanel.tsx          # All panel controls (new)
│   ├── FilterPanel.test.tsx     # Control/interaction coverage (new)
│   ├── LibraryView.tsx          # Generated-client queries, count/title results (new)
│   ├── LibraryView.test.tsx     # Refetch/retained-data coverage (new)
│   ├── filterQuery.ts           # Compact URL serialization/hydration + canonical API query + query key (new)
│   ├── filterQuery.test.ts      # URL/group-ordering/canonical-equality coverage (new)
│   ├── useDebouncedSearch.ts        # 300 ms local hook (new)
│   └── useDebouncedSearch.test.ts   # Fake-timer coverage (new)
├── api/
│   ├── openapi.json   # Regenerated committed baseline (Q1)
│   └── types.ts       # Regenerated via DEV-320 tooling (prerequisite, not authored)
├── App.tsx            # Minimal host, conditional per Q5
└── App.test.tsx       # Host AC coverage, conditional per Q5

tests/
├── LamuFlix.IntegrationTests/
│   ├── FacetEndpointsTests.cs        # Exact id/name/movieCount shape + values
│   ├── EfMovieCatalogFacetsTests.cs  # Real count translation/data behavior
│   └── OpenApiContractTests.cs (+ Snapshots/...verified.json)  # Drift baseline + snapshot regen
├── LamuFlix.UnitTests/Features/Library/GetGenresQueryHandlerTests.cs  # Count passthrough
└── (unchanged witnesses) LibraryEndpointsTests, ApiEndToEndBrowseTests,
    EfMovieCatalogBrowseTests, MovieQueryString/Validator tests: existing
    range parser/validator evidence, not re-planned for edits.
```

**Structure Decision**: Backend keeps the existing query/port/persistence flow (`IMovieCatalog` port, `EfMovieCatalog` persistence, `FacetEndpoints` slice, handler passthrough; no new abstraction, no DI change). Frontend is one feature slice under the existing `web/src/` tree. New filenames are plan decisions, not claims of existing files.

### Frozen file scope

Existing production modifications (frozen per brief):

| Path | Reason |
| --- | --- |
| `src/LamuFlix.Core/Ports/IMovieCatalog.cs` | Extend `GenreFacet` with count; retain `GetGenresAsync` signature |
| `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` | Query global count through existing `Movies` relationship; retain name-then-id ordering and `AsNoTracking` |
| `src/LamuFlix.Api/Endpoints/GenreDto.cs` | Required wire `movieCount` |
| `src/LamuFlix.Api/Endpoints/FacetEndpoints.cs` | Map count into wire DTO |
| `web/src/App.tsx` | AC-forced minimal panel/results host, conditional per Q5 |
| `web/package.json`, `web/package-lock.json` | Only if constitution-chosen TanStack Query is absent at Phase B pickup; pin it per Q3 |

Existing test/contract modifications (frozen per brief):

- `tests/LamuFlix.IntegrationTests/FacetEndpointsTests.cs`: exact id/name/movieCount shape and count values; retain empty-array coverage.
- `tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs`: real count translation/data behavior and existing sorting/empty cases.
- `tests/LamuFlix.UnitTests/Features/Library/GetGenresQueryHandlerTests.cs`: count-bearing facet passthrough and empty result.
- `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`: exercise existing drift assertion; source changes only if the added field forces them.
- `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`: regenerate verified contract snapshot only through the established opt-in workflow (`LAMUFLIX_REGENERATE_OPENAPI=true` writes baseline plus snapshot together, cleared before drift enforcement; recon-DEV-322:221-225).
- `web/src/api/openapi.json` and `web/src/api/types.ts`: regenerated artifacts through contract tooling; `types.ts` regeneration runs the recon-verified inherited DEV-320 TypeScript workflow after T035 (command obtained at T001 pickup; never a DEV-322 substitute tool or handwritten DTO), and badge UI wires to required integer `movieCount` only after regeneration.
- `web/src/App.test.tsx`: replace readiness-only expectations with host integration AC coverage, conditional per Q5.
- `web/src/test/server.ts`: add/compose feature MSW handlers only if required by the inherited DEV-320 test setup.

No schema/entity configuration, migration, query parser, sort/status enum, query handler, DI registration, or unrelated source rewrite is planned. `GenreFacet` retains the same type, so empty-list implementations and DI composition expectations need no automatic edit. If build evidence identifies additional count-bearing construction sites, request bounded recon and record the forced scope amendment before editing.

### Implementation order

1. Phase B pickup stop: confirm DEV-320 `types.ts`/client tooling present with the exact inherited codegen command and a repeatable post-OpenAPI-delta regeneration path on the actual inherited base (else order after DEV-320 and report the missing prerequisite; presence of `types.ts` alone is insufficient); recon whether a library page and TanStack Query now exist (Q5/Q3 conditionals); recheck drift against main.
2. Backend count path: `GenreFacet` + `EfMovieCatalog.GetGenresAsync` database-translated count, `GenreDto` + endpoint mapping, bounded tests.
3. Contract chain order: after T035 run the opt-in OpenAPI/snapshot workflow then the inherited DEV-320 TypeScript workflow before wiring badge UI to `movieCount`.
4. Frontend foundations: `filterQuery.ts` compact-URL plus canonical-API-query/query-key normalization + `useDebouncedSearch.ts` before controls/queries; year extent via `currentYearExtent(now)` mount-time snapshot.
5. Controls + queries + minimal host; then URL/refetch and pending-data acceptance coverage.
6. Gates only at their pipeline stages (backend analyzers/complexity/InspectCode/format/tests/security/contract-drift; `web-implement` TDD and web gate; mutation on changed `src` scope via `scripts/run-mutation.ps1`, never Stryker from a worktree).

## Complexity Tracking

> Constitution Check has no violations; no justification entry follows.

No complexity exception or constitution departure is requested. The supplied evidence accounts for the required existing property-test coverage; verification remains at its pipeline stage.
