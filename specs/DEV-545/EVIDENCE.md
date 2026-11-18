# DEV-545 evidence runbook

Status: Phase B partially filled. T019 (seed lookup and literal `-SeedIds` defaults) is complete and evidenced in §1. T019a ran and **stopped on an identity mismatch**: the deployed home API is build `b98a4b0a` (99 commits behind `origin/main` `8a2deb9b`), with recommendation content differences and an off-main Argo home revision. T020 therefore did not start; §2/§3/§4/§5 runtime slots are recorded as `Could not run`, not as passes. T021's scrub half is complete in §6; its after-state half is `Could not run`. Nothing here is invented, and there are no credentials.

## 0. Provenance / T019a identity manifest (never credentials; separate from deterministic capture JSON)

Fill **before** T020 (T019a). Refresh and recheck immediately before T020 and again before T024 publication. If identity is invalidated after this gate, re-run T019a → T020 → T021 (recapture) before claiming the baseline. A raw file hash alone is **not** semantic equivalence proof.

### Operator commands (nonsecret)

```powershell
git fetch origin main
git rev-parse origin/main
git rev-parse HEAD
git status --porcelain
git diff origin/main -- `
  src/LamuFlix.Core/Features/Recommendations/ `
  src/LamuFlix.Core/Options/RecommendationOptions.cs `
  src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs `
  src/LamuFlix.Infrastructure/Persistence/EfRecommendationCandidateSource.cs
Get-FileHash -Algorithm SHA256 <path-to-relevant-built-assembly-or-artifact>
```

Record API running build/config identity (assembly/informational version or equivalent nonsecret build stamp) as well as offline harness identity. Record effective nonsecret `RecommendationOptions`: weights, `CandidatePoolSize`, `MmrLambda`, takes. Never print connection strings.

Any mismatch in main SHA, relevant production content, build, harness, or effective config **stops** for reconciliation/recapture. Feature-only diagnostic seam differences require bounded `git diff` plus score/reason invariance test receipts.

| Field | Value |
|---|---|
| Current `origin/main` SHA | `8a2deb9b7a971b50108721aa8465620e551f1cfa` (`git fetch origin main` re-run immediately before this gate; commit dated 2026-11-18T07:52:33-03:00, "DEV-522 - remediate Sentry F1 bulk snapshot N+1 and Ledger F1 JSDoc") |
| Harness SHA (`HEAD`) | `89a51cf155524d003433522eef9473b9c35c5eee` at gate time (Phase 7 receipt commit). The T019 implementation rides on top of it in the Phase 8 commit named in the phase receipt below; T019a must be rechecked against that SHA before any T020 attempt |
| Harness dirty status | 2 modified, 0 staged, 0 untracked: `scripts/run-recommendation-snapshot.ps1` and `tests/LamuFlix.Evaluation.IntegrationTests/Entry/RecommendationSnapshotCommandTests.cs` (the T019 seed-default edit under test). No output/judgment files present (`artifacts/` is ignored) |
| Relevant production source diff vs main (summary / receipt path) | Branch-vs-`origin/main` on the recommendation surface is exactly two files, both diagnostic-seam only: `SimilarityResult.cs` +7/−1 (adds non-positional `Breakdown` + `CriterionContribution`) and `SimilarityScorer.cs` +112/−17 (populates it, `IdentityFallback` on zero-weight). No change to weights, options, candidates, profile source or handlers. `EfMovieCatalog.cs` also appears in `git diff origin/main -- src/` but that is **main drift**, not branch work: it is unchanged across `merge-base..HEAD` and was rewritten on main by DEV-522 after this branch's base |
| API running build/config identity | Pod `api-5cfb5596c7-t87q8`, image `ghcr.io/lamuniercosta/lamuflix-api@sha256:5a63b1377e6853015348c551d9314a75cf27b4e9836b8b385d7adb7524505282`; OCI label `org.opencontainers.image.revision` = `b98a4b0a6287673e56015145862be43c2e3dd3d0` (2026-11-15T15:28:32-03:00, "DEV-475 - Record Phase 5 step 3 ship-review Q16 ruling"). That revision is **99 commits behind** `origin/main`. Argo application `lamuflix-home` reports Synced/Healthy at revision `bd0a83fe4cbef2519fd841450f864a0732b5f9bc` ("home: promote b98a4b0a"), which is **not** an ancestor of `origin/main` |
| Effective nonsecret RecommendationOptions | The pod injects only `Ollama__Endpoint`, `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME`, `Tmdb__Enabled`, `Tmdb__ApiKey`; no `Recommendations` env or config override exists (`kubectl get deploy api` contains no `recommendations` key), so code defaults bind: `Take` 10, `CandidatePoolSize` 50, `MmrLambda` 0.7, `RecencyDecayLambda` ln2/30, `SimilarStoryThreshold` 0.5, blend 1.0/1.0, penalties 0.25/1.0, review 1.0/1.0, weights Director 3.0, Writers 2.0, Cast 2.0, Genres 3.0, YearProximity 1.0, RuntimeProximity 0.5, RatingBoost 0.5, Keyword 1.0, Collection 2.0, ReviewPlotWeight 1.0. Harness takes: For you 30, each Related 10 |
| Production scoring equivalence judgment + invariance test receipts | **MISMATCH — gate STOP.** The running API revision `b98a4b0a` predates main's recommendation work: `git diff b98a4b0a origin/main` touches `GetForYouRecommendationsQueryHandler.cs`, `GetRecommendationsQueryHandler.cs`, `ProfileSimilarityCalculator.cs`, `TasteProfileCalculator.cs`, `RecommendationProfileInputs.cs`, `EfRecommendationProfileSource.cs` and `HybridCachedRecommendationProfileSource.cs` (147+/25−). Scoring behaviour is therefore not equivalent to main, and a file hash cannot prove otherwise. Branch-side receipts exist and are green (see below), but they cover the branch against main, not the deployed build against main |
| Drift reconciliation note (or "none") | **Required, and not reconcilable from this seat.** Two independent mismatches: (1) deployed API build `b98a4b0a` ≠ `origin/main` `8a2deb9b` on recommendation content; (2) the Argo home promotion commit `bd0a83fe` is off-main, so "the home instance" is not a main build at all. Redeploying or re-pinning the home overlay to a main build is an operator/environment action outside a Build-lane code commit. Recapture would then be T019a → T020 → T021 in full |
| Manifest recheck before T020 | **STOPPED.** T020 did not start, per "T020 MUST NOT start until T019a passes". Recorded `Could not run`, not a pass |
| Manifest recheck before publication | N/A — no T020 run exists to publish; T024 must not attach anything until T019a passes |
| Cohort counts (liked, watched, overlap) | Not measured — requires the real-library run that T019a blocked |

