# Requirements Quality Checklist: DEV-330 Enrichment status SignalR push

**Purpose**: Unit tests for English — validate that Phase A requirements (spec/plan/tasks) are complete, clear, consistent, and measurable before Gate 1 / plan challenge.
**Created**: 2026-10-26
**Feature**: [spec.md](../spec.md), [plan.md](../plan.md), [brief.md](../brief.md)

**Depth**: Standard | **Audience**: Author + Keel analyze / plan challenge | **Focus**: commit semantics, public hub contract, Gate 1 constitution alignment, CORS, contract chain

## Requirement Completeness

- [ ] CHK001 Are ticket AC1–AC3 each traced to numbered FRs and SCs? [Completeness, Spec §Requirements / §Success Criteria]
- [ ] CHK002 Are insert-Pending, actual transition, no-op Pending, failed save, and duplicate-IMDb recovery emission rules all stated? [Completeness, Spec §US1 / Edge Cases, Brief Q2]
- [ ] CHK003 Are post-commit send-failure isolation (repository per-movie + adapter), `CancellationToken.None` call-site policy, and post-commit cancellation rules documented? [Completeness, Spec FR-007/FR-008, Brief Q3/PC1]
- [ ] CHK004 Is the owner Gate 1 constitution/ADR checkbox present verbatim and marked unchecked? [Completeness, Spec Owner question, Brief Q1]
- [ ] CHK005 Are out-of-scope items (DEV-422 client, backplane, replay, outbox, schema, auth, LocalPlay) explicitly excluded? [Completeness, Spec FR-014, Brief Q9]

## Requirement Clarity

- [ ] CHK006 Is “committed change” defined as persisted-before vs committed-after (not intermediate in-memory states)? [Clarity, Spec Edge Cases / FR-005, Brief Q2]
- [ ] CHK007 Is the wire payload limited to exact fields `id` and `enrichmentStatus` with the four string names? [Clarity, Spec FR-002, contracts/enrichment-hub.md]
- [ ] CHK008 Is the notification boundary “enclosing persistence transaction commit” distinguished from intermediate SaveChanges? [Clarity, Spec Edge Cases, Brief Q5]
- [ ] CHK009 Is AC1 framed as one broadcast invocation (not guaranteed client receipt)? [Clarity, Spec US1 / FR-007, Brief Q3]

## Requirement Consistency

- [ ] CHK010 Do spec, plan, ADR-0029, and constitution-amendment proposal agree on port signature `NotifyAsync(MovieId, EnrichmentStatus, CancellationToken)`? [Consistency]
- [ ] CHK011 Do CORS requirements restate existing DevSpa (5173, no credentials) without inventing a second policy? [Consistency, Spec FR-010, Brief Q7]
- [ ] CHK012 Does the OpenAPI path forbid new REST operations while requiring components.schemas exposure? [Consistency, Spec FR-011 / US3, Brief Q4]
- [ ] CHK013 Does Phase B task order match brief Q8 steps (1) constitution/port/CPM → (2) persistence → (3) hub/adapter → (4) OpenAPI/TS → (5) gates? [Consistency, plan.md / tasks.md]

## Acceptance Criteria Quality

- [ ] CHK014 Can SC-001–SC-003 be objectively observed from tests without subjective UX language? [Measurability, Spec §Success Criteria]
- [ ] CHK015 Are independent tests named per user story so each story is demonstrable alone? [Acceptance Criteria, Spec US1–US3]
- [ ] CHK016 Is gate reporting required to distinguish SKIPPED / SKIP / N/A / Could not run from PASS? [Acceptance Criteria, Spec SC-006]

## Scenario & Edge Coverage

- [ ] CHK017 Are multi-movie single-save and multi-mutation-before-save cases covered in requirements? [Coverage, Spec Edge Cases]
- [ ] CHK018 Are unused-handler non-registration and repository-wide coverage both stated without conflict? [Coverage, Spec Edge Cases, Brief Q2]
- [ ] CHK019 Is `IEnrichmentStatusNotifier` required on the repository constructor with explicit fixture doubles, no optional/no-op production fallback, and a named live-host real-adapter resolution proof? [Edge Case, Spec FR-009, PC1 / Ledger M1]
- [ ] CHK024 Does research/plan distinguish discovered-type `EnrichmentStatusSchemaTransformer` from document-level `EnrichmentHubSchemaTransformer` insertion (recon-DEV-330-2:30-33) while preserving required-fields/enum/exact-operations proofs? [Consistency, Ledger L1 / PC2]
- [ ] CHK025 Do T018 assertions cover retained DevSpa origin/method/header/no-credentials and exposed `Location` on the appropriate actual CORS response (not preflight-only)? [Coverage, Spec US2 / FR-010, Compass L1 / PC2]
- [ ] CHK026 Are T006/T007 gated on recorded T001 Gate 1 evidence, and do incremental gates require exact touched-file `-Files` receipts while keeping stable T012? [Consistency, tasks.md, Ledger L3 / Compass L3 / PC2]
- [ ] CHK027 Does T036 name crash loss between commit and send, no guaranteed receipt, and DEV-422 reconnect/refetch without claiming durable crash-window detection? [Coverage, Spec Edge Cases / FR-007, Sentry R4 / PC1]
- [ ] CHK020 Are hub negotiate preflight, rejected origin, and live delivery all required in requirements (not only “CORS works”)? [Coverage, Spec US2]

## Dependencies & Ambiguities

- [ ] CHK021 Is the test-only SignalR.Client 10.0.14 pin recorded with Build-pickup drift recheck? [Dependency, Spec Assumptions / FR-012, Brief Q6]
- [ ] CHK022 Does the amendment proposal preserve unrelated constitution rules and only add the notifier + polling wording alignment? [Consistency, constitution-amendment-v1.5.0.md]
- [ ] CHK023 Are there zero unresolved `[NEEDS CLARIFICATION]` markers and zero Quill-invented structural choices outside `needs decision:` lines? [Ambiguity, DRAFTING_RECEIPT.md]

## Notes

- Checklist validates requirements writing quality, not implementation behavior.
- Gate 1 remains closed until the user answers the structural checkbox; checking CHK004 confirms presence, not owner approval.
- Plan challenge Round 1 PC1/PC2 clarifications applied; Ledger M2 remains Follow-up (noted, no ticket) — no speculative helper in frozen file set.
