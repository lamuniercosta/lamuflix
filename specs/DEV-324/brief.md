# DEV-324 — Watchlist integration brief

## Status and authority

Written by Keel on 2026-10-12 at branch `feature/324-spec`, HEAD `1f2034b`, in `F:/Dev/LamuFlix.worktrees/feature-324-spec`. Updated at HEAD `be2c23c` to reflect Patron's Q3 details-hero addendum; bounded recon remains evidence from `1f2034b`. Updated at HEAD `f72ff6a0f5cb788689759cb9f8e4ddcbdb22a428` to record Patron's Q7/Q10 shared-state mechanism ruling.

This brief records the settled 12-question Patron grill and bounded dependency evidence. It is not plan freeze, Gate 1 approval, or Phase B authorization. Patron's Q3 addendum settles the details-hero prerequisite as the backdrop/poster/title region of DEV-323 `MovieDetailsView`, mounted via `App.tsx`. DEV-324 adds its toggle there; no new host, ticket or YouTrack change is needed. The brief is ready for Quill; no open hero-assignment decision remains. CONCLUSIONS Q3 remains in force: DEV-324 integrates the toggle into prerequisite hosts, and does not build those hosts.

Authority: task note `DEV-324:7-8`; `CONCLUSIONS.md` Q1–Q12, Q3 and Q7 addenda and closing bar; task note `DEV-324:75-80`; `ASSUMPTIONS.md`; evidence notes `recon-DEV-324:19-26,31-40` and `recon-DEV-324-6:4-25`. Loop limits are the standing `task-pipeline` §2.2 limits, adopted in CONCLUSIONS. Dependency states below are bounded note evidence at recon HEAD `1f2034b`, qualified by Patron's Q3 addendum, not live PR or merge verification. Mechanism authority: task note `DEV-324:132-148`, CONCLUSIONS Q7 addendum at commit `f72ff6a`.

## Closing bar, frozen scope, and round cap

Closing bar: both ticket acceptance criteria hold in the React SPA: instant bookmark feedback and correct watchlist-only filtering of the browsed movie list. Required web tests and applicable web gates pass; the committed OpenAPI baseline and verified snapshot stay byte-identical; no C# files change (CONCLUSIONS Q1, Q4, Q6, Q12). All prerequisite integration hosts must be established and delivered before Phase B begins (Q3). Any unresolved blocker prevents clearance; this brief does not supply a new severity policy.

Frozen scope: bookmark toggles on the existing movie card and details hero, a Watchlist quick-filter toggle in the existing header or filter panel, shared optimistic state for those toggles, and translation from SPA URL `watchlist=true` to API `inWatchlist=true`. No host construction, backend or contract change, MVC work, new dependency, state library, architectural layer, or LocalPlay work is authorized. Anything else is a follow-up issue, not a finding in this round.

Round cap: grill capped at 12 questions, already reached; review capped at two rounds; remediation capped at two fix commits per round (`task-pipeline:25-27`). Third-round findings go to user review with the PR. The Q3 and Q7 addenda clarify the host and shared-state mechanism within existing scope; neither is a thirteenth grill question.

## Dependency map and delivery prerequisites

| Surface | Bounded attribution | Recorded planned integration surface | Delivery status / limitation |
|---|---|---|---|
| Generated types and API client | DEV-320 (`recon-DEV-324:34`) | `web/src/api`, including generated `types.ts`; exact client module not supplied | Absent at this HEAD; delivery merge not found in the named roots |
| Movie card and grid | DEV-321 (`recon-DEV-324:35`) | LibraryView / App composition; isolated tile proof | Exact card component file not established; no delivered card/grid at this HEAD |
| Details hero | DEV-323 (CONCLUSIONS Q3 addendum; `DEV-324:75-77`) | Hero region (backdrop/poster/title) of `MovieDetailsView`, mounted via `App.tsx` | Phase B waits for DEV-323 delivery merge; actual path and placement require pickup drift recon; no separate hero host is built |
| Filter quick-toggle host | DEV-322 filter panel (`recon-DEV-324:37`) | `web/src/features/library`, FilterPanel, mounted via `App.tsx` | Filter panel absent at this HEAD; header assignment not found. Q3 does not authorize constructing a header |
| URL-synced browse state | DEV-321 and DEV-322, parent DEV-286 (`recon-DEV-324:38`) | Adapter state/history/listener with LibraryView / App composition; `web/src/features/library` | Exact module filenames and delivered state integration not established |

