# DEV-471 — Compose smoke grill brief

Status: PLAN FROZEN — Keel, 2026-11-15. Gate 1 spec PR #414 merged at 0e6a68934ef8767dc00618aecd22d3376f8fb913 (receipt synchronized 2026-11-16). Phase B T001 complete on 2026-11-16; Anvil cleared for local tasks.md Phase 2 T002-T004. See T001 closure and Phase 2 dispatch clearance below. Compose runtime acceptance, implementation gate results, protection verification and merge clearance remain open.

Worktree: F:/Dev/LamuFlix.worktrees/feature-471-spec  
Branch: feature/471-spec  
Intake HEAD: e06587031dae1d974a41ee98e7b23aa291107129. Latest recon snapshot: 0e13095865948a342ffe30b4279e059c66440eab. These identify evidence snapshots, not a claim of the current post-document-write HEAD.  
Claimed ADR: 0043, assigned by Conductor and recorded in chain.  
Fact source: recon-DEV-471. The note retains DEV-440 evidence at lines 1–25 and appends current plan-adjudication recon at lines 27–73, including verbatim ticket body at lines 36–48. Historical initial-snapshot citations in CONCLUSIONS.md must not be remapped to current line numbers. Current verbatim ticket body and the appended Patron provenance ruling supersede Q2 provenance wording.

## Closing bar, frozen scope and round cap

All verified unresolved Critical, High, Medium and Low contract/safety/acceptance findings block closure. Speculative abstractions defer. Two review rounds, at most two fix commits per round; unresolved findings at the cap escalate for owner review. Standing scope-empty SKIPPED, configured SKIP and mutation N/A remain non-blocking where the pipeline permits them and never count as PASS. A failed or unrunnable applicable gate blocks.

Frozen scope (Q2 supersedes the intake-only Q1 scope): a required check on every PR targeting main, using shipped docker-compose.yml, proving healthy startup, known fixture import, stubbed-provider enrichment, thumbnail generation, persisted library listing, /hubs/ live updates, browser in-page playback, DEV-440 regression failure proof, isolation, CI wiring, diagnostics and cleanup; anything else is a follow-up issue, not a finding in this round.

Q2 Patron ruling establishes that browser playback preserves the ticket. Features:LocalPlay remains false and shipped nginx must continue returning 403 for the local process-launch route. There is no open structural escalation from this grill. A later ticket contradiction or necessary constitution departure must still take the section 2.3 owner-checkbox path.

## Approach and architectural decision

One stable required check, compose-smoke, executes two separately isolated positive phases:

1. Start plain shipped docker-compose.yml without the e2e Ollama endpoint override. Provision real Ollama/model readiness and require API health. This covers the production endpoint binding fixed in DEV-440.
2. Start a separate shipped-base plus existing web/e2e/compose.e2e.yml overlay stack for deterministic external-provider-stubbed full-flow assertions.

The overlay overrides the very endpoint fixed by production commit 4646476fb1f8afb8539a07c8f404c911a5fc174e. It therefore cannot alone establish production startup coverage. The separate plain startup phase deliberately accepts real model provisioning cost; two stack lifecycles are the price of unmasked startup coverage plus deterministic flow. Record this in docs/adr/0043-unmasked-compose-startup-and-stubbed-smoke.md.

DEV-440 proof is delivery acceptance evidence on the implemented candidate, repeated whenever its startup runner or health assertion changes. Run the identical plain-stack health assertion against a disposable controlled revert of 4646476f, outside the PR checkout. Candidate startup must pass; reverted startup must fail specifically for the expected API startup problem after dependencies are ready. Exit 139 is expected where observed, but lack of API health due to that startup failure is the substantive oracle. Docker/download/build/setup failures do not establish this proof. Do not claim this expected regression has already been exercised.

## Full-flow acceptance contract

Before import, establish the browser /hubs/ connection through shipped nginx. Trigger import of a known fixture through the real application stack. Correlate every assertion with that fixture:

- The imported record persists and appears in library listing.
- Provider enrichment yields deterministic stub-defined fields.
- A fetched thumbnail is nonempty and decodes as an image.
- A genuine server-originated /hubs/ event for that fixture updates the UI without reload or polling substitution.
- The fixture plays in the in-page browser player through shipped nginx, with valid media metadata and currentTime advancing while playing.

A handshake, HTTP 200, visible player, unrelated event or preexisting library entry alone is insufficient. Only external third-party/LLM providers are stubbed; no application API interception or mocked application/hub responses can satisfy acceptance. Use existing API contracts and generated TypeScript types. Existing sample media decodability is not established; verifying it is an explicit first task.

## CI operation and failure behavior

Every main-targeting PR executes both positive phases without path filters that bypass the check. Missing Docker, failed model download, unhealthy stack, timeout, assertion failure and teardown failure cause exit 1, never green or SKIPPED for this required smoke check. This rule does not reclassify unrelated harness scope-empty exits.

Use randomized per-run/per-phase Compose projects, generated ephemeral credentials/settings, isolated data/model state and fixtures; never commit secrets, mutate the PR checkout or clean unrelated projects. Capture sanitized container logs, service health/exit status and browser failure artifacts before bounded project-scoped teardown. Always attempt cleanup; cleanup failure remains failure.

Budgets: 30 minutes per stack startup, 120 seconds per async assertion, 3 minutes teardown per project, 120 minutes total job cap; zero automatic whole-test retries. These are approved choices, tagged assumed in ASSUMPTIONS.md. A timeout is failure, not acceptance evidence of a specific regression unless the expected startup cause is established.

An authorized repository operator/owner must verify main branch protection requires compose-smoke. Workflow publication alone does not prove this. Keel/build seats do not make unapproved admin mutations.

## Authorized files and boundaries

Targeted existing-file edits:

- .github/workflows/compose-smoke.yml: new dedicated unfiltered main-targeting PR workflow, budgets and sanitized artifacts; replaces Q6 ci.yml edit authorization. Existing ci.yml and promote-home.yml remain untouched.
- web/e2e/run.mjs: minimal smoke selection/configuration and strict exit integration while reusing the existing overlay lifecycle.

New files:

