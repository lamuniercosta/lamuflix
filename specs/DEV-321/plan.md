# Implementation Plan: DEV-321 — Library browse view

**Branch**: `feature/321-spec` | **Date**: 2026-10-11 | **Spec**: `specs/DEV-321/spec.md`

**Input**: `specs/DEV-321/spec.md`, decided by `specs/DEV-321/brief.md` (grill closed Q10/12); facts `recon-DEV-321` sections 9–15.

**Note**: Phase A plan only. No implementation authorization (Gate 1 stays closed until the user merges the spec PR). Phase B additionally waits for the user merge of DEV-320 #124 plus Wisp recon of merged `types.ts`, `client.ts`, and abort-signal support (brief Q5).

## Summary

Extend the frozen browse contract additively (`MovieSummary` → `MovieSummaryResponse` on `GET /api/movies` with nullable `posterUrl`/`releaseYear`/`runtime`/`imdbRating` plus required string `enrichmentStatus`, same-row projection, no migration/join), regenerate OpenAPI + baseline snapshot + generated TS through the existing pipelines, and build the SPA Library view at the current root (feature-local `web/src/library/`, TanStack Query, hand-owned URL adapter over native History API, hand-written error boundary, responsive 2:3 grid, full codec controls, Previous/Next pagination), proven by extended real-DB backend tests and Vitest/RTL web tests per the Q8/Q9 envelope.

## Technical Context

**Language/Version**: C# on .NET 10 (backend) + React 19 / TypeScript / Vite (SPA).

**Primary Dependencies**: Ticket-named `@tanstack/react-query` only (new); generator/client packages inherited from DEV-320 #124 after its user merge — never re-added here, never copied unmerged, never hand-duplicated. No router, state, validation, or error-boundary package (brief Q2/Q5/Q6).

**Storage**: Existing Postgres `movies` row already carries every tile scalar (`poster_url`, `release_year`, `runtime_minutes`, `imdb_rating`, `status` int via `EnrichmentStatusConverter`); read-only browse, no migration (recon 11, brief Q1).

**Testing**: Backend — xUnit with existing assertion/mocking conventions, Testcontainers Postgres for translation/projection/serialization; no new test package. Web — Vitest + RTL with existing MSW/RTL setup (`web/src/test/server.ts`, `setup.ts`); table/generated adapter cases, no new property-testing dependency. C# property gate under the Q9 amendment (brief:131): `MovieQueryString` unchanged, so no new C# URL round-trip property tests and the SPA adapter owns round-trip proof; any wider property-gate opt-out applies only if the changed C# introduces no invariant, with the reason recorded — unchanged `MovieQueryString` alone does not waive it.

**Target Platform**: ASP.NET Core Web API + Vite SPA (existing `web/src/main.tsx`, `index.css` entry).

**Project Type**: Web application (API backend + SPA frontend), ports and adapters with feature-organised Core per Constitution §I. Terminology note: `specs/PRODUCT.md` §1 calls the backend "vertical slice / feature-folder architecture"; on architecture terminology the Constitution governs, so this plan uses the Constitution's terms. PRODUCT.md is not edited and no architecture change is proposed.

**Performance Goals**: None invented — the AC says "smoothly" with no metric (brief Q7). Skeletons reserve tile size (no layout shift); no per-tile fetches or prefetch.

**Constraints**: Contract chain C# DTO → OpenAPI (`web/src/api/openapi.json`) → generated TS (`web/src/api/types.ts`); generated files never hand-edited; regeneration via existing `LAMUFLIX_REGENERATE_OPENAPI=true` opt-in plus DEV-320 merged tooling. `TimeProvider`/seeded-`Random` rules and analyzer gates per repo non-negotiables; thresholds only from `harness.yml`, never hardcoded or lowered.

**Scale/Scope**: `size:L`, `ui:` (Phase A spec path). Bounded envelope: 3 production files + 1 conditional (`Program.cs` schema transformer scoped to `EnrichmentStatus`, only if regenerated output lacks the four-name string schema) + compile-forced construction sites; existing web entry files; 9 new feature-local files under `web/src/library/` plus adjacent tests.

## Constitution Check

