# DEV-518 evidence runbook

Status: Phase A slots only — fill during Phase B. Do not invent measurements, seed IDs, or publication success here. Never credentials.

## 0. Provenance

| Field | Value |
|---|---|
| Pickup SHA | _pending_ |
| Baseline capture SHA | _pending_ |
| After capture SHA | _pending_ |
| Gate-checked SHA | _pending_ |
| DEV-545 tooling SHA / notes | _pending_ |

## 1. Five Related seed IDs (DEV-545 ordered titles)

| # | Title | Home library ID |
|---|---|---|
| 1 | A Fistful of Dollars | _pending_ |
| 2 | About Time | _pending_ |
| 3 | A Few Good Men | _pending_ |
| 4 | Aladdin | _pending_ |
| 5 | The Wind Rises | _pending_ |

Lookup notes / command references (no secrets): _pending_

## 2. Before snapshots (must precede scoring edits)

| Artifact | Path / hash | Status |
|---|---|---|
| Related ×5 baseline | _pending_ | pending |
| For You baseline (Genres default comparison) | _pending_ | pending |

## 3. After snapshots

| Artifact | Path / hash | Status |
|---|---|---|
| Related ×5 after | _pending_ | pending |
| For You after | _pending_ | pending |
| Diff / comparison notes | _pending_ | pending |

## 4. Home-library acceptance (Fistful)

| Check | Result |
|---|---|
| Related for A Fistful of Dollars lists The Good, the Bad and the Ugly | _pending_ |
| Reasons include Same director | _pending_ |
| Reasons include Shared themes | _pending_ |
| No year/runtime/rating/writers/identical-profile reasons on that card | _pending_ |

## 5. Gates (changed C#) — T019 executable checklist (`plan.md` governance row)

| Gate | Exit / verdict | SHA | Notes |
|---|---|---|---|
| run-roslyn-analyzers.ps1 (required static) | _pending_ | | MUST pass |
| run-cyclomatic-complexity.ps1 (required static; incl. applicable refactor-gate threshold) | _pending_ | | MUST pass |
| run-jetbrains-inspectcode.ps1 (required static) | _pending_ | | MUST pass |
| dotnet format --verify-no-changes | _pending_ | | MUST pass |
| architecture tests | _pending_ | | |
| property tests (applicable) | _pending_ | | |
| vulnerable packages / security checks (applicable) | _pending_ | | |
| dotnet test (focused / full) | _pending_ | | |
| mutation (when production C# under `src/` changes) | _pending_ | | |
| web (SKIPPED scope-empty if `/web` untouched) | _pending_ | | |

Honest labels only: PASS / FAIL / SKIPPED (scope-empty) / SKIP / N/A / Could not run. Never fold N/A or non-pass into PASS. These rows are Phase B evidence slots — not claimed PASS at drafting.

## 6. Scrub

- [ ] No credentials, connection strings, or secrets in evidence paths or this file
- [ ] Snapshot artifacts remain unversioned under ignored outputs
- [ ] `git status` clean of personal captures before claiming delivery readiness
