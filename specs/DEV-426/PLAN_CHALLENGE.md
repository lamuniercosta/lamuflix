# DEV-426 plan challenge adjudication

Status: **PLAN FROZEN — final round 2 of maximum 2 independently verified**. All 24 findings have final individual dispositions; every accepted correction is resolved. Gate 1 remains closed by the unchecked owner decision in spec.md. This document adjudicates planning evidence only; no implementation gates, review clearance or merge approval are claimed. This final status supersedes the drafting-time pending/not-declared wording preserved in Quill's artifacts and historical round receipts below.

## Authority and evidence

Worktree `F:/Dev/LamuFlix.worktrees/feature-426-spec`, verified branch `feature/426-spec`. All three required reports were read directly: notes `findings-DEV-426-Sentry` (9 findings), `findings-DEV-426-Ledger` (10 findings, including L5 despite its header claiming four Lows), `findings-DEV-426-Compass` (5 findings). Total: **24 individually adjudicated findings**.

Named planning artifacts read: plan.md, tasks.md, spec.md, brief.md, CONCLUSIONS.md, ADR-0031 and constitution.md. Implementation facts come from `recon-DEV-426`, particularly lines 23-27, 32-33, 41-52, 74-88; no independent production searches, measurements or tests. Patron PC1/PC2 at CONCLUSIONS.md (c625ea29) supplies the cited decisions, now carried into brief.md and ADR-0031 before Quill edits. Earlier references below identify the challenged pre-remediation passages; closure receipts will cite repaired passages.

## Risk axis — Sentry

| ID | Severity | Disposition and rationale | Closure required |
|---|---|---|---|
| H1 | High | **Accept clarification/test gap; reject retry-policy redesign.** Gate latency lies inside the existing attempt timeout (recon:77-88; plan:11,21). Q2/Q3 already accept committed reservations without dispatch. Patron PC2 pins ambient token and bounded slow-gate evidence. Never promise commits equal wire requests under timeout or a refund. | FR-005/FR-007, T016/T024: ambient token, no bookkeeping increment, no dispatch without true, slow-gate composed test bounds admission/commit counts by configured attempt/total budgets. |
| H2 | High | **Accept hardening.** US2:68 and US3:89 already require unwrapped in-flight cancellation, but FR-004 and fault handling leave room for a broad catch to violate it. Q2/Q3 and PC2 forbid OCE/TaskCanceledException conversion. | FR-004/FR-006, T009/T016/T024 explicitly exempt cancellation before fault-to-signal handling; pre/in-flight cancellation evidence. |
| M1 | Medium | **Accept zero-row interpretation clarification; reject alleged positive-limit insert overage.** Q4 requires a positive integer limit, so initial count 1 cannot exceed a valid lowered limit. Historical lowered-limit denial is already specified (US4:104). One statement must report zero rows as false, one committed admission row as true; SQL errors remain faults. | FR-004/T009 and real-PostgreSQL tests pin zero/one-row interpretation and retain first-row/lowered-limit cases. No SQL redesign mandated. |
| M2 | Medium | **Accepted existing cost; no fix.** brief:27 and CONCLUSIONS Q2 explicitly accept one DB round trip per attempt, including denied admission. Q6 excludes a negative-cache abstraction and DEV-333 work. No additional feature or follow-up ticket is required to freeze this plan. | Closed by cited Q2/Q6 cost acceptance; carry into risks without a new cache task. |
| M3 | Medium | **Accept test specificity.** brief:33 already requires disposal before transport/on all exits; a method-wide await-using would violate it. A bounded async scope must end before base.SendAsync, without double-disposal dependence or later gate access. | T016/T020 explicitly test disposal on success, false, persistence fault and cancellation, plus dispose-before-transport. |
| M4 | Medium | **Accept safe-log boundary.** Preserve original inner exception per Q3 but do not serialize it into logs. PC2 confirms safe structured outcome/category fields and inherited processing identity where available. | FR-006/T019/T020 safe log assertions cover URI/key/raw exception and scope exposure; retain original inner only in memory. |
| L1 | Low | **Accept clarification.** Q2 samples once per acquisition, Q3 retries acquire independently; retries across midnight therefore charge their respective sampled dates. | Edge Cases/T014 clarify per-acquisition date, including a logical lookup crossing midnight. |
| L2 | Low | **Accepted operational precondition; no fix.** brief:43 and CONCLUSIONS Q4 explicitly accept inconsistent host limits as operator error without detection/reconciliation. Adding startup logging is optional scope, not necessary evidence of this contract. | Closed by Q4 and ADR consequences; no new startup-log requirement. |
| L3 | Low | **Accept test hardening.** Q5 requires synchronized contenders and independent scopes; constitution IX forbids sync-over-async. | T013 names async start coordination plus Task.WhenAll; no Task.Delay staggering or blocking barrier waits. |

