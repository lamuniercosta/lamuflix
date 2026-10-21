# DEV-343 Design — RelatedMovies on Movie Details

Status: Phase A design, Palette-owned. Required before plan challenge (brief Q6).
Branch: `feature/343-spec`. Base: `8edc1b8f4832006bc812f8ebd2da3caf0e9c3ea9`.
Sources: closed `brief.md` (grill closed Q7), `spec.md`, `plan.md`, `tasks.md`,
`CONCLUSIONS.md` Q1–Q7, `ASSUMPTIONS.md` Q3–Q5, `specs/PRODUCT.md`.
Recon: brief cites `recon-DEV-343` throughout; `recon-DEV-343-2` was read as the
Maestri companion note (section 11, capability-path only, no additional UI
facts), so all UI recon facts below are the brief/spec/plan citations of
`recon-DEV-343`.
Skill: `frontend-design` loaded and applied; receipt at the end of this file.
Binding constraints: the Q3 keyboard/scroll contract, Q4 states and copy, and Q5
card/navigation/placement rulings are binding. This design owns spacing, layout,
and responsive visuals only and changes none of that contract.

## Design plan (frontend-design pass 1)

LamuFlix is a personal library, not a storefront. The subject matter is shelves
of films the patron already owns. The visual language is a quiet screening room:
existing details tokens (`--color-ink`, `--color-dim`, `--color-boundary`,
`--lamu-space-*`, `--lamu-radius-media`) stay authoritative. One memorable
element carries the design: the verbatim Reason chips read as a film-sprocket
strip under each poster, small monospace-free sentence-case text on a flat
tinted field, wrapping without limit to show every verbatim Reason string in
returned order with no clipping, clamping, or truncation. Everything around
them stays quiet:
no new accent color, no gradient wash, no card shadows, no numbered markers.

- Color: reuse host tokens only. Ink text, dim secondary, boundary hairlines,
  flat chip field mixed from boundary at low opacity, neutral poster fallback
  field. No new hex palette is introduced; the chip strip is the single
  expressive surface.
- Type: inherit the host stack. Section heading matches existing
  `.details-section-heading` (the one place uppercase letterspacing is kept,
  for consistency with the rest of the page). Card titles use the host
  card-title size, one line, ellipsis. Reason chips use the host small-body
  size in sentence case. No new typeface, no single-word accent coloring.
- Layout: a full-width details section with a left-aligned heading, a
  boundary-aware rail beneath it, and cards at a fixed 2:3 poster ratio. The
  rail aligns its left edge with the details hero content above it.
  Centered or justified content is rejected; the library reads left to right.
- Principles: the rail is a shelf, not a deck of cards. Posters keep their
  theatrical ratio and touch each other through whitespace, not borders. Text
  never decorates: every string on screen is ticket copy or API text.

Uniqueness review (frontend-design pass 2): the first draft reached for
identical rounded SaaS cards with soft shadows and an all-caps eyebrow over
every sub-part. Both were cut as generic defaults: cards are borderless with
no shadow, sharing only the host `--lamu-radius-media` poster radius, and the
only uppercase label is the pre-existing section-heading style. The middle-dot
meta string, spaced-em-dash labels, and arrow-suffixed buttons were all
rejected; Previous/Next are icon buttons with accessible names, not text links
with arrows. Motion is limited to the single scroll response; there is no
staggered fade-and-slide entrance.

## Route map and URL structure

No new route. The section lives on the existing Movie Details route:

- `/movies/{id}` — existing Movie Details view (host). RelatedMovies is a
  section inside it, placed after `MoreWithPanel` and before `FileSection`,
  retaining both (binding `[assumed]` placement, ASSUMPTIONS.md Q5).
- Card activation navigates to `/movies/{id}` for the recommended movie via a
  single descriptive native anchor per card, using the existing SPA
  `openDetails` / `navigateTo` and validated `libraryReturnUrl` handling.
  Modified clicks keep native browser behavior (brief Q5).

