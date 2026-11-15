---

description: "Task list for DEV-510 shrink the cached taste profile"
---

# Tasks: Shrink the cached taste profile

**Input**: Design documents from `specs/DEV-510/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q7), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `EVIDENCE.md`; Maestri notes `DEV-510`, `recon-DEV-510` (lines 27–37, 51–67); `specs/PRODUCT.md`.

**Status**: PLAN FROZEN — Keel, 2026-11-15, after Patron PA1 and independent cross-artifact reread; see PLAN_ADJUDICATION.md. Gate 1, implementation verification, home measurement/promotion and runtime acceptance remain open; runtime-pending.

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
Phase 1   T001          (Wisp pickup recon + path inventory + Keel reanalysis + Gauge baseline) — blocks T002+
Phase 2   T002          (US1 pre-change baseline into EVIDENCE.md) — blocks representation change
Phase 3   T003 → T006   (US2 input contract + compact profile + parity)
Phase 4   T007 → T009   (US3 budget / warning / cache regressions)
Phase 5   T010 → T011   (US4 post-change evidence + mandatory gates)
Phase 6   T012          (capped three-axis review / remediation per Q2)
Phase 7   T013          (delivery / merge-bar handoff — user merges)
Phase 8   T014          (US5 post-merge promotion + ≥24h runtime receipt)
```

Ordering per brief Task ordering / Q7: **baseline (T002) before representation (T003+)**. No dependency cycle. Failed/unavailable baseline, gates, or runtime remain open and cannot yield Done when.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-510 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-510 in `specs/DEV-510/CONCLUSIONS.md`. Receipt only. T001–T014 MUST NOT proceed without verified merge (`brief.md` Status; Q7; no structural owner checkbox on this ticket).

---

## Phase 1: Pickup recon (blocks baseline and implementation)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; resolve exact paths for declarations/suites not path-qualified in `recon-DEV-510`; confirm HybridCache serializer/options identity; Gauge baseline.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift/decision stops — no invention.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying: authorized production files `src/LamuFlix.Core/Domain/TasteProfile.cs`, `src/LamuFlix.Core/Features/Recommendations/TasteProfileCalculator.cs`, `src/LamuFlix.Core/Ports/IRecommendationProfileSource.cs`, `src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs`, `HybridCachedRecommendationProfileSource.cs`, `PersistenceServiceCollectionExtensions.cs`; exact paths for existing `ProfileSimilarityCalculator` / `BestLikedSource` / `PlotChannel` consumers, `GetRecommendationsQueryHandler`, `GetForYouRecommendationsQueryHandler`, and revalidate the now-confirmed `src/LamuFlix.Infrastructure/Persistence/TasteProfileCacheOptions.cs`; exact paths for suites named in `recon-DEV-510` lines 35–37; production-equivalent serializer/options used for `CachedProfile`; how HybridCache effective ceiling is configured today; home measurement access facts without inventing bytes. **Keel** reanalyzes brief/spec/plan/tasks against landed main and amends only for authorized demonstrated drift. **Gauge** records pre-edit gate baseline. **STOP** `needs recon` on missing seams required for T002+; **STOP** for cited Patron ruling rather than expanding file/protected surface (`brief.md` Q3/Q7; FR-011). No production edits in this task. Targeted recon confirms options and test paths; verify pinned package serializer API and resolution for the same registered HybridCache serializer/options on CachedProfile (no wrapper or independent JSON defaults). Unsupported seam blocks implementation for recon/Patron. Read effective ceiling from IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes; do not hardcode the reported default.

---

## Phase 2: User Story 1 - Pre-change baseline evidence (Priority: P1)

**Goal**: Capture measured home payload/component bytes, no-distributed-cache limit behaviour, matched for-you latency, and fixed scoring snapshot before any representation change.
**Independent Test**: EVIDENCE.md baseline and fixed pre-change parity snapshot plus retained-map/envelope fivefold feasibility proof exist before edits. Pending/Could not run is honest status but cannot unlock T003+.
**Order**: T002. **Build lane** owns otherwise-unassigned measurement work.