Patron's Q3 addendum identifies the details hero as the top region of DEV-323 `MovieDetailsView` displaying backdrop, poster and title, mounted via `App.tsx`. Basis: DEV-323 ticket description and planned surface (`DEV-323:8,45,49`), as cited by CONCLUSIONS Q3 addendum. This closes the previous hero-assignment decision without changing DEV-324 ticket scope, creating a host or ticket, or changing YouTrack. DEV-324 only adds the toggle to that delivered region.

At Phase B pickup, bounded drift recon records the actual `MovieDetailsView` path and title/backdrop block. If no distinct hero block exists, the toggle goes next to the title in `MovieDetailsView`; extracting a hero component remains out of scope. If DEV-323 delivers no details view, report `needs decision:` rather than build one (Q3 addendum).

The notes record open specification PRs, not delivered prerequisites (`recon-DEV-324:39`). No implementation/delivery merge was found in the bounded roots. Spec-PR Gate 1 is authorization of the specification and remains distinct from implementation/delivery merge evidence. Phase B requires Gate 1 plus prerequisite delivery merges and pickup drift recon confirming the hosts and generated client exist. In particular, DEV-324 Phase B waits on the DEV-323 delivery merge; a merged specification PR alone does not satisfy Q3. These are future pickup constraints, not blockers to Quill drafting from this settled brief.

## Approach and files touched

Use the generated API client and generated types to invoke the existing POST/DELETE `/api/movies/{id}/watchlist` endpoints. Keep the C# DTO → OpenAPI → generated TypeScript chain intact. The small shared hook owns a module-scoped per-movie store outside components, read through React `useSyncExternalStore`, with an overlay on each delivered host's fetched `isInWatchlist` baseline. If pickup recon finds an already-delivered shared client cache for movie data, update its per-movie entry instead and do not build the module store. No Context/provider, state library, new layer or dependency is authorized (Q7 addendum, Q8). Card and hero share the same per-movie state across navigation and request settlement after unmount (Q10; Q7 addendum). Exact hook and integration filenames await delivered hosts and pickup evidence; the recorded component names above are planned surfaces, not verified file paths.

Restrict implementation and related frontend tests to `web/src`: the delivered card, delivered hero, delivered filter host, delivered URL/browse integration, and the small shared hook. `App.tsx` is a recorded composition surface, not permission to replace the app or introduce a router. Quill must state unresolved filenames as such rather than invent prerequisite code. Planning artifacts belong in `specs/DEV-324`; Quill writes spec, plan and tasks from this brief. The existing `.specify/feature.json` modification is preserved.

Do not edit `src/`, the MVC app, endpoint routes/statuses/DTOs, `web/src/api/openapi.json`, the verified OpenAPI snapshot, or generated `web/src/api/types.ts` by hand. No new dependency, schema, project, top-level folder, architectural layer, or L-ticket ADR is selected here.

## Test strategy and gate expectations

Use the existing Vitest, React Testing Library and MSW stack and the `web-implement` TDD workflow (Q12). Cover both card and hero integration after their hosts are delivered, and exercise the shared state across navigation. Required behavior is:

