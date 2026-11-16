---

description: "Task list for DEV-471 required Compose smoke"
---

# Tasks: Required Compose smoke with unmasked startup and stubbed full flow

**Input**: Design documents from `specs/DEV-471/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q7 plus plan-challenge groups 1–6), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, `PLAN_ADJUDICATION.md`; proposed ADR `docs/adr/0043-unmasked-compose-startup-and-stubbed-smoke.md`; Maestri notes `DEV-471`, `recon-DEV-471` (retained 1–25 + appended 27–73; verbatim ticket body 36–48); `specs/PRODUCT.md`.

**Status**: PLAN FROZEN. Gate 1 spec PR #414 merged; T000 and T001 complete (receipts synchronized 2026-11-16). Anvil cleared for local Phase 2 T002-T004 on Chromium probe evidence in recon-DEV-471:12-35. See brief.md T001 closure and Phase 2 dispatch clearance. Compose runtime acceptance, implementation gate results, branch-protection verification and merge clearance remain open.

**Tests**: Required (`brief.md` Full-flow acceptance / Task ordering / Gates / groups 1–6; FR-004–FR-005, FR-007–FR-008, FR-014). Real Compose positive phases and controlled-revert proof are mandatory. Runner failure-path proofs do not replace them. No application API/hub mocks. No new test framework/dependency.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file edits are never `[P]`. Shared gate/proof/CI tasks are never `[P]`.

**Conventions**: Edit only amended Q6 authorized paths (dedicated `compose-smoke.yml`, not `ci.yml`); reuse `docker-compose.yml` and `web/e2e/compose.e2e.yml` unchanged; leave `ci.yml`/`promote-home.yml` untouched; STOP for recon/Patron if Compose edit, missing production capability, or unavailable mount/route/event/media identity is required; preserve LocalPlay false and nginx process-play 403 (active Phase 2 POST); no committed secrets; no PR-checkout mutation; no explanatory comments beyond AGENTS exemptions; never invent fixture IDs/route names — collect via recon; verify helper/gate flags via help at pickup before use. Browser play provenance = ticket summary + Patron Q2/Q4 (verbatim body `recon-DEV-471:36–48` does not name play). Otherwise-unassigned implementation work is Build-lane owned.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` / build dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel T000 records the verified merge receipt. No Build work (T001+) before Gate 1. Spec Gate 1 is independent of pending SC-006/T011.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (Wisp pickup recon + Keel reanalysis + Gauge baseline) — blocks T002+
Phase 2   T002 → T004   (US2 budgets.json + plain startup runner + disposable -RegressionProof mode + diagnostics/cleanup; T004 developmental)
Phase 3   T005 → T007   (US3 stubbed full-flow e2e + active 403 + minimal run.mjs exclusion/selection + fixture media/settings excluding budgets.json)
Phase 4   T008          (US4 dedicated compose-smoke.yml wiring)
Phase 5   T009 → T011   (US5 failure-path/planted-secret/existing-runner proofs + T010 final-candidate DEV-440 evidence + operator required-check verification)
Phase 6   T012          (npm run typecheck:e2e + scripts/run-web-gates.ps1 + harness gates with honest verdicts)
Phase 7   T013          (capped review / remediation per Q1 bar; all severities)
Phase 8   T014          (delivery / merge-bar handoff — user merges; pending T011 keeps Done/merge-bar open)
```

Ordering per brief Task ordering: recon → runner → stubbed tests → CI wiring → positive/negative validation. Gates after implementation evidence; review before delivery.

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt in `CONCLUSIONS.md`, or stop. No Build tasks before that receipt. Pre-Gate 1 ADR-review receipt points to `PLAN_ADJUDICATION.md` (ADR remains proposed).

- [x] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-471 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-471 in `specs/DEV-471/CONCLUSIONS.md`. Receipt only. T001–T014 MUST NOT proceed without verified merge (`brief.md` Gates and handoff; Q6–Q7; groups 4; no structural owner checkbox on this ticket). Spec Gate 1 is independent of SC-006/T011 and of implementation merge-bar.

---

## Phase 1: Pickup recon (blocks implementation)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; verify exact fixture mount input, route/event/media identities, helper/wrapper/gate interfaces via help, credential/settings setup, Chromium fixture decodability, and CI seams without inventing facts.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift/decision/missing production capability/unavailable required identity stops — no invention. Baseline captured.

**Closure (2026-11-16)**: Prior pickup reanalysis and baseline recorded in DEV-471:20-24,41-57; final Chromium fixture probe PASS, exit 0 in recon-DEV-471:12-35. Keel closes T001 and clears Anvil for T002-T004; see brief.md closure receipt for fixture identity and evidence limits. This closes pickup only; implementation/runtime/protection evidence remains due.

- [x] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying: shipped `docker-compose.yml` and `web/e2e/compose.e2e.yml` reuse points (including DEV-440 endpoint bindings `4646476f` / `010e0cb6` per `recon-DEV-471`); **exact fixture library mount input** and route/event/media identities required by fixture-correlated assertions; existing import/listing/enrichment/thumbnail/`/hubs/`/browser-play contracts and generated `web/src/api/types.ts` usage seams; runtime credential/settings bootstrap used by `web/e2e/run.mjs` and fixed `docker/api.settings.json` mount (`recon-DEV-471:64`); Chromium decodability of candidate fixture/sample media for advancing playback; nginx LocalPlay-false / process-play 403 preservation points; helper/wrapper/gate interfaces via `-Help` for `scripts/run-e2e-smoke.ps1`, `scripts/_gate-common.ps1`, and `scripts/run-web-gates.ps1` (do not invent flags); dedicated-workflow coexistence facts vs existing `e2e-smoke` / `npm run e2e` (`recon-DEV-471:53–55,59`). **Keel** reanalyzes brief/spec/plan/tasks against landed main and amends only for authorized demonstrated drift. **Gauge** records pre-edit gate baseline. **STOP** if exact mount input or required route/event/media identities are unavailable; **STOP** `needs recon` on missing/drifted seams required for acceptance; **STOP** for cited Patron ruling rather than editing Compose files or expanding §2.3 surfaces (`brief.md` Task ordering step 1; FR-006–FR-007, FR-013; Q4/Q6; groups 2/6). No production edits in this task. Do not claim unverified upload helpers exist (`recon-DEV-471:55`).

---

## Phase 2: User Story 2 - Plain startup runner and disposable DEV-440 proof mode (Priority: P1) 🎯 MVP

**Goal**: Create `web/e2e/fixtures/dev471/budgets.json` then `scripts/run-compose-smoke.ps1` disposable candidate-HEAD source staging, plain shipped startup with real dependency/model readiness, named `-RegressionProof`, redacted diagnostics, and project-scoped cleanup.
**Independent Test**: Candidate plain startup reaches healthy API after Postgres/RabbitMQ + real Ollama/model readiness; disposable reverse-only-`4646476f` fails identical health assertion for attributable localhost/embedding cause; checkout baseline including dirty/untracked unchanged; diagnostics/cleanup attempted; budget config present before first runner use.
**Order**: T002 → T004. **Build lane** owns otherwise-unassigned runner work. T004 is developmental proof; T010 is final-candidate evidence that supersedes it. T002 does not depend on T005.

- [x] T002 [US2] **Build lane**. After T001: Create `web/e2e/fixtures/dev471/budgets.json` with the single validated ASSUMPTIONS values (30m startup / 120s async / 3m teardown / 120m job; zero whole-test retries) **before** first runner use; then create `scripts/run-compose-smoke.ps1` with CmdletBinding/`-Help` and documented strict smoke exit semantics, implementing: disposable candidate-HEAD source copy outside the PR checkout; generated ephemeral `docker/api.settings.json` only inside that copy (`Features:LocalPlay: false`, no committed secrets); plain shipped `docker-compose.yml` startup without the e2e Ollama endpoint override; Postgres/RabbitMQ health then real Ollama/model readiness then API health; randomized Compose project isolation with projects registered before startup; sanitized redacted log/health capture (generated secret values and credential/connection-string fields redacted before stdout; no raw settings/env dumps); always-attempted project-scoped teardown via finally/termination with failure-on-cleanup-failure and reserved cleanup time inside job cap; runner reads budgets from `budgets.json` (FR-002–FR-003, FR-009–FR-012, FR-019; `brief.md` Approach phase 1 / Round 1 verification decision — budget ordering; Q3/Q5; groups 2/5/6). Do not edit Compose/nginx files. Do not mutate PR checkout. Do not claim unverified upload helpers. Do not own T005 fixture media/settings files.
- [x] T003 [US2] **Build lane**. After T002: Extend `scripts/run-compose-smoke.ps1` with named `-RegressionProof` mode that clones candidate HEAD outside the PR checkout, reverses only production commit `4646476f` (fail closed on patch mismatch; never use historical parent as candidate), runs the identical plain-stack health assertion, and classifies proof only after successful Docker/build/settings and healthy Postgres/RabbitMQ + real Ollama/model readiness: never-healthy API with attributable localhost/embedding startup cause (exit 139 optional corroboration only); reject infrastructure failures and unexplained timeouts; leave original checkout baseline including dirty/untracked state unchanged; record receipt fields prerequisites/health/exit/cause/source/patch (FR-008; `brief.md` Approach DEV-440 proof; Q3/Q5; groups 2/5; `recon-DEV-471:13,15,20,23`).
- [x] T004 [US2] **Build lane**. After T003: Developmental proof that candidate plain startup reaches healthy API and that `-RegressionProof` negative mode fails for the expected cause; record source/patch identities and observable cause in a scrubbed receipt (FR-008, FR-011; SC-002, SC-004). This evidence is developmental only and is superseded by T010 final-candidate proof. Missing live proof remains open — do not claim prior exercise. Refresh after any invalidating runner/health changes before treating as current.

**Checkpoint**: US2 independently demonstrable for plain startup + revert mode (developmental).

---

## Phase 3: User Story 3 - Stubbed fixture-correlated full-flow smoke (Priority: P1)

**Goal**: Separately isolated overlay stack assertions for import → listing → enrichment → thumbnail → hubs → advancing browser play + active process-play 403; minimal `run.mjs` exclusion/selection; fixture media/settings excluding `budgets.json`.
**Independent Test**: `web/e2e/dev471-compose-smoke.e2e.ts` fails closed unless all fixture-correlated checks pass under external-provider-only stubbing; excluded from ordinary `npm run e2e`.
**Order**: T005 → T007. T005 file set is disjoint from T002 (`budgets.json`); T005 remains `[P]` after T001 and does not create or own the budget config.

- [x] T005 [P] [US3] **Build lane**. After T001: Add only needed fixture media/assets/settings under `web/e2e/fixtures/dev471/**` based on T001 decodability/mount facts — **excluding** `web/e2e/fixtures/dev471/budgets.json` (owned by T002; disjoint file sets) — do not rewrite existing fixtures; do not invent undecodable media as accepted; do not scatter new threshold values or edit `harness.yml` (FR-004, FR-013; Q4/Q6; group 5; `brief.md` Round 1 verification decision — budget ordering).
- [x] T006 [US3] **Build lane**. After T004 and T005: Implement `web/e2e/dev471-compose-smoke.e2e.ts` establishing browser `/hubs/` through shipped nginx before import; importing a known fixture through the real stack; asserting fixture-correlated library listing, deterministic stub enrichment fields, nonempty decodable thumbnail, genuine server-originated `/hubs/` UI update without reload/polling substitution, and in-page playback with valid metadata and advancing `currentTime`; actively `POST /api/movies/<fixture-id>/play` through shipped nginx for the imported fixture and require HTTP 403 with LocalPlay false and no process launch; forbidding application API/hub mocks (FR-004–FR-007; SC-007; SMOKE contract in brief; Q2/Q4; group 3). Use existing generated types only. Browser play provenance = ticket summary + Patron Q2/Q4 (not verbatim body).
- [x] T007 [US3] **Build lane**. After T006: Make minimal targeted edits to `web/e2e/run.mjs` for smoke selection/configuration and strict nonzero exit integration so the existing overlay lifecycle can run the DEV-471 spec under two-phase coordination from `scripts/run-compose-smoke.ps1` without weakening fail-closed semantics; **explicitly exclude** `dev471-compose-smoke.e2e.ts` from ordinary `npm run e2e` and opt it in only via the required smoke entrypoint — no double-run without that mechanism; preserve existing local wrapper skip semantics outside required smoke (FR-002, FR-009, FR-013, FR-018; Q6; groups 1/6). Do not broaden unrelated e2e behaviour.

**Checkpoint**: US2 + US3 independently testable on isolated stacks; DEV471 not selected by ordinary npm e2e.

---

## Phase 4: User Story 4 - Unconditional dedicated CI wiring (Priority: P1)

**Goal**: New `.github/workflows/compose-smoke.yml` adds unconditional main-targeting PR job id/name `compose-smoke` with budgets from prep output, allowlisted sanitized artifacts, and change-triggered `-RegressionProof`.
**Independent Test**: Dedicated workflow shows required job invoking both positive phases without path bypass (docs-only included); failure modes mapped to exit 1; `ci.yml`/`promote-home.yml` untouched; `e2e-smoke` remains distinct.
**Order**: T008.

- [x] T008 [US4] **Build lane**. After T007: Create `.github/workflows/compose-smoke.yml` with `pull_request` to `main`, no path filters, job id and displayed name `compose-smoke`, that runs both positive phases via `scripts/run-compose-smoke.ps1` on every main-targeting PR including docs-only; derives job/startup/assertion/cleanup budgets from `web/e2e/fixtures/dev471/budgets.json` prep output (`ASSUMPTIONS.md` 30m/120s/3m/120m; zero whole-test retries); uploads allowlisted redacted artifacts under artifact name `compose-smoke` with fail-closed guard (no raw settings/env; secret-bearing browser binaries excluded/regenerated); triggers `-RegressionProof` whenever the PR touches `scripts/run-compose-smoke.ps1` or extracted startup/health code, failing closed to run proof if change-base/source resolution fails; ordinary unrelated PRs run both positives only; never green/SKIPPED for Docker/model/health/timeout/assertion/teardown failure; include always-run project-scoped cleanup with cleanup time reserved inside the 120m cap (FR-001, FR-008–FR-012, FR-018; Q5/Q6; groups 1/4/5/6). Leave `.github/workflows/ci.yml` and `promote-home.yml` untouched. Do not assert double-run with `e2e-smoke`; they coexist as distinct checks. Do not claim unverified existing upload helpers.

**Checkpoint**: CI definition matches fail-closed policy (registration verification is US5).

---

## Phase 5: User Story 5 - Failure-path proofs and required-check verification (Priority: P2)

**Goal**: Deliberate failure-path / planted-secret / existing-runner evidence; final-candidate DEV-440 proof superseding T004; authorized operator verification that `main` requires the actual published `compose-smoke` context.
**Independent Test**: Receipts show exit 1 + diagnostics/cleanup for required failure classes; planted-secret stdout/upload safety; existing-runner nonregression; T010 final-candidate proof; operator verification of published context (or explicitly open). Pending T011 keeps Done and implementation merge-bar open.
**Order**: T009 → T011.

- [ ] T009 [US5] **Build lane**. After T008: Execute deliberate failure-path proofs for startup timeout, unavailable Docker, assertion failure, and teardown failure against the runner, showing required-smoke exit 1 with diagnostics/cleanup attempts each time; add planted-secret tests that inspect stdout and upload payload for redaction/fail-closed guard; prove cancellation/failure cleanup attempts and that unrelated projects are not cleaned; add existing-runner deliberate failure nonregression proving required smoke does not inherit local exit-2 green/SKIPPED while preserving existing local wrapper skip semantics elsewhere (FR-009, FR-011, FR-014; SC-005; Q5/Q6; groups 4/6). Meaningful orchestration simulation is allowed; it does not replace positive real-stack acceptance.
- [ ] T010 [US5] **Build lane**. After T009: Execute both positive Compose phases on the final implemented candidate and capture candidate-positive plus expected-revert-negative `-RegressionProof` with source/patch identities and observable cause; this final-candidate evidence **supersedes** developmental T004; document repeat obligation for future startup-runner/assertion changes and refresh after invalidating changes; ordinary unrelated PRs remain two-positives-only; change-base unresolved must fail closed to run proof (FR-002, FR-008; SC-002–SC-004; Q5; group 5).
- [ ] T011 [US5] **Build lane** / authorized operator. After T008: Arrange and record authorized repository operator/owner verification that `main` branch protection requires the **actual published** `compose-smoke` check context (not inferred id alone; receipt verifies published context enforced and no bypass). Workflow publication alone is insufficient. Accessible protection API omission of `required_status_checks` (`recon-DEV-471:70`) is not evidence of registration and does not prove global absence of other enforcement. Keel/build seats MUST NOT perform unapproved admin mutations. Remove-required-check kill switch is rejected (Patron noted, no ticket). If verification is pending, leave SC-006/T011 and ticket Done / implementation merge-bar open (non-PASS) — never claim PASS; spec Gate 1 remains independent; no postmerge-only registration assumption (FR-015; SC-006; Q5/Q6; group 4).

**Checkpoint**: Delivery evidence package complete or explicitly open where operator action remains.

---

## Phase 6: Gates (after implementation evidence)

**Purpose**: Honest gate reporting without threshold changes.
**Checkpoint**: All applicable commands recorded with exit codes and distinct verdicts.

- [ ] T012 **Gauge** / **Build lane**. After T007 (and after any web edits): Run `npm run typecheck:e2e` and `scripts/run-web-gates.ps1` for web edits (flags verified via `-Help` at T001 pickup — do not invent options), plus pipeline-required harness gates; preserve each script's actual status and scope-empty distinctions; never lower thresholds; never fold SKIPPED/SKIP/N/A/Could not run into PASS (FR-016; `brief.md` Gates; Q6; group 6).

---

## Phase 7: Capped review / remediation

- [ ] T013 After T010–T012 status: Three-axis implementation review within Q1 closing bar (all verified Critical/High/Medium/Low contract/safety/acceptance findings block; all accepted findings require closure at all severities); at most two rounds and two fix commits per round; unresolved at the cap escalate for owner review (FR-017; Q1). After any remediation commit, refresh invalidated proofs/gates on final HEAD (including T010 if runner/health evidence invalidated).

---

## Phase 8: Delivery / merge-bar handoff

- [ ] T014 After T011–T013: Prepare delivery/merge-bar handoff with both positive Compose phases, failure-path/planted-secret/existing-runner proofs, final-candidate controlled-revert evidence (T010), gate receipts, and operator required-check verification status for the actual published `compose-smoke` context. Pending T011 keeps Done and implementation merge-bar open. User merges only. Quill/Keel/build seats do not merge (`brief.md` Gates and handoff; group 4).

---

## Dependencies & parallel opportunities

- T000 blocks all Build work.
- T001 blocks T002+ (except T005 may proceed after T001 for fixture media/settings once decodability/mount facts exist).
- T002 creates `budgets.json` then the runner; T002 → T003 → T004 before claiming developmental startup/revert proof. T002 does **not** depend on T005 (no cycle).
- T006 depends on T004 (two-phase coordination available) and T005 (fixture media/settings).
- T007 depends on T006; T008 depends on T007 (and uses budgets.json created in T002).
- T009–T011 depend on T008; T010 supersedes T004; T012 may overlap review prep but must reflect final web edits.
- T005 is `[P]` relative to T002–T004 once T001 is done (disjoint files: T005 excludes `budgets.json`).

## MVP

User Stories 2 + plain-startup half of the required check (T002–T004) deliver unmasked DEV-440 coverage. Full ticket Done when requires US3–US5 evidence as well (including active 403, dedicated workflow, T010, and operator published-context verification).

## Implementation strategy

1. Gate 1 merge receipt first (independent of T011).
2. Recon facts (mount/route/media/help flags) before any runner/test/CI edits; STOP if unavailable.
3. Create `budgets.json` then disposable source staging + runner and `-RegressionProof` before stubbed flow assertions; T005 fixture media/settings stay parallel and exclude the budget file.
4. Minimal `run.mjs` exclusion/selection only as needed to reuse overlay lifecycle without ordinary-e2e double-run.
5. Dedicated `compose-smoke.yml` after local two-phase path works; leave `ci.yml`/`promote-home.yml` untouched.
6. Failure-path + planted-secret + existing-runner + T010 final-candidate + operator published-context verification + honest gates before review/delivery.

