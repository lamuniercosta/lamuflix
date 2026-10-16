# Feature Specification: LamuFlix App Layout

**Feature Branch**: `feature/DEV-432-design-and-apply-the-lamuflix-app-layout-shell-filter-sideba`
**Created**: 2026-10-15
**Status**: Draft — awaiting Human Gate 1 approval
**Input**: DEV-432 alignment brief and confirmed decisions in `brief.md` and `CONCLUSIONS.md`

## User Scenarios & Testing

### User Story 1 - Browse the shared library shell (Priority: P1)

As a movie-library user, I want the library and watchlist to share a clear, responsive LamuFlix shell so I can browse my collection and reach its main actions from one place.

**Why this priority**: This establishes the main product surface and shared navigation for all later browsing and detail journeys.

**Independent Test**: Open the library and watchlist directly at 375px, 768px, and 1440px; verify the shared header, navigation, import action, and usable content without horizontal scrolling.

**Acceptance Scenarios**:

1. **Given** the library is open, **When** the user chooses Library or Watchlist, **Then** the corresponding view opens while the shared wordmark and navigation remain available.
2. **Given** the browser is at `/` or `/movies/{id}`, **When** the user opens a movie directly or reloads the route, **Then** the correct page renders without requiring a previous in-app visit.
3. **Given** the user changes between library and watchlist, **When** the view opens, **Then** the existing URL-backed watchlist and filter state is reflected in the results.
4. **Given** the Import folder action is available, **When** the user imports a folder, **Then** the existing import flow and its pending movie behavior remain usable from the shared shell.

---

### User Story 2 - Find titles with filters and search (Priority: P1)

As a movie-library user, I want filters, search, result count, sorting, and removable filter chips to work together so I can narrow the collection and understand the active query.

**Why this priority**: Browsing and filtering are the library's central job; the layout must make them prominent on desktop and reachable on mobile.

**Independent Test**: Apply multiple filters, sort and search; remove one active chip and verify only that criterion clears, the remaining query stays in the URL, and the result page resets as needed.

**Acceptance Scenarios**:

1. **Given** the library is open at 768px or wider, **When** it first loads, **Then** the filter sidebar is expanded and can be collapsed without clearing filters.
2. **Given** the library is open below 768px, **When** it first loads, **Then** the filter drawer is closed; opening it displays current filters and selections update results live.
3. **Given** the mobile filter drawer is open, **When** the user activates Done, **Then** the drawer closes and all current selections remain applied.
4. **Given** a filter drawer is open, **When** focus moves with Tab or Shift+Tab, **Then** focus stays within the drawer; when it closes, focus returns to the control that opened it and body scrolling is restored.
5. **Given** filters, search, or sort are changed, **When** results update, **Then** the query URL remains the single shareable source for filter state, the current page resets when result membership/order changes, and browser Back/Forward restores the corresponding query and results.
6. **Given** one or more filters are active, **When** the result summary is shown, **Then** it includes result count, current sorting, and one removable chip per active filter; removing a range endpoint preserves the other endpoint and removing the final active value leaves unrelated filters intact.
7. **Given** the library search field is used, **When** the user types, **Then** search remains debounced and does not discard other filters.
8. **Given** a movie details page has an originating filtered library URL, **When** the user types into the shared search field, **Then** the details page remains open; on Enter, the library opens with prior filters retained and the submitted text replacing the prior text filter.

---

### User Story 3 - Scan movie posters and library states (Priority: P1)

As a movie-library user, I want consistent poster tiles with useful metadata and clear enrichment or loading states so I can scan the collection and act on a title.

**Why this priority**: Posters and their metadata are the primary way users recognize and choose a movie.

**Independent Test**: Render a representative library containing complete metadata, missing posters, missing metadata, Pending, NotFound, and Failed entries; verify card content, actions, and loading/empty/error states.

**Acceptance Scenarios**:

1. **Given** a movie has a poster, title, year, rating, and runtime, **When** its tile renders, **Then** the poster preserves a 2:3 ratio and the supplied metadata and bookmark action are visible.
2. **Given** a poster or metadata is missing, **When** its tile renders, **Then** a readable placeholder appears and absent values do not produce broken images or misleading data.
3. **Given** enrichment is Pending, NotFound, or Failed, **When** the movie tile renders, **Then** a concise text status identifies the state without relying on color alone; existing retry behavior remains available where applicable.
4. **Given** results are loading, empty, filtered to no matches, or unavailable after an error, **When** the grid renders, **Then** the user sees a designed state with an appropriate next action and any existing retry behavior.
5. **Given** the user toggles a bookmark or retries enrichment, **When** the action succeeds or fails, **Then** existing optimistic update, rollback, and error feedback behavior remains intact.

---

### User Story 4 - Open a movie and return to the same browse query (Priority: P2)

As a movie-library user, I want a movie detail page that presents its available information and actions, and a back link that restores my filtered library, so I can inspect a title without losing my place.

**Why this priority**: Details complete the browse journey after the shell and poster grid are usable.

