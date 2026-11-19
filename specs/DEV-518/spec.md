# Feature Specification: Ordered six-criterion similarity scorer

**Feature Branch**: `feature/518-spec`
**Created**: 2026-11-19
**Status**: gate1: provisional
**Input**: Ticket DEV-518 (parent DEV-490; size L); grill-closed `brief.md` (Q1–Q9); `CONCLUSIONS.md`; Maestri notes `DEV-518`, `recon-DEV-518` (lines 7–28, 30–69); `specs/PRODUCT.md`. No taste `ASSUMPTIONS.md` required (`CONCLUSIONS.md` Q9).

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Q1–Q9; `CONCLUSIONS.md`; `PRODUCT.md` §5). Ordinary owner Gate 1 remains pending until the user merges the Spec Kit PR. Quill does not invent PLAN FROZEN or a structural checkbox. Runtime DEV-545 before/after snapshots, home-library Related acceptance, and gate receipts remain Phase B evidence.

## Clarifications

### Session 2026-11-19

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen scope (shared six-criterion scorer, ordered evidence, keyword-count cache, options, three consumers, named tests/docs, DEV-545 and home-library evidence), callers (Q1), denominator (Q2), reason boundaries (Q3), zero-norm Themes (Q4), snapshot sequence (Q5), review scope (Q6), keyword-count lifecycle (Q7), closing bar (Q8), and two-round cap (Q9) are frozen in `brief.md` / `CONCLUSIONS.md`. The five Related seed titles reuse the DEV-545 ordered set (A Fistful of Dollars, About Time, A Few Good Men, Aladdin, The Wind Rises); literal home-library integers and measured receipts are Phase B evidence — not inventable here. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

## What the User Will See *(mandatory)*

- **Where**: Movie detail Related rail (home library / Compose stack with recommendations enabled)
- **What the user sees or does**: Related cards for a selected movie show ordered similarity reasons drawn only from Collection, Director, Themes, Plot, Cast, and Genre matches (for example “Same director: …” and “Shared themes: …”). Year, runtime, rating, writers, and “Identical feature profile” no longer appear as similarity reasons. A below-threshold Themes/Plot/Genre match can still influence ranking without showing that reason.
- **What it needs to show up**: Default Compose stack with an enriched library that includes shared directors/keywords/collections where expected; home-library case uses A Fistful of Dollars → The Good, the Bad and the Ugly.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** the home library with A Fistful of Dollars enriched and The Good, the Bad and the Ugly present, **When** the user opens Related for A Fistful of Dollars, **Then** The Good, the Bad and the Ugly appears with “Same director” and “Shared themes” reasons and without year/runtime/rating/writers/identical-profile reasons.

## Scope

**In scope** (`brief.md` Scoring contract, Grill rulings Q1–Q9, Files and implementation order, Test strategy; ticket Wanted/Done when at `recon-DEV-518` lines 32–59):

### Normative scoring contract (authoritative — `brief.md` Scoring contract lines 9–24)

Use one ordered `SimilarityScorer.Score` for the Related rail, For You MMR redundancy, and collection block ranking.

| Order | Criterion | Default weight | Value | Reason |
| --- | --- | --- | --- | --- |
| 1 | Collection | 3 | 1 for matching TMDb collection IDs, otherwise 0 | Same collection: {name} |
| 2 | Director | 3 | 1 for at least one shared director, otherwise 0 | Same director: {names} |
| 3 | Themes | 3 | Rarity-weighted keyword cosine divided by ThemesCeiling, capped at 1 | Shared themes: {top 3 by rarity} |
| 4 | Plot | 2 | Plot-embedding cosine rescaled between the configured floor and ceiling, clamped to [0, 1] | Similar story |
| 5 | Cast | 2 | min(1, shared actors / 2) | Shares cast: {names} |
| 6 | Genre | 2 | Existing rarity-weighted overlap | Same genres: {names} |

