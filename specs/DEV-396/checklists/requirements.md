# Specification Quality Checklist: DEV-396 Complete DEV-307 Observability and Run the DEV-308 Retrospective

**Purpose**: Validate specification completeness and quality before analyze and plan challenge
**Created**: 2026-10-05
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No unnecessary implementation detail beyond the frozen ticket scope (named files live in `plan.md`, not `spec.md`; OpenTelemetry and health-check terms are the ticket's own subject)
- [x] Focused on operator, maintainer and owner value
- [x] All mandatory sections completed
- [x] Spec carries the owner decision without answering it

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain (OD-1 is an inherited owner checkbox, not a drafting gap)
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] All acceptance scenarios are defined, including the BLOCKED ones
- [x] Edge cases are identified (harness dirt, NU1010, unreachable collector, gate exit 2, already-resolved findings)
- [x] Scope is clearly bounded (FR-017, FR-018; frozen to Q1-Q5)
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] Every functional requirement maps to a story and a success criterion (checked against the explicit FR to story to SC crosswalk in `spec.md`: FR-001..FR-018 each name a US and an SC; FR-017 is the cross-cutting boundary checked against SC-010 and every story; FR-018 is the conditional US5/OD-1 contract exclusion checked against SC-006, not an extra deliverable)
- [x] User scenarios cover primary flows (US1-US7)
- [x] Every inherited DEV-307 task ID maps to evidence, BLOCKED or deferral (`tasks.md` Inheritance Map)

## Clarify Action (L)

Coverage scan against `brief.md`, `CONCLUSIONS.md` Q1-Q6 and recon; no question needed an owner or Patron answer in this pass.

| Area | Result | Source |
|------|--------|--------|
| Q1 readiness BLOCKED, verbatim checkbox | Clear; verbatim check against `specs/DEV-307/brief.md:76` run, True | spec OD-1 |
| Q2 T010 placement | Clear | FR-009, US4 |
| Q3 retrospective boundary and SHAs | Clear; full SHAs in US6, T005, T006 | FR-011 |
| Q4 harness restoration | Clear; precedes gates | T003, SC-009 |
| Q5 order, no dependency or schema | Clear | plan Phase Order |
| Q6 loop caps and publication | Clear | FR-013, FR-016 |
| Q2 answer or decline | Open owner item; not a drafting gap | OD-1 |
| Mutation eligibility of the delivery diff | Deliberately unspecified; real verdict recorded by Gauge | FR-014 |

## Checklist Action (L): Requirements Quality Probes

- [x] Is every gate-exit outcome classified (pass, fail, SKIPPED, SKIP, NOT APPLICABLE, Could not run)? Yes (edge cases, FR-014).
- [x] Is "composition" vs "delivery" wording fixed? Yes (US1, plan test expectations).
- [x] Is the owner-answer sequencing unambiguous (T033-T036, T041)? Yes.
- [x] Is anything ticked by inference? No (T020A, OD-1).
- [x] Are historical and delivery receipts kept separate? Yes (FR-011, T014).

## Notes

- Items for Keel's read-only analyze: the brief says the L ADR step follows task-pipeline Phase 2; this draft does not number an ADR.
- `tasks.md` T001 inherits three DEV-307 setup IDs in one task; Keel may ask to split.
