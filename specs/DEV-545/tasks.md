---
description: "Task list for DEV-545 read-only recommendation evaluation harness"
---

# Tasks: Read-only recommendation evaluation harness

**Input**: Design documents from `specs/DEV-545/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q9), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `EVIDENCE.md`, ADR `docs/adr/0045-read-only-recommendation-evaluation.md`; Maestri notes `DEV-545`, `recon-DEV-545`, `recon-DEV-545-2`; `specs/PRODUCT.md`.

**Status**: Draft — Quill Spec Kit tasks from closed grill; plan-challenge PC1–PC3 binding; plan challenge repair round 1 applied. Not PLAN FROZEN. Gate 1 / T000 pending. No Build work before verified Gate 1 merge receipt. No implementation/task-completion/freeze claims in this repair.

**Tests**: Required (`brief.md` Task ordering / test strategy; FR-002–FR-010; PC3). TDD optional. Preserve existing scorer expected scores green. Docker/HTTP/pwsh only under `tests/LamuFlix.Evaluation.IntegrationTests` (Testcontainers.PostgreSql + WireMock.Net, existing CPM pins). Pure RealLibrary tests under `tests/LamuFlix.Evaluation/RealLibrary/`. Scorer/breakdown under `tests/LamuFlix.UnitTests/Features/Recommendations/`. Never driver mocks. No Pester/new framework/dependency/version.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/baseline tasks are never `[P]`.

**Conventions**: Edit only the authorized boundary in `plan.md` / brief Q9; STOP for cited Patron ruling before any other tracked file or §2.3 protected surface; STOP `needs recon` if a required path/fact is missing; no `/web` or public API edits; no explanatory comments beyond AGENTS exemptions; never invent seed IDs, ranks, or publication success; never lower harness thresholds; never commit outputs/credentials. Otherwise-unassigned implementation tasks are owned by the generic Build lane (do not invent named seats).

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel T000 records the verified merge receipt. No Build work (T001+) before Gate 1.

**Evidence**: Fill slots in `specs/DEV-545/EVIDENCE.md`. Disposable machine artifacts stay untracked/scrubbed — no secrets or connection strings.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (Wisp pickup recon + Keel reanalysis + Gauge baseline; read-only) — blocks T002
Phase 2   T002 → T003   (Foundational: synthetic/scorer invariance + shared artifact/denominator helpers)
Phase 3   T004 → T006   (US3 Q5 breakdown seam)
Phase 4   T007 → T009   (US2 Q3 loader + hidden-seed adapters)
Phase 5   T010 → T012   (US2 leave-one-out + US3 compare/pair)
Phase 6   T013 → T015   (US1 snapshot/diff)
Phase 7   T016 → T018   (US4 quality)
Phase 8   T019 → T019a → T020 → T021   (US5 seed lookup + identity gate + runtime runs with before/after DB split + scrub)
Phase 9   T022 → T024   (gates + three-axis review + Rigger publication)
```

Ordering per brief steps 1–8 / Q9. No dependency cycle. Failed/unavailable runtime expectations remain open — do not tune scores to hide them.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-545 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-545 in `specs/DEV-545/CONCLUSIONS.md`. Receipt only. T001–T024 MUST NOT proceed without verified merge (`brief.md` Status/Handoff; Q9; no structural owner checkbox on this ticket).

---

## Phase 1: Pickup recon (read-only; blocks T002)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; inventory exact paths for Quill-named RealLibrary helpers and any minimal seam need; Gauge baseline. No production edits.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying: `SimilarityResult.cs`, `SimilarityScorer.cs`, `GetForYouRecommendationsQueryHandler.cs`, `EfRecommendationProfileSource.cs`, `RecommendationOptions.CandidatePoolSize`, evaluation entry/csproj, `tests/LamuFlix.Evaluation.IntegrationTests`, existing SimilarityScorer* suites, `scripts/run-evaluation.ps1` and CI evaluation step non-touch confirmation, home measurement access facts without inventing IDs. **Keel** reanalyzes brief/spec/plan/tasks against landed main and amends only for authorized demonstrated drift. **Gauge** records pre-edit gate baseline. **STOP** `needs recon` on missing seams; **STOP** for cited Patron ruling rather than expanding file/protected surface (`brief.md` Q9). **Strictly read-only**.