Every result carries six ordered entries with criterion value, weight, matched evidence, and contribution. Contribution is weight × value divided by the sum of the six configured weights. Score is the sum of the six contributions. The denominator is 15 at defaults, including when data is missing. Missing criteria contribute zero. At defaults, genre-only similarity is at most 2/15.

For Themes, `N` is the count of movies in the library population and `n_k` is the distinct movie count for keyword `k` in that population. Keep keywords with `n_k` at least MinimumKeywordMovies (default 2) and exclude stoplisted names case-insensitively. Default stoplist entries are `aftercreditsstinger`, `duringcreditsstinger`, `sequel`, and `3d animation`. Each retained keyword has weight `ln(N / n_k)`. Compute cosine with sum of squared weights for shared keywords as the numerator and the product of the two vector norms as the denominator. An empty or zero-norm vector yields raw cosine zero. Themes value is `min(1, raw cosine / ThemesCeiling)`, with a default ceiling of 0.35. Evidence is up to three shared counted keyword names in descending rarity weight.

Plot value is `clamp((cosine − floor) / (ceiling − floor), 0, 1)`. The floor defaults to 0.16 and the ceiling to 0.45. Plot carries its value without matched names. Collection, Director, Cast, and Genre carry their matched names.

Generate reasons in table order. Collection, Director, and Cast require positive value. Themes, Plot, and Genre require positive value and a value at or above their thresholds, which default to 0.3, 0.5, and 0.5 respectively. A positive value below threshold retains its evidence and score contribution. A non-match produces no reason even at a zero threshold.

Remove release year, runtime, IMDb rating, writers, and “Identical feature profile” from similarity scoring and reasons. Remove `SimilarStoryThreshold` and use the Plot reason from the shared scorer.

1. **Shared ordered scorer**: One `SimilarityScorer.Score` used by Related (`GetRecommendationsQueryHandler`), For You MMR redundancy (`MmrSelector`), and collection block ranking (`SeedCollectionGrouper`), bound to the normative table above. Fixed six criteria in table order with default weights summing to 15. Score is sum of contributions; each contribution is `weight × value / (sum of six configured weights)`. Missing data yields value 0 and stays in the denominator.
2. **Ordered evidence result**: Every result carries six ordered criterion entries (value, weight, matched evidence, contribution). Reasons are built only from evidence in table order under the normative reason rules and Q3 thresholds. Remove year/runtime/IMDb rating/writers and “Identical feature profile” from scoring and reasons. Remove `SimilarStoryThreshold`; Plot reason comes from the shared scorer.
3. **Themes and Plot formulas**: Exact Themes rarity-weighted keyword cosine with stoplist, `MinimumKeywordMovies` (default 2), default stoplist, `ThemesCeiling` (default 0.35), and Q4 zero-norm behaviour; Plot rescale with floor 0.16 / ceiling 0.45 — all as stated in the normative scoring contract above.
4. **Keyword-count infrastructure**: `IMovieCatalog` / `EfMovieCatalog` read for library `N` and distinct `n_k`; singleton keyword-weights cache + scoped calculator mirroring `GenreWeightsCache` / `GenreWeightsCalculator`; invalidate after successful `EfMovieRepository` commits (including successful keyword-membership mutations that refresh counts, even when a pickup path does not already invalidate genre weights); version token; one snapshot per recommendation operation reused across pairs within that operation (Q7; `brief.md` line 43). No schema change.
5. **Feature mapping**: Extend `MovieFeatures` / `MovieFeaturesMapper` with keyword names, collection name, and optional plot embedding as needed by the scorer evidence.
6. **Options**: Update `SimilarityWeights` / `RecommendationOptions` under `Recommendations:Weights` — Themes replaces Keyword (default 3), Plot (2), Genres default 2, Collection default 3, Director 3, Cast 2; remove Writers / YearProximity / RuntimeProximity / RatingBoost; preserve ReviewPlotWeight; add stoplist, MinimumKeywordMovies, ThemesCeiling, plot floor/ceiling, and Themes/Plot/Genre display thresholds (defaults 0.3 / 0.5 / 0.5).
7. **Consumers**: Wire the three Q1 callers; keep For You profile relevance and Related taste blending formulas unchanged except Genres default 2; remove Related’s separate Similar story threshold/reason path; each recommendation operation obtains one keyword-count snapshot and reuses it across pairs (including For You MMR / collection flows where the scorer is used).
8. **Tests, docs, DEV-545 evidence**: Named unit/evaluation surfaces; table-pinning test bound to the normative table; `docs/recommendations.md` aligned with pinned table; CONTEXT.md vocabulary alignment; DEV-545 compare mode shows six criteria and contributions; five-movie Related before/after (DEV-545 titles) plus For You before/after for Genres default change; home-library Fistful acceptance; three static-analysis gates, format, architecture tests, applicable property/security/refactor checks, tests, mutation, and runtime receipts.

