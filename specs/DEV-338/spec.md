# Feature Specification: DEV-338 SimilarityScorer pure function

**Feature Branch**: `feature/338-spec`

**Created**: 2026-10-20

**Status**: Draft

**Input**: Ticket DEV-338 (size M, parent DEV-288, no ui); settled grill brief `specs/DEV-338/brief.md` (grill closed Q9/12); facts from note `recon-DEV-338` section 10 and sections 3–8; Patron rulings in `specs/DEV-338/CONCLUSIONS.md` Q1–Q9; taste copy in `specs/DEV-338/ASSUMPTIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md`.

## Domain vocabulary *(mandatory — published from root CONTEXT.md and brief)*

Root `CONTEXT.md` already carries **Recommendation**, **Reason**, **Genre weight**, **GenreWeights**, and **Genre similarity**. Brief Q8 requires no root glossary edit. Published Spec Kit artifacts use those terms as written, plus these scoping notes kept in this feature only:

- **Similarity score**: Normalized pairwise likeness of two movies' scoring features, in [0, 1].
- **Reason**: A caller-facing plain-text explanation of a positive weighted term, plus the Q1 identity-boundary explanation. Matches root `CONTEXT.md` Reason ("A caller-safe explanation attached to a recommendation") scoped to this pure-function return contract.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Bound pairwise likeness from shared people and genres (Priority: P1)

A caller asks how alike two movies are on director, writers, cast, and genres. The scorer returns a bounded score using only terms that have data on both sides. Shared people use set Jaccard under MovieFeatures case-insensitive membership. Genres use the landed GenreWeights genre-similarity contract (IDF-weighted Jaccard and unknown-genre default 1.0) without a second genre formula. Missing or empty on either side drops that term from both the numerator and the denominator. Nonempty but disjoint sets stay eligible, contribute similarity 0, and keep their weight.

**Why this priority**: Overlap terms are the ticket's primary likeness signal. Without them there is no scorer MVP.

**Independent Test**: Unit examples for each of the four overlap terms in isolation, known Jaccard ratios, delegated genre rarity and unknown defaults, missing-side exclusion versus disjoint zero inclusion, mixed and zero weights, and null/invalid-weight guards. No infrastructure.

**Acceptance Scenarios**:

1. **Given** two movies that share at least one director (and no other overlapping eligible terms under controlled weights), **When** Score is called, **Then** the director term uses Jaccard under OrdinalIgnoreCase set semantics and contributes to the normalized score.
2. **Given** two nonempty writer or cast sets that are disjoint, **When** Score is called, **Then** that term has data, contributes similarity 0, retains its weight in the denominator, and emits no Reason for that term.
3. **Given** an empty directors set on either side, **When** Score is called, **Then** the director term is excluded from both numerator and denominator.
4. **Given** both genre sets nonempty, **When** Score is called, **Then** genre similarity is taken from the landed GenreWeights contract (weighted Jaccard; unknown/empty-library default weight 1.0) and is not recomputed with a second formula inside the scorer. Exact call shape comes from Phase B recon of landed signatures.
5. **Given** a null `a`, `b`, `weights`, or `genreWeights`, **When** Score is called, **Then** the call throws ArgumentNullException naming that argument.
6. **Given** any of the seven SimilarityWeights values negative or nonfinite (NaN or either infinity), **When** Score is called, **Then** the call throws ArgumentOutOfRangeException naming `weights`. Finite zero through MaxValue remains valid.

---

### User Story 2 - Add proximity terms and stable normalized identity (Priority: P2)

A caller also scores year proximity, runtime proximity, and rating proximity. Year and runtime use exponential decay with fixed const rates. Rating uses the ticket formula `1 - |ratingA - ratingB| / 10`. Eligible weights are scaled by their maximum before accumulation so finite MaxValue weights cannot overflow. The final score is clamped to [0, 1]. When every eligible positive-weight term equals 1, the score is exactly 1.0. When no positive applicable weight remains, identical nonempty seven-field scoring profiles return 1.0 with the identity Reason; otherwise 0.0 with empty Reasons. Both fully empty scoring profiles return 0.0 with empty Reasons.

**Why this priority**: Completes the ticket's seven-term formula, normalization, and identity AC.

**Independent Test**: Unit examples for known exponential distances, rating boundaries, MaxValue stabilization, tiny-weight underflow, zero-denominator identity versus mismatch, all-empty and nonempty identical profiles, and exact-1 returns. FsCheck properties for finite [0,1] bound, symmetry within 1e-12, exact nonempty-profile self-score 1, and unchanged inputs.