---

## Phase 2: Foundational invariance and shared contracts (blocks user stories)

**Purpose**: Prove synthetic/scorer baselines remain green; introduce shared Q6 envelope and Q4/Q7 denominator helpers without behaviour change to synthetic publish.
**Checkpoint**: Synthetic publish path and existing SimilarityScorer expected scores green; shared types compile; no CI/fixture edits.

- [ ] T002 **Build lane**. After T001: Verify synthetic evaluation invocation remains intact for `tests/LamuFlix.Evaluation/Entry/EvaluationEntry.cs` + `scripts/run-evaluation.ps1` + `tests/LamuFlix.Evaluation/Fixtures/baseline.json` + CI evaluation step — add/adjust only non-behavioural dispatch hooks if required later; record current-main production scoring identity notes for EVIDENCE.md provenance (`brief.md` step 1; FR-005; SC-010).
- [ ] T003 [P] **Build lane**. After T001: Add C#-owned `tests/LamuFlix.Evaluation/RealLibrary/RecommendationArtifactEnvelope.cs` (no schemaVersion field) and `RecommendationArtifactSerializer.cs` following `Output/MetricsSummarySerializer` compact UTF-8 / `.partial` atomic publish; add `RealLibraryMetricCalculator.cs` nullable empty-observation wrapper; extend `tests/LamuFlix.Evaluation/Metrics/MetricCalculator.cs` with one shared rank-accumulation path for HR@1/@3/@10 while preserving synthetic API/output/zero behaviour; tests in `RecommendationArtifactEnvelopeTests.cs` and extended `Metrics/MetricCalculatorTests.cs` (null/unavailable empty-denominator per Q4/Q7; culture/adversarial finite-number golden vectors; cross-language `RealLibrary/Fixtures/capture-envelope.golden.json`). No duplicate rank loop; no second JSON library (`brief.md` step 1; FR-002/FR-007/FR-010; PC3 items 4/7/8).

---

## Phase 3: User Story 3 - Criterion breakdown seam (Priority: P1) 🎯 MVP slice

**Goal**: Internal SimilarityResult/SimilarityScorer breakdown without score/reason/API change.
**Independent Test**: Existing SimilarityScorerTests expected scores unchanged; contribution sums; reasons/order preserved.
**Order**: T004 → T006.

### Tests

- [X] T004 [P] [US3] **Build lane**. After T002/T003: Extend `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs` (no expected-score edits) and add `SimilarityResultBreakdownTests.cs` proving: ordinary weighted contribution sums; inactive → 0; non-identity zero-denominator → 0; identical-profile zero-weight branch emits identity-fallback diagnostic (value=1/weight=0/contribution=1, not strong-eligible); uncollected criteria omitted; empty default breakdown on two-argument construction/deconstruction; blend-style empty diagnostics; reason kind/text sequence unchanged. Accept: universal sum-to-score under PC1; no synthetic strong eligibility; no expected-score fixture edits (FR-008; SC-004; PC1; PC3 item 9).

### Implementation

- [X] T005 [US3] **Build lane**. After T004 authored: Extend `src/LamuFlix.Core/Features/Recommendations/SimilarityResult.cs` with a non-positional initialized empty immutable criterion breakdown property (name, value, weight, contribution) preserving existing two-argument construction and deconstruction (`brief.md` Q5; PC1; PC3 item 9; recon SimilarityResult.cs:6).
- [X] T006 [US3] **Build lane**. After T005: Populate breakdown from existing collected terms/denominator in `src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs`; on the existing identical-profile `ZeroWeightResult` branch emit the identity-fallback diagnostic entry; omit uncollected criteria; keep other zero-denominator contributions at 0.0; do not recompute a second score algorithm or change weights/filters/reason order; keep public DTO/OpenAPI untouched; production blend results remain empty diagnostics (FR-008/FR-012; PC1).

