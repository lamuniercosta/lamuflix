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
| Wanted 11 inventory | Wisp, recon-DEV-543:70–73 | **Incomplete**. Partial production caller matches only. Full per-member inventory and downstream evidence are pending in §1. |
| Typed recommendation options | Wisp, recon-DEV-543:74 | **Gap reported**. Consumers use `IOptions<RecommendationOptions>`. Wisp found startup validation for other option types and did not establish a recommendation registration with `ValidateOnStart()`. Pin the live registration and obtain a cited ruling if the required wiring falls outside the authorized files. |
| Proposed ADR 0047 | `git ls-tree origin/main docs/adr/0047-for-you-nearest-seeds.md`; T001 draft | Draft at [ADR 0047](../../docs/adr/0047-for-you-nearest-seeds.md) on the delivery branch. No standalone landing on pickup main. |
| SpecKit prerequisites | `check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` | **Could not run**. Script rejected `feature/DEV-543` because it expects a numeric feature-branch name. Manual bounded reanalysis is recorded in [CONCLUSIONS.md](CONCLUSIONS.md). |

**T001 status (2026-11-21): BLOCKED — needs recon.** The upstream merge stop from 2026-11-19 is cleared. The cross-artifact reread and Proposed ADR draft are complete. T001 remains open for the caller and contract evidence, options wiring disposition and Gauge baseline above. T002 and Build are held.

## 1. Wanted 11 caller inventory

Record disposition per member before deletion. Keep `AlreadyWatchedPenalty`.

| Member | Expected (Q2 / Wanted 11) | Callers found | Disposition |
| --- | --- | --- | --- |
| `BlendScorer.Score` five-arg overload | Remove after checks | _pending_ | _pending_ |
| `ProfileSimilarityCalculator.Similarity` | Remove after checks | _pending_ | _pending_ |
| `NotInterestedPenalty` | Remove after checks | _pending_ | _pending_ |
| `RecommendationScoring.ProfileContribution` | Remove after checks (Q2) | _pending_ | _pending_ |
| `LikedReviewWeight` / `DislikedReviewWeight` | Remove after checks (Q2) | _pending_ | _pending_ |
| `RecommendationReasonBuilder.AddProfileReasons` | Remove after checks (Q2) | _pending_ | _pending_ |
| Plot centroid (`ComputePlotCentroid` / `PlotEmbedding`) | Remove after checks (Q2) | _pending_ | _pending_ |
| Review aggregates / incidence maps on `TasteProfile` | Remove after checks (Q2) | _pending_ | _pending_ |
| `BlendProfileWeight` | Remove after checks (Q2) | _pending_ | _pending_ |
| `SimilarityWeights.ReviewPlotWeight` / `HasEnabledReviewChannel` | Remove after checks (Q2) | _pending_ | _pending_ |
| `AlreadyWatchedPenalty` | **Keep** | _n/a_ | Keep |
| Year/Runtime/Rating weights | Out of scope (DEV-518) | _n/a_ | Do not remove here |

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
| Top 10 distinct best seeds | ≥ 4 | _pending T020_ |
| The Good, the Bad and the Ugly → A Fistful of Dollars | in top 10 | _pending T020_ |
| Hercules → Aladdin | in top 10 | _pending T020_ |
| Pirate Radio → About Time | in top 10 | _pending T020_ |
| Prohibited For you reason templates absent | three superseded strings absent | _pending T020_ |
| Leave-one-out hit rate after | not lower than before | _pending T021_ |
| Leave-one-out MRR after | recorded | _pending T021_ |
| Warm For you runtime after | < 500 ms (450 / 150 liked) | _pending T021_ |

Ticket reference: 155 of 422 qualifying candidates on 2026-11-16 — reference data, not a hard gate.

## 4. Load / contract checks

