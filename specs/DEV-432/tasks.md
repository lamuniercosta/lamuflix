---
description: "Ordered implementation tasks for DEV-432"
---

# Tasks: LamuFlix App Layout

**Input**: Approved `spec.md`, `plan.md`, `research.md`, `data-model.md`, `design.md`, and `contracts/frontend-navigation.md` in `specs/DEV-432/`.

**Prerequisites**: Human Gate 1 approval; frontend-only scope confirmed. Genres and proactive Play capability are deferred to backend follow-up; no API/backend/generated changes are included.

**Tests**: Regression coverage is required by FR-014. Use existing Vitest, React Testing Library, and MSW. Keep all implementation and test tasks unchecked until performed.

**Organization**: Tasks are sequential because shell, URL ownership, routes, and the views share components and CSS. No parallel markers are used where work can touch common files.

## Format: `[ID] [Story] Description`

## Phase 1: Shared foundation

**Purpose**: Establish tokens and reactive navigation needed by each app view.

- [X] T001 Define shared Tailwind theme variables and semantic dark-surface, text, boundary, focus, spacing, radius, and typography rules in `web/src/index.css`; pair amber fills with dark foreground and retain `#2B313B` only for decorative dividers (FR-001, FR-013; SC-001, SC-005).
- [X] T002 Add route regression cases in `web/src/App.test.tsx` for direct valid detail route, reload, malformed/non-positive/non-integer/unmatched path fallback, client-side movie navigation without reload, mounted Back/Forward updates, same-origin validated return state, and canceling pending library debounce on route/history changes; include direct-detail fallback to `/` (FR-003, FR-008, FR-012, FR-014; SC-003).
- [X] T003 Implement the concrete route snapshot/transition owner in `web/src/features/app-navigation/useAppNavigation.ts` and connect `web/src/App.tsx`; publish app-initiated history changes, subscribe to `popstate`, accept only validated same-origin library return URLs in history state, fall back to `/` for absent/invalid state, and keep a direct detail route reloadable (FR-003, FR-012; SC-003).
- [X] T004 Integrate the shared wordmark, Library and Watchlist navigation, shell search, and Import folder action in `web/src/App.tsx`; derive one `FilterQuery` from the navigation snapshot with `filterQuery.ts`, pass controlled query/update handlers to the shell/search, `LibraryView`, `FilterPanel`, and results toolbar, and route integrated query/history changes through `useAppNavigation`. Remove independent integrated history writes from `FilterPanel` and `useWatchlistFilter`, keeping adapters for isolated component behavior; preserve import, pending-row, and watchlist behavior (FR-002, FR-003, FR-007, FR-008; SC-001, SC-002).

**Checkpoint**: The shared shell and route snapshot exist; no API, generated type, backend, or new dependency has been added.

## Phase 2: User Story 1 - Browse the shared library shell (Priority: P1)

**Goal**: Use the same recognizable app shell while moving between Library and Watchlist and opening Import.

**Independent Test**: Open `/` and `/?watchlist=true`; use the shell actions and confirm the view and query reflect the destination.

- [X] T005 [US1] Extend `web/src/App.test.tsx` and `web/src/library/ImportDialog.test.tsx` for shell navigation, URL-backed Watchlist state, import dialog open/close focus return, and imported pending-row behavior (FR-002, FR-014).
- [X] T006 [US1] Apply the shared shell structure to Library, Watchlist, and Movie Details in `web/src/App.tsx` and the existing view styles; verify Import and Watchlist actions remain reachable at narrow widths (FR-001, FR-002; SC-001).

## Phase 3: User Story 2 - Find titles with filters and search (Priority: P1)

**Goal**: Keep one query source behind the responsive controls, chips, sort, count, and results.

**Independent Test**: Apply search and multiple filters, remove individual chips and one range endpoint, and verify URL, remaining controls, page reset, and results.

- [X] T007 [US2] Extend `web/src/App.test.tsx`, `web/src/features/library/FilterPanel.test.tsx`, and `web/src/library/LibraryView.composed.test.tsx` for shell-search debounce and preservation of other filters, mobile live updates, Done retention, desktop expanded/collapse defaults, no panel-visibility persistence, exact 767px/768px mode selection, removable multi-value chips, one-sided range removal, sort/count updates, unknown-query preservation, Back/Forward query restoration, and page reset (FR-004, FR-005, FR-007, FR-008, FR-014; SC-002).
- [X] T008 [US2] Adapt `web/src/features/library/FilterPanel.tsx` and `web/src/library/LibraryView.tsx` to a collapsible desktop sidebar at `>=768px` and closed-by-default modal mobile drawer below 768px; implement live updates, Done close-with-retention, focus trap/return, background inertness, breakpoint cleanup, and body-scroll lock cleanup on every close/unmount path (FR-004, FR-005, FR-006; SC-004).
- [X] T009 [US2] Add active filter chips, per-value/range-endpoint removal, result count, and current sort summary to `web/src/library/LibraryView.tsx` (and `web/src/library/MovieGrid.tsx` if needed); consume the controlled query and update handler from `App.tsx`, preserve `filterQuery.ts` as the sole parse/serialize utility, and reset page when membership or ordering changes (FR-007; SC-002).
- [X] T010 [US2] Integrate the single library-title search in `web/src/App.tsx` with the existing `web/src/features/library/useDebouncedSearch.ts`; report debounced changes through the App-owned handler, preserve other active filters, and cancel pending debounce on route/history transitions. Keep `FilterPanel.tsx`'s cast-option search as an isolated facet helper and preserve its standalone adapter; verify details search stays local until Enter (FR-008; SC-003).

