# Design: DEV-321 — Library browse view

**Branch**: `feature/321-spec` | **Date**: 2026-10-11 | **Seat**: Palette (Designer)

**Authority**: `specs/DEV-321/brief.md` (grill closed Q10/12), `spec.md` (FR-001–FR-031), `plan.md`, `tasks.md`, `clarify.md`, `checklists/requirements.md`, `ASSUMPTIONS.md` (taste), `recon-DEV-321` sections 9–17. Ticket body `DEV-321:19-35`.

**Scope of this document**: presentation design only — route map, layout wireframes, component tree, component states, data needs, keyboard/a11y. Every scope and behavior decision is frozen in `brief.md` and restated here only where the UI binds to it. Nothing in this document changes the contract, the file envelope (brief Q8), fetching semantics (Q6), URL semantics (Q2/Q3), or any acceptance criterion. Copy and label choices marked *taste* are presentation refinements inside frozen behavior (brief Q9: "Palette may refine presentation within frozen behavior"); they want `[assumed]` logging in `ASSUMPTIONS.md` in a later authorized edit (this ask authorizes this file only).

---

## 1. Design direction

Subject: a household's own film library — a personal media collection, not a storefront. The browse view is a working tool whose hero is the poster wall itself (Q7: no marketing hero, no invented performance target). Chrome stays quiet so poster art carries the color.

**Palette tokens** (view-local; CSS custom properties in `web/src/index.css`):

| Token | Hex | Role |
| --- | --- | --- |
| `--booth` | `#16181D` | page ground, a blue-slate screening-room dark |
| `--reel` | `#20242B` | raised bands (filter band, skeleton base, inputs) |
| `--aperture` | `#2B313B` | rules, input borders, tile caption dividers |
| `--ink` | `#F0EDE6` | primary text (warm paper-white) |
| `--dim` | `#98A0AC` | secondary text (catalogue line, helper text) |
| `--amber` | `#E0A34A` | single accent — projector lamp: focus rings, active page, primary buttons, IMDb badge |

Status chips use text plus a quiet dot, never color alone (a11y): Pending `#8A93A0`, Enriched `#7FA06A`, NotFound `#98A0AC`, Failed `#C4614F`, each on a 10% tint of itself over `--reel`.

**Type** (no new dependency, no font files — the Q8 envelope has room for none, and a self-hosted media app should not fetch fonts at runtime):

- Display: `Iowan Old Style, Palatino Linotype, Palatino, Georgia, serif` — wordmark "LamuFlix" and the page heading "Library" only.
- UI: `ui-sans-serif, system-ui, Segoe UI, Roboto, Helvetica Neue, Arial, sans-serif` — controls, titles, meta. Titles at 15px/650; body 14px/400; catalogue meta 12px/400 with `font-variant-numeric: tabular-nums` so year, runtime and rating align down the wall.
- Sentence case everywhere. No all-caps labels, no tracked eyebrows, no middle-dot or em-dash meta joiners (meta items are separated by spacing and a 1px `--aperture` divider).

**Layout concept**: a catalogue wall. One quiet header band (wordmark, result count), one filter band, then a fluid poster wall of 2:3 frames, then a pagination footer. Chrome is left-aligned; the wall is fluid auto-fill; captions are left-aligned with tabular figures. Nothing shifts between states: the media slot is always 2:3, captions reserve two title lines plus two fixed meta rows, skeletons occupy exactly the final tile geometry.

**Memorable moment**: the title-card placeholder. A movie with no poster (or a broken image) shows its title set in the frame like a silent-film title card, at the same 2:3 ratio — so an imperfect library still reads as a designed contact sheet, and skeletons resolve into posters without one pixel of movement.

**Principles**: posters carry the color; chrome never competes; one accent used only for focus, active and primary; metadata reads as a catalogue line; status is a stamp with its exact label; every state explains itself in plain verbs.

---

## 2. Route map and URL structure

No router (brief Q2: native History API only; the feature URL adapter is sole owner of history and URL state).

| Route | View | Notes |
| --- | --- | --- |
| `/` (SPA root) | Library browse (this ticket) | The only route. All state lives in the query string (brief Q7). |

