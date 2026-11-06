# Tasks: Explicit movie reviews and compact feedback icon row

**Input**: `specs/DEV-461/` spec.md, plan.md, frozen brief.md (Q1–Q10 + step 6 addendum), CONCLUSIONS.md, ASSUMPTIONS.md, ADR 0034, notes DEV-461 / recon-DEV-461 / recon-DEV-461-2 / recon-DEV-461-3 / adjudication-DEV-461-Keel, specs/PRODUCT.md

**Status**: Draft — Phase 2 Spec Kit drafting + challenge repair round 1 (Keel items 1–6). Gate 1 not approved. PLAN FROZEN not declared.

**Prerequisites**: plan.md (required), spec.md (required). No `research.md` / `data-model.md` / `contracts/` (plan records why). ADR 0034 already authored by Keel (Proposed until Gate 1 Accepted lifecycle). Palette owns `design.md`; Quill does not author it.

**Phase labels**: Phase A = planning docs. Gate 1 = explicit user approval + user-owned spec PR merge. Phase B = `T000` pickup after that merge. Implementation Phases 1–5 below run only after T000.

**Tests**: Required (brief Q7; ticket Acceptance). Prefer red-green within each phase; required coverage green before verification.

**Format**: `- [ ] [ID] [P?] [Story?] Description with file path`. `[P]` = parallelisable once own prerequisites complete (different files, no incomplete deps). Same-file edits are never `[P]`.

**Conventions**: `Method_Condition_Expected`; `TestContext.Current.CancellationToken`; only `// arrange` / `// act` / `// assert`; no new explanatory comments; FakeEmbeddingGenerator not live Ollama; no mocked EF/driver serialization; never hand-edit generated OpenAPI/TS/MSW; Q9 paths (`watchlist.tsx`, `library.css`); thresholds only from harness.yml.

---

## Phase dependency / execution order

```text
Gate 1   Explicit user approval + user-owned spec PR merge (prerequisite)
Phase B  T000 (pickup/drift) — blocks T001+
Phase 1  T001 → T008     Domain + SmartEnum migration + projections (US1)
Phase 2  T009 → T016     API + validators + DTOs + contracts (US1)
Phase 3  T017 → T028     Recommendations + cache seam + reasons (US2, US3)
Phase 4  T029 → T038     Shared icon row UI (US4)
Phase 5  T039 → T050     Glossary + ADR lifecycle note + gates + live evidence
```

Task IDs remain T000–T050 (51 tasks). Challenge repair clarifies wording; no ID renumber.

---

## Phase B: Pickup — fresh recon and drift reanalysis (blocks T001+)

**Purpose**: After Gate 1 user approval and user-owned spec PR merge, confirm frozen brief/spec/plan/tasks still match landed main and contracts before any production edit. PLAN FROZEN is not Gate 1 approval.

- [X] T000 Obtain fresh Phase B pickup recon against picked HEAD / landed main for the authorized file set (Movie/Feedback/Recommendations/persistence/web library surfaces + OpenAPI chain). Run cross-artifact reanalysis of brief/spec/plan/tasks against landed main/contracts. Amend tasks only where authorized by demonstrated drift through the assigned workflow. **STOP** on unresolved drift or missing facts (`needs recon` / cited ruling) — do not invent substitutes. No production edits in this task (brief Phase B; plan Risks).

---

## Phase 1: User Story 1 — Domain Review invariant, migration, projections (Priority: P1)

**Goal**: Canonical Review on Movie aggregate/record; historical IsNotInterested migrated; projections ready for API.

**Independent Test**: Domain transition tests + Testcontainers migration/reload proving Review and independence from Watched/Watchlist.

