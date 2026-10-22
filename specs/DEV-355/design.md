# DEV-355 — UI design: Watched and Not Interested actions with reason chips

**Status:** Palette design, Phase A. Required before plan challenge (brief Q1, Q5).
**Branch:** `feature/355-spec` | **Phase A base:** `8f9720958f99d62397b306615daf8d49a76d3032`
**Authority:** Ticket DEV-355 Scope & Technical Design and acceptance criteria (retrieved read-only via
`scripts/get-task.ps1 DEV-355`); settled grill brief `specs/DEV-355/brief.md` Q1–Q6; `spec.md`; `plan.md`;
`tasks.md`; Patron rulings in `CONCLUSIONS.md`; taste receipt in `ASSUMPTIONS.md`; `specs/PRODUCT.md` §§2–5.
Recon facts are taken from their citations in `brief.md` / `CONCLUSIONS.md`
(`recon-DEV-355:14-23,26,29-38,41-48,75-99`); the recon note has no file copy in this worktree,
so no recon line is quoted beyond what those artifacts record.
**This design changes no settled decision.** Labels, mark-only contract, shared pending state, optimistic
lifecycle, verbatim reasons, and accessibility contracts below are restated from the brief, not re-decided.
Concrete cache keys, cross-representation DTO field availability, and consumer propagation are Wisp bind
facts (brief Q3/Q5, tasks T001); this design invents none of them.

**Mode:** Operate. These are task controls on existing browse/detail surfaces. Ambition shows as precision
and restraint: the new controls read as native parts of the card and details row, not as a new visual world.
No new palette, typeface, or page archetype is introduced.

## Route map and URL structure

No new route. Both actions live on existing routes:

| Route | Surface | What is added |
|---|---|---|
| `/` | Library grid (`MovieGrid` → `MovieCard`) | `MovieFeedbackActions` row on every movie card |
| `/movies/{id}` | Movie Details (`MovieDetailsView`) | `MovieFeedbackActions` in the hero action row; `RelatedMovies` section reuses the shared card with actions plus reason chips |

Recommendation panels reuse the same card component (ticket Scope item 1). The current consumer is the
`RelatedMovies` section on `/movies/{id}`, rendered through `MovieCarousel`. Future consumers reuse the
same card; the DEV-418 panel is not built here.

## Per-page layout (text wireframes)

### Library grid card (`/`)

```
┌ article.movie-card ─────────────────┐
│ poster (play / watchlist overlay)   │
│ Title (link → /movies/{id})         │
│ ★ 7.8  |  1995                      │  existing meta
│ ┌ movie-feedback ─────────────────┐ │
│ │ [Mark Watched] [Not Interested] │ │  new; wraps on narrow cards
│ └─────────────────────────────────┘ │
│ (retry / error blocks, unchanged)   │
└─────────────────────────────────────┘
```

Placement ruling: the feedback row sits beneath the meta line, after title/meta and before the
enrichment-retry and error blocks. Rationale: the settled labels are full text buttons
(`Mark Watched`, `Not Interested`), which need a text row; the poster overlay already hosts the watchlist
toggle and play button, and crowding a second pair of controls into the overlay would shrink touch and
keyboard targets. The row wraps to two lines on narrow grid columns; card height may grow by one row.

### Movie Details hero (`/movies/{id}`)

```
poster │ Title          facts · ratings · directors · cast · synopsis
       │ ┌ details-actions (extended, wraps) ───────────────────────┐
       │ │ [▶ Play] [watchlist] [Mark Watched] [Not Interested]     │
       │ │ [Remove from library]                                    │
       │ └──────────────────────────────────────────────────────────┘
       │ (mutation / refresh error strip directly below the row)
       │ RelatedMovies section …  FileSection …
```

Placement ruling: both actions join the existing `.details-actions` row, after the watchlist slot and
before `Remove from library`. They use the quiet button treatment already present in that row
(`lamu-action-quiet` family), so feedback reads as secondary to Play and parallel to the watchlist toggle.
The row wraps; order is fixed as above on every viewport so keyboard and screen-reader order never shifts.

### Recommendation card (`RelatedMovies` inside `MovieCarousel`)

```
┌ article.related-movie-card ─────────┐
│ link → /movies/{id}                 │
│   poster · title · year              │
│   ┌ reasons ──────────────────────┐ │
│   │ (chip) (chip) (chip, wraps)   │ │  verbatim API strings
│   └───────────────────────────────┘ │
│ ┌ feedback footer (OUTSIDE link) ─┐ │
│ │ [Mark Watched] [Not Interested] │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

Placement ruling: the feedback footer is a sibling of the card link, never nested inside the `<a>`.
Buttons inside an anchor are invalid markup and make navigation isolation fragile; sibling placement makes
"activation must not trigger parent-card navigation" (brief Q4) true by construction rather than by
`stopPropagation` discipline. Reasons stay inside the link as informational text, matching the existing
`.related-movie-reasons` rendering. The footer is always rendered (both buttons, correct marked/disabled
state), so every recommendation instance carries the same control shape as Library and Details.

## Component tree

```
LibraryGrid (/)
└── MovieCard (movie, onOpenMovie)
    ├── poster / title link / meta            (unchanged)
    └── MovieFeedbackActions                  (new)
        ├── watched button                    label per state below
        └── not-interested button