- Optimistic bookmark state changes before the API response arrives.
- POST adds and DELETE removes; 204 and 409 retain the confirmed target overlay after request settlement and across card/hero navigation, including completion after the originating surface unmounts.
- 404, 422, 500, network failure, and other failures roll back and show the inline error.
- Repeated in-flight clicks on any surface for the same movie are ignored; `aria-busy` clears on success or failure, and accessible names and `aria-pressed` reflect state. State, pending guard and errors for movie A never affect movie B; delayed failure after navigation rolls back and shows the per-movie error on the mounted surface.
- With the Watchlist filter active, successful removal removes the card; failed removal keeps it.
- Only URL value `watchlist=true` enables API `inWatchlist=true`; absent, false, and other values leave that filter off.
- Switching the filter updates the URL, resets page to 1, retains other filters, and combines them using AND.
- The settled labels, bookmark treatment and empty-state copy in ASSUMPTIONS are applied.

Run applicable web verification through `scripts/run-web-gates.ps1` in Phase B (`task-pipeline:94`) and preserve the unchanged OpenAPI drift test. The prior candidate C# baseline counts 0/0/0 are historical measurements, not verification of this feature. Q12 anticipates no C# diff; diff-based C# applicability is reported by Gauge, with any scope-empty result explicitly SKIPPED (scope-empty), never PASS. No implementation tests or gates are run to produce this documentation brief.

## Task ordering and handoff constraints

1. Carry Patron's settled Q3 addendum into spec/plan/tasks: DEV-323 `MovieDetailsView` via `App.tsx` supplies the hero region; DEV-324 integrates the toggle there and builds no host. Preserve delivery prerequisites and defer actual file paths to pickup drift recon.
2. Quill drafts spec, plan and tasks from the recorded decisions and bounded evidence. Gaps return as `needs decision:`; no prerequisite construction or hero omission may be inserted.
3. For this `ui:` ticket, await Palette's `design.md` before plan challenge. Keel performs read-only speckit-analyze and checks the plan/tasks against this brief after Quill's draft. This brief makes no claim that either step has occurred.
4. Spec-PR Gate 1 authorization and any applicable owner checkbox stay separate from plan freeze and from implementation/delivery merges. Phase B also waits for all prerequisite implementation/delivery merges, explicitly including DEV-323; merging a spec PR does not deliver a host or authorize implementation by itself.
5. At Phase B pickup, get bounded drift recon for delivered component/client paths, actual dependency delivery and any already-delivered shared client cache. Confirm that both delivered hosts read `isInWatchlist` from their generated DTOs; this is unverified, and absence means `needs decision:` without a contract change. List reconciliation with the filter on remains pickup seam work, not settled by Q7. Carry the Q7 addendum owner, cache alternative and no-cache module-store fallback into plan/tasks before implementation; re-run speckit-analyze against main and amend tasks for recorded drift. Serialize changes to shared hook, composition and shared test files.
6. Implement the shared optimistic behavior and card/hero integration after the client and both hosts exist, using the settled Q7 addendum mechanism and per-movie isolation. Wire the Watchlist URL/API mapping into the delivered browse/filter state, then verify paging, retained filters, removal behavior and accessible states through the required tests.
7. Complete applicable web verification and the M-ticket three-axis review before clearance. This brief grants no build, review, merge or delivery clearance.

## Every grill question and answer

The following Q1–Q12 exchanges are reproduced from CONCLUSIONS without changing Patron's rulings. In Q3, the original Unverified attribution remains historical and is qualified by the dependency evidence and settled Patron addendum below. Q7's original built-in-hook choice is historical and is qualified by the settled Q7 addendum below; component-local state alone does not satisfy Q10.
### Q1. Which UI surface does the ticket target: the React SPA (`web/`) or the Razor MVC app (`src/LamuFlix.Web`)?
**Ruling: only the React SPA (`web/`). The MVC app is out of scope and is not edited.**
- The parent is DEV-286 "Epic 6: React SPA", whose description lists "watchlist toggle" as an SPA deliverable. PRODUCT.md §1 names the frontend as `web/` (React 19, TypeScript, Vite).
- S&TD 1 names `/api/movies/{id}/watchlist`. The MVC toggle posts to `/Movies/AddToWatchList`, not to the API (recon-DEV-324-3 §3, site.js:141-159).
- The MVC app already has a toggle, a page and navigation (recon-DEV-324-3 §3). Its lack of tests (recon-DEV-324-2:12) is noted, no ticket.

