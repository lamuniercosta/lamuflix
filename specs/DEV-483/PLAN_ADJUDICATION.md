# DEV-483 Plan Adjudication — Round 1

Status: NEEDS FIXES. PLAN FROZEN is withheld pending Quill repairs and Keel independent reread. Gate 1, runtime acceptance and merge clearance remain separate and open.

## Evidence and authority

Read all three axis notes: findings-DEV-483-Sentry (61 lines), findings-DEV-483-Ledger (47 lines), findings-DEV-483-Compass (43 lines), plus DEV-483 and recon-DEV-483. Checked spec.md, plan.md, tasks.md and brief.md against the cited findings. No production-code reconnaissance or runtime measurements performed.

Sentry reports reviewed HEAD d7a8b6247f35100d8ba33fdb1221e1d92b985076; Compass identifies recon reference 73f3f74ac2049be1ca4c685472a2b4df42c237f0, not its exact challenge HEAD; Ledger identifies the branch but supplies no SHA. These are report provenance as supplied, not a claim of independently verified same-head review. Exact reviewed-snapshot provenance must be clarified through Bernstein/Wisp before freeze; no missing axis is treated as skipped.

Authority: brief Q1–Q6, ticket at recon-DEV-483:42–48, and Patron full cited ruling in CONCLUSIONS.md, section Plan Challenge Round 1 Adjudication. Patron approved all dispositions below with zero structural escalation or file-boundary expansion. All decisions are mirrored in brief.md before Quill repair dispatch.

## All findings and dispositions

| Axis / ID | Severity | Adjudication and artifact evidence | Repair |
|---|---|---|---|
| Sentry H1 | High | Reject unconditional concurrent acceptance leg: brief:65/Q2 explicitly selects sequential samples; recon:42 does not establish a concurrency trigger. Accept conditional investigation if evidenced; no claim that concurrency is the cause. | 1 |
| Sentry H2 | High | Accept conditionally. FR-002 and T005 preserve history but T004/T006 need explicit existing cache-identity/history boundary regressions if cache touched. No invented multi-user contract or prescribed cache-key design. | 2 |
| Sentry M1 | Medium | Accept narrowed to actual diagnosis: T004 lacks an explicit deterministic performance-relevant invariant. Query counts are an example only when causal. | 2 |
| Sentry M2 | Medium | Accept. T003 must observe cancellation propagation/abandoned work, without presuming pool exhaustion. | 1 |
| Sentry M3 | Medium | Accept. T002/T007 validity lacks route-specific fixture oracles. Fast empty results cannot satisfy a fixture with eligible recommendations. | 3 |
| Sentry M4 | Medium | Accept matched direct-API comparison in T003 with documented transport differences; web port 3000 remains acceptance path. Diagnostic facilities must come from recon, not invented names/APIs. | 1, 4 |
| Sentry M5 | Medium | Accept. T003 attachment alone does not prove T002 exercised the newly diagnosed trigger. Require pre-edit rerun when absent. | 1 |
| Sentry M6 | Medium | Accept; duplicate Ledger M1. tasks:162 permits undefined resource-safe overlap, contrary to idle measurement conditions. | 5 |
| Sentry L1 | Low | Accept protocol reproducibility clarification: readiness without warming target, fixture pin/reset, paired controls and noise interpretation; no new control thresholds. | 3, 5 |
| Ledger M1 | Medium | Accept; duplicate Sentry M6. Remove conflicting tasks:162–171 concurrency wording/examples. | 5 |
| Ledger M2 | Medium | Accept bounded instrumentation policy. plan:48 and T003 only say existing facilities; no new telemetry API or arbitrary Stopwatch decision needed. Retained changes require justification and full gates. | 4 |
| Ledger M3 | Medium | Accept boundary/layering clarification only. plan:120/T003 STOP remains valid; no speculative expansion to adapters/config/startup and no extra suite facts without recon. | 4 |
| Ledger L1 | Low | Accept honest script-derived mutation receipt, not assertion that every Api/Infrastructure edit is scope-empty or exempt. Actual configuration/output determines eligibility. | 6 |
| Ledger L2 | Low | Accept explicit applicable configured complexity/refactor-gate obligation; no threshold override or invented exemption. | 6 |
| Ledger L3 | Low | Accept concrete disposable untracked evidence location, replacing tasks:64 vague Build workspace wording; never publish credentials/config dumps. | 7 |
| Ledger L4 | Low | Accept non-blocking metadata cleanup at freeze; plan:7/tasks:10 are stale. | 10 |
| Compass M1 | Medium | Accept. T009 precedes T010 fixes but T011 lacks final-head rerun/freshness rule. | 8 |
| Compass M2 | Medium | Accept. tasks:108/148 permit early T008, contradicting brief:85–86. Correct ordering requires Q2 status and mandatory gates before cause routing. | 9 |
| Compass L1 | Low | Accept; duplicate Ledger L4, including spec status. | 10 |
| Compass L2 | Low | Accept; merge with Sentry L1. T001 controls need corresponding T007 control batches, interpreted diagnostically. | 3, 5 |
| Compass L3 | Low | Accept non-blocking attribution clarity. State Build-lane default in conventions instead of inventing named seats. | 10 |

