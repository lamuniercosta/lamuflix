# DEV-543 evidence slots (Phase B)

**Status**: Slots only — no invented measurements, seed IDs beyond ticket-named titles, or gate PASS claims from planning.
**Worktree**: `F:/Dev/LamuFlix.worktrees/DEV-543`
**Rules**: Never commit credentials, connection strings, tokens, or live secret values. Presence-only checks for any environment credentials. Scrub disposable artifacts.

## 0. Prerequisites and identity

| Slot | Command / note | Result |
| --- | --- | --- |
| Gate 1 | CONCLUSIONS.md, verified spec PR merge receipt | T000 complete. PR #450 landed at `18e012145a9a8b2f1a3c00b1437611870b5a2433`. |
| Worktree / branch | Pickup identity | `F:/Dev/LamuFlix.worktrees/DEV-543` / `feature/DEV-543` |
| Pickup HEAD | `git log -1` | `345d3974657d577d60d5caaeac477197dd6d9c2f` |
| Landed main | `git rev-parse main origin/main`; `git merge-base HEAD origin/main` | Both refs and the merge base resolve to `1bbc8a637b97ec2efdf0eaa6f2be6da8abbbc089`. |
| DEV-518 implementation | Wisp, recon-DEV-543:51–52, 65 | **MERGED**. `2f1647b2` is an ancestor of pickup main. `SimilarityScorer` is present. Missing-feature semantics still need a pinned source reference for T001. |
| DEV-544 implementation | Wisp, recon-DEV-543:51–52, 58, 66–67; landed DEV-544 CONCLUSIONS Q3/Q5 | **MERGED**. `7d31c4b1` is an ancestor. Wisp identifies `IRecommendationCandidateSource.GetLibraryFeaturesAsync(CancellationToken)`, its EF implementation, `RecommendationOptions.StrongReasonCriteria`, and Related strong-reason tiering. Landed DEV-544 decisions document `MinimumSimilarity` at 0.225 and strong criteria Collection, Director and Themes at 1. Exact Related policy application and selection/tie keys remain to be pinned. |
| DEV-510 cache shape | Wisp, recon-DEV-543:51–52, 68 | **MERGED**. `5fa68c83` is an ancestor. Wisp identifies `HybridCachedRecommendationProfileSource` and `CachedProfile`. Serialized fields, playback timestamp join and freshness paths remain open. |
| DEV-545 harness / pre-edit baseline | Prior §0 receipt; T001 Gauge slot | Prior receipt identified `scripts/run-evaluation.ps1` and `scripts/run-recommendation-snapshot.ps1`. Pickup availability and Gauge's pre-edit baseline are pending on this identity. |
| Wanted 11 inventory | Wisp, recon-DEV-543:70–73; T014 repeat at `2e676e30` | **Recorded in §1**. Every remove-listed member has zero `src` callers; dependent test/Evaluation/DI/config dispositions are listed. T014 stops for a cited Patron approval on `AddLikedReviewReason` and on the outside-boundary test rewrites/deletions. |
| Typed recommendation options | Wisp, recon-DEV-543:74 | **Gap reported**. Consumers use `IOptions<RecommendationOptions>`. Wisp found startup validation for other option types and did not establish a recommendation registration with `ValidateOnStart()`. Pin the live registration and obtain a cited ruling if the required wiring falls outside the authorized files. |
| Proposed ADR 0047 | `git ls-tree origin/main docs/adr/0047-for-you-nearest-seeds.md`; T001 draft | Draft at [ADR 0047](../../docs/adr/0047-for-you-nearest-seeds.md) on the delivery branch. No standalone landing on pickup main. |
| SpecKit prerequisites | `check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` | **Could not run**. Script rejected `feature/DEV-543` because it expects a numeric feature-branch name. Manual bounded reanalysis is recorded in [CONCLUSIONS.md](CONCLUSIONS.md). |

**T001 status (2026-11-21): BLOCKED — needs recon.** The upstream merge stop from 2026-11-19 is cleared. The cross-artifact reread and Proposed ADR draft are complete. T001 remains open for the caller and contract evidence, options wiring disposition and Gauge baseline above. T002 and Build are held.

## 1. Wanted 11 caller inventory

**T014 inventory (2026-11-21, repeated at `2e676e30` on `feature/DEV-543`).** Searches were `grep -rn` over `src`, `tests`, `docker`, `scripts`, `docs` and `web` for each member name and for `TasteProfile`, `ReviewProfileChannels`, `LikedSource`, `ReviewAffinity`, `GetProfileInputsAsync`, `GetTasteProfileAsync`. Production (`src`) callers are separated from test, Evaluation, DI and configuration callers. Keep `AlreadyWatchedPenalty`.

