# Implementation Plan: Deterministic For-You ranking and paging

**Branch**: `feature/541-spec` | **Date**: 2026-11-19 | **Spec**: `specs/DEV-541/spec.md`

**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-541-spec`

**Input**: Grill-closed `brief.md` (Q1–Q10); `CONCLUSIONS.md`; Maestri notes `DEV-541`, `recon-DEV-541`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: PLAN FROZEN · Phase 2 step 6 complete. See [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md). Gate 1, implementation and runtime acceptance are pending.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md` and `PLAN_ADJUDICATION.md`. The brief contains the design decisions, including the For-You page envelope (Q6).

## Summary

Deliver deterministic For-You recommendations with seed and collection caps, release-order collection eligibility, stable offset paging with exclusions, and a successful GET body of `{ items, hasMore }`. Migrate the API → OpenAPI → generated TypeScript contract chain and brief-named mocks, mechanically adapt the confirmed bare-array consumers, and verify with the focused Size M matrix in `tasks.md`.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Core`, `LamuFlix.Api`, `LamuFlix.Infrastructure`) and TypeScript / React 19 in `web/` per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing stack only — `RecommendationOptions`, For-You query/handler, `MmrSelector`, `MovieFeaturesMapper`, endpoint validators, OpenAPI transformer, openapi-fetch generated client, MSW mocks. No new NuGet/npm package or version change (`PRODUCT.md` §5 care list item 1; brief out of scope).

**Storage**: Existing recommendation profile/candidate/features sources. No schema or migration change.

**Testing**: Existing xUnit + Shouldly unit and integration tests; OpenAPI snapshot (`OpenApiContractTests`); web Vitest + existing web build/test gate. Never mock the data-access driver for query-translation behaviour. No new test framework.

**Target Platform**: Local home API + SPA For You page (`/for-you`).

**Project Type**: Existing vertical-slice .NET API + Vite/React web client.

**Performance Goals**: Q1/Q5 define paging over one full capped ranking. Use a prefix only through the extra item needed for `hasMore`, with widened arithmetic and a horizon clamped to the post-cut candidate count. Skip MMR when offset reaches that count. Prove bounded-prefix equivalence against the same full-run fixture.

**Constraints**: Charter §2.3 items 1–6 and Q6 mechanical compatibility authority. DEV-517/538/542/543 are out of scope. Use `const` defaults for new cap and limit options and avoid mutation-sensitive static initialization. Exercise each cap and limit boundary. Harness thresholds remain authoritative.

**Scale/Scope**: Size M; For-You ranking/paging/envelope + contract/mock/caller migration + focused verification. Parent DEV-490.

## Constitution Check

Gates checked against `.specify/memory/constitution.md` (worktree current). No owner structural checkbox from this draft.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **PASS** — extend existing Core ranking/options; no new port or Infrastructure leak into Core. |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — extend existing `GetForYouRecommendationsQuery` / handler; no MediatR. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — read-only recommendation path; no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails / existing 400 pattern | **PASS** — the endpoint rejects raw transport violations with `BadHttpRequestException` and existing ProblemDetails plus `traceId` before dispatch. The query validator checks normalized invariants (Q4, T009–T010). |
| VI (Observability) | Safe diagnostics | **PASS** — no new credential/logging surface. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — extend typed `RecommendationOptions` only; preserve existing data-annotation validation and `ValidateOnStart()` registration for the new typed knobs; no LocalPlay, secrets, or Process.Start. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — spec-local vocabulary only; no CONTEXT.md expansion claimed. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — unit for selector/handler/validator/options; HTTP integration for envelope and ProblemDetails; OpenAPI + web gate for contract. |
| Tech stack / API docs | OpenAPI chain | **PASS** — For-You DTO → OpenAPI → generated TS per `PRODUCT.md` / contract chain (Q6); public `offset` and repeated `exclude` inputs discoverable with the envelope; movie-specific contract preserved. |
| Governance / Static-Analysis Gates | Configured Size M bar | T016/T017 specify verification and review criteria. |

### Complexity Tracking

No constitution violations requiring justification.

## Approach

- **Phase A**: Quill drafted the Spec Kit artifacts. Keel completed the read-only analysis and recorded the three-axis challenge dispositions. For the spec PR, Bernstein coordinates, Rigger prepares and opens it, the user merges it, and Keel records the Gate 1 receipt. Gate 1 precedes Build.
- **Phase B pickup**: Follow frozen Q7 order exactly (`brief.md` Task order; Q7 lines 74–76).
- **Task order (`brief.md` Task order steps 1–5; Q7)**:
  1. T001 confirms the challenge baseline against pickup HEAD and records exact caller paths and deltas. `recon-DEV-541` could not establish the original caller inventory. Sentry F1 and Compass F3 establish the baseline below. T001 precedes Core and later Build phases.
  2. Update typed `RecommendationOptions` with existing data annotations and `ValidateOnStart`. Both ranking caps have a minimum of one and the raw exclusion limit has a minimum of zero. Update the For-You query and handler, collection data via `MovieFeaturesMapper`, and capped `MmrSelector` selection. Add focused tests in the existing suites.
  3. Add the Core page record and dedicated API page DTO in the existing folders below and update the existing For-You handler binding in `HandlerRegistration.cs`. The endpoint owns raw offset cardinality, invariant integer parsing and range, raw exclude count before deduplication, and integer parsing before positive MovieId conversion. Use the existing 400 path with static messages and `traceId`. The query validator checks normalized invariants, including nonnegative offset. Verify dispatch and actual wrapped JSON in `RecommendationEndpointTests.cs`.
  4. Update `RecommendationsOperationTransformer.cs` with the envelope and public offset plus form-style exploded integer-array exclude. Review the refreshed OpenAPI snapshot diff. Regenerate OpenAPI, TypeScript and MSW handlers, then migrate the success fixture using the generated page schema. In the take-only hook unwrap `items` under the existing query key, preserving the page's array input. Adapt the known tests and T001 deltas mechanically.
  5. Collect the focused, configured gate and implementation review evidence under T015–T017, and record the independent SMOKE-001 disposition.
- Preserve the movie-specific bare-list endpoint. DEV-517 owns load-more/swap and consumes `hasMore` in a later change. DEV-538 owns reason grouping, DEV-542 owns card layout/text and DEV-543 owns weights/threshold/formula.

## Verification Approach

- T002–T004 and T007–T008 cover ranking, cap and option boundaries, prefix equivalence, the huge-offset short-circuit and HTTP empty page, and sanitized ProblemDetails validation.
- T011–T015 cover snapshot diff review, generated contracts and fixtures, repeated-key serialization and a generated-client request to the API, and array-hook compatibility.
- T016/T017 carry requirements for `task-pipeline` Phase 3 step 5 and Phase 4. T017 records SMOKE-001 independently. The final implementation PR merge bar and the user's merge are Phase 5.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-541/
├── brief.md
├── CONCLUSIONS.md
├── spec.md
├── plan.md              # This file (worktree / Spec PR copy)
├── tasks.md
├── DRAFTING_RECEIPT.md
├── PLAN_ADJUDICATION.md
└── checklists/
    └── requirements.md
```

