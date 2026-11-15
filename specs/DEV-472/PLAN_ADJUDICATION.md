# DEV-472 Plan adjudication

Date: 2026-11-14. Worktree: F:/Dev/LamuFlix.worktrees/feature-472-spec; branch feature/472-spec; baseline e06587031dae1d974a41ee98e7b23aa291107129. Planning artifacts are uncommitted; this baseline identifies recon, not a commit containing the draft.

Authority: brief.md Q1-Q7, CONCLUSIONS.md, spec.md, plan.md, tasks.md; notes recon-DEV-472:4-35 and all three findings-DEV-472-{Sentry,Ledger,Compass}. All axis reports are present. Review cap: two Quill repair rounds; two dispatched; round 2 corrects the aggregate count only (Keel originated the typo). Status: PLAN FROZEN after independent verification below. Gate 1, implementation, runtime acceptance and merge clearance remain pending.

## Dispositions

Each row covers the original axis finding, including overlapping findings. Accepted repair required independent read-back; the verified closure table below now records closure for every finding.

| Finding | Disposition and cited basis | Required closure |
| --- | --- | --- |
| Sentry H1 | Accept ordering defect. tasks.md T003 excludes push before T004 adds schedule; MVP stop could retain that intermediate state. Q2/Q4 require replacement automatic full coverage. | T003 excludes PR only; push exclusion and schedule enablement occur atomically in T004. No US1-only release that loses full push coverage. |
| Sentry H2 | Accept. plan.md PR diff check and T002 do not bind the wrapper contract; Q3 requires fail closed and accepted exit 2. | Capture child pwsh process exit immediately; 0 retains real runner PASS, 2 preserves full output and writes explicit script label to job summary while wrapper exits 0, 1/unrunnable/unexpected exit fail. No blanket continue-on-error. T006 mechanically checks mapping. |
| Sentry H3 | Accept gap, now resolvable by recon:35: there is no concurrency block. No existing cancellation semantics can be preserved by inventing a group. | Preserve no concurrency/group/cancel-in-progress for all events; static absence check and drift stop at pickup. Patron approved in CONCLUSIONS.md Phase 2 Step 6. |
| Sentry M1 | Accept with Compass H1. Stable identity and external required-check dependency must be named. recon:34 has no required checks or repository rulesets visible to CLI. | Freeze job id/name; name user-owned registration/read-back dependency and do not claim merge blocking until required-check evidence exists. Patron approved in CONCLUSIONS.md Phase 2 Step 6. |
| Sentry M2 | Accept acquisition gap, not permission to add delivery src edits. Q5 already requires a changed-C# PR receipt. | Use a separate non-delivery changed-production-C# PR with PR/run/base/head identity recorded; unavailable receipt remains pending. |
| Sentry M3 | Accept. T006 narrative alone is weaker than Q5 matrix checks. | Require recorded mechanical assertions of actual YAML structure/conditions with exact commands, expected and observed outcomes, all four event truth tables and exit/history contracts; no new dependency/test project/script edits. Hosted proof remains mandatory for runtime acceptance. |
| Sentry L1 | Accept. Q3 requires concrete history preparation. | Checkout immutable payload head SHA with fetch-depth 0; explicitly fetch payload base SHA, verify both commits exist, pass base SHA via env/argument to -BaseRef; any fetch/verification failure blocks. |
| Sentry L2 | Accept. recon:26 proves push+main publish guard already excludes schedule and dispatch. | Preserve guard; T005 verification-only, T006 negative assertions for PR/schedule/dispatch. |
| Ledger M1 | Accept; duplicate of Sentry H3. recon:35 is authoritative baseline. | Preserve absence of concurrency; remove false existing-cancel premise. |
| Ledger M2 | Accept in-file stale-comment cleanup. recon:8,14 identifies historical routing prose. ADR/AGENTS changes exceed Q6 file boundary. | Remove stale routing comments in ci.yml; Patron ruled historical ADR/AGENTS drift noted, no ticket; no unauthorized rewrite. No explanatory comments introduced. |
| Ledger L1 | Accept; recon:21 says no existing PR mutation job. Registration dependency overlaps Compass H1. | Say add, freeze id/name, record external required-check registration ownership. |
| Ledger L2 | Accept least-privilege/convention constraint; no source inspection beyond named facts is needed. | Reuse existing action versions confirmed by T001 recon; job permissions contents: read; explicit checkout history/head/base preparation. Do not assert unverified version majors as facts. |
| Ledger L3 | Accept; same disposition as Sentry L2. | Preserve push+main publish guard, verify-only. |
| Compass H1 | Accept. recon:34 answers missing configuration facts but does not itself authorize repository-settings writes. Ticket merge-blocking criterion remains required. | Name external user-owned required-check registration/read-back prerequisite, exact identity, evidence and handoff owner; do not invent structural checkbox unless ticket/constitution changes. Patron approved in CONCLUSIONS.md Phase 2 Step 6. |
| Compass M1 | Accept as residual-risk observation under Q5, not an extra hosted acceptance criterion. | Record static exit-2 labelling assurance and residual lack of hosted-label proof; hosted green check differs from mutation PASS. Patron confirmed in CONCLUSIONS.md Phase 2 Step 6. |
| Compass L1 | Accept. T008 has no stable ledger home. | Name specs/DEV-472/EVIDENCE.md, with pending rows, run URL/event/head/base/SHA/outcome and required-check registration evidence. |
| Compass L2 | Accept; overlaps Ledger M2. | Remove stale routing comments in implementation file only. |
| Compass L3 | Accept; overlaps Ledger L1. | T002 adds a new job, does not add/adjust an imaginary existing PR gate. |

