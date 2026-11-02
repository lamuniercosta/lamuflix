# DEV-358 — Paired DEV-356 evaluation measurement receipt

Tracking: DEV-358 (US4b / Package 7 / T028). Protocol: Q10 (CONCLUSIONS.md, "Q10 — Paired replay
measurement protocol and acceptance bar"); FR-010; SC-002; R6/R8; Q11 step 7.

Measurement HEAD: `8f7973ebd53e740a4f38e0cd205a84a0b284135f` (Phase 6 build, worktree
`F:/Dev/LamuFlix.worktrees/DEV-358`, branch `feature/DEV-358`).
Run completed: 2026-11-02T08:35:22Z. Evidence is from the pinned commands below, not from spec
drafting.

## 1. Exact pinned commands

Both runs use the same pinned enriched fixture, seed 356, observation clocks/order, eligible
cohort, cutoff 10 and lambda. The only differences are the output path and the TMDb signal mode.

```powershell
pwsh -NoProfile -File ./scripts/run-evaluation.ps1 -FixturePath ./tests/LamuFlix.Evaluation/Fixtures/baseline.tmdb.json -OutputPath ./artifacts/evaluation/DEV-358/baseline.json -Seed 356 -TmdbSignals Disabled
pwsh -NoProfile -File ./scripts/run-evaluation.ps1 -FixturePath ./tests/LamuFlix.Evaluation/Fixtures/baseline.tmdb.json -OutputPath ./artifacts/evaluation/DEV-358/tmdb.json -Seed 356 -TmdbSignals Enabled
```

Both commands completed with exit code 0, built `LamuFlix.Evaluation`, selected exactly one
evaluation entry (`LamuFlix.Evaluation.Entry.EvaluationEntryTests`) and published a non-empty
summary at the requested path. No live TMDb request is made by either run: replay consumes only
the pinned offline fixture and makes zero network calls.

## 2. Inputs and hashes (SHA-256)

| Artefact | Path | SHA-256 |
| --- | --- | --- |
| Enriched fixture (both runs) | `tests/LamuFlix.Evaluation/Fixtures/baseline.tmdb.json` | `d5549ae58f7799b67004dd90d37333d0ea72a36e20e9746bc36cd08256f0050f` |
| Scrubbed provenance sources | `tests/LamuFlix.Evaluation/Fixtures/baseline.tmdb.sources.json` | `a85c74d760a97b5f61d9b8dcfa9d3c606d6617a97318a10fa9c733906edf41cc` |
| Original DEV-356 fixture (untouched) | `tests/LamuFlix.Evaluation/Fixtures/baseline.json` | `14872f98c15607135464b560b339e076da605a927d5db05617cb23ba997b2533` |
| Original expected metrics (untouched) | `tests/LamuFlix.Evaluation/Fixtures/baseline.expected.json` | `44d2c9888fff748ffce31a87e3a507eff24b36a6a929524c2827f24cafb4d654` |
| Raw Disabled summary | `artifacts/evaluation/DEV-358/baseline.json` | `deb4fb32b3cfc46473984f7f73e5376f3558660b5e831807481b151a2a52dca3` |
| Raw Enabled summary | `artifacts/evaluation/DEV-358/tmdb.json` | `deb4fb32b3cfc46473984f7f73e5376f3558660b5e831807481b151a2a52dca3` |

`artifacts/` is git-ignored (`.gitignore:66`); the two raw summaries are never force-added and
exist only as local run output.

## 3. ranking inputs (identical for both runs)

- lambda = 0.5
- cutoff = 10
- weights: director 3.0, writers 2.0, cast 2.0, genres 3.0, yearProximity 1.0,
  runtimeProximity 0.5, ratingBoost 0.5, **keyword 1.0**, **collection 2.0**
- seed = 356 for both modes; fixture `fixtureId` = `dev-356-baseline`

The enriched fixture retains the exact original DEV-356 movie IDs 1–12, the exact original
events `e01`–`e12` (users, movie IDs, timestamps), the original catalog genres, and the original
ranking fields; it only adds the signal fields plus the explicit `keyword`/`collection` weights.

## 4. Paired results

| Metric | Disabled (`baseline.json`) | Enabled (`tmdb.json`) | Delta (Enabled − Disabled) |
| --- | --- | --- | --- |
| Mode | Disabled | Enabled | — |
| Seed | 356 | 356 | — |
| Eligible cohort (fixture) | `dev-356-baseline`, 12 movies | `dev-356-baseline`, 12 movies | identical |
| Observation count / denominator | 12 (nonzero) | 12 (nonzero) | 0 |
| Hit count | 9 | 9 | 0 |
| Hit Rate@10 | 0.75 | 0.75 | 0.0 |
| Reciprocal rank sum | 2.7484126984126984 | 2.7484126984126984 | 0.0 |
| MRR | 0.22903439153439153 | 0.22903439153439153 | 0.0 |

MRR is reported for both runs and is not an added no-regression gate (Q10).

## 5. Acceptance evidence

1. **Identical eligible observations and identical nonzero denominator.** Both runs replay the
   same pinned fixture and seed; raw summaries report `observationCount` = 12 (nonzero) with the
   same `fixtureId` and `cutoff`, and the paired raw files are byte-identical
   (`deb4fb32…`). `TmdbSignalReplayTests.Run_EnabledMode_KeepsTheIdenticalNonzeroDenominatorAndDoesNotLoseHits`
   asserts the same ordered event IDs in both modes.
2. **Disabled reproduces the existing DEV-356 baseline.** Disabled metrics match
   `baseline.expected.json` exactly: 12 observations, 9 hits, Hit Rate@10 = 0.75,
   reciprocal rank sum 3463/1260 = 2.7484126984126984, MRR 3463/15120 =
   0.22903439153439152 (double printed as 0.22903439153439153 within tolerance).
   `TmdbSignalReplayTests.Run_DisabledMode_ReproducesTheDev356BaselineObservationsAndMetrics`
   passes for per-observation rankings and ranks.
3. **Enabled exercises independently nonzero keyword and collection coverage with positive
   weights.** The pinned fixture carries nonzero provenance-backed coverage for every catalog
   entry: 12/12 movies have keywords (8 distinct keyword IDs), 12/12 have a collection
   (4 distinct collection IDs), 12/12 have full cast (8 distinct actor names).
   `TmdbSignalReplayTests.EnrichedFixture_CarriesNonzeroKeywordAndCollectionCoverage` asserts
   nonzero counts. Neither run is empty/no-op: with `keyword = 1.0` and `collection = 2.0`
   enabled, the keyword Jaccard and same-collection terms are nonzero on same-cluster pairs
   (`SimilarityScorer.AddKeywordTerm`/`AddCollectionTerm`), and the independent-term proof
   `TmdbSignalReplayTests.SimilarityScorer_ExercisesKeywordAndCollectionTermsIndependentlyWithPositiveWeights`
   shows keyword-only = 1.0, collection-only = 1.0, both = 0.0 with weights removed, and
   cast-only = 1.0 for a cast twin — so cast overlap alone does not produce either signal term.
4. **Enabled HitCount ≥ Disabled HitCount at that identical denominator.** 9 ≥ 9 with
   denominator 12. The comparison uses exact hit counts, not rounded HR values.
5. **Why the paired metrics are identical.** The Patron-approved provenance mapping
   (CONCLUSIONS.md:384-392) maps movies 1–12 into four clusters whose keyword sets
   (`1001/1002`, `2001/2002`, `3001/3002`, `4001/4002`), collection IDs (`101`–`104`) and full
   cast align one-to-one with the existing genre clusters. Adding the signal terms therefore
   raises/keeps similarity within the same cluster and leaves cross-cluster similarity at zero,
   so normalized similarity ordering — and hence MMR selection — is unchanged. Identical
   rankings are the honest expected outcome for this cluster-aligned provenance, not an
   unexercised signal path; the coverage and independent-term assertions above are what prove
   the terms were exercised.

## 6. Scrubbed provenance

`baseline.tmdb.sources.json` records `"provenance": "offline-q7-contract"` with, per movie,
the scrubbed relative endpoints (`/3/movie/{tmdbMovieId}`, `/3/movie/{tmdbMovieId}/keywords`,
`/3/movie/{tmdbMovieId}/credits`) and canonical payload SHA-256 digests for details, keywords
and credits. Movie IDs map 1–12 → TMDb movie IDs 1001–1012 in the four Q7 clusters. The file
contains zero `api_key` occurrences, no credential-bearing URIs and no secrets.

## 7. Seed-grouping boundary

This receipt is a profile-replay measurement. It does **not** prove Q9 seed franchise grouping:
the replay has no seed context, and no replay seed, association or grouping was invented.
Seed-grouping acceptance remains mandatory and is verified separately through the production
seed handler, the pure `SeedCollectionGrouper` helper and the top-10 API integration cases
(T023; SC-003).

## 8. Verification runs for this receipt

- `pwsh -NoProfile -File ./scripts/run-evaluation.ps1 … -TmdbSignals Disabled` → exit 0,
  published `artifacts/evaluation/DEV-358/baseline.json`.
- `pwsh -NoProfile -File ./scripts/run-evaluation.ps1 … -TmdbSignals Enabled` → exit 0,
  published `artifacts/evaluation/DEV-358/tmdb.json`.
- `dotnet test tests/LamuFlix.Evaluation/LamuFlix.Evaluation.csproj --filter "FullyQualifiedName~TmdbSignalReplayTests"`
  → 13 passed, 0 failed, 0 skipped.

No BLOCKED or FAIL condition occurred: provenance is available and truthful against the
approved offline mapping, coverage is nonzero and independently exercised for both signals,
and Enabled HitCount equals Disabled HitCount at the identical nonzero denominator.
