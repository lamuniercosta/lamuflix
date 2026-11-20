
---

## Q1

**Keel**

[from Keel] /grill-with-docs DEV-544 library feature load and Related strong matches. Q1. How should Related enforce above-bar ordering versus strong-reason-only results while preserving the existing score blend, MMR and collection grouping? I recommend pair similarity and its breakdown decide eligibility before penalties, then preserve the existing selectors within the ordering required by Wanted 4. Please rule on the precise ordering contract, citing DEV-544 Wanted 4 and 6 and Done when. I will append the exchange to specs/DEV-544/CONCLUSIONS.md.

**Patron**

Ruling: Partition strong matches before penalties into Tier A (pair similarity >= MinimumSimilarity) and Tier B (pair similarity < MinimumSimilarity with >= 1 StrongReasonCriteria criterion at value 1), then run BlendScorer + AlreadyWatchedPenalty, MmrSelector, and SeedCollectionGrouper keeping Tier A ordered strictly before Tier B up to take.

- Eligibility (DEV-544 Wanted 4): Evaluated on raw DEV-518 pair similarity and breakdown before taste-profile blend or AlreadyWatchedPenalty (MinimumSimilarity default 0.225; StrongReasonCriteria default Collection, Director, Themes where Themes=1 at raw keyword cosine >= ThemesCeiling); non-qualifiers excluded, rail never padded.
- Preserved pipeline (DEV-544 Wanted 6, Done when): ExcludeDisliked, taste-profile blend, AlreadyWatchedPenalty (BlendScorer.cs:99), MmrSelector, and SeedCollectionGrouper (GetRecommendationsQueryHandler.cs:42-87) stay intact and MmrSelectorTests.cs:329,:400 stay green.
- Ordering contract (DEV-544 Wanted 4, Done when): Every Tier B strong-reason-only match comes strictly after every Tier A above-bar match in the final rail, even when blend/penalties/MMR/grouping would otherwise reorder across tiers.

---

## Q2

**Keel**

[from Keel] DEV-544 grill Q2. Should a selected movie with no usable plot embedding continue to get an empty rail? I recommend the ticket default: preserve the selected-movie empty result, while candidates with no usable embedding can qualify with Plot = 0. This keeps the rollout candidate-side and requires coverage of both cases. Please cite the open point and Wanted 5 and send your complete ruling inline.

**Patron**

Ruling: Preserve the empty Related rail when the selected movie has no usable plot embedding, while keeping enriched candidates with no usable embedding eligible with Plot = 0.

- Selected movie with no usable vector (DEV-544 Open points, Problem 7, Where it lands EfRecommendationCandidateSource.cs:35-39): Follow the ticket default to keep the empty result for a selected movie missing a usable vector so this rollout stays strictly on the candidate side and existing selected-vector tests (EfRecommendationCandidateSourceTests.cs:353, :375) remain anchored.
- Candidate movie with no usable vector (DEV-544 Wanted 5, Done when): An enriched candidate lacking a feature row or usable embedding remains in the library feature load with its Plot criterion at 0 and all other DEV-518 criteria evaluated normally, enabling qualification (e.g. on shared director alone).

---

## Q3

**Keel**

[from Keel] DEV-544 grill Q3. How should the new library-load contract coexist with GetCandidatesAsync? Where it lands says add the method beside GetCandidatesAsync, while Wanted 3 removes its capped plot query and collection union and Wanted 8 keeps For you untouched. I recommend keeping the existing port, returning DEV-518 MovieFeatures plus state in the library load, and giving Related the single new call. Please resolve the old method and record compatibility without assuming any callers beyond recon. Send the ruling inline.

**Patron**

Ruling: Add the new whole-library feature load method on IRecommendationCandidateSource returning DEV-518 MovieFeatures + state flags + summary/embedding data, switch Related to use it exclusively, and rewrite GetCandidatesAsync / RecommendationCandidate to delegate to or reflect the uncapped feature-and-state load without the plot top-50 cap or collection union.

- Port and method contract (DEV-544 Wanted 1-3, Where it lands IRecommendationCandidateSource.cs:9-15): Keep IRecommendationCandidateSource (no new port, no ADR per constitution.md:510); add the library feature load method beside GetCandidatesAsync for whole-library feature loading (reused by DEV-543 For you), while GetCandidatesAsync in EfRecommendationCandidateSource drops the Take(CandidatePoolSize) plot cutoff and UnionSameCollectionAsync (:53, :65-68, :73-94) and keeps the enriched filter and selected-vector early return (:35-39, :46).
- Record replacement (DEV-544 Wanted 1, Where it lands RecommendationCandidate.cs:5, Open points): Replace the Movie entity on RecommendationCandidate with DEV-518 MovieFeatures, summary/display fields (id, title, release year, etc.), watched/review/watchlist state flags, and optional plot embedding (plus cosine when selected-relative), built via MovieFeaturesMapper with signals.
- Callers and tests (DEV-544 Done when, Where it lands): GetRecommendationsQueryHandler calls the single library load and For you stays untouched (Wanted 8); existing EfRecommendationCandidateSourceTests (:223, :241, :323, :442) and handler unit/property tests are rewritten against the uncapped feature-bearing contract.

