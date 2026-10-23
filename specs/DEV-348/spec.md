# Feature Specification: DEV-348 Postgres pgvector recommendation candidate source

**Feature Branch**: `feature/DEV-348` (Phase B reconciliation). Historical Phase A drafting used `feature/348-spec`; that name is retained only as history and is not current authority.

**Created**: 2026-10-21

**Status**: Draft — Phase B document reconciliation (2026-10-23) on `feature/DEV-348` after owner unblock and Patron amended-calibration ruling (`CONCLUSIONS.md:204-211`; brief.md:347-358). Not Gate 1, not implementation clearance, and not a claim that prerequisites, receipts, tests, or gates have passed.

**Input**: Ticket DEV-348 (size M, parent DEV-288, no ui tag); settled grill brief `specs/DEV-348/brief.md` (Q1–Q11 closed; calibration portions superseded by brief.md:347-358); facts from note `recon-DEV-348`; Patron rulings in `specs/DEV-348/CONCLUSIONS.md` including the 2026-10-23 amendment at lines 204-211; taste receipt `specs/DEV-348/ASSUMPTIONS.md`; `specs/PRODUCT.md`. No decisions beyond `brief.md` and that cited ruling.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Return nearest semantic neighbours with cosine similarity (Priority: P1)

When recommendations are requested for a movie that has a usable stored feature vector, the candidate source returns at most `CandidatePoolSize` other enriched movies nearest by pgvector cosine distance, each paired with its cosine similarity. Ordering is distance ascending then movie id ascending on the server before domain hydration. Final recommendation ranking by weighted metadata score remains the existing handler contract (DEV-354 owns reranking).

**Why this priority**: Ticket Acceptance Criteria require at most `CandidatePoolSize` candidates ordered by distance, with candidates carrying similarity (canvas note DEV-348 Tracker summary; brief Frozen scope).

**Independent Test**: Real PostgreSQL+pgvector Testcontainers tests on `EfRecommendationCandidateSource` prove default-50 and custom caps with excess eligible rows, distance order opposite id order, equal-distance id ties, accurate `1.0 - distance` conversion clamped to finite `[-1, 1]`, enriched-only and self-exclusion, four-collection hydration, AsNoTracking, and at most two reader commands (`recon-DEV-348:15-18,30`; brief Test strategy / Q5–Q7).

**Acceptance Scenarios**:

1. **Given** a selected movie with a usable stored `movie_features` vector and more than `CandidatePoolSize` other enriched movies with usable vectors, **When** `GetCandidatesAsync` runs, **Then** it returns at most `options.CandidatePoolSize` (default 50) `RecommendationCandidate` values ordered by cosine distance ascending then movie id ascending, each with finite `CosineSimilarity` in `[-1, 1]` equal to `1.0 - distance` (endpoint clamp for floating-point tolerance only).
2. **Given** known stored vectors whose distance order is deliberately opposite movie-id order, **When** candidates are loaded, **Then** the returned order follows server distance (then id), not client id order.
3. **Given** two candidates with equal cosine distance, **When** results are returned, **Then** ascending movie id breaks the tie (ASSUMPTIONS equal-distance tie-breaker).
4. **Given** a populated eligible pool, **When** the adapter runs, **Then** it uses at most two sequential read-only async reader commands (selected-vector lookup, then one server-bounded candidate query with the four existing collection includes and complete `ToDomain` hydration), propagates `CancellationToken`, and does not N+1.

---

### User Story 2 - Empty success when the selected feature vector is missing or unusable (Priority: P2)

When the selected movie has no `movie_features` row or no usable stored vector, the candidate source returns an empty list without throwing. Candidates with missing, null, or zero-norm vectors, or non-finite cosine distances, are excluded before the pool cap and do not consume slots.

**Why this priority**: Ticket Acceptance Criteria require a missing `movie_features` row to return an empty list without throw (canvas note DEV-348 Tracker summary; brief Q5).

