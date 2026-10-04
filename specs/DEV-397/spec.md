# Feature Specification: Mutation Runner Api Diff Support

**Feature Branch**: `feature/397-spec`

**Created**: 2026-10-04

**Status**: Draft

**Input**: DEV-397 task brief, Patron rulings in `CONCLUSIONS.md`, and `recon-DEV-397`.

## Owner Decisions (Gate 1 remains closed)

- [ ] **OD-1 (Q1)** — `blocked: structural — does the owner choose (a) the configured Api exclusion in harness.yml or (b) Api unit tests in LamuFlix.UnitTests plus a UnitTests -> LamuFlix.Api project reference?` Planning proceeds on recommended option (a), with reason `host proof lives in IntegrationTests per Constitution IX`; this does not answer the checkbox. If (b) is selected, Keel amends the brief before Phase B as specified there.
- [ ] **OD-2 (Q9)** — `blocked: structural — may AC7 be replaced with: "All applicable harness gates pass and are recorded with exit codes. Record the own-diff mutation command, verdict and exit code verbatim; when no production C# under src/ changed, its exit 2 SKIPPED is recorded as scope-empty, never PASS or all-green evidence. Validate the changed mutation runner with the real Api-plus-Infrastructure run against 9f92ad1 and the DEV-309 retrospective run, and record their scores and dispositions."` Until answered, the original AC7 remains unresolved.

## User Scenarios & Testing

### User Story 1 - Mutate eligible projects in a mixed diff (Priority: P1)

As a maintainer running the mutation gate on a change that touches Api and Infrastructure, I need the gate to run Stryker for Infrastructure while reporting Api as NOT APPLICABLE under its configured policy, so a host project without eligible unit tests does not prevent measurement of the eligible project.

**Why this priority**: This is the ticket's primary failure and closing-bar proof.

**Independent Test**: Run `scripts/run-mutation.ps1 -BaseRef 9f92ad1` without `-DryRun`; confirm Infrastructure is run and scored, Api is NOT APPLICABLE with its configured reason, and the final exit follows the agreed table.

**Acceptance Scenarios**:

1. **Given** Api and Infrastructure changed and Api is listed in `gates.mutation.exclusions`, **When** the real mutation gate runs, **Then** Infrastructure is mutated and scored and Api is reported NOT APPLICABLE with its reason.
2. **Given** Api is listed and Api is the only changed project, **When** the fixture runs with and without `-DryRun`, **Then** no project is mutated and both invocations return exit 2 NOT APPLICABLE.
3. **Given** Api and Infrastructure changed and Api is not listed, **When** the DryRun fixture runs, **Then** it prints both classification lines and exits 1 naming the unlisted ineligible Api project.

### User Story 2 - Understand every changed project's classification (Priority: P1)

As a maintainer using `-DryRun`, I need a classification line for each changed project, so I can check policy and project eligibility without mistaking a classification run for mutation evidence.

**Why this priority**: Complete classification is required to diagnose mixed diffs and validate the fail-closed behavior.

**Independent Test**: Drive DryRun fixture cases and verify every changed project has a classification, including when an ineligible project forces exit 1.

**Acceptance Scenarios**:

1. **Given** a changed project has eligible tests, **When** DryRun runs, **Then** its eligible test-project list is printed.
2. **Given** a changed project is listed in exclusions, **When** DryRun runs, **Then** NOT APPLICABLE and the reason are printed; if eligible tests exist, a WARNING says the exclusion may be stale.
3. **Given** a changed project has no eligible test project and is not listed, **When** DryRun runs, **Then** `no eligible test project, not in policy` is printed and the exit is 1.
4. **Given** configured exclusions contain valid entries that match no changed project, **When** classification runs, **Then** they produce no project-specific output.

### User Story 3 - Configure exclusions and interpret outcomes (Priority: P1)

As a maintainer, I need exclusions to be explicit, validated, and accompanied by consistent exit-code documentation, so configuration errors and unmeasured projects cannot be mistaken for passing mutation evidence.

**Why this priority**: Policy clarity and outcome semantics are necessary for reliable gate use across tiers.

**Independent Test**: Exercise the five PowerShell fixture cases, including blank reason and stale exclusion, and compare the script help and `AGENTS.md` table with the specified exit semantics.

**Acceptance Scenarios**:

1. **Given** an exclusion reason is empty or whitespace, **When** the gate validates configuration, **Then** it reports a configuration error and exits 1 before Stryker.
2. **Given** all changed projects are explicitly excluded, **When** the real gate runs, **Then** it exits 2 NOT APPLICABLE, which is non-blocking N/A and never PASS.
3. **Given** no production C# under `src/` changed, **When** the gate runs, **Then** it exits 2 SKIPPED (scope-empty), which is blocking and never green.
4. **Given** a Stryker failure, below-threshold score, invalid configuration, or unlisted changed project with no eligible test project, **When** the gate runs, **Then** it exits 1 FAILED.
5. **Given** a successful real run, **When** at least one eligible project was mutated, all eligible scores meet the configured threshold, all configured changed exclusions are reported, and no unlisted ineligible project exists, **Then** it exits 0 PASSED.

