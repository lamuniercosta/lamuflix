# Feature Specification: Required Compose smoke with unmasked startup and stubbed full flow

**Feature Branch**: `feature/471-spec`
**Created**: 2026-11-15
**Status**: Gate 1 spec PR #414 merged; T000 and T001 complete (2026-11-16). Anvil cleared for local tasks.md Phase 2 T002-T004; Chromium probe receipt recon-DEV-471:12-35 and brief.md T001 closure. Historical Gate 1 pending and T001 blocked wording is superseded. Compose runtime acceptance, implementation gates, protection verification and merge clearance remain open.
**Input**: Ticket DEV-471 (size L; parent DEV-492); grill-closed `brief.md` (Q1–Q7 plus plan-challenge decision record and groups 1–6 amendment); `CONCLUSIONS.md` (including plan-adjudication provenance and groups 1–6); `ASSUMPTIONS.md`; proposed ADR `docs/adr/0043-unmasked-compose-startup-and-stubbed-smoke.md` (proposed until Gate 1); Maestri notes `DEV-471`, `recon-DEV-471` (retained DEV-440 lines 1–25 plus appended plan-adjudication lines 27–73; verbatim ticket body at lines 36–48); `PLAN_ADJUDICATION.md`; `specs/PRODUCT.md`.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Status / Closing bar; Q1–Q7; plan-adjudication groups 1–6; `PRODUCT.md` §5). Ordinary owner Gate 1 on the spec PR remains pending after Bernstein coordination / Rigger preparation-opening of that PR and verified user merge. Spec Gate 1 is independent of implementation merge-bar and of pending SC-006/T011 operator verification. Patron approval is not owner approval. Quill does not invent PLAN FROZEN or a structural checkbox.

## Clarifications

### Session 2026-11-15

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for drafting? → A: Frozen delivery scope (plain shipped Compose startup phase, separately isolated stubbed full-flow phase, fixture-correlated import/enrich/thumbnail/library listing/`/hubs/`/browser play, LocalPlay false with nginx process-play 403 preserved, delivery/change-triggered disposable DEV-440 `4646476f` revert proof, unconditional PR `compose-smoke` check, fail-closed exits, isolation/diagnostics/cleanup, budgets/retries, authorized file set and five-step order, operator branch-protection verification) is settled in brief Q1–Q7 as amended by plan-challenge groups 1–6. Exact import/route/event/media identities, Chromium fixture decodability, library mount input, and helper/gate flag surfaces remain Phase B recon facts (`brief.md` Closure receipt; Q4; group 2/6), not inventable here. No `[NEEDS CLARIFICATION]` markers inside this spec for inventable behaviour.

### Repair round 1 — provenance correction (2026-11-15)

- Verbatim ticket Wanted/Done when (`recon-DEV-471:36–48`) mandates shipped Compose healthy startup, fixture import, stubbed enrichment, thumbnail, library listing, `/hubs/` live updates, every-main-PR blocking check, and DEV-440 revert proof. It does **not** mention play.
- Browser playback is retained per Patron no-scope-change ruling as justified verification within the existing wanted path. Provenance is the ticket summary (`DEV-471` note) plus Patron Q2/Q4, not the verbatim body. LocalPlay remains false; nginx process-play 403 remains.
- Historical CONCLUSIONS citations that used earlier recon line numbers must not be remapped onto the current 1–25 / 27–73 note layout.

## Scope

**In scope** (`brief.md` Frozen scope / Approach / Full-flow acceptance / CI operation / Authorized files / Task ordering / Plan challenge groups 1–6; Q1–Q7; ticket authority via `recon-DEV-471:36–48` plus Patron provenance ruling):

