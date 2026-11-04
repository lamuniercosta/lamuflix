# DEV-451 T012 native phase4 receipt — DEV-448 attempt01

## Provenance
- Native shipping gate (T011 at e99267cb): F:\Dev\LamuFlix.worktrees\DEV-451\scripts\run-mutation.ps1
- Native gate SHA256: 56DDBA5C1ED542623C00708D357768A29D15BF378D0F155CDEF49898B7F08C67
- CoverageAnalysis: perTestInIsolation (default generated; no -CoverageAnalysis override)
- Replay tracked scripts/run-mutation.ps1: frozen historical (untouched) hash 2A0A8FF8A80E87FE326BB0537AE83C36F82E5587068F67656F6DBE5F06E052BE
- InvalidReason: none

## Command
- Replay root / CWD: F:/Dev/LamuFlix.worktrees/DEV-451-replay-DEV-448
- HARNESS_REPO_ROOT: unset
- Command: pwsh -NoProfile -File F:\Dev\LamuFlix.worktrees\DEV-451\scripts\run-mutation.ps1 -BaseRef f8bb21cb38e3b9105ee1bc0500966903a5fad40a -OutputRoot C:\Users\lamun\AppData\Local\Temp\DEV-451-phase4-DEV-448-attempt01
- BaseRef: f8bb21cb38e3b9105ee1bc0500966903a5fad40a
- Head: 6fcbd1ffdb3805b46937534bac48a37a68ebd62b (detached)
- Wrapper exit: 0
- Observed wallMs: 772044
- Start UTC: 2026-11-04T19:56:26.4897694+00:00
- End UTC: 2026-11-04T20:09:18.5370898+00:00
- Preflight exit: 0
- EvaluateReport exit: 0

## Frozen identities
- src aggregate SHA256: 84D7D2473E136CD8F6A96B60BE1DF32BA82C0BA32EC7807EA4E1205E35944742
- tests aggregate SHA256: 6AAEA5EAEF8678D6401F740D63D5AF6AD8C97AF63C81615A4EC27678651FEFD8
- scripts/run-mutation.ps1 (tracked in replay): 2A0A8FF8A80E87FE326BB0537AE83C36F82E5587068F67656F6DBE5F06E052BE
- scripts/run-mutation.ps1 (native T011 invoked): 56DDBA5C1ED542623C00708D357768A29D15BF378D0F155CDEF49898B7F08C67
- stryker-config.json: 71485FB909349B0C3162C33215C67270E9143E86131AC44E328B1DCF04DA4B8B
- harness.yml: EE866EFADCB2BBB82206A677FF7B47F2537FD7D6802B9243FAD0D45BA7514F09
- .config/dotnet-tools.json: DE20C1911406A8132D1F8509853232988CDA75507A17DC66290980727594C67B
- Directory.Packages.props: 28575BE8E50527D8FE9A1F015F9C9C79C77FEEAEB05216ED3813C52ED222D586
- Directory.Build.props: 6AC7F1D8BD1811A525DFDAD0F236B87C48353C14CDEA4A57A616AF07AFA44F78
- Policy overlay: none
- Porcelain after: 

## Runtime policy (observed)
- concurrency: 8
- additional-timeout: 90000 (from effective config when present)
- test-runner: mtp
- coverage-analysis: perTestInIsolation (native default)
- since.enabled: false
- Eligible suites: tests/LamuFlix.Evaluation, tests/LamuFlix.UnitTests
- Native durationSeconds: 771.6
- Timing coverageAnalysis field: perTestInIsolation

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
- CPU load % at receipt: 43

## Archive
- TEMP output root: C:\Users\lamun\AppData\Local\Temp\DEV-451-phase4-DEV-448-attempt01
- Archived to: F:\Dev\LamuFlix.worktrees\DEV-451\specs\DEV-451\evidence\phase4\DEV-448\attempt01
- Artifacts: console.log, preflight.log, gauge-timing.json, receipt.md, ownership-*.json, eval-*, output/, driver.log