- [ ] T002 [US1] **Build lane**. After T001: Before any representation edit, fill `specs/DEV-510/EVIDENCE.md` baseline slots: serialized `CachedProfile` total and per-component split from home Postgres using production-equivalent serializer/options (including `Generation` and `CreatedAt`); separate no-`IDistributedCache` limit-behaviour reproduction and conclusion; matched cold/uncached vs warm-cache `for-you` timings with cache-state classification, sample count, elapsed observations, summary statistics, and SHA; fixed input/database snapshot and candidate set with pre-change title/order, exact score, and `Because you liked X` reason. Record safe evidence references only — no secrets/vectors/identities. If access or reproduction is unavailable, mark pending/Could not run explicitly — do **not** silently defer baseline until after the change or invent measurements (FR-011; SC-001; `brief.md` Task ordering step 1; Q6). Hard feasibility gate: record plot-vs-retained-maps-vs-envelope byte split and realistic 725-source retained-content projection against 80% effective ceiling. Missing fixed parity snapshot, missing feasibility evidence or an infeasible projection blocks T003+ for recon/Patron ruling; no silent assumption relaxation.

**Checkpoint**: T003+ BLOCKED until the fixed pre-change parity snapshot is captured and measured retained-maps/envelope plus representative fivefold projection are proven strictly below the frozen 80% effective-ceiling budget. Unavailable proof or infeasibility is recorded pending/Could not run and stops for recon/Patron disposition; recording pending does not unlock implementation. Keep 725/0.8 frozen; only proposed ticket contradiction or constitution departure goes to owner.

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

**Goal**: Fivefold fixture, warning/ceiling boundary coverage as applicable, options validation/`ValidateOnStart` for the warning setting, and observability contract; prove admission/reuse; retain cache regressions.
**Independent Test**: Size/warning/ceiling-boundary/admission and invalid-configuration tests green; home-budget assertion recorded when measured.
**Order**: T007 → T009.

### Tests for User Story 3

- [ ] T007 [US3] **Build lane**. After T006: Add failing coverage for representative 725-liked-source fixture serialization strictly below 80 percent of IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes (ticket reports 1,048,576-byte default) (realistic retained maps; 384-float vectors in uncached inputs only; no thinning); warning-fraction boundaries below/equal/above default 0.8; payload-ceiling boundaries below/equal/above as applicable using the confirmed no-`IDistributedCache` limit behaviour from T002 (preserve observed failure semantics; if a boundary is inapplicable under that evidence, record evidenced inapplicability rather than assuming rejection); invalid-configuration regression coverage for the new warning setting under preserved or extended typed options validation and `ValidateOnStart` (constitution VII lines 648–652, 672); and actual cache admission/reuse without per-hit re-serialization — extend existing HybridCache / registration / options suites from T001 without inventing undeclared wiring facts (FR-006–FR-010; SC-002; SC-005; `ASSUMPTIONS.md` A1–A2; `brief.md` Q4; `spec.md` scope item 5). Extend tests/LamuFlix.UnitTests/Options/TasteProfileCacheOptionsTests.cs and tests/LamuFlix.UnitTests/Persistence/TasteProfileCacheRegistrationTests.cs for finite 0 < WarningFraction <= 1: 0, 1, negative, >1, NaN, +/-infinity. Use effective runtime options for all budgets, same registered serializer/options on same envelope, and assert observation/admission agreement below/equal/above real ceiling. Assert numeric-only logs; over-ceiling construction warns, returns to coalesced callers without size-only throw, and is not a false warm hit.

### Implementation for User Story 3

