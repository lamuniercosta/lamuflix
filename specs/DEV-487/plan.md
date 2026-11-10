# Implementation Plan: Library bulk import (DEV-487)

**Branch**: `feature/DEV-487-bulk-import-scan-the-library-root-and-import-all-new-folders` | **Date**: 2026-11-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/DEV-487/spec.md` (this directory).

## Summary

Expose the existing internal bulk-import machinery over HTTP as a list-then-import flow and repoint the UI at it. Two new endpoints: `GET /api/movies/import/candidates` (folders on disk minus library paths) and `POST /api/movies/import/library` (`{ folders, retryFailed }` → imported/skipped/retried plus per-folder failures with caller-safe reason codes). New Core query/command pair under `Features/Import` reusing `ILibraryFolderDiscovery` and `IMovieFolderImporter` (`ScopedMovieFolderImporter`, one DI scope per folder). Already-imported detection is a new `IMovieRepository.SnapshotMoviePathsAsync` compared case-insensitively (mirrors the `IX_movies_library_path_key` uppercase semantics). Failed retry reuses the `RequestEnrichment` guard (`NotFound`/`Failed` only) with `IMovieRepository` + `IEnrichmentOutbox` directly, backed by a new `FindFailedMovieIdsAsync`. The header button becomes `Bulk import` opening a candidate dialog; `ImportDialog` and the single-import optimistic-placeholder machinery are deleted; progress rides the existing browse invalidation plus SignalR enrichment subscription.

## Technical Context

**Language/Version**: C# (net10.0, see `global.json`) + TypeScript React 19 SPA (`web/`)

**Primary Dependencies**: ASP.NET Core minimal APIs, TanStack Query, Entity Framework Core (PostgreSQL), System.IO.Abstractions, FluentValidation, openapi-fetch / openapi-msw / MSW

**Storage**: PostgreSQL via EF Core (`LamuFlixDbContext`, `MovieRecord` with `LibraryPath` + derived `LibraryPathKey`)

**Testing**: xUnit v3 + NSubstitute + Shouldly (unit), Testcontainers-backed `PostgresFixture` (integration, mandatory for real query translation — never mock the data-access driver), Vitest + React Testing Library + MSW (web), Playwright `web/e2e` (smoke)

**Target Platform**: Linux containers (Compose) + Windows dev checkout; `TimeProvider` for time (no `DateTime.Now` — build error)

**Project Type**: Web application (ASP.NET Core API + React SPA)

**Performance Goals**: One user-triggered sequential pass over the root; no p95 target (explicit action, progress shown)

**Constraints**: `BannedSymbols.txt` (`DateTime.Now` fails build); no explanatory comments (AAA headers in tests only); no second assertion/mocking library; generated files (`openapi.json`, `types.ts`, `handlers.ts`) only via `npm run generate:api`; never lower a gate threshold; constitution Principle IV — raw exception text never crosses the API (reason codes only); Principle VIII — Import vocabulary everywhere (never Scan for import behavior)

**Scale/Scope**: Single ticket; production files ~8 new + ~6 edited; unit + integration + web + 1 e2e rework

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- Time: the new handlers need no clock (no timestamps written on the import/retry path). No `TimeProvider` needed; nothing to violate.
- Randomness: none. Seeded `Random` n/a.
- Mutants: new Core handler branches (skip/retry/fault-map) are kill-tested by NSubstitute-double unit tests in the `ImportLibraryHandlerDoubles` style; Infrastructure additions are mutation-excluded per `harness.yml` but proven by Testcontainers integration tests.
- Architecture: Core stays BCL-only (new Core files reference only `Core.Domain`, `Core.Ports`, `Core.Pipeline`, `Microsoft.Extensions.Options` — the existing `ImportLibraryCommandHandler` precedent). DI scope seam stays in `Infrastructure/Import`. No new port interfaces — only methods on the existing `IMovieRepository`, so the closed port-name whitelist in `ArchitectureTests` and Principle I are untouched. No ADR required.
- Validation: `ImportLibraryFoldersCommand` carries a payload → real FluentValidation validator required (precedent: `RescanMovieCommandValidator`; the payload-free `ImportLibraryCommand` deliberately has none — do not touch it). Candidates query is payload-free → no validator (payload-free sweep-command precedent).
- State machine: retry reuses the exact `RequestEnrichmentCommandHandler` guard (`NotFound`/`Failed` only); per-folder failures surface as closed reason codes, never exception text (Principle IV).
- Telemetry: reuse the existing `lamuflix.import.count` counter (`EnrichmentMetrics.ImportCount`) once per imported folder — no new metric names (Principle VI).
- No new public route for old behavior; `POST /api/movies/import` and `POST /api/movies/rescan` untouched.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-487/
├── brief.md             # Tracker intake + settled scope (done)
├── CONCLUSIONS.md       # Grill decisions + analyze amendment (done)
├── spec.md              # This feature
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (seams verified 2026-11-10)
├── data-model.md        # Phase 1 output (command/query/result shapes)
├── quickstart.md        # Phase 1 output (how to run the flow locally)
├── contracts/           # Phase 1 output (OpenAPI delta for the two endpoints)
└── tasks.md             # Phase 2 output (mechanical slices for the headless session)
```

