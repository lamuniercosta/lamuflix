# Feature Specification: DEV-342 SimilarityScorer property and golden tests

**Feature Branch**: `feature/342-spec`

**Created**: 2026-10-20

**Status**: Draft

**Input**: Ticket DEV-342 (size M, parent DEV-288, no ui); settled grill brief `specs/DEV-342/brief.md` (grill closed Q8/12); facts from notes `recon-DEV-342` and `recon-DEV-342-2`; Patron rulings in `specs/DEV-342/CONCLUSIONS.md` Q1–Q8; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## Domain vocabulary *(mandatory — published from root CONTEXT.md and brief)*

Root `CONTEXT.md` already carries Recommendation, Reason, Genre weight, GenreWeights, and Genre similarity. Brief Q8 requires no root glossary edit and records that no new domain glossary terms were settled. Published Spec Kit artifacts use those terms as written. Feature-scoped labels used only in this package:

- **Scorer invariant properties**: The four ticket properties over `SimilarityScorer` — self-similarity, symmetry, boundedness, and one-way Reasons implication.
- **Golden ranking fixture**: The hand-curated 30-movie library with three seeds and independently derived ordered top-3 identities.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Scorer invariant properties across 1000 cases each (Priority: P1)

A verifier confirms that the real merged `SimilarityScorer` obeys the four ticket properties for generated inputs: exact self-score 1.0 on nonempty seven-field scoring profiles (including valid all-zero weights), symmetry within absolute tolerance 1e-12 under identical weights and the same GenreWeights snapshot, finite scores clamped to inclusive [0.0, 1.0], and nonempty Reasons whenever the final score is greater than zero (one-way; converse not asserted). Local FsCheck generators and shrinkers supply valid MovieFeatures pairs, finite nonnegative SimilarityWeights, and pure GenreWeights contexts. Self-similarity inputs are constructed with at least one scoring feature and keep that invariant while shrinking. The other three properties retain featureless inputs. Each property uses MaxTest = 1000, Category = Property, and a distinct committed Replay seed, with shrinking and minimized-counterexample diagnostics preserved.

**Why this priority**: Ticket acceptance requires all four scorer properties across 1000 generated iterations each. Without this suite there is no property proof of scorer correctness.

**Independent Test**: Run the four tagged properties in `LamuFlix.UnitTests` against the real merged scorer and GenreWeights. No HTTP, handler, or golden fixture required.

**Acceptance Scenarios**:

1. **Given** a constructively generated MovieFeatures value with at least one of the seven scored fields present, **When** the self-similarity property scores `(a, a)` under valid SimilarityWeights (including all-zero), **Then** the score equals exactly `1.0`.
2. **Given** a generated pair `(a, b)` under the same SimilarityWeights and GenreWeights snapshot, **When** the symmetry property scores both orders, **Then** `Math.Abs(scoreAB - scoreBA) <= 1e-12`.
3. **Given** a generated pair and valid weights, **When** the boundedness property scores the pair, **Then** the score is finite and satisfies `0.0 <= score <= 1.0` with no outside-bound epsilon.
4. **Given** a generated pair whose score is greater than zero, **When** the Reasons property inspects the result, **Then** Reasons are nonempty; when the score is zero, nonempty Reasons remain allowed (underflow / zero-denominator cases) and are not forbidden.
5. **Given** each of the four properties, **When** they run under the property gate, **Then** each uses `[Property(MaxTest = 1000, Replay = "...")]` with a distinct committed Replay seed, `[Trait("Category", "Property")]`, preserved shrinking, and reproducible minimized-counterexample diagnostics.
6. **Given** existing unrelated property tests that use `MaxTest = 100`, **When** this feature lands, **Then** those tests remain unchanged.

---

### User Story 2 - Independent golden top-3 rankings for three seeds (Priority: P2)

A verifier confirms ranking behavior on a fixed hand-curated library of exactly 30 movie identities. Three seeds are drawn from those 30. For each seed, the other 29 are scored directly through the real merged scorer with GenreWeights calculated from the full 30-movie library and explicitly constructed SimilarityWeights pinned to Director = 3.0, Writers = 2.0, Cast = 2.0, Genres = 3.0, YearProximity = 1.0, RuntimeProximity = 0.5, RatingBoost = 0.5. Expected ordered top-3 identities are independently hand-derived and recorded in `golden-calculations.md` before golden assertions exist. Relevant scores are strictly separated so ties never govern top-3 membership or order. Seed removal from the candidate list is test setup only and does not call or depend on the DEV-340 query handler.

**Why this priority**: Ticket acceptance requires the golden fixture to produce the expected top-3 for each of three seeds, after DEV-337 and DEV-338 are merged.

