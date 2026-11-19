---

description: "Task list for DEV-518 ordered six-criterion similarity scorer"
---

# Tasks: Ordered six-criterion similarity scorer

**Input**: Design documents from `specs/DEV-518/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q9), `CONCLUSIONS.md`, `EVIDENCE.md`; Maestri notes `DEV-518`, `recon-DEV-518` (lines 7–28, 30–69); `specs/PRODUCT.md`.

**Status** — PLAN FROZEN. Gate 1 awaits owner merge of the spec PR. Implementation gates and runtime acceptance are pending.

**Tests**: Required (`brief.md` Test strategy; FR-001–FR-014). TDD optional. Persistence/query-translation behaviour uses Postgres Testcontainers — never driver mocks. No new test framework/dependency. `SimilarityCriteriaTableTests` is created if absent (recon noted not found).

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/evidence/baseline tasks are never `[P]`.

**Conventions**: Edit only the authorized targeted set in `plan.md` / brief Q6–Q7 (production source list plus authorized test/compare surfaces resolved by T001); STOP for cited Patron ruling before any other tracked production file or §2.3 protected surface; STOP `needs recon` if a required path/fact is missing; no `/web` or public API edits; no explanatory comments beyond AGENTS exemptions; never invent seed integers or snapshot contents; never lower harness thresholds. Otherwise-unassigned implementation tasks are owned by the generic Build lane (do not invent named seats).

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel T000 records the verified merge receipt. No Build work (T001+) before Gate 1. The spec PR includes [ADR 0046](../../docs/adr/0046-shared-recommendation-similarity.md) and [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md).

**Evidence**: Fill slots in `specs/DEV-518/EVIDENCE.md`. Disposable machine artifacts stay untracked/scrubbed — no secrets or connection strings. Honest pending runtime slots may allow capped review and PR preparation/opening; they do **not** clear Done-when or merge clearance. Done-when and merge clearance require published attachments verified by T023 read-back and observed acceptance. The delivery PR may open while evidence is pending.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (Wisp pickup recon; Keel read-only reanalysis; Gauge baseline) — blocks T001a / T002
Phase 1b  T001a         (Keel amends tasks.md only if T001 demonstrates authorized drift) — optional; blocks T002 when required
Phase 2   T002          (US1 DEV-545 Related + For You baseline before scoring edits) — blocks T003+
Phase 3   T003 → T007   (US2 feature mapping + keyword-count infra + lifecycle tests)
Phase 4   T008 → T011   (US3 options + ordered scorer authored first; SimilarStoryThreshold removed in T010 before T012 caller update — no solution build/green required yet)
Phase 5   T012 → T014   (US4 migrate three consumers + fixtures + Related SimilarStoryThreshold caller path; then require buildable solution + green tests)
Phase 6   T015 → T018   (US5 docs; T016 non-overlapping compare; T017 deferred compare + fixtures; six-criteria output proof after both; then legacy absence scan)
Phase 7   T019 → T021   (gates checklist; stage after/baseline comparisons; Fistful observed acceptance; publishable receipts)
Phase 8   T022          (capped three-axis review / remediation per Q8–Q9; may proceed with honest pending runtime)
Phase 9   T023          (delivery PR may open without attachments; Done-when + merge clearance require verified attachment read-back and observed acceptance; user merges)
```

Ordering per brief Files steps 1–6: **baseline before scoring edits**; keyword infra before Themes-dependent scorer wiring; options/scorer authored before consumers; migrate consumers/fixtures before requiring solution build or green tests; legacy absence scan after consumer + evaluation migration; stage after/baseline comparisons before merge clearance. No dependency cycle. Failed/unavailable baseline blocks scoring edits. Honest pending runtime does not waive Done-when / merge clearance.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-518 spec PR for ordinary owner Gate 1; only the user merges. Include ADR 0046 and PLAN_ADJUDICATION.md in the spec PR. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-518 in `specs/DEV-518/CONCLUSIONS.md`. Receipt only. T001–T023 MUST NOT proceed without verified merge (`brief.md`; Q8–Q9; no structural owner checkbox on this ticket).

---

