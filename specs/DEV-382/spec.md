# Feature Specification: Mutation gate linkage fix and receipts

**Feature Branch**: `bug/DEV-382-diagnose-stryker-mutant-linkage-infrastructure-api`

**Created**: 2026-09-28

**Status**: Draft

**Input**: DEV-382: "Diagnose Stryker mutant-to-test linkage for LamuFlix.Infrastructure and LamuFlix.Api". DEV-296 D7d showed Infrastructure 0/40 and Api 0/9 killed. Diagnose and fix the linkage, then produce reproducible receipts: effective config, mutated file list, per-mutant status and killedBy, native exits, and test-host assembly-loading evidence. Preserve the 80 threshold, and do not reclassify zero kills as a test-quality verdict.

Alignment: `brief.md` (closing bar AC1-AC6, frozen scope) and `CONCLUSIONS.md` (Q1-Q5, D1-D5). This spec restates those decisions as requirements and adds nothing to them.

## Clarifications

### Session 2026-09-28

The owner said not to ask more questions (CONCLUSIONS D5), so the recommender resolved the ambiguities in the scan below:

- Q: Should direct or transitive project references decide eligibility? → A: direct only. Stryker's project mode needs the test project to reference the mutated project, and on main and DEV-296 every mutated project that has unit tests is referenced directly. A transitive-only project fails closed (FR-006), which is visible, not silent.
- Q: What is the "clean build" for assembly identity? → A: after the run, `dotnet build` the eligible test project in the Debug configuration (the configuration Stryker builds), then SHA256 the same loaded path.
- Q: What is the default output root? → A: `<system temp>/LamuFlix-stryker/<UTC yyyyMMdd-HHmmss>`. It is printed at the start and in the summary.
- Q: DEV-296 also changes Core. Should the receipts cover it? → A: no. The receipt run uses `-Project LamuFlix.Infrastructure,LamuFlix.Api`. Core re-measurement is the D1 follow-up.

## User Scenarios & Testing *(mandatory)*

The actor is a developer or pipeline agent running the mutation gate (`scripts/run-mutation.ps1`) on a task branch.

### User Story 1 - Gate scores reflect the unit tests (Priority: P1)

A developer changes production code in a project that the unit tests cover, then runs the mutation gate. Every mutant that the tests catch is reported Killed, and each kill names the test that caught it. The score compares against the unchanged threshold of 80.

**Why this priority**: today every mutant survives, whatever the tests do (E2, E4). Until that changes, the gate cannot pass or fail on merit.

**Independent Test**: on a branch whose changes are covered by unit tests, run the gate. The report has Killed mutants whose `killedBy` entries resolve to `LamuFlix.Test` test names. Measured: 63 Core mutants on 5.0.0 MTP gave 60 Killed, 2 Survived, 1 Timeout (E5).

**Acceptance Scenarios**:

1. **Given** changed files in a project referenced by `LamuFlix.Test`, **When** the gate runs, **Then** Stryker runs with the MTP runner and coverage off, and the gate prints the discovered test projects. Every Killed mutant names at least one test.
2. **Given** a score below 80, **When** the gate evaluates, **Then** it exits 1 with the existing "below threshold" reason.
3. **Given** 0 killed out of N tested, **When** the gate evaluates, **Then** it exits 1. The reason calls this a linkage or measurement failure, not a test-quality verdict.

---

### User Story 2 - Artifact kills cannot pass the gate (Priority: P1)

A run where tests "kill" mutants only because Stryker's instrumentation breaks them must not count as a pass.

**Why this priority**: E1 reported 100% only because ArchitectureTests reject the injected `Stryker.*` types. An inflated score is worse than a zero.

**Independent Test**: evaluate the E1 report with `-EvaluateReport`. It exits 1 and names the ArchitectureTests killers. Evaluating the E5 report exits 0.

**Acceptance Scenarios**:

1. **Given** a report where a Killed mutant's `killedBy` resolves to a test in an `*.ArchitectureTests` project, **When** it is evaluated, **Then** the gate exits 1 and lists the offending tests.
2. **Given** a report where a Killed mutant has empty `killedBy`, or ids that don't resolve through `testFiles`, **When** it is evaluated, **Then** the gate exits 1, because artifact kills cannot be ruled out.
3. **Given** a mutated project that no eligible test project references, **When** the gate prepares configs, **Then** it exits 1 before running Stryker.

---

### User Story 3 - Reproducible receipts for Infrastructure and Api (Priority: P2)

A reviewer can see, in the repo, what DEV-382 measured on DEV-296's Infrastructure and Api code, and can regenerate it with one command.

**Why this priority**: the ticket's deliverable is evidence. It depends on US1 and US2 being in place.

**Independent Test**: run the receipt script against the pushed `feature/DEV-296` SHA. It writes `specs/DEV-382/receipts/<project>/receipt.json` and `receipt.md` without touching the DEV-296 worktree.

**Acceptance Scenarios**:

1. **Given** a pushed commit SHA, **When** the receipt script runs, **Then** it measures that commit in a temporary detached checkout outside the repo, with this branch's gate tooling overlaid, and removes the checkout afterwards.
2. **Given** a completed run, **Then** each receipt records:
   - both SHAs;
   - the Stryker version;
   - the effective config and the discovered test projects;
   - the mutated files;
   - per-mutant file:line, mutator, status and `killedBy` names;
   - the gate and Stryker native exits;
   - process samples;
   - assembly identity.
3. **Given** a SHA that is not on any remote branch, **When** the receipt script runs, **Then** it refuses and exits 1.
4. **Given** the sampler never saw a test process, **Then** the receipt says so explicitly and does not present absent evidence as proof.

### Edge Cases

- **The mutated project is referenced only by ArchitectureTests or by a non-test library** (for example `LamuFlix.Tests.Common`): no project is eligible, so the gate fails (US2-3).
- **More than one eligible test project**: all of them are listed in `test-projects`.
- **A ProjectReference with child elements** (for example `<Aliases>api</Aliases>` on DEV-296): it is still detected.
- **Timeout mutants**: they have no `killedBy` and are not subject to the tripwire. They still count as detected, as they do today.
- **`-EvaluateReport` standalone**: it applies the tripwire and the unresolved-killer check. It has no changed-file context, which is the existing behaviour.
- **The Stryker `since` filter**: it stays disabled. The worktree-safe changed-file discovery is kept, and it is not re-verified on 5.0.0.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The tool manifest MUST pin `dotnet-stryker` `5.0.0`, with `rollForward` false.
- **FR-002**: The tracked `stryker-config.json` MUST set `"test-runner": "mtp"` and `"coverage-analysis": "off"`. Thresholds (90/80/80), mutation level and reporters stay unchanged.
- **FR-003**: Every config the gate generates MUST carry `test-runner` and `coverage-analysis` from the tracked config. It MUST also carry the existing `since` disabled, `mutate`, thresholds (break = `harness.yml` `gates.mutation.threshold`) and reporters.
- **FR-004**: Every generated config MUST set `test-projects` to the eligible test projects, as absolute paths. A project is eligible when all of these hold:
  - it is a test project found by the harness's shared discovery (`Get-TestProjects` in `scripts/_gate-common.ps1`): a solution project that references `Microsoft.NET.Test.Sdk` or sets `IsTestProject` true. On this repo that means the projects under `tests/`;
  - it has a direct `ProjectReference` to the mutated project;
  - its name does not end in `.ArchitectureTests`.
- **FR-005**: The gate MUST print the eligible test projects for each mutated project. With `-DryRun`, it MUST show them in the generated config.
- **FR-006**: If a mutated project has no eligible test project, the gate MUST exit 1 without running Stryker. The message names the project.
- **FR-007**: Report evaluation MUST resolve each Killed mutant's `killedBy` ids through `testFiles`. The gate MUST fail if:
  - any resolved killer belongs to an `*.ArchitectureTests` project, judged by its test file path or its fully-qualified name; or
  - any Killed mutant has empty or unresolvable `killedBy`.
  
  This applies both to gate runs and to `-EvaluateReport`.
