# Tasks: DEV-418 For You recommendation rail above the Library browse grid

**Input**: Design documents from `specs/DEV-418/` (`spec.md`, `plan.md`, `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`).

**Prerequisites**: `plan.md` (required), `spec.md` (required). No `research.md`, `data-model.md`, or `contracts/` (`plan.md` records why). Palette `design.md` must exist before plan challenge; it is not authored by these tasks.

**Tests**: Included. Brief Q7 requires Vitest/RTL + typed MSW behavior matrix for forYouApi, ForYouRail, Library eligibility/navigation/cached-state regressions, and generated MSW consumer smoke. Author tests in the same phase as the surfaces they lock (web-implement TDD). Compile-red is permitted while types are introduced inside a phase; the phase is not done until its named targeted proofs are green. Do not start Phase 2 production until Phase 1 drift recon passes, the spec PR has merged, Gate 1 is established, and Phase B prerequisites (landed DEV-357 contract chain, Keel pickup clearance) allow build intake.

**Organization**: Phases follow brief Q7 Task ordering and `spec.md` user stories US1–US3. Anvil implements one phase per ask. Polish is Cog then Gauge.

**Domain vocabulary** (feature-local; do not edit CONTEXT.md): **For You**, **Personal reason**, **Cold start**, **Library browse grid** — definitions in `spec.md` Domain vocabulary.

**propertyTests**: opt-out — frontend presentation/query integration has no new .NET domain invariants (brief Q7). Accepts no-tests-tagged exit 2 only; actual failure still blocks.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 populated For You rail + navigation; US2 hide/suppress states; US3 forYouApi query identity and cache rules.
- Exact file paths are in each description.

## Phase 1: Setup — prerequisite drift recon

**Purpose**: Confirm landed DEV-357 contract before production work.

- [ ] T001 Wisp prerequisite/drift recon against picked HEAD: confirm landed DEV-357 GET /api/recommendations/for-you route; take default/range and error semantics matching cited `specs/DEV-357/spec.md:79-87`; RecommendedMovieDto and typed reasons in generated `web/src/api/openapi.json` / `web/src/api/types.ts` / `web/src/mocks/handlers.ts`; MovieCard assignability of RecommendedMovieDto (`recon-DEV-418:127`); LibraryView eligibility seams (`activeFilters`, `watchlistActive`, Results section); MovieCarousel/MovieCard reuse surfaces; Vitest/MSW conventions under `web/src/test/server.ts` and `web/src/mocks/`. Gauge supplies applicable baseline. Keel supplies pickup/drift clearance. Stop for a cited decision on missing prerequisites or any contract/placement mismatch rather than inventing a substitute. No production edits in this task.

**Checkpoint**: Prerequisites match the brief; Phase B build intake is allowed.

---

## Phase 2: Foundational — typed fixtures and generated MSW consumer smoke

**Purpose**: Generated-type for-you fixtures and MSW consumer smoke against the landed operation confirmed in T001. No UI yet.

**⚠️ CRITICAL**: forYouApi and ForYouRail proofs depend on typed fixtures/handlers existing.

### Tests

- [ ] T002 [P] Write generated MSW consumer smoke in `web/src/mocks/forYou.test.ts` proving the landed for-you operation returns fixture data via typed client GET (pattern `recon-DEV-418:160-167`). AAA headers only.

### Implementation

- [ ] T003 [P] Add generated-type for-you fixtures and success aggregate entry in `web/src/mocks/fixtures.ts`. Consume generated `web/src/mocks/handlers.ts`. If T001 proved regeneration is required, regenerate via C# export → `web/src/api/openapi.json` → `npm run generate:api`; never hand-edit generated outputs or duplicate a handler. Include nonempty ordered rows covering personal reason, empty reasons, structural reason, and nullable poster as needed for later US1 proofs (RelatedMovies fixture precedent `recon-DEV-418:162`).

**Checkpoint**: Named targeted proofs — `mocks/forYou.test.ts` green against landed generated handler/fixtures.

---

## Phase 3: User Story 3 - forYouApi query consumer (Priority: P3 foundational for rail)

**Goal**: Independent feature-local TanStack Query consumer with key `['forYou', 10]`, take=10, cancellation, retry:false, and quiet cache rules.

**Independent Test**: `forYouApi.test.ts` proves take=10, request cancellation, distinct query identity, order preservation through the consumer, and that the query uses the shared client without a new QueryClient.

### Tests

- [ ] T004 [P] [US3] Write Vitest proofs in `web/src/features/for-you/forYouApi.test.ts`: take=10 request; query key `['forYou', 10]`; cancellation signal forwarded; distinct identity from browse/relatedMovies keys; retry:false retained. Use existing test server and typed MSW overrides. AAA headers only.

### Implementation

