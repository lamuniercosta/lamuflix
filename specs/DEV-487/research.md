# Research: Library bulk import (DEV-487)

Verified in tree 2026-11-10 (worktree `feature/DEV-487-…`, base `origin/main`).

## Reused seams (do not modify)

- `Core/Features/Import/ImportLibraryCommand{,Handler,Result}.cs` — one-shot bulk import, fail-fast, no skip, no HTTP surface (DEV-333 froze this). Reuse its *dependencies* (`ILibraryFolderDiscovery`, `IMovieFolderImporter`), never its semantics.
- `Infrastructure/FileSystem/FileSystemLibraryFolderDiscovery.cs` — recursive lazy discovery, root `.` identity for loose root video, skips links, containment guard.
- `Infrastructure/Import/ScopedMovieFolderImporter.cs` — fresh DI scope per folder over the decorated `ImportMovieFolderCommandHandler`. Safe to call sequentially (or in parallel).
- `RequestEnrichmentCommandHandler` — retry guard is `NotFound`/`Failed` only, then `RequestEnrichment()` + `outbox.Stage(EnrichmentRequested(id, 1))` + `SaveChangesAsync`.
- `GlobalExceptionHandler` — `ValidationException`→422, `NotFoundException`→404, `InvalidTransitionException`→409, else 500.
- Web live updates — `useEnrichmentStatus` SignalR subscription + `invalidateBrowseMovies`; this is the "progress" surface (no new infra).
- `EnrichmentMetrics.ImportCount` (`lamuflix.import.count`) — reused per imported folder; no new telemetry names (Principle VI).

## Gaps closed by this ticket

- No repository read of paths or failed ids: add `SnapshotMoviePathsAsync` + `FindFailedMovieIdsAsync` to `IMovieRepository` (methods only — the closed port-name whitelist and Principle I are untouched; no ADR) with `EfMovieRepository` implementations (`AsNoTracking`, `PresentIds` pattern) proven by Testcontainers tests.
- Skip comparison is `OrdinalIgnoreCase` to mirror `IX_movies_library_path_key` (uppercase derived key, unique).
- Per-folder faults are data (`ImportLibraryFolderFailure` with closed reason codes), never thrown — parallels `RescanLibraryResult.FailedMovieIds` and satisfies Principle IV (no exception text over the wire).
- Validator: `ImportLibraryFoldersCommand` has a payload → real FluentValidator (mirror `RescanMovieCommandValidator`); candidates query is payload-free → none (payload-free sweep-command precedent).
- Vocabulary: constitution Principle VIII reserves Import (never Scan for import behavior) — all identifiers, routes, and UI text use Import; the button reads `Bulk import`.