---

## Q4

**Keel**

[from Keel] DEV-544 grill Q4. What library size should trigger review of the request-local embedding load? I recommend revisiting after the library exceeds 450 enriched movies, the ticket performance fixture size. Treat this as a follow-up measurement trigger only, with no runtime cap, paging or cache. At 450 x 384 floats the raw vector payload is 691200 bytes before object overhead. Please rule with a cited basis and reply inline.

**Patron**

Ruling: Load embeddings in memory per request with the library feature load, and set the documentation/measurement review trigger at > 450 enriched movies (the ticket performance benchmark size), with no runtime cap, paging, or embedding cache.

- Request-local load and no cache (DEV-544 Wanted 1, Wanted 9, Open points): Embeddings (EmbeddingDimensions.Value = 384 floats; 450 x 384 x 4 = 691,200 bytes raw, under 1 MB per request) are loaded per request alongside MovieFeatures, and nothing that scales with library size times embedding size is cached (HybridCachedRecommendationProfileSource.cs:39-74 unchanged).
- Review trigger at > 450 enriched movies (DEV-544 Wanted 10, Problem 2, Open points, Out of scope): The spec names > 450 enriched movies (matching the 449-movie home library and the 450-movie < 500 ms warm SLA in Wanted 10) purely as a follow-up measurement revisit threshold, since paging, caps, and caching are explicitly out of scope.

---

## Q5

**Keel**

[from Keel] DEV-544 grill Q5. How should we freeze the configurable strong-match options and the DEV-545 tuning boundary? I recommend names MinimumSimilarity and StrongReasonCriteria, ticket defaults 0.225 and Collection/Director/Themes, an empty list allowed, and Plot/Cast/Genre never usable as a strong reason. Keep 0.225 as the spec default and use DEV-545 quality evidence for pre-merge tuning review, with no silent default or rule change. Please rule on valid threshold and criterion configuration and send the answer inline.

**Patron**

Ruling: Freeze RecommendationOptions properties MinimumSimilarity (default 0.225, finite double in [0.0, 1.0]) and StrongReasonCriteria (default Collection, Director, Themes; empty collection valid; Plot, Cast, and Genre rejected by IValidatableObject validation), with DEV-545 used strictly for pre-merge quality verification and never for silent default or rule changes.

- Option names, defaults, and domain validation (DEV-544 Wanted 4, Done when, RecommendationOptions.cs): MinimumSimilarity defaults to 0.225 and validates as finite in [0.0, 1.0] (allowing values > max fixture pair score up to 1.0 for the empty-rail 200 integration test); StrongReasonCriteria defaults to [Collection, Director, Themes], permits an empty collection, and rejects null entries or disallowed criteria (Plot, Cast, Genre, or unknown names).
- DEV-545 tuning boundary (DEV-544 Open points Bar value and Odd strong reasons): 0.225 and [Collection, Director, Themes] are the frozen spec defaults; DEV-545 quality report and snapshot diff verify coverage and Pirate Radio / Hercules placement before merge, while odd rare-keyword pairs go to DEV-545 judged pairs / stoplist / ThemesCeiling rather than altering the rule or silently changing defaults.

---

## Q6

**Keel**

[from Keel] DEV-544 grill Q6. Please confirm the review loop terms. I recommend every accepted in-scope finding through Low blocks closure, with at most two rounds per loop and any unresolved blocker reported to Bernstein at the cap. Scope is the ticket whole-library load and Related behavior, options, copy, take descriptions, and ticket evidence as settled in Q1 to Q5. Anything else is a follow-up issue. A ticket change or necessary constitution departure follows PRODUCT section 5. Please confirm the closing bar, frozen scope and round cap inline.

**Patron**

Confirmed: closing bar is every accepted in-scope finding through Low blocking closure across Risk/Standards/Spec axes, round cap is 2 rounds per loop (escalate unresolved blockers to Bernstein at the cap), and scope is frozen to DEV-544 as settled in Q1-Q5 (out-of-scope Critical/High/broken behaviour gets a follow-up ticket via Rigger; ticket changes or constitution departures escalate per charter 2.3 / PRODUCT section 5).

---

## Plan Adjudication Ruling (Charter §2.3 item 6 & Q1/Q3/Q5 Refinements)

**Keel**

[from Keel] DEV-544 plan adjudication narrow Charter 2.3 item 6 ruling requested. Please approve or revise permission to adapt ReviewRankingHandlerTests.cs only at the cited Related substitute/constructor (Ledger L-2, lines 463-480) and RelatedMovies.test.tsx only for existing rail smoke (Compass F6, ticket Goal and FR-012). I propose putting option validation cases in already-authorized GetRecommendationsQueryHandlerTests.cs and no separate file. Please also confirm Q3 refinement (Wanted 1-3, 6, FR-001): GetLibraryFeaturesAsync remains the selected-agnostic name, selected-relative cosine/guard belong only to the selected-relative caller/compatibility method; preserve a selected movie with a usable embedding even if absent from the enriched load by using the existing selected-only seam when needed. Q1 uses existing MMR and seed grouping inside each tier, with A filling its budget first and B only getting remaining slots; unchanged standalone MMR tie/prefix contracts, no new cross-take guarantee for the grouped rail. Q5 uses exact normalized value 1 on DEV-518 terms, absent term never qualifies, and accepts only canonical configured tokens Collection/Director/Themes with ordinal case-insensitive comparisons and no whitespace trimming. Tier labels are spec-local partitions of the ticket term strong match, with no new glossary terms. Please give cited ruling inline.

