# DEV-544 Phase 2 plan adjudication

**PLAN FROZEN** · 2026-11-19 · Keel · `feature/544-spec`

All 30 findings have a disposition. Accepted actions are reflected in `brief.md`, `plan.md` and `tasks.md`: 22 accepted, seven accepted in part and one declined. Patron's cited test-boundary and Q1/Q3/Q5 rulings are in `CONCLUSIONS.md`. This closes Phase 2 step 6. Gate 1, implementation and runtime acceptance remain pending. The plan's two-round cap and closing bar through Low still apply.

Sources are the three named findings notes in LamuFlix notes, `DEV-544`, `recon-DEV-544`, the ticket, `brief.md`, `CONCLUSIONS.md`, `spec.md`, `plan.md`, `tasks.md` and the requirements checklist. Finding IDs and severities follow the individual report headings. Task IDs are kept stable for traceability to the analyze receipts.

## Sentry · Risk · 12 findings

| Finding | Severity | Disposition | Basis and closure |
|---|---|---|---|
| H1 — Tier ordering through MMR and grouping | High | Accept | Wanted 4/6 and Patron Q1 refinement. Run MMR and grouping inside each tier with A receiving the first slots and B receiving the remainder. T007/T010 prove the grouped rail rule across budgets and T011 preserves standalone MMR tie and prefix tests. |
| H2 — Exact normalized value and scorer fidelity | High | Accept | Wanted 4 and Patron Q5 refinement. Require exact 1.0 on a present emitted term. T001 confirms the merged scorer and T007 drives exact and just-below Themes cases with its real ceiling. |
| H3 — Evidence for whole-library scaling | High | Accept in part | Wanted 9/10, FR-010 and Q4. T021 records the required warm 450-movie run and the raw vector payload basis, plus cold, concurrency and measured peak observations where supported. Route a measurement follow-up when the >450 trigger is observed. The ticket sets one warm SLA and treats >450 as a review trigger. |
| M1 — Selected-vector authority and guard parity | Medium | Accept | Q2 and Patron Q3 refinement. The loaded selected entry is authoritative when present and the existing selected-only seam covers absence. The retained method keeps its guard. T007 tests both fallback outcomes and T015 tests both guards on the same missing-feature-row fixture. |
| M2 — Scorer behavior with missing feature row | Medium | Accept | Wanted 5 and FR-009. T015 sends the missing-row director-only candidate through the real scorer and requires finite scoring, Plot zero, Director 1 and admission. T006 separately proves the EF projection. |
| M3 — Cancellation and concurrency | Medium | Accept in part | Wanted 1/9/10. T009 and T007 add in-process cancellation and regression coverage. T021 records supported concurrent observations. Concurrent requests still load request-local vectors and the warm single-request SLA is the acceptance bar. |
| M4 — Options validation and diagnostics | Medium | Accept | Q5 and constitution V/VI. T008 pins binding, validation, case and whitespace behavior. T007 exercises the real options path. T021 records effective options and tier counts in sanitized evidence. |
| M5 — Undefined compile-boundary escape hatch | Medium | Accept | T008 lands first. T003–T005, T009 and T007's reference adaptations share one landing boundary. T006/T010 require the compiling result. |
| M6 — Before-snapshot identity | Medium | Accept | FR-015. T002 records the artifact path, SHA-256, fixture and source revision, and requires verification at the first production edit. T021 checks the same receipt before comparison. |
| L1 — Library-wide state and a hypothetical multi-profile path | Low | Decline | Wanted 1 requires the state fields. The finding identifies no current authorization defect and the ticket has no profile trust-boundary change. A new single-profile assumption would assert more than the ticket and recon establish. |
| L2 — Standing failure for odd judged pairs | Low | Accept in part | SC-001 and Q5. T021 keeps both named pairs visibly recorded as bad judgments and routes remediation through DEV-518. A permanent failing gate would make the separate DEV-518 debt a DEV-544 acceptance condition. |
| L3 — Stable OpenAPI comparison | Low | Accept | Wanted 7 and SC-006. T001/T013 identify descriptions by operation and `take` parameter and validate the textual diff. |

## Ledger · Standards · 12 findings