**Out of scope** (`brief.md` Scope exclusions; Q6):

- For You profile relevance ranking and seed scoring (DEV-543)
- Related taste blending formula changes beyond Genres default (DEV-543)
- Candidate pool widening beyond plot top 50 (DEV-544)
- Card layout and reason display UI (DEV-542)
- Diversity caps, paging, exclusions (DEV-541)
- Plot text and embedding model changes (DEV-540)
- TMDb crowd similarity (DEV-539)
- HTTP route, DTO, or OpenAPI / TypeScript contract changes
- New NuGet/npm dependency, project, top-level folder, architectural layer, database schema, `Features:LocalPlay`, secrets, or `Process.Start`
- Inventing home-library integer seed IDs or measured snapshot contents
- Claiming PLAN FROZEN, Gate 1 merge, implementation PASS, or runtime acceptance from this draft

**Precedence**: Ticket Scope & Technical Design and acceptance criteria (`recon-DEV-518`) are already approved authority (`PRODUCT.md` §3). Brief Q1–Q9 and `CONCLUSIONS.md` govern plan boundaries. Phase 2 step 3 clean analyze precedes the Size L ADR and plan challenge; clean analyze alone does not freeze the plan. ADR authoring remains Keel step 5 with a Conductor-assigned number (pending here). Only Keel declares PLAN FROZEN after the full step-3+ challenge path completes. Q6 frozen review scope and Q8 **conjunctive** closure — every actionable in-scope Critical/High/Medium/Low finding resolved **and** cited dispositions for disputed findings, with the two-round cap — remain governing.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion beyond vocabulary alignment named in brief)*

**Ordered criterion**
One of Collection, Director, Themes, Plot, Cast, Genre in fixed table order with configured weight, value in [0, 1], matched evidence, and contribution.
_Avoid_: legacy year/runtime/rating/writers terms; unordered evidence

**Contribution**
`weight × value / denominator` where denominator is the sum of all six configured weights (15 at defaults). Six contributions always sum to Score.
_Avoid_: normalizing only over present criteria

**Themes**
Rarity-weighted keyword cosine over counted library keywords, divided by ThemesCeiling and capped at 1, per the normative scoring contract. Stoplisted names and below-minimum-frequency keywords are excluded.
_Avoid_: treating Keyword weight as a separate live term; showing stoplisted names in reasons

**Display threshold**
Minimum value required to emit a Themes, Plot, or Genre reason, in addition to `value > 0` (defaults 0.3 / 0.5 / 0.5). Collection, Director, and Cast require only `value > 0`.
_Avoid_: suppressing score contribution for below-threshold positive values

**Keyword-count snapshot**
Library movie total `N` and distinct movie counts `n_k` from one library population, cached with GenreWeights-style invalidation and version-token protection; one snapshot resolved per recommendation operation and reused across pairs.
_Avoid_: schema change; TTL-based lifetime; inventing a new architectural layer