## Phase 1: Pickup recon (read-only; blocks T002)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; inventory authorized paths; Gauge baseline. No production edits and no plan/task amendments in T001.
**Checkpoint**: Fresh recon recorded; Keel read-only reanalysis complete; if demonstrated drift exists, T001a must complete before T002; unresolved drift/decision stops — no invention.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying authorized production files from `plan.md` Source Code, GenreWeightsCache/Calculator/DI invalidation call sites (including all production commit paths affecting library N, counted-keyword eligibility or keyword membership, including paths bypassing EfMovieRepository and paths without genre invalidation), SimilarityScorer call sites in `GetRecommendationsQueryHandler.cs` / `MmrSelector.cs` / `SeedCollectionGrouper.cs` and the For You handler boundary approved in CONCLUSIONS.md, existing scorer/options/evaluation suites (including whether `SimilarityCriteriaTableTests.cs` exists), catalog/repository/cache Testcontainers homes, consumer test homes for Related/MMR/collection + one-snapshot-per-operation proof, and DEV-545 compare/snapshot entry points needed for six-criteria output and baseline capture. **Keel** performs a **strictly read-only** reanalysis of brief/spec/plan/tasks against landed main and records any demonstrated drift — T001 itself MUST NOT amend artifacts. **Gauge** records pre-edit gate baseline. **STOP** `needs recon` on missing seams required for T002+; **STOP** for cited Patron ruling rather than expanding file/protected surface (`brief.md` Q6–Q7). Route any required out-of-bound caller edit through Patron per the brief. **Strictly read-only**.
- [ ] T001a **Keel** (only if T001 demonstrated authorized drift): Amend `tasks.md` only for demonstrated drift within the approved scope and cite the T001 pickup evidence in the amendment. Skip T001a if no drift is demonstrated. Stop and report to Bernstein if addressing the drift would require changing another artifact or the approved scope (`brief.md`, Phase 2 step 3 decision 2).

---

## Phase 2: User Story 1 - DEV-545 baseline before scoring changes (Priority: P1)

**Goal**: Record five Related seed IDs (DEV-545 ordered titles including A Fistful of Dollars) and capture Related + For You baselines before scoring edits.
**Independent Test**: `EVIDENCE.md` baseline slots filled (or explicitly Could not run with stop) with provenance before T003+.
**Order**: T002 after T001 (and T001a when required). **Build lane** owns otherwise-unassigned measurement work.

- [X] T002 [US1] **Build lane**. After T001 (and T001a when required): Resolve the five DEV-545 ordered titles (A Fistful of Dollars, About Time, A Few Good Men, Aladdin, The Wind Rises) to positive home-library integer IDs without inventing placeholders; capture Related baseline for those five seeds and For You baseline for the Genres-default comparison into `specs/DEV-518/EVIDENCE.md` using DEV-545 snapshot tooling; record SHA/provenance; keep artifacts unversioned/scrubbed. Preserve before-scoring capture — do **not** defer baseline until after scoring changes or invent measurements (FR-013; SC-006; `brief.md` step 1; Q5). If access is unavailable, mark Could not run explicitly and **STOP** — there is no unruled pending-baseline bypass; missing filled baseline blocks T003+.

**Checkpoint**: T003+ unlocks only when baseline slots are filled with provenance. Could not run / unavailable access keeps T003+ blocked (no Patron/Conductor pending-baseline bypass in this plan).

---

## Phase 3: User Story 2 - Keyword-count infrastructure (Priority: P1)

**Goal**: Feature fields for evidence; catalog keyword counts; GenreWeights-style cache/calculator/invalidation/DI; lifecycle tests with real persistence; consumer one-snapshot proof lives in US4 (separate).
**Independent Test**: Testcontainers counts + invalidation suites green; mapper exposes names/embedding fields.
**Order**: T003 → T007.

### Tests for User Story 2 (lifecycle only — separate from consumer wiring proof)