**Independent Test**: Integration tests prove missing selected row / unusable selected vector → empty after exactly one reader command without throw; candidate missing/unusable vectors excluded before the cap; cancellation and database faults still propagate as faults (`recon-DEV-348:30`; brief Test strategy / Q5–Q6).

**Acceptance Scenarios**:

1. **Given** a selected movie with no `movie_features` row or an unusable (null / zero-norm) selected vector, **When** `GetCandidatesAsync` runs, **Then** it returns an empty list without throwing after one reader command and does not generate a request-time embedding.
2. **Given** eligible enriched movies some of which lack usable stored vectors, **When** the candidate query runs, **Then** those candidates are excluded before `CandidatePoolSize` is applied.
3. **Given** cancellation or a database fault during either read, **When** the operation fails, **Then** the failure propagates normally and is not disguised as missing-vector empty success.

---

### User Story 3 - Emit "Similar story" only above the calibrated cosine threshold (Priority: P3)

When the handler scores candidates, it preserves weighted metadata `SimilarityResult.Score`, existing feature reasons (including zero-weight identity), and final order by score descending then movie id ascending with `Take(query.Take)`. It appends exactly one `Reason("Similar story")` after those feature reasons if and only if `candidate.CosineSimilarity` is strictly greater than `options.SimilarStoryThreshold`. Equality emits none. Candidates at or below the threshold remain eligible for ordinary scoring and selection.

**Why this priority**: Ticket Acceptance Criteria require "Similar story" only above the configurable threshold; the calibration source is the Patron-amended synthetic set (canvas note DEV-348 Tracker summary; brief.md:347-358; `CONCLUSIONS.md:204-211` superseding DEV-342 semantic calibration).

**Independent Test**: Handler unit and FsCheck property tests prove strict `>`, equality emits none, threshold overrides, the named synthetic cosine set (0.8 and 0.6 above 0.5; 0.4, 0.0, and -1.0 at/below 0.5), independence from metadata weights, retention at/below threshold, unchanged weighted score/order/`query.Take`, and feature-reason prefix plus optional single story suffix. Options tests reject NaN/infinities/out-of-range values, accept endpoints `-1` and `1`, exercise `IStartupValidator` via `AddLamuFlixOptions`, and assert default `0.5` (`recon-DEV-348:28,31,57-94`; brief.md:347-358; `CONCLUSIONS.md:210-211`).

**Acceptance Scenarios**:

1. **Given** a candidate whose `CosineSimilarity` is strictly above `SimilarStoryThreshold`, **When** the handler builds `RecommendedMovie`, **Then** reasons are the existing feature-reason list followed by exactly one `Reason("Similar story")`, and `Score` remains the weighted metadata score.
2. **Given** a candidate whose `CosineSimilarity` equals or is below `SimilarStoryThreshold`, **When** the handler builds `RecommendedMovie`, **Then** no "Similar story" reason is appended and the candidate remains eligible for scoring and final selection.
3. **Given** the configurable default `SimilarStoryThreshold = 0.5` ratified by Patron on deterministic synthetic 384-d pairs (`CONCLUSIONS.md:210-211`), **When** options bind at startup, **Then** `SimilarStoryThreshold` defaults to `0.5` and rejects NaN, infinities, and values outside `[-1, 1]` while accepting endpoints `-1` and `1`.

### Edge Cases

