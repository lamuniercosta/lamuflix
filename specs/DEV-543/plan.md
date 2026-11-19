# Implementation Plan: For you nearest-seed ranking

**Branch**: `feature/543-spec` | **Date**: 2026-11-19 | **Spec**: `specs/DEV-543/spec.md`

**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-543-spec`

**Input**: Grill-closed `brief.md` (Q1–Q8); `CONCLUSIONS.md`; `ASSUMPTIONS.md`; Maestri note `DEV-543`; the authoritative DEV-543 YouTrack export provided by the Conductor in disposable `specs/DEV-543/ticket-text.txt`; historical intake pointers `recon-DEV-543` / `recon-DEV-543-2` (see `DRAFTING_RECEIPT.md` for pickup evidence); `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: **PLAN FROZEN** after the three-axis challenge in [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md). Gate 1, implementation and runtime acceptance are pending.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `checklists/ranking.md`, `EVIDENCE.md`, `DRAFTING_RECEIPT.md`. No `research.md`, `data-model.md`, or `contracts/` — decisions frozen in the brief; no public API delta. ADR text is Keel-owned Proposed draft at `docs/adr/0047-for-you-nearest-seeds.md` (delivery PR with T017–T019).

## Summary

Replace For you profile-centroid ranking with DEV-518 nearest-seed scoring over liked / watched-without-review / watchlist seeds, admit only strong-match candidates, explain each card with its best strong seed, remove Related taste-profile score and reasons, and land one-library-load / bounded-cache / shared-options / decay-helper work with caller-gated legacy cleanup, ADR/glossary updates, and DEV-545 quality and speed evidence.

## Technical Context

**Language/Version**: C# / .NET 10 (`LamuFlix.Core`, `LamuFlix.Infrastructure`, `LamuFlix.Api`) per `specs/PRODUCT.md` §1.

**Primary Dependencies**: Existing stack only — `SimilarityScorer` / DEV-518 similarity, DEV-544 library feature load and shared strong-match options (prerequisite merges), recommendation handlers, EF/hybrid profile sources, xUnit/Shouldly/Testcontainers. No new NuGet/npm package or version change (`PRODUCT.md` §5 care list item 1; recon charter screen).

**Storage**: Existing PostgreSQL movie/profile/playback/watchlist data. No schema or migration change. Cache shape follows DEV-510 trim — cached payload must not grow with library size × embedding size.

**Testing**: Existing xUnit unit + integration projects; DEV-545 snapshot / leave-one-out harness for quality and warm runtime evidence; Compose UI smoke for SMOKE-001/SMOKE-002 (verification only; no `/web` source edits). Never mock EF driver translation for persistence behaviour. OpenAPI contract snapshot must stay unchanged.

**Target Platform**: Local/home API and Compose stack; home library fixture 2026-11-16 for acceptance pairs.

**Project Type**: Existing ports-and-adapters .NET API vertical slice under `Features/Recommendations`. No `/web` contract changes (reasons travel in existing list until DEV-542).

**Performance Goals**: For you warm under 500 ms with 450 movies and 150 liked movies (Wanted 13 / brief Acceptance), measured via DEV-545 snapshot script.

**Constraints**: Charter §2.3 items 1–6 deliberately screened (recon). Zero public API/DTO/OpenAPI/TS change. DEV-542/541/518-similarity/544-pool/539/540 out of scope. Closing bar includes Low; Size L requires Sentry/Ledger/Compass; max two review/fix rounds (Q7–Q8). Incomplete caller inventory → cleanup conditional on fresh checks (Phase A may challenge the conditional plan; Phase B checks the planned guard before deletions). Edits outside the authorized Files boundary require the existing recon/ruling process.