1. **Required PR check `compose-smoke`**: Every pull request targeting `main` — including docs-only PRs — runs one stable required check that executes two separately isolated positive phases, without path filters that bypass the check. New dedicated workflow `.github/workflows/compose-smoke.yml` (`pull_request` to `main`, no path filters); job id and displayed name `compose-smoke`. Existing `ci.yml` and `promote-home.yml` remain untouched (their `paths-ignore` / promote-home coupling stays unchanged). Existing `e2e-smoke` coexists as a distinct check/context from `compose-smoke`. Reserve **required** for the ticket's `compose-smoke` requirement and for actual authorized operator verification of the published context — recon confirms an `e2e-smoke` job, not required registration.
2. **Phase 1 — plain shipped startup**: From a disposable candidate-HEAD source copy outside the PR checkout, start plain shipped `docker-compose.yml` without the e2e Ollama endpoint override; materialize generated ephemeral `docker/api.settings.json` only inside that copy (runtime preparation, not a source edit); require healthy Postgres/RabbitMQ, real Ollama/model readiness, then API health. Covers the production endpoint binding fixed in DEV-440 (`4646476fb1f8afb8539a07c8f404c911a5fc174e` adds `api.environment.Ollama__Endpoint: http://ollama:11434/` per `recon-DEV-471:13,15`). Phase 1 establishes API/dependency health coverage; web/nginx functional coverage is Phase 2.
3. **Phase 2 — stubbed full flow**: Separately start shipped-base plus existing `web/e2e/compose.e2e.yml` overlay (existing `${E2E_SETTINGS_FILE}` seam) for deterministic external-provider-stubbed assertions. Overlay endpoint override (`010e0cb6`) must not be treated as production-startup coverage. Positive shipped Compose and nginx files remain unchanged.
4. **Fixture-correlated acceptance** (stubbed phase): Before import, establish browser `/hubs/` connection through shipped nginx. Trigger import of a known fixture through the real application stack. For that fixture assert: persisted library listing presence; deterministic stub-defined enrichment fields; nonempty thumbnail that decodes as an image; a genuine server-originated `/hubs/` event reflected in the UI without reload or polling substitution; in-page browser playback through shipped nginx with valid media metadata and `currentTime` advancing while playing.
5. **Stub boundary**: Only external third-party/LLM providers are stubbed. No application API interception or mocked application/hub responses may satisfy acceptance. Use existing API contracts and generated TypeScript types; no new public route or DTO.
6. **Playback interpretation (corrected provenance)**: Browser in-page media playback through shipped nginx is required verification (ticket summary + Patron Q2/Q4; no scope change). Verbatim body (`recon-DEV-471:36–48`) does not name play. `Features:LocalPlay` remains false. Phase 2 MUST actively `POST /api/movies/<fixture-id>/play` through shipped nginx for the imported fixture and require HTTP 403, with no process launch.
7. **DEV-440 regression proof**: Named `-RegressionProof` runner mode. Delivery acceptance evidence on the implemented candidate (`T010` final-candidate proof supersedes developmental `T004`), and workflow-triggered whenever the startup runner or extracted startup/health code changes. If change-base/source resolution is unavailable, run proof fail-closed (do not silently skip). Ordinary unrelated PRs run both positives only. Negative copy = candidate HEAD plus reverse of only `4646476f`; patch mismatch fails; never run the historical parent as the candidate. Classifier requires successful Docker/build/settings, healthy Postgres/RabbitMQ, real Ollama/model readiness, never-healthy API, and observed expected localhost/embedding startup cause; exit 139 is optional corroboration only; infrastructure errors and unexplained timeouts are rejected. Receipt binds prerequisites, health outcome, exit status, cause, source identity, and patch identity. Original PR checkout baseline remains unchanged, including preexisting dirty/untracked state.
8. **Fail-closed operation**: Missing Docker, failed model download, unhealthy stack, timeout, assertion failure, and teardown failure cause exit 1 — never green or SKIPPED for this required smoke check. Existing local wrapper skip semantics elsewhere are preserved. This does not reclassify unrelated harness scope-empty exits.
9. **Isolation & hygiene**: Randomized per-run/per-phase Compose projects; generated ephemeral credentials/settings inside disposable source copies; isolated data/model state and fixtures; never commit secrets; never mutate the PR checkout; never clean unrelated projects. Register each created project before startup.
10. **Diagnostics & cleanup**: Capture sanitized container logs, service health/exit status, and browser failure artifacts before bounded project-scoped teardown. Redact generated secret values and credential/connection-string fields before stdout or upload; never dump raw settings/env. Allowlisted staged payload with fail-closed upload guard; distinct artifact name `compose-smoke`. Binary browser evidence containing secrets must be excluded or safely regenerated — text replacement alone is insufficient. Do not assert unverified existing upload helpers; reuse only if T001 verifies within authorized paths. Bounded runner `finally`/termination handling plus CI always-run project-scoped cleanup with cleanup time reserved inside the job cap; cleanup failure remains failure. Hard runner loss cannot guarantee teardown.
11. **Budgets** (`ASSUMPTIONS.md` `[assumed]`; single validated DEV-471 budget config at `web/e2e/fixtures/dev471/budgets.json` driving runner inputs and workflow prep outputs): 30 minutes per stack startup, 120 seconds per async assertion, 3 minutes teardown per project, 120 minutes total job cap; zero automatic whole-test retries. Created by T002 before first runner use; T005 owns fixture media/settings under `web/e2e/fixtures/dev471/**` excluding `budgets.json` (disjoint file sets). No scattered new threshold values; no `harness.yml` rewrite. A timeout is failure, not acceptance evidence of a specific regression unless the expected startup cause is established.
12. **Authorized files** (Q6 §2.3 item 6 as amended by plan-adjudication group 1): new `.github/workflows/compose-smoke.yml`; targeted edits to `web/e2e/run.mjs` (minimal smoke selection/configuration and strict exit integration that **excludes** `dev471-compose-smoke.e2e.ts` from ordinary `npm run e2e` and selects it only via the required smoke entrypoint — no double-run without that mechanism); new `scripts/run-compose-smoke.ps1` (CmdletBinding/`-Help`, named `-RegressionProof`, documented strict smoke exit semantics), `web/e2e/dev471-compose-smoke.e2e.ts`, `web/e2e/fixtures/dev471/**` (fixtures/settings plus `budgets.json`); planning evidence under `specs/DEV-471/**` and proposed ADR `docs/adr/0043-unmasked-compose-startup-and-stubbed-smoke.md`. Reuse `docker-compose.yml` and `web/e2e/compose.e2e.yml` unchanged. Leave `ci.yml` and `promote-home.yml` untouched. No new packages, projects, layers, schema/public API/production C#/web UI/nginx changes, existing-fixture rewrites, or generated-contract changes. If a required Compose edit or missing production capability emerges, stop for bounded recon and Patron ruling.
13. **Task order**: (1) bounded recon verifying exact fixture mount input and route/event/media identities, helper/wrapper/gate interfaces via help at pickup, credential/settings bootstrap, Chromium fixture decodability — STOP if unavailable; (2) plain-startup runner on disposable candidate source, `-RegressionProof` mode, diagnostics, cleanup; (3) fixture-correlated stubbed-flow tests including active process-play 403, and minimal runner integration with ordinary-e2e exclusion; (4) dedicated `compose-smoke.yml` wiring and authorized required-check verification arrangement; (5) execute both positive phases; prove failure classes including planted-secret and existing-runner nonregression; capture final-candidate DEV-440 proof with source/patch identities and observable cause.
14. **Branch protection**: An authorized repository operator/owner must verify that `main` branch protection requires the **actual published** `compose-smoke` check context (not inferred id alone; accessible protection response may omit required-status-checks — `recon-DEV-471:70` does not evidence required checks and does not prove global absence). Workflow publication alone does not prove this. Pending SC-006/T011 keeps ticket Done and implementation merge-bar open (non-PASS); spec Gate 1 remains independent; no postmerge-only registration assumption; no unauthorized admin mutation. Suggested remove-required-check kill switch is rejected (Patron noted, no ticket).
15. **Loop discipline**: Closing bar — all verified unresolved Critical, High, Medium, and Low contract/safety/acceptance findings block closure; speculative abstractions defer. Two review rounds, at most two fix commits per round; unresolved at the cap escalate for owner review. Standing scope-empty SKIPPED, configured SKIP, and mutation N/A remain non-blocking where the pipeline permits and never count as PASS. A failed or unrunnable applicable gate blocks. Frozen scope ends: anything else is a follow-up issue, not a finding in this round.

