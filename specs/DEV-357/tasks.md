---
description: "Task list for DEV-357 GET /api/recommendations/for-you from taste profile"
---

# Tasks: GET /api/recommendations/for-you from taste profile

**Input**: Design documents from `specs/DEV-357/` (`spec.md`, `plan.md`, closed `brief.md` Q9 + Phase B pickup amendment 2026-10-24)

**Prerequisites**: Closed grill Q9; Quill-aligned `spec.md` and `plan.md` to Phase B amendment. Plan freeze, Gate 1, analysis clearance, and source-implementation authorization are **separate later decisions** — not granted by these tasks or by technical readiness alone.

**Tests**: Required per brief Q8 / plan proof matrix (unit/FsCheck, real PostgreSQL where data-dependent, endpoint/E2E/OpenAPI), including Phase B proofs: full-library paging beyond `CandidatePoolSize`, vectorless/unenriched retention, feedback exclusion/recheck, null-movie skips, cache/history semantics, and negative-score preservation through selector-only rebase. No property-test opt-out.

**Organization**: Phase 1 technical STOP items T001–T011, T012, T013a are SATISFIED at HEAD `0607eb7` per Patron Phase B amendment. After Quill alignment and applicable separate pipeline approvals, Anvil implements Core query/handler + Infrastructure validator → API (with host composition) → OpenAPI/`generate:api` as ordered committed phases. Cog/Gauge/reviews follow at Build stages.

**Evidence**: worktree `F:/Dev/LamuFlix.worktrees/DEV-357`; branch `feature/DEV-357`; Phase A base `ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28`; current source HEAD `0607eb7ea8a4ec48ffffa933767568a87eb8df30`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 personalized ranking, US2 cold start, US3 take/OpenAPI
- Include exact file paths in descriptions

## Path Conventions

Repository root layout: `src/`, `tests/`, `web/` as in plan.md authorized ledger.

---

## Phase 1: Wisp read-only pickup STOP (technically cleared at HEAD `0607eb7`)

**Purpose**: Prove prerequisites before any authorized source change. Read-only. No production or test writes.

**Disposition (Patron Phase B amendment, 2026-10-24)**: APPROVED WITH AMENDMENTS FOR QUILL ALIGNMENT. T001–T011 SATISFIED; T012 and T013a refreshed and satisfied; T013 Gauge baseline/gates remain separate and unclaimed. Quill alignment of `spec.md` / `plan.md` / `tasks.md` is required before Phase 2. This clearance does **not** grant plan freeze, Gate 1, or source-implementation authorization.

**⚠️ CRITICAL**: Do not start Phase 2+ until Quill alignment is complete and applicable separate pipeline approvals exist. Never guess interfaces, waive DEV-353/354/415/416, invent signatures, write production substitutes, or add an ad hoc cache. No new port/adapter.

