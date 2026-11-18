# DEV-545 evidence runbook

Status: Option A run executed on 2026-11-18. Home redeployed to main `8a2deb9b`; T019a identity gate PASS; T020 runtime PASS (fingerprint and capture hashes equal, DB before==after); T021 PASS. Rows that were `Could not run` in the earlier stopped attempt are replaced with measured values. Raw outputs stay unversioned in `artifacts/recommendations/` (git-ignored). Dated sections: §0 (Option A manifest) and §9.

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
| Manifest recheck before T020 | **PASS** (2026-11-18). Immediately before the capture window: `origin/main` = `8a2deb9b`; api pod image digest `sha256:9f2cffbf...` (revision label `8a2deb9b`); Argo `lamuflix-home` Synced/Healthy at `cd8de7d4`; harness HEAD `494c08e2`, worktree clean. See the Option A manifest below |
| Manifest recheck before publication | Performed as the first step of T024 (re-run `git fetch origin main`, pod digest, Argo revision) |
| Cohort counts (liked, watched, overlap) | liked 4, watched 27, overlap 4 (LOO JSON `overlap`) |

#### Option A re-run manifest (2026-11-18; the T019a PASS receipt)

| Field | Value |
|---|---|
| Promoted main SHA | `8a2deb9b7a971b50108721aa8465620e551f1cfa`. Dry run (promote-home run 44385356743) resolved the requested sha with no fallback. Real run 44385558517 (`dry_run=false`) pushed `env/home` as `cd8de7d4971ac61d2a7b22958413defda49fb25c` (`home: promote 8a2deb9b`). Images `sha-8a2deb9b` were published by push CI run 44289481302 |
| Served build | Deployment `api` in namespace `lamuflix-home`, pod `api-d9d786c9-f2pzx` Ready, image `ghcr.io/lamuniercosta/lamuflix-api@sha256:9f2cffbfd3394dd35849bde9f93bc64d8e01b5f6679fac4bbae68672e4f07924`. OCI label `org.opencontainers.image.revision` on that digest = `8a2deb9b7a971b50108721aa8465620e551f1cfa` (read with `docker buildx imagetools inspect`). Argo `lamuflix-home` Synced/Healthy at `cd8de7d4` |
| Harness | HEAD `494c08e24829fb7a88c7d49e7fe22370a762b67b`, worktree clean. `git merge-base HEAD origin/main` = `8a2deb9b`, so the branch already contains the promoted main |
| Branch vs promoted build | `git diff --name-status 8a2deb9b HEAD -- src/` lists only `M` `SimilarityResult.cs` and `M` `SimilarityScorer.cs`. SHA-256 of `RecommendationOptions.cs`, `EfRecommendationProfileSource.cs` and `EfRecommendationCandidateSource.cs` is identical at `8a2deb9b` and HEAD. Bounded read of the scorer diff: criterion names, `Breakdown` population and the identity-fallback breakdown were added; `ComputeScore`, eligibility and weight arithmetic are unchanged. Invariance receipt: `dotnet test tests/LamuFlix.UnitTests --filter "FullyQualifiedName~Features.Recommendations"` = 635 passed, 0 failed at HEAD `494c08e2` |
| Effective RecommendationOptions | Code defaults bind. The pod environment has no `Recommendations` setting (env names only: `Ollama__Endpoint`, `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_SERVICE_NAME`, `Tmdb__Enabled`, `Tmdb__ApiKey`). The secret `api.settings.json` top-level keys contain no `Recommendations`, and no `appsettings*.json` or `deploy/**` file at `8a2deb9b` sets one. Defaults: Take 10, CandidatePoolSize 50, MmrLambda 0.7 (others as in the stopped manifest above). Harness takes: For you 30, each Related 10 |
| Credentials | None printed, logged or written. The connection string was built inside the run-script process only; checks were presence-only |
| Recheck before T020 | PASS (values above) |

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
| Quiescence precondition verified (no scan/enrichment/user mutation) | **PASS**. `outbox_messages` with `processed_at IS NULL` = 0; `movies` status counts = `1` for all 449 rows; api logs for the prior 30 minutes contain no scan or enrichment template, only one startup library-gate line (no writes needed) |
| **Before** recommendation-input fingerprint/counts (row counts + pending/enrichment state) | **PASS, captured before Capture A.** 18 aggregate rows: per-table `count(*)` plus `md5` over row text for movies, movie_features, movie_actors/directors/writers/genres/keywords, movie_tmdb_cast, actors, directors, writers, genres, keywords, collections; outbox unprocessed count; movie status counts. Sorted-output SHA-256 `ef86262fd5979769cad041e0ae4b7dd031ada4ee7a9820e8a864a76474e840e6`. Raw output unversioned |
| Capture A path + hash | `artifacts/recommendations/baseline-capture-a.json` (37783 bytes), SHA-256 `e4a4f4b6ac333a943dcbdcd87c2b360288ecbf7d0eceb7e4355fdd8216225d04` |
| Capture B path + hash (unchanged data) | `artifacts/recommendations/baseline-capture-b.json` (37783 bytes), SHA-256 `e4a4f4b6ac333a943dcbdcd87c2b360288ecbf7d0eceb7e4355fdd8216225d04` |
| Capture hashes identical | **PASS** (compared only after the fingerprint equality check) |
| **After** recommendation-input fingerprint/counts (same fields as before) | **Captured after Capture B**, same query. Sorted-output SHA-256 `ef86262fd5979769cad041e0ae4b7dd031ada4ee7a9820e8a864a76474e840e6` (same as before) |
| Before==after fingerprint/counts equality | **PASS** on the first attempt; no retry needed |
| Mismatch → discarded/retried before hash interpretation (or "n/a — equal") | N/A — equal |
| Diff five groups observed (same-mode operands) | **PASS**: moved up 0, moved down 0, entered 0, left 0, reasons changed 0; summary line reports 0 in all groups |

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
| Liked HR@1/3/10, MRR, seed count | HR@1 0, HR@3 0, HR@10 0, MRR 0; **4 seeds** attempted (515, 518, 519, 525) |
| Watched HR@1/3/10, MRR, seed count | HR@1 0, HR@3 0.0741 (2/27), HR@10 0.1481 (4/27), MRR 0.0537; **27 seeds** attempted |
| Overlap disclosed | liked 4, watched 27, overlap 4 |
| Bypass disclosure present (endpoint/validator + full-eligible take) | Present in code/report design (Phase 5 receipts above); no runtime report was produced |
| Harness-relative HR/MRR validity disclaimer present | Present in report design; no runtime report was produced |
| **Before-state** row counts (captured and preserved **before** leave-one-out begins — T020) | **PASS**. Full before-state (per-table counts for 15 tables plus 449 review lines) preserved at `artifacts/recommendations/db-before-state.txt`, SHA-256 `390711968a2666aeb45f06865e694815555e3c700ef490bd046206d83e884f1e`; movies count 449 |
| **Before-state** every movie review value (same timing — T020) | **PASS**: 449 `review|id|value` lines in the same preserved file, captured before LOO |
| **After-state** row counts (captured **immediately after** the same leave-one-out run — T021) | **PASS**: identical to before (captured right after the LOO run exited 0) |
| **After-state** every movie review value (same timing — T021) | **PASS**: identical to before, same whole-file comparison |
| Before vs after comparison: row counts and every review value unchanged (SC-003 ticket-required boundary) | **PASS**: byte-identical after LOO, and again after all compare, pair and quality runs. Boundary disclosure: counts and review values do not prove every column or transient write. The no-write proof remains the loader Testcontainers receipt plus code-path inspection |
| Explicit disclosure: counts+reviews do **not** prove every untested column or transient write | Disclosed as a standing limitation of this evidence type; still applies whenever the run happens |
| No save/add/update/remove in new LOO path (reviewer read of complete loaded path) | Static code-path evidence stands from Phase 5; runtime confirmation is recorded in §3 Option A rows (DB before==after) |
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
| A Fistful of Dollars | `artifacts/recommendations/compare-575.json` published, exit 0 (not summarized row by row) | |
| About Time | `artifacts/recommendations/compare-518.json` published, exit 0 | Pirate Radio (759) in `pair-518-759.json`: plotRank **192**, `inPool` **false**, so rank > 50 and outside pool: **PASS** |
| A Few Good Men | `artifacts/recommendations/compare-515.json` published, exit 0 | |
| Aladdin | `artifacts/recommendations/compare-519.json` published, exit 0 | |
| The Wind Rises | `artifacts/recommendations/compare-831.json` published, exit 0 | |