- [X] T001 [US1] Add Review as approved Ardalis.SmartEnum (CLR `None`/`Liked`/`Disliked`, literal lowercase Name `none`/`liked`/`disliked`, exact `TryFromName`) with transition invariants (active clear, opposite switch, Watched/Watchlist independence) on `src/LamuFlix.Core/Domain/Movie.cs` — no new package or project-owned C# enum (FR-001, FR-002; Q1; step 6)
- [X] T002 [US1] Persist Review Value on `src/LamuFlix.Infrastructure/Persistence/Records/MovieRecord.cs` and configure EF Value converter in `src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs`; prepare for same-migration drop of legacy `IsNotInterested` mapping (FR-001, FR-003; Q2; step 6)
- [X] T003 [US1] Add EF migration under `src/LamuFlix.Infrastructure/Persistence/Migrations/` that backfills `IsNotInterested` true→Disliked / false→None then drops the legacy mapping/column; Down recreates boolean as Disliked→true and None|Liked→false (explicitly lossy for Liked); Review is sole persisted opinion truth (FR-003; Q2; ADR 0034; step 6)
- [X] T004 [US1] Update repository/projection mappings in `src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs` (and related mappers) so reload yields Review and public legacy not-interested projection only — no independent legacy column truth (FR-003, FR-004; Q2)
- [X] T005 [P] [US1] Extend unit domain/handler-facing tests under `tests/LamuFlix.UnitTests/` for all Review transitions, active clear, opposite switch, Watched/Watchlist independence, and SmartEnum Name/Value parsing (FR-017; Q7)
- [X] T006 [US1] Add/extend Testcontainers migration + reload proofs covering historical mark backfill, legacy column drop, sole persisted opinion truth, lossy Down mapping, and projection round-trip (no mocked driver) in `tests/LamuFlix.IntegrationTests/` (FR-003, FR-017; Q7; step 6)
- [X] T007 [US1] Core legacy not-interested domain/command adapters under `src/LamuFlix.Core/Features/Feedback/` (existing Feedback feature folder only): mark→Disliked, unmark clears only Disliked preserving Liked, invalid/repeat keep 409. API endpoint wiring of those adapters is T011 — do not duplicate endpoint work here (FR-004; Q2)
- [X] T008 [US1] Checkpoint: migration/reload/Down green; no web consumption of `review` yet

---

## Phase 2: User Story 1 — PUT review API, DTOs, contract chain (Priority: P1)

**Goal**: Public review API and DTO field; legacy routes as projections; generated contracts synchronized.

**Independent Test**: FeedbackEndpointTests HTTP matrix + OpenApiContractTests Verify equality after tooling generation.

- [X] T009 [US1] Add idempotent review command/handler under `src/LamuFlix.Core/Features/Feedback/` accepting raw-string `review` parsed with exact SmartEnum `TryFromName`; add Infrastructure FluentValidation validator in existing Infrastructure validation directories, hand-wired through existing ValidationDecorator registration — missing/null/whitespace/unknown/wrong-case must reach semantic validation (FR-005; Q2; step 6)
- [X] T010 [US1] Map `PUT /api/movies/{id}/review` in `src/LamuFlix.Api/Endpoints/FeedbackEndpoints.cs` and register in `src/LamuFlix.Api/HandlerRegistration.cs` with raw-string body binding (no incoming enum converter); status matrix malformed/unbindable JSON/body or route syntax → 400, missing/null/whitespace/unknown/wrong-case review or `id <= 0` → 422, unknown movie → 404, valid including repeat → 204; central ProblemDetails/traceId; no exception-text leakage (FR-005; Q2; step 6)
- [X] T011 [US1] Wire legacy `/not-interested` routes in `src/LamuFlix.Api/Endpoints/FeedbackEndpoints.cs` (and related API registration) to the Core adapters from T007 as Review projections without a second persisted truth — T007 owns domain semantics; T011 owns HTTP adapter surface (FR-004; Q2)
- [X] T012 [US1] Add required `review` as `Review.Name` strings (never numbers) to `src/LamuFlix.Api/Endpoints/LibraryResponses.cs` and `src/LamuFlix.Api/Endpoints/RecommendedMovieDto.cs` (and Core ports as needed) while preserving existing fields; `isNotInterested` projects Disliked; no handwritten web contract duplicates (FR-006; Q2; step 6)
- [X] T013 [US1] Regenerate `web/src/api/openapi.json`, `web/src/api/types.ts`, and `web/src/mocks/handlers.ts` via existing tooling only — never hand-edit (FR-007; Q2, Q7)
- [X] T014 [US1] Update Verify baseline for `tests/LamuFlix.IntegrationTests` OpenApiContractTests to match regenerated OpenAPI (FR-007; ADR-0007)
- [X] T015 [P] [US1] Extend `tests/LamuFlix.IntegrationTests/FeedbackEndpointTests.cs` (and unit Feedback handler/validator tests) for full canonical PUT boundary matrix (400/422/404/204 including missing/null → 422) and legacy compatibility matrix; assert central errors/traceId and no exception leakage (FR-005, FR-017; Q7; step 6)
- [X] T016 [US1] Checkpoint: HTTP + contract green before recommendation or web review consumption

