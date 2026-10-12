# Design: DEV-324 Watchlist Toggle and Filter

**Ticket**: DEV-324 | **Branch**: `feature/324-spec` | **Date**: 2026-10-12
**Inputs**: `spec.md`, `plan.md`, `tasks.md`, `brief.md`, `CONCLUSIONS.md` (Q1–Q12 plus Q3 and Q7 addenda), `ASSUMPTIONS.md`
**Scope**: React SPA (`web/src`) only. DEV-324 builds no host: it wires a toggle into the delivered card, the delivered DEV-323 `MovieDetailsView` hero region, and the delivered filter host. Exact host file paths are fixed at Phase B pickup drift recon. (`frontend-design` skill unavailable in this session; design derives strictly from the settled inputs above.)

## 1. Route map and URL structure

No new routes. DEV-324 rides the delivered browse and details surfaces, both mounted via `App.tsx` (composition surface only; no rewrite, no router introduction).

| Surface | Host (prerequisite, path fixed at pickup) | URL |
|---|---|---|
| Browse grid | Delivered card/grid, mounted via `App.tsx` | Existing browse URL + `?watchlist=true` when filter is on |
| Details | DEV-323 `MovieDetailsView` hero region (backdrop/poster/title), mounted via `App.tsx` | Existing details URL for the movie; no watchlist key |

URL contract (CONCLUSIONS Q5, spec FR-008/FR-009):

- Only the exact value `watchlist=true` enables the filter. Absent, `false`, or any other value means off.
- Turning the filter on adds `watchlist=true`; turning it off drops the key.
- Switching the filter resets paging to page 1 and retains all other filter keys.
- The SPA translates `watchlist=true` to the browse request parameter `inWatchlist=true`; when off, no `inWatchlist` parameter is sent. No `watchlist` key is ever sent to the API.

## 2. Per-page layout (text wireframes)

### 2a. Browse page (grid + filter bar)

```text
+----------------------------------------------------------+
| FilterBar (delivered host)                               |
| [Search....] [Genre v] [Sort v] [Watchlist: off]         |
+----------------------------------------------------------+
| Grid (delivered)                                         |
| +----------------+ +----------------+ +----------------+ |
| | Poster         | | Poster         | | Poster         | |
| | [bookmark] (*) | | [bookmark]     | | [bookmark] (*) | |
| | Title          | | Title          | | Title          | |
| | year · genre   | | year · genre   | | year · genre   | |
| +----------------+ +----------------+ +----------------+ |
| (*) = filled bookmark (in watchlist); plain = outlined   |
+----------------------------------------------------------+
| Pager (delivered; resets to 1 on filter switch)           |
+----------------------------------------------------------+
```

- The bookmark button sits on each delivered movie card (placement follows the delivered card; default: poster corner overlay).
- The `Watchlist` quick-filter toggle sits in the delivered filter host (header or filter panel per delivered host; DEV-324 builds neither).
- Empty-filter state replaces the grid body:

```text
+----------------------------------------------------------+
| FilterBar: [Watchlist: ON]                               |
+----------------------------------------------------------+
| Your watchlist is empty.                                 |
+----------------------------------------------------------+
```

### 2b. Details page (hero region)

```text
+----------------------------------------------------------+
| Hero band (DEV-323 MovieDetailsView region)              |
| [Backdrop image                                  ]        |
|   [Poster]  Title (year)  [bookmark] (*)                 |
|             tagline                                  |
+----------------------------------------------------------+
| Rest of details (delivered; untouched)                   |
+----------------------------------------------------------+
```

- The toggle goes inside the delivered hero region (backdrop/poster/title block).
- Fallback (settled, Q3 addendum): if DEV-323 delivers no distinct hero block, the toggle goes next to the title in `MovieDetailsView`. Extracting a hero component stays out of scope.
- If DEV-323 delivers no details view at pickup, work stops with `needs decision:`; DEV-324 builds none.

## 3. Component tree

Added by DEV-324 (all under `web/src`; exact files fixed at pickup drift recon):

```text
App.tsx (composition only; wires delivered hosts)
+-- FilterBar (delivered host)
|   +-- WatchlistFilterToggle [NEW: "Watchlist", toggle button]
+-- MovieGrid (delivered host)
|   +-- MovieCard (delivered host)
|       +-- WatchlistToggleButton [NEW: bookmark button]
+-- MovieDetailsView (delivered DEV-323 host)
    +-- HeroRegion (delivered backdrop/poster/title block)
        +-- WatchlistToggleButton [NEW: same component]
```

