# Requirements Checklist: Core Use-Case Handlers (DEV-299)

**Purpose**: Validate spec, plan and tasks quality before Gate 1.

**Created**: 2026-09-30

## Content Quality

- [x] No handler behaviour is invented beyond the brief's Handler contract table and CONCLUSIONS Q1-Q12
- [x] Every requirement traces to a ticket line, a ruling or the constitution
- [x] Scope is fenced; wiring prerequisites are listed, not delivered (FR-018)

## Requirement Completeness

- [x] Ticket ACs 1-2 appear verbatim in SC-001/SC-002 intent
- [x] Every handler has a story, an FR and test tasks
- [x] Every exception path (NotFound, InvalidTransition, ArgumentException, queue failure) has an acceptance scenario
- [x] Edge cases named (out-of-range Attempt, deleted-between-decisions, duplicate path, cancellation)
- [ ] D1 answered (owner)
- [ ] D2 answered (owner)
- [ ] D3 answered (owner; wiring deferral, constitution departure)
- [ ] Q13 ruled (Patron)

## Feature Readiness

- [x] Blocked tasks marked `[BLOCKED: D1|D2|Q13]` and none dropped
- [x] Save-before-enqueue ordering is testable
- [x] Task ordering matches the brief; T021 probe precedes any arch-test edit
- [x] Frozen scope matches `git diff --stat` gate (FR-022)

## Notes

- Items marked incomplete are the three open decisions and keep Gate 1 closed.
