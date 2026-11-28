# Specification Quality Checklist: Subtitle picker grouped search and batch download

**Purpose**: Validate specification completeness and quality before proceeding to planning (documentation readiness — not runtime PASS)
**Created**: 2026-11-28
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No unconstrained implementation details beyond (a) frozen Q1/Q2/Q4 Patron tokens and (b) Quill-drafted wire names needed for interoperability
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders aside from those frozen/drafted contract tokens
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria stay outcome-focused aside from frozen/drafted contract tokens required by brief/contract
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness (documentation)

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [ ] Runtime success against Success Criteria — tracked separately at Gate 1 / Build; this checklist does not claim runtime PASS
- [x] Spec documentation is ready for planning/review given closed grill Q1–Q7

## User-Visible Outcome

- [x] "What the User Will See" names each visible change by page or component, or says "Nothing visible" with a reason
- [x] Each visible change has a SMOKE-### check in user terms that can run against the Compose stack
- [x] What each visible change needs to show up in a plain `docker compose up` is listed (config, data, services)

## Notes

- **Frozen Patron decisions (Q1/Q2/Q4/Q8)**: opt-in `Accept: text/event-stream`; named C# SSE payload DTOs in OpenAPI; additive `preferredLanguageCodes` and non-null `hashMatch`; terminal settled-provider count; pre-stream JSON errors; required reused `downloadAvailability` on `providerSettled`. Spec may cite these because Patron already ruled them.
- **Quill-drafted interoperability names (not Patron tokens)**: SSE wire event strings `providerSettled` and `searchCompleted`, plus Quill DTO type names authorized by brief Approach #3. Distinguish these from the frozen Q1/Q2/Q4/Q8 decisions above.
- Checklist validation: documentation-readiness items for drafting/Gate 1 review. Grill Q1–Q8 closed; no inventable `[NEEDS CLARIFICATION]` markers. Clarify session recorded in `spec.md`. Runtime success evidence remains pending and is tracked separately.
- SMOKE-001 covers the visible picker batch path against Compose with two providers and DEV-553 track visibility.
- Analyze fix / plan-challenge repair rounds (2026-11-28): validation claims corrected; frozen vs drafted attribution clarified; documentation readiness separated from runtime PASS; final Phase 2 repair at `e9175fcb` applies dispositions 1–9 including Patron Q8.
