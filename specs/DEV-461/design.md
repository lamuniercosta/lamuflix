# DEV-461 — Icon row and review interactions design

Status: visual detail for Phase A (Palette). Frozen interaction and copy contracts from `brief.md` Q1–Q9 plus the Phase 2 step 6 challenge addendum (`brief.md:297-321`, `CONCLUSIONS.md:214-219`, `ASSUMPTIONS.md` Q9 supersession), `spec.md` US4, `plan.md` order 4, `tasks.md` T029–T038. This file finalizes CSS-level visuals only. It changes no copy, order, endpoint, or state contract. Phase 2 step 6 repair item 7 applied.

Inputs: `brief.md`, `spec.md`, `plan.md`, `tasks.md`, Maestri notes `DEV-461` and `adjudication-DEV-461-Keel` item 7. Incumbent truth read from `web/src/library/MovieFeedbackActions.tsx/.css`, `web/src/library/MovieCard.tsx`, `web/src/features/movie-details/MovieDetailsView.tsx`, `RelatedMovies.tsx`, `ForYouRail.tsx`, `web/src/library/watchlist.tsx` (`WatchlistToggleButton`), `web/src/library/library.css`, `web/src/ui/icons.tsx`, and theme tokens in `web/src/index.css:3-11`.

Mode: Operate. The visitor completes a task (mark watched, like, dislike, save). Precision and restraint win. No marketing hero, no new palette, no motion showcase.

## Design tokens (incumbent Lamu system, no new dependency)

- Color: reuse only existing tokens — `ink` (active fill + text), `booth` (icon on active fill), `glass` / `glass-2` / `glass-3` (row and hover surfaces), `boundary` (quiet button border), `rule` (separators), `amber` (focus ring + rating star only, never review state), `dim` (inactive icon + meta), `plate` (tooltip surface), `reel` (poster fallback).
- Type: inherit card and details type. No new face. Icon buttons carry no visible text; the accessible name is the label. Tooltip text 0.8125rem, title-bearing, exact Q9 casing preserved verbatim (content, never a CSS `text-transform`).
- Shape: one radius language. Icon buttons are 2.5rem circles (`radius-pill` / 9999px), matching the existing poster bookmark and play affordances. The row itself has no card, no border, no background wash.
- Density: 44px minimum target on touch (2.5rem button + padding), 0.5rem gaps on card, 0.75rem gaps on Details.

## Principle: one quiet instrument strip

The memorable thing is the strip, not any single icon. Four equal circular buttons in a single row in Q9 order (Watched, Like, Dislike, Watchlist), left-aligned under the card meta and inside the Details action cluster. The row sits on one line at standard card and Details widths; `flex-wrap: wrap` (already on `.movie-feedback-actions`) is narrow-viewport and text-zoom overflow safety only, so buttons never clip or overlap and Q9 order plus 2.5rem targets are preserved. Inactive icons are `dim` line icons on transparent. An active control shows an `ink` disc with a `booth` glyph. Only the thumbs pair is mutually exclusive: at most one of Like/Dislike holds the disc, and selecting the active thumb clears `Review` to `None` while selecting the opposite thumb switches directly. Watched and Watchlist are independent toggles: each holds its own disc without affecting the other or the thumbs, `Review None` leaves their pressed state unchanged, and several discs may be present at once (for example Liked plus Watched plus Watchlist). Nothing else on the card changes. Poster keeps play and status only; the floating poster bookmark is removed, not duplicated.

Against the generic default: no four rounded cards, no per-icon tint (green thumb, red thumb, gold star), no eyebrow label above the row, no middle-dot meta string. The row is chrome, and chrome stays quiet.

## Route map and URL structure

No new route. The shared row renders on existing surfaces only.

| Surface | Route | Row placement |
|---|---|---|
| Library grid, LibraryTile, For You rail | `/` (`LibraryView`: `ForYouRail` + browse grid) | Below card meta, above retry/error lines; same strip on every `MovieCard` |
| Related row | `/movies/:id` (`MovieDetailsView` + `RelatedMovies`) | Same strip on each related `MovieCard` |
| Movie Details | `/movies/:id` | Inside `.details-actions`, between Play and Remove; replaces standalone `.details-watchlist` bookmark position |
| List rows | unchanged | `MovieListRow` keeps existing controls; explicitly out of scope |

## Per-page layout (text wireframes)

Card (`MovieCard`, all callers):

