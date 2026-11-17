# Feature Specification: Move full Core Stryker off main push (PR diff gate + nightly full run)

> **Superseded in part (2026-11-17):** the `mutation-pr` job (check name `PR mutation`) described here was removed from `.github/workflows/ci.yml` by PR #434. Gauge already runs diff-scoped mutation (`scripts/run-mutation.ps1`) in Phase 5, just before the PR opens, and no new code lands after that, so the PR-side run only repeated it. Do not register `PR mutation` as a required check. The rest of DEV-472 still stands: no Stryker on main push, and the full 16-shard Core run on the nightly schedule and manual dispatch.

**Feature Branch**: `feature/472-spec`

**Created**: 2026-11-14

**Status**: `gate1: provisional` (no owner questions or structural checkboxes required)

**Input**: Ticket DEV-472 (parent DEV-492); grill-closed `specs/DEV-472/brief.md` (Q1–Q7 + Phase 2 step 6 decision addendum); `CONCLUSIONS.md` (including Phase 2 Step 6); `ASSUMPTIONS.md`; Maestri notes `DEV-472`, `recon-DEV-472` (lines 4–35); `PLAN_ADJUDICATION.md`; `specs/PRODUCT.md`; existing ADR `docs/adr/0016-stryker-mtp-runner.md` (cite only; no new ADR; historical-routing drift noted, no ticket). Size: M.

## Owner question — Gate 1

None from ticket contradiction or constitution departure (`brief.md` Goal and frozen scope; Q1–Q6; Phase 2 Step 6; `PRODUCT.md` §5). Ordinary owner Gate 1 on the eventual spec PR remains pending after Bernstein coordination / Rigger preparation-opening and user merge. Patron grill approval is not owner Gate 1. Quill does not invent PLAN FROZEN or a structural checkbox. External required-check registration is a user-owned runtime prerequisite tracked in evidence, not a Gate 1 structural checkbox.

## Clarifications

### Session 2026-11-14

- Q: Interactive `/speckit-clarify` after grill closure — any remaining critical ambiguity for inventable behaviour? → A: Event matrix, immutable PR base/`-BaseRef` contract, honest exit translation, nightly cron `17 3 * * *` (03:17 UTC, taste in `ASSUMPTIONS.md`), concurrency absence (recon line 35), no scheduled image publication, property-test end-state, `.github/workflows/ci.yml`-only file boundary, four-step ordering, three-layer validation/evidence, check identity `mutation-pr` / `PR mutation`, evidence ledger `specs/DEV-472/EVIDENCE.md`, external required-check registration ownership, and review-loop bar are frozen in brief Q1–Q6 / Phase 2 Step 6 / `CONCLUSIONS.md`. No `[NEEDS CLARIFICATION]` markers for inventable behaviour. Gaps that would invent routing/YAML mechanics beyond those rulings go to `DRAFTING_RECEIPT.md` as `needs decision:` for Keel — not filled here.

## Scope

**In scope** (`brief.md` Goal and frozen scope; Plan decisions; Q1–Q6; Phase 2 step 6 addendum):

1. **Single existing workflow event matrix** in `.github/workflows/ci.yml` only:
   - `pull_request` to main: required diff-scoped mutation gate via ADD job id `mutation-pr` (display/check name `PR mutation`) running `scripts/run-mutation.ps1 -BaseRef`; full 16-shard Core + evaluator excluded; no new image-publish permission; preserve existing non-nightly publish policy.
   - `push` to main: no Stryker (diff-scoped or full) once schedule replacement is present; full shards/evaluator excluded only in the same releasable workflow edit that enables nightly; preserve existing push+main image-publish behaviour (recon line 26).
   - `schedule` on main (`cron: '17 3 * * *'`, 03:17 UTC): required full 16-shard `stryker-core` plus merged-report `stryker-core-gate`; diff-scoped PR gate excluded; image publication excluded.
   - `workflow_dispatch`: required full Core shards + evaluator as diagnostic/preflight; diff-scoped PR gate excluded; preserve existing non-nightly publish behaviour.