**Acceptance Scenarios**:

1. **Given** both release years present, **When** Score is called, **Then** year proximity is `exp(-0.1 * |yearA - yearB|)` with year values cast to double before the absolute difference.
2. **Given** both runtimes present, **When** Score is called, **Then** runtime proximity is `exp(-0.01 * |runtimeA - runtimeB|)` with runtime values cast to double before the absolute difference.
3. **Given** both IMDb ratings present, **When** Score is called, **Then** rating proximity is `1 - |ratingA - ratingB| / 10` with rating values cast to double before the absolute difference, and the existing `RatingBoost` weight is applied as that term's weight (never an additive boost).
4. **Given** eligible weights that include a finite MaxValue, **When** Score is called, **Then** eligible weights are divided by their maximum before numerator and denominator accumulation (maximum zero handled before dividing), the result is finite, and the final score is clamped to [0, 1].
5. **Given** at least one eligible positive-weight term and every such term equals 1.0, **When** Score is called, **Then** the score is exactly 1.0.
6. **Given** all seven applicable weights effectively zero (denominator zero) and identical nonempty seven-field scoring profiles, **When** Score is called, **Then** the score is 1.0 with exactly one Reason whose text is `Identical feature profile`.
7. **Given** denominator zero and non-identical or empty scoring profiles, or no eligible terms outside that Q1 identity case, **When** Score is called, **Then** the score is 0.0 with empty Reasons.
8. **Given** the same pair swapped as `(a, b)` and `(b, a)` under the same weights, **When** Score is called, **Then** the scores are equal within absolute tolerance 1e-12.

---

### User Story 3 - Emit deterministic caller-safe Reason chips (Priority: P3)

Whenever an eligible term has positive weight and positive similarity, the scorer emits one plain-text Reason for that term in fixed weight order. Shared set names use a stable representative spelling across both inputs so swapped callers see identical Reasons. Control characters are replaced with spaces and tokens are trimmed; empty sanitized tokens are omitted; if none remain, the generic fallback for that set term is used. Reasons never include movie IDs, internal keys, file paths, or diagnostics. When the final score is greater than zero, Reasons are nonempty. Per-term Reasons may still appear when scaled arithmetic underflows the final score to zero.

**Why this priority**: Ticket AC requires explainable, caller-safe Reasons whenever the score is positive.

**Independent Test**: Unit examples for term eligibility, fixed order, case-variant spelling under swap, preserved null/blank feature tokens with sanitization for display only, control characters, adversarial plain text, generic fallbacks, exclusion of non-feature/internal fields, and Q1 identity Reason. FsCheck properties for deterministic swap-identical Reasons and `Score > 0 => Reasons nonempty` (one-way; Q9 supersedes Q7 iff wording).

**Acceptance Scenarios**:

1. **Given** multiple positive weighted terms, **When** Score is called, **Then** Reasons appear in fixed order Director, Writers, Cast, Genres, YearProximity, RuntimeProximity, RatingBoost, one Reason per eligible term with `w > 0` and `s > 0`.
2. **Given** shared set members that differ only by letter case, **When** `(a, b)` and `(b, a)` are scored, **Then** Reasons texts are identical: each matched token uses the Ordinal-minimum spelling across both inputs, then tokens sort OrdinalIgnoreCase with Ordinal tie-break.
3. **Given** shared tokens that sanitize to empty (control characters / blanks) while other tokens remain, **When** Score is called, **Then** empty sanitized tokens are omitted and remaining tokens appear in the label.
4. **Given** an eligible positive set overlap whose sanitized shared tokens are all empty, **When** Score is called, **Then** the Reason text is exactly `Shared director`, `Shared writers`, `Shared cast`, or `Shared genres` for that term.
5. **Given** positive director, writers, cast, genres, year, runtime, or rating contribution, **When** Score is called, **Then** labels use `Same director: <names>`, `Shared writers: <names>`, `Features <names>`, `Shares <genres>`, `Similar release years`, `Similar runtime`, or `Similar rating` respectively (names/genres are the sorted sanitized shared tokens).
6. **Given** a final normalized score of 0 caused only by scaled underflow while at least one term had `w > 0` and `s > 0`, **When** Score is called, **Then** that term's Reason is still emitted, and the one-way rule `Score > 0 => Reasons nonempty` still holds for positive final scores.
7. **Given** any emitted Reason, **When** its text is inspected, **Then** it contains no movie ID, GUID, internal key, file path, or score diagnostic, and is plain text (no HTML or Markdown generation).

### Edge Cases

