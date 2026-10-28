# DEV-425 design — Director filter combobox

Status: Phase A UI design for `ui:` ticket. Inputs: `brief.md` Q1–Q8, `spec.md` US3 / FR-011–FR-015, `plan.md` Approach UI, `tasks.md` T043–T046, `ASSUMPTIONS.md` copy, incumbent `web/src/features/library/FilterPanel.tsx` (genre chips, cast search + `CAST_SUGGESTION_LIMIT = 8`, `lamu-*` classes). Mode: Operate — precision and restraint, matches existing FilterPanel patterns. This design settles presentation only and changes no Q1–Q6 behavior, API shape, URL key, or scope.

## Route map and URL structure

No new routes. The control lives inside the existing FilterPanel on both hosts:

- Library host: existing library browse route, FilterPanel section.
- Watchlist host: existing watchlist browse route, FilterPanel section.

URL key is singular `directorId` (raw nonempty scalar, first occurrence wins, empty omits). Select, replace, Clear Director, and Clear all reset `page` to 1 and preserve all other filter values. Reload and back/forward navigation restore the selection. No change to any other query key.

## Per-page layout

Both hosts reuse one FilterPanel group order. Director sits directly after the Cast group so director and cast person-picking stay adjacent, before Runtime:

```text
FilterPanel (section, aria-label "Filters")
  [Title search group] (unchanged)
  [Genres fieldset] (unchanged)
  [Cast fieldset] (unchanged, multi-select chips)
  [Director fieldset] (NEW, this design)
  [Runtime fieldset] (unchanged)
  ... rest unchanged
```

Director group wireframe:

```text
fieldset.lamu-filter-group
  legend.lamu-filter-legend: "Director"
  [when selected]
    div selected row:
      [chip: "Name" + sr-only ", N movies"] [x Clear Director button]
  label.lamu-filter-sublabel for=input: "Director"
  input[type=search]: placeholder "Search directors"
  div status region (aria-live polite):
    [loading]  "Loading directors" (suggestions disabled)
    [error]    "Could not load directors" + "Retry" button
    [typing, hits]      listbox, max 8 rows: "Name" + count badge + sr-only count
    [typing, no hits]   "No directors found"
    [idle, empty query] no suggestion list (no auto-popup)
```

Responsive placement: inherits the panel's single-column stack at narrow widths and the panel's docked/sidebar placement at wide widths. No new breakpoints, no panel-width change. The suggestion listbox opens directly under the input, full group width, and never shifts sibling groups except by its own height. Long names wrap to two lines; the count badge stays right-aligned on the first line.

## Component tree

Conceptual structure within `FilterPanel.tsx` only — names below are in-file groupings, not extracted files; no new files or abstraction are mandated or authorized.

```text
FilterPanel
  DirectorFilterGroup (fieldset, new; props: directors, directorId,
    directorLoading, directorError, onRetry, onSelect, onClearDirector)
    SelectedDirectorChip (only when directorId set)
      name text (or "Director <id>" fallback) + count sr-only when known
      ClearDirectorButton ("Clear Director" — clears only directorId)
    DirectorSearchInput (input[type=search] with explicit role=combobox;
      aria-expanded false when closed / true when open; aria-controls only
      when listbox mounted; aria-autocomplete=list)
    DirectorSuggestionList (listbox, only while user-typed needle nonempty
      and not loading/error)
      DirectorSuggestionOption (option, up to 8; aria-selected; count badge
        aria-hidden + sr-only ", N movies")
    DirectorStatusNote (loading / error + Retry / empty messages)
```

Single-select semantics: selecting replaces the sole `directorId` (no chips accumulate, unlike Cast). Search text and selection are separate state: typing never changes the predicate; only Enter/click commits. Clearing the search text does not clear the selection; only Clear Director or Clear all does.

Same-name directors render as separate rows with identical names and distinct counts/ids. The row's accessible name includes the count so two same-name options are distinguishable.

## Component states