**Checkpoint**: Breakdown seam green; existing expected scores untouched.

---

## Phase 4: User Story 2 foundation - Read-only loader and hidden-seed adapters (Priority: P1)

**Goal**: Immutable library load + per-seed hide copies; Core free of EF/HTTP.
**Independent Test**: Testcontainers prove no-tracking read, no leakage, fresh copies, no mutation calls.
**Order**: T007 → T009.

- [X] T007 [US2] **Build lane**. After T006: Add `LamuFlix.Infrastructure` ProjectReference to `tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj` while keeping Core free of EF/HTTP and synthetic package set intact (FR-005; Q1).
- [X] T008 [P] [US2] **Build lane**. After T007: Implement `tests/LamuFlix.Evaluation/RealLibrary/RealLibrarySnapshotLoader.cs` for one immutable no-tracking library snapshot; credentials only via environment `ConnectionStrings__DefaultConnection` — never CLI argument/history, commit, print, echo, or embed (FR-006/FR-011; Q3; PC3 item 5).
- [X] T009 [US2] **Build lane**. After T008: Implement runner-local `tests/LamuFlix.Evaluation/RealLibrary/HiddenSeedEvaluationCopy.cs` clearing hidden watched/review/profile and candidate IsWatched/Review while retaining the target; prefer no production seam; if a minimal handler/profile seam in ticket-named files is proven necessary, preserve all score/filter/reason behaviour, record necessity, and list it for the PR (FR-006/FR-012; Q3). Add IntegrationTests-only coverage in `tests/LamuFlix.Evaluation.IntegrationTests/RealLibrary/RealLibraryReadOnlyLoaderTests.cs` (authorize `Testcontainers.PostgreSql` PackageReference on `LamuFlix.Evaluation.IntegrationTests.csproj` if absent; existing CPM pin; no version change) proving: loader no-tracking; no hidden-target leakage; fresh per-seed copies; absence of save/add/update/remove. Accept: queries use no-tracking; hidden seed cleared while retained as target; each seed gets a distinct copy; mutation-call absence holds. No Docker tests under Evaluation project.

**Checkpoint**: Loader/adapters proven read-only.

---

## Phase 5: User Story 2 leave-one-out + User Story 3 compare/pair (Priority: P1)

**Goal**: LOO metrics and whole-library compare/pair using Q3–Q5.
**Independent Test**: Full ranking, absence, ties, contribution sums, diff-readable compare output.
**Order**: T010 → T012.

