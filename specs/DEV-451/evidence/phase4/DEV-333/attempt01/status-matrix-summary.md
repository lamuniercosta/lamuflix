# Status matrix — DEV-333 (baseline attempt01 vs phase4 native attempt01)

## Population
- Baseline mutants: 104
- Phase4 native mutants: 104
- Unique stable ids: 104
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
| Killed | 55 | 55 |
| Survived | 9 | 9 |
| Timeout | 4 | 4 |
| NoCoverage | 0 | 0 |
| Ignored | 28 | 28 |
| CompileError | 8 | 8 |

## Integrity reject: False

## Transition counts
- CompileError->CompileError: 8
- Ignored->Ignored: 28
- Killed->Killed: 55
- Survived->Survived: 9
- Timeout->Timeout: 4