- **FR-008**: The gate MUST keep every existing failure reason:
  - native exit;
  - missing report;
  - since-filter leak;
  - mutated files outside the changed files;
  - changed files with no tested mutants;
  - 0 tested;
  - 0 killed;
  - below threshold.
  
  The 0-killed reason MUST say it indicates a linkage or measurement failure.
- **FR-009**: The gate MUST run Stryker from the mutated project's directory. It MUST write output under an output root outside the repository. The default is under the system temp directory, and `-OutputRoot` overrides it. Each project gets `<root>/<project>` with `reports/mutation-report.json` and a copy of the effective config.
- **FR-010**: The gate MUST accept `-Project <name[]>`. It limits the run to the named mutated projects among those with changed files; a name that matches no changed project is an error (exit 1).
- **FR-011**: The gate MUST print each Stryker native exit code.
- **FR-012**: A receipt script MUST exist. It MUST:
  - take a commit SHA, target projects, and a receipt directory;
  - refuse a SHA that is not contained in any remote-tracking branch;
  - create a temporary detached checkout outside the repo and overlay the current branch's gate tooling;
  - restore tools;
  - run the gate with `-Project` and `-OutputRoot` while a process sampler runs;
  - write `receipt.json` and `receipt.md` per project;
  - remove the temporary checkout.
- **FR-013**: The receipt MUST record:
  - the measured SHA and the tooling SHA (the tooling SHA is flagged dirty when the tooling files differ from `HEAD`);
  - the Stryker version reported by the tool;
  - the effective config and the eligible test projects;
  - the mutated file list;
  - per mutant: file:line, mutator, status, statusReason, and `killedBy` names;
  - the gate exit and the Stryker native exit;
  - per sampled test process: pid, parent, command line, and loaded `LamuFlix.*` modules;
  - for the mutated project's DLL at the loaded path: its SHA256 during the run, whether its metadata defines types in a `Stryker` namespace, and its SHA256 after a clean rebuild.
- **FR-014**: The receipt script MUST record "sampler observed no test process" explicitly when that happens.
- **FR-015**: The receipt script MUST NOT modify any other worktree. It writes only its temp checkout, its output root, and the receipt directory.
- **FR-016**: Comments and docs that describe the gate as Stryker 4.16 or VSTest behaviour MUST be updated where they would now be wrong.

### Key Entities

- **Effective config**: the generated `stryker-config` object for one mutated project.
- **Eligible test project**: a test project under `tests/` that directly references the mutated project and is not ArchitectureTests.
- **Mutation receipt**: the committed, compact evidence of one project's run (see CONTEXT.md).
- **Artifact kill**: a kill attributed to an ArchitectureTests test (see CONTEXT.md).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With the E1 report (ArchitectureTests killers), `-EvaluateReport` exits 1. With the E5 report, it exits 0.
- **SC-002**: The gate `-DryRun` on a branch with a Core change prints `test-projects` containing `LamuFlix.Test` and not `LamuFlix.ArchitectureTests`.
- **SC-003**: Receipts exist for Infrastructure and Api, measured at the pushed DEV-296 SHA. Every Killed mutant has at least one resolved `killedBy` name. None of those names is an ArchitectureTests test.
- **SC-004**: Each receipt shows a test process whose loaded mutated DLL has a SHA256 that differs from the clean build and that contains `Stryker` types. Otherwise it shows an explicit "not observed".
- **SC-005**: The threshold is unchanged: `harness.yml` stays at 80, and `stryker-config.json` thresholds stay at 90/80/80.

## Assumptions

- The owner delegated every decision after Q5 (CONCLUSIONS D1-D5). Human gate 1 is a veto point in the hand-off, not a blocking question.
- The receipts measure `feature/DEV-296` at its pushed head (`eefbbd9` when this was written). If the branch moves before the receipts run, the new pushed head is measured and recorded.
- Receipts report whatever scores Infrastructure and Api get. A score below 80 is a finding for DEV-296, not for DEV-382 (D1, frozen scope).
- Windows with PowerShell 7 is the receipt platform. The process sampler is Windows-only (Q4). The gate stays cross-platform.
- There is no script-test harness in the repo. Behaviour is verified with scratch report fixtures and real runs.
