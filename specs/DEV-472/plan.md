# Implementation Plan: Move full Core Stryker off main push (PR diff gate + nightly full run)

**Branch**: `feature/472-spec` | **Date**: 2026-11-14 | **Spec**: `specs/DEV-472/spec.md`

**Input**: Grill-closed `brief.md` (Q1–Q7 + Phase 2 step 6 decision addendum); `CONCLUSIONS.md` (including Phase 2 Step 6 at `6f87b755`); `ASSUMPTIONS.md`; Maestri notes `DEV-472`, `recon-DEV-472` (lines 4–35); `PLAN_ADJUDICATION.md`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only); ADR `docs/adr/0016-stryker-mtp-runner.md` (cite only; no new ADR; historical-routing drift noted, no ticket).

**Status**: Round 1 repairs applied from `PLAN_ADJUDICATION.md` Consolidated Quill repair list items 1–9. Awaiting Keel independent read-back for PLAN FROZEN. Gate 1, implementation, runtime acceptance, and merge clearance remain pending.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md`. Ledger home for Phase B: `specs/DEV-472/EVIDENCE.md`. No `research.md`, `data-model.md`, or `contracts/` — decisions frozen in brief; workflow-only; no public API/schema delta.

## Summary

Re-route mutation work inside the existing `.github/workflows/ci.yml`: ADD PR-only job id `mutation-pr` (display/check name `PR mutation`) running `scripts/run-mutation.ps1 -BaseRef` against the immutable PR base SHA with fail-closed history preparation and explicit exit translation; main push runs no Stryker once schedule replacement is atomic; nightly cron `17 3 * * *` and `workflow_dispatch` keep the existing 16-shard `stryker-core` + `stryker-core-gate` evaluator lane. Preserve absence of any concurrency block (recon line 35); preserve existing push+main publish guard (recon line 26); preserve the single property-tests job and mutation policy. Do not edit reused scripts, generated `stryker-config.json`, ADR 0016, or `AGENTS.md`. External required-check registration for `PR mutation` is user-owned (Bernstein/Rigger coordinated) and remains pending evidence — this plan does not authorize repository-settings writes.

## Technical Context

**Language/Version**: GitHub Actions YAML; PowerShell 7 gate scripts already in repo; .NET 10 via existing `actions/setup-dotnet` + `global.json` (unchanged).

**Primary Dependencies**: NONE ADDED. Reuse `scripts/run-mutation.ps1`, `scripts/ci-stryker-shard.sh`, `scripts/Test-RunMutation.ps1`, existing Actions versions already pinned in `ci.yml` (confirmed at T001). No NuGet/npm change (`PRODUCT.md` §5 / charter §2.3).

**Storage**: N/A (CI artifacts only; existing shard/merged-report upload retention preserved). Evidence ledger path: `specs/DEV-472/EVIDENCE.md`.

**Testing**: Mechanical assertions against YAML structure and four-event condition truth tables (exact command/exit/expected/observed); `scripts/Test-RunMutation.ps1` synthetic runner/classification/evaluation fixture (recon lines 12–13; not nightly proof); hosted runtime receipts per Q5 including residual exit-2 label risk recorded as static/synthetic-only. No new test project, dependency, or script edit. No artificial `src/` edits in the delivery diff for proof; regression evidence uses a separate non-delivery changed-production-C# PR.

**Target Platform**: GitHub-hosted `ubuntu-latest` CI for LamuFlix workflows.

**Project Type**: CI/workflow routing (repo infra; `ui: none`).

**Performance Goals**: Remove full-Core mutation cost from main-push event runs; retain full coverage on nightly schedule and manual dispatch (`brief.md` Goal). No releasable intermediate that drops push full mutation before nightly exists.

**Constraints**: `.github/workflows/ci.yml` only. Never enable Stryker `--since`. Never lower thresholds or relax kill-share / kill-file-span / exclusions. Fail closed on exit 1 / unrunnable / fetch-verify failure; never label exit 2 as mutation PASS; hosted check success ≠ mutation PASS. Preserve absence of concurrency across all four events; do not invent group/cancel-in-progress. Preserve push+main publish guard (verify-only). Stop for recon/ruling if a runner defect or concurrency/required-check baseline drifts. No repo-settings mutation. ADR 0016 / `AGENTS.md` historical-routing drift: noted, no ticket. Harness exits stay distinct: `SKIPPED (scope-empty)`, mutation `N/A`, configured `SKIP`, exit 1, `Could not run` ≠ PASS.

**Scale/Scope**: Size M. Single implementation file. Planning artifacts under `specs/DEV-472/`. Parent DEV-492.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` (worktree current). Workflow-only scope; no owner structural checkbox from Q1–Q6 or Phase 2 Step 6 (ticket already authorizes merge-blocking behaviour; registration is external).

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | Named ports; Core isolation | **N/A** — no Core/Infrastructure/Api production edits; no new port. |
| II (Explicit Handlers) | Handlers + AddHandler | **N/A** — no commands/queries. |
| III (Typed query model) | MovieQuery | **N/A** — no browse filter change. |
| IV (Domain transitions) | Movie mutators | **PASS** — no Movie mutation. |
| V (Validated inputs / errors) | ProblemDetails | **PASS** — no public API/error-contract change. |
| VI (Observability) | Safe diagnostics | **PASS** — CI receipts use run URLs/SHAs in `EVIDENCE.md`; no secrets in ledger. |
| VII (Configuration / LocalPlay / Process.Start) | Options; LocalPlay gated | **PASS** — no LocalPlay, Process.Start, secrets, or options-record changes; publish continues to use existing `GITHUB_TOKEN` pattern only on non-schedule events already authorized (push+main guard preserved). |
| VIII (Ubiquitous Language) | CONTEXT.md | **PASS** — no new domain term; no CONTEXT.md/ADR edit; ADR 0016 cite-only; historical ADR/`AGENTS.md` routing drift noted, no ticket (`CONCLUSIONS.md` Phase 2 Step 6). |
| IX (Test Pyramid) | Meaningful layers | **PASS** — mechanical assertions + synthetic fixture + hosted runtime evidence for behaviours fixtures cannot prove; no fabricated production scope to green a skipped lane; residual exit-2 hosted-label risk recorded. |
| Tech stack / API docs | OpenAPI chain | **PASS** — no contract generation; no DTO/route change. |
| Governance / Static-Analysis Gates | Honest exits | Phase B: applicable gates on actual diff; workflow-only ⇒ C#/web/mutation production lanes `SKIPPED (scope-empty)` / N/A as scripts report; never PASS. Closing bar and 2×2 caps per brief Q6. Required-check registration evidence tracked separately; never claim merge blocking until evidenced. |

