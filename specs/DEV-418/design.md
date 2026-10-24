# DEV-418 Design - For You rail above the Library browse grid

Status: Phase A design, Palette-owned. Required before plan challenge (brief Q7).
Branch: `feature/418-spec`. Base: `7c7b4079f95568bbbeffa61d0a0ea76dc0fb4947`.
Sources: frozen `brief.md` (grill closed Q8), `spec.md`, `plan.md`, `tasks.md`,
`CONCLUSIONS.md` Q1-Q8, `ASSUMPTIONS.md` Q4-Q5, `specs/PRODUCT.md`, note
`recon-DEV-418` including grill supplements 1-2. Incumbent visual truth read
from shipped sources this session: `web/src/ui/MovieCarousel.tsx`/`.css`,
`web/src/library/MovieCard.tsx`, `web/src/library/LibraryView.tsx`,
`web/src/library/library.css`, `web/src/features/movie-details/RelatedMovies.tsx`/`.css`,
`web/src/index.css`.
Skills: `frontend-design` (design method), `impeccable` (Operate-mode UX
critique; context launcher not run under the seat write-boundary fallback,
incumbent context read directly), `web-design-guidelines` (review checklist;
applicable requirements recorded below). Receipts at the end of this file.
Binding constraints: brief Q1-Q8 and spec FR-001-FR-015 are frozen product
behavior. This design owns responsive layout, reason placement, and
keyboard/focus/visual details only (brief Q5) and changes none of those
decisions. The only copy in the feature is `For You` (Patron `[assumed]`,
ASSUMPTIONS.md Q5); every other string on screen is existing MovieCard copy or
API text.
Phase B STOP: build waits until DEV-357 has landed and the C# DTO ->
`web/src/api/openapi.json` -> generated TypeScript/MSW chain is available and
checked for drift at the picked HEAD (brief Q1, Q7; tasks T001). Missing or
drifted contract is STOP, never permission for substitutes.

## Design plan (frontend-design pass 1)

LamuFlix is a personal library, not a storefront: shelves of films the patron
already owns. The existing screening-room tokens stay authoritative
(`--color-ink`, `--color-dim`, `--color-boundary`, `--color-reel`,
`--lamu-space-*`, `--lamu-radius-media` in `web/src/index.css`). This ticket
adds one shelf to an existing wall, so the design is a refinement of the
incumbent world, not a new visual system.

One expressive surface carries the design: the returned reason chips under each
poster - verbatim caller-safe text ("Because you watched ...", "Same director")
that makes the shelf personal. The chips are the existing
`.related-movie-reasons` treatment emitted by unchanged MovieCard; nothing
around them is new. The shelf itself is the memorable moment: ten films chosen
for this patron, sitting above their own catalogue wall with their reasons
printed underneath.

- Color: host tokens only. No new hex value, no gradient, no card shadow, no
  second accent. The flat chip field (boundary at low opacity) is the single
  tinted surface, exactly as on the details-page shelf.
- Type: host stack only. The shelf label reuses the host's one shelf-stamp
  voice (the `details-section-heading` values: 0.75rem / 600 / 0.08em
  tracking / uppercase / `--color-boundary`) - the single pre-existing
  uppercase exception in the product (kept by DEV-343 "for consistency").
  Card titles and meta are unchanged MovieCard. No display face, no
  single-word accenting.
- Layout: one horizontal shelf band between the Library filter band and the
  Results wall. Left-aligned stamp, MovieCarousel's existing right-aligned
  Previous/Next row, then the fixed-geometry poster track. Centered or
  justified content is rejected; the library reads left to right.
- Principles: the rail is a shelf, not a deck of cards. Posters keep the
  theatrical 2:3 ratio and separate by whitespace, not borders. Text never
  decorates: every string is ticket copy, API text, or existing MovieCard
  copy.

## Uniqueness review (frontend-design pass 2)

