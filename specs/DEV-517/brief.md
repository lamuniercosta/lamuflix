# DEV-517 | For You load more and suggestion replacement

**PLAN FROZEN.** Patron closed the Q1–Q8 grill and ruled on the round-one refinements in `CONCLUSIONS.md`. Size M, branch `feature/517-spec`, worktree `F:/Dev/LamuFlix.worktrees/feature-517-spec`. Intake recon is pinned to `1bbc8a637b97ec2efdf0eaa6f2be6da8abbbc089`. Gate 1, implementation, runtime acceptance and merge clearance remain pending.

## Frozen scope

- Add **Load more** to the For You page. It appends the next available suggestions to the existing grid without a page reload.
- Offer replacement on each For You recommendation card. A successful replacement changes that slot to the next unseen suggestion and retires the old movie ID for the remainder of the mounted visit. Leaving For You or reloading the page starts a new visit.
- Use the existing DEV-541 endpoint and generated types for `take`, `offset`, `exclude`, `items` and `hasMore`. The existing ranking and pagination contract is the sole server mechanism. File scope is the three web implementation files and two web test files listed below. This brief authorizes focused edits within those files and uses the existing frontend stack and API contract.
- Delivery requires the running-app e2e spec, proof that a replaced movie stays absent during the same visit, and screenshots of both controls on the PR. Anything else is a follow-up issue, not a finding in this round.

## Approach and state transitions

1. `ForYouPage` owns the mounted visit and seeds it once from a typed, uncached initial slice at offset zero. Keep the ordered grid, exclusions and cursor after initial commit across incidental focus/invalidation/refetch and action retry. A fresh mount starts a fresh initial request; initial failure may be retried. Keep the existing `useForYou`, `forYouQueryKey`, `FOR_YOU_TAKE` and `ForYouError` exports compatible. Add direct typed slice access through the existing client; initial/append/replace requests for this page bypass React Query’s cache and use their explicit `take`, `offset` and `exclude`.
2. Load more consumes eligible buffered rows from the committed exclusion set, then fetches consecutive slices until it has `take` unseen items or the endpoint reports `hasMore=false`. Advance by requested `take` per fetch and skip rendered, retired and already staged IDs. Commit the append and next cursor together in rank order. An empty terminal append leaves the grid in place and removes Load more when there are no eligible buffered rows; a failed multi-slice append rolls back all staged progress.
3. Start replacement at offset zero and scan in ranking order for the first unseen candidate, replacing the target in its existing slot. Use a deduplicated set for `exclude`: committed IDs only for initial/append and the committed set plus only the target for replacement. On swap success, replace the previous offset, buffer and `hasMore` together with the exclusion set and card, retaining eligible unused rows only from the successful replacement scan. Failure or true exhaustion preserves the previous committed scan; failures use static client-owned feedback and manual retry, including HTTP 400, without interpolating server error content.
4. One in-flight action per visit. Own an AbortController and visit identity in the page, pass the visit signal through every slice fetch, abort on cleanup, and check the identity before further requests and before any state commit. Scans advance by `take` and stop for their result, endpoint exhaustion, request failure or visit cancellation. Visit logic needs no timers or randomness. A truly exhausted replacement reports concise non-error status. Load more remains usable under the committed state when candidates remain.
5. Q4 governs the exclusion ceiling. The server enforces `MaxExcludeCount`. Use the ordinary replacement failure path for a rejected request, including HTTP 400. Keep committed exclusions and the pre-attempt grid, offset, buffer and `hasMore`. Frontend cap consumption and a dedicated limit discriminator were not established within the authorized recon roots. The plan requires neither a numeric client threshold nor error-message parsing or an API expansion.

## Files

| File | Planned change |
| --- | --- |
| `web/src/features/for-you/ForYouPage.tsx` | Own the mounted visit and its actions, render Load more, supply the per-card action and display pending, error and exhaustion states. |
| `web/src/features/for-you/forYouApi.ts` | Provide typed access to DEV-541 slices through the existing API client. Preserve `items` and `hasMore` for the feature. |
| `web/src/library/MovieCard.tsx` | Render the optional stateless action outside navigation and play affordances, with existing card action styles and accessible naming. |
| `web/src/features/for-you/ForYouPage.test.tsx` | Extend component coverage for payloads, visible behavior and visit state transitions. |
| `web/e2e/for-you-review.e2e.ts` | Extend the existing browser spec to drive both controls against the running app. |