No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief; public contract is the ruled For-You envelope.

### Source Code (authorized boundary — brief Task order; recon)

```text
src/LamuFlix.Core/
├── Options/RecommendationOptions.cs
└── Features/Recommendations/
    ├── ForYouRecommendationsPage.cs
    ├── GetForYouRecommendationsQuery.cs
    ├── GetForYouRecommendationsQueryHandler.cs
    ├── MmrSelector.cs
    ├── MmrCandidate.cs                    # extend only if cap/collection fields require it
    └── MovieFeaturesMapper.cs

src/LamuFlix.Infrastructure/Recommendations/
└── GetForYouRecommendationsQueryValidator.cs

src/LamuFlix.Api/
├── Endpoints/RecommendationEndpoints.cs
├── Endpoints/ForYouRecommendationsPageDto.cs
├── Endpoints/RecommendedMovieDto.cs       # existing item DTO
└── RecommendationsOperationTransformer.cs

tests/LamuFlix.UnitTests/
├── Options/RecommendationOptionsTests.cs (+ related existing options tests as needed)
└── Features/Recommendations/
    ├── GetForYouRecommendationsQueryHandlerTests.cs
    ├── GetForYouRecommendationsQueryTests.cs
    ├── GetForYouRecommendationsQueryValidatorTests.cs
    ├── MmrSelectorTests.cs
    ├── MmrSelectorSequelChainTests.cs
    └── MovieFeaturesMapperTests.cs

tests/LamuFlix.IntegrationTests/
├── RecommendationEndpointTests.cs
├── OpenApiContractTests.cs
└── Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json

web/src/
├── api/openapi.json                       # regenerated
├── api/types.ts                           # regenerated
├── mocks/handlers.ts                      # regenerated
├── mocks/fixtures.ts                      # For-You success envelope
├── mocks/forYou.test.ts
├── features/for-you/forYouApi.ts
├── features/for-you/forYouApi.test.ts
├── features/for-you/ForYouPage.tsx         # compatibility verification
├── features/for-you/ForYouPage.test.tsx
└── (additional mechanical adaptations confirmed by T001)
```

**Structure Decision**: Extend the existing API/Core recommendation folders and web For You feature. Wisp confirms the listed caller baseline and records pickup deltas under Q6/Q7.
