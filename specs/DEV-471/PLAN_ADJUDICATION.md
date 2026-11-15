# DEV-471 Phase 2 step 6 plan adjudication

Date: 2026-11-15. Seat: Keel. Worktree: `F:/Dev/LamuFlix.worktrees/feature-471-spec`; verified branch: `feature/471-spec`.

Status: **CLOSED — PLAN FROZEN after independent verification; see final receipt below**. All three axis reports read: Sentry (10 findings), Ledger (7), Compass (9): 26 findings total. Two repair rounds used (2/2); final verification below supersedes historical pending entries. Adjudication is distinct from repair verification, Gate 1, runtime acceptance and merge clearance.

Evidence: named `brief.md`, `spec.md`, `plan.md`, `tasks.md`, `CONCLUSIONS.md`, ADR 0043; the three named challenge notes; current `recon-DEV-471` lines 1–25; `checklists/compose-smoke.md`. Worktree already contained uncommitted drafting artifacts; preserved. No production-code inspection, measurement or edits performed. Source locations below refer to the pre-repair draft. Challenge-reported repository facts not in recon are explicitly unverified, not adopted as reconnaissance.

## Risk axis — Sentry

| ID | Disposition and evidence | Required closure |
|---|---|---|
| H1 | Accepted plan gap: `tasks.md:67` binds neither disposable mechanism nor checkout-state receipt. Q3/Q5 require checkout isolation. | Quill specifies candidate-HEAD disposable copy outside checkout, reverse only the exact production fix patch there (stop on mismatch), distinct project and bounded teardown; record source/patch identities and unchanged checkout baseline including preexisting changes. Do not run the historical parent as the candidate. |
| H2 | Accepted: `tasks.md:67-68`, `spec.md:188` name expected cause without an auditable classifier. Recon:15,20,23,25 establish only the expected regression, not live proof. | Classify proof only after Docker/build/settings and real dependency/model readiness succeed; same candidate health assertion never succeeds on revert; diagnostics establish the localhost/embedding startup cause. Reject infrastructure failures and unexplained timeouts. Non-139 failures need the same causal evidence; 139 alone is insufficient. Record prerequisites, health outcome, exit status, cause and source identity. Exact service/readiness seams require recon. |
| H3 | Accepted: `tasks.md:66,94,106` promise sanitized uploads without scrub/verification contract. | Require allowlisted upload staging, redaction of actual generated secret values and credential/connection-string fields, no raw settings/env dumps, fail-closed upload guard, and planted-secret failure-path proof across upload payload and console output. Screenshot/trace/video artifacts containing secrets must be excluded or safely regenerated; text replacement alone cannot sanitize them. Existing reuse seam requires recon. |
| H4 | Accepted design feasibility gap: Q4 fixture isolation + Q6 unchanged Compose + `tasks.md:56,66,81` do not pin a workable bootstrap. Shipped sample-mount fact is challenge-reported, absent from current recon. | Obtain bounded recon before final plan selection: unchanged-Compose settings/library bootstrap for both phases, isolated fixture identity/data and no checkout mutation. Then Quill pins the proven seam; stop for Patron if impossible. Do not assert an existing override mechanism. |
| M1 | Accepted delivery-status ambiguity: `tasks.md:108,131`, `spec.md:195,214`. | Pending required-check verification keeps SC-006 and ticket Done open; distinguish spec Gate 1 from implementation merge sequencing. Patron rules timing using recon of available registration mechanism. Never invent verification or operator authority. |
| M2 | Accepted: `tasks.md:82,106,119` lacks explicit old-runner non-regression evidence. | Add deliberate existing-runner failure check and required-smoke operational-failure checks proving exit 1, never inherited exit-2 green/SKIPPED. Preserve existing local skip semantics outside required smoke. |
| M3 | Accepted: `tasks.md:66,94` lacks cancellation fallback. | Track every created project before startup; runner finally/termination handling plus CI always-run project-scoped cleanup with time reserved inside job cap. Record cancellation/failure cleanup attempts. Do not promise cleanup after hard runner loss; cleanup failures stay failures. |
| M4 | Accepted: `tasks.md:107` documents repetition but no named invocation/gate. | Name explicit regression-proof runner mode and change-triggered evidence gate for startup/health logic; ordinary unrelated PRs run two positives only. Patron settles enforcement choice; do not silently add PR-template files outside Q6. |
| M5 | Proposed remove-required-check kill switch **not accepted as an authorized repair**: contradicts unconditional-required Q2/Q5 policy unless separately ruled. No evidence establishes a ticket-mandated rollback deliverable. | Patron disposition needed: recommend follow-up operational concern, retain strict check and operator escalation. No plan instruction to remove protection and no admin mutation authorized. A ticket-scope departure, if judged necessary, follows the owner-checkbox path. |
| L1 | Accepted duplicate of H2: `tasks.md:67` leads with 139. | H2 repair leads with attributable unhealthy API after readiness; exit 139 is corroboration only where observed. |