### User Story 4 - Preserve retrospective evidence for DEV-309 (Priority: P2)

As a maintainer reviewing the earlier DEV-309 Infrastructure validators, I need the retrospective mutation score and dispositions for the two named validators recorded, so surviving mutants receive explicit treatment.

**Why this priority**: The ticket requires retrospective evidence and targeted survivor disposition after the runner supports mixed project changes.

**Independent Test**: Run Infrastructure with `-Project LamuFlix.Infrastructure -BaseRef <verified DEV-309 pre-merge SHA>`, record its score and per-validator results, kill in-scope validator survivors with UnitTests, then rerun the retrospective. Other or out-of-box survivors are duplicate-checked follow-up tickets with disposition; follow-ups do not make a failing gate pass.

**Acceptance Scenarios**:

1. **Given** the verified pre-merge base SHA, **When** Gauge runs the retrospective, **Then** the Infrastructure score and results for `BrowseMoviesQueryValidator` and `GetMovieDetailsQueryValidator` are recorded.
2. **Given** survivors occur in either named validator, **When** implementation adds tests, **Then** those survivors are killed by UnitTests tests.
3. **Given** a survivor is outside the two validators or requires an out-of-box change, **When** it is dispositioned, **Then** it is handed to Rigger as a duplicate-checked follow-up with an explicit disposition.

## Edge Cases

- A listed project may gain eligible tests: continue excluding it and emit a WARNING; warning alone does not affect the exit.
- Validate every exclusion entry before classification, even entries that match no changed project.
- A valid exclusion that matches no changed project remains silent.
- If eligible and unlisted ineligible projects coexist, run all eligible projects and report their scores before returning FAILED.
- DryRun must print every classification and does not invoke Stryker; a clean DryRun is not mutation proof.
- When `-Project` names a listed project, print its NOT APPLICABLE classification and reason, run no Stryker, and exit 2 NOT APPLICABLE. The receipt helper then fails that receipt because no report exists; this is the intended disposition and the helper is not changed.
- Preserve receipt-parser seams: raw gate exit, final `Mutation testing:` verdict, and `Stryker native exit for <project>.csproj: <n>`. Excluded projects have no report and remain unmeasured.

## Requirements

### Functional Requirements

- **FR-001**: The runner MUST accept `gates.mutation.exclusions` as a map from project names without `.csproj` to reason strings; keys follow the `-Project` naming form and match ordinal-ignore-case. A key ending in `.csproj` is a configuration error, exit 1.
- **FR-002**: The harness configuration reader MUST widen the key grammar to allow dotted keys only as direct children of the exact `gates.mutation.exclusions` prefix. The prefix match is anchored on those exact path segments; nesting below a child, a scalar on `exclusions`, and near-miss neighbours such as `gates.mutation.exclusion.*` and `gates.mutations.exclusions.*` remain hard errors. Add one prefix schema entry and an empty-map default in `Get-HarnessDefaults`; provide one accessor that lists all map entries while `Get-HarnessValue` retains its scalar contract. Unknown keys elsewhere remain errors.
- **FR-003**: The runner MUST validate every exclusion entry before classification and before the scope-empty check; empty or whitespace reasons MUST produce a configuration error and exit 1 before Stryker. If exclusions are absent, the map is empty.
- **FR-004**: Planning proceeds with only `LamuFlix.Api` listed, reason `host proof lives in IntegrationTests per Constitution IX`, pending OD-1. Worker and Web remain unlisted and fail closed.
- **FR-005**: For every changed project the runner MUST compute eligible tests using the existing direct-reference rule and continue excluding both `*.ArchitectureTests` and `*.IntegrationTests`.
- **FR-006**: Each changed project MUST be classified as configured exclusion (NOT APPLICABLE plus reason), eligible (test-project list), or unlisted ineligible (`no eligible test project, not in policy`). A listed project with eligible tests remains excluded and emits a stale-policy WARNING.
- **FR-007**: The runner MUST print one classification line per changed project in DryRun and real runs. DryRun MUST NOT invoke Stryker and MUST use the brief §4.2 step 7 exits: 1 for invalid configuration or any unlisted ineligible project; 2 `SKIPPED` when no production C# under `src/` changed; 2 `NOT APPLICABLE` when every changed project is listed; otherwise 0 for classification only, never mutation proof.
- **FR-008**: In real runs, the runner MUST run every eligible changed project, report every eligible score, and only then compute the verdict, even if another changed project is unlisted and ineligible.
- **FR-009**: The runner MUST follow the exit table below, using `gates.mutation.threshold` from `harness.yml` without lowering or hardcoding it.
- **FR-010**: The `:564` failure message and `:12-17` header comment MUST name both `*.ArchitectureTests` and `*.IntegrationTests`; the failure message MUST name `gates.mutation.exclusions` and match code behavior.
- **FR-011**: Script help and `AGENTS.md` MUST carry the exit-code table verbatim: identical header and row text after trimming the leading whitespace on each line. In script help, place the table inside the `<# ... #>` comment-help block.
- **FR-012**: A dependency-free `scripts/Test-RunMutation.ps1` harness MUST overlay the gate files under test, `.config/dotnet-tools.json`, `stryker-config.json`, the solution with its test projects, and git history reaching the merge-base through `HARNESS_REPO_ROOT`. Every case MUST assert both exit code and verdict/classification text. Cover: (1) listed Api + eligible Infrastructure (exit 0); (2) listed Api only (exit 2 NOT APPLICABLE with and without `-DryRun`); (3) unlisted Api + Infrastructure (exit 1 and both classifications); (4) blank reason (exit 1); and (5) listed-but-eligible warning while excluded.
- **FR-013**: The PR body MUST contain the real non-DryRun Api-plus-Infrastructure run against `9f92ad1`, including verdict lines and exit code.
- **FR-014**: The PR body MUST contain the DEV-309 retrospective base SHA, Infrastructure score, per-validator findings, survivor disposition, and rerun evidence after in-scope survivor tests.
- **FR-015**: Preserve the receipt output seams read by `scripts/new-mutation-receipt.ps1`: raw gate exit, final `Mutation testing:` verdict line, and `Stryker native exit for <project>.csproj: <n>`. Excluded projects produce no report and stay unmeasured. Do not modify the receipt helper.
- **FR-016**: Update Phase 3 step 5 of the `task-pipeline` note with the replacement wording in the brief, verbatim; Bernstein applies and reads it back. Keep step 4's L-only `/architect` unchanged.
- **FR-017**: Keep implementation to the brief's frozen scope. Out-of-scope items and changes require follow-up via Rigger if found wrong; they are not findings in this round.
- **FR-018**: AC7 remains unresolved pending OD-2. Until resolved, record own-diff mutation command, verdict, and exit verbatim; expected exit 2 SKIPPED for this scripts-only specification/implementation diff is scope-empty and never PASS.