Consumed vocabulary (already in use; not redefined): SimilarityScorer, SimilarityResult, MovieFeatures, Related rail, MMR, SeedCollectionGrouper, DEV-545 compare/snapshot.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capture DEV-545 baseline before scoring changes (Priority: P1)

Operators fix the five Related seed titles/IDs (DEV-545 ordered set including A Fistful of Dollars) and capture the Related and For You baseline snapshots before any scoring code lands.

**Why this priority**: Q5 / brief step 1 — without a before snapshot, after comparison and Genres-default For You evidence cannot close.

**Independent Test**: Baseline artifacts exist for the five Related seeds and For You under recorded SHAs before scorer edits; IDs reused for the after run.

**Acceptance Scenarios**:

1. **Given** the DEV-545 five-title ordered set and a running home instance with looked-up positive integer IDs, **When** the before capture runs, **Then** Related lists for those five seeds and a For You snapshot are recorded as baseline with provenance and no invented scores.
2. **Given** that baseline, **When** scoring implementation later completes, **Then** the same five IDs are used for the after Related run and For You after snapshot.

---

### User Story 2 - Library keyword-count snapshot for Themes (Priority: P1)

The library exposes one population read of `N` and distinct keyword movie counts, cached like genre weights, so Themes rarity weights stay consistent for a recommendation operation and refresh after successful movie commits, including successful keyword-membership mutations.

**Why this priority**: Themes formula depends on library rarity; Q7 freezes the lifecycle before scorer consumers can use it safely.

**Independent Test**: Catalog query returns distinct counts from the shared population with real persistence; scoped calculator + singleton cache resolve one snapshot per operation; success-only invalidation (including keyword-membership mutations) and stale in-flight load rejection are covered. Consumer wiring proof that one snapshot is obtained and reused across pairs is a separate scenario from the cache lifecycle suite.

**Acceptance Scenarios**:

1. **Given** movies and movie_keywords in the library population (real persistence), **When** the catalog keyword-count read runs, **Then** it returns `N` and distinct `n_k` from that same population with no schema migration.
2. **Given** a successful movie repository commit — including a successful keyword-membership mutation that refreshes counts (`brief.md` line 43) — **When** keyword cache invalidation runs (alongside genre-weight invalidation when that site already exists, and also for keyword mutations that pickup finds do not already invalidate genre weights), **Then** the next load refreshes; a failed write leaves the prior snapshot; an in-flight load observing a newer version token does not repopulate the invalidated snapshot.
3. **Given** a recommendation operation (Related and, where used, For You MMR / collection scoring), **When** Themes scoring needs weights across multiple pairs, **Then** the consumer wiring obtains one snapshot for that operation and reuses it across pairs (separate from the cache lifecycle suite).

---

### User Story 3 - Six-criterion ordered score and evidence (Priority: P1) 🎯 MVP

Similarity scoring returns a single ordered six-criterion result with contributions that sum to the score, reasons only for qualifying matches, and no legacy year/runtime/rating/writers/identical-profile terms.

**Why this priority**: Core ticket Wanted design and Done-when contract.

**Independent Test**: Unit table pin against the normative scoring contract, contribution reconciliation, Themes/Plot edges, threshold boundaries, and (after consumer/evaluation migration) legacy-term absence can be proven without the SPA.

**Acceptance Scenarios**:

1. **Given** defaults, **When** two movies are scored, **Then** six ordered criteria appear with weights 3,3,3,2,2,2, denominator 15, contributions summing to Score, Collection/Director/Cast/Genre value rules and exact reason texts from the normative table, and genre-only similarity at most 2/15.
2. **Given** missing keyword data on either side, **When** Themes is evaluated, **Then** Themes value is 0, denominator remains 15, and no Themes reason is emitted.
3. **Given** shared counted keywords including a stoplisted name, **When** Themes evidence and reasons are built, **Then** the stoplisted name is absent; evidence lists up to three shared counted keyword names by descending rarity weight; Themes uses MinimumKeywordMovies default 2, default stoplist, `ln(N/n_k)`, ThemesCeiling default 0.35.
4. **Given** a graded criterion with `0 < value < threshold`, **When** reasons are generated, **Then** the criterion still contributes to Score and retains evidence but emits no reason; `value == threshold` emits the reason; `value == 0` never emits a reason even if threshold is 0; Themes/Plot/Genre thresholds default to 0.3 / 0.5 / 0.5.
5. **Given** either movie’s counted-keyword vector has zero norm, **When** Themes cosine is computed, **Then** raw cosine and Themes value are 0 with no Themes reason (finite Score, never NaN).
6. **Given** Themes raw cosine at ThemesCeiling or half ceiling, **When** value is computed, **Then** value is 1.0 or 0.5 respectively (capped at 1).
7. **Given** search over `src/` and `tests/` recommendation code/options/tests after consumer and evaluation migration, **When** legacy symbols/reason phrases are sought, **Then** YearProximity, RuntimeProximity, RatingBoost, Writers, SimilarStoryThreshold, Keyword-as-a-weight (retired options weight; live keyword features remain), Similar release years, Similar runtime, Similar rating, Shared writers, and Identical feature profile have no hits.

---

### User Story 4 - Shared scorer on Related, MMR, and collection blocks (Priority: P1)

Related ranking, For You MMR redundancy, and collection block ranking all consume the same ordered scorer; Related no longer applies a separate Similar story threshold.

**Why this priority**: Q1 — one function, one weight/reason vocabulary across callers.

**Independent Test**: Consumer tests/fixtures supply feature evidence and weights; Related reasons come from ordered scorer evidence; MMR uses shared Score; collection grouping uses shared Score; For You profile relevance path remains otherwise unchanged; consumer proof shows one keyword-count snapshot per recommendation operation reused across pairs.

**Acceptance Scenarios**:

1. **Given** Related candidates, **When** recommendations are built, **Then** each pair uses the ordered scorer and displayed reasons follow table order and thresholds without a separate `SimilarStoryThreshold` path, and one keyword-count snapshot is obtained for the operation and reused across pairs.
2. **Given** For You MMR selection, **When** redundancy is measured, **Then** it uses `SimilarityScorer.Score(...).Score` under the shared weights (including Genres default 2) and reuses the operation’s keyword-count snapshot where Themes is evaluated across pairs.
3. **Given** collection block ranking, **When** SeedCollectionGrouper scores pairs, **Then** it uses the same ordered scorer contract and the same one-snapshot-per-operation reuse where applicable.

---

### User Story 5 - Docs, DEV-545 compare, gates, and after/home evidence (Priority: P2)

Documentation, compare-mode output, static gates, after snapshots, and the Fistful home-library case close the Done-when bar.

**Why this priority**: Delivery evidence and operator-facing table; depends on US3–US4 behaviour.

**Independent Test**: Pinned docs table matches tests; compare mode prints six criteria/contributions; gate scripts and runtime receipts are recorded or honestly pending. Runtime-pending labels do not clear Done-when or merge clearance.

**Acceptance Scenarios**:

1. **Given** the pinned criteria table (normative scoring contract), **When** `docs/recommendations.md` and the table-pinning test are read, **Then** order, defaults, reason text, and vocabulary match CONTEXT.md alignment named in the brief.
2. **Given** DEV-545 compare mode after implementation, **When** a pair is inspected, **Then** all six criteria and contributions print.
3. **Given** after snapshots for the same five Related IDs and For You, **When** compared to baseline, **Then** comparisons are staged and receipts are publishable before merge clearance; Fistful Related lists The Good, the Bad and the Ugly with Same director and Shared themes when observed on the home library. Attachments are not required merely to open the delivery PR.
4. **Given** changed C# files, **When** Roslyn, cyclomatic complexity, InspectCode, `dotnet format --verify-no-changes`, architecture tests, applicable property/security/refactor checks, tests, and mutation run, **Then** required static gates pass and other receipts are recorded with honest labels (never folding non-pass into PASS). Published attachments and observed acceptance remain required for Done-when / merge clearance separately from review preparation and PR opening that may proceed with honest pending runtime slots.