Count: 21/21 explicitly disposed. One unconditional remedy rejected with conditional diagnosis retained; 20 accepted, including conditional/narrowed and duplicate items. Accepted non-blocking Low items have explicit repairs; none is silently waived.

## Consolidated Quill fix list — round 1

Apply consistently to spec.md, plan.md, tasks.md, checklists/requirements.md and DRAFTING_RECEIPT.md as relevant. Do not edit brief.md, CONCLUSIONS.md or this adjudication; Keel owns those decisions. Do not invent facts or expand production/test boundaries. One list for this round; two-round/two-fix-commit caps remain.

1. T003: investigate concurrency when evidence indicates it; observe cancellation/abandoned work; compare matched direct-API and web-proxy requests with transport differences recorded. If diagnosed trigger was absent from T002, rerun the affected complete baseline protocol with it before T004/production edits. No unconditional concurrent acceptance leg; retain Q2 sequential acceptance.
2. T004–T006: add a deterministic performance-relevant invariant selected from actual diagnosis (query/round-trip bounds only if causal). If cached profile source is touched, prove preservation of existing cache identity/history boundaries with regression coverage; collect missing semantics through recon, never invent per-user architecture. Keep Testcontainers rule and no unit wall-clock assertions.
3. T001/T002/T007: recon supplies route-specific status/payload/fixture oracles: known details identity/content and existing contract; missing id 9999 existing 404 semantics; by-movie eligible results and preserved ranking/filtering; for-you fixture-specific history/candidate expectations, including non-empty where eligible. Define readiness observation without prewarming target; pin/reset dataset/history between comparable runs. Repeat genres/movie-list control batches before and after; record host/control noise and investigate comparability without adding numeric acceptance thresholds.
4. T003/plan: existing Activity/log facilities only, exact available facilities via recon; TimeProvider for injected time, existing comment rules, retained instrumentation justified inside approved candidate boundary and gated. Temporary instrumentation must be accounted for and removed before fix acceptance, with measured build identity recorded; any retained code is a production change. A proven outside-boundary cause is an expected STOP for cited Patron ruling, never permission to edit. Preserve Core dependency direction and layering evidence through existing tests; obtain suite facts via recon if needed.
5. Forbid build/gate/test or competing measurement/packaging load during T002/T007 sample windows. T009 and T007 must run sequentially. Remove resource-safe concurrency permission and conflicting example; controls receive the same isolation. Record contaminated runs and rerun for valid comparison rather than silently ignoring failing samples.
6. T009/plan: use actual supported scripts and configured applicable complexity/refactor obligations; no new hardcoded threshold, lower threshold or invented refactor opt-out. Mutation eligibility/exemptions and exact SKIPPED (scope-empty), SKIP, N/A, FAIL/PASS/Could not run come from actual script/config output, never assumed by project name. Do not open/change preserved harness.yml dirt. Missing configuration facts go through recon.
7. Name disposable untracked receipt directory resolved by git rev-parse --git-path dev-483-evidence. Store scrubbed commands/raw samples/provenance/summaries there, no secrets, connection strings, tokens, env dumps or raw config dumps. Record safe config identity only. This is operational evidence, not a new source folder. Publish a scrubbed reviewable receipt through team handoff; do not commit raw machine/config artifacts accidentally.
8. T009–T011: after any T010 remediation commit, rerun full mandatory gate set on final delivery/review HEAD before packaging. Explicitly annotate stale receipts; refresh affected Q2 runtime evidence and cause documentation/read-back when the change invalidates them. Never label an old-head run as final-head PASS.
9. Make T008 require T007 outcome/status recorded and mandatory gate run/status recorded first, per brief Q6. Reorder local phases/dependency map to T007 then T009 then T008, followed by T010/T011; no dependency cycle. Failed/unavailable runtime/gates remain open and cannot yield Done when, even if cause documentation is recorded. Remove early After T003/when available routing.
10. State generic Build-lane ownership for otherwise unassigned tasks. Refresh all stale drafting/analyze metadata to current challenge-repair state; only Keel declares PLAN FROZEN after independent reread. Gate 1, implementation, runtime and merge remain distinct. Update receipt/checklist with repair traceability and no new decision claims.