### Complexity Tracking

No constitution violations requiring justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |

## Approach

- **Phase A (this draft + round-1 repairs)**: Quill writes/repairs spec/plan/tasks/checklist/receipt from brief/CONCLUSIONS/ASSUMPTIONS/recon facts (through line 35), PRODUCT, and `PLAN_ADJUDICATION.md`. Keel independently reads back repairs before any PLAN FROZEN. No Build work before Gate 1.
- **Phase B pickup (Build workspace, after Gate 1)**: Fresh recon of `ci.yml` against landed main (including concurrency absence and required-check visibility); confirm script contracts still match; implement workflow-only edits in brief task order with atomic T004 schedule+push cutover; gather hosted evidence in `EVIDENCE.md` with honest pending rows including external required-check registration.
- **Dependency-ordered implementation (`brief.md` Task ordering + Phase 2 step 6 addendum)**:
  1. ADD `mutation-pr` / `PR mutation`, immutable base/head fetch+verify, child-`pwsh` exit translation; exclude full lane from PR only (push full lane retained).
  2. Atomically add nightly schedule and exclude push from full lane; verify no-concurrency and push+main publish guard; retain single property-tests job and both project calls; remove stale `ci.yml` routing comments without replacement prose.
  3. Record mechanical assertions (command/exit/expected/observed) for structure, four-event conditions, history/base/head, exit mapping, identity, no concurrency, publish negatives, property calls; run existing fixture and applicable harness gates; no new dependency/script.
  4. Maintain `specs/DEV-472/EVIDENCE.md`; obtain non-delivery changed-C# PR regression evidence with PR/run/base/head identity; main-push no-Stryker; green scheduled full-Core; required-check registration/read-back/red-block proof — keep missing receipts explicitly pending.
