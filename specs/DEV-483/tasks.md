---

description: "Task list for DEV-483 movie details and recommendation endpoint latency"
---

# Tasks: Movie details and recommendation endpoint latency

**Input**: Design documents from `specs/DEV-483/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q6), Plan Challenge Round 1 adjudication ruling in `brief.md` / `CONCLUSIONS.md`, `ASSUMPTIONS.md`; Maestri notes `DEV-483`, `recon-DEV-483` (lines 11–22, 25–29, 34–49); `PLAN_ADJUDICATION.md` fix list 1–10; `specs/PRODUCT.md`.

**Status**: Challenge-repair round 1 — Quill applying `PLAN_ADJUDICATION.md` fix list 1–10. PLAN FROZEN withheld pending Keel independent reread. Gate 1, implementation, runtime acceptance, and merge remain distinct and open. Quill does not declare freeze or Gate 1.

**Phase B pickup — 2026-11-13**: Gate 1 verified landed via PR #387 at `bac075b02239eee02fe5e39bf40e8a0d21a0be1d`; T000 complete. Keel reanalysis at `f88ab2336b6733684041082ad10c5d87d28e4e23` complete; Gauge baseline recorded in DEV-483 note. T001 pickup completed per Conductor disposition after recon-DEV-483:38-58. Phase 2 may proceed to runtime restoration and diagnosis preparation; T002 Q2 sampling is blocked while Compose port 3000 is unreachable. Test fixtures are not verified Compose fixtures; collect runtime dataset/history/oracles and exact usable diagnostic facilities before dependent measurements. See CONCLUSIONS.md T001 follow-up receipt for limits. This current status supersedes the historical drafting status above without changing requirements.

**Tests**: Required (`brief.md` Q4; FR-006–FR-008). Prefer failing tests before production edits at the diagnosed seam. Latency wall-clock proof is Compose Q2 receipt only — never unit timers. Persistence/query-translation behaviour uses Testcontainers, never driver mocks. No new test framework/dependency/benchmark platform. Deterministic performance-relevant invariants come from actual diagnosis (query/round-trip bounds only if causal). If cached profile source is touched, prove existing cache identity/history boundaries with regression coverage — collect missing semantics through recon; never invent per-user architecture.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/receipt/diagnosis tasks are never `[P]`.

**Conventions**: Edit only the minimal proven causal subset of Q3 candidate production files and justified existing test suites; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` if cause unestablished; preserve unrelated `harness.yml` dirt; no `/web` edits; no explanatory comments beyond AGENTS exemptions; never invent fixture IDs — collect via recon. Otherwise-unassigned implementation/measurement tasks are owned by the generic Build lane (do not invent named seats). Use existing Activity/log facilities only (exact available facilities via recon); `TimeProvider` for injected time; temporary instrumentation must be accounted for and removed before fix acceptance with measured build identity recorded — any retained instrumentation is a production change inside the approved candidate boundary and gated. Outside-boundary proven cause is an expected STOP for cited Patron ruling, never permission to edit.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel T000 records the verified merge receipt. No Build work (T001+) before Gate 1.