#### Branch-side invariance receipts gathered during this gate (not a substitute for the deployed-build check)

| Check | Value |
|---|---|
| `dotnet test tests/LamuFlix.UnitTests --filter "FullyQualifiedName~Features.Recommendations"` | 635 passed, 0 failed — includes `SimilarityScorerTests` and `SimilarityResultBreakdownTests` |
| Expected-score fixture edits in `SimilarityScorerTests.cs` vs `merge-base` | 0 deletions — the diff is 85 pure additions (breakdown assertions); no expected score was retuned |
| `pwsh -NoProfile -File ./scripts/run-evaluation.ps1` | exit 0; summary byte-identical to the T002 receipt: `{"schemaVersion":1,"fixtureId":"dev-356-baseline","seed":356,"cutoff":10,"observationCount":12,"hitCount":9,"reciprocalRankSum":2.7484126984126984,"hitRateAt10":0.75,"mrr":0.22903439153439153}` |
| `baseline.json` and `scripts/run-evaluation.ps1` vs `origin/main` | unchanged (empty diff) |
| SHA-256 `SimilarityScorer.cs` | `FEB56F66CC698D823B1CB63922874182E004539E6F0922B6B6DE6F2F661E0756` |
| SHA-256 `SimilarityResult.cs` | `60B253B3540EB571A70BA99E3B69B4B1C98C9FA3AB53CF3B4F006A9D2E7C2A50` |
| SHA-256 `RecommendationOptions.cs` | `10BF864D2A67EF5B1FB7877CA59DE61740E9C98B29DEB2356515D6C8D90B3434` (identical to the T002 pre-edit note — options untouched) |

### T002 pre-edit identity note (provenance only — does **not** fill the T019a manifest above)

Recorded by the Build lane immediately before the T003 shared-contract edits, so a later T019a run has a reference point. It is not a scoring-equivalence claim and must not be cited as one.

| Observation | Value |
|---|---|
| `origin/main` SHA at T002 | `8a2deb9b7a971b50108721aa8465620e551f1cfa` |
| Worktree `HEAD` at T002 | `dca52fc8c7ba9886848785a8e48fefca85084d1b` |
| `git diff origin/main` on production recommendation paths (`Features/Recommendations/`, `Options/RecommendationOptions.cs`, `EfRecommendationProfileSource.cs`, `EfRecommendationCandidateSource.cs`) | empty — identical to main |
| `SimilarityScorer.cs` SHA-256 (first 16) | `CE36CAE3F01A181A` |
| `SimilarityResult.cs` SHA-256 (first 16) | `696A85859D7AB4C0` |
| `RecommendationOptions.cs` SHA-256 (first 16) | `10BF864D2A67EF5B` |
| `EfRecommendationProfileSource.cs` SHA-256 (first 16) | `8914A2C77297F4B5` |
| `EfRecommendationCandidateSource.cs` SHA-256 (first 16) | `91AC356A18087FB9` |
| Synthetic publish receipt at T002 | `pwsh -NoProfile -File ./scripts/run-evaluation.ps1` exit 0; summary `{"schemaVersion":1,"fixtureId":"dev-356-baseline","seed":356,"cutoff":10,"observationCount":12,"hitCount":9,"reciprocalRankSum":2.7484126984126984,"hitRateAt10":0.75,"mrr":0.22903439153439153}` |
| T003 evaluation-project test receipt | `dotnet test tests/LamuFlix.Evaluation` — 112 passed, 0 failed |

### Phase 3 (T004–T006) breakdown seam receipts

| Check | Value |
|---|---|
| `dotnet test tests/LamuFlix.UnitTests --filter "FullyQualifiedName~Features.Recommendations"` | 635 passed, 0 failed (includes `SimilarityResultBreakdownTests`, `SimilarityScorerTests` extended, breakdown sum property) |
| `dotnet test tests/LamuFlix.UnitTests` (full project) | 4032 passed, 1 skipped, 0 failed |
| `dotnet test tests/LamuFlix.Evaluation` | 112 passed, 0 failed |
| `pwsh -NoProfile -File ./scripts/run-evaluation.ps1` | exit 0; summary byte-identical to the T002 receipt above (synthetic publish unchanged) |
| `./scripts/run-roslyn-analyzers.ps1` | exit 0, passed |
| `./scripts/run-cyclomatic-complexity.ps1` | exit 0, passed (threshold 15) |
| `./scripts/run-jetbrains-inspectcode.ps1` | exit 0, passed (no WARNING+) |
| `dotnet format --verify-no-changes` | exit 0 |
| Expected-score fixture edits | none — `SimilarityScorerTests` expected scores untouched; new assertions added only |
| Public DTO / OpenAPI / TS | untouched (`SimilarityResult`/`CriterionContribution` are internal Core types, not DTOs) |
| Blend constructions | `SimilarityResult` two-argument construction keeps `Breakdown` empty (asserted in `SimilarityResultBreakdownTests`) |