## Standards axis — Ledger

| ID | Disposition and evidence | Required closure |
|---|---|---|
| H1 | Acceptance risk accepted; reported existing workflow-level paths-ignore/promote-home coupling is **not verified in recon**. `tasks.md:94` cannot solve a workflow-level bypass merely by adding an unconditional job. | Recon must supply exact trigger/filter/coupling facts. Patron then selects and authorizes a bounded always-run workflow design/file set; Quill updates brief-derived artifacts. A separate workflow needs Q6 amendment. No invented CI edit or freeze before that decision. |
| M1 | Accepted relationship gap: `plan.md:108`, `tasks.md:82,94`. Existing e2e-smoke job identity/selection is challenge-reported only. | Confirm existing job, npm glob and runner selection in recon; Patron chooses coexistence/reuse and DEV471 inclusion/exclusion, then Quill states relationship and prevents accidental double execution. |
| M2 | Accepted documentation/convention gap: `tasks.md:66`; Q5 explicitly overrides required-smoke skips. Claimed script header/helper conventions need recon. | Pin verified existing header/help/helper pattern; document deliberate required-smoke exit 1 for unavailable Docker, without changing unrelated gate semantics. |
| M3 | Accepted sanitization reuse requirement, consolidated with Sentry H3; exact allowlist/block/upload machinery not in recon. | Recon identifies safe reusable mechanism and authorized extraction seam. Prefer reuse within Q6; any additional existing-file edit needs Patron authorization. Distinct artifact identity compose-smoke evidence. |
| L1 | Accepted single-source budget concern: `tasks.md:94` repeats Q5 budgets without configuration ownership. | State one runtime budget source with validated inputs and workflow/job cap derived from it, preserving ASSUMPTIONS values. Recon confirms feasible CI evaluation seam; no new dependency or arbitrary threshold. |
| L2 | Accepted: `tasks.md:94,108` does not fix id versus displayed check context. | Pin job id and displayed name `compose-smoke`; operator receipt verifies actual published required context, not inferred id alone. |
| L3 | Accepted, consolidated with Sentry M4 for repeat enforcement; `tasks.md:119` also leaves web gate scope vague. | Named proof mode/change gate; explicit `run-web-gates.ps1` scope using verified supported flags from recon, never invented command options. |

## Spec axis — Compass

| ID | Disposition and evidence | Required closure |
|---|---|---|
| M1 | Accepted acceptance ambiguity: `spec.md:187,215`, US3 scenario 4, `tasks.md:81` leave 403 discharge unclear. | Patron rules active stubbed-phase nginx POST/403 assertion versus inspection-only constraint; recommend active assertion on imported fixture via existing route, LocalPlay false, without process launch. Record ruling in brief/CONCLUSIONS before Quill repairs. |
| L1 | Accepted timing ambiguity, consolidated with Sentry M1: `spec.md:195,214`, `tasks.md:108,131`. | Recon registration feasibility, then Patron timing ruling; keep ticket Done open until verified even if implementation publication precedes it. Do not infer that checks can only be registered after merge. |
| L2 | Accepted evidence freshness ambiguity: `tasks.md:68,107,125`. | T004 is development proof; T010 final implemented-candidate proof supersedes it. Refresh after invalidating changes; preserve source identity and do not claim earlier evidence is final-HEAD proof. |
| L3 | Accepted provenance gap, consolidated with Ledger M1. | Recon actual CI job and npm selection; remove unsupported factual assertions until verified. |
| L4 | Accepted bootstrap gap, consolidated with Sentry H4. | Recon plain-phase ephemeral settings/credentials mechanism without Compose edits or checkout mutation before pinning it in plan. |
| L5 | Accepted receipt gap: `brief.md:78`, `tasks.md:47`, ADR 0043:1–9. | This adjudication records ADR content review: two-phase boundary, unchanged LocalPlay, identical disposable health oracle and cost tradeoff match Q2–Q6. ADR remains proposed; final receipt must confirm repaired plan still matches ADR before freeze/Gate 1. Add pre-Gate-1 receipt pointer. |
| L6 | Accepted checklist-process gap: `checklists/compose-smoke.md:12-56` unchecked. | Complete reviewer per-item verdicts with evidence and honest unresolved labels after repairs. No blanket tick or runtime PASS; CHK027 is a gap question, not automatically an affirmative pass. |
| L7 | Provenance-hardening request accepted, **no ticket contradiction established**. Q2 normalized record remains authority; old line numbers must not be applied to replacement note. | Ask Wisp through Conductor to preserve verbatim current DEV-471 scope/technical design/Done when in recon and compare against Q2; do not directly query tracker or infer change. |
| L8 | Accepted oracle-clarity gap: `spec.md:183`, scope 2, SC-002. | State phase-1 API health plus real dependency/model readiness; web/nginx functional coverage is phase 2. Patron confirms this operationalization preserves ticket healthy-startup wording, using authoritative ticket recon. |