- [X] T003 [P] [US2] **Build lane**. After T002: Add failing Postgres Testcontainers coverage proving `IMovieCatalog` keyword-count read returns library `N` and distinct `n_k` from the same movies/movie_keywords population with **real persistence**. Cover an empty library, distinct counts, and a concurrent commit during the read proving one coherent result (0 ≤ n_k ≤ N, no mixed versions). Inspect the representative grouping query plan and existing index support against PostgreSQL. Record concrete cost concerns for scope disposition (FR-008; SC-004; US2 scenario 1; `brief.md` Q7). Exact test home under existing Infrastructure/catalog test areas from T001 (authorized test surface — not blocked by production source list).
- [X] T004 [P] [US2] **Build lane**. After T002: Add failing **lifecycle** coverage (separate from consumer proof in T014): singleton keyword cache + scoped calculator; success-only invalidation after successful `EfMovieRepository` commits with **real persistence repository commit outcomes**, including **successful keyword-membership mutations that refresh counts** (`brief.md` line 43) even when T001 finds those mutations do not already invalidate genre weights; cover each applicable T001 commit site; failed writes preserve prior snapshot; version-token rejects stale in-flight repopulation; cancellation aborts a stale-version retry without publishing; a failed load is propagated and the next operation retries (FR-008; SC-004; US2 scenario 2; GenreWeightsCache.cs:7-64 precedent). Do **not** fold the one-snapshot-per-recommendation-operation consumer wiring proof into this task.

### Implementation for User Story 2

- [X] T005 [US2] **Build lane**. After T003: Extend `src/LamuFlix.Core/Features/Recommendations/MovieFeatures.cs` and `MovieFeaturesMapper.cs` with keyword names, collection name, and optional plot embedding required for scorer evidence (FR-009; `brief.md` step 2). Keep writers/year/runtime/rating fields out of the new scoring path even if still present on the type until scorer migration removes their use.
- [X] T006 [US2] **Build lane**. After T003–T004 authored: Add keyword-count read on `src/LamuFlix.Core/Ports/IMovieCatalog.cs` and implement in `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` following the existing catalog boundary. Use one statement or a repeatable-read-or-stronger transaction for N and distinct n_k. Inspect the existing-index query plan with representative data at T003/T007. Add a single-catalog, single-process keyword cache and scoped calculator mirroring GenreWeights. Keep failed loads uncached and honor cancellation during stale-version retries; register singleton cache and scoped calculator in `PersistenceServiceCollectionExtensions.cs` / `MovieCatalogServiceCollectionExtensions.cs`; invalidate from `EfMovieRepository.cs` after successful commits at each applicable T001 site, including existing genre invalidation sites and successful keyword-membership mutations that refresh counts even without genre invalidation (FR-008; Q7; `brief.md` line 43). No schema change; no new project/layer/package. If a required caller edit falls outside the authorized production set, STOP for cited Patron ruling (T001).
- [X] T007 [US2] **Build lane**. After T005–T006: Make T003–T004 green without weakening assertions.

**Checkpoint**: US2 independently testable — catalog counts + cache lifecycle + feature mapping ready for Themes. Consumer one-snapshot proof remains US4. Phase 3 checkpoint met: focused cache/mapper unit tests (4) and keyword-count integration tests (7) pass; representative PostgreSQL plan and cost disposition are recorded in `EVIDENCE.md` §7.

---

## Phase 4: User Story 3 - Ordered scorer and options (Priority: P1) 🎯 MVP

**Goal**: SimilarityWeights/RecommendationOptions updates; ordered SimilarityScorer/SimilarityResult; table and edge coverage bound to the normative scoring contract in `spec.md` (authoritative copy of `brief.md` lines 9–24).
**Independent Test**: Table pin, contribution reconciliation, Themes/Plot/threshold edges — proven green only after T012–T014 restore a buildable solution. Legacy absence scan is deferred to T018 after consumer + evaluation migration.
**Order**: T008 → T011 (author first). T010 removes `SimilarStoryThreshold` before T012 updates the Related caller; that intermediate window is expected and **acyclic** — do **not** require solution build or green tests until consumers and fixtures migrate in T012–T014.

### Tests for User Story 3

