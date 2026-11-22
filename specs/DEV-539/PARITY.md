# T015a pre-change score parity evidence

Captured for Task 015a (US4) before any Crowd scoring code was written into this worktree.

## Provenance

- **Base revision (pre-Crowd scorer)**: `df855da3bc4fe6a53ed42faf8659b4c0f1f14e7d` — `DEV-539 - Library feature-load resolution`, the last commit before Phase 6.
- **Capture method**: a scratch worktree at that SHA, with the unchanged fixture source copied in. The fixtures exercise the two production scoring paths that Phase 6 changes: `NearestSeedScorer.Rank` (For you) and `SimilarityScorer.Score` (Related). The capture ran the fixture once and read the actual values; no expected value was derived from the changed scorer.
- **Harness revision**: not the DEV-545 capture harness — these are unit-level pins over the shared production scorer, where the harness revision is the base SHA itself. The DEV-545 before/after run remains T017.
- **Fixture inputs** (identical in the capture and the pinned test, `CrowdParityFixture`): default `RecommendationOptions`; genre counts `N=100, Drama=10, Comedy=20`; no counted keywords; seed movie 90 ("Saga Starter", liked, Drama/Ana/Dora,Emil/collection 11/plot `[1, 0]`) and candidate movie 2 ("Candidate", Comedy+Comedy, Bruno…, plot `[0.44, 0.9]`).
- **Seed**: none — the fixtures are pure pair scores, so no random seed is applicable.

## Captured pre-change values

- **For you** (`NearestSeedScorer.Rank`, winner seed "Saga Starter", signal `Liked`): `0.5358206122133712`
  - breakdown contributions: Collection `0`, Director `0.2`, Themes `0`, Plot `0.12837247925306366`, Cast `0.13333333333333333`, Genre `0.07411479962697422`
- **Related** (`SimilarityScorer.Score`, plain pair): `0.5358206122133712`
  - breakdown contributions: same six values in the same order

## Pinned by

- `tests/LamuFlix.UnitTests/Features/Recommendations/CrowdScoreParityTests.cs` — exact-score and criteria-table parity for the disabled TMDb, blank-API-key, and whitespace-API-key states, with `Recommendations:CrowdEnabled` on so the omission is proven rather than accidental.
- `tests/LamuFlix.UnitTests/Features/Recommendations/CrowdParityFixture.cs` — the shared fixture, carrying the base SHA in its provenance comment.