| Member | Expected (Q2 / Wanted 11) | `src` callers found | Test / Evaluation callers found | Disposition |
| --- | --- | --- | --- | --- |
| `BlendScorer.Score` five-arg overload | Remove after checks | **None.** Only `BlendScorer.ScoreWithProfileContribution` is called (`GetRecommendationsQueryHandler.cs:345`); `BlendScorer.NonNegativeOffset` is called at `GetRecommendationsQueryHandler.cs:76` | `BlendScorerTests.cs`, `BlendScorerPropertyTests.cs`, `BlendScorerSurvivorTests.cs` (25 call sites; those files test only this overload) | Remove. Dependent test files are obsolete-only and would be deleted |
| `ProfileSimilarityCalculator.Similarity` | Remove after checks | **None** | `ProfileSimilarityCalculatorTests.cs` (named by T015; 17 call sites) and `ProfileSimilarityCalculatorPropertyTests.cs` (8) | Remove. `ProfileSimilarityCalculatorTests.cs` is T015-named; the property file is obsolete-only |
| `ProfileSimilarityCalculator.Evaluate` (same class) | Remove after For you/Related callers clear (boundary text) | **None** | `Dev510MutationSurvivorTests.cs:285,305` (2 of 37 class references) | Remove with `Similarity`; delete those two tests, keep the rest of the file |
| `ProfileSimilarityCalculator.EvaluateReviewAffinity` (same class) | Review-aggregate removal implies it (Q2) | **None** (Related no longer evaluates review affinity since T011) | `ReviewAffinityTests.cs` (52), `ReviewAffinityPropertyTests.cs` (28), `Dev510MutationSurvivorTests.cs` (35), `LikedReviewReasonTests.cs` (indirect) | Remove with the whole class; four dependent files become obsolete-only |
| `NotInterestedPenalty` | Remove after checks | **None as a signal.** `GetRecommendationsQueryHandler.cs:348` passes a literal `0.0`; validation at `RecommendationOptions.cs:511` | `BlendScorer*` (3 files), `RecommendationOptionsTests.cs`, `RecommendationOptionsSurvivorTests.cs`, `GetRecommendationsQueryHandlerTests.cs:1598`, `GetRecommendationsQueryHandlerPropertyTests.cs:176-187` | Remove; no configuration key exists for it (`docker/api.settings.example.json` `Recommendations` holds only `MinimumSimilarity` and `StrongReasonCriteria`; no `src/LamuFlix.Api/appsettings.json` recommendation keys) |
| `RecommendationScoring.ProfileContribution` | Remove after checks (Q2) | **None** | `LikedReviewReasonTests.cs:148`, `RecommendationScoringSurvivorTests.cs` (whole file), `ReviewAffinityPropertyTests.cs:84` | Remove; `RecommendationScoringSurvivorTests.cs` is obsolete-only |
| `LikedReviewWeight` / `DislikedReviewWeight` | Remove after checks (Q2) | **None once** `ProfileContribution` and `AddLikedReviewReason` go. Validation at `RecommendationOptions.cs:547,552` | `RecommendationOptionsTests.cs`, `RecommendationOptionsSurvivorTests.cs`, `LikedReviewReasonTests.cs`, `ReviewAffinityPropertyTests.cs` | Remove with the review-affinity stack; no config key bound |
| `RecommendationReasonBuilder.AddProfileReasons` (+ `Matches your {genre} interest` and `Matches your viewing history` templates) | Remove after checks (Q2) | **None.** For you uses `BuildSeedReasons`; Related emits structural reasons only since T012 | `RecommendationReasonBuilderSurvivorTests.cs:10,37` | Remove; delete those two tests, keep `EnsureFallbackReason` coverage |
| Plot centroid (`ComputePlotCentroid`, `TasteProfile.PlotEmbedding`) | Remove after checks (Q2) | `TasteProfileCalculator.cs:35` (producer) and `ProfileSimilarityCalculator.cs:29,43` (removed with the class); `TasteProfile.IsEmpty` | `TasteProfileCalculatorTests.cs` (T015-named), `TasteProfileProperties.cs`, `Domain/TasteProfileTests.cs`, `TasteProfileCacheBudgetFixture.cs`, `ProfileSimilarityCalculator*` | Remove. `MovieFeatures.PlotEmbedding` is a different, DEV-518-owned member and stays |
| Review aggregates / incidence maps on `TasteProfile` (`Liked`, `Disliked`, `LikedSources`, `ReviewProfileChannels`, `LikedReviewSource`) | Remove after checks (Q2) | **None.** Related dropped the profile at T011-T012; For you consumes only `PlaybackByMovieId` | `TasteProfileReviewAggregateTests.cs`, `Domain/ReviewProfileChannelsSurvivorTests.cs`, `ReviewAffinityTests.cs`, `Dev510MutationSurvivorTests.cs`, `TasteProfileCacheBudgetFixture.cs`/`Tests.cs`, `Evaluation.IntegrationTests/RealLibrary/RealLibraryReadOnlyLoaderTests.cs:122,161-163` | Remove. `PlaybackByMovieId` (T005 recency contract) is retained |
| `BlendProfileWeight` | Remove after checks (Q2) | `BlendScorer.cs:85,98` (removed overload) and `RecommendationReasonBuilder.cs:120`; validation `RecommendationOptions.cs:501,533` | `RecommendationOptionsTests.cs`, `RecommendationOptionsSurvivorTests.cs`, `BlendScorer*`, `LikedReviewReasonTests.cs`, `ReviewRankingHandlerTests.cs`, `GetRecommendationsQueryHandlerTests.cs:1596` | Remove with `BlendProfileWeight`-only validation (`BlendSignal` must keep the `BlendSelectedWeight` guard) |
| `SimilarityWeights.ReviewPlotWeight` / `HasEnabledReviewChannel` | Remove after checks (Q2) | `ProfileSimilarityCalculator.cs:175` (removed) and `RecommendationOptions.cs:585-595` (review-channel validation) | `SimilarityWeightsTests.cs`, `SimilarityWeightsSurvivorTests.cs`, `RecommendationOptionsSurvivorTests.cs:105-114`, plus `ReviewPlotWeight` in option fixtures of `GetForYouRecommendationsQueryHandlerTests.cs`, `NearestSeedScorerTests.cs:36-503`, `NearestSeedScorerPropertyTests.cs:34`, `GetRecommendationsQueryHandlerTests.cs:1574`, `ReviewRankingHandlerTests.cs:36`, `Evaluation/RealLibrary/QualityReportRunnerTests.cs:663` | Remove; DEV-518 keeps `Director`/`Themes`/`Plot`/`Cast`/`Genres`/`Collection` |
| `AlreadyWatchedPenalty` | **Keep** | `BlendScorer.cs:86,99` consumed by `GetRecommendationsQueryHandler.cs:345` | `BlendScorer*`, `GetRecommendationsQueryHandlerTests.cs`, `RecommendationOptionsTests.cs` | Keep |
| Year/Runtime/Rating weights | Out of scope (DEV-518) | n/a | n/a | Do not remove here |

**Separate disposition (T014 requires a cited Patron approval before deletion):** `RecommendationReasonBuilder.AddLikedReviewReason` has **no `src` caller** since T012 removed it from the Related handler; the For you seed path is now the only producer of the liked line (`brief.md` Cleanup and docs; Q2/Q5). Its only callers are `tests/LamuFlix.UnitTests/Features/Recommendations/LikedReviewReasonTests.cs` (10 tests). It also depends on `LikedReviewWeight`, `BlendProfileWeight` and `ReviewAffinityEvidence`, so it cannot stay once those members go.

**Dependency shrink dispositions (no `src` behaviour change):**

| Item | Evidence | Disposition |
| --- | --- | --- |
| `IRecommendationProfileSource.GetProfileInputsAsync(likedSourceIds)` | Only caller passes `[]` (`GetRecommendationsQueryHandler.cs:156-159`) | `likedSourceIds` and `RecommendationProfileInputs.LikedSourceEmbeddings` are dead; shrink is possible but touches `EfRecommendationProfileSource.cs`, `HybridCachedRecommendationProfileSource.cs`, `EfRecommendationProfileSourceTests.cs` (4 tests) and the Evaluation read-only harness |
| DI registration | `PersistenceServiceCollectionExtensions.cs:77-84` registers `EfRecommendationProfileSource` and the `HybridCachedRecommendationProfileSource` singleton | Unchanged by member removal; no removal needed |
| Configuration keys | `docker/api.settings.example.json:33-36` `Recommendations` = `MinimumSimilarity`, `StrongReasonCriteria`; no removed option is bound anywhere | No configuration key deletion needed |
| Evaluation / replay fixture | `HiddenSeedEvaluationCopy.cs:332` builds the profile through `TasteProfileCalculator`; `QualityReportRunnerTests.cs:663` sets `ReviewPlotWeight`; `RealLibraryReadOnlyLoaderTests.cs` asserts `LikedSources` | Needs the same edits as the unit suite; no committed binary fixture carries the removed fields |
| Cache shape | `HybridCachedRecommendationProfileSource.cs:155` `CachedProfile(TasteProfile, Generation, CreatedAt)` and cache key `v2` | Removing aggregates/centroid shrinks the payload; the DEV-510 "no library x embedding growth" invariant and the T005 playback payload must stay asserted |

**T014 outcome (2026-11-21): STOP — outside-boundary rewrite authorization required.** Every remove-listed member is free of `src` callers, but the deletions cascade into about 30 tracked files the ticket does not name: delete-only obsolete tests (`BlendScorerTests.cs`, `BlendScorerPropertyTests.cs`, `BlendScorerSurvivorTests.cs`, `ProfileSimilarityCalculatorPropertyTests.cs`, `ReviewAffinityTests.cs`, `ReviewAffinityPropertyTests.cs`, `LikedReviewReasonTests.cs`, `RecommendationScoringSurvivorTests.cs`, `Domain/ReviewProfileChannelsSurvivorTests.cs`, `Domain/TasteProfileTests.cs`, `TasteProfileReviewAggregateTests.cs`) and retained-coverage rewrites (`ProfileSimilarityCalculatorTests.cs` and `TasteProfileCalculatorTests.cs` are T015-named; `TasteProfileProperties.cs`, `TasteProfileCacheBudgetFixture.cs`, `TasteProfileCacheBudgetTests.cs`, `SimilarityWeightsTests.cs`, `SimilarityWeightsSurvivorTests.cs`, `RecommendationOptionsTests.cs`, `RecommendationOptionsSurvivorTests.cs`, `RecommendationReasonBuilderSurvivorTests.cs`, `Dev510MutationSurvivorTests.cs`, `GetForYouRecommendationsQueryHandlerTests.cs`, `NearestSeedScorerTests.cs`, `NearestSeedScorerPropertyTests.cs`, `GetRecommendationsQueryHandlerTests.cs`, `GetRecommendationsQueryHandlerPropertyTests.cs`, `ReviewRankingHandlerTests.cs`, `IntegrationTests/EfRecommendationProfileSourceTests.cs`, `HybridCachedRecommendationProfileSourceCacheTests.cs`, `ReviewTasteProfileIntegrationTests.cs`, `TasteProfileCacheHarness.cs`, `Evaluation/RealLibrary/QualityReportRunnerTests.cs`, `Evaluation.IntegrationTests/RealLibrary/RealLibraryReadOnlyLoaderTests.cs`). T015 stays unchecked until the cited ruling is recorded.

