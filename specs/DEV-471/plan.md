# Implementation Plan: Required Compose smoke with unmasked startup and stubbed full flow

**Branch**: `feature/471-spec` | **Date**: 2026-11-15 | **Spec**: `specs/DEV-471/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q7 plus plan-challenge decision record and groups 1–6 amendment); `CONCLUSIONS.md` (provenance + groups 1–6); `ASSUMPTIONS.md`; proposed ADR `docs/adr/0043-unmasked-compose-startup-and-stubbed-smoke.md`; Maestri notes `DEV-471`, `recon-DEV-471` (retained lines 1–25 + appended 27–73; verbatim ticket body 36–48); `PLAN_ADJUDICATION.md`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: PLAN FROZEN. Gate 1 spec PR #414 merged; T000 and T001 complete (receipts synchronized 2026-11-16). Anvil cleared for local Phase 2 T002-T004 on Chromium probe evidence in recon-DEV-471:12-35. See brief.md T001 closure and Phase 2 dispatch clearance. Compose runtime acceptance, implementation gate results, branch-protection verification and merge clearance remain open.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md` (Quill-owned). `checklists/compose-smoke.md` remains Keel reviewer-owned. No `research.md`, `data-model.md`, or `contracts/` — behaviour and file boundaries are frozen in the brief as amended; no public API/schema design delta. ADR 0043 already drafted by Keel (proposed). Pre-Gate 1 ADR content alignment is recorded in `PLAN_ADJUDICATION.md` (Compass L5).

## Summary

Add a required main-targeting PR check `compose-smoke` via new dedicated `.github/workflows/compose-smoke.yml` that (1) proves healthy startup of plain shipped Compose from a disposable candidate-HEAD source copy with real Postgres/RabbitMQ + Ollama/model readiness (unmasked DEV-440 production binding), then (2) on a separately isolated shipped-base-plus-e2e-overlay stack asserts fixture-correlated import, stubbed enrichment, decodable thumbnail, library listing, genuine `/hubs/` UI update, advancing browser playback (ticket-summary + Q2/Q4 provenance), and active process-play POST 403 while keeping LocalPlay false. Delivery includes named `-RegressionProof` reverse-only-`4646476f` evidence, fail-closed redacted diagnostics/cleanup, single budget config at `web/e2e/fixtures/dev471/budgets.json`, ordinary-e2e exclusion of DEV471, and authorized operator verification of the actual published required-check context.

## Technical Context

**Language/Version**: PowerShell runner + Node/Playwright e2e under `web/e2e` (existing stack). No production C# edits planned.

**Primary Dependencies**: Existing Docker Compose, Playwright e2e runner (`web/e2e/run.mjs`), shipped `docker-compose.yml`, overlay `web/e2e/compose.e2e.yml`, GitHub Actions. No new NuGet/npm dependency (`brief.md` Q6 / group 1).

**Storage**: Ephemeral per-run Compose data/model/fixture state only inside disposable source copies; no schema change.

**Testing**: Real Compose positive phases; Playwright fixture-correlated assertions in `web/e2e/dev471-compose-smoke.e2e.ts` (smoke-entrypoint only); meaningful runner failure-path proofs including planted-secret and existing-runner nonregression; disposable `-RegressionProof`. `npm run typecheck:e2e` plus `scripts/run-web-gates.ps1` for web edits and harness gates with honest scope-empty reporting. No new test framework.

**Target Platform**: CI GitHub Actions runners with Docker; local operator verification of branch protection.

**Project Type**: CI/ops Compose smoke orchestration reusing existing e2e overlay lifecycle; not a Core/API feature slice.

**Performance Goals**: Bounded budgets only (`ASSUMPTIONS.md`), owned by one validated DEV-471 budget config at `web/e2e/fixtures/dev471/budgets.json` (created by T002 before first runner use; T005 owns fixture media/settings excluding that file): 30m stack startup, 120s async assertion, 3m teardown per project, 120m job; zero whole-test retries. Workflow cap derived from prep output of that config.

**Constraints**: Charter §2.3 items 1–6 ruled (Q2/Q6 as amended by groups 1–6). Reuse both Compose files unchanged. Preserve LocalPlay false and nginx 403 (active Phase 2 POST). No application API/hub mocks. No PR-checkout mutation. No secrets committed. No threshold lowering. No unverified upload-helper claims. Unauthorized seats must not mutate branch protection. Reject remove-required-check kill switch (noted, no ticket). STOP for recon/Patron if Compose edit or missing production capability / unavailable mount/route/media appears.

