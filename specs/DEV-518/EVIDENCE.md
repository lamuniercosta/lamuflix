# DEV-518 evidence runbook

Status: T002 baseline captured at pickup HEAD; T003+ remain pending. Do not invent measurements, seed IDs, or publication success here. Never credentials.

## 0. Provenance

| Field | Value |
|---|---|
| Pickup SHA | `c516859dba97da61c0b8c42a083a8d59b29c8b46` |
| Baseline capture SHA | `c516859dba97da61c0b8c42a083a8d59b29c8b46` |
| After capture SHA | _pending_ |
| Gate-checked SHA | _pending_ |
| DEV-545 tooling SHA / notes | `scripts/run-recommendation-snapshot.ps1` SHA-256 `ae07c64c1137ea2357cb9ff1fd54011d577034f83eef5670183c5de30a10b7e8`; captured from `http://localhost:3100` at pickup HEAD before DEV-518 scoring edits; DEV-545 seed lookup receipt: `specs/DEV-545/EVIDENCE.md` §0, T019a. |

## 1. Five Related seed IDs (DEV-545 ordered titles)

| # | Title | Home library ID |
|---|---|---|
| 1 | A Fistful of Dollars | `575` |
| 2 | About Time | `518` |
| 3 | A Few Good Men | `515` |
| 4 | Aladdin | `519` |
| 5 | The Wind Rises | `831` |

Lookup notes / command references (no secrets): DEV-545 one-time home-library lookup documented in `specs/DEV-545/EVIDENCE.md` §0; explicit `-SeedIds 575,518,515,519,831` supplied to the capture command.

## 2. Before snapshots (must precede scoring edits)

| Artifact | Path / hash | Status |
|---|---|---|
| Related ×5 baseline | `artifacts/recommendations/DEV-518-baseline.json`; SHA-256 `b104dacc7e3a610e07d1f3d07c8d9baf7f68ce9b145c27c2ab1900f6edab5ba8` | captured: five ordered targets, each HTTP success with 10 rows |
| For You baseline (Genres default comparison) | `artifacts/recommendations/DEV-518-baseline.json`; SHA-256 `b104dacc7e3a610e07d1f3d07c8d9baf7f68ce9b145c27c2ab1900f6edab5ba8` | captured: HTTP success with 30 rows |

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