2. **PR base and result contract**: Checkout immutable pull_request payload head with full history (`fetch-depth: 0`); explicitly fetch immutable payload base SHA; verify both commits exist; pass base SHA to `-BaseRef` via env/argument. Never use Stryker `--since`. Fail closed on fetch/checkout/verification failure, runner exit 1, or inability to run. Translate accepted exit 2 only as script-reported `SKIPPED (scope-empty)` or `N/A` (child `pwsh`, immediate exit capture, summary label, wrapper 0, full log preserved), never as mutation PASS. Hosted check success is not mutation PASS. Preserve existing mutation thresholds, timeout policy, exclusion rules, kill-share and kill-file-span safeguards.
3. **Full-run reuse**: Keep existing 16-shard orchestration (`scripts/ci-stryker-shard.sh`), report artifacts, and merged-report evaluation (`scripts/run-mutation.ps1 -EvaluateReport`). Every shard and final evaluation must complete for a green full-run receipt.
4. **Cancellation isolation**: Preserve absence of any `concurrency:` block / group / cancel-in-progress across all four events (recon line 35). Do not invent a cancellation policy. Stop for recon/ruling if pickup baseline drifts.
5. **Property-test end-state**: Retain the single `property-tests` job and its separate UnitTests and IntegrationTests calls (already satisfied at baseline; do not delete distinct coverage or manufacture a deletion). Acceptance checks the final no-duplicate outcome.
6. **File boundary**: Implementation edits only `.github/workflows/ci.yml` (including removal of stale routing comments without explanatory replacement prose). Reuse `scripts/run-mutation.ps1`, `scripts/ci-stryker-shard.sh`, and `scripts/Test-RunMutation.ps1` unchanged. Do not hand-edit generated `stryker-config.json`. Do not edit ADR 0016 or `AGENTS.md` (Patron: noted, no ticket). No new dependency, project, layer, API, schema, production/frontend code, LocalPlay, secrets, or Process.Start change. No new ADR or glossary term. No repository-settings mutation.
7. **Validation and evidence** (three layers): (1) mechanical assertions of YAML structure and four-event condition truth tables with exact command/exit/expected/observed (including identity, no concurrency, publish negatives, exit mapping, history/base/head, property calls); (2) existing `scripts/Test-RunMutation.ps1` as synthetic runner-contract regression only; (3) hosted runtime receipts in `specs/DEV-472/EVIDENCE.md` for PR mutation-regression blocking on a separate non-delivery changed-production-C# PR (PR/run/base/head identity; without shipping artificial `src/` edits in the delivery diff), main-push without Stryker, a genuine green scheduled full-Core + evaluator run on the delivered main SHA, and external required-check registration/read-back/red-block proof. Exit-2 labelling has static/synthetic assurance with residual lack of hosted-label proof recorded. Manual dispatch is preflight only. Spec/Gate 1 status is separate from runtime acceptance. Static assertions do not replace hosted proof.
8. **Review loop (size M)**: Block on every unresolved in-scope Critical/High/Medium/Low finding with explicit disposition; max two review rounds and two fix commits per round; Sentry, Ledger, and Compass evidence mandatory for adjudication. Frozen scope is the paragraph above; anything else is a follow-up issue, not a finding in this round. Historical ADR/`AGENTS.md` routing drift remains noted, no ticket — not a follow-up request.

**Out of scope**:

- Editing `scripts/run-mutation.ps1`, `scripts/ci-stryker-shard.sh`, `scripts/Test-RunMutation.ps1`, `stryker-config.json`, `harness.yml`, or production/`web` sources.
- Editing `docs/adr/0016-stryker-mtp-runner.md` or `AGENTS.md`, or requesting a follow-up ticket for their historical-routing drift.
- Inventing a concurrency policy or performing repository-settings / required-check writes from this plan.
- Lowering mutation thresholds or relaxing kill-share / kill-file-span / exclusion safeguards.
- Enabling Stryker `--since`.
- Substituting manual dispatch for the green scheduled nightly acceptance criterion.
- Claiming runtime PASS from fixture success, from a workflow-only scope-empty PR, or from hosted check success alone as mutation PASS.
- A releasable intermediate that removes push full mutation before nightly schedule replacement exists.
- New ADR, CONTEXT.md term, constitution amendment, or structural owner checkbox from this grill.
- Expanding file scope to fix a discovered runner defect without recon and a cited ruling.

