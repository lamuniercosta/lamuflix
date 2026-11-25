# Implementation Plan: Outside-library discovery on For You

**Branch** — `feature/561-spec` | **Date** — 2026-11-24 | **Worktree** — `F:/Dev/LamuFlix.worktrees/feature-561-spec`

**Status** — PLAN FROZEN after the Size L plan-plus-ADR challenge. Ready for owner Gate 1. The spec PR merge is the Gate 1 receipt. Phase B pickup, implementation, and runtime acceptance follow separately. See [the challenge dispositions](brief.md#phase-2-step-6-adjudication).

**Authority** — DEV-561 ticket (Wanted, Done when, Out of scope, Open points, Where it lands) as amended by Q3 Option A; `brief.md` Q1–Q12; `CONCLUSIONS.md`; `ASSUMPTIONS.md`; `spec.md`; Maestri `recon-DEV-561`; `specs/PRODUCT.md`; Accepted `docs/adr/0049-for-you-outside-library-discovery.md`; `.specify/memory/constitution.md`.

## Summary

Add a separate read-only **Not in your library yet** row on For You. Persist title/year/poster on existing DEV-539 recommendation links, hydrate via enrichment/backfill, rank locally with Liked/Watched weights and a two-seed bar under Option A identity rules, expose `GET /api/recommendations/not-in-library`, regenerate OpenAPI/TS, compose an independent web section with TMDb credit, and update PRODUCT/CONTEXT/README plus home-library acceptance evidence.

## Technical context

- **Stack** — .NET 10 (`LamuFlix.Core`, `LamuFlix.Infrastructure`, `LamuFlix.Api`); EF Core / PostgreSQL; existing TMDb typed client and resilience; React/Vite For You feature; xUnit + Testcontainers (+ existing WireMock patterns where provider mapping needs them); web Vitest/RTL and local E2E.
- **Scale** — Personal library; discovery caps at 20 cards; ranking is local set-based over persisted links; enrichment/backfill keep the existing first-page request budget (no per-candidate identity calls).
- **Boundaries** — ADR 0049 Accepted amending ADR-0011. DEV-539 owns the link table and backfill mode this ticket extends. No new package/project/layer, no LocalPlay/secrets/`Process.Start`, no Related outside movies, no Not interested.

## Constitution check

Gates checked against `.specify/memory/constitution.md` (worktree current). Constitution compliance requires each constitution-gate evaluation to cite the principle number.

| Principle | Gate | Plan response |
|---|---|---|
| I — Ports and adapters | Named ports; Core isolation | Reuse existing TMDb adapter, movie repository/persistence, and recommendation feature seams. Extend `IRecommendationCandidateSource` with one set-based read returning a Core-owned `OutsideLibraryRecommendationSnapshot`. Implement it in `EfRecommendationCandidateSource` and preserve the existing library-only Crowd read. |
| II — Explicit handlers | Handlers + AddHandler | New sealed query record + sealed handler under Features/Recommendations; register via existing `AddHandler` decorator path (Tracing → Logging → Validation → handler). Backfill remains the existing DEV-539 operator command. |
| III — Typed query model | MovieQuery | N/A for browse filtering. Discovery returns its own card DTO array from Core Recommendations. |
| IV — Domain transitions | Movie mutators | No new Movie lifecycle. Link metadata rides existing enrichment/backfill persistence. |
| V — Validated inputs and errors | ProblemDetails / options | Use the existing Validation decorator with the parameterless query; omit an empty validator class; shared RFC 7807 `ProblemDetails` for error paths; bind discovery weights in `RecommendationOptions` with existing options validation enforcing integer Liked > Watched > 0; keep empty HTTP 200 `[]` for unconfigured TMDb / no eligible candidates (not an error); existing `/api` auth convention. |
| VI — Observability | Safe diagnostics | Home-library acceptance counts/verdicts are sanitized delivery evidence; never log ApiKey or raw personal seed dumps in the PR. |
| VII — Configuration | Options; LocalPlay; Process.Start | Extend `RecommendationOptions` for Liked/Watched discovery weights. Reuse `TmdbOptions` gates. No LocalPlay, secrets, or `Process.Start`. |
| VIII — Ubiquitous language | Constitution VIII | Update the `CONTEXT.md` Recommendation entry and add **Outside-library recommendation**. Q8 and Wanted 2 authorize the separate `specs/PRODUCT.md` Positioning update. |
| IX — Test pyramid | Meaningful layers | Provider/mapping tests, real-Postgres migration/backfill, domain/property ranking pins, API zero-HTTP integration, web + local E2E, home-library receipt. |

### API and contract rules

Public route and DTO field set are ticket/brief-authorized (`GET /api/recommendations/not-in-library`; card fields per Q4). Place the response DTO in Core Recommendations. Endpoint uses `TypedResults` / `Results<…>` unions. Cover with `WebApplicationFactory` integration tests. Regenerate and commit the OpenAPI document at `web/src/api/openapi.json` and generated TypeScript; satisfy the constitution Verify OpenAPI snapshot / drift obligation (committed snapshot, Verify snapshot test, CI drift fails the build) and keep drift green. Existing For You contract stays unchanged (constitution Development Workflow → API and Contract Rules). Keep empty HTTP 200 `[]` behaviour.

### Static-analysis gates

Size L delivery revision must pass Roslyn, CA1502 including the refactor threshold configured in `harness.yml`, InspectCode, mutation, property tests, vulnerable packages, format, .NET tests, and web gates with honest exits. Report exit-2 statuses on their own terms: scope-empty → `SKIPPED (scope-empty)`; mutation with every changed project excluded → `NOT APPLICABLE` / N/A; harness-disabled gate → `SKIP`. Failure or `Could not run` blocks. Never fold non-pass into PASS; never lower thresholds.

## Approach

1. **Pre-merge Gate 1 sequence.** Keel completed analyze and the Size L plan-plus-ADR challenge. Provisional Gate 1 follows the challenge disposition. Owner merge of the Spec Kit PR (Accepted ADR 0049 already present) is the Gate 1 receipt. Review caps (two rounds / two fix commits) apply to the delivery revision, not Gate 1. Phase B pickup is after that merge and verifies DEV-539 Done, ADR Accepted, column facets, the named port and EF read path, existing recommendation-ID index, OpenAPI Verify snapshot and CI drift check, enrichment contract flow, and implementation/test seams; missing facts go to recon.
2. **Schema and mapping.** Additive nullable title/year/poster on `MovieRecommendationRecord` + EF configuration + migration (EF stays in Infrastructure). Extend TMDb first-page mapping and enrichment/backfill writers through existing enrichment-contract seams; hydrate legacy ID-only rows; use the same first-page typed client and existing resilience for initial runs and reruns. Publish each successful per-source link replacement in one transaction after fetching. A failed fetch or write leaves prior rows intact. Test reader visibility during replacement and preserve ID-only Crowd consumers.
3. **Local discovery query.** The parameterless query handler orchestrates the set-based read and delegates to cohesive pure Core ranking operations in the order defined by [data-model.md](data-model.md#discovery-candidate-derived-not-persisted). Keep normalization and ranking free of mutable static initialization under the harness mutation rules. Complete plain-text reason and response DTO assembly after ranking. Prove zero provider HTTP at the API boundary.
4. **Endpoint and codegen.** After reason/response assembly is complete, register `GET /api/recommendations/not-in-library` with `TypedResults`. Pin JSON, unauthenticated HTTP 401 and For You parity via `WebApplicationFactory`, including TMDb-configured empty (no eligible candidates → 200 `[]`) alongside both config-off empties. Regenerate committed OpenAPI snapshot + TypeScript under the constitution Verify snapshot/drift obligation and keep drift green before web composition.
5. **Web section.** Fetch independently via TanStack Query and the generated openapi-fetch client. Hide the section on loading, empty or error with the grid still usable. Use read-only cards, plain text for title/reason, safe poster filename handling with fallback, and accessible TMDb links with one tested URL builder. Use MSW for web API fixtures. Display day-verified TMDb credit and record the terms URL and verification date in the delivery PR and CONCLUSIONS.md. Run local seeded E2E for SMOKE-001 with the fixture strategy confirmed at pickup.
6. **Docs and evidence.** Positioning on `specs/PRODUCT.md` + glossary + README feature list; home-library ≥1-seed / ≥2-seed counts under the same Option A exclusion and owner top-10 verdicts on the delivery PR. Document Option A's incomplete-identity limitations in the receipt.
7. **Gates and Size L review.** Phase 3 gates with honest SKIPPED / N/A / SKIP dispositions, two-round closing bar including Low, brief follow-up disposition for out-of-scope items (check for an existing open ticket before filing eligible follow-ups), rebase, ship review, merge-bar receipt; owner merges.

## Authorized project files

| Area | Files and scope |
|---|---|
| ADR | `docs/adr/0049-for-you-outside-library-discovery.md` (Accepted; review artifact — already present) |
| Product / glossary / README | `specs/PRODUCT.md` Positioning subsection; root `CONTEXT.md` Recommendation + **Outside-library recommendation**; `README.md` feature list only (provider attribution stays DEV-559) |
| TMDb adapter | `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.Signals.cs`, `TmdbMetadataProvider.Mapping.cs` — carry title/year/poster from first-page recommendations |
| Persistence | `MovieRecommendationRecord.cs`, `MovieConfiguration.cs` / `MovieRecommendationConfiguration`, repository write paths in `EfMovieRepository.cs`, additive migration + snapshot; preserve the existing recommended TMDb ID index |
| Enrichment / backfill | `BackfillTmdbRecommendationsCommandHandler.cs` and enrichment persistence path that writes recommendation rows — hydrate titled metadata; retain links on failure |
| Options | `RecommendationOptions` — Liked/Watched discovery weights (defaults 2 / 1) |
| Discovery feature | `GetOutsideLibraryRecommendationsQuery`, `GetOutsideLibraryRecommendationsQueryHandler`, `OutsideLibraryRecommendation` response DTO, pure ranking operations and reason formatter under `src/LamuFlix.Core/Features/Recommendations/`; Core-owned `OutsideLibraryRecommendationSnapshot` returned by the existing `IRecommendationCandidateSource` port through `EfRecommendationCandidateSource`; `AddHandler` registration |
| Endpoint | `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs` — `TypedResults`; empty 200 `[]` retained |
| Generated contract | Committed `web/src/api/openapi.json` snapshot + `web/src/api/types.ts`; constitution Verify snapshot / drift obligation green |
| Web | `web/src/features/for-you/ForYouPage.tsx`, `forYouApi.ts`, and discovery section/card components and URL handling in that feature folder |
| Tests | Existing provider/Core/EF/API/web/E2E locations identified at pickup |

## Project structure

### Documentation (this feature)

```text
specs/DEV-561/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # this file
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/not-in-library.md
├── checklists/
├── tasks.md
└── DRAFTING_RECEIPT.md
```

### Source Code (authorized)

See the authorized file table above.

**Structure Decision**: Extend existing vertical-slice / ports-and-adapters layout. No new project, top-level folder, or architectural layer.

## Complexity Tracking

> No constitution violations requiring justification. Schema change is ticket-authorized (`specs/PRODUCT.md` §5 / Wanted 3). Public API shape is ticket/brief-authorized (Q4). Docs edits are Patron-approved under Charter §2.3 items 2 and 6 (Q8).

## Phase 0 research

See [research.md](research.md). All drafting unknowns are resolved from brief/CONCLUSIONS; no NEEDS CLARIFICATION remains for Gate 1 drafting.

## Phase 1 design

See [data-model.md](data-model.md), [contracts/not-in-library.md](contracts/not-in-library.md), and [quickstart.md](quickstart.md).