- [ ] T005 [US3] Add `web/src/features/for-you/forYouApi.ts` using existing shared QueryClient and generated `api/client.ts` client.GET for `/api/recommendations/for-you` with `take:10`, key `['forYou', 10]`, signal forwarding, retry:false. No new QueryClient, state library, npm dependency, wrapper layer, or hand-rolled DTO duplicates. Do not add feedback/enrichment invalidation edits.

**Checkpoint**: Named targeted proofs — `forYouApi.test.ts` green for US3 query identity and take=10.

---

## Phase 4: User Story 1 + User Story 2 - ForYouRail presentation and quiet/hide states

**Goal**: ForYouRail composes unchanged MovieCarousel/MovieCard; shows populated ordered recommendations with typed reasons; hides on pending/error/200 []; eligibility-gated render with quiet async behavior.

**Independent Test**: `ForYouRail.test.tsx` proves populated order/reasons/poster via MovieCard; For You accessible label; 200 [] hides heading and rail; pending and 400/422/500/network failure render nothing with no rail-specific error UI; cached nonempty remains while eligible and successful [] clears it.

### Tests

- [ ] T006 [P] [US1] [US2] Write Vitest/RTL proofs in `web/src/features/for-you/ForYouRail.test.tsx` covering brief Q7 matrix for the rail surface: returned order; personal reasons; structural fallback; empty reasons; nullable poster through unchanged MovieCard; For You accessible carousel label; 200 [] hides heading and rail; pending and 400/422/500/network failure leave no rail UI; cached nonempty refresh remains visible while eligible; successful [] clears it. Also prove focus continuity from `design.md:301-311`: nonempty refresh that removes the focused card moves focus to the For You heading; successful empty refresh that removes the whole shelf while focus is inside moves focus to stable Library heading `h2.library-title` (or no-op when that landmark is missing); focus outside the shelf is preserved across refresh/hide. Use typed fixtures and per-test MSW overrides. AAA headers only. Do not assert implementation internals or duplicate backend ranking tests.

### Implementation

- [ ] T007 [US1] [US2] Add `web/src/features/for-you/ForYouRail.tsx` and required `web/src/features/for-you/ForYouRail.css` (exactly three rules per `design.md:194-211`: `.for-you-rail`, `.for-you-heading`, `.for-you-card`): heading and MovieCarousel label For You; compose unchanged MovieCarousel `{label,children}`; pass generated movie to MovieCard.movie and movie.reasons to reasons; pass caller-supplied openMovie and feedback clients; quiet pending/error (render nothing); hide complete rail including heading on 200 []; preserve API order; never invent Personal explanations. Implement rail-owned focus continuity inside `ForYouRail.tsx` per `design.md:284-311` / `301-311`: focused-card removal on nonempty refresh → For You heading (`h2`, `tabIndex={-1}`); whole-shelf removal on successful `[]` (and unmount cleanup when focus is inside) → Library `h2.library-title` with scoped lookup, temporary `tabindex="-1"`, missing-landmark no-op; preserve focus when it is outside the shelf. Conformance to Palette `design.md` without changing product decisions in the brief.

**Checkpoint**: Named targeted proofs — `ForYouRail.test.tsx` green for US1 populated presentation and US2 quiet/hide states.

---

## Phase 5: User Story 1 + User Story 2 - LibraryView integration and eligibility regressions

**Goal**: Mount ForYouRail above Results with inline eligibility; prove filter/watchlist suppression, navigation, and browse regressions.

**Independent Test**: `LibraryView.forYou.test.tsx` proves placement before Results; active filters and watchlist-only suppress mount and new requests including cached and in-flight cases; clearing filters restores eligibility; existing openMovie/modified-click behavior intact; Library grid remains usable.

### Tests

- [ ] T008 [US1] [US2] Write Vitest/RTL proofs in `web/src/library/LibraryView.forYou.test.tsx`: For You placement above Results when eligible and nonempty; activeFilters and watchlistActive suppress cached rail and initiate no new requests; in-flight completion cannot remount while ineligible; clearing filters restores eligibility; existing openMovie/details callback and modified-click link behavior remain intact; pending/error leave Library grid usable. Also prove eligibility-unmount focus continuity from `design.md:301-311`: when the shelf unmounts because filters/watchlist become active while focus is inside the shelf, focus moves to stable Library heading `h2.library-title` (missing-landmark no-op); focus outside the shelf is preserved. Same-folder sequential work with T009 — do not parallelize conflicting LibraryView edits.

### Implementation

- [ ] T009 [US1] [US2] Integratively mount ForYouRail in `web/src/library/LibraryView.tsx` above `<section aria-label="Results">` with eligibility `!activeFilters && !watchlistActive` using existing calculations (`recon-DEV-418:130-138`). Wire existing openMovie callback and feedback clients. Suppress mount and new request execution when ineligible. Focus continuity on eligibility unmount is owned by ForYouRail cleanup (T007); host only gates mount. Preserve all existing Library functionality. No unrelated rewrites; no movieFeedbackStore/movieDetailsQueries edits.

