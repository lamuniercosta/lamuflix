# Implementation Plan: Persist TMDb Collections and Collection Facet Filter

**Branch**: `feature/424-spec` | **Date**: 2026-10-28 | **Spec**: `specs/DEV-424/spec.md`
**Input**: Frozen `brief.md` (Q1–Q8 + C1 + Phase 2 step 6 @ `7a335ac3` + F7 amendment @ `10ffc9c1`); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; `CONTEXT.md`; Maestri notes `DEV-424`, `recon-DEV-424`, `recon-DEV-424-2`; `specs/PRODUCT.md`; `.specify/memory/constitution.md`
**Status**: PLAN FROZEN — Keel Phase 2 step 6 adjudication; see PLAN_CHALLENGE.md. Gate 1 remains closed.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — every technical choice is ruled in the brief (Q1–Q8), and the public API shape is fixed under §2.3 item 4 (Q3). Contract chain remains C# DTO → regenerated OpenAPI → generated TypeScript. Precedent: DEV-423 / DEV-331 (fold decisions into plan; no parallel research packet).

## Summary

Persist optional TMDb collection membership per movie (`CollectionRecord` + nullable `MovieRecord.CollectionId`), expose a genre-mirrored `GET /api/collections` facet, add browse `collectionId` filtering through `MovieQuery`/codecs, regenerate OpenAPI/TypeScript, and add Filter Panel single-select Collection chips bound to URL key `collectionId`. Extend the existing TMDb mapping and `MovieMetadata` into the existing enrichment write path (DEV-331 deferred collections here). No backfill, grid grouping, new dependency/layer, cleanup job, or ADR.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (`TreatWarningsAsErrors=true`, nullable enabled, CPM); TypeScript/React 19 in `web/` via Vite.
**Primary Dependencies**: Existing pins only — EF Core, existing OpenAPI generation (`npm run generate:api` / `openapi-typescript`), existing web query client. **No new NuGet/npm packages** (Q1; §2.3 item 1). `dotnet-ef` already in `.config/dotnet-tools.json`.
**Storage**: PostgreSQL via existing EF Core; new `collections` table + nullable FK column on `movies` only (ticket-named tables; Q1/Q2).
**Testing**: xUnit v3, Shouldly, FsCheck property conventions where already used for `MovieQuery`; Testcontainers for EF translation/serialisation/migration; NSubstitute only where existing facet endpoint tests mock ports (not the data-access driver for EF behaviour); Vitest for web. No live TMDb.
**Target Platform**: .NET ASP.NET Core Minimal API + SPA.
**Project Type**: Ports and adapters with feature-organised Core across Core / Infrastructure / Api / web — no new project or layer (Q1; constitution Principle I). Not vertical-slice.
**Performance Goals**: Match existing genre facet and browse latency class; no new SLA invented.
**Constraints**: No new explanatory comments; preserve unrelated comments; `CancellationToken` end to end; complexity ≤ 15 then refactor gate from `harness.yml`; never hardcode/lower thresholds; generated contracts never hand-edited; do not rename `HasSameCollectionValues` / `SameCollections`; Palette owns visual layout.
**Scale/Scope**: Personal library; M-sized extension of settled genre/filter patterns; DEV-358 depends on this delivery.

## Constitution Check

Gates checked against `.specify/memory/constitution.md`. DEV-424 extends existing records, repository, facet, query, and chip patterns. Q8 ruled no ADR. **No constitution departure**: analyze C1 resolved by `CONCLUSIONS.md` C1 (commit `2754a161`) — malformed/nonpositive/repeated `collectionId` returns **422** ProblemDetails under Principle V; no owner checkbox.