**Evidence location**: Disposable untracked receipt directory resolved by `git rev-parse --git-path dev-483-evidence`. Store scrubbed commands/raw samples/provenance/summaries there — no secrets, connection strings, tokens, env dumps, or raw config dumps. Record safe config identity only. Operational evidence, not a new source folder. Publish a scrubbed reviewable receipt through team handoff; do not commit raw machine/config artifacts.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (Wisp pickup recon + fixture/oracles + Keel reanalysis + Gauge baseline) — blocks T002+
Phase 2   T002 → T003   (US1 diagnosis: Q2 baseline, isolation, causal chain — STOP needs recon if unestablished)
Phase 3   T004 → T006   (US2 minimal causal fix + regressions at diagnosed seam)
Phase 4   T007          (US3 Q2 after-measurement + for-you timeout trigger + settled batches)
Phase 5   T009          (Gauge mandatory .NET gates + honest verdicts — after T007; sequential vs sample windows)
Phase 6   T008          (US4 Patron→Rigger YouTrack cause documentation + verified read-back — after T007+T009 status)
Phase 7   T010          (capped three-axis review / remediation per Q5)
Phase 8   T011          (delivery / merge-bar handoff — user merges; final-head freshness after any T010 fix)
```

Ordering per brief Q6 / Round 1 fix 9: **T007 → T009 → T008 → T010 → T011**. No dependency cycle. Failed/unavailable runtime or gates remain open and cannot yield Done when, even if cause documentation is recorded.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [X] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-483 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-483 in `specs/DEV-483/CONCLUSIONS.md`. Receipt only. T001–T011 MUST NOT proceed without verified merge (`brief.md` task ordering; Q6; no structural owner checkbox on this ticket).

---

## Phase 1: Pickup recon (blocks diagnosis)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; refresh inventory of Q3 candidate files and existing test suites; collect missing fixture IDs / history / candidate facts and route-specific oracles without inventing them; note diagnostic control endpoints; preserve unrelated `harness.yml` dirt.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift/decision stops — no invention. Baseline captured.

- [X] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying: Q3 candidate production files `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs`, `RecommendationEndpoints.cs`, `src/LamuFlix.Core/Features/Library/GetMovieDetailsQueryHandler.cs`, `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`, `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs`, `GetForYouRecommendationsQueryHandler.cs`, `src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs`, `EfRecommendationProfileSource.cs`, `HybridCachedRecommendationProfileSource.cs`; existing suites `tests/LamuFlix.UnitTests/Features/Library/GetMovieDetailsQueryHandlerTests.cs`, `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs`, `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs`, `GetForYouRecommendationsQueryHandlerTests.cs`, `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs`, `ApiEndToEndRecommendationTests.cs`; Compose reachability via web port 3000; **route-specific status/payload/fixture oracles** supplied by recon (do not invent): known details identity/content and existing contract; missing id `9999` existing 404 semantics; by-movie eligible results and preserved ranking/filtering; for-you fixture-specific history/candidate expectations, including non-empty where eligible; concrete known details ID, recommendation seed with eligible results, and for-you history/candidate fixture facts; exact available Activity/log diagnostic facilities; applicable configured complexity/refactor-gate and mutation eligibility facts from actual scripts/config (never assumed by project name). Define readiness observation **without prewarming** the measurement target. Plan pin/reset of dataset/history between comparable runs. Note diagnostic-control baselines for genres/movie list (controls only). Preserve pre-existing unrelated `harness.yml` modification without opening/changing it. **Keel** reanalyzes brief/spec/plan/tasks against landed main and amends only for authorized demonstrated drift. **Gauge** records pre-edit gate baseline. **STOP** `needs recon` on missing/drifted seams required for Q2; **STOP** for cited Patron ruling rather than expanding file/protected surface (`brief.md` Q2/Q3/Q6; FR-003–FR-005; Round 1 fixes 3, 4, 6). No production edits in this task.

---

## Phase 2: User Story 1 - Reproduce and establish causal diagnosis (Priority: P1)

**Goal**: Q2 baseline on idle 17-movie Compose stack; isolate web-proxy vs API/handler/persistence/query/cache/external-wait; evidence-backed causal chain — or `needs recon` stop.
**Independent Test**: Diagnosis receipt published with baseline samples and causal chain (or explicit stop). No production code change required.
**Order**: T002 → T003. **Build lane** owns otherwise-unassigned measurement/diagnosis work.

- [X] T002 [US1] **Build lane**. After T001: On the idle 17-movie Compose stack, with **no** concurrent build/gate/test/competing measurement/packaging load during sample windows, run the complete Q2 before protocol via Invoke-WebRequest through web port 3000 for `GET /api/movies/{knownId}`, `GET /api/movies/9999` (expect 404), `GET /api/movies/{seed}/recommendations`, and `GET /api/recommendations/for-you?take=10` using T001-collected fixtures and route-specific oracles. Observe readiness without prewarming the target; pin/reset dataset/history between comparable runs. Capture first-after-readiness separately, then 20 **sequential** samples each with elapsed time, HTTP status, and payload validity checked against T001 oracles (known details identity/content; 9999 404; by-movie eligible ranking/filtering; for-you fixture expectations including non-empty where eligible); record median, p95, max. Repeat genres/movie-list **control** batches before and after target batches; record host/control noise and investigate comparability without adding numeric acceptance thresholds for controls. Then repeat the 20-request target batches after a settled post-startup/post-load interval of at least 60 seconds under the same isolation. Record dataset/seed/history, environment, build/head, resource/container state, and **safe config identity only**. Reproduce/exercise the post-startup for-you degradation path described by the ticket; when T003 establishes the diagnosed timeout trigger, attach that evidence to this baseline receipt. Record contaminated runs and **rerun** for valid comparison rather than silently ignoring failing samples. Publish the complete baseline receipt under `git rev-parse --git-path dev-483-evidence` (scrubbed commands + raw samples + settled re-tests + provenance + summary; no secrets/env/config dumps) and complete it before any production edits. Baseline failures and misses are diagnostic observations only — do not claim acceptance PASS from baseline alone. **No unconditional concurrent acceptance leg** — retain Q2 sequential acceptance (FR-009–FR-012; SC-001–SC-002 baseline half; `brief.md` Q2; Round 1 fixes 1, 3, 5, 7).
- [X] T003 [US1] **Build lane**. After T002: Using **existing** Activity/log diagnostic facilities only (exact facilities from T001 recon — do not invent names/APIs), isolate web-proxy time versus API/handler/persistence/query/cache/external-wait time across the Q3 candidate seams named in `plan.md` / `recon-DEV-483` lines 11–16. Compare matched direct-API and web-proxy (port 3000) requests with transport differences recorded; web port 3000 remains the acceptance path. Investigate concurrency **only when evidence indicates it** — observe cancellation propagation / abandoned work without presuming pool exhaustion; do **not** add an unconditional concurrent acceptance leg. Write an evidence-backed causal chain naming the proven bottleneck; attach diagnosed for-you timeout-trigger evidence to the T002 baseline receipt so the before-protocol receipt is complete before T004. **If the diagnosed trigger was absent from T002, rerun the affected complete baseline protocol with that trigger before T004/production edits.** Temporary instrumentation must be accounted for and removed before fix acceptance, with measured build identity recorded; any retained instrumentation is a production change inside the approved candidate boundary and must be gated. Preserve Core dependency direction and layering evidence through existing tests (obtain suite facts via recon if needed). **If cause cannot be established, STOP `needs recon` and do not proceed to T004.** If the proven cause requires any tracked file outside the Q3 candidate set or any §2.3 item 1–6 surface (Compose/proxy config, schema/index/migration, startup registration, dependency, LocalPlay/secrets/Process.Start, public API), **STOP for a deliberate cited Patron ruling** before any edit — outside-boundary cause is never permission to edit (`brief.md` Q3; FR-003–FR-005; SC-003 diagnosis half; Round 1 fixes 1, 4).

**Checkpoint**: US1 complete only with causal chain or explicit stop.

**Phase 2 execution record (Build lane, 2026-11-13, head `30f04861`)**: T002 and T003 complete. Evidence `git rev-parse --git-path dev-483-evidence`: `T003-causal-chain.md` (causal chain + baseline tables + limits), `cold-before.log`, `q2-before.log`, `q2-before.json`, `dataset.log`, `containers.log`, `resources.log`, `q2-measure.ps1`. Runtime restored from the preserved Compose catalog dump and pinned to 17 movies with realistic keyword/full-cast density; history created through `POST /api/movies/{id}/watched`. Before-state medians via web port 3000: details known 1199 ms, details 9999 6 ms, by-movie 20423 ms, for-you 14367 ms; controls 4–6 ms. Proven cause: EF Core multiple-collection `Include` cartesian product materialized in-process (32,400 rows/movie; ~5.99M rows per by-movie request) while Postgres measured 63 ms of 20.19 s. No production or test edits; no instrumentation added. **Forward STOP for T004/T005** recorded in the receipt: the by-movie and for-you routes also enter the fan-out through `EfMovieRepository.LoadTrackedRecordAsync` (`EfMovieRepository.cs:575-584`), outside the Q3 candidate list — needs a deliberate cited Patron ruling before any edit.

---

## Phase 3: User Story 2 - Minimal causal fix with preserved behaviour (Priority: P1) 🎯 MVP

**Goal**: Smallest proven causal production fix inside Q3 candidates; deterministic regressions at the fault seam; preserve contracts and recommendation semantics.
**Independent Test**: New/extended behavioural tests green; existing suites for preserved semantics green; no unit latency timers.
**Order**: T004 → T006. Exact files depend on T003 — edit only the proven subset. **Build lane** owns otherwise-unassigned fix/regression work.

### Tests for User Story 2

- [X] T004 [US2] **Build lane**. After T003 (cause established; boundary clear; any newly diagnosed trigger re-baselined in T002/T003): Add failing deterministic regression coverage at the **diagnosed fault seam** in the justified existing suite(s) among `tests/LamuFlix.UnitTests/Features/Library/GetMovieDetailsQueryHandlerTests.cs`, `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs`, `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs`, `GetForYouRecommendationsQueryHandlerTests.cs`, `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs`, and/or `ApiEndToEndRecommendationTests.cs`. Include a deterministic **performance-relevant invariant selected from the actual diagnosis** (query/round-trip bounds only if causal — do not invent). Preserve known/missing details, cancellation, ranking/filtering, and for-you history assertions. **If** `HybridCachedRecommendationProfileSource` / cached profile source is touched, prove preservation of existing cache identity/history boundaries with regression coverage; collect missing cache-identity/history semantics through recon — never invent per-user architecture. If the seam depends on query translation or persistence serialization, use Testcontainers — never driver mocks. Do **not** add machine-dependent wall-clock latency asserts in unit/integration tests (FR-006–FR-008; SC-004; `brief.md` Q4; Round 1 fix 2).

### Implementation for User Story 2

- [X] T005 [US2] **Build lane**. After T004: Apply only the minimal proven causal fix in the minimal subset of Q3 candidate production files proven by T003 among `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs`, `RecommendationEndpoints.cs`, `src/LamuFlix.Core/Features/Library/GetMovieDetailsQueryHandler.cs`, `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`, `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs`, `GetForYouRecommendationsQueryHandler.cs`, `src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs`, `EfRecommendationProfileSource.cs`, `HybridCachedRecommendationProfileSource.cs`. Preserve routes, DTO field sets, HTTP statuses (including 404), cancellation, ranking/filtering, history behaviour, and the C# DTO → OpenAPI → generated TypeScript chain. If cached profile source is edited, preserve existing cache identity/history boundaries (semantics from recon). Use `TimeProvider` for any injected time; no explanatory comments beyond AGENTS exemptions. No speculative cache/index/algorithm/framework change; no `/web` edits; no new dependency/layer/schema (FR-001–FR-005, FR-017; `brief.md` Q1/Q3; Round 1 fixes 2, 4).
- [X] T006 [US2] **Build lane**. After T005: Make T004 green against T005 without weakening assertions (including the diagnosis-selected performance-relevant invariant and any cache identity/history regressions); remove temporary instrumentation before claiming fix acceptance (or justify retained instrumentation as gated production change with measured build identity); rerun the touched existing suites listed in T004 to confirm preserved semantics (FR-006; SC-004; Round 1 fixes 2, 4).

**Checkpoint**: US2 independently testable at the fault seam.

**Phase 3 execution record (Build lane, 2026-11-13, head recorded in receipt)**: T004, T005, T006 complete. Evidence `git rev-parse --git-path dev-483-evidence`: `T004-T006-fix-receipt.md`. Causal fix: `.AsSplitQuery()` at the three proven fan-out seams — `EfMovieCatalog.GetDetailsAsync`, `EfMovieRepository.LoadTrackedRecordAsync` (boundary expanded by the Phase 2 Causal Boundary & Dataset Ruling), and `EfRecommendationCandidateSource.Traversable`. Same idiom already used by `EfMovieRepository.FindFailedMoviesAsync`, so no new mechanism, dependency, schema, index, migration, API shape, route, DTO, status, LocalPlay, secret, or `/web` change. Four new Testcontainers regression tests at the seam, red before the fix (6 fanned association joins vs 1 allowed) and green after; the diagnosis-selected invariant is "no single SQL command joins more than one of the six movie association tables", which is causal and not a wall-clock timer. Four pre-existing round-trip-count/query-shape assertions were re-based to the new bounded split-query shape (1 vector + 1 root + 6 per-collection readers, at most one association table each) with no assertion deleted or loosened. Affected integration suites: 495 passed / 0 failed. `dotnet format --verify-no-changes` on the six changed files: exit 0. A non-isolated runtime spot-check on the rebuilt stack showed details 142.7 ms, 9999 118.4 ms, for-you 269.5 ms, by-movie warm median 103.6 ms versus before-fix medians of 1199 / 6 / 14367 / 20423 ms; the cold by-movie sample still exceeded 1 s. That is a spot-check only — T007 must run the full isolated Q2 protocol on this head before any acceptance claim, and T009 gates remain open.

---

## Phase 4: User Story 3 - Compose after-measurement (Priority: P1)

**Goal**: Q2 after-measurement proving every sample meets targets, including for-you timeout trigger exercise and settled re-tests.
**Independent Test**: Before/after Compose receipt with all required samples, or explicit Could not run / open acceptance.
**Order**: T007. Must complete before T009. **Build lane** owns measurement work. No concurrent build/gate/test/packaging load during sample windows.

- [X] T007 [US3] **Build lane**. After T006: With **no** concurrent build/gate/test/competing measurement/packaging load during sample windows (T009 must run **sequentially after** this task — never overlapping sample windows), rerun the full Q2 protocol on the same idle 17-movie dataset and recorded conditions via Invoke-WebRequest through web port 3000 for known-ID details, id `9999` 404, by-movie recommendations, and `for-you?take=10`. Observe readiness without prewarming; pin/reset dataset/history between comparable runs. Capture cold first + 20 **sequential** sample batches with median/p95/max/status/payload validity against T001 oracles. Repeat genres/movie-list control batches before and after; record host/control noise and investigate comparability without new numeric acceptance thresholds. Reproduce/exercise the diagnosed post-startup for-you timeout trigger; repeat 20-request batches after a settled post-startup/post-load interval of at least 60 seconds under the same isolation. Require every measured sample strictly `< 300 ms` (details known + 404) and `< 1 s` (both recommendation routes), including cold and settled re-tests. Record contaminated runs and rerun rather than silently ignoring failing samples. Publish a reviewable before/after receipt under `git rev-parse --git-path dev-483-evidence` with scrubbed exact commands, first/batch/re-test data, validity checks, diagnosed trigger, causal evidence pointer, safe config identity, and limitations — no secrets/env/raw config dumps. If runtime unavailable or evidence fails, mark **Could not run** / keep acceptance open — never warmed-cache-only or synthetic PASS. Failed/unavailable runtime remains open and cannot yield Done when (FR-009–FR-013; SC-001–SC-002; SMOKE-001–003 runtime; `brief.md` Q2; Round 1 fixes 3, 5, 7).

**Checkpoint**: Runtime acceptance evidenced or explicitly open.

**Phase 4 execution record (Build lane, 2026-11-13, head `5717d17d`)**: T007 complete as measurement. Evidence `git rev-parse --git-path dev-483-evidence`: `T007-after-measurement-receipt.md`. Full Q2 protocol rerun through web port 3000 on the identical pinned 17-movie dataset (17 movies / 340 keyword links / 1020 cast links / ids 1-3 watched — unchanged from the T002 baseline), no concurrent build/gate/test/packaging load, 65 s settled re-test, diagnosed post-startup trigger exercised by restarting the API and observing readiness at `/health/ready` only so nothing was prewarmed. Result split: **all 40 sequential batch samples and all 40 settled re-test samples clear their limits** (details known median 10.6 ms / max 16.2 ms against 300 ms; 9999 404 median 4.7 ms / max 8.3 ms; by-movie median 75.0 ms / max 111.2 ms; for-you median 141.8 ms / max 194.9 ms; settled: 10.1 / 5.0 / 66.5 / 161.9 ms; controls steady at 4-6 ms, so no quieter-machine artefact). Against the before-baseline that is 113x on details, 272x on by-movie and 101x on for-you for the same dataset and path. **Cold first request after readiness remains OVER limit** and this is recorded as explicitly open, not as PASS: details 475 ms (limit 300) and by-movie 1291 ms (limit 1 s), while 9999 404 (37 ms) and for-you (466 ms) pass cold. The cold overshoot is first-hit process cost, not the fixed fan-out: the API log shows 1257 ms finished against ~60 ms of summed DB command time for that same cold by-movie request, and three separately restarted processes reproduced it (details 406/460/557 ms; by-movie 1367/1508/1407 ms, each followed by 21-29 ms / 149-206 ms on the next request). A non-target `/api/genres` call before details brings details to 243.6 ms (under limit), and a 30 s settle does not help (details 340.9 ms, by-movie 1034.5 ms), so it is first-use cost rather than a warmup delay. Clearing it would require a warmup/query-preparation change outside the proven causal fix and outside the approved Phase 3 file set, so it needs a cited Patron ruling before any edit; T007 does not warm the target to manufacture a pass. Runtime acceptance therefore stays **open and cannot yield Done when**; T009 gates are unrun and must still run sequentially after this task.

---

## Phase 5: Mandatory .NET gates (after T007; before T008)

**Purpose**: Honest gate reporting for in-scope production C# changes. Runs only after T007 sample windows complete (sequential isolation).
**Checkpoint**: Gate receipt with distinct verdicts; failed/Could not run remain open and cannot yield Done when.

- [ ] T009 **Gauge** / Build-lane gate execution. After T007 (sample windows complete; never concurrent with T002/T007 measurement): run the mandatory delivery verification required by brief.md:73-75 / Q4 and constitution.md:750-756 against the implementation HEAD using **actual supported scripts** and **configured applicable** complexity/refactor obligations: `dotnet build` (required in addition to all analyzer gates — never a substitute for them), `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (including applicable configured refactor-gate obligation — no hardcoded/lowered threshold or invented opt-out), `./scripts/run-jetbrains-inspectcode.ps1`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and pre-PR `./scripts/run-mutation.ps1` when production C# is eligible. Mutation eligibility/exemptions and exact SKIPPED (scope-empty) / SKIP / N/A / FAIL / PASS / Could not run come from **actual script/config output**, never assumed by project name. Do **not** open/change preserved `harness.yml` dirt. Missing configuration facts go through recon. Record exact commands, HEAD, exit codes, and distinct verdicts. Never fold non-pass into PASS; never lower thresholds or edit generated gate configuration. Failed/Could not run gates remain open and cannot yield Done when (FR-015; SC-005; `brief.md` Q4; constitution.md:751; Round 1 fixes 5, 6).

