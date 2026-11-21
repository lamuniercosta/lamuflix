---
description: "Task list for DEV-543 For you nearest-seed ranking"
---

# Tasks: For you nearest-seed ranking

**Input**: Design documents from `specs/DEV-543/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q8), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `EVIDENCE.md`; Maestri notes `DEV-543`, historical `recon-DEV-543` / `recon-DEV-543-2` (see `DRAFTING_RECEIPT.md` recovery gap); `specs/PRODUCT.md`.

**Status**: **PLAN FROZEN**. Gate 1 verified and T000 complete. The merge receipt is in [CONCLUSIONS.md](CONCLUSIONS.md). Plan challenge dispositions are in [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md).

**Tests**: Required (`brief.md` Acceptance and verification; FR-001–FR-012). TDD optional. Unit + integration suites must pass; OpenAPI snapshot unchanged. DEV-545 harness for quality/runtime evidence. Compose UI smoke for SMOKE-001/SMOKE-002 (verification only; no `/web` source edits) against a recorded build identity and fixture with both rail changes present. Follow aaron-csharp-testing: Method_Condition_Expected names, AAA headers, Shouldly and the existing mocking library. Use Testcontainers for actual persistence behavior.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence tasks are never `[P]`.

**Conventions**: Edit only the authorized boundary in `plan.md` / brief Files table; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface (route via existing recon/ruling); STOP `needs recon` if a required path/fact is missing; STOP on incomplete caller checks before Wanted 11 deletions; no `/web` OpenAPI/TS edits; no explanatory comments beyond AGENTS exemptions; never invent ADR numbers; never lower harness thresholds; never commit secrets. Otherwise-unassigned implementation tasks are owned by the generic Build lane (do not invent named seats). Keel owns ADR authorship; T017 verifies Proposed ADR 0047 lands in the delivery diff with T018/T019.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel T000 records the verified merge receipt. No Build work (T001+) before Gate 1.

**Evidence**: Fill slots in `specs/DEV-543/EVIDENCE.md`. Disposable machine artifacts stay untracked/scrubbed — no secrets or connection strings.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (Wisp pickup recon + Keel reanalysis + Gauge baseline; read-only) — blocks T002
Phase 2   T002          (DEV-545 before baseline — HARD GATE before T003/T004/T005 and all behaviour-changing work)
          T003 ∥ T004   (options + decay helper; AFTER T002)
          T005          (one-load/cache — AFTER T002)
Phase 3   T006 → T010 → T025 (For you ranking, reasons, tests and early timing probe)
Phase 4   T011 → T013   (US3 Related profile removal; after T002 via T007 chain)
Phase 5   T014 → T016   (US4/US5 cleanup after caller checks — Phase B checks planned guard)
Phase 6   T018 ∥ T019 → T017   (US5 prepare supersession, glossary and reason docs, then check the combined delivery diff)
Phase 7   T020 → T021 → T022 → T024 → T023
          (US5 DEV-545 evidence + gates + Compose SMOKE-001/002 with build identity/fixture + both rails → Size L review closure on T021/T022/T024 + Q7)
```

Ordering per brief Task order. **T002 completion is a hard dependency for T003, T004, T005 and all subsequent ranking/Related behaviour-changing tasks.** No dependency cycle. Failed/unavailable runtime expectations remain open — do not tune scores or bars to hide them without DEV-545 evidence + Patron path.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [x] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-543 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-543 in `specs/DEV-543/CONCLUSIONS.md`. Receipt only. T001–T025 MUST NOT proceed without verified merge. Proposed ADR 0047 is claimed and drafted for the **delivery** PR (T017–T019).

---

