---

description: "Task list for DEV-472 CI mutation event routing"
---

# Tasks: Move full Core Stryker off main push (PR diff gate + nightly full run)

**Input**: Design documents from `specs/DEV-472/` — `spec.md`, `plan.md`, grill-closed `brief.md` (Q1–Q7 + Phase 2 step 6 decision addendum), `CONCLUSIONS.md` (including Phase 2 Step 6 at `6f87b755`), `ASSUMPTIONS.md`; Maestri notes `DEV-472`, `recon-DEV-472` (lines 4–35); `PLAN_ADJUDICATION.md` round-1 repair list; `specs/PRODUCT.md`.

**Status**: Round 1 repairs applied from `PLAN_ADJUDICATION.md` Consolidated Quill repair list items 1–9. Awaiting Keel independent read-back for PLAN FROZEN. Gate 1, implementation, runtime acceptance, and merge clearance remain pending.

**Prerequisites**: `plan.md`, `spec.md` (required). No `research.md` / `data-model.md` / `contracts/` for this workflow-only ticket.

**Tests**: Required per brief Test strategy / Q5. Event-matrix mechanical assertions; `scripts/Test-RunMutation.ps1` synthetic regression; hosted evidence ledger at `specs/DEV-472/EVIDENCE.md`. No new test project, dependency, or script edit. Do not ship artificial `src/` edits in the delivery diff for proof.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites are complete (different files, no incomplete deps). Same-file `ci.yml` edits are never `[P]` with each other. Shared gate/receipt tasks are never `[P]`.

**Conventions**: Implementation edits only `.github/workflows/ci.yml`. Reuse `scripts/run-mutation.ps1`, `scripts/ci-stryker-shard.sh`, `scripts/Test-RunMutation.ps1` unchanged. Never hand-edit `stryker-config.json`. Never enable Stryker `--since`. Never fold `SKIPPED (scope-empty)`, mutation `N/A`, configured `SKIP`, or `Could not run` into PASS. Hosted check success is distinct from mutation PASS. Preserve absence of any `concurrency:` block (recon line 35). Do not invent a concurrency group or cancel-in-progress policy. Do not edit `docs/adr/0016-stryker-mtp-runner.md` or `AGENTS.md` (Patron: noted, no ticket — `CONCLUSIONS.md` Phase 2 Step 6). STOP for recon/ruling on runner defects or concurrency/required-check baseline drift before expanding file set or inventing settings writes.

**Pipeline note**: Numbered `## Phase N:` headings below are **LOCAL `tasks.md` phase IDs** for `/implement` dispatch. They are **not** canonical `task-pipeline` phases.

**Spec-PR provenance**: Bernstein coordinates; Rigger prepares/opens the spec PR; only the user merges; Keel records the verified Gate 1 merge receipt. No Build work (T001+) before Gate 1. No structural owner checkbox unless Patron rules a ticket or constitution departure.

---

## Local phase dependency / execution order

```text
Phase 0   Spec-PR prep → T000   (Bernstein/Rigger produce/open; user merges; Keel Gate 1 merge record) — blocks T001+
Phase 1   T001          (Wisp pickup recon + Keel reanalysis + Gauge baseline) — blocks T002+
Phase 2   T002 → T003   (ci.yml; US1 ADD mutation-pr + PR-only full-lane exclusion; push full lane still present)
Phase 3   T004 → T005   (ci.yml; US2 atomic schedule + push exclusion; verify no-concurrency + publish guard + property-tests)
Phase 4   T006 → T007   (US3 mechanical matrix assertions + Test-RunMutation.ps1 + applicable harness gates)
Phase 5   T008 → T011   (US3 EVIDENCE.md ledger + hosted receipts incl. required-check registration; pending until evidenced)
Phase 6   T012          (capped review / remediation per Q6 bar)
Phase 7   T013          (delivery / merge-bar handoff — user merges; runtime/required-check receipts may remain pending)
```

---

## Phase 0: Spec PR and Gate 1 merge record (blocks Phase 1)

