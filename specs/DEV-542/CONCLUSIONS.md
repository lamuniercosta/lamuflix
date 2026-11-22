# DEV-542 grill conclusions

## Q1 — For You seed wording

**Keel asked.** Should the visible For You seed line use “Because you liked {title}” for each seed signal? Keel recommended the one template in the intake summary, with separate signal values in the data, and called out that this could imply a like for a watched or watchlisted seed. Keel asked Patron to flag any necessary delivery change for the owner.

**Patron ruled.** Use “Because you liked {title}” for `liked`, “Because you watched {title}” for `watched`, and “Because {title} is on your watchlist” for `watchlist`. Wanted item 2 expressly names all three templates. The earlier summary compressed that item. The exact ticket wording settles the decision under charter §2.3, without an owner checkbox.

**Basis and implications.** `recon-DEV-542:79–81,90–92` records the verbatim ticket text. Link each title to `/movies/{movieId}`, use “Open {title}” as the link accessible name, and cover all three signals in component tests.

---

## Q2 — Seed on the shared response DTO

**Keel asked.** Should `RecommendedMovieDto` describe `seed` as optional in OpenAPI, with Related omitting the JSON key and For You always sending a populated seed? Keel recommended that interpretation to preserve the shared generated DTO and meet the Related endpoint's evidence-only contract. Generated consumers will need to account for optionality, and wire tests can enforce the endpoint-specific guarantees.

**Patron ruled.** Yes. Keep the single shared DTO and omit the JSON key when the seed is null. A nullable seed property with a conditional JSON ignore attribute is one implementation. Related omits the key. Every For You item includes a populated seed.

**Basis and implications.** Wanted item 1, Done when item 3, `recon-DEV-542:52,89`, and `specs/PRODUCT.md` §5 item 4. Cover a populated For You seed and absence of the Related key in JSON integration tests, and assert the resulting generated API shape.

---

## Q3 — Evidence text and matched names

**Keel asked.** Which layer should format the three-names-plus-`+N` limit? Keel recommended that the backend supply display-ready text, preserve the complete ordered `matchedNames` list, and have the web display the server text verbatim. An API test would prove the formatting, and a component test would exercise a five-name item from that contract.

**Patron ruled.** The server owns the display-ready `text`. With five matched names it builds, for example, “Shared cast: Name One, Name Two, Name Three +2” and retains all five in `matchedNames`. Patron said that a component test can supply an item with five names and already-formatted text, and also discussed a possible client cap for fixtures that supply raw text. This plan uses the server-formatted response in the component test and renders `text` as given, so valid responses have one copy of the formatting logic.

**Basis and implications.** Wanted items 1 and 4, Open points, Done when item 5, and `recon-DEV-542:52,57,91`. Verify the formatted text and full name list at the server boundary and verify the visible first three names and “+2” in the card.

---

## Q4 — Dependency checkpoint at pickup

**Keel asked.** What rule applies if DEV-543 or DEV-518 cannot supply provenance at pickup? Keel recommended verifying both contracts before production edits and routing any gap to bounded recon and Patron. This lets Phase A plan against the ticket while protecting the existing result set and ranking.

**Patron ruled.** Require the checkpoint at Phase B pickup. Verify a real seed with movie ID, title, and one of the three supported signals for every For You item from DEV-543. Verify ordered criterion evidence from DEV-518 for For You and Related, including legitimate empty lists. A missing or insufficient contract stops pickup for bounded recon and a Patron ruling. Preserve recommendation scoring, ranking, and selection throughout.

**Basis and implications.** Ticket Needs, Technical design, Scope, and Out of scope at `recon-DEV-542:52–57,63,90`, charter §2.3, and `specs/PRODUCT.md` §§3 and 5. Phase A planning depends on the ticket contracts. Phase B must verify the upstream deliverables in its checkout.

---

## Q5 — Review closing agreement

**Keel asked.** Should the review close under a two-round cap, with all accepted in-scope findings from Critical through Low fixed or explicitly disposed with evidence, all applicable gates satisfied, and all acceptance evidence present? Keel proposed limiting the review to the seed/evidence contract and rendering, retirement of legacy reasons, generated artifacts, and ticket tests and evidence. Unresolved findings after round two would go to the PR.