**URL shape**: `/?<canonical query>` — lowercase keys, ampersand-joined, keys and values encoded with parity to the server codec (Q3). `sort`, `direction`, `page`, `pageSize` are always emitted; lists emit one repeated key per value; the runtime group (`runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown`) and the year group (`yearMin`, `yearMax`) are emitted complete or omitted entirely.

**Canonical keys** (FR-009): `text`, `genreIds`, `actorIds`, `runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown`, `yearMin`, `yearMax`, `statuses`, `inWatchlist`, `sort`, `direction`, `page`, `pageSize`.
Sort names `Title`, `Year`, `Rating`, `Runtime`; direction names `Ascending`, `Descending`.
Read-only aliases (never emitted): `genres` → `genreIds` (`genreIds` wins on conflict), `Asc`/`Desc` → `Ascending`/`Descending` (FR-010).

**Examples**:

```text
Default entry (initial load normalizes to this and replaces the entry):
  /?sort=Title&direction=Ascending&page=1&pageSize=24

Ticket-example bookmark (DEV-321:29) before normalization:
  /?text=heat&genres=4&sort=Year&direction=Desc&page=1
After read + alias/default normalization (replaceState, aliases never emitted):
  /?text=heat&genreIds=4&sort=Year&direction=Descending&page=1&pageSize=24

List and group shapes:
  /?genreIds=4&genreIds=7&statuses=Enriched&statuses=Failed&runtimeMin=&runtimeMax=&runtimeIncludeUnknown=false&sort=Rating&direction=Descending&page=2&pageSize=24

Valid but server-rejected values stay in the URL (no SPA validation, FR-014) and yield the 422 state:
  /?yearMin=2000&yearMax=1990&sort=Title&direction=Ascending&page=1&pageSize=24
```

History semantics (FR-012, frozen): user changes push and update adapter state immediately; initialization and alias/default/malformed normalization replace; `popstate` restores all controls and query state without a push; the listener is removed on cleanup. Filter/sort edits reset `page` to 1; pagination changes `page` alone (FR-013). A valid page past the end stays in the URL unchanged (FR-027).

---

## 3. Page layout

One page archetype (browse grid). Text wireframes are design intent, not pixel specs.

### 3.1 Desktop / wide

```text
┌──────────────────────────────────────────────────────────────────────────────────┐
│  LamuFlix                                                  271 titles            │  header band
├──────────────────────────────────────────────────────────────────────────────────┤
│  Search [_______________________] [Search]   Sort by [Title ▾]  Direction [Ascending ▾]   [Clear filters] │
│  Enrichment status  [ ] Pending  [ ] Enriched  [ ] NotFound  [ ] Failed           │
│                       Watchlist   ( ) Any  (•) In watchlist  ( ) Not in watchlist │
│  Runtime (minutes) [____] – [____]  [ ] Include unknown     Year [____] – [____]  │
│  Genre IDs [ 4, 7 ]   Actor IDs [ 12 ]                                  [Apply filters] │
├──────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│   ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐         │
│   │        │  │        │  │        │  │        │  │        │  │        │         │
│   │  2:3   │  │  2:3   │  │  2:3   │  │  2:3   │  │  2:3   │  │  2:3   │         │
│   │ poster │  │ poster │  │ poster │  │ poster │  │ poster │  │ poster │         │
│   │        │  │        │  │        │  │        │  │        │  │        │         │
│   └────────┘  └────────┘  └────────┘  └────────┘  └────────┘  └────────┘         │
│   Heat         The Thing    Yi Yi      Chungking   Aftersun    Portrait           │
│   2010 | 124 min  1982 | 109 min  …                                               │
│   [7.8] [Enriched]  [7.9] [Pending]  …                                            │
│                                                                                  │
│                      [Previous]   Page 3 of 12   271 titles   [Next]              │
└──────────────────────────────────────────────────────────────────────────────────┘
```

Grid: CSS grid, `grid-template-columns: repeat(auto-fill, minmax(9.5rem, 1fr))`, gap 1rem/1.25rem — six columns at typical desktop, collapsing fluidly. No card chrome around tiles (no border, no shadow): the poster is the tile.