## Repair dispatch and freeze bar

No finding is marked repaired. Duplicates remain individually traceable above. Quill receives one numbered, consolidated repair list per round after the missing facts and Patron decisions are available. Maximum two rounds; no new challenge round is invented by requesting evidence. Keel independently rereads amended files and checklist before closure. Any new decision must be recorded in brief first; Quill owns spec/plan/tasks repairs.

Before freeze: bounded recon resolves CI trigger/selection/upload conventions and the unchanged-Compose settings/isolation feasibility; Patron records cited CI/file-authorization, 403 discharge, registration timing, repeat-proof enforcement, healthy-startup interpretation and M5 dispositions; Quill repairs artifacts; Keel verifies all dispositions, checklist and ADR consistency. Applicable read-only cross-artifact analysis follows its skill when run. No runtime or branch-protection PASS is claimed at planning stage.

## Pending recon request

needs recon: In the named worktree, provide (1) exact ci.yml pull_request trigger/path filters and reported promote-home coupling, existing e2e job/displayed context/npm selection, existing sanitized-upload and script/helper/web-gate interfaces; (2) concrete existing settings/credential and empty-fixture-library bootstrap for plain shipped Compose and stubbed overlay with no Compose edits or PR-checkout mutation, plus dependency/model readiness seams for the expected-cause classifier; (3) verbatim DEV-471 Scope & Technical Design/Done when and current compose-smoke required-check registration feasibility/timing. Preserve this alongside existing DEV-440 evidence rather than replacing it. No secrets or credential values in recon.

## Resume evidence update — recon lines 27–73

The previous pending recon request is historical, answered in part by the appended snapshot at HEAD `0e13095865948a342ffe30b4279e059c66440eab`; its known limits are explicit below. No additional production search was performed.

- Ledger H1 factual basis confirmed by recon:53: workflow-level filters and promote-home coupling. Every-PR wording is verbatim recon:44. New dedicated workflow is a proposed Patron file-scope ruling, not yet approved.
- Ledger M1 / Compass L3: recon:54,59 confirms e2e-smoke invoking npm run e2e and run.mjs entrypoint. Exact current selection details are not established; explicit future selection is a design obligation, verified at pickup rather than falsely described as existing capability.
- Sentry H4 / Compass L4: recon:64–67 disproves an existing ephemeral plain-settings override: base mounts fixed docker/api.settings.json; overlay alone has E2E_SETTINGS_FILE. Proposed remedy is disposable candidate source staging with generated runtime settings at the fixed mount path, unchanged positive Compose/nginx, subject to Patron approval. No positive runtime feasibility is claimed.
- Ledger M3: recon:55 establishes evaluation-metrics upload only; it does not confirm Ledger's named upload helper/guard pattern. Reuse assertion is unproven and must not be copied into the plan as fact. Required safe staging/allowlist/blocking is still an accepted design repair; existing helper reuse only after explicit pickup verification.
- Sentry H2 readiness evidence now includes Postgres/RabbitMQ health and real Ollama/model readiness (recon:65 plus Q3/Q5); precise probes remain T001 facts, not an unbound regression classifier.
- Compass L7: verbatim ticket now preserved at recon:36–48. **New provenance discrepancy**: it does not mention play, unlike Q2's normalized ticket-mandate wording. Patron scope ruling requested; prior no-contradiction/no-checkbox statements cannot be reaffirmed until that ruling. Keep append-only history and correct current authority explicitly.
- Sentry M1 / Compass L1: recon:70 does not evidence required status checks in the accessible branch-protection response. It does not prove other enforcement is absent or that registration requires a merge. Operator verification remains an open implementation acceptance obligation, not a planning PASS.
- No Quill round dispatched. Patron questions are in `PATRON_PLAN_QUESTIONS.md`; the initial playback-provenance question was sent separately. PLAN FROZEN remains withheld.

## Decision resolution receipt — before Quill round 1