**Out of scope**:

- Changing shipped `docker-compose.yml` or `web/e2e/compose.e2e.yml` without a new cited Patron ruling.
- Editing `.github/workflows/ci.yml` or `promote-home.yml` for this ticket.
- New NuGet/npm dependency, project, top-level folder, architectural layer, schema/migration, public API shape, production C#/web UI/nginx edits, or generated OpenAPI/TypeScript contract changes.
- Enabling `Features:LocalPlay`, altering the nginx process-play 403, or invoking `Process.Start` play as the smoke oracle.
- Mocking application HTTP/hub responses or intercepting application APIs to satisfy smoke acceptance.
- Claiming live DEV-440 regression has already been exercised; treating Docker/build/download/setup failure or unexplained timeout as DEV-440 proof; running historical parent as candidate; silently skipping RegressionProof when change-base is unresolved.
- Path-filtered skip of `compose-smoke` on main-targeting PRs (including docs-only); reporting SKIPPED/green for this required smoke on operational or assertion failure; double-running DEV471 via ordinary `npm run e2e` without the smoke selection mechanism.
- Claiming unverified existing upload helpers exist; dumping raw settings/env; uploading unsanitized secret-bearing browser binaries.
- Unapproved branch-protection admin mutation by Keel/build seats; remove-required-check kill switch.
- New domain glossary term or CONTEXT.md expansion (ADR 0043 already assigned/proposed for the two-phase tradeoff).
- Claiming PLAN FROZEN, Gate 1 clearance, implementation, runtime PASS, gate PASS, or merge from this Phase A draft.

