# Specification Quality Checklist: SPA cleanup (dead exports, navigation, cohesive splits)

**Purpose**: Validate specification completeness and quality before proceeding to planning / Gate 1
**Created**: 2026-11-14
**Feature**: [spec.md](../spec.md)
**Status**: Plan-repair round 2 complete; Q13 governance consistency applied. Analyze CLEAN then plan-challenge NEEDS FIXES recorded. PLAN FROZEN withheld. Gate 1 remains closed pending unanswered Q13 structural owner checkbox (retain runtime `role=actor` on `GET /people`) plus ordinary owner Gate 1. Implementation, runtime, and merge remain distinct and open.

## Content Quality

- [x] No implementation details (languages, frameworks, APIs) beyond frozen ticket/brief/CONCLUSIONS boundaries named as scope
- [x] Focused on user value and business needs (behavioural parity + maintainability cleanup)
- [x] Written for non-technical stakeholders in user-visible / success sections
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain for inventable behaviour
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic in outcomes (web lane names appear only where ticket Done-when requires them)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded (Tailwind DEV-511 and jsx-a11y DEV-512 follow-up exclusions; Q12 GetPeople retention pending Q13 owner checkbox)
- [x] Dependencies and assumptions identified (Q10–Q13 resolutions cited; not invented)
- [x] Spec Owner question carries unanswered Q13 structural checkbox for retaining runtime `role=actor` on `GET /people`; Gate 1 stays closed until checked; Spec PR MUST carry that checkbox

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows (orphan/type, navigation, splits, verification; a11y deferred)
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No unjustified implementation details leak into specification inventable behaviour

## User-Visible Outcome

- [x] "What the User Will See" states nothing new as a product feature, with behavioural-parity surfaces named
- [x] Each parity surface has a SMOKE-### check in user terms with named e2e candidate/extension map
- [x] What each surface needs to show up in a plain `docker compose up` is listed

## Clarify / recon honesty

- [x] Interactive clarify session recorded: inventable behaviour frozen by Q1–Q9; Q10–Q13 supersessions/reconciliations applied
- [x] Former recon/plan-challenge gaps resolved individually in `DRAFTING_RECEIPT.md` (siblings Q10, navigation path Q11, GetPeople/chip/range Q12, Q13 structural checkbox, complete importer set lines 12–14, smoke map lines 25–27, ticket authority lines 39–58, jsx-a11y deferred DEV-512, commands from package.json, Tailwind DEV-511)
- [x] plan.md / tasks.md T000 / DRAFTING_RECEIPT no longer claim zero structural checkboxes; they require the Q13 owner checkbox on the spec PR

## Notes

- Spec cites approved file paths and proposed ADR 0042 as brief/CONCLUSIONS boundaries (Q1–Q13), not as HOW invented by Quill.
- Q10-ratified sibling filenames, Q11 `appNavigation.ts`, Q12 role/chip/range rulings, and Q13 structural checkbox are enumerated in plan/tasks.
- Validation iteration 4 (2026-11-14 Q13 governance consistency): checklist aligned with Patron Q13. PLAN FROZEN remains Keel's to declare after independent reread; checkbox remains unanswered.