- [X] T008 [P] [US3] **Build lane**. After T007: Create or update `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityCriteriaTableTests.cs` pinning the **six normative table rows** (`spec.md` Normative scoring contract / `brief.md` lines 9–24): order Collection→Director→Themes→Plot→Cast→Genre; defaults 3/3/3/2/2/2; Collection/Director/Cast/Genre **value rules**; exact reason texts (`Same collection: {name}`, `Same director: {names}`, `Shared themes: {top 3 by rarity}`, `Similar story`, `Shares cast: {names}`, `Same genres: {names}`); denominator 15; contribution sum = Score; overridden-weight denominator reconciliation (FR-001–FR-003; SC-001; `brief.md` Test strategy).
- [ ] T009 [P] [US3] **Build lane**. After T007: Extend `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs` (and related threshold/property/golden suites as applicable) for the normative Themes formula/defaults and graded thresholds: missing keywords → Themes 0 with denominator 15; genre-only ≤ 2/15; MinimumKeywordMovies default 2; default stoplist and culture-independent OrdinalIgnoreCase matching; trimmed/deduplicated entries with null or blank entries discarded and explicit null meaning empty; `ln(N/n_k)` rarity weighting; evidence up to three shared counted names by descending rarity; empty/zero-norm Themes including an empty catalog and ubiquitous-only shared counted evidence with zero contribution and no reason; ThemesCeiling default 0.35 (ceiling and half-ceiling values); Plot floor 0.16 / ceiling 0.45; Themes/Plot/Genre display thresholds default 0.3 / 0.5 / 0.5; reason ordering; zero threshold; equality; below-threshold graded contribution with evidence; director/cast/keyword/collection/genre evidence names without requiring reason text when below threshold (FR-004–FR-007; SC-001–SC-003; Q3–Q4; `brief.md` lines 9–24).
  - **T001a pickup amendment for T009.** Migrate the DEV-545 breakdown assertions in `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityResultBreakdownTests.cs` and `SimilarityScorerTests.cs` to the approved six ordered entries. Replace legacy criterion and omission expectations with six entries, including zero-value entries for missing data. Replace identity-fallback expectations with the six-criterion result for valid weights and rejection for an all-zero configuration. Retain contribution-to-score reconciliation. Source evidence is `recon-DEV-518` lines 7–14, commit `9bc3de5c`. The landed breakdown adds diagnostic rows without changing scores or reasons, so T002 still captures the baseline before DEV-518 scoring edits.

### Implementation for User Story 3

- [ ] T010 [US3] **Build lane**. After T008–T009 authored: Update `src/LamuFlix.Core/Options/SimilarityWeights.cs` and `RecommendationOptions.cs` — Themes replaces Keyword (default 3), Plot (2), Genres default 2, Collection default 3, Director 3, Cast 2; remove Writers/YearProximity/RuntimeProximity/RatingBoost; preserve ReviewPlotWeight; remove SimilarStoryThreshold; add stoplist (default four entries), MinimumKeywordMovies (default 2), ThemesCeiling (default 0.35), plot floor/ceiling (0.16/0.45), Themes/Plot/Genre display thresholds (0.3/0.5/0.5) with typed startup validation. Check the finite positive sum of the six non-negative scoring weights, finite positive ThemesCeiling, finite Plot bounds in [-1,1] with floor < ceiling, finite display thresholds in [0,1], and MinimumKeywordMovies ≥ 1. Pin rejected all-zero and non-finite weights, nonpositive ThemesCeiling, equal or reversed Plot bounds, out-of-range/non-finite settings, invalid minimum count, and stoplist normalization/defaults (FR-010; `brief.md` step 3; normative contract). Update `SimilarityWeightsTests.cs`, `SimilarityWeightsSurvivorTests.cs`, `RecommendationOptionsTests.cs` accordingly. Author options here even though Related still references `SimilarStoryThreshold` until T012 — do **not** require solution build or green tests at this task.
- [ ] T011 [US3] **Build lane**. After T010: Implement ordered six-criterion scoring and evidence in `src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs` and `SimilarityResult.cs` per the normative table rows (Collection/Director/Themes/Plot/Cast/Genre value rules, exact reason texts, Themes formula/defaults, graded thresholds); generate reasons only from evidence under Q3 rules; remove year/runtime/rating/writers and Identical feature profile behaviour; accept the immutable keyword snapshot explicitly as an argument for Themes. The scorer stays pure and rejects invalid scoring inputs, including all-zero weights for explicit callers (FR-001–FR-007). Author the scorer here; do **not** require T008–T009 green or solution-wide build yet — that bar lands after T012–T014 migrate consumers and fixtures. **Do not** run the full legacy absence scan here — that is T018 after consumer + evaluation migration.

