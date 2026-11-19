# Specification Quality Checklist: Recommendation cards show seed and evidence

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-11-19
**Feature**: [spec.md](../spec.md)

## Content Quality

- [ ] No implementation details — deliberate exception: Patron-frozen DTO field names, routes, regeneration commands, and named test files are acceptance boundaries from the ticket/brief; not a blanket PASS.
- [x] Focused on user value and business needs
- [ ] Written exclusively for non-technical stakeholders — API/contract and gate evidence also appear where Done when requires them.
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [ ] Success criteria entirely technology-agnostic — regenerated types, `npm run check:api`, and named test files are deliberate Done-when boundaries.
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [ ] No implementation details anywhere — authorized API/UI/test/gate boundaries intentionally appear; user outcomes remain explicit.

## User-Visible Outcome

- [x] "What the User Will See" names each visible change by page or component
- [x] Each visible change has a SMOKE-### check in user terms (SMOKE-001 For You e2e + home-library reviewer; SMOKE-002 Related component coverage)
- [x] What each visible change needs to show up in a plain `docker compose up` is listed (config, data, services)

## Notes

- Content Quality exceptions are explicitly unchecked and annotated; DTO names, regeneration order, and named test paths appear only where ticket/brief froze them as acceptance boundaries and are expanded in `plan.md` / `contracts/`.
- Validation iteration 1 (2026-11-19): checklist ready for drafting handoff to Keel analyze. No PLAN FROZEN or Gate 1 claimed.
- No ASSUMPTIONS.md — Patron Q1–Q5 cite ticket text; no separate taste `[assumed]` items invented by Quill.