Ledger non-finding about web applicability remains informational: report actual scope-empty SKIPPED, never PASS. Compass cleared checks remain cleared: ticket event scope, retained dispatch lane, cron taste, delivered-main SHA recording and Patron Q1 property-test reconciliation.

## Pending authority and repair verification

Patron provided cited rulings on check identity/registration ownership, no-concurrency preservation, documentation disposition, ledger/evidence vehicle and residual label risk. CONCLUSIONS.md and ASSUMPTIONS.md were committed by Patron at 6f87b755. Keel recorded the ruling and concrete engineering contracts in brief.md before dispatching round 1. Quill owns spec/plan/tasks/checklist/drafting receipt repairs; Keel owns this adjudication and decision records. No production/workflow edits in this phase.

## Consolidated Quill repair list - round 1

1. Carry the new brief/CONCLUSIONS decisions consistently into spec, plan, tasks, checklist and DRAFTING_RECEIPT. Keep Gate 1/runtime/merge pending. Remove stale Phase B invent language for check identity/concurrency. Cite recon through line 35.
2. T002 must ADD job id mutation-pr, display/check name PR mutation, PR-only. Reuse existing action versions verified by T001; contents: read; immutable payload head checkout with fetch-depth 0; explicitly fetch immutable payload base SHA and verify both commit objects before run-mutation -BaseRef. Supply event SHAs via env. Fetch/checkout/verification failures fail closed.
3. Bind exit translation: child pwsh process with output preserved and immediate LASTEXITCODE capture. 0 preserves runner PASS; 2 writes runner-reported SKIPPED (scope-empty) or N/A to GITHUB_STEP_SUMMARY and returns wrapper 0; preserve full log. 1, launch failure, missing executable and any unexpected status fail. No blanket continue-on-error and no unvalidated unknown exit-2 classification. Explain hosted success is not mutation PASS.
4. T003 excludes full lane from PR only. T004 atomically adds schedule and excludes push while enabling schedule/dispatch full lane. Update MVP/incremental checkpoints so no releasable state removes push full mutation before nightly exists. Remove stale routing comments in ci.yml, without new explanatory prose.
5. Concurrency: recon:35 has no block; preserve absence, with no group/cancel-in-progress for all four events. T005 verifies this, rather than configuring a new policy. T001 stops for recon/ruling on baseline drift. Publish: preserve existing push+main guard; T005 verify-only; T006 asserts PR/schedule/dispatch excluded. Preserve one property-tests job and two distinct calls.
6. T006 requires recorded mechanical assertions using available tooling, exact command/exit/expected/observed evidence against YAML structure and condition truth table for all four events, base/head/history preparation, exit mapping, stable identity, no concurrency, publish negatives and property calls. No new dependency or script file. Static assertions do not replace hosted runtime acceptance.
7. T008 ledger home specs/DEV-472/EVIDENCE.md. T009 uses separate non-delivery changed-C# PR with PR/run/base/head SHAs; no artificial src in delivery. Record residual exit-2 label assurance as static-only under unchanged Q5 hosted receipt contract. Keep all absent receipts explicitly pending.
8. Required-check configuration: no current required checks/rulesets visible per recon:34. Name user-owned external PR mutation required-check registration dependency, coordinated by Bernstein/Rigger, with live read-back and red-check merge-blocking proof. No repo-settings mutation authorized by plan. T009/T013 must track it; no claim ticket done until evidenced. Keep ci.yml-only implementation boundary. No structural checkbox unless Patron rules a ticket or constitution departure.
9. Documentation drift: Patron ruled historical ADR 0016/AGENTS routing wording noted, no ticket. Do not edit those files or request a follow-up ticket. Keep stale ci.yml comment removal in T002-T004. Cite CONCLUSIONS.md Phase 2 Step 6.

Quill may edit only spec.md, plan.md, tasks.md, checklists/requirements.md and DRAFTING_RECEIPT.md. Report exact repair mapping and needs decision items to Keel. Keel independently reads repairs. Max two repair rounds; round 1 dispatched to Quill after the decision addendum.




