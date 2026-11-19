# DEV-541 Phase 2 brief

**PLAN FROZEN** · Phase 2 step 6 complete. Gate 1 and runtime acceptance are pending.

**Grill closed with Patron on Q1–Q10.** Size M. Worktree `F:/Dev/LamuFlix.worktrees/feature-541-spec`, branch `feature/541-spec`. Scope and evidence come from DEV-541, `recon-DEV-541`, and the recorded Patron rulings. [Plan challenge dispositions](PLAN_ADJUDICATION.md) record the three-axis evidence and the decisions below.

## Scope and review terms

Deliver deterministic For-You recommendations with seed and collection caps, release-order collection eligibility, stable offset paging, movie ID exclusions, and a successful GET response of `{ items, hasMore }`. Include the API contract, generated web contract, mock, compatibility edits needed by existing callers, and focused verification.

DEV-517 load-more and swap, DEV-538 reason grouping, DEV-542 card layout and text, and DEV-543 seed weights, threshold, and formula are out of scope. Anything else is a follow-up issue, not a finding in this round.

- Closing bar for code-review and ship-review requires Risk, Standards, and Spec reports, closure of every accepted in-scope finding including Low, explicit rationale for other dispositions, and all required gates meeting the criteria in `harness.yml`.
- Two review rounds maximum. Route any third-round findings to user review on the PR.

## Ranking and paging contract

- Use the existing eligibility and strong-match bar. Exclude the supplied movie IDs before the collection cut and MMR. Unknown valid IDs have no effect.
- For each collection, order its remaining eligible unwatched candidates by ascending release year, null last, then ascending movie ID. Keep the earliest `MaxPerCollection` members before MMR. Excluding a member can advance the next eligible member into that set. Seed saturation during MMR never backfills the set with a later member.
- Run MMR across the remaining candidate list, preserving ascending movie ID as the MMR score tie-break. Each selected movie consumes one slot for its best-seed ID (default cap 3). Each selected movie with a non-null collection consumes one slot for that collection (default cap 1). Skip a candidate when either applicable cap is full. Count selections throughout the ranking before paging.
- Use `CandidatePoolSize` to validate `take`. The For-You MMR input is the full eligible list after exclusions and the collection cut. Identical ranking inputs produce one stable ranking. Paging with a changed exclusion set starts at offset zero.
- Return the ranking slice `[offset, offset + take)`. Set `hasMore` only when a selected result exists at index `offset + take`. If offset reaches the post-cut candidate count, return an empty page with `hasMore: false` before MMR. Otherwise compute the one-extra prefix in widened arithmetic, clamp it to the post-cut count, and preserve full-ranking semantics.

## API and contract chain

- Keep existing `take` validation. At the HTTP boundary accept one nonnegative invariant-culture integer `offset` (default zero, no additional maximum). Malformed, repeated, negative and overflowing values return the existing 400 ProblemDetails with `traceId`. Retain the nonnegative invariant in the query validator.
- Accept zero through `MaxExcludeCount` raw repeated query values, such as `exclude=1&exclude=2`. Use `NumberStyles.Integer` and `CultureInfo.InvariantCulture` before `MovieId.TryCreate`. Count raw values at the HTTP boundary before deduplication. Return 400 with static messages for invalid or excess values. The query validator checks normalized inputs. The default raw limit is 50, matching the default `CandidatePoolSize`. Keep constant defaults and data-annotation/startup validation. Both ranking caps have a minimum of one, and the raw exclusion limit has a minimum of zero.
- Return the page envelope on For-You. The movie-specific recommendation endpoint keeps its bare-list contract. Use dedicated `ForYouRecommendationsPage.cs` and `ForYouRecommendationsPageDto.cs` files in the existing Core recommendation and API endpoint folders. Flow the API DTO through OpenAPI into generated TypeScript. Document `exclude` as a form-style exploded integer array.
- Caller follow-up recon confirmed the producer, endpoint tests, and generated client chain. Sentry F1 and Compass F3 established a baseline later. Wisp confirms it and records the pickup delta in T001. Under Q6 and charter §2.3 item 6, adapt `web/src/features/for-you/forYouApi.ts`, `forYouApi.test.ts`, `ForYouPage.test.tsx` and `web/src/mocks/forYou.test.ts` as required by the envelope. Verify `ForYouPage.tsx` against the preserved array-returning hook. Keep its take-only input and existing query key. The hook unwraps `data.items` and leaves `hasMore` for DEV-517. Apply the same mechanical scope to any pickup delta.

## Task order and file boundaries

1. Confirm the challenge's caller baseline against pickup HEAD and record exact paths and deltas.
2. Update typed `RecommendationOptions`, the For-You query and handler, and the collection data supplied through `MovieFeaturesMapper`. Implement the capped `MmrSelector` selection with focused query, validator, handler, and selector unit coverage, using the existing test files identified in intake recon.
3. Update `RecommendationEndpoints.cs`, `GetForYouRecommendationsQueryValidator.cs`, and the For-You-specific API DTO. Verify dispatch and actual wrapped HTTP JSON in `RecommendationEndpointTests.cs`.
4. Update `RecommendationsOperationTransformer.cs` and review the diff of the refreshed `OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` snapshot. Regenerate `web/src/api/openapi.json`, `web/src/api/types.ts` and `web/src/mocks/handlers.ts`. Migrate the `forYouRecommendationsData` fixture in `web/src/mocks/fixtures.ts` using the generated page schema, then adapt confirmed callers and tests.
5. Run focused unit and endpoint integration tests, OpenAPI snapshot and generated contract checks, the existing web build/test gate, and the applicable Size M backend gates. Record each gate as reported.