**Independent Test**: Run the three seed assertions against the typed in-memory fixture and real scorer. Property suite from US1 is not required to interpret a single seed failure, but Phase B still delivers both stories before review.

**Acceptance Scenarios**:

1. **Given** the committed 30-movie typed fixture and three seed identities drawn from it, **When** each seed's remaining 29 candidates are scored with full-library GenreWeights and the pinned explicit SimilarityWeights, **Then** the ordered top-3 movie identities match the independently recorded expected list for that seed.
2. **Given** each seed's relevant candidate scores, **When** top-3 membership and order are determined, **Then** strict score separation holds across the top three and the membership boundary so ties do not decide either.
3. **Given** the golden expected rankings, **When** they are authored, **Then** they come from independent manual arithmetic recorded in `specs/DEV-342/golden-calculations.md`, not from executing the scorer under test, not from a shadow scorer, and not from the DEV-340 handler.
4. **Given** the fixture weights, **When** they are constructed, **Then** they use the explicit pinned coefficients above rather than relying on mutable option defaults.
5. **Given** Phase A, **When** the spec package is drafted, **Then** no golden movie data or expected rankings are authored yet; fixture and calculations are written only in Phase B after both prerequisite implementations merge.

### Edge Cases

- Wholly unscoreable / empty seven-field profiles and zero-denominator identity versus mismatch follow the landed DEV-338 contract (`recon-DEV-342-2:19-22,42-44`); properties interpret ticket semantics without adding new property deliverables.
- Extreme valid weight ratios may underflow the normalized score to zero while Reasons remain nonempty (`recon-DEV-342-2:37-38`); Reasons property must not assert the converse.
- MetaScore and FullPlot do not establish scoring-feature presence or identity (`recon-DEV-342-2:58`).
- Selected-movie exclusion belongs to DEV-340 (Description:14); golden seed filtering is harness setup only.
- Exact landed VO bounds, scorer/IDF call signatures, and the string-genre / integer-ID bridge are Phase B pickup facts, not invented here (`recon-DEV-342-2:58-64`; brief Q6).
- No production edits, predecessor-test rewrites, new packages/projects/layers, disk-backed golden loaders, `LamuFlix.Tests.Common` changes, HTTP/OpenAPI/UI, schema, secrets, `Features:LocalPlay`, or `Process.Start`.
- Phase B build remains gated on Epic 7 (`specs/PRODUCT.md`) plus merged DEV-337 and DEV-338 production implementations verified by pickup drift recon. Spec-package merges do not establish production availability. Phase A does not authorize stubs or Spec-workspace build.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Provide local FsCheck generators and valid shrinkers in `tests/LamuFlix.UnitTests/Features/Recommendations/MovieFeaturesArbitraries.cs` covering MovieFeatures feature/pair families, finite nonnegative SimilarityWeights in `[0.0, double.MaxValue]`, and real pure GenreWeights contexts (populated counts and empty/unknown defaults), without catalog/repository.
- **FR-002**: Generators MUST include explicit branches for empty, single-field, partially missing, identical, and nonempty disjoint/overlapping shared-vocabulary profiles, with case variants and preserved null/blank tokens per landed MovieFeatures rules. Self-similarity generation and shrinking MUST stay on nonempty seven-field scoring profiles. MetaScore and FullPlot MUST NOT establish scoring-feature presence or identity.
- **FR-003**: Weight generators MUST cover all-zero, single-positive across all seven positions, ordinary mixed, and extreme-ratio/MaxValue families. Invalid-argument guard coverage remains predecessor responsibility; do not duplicate it here.
- **FR-004**: Provide the four scorer properties in `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerInvariantPropertyTests.cs`: self-similarity exactly `1.0`; symmetry within `1e-12`; boundedness as finite inclusive `[0.0, 1.0]`; Reasons as `score > 0.0 => Reasons nonempty` (one-way only).
- **FR-005**: Each of the four properties MUST use `[Property(MaxTest = 1000, Replay = "...")]` with a distinct committed Replay seed and `[Trait("Category", "Property")]`, preserve shrinking, and report reproducible minimized failures. Unrelated existing `MaxTest = 100` property tests MUST remain unchanged.
- **FR-006**: Provide typed in-memory golden data in `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenFixture.cs`: exactly 30 fixed movie identities, three seeds drawn from those 30, explicitly constructed SimilarityWeights with Director = 3.0, Writers = 2.0, Cast = 2.0, Genres = 3.0, YearProximity = 1.0, RuntimeProximity = 0.5, RatingBoost = 0.5, and independently derived ordered top-3 identity expectations per seed. No disk I/O or external data loader.
- **FR-007**: Provide golden assertions in `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerGoldenTests.cs` that, for each seed, score the other 29 movies through the real merged scorer with GenreWeights calculated from the full 30-movie library and the fixture's explicit weights, then assert the ordered top-3 identities. Seed removal is test setup only; do not call the DEV-340 handler or add the selected-movie exclusion property.
- **FR-008**: Record independent oracle arithmetic and why the selected three outrank remaining candidates in `specs/DEV-342/golden-calculations.md`, authored in Phase B after prerequisites merge. Independently derived expected rankings and the completed oracle record MUST exist before golden assertions start. Do not generate expected rankings from the scorer under test or create a shadow scorer. Put calculation explanations in that document, not in explanatory C# comments.
- **FR-009**: Curate strict score separation across each seed's top three and the membership boundary so ties never govern membership or order.
- **FR-010**: Phase B MUST NOT begin until Epic 7 completion and merged DEV-337 and DEV-338 production implementations on main are verified by pickup drift recon that records landed VO bounds, constructors, scorer/IDF call signatures, predecessor-test overlap, and the string-genre/integer-ID bridge (including null/blank handling). Do not invent a bridge, stub SimilarityScorer/GenreWeights, duplicate contracts, or alter dependencies to fit generators. Pickup contradictions route for a decision before any dependency edit. Preserve landed DEV-337/DEV-338 tests, including DEV-338's planned `SimilarityScorerTests.cs` and `SimilarityScorerPropertyTests.cs`.
- **FR-011**: This feature MUST NOT add production code, new NuGet/npm packages, new projects/layers, `LamuFlix.Tests.Common` changes, HTTP/OpenAPI/generated TypeScript, UI, schema/migration, secrets, `Features:LocalPlay`, `Process.Start`, Gherkin/Reqnroll, root `CONTEXT.md` edit, ADR, or `ASSUMPTIONS.md`. Approved new files only: the four test files under `tests/LamuFlix.UnitTests/Features/Recommendations/` named above plus `specs/DEV-342/golden-calculations.md`.

