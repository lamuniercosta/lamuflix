# DEV-472 Grill Conclusions

Worktree: <task-worktree>/feature-472-spec
Branch: feature/472-spec
Baseline: e06587031dae1d974a41ee98e7b23aa291107129
Factual source: recon-DEV-472. Decision authority: Patron under charter section 2.3.

## Q1/12 - Property-test duplicate reconciliation

**Keel question:** Recon lines 11 and 22 establish no duplicate property-test invocation at the ticket locations on this HEAD. I recommend treating removal as an already-satisfied outcome: retain the one property-tests job and its separate UnitTests/IntegrationTests calls, record stale locations through Rigger without changing deliverables, and require no duplicate invocation after this change. This avoids deleting distinct coverage; cost is documenting evidence rather than showing an artificial deletion. Do you approve this interpretation under section 2.3, or does the exact ticket require a structural checkbox?

**Patron answer:** Approved under section 2.3 (no structural checkbox): end-state deliverable (single property-tests job, no duplicate invocation) is unchanged per recon-DEV-472 lines 11 and 22; retain .github/workflows/ci.yml:295-315 and record stale line refs via Rigger.

**Cited ruling:** Approved: treat duplicate property-test removal as already satisfied on HEAD e06587031dae1d974a41ee98e7b23aa291107129 without a structural checkbox. Recon lines 11 and 22 verify one property-tests job, one call each for UnitTests and IntegrationTests, and no duplicate at ci.yml:38 or ci.yml:277-287. Charter section 2.3 permits reconciling stale references through Rigger where the delivered outcome is unchanged.

**Rationale and implications:** Preserve distinct coverage. Acceptance checks the final no-duplicate outcome; it does not demand an artificial deletion. Patron owns the recording request to Rigger. No user scope decision arises from this reconciliation.

---

## Q2/12 - CI event topology

**Keel question:** I recommend one event matrix in existing .github/workflows/ci.yml: pull_request to main runs the diff-scoped scripts/run-mutation.ps1 -BaseRef gate; push to main runs no Stryker; a nightly schedule runs the existing full Core 16-shard run plus merged-report evaluator; workflow_dispatch retains an explicit full-Core diagnostic path. Reuse the existing sharding/evaluation scripts and do not change their gate policy. Basis: recon lines 4,8-10,12,14,21 and ticket desired outcomes. Cost: event guards must also prevent full shards and full evaluator from running on PR/main push, and nightly must not accidentally run ordinary publish work. Approve this event topology and bounded reuse?

**Patron answer:** Approved: single-workflow event matrix in .github/workflows/ci.yml (pull_request runs scripts/run-mutation.ps1 -BaseRef; push to main runs no Stryker; nightly schedule and workflow_dispatch run 16-shard stryker-core + stryker-core-gate; event guards exclude push/PR from full shards and exclude schedule from publish-image) reusing existing scripts per recon-DEV-472 lines 4, 8-10, 12, 14, 18, 21.

**Rationale and implications:** Move full-Core cost off main push, retain full nightly coverage and on-demand diagnostics, and make the PR diff gate authoritative for changed production code. Preserve the full report evaluator and existing gate policy; scheduled runs must not publish images. No dependency or production behavior change.

---

## Q3/12 - PR base and mutation result contract

**Keel question:** I recommend the PR gate compares the PR head against an explicitly fetched immutable base SHA from the pull_request payload, passing that SHA to -BaseRef with sufficient Git history; never dotnet-stryker --since. Keep one stable PR mutation check, fail on runner exit 1 or inability to run, and translate exit 2 only as script-reported SKIPPED (scope-empty) or N/A, visibly labelled without claiming mutation PASS. Changed production C# must exercise the real runner and block on mutation regression; do not weaken thresholds, timeouts, kill-share/file-span safeguards or exclusions. Basis: recon lines 4,12-14 plus charter standing ruling on since and scope-empty exits. Cost: checkout/fetch preparation and explicit exit translation are required. Approve this base-selection and PR result contract?

**Patron answer:** Approved: PR gate fetches the immutable PR base SHA and passes it to scripts/run-mutation.ps1 -BaseRef (never dotnet-stryker --since), fails on exit 1 or unrunnable state, translates exit 2 visibly as non-blocking SKIPPED (scope-empty) or N/A without claiming PASS, and enforces existing thresholds/safeguards on changed production C# per recon-DEV-472 lines 4, 12, 14 and AGENTS.md.

**Rationale and implications:** Ensure the PR diff is bounded to its declared base, prevent the known since-filter false green, and preserve honest non-mutating outcomes. The PR head and base must both be available; missing history or a missing executable cannot become a passing mutation result. Hosted check success for an accepted exit 2 is distinct from a mutation PASS.

---

## Q4/12 - Nightly schedule and cancellation isolation

