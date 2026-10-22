---
description: "Task list for DEV-354 multi-factor blend scoring and personal reason contract"
---

# Tasks: Multi-factor blend scoring with personal reason contract

**Input**: Design documents from `specs/DEV-354/` (`spec.md`, `plan.md`, closed `brief.md` Q12)

**Prerequisites**: Closed grill Q12; drafted `spec.md` and `plan.md`. Plan freeze and Gate 1 are **separate later decisions** — not granted by these tasks.

**Tests**: Required per Q11 proof matrix (unit/property, PostgreSQL adapter, endpoint/E2E/OpenAPI, architecture, affected web).

**Organization**: Phase A (spec-only follow-ons) then Phase B STOP gate, then dependency-aware implementation order from plan.md / brief Q11.

**Evidence**: branch `feature/354-spec`; HEAD `d794a0e0643a012d877e736349e02a2af59d9691`; intake base `53f4205d901a1798f41077bbd67127305dff301c`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 blend, US2 reasons/contract, US3 Not Interested exclusion
- Include exact file paths in descriptions

## Path Conventions

Repository root layout: `src/`, `tests/`, `web/` as in plan.md authorized ledger.

---

## Phase 0: Phase A specification follow-on (no source edits)

**Purpose**: Complete specification pipeline without claiming freeze/Gate 1 or running implementation gates.

- [ ] T001 Keel read-only `/speckit-analyze` and brief conformance against `specs/DEV-354/spec.md`, `plan.md`, `tasks.md`, and controlling brief Q12
- [ ] T002 Required plan challenge (Keel) against closed Q1–Q12 ledger; return `needs decision` / gaps without inventing scope
- [ ] T003 Separate plan-freeze decision (not assumed by this task list)
- [ ] T004 Separate Gate 1 / gate1: provisional decision (not assumed by this task list)

**Checkpoint**: Drafts analyzed; freeze/Gate 1 only if separately granted. No production/generated edits yet.

---

## Phase 1: Phase B STOP — readiness recon (blocks all implementation)

**Purpose**: Prove prerequisites before any authorized source change.

**⚠️ CRITICAL**: No US1/US2/US3 implementation until this phase passes. On failure → stop for dependency recon; no waiver, empty-profile substitute, invented vector store, or selected-cosine reuse.

- [ ] T005 Confirm merged specs required for this delivery are available
- [ ] T006 Confirm Epic 7 sequencing allows DEV-354 implementation
- [ ] T007 Fresh bounded main drift/dependency recon (do not treat evidence HEAD `d794a0e` source absences as still true without re-check)
- [ ] T008 Verify compatible landed DEV-338, DEV-348, and DEV-353 contracts
- [ ] T009 Verify **real candidate AND contributor 384-vector acquisition** surfaces exist; if either missing → STOP
- [ ] T010 [P] Phase B recheck only (do not invent): confirm unread rendered/tool config still matches prescribed gates — `CodeMetricsConfig.txt`, `.editorconfig`, `_gate-common.ps1`, and any resolved `gates.mutation.runner` / `concurrency` defaults (recon-DEV-354-3:33,51). `.config/dotnet-tools.json` and `stryker-config.json` verified byte-identical HEAD vs origin/main (recon-DEV-354-3:51,79; DEV-354:66) — not unread; does not clear T009 STOP / Phase B NOT READY. Options/scorer-error policy and handler/`GenreWeightsCalculator` registration facts are already pinned in plan.md — not re-opened here
- [ ] T011 [P] Apply prescribed gate commands/settings from plan.md / recon-DEV-354-3:24-51 as the Phase B verification ledger (prescribed, not executed at draft time); keep PASS / SKIPPED (scope-empty) / SKIP / N/A / Could not run distinct; recheck harness wiring before claiming readiness

**Checkpoint**: Phase B STOP cleared (or explicitly blocked). Only then proceed to Phase 2+.

---

## Phase 2: Foundational options, typed contracts, pure calculators (US1 + US2 foundations)