**Checkpoint**: Host integration green; eligibility/navigation/cached-state regressions green; existing Library browse regressions green.

---

## Phase 6: Polish — full web suite, Cog refactor, and Gauge gates

**Purpose**: Full existing web suite; diff refactor; applicable Phase 3 gate bar.

- [ ] T010 Run the full existing web suite and `scripts/run-web-gates.ps1`. Fix only in-scope failures; do not invent gate flags, thresholds, or exemptions.

- [ ] T011 Cog `/refactor` on the delivery diff only (helpers/early returns/guards; no scope expansion; no threshold changes; no new packages).

- [ ] T012 Gauge gates: `scripts/run-web-gates.ps1`; full web suite; and all applicable task-pipeline Phase 3 gates for this ui slice (Roslyn, complexity, InspectCode, mutation where applicable, property tests, vulnerable packages, format verification, dotnet tests). Record command, head, and actual exit/verdict. `./scripts/run-property-tests.ps1` honors `propertyTests: opt-out — frontend presentation/query integration has no new .NET domain invariants` (no-tests-tagged exit 2 only). Report SKIPPED (scope-empty) / SKIP / NOT APPLICABLE verbatim — never as PASS. Exit 1, Could not run, or unaccepted property exit 2 blocks. Do not lower `harness.yml` thresholds. Do not edit CONTEXT.md or add/promote an ADR. Do not claim Phase A gate results.

**Checkpoint**: Delivery ready for review loops under brief Q8 closing bar (zero unresolved Critical/High/Medium; Low recorded; plan challenge and remediation each at most two rounds; at most two fix commits per round).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Blocks all production — Wisp recon must pass or stop for decision.
- **Phase 2 (Fixtures/MSW smoke)**: Depends on Phase 1; blocks consumer and rail proofs that need typed fixtures.
- **Phase 3 (US3 forYouApi)**: Depends on Phase 2; blocks ForYouRail that consumes the query.
- **Phase 4 (US1/US2 ForYouRail)**: Depends on Phase 3; blocks host integration that mounts the rail.
- **Phase 5 (US1/US2 LibraryView)**: Depends on Phase 4.
- **Phase 6 (Polish)**: Depends on Phases 2–5 complete.

### User Story Dependencies

- **US3 (P3)**: forYouApi — Phase 3; independently testable before ForYouRail exists.
- **US2 (P2)**: Hide/suppress — Phase 4 rail states + Phase 5 host eligibility.
- **US1 (P1)**: Populated rail + navigation — Phase 4 cards + Phase 5 host integration.

### Parallel Opportunities

- T002 and T003 can proceed in parallel after T001 clears the contract.
- T004 can scaffold alongside T005 once the landed operation shape from T001 is known.
- T006 can scaffold alongside T007 once forYouApi from Phase 3 exists.
- T008 and T009 are same-folder LibraryView edits — run sequentially.

### Parallel Example: Phase 2 fixtures

```text
Task: "Generated MSW consumer smoke in web/src/mocks/forYou.test.ts"
Task: "Generated-type for-you fixtures in web/src/mocks/fixtures.ts"
```

---

## Implementation Strategy

### MVP First (US3 query + US1/US2 rail)

1. Complete Phase 1 recon (stop on divergence).
2. Complete Phase 2 fixtures + MSW smoke.
3. Complete Phase 3 forYouApi + proofs.
4. Complete Phase 4 ForYouRail + quiet/hide proofs.
5. **STOP and VALIDATE**: ForYouRail independently before host integration.
6. Add Phase 5 LibraryView integration + eligibility regressions.
7. Phase 6 full web suite, Cog refactor, Gauge gates.

### Incremental Delivery

1. Setup recon → foundation ready or blocked with cited decision.
2. Fixtures/MSW → contract smoke.
3. forYouApi → query identity and take=10 (US3).
4. ForYouRail → populated presentation and quiet/hide (US1/US2).
5. LibraryView integration → Library delivery.
6. Suite + refactor + gates.

### Notes

- [P] tasks = different files, no unfinished dependency.
- Do not invent route defaults, DTO fields, or handler duplicates beyond landed DEV-357 / T001 facts.
- Do not commit secrets; do not touch LocalPlay / Process.Start.
- Preserve brief `propertyTests: opt-out` disposition; do not add FsCheck suites for this ticket.
- Frozen scope (brief Q8) governs review findings.
- Copy and quiet-state micro-interactions come from `ASSUMPTIONS.md`; Palette owns responsive layout/reason placement/keyboard-focus visuals without changing product decisions.
