# DEV-397 — Patron grill rulings

2026-10-04. Requested by Keel: ten recommended decisions from recon-DEV-397, on feature/397-spec in F:/Dev/LamuFlix.worktrees/feature-397-spec. Recon measurements are trusted evidence, not rerun by Patron. No taste decisions are made in this exchange. Q1 and Q9 remain owner decisions; Gate 1 stays closed.

---

## Q1 — Api policy and reserved owner choice

RULING: ACCEPT planning on (a), with Api reason `host proof lives in IntegrationTests per Constitution IX`; blocked: structural — does the owner choose (a) the configured Api exclusion or (b) Api unit tests plus the UnitTests project reference? Carry that explicit choice as an unchecked spec-PR checkbox. Planning on (a) does not answer it or open Gate 1.

- Basis: DEV-397 ticket Scope 1 expressly reserves the choice to the owner; constitution IX, .specify/memory/constitution.md:286-290, locates API host proof in IntegrationTests and mutation in UnitTests.
- specs/DEV-308/CONCLUSIONS.md:426 supports the recommendation but was specific to DEV-308 and supplies neither a standing Api exclusion nor a gate waiver.

---

## Q2 — Exclusion configuration and parser scope

RULING: ACCEPT `gates.mutation.exclusions` as a map from project name without `.csproj` to a reason string, using the same names as `-Project`. Approve the necessary `_harness-config.ps1` edit: dots in each direct map key belong to the project name, not another configuration level; unknown keys elsewhere remain errors. Validate trimmed, nonblank string reasons and reject empty reasons with exit 1. Under pending policy (a), list only `LamuFlix.Api`; Worker and Web remain unlisted and fail closed.

- Basis: DEV-397 Scope 1(a), Scope 2, and AC1-AC3 require configured named exclusions and fail-closed unlisted projects; recon-DEV-397:102-109 and scripts/_harness-config.ps1:137-192 establish that its parser must change to implement them.
- The parser edit is forced by those acceptance criteria and authorized by the assigned-role §2.3 file-scope rule; no new dependency, general YAML parser, or Worker/Web policy is needed.

---

## Q3 — Mutation exit table

RULING: ACCEPT the table for real runs: exit 0 PASSED only when at least one eligible changed project is mutated and every eligible result meets the configured threshold, with each configured exclusion printed as NOT APPLICABLE plus its reason and no unlisted ineligible project; exit 1 FAILED for below-threshold scores, Stryker failures, invalid configuration, or any unlisted ineligible project; exit 2 SKIPPED for no changed production C# under src/ (scope-empty, blocking, never green); exit 2 NOT APPLICABLE only when every changed project is explicitly excluded (non-blocking N/A, never PASS). DryRun success is a classification/configuration check, never mutation proof.

- Basis: DEV-397 Scope 1(a), Scope 2, Scope 4 and AC1-AC3/AC5; AGENTS.md:124-131 distinguishes failure, blocking scope-empty, and configuration-driven non-blocking opt-out.
- Read the threshold from harness.yml, currently 80 (recon-DEV-397:88-100); no threshold reduction or hardcoded substitute is authorized.

---

## Q4 — Mixed eligible and unlisted ineligible projects

RULING: ACCEPT classify-all, run every eligible project, report their scores, then exit 1 naming every unlisted ineligible project. Under DryRun, print every changed project's eligible tests, configured exclusion reason, or `no eligible test project, not in policy`; return 1 if any unlisted ineligible project exists. Invalid configuration can fail before execution; a valid but missing policy entry cannot suppress eligible runs.

- Basis: DEV-397 Scope 2 expressly requires running every eligible changed project despite an ineligible neighbor; AC2-AC3 require the failing fixture/dry-run and complete project classifications.

---

## Q5 — Stale and unmatched exclusions

RULING: ACCEPT as a structural policy decision: a valid listed project remains excluded even when eligible tests exist, with a WARNING that the exclusion may be stale; a valid entry matching no changed project produces no project-specific output. Validate all configured entries first, so unmatched entries do not conceal malformed or empty reasons. The warning alone does not change the exit result.