**Precedence**: Verbatim ticket Wanted/Done when (`recon-DEV-471:36–48`) is already approved authority for body deliverables (`PRODUCT.md` §3). Playback provenance is ticket summary + Patron Q2/Q4 under the plan-adjudication no-scope-change ruling. Brief Q1–Q7 as amended by plan-challenge groups 1–6 and `CONCLUSIONS.md` govern boundaries. Ordinary owner Gate 1 remains pending. Only Keel declares PLAN FROZEN after analyze / three-axis challenge / repair verification.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md expansion)*

**compose-smoke**
The stable required CI check name (job id and displayed context) for every main-targeting PR that runs both positive Compose smoke phases under this ticket via dedicated `.github/workflows/compose-smoke.yml`.
_Avoid_: treating existing `e2e-smoke` alone as fulfilling DEV-471; path-filtered bypass; conflating workflow publication with branch-protection registration

**Plain shipped startup phase**
Isolated run of shipped `docker-compose.yml` from a disposable candidate-HEAD source copy without the e2e Ollama endpoint override, requiring healthy Postgres/RabbitMQ, real Ollama/model readiness, and API health (unmasked DEV-440 production binding coverage).
_Avoid_: overlay-only startup as production proof; treating web/nginx coverage as Phase 1

**Stubbed full-flow phase**
Separately isolated shipped-base plus `web/e2e/compose.e2e.yml` stack used for deterministic external-provider-stubbed import/enrich/thumbnail/listing/`/hubs/`/browser-play assertions plus active process-play 403 discharge; establishes web/nginx functional coverage.
_Avoid_: claiming this phase alone covers production Ollama endpoint binding

**DEV-440 controlled-revert proof (`-RegressionProof`)**
Named disposable, checkout-non-mutating demonstration that the identical plain-stack health assertion passes on the candidate and fails for the attributable localhost/embedding API startup cause after reversing only production commit `4646476f` on a candidate-HEAD copy.
_Avoid_: infrastructure/setup failures or unexplained timeouts as proof; claiming proof already ran; using historical parent as candidate; silently skipping when change-base unresolved

**Browser play (smoke)**
In-page browser media playback through shipped nginx with valid metadata and advancing `currentTime`; provenance is ticket summary + Patron Q2/Q4 (not verbatim Wanted/Done when body); LocalPlay remains false; Phase 2 actively asserts process-launch POST 403.
_Avoid_: process-launch play as the smoke oracle; claiming the verbatim ticket body mandates play

Consumed vocabulary (already in use; not redefined): Compose stack, `/hubs/`, library listing, enrichment, thumbnail, generated TypeScript types, `Features:LocalPlay`.

## What the User Will See *(mandatory)*

Nothing visible: this ticket adds a required Compose smoke check, diagnostics, and DEV-440 regression proof in CI/tooling. It does not change product UI, public API shapes, nginx routing, or LocalPlay behaviour. Existing library import, enrichment, thumbnail, hub updates, and browser playback surfaces remain as shipped; the smoke proves they still work under the ruled contracts.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bounded recon of contracts and fixture media (Priority: P1)

An operator (Build lane) verifies exact fixture mount input, route/event/media identities, existing helper/wrapper/gate interfaces via help, runtime credential/settings setup, and whether the chosen Chromium fixture media actually decodes — recording facts to recon without inventing production fixes.

**Why this priority**: Brief makes this the explicit first task; mount/route/media identities and helper flags are T001 STOP facts (`brief.md` Full-flow acceptance; Q4; groups 2/6).

**Independent Test**: Recon receipt lists verified mount input, contracts/bootstrap seams, helper/gate flags, and fixture decodability (or explicit `needs recon` / STOP if unavailable). No production or Compose file edits in this story.

**Acceptance Scenarios**:

1. **Given** Gate 1 merged artifacts on the pickup HEAD, **When** bounded recon runs, **Then** exact library mount input, import/listing/enrichment/thumbnail/`/hubs/`/browser-play contracts, credential/settings bootstrap, overlay lifecycle reuse points, and helper/wrapper/gate help surfaces are recorded as facts without inventing new routes, DTOs, or claimed existing APIs not in recon.
2. **Given** candidate fixture media under the approved fixture area / sample library, **When** Chromium decodability is checked, **Then** the receipt states whether the fixture decodes for advancing playback, or stops for recon/ruling rather than silently swapping undeclared production behaviour.
3. **Given** a missing production capability, unavailable required mount/route/event/media identity, or required Compose edit, **When** implementation would otherwise expand scope, **Then** work stops for bounded recon and Patron ruling — no silent plan expansion.

---

### User Story 2 - Plain startup runner and disposable DEV-440 proof mode (Priority: P1) 🎯 MVP

The required check can start plain shipped Compose from a disposable candidate-HEAD source copy without the e2e endpoint override, prove API health after real dependency/model readiness, and run named `-RegressionProof` against a candidate-HEAD copy that reverses only `4646476f`.