## Closure

Quill repair reread: pending. Same-snapshot report provenance: pending clarification. PLAN FROZEN: withheld. No production gates run for this plan-only adjudication.

## Independent repair reread — 2026-11-12

Keel read `_quill-round1-report.md` and independently checked the repaired spec requirements/scenarios, plan approach/test strategy, task execution/dependency graph, requirements checklist and drafting receipt. The report was treated as a handoff, not closure evidence by itself.

| Repair | Independent artifact verification | Result |
|---|---|---|
| 1 | T002/T003 and FR-010/012/019 preserve sequential acceptance, conditional concurrency diagnosis, cancellation/abandoned-work observation, matched transport comparison, and pre-edit re-baseline | CLOSED |
| 2 | T004–T006 and FR-006 require diagnosis-selected invariant and conditional existing cache identity/history regression, without guessed user semantics or unit latency timers | CLOSED |
| 3 | T001/T002/T007 and FR-009 require recon oracles, non-warming readiness, fixture pin/reset and repeated diagnostic controls/noise interpretation | CLOSED |
| 4 | Conventions/T003/T005/T006 and FR-017/019 retain instrumentation boundary/gating, TimeProvider, dependency direction and outside-boundary stop | CLOSED |
| 5 | T002/T007/T009 and Parallel Opportunities exclude competing sample-window load and rerun contaminated measurements | CLOSED |
| 6 | T001/T009 and FR-015 derive gate obligations/eligibility from actual supported scripts/config, preserve honest verdicts and harness dirt | CLOSED |
| 7 | Evidence location, T002/T007 and FR-009 name disposable git-local directory, scrubbed handoff and secret/config-dump exclusions | CLOSED |
| 8 | T010/T011 and FR-015 require full final-head gate rerun after remediation plus invalidated runtime/cause receipt refresh | CLOSED |
| 9 | Phase map and dependency graph require T007 → T009 → T008 → T010 → T011 without cycle or early cause routing | CLOSED |
| 10 | Artifact headers and task conventions use challenge-repair state/Build-lane attribution; checklist and receipt trace all ten repairs without declaring freeze/Gate 1 | CLOSED |

Content repair verdict: CLEAN for the Round 1 fix list; all 21 original findings retain their explicit dispositions above. No further numbered content repair list issued and no additional challenge round claimed. No runtime or production gate claim follows from this documentation review.

Provenance remains unresolved: recon-DEV-483:51–63 explicitly concludes that exact Compass and Ledger reviewed snapshots are not confirmable from the authorized roots. Its current-head/commit-order evidence does not attest which working-tree artifact versions either challenger read. The generic PLAN FROZEN wording in downstream delivery tasks does not authorize those agents to freeze this plan; brief/FR-020 reserve that action to Keel after closure.

Final status for this reread: content repairs CLOSED; PLAN FROZEN WITHHELD solely for challenge snapshot provenance. Needs recon: obtain Compass and Ledger author attestations identifying the exact reviewed commit and artifact working-tree state, or conduct the permitted second challenge round on an explicitly pinned artifact snapshot with all three axes. An unsupported inference from commit order or recon reference cannot close this item. Gate 1, implementation, runtime acceptance and merge remain open.

## Final provenance disposition and plan freeze — 2026-11-12

Status: PLAN FROZEN. Supersedes earlier withheld/pending statuses, retained as chronology. All 21 findings retain their dispositions; all ten content repairs remain CLOSED after Keel independent reread.

Compass attestation at findings-DEV-483-Compass:45–56 confirms the original uncommitted plan/spec/tasks/brief review over base d7a8b6247f35100d8ba33fdb1221e1d92b985076, before adjudication repairs. Ledger at findings-DEV-483-Ledger:49–56 attests its findings apply to pre-adjudication drafts over that same base while explicitly declining proof of original checkout HEAD or current-tree equivalence. These author statements and their original defect citations settle original draft-stage provenance; Sentry already names d7a8b624 as reviewed HEAD. No review-time content hashes or historical byte-level reconstruction are claimed. Current repaired artifacts were independently reread by Keel, not retroactively attributed to the original reviewers.

Ruling: the original-review provenance condition is CLOSED on that bounded basis. No second three-axis plan challenge required; no extra challenge round consumed. PLAN FROZEN recorded in brief.md. Gate 1, implementation, runtime acceptance, verified YouTrack cause documentation and merge clearance remain distinct/open. No new decisions or production gate claims.
