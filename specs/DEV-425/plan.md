# Implementation Plan: DEV-425 Director facet and Director filter

**Branch**: `feature/425-spec` | **Date**: 2026-10-28 | **Spec**: `specs/DEV-425/spec.md`

**Input**: Frozen brief `specs/DEV-425/brief.md` (Q1–Q8); `CONTEXT.md`; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri notes `DEV-425`, `recon-DEV-425`; `specs/PRODUCT.md`; `.specify/memory/constitution.md`

**Note**: Palette `design.md` is required before plan challenge on this `ui:` ticket; design cannot alter Q1–Q6 behavior, API shape, dependency choices, or frozen scope. Plan challenge and Gate 1 remain separate uncleared steps. No ADR for this M ticket.

## Summary

Deliver a director facet (`GET /api/directors` → `DirectorDto[]` with id, name, movieCount; orphans excluded) and a singular `directorId` browse filter through existing Core → EF → API → generated-contract → FilterPanel hosts. Strict API/C# parsing; lenient live-web raw scalars forwarded for authoritative validation. Searchable dependency-free single-select in Library and Watchlist with page reset, local loading/error/retry, and approved accessibility. No schema, package, layer, or parser-wide cleanup.

## Technical Context

**Language/Version**: C# (.NET 10 backend) + TypeScript (React / Vite frontend under `web/`)

**Primary Dependencies**: Existing only. No new NuGet or npm package (Q6; §2.3 care 1).

**Storage**: Existing PostgreSQL via EF Core; `directors` and `movie_directors` already present. No migration or schema change (Q6).

**Testing**: Backend xUnit + Testcontainers/real Postgres for facet and browse; Core unit tests including seeded property roundtrips; API integration following existing FacetEndpointsTests / LibraryEndpointsTests / MovieQuery* patterns. Frontend Vitest + RTL extending FilterPanel.test.tsx, filterQuery.test.ts, browseWatchlist.test.tsx. Mutation via `scripts/run-mutation.ps1` (never Stryker `--since` from a worktree).

**Target Platform**: Web API + Vite SPA (same as repo).

**Project Type**: Web application (`src/` + `tests/` + `web/src/`).

**Performance Goals**: Independent facet fetch per host needing it; client-side search capped at eight suggestions; no new endpoint search/pagination (Q4).

**Constraints**: Contract chain C# DTO → OpenAPI → generated TS; zero hand edits to generated files; no LocalPlay/secrets/Process.Start; Roslyn/complexity/InspectCode/format/full test/web gates at pipeline stages without threshold changes; property roundtrips in scope with no preapproved opt-out (brief Gate expectations).

**Scale/Scope**: Size M; narrow edits along the named Q6 chain only.

## Constitution Check

*GATE: analysis only for Phase A; Gate 1 remains user-owned.*

| Care / principle | Response |
|---|---|
| §2.3 care 1 (dependencies) | No new NuGet/npm; no package swap (Q6). PASS. |
| §2.3 care 2 (architecture) | Reuse existing query/handler/port/facet and FilterPanel; no new project, top-level folder, repository/mediator/services abstraction, or state library (Q6). PASS. |
| §2.3 care 3 (schema) | No schema beyond ticket tables; directors/movie_directors already exist; no migration (Q6). PASS. |
| §2.3 care 4 (public API) | Ticket-authorized `GET /api/directors`, `DirectorDto`, and browse `directorId` (Q1–Q2). PASS. |
| §2.3 care 5 (LocalPlay/secrets/Process.Start) | Untouched (Q6). PASS. |
| §2.3 care 6 (file deletion/rewrite) | Only narrow named-chain edits; no tracked-file deletion or broad rewrite (Q6). PASS. |
| I Ports and Adapters | `GetDirectorsAsync` / `DirectorFacet` on existing `IMovieCatalog`; handlers in Core Features; EF adapter (Q1, Q6). |
| III / API and Contract Rules | Strict MovieQueryString + API 422; OpenAPI committed and regenerated; web consumes generated types (Q2, Q6). |
| VIII Ubiquitous Language | Terms Director / Director facet / Director filter from `CONTEXT.md` (Q1). |
| IX Test Pyramid | Real Postgres for query translation; no mocked driver for EF behavior (Q6). |
| Static-Analysis / mutation gates | Run later at pipeline stages; exit 2 SKIPPED/N/A/SKIP never PASS; Could not run blocks (brief Gate expectations). |