- **I — Ports and adapters, feature-organised Core**: `MovieSummary` widening stays in the existing `Core/Ports/` port; the projection change stays in the Infrastructure adapter (`EfMovieCatalog`); no new port (so no ADR); no Core feature-folder cross-reference; no EF/Npgsql/RabbitMQ/HTTP-client reference added to Core.
- **III — Typed query model**: `MovieQuery`, `MovieSort`/`SortDirection` enumerations, and the `IQueryable` predicate extensions are unchanged; no reflection or string-named sort; `NULLS LAST` semantics preserved; query-string round-trip proof is owned by the SPA adapter tests under the Q9 amendment (no new C# round-trip tests).
- **V — Validated inputs, consistent errors**: `MovieQueryValidator` stays the single validation authority; invalid-filter failures stay 422 `ProblemDetails` through the single `GlobalExceptionHandler` (preserved, not re-mapped); the 422 invalid-filter state is never retried; no `BadRequest(ex.Message)` or new exception type.
- **VII — Configuration isolation and deterministic time**: no secrets, connection strings, or machine paths; no new options records; no `DateTime.Now`-family calls; LocalPlay untouched (no play/detail/mutation controls, no `Process.Start`).
- **IX — Test pyramid with real infrastructure**: backend translation/projection/serialization proven on Testcontainers Postgres; xUnit v3 + NSubstitute + Shouldly + AutoFixture/Faker.Net conventions kept; no new test package; no mocked Infrastructure; mutation threshold from `harness.yml`, never lowered.
- **Contract + analyzer gates**: C# DTO → OpenAPI → generated TS chain with drift check green; generated files never hand-edited; diff-scoped Roslyn/complexity/InspectCode gates plus format, `dotnet test`, and `scripts/run-web-gates.ps1` per brief Q9.

### Patron care-list rulings (kept separately; all decided in brief/CONCLUSIONS, not re-ruled here)

- Public API shape (care item 4): approved by Patron in brief Q1 (additive widening, cited ticket + charter + contract chain). No other shape change.
- Dependencies (item 1): `@tanstack/react-query` is ticket-named (DEV-321:30), not a new-dependency checkbox (brief Q5). No other runtime dependency.
- Architecture (item 2): `web/src/library/` is a feature folder, not a new top-level folder or layer (brief Q8). No new project/layer.
- Database schema (item 3): no migration (brief Q1; recon 11 same-row columns). If one is needed, stop and re-ask Patron.
- Security/LocalPlay (item 5): untouched — no play/detail/mutation controls (brief Q7).
- File scope (item 6): `App.tsx` + orphaned readiness test replacement is a forced edit ruled in brief Q5; construction-site updates are compile-forced (brief Q8/Q9). Nothing else unnamed is edited.
- No ticket-scope change and no constitution departure (brief Q10): no owner structural checkbox; Gate 1 stays closed until the user merges the spec PR.
- Gates: Q9/Q10 closing bar — Critical/High findings block `$code-review`/`$ship-review`; failed/unrunnable applicable gate, missing axis report, or contract mismatch blocks independently; exit-2 scope-empty is non-blocking SKIPPED, never PASS.

*Re-check after Phase 1 design: Palette may refine presentation within frozen behavior only; product/scope changes return to Patron (brief Q9).*

## Project Structure

### Documentation (this feature)

```text
specs/DEV-321/
├── spec.md              # Feature specification (this plan's input)
├── plan.md              # This file
├── tasks.md             # Phase 2 output
├── clarify.md           # L clarification record (all Q1–Q10 closed, no open items)
├── checklists/
│   └── requirements.md  # L requirements checklist
├── brief.md             # Decision-complete Patron brief (authority)
├── CONCLUSIONS.md       # Patron rulings Q1–Q10 (append-only)
└── ASSUMPTIONS.md       # Taste assumptions (tagged [assumed])
```

### Source Code (repository root)

```text
src/
├── LamuFlix.Core/
│   ├── Library/            # MovieQuery, Page, MovieQueryString (UNCHANGED), MovieSort, SortDirection
│   ├── Domain/             # EnrichmentStatus (existing enum, unchanged)
│   └── Ports/              # MovieSummary.cs (WIDEN: add 5 members)
├── LamuFlix.Infrastructure/
│   └── Persistence/        # EfMovieCatalog.cs (WIDEN same-row projection)
└── LamuFlix.Api/
    ├── Endpoints/          # LibraryResponses.cs (WIDEN DTO+mapping), LibraryEndpoints.cs (unchanged)
    └── Program.cs          # CONDITIONAL: EnrichmentStatus-scoped schema transformer only

tests/
├── LamuFlix.UnitTests/Features/Library/BrowseMoviesQueryHandlerTests.cs  # compile-forced
└── LamuFlix.IntegrationTests/
    ├── LibraryEndpointsTests.cs        # wire shape + fake construction
    ├── ApiEndToEndBrowseTests.cs       # full-stack grid proof
    ├── EfMovieCatalogBrowseTests.cs    # projection proof (4 statuses, populated/null)
    ├── EfMovieCatalogPagingTests.cs    # SQL-shape assertion update (still no JOIN)
    ├── OpenApiContractTests.cs         # schema/status/nullability + baseline drift
    └── Snapshots/...verified.json      # regenerated together with baseline

web/
├── src/
│   ├── App.tsx / main.tsx / index.css / App.test.tsx  # existing entry (replace/reuse)
│   ├── api/                # openapi.json + types.ts via pipelines (never hand-edited)
│   └── library/            # NEW feature folder: LibraryView, MovieGrid, MovieCard,
│                           # LibraryFilters, Pagination, LibraryErrorBoundary,
│                           # useBrowseMovies, libraryUrl, useLibraryUrl + adjacent tests
├── package.json + lockfile # add @tanstack/react-query only
└── test/server.ts, setup.ts # existing MSW/RTL setup reused
```

**Structure Decision**: Ports-and-adapters backend with feature-organised Core (Constitution §I) plus feature-local SPA folder, per brief Q8 — no new project, top-level folder, or architectural layer. (PRODUCT.md §1 uses "vertical slice" wording; Constitution terms govern here without editing PRODUCT.md or changing the architecture.)

## Complexity Tracking

No constitution violations to justify; nothing tracked here.