## Acceptance evidence

Cover deterministic movie-ID ties and stable, disjoint slices across the same ranking. Cover option minima and default caps, standalone movies, release-year order including nulls and ties, exclusion-driven promotion, and skipped collection representatives under seed saturation. Cover exclusions with duplicates and unknown IDs, malformed and excess raw inputs, defaults and request dispatch, wrapped JSON, empty and beyond-end pages, and `hasMore` on both sides of an exact page boundary. Prove bounded-prefix equivalence and that a huge valid offset skips MMR and returns an empty HTTP page. Verify a generated-client request with two exclude IDs serializes as repeated keys and returns 200 with both known eligible IDs filtered. Check static ProblemDetails messages and `traceId`.

## Patron rulings


### Q1. Stable paging and exclusions

> [from Patron] Confirmed: stable paging is a slice (offset, take) of one deterministic full-ranking capped MMR list (ascending movie-ID tie-break, caps applied inside the selection loop over the whole ranking), with exclude filtered before MMR/caps and hasMore true iff at least one cap-eligible result exists past offset + take (DEV-541 Wanted 2, 4, 5, 7; Open points: Caps).


### Q2. Seed and collection eligibility

> [from Patron] Confirmed: each selected movie consumes one slot from its best-seed ID (default cap 3) and, only when CollectionId is non-null, one slot from that collection (default cap 1, where only the earliest eligible unwatched collection member by release year, null year last, tie-break lower movie ID, may take the collection slot); candidates with a full applicable cap are skipped inside the MMR loop across the whole ranking before slicing (DEV-541 Wanted 2-3; Open points: Caps, Collection rule).


### Q3. Collection ordering and promotion

> [from Patron] Confirmed: order each collection's eligible, exclusion-filtered unwatched candidates (already clearing the strong-match bar) by release year ascending, null year last, tie-break lower movie ID, and keep only the earliest MaxPerCollection (default 1) member(s) before MMR; an excluded member is removed before that cut so the next eligible member advances, whereas a kept representative skipped inside MMR because its best-seed cap is full is never backfilled by a later collection member (DEV-541 Wanted 2-3; Open points: Collection rule).


### Q4. HTTP input validation

> [from Patron] Confirmed: offset defaults to 0 and must be a single integer >= 0 (no upper bound, parsed with take's CultureInfo.InvariantCulture / BadHttpRequestException 400 pattern and validated in GetForYouRecommendationsQueryValidator); exclude accepts 0..MaxExcludeCount raw repeated query values (exclude=1&exclude=2, default option MaxExcludeCount = 50 matching CandidatePoolSize), where each raw entry must parse as a valid positive MovieId (> 0 via MovieId.TryCreate) or fail with 400 before deduplicating for ranking and silently ignoring unknown library IDs (DEV-541 Wanted 5, Done when, Open points; RecommendationEndpoints.cs:97-119, MovieId.cs:20-32).


### Q5. Candidate pool and ranking horizon

> [from Patron] Correction from ticket text (DEV-541 Problem 4, Wanted 7; GetForYouRecommendationsQueryHandler.cs:65-83): CandidatePoolSize is NOT an MMR input ceiling in For-You (only an upper bound on take). The handler scores all eligible movies clearing the strong-match bar, applies exclude and the collection release-year cut, runs capped MMR across that full remaining candidate list (selecting up to offset + take + 1 items is sufficient because MMR is prefix-stable), slices [offset, offset + take), and sets hasMore when a selected item exists at index offset + take.


### Q6. Contract migration and caller recon

> [from Patron] Confirmed: update the contract chain (OpenAPI snapshot, web/src/api/openapi.json, web/src/api/types.ts), mock fixture/handler (web/src/mocks/fixtures.ts forYouRecommendationsData where handlers.ts:46 sources success.GetForYouRecommendations), and mechanically adapt any existing web callers and tests consuming the bare For-You array to unwrap response.items so the web build/test gate stays green without adding DEV-517 UI behavior (§2.3 item 6 forced edit; DEV-541 Wanted 6, Done when, Out of scope).


### Q7. Scope and task ordering

> [from Patron] Confirmed: freeze DEV-541 scope and dependency order as stated (caller recon -> Core options/ranking/caps/unit tests -> API query/validation/page DTO/HTTP tests -> OpenAPI snapshot and generated web contract/mocks/mechanical caller unwrap -> verification), keeping DEV-517, DEV-538, DEV-542, and DEV-543 out of scope (DEV-541 Wanted, Out of scope, Where it lands, Needs).


### Q8. Acceptance and gate evidence

> [from Patron] Confirmed: adopt that acceptance evidence and Size M verification matrix, including the overflow-safe large-offset regression test (e.g. offset near int.MaxValue so slicing/hasMore does not overflow offset + take + 1), OpenAPI snapshot/contract verification, existing web build/test gate, and standard backend gates (DEV-541 Done when; AGENTS.md Verification commands).


### Q9. Review closing bar

> [from Patron] Confirmed: adopt the standard Size M closing bar (Risk, Standards, and Spec review reports; every accepted in-scope finding closed at all severities including Low; explicit rationale recorded for any non-accepted or out-of-scope finding; all required gates meeting their configured criteria in harness.yml).


### Q10. Review round cap and grill closure

> [from Patron] Confirmed: adopt the task-pipeline two-round review cap (any third-round findings route to user review on the PR), and the DEV-541 grill is closed on Q1-Q10.