- [X] T010 [US2] **Build lane**. After T009: Implement `tests/LamuFlix.Evaluation/RealLibrary/LeaveOneOutRunner.cs` invoking real `GetForYouRecommendationsQueryHandler` with take = eligible candidate count, disclosing endpoint/validator bypass and harness-relative HR/MRR validity; liked/watched cohorts with overlap disclosure; HR@1/3/10 and MRR via shared `MetricCalculator` + `RealLibraryMetricCalculator` null wrapper; fail on load/scoring errors; wire `Entry/EvaluationEntry.cs` dispatch for `LAMUFLIX_EVALUATION_MODE=leave-one-out` without breaking synthetic publish; read optional `LAMUFLIX_EVALUATION_OUTPUT_PATH` (default `artifacts/recommendations/leave-one-out.json`); require pre-provisioned `ConnectionStrings__DefaultConnection` with **presence-only** validation (never read/echo/assign in tests or operator examples); extend `Entry/EvaluationEntryModeTests.cs` proving ordinary `dotnet test` without marker is inert, marker+nonsecret env inputs are required, credential presence is checked without echoing value, and mode/nonsecret markers are scoped/restored so examples do not leave real-library mode enabled (FR-005/FR-006/FR-007/FR-011; PC3 items 5/6/8; EVIDENCE §§3/8; `brief.md` step 4).
- [X] T011 [P] [US3] **Build lane**. After T006 and T008: Implement `tests/LamuFlix.Evaluation/RealLibrary/CompareRunner.cs` for whole-library compare over `EnrichmentStatus.Enriched` (score desc / ID asc), persisted-vector production-equivalent cosine with null cosine/plotRank for unusable pairs, usable-only plot order, pool flag `(plotRank is not null && plotRank <= CandidatePoolSize) || sameCollection` with selected-unusable disclosure, breakdown rows, pair-inspect matched names from runner-local loaded ID/name intersections, and diff-compatible artifact serialization; wire `LAMUFLIX_EVALUATION_MODE=compare` reading required `LAMUFLIX_EVALUATION_SELECTED_ID` (positive int; reject missing/non-positive), optional `LAMUFLIX_EVALUATION_OTHER_ID` (when set: positive int ≠ selected → pair-inspect that directed pair only; when unset → whole-library compare), and optional `LAMUFLIX_EVALUATION_OUTPUT_PATH` (defaults `artifacts/recommendations/compare-{selectedId}.json` / `pair-{selectedId}-{otherId}.json`); same pre-provisioned presence-only credential rule and mode/nonsecret scope/restore as T010; document complete commands in EVIDENCE §4/§8 using synthetic example IDs only (never invent home IDs) (FR-005/FR-009/FR-011; Q5; PC2; PC3 items 5/9; ASSUMPTIONS.md).
- [X] T012 [US2][US3] **Build lane**. After T010/T011: Add pure tests in `tests/LamuFlix.Evaluation/RealLibrary/LeaveOneOutRunnerTests.cs` and `CompareRunnerTests.cs` covering: full ranking; target absence → 0; zero-cohort null metrics; cohort overlap/attempted denominators; load/scoring failure integrity; pair diagnostic contribution sums (not fabricated blend sums); tie ordering; missing selected/candidate vector, wrong dimension, nonfinite, zero-norm, and usable-pair tie cases without dropping enriched rows or fabricating cosine 0; null plot diagnostics; disclosed ticket pool formula; pair-inspect matched names from intersections (never reason parsing); compare artifact compatibility. Accept: overlap disclosed; HR/MRR use attempted denominators via shared MetricCalculator path; fail-on-error integrity holds; PC2 vector/null/pool rules hold (FR-007/FR-009; PC2; `brief.md` steps 3–4).

**Checkpoint**: LOO + compare independently runnable on fixtures/Testcontainers.

---

## Phase 6: User Story 1 - Snapshot and diff (Priority: P1)

**Goal**: PowerShell Capture/Diff with deterministic envelope.
**Independent Test**: Byte-equal repeated captures; five Diff groups; fail-fast without partial success.
**Order**: T013 → T015.

- [X] T013 [US1] **Build lane**. After T003: Add `scripts/run-recommendation-snapshot.ps1` (PowerShell 7) with BaseUrl default `http://localhost:3100`, SeedIds contract (five positive ints validated before HTTP; overrides supported; titles documented; literal IDs placeholder until T019), OutputPath under `artifacts/recommendations/`, Modes Capture/Diff (FR-001/FR-003; Q2/Q6).
- [X] T014 [US1] **Build lane**. After T013: Implement Capture calling For-you `take=30` and Related `take=10` per seed, preserving exact API order and reason kind/text; record empty Related success; omit timestamp/host/elapsed; emit canonical compact UTF-8 JSON (no BOM/no terminal newline; invariant finite numerics; fixed property order) via `.partial` atomic publish conforming to C#-owned envelope golden fixtures; fail on HTTP/JSON/schema/nonfinite errors without partial success (FR-002; SC-001; PC3 item 4).
- [X] T015 [US1] **Build lane**. After T014: Implement Diff validating same-mode compatible mode/target/take only (capture/capture or compare/compare; reject mixed modes), matching movieId within lists, always printing moved up/down/entered/left/reasons changed (kind/text sequence), rejecting malformed/duplicate IDs. Adapt `tests/LamuFlix.Evaluation.IntegrationTests/TestSupport/EvaluationCommand.cs` solely to also launch `scripts/run-recommendation-snapshot.ps1` (preserve existing `run-evaluation.ps1` path; test-only Process.Start). Authorize `WireMock.Net` PackageReference on IntegrationTests csproj if absent (existing CPM pin). Add `tests/LamuFlix.Evaluation.IntegrationTests/Entry/RecommendationSnapshotCommandTests.cs` for: byte/hash-identical repeated captures; exact API order; explicit takes; five-ID validation; empty Related success; HTTP/JSON/schema/duplicate rejection without partial publication; five Diff groups; same-mode acceptance and cross-mode rejection; culture/adversarial finite-number golden vectors. Accept: order and takes match contract; invalid/duplicate/cross-mode inputs fail with no success artifact published (FR-002/FR-004; SC-001/SC-002; PC3 items 4/7; `brief.md` step 5).