## Standards axis — Ledger

| ID | Severity | Disposition and rationale | Closure required |
|---|---|---|---|
| H1 | High | **Accept.** Constitution VIII:378-383 does not exempt all Infrastructure support types. Patron PC1 chooses functional names rather than widening the amendment; exact ticket Core names/table stay. | Rename all new support-type/file references to PC1 names in plan/spec/tasks; split signal files. No constitution change or new owner checkbox. |
| M1 | Medium | **Not upheld as a missing domain glossary change.** Patron PC1 confirms existing Q6 (CONCLUSIONS:165) and brief:15: quota/admission/reservation are local implementation vocabulary, not new Movies domain concepts. Constitution VIII:384 applies to new domain terms; no new one is introduced. | Closed by deliberate cited Patron ruling; no CONTEXT.md edit. |
| M2 | Medium | **Accept delivery ownership gap.** T035:18 mixes verification and mutation. Patron PC2 supplies the implementation-PR status-bump path; ADR remains Proposed during Phase A. | Name Keel, implementation PR, prior verified Gate 1 approval and before-code-merge boundary consistently; no direct main writes. |
| M3 | Medium | **Accept bounded-file planning gap.** Q6 permits narrow recon-named adaptations, not an open-ended sweep; T004/T021/T022 overlap and plan:85-94 is vague. | Enumerate exact recon-backed file sets, disjoint task ownership and quota-only changes; distinguish migration guards from fixture config. Missing path evidence requires recon, not invention. |
| M4 | Medium | **Accept standards detail.** Constitution IX:403-410 explicitly requires AutoFixture/Faker, fresh data, Theory/MemberData over repeated Facts and no sync-over-async. | tasks Conventions and plan Test Strategy apply these to T004/T023/T024 matrices, with async coordination for T013. Domain boundary values may be explicit inputs. |
| L1 | Low | **Accept traceability.** Constitution Compliance:637 requires the principle number for evaluated gates. | Add VII deterministic time, II/V internal-signal boundary and async CancellationToken quality gate rows. |
| L2 | Low | **Accept documentation clarification.** Principle II:242-245 concerns domain failures mapped at the API. Q3 internal control signals are caught inside Infrastructure and become existing failure categories; they must never reach API exception mapping. | Explicit internal-only/no API escape statement and mapping tests in plan/T015/T018/T020. No public exception mapper added. |
| L3 | Low | **Accept shape specificity.** Q4 numeric options fit constitution VII:351-354 and Types:511; recon:23 supplies sealed-record/SectionName precedent. | T002/T003 specify sealed record, SectionName constant, positive Range data annotation, existing BindAndValidate/ValidateOnStart mechanism. |
| L4 | Low | **Accept field clarification with PC2 ruling.** Constitution VI:334-335, Q3 and PC2 govern existing processing identity and safe failure category/outcome logs; gate has no fabricated movie id. | Align logging/test requirements with PC2; do not introduce a new identifier flow or serialize inner exceptions. |
| L5 | Low | **Accept split signal files; existing phase order already resolves numbering.** T034/T035 are expressly Phase 0 and tasks Dependencies:99-104 orders T021 before T013 where needed. Numeric IDs need not match execution order. PC1 separately requires one file per signal. | Keep stable task IDs, state explicit phase dependency table/sequence, split signals by PC1 names. No wholesale renumber needed. |