**Patron ruled.** Confirmed the two-round cap and the closing bar at every severity. Require the home-library Hercules check and the before/after screenshots alongside the other acceptance evidence. The review scope is the DEV-542 response and rendering changes, legacy reason removal, generated OpenAPI and TypeScript artifacts, and the ticket tests.

**Basis and implications.** `recon-DEV-542:52–69,79–97`, the standing closing bar cited by Patron at `DEV-518:19–20`, and `task-pipeline` Phases 4 and 5. DEV-538 seed grouping, DEV-517 load more or swap, and scoring, ranking, or recommendation selection are outside the frozen scope. Anything else is a follow-up issue, not a finding in this round.

---

## Plan Challenge Ruling — Vocabulary, orphan cleanup, formatter seam, and upstream invariant guardrails

**Verdict.** Confirmed as proposed across all four plan-challenge decisions.

- **Vocabulary and formatter seam**: Keep domain vocabulary owned by DEV-543/DEV-518 with spec-local presentation terms in `specs/DEV-542/spec.md` and no root `CONTEXT.md` edit; reuse one pure Core DEV-518 formatter or existing feature-local seam located at pickup with no new architectural layer (Constitution VIII, Charter §2.3 item 2, `CONCLUSIONS.md` Q3–Q4, `recon-DEV-542:57`).
- **Legacy reason retirement and `Reason.cs`**: Retire `RecommendationReasonBuilder` and unused `ToReasonDto`/`Kind` mapping per ticket Technical Design, and delete `Reason.cs` only if Phase B pickup proves zero remaining callers (orphaned dead code); otherwise preserve unrelated callers intact (Charter §2.3 item 6 and dead-code rule, `recon-DEV-542:16–18,57`).
- **Upstream contract invariants**: Do not alter API status codes, invent fallback labels/seeds, or drop recommendations for malformed upstream seeds or unknown criteria; stop at the Phase B pickup checkpoint if upstream invariants cannot be demonstrated, and route any observed runtime defect back to Patron for disposition (Charter §2.3 item 4, `CONCLUSIONS.md` Q4, `recon-DEV-542:52–57,63`).

---

## Provisional Gate 1 Ruling

**Verdict.** `gate1: provisional` — zero structural owner checkboxes.

- **Checklist & Analyze**: `checklists/requirements.md` User-Visible Outcome items are all ticked (`[x]`), `/speckit-analyze` is clean, and all 28 plan challenge findings are adjudicated (`PLAN FROZEN` in `PLAN_ADJUDICATION.md`).
- **Owner escalations**: Zero structural owner checkboxes (no ticket contradiction or constitution departure under Charter §2.3).
- **Status**: `spec.md` marked `**Status**: gate1: provisional`; ordinary owner Gate 1 awaited user merge of the spec PR (`T000`).

## Gate 1 Spec-PR Merge Receipt

