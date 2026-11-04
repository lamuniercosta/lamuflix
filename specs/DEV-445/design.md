# DEV-445 design — Collection facet healthy-empty hide

Status: draft for `ui:` plan challenge. Sources: `specs/DEV-445/spec.md` (US3, FR-009),
`specs/DEV-445/brief.md` Q3, `specs/DEV-445/ASSUMPTIONS.md`, `specs/PRODUCT.md` §4.
Mode: Operate refinement — incumbent identity, classes, and copy are preserved; no new
visual world, no new labels or copy (frozen scope, brief Q5).

## 1. Route map and URL structure

No new routes. Two existing routes are in play:

- `/` — library browse (`isLibraryRoute('/')` is true; everything else falls back here).
  `App.tsx` parses `route.search` into `FilterQuery` and renders `LibraryView`, which owns
  the filter state, the active-filter chips, and the results grid.
- `/movies/:id` — movie details. `DetailsBackLink` returns to the stored library URL,
  preserving the full query string including `collectionId`.

URL is the single source of truth for the active collection filter. `FilterPanel` commits
through `toUrlQuery` + `history.replaceState` (non-integrated) and `onChange`/
`onQueryChange`; `collectionId` survives a hidden facet because nothing in this change
writes to the query (ASSUMPTIONS.md: removal stays with the active-filter chip and
Clear All).

## 2. Per-page layout (text wireframes)

Library browse, filter panel open. Only the Collection group changes; all other groups
keep position, order, and copy. The chip row never co-renders with the open panel:
chips render only when the panel is closed with active filters.

Healthy empty — Collection group absent, grid unaffected, no chip row while panel open:

```text
+------------------------------------------------------+
| Library toolbar: search, filter toggle (N active),   |
| sort, Clear All                                      |
+------------------------------------------------------+
| Filter panel (fieldset stack, unchanged order):      |
|   [Search titles]                                    |
|   [Genres]                                           |
|   -- no Collection group here when healthy-empty --  |
|   [Cast]                                             |
|   [Director]                                         |
|   [Runtime]                                          |
|   [Release year]                                     |
|   [IMDb rating]                                      |
|   [Enrichment status]                                |
|   [Sort]                                             |
|   footer                                             |
+------------------------------------------------------+
| Results grid (unchanged)                             |
+------------------------------------------------------+
```

Populated — Collection group renders exactly as today (unchanged ordering, labels, copy):

```text
|   [Collection]                                       |
|     ( ) James Bond Collection  [12]  ( ) ...         |
```

Fetch error — Collection group renders exactly as today, with the existing alert:

```text
|   [Collection]                                       |
|     (!) Collections could not be loaded. (role=alert)|
```

Stale selection — facet hidden AND `?collectionId=4` present (query retained; nothing
in this change writes to the query): panel open shows no Collection group and no chip
row (chips render only when the panel is closed). Panel closed shows the chip fallback
exactly `Collection: Collection 4` with remove label `Remove collection filter
Collection 4`, and Clear All still clears it. Grid stays filtered until the user
removes the chip.

## 3. Component tree

```text
App (route.search -> FilterQuery)
└─ LibraryView
   ├─ toolbar (search, sort, Clear All, active-filter count)
   ├─ chips (only when panel closed with active filters):
   │   .library-chip-list [aria-label="Active filters"]
   │   └─ chip[key=collection] "Collection: {name}"
   │       aria-label "Remove collection filter {name}"
   │       -> next.collectionId = null
   ├─ FilterPanel (query, collections, collectionsError, integrated, ...)
   │   ├─ Search titles (unchanged)
   │   ├─ fieldset Genres (unchanged)
   │   ├─ render Collection fieldset unless healthy-empty
   │   ├─ fieldset.lamu-filter-group > legend "Collection" (only when visible)
   │   │   ├─ p[role=alert].lamu-filter-error (collectionFailed only,
   │   │   │   copy unchanged: "Collections could not be loaded.")
   │   │   └─ .lamu-chip-row > label.lamu-chip > input[type=checkbox].sr-only
   │   │       + span name + span.lamu-chip-count[aria-hidden] + span.sr-only ", N movies"
   │   ├─ fieldset Cast (unchanged)
   │   ├─ fieldset Director (unchanged)
   │   ├─ fieldset Runtime (unchanged)
   │   ├─ fieldset Release year (unchanged)
   │   ├─ fieldset IMDb rating (unchanged)
   │   ├─ fieldset Enrichment status (unchanged)
   │   ├─ fieldset Sort (unchanged)
   │   └─ footer (unchanged)
   ├─ ResultsBody (grid, unchanged)
   └─ ForYouRail (only when no active filters and no watchlist, unchanged)
```