**Why this priority**: Overlay alone would mask DEV-440; unmasked startup is the architectural hinge (Q3; ADR 0043; groups 2/5).

**Independent Test**: Candidate plain startup reaches healthy API after Postgres/RabbitMQ + real Ollama/model readiness; disposable revert fails the identical health assertion for the attributable localhost/embedding cause; PR checkout baseline including dirty/untracked state is unchanged; diagnostics/cleanup attempted.

**Acceptance Scenarios**:

1. **Given** a disposable candidate-HEAD source copy with generated ephemeral `docker/api.settings.json` at the fixed mount path, **When** the plain-startup runner executes, **Then** Postgres/RabbitMQ are healthy, real Ollama/model readiness is provisioned, and the API reaches healthy without the e2e endpoint override; positive Compose/nginx files are unchanged.
2. **Given** the identical plain-stack health assertion on a candidate-HEAD copy that reverses only `4646476f`, **When** `-RegressionProof` runs, **Then** Docker/build/settings and dependency/model readiness succeed first; API never reaches healthy for the expected localhost/embedding startup cause (exit 139 optional corroboration only); infrastructure failures and unexplained timeouts are rejected; patch mismatch fails closed; historical parent is not used as candidate.
3. **Given** runner completion or cancellation, **When** teardown runs, **Then** sanitized redacted logs and health/exit status were captured before project-scoped cleanup; every created project was registered before startup; cleanup is always attempted within reserved cap time; cleanup failure yields failure; hard runner loss cannot be claimed as guaranteed cleanup.

---

### User Story 3 - Stubbed fixture-correlated full-flow smoke (Priority: P1)

On a separately isolated overlay stack, the smoke imports a known fixture through the real app and asserts listing, stub enrichment, decodable thumbnail, genuine hub UI update, advancing in-page playback, and active process-play 403 — without mocking application responses.

**Why this priority**: Ticket Done when requires import/enrich/thumbnail/listing/`/hubs/` on the real stack with deterministic stubs; Patron rules playback + active 403 as justified verification (groups 3; Q2/Q4).

**Independent Test**: `web/e2e/dev471-compose-smoke.e2e.ts` (selected only via required smoke entrypoint, excluded from ordinary `npm run e2e`) fails closed on any missing fixture-correlated assertion; external providers alone are stubbed; active POST process-play returns 403.

**Acceptance Scenarios**:

1. **Given** an isolated overlay stack with ephemeral credentials/settings via existing `${E2E_SETTINGS_FILE}` seam, **When** the browser establishes `/hubs/` through shipped nginx before import, **Then** the connection is ready for a genuine server-originated event (handshake alone is insufficient later).
2. **Given** that connection, **When** a known fixture is imported through the real stack, **Then** that fixture persists in library listing with deterministic stub enrichment fields and a nonempty decodable thumbnail.
3. **Given** the same fixture, **When** enrichment/import completes, **Then** a genuine server-originated `/hubs/` event for that fixture updates the UI without reload or polling substitution.
4. **Given** the imported fixture, **When** in-page browser play runs through shipped nginx, **Then** media metadata is valid and `currentTime` advances while playing; LocalPlay remains false; Phase 2 actively POSTs `/api/movies/<fixture-id>/play` through shipped nginx and requires HTTP 403 with no process launch.
5. **Given** any attempt to satisfy assertions via application API/hub mocks or unrelated preexisting library data, **When** acceptance is judged, **Then** those substitutes are rejected.

---

### User Story 4 - Unconditional CI wiring, isolation, and fail-closed policy (Priority: P1)

Every main-targeting PR — including docs-only — runs dedicated `compose-smoke` with both positive phases, budgets from the single DEV-471 config, sanitized allowlisted artifacts, and exit 1 on operational/assertion/teardown failure. Change-touching PRs additionally run `-RegressionProof` fail-closed.

**Why this priority**: Ticket requires a required check on every PR; `ci.yml` path filters would bypass docs-only PRs (`recon-DEV-471:53`); workflow alone without fail-closed semantics would greenwash breakage.

**Independent Test**: Dedicated workflow shows unconditional main-targeting PR job id/name `compose-smoke` invoking the two-phase runner with budgets/artifacts; DEV471 excluded from ordinary npm e2e; simulated/operational failures yield exit 1 (not SKIPPED); change-triggered RegressionProof present.

**Acceptance Scenarios**:

