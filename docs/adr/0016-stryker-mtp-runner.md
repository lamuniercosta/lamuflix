# 0016. Mutation gate runs Stryker 5 on the Microsoft Testing Platform runner

- Status: Accepted
- Date: 2026-09-28
- Ticket: DEV-382 (delivered and merged, amended DEV-410, finalized DEV-328, amended DEV-451)

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

- Pin `dotnet-stryker` 5.0.0 and keep `"test-runner": "mtp"`. The tracked
  `stryker-config.json` and CI shards retain `"coverage-analysis": "off"`. Under the DEV-451
  amendment below, the local pre-PR gate generates `"coverage-analysis": "perTestInIsolation"`
  by default and retains explicit `-CoverageAnalysis off` as the control.
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
  or one half alone, fails. CI ran 20 shards at first and now runs 16: GitHub's free plan runs
  20 jobs at once per account, so 20 shards held every slot for a main run's half hour and queued
  all PR jobs behind it. 16 leave 4 slots free; the slowest shard is expected to go from about 33
  to about 41 minutes.
- Extrapolated before CI: E5 is about 50 s per mutant per test session, so full Core is about
  2.8 h on the local box (1616 x 50 / 8). On CI (4 vCPU, concurrency 2), 20 shards would take
  about 34 to 45 minutes of mutation plus about 5 minutes of setup each.
- Measured on CI, run 41253628381 (20 shards, concurrency 2): every shard found 2105 tests and
  created 2246 mutants. The slowest shard took 31.5 minutes, the fastest 5.8, and the whole
  workflow 32 minutes including the 16 s gate job. Merged: 1616 tested, 1497 killed, 113
  survived, 6 timeout, score 93.01%, top kill share 14.73%. The earlier run 41250950142 took
  27 minutes, slowest shard 26.6. Both are under the 1 hour goal and under the extrapolation.
- Three fixes came out of those runs. Each shard restores its test projects one at a time
  before Stryker starts: Stryker's parallel restore lost `LamuFlix.UnitTests` on one shard,
  which then tested against `LamuFlix.Evaluation` alone, and the shard now fails on Stryker's
  "Analysis of project ... failed" line. The merge takes each file's mutant listing only from
  reports that list mutants for it, so a shard that tested no span of a file cannot blank it.
  Each shard stamps Stryker's mutants-created count into its report, and the merge fails unless
  the merged total equals it.
- The gate fails on run 41253628381, on the file-span rule (21 files against a limit of 15).
  It is right to. The same 69 recommendation tests (`SimilarityScorer*`, `MmrSelector*`,
  `ProfileSimilarityCalculator*`, `TasteProfileProperties`, `GetForYouRecommendationsQueryHandlerTests`)
  "kill" AddToWatchlistCommandHandler.cs 15 and 16 and NotFoundException.cs 8, code those tests
  never reach. All three mutants sit in shard 20, and shard 20 of run 41250950142 shows the same
  kills, so the merge did not make them. Shard 20 also ran about 4.6 s per mutant against about
  20 s elsewhere. In shard 20 the widest test,
  `GetForYouRecommendationsQueryHandlerTests.HandleAsync_ResultSummary_CarriesEveryField`, kills
  109 of the 112 tested mutants, all 62 MmrSelector.cs mutants among them; shard 16 tests the
  other half of MmrSelector.cs and the same test kills 14 of its 45. Run locally on just
  AddToWatchlistCommandHandler.cs and NotFoundException.cs (concurrency 1, same config), the
  three mutants are killed only by `AddToWatchlistCommandHandlerTests` and
  `NotFoundExceptionTests`. So shard 20's results are contaminated: its tests fail for a reason
  outside the active mutant, and the 93.01% score is inflated by up to 112 mutants. Leaving shard
  20 out, the widest test still spans 17 files, so the gate would still fail; see the
  `killFileSpanExempt` entry below for how that was resolved.
