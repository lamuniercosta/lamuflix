# DEV-521 Conclusions

## Sentry Web Candidate Findings Ruling (2026-11-17)

**Verdict:** Accept Sentry F1 (Medium) for remediation by Cog; waive Sentry F2 (Low) as out of scope (`noted, no ticket`).

- **F1 Medium accepted for remediation (`DEV-521` `## Wanted`, `web/src/library/BulkImportDialog.tsx:183`, `web/src/library/libraryQueries.ts:144-160`):** `DEV-521` requires showing a loading state while the scan runs and keeping `Cancel` while a scan is running; gating `scanning` on `candidates.isPending` alone misses warm-cache reopens and retries where `candidates.isFetching` is `true` while `isPending` is `false`, so Cog must derive `scanning` from `candidates.isPending || candidates.isFetching` (and disable `Select all` / `Import` while `scanning`).
- **F2 Low waived / out of scope (`DEV-521` `## Problem` & `## Wanted`, `findings-DEV-521-Sentry:19-27`):** `DEV-521` scopes button state (`Close` when idle, `Cancel` while scanning or importing) and does not add HTTP abort/cache-cancellation semantics for the read-only `GET /api/movies/import/candidates` query; noted, no ticket.
- **No Charter §2.3 escalation (`lamuflix-team-charter` §2.3):** Remediating F1 stays within `web/src/library/BulkImportDialog.tsx` and its existing test surface without adding dependencies, layers, schema changes, or contract changes.