**Precedence**: Ticket desired outcomes and ACs are already approved authority (`PRODUCT.md` §3; `recon-DEV-472` line 4). Brief Q1–Q6, Phase 2 Step 6, and `CONCLUSIONS.md` govern boundaries. Ordinary owner Gate 1 remains pending. Quill does not invent PLAN FROZEN.

## Domain vocabulary *(mandatory — spec-local clarification of existing ticket/brief meanings only; no CONTEXT.md or ADR expansion)*

**Diff-scoped PR mutation gate**
The pull_request CI check job id `mutation-pr` (display/check name `PR mutation`) that runs `scripts/run-mutation.ps1 -BaseRef` against the immutable PR base SHA and PR head for changed production scope.
_Avoid_: full 16-shard Core run; Stryker `--since`; claiming exit 2 as mutation PASS; inventing an already-existing PR mutation job

**Full Core mutation lane**
The existing 16-shard `stryker-core` matrix plus `stryker-core-gate` merged-report evaluation used for schedule and workflow_dispatch.
_Avoid_: PR diff-scoped gate; main-push Stryker after atomic cutover

**Honest mutation exit**
Script exit 0 = mutation PASS only when the runner actually passed; exit 1 / unrunnable / fetch-verify failure = fail closed; exit 2 = `SKIPPED (scope-empty)` or `N/A` only as the script reports — never folded into PASS; hosted check success ≠ mutation PASS.
_Avoid_: silent green on missing history, missing executable, or scope-empty

Consumed vocabulary (already in use; not redefined): Stryker, `run-mutation.ps1`, `ci-stryker-shard.sh`, kill-share, kill-file-span, `SKIPPED (scope-empty)`, mutation `N/A`, property-tests.

## What the User Will See *(mandatory)*

Nothing visible: this ticket changes CI workflow routing and mutation scheduling only. No Compose-stack UI, API, or library behaviour change for the running app.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Diff-scoped PR mutation gate (Priority: P1)

A maintainer opening a pull request to main gets a new stable mutation check named `PR mutation` (job id `mutation-pr`) that compares the PR head to the immutable PR base SHA via `scripts/run-mutation.ps1 -BaseRef`, fails closed on mutation regression or unrunnable states, and never labels accepted exit 2 as mutation PASS. Full Core lane remains excluded on PR only; push full coverage is not removed until nightly replacement exists.

**Why this priority**: Ticket desired outcome — PRs use the diff-scoped gate and mutation regression blocks merge (subject to external required-check registration evidence).

**Independent Test**: On a `pull_request` to main, observe `PR mutation`: immutable head checkout with full history; explicit base fetch and verification; `-BaseRef` to the immutable base SHA; exit 1 / unrunnable fails; accepted exit 2 labelled `SKIPPED (scope-empty)` or `N/A` only; full 16-shard Core jobs do not run on the PR; push full-lane topology still present until US2 atomic cutover.

**Acceptance Scenarios**:

1. **Given** a pull_request to main, **When** CI runs, **Then** job `mutation-pr` / check `PR mutation` runs with the immutable base SHA passed to `-BaseRef` after head checkout (`fetch-depth: 0`) and verified base/head commits.
2. **Given** that check, **When** fetch/checkout/verification fails, the runner exits 1, or the runner cannot run, **Then** the check fails (does not claim mutation PASS).
3. **Given** an accepted script exit 2, **When** the check finishes, **Then** the job summary shows `SKIPPED (scope-empty)` or `N/A` as reported, the wrapper exits 0, the full log is preserved, and the result is never mutation PASS; hosted check success remains distinct from mutation PASS.
4. **Given** a pull_request to main, **When** CI runs, **Then** the full 16-shard `stryker-core` and `stryker-core-gate` jobs do not run.

---

### User Story 2 - Main push off Stryker; nightly and manual full Core (Priority: P1)