- `WatchlistToggleButton`: presentational bookmark button; reads one movie's `isInWatchlist` from the shared hook; props: `movieId`, optional `size` (card vs hero). Inline SVG bookmark, outlined when off / filled when on. No icon-library dependency.
- `useWatchlistToggle(movieId)` [NEW shared hook]: owns optimistic `isInWatchlist`, in-flight guard, POST on add / DELETE on remove via the generated client, 204/409 retain, other-failure rollback + inline error string. State lives in a module-scoped per-movie store (`Map` keyed by `movieId`, private to the hook module; reset helper for tests only) read through React built-in `useSyncExternalStore`, overlaid on each delivered host's fetched `isInWatchlist` baseline; if pickup drift recon finds an already-delivered shared client cache for movie data, the hook updates that cache's per-movie entry instead and the module store is not built. Component-local state alone does not own this state: it is lost on unmount/navigation and fails Q10. No Context/provider, no state library, no new layer, no new dependency. Baseline `isInWatchlist` availability on the browse/details DTOs is unverified at pickup: absence means `needs decision:`, never a contract change.
- `WatchlistFilterToggle`: thin toggle bound to the delivered URL/browse state; translates `watchlist=true` to `inWatchlist=true` on the browse request, resets page to 1, retains other filters, combines with AND.
- Card and hero instances for the same movie read the same hook/cache entry, so a toggle in one surface shows in the other after navigation.

## 4. Component states

### WatchlistToggleButton

| State | Visual | Semantics |
|---|---|---|
| Off (settled) | Outlined bookmark | Name "Add to watchlist", `aria-pressed="false"` |
| On (settled) | Filled bookmark | Name "Remove from watchlist", `aria-pressed="true"` |
| Pending (request in flight) | Target state shown immediately (optimistic) | `aria-busy="true"`; repeat clicks ignored; name/`aria-pressed` reflect the target state |
| Success 204 / 409 | Target state retained | No message |
| Failure 404 / 422 / 500 / network / other | Rolls back to prior state | Inline, non-blocking error next to the button: "Couldn't update watchlist. Try again." |
| Filter-on list, removal succeeds | Card leaves the list | — |
| Filter-on list, removal fails | Card stays, rollback + inline error | — |

Error is inline and non-blocking: it never covers the grid, hero, or filter bar, and never navigates away.

### WatchlistFilterToggle

| State | Visual | URL / request |
|---|---|---|
| Off | "Watchlist", unpressed | No `watchlist` key; browse sends no `inWatchlist` |
| On | "Watchlist", pressed (`aria-pressed="true"`) | `?watchlist=true` (+ retained filters, page 1); browse sends `inWatchlist=true` ANDed with other filters |
| On, zero matches | Pressed + empty-state block | Grid body shows "Your watchlist is empty." |

Loading, partial-data, and list-error states of the grid, pager, and details body remain owned by their delivered hosts; DEV-324 adds no skeleton, spinner, or page-level error of its own.

## 5. Data needs (endpoint + fields)

Contract chain: C# DTO → OpenAPI (`web/src/api/openapi.json`) → generated TypeScript (`web/src/api/types.ts`). DEV-324 calls only the generated client with generated types; no hand-written request/response types; `openapi.json`, the verified snapshot, and `types.ts` are never hand-edited.

| Need | Endpoint | Fields / mapping |
|---|---|---|
| Add to watchlist | `POST /api/movies/{id}/watchlist` | Path `id`; per-movie `isInWatchlist` flips to `true` optimistically; 204/409 retain, else rollback + inline error |
| Remove from watchlist | `DELETE /api/movies/{id}/watchlist` | Path `id`; per-movie `isInWatchlist` flips to `false` optimistically; 204/409 retain, else rollback + inline error |
| Browse watchlist-only | Existing browse request + `inWatchlist=true` (only when URL is exactly `watchlist=true`) | `inWatchlist` ANDed with other delivered filters; page reset to 1 on switch |

No new endpoint, route, status code, DTO field, or query key. No `src/` or MVC change.

## 6. Keyboard navigation and accessibility (a11y)

- Both toggles are native buttons: reachable and operable by Tab / Shift+Tab and Enter / Space, with a visible focus indicator from the host styles.
- Bookmark button: accessible name "Add to watchlist" / "Remove from watchlist" (settled copy); `aria-pressed` reflects settled/target state; `aria-busy="true"` while in flight; in-flight clicks ignored (no double-POST 409 race).
- Filter toggle: label "Watchlist"; `aria-pressed` reflects on/off; URL is the state record, so Back/Forward and link-sharing preserve it.
- Optimistic flip keeps focus on the button; no focus jump on success, rollback, or card removal except the unavoidable unmount of a removed card (focus then moves to the grid region per the delivered list behavior, not to `body`).
- Inline toggle error is exposed to assistive technology (announced, non-modal) and stays adjacent to the failed button; the empty state "Your watchlist is empty." is a plain text block in the list region.
- Icon meaning never relies on color or fill alone: name + `aria-pressed` carry the state; the SVG is decorative (`aria-hidden`).