- scripts/run-compose-smoke.ps1: plain startup, two-phase coordination, disposable regression mode, diagnostics and cleanup.
- web/e2e/dev471-compose-smoke.e2e.ts: real full-flow assertions.
- web/e2e/fixtures/dev471/**: only fixture assets/settings needed by this smoke.
- specs/DEV-471/** and docs/adr/0043-unmasked-compose-startup-and-stubbed-smoke.md: planning evidence.

Reuse docker-compose.yml and web/e2e/compose.e2e.yml unchanged. No new packages, projects, top-level architectural layer, schema/public API/production C#/web UI/nginx changes, rewrites of existing fixtures or generated contract changes are authorized. If a required Compose edit or missing production capability emerges, stop for bounded recon and Patron ruling; do not silently expand the plan. Q6 is explicit section 2.3 item 6 authorization for the named targeted edits.

## Task ordering and test strategy

1. Bounded reconnaissance verifies existing route/event/bootstrap contracts, runtime credential/settings setup and Chromium fixture decodability. Findings are facts to recon, not assumptions or proposed production fixes.
2. Implement the plain-startup runner, disposable DEV-440 proof mode, diagnostics and project-scoped cleanup.
3. Add fixture-correlated real stubbed-flow tests and minimal existing runner integration.
4. Wire the stable required CI job and arrange authorized required-check verification.
5. Execute both positive Compose phases; validate startup timeout, unavailable Docker, assertion failure and teardown failure all produce nonzero status and diagnostics/cleanup attempts. Capture candidate-positive and expected-revert-negative DEV-440 proof with source identities and observable cause.

Meaningful runner failure-path tests may simulate orchestration failures; they do not replace real-stack positive acceptance or the controlled-revert proof. No production behavior is mocked to satisfy smoke acceptance.

## Gates and handoff

Phase 2 remains: Quill drafts spec/plan/tasks from this brief; size L also requires clarify/checklist; Keel performs read-only speckit-analyze and brief consistency review; three-axis plan challenge and ADR review precede Gate 1. A completed grill does not freeze a challenged plan or open Gate 1.

Build phase expectations: npm run typecheck:e2e, relevant web gates and pipeline-required harness gates; preserve each script's actual status and scope-empty distinctions. No threshold changes. Both real Compose phases, failure-path proofs, controlled-revert evidence and authorized required-check registration verification are necessary delivery evidence. No build/runtime tests were run by this planning seat.

## Every grill answer

- Q1: Patron approved all-severity contract/safety/acceptance closing bar, two rounds with at most two fix commits per round, and bounded scope. Q2 explicitly expands the intake wording to the full ticket requirements.
- Q2: Patron approved full authoritative scope and browser-play interpretation, LocalPlay false/nginx 403 preserved; no structural escalation.
- Q3: Patron approved separate plain startup and overlay flow, disposable production-revert proof and ADR 0043 tradeoff.
- Q4: Patron approved the complete fixture-correlated real-stack acceptance contract, genuine hub update, decodable thumbnail and advancing video; existing fixture compatibility must be verified.
- Q5: Patron approved unconditional PR execution, exit1 on all smoke failures, isolated ephemeral state/secrets, diagnostics, cleanup, budgets, no whole-test retries and delivery/change-triggered DEV-440 proof.
- Q6: Patron approved the exact file scope, reuse boundaries, five-step order, validation gates and authorized operator verification of required status.
- Q7: Patron approved and ratified the complete brief and proposed ADR without corrections, confirmed shared understanding, and closed the grill at seven questions for Quill drafting.

The complete verbatim questions and responses, full cited Patron rulings, rationale and implications are preserved append-only in CONCLUSIONS.md. Q1–Q7 above reproduce every confirmed answer in actionable form; the full Q7 exchange is recorded before closure. ASSUMPTIONS.md records the approved assumed budget/retry policy. No new domain glossary terms were introduced; no CONTEXT.md change is needed.


## Closure receipt

Patron Q7 approved and ratified this brief and proposed ADR without corrections. The append-only full Q1–Q7 exchanges and cited rulings are recorded in CONCLUSIONS.md. No unanswered grill decision remains. The earlier missing ticket/playback/DEV440 facts were resolved through Conductor-provided recon before subsequent decisions. Remaining fixture/route/bootstrap reconnaissance is the approved first build task, not an invented implementation fact. Existing canonical vocabulary and ADR 0043 are ready to feed speckit-specify; the spec must not contradict them. Gate1 remains closed until the separate documented plan/review/owner merge process completes.

## Plan challenge decision record — playback provenance

Patron plan-adjudication scope ruling (CONCLUSIONS.md, “Verbatim ticket provenance vs play and scope confirmation”) corrects Q2 provenance only. The verbatim ticket Wanted/Done when in recon-DEV-471:36–48 mandates shipped Compose healthy startup, fixture import, stubbed enrichment, thumbnail, library listing, hubs, every-main-PR blocking check and DEV-440 revert proof; it does not mention play. Playback derives from the ticket summary preserved in Patron cited ruling plus Q2/Q4 verification policy. Patron rules advancing browser playback justified verification within the existing wanted path, with no scope change, constitution departure or section 2.3 owner checkbox. Existing LocalPlay false/nginx 403 boundary remains. Gate 1 remains closed.

Full question: [from Keel] DEV-471 plan adjudication needs cited scope ruling before Quill repair. recon-DEV-471:36-48 now preserves verbatim ticket: shipped Compose healthy/import/enrich/thumbnail/listing/hubs and DEV440 proof; it does not mention play, whereas CONCLUSIONS Q2 normalized record and brief claim ticket mandates play. Please decide whether browser playback is a scope addition requiring section 2.3 owner checkbox, or justified verification within existing wanted path; do not silently call it ticket text. Recommend correct provenance and explicitly rule whether scope changes.

Answer: Patron approved as justified verification within the existing wanted path, NOT blocked: structural, no scope change and no section 2.3 owner checkbox. Playback originates in YouTrack summary and Patron Q2/Q4; correct attribution in all active artifacts. Full cited ruling is append-only in CONCLUSIONS.md. Other plan-challenge decisions and repairs remain pending; PLAN FROZEN withheld.

## Plan challenge decisions — Patron groups 1–6 (authoritative amendment)

Basis: appended “Plan Adjudication Rulings (Groups 1–6)” in CONCLUSIONS.md; recon-DEV-471:27–73. This amendment supersedes the old Q6 ci.yml edit and any earlier unverified capability/provenance wording. Full six-group question and recommendations are retained in PATRON_PLAN_QUESTIONS.md; Patron approved every group, NOT blocked: structural, no owner checkbox, with the cited reasoning recorded append-only in CONCLUSIONS.

1. **CI/file set**: New `.github/workflows/compose-smoke.yml`, `pull_request` to main with no path filters, job id/displayed name `compose-smoke`; leave `ci.yml` and `promote-home.yml` intact. Existing e2e-smoke coexists. Minimal authorized `run.mjs` explicit selection excludes DEV471 from ordinary npm e2e and selects it in required smoke.
2. **Runtime bootstrap**: Each phase runs from a disposable candidate-HEAD source copy outside the PR checkout. Generated `docker/api.settings.json` there is runtime preparation, not a source edit. Positive Compose/nginx files remain unchanged; phase1 has no endpoint override, phase2 uses existing E2E_SETTINGS_FILE overlay seam. Separate randomized project/data/model/fixture state and fixture-identity assertions; T001 verifies exact library mount input and routes/events/media, stopping if unavailable. Negative copy is candidate HEAD plus reverse only `4646476f`; patch mismatch fails. Record source/patch identity and original checkout baseline unchanged including preexisting changes. Do not run the historical parent as candidate.
3. **Acceptance discharge**: Phase2 actively POSTs existing `/api/movies/<fixture-id>/play` through shipped nginx and requires403, LocalPlay false, no process launch. Phase1 requires API health after Postgres/RabbitMQ health and real Ollama/model readiness; phase2 real flow proves web/nginx coverage. Browser playback remains required under corrected provenance above.
4. **Protection and rollback**: Actual published compose-smoke context must be verified required by authorized operator before T011/SC006/ticket Done and implementation merge-bar closure. Pending stays open/non-PASS; spec Gate1 independent. No postmerge-only registration assumption and no build-seat admin mutation. Sentry M5 removal of required check rejected as ticket contradiction; disposition noted, no ticket.
5. **Proof/budgets**: Named `-RegressionProof` runner mode on delivery candidate and workflow-triggered whenever runner or extracted startup/health code changes; if diff/base unresolved run proof fail-closed. Ordinary unrelated PRs run both positives only. T010 final-candidate proof supersedes developmental T004. Classifier requires successful Docker/build/settings and healthy real dependencies/model, never-healthy API with attributable localhost/embedding startup failure;139 optional corroboration, no infrastructure/unexplained timeout substitute. Single validated DEV471 budget config under authorized `web/e2e/fixtures/dev471/` drives runner and workflow prep outputs:30m startup/120s assertion/3m cleanup/120m cap/zero retries, cleanup inside cap, no threshold or harness rewrite.
6. **Diagnostics/conventions/gates**: New safe allowlisted staging with compose-smoke artifact name, generated-value/credential/connection-string redaction before console/upload, no raw secret settings/env, unsafe binary browser artifacts excluded/regenerated, fail-closed upload guard and planted-secret proof. No claim Ledger's upload helpers already exist; reuse only if T001 verifies within authorized paths. Verify helper/wrapper/gate interfaces via help at pickup. Required smoke exits1 for all operational/assertion/teardown failures, preserving local wrapper behavior elsewhere. Run explicit scripts/run-web-gates.ps1 for web edits plus npm run typecheck:e2e and pipeline harness gates with honest statuses. Record pre-Gate1 ADR alignment and per-item checklist results.

Additional execution detail within approved policy: register each created project before startup; bounded runner finally/termination handling plus CI always-run project-scoped cleanup with reserved cap time; hard runner loss cannot guarantee teardown. Capture sanitized diagnostics before teardown and preserve failure on cleanup failure. Test deliberate existing-runner failure nonregression and all required smoke failure classes, planted-secret stdout/upload safety, and cancellation cleanup attempts. No unrelated project cleanup. These are accepted safety/acceptance repairs, not runtime receipts.

Question: [from Keel] Please settle six decision groups in PATRON_PLAN_QUESTIONS.md using recon and adjudication, append cited rulings in CONCLUSIONS only and report. Answer: Patron approved all six, amended Q6 as above, rejected Sentry M5 removal as noted/no ticket, no structural block or owner checkbox. Quill repairs drafted artifacts against these recorded decisions; Keel verifies before any PLAN FROZEN declaration. Maximum two repair rounds remains.

## Round 1 verification decision — budget ordering

Read-only cross-artifact analysis found T002–T004 consume single budget config while T005 owns its creation and is later/parallel. Resolve within approved group5: T002 creates `web/e2e/fixtures/dev471/budgets.json` before first runner use (same approved values); T005 owns fixture assets/settings excluding that config. T002/T005 file sets are disjoint, T005 remains parallel after T001. Plan/task order and FR012 closure pointers must reflect config available before developmental startup proof. This is task-order/ownership clarification inside approved scope, not a new budget or dependency.

Existing e2e-smoke is confirmed as a job, not confirmed required by protection; only compose-smoke is specified as required under this ticket. Correct unsupported required-check wording in active spec.

## Phase B pickup ruling — Keel (2026-11-16)

This current receipt supersedes historical Gate 1 pending wording in the planning artifacts and exchanges. DEV-471 task note lines 4, 28 and 32 records spec PR #414 merged at `0e6a68934ef8767dc00618aecd22d3376f8fb913`; lines 36-39 records restoration at `c03be7387ca2fb736977c9d3436067802cb7a82b`. Synchronize T000 as complete on that supplied verified receipt; this is not a new approval or live GitHub re-verification.

T001 remains incomplete. Current `recon-DEV-471:17-51` establishes mount/bootstrap, route/event/generated-type seams, LocalPlay protection and helper interfaces at the pickup HEAD. Lines 39 and 50 explicitly leave Chromium decoding unverified; codec metadata and existing test source do not satisfy the browser-decoding checkpoint (`tasks.md` Phase 1; `spec.md` US1). Baseline evidence is recorded separately in DEV-471 task note lines 20-24. No production-seam contradiction requiring a scope change is established by the supplied recon.

Phase 2 T002-T004 dispatch remains blocked until Wisp supplies actual Chromium decoding/advancing-playback evidence for an identified candidate fixture, or an explicit failed/unavailable result for further disposition. Do not move this prerequisite into T004 or substitute ffprobe metadata. The missing fixed settings source is covered by approved disposable-copy generation; plain startup must supply it without the overlay endpoint override. `_gate-common.ps1` is dot-sourced and has no standalone Help interface; do not invent a flag. T001 stays unchecked. No substantive scope or task-order change is made.

Historical recon citations remain historical; current pickup evidence uses the explicitly dated section at lines 17-51 and must not be substituted for the earlier note layout.

## PLAN FROZEN — Keel receipt (2026-11-15)

Phase 2 step6 complete. All three challenge reports read (Sentry10, Ledger7, Compass9); all26 findings explicitly dispositioned in PLAN_ADJUDICATION.md, accepted repairs independently verified after Quill rounds1 and2 (2/2 cap), with two residual round1 issues closed in final round. Read-only speckit-analyze prerequisite succeeded; final analysis26/26 FR+SC covered across15 tasks, no unresolved inconsistency/ambiguity/constitution/coverage finding. Reviewer compose-smoke checklist27/27 reviewed with individual evidence (CHK027 answer NO), requirements checklist revalidated. ADR0043 final content aligns and remains proposed until Gate1.

Frozen implementation decisions are Q1–Q7 as amended by playback provenance, Patron groups1–6 and budget ownership clarification above: dedicated unfiltered workflow; disposable candidate settings prep; real plain startup then isolated stubbed real flow; active403/LocalPlay false; reverse-only4646476f causal proof and change-triggered RegressionProof; one budgets.json owned by T002 before runner use; safe redacted diagnostics and bounded cleanup; operator actual-context required-registration verification blocking Done/implementation merge-bar. Existing ci/promote-home/Compose/nginx remain unchanged.

This freeze supersedes historical pending/withheld language in earlier entries. Keel may synchronize only status pointers in spec/plan/tasks to this decision after Quill has finished; substantive artifacts remain Quill-authored. Ordinary owner Gate1 is still closed until Rigger/Bernstein spec-PR preparation and verified user merge. T001+ remain blocked. Pickup facts (exact mount/routes/events/media/help surfaces) retain explicit recon/STOP obligations; live positive/revert/failure/gate/protection evidence is future work, never inferred from this freeze. No structural owner checkbox is open per cited Patron rulings. No implementation or merge authorized by freeze.


## T001 closure and Phase 2 dispatch clearance — Keel (2026-11-16)

Decision: T001 is complete; Anvil is cleared for local tasks.md Phase 2 T002-T004 in `F:/Dev/LamuFlix.worktrees/DEV-471`, branch `feature/DEV-471`. This supersedes the earlier T001-open/dispatch-blocked status and historical Gate 1 pending language. The scope, approach, task order, authorized files, test strategy and closing bar stay as frozen.

Basis: prior pickup reanalysis and recon disposition recorded above and in DEV-471 task note lines 41-57; pre-edit baseline receipts in DEV-471:20-24; remaining fixture-decoding prerequisite now demonstrated by Wisp in `recon-DEV-471:12-35`, section Chromium probe execution. The approved script is in note `DEV-471-Keel-Chromium-probe`. Receipt: exit 0, verdict PASS; `docker/sample-library/Big Buck Bunny (2008).mp4`, 202927 bytes, SHA-256 `3a82a333fe6127cc03bcdbc4e3aff69a6c99abcd34a9a14d60e870d9ced8de07`; Playwright 1.65.0, Chromium 158.0.8127.18; dimensions 320x180, duration 8 seconds, currentTime 0 to 0.004345, readyState 4. These are supplied execution facts, not a probe rerun by Keel. Earlier recon line references retain their historical layout; current probe evidence is specifically lines 12-35.

Dispatch order: T002 creates the single approved `web/e2e/fixtures/dev471/budgets.json` before runner use and implements plain-startup coordination; T003 adds disposable reverse-only `4646476f` RegressionProof mode and diagnostics/cleanup; T004 records developmental positive/negative proof. Preserve the authorized disposable settings bootstrap, unchanged shipped Compose/nginx, LocalPlay false and existing STOP boundaries. No unresolved pickup fact remains in the supplied evidence.

The loopback fixture probe establishes candidate media decodability only. It does not establish seeking, nginx/API/Compose playback, healthy plain startup, full-flow acceptance, controlled-revert proof, failure-path safety, implementation gates or required-check registration. T002-T014 remain unchecked; T010 final-candidate proof supersedes developmental T004; T011 remains a Done/implementation-merge-bar prerequisite. No merge authorization.