- Cosine similarity is `1.0 -` pgvector cosine distance, finite in `[-1, 1]`, distinct from `SimilarityResult.Score` (normalized weighted metadata similarity in `[0, 1]`) (brief Frozen scope / vocabulary).
- No request-time embedding and no runtime DEV-345 / `IEmbeddingGenerator` dependency; query vector comes only from the selected movie's stored `movie_features` row (brief Frozen scope; `recon-DEV-348:23`).
- No DEV-354 reranking, blended semantic scoring, or MMR in this ticket (brief Frozen scope; ticket Tracker summary).
- No new schema, migration, index, NuGet/npm dependency, project, architectural layer, HTTP route/DTO/OpenAPI/web change, secrets, `Process.Start`, or `Features:LocalPlay` change (brief Frozen scope; `specs/PRODUCT.md` §5). Consume landed DEV-413 entity/column identifiers unchanged after Phase B recon; Quill does not invent those identifiers (brief Q1/Q5/Q8).
- `SimilarStoryThreshold` is configurable, defaults to `0.5`, rejects non-finite and out-of-range values outside `[-1, 1]`, and emits "Similar story" only for strict `>`; equality still emits none (`CONCLUSIONS.md:210-211`; brief.md:351-353). Calibration source is deterministic synthetic 384-d vector pairs with cosine similarities 0.8 and 0.6 above 0.5 and 0.4, 0.0, and -1.0 at/below 0.5 — an engineering default by construction, not semantic calibration of real `all-minilm` embeddings. No invented model provenance or semantic labels. No live Ollama. T004 numeric ratification is satisfied by `CONCLUSIONS.md:210-211`; the T003 synthetic-pair receipt remains a Stage A prerequisite unless separately evidenced (brief.md:354-355). Landed DEV-342 implementation remains a sequencing prerequisite distinct from calibration; no new DEV-342 fixture text, labels, stored embeddings, or semantic model is required (brief.md:356).
- Persistence constructs no `Reason`; only the handler adds the story suffix (brief Q2–Q3).
- Source distance ordering and final handler metadata-score ordering are distinct contracts and do not conflict (brief Q3).
- PRODUCT defers Recommendations / Ollama / pgvector execution until Epic 7 completes (`specs/PRODUCT.md:17`); Phase B build is gated on Epic 7 plus landed DEV-413 and landed DEV-342 implementation (sequencing), plus Stage A receipts including the synthetic calibration receipt (brief Q1; brief.md:354-356).
- Scope-empty gate exit 2 is `SKIPPED (scope-empty)`, non-blocking and never PASS; Exit 1 and Could not run remain blocking (brief Gate expectations). This brief grants no `propertyTests` opt-out.
- Frozen scope ends: anything else is a follow-up issue, not a finding in this round (brief Q9).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Add `public sealed record RecommendationCandidate(Movie Movie, double CosineSimilarity)` in `src/LamuFlix.Core/Features/Recommendations/RecommendationCandidate.cs`. A legitimately computed zero cosine similarity is valid; a sentinel zero is not (brief Q2/Q10).
- **FR-002**: Change existing `IRecommendationCandidateSource.GetCandidatesAsync(MovieId selectedMovieId, CancellationToken ct)` in place to return `Task<IReadOnlyList<RecommendationCandidate>>`. No new port, layer, compatibility overload, or architecture-whitelist edit (brief Q2; `recon-DEV-348:14,29`).
- **FR-003**: Add `public double SimilarStoryThreshold { get; init; }` on `RecommendationOptions` with configurable default `0.5` (T004 satisfied by `CONCLUSIONS.md:210-211`); validate finite and within `[-1, 1]` via existing `IValidatableObject` / `AddLamuFlixOptions` startup validation. Preserve `Take=10`, `CandidatePoolSize=50`, `MmrLambda=0.7`, `Weights`, and `CandidatePoolSize >= Take` (brief.md:351-355; `recon-DEV-348:18-19,89-94`).
- **FR-004**: Implement `EfRecommendationCandidateSource` as a stored-vector bounded query: (1) read selected stored vector asynchronously — missing/unusable → empty after one reader; (2) one `AsNoTracking` candidate query with the four existing collection includes and complete `ToDomain` hydration; preserve enriched-only filtering and selected-id exclusion; require usable candidate vectors; exclude missing/null/zero vectors and non-finite distances before the cap; order on the server by `vector <=> selected` ascending then movie id ascending; apply `options.Value.CandidatePoolSize` on the server before hydration; project `Movie` and `CosineSimilarity` only (`1.0 - distance`, clamp finite to `[-1, 1]` for endpoint tolerance). Maximum two sequential reads on populated pools. Inject `IOptions<RecommendationOptions>` directly. Preserve existing scoped DI registration. No client full-table scan, Take-before-filter, N+1, snapshot transaction wrapper, request-time embedding, or below-threshold pool filtering (brief Q5–Q6; `recon-DEV-348:15-18,30`).
- **FR-005**: In `GetRecommendationsQueryHandler`, unwrap `candidate.Movie` for existing `SimilarityScorer.Score`; preserve weighted `Score`, feature reasons (including zero-weight identity), descending score / ascending movie-id ties, `Take(query.Take)`, selected-movie `NotFound`, and empty-pool short circuit. Append a single `Reason("Similar story")` after all existing feature reasons iff `candidate.CosineSimilarity > options.SimilarStoryThreshold` (strict greater-than; equality emits none; independent of metadata weights). Do not change `SimilarityScorer`, `SimilarityResult`, `Reason`, or `RecommendedMovie` types (brief Q3; `recon-DEV-348:57-85`).
- **FR-006**: Use landed DEV-413 `movie_features` mapping unchanged. Bind concrete entity/column identifiers only from Phase B recon. Translation failure blocks; it never permits client scanning, schema changes, or a new abstraction (brief Q5; `recon-DEV-348:21-22,36`).
- **FR-007**: Before reason implementation, a read-only Stage A calibration task (T003) records the deterministic synthetic 384-d pair set, construction, computed cosines (0.8 and 0.6 above; 0.4, 0.0, and -1.0 at/below), separating midpoint `0.5`, and cited rationale. T004 numeric ratification is already satisfied by `CONCLUSIONS.md:210-211`; Stage A no longer waits for DEV-342 semantic vectors or another numeric ruling. Never invent semantic labels, substitute weighted scores, require live Ollama, or expand the nine-file envelope (brief.md:352-357; `CONCLUSIONS.md:209-211`).
- **FR-008**: Cover adapter behavior in `tests/LamuFlix.IntegrationTests/EfRecommendationCandidateSourceTests.cs` against real PostgreSQL+pgvector via Testcontainers with deterministic 384-dimensional stored vectors (`EmbeddingDimensions.Value`), including the named synthetic cosine set where stored-vector query/serialization proof applies; never mock the driver; no live Ollama; no container-availability waiver. Replace obsolete uncapped-pool and exact-one-reader expectations forced by new AC. Cover handler behavior in `GetRecommendationsQueryHandlerTests.cs` and `GetRecommendationsQueryHandlerPropertyTests.cs` with the named synthetic set, equality at `0.5`, and configuration overrides (handler substitutes carry known cosines). Cover options in `RecommendationOptionsTests.cs` including default-`0.5` regression (`CONCLUSIONS.md:210-211`; brief.md:357; `recon-DEV-348:27-31,89-94`).
- **FR-009**: Frozen production file set is the five Core/Infrastructure files and four test files named in brief Q8. Existing DI registration and architecture whitelist remain unchanged. Drift-induced further files require cited scope adjudication (brief Q8/Q10).
- **FR-010**: No new NuGet/npm dependency, project, top-level folder, architectural layer, database schema/migration beyond consuming DEV-413, public HTTP/OpenAPI/web contract change, `Features:LocalPlay`, secrets, or `Process.Start` (`specs/PRODUCT.md` §5; brief Frozen scope). No ADR or global glossary edit for this M ticket (brief Q8/Q11).
- **FR-011**: Phase B gate bar follows brief Gate expectations and task-pipeline Phase 3 step 5: Roslyn, complexity, InspectCode (no `-All`), mutation for changed production C# (`run-mutation.ps1` with since disabled), property tests (mandatory; no opt-out), vulnerable packages, format verification, `dotnet test`, web gates. Exit 1 and Could not run block; scope-empty exit 2 is `SKIPPED (scope-empty)`, never PASS; mutation exclusions are N/A, never PASS; harness opt-out is SKIP, never PASS. Thresholds stay in `harness.yml` and are never lowered. No web changes → web gate is scope-empty, not checked green.
- **FR-012**: Closing bar: in-scope Critical/High/Medium findings block; Low are nonblocking observations; AC failures and failed/unrunnable applicable gates always block. Require Sentry Risk, Ledger Standards, and Compass Spec on the pinned head/range. Plan challenge and code-review/remediation each capped at two rounds; at most two fix commits per remediation round. Frozen scope ends with anything else is a follow-up issue, not a finding in this round (brief Q9).
- **FR-013**: Delivery scope is the ticket Scope & Technical Design and AC plus Patron Q1–Q11 as amended by `CONCLUSIONS.md:204-211`: stored-vector server-bounded candidate retrieval with similarity, validated configurable threshold (default `0.5`, synthetic calibration), handler-only story reason, and the nine-file migration/test set. Scope statements describe that delivery boundary and do not preclude findings about failures to meet in-scope requirements.

