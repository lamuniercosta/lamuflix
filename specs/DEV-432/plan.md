# Implementation Plan: LamuFlix App Layout

**Branch**: `feature/DEV-432-design-and-apply-the-lamuflix-app-layout-shell-filter-sideba` | **Date**: 2026-10-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification in `specs/DEV-432/spec.md`; approved scope and interactions in `brief.md` and `CONCLUSIONS.md`.

## Summary

Implement the shared LamuFlix shell, reactive client-side library/details navigation, responsive filters and grid treatment, and details presentation in the existing React web app. Continue using the current filter query model, generated API contract, import/watchlist/enrichment behaviors, and installed web test/gate setup. No backend/API or dependency changes are planned. The current details response has no playback-capability signal, so retain the existing Play request and 403 dialog behavior; hiding Play before a 403 would need a future authorized capability contract.

## Technical Context

**Language/Version**: TypeScript 6.0, React 19.3, CSS/Tailwind CSS 4.3.

**Primary Dependencies**: Existing React, React DOM, TanStack Query, Vite, Tailwind CSS/Vite plugin, Vitest, Testing Library, MSW, and generated `openapi-fetch` client. No new dependencies.

**Storage**: No new persistent state. Library filter values remain in the existing URL query; panel visibility and details search draft remain transient.

**Testing**: Existing Vitest and React Testing Library with MSW API handlers. Run `scripts/run-web-gates.ps1 -All` for lint, TypeScript, tests, and build. Component tests remain colocated with web features; no second test framework.

**Target Platform**: Existing desktop/mobile web application.

**Project Type**: React SPA in `web/` with ASP.NET Core API held outside scope.

**Performance Goals**: No new numeric performance budget. Preserve existing debounced library search and avoid full-page reloads for in-app route changes.

**Constraints**: Freeze API and generated schema; do not edit `web/src/api/**`, backend, build output, dependencies, gate thresholds, or generated files. No browser router dependency. Preserve current browse query keys, import, watchlist, enrichment retry and playback behavior. New CSS variables must use the approved dark slate/warm white/amber direction and measured accessible pairings.

**Scale/Scope**: Six agreed areas across Library, Watchlist, Import shell action, and Movie Details; viewports 375px, 768px, 1440px; breakpoint strictly below 768px for drawer and 768px or wider for sidebar.

## Constitution Check

The initial check passes. This change is frontend-only and adds no domain model, backend feature, external boundary, package, or new persistence. It preserves the typed backend query/API contract, generated types, existing React Query usage, and current tests. No ADR or CONTEXT.md term change is required by the confirmed alignment; the existing vocabulary remains canonical. The current web gate is the required frontend validation lane. C#-only gates may later report `SKIPPED (scope-empty)` and must not be recorded as PASS.

The post-design check also passes: the small navigation owner is a frontend view-state seam, not a new backend port or domain concept; URL query remains the browse model; all async movie data stays through the existing client/query patterns; no new library or test framework is proposed. No constitutional exception needs justification.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-432/
├── spec.md
├── plan.md
├── research.md
├── design.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── frontend-navigation.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
web/src/
├── App.tsx                                  # shared shell and route composition
├── index.css                                # Tailwind theme tokens and shared layout rules
├── features/
│   ├── app-navigation/
│   │   └── useAppNavigation.ts              # reactive pathname/search snapshot and transitions
│   ├── library/
│   │   ├── FilterPanel.tsx                  # existing filters in sidebar/drawer
│   │   ├── filterQuery.ts                   # existing query parse/serialize behavior
│   │   └── useDebouncedSearch.ts             # existing library search behavior
│   └── movie-details/
│       ├── MovieDetailsView.tsx              # detail page content and existing actions
│       └── MovieDetailsView.css              # detail layout using shared tokens
└── library/
    ├── LibraryView.tsx                      # query owner integration/results toolbar
    ├── MovieCard.tsx                        # poster card and movie detail link
    ├── MovieGrid.tsx                        # grid and loading/empty/error presentation
    ├── ImportDialog.tsx                     # existing import behavior in shared shell
    └── useWatchlistFilter.ts                # existing URL-backed watchlist behavior

