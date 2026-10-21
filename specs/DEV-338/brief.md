# DEV-338 decision brief

## Status and evidence

Phase 2 step 1 grill complete: Patron confirmed shared understanding after nine questions (Q9 final precision correction; Q8 loop ratification). No owner structural checkbox. This is a planning decision receipt, not Gate 1 approval, plan freeze, implementation, or gate evidence.

- Worktree: `F:/Dev/LamuFlix.worktrees/feature-338-spec`.
- Branch: `feature/338-spec`.
- Base HEAD: `06ec67774a5a957369780bf7d431f6d4ca3eb900`.
- Ticket: DEV-338, parent DEV-288, size:M, no ui tag.
- Authoritative ticket Scope & Technical Design and AC: `recon-DEV-338` section 10, especially lines 108-123. Recon sections 3-8 provide existing types, conventions, file placement, and dependency absence evidence.
- Full append-only questions, answers, citations, rationale, and implications: `CONCLUSIONS.md` Q1-Q9. Taste/tuning choices: `ASSUMPTIONS.md`.
- Patron decisions use the ticket, constitution, PRODUCT, and charter citations preserved verbatim in CONCLUSIONS. Facts not in recon require `needs recon: <question>` to Conductor; no speculative searches or silently invented contracts.

## Ticket requirements already decided

Implement a pure function:

`Score(MovieFeatures a, MovieFeatures b, SimilarityWeights weights, GenreWeights genreWeights) -> SimilarityResult(double Score, ImmutableList<Reason> Reasons)`.

Seven terms: director, writers, cast, IDF-weighted genres, exponential year proximity, exponential runtime proximity, and rating proximity `1 - |ratingA-ratingB| / 10`. Normalize using weighted mean over terms with data and clamp to [0,1]. Preserve symmetry, exact identity for any nonempty scoring profile, no side effects/infrastructure dependencies, and nonempty caller-safe Reasons whenever score is positive. Ticket example labels are examples rather than mandated copy. Existing `RatingBoost` is the seventh weight, applied to rating proximity, never an additive boost. Do not rename or add weights.

## Settled decisions, all grill answers

