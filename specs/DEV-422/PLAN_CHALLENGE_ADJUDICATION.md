# DEV-422 — Phase 2 plan-challenge adjudication

Date: 2026-10-26. Adjudicator: Keel. Worktree: `F:/Dev/LamuFlix.worktrees/feature-422-spec`; branch: `feature/422-spec`.

## Authority and scope

All three reports read: notes `findings-DEV-422-Sentry`, `findings-DEV-422-Ledger`, and `findings-DEV-422-Compass`. Repository facts come from `recon-DEV-422`; no independent code searches or measurements. Rulings: `CONCLUSIONS.md`, Phase 2 Challenge Rulings. Binding decisions: `brief.md`, Phase 2 plan-challenge decisions. Drafting changes belong to Quill. No production writes, gates, tracker writes, publication, or merge authorization.

The actual reports contain 23 entries: 7 Medium, 10 Low, 6 Informational. Sentry has 10 entries, despite its task-note receipt summarizing 8. Every entry is disposed below. Ledger uses numbered findings 1–7; M1–M4/L5–L7 below preserve those numbers. Compass likewise uses findings 1–6.

## Dispositions

| Axis / ID | Severity | Disposition and basis | Required documentation/proof |
|---|---|---|---|
| Sentry M1 | Medium | Accept. Existing FR-007 freshness cannot be defeated by coalescing. | Brief item 1; T006/T007 explicit older-refetch/event/fresh-convergence order, T008/T009 implementation constraint. |
| Sentry M2 | Medium | Accept, consolidated with Ledger M2 and Compass 1. | Brief item 2; instance-owned callback subscriptions, root→App→details props, event/recovery publishes independent of browse presence or status. |
| Sentry L1 | Low | Defer added debounce/storm controls under Q6 closing bar. | Existing coalescing and freshness retained; no new timing policy. Noted, no ticket per Patron. |
| Sentry L2 | Low | Accepted degraded behavior: prolonged disconnection can leave status stale until mutation invalidation or hub recovery. | No polling fallback or new visual indicator in scope. Noted, no ticket per Patron. |
| Sentry L3 | Low | Clarify existing event-patch behavior; no rollback machinery. | Brief item 6; failed refetch retains patch until subsequent event/recovery refetch. |
| Sentry L4 | Low | Accept arity/integer/status validation gap with Ledger M3. Reject uncontracted positive-only id restriction. | Existing Zod; explicit malformed matrix in T006. Upstream contract specifies int, not a positive-only client rule. |
| Sentry L5 | Low | Acknowledge upstream anonymous minimal broadcast audience. | No unilateral auth change; upstream contract/drift pickup remains mandatory. Noted, no ticket per Patron. |
| Sentry I1 | Info | Acknowledge existing pickup obligation. | Exact compatible published 10.0.x pin, upstream protocol compatibility; no added MessagePack dependency. |
| Sentry I2 | Info | Already covered. | Partial `['browseMovies']` prefix across variants, not exact-key invalidation; T006/T008. |
| Sentry I3 | Info | Out-of-scope HMR enhancement, no fix. | StrictMode/session lifecycle acceptance remains mandatory. |
| Ledger M1 (1) | Medium | Accept hook-instance isolation and injected factory. | Brief item 2; T003/T004 per-instance ownership, zero fake live connections/timers before restoring timers. |
| Ledger M2 (2) | Medium | Accept bounded seam; consolidate bridge decision. | No new layer/context/provider/store/module/global registry; default factory private, narrow subscription surface allowed for existing-file wiring. |
| Ledger M3 (3) | Medium | Accept existing-Zod mechanism. | Brief item 3; exactly one argument, non-null object, finite integer id, exact four status names; no new dependency/duplicate DTO. |
| Ledger M4 (4) | Medium | Accept hygiene with Patron-qualified audit disposition. | T002 exact-save/package+lock root; audit and disposition of introduced findings before clearance, no blanket failure for unrelated historical advisories; T013 build-output delta/no new tooling or budget, browser runtime imports. |
| Ledger L5 (5) | Low | Defer same-reference/redundant-write optimization under closing bar. | Existing absent-row/no-fabrication and multi-variant acceptance remains mandatory. Noted, no ticket per Patron. |
| Ledger L6 (6) | Low | Defer exported retry constants as implementation detail. | Ratified retry schedule/fake-timer proofs unchanged. Noted, no ticket per Patron. |
| Ledger L7 (7) | Low | Clarify existing fake choice, no dependency change. | T003 hand-written fake, no second mocking library. |
| Compass 1 | Medium | Accept and consolidate bridge seam finding. | Brief item 2; T009 stays in existing movie-details files with root-instance subscription passed through existing shell props. |
| Compass 2 | Low | Accept concrete FR-009/US3 coverage omission. | T007 explicit successful initial-start and reconnect without event, including absent-browse id. |
| Compass 3 | Low | Accept concrete FR-011/US1 coverage wording. | T010 explicit `aria-live="polite"` assertion after polling rewrite. |
| Compass 4 | Info | Acknowledge T003 parallel-label observation; no change required. | Existing phase compile-red/green checkpoint applies. |
| Compass 5 | Info | Already covered; emphasize placeholder coupling at delivery review. | T010/T012 preserve placeholder/row-id reconciliation while removing interval triggers. |
| Compass 6 | Info | Already covered by full Vitest suite. | T013 catches collateral failures; no speculative test-file scope expansion. |