### Phase 4 (T007–T009) read-only loader and hidden-seed adapter receipts

| Check | Value |
|---|---|
| `dotnet test tests/LamuFlix.Evaluation` | 112 passed, 0 failed (synthetic evaluation unchanged) |
| `dotnet test tests/LamuFlix.Evaluation.IntegrationTests` (full project) | 28 passed, 0 failed (7 new `RealLibraryReadOnlyLoaderTests` + 21 existing command tests) |
| `pwsh -NoProfile -File ./scripts/run-evaluation.ps1` | exit 0; summary byte-identical to the T002 receipt above (synthetic publish unchanged) |
| `./scripts/run-roslyn-analyzers.ps1` | exit 0, passed |
| `./scripts/run-cyclomatic-complexity.ps1` | exit 0, passed (threshold 15) |
| `./scripts/run-jetbrains-inspectcode.ps1` | exit 0, passed (no WARNING+) |
| `dotnet format --verify-no-changes` | exit 0 |
| Production `src/` seam | none added — loader + hidden-seed adapter are runner-local under `tests/LamuFlix.Evaluation/RealLibrary/` (Q3 preferred path) |
| Credential transport | presence-only `RealLibrarySnapshotLoader.IsCredentialPresent(Func<string,string?>)`; no value read, echoed, or logged |
| Read-only proof | `ChangeTracker.Entries()` empty after load; write-command interceptor count 0 on load and on hidden-seed ranking |
| New dependency | `Testcontainers.PostgreSql` PackageReference on `LamuFlix.Evaluation.IntegrationTests.csproj` using the existing central pin; no version change |



## 1. Seed ID lookup (Q2)

Ordered titles → home-library integer IDs (one-time lookup against the running home instance at `http://localhost:3100`, `GET /api/movies?text=<title>&sort=Title&direction=Ascending&page=1&pageSize=100`). Each title matched exactly one row (`totalCount` 1). Never use fixture About Time ID 90 — 90 is not in this set.

| Order | Title | Home movie ID |
|---|---|---|
| 1 | A Fistful of Dollars | `575` (1967) |
| 2 | About Time | `518` (2013) |
| 3 | A Few Good Men | `515` (1992) |
| 4 | Aladdin | `519` (1992) |
| 5 | The Wind Rises | `831` (2014) |

Literal defaults/help populated in `scripts/run-recommendation-snapshot.ps1`: yes — `[string[]]$SeedIds = @('575', '518', '515', '519', '831')`, the `.PARAMETER SeedIds` block and the `-Help` usage text list all five title→id pairs, and `$seedTitles`/`$seedDefaultIds` feed the rendered help so the two never drift. Explicit overrides still work; validation (exactly five, positive, distinct) is unchanged and still runs before any HTTP request.

| Check | Result |
|---|---|
| Cross-check that each id is the expected movie | `GET /api/movies/{id}` returned the matching title for all five; all five `EnrichmentStatus` = `Enriched` |
| All five Related endpoints reachable for these ids | `GET /api/movies/{575,518,515,519,831}/recommendations?take=10` → 200 for each (8073/7875/7606/9108/8369 bytes) |
| Parameter-free Capture works (defaults, no `-SeedIds`) | `pwsh -NoProfile -File scripts/run-recommendation-snapshot.ps1 -Mode Capture -OutputPath <temp>\capture-a.json` exit 0, published; artifact `targets` = `575,518,515,519,831`; For you 30 rows; all five Related 10 rows; 37781 bytes |
| Repeated parameter-free capture identical | capture A SHA-256 `AD878F612789EEAF7EEA4B23CCBDFDA84860DA2E51105712B500B5ACF2A7F820`, capture B identical SHA-256 |
| Fixture ID 90 excluded | yes — `575,518,515,519,831` does not contain 90 |

Disclosure: the two captures above were written to a temporary operator path purely to prove the T019 defaults produce a working, byte-stable parameter-free run. They are **not** T020 snapshot-determinism evidence — T019a stopped before T020, so §2 stays unpopulated and no published baseline artifact was created. Raw output stays unversioned.

## 2. Snapshot determinism (Q6) — paired quiescent recommendation-input fingerprint

**Precondition**: no scan/enrichment job and no user mutation during the Capture×2 window and the DB proof window that follows. No new production job controls.

**Paired slots (required)**: record a recommendation-input data fingerprint/count **before** Capture A and a matching **after** observation after Capture B (same window that also covers the later DB before/after proof). Include library/recommendation row counts plus stable pending/enrichment-state counts (or equivalent stable fingerprints of those inputs). Require explicit before==after equality. On mismatch: **discard** that Capture×2 attempt (do not interpret capture hashes), restore quiescence, and **retry** the window. Do not treat every mismatch as a script defect.

| Check | Result |
|---|---|
| Quiescence precondition verified (no scan/enrichment/user mutation) | **Could not run** — blocked by the T019a identity gate stop (deployed API `b98a4b0a` ≠ `origin/main`, and Argo home revision off-main). No quiescent window was attempted |
| **Before** recommendation-input fingerprint/counts (row counts + pending/enrichment state) | **Could not run** — not captured; capturing after a blocked gate would be meaningless |
| Capture A path + hash | **Could not run** (no T020 capture). The T019 script-verification captures in §1 are explicitly not this evidence |
| Capture B path + hash (unchanged data) | **Could not run** |
| Capture hashes identical | **Could not run** |
| **After** recommendation-input fingerprint/counts (same fields as before) | **Could not run** |
| Before==after fingerprint/counts equality | **Could not run** |
| Mismatch → discarded/retried before hash interpretation (or "n/a — equal") | N/A — no window ran |
| Diff five groups observed (same-mode operands) | **Could not run** |