**Purpose**: Coefficient config; **atomic** Reason.Kind / Structural-producer / DTO / endpoint-mapper contract (compile-safe before checkpoint); then pure scorers with focused tests — before handler orchestration.

### Tests

- [ ] T012 [P] [US1] Add `tests/LamuFlix.UnitTests/Features/Recommendations/BlendScorerTests.cs` (formula, binary penalties, negative/>1 raw scores; `ArgumentOutOfRangeException` for negative/nonfinite blend members; `InvalidOperationException` without clamp for nonfinite calculated raw Score; N=1 allowed in pure tests). Pure tests supply W as a 0.0/1.0 numeric input — do **not** assert candidate-source mapping here (handler W mapping is T032/T034)
- [ ] T013 [P] [US1] Add `tests/LamuFlix.UnitTests/Features/Recommendations/BlendScorerPropertyTests.cs`
- [ ] T014 [P] [US1] Add `tests/LamuFlix.UnitTests/Features/Recommendations/ProfileSimilarityCalculatorTests.cs` (weighted Jaccard, channel combinations, cosine extremes, malformed rejection; no selected-cosine substitution)
- [ ] T015 [P] [US1] Add `tests/LamuFlix.UnitTests/Features/Recommendations/ProfileSimilarityCalculatorPropertyTests.cs`
- [ ] T016 [P] [US1] Extend `tests/LamuFlix.UnitTests/Options/RecommendationOptionsTests.cs` for a/b/c/d defaults and `IValidatableObject` field-level `ValidationResult`s (finite nonnegative members; `a=b=0` field-level failure; preserve BindAndValidate / DataAnnotations / ValidateOnStart path)

### Implementation

- [ ] T017 [P] [US1] Extend `src/LamuFlix.Core/Options/RecommendationOptions.cs` with `BlendSelectedWeight`, `BlendProfileWeight`, `AlreadyWatchedPenalty`, `NotInterestedPenalty` (defaults 1.0, 1.0, 0.25, 1.0); preserve `IValidatableObject` and add finite/nonnegative member plus `a=b=0` field-level `ValidationResult`s (do not validate via `a+b` sum)
- [ ] T018 [US2] **Atomic Reason.Kind contract step (must complete before Phase 2 checkpoint; not parallel with T019/T022)**: extend `src/LamuFlix.Core/Features/Recommendations/Reason.cs` with `Kind` (`Structural` | `Personal`) and caller-safe `Text`; update all existing Structural producers including `src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs` to emit `Kind=Structural` without changing DEV-338 selected-score semantics or reason order and **without** a Structural-defaulting constructor; update `src/LamuFlix.Api/Endpoints/RecommendedMovieDto.cs` Reasons to `{kind,text}` lowercase wire kinds (**RecommendedMovieDto.cs assigned only here**); update `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs` mapper to wire `{kind,text}`; apply the minimum fixture/test adaptation in `SimilarityScorerTests.cs`, `SimilarityScorerPropertyTests.cs`, `SimilarityScorerInvariantPropertyTests.cs`, `SimilarityScorerGoldenTests.cs`, `SimilarityScorerGoldenFixture.cs` and any other broken Structural call sites required to compile and test. T019/T022 are dependency-linked parts of this same atomic step (not independent [P] work).
- [ ] T019 [US2] Dependency-linked with T018 (same atomic contract; **not** [P]): complete `RecommendedMovieDto.cs` + `RecommendationEndpoints.cs` mapper half of T018 if split for authorship — file ownership remains T018; do **not** re-assign `RecommendedMovieDto.cs` later (T044 does not own it)
- [ ] T020 [US1] Add `src/LamuFlix.Core/Features/Recommendations/BlendScorer.cs` (raw `a*S+b*P-c*W-d*N`; direct construction; `ArgumentOutOfRangeException` for negative/nonfinite members; `InvalidOperationException` without clamp for nonfinite calculated raw Score). Depends on typed contracts compiling (T018 atomic step complete)
- [ ] T021 [US1] Add `src/LamuFlix.Core/Features/Recommendations/ProfileSimilarityCalculator.cs` (Q6 policy; direct construction). Depends on typed contracts compiling (T018 atomic step complete)
- [ ] T022 [US2] Dependency-linked with T018 (same atomic contract; **not** [P]): finish Structural producer + golden/unit fixture adaptation named in T018 (`SimilarityScorer.cs` + `SimilarityScorer*.cs` / golden fixtures) without changing DEV-338 selected-score semantics or order; no Structural-defaulting constructor
- [ ] T023 Necessary options wiring only in `src/LamuFlix.ServiceDefaults/Extensions.cs` — preserve existing BindAndValidate / ValidateDataAnnotations / ValidateOnStart for `RecommendationOptions` (recon-DEV-354-3:16-17, `:90`)