1. **Given** a PR targeting `main` (including docs-only), **When** CI runs, **Then** dedicated `.github/workflows/compose-smoke.yml` executes both positive phases without path filters; `ci.yml`/`promote-home.yml` remain untouched; existing `e2e-smoke` coexists as a distinct check.
2. **Given** missing Docker, model download failure, unhealthy services, timeout, assertion failure, or teardown failure, **When** the job finishes, **Then** the check exits 1 and never reports green or SKIPPED for this required smoke; local wrapper skip semantics elsewhere remain unchanged.
3. **Given** each phase, **When** projects are created, **Then** randomized project names, disposable source copies with ephemeral credentials/settings, and isolated state are used; no secrets are committed; the PR checkout is not mutated; unrelated projects are not cleaned.
4. **Given** failure or success paths, **When** artifacts are handled, **Then** allowlisted redacted diagnostics under artifact name `compose-smoke` are retained under the single budget config (`ASSUMPTIONS.md` values); unsafe secret-bearing browser binaries are excluded or safely regenerated.
5. **Given** a PR that changes the startup runner or extracted startup/health code, **When** CI evaluates change-base, **Then** `-RegressionProof` runs; if change-base/source is unresolved, proof runs fail-closed rather than being skipped. Ordinary unrelated PRs run both positives only.

---

### User Story 5 - Failure-path proofs and required-check registration verification (Priority: P2)

Delivery evidence includes deliberate failure-path proofs (including planted-secret and existing-runner nonregression), final-candidate DEV-440 proof superseding developmental proof, and authorized operator verification that `main` protection requires the actual published `compose-smoke` context.

**Why this priority**: Completes Done when / Q5–Q6 / groups 4–6 without conflating workflow publication with protection registration or developmental proof with final-candidate proof.

**Independent Test**: Receipts show exit 1 + diagnostics/cleanup attempts for required failure classes; planted-secret stdout/upload safety; existing-runner deliberate failure nonregression; T010 final-candidate proof; operator verification of published context (or explicit open — never claimed by unauthorized seats). Pending verification keeps Done and implementation merge-bar open.

**Acceptance Scenarios**:

1. **Given** the implemented runner, **When** startup timeout, unavailable Docker, assertion failure, teardown failure, planted-secret leak paths, and cancellation cleanup are exercised, **Then** each required-smoke failure yields exit 1 with diagnostics/cleanup attempts; planted-secret tests inspect stdout and upload payload; cancellation/failure proofs verify cleanup attempts and no unrelated project cleanup; existing-runner deliberate failure nonregression is recorded.
2. **Given** candidate-positive and revert-negative DEV-440 proof on the final implemented candidate, **When** delivery evidence is packaged (`T010`), **Then** source/patch identities and observable cause are recorded and supersede developmental `T004`; proof is repeated if the startup runner or health assertion changes; refresh after invalidating changes.
3. **Given** workflow publication, **When** required status is judged, **Then** only an authorized repository operator/owner verification that `main` protection requires the actual published `compose-smoke` context closes SC-006/T011; pending keeps Done and implementation merge-bar open (non-PASS); accessible protection API omission of required-status-checks is not evidence of registration or of global absence; Keel/build seats do not perform unapproved admin mutations; no remove-required-check kill switch.

---

### Edge Cases

