# Specification Quality Checklist: Stream primary movie file over HTTP

**Purpose**: Validate specification completeness and quality before planning challenge / Gate 1
**Created**: 2026-11-07
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No unexplained implementation HOW leaking as product UX (API streaming; Compose SPA product UI unchanged)
- [x] Focused on ticket ACs and Patron Q1–Q11 rulings (Q10 supersedes earlier no-ADR wording; Q11 plan-challenge refinements; ADR 0037 authored / remains Proposed until governance)
- [x] Written for stakeholders reviewing Gate 1 (Patron/owner), not only implementers
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous against frozen Q1–Q11 contracts
- [x] Success criteria are measurable against ticket full/partial/invalid/seeking ACs (incl. nosniff / policy-exception distinctions)
- [x] All acceptance scenarios are defined (US1–US4), including LocalPlay=false, containment denials, concurrent-reader proof, Principle VI structured movie-id errors, and required browser seek
- [x] Edge cases are identified (characterization-only Range edges, linked-path fail-closed with fail-loud fixtures, TOCTOU out of scope, 416 ProblemDetails exemption, inherited-only 422, DI-file deferral, residual Q10/Q11 constitution/governance + atomic port/whitelist + Principle VI Build requirements still open — ADR 0037 already exists)
- [x] Scope is clearly bounded (in/out; frozen scope; Q9 follow-up rule; Q10/Q11 scoped compliance surfaces; Sentry F4 / Ledger L4 no-code residuals)
- [x] Dependencies and assumptions identified (DEV-438/DEV-459; ASSUMPTIONS.md none; Phase B Wisp DI file; existing ADR `docs/adr/0037-library-file-reader-port.md`; whitelist plan from recon-DEV-456-3; whitelist code with T003; Keel T001c verify-only + T001d constitution edit)
- [x] Q1–Q11 encoded across spec/plan/tasks (incl. Q11 port/exception/open/nosniff/Principle VI/sequencing; T006a/b/c regressions; T001c/d split)

## Feature Readiness

- [x] All functional requirements have clear acceptance / success links (13 FR + 5 SC retained)
- [x] User scenarios cover containment → stream contract → OpenAPI → live nginx/browser
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] Task order matches brief Q8 as amended by Q10/Q11 (Wisp facts → Keel analyze → T001c verify-only → T001d constitution → T003 type slice → T002 behavioral reds → T004 → T005 → endpoint incl. T006a/b/c → contract → Compose → gates)
- [x] No owner Gate 1 structural checkbox from grill / Q10–Q11 (compliant correction; no principle waiver; no ticket deliverable change)
- [x] Code tasks remain unchecked; no implementation clearance claimed
- [x] PLAN FROZEN / analyze CLEAN / Principle I implementation PASS / runtime PASS not claimed by Quill; repaired draft pending Keel independent plan-challenge freeze; ordinary Gate 1 remains pending

## User-Visible Outcome

- [x] "What the User Will See" says Nothing visible in Compose SPA product UI with API-streaming reason
- [x] Smoke Test subsection skipped because nothing is visible in the SPA product UI
- [x] Required browser seek is covered as US4 / FR-010 live acceptance, not as product SPA smoke

## Notes

- Checklist revalidated by Quill after plan-challenge repair round 1 from brief/CONCLUSIONS Q11 + `PLAN_CHALLENGE_FIXES.md`. Authoritative decisions live in brief/CONCLUSIONS Q10–Q11; stale analyze-fix round-2/final-under-cap banners trimmed from current authored status.
- ADR `docs/adr/0037-library-file-reader-port.md` is Keel-authored and already exists; T001c is Keel verify-only at pickup (does not reauthor); ADR remains Proposed until T001d governance/extension. ADR M1 repair is Keel-owned.
- T001d is the distinct Keel-owned constitution-edit deliverable (ILibraryFileReader only; appropriate MINOR; Sync Impact Report; Last Amended; template review; migration/follow-up; no DEV-467 backfill).
- Whitelist **plan** from recon-DEV-456-3: append-only `|ILibraryFileReader` before `)$` on `ArchitectureTests.cs:31`, preserve 16 entries/order; whitelist **code** lands atomically with port at T003 only.
- Constitution omission of two prior ports is recon fact only — not authorization to broaden this scoped extension.
- Principle VI is an explicit pending Build requirement (not N/A); T006c + T007/T010 cover structured requested-movie-id errors without filesystem paths.
- Quill does **not** claim PLAN FROZEN, CLEAN, Gate 1 approval, Principle I implementation PASS, analyze-clean, or runtime PASS. Constitution/governance + atomic port/whitelist execution remain mandatory.
- Phase A / plan-challenge repair runs no production tests, delivery gates, commits, or PR.
- Exact Infrastructure DI file remains Phase B Wisp fact-finding, not a Quill fill.
