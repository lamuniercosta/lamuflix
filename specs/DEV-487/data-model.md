# Data model: Library bulk import (DEV-487)

## Core (records, `Core/Features/Import/`; commands/queries sealed per Principle II)

- `ImportCandidatesQuery` — sealed payload-free record.
- `ImportCandidatesResult(IReadOnlyList<LibraryPath> NewFolders)` — canonical relative identities; root video = `"."`.
- `ImportLibraryFoldersCommand(IReadOnlyList<string> Folders, bool RetryFailed)` — raw strings in; canonicalized via `LibraryPath.TryCreate` by the validator/handler; at least one of (non-empty `Folders`, `RetryFailed`) required.
- `ImportLibraryFolderFailure(LibraryPath Folder, string Reason)` — one per faulted folder. `Reason` is a closed code, never exception text (Principle IV): `folder-not-found` (`NotFoundException`), `no-video` (scanner `InvalidTransitionException`), `invalid-folder` (`ValidationException`), `import-failed` (anything else).
- `ImportLibraryFoldersResult(int Imported, int Skipped, int Retried, IReadOnlyList<ImportLibraryFolderFailure> Failures)`.

## Repository additions (`IMovieRepository` / `EfMovieRepository`)

- `SnapshotMoviePathsAsync(CancellationToken) → IReadOnlyList<LibraryPath>` — `AsNoTracking`, `OrderBy(id)`, select `LibraryPath`.
- `FindFailedMovieIdsAsync(CancellationToken) → IReadOnlyList<MovieId>` — `AsNoTracking`, `Where(Status == Failed)`, select id, `PresentIds` pattern.

## Telemetry

- Reuse `EnrichmentMetrics.ImportCount` (`lamuflix.import.count`, Principle VI — no new names): `Add(1)` per successfully imported folder in `ImportLibraryFoldersCommandHandler`, mirroring `ImportMovieFolderCommandHandler`.

## HTTP (`Api/Endpoints/BulkImportEndpoints.cs`, `BulkImportResponses.cs`)

- `GET /api/movies/import/candidates` → `200 ImportCandidatesResponse { string[] NewFolders }`.
- `POST /api/movies/import/library` ← `ImportLibraryRequest { string[] Folders, bool RetryFailed }` → `200 ImportLibraryResponse { int Imported, Skipped, Retried; ImportFolderFailureDto[] Failures }`.
- `ImportFolderFailureDto { string Folder, string Reason }`; `Reason` from the closed set above.
- `422` on validation faults (pipeline `ValidationException` → `errors` extension); `500` when the root is missing or unreadable (existing discovery exception contract).