## 3. Leave-one-out (Q3–Q4)

### Credential precondition (never assign or echo)

`ConnectionStrings__DefaultConnection` MUST already be present in the process environment from a pre-provisioned operator setup outside these copyable examples. Tests and operators MUST validate **presence only** (for example `Test-Path Env:ConnectionStrings__DefaultConnection`) and MUST NOT read, echo, log, or print the value. Never put the connection string in CLI arguments, shell history, evidence, provenance, notes, or publication. Do **not** include `$env:ConnectionStrings__DefaultConnection = ...` in any copyable command.

### Operator invocation (scope mode + nonsecret markers; restore afterward)

```powershell
# Precondition: ConnectionStrings__DefaultConnection already present (presence-only check; never echo).
if (-not (Test-Path Env:ConnectionStrings__DefaultConnection)) { throw 'ConnectionStrings__DefaultConnection missing' }

$priorMode = $env:LAMUFLIX_EVALUATION_MODE
$priorOutput = $env:LAMUFLIX_EVALUATION_OUTPUT_PATH
try {
  $env:LAMUFLIX_EVALUATION_MODE = 'leave-one-out'
  $env:LAMUFLIX_EVALUATION_OUTPUT_PATH = 'artifacts/recommendations/leave-one-out.json'
  dotnet run --project tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj -- -class LamuFlix.Evaluation.Entry.RealLibraryEntryTests -noLogo -noColor
}
finally {
  if ($null -eq $priorMode) { Remove-Item Env:LAMUFLIX_EVALUATION_MODE -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_MODE = $priorMode }
  if ($null -eq $priorOutput) { Remove-Item Env:LAMUFLIX_EVALUATION_OUTPUT_PATH -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_OUTPUT_PATH = $priorOutput }
}
```

Inertness check: ordinary `dotnet test` **without** `LAMUFLIX_EVALUATION_MODE=leave-one-out|compare` must not execute real-library modes (covered by `EvaluationEntryModeTests (extended)` and `RealLibraryEntryTests`). Mode and nonsecret markers MUST be restored/removed so copyable examples do not leave real-library mode enabled. The `-class LamuFlix.Evaluation.Entry.RealLibraryEntryTests` filter is required: without it the child runs the whole evaluation suite.

| Check | Result |
|---|---|
| Liked HR@1/3/10, MRR, seed count | **Could not run** — blocked by T019a |
| Watched HR@1/3/10, MRR, seed count | **Could not run** |
| Overlap disclosed | **Could not run** |
| Bypass disclosure present (endpoint/validator + full-eligible take) | Present in code/report design (Phase 5 receipts above); no runtime report was produced |
| Harness-relative HR/MRR validity disclaimer present | Present in report design; no runtime report was produced |
| **Before-state** row counts (captured and preserved **before** leave-one-out begins — T020) | **Could not run** — no run occurred, so there is no before-state to preserve |
| **Before-state** every movie review value (same timing — T020) | **Could not run** |
| **After-state** row counts (captured **immediately after** the same leave-one-out run — T021) | **Could not run** |
| **After-state** every movie review value (same timing — T021) | **Could not run** |
| Before vs after comparison: row counts and every review value unchanged (SC-003 ticket-required boundary) | **Could not run** — deliberately not substituted with a post-hoc dump, which could not prove non-mutation anyway |
| Explicit disclosure: counts+reviews do **not** prove every untested column or transient write | Disclosed as a standing limitation of this evidence type; still applies whenever the run happens |
| No save/add/update/remove in new LOO path (reviewer read of complete loaded path) | Static code-path evidence stands from Phase 5; runtime confirmation remains `Could not run` |
| Testcontainers no-write proof receipt (`RealLibraryReadOnlyLoaderTests`) | `dotnet test tests/LamuFlix.Evaluation.IntegrationTests` — 67 passed, 0 failed on this diff (re-run this phase), including the loader no-tracking/no-write tests |

Note: Evidence acquired only after the run cannot prove non-mutation during that run. Keep raw personal before/after dumps unversioned. This is bounded ticket-required behavioral evidence, not a waiver of the strict no-write contract.

### Phase 5 (T010–T012) leave-one-out and compare receipts

| Check | Value |
|---|---|
| `dotnet test tests/LamuFlix.Evaluation` | 146 passed, 0 failed (synthetic publish unchanged) |
| `dotnet test tests/LamuFlix.Evaluation.IntegrationTests` | 32 passed, 0 failed (4 new `RealLibraryRunnerTests`, including the two operator-entry paths) |
| `dotnet test tests/LamuFlix.UnitTests` | 4032 passed, 1 skipped, 0 failed |
| `pwsh -NoProfile -File ./scripts/run-evaluation.ps1` | exit 0; summary byte-identical to the T002 receipt above |
| `./scripts/run-roslyn-analyzers.ps1` | exit 0, passed |
| `./scripts/run-cyclomatic-complexity.ps1` | exit 0, passed (threshold 15) |
| `./scripts/run-jetbrains-inspectcode.ps1` | exit 0, passed (no WARNING+) |
| `dotnet format --verify-no-changes` | exit 0 |
| Production `src/` seam | none added — LOO and compare are runner-local under `tests/LamuFlix.Evaluation/RealLibrary/` |
| LOO denominator | `RealLibraryMetricCalculator` over one shared `MetricCalculator.Accumulate` path; attempted seeds per cohort; absent target contributes 0; zero cohort → null metrics |
| LOO take | eligible candidate count from the immutable snapshot; endpoint/validator bypass and harness-relative validity are emitted as report `disclosures` |
| Compare population | every other `EnrichmentStatus.Enriched` row retained; missing/wrong-dimension/nonfinite/zero-norm vectors emit null `rawPlotCosine`/`plotRank`, never a fabricated 0.0 |
| Compare ordering | score desc then movie id asc; plot rank cosine desc then id asc among usable pairs only |
| Pool flag | ticket formula `(plotRank is not null && plotRank <= CandidatePoolSize) || sameCollection`, disclosed as diagnostic |
| Pair inspect | directed pair only; matched names from runner-local loaded feature names; no reason-text parsing and no new scoring term |
| Credential transport | presence-only gate in `EvaluationEntry.RunRealLibraryAsync`; value never echoed, included in an exception message, or written to an artifact |
| Mode scope | `RealLibraryModeScope` restores every prior nonsecret marker and removes markers that were absent before |
| Inertness | `RealLibraryEntryTests` + `EvaluationEntryModeTests` (extended) prove ordinary `dotnet test` without `LAMUFLIX_EVALUATION_MODE` runs no real-library mode |
| Nonsecret env names added | `LAMUFLIX_EVALUATION_SELECTED_ID`, `LAMUFLIX_EVALUATION_OTHER_ID` (compare/pair); `LAMUFLIX_EVALUATION_OUTPUT_PATH` defaults documented in §4 and §8 |