**Checkpoint**: Atomic Reason.Kind + Structural producers (`SimilarityScorer.cs`) + DTO + endpoint-mapper contract compiles and minimum Structural fixtures/tests pass; pure formula and profile similarity proven; typed Reason/DTO shapes exist end-to-end on wire mapping. No handler blend or Personal/fallback emission required yet (T043).

---

## Phase 3: Profile acquisition port and EF adapter (US1 dependency)

**Purpose**: Materialize watched/launch/genre/(vector when landed) inputs; real PostgreSQL proof.

### Tests

- [ ] T024 [US1] Add `tests/LamuFlix.IntegrationTests/EfRecommendationProfileSourceTests.cs` — real PostgreSQL: watched-only, launch-only, multiple launches, watched+launch without duplicate undated signal, genres, cancellation
- [ ] T025 [US1] Vector roundtrip cases — only after T009 cleared; never mock driver; never empty-profile substitute

### Implementation

- [ ] T026 [P] [US1] Add `src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs` (cancellation-aware)
- [ ] T027 [P] [US1] Add `src/LamuFlix.Core/Features/Recommendations/RecommendationProfileInputs.cs` (immutable TasteProfileMovie rows + candidate-ID→384-vector map)
- [ ] T028 [US1] Add `src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs` with NEW PlayedAt read query; read existing `is_watched` / genres; no entity/config/writer/schema edits
- [ ] T029 [US1] Scoped-compatible registration of `IRecommendationProfileSource` / `EfRecommendationProfileSource` beside existing sources in `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` (retain existing movie/playback/candidate scoped registrations; do not touch `GenreWeightsCalculator` TryAdd at `MovieCatalogServiceCollectionExtensions.cs:14`)

**Checkpoint**: Adapter returns real materialized inputs for non-vector fields; vector path only if T009 passed.

---

## Phase 4: User Story 3 + User Story 1 handler orchestration — exclusion, blend, order (Priority: P1)

**Goal**: Exclude Not Interested before scoring; build profile once; blend and order eligible candidates.

**Independent Test**: Mixed/high-similarity/all-excluded fixtures + FsCheck; Score-desc/Id-asc/Take; N=0 on handler output.

### Tests

- [ ] T030 [P] [US3] Extend `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs` for pre-scoring exclusion (mixed, high-similarity, all-excluded); no spy-only abstraction
- [ ] T031 [P] [US3] Extend `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerPropertyTests.cs` FsCheck: Not Interested never scored/returned; d never substitutes
- [ ] T032 [US1] Handler fixtures for raw finite blend, profile-once construction, ordering without refill; prove W mapping: `W = 1.0` iff candidate `Movie.IsWatched` is true else `0.0` via existing candidate source/summary (recon-DEV-354:13,16) — distinct from pure BlendScorer W-input tests in T012; no new W query

### Implementation

