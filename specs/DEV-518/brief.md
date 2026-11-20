# DEV-518 Phase 2 grill brief

Patron closed the grill with Q1–Q9. The cited rulings are in [CONCLUSIONS.md](CONCLUSIONS.md), commit ad5127ff. Ticket facts come from recon-DEV-518 lines 7–28 and 30–69. **PLAN FROZEN** after the three-axis plan challenge. See [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md) and [ADR 0046](../../docs/adr/0046-shared-recommendation-similarity.md). Gate 1 awaits owner merge of the spec PR. Implementation gates and runtime acceptance are pending.

## Scoring contract

Use one ordered SimilarityScorer.Score for the Related rail, For You MMR redundancy, and collection block ranking.

| Order | Criterion | Default weight | Value | Reason |
| --- | --- | --- | --- | --- |
| 1 | Collection | 3 | 1 for matching TMDb collection IDs, otherwise 0 | Same collection: {name} |
| 2 | Director | 3 | 1 for at least one shared director, otherwise 0 | Same director: {names} |
| 3 | Themes | 3 | Rarity-weighted keyword cosine divided by ThemesCeiling, capped at 1 | Shared themes: {top 3 by rarity} |
| 4 | Plot | 2 | Plot-embedding cosine rescaled between the configured floor and ceiling, clamped to [0, 1] | Similar story |
| 5 | Cast | 2 | min(1, shared actors / 2) | Shares cast: {names} |
| 6 | Genre | 2 | Existing rarity-weighted overlap | Same genres: {names} |

Every result carries six ordered entries with criterion value, weight, matched evidence, and contribution. Contribution is weight × value divided by the sum of the six configured weights. Score is the sum of the six contributions. The denominator is 15 at defaults, including when data is missing. Missing criteria contribute zero. At defaults, genre-only similarity is at most 2/15.

For Themes, N is the count of movies in the library population and n_k is the distinct movie count for keyword k in that population. Keep keywords with n_k at least MinimumKeywordMovies (default 2) and exclude stoplisted names case-insensitively. Default stoplist entries are aftercreditsstinger, duringcreditsstinger, sequel, and 3d animation. Each retained keyword has weight ln(N / n_k). Compute cosine with sum of squared weights for shared keywords as the numerator and the product of the two vector norms as the denominator. An empty or zero-norm vector yields raw cosine zero. Themes value is min(1, raw cosine / ThemesCeiling), with a default ceiling of 0.35. Evidence is up to three shared counted keyword names in descending rarity weight.

Plot value is clamp((cosine − floor) / (ceiling − floor), 0, 1). The floor defaults to 0.16 and the ceiling to 0.45. Plot carries its value without matched names. Collection, Director, Cast, and Genre carry their matched names.

Generate reasons in table order. Collection, Director, and Cast require positive value. Themes, Plot, and Genre require positive value and a value at or above their thresholds, which default to 0.3, 0.5, and 0.5 respectively. A positive value below threshold retains its evidence and score contribution. A non-match produces no reason even at a zero threshold.

Remove release year, runtime, IMDb rating, writers, and “Identical feature profile” from similarity scoring and reasons. Remove SimilarStoryThreshold and use the Plot reason from the shared scorer.

## Grill rulings