---

## Phase 3: User Stories 2–3 — Recommendation affinities, eligibility, cache, reasons (Priority: P1/P2)

**Goal**: Likes raise and dislikes lower/exclude in both handlers; next request observes mutation; truthful liked chip.

**Independent Test**: Controlled ranking tests both handlers; warm-mutate-next-request cache tests; reason provenance tests.

- [X] T017 [US2] Extend `TasteProfile` / `TasteProfileMovie` and `TasteProfileCalculator` for separate liked/disliked aggregates (plot, genres, cast, director), equal per-movie review weight, mean incidence / equal-weight centroids, omit Disliked from positive baseline inputs; include reviewed-but-unplayed movies as equal review signals with no invented review timestamps (FR-008; Q4–Q5; brief:57)
- [X] T018 [US2] Extend `ProfileSimilarityCalculator` for bounded `L`/`D` affinities (plot cosine floored at 0; weighted Jaccard metadata; omit missing/disabled channels) (FR-009; Q4)
- [X] T019 [US2] Add `ReviewPlotWeight`, `LikedReviewWeight`, `DislikedReviewWeight` (defaults 1.0) and startup validation on `src/LamuFlix.Core/Options/RecommendationOptions.cs` / SimilarityWeights (FR-010; Q4; ASSUMPTIONS)
- [X] T020 [US2] Wire profile contribution `baselineSimilarity + LikedReviewWeight*L - DislikedReviewWeight*D` inside existing `BlendProfileWeight` in both `GetForYouRecommendationsQueryHandler` and `GetRecommendationsQueryHandler` without clamping to `[0,1]` and without repurposing dead notInterested blend penalty; preserve selected-movie similarity, existing blend composition, MMR, and deterministic ordering except for this changed profile contribution (FR-009, FR-011; Q4–Q5; brief:69)
- [X] T021 [US2] In `src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs` (implementation Phase 3): (a) source-loading — load reviewed-but-unplayed movies with Review state and required plot embeddings plus genres/cast/director metadata into separate liked/disliked aggregates while preserving watched/playback baseline query separation (recon-DEV-461-2); (b) eligibility projections — For You excludes Watched or Disliked; Related excludes Disliked; Liked alone excludes neither. Both checks are required before T024/T025 claim ranking/eligibility success (FR-008, FR-011; Q4–Q5)
- [X] T022 [US2] Route both canonical and legacy review writes through existing `EfMovieRepository` post-commit generation-bump-then-tag-remove invalidation (with `HybridCachedRecommendationProfileSource`); no Feedback-to-Recommendations Core reference, new port, retry queue, swallow, or distributed guarantee; preserve existing genre-cache invalidation and notification behavior (FR-012; Q5; step 6; ADR 0034)
- [X] T023 [P] [US2] Unit + FsCheck coverage for bounded affinities, equal-count normalization, signed contribution, missing metadata/embeddings, invalid options under `tests/LamuFlix.UnitTests/Features/Recommendations/` (FR-017; Q7)
- [X] T024 [US2] Handler ranking tests for both For You and Related with controlled candidate pools designed for demonstrable ranking/exclusion before/after Like, Dislike, Clear, opposite switches (MMR controlled); assert relative order and exclusion on that pool — do not require universal rank-change for arbitrary reviews; include channel-isolated plot, genre, cast, and director contribution proofs in both handlers (FR-008, FR-017; Q7; SC-003; Sentry M2)
- [X] T025 [US2] Integration cache freshness proofs (`FeedbackTasteProfileInvalidationTests` / `HybridCachedRecommendationProfileSourceFreshnessTests` or equivalent): warm real cached profile → mutate canonical and legacy → completed invalidation → next recommendation on controlled pool asserts fresh ranking/exclusion and clear/switch removal of prior effects (no universal rank-change claim); failed `RemoveByTagAsync` then read; preserve in-flight generation rejection; post-commit invalidation failure → 500 with committed write retained; architecture/notification regressions; plus real-source Testcontainers regression proving unplayed Liked and Disliked movies enter `EfRecommendationProfileSource` with Review state and required metadata/embeddings while baseline separation holds (FR-008, FR-012, FR-017; Q5, Q7; SC-005; step 6)
- [X] T026 [US3] Carry liked-source provenance and emit at most one `Because you liked {X}` under Q6 gates; keep Reason kind/text; sanitize title; tie-break by movie id (FR-013; Q6; ASSUMPTIONS)
- [X] T027 [P] [US3] Reason provenance tests: baseline vs review evidence, positive liked-source, ties, sanitation, disabled-channel and negative-only non-fabrication (FR-017; Q6–Q7; SC-007)
- [X] T028 [US2] Checkpoint: recommendation + reason proofs green before UI consumption depends on ranking claims

