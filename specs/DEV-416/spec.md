# Feature Specification: DEV-416 Apply Maximal Marginal Relevance diversification to blended recommendations

**Feature Branch**: `feature/416-spec`

**Created**: 2026-10-24

**Status**: Draft — gate1: provisional

**Input**: Ticket DEV-416 (size M, parent DEV-288, no ui tag); settled grill brief `specs/DEV-416/brief.md` (grill closed by Patron Q12; Q1–Q12); facts from note `recon-DEV-416`; Patron rulings in `specs/DEV-416/CONCLUSIONS.md`; product authority in `specs/PRODUCT.md` sections 1–3 and 5. No decisions beyond the brief and those rulings. No `design.md` (no ui tag). No clarify/checklist or ADR on this M path.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Deterministic MMR selection over the scored pool (Priority: P1)

Given a complete list of uniquely identified scored candidates, the system iteratively selects items by maximizing `lambda * relevance - (1 - lambda) * maximum similarity to already-selected items`, returns ordered movie IDs of size `min(Take, candidate count)`, breaks exact objective ties by ascending movie ID, and rejects invalid inputs before any empty-result shortcut.

**Why this priority**: Ticket Scope items 1–3 and AC require MMR diversification with deterministic ties and correct output size; Q2–Q4 and Q10–Q11 freeze the selector contract and incremental redundancy maxima.

**Independent Test**: Unit tests on pure `MmrSelector` cover every invalid input category, valid empty/zero/oversized Take, lambda endpoints, exact-ID ties across permutations, missing-feature/zero-weight reuse of DEV-338, earlier-selected maximum redundancy, call isolation, and cross-Take prefix consistency; bounded FsCheck proves count, uniqueness, determinism, permutation invariance, and equivalence to an independent full-rescan reference.

**Acceptance Scenarios**:

1. **Given** a valid non-empty candidate pool and positive Take, **When** MMR selection runs, **Then** the result contains `min(Take, candidate count)` unique movie IDs from the input pool in selection order.
2. **Given** two or more candidates with equal marginal objectives, **When** selection chooses among them, **Then** the smaller `Movie.Id.Value` wins, with no epsilon or rounding.
3. **Given** lambda = 1, **When** selection runs, **Then** order is relevance descending then ascending movie ID.
4. **Given** lambda = 0, **When** selection runs, **Then** the first pick is the smallest movie ID and subsequent picks minimize maximum redundancy with ID ties.
5. **Given** invalid inputs (null list/items/features/weights, duplicate MovieIds, negative Take, non-finite or out-of-[0,1] lambda, negative or non-finite relevance), **When** selection is invoked, **Then** an argument exception is thrown before empty-result shortcuts.
6. **Given** a valid empty pool or Take = 0, **When** selection runs, **Then** the result is empty; oversized Take returns the entire pool.
7. **Given** identical valid inputs, **When** selection runs repeatedly or after interleaved calls with different valid lambda/Take, **Then** each retained prior result stays unchanged and repeated identical calls return the same order.
8. **Given** identical valid pool, features, weights, and lambda with `0 <= a <= b`, **When** `Select(pool, a)` and `Select(pool, b)` run, **Then** the shorter result equals the prefix of length `min(a, pool.Count)` of the longer result.

---

### User Story 2 - Sequel-chain fixture shows diversified top results (Priority: P1)

A deterministic in-memory fixture with a six-sequel franchise chain and five genre-disjoint outsiders proves that MMR at lambda 0.7 and Take 10 does not return five consecutive sequels at the top, while a relevance-only baseline does start with five sequels.

**Why this priority**: Ticket Overview and AC require that a sequel-chain fixture’s top-10 includes at least one movie from outside that franchise; Q5 freezes the fixture shape and expected order.

**Independent Test**: Selector-only sequel-chain tests use the real DEV-338 scorer (no mock), genre-only weights, and the Q5 candidate set; assert relevance baseline first five are sequels; assert MMR order `0.90 sequel`, then five outsiders ascending by MovieId, then sequels `0.89`–`0.86`; assert ten unique IDs, at least one outsider in top ten, and first five not all sequels.

**Acceptance Scenarios**:

1. **Given** the Q5 fixture (six sequels relevances 0.90…0.85 sharing a nonempty genre set; five outsiders at relevance 0.80 with mutually disjoint nonempty genre sets disjoint from the chain; Take = 10; lambda = 0.7; genre-only SimilarityWeights; valid uniform GenreWeights), **When** relevance-only ranking is computed, **Then** the first five IDs are sequels.
2. **Given** the same fixture, **When** MMR selection runs with the real DEV-338 scorer, **Then** the order is the 0.90 sequel, all five outsiders in ascending MovieId order, then sequels 0.89, 0.88, 0.87, and 0.86.
3. **Given** that MMR result, **When** acceptance checks run, **Then** there are ten unique IDs, at least one outsider appears in the top ten, and the first five are not all sequels.
4. **Given** selection step two under that fixture, **When** marginal objectives are compared, **Then** outsiders score 0.56 versus 0.323 for the next sequel.

---

### User Story 3 - Handler diversifies before Take and preserves payloads (Priority: P1)

`GetRecommendationsQueryHandler` runs MMR over the entire eligible scored pool before Take using configured `RecommendationOptions.MmrLambda`, maps selected IDs back to original Movie, blended Score, and caller-safe Reasons without replacing those fields, and retains selector order even when original Scores are not descending.

**Why this priority**: Ticket Scope places MMR on the blended ranking; Q2 and Q6 require pre-Take diversification, lambda sensitivity, and payload preservation; Q11 requires non-monotonic score order retention.

**Independent Test**: Handler unit tests use a pool larger than Take with a diversifying item beyond the relevance-only cutoff; prove configured MmrLambda sensitivity; prove original Movie/Score/Reasons preserved; assert non-monotonic-score order retention; update existing ordering expectations only where they conflict with ratified MMR.

**Acceptance Scenarios**:

1. **Given** an eligible scored pool larger than Take that includes a diversifying item beyond the relevance-only cutoff, **When** recommendations are produced, **Then** MMR runs over the full scored pool before Take and the diversifying item can appear inside the truncated result.
2. **Given** two configured MmrLambda values that change selection, **When** the handler runs on the same pool, **Then** the ordered results differ according to that lambda (sensitivity).
3. **Given** any successful diversified result, **When** each recommended item is inspected, **Then** Movie, blended Score, and caller-safe Reasons match the original scored payloads for those IDs; the marginal objective never replaces Score or Reasons.
4. **Given** a pool whose MMR order is not strictly descending by blended Score, **When** the handler maps selector output, **Then** recommendation order follows selector order, not a post-sort by Score.
5. **Given** RecommendedMovie and OpenAPI field shapes, **When** this feature lands, **Then** those public shapes remain unchanged.

### Edge Cases