## Phase 4: User Story 3 - Scan movie posters and library states (Priority: P1)

**Goal**: Make movie cards, enrichment state, and result states readable without changing their behavior.

**Independent Test**: Render complete, partial, Pending, NotFound, and Failed movie examples and exercise bookmark, retry, loading, empty, no-match, and error paths.

- [X] T011 [US3] Extend `web/src/library/MovieCard.test.tsx`, `web/src/library/MovieGrid.test.tsx`, `web/src/library/LibraryView.retry.test.tsx`, and `web/src/library/watchlist.integration.test.tsx` for 2:3 poster/placeholder, missing year/rating/runtime/metadata, accessible status text, existing bookmark optimistic update/rollback, enrichment retry, loading/empty/no-match/error recovery, and query state (FR-009, FR-010, FR-014).
- [X] T012 [US3] Refine `web/src/library/MovieCard.tsx` and `web/src/library/MovieGrid.tsx` with shared poster sizing, title/year/rating/runtime presentation, bookmark and enrichment badges, and designed skeleton/empty/no-match/error states while preserving placeholders and recovery controls (FR-009, FR-010; SC-001).
- [X] T013 [US3] Keep the import pending row and enrichment retry feedback styled consistently in `web/src/library/LibraryView.tsx` and `web/src/library/ImportDialog.tsx`; verify failed retry and watchlist removal continue to produce their existing feedback (FR-009, FR-010, FR-014).

## Phase 5: User Story 4 - Open a movie and return to the same browse query (Priority: P2)

**Goal**: Show the available detail response in the shared shell and restore browsing context safely.

**Independent Test**: Open a detail page from a filtered/sorted/paginated/watchlist URL, test Back/Forward, return link, details search without Enter and with Enter, and existing Play responses.

- [X] T014 [US4] Extend `web/src/features/movie-details/MovieDetailsView.test.tsx` and `web/src/App.test.tsx` for exact return URL with recognized and unknown query keys, direct details entry fallback, invalid/external/non-library history state rejection, Enter-only details search preserving prior filters and replacing text, details typing without navigation, browser history synchronization, missing metadata, and Play 204/403/404/500 handling (FR-003, FR-008, FR-011, FR-012, FR-014; SC-003).
- [X] T015 [US4] Integrate `web/src/features/movie-details/MovieDetailsView.tsx` and `web/src/features/movie-details/MovieDetailsView.css` into the shared shell; show only available response fields (no invented genres), preserve loading/not-found/error states, poster placeholder, title, watchlist action, Play feedback and accessible 403 dialog, and style poster/metadata responsively (FR-001, FR-011, FR-012; SC-001).
- [X] T016 [US4] Implement the details back link and shared top-bar search behavior in `web/src/App.tsx` and the navigation owner: restore validated same-origin library state or `/`, keep details search local until Enter, then replace only `text` on the retained query or default library query and reset page (FR-003, FR-008, FR-012; SC-003).

## Phase 6: User Story 5 - Use the app with keyboard and across viewport sizes (Priority: P2)

**Goal**: Verify layout, keyboard, focus, contrast, and cleanup across all agreed screen sizes.

**Independent Test**: Review Library, Watchlist, Import, drawer, and Details at 375px, 768px, and 1440px; exercise the drawer with keyboard and measure actual contrast pairings.

- [X] T017 [US5] Extend `web/src/features/library/FilterPanel.test.tsx`, `web/src/App.test.tsx`, and `web/src/features/movie-details/MovieDetailsView.test.tsx` for visible focus, drawer focus containment/return, Escape/backdrop/route-change cleanup, body scroll restoration, responsive state cleanup, and no persisted panel visibility (FR-006, FR-013, FR-014; SC-004).
- [X] T018 [US5] Finish shared responsive and keyboard styling in `web/src/index.css`, `web/src/features/library/FilterPanel.tsx`, `web/src/library/MovieGrid.tsx`, and `web/src/features/movie-details/MovieDetailsView.css`; verify no horizontal overflow at 375/768/1440px, correct 767/768 transition, reduced-motion handling, text contrast 4.5:1/3:1 and meaningful control/focus boundaries 3:1 (FR-001, FR-006, FR-013; SC-001, SC-005).
- [X] T019 [US5] Record browser acceptance evidence in `artifacts/DEV-432/acceptance.md`, including direct routes/reloads, Back/Forward, query/chip/range semantics, 767/768 boundary, focus trap/return, body-scroll unlock, details search Enter behavior, Import, bookmarks, retries, missing media, and Play 403; capture 375px, 768px, and 1440px screenshots under `artifacts/DEV-432/screenshots/` (FR-003, FR-006, FR-008, FR-013, FR-014; SC-001, SC-003, SC-004, SC-005).