### Q2. Does the ticket touch `Features:LocalPlay`, secrets or `Process.Start` (§2.3 item 5)?
**Ruling: no. The MVC files that sit next to LocalPlay code are not touched.**
- `MovieService.cs:58,64` and `MoviesController.cs:65-76` are MVC files only (recon-DEV-324-2 §2.3-5). Q1 removes the MVC app from scope.
- The Core, API and `web/src` watchlist code does not reference LocalPlay (same source).

### Q3. The SPA has no movie card, details hero, header or filter panel, and no generated `types.ts`. Who builds them?
**Ruling: sibling Epic 6 tickets build them, not DEV-324. Phase B of DEV-324 starts only after the tickets that deliver those pieces have merged.**
- Recon-DEV-324-3 §4 shows `web/src` is still a placeholder. DEV-286 assigns the generated API client, the movie grid, the filter panel and the details page to Epic 6 as separate deliverables.
- If DEV-324 built cards, a hero or a header itself, it would add to what the ticket delivers (charter §2.3 a). Sequencing the work avoids that.
- The dependency tickets are the open Epic 6 spec PRs in the `chain` note (DEV-320–323, DEV-80; DEV-323 is the details page). **Unverified:** I did not open their specs. Keel names the exact dependency per surface in `brief.md`. Phase B drift recon confirms the components exist before build starts.

### Q4. Do any API routes, DTOs or status codes change (§2.3 item 4)?
**Ruling: no backend or contract change. DEV-324 is frontend only.**
- POST and DELETE `/api/movies/{id}/watchlist` already exist with 204/400/404/409/422 responses, and they are frozen in `web/src/api/openapi.json:309-382` (recon-DEV-324 §1).
- The browse filter `inWatchlist` already exists in `openapi.json:94` (recon-DEV-324-3 §2).
- The OpenAPI baseline and its verified snapshot stay byte-identical.

### Q5. The ticket says `?watchlist=true`, but the API key is `inWatchlist`. Is this a contradiction?
**Ruling: no. `?watchlist=true` is the SPA URL. The SPA translates it to the API's `inWatchlist=true`.**
- S&TD 2 describes the *view* filter. DEV-286 specifies "URL-synced filter state", so the URL key belongs to the SPA. The ticket line is authoritative for that key (PRODUCT.md §3).
- No `watchlist` parameter is added to the API, so Q4 stands. A `watchlist` value other than `true` (absent, `false` or anything else) means the filter is off.

### Q6. The AC says "filters movie collection". Does that mean the `CollectionId` entity?
**Ruling: no. "Movie collection" means the browsed movie list. No collection parameter is involved.**
- S&TD 2 says "filter view to watchlist movies only". There is no collection field in `MovieQuery` or the API (recon-DEV-324-3 §2), and the ticket names none.

### Q7. How is the optimistic UI implemented, and is a new npm dependency allowed (§2.3 items 1–2)?
**Ruling: no new dependency and no state library. Use React 19 built-ins (`useOptimistic` or `useState`) in a small hook shared by the card and the hero.**
- `web/package.json` runtime dependencies are only react and react-dom 19.3.0 (recon-DEV-324-2 §2.3-1). React 19 covers optimistic state natively.
- PRODUCT.md §2 forbids speculative abstractions. TanStack Query is mentioned in DEV-286 only for import polling. If a sibling ticket has already merged it by Phase B, DEV-324 may use it, because that adds no new dependency. DEV-324 does not introduce it.

### Q8. How does the API client get called, given the contract chain?
**Ruling: use the generated `web/src/api/types.ts` and the Epic 6 generated client only. No hand-written request or response types.**
- Charter, contract chain rule. Recon-DEV-324-2 §contract chain shows `types.ts` is missing today, which is covered by Q3's sequencing.

