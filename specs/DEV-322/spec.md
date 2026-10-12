# Feature Specification: DEV-322 Filter Panel

**Feature Branch**: `feature/322-spec`

**Created**: 2026-10-12

**Status**: gate1: provisional

**Input**: Brief `specs/DEV-322/brief.md` (frozen grill receipt, Q1-Q12; Q12 at brief.md:170-181, F5 clarification at brief.md:182-188); facts `recon-DEV-322:11-63,65-106,108-187,189-234`; ticket text note `DEV-322:8,140-148`; `specs/PRODUCT.md`; taste assumptions `specs/DEV-322/ASSUMPTIONS.md`; rulings `specs/DEV-322/CONCLUSIONS.md` (Q12 at :54-58, F5 clarification at :42-45).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Filter the library and see matching movies (Priority: P1)

A user opens the library, adjusts any combination of search text, genres, cast, runtime range, year range, sort, or enrichment status, and the movie list refetches to match. The URL query always reflects the active filters, so reload and back/forward restore the same controls and results. Controls stay mounted with fixed dimensions during loading and refetch, and prior results remain visible until new data arrives.

**Why this priority**: This is the ticket's core acceptance criterion: a filter change updates the URL query and refetches movies, with fast fluid interaction and no layout shift.

**Independent Test**: Set filters in the panel, confirm `window.location.search` carries the compact URL form (defaults omitted per Q12) and the MSW-observed movie request carries the complete canonical API query (decoded URL state plus web transport defaults `sort=Title&direction=Ascending&page=1&pageSize=20`); confirm canonical equality between the expected API query (independently specified from the brief, not computed by the encoder under test) and the observed request; reload from that URL and confirm controls hydrate to the same state.

**Acceptance Scenarios**:

1. **Given** the library with default filters, **When** the user types a title and the 300 ms debounce elapses, **Then** the URL gains `text=<value>` via `replaceState` and exactly one movie fetch is issued with that text.
2. **Given** any active filter set, **When** the user changes one discrete control, **Then** the URL updates through `replaceState` and exactly one movie fetch fires for the committed normalized query.
3. **Given** a pending refetch, **When** the response has not yet arrived, **Then** all controls stay mounted with fixed dimensions and the previous titles remain visible (`placeholderData: keepPreviousData`).
4. **Given** a URL carrying filter keys, **When** the page reloads or a `popstate` event fires, **Then** every control hydrates to the URL state.
5. **Given** active filters, **When** the user presses `Clear filters`, **Then** every panel key resets except `sort`/`direction`, the URL drops those keys, and movies refetch.

---

### User Story 2 - Browse by genre and cast with counts (Priority: P2)

A user picks one or more genres (each showing a global movie-count badge) and one or more cast members (filterable through a `Filter cast` text box), and the list narrows to matching movies. Genre badges show total movies per genre independent of active filters.

**Why this priority**: Genre multi-select with count badges and cast multi-select are the ticket's named discovery controls; the count badge is the only public contract extension (`GenreDto.movieCount`).

**Independent Test**: Select genres and cast members, confirm repeated `genreIds`/`actorIds` keys preserve selection order in the URL and the movie request, and confirm each genre badge shows its global count.

**Acceptance Scenarios**:

1. **Given** stored genres with movies, **When** `GET /api/genres` returns, **Then** each entry carries required integer `movieCount` equal to the total movies carrying that genre.
2. **Given** the genre list, **When** rendered, **Then** options appear sorted by name with a fixed-width count badge beside each name.
3. **Given** the cast list, **When** the user types in `Filter cast`, **Then** the cast choices narrow client-side and selections persist.
4. **Given** selected genres or cast, **When** the query commits, **Then** the URL carries repeated `genreIds`/`actorIds` keys in selection order.

---

### User Story 3 - Narrow by runtime, year, sort, and status (Priority: P3)

A user bounds runtime (two native sliders plus `Include unknown runtime`), bounds release year (two native sliders), picks one of eight sort choices, and toggles enrichment-status chips, and the list refetches accordingly.

**Why this priority**: Ranges, sort, and status chips complete the ticket-named control set; range serialization follows the authoritative Q11 all-or-nothing rule.

**Independent Test**: Move each range end, toggle include-unknown and status chips, change sort; confirm the URL group keys and the movie request params match the Q11 encoding in every combination.

**Acceptance Scenarios**:

1. **Given** both runtime ends at extents (0 and 240+) with include-unknown unchecked, **When** the query commits, **Then** no runtime key is sent.
2. **Given** either runtime end moved or include-unknown checked, **When** the query commits, **Then** all three keys `runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown` are sent with explicit `true`/`false`, and an end at its extent serializes as an empty value (e.g. `runtimeMin=60&runtimeMax=&runtimeIncludeUnknown=false` means 60 minutes and up, excluding unknowns).
3. **Given** both year ends at extents (1900 and current year), **When** the query commits, **Then** no year key is sent; **When** either moves, **Then** both `yearMin` and `yearMax` are sent with empty values at extent stops.
4. **Given** the sort dropdown, **When** the user picks any of the eight choices, **Then** the request sends the matching `sort` plus `direction`, with the `IMDb` label sending `sort=Rating`.
5. **Given** no status chip selected, **When** the query commits, **Then** no `statuses` parameter is sent (all statuses); **When** chips are selected, **Then** the generated contract values are sent.

---

### Edge Cases

- A filter change resets `page` but preserves foreign URL keys (`inWatchlist`, `pageSize`, and any unknown keys) untouched.
- Rapid typing cancels stale pending text commits; only the settled value commits after 300 ms of quiet.
- An empty value at a range extent means an open bound, never an omitted member: `runtimeMax=` with an active runtime group is 240+ (open upper bound), not a dropped key; empty year ends never silently exclude pre-1900 or future-year titles.
- A partial range group is never sent: the existing API rejects it with 422 Unprocessable Entity (recon-DEV-322:147-148; constitution principle V; corrected CONCLUSIONS.md:47-52), so the panel always sends the whole group or omits it. Q11 changes neither the existing status code nor backend range behavior.
- Zero movies for a genre yields `movieCount` zero; an empty genre table yields an empty array.
- A movie assigned to multiple genres contributes once to each genre's count.
- Empty results render `No movies match these filters.`
- Text typed but not yet debounced is transient interaction state only, never a second committed filter state.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The panel MUST offer debounced free-text search committing `text` 300 ms after the last keystroke via `replaceState`.
- **FR-002**: The panel MUST offer genre multi-select as a native checkbox list with global `movieCount` badges sorted by name.
- **FR-003**: The panel MUST offer cast multi-select as a native checkbox list with a client-side `Filter cast` text box, options sorted by name, sourced from `GET /api/people?role=actor`.
- **FR-004**: The panel MUST offer a runtime range with two native `input type=range` controls (0..240 min, step 5, top stop shown `240+ min`) plus an `Include unknown runtime` checkbox, serialized as the all-or-nothing group `runtimeMin`, `runtimeMax`, `runtimeIncludeUnknown` per Q11.
- **FR-005**: The panel MUST offer a release-year range with two native `input type=range` controls (1900..current year, step 1), serialized as the all-or-nothing group `yearMin`, `yearMax` per Q11.
- **FR-006**: The panel MUST offer eight sort choices (`Title (A-Z)`, `Title (Z-A)`, `Year (oldest)`, `Year (newest)`, `IMDb (lowest)`, `IMDb (highest)`, `Runtime (shortest)`, `Runtime (longest)`), sent as `sort` plus `direction`; `IMDb` sends `sort=Rating` (generated enum spellings; `Ascending`, not `Asc`); default `Title (A-Z)` is omitted from the URL, while every movie API request carries web transport defaults `sort=Title`, `direction=Ascending`, `page=1`, `pageSize=20` after normalization (Q12; no backend default change).
- **FR-007**: The panel MUST offer four enrichment-status toggle chips (`Pending`, `Enriched`, `Not found`, `Failed`); none selected sends no `statuses` param.
- **FR-008**: Every committed normalized query change MUST update the compact URL (arrays as repeated keys preserving selection order, defaults omitted subject to Q11 group completeness) and trigger exactly one movie fetch against the complete canonical API query (decoded URL state plus Q12 transport defaults; the canonical API query string is also the movie query key, so equivalent omitted/explicit defaults yield the same key and no extra fetch).
- **FR-009**: The URL MUST be the committed source of truth: reload and `popstate` hydrate all controls; a filter change drops `page` from the URL and sends `page=1` in the API request; supplied foreign keys (`pageSize`, `inWatchlist`, unknown keys) are preserved in the URL, while unknown keys never enter the API query or its key; defaults fill absent required API values without overwriting supplied values.
- **FR-010**: Controls MUST keep fixed dimensions (including fixed-width badges) and stay mounted during loading/refetch; results MUST retain prior data during a pending refetch (`placeholderData: keepPreviousData`).
- **FR-011**: `GET /api/genres` MUST return required integer `movieCount` per genre (global total over existing relationships), carried through `GenreFacet` and the endpoint mapping; no new endpoint, status-code change, table, or migration.
- **FR-012**: The panel MUST live in `web/src/features/library/` and mount in `App.tsx` with only `totalCount` and a title list, unless the Phase B base already supplies a library page, in which case it integrates there and leaves `App.tsx` alone. No card/grid feature is added.
- **FR-013**: Frontend code MUST consume DEV-320 generated API types/client; no hand-written DTO duplicates and no hand edits to generated files (`openapi.json`, `types.ts`).
- **FR-014**: The panel MUST provide a `Clear filters` button resetting every panel key except `sort`/`direction`; the search box placeholder MUST read `Search titles`.
- **FR-015**: No new npm dependency is permitted for slider, multi-select, debounce, router, or URL state (native controls, a small local debounce hook, `URLSearchParams` with `history.replaceState`/`popstate`); TanStack Query is the sole permitted addition, only if absent at Phase B pickup and pinned as the constitution-chosen dependency.
- **FR-016**: Playback execution, `Features:LocalPlay`, secrets, and `Process.Start` are out of scope; `inWatchlist`, `page`, and `pageSize` are not panel controls.

