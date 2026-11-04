# DEV-451 T012 findings — native matched candidate measurements (Gauge)

**Status:** T012 measurements complete; native shipping-gate provenance (T011 `e99267cb`); not T013 validation.

**Gate:** `F:/Dev/LamuFlix.worktrees/DEV-451/scripts/run-mutation.ps1` SHA256 `56DDBA5C1ED542623C00708D357768A29D15BF378D0F155CDEF49898B7F08C67`
**Mode:** default generated `coverage-analysis: perTestInIsolation` (no `-CoverageAnalysis` override)
**Pinned:** concurrency 8, additional-timeout 90000, mtp, since disabled, machine ALIEN, serial quiet-machine ownership
**Control baselines:** DEV-444 attempt01, DEV-448 attempt03, DEV-333 attempt01

## Wall-time / verdict matrix

| Case | Baseline native s | Phase4 native s | Δ | Wrapper exit B→P4 | Score B→P4 | Tested | Timeouts B→P4 | Eval exit |
|---|---:|---:|---:|---|---|---:|---|---:|
| DEV-444 | 1088,7 | **813,3** | -275,4 (-25,3%) | 1→1 | 58,82%→58,82352941176471% | 68 | 4→4 | 1 |
| DEV-448 | 3385,7 | **771,6** | -2614,1 (-77,2%) | 0→0 | 89,59%→89,59276018099548% | 221 | 0→0 | 0 |
| DEV-333 | 1233,3 | **828,2** | -405,1 (-32,8%) | 0→0 | 86,76%→86,76470588235294% | 68 | 4→4 | 0 |

## Stable-identity integrity

| Case | Population | Dup/unmatched | S→Timeout | K→Timeout | integrityReject |
|---|---|---:|---:|---:|---|
| DEV-444 | 98=98 | 0/0 | **0** | **0** | False |
| DEV-448 | 283=283 | 0/0 | **0** | **0** | False |
| DEV-333 | 104=104 | 0/0 | **0** | **0** | False |

## Artifact roots
- `specs/DEV-451/evidence/phase4/DEV-444/attempt01/`
- `specs/DEV-451/evidence/phase4/DEV-448/attempt01/`
- `specs/DEV-451/evidence/phase4/DEV-333/attempt01/`
- Each: console.log, preflight.log, gauge-timing.json, receipt.md, ownership-*.json, eval-*, output/ (native timing+config+report), status-matrix.json + status-matrix-summary.md, driver.log

## Orchestrator log
- C:\Users\lamun\AppData\Local\Temp\DEV-451-T012-orchestrator.log

## Disposition
- T012 measurement delivery complete. T013 validates the authoritative DEV-444 sample under 1800 s.
- Report raw exits/scores/walls honestly; retain FAIL/slow samples.