**Keel question:** I recommend nightly cron 17 3 * * * (03:17 UTC) on the default branch, reusing the existing full Core sharding/report evaluation without changing production mutation policy. Exact wall-clock time is operational taste, so log it [assumed]. Keep scheduled full runs independent of push/PR cancellation so ordinary traffic does not cancel the only full nightly result. Retain full manual dispatch as Q2 decided. Cost: full nightly compute and possible waiting for available runners; no guarantee of exact start time. Basis: ticket nightly-full outcome in recon line 4 and existing sharding/evaluation in lines 8-10,14. Approve this schedule and isolation contract?

**Patron answer:** Approved: nightly schedule cron 17 3 * * * (03:17 UTC, logged [assumed] in specs/DEV-472/ASSUMPTIONS.md) on main, reusing existing 16-shard stryker-core + stryker-core-gate and workflow_dispatch, with concurrency isolated so push/PR runs cannot cancel scheduled nightly runs per recon-DEV-472 lines 4, 8-10, 14, 21.

**Rationale and implications:** Preserve an uninterrupted full mutation lane independent of normal CI traffic. Exact start time is an operational preference, not a deadline guarantee. Full-run cost remains nightly and manually available.

---

## Q5/12 - Validation and runtime acceptance

**Keel question:** I recommend validation in three layers: (1) workflow event-matrix checks cover PR, push main, schedule and manual dispatch, including guards, base argument, cancellation isolation and no property-test duplicate; (2) run existing scripts/Test-RunMutation.ps1 as runner-contract regression, explicitly synthetic and not nightly evidence; (3) hosted evidence proves changed-production-C# PR regression blocks the mutation check, main push runs no Stryker, and an actual scheduled full-Core run on the delivered default-branch SHA completes all shards and passes merged-report evaluation. A manual full run is useful preflight but does not substitute for the ticket green nightly criterion. Workflow-only scope-empty PR results do not prove regression blocking. Spec/Gate 1 may proceed, but implementation/nightly acceptance remains explicitly pending until those receipts exist; do not manufacture production edits in the delivery diff just to demonstrate the gate. Cost: external runtime evidence and nightly completion cannot be replaced by fixture tests. Basis: recon lines 4,8-13,21 and phase separation. Approve this acceptance and evidence contract?

**Patron answer:** Approved: three-layer validation contract (1: workflow event-matrix/guard/BaseRef/isolation/no-duplicate checks; 2: scripts/Test-RunMutation.ps1 as synthetic runner-contract regression only; 3: hosted runtime evidence for PR regression blocking without shipping artificial src/ edits, main-push without Stryker, and green scheduled nightly 16-shard + stryker-core-gate run, with workflow_dispatch preflight only and Spec/Gate 1 separated from runtime receipts) per recon-DEV-472 lines 4, 8-13, 21.

**Rationale and implications:** Static topology and synthetic fixtures cannot certify hosted mutation behavior. Keep an evidence ledger with immutable run/head references and each pending result. Runtime acceptance is not a prerequisite falsely claimed complete during planning; the ticket remains incomplete until its actual nightly criterion is evidenced.

---

## Q6/12 - File boundary, ordering, and review loop

**Keel question:** I recommend minimal workflow-only implementation in .github/workflows/ci.yml, reusing scripts/run-mutation.ps1, scripts/ci-stryker-shard.sh and scripts/Test-RunMutation.ps1 without rewriting them or generated stryker-config.json. No new dependency/project/API/schema/production/web edits. Plan ordering: event matrix and base/exit wiring; nightly guards and cancellation isolation; static checks plus existing runner fixture; hosted evidence ledger. If implementation exposes a real runner defect, stop for recon and a cited decision rather than silently expanding scope. For review loop terms, block on every unresolved in-scope Critical/High/Medium/Low finding, with explicit dispositions; max two review rounds and two fix commits per round, third-round findings go to owner review and do not silently clear. Frozen scope is exactly Q1-Q5 event routing, mutation policy preservation, property-test end-state and acceptance evidence; anything else is a follow-up issue, not a finding in this round. Use current applicable harness gates and preserve non-blocking SKIPPED (scope-empty), N/A and configured SKIP as distinct from PASS; failures and Could not run block. Cost: stricter Low closure and deferring unrelated cleanup. Approve this file boundary, task ordering and loop discipline as the final plan decision?

**Patron answer:** Approved: minimal .github/workflows/ci.yml-only file boundary (reusing scripts/run-mutation.ps1, scripts/ci-stryker-shard.sh, scripts/Test-RunMutation.ps1; no edits to generated stryker-config.json; stop for recon/ruling if a runner defect emerges), four-step plan ordering, and review-loop/gate discipline (frozen Q1-Q5 scope, block on all unresolved Critical/High/Medium/Low, max 2 rounds and 2 fix commits/round, distinct SKIPPED/N/A/SKIP vs PASS) per recon-DEV-472 lines 4, 8-14, 17-18, 21 and AGENTS.md.

**Rationale and implications:** Limit behavior changes to workflow routing and orchestration; keep existing mutation policy and runner behavior intact. All four review severities require explicit closure. The cap bounds rounds without waiving unresolved findings. Scope exceptions require recon and a recorded ruling before changing the plan.