### Source Code (repository root)

```text
src/
├── LamuFlix.Core/
│   ├── Features/Import/
│   │   ├── ImportCandidatesQuery.cs              # NEW sealed payload-free record
│   │   ├── ImportCandidatesResult.cs            # NEW (IReadOnlyList<LibraryPath> NewFolders)
│   │   ├── ImportCandidatesQueryHandler.cs      # NEW sealed (discovery minus repo paths)
│   │   ├── ImportLibraryFoldersCommand.cs       # NEW sealed (IReadOnlyList<string> Folders, bool RetryFailed)
│   │   ├── ImportLibraryFoldersResult.cs        # NEW (Imported, Skipped, Retried, Failures)
│   │   ├── ImportLibraryFolderFailure.cs        # NEW (LibraryPath Folder, string Reason: closed code)
│   │   └── ImportLibraryFoldersCommandValidator.cs # NEW FluentValidation
│   └── Ports/
│       └── IMovieRepository.cs                  # ADD SnapshotMoviePathsAsync + FindFailedMovieIdsAsync
├── LamuFlix.Infrastructure/
│   └── Persistence/
│       └── EfMovieRepository.cs                 # ADD the two queries (AsNoTracking, PresentIds pattern)
└── LamuFlix.Api/
    └── Endpoints/
        ├── BulkImportEndpoints.cs               # NEW MapBulkImportEndpoints (candidates GET + library POST)
        └── BulkImportResponses.cs               # NEW DTOs with From mappers (ReSharper disable/restore pattern)

web/
└── src/
    ├── library/
    │   ├── ImportDialog.tsx                     # DELETE
    │   ├── ImportDialog.test.tsx                # DELETE
    │   ├── BulkImportDialog.tsx                 # NEW candidate dialog
    │   ├── BulkImportDialog.test.tsx            # NEW dialog behavior
    │   └── libraryQueries.ts                    # ADD import hooks; DELETE placeholder machinery
    └── App.tsx                                  # Bulk import button + dialog; DELETE import wiring
```

**Structure Decision**: Follow the existing feature-folder layout (`Features/Import` for the bulk path, one endpoints file per surface like `RescanEndpoints`). No new projects, layers, or port interfaces.

## Phase 0 — Research findings (verified in tree, 2026-11-10)

- Bulk internals exist but are HTTP-invisible: `ImportLibraryCommand` (fail-fast, no skip) + `FileSystemLibraryFolderDiscovery` (recursive, yields root `.` for loose root video, skips links, containment guard) + `ScopedMovieFolderImporter` (fresh scope per folder over the decorated `ImportMovieFolderCommandHandler`). Reuse discovery + importer; do not alter `ImportLibrary*` semantics or tests.
- `POST /api/movies/rescan` re-probes stored movies only — untouched by this ticket.
- `MovieRecord` carries `LibraryPath` + derived uppercase `LibraryPathKey` with unique index; duplicates fault at the DB. Skip-compare is `OrdinalIgnoreCase` to mirror the index.
- Retry guard precedent: `RequestEnrichmentCommandHandler` allows `NotFound`/`Failed` only, then `movie.RequestEnrichment()` + `outbox.Stage(EnrichmentRequested(id, 1))` + `SaveChangesAsync`. Replicate exactly.
- HTTP faults: `ValidationException` → 422, `NotFoundException` → 404, `InvalidTransitionException` → 409 (`GlobalExceptionHandler`); anything else → 500. Per-folder import faults are therefore data, never thrown.
- Failure reasons are a closed code set mapped from exception type in the handler: `NotFoundException` → `folder-not-found`, scanner `InvalidTransitionException` → `no-video`, `ValidationException` → `invalid-folder`, anything else → `import-failed`. The web maps codes to fixed sentences; no exception text crosses the API (Principle IV).
- Web placeholder machinery (`armImportPlaceholder`, `pendingRow`, token fns in `libraryQueries.ts`) serves only the deleted dialog flow; `markRowPending` stays (retry flow in `movieDetailsQueries.ts`).
- Failed listing reuses `GET /api/movies?Statuses=Failed` (existing `Statuses` filter; T5 verifies it accepts `Failed`).
- Generated web API files refresh via `npm run generate:api`; `npm run check:api` gates drift.

## Phase 1 — Wire shapes (data-model.md / contracts detail)

- `GET /api/movies/import/candidates` → `200 ImportCandidatesResponse { string[] NewFolders }` (canonical relative identities; root video = `"."`). Missing/unreadable root → `500` (existing discovery exception contract).
- `POST /api/movies/import/library` ← `ImportLibraryRequest { string[] Folders, bool RetryFailed }` → `200 ImportLibraryResponse { int Imported, Skipped, Retried; ImportFolderFailureDto[] Failures }` with `ImportFolderFailureDto { string Folder, string Reason }` where `Reason` is one of `folder-not-found | no-video | invalid-folder | import-failed`. Validation failure → `422` with `errors`.
- OpenAPI delta in `contracts/` mirrors these shapes; committed `openapi.json` + `types.ts` regenerated, never hand-edited.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | — | New handlers stay small: candidates = set difference; import = sequential loop with three counters. If any file nears `gates.complexity.implement` (15), split helpers before handoff. |