```text
+-- movie-card ------------------+
| [poster 2:3]              |
| title (link, stretched)       |
| star rating | year              |
| (Because you liked {X})*      |  <- reason chip, recommendations only
| (o) (o) (o) (o)               |  <- icon row in Q9 order: Watched, Like, Dislike, Watchlist
| [retry | error]*              |
+-------------------------------+
```

Details (`MovieDetailsView`):

```text
+-- details-hero ----------------+
| poster | title                  |
|        | format · facts         |
|        | ratings                  |
|        | director(s)              |
|        | synopsis                 |
|        | [Play] (o)(o)(o)(o)       |  <- Play, then icon row, then Remove
|        | Remove from library       |
|        | [status | error]*         |
+--------------------------------+
| More with {actor}             |
| Related (RelatedMovies cards)  |  <- each card carries the same strip
| File                          |
+--------------------------------+
```

`*` conditional. Reason chips render above the row, never inside it.

## Component tree

```text
MovieCard / MovieDetailsViewInner
└─ MovieFeedbackActions (shared row, extended props: review + isWatched + isInWatchlist)
   ├─ IconButton Watched      (EyeIcon line/filled or existing watched glyph)
   ├─ IconButton Like         (ThumbsUpIcon, new local SVG in ui/icons.tsx)
   ├─ IconButton Dislike      (ThumbsDownIcon, new local SVG in ui/icons.tsx)
   ├─ IconButton Watchlist    (BookmarkIcon / BookmarkFilledIcon, moved from
   │                            poster overlay + standalone details position into this row;
   │                            TSX owner is `WatchlistToggleButton` in
   │                            `web/src/library/watchlist.tsx`, styled from
   │                            `web/src/library/library.css`. Earlier shorthand names
   │                            `WatchlistButton.tsx` / `MovieCard.css` are conceptual only
   │                            and create no new files)
   ├─ sr-only status (pending announcer, role=status)
   ├─ error block (role=alert + Retry, mutation failure)
   └─ refresh-error block (role=alert + Retry refresh, no replay of success)
```

Stores and clients unchanged in shape: `useMovieFeedback` extended for review PUT plus legacy projection fields; `useWatchlistToggle` reused inside the row. Props stay `movieId`, `title`, `isWatched`, `review`, `isInWatchlist`.

New thumbs SVGs follow the `IconFrame` convention in `ui/icons.tsx`: 24 viewBox, `currentColor` stroke, `aria-hidden`, `focusable=false`, outline style for inactive and `fill=currentColor` for the active glyph. No npm icon package.

## Component states

- Loading: row renders immediately with `aria-busy` on the in-flight button and `disabled` on siblings via the existing per-movie pending guard (`blocked`). Card skeleton and Details `Loading movie…` keep current shimmer and status text. No icon skeletons.
- Empty: row never renders without a movie. Library empty (`No movies yet`), Related empty (`No related movies yet`), and no-metadata Details keep existing copy; the row is absent, not zero-state.
- Error: mutation failure keeps optimistic value rolled back, shows existing `.movie-feedback-error` block with `role=alert` and Retry. Refresh failure shows the refresh block with Retry refresh; refresh retry never replays a successful mutation. Watchlist failure keeps its inline `role=alert` under the row.
- Partial data: rating/year missing renders existing `—` placeholders; row unaffected. Missing review means `Review None` — both thumbs inactive, while Watched and Watchlist show their own independent retained state (either may still hold a disc; `None` never clears them).
- Active/clear/switch: the active thumb holds the ink disc. Activating the pressed thumb clears `Review` to `None` (thumb disc leaves; Watched/Watchlist discs stay as they were). Activating the opposite thumb moves the disc directly to the new thumb. Watched and Watchlist are independent toggles: activating either never selects, moves, or clears a thumb, and thumb changes never alter Watched/Watchlist. Only the thumbs pair is exclusive.
- Shared state: card and Details instances for the same movie reflect one store value. Mutation on either surface updates both after settle, surviving reload.
- Focus/hover: every button shows its tooltip on pointer hover and on keyboard focus. Visible focus is a 2px `ink` outline at 2px offset (matching `lamu-chip:has(input:focus-visible)`), never outline removal. Active disc plus focus ring compose.
- Reduced motion: only opacity transitions already on poster affordances (160ms). No entrance choreography on the row. Honor `prefers-reduced-motion` by disabling shimmer and opacity fades.

## Data needs (endpoint + fields, contract chain C# DTO → OpenAPI → generated TS)