### 3.2 Narrow (mobile)

```text
┌──────────────────────────────┐
│  LamuFlix     271 titles     │
├──────────────────────────────┤
│  Search [____________] [Search] │
│  Sort by [Title ▾]            │
│  Direction [Ascending ▾]      │
│  Enrichment status            │
│   [ ] Pending [ ] Enriched    │
│   [ ] NotFound [ ] Failed     │
│  Watchlist  ( ) Any (•) In ( ) Not │
│  Runtime (min) [__] – [__]    │
│   [ ] Include unknown         │
│  Year [__] – [__]             │
│  Genre IDs [ 4, 7 ]           │
│  Actor IDs [ 12 ]             │
│  [Apply filters] [Clear filters] │
├──────────────────────────────┤
│  ┌────────┐ ┌────────┐        │
│  │  2:3   │ │  2:3   │        │
│  │ poster │ │ poster │        │
│  └────────┘ └────────┘        │
│  Heat       The Thing         │
│  2010 | 124 min               │
│  [7.8] [Enriched]             │
│  ┌────────┐ ┌────────┐        │
│  …                            │
├──────────────────────────────┤
│  [Previous]  Page 3 of 12     │
│  271 titles        [Next]     │
└──────────────────────────────┘
```

Filter band stacks vertically; buttons go full width; grid runs two columns down to ~20rem, then one.

### 3.3 Tile anatomy (MovieCard)

```text
┌──────────────┐
│              │   media slot — reserved 2:3 (aspect-ratio CSS), shared by
│   poster /   │   poster image, title-card placeholder, and skeleton
│   title card │
│              │
└──────────────┘
Heat                          title — <h3>, clamp 2 reserved lines
2010 │ 124 min                catalogue line — dim, tabular numerals, 1px divider
[7.8]  [● Enriched]           IMDb badge (amber pill) + status stamp (exact label)
```

- Missing year/runtime/rating renders `—` (em dash) in its own slot (ASSUMPTIONS Q7), so the line never reflows.
- Runtime always renders as `<n> min` when present; IMDb badge always one decimal (e.g. `7.8`).
- Status stamp text is exactly `Pending`, `Enriched`, `NotFound`, `Failed` (FR-026).
- No detail, play, or watchlist-mutation controls on the tile (Q7); tiles are not focusable.

### 3.4 State wireframes

```text
Skeleton tile (24 of them on any normalized-key change):
┌──────────────┐
│ ░░░░░░░░░░░░ │   solid --reel block at exact 2:3
└──────────────┘
░░░░░░░░░░░     title bar (2 reserved lines)
░░░░░░░░        catalogue bar
░░░░░           badge + stamp bars

Title-card placeholder (null or broken poster, same 2:3):
┌──────────────┐
│              │
│     Heat     │   title centred in the frame (accessible text = the title)
│              │
└──────────────┘

Empty library (totalCount = 0, no active filters):
  Your library is empty.
  Titles added to your library will show up here.
  [Clear filters]

Filtered empty (totalCount = 0, filters active):
  No titles match these filters.
  [Clear filters]

Past-end page (page > 1 and page > ceil(totalCount/pageSize); URL unchanged):
  This page is past the end of the results.
  [Go to page 1]        (+ [Clear filters] as well when totalCount = 0)

Invalid filters (422 from the server):
  These filters can't be applied.
  The server rejected the filter values in the URL. Clear the filters to start over.
  [Clear filters]                    (no Retry — 422 is never retried, FR-023)

Recoverable fetch failure (network, 400, 5xx):
  Couldn't load your library.
  Something went wrong while loading the titles.
  [Retry]
```

Copy is *taste* (plain verbs, sentence case, direction over mood).

---

## 4. Component tree

Exactly the nine approved feature-local files (brief Q8) — no additional component files. Header band, chips, badges, skeletons and title cards are markup and CSS classes inside these files.