## Phase 1: Pickup recon (read-only; blocks T002)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; pin DEV-518/544/510 contracts; complete Wanted 11 caller inventory against the **already-planned** cleanup guard; Gauge baseline; verify options binding/startup-validation wiring. No production edits. Phase A may have challenged the conditional cleanup plan; Phase B checks that planned guard before deletions.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; prerequisites merged or stop; binding path for `RecommendationOptions` verified (do not rely on planning-time symbol claims).

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying: `GetForYouRecommendationsQueryHandler.cs`, `GetRecommendationsQueryHandler.cs`, `RecommendationReasonBuilder.cs`, `MovieFeaturesMapper.cs`, `TasteProfileCalculator.cs`, `BlendScorer.cs`, `ProfileSimilarityCalculator.cs`, `RecommendationScoring.cs`, `SimilarityScorer.cs`, `RecommendationOptions.cs`, `SimilarityWeights.cs`, `TasteProfile.cs`, `IRecommendationProfileSource.cs`, `EfRecommendationProfileSource.cs`, `HybridCachedRecommendationProfileSource.cs`, DEV-544 library feature load + strong-match options symbols, DEV-510 cache shape, and a **complete Wanted 11 caller inventory** (historical recon caller inventory incomplete; current note recovery gap in `DRAFTING_RECEIPT.md`). Confirm DEV-518, DEV-544, DEV-510 merged. Confirm Proposed ADR draft path `docs/adr/0047-for-you-nearest-seeds.md` is still present for delivery (not already landed alone). **Verify** the live typed options binding + `ValidateOnStart()` wiring used for `RecommendationOptions` at pickup (record the verified registration path; do not treat any planning-time binding helper name as authoritative until verified). **Keel** reanalyzes brief/spec/plan/tasks against landed main and amends only for authorized demonstrated drift. **Gauge** records pre-edit gate baseline and ensures DEV-545 harness is available. **STOP** `needs recon` on missing seams; **STOP** if prerequisites unmerged. **Strictly read-only**. In EVIDENCE §1 record exact refs, queries and caller results for production, API/DI, appsettings/config, tests and Evaluation, using the adjudication leads. Pin accessible DEV-538/539/540/541/542 work refs or their recorded recon and classify missing evidence before removal. Pin the existing selection and tie keys, landed DEV-544 Related pool/option application, DEV-518 missing-feature contract, playback payload and timestamp join, and all transitions that can change seed state or playback. Record whether each transition needs invalidation under the landed data path and which live path supplies freshness.

---

## Phase 2: Foundational load, options, decay, cache (blocks user-story ranking)

**Purpose**: Shared data path and options without yet switching Related/For you user-visible ranking end-to-end. **T002 must complete before T003/T004/T005 and any behaviour-changing work.**
**Checkpoint**: Before baseline captured; one-load path compilable; shared decay helper covered; options present with typed startup validation; cache growth constraint preserved.
**Story foundation for**: US4 (and enables US1–US3).

