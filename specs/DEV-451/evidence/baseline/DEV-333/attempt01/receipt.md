# DEV-451 T002 baseline receipt — DEV-333 attempt01

## Command
- Root: F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-333
- Command: pwsh -NoProfile -File ./scripts/run-mutation.ps1 -BaseRef f00e01bacb63c083349b3a46425189e31e3564c2 -OutputRoot (Join-Path $env:TEMP 'DEV-451-baseline-DEV-333-attempt01')
- BaseRef: f00e01bacb63c083349b3a46425189e31e3564c2
- Head: b25ecb1614a0c82771f254b700bb53dff0a25bf0 (detached HEAD)
- Gate exit: 0
- Observed wall: 1233826 ms (1233.3s native timing)
- Start: 2026-11-04T12:29:08.3683899-03:00
- End: 2026-11-04T12:49:42.1965590-03:00

## Frozen identities
- src aggregate SHA256: 494F0D93E241D64717FA44C0AA03A0A88C7DF86FF6B417F39439ACC34E73E5C6
- tests aggregate SHA256: 34B222502A27A997F5891FBE31D87F4D9E7FC4EE543C7BCDD765EF535D6CC658
- scripts/run-mutation.ps1: 2A0A8FF8A80E87FE326BB0537AE83C36F82E5587068F67656F6DBE5F06E052BE
- stryker-config.json: 71485FB909349B0C3162C33215C67270E9143E86131AC44E328B1DCF04DA4B8B
- harness.yml (working tree, overlay present): EE866EFADCB2BBB82206A677FF7B47F2537FD7D6802B9243FAD0D45BA7514F09
- .config/dotnet-tools.json: DE20C1911406A8132D1F8509853232988CDA75507A17DC66290980727594C67B
- Directory.Packages.props: 28575BE8E50527D8FE9A1F015F9C9C79C77FEEAEB05216ED3813C52ED222D586
- Directory.Build.props: 6AC7F1D8BD1811A525DFDAD0F236B87C48353C14CDEA4A57A616AF07AFA44F78
- Policy overlay: harness.yml modified in working tree (Rigger DEV-333 overlay receipt verified by Conductor); git status showed M harness.yml

## Runtime policy (observed)
- concurrency: 8
- additional-timeout: 90000
- test-runner: mtp
- coverage-analysis: off
- since.enabled: false
- Eligible suites: tests/LamuFlix.Evaluation, tests/LamuFlix.UnitTests
- Mutate: Features/Enrichment/ProcessEnrichmentCommandHandler.cs, Features/Import/ImportLibraryCommandHandler.cs
- Infrastructure: NOT APPLICABLE - listed in gates.mutation.exclusions

## Results
- Mutants (script summary): 104 | Killed: 55 | Survived: 9 | Timeout: 4 | NoCoverage: 0 | Ignored: 28 | CompileError: 8
- Tested: 68 | Score: 86.76% | Verdict: PASS
- Native Stryker exit for Core: 0; gate exit: 0
- Timing artifact: output/mutation-timing.json (present)
- Report: output/LamuFlix.Core/reports/mutation-report.json (present)

## Machine
- computer: ALIEN
- OS: Microsoft Windows NT 10.0.26300.0
- logical processors: 16
- PowerShell: 7.6.9
- dotnet SDK: 10.0.403
- CPU load % at receipt: 38

## Archive
- TEMP output root: C:\Users\lamun\AppData\Local\Temp\DEV-451-baseline-DEV-333-attempt01
- Archived to: F:\Dev\LamuFlix.worktrees\DEV-451\specs\DEV-451\evidence\baseline\DEV-333\attempt01