The typed client in `web/src/api/client.ts` and generated contract in `web/src/api/types.ts` are read-only inputs to this change. Button wording, the placement within the card and status presentation are taste decisions recorded in [ASSUMPTIONS.md](ASSUMPTIONS.md).

## Test and task order

1. Quill drafts `spec.md`, `plan.md` and `tasks.md` from this brief. Run cross-artifact analysis and plan challenge before implementation approval. Use Palette's design for the plan challenge where the UI workflow requires it.
2. At pickup, confirm DEV-541 and the initial For You path on the intended base, including repeated-key `exclude=ID1&exclude=ID2` on the wire. Inventory existing For You API export consumers. Report drift before edits and route upstream corrections through C# DTO → OpenAPI → generated TypeScript.
3. Record local red component tests before production wiring. Exercise initial states, wire payloads and offsets, duplicate-only slices, buffer validity, rollback and retry, mounted-visit stability and cancellation. Cover the actual shared card with and without the action and check navigation/play isolation. Run the existing shared-card regression suites.
4. Wire the existing typed slice requests and visit state, then connect the page and optional card controls with targeted tests. Exercise Load more after a swap and after a failed swap. Check the shared card path without the optional action.
5. Run applicable web gates and the running-app e2e separately. Use the existing running-app Playwright spec and its supported data setup. Give the DEV-517 journey isolated or reset library/feedback state; it must run independently of the DEV-541 like test and must preserve that test’s fresh-start assumption. Preflight against the live endpoint with the scenario's take and initial exclusions: require at least `2 × take + 2` distinct ranked candidates for the initial page, full append, replacement and follow-on. Also verify the planned target’s exclusion leaves at least two unseen candidates outside the initial and appended grid. Fail setup explicitly with observed and required counts. Keep setup in the existing e2e file and record the reset/seed procedure. Append without reloading, replace in place, perform a further action in that visit and prove the retired ID stays absent. Capture and publish screenshots of both controls on the delivery PR.

## Closing bar and round limits

- The grill used **8 of 12 questions**. Plan challenge and implementation review each have a **two-round cap**.
- Size M review requires Risk/Sentry, Standards/Ledger and Spec/Compass reports. Close every in-scope Critical, High or Medium finding and provide evidence at the reviewed HEAD for every accepted finding, including Low. Give each other disposition an explicit cited rationale. Return any blocker still open at the cap to the Conductor and record its disposition on the PR.
- Delivery evidence requires green applicable web gates, the running-app e2e receipt and PR screenshots of both controls. Report C# gates returning exit 2 for empty scope as `SKIPPED (scope-empty)`. The owner retains merge authority.

## Plan challenge

See [PLAN_ADJUDICATION.md](PLAN_ADJUDICATION.md) for the 25 round-one dispositions and their implementation tasks. See the addendum to [CONCLUSIONS.md](CONCLUSIONS.md) for Patron’s rulings.

## Grill record

The complete Q1–Q8 exchanges follow. Q4 supersedes the client-cap detection, numeric limit and limit status in Q3.


---

## Q1. Visit boundary

**Keel.** What is a same visit for the exclusion guarantee? I recommend the continuous mounted For you page visit, preserving exclusions across Load more, swaps, retries and incidental refetches until the user leaves For you or reloads the page. This keeps scope local to a visit and lets a new visit start fresh. Does that boundary fit the ticket?

**Patron.** Approved (continuous mounted For You visit boundary, [assumed]).

**Basis and implications.** The ticket requires that a replaced suggestion stay excluded for the same visit (`recon-DEV-517`, lines 17–20). Keep successfully replaced suggestions excluded throughout the continuous mounted page visit, including subsequent Load more, replacement, retry, and incidental refetch. Leaving For you or reloading starts a new visit.

---

## Q2. Pager and replacement requests