Web consumes generated types only (`web/src/api/types.ts`, MSW `web/src/mocks/handlers.ts`). No hand-rolled contract duplicates.

- Read review state: `GET` browse/library, `GET /api/movies/{id}` details, `GET` For You, `GET` Related — fields `review` (`none | liked | disliked`, required after contract regen), `isNotInterested` (compatibility projection of `review == disliked`), `isWatched`, `isInWatchlist`, plus existing `id`, `title`.
- Write review: `PUT /api/movies/{id}/review`, body `{ review: none | liked | disliked }` → 204, 400, 404, 422. Only the legacy not-interested route and `isNotInterested` DTO flag are compatibility projections of `Review == Disliked`; legacy watched and watchlist routes remain independent retained routes, never projections.
- Cache: successful review mutation invalidates library, details, For You, and Related queries; server invalidates the Taste Profile cache so the next recommendation request reflects the change. Failure rolls back and claims nothing.
- Reasons: existing `kind`/`text` shape; personal chip `Because you liked {X}` renders above the row on recommendation cards when Q6 gates hold.

## Keyboard navigation and a11y

- Each icon is a native `button` with `aria-pressed` and a title-bearing accessible name. Exact Q9 copy, tooltip identical to the name:

| Icon | Inactive | Active |
|---|---|---|
| Watched | `Mark Watched, {title}` | `Unmark Watched, {title}` |
| Thumbs up | `Like, {title}` | `Clear liked review, {title}` |
| Thumbs down | `Dislike, {title}` | `Clear disliked review, {title}` |
| Watchlist | `Add to Watchlist, {title}` | `Remove from Watchlist, {title}` |

- Tab order follows visual order: Watched, Like, Dislike, Watchlist. Enter/Space activate. No roving tabindex; the row is four stops, matching four controls.
- Card title link keeps stretched-click behavior with `z-index: 1` underlay; row buttons sit at `z-index: 2` so mouse and keyboard activation never navigates. `MovieDetailsView` row is outside any link.
- Tooltips: available on hover and focus, plain text, no information exclusive to the tooltip. Screen readers get the same string via the accessible name; `aria-pressed` carries state independently of wording.
- Status and errors use `role=status` (pending, Copied-style confirmations) and `role=alert` (failures), with `aria-describedby` pointing at the message id.
- Contrast (measured shipped-theme evidence, 2026-11-05, WCAG relative-luminance ratios from `web/src/index.css:3-11` tokens): inactive `dim` `#c2c8d1` on page `booth` `#16181d` is 10.55:1; inactive `dim` on card-adjacent `reel` `#20242b` is 9.25:1; active glyph `booth` `#16181d` on `ink` disc `#f0ede6` is 15.19:1; quiet border `boundary` `#9aa3b3` on `booth` is 6.99:1 (informative). All text/icon pairs exceed the ruled >= 4.5:1 target. Status: PASS with measurements (not token names). No darkening of `dim` required.
- Touch: 2.5rem targets, no hover-only affordance. The poster bookmark removal must not leave a hover-only path to Watchlist on touch; the row is always visible.
- Vocabulary: `Review` is the per-movie `None` / `Liked` / `Disliked` opinion; distinct from external metadata `Rating` (IMDb/TMDb star rating) and from engineering code review, per `ASSUMPTIONS.md` Ledger F7 disambiguation.
- Reduced motion and focus visibility per Component states above. `web-design-guidelines` checklist applied: semantic buttons, visible focus, hover+focus parity, single line at standard widths with narrow-viewport/text-zoom `flex-wrap` safety only, loading/error/empty roles, no new form, no decorative motion. Tooltips and accessible names use exact Q9 casing without sentence-case transformation.

## What this design does not change

Copy and order above are frozen. `MovieListRow` placement unchanged. No new route, project, folder, store library, or icon dependency. TSX edits stay inside `MovieFeedbackActions.tsx`, `MovieCard.tsx`, `MovieDetailsView.tsx`, `web/src/library/watchlist.tsx` (`WatchlistToggleButton`), `web/src/ui/icons.tsx`, and callers per plan order 4; CSS edits stay inside `MovieFeedbackActions.css`, `MovieDetailsView.css`, and `web/src/library/library.css`. Earlier `WatchlistButton.tsx` / `MovieCard.css` shorthand names are conceptual only and create no new files. Any visual tweak that would alter interaction, copy, or endpoint behavior stops and returns to Patron.