**Independent Test**: Open a movie from a filtered library URL, use its detail actions, and return; verify the full prior URL and query state are restored.

**Acceptance Scenarios**:

1. **Given** a movie is selected from results, **When** its tile is activated, **Then** client-side navigation opens `/movies/{id}` without a full-page reload.
2. **Given** movie metadata is available, **When** details render, **Then** the poster is beside the title and available year, format, runtime, ratings, director, cast, and plot are presented clearly.
3. **Given** poster or metadata is unavailable, **When** details render, **Then** a designed placeholder or no-metadata state is shown without hiding the movie title or watchlist action.
4. **Given** the user activates the details back link after opening a movie from the library, **When** the library opens, **Then** the exact originating same-origin library path and query and its results are restored; **Given** details is opened directly without valid library navigation state, **When** the user activates the back link, **Then** the default library route opens.
5. **Given** the user selects Play, **When** the existing endpoint responds, **Then** its success, not-found, generic error, or accessible 403 Playback unavailable feedback is shown; the interface does not claim to know capability before that response.
6. **Given** a detail route returns 404 or a server error, **When** the page renders, **Then** a distinct not-found or recoverable error state is shown.

---

### User Story 5 - Use the app with keyboard and across viewport sizes (Priority: P2)

As a keyboard or small-screen user, I want clear focus, accessible contrast, and layouts that adapt to the screen so I can complete the same browse and detail actions.

**Why this priority**: Responsive and keyboard access are required for every feature surface, including the mobile filter dialog.

**Independent Test**: Review and exercise the library, watchlist, drawer, import dialog, cards, and movie details at 375px, 768px, and 1440px using keyboard and contrast checks.

**Acceptance Scenarios**:

1. **Given** a viewport width of 375px, 768px, or 1440px, **When** each primary view is rendered, **Then** content remains usable and no horizontal page scrolling is required.
2. **Given** any interactive control receives keyboard focus, **When** it is focused, **Then** a visible focus indicator is shown.
3. **Given** the viewport changes across the 767px/768px breakpoint, **When** the library is rendered, **Then** the mobile drawer presentation applies below 768px and the collapsible sidebar applies at and above 768px without losing query state.
4. **Given** users view text, controls, or state indicators, **When** foreground/background pairs are checked, **Then** normal text meets WCAG AA contrast (4.5:1), large text meets 3:1, and non-text control boundaries/focus indicators meet 3:1.
5. **Given** browser reload, history navigation, or route transitions occur, **When** transient panels are reopened, **Then** panel visibility is not persisted across reloads and temporary overlays do not leave focus trapped or body scrolling disabled.

---

### User Story 6 - Share a representative library screenshot (Priority: P3)

As a project maintainer, I want a representative library screenshot so the README can show the intended product layout.

**Why this priority**: The screenshot communicates the finished layout but does not block the primary user journeys.

**Independent Test**: Capture the finished library at the agreed desktop viewport with representative posters, filters, chips, sorting, and result count; review that it contains no debug or loading artifacts.

**Acceptance Scenarios**:

1. **Given** the representative library is in its stable populated state, **When** its screenshot is captured, **Then** it shows the shared shell, filter sidebar, poster grid, result count, sorting, and active filter chips in one readable frame.

### Edge Cases

- A direct details URL is opened with no previous library visit or with malformed, non-positive, or non-integer movie IDs; unmatched paths fall back to the library, and a valid numeric details route still renders after reload.
- A details URL is opened from a filtered, sorted, paginated, or watchlist query; its history entry retains the exact same-origin library URL, Back/Forward restore the matching browser entry, and the details back link returns to that URL. Direct details entry without valid navigation state falls back to the default library.
- A malformed or untrusted return URL is supplied in history state; it must not navigate off-origin or to a non-library route.
- Browser history changes while a details route is mounted; route and page content must remain synchronized.
- A filter range has only its minimum or maximum set; removing one endpoint must retain the other endpoint.
- The drawer closes by Done, Escape, backdrop, route change, or viewport transition; focus and body scroll cleanup must remain correct.
- A route or filter transition happens during a pending browse request or enrichment retry.
- A movie has a broken poster URL, no metadata, nullable rating/runtime/year, or a NotFound/Failed enrichment state.
- The playback endpoint returns 403, 404, or an unexpected failure; 403 must retain the existing disabled-playback dialog behavior.
- Search is typed on details but not submitted; route and originating library URL remain unchanged.

## Requirements

### Functional Requirements