**Keel.** Should we select the next best unseen recommendations by requesting `offset=0` against the excluded union of rendered IDs and previously replaced IDs, then append the returned items for Load more or take one to replace in place? I recommend this because excluding displayed IDs already removes them before offset and a positive offset would skip eligible suggestions. It also gives both actions the same candidate pool. The exclusion budget is a separate decision.

**Patron.** Rejected as proposed — wire exclude must carry only replaced movie IDs (never currently rendered IDs), while offset steps through the DEV-541 ranking for the active exclude set and the client skips already-rendered IDs client-side when a swap restarts the ranking at offset=0.

Basis: (1) Ticket Needs (recon-DEV-517:26,45-49; specs/DEV-517/ticket-text.txt) builds Load more and swap on both offset and exclude without a second paging mechanism; (2) DEV-541 ranking (GetForYouRecommendationsQueryHandler.cs:35-38,75,87-113; specs/DEV-541/spec.md:35-37,100-105) strips exclude before the collection cut and MMR seed/collection caps, so excluding rendered IDs would violate collection/seed caps across the visible grid and burn MaxExcludeCount (50, RecommendationEndpoints.cs:143-153); (3) Slice and swap contract (specs/DEV-541/spec.md:36,100,104): Load more advances offset by take while exclude is unchanged, and when a swap adds a replaced ID to exclude, the ranking restarts at offset=0 and the client scans offset slices for the first item not in renderedIds to replace in place (and continues Load more from that ranking while deduplicating against renderedIds).

**Implications.** Keep the request and progress tied to one exclusion set. A swap starts a new scan at offset zero. Preserve unconsumed candidates from the final slice so a later append can continue without skipping them. The recon confirms the configurable exclusion limit and its validation. The numeric value in the quoted ruling is attributed to Patron.

---

## Q3. Replacement exclusion budget

**Keel.** What should happen when the visit reaches the DEV-541 exclusion budget? I recommend keeping committed exclusions for the whole visit, stopping additional replacements at the supported limit with visible status, and leaving Load more available under the committed set. Dropping old retirements would break the visit guarantee, and an API change would expand the ticket. Please rule whether a replacement ceiling fits the ticket or needs structural escalation. Recon confirms a configurable limit but does not establish how to obtain its value on the client.

**Patron.** Approved without structural escalation — retain all committed exclusions for the visit, stop further replacements at the DEV-541 exclusion ceiling with visible status ([assumed]), and keep Load more active under the committed exclusion set.

Basis: (1) No structural escalation (recon-DEV-517:19,26,36; specs/PRODUCT.md sections 3, 5): ticket Needs explicitly builds swap on DEV-541 exclude (bounded by MaxExcludeCount, default 50 per specs/DEV-541/spec.md:37,120 and RecommendationEndpoints.cs:143-153), and keeping all committed exclusions preserves Done when item 2 (no replaced item returns in the same visit) without altering the API shape; (2) Client limit enforcement without new API (recon-DEV-517:46,54; specs/DEV-541/spec.md:37; [assumed]): disable further replacements and show concise limit status once committed exclusions reach the DEV-541 default 50 (and if a lower server MaxExcludeCount returns HTTP 400 on a swap, roll back only that uncommitted exclusion ID and enter the same limit-reached state); (3) Load more continuity (recon-DEV-517:14,26): Load more reuses the committed exclusion set (<= MaxExcludeCount) and continues paging normally via offset while hasMore is true.

**Implementation evidence pending.** The authorized recon confirms the configured limit and request validation. The client source for the existing default and the response details for identifying a limit rejection still need recon. The quoted ruling attributes the default value to Patron and does not authorize hardcoding a threshold.

---

## Q4. Server-enforced exclusion budget and failed replacements

**Keel.** What contract rule supersedes Q3 on how to detect the exclusion ceiling? I recommend letting the server enforce the threshold, with no duplicated numeric default or new API. Only a documented limit-specific signal may latch a replacement-limit state. If there is no such signal, keep the last committed grid, exclusions and offset after any 400, present retryable replacement error, and leave Load more usable. This lets us specify behavior without assuming a validation payload.

**Patron.** Approved without structural escalation (superseding Q3 client-cap detection) — let the server enforce MaxExcludeCount with no hardcoded client numeric cap or new API, and since the OpenAPI contract exposes no limit-specific signal, roll back the uncommitted exclusion on any failed replacement request, keep the last committed grid, exclusions, and offset/hasMore intact, show a retryable replacement error ([assumed]), and keep Load more usable.

