# DEV-451 — Pre-PR mutation under 30 minutes

## Status and authority

Phase 2 grill CLOSED by Patron at Q6, 6/12 questions, 2026-11-04. Quill drafts `spec.md`, `plan.md` and `tasks.md` from this brief. Analysis, challenge, plan freeze, Gate 1, implementation and delivery remain separate and unclaimed.

Worktree: `F:/Dev/LamuFlix.worktrees/feature-451-spec`; branch `feature/451-spec`; intake HEAD `010e0cb63a52e3689d9f246c4179e9bb30959738`. Facts come solely from `recon-DEV-451`; missing facts require `needs recon: <question>` to Bernstein. Full questions, answers, rationale, implications and supplied Patron receipts are in [CONCLUSIONS.md](CONCLUSIONS.md). Receipt control characters are preserved in JSON-escaped form; readable ruling summaries retain the identifiers from recon and Patron messages.

## Every grill answer

1. **Q1 ACCEPT — preserve mutation policy.** Exclude the pre-existing dirty `harness.yml` removal of `LamuFlix.Tests.Common` from DEV-451. Bernstein/Rigger arrange ownership and restoration to intake HEAD before implementation. Keel does not edit that file. Final PR must leave all `gates.mutation` values unchanged. Basis: ticket AC3 and scope item 4; recon §1:13–28, §2:37,44. This is compliance cleanup, not a scope amendment or owner checkbox.
2. **Q2 ACCEPT — measurement set and acceptance ticket.** Measure matched baseline/candidate runs for DEV-444 (`00799914`), DEV-448 (`6fcbd1ff`) and DEV-333 (`b25ecb16`). Patron picks **DEV-444** for the strictly **under 30 minutes** local pre-PR acceptance run. Freeze each case's changed-file diff and code/test tree; use the same runner machine, concurrency and timeout for each pair. Capture live baseline before lever selection, with timing JSON, mutation receipts, wall time, per-project mutant counts, eligible test projects and timeout/verdict distribution. No historical timing is invented. Basis: ticket scope item 1 and ACs; recon §2:34,44, §3:48–50, §4:54–60.
3. **Q3 ACCEPT — measurement-gated lever selection.** Current `mtp` / `coverage-analysis: off` is the control. Per-test coverage is only a bounded experimental candidate, not an adopted solution: ADR 0016 records 42 minutes, 45 restarts and survivor-to-timeout inflation; `all` was slower. Changed-file globs and direct-reference test filtering are already baseline, `since` stays forbidden, and concurrency stays at or below half the runner cores. Build measures first, experiments second, and obtains explicit Patron ratification with matched speed and verdict-integrity evidence before adoption/Phase 5; amend ADR 0016 where applicable. Unsupported runner/mutator candidates require targeted recon and a further Patron decision. Fail closed if none succeeds. Basis: ticket scope items 1–4; recon §2:34–37, §6:95–102, §7:117.
4. **Q4 ACCEPT — split the CRLF span defect.** Use ticket item 6's explicit split option for PR #297 / `MmrSelector.cs` LF/CRLF span-offset defect. Rigger checks existing tickets, folds into a matching issue or files the broken-behavior follow-up, verifies its ID and links it before handoff. Leave `.gitattributes` and `src/**/*.cs` unchanged. The new diff-only test must not claim to fix this defect. Basis: recon §2:39, §5:81, §6:104, §7:117. No owner checkbox is needed.
5. **Q5 ACCEPT — test design, edit boundary and upstream sync.** Extend the existing Contoso fixture in `scripts/Test-RunMutation.ps1` with changed/unchanged production `.cs` siblings in one project. Assert exact changed-file-only generated `mutate`, `since.enabled=false`, and full target only with explicit `-WholeProject`, covering dry-run and stubbed real invocation. Preserve all existing cases and pass the full suite. Code edits are limited to the three ticket-named scripts as needed. Mirror changed scripts to `dotnet-agent-harness/packs/dotnet/scripts`; require exact SHAs and verified parity merged or confirmed on its `origin/main` before delivery. Real Q2/Q3 mutation runs remain mandatory. Basis: ticket scope items 5 and 7, ACs; recon §2:38,40,44, §5:74,79–81,91.
6. **Q6 ACCEPT AND SHARED UNDERSTANDING CONFIRMED — close grill.** Adopt the closing bar, frozen scope, two-round review/remediation cap and task order below. Record Q1–Q6 here and in CONCLUSIONS.md. Build measurements and lever selection are blocking future tasks; no ADR amendment is drafted now. Basis: ticket scope and ACs in recon §2:34–44; runtime evidence absence §3:48–50; ADR evidence §6:95–100; harness verification discipline.

## Loop discipline