| Finding | Severity | Disposition | Basis and closure |
|---|---|---|---|
| L-1 — Option description versus comment rule | High | Accept | Wanted 7, FR-011 and AGENTS.md. T012 documents the option in validation-message text and T013 documents both OpenAPI operations. |
| L-2 — Omitted substitute and options validation file | High | Accept | Patron approved the RelatedHarness adaptation in `ReviewRankingHandlerTests.cs` and located option validation tests in `GetRecommendationsQueryHandlerTests.cs`. T001 searches compile references and T007 includes both. |
| L-3 — Retained port method behavior | Medium | Accept | Wanted 2/3 and Q3 retain both methods. T005 defines direct delegation with the retained selected-vector guard and selected-relative derivation. |
| L-4 — Cosine on a shared record | Medium | Accept | Wanted 1 and Patron Q3 refinement. Keep the shared library entry selected-agnostic. T003–T005/T009 derive cosine only on selected-relative paths. |
| L-5 — Options type, scorer mapping and exact value | Medium | Accept in part | Q5 accepts canonical string tokens with ordinal case-insensitive matching and rejects invalid values through options validation. T001/T007 verify mapping to actual emitted terms. Patron ruled on exact normalized 1.0. The proposed enum and greater-than-or-equal comparison are outside that ruling. |
| L-6 — Concrete A-first algorithm and prefix | Medium | Accept in part | Patron Q1 refinement selects tier-local MMR and grouping with remaining slots for B. T007/T010 cover varying takes and adversarial grouping. Standalone MMR keeps the prefix contract in T011. The grouped rail keeps its ticket ordering contract. |
| L-7 — Handler complexity and oversized test task | Medium | Accept | T009 calls for a pure method within the existing slice and identifies the separate responsibilities. T007 divides guard/cancellation, classification/options and ordering/input-reuse proofs. T019 applies the refactor gate. |
| L-8 — Dependency and compile sequencing | Medium | Accept in part | T008 precedes T007 and the vertical compile boundary. T006 follows the compiling slice. The explicit dependencies and phase order govern execution. Stable IDs preserve references in prior review receipts and the spec. |
| L-9 — Embedding row versus scorer inputs | Medium | Accept | Wanted 1/5 and Q2. T001 confirms the persistence projection, T003 specifies metadata/signals and optional embedding separately, and T006/T015 verify those two aspects. |
| L-10 — Provisional port name | Low | Accept | Patron's Q3 refinement fixes `GetLibraryFeaturesAsync` on the existing port. The draft's provisional name is ratified. |
| L-11 — Fragile anchors and duplicated checklists | Low | Accept | T001 resolves names on pickup. T006 uses named EF cases and SQL behavior, T011 uses test behavior and T013 uses operation/parameter identity. T019 and T021 carry the detailed gate and runtime checklists. |
| L-12 — Glossary scope | Low | Accept | Patron's Q1/Q5 refinement and constitution VIII. Tier A/B are spec-local partitions of the ticket's strong match concept. The plan records the ruling. |

## Compass · Spec · 6 findings

| Finding | Severity | Disposition | Basis and closure |
|---|---|---|---|
| F1 — Selected-agnostic signature, cosine and guard | High | Accept in part | Wanted 1–3/6, FR-001/003 and Patron Q3 refinement. T003–T005/T009 fix signature, payload and selected-relative responsibilities. On absence from the enriched list preserve the existing selected-only fallback, including a usable vector. FR-001's “where applicable” qualifies the selected-relative paths. |
| F2 — Tier enforcement | Medium | Accept | FR-007/008 and Patron Q1 refinement. T009 runs existing selectors and grouping per tier with an A-first budget. T007/T010 prove final order and T011 retains standalone tests. |
| F3 — Duplicate candidate embedding reads | Medium | Accept | Wanted 1/6/9 and FR-003. T001 confirms the liked-only fetch seam. T009 passes no candidate IDs to the profile-input fetch and scores from library vectors. T007/T015 verify input reuse as well as fixed round trips. |
| F4 — Configuration vocabulary | Medium | Accept | FR-005 and Patron Q5 refinement. T008 uses canonical string tokens, ordinal case-insensitive matching and no trim. T001 confirms exact emitted DEV-518 names and T007 checks mapping and validation. |
| F5 — Absent breakdown term | Low | Accept | FR-005 and Patron Q5 refinement require a present term at exactly 1.0. T007 adds an absent-term test. |
| F6 — Web smoke file authorization | Low | Accept | Ticket Goal, FR-012 and Patron's charter §2.3 item 6 ruling authorize the existing `RelatedMovies.test.tsx` rail smoke only. Brief Files, plan and T014 record that boundary. |

## Freeze conditions and remaining evidence

- Gate 1 is ordinary owner spec-PR approval and verified merge (T000). Patron ruled on the in-scope questions. There is no structural owner checkbox.
- T001 verifies the actual merged DEV-518 scorer and the named seams on the pickup base. Absence of the required scorer contract blocks pickup. The plan makes no claim about the eventual picked base.
- T002 protects the before snapshot. T019–T021 cover automated gates, implementation review and measured runtime/quality evidence. The 450-movie warm bar and frozen defaults follow the ticket. No runtime or implementation verification has been performed during this adjudication.