- **Reuse, do not rewrite**: `scripts/run-mutation.ps1`, `scripts/ci-stryker-shard.sh`, `scripts/Test-RunMutation.ps1`. Do not hand-edit `stryker-config.json`. Do not edit ADR 0016 or `AGENTS.md`.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-472/
├── brief.md
├── CONCLUSIONS.md
├── ASSUMPTIONS.md
├── PLAN_ADJUDICATION.md
├── spec.md
├── plan.md              # This file
├── tasks.md
├── DRAFTING_RECEIPT.md
├── EVIDENCE.md          # Phase B ledger home (T008)
└── checklists/requirements.md
```

No `research.md` / `data-model.md` / `contracts/` — decisions frozen in brief; no API/schema surface.

### Source Code (approved boundary — Q6 / recon lines 8–14, 17–18, 26, 34–35)

```text
.github/workflows/ci.yml          # ONLY implementation edit

Reuse unchanged (do not edit without new cited ruling):
scripts/run-mutation.ps1
scripts/ci-stryker-shard.sh
scripts/Test-RunMutation.ps1
stryker-config.json               # generated — do not hand-edit

Out of implementation diff (cite / noted only):
docs/adr/0016-stryker-mtp-runner.md   # historical-routing drift: noted, no ticket
AGENTS.md                             # historical-routing drift: noted, no ticket
```

**Structure Decision**: Stay inside the existing single CI workflow file. No new workflow file, project, top-level folder, or architectural layer. Full-run sharding/evaluation design remains ADR 0016 as already shipped; this ticket only changes when that lane runs. No repository-settings mutation authorized.

## Design

### Event matrix (authoritative — brief Plan decisions)

| Event | Diff-scoped PR mutation | Full 16-shard Core + evaluator | Image publication |
| --- | --- | --- | --- |
| pull_request to main | Required job `mutation-pr` / check `PR mutation` | Excluded | No new permission; preserve existing event policy |
| push to main | No Stryker | Excluded (only after atomic T004 with schedule) | Preserve existing push+main guard (recon line 26) |
| schedule on main | Excluded | Required | Excluded |
| workflow_dispatch | Excluded | Required, diagnostic/preflight | Preserve existing non-nightly behavior |

### PR diff-scoped check (ADD — not adjust an imaginary existing PR gate)

- ADD job id `mutation-pr`, display/check name `PR mutation`, PR-only (`ASSUMPTIONS.md` taste; `CONCLUSIONS.md` Phase 2 Step 6 (1)).
- Permissions: `contents: read`. Reuse action versions verified by T001.
- Checkout immutable pull_request payload head SHA with `fetch-depth: 0`; explicitly fetch immutable payload base SHA; verify both commit objects exist; supply SHAs via env; invoke `pwsh` child process running `./scripts/run-mutation.ps1 -BaseRef <immutable-base-sha>` (never `--since`). Any fetch/checkout/verification failure fails closed.
- Exit translation (immediate `LASTEXITCODE` after child `pwsh`, full output preserved): 0 ⇒ retain actual runner mutation PASS; accepted 2 ⇒ write runner-reported `SKIPPED (scope-empty)` or `N/A` to `GITHUB_STEP_SUMMARY`, wrapper exits 0, full log preserved; 1 / launch failure / missing executable / unexpected status ⇒ fail. No blanket `continue-on-error`. Accepted exit-2 classification must be verified, never guessed. Hosted check success ≠ mutation PASS.
- Exclude full `stryker-core` / `stryker-core-gate` from pull_request in T003; push exclusion waits for atomic T004 with schedule.

### Full Core lane guards

- T004 atomically adds `on.schedule` cron `17 3 * * *` (03:17 UTC) and changes full-lane `if:` guards from today's "not pull_request" (recon lines 8–9) so push is also excluded; enable on `schedule` and `workflow_dispatch` only. Exact start may slip with runner availability (`ASSUMPTIONS.md`).
- Reuse existing 16-shard matrix, `ci-stryker-shard.sh`, artifact upload/download, merge, and `run-mutation.ps1 -EvaluateReport` steps already present (recon lines 8–10, 14).
- Publish: preserve existing push + `refs/heads/main` guard (recon line 26); T005 verification-only; T006 negative asserts for PR/schedule/dispatch. Do not rewrite unless authorized drift demands a ruling.
- Within T002–T004 edited spans: remove stale routing comments (recon line 8) without explanatory replacement prose.

### Concurrency (preserve absence)

- Recon line 35: there is no concurrency block. Preserve absence of any `concurrency:` / group / `cancel-in-progress` for all four events. Do not invent a cancellation policy. T005 verifies absence; T001 stops for recon/ruling if pickup baseline drifts. This satisfies Q4/FR-007 isolation without invented changes (`CONCLUSIONS.md` Phase 2 Step 6 (2)).

### Property-tests

- Leave the single `property-tests` job and both UnitTests / IntegrationTests invocations in place (recon lines 11, 22; Q1). Do not invent a deletion.

### Required-check registration (external)

- Recon line 34: no required checks and zero repository rulesets visible to CLI at intake.
- User owns external required-check registration for `PR mutation` / `mutation-pr`, coordinated by Bernstein/Rigger, with live API read-back and proof that a red mutation regression blocks normal merge.
- This plan does not authorize repository-settings writes. Ticket merge-blocking criterion already exists; no structural checkbox. T009/T013 track evidence in `EVIDENCE.md`; do not claim ticket done until evidenced (`CONCLUSIONS.md` Phase 2 Step 6 (1)).

### Documentation boundary

- Stale routing comments in `.github/workflows/ci.yml` only. ADR 0016 and `AGENTS.md` remain outside the implementation diff; Patron disposition: **noted, no ticket** — not a follow-up request (`CONCLUSIONS.md` Phase 2 Step 6 (3)).

## Test Strategy

| Family | Required evidence |
|---|---|
| Event-matrix mechanical assertions | Exact command/exit/expected/observed for YAML structure and all four event conditions; negative asserts for excluded mutation paths and scheduled publication |
| Base/exit contract | Immutable head checkout `fetch-depth: 0`, explicit base fetch+verify, env SHAs, `-BaseRef`, child-`pwsh` exit mapping, stable `PR mutation` result; hosted success ≠ mutation PASS |
| Isolation / publish | Absence of concurrency asserted; publish push+main guard preserved; PR/schedule/dispatch publish negatives |
| Property-tests end-state | One job; UnitTests + IntegrationTests once each; no duplicate |
| Identity / registration | Frozen `mutation-pr` / `PR mutation`; external required-check registration evidence or explicitly pending |
| Synthetic runner fixture | `scripts/Test-RunMutation.ps1` green — not nightly proof; contributes to exit-2 label assurance |
| Residual exit-2 label risk | Static + synthetic assurance recorded; hosted-label proof residual under unchanged Q5 |
| Hosted PR regression | Separate non-delivery changed-production-C# PR with PR/run/base/head SHAs; no artificial `src/` in delivery diff; scope-empty PR alone insufficient |
| Hosted main push | No Stryker execution on push to main |
| Hosted schedule | Green full Core across 16 shards + evaluator on delivered main SHA; run URL + SHA recorded; dispatch ≠ substitute |
| Harness gates | Applicable scripts only; scope-empty ≠ PASS |
| Ledger | `specs/DEV-472/EVIDENCE.md` with pending rows until evidenced |

## Phase 0 / Phase 1 note

Per Spec Kit plan workflow: unknowns that would normally populate `research.md` are already ruled in brief Q1–Q6 and Phase 2 Step 6. No data-model or contracts artifacts for this workflow-only ticket. Open drafting gaps (if any) are listed only in `DRAFTING_RECEIPT.md` as `needs decision:` for Keel — not silently filled.

## Post-design Constitution Check

Re-checked after round-1 repairs: still PASS/N/A as in the Constitution Check table; no new port, handler, schema, API, LocalPlay, secrets, Process.Start, CONTEXT, ADR edit, structural checkbox, or repository-settings write introduced by this plan. PLAN FROZEN remains for Keel after independent read-back.
