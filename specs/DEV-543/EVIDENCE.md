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

| Metric | Fixture | Value | Artifact path (untracked OK) |
| --- | --- | --- | --- |
| Leave-one-out hit rate | 2026-11-16 home / harness protocol | _pending T002_ | |
| Leave-one-out MRR | same | _pending T002_ | |
| Warm For you runtime | 450 movies / 150 liked | _pending T002_ | |
| Notes / LOO disclaimer | harness-relative validity | _pending_ | |

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
| One DEV-544 library feature load per For you request | _pending_ |
| No per-movie `GetAsync` | _pending_ |
| No second profile-inputs read on cache miss | _pending_ |
| Cache not growing with library size × embedding size | _pending_ |
| `OpenApiContractTests` snapshot unchanged | _pending T022_ |
| `dotnet test` UnitTests + IntegrationTests | _pending T022_ |

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
