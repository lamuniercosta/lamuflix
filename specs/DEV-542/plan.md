# Implementation Plan: Recommendation cards show seed and evidence

**Branch**: `feature/542-spec` (worktree `F:/Dev/LamuFlix.worktrees/feature-542-spec`) | **Date**: 2026-11-19 | **Spec**: `specs/DEV-542/spec.md`

**Input**: Grill-closed private `brief.md` (Q1–Q5); `specs/DEV-542/CONCLUSIONS.md`; Maestri notes `DEV-542`, `recon-DEV-542` (lines 44–69, 71–97; HEAD `1ac83297c1ba8ffc936987e8b198ce8dc612b01a`); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: PLAN FROZEN. Plan challenge dispositions and repairs are recorded in `PLAN_ADJUDICATION.md`. Gate 1, Build, and runtime acceptance remain open.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `contracts/recommended-movie-seed-evidence.md`, `DRAFTING_RECEIPT.md`, `PLAN_ADJUDICATION.md`. No `research.md` / `data-model.md` / `quickstart.md` — grill Q1–Q5 freeze the inventable unknowns; no schema change; API shape is documented under `contracts/`.

## Summary

Replace recommendation-card reasons with seed provenance and ordered criterion evidence. For You responses carry a populated `seed` plus strongest-first `evidence`; Related responses carry `evidence` only and omit the `seed` key. Server builds display-ready evidence text (including three-names-plus-`+N`); the web renders that text and shows For You seed templates from Wanted item 2. Retire legacy reasons/DTO/builder path, regenerate OpenAPI/TypeScript/MSW together, update the named tests, and attach the required screenshot and home-library evidence. Preserve scoring, ranking, selection, routes, and status behaviour. Phase B verifies DEV-543 and DEV-518 inputs before production edits.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Api`, `LamuFlix.Core`) + React 19 / TypeScript / Vite (`web/`) per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing recommendation endpoints, handlers, and card UI only (`recon-DEV-542:11–30`). Consume DEV-543 nearest-seed fields and DEV-518 ordered evidence. No new NuGet/npm dependency (`brief.md` Frozen scope).

**Storage**: No schema or migration change.

**Testing**: Existing xUnit recommendation integration/e2e suites (`ApiEndToEndRecommendationTests`, `RecommendationEndpointTests`, handler unit tests); Vitest + RTL component tests for `MovieCard`, For You, and Related (including the four named Related tests); Playwright For You e2e (`web/e2e/for-you-review.e2e.ts`). No new test framework/dependency.

**Target Platform**: Local API + Compose SPA; home-library reviewer check and before/after screenshots are delivery evidence.

**Project Type**: Existing ports-and-adapters .NET API with feature-organised Core handlers; SPA consumes generated types only.

**Performance Goals**: Preserve result identity, score, and order. Bound added presentation-only work to the selected results and verify there are no added candidate-loop reads or per-result seed/evidence lookups at T001/T004. No new timing SLA.

**Constraints**: Charter §2.3 care-list items deliberately ruled where applicable (API shape authorized by ticket + Q2). Contract chain C# DTO → OpenAPI → TypeScript. No LocalPlay/secrets/`Process.Start`. Honest gate labels; never lower thresholds. Closing bar: every accepted in-scope Critical–Low finding fixed or explicitly disposed with evidence; two review/remediation rounds; unresolved after round two reported on the PR (`brief.md` Q5). Size M requires Sentry, Ledger, and Compass. Sequence ahead of DEV-541 (shared OpenAPI regeneration); blocks DEV-538.

**Scale/Scope**: Size M; bounded recommendation DTO/mapping, Core reason→evidence switch, shared card/For You/Related UI, generated artifacts, and named tests/evidence listed below.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No owner structural checkbox: ticket delivery is preserved; Patron Q1–Q5 closed inventable plan choices without ticket contradiction or constitution departure.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **PASS** — reuse existing recommendation query handlers and endpoint mapping; no new port/layer. Upstream seed/evidence come from DEV-543/DEV-518 contracts verified at pickup. |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — existing For You / Related query handlers and `RecommendationEndpoints` mapping only; no MediatR. |
| III (Typed query model) | MovieQuery | **PASS** — no browse/query-model change; recommendation DTOs only. |
| IV (Enrichment state machine) | Status values | **PASS** — pending-enrichment empty Related list behaviour preserved (`recon-DEV-542:13`). |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — existing routes/status codes retained; no new error taxonomy. |
| VI (Observability) | Safe diagnostics | **PASS** — no new telemetry requirement; no secrets in artifacts/screenshots. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no LocalPlay, secrets, or Process.Start change. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS (design)** — Patron confirmed DEV-543/DEV-518 own domain vocabulary and DEV-542 uses the spec-local presentation terms. No root `CONTEXT.md` edit (`CONCLUSIONS.md`, Plan Challenge Ruling). |
| IX (Test Pyramid) | Meaningful layers | **PASS** — API/integration for contract JSON; component tests for card presentation; existing For You e2e for seed-title check; no new framework. |
| Tech stack / API docs | OpenAPI chain | **PASS** — ticket-authorized DTO change; regenerate OpenAPI then TypeScript/MSW; web consumes generated types only (`PRODUCT.md` §5 item 4; brief Frozen scope). |
| Governance / Static-Analysis Gates | Honest exits | Phase B gate evidence MUST include: (1) constitution PR quality gate that `LamuFlix.ArchitectureTests` passes (`constitution.md:775`); (2) constitution static-analysis gates for every changed `.cs` file — Roslyn, cyclomatic complexity (including applicable refactor-gate `-Threshold 6`), InspectCode (`constitution.md:804–815`); (3) applicable property-test and vulnerable-package gates with recorded applicability and actual verdicts; plus `dotnet format --verify-no-changes` as a supplemental formatting check, `dotnet test`, mutation via `scripts/run-mutation.ps1` without `--since` when production C# under `src/` is eligible, and web lint/`check:api`/component/e2e as applicable. Never fold non-pass into PASS; never lower thresholds; no explanatory comments on changed C#. |

### Complexity Tracking

No constitution violations requiring justification. Public API field-set change is authorized by the ticket Scope & Technical Design and Patron Q2. No new architectural layer.

## Approach

- **Phase A**: Spec Kit drafts and independent analysis preceded the three-axis plan challenge. `PLAN_ADJUDICATION.md` records dispositions and fixes. Gate 1 requires the user's spec-PR merge before Build work.
- **Phase B pickup (after Gate 1)**: Fresh drift recon as required; then **dependency checkpoint** — verify DEV-543 real seed with a nonblank title and supported signal on every For You item and DEV-518 strongest-first evidence with public criterion identities on both routes (empty lists allowed). Cite the exact consumption and formatter/test seams and shared viewport anchor. Stop for bounded recon and Patron ruling if the contract or required anchors cannot be established. For any observed runtime contract defect ask Patron for disposition. Preserve existing API status behaviour and result membership. Record the baseline build/ref, library state, cards, viewport widths and paths for desktop For You and the Related rail before production edits.
- **Task order (`brief.md` Task ordering)**:
  1. Spec Kit drafting + Keel analyze + plan challenge + Gate 1 (Phase A).
  2. Pickup checkpoint for DEV-543/DEV-518 + before screenshots.
  3. DTO + server mapping to seed/evidence; retire reasons path; run parsed-wire checks and pure Core formatter coverage; regenerate OpenAPI drift snapshot + `openapi.json` + `types.ts` + `handlers.ts` together (`LAMUFLIX_REGENERATE_OPENAPI` then `npm run generate:api`). **API-only checkpoint** here: integration JSON and generated schema/type shape, including the closed signal set, before web consumers move. This is an internal checkpoint in the T002–T010 uncommitted transition. T006 records the DEV-542-first order and DEV-541 rebases on the landed DEV-542 change before its regeneration.
  4. Move generated-contract consumers and fixtures together (no legacy-`reasons` bridge): wire `MovieCard` / `ForYouPage` / `RelatedMovies` + styles with locally runnable component coverage. At T010 run web typecheck, `npm run check:api`, component/e2e, browser navigation, and desktop/small-width visible and accessible list counts for both surfaces. Record home-library Hercules separately. Commit the coherent transition after the automated checks pass. Capture and record matched after-screenshot paths; run applicable gates including ArchitectureTests + property/vulnerable-package applicability verdicts.
  5. Three-axis technical review/remediation under written closing bar (2-round cap; may use captured screenshot files); then open/update delivery PR, attach and read back both screenshot pairs (publication), and only then make the final merge-bar / acceptance claim.
- **Do not**: change scoring/ranking/selection; add DEV-538 grouping or DEV-517 load-more; keep `reasons` beside the new fields or retain them to bridge the web transition; hand-edit generated artifacts; invent upstream provenance; claim Gate 1 / runtime acceptance from this draft; lower gate thresholds; touch LocalPlay/secrets/`Process.Start`.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-542/
├── CONCLUSIONS.md
├── spec.md
├── plan.md              # This file (also mirrored to private IMPL_PLAN)
├── tasks.md
├── PLAN_ADJUDICATION.md
├── contracts/
│   └── recommended-movie-seed-evidence.md
├── DRAFTING_RECEIPT.md
└── checklists/requirements.md

# Grill brief (private artifact root; not duplicated under specs/)
C:/Users/lamun/AppData/Local/LamuFlix/artifacts/LamuFlix/DEV-542/brief.md
```