**Checkpoint**: Capture/Diff script behaviour covered; parameter-free defaults still pending T019 IDs.

---

## Phase 7: User Story 4 - Quality report (Priority: P2)

**Goal**: Coverage, samples, judged pairs.
**Independent Test**: Partition sums; deterministic samples; skipped-ID denominators.
**Order**: T016 → T018.

- [X] T016 [US4] **Build lane**. After T011: Implement `tests/LamuFlix.Evaluation/RealLibrary/QualityReportRunner.cs` coverage table for default bars with score-only and score-OR-strong lines; Collection/Director only before Themes exists; identity-fallback never strong-eligible; four buckets partition `EnrichmentStatus.Enriched` selected population; median 1st/10th with counts/nulls; wire `LAMUFLIX_EVALUATION_MODE=quality` reading optional `LAMUFLIX_EVALUATION_BARS` (comma-separated invariant decimals; default `0.15,0.20,0.225,0.25,0.30,0.35,0.40`; reject nonfinite/non-ascending/duplicates), optional `LAMUFLIX_EVALUATION_STRONG_ONLY_BAR` (invariant decimal; default `0.225`; reject nonfinite/non-positive), and optional `LAMUFLIX_EVALUATION_OUTPUT_PATH` (default `artifacts/recommendations/quality.json`); same presence-only credential + mode/nonsecret scope/restore rules as T010; complete commands in EVIDENCE §5/§8 (FR-005/FR-010/FR-011; SC-006; Q7; PC1/PC2; PC3 item 5). Add `QualityReportRunnerTests.cs` proving every-line partition totals, strong-only/Themes non-synthesis, median/count/null rules, and bars/strong-only default+override validation. Accept: SC-006; Themes non-synthesis; median/count/null rules hold (`brief.md` step 6).
- [X] T017 [P] [US4] **Build lane**. After T016: Implement `tests/LamuFlix.Evaluation/RealLibrary/BandSampleSelector.cs` deterministic band `[lower,upper)` and strong-only (default 0.225) samples up to 12 via SHA-256 of UTF-8 invariant-decimal `selectedId + ":" + otherId` (no whitespace/newline; order by full digest bytes lexicographically then selectedId/otherId ascending; no GetHashCode/HashCode.Combine); disclose undersupply; add `BandSampleSelectorTests.cs` plus `RealLibrary/Fixtures/band-sample-hash.golden.json` covering golden synthetic vectors including direction reversal and cross-process stability (FR-010; SC-007; PC3 item 3; ASSUMPTIONS.md; `brief.md` step 6).
- [X] T018 [US4] **Build lane**. After T017: Implement directed judged-pairs reader under ignored `artifacts/recommendations/` (`movieId`,`otherId`,`verdict`,`note?`) using optional `LAMUFLIX_EVALUATION_JUDGED_PAIRS_PATH` (default `artifacts/recommendations/judged-pairs.json`); reject malformed/duplicate directed entries; skip unknown/unenriched visibly; good/bad shares; preserve ticket known-miss good pair The Naked Gun → Hot Shots! in owner-corrected baseline input unless owner changes verdict. Wire path into quality mode with T016 markers; document complete judged-pairs/bars/strong-only commands in EVIDENCE §5 (never invent home IDs). Add tests in `QualityReportRunnerTests.cs` for: skips/null denominators; directed good/bad share computation; denominator integrity (skipped IDs excluded; zero valid → null); rejection of malformed/duplicate directed entries without partial success; and judged-pairs path default/override (FR-010/FR-011; Q7; PC3 item 5; `brief.md` step 6).