Maintainers keep main-push CI free of Stryker cost once nightly replacement is enabled in the same releasable workflow edit; scheduled nightly (03:17 UTC) and manual dispatch still run the full 16-shard Core lane plus evaluator, without scheduled image publication, and without inventing a concurrency cancellation policy (absence preserved).

**Why this priority**: Ticket desired outcomes — main push has no Stryker; nightly full run; manual diagnostic path retained.

**Independent Test**: A push to main shows no Stryker jobs after atomic cutover; a schedule (and workflow_dispatch) run completes all 16 shards and merged-report evaluation; scheduled run does not publish images; workflow has no `concurrency:` block for any of the four events; push+main publish guard remains.

**Acceptance Scenarios**:

1. **Given** a push to main after the atomic schedule cutover, **When** CI runs, **Then** no diff-scoped Stryker gate and no full Core shards/evaluator run.
2. **Given** a schedule event on main at cron `17 3 * * *`, **When** CI runs, **Then** the existing 16-shard `stryker-core` and `stryker-core-gate` run and image publication does not.
3. **Given** workflow_dispatch, **When** CI runs, **Then** the full Core shards and evaluator run as diagnostic/preflight (not a substitute for the green scheduled acceptance criterion).
4. **Given** the delivered workflow, **When** inspected for all four events, **Then** there is no `concurrency:` block / group / cancel-in-progress (recon line 35 preserved), and the existing push+main publish guard still excludes PR/schedule/dispatch.

---

### User Story 3 - Property-test end-state and acceptance evidence (Priority: P2)

The single property-tests job with separate UnitTests and IntegrationTests calls remains; validation covers mechanical event-matrix assertions and the synthetic runner fixture; hosted receipts in `specs/DEV-472/EVIDENCE.md` prove PR regression blocking (non-delivery PR), main-push without Stryker, green scheduled full-Core, and required-check registration — with pending items and residual exit-2 hosted-label risk kept explicit until evidenced.

**Why this priority**: Ticket done criteria and Q1/Q5 evidence contract; preserves distinct property coverage without manufacturing a deletion.

**Independent Test**: Inspect final workflow for one property-tests job and two project calls; run `scripts/Test-RunMutation.ps1`; maintain `specs/DEV-472/EVIDENCE.md` with run URLs, immutable SHAs, event types, required-check registration evidence, and outcomes (PASS or explicitly pending).

**Acceptance Scenarios**:

1. **Given** the delivered workflow, **When** inspected, **Then** there is exactly one property-tests job invoking UnitTests and IntegrationTests once each (no duplicate property-test invocation).
2. **Given** local validation, **When** mechanical assertions and `scripts/Test-RunMutation.ps1` run, **Then** assertion records include command/exit/expected/observed for the four-event matrix and related contracts, and the fixture passes as synthetic runner-contract regression only (not nightly proof; not a substitute for hosted acceptance).
3. **Given** hosted evidence gathering, **When** receipts are recorded in `specs/DEV-472/EVIDENCE.md`, **Then** the ledger includes (a) a separate non-delivery changed-production-C# PR where mutation regression fails `PR mutation` with PR/run/base/head identity and without artificial `src/` edits in the DEV-472 delivery diff, (b) main-push without Stryker, (c) a green scheduled full-Core + evaluator run on the delivered main SHA with run URL, and (d) external required-check registration/read-back/red-block evidence for `PR mutation` — or each missing item is explicitly pending.
4. **Given** only a workflow-only scope-empty PR success, **When** judging mutation-regression blocking, **Then** that alone is insufficient proof.
5. **Given** exit-2 labelling assurance, **When** hosted-label proof is absent, **Then** residual risk is recorded as static/synthetic-only under the unchanged Q5 hosted receipt contract.

---

### Edge Cases