### Q9. What do the toggle's error statuses do?
**Ruling:**
- **204:** keep the optimistic state.
- **409:** keep the optimistic state. The server already has the target state, so this counts as success.
- **404, 422, a network failure or any other status:** roll back to the previous state and show a non-blocking inline error.
- Basis: the endpoint statuses (recon-DEV-324 §1). The domain throws on a double add or remove (`Movie.cs:101-118`), which is the 409. Rolling back on a real failure is sound engineering, and AC1 asks for "instant feedback", not hidden failures.

### Q10. What happens on rapid repeat clicks, and how do card and hero stay in sync?
**Ruling:**
- The button ignores clicks, and shows `aria-busy`, while its request is in flight.
- The card and the hero read the same movie's `isInWatchlist` from the shared hook or cache, so a toggle in one is shown in the other after navigation.
- With the watchlist filter on, removing a movie removes its card from the list after the request succeeds. If the request fails, the card stays.
- Basis: sound engineering. This prevents a 409 race from double POSTs and avoids a stale list.

### Q11. How does the quick filter combine with other filters and with paging?
**Ruling: the filter combines with the other filters using AND. Switching it resets paging to page 1 and keeps the other filters.**
- `MovieQuery` already combines `InWatchlist` with the other filters (recon-DEV-324-3 §2; `ApiEndToEndBrowseTests.cs:53-61`).
- Resetting to page 1 when a filter changes is standard and is logged `[assumed]`.

### Q12. What are the test strategy and gate expectations?
**Ruling:**
- Use Vitest, React Testing Library and MSW, which are already devDependencies (recon-DEV-324-3 §4). Build with the `web-implement` skill (charter preamble).
- Required tests:
  - the toggle flips immediately before the response arrives;
  - 204 and 409 keep the new state;
  - 404, 422 and 500 roll back and show the error;
  - in-flight clicks are ignored;
  - `?watchlist=true` sends `inWatchlist=true`, and an absent or other value sends no filter;
  - the filter toggle syncs to the URL and resets the page;
  - the accessible name and `aria-pressed` are correct.
- No C# file is expected to change. The C# gates (Roslyn, complexity, InspectCode, Stryker) are N/A unless the diff touches `.cs`, so the baseline of 0/0/0 stays.
- The OpenAPI drift test must stay green and unchanged.


## Q3 addendum: details-hero host (2026-10-12, Conductor ask; not a 13th grill question)
**Ruling: DEV-323 is the prerequisite for the details hero. The host is the hero region of its `MovieDetailsView` (mounted via `App.tsx`): the block that shows the backdrop, poster and title. DEV-324 puts the toggle there. It builds no separate hero component, and no ticket or YouTrack change is needed.**
- In UI terms, a details hero is the top band of a details page: backdrop, poster and title. DEV-323 delivers that band. Its description lists poster, backdrop, title and tagline (note `DEV-323:8`), and its planned surface is `MovieDetailsView` via `App.tsx` (`DEV-323:45,49`). It is the only details-page ticket under DEV-286 (Q3; `recon-DEV-324-6:10-12,19-20`). Because the word "hero" names a region of that view and not a separate deliverable, finding no hero assignment (`recon-DEV-324-6:23-25`) does not mean a ticket is missing.
- This keeps Q3: DEV-324 adds a toggle to a host that DEV-323 delivered and constructs nothing. It also keeps the ticket text (`DEV-324:8`, "card and details hero") unchanged, so there is no §2.3 a escalation.
- Phase B rule: Phase B waits for the DEV-323 delivery merge, as Q3 requires. Pickup drift recon records the actual `MovieDetailsView` path and its title/backdrop block. If DEV-323 delivers no distinct hero block, the toggle goes next to the title in `MovieDetailsView`. That placement is a taste matter, and extracting a hero component is still out of scope. If DEV-323 delivers no details view at all, it is reported as `needs decision:` and not built.