**Checkpoint**: US3 authored — ordered scorer + options complete against the normative contract. Solution build / green tests are deferred until Phase 5 migrates callers and fixtures.

---

## Phase 5: User Story 4 - Shared consumers (Priority: P1)

**Goal**: Wire Related, MMR, and SeedCollectionGrouper; remove Related separate Similar story threshold path (caller side after T010 options removal); update fixtures; prove consumer one-snapshot-per-operation reuse separately from US2 lifecycle tests; restore buildable solution + green tests.
**Independent Test**: Solution builds; T008–T009 and consumer coverage green; shared Score/ordered reasons where displayed; one snapshot obtained per recommendation operation and reused across pairs (Related and For You MMR/collection flows where used).
**Order**: T012 → T014. Migrate consumers and fixtures **before** requiring solution build or green tests deferred from Phase 4.

- [ ] T012 [P] [US4] **Build lane**. After T011: Update `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs` to consume ordered scorer reasons/evidence; remove separate SimilarStoryThreshold / Similar story reason path left dangling by T010 options removal; keep taste blending formula unchanged except Genres default 2 via shared options; resolve one immutable keyword snapshot at the Related handler boundary and pass it explicitly to each direct pair score and to SeedCollectionGrouper for grouping pairs (FR-011; Q1; Q7; US4 scenario 1). Consumer test home from T001. If a required caller edit is out of bounds, STOP for Patron.
- [ ] T013 [P] [US4] **Build lane**. After T011: Ensure `src/LamuFlix.Core/Features/Recommendations/MmrSelector.cs` and `SeedCollectionGrouper.cs` call the shared ordered `SimilarityScorer.Score` under updated weights; extend the existing `GetForYouRecommendationsQueryHandler.cs` boundary solely to resolve one immutable keyword snapshot and pass it explicitly to MmrSelector. Keep profile relevance as specified. Each helper passes the snapshot supplied by its caller through to every Score call. See Patron's Q6/Q7 ruling in CONCLUSIONS.md (FR-011; Q1; Q7; US4 scenarios 2–3). Consumer test homes from T001. If a required caller edit is out of bounds, STOP for Patron.
- [ ] T014 [US4] **Build lane**. After T012–T013: Update **consumer / handler fixtures and tests only** (paths from T001; disjoint from T017 evaluation fixture files) so they supply keyword names, collection name, optional plot embedding, and new weights; prove that Related and For You each resolve one snapshot and pass the same instance into every pair, including collection and MMR calls. Invalidate between pairs, assert unchanged identity and rarity weights across that operation, and assert the next operation refreshes. Exercise MMR selection and collection ordering under the shared score and existing tuning — separate from T003–T004 lifecycle suites; restore a **buildable** solution and make T008–T009 plus consumer regressions **green** without weakening assertions (SC-005; `brief.md` step 4). Do **not** edit `EvaluationFixtureLoader*.cs` / `TmdbSignalReplayTests.cs` here (those are T017).

**Checkpoint**: US4 independently testable — three consumers share the ordered scorer; consumer snapshot reuse proven separately from lifecycle; solution buildable with Phase 4 table/edge and consumer suites green.

---

## Phase 6: User Story 5 - Docs, compare mode, remaining suites (Priority: P2)

**Goal**: docs table, CONTEXT vocabulary, DEV-545 compare six-criteria output, evaluation suite alignment, then legacy absence scan.
**Independent Test**: Docs match pinned table; six-criteria/contributions proof after both T016 and T017; legacy symbols absent after migration.
**Order**: T015 → T016 → T017 → six-criteria output proof → T018 (serialize T016 then T017; assign deferred/overlapping compare changes to T017; T015 may run parallel to the T016→T017 sequence).