- [x] T002 **Build lane**. After T001: Capture DEV-545 before baseline (leave-one-out hit rate/MRR and warm runtime slot) into `specs/DEV-543/EVIDENCE.md` before any scoring behaviour change (`brief.md` Task order step 1; FR-011). **Hard dependency**: T003, T004, T005 and all subsequent ranking/Related behaviour-changing tasks MUST NOT start until T002 is complete. Before any timing, pin the DEV-545 command, fixture, environment, cache state, prescribed warmup and measured iterations in EVIDENCE. Use the brief fallback if the harness has no warmup protocol. Reuse the protocol in T025 and T021 and record individual timings, median and maximum.
- [x] T003 [P] **Build lane**. After **T002** (and T001): Add ticket seed-weight, rank-multiplier, disliked-weight, and recency-floor options (defaults per brief) on `src/LamuFlix.Core/Options/RecommendationOptions.cs` with typed data-annotation / `IValidatableObject` validation under the existing typed binding + `ValidateOnStart()` contract verified at T001; reuse landed DEV-544 strong-match option set; retain `AlreadyWatchedPenalty`; do not remove legacy options until Phase 5 caller-gated cleanup. If binding/validation wiring outside the authorized Files table is required, STOP and route through recon/ruling (FR-003/FR-006/FR-007; Principle VII). Validate finite non-negative weights and multipliers, non-increasing multipliers and a finite floor in (0, 1]. Add valid/default/invalid cases to tests/LamuFlix.UnitTests/Options/RecommendationOptionsTests.cs. Use option defaults or constants and verify DEV-544 still owns validation of the shared bar.
- [x] T004 [P] **Build lane**. After **T002** (and T001): Extract existing exponential decay from `src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs` into a shared helper colocated under `src/LamuFlix.Core/Features/Recommendations/` that uses injected `TimeProvider` for current timestamps and preserves existing age semantics; update `tests/LamuFlix.UnitTests/Features/Recommendations/TasteProfileCalculatorTests.cs` for shared decay + floor 0.25 semantics. If a consumer outside the authorized boundary must change, STOP and route through recon/ruling (FR-006; Principle VII). Name the helper src/LamuFlix.Core/Features/Recommendations/RecommendationRecencyDecay.cs and test it in tests/LamuFlix.UnitTests/Features/Recommendations/RecommendationRecencyDecayTests.cs with FakeTimeProvider.
- [x] T005 **Build lane**. After **T002** (and T001): Wire For you to the DEV-544 library feature load and consistent `MovieFeaturesMapper` signal mapping (full cast, keywords, collection) in `GetForYouRecommendationsQueryHandler.cs` / `MovieFeaturesMapper.cs`; remove paged catalog walk and per-movie `GetAsync`; eliminate second profile-inputs read on cache miss in `EfRecommendationProfileSource.cs` / `HybridCachedRecommendationProfileSource.cs` while following DEV-510 cache shape (no library×embedding growth). Cover in `GetForYouRecommendationsQueryHandlerTests.cs` and `EfRecommendationProfileSourceTests.cs` (FR-007; SC-006). Join retained playback-only inputs by seed movie ID. In EfRecommendationProfileSourceTests.cs use Testcontainers and the real persistence path to prove old and fresh equal-similarity seeds receive different effective weights and no-playback positives stay unscaled. Verify both cache miss and hit and count library/profile reads.

---

## Phase 3: User Stories 1–2 - For you nearest-seed ranking and reasons (Priority: P1) 🎯 MVP

**Goal**: Strong-match nearest-seed ranking with best-seed reasons on the existing endpoint shape.
**Independent Test**: Unit tests listed in brief Acceptance for precedence, watchlist, dislike, recency, aggregation, strong-match boundary, ties, reason order/templates.
**Order**: T006 → T010 → T025. All after T002 via T005.

### Tests

- [ ] T006 [US1] **Build lane**. After **T005** (hence after T002): Replace baseline-centric cases and add coverage in `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs` for: signal precedence; watched+liked as liked; watchlist-only ranking; rank change after watchlisting with unchanged features; self-exclusion; disliked penalty ordering; recency with/without events including floor; top-three aggregation; unweighted strong-match boundary; weak-aggregate rejection; director-only strong match; deterministic ID ties (FR-001–FR-004; SC-004/SC-005). **Not parallel with T005.** Cover watchlisted+disliked, zero negative penalty with no disliked seeds, every individual configured weight and rank multiplier, valid missing-feature and non-finite pair handling, and the actual ordered page when legacy MMR would pick a different order. Add focused pure scoring tests in NearestSeedScorerTests.cs and FsCheck tests in NearestSeedScorerPropertyTests.cs for input-order independence, final ID ties and adding a disliked seed never increasing score.

### Implementation

