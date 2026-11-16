---

description: "Task list for DEV-510 shrink the cached taste profile"
---

# Tasks: Shrink the cached taste profile

**Input**: Design documents from `specs/DEV-510/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q7), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `EVIDENCE.md`; Maestri notes `DEV-510`, `recon-DEV-510` (lines 27–37, 51–67); `specs/PRODUCT.md`.

**Status**: PLAN FROZEN — Keel, 2026-11-15, after Patron PA1 and independent cross-artifact reread; see PLAN_ADJUDICATION.md. Gate 1 / T000 complete (spec PR #415, 22261b35). Inventory is 17 tasks (was 16) after inserting T001b; 22/22 requirements/criteria coverage (15 FR + 7 SC) is preserved. T001b serializer seam complete at f6647681; T002 baseline measured at d1809a46; feasibility PASSED under amended A1 (Patron 4abc7c53; brief PA1/Q4). Oversize-cache / Finding 1–2 derivative repair applied per Patron 0004b000 / brief.md final PA1 amendment; T003+ held until Keel independent reread of those repaired artifacts. Historical 725/strict-80% FAILED evidence is preserved as superseded provenance. Implementation verification, T007/T009, T010, delivery merge, home promotion and runtime acceptance remain open; runtime-pending. Current branch `feature/DEV-510`; `feature/510-spec` is historical provenance only.

**Tests**: Required (`brief.md` Test strategy; FR-004–FR-012). TDD is optional. Write parity tests pre-change and keep them green across the representation edit; edge-contract tests may start red. Do not manufacture failing parity tests. Persistence/query-translation and serialization-sensitive behaviour use Postgres Testcontainers — never driver mocks. Exact parity uses equality, not tolerance. No new test framework/dependency. Exact suite file paths for suites named only in recon 35–37 are collected at pickup recon — do not invent paths.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/baseline tasks are never `[P]`.

**Conventions**: Edit only the authorized targeted set in `plan.md` / brief Q3/Q7; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` if a required path/fact is missing; no `/web` or public API edits; no explanatory comments beyond AGENTS exemptions; never invent measured bytes or latency; never raise the payload ceiling; never lower harness thresholds. Otherwise-unassigned implementation/measurement tasks are owned by the generic Build lane (do not invent named seats).

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel T000 records the verified merge receipt. No Build work (T001+) before Gate 1.