**Purpose**: Produce/open the spec PR, then confirm ordinary owner Gate 1 merge before Build work.
**Checkpoint**: Spec PR opened by Rigger under Bernstein coordination; verified user-merge receipt recorded, or stop. No Build tasks before that receipt. Gate 1 / runtime / merge remain separate pending evidence.

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/opens the DEV-472 spec PR for ordinary owner Gate 1; only the user merges. After merge, **Keel** records the verified Gate 1 spec-PR merge receipt for DEV-472 in `specs/DEV-472/CONCLUSIONS.md`. Receipt only. T001–T013 MUST NOT proceed without verified merge (`brief.md` Handoff; no structural owner checkbox on this ticket).

---

## Phase 1: Pickup recon (blocks implementation)

**Purpose**: Confirm Gate-1-merged artifacts still match landed main; inventory `ci.yml` mutation/property/publish seams against recon lines 8–14, 21–22, 26, 34–35; confirm reused scripts and generated config remain non-edit targets; confirm still no concurrency block and still no required checks/rulesets visible unless drift is recorded.
**Checkpoint**: Fresh recon recorded; cross-artifact reanalysis clean or tasks amended only for authorized demonstrated drift; unresolved drift/decision stops — no invention. Baseline captured.

- [ ] T001 After T000: **Wisp** obtains fresh Phase B pickup recon against picked HEAD / landed main inventorying `.github/workflows/ci.yml` triggers, `stryker-core` / `stryker-core-gate` `if:` guards, property-tests job (lines ~295–315 at intake), publish-images guards (push+main, recon line 26), absence of any `concurrency:` block (recon line 35), required-check / ruleset visibility (recon line 34), and checkout/setup action versions already present for reuse; confirm `scripts/run-mutation.ps1` `-BaseRef` / exit contract, `scripts/ci-stryker-shard.sh`, `scripts/Test-RunMutation.ps1`, and generated `stryker-config.json` remain reuse-only. **Keel** reanalyzes brief/spec/plan/tasks against landed main and amends only for authorized demonstrated drift. **Gauge** records pre-edit applicable-gate baseline (expect workflow-only scope-empty / N/A honesty). **STOP** `needs recon` / `needs decision` on concurrency baseline drift, required-check configuration drift, runner defects, or any urge to invent a concurrency group / repo-settings write / script edit rather than inventing (`brief.md` Q6; Phase 2 step 6 addendum). No production, script, ADR, or `AGENTS.md` edits in this task.

---

## Phase 2: User Story 1 - Diff-scoped PR mutation gate (Priority: P1) 🎯 MVP

**Goal**: ADD PR-only job `mutation-pr` / check name `PR mutation` with immutable base/head preparation, honest exit translation, and full-lane exclusion on PR only. Do **not** remove push full-lane coverage in this phase.
**Independent Test**: A pull_request to main runs `PR mutation` with explicit `-BaseRef` and does not start `stryker-core` / `stryker-core-gate`; push full-lane topology remains until T004.
**Order**: T002 → T003. Same-file sequential edits in `.github/workflows/ci.yml`.
**MVP releasability**: No US1-only release that loses automatic push full mutation before the nightly replacement exists in T004.

### Implementation for User Story 1

- [X] T002 [US1] After T001: In `.github/workflows/ci.yml`, **ADD** (do not invent an existing PR mutation job) job id `mutation-pr` with display/check name `PR mutation`, PR-only (`pull_request` to main). Reuse existing action versions verified by T001; grant job permissions `contents: read` only. Check out the immutable pull_request payload head SHA with `fetch-depth: 0`; explicitly fetch the immutable pull_request payload base SHA; verify both commit objects exist; supply event SHAs via env; then invoke `scripts/run-mutation.ps1 -BaseRef` with the verified base SHA (never Stryker `--since`). Any fetch/checkout/verification failure fails closed. Bind exit translation via a child `pwsh` process that preserves full output and immediately captures `LASTEXITCODE`: exit 0 preserves actual runner mutation PASS; accepted exit 2 writes the runner-reported `SKIPPED (scope-empty)` or `N/A` label to `GITHUB_STEP_SUMMARY` and the wrapper returns 0 while preserving the full log; exit 1, launch failure, missing executable, or any unexpected status fails the job. No blanket `continue-on-error`. Do not invent unvalidated unknown exit-2 classification. Hosted check success is not mutation PASS (FR-003, FR-004; `brief.md` Q3; Phase 2 step 6 addendum). Within the edited span, remove stale routing comments identified by recon line 8 without introducing explanatory replacement prose. Do not edit scripts, `stryker-config.json`, ADR 0016, or `AGENTS.md`.
- [X] T003 [US1] After T002: In `.github/workflows/ci.yml`, exclude full `stryker-core` and `stryker-core-gate` from **pull_request only** while preserving existing shard/merge/evaluate step bodies for events that still run them. Do **not** exclude push from the full lane in this task (push exclusion is atomic with schedule enablement in T004) (FR-005 partial — PR side; recon lines 8–10; `PLAN_ADJUDICATION` item 4 / Sentry H1). Within the edited span, remove stale routing comments without new explanatory prose. Still `ci.yml` only.