No `research.md` / `data-model.md` / `quickstart.md` — grill decisions freeze Spec Kit research unknowns; no schema/data-model change; operational quickstart is the pickup checkpoint + test/evidence plan in brief/tasks.

### Source Code (authorized responsibility envelope — ticket Technical design; recon 11–30, 52–57)

```text
# API DTO + mapping
src/LamuFlix.Api/Endpoints/RecommendedMovieDto.cs
src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs

# Core recommendation reasons → DEV-518 evidence switch
src/LamuFlix.Core/Features/Recommendations/Reason.cs                    # delete only with no remaining callers and under Patron's ruling
src/LamuFlix.Core/Features/Recommendations/RecommendationReasonBuilder.cs   # retire legacy path
src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs
# T001 bounded recon: Related assembly, exact DEV-543/DEV-518 call sites,
# shared pure DEV-518 Core formatter (or existing feature-local seam) and its test file

# Generated contracts (after C# DTO change; never hand-edit)
web/src/api/openapi.json
web/src/api/types.ts
web/src/mocks/handlers.ts
tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json

# Web presentation
web/src/library/MovieCard.tsx
web/src/library/MovieCard.test.tsx
web/src/features/for-you/ForYouPage.tsx
web/src/features/movie-details/RelatedMovies.tsx
web/src/features/movie-details/RelatedMovies.css
# T001 bounded recon: existing For You component test and style paths, shared
# MovieCard styles, and one reusable responsive file/selector/breakpoint

# Tests named by Done when / recon
tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs
tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs
web/src/features/movie-details/RelatedMovies.test.tsx   # lines 125, 148, 548, 580 rewrite
web/e2e/for-you-review.e2e.ts
```

