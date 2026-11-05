# Implementation Plan: Explicit movie reviews and compact feedback icon row

**Branch**: `feature/461-spec` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Frozen `specs/DEV-461/brief.md` (Q1–Q10 + Phase 2 step 6 challenge addendum); `CONCLUSIONS.md` (incl. `9e3fdc1663d53b4fedcdf88c201a5fdcd56d4c71`); `ASSUMPTIONS.md`; ADR `docs/adr/0034-explicit-movie-reviews.md`; Maestri notes `DEV-461`, `recon-DEV-461`, `recon-DEV-461-2`, `recon-DEV-461-3`, `adjudication-DEV-461-Keel`; `specs/PRODUCT.md`; `.specify/memory/constitution.md` (cite only).

**Status**: Draft — Phase 2 Spec Kit drafting + challenge repair round 1 (Keel adjudication items 1–6). Gate 1 not approved. PLAN FROZEN not declared. No implementation clearance. No production gates run in this ask.

**Artifacts**: `spec.md`, `plan.md`, `tasks.md`, `checklists/requirements.md`, `DRAFTING_RECEIPT.md` (Quill). Palette-owned `design.md` is listed in the tree; Quill does not author or rewrite it. No `research.md`, `data-model.md`, `quickstart.md`, or `contracts/` — every technical choice is ruled in the brief/ADR; public HTTP contract is specified in brief Q2 + step 6 and restated in spec/plan. ADR 0034 drafted by Keel — Quill does not edit ADR bodies; status remains **Proposed** until Gate 1 acceptance, then explicit owner/Keel Accepted lifecycle update.

## Summary

Replace bulky card Watched / Not interested text buttons with a shared four-icon row (Watched, Thumbs up, Thumbs down, Watchlist) on all existing MovieCard surfaces and Movie Details, and make mutually exclusive thumbs reviews the single canonical Review (`None` / `Liked` / `Disliked`) that persists, projects legacy not-interested compatibility, and actually changes For You and Related through separate liked/disliked Taste Profile aggregates (plot + genres/cast/director), cache invalidation, and truthful reason chips. Contract chain regenerates through tooling. Storage-only or UI-only reviews do not meet the ticket.

## Domain vocabulary

| Term | Definition (brief Q8) | Notes |
|------|----------------------|-------|
| Review | Explicit opinion SmartEnum None/Liked/Disliked (Name none/liked/disliked), independent of watching and watchlist | New glossary term in delivery PR; distinguish from Rating / code review |
| Feedback | Explicit Watched and Review actions | Updates existing CONTEXT.md Feedback entry |
| Taste Profile | Aggregate of viewing history and explicit review preferences | Updates existing CONTEXT.md entry |
| Reason | Existing kind/text chip; may include Because you liked {X} | Shape unchanged (Q6) |

## Technical Context

**Language/Version**: C# / .NET 10 backend — ports and adapters with a feature-organised core and explicit handlers with decorators (constitution Principle I–II; not vertical-slice architecture and not four-layer Clean Architecture); TypeScript / React 19 SPA under `web/`.

**Primary Dependencies**: Existing pins only — including already-referenced Ardalis.SmartEnum. **No new NuGet/npm packages** and no project-owned C# enum substitute (local SVG thumbs in `web/src/ui/icons.tsx`; Q3; PRODUCT §5 item 1; step 6).

**Storage**: EF Core on existing Movies table — add canonical Review SmartEnum on `Movie` / `MovieRecord` with EF Value converter; same-migration backfill `IsNotInterested` → Review then drop legacy mapping/column (lossy Down Disliked→true, None|Liked→false); public legacy flag becomes projection only (Q2; step 6; care item 3 authorized by Patron).

**Testing**: xUnit v3, Shouldly, NSubstitute, FsCheck; Testcontainers for migration/reload/HTTP/cache; FakeEmbeddingGenerator (never live Ollama); Vitest + RTL + existing typed MSW; OpenAPI Verify drift. Gates from harness.yml at implementation time — do not freeze numeric thresholds here (Q7–Q8).

**Target Platform**: Existing `LamuFlix.Api` + `web/` Compose stack.

**Project Type**: Extend existing Feedback, Recommendations, persistence, and library UI surfaces — no new project/layer (Q8).

**Performance Goals**: None numeric in brief (no invented load bound for reviewed-but-unplayed inputs — Sentry L2 follow-up). Functional requirement: next recommendation request observes successful review mutation after completed invalidation (Q5; step 6).

**Constraints**: TreatWarningsAsErrors; TimeProvider for time; no explanatory comments beyond allowed exemptions; no LocalPlay/secrets/`Process.Start`; never hand-edit generated OpenAPI/TS/MSW; never lower gate thresholds; report PASS/failure/SKIPPED/SKIP/N/A/Could not run distinctly; complexity ≤ harness then refactor gate; no new Feedback folder, Feedback-to-Recommendations Core port, retry queue, or distributed cache guarantee.

