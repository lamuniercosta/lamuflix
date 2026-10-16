# Design Direction: LamuFlix App Layout

## 1. Design direction

LamuFlix is a personal movie catalogue. Posters carry the color and character; the surrounding interface behaves like a quiet screening-room index, with a fixed editorial wordmark, legible utility controls, and filters kept close to the collection. There is no marketing hero: the catalogue is the first view.

### Shared tokens

| Token | Value | Use |
|---|---|---|
| `--color-booth` | `#16181D` | Page background |
| `--color-reel` | `#20242B` | Header, raised panels, cards, inputs |
| `--color-rule` | `#2B313B` | Decorative dividers only |
| `--color-boundary` | `#64748B` | Input/control boundary against dark surfaces |
| `--color-ink` | `#F0EDE6` | Primary text on dark surfaces |
| `--color-dim` | `#98A0AC` | Secondary text on raised surfaces |
| `--color-amber` | `#E0A34A` | Focus ring, selected markers, filled primary action background |
| `--font-wordmark` | System serif stack | LamuFlix wordmark only |
| `--font-interface` | System sans-serif stack | Navigation, controls, metadata, body copy |

Type scale: 12px supporting labels, 14px controls and metadata, 16px body, 20px section headings, 28px view headings, and 36px wordmark. Use sentence case and keep supporting labels short. Poster titles wrap to two lines before truncation is considered.

Spacing follows a 4px base with named steps at 4, 8, 12, 16, 24, 32, and 48px. Controls use a 6px radius; poster and detail media use an 8px radius; the filter drawer may use an 8px top corner on small screens. These values are a compact shared system, not a radius applied indiscriminately to every element.

The amber fill is paired with the dark booth text token, never warm white. Focus uses a visible amber outline plus a dark offset. Quiet rule color is decorative only; controls use the brighter boundary token. Statuses use a word label and a marker, with contrast checked independently from status hue. Respect `prefers-reduced-motion`; interaction transitions remain brief and functional.

### Layout concepts

Desktop, 768px and wider: a compact shared header sits above a two-column browse surface. The collapsible filter rail has a stable bounded width; the poster field receives remaining width. Results count, removable chips, and sort control align immediately above the grid. No oversized empty hero band competes with the posters.

```text
┌──────────────────────────────────────────────────────────────────────┐
│ LamuFlix     Library   Watchlist   Import folder        Search…     │
├───────────────────────┬──────────────────────────────────────────────┤
│ Filters          [‹]  │ 128 movies   [Genre: Drama ×] [Sort: Title ▾]│
│ Genre / Cast          │ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐         │
│ Year / Runtime        │ │poster│ │poster│ │poster│ │poster│         │
│ Enrichment status     │ │title │ │title │ │title │ │title │         │
│                       │ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐         │
└───────────────────────┴──────────────────────────────────────────────┘
```

Mobile, below 768px: the header actions wrap without squeezing the wordmark; the content begins with a clear Filters trigger and one results toolbar. The filter panel is a modal drawer over the same page. Its Done action closes only; live filter selections stay active.

```text
┌────────────────────────────┐
│ LamuFlix      [Search…]    │
│ Library  Watchlist  Import │
├────────────────────────────┤
│ [Filters]     12 movies    │
│ [Drama ×] [Sort: Title ▾]  │
│ ┌───────────┐ ┌───────────┐│
│ │  poster   │ │  poster   ││
│ │ title     │ │ title     ││
│ └───────────┘ └───────────┘│
└────────────────────────────┘

Drawer open:                 │
┌────────────────────────────┐
│ dimmed library              │
│ ┌────────────────────────┐ │
│ │ Filters            Close│ │
│ │ Genres / cast / ranges│ │
│ │ Status                │ │
│ │                        │ │
│ │ [Done]                 │ │
│ └────────────────────────┘ │
└────────────────────────────┘
```

Movie details uses the same header and background. On wide screens the poster is a distinct left column and metadata/actions align beside it; on narrow screens the poster leads, followed by title, actions, rating, and readable plot. A back link stays above the movie information and preserves the originating library query.

```text
┌──────────────────────────────────────────────────────────────────────┐
│ LamuFlix     Library   Watchlist   Import folder        Search…     │
├──────────────────────────────────────────────────────────────────────┤
│ ‹ Back to library                                                     │
│ ┌───────────┐   Arrival                               [Watchlist]     │
│ │           │   2016  ·  1h 56m                       [Play]          │
│ │  poster   │   IMDb 7.9   Metacritic 81                              │
│ │           │   Director · Cast                                       │
│ └───────────┘   Plot text in a readable measure…                       │
└──────────────────────────────────────────────────────────────────────┘
```

Alignment is left-first throughout. Cards are visually quiet and rely on poster art; their metadata sits on the shared raised surface. Chips encode active query values and have individually named removal actions. State messages explain the condition and the next available action in plain language.

### Contrast notes

Measured against current proposed token pairings: warm white on page ground is 15.19:1; secondary text on raised surface is 5.90:1; amber on page ground is 8.04:1; amber on warm white is 1.89:1; quiet rule on raised surface is 1.19:1. Therefore filled amber actions use dark text and quiet rules do not serve as action boundaries. Verify final foreground/background values and focus/non-text boundaries in implementation before marking visual acceptance complete.

## 2. Interaction principles

- Library search and filters are live; search remains debounced. The mobile drawer does not introduce a staged edit transaction.
- The shell has the single library-title search control. Keep the existing cast-option search within the cast facet; it narrows available cast choices and does not search movie titles. Sorting appears only in the results toolbar.
- One library query representation feeds controls, chip summaries, results, and URL serialization. Details keeps its search draft local until Enter.
- Route transitions preserve the full originating library URL, including unknown query keys handled by the existing query parser.
- Responsive panels are transient; filter values are represented by the URL and survive reloads, panel state does not.
- Keyboard behavior is deliberate: focus is visible everywhere; the modal drawer traps and returns focus; scroll locking is released on every exit and cleanup.
- Missing information is shown as an intentional placeholder or concise state, never an empty visual hole or broken image.

## 3. Existing component relationships to preserve

- `web/src/App.tsx` owns the app entry today and chooses Library versus Details; it is the shared-shell and reactive-route integration point.
- `web/src/library/LibraryView.tsx` composes `FilterPanel`, query-backed results, `MovieCard`, `MovieGrid`, pagination, bookmark, import row, and retry behavior.
- `web/src/features/library/FilterPanel.tsx` owns filter controls today; `filterQuery.ts` parses, canonicalizes, and serializes existing query keys; search behavior is supported by `useDebouncedSearch.ts`.
- `web/src/features/movie-details/MovieDetailsView.tsx` and `.css` currently present details outside the shared shell. The view already handles loading, missing/error detail, optional metadata, watchlist, Play, and 403 dialog states.
- `web/src/index.css` is the global Tailwind import and existing grid rule. `web/package.json` already installs Tailwind 4; no design-system dependency is needed.

## 4. Acceptance review

Review the implemented shell at 375px, 768px, and 1440px, including the exact 767/768 transition. Check keyboard focus visibility, drawer focus containment/return, body-scroll cleanup, contrast pairs, direct detail routes, filter chips, query preservation, browser history, and missing media. Capture a stable populated library screenshot for the DEV-23 README handoff. The review is planned acceptance evidence; this design artifact does not claim those checks have run.
