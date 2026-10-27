# DEV-423 Phase 2 Plan Challenge Adjudication

Date: 2026-10-26. Adjudicator: Keel. Decision owner: Patron.
Worktree: F:/Dev/LamuFlix.worktrees/feature-423-spec. Branch: feature/423-spec.
Status: PLAN FROZEN FOR GATE 1 REVIEW - final repair verification and read-only analysis CLEAN. Gate 1 not approved; Phase B blocked.

## Inputs and authority

All three reports present: findings-DEV-423-Sentry (Risk: M1, L1-L6), findings-DEV-423-Ledger (Standards: S-01-S-05), findings-DEV-423-Compass (Spec: F1-F5), fichario LamuFlix notes. Total 17: 4 Medium, 13 Low, no High/Critical. Challenger Round 1; one consolidated Quill repair list, followed only by verification. Prior drafting/analyze corrections are preserved in DRAFTING_RECEIPT.md; no third fix/challenge round is authorized. Q6 two-round cap remains binding.

Facts used solely from recon-DEV-423 sections 6-8 and recon-DEV-423-2 B-L; challenger observations are evaluated against that recorded evidence, not treated as new independently gathered recon. Artifact consistency checked directly in this worktree. No production-code search or measurements performed.

Patron PC1-PC6 approved, cited, and read back in CONCLUSIONS.md, Phase 2 Plan-Challenge Adjudication (Round 1); reproduced in append-only brief.md amendment before repairs. These supersede only conflicting historical Q2/Q5 path/assertion summaries. Ticket items 1-5 and AC1-3 remain authoritative (recon-DEV-423-2 B; PRODUCT sections 3,5).

## Finding dispositions (17/17)

| Axis / ID | Reported severity | Disposition and evidence | Repair / closure anchor |
| --- | --- | --- | --- |
| Sentry M1 | Medium | ACCEPTED COST, explicit Patron PC1 re-ruling. Later terminal failure overrides remembered eligible failure. Secondary InvalidResponse/DuplicateImdbId can immediately dead-letter rather than retry; bad data must remain visible. Unknown is terminal only for chain advancement and remains retryable downstream. Q3; recon original 7:105-108, part 2 B item 2. No algorithm change or ticket escalation. | brief PC1; CONCLUSIONS PC1; spec US1-7/FR-002/Edge Cases, plan Risks. |
| Sentry L1 | Low | ACCEPTED COST. Unknown stops fallback as mandated by ticket, despite downstream retryability; no requirement defect. Patron PC1, original recon 6.1/7 and part 2 B item 2. | brief PC1; explicit Edge Cases/Risks. |
| Sentry L2 | Low | ACCEPTED COST. Q4 permits both independent provider budgets; no chain deadline added and no timing-sensitive elapsed-time ceiling test required. Patron ratification; recon part 2 G. | brief non-blocking dispositions; plan Performance Goals/Risks. |
| Sentry L3 | Low | NOTED, NO TICKET. RateLimited fan-out is ticket-required. Chain bulkhead/cap is outside Q4/Q5; Q6 follow-up filing bar not met. Patron ratification. | CONCLUSIONS Keel PC receipt; no new task. |
| Sentry L4 | Low | ACCEPT keyed factory precision; decline optional duplicate-list guard. Resolve entries exclusively via GetRequiredKeyedService<IMetadataProvider>; factory owns fixed ordered list. Patron PC4; recon part 2 B items 3-4/J and Q2. | FR-004, plan Approach, T005. |
| Sentry L5 | Low | ACCEPT exact guard contract. ArgumentNullException for null, ArgumentException for empty; snapshot and explicit tests. Patron PC3; Q5. | FR-001/FR-006, plan Approach, T002/T003. |
| Sentry L6 | Low | NO ADDITIONAL CASE REQUIRED. Unit lookup/ManualImdbId identity plus real typed-client fallback tests suffice; adapter-specific manual-id lookup semantics stay OMDb/DEV-331 owned. Patron ratification; Q3, recon original 6.2 and part 2 E. | FR-003/FR-006, T004/T011; no added upstream test scope. |
| Ledger S-01 | Medium | ACCEPT; Q5 exact path deliberately amended by Patron PC2. Unit file in Adapters/, namespace LamuFlix.UnitTests.Adapters; class unchanged. No production-file expansion. | FR-006, plan Project Structure, T002/T004. |
| Ledger S-02 | Medium | ACCEPT precision; no category enum/type change. Eligibility is exact record equality to RateLimited/ProviderUnavailable; every other/future category terminal. No mutable static initialisation. Derive matrix categories from type; current 7x7 expands on category additions. Patron PC3; recon original 6.1 and AGENTS static-init rule. | FR-002/FR-006, plan Approach/Test Strategy, T002/T003. |
| Ledger S-03 | Low | ACCEPT snapshot guard design. Guard then ToArray snapshot so caller-list mutation cannot change order; unit mutation case. Do not capture an unguarded mutable list via primary constructor. Patron PC3; Q5 constructor contract. | FR-001/FR-006, plan Approach, T002/T003. |
| Ledger S-04 | Low | NO ACTION beyond S-01. MetadataProviderChainTests and MetadataProviderChainCompositionTests are distinct, vendor-neutral names. Patron PC2 ratification; Q5/constitution VIII. | Existing class names retained. |
| Ledger S-05 | Low | ACCEPT ownership precision. Replace OMDb port-typed client only, retain landed TMDb registration/resilience/normalization/secondary health verbatim; preserve private shared Configure, MetadataProviderResilienceOptions and unrelated comments. Patron PC4; recon original 6.3-6.4, part 2 G/J. | FR-004/FR-005/FR-008, plan Approach, T001/T005. |
| Compass F1 | Medium | ACCEPT. Distinguish authored OMDb concrete registration + keyed forwards + sole unkeyed factory from retained DEV-331 TMDb client. T001 confirms landed lines before T005; do not duplicate/re-author upstream client/normalization. Patron PC4; recon part 2 J. | Scope/FR-004/FR-008, plan Approach, T001/T005. |
| Compass F2 | Low | ACCEPT narrow Q5 amendment by Patron PC5. Only type or behaviour assertions pinning unkeyed IMetadataProvider to bare OMDb in the two already named composition files may change; unrelated health/telemetry/resilience assertions preserved. Broader drift needs cited ruling first. | FR-008, plan file set, T007/T008. |
| Compass F3 | Low | ACCEPT. T009 resolves keyed providers under both Enabled modes in separate proof from T010 disabled chain zero secondary resolve/invoke/HTTP. Explicit direct keyed resolution in its own test is not disabled chain construction. Patron PC6; Q2, recon part 2 J. | FR-007, plan composition strategy, T009/T010. |
| Compass F4 | Low | ACCEPT traceability repair. Scope bounded-file citation is FR-008, not FR-010. Patron PC6; Q5. | spec Scope. |
| Compass F5 | Low | ACCEPT dependency repair. Remove T002 P marker and corresponding example/dependency markers; tests may be drafted first but compile with T003. No mandatory red-test sequencing. Patron PC6; Q5 and prior analyze optional-TDD ruling. | tasks T002/Dependencies/examples. |