### Key Entities

- **RecommendationCandidate**: Immutable Core pair of a candidate `Movie` and its semantic `CosineSimilarity` from the pgvector query.
- **SimilarStoryThreshold**: Configurable finite cosine-similarity threshold on `RecommendationOptions` that gates emission of the "Similar story" reason.
- **movie_features stored vector**: Persisted embedding row consumed unchanged from landed DEV-413; not created or altered by this ticket.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For a selected movie with a usable stored vector, the candidate source returns at most `CandidatePoolSize` candidates ordered by cosine distance ascending then movie id ascending, each with finite cosine similarity in `[-1, 1]`.
- **SC-002**: Missing or unusable selected `movie_features` vector yields an empty candidate list without throw after one reader command.
- **SC-003**: "Similar story" appears on a recommendation if and only if candidate cosine similarity is strictly above `SimilarStoryThreshold` (default `0.5`); equality and below emit none; weighted score and final order remain the existing metadata contracts.
- **SC-004**: Populated pools use at most two sequential database reads; no N+1; no request-time embedding; no client full-table distance scan.
- **SC-005**: Options reject non-finite and out-of-range thresholds and accept endpoints; default `0.5` is regression-tested; named synthetic cosines 0.8/0.6 emit above and 0.4/0.0/-1.0 stay at/below (`CONCLUSIONS.md:210-211`).
- **SC-006**: No schema/migration authored by this ticket, no new dependency/port/layer/HTTP/web change, and no DEV-354 reranking ships.
- **SC-007**: Phase B gates run with honest exit/verdict recording; SKIPPED/SKIP/N/A are never recorded as PASS.

