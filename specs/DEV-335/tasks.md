# Tasks: DEV-335 Add IMDb rating range filter

**Input**: Design documents from `specs/DEV-335/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: filled `plan.md` (required; Constitution Check, file envelope, Q1–Q8 design, Q6 proof, Q7 order/gates, Palette prerequisite, pickup boundary, closing bar), `spec.md` (required). No `research.md`, `data-model.md`, `contracts/`, or `quickstart.md` — reasons in `plan.md` Documentation omissions.

**Tests**: Included. Brief Q6 + Round 1 amendment require them and ticket AC names integration and slider proof. Author tests before (or with) the production change they lock within each phase; keep passing phase checkpoints. Do **not** add a universal observed-red gate from this challenge. A phase is not done until its listed tests pass. Do not start Phase B implementation until the spec PR has merged / Gate 1 process clears and pickup verifies dependency landing plus current-main drift. Plan challenge/fix cap remains two rounds; freeze/Gate 1 pending Keel verification.

**Organization**: Phases follow brief Q7 build order. User stories map to spec.md US1–US3.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 catalog filter, US2 HTTP/contract, US3 Filter Panel.
- Exact file paths are in each description.

## Phase 1: Setup (Phase B pickup)

**Purpose**: Confirm the implementation base before any production edit.

- [X] T001 Verify dependencies DEV-322, DEV-297, and DEV-302 have landed on the authorized implementation base and recheck current-main drift for the planned file envelope in `specs/DEV-335/plan.md`. If either check fails, stop and report; do not begin Phase 2. Record the verified base commit in the work notes. No production file edits in this task.

---

## Phase 2: Foundational (Rating model, codec, validator)

**Purpose**: Core query types that MUST exist before catalog, HTTP, or UI work.

**⚠️ CRITICAL**: No user story implementation begins until this phase is green.

### Tests

- [X] T002 [P] Extend `tests/LamuFlix.UnitTests/Library/MovieQueryTests.cs` so `MovieQuery.Rating` participates in equality and hash: queries that differ only by rating range compare unequal; equivalent rating ranges compare equal and share a hash; `HasSameCoreValues` includes Rating with Text/Runtime/Year. Name methods `Method_Condition_Expected`. AAA comments only. If pre-PR mutation requires rating mutants killed here, also extend only `tests/LamuFlix.UnitTests/Library/MovieQuerySurvivorTests.cs` with rating-specific proof (no new task ID; no unrelated survivor sweep).

- [X] T003 [P] Extend `tests/LamuFlix.UnitTests/Library/MovieQueryStringTests.cs` for invariant decimal round trips; absent group; open bounds; full-span present group; Format shortest ≤1 fractional digit (`7`, `7.1`, `0`, `10`); `7`/`7.0`/accepted `07.0` equivalent. Failure matrix MUST cover duplicate, partial, out-of-domain, inverted, and the shared lexical rejects: `+7`, `-0`, `1e1`, surrounding whitespace, `7,0`, `7.`, `.5`, `7.00`, `7.10`, `NaN`. Preserve absent versus present-with-null-bounds. Name methods `Method_Condition_Expected`. If mutation requires, also extend only `tests/LamuFlix.UnitTests/Library/MovieQueryStringSurvivorTests.cs` with rating-specific proof (no new task ID).

- [X] T004 [P] Extend `tests/LamuFlix.UnitTests/Library/MovieQueryValidatorTests.cs` for numeric domain via `ImdbRating.TryCreate` semantics (0.0..10.0, one decimal), null/open bounds valid, each non-null bound independently valid, and min <= max only when both bounds exist. Name methods `Method_Condition_Expected`.

- [X] T005 [P] Extend `tests/LamuFlix.UnitTests/Library/MovieQueryFixture.cs` so the existing property suite can generate valid `RatingRange` values (inclusive 0.0–10.0, ≤1 decimal; open bounds allowed) without inventing a new test framework.

### Implementation

- [X] T006 Add `src/LamuFlix.Core/Library/RatingRange.cs` as `public sealed record RatingRange(decimal? Min, decimal? Max)`.

- [X] T007 Update `src/LamuFlix.Core/Library/MovieQuery.cs`: add `RatingRange? Rating`; include Rating in `Equals` / `GetHashCode` / `HasSameCoreValues`. No comments.

- [X] T008 Update `src/LamuFlix.Core/Library/MovieQueryString.cs`: canonical keys `minRating`/`maxRating`; shared Round 1 lexical parse (ASCII unsigned `[0-9]+(?:\.[0-9])?`, leading zeros accepted, reject sign/whitespace/exponent/comma/leading-trailing-dot/`7.00`/`7.10`); Format shortest invariant ≤1 fractional digit; HasAny/HasComplete/Append wiring; reject invalid and duplicate rating keys via existing `Seen` pattern. No new parser layer/file. Depends on T006–T007.

- [X] T009 Update `src/LamuFlix.Infrastructure/Library/MovieQueryValidator.cs`: reuse public `ImdbRating.TryCreate` for numeric domain/precision (do not expose/edit private `IsValid`; no `ImdbRating.cs` edit); null bounds valid; validate each non-null bound; compare min <= max only when both exist. Depends on T007.

**Checkpoint**: Unit and fixture tests for model/codec/validator pass. No catalog or HTTP change yet.

---

## Phase 3: User Story 1 - Browse by IMDb rating range (Priority: P1) 🎯 MVP

**Goal**: `EfMovieCatalog` filters by inclusive rating bounds and excludes unrated whenever a range is present.

**Independent Test**: Real-PostgreSQL `EfMovieCatalogBrowseTests` covering `RatingRange(7m, null)`, inclusive 0/7/10 and fractional/equal bounds, unrated excluded when range present and retained when absent, defensive invalid-empty, AND composition, count-before-paging.

### Tests for User Story 1

- [X] T010 [US1] Extend `tests/LamuFlix.IntegrationTests/EfMovieCatalogBrowseTests.cs` on real Testcontainers PostgreSQL (no data-driver mocks): minimum 7 with open maximum; inclusive endpoints including 0, 7, 10 and fractional/equal bounds; unrated excluded for present range and retained with absent range; defensive empty for invalid/inverted bounds proving total 0 and empty-page handling; AND filtering with another filter; total count reflects filtered set before paging. Retain the existing suite translation posture — do not invent client-evaluation harness switches. Name methods `Method_Condition_Expected`. If mutation requires, also extend only `tests/LamuFlix.IntegrationTests/EfMovieCatalogSurvivorTests.cs` with rating-specific proof (no new task ID).

### Implementation for User Story 1

- [X] T011 [US1] Add a small `WhereRating` to `src/LamuFlix.Infrastructure/Persistence/MovieRecordQueryExtensions.cs` mirroring `WhereYear`: when `query.Rating` is non-null, require non-null `ImdbRating`, apply inclusive min/max for non-null ends using `ImdbRating.TryCreate` for defensive numeric domain/precision, and map invalid/inverted bounds to the existing defensive empty-set path (total 0 / empty page). Decimal-specific private helpers only if required; no generic range framework. Depends on T006–T009 and T010 authored.

- [X] T012 [US1] Chain `.WhereRating(...)` in `src/LamuFlix.Infrastructure/Persistence/EfMovieCatalog.cs` `BrowseAsync` with the existing predicate chain before `CountAsync` and paging. Do not change sort/order dispatch. Depends on T011.

**Checkpoint**: US1 integration tests pass. Catalog-level minRating=7 and unrated exclusion are proven.

---

## Phase 4: User Story 2 - Pass rating bounds on the movies API (Priority: P2)

**Goal**: Paired canonical `minRating`/`maxRating` on `GET /api/movies` with 422 validation and regenerated contract artifacts.

**Independent Test**: `LibraryEndpointsTests` paired-key/422 matrix; `OpenApiContractTests` exact parameter list + drift after regeneration; generated TS/MSW match baseline.

### Tests for User Story 2

- [X] T013 [P] [US2] Extend `tests/LamuFlix.IntegrationTests/LibraryEndpointsTests.cs` for paired canonical camelCase wire keys `minRating`/`maxRating`, decimals and open values (including accepted `07.0`), and 422 for partial, duplicate, out-of-domain, inverted, and the same lexical rejects as T003 (`+7`, `-0`, `1e1`, whitespace, `7,0`, `7.`, `.5`, `7.00`, `7.10`, `NaN`). Name methods `Method_Condition_Expected`.

- [X] T014 [P] [US2] Extend `tests/LamuFlix.IntegrationTests/OpenApiContractTests.cs` exact BrowseMovies parameter-list equality assertion to include documented/generated PascalCase `MinRating`/`MaxRating` while clarifying canonical raw wire keys remain camelCase `minRating`/`maxRating` (no rename, no new DTO, no subset match).

### Implementation for User Story 2

- [X] T015 [US2] Update `src/LamuFlix.Api/Endpoints/BrowseMoviesRequest.cs` with string-bound `MinRating` and `MaxRating` documenting canonical `minRating`/`maxRating`. Place new properties inside the existing scoped ReSharper disable/restore pair; preserve existing comments; add no new explanatory comments. Leave existing parameter names unchanged; no new DTO.

- [X] T016 [US2] Update `src/LamuFlix.Api/Endpoints/LibraryEndpoints.cs` with decimal rating parse path that shares T008's Round 1 lexical rule, paired-key require-both semantics, duplicate MarkSeenOnce → 422, and mapping into `MovieQuery.Rating`. No silent rounding/clamping. Depends on T015 and Phase 2 codec rules.

- [X] T017 [US2] After T015–T016 compile: regenerate `web/src/api/openapi.json` and `tests/LamuFlix.IntegrationTests/Snapshots/OpenApiContractTests.DriftMatchesCommittedBaseline.verified.json` with `LAMUFLIX_REGENERATE_OPENAPI=true`, clear the flag, then run `npm run generate:api` so `web/src/api/types.ts` and `web/src/mocks/handlers.ts` update. Never hand-edit generated files. Confirm T013–T014 and drift tests pass.

**Checkpoint**: HTTP accepts valid paired rating keys, rejects invalid input with 422, and contract drift is green on regenerated artifacts.

---

## Phase 5: User Story 3 - Adjust the IMDb rating slider (Priority: P3)

**Goal**: Live Filter Panel DualRangeField updates URL, API params, grid, and match count.

**Independent Test**: Vitest coverage in FilterPanel, filterQuery, and LibraryView.composed for step/extents, absent vs active full-span, open-bound hydration, Clear all, page=1, paired decimal params, and slider-triggered fetch changing visible grid/count while preserving other filters and unknown keys.

### Tests for User Story 3

- [ ] T018 [P] [US3] Extend `web/src/features/library/filterQuery.test.ts` for rating keys in serialize/fromParams: absent → no keys; active full-span → `minRating=0&maxRating=10` (not elided); both-empty → keys present with empty values; open-bound preservation; shortest ≤1dp emission; interaction-only float normalization (e.g. `7.300000000000001` → `7.3`); raw-invalid and partial groups preserve raw key presence/values (partial stays partial; never invent the missing pair; never repair raw URL text); unknown keys preserved; unrelated-filter preservation; presence-based active detection (not value/extent comparison).

- [ ] T019 [P] [US3] Extend `web/src/features/library/FilterPanel.test.tsx` for IMDb rating DualRangeField: extent 0..10 step 0.1; one-decimal captions; keyboard and non-crossing thumbs; absent displays `[0,10]` inactive; first change activates; return to `[0,10]` stays active until Clear all; open-bound hydration without URL mutation; safe `[0,10]` display for invalid/partial/domain/precision/order URL input without mutating raw URL values; float-hostile keyboard/step interaction commits shortest ≤1dp; page reset and `window.location.search` updates on commit.

- [ ] T020 [P] [US3] Extend `web/src/library/LibraryView.composed.test.tsx` with test-local `server.use` rating-dependent MSW responses (do not hand-edit generated `handlers.ts`): assert the actual outgoing browse request carries paired canonical `minRating`/`maxRating`, visible grid/count update on slider-driven change, other filters preserved, rating counted as one active filter group, and invalid malformed/domain/precision/order/partial rating URLs fetch 422 and show the existing invalid-filter UI without URL rewrite (read `libraryQueries.ts` classification only; do not edit it).

### Implementation for User Story 3

- [ ] T021 [US3] Extend `web/src/features/library/filterQuery.ts` with `minRating`/`maxRating` in recognized keys and `FilterQuery`; presence-based `isRatingActive` for VALID complete groups (both keys present, even empty/extent-valued — never value/extent-elision). For a VALID complete group, serialize emits both keys (`null`→`''`, `0`→`"0"`, `10`→`"10"`) with shortest invariant ≤1dp output; normalize slider float noise once on interaction. Invalid/partial input uses a separate raw pass-through branch that preserves exactly the supplied key presence/value and bypasses valid-group active detection/canonicalization (partial stays partial; never invent the missing key; never repair raw URL text). fromParams preserves absent vs present-with-null-bounds. Do not edit `web/src/library/libraryUrl.ts`. Depends on T017 generated types being available for any typed browse params.

- [ ] T022 [US3] Extend `web/src/library/browseParams.ts` so valid present complete groups emit paired canonical camelCase `minRating`/`maxRating` (including full-span and both-empty); invalid/partial groups forward exactly the supplied raw rating key presence/values even when `isRatingActive` is false; never synthesize a missing pair. Consume generated PascalCase type names without hand-rolled DTO duplicates. Depends on T021.

- [ ] T023 [US3] Update `web/src/features/library/FilterPanel.tsx`: reuse local `DualRangeField` after Release year; label `IMDb rating`; accessible min/max names; one-decimal captions; extent 0..10 step 0.1; absent/active/full-span and open-bound hydration semantics from Q4; invalid display safe `[0,10]` without URL rewrite; deliberate rating-thumb interaction replaces BOTH bounds with valid finite displayed bounds (interaction-normalized via T021); Clear all resets Rating to absent. Reuse existing commit/page-reset/URL ownership. No new CSS dependency. Depends on T021.

- [ ] T024 [US3] Update `web/src/library/LibraryView.tsx` so present rating counts as one active filter group and continues to drive browse via existing `buildBrowseParams` / queryKey / grid/count path. Depends on T022–T023.

**Checkpoint**: Slider updates URL and visible results. Ticket AC "slider updates grid and URL" is proven in Vitest.

---

## Phase 6: Polish & gates

**Purpose**: Configured verification and three-axis M review readiness. No new behavior.

- [ ] T025 Run configured backend gates on the changed scope and record each script verdict without lowering thresholds and without passing literal threshold overrides (scripts read `harness.yml`): `./scripts/run-roslyn-analyzers.ps1`, `./scripts/run-cyclomatic-complexity.ps1` (refactor pass uses the configured harness threshold when at that stage — do not hardcode a number), `./scripts/run-jetbrains-inspectcode.ps1`, `./scripts/run-property-tests.ps1`, `./scripts/run-vulnerable-packages.ps1`, `dotnet format --verify-no-changes`, `dotnet test`, and pre-PR `./scripts/run-mutation.ps1` per AGENTS.md. If mutation survivors require it, add rating-specific cases only in the three recon-named survivor suites already named under T002/T003/T010 — no new task IDs, no unrelated sweep. Exit 2 = SKIPPED (scope-empty) / SKIP / mutation N/A as reported — never PASS. Could not run blocks.

- [ ] T026 Run applicable web gates (tests, lint, typecheck, build, API drift / `check:api`) via the repo’s configured web commands for this stage. Record verdicts; scope-empty skip is not PASS.

- [ ] T027 When M review starts, require all three axes — Sentry, Ledger, and Compass — against the frozen scope and closing bar in `specs/DEV-335/brief.md` (Critical/High/Medium blocking; Low non-blocking; max two review/fix rounds). A missing axis is blocking.

---

## Excluded (never scheduled)

New NuGet/npm dependencies; new project or architectural layer; schema/migration; sort/order changes; CSS/slider package; edits to `ImdbRating.cs` / storage mapping; `libraryUrl.ts`; `libraryQueries.ts`; LocalPlay/secrets/`Process.Start`; unrelated file rewrites; hand-edits to generated OpenAPI/TS/MSW; new DTO or shared parser layer/file; universal observed-red gate; literal gate-threshold overrides; ADR or glossary rewrite; lowering any `harness.yml` threshold; unrelated survivor-suite sweep. A discovered need outside this list stops for a decision.

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: No earlier phase. Blocks all implementation.
- **Phase 2**: Depends on T001. Blocks US1–US3.
- **Phase 3 (US1)**: Depends on Phase 2. MVP catalog filter.
- **Phase 4 (US2)**: Depends on Phase 2 (and practically US1 for end-to-end browse with rating, though endpoint mapping can compile against Phase 2 types). Contract regeneration (T017) blocks US3 typed/API param wiring.
- **Phase 5 (US3)**: Depends on T017 (generated contract) and Phase 2 semantics.
- **Phase 6**: Depends on US1–US3 complete.

### User Story Dependencies

- **US1 (P1)**: After Phase 2. No dependency on US2/US3.
- **US2 (P2)**: After Phase 2. Uses the same `MovieQuery.Rating` model. Independent HTTP proof.
- **US3 (P3)**: After contract regeneration (T017). UI proof is independent of catalog tests but requires live API param names.

### Within Each Phase

- Author listed tests before or with the production edit they lock; phase commit waits until those tests pass.
- T006 before T007–T009; T011 before T012; T015 before T016 before T017; T021 before T022–T024.

### Parallel Opportunities

- T002–T005 can be authored in parallel (different test files).
- T013 and T014 can be authored in parallel.
- T018–T020 can be authored in parallel.
- T006–T009 are sequential on shared types once tests exist.
- Do not parallelize T017 with hand edits to generated files.

## Parallel Example: Foundational tests

```text
Task: "T002 Extend MovieQueryTests for Rating equality/hash"
Task: "T003 Extend MovieQueryStringTests for decimal rating codec"
Task: "T004 Extend MovieQueryValidatorTests for rating rules"
Task: "T005 Extend MovieQueryFixture for valid RatingRange generation"
```

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1 pickup and Phase 2 model/codec/validator.
2. Complete Phase 3 catalog filter + Postgres proof.
3. Stop to validate ticket AC minRating=7 and unrated exclusion at catalog level before HTTP/UI.

### Incremental Delivery

1. US2 adds the public wire contract and regeneration.
2. US3 adds the Filter Panel slider and URL/grid proof.
3. Phase 6 records gate and M-axis verdicts. No extra behavior.

### Parallel Team Strategy

One implementer in phase order is the expected path. Safe splits are parallel test authorship within a phase (T002–T005, T013–T014, T018–T020). Do not split two writers onto `FilterPanel.tsx` or `MovieQueryString.cs`.

## Notes

- [P] tasks use different files and do not depend on an unfinished task.
- Story labels map to `spec.md`.
- Commit messages use `DEV-335 - {subject}` when implementation commits are authorized.
- Gate 1 / plan freeze / Palette design prerequisite remain separate from this task list.
- Taste items in `ASSUMPTIONS.md` are settled for drafting; Palette still owns `design.md` before challenge.
