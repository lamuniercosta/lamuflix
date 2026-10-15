# Feature Specification: DEV-324 Watchlist Toggle and Filter (React SPA)

**Feature Branch**: `feature/DEV-324-implement-watchlist-toggle-on-cards-and-dedicated-watchlist`

**Created**: 2026-10-12

**Status**: gate1: provisional

**Input**: `specs/DEV-324/brief.md` (Patron grill Q1–Q12 plus Q3 addendum), `specs/DEV-324/CONCLUSIONS.md`, `specs/DEV-324/ASSUMPTIONS.md`, notes `DEV-324`, `recon-DEV-324` with parts `-2`, `-3`, `-4`, `-5`, `-6`, `specs/PRODUCT.md`.

## User Scenarios & Testing

### User Story 1 - Bookmark toggle on card and details hero (Priority: P1)

A user browsing movies toggles the bookmark on a movie card or on the details hero and sees the state flip instantly, before the API responds.

**Why this priority**: Ticket acceptance criterion 1 ("instant bookmark feedback"); the core deliverable of S&TD 1.

**Independent Test**: With the delivered card and hero hosts present, click the toggle with a delayed MSW response and observe the bookmark state change before the response arrives; toggle in one surface and confirm the other reflects it after navigation. US1 standalone excludes the filter-active scenario 6 below; scenario 6 is verified with US2 in Phase 4 after filter integration.

**Acceptance Scenarios**:

1. **Given** a movie not in the watchlist, **When** the user clicks the bookmark, **Then** it shows the on state immediately and POST `/api/movies/{id}/watchlist` is sent.
2. **Given** a movie in the watchlist, **When** the user clicks the bookmark, **Then** it shows the off state immediately and DELETE `/api/movies/{id}/watchlist` is sent.
3. **Given** the API returns 204 or 409, **When** the response arrives, **Then** the optimistic state is retained.
4. **Given** the API returns 404, 422, 500, a network failure, or any other failure, **When** the response arrives, **Then** the toggle rolls back and shows inline error "Couldn't update watchlist. Try again."
5. **Given** a toggle request in flight, **When** the user clicks again, **Then** the click is ignored and the button shows `aria-busy`.
6. **Given** the watchlist filter is active, **When** removal succeeds, **Then** the card leaves the list; **When** removal fails, **Then** the card stays. (Requires US2 filter integration; verified in Phase 4, not in US1 standalone.)

---

### User Story 2 - Watchlist quick filter (Priority: P2)

A user clicks the Watchlist quick filter and sees only watchlist movies; the state is reflected in the SPA URL.

**Why this priority**: Ticket acceptance criterion 2 ("correct watchlist-only filter of the browsed movie list"); S&TD 2.

**Independent Test**: Toggle the filter on and off and verify the movie list, the URL, the page reset, and the API `inWatchlist` parameter.

**Acceptance Scenarios**:

1. **Given** the filter is off, **When** the user turns it on, **Then** the URL gains `watchlist=true`, paging resets to page 1, other filters are retained, and the browse request sends `inWatchlist=true`.
2. **Given** the filter is on, **When** the user turns it off, **Then** the URL drops the key and the browse request sends no `inWatchlist` filter.
3. **Given** URL values absent, `false`, or anything other than `true`, **When** the browse loads, **Then** no `inWatchlist` filter is sent.
4. **Given** other filters are set, **When** the watchlist filter is combined with them, **Then** they combine with AND.
5. **Given** the filter is on and nothing matches, **When** the list renders, **Then** it shows "Your watchlist is empty."

---

### Edge Cases

- 409 on double add or remove counts as success and retains the target state.
- In-flight clicks are ignored; `aria-busy`, accessible name ("Add to watchlist" / "Remove from watchlist"), and `aria-pressed` reflect state.
- Card and hero share one movie's `isInWatchlist` state, so a toggle in one surface shows in the other after navigation.
- If the DEV-323 delivery has no distinct hero block, the toggle goes next to the title in `MovieDetailsView`; extracting a hero component stays out of scope.
- If DEV-323 delivers no details view at pickup, work stops with `needs decision:` rather than building one.

## Requirements

### Functional Requirements