**Evidence**: Fill slots in `specs/DEV-510/EVIDENCE.md`. Disposable machine artifacts stay untracked/scrubbed — no secrets, vectors, identities, or connection strings.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (Wisp pickup recon + path inventory + Keel reanalysis + Gauge baseline; read-only) — blocks T001b
Phase 1b  T001b         (serializer-only setup + compile accessibility + runtime singleton/admission proof over unchanged incumbent representation) — blocks T002
Phase 2   T002          (US1 pre-change baseline into EVIDENCE.md via live T001b seam) — blocks representation change
Phase 3   T003 → T006   (US2 input contract + compact profile + parity)
Phase 4   T007 → T009   (US3 budget / warning / cache regressions; reuses T001b seam)
Phase 5   T010 → T011   (US4 post-change evidence + mandatory gates)
Phase 6   T012          (capped three-axis review / remediation per Q2)
Phase 7   T013          (delivery / merge-bar handoff — user merges)
Phase 8   T014          (US5 post-merge promotion + ≥24h runtime receipt)
```

Ordering per brief Task ordering / Q7 and PA1 Serializer setup before baseline: **T001 → T001b → T002 → T003+**. Shared serializer installs in T001b before baseline; baseline (T002) before representation (T003+). No dependency cycle. Failed/unavailable serializer proof, baseline, gates, or runtime remain open and cannot yield Done when.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [x] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-510 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-510 in `specs/DEV-510/CONCLUSIONS.md`. Receipt only. T001–T014 MUST NOT proceed without verified merge (`brief.md` Status; Q7; no structural owner checkbox on this ticket). Completed receipt: CONCLUSIONS.md Phase B pickup synchronization, spec PR #415 at 22261b356b8aec7f06b00380d42f4dbf2b755d28.

---

## Phase 1: Pickup recon (read-only; blocks T001b)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; resolve exact paths for declarations/suites not path-qualified in `recon-DEV-510`; inventory HybridCache suite/paths, ceiling-configuration and home-access facts needed by later T001b/T002; Gauge baseline. No production edits.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift/decision stops — no invention. T001 read-only evidence is complete; T001b and T002 receipts exist at f6647681 / d1809a46.

- [x] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying: authorized production files `src/LamuFlix.Core/Domain/TasteProfile.cs`, `src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs`, `src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs`, `src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs`, `HybridCachedRecommendationProfileSource.cs`, `PersistenceServiceCollectionExtensions.cs`; exact paths for existing `ProfileSimilarityCalculator` / `BestLikedSource` / `PlotChannel` consumers, `GetRecommendationsQueryHandler`, `GetForYouRecommendationsQueryHandler`, and revalidate the now-confirmed `src/LamuFlix.Infrastructure/Persistence/TasteProfileCacheOptions.cs`; exact paths for suites named in `recon-DEV-510` lines 35–37 and existing HybridCache suites needed by T001b; facts relevant to later shared `IHybridCacheSerializer<CachedProfile>` registration/accessibility for the exact `CachedProfile(Profile, Generation, CreatedAt)` envelope (inventory only — do not install); how HybridCache effective ceiling is configured today; home measurement access facts without inventing bytes. **Keel** reanalyzes brief/spec/plan/tasks against landed main and amends only for authorized demonstrated drift. **Gauge** records pre-edit gate baseline. **STOP** `needs recon` on missing seams required for T001b/T002+; **STOP** for cited Patron ruling rather than expanding file/protected surface (`brief.md` Q3/Q7; FR-011). **Strictly read-only**: no production or test edits in this task. Do not invent an exact `AddSerializer` overload or DI registration signature — those remain T001b verification requirements, not established API facts. Read effective ceiling from IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes; do not hardcode the reported default. Record original unmodified pickup SHA for later EVIDENCE.md provenance. **Complete as read-only evidence** (task note `DEV-510` / CONCLUSIONS.md Phase B pickup); T001b and T002 proceeded on that evidence.

---

## Phase 1b: Serializer-only setup before baseline (blocks T002)

**Purpose**: Install and prove only the shared `IHybridCacheSerializer<CachedProfile>` seam over the unchanged incumbent TasteProfile/LikedSources representation so T002 can measure through a live registered singleton. Split out of T008 per brief.md PA1 amendment Serializer setup before baseline / CONCLUSIONS.md sequencing ruling at 85feb708.
**Checkpoint**: Passing compiler/type-check accessibility evidence AND passing runtime same-singleton exact-envelope measurement plus real `GetOrCreateAsync` admission proof. Compilation alone cannot close T001b. No assumed serializer invocation on the no-`IDistributedCache` path. Unsupported/unavailable proof blocks for recon/Patron; T002 stays held.

- [x] T001b **Build lane**. After T001: Install and verify only the shared serializer seam confined to already-authorized `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs`, `src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs`, `tests/LamuFlix.UnitTests/Persistence/TasteProfileCacheRegistrationTests.cs`, and existing HybridCache suites identified by T001 recon. Register/inject one shared `IHybridCacheSerializer<CachedProfile>` singleton (thin internal serializer implementation and internal `CachedProfile` envelope accessibility permitted if needed) over the **unchanged incumbent** pre-change `TasteProfile` / `LikedSources` shape and actual `CachedProfile(Profile, Generation, CreatedAt)` envelope. **Forbid** edits in T001b to `TasteProfile.cs`, `TasteProfileCalculator.cs`, `IRecommendationProfileSource.cs`, `EfRecommendationProfileSource.cs`, scoring math, profile inputs, or cache-key namespace. No new package, project, or architectural layer. Do not invent an exact `AddSerializer` overload or DI registration signature — verify the pinned-package registration/accessibility inside `F:/Dev/LamuFlix.worktrees/DEV-510`. **T001b requires both**: (1) passing compiler/type-check evidence for supported pinned-package registration and shared-envelope accessibility; and (2) a passing runtime test proving real `GetOrCreateAsync` admission and exact-envelope byte measurement invoke that same registered singleton serializer/options. Compilation alone is insufficient. Never substitute independently assumed JSON defaults, reflection, a replacement admission path, or assumed serializer invocation without distributed cache. Record serializer-seam SHA plus serializer/configuration provenance for EVIDENCE.md. Unsupported registration/accessibility or failed runtime agreement blocks T002 for recon/Patron. **DONE at f6647681**: compile exit 0; 10/10 TasteProfileCacheRegistrationTests + 4/4 HybridCachedRecommendationProfileSourceCacheTests PASS; shared singleton seam live.

---

## Phase 2: User Story 1 - Pre-change baseline evidence (Priority: P1)

**Goal**: Capture measured home payload/component bytes, no-distributed-cache limit behaviour, matched for-you latency, and fixed scoring snapshot before any representation change, through the live T001b seam.
**Independent Test**: EVIDENCE.md baseline and fixed pre-change parity snapshot plus amended-A1 two-tier feasibility proof exist before edits. Pending/Could not run is honest status but cannot unlock T003+.
**Order**: T002 after successful T001b. **Build lane** owns otherwise-unassigned measurement work.

- [x] T002 [US1] **Build lane**. After successful T001b: Before any representation edit, fill `specs/DEV-510/EVIDENCE.md` baseline slots using the live T001b registered singleton seam over the **unchanged** pre-representation profile: record **both** original unmodified pickup SHA **and** T001b serializer-seam SHA plus serializer/configuration provenance; serialized `CachedProfile` total and per-component split from home Postgres (including `Generation` and `CreatedAt`) via that same singleton; separate no-`IDistributedCache` limit-behaviour reproduction and conclusion (do not assume serializer invocation proves that path); matched cold/uncached vs warm-cache `for-you` timings with cache-state classification, sample count, elapsed observations, summary statistics, and SHA; fixed input/database snapshot and candidate set with pre-change title/order, exact score, and `Because you liked X` reason. Preserve actual pre-representation bytes/components — no thinning. Record safe evidence references only — no secrets/vectors/identities. If access or reproduction is unavailable, mark pending/Could not run explicitly — do **not** silently defer baseline until after the change or invent measurements (FR-011; SC-001; `brief.md` Task ordering step 1; Q6; PA1 Serializer setup before baseline). Hard feasibility gate (amended A1 / brief.md PA1/Q4 amendment at f17205ff / CONCLUSIONS.md at 4abc7c53): record plot-vs-retained-maps-vs-envelope byte split and prove (a) 145-source ticket fixture and live 4-source home each strictly below 0.8× effective ceiling; (b) 300-source fixture strictly below ceiling inside `[0.8×ceiling, ceiling)`. Missing fixed parity snapshot or missing amended-A1 feasibility evidence blocks T003+; no silent assumption relaxation. Unavailable home evidence remains pending/Could not run and never unlocks T003+. **Baseline DONE at f6647681 + evidence commit d1809a46**: all baseline slots filled from real home data through the live seam (total 41,908 B; component split recorded; parity snapshot captured with digest `9b66ab6e08f9a5091fe48712ccebd9c1b7faeffbfc95c11795bbcb1235e259a2`; no-`IDistributedCache` limit behaviour characterized as silent over-ceiling tolerance with an `Error` log and no throw). **Historical 725-source / strict-80% gate FAILED** (superseded as active acceptance, never relabeled passing): measured 449-source compact profile is 1,377,125 B (131.3% of ceiling) and every 725-source projection from the measured per-source distribution is 155–220% of ceiling, because the retained `Liked.Cast` aggregate map dominates (800,238 B) and per-source cast maps add ~1,282 B/source — see `EVIDENCE.md` section 1c. **Amended-A1 feasibility PASSED on existing evidence** (Patron 4abc7c53; measurement d1809a46): 145 sources = 465,340 B (44.4%); 300 sources = 940,943 B (89.7%, in warning band); live home 4 sources = 41,908 B (4.0%). No thinning applied or proposed. T003+ unlocks only upon Keel independent reread of synchronized artifacts.

**Checkpoint**: T003+ unlocks when the fixed pre-change parity snapshot is captured and amended-A1 two-tier feasibility is proven (145 + live home < 0.8× ceiling; 300 in `[0.8×ceiling, ceiling)` below ceiling). Unavailable proof stops for recon/Patron disposition; recording pending does not unlock implementation. Historical 725/strict-80% FAILED projections remain provenance. Only proposed ticket contradiction or constitution departure goes to owner.

**Checkpoint status at T002 (measured, 2026-11-16, SHA f6647681 / evidence d1809a46; Patron disposition 4abc7c53)**: fixed pre-change parity snapshot — **CAPTURED** (EVIDENCE.md section 3, digest `9b66ab6e08f9a5091fe48712ccebd9c1b7faeffbfc95c11795bbcb1235e259a2`). Historical fivefold 725 / strict-80% — **FAILED** (superseded; 449 sources = 1,377,125 B / 131.3%; 725 projections 1,628,412–2,303,387 B / 155–220%). Amended A1 two-tier feasibility — **PASSED**: 145 = 465,340 B (44.4% < 80%); 300 = 940,943 B (89.7% in `[80%, 100%)`); live home 4 sources = 41,908 B (4.0% < 80%). No thinning was applied or proposed. **T002 feasibility checkpoint: PASSED under amended A1** after derivative sync; T003+ held only for Keel independent reread.

---

## Phase 3: User Story 2 - Compact profile with embedding transfer and exact parity (Priority: P1) 🎯 MVP

**Goal**: Extend uncached inputs with one batched liked-source query; remove only per-source plot vectors from cached profile; wire consumers; prove exact parity on both recommendation paths.
**Independent Test**: Retrieval/query-count and parity tests green; scoring edge cases preserved.
**Order**: T003 → T006. **Build lane** owns otherwise-unassigned work. Exact undeclared paths come from T001 recon.

### Tests for User Story 2

- [ ] T003 [P] [US2] **Build lane**. After T002: Add failing Postgres Testcontainers coverage in the existing `EfRecommendationProfileSourceTests` suite (exact path from T001) proving liked-source embeddings return keyed by existing movie identity and that the additional query count is one batched query independent of source/candidate count; keep inputs uncached (FR-002; SC-004; `brief.md` Q5). Also test exact active-profile key set, existing identity equality, missing/partial and empty result maps with nonempty sources, zero additional embedding query for empty sources, cancellation propagation, and no independent liked-membership reload between awaits.
- [ ] T004 [P] [US2] **Build lane**. After T002: Add pre-change exact-parity tests that stay green and scoring-edge coverage that may start red using existing calculator/property/consumer suites from T001 (including both recommendation consumers) for fixed-snapshot title/order/score/reason equality plus multiple liked sources, missing/zero vectors, ties, empty/disliked-only, and retained non-plot channels — no tolerance (FR-004–FR-005; SC-003; `brief.md` Q5). Both consumers must pass extended inputs; prove compile-time absence of cached per-source plot vectors and preserve incumbent missing-vector/tie/non-plot behavior. Fixed-snapshot parity makes no new cross-read transactional consistency promise.

### Implementation for User Story 2

- [ ] T005a [US2] **Build lane**. After T003 and T004 are authored: extend existing GetProfileInputsAsync internal contract in src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs and implement EF retrieval in src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs. Accept exactly the active request profile LikedSources identity set, existing equality; missing/partial rows preserve missing-vector semantics; empty set yields empty map and no additional embedding query; otherwise one additional batch, propagate CancellationToken. Keep a compatible compiling consumer path until T005b; retrieval and pre-change parity suites green at this boundary. Do not weaken tests or commit an uncompilable intermediate signature change (FR-002/FR-004/FR-005; brief PA1).

- [ ] T005b [US2] **Build lane**. After T005a and T004: Implement the compact representation and embedding transfer only in the authorized files: `src/LamuFlix.Core/Domain/TasteProfile.cs`, `src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs`, `src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs` (`GetProfileInputsAsync` return type), `src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs`, existing `ProfileSimilarityCalculator` / `BestLikedSource` / `PlotChannel` consumers (T001 paths), existing `GetRecommendationsQueryHandler` and `GetForYouRecommendationsQueryHandler` (T001 paths), and `src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs` as required for the compact cache value. Remove only per-liked-source plot embeddings from cached `LikedSources`; retain identities, cast/genre/director maps, and aggregates; one additional batched query; preserve scoring math and zero/missing/tie behaviour; no public API/`/web`/schema/dependency/LocalPlay/secrets/Process.Start changes (FR-001–FR-005, FR-014; `brief.md` Q3). Remove cached source plot-vector member so neither consumer can fall back to cached vectors. Version compact cache key namespace while preserving generation/expiry; compile and run both-consumer parity at this boundary.
- [ ] T006 [US2] **Build lane**. After T005b: Make T003–T004 green against T005a/T005b without weakening assertions; rerun touched existing suites from T001 to confirm preserved cache freshness/invalidation/cancellation/factory-failure and uncached-input regressions still in scope for this story’s seams (FR-004–FR-005; SC-003–SC-004). Extend cancellation coverage specifically to the new EF batch; incomplete/empty-map cases run across both consumers.

**Checkpoint**: US2 independently testable — compact profile + batched inputs + exact parity.

---

## Phase 4: User Story 3 - Payload budget, warning, and cache regressions (Priority: P2)

**Goal**: Amended-A1 two-tier fixtures (145 below 0.8; 300 in warning band below ceiling), warning/ceiling boundary coverage as applicable, options validation/`ValidateOnStart` for the warning setting, and observability contract; prove admission/reuse including 300-source without `MaximumPayloadBytesExceeded`; retain cache regressions.
**Independent Test**: Size/warning/ceiling-boundary/admission and invalid-configuration tests green; home-budget assertion recorded when measured.
**Order**: T007 → T009.

### Tests for User Story 3

- [ ] T007 [US3] **Build lane**. After T006: Add failing coverage for the amended two-tier A1 budget against IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes (ticket reports 1,048,576-byte default) on realistic retained maps with 384-float vectors in uncached inputs only and no thinning (`ASSUMPTIONS.md` A1–A3; brief.md PA1/Q4 amendment; FR-006–FR-010; SC-002; SC-005): (a) 145-liked-source ticket-baseline fixture and live 4-source home profile each serialize strictly below 0.8× the effective ceiling; (b) 300-liked-source fixture serializes strictly below the ceiling inside `[0.8×ceiling, ceiling)`, exercises the near-limit warning, and proves real HybridCache admission/reuse without `MaximumPayloadBytesExceeded`. Also cover warning-fraction boundaries below/equal/above default 0.8 (`WarningFraction` const); payload-ceiling boundaries below/equal/above as applicable using the confirmed no-`IDistributedCache` limit behaviour from T002 (preserve observed failure semantics; if a boundary is inapplicable under that evidence, record evidenced inapplicability rather than assuming rejection); invalid-configuration regression coverage for the new warning setting under preserved or extended typed options validation and `ValidateOnStart` (constitution VII lines 648–652, 672); and actual cache admission/reuse without per-hit re-serialization — extend existing HybridCache / registration / options suites from T001 without inventing undeclared wiring facts. Extend tests/LamuFlix.UnitTests/Options/TasteProfileCacheOptionsTests.cs and tests/LamuFlix.UnitTests/Persistence/TasteProfileCacheRegistrationTests.cs for finite 0 < WarningFraction <= 1: 0, 1, negative, >1, NaN, +/-infinity. Use effective runtime options for all budgets. **Reuse the T001b shared singleton seam** (do not reinstall the serializer): prove compact-profile HybridCache admission and construction-only exact-envelope byte measurement still invoke that same registered singleton `IHybridCacheSerializer<CachedProfile>` / options on the actual `CachedProfile(Profile, Generation, CreatedAt)` envelope after representation shrink; assert runtime observation/admission agreement below/equal/above the real ceiling; prove real `GetOrCreateAsync` admission/reuse (including at 300-source scale); and keep the separate no-`IDistributedCache` limit-behaviour proof from T002. Compilation/type-check of registration alone is not runtime proof. Assert numeric-only logs. Per Patron `0004b000` / brief.md PA1 amendment Oversize cache behavior (superseding the historical blanket no-warm-hit assumption): over-ceiling construction with local cache enabled and no `IDistributedCache` warns before admission (`WarningFraction`), logs HybridCache `MaximumPayloadBytesExceeded` at Error, returns successfully to callers including coalesced waiters without size-only throw, and retains native L1 reuse (`entryWasReused = true`, `SerializeCount = 1`); actual non-admission (`entryWasReused = false`, factory re-invocation on repeat read) is proven only for the measured `DisableLocalCache` + `IDistributedCache` L2 case from T002. Keep these proofs pending until green; do not add a cache-policy wrapper or change dependencies. Historical 725/strict-80% FAILED projections are not the active T007 acceptance fixture.

### Implementation for User Story 3

- [ ] T008 [US3] **Build lane**. After T007: Implement structured serialized-payload-byte and effective-budget observations on cache construction in `src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs`, and wire configurable warning fraction (const default 0.8) through existing `src/LamuFlix.Infrastructure/Persistence/TasteProfileCacheOptions.cs` and `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` if needed — **preserve or extend** that type's existing typed options validation and `ValidateOnStart` for the new setting (constitution VII lines 648–652, 672; extend the existing .Validate chain and ValidateOnStart confirmed by targeted recon lines 3-4; no new validator type); single source of truth, no scattered literals, no vector/identity/secret logs, retain default ceiling, preserve freshness semantics (FR-008–FR-010; `brief.md` Q4/Q6). Extend existing validation chain, retain startup validation, match SectionName:nameof(property) error-message style. Logging only through minimal src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.Log.cs source-generated LoggerMessage partial with numeric PayloadBytes/BudgetBytes/Fraction and no interpolation. **Do not reinstall the serializer** — reuse the T001b shared `IHybridCacheSerializer<CachedProfile>` singleton already registered over `CachedProfile`; after T003–T006 shrink TasteProfile that same singleton remains the admission/measurement seam. Construction-only byte observation and HybridCache admission MUST continue to use that same singleton serializer/options on the exact `CachedProfile(Profile, Generation, CreatedAt)` envelope; warn before real `GetOrCreateAsync` admission/reuse; never throw for size alone; never reflection or independently assumed JSON defaults. Per Patron `0004b000` / brief.md PA1 amendment Oversize cache behavior: local-enabled / no-`IDistributedCache` oversize warns, logs HybridCache `MaximumPayloadBytesExceeded` at Error, returns successfully including coalesced waiters, and retains native L1 reuse; actual non-admission is only the measured `DisableLocalCache` + `IDistributedCache` case — no adapter-level size rejection, L1 eviction, or bypass. T007 runtime proofs (compact-profile byte/admission agreement, two-configuration reuse/non-admission, separate no-`IDistributedCache` behaviour) remain required and pending — compilation alone is not runtime proof. No static readonly warning default. No new package/project/layer.
- [ ] T009 [US3] **Build lane**. After T008: Make T007 green; confirm existing HybridCache cold/freshness/cache suites from T001 still cover reuse, expiry, invalidation/generation, concurrency/coalescing, cancellation, factory failure, scope-lifetime, and uncached inputs (FR-009; SC-005). Seed a legacy-shaped entry under the old key namespace; prove it is ignored/rebuilt under the compact version and cannot supply authoritative source vectors. Preserve coalescing; for oversize, prove successful coalesced return plus native L1 reuse under local-enabled / no-`IDistributedCache`, and prove actual non-admission only under the measured `DisableLocalCache` + `IDistributedCache` case (Patron `0004b000`).

**Checkpoint**: US3 independently testable for budget/observability/cache behaviour.

---

## Phase 5: User Story 4 - Post-change evidence and required gates (Priority: P2)

**Goal**: Refresh post-change measurements/parity in `EVIDENCE.md` and run mandatory gates at exact SHA.
**Independent Test**: Evidence sections updated; gate table filled with distinct verdicts — or explicit open/Could not run.
**Order**: T010 → T011. Gates sequential after measurement windows.

- [ ] T010 [US4] **Build lane**. After T009: Fill `specs/DEV-510/EVIDENCE.md` distinct post-change slots: section 1b post-change payload (post-change SHA; serializer/configuration/effective ceiling; profile/library count; serialized total/component split; calculated headroom) and section 4 post-change matched latency (matched timing samples and summary statistics separate from pre-change); confirm current measured 4-liked-source home serialized profile strictly below 0.8× ceiling budget (amended A1/A3); complete fixed-snapshot parity comparison; record 145- and 300-source fixture bytes, admission/reuse, and bounded-query evidence. Publish measurements only — no invented speedup. Keep all unmeasured values pending/Could not run (FR-007; FR-012; SC-001–SC-003; `ASSUMPTIONS.md` A3; `brief.md` Q6). Classify warm samples as profile-cache-hit plus uncached embedding batch (zero batch only for empty sources); record extra query count/cost and vector volume for matched pre/post interpretation.
- [ ] T011 [US4] **Gauge** / Build-lane gate execution. After T010: Run ticket-required and applicable pipeline gates against the implementation HEAD: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (including applicable refactor-gate obligation — no lowered threshold), `./scripts/run-jetbrains-inspectcode.ps1`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, pre-PR `./scripts/run-mutation.ps1` when production C# under `src/` is eligible, and `./scripts/run-web-gates.ps1` (expect SKIPPED (scope-empty) if `/web` untouched). Record exact commands, HEAD, exits, and distinct verdicts in `EVIDENCE.md` section 7. Never fold non-pass into PASS; surviving mutants require tests (FR-015; SC-006; `brief.md` Gate expectations). From worktrees invoke pwsh -NoProfile -File ./scripts/run-mutation.ps1 without --since. Preserve maxKillShare/maxKillFileSpan and additional-timeout safeguards; no threshold changes.

**Checkpoint**: US4 evidenced or explicitly open; gates cannot be claimed green from empty/old-head runs.

---

## Phase 6: Review / remediation (Q2 closing bar)

**Purpose**: Capped three-axis implementation review under the frozen closing bar.
**Checkpoint**: Zero above-bar findings or unresolved items explicitly left for user review on the PR at the 2×2 cap — never auto-cleared. After any remediation commit, refresh gates and invalidated evidence on final HEAD.

- [ ] T012 After T010–T011: Conduct capped implementation review/remediation (at most two rounds and two fix commits per round; one consolidated numbered fix list per round) requiring Sentry/Risk, Ledger/Standards, and Compass/Spec reports pinned to the reviewed head (size M). Closing bar: every verified Critical/High/Medium/Low finding within frozen scope resolved or explicitly dispositioned with cited evidence; no accepted unresolved correctness or acceptance gap closes review. Unresolved above-bar findings after the cap go to user PR review — never silent clearance. **After any remediation commit**: rerun affected gates on final HEAD; refresh invalidated `EVIDENCE.md` measurements; never label an old-head run as final-head PASS (`brief.md` Q2; FR-015; SC-006).

---

## Phase 7: Delivery handoff

**Purpose**: Prepare merge-bar evidence packaging; only the user merges.
**Checkpoint**: No claim of merge clearance by agents. Final-head freshness required.

- [ ] T013 After T012: Publish exact-head merge-bar evidence package for DEV-510 (gates, parity, size/budget, `EVIDENCE.md` status) using only receipts that match the **final delivery/review HEAD**. Leave merge to the user. Do not merge or enable auto-merge. Do not claim PLAN FROZEN, Gate 1, home promotion, or runtime PASS from packaging alone. Only Keel declares PLAN FROZEN after independent reread (SC-006–SC-007; `brief.md` Q2/Q6/Q7).

---

## Phase 8: User Story 5 - Post-merge home runtime acceptance (Priority: P3)

**Goal**: After user-owned delivery merge, authorized operator promotes home and records ≥24-hour zero-exceedance + warm-cache latency.
**Independent Test**: `EVIDENCE.md` section 8 complete, or status remains `runtime-pending`.
**Order**: T014 after user merge of the delivery PR (not the spec PR).

- [ ] T014 [US5] After user-owned delivery merge: authorized operator promotes home and fills `specs/DEV-510/EVIDENCE.md` section 8 — promotion receipt, deployed SHA, observation start/end/time zone spanning ≥24 hours, workload/profile count, `MaximumPayloadBytesExceeded` count (**must be 0**), and measured warm-cache latency. Status stays **runtime-pending** until that receipt exists. No seat merges or implies deployment authorization (FR-013; SC-007; `brief.md` Task ordering step 6; Q6).

**Checkpoint**: Runtime acceptance evidenced or explicitly still `runtime-pending`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Blocks all Build work
- **Phase 1 (T001)**: Depends on T000 — read-only; blocks T001b
- **Phase 1b (T001b)**: Depends on T001 — serializer-only setup + compile and runtime proofs; blocks T002
- **Phase 2 (T002 / US1)**: Depends on successful T001b — blocks representation change
- **Phase 3 (US2)**: Depends on T002; T003/T004 may run in parallel after T002; T005a after both authored; T005b after T005a; T006 after T005b
- **Phase 4 (US3)**: Depends on T006; T007/T008 reuse T001b seam (no serializer reinstall)
- **Phase 5 (US4)**: Depends on T009; T011 after T010
- **Phase 6–7**: Depend on T011 status
- **Phase 8 (US5)**: Depends on user delivery merge after T013 packaging

### User Story Dependencies

- **US1 (P1)**: After T001b seam is live; measures unchanged incumbent representation (no representation change in T001b); amended-A1 feasibility PASSED on existing evidence after sync + Keel reread
- **US2 (P1) MVP**: After US1 parity baseline and amended-A1 two-tier feasibility checkpoint pass (not the superseded 725/strict-80% gate)
- **US3 (P2)**: After US2 compact representation; reuses T001b serializer seam; covers 145/live-home < 0.8 and 300 warning-band admission/reuse
- **US4 (P2)**: After US3
- **US5 (P3)**: After user delivery merge

### Parallel Opportunities

- T003 and T004 after T002 (different suites)
- No parallelization of same-file edits, shared evidence fills, or gate runs
- T001b is never parallel with T001 or T002

---

## Implementation Strategy

### MVP First (User Stories 1–2)

1. Gate 1 merge + read-only pickup recon (T001) — complete
2. Serializer-only setup and runtime agreement over unchanged representation (T001b) — complete at f6647681
3. Baseline evidence through live T001b seam (US1 / T002) — measured at d1809a46; feasibility PASSED under amended A1 after sync + Keel reread
4. Compact profile + embedding transfer + exact parity (US2) — next after reread
5. **STOP and VALIDATE** parity/query-count before budget instrumentation

### Incremental Delivery

1. T001 → T001b → US1 baseline (amended A1) → US2 MVP → US3 budget/observability (145 + 300 fixtures; reuse T001b seam) → US4 gates/evidence → user merge → US5 runtime receipt
2. Each story leaves prior evidence intact; unavailable runtime stays explicitly open; historical 725 FAILED projections remain provenance
3. Task inventory: 17 tasks; requirements/criteria coverage remains 22/22 (15 FR + 7 SC)

---

## Notes

- `[P]` tasks = different files, no incomplete dependencies
- `[USn]` maps to spec.md user stories
- Local Phase N ≠ task-pipeline Phase N
- Parity tests stay green; edge-contract tests may start red; TDD optional
- Commit after each logical group in Build lane
- Avoid: inventing paths/bytes/latency; raising ceiling; truncating sources; claiming runtime PASS from gates