| Principle | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Ports in Core; adapters in Infrastructure; feature-organised Core — not VSA | Extend existing `IMovieCatalog` / `IMetadataProvider` surfaces; add collection facet query siblings beside genres in feature folders — no new port family requiring ADR (Q3, Q8). |
| II (Explicit Handlers and Decorators) | Sealed record queries; sealed handlers; `AddHandler` decorator chain Tracing → Logging → Validation → handler | `GetCollectionsQuery` is a sealed record; `GetCollectionsQueryHandler` is sealed; register via existing `services.AddHandler<…>` beside genres with **no empty `GetCollectionsQueryValidator`** (mirrors genres; step 6 ruling 5). recon-DEV-424-2 lines 39–45 confirm existing `AddHandler` empty-validator set is pass-through — no MediatR/reflection dispatch (Q3, Q8). |
| III (Filters) | Library filters via `MovieQuery` | Add nullable `CollectionId` to existing `MovieQuery`/codecs/`MovieQueryValidator` (null or > 0) (Q3; step 6 ruling 4). |
| IV (Enrichment) | Whole-metadata replacement on success | Collection follows success-replace / failure-preserve and whole-metadata clear including successful non-TMDb fallback clear (Q2, Q5; step 6 ruling 2). |
| V (Validated Inputs / Error Shape) | FluentValidation semantic failures → 422; RFC 7807 ProblemDetails via single `IExceptionHandler` | Raw `string?` browse `CollectionId` via `ReadNumber` + repeated-key detection; empty/`abc`/`0`/`-3`/repeated → **422** ProblemDetails (CONCLUSIONS.md C1; step 6 ruling 4; Principle V). |
| VII (Configuration Isolation) | Validated options; no hardcoded secrets | No new options/secrets; TMDb key path untouched (Q1). |
| VIII (Ubiquitous Language) | Feature glossary | `CONTEXT.md` terms Collection / membership / facet; do not conflate with sequence helpers (Q8). |
| IX (Test Pyramid) | Testcontainers for EF; no mocked driver for translation | Real-DB proofs for migration/upsert/filter/count (Q6). |
| API and Contract Rules | C# DTO → OpenAPI → generated TS | FR-007; Q3 item 4 shape; never hand-edit (brief scope 8). |
| Gates | Roslyn, complexity, InspectCode, format, vulnerable packages, mutation | Phase B/implement stages only; see Gates below. |

### Complexity Tracking

No constitution violation to justify. C1 closed within Principle V (`CONCLUSIONS.md` C1). Empty by design.

## Approach

Follow brief Q6 ordering exactly, as amended by Phase 2 step 6 rulings:

1. **Metadata / TMDb mapping (Q1, Q5; Sentry F7 @ `10ffc9c1`)**: Extend response DTO + `TmdbMetadataProvider.Mapping.cs` and `MovieMetadata` with optional collection id/name. Deliberate `belongs_to_collection` rule: present object with nonpositive id or blank name rejects the **whole** TMDb details response as Failed InvalidResponse through the existing failure seam — not Found with the collection silently dropped; missing/null remains valid absent collection; never persist an invalid or invented row; does not claim all existing optional fields fail wholesale (recon-DEV-424-2 lines 11–20: optional omission normally maps null; title gate fails whole response — new field needs its own explicit rule). Final failed/unavailable enrichment preserves prior metadata/membership; successful non-TMDb fallback still clears under Q5. Focused adapter/domain tests alongside with malformed-present Failed-not-Found and missing/null valid-absent oracles.
2. **Persistence (Q1, Q2, Q5; step 6 ruling 1; recon-DEV-424-2)**: Add `CollectionRecord` with TMDb **int** id as PK (`ValueGeneratedNever`, no surrogate), Name + movies nav; nullable `MovieRecord.CollectionId`; `CollectionConfiguration`; DbContext DbSet; generated migration that asserts natural PK **and** an index on `CollectionId` (Sentry F6). Atomic upsert/replace/clear in `EfMovieRepository` save boundary: on collection unique/PK conflict from concurrent insert, **roll back/reset**, re-read the existing collection row, and **retry the whole atomic save** (collection upsert + membership) across reachable save paths — never partial catch-and-continue; preserve caller transaction ownership; do not invent extra infrastructure or a partial retry within an aborted ambient transaction. **recon-DEV-424-2 evidence** (lines 22–33): existing `SaveMetadataWithRetryAsync`/`IsRetryableConflict` does **not** currently classify collection PK conflicts (only `PK_movies` / `PK_movie_*` join PKs plus serialization/deadlock states), and `RunAmbientMetadataWriteAsync` has **no retry loop** — so collection conflict recovery is required implement-and-prove, not existing proven behaviour. Forced two-movie concurrent same-new-id Testcontainers oracle: one row, both memberships, no surfaced conflict. Explicit fallback-clear + failure-preserves tests; accepted residual until next successful TMDb; no cascade delete; retain orphan rows; no release-note task (step 6 ruling 2).
3. **Catalog / facet / query / API (Q3; step 6 rulings 4–5)**: `CollectionFacet`/`CollectionDto` = `(int Id, string Name, int MovieCount)` — **reject long count**; `GetCollectionsQuery`(+handler, **no empty validator**, unchanged `AddHandler` chain); `IMovieCatalog.GetCollectionsAsync`; `EfMovieCatalog` (omit zero members; order name then id; distinct counts; no active-filter narrowing); `FacetEndpoints` `GET /api/collections`. Browse: `BrowseMoviesRequest.CollectionId` as raw `string?`, `CollectionIdKey` const, `ReadNumber` + repeated-key detection; preserve ReSharper suppression around request properties; `LibraryEndpoints` on complexity watchlist; `MovieQueryValidator` positive-nullable rule; codecs reject invalid; empty/`abc`/`0`/`-3`/repeated → 422.
4. **Contracts (brief scope 8)**: Regenerate OpenAPI + TypeScript via `npm run generate:api`; drift check. Backend suite all green before this handoff (step 6 ruling 6).
5. **Web (Q4; step 6 ruling 3)**: `web-implement` red-first TDD — Genre fieldset/legend + `label.lamu-chip` sr-only **checkbox** single-select toggle (not radio); toolbar-only unknown-id fallback retained through loading/error/empty; legend-only empty (no helper copy); no skeleton/`aria-busy`; plain-text hostile-name and overlong-layout oracles (Sentry F4); URL restore; consume generated types. Palette owns `design.md` layout corrections (do not edit design here).