- Redundancy is zero before the first selection (Q2).
- Pairwise similarity uses `SimilarityScorer.Score(candidateFeatures, selectedFeatures, options.Weights, genreWeights).Score` with `MovieFeaturesMapper.From` supplying candidate features; DEV-338 missing-feature and zero-weight behavior are reused; pairwise Reasons are discarded (Q2, Q4).
- No embedding term in pairwise redundancy, no hard franchise quota, and no forced outsider insertion (Q2).
- Relevance has no upper bound; values above 1 are valid when finite and nonnegative (Q3).
- Duplicate MovieIds are rejected rather than silently deduplicated (Q3).
- Caller candidate list, items, features, and weight inputs are read-only for the selector; selection bookkeeping is invocation-owned (Q10).
- Per-call feature projection and O(n) incremental numeric redundancy maxima only; no pairwise matrix and no cross-request/static mutable cache (Q4).
- Franchise membership in the sequel fixture is a test-only ID grouping with no production metadata field (Q5).
- The sequel fixture asserts neither empirical franchise detection nor a universal diversity guarantee (Q5).
- Existing `RecommendationOptions`, `MovieFeaturesMapper`, `SimilarityScorer`, and `BlendScorer` remain unchanged; `MmrLambda` default 0.7 already exists (Q7; recon-DEV-416:9).
- No DTO, endpoint, OpenAPI/generated TypeScript, web, schema/persistence, package/project/harness, playback, LocalPlay, secrets, `Process.Start`, unrelated-file, CONTEXT.md, or ASSUMPTIONS.md edits; no deletions or whole-file rewrites (Q7, Q12).
- No new interface, DI service, repository, architectural layer, or dependency (Q3).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Core MUST add pure static `MmrSelector` and sealed record `MmrCandidate(MovieId MovieId, double Relevance, MovieFeatures Features)` under `src/LamuFlix.Core/Features/Recommendations/`. `Select` MUST accept the complete candidate list, Take, lambda, `SimilarityWeights`, and `GenreWeights`, and MUST return ordered `MovieId`s. No interface, DI registration, repository, architectural layer, or new dependency (ticket Scope item 1; Q3).
- **FR-002**: Selection MUST iteratively maximize `lambda * relevance - (1 - lambda) * maximum similarity to already-selected candidates`, with redundancy zero before the first selection. Relevance MUST be the original blended `Result.Score`. Pairwise similarity MUST be `SimilarityScorer.Score(...).Score` with features from `MovieFeaturesMapper.From`. Exact equal objectives MUST tie by ascending `Movie.Id.Value` without epsilon or rounding (ticket Scope items 1–2; Q2).
- **FR-003**: Output size MUST be `min(Take, candidate count)`. Valid empty pools and Take = 0 MUST return empty; oversized Take MUST return the entire pool (ticket Scope item 3; Q3).
- **FR-004**: Full input validation MUST run before empty-result shortcuts. The selector MUST reject null list/items/features/weight inputs, duplicate MovieIds, negative Take, non-finite lambda or lambda outside inclusive [0, 1], and negative or non-finite relevance with argument exceptions. Relevance has no upper bound. Weight validation MUST reuse the identical DEV-338 SimilarityWeights policy, implemented privately in `MmrSelector`: reject each of the seven properties (Director, Writers, Cast, Genres, YearProximity, RuntimeProximity, RatingBoost) when < 0.0 or !double.IsFinite with `ArgumentOutOfRangeException` before empty shortcuts; no scorer visibility edit, reflection, or dummy Score call; `SimilarityScorer` remains unchanged. Duplicate IDs MUST be rejected, not silently deduplicated (Q3; A1).
- **FR-005**: Per handler invocation, each scored candidate MUST be projected once via `MovieFeaturesMapper.From` and retained for that call. The selector MUST keep one numeric maximum redundancy per unselected candidate (initially zero) and, after each selection while further selections remain, update remaining candidates against the newly selected item using the maximum over all prior selections. Pairwise Reasons MUST be discarded. Work bound is O(n*k) with auxiliary O(n) for n candidates and k = min(Take, n). No full pairwise matrix and no cross-request/static mutable cache (Q4).
- **FR-006**: `GetRecommendationsQueryHandler` MUST replace only the current ranking/truncation stage: run MMR over the entire eligible scored pool before Take using `RecommendationOptions.MmrLambda` (default 0.7 already present), then map selected IDs to original Movie, blended Score, and caller-safe Reasons. The marginal objective MUST NOT replace Score or Reasons. Selector order MUST be retained even when original Scores are not descending. `RecommendedMovie` and OpenAPI field shapes MUST remain unchanged (ticket Scope; Q2, Q6, Q11; recon-DEV-416:15,97-101).
- **FR-007**: Caller candidate list, items, features, and both weight inputs MUST be treated as read-only. Selection bookkeeping and output sequence are invocation-owned. A retained earlier result MUST stay unchanged after later calls with different valid lambda or Take (Q10).
- **FR-008**: For identical valid pool, stable features/weights, and lambda, with `0 <= a <= b`, `Select(pool, a)` MUST equal the first `min(a, pool.Count)` IDs of `Select(pool, b)`. Take only stops iterations: no pool pre-truncation, Take-based normalization, or post-sorting (Q11).
- **FR-009**: Proof MUST include focused selector tests for every Q3 invalid category and valid empty/zero/oversized Take; relevance above 1; lambda endpoints; exact-ID ties across permutations; real DEV-338 missing-feature/zero-weight behavior; a fixture with at least three selections where an earlier selected item supplies the decisive maximum; Q10 call-isolation checks; bounded FsCheck properties (count, unique input-subset IDs, repeated-call determinism, permutation invariance including all-equal objectives, equivalence to an independent test-only full-rescan reference using the real scorer, and cross-Take prefix including zero/oversized Takes); and the Q5 sequel-chain fixture with real scorer. The full-rescan reference MUST recompute the maximum over all selected items and MUST NOT copy production incremental state (ticket tests; Q5–Q6, Q10–Q11).
- **FR-010**: Handler tests MUST use a pool larger than Take with a diversifying item beyond the relevance-only cutoff, prove configured MmrLambda sensitivity, preserve original Movie/Score/Reasons, and assert non-monotonic-score order retention. Existing ordering expectations MAY be updated only where they conflict with ratified MMR. Reuse existing unit/property conventions; no new dependency, database fixture, or scorer mock. No benchmark, instrumentation seam, or exact scorer-call-count assertion is required (Q6, Q11).
- **FR-011**: Implementation file inventory MUST be exactly: add `MmrCandidate.cs`, add `MmrSelector.cs`, narrowly modify `GetRecommendationsQueryHandler.cs`; add `MmrSelectorTests.cs`, `MmrSelectorPropertyTests.cs`, `MmrSelectorSequelChainTests.cs`; modify `GetRecommendationsQueryHandlerTests.cs` and `GetRecommendationsQueryHandlerPropertyTests.cs` only as required. Fixture builders and the full-rescan reference stay test-local. Existing `RecommendationOptions`, `MovieFeaturesMapper`, `SimilarityScorer`, and `BlendScorer` remain unchanged. No DTO/endpoint/OpenAPI/web/schema/package/harness/playback/unrelated-file/CONTEXT.md edits; no deletions or whole-file rewrites (Q7).
- **FR-012**: Later Build MUST earn applicable configured harness gates (Roslyn; cyclomatic complexity ≤15 build and ≤6 refactor; InspectCode in changed scope without `-All`; mutation via `run-mutation.ps1` with since disabled; property tests; vulnerable packages; `dotnet format --verify-no-changes`; `dotnet test`; web gates with expected scope-empty). Mutation and property tests MUST execute and exit 0: no property opt-out or threshold lowering. Surviving mutants route to missing-test remediation. Exit 1 or Could not run blocks. Scope-empty exit 2 is non-blocking SKIPPED (scope-empty), never PASS. Harness SKIP and mutation N/A stay separately labeled and never count as executed mutation proof (Q8).
- **FR-013**: The system MUST NOT add a new NuGet/npm dependency; MUST NOT change public HTTP/OpenAPI/generated TypeScript/web; MUST NOT add schema/migration; MUST NOT touch playback execution, LocalPlay, secrets, or `Process.Start`; MUST NOT add a new port/project/top-level folder/ADR/clarify/checklist; MUST NOT rewrite CONTEXT.md or ASSUMPTIONS.md; MUST NOT invent a UI taste assumption (Q7, Q12; PRODUCT §5).

