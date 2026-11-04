# DEV-451 grill conclusions

## Q2 — Measurement cohort and acceptance ticket

**Keel question:** Q2/12: I recommend the recon section 3 candidate set DEV-444 (00799914), DEV-448 (6fcbd1ff), DEV-333 (b25ecb16) for matched before/after measurement, and DEV-444 as your picked under-30-minute acceptance ticket. Freeze each changed-file diff, run baseline and candidate against the same code/test tree per case and same machine, concurrency and timeout; preserve timing JSON and receipt with per-project mutant counts and eligible test-project lists, total wall time and timeout/verdict distribution. No invented historical numbers: Build obtains missing baseline measurements before choosing a lever. Recommendation costs three paired real runs but makes the speed claim auditable. ACCEPT or CHANGE with cited basis? Facts from recon sections 2-4 and 6; missing historical timings are explicitly absent, not a planning blocker because measure-first is ticket work. Report back to Keel.

**Patron answer:** ACCEPT (use DEV-444, DEV-448, DEV-333 for matched before/after measurement; DEV-444 is the Patron-picked <30m ticket; Build measures baseline live before lever selection).

**Rationale and implications:** Ticket scope item 1 and ACs (recon §2:34,44); cohort recon §3:50 and direct test references §4:56,60. Preserve matching code/test tree, machine, concurrency and additional-timeout 90000; record timeout/verdict distribution and raw artifacts. No baseline result is claimed. Full supplied receipt follows below.

---

Worktree: `F:/Dev/LamuFlix.worktrees/feature-451-spec`; branch: `feature/451-spec`.
Facts: `recon-DEV-451`. Patron decides; Keel records. Grill cap: 12 questions.

## Q1 — Preserve mutation policy and resolve dirty configuration

**Keel question:** I recommend treating the pre-existing dirty harness.yml removal as unrelated and forbidden in this ticket, preserving the HEAD gates.mutation values including Tests.Common exclusion, with Bernstein/Rigger arranging ownership and restoration before implementation and proof that final PR diff leaves gates.mutation unchanged. Do you ACCEPT or CHANGE? Basis recon section 1 and ticket AC3. Cost: owner cleanup prerequisite; no scope change or owner checkbox if the removal is excluded.

**Patron answer:** ACCEPT (keep HEAD harness.yml gates.mutation values; exclude and restore dirty Tests.Common removal before implementation).

**Cited rationale:** Ticket AC3 and scope item 4 (recon §1:26, §2:37,44) prohibit a mutation-policy diff. Recon §1:27 shows Tests.Common has no eligible unit-test observer, so removing its exclusion makes its changes fail classification. Charter §2.3 does not require an owner checkbox for restoring compliance.

**Implications:** Bernstein/Rigger must arrange restoration to intake HEAD `010e0cb6` before implementation; Keel does not change harness.yml. Preserve threshold 80, maxKillShare 50, maxKillFileSpan 15, exemptions, all four exclusions, and the default half-core concurrency ceiling. Final PR must prove unchanged gates.mutation values.

**Full Patron receipt (JSON-escaped to preserve control characters in the supplied file):**

```json
"[from Patron] DEV-451 Q1 Ruling: ACCEPT — exclude the pre-existing dirty harness.yml modification from DEV-451, restore harness.yml to HEAD (\u000010e0cb6) via Bernstein/Rigger before implementation, and require the final PR diff to leave gates.mutation in harness.yml unchanged.\n- Ticket AC 3 & Scope item 4 (\recon-DEV-451 §1 line 26, §2 lines 37, 44): explicitly require that the PR diff does not change gates.mutation values in harness.yml (\threshold: 80, maxKillShare: 50, maxKillFileSpan: 15, killFileSpanExempt, and the 4 \u001bxclusions entries at HEAD harness.yml:18-47).\n- Behavioral & gate integrity (\recon-DEV-451 §1 line 27): \tests/LamuFlix.Tests.Common/LamuFlix.Tests.Common.csproj:6 has <IsTestProject>false</IsTestProject> and its sole direct referrer is LamuFlix.IntegrationTests (IntegrationTests.csproj:41), which Get-EligibleTestProjects (scripts/run-mutation.ps1:200) excludes by design; removing the LamuFlix.Tests.Common exclusion causes scripts/run-mutation.ps1:1038-1039 (1163-1164 / 1255-1256) to fail with exit 1 on any Tests.Common edit.\n- Charter §2.3 item 6 (\recon-DEV-451 §7 line 115): harness.yml is an unnamed tracked config file whose dirty edit contradicts AC 3 rather than being forced by a gate or AC; restoring it to HEAD requires no ticket change or owner checkbox.\r\n"
```

---

**Full Patron Q2 receipt (JSON-escaped):**

```json
"[from Patron] DEV-451 Q2 Ruling: ACCEPT — adopt DEV-444 (\u00000799914), DEV-448 (6fcbd1ff), and DEV-333 (\b25ecb16) as the three recent Core changes for matched before/after measurement, designate DEV-444 (\u00000799914) as the Patron-picked Core ticket for the under-30-minute pre-PR acceptance run, and require Build to capture live baseline before/after timing JSON and receipt artifacts on the same machine, code/test tree, concurrency, and timeout before picking the lever.\n- Ticket Scope item 1 & ACs (\recon-DEV-451 §2 lines 34, 44): require measuring wall time, mutant count, and eligible test projects from mutation-timing.json and the mutation receipt across >= 3 recent Core changes first, plus proving a < 30 min local pre-PR run on a Patron-picked Core ticket.\n- Candidate set & representative Patron-picked ticket (\recon-DEV-451 §3 line 50, §4 lines 56, 60, §5 line 87): \u00000799914 (DEV-444, GetForYouRecommendationsQueryHandler.cs), 6fcbd1ff (DEV-448, SimilarityScorer.cs), and \b25ecb16 (DEV-333, ProcessEnrichmentCommandHandler.cs, ImportLibraryCommandHandler.cs) are the three recent src/LamuFlix.Core changes, each pulling in LamuFlix.UnitTests (UnitTests.csproj:38) and LamuFlix.Evaluation (Evaluation.csproj:24) via Get-EligibleTestProjects (scripts/run-mutation.ps1:188-213), with DEV-444 covering the heavy recommendation handler path (harness.yml:33-37).\n- Live measurement & verdict stability (\recon-DEV-451 §3 lines 48-49, §6 lines 97-102): because mutation-timing.json (scripts/run-mutation.ps1:1276-1285) is runtime-only in %TEMP%\\stryker\\... and no Core receipts exist in the repo (specs/DEV-382/receipts/{Api,Infrastructure} only), Build must run matched baseline and candidate measurements holding concurrency (\run-mutation.ps1:643-653) and \u0007dditional-timeout: 90000 (stryker-config.json, ADR 0016 line 111) constant and recording timeout/verdict distribution (specs/DEV-435/CONCLUSIONS.md:9-11).\r\n"
```

---

## Q3 — Measurement-gated lever selection

**Keel question:** Q3/12: I recommend a measurement-gated lever decision, not committing now to coverage-analysis perTest. Keep mtp/off as control; evaluate per-test coverage only as a bounded candidate because recon section 6 records the prior 42-minute, 45-restart, survivor-to-timeout failure and all slower. DEV-435 globs/filtering are baseline; since remains forbidden; concurrency never exceeds half cores; no policy or timeout reduction, test-project omission or threshold relaxation. Build first measures Q2 baseline, then proposes a concrete lever outside DEV-435 with matched evidence of speed and verdict integrity; Patron must ratify the selected lever and any ADR0016 amendment before Phase5, failing closed if no lever succeeds. Unsupported candidates such as a runner or mutator-mode change require targeted recon and a further Patron decision, not silent selection. ACCEPT or CHANGE this experimental selection contract? Cost: lever remains conditional on measured evidence, and ticket cannot close on timing-only gains that corrupt verdicts. Cite your basis; report back to Keel.

**Patron answer:** ACCEPT (measurement-gated lever contract; mtp/off control; Patron ratification + ADR 0016 amendment before Phase 5; fail closed if speed or verdict integrity fails).

**Rationale and implications:** Ticket scope items 1–4 and recon §6:95–102, §7:117 require measured selection beyond DEV-435, preserve integrity safeguards, and require an explicit ruling/amendment before adoption. Unsupported alternatives require recon. No lever is yet authorized for adoption.

**Full Patron Q3 receipt (JSON-escaped):**

```json
"[from Patron] DEV-451 Q3 Ruling: ACCEPT — adopt the measurement-gated lever selection contract with \test-runner: mtp and coverage-analysis: off as control, treating perTest coverage (or any other non-DEV-435 candidate) strictly as a bounded experiment that requires matched Q2 baseline/candidate speed and verdict-integrity evidence, targeted recon for any unreconnoitered runner/mutator-mode candidate, and explicit Patron ratification (plus ADR 0016 amendment if applicable) before Phase 5, failing closed if no lever meets both bars.\n- Ticket Scope items 1–4 (\recon-DEV-451 §2 lines 34-37): mandate measuring first, selecting a lever outside the exhausted DEV-435 set, keeping concurrency <= floor(ProcessorCount/2) (scripts/run-mutation.ps1:643-653), keeping gate rules (\threshold: 80, maxKillShare: 50, maxKillFileSpan: 15, killFileSpanExempt, \u0007dditional-timeout: 90000) and eligible test projects (scripts/run-mutation.ps1:188-213) intact, and securing an ADR amendment or ruling before Phase 5 for any ADR 0016 departure.\n- ADR 0016 & DEV-435 failure history (\recon-DEV-451 §6 lines 95-102): docs/adr/0016-stryker-mtp-runner.md:19-31,105,111 measured coverage-analysis: perTest on MTP (E6) at 42 minutes with 45 test-server restarts and survivor-to-timeout inflation (and coverage-analysis: all slower still), while specs/DEV-435/CONCLUSIONS.md:9-11 confirmed changed-file mutate globs + direct-reference test-project filtering are already baseline (scripts/run-mutation.ps1:7-21,172-196), Stryker --since is barred (lamuflix-team-charter:66-68), and high concurrency corrupts verdicts via timeouts.\n- Governance & fail-closed gate (\recon-DEV-451 §6 line 100, §7 line 117): no candidate lever may be adopted silently or on wall-time reduction alone if it converts survivors to timeouts or alters verdicts; any candidate outside current recon requires targeted recon and explicit Patron ratification (with ADR 0016 amendment where \test-runner/coverage-analysis changes) before Phase 5, or the ticket blocks/fails closed.\r\n"
```

---

## Q4 — Track the CRLF span defect separately

**Keel question:** Q4/12: Ticket item 6 explicitly permits either src/**/*.cs LF normalization here or splitting the CRLF span gap into its own ticket. I recommend splitting the known PR297/MmrSelector CRLF span gap into a separate tracked follow-up, keeping .gitattributes and production source unchanged in DEV-451 so timing comparisons do not conflate line-ending churn with the lever. Rigger should check existing tickets then fold into an existing matching issue or file the broken-behavior follow-up, with verified id linked before handoff. Cost: this known scope-checking defect remains open under a named ticket; it must not be represented as fixed by the new diff-only config test. ACCEPT or CHANGE with cited basis? Report back to Keel.

