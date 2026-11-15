# DEV-472 Grill Brief

## Authority and status

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-472-spec`.
- Branch: `feature/472-spec`; intake baseline: `e06587031dae1d974a41ee98e7b23aa291107129`.
- Factual authority: `recon-DEV-472` lines 4-23 and baseline receipts in `DEV-472`. Decisions: Patron Q1-Q6, full exchanges and cited bases in `CONCLUSIONS.md`.
- This is the Phase 2 grill brief for a size:M workflow ticket. Quill drafts spec, plan and tasks from these decisions. No implementation, plan-challenge freeze, Gate 1 approval, hosted acceptance, review closure or merge clearance is claimed here.
- `PYTHONUTF8=1` is required for Spec Kit commands.

## Goal and frozen scope

Move full Core mutation cost off main push. PRs run the existing diff-scoped mutation gate; scheduled nightly and manual dispatch retain full Core mutation and report evaluation. Preserve mutation policy and distinct property-test coverage.

Frozen scope: Q1-Q5 event routing, immutable PR base selection and honest exit handling, full nightly/manual sharding and evaluation, cancellation isolation, no scheduled image publication, unchanged mutation safeguards, the no-duplicate property-test end-state, and acceptance evidence; anything else is a follow-up issue, not a finding in this round.

No new dependency, project, architectural layer, API, schema, production code or frontend change. No LocalPlay, secrets or process-execution change. No new ADR or glossary term is required by these decisions. No structural owner checkbox has been identified in Q1-Q6.

## Every grill answer

The full question, answer, rationale and implications for each exchange are append-only in `CONCLUSIONS.md`.

1. **Q1:** Patron approved keeping the single property-tests job with separate UnitTests and IntegrationTests calls. The ticket duplicate references are stale; the outcome is already satisfied at this baseline. Record the stale references through Rigger; do not delete distinct coverage or manufacture a deletion. Basis: recon lines 11,22 and charter section 2.3. No structural checkbox.
2. **Q2:** Patron approved a single existing workflow: PR uses `scripts/run-mutation.ps1 -BaseRef`; main push runs no Stryker; schedule and manual dispatch run the existing 16-shard `stryker-core` plus merged-report `stryker-core-gate`. Exclude push and PR from full runs; exclude schedule from image publication. Basis: recon lines 4,8-10,12,14,18,21.
3. **Q3:** Patron approved explicitly fetching the immutable PR base SHA and passing it to `-BaseRef`, with sufficient history and PR head availability. Never use Stryker `--since`. Fail on exit 1 or inability to run. Show accepted exit 2 as `SKIPPED (scope-empty)` or `N/A`, never mutation PASS. Preserve existing policy and safeguards. Basis: recon lines 4,12,14 and AGENTS.md.
4. **Q4:** Patron approved nightly cron `17 3 * * *` (03:17 UTC) on main, isolated from push/PR cancellation, with the existing full Core lane and manual dispatch. Exact time is operational taste, recorded `[assumed]` in `ASSUMPTIONS.md`; runner/scheduler availability can delay start. Basis: recon lines 4,8-10,14,21.
5. **Q5:** Patron approved static event-matrix checks, the existing synthetic runner fixture, and distinct hosted runtime receipts. A genuine green scheduled full Core run is required; manual dispatch is preflight only. Scope-empty workflow PR success does not prove mutation regression blocking. Do not ship artificial production edits for proof. Spec/Gate 1 status is separate from runtime acceptance. Basis: recon lines 4,8-13,21.
6. **Q6:** Patron approved a `.github/workflows/ci.yml`-only implementation, four-step task ordering, all-severity review closure and bounded rounds. Reuse scripts unchanged and do not edit generated `stryker-config.json`. A discovered runner defect requires recon and a ruling before scope expansion. Basis: recon lines 4,8-14,17-18,21 and AGENTS.md.

## Plan decisions and files

The only implementation file authorized by this grill is `.github/workflows/ci.yml`. Planning artifacts live under `specs/DEV-472/`. Reuse `scripts/run-mutation.ps1`, `scripts/ci-stryker-shard.sh`, and `scripts/Test-RunMutation.ps1`; do not rewrite them. Do not hand-edit generated configuration.

| Event | Diff-scoped PR mutation | Full 16-shard Core + evaluator | Image publication |
| --- | --- | --- | --- |
| pull_request to main | Required, stable check | Excluded | No new permission; preserve existing event policy |
| push to main | No Stryker | Excluded | Preserve existing non-nightly behavior |
| schedule on main | Excluded | Required | Excluded |
| workflow_dispatch | Excluded | Required, diagnostic/preflight | Preserve existing non-nightly behavior |

PR comparison uses the immutable base SHA from its event payload and the PR head, not a moving branch tip. Missing base/head history fails closed. Runtime wiring must pass explicit `-BaseRef`, preserve runner exit meanings and publish a stable PR check result. It must not introduce `--since` or relax thresholds, timeout policy, exclusion rules, kill-share or kill-file-span safeguards.

Full runs reuse current 16-shard orchestration, report artifacts and merged-report evaluation. Every shard and the final evaluation must complete for a green full-run receipt. Scheduled run concurrency must be isolated so ordinary push/PR runs cannot cancel it. Ordinary workflow jobs retain their existing non-nightly behavior; scheduled events must not accidentally publish an image.

## Task ordering

1. Encode the event matrix, stable PR check, immutable base fetch and runner exit translation.
2. Wire nightly/manual full-run guards, scheduled publication exclusion and cancellation isolation; retain the single property-tests job and both distinct project calls.
3. Validate event conditions and runner-contract compatibility with the existing fixture, then run applicable harness gates. Do not add unrelated infrastructure or alter runner policy.
4. Maintain a hosted evidence ledger with run URLs, immutable SHAs, event types and actual outcomes. Obtain PR regression-blocking evidence, no-Stryker main-push evidence and the green scheduled full-Core/evaluator receipt. Keep missing receipts explicitly pending.

A factual prerequisite absent from recon is `needs recon: <question>` to Bernstein. Do not guess exact workflow wiring or expand the file set to solve a newly discovered runner defect.

## Test strategy and acceptance evidence

- Check all four event types against the matrix, including negative assertions for excluded mutation paths and scheduled publication.
- Check explicit immutable `-BaseRef` wiring, history preparation, stable PR result, cancellation isolation and the no-duplicate property-test outcome.
- Run `scripts/Test-RunMutation.ps1`. Recon lines 12-13 identify its synthetic runner/classification/evaluation coverage. Its success does not demonstrate a real nightly production mutation run.
- A changed-production-C# PR must demonstrate a mutation regression makes the hosted mutation check fail. Capture runtime evidence without introducing artificial source changes in the delivery diff. A workflow-only scope-empty PR is insufficient evidence.
- A main-push run must demonstrate no Stryker execution.
- A scheduled run on the delivered main revision must complete full Core mutation across all 16 shards and pass merged-report evaluation. Record its SHA and run URL. A manual dispatch can be preflight, not a substitute for this acceptance criterion.
- No runtime receipt exists merely because this brief approves its requirement. Planning, merge clearance and post-merge ticket completion remain distinct.

## Gate expectations and review loop discipline

Apply the current harness gate applicability rules. Baseline note `DEV-472` reports Roslyn, complexity and web lanes as exit 2 scope-empty, never PASS. For the expected workflow-only implementation, production-C# mutation and web lanes remain scope-empty unless the actual diff proves otherwise. Run the runner fixture and meaningful workflow validation; do not fabricate production scope to turn a skipped lane green.

Keep statuses exact: exit 1 and `Could not run` block; script-reported `SKIPPED (scope-empty)`, mutation `N/A` and configured harness `SKIP` are non-blocking and never PASS. A property-test opt-out requires the standing recorded reason; do not invent one.

- **Closing bar for code-review/ship-review:** Every unresolved in-scope Critical, High, Medium or Low finding blocks closure. Each finding needs an explicit evidence-based disposition; no severity silently falls below the bar. On M, Sentry, Ledger and Compass evidence is mandatory for adjudication.
- **Frozen scope:** The scope paragraph above applies; anything else is a follow-up issue, not a finding in this round.
- **Round cap:** Two review rounds maximum, at most two fix commits per round. Third-round findings go to owner review through the PR; the cap never waives an unresolved blocker or grants merge clearance.

## Handoff

Quill should draft spec.md, plan.md and tasks.md from this brief, with explicit tasks for every acceptance receipt and honest pending runtime status. Keel subsequently runs read-only speckit-analyze, checks consistency against this brief and adjudicates plan challenges before any plan freeze. The settled terms here and the existing glossary/ADRs are ready to feed speckit-specify; do not invent new terminology or contradict existing ADRs. This grill does not grant Gate 1 or authorize implementation in the Spec workspace.

## Final shared-understanding confirmation (Q7/12)

Patron read and confirmed brief.md, CONCLUSIONS.md and ASSUMPTIONS.md accurately capture Q1-Q6 with no unresolved scope decisions. The grill is closed after seven questions; Quill may draft. The full final question and answer are recorded in CONCLUSIONS.md Q7. Plan challenge/freeze, Gate 1 and runtime acceptance remain separate and pending their own evidence.

Patron routed the stale ticket-reference comment to Rigger using `C:/Users/lamun/AppData/Local/Temp/dev-472-youtrack-comment.md`; the verified recording receipt remains pending. This is a recording status, not a scope blocker or a verified YouTrack write.

## Phase 2 step 6 decision addendum (2026-11-14)

Patron rulings in CONCLUSIONS.md Phase 2 Step 6, committed at 6f87b755, govern this addendum. Recon baseline remains e06587031dae1d974a41ee98e7b23aa291107129; recon lines 31-35 provide targeted configuration facts. The full Patron answer is recorded in CONCLUSIONS.md and the naming/location taste in ASSUMPTIONS.md.

- Add a new PR-only job id `mutation-pr`, fixed display/check name `PR mutation`. Recon:34 reports no required checks and zero repository rulesets visible to the CLI. The user owns external required-check registration, coordinated by Bernstein/Rigger with live API read-back. This plan does not authorize repository-settings writes. The ticket already requires merge blocking; no structural checkbox is added. Runtime acceptance requires registration plus evidence that a red mutation regression blocks normal merge, not merely a failed job.
- Preserve absence of any concurrency block (recon:35), across all four events; no new group or cancellation policy. This satisfies Q4/FR-007 without invented changes. Stop for recon/ruling if pickup baseline drifts.
- Preserve the existing `push` plus `refs/heads/main` publish guard (recon:26); schedule, dispatch and PR remain excluded. Verification-only unless authorized baseline drift demands a ruling. Keep the distinct property-test calls.
- Keep T003 limited to PR full-lane exclusion. T004 must atomically add nightly schedule and remove main-push full mutation. The US1 checkpoint must preserve existing automatic push coverage until the schedule replacement is in the same releasable workflow edit.
- Concretize Q3 through immutable payload head checkout with full history (`fetch-depth: 0`), explicit immutable base-SHA fetch and verification of both commits, then explicit `-BaseRef`. Missing/failing preparation fails closed. Reuse action versions from pickup recon and grant the PR job `contents: read` only.
- Bind Q3 exit translation: invoke runner in child `pwsh` process, preserve output and immediately capture exit; 0 preserves actual mutation PASS, accepted 2 writes script-reported `SKIPPED (scope-empty)` or `N/A` to job summary and wrapper exits 0, while 1, launch failure or unexpected status fails. No blanket continue-on-error. Accepted exit-2 summary classification must be verified, never guessed. A successful hosted check is distinct from mutation PASS.
- T006 records mechanical checks with exact commands, expected/observed outcomes and exit codes for YAML structure and all four event conditions, immutable history/base/head, exit translation, check identity, no concurrency, publish negatives and property calls. No new dependency, framework or script edit. Keep the existing synthetic fixture and applicable gates; no runtime claim from static validation.
- Ledger home: `specs/DEV-472/EVIDENCE.md`. A separate non-delivery changed-production-C# PR supplies regression evidence with PR/base/head/run identity; never add artificial `src/` to the delivery diff. Registration evidence, main-push no-Stryker and genuine scheduled all-16-shards/evaluator receipts remain pending until obtained. Q5 hosted evidence scope stays unchanged; exit-2 labelling has static and synthetic assurance with explicit residual lack of hosted-label proof.
- Remove stale routing comments within ci.yml (recon:8), without explanatory replacement prose. ADR 0016 and AGENTS.md remain outside implementation diff. Patron disposition of their Medium historical-routing drift is **noted, no ticket**, not a follow-up request.

Repair bar: every accepted plan repair must be independently read back; all three axis reports remain mandatory. Two Quill repair rounds maximum. No implementation in this Spec workspace. This addendum is a decision record, not a PLAN FROZEN declaration.

## PLAN FROZEN - Phase 2 step 6 (2026-11-15)

**PLAN FROZEN.** Keel adjudicated all 18 findings from Sentry (8), Ledger (5), and Compass (5), obtained Patron rulings recorded in CONCLUSIONS.md Phase 2 Step 6, and independently read back Quill repairs against this brief. All accepted in-scope plan defects are repaired or explicitly closed by the cited ruling; no unresolved plan blocker remains. Full dispositions, requirement/task coverage and file:line verification are in PLAN_ADJUDICATION.md.

Two Quill repair rounds used: round 1 substantive cross-artifact repairs; round 2 corrected Keel original aggregate-count typo from 19 to 18 in the receipt/report only. Cap exhausted; no third repair round is authorized. Frozen artifacts are this brief, spec.md, plan.md, tasks.md, CONCLUSIONS.md, ASSUMPTIONS.md, checklist, DRAFTING_RECEIPT.md and PLAN_ADJUDICATION.md as read back in this worktree. Repository HEAD is 6f87b7550a88743dbb14bbc78db9e05020667eae (Patron rulings); most planning artifacts remain uncommitted for Rigger spec-PR preparation. This is an artifact freeze, not a claim that HEAD already contains the complete plan.

The frozen plan preserves the ci.yml-only implementation boundary, mutation safeguards, one property-tests job/two calls, no concurrency settings and existing publish guard. It binds mutation-pr / PR mutation, immutable base/head preparation, explicit fail-closed exit translation, atomic nightly/push cutover, mechanical validation and EVIDENCE.md receipt tasks. User-owned required-check registration remains an explicit external acceptance dependency coordinated by Bernstein/Rigger; this plan authorizes no settings mutation. Historical ADR/AGENTS prose drift is noted, no ticket per Patron.

**Next:** Bernstein/Rigger prepare the ordinary spec PR. Only the user merges; T000 verified Gate 1 merge receipt blocks T001+. **Still pending:** Gate 1, pickup drift check, implementation, required-check registration/read-back/red-block proof, hosted main-push and genuine scheduled 16-shard/evaluator acceptance, code-review/ship-review, merge clearance and ticket completion. No runtime tests or gate PASS are claimed by plan freeze.
