# DEV-424 — Collection Facet Filter Design

**Status:** Phase 2 step 6 — Ledger F1/F5 + Compass Low1 corrections applied.
**Authority:** Ticket DEV-424 (`ui:`); frozen `brief.md` Q1–Q8; `spec.md` US4 / FR-008; `CONCLUSIONS.md` Q4 + C1 + Phase 2 step 6 ruling 3; `ASSUMPTIONS.md` taste labels; `specs/PRODUCT.md` taste rules.
**Scope:** Filter Panel Collection section only. No grid grouping, no backfill UI, no new component vocabulary or state library. Visual layout is owned here; interaction contract is frozen by Q4 and step 6 ruling 3.
**Mode:** Operate — the visitor completes a task (narrow the library grid). Precision and consistency with the existing Genre chips outrank expression. One memorable distinction only: single-select toggle semantics in a multi-select panel.

## Design intent

Collection filtering mirrors the Genre facet chip pattern file-for-file: same `fieldset.lamu-filter-group` + `legend.lamu-filter-legend` section shell, same `label.lamu-chip` controls each wrapping a controlled `input[type=checkbox].sr-only`, same visible count badge + screen-reader suffix, same `p[role=alert].lamu-filter-error` independent-error plumbing, same active-filter chip row. The single deliberate difference is selection behavior — one selected collection at a time (check selects, check another replaces, uncheck the active clears) — because a movie has zero or one membership (Q2). Everything else stays quiet and disciplined: no new palette, type, panel layout, skeleton, `aria-busy`, `aria-pressed` buttons, or radios.

Taste labels (already decided, `ASSUMPTIONS.md`): section legend `Collection`; unknown-id fallback label `Collection <id>` shown only in the toolbar; empty facet shows the legend only with no selectable options and no helper copy.

## Route map and URL structure

No new routes. All state lives on the existing Library browse route via query string.

- Library grid (existing route, e.g. `/library`): reads and writes single URL key `collectionId` carrying one positive integer.
- `collectionId` absent → unconstrained (existing behavior, no constraint).
- `collectionId=<positive id>` → grid filtered to that collection's members, ANDed with all other active filters/sort.
- `collectionId=<valid unknown id>` → stays applied in URL and toolbar with fallback label; grid shows the empty page (never silently broadened).
- Selection change or clear resets `page` to 1 and preserves every other filter/sort key (same page-reset rule as other filters).
- Reload, back/forward restore the selection from the URL.

## Per-page layout (text wireframe)

Library view, Filter Panel open (integrated and docked variants unchanged). Section order: Title search → Genres → Collection (NEW) → Cast → Runtime → Release year → IMDb rating → Enrichment status → Sort → footer.

```text
+----------------------------------------------------------+
| Library toolbar: search, sort, active-filter chips  [x]  |
|   [Collection: <Name|Collection <id>> x] [Clear all]     |
|   (fallback Collection <id> chip stays here during       |
|    collection loading, error, and empty states)           |
+----------------------------------------------------------+
| Filter Panel                | Browse grid (unchanged)      |
|  Title search (unchanged)   |  movie cards ...             |
|  fieldset Genres (unchanged)|                              |
|   legend: Genres            |                              |
|   label.lamu-chip × n       |                              |
|  fieldset Collection (NEW)  |  empty page when filter      |
|   legend: Collection        |  matches nothing             |
|   label.lamu-chip Name (n)* |                              |
|   label.lamu-chip Name (n)  |                              |
|   ... sorted name, then id  |                              |
|   --OR-- p[role=alert]      |                              |
|   "Collections could not    |                              |
|   be loaded." (existing     |                              |
|   inline-alert pattern)     |                              |
|  fieldset Cast (unchanged)  |                              |
|  Runtime / Year / etc.      |                              |
|  footer [Clear all]         |                              |
+-----------------------------+--------------------------------+
```

`*` = checked `input[type=checkbox].sr-only` inside the `label.lamu-chip-selected` (controlled single-select toggle, never `aria-pressed`, never radio).

## Component tree

Existing components reused; one new props branch. No new component vocabulary.

```text
LibraryView
  Toolbar active-filter chip row (existing: .library-chips >
    ul[aria-label="Active filters"] > li > span.lamu-chip +
    button.lamu-chip-remove, plus Clear all)
    ActiveFilterChip "Collection: <name|Collection <id>>" (removable, existing chip;
      fallback present during collection loading, error, and empty states)
    ClearAll (existing; now also clears collectionId)
  FilterPanel (existing; new props only)
    CollectionSection (new fieldset.lamu-filter-group inside FilterPanel,
      same section shell as Genre: legend.lamu-filter-legend "Collection")
      ChipList (existing .lamu-chip-row of label.lamu-chip controls, each:
        input[type=checkbox].sr-only[checked + onChange] +
        span name + span.lamu-chip-count[aria-hidden] + span.sr-only ", N movies")
      SectionError (existing independent-error pattern:
        p[role=alert].lamu-filter-error; collection errors
        keep every other section usable)
  BrowseGrid (unchanged; consumes filtered page)
```

