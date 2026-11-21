# DEV-517 conclusions

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

---

## Plan challenge refinement — Q1/Q2/Q5 visit retention, multi-slice Load more, and Q4/Q5 recovery bounds

**Keel.** DEV-517 Phase 2 plan challenge needs a cited ruling for two in-scope refinements to Q1/Q2/Q5. Proposed: once initial page commits, keep the ordered visit grid, exclusions and cursor on incidental query invalidation/refetch, and start fresh on remount. For Load more, consume eligible active-set buffer then scan consecutive DEV-541 slices until up to take unseen suggestions are ready or hasMore=false, atomically append in order, hide Load more on true exhaustion (including empty terminal append), and preserve retry rollback. Keep Q4 manual retry with no retry cap/latch and Q5 replacement scan to true exhaustion with visit cancellation, no arbitrary fetch/time cap. Please rule from ticket/Q1-Q5, including [assumed] UX dispositions.

**Patron.** Approved (including `[assumed]` UX dispositions for visit grid/cursor preservation across incidental refetch, multi-slice Load more scan with empty-terminal hiding, uncapped manual retry, and uncapped replacement scan to `hasMore=false`) — once the initial page commits, retain the ordered visit grid, committed exclusions, and scan cursor/buffer across incidental query invalidation or refetch while resetting on remount; for Load more, consume eligible buffered items under the active exclusion set and scan consecutive DEV-541 slices until up to `take` unseen suggestions are collected or `hasMore=false`, atomically append in order, hide Load more on true exhaustion (including a 0-item terminal scan) (`[assumed]`), and preserve full rollback on failure for retry; keep Q4 manual retry with no retry cap or latch (`[assumed]`) and Q5 replacement scan to true exhaustion (`hasMore=false`) with visit cancellation and no arbitrary fetch or time cap (`[assumed]`).

Basis: (1) Committed visit state across incidental refetch (ticket Wanted/Done when in specs/DEV-517/ticket-text.txt; recon-DEV-517:14-19,52-54; CONCLUSIONS.md Q1, Q5; [assumed]): preserving the committed ordered grid, exclusions, and scan cursor/buffer once the initial page commits prevents React Query background refetch/invalidation from wiping appended rows, reintroducing replaced cards, or resetting the offset cursor mid-visit, while remount starts a fresh visit per Q1; (2) Multi-slice Load more scan, atomic append, and exhaustion hiding (ticket Wanted item 1 and Needs in specs/DEV-517/ticket-text.txt; recon-DEV-517:26,47-48; CONCLUSIONS.md Q2, Q5; [assumed]): because a prior replacement restarts ranking at offset=0 under the updated exclude set and early slices can overlap already-rendered cards, draining the active-set buffer and scanning consecutive DEV-541 slices until up to take unseen items are ready or hasMore=false guarantees Load more makes visible progress (or cleanly hides on an empty terminal scan when hasMore=false) and commits or rolls back atomically for retry; (3) Unbounded manual retry and true-exhaustion replacement scan with visit cancellation (recon-DEV-517:15,19,46-48; CONCLUSIONS.md Q2, Q4, Q5; AGENTS.md Non-negotiables; [assumed]): keeping Q4 retry manual without a client retry cap/latch and scanning Q5 replacements to server hasMore=false without an arbitrary fetch-count or timeout cap avoids inventing magic client thresholds while single in-flight locking and unmount/visit cancellation bound concurrent work.

---

## Gate 1 — Provisional ruling

**Patron.** `gate1: provisional` — all Q1–Q8 grill rulings, the plan challenge refinement, and all 25 round-one plan challenge dispositions in `PLAN_ADJUDICATION.md` are accepted with zero structural owner checkboxes.

Basis: (1) Cross-artifact alignment and plan challenge closure (`specs/DEV-517/brief.md`, `specs/DEV-517/spec.md`, `specs/DEV-517/plan.md`, `specs/DEV-517/tasks.md`, `specs/DEV-517/PLAN_ADJUDICATION.md`): Q1–Q8 grill rulings and the 25 round-one plan challenge dispositions across Risk/Sentry (10), Standards/Ledger (10), and Spec/Compass (5) are consistently incorporated into the frozen plan with zero open in-scope Critical, High, or Medium findings; (2) User-visible outcome readiness (`specs/DEV-517/checklists/requirements.md`:32-36; `specs/DEV-517/spec.md`:21-36): all "User-Visible Outcome" checklist items and SMOKE-001 through SMOKE-003 are ticked and specified; (3) Zero structural owner checkboxes (`specs/DEV-517/spec.md`:11-13; `specs/DEV-517/plan.md`:39-51): no ticket contradiction or constitution departure exists, leaving only ordinary owner Gate 1 spec-PR merge before Build.
