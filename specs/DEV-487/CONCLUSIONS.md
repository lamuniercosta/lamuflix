# DEV-487 — Conclusions (grill, 2026-11-10)

Task: Bulk import: scan the library root and import all new folders in one action (feature).
Tracker source of truth: YouTrack DEV-487 summary + description, fetched via `scripts/get-task.ps1`.

## Prior session context

The work began as a chat ask: replace the header `Import folder` button with a
rescan button running import over the library folder for new or failed movies.
Inspection showed `POST /api/movies/rescan` only re-probes stored movies and
cannot discover new folders on disk, so the ask split into scope (frontend-only
rescan vs rescan plus new-folder discovery) and dialog (remove vs keep)
questions. The user chose new-folder discovery plus dialog removal then asked
whether a YouTrack task exists to remove the import file. No removal task was
found (`import`, `remove`, `rescan OR dialog` searches, DEV-489 subtasks,
recently-updated list); the closest match was DEV-487, plus epic DEV-489.
The user directed work onto DEV-487. Intake completed; branch
`feature/DEV-487-bulk-import-scan-the-library-root-and-import-all-new-folders`
in its own worktree off `origin/main`.

---

## Decision 1 — UX shape: scan list with select and progress

Question: DEV-487 asks for a Scan action that lists not-yet-imported folders
and imports the selected ones or all with progress; the earlier chat ask was a
one-click rescan button. Recommended the scan list because the task is the
source of truth and a blind one-click gives no visibility across hundreds of
folders.

Answer: scan list with select and progress.

Rationale: tracker text governs; per-folder visibility matters at 342 folders.
Implications: larger frontend (candidate list, selection, progress feedback);
the one-click rescan variant is dropped.

---

## Decision 2 — Failed movies: include retry in this ticket

Question: DEV-487 says nothing about failed movies — Done-when is new folders
plus skip-imported, and per-row Retry already covers Failed enrichment.
Recommended new-folders-only to keep the ticket tight.

Answer: include failed-movie retry.

Rationale (user's call): the original ask was "new or failed movies".
Implications: the bulk flow needs a Failed-status query plus a requeue path
and tests; scope grows beyond the task's Done-when, recorded here as an
explicit extension.

---

## Decision 3 — Import dialog: remove it

Question: with the scan-list UX confirmed, recommended removing the per-folder
import dialog so there is one import path.

Answer: remove the dialog.

Rationale: matches the earlier call; single import path. Implications:
delete `web/src/library/ImportDialog.tsx` and its unit test; rework the App,
polling, and e2e import-flow tests around the bulk import action. `POST
/api/movies/import` stays as the single-folder surface for scripts and API
consumers.

---

## Amendment — analyze pass (2026-11-10, pre-handoff)

Self-review of spec/plan/tasks against the constitution found two critical
draft defects, fixed in the artifacts before any implementation:

1. Vocabulary (Principle VIII): the drafts named the feature Scan
(`ScanLibrary*` types, `/api/movies/scan/*` routes, `Scan library` button).
The constitution reserves Import and explicitly lists Scan as a non-Import
term. Renamed throughout to Import vocabulary: `ImportCandidates*`,
`ImportLibraryFolders*`, routes `GET /api/movies/import/candidates` and
`POST /api/movies/import/library`, button `Bulk import` (the task's own noun
phrase).
2. Failure surfacing (Principle IV): the drafts returned `ex.Message` per
failed folder. Raw exception text must never cross the API. Failures now
carry a closed reason code (`folder-not-found | no-video | invalid-folder |
import-failed`) mapped from exception type in the handler; the web maps codes
to fixed sentences.

Non-blocking notes: the prerequisites script rejects the task branch name
(it expects speckit numbering; the harness owns task-branch naming, so this
is environmental noise); `rescan` semantics are pinned by the untouched
existing suites in the T7 full run.