## Resolution record

Correction round 1 of maximum 2 sent to Quill as one numbered six-item list, derived from the recorded Patron ruling. Quill returned a numbered receipt in DRAFTING_RECEIPT.md. Keel independently read the returned FR-001–FR-017, plan Structure/Design/ordering, and T002–T015 descriptions against brief items 1–6. Verified:

- Freshness constraints and explicit R1/E2 ordering proofs in T006–T009 for both root and mounted details.
- Instance-owned subscription/factory state, existing-file prop wiring and cleanup in FR-004/005/008 and T003–T005/T009; zero fake connections/timers teardown in T003.
- Existing Zod mechanism and malformed arity/id/status matrix in FR-007 and T006/T008.
- Exact-save/package-lock pin, qualified audit disposition, build-output delta and browser import checks in FR-012/015 and T002/T013.
- No-event initial-start and reconnect details reconciliation in T007, concrete polite-live-region assertion in T010.
- Patch retention after refetch failure in FR-007 and T006/T008; no rollback machinery.

All accepted Medium findings are resolved in documentation; accepted Low clarifications are incorporated, remaining Low/Info entries are disposed as above. FR/SC identities, production/test file boundaries, owner checkbox, property opt-out, and Phase B STOP prerequisites are preserved. Round 1 closes the plan challenge; no round 2 is needed. No live implementation claim or gate result is made. The initial git diff read showed no tracked diff for the three draft paths, so verification used actual working document contents rather than treating an empty diff as evidence of no changes; no HEAD-pinned artifact claim is made.

**Final verdict: adjudication complete; plan frozen for owner Gate 1 review**, per brief Phase 2 closure. Ready for spec-PR preparation with the unanswered owner checkbox. Gate 1 is not approved.

## Gate 1 and Phase B

Owner checkbox remains unanswered:

- [ ] **blocked: structural — replace ticket-mandated hand-typed hub payload types with the generated DEV-330 schema type?**

A plan-challenge disposition cannot answer this checkbox. Gate 1 stays closed until owner resolution and the separate spec PR process. Phase B additionally waits for landed approved upstream DEV-330 constitution disposition/amendment, landed hub implementation and generated schema chain, and contract-drift verification against main. Frozen PR #264 is planning evidence, not a landed implementation. Property tests retain the recorded opt-out, never PASS; the Vitest matrix is mandatory.