- [x] T001 Confirm merged specifications required for this delivery are available — **SATISFIED** (Patron): predecessor bundles `DEV-338`, `DEV-341`, `DEV-353`, `DEV-354`, `DEV-415`, `DEV-416` landed on `origin/main == HEAD` `0607eb7`; DEV-357 own plan freeze/Gate 1 remain separate (recon-DEV-357-2:11-36,75,94; brief.md Phase B amendment)
- [x] T002 Confirm PRODUCT section 2 Epic 7 prerequisite allows Epic 9 / DEV-357 implementation (`specs/PRODUCT.md`) — **SATISFIED** (Patron): owner-final `specs/DEV-338/CONCLUSIONS.md:131-133` and `specs/DEV-342/CONCLUSIONS.md:118,121` (recon-DEV-357-2:77,94-95); do not rely on unsupported `DEV-415:41-44` citation
- [x] T003 Verify landed DEV-353 TasteProfile contracts in src (not draft-only) — **SATISFIED** (recon-DEV-357-2 Sections 2–4; Phase B amendment)
- [x] T004 Verify landed DEV-354 blend / Personal-reason contracts in src (not draft-only) — **SATISFIED** (recon-DEV-357-2 Sections 2–4; Phase B amendment)
- [x] T005 Verify landed DEV-415 TasteProfile cache contracts in src (not draft-only) — **SATISFIED**: `IRecommendationProfileSource.GetTasteProfileAsync` / `GetProfileInputsAsync`; HybridCached tagged expiry + playback/feedback invalidation (recon-DEV-357-2:14-24; Phase B amendment)
- [x] T006 Verify landed DEV-416 MMR contracts in src (not draft-only) — **SATISFIED**: static `MmrSelector.Select`; nonnegative relevance constraint; pool-wide offset precedent (recon-DEV-357-2:26-36; Phase B amendment)
- [x] T007 Verify real vector/acquisition dependencies required by those predecessors — **SATISFIED** via three-port composition (recon-DEV-357-2:63-73,88-93; Phase B amendment): page `IMovieCatalog.BrowseAsync` (explicit valid non-null `Sort`/`Direction`, `Page.Number >= 1`, `Page.Size` within `1..100`, terminate on first short page); per-ID `IMovieRepository.GetAsync`; one `GetProfileInputsAsync(allEligibleIds, ct)`. Retain vectorless/unenriched with empty-feature/embedding fallbacks; corrupt vectors keep landed scoring failure; missing vectors take established fallback. No new port/adapter
- [x] T008 Identify exact landed predecessor signatures for profile/cache/history, blend, and MMR consumption — **SATISFIED**; recorded in recon-DEV-357-2 Sections 3–4 (do not invent beyond those)
- [x] T009 Verify coherent `HasHistory` support (playback OR watched) from the same underlying history used for profile calculation — **SATISFIED**: `RecommendationProfileInputs.Movies` non-empty where adapter rows represent `IsWatched || launchedIds.Contains(Id)`; never `TasteProfile.IsEmpty` (recon-DEV-357-2:41-43,88-93; Phase B amendment)
- [x] T010 Verify all-library summary/feedback/384-vector acquisition source exists without requiring a new port/adapter — **SATISFIED** by the same three-port composition as T007; no `CandidatePoolSize` truncation of the candidate universe; accepted cost `ceil(N/Size)` + N+1 `GetAsync` + one `GetProfileInputsAsync`; per-query consistency + DEV-415 invalidation only (no atomic snapshot / concurrent-write completeness guarantee) (recon-DEV-357-2:63-73,88-93; Phase B amendment)
- [x] T011 Fresh bounded drift recon against Phase A base `ba63fbc8f4958df76c2e92f0e9ac6bb3b972fd28` — **SATISFIED** at HEAD `0607eb7` (recon-DEV-357-2:9-12,61; Phase B amendment)
- [x] T012 [P] Confirm exact OpenAPI regeneration and `web generate:api` commands from landed configuration — **SATISFIED** at HEAD `0607eb7` (recon-DEV-357-2:79-81,96): reuse `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` with `LAMUFLIX_REGENERATE_OPENAPI=true`, then `generate:api` producing `web/src/api/types.ts` and `web/src/mocks/handlers.ts`; do not invent extra flags
- [ ] T013 [P] Gauge baseline measurements remain separate from this STOP phase; record that Build-stage gates are not claimed here — **unclaimed** (Phase B amendment: T013 remains separate)
- [x] T013a Verify landed typed-reason wire/types, existing `web/src/features/movie-details/RelatedMovies.tsx` consumers/tests, and mock fixtures (`web/src/mocks/fixtures.ts`, related tests) are coherent with predecessor typed `{kind,text}` reasons — **SATISFIED** at HEAD `0607eb7` (recon-DEV-357-2:79-81,96; brief.md:106). No ownership STOP; no UI edits or web-gate waiver

**Checkpoint**: Phase 1 technical STOP cleared at HEAD `0607eb7` for T001–T011, T012, T013a. Quill alignment of this ledger (and companion spec/plan) is complete when this file reflects the Phase B amendment. Phase 2+ still requires applicable separate pipeline approvals (plan freeze / Gate 1 / source authorization). T013 Build baseline remains unclaimed.

---

## Phase 2: Core query/handler + Infrastructure validator unit/FsCheck (US1 + US2)

**Goal**: Land Core for-you query/handler orchestration and Infrastructure FluentValidation validator; prove history/blend/MMR composition with unit and FsCheck tests, including Phase B acquisition/MMR proofs. Commit before reporting.

**Independent Test**: Handler fixtures cover positive-match Personal path, cold-start empty list, Not Interested-only, empty-features history, full-library paging/`S=0` blend, vectorless retention, Not Interested exclusion/recheck, null-movie skips, MMR selector-only offset with negative-score preservation, uniqueness/size/ties, cancellation.

