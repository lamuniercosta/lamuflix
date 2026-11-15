# DEV-471 Phase 2 Spec Kit Drafting Receipt

**Date**: 2026-11-15  
**Seat**: Quill  
**Worktree**: `F:\Dev\LamuFlix.worktrees\feature-471-spec` · branch `feature/471-spec`  
**Method**: Spec Kit `/speckit-specify` → `/speckit-plan` with `-clarify -checklist` → `/speckit-tasks` from the closed grill in one ask; then repair round 1 against `QUILL_REPAIR_ROUND1.md` after Patron groups 1–6 + provenance ruling. Grill already closed (`brief.md` Q1–Q7). `.specify/feature.json` already pointed at `specs/DEV-471` and was left unchanged. `$env:PYTHONUTF8=1` set for the drafting shell. `before_specify` grill hook treated as already satisfied by closed grill. `after_tasks` analyze hook left for Keel (Quill does not run analyze).

**Inputs**: grill-closed `specs/DEV-471/brief.md` (Q1–Q7 plus plan-challenge decision record and groups 1–6 amendment), rulings in `CONCLUSIONS.md` / `ASSUMPTIONS.md`, `PLAN_ADJUDICATION.md`, proposed ADR `docs/adr/0043-unmasked-compose-startup-and-stubbed-smoke.md` (Keel-authored; cite only), Maestri notes `DEV-471` and `recon-DEV-471` (retained DEV-440 lines 1–25 plus appended plan-adjudication lines 27–73; verbatim ticket body 36–48), `specs/PRODUCT.md`, `QUILL_REPAIR_ROUND1.md`. Constitution cited only for plan gates; Quill did not edit constitution.

**Artifacts written / repaired (Quill-owned)**:

- `specs/DEV-471/spec.md`
- `specs/DEV-471/plan.md`
- `specs/DEV-471/tasks.md`
- `specs/DEV-471/checklists/requirements.md`
- this receipt

**Keel reviewer-owned (not edited by Quill in repair round 1)**: `specs/DEV-471/checklists/compose-smoke.md` — Keel independently completes per-item verdicts after repairs.

**Not written**: `research.md`, `data-model.md`, `contracts/`, `quickstart.md` — decisions frozen in brief as amended; no public API/schema design delta; remaining fixture/route/media/mount identities are Phase B recon facts (`brief.md` Closure receipt; groups 2/6).

