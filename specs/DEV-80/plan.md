# Implementation Plan: DEV-80 Folder Import Dialog and Smart Polling

**Branch**: `feature/080-spec` | **Date**: 2026-10-12 | **Spec**: `specs/DEV-80/spec.md`

**Input**: Feature specification from `specs/DEV-80/spec.md`, frozen `brief.md` (Q1-Q7, R1-R4, A1-A6), `recon-DEV-80` and `recon-DEV-80-3`

## Summary

Carry the existing persisted enrichment status through the backend summary port record, the `EfMovieCatalog` projection, and the browse response mapping into the committed OpenAPI baseline and Verify snapshot; generate and commit the TypeScript client and wire TanStack Query as the sole cache layer; then implement the minimum library list, import dialog with cache prepend, conditional 2-second polling, and NotFound/Failed retry, following the brief ordering: status contract first, then generated client and provider, then list/dialog/poll/retry, then tests and gates. The backend contract/projection and generated-client/provider foundations pass tests independently; the first combined US1/US2 frontend story handoff completes after T017 with `App.test.tsx` and `ImportDialog.test.tsx` passing, followed by the polling and retry handoffs with dependencies and typed MSW fixtures. MSW handlers are typed through `openapi-msw` using generated paths.

## Technical Context

**Language/Version**: C# with .NET 10; web with React 19, TypeScript strict, and Vite

**Primary Dependencies**: Existing ASP.NET Core Minimal API surface (no new route); approved npm names per brief.md Q1-Q3 (brief.md:73-94) with no version pinned in this plan: `@tanstack/react-query` (runtime, sole client state/cache layer), `openapi-fetch` (runtime HTTP typed by generated paths), `openapi-typescript` (dev, `generate:api` script writing `web/src/api/types.ts` from `web/src/api/openapi.json`), `openapi-msw` (dev, test handlers derived from generated paths); Zod is not added

**Storage**: Existing persisted status column (`MovieRecord.Status`); no schema change or migration

**Testing**: Backend xUnit with the repo's existing assertion and mocking libraries and conventions; Verify snapshot plus `OpenApiContractTests`; frontend Vitest with React Testing Library and MSW; no new test library

**Target Platform**: Linux server API plus Vite SPA served against the existing `DevSpa` CORS policy with exposed `Location`

**Project Type**: Backend contract slice plus web feature slice (no new project or layer)

**Performance Goals**: Poll only while a Pending item is in the current browse data (2 s interval, no background refetch); no other goal beyond existing gate behavior

**Constraints**: `MovieSummaryResponse` gains exactly `enrichmentStatus` serialized by name (`Pending|Enriched|NotFound|Failed`); browse first page only via the standing wire query `sort=Title&direction=Ascending&page=1&pageSize=20` (approved taste A6 per brief R3); the committed OpenAPI browse-query metadata for those four keys is corrected by the approved operation transformer (brief R4, recon-DEV-80-3:22-27) with no binding, validation, route, runtime, or UI change; `TimeProvider` for time and injected seeded `Random` for randomness; no explanatory comments

**Scale/Scope**: One DTO field carried through port, projection, response mapping, OpenAPI baseline, Verify snapshot, generated types, provider, list, dialog, polling, retry, and their tests; file envelope per brief.md delivery touch set

## Constitution Check