### Exit-Code Table

| Exit | Verdict | Meaning | Blocking |
|---|---|---|---|
| 0 | `PASSED` | At least one eligible changed project was mutated, every eligible result is at or above `gates.mutation.threshold`, each configured exclusion is printed as NOT APPLICABLE with its reason, and no unlisted ineligible project exists. | — |
| 1 | `FAILED` | A score below threshold, a Stryker failure, invalid configuration, or any changed project with no eligible test project that is not in `gates.mutation.exclusions`. | Yes, on every tier |
| 2 | `SKIPPED` | No production C# under `src/` changed (scope-empty). | Yes; never green |
| 2 | `NOT APPLICABLE` | Every changed project is explicitly listed in `gates.mutation.exclusions`; nothing was mutated. | No; reported as N/A, never PASS |

### Frozen Scope

Only files/responsibilities F1-F8 from `brief.md` §3 are authorized. In particular, do not change README, architect rule files, Codex agent files, missing configuration/install artifacts, legacy test project, Worker/Web policy, receipt helper, Stryker configuration, or threshold.

## Key Entities

- **Changed project**: A production project under `src/` included in the selected base-to-head diff.
- **Eligible test project**: A test project that directly references the changed project, excluding projects whose names end with `.ArchitectureTests` or `.IntegrationTests`.
- **Configured exclusion**: A project-name-to-nonblank-reason entry under `gates.mutation.exclusions` that suppresses Stryker measurement for that project and is reported as NOT APPLICABLE.
- **Mutation result**: Per-project Stryker score and native exit, plus the overall gate verdict and exit code.
- **Survivor disposition**: A named validator survivor killed by a UnitTests test, or an out-of-scope survivor assigned a checked follow-up with explicit disposition.

## Success Criteria

- **SC-001**: A real run against `9f92ad1` mutates and reports Infrastructure, reports Api as NOT APPLICABLE with its reason, and exits according to §Exit-Code Table.
- **SC-002**: All five `Test-RunMutation.ps1` cases pass with expected exit codes and classification output.
- **SC-003**: Every changed project is classified once in DryRun; invalid exclusions fail before Stryker.
- **SC-004**: Script help and `AGENTS.md` contain the same exit-code table; the specified failure and header text name both exclusions.
- **SC-005**: DEV-309 retrospective validator results, score, survivor dispositions, and post-test rerun are recorded in the PR body.
- **SC-006**: Applicable gates and the own-diff mutation outcome are recorded with exit codes according to brief.md §7; the unresolved AC7 conflict remains explicit until OD-2 is checked.

## Assumptions

- None. Taste assumptions are not needed for this tooling feature.