## Q7 addendum: shared watchlist state across navigation (2026-10-12, Conductor ask; not a 13th grill question)
**Ruling: Q7 permits only the small shared hook, and that hook must own its state outside any component. The state lives in a per-movie store inside the hook's own module, read with React's built-in `useSyncExternalStore`. Each surface's `isInWatchlist` from its fetched data is the baseline, and the store holds an overlay on top of it. No Context/provider, no store or state library, no new layer, and no dependency. This is a §2.3 item 2 ruling (not triggered), not an escalation.**
- **Why a hook with only local state is not enough:** Q10 (`:86-91`) and FR-006 require that a toggle in one surface shows in the other after navigation. There is no router, so the card unmounts when the details view mounts (`recon-DEV-324:36-38`; DEV-323 `MovieDetailsView` mounted via `App.tsx`). A hook that keeps its state only in `useState` or `useOptimistic` inside the component loses that state on unmount, so it fails Q10. The plan and the tasks must name the module store as the owner (accepted Sentry F1/Compass M1, note `DEV-324:134,140`).
- **Who owns the state:** a `Map` keyed by `movieId`, private to the hook's file. The hook is its only public API, plus a reset helper for tests only.
  - A click writes `{ target, pending: true }` to the store first (the optimistic change), and then the request starts. The request's result is written to the store, not to component state, so it still lands if the card has unmounted or the user has navigated away (accepted F7).
  - On 204 or 409, the overlay stays as confirmed and outlives the request (accepted F4).
  - On any other result, the overlay is removed, so the value rolls back to the baseline. A per-movie error is recorded and any mounted toggle for that movie shows it inline (Q9).
  - The displayed value is the overlay if one exists, otherwise the baseline. Overlays last only for the page's lifetime: no `localStorage` and no persistence.
  - Why a module store and not Context: a module store needs no provider placed above hosts that do not exist yet. The `App.tsx` guard therefore stays composition-only (brief guard; PRODUCT.md §2 no speculative abstraction, as cited in Q7). React's own `useSyncExternalStore` is not a state library.
- **When no sibling cache exists, and how movies stay isolated:**
  - If drift recon at pickup finds an already-delivered shared client cache for movie data (Q7: one that a sibling ticket has already merged), the optimistic update writes to that cache's entry for the movie, and the module store is not built.
  - Otherwise the module store is used, and the baseline comes from the `isInWatchlist` field that each delivered host already reads from its generated DTO.
  - **Unverified:** whether the browse and details DTOs expose `isInWatchlist`. If one does not, report `needs decision:` at pickup. Do not change the contract (Q4).
  - Everything is keyed per `movieId`: the overlay, the pending flag, the in-flight guard and the error. Toggling, a request in flight, or a failure on movie A never changes movie B (accepted F-isolation, FR-006).
  - The in-flight guard is shared by every surface for that movie. A hero click made while the card's request for the same movie is still pending is ignored, and `aria-busy` clears when the request settles (Q10).
  - How the list reconciles when the filter is on stays with the pickup seam work (accepted F5); this ruling does not settle it.

## Settled taste assumptions

# DEV-324 — Taste assumptions (Patron)

- [assumed] Icon: an inline SVG bookmark, outlined when off and filled when on, with no icon-library dependency. Basis: S&TD 1 "bookmark icon"; PRODUCT.md §4.
- [assumed] Toggle accessible name: "Add to watchlist" / "Remove from watchlist", with `aria-pressed` reflecting state. Copy is English, matching the existing SPA copy (`App.tsx`).
- [assumed] Quick-filter button label: "Watchlist", shown as a pressed toggle when `?watchlist=true` is set.
- [assumed] Error copy for a failed toggle, shown inline and non-blocking: "Couldn't update watchlist. Try again."
- [assumed] Empty state when the filter is on and nothing matches: "Your watchlist is empty."
- [assumed] Turning the filter on or off resets paging to page 1 and keeps the other filters (CONCLUSIONS Q11).