**T014 ruling receipt (2026-11-21)**: Patron authorized the `AddLikedReviewReason` deletion, the `IRecommendationProfileSource` / `RecommendationProfileInputs` shrink, and the outside-boundary obsolete-test deletions and retained-coverage rewrites listed above (recorded in [CONCLUSIONS.md](CONCLUSIONS.md), "Phase 5 T014–T015"). T015 proceeded on that cited disposition only.

**T015 outcome (executed on `feature/DEV-543`)**: every remove-listed Wanted 11 member is gone from `src`. `AlreadyWatchedPenalty`, `BlendSelectedWeight`, `NonNegativeOffset`, the DEV-518 year/runtime/rating weights and all other live scoring are retained.

- Removed production members: `BlendScorer.Score(5-arg)`, `BlendScorer.ScoreWithProfileContribution`, `ProfileSimilarityCalculator` (whole class), `ProfileSimilarityEvidence`, `ReviewAffinityEvidence`, `LikedSourceEvidence`, `ReviewProfileChannels`, `LikedReviewSource`, `RecommendationScoring.ProfileContribution`, `RecommendationReasonBuilder.AddProfileReasons` / `AddLikedReviewReason` / `Match your viewing history` / `Matches your {genre} interest` templates, `TasteProfile.PlotEmbedding` / `Liked` / `Disliked` / `LikedSources`, `TasteProfileCalculator` review aggregates / liked sources / plot centroid / equal-weight centroid / incidence maps, `RecommendationOptions.BlendProfileWeight` / `NotInterestedPenalty` / `LikedReviewWeight` / `DislikedReviewWeight`, `SimilarityWeights.ReviewPlotWeight` / `HasEnabledReviewChannel`, `RecommendationProfileInputs.LikedSourceEmbeddings`, `IRecommendationProfileSource.GetProfileInputsAsync(likedSourceIds)` and the matching EF / hybrid-cache overrides.
- `RecommendationOptions.Validate` drops `ValidateReview`, `ValidateReviewCoefficient` and `ValidateReviewChannels`; `ValidateBlendSignal` keeps only the `BlendSelectedWeight` guard (`BlendSelectedWeight cannot be 0.0.`). `BlendScorer.Combine` returns the blend directly because the finite-result guard became unreachable once the coefficients are validated non-negative/finite and the terms are bounded to [0, 1].
- Deleted obsolete-only test files (9): `BlendScorerTests.cs`, `BlendScorerPropertyTests.cs`, `ProfileSimilarityCalculatorTests.cs`, `ProfileSimilarityCalculatorPropertyTests.cs`, `ReviewAffinityTests.cs`, `ReviewAffinityPropertyTests.cs`, `LikedReviewReasonTests.cs`, `Domain/ReviewProfileChannelsSurvivorTests.cs`, `TasteProfileReviewAggregateTests.cs`.
- Retained-coverage rewrites in the inventoried files: `BlendScorerSurvivorTests.cs` (kept `Score` and `NonNegativeOffset` coverage), `RecommendationScoringSurvivorTests.cs` (kept `Genres`/`Cast`/`Directors`), `RecommendationReasonBuilderSurvivorTests.cs` (kept `EnsureFallbackReason` and the seed templates), `Domain/TasteProfileTests.cs` (rewritten to the `GenrePreferences` + `PlaybackByMovieId` shape), `TasteProfileCalculatorTests.cs`, `TasteProfileProperties.cs`, `TasteProfileCacheBudgetFixture.cs` / `TasteProfileCacheBudgetTests.cs` (payload now scales with playback-entry count, not liked-source count), `TasteProfileCacheObservabilityTests.cs` (warning-band fixture 13 500 and oversize fixture 18 000 playback entries), `TasteProfileCacheRegistrationTests.cs`, `Dev510MutationSurvivorTests.cs`, `SimilarityWeightsTests.cs` / `SimilarityWeightsSurvivorTests.cs`, `RecommendationOptionsTests.cs` / `RecommendationOptionsSurvivorTests.cs`, `ReviewRankingHandlerTests.cs`, `GetRecommendationsQueryHandlerTests.cs`, `GetRecommendationsQueryHandlerPropertyTests.cs` (dropped the `NotInterestedPenalty` property case), `GetForYouRecommendationsQueryHandlerTests.cs`, `NearestSeedScorerTests.cs` / `NearestSeedScorerPropertyTests.cs`, plus the integration and Evaluation files below.
- Behaviour-preserving note: the Related handler previously passed a constant `0.0` profile contribution and `0.0` not-interested term, so removing those coefficients leaves the published Related scores numerically identical (asserted by the retained Related suites).



## 2. DEV-545 before (capture before behaviour change)

**T002 executed on 2026-11-21 before any T003/T004/T005 edit**, on worktree `F:/Dev/LamuFlix.worktrees/DEV-543`, branch `feature/DEV-543`, HEAD `52c48c21` (production recommendation sources identical to `origin/main` at pickup; no scoring edit yet).

### Timing protocol (pinned — reused at T025 and T021)

1. Identity: `git rev-parse HEAD` recorded with every run.
2. Cache state: warm — five priming requests, discarded.
3. Measurement: ten sequential `GET /api/recommendations/for-you?take=30` requests; individual durations recorded, then median and maximum.
4. Pass bar: all ten under 500 ms with the 450-movie / 150-liked fixture.
5. No permanent telemetry is added for the count probe at T025.

### Baseline values

| Metric | Fixture / protocol | Value | Artifact path (untracked OK) |
| --- | --- | --- | --- |
| Leave-one-out hit rate @10 (synthetic publish) | `pwsh -NoProfile -File ./scripts/run-evaluation.ps1`, fixture `dev-356-baseline`, seed 356, cutoff 10 | **0.75** (9/12 observations), exit 0 | `artifacts/evaluation/metrics-summary.json` |
| Leave-one-out MRR (synthetic publish) | same run | **0.22903439153439153** (`reciprocalRankSum` 2.7484126984126984) | same |
| Leave-one-out HR/MRR on the real home library | `LAMUFLIX_EVALUATION_MODE=leave-one-out` (DEV-545 harness) | **Could not run** — the DEV-545 protocol requires a pre-provisioned `ConnectionStrings__DefaultConnection` in the process environment; it is absent in this Build-lane shell and the home Postgres service is ClusterIP-only. Not invented, not substituted. | n/a |
| Home snapshot capture | `pwsh -NoProfile -File ./scripts/run-recommendation-snapshot.ps1 -Mode Capture` against `http://localhost:3100`; 449-movie home library, seed ids 575/518/515/519/831, For you take 30, Related take 10 | exit 0, published 39236 bytes, SHA-256 `1E658F8F05BFBF95DC70B8518893F9116C1D0A80A27FCBEC9B2D1B465FEEEB93` | `artifacts/recommendations/t002-before-capture.json` |
| Warm For you runtime, individual (ms) | home instance `http://localhost:3100`, 449 movies / 4 liked (DEV-545 §9 cohort), 5 primes + 10 measured, take 30 | 12530.1, 12249.5, 12432.9, 12141.4, 10365.4, 10948.0, 11409.0, 11390.9, 12413.7, 11727.6 | n/a (measured in-shell) |
| Warm For you runtime, median | same run | **11727.6 ms** | |
| Warm For you runtime, maximum | same run | **12530.1 ms** | |
| 500 ms bar at T002 | brief fallback protocol | **NOT MET at baseline**: all ten timings exceeded 500 ms. This is the pre-change state the one-load/cache work (T005) and T021 must move. No score or bar was tuned to hide it. | |

**Disclosure**: the home instance serves 449 movies and 4 liked movies, not the 450/150 acceptance fixture; the absolute numbers are home-instance-relative and only same-protocol deltas are comparable. Library size obtained from `GET /api/movies?...` `totalCount` = 449. Cohort sizes quoted from `specs/DEV-545/EVIDENCE.md` §9 (liked 4, watched 27, overlap 4).