## 4. Compare and pair inspect (Q5 / PC2)

### Nonsecret inputs (pinned names)

| Env name | Required when | Validation / default |
|---|---|---|
| `LAMUFLIX_EVALUATION_MODE` | compare / pair inspect | must be `compare` |
| `LAMUFLIX_EVALUATION_SELECTED_ID` | compare / pair inspect | required positive integer; reject missing/non-positive |
| `LAMUFLIX_EVALUATION_OTHER_ID` | pair inspect only | when set: positive integer ≠ selected → print that directed pair only; when unset: whole-library compare for selected |
| `LAMUFLIX_EVALUATION_OUTPUT_PATH` | optional | default `artifacts/recommendations/compare-{selectedId}.json` or `artifacts/recommendations/pair-{selectedId}-{otherId}.json` |

IDs in copyable examples below are **synthetic** (`101`, `202`). Never invent home-library IDs here; Phase B substitutes looked-up seeds from §1.

### Whole-library compare

```powershell
if (-not (Test-Path Env:ConnectionStrings__DefaultConnection)) { throw 'ConnectionStrings__DefaultConnection missing' }

$priorMode = $env:LAMUFLIX_EVALUATION_MODE
$priorSelected = $env:LAMUFLIX_EVALUATION_SELECTED_ID
$priorOther = $env:LAMUFLIX_EVALUATION_OTHER_ID
$priorOutput = $env:LAMUFLIX_EVALUATION_OUTPUT_PATH
try {
  $env:LAMUFLIX_EVALUATION_MODE = 'compare'
  $env:LAMUFLIX_EVALUATION_SELECTED_ID = '101' # synthetic example ID — replace with looked-up home ID at Phase B
  Remove-Item Env:LAMUFLIX_EVALUATION_OTHER_ID -ErrorAction SilentlyContinue
  $env:LAMUFLIX_EVALUATION_OUTPUT_PATH = 'artifacts/recommendations/compare-101.json'
  dotnet run --project tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj -- -class LamuFlix.Evaluation.Entry.RealLibraryEntryTests -noLogo -noColor
}
finally {
  if ($null -eq $priorMode) { Remove-Item Env:LAMUFLIX_EVALUATION_MODE -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_MODE = $priorMode }
  if ($null -eq $priorSelected) { Remove-Item Env:LAMUFLIX_EVALUATION_SELECTED_ID -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_SELECTED_ID = $priorSelected }
  if ($null -eq $priorOther) { Remove-Item Env:LAMUFLIX_EVALUATION_OTHER_ID -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_OTHER_ID = $priorOther }
  if ($null -eq $priorOutput) { Remove-Item Env:LAMUFLIX_EVALUATION_OUTPUT_PATH -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_OUTPUT_PATH = $priorOutput }
}
```

### Pair inspect

```powershell
if (-not (Test-Path Env:ConnectionStrings__DefaultConnection)) { throw 'ConnectionStrings__DefaultConnection missing' }

$priorMode = $env:LAMUFLIX_EVALUATION_MODE
$priorSelected = $env:LAMUFLIX_EVALUATION_SELECTED_ID
$priorOther = $env:LAMUFLIX_EVALUATION_OTHER_ID
$priorOutput = $env:LAMUFLIX_EVALUATION_OUTPUT_PATH
try {
  $env:LAMUFLIX_EVALUATION_MODE = 'compare'
  $env:LAMUFLIX_EVALUATION_SELECTED_ID = '101' # synthetic example
  $env:LAMUFLIX_EVALUATION_OTHER_ID = '202'   # synthetic example; directed pair 101 → 202
  $env:LAMUFLIX_EVALUATION_OUTPUT_PATH = 'artifacts/recommendations/pair-101-202.json'
  dotnet run --project tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj -- -class LamuFlix.Evaluation.Entry.RealLibraryEntryTests -noLogo -noColor
}
finally {
  if ($null -eq $priorMode) { Remove-Item Env:LAMUFLIX_EVALUATION_MODE -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_MODE = $priorMode }
  if ($null -eq $priorSelected) { Remove-Item Env:LAMUFLIX_EVALUATION_SELECTED_ID -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_SELECTED_ID = $priorSelected }
  if ($null -eq $priorOther) { Remove-Item Env:LAMUFLIX_EVALUATION_OTHER_ID -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_OTHER_ID = $priorOther }
  if ($null -eq $priorOutput) { Remove-Item Env:LAMUFLIX_EVALUATION_OUTPUT_PATH -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_OUTPUT_PATH = $priorOutput }
}
```