- Basis: DEV-397 Scope 1(a) makes configured policy authoritative; Scope 4 requires transparent outcomes. A warning exposes newly available test coverage without silently changing the owner's configured policy; Q2's configuration validation applies to the whole map.

---

## Q6 — PowerShell test harness and execution evidence

RULING: ACCEPT scripts/Test-RunMutation.ps1 as a dependency-free harness following the existing scripts/hooks/Test-*.ps1 style, with a temporary checkout, overlaid gate files, and temporary harness.yml. Accept the five recommended cases: listed Api plus Infrastructure; listed Api only, exit 2 NOT APPLICABLE; unlisted Api plus Infrastructure, exit 1 with both classifications; empty reason, exit 1 configuration error; listed but eligible, warning plus exclusion. Record the real non-DryRun `-BaseRef 9f92ad1` execution separately; dry-run fixtures do not satisfy real mutation evidence.

- Basis: DEV-397 AC1-AC3 require those classification paths, while AC7 requires recorded gate execution; recon-DEV-397:138-154 records existing plain PowerShell harnesses and the receipt overlay pattern, with no Pester suite.
- This adds a test file under the existing scripts folder, not a new project or test framework; constitution IX:292 and specs/PRODUCT.md:43 give no reason to introduce Pester for this seam.

---

## Q7 — Pipeline note and M-tier mutation

RULING: ACCEPT mutation in Phase 3 step 5 for M and L whenever the diff changes production C# under src/. Keel records the exact note replacement in specs/DEV-397; Bernstein applies it in Phase B and reads it back into the PR body. Exit 1 blocks every tier; non-blocking mutation N/A requires the script's exit-2 NOT APPLICABLE verdict from configured exclusions, never a seat waiver. The existing L-only /architect stage remains separate.

- Basis: DEV-397 Scope 4/AC5 mandates note alignment and forbids repeating the DEV-309 waiver; ticket Size and order expressly cites DEV-396's recorded mutation requirement. recon-DEV-397:128-136 and task-pipeline:90-99 locate the missing M-tier gate entry.

---

## Q8 — DEV-309 retrospective run and survivors

RULING: ACCEPT Phase B recon establishing DEV-309's pre-merge base SHA, then Gauge running `-Project LamuFlix.Infrastructure -BaseRef <verified-sha>` and recording the project score plus the two validators' results in the PR body. Anvil kills survivors in BrowseMoviesQueryValidator and GetMovieDetailsQueryValidator through UnitTests within the implementation box. Every survivor outside those files, or requiring an out-of-box change, is handed to Rigger for a checked-for-duplicates follow-up with an explicit disposition. Follow-ups do not convert a failing applicable gate to PASS.

- Basis: DEV-397 Scope 5 and AC6 expressly authorize the retrospective run and killing or ticketing each survivor; recon-DEV-397:179-183 names the validators and the omitted Infrastructure run.
- Constitution IX:286,290,292-303 supplies UnitTests and its existing testing conventions; the assigned-role tracker rule reserves tracker writes to Rigger.

---

## Q9 — Own-diff green AC conflict

RULING: blocked: structural — may AC7 be replaced with: `All applicable harness gates pass and are recorded with exit codes. Record the own-diff mutation command, verdict and exit code verbatim; when no production C# under src/ changed, its exit 2 SKIPPED is recorded as scope-empty, never PASS or all-green evidence. Validate the changed mutation runner with the real Api-plus-Infrastructure run against 9f92ad1 and the DEV-309 retrospective run, and record their scores and dispositions.` This relaxes the explicit own-diff-green requirement, so it is an owner checkbox, not a wording-only correction for Rigger. Pending that answer, the requirement remains unresolved.

- Basis: DEV-397 AC7 explicitly requires all-green evidence including the own-diff run; AGENTS.md:124-131 explicitly makes scope-empty exit 2 blocking and never green. recon-DEV-397:220-227 demonstrates the conflict for a scripts-only diff. specs/PRODUCT.md:36-38 reserves acceptance-criterion changes to the owner.
- ACCEPT raw own-diff recording and substantive historical runs as the proposed evidence. Use `-Files` on changed C# for the three analyzers; if no C# changes, Roslyn/complexity may use their documented `-All`. Do not use InspectCode `-All`: task-pipeline:94 expressly forbids it. Record its default scope-empty result without claiming a pass; any remaining all-green conflict stays visible to the owner.