| Check | Result |
| --- | --- |
| One DEV-544 library feature load per For you request | **PASS (T005)** — `GetForYouRecommendationsQueryHandler` takes `IRecommendationCandidateSource` and calls `GetLibraryFeaturesAsync` exactly once; asserted by `HandleAsync_LibraryBeyondCandidatePoolSize_IsScoredFromOneLibraryLoad`, `HandleAsync_FullLibrary_IsReadOnceAndScored`, `HandleAsync_ForwardsActiveProfileLikedIdentities_ToProfileInputs` and `HandleAsync_LibraryLoadWithoutEligibleCandidates_NeverReadsTheProfile` in `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs` |
| No per-movie `GetAsync` | **PASS (T005)** — `IMovieCatalog`/`IMovieRepository` removed from the handler; no paged walk and no per-movie load remain in the path |
| No second profile-inputs read on cache miss | **PASS (T005)** — the handler no longer calls `IRecommendationProfileSource.GetProfileInputsAsync`; playback is carried on `TasteProfile.PlaybackByMovieId` produced by the single `GetTasteProfileAsync` read (asserted `DidNotReceive` in the handler tests and `HandleAsync_ProfileCancelsToken_ThrowsBeforeGenreWeights`) |
| Cache not growing with library size × embedding size | **PASS (T005)** — `CachedProfile` shape unchanged (DEV-510); the added payload field is playback timestamps keyed by movie id, independent of embedding dimension |
| Playback joined by seed movie ID | **PASS (T005)** — `GetTasteProfileAsync_RetainsPlaybackBySeedMovieId_SoOldAndFreshSeedsScaleDifferently` in `tests/LamuFlix.IntegrationTests/EfRecommendationProfileSourceTests.cs` (Testcontainers, real persistence path): old and fresh equal-feature seeds receive different effective weights (0.25 floor vs 1.0) and a no-playback positive stays unscaled |
| `OpenApiContractTests` snapshot unchanged | _pending T022_ |
| `dotnet test` UnitTests + IntegrationTests (Phase 2 scope) | `dotnet test tests/LamuFlix.UnitTests` — 4188 passed, 1 skipped, 0 failed. `dotnet test tests/LamuFlix.IntegrationTests --filter EfRecommendationProfileSourceTests` — 24 passed, 0 failed. Full T022 run still pending. |

## 5. Gate outcomes (honest vocabulary)

Record Pass / Failure / SKIPPED (scope-empty) / SKIP (opt-out) / Could not run / N/A — never fold non-pass into PASS.

| Gate | Outcome | Notes |
| --- | --- | --- |
| Build / `dotnet test` | _pending T022_ | |
| Roslyn analyzers | _pending_ | |
| Cyclomatic complexity | _pending_ | |
| InspectCode | _pending_ | |
| Property tests | _pending_ | |
| Vulnerable packages | _pending_ | |
| Format | _pending_ | |
| Mutation | _pending_ | |
| OpenAPI / contract | _pending_ | |
| Web / e2e | _pending_ | expect SKIPPED if `/web` untouched |

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
| Nearest-seed ADR | Proposed `docs/adr/0047-for-you-nearest-seeds.md` (Conductor claim; Keel draft preserved for delivery; historical ticket/grill named 0045; DEV-545 took 0045; not landed at Gate 1 alone) | _pending T017 delivery-diff check with T018/T019_ |
| ADR 0034 supersession note | `docs/adr/0034-explicit-movie-reviews.md` | _pending T018_ |
| CONTEXT Taste Profile rewrite | `CONTEXT.md` ~143–145 | _pending T019_ |
| Reason table documented | docs + builder parity | _pending T019_ |

## 8. Compose UI smoke (verification only; no `/web` edits)

Record outcomes for both spec smoke scenarios after T009/T012 on the same candidate build. Default Compose stack. Required before T023 closure.

| Slot | Value |
| --- | --- |
| Build identity (`git rev-parse HEAD` / compose service or image id) | _pending T024_ |
| Fixture | _pending T024_ |
| Both rail changes present on this identity (For you seed reasons + Related profile-reason removal) | _pending T024_ |

| Scenario | Surface | Expected | Outcome | Notes / date |
| --- | --- | --- | --- | --- |
| SMOKE-001 | Home For you rail | Seed-first reason lines naming liked/watched/watchlist title; three superseded generic templates absent | _pending T024_ | |
| SMOKE-002 | Related rail | No result shows a "Matches your" profile reason | _pending T024_ | |