| Seed title | Output path | Notes |
|---|---|---|
| A Fistful of Dollars | **Could not run** — blocked by T019a | |
| About Time | **Could not run** | Pirate Radio plot rank / pool: **Could not run** (acceptance remains open until evidenced) |
| A Few Good Men | **Could not run** | |
| Aladdin | **Could not run** | |
| The Wind Rises | **Could not run** | |

Pair inspect sample (optional operator note; record selected/other IDs used): **Could not run** — no home-library pair was inspected; no IDs invented.

Pool diagnostic disclosure (ticket formula; not production pool parity): unchanged design disclosure — pool flag `(plotRank is not null && plotRank <= CandidatePoolSize) || sameCollection` is a diagnostic, not production pool parity. No runtime measurement exists.

## 5. Quality (Q7)

### Nonsecret inputs (pinned names)

| Env name | Required when | Validation / default |
|---|---|---|
| `LAMUFLIX_EVALUATION_MODE` | quality | must be `quality` |
| `LAMUFLIX_EVALUATION_JUDGED_PAIRS_PATH` | optional | default `artifacts/recommendations/judged-pairs.json` (unversioned); reject malformed/duplicate directed entries |
| `LAMUFLIX_EVALUATION_BARS` | optional | comma-separated invariant decimals; default `0.15,0.20,0.225,0.25,0.30,0.35,0.40`; must parse as finite unique ascending positives |
| `LAMUFLIX_EVALUATION_STRONG_ONLY_BAR` | optional | invariant decimal; default `0.225`; must be finite positive |
| `LAMUFLIX_EVALUATION_OUTPUT_PATH` | optional | default `artifacts/recommendations/quality.json` |

### Operator invocation

```powershell
if (-not (Test-Path Env:ConnectionStrings__DefaultConnection)) { throw 'ConnectionStrings__DefaultConnection missing' }

$priorMode = $env:LAMUFLIX_EVALUATION_MODE
$priorJudged = $env:LAMUFLIX_EVALUATION_JUDGED_PAIRS_PATH
$priorBars = $env:LAMUFLIX_EVALUATION_BARS
$priorStrong = $env:LAMUFLIX_EVALUATION_STRONG_ONLY_BAR
$priorOutput = $env:LAMUFLIX_EVALUATION_OUTPUT_PATH
try {
  $env:LAMUFLIX_EVALUATION_MODE = 'quality'
  $env:LAMUFLIX_EVALUATION_JUDGED_PAIRS_PATH = 'artifacts/recommendations/judged-pairs.json'
  # optional overrides (omit to use defaults above):
  # $env:LAMUFLIX_EVALUATION_BARS = '0.15,0.20,0.225,0.25,0.30,0.35,0.40'
  # $env:LAMUFLIX_EVALUATION_STRONG_ONLY_BAR = '0.225'
  $env:LAMUFLIX_EVALUATION_OUTPUT_PATH = 'artifacts/recommendations/quality.json'
  dotnet run --project tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj -- -class LamuFlix.Evaluation.Entry.RealLibraryEntryTests -noLogo -noColor
}
finally {
  if ($null -eq $priorMode) { Remove-Item Env:LAMUFLIX_EVALUATION_MODE -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_MODE = $priorMode }
  if ($null -eq $priorJudged) { Remove-Item Env:LAMUFLIX_EVALUATION_JUDGED_PAIRS_PATH -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_JUDGED_PAIRS_PATH = $priorJudged }
  if ($null -eq $priorBars) { Remove-Item Env:LAMUFLIX_EVALUATION_BARS -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_BARS = $priorBars }
  if ($null -eq $priorStrong) { Remove-Item Env:LAMUFLIX_EVALUATION_STRONG_ONLY_BAR -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_STRONG_ONLY_BAR = $priorStrong }
  if ($null -eq $priorOutput) { Remove-Item Env:LAMUFLIX_EVALUATION_OUTPUT_PATH -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_OUTPUT_PATH = $priorOutput }
}
```

| Check | Result |
|---|---|
| Coverage partition sums verified | **Could not run** at runtime (blocked by T019a). Fixture proof stands: `QualityReportRunnerTests.Run_every_coverage_line_partitions_the_enriched_selected_population` |
| Band/strong-only samples stable across repeat (SHA-256 directed encoding) | Runtime repeat **Could not run**; fixture/golden proof stands (`BandSampleSelectorTests` golden vectors, `QualityReportRunnerTests.Run_repeats_identical_band_samples_on_unchanged_data`) |
| Judged-pairs path (unversioned) | Interface complete; no owner-corrected home judged-pairs file exists in this worktree (`artifacts/recommendations/` is empty), so no home judgment was run or invented |
| Known-miss good pair retained unless owner changed | **Unresolved at runtime** — Phase B owner input (The Naked Gun → Hot Shots!) was never supplied here; nothing was written into the repo by the harness |
| Skipped IDs listed | Fixture proof stands: `QualityReportRunnerTests.Run_skips_unknown_and_unenriched_ids_visibly_and_excludes_them_from_denominators` |
| Output discloses LOO/full-take harness-relative metric validity where applicable | Design disclosure stands; no runtime output produced |

### Phase 7 (T016–T018) quality receipts