**Checkpoint**: US1 independently verifiable on a PR event (topology + local YAML inspection); push full mutation still present; hosted regression-blocking proof remains Phase 5. Not a releasable US1-only cut that drops push coverage.

---

## Phase 3: User Story 2 - Main push off Stryker; nightly/manual full Core (Priority: P1)

**Goal**: Atomically add schedule and exclude push from the full lane while enabling schedule/dispatch full Core; verify preserved no-concurrency and push+main publish guard; retain property-tests.
**Independent Test**: Matrix negatives/positives visible in workflow conditions; no `concurrency:` block; publish remains push+main only; property-tests still one job with two project calls.
**Order**: T004 → T005.

### Implementation for User Story 2

- [ ] T004 [US2] After T003: In `.github/workflows/ci.yml`, in one atomic workflow edit: add `on.schedule` cron `17 3 * * *` (03:17 UTC) **and** set full-lane guards so `stryker-core` and `stryker-core-gate` run on `schedule` and `workflow_dispatch` only (not push, not pull_request). Reuse existing 16-shard matrix, `scripts/ci-stryker-shard.sh`, artifacts, merge, and `run-mutation.ps1 -EvaluateReport` without rewriting those scripts (FR-001, FR-002, FR-005 complete, FR-006; `brief.md` Q2/Q4; Phase 2 step 6 addendum). Within the edited span, remove stale routing comments (recon line 8 / `ci.yml` historical routing prose) without introducing explanatory replacement prose. Do not edit ADR 0016 or `AGENTS.md` (Patron: noted, no ticket — `CONCLUSIONS.md` Phase 2 Step 6).
- [ ] T005 [US2] After T004: In `.github/workflows/ci.yml`, **verify** (do not newly configure a concurrency policy): (a) preserve absence of any `concurrency:` / group / `cancel-in-progress` across all four events (recon line 35; FR-007); if pickup baseline now has a concurrency block, STOP for recon/ruling — do not invent isolation; (b) **verify-only** that the existing push + `refs/heads/main` publish-images guard remains and continues to exclude PR/schedule/dispatch (recon line 26; FR-008) — do not rewrite the guard unless authorized baseline drift demands a ruling; (c) retain the single `property-tests` job and both UnitTests / IntegrationTests invocations without manufacturing a deletion (FR-010; `brief.md` Q1). Still `ci.yml` only.

**Checkpoint**: US1 + US2 topology complete in `ci.yml`. No intermediate releasable state that removed push full mutation before nightly existed.

---

## Phase 4: User Story 3 - Static validation, fixture, and harness gates (Priority: P2)

**Goal**: Record mechanical assertions of YAML structure/conditions with exact commands and expected/observed outcomes; run synthetic fixture; report applicable harness gates honestly. Static proof does not replace hosted runtime acceptance.
**Independent Test**: Written assertion records with command/exit/expected/observed + `Test-RunMutation.ps1` exit; gate exits recorded without folding skips into PASS.
**Order**: T006 → T007.

### Tests / validation for User Story 3