- **FR-001**: SPA MUST render a bookmark toggle on the composed browse item `MovieRow` in `web/src/library/LibraryView.tsx` (F2) and in the hero region (backdrop/poster/title) of the delivered DEV-323 `MovieDetailsView` (mounted via `App.tsx`); DEV-324 builds no host and MUST NOT compose the orphaned `MovieGrid`/`MovieCard`.
- **FR-002**: Toggle MUST update state optimistically before the API response, using React 19 built-ins in one shared hook (or an already-delivered sibling cache at pickup); no new dependency or state library.
- **FR-003**: Toggle MUST call the generated client with generated types for POST/DELETE `/api/movies/{id}/watchlist`; no hand-written request or response types.
- **FR-004**: Toggle MUST retain state on 204 and 409, and MUST roll back with a non-blocking inline error on 404, 422, 500, network failure, or any other failure.
- **FR-005**: Toggle MUST ignore repeat clicks while a request is in flight and expose `aria-busy`, the settled accessible name, and `aria-pressed`.
- **FR-006**: Card and hero MUST read the same movie's watchlist state from the shared hook or cache across navigation.
- **FR-007**: With the watchlist filter active, successful removal MUST remove the card; failed removal MUST keep it.
- **FR-008**: SPA MUST translate only URL value `watchlist=true` to API `inWatchlist=true`; absent, `false`, and other values leave the filter off.
- **FR-009**: Switching the filter MUST update the URL, reset paging to page 1, retain other filters, and combine with AND.
- **FR-010**: UI MUST apply the settled copy: bookmark icon (inline SVG, outlined off / filled on), "Add to watchlist" / "Remove from watchlist", filter label "Watchlist", error "Couldn't update watchlist. Try again.", empty state "Your watchlist is empty."
- **FR-011**: Frontend implementation and tests MUST stay in `web/src`: toggle on `MovieRow` in `web/src/library/LibraryView.tsx`, delivered hero, Watchlist quick-filter toggle in the existing `App.tsx` header beside Import folder, delivered URL/browse integration, and shared hook. `App.tsx` is a composition surface only, not a rewrite or router introduction; MUST NOT compose `MovieGrid`/`MovieCard` or `FilterPanel`/`LibraryFilters`.
- **FR-012**: No change to the MVC app or endpoint routes/statuses. Add the required `isInWatchlist: boolean` field to `MovieSummaryResponse` and `MovieDetailsResponse`, flowing C# DTO → OpenAPI → regenerated `web/src/api/types.ts`; update the OpenAPI verified snapshot and `OpenApiContractTests` accordingly. No generated contract artifact is hand-edited; no other backend or contract change is in scope.

### Key Entities

- **Watchlist membership**: Per-movie boolean (`isInWatchlist`); flipped optimistically, confirmed or rolled back per FR-004.
- **Browse filter state**: URL key `watchlist` (`true` only) mapped to API key `inWatchlist`; combined with other filters by AND with page reset to 1.

## Success Criteria

- **SC-001**: Both ticket acceptance criteria hold in the React SPA: instant bookmark feedback and correct watchlist-only filtering of the browsed list.
- **SC-002**: Required web tests (Vitest, React Testing Library, MSW via `web-implement` TDD) pass, covering FR-002 through FR-009 including 204/409 retention, rollback paths, in-flight ignore, URL/API mapping, page reset, and accessible states.
- **SC-003**: Applicable web gates pass via `scripts/run-web-gates.ps1`; the OpenAPI baseline and verified snapshot match the regenerated contract, and applicable C# gates (Roslyn, complexity, InspectCode, Stryker) pass for the touched DTO projection files.

## Assumptions

Settled taste assumptions in `specs/DEV-324/ASSUMPTIONS.md` (icon, names, labels, error and empty-state copy, page reset) apply verbatim.

## Dependencies and Phase B Gate

- Prerequisite hosts (generated types/client, `MovieRow` in `web/src/library/LibraryView.tsx`, DEV-323 `MovieDetailsView` hero region, and URL-synced browse state) are delivered by sibling Epic 6 tickets, not DEV-324. The quick-filter toggle is integrated into the existing `App.tsx` header beside Import folder (F3); DEV-324 does not compose `FilterPanel`/`LibraryFilters`.
- Phase B starts only after all prerequisite implementation/delivery merges exist, explicitly including the DEV-323 delivery merge; a merged spec PR alone does not satisfy this. Spec-PR Gate 1 is distinct from delivery evidence.
- At Phase B pickup, bounded drift recon records actual paths (including the real `MovieDetailsView` path and its title/backdrop block) and amends tasks for drift.
- This spec does not freeze the plan and does not authorize Phase B.
