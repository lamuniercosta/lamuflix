# DEV-518 evidence runbook

Status: T002 baseline captured at pickup HEAD; Phase 6 T015-T018 evidence recorded. Before/after comparisons, gates, and home-library acceptance remain pending. Do not invent measurements, seed IDs, or publication success here. Never credentials.

## 0. Provenance

| Field | Value |
|---|---|
| Pickup SHA | `c516859dba97da61c0b8c42a083a8d59b29c8b46` |
| Baseline capture SHA | `c516859dba97da61c0b8c42a083a8d59b29c8b46` |
| After capture SHA | _pending_ |
| Gate-checked SHA | `4d4f0ae85fdbc65b27806f0dc81291dd81d91bf1` (Round 1 remediation: Related seed/candidate plot vectors, For You signal-inclusive MMR, once-per-operation keyword weights) |
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
| run-roslyn-analyzers.ps1 (required static) | PASS (exit 0, `-BaseRef origin/main`, 63 files) | `4d4f0ae85fdbc65b27806f0dc81291dd81d91bf1` | No CA/IDE warnings on target files |
| run-cyclomatic-complexity.ps1 (required static; incl. applicable refactor-gate threshold) | PASS (exit 0, `-BaseRef origin/main`, 63 files) | `4d4f0ae85fdbc65b27806f0dc81291dd81d91bf1` | No methods exceed threshold of 15 |
| run-jetbrains-inspectcode.ps1 (required static) | PASS (exit 0, `-BaseRef origin/main`, 63 files) | `4d4f0ae85fdbc65b27806f0dc81291dd81d91bf1` | No WARNING+ issues |
| dotnet format --verify-no-changes | Could not run | `4d4f0ae85fdbc65b27806f0dc81291dd81d91bf1` | Exit 1 from pre-existing workspace-load notices only (identical on pristine HEAD `6edf469b`; 0 of 1062 files need formatting, so no file change is required) |
| architecture tests | PASS | `4d4f0ae85fdbc65b27806f0dc81291dd81d91bf1` | `LamuFlix.ArchitectureTests` 16 passed, 0 failed |
| property tests (applicable) | PASS | `4d4f0ae85fdbc65b27806f0dc81291dd81d91bf1` | Ran inside `LamuFlix.UnitTests` full run (FsCheck suites green) |
| vulnerable packages / security checks (applicable) | _pending_ | | Not run in this remediation round |
| dotnet test (focused / full) | PASS | `4d4f0ae85fdbc65b27806f0dc81291dd81d91bf1` | `LamuFlix.UnitTests` 4075 passed, 1 skipped, 0 failed; `LamuFlix.Evaluation` 211 passed, 0 failed |
| mutation (when production C# under `src/` changes) | _pending_ | | Not run in this remediation round (minutes-expensive; pre-PR) |
| web (SKIPPED scope-empty if `/web` untouched) | SKIP | `4d4f0ae85fdbc65b27806f0dc81291dd81d91bf1` | `/web` untouched by this remediation |

Honest labels only: PASS / FAIL / SKIPPED (scope-empty) / SKIP / N/A / Could not run. Never fold N/A or non-pass into PASS. These rows are Phase B evidence slots — not claimed PASS at drafting.

## 6. Scrub

- [ ] No credentials, connection strings, or secrets in evidence paths or this file
- [ ] Snapshot artifacts remain unversioned under ignored outputs
- [ ] `git status` clean of personal captures before claiming delivery readiness

## 7. Phase 3 keyword-count query plan (T003/T007)

- PostgreSQL Testcontainers run against 1,000 movies and 1,000 `movie_keywords` memberships using `EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)` for the production grouping query.
- Plan: sequential scans of `movies`, `movie_keywords`, and `keywords`; hash joins; a quicksort using 56 kB; `GroupAggregate`; one aggregate for library `N`; 36 shared buffer hits. Planning time 0.358 ms; execution time 0.800 ms.
- Cost disposition: one full scan of the library population and keyword memberships is inherent in calculating `N` and all `n_k`; the existing plan is low-cost at this representative size and the operation is cached per process. No new index or schema change is justified. Planner estimates (225 movies / 2,260 memberships) differed from actual rows (1,000 / 1,000), so larger-library planning should be rechecked if the catalog grows materially.

## 8. Six-criteria compare output proof (T017, after T016 and T017)

Captured from the candidate branch by serializing a real compare-mode artifact built from the `CompareRunnerTests` pair fixture (`RecommendationArtifactSerializer.SerializeToUtf8Bytes`). The capture was a temporary harness; the retained regression `CompareRunnerTests.Run_pair_diagnostics_expose_all_six_ordered_criteria_and_contributions` pins the same contract: criterion order, the 3/3/3/2/2/2 weights, `weight x value / 15` per contribution, contributions summing to Score, and a non-zero Plot value from the persisted embedding.

```json
{"mode":"compare","targets":[1,2],"lists":[{"kind":"compare","targetId":1,"take":null,"status":"ok","rows":[{"rank":1,"movieId":2,"title":"Other","score":0.7333333333333333,"reasons":[{"kind":"structural","text":"Same collection: Franchise"},{"kind":"structural","text":"Same director: Ada"},{"kind":"structural","text":"Similar story"},{"kind":"structural","text":"Shares cast: Ann"},{"kind":"structural","text":"Same genres: Drama"}],"diagnostics":{"breakdown":[{"criterion":"Collection","value":1,"weight":3,"contribution":0.2},{"criterion":"Director","value":1,"weight":3,"contribution":0.2},{"criterion":"Themes","value":0,"weight":3,"contribution":0},{"criterion":"Plot","value":1,"weight":2,"contribution":0.13333333333333333},{"criterion":"Cast","value":0.5,"weight":2,"contribution":0.06666666666666667},{"criterion":"Genre","value":1,"weight":2,"contribution":0.13333333333333333}],"rawPlotCosine":1,"plotRank":1,"inPool":true}}],"matchedNames":[{"criterion":"Cast","name":"Ann"},{"criterion":"Collection","name":"Franchise"},{"criterion":"Director","name":"Ada"},{"criterion":"Genre","name":"Drama"},{"criterion":"Themes","name":"heist"}]}]}
```

Notes on the capture: all six criteria and their contributions print in table order; the reasons list follows the same order; matched names use the Themes criterion name and carry no Writer entry; Themes is 0 in this fixture because the fixture snapshot declares no keyword-count population, which exercises the zero-norm case rather than a missing criterion.

## 9. T018 legacy absence scan and suite re-run

Scan scope: `src/LamuFlix.Core/Features/Recommendations`, `src/LamuFlix.Core/Options`, `tests/LamuFlix.UnitTests/Features/Recommendations`, `tests/LamuFlix.UnitTests/Options`, plus the evaluation and evaluation-integration fixture surfaces aligned in T017.

| Token / pattern | Hits | Verdict |
|---|---|---|
| `YearProximity`, `RuntimeProximity`, `RatingBoost` | 0 | absent |
| `SimilarStoryThreshold` | 0 | absent |
| `Similar release years`, `Similar runtime`, `Similar rating`, `Shared writers`, `Identical feature profile` | 0 | absent |
| `SimilarityWeights.Keyword`, `Weights.Keyword`, `Keyword =` in weight initializers | 0 | absent |
| `"keyword"` / `"Keyword"` weight keys in fixtures, config, and evaluation settings | 0 | absent after the T017 fixture alignment |
| `Writers` | `MovieFeatures.Writers`, its mapper pass-through, immutability/hash members, and `MovieFeaturesMapperTests` | retained by design: T005 keeps the type field while the ordered scorer never reads it |

Re-run on the candidate branch:

| Suite | Result |
|---|---|
| `LamuFlix.Evaluation` | 211 passed, 0 failed (10 failed at the Phase 5 checkpoint; all ten were stale evaluation fixtures and expectations, corrected without weakening assertions) |
| `LamuFlix.Evaluation.IntegrationTests` | 68 passed, 0 failed |
| `LamuFlix.UnitTests` | 4072 passed, 1 skipped, 0 failed |
| `LamuFlix.ArchitectureTests` | 16 passed, 0 failed |

These are test results, not gate receipts. The required static gates, format check, mutation run, after snapshots, and home-library acceptance remain T019-T021 work.
