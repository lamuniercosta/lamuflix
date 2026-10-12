# Feature Specification: DEV-80 Folder Import Dialog and Smart Polling

**Feature Branch**: `feature/080-spec`

**Created**: 2026-10-12

**Status**: gate1: provisional

**Input**: Frozen `specs/DEV-80/brief.md` (Q1-Q7 grill rulings brief.md:73-140, R1-R4 decision record brief.md:141-175 / CONCLUSIONS.md, taste A1-A6), `ASSUMPTIONS.md` A1-A6, `recon-DEV-80` sections 1-9 plus `recon-DEV-80-3`:22-33, `specs/PRODUCT.md`

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse the minimum library list (Priority: P1)

A user opens the app and sees the first page of the movie library, one row per movie with its title, its enrichment status badge, and a retry action where eligible.

**Why this priority**: The list is the only mount point for every other story; without it there is no row to prepend, no Pending in view to poll on, and no NotFound/Failed row to retry.

**Independent Test**: Render the library screen against a browse response and verify one row per item showing title plus status badge; an empty browse response shows the empty-library copy.

**Acceptance Scenarios**:

1. **Given** a browse response with movies, **When** the library screen renders, **Then** each row shows the title, the status badge text for its `enrichmentStatus`, and a `Retry` action only when the status is `NotFound` or `Failed`.
2. **Given** a browse response with zero movies, **When** the library screen renders, **Then** it shows `No movies yet. Import a folder to get started.`
3. **Given** a browse response in server order, **When** a poll reconciles rows, **Then** the client keeps server browse order and does not re-sort.

---

### User Story 2 - Import a movie folder (Priority: P1)

A user opens the import dialog, submits a folder path, and on acceptance sees the new movie as a Pending row without waiting for enrichment.

**Why this priority**: Import is the entry point that creates Pending rows; polling and retry have nothing to act on without it.

**Independent Test**: Submit a valid folder path, receive empty 202, and verify the dialog closes and a Pending row with the Location id is prepended to the cached browse data.

**Acceptance Scenarios**:

1. **Given** the library screen, **When** the user opens import, **Then** the dialog shows title `Import a movie folder`, input label `Folder path`, and `Import` / `Cancel` actions, opened from an `Import folder` button.
2. **Given** a valid folder path, **When** POST `/api/movies/import` returns empty 202, **Then** the dialog closes and the cached browse query is prepended with `{ id, title, enrichmentStatus: 'Pending' }` where `id` comes from the `Location` header and `title` is the last segment of the submitted folder path; the next poll reconciles it with the server row.
3. **Given** a blank or traversal folder path, **When** POST `/api/movies/import` returns 422, **Then** the dialog stays open and shows the `folderPath` problem message inline.
4. **Given** a malformed request or a network failure, **When** the import fails with 400 or no response, **Then** the dialog stays open and shows a generic error inline.

---

### User Story 3 - Poll while any Pending is in view (Priority: P1)

A user who has a Pending row sees it reconcile with the server automatically; a user with no Pending rows causes no polling traffic.

**Why this priority**: Polling is the ticket's reconciliation mechanism; gating it on visible Pending rows bounds its cost.

**Independent Test**: Seed cached browse data with and without a Pending item and verify the query refetches on a 2-second interval only while a Pending item is present.

**Acceptance Scenarios**:

1. **Given** current browse data containing any item with `enrichmentStatus === 'Pending'`, **When** the query evaluates its interval, **Then** it refetches every 2000 ms.
2. **Given** current browse data with no Pending item, **When** the query evaluates its interval, **Then** it does not poll.
3. **Given** an active poll, **When** the tab is in the background, **Then** background refetch stays off.

---

### User Story 4 - Retry enrichment for NotFound/Failed rows (Priority: P1)

A user retries enrichment for a row that needs metadata and sees it return to Pending, or sees an inline row error when the retry cannot proceed.

**Why this priority**: Retry unblocks rows the poller will never advance on its own.

**Independent Test**: Render NotFound, Failed, Pending, and Enriched rows and verify the retry action appears only on NotFound/Failed; trigger retry and verify the 202/404/409 outcomes.

**Acceptance Scenarios**:

1. **Given** rows with each enrichment status, **When** the list renders, **Then** `Retry` appears only on `NotFound` and `Failed` rows.
2. **Given** a NotFound/Failed row, **When** POST `/api/movies/{id}/enrichment` returns empty 202, **Then** that row becomes `Pending` in cache, which re-arms polling.
3. **Given** a NotFound/Failed row, **When** the retry returns 404 or 409, **Then** an inline row error shows and the browse query is invalidated.

---

### Edge Cases