**Scale/Scope**: Size L; one dedicated required CI workflow/job; two positive stack lifecycles; authorized file set listed below.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). No owner structural checkbox.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **N/A / PASS** — no Core/Infrastructure/Api production edits; no new port. |
| II (Explicit Handlers) | Handlers + AddHandler | **N/A** — no new commands/queries. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — smoke may trigger existing import/enrichment through real stack; no new mutator/state-machine design. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no new public API/error contract; use existing routes/generated types. |
| VI (Observability) | Safe diagnostics | **PASS** — allowlisted redacted staging under artifact `compose-smoke`; no raw settings/env; secret-bearing binaries excluded/regenerated; planted-secret proof required. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — LocalPlay remains false; active process-play 403; ephemeral runtime credentials/settings inside disposable copies only; no Process.Start play oracle; no committed secrets. |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new domain glossary term; ADR 0043 already assigned for the two-phase tradeoff (proposed). |
| IX (Test Pyramid) | Meaningful layers | **PASS** — real Compose positive acceptance + controlled-revert proof; runner failure-path proofs do not replace them; no mocked app responses. |
| Tech stack / API docs | OpenAPI chain | **PASS** — no contract generation or public shape change. |
| Governance / Static-Analysis Gates | Honest exits | Phase B: `npm run typecheck:e2e`, `scripts/run-web-gates.ps1` when web in scope (flags verified at pickup), harness gates with honest SKIPPED/SKIP/N/A/Could not run; never fold into PASS; never lower thresholds. Closing bar and 2×2 caps per Q1; all severities require closure. |

### Complexity Tracking

No constitution violations requiring justification. Two Compose stack lifecycles are an explicit Patron-approved tradeoff recorded in ADR 0043 (Q3), not a silent complexity expansion. Dedicated workflow (vs editing `ci.yml`) is the Patron-amended Q6 file authorization resolving `paths-ignore`/promote-home coupling (`recon-DEV-471:53`).

## Approach

- **Phase A (this draft)**: Quill writes/repairs spec/plan/tasks/requirements checklist/receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon facts/PRODUCT/plan-adjudication rulings. Keel performs read-only analyze, three-axis plan challenge, compose-smoke checklist verdicts, and ADR consistency before any PLAN FROZEN declaration. No Build work before Gate 1.
- **Phase B pickup (after Gate 1)**: Fresh recon of exact fixture mount input, route/event/media identities, credential/settings setup, Chromium fixture decodability, helper/wrapper/gate help surfaces, and CI seams; STOP on missing production capability, unavailable required identity, or required Compose edit.
- **Implementation order (`brief.md` Task ordering / groups 1–6)**:
  1. Bounded recon (facts only; T001 STOP gates).
  2. Create `web/e2e/fixtures/dev471/budgets.json` (ASSUMPTIONS values) then `scripts/run-compose-smoke.ps1` — disposable candidate source staging, plain startup, two-phase coordination, named `-RegressionProof`, redacted diagnostics, project-scoped cleanup with finally/always-run. Budget config exists before developmental startup proof.
  3. `web/e2e/dev471-compose-smoke.e2e.ts` + minimal `web/e2e/run.mjs` selection that excludes DEV471 from ordinary npm e2e and opts in via smoke entrypoint; fixture media/settings under `web/e2e/fixtures/dev471/**` excluding `budgets.json` (disjoint from T002).
  4. New `.github/workflows/compose-smoke.yml` — unconditional main-targeting PR job id/name `compose-smoke`, budgets from `budgets.json` prep output, sanitized allowlisted artifacts, change-triggered RegressionProof; leave `ci.yml`/`promote-home.yml` untouched; arrange authorized required-check verification of actual published context.
  5. Execute both positive phases; prove failure paths including planted-secret and existing-runner nonregression; capture final-candidate (`T010`) DEV-440 evidence superseding developmental `T004`.
- **ADR**: Keep Keel-authored `docs/adr/0043-unmasked-compose-startup-and-stubbed-smoke.md` proposed until Gate 1; do not reauthor. Pre-Gate 1 ADR-review receipt → `PLAN_ADJUDICATION.md`.
- **Provenance**: Verbatim body (`recon-DEV-471:36–48`) does not name play; browser playback retained per Patron no-scope-change ruling with summary + Q2/Q4 attribution.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-471/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── PLAN_ADJUDICATION.md
├── QUILL_REPAIR_ROUND1.md
├── spec.md
├── plan.md              # This file
├── tasks.md
├── DRAFTING_RECEIPT.md
└── checklists/
    ├── requirements.md      # Quill-owned
    └── compose-smoke.md     # Keel reviewer-owned

docs/adr/
└── 0043-unmasked-compose-startup-and-stubbed-smoke.md  # Keel-authored; proposed
```

No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief as amended; no API/schema delta.

### Source Code (authorized boundary — Q6 as amended by group 1)

```text
.github/workflows/compose-smoke.yml      # NEW dedicated: unconditional pull_request to main, no path filters,
                                         # job id + displayed name compose-smoke, budgets from prep output,
                                         # allowlisted sanitized artifacts, change-triggered -RegressionProof
web/e2e/run.mjs                          # targeted: exclude DEV471 from ordinary npm e2e; select via smoke
                                         # entrypoint; strict exit integration
scripts/run-compose-smoke.ps1            # new: CmdletBinding/-Help, plain startup, two-phase coordination,
                                         # disposable source staging, -RegressionProof, redaction, cleanup