- Missing immutable base SHA, missing PR head, insufficient Git history, or failed commit verification: fail closed — never mutation PASS.
- Runner executable missing or otherwise unrunnable; unexpected child exit: fail closed. No blanket continue-on-error.
- Scope-empty changed set (script exit 2): visible `SKIPPED (scope-empty)` or `N/A` only; hosted check success is not mutation PASS.
- Scheduler/runner delay past 03:17 UTC: allowed; exact start is operational taste (`ASSUMPTIONS.md`), not a deadline guarantee.
- Manual dispatch green while scheduled receipt pending: preflight only; ticket nightly criterion remains open.
- Required checks / rulesets absent at intake (recon line 34): registration remains external user-owned pending evidence; no repo-settings write from this plan; no structural checkbox.
- Pickup baseline gains a concurrency block: STOP for recon/ruling — do not invent isolation policy.
- Discovered defect in reused runner scripts: STOP for recon and a cited ruling before expanding file scope.
- Harness gates on workflow-only diff: Roslyn/complexity/web/mutation production lanes report `SKIPPED (scope-empty)` or N/A as applicable — never folded into PASS.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `.github/workflows/ci.yml` MUST implement the Q2 event matrix (PR diff-scoped gate; push without Stryker after atomic schedule cutover; schedule and workflow_dispatch full 16-shard + evaluator; schedule excluded from image publication).
- **FR-002**: The workflow MUST add schedule trigger cron `17 3 * * *` (03:17 UTC) on main in the same releasable edit that excludes push from the full lane, in addition to existing push/pull_request/workflow_dispatch triggers as needed to realise the matrix.
- **FR-003**: On pull_request to main, CI MUST ADD job id `mutation-pr` (display/check name `PR mutation`) that checks out the immutable payload head with `fetch-depth: 0`, explicitly fetches and verifies the immutable payload base SHA, supplies event SHAs via env, and passes the base SHA to `scripts/run-mutation.ps1 -BaseRef`; Stryker `--since` MUST NOT be used; job permissions MUST be `contents: read`; action versions MUST be those verified at pickup recon.
- **FR-004**: The PR mutation check MUST invoke the runner in a child `pwsh` process with output preserved and immediate exit capture; MUST fail on exit 1, launch failure, missing executable, unexpected status, or inability to prepare history; MUST translate accepted exit 2 only as script-reported `SKIPPED (scope-empty)` or `N/A` written to the job summary with wrapper exit 0 and full log preserved, without claiming mutation PASS; MUST NOT use blanket continue-on-error; hosted check success MUST remain distinct from mutation PASS.
- **FR-005**: On pull_request, full `stryker-core` shards and `stryker-core-gate` MUST NOT run; on push to main, they MUST NOT run only after the atomic schedule enablement that replaces push full coverage — no releasable intermediate that drops push full mutation before nightly exists.
- **FR-006**: On schedule and workflow_dispatch, full `stryker-core` (16 shards) and `stryker-core-gate` MUST run using existing shard/merge/evaluate scripts and artifact behaviour; every shard and final evaluation MUST complete for a green full-run receipt.
- **FR-007**: The workflow MUST preserve absence of any `concurrency:` block / group / cancel-in-progress across all four events (recon line 35); MUST NOT invent a cancellation policy; pickup concurrency drift MUST stop for recon/ruling.
- **FR-008**: Scheduled events MUST NOT publish container images; existing push+main publish behaviour MUST be preserved (verify-only unless authorized drift); PR/schedule/dispatch MUST remain excluded from publish.
- **FR-009**: Existing mutation policy and safeguards (threshold, timeouts, exclusions, kill-share, kill-file-span) MUST remain unchanged; `stryker-config.json` MUST NOT be hand-edited.
- **FR-010**: The single `property-tests` job and its two distinct project invocations MUST remain; no duplicate property-test invocation; no manufactured deletion of distinct coverage.
- **FR-011**: Implementation MUST edit only `.github/workflows/ci.yml` (including removal of stale routing comments without explanatory replacement prose); reused scripts, ADR 0016, and `AGENTS.md` MUST stay unchanged unless a new cited ruling expands scope after recon; ADR/`AGENTS.md` historical-routing drift remains noted, no ticket.
- **FR-012**: Validation MUST include mechanical event-matrix/guard assertions for all four events (command/exit/expected/observed), `scripts/Test-RunMutation.ps1` synthetic regression, and hosted evidence ledger `specs/DEV-472/EVIDENCE.md` with explicit pending status until PR regression-blocking (non-delivery PR with identities), main-push no-Stryker, green scheduled full-Core, and external required-check registration/read-back/red-block receipts exist; residual exit-2 hosted-label risk MUST be recorded as static/synthetic-only under unchanged Q5.
- **FR-013**: Spec/Gate 1 progress MUST remain separable from runtime acceptance; fixture green, scope-empty PR green, and hosted check success MUST NOT be claimed as nightly proof, mutation PASS, or ticket-done without required-check evidence.
- **FR-014**: Size-M review closure MUST block on every unresolved in-scope Critical/High/Medium/Low finding with explicit disposition; max two rounds and two fix commits per round; Sentry/Ledger/Compass evidence required for adjudication.
- **FR-015**: External user-owned required-check registration for `PR mutation` / `mutation-pr`, coordinated by Bernstein/Rigger with live read-back and red-block merge proof, MUST be tracked as an evidence dependency; this plan MUST NOT authorize repository-settings writes; no structural checkbox is added.