`FilterPanel` gains collection props mirroring the genre props (`collections`, `collectionError`-style seams — exact prop names are implementation's choice within the existing pattern). There is no Collection option inside the section for an unknown id; the fallback lives only in the toolbar row above.

## Component states

| State | Collection section | Active chip / grid |
|---|---|---|
| Loading facet | `legend: Collection` + empty `.lamu-chip-row` via the existing `client.GET` facet query pattern; no new skeleton, no `aria-busy`; other sections interactive | Fallback `Collection <id>` chip retained in the toolbar when the URL holds an unknown id; grid shows existing loading state |
| Loaded, nonempty | `label.lamu-chip` per collection sorted name then id, each with `movieCount` badge + sr suffix | Normal grid |
| Empty facet | `legend: Collection` only; no chips, no helper copy, no placeholder decoration, no disabled decoys | No section option exists so no active chip can arise from it; a URL-held fallback chip is still retained; grid unaffected |
| Facet error | Existing inline `p[role=alert].lamu-filter-error` in the section; genres/cast stay usable (existing independent-error pattern); no new alert vocabulary | Fallback `Collection <id>` chip retained in the toolbar when applied; grid unaffected; retry follows existing facet retry |
| Checked (select) | Checking an unchecked chip selects that single id (`label.lamu-chip-selected`); URL holds `collectionId` | Removable `Collection: <name>` chip in toolbar; grid AND-filtered; page reset to 1 |
| Replace | Checking another chip swaps the single selection (never multi-select) | URL id replaced; page reset; other filters/sort preserved |
| Uncheck active (re-select) | Unchecking the checked chip clears the filter (radios rejected: reselect-to-clear is required) | Chip removed; `collectionId` dropped from URL; page reset |
| Unknown valid id in URL | Never rendered as a section option | Fallback `Collection <id>` chip retained in the toolbar during loading, error, and empty states; grid shows empty page; result is never silently broadened |
| Clear-all | Selection cleared with all other filters | URL key removed; page reset |
| Partial data (some rows missing names) | Cannot occur: malformed id/blank name never persists (Q5); only valid rows reach the facet | — |

Invalid `collectionId` input (malformed, nonpositive, repeated single-valued key) is a backend 422 ProblemDetails concern (C1 + step 6 ruling 4); the UI never constructs such URLs — it writes one positive integer or removes the key.

## Data needs (endpoint + fields)

Consumed via generated types only (`components['schemas'][...]`); no hand-rolled DTOs; contract path stays C# DTO → `openapi.json` → `types.ts`.

- Facet list: `GET /api/collections` (operation `GetCollections`) → `CollectionDto[]` with `id` (positive int, TMDb id), `name` (string), `movieCount` (int, distinct current library members). Only nonempty collections returned; ordering name then id; counts never narrowed by active filters. Fetch reuses the existing `client.GET` facet query pattern with its loading/error behavior (no new skeleton or busy semantics).
- Browse: existing browse request gains `collectionId` (nullable positive int); absent = unconstrained; valid unknown = empty page. URL key `collectionId` round-trips through `filterQuery` / `libraryUrl` / `browseParams` codecs alongside existing keys; page resets on change/clear.
- Mocks/fixtures: extend existing `handlers.ts` / `fixtures.ts` collection facet entries following the genre mock shape.

## Keyboard navigation and accessibility

- Chips are `label.lamu-chip` controls wrapping native controlled checkboxes (`input[type=checkbox].sr-only`), reachable by Tab in DOM order (sorted name, then id); toggled by Space like every other facet chip. No `button[aria-pressed]`, no radios, no `aria-multiselectable` on this section.
- Controlled single-select toggle communicated by the checkbox checked state plus the toolbar's single removable `Collection: <name|Collection <id>>` chip; unchecking the active chip clears the filter.
- The section label is the real `legend` of its `fieldset` (same level as Genre/Cast legends); the chip row is the existing `.lamu-chip-row` group inside it.
- Visible focus indicator identical to existing chips; no new focus style.
- Screen-reader name per chip includes the count (existing genre pattern: visible badge + accessible suffix, e.g. announced "<name>, N movies").
- Unknown-id fallback chip announces `Collection <id>` from the toolbar so the applied filter is never silent; it is never a section option.
- Empty facet exposes the legend only; no helper copy and no disabled decoy chips to tab through.
- Loading adds no `aria-busy` and no skeleton; errors use the existing inline `p[role=alert]` region (assertive only on change, per existing pattern).
- Reduced motion: no entrance or hover animation beyond what chips already do; state changes are instant checked-attribute swaps, never motion-dependent.
- Color/contrast: reuse existing chip, badge, and error tokens; selection must not rely on color alone (checked state + label change carry it).
- Responsive: section stacks with the existing panel; no horizontal scroll introduced; touch targets match existing chips.

## Visual guidance (restraint)

- Reuse `lamu-chip` sizing, radius, badge placement, and section spacing exactly; the Collection section is visually indistinguishable from Genre except its legend text.
- No new accent, badge color, iconography, panel decoration, skeleton, or alert style. The boldest thing on the panel remains the grid content, not the filter.
- Copy stays sentence case and plain: `Collection`, `Collection <id>`, existing clear-all and inline-error wording. No placeholder or helper text.
