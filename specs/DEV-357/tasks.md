---
description: "Task list for DEV-357 GET /api/recommendations/for-you from taste profile"
---

# Tasks: GET /api/recommendations/for-you from taste profile

**Input**: Design documents from `specs/DEV-357/` (`spec.md`, `plan.md`, closed `brief.md` Q9)

**Prerequisites**: Closed grill Q9; drafted `spec.md` and `plan.md`. Plan freeze, Gate 1, analysis clearance, and implementation readiness are **separate later decisions** — not granted by these tasks.

**Tests**: Required per brief Q8 / plan proof matrix (unit/FsCheck, real PostgreSQL where data-dependent, endpoint/E2E/OpenAPI). No property-test opt-out.

**Organization**: First phase is Wisp read-only STOP (including typed-reason wire/consumer/fixture coherence). After readiness clear, Anvil implements Core query/handler + Infrastructure validator → API (with host composition) → OpenAPI/`generate:api` as ordered committed phases. Cog/Gauge/reviews follow at Build stages.

**Evidence**: branch `feature/357-spec`; Phase A base / HEAD `ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 personalized ranking, US2 cold start, US3 take/OpenAPI
- Include exact file paths in descriptions

## Path Conventions

Repository root layout: `src/`, `tests/`, `web/` as in plan.md authorized ledger.

---

## Phase 1: Wisp read-only pickup STOP (blocks all implementation)

**Purpose**: Prove prerequisites before any authorized source change. Read-only. No production or test writes.

**⚠️ CRITICAL — STOP**: No Core/API/OpenAPI implementation until this phase passes. On failure → bounded dependency recon and a cited Patron amendment before any new port or adapter. Never guess interfaces, waive DEV-353/354/415/416, invent signatures, write production substitutes, or add an ad hoc cache.

- [ ] T001 Confirm merged specifications required for this delivery are available
- [ ] T002 Confirm PRODUCT section 2 Epic 7 prerequisite allows Epic 9 / DEV-357 implementation (`specs/PRODUCT.md`)
- [ ] T003 Verify landed DEV-353 TasteProfile contracts in src (not draft-only)
- [ ] T004 Verify landed DEV-354 blend / Personal-reason contracts in src (not draft-only)
- [ ] T005 Verify landed DEV-415 TasteProfile cache contracts in src (not draft-only; specs/DEV-415 may still be absent at Phase A base — recon-DEV-357:76)
- [ ] T006 Verify landed DEV-416 MMR contracts in src (not draft-only; specs/DEV-416 may still be absent at Phase A base — recon-DEV-357:80)
- [ ] T007 Verify real vector/acquisition dependencies required by those predecessors (candidate and contributor 384-vector surfaces). At Phase A base, playback event/write/persistence and movie-feature vector record/column/snapshot/repository exist (recon-DEV-357-2:50-60); do not infer complete all-library acquisition, a confirmed `IMovieFeaturesRepository` interface signature, a history-read contract, or runtime readiness (:56,60,62). If required acquisition/runtime support is missing → STOP
- [ ] T008 Identify exact landed predecessor signatures for profile/cache/history, blend, and MMR consumption — record them; do not invent
- [ ] T009 Verify coherent `HasHistory` support (playback OR watched) from the same underlying history used for profile calculation; if absent → STOP
- [ ] T010 Verify all-library summary/feedback/384-vector acquisition source exists without requiring a new port/adapter; if existing ports cannot supply these → STOP for bounded recon and cited Patron amendment
- [ ] T011 Fresh bounded drift recon against Phase A base `ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28` (do not treat Phase A unlanded facts as still true without re-check)
- [ ] T012 [P] Confirm exact OpenAPI regeneration and `web generate:api` commands from landed configuration (reuse `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` with `LAMUFLIX_REGENERATE_OPENAPI=true`, then `generate:api` producing `web/src/api/types.ts` and `web/src/mocks/handlers.ts` per recon-DEV-357-2:95-106,112-115; do not invent extra flags)
- [ ] T013 [P] Gauge baseline measurements remain separate from this STOP phase; record that Build-stage gates are not claimed here
- [ ] T013a Verify landed typed-reason wire/types, existing `web/src/features/movie-details/RelatedMovies.tsx` consumers/tests, and mock fixtures (`web/src/mocks/fixtures.ts`, related tests) are coherent with predecessor typed `{kind,text}` reasons (recon-DEV-357-2:41-44; brief.md:106; FR-014). If not coherent → STOP for bounded ownership recon and a cited Patron decision before out-of-ledger edits; do not assign unproven predecessor ownership, send fixes to DEV-418, authorize UI work, or waive web gates

**Checkpoint**: Phase B STOP cleared (or explicitly blocked with recon + Patron amendment / ownership-decision path). Only then proceed to Phase 2+.

---

## Phase 2: Core query/handler + Infrastructure validator unit/FsCheck (US1 + US2)

**Goal**: Land Core for-you query/handler orchestration and Infrastructure FluentValidation validator; prove history/blend/MMR composition with unit and FsCheck tests. Commit before reporting.

**Independent Test**: Handler fixtures cover positive-match Personal path, cold-start empty list, Not Interested-only, empty-features history, full-library/`S=0` blend, MMR order without score mutation, uniqueness/size/ties, cancellation.

### Tests

- [ ] T014 [P] [US1] Add `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryTests.cs`
- [ ] T015 [P] [US3] Add `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryValidatorTests.cs` covering FluentValidation take bounds (`CandidatePoolSize`); direct-query validation failures are 422-class ValidationException outcomes (distinct from HTTP parser 400)
- [ ] T016 [US1] Add `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs` covering: positive genre/plot Personal explanations; disabled profile-weight fallback; all-library candidates beyond `CandidatePoolSize`; Not Interested exclusion before scoring; `S = 0` unnormalized blend; watched penalty; negative finite score preservation; MMR order without mutating scores/reasons; uniqueness; min-size; determinism/ties; diverse sequel fixture; cancellation propagation
- [ ] T017 [US2] In `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs`: playback-only and watched-only history; no-history and Not Interested-only cold start → empty before scoring/MMR; real history with empty features is not cold start; no candidates → empty-list semantics at Core result level
- [ ] T018 [US1] In `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs` only (additional class within that same file is compatible; no adjacent property file outside the Q7 ledger): FsCheck properties for uniqueness, output size, deterministic ties / ordering invariants (no property opt-out)

### Implementation

- [ ] T019 [P] [US1] Add sealed-record query `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQuery.cs` (Principle II)
- [ ] T020 [P] [US3] Add FluentValidation validator `src/LamuFlix.Infrastructure/Recommendations/GetForYouRecommendationsQueryValidator.cs` (Patron Q7 amendment DEV-357:225; brief.md:71,87-91; recon-DEV-357-2:8-13 — do not place FluentValidation in Core or add a new package; take bounds aligned with DEV-341 / `CandidatePoolSize`; Principle V — decorator ValidationException → 422, distinct from route parser 400)
- [ ] T021 [US1] Add sealed `IQueryHandler` `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs` orchestrating landed profile/cache/history, all-library acquisition, DEV-354 blend with `S = 0`, DEV-416 MMR before Take; immutable results; cancellation propagates; consume exact signatures recorded in T008 (Principles I–II)
- [ ] T022 [US2] In `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs`: enforce `HasHistory` predicate; return empty before scoring/MMR when false; do not treat `TasteProfile.IsEmpty` as no history
- [ ] T023 Commit Core/Infrastructure validator phase; report readiness for API phase — do not start API until commit exists

**Checkpoint**: Core query/handler, Infrastructure validator, and unit/FsCheck green against landed predecessors. No API route required yet.

---

## Phase 3: Thin API route, registration, real integration (US1 + US2 + US3 take)

**Goal**: Expose the route with reused parser/`ToDto`, register the handler, and prove HTTP behavior with real integration coverage. Commit before reporting.

**Independent Test**: Endpoint returns ranked Personal reasons with history; 200 `[]` without; omitted take defaults to constant 10 when pool ≥ 10; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` → parser 400; explicitly empty/malformed take → 400 ProblemDetails/`traceId` (not 422); host route equality includes for-you and ManifestContracts resolves the for-you decorator row; per-movie route still passes.