### Key Entities

- **MmrCandidate**: Selector input carrying MovieId, relevance (original blended score), and MovieFeatures for pairwise redundancy.
- **MMR selection result**: Ordered sequence of MovieIds of length `min(Take, candidate count)`.
- **Pairwise redundancy**: Maximum DEV-338 structural similarity of a candidate to already-selected items; zero before the first selection.
- **Sequel-chain fixture**: Test-only eleven-candidate pool (six sequels, five outsiders) used to prove diversification at lambda 0.7 and Take 10.
- **Recommended movie payload**: Existing Movie plus blended Score and caller-safe Reasons; unchanged by the marginal objective.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For the Q5 sequel-chain fixture, MMR top-10 includes at least one outsider and the first five results are not all sequels, while the relevance-only baseline starts with five sequels.
- **SC-002**: Every MMR result has no duplicate movies and size equal to `min(Take, candidate count)`.
- **SC-003**: Repeated runs on the same valid inputs return the same order; exact objective ties resolve by ascending movie ID.
- **SC-004**: Bounded FsCheck properties for count, uniqueness, determinism, permutation invariance, full-rescan equivalence, and cross-Take prefix all hold on valid generated inputs.
- **SC-005**: Handler diversification uses configured MmrLambda over the full scored pool before Take and preserves original Movie, Score, and Reasons in selector order.
- **SC-006**: Applicable configured gates run without lowered thresholds; skipped/opt-out/N/A/could-not-run verdicts are never recorded as PASS.

## Assumptions

- No `[assumed]` taste items were recorded for this ticket; there is no `ASSUMPTIONS.md`, and none is created here (brief Q12).
- No owner structural checkbox is open. Anything not decided in `brief.md` / `CONCLUSIONS.md` is a `needs decision:` item, not an assumption invented here.
- Facts for existing symbols (handler insertion point, SimilarityScorer signature, MmrLambda default, duplicate-candidate structural guarantee) come from `recon-DEV-416`; proposed new types/files are approved design decisions in the brief, not claims those seams already exist.
- Upstream candidate uniqueness remains a structural source guarantee; the selector still rejects duplicate IDs if presented (Q3; recon-DEV-416:108-112).
