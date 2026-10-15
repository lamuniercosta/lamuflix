# DEV-431 — Remediation phases

Bookkeeping for the `$remediate` loop only. The working plan lives in `brief.md`
(pipeline shape: no full tasks.md — CONCLUSIONS.md Decision 1).

## Round 1 (PR #169 review, 2026-10-15)

- [ ] R1-G1 **[Medium · coverage]** (source: PR #169 inline comment 4279750096 on `web/src/features/library/FilterPanel.test.tsx`): assert the intermediate upper-only year range `?yearMin=&yearMax=2010` after resetting yearMin to its extent, before resetting yearMax, in `'sends both year keys and omits the group back at the year extents'`.