- [ ] T007 [US1] **Build lane**. After T003–T006 authored: Implement seed classification and nearest-seed signed scoring (top-three positive products minus disliked term) in `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs` using DEV-518 `SimilarityScorer` and shared options; apply ordinary ranking for watchlist-only; return empty with no padding when no positive seeds or no strong pairs (FR-001–FR-003; FR-006). Put pure scoring and selection in src/LamuFlix.Core/Features/Recommendations/NearestSeedScorer.cs, with no new interface. Remove the For you handler MMR/genre-weight stage. Return qualified candidates in signed-aggregate order with existing secondary tie keys where applicable and ascending movie ID before applying the existing limit. Retain existing candidate eligibility and keep Related/shared selection unchanged.
- [ ] T008 [US1] **Build lane**. After T007: Enforce strong-match admission via shared DEV-544 bar/strong-reason policy on raw pair similarity/breakdown before weight/recency/aggregation/penalty; allow fewer results than requested (FR-003; SC-005). Implement admission in src/LamuFlix.Core/Features/Recommendations/NearestSeedScorer.cs. Ignore non-finite pair results for scoring and admission after calling DEV-518. Honor its finite missing-feature behavior.
- [ ] T009 [US2] **Build lane**. After T008: Select best strong seed by recency-adjusted weighted similarity with ascending movie ID tie-break; keep seed id/title/signal with DEV-518 evidence; emit seed template first then evidence in existing `reasons` via `RecommendationReasonBuilder.cs`; exclude the three superseded templates (FR-004–FR-005; SC-002). Keep seed details as inputs to reason construction. Use constant templates. Pass titles as literal text and preserve existing client escaping. The seed path is the only For you producer of the liked seed line.
- [ ] T010 [US2] **Build lane**. After T009: Extend handler tests for reason order, templates by signal kind, superseded-template absence, and best-strong-seed naming even when that pair is outside the top-three score contributions (`GetForYouRecommendationsQueryHandlerTests.cs`; FR-005). Check literal braces and markup in the seed title.

- [ ] T025 [US1] **Build lane**. After T010 and before T011: Run the DEV-545 450-movie / 150-liked warm probe on the completed For you path. In specs/DEV-543/EVIDENCE.md record the T002 timing protocol, identity, timings, evaluated positive and negative pair counts, and admitted candidates. Compare counts to the full pair bound in plan.md. If the probe fails or cannot run, report the specific result to Bernstein before the Related switch so the cost issue can be evaluated while the For you diff is isolated. T021 is still the final acceptance run.

**Checkpoint**: For you MVP ranking+reasons green under unit tests; OpenAPI untouched.

---

## Phase 4: User Story 3 - Related without profile (Priority: P1)

**Goal**: Related uses DEV-518 similarity only (+ watched penalty / disliked exclusion); no profile reasons.
**Independent Test**: Unrelated liked and disliked reviews preserve scores on surviving results. A disliked movie is excluded when it would otherwise be eligible. Existing watched penalty and removal of "Matches your" reasons are covered.
**Order**: T011 → T013. Starts after T025.

- [ ] T011 [US3] **Build lane**. After T025: Switch `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs` off profile blend scoring to DEV-518 similarity with existing `AlreadyWatchedPenalty` and disliked exclusion (FR-008; SC-003). Keep landed DEV-544 pool behavior and existing Related selection. Record the shared policy integration from T001 and add no Related-handler admission filter or global disliked-seed penalty.
- [ ] T012 [US3] **Build lane**. After T011: Remove Related profile reason emission (`RecommendationReasonBuilder.AddProfileReasons` call path) so results carry no "Matches your" profile reasons (FR-008; SC-002).
- [ ] T013 [US3] **Build lane**. After T012: Add/adjust Related handler unit coverage in `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs` proving unchanged scores on existing results after unrelated liked and disliked reviews, absence of "Matches your" reasons, disliked candidate exclusion when the reviewed movie was eligible, and the watched penalty (FR-008).

**Checkpoint**: Both rails free of taste-profile contribution.

---

## Phase 5: User Stories 4–5 - Caller-gated legacy cleanup (Priority: P2)

**Goal**: Remove Wanted 11 dead members only after caller checks; keep `AlreadyWatchedPenalty`. Phase B pickup (T001) already checked the planned cleanup guard; deletions still require recorded clear dispositions.
**Independent Test**: Inventory recorded; no remaining `src` callers for removed members; Related/For you still green.
**Order**: T014 → T016.