- [ ] T033 [US3] Update `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs` to exclude `Movie.IsNotInterested` before scoring/reasons
- [ ] T034 [US1] In same handler: add approved `IRecommendationProfileSource` dependency; acquire profile inputs; construct `TasteProfile` once via DEV-353 `TasteProfileCalculator` + existing `GenreWeightsCalculator` (direct calculator calls; current ctor already takes `GenreWeightsCalculator` — recon-DEV-354-3:12 / DEV-354:59); compute S/P/W/N and Score via `BlendScorer` / `ProfileSimilarityCalculator` (direct construction; no artificial DI); pin `W = 1.0` iff candidate `Movie.IsWatched` else `0.0` from existing candidate source/summary — no new W query; contributor launch history remains profile-port only
- [ ] T035 [US1] Preserve Score descending, Movie.Id ascending, then Take; no refill/query widening
- [ ] T036 Update `src/LamuFlix.Core/Features/Recommendations/RecommendedMovie.cs` / `SimilarityResult.cs` as needed for Score + typed Reasons carrying
- [ ] T037 Necessary handler composition only in `src/LamuFlix.Api/HandlerRegistration.cs` — retain existing `AddHandler<GetRecommendationsQueryHandler, GetRecommendationsQuery, ImmutableList<RecommendedMovie>>()` at `:49` (DEV-354:59); do not invent a second registration path. Keep `GenreWeightsCalculator` scoped `TryAdd` at `MovieCatalogServiceCollectionExtensions.cs:14`

**Checkpoint**: US3 invariant holds; US1 blend ranking works on eligible candidates with materialized profile inputs.

---

## Phase 5: User Story 2 — reasons emission and API/web contract (Priority: P1)

**Goal**: Every result has deterministic Structural-then-Personal reasons; wire `{kind,text}`; regenerate OpenAPI/TS/mocks; migrate consumers.

**Independent Test**: Cold-start/fallback, genre/history Personal when b>0, endpoint/E2E/web consumers show lowercase kinds and safe generic text without constructing explanations.

### Tests

- [ ] T038 [US2] Handler/reason fixtures: Structural order, generic Personal templates only when b>0, one Personal genre reason per canonical positive-mass overlapping genre in OrdinalIgnoreCase then Ordinal order (multi-overlap fixture distinguishes one-per-genre, not first-only), Structural fallback `No shared features identified`, no named-title template
- [ ] T039 [P] [US2] Update `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` for eligible ordering, raw finite scores, nonempty `{kind,text}`
- [ ] T040 [P] [US2] Update `tests/LamuFlix.IntegrationTests/ApiEndToEndRecommendationTests.cs` similarly
- [ ] T041 [P] [US2] Update `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` for dependency boundaries as needed
- [ ] T042 [US2] Update affected web tests **after** T048–T049 (generated-type consumer tests depend on generation/freshness and migration): `web/src/features/movie-details/RelatedMovies.test.tsx`, `web/src/mocks/recommendations.test.ts`, `web/src/mocks/handlers.test.ts`, `web/src/mocks/generate-api.test.ts`

### Implementation

- [ ] T043 [US2] Emit Personal/fallback reasons in the handler path only: Personal `Matches your <genre> interest` / `Matches your viewing history` only when authorized; Structural fallback text when otherwise empty. Existing DEV-338 + DEV-348 Similar-story Structural emission and Kind tagging remain owned by the Phase 2 atomic contract (T018/T022 on `SimilarityScorer.cs`); do not re-open RecommendedMovieDto ownership
- [ ] T044 [US2] No separate DTO remapping — `RecommendedMovieDto.cs` and `RecommendationEndpoints.cs` mapper already owned by the Phase 2 atomic contract (T018/T019). After T043 Personal/fallback emission, confirm wire `{kind,text}` still maps both kinds through the existing mapper (do **not** re-assign or rewrite `RecommendedMovieDto.cs`)
- [ ] T045 [US2] Extend existing single writer in `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs`: retain `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` / `WriteRegeneratedOutput` gated by case-insensitive `LAMUFLIX_REGENERATE_OPENAPI=true`; reuse `RepositoryRoot` (≤8 ancestors, exactly one `Directory.Packages.props`), absolute baseline/snapshot paths, `IsWithinRoot`; migrate reasons assertion to `{kind,text}`; no duplicate NEW exporter / `LAMUFLIX_OPENAPI_EXPORT`
- [ ] T046 [US2] After DTO/assertion changes (Phase 2 atomic T018/T019 + T045), from repo root run opt-in paired regeneration; clear env **even on failure**:

  ```powershell
  $env:LAMUFLIX_REGENERATE_OPENAPI = 'true'
  try {
    dotnet test tests/LamuFlix.IntegrationTests --filter "FullyQualifiedName~OpenApiContractTests.OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether" --nologo
  }
  finally {
    Remove-Item Env:LAMUFLIX_REGENERATE_OPENAPI -ErrorAction SilentlyContinue
  }
  ```

  Assert **both** `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` receive normalized registered `/openapi/v1.json` with authorized `{kind,text}` Reasons