web/e2e/dev471-compose-smoke.e2e.ts      # new: fixture-correlated real full-flow + active process-play 403
web/e2e/fixtures/dev471/budgets.json     # new: single DEV-471 budget config (T002 before first runner use)
web/e2e/fixtures/dev471/**               # new: fixture media/settings excluding budgets.json (T005)

Leave untouched:
.github/workflows/ci.yml                 # paths-ignore / promote-home coupling unchanged
.github/workflows/promote-home.yml

Reuse unchanged (do not edit without new Patron ruling):
docker-compose.yml
web/e2e/compose.e2e.yml
```

**Structure Decision**: Stay inside the amended Q6 authorized paths. Dedicated workflow replaces the original Q6 `ci.yml` edit so docs-only PRs cannot bypass via `paths-ignore` while preserve promote-home coupling. No new top-level folder, project, or architectural layer. Reuse existing overlay lifecycle via minimal `run.mjs` integration rather than a second orchestration stack. Disposable candidate-HEAD source copies host generated `docker/api.settings.json` at the fixed mount path (runtime prep, not source edit).

## Disposable staging, readiness classifier, and diagnostics

- **Positive source**: Disposable candidate-HEAD copy outside PR checkout; generated settings at fixed mount; unchanged positive Compose/nginx; Phase 1 no endpoint override; Phase 2 uses existing `${E2E_SETTINGS_FILE}` overlay seam; separate randomized project/data/model/fixture state.
- **Negative source**: Same candidate HEAD plus reverse of only `4646476f`; never historical parent; patch mismatch fails closed.
- **Checkout receipt**: Record source/patch identities and original checkout baseline unchanged including preexisting dirty/untracked state.
- **Classifier**: Successful Docker/build/settings → healthy Postgres/RabbitMQ → real Ollama/model readiness → never-healthy API with attributable localhost/embedding cause; 139 optional corroboration; infrastructure errors and unexplained timeouts rejected. Receipt fields: prerequisites, health, exit, cause, source, patch.
- **Phase coverage**: Phase 1 = API + dependencies; Phase 2 = web/nginx via real user flow.
- **Cleanup**: Register projects before startup; runner finally/termination + CI always-run project-scoped cleanup with reserved cap time; cannot guarantee cleanup after hard runner loss.
- **Upload**: New safe allowlisted staging; redact generated values/credentials/connection strings before stdout/upload; no raw settings/env; exclude/regenerate unsafe binary browser evidence; fail-closed guard; distinct artifact `compose-smoke`. Do not claim Ledger-named upload helpers already exist (`recon-DEV-471:55` shows evaluation-metrics only); reuse only if T001 verifies within authorized paths.

## Test Strategy

| Family | Required evidence |
|---|---|
| Plain startup (positive) | Candidate disposable copy plain `docker-compose.yml` reaches API healthy after Postgres/RabbitMQ + real Ollama/model readiness; no e2e endpoint override |
| DEV-440 controlled revert | Named `-RegressionProof`; reverse only `4646476f`; identical health assertion fails for attributable localhost/embedding cause after readiness; not infra/timeout; checkout baseline unchanged |
| Stubbed full flow | Fixture-correlated listing, stub enrichment, decodable thumbnail, genuine `/hubs/` UI update, advancing browser play, active process-play POST 403; external providers only stubbed; smoke-entrypoint selection only |
| Failure paths | Startup timeout, unavailable Docker, assertion failure, teardown failure, planted-secret stdout/upload, cancellation cleanup, existing-runner deliberate failure nonregression → required-smoke exit 1 + diagnostics/cleanup attempts |
| Gates | `npm run typecheck:e2e`; `scripts/run-web-gates.ps1` for web edits (flags via `-Help`); harness gates with honest SKIPPED/SKIP/N/A/Could not run |
| Branch protection | Authorized operator/owner verification that `main` requires actual published `compose-smoke` context; pending keeps Done/merge-bar open; protection API omission ≠ PASS |

## Phase 0 / Phase 1 note

Per Spec Kit plan workflow: unknowns that would normally populate `research.md` are already ruled in brief Q1–Q7 as amended by plan-challenge groups 1–6 (two-phase architecture, dedicated workflow, disposable staging, acceptance contract including active 403, operational policy, file scope, budgets, RegressionProof enforcement). Remaining fixture/route/media/mount identities and helper/gate flag surfaces are Phase B recon tasks (`T001` STOP facts), not inventable design artifacts. No open inventable-behaviour `needs decision` items remain after Patron groups 1–6; outstanding items are pickup facts / operator evidence.

## Post-design Constitution Check

Re-checked after documenting the dedicated-workflow / disposable-staging structure above: still PASS/N/A as in the Constitution Check table; no new port, handler, schema, API, LocalPlay enablement, secrets, Process.Start play oracle, CONTEXT, or additional ADR obligations introduced by this plan (ADR 0043 already proposed by Keel; pre-Gate 1 alignment receipt in `PLAN_ADJUDICATION.md`).