- Both fully empty seven-field scoring profiles: score 0.0, empty Reasons (not the Q1 identity fallback).
- Q1 zero-denominator identity: score 1.0 with sole Reason `Identical feature profile`; Q1 is the only synthetic identity Reason.
- Q1 zero-denominator non-identity: score 0.0, empty Reasons.
- Zero weight on a term: term never emits a normal per-term Reason even if similarity would be positive.
- Similarity 0 on an eligible term: weight retained in denominator; no Reason for that term.
- Scoring-feature identity compares only Directors, Writers, Cast, Genres (null-safe OrdinalIgnoreCase set equality), ReleaseYear, Runtime, and ImdbRating (exact scalar equality). MetaScore, FullPlot, movie IDs, and reference equality are ignored.
- Display sanitization never changes scoring inputs or set membership.
- No HTTP route, OpenAPI, generated TypeScript, UI chip surface, configuration, options, schema, playback, secrets, or predecessor (DEV-336/DEV-337) implementation in this ticket.
- Phase B build remains gated on Epic 7 (`specs/PRODUCT.md`) plus merged DEV-336 and DEV-337 implementations and landed-signature recon. Phase A specification does not authorize stubs or predecessor code.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Provide `public static class SimilarityScorer` in `LamuFlix.Core.Features.Recommendations` with `Score(MovieFeatures a, MovieFeatures b, SimilarityWeights weights, GenreWeights genreWeights)` returning `SimilarityResult`. No interface, service wrapper, DI registration, or cache.
- **FR-002**: Provide `public sealed record Reason(string Text)` and `public sealed record SimilarityResult(double Score, ImmutableList<Reason> Reasons)` in the same namespace.
- **FR-003**: Compute seven terms only: Director, Writers, Cast, Genres (IDF-weighted via GenreWeights), YearProximity, RuntimeProximity, and RatingBoost (rating proximity). Do not rename or add SimilarityWeights properties. Do not treat RatingBoost as an additive boost.
- **FR-004**: A set term has data only when both corresponding sets are nonempty. A scalar term has data only when both values are non-null. Missing/empty on either side excludes the term from numerator and denominator. Nonempty disjoint sets remain eligible with `s = 0` and retain `w`.
- **FR-005**: Directors, Writers, and Cast MUST use unweighted Jaccard `|A ∩ B| / |A ∪ B|` under MovieFeatures `StringComparer.OrdinalIgnoreCase` set semantics.
- **FR-006**: When both genre sets are nonempty, genre similarity MUST come from the landed DEV-337 GenreWeights contract (weighted Jaccard and unknown/empty-library default weight 1.0). The scorer MUST NOT implement a second genre formula. Exact landed method names and any genre-identity bridging required by landed MovieFeatures/GenreWeights signatures MUST come from Phase B pickup recon; do not invent them in Phase A.
- **FR-007**: Year proximity MUST be `Math.Exp(-0.1 * distance)` and runtime proximity `Math.Exp(-0.01 * distance)` with `const double` decay rates and absolute differences after casting scalars to double. Rating proximity MUST be `1.0 - Math.Abs((double)ratingA - (double)ratingB) / 10.0`.
- **FR-008**: Normalize as `sum((w_i / maxW) * s_i) / sum(w_i / maxW)` over terms that have data, after handling a zero maximum, then clamp to [0, 1]. No additive boost and no unbounded penalties.
- **FR-009**: When `sum(w_i) == 0` over terms with data: if the seven scoring fields are identical and at least one scoring field is present, return score 1.0 with one Reason `Identical feature profile`; otherwise return 0.0 with empty Reasons.
- **FR-010**: When at least one eligible positive-weight term exists and every such term has `s_i == 1.0`, return exactly 1.0. `Score(a, a)` MUST be 1.0 whenever `a` has at least one scoring feature. `Score(a, b)` MUST be symmetric and bounded to [0, 1].
- **FR-011**: Throw `ArgumentNullException` (`nameof`) for null `a`, `b`, `weights`, or `genreWeights`. Throw `ArgumentOutOfRangeException` (`nameof(weights)`) if any of the seven weights is `< 0.0` or not finite. Do not edit SimilarityWeights, RecommendationOptions, MovieFeatures, or GenreWeights.
- **FR-012**: Emit one Reason per eligible term with `w_i > 0` and `s_i > 0`, in fixed order Director, Writers, Cast, Genres, YearProximity, RuntimeProximity, RatingBoost, using the `[assumed]` label templates and generic fallbacks in `ASSUMPTIONS.md`. Shared set tokens: Ordinal-min spelling across both inputs per case-insensitive match; sort OrdinalIgnoreCase then Ordinal. Replace control characters with spaces, trim, omit empty sanitized tokens; if none remain use `Shared director` / `Shared writers` / `Shared cast` / `Shared genres`. Preserve raw feature membership for scoring; sanitize display only. Plain text only; no internal identifiers; callers escape for their rendering context.
- **FR-013**: Reasons MUST be nonempty whenever the final score is greater than zero (one-way implication). Emit per-term Reasons whenever `w_i > 0` and `s_i > 0` even if scaled arithmetic underflows the final score to 0. Empty Reasons remain correct for Q1 non-identity zero-denominator and for pairs with no contributing terms.
- **FR-014**: Proof MUST use focused unit tests in `SimilarityScorerTests.cs` and FsCheck properties in `SimilarityScorerPropertyTests.cs` (optional separate `ReasonTests.cs` if focused Reason coverage merits it), existing xUnit v3, Shouldly, and FsCheck only. Property tests MUST be tagged for `run-property-tests.ps1`, cover finite [0,1] scores, symmetry within 1e-12, exact nonempty-profile self-score 1, deterministic swap-identical Reasons, `Score > 0 => Reasons nonempty`, and unchanged input snapshots. No IO, mocks, Testcontainers, new packages, Gherkin, or property-test opt-out.
- **FR-015**: This feature MUST NOT add HTTP, OpenAPI, generated TypeScript, UI, infrastructure, persistence, configuration, options edits, schema/migration, DI, interfaces, services, caching, recommendation orchestration, candidate sourcing, predecessor MovieFeatures/GenreWeights implementations or stubs, new dependencies, projects, layers, secrets, `Features:LocalPlay`, or `Process.Start`. Approved production files only: `SimilarityScorer.cs`, `SimilarityResult.cs`, `Reason.cs` under `src/LamuFlix.Core/Features/Recommendations/`. Approved test files only under `tests/LamuFlix.UnitTests/Features/Recommendations/` as named above. No root `CONTEXT.md` edit and no ADR.