- [ ] T047 [US2] Final ordinary no-export drift/equality/no-write checks: env unset (and cover non-true), `dotnet test tests/LamuFlix.IntegrationTests --filter "FullyQualifiedName~OpenApiContractTests" --nologo`; prove neither baseline nor snapshot is written (byte equality alone is insufficient no-write proof); host/baseline/snapshot equality and `{kind,text}` assertions hold. Depends on T046 env cleared
- [ ] T048 [US2] **Only after T047**: from `web/`: `npm run generate:api` then `npm run check:api` (never hand-edit `web/src/api/types.ts` or `web/src/mocks/handlers.ts`). Depends on T046 paired artifacts + T047 ordinary no-export green
- [ ] T049 [US2] **Only after T048**: migrate `web/src/mocks/fixtures.ts` and `web/src/features/movie-details/RelatedMovies.tsx` to consume generated reason objects without constructing explanation strings; then run dependent consumer tests (T042). Consumer tests depend on generation/freshness (T048) and migration (this task)

**Checkpoint**: US2 contract complete end-to-end; paired regenerate → clear env → ordinary no-export → generate:api → check:api → consumers/tests green.

---

## Phase 6: Full gates, mutation, M-tier review, ship/merge bar

**Purpose**: Cross-cutting verification. Use prescribed gate commands/settings from plan.md / T011 (recon-DEV-354-3:24-51); Phase B recheck unread tool config via T010 — do not invent rendered runner settings or hardcode unverified numbers.

- [ ] T050 Focused unit/property + infrastructure + API/architecture + affected web tests for changed scope
- [ ] T051 Backend build and analyzer gates: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (incl. refactor tightening per harness), `./scripts/run-jetbrains-inspectcode.ps1`
- [ ] T052 Property, vulnerable-packages, `dotnet format --verify-no-changes`, full `dotnet test`
- [ ] T053 Current web gates for touched web scope
- [ ] T054 Pre-PR mutation per harness; fix survivors with tests — never lower threshold; report PASS / SKIPPED / SKIP / N/A / Could not run distinctly
- [ ] T055 M-tier reviews: Sentry, Ledger, Compass under Q2 bar (Critical/High/Medium block; Low non-blocking; max 2 rounds / 2 fix commits per round)
- [ ] T056 Ship review, hosted checks, merge bar (separate); user-owned merge

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0**: Spec follow-on only; no source implementation
- **Phase 1 (STOP)**: Blocks all Phase 2+ source work
- **Phase 2**: After STOP clear — options; **atomic** Reason.Kind + Structural producers + DTO + endpoint-mapper (T018/T019/T022, no conflicting [P]) before Phase 2 checkpoint; then pure calculators/tests
- **Phase 3**: Adapter — after Phase 2 types exist; vectors only if T009 passed
- **Phase 4 (US3+US1 handler)**: After Phase 2–3; W from candidate `Movie.IsWatched` via existing candidate source/summary
- **Phase 5 (US2 contract)**: After Phase 4; Personal/fallback emission (T043) uses Phase 2 mapper (T044 does not re-own DTO); order is mandatory: author export/assertions (T045) → opt-in paired regenerate (T046) → clear env → final ordinary no-export (T047) → web generate:api/check:api (T048) → consumer migration + dependent consumer tests (T049/T042)
- **Phase 6**: After Phase 5