**Scale/Scope**: Size L; frozen Q1–Q9 surfaces named in brief Implementation plan decisions table; anything else is a follow-up issue.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates checked against `.specify/memory/constitution.md` on this worktree. Brief Q10: no constitution departure and no owner checkbox currently required.

| Principle / rule | Gate | Plan response |
|---|---|---|
| I (Ports and Adapters) | No new ports/layers without ADR | **PASS** — extend existing Feedback/Recommendations/persistence; ADR 0034 records review/profile tradeoff (Keel). |
| II / V (Failures / API) | ProblemDetails; ruled status codes | **PASS** — PUT review raw-string + ValidationDecorator: 400/422/404/204 matrix (step 6); legacy 409 retained (Q2). |
| VII (Configuration / secrets / LocalPlay) | Options validation; no secrets/LocalPlay | **PASS** — RecommendationOptions/SimilarityWeights validation (Q4); no LocalPlay/secrets/`Process.Start`. |
| VIII (Ubiquitous Language) | CONTEXT.md + ADR | **PASS path** — glossary update in delivery PR; ADR 0034 Proposed until Gate 1 Accepted lifecycle; terms used in this plan/spec. |
| IX (Test Pyramid) | Real DB for translation/migration; no mocked driver | **PASS** — Testcontainers + FakeEmbeddingGenerator; Vitest/MSW for UI (Q7). |
| Enumeration / wire | SmartEnum by Name on wire, Value persisted | **PASS** — approved Ardalis.SmartEnum; lowercase Names; EF Value converter; responses expose Name strings (step 6; recon-3). |
| API and Contract Rules | C# → OpenAPI → generated TS | **PASS** — tooling regeneration; Verify baseline; no handwritten web duplicates (Q2, Q7; ADR-0007). |
| Static-Analysis / PR gates | Harness scripts; never lower thresholds | After Gate 1 + Phase B pickup; honest dispositions (Q7–Q8). |
| Governance / ADR | Architectural choice recorded | **PASS** — ADR 0034 Proposed until Gate 1 acceptance; Quill does not rewrite it. |

### Complexity Tracking

No unjustified constitution violation. No Complexity Tracking rows.

## Approach

Follow brief Implementation plan decisions order (Q8) with step 6 repairs:

1. **Domain / migration**: Glossary terms in artifacts; Movie Review SmartEnum invariant (CLR None/Liked/Disliked, Name none/liked/disliked); `MovieRecord` / `MovieConfiguration` EF Value converter; same-migration backfill then drop `IsNotInterested` mapping/column with explicit lossy Down; repository/projection mappings; domain/handler tests for transitions, Watched/Watchlist independence, backfill, sole truth, and Down.
2. **API / contract**: Feedback commands/handlers with raw-string review body; Infrastructure FluentValidation + existing ValidationDecorator/registration; full 400/422/404/204 boundary matrix (missing/null reach semantic 422); `FeedbackEndpoints`, `HandlerRegistration`, `LibraryResponses`, `RecommendedMovieDto` Name-string fields; legacy adapters as projections; regenerate OpenAPI/types/MSW; Verify snapshot; no handwritten web duplicates.
3. **Recommendations**: `TasteProfile` / `TasteProfileMovie` / calculator / profile similarity / options weights; both query handlers preserving selected-movie similarity, blend composition, MMR, and deterministic ordering except the signed profile contribution (brief:69); `EfRecommendationProfileSource` source-input work to load reviewed-but-unplayed movies with Review state and required plot embeddings plus genres/cast/director metadata into separate liked/disliked aggregates while preserving the watched/playback baseline query separation; eligibility projections; both review paths through existing `EfMovieRepository` post-commit generation-bump-then-tag-remove invalidation (no Feedback→Recommendations Core reference/new port); ranking on controlled pools, reason provenance, warm-cache mutation + failed-tag-remove/read + in-flight generation rejection + architecture/notification regressions; real-source Testcontainers regression for unplayed Liked/Disliked; channel-isolated plot/genre/cast/director proofs in both handlers.
4. **Web UI**: `MovieFeedbackActions` (+ CSS), `MovieCard`, `MovieDetailsView`, `library.css`, `watchlist.tsx` (`WatchlistToggleButton`), `movieFeedback.ts` / store, `ui/icons.tsx`; provisional rollback reconciled by authoritative refresh/idempotent retry; typed fixtures/MSW tests for four icons and lifecycle.
5. **Verification**: Backend harness gates + `web-implement` / `scripts/run-web-gates.ps1`; live acceptance evidence honestly reported; no unrelated gate/config changes.