Render rule (single condition in `FilterPanel.tsx`, beside the existing
`collectionFailed` derivation and the Collection `<fieldset>`): render the Collection
`<fieldset>` unless healthy-empty — omit the whole `<fieldset>` (legend included) iff
`sortedCollections.length === 0 && !collectionFailed`. Sorting stays `sortCollections`
(name, then numeric id); toggle stays `toggleCollection`
(`collectionId = on ? id : null`).

## 4. Component states

| State | Collection group | Notes |
|---|---|---|
| Loading (collections fetch in flight) | Hidden | No `collectionsLoading` prop exists today and none is added; `data` undefined coerces to `[]` with no error, so the group mounts once data arrives. Panel and grid stay interactive meanwhile. |
| Healthy empty (`[]`, no error) | Hidden (the change) | Grid, chips, and all other groups unchanged. |
| Populated | Visible, unchanged | Existing name-then-id ordering, `lamu-chip` / `lamu-chip-selected` classes, count badges. |
| Fetch error | Visible with alert | Existing `role="alert"` copy preserved; legend stays so the error keeps its group context. |
| Partial data (collections ok, another facet failed) | Independent | Each facet keeps its own error branch; Collection visibility depends only on collections state. |
| Stale `collectionId` + hidden facet, panel open | Hidden facet, no chip row | Query retained (`?collectionId=4` present); grid remains filtered; chips render only when the panel is closed. |
| Stale `collectionId` + hidden facet, panel closed | Hidden facet, chip fallback exactly `Collection: Collection 4` | Remove label exactly `Remove collection filter Collection 4`; chip remove and Clear All clear it; grid stays filtered until removed. |

No skeleton, spinner, or empty-legend copy is introduced: the obsolete empty-legend
assertion in `FilterPanel.test.tsx` is replaced by hidden-state coverage (brief Q4).

## 5. Data needs (endpoint + fields)

Web code consumes generated types only (`components['schemas'][...]` from
`web/src/api/types.ts`); no hand-rolled duplicates, no contract change (spec FR-012).

- `GET /api/collections` → `CollectionDto[]`: `id: number | string`, `name: string`,
  `movieCount: number | string` (`web/src/api/types.ts`, `CollectionDto`; operation
  `GetCollections`, `openapi.json` `/api/collections`). Fetched in `LibraryView`
  (`useQuery(['collections'])`, error message `Collections could not be loaded.`),
  passed as `collections` / `collectionsError` props into `FilterPanel`, memoised to
  `sortedCollections`. This is the only data this change reads.
- `GET /api/movies?CollectionId=…` → `BrowseMoviesResponse` (`items:
  MovieSummaryResponse[]`, `totalCount`) — existing browse filtering; unchanged, listed
  only because the hidden facet must not disturb it.

## 6. Keyboard navigation and accessibility

- Hidden means unmounted, not `aria-hidden`: when healthy-empty, the Collection group
  leaves the accessibility tree entirely, so screen-reader and keyboard users meet no
  phantom group. No focus management change is needed.
- When visible, semantics are untouched: real `<fieldset>`/`<legend>`, real checkboxes
  (visually hidden `.sr-only` inputs inside labels), error exposed via `role="alert"`.
- Keyboard: `Tab` reaches each collection checkbox (`Space` toggles); the toolbar chip
  remove control and Clear All are buttons (`Enter`/`Space`); existing `inert={!panelOpen}`
  collapse behavior and `onCollapse` are unchanged.
- Names: chip remove buttons keep `Remove collection filter {name}`; count badges stay
  `aria-hidden` with the `, N movies` screen-reader suffix.
- Visible focus, contrast, responsive collapse, and reduced-motion behavior all ride the
  existing `lamu-*` classes — this change adds no new controls, so no new focus or motion
  treatment is specified. Checklist against `web-design-guidelines` (keyboard, visible
  focus, semantic controls, loading/error/empty states) is satisfied by preservation:
  every applicable requirement keeps its existing, already-reviewed implementation.
