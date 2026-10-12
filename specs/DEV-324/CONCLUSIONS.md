# DEV-324 — Grill conclusions (Patron)

Grill: Phase 2 step 1, M-ticket, 12 questions (cap reached, none over). Worktree
`F:\Dev\LamuFlix.worktrees\feature-324-spec`, branch `feature/324-spec`, base
`1de5162257b4eef7f1ca111ef8f6a9d1594ff542`.

Sources: ticket DEV-324 (get-task.ps1, 2026-10-12): *Scope & Technical Design* (S&TD) items 1–2 and
the acceptance criteria (AC). Parent DEV-286 "Epic 6: React SPA". Recon notes `recon-DEV-324`,
`recon-DEV-324-3`, `recon-DEV-324-2`. `specs/PRODUCT.md`. `lamuflix-team-charter`.

Verdict: **no `blocked: structural`**. No escalation to the owner. No YouTrack change.

## Q&A and rulings

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

## Closing bar, frozen scope, round cap
- **Closing bar:** both AC hold in the SPA under the Q12 tests, the web gates are green, the contract snapshot is unchanged, and there are no `.cs` changes.
- **Frozen scope:**
  - in `web/src`: the bookmark toggle on the card and the details hero, the watchlist quick-filter toggle in the header or filter panel, and the shared optimistic hook;
  - mapping the URL key `watchlist` to the API key `inWatchlist`.
  - Nothing else.
- **Round cap:** the standing pipeline cap applies, cited and not re-ruled.

## Noted, no ticket
- The MVC watchlist UI has no tests (recon-DEV-324-2:12). This is legacy surface outside Epic 6 and is not broken behaviour.
- The MVC app uses a star icon while the ticket says bookmark. The SPA follows the ticket, and the MVC app is left alone.

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

## Gate 1 provisional ruling (2026-10-12, Conductor ask, task-pipeline Phase 2 step 7)
**Ruling: `gate1: provisional` is set on the `spec.md` `**Status**:` line. This is not Gate 1 completion, not Phase A completion, and not Phase B authorization. Gate 1 opens only when the user merges the spec PR.**
- Analysis is clean: Keel's corrected-design `/speckit-analyze` is CLEAN, with 15/15 requirements and criteria covered across 31 tasks and zero remaining fixes (note `DEV-324:170-174`).
- Plan challenge is adjudicated and the plan is frozen: all six accepted fixes are satisfied, and Sentry/Ledger/Compass findings are closed or bounded as pickup stops (note `DEV-324:181-187`). This matches the step order in task-pipeline:68-82 (`recon-DEV-324:46-47`).
- No open `blocked: structural` item or owner checkbox is recorded in these roots. Phase B still waits on the prerequisite delivery merges, DEV-323 included (brief:33,63; Q3 addendum).