1. **Q1: zero denominator.** If no positive applicable weight remains, return exactly 1 with one `Identical feature profile` Reason only when all seven scoring fields are identical and at least one scoring field is present. Otherwise return 0 and empty Reasons. Patron rules this defines the formula's indeterminate 0/0 boundary rather than changing the ticket.
2. **Q2: eligibility and overlap.** Set terms have data only when both sets are nonempty; scalars require both values. Missing on either side excludes the term from numerator and denominator. Nonempty disjoint sets remain eligible with similarity 0 and retain their weight. Directors/Writers/Cast use Jaccard intersection/union under MovieFeatures OrdinalIgnoreCase semantics. Genres delegate weighted Jaccard and unknown-genre/default-1 handling to the landed DEV-337 GenreWeights contract; no second genre formula. No eligible terms returns 0/empty outside Q1.
3. **Q3: numerical rules.** Fixed `const double` year k=0.1/year and runtime k=0.01/minute. Cast scalar values to double before absolute difference. Use `Math.Exp(-k * distance)` and the ticket rating formula. Divide eligible weights by their maximum before accumulating numerator/denominator; handle maximum zero before dividing, then clamp final result [0,1]. Finite MaxValue weights must not overflow. These constants do not add configuration or change gate thresholds. Normal Reasons require eligibility, w>0, and s>0.
4. **Q4: types and Reasons.** Public static SimilarityScorer and public sealed records `Reason(string Text)` and `SimilarityResult(double Score, ImmutableList<Reason> Reasons)` in `LamuFlix.Core.Features.Recommendations`. Emit one Reason per positive weighted term, fixed order Director, Writers, Cast, Genres, YearProximity, RuntimeProximity, RatingBoost. Shared set tokens only: choose ordinal-min spelling across both inputs for each case-insensitive match, then sort OrdinalIgnoreCase with Ordinal tie-break. Replace control characters with spaces, trim, omit empty sanitized tokens, and use Q9 fallback if none remain. Preserve human feature labels as plain text; never include movie IDs, internal keys, file paths, or diagnostics. Callers escape plain text for their rendering context; no HTML/Markdown generation or UI changes here.
5. **Q5: guards and identity.** ArgumentNullException naming a/b/weights/genreWeights for null arguments. ArgumentOutOfRangeException naming weights for any negative or nonfinite weight, including NaN and either infinity. Finite zero through MaxValue is valid. Seven-field profile equality uses null-safe case-insensitive set equality plus exact scalar values; ignore MetaScore, FullPlot, IDs, and reference equality. Both fully empty scoring profiles return 0. If any eligible positive-weight term exists and every such term equals 1, return exactly 1.0. No changes to existing options or value objects.
6. **Q6: delivery boundary.** Exact approved files and sequence below. Pure static implementation; no interface/service/DI/cache. Phase A can finish before dependencies exist; Phase B is gated on the PRODUCT Epic 7 prerequisite plus merged DEV-336/DEV-337 implementations and landed-signature recon. No predecessor code or placeholder types in DEV-338.
7. **Q7: verification.** Existing xUnit v3, Shouldly, FsCheck in the unit project; focused seven-term examples plus invariant properties and full Phase B M gates. No infrastructure, IO, mocks, Testcontainers, new packages, or property-test opt-out. Q9 supersedes the accidental Q7 `iff` wording.
8. **Q8: loop terms.** Closing bar, frozen scope, cap, and exclusions below are ratified. Zero owner checkboxes. Existing CONTEXT recommendation/Reason/genre entries suffice; no root glossary modification and no ADR for this reversible pure function.
9. **Q9: precision.** Required property is one-way `Score > 0 => Reasons nonempty`, not biconditional. Emit per-term Reasons whenever eligible w>0 and s>0 even if scaled arithmetic underflows the final score to 0. Q1 zero-denominator nonidentity/no-contribution cases still return empty Reasons. Generic fallbacks: `Shared director`, `Shared writers`, `Shared cast`, `Shared genres`.

Normal label templates (Patron-approved taste assumptions): `Same director: <names>`, `Shared writers: <names>`, `Features <names>`, `Shares <genres>`, `Similar release years`, `Similar runtime`, `Similar rating`. The fixed term order is separate from token sorting. Q1 fallback is the sole synthetic identity Reason. Sanitization changes display only, never scoring inputs or set membership.

Vocabulary: Similarity score is normalized pairwise likeness; Reason is a caller-facing plain-text explanation of a positive weighted term, plus the Q1 identity boundary explanation. Existing CONTEXT glossary remains canonical.

## Approach and approved file set

Build only these new production files:

- `src/LamuFlix.Core/Features/Recommendations/SimilarityScorer.cs`
- `src/LamuFlix.Core/Features/Recommendations/SimilarityResult.cs`
- `src/LamuFlix.Core/Features/Recommendations/Reason.cs`

Build only these new test files:

- `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/SimilarityScorerPropertyTests.cs`
- Optional `tests/LamuFlix.UnitTests/Features/Recommendations/ReasonTests.cs` if focused Reason coverage merits separation.

This Phase A writes only `specs/DEV-338/` decisions and Quill's later spec/plan/tasks artifacts. Do not change production, tests, dependencies, projects, options/configuration, schema, HTTP/OpenAPI/generated TS, web, playback, or other tracked source files during planning. No §2.3 care-item change is approved beyond the ticket-named scorer/result contract and the focused new files above.

## Task ordering constraints for Quill