### Key Entities

- **CI workflow (`ci.yml`)**: single workflow carrying the four-event mutation matrix and preserved property-tests / publish guards; no concurrency block.
- **Diff-scoped PR mutation check**: ADD job `mutation-pr` / check `PR mutation` invoking `run-mutation.ps1 -BaseRef`.
- **Full Core lane**: `stryker-core` (16 shards) + `stryker-core-gate` (merge + `-EvaluateReport`).
- **Evidence ledger (`specs/DEV-472/EVIDENCE.md`)**: run URL, event, immutable head/base SHA, outcome (or explicitly pending), residual risks, and required-check registration evidence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A pull_request to main shows check `PR mutation` (job `mutation-pr`) using immutable `-BaseRef` after verified history preparation, and does not run the full 16-shard Core lane.
- **SC-002**: A push to main runs no Stryker jobs (diff-scoped or full) after the atomic schedule cutover.
- **SC-003**: A scheduled run on the delivered main revision completes all 16 shards and passes merged-report evaluation; the receipt records SHA and run URL; the run does not publish images.
- **SC-004**: workflow_dispatch can still run the full Core lane as preflight without substituting for SC-003.
- **SC-005**: Final workflow retains one property-tests job with UnitTests and IntegrationTests calls and no duplicate property-test invocation; preserves absence of concurrency; preserves push+main publish guard.
- **SC-006**: `scripts/Test-RunMutation.ps1` passes locally as synthetic contract regression; hosted PR regression-blocking evidence from a separate non-delivery changed-C# PR is recorded in `EVIDENCE.md` without artificial `src/` edits in the delivery diff, or remains explicitly pending; residual exit-2 hosted-label risk is recorded.
- **SC-007**: Harness gate reporting on the workflow-only diff keeps `SKIPPED (scope-empty)`, mutation `N/A`, configured `SKIP`, exit 1, and `Could not run` distinct from PASS.
- **SC-008**: External `PR mutation` required-check registration/read-back/red-block evidence is recorded in `EVIDENCE.md` or remains explicitly pending; ticket done is not claimed without it.

## Assumptions

- Nightly wall-clock cron `17 3 * * *` (03:17 UTC) is Patron-approved operational taste logged in `ASSUMPTIONS.md`; scheduler/runner availability may delay actual start (`brief.md` Q4).
- Job id `mutation-pr`, display/check name `PR mutation`, and ledger path `specs/DEV-472/EVIDENCE.md` are Patron-approved naming/location taste logged in `ASSUMPTIONS.md` (Phase 2 Step 6).
- Reused scripts and generated Stryker config remain authoritative for mutation policy; this ticket only re-routes when they run (`brief.md` Q2/Q6; ADR 0016 cite-only).
- Stale ticket line references for a property-test duplicate are already reconciled as an already-satisfied end-state; Rigger recording receipt may still be pending and is not a scope blocker (`brief.md` Q1/Q7).
- No new PRODUCT.md dependency or architecture choice is required; reliability bar remains Stryker >= 80% with honest gate exits (`specs/PRODUCT.md` §1).
- Historical-routing prose in ADR 0016 / `AGENTS.md` is noted, no ticket (`CONCLUSIONS.md` Phase 2 Step 6) — not a follow-up request.
