# 0016. Mutation gate runs Stryker 5 on the Microsoft Testing Platform runner

- Status: Accepted
- Date: 2026-09-28
- Ticket: DEV-382 (delivered and merged, amended DEV-410, finalized DEV-328)

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

## Amendment: sharded whole-Core run in CI

- The single `stryker-core` CI job ran past its 2 h limit on every push to main and was cancelled
  (runs 41160737459, 41160767088, 41160801922; the report upload then failed with no files).
  With `coverage-analysis: off` each mutant runs every eligible test project, so wall time is
  mutants x suite time / concurrency. The eligible set for Core is `LamuFlix.UnitTests`,
  `LamuFlix.Test` and `LamuFlix.Evaluation`.
- CI now splits the Core source files over 8 parallel jobs (`scripts/ci-stryker-shard.sh run`,
  balanced by line count, 60 minute cap each). Shards report only (`thresholds.break` 0). The
  `stryker-core-gate` job merges the reports and runs `run-mutation.ps1 -EvaluateReport` once,
  so the score threshold, kill-share and file-span rules still apply to the whole of Core.
  Merging rewrites test ids to `<test file>|<test name>` so one test stays one test across shards.
- Not changed, deliberately: `coverage-analysis`. Stryker 5.0.0's MTP runner also contains the
  `all` and `perTestInIsolation` modes; only `perTest` was measured here (E6, rejected). `all`
  (skip mutants no test covers) is unmeasured and is the next experiment, not a default.
- Open observation: `LamuFlix.Test` registers an assembly fixture that starts Postgres and
  RabbitMQ Testcontainers, and no test in that project uses them. Every Stryker test server for
  that project pays the container start-up. Removing the fixture needs its own ticket.

## Amendment: measured speed-up of the whole-Core run (perf/stryker-speed)

- Two failures on main. Every shard failed in under 2 minutes because Stryker's initial test run
  found more than half of `LamuFlix.Test` failing. That retired project is deleted, so the open
  observation above about its Testcontainers fixture is gone with it. Before that, run
  41220833451 (8 shards) hit the 60 minute cap on every shard. The shards held 219, 256, 250, 180,
  164, 187, 153 and 207 mutants, 1616 in total.
- Measured on shard 1 of 8 (219 mutants), local Windows box, concurrency 8 (cores/2). A run is
  the same mutants with one setting changed. Diffs compare per-mutant status with the baseline.

  | Run | Change | Tests | Killed | Survived | Timeout | Score | Wall |
  |---|---|---|---|---|---|---|---|
  | baseline | main config | 2278 | 217 | 2 | 0 | 99.09 | 75.5 min |
  | E1 | `coverage-analysis: all` | 2278 | 216 | 3 | 0 | 98.63 | 78.1 min |
  | E2 | without `LamuFlix.Test` | 2119 | 216 | 3 | 0 | 98.63 | 64.4 min |
  | E3 | E1 + E2 | 2119 | 216 | 3 | 0 | 98.63 | 66.4 min |
  | E4 | E2 without `EvaluationCommandTests` | 2105 | 216 | 0 | 3 | 100.00 | 24.5 min |
  | E5 | E4 + `additional-timeout` 90000 | 2105 | 216 | 3 | 0 | 98.63 | 22.8 min |

- The only status that changed between baseline and E1, E2 and E3 is MovieQueryString.cs 257:33,
  which went from Killed to Survived. A test in `EvaluationCommandTests` killed it, and that test
  starts pwsh, which builds and runs an unmutated copy of `LamuFlix.Evaluation`. So that kill was
  an artifact, and it is the only one. `LamuFlix.Test` killed no mutant that the other projects
  missed.
- `coverage-analysis: all` was slower and changed nothing, so it is rejected. The setting stays `off`.
- `EvaluationCommandTests` (14 tests, each running `dotnet build` through pwsh) and its helpers
  move to the new `tests/LamuFlix.Evaluation.IntegrationTests`. The `*IntegrationTests` name keeps
  them out of both mutation scripts. The eligible set for Core is now `LamuFlix.UnitTests` and
  `LamuFlix.Evaluation`. They were taking about two thirds of every mutant's run time without
  ever seeing a mutant.
- `additional-timeout` goes from 30000 to 90000. In E4, the 30000 allowance turned three real
  survivors (MovieQueryString.cs 223:13, 257:33, 267:16) into Timeouts, which Stryker counts as
  detected and which inflated the score to 100. E5 is status-for-status identical to E2 and 3.3x
  faster than the baseline. The larger allowance only costs time on a mutant that really hangs.
  The gate's floor of 30000 is unchanged.
- Sharding: one file used to stay on one shard, and MovieQueryString.cs alone has about 190
  mutants, which is at least 80 minutes on CI however many shards there are. The planner in
  `ci-stryker-shard.sh` now cuts any file longer than half a shard's line share into Stryker mutate
  spans (`path{start..end}`) at member boundaries. The merge fails unless every mutant is tested
  by exactly one shard. CompileError mutants are the one exception: Stryker lists them in every
  shard of the file whatever the spans, so the merge takes them from the first report. Checked on
  MovieQueryString.cs with the planner's 20-shard spans: the whole file and the two halves (spans
  1+3, spans 2+4) list the same 303 mutants, and the merged halves match the whole-file run
  status for status (191 tested, 44 block-filtered, 68 CompileError). Merging a shard with itself,
  or one half alone, fails. CI runs 20 shards.
- Extrapolated, not measured: E5 is about 50 s per mutant per test session. Full Core is
  1616 x 50 / 8, about 2.8 h on the local box. On CI (4 vCPU, concurrency 2), 20 shards average
  about 81 mutants each, about 34 to 45 minutes of mutation plus about 5 minutes of setup per
  shard. The first CI run of this branch replaces the extrapolation with measured times.
- `stryker-core-gate` downloads the shard reports with `actions/download-artifact@v8`, to match
  the `upload-artifact@v7` that writes them.