---

## Phase 6: User Story 4 - Cause documentation (Priority: P2)

**Goal**: Verified YouTrack cause documentation via Patron→Rigger after runtime and gate status are recorded.
**Independent Test**: Rigger verified read-back on DEV-483.
**Order**: T008 after T007 and T009 status recorded (brief Q6). Early “After T003 / when available” routing is forbidden.

- [ ] T008 [US4] After **T007 outcome/status recorded** and **T009 mandatory gate run/status recorded** (both required first per brief Q6 / Round 1 fix 9): **Patron** routes the verified causal diagnosis and evidence package to **Rigger**. **Rigger** updates YouTrack DEV-483 with the documented cause and returns verified read-back. No other seat writes YouTrack for this Done when item. Failed/unavailable runtime or gates remain open and **cannot** yield Done when even if cause documentation is recorded (FR-014; SC-003; `brief.md` Q3/Q5/Q6; Round 1 fix 9).

---

## Phase 7: Review / remediation (Q5 closing bar)

**Purpose**: Capped three-axis implementation review under the frozen closing bar.
**Checkpoint**: Zero above-bar findings or unresolved items explicitly left for user review on the PR at the 2×2 cap — never auto-cleared. After any T010 remediation commit, full mandatory gates and invalidated Q2/cause evidence must be refreshed on final HEAD before packaging.