**Phase labels**: Phase A = planning documents only (this draft + challenge repair). Gate 1 = explicit user approval of the spec package + user-owned spec PR merge. Phase B = post-merge `T000` pickup drift against landed main/contracts, then implementation Phases 1–5 in `tasks.md`. PLAN FROZEN ≠ Gate 1 approval; no seat merges.

## Project Structure

### Documentation (this feature)

```text
specs/DEV-461/
  brief.md, CONCLUSIONS.md, ASSUMPTIONS.md
  spec.md, plan.md, tasks.md, DRAFTING_RECEIPT.md
  design.md                               # Palette-owned; Quill links only, does not author
  checklists/requirements.md
docs/adr/0034-explicit-movie-reviews.md   # Keel; Proposed until Gate 1 Accepted lifecycle; Quill does not rewrite
CONTEXT.md                                # Review / Feedback / Taste Profile update in delivery PR (Q8)
```

`research.md`, `data-model.md`, `quickstart.md`, and `contracts/` are not produced — brief freezes schema/API/math/files/proofs. Palette owns `design.md` (contrast evidence ≥4.5:1; Quill does not claim measurements).

### Source Code (repository root)

```text
# Domain / persistence (order 1)
src/LamuFlix.Core/Domain/Movie.cs
src/LamuFlix.Infrastructure/Persistence/Records/MovieRecord.cs
src/LamuFlix.Infrastructure/Persistence/Configurations/MovieConfiguration.cs
src/LamuFlix.Infrastructure/Persistence/Migrations/   # new migration + backfill
src/LamuFlix.Infrastructure/Persistence/EfMovieRepository.cs

# Feedback API (order 2)
src/LamuFlix.Core/Features/Feedback/          # review command/handler; legacy adapters
src/LamuFlix.Api/Endpoints/FeedbackEndpoints.cs
src/LamuFlix.Api/HandlerRegistration.cs
src/LamuFlix.Api/Endpoints/LibraryResponses.cs
src/LamuFlix.Api/Endpoints/RecommendedMovieDto.cs
web/src/api/openapi.json                      # tooling only
web/src/api/types.ts                          # generated
web/src/mocks/handlers.ts                     # generated
tests/.../Snapshots/OpenApiContractTests...   # Verify baseline

# Recommendations (order 3)
src/LamuFlix.Core/Features/Recommendations/   # TasteProfile*, calculators, both handlers
src/LamuFlix.Core/Options/RecommendationOptions.cs  # + SimilarityWeights
src/LamuFlix.Infrastructure/Persistence/EfRecommendationProfileSource.cs
src/LamuFlix.Infrastructure/Persistence/HybridCachedRecommendationProfileSource.cs  # as needed

# Web (order 4)
web/src/library/MovieFeedbackActions.tsx|.css
web/src/library/MovieCard.tsx
web/src/features/movie-details/MovieDetailsView.tsx|.css
web/src/library/library.css
web/src/library/watchlist.tsx                 # WatchlistToggleButton (Q9 path)
web/src/library/movieFeedback.ts
web/src/library/movieFeedbackStore.ts
web/src/ui/icons.tsx                          # ThumbsUp/ThumbsDown local SVGs

# Tests (allowlist from brief / recon maps)
tests/LamuFlix.UnitTests/Features/Feedback/
tests/LamuFlix.UnitTests/Features/Recommendations/
tests/LamuFlix.IntegrationTests/Feedback*.cs, Recommendation*.cs, OpenApiContractTests.cs, migration/reload coverage
web/src/library/MovieFeedbackActions.test.tsx
web/src/library/MovieCard.test.tsx
web/src/library/movieFeedback*.test.*
web/src/features/movie-details/MovieDetailsView.test.tsx
web/src/features/movie-details/RelatedMovies.test.tsx
# plus fixture callers named in recon-DEV-461 §5
```

**Layout decisions**:

- New files only inside existing feature/test/migration folders (Q8 care item 6).
- Do not create `WatchlistButton.tsx` / `MovieCard.css` — Q9 clarifies those names were shorthand.
- `MovieListRow` unchanged (Q3).
- Preserve existing list-row controls outside the redesign.

## Test Strategy

