# UI Design: DEV-322 Filter Panel

**Feature Branch**: `feature/322-spec`

**Created**: 2026-10-12

**Status**: Draft — for plan challenge

**Input**: frozen brief `specs/DEV-322/brief.md` (Q1–Q11, Q11 authoritative on range serialization; Q12 transport defaults and canonical equality plus the Q7 year clarification at brief.md:170-188); `specs/DEV-322/spec.md`; `specs/DEV-322/plan.md`; `specs/DEV-322/tasks.md`; `specs/DEV-322/CONCLUSIONS.md`; `specs/DEV-322/ASSUMPTIONS.md`; `specs/PRODUCT.md`; facts `recon-DEV-322:11-63,65-106,189-234`; ticket text `DEV-322:8`.

**Authority guard**: this design settles presentation inside the feature slice only. It does not change ticket behavior, API shape, dependency choices, or range serialization (Q11, CONCLUSIONS.md:47-52). Q1–Q11, the frozen file scope, and every string in `ASSUMPTIONS.md` are cited and obeyed, not re-litigated. The §8 count/error copy is approved by Patron in CONCLUSIONS.md:74-77 and logged `[assumed]` in ASSUMPTIONS.md:12.

---

## 1. Design direction

LamuFlix is a personal film library. The screen's job is narrow and physical: line up a wall of titles, then slide, check, and type until the wall is the one film you want. The direction takes its cues from the projection booth rather than from a SaaS dashboard — a dark canvas, brass hardware, and numbers that line up like a timecode readout.

The current readiness screen (`web/src/App.tsx`) already sets a dark canvas (`bg-slate-950` with `slate-50` type). This design keeps that dark ground and sharpens it, so the panel reads as the product growing out of its own shell rather than a second theme.

### 1.1 Palette (feature-slice tokens)

Six core tokens plus one semantic error token. Named hexes below are the source of truth; implementation expresses them as Tailwind arbitrary values inside the frozen component files (see §1.4).

| Token | Hex | Role |
| --- | --- | --- |
| `ink` | `#0C1220` | page canvas; also the text color on brass fills |
| `panel` | `#151D2C` | filter rail and field grounds |
| `line` | `#2B3648` | hairline rules, control borders, placeholder bars |
| `text` | `#F2F5F9` | primary type (≈15:1 on `panel`) |
| `text-dim` | `#9AA7BB` | legends-as-secondary, readouts, badges at rest (≈7:1 on `panel`) |
| `brass` | `#E2B45C` | the single active accent: focus ring, slider handles, checked genre/cast marks, active chips, checked include-unknown |
| `alert` | `#E88B84` | error copy only (≈6.8:1 on `panel`) |

Contrast floor: all text ≥ 4.5:1 against its own ground; `brass` on `ink` and `ink` on `brass` both ≈10:1, so active chips may fill brass with ink type.

Brass is chosen from the subject (marquee and projector hardware, film-leader amber), not as a generic dark-UI accent. It is spent only on state — nothing decorative is brass.

### 1.2 Type