Basis: (1) No hardcoded threshold or API expansion (AGENTS.md Non-negotiables; specs/PRODUCT.md section 5 item 4; recon-DEV-517:26,36,46): avoids duplicating server configuration in web code and avoids adding a new endpoint or DTO field; (2) Contract reality (recon-DEV-517:45-46,54,56): GET /api/recommendations/for-you declares generic 400 ProblemDetails without a typed limit discriminator, so only a documented contract signal could latch a permanent limit state and in its absence a 400 is handled as a non-destructive failed replacement attempt ([assumed]); (3) No structural escalation (recon-DEV-517:14-26; specs/PRODUCT.md sections 3, 5): preserving committed exclusions, grid state, and Load more continuity on failed replacement attempts fulfills all ticket Wanted and Done when requirements.

**Implications.** Q4 supersedes the numeric cap, limit status, and lower-limit detection proposed in Q3. The restricted recon could not verify the generated error declaration (`recon-DEV-517`, lines 58–61). The design requires no typed discriminator and handles all failed replacement attempts through the rollback path.


---

## Q5. Atomic actions and exhausted replacements

**Keel.** Should each visit treat a Load more or replacement as one in-flight operation at a time, with success committed only for that visit and with an exhausted replacement leaving the original card, exclusions and paging intact? I recommend that atomic rule. It prevents overlapping scans from corrupting the grid, prevents stale requests from applying after navigation, and gives a legitimate no-candidate result a non-error status while keeping the original card.

**Patron.** Approved ([assumed] for single in-flight interaction locking and non-error exhaustion notice) — allow at most one in-flight Load more or replacement operation per visit, commit results only to the active visit, and when a replacement finds no unseen candidate, leave the original card, committed exclusions, and paging state intact with a non-error exhaustion status.

Basis: (1) Deterministic ranking integrity (recon-DEV-517:26,47; CONCLUSIONS.md Q2): because a swap changes exclude and restarts ranking from offset=0 while Load more advances offset, allowing only one in-flight operation at a time ([assumed]) prevents racing over replacedIds, renderedIds, and offset; (2) Visit boundary enforcement (recon-DEV-517:19; CONCLUSIONS.md Q1): discarding or aborting in-flight responses across unmount/remount guarantees that state belongs strictly to the active visit; (3) Exhausted replacement semantics (recon-DEV-517:15,48; specs/PRODUCT.md section 4; [assumed]): Wanted item 2 replaces a card with the next best suggestion, so if the server ranking yields no unseen candidate (empty unseen result with HasMore=false), rolling back the candidate exclusion keeps the original card in place and presents a concise non-error exhaustion status ([assumed]).

**Implications.** A replacement commits its card, exclusion and new paging state together. Failed or exhausted replacements leave the previously committed state available for subsequent actions.


---

## Q6. Frontend integration and file scope

**Keel.** Which frontend integration boundary should the plan use? I recommend feature-owned paging, visit state and replacement behavior in the existing For You page/API integration, a minimal optional action slot on the shared MovieCard supplied only by ForYouPage, and focused changes in the recon-identified web test files. The shared card would render the action and carry no For You state or query logic. Use the generated DEV-541 types and the existing client with no production backend changes, new dependency, new layer or rewriting existing files. This keeps the control on each recommendation card and confines behavior to the feature.

**Patron.** Approved ([assumed] for card control placement and label wording) — keep paging, visit exclusion state, and swap orchestration in web/src/features/for-you/ (ForYouPage.tsx and forYouApi.ts), expose only an optional presentation callback/slot on shared web/src/library/MovieCard.tsx supplied solely by ForYouPage, and update the recon-identified web tests with zero backend, dependency, or layer additions.