## Spec axis — Compass

| ID | Severity | Disposition and rationale | Closure required |
|---|---|---|---|
| M1 | Medium | **Accept missing observable behavior.** FR-006 did not settle health-state recording. Patron PC2: neither local signal changes MetadataProviderHealthState, because neither observes upstream health; existing HTTP behavior unchanged. | Acceptance/FR-006 and T020 test both signals leave health state unchanged. |
| M2 | Medium | **Accept missing partial-result behavior.** Q3 hard-stop applies even after earlier variants returned candidates. Patron PC2: discard partial accumulation, return mapped failure, stop OMDb variants, preserve chain fallback. | Acceptance/FR-006 and T020/T024 cases for both later interruption signals, with prior partial candidates. |
| M3 | Medium | **Accept edit-ownership inconsistency.** FR-008 verify-only conflicts with T035 date editing. Patron PC2 supersedes I2 date reconciliation: constitution receives no Phase B edit, date-only discrepancy is recorded in CONCLUSIONS.md. | Remove all date-edit authorization from Quill artifacts/proposal; T034/T035 name Keel and recording location, substantive mismatch-only stop. |
| L1 | Low | **Accept exact-count clarification.** Q1/Q5 count each physical request; a direct detail success costs 1, one search then one detail costs 2, each extra request costs another reservation. | US3/FR-007/T023 spell exact WireMock and persisted deltas for defined request shapes, avoiding ambiguous logical-call wording. |
| L2 | Low | **Accept receipt destination.** Patron PC2 makes Keel record checkbox answer plus spec-PR merge receipt in CONCLUSIONS.md; current T034:17 gives no destination. | T034 names file, recorder and verified owner/merge evidence; no approval inferred from planning. |

## Remediation protocol

Quill round 1: one numbered list covering the accepted fix groups, derived from the frozen brief supplement and PC1/PC2. Quill owns spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md, checklists/requirements.md and the amendment proposal record only; Keel owns brief/CONCLUSIONS/ADR/this adjudication. No constitution or production writes. Independently read repaired passages before closing each accepted finding. A second numbered list may address residuals; after round 2 any remaining blocker stops as blocked under the cap. No extra round, waiver or reclassification to manufacture freeze.

PLAN FROZEN may be declared only after every accepted correction is resolved and every remaining finding has the explicit disposition above. Freeze does not close the owner Gate 1 checkbox, authorize implementation or establish merge clearance.

### Numbered fix list sent to Quill — round 1

1. Apply PC1 functional support-type/file names everywhere; one signal per file (Ledger H1/L5).
2. Pin unchanged health state and partial-result discard for both signals, with acceptance/tests (Compass M1/M2).
3. Name Keel and CONCLUSIONS receipt for T034; T035 constitution verification has no edits; ADR status-only change belongs to the implementation PR after verified approval (Ledger M2; Compass M3/L2).
4. Pin ambient token, cancellation exclusion, committed/no-refund caveat and slow-gate shipped-chain bounds without retry changes (Sentry H1/H2).
5. Pin zero/one admission-row interpretation and retain real-PostgreSQL first-row/lowered-limit tests (Sentry M1).
6. Pin bounded async scope and disposal assertions on true/false/fault/cancellation (Sentry M3).
7. Pin safe outcome/category logs, existing processing identity and leakage tests; preserve inner exception in memory only (Sentry M4; Ledger L4).
8. Enumerate recon-backed disjoint fixture/migration file sets and narrow edit bounds; no open-ended sweep (Ledger M3).
9. Specify fresh AutoFixture/Faker data, Theory/MemberData matrices, async start coordination and explicit phase dependency order with stable IDs (Ledger M4/L5; Sentry L3).
10. Specify options shape/validation and missing Constitution Check rows, internal signals never escaping to API (Ledger L1/L2/L3).
11. Pin exact success/search/detail attempt counts and retry-acquisition midnight semantics; retain existing cost/precondition dispositions (Compass L1; Sentry L1/M2/L2).

