# Status matrix — DEV-448 (baseline attempt03 vs phase4 native attempt01)

## Population
- Baseline mutants: 283
- Phase4 native mutants: 283
- Unique stable ids: 283
- Baseline-only unmatched: 0
- Phase4-only unmatched: 0
- Duplicate baseline ids: 0
- Duplicate phase4 ids: 0
- Source content mismatches: 0

## Timeout conversion (must be zero Survived/Killed → Timeout)
- Survived → Timeout: 0
- Killed → Timeout: 0
- Timeout → Survived: 0
- Timeout → Killed: 0

## Status totals
| Status | Baseline | Phase4 native |
|---|---:|---:|
| Killed | 198 | 198 |
| Survived | 23 | 23 |
| Timeout | 0 | 0 |
| NoCoverage | 0 | 0 |
| Ignored | 41 | 41 |
| CompileError | 21 | 21 |

## Integrity reject: False

## Transition counts
- CompileError->CompileError: 21
- Ignored->Ignored: 41
- Killed->Killed: 198
- Survived->Survived: 23