Reviewed against the generated defaults and cut: no identical rounded SaaS
cards with soft shadows (cards keep MovieCard's borderless poster-first
anatomy); no gradient wash; no numbered eyebrow markers (the shelf is not a
sequence); no middle-dot meta strings or spaced-em-dash labels (MovieCard meta
keeps its existing `rating | year` line); no arrow-suffixed links (cards are
native anchors); no staggered fade-in entrance (the shelf simply appears when
populated); no cold-start illustration or empty-state mood copy (forbidden by
brief Q3 and unnecessary - the shelf is absent, not apologetic); no
personal-vs-structural color coding (invented hierarchy - both kinds keep the
identical chip treatment already shipped on the details shelf). The uppercase
shelf stamp is reused house style for this exact widget archetype, not a new
eyebrow system.

## Route map and URL structure

No new route, no router, no URL state (brief Q2, Q5; Proposed ADR-0021 in
`specs/DEV-321` continues to own browse URL semantics and is untouched).

| Route | View | Notes |
| --- | --- | --- |
| `/` (SPA root) | Library browse (host) | The For You shelf is a band inside this view, above Results. |
| `/movies/{id}` | Movie Details (host) | Card activation target via LibraryView's existing `openMovie` callback. |

- The shelf is a function of eligibility and one fetch; it is not
  deep-linkable state. No query-string key is added or read (deviation from
  "URL reflects state" is deliberate: the only state here is the fixed
  `take=10` request and the already-URL-reflected filter/watchlist state).
- Card activation: plain primary activation runs LibraryView's existing
  `onOpenMovie`; modified clicks keep the native anchor behavior of
  `<a href="/movies/{id}">` (unchanged MovieCard). The details-page history
  implementation (`libraryReturnUrl` handling in RelatedMovies) is not copied.

## Per-page layout (text wireframes)

One page archetype (browse grid). Text wireframes are design intent, not pixel
specs. Desktop, narrow, and hidden states share one structure; widths collapse,
order never changes.

### Eligible Library, populated shelf (desktop)

```text
[Genre tiles: (Action) (Comedy) (Drama) ...]                    (unchanged)

All movies                                        [Grid][List][Filters]
271 movies                                              (heading band, unchanged)

FOR YOU                                                 (shelf stamp: h2.for-you-heading,
                                        [Previous] [Next]  source text "For You")
+----------+ +----------+ +----------+ +----------+ +----
|   2:3    | |   2:3    | |   2:3    | |   2:3    | |      (MovieCarousel viewport:
|  poster  | |  poster  | |  poster  | |  poster  | |       horizontal scroll track)
+----------+ +----------+ +----------+ +----------+
Heat         The Thing    Yi Yi        Aftersun             (h3 title links)
7.8 | 2010   7.9 | 1982   - | 2000      8.1 | 2022           (IMDb | year)
[reason]     [reason]                                       (returned reason chips,
[reason]                                                    0..n, wrap, verbatim)
[Watched][Not interested]   ...                             (existing feedback actions)

+-------------------------------------------------------+
|  Results wall (unchanged): poster tiles, pagination   |
+-------------------------------------------------------+
```

### Rail card close-up (unchanged MovieCard anatomy)

```text
+------------------+
| poster 2:3       |   status stamp only for Pending / NotFound / Failed
| (or title-card   |   [watch] and [play] overlays (existing card chrome)
|  placeholder)    |
+------------------+
Heat                      h3 link: plain click -> openMovie,
7.8 | 2010               modified click -> native anchor
[Because you watched..]   reason chips: returned {kind,text} verbatim,
[Same director]           both kinds share the flat chip treatment
[Watched] [Not interested]  existing MovieFeedbackActions
```

`score` and `runtime` never render. A movie with no reasons shows poster,
title, and meta only.

### Narrow (mobile)

The heading band and filter band already stack (host, unchanged). The shelf
stays horizontal with touch scrolling; cards drop to 8.5rem so roughly
two-and-a-half posters show and the half card signals scrollability. Chips
wrap without limit. Previous/Next remain reachable but never replace swipe.

### Hidden states (ineligible, pending, initial failure, 200 [])