- Cause: test classes held `static readonly` fields that call mutated Core code, such as
  `GenreTable = GenreWeights.Calculate(...)`. Stryker's MTP runner keeps one test server per
  session, and a static is computed once per server process, under whichever mutant is active
  when the class first loads. Shard 20 mutates GenreWeights.cs, so a GenreWeights mutant was
  baked into those fields, and every later mutant in that server "failed" the same 69 tests.
  The fast 4.6 s per mutant is consistent with tests failing early. The fix turns each such static into a
  computed property (`static X => ...`), so it is evaluated under the active mutant on every
  call. That covers 16 test files across `LamuFlix.UnitTests` and `LamuFlix.Evaluation`. Statics
  built only from test data or plain records (`GenreCounts`, enum arrays, `Regex`) stay as they
  are. Rule for new tests: no `static readonly` field or `static X { get; } =` initializer may
  call Core code.
- Verified locally on shard 20's mutate set (concurrency 8, same config): the three mutants
  above are now killed only by `AddToWatchlistCommandHandlerTests` and `NotFoundExceptionTests`,
  and the widest test spans 2 files instead of 21. MmrSelector.cs's span matched no mutants in
  that run, all 133 removed by the mutate filter. The likely cause is line endings: the Windows
  checkout uses CRLF and the CI checkout uses LF. So the local run
  covers 50 of shard 20's 112 mutants, and CI covers the rest.
- CI run 41277335738, with the fix: every shard green, about 36 minutes for the workflow, 1616
  mutants tested, score 90.97%, top kill share 11.94%. The contaminated kills are gone. The gate
  still failed on file span alone: the widest tests span 16 to 20 files, all of them in four
  recommendation-handler test classes. Those spans are real reach. Each test drives a handler end
  to end, and the 22 distinct files they kill in all sit on the handler's call path
  (scorers, selectors, the taste profile, the handler itself).
- Resolution: `gates.mutation.killFileSpanExempt` in `harness.yml` names test classes the
  file-span rule skips, each with a written reason. It lists `GetRecommendationsQueryHandlerTests`,
  `GetRecommendationsQueryHandlerPropertyTests`, `GetForYouRecommendationsQueryHandlerTests` and
  `GetForYouRecommendationsQueryHandlerPropertyTests`. A test is exempt only when its name starts
  with a listed class name plus `.`, so a class whose name merely begins the same way is not.
  `maxKillShare` still applies to every test, exempt or not, and `maxKillFileSpan` stays at 15
  for everything else; the gate note reports how many exempt tests it skipped and their widest
  span. Raising the limit to 22 instead was rejected: it would also have passed the 21-file
  contaminated kill above, which is the failure the rule exists to catch.
- `stryker-core-gate` downloads the shard reports with `actions/download-artifact@v8`, to match
  the `upload-artifact@v7` that writes them.

## Amendment: measured local pre-PR coverage default (DEV-451, 2026-11-04)

Patron ratified this local-only decision at T006; Bernstein authorized this in-place T010
amendment before T011 adoption. The complete cited ruling and validated evidence are in
`specs/DEV-451/CONCLUSIONS.md` (T005 validation and T006 ruling), with raw receipts and
stable-identity matrices under `specs/DEV-451/evidence/`. No new ADR number is introduced.
This amendment changes the generated local coverage default in Decision item 1. All other
Decision items and the DEV-410 safeguards remain in force.

### Evidence and limits

Matched Stryker 5.0.0 mtp/off versus mtp/perTestInIsolation scratch runs held frozen source,
tests, machine, eligible projects, concurrency 8 and additional-timeout 90000 constant:

| Case | Control wall seconds | Candidate wall seconds | Reduction | Score / gate outcome retained | Timeouts retained |
|---|---:|---:|---:|---|---:|
| DEV-444 | 1088.7 | 840.3 | 22.8% | 58.82%, FAIL | 4 |
| DEV-448 | 3385.7 | 880.0 | 74.0% | 89.59%, PASS | 0 |
| DEV-333 | 1233.3 | 919.4 | 25.5% | 86.76%, Core PASS / Infrastructure N/A | 4 |

Full mutant populations match at 98, 283 and 104, with no duplicate or unmatched identities
and zero Survived-to-Timeout or Killed-to-Timeout conversions. DEV-448 and DEV-333 retain
all status totals. DEV-444 has three explained Survived-to-NoCoverage transitions; NoCoverage
stays in its tested denominator of 68, so neither score nor FAIL is erased. The older rejected
`perTest` experiment and the rejected `all` experiment remain historical evidence for those
modes; this decision ratifies only the measured `perTestInIsolation` local path.

These are single matched scratch samples with pre-existing timeouts, not repeatability,
whole-Core or CI performance proof. Recommendation-test span exemptions still weaken that
signal. T012 must supply native shipping-gate timing and integrity evidence for all three
frozen cases; T013 validates the single authoritative DEV-444 sample strictly below 1800
seconds. Scratch speed does not complete those acceptance tasks or convert a mutation FAIL
into PASS.

### Local generated configuration and controls

- `scripts/run-mutation.ps1` generates mtp/perTestInIsolation by default for the local pre-PR
  gate. Retain explicit `-CoverageAnalysis off` for the matched control. D1b captures caller
  parameters early at script scope so later dot-sourcing cannot lose the requested mode.
  Shipping guards validate the effective generated mode: perTestInIsolation default and off
  control are permitted; unsupported modes are rejected. Regression proof belongs in
  `scripts/Test-RunMutation.ps1`. Scratch-only guard behavior is not the shipping contract.
- Tracked `stryker-config.json` remains mtp/off; it is never hand-edited. There is no
  `harness.yml`, `install.ps1`, renderer or fourth-script change. The local default is selected
  within the ticket-named scripts, not by changing the tracked base configuration.
- `scripts/ci-stryker-shard.sh` stays mtp/off. Its post-merge whole-Core workload is outside
  DEV-451 scope and has not been measured with perTestInIsolation. The CI `-EvaluateReport`
  entry point remains mode-agnostic, and aggregate score, attribution, share/span, shard
  population/merge and restore checks remain unchanged.
- Existing receipt invocations inherit the local default without consumer edits. Preserve
  timing/receipt fields, exits and the parsed `Mutation testing:` and `Stryker native exit for`
  prefixes; additive effective-mode/provenance output must not begin with either prefix.
  Patron dispositioned off-only mentions in unnamed docs/skills/settings as `noted, no ticket`;
  those files remain untouched in DEV-451. This ADR is the canonical coverage decision.

### Safeguards and remaining delivery work

Keep Stryker 5.0.0 and mtp, explicit directly referencing eligible projects, fail-if-none,
ArchitectureTests exclusion and killedBy rejection, threshold 80, actual additional-timeout
90000 with the 30000 floor, concurrency no greater than half the processor count,
`since.enabled=false`, diff-only mutation by default and explicit `-WholeProject` behavior.
The entire `gates.mutation` block remains unchanged: share 50 with its 20-tested minimum,
span 15, exact class-prefix/reason exemptions and project exclusions. NoCoverage remains in
the score denominator. Preserve static-initialization discipline: perTestInIsolation does
not establish isolation of mutant-server state or authorize mutable static initialization.
The CRLF span defect remains the separate DEV-460 follow-up.

T010 records this approved amendment before T011 implementation. T011 must retain these
controls and prove the default, explicit off mode, parameter preservation and rejection
paths within the three-script boundary. Native T012/T013 acceptance, all-three-script
`dotnet-agent-harness` origin/main parity at T014 and remaining gates/review are still
required before delivery. This amendment authorizes no CI speed claim or policy relaxation.