- [I] Ports and adapters with a feature-organised core (constitution.md:93-126): the status field flows through the existing `Core/Ports/` seam (`MovieSummary`) and the `Infrastructure` projection/endpoint mapping; no new port, no new project, no new architectural layer; `LamuFlix.ArchitectureTests` must pass (Pull Request Quality Gates, constitution.md:350).
- [III] Typed query model (constitution.md:147-163): browse keeps the existing typed query path with no reflection-based or string-named sort; server browse order is kept client-side with no re-sort. The server requires lowercase wire keys `sort,direction,page,pageSize` with `page=1`, case-sensitive `Title|Year|Rating|Runtime` and `Ascending|Descending`, `pageSize` 1..100; omission returns 422 (recon-DEV-80:10.3, recon-DEV-80-3:22-27). `src/LamuFlix.Api/BrowseMoviesOperationTransformer.cs` (`IOpenApiOperationTransformer`, applied to `BrowseMovies` only, registered via `AddOperationTransformer` beside the schema transformer in the existing `AddOpenApi` options in `src/LamuFlix.Api/Program.cs` per brief R4) corrects the document metadata only: the four parameters are named `sort`/`direction`/`page`/`pageSize`, each `Required = true`, `sort` string enum `Title|Year|Rating|Runtime`, `direction` string enum `Ascending|Descending`, `page` integer minimum 1, `pageSize` integer 1..100; the other ten parameters, binding, validation, route, runtime, and UI are unchanged. The historical baseline defect (optional PascalCase string properties not typing these keys) is superseded by the corrected contract; the client and handlers consume the corrected generated query types and send/assert the A6 standing values `sort=Title&direction=Ascending&page=1&pageSize=20` with no handwritten DTOs, casts, or stale PascalCase workaround; no sort UI or paging controls are added.
- [IV] Enrichment state machine (constitution.md:169-190): `enrichmentStatus` is the `EnrichmentStatus` SmartEnum (`src/LamuFlix.Core/Domain/EnrichmentStatus.cs:5-10`: `Pending|Enriched|NotFound|Failed`) serialized by name via the project-local `EnrichmentStatusJsonConverter` (API and Contract Rules, constitution.md:468); numeric values never cross the API; illegal Pending retry maps to 409 via the single `IExceptionHandler`. The generator does not consult that converter, so the four-name string shape is emitted by the approved `src/LamuFlix.Api/EnrichmentStatusSchemaTransformer.cs` (`IOpenApiSchemaTransformer`, scoped to `EnrichmentStatus` only, emitting `type: string` with `enum [Pending, Enriched, NotFound, Failed]`), registered via `AddSchemaTransformer` in the existing `AddOpenApi` options in `src/LamuFlix.Api/Program.cs` (brief R2, recon-DEV-80:10.1); both paths extend the brief.md:34-38 touch set per brief:45.
- [V] Validated inputs and consistent errors (constitution.md:196-213): folder-path validation returns 422 through the validation decorator; every error surface is RFC 7807 `ProblemDetails` with `traceId` from the single handler (Pull Request Quality Gates, constitution.md:361-362).
- [VII] Configuration and deterministic time (constitution.md:241-257): no secret, connection string, machine path, or `IConfiguration` magic string is added; `TimeProvider` for time; new options records, if any, validated at startup (Pull Request Quality Gates, constitution.md:365-367).
- [IX] Test pyramid with real infrastructure (constitution.md:282-309): xUnit v3 with Shouldly, NSubstitute, AutoFixture/Faker.Net; persistence/API coverage via Testcontainers and Verify; no EF Core InMemory, no Moq, no second assertion or mocking library (Pull Request Quality Gates, constitution.md:370-371).
- [API contract] C# DTO to committed `web/src/api/openapi.json` to generated `web/src/api/types.ts` (API and Contract Rules, constitution.md:470-473); web HTTP and MSW handlers derive from the generated paths, never hand-written; the regenerated baseline and Verify snapshot are committed through the opt-in regeneration test `OpenApiContractTests.OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` run once with `LAMUFLIX_REGENERATE_OPENAPI=true` (then the switch is removed), writing baseline and verified snapshot together per recon-DEV-80:10.4, with the independent drift check green and no manual snapshot acceptance or generated-file edits (Pull Request Quality Gates, constitution.md:359-360). Proof: the generated `enrichmentStatus` schema is `type: string` with `enum` exactly `[Pending, Enriched, NotFound, Failed]`; `OpenApiContractTests` also proves the four browse-query parameter names, parameter-level required flags, enum sets, and integer bounds, with the operation set and unrelated parameters unchanged.
- [Quality gates] Stage-appropriate web gates (`scripts/run-web-gates.ps1`) and backend gates for the touched C# files; new or modified `.cs` files pass all three static-analysis gates (constitution.md:383-387) plus `dotnet format --verify-no-changes` (Pull Request Quality Gates, constitution.md:375-376); PASS, SKIPPED (scope-empty), configured SKIP, mutation N/A, and Could not run stay distinct and thresholds are never lowered.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-80/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── LamuFlix.Core/
│   └── Ports/MovieSummary.cs (carry status in the summary port record)
├── LamuFlix.Infrastructure/
│   └── Persistence/EfMovieCatalog.cs (project the existing persisted status)
├── LamuFlix.Api/
│   ├── EnrichmentStatusSchemaTransformer.cs (approved R2 schema transformer, EnrichmentStatus only)
│   ├── BrowseMoviesOperationTransformer.cs (approved R4 operation transformer, BrowseMovies only)
│   ├── Program.cs (existing AddOpenApi options gain AddSchemaTransformer + AddOperationTransformer)
│   └── Endpoints/
│       ├── LibraryResponses.cs (MovieSummaryResponse gains enrichmentStatus)
│       └── LibraryEndpoints.cs (browse response mapping exposes the status)
web/
├── package.json (+ approved deps and generate:api script; no version pinned here)
├── package-lock.json
├── src/
│   ├── api/
│   │   ├── openapi.json (regenerated baseline)
│   │   ├── types.ts (committed openapi-typescript output; never hand-edited)
│   │   └── client.ts (openapi-fetch client typed by generated paths)
│   ├── library/
│   │   ├── LibraryView.tsx (minimum list: title, badge, eligible retry)
│   │   ├── ImportDialog.tsx (folder-path dialog with inline errors)
│   │   └── libraryQueries.ts (browse query, import/retry mutations, poll interval)
│   ├── App.tsx (library screen host)
│   ├── App.test.tsx (library screen behavior coverage)
│   └── test/handlers.ts (openapi-msw handlers from generated paths)

tests/
├── LamuFlix.IntegrationTests/
│   ├── OpenApiContractTests.cs (generated status contract expectations: `enrichmentStatus` is `type: string` with `enum` exactly `[Pending, Enriched, NotFound, Failed]`; plus the four browse-query parameter names, required flags, enum sets, and integer bounds; owns schema/baseline/snapshot proof for both)
│   ├── Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json
│   ├── LibraryEndpointsTests.cs (RecordingMovieCatalog DTO/HTTP mapping proof only; cannot prove EfMovieCatalog)
│   ├── EfMovieCatalogBrowseTests.cs (real Postgres/Testcontainers projection proof)
│   └── ApiEndToEndBrowseTests.cs (end-to-end item exact-shape proof over HTTP against real Postgres)
└── LamuFlix.UnitTests/Features/Library/BrowseMoviesQueryHandlerTests.cs (positional-record constructor update)
web tests under web/src/library/ plus the rewritten App.test.tsx
Constructor inventory is bounded to the recon roots (recon-DEV-80:10.2, 10.5), not an exhaustive repo claim.
```

**Structure Decision**: The constitution ports-and-adapters architecture with a feature-organised core (constitution.md:93-126) and the existing web layout are kept; the status field flows through the named port, projection, and endpoint files, and the web side adds only the client, query, dialog/list, and test modules above under the approved single state/cache layer, with `Palette design.md` awaited before the UI plan challenge.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | No constitution departure; Q1-Q6 rulings carry the dependency, layer, and API-shape items | N/A |