```text
[Genre tiles ...]                                    (unchanged)
All movies / 271 movies / controls                   (unchanged)
[filter panel and chips as today]                    (unchanged)
+-------------------------------------------------------+
|  Results wall (unchanged)                             |
+-------------------------------------------------------+
```

No shelf band, no heading stamp, no reserved space. The wall and its heading
band are the page, exactly as today.

## Component tree

```text
LibraryView.tsx (existing; bounded additive edit only)
  GenreTiles / library-heading / chips / FilterPanel (existing, unchanged)
  ForYouRail (new, web/src/features/for-you)          <- mounted only when
  |                                                      !activeFilters &&
  |                                                      !watchlistActive
  |   (brief Q3; LibraryView.tsx:275-276,360 existing calculations)
  +-> section.for-you-rail
  |     h2.for-you-heading  "For You"  (tabIndex -1; row-change focus anchor)
  |     MovieCarousel (web/src/ui, unchanged) label="For You"
  |       Previous / Next (existing icon buttons, right-aligned row)
  |       Viewport (focusable group, labelled "For You")
  |         ul > li * n > MovieCard (web/src/library, unchanged)
  |           className="for-you-card"
  |           movie={generated RecommendedMovieDto}   (direct, no adapter)
  |           reasons={movie.reasons}                 (direct, unchanged chips)
  |           onOpenMovie={LibraryView openMovie}
  |           feedbackClients={LibraryView's existing clients}
  +-> (focus-continuity cleanup lives inside ForYouRail; see A11y notes)

section[aria-label="Results"] (existing, unchanged; still holds refreshFailed
                               notice, ResultsBody, pagination)

web/src/features/for-you/forYouApi.ts (new)  - useForYou() TanStack Query
  |   consumer on the shared QueryClient via generated api/client.ts
  |   query key ['forYou', 10]; request take=10; signal forwarded; retry:false
web/src/features/for-you/ForYouRail.css (new) - exactly three rules:
  |   .for-you-rail (wrapper grid, gap --lamu-space-3), .for-you-heading
  |   (shelf-stamp values), .for-you-card (rail card geometry)
```

Ownership notes:

- `ForYouRail` owns eligibility-gated presence (host mounts it conditionally),
  quiet states, focus continuity, and card composition. It does not fetch
  beyond `forYouApi`, and `forYouApi` does not render.
- `ForYouRail.css` is required by this design and is limited to the three
  rules above. Rationale for named rail geometry instead of reusing the
  details-page class names (`related-movie-card`, `details-section-heading`):
  truthful names and no silent coupling to another feature's stylesheet if the
  details page is restyled. Chip styles stay the shared
  `.related-movie-reasons` treatment that unchanged MovieCard already emits
  (defined in `RelatedMovies.css`, in the single app bundle via static
  `App.tsx` imports today).
- Feed wiring per brief Q5/Q6: LibraryView passes its existing `openMovie`
  (`LibraryView.tsx:364`) and the feedback clients its cards already use
  (shared `QueryClient` library client, `MovieFeedbackActions.tsx:24`
  default). No new feedback or playback flow.

## Component states

Copy is fixed: only `For You` exists (ASSUMPTIONS.md Q5). Quiet-state behavior
is Patron `[assumed]` (ASSUMPTIONS.md Q4).