## Independent read-back - round 1

Keel read the repaired spec.md, plan.md, tasks.md, checklist and drafting receipt directly, rather than accepting Quill self-report. All substantive repairs match brief.md Phase 2 step 6 addendum and Patron CONCLUSIONS.md rulings. The aggregate count is **18** (Sentry 8, Ledger 5, Compass 5); earlier 19 was Keel arithmetic, not an omitted finding. Round 2 is confined to correcting that count in Quill receipt/report.

| Findings | Verified location | Closure |
| --- | --- | --- |
| Sentry H1 | tasks.md:69,83,188-195; spec.md:144,147 | T003 PR only; T004 atomic schedule and push exclusion; checkpoints preserve coverage. Closed by repair. |
| Sentry H2 | tasks.md:68,98; plan.md:125-129; spec.md:146 | Explicit child pwsh exit mapping and fail-closed history, summary label, no blanket continue-on-error. Closed by repair. |
| Sentry H3; Ledger M1 | plan.md:138-140; tasks.md:55,84; spec.md:149 | No concurrency invented; pickup drift stops. Closed by repair plus Patron ruling. |
| Sentry M1; Ledger L1; Compass H1 | plan.md:125,146-150; tasks.md:68,112,133; spec.md:157,177 | Frozen identity plus user-owned external registration/read-back/red-block dependency; no settings writes and acceptance pending. Closed as planning defects; runtime dependency remains open. |
| Sentry M2 | tasks.md:111-112; spec.md:154,175 | Named ledger and non-delivery changed-C# PR vehicle, no artificial delivery src edits. Closed by repair. |
| Sentry M3 | tasks.md:98; plan.md Test Strategy; spec.md:154 | Recorded mechanical command/exit/expected/observed assertions for all four events/contracts. Closed by repair. |
| Sentry L1; Ledger L2 | tasks.md:55,68,98; spec.md:145 | Immutable payload head/full history, explicit base fetch+verify, env SHAs, contents read, existing action versions. Closed by repair. |
| Sentry L2; Ledger L3 | tasks.md:84,98; spec.md:150 | Publish push+main preserved, verify-only and PR/schedule/dispatch negatives. Closed by repair. |
| Ledger M2; Compass L2 | plan.md:152-154; tasks.md:68-69,83,125; spec.md:153 | ci.yml stale comments removed in implementation task; ADR/AGENTS excluded and historical drift noted, no ticket by Patron ruling. Closed by repair/ruling. |
| Compass M1 | tasks.md:98,111; spec.md:154,175; CONCLUSIONS.md Step 6 (4) | Static/synthetic assurance plus explicit residual hosted-label risk; unchanged Q5 hosted contract. Closed by cited ruling. |
| Compass L1 | tasks.md:98,111-114,133; spec.md:154 | EVIDENCE.md home and exact receipt/pending fields. Closed by repair. |
| Compass L3 | tasks.md:68; plan.md:123-125; spec.md:145 | Add new named job, no imaginary existing gate. Closed by repair. |

Requirement/task cross-check: FR-001/002/005/006 -> T002-T004; FR-003/004 -> T002/T006; FR-007/008/010 -> T005/T006; FR-009/011 -> T001-T007 constraints; FR-012 -> T006-T011; FR-013 -> T008-T013; FR-014 -> T012; FR-015 -> T009/T013. SC-001 -> T002/T006/T009; SC-002 -> T010; SC-003 -> T011; SC-004 -> T004/T011; SC-005 -> T005/T006; SC-006 -> T007/T009; SC-007 -> T007; SC-008 -> T009/T013. No unmapped requirement or unowned acceptance dependency found in repaired artifacts. This is bounded cross-artifact read-back, not a new scripted analyzer PASS or runtime test run.

No additional production/workflow/test inspection or gate execution was needed or claimed for this plan adjudication. Earlier Spec Kit analyze/repair history remains documented in DRAFTING_RECEIPT.md; this step independently validates challenge repairs. Plan freeze awaits only verified round-2 count correction. Gate 1, implementation, runtime acceptance, required-check registration and merge clearance remain pending.


## Final receipt - 2026-11-15

Round 2 independently verified in DRAFTING_RECEIPT.md and _quill-round1-report.md: aggregate 18 = 8 + 5 + 5, all finding IDs mapped. No substantive change was needed after round 1. All 18 findings are closed as plan findings by the verified repair/ruling table; zero unresolved in-scope Critical/High/Medium/Low plan blockers. Round cap 2/2 consumed, no third round.

PLAN FROZEN declared in brief.md. Current HEAD 6f87b7550a88743dbb14bbc78db9e05020667eae contains Patron rulings; most planning artifacts are uncommitted and must be included by Rigger in the spec PR. No implementation, settings mutation, runtime receipt, Gate 1, ticket-done or merge clearance is implied. The earlier pending-state sections are chronological records superseded by this final receipt.