---

## Phase 4: User Story 4 — Shared four-icon row (Priority: P1)

**Goal**: Compact accessible icon row on cards and Details; bookmark relocated; shared store lifecycle.

**Independent Test**: Vitest/RTL + typed MSW for icons, a11y, mutual exclusion, shared state, pending/rollback/retry.

- [X] T029 [US4] Add local ThumbsUp/ThumbsDown SVG components in `web/src/ui/icons.tsx` (no new dependency) (FR-014; Q3)
- [X] T030 [US4] Rewrite `web/src/library/MovieFeedbackActions.tsx` (+ `.css` as needed) to the four-icon row in Q3 order with Q9 accessible names/tooltips, `aria-pressed`, hover+focus tooltips, focus/active, navigation isolation (FR-014, FR-015; Q3, Q9)
- [X] T031 [US4] Update `web/src/library/movieFeedback.ts` and `web/src/library/movieFeedbackStore.ts` for review PUT + legacy projection fields; preserve optimistic updates, provisional rollback (no claim of DB rollback on ambiguous 500), pending/retry; invalidate library/details/For You/Related after success; authoritative refresh + idempotent retry reconcile; refresh retry never replays successful mutation (FR-012, FR-015; Q5; step 6)
- [X] T032 [US4] Move Watchlist into the shared row via `WatchlistToggleButton` in `web/src/library/watchlist.tsx` and styles in `web/src/library/library.css`; remove duplicate poster/details bookmark; leave `MovieListRow` unchanged (FR-014; Q3, Q9)
- [X] T033 [US4] Wire callers: `web/src/library/MovieCard.tsx` and `web/src/features/movie-details/MovieDetailsView.tsx` (+ CSS as needed) consuming shared row and `review` from generated types (FR-014; Q3)
- [X] T034 [P] [US4] Extend `web/src/library/MovieFeedbackActions.test.tsx` for four icons, exact Q9 names, tooltips/focus/active, mutual exclusion, pending, rollback/retry, navigation isolation (FR-017; Q7, Q9; SC-004)
- [X] T035 [P] [US4] Extend `web/src/library/MovieCard.test.tsx`, `movieFeedback.test.ts`, `movieFeedbackStore.test.tsx` for shared state, query invalidation, and review lifecycle (FR-017; Q7)
- [X] T036 [P] [US4] Extend `web/src/features/movie-details/MovieDetailsView.test.tsx` and `RelatedMovies.test.tsx` for Details reflection and related-card icons (FR-017; Q7; SC-002)
- [X] T037 [US4] Update typed fixtures/callers listed in recon-DEV-461 §5 that still assume Not interested text buttons / missing `review` (FR-006, FR-014)
- [X] T038 [US4] Checkpoint: UI MSW suite green; SMOKE-001/002 user-visible behaviours covered by component + contract proofs (live SMOKE-003 under Phase 5)

---

## Phase 5: Polish — glossary, gates, live evidence

**Purpose**: Delivery glossary/ADR presence, harness gates, honest live acceptance.