**Closing bar:** zero unresolved accepted Critical, High or Medium findings; Low findings receive explicit dispositions; all applicable required gates and ticket ACs satisfied. Scope-empty, opt-out, N/A and Could not run are reported using the actual gate semantics and never relabeled PASS. Required unrunnable or failed gates block. The under-30-minute acceptance is a real measured run, not the script fixture.

**Frozen scope:** three-case matched measurement and DEV-444 under-30-minute acceptance; a measured and Patron-ratified lever beyond DEV-435 preserving mutation policy and verdict integrity; exact diff-only and explicit whole-project regression proof; verified upstream parity for changed scripts; linked CRLF follow-up; before/after PR evidence. Exclude post-merge whole-Core DEV-409, threshold changes, production C# and web changes, line-ending normalization and unrelated improvements; anything else is a follow-up issue, not a finding in this round.

**Round cap:** maximum two review/remediation rounds. At cap, unresolved blockers halt and return to Bernstein. Never create an extra immutable review merely to repair encoding or a receipt. Grill itself closed after six questions, below the 12-question cap.

## Approach and safeguards

Measure first, then select a concrete speedup from evidence. No present speedup, score or runtime success is asserted. For Core, recon identifies both `LamuFlix.UnitTests` and `LamuFlix.Evaluation` as eligible; preserve the eligible-project rule and both suites. Do not omit tests to claim a faster gate.

Preserve threshold 80, maxKillShare 50, maxKillFileSpan 15, the exemption list, all four policy exclusions, default half-core concurrency ceiling and `additional-timeout: 90000`. Never raise concurrency or relax integrity rules, shrink scope or convert survivors to timeouts to meet the target. `-WholeProject` remains explicit. Stryker `since` remains disabled in linked worktrees. Preserve nonzero-tested, actual kills, scope containment, score, timeout, kill-share and file-span safeguards; inspect timeout distribution rather than accepting Stryker's score alone.

Any departure from ADR 0016 needs explicit Patron ratification and an evidence-backed amendment before Phase 5. Generated `stryker-config.json` is never hand-edited; any needed generated configuration change goes through the upstream renderer/install after the approved ADR/config decision, while ticket AC3 remains binding. If a candidate cannot fit these constraints, stop for a decision rather than silently expanding the file set or changing ticket delivery. No new ADR is drafted without a Conductor-assigned number when required.

## Files and artifact boundaries

| Surface | Intended treatment |
|---|---|
| `scripts/run-mutation.ps1` | Conditional implementation of the selected, ratified lever; retain scope and verdict safeguards |
| `scripts/_harness-config.ps1` | Conditional support only if the selected lever requires it within unchanged policy |
| `scripts/Test-RunMutation.ps1` | Existing-fixture regression additions and full suite |
| `dotnet-agent-harness/packs/dotnet/scripts` counterparts | Required mirror for any changed ticket-named scripts; external repository, verified origin/main evidence |
| `specs/DEV-451/` | Brief, full conclusions, Quill artifacts, bounded measurement/receipt evidence and dependencies |
| `docs/adr/0016-stryker-mtp-runner.md` | Conditional measured amendment after Patron ratification; none now |
| `harness.yml` | Owner cleanup prerequisite; final gates.mutation values unchanged |
| `stryker-config.json` | Generated output only; never hand-edit |
| `.gitattributes`, `src/**/*.cs`, `web/`, post-merge whole-Core job | No implementation changes in DEV-451 |

No new dependency, project, architectural layer, schema, public API or LocalPlay behavior is approved. No taste ruling or new domain term was needed; no ASSUMPTIONS.md or CONTEXT.md change is required by this grill. No new ADR is required at this stage.

## Test and evidence strategy

- Existing PowerShell fixture proves exact file selection on both ordinary and explicit whole-project paths, with an unchanged sibling as a negative control; retain existing classification, report containment, share/span, runner, timeout and timing cases.
- Run the complete `scripts/Test-RunMutation.ps1` suite after implementation. Report actual results; stubbed invocation is configuration/regression proof only.
- Capture paired baseline/candidate `mutation-timing.json` and receipts for all three selected cases, identifying source diff/base/head, script/config versions, machine, concurrency, timeout, project test lists, counts, outcomes and wall times. Keep each pair on the same code/test tree and frozen diff.
- Reject a speedup attributable to missing scope/tests, policy relaxation or verdict inflation. Explicitly investigate survivor-to-timeout conversions and abnormal timeout/share/span behavior.
- Require a real DEV-444 run strictly below 1800 seconds, eligible suites intact, and all mutation integrity checks satisfied. Include a before/after table for all three changes in the PR description, linking durable artifacts.
- Run applicable repository gates at the Build stage and retain honest per-gate dispositions; a script-only ordinary diff may be scope-empty for production C# mutation, but this cannot replace the ticket's explicit Core measurement runs.
- Verify changed-script parity against the external harness origin/main with exact SHAs and receipts; local copies or a pending PR are not merged evidence.

