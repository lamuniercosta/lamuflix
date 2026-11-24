# Implementation Plan: Outside-library discovery on For You

**Branch** — `feature/561-spec` | **Date** — 2026-11-24 | **Worktree** — `F:/Dev/LamuFlix.worktrees/feature-561-spec`

**Status** — Draft for Gate 1. Pre-merge sequence: Spec Kit analyze repair → size-L plan-plus-ADR challenge (Keel) → provisional Gate 1 only after that challenge disposition. PLAN FROZEN is Keel-owned. Ordinary owner merge of the Spec Kit PR is Gate 1. Phase B pickup, implementation, review caps, and runtime acceptance remain distinct and pending.

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
| I — Ports and adapters | Named ports; Core isolation | Reuse existing TMDb adapter, movie repository/persistence, and recommendation feature seams. No new port unless pickup proves one is required (then Patron ruling before invent). |
| II — Explicit handlers | Handlers + AddHandler | New sealed query record + sealed handler under Features/Recommendations; register via existing `AddHandler` decorator path (Tracing → Logging → Validation → handler). Backfill remains the existing DEV-539 operator command. |
| III — Typed query model | MovieQuery | N/A for browse filtering. Discovery returns its own card DTO array from Core Recommendations. |
| IV — Domain transitions | Movie mutators | No new Movie lifecycle. Link metadata rides existing enrichment/backfill persistence. |
| V — Validated inputs and errors | ProblemDetails / options | FluentValidation on the sealed query via the existing validation decorator; shared RFC 7807 `ProblemDetails` for error paths; bind discovery weights in `RecommendationOptions`; keep empty HTTP 200 `[]` for unconfigured TMDb / no eligible candidates (not an error); existing `/api` auth convention. |
| VI — Observability | Safe diagnostics | Home-library acceptance counts/verdicts are sanitized delivery evidence; never log ApiKey or raw personal seed dumps in the PR. |
| VII — Configuration | Options; LocalPlay; Process.Start | Extend `RecommendationOptions` for Liked/Watched discovery weights. Reuse `TmdbOptions` gates. No LocalPlay, secrets, or `Process.Start`. |
| VIII — Ubiquitous language | Constitution VIII | Update `CONTEXT.md` Recommendation entry and add **Outside-library recommendation**; add Positioning subsection to `specs/PRODUCT.md` in the introducing delivery PR. |
| IX — Test pyramid | Meaningful layers | Provider/mapping tests, real-Postgres migration/backfill, domain/property ranking pins, API zero-HTTP integration, web + local E2E, home-library receipt. |

### API and contract rules

Public route and DTO field set are ticket/brief-authorized (`GET /api/recommendations/not-in-library`; card fields per Q4). Place the response DTO in Core Recommendations. Endpoint uses `TypedResults` / `Results<…>` unions. Cover with `WebApplicationFactory` integration tests. Regenerate and commit the OpenAPI snapshot (`web/src/api/openapi.json`) and generated TypeScript; OpenAPI drift verification must stay green. Existing For You contract stays unchanged (constitution Development Workflow → API and Contract Rules). Keep empty HTTP 200 `[]` behaviour.

### Static-analysis gates

Size L delivery revision must pass Roslyn, CA1502 including refactor `-Threshold 6`, InspectCode, mutation, property tests, vulnerable packages, format, .NET tests, and web gates with honest exits. Report exit-2 statuses on their own terms: scope-empty → `SKIPPED (scope-empty)`; mutation with every changed project excluded → `NOT APPLICABLE` / N/A; harness-disabled gate → `SKIP`. Failure or `Could not run` blocks. Never fold non-pass into PASS; never lower thresholds.

## Approach

1. **Pre-merge Gate 1 sequence.** Finish analyze repairs; Keel runs the size-L plan-plus-ADR challenge; provisional Gate 1 only after that disposition. Owner merge of the Spec Kit PR (Accepted ADR 0049 already present) is the Gate 1 receipt. Review caps (two rounds / two fix commits) apply to the delivery revision, not Gate 1. Phase B pickup is after that merge and verifies DEV-539 Done, ADR Accepted, column facets, repository projection/read path, enrichment contract flow, and implementation/test seams; missing facts go to recon.
2. **Schema and mapping.** Additive nullable title/year/poster on `MovieRecommendationRecord` + EF configuration + migration (EF stays in Infrastructure). Extend TMDb first-page mapping and enrichment/backfill writers through existing enrichment-contract seams; hydrate legacy ID-only rows; retain prior links on failed fetch with retry-after-failure eligibility; keep Crowd ID-only consumers green.
3. **Local discovery query.** Implement sealed query/handler set-based ranking via pickup-confirmed repository projection/read path: eligible titled dedupe → canonical metadata tuple → Option A exclusion → two-seed bar → weights/sort/cap → reason + response DTO under Features/Recommendations. Bind weights in `RecommendationOptions`. Prove zero provider HTTP at the API boundary.
4. **Endpoint and codegen.** After reason/response assembly is complete, register `GET /api/recommendations/not-in-library` with `TypedResults`. Pin JSON and For You parity via `WebApplicationFactory`. Regenerate committed OpenAPI snapshot + TypeScript and verify drift before web composition.
5. **Web section.** Independent fetch under For You; hide on loading/empty/error including both config-off API `200 []` paths; distinct read-only cards with accessible TMDb link naming; day-verified TMDb credit (source + verification date recorded); local seeded E2E for SMOKE-001 with fixture strategy confirmed at pickup.
6. **Docs and evidence.** Positioning on `specs/PRODUCT.md` + glossary + README feature list; home-library ≥1-seed / ≥2-seed counts under the same Option A exclusion and owner top-10 verdicts on the delivery PR.
7. **Gates and Size L review.** Phase 3 gates with honest SKIPPED / N/A / SKIP dispositions, two-round closing bar including Low, brief follow-up disposition for out-of-scope items, rebase, ship review, merge-bar receipt; owner merges.

## Authorized project files

| Area | Files and scope |
|---|---|
| ADR | `docs/adr/0049-for-you-outside-library-discovery.md` (Accepted; review artifact — already present) |
| Product / glossary / README | `specs/PRODUCT.md` Positioning subsection; root `CONTEXT.md` Recommendation + **Outside-library recommendation**; `README.md` feature list only (provider attribution stays DEV-559) |
| TMDb adapter | `src/LamuFlix.Infrastructure/Adapters/TmdbMetadataProvider.Signals.cs`, `TmdbMetadataProvider.Mapping.cs` — carry title/year/poster from first-page recommendations |
| Persistence | `MovieRecommendationRecord.cs`, `MovieConfiguration.cs` / `MovieRecommendationConfiguration`, repository write paths in `EfMovieRepository.cs`, additive migration + snapshot |
| Enrichment / backfill | `BackfillTmdbRecommendationsCommandHandler.cs` and enrichment persistence path that writes recommendation rows — hydrate titled metadata; retain links on failure |
| Options | `RecommendationOptions` — Liked/Watched discovery weights (defaults 2 / 1) |
| Discovery feature | Sealed query record, sealed handler, Core Recommendations response DTO, and reason formatter under `src/LamuFlix.Core/Features/Recommendations/`; `AddHandler` registration; pickup-confirmed repository projection/read path (EF remains in Infrastructure) |
| Endpoint | `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs` — `TypedResults`; empty 200 `[]` retained |
| Generated contract | Committed `web/src/api/openapi.json` snapshot + `web/src/api/types.ts`; drift verification green |
| Web | `web/src/features/for-you/ForYouPage.tsx`, `forYouApi.ts`, and discovery section/card components in that feature folder |
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
