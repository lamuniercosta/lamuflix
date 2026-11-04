# DEV-451 T005 candidate receipt — DEV-333 attempt01

## Provenance
- Experimental scratch gate (non-shipping): C:\Users\lamun\AppData\Local\Temp\DEV-451-scratch-gate\run-mutation.ps1
- Gate copy SHA256: A7184D91350028B77C24B5D35CCDF3C69817306EA2824E787C9AB67D42CA79BE
- CoverageAnalysis: perTestInIsolation
- Tracked shipping scripts: untouched
- Manifest digest: 0A20E74482C997D7C507DFD15D037BFFB2FC63658F7D7F08C338D89820350B62
- Delta hash: 287CD53308FB4201477296E0A5323FEA7F5FBC7CD970546D29CC53693CD503E6
- InvalidReason: none

## Command
- Replay root / CWD: F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-333
- HARNESS_REPO_ROOT: unset
- Command: pwsh -NoProfile -File C:\Users\lamun\AppData\Local\Temp\DEV-451-scratch-gate\run-mutation.ps1 -BaseRef f00e01bacb63c083349b3a46425189e31e3564c2 -OutputRoot C:\Users\lamun\AppData\Local\Temp\DEV-451-candidate-DEV-333-attempt01 -CoverageAnalysis perTestInIsolation
- BaseRef: f00e01bacb63c083349b3a46425189e31e3564c2
- Head: b25ecb1614a0c82771f254b700bb53dff0a25bf0 (detached)
- Wrapper exit: 0
- Observed wallMs: 920043
- Start UTC: 2026-11-04T18:52:36.9980154+00:00
- End UTC: 2026-11-04T19:07:57.0449292+00:00
- Preflight exit: 0
- EvaluateReport exit: 0

## Frozen identities
- src aggregate SHA256: 02D0F8DA12AB6BC7E5B0BF597BC0D4C12C1F390006624A4F03211520E1E48F37
- tests aggregate SHA256: F24456D141598CDDE0DC8CD1379743D260C78323245C812EA79CC2DFBCDC3ECC
- scripts/run-mutation.ps1 (tracked in replay): 2A0A8FF8A80E87FE326BB0537AE83C36F82E5587068F67656F6DBE5F06E052BE
- stryker-config.json: 71485FB909349B0C3162C33215C67270E9143E86131AC44E328B1DCF04DA4B8B
- harness.yml: EE866EFADCB2BBB82206A677FF7B47F2537FD7D6802B9243FAD0D45BA7514F09
- .config/dotnet-tools.json: DE20C1911406A8132D1F8509853232988CDA75507A17DC66290980727594C67B
- Directory.Packages.props: 28575BE8E50527D8FE9A1F015F9C9C79C77FEEAEB05216ED3813C52ED222D586
- Directory.Build.props: 6AC7F1D8BD1811A525DFDAD0F236B87C48353C14CDEA4A57A616AF07AFA44F78
- Policy overlay: Rigger Tests.Common harness.yml overlay hash EE866EFADCB2BBB82206A677FF7B47F2537FD7D6802B9243FAD0D45BA7514F09
- Porcelain after:  M harness.yml

## Runtime policy (observed)
- concurrency: 8
- additional-timeout: 90000 (from effective config when present)
- test-runner: mtp
- coverage-analysis: perTestInIsolation (candidate)
- since.enabled: false
- Eligible suites: tests/LamuFlix.Evaluation, tests/LamuFlix.UnitTests
- Native durationSeconds: 919.4
- D4 provenance: {"kind":"non-shipping-scratch-experiment","coverageAnalysis":"perTestInIsolation","shippingScripts":"untouched"}

## Results (from console / timing; see artifacts for authority)
- Wrapper exit: 0
- Eval exit: 0
- Completion flags: {"hasSummary":true,"hasTerminal":true,"timingExists":true,"reportExists":true,"configExists":true}

## Machine
- computer: ALIEN
- OS: Microsoft Windows NT 10.0.26300.0
- logical processors: 16
- PowerShell: 7.6.9
- dotnet SDK: 10.0.403
- CPU load % at receipt: 33

## Archive
- TEMP output root: C:\Users\lamun\AppData\Local\Temp\DEV-451-candidate-DEV-333-attempt01
- Archived to: F:\Dev\LamuFlix.worktrees\DEV-451\specs\DEV-451\evidence\candidate\DEV-333\attempt01
- Artifacts: console.log, preflight.log, gauge-timing.json, receipt.md, ownership-*.json, eval-*, output/, driver.log
