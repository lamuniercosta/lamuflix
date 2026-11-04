# DEV-451 T005 findings — matched candidate measurements (Gauge)

**Status:** measurements complete; experimental scratch provenance only; **not** native shipping-gate success; **not** T006 ratification; tracked scripts untouched.

**Candidate:** Stryker 5.0.0 MTP / `coverage-analysis: perTestInIsolation` via external `%TEMP%/DEV-451-scratch-gate/` copy (D1–D4 + D1b early `$script:ScratchCoverageAnalysis` capture; **zero guard delta**).
**Control:** accepted baselines (DEV-444 attempt01, DEV-448 attempt03, DEV-333 attempt01). No control reruns.
**Pinned:** concurrency 8, additional-timeout 90000, mtp, since disabled, machine ALIEN, serial quiet-machine ownership.

## Scratch gate

| Item | Value |
|---|---|
| Archive | `specs/DEV-451/evidence/candidate/_gate/` |
| Original SHA256 | `2A0A8FF8A80E87FE326BB0537AE83C36F82E5587068F67656F6DBE5F06E052BE` |
| Candidate SHA256 | `A7184D91350028B77C24B5D35CCDF3C69817306EA2824E787C9AB67D42CA79BE` |
| Manifest digest | `0A20E74482C997D7C507DFD15D037BFFB2FC63658F7D7F08C338D89820350B62` |
| Guard delta | zero (`:973-977` byte-identical) |
| Case-collision note | First preflight exited 1 with `Invalid -CoverageAnalysis value 'off'` because PowerShell case-insensitivity let the guard local `$coverageAnalysis` overwrite param `$CoverageAnalysis`. Corrected via D1b early script-scoped capture before any measurement archive. Collision log retained in `_gate/first-preflight-case-collision.log`. |

Tracked `scripts/run-mutation.ps1`, `_harness-config.ps1`, `_gate-common.ps1`, `Test-RunMutation.ps1`, `harness.yml`, `stryker-config.json` remain at frozen hashes (run-mutation `2A0A8FF8…`).

## Wall-time / verdict matrix

| Case | Baseline native s | Candidate native s | Δ | Wrapper exit B→C | Score B→C | Tested | Timeouts B→C |
|---|---:|---:|---:|---|---|---:|---|
| DEV-444 | 1088.7 | **840.3** | −248.4 (−22.8%) | 1→1 (FAIL) | 58.82%→58.82% | 68 | 4→4 |
| DEV-448 | 3385.7 | **880.0** | −2505.7 (−74.0%) | 0→0 (PASS) | 89.59%→89.59% | 221 | 0→0 |
| DEV-333 | 1233.3 | **919.4** | −313.9 (−25.5%) | 0→0 (PASS; Infra N/A) | 86.76%→86.76% | 68 | 4→4 |

All three candidate walls are under 1800 s. T012/T013 under-30-minute acceptance remains a separate **native shipping-gate** bar on DEV-444 after T006/T010/T011 — this scratch evidence does not satisfy that bar by itself.

## Stable-identity integrity

Keyed by normalized path + full location + mutatorName + replacement.

| Case | Population | Dup/unmatched | S→Timeout | K→Timeout | integrityReject | Coverage-derived transitions |
|---|---|---|---:|---:|---|---|
| DEV-444 | 98=98 | 0 | **0** | **0** | false | 3× Survived→NoCoverage (NoCoverage remains in tested denominator; score unchanged) |
| DEV-448 | 283=283 | 0 | **0** | **0** | false | none (identical status totals) |
| DEV-333 | 104=104 | 0 | **0** | **0** | false | none (identical status totals) |

Timeout totals did not grow. No suite omitted. Policy/share/span safeguards unchanged on tracked EvaluateReport:

| Case | Eval exit | Kill share | Non-exempt span | Notes |
|---|---:|---|---|---|
| DEV-444 | 1 | 35.29%/50% | 0/15 (32 exempt; widest exempt span 1) | Recommendation exemptions weaken span; share and per-mutant statuses inspected; FAIL retained |
| DEV-448 | 0 | 33.48%/50% | 1/15 (77 exempt) | Matches accepted attempt03 control |
| DEV-333 | 0 | 20.59%/50% | 1/15 | Infrastructure NOT APPLICABLE retained |

## Artifact roots

- `_gate/` — original/candidate/support bytes, delta, manifest, construction receipt, smoke + collision logs
- `DEV-444/attempt01/`, `DEV-448/attempt01/`, `DEV-333/attempt01/` — console, preflight, gauge-timing, ownership, eval-console/exit, receipt, output/ (timing+config+report), status-matrix.json + status-matrix-summary.md

## Disposition for T006

- Speed and verdict-integrity bars are **met on this scratch evidence** for all three frozen cases (zero S→Timeout / K→Timeout; matched populations; unchanged scores/exits; large wall-time reductions).
- Shipping adoption still requires: T006 Patron ratification (incl. ADR 0016 Decision departure → mandatory in-place T010 amendment before T011); cited Charter §2.3 STOP if T011 needs `ci-stryker-shard.sh` / doc consumer reconciliation; D1b case-collision capture must be part of any shipping script design.
- No lever is ratified by Gauge. No tracked script edit was performed. T005 complete as measurement delivery.