- [ ] T010 After T007–T009 and T008 status recorded: Conduct capped implementation review/remediation (at most two rounds and two fix commits per round; one consolidated numbered fix list per round) requiring Sentry/Risk, Ledger/Standards, and Compass/Spec reports pinned to the reviewed head. Closing bar: every verified in-scope correctness, security, contract, or ticket-acceptance violation at any severity including Low blocks until fixed or proven invalid; other accepted non-blocking Low items need explicit recorded disposition; out-of-scope observations follow Q1 (follow-up ticket vs noted). Unresolved blockers at the cap remain open for user review — never silently expand scope or declare success. **After any T010 remediation commit**: rerun the full mandatory gate set on the final delivery/review HEAD before packaging; explicitly annotate stale receipts; refresh affected Q2 runtime evidence and cause documentation/read-back when the change invalidates them. Never label an old-head run as final-head PASS (`brief.md` Q5; FR-016; SC-006; Round 1 fix 8).

---

## Phase 8: Delivery handoff

**Purpose**: Prepare merge-bar evidence packaging; only the user merges.
**Checkpoint**: No claim of merge clearance by agents. Final-head freshness required.

- [ ] T011 After T010: Publish exact-head merge-bar evidence package for DEV-483 per pipeline (gates, Q2 receipt status, Rigger verified cause read-back, three-axis review) using only receipts that match the **final delivery/review HEAD**. Explicitly annotate any stale receipts; ensure T009 full gate set and any invalidated T007/T008 evidence were refreshed after the last T010 remediation commit. Leave merge to the user. Do not merge or enable auto-merge. Do not claim PLAN FROZEN, Gate 1, or runtime PASS from packaging alone. Only Keel declares PLAN FROZEN after independent reread (SC-006; `brief.md` Q5/Q6; Round 1 fixes 8, 10).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (spec-PR + T000)**: Bernstein/Rigger produce/open; user merges; Keel receipt blocks all Build work
- **Phase 1 (T001)**: Depends on T000; blocks diagnosis
- **US1 (Phase 2)**: Depends on T001; **blocks US2** until causal chain established (or stops)
- **US2 (Phase 3)**: Depends on T003 cause+boundary (and any required T002 re-baseline with diagnosed trigger)
- **US3 (Phase 4 / T007)**: Depends on T006 fix; **blocks T009** (sequential sample isolation)
- **Gates (Phase 5 / T009)**: Depends on T007 complete; **blocks T008**
- **US4 docs (Phase 6 / T008)**: Depends on T007 outcome/status **and** T009 gate run/status recorded first (brief Q6) — no early After-T003 routing
- **Review (Phase 7 / T010)**: Depends on T007–T009 and T008 status recorded; remediation may require final-head gate/runtime/cause refresh
- **Delivery (Phase 8 / T011)**: Depends on review cap reached with final-head freshness