Patron appended both provenance and groups1–6 rulings in CONCLUSIONS.md, recorded by Keel in brief before dispatch. No structural block/owner checkbox. Ledger H1 approved resolution is new dedicated unfiltered compose-smoke.yml, replacing ci.yml edit authorization; recon:53 confirms coupling. Sentry H4/Compass L4 resolved at design level by disposable candidate source runtime settings prep (recon:64); live feasibility and exact library mount input remain explicit T001 STOP facts. Sentry M5 is **rejected as ticket contradiction; noted, no ticket**, per Patron, not an accepted rollback repair. Ledger M3 named-existing-helper assertion is unproven (recon:55); close reuse demand as unsupported, retain required safe staging/redaction design. Compass L7 provenance request supplied at recon:36–48; play attribution corrected by Patron. All other accepted rows require Quill amendment and Keel verification, not merely decision approval.

Quill round 1 dispatched with numbered QUILL_REPAIR_ROUND1.md (eight consolidated groups), owners explicit; no round2 used. PLAN FROZEN withheld during repair. No runtime/protection acceptance claimed.

## Independent round 1 verification / analysis report

Keel read amended spec/plan/tasks/DRAFTING_RECEIPT (not delegate self-report alone), checked brief/Patron/recon correspondence and constitution active principles I–IX/stack/gates. SpecKit prerequisites succeeded with FEATURE_DIR=specs/DEV-471 and tasks.md present. extensions.yml has no before_analyze/after_analyze hooks; read-only analysis executed without file mutation during the analysis pass. No production, runtime or gate command was run.

Inventory: FR001–FR019 (19) + SC001–SC007 (7) =26 requirements; T000–T014 =15 tasks; all26 have task coverage (100%); unmapped tasks:none (T000 governance; T001 recon; others directly FR/SC). No constitution conflict or missing core artifact. No duplicated acceptance obligation requiring removal. Two residual issues only: A1 Medium task ordering T002/T004 consume budget config created at later/parallel T005; A2 Low spec Scope1 falsely calls existing e2e-smoke a required-check context. The original accepted design repairs are present; budget ordering is a remaining closure detail for Ledger L1.

Keel recorded A1 ownership decision in brief before final round dispatch: T002 creates budgets.json before first use, T005 excludes it. Quill receives exactly two numbered fixes in QUILL_REPAIR_ROUND2.md. Round2/2 dispatched, no round3 authorized. Remaining freeze bar: independently reread final amendments, complete reviewer checklist, record final ADR match and row-by-row closure. PLAN FROZEN withheld until then.

## Final independent verification — PLAN FROZEN

Keel independently reread Quill round2 amendments and affected spec scope/FR012, plan order/structure, tasks T002/T005/T008/dependencies, and DRAFTING_RECEIPT mappings. A1 closed: T002 creates budgets.json before first runner use; T005 excludes it and stays disjoint/parallel after T001; no cycle. A2 closed: existing e2e-smoke is described as a coexisting check, without unsupported required-registration claim. Both rounds complete (2/2), no further repair round needed. Source locations below refer to the final repaired artifact, superseding initial pre-repair locations and pending language above.

### Every challenge finding — final disposition