| Condition | Rail output | Notes |
| --- | --- | --- |
| Ineligible (`activeFilters` or `watchlistActive`) | Not mounted at all: no heading, no request, cached data invisible | Inline predicate `!activeFilters && !watchlistActive` (brief Q3, Q6). Watchlist-only also suppresses. |
| Eligible, pending, no prior nonempty success | Render nothing | No skeleton, spinner, toast, or reserved space. |
| Initial failure (400 / 422 / 5xx / network) | Render nothing | No rail error/retry widget. A query error is an error, never cold-start evidence. Browse grid is unaffected. |
| Success `[]` | Render nothing, heading included | Covers both no positive history and no eligible candidates; the UI never claims which (brief Q3). |
| Success nonempty | Stamp + carousel + cards in API order | Order preserved; no client sort. |
| Background refresh, prior nonempty data, still eligible | Prior nonempty shelf stays visible | Normal query lifecycle; no bespoke invalidation. |
| Background refresh returning `[]` | Whole shelf hides, heading included | Cached nonempty data is cleared from view. |
| In-flight completion while ineligible | Never revealed | Eligibility is a mount condition; late data may rest in cache for the next eligible mount. |
| Clearing filters / leaving watchlist-only | Normal eligible lifecycle resumes | Cached nonempty data may render immediately, then refresh. |
| Partial data (per card) | Independent fallbacks in unchanged MovieCard | null/broken `posterUrl` -> title-card placeholder; null year/rating -> `-`; empty `reasons` -> no chips; unknown reason kind -> base chip class; one bad card never breaks the shelf. |

## Data needs (endpoint + fields)

Single source, consumed never invented (brief Q1-Q2). Field names below are the
base-HEAD generated shape evidenced at `web/src/api/types.ts:151-165`
(`RecommendedMovieDto` = `MovieSummaryResponse` fields plus `score` and
`reasons`; `recon-DEV-418:127`). At base HEAD the for-you operation, query key,
handler, and fixture are absent (`recon-DEV-418:146,158,167,177-179`); Phase B
T001 verifies the landed contract and any drift is STOP.

- Endpoint: `GET /api/recommendations/for-you` (DEV-357; cited contract
  `specs/DEV-357/spec.md:79-87`), called through the existing generated
  openapi-fetch `client.GET` with `take: 10` and the query cancellation
  `signal`. Query key `['forYou', 10]`, `retry: false`, shared `QueryClient`.
  Take semantics (default/range, parser 400 vs validation 422 ProblemDetails)
  are server-owned; this consumer always sends the constant `take=10` and
  treats any consumer-visible failure quietly (FR-003, FR-006).

| Field | Wire type | UI use |
| --- | --- | --- |
| `id` | int | React key; card link target; existing openMovie target; feedback/play ids |
| `title` | string | h3 link text; poster `alt`; placeholder and control accessible names |
| `posterUrl` | string \| null | Poster image; null or broken -> MovieCard placeholder |
| `releaseYear` | int \| null | MovieCard meta line; null -> `-` |
| `imdbRating` | number \| null | MovieCard meta line; null -> `-` |
| `enrichmentStatus` | enum string | MovieCard status stamp (Pending / NotFound / Failed) and retry gate |
| `isInWatchlist` | bool | Existing watchlist toggle state on the card |
| `isWatched` | bool | Existing MovieFeedbackActions state |
| `isNotInterested` | bool | Existing MovieFeedbackActions state |
| `runtime` | int \| null | Present on the DTO; not rendered by unchanged MovieCard |
| `score` | number | Wire-only; never rendered |
| `reasons` | `{ kind, text }[]` | Chips, verbatim `text`, in returned order; `kind` lowercase `structural` / `personal`; both kinds share the flat chip treatment |

- Reasons semantics (brief Q3): show the returned text as-is; personal text
  appears only when the API returned kind `personal`; structural fallback and
  movies with no reasons are retained; never fabricate or relabel.
- Error shapes: `400`/`422` ProblemDetails (DEV-357-owned), `5xx`, and network
  failures all map to the quiet no-render state above. No error copy exists.
- Contract chain (constitution, `AGENTS.md`): C# DTO ->
  `web/src/api/openapi.json` -> generated `web/src/api/types.ts` and
  `web/src/mocks/handlers.ts` via `npm run generate:api`; generated outputs are
  never hand-edited. Fixtures in `web/src/mocks/fixtures.ts` are typed from the
  generated schema (brief Q6).

## Keyboard navigation and accessibility notes

Carousel and card behavior are inherited unchanged from the DEV-343 contract
(`MovieCarousel.tsx`, `MovieCard.tsx`); the notes below confirm them for this
composition and add the two rail-owned focus rules.

