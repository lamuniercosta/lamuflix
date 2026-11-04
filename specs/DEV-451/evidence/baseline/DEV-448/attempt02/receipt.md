# DEV-451 T002 baseline receipt — DEV-448 attempt02

## Command
- Root: F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-448
- Command: pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef f8bb21cb38e3b9105ee1bc0500966903a5fad40a -OutputRoot C:\Users\lamun\AppData\Local\Temp\DEV-451-baseline-DEV-448-attempt02
- BaseRef: f8bb21cb38e3b9105ee1bc0500966903a5fad40a
- Head: 6fcbd1ffdb3805b46937534bac48a37a68ebd62b (detached HEAD)
- Stryker native exit (Core): 0
- Gate wrapper exit: NOT CAPTURED (process ended after Stryker summary; mutation-timing.json absent)
- Observed Stryker wall: 3205.5s (Time Elapsed 00:53:23.7695489)
- Stryker start/end (from log timestamps): 2026-11-04T11:33:21 -> 2026-11-04T12:26:44
- Note: Overlapping re-run started while attempt01 still active (~11:33 vs attempt01 end 11:36). Contended CPU. Wrapper did not write mutation-timing.json; Stryker native exit 0, score 89.14%.

## Frozen identities
- src aggregate SHA256: 43058FDA84FC9776A6C54BBC63C85BCD2C7CFE21A589F8321F2A0AD2DD4C5FBF
- tests aggregate SHA256: A23F845ADABE8D2C6D4F9ECEB4ADFC2F5A995D44C70E5C5AC4509C49C9657D5A
- scripts/run-mutation.ps1: 2A0A8FF8A80E87FE326BB0537AE83C36F82E5587068F67656F6DBE5F06E052BE
- stryker-config.json: 71485FB909349B0C3162C33215C67270E9143E86131AC44E328B1DCF04DA4B8B
- harness.yml: EE866EFADCB2BBB82206A677FF7B47F2537FD7D6802B9243FAD0D45BA7514F09
- .config/dotnet-tools.json: DE20C1911406A8132D1F8509853232988CDA75507A17DC66290980727594C67B
- Directory.Packages.props: 28575BE8E50527D8FE9A1F015F9C9C79C77FEEAEB05216ED3813C52ED222D586
- Directory.Build.props: 6AC7F1D8BD1811A525DFDAD0F236B87C48353C14CDEA4A57A616AF07AFA44F78
- Policy overlay: none

## Runtime policy (observed)
- concurrency: 8
- additional-timeout: 90000
- test-runner: mtp
- coverage-analysis: off
- since.enabled: false
- Eligible suites: tests/LamuFlix.Evaluation, tests/LamuFlix.UnitTests
- Mutate: Features/Recommendations/SimilarityScorer.cs

## Results (from Stryker console + JSON report)
- Mutants created: 2556; skipped CompileError 190 + Ignored 2145; tested 221
- Report aggregate: killed=197 survived=24 timeout=0 nocov=0 compileError(in report)=21 ignored=41
- Score: 89.14% (thresholds high 90 / low 80; break 80 from config) -> Stryker PASS
- mutation-timing.json: ABSENT
- Report: output/LamuFlix.Core/reports/mutation-report.json (present)

## Archive
- TEMP: C:\Users\lamun\AppData\Local\Temp\DEV-451-baseline-DEV-448-attempt02
- Archived to: F:\Dev\LamuFlix.worktrees\DEV-451\specs\DEV-451\evidence\baseline\DEV-448\attempt02