---

## Q10 — Frozen implementation scope

RULING: ACCEPT the recommended frozen box, contingent on Q1/Q9 owner decisions: run-mutation.ps1 classification, outcomes, stale header/message and help; _harness-config.ps1 map support; harness.yml Api policy; AGENTS.md mutation exit table; scripts/Test-RunMutation.ps1; survivor-killing DEV-309 validator UnitTests; and exact task-pipeline note replacement text. README.md, Cursor/Claude architect rules, Codex agent files, missing harness.yml.example/install.ps1, legacy LamuFlix.Test, Worker/Web policies and new-mutation-receipt.ps1 edits stay outside. Anything else is a follow-up issue, not a finding in this round.

- Basis: DEV-397 Scope 1-5 and AC1-AC7 establish this delivery; Q2/Q6 authorize its required parser and test-harness seams. §2.3 leaves unrelated findings outside this ticket unless the user changes scope.
- Static compatibility check: scripts/new-mutation-receipt.ps1:337-351 captures the raw gate exit and the final `Mutation testing:` verdict, and separately parses `Stryker native exit for <project>.csproj:`. Preserve those output seams. Its :353-359 requires an actual report/config and fails without them, so excluded projects remain unmeasured rather than successful receipts. No helper edit is required; runtime compatibility remains Phase B verification.

---

## Follow-up candidate — gate-runner mutation N/A label

RULING: noted, no ticket. Log the missing gate-runner NOT APPLICABLE bucket here; Rigger need not file a follow-up on the evidence supplied. No incorrect gate report or Critical/High finding was supplied, so this terminology gap does not establish broken behaviour.

- Basis: assigned Patron role, tracker follow-up rule, permits out-of-scope tickets for Critical/High findings, broken behaviour, or an explicit user request; other findings are recorded as noted, no ticket. Conductor requested adjudication, not mandatory filing.
- .cursor/rules/delegation.mdc:32-33 lists Pass, Failure, Skipped, Opt-out and Could not run; specs/DEV-397/spec.md:116-117 distinguishes blocking exit-2 SKIPPED from non-blocking exit-2 NOT APPLICABLE, never PASS. That contract remains authoritative for the parent interpreting a gate-runner report.
- specs/DEV-397/spec.md:119-121 and brief.md section 3 freeze the implementation box and exclude Codex agent edits. This ruling adds no implementation work and answers neither OD-1 nor OD-2.

## Gate 1 provisional — spec PR handoff

RULING: ACCEPT setting `gate1: provisional` and opening `DEV-397: spec`; OD-1 and OD-2 remain unchecked owner decisions. Gate 1 stays closed until both owner decisions are answered and the user merges the spec PR; this marker grants no Phase B authorization.

- Basis: task-pipeline:72,75-76; Conductor and task note DEV-397:23-25 confirm Quill fix commit `298e3a3`, clean re-analyze (18/18 FRs, 26 tasks, no critical/high/medium), adjudicated plan challenge and freeze recorded in brief.md section 11 at `067d65d`. Patron accepts those verified receipts without re-running another seat's checks.
- Owner basis: spec.md:11-14, brief.md section 2 and PRODUCT.md:34-39. OD-1 retains the explicit Api policy choice; OD-2 retains the exact proposed AC7 replacement. Planning on option (a) and recording scope-empty mutation evidence answer neither checkbox.
- Rigger PR body: copy both unchecked owner checkboxes verbatim from spec.md; cite the clean analysis and frozen-plan commits above; state provisional spec handoff only, Gate 1 closed pending owner answers and user merge, and no Phase B authorization. Do not claim AC7 amended or own-diff mutation green; if OD-1 selects (b), Keel amends the brief before implementation as brief.md section 2 requires. Limit the PR to this ticket's specs and exclude the local .specify/feature.json pin.

## T021 — MetadataProviderHealthCheck survivor disposition

