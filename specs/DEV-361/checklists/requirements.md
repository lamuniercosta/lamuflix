# Specification Quality Checklist: DEV-361

**Purpose:** Validate the DEV-361 spec before planning. Revised for the owner's B1 answer.  
**Created:** 2026-09-23 · **Revised:** 2026-09-23  
**Feature:** [spec.md](../spec.md)

- [x] The six authorized inspection keys are named exactly: five from the ticket, one from the owner's B1 answer.
- [x] AC2 has a measured 25-path target, backed by the solution-wide SUGGESTION probe with its exit and counts.
- [x] Q2–Q4 preserve behavior and external writers. FR-008 names a writer reason for each B1 group.
- [x] No new package, project, layer, schema, entity nullability change, or wider file set is assumed.
- [x] The B1 owner checkbox is answered [x], citing PR #3 and the user's confirmation.
- [x] Taste assumptions are tagged `[assumed]` in `ASSUMPTIONS.md`.
- [x] The refactor gate on `EntityExtensions.DynamicQuery` follows `constitution.md:344-345` (T019), with no waiver.
- [x] Patron ruled P1 (T019 is constitution-mandated, hard stop if unremedied) and P2 (size M).
- [x] Owner answered the (A)/(B) plan-change checkbox (T020): **(A)**, 2026-09-23. Proceed without waiting for the re-spec PR to merge.
- [x] Patron ruled on P3, and the Round 2 re-freeze is done (T018, 2026-09-23).
- [x] FR-008 is fix-first per ticket AC3, with suppression only as a fallback (Compass F1).
