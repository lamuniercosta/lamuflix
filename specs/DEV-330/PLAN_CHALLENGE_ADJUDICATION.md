# DEV-330 — Plan challenge adjudication

Worktree: `F:/Dev/LamuFlix.worktrees/feature-330-spec`; branch verified `feature/330-spec`. Phase A documentation only. Sources: all three `findings-DEV-330-{Sentry,Ledger,Compass}` notes, `recon-DEV-330` and `recon-DEV-330-2`, brief Q1–Q9 plus operative PC1/PC2, and complete Patron exchanges in `CONCLUSIONS.md`.

Round: 1 adjudication and one consolidated Quill fix list; cap two. All 18 findings adjudicated below. Quill Round 1 fix receipt at DRAFTING_RECEIPT.md:38-71 is complete and independently read back. All accepted document fixes resolved. Plan challenge adjudication COMPLETE; plan frozen for owner Gate 1 review with PC1/PC2 incorporated. Zero unresolved blocking challenge findings. Gate 1 remains CLOSED pending the existing owner checkbox.

| Axis / finding | Disposition | Basis and required resolution |
|---|---|---|
| Sentry R1 Medium | Accepted clarification | PC1: `CancellationToken.None` at repository post-commit call; T011/T013/T021 and ADR name the source. |
| Sentry R2 Medium | Accepted clarification | PC1: per-movie repository catch/log in addition to adapter isolation; throwing-double multi-movie proof leaves commit successful and attempts every remaining movie. |
| Sentry R3 Medium | Accepted risk; no new mechanism | PC1/Q3: sequential awaited one-shot attempt has no application-level timeout/fan-out bound; possible unbounded latency explicit. Not a silently deferred fix. |
| Sentry R4 Medium | Accepted delivery limitation | PC1/Q3/ADR-0022: start/failure logs with id/status, no durable crash-window detection, T036 states crash loss and DEV-422 reconnect/refetch. No guaranteed receipt. |
| Sentry R5 Low | Recorded non-blocking | Cross-commit event ordering unguaranteed; DEV-422 convergence; no new ordering mechanism. |
| Sentry R6 Low | Recorded; existing scope | Q4/Q7: single-user Clients.All, no auth, minimal payload, credential-free CORS. Future audience/auth work outside ticket. |
| Sentry R7 Low | No gap | Q7/recon section 5: missing allow-origin on rejected browser negotiation, no invented authorization boundary or raw-WebSocket protection claim. |
| Sentry R8 Low | Already covered | Q2/T011: assigned insert id read after persistence assignment, retained until successful commit; ordinary inserted-Pending proof retained. |
| Sentry R9 Low | Already covered | Q4/T025/T030: generation succeeds, required DTO schema/enum and exact REST operations preserved. |
| Sentry R10 Info | Already covered; constraint retained | Q2/Q5/T011/T013: no ghost/failed-commit events, persisted-before/final-after only; save-local transition candidates, never shared/static. |
| Ledger M1 Medium | Accepted clarification | PC1 supersedes optional fallback permission: required notifier constructor dependency, explicit fixture doubles, named live-host real-adapter DI proof. |
| Ledger M2 Medium | Follow-up non-blocking; noted, no ticket | PC2/Q9: proposed collaborator is speculative abstraction without measured implementation evidence. No helper/file expansion now; T016/T031 gates mandatory. Measured need requires bounded ruling, not gate suppression. |
| Ledger L1 Low | Rationale clarification | PC2/recon-2 Q3: existing schema transformer handles discovered types; hub DTO needs document-level insertion. Research/plan explain registration and keep exact operations tests. |
| Ledger L2 Low | Not adopted | PC2: retain frozen flat Api root paths; no taste change or ASSUMPTIONS entry. |
| Ledger L3 Low | Accepted task clarification | PC2: T001 explicitly blocks T006/T007; exact touched-file `-Files` scope receipts for incremental gates; keep stable IDs and phase order. |
| Compass L1 Low | Accepted task clarification | PC2/Q7: T018 proves retained DevSpa facets; exposed Location on appropriate actual response, not a preflight-only requirement. |
| Compass L2 Low | Accepted task clarification | PC2/Q2/Q5: T011 explicitly proves caller-independent direct repository path without unused-handler registration. |
| Compass L3 Low | Recorded cosmetic; stable ID retained | PC2: T012 remains in Phase 4 with explicit dependency ordering; no renumber churn. |

## Gate boundary

The Q1 constitution v1.5.0 / ADR-0029 owner checkbox remains unchecked. Patron design rulings do not answer it. Phase A can become ready for owner Gate 1 review after reconciliation, while Gate 1 itself remains closed pending user approval. No production implementation, tests, analyzers, mutation, generated-file updates, code-review clearance, hosted checks, merge bar or merge are claimed here.

## Read-only cross-artifact reconciliation

The Spec Kit prerequisite command resolved FEATURE_DIR to this feature. `.specify/extensions.yml` has no before_analyze or after_analyze hook. Constitution v1.4.0 remains unchanged; the existing Q1 departure is explicitly proposed and blocked from implementation pending owner approval, not waived by this analysis.

| Requirement | Task coverage |
|---|---|
| FR-001 | T017, T020, T022 |
| FR-002 | T012, T017, T019, T021 |
| FR-003 | T009, T019, T021 |
| FR-004 | T009 |
| FR-005 | T011, T013, T015 |
| FR-006 | T011, T015 |
| FR-007 | T012, T021, T023 |
| FR-008 | T011, T013, T015 |
| FR-009 | T014, T022 |
| FR-010 | T018, T022, T023 |
| FR-011 | T025–T030 |
| FR-012 | T002, T010 |
| FR-013 | T001, T003, T006–T008 |
| FR-014 | T035 plus explicit frozen exclusions in story tasks |
| SC-001 | T011, T015, T017, T023 |
| SC-002 | T011, T015 |
| SC-003 | T018, T023 |
| SC-004 | T025–T030 |
| SC-005 | T011–T015, T021, T023 |
| SC-006 | T016, T024, T030–T036 |

T004 feature/worktree identity and T005 test baseline are supporting setup under SC-006; no unmapped tasks. Total: 14 FR + 6 SC, 36 stable task IDs; 100% requirement mapping. No newly unresolved ambiguity, conflicting requirement, or blocking duplication after PC1/PC2. Owner Gate 1 is an explicit unresolved approval boundary and is not counted as missing task coverage.

Read-back evidence: `spec.md:84-109,121-126`, `plan.md:109-125`, `research.md:25-29,55-69`, `tasks.md:27,41-47,61-68,82-93,107-130,140-157`, ADR-0029 post-commit/constructor paragraphs, brief operative PC1/PC2. Required constructor and per-movie failure policy agree. CORS assertions distinguish preflight from actual-response exposed headers. Stable T012 follows Phase 4 dependencies. No speculative collaborator or new directory is introduced. No tests or production gates were run for this document analysis.