- Overlay phase passes while plain shipped startup would fail after reverting `4646476f` — plain phase and controlled-revert proof remain mandatory (masking risk).
- Fixture appears in library from preexisting data unrelated to the import under test — must not satisfy fixture-correlated assertions.
- `/hubs/` websocket handshake succeeds but no fixture-tied server event updates the UI — must fail.
- Thumbnail HTTP 200 with empty or non-image body — must fail.
- Player UI visible without advancing `currentTime` / valid metadata — must fail.
- Process-play POST returns non-403 or launches a process — must fail.
- Controlled-revert fails due to Docker/build/model-download/setup or unexplained timeout — does not count as DEV-440 proof.
- Patch reverse of anything other than exactly `4646476f` — fail closed.
- Change-base unresolved on a runner/health-touching PR — run RegressionProof fail-closed, do not skip.
- Teardown fails after a green assertion path — overall result is failure.
- Timeout fires without establishing the expected API startup cause — failure, not regression proof.
- Hard runner loss mid-job — cleanup cannot be guaranteed; do not claim otherwise.
- Required Compose edit or missing production capability / unavailable mount/route/media discovered mid-build — stop for recon/Patron; do not silently expand.
- Ordinary `npm run e2e` accidentally selecting DEV471 — must not occur; smoke entrypoint is the sole opt-in.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a stable required check named `compose-smoke` (job id and displayed name) via new dedicated `.github/workflows/compose-smoke.yml` that runs on every PR targeting `main` — including docs-only — without path-based bypass; `ci.yml` and `promote-home.yml` MUST remain untouched; existing `e2e-smoke` MUST remain a distinct coexisting check.
- **FR-002**: `compose-smoke` MUST execute two separately isolated positive phases from disposable candidate-HEAD source copies: plain shipped Compose startup without the e2e Ollama endpoint override (Phase 1 API/dependency health), then shipped-base plus existing e2e overlay stubbed full flow (Phase 2 web/nginx coverage).
- **FR-003**: Plain startup MUST require healthy Postgres/RabbitMQ, provision real Ollama/model readiness, and require API health, covering production commit `4646476f` binding behaviour; generated ephemeral settings MUST be materialized only inside the disposable copy at the fixed `docker/api.settings.json` mount path; positive Compose/nginx MUST remain unchanged.
- **FR-004**: Stubbed full flow MUST assert, for one imported known fixture: persisted library listing, deterministic stub enrichment fields, nonempty decodable thumbnail, genuine fixture-tied `/hubs/` UI update without reload/polling substitution, and in-page browser playback with valid metadata and advancing `currentTime` through shipped nginx.
- **FR-005**: Acceptance MUST NOT be satisfied by application API/hub mocks, unrelated preexisting library entries, handshake-only hub checks, HTTP 200 alone, or a visible player without advancing playback.
- **FR-006**: Only external third-party/LLM providers may be stubbed; existing API routes and generated TypeScript types MUST be used with no new public route or DTO.
- **FR-007**: `Features:LocalPlay` MUST remain false; Phase 2 MUST actively `POST /api/movies/<fixture-id>/play` through shipped nginx for the imported fixture and require HTTP 403 with no process launch; smoke `play` is browser in-page playback (ticket summary + Patron Q2/Q4 provenance — not verbatim body).
- **FR-008**: Delivery MUST include named `-RegressionProof` disposable controlled-revert proof of `4646476f` using the identical plain-stack health assertion on a candidate-HEAD copy that reverses only that commit; candidate passes; revert fails only after successful Docker/build/settings and dependency/model readiness, with never-healthy API attributable to localhost/embedding startup cause (139 optional); infrastructure/unexplained timeouts rejected; patch mismatch fails; historical parent MUST NOT be the candidate; PR checkout baseline including dirty/untracked state MUST remain unchanged; repeat when startup runner/assertion changes; fail closed if change-base unresolved; ordinary unrelated PRs run both positives only; `T010` final-candidate proof supersedes developmental `T004`.
- **FR-009**: Missing Docker, model download failure, unhealthy stack, timeout, assertion failure, and teardown failure MUST exit 1 for this required smoke — never green or SKIPPED; existing local wrapper skip semantics elsewhere MUST be preserved.
- **FR-010**: Each run/phase MUST use randomized Compose projects (registered before startup), disposable source copies with ephemeral generated credentials/settings, and isolated data/model/fixture state; MUST NOT commit secrets, mutate the PR checkout, or clean unrelated projects.
- **FR-011**: Sanitized redacted container logs, service health/exit status, and browser failure artifacts MUST be captured before bounded project-scoped teardown using allowlisted staging under artifact name `compose-smoke`; generated secret values and credential/connection-string fields MUST be redacted before stdout/upload; raw settings/env MUST NOT be dumped; secret-bearing binary browser artifacts MUST be excluded or safely regenerated; fail-closed upload guard REQUIRED; do not claim unverified existing upload helpers; cleanup MUST always be attempted via runner finally/termination and CI always-run with time reserved inside job cap; cleanup failure remains failure; hard runner loss cannot guarantee cleanup.
- **FR-012**: Budgets MUST follow `ASSUMPTIONS.md` via one validated DEV-471 budget config at `web/e2e/fixtures/dev471/budgets.json` (created before first runner use) driving runner inputs and workflow prep outputs: 30m startup / 120s async assertion / 3m teardown per project / 120m job cap; zero automatic whole-test retries; no scattered new thresholds; no harness rewrite.
- **FR-013**: Implementation MUST stay within the amended Q6 authorized file set (dedicated `compose-smoke.yml`; `run.mjs` exclusion/selection; new runner/spec/fixtures/budget config) and reuse both Compose files unchanged; STOP for recon/Patron if Compose edit or missing production capability is required; verify helper/gate flags at pickup before use.
- **FR-014**: Meaningful runner failure-path proofs MUST cover startup timeout, unavailable Docker, assertion failure, teardown failure, planted-secret stdout/upload safety, cancellation cleanup attempts, and existing-runner deliberate failure nonregression, without replacing real-stack positive acceptance or controlled-revert proof.
- **FR-015**: Authorized operator/owner verification that `main` branch protection requires the actual published `compose-smoke` context MUST be obtained before SC-006/T011 and ticket Done / implementation merge-bar close; pending stays open/non-PASS; spec Gate 1 independent; workflow publication alone insufficient; accessible protection response omission is not registration evidence; unauthorized admin mutation forbidden; remove-required-check kill switch rejected (noted, no ticket).
- **FR-016**: Phase B MUST run `npm run typecheck:e2e`, `scripts/run-web-gates.ps1` for web edits (flags verified via `-Help` at pickup), and pipeline-required harness gates with honest SKIPPED/SKIP/N/A/Could not run reporting; never lower thresholds; never fold non-pass into PASS.
- **FR-017**: Review MUST apply the all-severity contract/safety/acceptance closing bar with two rounds and at most two fix commits per round; unresolved at the cap escalate for owner review; all accepted findings require closure at all severities.
- **FR-018**: `web/e2e/run.mjs` MUST exclude `dev471-compose-smoke.e2e.ts` from ordinary `npm run e2e` and opt it in only via the required smoke entrypoint; no double-run assertion without that mechanism.
- **FR-019**: New runner `scripts/run-compose-smoke.ps1` MUST expose CmdletBinding/`-Help`, documented strict smoke exit semantics, and named `-RegressionProof` mode.