RULING: ACCEPT a mandatory, duplicate-checked follow-up covering all eight MetadataProviderHealthCheck.cs survivors at lines 18, 24, 25 and 29. Rigger folds each survivor into an existing open ticket covering it, or creates one consolidated follow-up under parent epic DEV-284, estimate 2 hours, tag `size:S`; each survivor must have an explicit ticket-linked disposition. T021 awaits Rigger's verified receipt; no health-check implementation is added to DEV-397.

- Basis: DEV-397 ticket Scope 5 and AC6 require killing or ticketing survivors; brief.md:126 and tasks.md:72 explicitly send every survivor outside the two validators to a duplicate-checked follow-up. This is required delivery, not a discretionary severity-based candidate (PRODUCT.md:23-24).
- Evidence: Conductor's completed T019 report and task note DEV-397:45-46 record 88.06% (59 killed, 8 survived, threshold 80, exit 0), zero survivors in both named validators, and all eight survivors in MetadataProviderHealthCheck.cs. Patron accepts Gauge's results without rerunning verification. Parent basis: specs/DEV-307/spec.md:9 and brief.md:3 identify DEV-284 as the hosting-defaults epic.
- Rigger records the exact eight mutant identifiers and changes from the saved T019 report, with a disposition for each; follow-up creation is not mutation proof and cannot convert the separately recorded T018 Core failure into PASS (brief.md:126; task note DEV-397:44).

## T021 — Reconcile DEV-398 and DEV-399

RULING: ACCEPT DEV-398 as the retained follow-up; resolve DEV-399 as its linked duplicate (resolved Canceled if the project has no resolved Duplicate state). Rigger repairs DEV-398 in place to Type Task, parent DEV-284, estimate 2h and tag `size:S`, then posts the retained link on DEV-397. Do not delete either ticket or retry creation. T021 awaits Rigger's verified reconciliation receipt.