1. Phase B pickup: Wisp reconfirms dependency delivery and actual MovieFeatures/GenreWeights contracts against main; Gauge owns baseline gates; Keel handles spec drift analysis. Do not ask the Builder to search for missing contracts or substitute predecessor implementations. Missing dependencies block build, not this Phase A spec.
2. Result and Reason records plus scorer argument boundaries and set overlap.
3. Scalar proximity, rating, stable weighted normalization, zero-denominator/identity boundaries.
4. Deterministic safe Reasons, including fallback behavior and symmetric spelling.
5. Focused examples and tagged FsCheck properties alongside production phases; all relevant tests pass before handoff. No required new acceptance/Gherkin suite.
6. Whole-diff refactor by Cog and Gauge M gates, followed by all three review axes through the Build workspace pipeline.

Quill turns these decisions into dependency-ordered phases/tasks; no unspecified design choice. A missing decision is `needs decision: <question>` to Keel. Exact landed dependency method names must come from Phase B recon; do not invent them from conceptual signatures.

## Test strategy

Unit examples cover isolated terms, known Jaccard ratios, delegated genre rarity/unknown defaults, known exponential distances, rating boundaries, missing-side exclusion versus disjoint zero inclusion, mixed/zero weights, zero-denominator identity versus mismatch, all-empty and nonempty identical profiles, exact 1 returns, null and invalid-weight guards, finite MaxValue stabilization, and tiny-weight underflow behavior. Cover Reasons separately: term eligibility, stable order, case-variant spelling under swap, preserved null/blank feature tokens, control characters, adversarial plain text, generic fallbacks, and exclusion of non-feature/internal fields. Scoring preserves raw feature membership; sanitize display only.

FsCheck properties (tagged for `run-property-tests.ps1`): finite [0,1] score, score symmetry with absolute tolerance 1e-12, exact nonempty-profile self score 1, deterministic and swap-identical Reasons, positive score implies nonempty Reasons, and unchanged input snapshots. Validate emission independently rather than imposing the superseded iff invariant. Existing VO construction rules still apply to generated inputs. Use approved assertion/mocking libraries only; no new IO or test infrastructure.

## Gate expectations

Phase A runs no build/test/analyzer/mutation gates and makes no runtime claim. Later size:M delivery requires configured Phase 3 step 5 gates: Roslyn, complexity, JetBrains changed-diff inspection without -All, property tests, vulnerable packages, format verification, dotnet test, mutation on changed Core production, and web gate reporting its actual applicability. Thresholds come from harness.yml; never lower or hardcode gate thresholds. Property tests are applicable with no opt-out. Mutation must use the repository script without Stryker since, and its own eligible/scope results. Exact exit-2 `SKIPPED (scope-empty)`, configured `SKIP`, or mutation `NOT APPLICABLE` remain distinct from PASS. Exit 1 or Could not run blocks. Review evidence covers its pinned current head only.

## Loop discipline

- Closing bar: in-scope Critical/High block. Medium/Low are recorded nonblocking unless they demonstrate a failed explicit AC, classified by actual impact. No gate failure is waived by review severity.
- Frozen scope: the ticket's pure seven-term scorer, immutable result and Reason records, deterministic caller-safe explanations, Q1-Q9 math/input boundaries, focused unit/property tests, and approved file set. Exclude recommendation orchestration, candidates, storage, caching, endpoints, UI chips, configuration edits, predecessor implementations, tuning studies, dependencies, schema, and unrelated cleanup; anything else is a follow-up issue, not a finding in this round.
- Round cap: two plan-challenge rounds; two build review/remediation rounds, at most two fix commits per round. Unresolved above-bar findings at cap block; no third round, lowered threshold, or silent scope growth.
- M requires Sentry, Ledger, and Compass; missing axis blocks adjudication. Follow charter follow-up policy for excluded concerns.

## Handoff

Quill drafts spec.md, plan.md, and tasks.md from this brief, full CONCLUSIONS, recon-DEV-338, and PRODUCT. Keel later runs read-only speckit-analyze and checks every plan/task decision against this brief. Plan challenge, freeze, provisional Gate 1, spec PR, and user merge remain later steps owned by their assigned seats. No spec PR merge or Gate 1 approval is implied by grill completion.