### Key Entities

- **Compose smoke run**: One CI/job execution coordinating two isolated positive phases plus optional `-RegressionProof` mode, with budgets, diagnostics, and cleanup.
- **Disposable candidate source**: Checkout-external candidate-HEAD copy used for positive phases and for reverse-only-`4646476f` negative proof; original PR checkout baseline unchanged.
- **Smoke fixture**: Known media/settings asset under `web/e2e/fixtures/dev471/**` excluding `budgets.json` (and/or verified sample library media) correlated through import → listing → enrichment → thumbnail → hubs → play.
- **DEV-471 budget config**: Single validated file `web/e2e/fixtures/dev471/budgets.json` (ASSUMPTIONS values) owned by runner setup before first use.
- **DEV-440 revert subject**: Production Compose endpoint binding introduced by commit `4646476f`; disposable reverse-only target for negative health proof.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a main-targeting PR (including docs-only), dedicated `compose-smoke` runs both positive phases and fails closed (exit 1) on Docker/model/health/timeout/assertion/teardown failure — never SKIPPED/green for those cases; existing `e2e-smoke` remains a distinct check.
- **SC-002**: Plain shipped startup on the candidate reaches API healthy after Postgres/RabbitMQ health and real Ollama/model readiness, without the e2e endpoint override; Phase 1 covers API/dependencies; Phase 2 covers web/nginx via the real flow.
- **SC-003**: Stubbed full-flow phase demonstrates fixture-correlated listing, stub enrichment, decodable thumbnail, genuine hubs UI update, and advancing browser playback under the ruled stub boundary.
- **SC-004**: Disposable `-RegressionProof` reverse of only `4646476f` fails the identical plain-stack health assertion for the attributable localhost/embedding startup cause after readiness prerequisites succeed, while the candidate passes; PR checkout baseline remains unmutated; change-base unresolved fails closed to run proof.
- **SC-005**: Deliberate failure-path exercises (startup timeout, unavailable Docker, assertion failure, teardown failure, planted-secret stdout/upload, cancellation cleanup, existing-runner nonregression) each produce the required nonzero/exit-1 outcomes with diagnostics/cleanup attempts as applicable.
- **SC-006**: Authorized operator/owner confirms `main` branch protection lists the actual published `compose-smoke` context as required; evidence is recorded separately from workflow file presence; pending keeps Done and implementation merge-bar open; accessible protection API omission is not treated as PASS or as proof of global absence.
- **SC-007**: Applicable Phase B gates report honest verdicts with no threshold lowering; LocalPlay remains false; Phase 2 active process-play POST through nginx returns 403 with no process launch; browser play provenance remains ticket summary + Patron Q2/Q4.

## Assumptions

- Budgets and zero whole-test retries are approved `[assumed]` choices recorded in `ASSUMPTIONS.md` (Q5), owned by one validated DEV-471 budget config at `web/e2e/fixtures/dev471/budgets.json`.
- T001 pickup recon is complete; candidate fixture Chromium decodability is demonstrated in recon-DEV-471:12-35 (identity and limits in brief.md T001 closure). Full-stack fixture-correlated playback remains future acceptance work; STOP for recon if required identities or seams drift.
- `recon-DEV-471` retains DEV-440 evidence at lines 1–25 and appends plan-adjudication facts at lines 27–73 (verbatim ticket body 36–48). Historical CONCLUSIONS line citations must not be remapped to the current note.
- Proposed ADR 0043 remains proposed until Gate 1; drafting references it without claiming acceptance. Pre-Gate 1 ADR-review receipt points to `PLAN_ADJUDICATION.md`.
- Ordinary owner Gate 1 and PLAN FROZEN are distinct from Patron grill closure and from pending SC-006/T011; Quill invents neither freeze nor Gate 1 clearance; no runtime/gates/protection PASS is claimed from planning.

