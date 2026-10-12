# Implementation Plan: DEV-323 Movie Details view

**Branch**: `feature/323-spec` | **Date**: 2026-10-12 | **Spec**: `specs/DEV-323/spec.md`

**Input**: `specs/DEV-323/spec.md`, frozen `brief.md`, `CONCLUSIONS.md` (Q1-Q12 rulings), `ASSUMPTIONS.md` (taste), `specs/PRODUCT.md`.

## Summary

Extend the GET movie-details response additively with poster, RT/Metacritic, directors, and cast over already-stored fields; regenerate the OpenAPI baseline/snapshot plus DEV-320-generated TypeScript; deliver a feature-local `MovieDetailsView` with load states and feature-gated Play handling, mounted from `App.tsx` at the initial `/movies/<id>` path with no router.

## Technical Context

**Language/Version**: C# on .NET 10 (backend); TypeScript/React 19 (web). Node version, TypeScript strictness, and CI environment are Phase B Wisp environment recon before dependent environment claims.

**Primary Dependencies**: Existing ASP.NET Core slices, EF Core catalog, xUnit; web Patron stack (TanStack Query, Zod, delivered DEV-320 openapi-fetch client, generated MSW handlers), RTL, Vitest, MSW (constitution:313-335, constitution:470-472). DEV-323 adds no new NuGet dependency and no competing codegen npm dependency/script; codegen tooling stays DEV-320 owned. Exact pins and consuming configuration are deferred to Phase B recon, never invented here.

**Storage**: No schema or migration change. Projection reads already-stored poster/rating/director/actor fields and joins (recon-DEV-323:58).

**Testing**: xUnit backend (real PostgreSQL integration, endpoint mapping, e2e frozen shape, contract regeneration); Vitest + RTL + MSW web (generated contract types, Zod validation, generated MSW handlers in fixtures, operation-scoped overrides).

**Target Platform**: Local dev; web served as existing SPA scaffold. CI/hosting details are Phase B Wisp environment recon before dependent environment claims.

**Project Type**: Web application (backend API plus React frontend).

**Performance Goals**: N/A beyond single-shot test/build termination.

**Constraints**: Additive-only DTO change; route and status codes unchanged; `types.ts` generated only; no `main.tsx`/`vite.config.ts`/global `index.css` edits; no playback backend, flag, secrets, legacy MVC, or new-layer edits.

**Scale/Scope**: 4 new web feature files plus named existing-file edits in `brief.md` plan-decisions section; M size, `ui:` ticket.

## Constitution Check (principle numbers per constitution:529)

- Ports and Adapters / Explicit Handlers (Principle I, constitution:93; Principle II, constitution:128): change stays inside existing Core/Infrastructure/Api boundaries; no new project, layer, repository, or mediator.
- Typed Query Model / Enrichment (Principle III, constitution:147; Principle IV, constitution:169): no query-model or enrichment-transition change; projection only.
- Validated Inputs / Consistent Errors (Principle V, constitution:196): routes, DTO nullability (nullable scalars, non-null people arrays), and status codes preserved; GET 404 and Play 204/400/403/404/422/5xx semantics unchanged.
- API and Contract Rules (unnumbered constitution section, constitution:461): C# DTO to `openapi.json` to generated `types.ts` chain honored; no hand-rolled web DTO duplicates; regeneration through the existing facility (constitution:470-472).
- Frontend stack (unnumbered constitution section Technology Stack Constraints, constitution:311): TanStack Query, Zod, delivered DEV-320 openapi-fetch client, and generated MSW handlers per the Patron amendment; no native-fetch/no-query instruction remains (constitution:313-335, constitution:470-472).
- LocalPlay / secrets / Process.Start (Principle VII, constitution:241; secrets constitution:243-244; LocalPlay + ProcessStartInfo.ArgumentList constitution:253-255): untouched; server-side adapter stays authoritative; no flag endpoint.
- Test Pyramid (Principle IX, constitution:282): xUnit plus Vitest/RTL/MSW only; no second assertion/mocking stack; no data-driver mocks for query translation or serialization.
- Ubiquitous Language (Principle VIII, constitution:262): Movie Details and Playback per Q12; no `CONTEXT.md` edit; no ADR (M ticket, no new layer).

*Re-check after Phase 1 design: scope, file set, and dependency deferral unchanged from Q1-Q12.*

## Project Structure

### Documentation (this feature)

```text
specs/DEV-323/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── get-movie-details.md
│   └── post-play.md
└── tasks.md             # Phase 2 output (created by /speckit-tasks)
```

### Source Code (repository root)

```text
src/LamuFlix.Core/Domain/MovieMetadata.cs
src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs
src/LamuFlix.Api/Endpoints/LibraryResponses.cs
tests/LamuFlix.IntegrationTests/EfMovieCatalogDetailsTests.cs
tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs
tests/LamuFlix.IntegrationTests/ApiEndToEndLibraryTests.cs
tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs (regeneration facility only; source edits not preauthorized)
web/src/api/openapi.json (regenerated)
tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json (regenerated)
web/src/api/types.ts (generated via delivered DEV-320 tooling only)
web/src/features/movie-details/MovieDetailsView.tsx
web/src/features/movie-details/MovieDetailsView.test.tsx
web/src/features/movie-details/movieDetailsApi.ts
web/src/features/movie-details/MovieDetailsView.css
web/src/App.tsx (Q5 fallback mount)
web/src/App.test.tsx (entry tests)
```

**Structure Decision**: Existing vertical slices plus one feature-local web folder; no new project, top-level folder beyond the feature folder, or state/router/client layer. File inventory matches brief.md:36-48; Wisp first identifies exact forced tracked paths and evidence, then the decision is recorded before tasks expand. Any Core constructor/test caller adjustments beyond this list are deferred to Phase B Wisp recon, never invented here; preserve main.tsx/vite.config.ts/global index.css exclusions and do not invent provider paths or pins.

## Phase B Pickup and Verification (brief gate expectations and task ordering)

- Prerequisite: Phase B starts only after the spec PR is merged and the original owner checkbox is answered; owner no requires re-plan before Gate 1. Phase B then starts with Wisp drift recon on the approved spec against the current build base, verifying delivered DEV-320 codegen tooling plus TanStack Query/Zod integration before typed web implementation, plus targeted Wisp direct-link hosting recon before dependent deployment claims; do not solve hosting in Phase A or assert direct-link support at this HEAD. Absent tooling is needs recon; dependent build work stops until resolved.
- Order (strictly sequential): additive Core data, then EF projection, then API mapping and frozen-shape tests; regenerate OpenAPI/snapshot, then generated types with delivered tooling; frontend implementation consumes generated fields through the DEV-320 openapi-fetch client, TanStack Query, and Zod with web-implement TDD; mount through App and prove entry behavior. No hand-written substitute types while waiting for codegen.
- Gauge verification per pipeline Phase 3 step 5 after Cog refactor: final gates are Gauge-owned (Roslyn, complexity, InspectCode in changed scope, mutation for changed production C#, property tests, vulnerability audit, `dotnet format` verification, `dotnet test`, web gates). No thresholds changed; scope-empty SKIPPED, configured SKIP, and mutation NOT APPLICABLE stay distinct non-blocking verdicts, never PASS; exit 1 or Could not run blocks; unaccepted property-test exit-2 remains blocking.
- Loop discipline: closing bar, two review rounds with at most two fix commits each, posted/read-back findings, awaiting-merge end state; nobody merges or enables auto-merge.

## Complexity Tracking

No constitution violations to justify. No new project/layer, no repository/mediator wrapper, no migration, no route/status change.