### Edge Cases

- Empty or zero-norm Themes vectors → value 0, no reason, finite score.
- Ubiquitous counted keywords (`n_k == N`) yield `w_k = 0` and can produce zero norms.
- Case-insensitive stoplist exclusion; names never appear in reasons.
- Positive below-threshold graded values keep evidence and contribution.
- Overridden non-default weights: denominator is sum of configured six weights; contributions still reconcile to Score.
- Failed movie writes must not invalidate keyword cache.
- Successful keyword-membership mutations must refresh counts on next load.
- In-flight keyword load after Invalidate must not restore the stale snapshot.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST score movie pairs with exactly six ordered criteria — Collection, Director, Themes, Plot, Cast, Genre — using the normative scoring contract table (default weights 3, 3, 3, 2, 2, 2) including Collection/Director/Cast/Genre value rules (`brief.md` Scoring contract lines 9–24; Q2).
- **FR-002**: System MUST compute Score as the sum of six contributions `weight × value / Σ(weights)`, with missing criteria valued at 0 and retained in the denominator (`brief.md` Q2; normative contract).
- **FR-003**: System MUST return six ordered evidence entries (value, weight, matched evidence, contribution) on every score result, with Collection/Director/Cast/Genre carrying matched names and Plot value-only (`brief.md` Scoring contract).
- **FR-004**: System MUST emit reasons only from evidence in table order under Q3 match/threshold rules using the exact reason texts in the normative table (Same collection / Same director / Shared themes / Similar story / Shares cast / Same genres); MUST NOT emit reasons for non-matches or stoplisted keywords; Themes/Plot/Genre thresholds default to 0.3 / 0.5 / 0.5; MUST remove Identical feature profile (`brief.md` Q3).
- **FR-005**: System MUST compute Themes via rarity-weighted keyword cosine with MinimumKeywordMovies (default 2), case-insensitive default stoplist (`aftercreditsstinger`, `duringcreditsstinger`, `sequel`, `3d animation`), weight `ln(N / n_k)`, ThemesCeiling (default 0.35), cosine numerator = sum of squared weights for shared keywords, denominator = product of vector norms, and Q4 zero-norm behaviour (`brief.md` Scoring contract; Q4).
- **FR-006**: System MUST compute Plot as clamp((cosine − floor) / (ceiling − floor), 0, 1) with defaults floor 0.16 and ceiling 0.45, value-only evidence, and no `SimilarStoryThreshold` (`brief.md` Scoring contract; Q1).
- **FR-007**: System MUST remove year, runtime, IMDb rating, and writers from similarity score, reasons, and weights (`recon-DEV-518` Done when; `brief.md`).
- **FR-008**: System MUST expose keyword-count reads on the movie catalog, cache them with GenreWeights-style singleton/scoped DI and version-token invalidation after successful repository commits (including successful keyword-membership mutations that refresh counts), and resolve one snapshot per recommendation operation reused across pairs (`brief.md` Q7; line 43).
- **FR-009**: System MUST extend feature mapping with keyword names, collection name, and optional plot embedding required for evidence (`brief.md` Files step 2).
- **FR-010**: System MUST update SimilarityWeights/RecommendationOptions as specified (Themes replaces Keyword; Plot added; Genres default 2; Collection default 3; legacy proximity/writers removed; ReviewPlotWeight preserved; Themes/Plot/threshold settings added with defaults above) (`brief.md` Files step 3; Q1).
- **FR-011**: System MUST wire GetRecommendationsQueryHandler, MmrSelector, and SeedCollectionGrouper to the ordered scorer while leaving For You profile relevance and Related taste blending formulas otherwise unchanged, and MUST prove consumer wiring obtains one keyword-count snapshot per recommendation operation reused across pairs including For You MMR/collection flows where used (`brief.md` Q1; Q7).
- **FR-012**: System MUST pin the criteria table in tests and `docs/recommendations.md`, align CONTEXT.md vocabulary, and expose six criteria/contributions in DEV-545 compare mode (`brief.md` Files steps 5–6; Test strategy).
- **FR-013**: Operators MUST capture DEV-545 Related before/after for the five fixed titles and For You before/after for the Genres default change **before scoring edits for the before run**, stage those comparisons before merge clearance, verify the Fistful home-library case (observed acceptance), and publish receipts before merge clearance; the delivery PR may open without attachments; runtime acceptance stays pending until observed; published attachments and observed acceptance remain required for Done-when and merge clearance (`brief.md` Q5; Q8).
- **FR-014**: Changed C# MUST pass the three CLAUDE.md static-analysis gates plus `dotnet format --verify-no-changes`; architecture tests and applicable property/security/refactor checks, tests, and mutation evidence MUST be recorded with honest non-PASS labels (`brief.md` Q8; Done when; `plan.md` Constitution Check governance row).