- Region and heading: the shelf stamp is an `h2` (`For You`, `tabIndex={-1}`)
  above `MovieCarousel`, whose `<section aria-label="For You">` and focusable
  `role="group"` viewport carry the accessible name `For You` from the `label`
  prop. Level matches the details-page shelf heading (h2); card titles stay
  `h3`. The host Results region keeps its own accessible name (`Results`) for
  region navigation; host heading structure is unchanged and out of scope.
- Keyboard: the viewport is the only Arrow-key surface
  (`ArrowLeft`/`ArrowRight` scroll only when the viewport itself has focus).
  Previous/Next are real buttons with accessible names, disabled at their
  scroll boundaries. Native card links stay in ordinary Tab order with
  standard Enter and modified-click behavior. No roving tabindex, no
  descendant-key interception, no autoplay.
- Images and text: poster `alt` carries the title (existing accepted
  duplication with the title link, as shipped on the wall and details shelf);
  the placeholder exposes `role="img"` with the title. Reason chips are inert
  text, never buttons or raw HTML. Poster overlays (watch/play) keep their
  existing accessible names.
- Focus continuity (rail-owned, both inside ForYouRail.tsx):
  1. Row-set change while mounted: if the focused card left the refreshed
     track, focus moves to the For You heading (`h2`, `tabIndex={-1}`) -
     the RelatedMovies pattern (`RelatedMovies.tsx:64-71`).
  2. Whole shelf unmounting while focus is inside it (background refresh
     returning `[]`, popstate filter restoration): focus moves to the Library
     heading `h2.library-title` as the nearest stable landmark. The lookup is
     scoped from the rail's own DOM subtree, makes the heading programmatically
     focusable at that moment (`tabindex="-1"`), and degrades to a no-op if the
     host structure differs. No rail UI is rendered in hidden states; this is
     cleanup only.
- Quiet means quiet: the rail adds no live region and announces nothing
  (ASSUMPTIONS.md Q4). Populated content simply enters document order before
  Results and is reachable by Tab and by the labelled region.
- Visible focus: existing `:focus-visible` rings on buttons, links, and the
  viewport are reused; nothing removes outlines. Reduced motion: the existing
  `prefers-reduced-motion` override on `MovieCarousel.css` applies unchanged.
- The view toggle (Grid/List) affects ResultsBody only; the shelf is always
  the horizontal carousel.

## Web Interface Guidelines checklist (applicable requirements)

Recorded per role duty; fetched `vercel-labs/web-interface-guidelines`
`command.md` this session. No /web code was reviewed or edited.

- Icon buttons have accessible names: met by unchanged MovieCarousel
  (`Previous` / `Next`) and MovieCard controls.
- Actions are `<button>`, navigation is `<a>`: met; cards are native anchors
  with plain-click interception preserved.
- Images have `alt`; decorative icons `aria-hidden`: met by unchanged
  MovieCard; the rail adds no images or icons.
- Semantic HTML and hierarchical headings: met (`section`/`ul`/`li`/`article`,
  `h2` stamp then `h3` card titles).
- Visible focus, `:focus-visible`, `min-w-0` flex children: met
  (`ForYouRail.css .for-you-card` keeps `min-width: 0`; hosts styles reused).
- Long content: reason chips wrap without limit by design; titles keep the
  existing card ellipsis behavior.
- Reduced motion honored; no decorative motion; no entrance choreography.
- `overscroll-behavior-inline: contain` on the scroll viewport: existing
  MovieCarousel behavior.
- Deliberate deviations, each cited: no `aria-live` for rail async updates
  (frozen quiet states, ASSUMPTIONS.md Q4); no skeleton for loading (frozen;
  the brief wins over the Operate-mode convention); URL does not gain rail
  state (frozen no-router scope, Proposed ADR-0021 browse URL ownership);
  skip link and `scroll-margin-top` are host shell concerns, unchanged.
- Anti-pattern scan: none introduced (no `transition: all`, no outline
  removal, no div-click navigation, no gesture-only actions, no
  auto-focused inputs).

