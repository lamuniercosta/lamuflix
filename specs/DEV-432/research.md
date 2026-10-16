# Research: DEV-432 App Layout

## Decision: use the installed Tailwind v4 CSS-first theme as the shared visual-token source

**Decision**: Define the agreed LamuFlix palette, spacing, radius, and type-scale tokens through Tailwind v4 `@theme` namespaces in `web/src/index.css`, alongside a small set of semantic CSS custom properties where a semantic pairing is clearer than a utility. Use the existing system serif stack for the wordmark and system sans-serif for controls and metadata.

**Rationale**: The project already has Tailwind 4.3.3 and its Vite plugin. Tailwind's official theme documentation explains that `@theme` variables both create utilities and expose CSS variables, including the `--color-*` namespace. Its documented default `--breakpoint-md` is 48rem (768px), matching the user's agreed layout boundary. Keep all custom type and spacing tokens explicit so the visual system has one source of truth rather than unrelated one-off values.

**Alternatives considered**: Add a CSS-in-JS or component library (adds dependencies and a second styling system); keep ad hoc per-view CSS values (does not meet the shared-token requirement); redefine Tailwind's breakpoint (would conflict with the agreed 768px behavior).

**Sources**: [Tailwind CSS theme variables](https://tailwindcss.com/docs/theme) and [Tailwind CSS responsive design](https://tailwindcss.com/docs/responsive-design) (official Tailwind documentation; consulted through Context7, library `/tailwindlabs/tailwindcss.com`).

## Decision: keep routing and library query synchronization within the existing client

**Decision**: Use a small shared navigation state owner over the browser's existing History API rather than add a router dependency. The owner is responsible for parsing the current pathname, publishing app-initiated route changes, subscribing to browser `popstate`, and carrying the originating library URL into details. The library filter query remains the sole owner of browse filter values; panel visibility remains transient component state.

**Rationale**: `web/src/App.tsx` currently reads the pathname once with `useMemo(..., [])`; cards are not links, and details bypasses the shared shell. `FilterPanel` writes URL query state with `replaceState`, while `useWatchlistFilter` pushes history and listens for `popstate`. Centralizing navigation notification and having one query-state owner prevents a new router or separate shell/query state from competing with these existing responsibilities. On details, search edits remain local until Enter; the submitted destination uses the originating query with only the text criterion replaced. User-initiated filter changes continue to update the current library query and reset pagination when needed.

**Alternatives considered**: Add React Router (new dependency and broad route migration beyond this front-end feature); keep pathname memoized and add local one-off navigation handlers (direct, Back/Forward, and mounted-route state can diverge); duplicate filters in App and LibraryView (creates multiple sources of query truth).

## Decision: make transient overlays clean up on all exit paths

**Decision**: Use the existing React component/test setup for the mobile modal drawer, focus handling, and scroll lock. Close paths include Done, Escape, backdrop, route change, viewport change, and component cleanup; all paths release body scroll and restore focus when the trigger remains mounted.

**Rationale**: Mobile filters update live and closing retains selections. This avoids staged Apply/Cancel semantics while making keyboard and responsive transitions predictable.

**Alternatives considered**: Treat Done as Apply and add Cancel/revert state (changes confirmed behavior); persist drawer/sidebar visibility (explicitly rejected in alignment); install a modal library (unneeded dependency for this bounded interaction).

## Decision: preserve the current details API boundary

**Decision**: Show only details fields in the current response; do not invent a genres field. Preserve the current Play action and accessible 403 dialog behavior. A future requirement for per-movie genres or hiding Play before the request needs an authoritative backend contract and is deferred beyond this feature's API-frozen scope.

**Rationale**: `movieDetailsApi.ts` validates ID, title, path, format, watchlist state, and optional metadata; the API response does not include genres or a LocalPlay capability field. The play endpoint currently conveys a disabled state through HTTP 403. No corresponding frontend capability setting exists in the inspected surface.

**Alternatives considered**: Invent response properties or hide Play with a client environment flag (would misrepresent the current contract); remove 403 handling (regresses current behavior); widen the backend contract within a frontend-only ticket (explicitly deferred).

## Design research notes

- The existing palette reference is `specs/DEV-321/design.md` §1: dark slate ground and raised surface, warm white primary text, muted secondary text, and amber accent; its historical page layout is not adopted.
- Contrast must be checked for actual pairings. A measured warm-white-on-amber pairing is 1.89:1, so filled amber primary buttons use dark text. Existing quiet border `#2B313B` has only 1.19:1 against the raised surface, so actionable boundaries and focus rings need a brighter token; do not reuse the quiet divider as the only control boundary.
- The current detail view already renders optional poster, format, year/runtime, ratings, director, cast, plot, watchlist action, Play status, and a focus-managed 403 dialog. The browse grid already includes missing-poster handling and enrichment badges/retry behavior. The design consolidates their appearance without replacing those state contracts.