- [ ] T015 [P] [US5] **Build lane**. After T014: Create `docs/recommendations.md` with the pinned criteria table matching `SimilarityCriteriaTableTests` / normative scoring contract; align CONTEXT.md vocabulary only as named in brief (FR-012; `brief.md` step 5). Use ADR 0046 for the durable architecture decision.
- [ ] T016 [US5] **Build lane**. After T014: Apply **non-overlapping** DEV-545 **compare-mode output** edits only (evaluation compare/pair entry points from T001 paths that do not share files with T017) toward exposing all six criteria and contributions. Treat names in output as literal data and use structured logging fields where applicable (FR-012; SC-006). Do **not** edit `EvaluationFixtureLoader.cs`, `EvaluationFixtureLoaderTests.cs`, or `TmdbSignalReplayTests.cs` here. Assign every deferred or overlapping compare change to **T017**. Do **not** claim six-criteria output proof in this task — that proof is after both T016 and T017.
- [ ] T017 [US5] **Build lane**. After T016: Update `tests/LamuFlix.Evaluation/Fixtures/EvaluationFixtureLoader.cs`, `EvaluationFixtureLoaderTests.cs`, and `tests/LamuFlix.Evaluation/Replay/TmdbSignalReplayTests.cs` as applicable for new weights/evidence, **and** complete every deferred/overlapping compare-mode change assigned from T016 using literal metadata and structured logging where applicable (FR-012; `brief.md` step 5; SC-006). Serialized after T016 so compare-output and fixture/replay edits do not overlap the same files concurrently. After T016 and T017 both complete, record six-criteria/contributions **output proof** in `EVIDENCE.md` (compare prints all six criteria and contributions).
- [ ] T018 [US5] **Build lane**. After T015–T017 **and** six-criteria output proof: Re-run focused recommendation/options/evaluation suites; fix only in-scope failures without weakening assertions; then run the **legacy absence scan** over recommendation `src/`/`tests/` for `YearProximity`, `RuntimeProximity`, `RatingBoost`, `Writers`, `SimilarStoryThreshold`, `Similar release years`, `Similar runtime`, `Similar rating`, `Shared writers`, and `Identical feature profile`. Check `SimilarityWeights.Keyword`, weight member accesses such as `Weights.Keyword`, `Keyword` assignments in SimilarityWeights initializers and weight fixtures, and `Keyword` keys under `Recommendations:Weights`. Live fields and identifiers such as `MovieFeatures.Keywords`, `MovieKeyword`, keyword counts and stoplist configuration remain in use (SC-002; FR-007).

**Checkpoint**: Docs aligned; six-criteria compare output proven after T016+T017; evaluation surfaces aligned; legacy absence proven after migration.

---

## Phase 7: Gates, after snapshots, home-library evidence

**Goal**: Executable gate checklist from `plan.md` Constitution Check governance row; stage after/baseline comparisons; Fistful observed acceptance; publishable receipts for merge clearance.
**Order**: T019 (required static/format/architecture/applicable checks) → T020 (stage comparisons) → T021 (observed acceptance). T020 captures after snapshots from the candidate SHA only after T019 completes and all required gates pass on that SHA (`brief.md`, Phase 2 step 3 decision 1). Comparisons and observed acceptance are staged here **before** merge clearance at T023.

- [ ] T019 **Build lane**. After T018: Execute and record in `specs/DEV-518/EVIDENCE.md` §5 the full Phase B checklist already promised by `plan.md` governance row and the constitution — at minimum: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (including applicable refactor-gate threshold), `./scripts/run-jetbrains-inspectcode.ps1`, `dotnet format --verify-no-changes`, architecture tests, applicable property tests, applicable vulnerable-packages / security checks, applicable `dotnet test`, and mutation when production C# under `src/` changes; web SKIPPED (scope-empty) if `/web` untouched (FR-014; SC-007; `brief.md` step 6). All required gates, including required static gates and format, MUST pass before T020. Record applicable non-blocking dispositions separately. Use honest labels only (PASS / FAIL / SKIPPED / SKIP / N/A / Could not run); never fold N/A or non-pass into PASS; never lower thresholds. Record SHA.
- [ ] T020 [US5] **Build lane**. After T019 completes with all required gates passing: From the T019 candidate SHA, capture after Related snapshots for the same five baseline IDs and a For You after snapshot; **stage** the comparison to T002 baseline and prepare publishable receipts. Record MMR selection and collection ordering changes visible in these captures, indicating any behavior the capture cannot show and citing T014 coverage. Keep existing tuning (FR-013; SC-006; Q5). Mark pending/Could not run honestly if home access is unavailable — honest pending allows T022 review preparation and T023 PR opening but does **not** clear Done-when / merge clearance.
- [ ] T021 [US5] **Build lane**. After T020: On home library, verify Related for A Fistful of Dollars lists The Good, the Bad and the Ugly with Same director and Shared themes; record SMOKE-001 / Done-when receipt in `EVIDENCE.md` (FR-013; SC-006). Runtime remains pending until observed; **observed acceptance** plus published attachments verified by T023 read-back are required for Done-when and merge clearance (not for merely opening the delivery PR).