**Checkpoint**: Quality mode complete on fixtures; home judgments remain Phase 8.

---

## Phase 8: User Story 5 - Runtime baseline execution (Priority: P1)

**Goal**: Seed lookup, current-main identity gate, parameter-free runs with split before/after DB proof, scrub.
**Independent Test**: EVIDENCE.md slots filled honestly; failed expectations remain open; baseline identity reconciled before runs; before-state preserved before leave-one-out.
**Order**: T019 → T019a → T020 → T021. T020 MUST NOT start until T019a passes. Leave-one-out in T020 MUST NOT start until before-state is captured and preserved. T021 compares after-state to that preserved before-state.

- [X] T019 [US5] **Build lane**. After applicable implementation checks for T002–T018: Perform one-time home-library lookup for the five ordered titles; populate literal `-SeedIds` defaults and help in `scripts/run-recommendation-snapshot.ps1`; record title→id table in `specs/DEV-545/EVIDENCE.md` (FR-003; Q2; `brief.md` step 7). Never use fixture ID 90. T019 records an earlier identity note only where needed for seed lookup — it does **not** authorize baseline publication identity (that is T019a immediately before T020). **Done**: ids `575, 518, 515, 519, 831` are the defaults and help text (EVIDENCE §1); parameter-free Capture verified twice, byte-identical.
- [X] T019a [US5] **Build lane**. After T019 and **immediately before T020**: Fill EVIDENCE.md §0 identity manifest using operator commands `git rev-parse`, `git diff`, and `Get-FileHash` (no new script): then-current `origin/main` SHA; harness commit + dirty status; relevant production recommendation Core handlers/scorer/weights/MMR/options and Infrastructure profile/candidate source diffs vs main; API running build/config identity; effective nonsecret `RecommendationOptions` (weights, CandidatePoolSize, MmrLambda, takes). Any main SHA, relevant content, build, harness, or effective-config mismatch stops for reconciliation/recapture. Feature-only diagnostic seam differences require bounded diff + existing score/reason invariance test receipts — a raw hash alone is not semantic equivalence proof. Never credentials. Refresh/recheck immediately before T020 and again before publication. If identity is invalidated after this check, require recapture (re-run T019a → T020 → T021) (`brief.md` steps 1/7–8; FR-013; PC3 item 1). T024 does not replace this pre-run gate. **Performed during Phase 8, gate STOPPED: manifest filled in EVIDENCE §0 and it found real mismatches** — deployed home API is image revision `b98a4b0a` (99 commits behind `origin/main` `8a2deb9b`) with differing recommendation handlers/profile calculator/profile source, and the Argo home revision `bd0a83fe` is off-main. Left unchecked because the gate did not pass; reconciliation (redeploy home to a main build) is an operator/environment action outside a Build-lane commit. **Option A run 2026-11-18: PASS** - see EVIDENCE §0 Option A manifest and §9.
- [X] T020 [US5] **Build lane**. After T019a: Confirm pre-provisioned process environment has `ConnectionStrings__DefaultConnection` (presence-only; never assign/echo/CLI) and owner-corrected judged-pairs file at `LAMUFLIX_EVALUATION_JUDGED_PAIRS_PATH` default (unversioned). Establish a quiescent window (no scan/enrichment or user mutation during Capture×2 and the DB proof window). **Paired recommendation-input fingerprint**: before Capture A, capture and preserve recommendation-input fingerprint/counts including stable pending/enrichment state into EVIDENCE.md §2 before slots; after Capture B (same window), capture the matching after observation into §2 after slots and require explicit before==after equality — on mismatch discard/retry that data window **before interpreting capture hashes**; do not treat every mismatch as a script defect; no new production background-job/script/metric controls. **Before leave-one-out begins**, also capture and preserve ticket-required DB before-state (row counts and every movie review value) into unversioned operator evidence / EVIDENCE.md §3 before slots. Then run parameter-free Capture twice; compare hashes only after fingerprint equality; run Diff; leave-one-out / compare / pair inspect / quality using the pinned EVIDENCE §3–§5/§8 commands (`LAMUFLIX_EVALUATION_MODE` + nonsecret env names; scope/restore markers). Verify About Time/Pirate Radio plot rank > 50 and outside pool (remains open until evidenced). Fill remaining EVIDENCE.md run slots (FR-013; SC-001/SC-003/SC-005; PC3 items 2/5/6; `brief.md` step 7). Keep raw personal evidence unversioned. Do not tune scores/expectations to hide failures. **Depends on T019a**; **feeds T021**. **NOT STARTED — blocked twice over**: T019a stopped on identity, and no `ConnectionStrings__DefaultConnection` is present in the operator process environment (presence-only check returned absent in process/user/machine scope). EVIDENCE §2/§3/§4/§5 runtime slots recorded `Could not run`. **Option A run 2026-11-18: PASS** - see EVIDENCE §0 Option A manifest and §9.
- [X] T021 [US5] **Build lane**. Immediately after the same T020 leave-one-out/baseline run: capture DB after-state (row counts and every movie review value); compare to the T020-preserved DB before-state and record both sides in EVIDENCE.md §3 before/after slots for the ticket-required SC-003 behavioral boundary, explicitly disclosing that counts+reviews do not prove every column or transient write; retain code-path inspection + Testcontainers no-write proof references. Confirm the §2 paired recommendation-input fingerprint/count after slot (including pending/enrichment state) was recorded and equals the T020 before slot for the Capture×2 + DB proof window; on inequality the window is already discard/retry territory under T020 (do not reinterpret unequal hashes as script success). Scrub draft evidence and provenance for credentials/echoed secrets and remove any connection-string shell-assignment examples. Confirm `git status` excludes outputs/judgments; confirm no connection string committed, printed, or embedded in evidence/provenance/notes (SC-008; FR-011; PC3 items 5/6). **Depends on T020** preserved before-state and before fingerprint; evidence acquired only after the run cannot prove non-mutation during that run. **Split outcome**: scrub half PASS (§6 — `git status` shows only the three intended files, `artifacts/` ignored, no secret-shaped strings, no assignment examples); DB after-state half `Could not run` because T020 never ran, so no before-state exists to compare and no post-hoc dump was substituted. **Option A run 2026-11-18: PASS** - see EVIDENCE §0 Option A manifest and §9.