- Basis: tasks.md:72 and the T021 survivor disposition above require one duplicate-checked follow-up covering all eight health-check survivors; Conductor's Wisp recon establishes identical summary/description in DEV-398 and DEV-399 and missing metadata after the command HTTP 400. Retaining the first issue and resolving the duplicate gives one actionable disposition without changing ticket delivery (PRODUCT.md:23-24).
- Operational basis: scripts/local/Edit-YouTrackIssue.ps1:38-83 restricts Parent/Estimate to Create, and :253 bundles metadata into the failed command. Rigger uses its supported Edit mode for comments/tags and, where Edit cannot represent the repair, a disposable TEMP wrapper over the existing _youtrack.ps1 helper for documented issue/field/link updates against these existing IDs. No tracked helper change is authorized. [YouTrack custom-field updates](https://www.jetbrains.com/help/youtrack/devportal/api-how-to-update-custom-fields-values.html), [period values](https://www.jetbrains.com/help/youtrack/devportal/api-concept-custom-fields.html), and [issue links](https://www.jetbrains.com/help/youtrack/devportal/resource-api-issues-issueID-links-linkID-issues.html) define the update seam; discover field, bundle, state and link IDs live.
- Completion: verify DEV-398's four required metadata values, DEV-399's resolved state and directed duplicates link to DEV-398, and the exact DEV-397 comment after posting. Preserve the supplied survivor description; creation or reconciliation is not mutation proof and does not turn the separately recorded Core failure into PASS. Rigger alone writes YouTrack (assigned Patron role, tracker rule).

## T021 — Available resolved state for DEV-399

RULING: AMEND the reconciliation state fallback to Closed, conditional on live verification that Closed yields `isResolved=true`. DEV-399 remains linked Duplicate of DEV-398; its duplicate comment records why it is closed. If Closed is unresolved or the workflow rejects it, Rigger reports the blocker; no bundle or workflow change is authorized. T021 remains pending the full verified reconciliation receipt.

- Basis: Rigger's verified reconciliation report establishes DEV-398's required metadata, the directed DEV-399 duplicate link, and a State bundle containing Todo, In Progress, Done, Blocked, Code review and Closed, with neither Duplicate nor Canceled. Resolution semantics for Closed remain unverified.
- Basis: tasks.md:72, brief.md:19,126 and CONCLUSIONS.md:120-124 require one actionable follow-up and explicit duplicate disposition. Using the existing Closed state with a duplicate link and reason preserves that delivery (PRODUCT.md:23-24); it claims no implementation of DEV-399's duplicated work.
- Completion: Rigger verifies Closed and `isResolved=true`, the duplicate link to DEV-398, and the exact duplicate comment before posting and reading back the supplied DEV-397 disposition comment. Prior metadata receipts stand; no mutation result changes.

## Own-diff mutation scope — Phase 3 adjudication

RULING: blocked: structural — the existing ticket/AC and frozen scope provide no evidenced in-scope remedy that makes this ticket's own-diff mutation gate applicable; retain exit 2 SKIPPED as blocking. Patron recommends the existing Q9/OD-2 AC7 amendment below for the owner's decision. This ruling does not answer that checkbox or authorize additional production work.

- Basis: DEV-397 ticket Scope 1-4 and AC1-AC5 deliver runner/configuration/documentation changes; Scope 5 and AC6 authorize the DEV-309 retrospective and survivor disposition. Q8/Q10 above and spec.md:69-75 constrain in-ticket remediation to the two validators. DEV-397:47 records zero survivors there; DEV-397:55 identifies the separately measured Core and health-check survivors, not a needed validator production change. Adding tests, including the Core survivor tests, does not add changed production C# under src/. AC7 requires green own-diff evidence but supplies no independent production behavior change to implement; it does not justify a no-op source edit.
- Evidence and next route: DEV-397:57,65,68 establish zero changed production C# and the blocking own-diff exit 2 SKIPPED; task-pipeline:95 and spec.md:59 preserve that verdict. T018 against 9f92ad1 is separate runner-behavior evidence, not this gate's scope or result, and DEV-397:55 records its Core 76.92% failure. Preserve both receipts; send the unresolved owner decision to the user on the PR. Do not waive SKIPPED, relabel it N/A/PASS, lower thresholds, substitute the historical base, or add production edits just to populate mutation scope. Any substantive production addition requires its exact delivery scope to be owner-approved first.
- Owner checkbox (existing Q9/OD-2, still open): [ ] `blocked: structural — may AC7 be replaced with: "All applicable harness gates pass and are recorded with exit codes. Record the own-diff mutation command, verdict and exit code verbatim; when no production C# under src/ changed, its exit 2 SKIPPED is recorded as scope-empty, never PASS or all-green evidence. Validate the changed mutation runner with the real Api-plus-Infrastructure run against 9f92ad1 and the DEV-309 retrospective run, and record their scores and dispositions."` Recommendation: approve this explicit acceptance change while preserving raw gate results and every separate applicable failure. Ticket AC7 (description line 31), spec.md:14 and Q9 above require the owner's answer; the scope ruling cannot supply it. No current owner answer was supplied in this ask or verified here.

## Phase 4 round 1 — SafeDescription contract follow-up

RULING: ACCEPT one duplicate-checked follow-up to pin all four `EnrichmentFailureCategory.SafeDescription` members (`ProviderUnavailable`, `RateLimited`, `InvalidResponse`, `Unknown`) in `LamuFlix.UnitTests` as a single `TheoryData`-backed `Theory` with `MemberData` and Shouldly. Rigger checks open issues and folds this complete scope into an existing matching issue, or creates one Task under parent epic DEV-282, estimate 1 hour, tag `size:S`; link its verified disposition to DEV-397. This covers accepted Sentry R1 and Ledger L1 and targets prior T018 Core survivor IDs 1, 4, 7 at `EnrichmentFailureCategory.cs:7,12,17`. Filing or implementing the follow-up is not mutation proof; DEV-397 frozen scope and the recorded T018 Core 76.92% FAIL (exit 1) remain unchanged. Tracker recording is pending Rigger; no code change or issue creation is performed by Patron.

- Evidence: `findings-DEV-397-Sentry:51-53` identifies the unpinned Unknown caller-safe contract; `findings-DEV-397-Ledger:7-9` identifies the repeated Facts; Keel's accepted FOLLOW-UP adjudication and PR review receipt are at `DEV-397:87`. `DEV-397:55` records the three empty-string survivors and measured Core failure; targeting those IDs does not establish they are killed.
- Scope basis: `brief.md:45`, Q8/Q10 and `CONCLUSIONS.md:138` limit DEV-397 test remediation to the two DEV-309 validators. The explicit Conductor follow-up request and assigned Patron tracker rule authorize this separate issue, with Rigger alone performing duplicate checks and tracker writes; they authorize no expansion of the frozen implementation box.
- Design and metadata basis: constitution `.specify/memory/constitution.md:181-184,286,292-303` names all four caller-safe members, places domain tests in UnitTests, and requires the existing xUnit/Shouldly and Theory/MemberData conventions. `specs/DEV-295/spec.md:5` locates the taxonomy's parent epic at DEV-282. The four-row contract test is a small task (Patron estimate 1h, `size:S`); no dependency, production behavior, or threshold change is included.

## Phase 5 merge bar — approved OD-2 and scope-empty gates

RULING: blocked: structural — no remedy within DEV-397's approved delivery scope clears the Phase 5 merge bar without changing gate policy or adding unrelated work. Park DEV-397 at Phase 5 step 6, blocked pending an explicit owner decision; PR #87 stays draft/open, not merge-ready or awaiting-merge. The checked OD-2 is accepted as final and is not reopened. Recommend the narrow, owner-approved merge-bar exception proposed below rather than production edits solely to manufacture gate scope.

- Scope and evidence: ticket Scope 1-5 / AC1-AC6 as carried in `brief.md` §§2-3 and `CONCLUSIONS.md:138` authorize runner/configuration/documentation work and the named validator retrospective, not an independent production change. `DEV-397:121,135` verifies the final diff has zero C# files, findings are closed, and both Gate3 InspectCode and Gate8 own-diff mutation are exit 2 SKIPPED scope-empty and blocking. Adding or moving tests cannot create production mutation scope; neither `-All` InspectCode (forbidden by `task-pipeline:94`) nor the historical mutation base substitutes for the own-diff receipts. Follow-ups DEV-398/DEV-400 are separate delivery, not gate proof (`DEV-397:118-119,131`).
- Acceptance and authority: the checked OD-2 is present and verified (`DEV-397:133-135`; exact answer in `spec.md:14`): it requires recording scope-empty SKIPPED, never PASS or all-green, plus real-run scores and dispositions. It does not grant a Phase 5 exception. `brief.md:101` keeps mutation SKIPPED blocking; `task-pipeline:124-129` requires exit 0 or accepted N/A before awaiting-merge. Preserve Keel's NOT MET receipt, both blocking classifications, and the distinct T018 Core 76.92% FAIL exit 1 (`DEV-397:55,131`). Constitution IX and Static-Analysis Gates (`.specify/memory/constitution.md:290,375-391`) do not turn a skipped run into a pass; no constitution amendment is proposed because no new or modified C# is being accepted without its required analysis. This is a proposed delivery acceptance/merge-bar exception, not a gate reclassification; the assigned Patron §2.3 owner-only rule reserves a change to ticket delivery acceptance to the user.
- Next action / proposed PR checkbox for Rigger, only when publication is authorized: **[ ] blocked: structural — Do you authorize a DEV-397-only exception to the Phase 5 merge bar for PR #87's approved runner/configuration/documentation diff with no C# changes, accepting delivery on the already checked OD-2 evidence and recorded survivor dispositions while Gate3 InspectCode and Gate8 own-diff mutation remain exit 2 SKIPPED scope-empty, blocking and unpassed? This exception changes delivery acceptance only: it does not label either gate PASS/N/A, claim all-green or merge-ready, change classifications or thresholds, add production scope, erase T018 Core 76.92% FAIL exit 1, or authorize an agent to merge.** Recommendation: approve that narrow exception if the owner wants this tooling delivery to land; otherwise keep it parked. Until explicitly answered, no exception exists and no PR/tracker/state transition is authorized. After an answer, Conductor coordinates Rigger's exact decision receipt on the PR and Keel's reconciliation against that decision; the user alone merges. No PR or tracker edit is performed by this ruling.