---

## Phase 8: Capped three-axis review

- [ ] T022 **Review lane**. After T019 (required gates passed and recorded) and after T020–T021 slots are either filled or honestly pending: Obtain Risk, Standards, and Spec axis reports on the reviewed SHA; **Q8 conjunctive** — resolve **every** actionable in-scope Critical, High, Medium, and Low finding **and** cite disposition for each disputed finding (Q6 frozen scope; Q8); keep the **two-round cap** under Q6/Q8/Q9; unresolved blockers return to Bernstein with evidence — cap confers no waiver. Honest pending runtime does not block starting this review preparation, but unresolved required gate failures do.

---

## Phase 9: Delivery handoff

- [ ] T023 **Bernstein/Rigger**. After T022 closing bar met: Prepare and may **open** the delivery PR with merge-bar proof **without** requiring attachments on the unopened PR. After publishing the DEV-545 and home-library receipts, read back the delivery PR and open each published receipt link. Verify accessibility and confirm the contents match the staged T020–T021 evidence. Record the verified URLs and read-back outcome in `specs/DEV-518/EVIDENCE.md` (`brief.md`, Phase 2 step 3 decision 3). **Merge clearance** and Done-when require successful read-back and observed acceptance from T020–T021 (or an explicit owner disposition outside this plan). A missing receipt, failed read-back, or pending acceptance leaves both pending. Only the user merges. Do not claim runtime PASS from gates alone.

---

## Dependencies

- T000 blocks all Build work.
- T001 blocks T002; T001a (when required by demonstrated drift) blocks T002; T002 filled baseline blocks T003+.
- US2 lifecycle (T003–T007) blocks US3 scorer Themes integration (T011).
- US3 (T008–T011) authors options/scorer first and blocks US4 (T012–T014); solution build/green required only after T014 migrates consumers/fixtures.
- US4 blocks US5 docs/compare (T015–T018) and after evidence (T020–T021).
- T016 before T017 (serialize; deferred/overlapping compare assigned to T017); six-criteria output proof after both; T015 ∥ (T016→T017) after T014; T018 after that proof.
- Legacy absence scan is part of T018 after consumer + evaluation migration and six-criteria proof.
- T019 completion with required gates passing blocks T020 until the candidate SHA is gate-clean. T020 precedes T021. Unavailable home evidence does not waive T019. T023 may open the delivery PR while attachments are pending. Verified attachment read-back and observed acceptance are required for Done-when and merge clearance.

## Parallel opportunities

- T003 ∥ T004 after T002
- T008 ∥ T009 after T007
- T012 ∥ T013 after T011
- T015 ∥ (serialized T016 → T017) after T014

## MVP

User Stories 1–4 (baseline + keyword infra + ordered scorer + three consumers) deliver the ticket’s core scoring contract. US5 docs/compare/gates/runtime close Done when.

## Implementation strategy

1. Gate 1 merge (T000) → read-only pickup recon (T001) → optional Keel `tasks.md` drift amendment (T001a) → baseline (T002)
2. Keyword infra + features → options + scorer → consumers (+ separate one-snapshot consumer proof)
3. Docs/compare (deferred compare in T017; six-criteria proof after both) → legacy absence scan → required gates pass → staged comparisons + observed acceptance → capped review → delivery PR may open without attachments (merge clearance needs verified attachment read-back and observed acceptance)
