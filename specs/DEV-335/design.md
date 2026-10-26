# DEV-335 — Design: IMDb rating range filter

Status: Phase 2 design draft for `feature/335-spec`.
Basis: `specs/DEV-335/brief.md` Q4 (ACCEPT), `specs/DEV-335/ASSUMPTIONS.md` taste rulings,
`specs/DEV-335/spec.md` US3/FR-009, recon `recon-DEV-335` §2.6.
Mode: Operate (precision and restraint; incumbent `DualRangeField` pattern wins over expression).

No new route, component package, dependency, or CSS. This is a refinement of the existing
FilterPanel, not a new visual world.

## 1. Route map and URL structure

No new route. The control lives on the existing library browse route inside `LibraryView`,
which mounts the single `FilterPanel` caller (`LibraryView.tsx`, recon §2.6).

URL ownership stays `filterQuery.ts` (`toUrlQuery` / `parseFilterQuery`). New paired keys:

- `minRating`, `maxRating` — canonical ticket spelling (camelCase raw wire). Both keys present =
  supplied group; neither key = absent group (`Rating` null). Either value may be empty for an
  open bound. Valid present groups always emit both keys (`null`→empty, `0`→`0`, `10`→`10`);
  never apply runtime/year extent-elision. Canonical emitted values are shortest invariant ≤1
  fractional digit (`7`, `7.1`, `0`, `10`). Slider float noise is normalized once in
  `filterQuery.ts` on interaction; raw URL text is never repaired.
- Single-key / malformed / domain / precision / order-invalid URLs preserve raw supplied key
  presence and values through URL state and outgoing browse params (a partial group stays
  partial); HTTP 422 uses the existing invalid-filter UI. Do not invent a missing pair.
- Page resets to 1 on every rating change via existing `commit` (`FilterPanel.tsx:165-184`).
- Other filters and unrecognized URL keys are preserved (existing `RECOGNIZED_KEYS` behavior);
  unrelated filter changes must not rewrite raw rating data.
- Test-only `libraryUrl.ts` is untouched; `browseParams.ts` + `filterQuery.ts` are the live path.

Example URL states:

- Absent (no filter): `/library` — no `minRating`/`maxRating` keys emitted.
- Active floor only: `/library?minRating=7&maxRating=` — means rating >= 7.0.
- Active bounded: `/library?minRating=7&maxRating=9.5`.
- Active full-span: `/library?minRating=0&maxRating=10` — semantically active, excludes unrated.
- Invalid / partial (preserved raw): e.g. `/library?minRating=abc&maxRating=5` or a lone
  `minRating=7` — raw keys/values kept until deliberate rating interaction or Clear all.

## 2. Panel layout wireframe

Insertion point: new group directly after the Release year fieldset, before Enrichment
status (per `[assumed]` taste ruling). All other groups keep their order.

```text
<section aria-label="Filters">
  [Search titles              ]
  (Genres chips…)
  (Cast…)
  (Runtime dual slider + Include unknown runtime)
  (Release year dual slider)
  +------------------------------------------+
  | IMDb rating                              |
  |  7.0                          9.5        |  <- one-decimal captions
  |  o===========amber-fill===========o      |  <- DualRangeField track
  |  [min thumb]  [max thumb]                |  <- two native range inputs
  +------------------------------------------+
  (Enrichment status chips…)
  (Sort chips…)
  <footer> N matches | Clear all | Collapse </footer>
</section>
```

Captions: one decimal (`7.0`, never `7`), mirroring the existing runtime/year caption row
(`lamu-range-caption`). Max-end display follows the runtime `240+` precedent only in spirit:
rating shows the literal bound (`10.0`), never `10+`, since 10.0 is a real inclusive bound.

## 3. Component tree

```text
LibraryView
└─ FilterPanel (integrated, query, onQueryChange, matchCount={totalCount}, routeKey)
   └─ fieldset.lamu-filter-group > legend "IMDb rating"
      └─ DualRangeField (min=0, max=10, step=0.1)   <- reuse, FilterPanel.tsx:81-124
         ├─ input[type=range] #…-rating-min  (aria-label "IMDb rating minimum")
         └─ input[type=range] #…-rating-max  (aria-label "IMDb rating maximum")
```

Props and wiring (mirrors the Release year block, `FilterPanel.tsx:478-503`):

- `low`: id `<ids>-rating-min`, accessibleName `IMDb rating minimum`,
  valueText one-decimal minimum, value = open-bound-hydrated display value,
  `onValueChange` writes `next.minRating` (materializes finite bound, see §4).
- `high`: id `<ids>-rating-max`, accessibleName `IMDb rating maximum`, symmetric.
- State flow (existing machinery, no new path):
  thumb change → `update` → `commit` (page=1, `history.replaceState` when `!integrated`)
  → `onQueryChange` → LibraryView state → `buildBrowseParams` → new
  `queryKey: ['browseMovies', browseParams]` → refetch → grid + match count.
- Active-group chip: rating counts as one group when present
  (extends `activeFilterGroups`, `LibraryView.tsx:68-76`, alongside `isYearActive`).
- `handleClear` (`FilterPanel.tsx:295-311`) resets `minRating`/`maxRating` to absent.

## 4. Component states