MovieDetailsView (/movies/{id})
├── hero … .details-actions
│   ├── Play / WatchlistToggleButton / Remove (unchanged)
│   └── MovieFeedbackActions                  (new, same component)
├── mutation / refresh error strips           (new, existing .details-strip-error pattern)
└── RelatedMovies
    └── MovieCarousel
        └── RelatedMovieCard × n
            ├── link (poster / title / year / ReasonChips)
            └── MovieFeedbackActions          (new footer, sibling of link)

MovieFeedbackActions (new, web/src/library/MovieFeedbackActions.tsx)
  props: movieId · title · isWatched · isNotInterested
  state source: movieFeedbackStore hook (per-movie shared pending/optimistic; plan-owned)
  transport: movieFeedback.ts typed POST helpers (plan-owned)

ReasonChips (presentation only, part of the shared card, not a separate component)
  input:  reasons?: string[]   (optional; undefined on ordinary Library/Details data)
  output: one non-interactive <span> per string, verbatim, API order
```

`MovieFeedbackActions.css` is authorized only if scoped styling beyond existing tokens is required
(brief Q5). Default: reuse existing button and chip classes; no new stylesheet unless the Build finds a gap
the existing tokens cannot cover.

## Component states

### Feedback buttons (all three surfaces, identical)

| State | Watched button | Not Interested button | Announcement / affordance |
|---|---|---|---|
| Idle, unmarked | `Mark Watched`, enabled | `Not Interested`, enabled | — |
| Idle, already Watched | `Watched`, disabled, marked styling | `Not Interested`, enabled | Disabled button never dispatches (brief Q2) |
| Idle, already Not Interested | `Mark Watched`, enabled | `Not Interested (marked)`, disabled, marked styling | Mirror of above; flags are independent |
| Either POST pending (this movie) | disabled, `aria-busy="true"` | disabled, `aria-busy="true"` | Visually-hidden `role="status"` announces saving; labels do not change |
| Settled POST, reconciliation deferred | both stay disabled | both stay disabled | Per-movie exclusion covers update/reconciliation, not only in-flight POST (brief Q3) |
| POST failure | roll back only this operation; prior state restored | same | `role="alert"` error + explicit Retry button re-issues that POST once; server state reconciled; no automatic retry |
| Refresh failure after successful POST | confirmed mark stays | confirmed mark stays | Separate refresh error + Retry that refetches queries only, never another POST |

Marked styling must differ from the plain disabled treatment by more than grey text alone (marked = confirmed
server state, not unavailable control): filled-quiet treatment from existing tokens, with the disabled
attribute carrying the non-dispatch behavior. Pending uses disabled + `aria-busy`, never a label swap, so the
settled labels remain the stable accessible names.

### Reason chips

| Input | Rendering |
|---|---|
| Populated list | One chip per string, verbatim text, API order, wrapping layout, no ellipsis |
| Duplicate strings | Each occurrence renders; keys stay distinct and stable (`index`-prefixed, existing pattern) |
| Empty or absent | No chips, no container, no synthesized explanation |
| HTML-looking literal (e.g. `<b>…</b>`) | Rendered as React text (escaped); full wording stays in the accessibility tree |
| Ordinary Library/Details data | `reasons` prop stays `undefined`; nothing fabricated |

Chips are `<span>` text, never buttons or links, never focusable. Visual truncation (`text-overflow`) is
banned here: wrapping keeps the visible and AT-exposed strings identical.

### Panel states (Library grid, Details, RelatedMovies)

| State | Design |
|---|---|
| Loading recommendations | Existing `role="status"` loading text; feedback controls are absent until data arrives (no skeleton buttons) |
| Empty (`No related movies yet.`) | Unchanged empty copy; no feedback UI, no placeholder chips |
| Load error | Existing `role="alert"` + Retry (refetch); failed POST marks are unaffected by panel-level retry |
| Partial data (some cards lack flags) | Flags initialize only from actual contract fields; a card with absent flag data renders its actions from confirmed fields only and never invents a default (brief Q2) |
| Refresh removes the focused card | Focus moves to the section heading (`tabindex="-1"`, no visual change) or, if the panel prefers, the next card link; focus must never drop to `<body>` |

## Data needs (endpoint + fields)

| Need | Endpoint / field | Notes |
|---|---|---|
| Mark Watched | `POST /movies/{id}/watched` → `204 NoContent` | Typed helper in `movieFeedback.ts` via generated contract types; at-most-once per movie while pending |
| Mark Not Interested | `POST /movies/{id}/not-interested` → `204 NoContent` | Same as above; no automatic POST retry (repeat transitions throw) |
| Initialize / disable | `IsWatched`, `IsNotInterested` contract fields | Initialized only from actual fields after Wisp T001 bind; absent fields are a recon/contract gap, never fabricated defaults |
| Preserve independence | The unchosen flag | Successful action changes only the chosen flag |
| Reason chips | Recommendation representation `reasons: string[]` (flat list) | Verbatim, API order; Personal/Structural are API wording, never a client-side category |
| Authoritative refresh | Affected movie/library queries + all recommendation queries dependent on the global feedback profile | Invalidate/refetch per T001-bound keys; inactive queries go stale for next fetch; server owns final membership, flags, reasons |
| Explicitly unused | `DELETE /movies/{id}/watched`, `DELETE /movies/{id}/not-interested` | Exist in contract; not exposed as product surface in this ticket (brief Q2) |

No new endpoint, field, status code, or query shape is introduced. The C# DTO → OpenAPI
(`web/src/api/openapi.json`) → generated TypeScript (`web/src/api/types.ts`) chain stays authoritative;
web code consumes generated types, never hand-rolled duplicates.

## Keyboard navigation and accessibility notes

Checklist applied (`web-design-guidelines` review axes; interaction contract from brief Q4):

- **Native buttons.** Both actions are real `<button type="button">` elements in DOM order
  (Watched first, Not Interested second), operable with Enter and Space, Tab-reachable everywhere they appear,
  including inside `MovieCarousel` items.
- **Accessible names.** Settled labels are `Mark Watched` / `Watched` and `Not Interested` /
  `Not Interested (marked)` (ASSUMPTIONS.md Q4). On repeated cards (grid, carousel) the accessible name
  includes the movie title, e.g. via `aria-label="Mark Watched, Heat"`; the single Details instance uses the
  visible label alone.
- **Visible focus.** Every button shows the existing visible focus treatment; focus order follows visual order
  on all three surfaces. No focus styling is removed or replaced.
- **Pending announced.** Disabled + `aria-busy="true"` on both buttons for the affected movie only; a
  visually-hidden `role="status"` live region announces the saving state. Other movies stay fully interactive.
- **Errors announced with explicit retry.** Mutation failure: `role="alert"` message plus a named Retry button.
  Refresh failure after success: a separate message plus a refresh-only Retry. The two retries are visually
  adjacent to what they recover and never share one control.
- **Navigation isolation.** Feedback controls are never nested inside card links (carousel footer is a sibling
  of the `<a>`; Library and Details buttons already sit outside title links). Pointer, Enter, and Space
  activation on a feedback button never opens the details route.
- **Card-removal focus.** When authoritative refresh removes the focused recommendation card, focus lands on
  the section heading (`tabindex="-1"`) or the next card link — a sensible panel target, never `<body>`.
- **Chips are inert text.** Reason chips are not focusable, carry no `role`, and expose their full verbatim
  string to assistive technology. No tooltip-only or ellipsis-clipped wording.
- **Motion and layout restraint.** No new animation, spinner, or scroll-driven entrance is introduced for
  feedback state changes; state flips are instant and therefore `prefers-reduced-motion`-safe by construction.
  Layout shift is limited to the error strips and the wrapping feedback row; no control moves while pending.
- **Responsive.** The Library feedback row and the Details action row wrap; carousel items keep both buttons
  reachable through the existing horizontal scroll plus keyboard scrolling. Minimum target size follows the
  existing button tokens; no new compact icon-only variant is introduced.

## Visual styling (Palette-owned, within settled contracts)

- Reuse existing tokens and classes (`lamu-action-quiet` family, `.related-movie-reason` chips,
  `.details-strip-error` pattern). Reason chips reuse the non-interactive `.related-movie-reason` treatment,
  never the interactive `lamu-chip` toggle-button pattern used for cast filtering in `MovieDetailsView`.
  No new palette, typeface, radius, shadow, or gradient is introduced;
  restraint bans (purple/blue gradients, glow shadows, glass-everywhere, scale-on-hover, all-caps kickers)
  are respected by staying on incumbent styles.
- Marked state: quiet-filled treatment distinct from plain disabled; pending: standard disabled opacity plus
  `aria-busy`. No icon-only buttons — the settled text labels are the design.
- `MovieFeedbackActions.css` exists only if Build proves existing tokens insufficient; the default is no new
  stylesheet.

## Out of scope (frozen, brief Q1)

No undo/toggle UI; no DEV-418 panel; no new dependency or state library; no generic layer; no
backend/schema/API change; nothing touching playback, `Features:LocalPlay`, secrets, or `Process.Start`;
no rewrite of unrelated files. Existing-file edits stay inside the brief Q5 envelope and only as necessary
for acceptance. Anything else is a follow-up issue, not a finding in this round.