No constitution departure or ticket-text change is authorized. No structural owner checkbox (Q8).

## Project Structure

### Documentation (this feature)

```text
specs/DEV-425/
├── brief.md, CONTEXT.md, CONCLUSIONS.md, ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file
├── tasks.md
└── DRAFTING_RECEIPT.md
```

No clarify/checklist for this M ticket (brief Gate expectations). No `research.md`, `data-model.md`, `quickstart.md`, or `contracts/` directory: approach, data shape, and contract delta are frozen in the brief. Palette adds `design.md` before plan challenge (not authored by Quill).

### Source Code (repository root)

```text
src/LamuFlix.Core/
  Library/MovieQuery.cs
  Library/MovieQueryString.cs
  Ports/IMovieCatalog.cs                    # DirectorFacet + GetDirectorsAsync
  Features/Library/GetDirectorsQuery.cs     # new
  Features/Library/GetDirectorsQueryHandler.cs  # new

src/LamuFlix.Infrastructure/Persistence/
  EfMovieCatalog.cs                         # GetDirectorsAsync + WhereDirector
  MovieRecordQueryExtensions.cs             # WhereDirector via Directors Any

src/LamuFlix.Api/
  Endpoints/FacetEndpoints.cs
  Endpoints/DirectorDto.cs                  # new
  Endpoints/BrowseMoviesRequest.cs
  Endpoints/LibraryEndpoints.cs
  HandlerRegistration.cs

web/src/
  api/openapi.json                          # regenerated
  api/types.ts                              # regenerated
  api/client.typecheck.ts                   # coverage via established path
  features/library/filterQuery.ts
  features/library/FilterPanel.tsx
  library/browseParams.ts
  library/LibraryView.tsx
  library/libraryQueries.ts                 # only if required by existing browse/facet path

tests/
  LamuFlix.UnitTests/ … MovieQueryTests, MovieQueryStringTests, GetDirectors handler coverage
  LamuFlix.IntegrationTests/ … FacetEndpointsTests, EfMovieCatalogFacetsTests,
    EfMovieCatalogBrowseTests, LibraryEndpointsTests (+ OpenAPI drift/snapshot via established path)
  catalog test fakes                        # only as IMovieCatalog compilation requires

web tests:
  FilterPanel.test.tsx, filterQuery.test.ts, browseWatchlist.test.tsx (extend existing)
```

**Structure Decision**: Follow brief Q6 binding order. New files limited to GetDirectors query/handler and DirectorDto (and tests forced by those). All other work is narrow edit of named existing paths.

### Frozen file scope

Authorized creates/edits are those named in brief Q6. Genre/cast behavior must not change. Do not wire `libraryUrl`. Do not alter schema, ingestion, packages, or layers. Additional forced compilation sites (test fakes) may be touched only as required by the new `IMovieCatalog` member; broader drift needs a cited ruling.

### Implementation order (binding; brief Q6)

1. Core: `MovieQuery` / `MovieQueryString` / `IMovieCatalog` + `GetDirectorsQuery` / handler.
2. Persistence: `EfMovieCatalog` + `MovieRecordQueryExtensions` (`WhereDirector` via existing navigation `Any`).
3. API: `DirectorDto`, FacetEndpoints, BrowseMoviesRequest, LibraryEndpoints, HandlerRegistration.
4. Contract: generate OpenAPI then TypeScript/mocks/typecheck; never hand-edit generated files.
5. Web: filterQuery, FilterPanel, browseParams, LibraryView, optional libraryQueries wiring; both hosts; interactions per Q2–Q5.
6. Matching tests interleaved (fail first where TDD applies) covering the Q6 proof list.

## Approach