- [ ] T006 [US3] After T005: Using available tooling only (no new dependency, framework, or script file), record mechanical assertions against `.github/workflows/ci.yml` with exact command, exit code, expected outcome, and observed outcome for: YAML structure and condition truth table for all four events; immutable history/base/head preparation; exit-mapping wrapper contract (including accepted exit-2 summary labelling — verified, never guessed); stable identity (`mutation-pr` / `PR mutation`); absence of concurrency; publish negatives for PR/schedule/dispatch; and one property-tests job with two distinct project calls. Also assert residual exit-2 hosted-label risk remains static/synthetic-only under the unchanged Q5 hosted receipt contract. Record results in `specs/DEV-472/EVIDENCE.md` (or a clearly linked section pending T008 file creation). Static assertions do **not** replace hosted runtime acceptance (FR-012; `brief.md` Test strategy; Phase 2 step 6 addendum; `PLAN_ADJUDICATION` item 6).
- [ ] T007 [US3] After T006: Run `scripts/Test-RunMutation.ps1` as synthetic runner-contract regression only and record exit/outcome; then run applicable harness gates for the actual diff (`run-roslyn-analyzers.ps1`, `run-cyclomatic-complexity.ps1`, `run-jetbrains-inspectcode.ps1`, `run-web-gates.ps1`, mutation applicability as configured) and record exact commands, HEAD, exits, and distinct `PASS` / `SKIPPED (scope-empty)` / `SKIP` / `N/A` / `Could not run` verdicts — never fabricate production scope to green a skipped lane (FR-012, FR-013, SC-007; `brief.md` Gate expectations).

**Checkpoint**: Static + synthetic layers done; hosted receipts still Phase 5.

---

## Phase 5: User Story 3 - Hosted evidence ledger (Priority: P2)

**Goal**: Capture runtime receipts and required-check registration evidence in `specs/DEV-472/EVIDENCE.md`; keep missing items explicitly pending.
**Independent Test**: Ledger rows with run URL, event, head/base SHA, outcome or pending, plus required-check registration evidence or pending.
**Order**: T008 → T011. Manual dispatch may preflight T011 but does not close it.

- [ ] T008 [US3] After T007: Create/maintain hosted evidence ledger at `specs/DEV-472/EVIDENCE.md` with pending rows covering PR regression-blocking (PR/run/base/head SHA identity), main-push no-Stryker, scheduled full-Core + evaluator (run URL/event/head/SHA/outcome), residual exit-2 hosted-label risk (static/synthetic assurance noted; hosted-label proof absent), and external required-check registration/read-back evidence (FR-012; `brief.md` Task ordering step 4; `CONCLUSIONS.md` Phase 2 Step 6 (4); taste in `ASSUMPTIONS.md`).
- [ ] T009 [US3] After T008: Obtain PR mutation-regression-blocking evidence on a **separate non-delivery** changed-production-C# pull_request (mutation check fails on regression) recording PR/run/base/head SHAs; never introduce artificial `src/` edits in the DEV-472 delivery diff. Also track the user-owned external required-check registration dependency for check name `PR mutation` / job id `mutation-pr`: coordinated by Bernstein/Rigger; live `gh api` (or equivalent) read-back that the check is required; evidence that a red mutation regression blocks normal merge — not merely that the job failed. This plan does **not** authorize repository-settings writes (`recon` line 34: no required checks / 0 rulesets visible at intake). Record receipts or mark explicitly pending. Workflow-only scope-empty PR success is insufficient. Do not claim the ticket done criterion satisfied until registration + red-block evidence exist (FR-012; `brief.md` Q5; Phase 2 step 6 addendum; Compass H1).
- [ ] T010 [US3] After T008: Obtain main-push evidence that no Stryker jobs ran; record receipt or mark explicitly pending (SC-002).
- [ ] T011 [US3] After T008: Obtain a green scheduled full-Core run on the delivered main revision completing all 16 shards and passing merged-report evaluation; record SHA and run URL. workflow_dispatch may be used as preflight only and MUST NOT substitute for this receipt (SC-003, SC-004; `brief.md` Q5). Mark explicitly pending until the scheduled receipt exists.

**Checkpoint**: Ledger complete or honestly pending; Spec/Gate 1 status remains separate from runtime acceptance; required-check registration may remain pending without inventing a structural checkbox.

---

## Phase 6: Review loop (size M)

**Purpose**: Close review against the Q6 bar without expanding frozen scope.
**Checkpoint**: No unresolved in-scope Critical/High/Medium/Low findings, or stop for owner review after round cap.

- [ ] T012 After T007 (and with T008–T011 ledger state visible): Run capped code-review / ship-review per brief Q6 — every unresolved in-scope Critical/High/Medium/Low blocks closure with explicit evidence-based disposition; Sentry, Ledger, and Compass evidence mandatory on M; max two rounds and two fix commits per round; third-round findings go to owner review through the PR; frozen scope is Q1–Q5 event routing, mutation policy preservation, property-test end-state, and acceptance evidence — anything else is a follow-up issue, not a finding in this round (FR-014). Historical ADR 0016 / `AGENTS.md` routing wording remains noted, no ticket (`CONCLUSIONS.md` Phase 2 Step 6) — do not open a follow-up request for those files.