**Structure Decision**: T001 records concrete paths for the bounded entries above before edits. Any other tracked file or protected change needs a cited Patron ruling. Reuse one shared pure Core DEV-518 formatter or its existing feature-local seam, applying the same display rules on both routes. Retire `RecommendationReasonBuilder` and unused `ToReasonDto`/`Kind`. Delete `Reason.cs` only with zero remaining callers, as authorized in `CONCLUSIONS.md`.

## Test Strategy

| Family | Required evidence |
|---|---|
| Dependency checkpoint | T001 verifies seed and criterion invariants, selected-result cost, and concrete path anchors. Unverified upstream invariants stop pickup. |
| API / integration | T002 parses populated wire JSON, verifies For You seed, absence of Related seed and any `reasons`, exact fixture criterion sequences, server text, full names, empty evidence, existing pending-enrichment empty Related list/status, legacy copy retirement, and unchanged identity/score/order. T004 omits null seed with a property-level `JsonIgnoreCondition.WhenWritingNull` and maps the closed signal values. |
| Core formatter | T003 pins the full five-name example, sanitizes display text, verifies full ordered names and exercises the display cap and suffix arithmetic with the existing unit/property-test conventions. |
| OpenAPI / web contract | T006 verifies optional seed, required evidence, absent reasons and the three signal values. T010 runs `npm run check:api` and web typecheck after both consumers and fixtures move. DTO, drift snapshot, generated artifacts and consumers land together. |
| Component | T007/T011 verify zero/one/three evidence items, desktop cap, all three signals, defensive omission of an optional seed line, exact five-name text, seed-first reading order, independent anchors and keyboard/click behaviour for distinct IDs, and the four named Related tests. Evidence renders as plain text without client formatting. |
| E2e / reviewer | T008 pins distinct seed and recommendation IDs and verifies respective navigation. T010 verifies desktop and small-width visible/accessible list counts on both surfaces and records the home-library Hercules observation separately. |
| Legacy copy | T002 asserts retired copy is absent from both fixture responses. T013 records the ticket's one-off `rg "Matches your viewing history" src web/src` result. |
| Screenshots | Before (pre-presentation) and after desktop For You + one Related rail card set: capture at matched library state and viewport widths, record build refs and paths during Build; three-axis review may use the captured files; attach both pairs, read back publication, and only then claim final merge-bar readiness on the delivery PR |
| Gates | Size:M evidence records applicability + actual verdicts for `LamuFlix.ArchitectureTests`, constitution static-analysis gates on every changed `.cs`, applicable property tests, applicable vulnerable-package scan, mutation when eligible, web checks; honest labels; no explanatory comments |

## Phase 0 / Phase 1 Spec Kit outputs

Grill Q1–Q5 resolve inventable unknowns that Spec Kit would otherwise research. Contract documentation lives in `contracts/recommended-movie-seed-evidence.md`. No separate `research.md`, `data-model.md`, or `quickstart.md`.

## Post-design Constitution Check

Re-checked after design: still PASS. Ticket-authorized API field set + presentation wiring only; no new layer/dependency/schema/LocalPlay; contract chain preserved; tests map to Done when.