Accepted edits cover 11 findings (3 Medium, 8 Low); accepted costs cover 3 (1 Medium, 2 Low); noted/no ticket covers 1 Low; no further work required covers 2 Low. Total 17, no silent skip. All four original Medium findings require explicit closure before freeze: M1 via Patron re-ruling, S-01/S-02/F1 via verified repairs.

## Consolidated Quill repair list

1. Correct unit test path/namespace throughout operative artifacts (S-01).
2. Exact eligibility/default, static hygiene, snapshot, exception guards, caller-list mutation test and category-derived matrix (S-02/S-03/L5).
3. OMDb authored/TMDb retained registration boundary, keyed factory resolution, shared helper/options and pickup validation (F1/S-05/L4).
4. Narrowly permit bare-OMDb type/behaviour assertion updates in only the two named files, preserving unrelated assertions (F2).
5. Both Enabled modes for keyed tests, distinct disabled-chain zero-secondary-resolution proof (F3).
6. Scope FR citation and T002 parallel/dependency/examples correction (F4/F5).
7. Record accepted mixed-terminal dead-letter cost and distinguish chain-terminal Unknown from downstream retryability (M1/L1); keep other Low dispositions bounded; align receipt/checklist and readiness wording.

Quill owns only spec.md, plan.md, tasks.md, checklists/requirements.md and DRAFTING_RECEIPT.md. Keel owns decision appendices and this adjudication. No production changes, build/test gates, upstream implementation or tracker writes authorized.

## Read-only analysis method

SpecKit prerequisites command returned FEATURE_DIR specs/DEV-423 and tasks.md available. No before_analyze/after_analyze hooks in .specify/extensions.yml. Reviewed ticket coverage, FR/SC task mapping, brief decisions, scope/terminology, file sets, task order, constitution I/VI/VII/VIII/IX and stack constraints. Constitution is unchanged; DEV-331 Q7 owner resolution/amendment/ADR and implementation remain mandatory T000/T001 blockers, not a new DEV-423 departure. Final returned-artifact verification and coverage receipt follow below.


## Returned repairs and read-only Specification Analysis Report

Quill completion receipt was delivered as the literal CLI argument --file. Keel recovered and read the full numbered report from specs/DEV-423/_quill-keel-report.txt; this was a receipt transport defect, not another review or repair round. Independently read spec/plan/tasks and checklist/receipt, then mechanically verified requirement/task counts, 17 distinct finding rows, corrected unit paths/namespace, guard/snapshot/eligibility constraints, keyed factory mechanism, OMDb/TMDb ownership boundary, both-mode keyed proof versus disabled-chain proof, Scope FR-008 and T002 dependency marker removal. Quill was told no further repairs needed and ownership ended. Temporary receipt cleanup remains Quill-owned.