Sent via `maestri ask Quill` with ask-back to Keel, exclusive drafting-file ownership and prohibition on constitution/production writes. Receipt/checklist/proposal record must agree with repaired spec/plan/tasks. Keel will independently verify the returned passages.

### Round 1 independent read-back and final round 2

Read back spec.md:18-36,61-95,131-160; plan.md:27-53,75-106,118-149; tasks.md:8-33,43-93,113-166; proposal:1-26,28-116; drafting receipt and checklist. Narrow path evidence now independently supplied in recon-DEV-426:91-102 at c625ea29, confirming the six previously abbreviated test paths. No independent code search was used by Keel. Round 1 resolved behavioral, naming, fixture-ownership and test-evidence corrections. Two remaining drafting details were sent as the final round 2 numbered list:

1. **Ledger L1**: plan.md:39 async quality row has no principle number. Label IX / Coding Conventions / PR quality gates and cite constitution:409-410,475,540-542.
2. **Compass M3 / Ledger M2**: proposal:28-58 now contains an editorially updated illustrative Sync Impact Report, not the verbatim report in staged constitution:1-29. Mark it illustrative; T035/FR-008/FR-010 compare the actual Gate-1-approved staged constitution and its report, not literal equality to that illustration. CONCLUSIONS PC1/PC2 governs ADR delivery interpretation. Do not mutate constitution or create an automatic mismatch from the illustrative wording.

All other accepted corrections are resolved by the read-back above; the original no-fix dispositions remain intact. After final round 2, any retained blocker stops the process under the cap.

### Individual closure receipts from round 1

| Axis / finding | Independent read-back receipt | Final disposition |
|---|---|---|
| Sentry H1 | spec.md:83,94,150,156; tasks.md:77,93 | Resolved: ambient token and slow-gate composition bounds; Q2 committed-loss caveat retained. |
| Sentry H2 | spec.md:68,92,149,151; tasks.md:60,77,81,93 | Resolved: OCE/TCE excluded before signal mapping; caller cancellation and all-exit tests mandatory. |
| Sentry M1 | spec.md:65-71,149,154; tasks.md:60,65-66 | Resolved zero/one-row semantics; alleged positive-limit first-insert overage not upheld as adjudicated. |
| Sentry M2 | plan.md:21,144; spec.md:138,187 | Accepted existing Q2 cost; no cache feature. |
| Sentry M3 | spec.md:93,150,155; tasks.md:77,81 | Resolved bounded scope, all-path disposal and lifetime assertions. |
| Sentry M4 | spec.md:95,151,155; tasks.md:80-81 | Resolved safe structured logs/leakage evidence; original inner retained in memory. |
| Sentry L1 | spec.md:131-132,154,175; tasks.md:66 | Resolved independent per-acquisition date sampling across midnight. |
| Sentry L2 | spec.md:112,139,186; plan.md:145 | Accepted Q4 operational precondition; no new startup-log feature. |
| Sentry L3 | tasks.md:8,65; plan.md:119 | Resolved async coordinated start/WhenAll and no delay/blocking barrier. |
| Ledger H1 | plan.md:36,75-84; tasks.md:57-61,76-78; proposal:87-94 | Resolved functional support names, exact ticket names/table, separate signal files; no broader VIII exception. |
| Ledger M1 | CONCLUSIONS.md PC1; brief.md PC1/PC2 supplement; spec.md:169 | Not upheld: cited Patron ruling confirms local implementation vocabulary, no new domain glossary entry. |
| Ledger M2 | tasks.md:32-33; spec.md:158,160; proposal:30,114-115 | Resolved: owned implementation-PR status path; final round 2 distinguishes staged report from illustration. |
| Ledger M3 | plan.md:95-106; tasks.md:46,63-64,81,91-93; recon-DEV-426:95-100 | Resolved exact disjoint fixture paths and narrow edit bounds, independently backed by recon. |
| Ledger M4 | tasks.md:8,46,65,92-93; plan.md:18,37,118-121 | Resolved fresh data, Theory/MemberData matrices and async concurrency. |
| Ledger L1 | plan.md:32,35,39 | Resolved: VII time, II/V internal signals and IX async row with constitution:409-410,475,540-542 citations. |
| Ledger L2 | spec.md:91,151; plan.md:32; tasks.md:76,79,81 | Resolved Infrastructure-only signal mapping and no API escape. |
| Ledger L3 | spec.md:111,147; tasks.md:44-46 | Resolved sealed record, SectionName, positive Range and startup annotations/ValidateOnStart. |
| Ledger L4 | spec.md:95,151; plan.md:33; tasks.md:80-81 | Resolved safe category/outcome logging with existing processing identity where available. |
| Ledger L5 | tasks.md:12-23,76,113-119 | Resolved separate signal files; stable IDs have explicit phase/dependency ordering. |
| Compass M1 | spec.md:88-89,151,155; tasks.md:79,81 | Resolved health unchanged for both local signals with explicit tests. |
| Compass M2 | spec.md:90,134,151,155; tasks.md:79,81,93 | Resolved partial discard for both later-interruption signals and preserved chain fallback. |
| Compass M3 | spec.md:158,160; tasks.md:32-33; proposal:28-30,114-115 | Resolved: no constitution/date edit; actual approved staged artifact is comparison target, not the illustrative report. |
| Compass L1 | spec.md:86,136,156,176; tasks.md:92; plan.md:121 | Resolved exact detail=1/search+detail=2/+1 additional physical request counts. |
| Compass L2 | tasks.md:32; spec.md:12,160 | Resolved Keel/CONCLUSIONS destination and owner-answer plus verified spec-PR merge receipt. |