- **Facet (Q1, Q4)**: Project directors with `Movies.Count` (or equivalent distinct linked-movie count) where count ≥ 1; order Name then Id; map to `DirectorDto`.
- **Browse filter (Q2, Q3)**: Singular `DirectorId`; ReadNumber/Seen-set style duplicate rejection at API; MovieQueryString same; EF `Any` on Directors; AND with existing fluent browse including watchlist.
- **Web scalar split (Q2)**: Live filterQuery retains raw string; first `params.get` wins; browseParams forwards nonempty scalar; empty omits; FilterPanel commit already resets page=1 — keep that for director select/replace/clear.
- **UI (Q3–Q5)**: Dependency-free combobox mirroring cast suggestion limit (8) and genre count-badge precedent; independent facet query state per host; local error/retry; ASSUMPTIONS.md copy.
- **Contract (Q6)**: Established opt-in OpenAPI regeneration then inherited TypeScript generation; `client.typecheck.ts` coverage; no hand-rolled DTOs.

## Test Strategy

| Surface | Coverage (brief Q6) |
|---|---|
| Postgres facet | Orphan exclusion, empty list, distinct counts, shared-name identities, Name/Id order |
| Postgres browse | Null passthrough, match, unmatched/zero/negative, AND with watchlist, no duplicate rows |
| Core | Equality/hash for DirectorId; null/default; parse/format roundtrips including seeded property roundtrips; empty/valid/duplicate/malformed/overflow; unknown without extra validator |
| API | Facet field set/types and empty array; no required role; browse filter; 422 failures |
| Web | Raw scalar + first-wins; empty omission + API forwarding; generated contract/typecheck; URL reload/navigation; page reset with other filters preserved; select/replace/clear/Clear all; same-name/counts; typing without predicate change; keyboard/ARIA; missing selection; independent loading/error/retry; both hosts |
| Fakes | Update existing catalog test fakes only as compilation requires; handler coverage following existing facet handlers |

No Gherkin unless separately opted in (acceptance-tests policy).

## Gates (Phase B / later pipeline — not Phase A)

Configured Roslyn, complexity (then refactor threshold), InspectCode, mutation (`run-mutation.ps1`), property tests, vulnerable packages, format, full `dotnet test`, web gates, and generated API drift checking. Use harness thresholds; never lower. Exit 1 and Could not run block; scope-empty exit-2 is SKIPPED (scope-empty) never PASS; explicit mutation exclusions N/A; disabled gates SKIP never PASS.

Phase A (this draft) runs none of these and claims no gate result. Intake baseline counts in note DEV-425 are measurements at the intake base only.

## Loop discipline (Q7)

- Closing bar: Critical, High, Medium block; Low nonblocking.
- Round cap: two review rounds; ≤ two fix commits per round; third-round unresolved above-bar → user review on PR, stays blocked.
- All three Sentry, Ledger, Compass axes required for M.
- Frozen scope: Q1–Q6 Director facet and single-select Director filter in Library/Watchlist, necessary chain edits, approved a11y/local states. Everything else is a follow-up issue, not a finding in this round.

## Risks

- **Contract drift**: Mitigated by mandatory regeneration order and drift checks (Q6).
- **Lenient web vs strict API**: Intentional split (Q2); web tests must not assume client-side int sanitization beyond settled raw/first-wins/forward behavior.
- **Missing selected identity**: Must remain selected/clearable with `Director <id>` fallback — never silently broaden (Q3).
- **Shared-file FilterPanel edits**: Sequence web tasks to avoid parallel writers on the same files.
- **Recon note content**: Current `recon-DEV-425` holds Q2 URL evidence; intake facts are preserved in brief/CONCLUSIONS citations, not remeasured here (brief Identity).

## Complexity Tracking

> Constitution Check has no violations; no justification entry follows.

## Ordering

See `tasks.md`: Core → Persistence → API → Contract → Web → verification tasks at pipeline stages. Quill divided phases without reversing Q6 dependencies. Same-file edits are serial. Palette design precedes plan challenge; Keel analyze is the next read-only step after this draft.