## Assumptions

- Append "Similar story" after all existing metadata feature reasons, preserving their order (ASSUMPTIONS.md; tagged `[assumed]`).
- Options property name is `SimilarStoryThreshold` (ASSUMPTIONS.md; tagged `[assumed]`).
- Equal source cosine distances use ascending movie id as deterministic tie-breaker (ASSUMPTIONS.md; tagged `[assumed]`).
- Concrete DEV-413 entity/column identifiers and DEV-342 golden fixture paths are Phase B recon bindings, not Phase A inventions. Landed DEV-342 remains a sequencing prerequisite; calibration uses synthetic pairs per `CONCLUSIONS.md:204-211`, not DEV-342 semantic vectors (brief Q1/Q8; brief.md:356).
- PRODUCT defers Recommendations / Ollama / pgvector execution until after Epic 7 (`specs/PRODUCT.md:17`); Phase B also requires landed DEV-413 and landed DEV-342 implementation (sequencing) (brief Q1).
- The former owner structural calibration checkbox is superseded by the 2026-10-23 amended-calibration ruling (`CONCLUSIONS.md:204-211`; note DEV-348:66-74). This document reconciliation does not open a new owner checkbox and does not claim prerequisite, receipt, gate, or implementation clearance (brief.md:347-349; brief Q11; `specs/PRODUCT.md` §5).
- Exclusions in brief Frozen scope remain out of scope.