Pair inspect sample: `artifacts/recommendations/pair-518-759.json` (selected 518 About Time, other 759 Pirate Radio; directed pair), exit 0.

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
| Coverage partition sums verified | **PASS**: `enrichedSelectedCount` 449; all 14 coverage lines in `quality.json` have bucket sums equal to 449 |
| Band/strong-only samples stable across repeat (SHA-256 directed encoding) | Not repeated at runtime (one quality run). Recorded output: seven bands; six have 12 pairs; the strong-only band is undersupplied with 0 available. Fixture proof stands |
| Judged-pairs path (unversioned) | `artifacts/recommendations/judged-pairs.json` (git-ignored; SHA-256 `a765b99dc7ba5245f346fe3575c5fc667fb509889a1f84cc2a0023ea47544e46`), 15 directed entries, owner ticket starting list used as-is; 0 skipped |
| Known-miss good pair retained unless owner changed | **Retained, not tuned**: The Naked Gun → Hot Shots! is in the judged set; result rank 36, score 0.1917, not in top three (`quality.json` `judgedPairs.valid`) |
| Skipped IDs listed | `skipped` is empty in `quality.json` |
| Output discloses LOO/full-take harness-relative metric validity where applicable | LOO output carries the bypass and harness-relative disclosures. `quality.json` has six disclosures and does not contain the harness-relative phrase; noted as a gap, not changed here |

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
| T020 | Historical stop record (superseded by §9): originally not started because T019a did not pass; the Option A run completed |
| T021 | Historical stop record (superseded by §9): scrub half PASS in §6; DB after-state half later PASS in §9 |
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