---

## Q7/12 - Shared understanding and grill closure

**Keel question:** Please read brief.md together with CONCLUSIONS.md and ASSUMPTIONS.md in specs/DEV-472. Does it accurately capture our shared understanding and all Q1-Q6 decisions, with no unresolved scope decision, so the grill can close and Quill can draft? This confirms the grill only; plan challenge/freeze, Gate 1 and actual runtime acceptance remain separate. Please confirm whether the Q1 stale-reference request has been routed to Rigger, or mark the recording receipt pending.

**Patron answer:** Confirmed: specs/DEV-472/brief.md, CONCLUSIONS.md, and ASSUMPTIONS.md accurately capture all Q1-Q6 decisions from recon-DEV-472 lines 4-23 with no unresolved scope decisions so the grill can close and Quill can draft (plan challenge/freeze, Gate 1, and runtime acceptance remain separate); the Q1 stale-reference comment request has been routed to Rigger (<temp>/dev-472-youtrack-comment.md) with Rigger verified recording receipt pending.

**Rationale and implications:** Grill closed at seven questions out of the twelve-question budget. Quill may draft from the brief. The stale-reference recording request is routed, but no verified YouTrack write is claimed. No structural escalation remains from this grill. Plan challenge/freeze, Gate 1 and hosted runtime acceptance remain subsequent states.

---

## Phase 2 Step 6 — Plan Adjudication Rulings

**Verdict:** Approved (1), (2), and (4); approved (3) on `.github/workflows/ci.yml` comment removal and keeping `docs/adr/0016-stryker-mtp-runner.md` / `AGENTS.md` out of the diff, corrected under charter §2.3 to record their historical-routing doc drift as `noted, no ticket` rather than a follow-up ticket.

- **(1) Merge-blocking PR check identity & external registration (Compass H1, Ledger L1):** Freeze job id `mutation-pr` and display/check name `PR mutation` (taste logged `[assumed]` in `specs/DEV-472/ASSUMPTIONS.md`); ticket done criterion ("PR mutation regression blocks merge", `recon-DEV-472` line 4) already authorizes merge-blocking behavior without a Gate 1 structural checkbox, and because `main` currently has `required_status_checks: null` and `0` rulesets (`recon-DEV-472` line 34), require an explicit external user-owned required-check registration step coordinated by Bernstein/Rigger with live `gh api` read-back (no repo-settings mutation by this plan; runtime acceptance stays pending until `PR mutation` is required and a red regression blocks normal merge).
- **(2) Concurrency absence & (3) comment/doc boundary (Sentry H3, Ledger M1/M2, Compass L2/L3):** Preserve the absence of any `concurrency:` block across all four events in `.github/workflows/ci.yml` without inventing a cancellation policy (`recon-DEV-472` line 35 satisfies Q4/FR-007 isolation); remove stale routing comments in `.github/workflows/ci.yml:213-224` in-diff (`recon-DEV-472` line 8; `AGENTS.md` no-explanatory-comments rule) while keeping unnamed tracked files `docs/adr/0016-stryker-mtp-runner.md` (`recon-DEV-472` line 14) and `AGENTS.md` outside the implementation diff under charter §2.3 rule 6.
- **(4) Evidence ledger path, non-delivery regression PR, & exit-2 scope (Compass M1, L1):** Pin `specs/DEV-472/EVIDENCE.md` for runtime receipts (logged `[assumed]` in `specs/DEV-472/ASSUMPTIONS.md`), prove changed-C# PR mutation-regression blocking on a separate non-delivery PR so no artificial `src/` edits enter the DEV-472 delivery diff, and retain the Q5 hosted evidence scope with exit-2 (`SKIPPED (scope-empty)` / `N/A`) labelling verified statically (`T006`) and synthetically (`scripts/Test-RunMutation.ps1`) plus explicit residual hosted-label risk recorded.

- Out-of-scope historical-routing prose in `docs/adr/0016-stryker-mtp-runner.md:64-77` and `AGENTS.md` (Ledger M2, Medium; not Critical/High, broken behaviour, or user-requested per charter §2.3): `noted, no ticket`.

---

## Phase 2 Step 7 — Gate 1 Ruling

**Verdict:** `gate1: provisional` approved (`specs/DEV-472/spec.md` `**Status**:` updated); no owner questions or structural checkboxes required.

- **Basis:** Plan is frozen in `specs/DEV-472/brief.md:100-108` after Keel's independent read-back (`PLAN_ADJUDICATION.md`) and Q1–Q7 / Phase 2 Step 6 rulings; `specs/DEV-472/checklists/requirements.md:33-38` User-Visible Outcome items are ticked and `spec.md:69-71` states "Nothing visible" with reason (CI workflow routing and mutation scheduling only).
- **Escalations (§2.3):** None — no ticket contradiction and no constitution departure (`spec.md:11-13`, `plan.md:39-61`). External `PR mutation` required-check registration remains a tracked runtime prerequisite (`spec.md:157`), not a Gate 1 structural checkbox.