| Obligation (brief) | Proof |
|---|---|
| Independent Review + legacy compatibility | Domain/handler deterministic tests; Testcontainers same-migration backfill + drop + lossy Down + reload/projection; HTTP matrix with raw-string validation boundaries |
| Positive/negative effects both handlers | Controlled pool before/after Like/Dislike/Clear/switch demonstrating ranking/exclusion; MMR controlled; no universal rank-change assertion for arbitrary reviews (Sentry M2); channel-isolated plot/genre/cast/director proofs in both handlers |
| Reviewed-but-unplayed source inputs | Real-source Testcontainers regression: unplayed Liked and Disliked movies load through `EfRecommendationProfileSource` with Review state and required metadata/embeddings into liked/disliked aggregates while baseline watched/playback separation is preserved (FR-008/017; brief:57; recon-DEV-461-2) |
| Stable math/config | Unit + FsCheck: bounded affinities, equal-count normalization, signed contribution, missing data, invalid options; preservation of selected similarity / blend / MMR / ordering except profile contribution |
| Next request observes review (cache) | Warm real cached profile → mutate canonical/legacy through existing repository seam → completed invalidation → next recommendation differs on controlled pool; clear/switch removes old effect; failed RemoveByTagAsync then read; in-flight generation rejection preserved; post-commit invalidation failure → 500 with committed write retained; architecture/notification regressions; no retry queue / swallow / new port / distributed guarantee |
| Truthful explanations | Provenance, liked-source evidence, ties, sanitation, disabled/negative-only |
| Four icons + Details persistence | Vitest/RTL + typed MSW: names, tooltips, focus/active, isolation, thumbs mutual exclusion (Watched/Watchlist independent), shared state, pending, provisional rollback/retry, one bookmark |
| Contract sync | Tooling-generated OpenAPI/TS/MSW + Verify; Name-string `review`; no hand edits or handwritten duplicates |

No Gherkin unless later opted in. Property tests where domain invariants apply (Q7). Mutation survivors require tests, never lowered thresholds.

## Gates (after Gate 1 user approval + user-owned spec PR merge, then Phase B T000, then implementation)

1. Explicit changed-C# scope: `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` then refactor threshold from harness, `./scripts/run-jetbrains-inspectcode.ps1`.
2. Affected unit/integration suites, then full `dotnet test`.
3. Property / vulnerable-packages / format per harness; distinguish PASS, failure, SKIPPED, SKIP, mutation N/A, Could not run.
4. Mutation: `pwsh -NoProfile -File ./scripts/run-mutation.ps1` (no `--since` from worktree).
5. Web: `scripts/run-web-gates.ps1` when `/web` changed.
6. Size L review: Sentry, Ledger, Compass; closing bar Critical/High/Medium block; Low follow-ups; two-round cap; missing axis blocked (Q10).

Phase A runs none of these and claims no gate result. Gate 1 approval and PLAN FROZEN are distinct; no seat merges the spec PR.

## Risks

- **Dual-truth drift**: leaving `IsNotInterested` independently writable — mitigated by same-migration drop + Review-only persistence + projection adapters (Q2; step 6; ADR 0034).
- **UI-only / storage-only delivery**: failing ticket Recommendations clause — mitigated by mandatory controlled-pool ranking + cache tests before UI-complete claim (Q4–Q5, Q7).
- **Signed centroid / negative Jaccard**: cancelled signals — mitigated by separate liked/disliked aggregates (Q4; ADR 0034).
- **Stale Taste Profile / partial invalidation**: next request ignores review or 204 claimed after failed tag remove — mitigated by incumbent post-commit seam proofs (204 requires completed invalidation; 500 retains write; failed-remove/read; in-flight generation rejection) (Q5; step 6).
- **Fabricated reasons**: liked-unwatched inferred as viewing history — mitigated by Q6 provenance rules and tests.
- **Generated-file hand edits**: contract drift — tooling-only regeneration (Q2, Q7).
- **Pickup drift**: Phase A recon ages — after Gate 1 user-owned merge, Phase B T000 fresh recon + cross-artifact reanalysis before coding.
- **Visual polish timing**: Palette `design.md` / CSS may adjust styles without changing Q9 copy (brief); stop if a visual change would alter frozen interaction contracts; contrast evidence is Palette-owned.

## Retained Low follow-ups (recorded; not frozen delivery)

- No numeric performance/load bound for reviewed-but-unplayed inputs (Sentry L2).
- No new public concurrency/ETag/409/412 for review assignment (Sentry L3).
- Palette contrast ≥4.5:1 measurements and design wording (Sentry L4 / Compass F4) — Quill links only.
- Shared-function rewrite only if Phase B refactor gates show duplication (Ledger F8).
- Rejected as out of scope: new Feedback folder (Ledger F11), Feedback→Recommendations Core port, retry/swallow architecture, handwritten web contract duplicates.

## Ordering

```text
Phase A   Planning docs + challenge repair (current)
Gate 1    Explicit user approval + user-owned spec PR merge
Phase B   T000 (pickup/drift) — blocks implementation
Phase 1   Domain + SmartEnum migration + projections   (US1 foundation)
Phase 2   PUT review + validators + DTOs + contracts  (US1 API)
Phase 3   Profile/scoring/eligibility/cache/reasons   (US2 + US3)
Phase 4   Shared icon row + stores + MSW tests        (US4)
Phase 5   Glossary CONTEXT.md + gates + live evidence
```

Contract regeneration precedes web consumption of new `review` fields (brief). See `tasks.md` for task IDs (T000–T050; IDs unchanged this repair).