- [ ] T008 [US3] **Build lane**. After T007: Implement structured serialized-payload-byte and effective-budget observations on cache construction in `src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs`, and wire configurable warning fraction (const default 0.8) through existing `src/LamuFlix.Infrastructure/Persistence/TasteProfileCacheOptions.cs` and `src/LamuFlix.Infrastructure/Persistence/PersistenceServiceCollectionExtensions.cs` if needed — **preserve or extend** that type's existing typed options validation and `ValidateOnStart` for the new setting (constitution VII lines 648–652, 672; extend the existing .Validate chain and ValidateOnStart confirmed by targeted recon lines 3-4; no new validator type); single source of truth, no scattered literals, no vector/identity/secret logs, retain default ceiling, preserve freshness semantics (FR-008–FR-010; `brief.md` Q4/Q6). Extend existing validation chain, retain startup validation, match SectionName:nameof(property) error-message style. Logging only through minimal src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.Log.cs source-generated LoggerMessage partial with numeric PayloadBytes/BudgetBytes/Fraction and no interpolation. Serialize only on construction with the same registered serializer/options, warn before real GetOrCreateAsync admission, preserve swallowed non-admission and successful return, never throw for size alone. No static readonly warning default.
- [ ] T009 [US3] **Build lane**. After T008: Make T007 green; confirm existing HybridCache cold/freshness/cache suites from T001 still cover reuse, expiry, invalidation/generation, concurrency/coalescing, cancellation, factory failure, scope-lifetime, and uncached inputs (FR-009; SC-005). Seed a legacy-shaped entry under the old key namespace; prove it is ignored/rebuilt under the compact version and cannot supply authoritative source vectors. Preserve coalescing and prove rejected entries cause no false warm hit.

**Checkpoint**: US3 independently testable for budget/observability/cache behaviour.

---

## Phase 5: User Story 4 - Post-change evidence and required gates (Priority: P2)

**Goal**: Refresh post-change measurements/parity in `EVIDENCE.md` and run mandatory gates at exact SHA.
**Independent Test**: Evidence sections updated; gate table filled with distinct verdicts — or explicit open/Could not run.
**Order**: T010 → T011. Gates sequential after measurement windows.

- [ ] T010 [US4] **Build lane**. After T009: Fill `specs/DEV-510/EVIDENCE.md` distinct post-change slots: section 1b post-change payload (post-change SHA; serializer/configuration/effective ceiling; profile/library count; serialized total/component split; calculated headroom) and section 4 post-change matched latency (matched timing samples and summary statistics separate from pre-change); confirm current home serialized profile strictly below 80 percent budget; complete fixed-snapshot parity comparison; record synthetic fixture bytes, admission/reuse, and bounded-query evidence. Publish measurements only — no invented speedup. Keep all unmeasured values pending/Could not run (FR-007; FR-012; SC-001–SC-003; `ASSUMPTIONS.md` A3; `brief.md` Q6). Classify warm samples as profile-cache-hit plus uncached embedding batch (zero batch only for empty sources); record extra query count/cost and vector volume for matched pre/post interpretation.
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
- **Phase 1 (T001)**: Depends on T000 — blocks baseline and implementation
- **Phase 2 (T002 / US1)**: Depends on T001 — blocks representation change
- **Phase 3 (US2)**: Depends on T002; T003/T004 may run in parallel after T002; T005a after both authored; T005b after T005a; T006 after T005b
- **Phase 4 (US3)**: Depends on T006
- **Phase 5 (US4)**: Depends on T009; T011 after T010
- **Phase 6–7**: Depend on T011 status
- **Phase 8 (US5)**: Depends on user delivery merge after T013 packaging

### User Story Dependencies

- **US1 (P1)**: After pickup; no code change required
- **US2 (P1) MVP**: After US1 parity baseline and fivefold feasibility checkpoint pass
- **US3 (P2)**: After US2 compact representation
- **US4 (P2)**: After US3
- **US5 (P3)**: After user delivery merge

### Parallel Opportunities

- T003 and T004 after T002 (different suites)
- No parallelization of same-file edits, shared evidence fills, or gate runs

---

## Implementation Strategy

### MVP First (User Stories 1–2)

1. Gate 1 merge + pickup recon
2. Baseline evidence (US1)
3. Compact profile + embedding transfer + exact parity (US2)
4. **STOP and VALIDATE** parity/query-count before budget instrumentation

### Incremental Delivery

1. US1 baseline → US2 MVP → US3 budget/observability → US4 gates/evidence → user merge → US5 runtime receipt
2. Each story leaves prior evidence intact; unavailable runtime stays explicitly open

---

## Notes

- `[P]` tasks = different files, no incomplete dependencies
- `[USn]` maps to spec.md user stories
- Local Phase N ≠ task-pipeline Phase N
- Parity tests stay green; edge-contract tests may start red; TDD optional
- Commit after each logical group in Build lane
- Avoid: inventing paths/bytes/latency; raising ceiling; truncating sources; claiming runtime PASS from gates