**Patron**

Ruling: Approved narrow Charter §2.3 item 6 test adaptations (`ReviewRankingHandlerTests.cs:463-480`, `RelatedMovies.test.tsx` existing rail smoke) with option validation placed in `GetRecommendationsQueryHandlerTests.cs`, and ratified the Q1, Q3, and Q5 plan adjudication refinements.

- Charter §2.3 item 6 & test placement (DEV-544 Goal, Done when, Where it lands, FR-012): Forced edits to `tests/LamuFlix.UnitTests/Features/Recommendations/ReviewRankingHandlerTests.cs` (strictly adapting `RelatedHarness` substitute/constructor at lines 463-480 to the updated `IRecommendationCandidateSource` / `RecommendationCandidate` contract) and `web/src/features/movie-details/RelatedMovies.test.tsx` (updating empty-rail copy assertions to "No close match in your library." and existing rail smoke SMOKE-001/002) are approved; option validation cases belong in the already-authorized `GetRecommendationsQueryHandlerTests.cs` with no extra test file.
- Q3 refinement (DEV-544 Wanted 1-3, 6, FR-001, FR-003): `GetLibraryFeaturesAsync` is the selected-agnostic whole-library feature load name (reusable by DEV-543), while selected-relative cosine and the selected-vector empty guard belong strictly to the selected-relative caller (`GetRecommendationsQueryHandler`) and compatibility method (`GetCandidatesAsync`); when the selected movie is absent from the enriched library load (e.g. non-enriched selected record) yet has a usable embedding, the handler falls back to the existing selected-only seam (`IMovieRepository.GetAsync` / `IRecommendationProfileSource.GetProfileInputsAsync`) so usable-vector selected movies continue to score.
- Q1 & Q5 refinements and vocabulary (DEV-544 Wanted 4, 6, Done when, FR-005, FR-007): Tier A and Tier B (spec-local partitions of "strong match", adding no new ubiquitous-language glossary terms) each run existing `MmrSelector` and `SeedCollectionGrouper` within their tier so Tier A fills `take` first and Tier B receives only `Math.Max(0, take - tierACount)` (standalone `MmrSelectorTests.cs:329,:400` tie/prefix contracts stay unchanged, with no new cross-`take` prefix guarantee on the grouped rail); Q5 strong-reason qualification requires exact normalized value `1.0` on a present DEV-518 breakdown term (an absent term never qualifies) and `StrongReasonCriteria` accepts only canonical tokens `Collection`, `Director`, `Themes` matched via `StringComparer.OrdinalIgnoreCase` with no whitespace trimming.

---

## Gate 1 Ruling (2026-11-19)

- **Verdict**: `gate1: provisional` (all grill rulings Q1–Q6 and all 30 plan challenge dispositions in `PLAN_ADJUDICATION.md` accepted; `checklists/requirements.md` User-Visible Outcome items verified ticked; zero structural owner checkboxes).

---

## Gate 1 Owner Merge Receipt and Phase 1 Pickup (2026-11-20)

- **T000** — Owner spec PR [#452](https://github.com/lamuniercosta/lamuflix/pull/452) merged into `main` on 2026-11-19 at 17:18:25 UTC as `c589ff8c9c3b0bf801ed3e9420cc522262d3627d`. The DEV-544 task note identifies `1ac83297c1ba8ffc936987e8b198ce8dc612b01a` as the Phase A base. Git confirms the base precedes the spec merge and the merge is present in pickup HEAD.
- **T001** — Pickup worktree `F:/Dev/LamuFlix.worktrees/DEV-544`, branch `feature/DEV-544`, started at `2f1647b260963f645882eb2711cf425e143fa6ff`, which matched live `main`. DEV-518's scorer is available at that revision. The scorer populates `SimilarityResult.Breakdown` and normalizes Themes against `ThemesCeiling`.
- **Phase 1 evidence** — See `recon-DEV-544` for Wisp's bounded inventory and the `DEV-544` task note for Gauge's baseline at `2f1647b2`. Roslyn and cyclomatic complexity passed. InspectCode was `SKIPPED (scope-empty)`.
- **Remaining recon** — The Conductor directed the T001 checkbox update. Wisp's receipt leaves the solution-wide caller and test-symbol inventory, options host startup validation, selected-only fallback seam, contract generation workflow, ArchitectureTests receipt, and details of the scorer contract and Plot reason presentation unverified. These gaps remain open for recon before Build.