## Phase 7: User Story 6 - Share a representative library screenshot (Priority: P3)

**Goal**: Deliver a clean populated library image for the DEV-23 README handoff.

**Independent Test**: Review one stable desktop capture for shell, sidebar, active chips, result count, sort, and posters with no debug/loading artifact.

- [X] T020 [US6] Capture and review the stable populated library screenshot under `artifacts/DEV-432/screenshots/lamuflix-library.png`, documenting the chosen viewport and fixture in `artifacts/DEV-432/acceptance.md`; do not edit README copy in this feature (FR-015; SC-007).

## Phase 8: Verification

**Purpose**: Run the existing frontend gate and preserve a truthful receipt.

- [X] T021 Run `pwsh -NoProfile -File ./scripts/run-web-gates.ps1 -All` from the repository root and save concise command output and actual exit code under `artifacts/DEV-432/web-gates/`; classify only exit 0 as PASS and exit 2 as SKIPPED, never PASS (FR-014; SC-006).
- [X] T022 Reconcile `artifacts/DEV-432/acceptance.md` with the gate output, screenshots, and observed behavior; record any unmet acceptance criterion instead of marking it complete (FR-013, FR-014, FR-015; SC-001–SC-007).

## Coverage Map

| Spec key | Tasks |
|---|---|
| FR-001 | T001, T006, T015, T018 |
| FR-002 | T004, T005, T006 |
| FR-003 | T002, T003, T014, T016, T019 |
| FR-004 | T007, T008 |
| FR-005 | T007, T008 |
| FR-006 | T008, T017, T018, T019 |
| FR-007 | T007, T009 |
| FR-008 | T007, T010, T014, T016, T019 |
| FR-009 | T011, T012, T013 |
| FR-010 | T011, T012, T013 |
| FR-011 | T014, T015 |
| FR-012 | T002, T003, T014, T016 |
| FR-013 | T001, T017, T018, T019 |
| FR-014 | T005, T007, T011, T014, T017, T019, T021, T022 |
| FR-015 | T020, T022 |
| SC-001 | T001, T004, T006, T012, T015, T018, T019 |
| SC-002 | T007, T009 |
| SC-003 | T002, T003, T014, T016, T019 |
| SC-004 | T008, T017, T019 |
| SC-005 | T001, T018, T019 |
| SC-006 | T021, T022 |
| SC-007 | T020, T022 |

## Phase 9: Review remediation (round 1)

**Source**: post-implement audit findings, most severe first. Round base: `77976a6`.

- [X] R001 [High/standards] Restore generated `web/public/mockServiceWorker.js` to the committed 3.0.2 worker; no hand edits to generated files.
- [X] R002 [Medium/risk] Make the mobile drawer background truly inert while open (fix `inert` no-op) with a regression test.
- [X] R003 [Medium/risk] Cancel pending shell-search debounce synchronously on `popstate` with a regression test.
- [X] R004 [Medium/spec] Add the missing `runtimeIncludeUnknown` chip (FR-007) with a regression test.
- [X] R005 [Medium/spec] Add shell-seam regression tests for bookmark, enrichment retry, and playback (FR-014).
- [X] R006 [Medium/standards] Derive the toolbar sort options from `FilterPanel` `SORT_OPTIONS` (remove duplication).
- [X] R007 [Medium/spec] Record the `artifacts/` gitignore constraint for the FR-015 README handoff (deferred: force-add at commit; no `.gitignore` policy change in this feature).
- [X] R008 [Low/risk] Replace (not push) same-URL navigations to avoid dead history entries, with a regression test.
- [X] R009 [Low/risk] Suffix repeated-value chip keys with an index, with a regression test.
- [X] R010 [Low/risk] Suppress range chips for inactive extent defaults, with a regression test.
- [X] R011 [Low/risk] Fall back the toolbar sort control and summary for unknown sort values (preserving the query), with a regression test.
- [X] R012 [Low/risk] Delete dead `useWatchlistFilter.ts` (zero references).
- [X] R013 [Low/spec] Resync on state-only `popstate` (same URL, different history state), with a regression test.
- [X] R014 [Low/standards] Extract the shared shell search field markup (remove duplication).

## Dependencies & Execution Order

1. Complete T001–T004 before the story implementation because theme, shell, route synchronization, and App-owned query state are shared foundations.
2. Continue through story tasks in listed order; regression coverage is required, and task order reflects dependencies without imposing test-first development on every implementation task.
3. Complete T001–T018 before T019 browser acceptance; then complete T020 screenshot and T021 web gate, and reconcile their evidence in T022 last.
4. No task changes generated API files, backend, dependencies, thresholds, or unrelated specs. Genres and proactive playback capability remain deferred; do not create a backend follow-up issue as part of this task.
