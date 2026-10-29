# DEV-426 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-10-28
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-426-spec` · branch `feature/426-spec`
**Method**: `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` in one ask (plus requirements checklist + this receipt); prior remediation rounds (C1/C2/I1, I2); plan-challenge ROUND 1 of cap 2; **plan-challenge ROUND 2 FINAL (narrow documentation residuals only)** from Keel against `PLAN_CHALLENGE.md` + Patron PC1/PC2 (`CONCLUSIONS.md` c625ea29). Grill already closed (`brief.md`); mandatory `before_specify` grill hook not re-executed. `/speckit-analyze` not run by Quill (Keel owns read-only analyze / plan challenge). `.specify/feature.json` already pointed at `specs/DEV-426` and was left unchanged.

**Inputs**: frozen `specs/DEV-426/brief.md` (Q1–Q6 + post-grill C1/C2/I1 + PC1/PC2 supplement), `CONCLUSIONS.md` (incl. Patron C1/C2 and PC1/PC2), `PLAN_CHALLENGE.md` (24 dispositions), Proposed ADR-0031 (Keel-owned; not edited by Quill), Maestri notes `DEV-426`, `recon-DEV-426`, `specs/PRODUCT.md`, `.specify/memory/constitution.md` (gates cite only; no constitution file edits by Quill).

**Artifacts written / updated (Quill-owned only)**:

- `specs/DEV-426/spec.md`
- `specs/DEV-426/plan.md`
- `specs/DEV-426/tasks.md`
- `specs/DEV-426/checklists/requirements.md`
- `specs/DEV-426/constitution-amendment-v1.7.0.md` (proposal record; Quill)
- this receipt

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, ADR-0031, `.specify/memory/constitution.md`, `PLAN_CHALLENGE.md`, production code.

**Owner checkbox**: present and unchecked in `spec.md` — Owner approves ADR-0031 and constitution amendment 1.7.0 for the ticket-named IOmdbRequestGate and OmdbQuotaOptions; approval occurs through the Gate 1 spec-PR merge. Not a waiver; no present owner approval. Owner rejection → `blocked: structural — ticket-named port/types vs constitution`. PLAN FROZEN not declared.

## Plan-challenge ROUND 1 — Keel numbered fixes → PLAN_CHALLENGE IDs

| Fix # | Applied to | PLAN_CHALLENGE IDs closed by this edit |
|---|---|---|
| (1) PC1 functional support-type/file names; split signal files; ticket Core/table exact; no wider VIII exception | spec Scope/FR-003–006/Key Entities; plan Structure/Approach/Constitution VIII; tasks T006–T010/T015–T017; amendment VIII note; this receipt | Ledger H1, L5 (split); Ledger M1 closed by Patron (no CONTEXT edit) |
| (2) PC2 health unchanged; discard partials; both-signal acceptance/tests; stop variants/preserve chain | spec US3 scenarios 6–8, FR-006, Edge Cases, SC-005; plan Approach; tasks T018/T020/T024 | Compass M1, M2 |
| (3) PC2 gate docs: T034 Keel CONCLUSIONS checkbox+merge receipt; T035 verify-only no date edit; date-only in CONCLUSIONS; ADR Accepted in implementation PR only; remove date-reconcile/apply implications including amendment | spec Status/Owner/FR-008/FR-010; plan Governance/Ordering; tasks T034/T035/Notes; constitution-amendment Staging/Footer/Sync Impact; checklist Notes | Compass M3, L2; Ledger M2 |
| (4) Sentry H1/H2: ambient SendAsync token; exclude OCE/TCE before fault-to-signal; in-flight caveat; no increment/dispatch without true; slow-gate bounds by attempt/total budgets; no retry-policy change; no commits=wire under timeout | spec US3 1/10–12, FR-004–005, FR-007; plan Risks/Test Strategy; tasks T009/T016/T024 | Sentry H1, H2 |
| (5) M1 zero rows=false, one row=true, SQL errors=faults; retain first-row/lowered-positive-limit | spec US2 1–2/7, FR-004, FR-007; tasks T009/T014 | Sentry M1 |
| (6) T016/T020 bounded async scope ends before transport; disposal on true/false/fault/cancellation; fresh scopes/no scoped capture | spec US3 11, FR-005, FR-007; plan Approach/Risks; tasks T016/T020 | Sentry M3 |
| (7) Safe outcome/category logs; no URI/key/raw exception/unsafe scope fields; inner in memory; existing movie id; no invented gate id; leakage tests | spec US3 13, FR-006, FR-007; plan Constitution VI; tasks T019/T020 | Sentry M4; Ledger L4 |
| (8) Exact recon-backed disjoint ownership T004/T012/T020/T021/T022 (+T023/T024 probe); quota-only + named migration guards; no open-ended suites | plan Project Structure ownership table; tasks T004/T012/T020–T024; FR-007/FR-008 | Ledger M3 |
| (9) AutoFixture/Faker; Theory+MemberData T004/T023/T024; T013 async start+WhenAll; explicit phase dependency order; stable IDs | plan Test Strategy/Ordering/Constitution IX; tasks Conventions + phase table + T004/T013/T023/T024 | Ledger M4, L5; Sentry L3 |
| (10) T002 sealed record, SectionName, Range(1,int.MaxValue), BindAndValidate/annotations/ValidateOnStart; Constitution Check VII time, II/V internal signals, async token | spec FR-002/US4; plan Constitution Check rows; tasks T002/T003 | Ledger L1, L2, L3 |
| (11) Exact counts detail=1 search+detail=2 +1 each; midnight retry per acquisition; carry denied-attempt DB cost + mismatched-host precondition; no cache/startup-log | spec US3 4, Edge Cases, Assumptions, FR-007; plan Risks/Summary; tasks T014/T023/T024/Notes | Compass L1; Sentry L1, M2 (closed carry), L2 (closed carry) |

## Plan-challenge ROUND 2 FINAL — narrow documentation residuals

| Fix # | Applied to | PLAN_CHALLENGE IDs closed by this edit |
|---|---|---|
| (1) Ledger L1 residual: Constitution Check async row label → IX (async testing) / Coding Conventions / PR quality gates; cite `:409-410`, `:475`, `:540-542`; retain token text | plan.md Constitution Check row | Ledger L1 |
| (2) Compass M3 / Ledger M2 comparison clarity: mark amendment Sync Impact code block as illustrative proposal summary (not exact staged artifact / not pending constitution edit); FR-008/FR-010/T035/amendment verification compare actual Gate-1-approved staged constitution (version, I/VII/VIII, its Sync Impact Report), not literal equality to illustration; ADR delivery per CONCLUSIONS PC1/PC2; date-only mismatch recorded only; no constitution mutation; no new owner question | constitution-amendment Sync Impact heading + Staging verify; spec FR-008/FR-010; tasks T035; this receipt; checklist | Compass M3; Ledger M2 |

**Prior remediation (still in force)**: C1/C2 amendment path; I1 fixture-with-step sequencing (now tightened to enumerated files); I2 preparation-in-spec-PR timing — **PC2 supersedes I2 date-reconcile / same-delivery ADR publish wording only**. Round 1 behavioral / naming / fixture / test-evidence corrections remain accepted. Recon exact paths at `recon-DEV-426:91-102`.

**Decisions invented by Quill**: none. Behaviour taken only from brief/CONCLUSIONS/ticket/recon facts, Patron PC1/PC2, and Keel’s numbered fix list.

**File names (PC1; authorised folders only)**:

- `DailyRequestUsageRecord` / `DailyRequestUsageConfiguration`
- `EfMetadataRequestGate`
- `MetadataQuotaHandler`
- `MetadataQuotaExhaustedException` / `MetadataQuotaPersistenceException` (one file each)
- Retained exact: Core `IOmdbRequestGate`, `OmdbQuotaOptions`, table `omdb_daily_usage`

**needs decision:** none.

**needs recon:** none for the enumerated T004/T012/T020/T021/T022/T023/T024 paths above (paths verified against worktree + recon-DEV-426:27,42-52,79). If later work discovers additional suites that hard-require quota isolation beyond that set, stop and request recon rather than expanding ownership silently.

**Clarify**: coverage Clear; no `[NEEDS CLARIFICATION]` markers; Clarifications section notes C1/C2 and PC1/PC2 supersessions; Gate 1 checkbox open.

**Status**: drafts remediated for plan-challenge **ROUND 2 FINAL** (cap 2 exhausted). Unresolved residuals: **none**. Gate 1 not approved. Quill does **not** claim PLAN FROZEN or owner approval. No production gates run. Constitution file not edited by Quill (R2).

**Next**: Keel independently verifies R2 residual passages; plan freeze / Gate 1 remain owner/Keel.