**Checkpoint**: Runtime evidence complete or honestly pending/Could not run — never silent invent. Identity gate and split before/after DB proof both present.

**Phase 8 status at this commit (Anvil)**: T019 complete. T019a ran and **stopped on a genuine identity mismatch** (EVIDENCE §0): the deployed home API is image revision `b98a4b0a`, 99 commits behind `origin/main` `8a2deb9b`, with recommendation handler / profile calculator / profile source differences, and the Argo `lamuflix-home` revision `bd0a83fe` is off-main. T020 therefore did not start, and there is also no `ConnectionStrings__DefaultConnection` in the operator process environment (presence-only check: absent in process/user/machine scope). T021's scrub half is complete; its DB after-state half did not run because no before-state exists to compare. The checkpoint's "split before/after DB proof both present" condition is therefore **not met** — honestly pending, awaiting a main-build home deployment plus a pre-provisioned credential. T019a/T020/T021 remain unchecked on purpose; do not start Phase 9 on the strength of this receipt.

---

## Phase 9: Polish — gates, review, publication

**Purpose**: Repo gates, Size L review loop, Rigger baseline publication.
**Checkpoint**: Honest gate labels; Rigger-verified YouTrack attachments; no merge by seats.

- [ ] T022 **Build lane**. After T021 (or after implementation-complete if runtime blocked with honest pending): Run Phase B gates against actual diff — `dotnet test`, `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (and refactor `-Threshold 6` when applicable), `./scripts/run-jetbrains-inspectcode.ps1`, property/package/format gates, and `pwsh -NoProfile -File ./scripts/run-mutation.ps1` when `src/` changed. Record exact SHA and honest PASS/FAIL/SKIPPED/SKIP/N/A/Could not run labels. Confirm `baseline.json`, `scripts/run-evaluation.ps1`, and CI evaluation step unchanged; list every production seam in the PR description (FR-012/FR-014; Q9).
- [ ] T023 **Sentry / Ledger / Compass**. After T022: Size L three-axis review under Q8 closing bar (including Low); max two review/fix rounds; unresolved blockers → Bernstein; no silent waiver (`brief.md` Q8).
- [ ] T024 [US5] **Rigger**. After T020–T022 evidence exists and T019a manifest recheck passes: Attach baseline snapshot, leave-one-out, five compares, and quality report to DEV-545 YouTrack comment with provenance (main SHA, scoring equivalence, harness SHA, configuration, counts — never credentials) and the LOO full-eligible/harness-relative HR/MRR validity disclaimer (absolute metrics are harness-relative; only same-protocol deltas are comparable). Report verified publication (FR-013; SC-009; PC3 item 6; `brief.md` step 8). User retains merge authority — seats never merge.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Blocks all Build work
- **Phase 1 (T001)**: Blocks foundational implementation
- **Phase 2**: Blocks US seams that depend on shared envelope / invariance proof
- **Phases 3–7**: Follow brief order; US1 script can proceed after shared envelope (T003) even while LOO continues, but runtime baseline needs all parts
- **Phase 8**: Requires implementation checks green enough to run; runtime access required. Order is strict: T019 → T019a (identity gate) → T020 (preserve before-state, then run) → T021 (after-state compare). No parallelisation inside Phase 8.
- **Phase 9**: Gates/review after implementation; publication after runtime artifacts. T024 depends on T020–T022 evidence and does not replace T019a.

### User Story Dependencies

- **US3 breakdown (Phase 3)**: After foundational T002/T003
- **US2 loader/LOO (Phases 4–5)**: After breakdown for compare path; loader before LOO
- **US3 compare (Phase 5)**: After breakdown + loader
- **US1 snapshot (Phase 6)**: After shared envelope T003; literal IDs at T019
- **US4 quality (Phase 7)**: After compare runner; T016/T017/T018 own partition/median/strong/Themes, undersupply, and judged-share tests
- **US5 baseline (Phases 8–9)**: After Parts A–D implementation; T019a gates T020; T020 before-state gates T021

### Parallel Opportunities

- T003 parallel with T002 after T001
- T004 tests parallel before T005/T006 implementation sequencing
- T008 parallel with other files after T007
- T011 compare parallel with T010 LOO after shared prerequisites
- T017 parallel with adjacent quality files after T016
- T019a / T020 / T021 are never `[P]` — sequential identity and DB-invariance chain

---

## Implementation Strategy

### MVP First

1. T000 → T001
2. Foundational T002–T003 + US3 breakdown T004–T006
3. Validate scorer invariance independently

### Incremental Delivery

1. Add loader/hidden-seed → LOO
2. Add compare/pair
3. Add snapshot/diff
4. Add quality
5. Runtime baseline + gates + Rigger publication

### Notes

- Prefer runner-local hidden-seed adapter; production seam only if proven necessary and listed.
- Downstream tickets (DEV-518/544/543/540/539) consume the published baseline — do not claim their work inside this task list.
- Exact test/file homes are pinned in `plan.md` Source Code and T003–T018 above; do not relocate Docker/HTTP/pwsh tests into Evaluation, invent alternate frameworks, or add schema-version capture metadata.
- Plan challenge repair round 1 does not mark any implementation task complete and does not claim PLAN FROZEN.
