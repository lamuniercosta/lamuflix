# DEV-382 — Alignment Brief

Grill outcome for DEV-382 (parent DEV-282, repo lamuflix): diagnose and fix Stryker mutant-to-test linkage for `LamuFlix.Infrastructure` and `LamuFlix.Api`.
Rulings and evidence: `specs/DEV-382/CONCLUSIONS.md` (Q1-Q5 asked, D1-D5 delegated by the owner's Q5 answer).
Grill: **5 questions asked; 5/5 answered**, then the owner delegated every remaining decision ("Go with your recomendation. Don't ask me any more questions").

## Root cause

xUnit v3 test projects are executables. Under the VSTest runner, `testhost.exe` launches `LamuFlix.Test.exe` as a child process. Stryker's in-process coverage collector sets the active mutant inside `testhost`, not inside that child. The environment-variable fallback is read once at child startup and cannot change per mutant. So no mutant is ever active where the tests run, and every mutant survives.

This is a linkage defect, not a test-quality verdict. Evidence:

- E2 (4.16 VSTest) and E4 (5.0.0 VSTest) killed 0%.
- E3 (4.16 MTP) and E5 (5.0.0 MTP) killed 96.83% on the same 63 Core mutants.

The 5.0.0 Microsoft Testing Platform runner (`"test-runner": "mtp"`) passes the active mutant through a memory-mapped file, so it reaches the test process. It needs no csproj change.

A second defect showed up in the evidence (E1). Stryker injects types in the `Stryker` namespace into the mutated assembly. The NetArchTest rules in `LamuFlix.ArchitectureTests` then fail for every mutant. So a run that includes ArchitectureTests reports every mutant killed, whether the unit tests would catch it or not.

## Closing bar

- **AC1 (linkage):** the gate runs Stryker `5.0.0` with `"test-runner": "mtp"` and `"coverage-analysis": "off"`.
  - Both settings are in the tracked `stryker-config.json` and in every generated config.
  - On a run where the tests do catch mutants, `killedBy` holds real test names.
- **AC2 (test projects):**
  - Generated configs list `test-projects` explicitly: the test projects under `tests/` that reference the mutated project, excluding `*.ArchitectureTests`.
  - The gate prints the list.
  - If no project is eligible, the gate fails (exit 1) and does not run Stryker.
- **AC3 (artifact tripwire):**
  - Report evaluation resolves `killedBy` ids to names through `testFiles`.
  - It fails if any killer is an ArchitectureTests test.
  - `-EvaluateReport` applies the same check.
- **AC4 (receipts):** `specs/DEV-382/receipts/{Infrastructure,Api}/receipt.{json,md}`, measured against the pushed `feature/DEV-296` SHA (`eefbbd9` when this brief was written; the receipts measured `f66d8c2`). Each receipt records:
  - both SHAs;
  - the Stryker version;
  - the effective config and the discovered test projects;
  - the mutated files;
  - per-mutant file:line, mutator, status, and `killedBy` names;
  - native exits;
  - process samples;
  - assembly identity: the SHA256 of the loaded DLL during the run, whether it has `Stryker` types, and the SHA256 of a clean build.
- **AC5 (threshold):**
  - The threshold is unchanged: `harness.yml` `gates.mutation.threshold` 80, and the tracked config stays at 90/80/80.
  - The zero-kill check and all the other existing failure reasons are kept.
  - A zero kill is reported as a linkage or measurement problem, never as a test-quality verdict.
- **AC6 (gates):**
  - `scripts/run-mutation.ps1 -DryRun` works.
  - `-EvaluateReport` behaves as specified against scratch report fixtures (the repo has no script-test harness, and adding one is out of scope):
    - an ArchitectureTests killer gives exit 1;
    - named unit-test kills at or above 80 give exit 0;
    - the real E1 and E5 reports give 1 and 0.
  - Static-analysis gates do not apply, because no `.cs` file changes.

## Frozen scope

- `.config/dotnet-tools.json`: `dotnet-stryker` `4.16.0` → `5.0.0` (`rollForward` stays false).
- `stryker-config.json`: add `"test-runner": "mtp"` and `"coverage-analysis": "off"`. Thresholds and reporters are unchanged.
- `scripts/run-mutation.ps1`:
  - Carry runner and coverage settings into the generated configs.
  - Discover test projects and apply the ArchitectureTests exclusion.
  - Fail when no test project is eligible.
  - Add the `killedBy` ArchitectureTests tripwire.
  - Run Stryker from the project directory and write output outside the repo with `-O`.
  - Add `-OutputRoot` and `-Project` so the receipt script can drive the gate itself.
  - Print each Stryker native exit code.
  - Update the 4.16-specific comments.
- New receipt script under `scripts/` that drives the gate's generated config (Q4).
- The receipts described in AC4.
- Updated docs and comments that name Stryker 4.16 or VSTest behaviour.
- `CONTEXT.md` terms and an ADR for the runner choice.

**Anything else is a follow-up issue, not a finding in this round.**

Explicitly out of scope:

- Re-running or reclassifying DEV-296 D7d/D7f or DEV-294 D7a (D1). This is a follow-up issue.
- Any change on `feature/DEV-296` or in its worktree.
- Raising Infrastructure or Api scores by adding tests.
- `perTest` coverage (E6 was slow and gave spurious results).

## Round cap

- Analyze: 2 rounds.
- Review: 2 rounds, with at most 2 fix commits per round (D4).
- Closing bar for review findings: Critical or High, with a concrete failure scenario (D2).

## Human gate 1

The owner delegated it (D5). The spec, plan and tasks go in the hand-off for the owner to veto. Commit, push and any YouTrack post stay owner-confirmed actions.
