# Contracts: Library bulk import (DEV-487)

Mirrors the `openapi.json` operation shape the generator produces from the
minimal-API wiring. The committed `api/openapi.json` + `types.ts` are
regenerated in T4, never hand-edited; this file is the review surface.

## GET /api/movies/import/candidates

- Operation `ListImportCandidates`, tag `BulkImportEndpoints`, summary "List library folders not yet imported."
- `200` → `application/json` → `ImportCandidatesResponse { newFolders: string[] }`
- `500` → `problem+json` when the root is missing or unreadable (existing discovery exception contract; `InvalidOperationException`/`DirectoryNotFoundException` are unmapped)

## POST /api/movies/import/library

- Operation `ImportLibraryFolders`, tag `BulkImportEndpoints`, summary "Import selected library folders and retry failed enrichments."
- Request `application/json` → `ImportLibraryRequest { folders: string[], retryFailed: boolean }` (required)
- `200` → `application/json` → `ImportLibraryResponse { imported: integer, skipped: integer, retried: integer, failures: [{ folder: string, reason: string }] }`, where `reason` is one of `folder-not-found | no-video | invalid-folder | import-failed`
- `400` → `problem+json`; `422` → `problem+json` with `errors` (folder validation)