Basis: (1) Feature boundary and Charter section 2.3 item 6 forced edit (recon-DEV-517:14-15,52-54; specs/PRODUCT.md sections 2, 5 items 1-6): Wanted items 1-2 require a page-level Load more control and a per-card replace control, authorizing focused edits to ForYouPage.tsx, forYouApi.ts, and an optional stateless action prop on shared MovieCard.tsx without affecting non-For-You card callers or rewriting unnamed files; (2) Contract chain compliance (recon-DEV-517:26,54,56; specs/PRODUCT.md section 1): consume generated DEV-541 OpenAPI types via web/src/api/client.ts and web/src/api/types.ts with no manual type edits and no production backend changes; (3) Test and taste scope (recon-DEV-517:18,55; specs/PRODUCT.md section 4; [assumed]): cover unit and e2e behavior in web/src/features/for-you/ForYouPage.test.tsx and web/e2e/for-you-review.e2e.ts, tagging button copy and placement micro-interactions as [assumed].


---

## Q7. Verification and runtime evidence

**Keel.** What verification sequence should the brief require? I recommend confirming the DEV-541 generated contract at pickup, then adding focused ForYouPage tests for paging, deduplication, in-place swap, exhaustion, rollback and visit/race behavior before changing the feature. Follow with the normal web gates, an e2e spec in the existing browser suite against the running app with a library that has enough ranked items, and PR screenshots showing both controls. The e2e should append, replace, then append or replace again and assert the retired card stays absent during that visit. This keeps component checks distinct from runtime Done when evidence.

**Patron.** Approved — verify the DEV-541 generated contract at pickup, drive implementation via TDD in ForYouPage.test.tsx (paging, deduplication, in-place swap, exhaustion, rollback, and visit/single-in-flight behavior), run the web gates, and prove Done when with a running-app e2e spec in web/e2e/for-you-review.e2e.ts (append, replace, subsequent append/replace confirming the retired item never returns in the visit) plus PR screenshots of both controls.

Basis: (1) Pickup contract check and TDD loop (recon-DEV-517:26,54-56; specs/PRODUCT.md section 1): confirms generated DEV-541 types (offset, exclude, items/hasMore) before editing and verifies unit state transitions in ForYouPage.test.tsx; (2) Runtime Done when evidence (recon-DEV-517:17-20,55; specs/DEV-517/ticket-text.txt): Done when items 1-3 explicitly require both controls working in the running app with an e2e spec, a replaced suggestion not returning in the same visit across follow-on actions, and PR screenshots of both controls; (3) Gate discipline (AGENTS.md Verification commands; specs/PRODUCT.md section 1): require green web verification gates and report unchanged backend C# gates truthfully as SKIPPED (scope-empty) on exit 2.


---

## Q8. Closing bar, frozen scope, and round cap

**Keel.** Should we adopt this review closing policy for the frozen Q1-Q7 scope? I recommend at most two rounds in each of plan challenge and implementation review, with zero open in-scope Critical, High, or Medium findings and explicit dispositions for each Low. Any accepted fix, including Low, needs evidence at the reviewed head. Require Risk/Sentry, Standards/Ledger and Spec/Compass evidence for Size M, green applicable web gates, the running-app e2e receipt, and PR screenshots of both controls for delivery closure. Anything outside the agreed For You controls, integration, and tests is a follow-up. If blockers remain at the cap, return to the Conductor with the open disposition.

**Patron.** Confirmed — adopt the standing Size M closing policy on the frozen Q1-Q7 scope and close the DEV-517 grill.

Basis: (1) Standing closing bar and two-round cap (specs/DEV-541/spec.md:39; specs/DEV-542/CONCLUSIONS.md:47-49): maximum two rounds each in plan challenge and implementation review (returning any remaining blocker at the cap to Conductor/PR with explicit disposition), Size M Risk/Sentry, Standards/Ledger, and Spec/Compass reports, zero open in-scope Critical/High/Medium findings, every accepted finding including Low closed with evidence at the reviewed HEAD, and explicit cited rationale for any other disposition; (2) Delivery evidence (recon-DEV-517:17-20,55; AGENTS.md Verification commands): require green applicable web gates (reporting scope-empty C# gates as SKIPPED (scope-empty) on exit 2), running-app e2e spec receipt, and PR screenshots of both controls; (3) Frozen scope (CONCLUSIONS.md Q1-Q7; recon-DEV-517:13-26,52-55): scope is frozen to the For You Load more and per-card replacement controls, existing DEV-541 contract consumption, and named tests, with anything outside routed as a follow-up.