## 9. Option A run receipts (2026-11-18)

Executed under the owner decision "Option A" for DEV-545 in this worktree only. Raw outputs are under `artifacts/recommendations/` (git-ignored) and are not committed. The connection string existed only inside the run-script process.

| Step | Result |
|---|---|
| Home redeploy | promote-home dry run 44385356743 then real run 44385558517, both success. `env/home` `bd0a83fe` → `cd8de7d4`. Argo Synced/Healthy; api rolled out Ready |
| Pre-window identity recheck | PASS (see §0 Option A manifest) |
| Quiescence and fingerprint before, DB before-state | captured before Capture A |
| Capture A, Capture B | published; SHA-256 identical |
| Fingerprint after (after Capture B) | equal to before |
| Diff | five groups all 0 |
| Leave-one-out | exit 0 (output path note below) |
| DB after-state, immediately after LOO | equal to before |
| Compare for 575, 518, 515, 519, 831 | exit 0 each |
| Pair inspect 518 → 759 | exit 0; plotRank 192; inPool false |
| Quality | exit 0; judged pairs 10 good, 5 bad, 0 skipped |
| DB after-state after all runs | equal to before |

### Headline metrics (harness-relative; compare only same-protocol deltas)

- Liked cohort: 4 seeds; HR@1 0, HR@3 0, HR@10 0, MRR 0.
- Watched cohort: 27 seeds; HR@1 0, HR@3 0.0741 (2/27), HR@10 0.1481 (4/27), MRR 0.0537.
- Overlap: all 4 liked seeds are also watched.
- Judged pairs: good 10 valid (rank-one share 0.2, top-three share 0.3, strong share 0.2); bad 5 valid (top-three share 0, strong share 0).

### Notes and deviations (disclosed, no code changed)

- Leave-one-out output path: the first LOO run resolved the relative `LAMUFLIX_EVALUATION_OUTPUT_PATH` against the test binary directory, so the file was written to `tests/LamuFlix.Evaluation/bin/Debug/net10.0/artifacts/recommendations/leave-one-out.json` (54522 bytes, SHA-256 `5a3e4229a248211c68a31f8a8aeff806d7457c455caa9b52541fb7eba378694a`). It was copied byte-identical to `artifacts/recommendations/leave-one-out.json`. All later runs used absolute paths. Relative output paths are not reliable as pinned; recorded as a finding for follow-up.
- Compare, pair and quality attempts failed with exit 1 because of an error in the operator run script path string. Those attempts produced no harness output. The script was corrected and the runs repeated. No harness file was changed.
- The T019 captures on the superseded build were 37781 bytes (SHA-256 `AD878F…`). The captures on `8a2deb9b` are 37783 bytes (SHA-256 `e4a4f4b6…`). Not investigated further; these T019 captures are not T020 evidence.
- The pinned leave-one-out, compare and quality commands were used as written, with absolute output and judged-pairs paths substituted.

### Scrub (T021 after-state)

- Pattern scan over `artifacts/recommendations/*` for `Password=`, `ConnectionStrings`, `Host=127`, `Username=`, `POSTGRES_PASSWORD` and `postgres://`: no matches.
- `git status --porcelain --untracked-files=all` after all runs: empty. `artifacts/` is ignored (`.gitignore:66`).

### Publication (T024)

Performed after this commit. The YouTrack comment and its attachments are the record of publication; the comment id is reported in the run summary.
