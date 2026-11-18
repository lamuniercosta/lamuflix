# DEV-545 evidence runbook

Status: Phase A slots only — plan challenge repair round 2 applied (complete PC3 items 5–6 residual wording). Fill during Phase B. Do not invent measurements, seed IDs, or publication success here. Never credentials.

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
| Current `origin/main` SHA | _pending_ |
| Harness SHA (`HEAD`) | _pending_ |
| Harness dirty status | _pending_ |
| Relevant production source diff vs main (summary / receipt path) | _pending_ |
| API running build/config identity | _pending_ |
| Effective nonsecret RecommendationOptions | _pending_ |
| Production scoring equivalence judgment + invariance test receipts | _pending_ |
| Drift reconciliation note (or "none") | _pending_ |
| Manifest recheck before T020 | _pending_ |
| Manifest recheck before publication | _pending_ |
| Cohort counts (liked, watched, overlap) | _pending_ |

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



## 1. Seed ID lookup (Q2)

Ordered titles → home-library integer IDs (one-time lookup). Never use fixture About Time ID 90.

| Order | Title | Home movie ID |
|---|---|---|
| 1 | A Fistful of Dollars | _pending_ |
| 2 | About Time | _pending_ |
| 3 | A Few Good Men | _pending_ |
| 4 | Aladdin | _pending_ |
| 5 | The Wind Rises | _pending_ |

Literal defaults/help populated in `scripts/run-recommendation-snapshot.ps1`: _pending_

## 2. Snapshot determinism (Q6) — paired quiescent recommendation-input fingerprint

**Precondition**: no scan/enrichment job and no user mutation during the Capture×2 window and the DB proof window that follows. No new production job controls.

**Paired slots (required)**: record a recommendation-input data fingerprint/count **before** Capture A and a matching **after** observation after Capture B (same window that also covers the later DB before/after proof). Include library/recommendation row counts plus stable pending/enrichment-state counts (or equivalent stable fingerprints of those inputs). Require explicit before==after equality. On mismatch: **discard** that Capture×2 attempt (do not interpret capture hashes), restore quiescence, and **retry** the window. Do not treat every mismatch as a script defect.

| Check | Result |
|---|---|
| Quiescence precondition verified (no scan/enrichment/user mutation) | _pending_ |
| **Before** recommendation-input fingerprint/counts (row counts + pending/enrichment state) | _pending_ |
| Capture A path + hash | _pending_ |
| Capture B path + hash (unchanged data) | _pending_ |
| Capture hashes identical | _pending_ |
| **After** recommendation-input fingerprint/counts (same fields as before) | _pending_ |
| Before==after fingerprint/counts equality | _pending_ |
| Mismatch → discarded/retried before hash interpretation (or "n/a — equal") | _pending_ |
| Diff five groups observed (same-mode operands) | _pending_ |

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
  dotnet run --project tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj --no-build
}
finally {
  if ($null -eq $priorMode) { Remove-Item Env:LAMUFLIX_EVALUATION_MODE -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_MODE = $priorMode }
  if ($null -eq $priorOutput) { Remove-Item Env:LAMUFLIX_EVALUATION_OUTPUT_PATH -ErrorAction SilentlyContinue } else { $env:LAMUFLIX_EVALUATION_OUTPUT_PATH = $priorOutput }
}
```

Inertness check: ordinary `dotnet test` **without** `LAMUFLIX_EVALUATION_MODE=leave-one-out|compare|quality` must not execute real-library modes (covered by `EvaluationEntryModeTests`). Mode and nonsecret markers MUST be restored/removed so copyable examples do not leave real-library mode enabled.

| Check | Result |
|---|---|
| Liked HR@1/3/10, MRR, seed count | _pending_ |
| Watched HR@1/3/10, MRR, seed count | _pending_ |
| Overlap disclosed | _pending_ |
| Bypass disclosure present (endpoint/validator + full-eligible take) | _pending_ |
| Harness-relative HR/MRR validity disclaimer present | _pending_ |
| **Before-state** row counts (captured and preserved **before** leave-one-out begins — T020) | _pending_ |
| **Before-state** every movie review value (same timing — T020) | _pending_ |
| **After-state** row counts (captured **immediately after** the same leave-one-out run — T021) | _pending_ |
| **After-state** every movie review value (same timing — T021) | _pending_ |
| Before vs after comparison: row counts and every review value unchanged (SC-003 ticket-required boundary) | _pending_ |
| Explicit disclosure: counts+reviews do **not** prove every untested column or transient write | _pending_ |
| No save/add/update/remove in new LOO path (reviewer read of complete loaded path) | _pending_ |
| Testcontainers no-write proof receipt (`RealLibraryReadOnlyLoaderTests`) | _pending_ |

Note: Evidence acquired only after the run cannot prove non-mutation during that run. Keep raw personal before/after dumps unversioned. This is bounded ticket-required behavioral evidence, not a waiver of the strict no-write contract.

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
  dotnet run --project tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj --no-build
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
  dotnet run --project tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj --no-build
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
| A Fistful of Dollars | _pending_ | |
| About Time | _pending_ | Pirate Radio plot rank / pool: _pending_ (expect rank > 50, outside pool; acceptance remains open until evidenced) |
| A Few Good Men | _pending_ | |
| Aladdin | _pending_ | |
| The Wind Rises | _pending_ | |

Pair inspect sample (optional operator note; record selected/other IDs used): _pending_

Pool diagnostic disclosure (ticket formula; not production pool parity): _pending_

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
  dotnet run --project tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj --no-build
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
| Coverage partition sums verified | _pending_ |
| Band/strong-only samples stable across repeat (SHA-256 directed encoding) | _pending_ |
| Judged-pairs path (unversioned) | _pending_ |
| Known-miss good pair retained unless owner changed | _pending_ |
| Skipped IDs listed | _pending_ |
| Output discloses LOO/full-take harness-relative metric validity where applicable | _pending_ |

## 6. Scrub (T021)

| Check | Result |
|---|---|
| `git status` excludes outputs/judgments | _pending_ |
| Repo search: no committed connection string from this work | _pending_ |
| Evidence.md / provenance / notes / draft artifacts scrubbed of secrets and echoed credentials | _pending_ |
| No shell-history assignment examples for `ConnectionStrings__DefaultConnection` retained in drafts | _pending_ |

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
