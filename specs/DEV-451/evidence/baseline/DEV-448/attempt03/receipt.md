# DEV-451 T002 baseline receipt — DEV-448 attempt03

## Command
- Root: F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-448
- Command: pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef f8bb21cb38e3b9105ee1bc0500966903a5fad40a -OutputRoot (Join-Path $env:TEMP 'DEV-451-baseline-DEV-448-attempt03')
- Capture: synchronous in one persistent PowerShell session; stdout+stderr redirected via `*>` to console.log; $LASTEXITCODE recorded after wrapper exit (CONCLUSIONS.md:347-386 / recon 10.7)
- BaseRef: f8bb21cb38e3b9105ee1bc0500966903a5fad40a
- Head: 6fcbd1ffdb3805b46937534bac48a37a68ebd62b (detached HEAD; porcelain clean)
- Preflight02 exit: 0 (OutputRoot DEV-451-baseline-DEV-448-preflight02; mutate Features/Recommendations/SimilarityScorer.cs; Evaluation+UnitTests; concurrency 8; additional-timeout 90000; mtp/off; since disabled)
- Gate wrapper exit: 0 (CAPTURED)
- Native mutation-timing.json: PRESENT (durationSeconds 3385.7, concurrency 8, threshold 80)
- Observed wall (gauge): 3386169 ms
- Native wall: 3385.7s (Stryker project wall 3383.5s; Time Elapsed 00:56:22.6279870)
- Start UTC: 2026-11-04T16:27:01.8278242+00:00
- End UTC: 2026-11-04T17:23:27.9992904+00:00
- Stryker native exit for Core: 0
- Console local timestamps: 2026-11-04 13:27:04 -> 14:23:26 (UTC-3)

## Seriality and ownership
- Single runner: Gauge exclusive ownership of F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-448 through archive
- Process check before preflight (UTC 2026-11-04T16:25:15.9774354+00:00): no run-mutation.ps1 / dotnet stryker sessions
- Process check immediately before attempt03 (pre-launch): no run-mutation.ps1 / dotnet stryker sessions
- Process check after wrapper exit: 0 matching sessions
- No concurrent DEV-333/DEV-444/DEV-448 mutation run during this window
- No background job, pattern-triggered stop, or short command timeout used

## Frozen identities (match attempt01 receipt)
- src aggregate SHA256: 43058FDA84FC9776A6C54BBC63C85BCD2C7CFE21A589F8321F2A0AD2DD4C5FBF
- tests aggregate SHA256: A23F845ADABE8D2C6D4F9ECEB4ADFC2F5A995D44C70E5C5AC4509C49C9657D5A
- scripts/run-mutation.ps1: 2A0A8FF8A80E87FE326BB0537AE83C36F82E5587068F67656F6DBE5F06E052BE
- stryker-config.json: 71485FB909349B0C3162C33215C67270E9143E86131AC44E328B1DCF04DA4B8B
- harness.yml: EE866EFADCB2BBB82206A677FF7B47F2537FD7D6802B9243FAD0D45BA7514F09
- .config/dotnet-tools.json: DE20C1911406A8132D1F8509853232988CDA75507A17DC66290980727594C67B
- Directory.Packages.props: 28575BE8E50527D8FE9A1F015F9C9C79C77FEEAEB05216ED3813C52ED222D586
- Directory.Build.props: 6AC7F1D8BD1811A525DFDAD0F236B87C48353C14CDEA4A57A616AF07AFA44F78
- Policy overlay: none (clean detached replay; no harness.yml or code/test/config edit)

## Runtime policy (observed generated config + preflight)
- concurrency: 8
- additional-timeout: 90000
- test-runner: mtp
- coverage-analysis: off
- since.enabled: false
- Eligible suites: tests/LamuFlix.Evaluation, tests/LamuFlix.UnitTests
- Mutate: Features/Recommendations/SimilarityScorer.cs
- Generated config SHA256 (temp stryker-LamuFlix.Core-8ba19b21.json): D76B21AED15E96BDAEC15D86365E0151992276DD2A84A81F511754E0348679F4

## Results
- Mutants created (Stryker): 2556; CompileError 190; Ignored 2145 (41 block + 2104 mutate filter); skipped 2335
- Script summary mutants: 283 | Killed: 198 | Survived: 23 | Timeout: 0 | NoCoverage: 0 | Ignored: 41 | CompileError: 21
- Report aggregate (mutation-report.json): killed=198 survived=23 timeout=0 nocov=0 compileError=21 ignored=41 total=283 (matches console)
- Tested: 221 | Score: 89.59% (89.59276018099548) | Threshold break 80% -> PASS
- Kill share: largest single-test share 33.48% of 221 tested (limit 50%)
- Kill file span: largest single-test file span 1 file(s) (limit 15); 77 exempt test(s) in 4 exempt class(es), widest exempt span 1 file(s)
- Native Stryker exit for Core: 0; gate wrapper exit: 0
- Verdict line: Mutation testing: PASSED
- Timing artifact: output/mutation-timing.json (present; unmodified native)
- Report: output/LamuFlix.Core/reports/mutation-report.json (present)
- HTML report: output/LamuFlix.Core/reports/mutation-report.html

## Completion assertions (recon 10.7 item 6)
- a. console contains Mutation Testing Summary AND terminal Mutation testing: PASSED — PASS
- b. native mutation-timing.json with generatedUtc, durationSeconds, concurrency 8, threshold 80, projects[] — PASS
- c. gauge-timing.json records actual wrapper exit 0 and wallMs 3386169 — PASS
- d. report totals equal console summary — PASS

## Prior attempts invalidation (bytes retained)
- attempt01 and attempt02 left byte-identical under evidence/baseline/DEV-448/
- Invalid because: wrapper exits NOT CAPTURED; native mutation-timing.json ABSENT; gauge-timing.json ABSENT; attempt02 overlapped attempt01 ~3m15s (11:33:19 -> 11:36:34) in the same worktree (recon 10.2/10.6; CONCLUSIONS.md:344-345)
- attempt03 is the serial quiet-machine captured baseline for DEV-448

## Machine
- computer: ALIEN
- OS: Microsoft Windows NT 10.0.26300.0
- logical processors: 16
- CPU: 11th Gen Intel(R) Core(TM) i7-11800H @ 2.30GHz (8 cores / 16 logical)
- PowerShell: 7.6.9
- dotnet SDK: 10.0.403
- CPU load % at receipt: 24

## Archive
- TEMP output root: C:\Users\lamun\AppData\Local\Temp\DEV-451-baseline-DEV-448-attempt03
- Console: C:\Users\lamun\AppData\Local\Temp\DEV-451-DEV-448-attempt03-console.log
- Gauge timing: C:\Users\lamun\AppData\Local\Temp\DEV-451-DEV-448-attempt03-gauge-timing.json
- Preflight log: C:\Users\lamun\AppData\Local\Temp\DEV-451-DEV-448-preflight02-console.log
- Archived to: F:/Dev/LamuFlix.worktrees/DEV-451/specs/DEV-451/evidence/baseline/DEV-448/attempt03
- Contents: console.log, preflight.log, gauge-timing.json, receipt.md, ownership-preflight.json, ownership-prelaunch.json, output/ (mutation-timing.json + LamuFlix.Core/{stryker-config.json,.gitignore,reports/*})