### Key Entities

- **SimilarityScorer**: Pure static function that scores two MovieFeatures values.
- **SimilarityResult**: Bounded score plus immutable Reason list.
- **Reason**: Caller-safe plain-text explanation chip (see Domain vocabulary).
- **MovieFeatures**: Settled DEV-336 scoring input (seven scored fields used here; MetaScore/FullPlot ignored for identity and scoring terms).
- **GenreWeights**: Settled DEV-337 IDF table used for genre similarity delegation.
- **SimilarityWeights**: Existing seven-weight options record; unchanged.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For any pair with at least one scoring feature on `a`, `Score(a, a)` equals exactly 1.0 under every valid finite weight combination covered by the test matrix.
- **SC-002**: `Score(a, b)` equals `Score(b, a)` within absolute tolerance 1e-12, and every returned score is finite and inside [0, 1].
- **SC-003**: Isolated director/writers/cast Jaccard, delegated genre similarity, exponential year/runtime, and rating-proximity examples match the ruled formulas within ordinary double comparison used by the unit suite.
- **SC-004**: Missing-side terms are excluded; nonempty disjoint set terms contribute 0 similarity while retaining weight; zero-denominator identity versus mismatch matches Q1; MaxValue weights do not produce nonfinite scores.
- **SC-005**: Whenever the final score is greater than zero, Reasons are nonempty and contain no internal identifiers; swap-identical pairs emit identical Reason texts; fixed term order is preserved.
- **SC-006**: Backend verification for size:M runs configured Phase B gates with thresholds from `harness.yml` only; property tests have no opt-out; unchanged `/web` is scope-empty skip, never recorded as pass. Phase A makes no gate claim.

## Assumptions

- Label templates and generic fallbacks are Patron `[assumed]` taste decisions recorded in `specs/DEV-338/ASSUMPTIONS.md` (Q1, Q4, Q9).
- Year decay `0.1`/year and runtime decay `0.01`/minute are Patron `[assumed]` tuning constants (Q3), declared as `const double` in the scorer with no new options.
- Existing root `CONTEXT.md` Recommendation/Reason/genre entries remain canonical; no glossary edit (brief Q8).
- Phase B remains gated on Epic 7 (`specs/PRODUCT.md`) plus merged DEV-336 and DEV-337 and landed-signature recon (brief Q6). Specifying in Phase A does not authorize Phase B build or stubs.
- Exact landed GenreWeights/MovieFeatures call shapes are Phase B recon facts, not Quill inventions (brief task ordering).
- No owner structural checkbox (brief Q8–Q9).
- Exclusions in brief frozen scope remain out of scope.
