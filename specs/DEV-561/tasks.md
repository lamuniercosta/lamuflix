---
description: "Task list for DEV-561 outside-library discovery on For You"
---

# Tasks: Outside-library discovery on For You

**Input** — Design documents in `specs/DEV-561/`: spec, plan, research, data-model, contracts, quickstart, brief, conclusions, assumptions. Authority includes the DEV-561 ticket (as amended by Q3 Option A), `recon-DEV-561`, Accepted ADR 0049, and `specs/PRODUCT.md`.

**Status** — Draft. PLAN FROZEN is Keel-owned. Build tasks depend on the verified owner Gate 1 merge receipt.

Work inside the file boundaries in `plan.md` and `brief.md`. Follow phase order. `[P]` denotes disjoint work with prerequisites complete.

## Phase 0: Gate 1

- [ ] T000 **Bernstein** coordinates and **Rigger** prepares/updates the DEV-561 spec PR (Accepted ADR 0049 already in tree). Only the user merges. **Keel** records the verified owner merge receipt in `specs/DEV-561/CONCLUSIONS.md`. All Build tasks depend on this receipt. Q3 Option A owner checkbox is already resolved on PR #492.

## Phase 1: Read-only pickup

- [ ] T001 After T000, **Wisp** checks picked HEAD against named persistence, adapter mapping, enrichment/backfill, recommendation options, endpoint, For You web, and test seams. Confirm DEV-539 Done and ADR 0049 Accepted. Record exact nullable column facets and existing write paths for recommendation rows. Stop with `needs recon` for missing required facts. Keel records demonstrated drift and obtains a cited ruling before boundary extensions.

## Phase 2: Schema and metadata persistence (US1)

- [ ] T002 [US1] After T001, extend `src/LamuFlix.Infrastructure/Persistence/Records/MovieRecommendationRecord.cs` and `MovieRecommendationConfiguration` in `MovieConfiguration.cs` with nullable `title`, `release_year`, and `poster_path` while preserving composite key, cascade, and `recommended_tmdb_id` index.
- [ ] T003 [US1] After T002, add the additive EF migration under `src/LamuFlix.Infrastructure/Persistence/Migrations/` and update the model snapshot; prove it applies against real Postgres that already holds ID-only `movie_recommendations` rows.
- [ ] T004 [P] [US1] After T001, extend `TmdbMetadataProvider.Mapping.cs` / `TmdbMetadataProvider.Signals.cs` so the existing first-page recommendations path supplies candidate id, title, optional release year, and optional poster path with id dedupe and without increasing the production request count beyond the first page.
- [ ] T005 [US1] After T002/T004, update enrichment persistence in `EfMovieRepository.cs` (and any T001-confirmed enrichment seam) to write full recommendation metadata on successful fetch and retain prior links on failed fetch.
- [ ] T006 [US1] After T005, update `BackfillTmdbRecommendationsCommandHandler.cs` so matched source movies whose existing rows lack titles are hydrated (not skipped solely because `TmdbId` is present), with atomic per-source updates and retain-on-failure behaviour.
- [ ] T007 [US1] After T003/T005/T006, add real Postgres/Testcontainers coverage for migration compatibility, Crowd ID-only consumer preservation, ingest metadata, legacy hydration, and retain-prior-links on unsuccessful fetch. Add provider fixture coverage for mapping/optional fields/dedupe.

## Phase 3: Local discovery ranking (US2) 🎯 MVP

- [ ] T008 [US2] After T001, bind Liked (default 2) and Watched (default 1) discovery weights in `src/LamuFlix.Core/Options/RecommendationOptions.cs` with validation consistent with existing options patterns.
- [ ] T009 [US2] After T003/T008, implement the set-based local discovery query/handler under `src/LamuFlix.Core/Features/Recommendations/`: Option A exclusion, blank-title exclusion, disliked/watchlist rules, pair dedupe, two-seed minimum, weight sum, metadata tuple selection, stable sort, cap 20, and response DTO assembly. Zero provider HTTP.
- [ ] T010 [US2] After T009, implement the discovery reason formatter under Features/Recommendations using display-safe title sanitization and “Recommended for {seed title} and {seed title}” with two highest-weight seeds (MovieId ascending on ties).
- [ ] T011 [US2] After T009/T010, add domain and property tests for exclusion, unknown-year behaviour, blank titles, seed eligibility, Liked precedence, weighting, dedupe/order invariance, bounded results, metadata selection, and reason formatting including coincident titles.

## Phase 4: Public read route and codegen (US3)

- [ ] T012 [US3] After T009, register `GET /api/recommendations/not-in-library` in `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs` under the existing `/api` authorization convention; return HTTP 200 card array or `[]` for disabled TMDb, blank ApiKey, or no eligible candidates.
- [ ] T013 [US3] After T012, add API integration tests asserting JSON shape, authorization convention, both config-off empties, fail-on-any-request zero outside HTTP, unchanged For You movie ids/order, Related library-only, and DEV-539 Crowd link consumers still functional.
- [ ] T014 [US3] After T012, regenerate `web/src/api/openapi.json` and `web/src/api/types.ts` from the C# DTO before web composition.