## Per-page layout (text wireframes)

Loaded Movie Details with populated RelatedMovies (desktop, narrow, and rail
detail share one structure; widths collapse, order never changes):

```text
[Back link]
[Hero: poster 2:3 | title, facts, actions (unchanged)]
[MoreWithPanel (unchanged)]

Related movies                                    [ Previous ] [ Next ]
+---------------------------------------------------------------+
| [poster 2:3] [poster 2:3] [poster 2:3] [poster 2:3] ...       |
|     Title        Title        Title        Title              |
|     2019         year?        2024         1979               |
|     [reason]     [reason]     (no chips)   [reason]          |
|     [reason]     [reason]                  [reason]          |
+---------------------------------------------------------------+
[FileSection (unchanged)]
```

Rail close-up, one card:

```text
+------------------+
| poster 2:3       |
| (or neutral      |
|  fallback field) |
+------------------+
| Title (1 line)   |
| 2019 (optional)  |
| [Because…]       |
| [Same director]  |
+------------------+
```

Mobile (single column page, rail still horizontal): the rail keeps horizontal
overflow with touch scrolling; cards shrink to two-and-a-half visible posters
so the overflow affordance is visible; chips wrap without limit with no
clipping, clamping, or truncation; the Previous/Next buttons remain reachable
but never replace swipe.

## Component tree

```text
MovieDetailsView (existing, additive only)
  MoreWithPanel (existing, unchanged)
  RelatedMovies (new, features/movie-details)
    h2 "Related movies"
    MovieCarousel (new, ui, exported for DEV-418 reuse)
      PreviousButton (existing ArrowLeftIcon, heading row, targets Viewport)
      Viewport (focusable scroll container)
        ul > li * n > RelatedMovieCard (caller-composed children)
      NextButton (existing ArrowRightIcon, heading row, targets Viewport)
    RelatedMovieCard (per recommendation, composed by RelatedMovies)
      a[href="/movies/{id}"] (single, descriptive)
        PosterImage (decorative alt when title labels the link)
        PosterFallback (neutral, null/missing/broken)
        Title, optional ReleaseYear
        ReasonChips (verbatim API strings, inert text)
  FileSection (existing, unchanged)
```

`MovieCarousel` accepts an accessible label and rendered card children only.
Its single Previous/Next pair sits in the heading row within `MovieCarousel`
and targets the Viewport; there are no flanking rail arrows.
It owns horizontal layout and scrolling and has no fetching, endpoint DTO
dependency, or recommendation logic (brief Q2). `RelatedMovies` owns the query,
states, cards, and chips. Callers compose cards, so DEV-418 can mount
non-recommendation children without touching this section.

## Component states

Copy is binding `[assumed]` (ASSUMPTIONS.md Q4). Layout per state:

- Loading (initial, absent data): heading plus an accessible placeholder
  reading `Loading related movies…` in the rail position. No carousel, no
  buttons, no skeleton cards that could be mistaken for content.
- Empty (successful empty array): heading plus `No related movies yet.` and no
  carousel and no Previous/Next buttons. The section keeps its vertical rhythm
  so the page does not jump when results arrive later.
- Populated: heading plus the carousel with ordered cards, every API Reasons
  string as an inert text chip under its poster in returned order. Empty
  Reasons renders no chips and no invented explanation. Optional ReleaseYear
  renders only when present. Score is never shown; no playback or feedback
  controls are added.
- Error (local, including recommendations 404): heading plus a local notice
  `Could not load related movies.` with a `Retry` action that refetches the
  query. Never a false empty result and never a whole-page failure; a 404 on
  recommendations does not prove the host movie is missing. Same-movie data
  may remain visible during background refetch, with a local failure notice if
  that refetch fails.
