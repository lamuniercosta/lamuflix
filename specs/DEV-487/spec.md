# Feature Specification: Library bulk import (DEV-487)

**Feature Branch**: `feature/DEV-487-bulk-import-scan-the-library-root-and-import-all-new-folders`

**Created**: 2026-11-10

**Status**: Draft

**Input**: YouTrack DEV-487 — "Bulk import: scan the library root and import all new folders in one action", plus grill extension (2026-11-10): the pass also requeues Failed-enrichment movies, and the per-folder Import dialog is removed.

## What the User Will See *(mandatory)*

- **Where**: App header, where the `Import folder` button sits today.
- **What the user sees or does**: A `Bulk import` button. Clicking it opens a dialog that lists every library folder not yet imported (with checkboxes), plus a loose video file sitting directly in the library root as its own entry. The user ticks folders (or Select all) and clicks Import; folders already in the library never appear. A failed-movies section offers retry of movies stuck in Failed enrichment. Import runs with per-folder progress and ends with a summary (imported / skipped / retried / failed); the grid refreshes and new rows arrive Pending through the existing live updates. The old `Import folder` button and its dialog are gone; the empty-library copy no longer mentions importing a folder.
- **What it needs to show up**: Nothing beyond the default Compose stack. A library root containing at least one not-yet-imported folder (or a loose root video) for the candidate list to be non-empty.

### Smoke Test *(mandatory when something is visible)*

- **SMOKE-001**: **Given** a fresh stack whose library root holds folders not yet imported, **When** the user opens Bulk import, selects all, and imports, **Then** the dialog reports every folder imported, the grid shows the new rows, and a second pass lists zero candidates.
- **SMOKE-002**: **Given** a library root with a loose video file directly in the root, **When** the user opens Bulk import, **Then** the root video appears as an importable candidate and imports without a 404.
- **SMOKE-003**: **Given** a movie stuck in Failed enrichment, **When** the user retries failed from the bulk import flow, **Then** the movie returns to Pending and the grid updates.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - List and bulk-import new folders (Priority: P1)

The user opens Bulk import, sees every not-yet-imported folder (and a loose root video) listed, selects some or all, and imports them in one action. Folders already in the library are skipped silently, so repeating the pass is always safe.

**Why this priority**: This is the task verbatim — the only bulk path for a 342-folder library.

**Independent Test**: Can be fully tested by listing a root with a mix of imported and new folders and importing all; delivers the new rows without touching the old ones.

**Acceptance Scenarios**:

1. **Given** a root with new folders `A`, `B` and already-imported folder `C`, **When** the user lists candidates, **Then** the list shows `A` and `B` but not `C`.
2. **Given** candidates `A` and `B` selected, **When** the user imports, **Then** both movies are added Pending, the summary reports 2 imported / 0 skipped, and the grid refreshes.
3. **Given** a completed import, **When** the user lists and imports again, **Then** zero folders import, everything is reported skipped, and no duplicate or error occurs.
4. **Given** a loose video file directly in the library root, **When** the user lists candidates, **Then** it appears as a candidate and imports successfully (no 404).

---

### User Story 2 - Retry failed enrichments from the bulk import (Priority: P2)

The bulk import flow surfaces movies stuck in Failed enrichment and requeues them with the pass, so one action covers "new or failed movies".

**Why this priority**: Grill extension of the Done-when; previously required per-row Retry clicks.

**Independent Test**: Can be fully tested by marking a movie Failed, retrying from the bulk import, and observing it return to Pending.

**Acceptance Scenarios**:

1. **Given** a movie in Failed enrichment, **When** the user retries failed from the bulk import flow, **Then** the movie returns to Pending and enrichment is re-staged.
2. **Given** no Failed movies, **When** the user opens Bulk import, **Then** the failed section reports nothing to retry and the import proceeds normally.

---

### User Story 3 - Import dialog removed, API surface kept (Priority: P3)

The per-folder Import dialog (button, dialog, focus flow) is removed from the UI. Single-folder import stays available through `POST /api/movies/import` for scripts and API consumers.

**Why this priority**: One import path in the UI; keeps the supported API contract intact.

**Independent Test**: Can be fully tested by rendering the header (no `Import folder` button, no dialog) and posting one folder to the import endpoint directly.

**Acceptance Scenarios**:

1. **Given** the running app, **When** the header renders, **Then** it shows `Bulk import` and no `Import folder` button or dialog.
2. **Given** a folder path, **When** posted to `POST /api/movies/import`, **Then** the movie imports (202) exactly as before.

---

### Edge Cases

- What happens when a candidate folder disappears between listing and import? The pass reports that folder as failed with its reason and continues with the rest.
- What happens when a candidate holds no eligible video at import time? Same: reported as a failed folder, rest continue.
- What happens when the library root is missing or unreadable? The listing fails fast with a clear error (same behavior as the existing one-shot bulk import).
- What happens when the user selects nothing and does not retry failed? The import is rejected as a validation error (nothing to do).
- How does the system handle a duplicate import racing the listing? Already-imported folders are skipped by path before importing, so a rerun or race degrades to skipped, never a 500.
- What happens on a 342-folder root? Import runs to completion in one action; progress is visible per folder and the grid updates live.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST list every candidate movie folder under the configured library root that is not yet imported, using the same recursive discovery rules as the existing one-shot bulk import (including a loose video file directly in the root).
- **FR-002**: System MUST import the user-selected candidates, or all of them, in one action.
- **FR-003**: System MUST skip already-imported folders by library path without failing, so any bulk import pass is safely repeatable.
- **FR-004**: System MUST NOT abort the pass on a per-folder failure; each failure is reported with its folder and a caller-safe reason code while the rest continue. Raw exception text MUST NOT cross the API.
- **FR-005**: System MUST list Failed-enrichment movies in the bulk import flow and requeue the selected/all ones back to Pending with enrichment re-staged.
- **FR-006**: System MUST show per-folder progress during the pass and a summary (imported / skipped / retried / failed) at the end, and refresh the library grid when it settles.
- **FR-007**: System MUST remove the per-folder Import dialog from the UI while keeping `POST /api/movies/import` behavior unchanged.
- **FR-008**: System MUST keep the existing `POST /api/movies/rescan` semantics untouched (stored-movie format re-probe only).

### Key Entities

- **Import candidate**: A library folder identity (relative path) discovered on disk but absent from the library, importable as one movie.
- **Bulk import result**: Counts of imported / skipped / retried folders plus per-folder failures with caller-safe reason codes.
- **Failed movie**: A library movie in Failed enrichment status, eligible for requeue to Pending.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A fresh stack imports a whole multi-folder library root in one bulk import action with zero already-imported folders re-imported.
- **SC-002**: Re-running bulk import immediately after imports zero folders and reports all candidates skipped with no error.
- **SC-003**: A loose video file directly in the library root appears as a candidate and imports successfully.
- **SC-004**: Every Failed movie retried from the bulk import returns to Pending status.
- **SC-005**: No `Import folder` button or dialog remains reachable in the UI, while a direct single-folder API import still returns 202.

## Assumptions

- The configured library root is readable by the API process (same precondition as today).
- Per-folder import cost stays dominated by the local probe plus one insert; a sequential pass is acceptable for an explicit user action (no concurrency requirement from the task).
- Failed-movie listing reuses the existing browse `Statuses` filter; no new query surface for statuses.
- Progress means visible per-folder outcomes plus live grid updates through the existing enrichment-status subscription, not a resumable background job.
- Vocabulary follows the constitution: the feature, identifiers, routes, and UI all say Import (never Scan for import behavior); the button reads `Bulk import`.