One family: the platform stack already in place (`ui-sans-serif, system-ui, sans-serif`, Tailwind's default sans). No font files or webfont links: a display face would add assets outside the frozen file scope and belongs to `specs/DESIGN.md` (Sprint 7, DEV-20) with the component-library ruling. Identity here comes from weight contrast, sentence case, and aligned numerals.

| Role | Size / line-height | Weight | Notes |
| --- | --- | --- | --- |
| Page title (`LamuFlix`) | 24px / 1.2 | 600 | tracking `-0.01em` |
| Movie title in results | 17px / 1.4 | 500 | |
| Group legend | 13px / 1.3 | 600 | sentence case, normal tracking — no all-caps eyebrows |
| Control label, option name | 14px / 1.5 | 400 | |
| Numeric readout, count badge, slider extents | 12px / 1.2 | 500 | `tabular-nums` always |

Sentence case everywhere. No tracked-out caps labels, no "LABEL — fragment" constructions, no middle-dot meta strings.

### 1.3 Layout and spacing

- 4px base grid: gaps 4 / 8 / 12 / 16 / 24 / 32.
- Two zones at ≥1024px: a fixed **320px filter rail** on the left, results column filling the rest, 32px gutter. Below 1024px the rail stacks above the results at full width (§3.2).
- Alignment: labels and option rows left-aligned; every number (count badges, range readouts, slider end labels) sits in a fixed-width right-aligned frame so nothing reflows when values change. Results list left-aligned, single column of titles.
- Corners are tight and structural: 4px on fields, rows, and chips; the panel itself is a flush column separated by hairlines, not a rounded card. The one divider above `Sort` is meaning, not decoration (§3.1).
- Motion: none on load or scroll. 120ms color/border transitions on hover and checked states only. Under `prefers-reduced-motion: reduce` those become instant. Results list dips to 0.6 opacity while a refetch is pending and returns to 1 — no spinner, no movement, no layout change.

### 1.4 Implementation vehicles (no dependency, no file-scope drift)

- Styling: Tailwind 4 utilities with arbitrary hex values (`bg-[#0C1220]`, `text-[#E2B45C]`, `accent-[#E2B45C]`, `tabular-nums`, `sr-only`) composed inside the four planned `web/src/features/library/` files and `App.tsx` — all already in the frozen file scope. `web/src/index.css` is untouched; no theme file, no font asset, no CSS module.
- Controls: the frozen Q3 set — two native `input type=range` per range, native checkbox lists, a native `select` for sort, `URLSearchParams` + `history.replaceState`/`popstate`, a local `useDebouncedSearch` hook. Native controls are styled via `accent-color` and border/background utilities only.
- Data layer: DEV-320 generated client/types (Q4) and TanStack Query only if absent at Phase B pickup (Q3). No router.

### 1.5 Self-critique against template defaults

Checked and rejected: cream-and-terracotta marketing look; acid accent on near-black; broadsheet hairline grid with zero radius as a whole-page style; a kit of identical rounded cards with one radius and soft shadows; all-caps eyebrow labels; em-dash label fragments; monospace data labels; arrow-suffixed links. The dark canvas here is the existing product shell and the cinema subject, the radius is tight and structural, and the single accent is spent only on state. The one memorable element is the paired range readouts (§3.1); everything else stays quiet.

---

## 2. Route map and URL structure

### 2.1 Route map

There is no client router (Q3, FR-015). One document route serves the whole feature:

| Route | Document | Renders |
| --- | --- | --- |
| `/` | SPA shell | `App.tsx` → `LibraryView` → `FilterPanel` + results |

If Phase B pickup finds an existing library page (Q5 conditional), bounded recon identifies its route and mount point before file scope is revised; this draft does not assume its route is `/`. The panel URL contract is unchanged and no new router is introduced. `inWatchlist`, `page`, and `pageSize` are never panel routes or controls (FR-016).

### 2.2 URL query structure (committed source of truth)

The query string *is* the navigable state. Keys mirror the API keys 1:1 (Q6). This table is presentation reference over the frozen serialization rules; it decides nothing new.

URL and request are distinct strings (Q12). The browser URL remains the committed source of truth and is compact: the web defaults `sort=Title`, `direction=Ascending`, `page=1`, `pageSize=20` may be omitted, so an empty panel URL carries no keys. Every movie API request is complete: normalization fills absent required values with those four defaults — the empty panel URL requests `sort=Title&direction=Ascending&page=1&pageSize=20` (recon:217-218) — and a supplied `pageSize` or `inWatchlist` is preserved, never overwritten; unknown keys stay in the URL and are not forwarded as generated API fields. Equality is the canonical API query string: decoded URL state plus transport defaults, canonicalized over recognized API keys with repeated-array selection order and Q11 empty bound values preserved, compared with the canonicalized generated-client request — never literal `window.location.search`. The same canonical API query string is the variable part of the movie query key, so omitted and explicit defaults share one key and one fetch, and a committed change to the effective API query fetches once. A filter change removes `page` from the URL and sends `page=1` in the request. No endpoint, parser, validator, DTO, or status-code change comes with this: existing validation remains 422.

| Key | Control | Encoding | Omitted when |
| --- | --- | --- | --- |
| `text` | Search box | single value; commits 300 ms after last keystroke | empty |
| `genreIds` | Genre checkboxes | repeated key, selection order preserved | none selected |
| `actorIds` | Cast checkboxes | repeated key, selection order preserved | none selected |
| `runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown` | Runtime sliders + include-unknown | **all-or-nothing group** (Q11); all three sent whenever active, `runtimeIncludeUnknown` explicit `true`/`false`; an end at its extent serializes as an empty value | whole group at extents with include-unknown unchecked |
| `yearMin`, `yearMax` | Year sliders (extent 1900 → viewer local current year captured at panel mount) | **all-or-nothing group** (Q11); both sent whenever active; extent ends serialize empty | whole group at extents |
| `statuses` | Status chips | repeated key, generated contract values (`Pending`, `Enriched`, `NotFound`, `Failed`) | no chip selected |
| `sort`, `direction` | Sort dropdown | generated contract members `Title`/`Year`/`Rating`/`Runtime` + direction enum spellings — `Ascending` confirmed (recon:217-218), remaining spellings resolved from the inherited contract at Phase B pickup (Q12.6); `Asc`/`Desc` shorthand never goes on the wire; `IMDb` label sends `sort=Rating` (Q2) | default pair `Title`+`Ascending` (label `Title (A-Z)`) |
| `page` | (not a control) | reset to absent in the URL on every filter change; the request sends `page=1` (Q12) | default (1) |
| foreign keys (`inWatchlist`, `pageSize`, unknown) | (not panel state) | preserved untouched | — |

History semantics (Q6): filter commits use `history.replaceState` — never `pushState`, no history spam. `popstate` and full reload hydrate every control from the URL. Text typed but not yet debounced lives only in the input's local value; the URL changes once, at commit.

Empty-string extent values are open bounds, never dropped keys: `runtimeMin=60&runtimeMax=&runtimeIncludeUnknown=false` is "60 minutes and up, excluding unknowns"; `runtimeMax=` with an active runtime group is the `240+` stop; empty year ends never silently exclude pre-1900 or future titles (Q11).

---

## 3. Per-page layout

### 3.1 Desktop (≥1024px)

Left rail = the panel; right column = `totalCount` plus title list (Q5 minimal host; no cards, no grid).

```text
┌──────────────────────────────────────────────────────────────────────────┐
│  LamuFlix                                                                │
├───────────────────────┬──────────────────────────────────────────────────┤
│                       │                                                  │
│  Search               │   12 movies                                      │
│  ┌─────────────────┐  │   ────────────────────────────────────────────   │
│  │ Search titles   │  │   Title One                                      │
│  └─────────────────┘  │   Title Two                                      │
│                       │   Title Three                                    │
│  Genres               │   Title Four                                     │
│  ┌─────────────────┐  │   Title Five                                     │
│  │ ✓ Action     42 │  │                                                  │
│  │   Drama      31 │  │                                                  │
│  │   Comedy     18 │  │                                                  │
│  │   (scrolls)     │  │                                                  │
│  └─────────────────┘  │                                                  │
│                       │                                                  │
│  Cast                 │                                                  │
│  ┌─────────────────┐  │                                                  │
│  │ Filter cast     │  │                                                  │
│  ├─────────────────┤  │                                                  │
│  │ ✓ Actor A       │  │                                                  │
│  │   Actor B       │  │                                                  │
│  │   (scrolls)     │  │                                                  │
│  └─────────────────┘  │                                                  │
│                       │                                                  │
│  Runtime              │                                                  │
│  60 ─────────── 240+ min                                                  │
│  ──────○────────────  │                                                  │
│  ────────────────○──  │                                                  │
│  ☐  Include unknown runtime                                              │
│                       │                                                  │
│  Release year         │                                                  │
│  1900 ────────── 2026 │                                                  │
│  ──────○────────────  │                                                  │
│  ────────────────○──  │                                                  │
│                       │                                                  │
│  ───────────────────  │   ← hairline: introduces the preserved Sort      │
│  Sort                 │                                                  │
│  ┌ Title (A-Z)     ▾┐ │                                                  │
│  └─────────────────┘  │                                                  │
│                       │                                                  │
│  Enrichment status    │                                                  │
│  [Pending] [Enriched] │                                                  │
│  [Not found] [Failed] │                                                  │
│                       │                                                  │
│  [ Clear filters ]    │                                                  │
│                       │                                                  │
└───────────────────────┴──────────────────────────────────────────────────┘
```

Structural notes:

- **The hairline divider** sits between `Release year` and `Sort`, introducing the sort choice that `Clear filters` preserves (FR-014). It is not a reset boundary: enrichment-status chips below Sort still reset, as do all other panel keys except `sort`/`direction`.
- Group order follows the FR order in `spec.md`: Search → Genres → Cast → Runtime → Release year → Sort → Enrichment status → `Clear filters`. (The ticket's own noun order puts year before runtime in prose; FR-004/FR-005 fix runtime first — follow the FRs.)
- Genre and cast lists are bounded scroll regions (fixed max-height ≈ 180px) so the rail keeps a predictable height as facet data arrives.
- `Clear filters` is a full-width secondary button at the rail foot, directly under the status chips it resets.
- Results column: one count line, hairline, then a plain single-column title list. No posters, no cards, no grid (Q5).
- The year extent runs 1900 → the viewer local current year, captured once at panel mount from the browser clock (`currentYearExtent(now)`); no live New Year rollover, and a fresh mount captures the then-current year. The wireframe readout above shows one mount captured in 2026.

Range control detail (the memorable element):

```text
   Runtime                          60 ──────── 240+ min      ← live readout,
   ──────────○─────────────────────                            right-aligned,
   ────────────────────────────○────                            tabular numerals
   ☐  Include unknown runtime
```

Each range shows one combined readout above two stacked native sliders (minimum slider, then maximum slider). Sighted users see `60 ──────── 240+ min`; assistive tech gets each slider's own value from the native input (§6). The readout frames are fixed-width so a change from `60` to `120` never nudges the `240+ min` end label.

### 3.2 Narrow viewport (<1024px)

Single column: panel full width above the results, same internal order and spacing. Sliders and checkbox rows stay full width; chips wrap to two rows. The count line and title list sit directly under `Clear filters` in DOM and visual order alike. No horizontal scroll at any width down to 320px.

### 3.3 Empty and error frames

```text
│                       │
│                       │   No movies match these filters.      ← frozen copy
│                       │
```

```text
│  Genres               │   12 movies
│  ┌─────────────────┐  │   ─────────────────────
│  │ ▬▬▬▬ ▬▬          │  │   Title One             ← results error: prior rows
│  │ ▬▬▬▬▬ ▬▬▬        │  │   Title Two                stay, alert line replaces
│  └─────────────────┘  │   ⚠ Movies could not be refreshed.   the count refresh
```

(Exact error wording is approved copy — §8.)

---

## 4. Component tree

All presentation lives in the four planned `web/src/features/library/` files plus `App.tsx`. Subcomponents are file-local to `FilterPanel.tsx`; no new files, so the frozen file scope does not move. Names are implementation guidance for `web-implement`, not new scope.

```text
App.tsx                                  # minimal host (Q5); readiness screen replaced
└── LibraryView.tsx                      # owns queries, URL commit, results region
    ├── FilterPanel.tsx                  # all panel controls (one file, file-local parts)
    │   ├── SearchField                  # input type=search + useDebouncedSearch
    │   ├── GenreList                    # checkbox list, count badges, bounded scroll
    │   ├── CastList                     # Filter cast box + checkbox list
    │   ├── RuntimeRange                 # 2× input type=range + include-unknown checkbox
    │   ├── YearRange                    # 2× input type=range
    │   ├── SortSelect                   # native select, 8 options
    │   ├── StatusChips                  # 4 checkbox+label chips
    │   └── ClearButton                  # button type=button
    ├── results region (in LibraryView)  # count line + title list + state frames
    ├── filterQuery.ts                   # pure URL decode/encode + normalized query
    └── useDebouncedSearch.ts            # 300 ms local hook
```

Data wiring: `LibraryView` runs three generated-client queries (movies, genres, actors) and passes facet arrays down as props; `FilterPanel` is controlled — its only inputs are the decoded `FilterQuery` and callbacks. `filterQuery.ts` has no React in it (pure functions over `URLSearchParams`), which is what makes the serialization tests (T010) free of the DOM.

---

## 5. Component states

Every region handles loading, empty, error, and partial data independently — genres may be loading while results already render.

| Region | Loading (first) | Partial (refetch) | Empty | Error |
| --- | --- | --- | --- | --- |
| Search field | usable immediately (local state is instant) | unchanged; stays mounted, fixed dimensions | — | — |
| Genre list | 5 fixed-height placeholder rows in `line` (no shimmer); controls disabled only until facets arrive | prior options stay rendered; badges keep fixed width | (empty facet array is normal: list simply renders nothing below the legend) | one `alert` line (§8); panel's other groups unaffected |
| Cast list | same placeholder pattern | prior options stay; local `Filter cast` text and selections persist | same as genres | same as genres |
| Runtime / year ranges | usable immediately (runtime extents static per Q7; year extent 1900 → viewer local current year captured at panel mount, no live rollover) | unchanged; handles stay mounted | — | — |
| Include-unknown | usable immediately | unchanged | — | — |
| Sort select | usable immediately | unchanged | — | — |
| Status chips | usable immediately | unchanged | — | — |
| Clear filters | enabled whenever any resettable panel key is non-default; sort/direction alone do not enable it | unchanged | — | — |
| Results count line | `12 movies` shape once known; before first response the frame shows a fixed-width placeholder bar | number updates only when the response lands; the region is `aria-live="polite"` | shows `0 movies` only via the empty frame below | alert line replaces the count (role `alert`) |
| Results list | fixed-height placeholder title rows in `line`, count matching the frame | **prior titles retained** (`placeholderData: keepPreviousData`, Q9), list at 0.6 opacity until data lands | `No movies match these filters.` (frozen) | prior titles retained if any; `alert` line under the count |

No control ever unmounts on refetch (FR-010, SC-003). Placeholder rows use the same fixed heights as real rows, so first load and refetch cause no layout shift.

---

## 6. Data needs

Consumed exclusively through the DEV-320 generated client and types (Q4, FR-013). Field names below are the wire contract; nothing hand-rolled.

### 6.1 Endpoints

| Need | Endpoint | Request fields | Response fields | UI binding |
| --- | --- | --- | --- | --- |
| Movie results | `GET /api/movies` | `text`, `genreIds` (repeated), `actorIds` (repeated), `runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown`, `yearMin`, `yearMax`, `statuses` (repeated), `sort`, `direction`, `page`, `pageSize`, `inWatchlist` — only panel keys change; foreign keys ride along untouched. Every request sends all four required keys `sort`, `direction`, `page`, `pageSize`, filling web defaults `Title`/`Ascending`/`1`/`20` where the URL omits them (Q12); the request query string is the canonical API query string used for equality and the movie query key | `totalCount`, `items[].id`, `items[].title` | count line ← `totalCount`; list rows ← `items[].title` (id kept for keys only) |
| Genre options + global counts | `GET /api/genres` | none | `id`, `name`, `movieCount` (required int, Q1) | checkbox value ← `id`; label ← `name` sorted by name (facet order); badge ← `movieCount` |
| Cast options | `GET /api/people?role=actor` | `role=actor` fixed | `id`, `name` | checkbox value ← `id`; label ← `name` sorted by name; list filtered client-side by the `Filter cast` text |

`movieCount` is the ticket's only contract extension (FR-011): a global total per genre, independent of active filters. The design does not restate or extend it.

### 6.2 Control → URL key map

| Control | Reads | Writes |
| --- | --- | --- |
| SearchField | `text` | `text` (after 300 ms), `page` reset |
| GenreList | `genres[].id/name/movieCount`, `genreIds` | `genreIds` (repeated, selection order), `page` reset |
| CastList | `people[].id/name`, `actorIds`, local filter text | `actorIds` (repeated, selection order), `page` reset |
| RuntimeRange | `runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown` | all three or none (Q11), `page` reset |
| YearRange | `yearMin`, `yearMax` | both or none (Q11), `page` reset |
| SortSelect | `sort`, `direction` | both or neither in the URL (default pair `Title`+`Ascending` omitted), `page` dropped from the URL and sent as `page=1` on the request like any panel-key change (Q6, Q12) |
| StatusChips | `statuses` | `statuses` or none, `page` reset |
| ClearButton | whole `FilterQuery` | drops every panel key except `sort`/`direction`, preserves foreign keys, `page` reset |

Sort labels → wire values (Q2, frozen labels): `Title (A-Z)` → `Title`+ascending; `Title (Z-A)` → `Title`+descending; `Year (oldest)` → `Year`+ascending; `Year (newest)` → `Year`+descending; `IMDb (lowest)` → `Rating`+ascending; `IMDb (highest)` → `Rating`+descending; `Runtime (shortest)` → `Runtime`+ascending; `Runtime (longest)` → `Runtime`+descending. Direction values are generated contract spellings (Q12.6), never the `Asc`/`Desc` shorthand: `Ascending` is the confirmed ascending member (recon:217-218); the descending member spelling is resolved from the inherited generated contract at Phase B pickup. The default pair `Title`+`Ascending` is omitted from the URL and always sent in the request.

Status chip labels → wire values (Q8): `Pending`→`Pending`, `Enriched`→`Enriched`, `Not found`→`NotFound`, `Failed`→`Failed`. Display labels are the frozen strings; wire values are the generated contract members.

---

## 7. Keyboard navigation and accessibility

### 7.1 Semantics and structure

- The rail is a labelled region: `<section aria-label="Filters">` (the visible "Filters" foot of the panel is decorative; the accessible name is stable). Each control group is a `<fieldset>` with a `<legend>` matching its visible legend (`Search`, `Genres`, `Cast`, `Runtime`, `Release year`, `Sort`, `Enrichment status`).
- Every control has a real label. Where the frozen string is a placeholder (`Search titles`, `Filter cast`), a visually hidden `<label>` carries the same text so the accessible name never depends on the placeholder.
- Genre/cast options are `<input type="checkbox">` + `<label>` rows inside `<ul>/<li>`; the count badge is `aria-hidden="true"` with a visually hidden `, 42 movies` suffix inside the label, so a genre announces as "Action, 42 movies".
- Range sliders carry visually hidden labels ("Runtime minimum (minutes)", "Runtime maximum (minutes)", "Release year minimum", "Release year maximum"). The visible combined readout is `aria-hidden="true"` — the native inputs already expose their values and bounds. `Include unknown runtime` is a normal labelled checkbox.
- Status chips are checkbox+label pairs styled as chips (native checked semantics, no `aria-pressed` invention). Sort is a labelled native `<select>`.
- Results region: count line wrapped in an `aria-live="polite"` container; empty and error frames are text in the same region; errors additionally carry `role="alert"`.

### 7.2 Keyboard

| Key | Behavior |
| --- | --- |
| `Tab` / `Shift+Tab` | DOM order equals visual order (§3.1 / §3.2): search → genres → cast → runtime sliders → include-unknown → year sliders → sort → chips → clear → results |
| `Space` | toggles focused checkbox/chip; activates `Clear filters` |
| `←` `→` `↑` `↓` | native slider stepping (runtime 5-minute steps, year 1-year steps) |
| `Home` / `End` | native slider extents (0 / 240+; 1900 / the mount-time local current year) |
| Arrow keys inside a checkbox group | no custom group movement is introduced; use the Tab/Space interactions above |
| `Enter` in search | no form submit; the input is not inside a `<form>` |
| Browser back/forward | `popstate` rehydrates every control from the URL without moving focus |

Focus is never stolen on filter commit or data arrival (a refetch keeps focus in the control being used). `Clear filters` keeps focus on the button; the count line announces the new total through the live region.

### 7.3 Visual accessibility

- Visible focus ring on every interactive element: 2px `brass` outline, 2px offset, on `:focus-visible`.
- Target size ≥ 24×24px for chips, checkboxes, and slider handles; ≥ 32px height for text fields, select, and button.
- Text ≥ 4.5:1 on its ground (§1.1); badge numerals use `text-dim` at ≥7:1.
- Placeholder bars and the 0.6 refetch opacity never carry meaning alone — state is also exposed in text/ARIA (§5).
- `prefers-reduced-motion: reduce` removes the 120ms transitions and the opacity dip (instant state swap instead).
- No information is conveyed by color alone: checked state is native checkbox shape, active chip fills brass *and* checks, error lines pair `alert` color with `role="alert"` text.

---

## 8. Copy inventory

Frozen strings, used verbatim (ASSUMPTIONS.md / spec.md; no alternates):

| String | Where |
| --- | --- |
| `Search titles` | search placeholder + hidden label |
| `Filter cast` | cast filter placeholder + hidden label |
| `Include unknown runtime` | runtime checkbox |
| `Clear filters` | button |
| `No movies match these filters.` | results empty state |
| `Title (A-Z)`, `Title (Z-A)`, `Year (oldest)`, `Year (newest)`, `IMDb (lowest)`, `IMDb (highest)`, `Runtime (shortest)`, `Runtime (longest)` | sort options |
| `Pending`, `Enriched`, `Not found`, `Failed` | status chips |
| `240+ min` | runtime upper stop label |

Ticket-derived structural labels (group legends and hidden slider labels): `Filters`, `Search`, `Genres`, `Cast`, `Runtime`, `Release year`, `Sort`, `Enrichment status`, `Runtime minimum (minutes)`, `Runtime maximum (minutes)`, `Release year minimum`, `Release year maximum`.

Count/error strings approved by Patron in CONCLUSIONS.md:74-77 and logged `[assumed]` in ASSUMPTIONS.md:12:

| Approved string | Where | Note |
| --- | --- | --- |
| `12 movies` / `1 movie` / `0 movies` | results count line | pluralization over `totalCount` |
| `Movies could not be refreshed.` | results error frame | retry stays the browser/client's query retry; no extra button |
| `Genres could not be loaded.` / `Cast could not be loaded.` | facet list error frames | |

---

## 9. Constraints honored / non-goals

- No change to ticket behavior, API shape, dependency set, or Q11 range serialization.
- No new file beyond the frozen plan set; subcomponents stay file-local in `FilterPanel.tsx`; `index.css`, fonts, and theme files untouched.
- No card/grid browse design (Q5); results are `totalCount` + title list only.
- No `inWatchlist` / `page` / `pageSize` controls (FR-016); no faceted counts or server-provided extents (`noted, no ticket`).
- No CLS tooling or metric claim; fixed dimensions are asserted in implementation and visual review (Q9).
- No client router, no state library, no slider/multi-select/debounce/URL package (Q3, FR-015).
- `specs/DESIGN.md` (Sprint 7) remains the home for global palette, type identity, and the component-library ruling; the tokens in §1 are feature-slice scoped and migrate there later.