## Responsive and motion

- Card geometry: `10.5rem` wide on the shelf track, `8.5rem` at
  `max-width: 47.9375rem` (parity with the existing details shelf,
  `RelatedMovies.css`); poster locked at `aspect-ratio: 2 / 3` at every width
  (existing inline geometry on MovieCard).
- The shelf is fluid horizontally: roughly four-plus posters on wide screens,
  two-and-a-half on phones. Vertical rhythm: `--lamu-space-3` between stamp
  and carousel (matching the details shelf); the host `.library-view` grid gap
  (`--lamu-space-4`) separates the shelf from the filter band above and
  Results below.
- No new motion of any kind. The only movement is the existing carousel
  scroll answering button press, key press, or swipe, with its existing
  `prefers-reduced-motion` instant variant.

## Explicit non-goals

No new dependency, project, top-level folder, architectural layer, state
library, QueryClient, or wrapper layer (brief Q4, Q6). No backend route, DTO
field, status code, reason kind, exporter, or template. No substitute endpoint
and no hand-written DTOs or hand-edited generated files. No skeleton, spinner,
toast, retry widget, or cold-start/empty-candidate copy. No client sorting,
filtering, truncation, or score display. No kind-color differentiation and no
reason prefixing, relabeling, or HTML interpretation. No router, URL key, or
details-page history copy. No new playback or feedback flow and no edits to
`movieFeedbackStore.ts` / `movieDetailsQueries.ts` invalidation. No
`Features:LocalPlay`, secrets, or `Process.Start` involvement. No `CONTEXT.md`
edit, no ADR (Proposed ADR-0021 stays Proposed and untouched), no glossary
claim.

## Design conformance checklist

How review and tests read this design against the frozen Q7 matrix:

| Frozen behavior (brief Q7 / spec) | Design element | Proofs |
| --- | --- | --- |
| Placement above Results | Component tree; wireframes | T008, T009 |
| `For You` heading and accessible carousel label | Stamp `h2` + `label` prop | T006 |
| take=10, query key `['forYou', 10]`, signal, distinct identity | Data needs; forYouApi node | T004 |
| Returned order preserved | States table (no client sort) | T006 |
| Personal/structural/empty reasons, nullable poster via unchanged MovieCard | Card close-up; partial-data row | T006 |
| 200 `[]` hides heading and rail | States table; hidden wireframe | T006 |
| Pending and 400/422/500/network leave grid usable, no rail error UI | States table | T006, T008 |
| Cached nonempty stays while eligible; `[]` clears it | States table | T006 |
| Filters/watchlist suppress cached rail and new requests; in-flight cannot remount; clearing restores | Mount condition in component tree; states table | T008 |
| openMovie and modified-click link behavior intact | Card close-up; route map | T006, T008 |
| Reason text verbatim, no prefix/raw HTML | Card close-up; non-goals | T006 |

Presentation refinements inside frozen behavior made by this document:
shelf-stamp heading values, named rail card geometry in `ForYouRail.css`,
shared flat chip treatment for both reason kinds, and the two focus-continuity
rules. None changes product behavior; none adds copy. This ask authorizes
`design.md` only - no other artifact is edited and no `[assumed]` logging is
performed here.

## Skill-use receipt

`frontend-design` was loaded from the installed catalog and applied as the
design method: subject-grounded direction (personal screening-room shelf),
pass-1 token/type/layout/principle plan, pass-2 review against generic
defaults with stated cuts, one expressive surface (verbatim reason chips) with
quiet surroundings, copy limited to the frozen `For You`.
`impeccable` was used for Operate-mode UX judgment (earned familiarity,
consistency over surprise, restraint over decoration); its context launcher did
not run under the seat write-boundary fallback and its Operate reference was
read instead - no initialization, hooks, or extra files.
`web-design-guidelines` was fetched fresh and applied as the checklist recorded
above. No `/web` or code file was edited; the sole write is this
`specs/DEV-418/design.md`.