### Tests

- [X] T014 [P] [US1] Add `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryTests.cs`
- [X] T015 [P] [US3] Add `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryValidatorTests.cs` covering FluentValidation take bounds (`CandidatePoolSize`); direct-query validation failures are 422-class ValidationException outcomes (distinct from HTTP parser 400)
- [X] T016 [US1] Add `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs` covering: positive genre/plot Personal explanations; disabled profile-weight fallback; all-library candidates beyond `CandidatePoolSize` via three-port paging (explicit sort/direction, size `1..100`, short-page termination); Not Interested exclusion on summaries + recheck on domain rows; null `GetAsync` skips; vectorless/unenriched retention with empty-feature/embedding fallbacks; `S = 0` unnormalized blend; watched penalty; negative finite score preservation through selector-only rebase (`-Math.Min(0.0, minPoolScore)` then ID remap); MMR order without mutating scores/reasons; uniqueness; min-size; determinism/ties; diverse sequel fixture; cancellation propagation
- [X] T017 [US2] In `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs`: playback-only and watched-only history via `Movies` rows (`IsWatched || launched`); no-history and Not Interested-only cold start → empty before scoring/MMR; real history with empty features is not cold start; no candidates → empty-list semantics at Core result level; never treat `TasteProfile.IsEmpty` as no history
- [X] T018 [US1] In `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs` only (additional class within that same file is compatible; no adjacent property file outside the Q7 ledger): FsCheck properties for uniqueness, output size, deterministic ties / ordering invariants (no property opt-out)

### Implementation

- [X] T019 [P] [US1] Add sealed-record query `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQuery.cs` (Principle II)
- [X] T020 [P] [US3] Add FluentValidation validator `src/LamuFlix.Infrastructure/Recommendations/GetForYouRecommendationsQueryValidator.cs` (Patron Q7 amendment DEV-357:225; brief.md:71,87-91 — do not place FluentValidation in Core or add a new package; take bounds aligned with DEV-341 / `CandidatePoolSize`; Principle V — decorator ValidationException → 422, distinct from route parser 400)
- [X] T021 [US1] Add sealed `IQueryHandler` `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs` orchestrating landed profile/cache/history; three-port all-library acquisition (`BrowseAsync` page → exclude Not Interested → `GetAsync` with null skips/recheck → one `GetProfileInputsAsync`); vectorless/unenriched retention; DEV-354 blend with `S = 0`; DEV-416 MMR with selector-only nonnegative relevance offset then ID remap to original blend results before Take; immutable results; cancellation propagates; consume exact signatures recorded in T008 (Principles I–II; Phase B amendment)
- [X] T022 [US2] In `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs`: enforce `HasHistory` via `Movies` non-empty (`IsWatched || launched`); return empty before scoring/MMR when false; do not treat `TasteProfile.IsEmpty` as no history; accept per-query consistency + DEV-415 invalidation without claiming an atomic snapshot
- [X] T023 Commit Core/Infrastructure validator phase; report readiness for API phase — do not start API until commit exists

**Checkpoint**: Core query/handler, Infrastructure validator, and unit/FsCheck green against landed predecessors and Phase B composition rules. No API route required yet.

---

## Phase 3: Thin API route, registration, real integration (US1 + US2 + US3 take)

**Goal**: Expose the route with reused parser/`ToDto`, register the handler, and prove HTTP behavior with real integration coverage. Commit before reporting.