### Key Entities

- **MovieFeaturesArbitraries**: Local generators/shrinkers for valid scorer inputs.
- **SimilarityScorerInvariantPropertyTests**: The four 1000-case scorer properties.
- **SimilarityScorerGoldenFixture**: Typed 30-movie library, seeds, explicit weights, expected identities.
- **SimilarityScorerGoldenTests**: Direct scorer execution and ordered top-3 assertions.
- **golden-calculations.md**: Independent manual oracle audit record (Phase B).
- **SimilarityScorer / GenreWeights / SimilarityWeights / MovieFeatures**: Landed predecessor contracts consumed by tests; not implemented here.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All four scorer properties pass across 1000 generated iterations each under the property gate (ticket Description:17).
- **SC-002**: The golden fixture produces the expected ordered top-3 for each of the three seed movies (ticket Description:18).
- **SC-003**: Self-similarity failures never accept a non-exact `1.0`; symmetry failures never accept absolute difference above `1e-12`; boundedness failures never accept nonfinite or out-of-range scores; Reasons failures never accept empty Reasons when score > 0.
- **SC-004**: Golden failures are evidence of ranking mismatch against the independent oracle, not tautological snapshots of scorer output.
- **SC-005**: Phase A makes no implementation-gate claim. In Phase B, on a test-only diff with zero changed production C# under `src/`, configured gates exit as ruled: Roslyn, cyclomatic complexity (refactor threshold <= 6), JetBrains InspectCode, property tests, vulnerable packages, `dotnet format --verify-no-changes`, and `dotnet test` exit 0 (PASS); `scripts/run-mutation.ps1` reports native exit-2 `SKIPPED (scope-empty)`, nonblocking and never PASS; web gates are N/A; a gate that cannot run has not passed.

## Assumptions

- No taste `[assumed]` artifact is required (brief Q8). No `ASSUMPTIONS.md`.
- Existing root `CONTEXT.md` vocabulary suffices; no glossary edit (brief Q8).
- Phase B remains gated on Epic 7 (`specs/PRODUCT.md`) plus merged DEV-337 and DEV-338 production implementations and pickup drift recon (brief Q1/Q6/Q8). Specifying in Phase A does not authorize Phase B build or stubs.
- Exact landed VO bounds, call shapes, and genre bridge are Phase B pickup facts (`recon-DEV-342-2:58-64`), not Quill inventions.
- No owner structural checkbox (brief Q8).
- Exclusions in brief frozen scope remain out of scope; anything else is a follow-up issue, not a finding in this round.
- Q7 closing bar, mandatory three axes (Sentry, Ledger, Compass), and two-round caps govern later plan challenge and Phase 4 review; they do not expand this feature's deliverables.