**Scale/Scope**: Size L; For you ranking + reasons + Related profile removal + load/cache/options + cleanup + docs/evidence. Downstream blockers: DEV-542, DEV-541, DEV-540, DEV-539, DEV-538.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No owner structural checkbox from this draft. ADR numbering is resolved (0047); not a constitution departure.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **PASS** — reuse existing recommendation ports (`IMovieCatalog`, `IMovieRepository`, `IRecommendationProfileSource`, DEV-544 library feature load); no new architectural layer. |
| II (Explicit Handlers) | Handlers + AddHandler | **PASS** — change existing `GetForYouRecommendationsQueryHandler` / `GetRecommendationsQueryHandler`; no MediatR. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter / `MovieQuery` change. |
| IV (Domain transitions) | Movie mutators | **PASS** — ranking/read path only; no Movie state-machine change. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no public API/error-contract change; options remain typed/validated. |
| VI (Observability) | Safe diagnostics | **PASS** — no new secretful telemetry; evidence artifacts scrubbed of credentials. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated; TimeProvider | **PASS** — new RecommendationOptions settings use typed data-annotation / `IValidatableObject` validation with existing typed binding + `ValidateOnStart()` startup validation; **verify the live binding/registration path at Phase B pickup (T001)** rather than treating any planning-time helper symbol as authoritative; extracted recency path uses injected `TimeProvider` for current timestamps and preserves existing age semantics; no LocalPlay, secrets, or Process.Start. Any edit outside the authorized Files boundary routes through recon/ruling first. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — rewrite Taste Profile definition in same delivery PR; Proposed ADR 0047 Keel-drafted at claimed path and preserved for delivery (T017–T019), not landed separately at Gate 1. |
| IX (Test Pyramid) | Meaningful layers | **PASS** — unit for ranking/reasons/Related; integration for load/watchlist/endpoint; DEV-545 for quality/runtime; Compose UI smoke for SMOKE-001/002. |
| Tech stack / API docs | OpenAPI chain | **PASS** — existing `reasons` list carriage; OpenAPI snapshot unchanged (FR-005/SC-008). |
| Governance / Static-Analysis Gates | Honest exits | Phase B: build/test, Roslyn, complexity (incl. refactor obligation when applicable), InspectCode, property, vulnerable packages, format, mutation when `src/` changes; web SKIPPED if `/web` untouched; never fold non-pass into PASS. |

### Complexity Tracking

No constitution violations requiring justification. Dead-member removal is gated on caller checks (Wanted 11 / brief).

## Approach

- **Phase A**: The grill and plan challenge establish the delivery decisions in the brief and adjudication. Spec PR: Bernstein coordinates; Rigger prepares/opens; user merges; Keel records Gate 1 merge receipt. No Build work before Gate 1. Proposed ADR 0047 is claimed and drafted for **delivery** — Gate 1 does not land it and does not depend on an open ADR number.
- **Phase B pickup**: Fresh drift recon after Gate 1; confirm DEV-518 / DEV-544 / DEV-510 landed contracts; **verify typed options binding + ValidateOnStart wiring for RecommendationOptions**; **check the already-planned cleanup guard** (complete Wanted 11 caller inventory and record dispositions) before any deletions; Gauge baseline; capture DEV-545 before metrics (**T002**) before **T003/T004/T005** and any behaviour-changing work.
- **Task order (`brief.md` Task order)**:
  1. Verify prerequisite merges; pin DEV-544 load + strong-match options + DEV-518 scorer contracts; finish caller inventory for Wanted 11; capture DEV-545 baseline (**T002 hard gate** before T003/T004/T005 and all subsequent ranking/Related behaviour changes).
  2. Wire one-call feature load, shared options (typed ValidateOnStart contract verified at pickup), shared decay helper (injected TimeProvider; preserve age semantics), cache/read behaviour; build/test seed classification, scoring, strong-match qualification.
  3. Wire For you ranking and reasons through the existing endpoint and run T025. Switch Related scoring and reasons, verify stable scores for surviving results after unrelated reviews, and verify disliked exclusion. Remove ticket-named dead members after both rails stop using them and caller checks pass.
  4. Prepare the ADR 0034 supersession, CONTEXT Taste Profile rewrite and reason table (T018/T019) using the proposed ADR 0047 draft. Then verify all changes appear in the same delivery diff (T017). Keep downstream blockers visible.
  5. Run DEV-545 acceptance, applicable repo gates, and Compose UI SMOKE-001/SMOKE-002 with recorded build identity/fixture proving both rail changes; close Size L review only after T021/T022/T024 plus Q7.
- **Do not**: change OpenAPI/DTO shape; retune strong-match bar without DEV-545 evidence; pad empty results; invent ADR numbers; delete Wanted 11 members without caller checks; edit outside authorized Files without recon/ruling.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-543/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── spec.md
├── plan.md              # This file (worktree / Spec PR copy)
├── tasks.md
├── EVIDENCE.md
├── DRAFTING_RECEIPT.md
└── checklists/
    ├── requirements.md
    └── ranking.md

docs/adr/
├── 0034-explicit-movie-reviews.md          # mark signed formula superseded
└── 0047-for-you-nearest-seeds.md           # Proposed; Keel-drafted; Conductor-claimed; preserved for delivery PR (historical ticket/grill named 0045; DEV-545 took 0045)