- Partial data: per-card fallbacks compose independently. A null/missing or
  broken PosterUrl shows the neutral fallback field; a missing year shows no
  year line; a card with empty Reasons shows title/poster/year only. One bad
  poster never breaks the rail.

## Data needs (endpoint + fields)

Single source, consumed never invented (brief Q1). At the base HEAD the route,
generated types, and handler are absent; Phase B verifies the landed contract
before building (tasks T001).

- Endpoint: `GET /api/movies/{id}/recommendations` (landed DEV-341), called
  with default parameters through the existing typed openapi-fetch client and
  TanStack Query in `relatedMoviesApi.ts`, keyed by `movieId`, enabled only
  for a valid loaded Movie Details view, preserving API order with strict
  per-`movieId` identity isolation.
- Displayed fields from `RecommendedMovieDto`: target movie id (link target),
  `Title`, `PosterUrl`, optional `ReleaseYear`, `Reasons[]` (verbatim chip
  text, in order). `Score` and any other wire-only fields are consumed by the
  contract but never rendered.
- Contract chain: C# DTO → `web/src/api/openapi.json` → generated
  `web/src/api/types.ts` and `web/src/mocks/handlers.ts` via
  `npm run generate:api`. Fixtures in `web/src/mocks/fixtures.ts` and
  `client.typecheck.ts` exercises are additive and only as needed (tasks
  T007). Generated outputs are never hand-edited and handlers are never
  duplicated.

## Keyboard navigation and accessibility notes

Binding interaction contract (brief Q3, ASSUMPTIONS.md Q3); visuals support it
but do not alter it:

- The carousel is a labelled region containing a semantic list (`ul`/`li`).
  The scroll viewport is focusable and is the only Arrow-key surface:
  `ArrowLeft`/`ArrowRight` scroll only when the viewport itself has focus.
- Native card links stay in ordinary Tab order with standard Enter and
  modified-click behavior. No roving tabindex, no descendant-key interception,
  no wraparound, no autoplay. Scrolling never moves movie focus.
- `Previous`/`Next` are real buttons with accessible names, disabled at their
  respective boundaries and both disabled when content fits without overflow.
- Poster images are decorative (`alt=""`) when the card title names the same
  link, avoiding duplicate accessible names. Each anchor is a single
  descriptive link per card.
- Visible focus is always present on viewport, buttons, and links. Native
  touch and `overflow-x` scrolling work without the buttons. Motion respects
  `prefers-reduced-motion`: scrolling is instant, with no animated glide and
  no entrance animation.
- The loading placeholder is exposed as an accessible status; the error notice
  and `Retry` are reachable by keyboard; chips are inert text, never buttons.

## Responsive and motion

- Breakpoints follow the host page: rail shows roughly four posters on wide
  screens, two-and-a-half on phones (the half-card signals scrollability).
  Poster ratio locks at 2:3 at every width; chips wrap without limit with no
  clipping, clamping, or truncation.
- Reduced motion: no autoplay, no entrance sequence, instant scroll steps. The
  one permitted motion is the scroll itself answering a button press, key
  press, or swipe.

## Explicit non-goals

No new dependency, project, layer, schema, backend route, DTO change, LocalPlay
or `Process.Start` involvement, secrets, or unrelated rewrites (brief Q2).
No Score display, no chip filtering, no sorting or truncation, no synthetic
explanations, no HTML interpretation of Reason text, no playback or feedback
controls. No ADR and no glossary edit.

## Skill-use receipt

`frontend-design` skill body was loaded from the installed catalog path
`C:\Users\lamun\.config\muse\skills\frontend-design\SKILL.md` and applied as
the design method for this artifact: subject-grounded direction (personal
screening-room shelf), pass-1 token/type/layout/principle plan, pass-2 review
against generic defaults with stated cuts, one memorable element (Reason chip
strip) with quiet surroundings, and copy written as plain user-facing
direction. No `/web` or code file was edited; the sole write is this
`specs/DEV-343/design.md`.