| State | Slider display | URL | Predicate effect |
|---|---|---|---|
| Inactive (absent) | `[0, 10]`, captions `0.0` / `10.0` | keys omitted | no predicate; unrated eligible |
| First thumb move | moved thumb value | paired keys appear | active; unrated excluded |
| Bounded active | `[lo, hi]` | `minRating=lo&maxRating=hi` | inclusive both ends |
| Open bound hydrated | open end shows `0` / `10` | empty value kept (`maxRating=`) | open end unbounded; unrated excluded |
| Post-interaction | finite both ends | both values filled | closed range |
| Returned to `[0,10]` | `[0, 10]` | keys present (`0`/`10`) | still active; unrated excluded |
| Clear all | `[0, 10]` | keys removed | absent again |
| Invalid / partial / domain / precision / order failure | safe non-crossing `[0, 10]` | raw supplied key presence and values preserved (partial stays partial) | fetch hits existing HTTP 422 invalid-filter UI; no predicate success |

Rules:

- Absent vs. active full-span is semantic, not visual: identical thumb positions, different
  presence flag. Extent-elision in serializers must not collapse active `[0,10]` to absent;
  emit `minRating=0&maxRating=10` whenever that present group is serialized.
- Hydration never rewrites empty URL values. Display substitutes 0/10 at the open end;
  only an actual thumb interaction materializes the finite bound into state/URL.
- Thumbs never cross (existing clamp: low ≤ high, `DualRangeField` comment `:78-79`).
- Loading: existing panel/fetch pending state; no skeleton specific to this control.
- Error / invalid recognized rating input: no per-control error UI and no `libraryQueries.ts`
  edit. Preserve raw supplied key presence and values through URL state and outgoing browse
  params (including partial groups). Display safe non-crossing `[0,10]` without mutating the
  URL. Existing HTTP 422 invalid-filter UI handles the fetch. Deliberate rating-thumb
  interaction replaces BOTH bounds with valid finite displayed bounds; Clear all removes both.
  Unrelated filter changes must not repair or rewrite raw rating data. (Patron Round 1 ruling;
  supersedes any nearest-extent invention.)
- Empty grid: existing empty-state copy and count presentation unchanged (`N matches` footer).
- Partial data: unrated movies simply disappear from the grid while active; no placeholder row.

## 5. Data needs (endpoint + fields)

Single endpoint: `GET /api/movies` (existing `LibraryEndpoints` route).

New API params (via regenerated OpenAPI → `types.ts` → `browseParams.ts` → MSW):

- OpenAPI / generated TypeScript names: PascalCase `MinRating` / `MaxRating` (existing
  documented convention; exact parameter-list equality retained; no new DTO; never hand-edit
  generated files).
- Canonical raw URL / endpoint wire keys: camelCase `minRating` / `maxRating` (ticket
  spelling; case-insensitive binding). Browse builders consume generated names and emit the
  canonical camelCase wire keys.
- Wire values: invariant decimal, shortest ≤1 fractional digit, `0..10`, or empty string for
  open bound. Present (both keys) whenever the rating group is active — including full-span
  `0`/`10` and both-empty open groups. Invalid/partial raw values may still be present on
  outgoing params so the existing 422 path can fire.

Reused params (unchanged): `text`, `genreIds`, `actorIds`, `runtimeMin/Max/IncludeUnknown`,
`yearMin/Max`, `statuses`, `sort`, `direction`, `page`, `pageSize`.

Read model:

- `matchCount` / `totalCount` → footer `N matches` and LibraryView grid count.
- `MovieSummary.imdbRating` (existing projection) → card badge; `null` rows are absent
  from results while the range is active, so no per-card unrated treatment is needed here.

## 6. Keyboard navigation and accessibility

Checklist applied from `web-design-guidelines` (native-control posture):

- Native `<input type="range">` thumbs: Tab reaches min then max; arrow keys move by 0.1;
  Home/End jump to extent ends. No custom key handling.
- Accessible names: `IMDb rating minimum` / `IMDb rating maximum` via `aria-label`
  (matches the `Runtime minimum (minutes)` precedent, `FilterPanel.tsx:440-450`).
- Group semantics: `fieldset` + `legend` `IMDb rating` — one group, announced once.
- Captions (`7.0` / `9.5`) are `aria-hidden` display text (existing `lamu-range-caption`
  pattern); the input values are the source of truth for assistive tech.
- Visible focus: existing `.lamu-range` focus styling in `library.css:360-397`; no new CSS.
- Non-crossing clamp keeps both thumbs reachable (existing `lowOnTop` z-index handling).
- Reduced motion: no animation on this control; nothing to gate.
- Contrast and touch targets: inherited from existing range CSS; no change.
- Clear all is a plain button (`Clear all`), reachable by keyboard, resets the group.

## 7. Visual tokens

Inherit everything: `lamu-filter-group`, `lamu-filter-legend`, `lamu-range-field`,
`lamu-dual-range` amber fill, `lamu-range` track/thumb. Sentence-case legend `IMDb rating`.
No new color, type, spacing, or radius. Type scale and empty-grid/count copy unchanged.

## 8. Non-goals (guardrails for plan challenge)

- No `libraryUrl.ts` change, no sort-ordering change, no include-unrated toggle.
- No schema migration (`imdb_rating numeric(3,1)` exists), no new dependency or project.
- Generated artifacts (`openapi.json`, snapshot, `types.ts`, `handlers.ts`) are outputs of
  `LAMUFLIX_REGENERATE_OPENAPI=true` + `npm run generate:api`, never hand-edited.