- [X] T039 Update root `CONTEXT.md` Feedback and Taste Profile definitions and add Review per brief Q8 glossary, distinguishing Review from external Rating and code review (FR-016; Constitution VIII; ASSUMPTIONS). Do not rewrite ADR 0034 body; after Gate 1 acceptance, owner/Keel records Accepted lifecycle on ADR 0034 (Proposed until then).
- [X] T040 Confirm frozen scope only: no new dependency/project/layer; no LocalPlay/secrets/`Process.Start`; no list-row redesign; no handwritten generated artifacts; no threshold edits (brief Out of scope)
- [ ] T041 Run `./scripts/run-roslyn-analyzers.ps1` on changed `.cs` set
- [ ] T042 Run `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness.yml
- [ ] T043 Run `./scripts/run-jetbrains-inspectcode.ps1` on changed files
- [ ] T044 `dotnet format --verify-no-changes`
- [ ] T045 Run affected unit/integration suites then full `dotnet test` (FakeEmbeddingGenerator paths only for embeddings)
- [ ] T046 Run property/security/vulnerable-package gates per harness; report SKIP/SKIPPED/N/A/Could not run distinctly — never as PASS
- [ ] T047 Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` without `--since` from the worktree; survivors → tests, never lower threshold
- [ ] T048 Run `scripts/run-web-gates.ps1` for `/web` changes
- [ ] T049 Record live acceptance / SMOKE evidence against Compose when available; unavailable stack/credentials → `Could not run`, never PASS (brief Q7; SC-001–SC-003)
- [ ] T050 Size L code review with Sentry, Ledger, Compass; Q10 closing bar (Critical/High/Medium block; Low follow-ups); two-round cap; missing axis blocked

---

## Dependencies

- Gate 1 user approval + user-owned spec PR merge before Phase B T000.
- T000 before any T001+.
- Phase 1 before Phase 2 (persistence before HTTP/DTO).
- T007 (Core legacy adapters) before T011 (API legacy route wiring).
- T009 validator/handler before T010 endpoint and T015 boundary tests.
- Phase 2 contract regeneration (T013–T014) before Phase 4 web consumption of `review` (T031–T033).
- Phase 3 ranking/cache proofs (T024–T025) before claiming recommendation acceptance; may proceed in parallel with Phase 4 only after T016 if staffing allows, but UI must not claim ranking success without T024–T025.
- T021 (a) source-loading and (b) eligibility before T024/T025 claim those behaviours.
- T026–T027 after affinity plumbing (T017–T020).
- T039–T050 after implementation phases complete.

### User Story mapping

| Story | Tasks | Independent test |
|---|---|---|
| US1 Review persistence + API | T001–T016 | Domain + SmartEnum migration/Down + HTTP boundary matrix + OpenAPI |
| US2 Recommendation effects | T017–T025, T028 | Controlled-pool ranking both handlers (channel-isolated) + real-source unplayed Liked/Disliked + post-commit cache seam |
| US3 Truthful reasons | T026–T027 | Provenance tests |
| US4 Icon row | T029–T038 | Vitest/MSW a11y + provisional rollback lifecycle |

### Parallel opportunities

- After T004: T005 ∥ preparatory test scaffolding.
- After T012: T015 unit/integration HTTP tests ∥ T013 generation sequencing still serial with Verify T014.
- After T020: T023 property tests ∥ handler test authoring prep.
- After T033: T034 ∥ T035 ∥ T036 fixture/test files.

### MVP

US1 (T001–T016) delivers persisted Review + API + contracts. Ticket Acceptance still requires US2 ranking proof and US4 icons before done — MVP for demo is not ticket-complete.

---

## Notes

- `[P]` = different files, no incomplete deps.
- Palette owns `design.md` contrast evidence and CSS polish; may adjust CSS without changing Q9 copy or FR-014/015 interaction contracts; Quill does not claim contrast measurements.
- Rejected/out of scope remain out: new Feedback folder, Feedback→Recommendations Core port, concurrency/ETag API, retry/swallow cache architecture, handwritten web duplicates.
- Retained Low follow-ups: no invented load bound (Sentry L2); no new concurrency controls (Sentry L3); Palette contrast/design wording (Sentry L4 / Compass F4); shared-function rewrite only via Phase B refactor gates (Ledger F8).
- Quill drafting invented no decisions; gaps → `needs decision` in DRAFTING_RECEIPT.md.

