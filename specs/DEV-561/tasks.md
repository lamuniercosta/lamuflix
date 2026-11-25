---
description: "Task list for DEV-561 outside-library discovery on For You"
---

# Tasks: Outside-library discovery on For You

**Input** — Design documents in `specs/DEV-561/`: spec, plan, research, data-model, contracts, quickstart, brief, conclusions, assumptions. Authority includes the DEV-561 ticket (as amended by Q3 Option A), `recon-DEV-561`, Accepted ADR 0049, and `specs/PRODUCT.md`.

**Status** — PLAN FROZEN, ready for owner Gate 1. Build tasks and Phase B pickup depend on the verified owner merge receipt. See [the challenge dispositions](brief.md#phase-2-step-6-adjudication).

Work inside the file boundaries in `plan.md` and `brief.md`. Follow phase order. `[P]` denotes disjoint work with prerequisites complete.

## Phase 0: Pre-merge Gate 1 sequence

- [ ] T000 **Pre-merge work:** Keel completed analyze and the Size L plan-plus-ADR challenge. Bernstein coordinates and Rigger updates the DEV-561 Spec Kit PR (Accepted ADR 0049 already in tree; omit disposable `ticket-text.txt`). Q3 Option A is resolved on PR #492. The user merges the spec PR and Keel records the verified Gate 1 merge in CONCLUSIONS.md. All Build tasks depend on that receipt. Review caps apply to the delivery revision.

## Phase 1: Read-only pickup

- [ ] T001 After T000, Wisp confirms DEV-539 Done and ADR 0049 Accepted and checks the picked HEAD for drift in the named persistence, mapping, enrichment/backfill, options, endpoint and test seams. Verify `IRecommendationCandidateSource` and `EfRecommendationCandidateSource`, the existing `recommended_tmdb_id` index, the set-based projection shape, nullable column facets, the transactional write path and existing typed-client retry policy. Verify the Verify OpenAPI snapshot test and CI drift check. Refer absent verification infrastructure to Patron before expanding scope. Confirm unauthenticated challenge and test fixture seams, including both config-off cases, the concurrent-reader regression and seeded E2E. Stop with `needs recon` for missing required facts. Keel records demonstrated drift and obtains a cited ruling before boundary extensions.

## Phase 2: Schema and metadata persistence (US1)

- [ ] T002 [US1] After T001, extend `src/LamuFlix.Infrastructure/Persistence/Records/MovieRecommendationRecord.cs` and `MovieRecommendationConfiguration` in `MovieConfiguration.cs` with nullable `title`, `release_year`, and `poster_path` while preserving composite key, cascade, and `recommended_tmdb_id` index.
- [ ] T003 [US1] After T002, add the additive EF migration under `src/LamuFlix.Infrastructure/Persistence/Migrations/` and update the model snapshot; prove it applies against real Postgres that already holds ID-only `movie_recommendations` rows.
- [ ] T004 [P] [US1] After T001, extend `TmdbMetadataProvider.Mapping.cs` / `TmdbMetadataProvider.Signals.cs` so the existing first-page recommendations path supplies candidate id, title, optional release year, and optional poster path with id dedupe and without increasing the production request count beyond the first page; carry those fields through the existing enrichment contract flow.
- [ ] T005 [US1] After T002/T004, update enrichment persistence in `EfMovieRepository.cs` and the confirmed enrichment seam. Fetch before writing and replace one source's complete links and metadata in one transaction. Retain prior links on provider failure or write rollback.
- [ ] T006 [US1] After T005, update `BackfillTmdbRecommendationsCommandHandler.cs` so matched sources with titleless links are eligible for hydration on rerun. Use the same atomic per-source replacement and retain prior links on failure. Reuse the existing first-page typed client and resilience policy for every attempt. A later eligible run may retry without an added retry loop or budget.
- [ ] T007 [US1] After T003/T005/T006, use real PostgreSQL/Testcontainers to cover migration, key/cascade and index preservation, Crowd ID-only consumers, ingest, legacy hydration, fetch and write failure retention and a later successful eligible rerun. Pause a replacement before commit and use a separate reader to show all prior links remain countable during the write and the complete new set is visible after commit. Provider fixtures prove id/title, optional fields, dedupe and the unchanged first-page call budget per attempt, accounting separately for any existing client policy retries (SC-001).

## Phase 3: Local discovery ranking (US2) 🎯 MVP

- [ ] T008 [US2] After T001, bind integer Liked (default 2) and Watched (default 1) discovery weights in `src/LamuFlix.Core/Options/RecommendationOptions.cs`. Use the existing options-validation pattern to reject values unless Liked > Watched > 0.
- [ ] T009 [US2] After T007/T008, implement sealed `GetOutsideLibraryRecommendationsQuery` and `GetOutsideLibraryRecommendationsQueryHandler` in Core Recommendations with existing `AddHandler` registration (Tracing → Logging → Validation → handler). The parameterless query uses the decorator chain without a validator class. Extend `IRecommendationCandidateSource` with a set-based `OutsideLibraryRecommendationSnapshot` read implemented in `EfRecommendationCandidateSource`. Keep EF in Infrastructure, preserve Crowd's library-only read, and make zero provider HTTP calls. The handler delegates ranking to cohesive pure Core operations following [data-model.md](data-model.md#discovery-candidate-derived-not-persisted). Follow harness rules for mutation-safe initialization. T010 completes reason and response assembly.
- [ ] T010 [US2] After T009, implement the discovery reason formatter under Features/Recommendations as plain text using display-safe title sanitization and “Recommended for {seed title} and {seed title}” with two highest-weight seeds (MovieId ascending on ties). Complete reason and response DTO assembly before endpoint or codegen work.
- [ ] T011 [US2] After T009/T010, add domain and property tests pinning: TMDb-ID exclusion across the library; the positive normalized-title/equal-known-year match restricted to library movies without a TMDb ID; same-title/different-year non-match; punctuation/accent preservation under normalization; unknown-year behaviour; null, empty and whitespace-only title exclusion; Disliked rejection and Watchlist-only exclusion; one eligible seed excluded versus two distinct eligible seeds included; Liked-over-Watched precedence; valid overrides and rejection of nonpositive, equal or inverted configured weights; pair dedupe; conflicting metadata (ineligible or losing seed cannot determine fallback identity or displayed metadata); candidate ordering (summed weight/score desc / TMDb ID asc) distinct from seed/tuple ordering (weight desc / MovieId asc); cap 20; dedupe/order invariance; and reason formatting including coincident titles.

## Phase 4: Public read route and codegen (US3)

- [ ] T012 [US3] After T010 (complete reason/response assembly), register `GET /api/recommendations/not-in-library` in `src/LamuFlix.Api/Endpoints/RecommendationEndpoints.cs` with `TypedResults` under the existing `/api` authorization convention; return HTTP 200 card array or `[]` for disabled TMDb, blank ApiKey, or no eligible candidates; place the response DTO in Core Recommendations; keep shared `ProblemDetails` for error paths.
- [ ] T013 [US3] After T012, add `WebApplicationFactory` API integration tests asserting JSON shape, HTTP 401 for unauthenticated requests before config-off/empty responses, both config-off empties (`Enabled` false and blank `ApiKey` → 200 `[]`), TMDb configured with no eligible candidates → 200 `[]`, fail-on-any-request zero outside HTTP, unchanged For You movie ids/order, Related library-only, and DEV-539 Crowd link consumers still functional.
- [ ] T014 [US3] After T012 (and after reason/response assembly is complete), regenerate and commit `web/src/api/openapi.json` and `web/src/api/types.ts` from the Core Recommendations C# DTO; satisfy the constitution Verify OpenAPI snapshot / drift obligation (committed snapshot, Verify snapshot test, CI drift fails the build) and keep drift green before web composition.

## Phase 5: For You web section (US4)

- [ ] T015 [US4] After T014, use TanStack Query and the generated openapi-fetch client in the For You feature to fetch independently. Hide the section during loading, empty or error and keep the library grid usable. Render read-only cards, title and reason as plain text, optional year, validated poster or fallback, and an accessible new-tab TMDb link with `rel="noopener noreferrer`. Give movie-page URL construction one tested home and validate the poster filename as specified in the contract. Display day-verified TMDb attribution and record the official terms URL and verification date in the delivery PR and CONCLUSIONS.md.
- [ ] T016 [US4] After T015, add MSW-backed web tests covering both config-off cases from API `200 []` through hidden section and usable library grid (fixture strategy confirmed at T001 pickup), plus visible/empty/loading/error behaviour, grid independence, nullable metadata, credit, read-only affordances, accessible TMDb link naming, unit-tested movie-page URL construction, new-tab link safety, malformed poster fallback (traversal, query delimiters and whitespace), and markup-bearing title and reason rendered as text. Map SMOKE-002 to empty/unconfigured hide and SMOKE-003 to visible-card read-only actions/link/credit assertions.
- [ ] T017 [US4] After T015/T016, add the required local seeded E2E that opens For You and proves SMOKE-001 (**Not in your library yet** visible with title, two-seed reason, and TMDb link when candidates exist). Confirm SMOKE-002 empty/unconfigured hide and SMOKE-003 visible-card read-only/link/credit assertions where the harness allows. Use the connected Test URLs portal for browser work; fixture strategy confirmed at pickup.

## Phase 6: Docs and home-library evidence (US5)

- [ ] T018 [P] [US5] After T001, in the introducing delivery PR add a focused Positioning subsection to `specs/PRODUCT.md`, update the root `CONTEXT.md` Recommendation entry, define **Outside-library recommendation** with `_Avoid_: wanted movie`, and list the feature in `README.md` without taking DEV-559 provider attribution.
- [ ] T019 [US5] After T006/T009/T010 (hydration plus completed reason/response assembly) and production ranking on the home library, publish unique titled out-of-library ≥1-seed and ≥2-seed pre-cap counts under the same Option A exclusion as the live query, plus ranked top 10 (or all if fewer) with actual owner good-pick/not verdicts on the delivery PR. Document Option A misses possible with missing years or variant titles on library records without TMDb IDs. Keep raw viewing/seed data local. Report sparse results as observed.

## Phase 7: Gates, Size L review, ship, and mergeability

- [ ] T020 After T007/T011/T013/T016/T017/T018 (and T019 evidence), confirm the acceptance evidence set is complete per `brief.md` Q10 and Done when, including the recorded official TMDb terms source and verification date from T015.
- [ ] T021 After T020, run and record Size L Phase 3 gates with actual statuses: Roslyn, complexity (including the refactor threshold configured in `harness.yml`), InspectCode, mutation (`scripts/run-mutation.ps1` without `--since` from a worktree), property tests, vulnerable packages, format, .NET tests, and web gates. Retain honest exit-2 dispositions on their own terms: scope-empty `SKIPPED (scope-empty)`, mutation `NOT APPLICABLE` / N/A, and harness-disabled `SKIP`. Failure or `Could not run` blocks.
- [ ] T022 After T021, complete Size L three-axis review against the frozen diff per brief closing bar (Q12): two rounds, two fix commits per round; resolve all confirmed actionable in-scope findings including Low; escalate remaining blockers on the PR at the cap without declaring clearance. Preserve brief follow-up disposition for out-of-scope Critical/High, broken behaviour, and owner-requested items: check for an existing open ticket before filing eligible follow-ups (Patron rules; Rigger records; other observations logged “noted, no ticket”). Then follow task-pipeline Phase 5 order: rebase → Phase 3 gates on rebased head → Size L ship review → open/update delivery PR with acceptance evidence → verify findings/gitleaks/mergeability → merge-bar receipt. The user owns the merge.

## Dependencies & execution order

- T000 (analyze → plan-plus-ADR challenge → owner Gate 1 merge receipt) → T001 → schema/mapping (T002 ∥ T004 → T003/T005 → T006 → T007) → ranking (T008 → T009 waits on T007+T008 → T010 → T011) → endpoint/codegen after complete reason/response assembly (T012 → T013 ∥ T014) → web (T015 → T016 → T017) → docs/evidence (T018 may proceed once docs files are free; T019 after hydration + ranking + T010 reason/response assembly) → gates/review/ship (T020–T022).
- US1 must land before US2 relies on titled rows. T009 waits for T007 coverage and T008 weights; T009 owns query registration and ranking through cap 20; T010 completes reason/response assembly. US3 waits on complete reason/response assembly (T010). US4 waits on regenerated types. US5 docs can parallelize with later implementation; home-library evidence needs hydration + completed reason/response assembly (T010) + production ranking under the same Option A exclusion.
- Brief sequencing preserved: pre-merge Gate 1 sequence → pickup → persistence/backfill → local query → generated contract → web → docs/E2E/home acceptance → Size L verification and Phase 5 ship.

## Parallel opportunities

- After T001: T002 ∥ T004; T008 may draft against options once unblocked; T018 docs may proceed on disjoint files.
- After T012: T013 API tests ∥ T014 OpenAPI/TS regeneration.
- After T015: T016 web tests may proceed while E2E fixtures are prepared for T017.

## Implementation strategy

1. Pre-merge Gate 1 sequence + pickup (prerequisites and seams only).
2. Additive schema + mapping + enrichment/backfill (US1).
3. Local ranking + reasons + options (US2 MVP).
4. Endpoint + parity tests + codegen (US3).
5. Web section + tests + local E2E (US4).
6. Docs + home-library receipt (US5).
7. Size L gates and pinned review dispositions; Phase 5 rebase → gates → ship review → delivery PR → merge-bar; await owner merge.

## Notes

- `[P]` = disjoint files, prerequisites complete.
- Gate 1 owner merge receipt, Phase B pickup, delivery review caps, and runtime acceptance are distinct.
- Do not invent Not interested, candidate IMDb lookups, Related outside movies, new dependencies, LocalPlay, secrets, or `Process.Start`.
- Use existing test infrastructure. Omit explanatory comments. AGENTS.md permits AAA test headers, intentional empty-block justifications, and narrowly scoped warning suppressions.
- `ticket-text.txt` is a disposable raw intake snapshot (pre-amendment Q3 text) and is omitted from the Spec Kit PR surface; provenance is recon-DEV-561, the DEV-561 task note, and the Q3 Option A ruling.