## Task ordering and phase boundaries

1. Bernstein/Rigger resolve ownership and restore the pre-existing dirty policy removal; Rigger supplies the verified CRLF follow-up link. Both are recorded before the applicable handoff/implementation boundary.
2. Quill drafts spec/plan/tasks from this brief. Keel performs read-only cross-artifact analysis and brief comparison, followed by the normal plan challenge. Do not call the plan frozen early.
3. Owner Gate 1 and spec merge precede Build implementation. This Spec workspace does not run code/tests or create a Build branch.
4. Build performs live baseline measurements for the three frozen cases. Missing historical timings are expected work, not fabricated evidence.
5. Evaluate a concrete non-DEV-435 candidate under the Q3 experimental contract. Seek targeted recon for unsupported alternatives. Obtain Patron ratification and the required ADR amendment before adoption/Phase 5. Failure to select a sound lever blocks completion.
6. Implement the ratified lever and regression proof within the allowed script set; coordinate upstream mirror and exact parity evidence. Preserve generated-file discipline and unchanged mutation policy.
7. Complete matched candidate runs and DEV-444 acceptance, applicable gates, raw receipts and the three-row before/after PR table. Verify all delivery dependencies.
8. Perform bounded exact-head review/remediation under the two-round cap, post/read back required PR receipts and stop at awaiting-merge. The user merges.

## Operational dependencies

- Dirty harness.yml cleanup: requested from Rigger; verified receipt pending at initial brief creation. Never claim it restored without receipt.
- CRLF follow-up: requested from Rigger under Patron Q4; verified issue ID pending at initial brief creation. Must be linked before handoff.
- Runtime timing/receipt artifacts and selected lever: Build-stage blocking work, not found in the recon worktree and not measured in Phase 2.
- External harness origin/main parity: Build delivery dependency; recon §5:91 cannot establish it from this worktree.

No ticket contradiction or constitution departure was adopted, so this grill creates no structural owner checkbox. Any later proposal that does either must use the charter escalation path; Patron recommendation cannot close an owner checkbox.

## Handoff status after grill closure

Grill decisions and both Keel artifacts are complete and read back. Handoff to Quill is held for the required verified CRLF follow-up ID. Rigger remains responsible for the operation; its displayed creation dry-run returned exit 2 because the existing YouTrack Order sequence is not contiguous. No ticket ID is claimed and no broader tracker repair is authorized by this brief. Bernstein must coordinate this operational blocker. Dirty-policy restoration has not yet been accepted as verified by Keel; await the explicit Rigger receipt. This does not reopen Q1–Q6 or authorize implementation.

## Plan challenge round 1 — Patron decisions (2026-11-04)

Patron ACCEPTED recommendations 1–7. These decisions refine Q1–Q6; they do not approve Gate 1, a concrete lever, measured success, or implementation. Full cited ruling is appended in CONCLUSIONS.md; all 23 challenger dispositions are in Maestri note adjudication-DEV-451. Quill receives one eight-item consolidated repair list.

