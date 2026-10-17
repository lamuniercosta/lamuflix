# Specification Quality Checklist: DEV-352 Explicit watched and not-interested marks

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-10-17
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details beyond what the brief fixes — routes, status codes, column names, token pairs, the migration proof, and the contract chain appear only where `brief.md` Q1–Q12 and `CONCLUSIONS.md` already fix them (same convention as DEV-411)
- [x] Focused on user value and business needs (three stories: record the marks, see them on existing reads, keep existing movies safe across upgrade and downgrade)
- [x] Written for non-technical stakeholders (acceptance scenarios are Given/When/Then over observable outcomes)
- [x] All mandatory sections completed (User Scenarios & Testing, Requirements, Success Criteria, Assumptions)

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain — Q9 and Q10 closed the two drafting questions. Quill added no further gap fill. Two structural owner checkboxes stay unresolved in `spec.md`, `plan.md`, `tasks.md`, and `DRAFTING_RECEIPT.md`. They are user approvals, not markers this draft fills. Gate 1 stays closed
- [x] Requirements are testable and unambiguous (FR-001–FR-014 restate Q1–Q12). The FluentValidation omission and the additive migration are specified as pending owner approval, not as compliant exceptions
- [x] Success criteria are measurable (SC-001–SC-007 are pass/fail outcomes from the brief)
- [x] Success criteria stay free of invented latency or volume targets — the brief sets no performance number, so none is stated
- [x] All acceptance scenarios are defined (US1 nine, US2 four, US3 four)
- [x] Edge cases are identified (both marks true, all eight combinations, repeat conflict with the Q9 token pairs, id classes with zero saves, preexisting rows, Q10 upgrade and downgrade, and the stated exclusions). The two owner checkboxes are unresolved approvals, not unspecified edge cases
- [x] Scope is clearly bounded (FR-004, FR-009, FR-010, FR-012, and the edge cases match brief Q1, Q3, Q5, Q6, and Q8). Q11's two departures stay outside approved scope until the user checks them
- [x] Dependencies and assumptions identified (existing watchlist slice, existing error mapping, existing contract chain, Q9 `[assumed]` Interested wording, and the two unresolved owner checkboxes)

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria (FR-001–FR-014 map to US1–US3)
- [x] User scenarios cover primary flows (mark and unmark, read back, upgrade and downgrade)
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation detail leaks beyond the brief's own decisions, including the Q9–Q11 supplements
- [ ] Both structural owner approvals are resolved by the user. Unresolved after this assessment: the FluentValidation omission and the additive migration without a constitution amendment or ADR. This drafting round does not check this box. Gate 1 stays closed. Plan challenge freeze is conditional on these owner decisions for execution. Implementation is not authorized

## Notes

- Reassessed 2026-10-17 after all-three-axis plan challenge adjudication and Patron Q12, against corrected spec.md, plan.md, tasks.md and brief.md. The earlier drafting receipt is historical and was outside this adjudication read set.
- Drafting questions 1 and 2 are closed by Q9 and Q10. The historical zero-checkbox grill statement is superseded by Q11. The two owner checkboxes above stay unchecked.
- Research, data-model, contracts, and quickstart side files are not part of this draft. `plan.md` records why.
- All 16 plan-challenge findings are adjudicated in plan.md. Conditional plan freeze is recorded there; both Q11 owner decisions remain unresolved. No Gate 1, implementation authorization or executed delivery-gate success is claimed.