### User Story Dependencies

- **US1 (blend)**: Needs Phase 2 calculators + Phase 3 inputs + Phase 4 handler (W mapping distinct from pure scorer W input)
- **US3 (Not Interested)**: Handler exclusion in Phase 4; can share handler test files with US1
- **US2 (reasons/contract)**: Needs atomic typed Reason/DTO/mapper from Phase 2 (T018/T019/T022) before checkpoint; Personal/fallback emission in T043; OpenAPI paired regenerate/ordinary checks before web generate/check; generated-type consumer tests depend on T048–T049

### Parallel Opportunities

- T012–T016 and T017 marked [P]; T018/T019/T022 are a dependency-linked atomic contract — **not** [P] and must not run as independent parallel file edits
- T026–T027 marked [P]
- T039–T041 API/architecture tests may parallelize after the Phase 2 DTO/mapper atomic step; T042 web consumer tests wait for T048–T049
- Do not parallelize T046–T049 with hand edits to generated files (forbidden); do not run T048 before T047; do not run T042/T049 before T048

---

## Parallel Example: Pure calculator tests

```powershell
# After STOP clear, calculator test files can be authored in parallel:
# BlendScorerTests.cs / BlendScorerPropertyTests.cs
# ProfileSimilarityCalculatorTests.cs / ProfileSimilarityCalculatorPropertyTests.cs
# RecommendationOptionsTests.cs
```

---

## Implementation Strategy

### MVP First (US1 + US3 on API)

1. Phase 0–1 approvals and STOP clear
2. Phase 2–4: blend + exclusion with reasons deferred only if typed Reason already stubbed — prefer emitting fallback Structural early so AC-2 is not skipped
3. Validate handler fixtures/FsCheck independently

### Incremental Delivery

1. STOP clear → pure scorers → adapter → handler exclusion/blend → typed reasons → author OpenAPI assertions/extend existing paired writer → opt-in paired regenerate → clear env → ordinary no-export → web generate/check → consumer migration/tests → gates/reviews
2. Each phase checkpoint must remain honest about vector readiness

### Forbidden shortcuts

- No permanent partial delivery that drops AC-1..3
- No schema/dependency/vector-store invention
- No hand-edits of generated `types.ts` / `handlers.ts`
- No duplicate OpenAPI writer or `LAMUFLIX_OPENAPI_EXPORT` selector
- No plan-freeze or Gate 1 claim from completing draft tasks T001–T004 alone

---

## Notes

- Closing bar: Critical/High/Medium block; Low non-blocking; max two review rounds and two fix commits per round; M-tier Sentry/Ledger/Compass
- Scope: anything else is a follow-up issue, not a finding in this round
- MMR remains DEV-416
- Phase B STOP: merged specs, Epic 7 sequencing, fresh bounded main drift/dependency recon, compatible landed DEV-338/348/353, real candidate AND contributor vectors
- OpenAPI single writer: extend `OpenApiRegeneration_OptInEnabled_WritesTheBaselineAndDerivedSnapshotTogether` / `WriteRegeneratedOutput`; case-insensitive `LAMUFLIX_REGENERATE_OPENAPI=true`; paired writes to `web/src/api/openapi.json` and `Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`; cwd repo root; clear env even on failure; byte equality alone ≠ no-write proof
- Gate commands/settings prescribed from recon-DEV-354-3:24-51; not executed by drafting; Phase B recheck required; do not invent rendered tool config
- Unresolved items stay `needs recon` / `needs decision` — do not invent; do not re-open resolved scorer-error / exporter / registration facts