1. **Experimental feasibility:** T004 must specify a feasible reversible non-shipping scratch experiment with unchanged policy, gate-compatible mutation-timing.json and actual EvaluateReport receipts before T005. Tracked shipping lever scripts remain untouched until T010/T011 (independent Q5 fixture remains as previously authorized). The mtp/non-off GATE NOT WIRED guard must not be silently bypassed or relaxed in shipped code. Retrying ADR0016-rejected perTest/all, or unsupported runner/mutator modes, needs targeted recon and explicit Patron experimental authorization. Missing mechanism facts stop as needs recon before experiment.
2. **Integrity and single acceptance sample:** compare per-mutant baseline/candidate statuses by stable source/mutation identity; archive matrix, timeout totals and raw reports; reject any positive Survived-to-Timeout or Killed-to-Timeout conversion or unexplained timeout growth. Pin/reuse actual concurrency integer within half cores and unchanged runtime timeout, same machine under quiet-load control, without changing gates.mutation. DEV444 recommendation-test span exemptions weaken that signal: inspect kill-share and per-mutant statuses, preserving all safeguards. T013 validates the single T012 DEV444 sample; never rerun to cherry-pick time. Invalidated attempts remain archived and replacement needs documented reason plus Patron ruling.
3. **Historical replay:** Bernstein/Rigger provision authorized detached replay at each chosen historical SHA with frozen base/head, code/test hashes, mutate files and eligible test-project list. Overlay only versioned gate/tool/config support identified in recon section3:49, recording versions/hashes; hold all except candidate constant. Build makes no out-of-worktree writes or invented replay commands; missing mechanics require recon. DEV444 acceptance reuses the frozen replay case.
4. **Boundary and ADR applicability:** entire gates.mutation is frozen. Candidate must be implementable within named script surfaces; unnamed renderer/install/config edits STOP before ratification for cited Patron ruling, with user escalation only under charter two conditions. Any departure from ADR0016 Decision or DEV410 safeguards requires in-place ADR0016 amendment/approval at T010 before T011. No-amendment is valid only for zero departure, never merely lack of authorization. Existing0016 amendment needs no new number; a new superseding ADR does. No renderer/install edit is implicitly authorized by prior brief wording.
5. **External parity:** Bernstein arranges Rigger authorized external harness session; no guard workaround. Mirror changed scripts and confirm-or-merge all THREE scripts on origin/main with exact SHA/parity receipts, including unchanged counterparts, before awaiting-merge/delivery. This clarifies ticket AC5.
6. **Ordering retained:** DROP Ledger M4 proposed unblocking of fixture from T022. Existing Q4/Q6 T021/T022-before-T000 and verified follow-up-before-handoff ordering stays in force. Operational work remains Bernstein/Rigger-owned; verified receipts may be recorded by Keel as CONCLUSIONS owner in the Build copy after spec merge.
7. **Verification/hygiene:** explicitly list run-property-tests.ps1 and actual exits, with N/A mutation-only; source numeric policy snapshots from harness/config and strict under30m from ticket, record actual runtime values and stop on incompatible drift. Cite constitution Pull Request Quality Gates and Static-Analysis Gates. Explain phase labels and intentional task-ID ordering; P markers remain explicitly unused, no parallel writes to shared replay inputs. Evaluate compatibility of recon-listed external docs/skills/CI at candidate selection; required unnamed edits STOP for ruling. Patron expressly authorizes deletion of Quill own _quill-ask-keel-fix1.txt and _quill-ask-keel-fix2.txt; retain DRAFTING_RECEIPT.md and Rigger FOLLOW-UP-TICKETS.md. No speculative consumer rewrite or tracker repair is authorized here.

Basis: ticket scope/ACs in recon-DEV-451 section2:34–44; runtime replay precedent section3:48–50; mutation mechanics section5:67–91; measured ADR/DEV435 failures section6:95–102; boundaries section7:108–117; prior Q1–Q6; constitution Development Workflow/Governance. Readiness remains pending Quill repair and direct cross-artifact read-back.

## PLAN FROZEN — plan challenge closure (2026-11-04)

Keel independently read back all eight Quill repair items against this brief, the full Patron ruling, spec.md, plan.md, tasks.md, drafting receipt and checklist. All 23 Sentry/Ledger/Compass findings have cited dispositions in adjudication-DEV-451; zero unresolved accepted Critical/High/Medium findings and every Low disposition explicit. Quill plan-challenge repair round1 sufficed; no second challenge repair round or extra immutable review was needed. The earlier drafting/analyze repair receipts remain historical, not permission to exceed the standing review cap.

Read-only speckit-analyze CLEAN: 12 FR + 7 SC = 19 requirement keys, 23 unique tasks T000–T022, 100 percent planned coverage, zero unmapped tasks, zero remaining ambiguity/duplication/constitution-alignment/ordering findings. Native prerequisite check located the correct FEATURE_DIR; this verdict is the documented read-only cross-artifact analysis, not a fabricated native analyzer exit or an implementation gate pass. No before-analyze/after-analyze hooks registered in the inspected extensions file.

**PLAN FROZEN. Ready for Patron Gate1 review. Gate1 is NOT approved.** This closure supersedes earlier Draft / PLAN NOT FROZEN status sentences in the artifact snapshots and prior handoff-status paragraphs only for plan-challenge status. It does not authorize implementation, adopt a concrete lever, assert a measured speedup, complete any runtime gate, or waive operational prerequisites.

Build still requires ordinary Gate1/spec merge, verified T021 cleanup receipt and T022 linked CRLF issue before T000, fresh pickup recon/reanalysis, supported historical replay and scratch mechanics, live measurement, Patron candidate ratification and applicable ADR checkpoint, real native-gate acceptance, required gates and authorized external all-three-script parity. The task note reports Q1 cleanup done, but no additional runtime/cleanup evidence is invented by this plan freeze; verified file receipt remains part of T021. T022 follow-up ID remains pending in the Rigger-owned artifact; no tracker repair is authorized here.

Quill round1 completion report `_quill-ask-keel-planfix1.txt` is retained as bounded review evidence for this adjudication. The two obsolete scratch files expressly authorized for deletion are absent. No production code/config edits, tests, commits, PR publication or merge performed by Keel in this ask.