| Metric | Fixture | Value | Artifact path (untracked OK) |
| --- | --- | --- | --- |
| Leave-one-out hit rate | 2026-11-16 home / harness protocol | **pending** — could not run (credential absent); synthetic publish 0.75 recorded above | |
| Leave-one-out MRR | same | **pending** — could not run (credential absent); synthetic publish 0.229 recorded above | |
| Warm For you runtime | 450 movies / 150 liked | **pending** — measured on the 449/4 home library at 11727.6 ms median / 12530.1 ms max; 500 ms bar not met at baseline | |
| Notes / LOO disclaimer | harness-relative validity | The synthetic publish is deterministic and protocol-stable; real-library HR/MRR stay open until a credential-provisioned operator run. Only same-protocol deltas are comparable. | |

## 3. DEV-545 after + acceptance pairs

| Check | Expected | Result |
| --- | --- | --- |
| Top 10 distinct best seeds | ≥ 4 | **PASS** — 6 distinct best seeds in the top 10 (T020 run, §3a) |
| The Good, the Bad and the Ugly → A Fistful of Dollars | in top 10 | **FAIL (rank 14)** — pair and seed exist, outside top 10 (T020 run, §3a) |
| Hercules → Aladdin | in top 10 | **PASS** — rank 4, best seed Aladdin (T020 run, §3a) |
| Pirate Radio → About Time | in top 10 | **FAIL (rank 26)** — pair and seed exist, outside top 10 (T020 run, §3a) |
| Prohibited For you reason templates absent | three superseded strings absent | **PASS** — none of the three superseded templates and no `Matches your` text in the 50-item response (T020 run, §3a) |
| Leave-one-out hit rate after | not lower than before | **PASS on both cohorts** — liked HR@10 0 → 0.3636, watched HR@10 0.0357 → 0.2679 (same fixture, before `1bbc8a63` vs after `e537cc56`, §3b) |
| Leave-one-out MRR after | recorded | **PASS (recorded)** — liked MRR 0 → 0.2576, watched MRR 0.0058 → 0.1726 (§3b) |
| Warm For you runtime after | < 500 ms (450 / 150 liked) | **FAIL** — median 10931.6 ms, max 11699.6 ms; all ten timings above 500 ms. Same fixture before build `1bbc8a63` measured median 9327.6 ms (§3c) |

Ticket reference: 155 of 422 qualifying candidates on 2026-11-16 — reference data, not a hard gate.

### T020 snapshot acceptance probe (2026-11-21, candidate `e537cc56`)