## Phase 5: For You web section (US4)

- [ ] T015 [US4] After T014, extend `web/src/features/for-you/forYouApi.ts` and `ForYouPage.tsx` (plus discovery section/card files in that feature folder) to independently fetch discovery results, hide the section on loading/empty/error, keep the library grid usable, render distinct read-only cards (title, reason, optional year, poster/fallback, new-tab TMDb link with `rel="noopener noreferrer"`), and show day-verified TMDb attribution beside the visible section. Consume generated types only.
- [ ] T016 [US4] After T015, add web tests for visible/empty/loading/error behaviour, grid independence, nullable metadata, credit, read-only affordances, and new-tab link safety.
- [ ] T017 [US4] After T015/T016, add a local seeded E2E that opens For You and sees **Not in your library yet** when candidates exist (SMOKE-001) and confirms hide behaviour for empty/unconfigured cases where the harness allows (SMOKE-002/003). Use the connected Test URLs portal for browser work.

## Phase 6: Docs and home-library evidence (US5)

- [ ] T018 [P] [US5] After T001, in the introducing delivery PR add a focused Positioning subsection to `specs/PRODUCT.md`, update the root `CONTEXT.md` Recommendation entry, define **Outside-library recommendation** with `_Avoid_: wanted movie`, and list the feature in `README.md` without taking DEV-559 provider attribution.
- [ ] T019 [US5] After T006/T009 and hydration, run production ranking on the home library; publish unique titled out-of-library ≥1-seed and ≥2-seed pre-cap counts plus ranked top 10 (or all if fewer) with actual owner good-pick/not verdicts on the delivery PR. Keep raw viewing/seed data local. Sparse results reported as observed.

## Phase 7: Gates, Size L review, ship, and mergeability

- [ ] T020 After T007/T011/T013/T016/T017/T018 (and T019 evidence), confirm the acceptance evidence set is complete per `brief.md` Q10 and Done when.
- [ ] T021 After T020, run and record Size L Phase 3 gates with actual statuses: Roslyn, complexity (including refactor `-Threshold 6`), InspectCode, mutation (`scripts/run-mutation.ps1` without `--since` from a worktree), property tests, vulnerable packages, format, .NET tests, and web gates. Apply brief exit-2 dispositions. Failure or `Could not run` blocks.
- [ ] T022 After T021, complete Size L three-axis review against the frozen diff per brief closing bar (Q12): two rounds, two fix commits per round; resolve all confirmed actionable in-scope findings including Low; escalate remaining blockers on the PR at the cap without declaring clearance. Then follow task-pipeline Phase 5 order: rebase → Phase 3 gates on rebased head → Size L ship review → open/update delivery PR with acceptance evidence → verify findings/gitleaks/mergeability → merge-bar receipt. The user owns the merge.

## Dependencies & execution order

- T000 → T001 → schema/mapping (T002 ∥ T004 → T003/T005 → T006 → T007) → ranking (T008 → T009/T010 → T011) → endpoint/codegen (T012 → T013 ∥ T014) → web (T015 → T016 → T017) → docs/evidence (T018 may proceed once docs files are free; T019 after hydration + ranking) → gates/review/ship (T020–T022).
- US1 must land before US2 relies on titled rows. US3 waits on the query DTO. US4 waits on regenerated types. US5 docs can parallelize with later implementation; home-library evidence needs hydration + production ranking.
- Brief sequencing preserved: Gate 1 → pickup → persistence/backfill → local query → generated contract → web → docs/E2E/home acceptance → Size L verification and Phase 5 ship.

## Parallel opportunities

- After T001: T002 ∥ T004; T008 may draft against options once unblocked; T018 docs may proceed on disjoint files.
- After T012: T013 API tests ∥ T014 OpenAPI/TS regeneration.
- After T015: T016 web tests may proceed while E2E fixtures are prepared for T017.

## Implementation strategy

1. Gate 1 + pickup (prerequisites and seams only).
2. Additive schema + mapping + enrichment/backfill (US1).
3. Local ranking + reasons + options (US2 MVP).
4. Endpoint + parity tests + codegen (US3).
5. Web section + tests + local E2E (US4).
6. Docs + home-library receipt (US5).
7. Size L gates and pinned review dispositions; Phase 5 rebase → gates → ship review → delivery PR → merge-bar; await owner merge.

## Notes

- `[P]` = disjoint files, prerequisites complete.
- Gate 1 and runtime acceptance require their own receipts.
- Do not invent Not interested, candidate IMDb lookups, Related outside movies, new dependencies, LocalPlay, secrets, or `Process.Start`.
- Use existing test infrastructure and repository comment conventions.