- Default (no selection, no text): label + empty search input. No listbox rendered, `aria-expanded=false`, no `aria-controls`.
- Typing with matches: case-insensitive substring filter over facet names, deterministic name/id order, at most 8 rows. Each row shows the name and a `lamu-chip-count`-style badge with `movieCount`. Arrow highlight moves `aria-activedescendant`; list stays open until Enter, Escape, or focus leaves.
- Typing with no matches: `No directors found`. Predicate unchanged.
- Selected: chip with resolved name (or `Director <id>` fallback when the id is absent from facet results, including during loading/error) plus Clear Director. Selection stays selected and clearable in every state; it is never silently dropped.
- Missing selection: same chip with fallback text; suggestions still searchable; predicate still applied.
- Loading: input remains focusable but suggestions disabled and listbox not rendered. Note reads `Loading directors`. All other filter groups stay usable. Selection chip (if any) stays clearable.
- Error: suggestions disabled, listbox not rendered. Note reads `Could not load directors` with a `Retry` button wired to the existing query refetch for that host. Other groups stay usable. Selection chip stays clearable.
- Clear all: existing panel-wide clear; includes `directorId` alongside every other filter.
- Idle empty: no placeholder suggestions; the facet (potentially hundreds of directors) is never dumped into the DOM — search is the only way to surface rows.

Copy source: `ASSUMPTIONS.md` — label `Director`, placeholder `Search directors`, empty `No directors found`, `Clear Director`, `Loading directors`, `Could not load directors`, `Retry`, `Director <id>`. Sentence case throughout, matching panel voice.

## Data needs (endpoint + fields)

- Facet per host (independent fetch, full-library counts, Name then Id order):
  `GET /api/directors` → `DirectorDto[] { id: int, name: string, movieCount: int }`, orphans excluded, `[]` when empty. Consumed via generated TypeScript only; no hand-rolled DTO.
- Browse predicate: existing browse request with singular `directorId` scalar forwarded verbatim; API is authoritative for int validation. No new endpoint, search, or pagination.
- No other data: no actor/director identity merge (identities stay distinct even when names match), no genre/cast payload change.

## Keyboard navigation and accessibility

Pattern: dependency-free combobox that upgrades the cast text-plus-buttons precedent to proper `combobox` / `listbox` / `option` semantics per brief Q5 (cast group currently has no combobox roles; Director must not copy that gap):

- Input (`type=search`) uses explicit `role=combobox`, with an associated visible `label`. `aria-expanded=false` when closed / `true` when open; `aria-controls=<listbox id>` only when the listbox is mounted; `aria-activedescendant` only when an active descendant exists; `aria-autocomplete=list`.
- List container uses `role=listbox`; rows use `role=option` with `aria-selected` on the active/selected row.
- ArrowDown/ArrowUp move through at most 8 suggestions; Enter commits the highlighted row as the sole selection; Escape closes the listbox without changing selection and returns focus to the input; focus is always visible.
- Status/empty/error text lives in a polite live region; the selected chip and Retry button are reachable by Tab in logical order.
- Count badges are `aria-hidden` visual with an `sr-only` ", N movies" equivalent so same-name rows differ by name.
- Reduced motion: no animation on open/select beyond the panel default; nothing auto-plays or traps focus.
- Touch/targets: suggestion rows and Clear Director meet the panel's existing minimum target sizes; the listbox does not overlap the input's clear affordance.

## Visual details (within settled semantics)

- Reuses `lamu-filter-group`, `lamu-filter-legend`, `lamu-filter-sublabel`, `lamu-field-input`, `lamu-chip`, `lamu-filter-note`, `lamu-filter-error` so Director reads as a sibling of Genres/Cast, not a new component family. One quiet distinction: the single-select chip uses the selected-chip treatment permanently (it is a value, not a toggle), while suggestion rows use the unselected chip treatment until committed.
- Visible focus ring identical to other panel inputs; error text uses the panel error style; loading text uses the panel note style.
- Boldness budget spent once: the count badge in suggestion rows. Everything else stays quiet panel chrome.

## Non-goals (frozen scope)

Multi-director selection, genre/cast behavior change, endpoint search/pagination, `libraryUrl` wiring, parser cleanup, schema/ingest/package/layer changes, and any Q1–Q6 semantic change. Anything beyond this design is a follow-up issue, not a finding.