## Phase B amendment — T001b decision record (2026-10-21)

Authority: CONCLUSIONS.md, Phase B Intake Rulings (2026-10-20), committed in `6490fb794bb6bf702078a505d0cc0e088eb86e3a`. This later cited Patron ruling supersedes Q6/Q7/Q8 exclusions only for the contract alignment and file set below. The Phase A exchanges above remain historical records.

- Epic 7 is satisfied by the owner merge/active-chain ruling; DEV-338 is approved to build subject to the remaining Phase 1 receipts. DEV-336 and DEV-337 are landed (`recon-DEV-338-2:9-14`).
- Replace the landed integer-ID genre contract with case-insensitive string genre names: `GenreCounts.MoviesWithGenre` is `IReadOnlyDictionary<string, long>`; `GenreWeights.GetWeight(string genre)` and `Similarity(IEnumerable<string> first, IEnumerable<string> second)` use `StringComparer.OrdinalIgnoreCase`. Persistence counts select `g.name AS "Genre"` instead of the database ID. Keep the existing IDF/Jaccard formula, unknown/default weight 1.0, and empty-union behavior.
- Delegate directly with `genreWeights.Similarity(a.Genres, b.Genres)`; the four-argument scorer signature stays fixed. Follow the ruling safe-handling alternative: preserve raw MovieFeatures null/blank membership and make GenreWeights handle runtime-null members safely. Do not filter scoring membership or sanitize scoring tokens; display sanitization stays separate.

Additional existing production files approved by Patron:

- `src/LamuFlix.Core/Ports/GenreCounts.cs`
- `src/LamuFlix.Core/Domain/GenreWeights.cs`
- `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs`

Additional existing test files approved for forced contract alignment and focused regression proof:

- `tests/LamuFlix.UnitTests/Domain/GenreWeightsTests.cs`
- `tests/LamuFlix.UnitTests/Domain/GenreWeightsCacheTests.cs`
- `tests/LamuFlix.UnitTests/Features/Recommendations/GenreWeightsCalculatorTests.cs`
- `tests/LamuFlix.UnitTests/Persistence/EfMovieCatalogSurvivorTests.cs`
- `tests/LamuFlix.UnitTests/Persistence/EfMovieRepositorySurvivorTests.cs`
- `tests/LamuFlix.UnitTests/Persistence/MovieCatalogRegistrationTests.cs`
- `tests/LamuFlix.IntegrationTests/EfMovieCatalogFacetsTests.cs`
- `tests/LamuFlix.IntegrationTests/GenreWeightsIntegrationTests.cs`
- `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs`

- Delivery order: complete Phase 1 receipts, then align the genre count/weight/persistence contract and its existing tests before scorer overlap/delegation. Unit coverage retains defaults, guards, case-insensitive matching, null/blank safety, and weighted behavior; existing real-Postgres integration coverage proves name-keyed counts, case variants, and end-to-end calculator compatibility. No database-driver mocks. This narrowly supersedes the original no-IO/Testcontainers instruction for the named existing integration tests.
- Run the configured M gates over the amended Core and Infrastructure diff and the relevant unit/integration suites; mutation applicability includes both changed production projects according to harness.yml. Thresholds, round caps, and all-three-axis review requirements remain as frozen.
- No schema/migration, new package, project, port, catalog access from the scorer, auxiliary bridge parameter, or unrelated cleanup is authorized. The predecessor-edit prohibition in spec.md FR-011 is superseded only for GenreWeights and the explicitly named alignment files by this later Patron ruling; MovieFeatures and options remain excluded.

T001b verdict: **CLEAR — decision-resolved drift** at pickup HEAD `6490fb794bb6bf702078a505d0cc0e088eb86e3a`, with this amendment mirrored into plan.md/tasks.md. The incompatible landed int-ID shape remains the starting implementation fact, not the target contract. T001a is independently owned by Gauge; this verdict makes no baseline, runtime, gate, review, or merge claim.