```text
web/src/main.tsx
└─ QueryClientProvider                       (single instance, brief Q5)
   └─ web/src/App.tsx
      ├─ useLibraryUrl()                     (called ONCE here — sole history/URL owner, FR-024)
      └─ LibraryErrorBoundary                (hand-written class boundary)
         │   props: resetKey = serialized normalized browse state (query key)
         └─ LibraryView.tsx                  (owns controls/grid/pagination, FR-024)
            │   props: state + actions from useLibraryUrl (applyFilters, clearFilters, setPage)
            ├─ LibraryFilters.tsx            (text, sort, direction, statuses, watchlist,
            │                                 runtime + include-unknown, year, genre/actor IDs,
            │                                 Search/Apply filters/Clear filters)
            ├─ MovieGrid.tsx                 (responsive 2:3 wall; skeletons; empty/filtered-empty/
            │                                 past-end states)
            │   └─ MovieCard.tsx × items.length
            └─ Pagination.tsx                (Previous | Page x of y | count | Next)

web/src/library/libraryUrl.ts                (pure codec: parse → normalized state,
                                              serialize → canonical query string)
web/src/library/useLibraryUrl.ts             (history ownership: initial bookmark read, push on
                                              user change, replace on normalization, popstate
                                              restore, listener cleanup; exposes state, actions,
                                              and typed lowercase-key → generated PascalCase
                                              API-parameter mapping)
web/src/library/useBrowseMovies.ts           (one browse query keyed by the complete normalized
                                              filter/page state; merged generated client;
                                              AbortSignal propagated; retries off)
```

**Ownership** (frozen, FR-024): App composes providers and the boundary and instantiates the adapter once; the URL adapter owns all history and URL state; LibraryView owns controls, grid and pagination. `useBrowseMovies` is consumed by LibraryView only. `web/src/api/client.ts` and `types.ts` are consumed, never re-implemented (brief Q5).

**Data flow**: `useLibraryUrl` → normalized state → (a) `resetKey` to the boundary, (b) `LibraryView` props, (c) typed API params → `useBrowseMovies` → generated client `BrowseMovies` with `AbortSignal` → `MovieGrid`/`Pagination`.

---

## 5. Component states

| Component | Loading | Empty | Error | Partial data |
| --- | --- | --- | --- | --- |
| `LibraryErrorBoundary` | pass-through (children render) | — | Render/query failure → fallback panel. 422 → invalid-filter panel, never retried. Network/400/5xx → recoverable panel with Retry. Render failures are treated as recoverable (Retry offered). Retry resets the boundary and refetches the same canonical URL without a history entry. Resets automatically when `resetKey` (query key) changes. | — |
| `LibraryView` | shows skeletons via `MovieGrid` during a key-change fetch | composes empty states of `MovieGrid` | children replaced by boundary fallback | always shows controls with current URL values |
| `LibraryFilters` | never blocks; stays operable during fetches | n/a | n/a | every control reflects the normalized state, including absent optionals and a past-end `page` (page is not a control; it lives in `Pagination`) |
| `MovieGrid` | 24 reserved-size skeleton tiles on any normalized-key change; same-key background refetch keeps current tiles and announces politely | empty-library or filtered-empty panel per §3.4 (clear-filters offered in both, Q7) | inherited from boundary | tile list shorter than `pageSize` on the last page is normal, not partial |
| `MovieCard` | skeleton geometry (inside `MovieGrid`) | n/a | poster `onError` → title-card placeholder | null `posterUrl` → placeholder; null year/runtime/rating → `—` each; all scalars null still renders title + `—` line + status stamp |
| `Pagination` | shows during fetch (no disable; a key change replaces the grid, not the footer) | `totalCount = 0` → count reads `0 titles`, both directions disabled at page 1 | inherited from boundary | `Page x of y` with `y = ceil(totalCount/pageSize)` (min 1); Previous disabled at page 1; Next disabled at `page >= y`, including past-end pages |
| `useBrowseMovies` | TanStack `isLoading`/`isFetching` split: key change → skeletons; same-key refetch → retain content | returns `items: []` with `totalCount` | error object classified by HTTP status only (422 vs everything else), never by error-key vocabulary (FR-023) | stale in-flight responses for a superseded key are discarded (AbortSignal + keying, FR-022) |