---

## Phase 7: Delivery handoff

**Purpose**: Merge-bar handoff; user merges; do not claim runtime acceptance or ticket merge-blocking done while ledger/required-check items remain pending.

- [ ] T013 After T012: Prepare delivery/merge-bar handoff from evidenced tasks only; keep any pending T009–T011 receipts and the external `PR mutation` required-check registration/read-back/red-block proof explicit in `specs/DEV-472/EVIDENCE.md`; user merges only. Do not claim nightly/runtime PASS from fixture success or from Gate 1 alone; do not claim ticket done until required-check evidence exists; do not authorize or perform repository-settings mutation from this plan (`brief.md` Handoff; FR-013; Phase 2 step 6 addendum).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 0 (T000)**: Blocks all Build work.
- **Phase 1 (T001)**: Depends on T000; blocks implementation.
- **Phase 2 (US1)**: T002 → T003 after T001. Push full lane still present after this phase.
- **Phase 3 (US2)**: T004 → T005 after T003 (same `ci.yml` file — sequential). T004 atomically adds schedule and excludes push.
- **Phase 4 (validation)**: T006 → T007 after T005.
- **Phase 5 (hosted evidence)**: T008 after T007; T009/T010/T011 after T008 (can proceed in parallel once ledger exists).
- **Phase 6 (review)**: T012 after T007; uses ledger state from T008–T011.
- **Phase 7 (handoff)**: T013 after T012.

### User Story Dependencies

- **US1 (P1)**: After T001 — MVP topology for PR gate **without** dropping push full mutation.
- **US2 (P1)**: After US1 in the same file — atomic schedule + push exclusion; verify no-concurrency + publish guard; property-tests retention.
- **US3 (P2)**: After US2 — validation + evidence; does not rewrite US1/US2 behaviour.

### Parallel Opportunities

- After T008: T009, T010, and T011 can progress in parallel (different hosted events/receipts).
- No parallel edits inside `.github/workflows/ci.yml` across T002–T005.

### Forbidden in every task

- Edits to `scripts/run-mutation.ps1`, `scripts/ci-stryker-shard.sh`, `scripts/Test-RunMutation.ps1`, `stryker-config.json`, `harness.yml`, production/`web` sources without a new cited ruling.
- Edits to `docs/adr/0016-stryker-mtp-runner.md` or `AGENTS.md`; requesting a follow-up ticket for their historical-routing drift (Patron: noted, no ticket).
- Inventing a `concurrency:` group / cancel-in-progress policy, or repository-settings / required-check writes.
- Stryker `--since`; lowering thresholds; relaxing kill-share / kill-file-span / exclusions.
- Claiming exit 2 / scope-empty / fixture green / dispatch green / hosted check success as mutation PASS or as the scheduled nightly receipt.
- A releasable intermediate workflow that excludes push full mutation before schedule replacement exists.
- Expanding file scope to fix a runner defect without recon + ruling.

---

## Parallel Example: Hosted receipts

```text
# After T008 ledger exists at specs/DEV-472/EVIDENCE.md:
Task: "T009 non-delivery PR regression-blocking + required-check registration evidence (or explicitly pending)"
Task: "T010 main-push no-Stryker receipt (or explicitly pending)"
Task: "T011 scheduled full-Core green receipt (or explicitly pending)"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 0–1 (Gate 1 + pickup recon)
2. Complete Phase 2 (US1 ADD `mutation-pr` + PR-only full-lane exclusion; push full lane retained)
3. **STOP and VALIDATE**: PR topology + honest exits; confirm push full mutation still present before T004

### Incremental Delivery

1. US1 → PR diff gate live; push full coverage retained (not a shippable cut that drops push Stryker)
2. US2 → atomic nightly + push exclusion; verify no-concurrency + publish guard; property-tests retained
3. US3 → mechanical assertions/fixture/gates + `EVIDENCE.md` ledger (pending allowed)
4. Review + handoff without false runtime or required-check claims

### Parallel Team Strategy

Single-file workflow edit stays sequential (T002–T005). Evidence gathering (T009–T011) is the parallel lane after topology lands.