All 7 repair-list items VERIFIED. Medium closure: Sentry M1 via cited explicit PC1 accepted-cost ruling; Ledger S-01 via spec FR-006, plan Project Structure, tasks T002/T004; Ledger S-02 via FR-001/002/006, plan Approach/Test Strategy, T002/T003; Compass F1 via Scope/FR-004/008, plan Registration, T001/T005. The 13 Lows are individually repaired or dispositioned in the table above. No remaining blocking finding; none waived at a cap.

| Analysis ID | Category | Severity | Location | Result |
| --- | --- | --- | --- | --- |
| FINAL | Consistency/coverage/constitution | None outstanding | Repaired spec.md, plan.md, tasks.md against brief Q1-Q7 + PC1-PC6 | CLEAN within Phase A scope; accepted costs retained. |

### Coverage summary

| Requirement | Has task | Task IDs | Notes |
| --- | --- | --- | --- |
| FR-001 | Yes | T002-T004 | Guards, snapshot, chain. |
| FR-002 | Yes | T002-T003 | Category-derived matrix and fallback/exhaustion. |
| FR-003 | Yes | T003-T004 | Identity/cancellation/exceptions. |
| FR-004 | Yes | T005-T010 | Keyed composition, modes, narrow assertion updates. |
| FR-005 | Yes | T005, T009, T012 | Transparent health/resilience/telemetry. |
| FR-006 | Yes | T002, T004 | Unit coverage under Adapters. |
| FR-007 | Yes | T009-T012, T018 | Real-client local HTTP, unchanged regressions. |
| FR-008 | Yes | T005, T007-T008, T013 | Bounded file set. |
| FR-009 | Yes | T000-T001; task dependencies | Upstream landing then pickup, before T002-T021. |
| FR-010 | Yes | T014-T021 | Required gates/review, exit distinctions and cap. |
| SC-001 | Yes | T002-T003, T011 | Miss fallback and Found short circuit. |
| SC-002 | Yes | T002-T003, T011 | RateLimited fallback, InvalidResponse stop. |
| SC-003 | Yes | T010, T018 | Disabled unchanged enrichment regressions. |
| SC-004 | Yes | T002-T004, T010 | Exhaustion and one-provider equivalence. |
| SC-005 | Yes | T009-T010 | Keys/unkeyed, RabbitMQ/readiness. |
| SC-006 | Yes | T012, T018 | Per-provider measurements/health, existing tests unchanged. |
| SC-007 | Yes | T014-T020 | Analyzer/format/test/security/mutation gates. |

Metrics: 10 FR + 7 SC = 17 requirements; 22 unique tasks T000-T021; coverage 17/17 (100%); unmapped tasks 0; unresolved ambiguity 0; conflicting duplication 0; outstanding Critical/High/Medium consistency issues 0. Accepted risk/cost findings are separately recorded above, not reclassified as tests passed. Constitution I/VI/VII/VIII/IX and stack alignment remains conditional on the explicit upstream DEV-331 departure resolution and amendment/ADR landing before Phase B; this is an external prerequisite, not a new DEV-423 waiver.

## Final freeze and phase boundary

Keel freezes the repaired plan for Gate 1 review on 2026-10-26, after recording the decision append-only in brief.md and CONCLUSIONS.md. This is not Gate 1 approval. No DEV-423 owner checkbox is introduced or any upstream checkbox closed. T000/T001 remain mandatory before Phase B; no implementation or harness/build/test gates ran, no review/merge bar or merge claimed. Next: Conductor Gate 1 spec-PR review/publication workflow, owner-owned approval/merge, and later separate upstream-readiness/pickup verification. No third challenge/fix round requested.

### Frozen working-byte receipt

Current HEAD at freeze: `0dd47716d5500bef1b0fb92f380e216c96a8177f`. HEAD is identity only; most packet files remain untracked, so the following hashes pin reviewed working bytes rather than committed content.

| Artifact | SHA256 |
| --- | --- |
| spec.md | `01a6d5616b806741e8255ebaa0cfc42ab717923e11b83b1eb61fe5427a7eaa98` |
| plan.md | `71d23295f8b20c60d192385e527e766786751e6d4d3d80f72446f8dac312e2e2` |
| tasks.md | `683e8b8b929a7ce3f2c48fe6559229c5dbc576c0943a2296bd567e0a406abe4d` |
| brief.md | `9253667d286999579d2c29541f23d41e63c99932f8926332f1ef9e5f1780df61` |
| CONCLUSIONS.md | `b9ba356d888495908df6c74fd734b26f4f65da57800b853a39e4dc51d897e33b` |
| ASSUMPTIONS.md | `49f1de6f054c5d74d5b9a0f9b992f7a30f8ef73eca93b15d0f70509ebfdfa4ed` |
| DRAFTING_RECEIPT.md | `0f713fc3e2f3cd336c86e17d22c21e7b7c475977e5ed246bb2e49dd71a7fc54b` |
| checklists/requirements.md | `4e0ec07dc3df4b5aee4d496c9635e4dd71f6d7bb58bc767589ae1041ead988f5` |