1. **Q1 — Shared scorer callers.** Use the new scorer in GetRecommendationsQueryHandler, MmrSelector, and SeedCollectionGrouper. Keep the For You profile relevance and Related taste profile blending formulas. The Genres default changes from 3 to 2 for consumers of the shared options.
2. **Q2 — Denominator.** Sum all six configured weights, with 15 at defaults. A missing criterion still participates in the denominator. Pin the default and an overridden-weight configuration and reconcile contributions to Score.
3. **Q3 — Reason boundaries.** Require positive match and value greater than or equal to the threshold for graded reasons. Pin equality, zero at a zero threshold, and positive below-threshold contribution with evidence.
4. **Q4 — Zero-norm Themes.** Counted ubiquitous keywords can have weight zero. Either zero norm yields raw cosine and Themes value zero, with no Themes reason.
5. **Q5 — Runtime evidence.** Record five fixed movie IDs, including A Fistful of Dollars. Capture the DEV-545 baseline before scoring changes land. Repeat the same five for Related afterwards and collect the For You before/after snapshot for the Genres default change. Attach the comparison receipts to the PR. Keep runtime acceptance pending until observed.
6. **Q6 — Frozen review scope.** Cover the scorer and evidence, feature mapping, keyword-count infrastructure, options, three consumers, related unit and evaluation tests, documentation, and the DEV-545 and home-library evidence. Apply the exclusions below.
7. **Q7 — Keyword-count lifecycle.** Add a scoped IMovieCatalog/EfMovieCatalog read for N and distinct counts from one library population. Use a singleton cache with a scoped calculator and resolve one snapshot per recommendation operation. Invalidate after successful commits affecting the counted population, including keyword-membership mutations and the existing GenreWeightsCache invalidation sites. Use a version token to prevent an in-flight read from repopulating an invalidated snapshot. Lifetime is driven by invalidation. Follow the existing GenreWeightsCache and GenreWeightsCalculator pattern.
8. **Q8 — Closing bar.** Require Risk, Standards, and Spec axis reports on the reviewed SHA. Resolve all actionable in-scope Critical, High, Medium, and Low findings before closing. Record a cited disposition for each disputed finding. Track static gates, tests, mutation, and runtime receipts as acceptance evidence.
9. **Q9 — Round cap.** Limit review to two rounds under Q6 and Q8. Return unresolved blockers to Bernstein at the cap with evidence and disposition. The cap confers no waiver.

## Files and implementation order

1. Fix the five IDs and capture the DEV-545 baseline before scoring changes. Use the same IDs for the after run. The baseline and the home-library result are delivery evidence.
2. Extend MovieFeatures and MovieFeaturesMapper with keyword names, collection name, and optional plot embedding. Add the IMovieCatalog read, EfMovieCatalog query, keyword weights cache and calculator, EfMovieRepository invalidation, and DI registration. Test distinct counts, keyword mutations, failed writes, and invalidation during a load.
3. Update SimilarityWeights under Recommendations:Weights and the related RecommendationOptions. Use Themes in place of Keyword, add Plot, remove YearProximity, RuntimeProximity, RatingBoost, and Writers, and preserve ReviewPlotWeight. Add the Themes, Plot, and graded reason settings listed above. Implement the scorer and its ordered evidence in SimilarityScorer and SimilarityResult.
4. Wire GetRecommendationsQueryHandler, MmrSelector, and SeedCollectionGrouper to consume the ordered scorer. Remove the Related handler's separate Similar story threshold and reason. Ensure fixtures that supply the shared scorer can supply the new feature evidence and weights.
5. Update the named test and evaluation surfaces as applicable: SimilarityScorerTests, SimilarityCriteriaTableTests, SimilarityWeightsTests, SimilarityWeightsSurvivorTests, RecommendationOptionsTests, EvaluationFixtureLoader, EvaluationFixtureLoaderTests, and TmdbSignalReplayTests. Keep the table in docs/recommendations.md aligned with the pinned test and use the same vocabulary in CONTEXT.md. Expose all six criteria and contributions in DEV-545 compare mode.
6. Run the verification checklist and record its results. After the required gates pass, capture the after snapshots and verify the home-library case. Publish and verify the receipts on the delivery PR.

## Phase 2 step 3 decisions

1. **Gate-clean snapshot order (T019 → T020 → T021).** Complete T019 and record passing results for all required gates on the candidate SHA before T020 captures the after snapshots from that revision. Record applicable non-blocking dispositions as such. T021 follows T020. This makes the order in implementation step 6 mandatory and preserves the distinction between gate results and observed runtime acceptance (Q5, Q8, plan Constitution Check).
2. **Pickup amendment scope (T001a).** T001 remains read-only. If it demonstrates drift within the approved scope, Keel amends `tasks.md` only, citing the pickup evidence in the amendment. Skip T001a if no drift is demonstrated. Stop and report to Bernstein if resolving the drift would require a change to another artifact or to the approved scope (Phase B pickup responsibility, Q6–Q7).
3. **Delivery PR read-back (T023).** The delivery PR may open while receipts are pending. After publication, read the delivery PR back and open each DEV-545 and home-library receipt from its published link. Confirm the receipts are accessible and match the staged T020–T021 evidence, then record the verified URLs and outcome in `EVIDENCE.md`. A failed read-back leaves Done-when and merge clearance pending. Observed acceptance remains required (Q5, Q8, FR-013 and SC-006).

## Plan challenge decisions