## Final round 2 read-back and freeze

Quill reported final round 2 complete with no residuals. Keel independently read the saved passages, rather than relying on that report:

1. plan.md:39 now names IX (async testing) / Coding Conventions / PR quality gates and cites constitution:409-410,475,540-542, retaining ambient-token and cancellation behavior. Ledger L1 is closed.
2. proposal:28-30 explicitly labels the report illustrative, not an exact staged artifact or a pending constitution edit. spec.md:158,160, tasks.md:33 and proposal:114-115 compare the actual Gate-1-approved staged constitution and its Sync Impact Report, retain date-only recording without mutation, and defer ADR status-only change to Keel in the implementation PR under CONCLUSIONS PC1/PC2. Compass M3 and Ledger M2 are closed.
3. DRAFTING_RECEIPT.md:63-65 and checklists/requirements.md:60-64 record round 2 final with no residuals and preserve the open owner gate. No third round is used or authorized.

**PLAN FROZEN.** Sentry 9/9, Ledger 10/10 and Compass 5/5 findings individually disposed; no unresolved accepted correction remains. Accepted existing costs and the glossary non-finding retain their cited rationale above. No finding was downgraded to manufacture closure. Two remediation rounds used; cap exhausted.

Frozen authority: brief.md Q1-Q6 plus C1/C2/I1 and PC1/PC2 supplement; CONCLUSIONS.md including Patron PC1/PC2; the verified current spec.md, plan.md, tasks.md and proposal; ADR-0031 remains Proposed. Exact abbreviated test paths are backed by recon-DEV-426:91-102. Refresh recon at Phase B pickup for main/contract drift.

Next boundary is the owner's Gate 1 review/approval of ADR-0031 and constitution amendment 1.7.0 through the spec-PR merge. The checkbox at spec.md:10 remains unchecked. Freeze is not owner approval, implementation authorization, a passed gate, review clearance or merge permission. No build, test, analyzer or mutation command was run for this document-only adjudication.