Preserve existing consumers of `MovieMetadata`/`MovieQuery`. Do not rename sequence helpers.

## Project Structure

```text
specs/DEV-424/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md, CONTEXT.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  checklists/requirements.md
  design.md                                 (Palette-owned; exists — challenge disposition/freeze separate)

src/LamuFlix.Core/
  Domain/MovieMetadata.cs                   (edit: optional collection id/name)
  Library/MovieQuery.cs, MovieQueryString.cs (edit: CollectionId)
  Ports/IMovieCatalog.cs                    (edit: GetCollectionsAsync + CollectionFacet)
  Features/Library/GetCollectionsQuery.cs   (new)
  Features/Library/GetCollectionsQueryHandler.cs (new)

src/LamuFlix.Infrastructure/
  Adapters/TmdbMetadataProvider.Mapping.cs  (edit: response DTO + mapping)
  Persistence/Records/CollectionRecord.cs   (new)
  Persistence/Records/MovieRecord.cs        (edit: CollectionId + nav)
  Persistence/LamuFlixDbContext.cs          (edit: DbSet)
  Persistence/Configurations/CollectionConfiguration.cs (new)
  Persistence/Configurations/MovieConfiguration.cs (edit: FK)
  Persistence/Migrations/*                  (generated)
  Persistence/EfMovieRepository.cs          (edit: upsert/membership)
  Persistence/EfMovieCatalog.cs             (edit: GetCollections + browse)
  Persistence/MovieRecordQueryExtensions.cs (edit: WhereCollection)
  Library/*QueryValidator*.cs               (edit: MovieQueryValidator positive-nullable CollectionId)

src/LamuFlix.Api/
  Endpoints/FacetEndpoints.cs               (edit: /collections)
  Endpoints/CollectionDto.cs                (new; int Id, string Name, int MovieCount)
  Endpoints/LibraryEndpoints.cs             (edit: CollectionIdKey/ReadNumber/repeat; complexity watchlist)
  Endpoints/BrowseMoviesRequest.cs          (edit: string? CollectionId; preserve ReSharper pair)
  HandlerRegistration.cs                    (edit: AddHandler unchanged pattern; no empty validator)

web/src/
  api/openapi.json, api/types.ts            (regenerated)
  features/library/FilterPanel.tsx, filterQuery.ts
  library/libraryUrl.ts, browseParams.ts, LibraryView.tsx
  mocks/handlers.ts, fixtures.ts

tests/ + web tests                          (siblings per brief test strategy)
```

**Layout decisions**: Mirror genre chain file-for-file; new siblings rather than unrelated rewrites; generated migration only via `dotnet ef`; no research/data-model/contracts packets.

## Test Strategy

