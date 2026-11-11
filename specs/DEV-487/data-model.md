# Data model: Library bulk import (DEV-487)

## Core (records, `Core/Features/Import/`; commands/queries sealed per Principle II)

- `ImportCandidatesQuery` — sealed payload-free record.
- `ImportCandidatesResult(ImmutableArray<LibraryPath> NewFolders)` — canonical relative identities; root video = `"."`.
- `ImportLibraryFoldersCommand(ImmutableArray<string> Folders, bool RetryFailed)` — raw strings in; the constructor normalizes a default array to empty so the validator and handler never see an uninitialized collection; the handler canonicalizes each under `LibraryOptions.RootPath` with `LibraryPath.TryCanonicalizeUnderRoot` before the skip check, so an absolute in-root path resolves to the same identity the importer persists; at least one of (non-empty `Folders`, `RetryFailed`) required.
- `ImportLibraryFolderFailure(string Folder, ImportFolderFailureReason Reason)` — one per faulted folder. `Folder` is the request string as sent, so an entry that is not a valid `LibraryPath` is still representable. `Reason` is the `ImportFolderFailureReason` Enumeration, never exception text (Principle IV): `FolderNotFound` (`NotFoundException`), `NoVideo` (scanner `InvalidTransitionException`), `InvalidFolder` (`ValidationException`), `ImportFailed` (anything else); its `Code` is the wire value.
- `ImportLibraryFoldersResult(int Imported, int Skipped, int Retried, ImmutableArray<ImportLibraryFolderFailure> Failures)`.

## Repository additions (`IMovieRepository` / `EfMovieRepository`)

- `SnapshotMoviePathsAsync(CancellationToken) → IReadOnlyList<LibraryPath>` — `AsNoTracking`, `OrderBy(id)`, select `LibraryPath`.

## Telemetry

- Reuse `EnrichmentMetrics.ImportCount` (`lamuflix.import.count`, Principle VI — no new names): `Add(1)` per successfully imported folder in `ImportLibraryFoldersCommandHandler`, mirroring `ImportMovieFolderCommandHandler`.

## HTTP (`Api/Endpoints/BulkImportEndpoints.cs`, `BulkImportResponses.cs`)

- `GET /api/movies/import/candidates` → `200 ImportCandidatesResponse { string[] NewFolders }`.
- `POST /api/movies/import/library` ← `ImportLibraryRequest { string[] Folders, bool RetryFailed }` → `200 ImportLibraryResponse { int Imported, Skipped, Retried; ImportFolderFailureDto[] Failures }`.
- `ImportFolderFailureDto { string Folder, string Reason }`; `Reason` is `ImportFolderFailureReason.Code` from the closed set above.
- `422` on validation faults (pipeline `ValidationException` → `errors` extension); `500` when the root is missing or unreadable (existing discovery exception contract).