Gate 1 is complete (T000). GitHub reports that [spec PR #449](https://github.com/lamuniercosta/lamuflix/pull/449) merged into `main` on 2026-11-19 at 17:16:44 UTC as `cb7f9b894e1aaea78a7229b4bae41d19aab3e12e`.

The DEV-542 note in LamuFlix notes records Phase A base `1ac83297c1ba8ffc936987e8b198ce8dc612b01a`. Git confirms that this base precedes the spec merge and that the merge is an ancestor of the `feature/DEV-542` pickup HEAD `34c10f3e312176a67d2aa140f11ecd838adc7783`.

---

## T001 Pickup Dependency Checkpoint Ruling — Reschedule DEV-542 Build on DEV-518 (#455) and DEV-543

**Verdict.** Confirmed: stop DEV-542 Phase B at T001 without production edits, set DEV-542 in `chain` to `awaiting-merge: DEV-518 (#455); depends on DEV-543`, and proceed immediately to start the next eligible `queued` ticket (`DEV-543`).

- **T001 dependency checkpoint gate (`CONCLUSIONS.md` Q4 & Plan Challenge Ruling, `tasks.md` T001)**: DEV-542 explicitly requires real upstream For You seed attribution (`movieId`, title, `liked`/`watched`/`watchlist` signal) from DEV-543 and ordered criterion evidence from DEV-518 on `main` before any production edits (`FR-012`, `recon-DEV-542:52–57,63,90`). Because DEV-518 (`PR #455`) is awaiting merge and DEV-543 has not yet built/landed on `main`, DEV-542 must stop at T001 rather than stack branches or invent fallback provenance (`task-chain` §2: all tickets branch from `origin/main`, no stacking).
- **Non-blocking queue disposition (`task-chain` §2)**: Under `task-chain` §2 ("Next row = the first `queued` row that does not need code from an unmerged PR. A row that does gets status `awaiting-merge: DEV-### (#n)` (the PR it needs) and is skipped"), mark DEV-542 `awaiting-merge: DEV-518 (#455); depends on DEV-543` (which does not count toward the 2-in-flight limit) and re-run `DEV-542` Phase 1 rebase/pickup once both `DEV-518` (#455) and `DEV-543` have merged to `main`.
- **Chain continuation (`task-chain` §2)**: Advance the build chain immediately to the next eligible `queued` row (`DEV-543`, and `DEV-544` if eligible within the 2-in-flight limit).


## T001 Pickup Restart Checkpoint — 2026-11-22 (HEAD `2ada49f8`, origin/main `326756db`)

**Verdict.** `blocked` — the dependency checkpoint does not pass as written. No production edit was started, `tasks.md` T001 stays `[ ]`, and T002+ remain locked pending a cited Patron ruling on the paths below. Before screenshots were captured against the clean baseline first, as T001 requires.

**Restart context.** `git log`/`git merge-base` confirm `origin/main` = `326756db` and HEAD `2ada49f8`; DEV-543 (`326756db`, PR #461) and DEV-518 (`2f1647b2`, PR #455) are ancestors, so the previous `awaiting-merge` reason is resolved. This capture is the first before-screenshot baseline for the ticket: the earlier pickup stopped before capture.

### 1. DEV-543 seed contract — PASS

- `src/LamuFlix.Core/Features/Recommendations/NearestSeedScorer.cs:11-15` defines the closed `SeedSignal` vocabulary `Liked`, `WatchedWithoutReview`, `Watchlist`.
- `NearestSeedScorer.cs:19-25` carries `Signal`, `SeedMovieId`, `SeedTitle`; `:117-145` constructs `NearestSeedMatch` only from the strongest positive seed pair, so every emitted match has a real seed movie id, its title and one supported signal.
- `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs:98-106` maps every emitted For You item from a `NearestSeedMatch` via `ToRecommendedMovie`; no item is emitted without a match. Only retained (selected) results are mapped.
- Nonblank title invariant: `src/LamuFlix.Core/Domain/Movie.cs:226-231` (`RequireTitle` rejects null/whitespace on `Create` and `Rehydrate`); `src/LamuFlix.Core/Features/Recommendations/MovieSummaryMapper.cs:12-24` carries `movie.Title` into `MovieSummary`, which `NearestSeedMatch.SeedTitle` reads.
- Wire mapping for T004: `Liked` to `liked`, `WatchedWithoutReview` to `watched`, `Watchlist` to `watchlist`.

### 2. DEV-518 evidence contract — FAIL for the checkpoint as written

- **Supplied in Core, both scoring paths:** `src/LamuFlix.Core/Features/Recommendations/SimilarityResult.cs:8` `Breakdown` and `:12-15` `CriterionContribution(Name, Value, Weight, Contribution){ Evidence }`. `SimilarityScorer.Score` (`SimilarityScorer.cs:55-66`, used by the Related handler) and `ScorePreparedFull` (`:70-84`, used by `NearestSeedScorer.Evaluate`) both populate six criterion entries.
- **Public criterion vocabulary source:** the `CriterionContribution.Name` values produced by those two methods — `Collection`, `Director`, `Themes`, `Plot`, `Cast`, `Genre` (`SimilarityScorer.cs:58-63`, `:76-81`). There is no shared constants type; the record's `Name` is the public identity. `Director`, `Cast` and `Genre` names come from `Evidence`; `Plot` is value-only.
- **Not strongest-first:** `Breakdown` is fixed table order (Collection to Genre). `Contribution` is present, so sorting strongest-first is derivable by DEV-542 as presentation-only work, but DEV-518 does not itself supply the ordering the checkpoint claims.
- **Legitimate empty lists:** a criterion with no match carries an empty `Evidence` array, and the pending-enrichment Related path returns an empty list (`src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs:47-64`). Both are valid inputs.
- **Not exposed on the Related route, and not carried by the shared result:** `GetRecommendationsQueryHandler.FinishScore` (`src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs:352`) rebuilds the result as `new SimilarityResult(score, BuildReasons(similarity.Reasons))`, discarding `Breakdown`; `src/LamuFlix.Core/Features/Recommendations/RecommendedMovie.cs:7` carries only `Reasons`. Therefore `RecommendationEndpoints.ToDto` (`src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs:181-199`) cannot map `evidence` on **either** route without changing the Core result record.
- **No shared pure Core formatter in the ruled shape:** `SimilarityScorer.ReasonText` (`SimilarityScorer.cs:338-360`) is private, returns `Reason` display text only, exposes no criterion identity or `matchedNames`, has no three-name-plus-`N` cap, and emits `Shares cast: ...` where the ticket/Q3 text is `Shared cast: Name One, Name Two, Name Three +2`. Its concrete test files are `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs` (Cast/Director assertions at `:276`, `:310`, `:328`, `:344`), `SimilarityScorerReasonThresholdTests.cs` and `SimilarityCriteriaTableTests.cs:31-38`. Reuse means extending this seam, which changes reason text and format behavior.

### 3. Additional tracked paths outside the plan envelope — Patron ruling requested before edits

Charter §2.3 item 6 and `plan.md` ("Any other tracked file ... needs a cited Patron ruling") require a ruling for each:

- `src/LamuFlix.Core/Features/Recommendations/RecommendedMovie.cs` — must carry evidence (required by T004; not named by the ticket, spec or plan envelope).
- `src/LamuFlix.Core/Features/Recommendations/GetRecommendationsQueryHandler.cs` — Related assembly; named only by T001's bounded-recon line.
- `src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs` and its tests (`SimilarityScorerTests.cs`, `SimilarityScorerReasonThresholdTests.cs`, `SimilarityCriteriaTableTests.cs`) — formatter seam; named only by T001's bounded-recon line.
- Evaluation consumers of the retired `reasons` wire key (the ticket assumes only "the web app, one integration test and one e2e check" read reasons): `tests/LamuFlix.Evaluation/RealLibrary/CompareRunner.cs:215,241-249`, `RecommendationArtifactEnvelope.cs:39,45-57`, `RecommendationArtifactSerializer.cs:154,326-348,475-510`, `RecommendationArtifactEnvelopeTests.cs`, `CompareRunnerTests.cs`, `tests/LamuFlix.Evaluation.IntegrationTests/Entry/RecommendationSnapshotCommandTests.cs:94-99`, and golden fixtures `tests/LamuFlix.Evaluation/RealLibrary/Fixtures/capture-envelope.golden.json`.
- Other C# tests asserting `RecommendedMovie.Reasons` or reason text: `tests/LamuFlix.IntegrationTests/EfRecommendationCandidateSourceTests.cs:739-740,789`, `tests/LamuFlix.IntegrationTests/RecommendationEndpointTests.cs:484,660-667`, `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs:94-167`, `RecommendationReasonBuilderSurvivorTests.cs`.
- Web fixtures/tests outside the envelope: `web/src/mocks/fixtures.ts`, `web/src/mocks/forYou.test.ts`, `web/src/features/movie-details/MovieDetailsView.test.tsx`, `web/e2e/dev518-related-reasons.e2e.ts` (reads `reasons`; would become stale), `web/src/features/for-you/ForYouPage.test.tsx` and the evidence style anchor (named through T007/T009 only).

### 4. Verified path anchors for T002+ (once ruled)

- For You component test: `web/src/features/for-you/ForYouPage.test.tsx` (tests at `:26`, `:48`, `:68`). For You component style: `web/src/features/for-you/ForYouPage.css`; shared card style: `web/src/library/library.css`.
- Evidence style anchor (ticket-named `web/src/features/movie-details/RelatedMovies.css:73-80`): replace `.related-movie-reasons`/`.related-movie-reason` with the semantic evidence list. Shared responsive rule: **file** `web/src/features/movie-details/RelatedMovies.css`, **selector** `.movie-card-evidence` (new list class used by both `MovieCard` consumers), **breakpoint** the existing `@media (max-width: 47.9375rem)` (`RelatedMovies.css:89`; same breakpoint already used at `web/src/library/library.css:559`). Desktop shows two entries, small widths one; no new breakpoint number.
- Related assembly: `GetRecommendationsQueryHandler.HandleAsync` to `FinishScore` (`:352`) to `RecommendedMovie`.
- Consumer fixture inventory: `web/src/mocks/fixtures.ts:129,161,182`, `web/src/mocks/forYou.test.ts:45`, `web/src/features/movie-details/RelatedMovies.test.tsx` (four named cases at `:125`, `:148`, `:548`, `:580`), `web/src/library/MovieCard.test.tsx:361`, `web/e2e/for-you-review.e2e.ts:39-40,94-98`, `web/e2e/dev518-related-reasons.e2e.ts:63-92`.

### 5. Presentation bound — PASS

Mapping is over each already-selected result: For You maps every retained `NearestSeedMatch` (`GetForYouRecommendationsQueryHandler.cs:98-106`), Related maps the already-selected `RecommendedMovie`. Evidence formatting is a pure function over one result's six-entry `Breakdown`. No new candidate-loop read and no per-result seed/evidence lookup is required.

### 6. Before screenshots (clean baseline `2ada49f8`)

- Build/ref: `git rev-parse HEAD` = `2ada49f883e581de2ea9748081ebba3da8c3d2cb`; stack from `docker-compose.yml` plus `web/e2e/compose.e2e.yml`, served through nginx at `http://localhost:3000`; Chromium 1254; harness `npm run e2e` in `web/` exited 0 with the temporary baseline spec passing; the temporary spec was then deleted and the worktree returned to clean.
- Library fixture/state: `web/e2e/fixtures.mjs` sample library generated per run — two enriched movies, `Big Buck Bunny` (id 1, 2008) and `Elephants Dream` (id 2, 2006); action state = "Like" on `Big Buck Bunny`, so For You returns `Elephants Dream` and the Related rail for movie 1 returns the same card set.
- Viewport: 1440x900 (desktop) for both captures.
- Cards captured: For You card `Elephants Dream` with the legacy chip stack ("Because you liked Big Buck Bunny", "Same director: Smoke Fixture", "Similar story", "Shares cast: Smoke Fixture"); Related rail card set under `Related movies` with the same legacy chips.
- Paths (worktree-relative, git-ignored): `artifacts/DEV-542/before/for-you-desktop-1440x900.png`, `artifacts/DEV-542/before/related-rail-desktop-1440x900.png`; supporting raw responses `for-you-desktop-1440x900.json`, `related-rail-desktop-1440x900.json`, and `baseline.json` (HEAD, viewport, fixture state, seed movie) in the same folder.
- The home-library Hercules/Aladdin observation remains a separate T010 reviewer check, not this screenshot pair.

### 7. Regeneration and integration handoff

- Expected integration base at pickup: `origin/main` `326756db6733ce0032284919a866a5995f9f5d9d`; this branch is ahead only by the DEV-542 spec/record commits.
- Ordering: DEV-542 lands first; DEV-541 rebases onto the landed DEV-542 result before it regenerates `web/src/api/openapi.json`, `web/src/api/types.ts`, `web/src/mocks/handlers.ts` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`. DEV-542 blocks DEV-538.

### 8. What unblocks T001

A cited Patron ruling that either (a) extends the authorized envelope to the paths in §3, or (b) disposes of the evidence-wiring and formatter gap some other way that does not invent provenance or change scoring/ranking. With that ruling recorded here, the checkpoint's seed half already passes and the evidence half can be re-assessed against the ruled seam.

---

## T001 Pickup Attempt 2 Ruling — Authorized Envelope Extension, Evidence Ordering/Formatting Seam, and Out-of-Envelope Consumer Disposition

**Verdict.** Authorize the Core evidence-wiring and formatter seam edits (`RecommendedMovie.cs`, `GetRecommendationsQueryHandler.cs`, `SimilarityScorer.cs`, and their tests), derive strongest-first evidence ordering and display formatting (`Shared cast:` + three-name-plus-`+N` cap with full `matchedNames` and control-character sanitization) from DEV-518 `CriterionContribution`/`Breakdown` on selected results, and authorize updating all affected repo consumers/tests of the retired `reasons` contract so T001 passes and T002+ may proceed.

- **Core evidence & seed carrier and formatter seam (`RecommendedMovie.cs`, `GetRecommendationsQueryHandler.cs`, `SimilarityScorer.cs`, and unit/integration tests)**: Authorized under Charter §2.3 item 6 as forced by ticket Scope/Technical Design and `FR-001`–`FR-004`, `FR-009`, `FR-012` (`specs/DEV-542/spec.md:35–42,140–152`, `CONCLUSIONS.md` Q2–Q4). Extend `RecommendedMovie` to carry optional For You seed provenance (`SeedMovieId`, `SeedTitle`, `SeedSignal` — populated by `GetForYouRecommendationsQueryHandler.ToRecommendedMovie`, null on Related) and ordered criterion evidence (`Criterion`, server-built display `Text`, full ordered `MatchedNames`). Update `GetRecommendationsQueryHandler.FinishScore` (`:352`) and `HandleAsync` (`:99–102`) to preserve and project DEV-518 `similarity.Breakdown` (filtering to displayable criteria via `SimilarityScorer` thresholds and sorting strongest-first by `Contribution` descending with table-order tie-break) instead of discarding `Breakdown` into `BuildReasons`. Extend the `SimilarityScorer` feature-local formatter seam to format display text from `CriterionContribution` (`Shared cast:` prefix, first three names plus `+N` when `Evidence.Count > 3`, full ordered `MatchedNames` preserved, control characters sanitized at the display boundary) and update `SimilarityScorerTests.cs`, `SimilarityScorerReasonThresholdTests.cs`, `SimilarityCriteriaTableTests.cs`, `GetForYouRecommendationsQueryHandlerTests.cs`, `EfRecommendationCandidateSourceTests.cs`, and `RecommendationReasonBuilderSurvivorTests.cs` accordingly.
- **Web and E2E consumers/fixtures outside the initial plan list**: Authorized under Charter §2.3 item 6 as forced by the contract chain (`C# DTO → OpenAPI → generated TypeScript`, `FR-010`, `SC-002`, `SC-005`). Update `web/src/mocks/fixtures.ts`, `web/src/mocks/forYou.test.ts`, `web/src/features/movie-details/MovieDetailsView.test.tsx`, `web/src/features/for-you/ForYouPage.test.tsx`, `web/src/features/for-you/ForYouPage.css`, `web/src/library/library.css`, `web/src/features/movie-details/RelatedMovies.css`, and `web/e2e/dev518-related-reasons.e2e.ts` (migrating its `movie.reasons` reads to `movie.evidence` while preserving its DEV-518 smoke assertions) in the same T002–T010 contract transition.
- **Evaluation project consumers (`tests/LamuFlix.Evaluation/**` and `tests/LamuFlix.Evaluation.IntegrationTests/**`)**: Keep `LamuFlix.Evaluation`'s offline snapshot/compare artifact schema (`RecommendationArtifactEnvelope.cs`, `RecommendationArtifactSerializer.cs`, `CompareRunner.cs`, `RecommendationSnapshotCommandTests.cs`, and `capture-envelope.golden.json`) untouched if `SimilarityResult.Reasons` (or an internal evaluation mapping from `Breakdown`) can remain available without exposing `reasons` on `RecommendedMovie` or `RecommendedMovieDto` and without retaining `RecommendationReasonBuilder` ("Matches your viewing history" / fallback reason builder); only if retiring `RecommendationReasonBuilder` / `Reason.cs` forces a compile or test edit in `tests/LamuFlix.Evaluation/**`, those minimal mechanical edits are authorized to keep `dotnet test` green (`AGENTS.md` verification commands, Charter §2.3 item 6).

## Anvil T001 Checkpoint Report (Pickup Attempt 2, post-ruling) — PASS

**Verdict.** T001 dependency checkpoint PASSES with the T001 Pickup Attempt 2 ruling in place. No production edit was made in this task: T001 delivers verification, the before-screenshot baseline and this receipt. T002+ may proceed.

- **Worktree / branch**: `F:/Dev/LamuFlix.worktrees/DEV-542` on `feature/DEV-542`.
- **Baseline drift re-check**: `git diff --stat 2ada49f8..HEAD` = `specs/DEV-542/CONCLUSIONS.md` only (73 insertions, 0 deletions). Every commit since the captured baseline (`ef778cf9`, `0a16ae45`) is a record-only commit, so the existing `2ada49f8` before-screenshot pair is still a capture of the unmodified production baseline and needs no recapture. Branch is `origin/main` `326756db` + 4 doc-only commits.

### 1. Seed half — PASS (unchanged from pickup attempt 2)

- `src/LamuFlix.Core/Features/Recommendations/NearestSeedScorer.cs:11-15,19-25,117-145` — closed `SeedSignal` vocabulary (`Liked`, `WatchedWithoutReview`, `Watchlist`) plus `SeedMovieId`/`SeedTitle`, constructed only from the strongest positive seed pair.
- `src/LamuFlix.Core/Features/Recommendations/GetForYouRecommendationsQueryHandler.cs:98-106` — every emitted For You item comes from a `NearestSeedMatch`; none is emitted without one.
- Title invariant: `src/LamuFlix.Core/Domain/Movie.cs:226-231` (`RequireTitle`) via `MovieSummaryMapper.cs:12-24`.
- Wire mapping for T004: `Liked`→`liked`, `WatchedWithoutReview`→`watched`, `Watchlist`→`watchlist`.

### 2. Evidence half — PASS against the ruled seam

- **Supplied by DEV-518, both scoring paths**: `SimilarityResult.cs:8` `Breakdown`, `:12-15` `CriterionContribution(Name, Value, Weight, Contribution){ Evidence }`. Populated by `SimilarityScorer.Score` (`SimilarityScorer.cs:55-66`, Related) and `ScorePreparedFull` (`:70-84`, `NearestSeedScorer.Evaluate`), six entries each.
- **Public criterion vocabulary**: the `Name` values produced there — `Collection`, `Director`, `Themes`, `Plot`, `Cast`, `Genre` (`SimilarityScorer.cs:58-63,76-81`). No shared constants type; the record's `Name` is the public identity. `Director`/`Cast`/`Genre` carry names in `Evidence`; `Plot` is value-only.
- **Strongest-first ordering**: DEV-518 returns fixed table order, so ordering is DEV-542 presentation work. Authorized by the T001 Pickup Attempt 2 ruling (`Contribution` descending, table-order tie-break) over already-selected results. This is the previously blocking gap and is now authorized, not inherited.
- **Legitimate empty lists**: a criterion with no match carries an empty `Evidence` array; the pending-enrichment Related path returns an empty list (`src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs:47-64`). Both are valid inputs.
- **Carrier gap and its authorized remedy**: `GetRecommendationsQueryHandler.FinishScore:352` discards `Breakdown` (`new SimilarityResult(score, BuildReasons(similarity.Reasons))`) and `RecommendedMovie.cs:7` carries only `Reasons`; `RecommendationEndpoints.ToDto:181-199` therefore cannot map evidence on either route. Both files are inside the ruled envelope, so T004 can wire evidence without inventing provenance.
- **Formatter seam (reuse, no new layer)**: `SimilarityScorer.ReasonText:338-360` is the existing feature-local seam; T003/T004 extend it to the ruled `Shared cast:` + three-name-plus-`+N` shape and full `MatchedNames`. Its concrete test files, all present and verified: `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs` (T003 primary), `SimilarityScorerReasonThresholdTests.cs`, `SimilarityCriteriaTableTests.cs`. Confirmed `.Reasons` consumers in production code are exactly `GetForYouRecommendationsQueryHandler.cs:85-88`, `GetRecommendationsQueryHandler.cs:102,305,352`, `RecommendationReasonBuilder.cs`, and `RecommendationEndpoints.cs:198`.

### 3. Path anchors verified present (T002–T010)

- C# tests: `tests/LamuFlix.UnitTests/Features/Recommendations/GetForYouRecommendationsQueryHandlerTests.cs`, `RecommendationReasonBuilderSurvivorTests.cs`, `tests/LamuFlix.IntegrationTests/EfRecommendationCandidateSourceTests.cs`, `RecommendationEndpointTests.cs`, `ApiEndToEndRecommendationTests.cs`.
- DTO/envelope: `RecommendedMovieDto.cs` lives at `src/LamuFlix.Api/Endpoints/RecommendedMovieDto.cs` (matches `plan.md:97`); plus `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs`.
- Web: `web/src/features/for-you/ForYouPage.test.tsx` (cases at `:26`, `:48`, `:68`), `ForYouPage.css`, `web/src/features/movie-details/RelatedMovies.css`, `RelatedMovies.test.tsx`, `MovieDetailsView.test.tsx`, `web/src/library/library.css`, `MovieCard.test.tsx`, `web/src/mocks/fixtures.ts`, `web/src/mocks/forYou.test.ts`, `web/e2e/dev518-related-reasons.e2e.ts`, `web/e2e/for-you-review.e2e.ts`.
- Legacy reason retirement targets present: `src/LamuFlix.Core/Features/Recommendations/Reason.cs`, `RecommendationReasonBuilder.cs`.

### 4. Out-of-envelope consumer inventory (all now ruled)

- Web fixtures/tests reading the retired `reasons` key, confirmed by grep: `web/src/mocks/fixtures.ts:128,146,161,180,198,213,228`, `web/src/mocks/forYou.test.ts:43-48`, plus `MovieDetailsView.test.tsx` and `web/e2e/dev518-related-reasons.e2e.ts`.
- C# tests asserting `RecommendedMovie.Reasons` / reason text: `EfRecommendationCandidateSourceTests.cs`, `RecommendationEndpointTests.cs`, `GetForYouRecommendationsQueryHandlerTests.cs`, `RecommendationReasonBuilderSurvivorTests.cs`.
- Evaluation project: `CompareRunner.cs:215,241-243` and `RecommendationArtifactEnvelope.cs:39` still read `Result.Reasons`; the ruling keeps this artifact schema untouched where `SimilarityResult.Reasons` remains available, with only minimal mechanical edits authorized if retiring the reason builder forces them.

### 5. Presentational bounds and shared responsive rule — PASS

- Mapping is over already-selected results only: For You over retained `NearestSeedMatch`es (`GetForYouRecommendationsQueryHandler.cs:98-106`), Related over the already-selected `RecommendedMovie`. Formatting is a pure function over one result's six-entry `Breakdown`: no new candidate-loop read, no per-result seed/evidence lookup.
- Shared responsive rule recorded as one rule: **file** `web/src/features/movie-details/RelatedMovies.css`, **selector** `.movie-card-evidence` (new shared evidence-list class used by both `MovieCard` consumers), **breakpoint** the existing `@media (max-width: 47.9375rem)` (`RelatedMovies.css:89`; same value already used at `web/src/library/library.css:559`). Desktop shows two entries, small widths one; no new breakpoint number. Style anchor to replace: `.related-movie-reasons` / `.related-movie-reason` (`RelatedMovies.css:73-84`).

### 6. Before screenshots — present and valid (no recapture needed)

- Clean baseline: `gitHead` `2ada49f883e581de2ea9748081ebba3da8c3d2cb` (`artifacts/DEV-542/before/baseline.json`); production tree unchanged since (see drift re-check).
- Stack: `docker-compose.yml` + `web/e2e/compose.e2e.yml` behind nginx at `http://localhost:3000`; Chromium 1254; baseline captured via `npm run e2e` (exit 0) with a temporary spec, then removed.
- Fixture/state: `web/e2e` sample library (`Big Buck Bunny` id 1, 2008; `Elephants Dream` id 2, 2006), action = Like on `Big Buck Bunny`; `relatedSeedMovieId` = 1.
- Viewport 1440x900 desktop for both; cards captured: For You `Elephants Dream` with legacy chip stack, and the Related rail card set under `Related movies` with the same legacy chips (seed chip correctly absent there).
- Paths (worktree-relative, git-ignored): `artifacts/DEV-542/before/for-you-desktop-1440x900.png`, `artifacts/DEV-542/before/related-rail-desktop-1440x900.png`, with raw `*.json` responses and `baseline.json`. Both PNGs re-inspected during this task and confirmed to show the legacy reasons UI.
- The home-library Hercules/Aladdin observation stays a separate T010 reviewer check.

### 7. Regeneration handoff (unchanged)

- Expected integration base at pickup: `origin/main` `326756db6733ce0032284919a866a5995f9f5d9d`; this branch is ahead only by spec/record commits.
- Ordering: DEV-542 lands first; DEV-541 then rebases onto the landed DEV-542 result before regenerating `web/src/api/openapi.json`, `web/src/api/types.ts`, `web/src/mocks/handlers.ts` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json`. DEV-542 blocks DEV-538.

**Status.** done. T001 marked `[x]`; Phase 1 checkpoint unlocked. T002+ not started in this task.