CONTEXT.md                                  # Taste Profile entry rewrite (:143-145)
```

No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief; no public API delta.

### Source Code (authorized boundary — brief Files table; recon Where it lands)

```text
src/LamuFlix.Core/Features/Recommendations/
├── GetForYouRecommendationsQueryHandler.cs   # publish signed-aggregate order before request limit
├── NearestSeedScorer.cs                      # pure scoring, qualification, best strong seed
├── RecommendationRecencyDecay.cs             # shared existing decay and age semantics
├── GetRecommendationsQueryHandler.cs         # Related: drop profile score/reasons
├── RecommendationReasonBuilder.cs            # seed templates; drop For you profile/fallback templates
├── MovieFeaturesMapper.cs                    # includeSignals / full cast / keywords / collection
├── TasteProfileCalculator.cs                 # extract shared decay; remove centroid when callers clear
├── BlendScorer.cs                            # remove unused overload / For you profile call after checks
├── ProfileSimilarityCalculator.cs            # remove after For you/Related callers clear
├── RecommendationScoring.cs                  # remove ProfileContribution after checks
└── SimilarityScorer.cs                       # consume landed DEV-518 scorer

src/LamuFlix.Core/Domain/TasteProfile.cs      # remove centroid/review fields after checks
src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs  # shrink to remaining playback/recency needs after checks
src/LamuFlix.Core/Options/RecommendationOptions.cs
src/LamuFlix.Core/Options/SimilarityWeights.cs  # remove review-channel options after checks; DEV-518 owns year/runtime/rating

src/LamuFlix.Infrastructure/Persistence/
├── EfRecommendationProfileSource.cs          # playback/recency; no second read on miss; watchlist via DEV-544 load
└── HybridCachedRecommendationProfileSource.cs # DEV-510 cache shape; no library×embedding growth

tests/LamuFlix.UnitTests/Features/Recommendations/
├── GetForYouRecommendationsQueryHandlerTests.cs
├── ProfileSimilarityCalculatorTests.cs       # replace/remove baseline tests
├── TasteProfileCalculatorTests.cs            # shared decay; centroid removal
├── GetRecommendationsQueryHandlerTests.cs
├── NearestSeedScorerTests.cs
├── NearestSeedScorerPropertyTests.cs
└── RecommendationRecencyDecayTests.cs

tests/LamuFlix.UnitTests/Options/RecommendationOptionsTests.cs

tests/LamuFlix.IntegrationTests/
├── EfRecommendationProfileSourceTests.cs
├── RecommendationEndpointTests.cs
└── OpenApiContractTests                      # snapshot unchanged
```

### Implementation checks

- **Scoring and order**. The new pure scorer uses DEV-518 pair results and the brief's finite-input rules. The handler publishes descending signed scores with existing secondary tie keys where applicable, then ascending movie ID. Drop only the For you MMR/genre-weight selection call. Related selection, DEV-544 pool and shared selectors/options stay in their existing scope.
- **Inputs and validation**. Join playback by movie ID from the retained playback-only profile path. Wisp pins the actual read, cache payload, invalidation and typed startup-validation contracts at pickup. T005/T016 exercise production composition, freshness and cache bounds. T003 pins validation and defaults. Keep scoring defaults in options and reason templates as constants. Avoid mutable static initialization under Stryker.
- **Evidence before Related**. T025 runs the early warm 450/150 probe through the DEV-545 snapshot path and records the evaluated pair count, admitted count and latency. For C candidates, P positive and D disliked seeds, the full scoring bound is C × (P + D) minus positive self exclusions. Count evaluations in the probe without permanent telemetry. T021 reruns acceptance on the final build.
- **Removal boundary**. T001/T014 record the exact item-by-item inventory and dispositions, including named challenger leads in the adjudication. Any additional tracked file rewrite or deletion needs a cited Patron decision, with no deletion while any caller is unresolved. Production files and tests listed above are bounded by the tasks that name them.

## Phase 0 / Phase 1 design outputs

Skipped as separate `research.md` / `data-model.md` / `contracts/`: grill Q1–Q8 frozen the decisions; no new public contract. Constitution Check re-evaluated above after design-in-plan (III = N/A; VII = typed ValidateOnStart + TimeProvider, binding verified at pickup).

## EVIDENCE note

Fill `specs/DEV-543/EVIDENCE.md` slots in Phase B: prerequisite merge pins, caller inventory, DEV-545 before/after quality + warm runtime, Compose SMOKE-001/SMOKE-002 outcomes with build identity/fixture and both rail changes, gate outcomes with honest vocabulary, review dispositions. Never commit credentials or connection strings.
