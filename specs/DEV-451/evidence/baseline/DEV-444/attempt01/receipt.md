# DEV-451 T002 baseline receipt — DEV-444 attempt01

## Command
- Root: F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-444
- Command: pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef 1e95037eda58502e0f7088a31f4502798e51d3a0 -OutputRoot (Join-Path $env:TEMP 'DEV-451-baseline-DEV-444-attempt01')
- BaseRef: 1e95037eda58502e0f7088a31f4502798e51d3a0
- Head: 0079991451599e408b6ff37de25a6e3e731b2c6a (detached HEAD)
- Gate exit: 1
- Observed wall: 1089190 ms (1088.7s native timing)
- Start: 2026-11-04T10:28:17.9905341-03:00
- End: 2026-11-04T10:46:27.1830491-03:00

## Frozen identities
- src aggregate SHA256: B660674A1082E3E9A5FE3040D140395C6E46C4979AB0BF1452697BC2F0C4BBA5
- tests aggregate SHA256: 22A0DE8BDAA84CDF233067238E4A6B1C9DDF78B92504FD909F26B7DDC929C45E
- scripts/run-mutation.ps1: 2A0A8FF8A80E87FE326BB0537AE83C36F82E5587068F67656F6DBE5F06E052BE
- stryker-config.json: 71485FB909349B0C3162C33215C67270E9143E86131AC44E328B1DCF04DA4B8B
- harness.yml: EE866EFADCB2BBB82206A677FF7B47F2537FD7D6802B9243FAD0D45BA7514F09
- .config/dotnet-tools.json: DE20C1911406A8132D1F8509853232988CDA75507A17DC66290980727594C67B
- Directory.Packages.props: 28575BE8E50527D8FE9A1F015F9C9C79C77FEEAEB05216ED3813C52ED222D586
- Directory.Build.props: 6AC7F1D8BD1811A525DFDAD0F236B87C48353C14CDEA4A57A616AF07AFA44F78
- Policy overlay: none (clean detached replay; no harness.yml modification)

## Runtime policy (observed)
- concurrency: 8
- additional-timeout: 90000
- test-runner: mtp
- coverage-analysis: off
- since.enabled: false
- Eligible suites: tests/LamuFlix.Evaluation, tests/LamuFlix.UnitTests
- Mutate: Features/Recommendations/GetForYouRecommendationsQueryHandler.cs

## Results
- Mutants (script summary): 98 | Killed: 36 | Survived: 28 | Timeout: 4 | NoCoverage: 0 | Ignored: 23 | CompileError: 7
- Tested: 68 | Score: 58.82% | Verdict: FAIL (below break 80%)
- Native Stryker exit for Core: 2; gate exit: 1
- Timing artifact: output/mutation-timing.json (present)
- Report: output/LamuFlix.Core/reports/mutation-report.json (present)
- HTML report: output/LamuFlix.Core/reports/mutation-report.html

## Machine
- computer: ALIEN
- OS: Microsoft Windows NT 10.0.26300.0
- logical processors: 16
- PowerShell: 7.6.9
- dotnet SDK: 10.0.403
- CPU load % at receipt: 20

## Archive
- TEMP output root: C:\Users\lamun\AppData\Local\Temp\DEV-451-baseline-DEV-444-attempt01
- Archived to: F:\Dev\LamuFlix.worktrees\DEV-451\specs\DEV-451\evidence\baseline\DEV-444\attempt01
- console.log, preflight.log, gauge-timing.json, receipt.md, output/