**Independent Test**: Endpoint returns ranked Personal reasons with history; 200 `[]` without; omitted take defaults to constant 10 when pool ≥ 10; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` → parser 400; explicitly empty/malformed take → 400 ProblemDetails/`traceId` (not 422); host route equality includes for-you and ManifestContracts resolves the for-you decorator row; per-movie route still passes.

### Tests

Shared-file edits are serialized (no conflicting `[P]`). Exact authorized paths only (Q7 ledger as amended).

- [X] T024 [US1] Extend `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` for for-you ranked Personal path (positive-match fixture; typed lowercase `{kind,text}`; assert ProblemDetails/`traceId` on error paths exercised here)
- [X] T025 [US2] Extend `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` (after T024) for no-history and Not Interested-only → 200 `[]`
- [X] T026 [US3] Extend `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` (after T025) for DEV-341 parser contract: omitted `take` → default constant 10 when `CandidatePoolSize >= 10`; inclusive bounds; explicitly empty/whitespace/malformed/overflowing/repeated/noninteger/out-of-range → 400 ProblemDetails with `traceId`; omitted with startup-valid `options.Take <= CandidatePoolSize < 10` → parser 400 ProblemDetails/`traceId` (default exceeds pool; brief.md:44,112); keep parser 400 distinct from any direct-query validation 422 proved through composed registration
- [X] T027 [US1] Extend `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs` for for-you with real PostgreSQL/Testcontainers where persistence/vectors/cache-invalidation apply; never mock driver translation/serialization; never fake vectors; prove full-library / retention / cold-start semantics where data-dependent
- [X] T028 [US1] Extend `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs` (after T027) for cache repeat-read and playback/feedback invalidation coverage that consumes DEV-415 behavior (do not rebuild cache; no atomic-snapshot assertion)
- [X] T029 [US1] After T026 and T028: regress existing per-movie recommendations route in `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` and `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs`
- [X] T029a [US3] After T030–T031: update `tests/LamuFlix.IntegrationTests/ApiHostCompositionTests.cs` expected route equality to include for-you, and add the for-you `ManifestContracts` row for decorator-resolution coverage only (Patron DEV-357:254-255; brief.md:76,93-100). Route equality is the proven blocking failure. Do not claim forced manifest completeness/count or unsupported 16-to-17 route count; no unrelated host-test rewrite

### Implementation

- [X] T030 [US3] Add thin `GET /api/recommendations/for-you` route in `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs` using `TypedResults` / existing `Results<…>` patterns; reuse existing take parser and `ToDto`; preserve final MMR order; parser 400 remains distinct from ValidationException 422 (Principles II, V; Technology Stack endpoint rules)
- [X] T031 Register in `src/LamuFlix.Api/HandlerRegistration.cs`: scoped concrete `GetForYouRecommendationsQueryValidator`, scoped `IValidator<GetForYouRecommendationsQuery>` forwarding to that same concrete instance, and `AddHandler<GetForYouRecommendationsQueryHandler, GetForYouRecommendationsQuery, …>()` so decorator order is Tracing → Logging → Validation → handler; prove direct-query validation through composed registration; retain unit-validator coverage and parser 400 versus query-validation 422 distinction (brief.md:104; recon-DEV-357:101; Principle II — exact landed signature from T008; do not invent a second path; grants no additional test-file authorization beyond the Q7 ledger)
- [X] T032 Commit API/integration phase; report readiness for OpenAPI phase

**Checkpoint**: HTTP acceptance for US1–US3 take behavior holds on real integration where required; host route equality and for-you ManifestContracts row updated.

---

## Phase 4: OpenAPI, drift snapshot, generated types and MSW handlers (US3)

**Goal**: Update committed OpenAPI baseline, drift snapshot, and both `generate:api` outputs (`web/src/api/types.ts` and `web/src/mocks/handlers.ts`) through the existing opt-in writer then generator. Commit before reporting.

**Independent Test**: Ordinary no-export OpenAPI tests show no drift; generated `types.ts` and `handlers.ts` match the authorized contract; no hand-edits of generated files; no claim that every generator run must change output bytes.

### Implementation and verification

- [ ] T033 [US3] Extend assertions in `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` for the for-you route as needed while **reusing** existing `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` — no new exporter
- [ ] T034 [US3] Run opt-in paired regeneration using the exact commands confirmed in T012 (`LAMUFLIX_REGENERATE_OPENAPI=true`); clear env even on failure; update `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`
- [ ] T035 [US3] Final ordinary no-export OpenAPI drift/equality/no-write checks with env unset
- [ ] T036 [US3] Run existing `web generate:api` per T012 after the paired OpenAPI/snapshot export; refresh both `web/src/api/types.ts` and `web/src/mocks/handlers.ts` (Patron DEV-357:254-255; brief.md:83,93-100,122; recon-DEV-357:107-108; recon-DEV-357-2:79-81,96); never hand-edit generated outputs, replace the generator, or claim each run must change bytes; invent no flags
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

- **Phase 1 (STOP)**: Technical items T001–T011, T012, T013a cleared at HEAD `0607eb7`. Quill alignment of this ledger is required before Phase 2. Plan freeze / Gate 1 / source authorization remain separate. T013 Build baseline unclaimed.
- **Phase 2 (Core + Infrastructure validator)**: After Quill alignment + applicable separate pipeline approvals — Anvil; commit before Phase 3
- **Phase 3 (API + host composition)**: After Phase 2 commit — Anvil; commit before Phase 4
- **Phase 4 (OpenAPI / generate:api)**: After Phase 3 commit — Anvil; commit before Phase 5
- **Phase 5 (Build)**: After Phase 4 — Cog/Gauge/reviews

### User Story Dependencies

- **US1 (personalized ranking)**: Needs Phase 1 predecessors + Phase 2 handler (three-port acquisition + selector offset) + Phase 3 route
- **US2 (cold start)**: Same Core/API surfaces; distinct fixtures via `Movies` history predicate
- **US3 (take/OpenAPI)**: Infrastructure validator + route take behavior in Phases 2–3; host composition T029a; contract artifacts (`types.ts` + `handlers.ts`) in Phase 4

### Parallel Opportunities

- Phase 1 technical STOP is already cleared; T013 remains unclaimed and may proceed separately at Build
- T014–T015 and T019–T020 marked [P] after Phase 2 start authorization (disjoint new files; T020 is Infrastructure path)
- Phase 3 integration tests are **serialized** on shared files: T024 → T025 → T026 on `RecommendationEndpointTests.cs`; T027 → T028 on `ApiEndToEndRecommendationTests.cs`; T029 after both suites’ for-you extensions (T026 and T028); T029a after T030–T031 on `ApiHostCompositionTests.cs`. No `[P]` on T024–T029a
- Do not parallelize OpenAPI regeneration with hand edits to generated files (forbidden)

---

## Parallel Example: After Phase 2 start authorization (Core tests)

```powershell
# After Quill alignment + applicable pipeline approvals, these test files can be authored in parallel:
# GetForYouRecommendationsQueryTests.cs
# GetForYouRecommendationsQueryValidatorTests.cs
# (handler tests depend on query/validator shapes)
```

---

## Implementation Strategy

### MVP path

1. Phase 1 technical STOP cleared (T001–T011, T012, T013a) + Quill alignment complete
2. Applicable separate pipeline approvals (plan freeze / Gate 1 / source authorization)
3. Phase 2 Core query/handler + Infrastructure validator + unit/FsCheck (Phase B proofs) → commit
4. Phase 3 API + integration + host composition → commit
5. Phase 4 OpenAPI + `generate:api` (`types.ts` and `handlers.ts`) → commit
6. Phase 5 Build gates and M-tier reviews

### Forbidden shortcuts

- No skipping Quill alignment or applicable pipeline approvals before Phase 2
- No new port/adapter without cited Patron amendment
- No substitute algorithms, production stubs, or dependency waiver
- No invented predecessor signatures or generation flags
- No hand-edits of generated `openapi.json` / `types.ts` / `handlers.ts`
- No FluentValidation in Core or new package for the validator
- No forced host-test completeness/count claims beyond authorized route equality + additive ManifestContracts row
- No arbitrary candidate cap / `CandidatePoolSize` truncation of the eligible universe
- No clamping output blend scores or claiming atomic cross-query snapshots
- No For You UI (DEV-418)
- No plan-freeze or Gate 1 claim from drafting or aligning these tasks

---

## Notes

- Closing bar: Q3 — Critical/High (plus unmet acceptance/gates/axes/escalations/missing contracts) block; Medium/Low cannot waive acceptance/gate otherwise non-blocking; two remediation rounds total
- Scope: anything else is a follow-up issue, not a finding in this round
- Frontend rail: DEV-418
- Phase A outstanding (not part of Phase 1 technical STOP): Keel read-only `/speckit-analyze`, plan challenge, separate plan freeze, separate Gate 1 — brief Q9; Phase B amendment grants none of these
- Phase B refinements encoded here: three-port acquisition, vectorless retention, selector-only MMR offset, negative-score preservation, per-query consistency, T001/T002 certification
- Unresolved semantic product choices beyond the brief → `needs decision` to Keel; Quill never fills them