**Harness disclosure.** `scripts/run-recommendation-snapshot.ps1` (DEV-545, not a DEV-543 file) rejects the response shape now landed on main: it requires `GET /api/recommendations/for-you` to return a bare JSON array, while the handler returns `{"items":[…]}` (`scripts/run-recommendation-snapshot.ps1` line ~825 → `Read-ApiList`; error: `The for-you response … must be a JSON array`). The script was **not modified** (outside this ticket's boundary). The equivalent read-only probe below reads the same endpoint's `items` array (`take=30` for the top-10 checks and `take=50` for the eligibility/rank lookup) and evaluates the same acceptance questions. The Related half of the snapshot is covered by the Compose UI smoke in §8 (SMOKE-002).

**Fixture.** Live home library dumped 2026-11-21T10:28Z and restored into a disposable local pgvector container: **447 movies, 11 liked, 56 watched (all 11 liked are also watched), 39 watchlist**. The 2026-11-16 review/watched state is no longer recoverable: every home backup on disk (`F:/Dev/lamuflix-home/backups`, latest 2026-11-15) has zero reviews and zero watched flags, and DEV-545 §9 recorded liked 4 / watched 27 on 2026-11-18. The numbers below are therefore the candidate's behaviour on the **current** home library, not on the ticket's frozen snapshot. No value was tuned or invented.

| Check | Expected | Measured on the current home library | Outcome |
| --- | --- | --- | --- |
| Distinct best seeds in top 10 | ≥ 4 | **6** — liked Batman: The Dark Knight Returns Part 2 (×3), watched Batman Begins, liked Aladdin (×4), liked Back to the Future, watched Batman: Year One, liked Batman: The Dark Knight Returns Part 1 | PASS |
| Hercules → Aladdin | in top 10 | rank **4**, first reason `Because you liked Aladdin` | PASS |
| The Good, the Bad and the Ugly → A Fistful of Dollars | in top 10 | rank **14**, first reason `Because you watched A Fistful of Dollars` | FAIL (pair present, outside top 10) |
| Pirate Radio → About Time | in top 10 | rank **26**, first reason `Because you liked About Time` | FAIL (pair present, outside top 10) |
| Prohibited reason strings | absent | `Matches your viewing history`, `Matches your`, `interest`, `No shared features identified` all absent across the 50-item response | PASS |

The two rank misses are consistent with library drift: the current library carries 11 liked and 56 watched seeds versus the 4 liked / 27 watched recorded on 2026-11-18, and the extra liked seeds (Batman/Aladdin/Back to the Future) crowd the top 10. Both required pairs are still produced with the expected best seed. Reported to the Conductor as failing acceptance evidence against SC-001; no ranking parameter was tuned.

### T021 leave-one-out before/after (same fixture, same protocol)

Fixture: the restored home library above. Before build `1bbc8a63` (`origin/main`, pre-DEV-543) in a detached worktree; after build `e537cc56`. Both runs used `LAMUFLIX_EVALUATION_MODE=leave-one-out` with `dotnet run --project tests/LamuFlix.Evaluation -- -class LamuFlix.Evaluation.Entry.RealLibraryEntryTests`, `ConnectionStrings__DefaultConnection` pointing at the disposable container, and `Ollama__Endpoint` at the home `all-minilm` provider. Both runs exited 0.

| Cohort | Seeds | Before HR@1 / HR@3 / HR@10 | After HR@1 / HR@3 / HR@10 | Before MRR | After MRR |
| --- | --- | --- | --- | --- | --- |
| Liked | 11 | 0 / 0 / 0 | 0.1818 / 0.3636 / 0.3636 | 0 | 0.2576 |
| Watched | 56 | 0 / 0 / 0.0357 | 0.1071 / 0.2679 / 0.2679 | 0.0058 | 0.1726 |

Hit rate does not decline: it rises on both cohorts. Overlap disclosure: all 11 liked seeds are also watched in this library state. Absolute values are harness-relative (endpoint and request validator bypassed; full-eligible `take`), so only same-protocol deltas are comparable.

| No-write check | Result |
| --- | --- |
| DB before-state (per-table counts for 15 tables + every movie review value) | captured before the after-run, 465 lines, SHA-256 `a102d7f17cb201596757a4c7e8f71c2340c97d65ed755f31e09b74289a744e90` (unversioned) |
| DB after-state immediately after the after-run | byte-identical (`diff` clean) — counts plus every review value unchanged |
| Artifacts | `artifacts/dev543/leave-one-out-before.json`, `leave-one-out-after.json` (unversioned, git-ignored) |

### T021 warm For you runtime (447 movies / 150 liked)

Fixture: a clone of the restored home library with the liked cohort raised to exactly **150** (139 additional likes on unwatched, unreviewed movies; 447 movies total, not 450 — the home library holds 447 and no rows were invented). Protocol: the pinned T002 protocol — 5 discarded primes, 10 measured sequential `GET /api/recommendations/for-you?take=30`, individual durations, median, maximum, pass only if all ten are under 500 ms. Both APIs were warmed containers against the same database; TMDb disabled.

| Build | Individual timings (ms) | Median (ms) | Max (ms) | All ten < 500 ms |
| --- | --- | --- | --- | --- |
| Before `1bbc8a63` | 9345.2, 8717.8, 8623.2, 9302.4, 9881.8, 9339.5, 9315.7, 8858.8, 13025.3, 11682.9 | 9327.6 | 13025.3 | **FAIL** |
| After `e537cc56` | 11588.5, 10225.0, 10776.1, 11259.8, 11699.6, 11466.1, 10672.6, 10351.6, 10493.4, 11087.1 | 10931.6 | 11699.6 | **FAIL** |

**SC-007 speed criterion FAILS and is recorded as failing evidence**: warm For you is ~11 s, 22× the 500 ms bar, and the candidate is ~1.6 s slower in median than the pre-change build on the identical fixture. The T025 disclosure is confirmed: the reset cost is the per-pair `SimilarityScorer.Score` call over every candidate × positive seed (447 × ~200 pairs per request), which re-validates options per call. No query, cache, timeout or fixture was changed to move the number. Reported to the Conductor for T023 scoping.

### T025 early For you probe (Phase 3, in place of T020/T021 acceptance)

| Slot | Value |
| --- | --- |
| Identity | `feature/DEV-543` worktree at the Phase 3 commit (recorded in the `DEV-543` note) |
| Timing protocol | T002 protocol: warm cache (5 discarded primes), 10 sequential `GET /api/recommendations/for-you?take=30`, individual/median/max, PASS only if all ten < 500 ms |
| 450-movie / 150-liked fixture | **Could not run.** The DEV-545 harness needs a pre-provisioned credentialed home database (`ConnectionStrings__DefaultConnection`); it is absent in this Build-lane shell (same blocker recorded for T002 §2). The `:3100` home instance is not rebuilt on this identity, so its 449-movie / 4-liked numbers are not a measurement of this code. No value was invented or substituted. |
| Evaluated positive-pair bound | Formula from `plan.md`: for C admitted-check candidates, P positive seeds and D disliked seeds, `C x (P + D)` minus positive self exclusions. On a 450/150 fixture with the home-like watched/watchlist cohort this is the number T021 must confirm. The probe that would count it could not run. |
| Disclosed cost risk | Each positive and disliked pair calls `SimilarityScorer.Score`, which re-validates `RecommendationOptions` per call (DEV-518 contract). At 450 candidates x ~150+ seeds this is tens of thousands of pair calls per request, so the 500 ms bar is at risk until measured. Reported to Bernstein before the Related switch, as T025 requires. |
| Early-probe outcome | **Could not run (no fixture/credential).** Not a pass and not a fail measurement. T021 remains the final acceptance run. |

## 4. Load / contract checks

| Check | Result |
| --- | --- |
| One DEV-544 library feature load per For you request | **PASS (T005, refreshed T006)** — `GetForYouRecommendationsQueryHandler` takes `IRecommendationCandidateSource` and calls `GetLibraryFeaturesAsync` exactly once; asserted by `HandleAsync_OneLibraryLoad_AndNoProfileInputsRead`, `HandleAsync_NoPositiveSeeds_ReturnsEmptyWithoutPadding`, `HandleAsync_CandidatePoolSize_CapsTake` and `HandleAsync_NoEligibleCandidates_ReturnsEmptyWithoutProfile` in `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs` |
| No per-movie `GetAsync` | **PASS (T005)** — `IMovieCatalog`/`IMovieRepository` removed from the handler; no paged walk and no per-movie load remain in the path |
| No second profile-inputs read on cache miss | **PASS (T005)** — the handler no longer calls `IRecommendationProfileSource.GetProfileInputsAsync`; playback is carried on `TasteProfile.PlaybackByMovieId` produced by the single `GetTasteProfileAsync` read (asserted `DidNotReceive` in the handler tests and `HandleAsync_ProfileCancelsToken_ThrowsBeforeGenreWeights`) |
| Cache not growing with library size × embedding size | **PASS (T005, serializer fix T006)** — `CachedProfile` shape unchanged (DEV-510); the added payload field is playback timestamps keyed by movie id, independent of embedding dimension. `CachedProfileSerializer` now registers a `MovieId` dictionary-key converter so `PlaybackByMovieId` round-trips; integration cache/freshness suites (`ReviewTasteProfileFreshnessTests`, `HybridCachedRecommendationProfileSource*`, feedback/playback invalidation) are green. |
| Playback joined by seed movie ID | **PASS (T005)** — `GetTasteProfileAsync_RetainsPlaybackBySeedMovieId_SoOldAndFreshSeedsScaleDifferently` in `tests/LamuFlix.IntegrationTests/EfRecommendationProfileSourceTests.cs` (Testcontainers, real persistence path): old and fresh equal-feature seeds receive different effective weights (0.25 floor vs 1.0) and a no-playback positive stays unscaled |
| `OpenApiContractTests` snapshot unchanged | **PASS (T022)** — snapshot and derived files untouched in the diff; `OpenApiContractTests` green in the 1868-passed IntegrationTests run (§5) |
| `dotnet test` UnitTests + IntegrationTests (Phase 2 scope) | `dotnet test tests/LamuFlix.UnitTests` — 4188 passed, 1 skipped, 0 failed. `dotnet test tests/LamuFlix.IntegrationTests --filter EfRecommendationProfileSourceTests` — 24 passed, 0 failed. Full T022 run still pending. |
| `dotnet test` UnitTests (Phase 3 interim) | `dotnet test tests/LamuFlix.UnitTests` — **4208 passed, 1 skipped, 0 failed**. |
| `dotnet test` IntegrationTests (Phase 3 interim) | `dotnet test tests/LamuFlix.IntegrationTests` — **1875 passed, 2 skipped, 0 failed**. Full T022 rerun on the delivery build still pending. |
| Phase 3 static gates (interim) | Roslyn analyzers **pass** (0 findings), cyclomatic complexity **pass** (0 over 15), InspectCode **pass** (0 WARNING+), `dotnet format` applied (no residual diff). T022 is still the final gate run. |

### Phase 4 Related profile removal (T011–T013)

| Check | Result |
| --- | --- |
| Related scores with DEV-518 similarity only | **PASS (T011)** — `GetRecommendationsQueryHandler` scores each eligible candidate with `SimilarityScorer.Score` (selected movie features vs candidate features) and applies the existing `AlreadyWatchedPenalty` through `BlendScorer` with a constant zero profile contribution. Disliked candidates and the selected movie stay excluded in `Partition`/`TryScore`; no admission filter or disliked-seed penalty was added to the Related handler. Related selection (tiering + `MmrSelector` + `SeedCollectionGrouper`), the DEV-544 pool and shared selection options are unchanged. |
| No taste-profile read on the Related path | **PASS (T012)** — `GetTasteProfileAsync` is no longer called; `GetProfileInputsAsync` is still used only to resolve a missing selected-movie embedding (empty liked-source list). Asserted by `HandleAsync_RelatedOperation_DoesNotAcquireTasteProfile`. |
| No profile reasons in Related results | **PASS (T012)** — `RecommendationReasonBuilder.AddProfileReasons` / `AddLikedReviewReason` call path removed from the handler; reasons are the DEV-518 structural reasons plus the existing structural fallback. The builder members themselves remain until the caller-gated cleanup (T014/T015). |
| Unrelated liked and disliked reviews leave Related scores unchanged (SC-003) | **PASS (T013)** — `HandleAsync_UnrelatedLikedAndDislikedReviews_LeaveExistingScoresUnchanged` in `tests/LamuFlix.UnitTests/Features/Recommendations/GetRecommendationsQueryHandlerTests.cs`; `Related_LikedProfile_LeavesSimilarCandidateScoreUnchanged` and `Related_DislikedProfile_LeavesSimilarCandidateScoreUnchanged` in `ReviewRankingHandlerTests.cs` |
| Disliked candidate exclusion + watched penalty (FR-008) | **PASS (T013)** — `HandleAsync_ReviewedMovieThatWasEligible_IsExcludedFromRelatedResults`; `HandleAsync_WatchedCandidate_AppliesAlreadyWatchedPenalty` (0.0 unwatched / 0.25 watched). |
| `dotnet test` UnitTests (Phase 4 interim) | **4199 passed, 1 skipped, 0 failed** |
| `dotnet test` IntegrationTests (Phase 4 interim) | **1875 passed, 2 skipped, 0 failed** (real Related handler paths in `EfRecommendationCandidateSourceTests`/`RecommendationEndpointTests` unchanged and green) |
| Phase 4 static gates (interim) | Roslyn analyzers **pass** (0 findings), cyclomatic complexity **pass** (0 over 15), InspectCode **pass** (0 WARNING+), `dotnet format --verify-no-changes` **pass**. T022 is still the final gate run. |
| Disclosed forced test adaptations | `GetRecommendationsQueryHandlerTests.cs` is the T013-named file (profile-reason/derived-profile cases removed or rewritten; watched-penalty expectation corrected). `ReviewRankingHandlerTests.cs` is **not** named by T013: its three Related cases asserted the removed profile behaviour, so they were inverted to the no-profile contract to keep the suite green. This is a substantive (not purely mechanical) adaptation of an earlier Patron-authorized call-site file and is disclosed for Patron retro-confirmation. |

### Phase 5 caller-gated cleanup and T016 re-check (T014–T016)

| Check | Result |
| --- | --- |
| No remaining `src` caller for any removed Wanted 11 member | **PASS (T014/T015)** — after the removals `grep -rn` over `src` finds no reference to `BlendProfileWeight`, `NotInterestedPenalty`, `LikedReviewWeight`, `DislikedReviewWeight`, `ReviewPlotWeight`, `HasEnabledReviewChannel`, `ProfileContribution`, `EvaluateReviewAffinity`, `ProfileSimilarityCalculator`, `ReviewProfileChannels`, `LikedReviewSource`, `LikedSourceEmbeddings`, `AddProfileReasons`, `AddLikedReviewReason` or `TasteProfile.PlotEmbedding`. `AlreadyWatchedPenalty` is retained and still consumed by `GetRecommendationsQueryHandler` through `BlendScorer.Score`. |
| DI registration and configuration keys | **PASS (T014)** — `PersistenceServiceCollectionExtensions.cs` registration unchanged; `docker/api.settings.example.json` never bound a removed option, so no configuration key was deleted. |
| Endpoint compatibility | **PASS (T016)** — `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs` is green (payload shape, statuses and reason list unchanged); `OpenApiContractTests` snapshot untouched by this phase. |
| One-load and per-request seed state | **PASS (T016)** — `tests/LamuFlix.IntegrationTests/EfRecommendationProfileSourceTests.cs` green: one DEV-544 library feature read per request, no per-movie load, candidate embeddings keyed by movie id, playback retained by seed movie id. |
| Watchlist transitions | **N/A by design (T016)** — watchlist state is a movie flag carried on the DEV-544 one-load `IRecommendationCandidateSource` path, not on the cached `TasteProfile` (which now holds only `GenrePreferences` and `PlaybackByMovieId`), so no profile-cache invalidation applies. Watchlist seed behaviour is asserted on the handler path in `GetForYouRecommendationsQueryHandlerTests.cs` and `NearestSeedScorerTests.cs`. |
| Like/dislike and playback transitions with the cache primed | **PASS (T016)** — `ReviewTasteProfileFreshnessTests` and `FeedbackTasteProfileInvalidationTests` rewritten to the retained profile shape: an unrelated liked review leaves `GenrePreferences` and `PlaybackByMovieId` unchanged, a dislike removes the movie's genres from the positive profile, clearing the review restores them, and playback-adding transitions still invalidate and recompute on the next request. |
| Cached payload excludes per-movie embeddings | **PASS (T016)** — `TasteProfileCacheBudgetTests.CachedShape_ExposesNoEmbeddingPropertyAtAnyLibrarySize` and `CompactFixture_CarriesNoEmbeddingPayloadInTheCachedShape` assert against the real `CachedProfileSerializer` write shape: no property name contains `Embedding`/`Plot` and the serialised payload contains none. |
| Cached payload does not grow with embedding dimension at fixed library size | **PASS (T016)** — the cached shape has no embedding field at all, so payload size is a function of `GenrePreferences` + `PlaybackByMovieId` only. `SerializedBytes_GrowMonotonicallyWithPlaybackEntryCount` and `PlaybackHeavyProfile_SerializesInsideTheNearLimitBandBelowTheCeiling` still pin the DEV-510 budget behaviour on the new shape. |
| Test suites | `dotnet test tests/LamuFlix.UnitTests` — **4073 passed, 1 skipped, 0 failed**. `dotnet test tests/LamuFlix.IntegrationTests` — **1868 passed, 2 skipped, 0 failed**. `dotnet test tests/LamuFlix.ArchitectureTests` — **16 passed, 0 failed**. `dotnet test tests/LamuFlix.Evaluation.IntegrationTests` — **68 passed, 0 failed**. `dotnet test tests/LamuFlix.Evaluation` — **210 passed, 1 failed**. |
| Disclosed pre-existing failure outside this phase | `LeaveOneOutRunnerTests.RunAsync_ranks_every_seed_and_reports_full_ranking_metrics` in `tests/LamuFlix.Evaluation/RealLibrary/LeaveOneOutRunnerTests.cs` expects a 3-item ranking but gets 2. Reproduced identically on the untouched Phase 4 commit `1db03585` in a detached worktree, so it predates Phase 5 and comes from the Phase 4 Related switch. The file is not in the authorized T014/T015 boundary, so it was not edited; it needs a Patron disposition before any change. |
| Static gates (interim, this phase) | Roslyn analyzers **pass** (0 findings), cyclomatic complexity **pass** (0 over 15), InspectCode **pass** (0 WARNING+), `dotnet format` applied (no residual diff). T022 remains the final gate run. |



## 5. Gate outcomes (honest vocabulary)

Record Pass / Failure / SKIPPED (scope-empty) / SKIP (opt-out) / Could not run / N/A — never fold non-pass into PASS.

| Gate | Outcome | Notes |
| --- | --- | --- |
| Build / `dotnet test` | **Pass** | `dotnet build LamuFlix.sln` 0 errors. UnitTests 4073 passed / 1 skipped / 0 failed; IntegrationTests 1868 passed / 2 skipped / 0 failed; ArchitectureTests 16 passed; Evaluation 211 passed. All runs used `--blame-hang --blame-hang-timeout 10m`. |
| Roslyn analyzers | **Pass** | `run-roslyn-analyzers.ps1` exit 0, no CA/IDE warnings. |
| Cyclomatic complexity | **Pass** | `run-cyclomatic-complexity.ps1` exit 0, 0 methods over 15. |
| InspectCode | **Pass** | `run-jetbrains-inspectcode.ps1` exit 0, no WARNING+ issues. |
| Property tests | **Pass** | `run-property-tests.ps1` 147 UnitTests property tests passed; harness reports 150 tests across 5 projects. |
| Vulnerable packages | **Pass** | `run-vulnerable-packages.ps1` PASS, no vulnerable packages (transitive included). |
| Format | **Pass** | `dotnet format --verify-no-changes` exit 0 (workspace-load informational warnings only, no file changes). |
| Mutation | **Pass** | `run-mutation.ps1` against `origin/main`: 561 tested mutants, 461 killed, 88 survived, 12 no-coverage, score **82.17%** vs threshold 80; no artifact-kill limit breach. |
| OpenAPI / contract | **Pass** | `OpenApiContractTests` green, snapshot unchanged (see §4). |
| Web / e2e | **N/A (scope-empty) for source; PASS for smoke** | No `/web` source file changed by this ticket. The Compose UI smoke in §8 was executed read-only and both scenarios passed. |

## 6. Size L review dispositions

| Axis | Report present | Disposition summary |
| --- | --- | --- |
| Sentry | _pending T023_ | |
| Ledger | _pending T023_ | |
| Compass | _pending T023_ | |

Closing bar: all accepted in-scope findings including Low fixed and verified within two rounds, or report blockers and hold phase open (`brief.md` Q7–Q8).

## 7. Docs same-PR checklist

| Artifact | Path | Status |
| --- | --- | --- |
| Nearest-seed ADR | Proposed `docs/adr/0047-for-you-nearest-seeds.md` (Conductor claim; Keel draft preserved for delivery; historical ticket/grill named 0045; DEV-545 took 0045; not landed at Gate 1 alone) | **verified T017** |
| ADR 0034 supersession note | `docs/adr/0034-explicit-movie-reviews.md` | **verified T018** |
| CONTEXT Taste Profile rewrite | `CONTEXT.md` ~143–145 | **verified T019** |
| Reason table documented | docs + builder parity | **verified T019** |

## 8. Compose UI smoke (verification only; no `/web` edits)

Record outcomes for both spec smoke scenarios after T009/T012 on the same candidate build. Default Compose stack. Required before T023 closure.

| Slot | Value |
| --- | --- |
| Build identity (`git rev-parse HEAD` / compose service or image id) | `git rev-parse HEAD` = `e537cc56b7eabcd293035e52675ee1dde7047a5c` (`feature/DEV-543`). Shipped Compose stack `docker-compose.yml`, project `dev543smoke`, images built from that HEAD: api `dev543smoke-api:latest` ID `44c91e48d9c2`, web `dev543smoke-web:latest` ID `e7486b84b2c4` (container images `sha256:44c91e48…`, `sha256:e7486b84…`). UI served through the shipped nginx at `http://localhost:3000`. |
| Fixture | Live home library dump taken 2026-11-21T10:28Z with `kubectl exec postgres-0 -- pg_dump` (no credential read, echoed or committed), restored into the stack Postgres: **447 movies, 11 liked, 56 watched, 39 watchlist**. The user library changed after 2026-11-16, so this is the current home library, not the ticket's 2026-11-16 snapshot (see §3a). Local API settings: `Features:LocalPlay` false, TMDb disabled, Ollama at the home `all-minilm` endpoint for startup verification. |
| Both rail changes present on this identity (For you seed reasons + Related profile-reason removal) | **PASS** — the same For you cards carry seed-first `Because you liked/watched …` lines while the Related rail on the same build carries structural reasons only, no profile reason. |

| Scenario | Surface | Expected | Outcome | Notes / date |
| --- | --- | --- | --- | --- |
| SMOKE-001 | Home For you rail | Seed-first reason lines naming liked/watched/watchlist title; three superseded generic templates absent | **PASS** | 2026-11-21. `/for-you` rail snapshot (`artifacts/dev543/smoke001-for-you-snapshot.txt`, unversioned) shows every card's first reason naming a seed title, e.g. `Because you liked Batman - The Dark Knight Returns - Part 2`, `Because you watched Batman Begins`, followed by structural reasons. Zero matches for `Matches your viewing history`, `Matches your … interest` or `No shared features identified`. |
| SMOKE-002 | Related rail | No result shows a "Matches your" profile reason | **PASS** | 2026-11-21. `/movies/549` Related rail snapshot (`artifacts/dev543/smoke002-related-snapshot.txt`, unversioned) lists only structural reasons (`Same director`, `Same collection`, `Shared themes`, `Shares cast`, `Similar story`, `Same genres`); zero `Matches your` and zero `No shared features identified` matches. Screenshot `artifacts/dev543/smoke002-related.png`. |

Verification only: no `/web` file was created or edited. Browser work used the local Playwright CLI session against `http://localhost:3000`.

## 9. Remediation round 1 (review round 1 findings, HEAD a98608aa)

Code changes on the acceptance build (this round, fix commit 1 of at most 2):

- Compass-1 (High, warm For You latency): `NearestSeedScorer.Rank` now validates `RecommendationOptions` once per request via `SimilarityScorer.ThrowIfInvalid` and scores every positive/disliked pair through `SimilarityScorer.ScoreWithoutValidation`. The public `SimilarityScorer.Score` boundary still validates, so the DEV-518 invalid-options contract is unchanged; only the per-pair repeat validation (tens of thousands of full DataAnnotations `Validate()` calls per warm request at 447 candidates x ~200 seeds) is gone. Added `Rank_InvalidOptions_ThrowsBeforeScoring` to keep the boundary covered.
- Ledger-F1 (Medium, shared static rank multipliers): `RecommendationOptions.SeedRankMultipliers` defaults are now built per instance from `const` values; no `static readonly` mutable state remains on this member. Added `SeedRankMultipliers_DefaultsArePerInstance`.
- Scoring contract unchanged: same weights, multipliers, recency floor and disliked penalty inputs produce the same scores; only redundant validation was removed.

Docs (this round, fix commit 2 of at most 2):

- Compass-3 (Low): §7 checklist now marks T017–T019 verified (receipts in tasks.md T017/T018/T019). Historical run identities in §3–§5 are preserved; they belong to builds `e537cc56`/`a98608aa` and do not attest the post-fix acceptance build.
- Compass-2 (Medium, frozen snapshot top-10): T020 rerun on the ticket's 2026-11-16 snapshot is still pending — the snapshot was unrecoverable in this lane (see §3a) and fixture recovery is routed through recon (Wisp). SC-001 stays open; no fixture or criterion was substituted.
- T021 acceptance rerun (450 movies / 150 liked, pinned T002 protocol, individual timings, before/after leave-one-out comparison) is pending on the post-fix acceptance build with a credentialed environment. T022 gate receipts and T024 Compose smoke for both rails are likewise pending rerun on that build with fixture and build identity recorded.

## 10. Remediation round 2 (review round 1 findings, code HEAD 6ac86aab)

Code (fix commit 1 of at most 2: `6ac86aab`, `DEV-543 - remediation round 2 WIP: prepared per-request scoring facets with exact-equivalence tests`):

- Compass-1 latency fix, second half: per-request prepared scoring facets. `GenreWeights.Prepare` / `PrepareSorted` precompute lookup sets once per request; `SimilarityScorer` gained prepared overloads; `NearestSeedScorer` scores every positive/disliked pair through the prepared path instead of rebuilding sets per pair. Added exact-equivalence tests in `SimilarityScorerPreparedTests.cs` (prepared vs unprepared scores identical).
- Analyzer-driven tweaks folded into the docs commit (commit 2 of 2, behavior-neutral): `GenreWeights.Prepare` internal-to-private, `NearestSeedScorer` tiebreak `==` to `CompareTo` (total ordering; differs from `==` only when a score is NaN), removed unused `using Xunit`. Full suite green on the final tree (see T022 below).

Docs (commit 2 of 2): this appendix plus the CONCLUSIONS EOF fix (trailing blank lines removed; `12110d4f` ref updated to `6ac86aab`).


### T020 round-2 probe (approved fixtures, post-fix build)

Probe: `artifacts/dev543/probe-t020.ps1` against `GET /api/recommendations/for-you?take=50`, raw response `artifacts/dev543/foryou-round2-t020-take50.json` (SHA-256 `fcc1cb87901cdc42`, 19:03 local). Fixture: current home library, 447 movies / 11 liked / 56 watched / 39 watchlist (same state as round-1 section 3a and the T024 compose DB below; corroborated live: the compose stack serves the identical top ranking, first item id 686).

| Check | Expected | Measured | Outcome |
| --- | --- | --- | --- |
| Distinct best seeds in top 10 | >= 4 | **6** -- liked Batman: The Dark Knight Returns Part 2, watched Batman Begins, liked Aladdin (x4 across items 8-10), liked Back to the Future, watched Batman: Year One, liked Batman: The Dark Knight Returns Part 1 | PASS |
| Hercules -> Aladdin | in top 10 | rank **4**, first reason `Because you liked Aladdin` | PASS |
| The Good, the Bad and the Ugly -> A Fistful of Dollars | pair present with expected seed | rank **14**, `Because you watched A Fistful of Dollars` | pair present, outside top 10 (owner-accepted) |
| Pirate Radio -> About Time | pair present with expected seed | rank **26**, `Because you liked About Time` | pair present, outside top 10 (owner-accepted) |
| Prohibited reason strings | absent | `Matches your viewing history`, `Matches your`, `interest`, `No shared features identified` all absent across the 50-item response | PASS |

Ranks and seed attributions are identical to the owner-accepted round-1 values (6 distinct, 4/14/26). Per the Owner Decision Receipt, available fixtures stand in for the unrecoverable 2026-11-16 snapshot; no score was tuned and no row fabricated.


### T021 leave-one-out round-2 before/after (same protocol, same eval fixture)

Protocol: `LAMUFLIX_EVALUATION_MODE=leave-one-out` with `dotnet run --project tests/LamuFlix.Evaluation -- -class LamuFlix.Evaluation.Entry.RealLibraryEntryTests`, `ConnectionStrings__DefaultConnection` at the fixture container, `Ollama__Endpoint` at the home all-minilm provider. The endpoint and its request validator are bypassed; the real For-you handler runs in-process with take equal to the eligible candidate count. Absolute values are harness-relative; only same-protocol deltas are comparable.
Fixture: 447-movie clone of the home library with the liked cohort raised to 150 (overlap liked 150 / watched 56 / overlap 11, identical in both files).

| File | SHA-256 | Cohorts |
| --- | --- | --- |
| `artifacts/dev543/leave-one-out-round2-before.json` (17:38 local) | `ef2a2c8068d0b7` | liked 150, watched 56 |
| `artifacts/dev543/leave-one-out-round2-after.json` (16:52 local) | `8b45c34dd2a88fa` | liked 150, watched 56 |

Before provenance: executed from the before-worktree `F:/Dev/LamuFlix.worktrees/DEV-543-LOO-before`, verified clean at `1bbc8a63` (the review base_sha, pre-DEV-543). After build: worktree content committed as `6ac86aab`. The two files cannot share a source build: their quality numbers differ well beyond noise (below), and the before timestamp postdates the after run, consistent with a separate pre-fix-code session.

| Cohort | Before HR@1 / HR@3 / HR@10 | After HR@1 / HR@3 / HR@10 | Before MRR | After MRR |
| --- | --- | --- | --- | --- |
| Liked (150) | 0.0067 / 0.0133 / 0.04 | 0.0067 / 0.0133 / 0.0867 | 0.0137 | 0.0223 |
| Watched (56) | 0 / 0 / 0.0536 | 0 / 0 / 0.0893 | 0.0073 | 0.0148 |

Hit rate does not decline: HR@10 and MRR rise on both cohorts after the round-2 facet change.


### T021 warm For-you runtime round-2 (447 movies / 150 liked fixture)

Fixture: `dev543-fixture-db` (`localhost:55432`, `lamuflix_fixture`), 447 movies with the liked cohort at 150. Protocol: the pinned T002 protocol (5 discarded primes, 10 sequential `GET /api/recommendations/for-you?take=30`, individual/median/max, pass only if all ten under 500 ms). TMDb disabled; `Features:LocalPlay` false. API from the final tree (Release; `6ac86aab` plus the behavior-neutral analyzer tweaks folded into commit 2). The embedding startup validator requires a reachable provider, so a local stub answered `POST /api/embed` with one finite 384-dim vector; the stub log shows exactly 2 hits (manual check + startup probe) and zero hits during any measurement window, so measured timings are pure ranking plus database.

| Run | Build / port | Individual timings (ms) | Median | Max | All ten < 500 ms |
| --- | --- | --- | --- | --- | --- |
| 18083 session (prior, 16:48 local) | post-fix worktree | 350.0, 322.4, 351.1, 384.1, 402.2, 338.1, 319.6, 344.1, 307.1, 396.0 | 347.0 | 402.2 | **PASS** (`warm-round2-t021.json`, SHA-256 `64476c242243dbdb`) |
| 18084 session (prior, 19:13 local) | unknown, no API log preserved | 1166.9, 914.9, 859.2, 892.0, 1015.6, 966.5, 815.5, 932.8, 990.8, 929.1 | 931.0 | 1166.9 | **FAIL** (`warm-round2-t021-final.json`); all ten slow, cause unrecoverable forensically (suspected machine contention; no server log was kept) |
| Rerun (this session) | final tree, Release, :18085 | 816.6, 441.8, 435.3, 459.8, 346.3, 390.9, 334.9, 303.8, 384.1, 352.3 | 387.5 | 816.6 | 9/10 (`warm-round2-t021-rerun.json`); first request slow, rest 303-460 |
| Confirm (this session) | final tree, Release, :18085 | 663.3, 696.8, 396.6, 364.6, 383.1, 431.9, 345.8, 356.3, 343.0, 292.3 | 373.8 | 696.8 | 8/10 early, then steady (`warm-round2-t021-confirm.json`); first two slow, rest 292-432 |

Reading: the Compass-1 fix holds (round-1 median 10931.6 ms on this fixture; all round-2 runs median 347-931 ms, steady state 290-460 ms, about 30x faster). But the strict all-ten-under-500 bar is flaky on a fresh process: 5 primes do not reliably reach steady state (tiered-JIT/pool warmup lands inside the measured window), and one prior run shows unexplained sustained slowness. Only the 18083 run fully clears the bar. This appendix reports all four runs; the close-out verdict on the T021 bar belongs to verification (Keel/T023), not to this remediation.


### T022 gates round-2 (final tree: 6ac86aab plus analyzer tweaks)

Solution suite `dotnet test LamuFlix.sln --blame-hang --blame-hang-timeout 10m`, exit 0: Evaluation 211 passed; Architecture 16 passed; UnitTests 4201 passed / 1 skipped (FifoFile platform test) / 0 failed; Evaluation.IntegrationTests 68 passed; IntegrationTests 1868 passed / 2 skipped / 0 failed. Skips are by design: `OpenApiRegeneration_OptInEnabled` (snapshot rewrite is opt-in only; the contract snapshot itself is asserted green) and the Ollama live-sample test (no provider in this shell).

| Gate | Result |
| --- | --- |
| `dotnet test` (unit + integration + OpenAPI contract snapshot) | **Pass**, exit 0, counts above |
| Roslyn analyzers | **Pass**, no CA/IDE warnings on changed files |
| Cyclomatic complexity | **Pass**, no method over threshold 15 |
| InspectCode | **Pass**, no WARNING+ issues |
| Property tests | **Pass**, 150 tests across 5 projects |
| Vulnerable packages | **Pass**, no vulnerable packages (transitive included) |
| Format | **Pass on changed projects** (`LamuFlix.Core`, `LamuFlix.UnitTests` verify-no-changes clean). Solution-wide `dotnet format` cannot load `src/LamuFlix.AppHost` in this shell: MSBuild rejects the Aspire bundle configuration (`AspireUseCliBundle=false`), an environmental tooling gap unrelated to this diff (AppHost untouched; last touch is an unrelated dependabot bump). |
| Mutation | **PASSED** vs threshold 80: 1019 tested mutants (Killed 852, Timeout 4, Survived 150, NoCoverage 13; Ignored 284 and CompileError 104 excluded), score **84.0%**, 1835.3 s wall, concurrency 8, perTestInIsolation. Range `origin/main...HEAD`; output root `C:\Users\lamun\AppData\Local\Temp\stryker\20261121-230253-413577` (outside repo). Kill-share and file-span artifact rules validated by the gate script (exit 0). |
| Web gates | **N/A scope-empty**: no `/web` file changed by this ticket. |

### T024 compose smoke round-2 (verification only; no `/web` edits)

Stack `dev543round2` from `F:\Dev\LamuFlix.worktrees\DEV-543\docker-compose.yml`: api image `dev543round2-api:latest` `0d11dd3f` (built 18:57 local from this worktree, post-`6ac86aab`), web `dev543round2-web:latest` `110e58cc1b71`, postgres/rabbitmq/ollama companions; services up since 18:58 local, api `/health/ready` 200, zero restarts. Fixture (live-counted in the compose postgres): **447 movies, 11 liked, 56 watched, 39 watchlist** -- the current home-library state, same as the T020 probe fixture.

| Scenario | Surface | Expected | Outcome |
| --- | --- | --- | --- |
| SMOKE-001 | Home For-you rail | Seed-first reason lines naming liked/watched/watchlist title; superseded generic templates absent | **PASS** -- UI dump `artifacts/dev543/smoke001-for-you-round2.txt` (19:00 local) shows every card leading with `Because you liked/watched ...` (e.g. `Because you liked Batman - The Dark Knight Returns - Part 2`); live API re-check this session via the `:3000` proxy returns the identical top ranking (first item id 686) with personal-first reasons. |
| SMOKE-002 | Related rail (`/movies/549`) | Structural reasons only; no `Matches your` profile reason | **PASS** -- UI dump `artifacts/dev543/smoke002-related-round2.txt` (19:00 local) lists only structural reasons; live API re-check this session (`GET /api/movies/549/recommendations?take=8`) returns 8 items (Rises, Begins, Sin City, Inception, Prestige, ...) with zero profile hits. |

Both rails verified present on the same stack identity. The UI dumps and the live API re-checks corroborate each other (identical ordering and reason shapes). No `/web` file was created or edited.
