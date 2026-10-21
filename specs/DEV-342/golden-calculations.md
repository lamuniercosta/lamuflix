# DEV-342 Golden ranking oracle — independent manual calculations

**Scope**: User Story 2 (T004–T006). This record is authored before `SimilarityScorerGoldenTests.cs` and is derived only from the landed DEV-337 / DEV-338 contracts and closed-form arithmetic. It is not produced by executing `SimilarityScorer`, by a second scorer implementation, or by the DEV-340 query handler.

**Inputs**: `specs/DEV-342/spec.md` US2 (FR-006–FR-009) and `specs/DEV-342/CONCLUSIONS.md` Q3. Fixture profiles and seed choices are the ones committed in `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenFixture.cs`.

## 1. Landed formulas used

Term similarities (per `src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs`):

- Directors, Writers, Cast: Jaccard `|A ∩ B| / |A ∪ B|` over `OrdinalIgnoreCase` sets.
- Genres: `GenreWeights.Similarity` — weighted Jaccard on non-blank names, with genre weight `w(g) = ln((N + 1) / (f(g) + 1)) + 1` (`src/LamuFlix.Core/Domain/GenreWeights.cs`). Equal non-empty sets give `1`; disjoint sets give `0`; the common weight cancels in both cases.
- Release year: `exp(-0.1 · |Δyear|)`.
- Runtime: `exp(-0.01 · |Δminutes|)`.
- IMDb rating: `1 - |Δrating| / 10`.

Normalization (`SimilarityScorer.ComputeScore`): with `maxWeight` the largest term weight present on both sides,

```text
score = Σ (w_i / maxWeight) · s_i  /  Σ (w_i / maxWeight)
```

clamped to `[0, 1]`. If every present term with `w_i > 0` has `s_i = 1`, the landed shortcut returns exactly `1.0`.

Pinned `SimilarityWeights` (explicitly constructed, not option defaults): Director `3.0`, Writers `2.0`, Cast `2.0`, Genres `3.0`, YearProximity `1.0`, RuntimeProximity `0.5`, RatingBoost `0.5`.

## 2. The 30-movie library

Three clusters of ten. Within a cluster every movie has the same genre, release year, runtime, and rating; only directors, writers, and cast vary. Clusters use disjoint person-name vocabularies.

| Cluster | Genre | Year | Runtime | Rating | Directors | Writers | Cast |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Drama (A-01…A-10) | Drama | 1990 | 100 | 7.0 | Ana, Bruno, Celia | Dora, Emil | Fay, Gus |
| Comedy (B-01…B-10) | Comedy | 2000 | 120 | 8.0 | Hugo, Iris, Jonas | Kara, Liam | Mira, Nils |
| Horror (C-01…C-10) | Horror | 2010 | 140 | 6.0 | Olga, Pavel, Quinn | Rosa, Sven | Tara, Ugo |

Full-library genre counts for `GenreWeights.Calculate`: `N = 30`, `f(Drama) = f(Comedy) = f(Horror) = 10`. All three weights are therefore equal, so genre similarity is `1` inside a cluster and `0` across clusters regardless of the numeric weight value.

Each cluster reuses the same ten structural profiles. Let `D1, D2, D3` be the cluster's directors, `W1, W2` its writers, and `C1, C2` its cast. Template `T1` is the seed profile of every cluster:

| Template | Directors | Writers | Cast |
| --- | --- | --- | --- |
| T1 (seed) | {D1, D2} | {W1} | {C1} |
| T2 | {D1, D2} | {W1, W2} | {C1} |
| T3 | {D1} | {W1} | {C1} |
| T4 | {D1, D2} | {W1} | {C2} |
| T5 | {D1} | {W1, W2} | {C1} |
| T6 | {D1, D2} | {W1, W2} | {C2} |
| T7 | {D1} | {W1} | {C2} |
| T8 | {D3} | {W1, W2} | {C1} |
| T9 | {D1} | {W1, W2} | {C2} |
| T10 | {D3} | {W1} | {C2} |

Seeds: `A-05` (T1, Drama), `B-08` (T1, Comedy), `C-06` (T1, Horror).

## 3. Within-cluster closed form

For a pair inside one cluster all seven scored fields are present on both sides, so `maxWeight = 3`, the scaled denominator is `(3+2+2+3+1+0.5+0.5)/3 = 4`, and every present field contributes:

- Directors `d` (Jaccard), Writers `w` (Jaccard), Cast `c` (Jaccard);
- Genres `1`, Year `1`, Runtime `1`, Rating `1`.

```text
score = (3d + 2w + 2c + 3·1 + 1·1 + 0.5·1 + 0.5·1) / 12
      = (3d + 2w + 2c + 5) / 12
```

Similarities relative to the seed `T1` and resulting scores for the nine non-seed templates:

| Template | d | w | c | 3d + 2w + 2c + 5 | Score |
| --- | --- | --- | --- | --- | --- |
| T2 | 1 | 1/2 | 1 | 11 | 11/12 = 0.916666… |
| T3 | 1/2 | 1 | 1 | 10.5 | 7/8 = 0.875 |
| T4 | 1 | 1 | 0 | 10 | 5/6 = 0.833333… |
| T5 | 1/2 | 1/2 | 1 | 9.5 | 19/24 = 0.791666… |
| T6 | 1 | 1/2 | 0 | 9 | 3/4 = 0.75 |
| T7 | 1/2 | 1 | 0 | 8.5 | 17/24 = 0.708333… |
| T8 | 0 | 1/2 | 1 | 8 | 2/3 = 0.666666… |
| T9 | 1/2 | 1/2 | 0 | 7.5 | 5/8 = 0.625 |
| T10 | 0 | 1 | 0 | 7 | 7/12 = 0.583333… |

Each step is exactly `0.5 / 12 = 1/24` apart, so all nine within-cluster scores are strictly distinct and the third-to-fourth boundary (T4 `20/24` vs T5 `19/24`) is strict. The all-one shortcut does not apply to any candidate: every template differs from `T1` in at least one positively weighted field, so the weighted-average branch is used and the maximum possible candidate score is `11/12 < 1`.

## 4. Cross-cluster bound

Across clusters, director/writer/cast vocabularies and genres are disjoint, so `d = w = c = 0` and genre similarity is `0`. Only year, runtime, and rating can contribute:

```text
score = (1·Syear + 0.5·Sruntime + 0.5·Srating) / 12 ≤ 2/12 = 1/6 ≈ 0.166667
```

Actual cross-cluster scores:

| Pair (direction) | Δyear | Δruntime | Δrating | Syear | Sruntime | Srating | Score |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A ↔ B | 10 | 20 | 1.0 | 0.367879 | 0.818731 | 0.9 | 0.102270 |
| A ↔ C | 20 | 40 | 1.0 | 0.135335 | 0.670320 | 0.9 | 0.076708 |
| B ↔ C | 10 | 20 | 2.0 | 0.367879 | 0.818731 | 0.8 | 0.098104 |

The largest cross-cluster score (`0.102270`) is far below the smallest within-cluster score (`7/12 ≈ 0.583333`). Therefore no cross-cluster movie can reach the top four of any seed, and the top-3 for each seed is fully determined inside its own cluster.

## 5. Per-seed ordered top-3

Seed removal is test setup only: the other 29 identities are scored.

### Seed A-05 (Drama, T1)

| Rank | Movie | Template | Score |
| --- | --- | --- | --- |
| 1 | A-02 | T2 | 11/12 |
| 2 | A-09 | T3 | 7/8 |
| 3 | A-07 | T4 | 5/6 |
| 4 | A-01 | T5 | 19/24 |
| 5 | A-10 | T6 | 3/4 |
| 6 | A-04 | T7 | 17/24 |
| 7 | A-08 | T8 | 2/3 |
| 8 | A-03 | T9 | 5/8 |
| 9 | A-06 | T10 | 7/12 |

Expected top-3: **A-02, A-09, A-07**.

### Seed B-08 (Comedy, T1)

| Rank | Movie | Template | Score |
| --- | --- | --- | --- |
| 1 | B-03 | T2 | 11/12 |
| 2 | B-06 | T3 | 7/8 |
| 3 | B-01 | T4 | 5/6 |
| 4 | B-10 | T5 | 19/24 |
| 5 | B-04 | T6 | 3/4 |
| 6 | B-09 | T7 | 17/24 |
| 7 | B-02 | T8 | 2/3 |
| 8 | B-07 | T9 | 5/8 |
| 9 | B-05 | T10 | 7/12 |

Expected top-3: **B-03, B-06, B-01**.

### Seed C-06 (Horror, T1)

| Rank | Movie | Template | Score |
| --- | --- | --- | --- |
| 1 | C-09 | T2 | 11/12 |
| 2 | C-01 | T3 | 7/8 |
| 3 | C-04 | T4 | 5/6 |
| 4 | C-02 | T5 | 19/24 |
| 5 | C-08 | T6 | 3/4 |
| 6 | C-10 | T7 | 17/24 |
| 7 | C-05 | T8 | 2/3 |
| 8 | C-03 | T9 | 5/8 |
| 9 | C-07 | T10 | 7/12 |

Expected top-3: **C-09, C-01, C-04**.

## 6. Why the selected three outrank the rest

For every seed the three expected movies are the `T2`, `T3`, and `T4` profiles of its own cluster. By §3 they score `11/12`, `7/8`, and `5/6`, which are strictly greater than `T5` at `19/24` and every lower template. All other-cluster movies are bounded by `1/6` per §4. Both the top-3 ordering and the third-place membership boundary are therefore fixed by score arithmetic alone, with no tie and no dependence on any tie-breaker. The golden assertions in `SimilarityScorerGoldenTests.cs` compare against exactly these independently derived identities.