- **FR-001**: The Library, Watchlist, and Movie Details views MUST use a consistent shared dark-theme visual language with readable type and consistent control styling.
- **FR-002**: The shared shell MUST identify LamuFlix and provide Library, Watchlist, and Import folder actions on the main app views.
- **FR-003**: Selecting a movie MUST navigate to its details route without a full-page reload; direct route loads, reloads, Back, and Forward MUST render the route matching the browser URL.
- **FR-004**: Library filtering MUST place a collapsible sidebar at viewport widths of 768px and above, initially expanded on a fresh load, and a modal drawer below 768px, initially closed on a fresh load.
- **FR-005**: Filter selections in the mobile drawer MUST update results immediately; Done MUST close the drawer while retaining selections; drawer visibility MUST NOT be persisted across reloads.
- **FR-006**: The filter drawer MUST keep keyboard focus inside while open, return focus to its opening control when closed, and release any scroll lock on every close or cleanup path.
- **FR-007**: The library MUST show removable active-filter chips, result count, and current sorting; chip removal MUST clear only the represented value or range endpoint and reset pagination when result membership or ordering changes.
- **FR-008**: Library search MUST remain debounced and preserve other active filters; search submitted from details MUST preserve the originating browse filters, replace the prior text filter, and return to the library only on Enter.
- **FR-009**: Movie tiles MUST show a 2:3 poster or meaningful placeholder, title, available year, IMDb rating and runtime, a bookmark action, and concise Pending, NotFound, or Failed enrichment indicators; existing enrichment retry behavior MUST remain intact.
- **FR-010**: The library MUST provide intentional loading, empty, no-match, and error states, preserving existing recovery actions.
- **FR-011**: Movie details MUST show available poster, title, format, year, runtime, ratings, director, cast, and plot, together with watchlist and Play actions and distinct loading, missing, and error states. It MUST use only fields already available in the existing details response.
- **FR-012**: The movie details back link MUST restore the exact originating same-origin library path and query from valid navigation state, or open the default library when details was entered directly or its navigation state is invalid. Existing playback success, not-found, generic error, and 403-unavailable responses MUST remain accessible and behaviorally compatible.
- **FR-013**: Library, Watchlist, and Movie Details MUST remain usable at 375px, 768px, and 1440px without horizontal page scrolling; all interactive controls MUST expose visible keyboard focus and meet the stated WCAG AA text and non-text contrast ratios.
- **FR-014**: The implementation MUST include meaningful regression coverage for new routing, URL/filter synchronization, responsive drawer, focus and scroll cleanup, details search submission, and preserved import, bookmark, enrichment retry, and playback handling, using the existing web test framework.
- **FR-015**: The completed library layout MUST have a representative screenshot suitable for inclusion in the project README.

### Key Entities

- **Library query**: Search text, genres, cast, runtime and year bounds, enrichment statuses, sort and direction, page, page size, watchlist state, and other URL parameters already supported by browsing.
- **Movie summary**: A library item with identity, title, optional poster and metadata, watchlist state, and enrichment status.
- **Movie details**: A selected library movie with its file information, watchlist state, and optional metadata including ratings, runtime, year, director, cast, and plot.
- **Navigation state**: The current route and a validated same-origin library return URL, including its query, stored with the details history entry and used to restore browsing context. If that state is absent or invalid, the return route is the default library.

## Success Criteria

### Measurable Outcomes

- **SC-001**: At 375px, 768px, and 1440px, the Library, Watchlist, and Movie Details views render without horizontal page scrolling.
- **SC-002**: Every active filter represented in a library URL is reflected in controls and chips, and removing any one chip preserves all other filter values in both URL and results.
- **SC-003**: Opening a movie from a filtered library and using its back link restores the original path and query; browser Back and Forward render the matching library or details route.
- **SC-004**: Keyboard users can open, operate, and close the filter drawer while focus stays within it, returns to its trigger, and document scrolling is restored.
- **SC-005**: Required text and non-text contrast pairings meet WCAG AA ratios: 4.5:1 for normal text, 3:1 for large text and meaningful controls/focus boundaries.
- **SC-006**: The existing web gate completes lint, TypeScript checking, Vitest, and production build successfully after implementation; regression tests cover each newly introduced interaction in FR-014.
- **SC-007**: A reviewed screenshot shows the stable library shell, sidebar, chips, sorting, result count, and poster grid in a single representative desktop view.

## Assumptions

- The DEV-433 composed library and existing web components, URL filter model, API client, React Query state, watchlist, import, and enrichment behavior are the starting point and remain authoritative.
- The existing query string is the shareable source of library filters; filter-panel visibility is transient UI state and is not added to the URL or local storage.
- Client-side routing will use existing browser capabilities and project dependencies; no router package, backend route, API contract, generated API file, or endpoint change is in scope.
- The current movie details response exposes neither genres nor playback capability. Details shows only available response fields; genres and proactive Play capability visibility are deferred to a backend follow-up. Play remains available with existing endpoint behavior, including the accessible 403 disabled-playback dialog. This planning task does not create that follow-up or change the API scope.
- The system font stacks and palette follow the approved DEV-321 design direction; every actual text/control pairing must still be measured for contrast.
- A representative screenshot is an implementation deliverable; no README copy or broader documentation rewrite is included.

## Out of Scope

- Recommendations, thumbnail generation or optimization beyond DEV-429, internationalization, backend or API changes, new routes beyond the library and `/movies/{id}`, and new test frameworks.
