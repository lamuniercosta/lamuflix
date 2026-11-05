# DEV-461 — Grill brief

Phase 2 grill closed with Patron on 2026-11-05 at **10 of 12 questions**. Q10 confirms shared understanding. The Phase 2 step 6 addendum below records subsequent challenge rulings and supersedes ambiguous implementation details in the original exchanges. This brief is the decision input for Quill, not a specification, implementation authorization, plan freeze, Gate 1 approval, or merge clearance.

## Identity and evidence

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-461-spec`.
- Branch: `feature/461-spec`.
- Intake base: `604d98e9a7376674fbe0adb0617ed0ca7cf490d3`.
- Task: DEV-461, Size L, parent DEV-288. Task-note UI tag was empty at intake.
- Facts: connected notes `DEV-461`, `recon-DEV-461`, and `recon-DEV-461-2`. The first recon note includes the verbatim tracker Problem, Scope, Recommendations, Open points, and Acceptance supplement. No independent production-code reconnaissance was performed by Keel.
- Decisions: Patron's append-only [CONCLUSIONS.md](CONCLUSIONS.md), Q1-Q10, and [ASSUMPTIONS.md](ASSUMPTIONS.md). Patron committed these at `3812c1c4`; Q9 supersedes earlier label alternatives and conceptual file-name shorthand.
- ADR: [0034-explicit-movie-reviews.md](../../docs/adr/0034-explicit-movie-reviews.md), number assigned by Bernstein and claimed in `chain`.
- The existing `.specify/feature.json` change belongs to intake and remains preserved.

## Problem and frozen delivery

Home-page card text feedback controls are bulky. Deliver a compact row of Watched, Thumbs up, Thumbs down, and Watchlist icons. Reviews must persist, appear on Movie Details, and actually affect both For You and Related through plot embedding and metadata preferences. A UI-only replacement or storage-only review does not meet the ticket.

The scope includes the shared row on all existing MovieCard surfaces and Movie Details, review persistence and migration, legacy compatibility, the approved review API and DTO fields, profile/scoring/configuration changes, cache freshness, truthful reason chips, glossary/ADR, and regression/contract/acceptance evidence. Existing list-row placement is unchanged. No new dependency, project, architectural layer, recommendation-engine replacement, LocalPlay/secrets/process work, or unrelated cleanup is included. The frozen scope is Q1-Q9; anything else is a follow-up issue, not a finding in this round.

Basis: ticket Scope/Recommendations/Acceptance in `recon-DEV-461`; Patron Q1-Q10.

## Canonical domain and compatibility decisions

One canonical per-movie `Review` has values `None`, `Liked`, and `Disliked`. It is independent of Watched and Watchlist. Selecting an inactive thumb selects that review directly, selecting the opposite thumb switches directly, and selecting the active thumb clears to None. Watched and Watchlist do not change Review.

Persist Review on the existing Movie aggregate/record. Migrate existing `IsNotInterested == true` to Disliked and false to None. Legacy not-interested state is a projection of `Review == Disliked`, never a second independently persisted truth. Legacy mark sets Disliked; legacy unmark clears only Disliked and preserves Liked. Repeat/invalid legacy transitions retain 409 behavior. New movies have no explicit review until reviewed.

Add idempotent `PUT /api/movies/{id}/review` with required body `{ "review": "none" | "liked" | "disliked" }`. Success, including repeat assignment, returns 204; malformed request returns 400; unknown movie returns 404; invalid id/value returns 422. Preserve legacy watched/not-interested/watchlist routes and existing fields. Add required `review` to summary, details, and recommended-movie DTOs; `isNotInterested` remains the compatibility projection.

Contract order is C# DTOs → OpenAPI → generated TypeScript and MSW handlers, with matching Verify baseline. Generate artifacts with existing tooling; never edit generated types or handlers manually or introduce handwritten duplicate web contracts.

Basis: Q1-Q2, Q5; recon route, record, contract, and cache maps.

## Shared icon row and exact wording

Use the existing shared feedback component on MovieCard and Movie Details. Existing MovieCard callers include the library grid, LibraryTile, For You, and Related. Move the bookmark from its card-poster and standalone-details positions into this same row; do not duplicate it. Keep existing store behavior and use the local SVG system with new local thumbs SVGs, without an icon dependency.

Every action has a title-bearing accessible name, `aria-pressed`, a tooltip on pointer hover and keyboard focus, visible active/focus state, and mouse/keyboard isolation from card navigation. Preserve optimistic updates, rollback, per-movie pending guards, error recovery, and shared state across card/details instances. Refresh retry must not replay a successful mutation.

Exact accessible names and tooltip text are action plus comma plus movie title:

| Icon, in row order | Inactive action | Active action |
| --- | --- | --- |
| Watched | `Mark Watched, {title}` | `Unmark Watched, {title}` |
| Thumbs up | `Like, {title}` | `Clear liked review, {title}` |
| Thumbs down | `Dislike, {title}` | `Clear disliked review, {title}` |
| Watchlist | `Add to Watchlist, {title}` | `Remove from Watchlist, {title}` |

These taste rulings are `[assumed]` in ASSUMPTIONS.md. Q9 replaces the earlier alternatives. The existing bookmark component is `WatchlistToggleButton` in `web/src/library/watchlist.tsx`; card/bookmark styling lives in `web/src/library/library.css`. Earlier references to `WatchlistButton.tsx` or `MovieCard.css` are not instructions to create those files. Palette finalizes visual details before plan challenge.

Basis: Q3, Q9; recon component/caller/watchlist maps and ticket accessibility acceptance.

## Recommendation approach

Retain the existing recency-weighted watched/playback baseline, excluding Disliked movies from its positive inputs. Extend Taste Profile with separate liked and disliked aggregates containing plot embeddings, genres, cast, and director. Include reviewed-but-unplayed movies. Each reviewed movie contributes one equal review signal; do not invent review timestamps or review recency.

Normalize review metadata as mean per-movie feature incidence and plot vectors as equal-weight centroids. Reuse existing feature identity, genre normalization, rarity conventions, finite-value checks, and embedding-dimension checks. Missing metadata creates no invented signal. Keep positive and negative aggregates separate so they cannot cancel inside a signed centroid or produce negative weighted-Jaccard maps.

Compute liked affinity `L` and disliked affinity `D` independently in `[0,1]`, as weighted averages over usable plot and metadata channels. Plot similarity uses cosine floored at zero; metadata uses weighted Jaccard. Use existing `SimilarityWeights.Genres`, `.Cast`, and `.Director`, plus configurable `ReviewPlotWeight`. Omit missing/disabled channels; no usable channel yields zero affinity.

```text
profileContribution = baselineSimilarity
                    + LikedReviewWeight * L
                    - DislikedReviewWeight * D