| Axis / ID | Final disposition | Verified closure evidence |
|---|---|---|
| Sentry H1 | Repaired | tasks T002/T003, plan Disposable staging: candidate source copy, reverse-only patch, checkout baseline receipt |
| Sentry H2 | Repaired | spec FR008/SC004, T003: readiness prerequisite + expected causal classifier + receipt fields; no live proof claimed |
| Sentry H3 | Repaired | spec FR011/FR014, T002/T008/T009: redaction before stdout/upload, allowlist/guard, unsafe binary exclusion, planted-secret proof |
| Sentry H4 | Repaired at planning level | Patron group2, brief, T001/T002/T006: disposable fixed-path settings prep, isolated fixture identity, exact mount verification STOP; no invented working runtime |
| Sentry M1 | Repaired | FR015/SC006, T011/T014 block Done and implementation merge-bar pending operator verification |
| Sentry M2 | Repaired | FR009/FR014, T007/T009 existing-runner failure nonregression and preserved local skip semantics |
| Sentry M3 | Repaired | FR010/FR011, T002/T008/T009 project registration, finally/always-run fallback, reserved cap time, hard-loss limit |
| Sentry M4 | Repaired | FR008, T003/T008/T010 named RegressionProof and changed-startup-code invocation, unresolved-base runs proof |
| Sentry M5 | Rejected; noted, no ticket | Patron group4: removing required check contradicts ticket; no admin/rollback authority added |
| Sentry L1 | Repaired with H2 | T003 leads with never-healthy API after ready plus expected cause;139 corroboration only |
| Ledger H1 | Repaired | Patron group1, FR001, T008 dedicated unfiltered workflow, exact job id/name; ci/promote-home untouched |
| Ledger M1 | Repaired | FR018, T007/T008 explicit ordinary-e2e exclusion/smoke opt-in; distinct coexisting e2e-smoke |
| Ledger M2 | Repaired | FR019, T001/T002 helper-interface verification, CmdletBinding/Help and strict required-smoke exits |
| Ledger M3 | Partially rejected fact claim; safety repair complete | Claimed existing named upload machinery unverified by recon55, not adopted. FR011/T008/T009 require new safe staging/guard within authorized files, reuse only after T001 evidence |
| Ledger L1 | Repaired including A1 | FR012, T002 budgets.json before use, T008 prep outputs, T005 explicitly excludes config; approved values unchanged |
| Ledger L2 | Repaired | FR001, T008 id/name compose-smoke; T011 actual published required context verification |
| Ledger L3 | Repaired | FR008/FR016, T008 proof trigger and T012 explicit web gate/typecheck with supported flags verified at pickup |
| Compass M1 | Repaired | Patron group3, FR007/SC007, US3.4/T006 active shipped-nginx imported-fixture POST403, LocalPlay false |
| Compass L1 | Repaired | Patron group4, FR015/SC006, T011 spec Gate1 independent, no postmerge-only assumption |
| Compass L2 | Repaired | T004 developmental, T010 final candidate supersedes and T013 refresh after invalidating changes |
| Compass L3 | Repaired | recon54/59 confirms e2e-smoke/npm entrypoint; active artifacts correct role/selection, no unsupported required-registration claim (A2) |
| Compass L4 | Repaired at planning level | recon64 + Patron group2, T001/T002 disposable fixed-source settings prep rather than invented ephemeral base override |
| Compass L5 | Repaired | plan ADR/PhaseA, tasks Phase0, DRAFTING_RECEIPT point here; final ADR content review below |
| Compass L6 | Repaired | checklists/compose-smoke.md: all27 reviewed with per-item evidence and explicit NO answer for negative-polarity CHK027; no runtime PASS |
| Compass L7 | Repaired | recon36–48 preserves verbatim body; Patron provenance ruling and spec Clarifications correct playback attribution to summary+Q2/Q4 |
| Compass L8 | Repaired | Patron group3, FR002/FR003/SC002, plan Phase coverage: API/dependencies phase1 and real web/nginx flow phase2 |

### Final read-only cross-artifact analysis

Prerequisite command previously succeeded for exact feature directory; no analyze hooks registered. Final targeted reread confirms no unresolved inconsistency, ambiguity or coverage finding. Inventory:19 FR +7 SC =26 requirements,15 tasks, coverage26/26 (100%), unmapped tasks0, unresolved ambiguity0, actionable duplication0, Critical0. All items trace to brief/Patron decisions or declared pickup facts; no constitution departure found. The prior two findings A1/A2 are closed above. No further remediation list.

Coverage: FR001→T008; FR002/003→T002/T006/T008; FR004/005/006/007→T005/T006; FR008→T003/T004/T008/T010; FR009/010/011→T002/T007/T008/T009; FR012→T002/T008; FR013→T001 plus bounded source tasks; FR014→T009; FR015→T011/T014; FR016→T012; FR017→T013; FR018→T007/T008; FR019→T002/T003. SC001→T008/T009; SC002→T002/T010; SC003→T006/T010; SC004→T003/T010; SC005→T009; SC006→T011/T014; SC007→T006/T012. T000 is owner Gate1 receipt; T001 recon/governance, both intentionally build-blocking.

ADR0043 final content reviewed: separate plain startup vs stubbed flow, same disposable candidate health oracle, expected startup cause, unchanged LocalPlay/nginx, accepted real-model and second-lifecycle cost remain consistent with brief/Q2–Q6 as amended. Status remains proposed. Dedicated workflow/bootstrap refinements do not change the ADR decision; no reauthoring required.

**PLAN FROZEN — Phase2 step6 clean after two repair rounds.** All26 challenge findings dispositioned and all accepted repairs verified, plus A1/A2 closed;27 checklist items reviewed. No unresolved plan finding remains. Ordinary owner Gate1 on the spec PR is still pending and blocks T001+; no implementation, runtime, harness-gate or protection verification is claimed. T001 pickup facts/STOP rules and T011 operator acceptance remain future required work. No merge or tracker mutation authorized/performed by Keel.