- [ ] T014 [US5] **Build lane**. After T011–T013 and T001 inventory: For each Wanted 11 member marked remove/spec-decided under Q2 (except `AlreadyWatchedPenalty`), record caller disposition in `specs/DEV-543/EVIDENCE.md`; **STOP** on any remaining caller (FR-009). Repeat T001 inventory against the current worktree and pinned downstream refs or recon. Record dispositions for each member and dependent test, replay fixture, DI registration and config key. Keep AddLikedReviewReason as a separate disposition, subject to cited Patron approval before deleting it. Stop for any unresolved caller or missing downstream evidence. Obtain a cited Patron ruling before authorizing an outside-boundary rewrite.
- [ ] T015 [US5] **Build lane**. After T014 clear: Remove cleared members from `BlendScorer.cs`, `ProfileSimilarityCalculator.cs`, `RecommendationScoring.cs`, `RecommendationReasonBuilder.cs`, `TasteProfileCalculator.cs`, `TasteProfile.cs`, `RecommendationOptions.cs`, `SimilarityWeights.cs`, and shrink `IRecommendationProfileSource.cs` / EF source as needed; update `ProfileSimilarityCalculatorTests.cs` / `TasteProfileCalculatorTests.cs` accordingly. Do not remove DEV-518-owned year/runtime/rating options (FR-009). Update the cleared dependent tests, Evaluation, DI and configuration only within the stated boundary or after cited Patron authorization from T014. For review-affinity/ranking tests, rewrite only assertions tied to the superseded blend where the test can cover retained behavior. Remove obsolete-only tests only with the applicable disposition and preserve tests for still-live scoring, contracts and compatibility.
- [ ] T016 [P] [US4] **Build lane**. After T015: Confirm integration endpoint compatibility in `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` and persistence coverage still asserts one-load / watchlist seed behaviour (`EfRecommendationProfileSourceTests.cs`) (FR-007; SC-006/SC-008). In EfRecommendationProfileSourceTests.cs verify watchlist add/remove, relevant like/dislike and playback transitions on the next request with cache primed, with fixture pairs that observe the seed change. Assert cached payload excludes per-movie embeddings and does not grow with embedding dimension at fixed library size using the real captured cache-write shape. Record the checked fields, result and test path in EVIDENCE §4.

---

## Phase 6: User Story 5 - Decision record and glossary (Priority: P2)

**Goal**: ADR 0047, ADR 0034's signed-formula supersession, the CONTEXT glossary update and the reason table land in the same delivery PR.
**Independent Test**: Proposed ADR 0047 present in the delivery diff; ADR 0034 signed formula marked superseded; reason table matches builder templates; all in the same delivery PR as T018/T019.
**Order**: T018 and T019 can run in parallel after T016 and confirmation of the ADR 0047 draft at T001. T017 follows both.

- [ ] T017 [US5] **Keel / Build handoff**. After T018 and T019: Keel updates the proposed draft at `docs/adr/0047-for-you-nearest-seeds.md` with the published-order decision, Related boundary and shared-policy evidence requirement from the brief, cites Q1–Q8 and removes the allocation process sentence. Verify the ADR is in the delivery diff with T018/T019 (FR-010; SC-009).
- [ ] T018 [P] [US5] **Build lane**. After T016 and T001 confirmation of the ADR 0047 draft: Mark signed-formula decision superseded in `docs/adr/0034-explicit-movie-reviews.md` citing ADR 0047 (FR-010). Same delivery PR as T017/T019. Retain the ADR 0034 status, since its movie review decision still governs.
- [ ] T019 [P] [US5] **Build lane**. After T016 and T001 confirmation of the ADR 0047 draft: Rewrite Taste Profile entry at `CONTEXT.md` (~lines 143–145) for post-centroid nearest-seed world; ensure reason templates are documented in one place mirroring `RecommendationReasonBuilder` / brief table (FR-010; SC-009). Same delivery PR as T017/T018.

---

## Phase 7: Evidence, gates, smoke, and Size L review (Priority: P1 closing)

**Goal**: DEV-545 acceptance evidence, Compose smoke outcomes (build identity + fixture + both rails), honest gates, then three-axis review closure under Q7 within two rounds.
**Independent Test**: SC-001/SC-007/SC-008/SC-010; closing bar Q7–Q8.
**Order**: T020 → T021 → T022 → T024 → T023. **T023 closure depends on T021, T022, T024 plus Q7.**