web/src/**/*.test.tsx                         # targeted interaction regression coverage
```

**Structure Decision**: Keep the React SPA organization already present. Add at most one focused app-navigation module because route changes need to notify mounted views and browser history needs to render the URL's current route. Continue to house library and details behavior in their existing feature/library folders; do not restructure APIs or generate route contracts.

## Design and Data Ownership

The App-mounted `useAppNavigation` owner in `web/src/features/app-navigation/useAppNavigation.ts` provides the reactive browser `pathname` and `search` snapshot, publishes app-initiated navigation changes, and listens for `popstate`. `App.tsx` derives one canonical `FilterQuery` from that snapshot with `web/src/features/library/filterQuery.ts` and passes the controlled query plus update handlers to the shared shell/search, `LibraryView`, `FilterPanel`, and results toolbar. Controls, chips, sort, result count context, API parameters, and URL serialization all derive from this single active query owner.

Integrated query updates and route transitions MUST go through the shared navigation owner so URL and snapshot update together. The integrated `FilterPanel` and `useWatchlistFilter` paths do not write browser history independently. Keep component-level adapters for isolated component tests/legacy use, but do not activate a second query owner in the App-mounted flow. `filterQuery.ts` remains the one query parser/serializer and preserves unknown parameters. Query updates that change membership/order reset page to the established first page; removing one range bound keeps the other. Replace operations for debounced keystroke synchronization do not create extra history entries.

Details search uses a local text draft. When Enter submits it, derive the library base query from the validated same-origin library return URL in the details navigation entry, never by parsing the `/movies/{id}` URL's query as browse state. If details was opened directly or has invalid/missing return state, use the default library query. Cancel any pending library-search debounce on route/history transitions before applying the next route snapshot, so a queued commit cannot overwrite a detail route.

Panel visibility is local transient state. At `min-width: 48rem` (768px) the sidebar is expanded by default and can collapse. Below it, the drawer starts closed and is modal. Breakpoint changes close the incompatible presentation, restore focus if possible, and release scroll lock. All event listeners and global body styles are cleaned up during close and unmount.

Use Tailwind `@theme` names for shared color, spacing, radius, and type tokens in `web/src/index.css`; use semantic CSS selectors for structural app layouts and any existing focused stylesheet rather than building a parallel component framework. Use dark foreground on amber fill; the brighter boundary/focus token supports required control contrast while the existing quiet rule is reserved for decoration.

The app route parser treats only positive integer `/movies/{id}` paths as details; non-integer, non-positive, and otherwise unmatched paths render the library. Direct valid details paths render details after reload. A details history entry stores the exact originating same-origin library URL in `history.state`; validate that it is same-origin and a library route before using it. Missing, malformed, or untrusted return state falls back to `/`. A details search submission uses the validated return URL as its base, or the default library query if entered directly. It replaces only text and resets page. New interactive controls and navigation use semantic links/buttons and preserve browser link affordances. The existing details 403 flow remains the only evidence that playback is disabled; genres and proactive Play capability visibility are deferred to a backend follow-up and are not added in this feature.

## Phase Plan

### Phase 0: Research

- Resolve Tailwind 4 token and responsive breakpoint use from official docs; see [research.md](research.md).
- Confirm navigation/query ownership and overlay cleanup decisions against current frontend behavior.
- Record the capability limitation and explicit no-API-change boundary.

### Phase 1: Design

- Define and document shared visual tokens, layouts, and accessibility review points in [design.md](design.md).
- Document library query, route state, and view-only entities in [data-model.md](data-model.md).
- Freeze the front-end navigation/query/overlay invariants in [contracts/frontend-navigation.md](contracts/frontend-navigation.md); backend API contracts remain unchanged.
- Provide implementer commands and expected evidence paths in [quickstart.md](quickstart.md).

### Phase 2: Implementation (awaits Human Gate 1)

1. Establish shared theme and semantic app shell; retain import and watchlist behavior.
2. Add a single reactive navigation owner and connect details links, direct routes, browser history, originating query restoration, and details search submission.
3. Adapt the filter and results toolbar to responsive sidebar/drawer, active chips, count, and sort while keeping URL query ownership and existing search debounce.
4. Apply shared poster/grid/loading/empty/error state styling while preserving bookmarks, import placeholders, enrichment badges and retries.
5. Integrate details into the shell and complete responsive metadata/actions and recoverable states, retaining Play response behavior.
6. Add/adjust meaningful regression tests for routing, query synchronization, responsive panel focus/cleanup, search submission, and preserved workflows; perform viewport/contrast/screenshot review.

### Phase 3: Verification (after implementation)

- Run `pwsh -NoProfile -File ./scripts/run-web-gates.ps1 -All` from the repo root. Required result: exit 0 with lint, TypeScript, Vitest, and build all passing.
- Capture concise gate output and Vitest report under `artifacts/DEV-432/web-gates/`; capture viewport screenshots under `artifacts/DEV-432/screenshots/` (375, 768, 1440 and populated README view).
- Record direct-route/reload, Back/Forward, filter removal/range, 767/768 transition, mobile focus trap/return/body unlock, details search Enter behavior, contrast measurements, import/bookmark/retry, missing media, and playback 403 evidence in `artifacts/DEV-432/acceptance.md`.
- Run applicable C# analyzers only if C# scope changes; with no changed C#, classify exit 2 as `SKIPPED (scope-empty)`, never PASS. No backend or OpenAPI gate is added by scope.

## Complexity Tracking

No constitution violations or project/dependency increases are planned.
