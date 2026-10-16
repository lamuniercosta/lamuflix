# Frontend Navigation and Query Contract

This is a UI behavior contract. It does not add or change a backend/API endpoint or generated schema.

## Route snapshot

- The rendered page MUST correspond to the current browser pathname and search string on initial load, after reload, after an app transition, and after browser Back/Forward.
- `/movies/{id}` is a movie-details route only for a positive integer ID. The library remains the fallback for the root browse route and malformed detail candidates.
- Selecting a movie MUST preserve normal link affordances and navigate without a full-page reload.
- The details history entry MUST retain the exact originating library path and query in navigation state. Accept a return URL only when it is same-origin and its path is the library route; reject malformed, external-origin, or non-library values.
- The details back link MUST restore that exact validated path/query, including paging and unknown parameters. If the details route was opened directly, reloaded without valid return state, or has invalid return state, its back link MUST go to `/`.
- Back/Forward MUST update the mounted route and browse query from the browser's current URL; pathname cannot be a one-time startup value.

## Library query

- `web/src/features/library/filterQuery.ts` remains the single parser/serializer for current recognized keys and unknown-key preservation.
- The App-mounted `web/src/features/app-navigation/useAppNavigation.ts` owner MUST provide a reactive `pathname`/`search` snapshot. `App.tsx` MUST derive one canonical `FilterQuery` from that snapshot via `filterQuery.ts`, then pass controlled query/update handlers to the shared shell/search, `LibraryView`, `FilterPanel`, and results toolbar.
- One canonical App-owned query MUST drive filter controls, active chips, sort, results/API input, and URL updates. Integrated children MUST NOT independently write browser history; isolated component adapters may preserve standalone component tests without creating a second active owner in the app.
- Removing a chip clears only its represented filter value. Range endpoint chips operate independently. Filter membership and sort changes reset page to the existing first-page value.
- Library typing keeps the existing debounce behavior. Query replacements do not create one browser history entry per keystroke.
- Route/history transitions MUST cancel any pending library-search debounce before applying the next route snapshot, so an old queued search cannot overwrite the active route/query.
- In-app navigation to a movie records the current library path and complete query before displaying details.
- Search typed on details remains local draft state. Enter navigates to the library using the retained validated library query (or the default library query when details was opened directly), replaces only the text filter with the submitted value, and resets page. It MUST derive that base from validated return state, not the `/movies/{id}` query. Typing without Enter leaves the details route and address bar unchanged.

## Responsive filter panel

- At widths `>= 768px`, render an expanded collapsible sidebar on fresh load. At widths `< 768px`, render a closed modal drawer on fresh load.
- Panel visibility is transient and does not enter the URL or persistent browser storage.
- Filter edits apply live in both layouts. Done, Escape, backdrop close, route transition, breakpoint change, and unmount do not revert current values.
- While the drawer is open it traps keyboard focus, makes the background non-interactive, and prevents document scrolling. On close it removes listeners/scroll lock and returns focus to the opener if still available.

## Preserved service behavior

- Import, watchlist, enrichment retry/status, browse error recovery, and details loading/missing/error behavior retain existing API semantics.
- Play continues to call the existing endpoint. Success, 404, generic failure, and 403 unavailable dialog remain distinct and accessible.
- The inspected details response exposes neither genres nor pre-request playback capability. Under the confirmed frontend-only boundary, details shows the fields currently available and keeps Play's existing request-time feedback, including 403. Genres and proactive per-movie Play capability visibility are deferred to a backend follow-up; this feature does not create that follow-up or extend API/generated types.