| Check | Value |
|---|---|
| `tests/LamuFlix.Evaluation/RealLibrary/QualityReportRunner.cs` | coverage table (score-only + score-or-strong per bar), four-bucket partition, ordinal first/tenth medians with counts and nulls, band + strong-only samples, judged-pair evaluation |
| `tests/LamuFlix.Evaluation/RealLibrary/BandSampleSelector.cs` | `[lower,upper)` bands and strong-only band; SHA-256 over UTF-8 invariant-decimal `selectedId:otherId`; digest-byte order then ascending ids; up to 12 per band; undersupply disclosed |
| `tests/LamuFlix.Evaluation/RealLibrary/JudgedPairReader.cs` | directed `movieId`/`otherId`/`verdict`/`note?` JSON under ignored `artifacts/recommendations/`; malformed, missing-field, unknown-verdict, self-pair and duplicate directed entries rejected without partial success |
| `tests/LamuFlix.Evaluation/RealLibrary/QualityReportSerializer.cs` | canonical compact UTF-8, `.partial` then atomic move, invariant finite numerics, explicit nulls, no timestamp/host/elapsed |
| Strong criteria | `["Collection", "Director"]` only; Themes never synthesized before DEV-518; identity-fallback (`Value` 1.0, `Weight` 0.0) excluded because the strong list names only real criteria |
| Mode wiring | `LAMUFLIX_EVALUATION_MODE=quality`; optional `LAMUFLIX_EVALUATION_BARS`, `LAMUFLIX_EVALUATION_STRONG_ONLY_BAR`, `LAMUFLIX_EVALUATION_JUDGED_PAIRS_PATH`, `LAMUFLIX_EVALUATION_OUTPUT_PATH`; inert without the marker; presence-only credential |
| Golden fixture | `RealLibrary/Fixtures/band-sample-hash.golden.json` (seven vectors, two direction-reversal pairs, digest order `3:4, 1:2, 34:12, 2:1, 12:34, 100:250, 7:9`) |
| `dotnet test tests/LamuFlix.Evaluation --filter "FullyQualifiedName~QualityReportRunnerTests\|FullyQualifiedName~BandSampleSelectorTests"` | 55 passed, 0 failed |
| `dotnet test tests/LamuFlix.Evaluation` (full project) | 203 passed, 0 failed (synthetic publish unchanged) |
| Home-library judgments/measurements | **open** — Phase B (T020); no home IDs, verdicts or numbers invented here |

## 6. Scrub (T021)

| Check | Result |
|---|---|
| `git status` excludes outputs/judgments | PASS — `git status --porcelain --untracked-files=all` shows only the three intended files (`scripts/run-recommendation-snapshot.ps1`, `tests/.../RecommendationSnapshotCommandTests.cs`, `specs/DEV-545/EVIDENCE.md`); `artifacts/` is ignored by `.gitignore:66`, so no output or judged-pairs file can be committed |
| Repo search: no committed connection string from this work | PASS — no connection string was read, echoed, or written at any point in this phase. `ConnectionStrings__DefaultConnection` was checked presence-only and was **absent** in this shell (process, user and machine scopes), which is itself why the real-library modes could not run |
| Evidence.md / provenance / notes / draft artifacts scrubbed of secrets and echoed credentials | PASS — a secret-pattern scan of this file (password, host, URI and API-key assignment patterns) returns no matches; the only credential-adjacent text is the standing presence-only rule |
| No shell-history assignment examples for `ConnectionStrings__DefaultConnection` retained in drafts | PASS — no assignment example was added; §3/§4/§5/§8 keep the pre-existing presence-only pattern |

## 7. Publication (Rigger / T024)

Re-run T019a manifest recheck before attaching. Include LOO harness-relative HR/MRR validity disclaimer in the comment.

| Artifact | Attached to DEV-545 | Verified by |
|---|---|---|
| Baseline snapshot | _pending_ | Rigger |
| Leave-one-out result | _pending_ | Rigger |
| Five compare outputs | _pending_ | Rigger |
| Quality report | _pending_ | Rigger |
| Provenance + LOO validity disclaimer (never credentials) | _pending_ | Rigger |

## 8. Real-library mode command summary (pinned nonsecret interface)

Credential transport for all real-library modes: pre-provisioned process environment variable `ConnectionStrings__DefaultConnection` only. Presence-only checks. Never assign, echo, or pass as CLI arguments. No plaintext secret in command examples or shell history.

| Mode | Marker | Nonsecret inputs | Output default | Notes |
|---|---|---|---|---|
| leave-one-out | `LAMUFLIX_EVALUATION_MODE=leave-one-out` | `LAMUFLIX_EVALUATION_OUTPUT_PATH` (optional) | `artifacts/recommendations/leave-one-out.json` | Inert without marker; restore/remove markers after run |
| compare (whole library) | `LAMUFLIX_EVALUATION_MODE=compare` | required `LAMUFLIX_EVALUATION_SELECTED_ID` (positive int); `OTHER_ID` unset; optional `OUTPUT_PATH` | `artifacts/recommendations/compare-{selectedId}.json` | Synthetic example IDs only in docs |
| pair inspect | `LAMUFLIX_EVALUATION_MODE=compare` | required `SELECTED_ID` + `OTHER_ID` (positive ints, distinct) | `artifacts/recommendations/pair-{selectedId}-{otherId}.json` | Same mode marker; OTHER_ID selects pair-only path |
| quality | `LAMUFLIX_EVALUATION_MODE=quality` | optional `JUDGED_PAIRS_PATH`, `BARS`, `STRONG_ONLY_BAR`, `OUTPUT_PATH` | `artifacts/recommendations/quality.json`; bars default `0.15,0.20,0.225,0.25,0.30,0.35,0.40`; strong-only default `0.225` | Judged-pairs path unversioned |
| Capture/Diff | `pwsh -NoProfile -File scripts/run-recommendation-snapshot.ps1 ...` | script params (nonsecret) | under `artifacts/recommendations/` | IntegrationTests adapt `EvaluationCommand` for script launch; N/A connection string |

### Phase 6 (T013–T015) snapshot and diff receipts