**Not edited by Quill**: `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `PLAN_ADJUDICATION.md`, `QUILL_REPAIR_ROUND1.md`, `docs/adr/0043-unmasked-compose-startup-and-stubbed-smoke.md` (Keel), constitution, production/test code, `harness.yml`, generated OpenAPI/TS, `CONTEXT.md`, Compose files, `ci.yml`, `promote-home.yml`.

**Clarify**: Session 2026-11-15 recorded in `spec.md`. No `[NEEDS CLARIFICATION]` markers. Interactive clarify not required — Q1–Q7 plus Patron groups 1–6 already freeze inventable behaviour. Provenance correction session recorded under Repair round 1 in `spec.md`.

**Checklist**: Specification quality checklist `checklists/requirements.md` (revalidated after repair round 1). Domain requirements-quality checklist `checklists/compose-smoke.md` remains Keel-owned (CHK001–CHK027; reviewer marks after repairs; no blanket tick or runtime PASS).

**Owner checkbox**: none (zero §2.3(a)/(b) escalations per brief / Q1–Q7 / groups 1–6; playback retained as justified verification / no scope change; Sentry M5 remove-required-check rejected as noted/no ticket). Ordinary owner Gate 1 remains pending after Bernstein coordination / Rigger preparation-opening of the spec PR and verified user merge. Quill does not invent PLAN FROZEN. Spec Gate 1 is independent of pending SC-006/T011.

**Decisions invented by Quill**: none. Behaviour and boundaries taken only from brief/CONCLUSIONS/ASSUMPTIONS/recon/PRODUCT/plan-adjudication Patron rulings. Exact import/route/event/media/mount names, Chromium fixture decodability, helper/gate flag surfaces, and live controlled-revert execution remain Phase B evidence work — not product guesses.

## Repair round 1 closure map (QUILL_REPAIR_ROUND1.md → artifacts)

| Repair item | Findings covered | Closure location |
|---|---|---|
| 1 Provenance and scope | Compass L3/L7 + provenance discrepancy | `spec.md` Clarifications / Scope §6 / vocabulary / FR-007 / SC-007; `plan.md` Approach provenance; `tasks.md` Conventions + T006; this receipt Inputs/Assumptions |
| 2 CI structure/file authorization | Ledger H1/M1/L2 | `spec.md` Scope §1/§12 / FR-001/FR-018; `plan.md` Project Structure; `tasks.md` T007/T008 |
| 3 Disposable staging and readiness | Sentry H1/H2/H4/L1; Compass L4/L8 | `spec.md` Scope §2–3/§7 / FR-002–FR-003/FR-008 / SC-002/SC-004; `plan.md` Disposable staging section; `tasks.md` T001–T003 |
| 4 Safe diagnostics and cleanup | Sentry H3/M3; Ledger M3 | `spec.md` Scope §10 / FR-011; `plan.md` diagnostics; `tasks.md` T002/T008/T009 |
| 5 Acceptance and nonregression | Compass M1; Sentry M2 | `spec.md` Scope §6 / US3.4 / FR-007/FR-014 / SC-007; `tasks.md` T006/T009 |
| 6 Proof and budget enforcement | Sentry M4; Ledger L1/L3; Compass L2 | `spec.md` Scope §7/§11 / FR-008/FR-012/FR-016; `tasks.md` T002 (`budgets.json`)+T003–T004/T008/T010/T012; T005 excludes budgets |
| 7 Protection and rollback | Sentry M1/M5; Compass L1 | `spec.md` Scope §14 / FR-015 / SC-006; `tasks.md` T011/T014 |
| 8 Review receipts | Compass L5/L6 | This receipt (ADR pointer + closure map); `plan.md` Status/ADR note; `checklists/requirements.md` Notes; compose-smoke checklist left for Keel |

All 26 PLAN_ADJUDICATION findings are covered by the eight consolidated items above (duplicates remain individually traceable in adjudication). Sentry M5 disposition is noted/no ticket — no Quill ticket write.

## Requirement / task closure pointers

- FR-001 ↔ T008; SC-001 ↔ T008/T009
- FR-002–FR-003 ↔ T002; SC-002 ↔ T002/T010
- FR-004–FR-007 / FR-018 ↔ T005–T007; SC-003 / SC-007 ↔ T006
- FR-008 / FR-019 ↔ T002–T003/T008/T010; SC-004 ↔ T003/T010
- FR-009–FR-011 / FR-014 ↔ T002/T008/T009; SC-005 ↔ T009
- FR-012 ↔ T002 (`web/e2e/fixtures/dev471/budgets.json` before first runner use) / T008 (workflow prep from that file); T005 excludes `budgets.json`
- FR-013 ↔ T001 STOP + authorized file set in plan/tasks
- FR-015 ↔ T011; SC-006 ↔ T011 (pending keeps Done/merge-bar open)
- FR-016 ↔ T012
- FR-017 ↔ T013

## Remaining pickup facts (not inventable decisions)

These are Phase B evidence obligations already ruled as first-task facts — not open design decisions:

- Exact library mount input and route/event/media identities (`T001` STOP if unavailable).
- Chromium fixture/sample media decodability for advancing playback.
- Helper/wrapper/gate help surfaces (`run-e2e-smoke.ps1`, `_gate-common.ps1`, `run-web-gates.ps1` flags) verified at pickup before use.
- Whether any existing upload helper within authorized paths is reusable (`recon-DEV-471:55` currently shows evaluation-metrics only — do not claim helpers exist until verified).
- Live candidate-positive / revert-negative `-RegressionProof` execution (explicitly not claimed exercised).
- Authorized operator/owner verification that `main` protection requires the actual published `compose-smoke` context (`recon-DEV-471:70` does not evidence required checks).
- Keel compose-smoke checklist per-item verdicts after repair verification.
- Keel PLAN FROZEN / Gate 1 process (ordinary owner Gate 1 on spec PR).

## needs decision (to Keel)

None. Patron approved provenance and groups 1–6; brief records them. No inventable behaviour remains unsettled for Quill. Outstanding items above are pickup facts / operator evidence / Keel process, not new undecided design.

## Notes

- ADR 0043 remains proposed until Gate 1; Quill referenced it and did not reauthor. Pre-Gate 1 ADR-review receipt points to `PLAN_ADJUDICATION.md` (Compass L5 content alignment).
- Existing CI job `e2e-smoke` is a distinct coexisting check/context from ticket-required `compose-smoke` (recon confirms a job, not required registration); dedicated workflow coexists; ordinary npm e2e must exclude DEV471. Reserve **required** for compose-smoke ticket requirement and actual operator verification.
- Round 2: budget ownership ordered per brief — T002 creates `budgets.json`; T005 owns fixture media/settings excluding that file; disjoint and parallel after T001; no T002→T005 dependency.
- Stale drafting claims of a current 25-line-only recon note are corrected: recon retains 1–25 and appends 27–73.
- Stale Q6 `ci.yml` edit authorization is superseded by dedicated `compose-smoke.yml`.
- No statement in Quill-owned artifacts claims runtime, gates, or protection PASS from planning.
- Report repair round 1 complete to Keel; freeze/analyze/checklist verdicts belong to Keel next.