### Tests

Shared-file edits are serialized (no conflicting `[P]`). Exact authorized paths only (Q7 ledger as amended).

- [ ] T024 [US1] Extend `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` for for-you ranked Personal path (positive-match fixture; typed lowercase `{kind,text}`; assert ProblemDetails/`traceId` on error paths exercised here)
- [ ] T025 [US2] Extend `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` (after T024) for no-history and Not Interested-only → 200 `[]`
- [ ] T026 [US3] Extend `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` (after T025) for DEV-341 parser contract: omitted `take` → default constant 10 when `CandidatePoolSize >= 10`; inclusive bounds; explicitly empty/whitespace/malformed/overflowing/repeated/noninteger/out-of-range → 400 ProblemDetails with `traceId`; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` → parser 400 ProblemDetails/`traceId` (default exceeds pool; brief.md:44,112; recon-DEV-357-2:65-71); keep parser 400 distinct from any direct-query validation 422 proved through composed registration
- [ ] T027 [US1] Extend `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs` for for-you with real PostgreSQL/Testcontainers where persistence/vectors/cache-invalidation apply; never mock driver translation/serialization; never fake vectors
- [ ] T028 [US1] Extend `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs` (after T027) for cache repeat-read and playback/feedback invalidation coverage that consumes DEV-415 behavior (do not rebuild cache)
- [ ] T029 [US1] After T026 and T028: regress existing per-movie recommendations route in `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` and `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs`
- [ ] T029a [US3] After T030–T031: update `tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs` expected route equality to include for-you, and add the for-you `ManifestContracts` row for decorator-resolution coverage only (Patron DEV-357:254-255; brief.md:76,93-100; recon-DEV-357-2:82-93). Route equality is the proven blocking failure. Do not claim forced manifest completeness/count or unsupported 16-to-17 route count; no unrelated host-test rewrite

### Implementation

- [ ] T030 [US3] Add thin `GET /api/recommendations/for-you` route in `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs` using `TypedResults` / existing `Results<…>` patterns; reuse existing take parser and `ToDto`; preserve final MMR order; parser 400 remains distinct from ValidationException 422 (Principles II, V; Technology Stack endpoint rules)
- [ ] T031 Register in `src/LamuFlix.Api/HandlerRegistration.cs`: scoped concrete `GetForYouRecommendationsQueryValidator`, scoped `IValidator<GetForYouRecommendationsQuery>` forwarding to that same concrete instance, and `AddHandler<GetForYouRecommendationsQueryHandler, GetForYouRecommendationsQuery, …>()` so decorator order is Tracing → Logging → Validation → handler; prove direct-query validation through composed registration; retain unit-validator coverage and parser 400 versus query-validation 422 distinction (brief.md:104; recon-DEV-357-2:16-20; Principle II — exact landed signature from T008; do not invent a second path; grants no additional test-file authorization beyond the Q7 ledger)
- [ ] T032 Commit API/integration phase; report readiness for OpenAPI phase

**Checkpoint**: HTTP acceptance for US1–US3 take behavior holds on real integration where required; host route equality and for-you ManifestContracts row updated.

---

## Phase 4: OpenAPI, drift snapshot, generated types and MSW handlers (US3)

**Goal**: Update committed OpenAPI baseline, drift snapshot, and both `generate:api` outputs (`web/src/api/types.ts` and `web/src/mocks/handlers.ts`) through the existing opt-in writer then generator. Commit before reporting.

**Independent Test**: Ordinary no-export OpenAPI tests show no drift; generated `types.ts` and `handlers.ts` match the authorized contract; no hand-edits of generated files; no claim that every generator run must change output bytes.

### Implementation and verification

- [ ] T033 [US3] Extend assertions in `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` for the for-you route as needed while **reusing** existing `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` — no new exporter
- [ ] T034 [US3] Run opt-in paired regeneration using the exact commands confirmed in T012 (`LAMUFLIX_REGENERATE_OPENAPI=true`); clear env even on failure; update `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`
- [ ] T035 [US3] Final ordinary no-export OpenAPI drift/equality/no-write checks with env unset
- [ ] T036 [US3] Run existing `web generate:api` per T012 after the paired OpenAPI/snapshot export; refresh both `web/src/api/types.ts` and `web/src/mocks/handlers.ts` (Patron DEV-357:254-255; brief.md:83,93-100,122; recon-DEV-357-2:95-106,112-115); never hand-edit generated outputs, replace the generator, or claim each run must change bytes; invent no flags
- [ ] T037 Commit OpenAPI/generated-contract phase; report for Build-stage refactor/gates/review

**Checkpoint**: SC-003 / OpenAPI no-drift satisfied; contract chain intact for both generator outputs.

---

## Phase 5: Cog refactor, Gauge gates, M-tier review (Build workspace)

**Purpose**: Cross-cutting verification at authorized Build stages. Do not claim these from Spec workspace drafting.

- [ ] T038 Cog refactor against harness refactor complexity threshold
- [ ] T039 Gauge / task-pipeline Phase 3 step 5 gates at harness thresholds: Roslyn, complexity, InspectCode, mutation, property tests, vulnerable packages, format verification, `dotnet test`, applicable web gates; keep PASS / SKIPPED (scope-empty) / SKIP / N/A / Could not run distinct; never lower thresholds
- [ ] T040 M-tier reviews: Sentry, Ledger, Compass under Q3 closing bar (Critical/High block; Medium/Low do not independently block unless they waive acceptance/gate; two remediation rounds max)
- [ ] T041 Remediations within Q3 cap; at cap report unresolved blockers without a third round
- [ ] T042 Ship review, hosted checks, merge bar (separate); user-owned merge

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (STOP)**: First phase. Blocks all Phase 2+ source work. Read-only (includes T013a typed-reason coherence).
- **Phase 2 (Core + Infrastructure validator)**: After STOP clear — Anvil; commit before Phase 3
- **Phase 3 (API + host composition)**: After Phase 2 commit — Anvil; commit before Phase 4
- **Phase 4 (OpenAPI / generate:api)**: After Phase 3 commit — Anvil; commit before Phase 5
- **Phase 5 (Build)**: After Phase 4 — Cog/Gauge/reviews

### User Story Dependencies

- **US1 (personalized ranking)**: Needs Phase 1 predecessors + Phase 2 handler + Phase 3 route
- **US2 (cold start)**: Same Core/API surfaces; distinct fixtures
- **US3 (take/OpenAPI)**: Infrastructure validator + route take behavior in Phases 2–3; host composition T029a; contract artifacts (`types.ts` + `handlers.ts`) in Phase 4

### Parallel Opportunities

- T001–T011 and T013a are sequential evidence checks; T012–T013 may proceed in parallel once dependency facts are being recorded
- T014–T015 and T019–T020 marked [P] after STOP clear (disjoint new files; T020 is Infrastructure path)
- Phase 3 integration tests are **serialized** on shared files: T024 → T025 → T026 on `RecommendationEndpointTests.cs`; T027 → T028 on `ApiEndToEndRecommendationTests.cs`; T029 after both suites’ for-you extensions (T026 and T028); T029a after T030–T031 on `ApiHostCompositionTests.cs`. No `[P]` on T024–T029a
- Do not parallelize OpenAPI regeneration with hand edits to generated files (forbidden)

---

## Parallel Example: After STOP clear (Core tests)

```powershell
# After Phase 1 STOP clear, these test files can be authored in parallel:
# GetForYouRecommendationsQueryTests.cs
# GetForYouRecommendationsQueryValidatorTests.cs
# (handler tests depend on query/validator shapes)
```

---

## Implementation Strategy

### MVP path

1. Phase 1 STOP clear (or explicit block), including typed-reason coherence
2. Phase 2 Core query/handler + Infrastructure validator + unit/FsCheck → commit
3. Phase 3 API + integration + host composition → commit
4. Phase 4 OpenAPI + `generate:api` (`types.ts` and `handlers.ts`) → commit
5. Phase 5 Build gates and M-tier reviews

### Forbidden shortcuts

- No skipping Phase 1 STOP
- No new port/adapter without cited Patron amendment after failed pickup
- No substitute algorithms, production stubs, or dependency waiver
- No invented predecessor signatures or generation flags
- No hand-edits of generated `openapi.json` / `types.ts` / `handlers.ts`
- No FluentValidation in Core or new package for the validator
- No forced host-test completeness/count claims beyond authorized route equality + additive ManifestContracts row
- No For You UI (DEV-418)
- No plan-freeze or Gate 1 claim from drafting these tasks

---

## Notes

- Closing bar: Q3 — Critical/High (plus unmet acceptance/gates/axes/escalations/missing contracts) block; Medium/Low cannot waive acceptance/gate otherwise non-blocking; two remediation rounds total
- Scope: anything else is a follow-up issue, not a finding in this round
- Frontend rail: DEV-418
- Phase A outstanding (not part of Phase 1 STOP): Keel read-only `/speckit-analyze`, plan challenge, separate plan freeze, separate Gate 1 — brief Q9
- Unresolved runtime facts stay `needs recon` at pickup — do not invent
