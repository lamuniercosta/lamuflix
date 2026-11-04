# Status matrix — DEV-444 (baseline attempt01 vs phase4 native attempt01)

## Population
- Baseline mutants: 98
- Phase4 native mutants: 98
- Unique stable ids: 98
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
| Killed | 36 | 36 |
| Survived | 28 | 25 |
| Timeout | 4 | 4 |
| NoCoverage | 0 | 3 |
| Ignored | 23 | 23 |
| CompileError | 7 | 7 |

## Integrity reject: False

## Transition counts
- CompileError->CompileError: 7
- Ignored->Ignored: 23
- Killed->Killed: 36
- Survived->NoCoverage: 3
- Survived->Survived: 25
- Timeout->Timeout: 4