**Patron answer:** ACCEPT (split PR #297 / MmrSelector.cs CRLF span gap into a Rigger-verified follow-up ticket; keep .gitattributes and src/**/*.cs unchanged in DEV-451).

**Rationale and implications:** Ticket item 6 explicitly permits this split (recon §2:39, §7:117). Keep measurements isolated from line-ending changes. The diff-only generated-config test does not fix the CRLF span bug. Rigger must check existing tickets and return a verified linked id before handoff; no owner checkbox is required.

**Full Patron Q4 receipt (JSON-escaped):**

```json
"[from Patron] DEV-451 Q4 Ruling: ACCEPT — exercise the second option in Ticket Scope item 6 by splitting the PR #297 / MmrSelector.cs CRLF span gap into a dedicated tracked YouTrack follow-up ticket (via Rigger checking open tickets first and linking the verified ticket ID in specs/DEV-451/ before handoff), leaving .gitattributes and src/**/*.cs untouched in DEV-451 and explicitly distinguishing the follow-up defect from the new diff-only mutate config test.\n- Ticket Scope item 6 & §2.3 authority (\recon-DEV-451 §2 line 39, §7 line 117): explicitly authorizes either normalizing src/**/*.cs to LF in .gitattributes here or splitting the CRLF span gap (MmrSelector.cs) into its own ticket; selecting the split option is within ticket scope and requires no owner checkbox.\n- Measurement isolation (\recon-DEV-451 §2 line 34, §6 line 104): .gitattributes currently has no *.cs rule and src/LamuFlix.Core/Features/Recommendations/MmrSelector.cs is checked out with CRLF line terminators; keeping .gitattributes and src/**/*.cs unchanged prevents repo-wide src/**/*.cs line-ending churn from contaminating the matched Q2/Q3 before/after diff-scoped mutation runs.\n- Honest test & defect tracking (\recon-DEV-451 §2 lines 38, 44, §5 line 81): the new scripts/Test-RunMutation.ps1 case proves diff-only file selection in the generated mutate list (Scope item 5 / AC 2) and must not represent the CRLF span-offset gap as fixed; Rigger will verify an existing or newly created follow-up ticket ID and record it in the spec artifacts before handoff.\r\n"
```

---

## Q5 — Regression proof, bounded edits and upstream parity

**Keel question:** Q5/12: I recommend extending the existing Contoso fixture in scripts/Test-RunMutation.ps1, adding a changed and an unchanged production .cs file in the same project, and asserting the default invocation generated mutate list equals only the changed file, since.enabled=false, and explicit WholeProject still includes the full target. Exercise the stubbed real invocation as well as dry-run configuration, retain existing classification/scope/share/span/runner/timeout cases, and require the full script suite to pass. Only the three ticket-named scripts may change as needed, plus bounded evidence documents; stryker-config.json is generated and may change only through upstream renderer/install after approved ADR/config decision, never by hand; harness.yml gates.mutation remains unchanged. Any script changes must be mirrored to dotnet-agent-harness packs/dotnet/scripts and merged or verified on origin/main before delivery, with exact SHAs and parity receipts; if absent, block delivery. Run applicable harness verification with honest exit dispositions and real Q2/Q3 mutation acceptance; no production C# or web implementation is in scope. ACCEPT or CHANGE this implementation/test/sync plan? Cost: upstream merge dependency and real measured gates cannot be replaced by stub success. Cite basis and report back to Keel.

**Patron answer:** ACCEPT (extend Contoso fixture in Test-RunMutation.ps1 for changed vs unchanged sibling .cs; restrict edits to the 3 scripts; require dotnet-agent-harness origin/main parity SHAs + real Q2/Q3 runs).

**Rationale and implications:** Recon §2:38,40,44 and §5:74,79–81,91 support the new diff-only proof, explicit whole-project behavior and upstream dependency. Generated configuration is never hand-edited. Preserve existing cases and report each applicable gate by its actual exit semantics; stub success cannot replace measured mutation acceptance.

**Full Patron Q5 receipt (JSON-escaped):**

```json
"[from Patron] DEV-451 Q5 Ruling: ACCEPT — extend the Contoso fixture in scripts/Test-RunMutation.ps1 with a changed + unchanged .cs sibling in the same project to assert diff-only mutate selection (since.enabled=false) and explicit -WholeProject full-target selection across dry-run and stubbed real runs, restrict code edits to the three ticket-named scripts (never hand-editing generated stryker-config.json or changing harness.yml gates.mutation), and require verified dotnet-agent-harness origin/main parity SHAs plus honest gate reporting and real Q2/Q3 mutation evidence before delivery.\n- Diff-only & -WholeProject test coverage (\recon-DEV-451 §2 lines 38, 44, §5 lines 74, 79-81): extending scripts/Test-RunMutation.ps1 (Contoso fixture lines 44-56, stub dotnet stryker lines 656-688, cases 10-12 & 11 at 1092-1247) with a changed and unchanged .cs file in the same project directly closes the AC 2 / Scope item 5 gap by proving scripts/run-mutation.ps1:1118-1119 sets since.enabled=false and mutate to only the changed file by default while -WholeProject (822-846) includes the full project, keeping all existing cases green.\n- Change-surface & generated-file discipline (\recon-DEV-451 §2 lines 37, 40, 44, §7 lines 108-117; AGENTS.md Non-negotiables & Configuration): implementation touches only scripts/run-mutation.ps1, scripts/_harness-config.ps1, and scripts/Test-RunMutation.ps1 (plus bounded spec/receipt/ADR docs if ratified under Q3); harness.yml gates.mutation stays unchanged (AC 3), and stryker-config.json is never edited by hand (install.ps1 renders it from harness.yml).\n- Upstream dotnet-agent-harness sync & verification gates (\recon-DEV-451 §2 lines 40, 44, §5 line 91; AGENTS.md Verification commands): because packs/dotnet/scripts lives in the external dotnet-agent-harness repository, any edit to the three scripts must be merged or confirmed on dotnet-agent-harness origin/main with exact commit SHAs and parity receipts before delivery (blocking closure if absent), and all verification gates must report honest exit codes (SKIPPED (scope-empty) on exit 2 when no production C# changed) alongside the real Q2/Q3 mutation acceptance runs.\r\n"
```

---

## Q6 — Loop discipline, ordering and grill closure

**Keel question:** Q6/12 closing decision: I recommend code-review/ship-review closing bar zero unresolved Critical/High/Medium accepted findings, required applicable gates and ticket ACs satisfied; Low items receive explicit disposition. Frozen scope: matched three-Core measurement and DEV-444 under-30-minute acceptance; a measured, Patron-ratified non-DEV-435 lever with unchanged mutation policy and verdict integrity; diff-only/explicit WholeProject proof in existing script tests; upstream three-script parity; and a linked CRLF follow-up. Post-merge whole-Core DEV-409, threshold changes, production C#/web changes and unrelated improvements are excluded; anything else is a follow-up issue, not a finding in this round. Cap review/remediation at two rounds (no extra immutable review for receipt repair); after cap, unresolved blockers halt and return to Bernstein. Order: ownership cleanup/follow-up link, recon-backed artifact drafting and analyze/challenge, owner Gate1/spec merge, Build baseline measurements, candidate experiment, Patron ratification/ADR0016 amendment if applicable, implementation/regression and parity, matched acceptance and before/after PR table, exact-head review/gates. No ADR amendment is drafted now without concrete evidence and a Conductor-assigned ADR number if a new ADR is needed. All Q1-Q5 decisions go in brief.md; Quill drafts spec/plan/tasks, and measurement/lever selection remains a blocking Build task rather than a claimed current result. Do you ACCEPT or CHANGE, and confirm shared understanding sufficient to close this six-question grill? Costs: no delivery if the measured target, integrity, parity or accepted blocking findings remain unresolved. Cite basis and report back to Keel.

**Patron answer:** ACCEPT and confirm shared understanding to close the 6-question grill.

**Rationale and implications:** Ticket scope/ACs in recon §2:34–44; missing runtime evidence §3:48–50; ADR evidence §6:95–100; harness verification discipline. Close grill only: no plan freeze, Gate 1, implementation clearance or measured success is asserted. Blocking review severities are Critical/High/Medium; Low needs disposition; maximum two review/remediation rounds. Full frozen scope and execution order are carried into brief.md.

**Full Patron Q6 receipt (JSON-escaped):**

```json
"[from Patron] DEV-451 Q6 Ruling: ACCEPT & CONFIRM SHARED UNDERSTANDING TO CLOSE GRILL (6/12) — ratify the standing closing bar (zero unresolved Critical/High/Medium accepted findings, explicit Low disposition, all applicable gates and ACs green), the two-round review/remediation cap (halting to Bernstein if unresolved), the frozen DEV-451 scope and exclusions, and the execution order recording Q1–Q6 in specs/DEV-451/CONCLUSIONS.md and specs/DEV-451/brief.md with no ADR amendment drafted until Build produces measured evidence.\n- Frozen scope & exclusions (\recon-DEV-451 §2 lines 34-44, §7 lines 108-117): scope is strictly bounded to Q1–Q5 (restoring dirty harness.yml to HEAD \u000010e0cb6; matched baseline/candidate measurement on DEV-444, DEV-448, DEV-333 with < 30 min acceptance on DEV-444; measurement-gated non-DEV-435 lever ratified by Patron before Phase 5 with unchanged gates.mutation and verdict integrity; Rigger-verified CRLF span follow-up ticket; Test-RunMutation.ps1 diff-only + -WholeProject Contoso fixture case; and verified dotnet-agent-harness origin/main parity for the three scripts), excluding post-merge whole-Core DEV-409, threshold edits, and production C#/web changes.\n- Standing closing bar & remediation cap (AGENTS.md Verification commands & Non-negotiables; \recon-DEV-451 §2 line 44): ship-review requires zero unresolved accepted Critical/High/Medium findings, explicit disposition on Low items, honest gate verdicts (SKIPPED (scope-empty) on exit 2 where no production C# changed), and at most 2 review/remediation rounds (no extra immutable review round for receipt repair) before escalating unresolved blockers to Bernstein.\n- Sequencing & ADR discipline (\recon-DEV-451 §3 lines 48-50, §6 lines 95-100): Keel closes CONCLUSIONS.md and \brief.md from Q1–Q6 so Quill drafts spec.md/plan.md/\tasks.md with live baseline measurement and candidate lever selection explicitly sequenced as blocking Build tasks after Gate 1, and no ADR 0016 amendment or new Conductor-numbered ADR is drafted until concrete Build measurements justify it.\r\n"
```

---

## Operational status after Q6

Six-question grill closed; brief.md written and read back. Rigger was asked to verify/link the Q4 CRLF follow-up and report Q1 policy cleanup. The verified follow-up ID remains pending; displayed creation dry-run exits 2 on a pre-existing YouTrack Order-sequence validation error. Report the handoff dependency to Bernstein as blocked; do not claim ticket creation, cleanup verification, plan freeze or Gate 1 clearance. This is an operational dependency, not a seventh decision.


## Plan challenge round 1 — Keel request and Patron ruling (2026-11-04)

Keel asked Patron to ACCEPT or CHANGE each of seven grouped recommendations: experimental feasibility and rejected modes; integrity/single sample; detached historical replay; edit boundary/ADR applicability; Rigger all-three-script parity; preserve Q6 operational ordering; verification/hygiene and scratch deletion. The complete operative request is represented by numbered decisions 1–7 in brief.md Plan challenge round1; no proposed lever was requested or approved. All 23 source findings and verified locations are recorded in adjudication-DEV-451.

**Full supplied Patron receipt (verbatim):**

[from Patron] DEV-451 Plan-Challenge Ruling: ACCEPT all 7 recommendations (1: ACCEPT; 2: ACCEPT; 3: ACCEPT; 4: ACCEPT; 5: ACCEPT; 6: ACCEPT; 7: ACCEPT; scratch files specs/DEV-451/_quill-ask-keel-fix1.txt and _quill-ask-keel-fix2.txt: delete) — ratify the bounded pre-ratification scratch experiment and known-rejected coverage guard, per-mutant status-matrix/timeout/concurrency/DEV-444 integrity and single-sample acceptance rules, operator-provisioned detached historical replay, frozen gates.mutation/renderer stop rule and in-place ADR 0016 amendment applicability, Rigger-owned 3-script upstream parity, standing Q6 Phase-0 ordering, and Low hygiene dispositions.
- [1, 2, 3 — ACCEPT (Sentry F1-F7; Compass M1-M2: Experiment, Integrity & Replay Mechanics)]: (1) Do not retry ADR 0016-rejected coverage-analysis: perTest or all without targeted recon plus explicit Patron experimental authorization (docs/adr/0016-stryker-mtp-runner.md:19-31,88-105; recon-DEV-451 §6:95-102; CONCLUSIONS.md Q3:42-54); T004 must specify a feasible reversible non-shipping scratch experiment using unchanged policy, gate-compatible mutation-timing.json (scripts/run-mutation.ps1:1276-1285), and -EvaluateReport (scripts/run-mutation.ps1:655-713) receipts before T005, keeping tracked shipping scripts untouched until T010/T011, never shipping a GATE NOT WIRED (scripts/run-mutation.ps1:973-978) relaxation, and stopping as needs recon before T005 if the mechanism cannot be specified from recon. (2) Require a per-mutant matched baseline-vs-candidate status matrix keyed by stable source/mutation identity in T005/T012, rejecting any positive Survived-to-Timeout or Killed-to-Timeout conversion or unexplained timeout growth (docs/adr/0016-stryker-mtp-runner.md:35-39,111-114; specs/DEV-435/CONCLUSIONS.md:9-11; scripts/run-mutation.ps1:400-404,981-984); pin and reuse the actual concurrency integer (<= floor(ProcessorCount/2), scripts/run-mutation.ps1:637-653,1113) and runtime additional-timeout (stryker-config.json, scripts/run-mutation.ps1:985-1005,1112) under quiet-machine load control; require kill-share (maxKillShare: 50) and the per-mutant status matrix as primary integrity signals on DEV-444 where harness.yml:33-37 (recon-DEV-451 §3:50, §5:87) exempts the 4 recommendation-handler test classes from maxKillFileSpan; and require T013 to validate the single T012 DEV-444 sample without cherry-picking reruns (invalidated runs stay archived and replacement requires a documented reason plus Patron ruling). (3) T001 must specify operator-provisioned (Bernstein/Rigger) detached replay at each historical SHA (00799914 DEV-444, 6fcbd1ff DEV-448, b25ecb16 DEV-333; recon-DEV-451 §3:48-50, §4:54-60) with frozen comparison base, code/test hashes, mutate list, and Get-EligibleTestProjects (scripts/run-mutation.ps1:188-213) list, overlaying only versioned gate/tool/config support recorded in recon-DEV-451 §3:49 (scripts/new-mutation-receipt.ps1:36-42), holding all except the candidate constant, forbidding invented checkout commands or out-of-worktree writes by Build (AGENTS.md §2.3 & guard.ps1), and routing missing mechanism facts as needs recon.
- [4, 5, 6 — ACCEPT (Ledger H1-H2, M1-M2, M4; Compass M3: Governance, Boundaries, Upstream Parity & Ordering)]: (4) gates.mutation in harness.yml is completely frozen (Ticket AC3 & Scope item 4, recon-DEV-451 §1:26, §2:37,44; CONCLUSIONS.md Q1:16-30); because stryker-config.json is rendered from harness.yml by install.ps1 (AGENTS.md Non-negotiables & Configuration), any candidate lever needing unnamed renderer/install edits or harness.yml keys must STOP before T006 ratification for a cited Patron ruling under Charter §2.3 #6 (or §2.3(a) owner checkbox if contradicting AC3), and any departure from docs/adr/0016-stryker-mtp-runner.md Decision items (lines 24-31) or DEV-410 safeguards (lines 47-55,111; recon-DEV-451 §6:95-100) deterministically requires an in-place amendment to docs/adr/0016-stryker-mtp-runner.md at T010 before T011 (the no-amendment path applies only when zero ADR 0016 Decision/DEV-410 safeguard departure exists; amend existing ADR 0016 without a new number, as only a brand-new superseding ADR uses a Conductor-assigned number). (5) Bernstein arranges Rigger's authorized external dotnet-agent-harness session (packs/dotnet/scripts; recon-DEV-451 §2:40,44, §5:91; CONCLUSIONS.md Q5:74-86) with no guard.ps1 workaround by Build, and FR-008/T014 must record confirmed-or-merged origin/main SHA parity receipts for all three ticket-named scripts (scripts/run-mutation.ps1, scripts/_harness-config.ps1, scripts/Test-RunMutation.ps1), changed or unchanged, before awaiting-merge (Ticket AC5). (6) DROP Ledger M4's request to unblock Contoso fixture tasks (T007-T009) from T022, preserving the standing Q4/Q6 Phase-0 ordering and Handoff gate requiring the verified CRLF follow-up link before handoff/implementation (CONCLUSIONS.md Q4:58-70, Q6:90-108; brief.md:14,16,62,74,82; AGENTS.md §2.3).
- [7 — ACCEPT (Ledger M3, L1-L4; Compass L1-L3: Low Hygiene, Property Gate, Citations & External Surface)]: Delete disposable scratch ask files specs/DEV-451/_quill-ask-keel-fix1.txt and specs/DEV-451/_quill-ask-keel-fix2.txt (one-line disposition: delete, per AGENTS.md §2.3 disposable-files rule) while retaining intended deliverables specs/DEV-451/DRAFTING_RECEIPT.md and specs/DEV-451/FOLLOW-UP-TICKETS.md (Ledger L2); explicitly list ./scripts/run-property-tests.ps1 in plan.md/T018 with actual repo exit semantics (exit 0 pass, exit 1 fail, exit 2 non-blocking SKIPPED (scope-empty) or SKIP opt-out, never PASS or mutation-only N/A, per AGENTS.md Verification commands; Ledger M3); cite authoritative config/ticket sources (stryker-config.json / ADR 0016:111 for additional-timeout, harness.yml for gates.mutation, Ticket AC1 for < 30 min / 1800s) with T001 recording runtime-read values and no change to the ticket target (Ledger L1); clarify Phase 0/4/5 naming against brief.md:13,66, note that post-Gate-1 T021/T022 receipts record in the Build worktree's copy of specs/DEV-451/CONCLUSIONS.md, cross-reference the constitution's Static-Analysis Gates and Pull Request Quality Gates sections in plan.md's Constitution Check table, and keep [P] explicitly unused in tasks.md while correcting parallel examples so shared replay inputs are never mutated concurrently (Ledger L3, L4; Compass L2, L3); and evaluate compatibility with external docs/skills/CI references (README.md:131,196,212, AGENTS.md:113,125, .agents/skills/architect/SKILL.md:30, .agents/skills/verify/SKILL.md:32,87, .github/workflows/ci.yml:254; recon-DEV-451 §5:89) at lever selection (T004/T006), stopping for a cited Patron ruling under Charter §2.3 #6 if required edits touch unnamed tracked files, with no silent scope expansion (Compass L1).


**Implications:** ACCEPT 1–7; Ledger M4 sequencing change dropped; Low violation claims narrowed as recorded in adjudication. Existing Q1–Q6 stay binding except the explicit clarifications above. Quill alone repairs spec/plan/tasks/receipt/checklist after these decisions. Keel records verified operational receipts in the Build CONCLUSIONS copy. No plan freeze, Gate1 approval, experiment execution or gate result is claimed.


## PLAN FROZEN — plan challenge closure (2026-11-04)

Keel independently read back all eight Quill repair items against this brief, the full Patron ruling, spec.md, plan.md, tasks.md, drafting receipt and checklist. All 23 Sentry/Ledger/Compass findings have cited dispositions in adjudication-DEV-451; zero unresolved accepted Critical/High/Medium findings and every Low disposition explicit. Quill plan-challenge repair round1 sufficed; no second challenge repair round or extra immutable review was needed. The earlier drafting/analyze repair receipts remain historical, not permission to exceed the standing review cap.

Read-only speckit-analyze CLEAN: 12 FR + 7 SC = 19 requirement keys, 23 unique tasks T000–T022, 100 percent planned coverage, zero unmapped tasks, zero remaining ambiguity/duplication/constitution-alignment/ordering findings. Native prerequisite check located the correct FEATURE_DIR; this verdict is the documented read-only cross-artifact analysis, not a fabricated native analyzer exit or an implementation gate pass. No before-analyze/after-analyze hooks registered in the inspected extensions file.

**PLAN FROZEN. Ready for Patron Gate1 review. Gate1 is NOT approved.** This closure supersedes earlier Draft / PLAN NOT FROZEN status sentences in the artifact snapshots and prior handoff-status paragraphs only for plan-challenge status. It does not authorize implementation, adopt a concrete lever, assert a measured speedup, complete any runtime gate, or waive operational prerequisites.

Build still requires ordinary Gate1/spec merge, verified T021 cleanup receipt and T022 linked CRLF issue before T000, fresh pickup recon/reanalysis, supported historical replay and scratch mechanics, live measurement, Patron candidate ratification and applicable ADR checkpoint, real native-gate acceptance, required gates and authorized external all-three-script parity. The task note reports Q1 cleanup done, but no additional runtime/cleanup evidence is invented by this plan freeze; verified file receipt remains part of T021. T022 follow-up ID remains pending in the Rigger-owned artifact; no tracker repair is authorized here.

Quill round1 completion report `_quill-ask-keel-planfix1.txt` is retained as bounded review evidence for this adjudication. The two obsolete scratch files expressly authorized for deletion are absent. No production code/config edits, tests, commits, PR publication or merge performed by Keel in this ask.

---

## Gate 1 review — Patron ruling (2026-11-04)

`gate1: provisional` — frozen `brief.md`, `spec.md`, `plan.md`, `tasks.md`, and `checklists/requirements.md` verified against `adjudication-DEV-451` and Q1–Q6 + Plan-Challenge 1–7 rulings.
- Read-only `/speckit-analyze` is clean (`brief.md:102`, `CONCLUSIONS.md:130`) and all 23 plan-challenge findings are resolved (`adjudication-DEV-451`; `brief.md:100`).
- `specs/DEV-451/checklists/requirements.md:35-40` has all User-Visible Outcome items ticked and `specs/DEV-451/spec.md:38-40` states `Nothing visible` with tooling-only rationale.
- Zero structural owner checkboxes (`spec.md:8-10`); operational prerequisites T021 and T022 remain tracked Phase 0 gates before Build T000 (`tasks.md:29-35`).


---

## Phase B intake reanalysis and pickup receipts — 2026-11-04 (Keel)

### Authority and Gate 1 receipt

Assigned Build root: `F:/Dev/LamuFlix.worktrees/DEV-451`, branch `feature/DEV-451` (branch read-back matches). Facts below come from the current Phase B `recon-DEV-451` note and `DEV-451` task note; historical Phase A recon citations above remain historical and must not be read as line references into the replacement Phase B note.

`DEV-451` note lines 3–6 records spec PR #321 merged at `1617165d`. Recon section 2 line 17 repeats that receipt; section 7 line 77 records both Gate 1 provisional commit `fe542c75` and spec merge `1617165d` among the seven landed commits. Accept this as the ordinary Gate 1/spec-merge pickup receipt under the existing no-owner-checkbox contract (spec Owner question; prior Patron provisional ruling). The provisional ruling alone was not final implementation clearance; the recorded spec merge satisfies that separate prerequisite. No additional owner answer or waiver is inferred. PLAN FROZEN remains as recorded above; Phase A Draft/Gate1-not-approved headers are historical snapshots, superseded for intake status by this receipt. This does not waive Phase 0.

### T021 — complete: policy intact, no cleanup write needed

Accept the fresh recon section 1 lines 7–11 and section 7 lines 79–81 as the verified pickup cleanup receipt: HEAD and origin/main both `2809dd0b1f77abc471a5a6606a351b04971c7aa9`, branch `feature/DEV-451`, worktree clean before these documentation edits. `harness.yml` has zero drift from Phase A base `010e0cb63a52e3689d9f246c4179e9bb30959738`; `LamuFlix.Tests.Common` exclusion is present at HEAD line 51 and at the base. The dirty removal is absent from this Build worktree. T021 is satisfied by demonstrated intact state; Keel does not restore or edit policy and does not claim an unobserved restoration command. No Rigger policy action remains for T021. Final PR policy proof at T015 is still required.

### T022 — blocked operational; Rigger action required

`FOLLOW-UP-TICKETS.md` still records no verified CRLF follow-up ID and the recorded Create dry-run exits 2 because the YouTrack Order sequence is not contiguous. Neither current intake note supplies a successful creation/link receipt. Keep T022 unchecked. Bernstein must coordinate Rigger to resolve the recorded prerequisite through authorized tracker operations, create or identify and verify the matching PR #297 / `MmrSelector.cs` CRLF span issue, and record its linked ID in the Rigger-owned follow-up artifact. This is a request for the missing receipt, not authorization for a broader tracker repair by Keel. No new tracker query, creation success, or current Order measurement is claimed. The current status of any repair beyond the supplied artifact is unknown.

### Read-only cross-artifact analysis against landed main

Inputs read: brief.md, spec.md, plan.md, tasks.md, CONCLUSIONS.md, FOLLOW-UP-TICKETS.md, current Phase B recon/task notes, constitution and extensions configuration. No before_analyze/after_analyze hook is registered. The prescribed prerequisite command `.specify/scripts/powershell/check-prerequisites.ps1 -Json -RequireTasks -IncludeTasks` exited 1 because its numeric branch convention rejects `feature/DEV-451`. Analysis therefore used the explicitly assigned `specs/DEV-451/` paths; this is a documented read-only semantic review, not a native analyzer PASS. Do not rename the assigned branch or change tooling to conceal that limitation.

Recon section 7 lines 77–81 establishes seven landed commits since Phase A base, including the spec merge; zero drift on the three ticket-named scripts, harness.yml, stryker-config.json, ADR 0016 and .gitattributes. The nine feature documents were added onto main. Adjacent DEV-452 drift changes `.github/workflows/ci.yml` and `scripts/ci-stryker-shard.sh`; these remain outside DEV-451 edit scope. Existing T004/T006 consumer compatibility and unnamed-file STOP rules cover that drift; carry the current section 4 lines 50–54 / section 7 line 80 references into execution. No task scope or ordering amendment is warranted. No lever, replay provisioning, measurements, or supported scratch mechanism is inferred from absence of scope drift.

| Requirement key | Planned task coverage |
|---|---|
| FR-001 | T001–T003, T005, T012 |
| FR-002 | T012–T013 |
| FR-003 | T000, T004–T006, T010–T011 |
| FR-004 | T021, T001, T011, T015 |
| FR-005 | T008, T011 |
| FR-006 | T007–T009, T019 |
| FR-007 | T004, T006, T010–T011, T017 |
| FR-008 | T014 |
| FR-009 | T022, T008, T015 |
| FR-010 | T012, T016 |
| FR-011 | T003, T005, T012–T013 |
| FR-012 | T018–T020 |
| SC-001 | T001–T003, T012, T016 |
| SC-002 | T012–T013 |
| SC-003 | T007–T009, T019 |
| SC-004 | T015 |
| SC-005 | T014 |
| SC-006 | T022, T008, T015 |
| SC-007 | T018–T020 |

Metrics: 12 FR + 7 buildable SC = 19 keys; 23 unique tasks T000–T022; 19/19 planned coverage (100 percent); zero unmapped tasks; zero unresolved semantic ambiguity, duplication, constitution-alignment or ordering findings. Operational receipts are covered by FR-004/FR-009 and pickup by FR-003. No new requirement or architectural ruling is introduced. Status snapshots require the receipt supersession above and the updated tasks header; they do not change the frozen plan. Planned coverage is not completed acceptance evidence.

### T000 disposition and next boundary

The fresh-recon and read-only reanalysis portions are recorded, but T000 remains unchecked because T022 is a mandatory predecessor under Q4/Q6 and plan-challenge decision 6. Build T001–T020, including independent Contoso T007–T009, remain blocked. After Rigger supplies the verified link and T022 is accepted, confirm this pickup evidence still applies to the then-current assigned HEAD before closing T000. Re-recon is required for changed or missing facts; no speculative lever or implementation is authorized by this review.

Only CONCLUSIONS.md and tasks.md are updated for this intake. No script/config/production edits, runtime gates, tracker writes, commit, PR publication or merge are claimed. Required Rigger action: T022 follow-up receipt; T021 needs no further action.
---

## Phase 0 / Phase B pickup finalization — 2026-11-04 (Keel)

This receipt supersedes the earlier T022-blocked and T000-unchecked intake dispositions above. It records receipt completion only; frozen Q1–Q6 and plan-challenge decisions are unchanged.

### T022 — complete: verified CRLF follow-up DEV-460

Accept Rigger’s completion receipt in `DEV-451` note lines 32–36, corroborated by the Rigger-owned `FOLLOW-UP-TICKETS.md` verified follow-up ID **DEV-460**, summary “Fix mutation scope span gap on CRLF Windows checkout (MmrSelector.cs, PR #297)”, parent DEV-100 and size:S. The Conductor’s finalization ask also explicitly confirms that Rigger verified creation and recorded the link. This is the successful receipt previously missing under Q4 / FR-009 / SC-006; no tracker operation or Order-sequence repair is performed or independently claimed by Keel. The prior dry-run blocker is superseded for T022 by Rigger’s verified result, not relabeled as success. DEV-460 remains separate delivery: `.gitattributes` and `src/**/*.cs` stay outside DEV-451 edits, and Contoso must not claim the span defect fixed.

### T021 and T000 — complete: pickup evidence remains applicable

T021 remains satisfied by recon-DEV-451 section 1 lines 7–11 and section 7 lines 79–81: Tests.Common exclusion intact, zero policy drift, no policy restoration write needed. Current assigned branch read-back is `feature/DEV-451` and HEAD remains `2809dd0b1f77abc471a5a6606a351b04971c7aa9`, matching recon. Current status shows only the expected documentation edits to CONCLUSIONS.md, FOLLOW-UP-TICKETS.md and tasks.md; the earlier clean-worktree fact describes intake before these edits, not the current working tree.

With T022 now accepted, the ordinary Gate 1/spec-merge receipt (PR #321 at `1617165d`) and both Phase 0 predecessors are satisfied. The recon section 7 zero-drift evidence and read-only cross-artifact review above remain applicable to this unchanged pickup HEAD. Rechecked spec/plan/tasks against frozen brief and recorded decisions: 12 FR + 7 SC = 19/19 requirement keys covered by 23 tasks; no new unresolved coverage, semantic, constitution or ordering finding; no drift-driven plan/task-scope amendment is required. Existing T004/T006 checks retain responsibility for adjacent DEV-452 CI caller compatibility (recon section 7 line 80). Mark T021, T022 and T000 `[X]`.

The native prerequisite command again rejected `feature/DEV-451` with exit 1 because it expects numeric feature branch names. Explicit assigned feature paths support this documented semantic review; no native analyzer PASS is claimed. No before_analyze/after_analyze hooks are registered.

### Phase 1 disposition

**Phase 1 is unblocked to start T001.** Bernstein/Rigger must first provision the three detached historical replay cases and supply supported mechanics, roots and immutable comparison/overlay records required by T001. Recon section 8 lines 86–89 records historical timing/receipts and replay checkouts as absent from the bounded roots; this closure does not assert they now exist or authorize invented replay commands. T002 native baseline runs start only after T001 is satisfied; missing mechanism facts require `needs recon` before execution. T003 evidence validation, later candidate ratification, ADR checkpoint and measured acceptance remain open. Independent Contoso T007–T009 may proceed under the existing post-T000 constraints, without changing frozen replay inputs or concurrent same-file writes.

Only CONCLUSIONS.md and tasks.md are changed by this finalization; Rigger’s FOLLOW-UP-TICKETS.md edit is preserved. No runtime gates, baseline measurement, lever selection, script/config/production edits, tracker writes, commit or merge are claimed.
---

## T001 complete — frozen replay inventory and T002 execution contract (2026-11-04, Keel)

Authority: Conductor assignment; `recon-DEV-451-2` sections 9.1–9.7 (continuation of `recon-DEV-451`); `DEV-451` note lines 44–57; spec FR-001/FR-004/FR-011 and plan Measure first. T001 inventory is complete. This supersedes the earlier unprovisioned-replay disposition. No real baseline has run: recon reports dry-runs only, with zero mutants/tests executed. T002 and T003 remain unchecked.

Freeze each pin's first-parent commit diff, using the full immutable base SHAs below rather than the gate default. Recon 9.1/9.2–9.4 verifies `-BaseRef HEAD^` dry-run exit 0 for these same bases. The default merge-base is HEAD and exits 2 scope-empty; it cannot supply this baseline. These are historical **pinned commit-diff cases**, not full-ticket branch ranges: DEV-448 excludes its later integration-test-only commit; DEV-333 is the final refactor commit at the named pin, not the full eight-commit phase range. Do not silently widen or substitute the scope for later comparisons or DEV-444 acceptance.

| Case / replay root under F:/Dev/LamuFlix.worktrees/ | Frozen HEAD | Frozen comparison base |
|---|---|---|
| DEV-444 / DEV-451-replay-DEV-444 | `0079991451599e408b6ff37de25a6e3e731b2c6a` | `1e95037eda58502e0f7088a31f4502798e51d3a0` |
| DEV-448 / DEV-451-replay-DEV-448 | `6fcbd1ffdb3805b46937534bac48a37a68ebd62b` | `f8bb21cb38e3b9105ee1bc0500966903a5fad40a` |
| DEV-333 / DEV-451-replay-DEV-333 | `b25ecb1614a0c82771f254b700bb53dff0a25bf0` | `f00e01bacb63c083349b3a46425189e31e3564c2` |

| Case | Frozen src tree | Frozen tests tree | Core mutate entries (relative to src/LamuFlix.Core/) |
|---|---|---|---|
| DEV-444 | `ed55006861f0e8ceb3b0d2dbe9cdb9c9f337cbd6` | `ab0ba312417f41941c4fb67705ea9ae8ea09d7ef` | `Features/Recommendations/GetForYouRecommendationsQueryHandler.cs` |
| DEV-448 | `f0f2db986c790097851862d8f23c9a5dba64bab7` | `eb2126572aa68172a2b8273a6e857e60cca24cd9` | `Features/Recommendations/SimilarityScorer.cs` |
| DEV-333 | `c526d167d6fdc7cbada72ef6c8f9969f86a5bc9c` | `82fbe561670ea1062062428391095cd1d96f6daa` | `Features/Enrichment/ProcessEnrichmentCommandHandler.cs`; `Features/Import/ImportLibraryCommandHandler.cs` |

Full tree identities are recorded in recon 9.5 lines 94–95. DEV-333 additionally changes the six Infrastructure files enumerated in recon 9.4 lines 70–75; all six remain policy-excluded **NOT APPLICABLE**, never PASS. Do not use `-Project` to hide that classification. Both Core suites remain eligible in every case: `tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj` and `tests/LamuFlix.UnitTests/LamuFlix.UnitTests.csproj`.

Shared control: machine ALIEN, PowerShell 7.6.9, 16 processors, actual generated concurrency **8** (half-core ceiling 8; absent config concurrency key), `additional-timeout` **90000**, `test-runner: mtp`, `coverage-analysis: off`, `since.enabled=false`, Stryker 5.0.0. Freeze these values for baseline/candidate; verify actual generated values before measuring, stop on drift, and serialize all real runs on the quiet machine. No concurrency config edit or unsupported command-line concurrency override is authorized.

### Frozen support identities and DEV-333 overlay prerequisite

Recon 9.1 lines 13–19 records byte-identical support at all pins and DEV-451 intake `2809dd0b1f77abc471a5a6606a351b04971c7aa9` (SHA-256):

| Support file | SHA-256 |
|---|---|
| scripts/run-mutation.ps1 | `2a0a8ff8a80e87fe326bb0537ae83c36f82e5587068f67656f6dbe5f06e052be` |
| scripts/_harness-config.ps1 | `8b669b6f309251bf5adf6747a58deed8c567d4c3e24c1aa7014d0c75f49615ec` |
| scripts/_gate-common.ps1 | `b4906af9e753329680dec85bfe9e65f93ef5538f00c87f73ceafc425822a85fd` |
| stryker-config.json | `71485fb909349b0c3162c33215c67270e9143e86131ac44e328b1dcf04da4b8b` |
| .config/dotnet-tools.json | `de20c1911406a8132d1f8509853232988cda75507a17dc66290980727594c67b` |

No copy is necessary for those identical files. Recon 9.4 line 87 separately identifies DEV-333 historical `harness.yml` SHA-256 `72218cdd232a47c97b393aab39d648bacc45cae22c5f006f16971aa9f9d239bc`; the sole difference from the current policy is the absent four-line Tests.Common exclusion block. DEV-444/448 and assigned DEV-451 policy SHA-256 is `ee866efadcb2bbb82206a677ff7b47f2537fd7d6802b9243fad0d45ba7514f09`.

Before Gauge starts T002, Bernstein must arrange **Rigger's replay-only support overlay** and receipt for DEV-333. This implements the Conductor's explicit overlay instruction and preserves spec FR-004's four-exclusion control; it does not change the assigned DEV-451 harness.yml or grant a shipping-policy edit. Rigger may copy the verified intact DEV-451 support policy into DEV-333 only, after checking both source/destination hashes against the values above:

```powershell
Copy-Item -LiteralPath 'F:/Dev/LamuFlix.worktrees/DEV-451/harness.yml' -Destination 'F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-333/harness.yml'
Get-FileHash -Algorithm SHA256 -LiteralPath 'F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-333/harness.yml'
```

Rigger must record before/after hashes, provenance, the exact exclusion-only delta, and unchanged pinned HEAD/src/tests identities. Expected replay dirtiness is only this support file. Retain this exact overlay for all DEV-333 baseline/candidate runs; disclose the overlay rather than describing DEV-333 as an unmodified historical checkout. No generated config, code or tests are edited. The inventory is complete, but this overlay is **specified, not yet verified as applied**; T002 execution waits for the receipt. Missing/different facts require `needs recon`, not a guessed repair. Recon's full-phase-range exit 1 is historical evidence, not a baseline verdict for the selected first-parent scope.

### T002 — exact Gauge execution and evidence handoff

Bernstein assigns Gauge the three named replay roots plus the bounded DEV-451 evidence destination. Gauge must not infer permission to write elsewhere from this document. After Rigger's overlay receipt, check detached HEAD, frozen src/tests and support identities, then use the corresponding command below with `-DryRun` appended once per case as preflight, replacing its `-OutputRoot` with a separate unique TEMP preflight root (never the real attempt root). Require expected mutate list, both Core suites, concurrency 8, timeout 90000, control runner/coverage, since disabled, and DEV-333 Infrastructure N/A. A dry-run exit 0 is preflight only. Stop before real measurement on mismatch. Restore only existing pinned tools if needed; no tool/version upgrades.

Run the following **one at a time**, from each specified root. The output directory for each attempt must be absent before invocation; if it exists, preserve it and choose a new attempt number, recording why. Native output stays in TEMP as plan.md requires. These commands deliberately omit `-DryRun`, `-WholeProject` and `-Project`:

```powershell
Set-Location 'F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-444'
pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef 1e95037eda58502e0f7088a31f4502798e51d3a0 -OutputRoot (Join-Path $env:TEMP 'DEV-451-baseline-DEV-444-attempt01')
$baselineExit444 = $LASTEXITCODE

Set-Location 'F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-448'
pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef f8bb21cb38e3b9105ee1bc0500966903a5fad40a -OutputRoot (Join-Path $env:TEMP 'DEV-451-baseline-DEV-448-attempt01')
$baselineExit448 = $LASTEXITCODE

Set-Location 'F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-333'
pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef f00e01bacb63c083349b3a46425189e31e3564c2 -OutputRoot (Join-Path $env:TEMP 'DEV-451-baseline-DEV-333-attempt01')
$baselineExit333 = $LASTEXITCODE
```

Capture each command's complete stdout/stderr, exit and observed wall time, and archive its full output root immediately after that run (including failed/invalid attempts) under `F:/Dev/LamuFlix.worktrees/DEV-451/specs/DEV-451/evidence/baseline/<DEV-id>/attempt01/`. Preserve native `mutation-timing.json`, generated configs and full Stryker JSON reports without rewriting them. Add a receipt in that destination carrying command/root/base/head/src/tests/support hashes, policy-overlay provenance, machine/runtime/load conditions, actual concurrency/timeout/runner, both eligible suites, start/end and wall time, per-project mutant counts, tested/score/verdict, timeout totals/distribution and report paths. If the gate fails before producing timing/report artifacts, record them as absent with the failure; never synthesize native artifacts. Retain stable source-location/mutator/replacement fields for T003 and later matched status matrices; numeric mutant IDs alone do not establish identity.

Report exits honestly: 0 native gate pass; 1 failure; 2 scope-empty SKIPPED or policy N/A as actually printed, never a measured pass. A valid failing baseline is evidence and must not be erased or rerun to manufacture a pass; Bernstein/Patron receives the actual result before deciding next steps. Interruptions/invalidations remain archived. No lever experiment starts before T003 validates all three baseline evidence sets. T002 does not claim under-30-minute acceptance; T012/T013 retain that separate bar.

**Readiness:** T001 inventory complete and execution commands specified. Ready for Bernstein to obtain the DEV-333 replay-overlay receipt and assign Gauge; real T002 starts only after that receipt and successful preflight. No baseline, timing success, candidate selection, runtime gate, script/config write or commit is claimed by Keel in this ask. Existing FOLLOW-UP-TICKETS.md and earlier documentation edits are preserved.

---

## T003 baseline evidence validation — blocked (2026-11-04, Keel)

Authority: Conductor T003 assignment; spec US1 / FR-001 / FR-011; tasks T002–T006; frozen T001/T002 execution contract above. Read DEV-451 and both recon notes, spec/plan/tasks/CONCLUSIONS, Gauge report `C:/Users/lamun/AppData/Local/Temp/DEV-451-T002-gauge-report.txt`, and the assigned baseline archives. This is validation of supplied artifacts, not a new replay measurement or independent recon. Branch read-back matches `feature/DEV-451`; existing documentation and evidence changes are preserved.

### Accepted and rejected receipts

Rigger overlay receipt in DEV-451 lines 64–69 satisfies the earlier DEV-333 prerequisite: policy hash changes from `72218cdd232a47c97b393aab39d648bacc45cae22c5f006f16971aa9f9d239bc` to `ee866efadcb2bbb82206a677ff7b47f2537fd7d6802b9243fad0d45ba7514f09`, adds only Tests.Common exclusion, retains pinned HEAD, and records only harness.yml dirty. This is a replay-only support overlay, not a shipping policy change.

All archive links below are relative to this document. Native timing JSON was read for DEV-444 and DEV-333; generated configs and full JSON reports were read from all four archived attempts. Each config retains concurrency 8, additional-timeout 90000, mtp/off, since disabled, both Core suites and the T001 mutate list. Receipts record the frozen bases/heads, support hashes and source/test aggregate hashes; recon 9.5 remains authority for frozen Git tree identities. Do not equate the receipt aggregate SHA-256 values with Git tree hashes or claim fresh post-run tree verification by Keel.

| Case | Durable evidence | Actual disposition |
|---|---|---|
| DEV-444 attempt01 | [receipt](evidence/baseline/DEV-444/attempt01/receipt.md), [native timing](evidence/baseline/DEV-444/attempt01/output/mutation-timing.json), console/config/full reports in same archive | Valid failing baseline retained: wrapper exit 1, 1088.7 s native (1089.190 s observed), 68 tested, 58.82%; Killed 36 / Survived 28 / Timeout 4 / NoCoverage 0 / Ignored 23 / CompileError 7. No rerun to manufacture a pass. |
| DEV-448 attempt01 | [receipt](evidence/baseline/DEV-448/attempt01/receipt.md), console/config/full reports in same archive | Incomplete and contended baseline: Stryker exit 0 and 89.14%, 221 tested (Killed 197 / Survived 24 / Timeout 0); **wrapper exit not captured; native mutation-timing.json absent**. 2959.8 s is Stryker elapsed time, not native gate wall time. |
| DEV-448 attempt02 | [receipt](evidence/baseline/DEV-448/attempt02/receipt.md), console/config/full reports in same archive | Invalid serial-control replacement: same tested/status totals and score; Stryker elapsed 3205.5 s; wrapper exit not captured and native timing absent. Started 11:33:21 while attempt01 ran until 11:36:34. Both attempts are affected by overlap and remain archived, neither establishes a quiet-machine gate baseline. |
| DEV-333 attempt01 | [receipt](evidence/baseline/DEV-333/attempt01/receipt.md), [native timing](evidence/baseline/DEV-333/attempt01/output/mutation-timing.json), console/config/full reports in same archive | Wrapper exit 0 / PASS, 1233.3 s native (1233.826 s observed), 68 tested, 86.76%; Killed 55 / Survived 9 / Timeout 4 / NoCoverage 0 / Ignored 28 / CompileError 8. Infrastructure **N/A**, never PASS. |

The detailed Gauge report and DEV-448 receipts override the abbreviated task-note/assignment wording “exit 0”: that exit belongs to Stryker, not the gate wrapper. No timing is invented, synthesized or substituted. DEV-448 created/skipped console totals and filtered report totals are different populations as explicitly recorded in its receipt; preserve them separately.

DEV-444 console records largest kill share 35.29% against 50%; widest non-exempt span 0, with 32 exempt tests / widest exempt span 1. DEV-333 records share 20.59%, widest non-exempt span 1, no exempt tests, and native PASSED. DEV-444 recommendation exemptions weaken the span signal; its four timeouts and 28 survivors remain the comparison control, not grounds to lower any threshold. Neither baseline under 1800 s substitutes for T012/T013 acceptance.

Full JSON reports retain source text and mutant status, location start/end line/column, mutatorName and replacement (schema 2). These provide the required stable identity ingredients; future T005 must normalize absolute replay prefixes to project-relative paths and key on path + full location + mutatorName + replacement, checking duplicate/ambiguous keys and unmatched populations rather than joining numeric IDs. This validation does not claim a completed candidate matrix or prove every identity unique.

### Task disposition and Phase 2 instructions

**T002 and T003 remain unchecked.** The requested completion marks would assert three comparable native baseline sets that the supplied evidence does not establish. T003 has found a concrete evidence blocker; this is not a policy waiver, native gate PASS, or a failed score relabeled invalid. DEV-444 failure remains valid evidence; DEV-448 is invalid/incomplete for measurement-control reasons independently of its passing Stryker score. T004–T006 remain blocked by Phase 1.

**needs recon: What caused the DEV-448 wrapper exits/native timing to be lost and the overlapping invocations, and what verified single-run capture procedure can produce a serial native-gate baseline on the unchanged frozen case while preserving both existing attempts?** Bernstein should obtain Wisp recon and coordinate Gauge remediation; Keel does not diagnose outside the assigned roots, invent missing facts, launch a rerun or authorize script repairs. Archive any replacement under a new attempt number with invalidation rationale, full wrapper exit, native timing, actual integrity evaluation, machine/load and unchanged identity receipts. Do not synthesize the missing JSON or rerun valid DEV-444 for a passing result. Return the complete evidence for T003 validation before closing T002/T003.

Once Phase 1 is actually closed, T004 must propose one concrete non-DEV-435 lever with cited recon mechanism, three-script implementability and consumer compatibility (including current DEV-452 CI callers). Keep mtp/off control and shipping guards/policy unchanged. Specify the reversible versioned non-shipping scratch path, exact control/candidate commands, durable equivalent timing/receipts and actual -EvaluateReport checks. Rejected perTest/all or unsupported modes require targeted recon plus explicit Patron experimental authorization before execution. Missing mechanism facts require needs recon; unnamed renderer/install/config/consumer edits require the existing cited Patron STOP. T005 then compares all three frozen cases using stable identity matrices, timeout totals and Survived/Killed-to-Timeout rejection; T006 ratification and T010 ADR checkpoint still precede T011 adoption. No lever is selected or experiment authorized by this receipt.

Only CONCLUSIONS.md and tasks.md are updated for this validation; all original attempts and unrelated edits are preserved. No runtime gate, script/config/production write, tracker change, commit or merge is performed.

---

## T003 resumed — recon received; Gauge attempt03 ready (2026-11-04, Keel)

Authority: Conductor resume assignment; `recon-DEV-451-2-2` section 10 (continuation of recon-DEV-451), especially 10.4–10.9; frozen T001/T002 contract. Assigned branch read-back remains `feature/DEV-451`. This receipt supersedes the prior unanswered-needs-recon disposition for preparing a capture attempt; it does not close the baseline evidence gap or select a lever.

### Recon receipt and limits

Accept Wisp section 10.4 lines 40–45: frozen Evaluate-MutationReport on both archived reports completes under StrictMode Latest / ErrorActionPreference Stop in 781 ms and 767 ms, respectively; both yield PASS, score 89.1402714932127, tested 221. Correct changed-file input is `src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs`, not the mutate-relative path. The probe records kill share 33.48% (limit 50%), span within limits, Unattributed 0 and Artifact 0. These are diagnostic function probes, **not native wrapper exits**, and do not repair either missing timing artifact.

Section 10.1/10.2/10.5 locates the last observed wrapper output at run-mutation.ps1:1197, before summary :1218 and native timing :1283. A Stryker-exit-coupled launcher/capture interruption is the best-supported **inference**; exact termination versus hang, stderr, launch/wait mechanics and the second runner decision are not determinable in roots (10.5/10.9). No script defect or repair is established. Wisp rules out slow/failing evaluation on these report bytes; do not claim the probe reproduces the full live wrapper environment.

Section 10.6 refines overlap to wrapper invocation **11:33:19 → 11:36:34, approximately 3m15s**, in the same worktree with shared obj/bin and concurrency 8 each. Both attempts remain invalid for quiet-machine timing, with wrapper exits NOT CAPTURED and native timing absent. Identical verdicts do not make their wall times comparable. Keep both archives byte-identical; retain DEV-444 valid failure and DEV-333 completed baseline without rerunning them.

### Exact Gauge attempt03 handoff

Bernstein assigns **one Gauge runner** exclusive ownership of `F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-448` and the quiet mutation-machine window through archive completion. No other case or runner may start concurrently. Before preflight and again immediately before the real invocation, confirm by machine process query that no other run-mutation.ps1 / dotnet stryker session is active; record the query/result and ownership in the new receipt. Quiet console output is not process completion. If a prior process is active or its state cannot be established, stop and report; do not launch a replacement or terminate it speculatively.

1. Follow recon 10.7 item 2: verify clean detached HEAD `6fcbd1ffdb3805b46937534bac48a37a68ebd62b`, comparison base `f8bb21cb38e3b9105ee1bc0500966903a5fad40a`, and all eight frozen identities against the full attempt01 receipt (src/tests aggregate hashes plus run-mutation, stryker-config, harness, tools, Directory.Packages.props and Directory.Build.props). Preserve T001 support identities too; stop on any drift. No support overlay or code/test/config edit is needed for DEV-448.
2. Use a separate, absent preflight root with the exact command below. Require exit 0, mutate only `Features/Recommendations/SimilarityScorer.cs`, both Evaluation and UnitTests, concurrency 8, timeout 90000, mtp/off and since disabled. Capture stdout/stderr and preflight exit. If preflight02 or any named capture/output/archive destination already exists, preserve it and report the collision before measuring; do not overwrite it or invent an attempt number silently.

```powershell
Set-Location 'F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-448'
pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef f8bb21cb38e3b9105ee1bc0500966903a5fad40a -OutputRoot (Join-Path $env:TEMP 'DEV-451-baseline-DEV-448-preflight02') -DryRun *> (Join-Path $env:TEMP 'DEV-451-DEV-448-preflight02-console.log')
$preflightExit448 = $LASTEXITCODE
```

3. After successful preflight and the fresh seriality check, execute the following in **one persistent PowerShell session**, synchronously. This expresses recon 10.7 items 4–5 directly in the owning shell, avoiding an outer double-quoted -Command string that would expand TEMP/LASTEXITCODE in the wrong shell. No background job, pattern-triggered stop, Stryker-child-only wait or short command timeout. If the execution tool returns a running session, retain it and wait for that same wrapper; never start another invocation. Record start/end UTC and wall time as observed, independently of native duration.

```powershell
Set-Location 'F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-448'
$attemptOutput448 = Join-Path $env:TEMP 'DEV-451-baseline-DEV-448-attempt03'
$attemptConsole448 = Join-Path $env:TEMP 'DEV-451-DEV-448-attempt03-console.log'
$attemptCapture448 = Join-Path $env:TEMP 'DEV-451-DEV-448-attempt03-gauge-timing.json'
$attemptArchive448 = 'F:/Dev/LamuFlix.worktrees/DEV-451/specs/DEV-451/evidence/baseline/DEV-448/attempt03'
foreach ($attemptPath448 in @($attemptOutput448, $attemptConsole448, $attemptCapture448, $attemptArchive448)) {
    if (Test-Path -LiteralPath $attemptPath448) { throw "Preserve existing destination and report collision: $attemptPath448" }
}
$attemptStart448 = [DateTimeOffset]::UtcNow
$attemptClock448 = [System.Diagnostics.Stopwatch]::StartNew()
pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef f8bb21cb38e3b9105ee1bc0500966903a5fad40a -OutputRoot $attemptOutput448 *> $attemptConsole448
$attemptExit448 = $LASTEXITCODE
$attemptClock448.Stop()
$attemptEnd448 = [DateTimeOffset]::UtcNow
[ordered]@{
    start = $attemptStart448.ToString('o')
    end = $attemptEnd448.ToString('o')
    wallMs = $attemptClock448.ElapsedMilliseconds
    exit = $attemptExit448
    consoleLog = $attemptConsole448
    baseRef = 'f8bb21cb38e3b9105ee1bc0500966903a5fad40a'
    outputRoot = $attemptOutput448
    head = '6fcbd1ffdb3805b46937534bac48a37a68ebd62b'
} | ConvertTo-Json | Set-Content -LiteralPath $attemptCapture448 -Encoding utf8
```

The head field records the verified preflight pin, not a substitute for the identity check. This gauge record is external observation; **never write or synthesize output/mutation-timing.json**. If the wrapper or owning capture session fails, archive all available output and report absent fields honestly. Do not patch scripts mid-run or treat Stryker exit 0 as wrapper exit 0.

4. Apply recon 10.7 item 6 before accepting the attempt: console contains `Mutation Testing Summary` and a terminal `Mutation testing: PASSED|FAILED|SKIPPED|NOT APPLICABLE` line; native output-root mutation-timing.json exists and carries generatedUtc, durationSeconds, concurrency 8, threshold 80 and projects; gauge record contains the actual wrapper exit and wall time; full Core JSON report exists and totals match console/native project summary. Confirm unchanged generated scope/settings, both eligible suites, timeout distribution, and actual integrity evaluation. A scope-empty/SKIPPED result does not establish this non-empty baseline. A valid native FAIL remains evidence; do not retry for a passing score.
5. Immediately archive to the exact attempt03 destination: `console.log`, `preflight.log`, `gauge-timing.json`, `receipt.md`, and `output/` with unmodified native timing, generated config and full reports. Include the complete command, preflight/real exits, frozen identities, machine/runtime/load, seriality and ownership receipts, start/end/wall, native duration, counts/tested/score/verdict/timeout totals, stable mutant identity fields and all artifact paths. Explicitly invalidate attempt01/02 for overlap and missing capture, retaining their bytes. If any completion assertion fails, archive the invalid attempt and report the reason to Bernstein; a subsequent attempt requires a separately recorded handoff, not an automatic best-of loop.

**Readiness:** Gauge attempt03 is ready to be assigned under the stated preflight and seriality conditions. The new serial measured number is the DEV-448 baseline regardless of whether it exceeds 1800 s; the under-30-minute acceptance bar remains T012/T013 on DEV-444. T002/T003 stay `[ ]` until complete attempt03 evidence returns for validation. Phase 2 T004–T006 stays blocked; prior conditional T004 instructions remain unchanged. No native baseline, launcher root-cause certainty, script repair, lever experiment, commit or merge is claimed by this receipt. Only CONCLUSIONS.md and tasks.md are changed; existing unrelated edits are preserved.

---

## T003 final validation — Phase 1 complete (2026-11-04, Keel)

Authority: Conductor attempt03 validation assignment; tasks T002/T003; spec FR-001/FR-011; frozen T001/T002 contract and recon-DEV-451-2-2 section 10. This receipt supersedes the earlier blocked/readiness snapshots for baseline completion only. Branch read-back is `feature/DEV-451` in the assigned Build worktree. Inputs are task note DEV-451 lines 96–101, section 10 recon, Gauge report `C:/Users/lamun/AppData/Local/Temp/DEV-451-T002-attempt03-gauge-report.txt`, and the archived artifacts linked below. No replay measurement or launcher diagnosis was performed by Keel.

### DEV-448 attempt03 — accepted

Read back [receipt](evidence/baseline/DEV-448/attempt03/receipt.md), [native timing](evidence/baseline/DEV-448/attempt03/output/mutation-timing.json), [wrapper capture](evidence/baseline/DEV-448/attempt03/gauge-timing.json), preflight/console logs, generated config, full Core JSON report, and ownership-preflight/prelaunch JSON in the same archive.

- Wrapper exit **0**, native verdict **PASSED**, native duration **3385.7 s**, independently captured Gauge wall **3386.169 s**, Core Stryker wall **3383.5 s**. Capture runs from `2026-11-04T16:27:01.8278242+00:00` to `2026-11-04T17:23:27.9992904+00:00`; native `generatedUtc` is `2026-11-04T17:23:27Z`. Console contains both `Mutation Testing Summary` and the terminal verdict. These are complete wrapper artifacts, not inferred from the Stryker child exit.
- Native timing and full report agree: **283** reported mutants; **221** tested; Killed **198**, Survived **23**, Timeout **0**, NoCoverage **0**, Ignored **41**, CompileError **21**; score **89.59276018099548%**, threshold **80**. Keep the Stryker-created population (2556, with pre-report filtering) separate from the 283 report entries.
- Preflight exit 0 is recorded in Gauge's receipt; the archived preflight log confirms scope/settings, not a measured gate pass. Generated config retains only `Features/Recommendations/SimilarityScorer.cs`, both Evaluation and UnitTests, concurrency **8** on 16 logical processors, additional-timeout **90000**, **mtp/off**, since disabled, and unchanged thresholds. Console records actual integrity evaluation: largest kill share **33.48% / 50%**, largest non-exempt file span **1 / 15**, with 77 exempt tests in four classes and widest exempt span 1. No safeguard was relaxed.
- Ownership receipts record Gauge exclusive ownership and empty active-mutation process lists before preflight (`16:25:15.9774354Z`) and immediately before launch (`16:26:43.0991707Z`). Gauge records no concurrent case through archive and zero matching sessions after exit. Accept the supplied seriality evidence; this is not a fresh machine-wide process audit by Keel.
- Receipt pins detached head `6fcbd1ffdb3805b46937534bac48a37a68ebd62b`, first-parent base `f8bb21cb38e3b9105ee1bc0500966903a5fad40a`, clean porcelain, no overlay, machine ALIEN, PowerShell 7.6.9 and SDK 10.0.403. All eight recorded frozen source/test/support hashes match attempt01. These are supplied identity receipts; aggregate SHA-256 values are not Git tree hashes. Native timing provenance/archive hash match is Gauge's recorded receipt, not an independent TEMP hash check by Keel.

The accepted serial number exceeds 1800 s; that does not invalidate a baseline. Attempt03 differs from the rejected attempts by one extra Killed / one fewer Survived mutant (198/23 versus 197/24), with the same 221 tested and zero timeouts. Preserve that observation without attributing it to a lever or claiming a speed/verdict improvement. Only attempt03 is the DEV-448 control for future matched candidate comparisons.

### Retained invalid attempts and completed baseline set

**DEV-448 attempt01 and attempt02 are retained and invalidated**, not deleted or repaired: their wrapper exits are NOT CAPTURED and native timing / Gauge timing are absent; they overlap `11:33:19 → 11:36:34` (approximately 3m15s) in the same worktree. Their Stryker scores and elapsed times cannot substitute for a serial native-gate baseline. Keel made no writes to either archive; Gauge records byte-identical retention. Exact launcher cause remains unproven; successful attempt03 does not retroactively establish it.

| Accepted baseline | Native wall | Tested / score | Actual gate disposition |
|---|---:|---|---|
| [DEV-444 attempt01](evidence/baseline/DEV-444/attempt01/receipt.md) | 1088.7 s | 68 / 58.82% | Valid **FAIL**, wrapper exit 1; retained without rerun; 4 timeouts |
| [DEV-448 attempt03](evidence/baseline/DEV-448/attempt03/receipt.md) | 3385.7 s | 221 / 89.59% | **PASS**, wrapper exit 0; 0 timeouts |
| [DEV-333 attempt01](evidence/baseline/DEV-333/attempt01/receipt.md) | 1233.3 s | 68 / 86.76% | Core **PASS**, wrapper exit 0; Infrastructure **N/A**, never PASS; 4 timeouts |

Earlier DEV-444/DEV-333 validation remains accepted; native timing was read back again. All three baselines are durable live measurements with frozen per-case comparison identities, unchanged control settings and both eligible Core suites; DEV-333 retains its explicitly recorded replay-only Tests.Common overlay. Comparability means matching each candidate to its own frozen baseline, not treating different historical code/test trees as interchangeable. No historical timing was invented or missing artifact synthesized.

Full archived reports carry source path, complete start/end line/column, mutatorName, replacement and status. Artifact validation found **zero duplicate identity groups** using path + full location + mutatorName + replacement in DEV-444's **98**, DEV-448's **283**, and DEV-333's **104** report entries. T005 must still normalize replay-root prefixes, confirm matching source content and populations, reject unmatched/ambiguous identities, and build the full baseline-to-candidate matrix rather than joining numeric IDs. This validation is not a candidate comparison.

### Task closure and next steps

**T002 and T003 are complete.** Phase 1 is closed with one valid failing baseline and two passing Core baselines; this is not an all-gates-PASS claim or ticket acceptance. No lever has been selected, experiment authorized, or shipping script changed.

Bernstein may proceed to **T004**: obtain a concrete non-DEV-435 lever proposal, cited mechanism recon, three-script implementability/consumer compatibility proof and a reversible versioned non-shipping scratch plan with unchanged policy and actual integrity evaluation. Existing mtp/off control, unsupported-mode experimental authorization, unnamed-file STOP rules, T005 stable-identity/timeout comparisons, T006 Patron ratification and T010-before-T011 adoption ordering all remain mandatory. Missing mechanism facts require `needs recon`; Phase 1 closure is not permission to bypass those conditions. T012/T013 retain the separate under-30-minute acceptance bar on DEV-444.

Only this CONCLUSIONS receipt and tasks.md completion/status are changed by Keel. Existing unrelated documentation and all evidence archives are preserved; no script/config/production write, runtime gate, tracker mutation, commit or merge is performed.

---

## T004 proposal review — blocked on mechanism recon and experimental decision (2026-11-04, Keel)

Authority: Conductor T004 assignment; brief Q3/Q5 and plan-challenge decisions 1–4/7; tasks T004–T006. Read the assigned task/recon notes, tasks, brief, this complete conclusions record and ADR 0016; also read linked recon-DEV-451-2 section 9 for frozen support/replay facts. Assigned branch read-back is feature/DEV-451. Existing documentation edits and evidence are preserved. No independent mechanism search, measurement or script edit was performed.

### One concrete proposal, not an authorized experiment

**Candidate for targeted recon: Stryker 5.0.0 MTP with coverage-analysis `perTestInIsolation`, replacing only the control's `off` value in an explicitly versioned experimental gate's generated configuration.** ADR 0016, Amendment: sharded whole-Core run in CI, states that the MTP runner contains `perTestInIsolation`; it does not establish this mode's current implementation, semantics, correctness or performance on these frozen cases. The proposed benefit is a hypothesis to investigate, not a measured or documented claim here. Do not equate this mode with measured `perTest` or infer that its name guarantees isolation or verdict integrity.

This is outside DEV-435's already-baseline changed-file globs/direct-reference filtering. It changes neither `since` nor concurrency. The control remains Stryker 5.0.0, mtp/off, concurrency 8, additional-timeout 90000, both eligible Core suites and the entire unchanged gates.mutation policy. No other candidate is proposed in this receipt.

**Shipping disposition remains GATE NOT WIRED for mtp/non-off**, including this candidate: recon-DEV-451 section 4 line 43 identifies run-mutation.ps1:976. Nothing in ADR 0016's historical mode listing overrides that guard. This is an unsupported shipping-gate mode and requires targeted recon plus explicit Patron experimental authorization under brief plan-challenge decision 1 before any trial. Existing Q3 and Phase 1 completion are not that authorization. Retrying rejected `perTest` or `all` is not authorized either.

### Three-script implementability: not proven

The proposed eventual allocation is run-mutation.ps1 for candidate configuration/orchestration and existing evaluation/timing, Test-RunMutation.ps1 for mode/configuration/guard/integrity regression coverage, and _harness-config.ps1 only if proven necessary without a new policy key. This is a proposed responsibility split, not proof that those changes suffice. No new dependency, renderer, install change, harness.yml key, generated-file hand edit or consumer rewrite is approved.

Recon identifies configuration construction at run-mutation.ps1:1112–1119, evaluation at :215/:1202 and native timing at :1283, but supplies no perTestInIsolation mechanism or script-only configuration precedence proof. The existing renderer relationship and shipping guard make a generated-config-only solution a fact to establish, not permission to bypass validation. If implementing the mode needs an unnamed renderer/install/config/consumer edit, stop for a cited Patron ruling; ticket contradiction or constitution departure uses the standing owner escalation. T004 cannot certify zero unnamed edits from the supplied evidence.

### Recon-listed consumers: compatibility obligations, no PASS claimed

| Consumer from recon-DEV-451 section 4 lines 48–57 / section 7 line 82 | Required compatibility and present limit |
|---|---|
| CI ci.yml:254, run-mutation.ps1 -EvaluateReport | Preserve parameters, exit semantics and actual whole-report score/kill-share/span/ArchitectureTests/scope evaluation. Recon proves the control evaluation path, not candidate report compatibility. |
| ci-stryker-shard.sh mirrored configuration and report merge | Do not silently change CI's mtp/off behavior. Candidate mode must be confined to the proposed local invocation path; prove that CI's report-only caller still works without a shard-script change. No such proof is supplied. |
| new-mutation-receipt.ps1 overlay/invocation | Preserve existing invocation, support overlay set and receipt/timing locations. A new scratch filename is not automatically selected by its existing overlay list. Exact scratch launch/provenance mechanics need recon. |
| _gate-common.ps1 and fixture harness-config stub | Preserve dot-source API, root resolution and fixture seam; no new mandatory config key. Current recon lists these dependencies but does not prove candidate wiring through them. |
| README, AGENTS, architect/verify/diagnosing-bugs skills, agent files and .claude/settings.json | Retain documented command forms, flags, gate verdicts and output artifacts. Candidate adoption must not make their mtp/off guidance false without an unnamed edit. Listing references is not a contents-level compatibility proof. |

Required unnamed consumer edits are a STOP, not a silent rewrite. DEV-452 caller drift is included in this obligation. No compatibility failure is asserted merely because proof is missing, and no consumer is labeled compatible without that proof.

### Scratch experiment specification awaiting feasibility proof

The intended non-shipping shape is a versioned copy of the gate in an operator-authorized replay support location, preserving the script's repository-root/dot-source/tool lookup behavior. Keep the tracked shipping scripts and generated root config unchanged. Archive original/candidate gate bytes, exact diff and SHA-256 manifest; disclose any experimental guard change explicitly. Do not create the copy or invent its path/launch command until recon verifies root resolution and Bernstein/Rigger supplies the authorized location. An external generic TEMP copy cannot be assumed to work just because baseline OutputRoot is in TEMP.

After mechanism proof and Patron experimental authorization, each frozen case would use its existing immutable base/head, code/tests, suites, concurrency 8 and timeout 90000, with only this lever changed. Reuse section 10.7's single-owner quiet-machine and synchronous wrapper capture requirements. Use fresh candidate attempt roots and archive all attempts under specs/DEV-451/evidence/candidate/<case>/<attempt>/; retain invalid attempts and absent-artifact explanations. Scratch timing must disclose experimental provenance and carry generatedUtc, durationSeconds, concurrency, threshold and per-project duration/counts/tested/score/passed equivalent to the gate schema. Independently capture start/end/wall/exit, full stdout/stderr, generated configs, full reports, source/support hashes and actual command lines. Never call these native shipping-gate acceptance artifacts or synthesize missing timing.

Run the **unchanged shipping gate's actual -EvaluateReport path** on the candidate report with the frozen git-diff-shaped scope and unchanged policy, capturing its command, output and exit as a separate integrity receipt. Recon-DEV-451-2-2 section 10.4 demonstrates why mutate-relative paths are wrong for scope containment; its successful function probes are not a substitute for this CLI receipt. Exact required EvaluateReport arguments and scope-file construction must come from targeted recon before execution. A real evaluation failure is retained; baseline DEV-444 is already a valid failure and must not be relabeled or rerun for a pass.

T005 additionally requires normalized stable source/location/mutator/replacement identity populations and the full baseline/candidate status matrix, zero positive Survived-to-Timeout or Killed-to-Timeout conversions, explained timeout totals, intact suites and kill-share/span safeguards, including DEV-444's weaker exempt-class span signal. Coverage-derived NoCoverage/ignored changes must be explained rather than used to hide an unmatched or untested population. Timing alone cannot authorize adoption. The requirements above specify the desired experiment contract; **a feasible executable scratch path and actual integrity results have not been proved**.

### Disposition and exact return

**T004 remains unchecked; T005 is blocked.** No candidate adoption, scratch feasibility PASS, compatibility PASS, experimental authorization, ADR amendment or runtime result is claimed. Only this conclusions section is appended; tasks.md completion/status is not changed because the completion conditions are unmet.

**needs recon: For pinned Stryker 5.0.0 mtp/perTestInIsolation, what are the actual mode semantics and gate/report wiring; can generated candidate configuration, unchanged policy and every listed CLI/output consumer be supported with edits confined to the three ticket-named scripts; and what verified versioned-copy root/launch/overlay plus actual -EvaluateReport scope/argument procedure produces complete durable scratch artifacts on the three frozen replay cases?** Return cited source locations, precise commands and any required unnamed edits, not a mode-name inference. Bernstein should obtain this bounded mechanism recon before returning T004 for completion.

**needs decision: Patron — after targeted recon, explicitly authorize or reject a bounded non-shipping mtp/perTestInIsolation experiment, identifying any scratch-only guard delta.** This requests experimental permission only, not shipping adoption, renderer/install/config scope expansion or T006 ratification. No decision ask is sent directly ahead of its missing factual basis. If authorized and feasible, finish T004, then assign Gauge T005; T006 ratification and T010 ADR checkpoint still precede T011. A change from ADR 0016's mtp/off Decision cannot take the zero-departure no-amendment path.

---

## T004 experimental decision — Patron ruling (2026-11-04)

[from Patron] DEV-451 Phase 2 T004 Ruling: AUTHORIZE the bounded non-shipping `mtp` / `coverage-analysis: perTestInIsolation` scratch experiment for T005 under the external copy procedure (`%TEMP%\DEV-451-scratch-gate\`) with scratch delta D1–D4 and **zero guard delta**, and confirm that all tracked shipping scripts and configs remain untouched until post-ratification T010/T011.
- Unmeasured mode & three-script generated-config boundary (`docs/adr/0016-stryker-mtp-runner.md:19-31,74-76,93-105`; `recon-DEV-451-3` §1.5, §2.1–2.3, §3:127-151): unlike rejected `perTest` (E6) and `all` (E1/E3), `perTestInIsolation` on Stryker 5.0.0 MTP is unmeasured in-repo and is reachable strictly via per-run generated config overrides (`scripts/run-mutation.ps1:1107-1129`) through scratch edits D1–D4 (`-CoverageAnalysis` opt-in parameter, `''`/`perTestInIsolation` allowlist validation + explicit experimental stdout disclosure, `$generated['coverage-analysis']` override, and additive `mutation-timing.json` provenance key at `:1276-1285`), requiring zero `harness.yml` or tracked `stryker-config.json` edits.
- Zero guard delta & consumer/evaluation integrity (`recon-DEV-451-3` §1.1–1.2, §2.4:101-115, `recon-DEV-451-3-2` §4.1–4.5, §5.3, §5.7): because `scripts/run-mutation.ps1:973-977` validates `$baseStryker` (the tracked `stryker-config.json` at `mtp`/`off`), the scratch guard delta is **zero lines** (`:973-977` stays byte-identical and passing while D2/D4 and archived `<out>/LamuFlix.Core/stryker-config.json` disclose the effective mode); all four recon-listed consumers are compatible with zero edits for the bounded trial, and each candidate report must be evaluated by the unchanged tracked `./scripts/run-mutation.ps1 -EvaluateReport` using `pwsh -Command` explicit `@(...)` `-ScopeFile` array binding.
- Non-shipping boundary & downstream checkpoints (`CONCLUSIONS.md` Q3:42-54, Plan-Challenge 1–4:117-120; `recon-DEV-451-3` §3:146-151, `recon-DEV-451-3-2` §5.1–5.8): this ruling authorizes only the T005 external scratch experiment (`%TEMP%\DEV-451-scratch-gate\` launched against the frozen replay roots with original/candidate bytes, `run-mutation.delta.diff`, and `MANIFEST.sha256` archived under `specs/DEV-451/evidence/candidate/_gate/`); tracked `scripts/run-mutation.ps1`, `scripts/_harness-config.ps1`, `scripts/Test-RunMutation.ps1`, `stryker-config.json`, `harness.yml`, and `scripts/ci-stryker-shard.sh` remain untouched until T005 matched evidence, T006 Patron ratification, and T010 in-place ADR 0016 amendment (plus a separate Charter §2.3 STOP ruling if T011 requires reconciling `scripts/ci-stryker-shard.sh:151-163`).

---

## T004 finalized — authorized scratch contract; ready for T005 (2026-11-04, Keel)

Authority: Conductor T004 resume; DEV-451 task note lines 117–129; recon-DEV-451-3 and recon-DEV-451-3-2; the complete Patron experimental ruling immediately above. Branch read-back is feature/DEV-451 in F:/Dev/LamuFlix.worktrees/DEV-451. This receipt supersedes the prior T004 needs-recon/needs-decision and T005-blocked dispositions for **experiment readiness only**. The supplied recon and explicit authorization satisfy those missing prerequisites; no independent search or runtime experiment is performed by Keel.

### Candidate, mechanism and three-script boundary proof

Candidate: Stryker 5.0.0 MTP / coverage-analysis perTestInIsolation. Control: the accepted archived mtp/off baselines, not new control reruns. This changes neither DEV-435 changed-file globs/direct-reference filtering, nor since, concurrency, eligible suites or policy. Recon-DEV-451-3 §2.1 records official Stryker configuration documentation: coverage-analysis is config-file-only; perTestInIsolation captures per-test coverage through isolated test runs, then uses covering tests for each mutant. This could reduce per-mutant work but increases startup cost. Neither speed nor correctness on these cases is established until T005. Its name does not prove immunity to DEV-410 contamination.

Source proof (recon-DEV-451-3 §1.5, §2.3–2.4, §3): run-mutation.ps1:1107–1110 copies the tracked config into a per-run generated map; :1111–1124 applies gate-owned overrides; :1129 writes TEMP config; :1189 passes that exact file to Stryker via -f; :1184 archives effective config. Introducing the candidate after the copy therefore requires only the named run-mutation.ps1 surface, without a renderer/install/harness.yml change or tracked stryker-config.json edit. The only gate write sites are generated TEMP config, its output-root copy and timing. _harness-config.ps1 has no write sites or coverage key and needs no change. Future Test-RunMutation.ps1 D5 cases can prove opt-in, absent-flag off and invalid-value failure within the third named surface; **D5 is not part of T005 and no tracked fixture edit is authorized by this scratch handoff**.

The :973–977 guard reads the **base file**, which remains mtp/off, rather than the generated map. Patron explicitly authorizes generated-only divergence for this experiment. Preserve the guard byte-for-byte: **zero guard delta**, including no effective-value guard substitution. Shipping mtp/non-off remains GATE NOT WIRED under the existing contract. This source-level experiment feasibility proof does not authorize a shipping loophole.

| Authorized scratch delta, only in the external run-mutation.ps1 copy | Exact bounded behavior |
|---|---|
| D1, param block :36–43 | Add string CoverageAnalysis, default empty string. |
| D2, validation region after :985 | Accept only empty string or perTestInIsolation, otherwise exit 1. For the candidate print explicit EXPERIMENTAL / non-shipping disclosure; do not prefix it with Mutation testing: or Stryker native exit for. |
| D3, generated overrides :1111–1124 | Only when CoverageAnalysis is nonempty, set generated coverage-analysis to that value. All other settings remain control. |
| D4, timing object :1276–1282 | Add provenance identifying a non-shipping scratch experiment and effective perTestInIsolation mode. Preserve all existing timing fields and meanings. D4 is required by this authorization/artifact contract. |

Thus the candidate configuration is implementable within the three ticket-named scripts with **zero unnamed renderer/install/harness.yml/consumer edits**. For T005 the only modified executable is the external copy; _gate-common.ps1 and _harness-config.ps1 travel with it as byte-identical support, and every tracked script/config remains untouched. No new policy key, package or project is needed.

### Consumer compatibility proof and shipping limits

Accept recon-DEV-451-3-2 §4.1–4.5: ci.yml:254 calls only the unchanged tracked mode-agnostic EvaluateReport branch and preserves its exits; ci-stryker-shard.sh reads the tracked mtp/off config and never sees the external trial; new-mutation-receipt.ps1 selects its existing overlay/command and cannot pick up the external candidate, while its parsed verdict/native-exit lines remain unchanged; _gate-common.ps1 and its harness-config dot-source API remain byte-identical. README/AGENTS/skills/agent/settings references still describe actual shipped behavior during the trial. No listed consumer needs an edit for this bounded experiment.

For later adoption, do not extend that compatibility conclusion: the CI shard script independently generates config from the tracked off value (:151–163), so local-only candidate adoption would diverge from CI. Shipping documentation also continues to describe off. T006 must resolve the intended shipping contract and any necessary unnamed consumer edits through a cited Patron ruling, before such edits or adoption. Do not defer a known required edit past ratification by calling it experiment-compatible. Any ticket contradiction or necessary constitution departure uses the standing user escalation. T004 proves trial feasibility, not unrestricted T011 readiness.

The candidate departs ADR 0016's Decision requiring off in every generated config. If ratified, an approved/landed in-place T010 amendment is mandatory before T011; the zero-departure no-amendment route is unavailable. All DEV-410 safeguards remain unchanged throughout the trial.

### Gauge T005 input construction and preflight

Bernstein may assign Gauge the following bounded writes: the already-authorized external %TEMP%/DEV-451-scratch-gate/ root, fresh per-case TEMP capture/output roots below, and specs/DEV-451/evidence/candidate/ archives in the assigned Build worktree. No copy is placed inside a replay checkout. No replay provisioning or new overlay is needed.

1. Before construction, require the scratch root and evidence/candidate/_gate/ destination absent. Preserve and report any existing destination; do not use recon's Force construction example to overwrite evidence. Copy the frozen run-mutation.ps1, _gate-common.ps1 and _harness-config.ps1 triplet from the assigned DEV-451 scripts directory; retain original/run-mutation.ps1; verify the full frozen SHA-256 values from T001 (2a0a8ff8… / b4906af9… / 8b669b6f…). Apply only D1–D4 to the external run-mutation.ps1. Archive original/candidate/support bytes, run-mutation.delta.diff and MANIFEST.sha256 at evidence/candidate/_gate/. Record complete hashes for each input and the delta, and a separately recorded manifest hash; the manifest must not attempt to include its own digest. git diff --no-index exit 1 means the expected delta exists, not a mutation gate failure. Review the delta to confirm no guard or unrelated change.
2. Use the exact case table below. Verify detached HEAD, immutable base, frozen src/tests and all support/tool/config hashes against T001 and each accepted baseline receipt. Machine ALIEN, concurrency 8 on 16 processors, additional-timeout 90000, Stryker 5.0.0, mtp and since.enabled=false remain pinned. Both Evaluation and UnitTests remain eligible. DEV-444/448 replay porcelain must be clean; DEV-333 may show only the existing Rigger harness.yml overlay, hash ee866efadcb2bbb82206a677ff7b47f2537fd7d6802b9243fad0d45ba7514f09. Never overlay candidate scripts into any replay tree.
3. Launch only with CWD equal to the named replay root; assert git rev-parse --show-toplevel matches. HARNESS_REPO_ROOT must be unset or exactly that same root; record it and stop on conflict. Recon P-COPY-1/3 proves root resolution with the external triplet; P-COPY-2 proves a wrong-CWD launch fails GATE NOT WIRED. All policy/tool/config reads still resolve against the frozen replay root.
4. Before preflight and again before measurement, record exclusive Gauge ownership and a machine process query proving no other mutation session. Hold the machine quiet through archive completion. Run cases serially, in table order. A silent console is not a completed process. No overlapping runner, background replacement, arbitrary runtime cutoff or best-of retry is allowed.

| Case / replay root under F:/Dev/LamuFlix.worktrees/ | Frozen HEAD | BaseRef | Accepted control |
|---|---|---|---|
| DEV-444 / DEV-451-replay-DEV-444 | 0079991451599e408b6ff37de25a6e3e731b2c6a | 1e95037eda58502e0f7088a31f4502798e51d3a0 | baseline/DEV-444/attempt01, valid FAIL |
| DEV-448 / DEV-451-replay-DEV-448 | 6fcbd1ffdb3805b46937534bac48a37a68ebd62b | f8bb21cb38e3b9105ee1bc0500966903a5fad40a | baseline/DEV-448/attempt03, PASS |
| DEV-333 / DEV-451-replay-DEV-333 | b25ecb1614a0c82771f254b700bb53dff0a25bf0 | f00e01bacb63c083349b3a46425189e31e3564c2 | baseline/DEV-333/attempt01, Core PASS / Infrastructure N/A |

Per case, use recon-DEV-451-3-2 §5.4's exact preflight command and §5.5's synchronous capture template, substituting that table's case/root/head/base consistently. Copy path is (Join-Path $env:TEMP 'DEV-451-scratch-gate/run-mutation.ps1'). Invocation is pwsh -NoProfile -File <copy> -BaseRef <table-base> -OutputRoot <fresh-root> -CoverageAnalysis perTestInIsolation; append -DryRun for preflight only. Do not add Project or WholeProject. Names are:

- Preflight output: %TEMP%/DEV-451-candidate-<case>-preflight01; log: %TEMP%/DEV-451-<case>-candidate-preflight01-console.log.
- Real output: %TEMP%/DEV-451-candidate-<case>-attempt01; log: %TEMP%/DEV-451-<case>-candidate-attempt01-console.log; capture: %TEMP%/DEV-451-<case>-candidate-attempt01-gauge-timing.json.
- Durable attempt destination: F:/Dev/LamuFlix.worktrees/DEV-451/specs/DEV-451/evidence/candidate/<case>/attempt01/.

Require every destination absent before use, including preflight logs and evaluation capture files. Preflight must exit 0 with the explicit experimental print and generated perTestInIsolation/mtp/8/90000/since-disabled settings, exact T001 mutate entries and both suites; DEV-333 retains Infrastructure N/A. Dry-run creates no OutputRoot; preserve its printed config as preflight.log. Any drift or failed assertion stops before a real run.

### Measurement, actual integrity receipt and durable evidence

Use one persistent owning PowerShell session and recon §5.5's Stopwatch/start/end capture, synchronous wrapper wait, immediate LASTEXITCODE and gauge-timing.json with gate-copy SHA and coverageAnalysis. If a tool yields a running session, wait on that same session. Require wrapper summary/terminal verdict, actual scratch-produced mutation-timing.json with unchanged fields plus D4 provenance, full JSON report, and effective config showing perTestInIsolation. Capture is not complete merely because the Stryker child exited. Do not synthesize absent artifacts or call scratch timing native shipping-gate acceptance.

After each run, use the **unchanged tracked replay gate** with the candidate report and the complete git-diff-shaped ScopeFile array. Recon-DEV-451-3 §1.1 proved that pwsh -File array arguments lose or combine entries; therefore use pwsh -Command explicit array binding or its equivalent in-process array. The following concrete DEV-444 command body implements recon §5.7 without string-interpolated report paths; put it in a single-quoted PowerShell here-string passed to pwsh -NoProfile -Command, redirect all output to the attempt's fresh eval-console.log, and immediately capture LASTEXITCODE in the owning shell:

```powershell
$scopeFiles = @(git diff --name-only --diff-filter=AMR 1e95037eda58502e0f7088a31f4502798e51d3a0 HEAD -- '*.cs')
if ($LASTEXITCODE -ne 0 -or $scopeFiles.Count -ne 3) { throw 'Frozen scope mismatch' }
$candidateReport = Join-Path $env:TEMP 'DEV-451-candidate-DEV-444-attempt01/LamuFlix.Core/reports/mutation-report.json'
& ./scripts/run-mutation.ps1 -EvaluateReport $candidateReport -ScopeFile @($scopeFiles)
exit $LASTEXITCODE
```

For DEV-448 and DEV-333 substitute table base/case and expected scope count 5 and 16, respectively. Before invocation compare every scope entry to recon §5.7's verified lists (DEV-333 list in recon-DEV-451-2 §9.4), not count alone. Recon proves these frozen raw diffs contain no migration/harness-excluded paths, so they equal the wrapper's changedFiles; stop on list drift instead of silently filtering a new tree. Do not omit ScopeFile as CI does, use mutate-relative paths or evaluate through the candidate copy. Archive the exact command/body, scope array, report hash, stdout/stderr and actual evaluation exit separately from the wrapper exit. Recon's unchanged-gate baseline probes reproduced DEV-444 exit 1 and DEV-448/333 exit 0; these are negative/positive control proofs of the procedure, not predictions for candidate results.

The unchanged EvaluateReport path exits before config/guard reads and applies scope containment, nonzero-tested, score, ArchitectureTests/artifact, kill attribution, share, span/exemptions and since checks under unchanged policy. NoCoverage remains in the tested/score denominator (Killed+Timeout+Survived+NoCoverage); coverage mode cannot remove it to inflate the score. Record all report statuses and failures honestly. A candidate report evaluation failure is evidence, not grounds for a best-of rerun; missing capture/artifact or broken preflight is an invalid attempt, and any replacement needs a recorded reason and handoff.

Immediately archive console.log, preflight.log, gauge-timing.json, receipt.md, complete unmodified output/ including scratch timing/effective config/full reports, and eval-console.log plus evaluation exit/command/scope record. Receipt includes all frozen identities, support/policy hashes, gate/manifest/delta hashes, machine/runtime/load/ownership/process receipts, start/end/wall, wrapper/native/evaluation exits, actual suites/concurrency/timeout, mutants/tested/score/status totals and overlay provenance. Keep all invalid/failed attempts and prior baselines intact. Preserve absence honestly if a stage could not run. End-of-run root/support hashes and porcelain must match the preflight state.

Build one complete baseline↔candidate status matrix per case keyed by normalized project-relative source path + full start/end location + mutatorName + replacement, with matching source content and duplicate/unmatched checks; numeric IDs are not identities. Require unchanged full mutant populations, preserve ignored/compile-error entries and explain coverage-derived status changes. Explicitly report Survived-to-Timeout and Killed-to-Timeout counts (both must be zero), timeout totals/growth rationale, actual kills, new NoCoverage and ignored changes, share and span signals. DEV-444 recommendation exemptions weaken span detection: inspect share and per-mutant outcomes without changing exemptions. No omitted suite, relaxed policy or wall-time-only improvement qualifies. Rejection of a candidate is a valid T005 result, not ticket completion or permission to try a different mode.

### Completion and next steps

**T004 complete; mark [X]. Gauge T005 is ready for assignment** under this contract and the named recon commands. This closes proposal/mechanism/boundary/consumer/scratch feasibility/experimental authorization conditions only. No candidate measurement, integrity PASS, speedup, shipping adoption or ticket acceptance is asserted. T005 and T006 remain unchecked.

Gauge returns all three archived attempts, gate input archive, exact evaluation receipts and stable-identity matrices to Bernstein for validation and T006 Patron ratification or fail-closed decision. T006 must address the shipping consumer boundary and every ADR/DEV-410 item; a successful scratch trial cannot authorize unnamed edits. T010 in-place ADR approval precedes any T011 adoption. T012/T013 remain the separate native shipping-gate matched measurement and single DEV-444 under-1800-second acceptance bar. Only this conclusions receipt and tasks.md status/completion are edited by Keel; existing unrelated changes are preserved.

---

## T005 validation complete / T006 preparation (2026-11-04, Keel)

Authority: Bernstein T005 validation/T006 preparation ask; DEV-451 task note 137–142; candidate/T005-findings.md and candidate archives; accepted baseline archives; recon-DEV-451-3-2 §4; ADR 0016 Decision and amendments. Branch verified feature/DEV-451 in the assigned worktree. Only this file and tasks.md are edited; earlier changes and archives are preserved. No new runtime measurements, mechanism searches, script/config/production writes or ADR drafting.

### Validated evidence

Read construction/delta/manifest, per-case receipts, effective configs/timing, evaluation logs/exits and matrices. Independently reconstructed joins from raw archived reports using normalized path + complete start/end location + mutatorName + replacement. Verified source content, duplicate/unmatched identities, report SHA-256 against eval-exit.json, full effective config equality except coverage mode, serial start/end windows and empty competing-session lists. This is artifact validation, not a fresh experiment.

| Case / accepted control | Baseline → scratch wall seconds | Reduction | Tested / unchanged score | Wrapper and tracked evaluation exits | Timeouts |
|---|---:|---:|---|---|---|
| DEV-444 / attempt01 | 1088.7 → 840.3 | 248.4 s / 22.8% | 68 / 58.82% | 1 → 1; **FAIL retained** | 4 → 4 |
| DEV-448 / attempt03 | 3385.7 → 880.0 | 2505.7 s / 74.0% | 221 / 89.59% | 0 → 0; PASS | 0 → 0 |
| DEV-333 / attempt01 | 1233.3 → 919.4 | 313.9 s / 25.5% | 68 / 86.76% | 0 → 0; Core PASS, Infrastructure N/A | 4 → 4 |

Populations match 98/98, 283/283, 104/104, with zero duplicate/unmatched identities or source mismatches. Independent transitions reproduce Gauge matrices: **zero Survived→Timeout and Killed→Timeout everywhere**. DEV-448/333 are status-for-status unchanged. DEV-444 has three Survived→NoCoverage; killed 36 and timeout 4 stay fixed, with NoCoverage retained in tested denominator: (36+4)/68 = 58.82%. Ignored/CompileError counts remain unchanged. This is explained coverage classification, not score inflation.

Effective configs preserve mtp, concurrency 8 on 16 processors, timeout 90000, since=false, Evaluation + UnitTests, exact mutate lists and thresholds. Receipts preserve frozen head/base/source/test hashes and DEV-333's existing Tests.Common policy overlay. ALIEN capture windows are serial: 18:20:36–18:34:37Z, 18:36:23–18:51:04Z, 18:52:36–19:07:57Z. Gauge wall seconds 840.999/880.635/920.043 bracket native scratch timing. Timing explicitly identifies non-shipping provenance.

Unchanged tracked EvaluateReport uses array-bound scope counts 3/5/16 and matching report hashes; actual exits 1/0/0. Largest shares 35.29%/33.48%/20.59% remain below 50%; non-exempt spans 0/1/1 remain below 15. DEV-444 has 32 exempt tests, DEV-448 77 (widest exempt span 1 each), DEV-333 none. Exemptions weaken the span signal; unchanged actual kills, individual statuses and share support this bounded result, not general immunity to contamination.

Recomputed original/candidate/manifest/delta hashes match the construction receipt: 2A0A8FF8A80E87FE326BB0537AE83C36F82E5587068F67656F6DBE5F06E052BE / A7184D91350028B77C24B5D35CCDF3C69817306EA2824E787C9AB67D42CA79BE / 0A20E74482C997D7C507DFD15D037BFFB2FC63658F7D7F08C338D89820350B62 / 287CD53308FB4201477296E0A5323FEA7F5FBC7CD970546D29CC53693CD503E6. Archived support hashes match frozen B4906AF9… / 8B669B6F…. Delta is D1–D4 plus disclosed D1b early script-scoped parameter capture: PowerShell case-insensitivity otherwise lets guard local coverageAnalysis overwrite CoverageAnalysis. Failed first preflight remains archived; correction precedes measurements; guard bytes unchanged. Accept D1b as input preservation within the same authorized external copy/mode, not another lever or guard authorization. Include it explicitly in ratification. Its explanatory scratch comment is not shipping precedent under the no-comments rule.

**T005 complete.** Measured speed and verdict-integrity criteria are met for Patron consideration. DEV-444 still fails. Scratch walls under 1800 seconds do not complete T006/T010/T011 or native T012/T013 acceptance. Three single matched samples establish neither repeatability nor whole-Core/CI performance.

### T006 — every ADR 0016 Decision item and DEV-410 safeguard

T006(c) experimental prerequisite is satisfied by the full Patron T004 experimental ruling already recorded above and task-note lines 125–135: the unsupported mtp/perTestInIsolation scratch trial was explicitly authorized before measurement. That authorization is not shipping ratification.

| Decision item | Evidence / required disposition |
|---|---|
| Pin Stryker 5.0.0 | Preserved frozen tool receipt; no package/version change. |
| test-runner=mtp | Preserved all effective configs; no VSTest proposal. |
| coverage-analysis=off in tracked and every generated config | **Departure:** generated perTestInIsolation. Base off and unchanged base-file guard do not cure it. Explicit ratification and approved in-place T010 amendment mandatory before T011; no-amendment path unavailable. |
| Explicit directly referencing eligible projects, excluding ArchitectureTests; fail if none | Both eligible suites retained. Preserve selection, fail-if-none and negative regression cases. Per-test selection inside retained suites does not authorize project omission. |
| Fail ArchitectureTests killedBy attribution | Unchanged tracked evaluation retains the check; preserve negative regression proof. Existing reports alone do not prove the negative case. |
| Threshold remains 80 | Preserved; DEV-444 remains FAIL. No denominator, exclusion or threshold change. |

Retain DEV-410 timeout floor 30000 and actual 90000; half-core ceiling; since disabled; share 50 with existing 20-tested minimum; span 15 with exact class-prefix/reason exemptions; artifact/ArchitectureTests and missing-suite failures. No mutable static initialization fix is proposed: perTestInIsolation does not establish later mutant-server state isolation. Preserve static-initialization discipline, CI aggregate share/span, unique-shard population/merge and restore safeguards. CRLF span defect remains DEV-460, not fixed here.

### Three-script boundary and shipping consumers

Recon proves local generated-config wiring fits run-mutation.ps1 plus Test-RunMutation.ps1 regression coverage; _harness-config.ps1 need not change. No new policy key, dependency, renderer/install edit, tracked generated-file edit or harness.yml policy mutation is needed for that mechanism. T014 still requires all-three-script upstream parity.

The scratch guard checks base off while effective config diverges. It is **not a shipping contract**. T010/T011 must expressly define and validate permitted effective mode/default invocation, retain off control, reject unsupported modes and test parameter preservation. Do not transplant scratch-only provenance into normal receipts.

- ci.yml:254 EvaluateReport remains mode-agnostic; preserve API/output/exits, no edit identified.
- ci-stryker-shard.sh:151–163 independently inherits tracked off. Local adoption leaves CI off: no mode parity or CI speedup established. A fourth-script edit is unauthorized.
- new-mutation-receipt.ps1 overlays the gate and invokes its default command. A local default propagates; an opt-in flag alone does not. Preserve parsed Mutation testing: / Stryker native exit for prefixes and timing/receipt fields; additive disclosure must avoid these prefixes.
- _gate-common.ps1 root/dot-source API stays unchanged; no edit needed.
- Recon §4.5 identifies README/AGENTS/skills/agent/settings off-only guidance that becomes stale on adoption. No approved exact patch set exists. These consumers are not cleared for shipping merely because the scratch experiment was compatible.

### Prepared Patron ask — pending, not a ruling

[from Keel] DEV-451 T006: Do you RATIFY Stryker 5.0.0 mtp/perTestInIsolation for the local pre-PR generated-config path, including D1b parameter preservation, subject to approved in-place ADR 0016 T010 amendment before T011 and unchanged DEV-410/policy/eligibility safeguards? Basis: local pre-PR objective and ACs (task note 12–14), Q2/Q3/Q5, candidate/T005-findings.md and validated matrix above: 22.8%/74.0%/25.5% faster, zero S→Timeout/K→Timeout, identical scores/exits, valid DEV-444 FAIL retained. This is not native acceptance or CI evidence.

Ratification must also settle shipping scope. Recommend an explicitly local default generated mode with tracked mtp/off control and CI shards remaining off, because only local cases were measured. Explicitly accept/document that difference in ADR 0016 and rule how known off-only references are reconciled under Q5. No silent fourth-script/config/renderer edit. If common local/CI mode or unnamed consumer/doc edits are required, identify a bounded ruling and obtain exact-file recon before implementation. A ticket Scope & Technical Design/AC contradiction or necessary constitution departure remains user-owned structural escalation; Patron approval cannot close that checkbox. A deliberate local-only contract within the ticket is Patron-owned, not automatically user escalation. If boundaries cannot be settled, fail closed to Bernstein rather than adopt through the scratch guard loophole.

Ratify the limits too: single matched samples, four pre-existing timeouts in DEV-444/333, three explained NoCoverage entries in DEV-444, and exemption-limited span evidence. No threshold/suite/timeout reduction or source-state fix approved. Shipping must prove default config/guard and consumers; T012 all-three native samples, T013 single DEV-444 under-1800-second validation, T014 parity and final gates remain.

### Next steps

**T006 remains unchecked.** Bernstein should route the prepared ask to Patron; no ADR drafting or T011 from this preparation receipt. Record the complete cited ruling/rationale/implications here and any changed plan decision in brief.md before artifact changes, then T010 in-place ADR 0016 amendment (no new number). Missing exact consumer facts go to Wisp through Bernstein as needs recon. Existing independent T007–T009 ordering remains. No ticket acceptance, shipping readiness or awaiting-merge claim.

---

## T006 lever ratification — Patron ruling (2026-11-04)

[from Patron] DEV-451 Phase 2 T006 Ruling: RATIFY Stryker 5.0.0 `mtp` / `coverage-analysis: perTestInIsolation` for the local pre-PR generated-config default (`scripts/run-mutation.ps1`), including D1b early parameter preservation, subject to an approved/landed in-place ADR 0016 amendment at T010 before T011 (`docs/adr/0016-stryker-mtp-runner.md`) and unchanged DEV-410/policy/eligibility safeguards; APPROVE the explicitly local default generated mode with tracked `stryker-config.json` `mtp`/`off` control, `-CoverageAnalysis off` control retention, and `scripts/ci-stryker-shard.sh` CI shards remaining `off` with zero fourth-script/renderer/config edits; and APPROVE keeping unnamed consumer/doc files untouched (`noted, no ticket` for stale `off`-only mentions outside `docs/adr/0016-stryker-mtp-runner.md` and `specs/DEV-451/`).
- Measured speed, verdict integrity, and ADR 0016 / DEV-410 comparison (`specs/DEV-451/evidence/candidate/T005-findings.md:22-58`, `CONCLUSIONS.md:582-615`, `docs/adr/0016-stryker-mtp-runner.md:24-31,47-60,74-76,108-115`): matched T005 runs cut native wall time by 22.8% on DEV-444 (1088.7s -> 840.3s, `< 1800s`), 74.0% on DEV-448 (3385.7s -> 880.0s), and 25.5% on DEV-333 (1233.3s -> 919.4s) with identical scores/exits (`58.82%` FAIL retained on DEV-444; `89.59%` PASS on DEV-448; `86.76%` Core PASS / Infra N/A on DEV-333), matched populations (`98/283/104`), zero `Survived->Timeout` or `Killed->Timeout` conversions, unchanged timeout totals (`4/0/4`), and 3 explained `Survived->NoCoverage` transitions on DEV-444 keeping `NoCoverage` in the tested denominator (`68`); because generated `coverage-analysis: perTestInIsolation` departs ADR 0016 Decision item 1 (`coverage-analysis: off` in every generated config, line 26-27) while preserving all other ADR 0016 items and DEV-410 safeguards (`dotnet-stryker` 5.0.0, `test-runner: mtp`, explicit eligible test-project filtering excluding `*.ArchitectureTests`, `ArchitectureTests` `killedBy` failure, threshold `80`, `additional-timeout` floor `30000` / actual `90000`, half-core concurrency ceiling, `since.enabled=false`, `maxKillShare: 50`, `maxKillFileSpan: 15` + `killFileSpanExempt`, static-init discipline), an approved in-place T010 amendment to `docs/adr/0016-stryker-mtp-runner.md` (no new ADR number) is mandatory before T011, and D1b script-scoped parameter capture is ratified (without scratch explanatory comments per AGENTS.md Non-negotiables) alongside explicit effective-mode guard validation (`perTestInIsolation` default + `off` control allowed; unsupported modes rejected) in `scripts/run-mutation.ps1` and `scripts/Test-RunMutation.ps1`.
- Local default vs CI-shard `off` shipping scope & three-script boundary (`DEV-451` task note lines 12-14, `CONCLUSIONS.md` Q2/Q3/Q5:5-54,74-86,517-523,617-626, `recon-DEV-451-3-2` §4.1-4.4:5-29): DEV-451's ticket scope and ACs target the local pre-PR mutation gate (`scripts/run-mutation.ps1`), which is also what `scripts/new-mutation-receipt.ps1:369` invokes by default (requiring D2/provenance stdout in T011 not to start with `Mutation testing:` or `Stryker native exit for`), whereas post-merge whole-Core CI shards (`scripts/ci-stryker-shard.sh:151-163`) are explicitly out of scope (`CONCLUSIONS.md` Q6:101, `tasks.md:101`) and unmeasured under `perTestInIsolation`, while `.github/workflows/ci.yml:254` calls only the mode-agnostic `-EvaluateReport` branch; therefore `scripts/run-mutation.ps1` shall default generated configs to `perTestInIsolation` while keeping tracked `stryker-config.json` at `mtp`/`off` (zero `install.ps1` renderer or `harness.yml` edits) and leaving `scripts/ci-stryker-shard.sh` untouched on `mtp`/`off`, with T010 documenting this deliberate local-gate vs CI-shard distinction in `docs/adr/0016-stryker-mtp-runner.md`.
- Q5 / Charter §2.3 #6 consumer & documentation boundary and remaining acceptance bars (`CONCLUSIONS.md` Q5:74-86, Plan-Challenge 4 & 7:119-120, `CONCLUSIONS.md:627,633-636`, `recon-DEV-451-3-2` §4.5:30-33): under Charter §2.3 item 6 and Q5, DEV-451 touches only the three ticket-named scripts (`scripts/run-mutation.ps1`, `scripts/_harness-config.ps1`, `scripts/Test-RunMutation.ps1` with all-three-script `dotnet-agent-harness` `origin/main` parity at T014), `docs/adr/0016-stryker-mtp-runner.md` (T010), and `specs/DEV-451/` artifacts, with zero fourth-script/renderer/unnamed doc edits (`README.md`, `AGENTS.md`, `.agents/skills/*`, `.claude/settings.json` remain untouched in DEV-451 as `noted, no ticket` because their command surfaces and tracked `stryker-config.json` base remain accurate and ADR 0016 is the canonical runner/coverage decision record); T005 scratch timings do not satisfy native shipping acceptance, which still requires T010 before T011, T012 native shipping-gate runs across all three frozen cases, T013 single-sample DEV-444 validation `< 1800s`, and T014-T020 gates.