Skeleton count is fixed at the active `pageSize` (24 by default) so the wall height is stable.

---

## 6. Data needs (endpoint + fields)

Consumption contract: generated TypeScript from `web/src/api/types.ts` and `web/src/api/client.ts` after the user merge of DEV-320 #124 (brief Q5). Exact generated symbol names are settled by Wisp's Phase B recon; this section binds the wire contract the UI depends on. No hand-rolled DTOs, no hand-edited generated files.

### 6.1 Browse query — `GET /api/movies` (OpenAPI operation `BrowseMovies`)

Request parameters (OpenAPI names are PascalCase; the typed adapter maps canonical lowercase URL keys onto them — the server query-parameter contract does not change):

| Canonical URL key | API param | Wire type | UI source | Notes |
| --- | --- | --- | --- | --- |
| `text` | `Text` | string \| null | Search field | absent = no filter; empty string is a distinct value (Q3) |
| `genreIds` | `GenreIds` | int[] (repeated) | Genre IDs entry | alias `genres` read-only on input |
| `actorIds` | `ActorIds` | int[] (repeated) | Actor IDs entry | |
| `runtimeMin` | `RuntimeMin` | int \| null | Runtime min | group with the two below; empty bound = unbounded |
| `runtimeMax` | `RuntimeMax` | int \| null | Runtime max | |
| `runtimeIncludeUnknown` | `RuntimeIncludeUnknown` | bool | Include unknown checkbox | emitted lowercase `true`/`false` |
| `yearMin` | `YearMin` | int \| null | Year min | group with `yearMax` |
| `yearMax` | `YearMax` | int \| null | Year max | |
| `statuses` | `Statuses` | `EnrichmentStatus`[] (repeated) | Status multiselect | names `Pending`, `Enriched`, `NotFound`, `Failed` |
| `inWatchlist` | `InWatchlist` | bool \| null | Watchlist tri-state | absent = Any |
| `sort` | `Sort` | `Title` \| `Year` \| `Rating` \| `Runtime` | Sort by select | always emitted |
| `direction` | `Direction` | `Ascending` \| `Descending` | Direction select | always emitted; `Asc`/`Desc` accepted on read |
| `page` | `Page` | int ≥ 1 | Pagination | always emitted |
| `pageSize` | `PageSize` | int 1–100 | fixed 24 in UI | always emitted; a URL-supplied value is honored (FR-011) |

Response `200` — `BrowseMoviesResponse`:

| Field | Wire type | UI use |
| --- | --- | --- |
| `items` | `MovieSummaryResponse[]` | `MovieGrid` → `MovieCard` |
| `totalCount` | int | `Pagination` (`ceil(totalCount/pageSize)`), result count, empty/past-end detection |

`MovieSummaryResponse` (additive widening, brief Q1):

| Field | Wire type | UI use |
| --- | --- | --- |
| `id` | int (required) | React key |
| `title` | string (required) | tile title, placeholder accessible text |
| `posterUrl` | string \| null | `MovieCard` image; null → placeholder |
| `releaseYear` | int \| null (calendar year) | catalogue line; null → `—` |
| `runtime` | int \| null (minutes) | catalogue line as `<n> min`; null → `—` |
| `imdbRating` | number \| null (0–10, ≤ 1 decimal) | IMDb badge, one decimal; null → `—` |
| `enrichmentStatus` | `Pending` \| `Enriched` \| `NotFound` \| `Failed` (required) | status stamp, exact label |

Null scalar members arrive as JSON null and are present, never omitted (FR-002). The four-name string schema must appear in `web/src/api/openapi.json` and the generated TS (FR-006; conditional `Program.cs` schema transformer only if regeneration lacks it, T015).

### 6.2 Error shapes

| Status | Body | UI state | Behavior |
| --- | --- | --- | --- |
| `422` | `application/problem+json`, field-keyed `errors` (malformed, out-of-range, cross-field-invalid all land here — recon §16) | invalid-filter panel | never retried (FR-023) |
| `400`, `5xx`, network | any / none | recoverable fetch panel | Retry resets/refetches the same canonical URL, no history entry |
| render crash | — | recoverable panel | Retry (see §5) |