### Key Entities

- **CriterionEvidence**: Ordered criterion identity, value, weight, matched names/evidence, contribution.
- **SimilarityOutcome**: Aggregate Score plus six CriterionEvidence entries and derived reasons.
- **KeywordCountSnapshot**: Library `N` and per-keyword distinct movie counts `n_k` for one population.
- **SimilarityWeights / RecommendationOptions**: Configured weights, Themes/Plot/threshold settings, stoplist, ceilings/floors.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At defaults, every scored pair exposes six contributions that sum to Score with denominator 15; genre-only pairs score at most 2/15; Collection/Director/Cast/Genre value rules and exact reason texts match the normative table.
- **SC-002**: After consumer and evaluation migration, recommendation code and tests under `src/` and `tests/` contain no YearProximity, RuntimeProximity, RatingBoost, Writers, SimilarStoryThreshold, Keyword-as-a-weight (retired), or the listed legacy reason phrases; live keyword feature fields remain.
- **SC-003**: Themes/Plot/Genre reason emission matches threshold equality (defaults 0.3 / 0.5 / 0.5) and below-threshold contribution rules in automated tests.
- **SC-004**: Keyword-count cache invalidates only after successful commits (including keyword-membership mutations) and rejects stale in-flight repopulation in automated tests with real persistence; consumer wiring separately proves one snapshot per recommendation operation reused across pairs.
- **SC-005**: Related, MMR, and collection-block consumers share the ordered scorer contract in automated coverage.
- **SC-006**: DEV-545 compare output lists all six criteria and contributions (proof after compare surfaces and evaluation fixture migration both complete); before/after Related (five titles) and For You snapshots are staged and publishable before merge clearance; Fistful home-library case shows Same director and Shared themes when observed. Done-when / merge clearance require published attachments and observed acceptance; an unopened PR does not need attachments; honest pending does not clear that bar.
- **SC-007**: Roslyn, cyclomatic complexity, and InspectCode pass on changed C#; format, architecture tests, applicable property/security/refactor checks, test and mutation receipts are recorded without lowering thresholds or folding non-pass into PASS.

## Assumptions

- No Patron taste assumptions were required; reason strings and table copy come from ticket text (`CONCLUSIONS.md` Q9).
- The five Related seed titles are the DEV-545 ordered set; literal integer IDs remain Phase B lookup/evidence, not drafting inventions.
- Keyword-count design reuses existing architecture and schema (Q7); type names follow the GenreWeights* pattern without a new layer.
- DEV-545 compare/snapshot tooling is available for Phase B evidence; this ticket does not redefine that harness beyond exposing six criteria/contributions and attaching receipts.
- Runtime home-library and snapshot acceptance remain open until observed (`brief.md` Q5); pending runtime does not waive Done-when evidence requirements.