1. **Scoring options (T008–T011).** Validate a finite, strictly positive sum of the six non-negative scoring weights at startup and reject invalid scoring inputs at the explicit scorer boundary. Validate finite positive ThemesCeiling, finite Plot bounds in [-1,1] with floor < ceiling, finite display thresholds in [0,1], and MinimumKeywordMovies ≥ 1. Normalize the stoplist with OrdinalIgnoreCase, trimming entries and dropping null, blank, and duplicate entries. An explicitly null list is empty and an omitted setting retains the four defaults. Themes evidence keeps the ticket's shared counted names ordered by rarity, including ubiquitous names when they qualify. Zero value emits no reason.
2. **Catalog and cache (T001, T003–T007).** Read N and distinct n_k from one PostgreSQL statement or transaction with repeatable-read or stronger snapshot semantics. Maintain 0 ≤ n_k ≤ N, including during concurrent writes. An empty library yields zero Themes. The singleton serves one shared catalog in one API process under Patron's plan challenge ruling in CONCLUSIONS.md (d01e60f0). Inventory all production commits that can change N, membership, or eligibility, including keyword mutations without genre invalidation. Successful commits invalidate at every applicable site and failed writes preserve the snapshot. Reject stale repopulation, honor cancellation during version retries, and propagate load failure without caching it. Inspect the representative grouping query plan using existing schema and indexes. Report gaps through Q6–Q7.
3. **Operation boundary (T011–T014).** Pass one immutable keyword snapshot explicitly to the pure scorer. Resolve it once in the Related or For You handler where pair comparisons run, then pass it through the Related collection grouper or For You MMR selector as applicable. The For You handler change is limited to this plumbing under Patron's cited Q6/Q7 ruling in CONCLUSIONS.md (d01e60f0). Prove identity across pairs during mid-operation invalidation and refresh on the next operation.
4. **Consumer and delivery evidence (T014, T016–T020).** Exercise MMR selection and collection ordering with the new score and record changes visible in existing DEV-545 receipts. Keep existing tuning. Render names as literal metadata in compare output and use structured fields when logging. T018 names the retired symbols and reason phrases. T019 uses a buildable candidate after consumer and evaluation migration.

## Phase 5 sequencing decision

Patron approved the build prerequisite in `CONCLUSIONS.md` at `71d841a7`, based on Anvil's task note report at `2726b9d5`. Complete the six legacy scorer test and fixture migrations under T009. Before closing T014, land a separate T017 prerequisite commit limited to compiler-proven scorer API and weight adaptations in the named evaluation files and direct evaluation callers forced by the solution build. T014 closes with the full solution build and green T008–T009 and consumer regressions. T016 compare work, remaining T017 evaluation alignment, output proof, and T018 follow in the existing Phase 6 order.

## Test strategy and acceptance

- Pin the order, six defaults, table reason text, and configured denominator. Cover missing keywords, genre-only ceiling, score and contribution reconciliation, and the absence of legacy scoring options and reasons in recommendation code and tests.
- Cover case-insensitive stoplist exclusion, minimum frequency, rarity weighting and evidence names, empty and zero-norm vectors, Themes at ceiling and half ceiling, Plot floor and ceiling, reason ordering, zero threshold, exact threshold equality, and a below-threshold graded contribution. Check director, cast, keyword, collection, and genre names and evidence without reason text.
- Exercise the catalog query with real persistence for distinct counts and shared population. Exercise scoped calculator and singleton cache behavior, success-only invalidation, and stale in-flight reads. Verify the three consumers use the shared score and ordered reasons where displayed.
- Pin DEV-545 compare output to all six criteria and contributions. Include the five-movie Related baseline and after run and the For You before/after snapshot. Verify that A Fistful of Dollars lists The Good, the Bad and the Ugly with Same director and Shared themes.
- Record the three static-analysis gate results on changed C#, the test and mutation results, and the observed runtime evidence. Those receipts remain pending at grill closure.

## Scope exclusions

Profile relevance ranking and seed scoring (DEV-543), Related taste blending (DEV-543), candidate pool widening beyond plot top 50 (DEV-544), card layout and reason display UI (DEV-542), diversity caps, paging, and exclusions (DEV-541), plot text and embedding model changes (DEV-540), TMDb crowd similarity (DEV-539), and HTTP route, DTO, or OpenAPI changes are outside the frozen boundary. The keyword count design uses the existing architecture and schema. Anything else is a follow-up issue, not a finding in this round. Per Q6, outside-scope Critical, High, or broken behavior can receive a follow-up ticket. Other outside-scope observations are noted with no ticket.