Classification is by HTTP status only — never by the field-key vocabulary of the error body (`page` vs `Query.Page.Size`, recon §16f).

---

## 7. Keyboard navigation and accessibility

- **Labels**: every control has a persistent visible label (Search, Sort by, Direction, Enrichment status, Watchlist, Runtime (minutes), Include unknown, Year, Genre IDs, Actor IDs). Helper text ("Comma-separated numeric IDs") is supplementary, never the label. IMDb badge exposes "IMDb rating 7.8"; status stamps expose their exact label as text.
- **Keyboard**: search is a form — Enter submits (never per keystroke, ASSUMPTIONS Q7). Sort/Direction are native selects. Statuses are a checkbox group in a `fieldset`/`legend`. Watchlist tri-state is a radio group (Any / In watchlist / Not in watchlist). Numeric bounds and ID entries are text/number inputs with `inputmode="numeric"`. Apply filters, Clear filters, Search, Retry, Go to page 1, Previous, Next are native buttons. Tab order follows visual order: header → filter band → pagination (tiles carry no interactive elements and are skipped).
- **Focus**: `:focus-visible` ring `2px solid var(--amber)` with 2px offset on every interactive element; never removed. Keyboard users reach every filter, both pagination directions, and every state action (Retry / Clear filters / Go to page 1).
- **Announcements**: one polite live region (`aria-live="polite"`) in `LibraryView` announces loading ("Loading titles…") on a key-change fetch and result summaries on completion ("271 titles", "Page 3 of 12"). Same-key background refetch announces politely and keeps current tiles (ASSUMPTIONS Q6).
- **Images and placeholders**: poster `alt` carries the movie title; the title-card placeholder's accessible text is the movie title (ASSUMPTIONS Q4). Placeholder tiles may announce the title twice (frame + caption); that duplication is accepted to keep the frozen placeholder accessible-text rule and one consistent caption line.
- **Structure**: `header`/`main`/`footer` landmarks around the three bands; the wall is a list (`ul`/`li`) of `article` tiles with an `h3` title each; `Pagination` sits in a `nav` with an accessible name ("Library pages").
- **Contrast and motion**: `--ink`/`--dim` on `--booth`/`--reel` meet WCAG AA; status is never color-alone; no entrance choreography (one optional 120ms fade on skeleton→poster resolution, disabled under `prefers-reduced-motion`, which is honored for all transitions).
- **No SPA validation**: no `min`/`max`/`pattern` on inputs — out-of-range and cross-field-invalid values pass to the server and surface as the 422 state (FR-014, brief Q2). Bounds are shown only as neutral helper text where useful ("minutes"), never as enforced client rules.
- **Responsive**: the wall is fluid from one to six-plus columns; the filter band stacks on narrow viewports; reserved 2:3 slots prevent layout shift at every width (ticket AC "grid renders smoothly with responsive layout").

---

## 8. Presentation notes for implementation

- All tokens and layout live in `web/src/index.css` per brief Q5; `web/src/library/*` files carry markup and behavior. No CSS framework, no styling package (Q5: only `@tanstack/react-query` is added).
- Sub-elements of the approved files (header band, chip, badge, skeleton block, title card, live region) are CSS classes/markup, not new components — the file envelope stays exact.
- `LibraryErrorBoundary` is a hand-written class component (React error boundaries require one); no error-boundary package (Q6).
- Reserved geometry: media slot `aspect-ratio: 2/3`; caption reserves two title lines and two meta rows so equal-height tiles align as a contact sheet.
- This document introduces no global design system. `specs/DESIGN.md` (Sprint 7, DEV-20) is **not present in these roots**; when it lands, these view-local tokens map onto it without any behavior change.
- *Taste* items in this document (palette, type stacks, chip colors, all copy strings, skeleton count, grid min column) are presentation refinements inside frozen behavior; they are not logged in `ASSUMPTIONS.md` yet because this ask authorizes `design.md` only.