### Key Entities

- **FilterQuery**: The committed panel state (text, genreIds, actorIds, runtime group, year group, statuses, sort, direction); serializes to/from the compact URL (defaults omitted); normalizes to the complete canonical API query (all four Q12 transport defaults present) used for the movie request and as the movie query key; canonicalization covers recognized API keys only, preserves repeated-array order and Q11 empty bound values.
- **GenreFacet**: Backend facet (id, name, movieCount) where movieCount is the global total of movies carrying the genre.
- **MovieSummary**: Browsed item (id, title) with result `totalCount`, consumed from the generated `BrowseMoviesResponse`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every filter change updates the compact URL query and triggers exactly one matching movie fetch (canonical equality: the independently expected canonical API query equals the canonicalized MSW-observed request; an empty panel URL requests `sort=Title&direction=Ascending&page=1&pageSize=20`).
- **SC-002**: Reloading from any panel URL, and back/forward navigation, restores all controls to the URL state.
- **SC-003**: During any pending refetch, all controls remain mounted and the previous titles remain visible; no control unmounts.
- **SC-004**: Every genre badge shows the correct global count (zero when no movies; per-genre totals with shared movies counted once each), and `GET /api/genres` exposes required integer `movieCount` with the exact additive property set.
- **SC-005**: All eight sort choices, all four status chips, both range sliders with steps/extents, genre/cast multi-select, and the 300 ms debounced search behave per the acceptance scenarios, including one-sided ranges and include-unknown alone activating the runtime group.

## Assumptions

- Taste assumptions in `specs/DEV-322/ASSUMPTIONS.md` (Q2 sort labels, Q8 chip labels, Q9 300 ms debounce and placeholder, Q7 slider extents, Q11 include-unknown label/default, genre/cast presentation, `Clear filters` behavior and empty-state copy).
- Year slider upper extent is the viewer local current year from the browser clock, captured once at panel mount via the pure `currentYearExtent(now)` seam in `FilterPanel.tsx` (no live rollover, no clock package, no backend request); tests drive it with `vi.setSystemTime`; no wall-clock sleeps (fake timers). Backend `TimeProvider` conventions stay backend-only.
- DEV-320 generated `types.ts` and client/codegen facilities are a Phase B pickup prerequisite, not present at this HEAD (recon-DEV-322:226-234): T001 must stop Phase B pickup unless bounded recon on the actual inherited base identifies generated types/client, the exact inherited codegen command and prerequisites, and a repeatable regeneration path after an OpenAPI delta; Q1 regeneration then runs through DEV-320 tooling in contract-chain order (C# DTO, committed `openapi.json`, generated TypeScript).
- Palette `design.md` may settle presentation within the feature slice but cannot alter frozen ticket behavior, API shape, dependency choices, or range serialization.
- Faceted (filter-narrowed) counts, server-provided slider extents, `inWatchlist`/`page`/`pageSize` as panel controls: out of scope (`noted, no ticket`).