- A row transitions out of Pending between polls: polling stops once no Pending item remains in the current browse data.
- Retry on a row that has become Pending since render: the server answers 409 and the row shows the inline error with invalidated browse data.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The library screen MUST render the browse first page with one row per item showing title, status badge, and the eligible retry action.
- **FR-002**: The library screen MUST expose an `Import folder` button that opens the import dialog.
- **FR-003**: The import dialog MUST submit `{ folderPath }` to POST `/api/movies/import` and on empty 202 close and prepend `{ id, title, enrichmentStatus: 'Pending' }` to the cached browse query, with `id` from the `Location` header and `title` from the last submitted folder-path segment.
- **FR-004**: The import dialog MUST keep open on 422 and show the `folderPath` problem message inline, and on 400 or network failure show a generic inline error.
- **FR-005**: The browse query MUST use a `refetchInterval` function returning `2000` while any current browse item has `enrichmentStatus === 'Pending'`, else `false`, with background refetch off.
- **FR-006**: The retry action MUST appear only for rows with `enrichmentStatus` `NotFound` or `Failed` and POST to `/api/movies/{id}/enrichment`.
- **FR-007**: A retry 202 MUST set that row to `Pending` in cache; a retry 404/409 MUST show an inline row error and invalidate the browse query.
- **FR-008**: `MovieSummaryResponse` MUST gain `enrichmentStatus` serialized by name as a string (`Pending` | `Enriched` | `NotFound` | `Failed`); no other DTO field, route, database schema/migration, or extra DTO scope is in scope. The approved OpenAPI metadata correction (the R2 schema transformer emitting that string shape and the R4 operation transformer stating the four enforced browse-query keys) is allowed as a document correction, not a schema change (brief R2-R4).
- **FR-009**: The persisted status MUST flow through the `MovieSummary` port record and the `EfMovieCatalog` projection into the browse response mapping.
- **FR-010**: The contract chain MUST stay complete: C# DTO to committed `web/src/api/openapi.json` to generated `web/src/api/types.ts`; all web HTTP calls go through an `openapi-fetch` client typed by the generated paths and test MSW handlers derive from those paths via `openapi-msw`.
- **FR-011**: TanStack Query MUST be the sole client state/cache layer with `QueryClientProvider` wrapping `App` in `web/src/main.tsx`; no other state library and no router.
- **FR-012**: `web/src/App.tsx` MUST become the library screen and `web/src/App.test.tsx` MUST cover it; the existing MSW harness contract in `web/src/test/` stays intact.

*Out of scope (frozen by brief.md Q7 from Q5): posters, filters, sorting UI, paging controls, details view, routing, other state libraries, `EnrichedAt` / `EnrichmentAttempts` / `LastFailureCategory`, `MovieDetailsResponse` changes, new routes, schema/migration changes, a new CI types-regeneration diff step.*

### Key Entities

- **MovieSummary (browse row)**: Identifier, title, and enrichment status as delivered by the browse response; the client renders, polls on, and retries against this row.
- **Import submission**: A folder path that either becomes a Pending row (202 + Location id) or an inline dialog error (422 field problem, 400/network generic).
- **Retry transition**: A NotFound/Failed row that either returns to Pending (202) or surfaces an inline row error with invalidation (404/409).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can submit a folder path and see a Pending row prepended without a manual refresh.
- **SC-002**: A Pending row reconciles with its server state within one poll cycle while polling issues no requests once no Pending row remains.
- **SC-003**: Every NotFound/Failed row offers retry and every Pending/Enriched row offers none.
- **SC-004**: The committed OpenAPI baseline and Verify snapshot encode `enrichmentStatus`, and the drift test passes against the regenerated document.
- **SC-005**: The web gates (lint, TypeScript check, Vitest, build) and the backend gates for the touched C# files pass at their pipeline stages.

## Assumptions

- Taste copy and optimistic-title behavior are taken from brief A1-A6: last folder-path segment as the optimistic title; labels `Import folder`, `Import a movie folder`, `Folder path`, `Import`, `Cancel`, `Retry`; badge text the enum name as received (`NotFound` shown as `Not found`); empty copy `No movies yet. Import a folder to get started.`; server browse order kept client-side; standing first-page request `sort=Title&direction=Ascending&page=1&pageSize=20` (A6 per brief R3, recon-DEV-80:123-125).
- Dependency names are taken from brief Q1-Q3; no version is pinned by this spec.
- Endpoint behavior (import 202 + Location, enrichment 202/404/409 with Pending-guard, 422/400 shapes) is taken from `recon-DEV-80` sections 2-3; this spec adds no new route. Browse-query wire enforcement (lowercase keys, case-sensitive values, `pageSize` 1..100, omission → 422) is taken from recon-DEV-80:122-126; the operation-transformer metadata surface is taken from recon-DEV-80-3:22-27 and the typed-client/MSW consumption from recon-DEV-80-3:29-33; design scope trim follows brief R1.
- `Palette design.md` is awaited before the UI plan challenge per the brief; this spec constrains only the minimum list, dialog, polling, and retry surface above.
