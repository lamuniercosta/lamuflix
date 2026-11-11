# DEV-487 — Brief

Source: YouTrack DEV-487 (fetched 2026-11-10 via `scripts/get-task.ps1`).
Type: feature. Branch:
`feature/DEV-487-bulk-import-scan-the-library-root-and-import-all-new-folders`.

## Problem

The only way to add movies is the Import folder dialog, which takes one folder
name typed by hand. There is no browse, scan or bulk option. The user's real
library has 342 folders. A video file sitting directly in the library root
(`Big Buck Bunny (2008).mp4`) cannot be imported at all (404).

## Wanted

A "Scan library" action that lists folders (and loose video files) not yet
imported, and imports the selected ones or all of them, with progress.

## Done when

A fresh stack can import a whole library root in one action, and already
imported folders are skipped.

Source: user test of the running Compose stack on 2026-11-09 (17-movie
stand-in library).

## Settled scope (grill 2026-11-10, see CONCLUSIONS.md)

1. UX is the task-verbatim scan list: candidates listed, import selected or
   all, with progress. No one-click blind variant.
2. Extension beyond Done-when: the bulk pass also covers Failed-enrichment
   movies (requeue), per the originating "new or failed movies" ask.
3. The per-folder Import dialog is removed (component, unit test, and
   dialog-driven App/polling/e2e coverage reworked around the scan action).
   `POST /api/movies/import` remains the single-folder API surface.

## Remediation loop (fix 2026-11-10)

- Closing bar: all 8 accepted `$code-review` findings closed, re-review of the round diff clean, `dotnet test` for touched areas plus Roslyn/complexity gates pass.
- Frozen scope: the 8 CONFIRMED findings in `findings-f59eb1722b543291fdaa2c4236e1c08b581e5c85.json` only (null Folders 500; double-count ImportCount; duplicate-folder double import; cancelled-as-failure; N+1 failed retry; duplicated skip set; triplicated folder validation; dead NotFound branch). No new behavior.
- Round cap: 2.
- Round 2 (in cap): the 5 accepted findings in `findings-6bec2fbd93e07e8d6d585dec059277c0ae29d0.json` (split-query fan-out, dead `FindFailedMovieIdsAsync`, overlapping-pass race, raw-string dedupe, redundant null layers). No new behavior beyond closing those rows.
- Round 3 (cap reopened by the parent, option A): the 3 findings in `findings-5bf56b1716524bf8a41e54fdb93a6d80da3fbce5.json` (static `PassGate` in the mutated project; malformed folder aborting the pass; validator NRE on null `Folders`). Closing bar unchanged.
- Corrected premise: the database already carries `IX_movies_library_path` UNIQUE (Initial migration), so duplicate imports were never persistable. The round-2 report's "no unique index" premise was wrong; the real defect is that the duplicate surfaced as `import-failed` instead of skipped.

## Known starting facts (verified pre-grill, not decisions)

- Core already has an internal one-shot bulk path (`ImportLibraryCommand`,
  `FileSystemLibraryFolderDiscovery`, `ScopedMovieFolderImporter`) with no
  HTTP surface and fail-fast duplicate semantics — unsuitable as-is for a
  repeatable skip-imported scan.
- `POST /api/movies/rescan` re-probes stored movies only; it cannot discover
  new folders on disk.
- Discovery already yields a root identity for loose root-level video.