| Check | Value |
|---|---|
| `scripts/run-recommendation-snapshot.ps1` modes | `Capture` (default) and `Diff`; PowerShell 7; no new package |
| Capture defaults | `-BaseUrl http://localhost:3100`; `-OutputPath artifacts/recommendations/capture.json`; For you `take=30`, each Related `take=10` |
| Seed contract | `-SeedIds` accepts five ordered positive ids (comma or space separated); validated before any HTTP request; duplicates, non-integers, non-positive and wrong counts rejected |
| Seed titles in help | A Fistful of Dollars, About Time, A Few Good Men, Aladdin, The Wind Rises |
| Literal default ids | **done in T019** — `575, 518, 515, 519, 831` are the `-SeedIds` defaults and are printed in `-Help`; see §1 |
| Envelope conformance | same field set and order as `RecommendationArtifactSerializer` (`mode`, `targets`, `lists[kind,targetId,take,status,rows[rank,movieId,title,score,reasons[kind,text]]]`); no `schemaVersion`, timestamp, host or elapsed |
| Publication | canonical compact UTF-8, no BOM, no terminal newline, invariant finite numerics, `.partial` write then atomic move; nothing is published on failure |
| Empty Related | recorded `status: "empty"` with zero rows, not an error |
| Diff groups | always prints `moved up`, `moved down`, `entered`, `left`, `reasons changed` plus a summary line, even when all are empty |
| Diff compatibility | same-mode only (capture/capture or compare/compare), identical targets, matching list kinds/targets/takes; mixed modes, malformed JSON, schema violations and duplicate ids rejected |
| `dotnet test tests/LamuFlix.Evaluation.IntegrationTests --filter RecommendationSnapshotCommandTests` | 34 passed, 0 failed (byte-identical repeated captures; exact API order; explicit takes; seed validation without HTTP; empty Related; HTTP/JSON/schema/duplicate rejection with no artifact; five Diff groups; cross-mode rejection; compare/compare acceptance; pt-BR adversarial finite numerics) |
| `dotnet test tests/LamuFlix.Evaluation.IntegrationTests` (full project) | 66 passed, 0 failed |
| `dotnet test tests/LamuFlix.Evaluation` | 146 passed, 0 failed (synthetic publish unchanged) |
| `dotnet test tests/LamuFlix.UnitTests` | 4032 passed, 1 skipped, 0 failed |
| `pwsh -NoProfile -File ./scripts/run-evaluation.ps1` | exit 0; summary byte-identical to the T002 receipt above |
| `./scripts/run-roslyn-analyzers.ps1` | exit 0, passed |
| `./scripts/run-cyclomatic-complexity.ps1` | exit 0, passed (threshold 15) |
| `./scripts/run-jetbrains-inspectcode.ps1` | exit 0, passed (no WARNING+) |
| `dotnet format --verify-no-changes` | exit 0 |
| Production `src/` seam | none — script plus `tests/` only |
| Test-only `Process.Start` | `EvaluationCommand.RunSnapshot` launches the script under `LamuFlix.Evaluation.IntegrationTests`; the existing `run-evaluation.ps1` path is preserved |
| New dependency | `WireMock.Net` PackageReference on `LamuFlix.Evaluation.IntegrationTests.csproj` using the existing central pin; no version change |
| Runtime acceptance | Phase B — `artifacts/recommendations/` captures, five Diff groups on real data and literal seed ids: literal ids are now done (T019); the real-data capture/Diff acceptance remains open because T019a stopped before T020 |

### Phase 8 (T019–T021) receipts

| Check | Value |
|---|---|
| T019 seed lookup | §1 — five ids `575, 518, 515, 519, 831`, each verified by `GET /api/movies/{id}`; fixture id 90 absent |
| T019 script change | `[string[]]$SeedIds = @('575', '518', '515', '519', '831')`; `.PARAMETER SeedIds` and `-Help` render the five title→id pairs from `$seedTitles`/`$seedDefaultIds`; validation unchanged |
| T019 test | New `Capture_DefaultsToTheLookedUpHomeSeedIdsWithoutAnyExplicitSeeds` proves a parameter-free Capture targets exactly those five ids and sends `take=10` to each Related path; `Help_DocumentsTheCaptureAndDiffSurface` now also asserts `575` and `831` appear |
| T019 live proof | Parameter-free Capture exit 0 twice; SHA-256 `AD878F612789EEAF7EEA4B23CCBDFDA84860DA2E51105712B500B5ACF2A7F820` both times; 30 For-you rows, 10 rows per Related |
| T019a identity gate | **STOP — mismatch.** Deployed API `b98a4b0a` vs `origin/main` `8a2deb9b` (99 commits behind) with recommendation handler/profile/calculator differences; Argo home revision `bd0a83fe` off-main. Full manifest in §0 |
| T020 | **Not started** — T019a did not pass. §2 and §3 runtime slots recorded `Could not run` |
| T021 | Split: scrub half PASS in §6; DB after-state half **Could not run** because no T020 run produced a preserved before-state |
| `dotnet test tests/LamuFlix.Evaluation.IntegrationTests` (full) | 67 passed, 0 failed (2 m 24 s) |
| `dotnet test tests/LamuFlix.Evaluation.IntegrationTests --filter RecommendationSnapshotCommandTests` | 35 passed, 0 failed |
| `dotnet test tests/LamuFlix.Evaluation` | 203 passed, 0 failed |
| `dotnet test tests/LamuFlix.UnitTests --filter "FullyQualifiedName~Features.Recommendations"` | 635 passed, 0 failed |
| `pwsh -NoProfile -File ./scripts/run-evaluation.ps1` | exit 0; summary byte-identical to the T002 receipt |
| `./scripts/run-roslyn-analyzers.ps1` | exit 0, passed (33 target files) |
| `./scripts/run-cyclomatic-complexity.ps1` | exit 0, passed (threshold 15) |
| `./scripts/run-jetbrains-inspectcode.ps1` | exit 0, passed (no WARNING+) |
| `dotnet format --verify-no-changes` | exit 0 |
| `baseline.json` and `scripts/run-evaluation.ps1` vs `origin/main` | unchanged |
| Production `src/` seam in this phase | none — this phase changed a script, a test and evidence only |