```

Use this contribution inside existing `BlendProfileWeight` in both recommendation handlers. Do not clamp the signed result back to `[0,1]`. Preserve selected-movie similarity, existing blend composition, MMR, and deterministic ordering except for the agreed changed profile contribution. Do not repurpose the unused not-interested blend penalty as an additional dislike penalty.

Defaults: `ReviewPlotWeight = 1.0`, `LikedReviewWeight = 1.0`, `DislikedReviewWeight = 1.0`. Keep weights configurable and validate at startup: finite nonnegative channel weights, finite strictly positive liked/disliked coefficients, and at least one enabled review channel. These defaults are recorded `[assumed]`, not asserted to be empirically tuned.

Eligibility remains: For You excludes Watched or Disliked; Related excludes Disliked. Liked alone excludes neither. None removes only the explicit review effect. Watchlist behavior is independent.

Persist review changes atomically in the database. Successful canonical and legacy review changes invalidate the existing server Taste Profile cache so the next recommendation request reflects the new state. Database commit and cache invalidation are not one transaction: preserve the incumbent post-commit failure contract specified in the step 6 addendum. Client mutations invalidate/refetch library, details, For You, and Related queries. A switch/clear removes the previous signal. Failures provisionally roll back optimistic state and do not claim success; authoritative refresh and idempotent retry reconcile an ambiguous committed write.

Basis: Q4-Q5; ticket embedding/metadata and next-request requirements; recon aggregation, similarity, options, eligibility, and invalidation evidence.

## Truthful reason chips

Keep the existing reason wire shape (`kind`, `text`). Emit at most one personal `Because you liked {X}` chip when a currently Liked source movie has positive evidence in an enabled review channel, the liked and profile weights are enabled, and the net profile contribution after dislikes is positive. Select the supported liked source by greatest positive review affinity, breaking ties by movie id, and sanitize its title with existing reason sanitation conventions.

Existing viewing-history/genre chips must use actual baseline evidence, never infer history from liked-only or disliked-only inputs. Preserve Related structural reasons and existing fallback. No fabricated positive reason for negative-only profiles or disabled channels. Carry internal provenance needed to prove the selected reason. Copy and tie-breaking are logged `[assumed]`.

Basis: Q6; ticket dynamic-reason requirement and recon reason/evidence map.

## Implementation plan decisions and ordering

Quill authors spec.md, plan.md, and tasks.md from this brief. Keel does not draft them. New code files stay within the existing feature/test/migration directories. Patron explicitly approved the following affected existing file groups under care item 6; this is not permission for unrelated rewriting.

| Order | Work and affected files | Evidence before dependent work |
| --- | --- | --- |
| 1 | Glossary, Movie review invariant, `MovieRecord`, `MovieConfiguration`, EF migrations/backfill, repository/projection mappings | State transitions and migration preserve historical marks; glossary terms settled |
| 2 | Existing `Features/Feedback` commands/handlers, `FeedbackEndpoints`, `HandlerRegistration`, `LibraryResponses`, `RecommendedMovieDto`, legacy adapters and DTO projections; contract outputs and Verify snapshot | HTTP matrix, reload projections, OpenAPI equality and generated types |
| 3 | `TasteProfile`, `TasteProfileMovie`, recommendation inputs/evidence, `TasteProfileCalculator`, `ProfileSimilarityCalculator`, `RecommendationOptions`/`SimilarityWeights`, both query handlers; `EfRecommendationProfileSource`, `EfMovieRepository`, cache wrapper only as needed | Per-channel math, ranking, reason provenance and warm-cache mutation tests |
| 4 | `MovieFeedbackActions.tsx/.css`, `MovieCard.tsx`, `MovieDetailsView.tsx/.css`, `library.css`, `watchlist.tsx`, existing watchlist store as needed, `movieFeedback.ts`, `movieFeedbackStore.ts`, `ui/icons.tsx`, generated-type fixtures/callers and associated tests | Four accessible icons, one bookmark, correct state/error behavior across surfaces |
| 5 | Existing backend/web verification and live acceptance evidence; no unrelated gate/config changes | Required gates and ticket acceptance evidence, honestly reported |

The contract step precedes web consumption. Extend existing layers and state mechanisms. Existing list-row controls remain outside the redesign. Do not change LocalPlay flags, process execution, secrets, dependencies, thresholds, or unrelated architecture.

Basis: Q2-Q8; both recon notes' file/test/contract maps.

## Glossary and ADR

Approved canonical definitions for the existing ubiquitous-language glossary (reported as root `CONTEXT.md` in `recon-DEV-461-2` §4):

- **Review**: An explicit opinion about a movie, with state None, Liked, or Disliked, independent of watching and watchlist intent.
- **Feedback**: Explicit Watched and Review actions about a movie.
- **Taste Profile**: An aggregate of viewing history and explicit review preferences.

Update the existing Feedback/Taste Profile definitions and add Review in the delivery PR, per Constitution VIII. Keel records these terms here within the seat's specs/ and docs/adr/ write boundaries; this brief does not claim the existing glossary has already been changed. Use the settled terms in the specification. ADR 0034 records the single canonical review, migration/compatibility, and separate-positive/negative-profile tradeoff; it is ready to feed specification drafting with these terms.

Basis: Q8-Q9; recon glossary and constitution evidence; Bernstein's ADR assignment.

## Test strategy and gate expectations

| Ticket/decision obligation | Required proof |
| --- | --- |
| Independent persistent review and legacy compatibility | Deterministic domain/handler tests for all states, active clear, opposite switch, Watched/Watchlist independence and legacy 409 semantics; Testcontainers migration/reload/projection/HTTP tests |
| Positive and negative effects in both recommendation handlers | Controlled candidate-pool comparisons before/after Like, Dislike, Clear and opposite switches, asserting actual relative ordering/exclusion with MMR controlled; per-channel tests for plot, genres, cast and director |
| Stable recommendation math/configuration | Unit/FsCheck properties for bounded affinities, equal-count normalization, signed contributions, absent metadata/embeddings, finite/dimension validation and invalid options |
| Next request observes successful review | Warm the real cached profile, mutate via canonical and legacy routes, then request recommendations and assert changed behavior; test clear/switch removes the old effect |
| Truthful explanations | Baseline-versus-review provenance, positive liked-source evidence, deterministic ties, sanitized titles, disabled-channel and negative-only cases |
| Four usable icons and persisted details state | Vitest/RTL with existing typed MSW handlers: every icon, exact names, tooltips, keyboard/focus/active states, navigation isolation, mutual exclusion, shared state, pending guard, rollback/retry and one relocated bookmark |
| Contract chain remains synchronized | Tooling-generated OpenAPI/TypeScript/MSW artifacts plus matching Verify baseline; no handwritten duplicate types or generated-file edits |

Use deterministic FakeEmbeddingGenerator rather than live Ollama in automated tests. Use Testcontainers for translation/serialization/migration behavior; follow existing xUnit v3/Shouldly/NSubstitute/FsCheck and web test conventions. Ranking changes are proven with controlled fixtures, not claimed as universal monotonic ordering under every MMR/configuration combination.

At implementation time use the repository backend verification pipeline and web-implement gates. Read applicable gate configuration from harness.yml; do not freeze guessed numeric gate thresholds into the plan. Mutation survivors require appropriate tests, never lowered thresholds. Generated artifacts change through their existing generator workflow. Report PASS, failure, scope-empty SKIPPED, opt-out SKIP, mutation N/A, and Could not run distinctly according to harness evidence. Planning artifact read-back is not execution of those gates. No build, test, mutation, or live runtime acceptance was run during this grill.

Basis: Q7-Q8 and supplied project rules; recon test/contract/constitution maps.

## Loop discipline and next stage

- **Closing bar:** Critical, High, and Medium findings block; Low findings are recorded follow-ups. Required gates, acceptance evidence, and all Sentry/Ledger/Compass reports must be present, with every blocker explicitly closed. Missing axis means blocked, never skipped.
- **Round cap:** Two rounds per analysis/review-remediation loop. Report unresolved blockers at the cap to Bernstein; do not waive them or silently start round three. Keel's subsequent speckit-analyze remains read-only and returns one numbered fix list per round.
- **Frozen scope:** Canonical independent three-state review, migration/legacy compatibility and agreed API/DTOs; accessible shared four-icon row and bookmark relocation; separate review plot/genre/cast/director affinities in both recommendation handlers; cache freshness, truthful reasons, configuration/validation, glossary/ADR, and agreed regression/contract/acceptance coverage; anything else is a follow-up issue, not a finding in this round.
- **Owner escalations:** None currently required. A later ticket contradiction or constitution departure requires the cited structural owner checkbox; Patron cannot close it and Gate 1 remains closed until the owner decides.
- **Next:** Bernstein coordinates Palette visual detail and Quill specification/plan/tasks drafting. Keel later checks artifacts against this brief and adjudicates plan challenge only at its assigned stage. Grill closure does not mean plan freeze or approval to implement. Merge remains user-owned.

Basis: Patron Q10, confirmed shared understanding at 10/12 questions.

## Full grill questions, answers, rationale, and implications

The following exchanges are copied from CONCLUSIONS.md so every answer is present in this brief. Q9's precise copy and factual file-name clarification supersede the earlier alternatives; historical exchanges are retained intact. Source citations inside Patron's exchanges are Patron-supplied; current technical facts for this brief come only from the two recon notes.

---

## Keel Q1/12 — full decision exchange

Question sent by Keel: Do you approve a canonical per-movie Review state None/Liked/Disliked, independent of Watched and Watchlist, with choosing the active thumb clearing to None and choosing the opposite thumb switching directly? I recommend this because ticket scope lines 149-153 requires a new positive signal, replacement negative signal, and mutual exclusion, while line 167 explicitly leaves watched independence open and suggests yes. The cost is a persisted three-state review and migration/compatibility decisions next. Ticket-mandated four icons and recommendation effects are already decided, not owner checkboxes.

Patron answer: Approved — canonical per-movie Review state (None / Liked / Disliked), independent of Watched and Watchlist, with choosing the active thumb clearing to None and choosing the opposite thumb switching directly.

Cited basis: DEV-461 Scope, recon-DEV-461 lines 148-153, and Open points, lines 166-167. Patron additionally cited specs/PRODUCT.md section 5; that source attribution is Patron-supplied, not independently verified by Keel.

Rationale: A single review value enforces mutually exclusive thumbs without conflating viewing history, watchlist intent, and opinion.

Implications: Persistence, migration, compatibility, public API, and recommendation weight semantics remain undecided. One of twelve grill questions used. No user escalation raised.


---

## Keel Q2/12 — full decision exchange

Question sent by Keel: Approve canonical review persistence and compatibility as follows? Add a Review None/Liked/Disliked value to the existing Movie aggregate/record, migrate IsNotInterested true to Disliked and false to None, retain legacy not-interested routes/DTO flag as compatibility projections of Review rather than a second source of truth. Legacy mark sets Disliked and legacy unmark clears only a Disliked value, preserving Liked; repeat/invalid legacy transitions keep existing 409 behavior. New idempotent PUT /api/movies/{id}/review with required body {review: none|liked|disliked} returns 204, malformed 400, unknown movie 404, invalid id/value 422; add required review field to summary/details/recommended DTOs and preserve existing fields. Recommend this to fulfill persistence and replacement without losing historical negative feedback or breaking existing clients. Cost: schema/data migration, compatibility tests and OpenAPI/generated types/snapshot updates. Basis recon-DEV-461 ticket lines 150,166,173,176 and existing routes/flags in sections 3-4. This is an explicit Patron ruling on schema/API and affected files, not owner escalation.

Patron answer: Approved — canonical Review (None/Liked/Disliked) on Movie aggregate/record, migration of IsNotInterested (true -> Disliked, false -> None), legacy /not-interested routes and isNotInterested DTO flags retained as compatibility projections of Review === Disliked (legacy mark sets Disliked, legacy unmark clears only Disliked preserving Liked, repeat/invalid transitions keep 409), idempotent PUT /api/movies/{id}/review with required {review: none|liked|disliked} returning 204/400/404/422, and required review field added to summary/details/recommended DTOs while preserving existing fields.

Cited basis: DEV-461 Scope, Open points, and Acceptance (recon-DEV-461 lines 150, 166, 173, 176), existing feedback routes/DTOs/records and OpenAPI contract chain (recon-DEV-461 sections 3-4, lines 110-115, 130-133), and specs/PRODUCT.md section 5 (care items 3, 4, 6).

Rationale: Keeping Review as the single persisted source of truth on the Movie aggregate/table eliminates state drift between legacy not-interested and thumbs reviews, preserves historical negative feedback via migration, provides an idempotent REST contract for direct three-state transitions, and maintains backward compatibility for existing /not-interested callers and DTO consumers.

Implications: Requires EF Core schema/data migration on Movies, domain/record/repository updates, PUT /api/movies/{id}/review endpoint and handler, compatibility projection on legacy /not-interested handlers and DTO mappers, and full regeneration of web/src/api/openapi.json, web/src/api/types.ts, MSW handlers, and OpenApiContractTests Verify snapshot. Two of twelve grill questions used. No user escalation raised.


---

## Keel Q3/12 — full decision exchange

Question sent by Keel: Approve one shared four-icon row (Watched, Thumbs up, Thumbs down, Watchlist in that order) on every existing MovieCard caller plus Movie Details, moving the existing bookmark from the card poster/details standalone position into this row and leaving list-row placement unchanged? Recommend extending the shared component rather than divergent home/details review controls because ticket acceptance requires details reflection, and recon maps shared card surfaces to library, For You, Related, and LibraryTile. Use existing local SVG system with new local thumbs SVGs and existing watchlist/feedback stores; no dependency, project or architectural layer. Every icon has title-bearing action name, pressed state, tooltip available on hover and keyboard focus, visible focus/active state, and click/key navigation isolation. Preserve optimistic updates, rollback, per-movie pending protection, error/retry behavior and shared cross-surface state; no duplicate watchlist control. Watched label reflects toggling, thumbs say Like/Dislike or Clear review with title, watchlist says Add/Remove with title. Cost: shared caller/CSS/tests and narrowly scoped watchlist button props change. Basis ticket scope/acceptance, recon-DEV-461 sections 1/5 and recon-DEV-461-2 sections 1/3. Record label/order assumptions [assumed] in ASSUMPTIONS.md and explicitly approve these affected existing files under care item 6; Palette owns final design details before plan challenge.

Patron answer: Approved — one shared four-icon row (Watched, Thumbs up, Thumbs down, Watchlist in that order) across all MovieCard callers and MovieDetailsView, moving the existing Watchlist bookmark into this row without duplicate controls and leaving MovieListRow unchanged; local SVG icons in web/src/ui/icons.tsx and existing feedback/watchlist stores with no new dependency, project, or layer; full accessible name/pressed/hover+focus tooltip/navigation isolation and optimistic/rollback/pending/retry behavior preserved; affected existing files explicitly approved under care item 6 and label/order assumptions logged [assumed] in specs/DEV-461/ASSUMPTIONS.md.

Cited basis: DEV-461 Scope and Acceptance (recon-DEV-461 lines 146-153, 172-175), component/caller/icon/test maps in recon-DEV-461 sections 1 & 5 and recon-DEV-461-2 sections 1 & 3, and specs/PRODUCT.md sections 4 & 5 (care items 1, 2, 6).

Rationale: Extending the single shared MovieFeedbackActions component across MovieCard and MovieDetailsView satisfies both the home card icon-row requirement and the Movie Details reflection AC without duplicating feedback or watchlist controls, while reusing local SVGs and existing stores avoids new dependencies or layers.

Implications: Rewrites/updates MovieFeedbackActions.tsx/.css, MovieCard.tsx/.css, MovieDetailsView.tsx/.css, WatchlistButton.tsx/.css, web/src/ui/icons.tsx, movieFeedback.ts, movieFeedbackStore.ts, and their test suites; Palette refines visual/UI design details prior to plan challenge. Three of twelve grill questions used. No user escalation raised.


---

## Keel Q4/12 — full decision exchange

Question sent by Keel: Approve this recommendation approach? Preserve the existing recency-weighted watched/playback baseline, but omit Disliked movies from its positive inputs; extend TasteProfile with separate liked and disliked aggregates including plot embedding, genres, cast, and director, including reviewed-but-unplayed movies. Each reviewed movie contributes one equal review signal, without invented review timestamps. Compute bounded [0,1] affinities L and D separately: weighted average of plot cosine floored at zero and metadata weighted-Jaccard channels; use existing SimilarityWeights Genres/Cast/Director and a configurable ReviewPlotWeight default 1. Missing/disabled channels are omitted and no usable channel yields zero. Recommendation profile contribution becomes baselineSimilarity + LikedReviewWeight*L - DislikedReviewWeight*D, inside existing BlendProfileWeight; default liked/disliked coefficients 1 each. Keep review profiles separate, not a signed centroid, to avoid cancellation and invalid negative Jaccard preferences. Preserve existing selected-movie similarity, blend, MMR and ordering except this signed contribution; do not clamp it back to [0,1]. Require finite nonnegative channel weights, finite strictly positive review coefficients, and at least one enabled review channel at startup. This directly satisfies ticket embedding AND metadata (genres,cast,director) effects in both For You and Related; cost is extending profile/source/evidence/options and focused tests, not a new layer/dependency. Basis recon-DEV-461 ticket lines 157-168 and recon-DEV-461-2 section 2 formulas.

Patron answer: Approved — preserve recency-weighted watched/playback baseline omitting Disliked movies from positive inputs; extend TasteProfile with separate liked and disliked aggregates (plot embedding, genres, cast, director, including reviewed-but-unplayed movies, one equal signal per reviewed movie without review timestamps); compute bounded [0,1] affinities L and D separately as weighted average of plot cosine floored at 0 and weighted-Jaccard metadata channels using SimilarityWeights (Genres, Cast, Director) plus configurable ReviewPlotWeight (default 1.0), omitting missing/disabled channels and yielding 0 when none are usable; set profile contribution to baselineSimilarity + LikedReviewWeight*L - DislikedReviewWeight*D (unclamped, defaults 1.0 each) inside BlendProfileWeight while preserving selected-movie similarity, blend, MMR, and ordering; validate finite non-negative channel weights, finite strictly positive review coefficients, and at least one enabled review channel at startup.

Cited basis: DEV-461 Recommendations and Open points sections (recon-DEV-461 lines 155-168, 174), existing recommendation pipeline and formulas in recon-DEV-461 section 3 and recon-DEV-461-2 section 2, and specs/PRODUCT.md sections 4 & 5 (care items 1, 2, 6).

Rationale: Separate liked and disliked aggregates preserve non-negative weighted-Jaccard math and prevent positive/negative vector cancellation, while combining baselineSimilarity + LikedReviewWeight*L - DislikedReviewWeight*D without clamping to [0,1] guarantees that both plot embeddings and metadata (genres, cast, director) raise similar candidates when liked and lower them when disliked in both For You and Related.

Implications: Extends EfRecommendationProfileSource, TasteProfileMovie, TasteProfile, TasteProfileCalculator, ProfileSimilarityCalculator, RecommendationOptions/SimilarityWeights + startup validation, and unit/property/integration tests in Features/Recommendations. Default weight values logged [assumed] in specs/DEV-461/ASSUMPTIONS.md. Four of twelve grill questions used. No user escalation raised.


---

## Keel Q5/12 — full decision exchange

Question sent by Keel: Approve the next-request review lifecycle? Persist review changes atomically, invalidate the existing cached TasteProfile after successful canonical or legacy review changes, and invalidate/refetch library, details, For You and Related client queries after a successful mutation. Clearing/switching must remove the old signal as well as add the new one; failures roll back optimistic review and must not claim success, and refresh retry must never replay the mutation. Retain existing eligibility: For You excludes Watched and Disliked, Related excludes Disliked, Liked alone excludes neither; None removes only the review effect. Existing watchlist behavior is unaffected. Do not add a candidate-specific dislike penalty in addition to Q4 (the existing dead notInterested blend term is not repurposed). Review profile metadata preferences are mean per-movie feature incidence, plot vectors are equal-weight centroids; reuse existing feature identity/genre normalization and rarity conventions, preserve finite/dimension checks, and missing metadata must not invent signals. This completes Q4 normalization and makes the ticket next-request effect reproducible without UI-only sorting. Cost: cache invalidation integration tests, projection coverage and per-movie shared-store lifecycle tests. Basis ticket lines 160-161 and recon maps for current eligibility/cache/store in recon-DEV-461 sections 1/3/5 and recon-DEV-461-2 section 2.

Patron answer: Approved — atomic review persistence; cached TasteProfile invalidation on successful canonical or legacy review changes; client query invalidation/refetch across library, details, For You, and Related after successful mutation (clearing/switching removes old signal and adds new one, failures roll back optimistic state without claiming success, refresh retry never replays mutation); retain existing eligibility (For You excludes Watched and Disliked, Related excludes Disliked, Liked alone excludes neither, None removes only review effect, watchlist unaffected, dead notInterested blend term not repurposed); and normalize review profile metadata preferences as mean per-movie feature incidence and plot vectors as equal-weight centroids reusing existing normalization, rarity, and finite/dimension checks without inventing signals for missing metadata.

Cited basis: DEV-461 Recommendations and Acceptance sections (recon-DEV-461 lines 160-161, 173-174), existing eligibility/cache/store/normalization maps in recon-DEV-461 sections 1, 3, 5 and recon-DEV-461-2 section 2, and specs/PRODUCT.md section 5 (care item 6).

Rationale: Atomic persistence plus server-side TasteProfile cache invalidation and client-side query invalidation guarantee that the very next For You or Related request reflects review additions, switches, or clears; mean feature incidence and equal-weight centroids keep Q4 affinities scale-invariant across review counts without double-counting candidate exclusion via the unused blend penalty.

Implications: Updates EfMovieRepository/FeedbackTasteProfileInvalidationTests for canonical and legacy review updates, projection/eligibility predicates in EfRecommendationProfileSource and GetForYou/GetRecommendations handlers, normalization in TasteProfileCalculator, and lifecycle tests in movieFeedbackStore.test.tsx and MovieFeedbackActions.test.tsx. Five of twelve grill questions used. No user escalation raised.


---

## Keel Q6/12 — full decision exchange

Question sent by Keel: Approve truthful recommendation explanations as follows? Add at most one personal chip Because you liked X only when a currently Liked source movie has a positive supported match in an enabled review channel, LikedReviewWeight and BlendProfileWeight are enabled, and the net profile contribution after dislikes is positive. Select the contributing liked source deterministically by greatest positive review affinity, ties by movie id; sanitize its title using existing reason sanitation conventions. Existing viewing-history/genre personal chips must be based on the actual baseline evidence, never inferred from liked-only/disliked-only inputs. Keep existing Related structural reasons/fallback; no fabricated positive explanation for negative-only profiles or disabled channels. Keep the Reason DTO kind/text shape. I recommend this because ticket says dynamic chips must still make sense and consider Because you liked X, while recon-DEV-461-2 section 2 shows current personal chips refer only to viewing history/genre. Cost: source provenance carried with profile evidence and focused reason tests; no endpoint/schema beyond Q2.

Patron answer: Approved — emit at most one personal chip "Because you liked {X}" only when a currently Liked source movie has a positive supported match in an enabled review channel, LikedReviewWeight and BlendProfileWeight are enabled, and the net profile contribution after dislikes is positive; select the contributing liked source deterministically by greatest positive review affinity with ties broken by movie id and sanitize its title using existing reason sanitation conventions; base existing viewing-history/genre personal chips strictly on actual baseline evidence (never liked-only or disliked-only inputs); keep existing Related structural reasons/fallback with no fabricated positive explanations for negative-only profiles or disabled channels; and keep the existing Reason DTO kind/text shape.

Cited basis: DEV-461 Recommendations section (recon-DEV-461 line 162), existing reason provenance and non-fabrication tests in recon-DEV-461 line 139 and recon-DEV-461-2 section 2, and specs/PRODUCT.md sections 4 & 5 (care items 4, 6).

Rationale: Grounding "Because you liked X" in actual positive per-source review affinity and positive net profile contribution prevents misleading viewing-history chips for liked-but-unwatched movies and avoids fabricating positive reasons when dislikes dominate or channels are disabled, while keeping the Reason DTO contract unchanged.

Implications: Carries liked-movie source provenance in TasteProfile/ProfileSimilarityCalculator evidence, updates reason builder logic and unit/integration tests in Features/Recommendations, and logs the chip copy template and tie-breaker [assumed] in specs/DEV-461/ASSUMPTIONS.md. Six of twelve grill questions used. No user escalation raised.


---

## Keel Q7/12 — full decision exchange

Question sent by Keel: Approve this acceptance/verification strategy? Deterministic unit/property tests cover all three review states, independence, legacy transitions, per-channel bounded affinities, equal-count normalization, signed score contribution, missing data, invalid weights and reason provenance. Handler ranking tests for BOTH For You and Related compare the same controlled candidate pool before/after Like, Dislike, Clear and opposite switches; assert actual relative order changes with MMR controlled, and negative exclusion, not only storage or score helpers. Testcontainers integration tests cover migration of existing marks, reload/DTO projection, canonical/legacy HTTP status matrix and real cache invalidation by warming profile, mutating, then making the next recommendation request. Use deterministic FakeEmbeddingGenerator, not live Ollama. Frontend Vitest/RTL with existing typed MSW handlers covers four icons, keyboard/title-bearing names, tooltips/focus/active state, no card navigation, mutual exclusion, shared details/card state, pending protection, rollback/retry and watchlist relocation. Contract checks regenerate OpenAPI/Verify baseline/generated TS and mocks via tooling, never hand-edit generated files. Later gates are existing backend verify/mutation and web-implement gates using harness configuration without threshold changes; report skips/unrunnable honestly. No implementation tests run at this planning phase. Cost: meaningful regression coverage across storage, ranking, cache and UI. Basis ticket acceptance and recon test/tool maps; constitution IX and contract rules cited in recon-DEV-461-2.

Patron answer: Approved — deterministic unit and FsCheck property tests for review states, independence, legacy transitions, bounded channel affinities, equal-count normalization, signed contribution, missing data, invalid weights, and reason provenance; handler ranking tests for both For You and Related asserting actual relative order changes (with MMR controlled) and negative exclusion before/after Like, Dislike, Clear, and opposite switches; Testcontainers integration tests with deterministic FakeEmbeddingGenerator (not live Ollama) covering migration, reload/DTO projection, canonical/legacy HTTP status matrices, and warm-mutate-next-request cache invalidation; Vitest/RTL frontend tests with existing typed MSW handlers covering the four icons, keyboard/title-bearing accessible names, tooltips/focus/active state, navigation isolation, mutual exclusion, shared card/details state, pending protection, rollback/retry, and watchlist relocation; tooling-driven OpenAPI/Verify/TS/MSW contract regeneration without hand-editing generated files; and unmodified backend/web verification gates at implementation time.

Cited basis: DEV-461 Acceptance criteria (recon-DEV-461 lines 172-176), existing test suites and OpenAPI/MSW generation pipeline (recon-DEV-461 sections 4-5, recon-DEV-461-2 sections 3-4, ADR-0007, ADR-0025), Constitution IX, AGENTS.md non-negotiables, and specs/PRODUCT.md sections 1 & 5.

Rationale: Testing actual relative ranking changes at the handler level, real DB migration and cache invalidation via Testcontainers + FakeEmbeddingGenerator, and UI interactions via typed MSW handlers directly proves every ticket acceptance criterion while respecting repository non-negotiables on generated contracts and gate integrity.

Implications: Defines the test and gate matrix for spec.md, plan.md, and tasks.md across LamuFlix.UnitTests, LamuFlix.IntegrationTests, and web/src. Seven of twelve grill questions used. No user escalation raised.


---

## Keel Q8/12 — full decision exchange

Question sent by Keel: Approve the bounded implementation plan and documentation decisions? Order: (1) glossary/domain review invariant, migration/backfill and repository projections; (2) canonical review commands/handler/endpoint plus legacy adapter and DTOs, regenerate/verify contract; (3) profile/source/evidence/scoring/options and truthful reason changes with ranking/cache tests; (4) shared icon row and feedback/watchlist clients/stores/callers, typed fixtures and MSW tests; (5) backend/web gates and live acceptance evidence. Existing change surface is Movie/MovieRecord/MovieConfiguration/migrations, Features/Feedback, existing recommendation types/calculators/handlers/options, EfMovieRepository/EfRecommendationProfileSource/HybridCachedRecommendationProfileSource as necessary, FeedbackEndpoints/HandlerRegistration/LibraryResponses/RecommendedMovieDto and projection mappings, contract generator outputs/snapshot, MovieFeedbackActions/MovieCard/MovieDetailsView/watchlist/movieFeedback/movieFeedbackStore/icons and associated CSS/tests/fixtures. New files only within those existing feature/test/migration folders; no new dependency/project/layer, LocalPlay/secrets/process work, list-row redesign, recommendation engine replacement or unrelated cleanup. Explicitly approve these file edits under care item 6. Canonical glossary terms: Review = explicit opinion None/Liked/Disliked independent of watching and watchlist intent; Feedback includes explicit Watched and Review actions; Taste Profile aggregates viewing history and explicit review preferences. Update existing glossary definitions in the delivery PR per constitution VIII; Keel records exact terms in brief within seat write boundaries. I will draft docs/adr/0034-explicit-movie-reviews.md for the durable review/migration and separate-profile tradeoff. Palette finalizes visual detail before plan challenge; Quill writes spec/plan/tasks from brief, Keel does not. Cost: coordinated contract and profile changes, no broader redesign. Basis Q1-Q7 and both recon notes.

Patron answer: Approved — 5-step bounded implementation order ((1) glossary/domain review invariant, migration/backfill, repository projections; (2) canonical review command/handler/endpoint, legacy adapter, DTOs, contract regeneration/verification; (3) profile/source/evidence/scoring/options and truthful reason changes with ranking/cache tests; (4) shared icon row, feedback/watchlist clients/stores/callers, typed fixtures, MSW tests; (5) backend/web gates and live acceptance evidence); explicit approval under care item 6 for the named existing change surface (Movie/MovieRecord/MovieConfiguration/migrations, Features/Feedback, recommendation types/calculators/handlers/options, EfMovieRepository/EfRecommendationProfileSource/HybridCachedRecommendationProfileSource, FeedbackEndpoints/HandlerRegistration/LibraryResponses/RecommendedMovieDto and projection mappings, contract generator outputs/snapshot, MovieFeedbackActions/MovieCard/MovieDetailsView/watchlist/movieFeedback/movieFeedbackStore/icons and associated CSS/tests/fixtures, plus new files only within those existing feature/test/migration folders) with no new dependency/project/layer, no LocalPlay/secrets/process work, no list-row redesign, and no unrelated cleanup; canonical glossary definitions (Review, Feedback, Taste Profile) recorded in brief.md and updated in the delivery PR per Constitution VIII; docs/adr/0034-explicit-movie-reviews.md drafted by Keel; visual details finalized by Palette before plan challenge and spec/plan/tasks authored by Quill from brief.md.

Cited basis: DEV-461 Scope, Recommendations, and Acceptance (recon-DEV-461 lines 146-178), ADR numbering and file maps in recon-DEV-461 sections 1-6 & lines 182-189 and recon-DEV-461-2 sections 1-4, Q1-Q7 rulings, Constitution VIII, and specs/PRODUCT.md sections 2 & 5 (care items 1-6).

Rationale: Sequencing domain/migration -> API/contract -> recommendation scoring/reasons -> UI/MSW -> verification gates keeps each layer compiling and testable against generated contracts while bounding edits strictly to the files required by DEV-461.

Implications: Completes the architectural, scope, glossary, and ADR alignment for DEV-461; Keel can finalize docs/adr/0034-explicit-movie-reviews.md and specs/DEV-461/brief.md and close the Phase 2 grill. Eight of twelve grill questions used. No user escalation raised.


---

## Keel Q9/12 — full decision exchange

Question sent by Keel: Approve final exact icon copy to remove the alternatives left in Q3 assumptions? Accessible names and tooltips both use action plus comma plus title: Watched toggle Mark Watched / Unmark Watched; thumbs up Like / Clear liked review; thumbs down Dislike / Clear disliked review; Watchlist Add to Watchlist / Remove from Watchlist. Keep aria-pressed conveying current state independently of action wording. Recommend explicit clear-action labels because users must understand what activating a pressed thumb does; cost is updating the original button-label tests. Log these exact values [assumed] and append a superseding clarification rather than overwrite earlier decision exchanges. Also correct file references as a factual citation clarification from recon-DEV-461-2 section 1: existing component is WatchlistToggleButton in web/src/library/watchlist.tsx, card/watchlist styling is web/src/library/library.css; earlier WatchlistButton.tsx/MovieCard.css names are conceptual shorthand, not new files to create. Existing glossary location should be carried exactly as recon reports; Keel writes only specs and docs/adr.

Patron answer: Approved — exact accessible names and tooltips both use "{action}, {title}" with no alternatives: Watched toggle "Mark Watched, {title}" / "Unmark Watched, {title}"; Thumbs up "Like, {title}" / "Clear liked review, {title}"; Thumbs down "Dislike, {title}" / "Clear disliked review, {title}"; Watchlist "Add to Watchlist, {title}" / "Remove from Watchlist, {title}", with aria-pressed conveying current state independently of action wording; superseding [assumed] clarification appended to specs/DEV-461/ASSUMPTIONS.md without overwriting earlier exchanges; and factual file-path clarification recorded under care item 6 that the existing watchlist component is WatchlistToggleButton in web/src/library/watchlist.tsx, card/watchlist styling is web/src/library/library.css (earlier WatchlistButton.tsx/MovieCard.css references were conceptual shorthand, not new files to create), and existing glossary location is carried as recon reports while Keel writes only under specs/ and docs/adr/.

Cited basis: DEV-461 Scope and Acceptance (recon-DEV-461 lines 153, 175), actual file and glossary paths in recon-DEV-461-2 sections 1 & 4, and specs/PRODUCT.md sections 4 & 5 (care item 6).

Rationale: Locking one exact action-oriented string per icon state eliminates ambiguity for MSW/RTL assertions and informs assistive-technology and tooltip users what activating a pressed toggle will do, while recording exact tracked file paths prevents creating spurious CSS/TSX files.

Implications: Updates button-label expectations in MovieFeedbackActions.test.tsx, MovieCard.test.tsx, MovieDetailsView.test.tsx, and RelatedMovies.test.tsx; edits target web/src/library/watchlist.tsx and web/src/library/library.css rather than non-existent shorthand paths. Nine of twelve grill questions used. No user escalation raised.


---

## Keel Q10/12 — full decision exchange

Question sent by Keel: Approve these loop terms and confirm shared understanding so I can close the grill and write brief.md? Closing bar: Critical/High/Medium findings block, Low findings are recorded follow-ups; required gates/acceptance evidence and all three Sentry/Ledger/Compass axis reports must be present, with every blocker explicitly closed. A missing axis is blocked, never skipped. Two rounds maximum per analysis/review-remediation loop; unresolved blockers at cap are reported to Bernstein, never waived or silently added as a third round. Frozen scope: Q1-Q9 canonical independent three-state review, migration/legacy compatibility and agreed API/DTOs; accessible shared four-icon row and bookmark relocation; separate review plot/genre/cast/director affinities in both recommendation handlers, cache freshness, truthful reasons, configuration/validation, glossary/ADR, and agreed regression/contract/acceptance coverage; anything else is a follow-up issue, not a finding in this round. No ticket contradiction or constitution departure has been proposed, so no owner checkbox is currently required; if one later appears, Gate 1 stays closed until owner decision. This closes grill only, not plan challenge/freeze, Gate 1, implementation, or merge clearance. Quill drafts spec/plan/tasks from brief; Palette visual work precedes challenge. I recommend this bounded bar to preserve ticket delivery and catch real blockers without expanding scope. Cost: cap exhaustion stops progression, Low items need tracking.

Patron answer: Approved — confirmed shared understanding and approved the closing loop terms: Critical/High/Medium findings block, Low findings are recorded follow-ups; required gates/acceptance evidence and all three Sentry/Ledger/Compass axis reports must be present with every blocker explicitly closed (missing axis is blocked, never skipped); two rounds maximum per analysis/review-remediation loop with unresolved blockers at cap reported to Bernstein (never waived or silently given a third round); scope frozen to Q1-Q9 (canonical independent three-state review, migration/legacy compatibility and agreed API/DTOs, accessible shared four-icon row and bookmark relocation, separate review plot/genre/cast/director affinities in both recommendation handlers, cache freshness, truthful reasons, configuration/validation, glossary/ADR 0034, and agreed regression/contract/acceptance coverage; anything else is a follow-up); no owner checkbox required (if a ticket contradiction or constitution departure later appears, Gate 1 stays closed until owner decision); closes Phase 2 grill only so Keel can write brief.md and docs/adr/0034-explicit-movie-reviews.md, followed by Palette visual work, Quill spec/plan/tasks authoring, and plan challenge.

Cited basis: DEV-461 ticket scope/acceptance (recon-DEV-461 lines 134-178), rulings Q1-Q9 in specs/DEV-461/CONCLUSIONS.md and specs/DEV-461/ASSUMPTIONS.md, and specs/PRODUCT.md sections 2-5.

Rationale: Freezing scope to Q1-Q9 and enforcing the standing Critical/High/Medium blocker bar, three-axis review requirement, and two-round cap protects ticket delivery and gate integrity without scope creep.

Implications: Phase 2 grill is complete at 10/12 questions used with zero user escalations; Patron commits specs/DEV-461/CONCLUSIONS.md and specs/DEV-461/ASSUMPTIONS.md on feature/461-spec, and Keel closes the grill, writes specs/DEV-461/brief.md and docs/adr/0034-explicit-movie-reviews.md, and reports grill completion to Bernstein.


## Phase 2 step 6 — challenge decision addendum

Patron approved this bounded clarification after recon-DEV-461-3, recorded in CONCLUSIONS.md:214-219 and ASSUMPTIONS.md, commit 9e3fdc1663d53b4fedcdf88c201a5fdcd56d4c71. This is a challenge repair decision, not an additional grill question or a restarted analysis loop. The original Q1-Q10 history remains intact.

Question sent by Keel: Approve the five-part bounded addendum: approved SmartEnum with lowercase Name values and EF Value conversion; raw-string request validation preserving the 400/422 boundary; incumbent post-commit cache invalidation/failure semantics with generation freshness proofs; removal of the legacy persisted column with an explicit lossy Down; proposed ADR until Gate 1 acceptance and no owner checkbox unless a real ticket contradiction or necessary constitution departure is identified. Preserve frozen API shape, use existing seams, and record cited rulings before Quill/Palette repair.

Patron answer and cited basis (full recorded ruling):
Verdict: Approved — adopt the 5-part bounded plan-adjudication addendum ((1) Review SmartEnum with literal lowercase Name values none/liked/disliked, CLR identifiers None/Liked/Disliked, EF Value converter, TryFromName, zero new packages/enums; (2) raw-string request binding + Infrastructure FluentValidation in ValidationDecorator with 400 malformed/unbindable body/route, 422 missing/null/whitespace/unknown/wrong-case review or id <= 0, 404 unknown movie, 204 repeat valid assignment, response strings = Review.Name; (3) incumbent EfMovieRepository post-commit generation-bump-then-tag-remove invalidation for canonical/legacy writes, 500 on post-commit invalidation failure with DB write retained and provisional UI rollback reconciled by refresh/idempotent retry, failed-remove-then-read + controlled-pool freshness proofs, no new port/retry/distributed guarantee; (4) drop legacy IsNotInterested column/mapping in same backfill migration with lossy Down Disliked->true and None|Liked->false while keeping public legacy routes/DTOs as projections; (5) keep ADR 0034 Proposed until Gate 1 acceptance with zero owner checkboxes, plus shipped-theme >=4.5:1 contrast evidence in design.md and narrow-viewport flex-wrap/Q9 casing/glossary disambiguation in ASSUMPTIONS.md).

- Constitution & Enumeration/Validation/Wire rules (recon-DEV-461-3 sections 1-2, .specify/memory/constitution.md:290, 320, 391-406, 525-526, 551-552, 604-615, 670): mandates Ardalis.SmartEnum (already in LamuFlix.Core.csproj:14) persisted by Value and serialized on the wire by Name (none|liked|disliked), TryFromName(..., false), FluentValidation in ValidationDecorator returning 422 via GlobalExceptionHandler, and no incoming request enum converter that would turn unknown names into 400.
- Incumbent cache/repository seam & failure proofs (recon-DEV-461-3 section 3, EfMovieRepository.cs:199-225, HybridCachedRecommendationProfileSource.cs:43-109, FeedbackTasteProfileInvalidationTests.cs:113,143, EnrichmentStatusNotificationPersistenceTests.cs:397-419, HybridCachedRecommendationProfileSourceFreshnessTests.cs:87, ArchitectureTests.cs:189-213): proves post-commit generation-bump-then-tag-remove is the sole permitted invalidation seam, post-commit invalidation errors propagate 500 with committed DB write retained (204 requires completed invalidation), generation bump rejects stale in-flight or failed-remove entries on next read, and controlled-pool freshness assertions satisfy Sentry M1/M2 and Ledger F3.
- Single-truth migration & governance (recon-DEV-461 lines 150, 166, 173-176; adjudication-DEV-461-Keel lines 8-42; specs/PRODUCT.md sections 2, 4, 5 items 1-6): dropping Movies.IsNotInterested in the backfill migration enforces Review as sole persisted truth with explicit lossy Down (Ledger F4/Sentry L1/Compass F2), keeps ADR 0034 Proposed until Gate 1 approval (Ledger F6), requires concrete >=4.5:1 contrast ratios in design.md (Sentry L4), logs narrow-overflow flex-wrap/Q9 casing/glossary disambiguation in specs/DEV-461/ASSUMPTIONS.md (Compass F4/Ledger F7), and raises zero owner checkboxes.


Implementation implications and rationale:

- Use the approved Ardalis.SmartEnum dependency already referenced by Core; static CLR identifiers may be None/Liked/Disliked, while Name is exactly none/liked/disliked. Parse exact names with TryFromName; persist Value through an EF converter. Responses expose Name strings, never numeric values. Do not add a new package reference or use a project-owned C# enum.
- Bind review as a raw string so a syntactically valid unknown name reaches Infrastructure FluentValidation through the existing ValidationDecorator instead of failing JSON enum conversion. Missing/null/whitespace/unknown/wrong-case review and nonpositive id return 422; malformed/unbindable JSON/body or route syntax returns 400; unknown movie 404; valid repeated assignment 204. Cover these boundaries and use the central exception mapper without exception-text leakage. New validators stay in existing Infrastructure validation directories and are hand-wired through existing registration.
- Both canonical and legacy review writes use existing EfMovieRepository persistence followed by generation increment then tag removal. No Feedback-to-Recommendations Core dependency, new port, retry queue, background repair, or distributed guarantee. Completed invalidation is required before 204. A post-commit invalidation failure propagates 500 while preserving the committed write, as existing tests require. Provisional UI rollback must not assert that the database rolled back; authoritative refresh and canonical idempotent retry reconcile state. Preserve existing genre-cache invalidation and notification behavior.
- Test failed RemoveByTagAsync followed by a read using the real cached adapter, retain generation rejection for invalidation during an in-flight fill, and prove canonical/legacy review freshness with controlled candidate pools including clear/switch. Fresh state is required; arbitrary mutations need not universally perturb ordering. Do not claim the failed-remove/read case is already covered by incumbent tests: recon identifies it as a gap.
- Backfill Review, then remove the IsNotInterested mapping/column in that migration. Down recreates the boolean from Disliked only; None and Liked map false, so positive-review information is explicitly lost on downgrade. Public legacy routes/DTO fields remain projections. Test historical backfill, reload, sole persisted opinion truth, and downgrade mapping.
- ADR 0034 remains Proposed until Gate 1 acceptance; record the status transition as an explicit owner/Keel lifecycle action. There is no ticket contradiction or constitution departure and no owner checkbox.
- Palette records concrete shipped-theme contrast ratios meeting the ruled >=4.5:1 target. Narrow viewports may flex-wrap to prevent overflow while preserving Q9 order and target sizes; exact Q9 casing is content, not a transform. Only the thumbs pair is mutually exclusive; Review None leaves Watched/Watchlist pressed state independent. Review vocabulary is distinguished from external rating and code review, per the appended assumptions.

Next action: Quill repairs spec/plan/tasks from this addendum and the single consolidated challenge fix list; Palette repairs design.md and supplies contrast evidence. Keel independently reads back that bounded repair round before considering PLAN FROZEN. The completed two-round speckit-analyze loop is not reopened. Gate 1 approval, user-owned merge, pickup drift, implementation and verification remain separate.
