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