| Surface | Coverage |
|---|---|
| Adapter/domain | Mapping with valid collection; missing/null → valid absent; present malformed (nonpositive id / blank name) → Failed InvalidResponse not Found (F7 @ `10ffc9c1`); metadata equality includes new optional identity without renaming `SameCollections` |
| Repository (Testcontainers) | Upsert/name update; **forced** two-movie concurrent same-new-id race → one row + both memberships + no surfaced conflict; replace; explicit fallback-clear + failure-preserves; old rows null; no cascade loss; migration natural-PK + `CollectionId` index assertions; extend `EfMovieRepositorySurvivorTests` |
| Catalog/facets | Populated/empty; distinct **int** counts; orphan exclusion; name-then-id order |
| Browse/query | AND composition; paging; unknown id empty page; 422 for empty/`abc`/`0`/`-3`/repeated; equality/hash/codec + `MovieQueryValidator`; extend `MovieQuerySurvivorTests` / `MovieQueryStringSurvivorTests` |
| API | `FacetEndpointsTests` collections route; `LibraryEndpointsTests` collectionId |
| Contracts | Backend green first; then `npm run generate:api` + `npm run check:api` |
| Web (red-first) | fieldset/legend + sr-only checkbox toggle; select/replace/clear/clear-all; page reset; URL restore; toolbar-only unknown-id fallback retained on loading/error/empty; legend-only empty; no skeleton/`aria-busy`; plain-text hostile + overlong name oracles |

Backend tests written **alongside** each backend task; suite all green before T020. Web remains red-first. No Gherkin unless opted in. Property tests follow existing `MovieQuery` conventions; report SKIP/SKIPPED/N/A distinctly.

## Gates

At pipeline stages (not during this drafting ask):

1. Backend: Roslyn analyzers, cyclomatic complexity (then refactor threshold), InspectCode on changed `.cs`; `dotnet format --verify-no-changes`; affected then full `dotnet test`; property/security/vulnerable-package gates per harness.
2. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from a worktree; never lower threshold; surviving mutant = missing test.
3. Web: lint, API drift, tests, build via web-implement workflow.
4. Size M review: Sentry, Ledger, Compass mandatory; closing bar Critical/High/Medium block; Low explicit disposition; every finding recorded; frozen scope per Q7; hard two remediation rounds then block to Conductor.
5. Exit 2: SKIPPED (scope-empty) / SKIP (opt-out) / mutation NOT APPLICABLE never folded into PASS; Could not run is not PASS.

Phase A drafting runs none of these gates. Gauge baseline note DEV-424:64–84 is evidence only.

## Risks

- **Challenge disposition/freeze pending**: `design.md` exists; disposition and Gate 1 remain separate — do not claim freeze; Palette owns further design.md corrections.
- **Whole-metadata clear on successful fallback**: successful non-TMDb result without collection clears membership (Q5; step 6 ruling 2) — accepted residual until next successful TMDb; explicit fallback-clear + failure-preserves tests; no release-note task.
- **Natural-key concurrency retry**: required whole-save rollback/reset/re-read/retry across reachable save paths is implement-and-prove; recon-DEV-424-2 lines 22–33 show existing retry does not classify collection PK conflicts and ambient metadata writes have no retry loop; preserve caller transaction ownership; no partial retry inside an aborted ambient transaction; no invented extra infrastructure (step 6 ruling 1); T010/T011 keep the concrete obligations; T011 must force the race.
- **Sentry F7 `belongs_to_collection`**: closed by Patron ruling @ `10ffc9c1` — present malformed → whole-response Failed InvalidResponse; missing/null valid absent; final failure preserves; successful non-TMDb fallback still clears; not a wholesale optional-field fail claim.
- **Dormant orphan rows**: retained by design (Q2); facets hide zero-member rows; orphan telemetry/cleanup out of scope (Sentry F5 — noted no fix).
- **LibraryEndpoints complexity**: ReadState already large; T017/T028 watchlist may force splits at refactor gate.
- **Naming collision**: `HasSameCollectionValues` / `SameCollections` must not be repurposed or renamed (Q8; recon).
- **Generated contract drift**: hand-edits forbidden; always regenerate (brief scope 8).
- **Phase B dependency drift**: recheck DEV-331/DEV-322 landing and contracts at Phase B pickup; route missing facts as needs recon to Conductor (brief handoff).

## Ordering

See `tasks.md`: metadata tests alongside impl → persistence/migration/repository (tests alongside; concurrency oracle) → catalog/facet/query/API (tests alongside; backend green) → regenerate contracts → web red-first TDD → verification/review. Same-file edits sequenced; `[P]` only across different files with no incomplete deps.

