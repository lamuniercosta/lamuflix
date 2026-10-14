# 0016. Mutation gate runs Stryker 5 on the Microsoft Testing Platform runner

- Status: Proposed (becomes Accepted when the DEV-382 PR merges)
- Date: 2026-09-28
- Ticket: DEV-382

## Context

The Stryker gate (`scripts/run-mutation.ps1`, pinned `dotnet-stryker` 4.16.0) reported 0 killed for
Infrastructure (0/40) and Api (0/9) in DEV-296 D7d. The DEV-382 experiments (E1-E6,
`specs/DEV-382/CONCLUSIONS.md`) show:

- The test projects use xUnit v3, which makes them executables. Under the VSTest runner, `testhost.exe` starts
  `LamuFlix.Test.exe` as a child process. Stryker activates mutants inside `testhost`, so the
  child never sees the active mutant, and every mutant survives. This happens on 4.16 (E2) and on 5.0.0 (E4).
- The Microsoft Testing Platform runner (`"test-runner": "mtp"`) runs the test DLL as a server
  and passes the active mutant through a memory-mapped file. The same 63 mutants scored 96.83%
  on 4.16 (E3) and 5.0.0 (E5). Only 5.0.0 reports `killedBy` test names.
- `perTest` coverage on MTP (E6) took 42 minutes, restarted the test server 45 times and
  turned survivors into timeouts. `coverage-analysis: off` (E5) was clean.
- Runs that include `LamuFlix.ArchitectureTests` report every mutant killed. Stryker's
  injected `Stryker.*` types break the NetArchTest rules, not the mutation (E1).

## Decision

- Pin `dotnet-stryker` 5.0.0. Set `"test-runner": "mtp"` and `"coverage-analysis": "off"`
  in `stryker-config.json` and in every config the gate generates.
- The gate sets `test-projects` explicitly: the test projects that reference the mutated project,
  excluding `*.ArchitectureTests`. It fails when none are eligible.
- Report evaluation fails when any `killedBy` test is an ArchitectureTests test.
- The threshold is unchanged (80).

## Consequences

- With coverage off, every mutant runs the full eligible test project, so gate runtime grows
  roughly with the number of mutants. Each mutant's run time also sits close to Stryker's
  timeout, and two identical receipt runs moved mutants between Survived, Killed and Timeout
  (DEV-382 CONCLUSIONS F1). Stryker counts Timeout as detected, so a score near the
  threshold can flip between runs. Repeat-run stability is a follow-up.
- Kills name the tests that caught them, so an artifact kill is visible and gated.
- Mutation scores recorded before this change on a VSTest run are not comparable. Scores
  from runs that included ArchitectureTests (DEV-296 D7d/D7f Core, DEV-294 D7a) need
  re-measurement in a follow-up issue.
- The csproj files need no MTP opt-in (`TestingPlatformDotnetTestSupport`): the MTP runner
  drives the test DLL directly.

## Amendment: honest timeouts and artifact-kill detection (DEV-410)

- `stryker-config.json` sets `"additional-timeout": 30000`, and the gate refuses to run
  (GATE NOT WIRED) when it is missing or below 30000. It closes the repeat-run follow-up above.
  With the default 5000 ms on top of the ~22 s suite, a busy machine turned survivors into
  timeouts, which Stryker counts as detected. The clean whole-Core run at 30000 ms tested 518
  mutants with 0 timeouts in 23 minutes, so no Core mutant really hangs and the larger
  allowance costs nothing on a normal run.
- Two rules catch a test that fails for a reason outside the mutation, usually state leaking
  across mutant runs in the shared test server. The first is `gates.mutation.maxKillShare`
  (default 50): no test may kill more than that percent of tested mutants once 20 are tested.
  The second is `gates.mutation.maxKillFileSpan` (default 15): no test may kill mutants in more
  than that many source files. Clean whole-Core runs peak at 9 files per test. A run
  contaminated by a mutated static initializer reached 26.
