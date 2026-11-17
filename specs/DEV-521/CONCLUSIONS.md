# DEV-521 Conclusions

## Sentry Web Candidate Findings Ruling (2026-11-17)

**Verdict:** Accept Sentry F1 (Medium) for remediation by Cog; waive Sentry F2 (Low) as out of scope (`noted, no ticket`).

- **F1 Medium accepted for remediation (`DEV-521` `## Wanted`, `web/src/library/BulkImportDialog.tsx:183`, `web/src/library/libraryQueries.ts:144-160`):** `DEV-521` requires showing a loading state while the scan runs and keeping `Cancel` while a scan is running; gating `scanning` on `candidates.isPending` alone misses warm-cache reopens and retries where `candidates.isFetching` is `true` while `isPending` is `false`, so Cog must derive `scanning` from `candidates.isPending || candidates.isFetching` (and disable `Select all` / `Import` while `scanning`).
- **F2 Low waived / out of scope (`DEV-521` `## Problem` & `## Wanted`, `findings-DEV-521-Sentry:19-27`):** `DEV-521` scopes button state (`Close` when idle, `Cancel` while scanning or importing) and does not add HTTP abort/cache-cancellation semantics for the read-only `GET /api/movies/import/candidates` query; noted, no ticket.
- **No Charter §2.3 escalation (`lamuflix-team-charter` §2.3):** Remediating F1 stays within `web/src/library/BulkImportDialog.tsx` and its existing test surface without adding dependencies, layers, schema changes, or contract changes.

## Review Round 2 Ruling on HEAD `7fe902a3` (2026-11-17)

**Verdict:** Waive Round 2 Sentry F1 (Medium) by amending the Round 1 action-guard clause to match existing candidate-list behavior during background refetch; waive Round 2 Sentry F2 (Low) under the `task-pipeline` §2.2 two-round review cap (`noted, no ticket`).

- **F1 Medium waived / Round 1 ruling reconciled (`DEV-521:182-193`, `findings-DEV-521-Sentry:11-19`, `findings-DEV-521-Keel:10`, `web/src/library/BulkImportDialog.tsx:183-184,207,239-260,331-344`):** Keeping cached candidate folders visible and actionable (`disabled` gated on `initialScan` / `candidates.isPending`) while showing `Scanning library…` and `Cancel` during `isFetching` preserves required candidate-list visibility and existing hosted-CI behavior without data loss or corruption; Round 1's `CONCLUSIONS.md:7` parenthetical is amended so `scanning` (`isPending || isFetching`) governs the status hint and `Cancel`/`Close` toggle while `initialScan` (`isPending`) governs `Select all` and `Import` disabled guards.
- **F2 Low waived (`task-pipeline` §2.2 Review cap, `findings-DEV-521-Sentry:21-29`, `findings-DEV-521-Keel:11,13`):** Omitting `role="status"` on the secondary non-empty background-refetch hint at `web/src/library/BulkImportDialog.tsx:257` is a Low cosmetic/a11y edge case (cold-open and empty-scan hints at `:226,231,235` carry `role="status"`), and DEV-521 has reached the 2-round review cap with 2 remediation commits (`5014c044`, `5251c646`); waived as `noted, no ticket`.
- **No Charter §2.3 escalation (`lamuflix-team-charter` §2.3):** No code, dependency, layer, schema, or contract changes are required.