- [ ] T020 [US5] **Build lane**. After T009/T016: Run DEV-545 snapshot on 2026-11-16 home library; record top-10 seed diversity (≥4 distinct best seeds) and required pairs (Good/Bad/Ugly→Fistful of Dollars; Hercules→Aladdin; Pirate Radio→About Time); confirm prohibited reason strings absent; attach to `EVIDENCE.md` / PR (SC-001/SC-002).
- [ ] T021 [US5] **Build lane**. After T020: Attach before/after DEV-545 leave-one-out hit rate and MRR (hit rate not lower) and warm For you runtime under 500 ms with 450 movies / 150 liked; record fixture and commands in `EVIDENCE.md` (SC-007; FR-011). **Required before T023 closure.** Repeat the T002 timing protocol and report unrounded timings with the fixture, commands and build identity. Report unmet speed or quality as pending/failing evidence.
- [ ] T022 **Build lane**. After T016 and T017: Run `dotnet test` for UnitTests + IntegrationTests including unchanged `OpenApiContractTests` snapshot; run applicable build/analyzer/complexity/InspectCode/property/security/format/mutation/contract gates under repo thresholds; record honest outcomes in `EVIDENCE.md` (SC-008; FR-011). **Required before T023 closure.**
- [ ] T024 [US5] **Build lane**. After T009 and T012 (For you seed reasons and Related profile-reason removal landed) and after T022 gates are runnable on the candidate build: Execute spec smoke scenarios **SMOKE-001** and **SMOKE-002** against the described Compose UI (Home For you rail; Related rail). Record **build identity** (`git rev-parse HEAD` / image or compose service identity), **fixture** used, and both outcomes in `specs/DEV-543/EVIDENCE.md` §8, proving **both rail changes** are present on that same identity. Verification only — **no `/web` source edits** (FR-011; SC-010). **Required before T023 closure.**
- [ ] T023 **Sentry / Ledger / Compass** then **Build lane** remediation as needed: After **T021**, **T022**, and **T024** are complete, run Size L three-axis review; fix and verify all accepted in-scope findings including Low within two fix/review rounds per **Q7** (and Q8 round cap); record dispositions; anything else is a follow-up issue, not a finding in this round (FR-012; Q7–Q8). **Closure depends on T021 + T022 + T024 + Q7.** Hold phase open if closing bar unmet at cap.

---

## Dependencies / parallel examples

- **T002 hard gate**: T003, T004, T005, T006–T013, and any other behaviour-changing ranking/Related work require T002 complete. Options/decay scaffolding (T003/T004) MUST NOT start before baseline capture.
- T003 ∥ T004 after T002; both block T007.
- T006 is **after T005** (not parallel with T005). T007 implements against T003–T006.
- T010 and T025 complete before T011–T013.
- T018 ∥ T019 after T016 and confirmation of the ADR 0047 draft in T001. T017 verifies their combined delivery diff with ADR 0047 before T022.
- T014 blocks all Wanted 11 deletions (T015). Phase B (T001) checks the planned cleanup guard; Phase A may challenge the conditional plan.
- Phase 7 order: T020 → T021 → T022 → T024 → T023. T024 after T009 and T012 (and on the gated candidate build); never edits `/web`. T023 closure requires T021 + T022 + T024 + Q7.

## Implementation strategy

1. Capture T002 baseline before any T003/T004/T005 or behaviour-changing work.
2. MVP = Phase 3 For you ranking + reasons on one-load foundation (Phases 2–3).
3. Immediately follow with Related profile removal (Phase 4) so cleanup is safe.
4. Caller-gated cleanup → delivery docs (Proposed ADR 0047 lands with T018/T019) → evidence/smoke/gates → review closure.
5. Do not start T003+ before T002; do not start T007+ before T001 prerequisite confirmation; do not close T023 before T021/T022/T024 and Q7.