### User Story Dependencies

- **US1 (P1)**: After T001 — diagnosis MVP prerequisite
- **US2 (P1)**: After US1 causal chain — minimal fix
- **US3 (P1)**: After US2 — Compose acceptance (before gates packaging order)
- **US4 (P2)**: After T007 + T009 status recorded — documentation cannot close Done when while runtime/gates remain open

### Parallel Opportunities

- No parallel T007/T009: sample windows forbid concurrent build/gate/test/packaging load; T009 runs only after T007 completes
- No parallel T002 measurement with competing load
- Controls receive the same isolation as targets
- No parallel production edits to the same candidate file
- Contaminated measurement runs are recorded and rerun — never silently ignored

---

## Implementation Strategy

### MVP First (Diagnosis + minimal fix)

1. Complete Phase 0–1: Gate 1 + pickup recon / oracles
2. Complete Phase 2: US1 diagnosis (STOP if `needs recon`; re-baseline if new trigger)
3. Complete Phase 3: US2 minimal fix + regressions (diagnosis-selected invariant; cache boundaries if touched)
4. **STOP and VALIDATE**: Behavioural suites green; then Q2 after-measurement (T007) → gates (T009) → cause docs (T008)

### Incremental Delivery

1. Diagnosis evidence → enables fix selection
2. Minimal fix → behavioural confidence
3. Compose receipt → gate receipt → Rigger docs → Done when evidence (all required; none substitutes for another)
4. Capped review with final-head freshness → awaiting user merge

### Parallel Team Strategy

Single Build lane preferred for same-file candidate seams and for sequential measurement/gate windows; do not invent named seats for otherwise-unassigned tasks.

---

## Notes

- `[P]` tasks = different files, no incomplete dependencies
- `[Story]` label maps task to US1–US4 from `spec.md`
- Do not invent root cause, fixture IDs, oracles, diagnostic API names, or expanded file edits
- Preserve SKIPPED/SKIP/N/A/Could not run as distinct from PASS; mutation eligibility from actual script/config output
- Evidence under `git rev-parse --git-path dev-483-evidence` only; scrubbed handoff; never commit secrets/config dumps
- Commit message format when committing: `DEV-483 - {subject}` (only when asked to commit)
- Avoid: speculative optimization, warmed-cache-only claims, unconditional concurrent acceptance, YouTrack writes by non-Rigger seats, merging, declaring PLAN FROZEN or Gate 1
- Round 1 repairs: `PLAN_ADJUDICATION.md` items 1–10 applied 2026-11-12
