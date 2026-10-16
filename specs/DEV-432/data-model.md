# Data Model: DEV-432

DEV-432 adds no persisted domain entity or backend contract. It organizes existing browser and API data into view state with explicit ownership.

## LibraryQuery

The existing `FilterQuery` parsed and serialized by `web/src/features/library/filterQuery.ts` remains the canonical query model.

| Field group | Existing values | Ownership and behavior |
|---|---|---|
| Text | `text` | Library input is debounced; details keeps a local draft until Enter, then replaces only this field on the originating query. |
| People/categories | `genreIds`, `actorIds` | Existing IDs; removable chips clear only the represented value. |
| Bounds | `runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown`, `yearMin`, `yearMax` | Each endpoint has its own removal behavior; clearing one does not erase its partner. |
| Enrichment | `statuses` | Multi-value status selection; chips remove one status. |
| Ordering | `sort`, `direction` | Visible in toolbar; changes reset page. |
| Result window | `page`, `pageSize` | Existing values; membership or order changes reset page to the first page. |
| Watchlist and extensions | `inWatchlist`, unknown keys | Existing watchlist behavior and unknown query parameters are preserved across local updates and details return. |

The App-mounted navigation owner provides a reactive `pathname`/`search` snapshot. `App.tsx` derives one canonical `FilterQuery` from that snapshot through `filterQuery.ts` and passes the controlled query and update handlers to the shell/search, `LibraryView`, `FilterPanel`, and results toolbar. These children do not become independent active owners in the integrated app. Integrated changes go through the shared navigation owner, keeping URL and snapshot synchronized; isolated component adapters remain available for existing component tests.

The URL query is the shareable and reloadable representation. Filter controls, chips, sort, result count context, and API parameters derive from the App-owned query rather than competing copies. Browser history restoration re-parses the active URL and updates mounted results. A pending library-search debounce is canceled when navigation/history changes route so an old search cannot overwrite the next route.

## AppRouteSnapshot

Frontend-only route view state for the pathname and search string currently in the browser address bar.

| Member | Meaning |
|---|---|
| `pathname` | `/` for browse views or a validated `/movies/{positive-id}` detail route. |
| `search` | Current query string; on library routes it is the existing `LibraryQuery` serialization. |
| `libraryReturnUrl` | Exact originating same-origin library path and query stored with the details history entry and validated before use. Restored by the detail back link and used as the base for a details search submission. If missing or invalid (including direct details entry), back falls back to `/` and details search uses the default library query. |

The browser URL is authoritative for current route and search. App-initiated transitions publish the new route snapshot; browser Back and Forward publish from the updated location. `libraryReturnUrl` is accepted only when it resolves to a same-origin library route; details paths, external origins, and malformed values are rejected. History state is navigation context, not an API or domain record. Direct detail route loading/reloading without valid return state uses `/` as the safe fallback. Details search derives its base `LibraryQuery` from validated `libraryReturnUrl`, not from the details route's query string; when return state is absent, it uses the default library query.

## Transient presentation state

| State | Default | Lifetime |
|---|---|---|
| Desktop sidebar visibility | Expanded at 768px+ | Current mounted view only; never persisted. |
| Mobile filter drawer visibility | Closed below 768px | Current mounted view only; never persisted. |
| Details search draft | Empty or initialized from the prior library text | Until submit or route exit; does not change route while typing. |
| Drawer focus origin and scroll lock | None while closed | Active only while modal drawer is open; released on close and unmount. |

## Existing API values used by the views

- `MovieSummaryResponse` continues to supply card identity, poster, title, enrichment state, metadata, and watchlist state.
- `MovieDetailsResponse` continues to supply movie file information, watchlist state, and nullable metadata for poster, plot, year, runtime, ratings, director, and actors.
- The detail schema exposes neither genres nor playback capability. Genres and proactive per-movie Play visibility depend on a future backend contract and are deferred. Playback remains an action against the existing endpoint; its 403 response is the current disabled-server signal.
